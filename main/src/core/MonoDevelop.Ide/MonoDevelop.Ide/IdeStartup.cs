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
using MonoDevelop.Core.LogReporting;

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
			
			if (options.PerfLog) {
				string logFile = Path.Combine (Environment.CurrentDirectory, "monodevelop.perf-log");
				LoggingService.LogInfo ("Logging instrumentation service data to file: " + logFile);
				InstrumentationService.StartAutoSave (logFile, 1000);
			}

			//ensure native libs initialized before we hit anything that p/invokes
			Platform.Initialize ();
			
			Counters.Initialization.Trace ("Initializing GTK");
			SetupExceptionManager ();
			
			try {
				MonoDevelop.Ide.Gui.GLibLogging.Enabled = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error initialising GLib logging.", ex);
			}

			SetupTheme ();

			var args = options.RemainingArgs.ToArray ();
			Gtk.Application.Init (BrandingService.ApplicationName, ref args);

			FilePath p = typeof(IdeStartup).Assembly.Location;
			Assembly.LoadFrom (p.ParentDirectory.Combine ("Xwt.Gtk.dll"));
			Xwt.Application.Initialize (Xwt.ToolkitType.Gtk);
			Xwt.Engine.Toolkit.ExitUserCode (null);

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
			
			if (!options.NewWindow && startupInfo.HasFiles) {
				Counters.Initialization.Trace ("Pre-Initializing Runtime to load files in existing window");
				Runtime.Initialize (true);
				foreach (var file in startupInfo.RequestedFileList) {
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file.FileName)) {
						options.NewWindow = true;
						break;
					}
				}
			}
			
			Counters.Initialization.Trace ("Initializing Runtime");
			Runtime.Initialize (true);

			Counters.Initialization.Trace ("Initializing theme and splash window");

			DefaultTheme = Gtk.Settings.Default.ThemeName;
			if (!string.IsNullOrEmpty (IdeApp.Preferences.UserInterfaceTheme)) {
				string theme;
				if (!ValidateGtkTheme (IdeApp.Preferences.UserInterfaceTheme, out theme))
					return 1;
				Gtk.Settings.Default.ThemeName = theme;
			}
			
			//don't show the splash screen on the Mac, so instead we get the expected "Dock bounce" effect
			//this also enables the Mac platform service to subscribe to open document events before the GUI loop starts.
			if (Platform.IsMac)
				options.NoSplash = true;
			
			IProgressMonitor monitor;
			
			if (options.NoSplash) {
				monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
			} else {
				monitor = SplashScreenForm.SplashScreen;
				SplashScreenForm.SplashScreen.ShowAll ();
			}
			
			monitor.BeginTask (GettextCatalog.GetString ("Starting {0}", BrandingService.ApplicationName), 2);

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
				                              BrandingService.BrandApplicationName (GettextCatalog.GetString ("MonoDevelop failed to start. The following error has been reported: ") + error.Message));
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
			
			IdeApp.Run ();
			
			// unloading services
			if (null != socket_filename)
				File.Delete (socket_filename);
			
			Runtime.Shutdown ();
			InstrumentationService.Stop ();
			
			return 0;
		}

		void SetupTheme ()
		{
			// Use the bundled gtkrc only if the Xamarin theme is installed
			if (File.Exists (Path.Combine (Gtk.Rc.ModuleDir, "libxamarin.so")) || File.Exists (Path.Combine (Gtk.Rc.ModuleDir, "libxamarin.dll"))) {
				var gtkrc = "gtkrc";
				if (Platform.IsWindows) {
					gtkrc += ".win32";
				} else if (Platform.IsMac) {
					gtkrc += ".mac";
				}
				Environment.SetEnvironmentVariable ("GTK2_RC_FILES", PropertyService.EntryAssemblyPath.Combine (gtkrc));
			}
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

		internal readonly static string[] FailingGtkThemes = new string[] {
			"QtCurve",
			"oxygen-gtk"
		};

		internal static string[] gtkThemeFallbacks = new string[] {
			"Gilouche", // SUSE
			"Mint-X", // MINT
			"Radiance", // Ubuntu 'light' theme (MD looks better with the light theme in 4.0 - if that changes switch this one)
			"Clearlooks" // GTK theme
		};

		bool ValidateGtkTheme (string requestedTheme, out string validTheme)
		{
			foreach (var theme in FailingGtkThemes) {
				if (requestedTheme == theme) {
					string msg = theme +" theme not supported";
					string desc = "Your system is using the " + theme + " GTK+ theme. This theme is known to cause stability issues in MonoDevelop. Please select another theme in the GTK+ Theme Selector.\n\nIf you click on Proceed, MonoDevelop will switch to the default GTK+ theme.";
					AlertButton res = MessageService.GenericAlert (Gtk.Stock.DialogWarning, msg, desc, AlertButton.Cancel, AlertButton.Proceed);
					if (res == AlertButton.Cancel) {
						validTheme = null;
						return false;
					}
					var themes = MonoDevelop.Ide.Gui.OptionPanels.IDEStyleOptionsPanelWidget.InstalledThemes;
					string fallback = null;
					foreach (string fb in gtkThemeFallbacks) {
						var foundTheme = themes.FirstOrDefault (t => string.Compare (fb, t, StringComparison.OrdinalIgnoreCase) == 0);
						if (foundTheme != null) {
							fallback = foundTheme;
							break;
						}
					}

					validTheme = fallback ?? themes.FirstOrDefault () ?? requestedTheme;
					return validTheme != null;
				}
			}

			validTheme = requestedTheme;
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
						LoggingService.LogWarning (BrandingService.BrandApplicationName (msg));
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
					string msg = GettextCatalog.GetString (
						"Some packages installed in your system are not compatible with {0}:\n",
						BrandingService.ApplicationName
					);
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
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) => {
				HandleException (e.Exception.Flatten (), false);
				e.SetObserved ();
			};
			GLib.ExceptionManager.UnhandledException += delegate (GLib.UnhandledExceptionArgs args) {
				HandleException ((Exception)args.ExceptionObject, args.IsTerminating);
			};
			AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args) {
				HandleException ((Exception)args.ExceptionObject, args.IsTerminating);
			};
			Xwt.Application.UnhandledException += (sender, e) => {
				HandleException (e.ErrorException, false);
			};
		}
		
		void HandleException (Exception ex, bool willShutdown)
		{
			// Log the crash to the MonoDevelop.log file first:
			LoggingService.LogError (string.Format ("An unhandled exception has occured. Terminating MonoDevelop? {0}", willShutdown), ex);
			
			// Pass it off to the reporting service now.
			LogReportingService.ReportUnhandledException (ex, willShutdown);
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
			
			LoggingService.Initialize (options.RedirectOutput);
			
			int ret = -1;
			bool retry = false;
			do {
				try {
					var exename = Path.GetFileNameWithoutExtension (Assembly.GetEntryAssembly ().Location);
					Runtime.SetProcessName (exename);
					var app = new IdeStartup ();
					ret = app.Run (options);
					break;
				} catch (Exception ex) {
					if (!retry && AddinManager.IsInitialized) {
						LoggingService.LogWarning (BrandingService.ApplicationName + " failed to start. Rebuilding addins registry.", ex);
						AddinManager.Registry.Rebuild (new Mono.Addins.ConsoleProgressStatus (true));
						LoggingService.LogInfo ("Addin registry rebuilt. Restarting {0}.", BrandingService.ApplicationName);
						retry = true;
					} else {
						LoggingService.LogFatalError (
							string.Format (
								"{0} failed to start. Some of the assemblies required to run {0} (for example gtk-sharp)" +
								"may not be properly installed in the GAC.",
								BrandingService.ApplicationName
							), ex);
						retry = false;
					}
				} finally {
					Runtime.Shutdown ();
				}
			}
			while (retry);

			LoggingService.Shutdown ();

			return ret;
		}
	}
	
	public class MonoDevelopOptions
	{
		MonoDevelopOptions ()
		{
			IpcTcp = (PlatformID.Unix != Environment.OSVersion.Platform);
			RedirectOutput = true;
		}
		
		Mono.Options.OptionSet GetOptionSet ()
		{
			return new Mono.Options.OptionSet () {
				{ "no-splash", "Do not display splash screen.", s => NoSplash = true },
				{ "ipc-tcp", "Use the Tcp channel for inter-process comunication.", s => IpcTcp = true },
				{ "new-window", "Do not open in an existing instance of " + BrandingService.ApplicationName, s => NewWindow = true },
				{ "h|?|help", "Show help", s => ShowHelp = true },
				{ "perf-log", "Enable performance counter logging", s => PerfLog = true },
				{ "no-redirect", "Disable redirection of stdout/stderr to a log file", s => RedirectOutput = false },
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
		
		public bool NoSplash { get; set; }
		public bool IpcTcp { get; set; }
		public bool NewWindow { get; set; }
		public bool ShowHelp { get; set; }
		public bool PerfLog { get; set; }
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
