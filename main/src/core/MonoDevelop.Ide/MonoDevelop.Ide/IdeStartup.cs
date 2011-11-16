//
// IdeStartup.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2011 Xamarin Inc (http://xamarin.com)
// Copyright (C) 2005-2011 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using Mono.Unix;

using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Instrumentation;
using System.Diagnostics;
using MonoDevelop.Projects;
using System.Collections.Generic;

namespace MonoDevelop.Ide
{
	public class IdeStartup: IApplication
	{
		Socket listen_socket   = null;
		ArrayList errorsList = new ArrayList ();
		bool initialized;
		internal static string DefaultTheme;
		static readonly int ipcBasePort = 40000;
		
		int IApplication.Run (string[] args)
		{
			var options = MonoDevelopOptions.Parse (args);
			if (options.Error != null || options.ShowHelp)
				return options.Error != null? -1 : 0;
			return Run (options);
		}
		
		int Run (MonoDevelopOptions options)
		{
			Counters.Initialization.BeginTiming ();
			
			if (options.LogCounters) {
				string logFile = Path.Combine (Environment.CurrentDirectory, "monodevelop.clog");
				LoggingService.LogInfo ("Logging instrumentation service data to file: " + logFile);
				InstrumentationService.StartAutoSave (logFile, 1000);
			}
			
			Counters.Initialization.Trace ("Initializing GTK");
			SetupExceptionManager ();
			
			try {
				MonoDevelop.Ide.Gui.GLibLogging.Enabled = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error initialising GLib logging.", ex);
			}
			
			//OSXFIXME
			var args = options.RemainingArgs.ToArray ();
			Gtk.Application.Init ("monodevelop", ref args);
			
			//default to Windows IME on Windows
			if (Platform.IsWindows && Mono.TextEditor.GtkWorkarounds.GtkMinorVersion >= 16) {
				var settings = Gtk.Settings.Default;
				var val = Mono.TextEditor.GtkWorkarounds.GetProperty (settings, "gtk-im-module");
				if (string.IsNullOrEmpty (val.Val as string))
					Mono.TextEditor.GtkWorkarounds.SetProperty (settings, "gtk-im-module", new GLib.Value ("ime"));
			}
			
			InternalLog.Initialize ();
			string socket_filename = null;
			EndPoint ep = null;
			
			DispatchService.Initialize ();
			
			// Set a synchronization context for the main gtk thread
			SynchronizationContext.SetSynchronizationContext (new GtkSynchronizationContext ());
			
			AddinManager.AddinLoadError += OnAddinError;
			
			var startupInfo = new StartupInfo (args);
			
			// If a combine was specified, force --newwindow.
			
			if(!options.NewWindow && startupInfo.HasFiles) {
				Counters.Initialization.Trace ("Pre-Initializing Runtime to load files in existing window");
				Runtime.Initialize (true);
				foreach (var file in startupInfo.RequestedFileList) {
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file.FileName)) {
						options.NewWindow = true;
						break;
					}
				}
			}
			
			DefaultTheme = Gtk.Settings.Default.ThemeName;
			if (!string.IsNullOrEmpty (IdeApp.Preferences.UserInterfaceTheme))
				Gtk.Settings.Default.ThemeName = IdeApp.Preferences.UserInterfaceTheme;
			
			//don't show the splash screen on the Mac, so instead we get the expected "Dock bounce" effect
			//this also enables the Mac platform service to subscribe to open document events before the GUI loop starts.
			if (Platform.IsMac)
				options.NoLogo = true;
			
			IProgressMonitor monitor;
			
			if (options.NoLogo) {
				monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
			} else {
				monitor = SplashScreenForm.SplashScreen;
				SplashScreenForm.SplashScreen.ShowAll ();
			}
			
			Counters.Initialization.Trace ("Initializing Runtime");
			monitor.BeginTask (GettextCatalog.GetString ("Starting " + BrandingService.ApplicationName), 3);
			monitor.Step (1);
			Runtime.Initialize (true);
			
			//make sure that the platform service is initialised so that the Mac platform can subscribe to open-document events
			Counters.Initialization.Trace ("Initializing Platform Service");
			DesktopService.Initialize ();
			
