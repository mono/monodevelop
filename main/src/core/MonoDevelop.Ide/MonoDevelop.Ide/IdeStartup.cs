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
using MonoDevelop.Core.Instrumentation;
using System.Diagnostics;
using System.Collections.Generic;
using MonoDevelop.Ide.Extensions;

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
			LoggingService.LogInfo ("Starting {0} {1}", BrandingService.ApplicationName, IdeVersionInfo.MonoDevelopVersion);
			LoggingService.LogInfo ("Running on {0}", IdeVersionInfo.GetRuntimeInfo ());

			Counters.Initialization.BeginTiming ();

			if (options.PerfLog) {
				string logFile = Path.Combine (Environment.CurrentDirectory, "monodevelop.perf-log");
				LoggingService.LogInfo ("Logging instrumentation service data to file: " + logFile);
				InstrumentationService.StartAutoSave (logFile, 1000);
			}

			//ensure native libs initialized before we hit anything that p/invokes
			Platform.Initialize ();
			
			Counters.Initialization.Trace ("Initializing GTK");
			if (Platform.IsWindows && !CheckWindowsGtk ())
				return 1;
			SetupExceptionManager ();
			
			try {
				GLibLogging.Enabled = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error initialising GLib logging.", ex);
			}

			SetupTheme ();

			var args = options.RemainingArgs.ToArray ();
			Gtk.Application.Init (BrandingService.ApplicationName, ref args);

			LoggingService.LogInfo ("Using GTK+ {0}", IdeVersionInfo.GetGtkVersion ());

			FilePath p = typeof(IdeStartup).Assembly.Location;
			Assembly.LoadFrom (p.ParentDirectory.Combine ("Xwt.Gtk.dll"));
			Xwt.Application.InitializeAsGuest (Xwt.ToolkitType.Gtk);

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
			Runtime.MainSynchronizationContext = SynchronizationContext.Current;
			
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


			IdeApp.Customizer = options.IdeCustomizer ?? new IdeCustomizer ();

			Counters.Initialization.Trace ("Initializing theme and splash window");

			DefaultTheme = Gtk.Settings.Default.ThemeName;
			string theme = IdeApp.Preferences.UserInterfaceTheme;
			if (string.IsNullOrEmpty (theme))
				theme = DefaultTheme;
			ValidateGtkTheme (ref theme);
			if (theme != DefaultTheme)
				Gtk.Settings.Default.ThemeName = theme;

			
			//don't show the splash screen on the Mac, so instead we get the expected "Dock bounce" effect
			//this also enables the Mac platform service to subscribe to open document events before the GUI loop starts.
			if (Platform.IsMac)
				options.NoSplash = true;
			
			IProgressMonitor monitor = null;
			if (!options.NoSplash) {
				try {
					monitor = new SplashScreenForm ();
					((SplashScreenForm)monitor).ShowAll ();
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to create splash screen", ex);
				}
			}
			if (monitor == null) {
				monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
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

			CheckFileWatcher ();
			
			Exception error = null;
			int reportedFailures = 0;
			
			try {
				Counters.Initialization.Trace ("Loading Icons");
				//force initialisation before the workbench so that it can register stock icons for GTK before they get requested
				ImageService.Initialize ();
				
				if (errorsList.Count > 0) {
					if (monitor is SplashScreenForm)
						((SplashScreenForm)monitor).Hide ();
					AddinLoadErrorDialog dlg = new AddinLoadErrorDialog ((AddinError[]) errorsList.ToArray (typeof(AddinError)), false);
					if (!dlg.Run ())
						return 1;
					if (monitor is SplashScreenForm)
						((SplashScreenForm)monitor).Show ();
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
				string message = BrandingService.BrandApplicationName (GettextCatalog.GetString (
					"MonoDevelop failed to start. The following error has been reported: {0}",
					error.Message
				));
				MessageService.ShowException (error, message);
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
			AddinManager.AddinLoadError -= OnAddinError;
			
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

		[System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
		[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
		static extern bool SetDllDirectory (string lpPathName);

		static bool CheckWindowsGtk ()
		{
			string location = null;
			Version version = null;
			Version minVersion = new Version (2, 12, 22);

			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Xamarin\GtkSharp\InstallFolder")) {
				if (key != null)
					location = key.GetValue (null) as string;
			}
			using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Xamarin\GtkSharp\Version")) {
				if (key != null)
					Version.TryParse (key.GetValue (null) as string, out version);
			}

			//TODO: check build version of GTK# dlls in GAC
			if (version == null || version < minVersion || location == null || !File.Exists (Path.Combine (location, "bin", "libgtk-win32-2.0-0.dll"))) {
				LoggingService.LogError ("Did not find required GTK# installation");
				string url = "http://monodevelop.com/Download";
				string caption = "Fatal Error";
				string message =
					"{0} did not find the required version of GTK#. Please click OK to open the download page, where " +
					"you can download and install the latest version.";
				if (DisplayWindowsOkCancelMessage (
					string.Format (message, BrandingService.ApplicationName, url), caption)
				) {
					Process.Start (url);
				}
				return false;
			}

			LoggingService.LogInfo ("Found GTK# version " + version);

			var path = Path.Combine (location, @"bin");
			try {
				if (SetDllDirectory (path)) {
					return true;
				}
			} catch (EntryPointNotFoundException) {
			}
			// this shouldn't happen unless something is weird in Windows
			LoggingService.LogError ("Unable to set GTK+ dll directory");
			return true;
		}

		static bool DisplayWindowsOkCancelMessage (string message, string caption)
		{
			var name = typeof(int).Assembly.FullName.Replace ("mscorlib", "System.Windows.Forms");
			var asm = Assembly.Load (name);
			var md = asm.GetType ("System.Windows.Forms.MessageBox");
			var mbb = asm.GetType ("System.Windows.Forms.MessageBoxButtons");
			var okCancel = Enum.ToObject (mbb, 1);
			var dr = asm.GetType ("System.Windows.Forms.DialogResult");
			var ok = Enum.ToObject (dr, 1);

			const BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static;
			return md.InvokeMember ("Show", flags, null, null, new object[] { message, caption, okCancel }).Equals (ok);
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

		internal static string[] gtkThemeFallbacks = new string[] {
			"Xamarin",// the best!
			"Gilouche", // SUSE
			"Mint-X", // MINT
			"Radiance", // Ubuntu 'light' theme (MD looks better with the light theme in 4.0 - if that changes switch this one)
			"Clearlooks" // GTK theme
		};

		static void ValidateGtkTheme (ref string theme)
		{
			if (!MonoDevelop.Ide.Gui.OptionPanels.IDEStyleOptionsPanelWidget.IsBadGtkTheme (theme))
				return;

			var themes = MonoDevelop.Ide.Gui.OptionPanels.IDEStyleOptionsPanelWidget.InstalledThemes;

			string fallback = gtkThemeFallbacks
				.Select (fb => themes.FirstOrDefault (t => string.Compare (fb, t, StringComparison.OrdinalIgnoreCase) == 0))
				.FirstOrDefault (t => t != null);

			string message = "Theme Not Supported";

			string detail;
			if (themes.Count > 0) {
				detail =
					"Your system is using the '{0}' GTK+ theme, which is known to be very unstable. MonoDevelop will " +
					"now switch to an alternate GTK+ theme.\n\n" +
					"This message will continue to be shown at startup until you set a alternate GTK+ theme as your " +
					"default in the GTK+ Theme Selector or MonoDevelop Preferences.";
			} else {
				detail =
					"Your system is using the '{0}' GTK+ theme, which is known to be very unstable, and no other GTK+ " +
					"themes appear to be installed. Please install another GTK+ theme.\n\n" +
					"This message will continue to be shown at startup until you install a different GTK+ theme and " +
					"set it as your default in the GTK+ Theme Selector or MonoDevelop Preferences.";
			}

			MessageService.GenericAlert (Gtk.Stock.DialogWarning, message, BrandingService.BrandApplicationName (detail), AlertButton.Ok);

			theme = fallback ?? themes.FirstOrDefault () ?? theme;
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

		static void SetupExceptionManager ()
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
		
		static void HandleException (Exception ex, bool willShutdown)
		{
			var msg = String.Format ("An unhandled exception has occured. Terminating MonoDevelop? {0}", willShutdown);

			if (willShutdown)
				LoggingService.LogFatalError (msg, ex);
			else
				LoggingService.LogCriticalError (msg, ex);
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
		
		public static int Main (string[] args, IdeCustomizer customizer = null)
		{
			var options = MonoDevelopOptions.Parse (args);
			if (options.ShowHelp || options.Error != null)
				return options.Error != null? -1 : 0;
			
			LoggingService.Initialize (options.RedirectOutput);

			options.IdeCustomizer = customizer;

			int ret = -1;
			bool retry = false;
			do {
				try {
					var exename = Path.GetFileNameWithoutExtension (Assembly.GetEntryAssembly ().Location);
					if (!Platform.IsMac && !Platform.IsWindows)
						exename = exename.ToLower ();
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
				Console.WriteLine (BrandingService.ApplicationName + " " + BuildInfo.VersionLabel);
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
		public IdeCustomizer IdeCustomizer { get; set; }
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