			monitor.Step (1);

			if (options.IpcTcp) {
				listen_socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
				ep = new IPEndPoint (IPAddress.Loopback, ipcBasePort + HashSdbmBounded (Environment.UserName));
			} else {
				socket_filename = "/tmp/md-" + Environment.GetEnvironmentVariable ("USER") + "-socket";
				listen_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
				ep = new UnixEndPoint (socket_filename);
			}
				
			// If not opening a combine, connect to existing monodevelop and pass filename(s) and exit
			if (!options.NewWindow && startupInfo.HasFiles) {
				try {
					StringBuilder builder = new StringBuilder ();
					foreach (var file in startupInfo.RequestedFileList) {
						builder.AppendFormat ("{0};{1};{2}\n", file.FileName, file.Line, file.Column);
					}
					listen_socket.Connect (ep);
					listen_socket.Send (Encoding.UTF8.GetBytes (builder.ToString ()));
					return 0;
				} catch {
					// Reset the socket
					if (null != socket_filename && File.Exists (socket_filename))
						File.Delete (socket_filename);
				}
			}
			
			Counters.Initialization.Trace ("Checking System");
			string version = Assembly.GetEntryAssembly ().GetName ().Version.Major + "." + Assembly.GetEntryAssembly ().GetName ().Version.Minor;
			
			if (Assembly.GetEntryAssembly ().GetName ().Version.Build != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Build;
			if (Assembly.GetEntryAssembly ().GetName ().Version.Revision != 0)
				version += "." + Assembly.GetEntryAssembly ().GetName ().Version.Revision;
			
			// System checks
			if (!CheckBug77135 ())
				return 1;
			
			if (!CheckQtCurve ())
				return 1;

			CheckFileWatcher ();
			
			Exception error = null;
			int reportedFailures = 0;
			
			try {
				Counters.Initialization.Trace ("Loading Icons");
				//force initialisation before the workbench so that it can register stock icons for GTK before they get requested
				ImageService.Initialize ();
				
				if (errorsList.Count > 0) {
					if (monitor is SplashScreenForm)
						SplashScreenForm.SplashScreen.Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog ((AddinError[]) errorsList.ToArray (typeof(AddinError)), false);
					if (!dlg.Run ())
						return 1;
					if (monitor is SplashScreenForm)
						SplashScreenForm.SplashScreen.Show ();
					reportedFailures = errorsList.Count;
				}
				
				// no alternative for Application.ThreadException?
				// Application.ThreadException += new ThreadExceptionEventHandler(ShowErrorBox);

				Counters.Initialization.Trace ("Initializing IdeApp");
				IdeApp.Initialize (monitor);
				
				// Load requested files
				Counters.Initialization.Trace ("Opening Files");
				IdeApp.OpenFiles (startupInfo.RequestedFileList);
				
				monitor.Step (1);
			
			} catch (Exception e) {
				error = e;
			} finally {
				monitor.Dispose ();
			}
			
			if (error != null) {
				LoggingService.LogFatalError (null, error);
				MessageService.ShowException (error,
				                              GettextCatalog.GetString ("MonoDevelop failed to start. The following error has been reported: ") + error.Message);
				return 1;
			}

			if (errorsList.Count > reportedFailures) {
				AddinLoadErrorDialog dlg = new AddinLoadErrorDialog ((AddinError[]) errorsList.ToArray (typeof(AddinError)), true);
				dlg.Run ();
			}
			
			errorsList = null;
			
			// FIXME: we should probably track the last 'selected' one
			// and do this more cleanly
			try {
				listen_socket.Bind (ep);
				listen_socket.Listen (5);
				listen_socket.BeginAccept (new AsyncCallback (ListenCallback), listen_socket);
			} catch {
				// Socket already in use
			}
			
			initialized = true;
			MessageService.RootWindow = IdeApp.Workbench.RootWindow;
			Thread.CurrentThread.Name = "GUI Thread";
			Counters.Initialization.Trace ("Running IdeApp");
			Counters.Initialization.EndTiming ();
				
			AddinManager.AddExtensionNodeHandler("/MonoDevelop/Ide/InitCompleteHandlers", OnExtensionChanged);
			
			string logAgentEnabled = Environment.GetEnvironmentVariable ("MONODEVELOP_LOG_AGENT_ENABLED");
			if (string.Equals (logAgentEnabled, "true", StringComparison.OrdinalIgnoreCase))
				LaunchCrashMonitoringService ();
			
			IdeApp.Run ();
			
			// unloading services
			if (null != socket_filename)
				File.Delete (socket_filename);
			
			Runtime.Shutdown ();
			InstrumentationService.Stop ();
			
			return 0;
		}
		
		public bool Initialized {
			get { return initialized; }
		}
		
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				try {
					if (typeof(CommandHandler).IsInstanceOfType (args.ExtensionObject))
						typeof(CommandHandler).GetMethod ("Run", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke (args.ExtensionObject, null);
					else
						LoggingService.LogError ("Type " + args.ExtensionObject.GetType () + " must be a subclass of MonoDevelop.Components.Commands.CommandHandler");
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
		}
		
		void OnAddinError (object s, AddinErrorEventArgs args)
		{
			if (errorsList != null)
				errorsList.Add (new AddinError (args.AddinId, args.Message, args.Exception, false));
		}
		
		void LaunchCrashMonitoringService ()
		{
			string enabledKey = "MonoDevelop.CrashMonitoring.Enabled";
			
			if (Platform.IsMac) {
				var crashmonitor = Path.Combine (PropertyService.EntryAssemblyPath, "MonoDevelopLogAgent.app");
				var pid = Process.GetCurrentProcess ().Id;
				var logPath = UserProfile.Current.LogDir.Combine ("LogAgent");
				var email = FeedbackService.ReporterEMail;
				var logOnly = "";
				
				var fileInfo = new FileInfo (Path.Combine (logPath, "crashlogs.xml"));
				if (!PropertyService.HasValue (enabledKey) && fileInfo.Exists && fileInfo.Length > 0) {
					var result = MessageService.AskQuestion ("A crash has been detected",
						"MonoDevelop has crashed recently. Details of this crash along with your configured " +
						"email address can be uploaded to Xamarin to help diagnose the issue. This information " +
						"will be used to help diagnose the crash and notify you of potential workarounds " +
						"or fixes. Do you wish to upload this information?",
						AlertButton.Yes, AlertButton.No);
					PropertyService.Set (enabledKey, result == AlertButton.Yes);
				}
				
				if (string.IsNullOrEmpty (email))
					email = AuthorInformation.Default.Email;
				if (string.IsNullOrEmpty (email))
					email = "unknown@email.com";
				if (!PropertyService.Get<bool> (enabledKey))
					logOnly = "-logonly";

				var psi = new ProcessStartInfo ("open", string.Format ("-a {0} -n --args -p {1} -l {2} -email {3} {4}", crashmonitor, pid, logPath, email, logOnly)) {
					UseShellExecute = false,
				};
				Process.Start (psi);
			}
			//else {
			//	LoggingService.LogError ("Could not launch crash reporter process. MonoDevelop will not be able to automatically report any crash information.");
			//}
		}

		void ListenCallback (IAsyncResult state)
		{
			Socket sock = (Socket)state.AsyncState;

			Socket client = sock.EndAccept (state);
			((Socket)state.AsyncState).BeginAccept (new AsyncCallback (ListenCallback), sock);
			byte[] buf = new byte[1024];
			client.Receive (buf);
			foreach (string filename in Encoding.UTF8.GetString (buf).Split ('\n')) {
				string trimmed = filename.Trim ();
				string file = "";
				foreach (char c in trimmed) {
					if (c == 0x0000)
						continue;
					file += c;
				}
				GLib.Idle.Add (delegate(){ return openFile (file); });
			}
		}

		bool openFile (string file) 
		{
			if (string.IsNullOrEmpty (file))
				return false;
			
			Match fileMatch = StartupInfo.FileExpression.Match (file);
			if (null == fileMatch || !fileMatch.Success)
				return false;
				
			int line = 1,
			    column = 1;
			
			file = fileMatch.Groups["filename"].Value;
			if (fileMatch.Groups["line"].Success)
				int.TryParse (fileMatch.Groups["line"].Value, out line);
			if (fileMatch.Groups["column"].Success)
				int.TryParse (fileMatch.Groups["column"].Value, out column);
				
			try {
				if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file) || 
					MonoDevelop.Projects.Services.ProjectService.IsSolutionItemFile (file)) {
						IdeApp.Workspace.OpenWorkspaceItem (file);
				} else {
						IdeApp.Workbench.OpenDocument (file, line, column);
				}
			} catch {
			}
			IdeApp.Workbench.Present ();
			return false;
		}
		
		bool CheckQtCurve ()
		{
			if (Gtk.Settings.Default.ThemeName == "QtCurve") {
				string msg = "QtCurve theme not supported";
				string desc = "Your system is using the QtCurve GTK+ theme. This theme is known to cause stability issues in MonoDevelop. Please select another theme in the GTK+ Theme Selector.\n\nIf you click on Proceed, MonoDevelop will switch to the default GTK+ theme.";
				AlertButton res = MessageService.GenericAlert (Gtk.Stock.DialogWarning, msg, desc, AlertButton.Cancel, AlertButton.Proceed);
				if (res == AlertButton.Cancel)
					return false;
				Gtk.Settings.Default.ThemeName = "Gilouche";
			}
			return true;
		}
		
		void CheckFileWatcher ()
		{
			string watchesFile = "/proc/sys/fs/inotify/max_user_watches";
			try {
				if (File.Exists (watchesFile)) {
					string val = File.ReadAllText (watchesFile);
					int n = int.Parse (val);
					if (n <= 9000) {
						string msg = "Inotify watch limit is too low (" + n + ").\n";
						msg += "MonoDevelop will switch to managed file watching.\n";
						msg += "See http://monodevelop.com/Inotify_Watches_Limit for more info.";
						LoggingService.LogWarning (msg);
						Runtime.ProcessService.EnvironmentVariableOverrides["MONO_MANAGED_WATCHER"] = 
							Environment.GetEnvironmentVariable ("MONO_MANAGED_WATCHER");
						Environment.SetEnvironmentVariable ("MONO_MANAGED_WATCHER", "1");
					}
				}
			} catch (Exception e) {
				LoggingService.LogWarning ("There was a problem checking whether to use managed file watching", e);
			}
		}
		
		bool CheckBug77135 ()
		{
			try {
				// Check for bug 77135. Some versions of gnome-vfs2 and libgda
				// make MD crash in the file open dialog or in FileIconLoader.
				// Only in Suse.
				
				string path = "/etc/SuSE-release";
				if (!File.Exists (path))
					return true;
					
				// Only run the check for SUSE 10
				StreamReader sr = File.OpenText (path);
				string txt = sr.ReadToEnd ();
				sr.Close ();
				
				if (txt.IndexOf ("SUSE LINUX 10") == -1)
					return true;
					
				string current_libgda;
				string current_gnomevfs;
				string required_libgda = "1.3.91.5.4";
				string required_gnomevfs = "2.12.0.9.2";
				
				StringWriter sw = new StringWriter ();
				ProcessWrapper pw = Runtime.ProcessService.StartProcess ("rpm", "--qf %{version}.%{release} -q libgda", null, sw, null, null);
				pw.WaitForOutput ();
				current_libgda = sw.ToString ().Trim (' ','\n');
				
				sw = new StringWriter ();
				pw = Runtime.ProcessService.StartProcess ("rpm", "--qf %{version}.%{release} -q gnome-vfs2", null, sw, null, null);
				pw.WaitForOutput ();
				current_gnomevfs = sw.ToString ().Trim (' ','\n');
				
				bool fail1 = Addin.CompareVersions (current_libgda, required_libgda) == 1;
				bool fail2 = Addin.CompareVersions (current_gnomevfs, required_gnomevfs) == 1;
				
				if (fail1 || fail2) {
					string msg = GettextCatalog.GetString ("Some packages installed in your system are not compatible with MonoDevelop:\n");
					if (fail1)
						msg += "\nlibgda " + current_libgda + " ("+ GettextCatalog.GetString ("version required: {0}", required_libgda) + ")";
					if (fail2)
						msg += "\ngnome-vfs2 " + current_gnomevfs + " ("+ GettextCatalog.GetString ("version required: {0}", required_gnomevfs) + ")";
					msg += "\n\n";
					msg += GettextCatalog.GetString ("You need to upgrade the previous packages to start using MonoDevelop.");
					
					SplashScreenForm.SplashScreen.Hide ();
					Gtk.MessageDialog dlg = new Gtk.MessageDialog (null, Gtk.DialogFlags.Modal, Gtk.MessageType.Error, Gtk.ButtonsType.Ok, msg);
					dlg.Run ();
					dlg.Destroy ();
					
					return false;
				} else
					return true;
			}
			catch (Exception ex)
			{
				// Just ignore for now.
				Console.WriteLine (ex);
				return true;
			}
		}
		
		void SetupExceptionManager ()
		{
			GLib.ExceptionManager.UnhandledException += delegate (GLib.UnhandledExceptionArgs args) {
				var ex = (Exception)args.ExceptionObject;
				LoggingService.LogError ("Unhandled Exception", ex);
				MessageService.ShowException (ex, "Unhandled Exception");
			};
			AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
				//FIXME: try to save all open files, since we can't prevent the runtime from terminating
				var ex = (Exception)args.ExceptionObject;
				LoggingService.LogFatalError ("Unhandled Exception", ex);
				MessageService.ShowException (ex, "Unhandled Exception. MonoDevelop will now close.");
			};
		}
		
		/// <summary>SDBM-style hash, bounded to a range of 1000.</summary>
		static int HashSdbmBounded (string input)
		{
			ulong hash = 0;
			for (int i = 0; i < input.Length; i++) {
				unchecked {
					hash = ((ulong)input[i]) + (hash << 6) + (hash << 16) - hash;
				}
			}
				
			return (int)(hash % 1000);
		}
		
		public static int Main (string[] args)
		{
			var options = MonoDevelopOptions.Parse (args);
			if (options.ShowHelp || options.Error != null)
				return options.Error != null? -1 : 0;
			
			if (Platform.IsWindows || options.RedirectOutput)
				RedirectOutputToLogFile ();
			
			int ret = -1;
			bool retry = false;
			do {
				try {
					Runtime.SetProcessName (BrandingService.ApplicationName);
					var app = new IdeStartup ();
					ret = app.Run (options);
					break;
				} catch (Exception ex) {
					if (!retry && AddinManager.IsInitialized) {
						LoggingService.LogWarning (BrandingService.ApplicationName + " failed to start. Rebuilding addins registry.");
						AddinManager.Registry.Rebuild (new Mono.Addins.ConsoleProgressStatus (true));
						LoggingService.LogInfo ("Addin registry rebuilt. Restarting MonoDevelop.");
						retry = true;
					} else {
						LoggingService.LogFatalError (BrandingService.ApplicationName + " failed to start. Some of the assemblies required to run MonoDevelop (for example gtk-sharp, gnome-sharp or gtkhtml-sharp) may not be properly installed in the GAC.", ex);
						retry = false;
					}
				} finally {
					Runtime.Shutdown ();
				}
			}
			while (retry);

			CloseOutputLogFile ();

			return ret;
		}

		static void RedirectOutputToLogFile ()
		{
			FilePath logDir = UserProfile.Current.LogDir;
			if (!Directory.Exists (logDir))
				Directory.CreateDirectory (logDir);
			
			//TODO: log rotation
			string file = logDir.Combine ("MonoDevelop.log");
			try {
				if (Platform.IsWindows) {
					//TODO: redirect the file descriptors on Windows, just plugging in a textwriter won't get everything
					RedirectOutputToFileWindows (file);
				} else {
					RedirectOutputToFileUnix (file);
				}
			} catch {
			}
		}

		static StreamWriter logFile;
		static int logFd = -1;
		
		static void CloseOutputLogFile ()
		{
			if (logFile != null) {
				logFile.Dispose ();
				logFile = null;
			}
			if (logFd > -1) {
				Mono.Unix.Native.Syscall.close (logFd);
				logFd = -1;
			}
		}
		
		static void RedirectOutputToFileWindows (string file)
		{
			logFile = new StreamWriter (file);
			logFile.AutoFlush = true;
			Console.SetOut (logFile);
			Console.SetError (logFile);
		}
		
		static void RedirectOutputToFileUnix (string file)
		{
			const int STDOUT_FILENO = 1;
			const int STDERR_FILENO = 2;
			
			Mono.Unix.Native.OpenFlags flags = Mono.Unix.Native.OpenFlags.O_WRONLY
				| Mono.Unix.Native.OpenFlags.O_CREAT | Mono.Unix.Native.OpenFlags.O_TRUNC;
			var mode = Mono.Unix.Native.FilePermissions.S_IFREG
				| Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR
				| Mono.Unix.Native.FilePermissions.S_IRGRP | Mono.Unix.Native.FilePermissions.S_IWGRP;
			
			int fd = Mono.Unix.Native.Syscall.open (file, flags, mode);
			if (fd < 0)
				//error
				return;
			
			int res = Mono.Unix.Native.Syscall.dup2 (fd, STDOUT_FILENO);
			if (res < 0)
				//error
				return;
			
			res = Mono.Unix.Native.Syscall.dup2 (fd, STDERR_FILENO);
			if (res < 0)
				//error
				return;
		}
	}
	
	public class MonoDevelopOptions
	{
		MonoDevelopOptions ()
		{
			IpcTcp = (PlatformID.Unix != Environment.OSVersion.Platform);
		}
		
		Mono.Options.OptionSet GetOptionSet ()
		{
			return new Mono.Options.OptionSet () {
				{ "nologo", "Do not display splash screen.", s => NoLogo = true },
				{ "ipc-tcp", "Use the Tcp channel for inter-process comunication.", s => IpcTcp = true },
				{ "newwindow", "Do not open in an existing instance of " + BrandingService.ApplicationName, s => NewWindow = true },
				{ "h|?|help", "Show help", s => ShowHelp = true },
				{ "clog", "Log internal counter data", s => LogCounters = true },
				{ "clog-interval=", "Interval between counter logs (in milliseconds)", (int i) => LogCountersInterval = i },
				{ "redirect-output", "Whether to redirect stdout/stderr to a log file", s => RedirectOutput = true },
			};
		}
		
		public static MonoDevelopOptions Parse (string[] args)
		{
			var opt = new MonoDevelopOptions ();
			var optSet = opt.GetOptionSet ();
			
			try {
				opt.RemainingArgs = optSet.Parse (args);
			} catch (Mono.Options.OptionException ex) {
				opt.Error = ex.ToString ();
			}
			
			if (opt.Error != null) {
				Console.WriteLine ("ERROR: {0}", opt.Error);
				Console.WriteLine ("Pass --help for usage information.");
			}
			
			if (opt.ShowHelp) {
				Console.WriteLine (BrandingService.ApplicationName + " " + BuildVariables.PackageVersionLabel);
				Console.WriteLine ("Options:");
				optSet.WriteOptionDescriptions (Console.Out);
			}
			
			return opt;
		}
		
		public bool NoLogo { get; set; }
		public bool IpcTcp { get; set; }
		public bool NewWindow { get; set; }
		public bool ShowHelp { get; set; }
		public bool LogCounters { get; set; }
		public int LogCountersInterval { get; set; }
		public bool RedirectOutput { get; set; }
		public string Error { get; set; }
		public IList<string> RemainingArgs { get; set; }
	}
	
	public class AddinError
	{
		string addinFile;
		Exception exception;
		bool fatal;
		string message;
		
		public AddinError (string addin, string message, Exception exception, bool fatal)
		{
			this.addinFile = addin;
			this.message = message;
			this.exception = exception;
			this.fatal = fatal;
		}
		
		public string AddinFile {
			get { return addinFile; }
		}
		
		public string Message {
			get { return message; }
		}
		
		public Exception Exception {
			get { return exception; }
		}
		
		public bool Fatal {
			get { return fatal; }
		}
	}
}
