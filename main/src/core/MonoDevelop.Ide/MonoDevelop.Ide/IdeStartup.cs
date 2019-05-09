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
using System.Reflection;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Instrumentation;
using System.Diagnostics;
using System.Collections.Generic;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Ide.Desktop;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Shell;
using MonoDevelop.Ide.Composition;

namespace MonoDevelop.Ide
{
	public class IdeStartup: IApplication
	{
		static IdeInstanceConnection instanceConnection;

		List<AddinError> errorsList = new List<AddinError> ();
		bool initialized;
		static bool hideWelcomePage;
		static StartupInfo startupInfo;

		static TimeToCodeMetadata ttcMetadata;

		Task<int> IApplication.Run (string[] args)
		{
			var options = MonoDevelopOptions.Parse (args);
			if (options.Error != null || options.ShowHelp)
				return Task.FromResult (options.Error != null? -1 : 0);
			return Task.FromResult (Run (options));
		}

		int Run (MonoDevelopOptions options)
		{
			LoggingService.LogInfo ("Starting {0} {1}", BrandingService.ApplicationLongName, IdeVersionInfo.MonoDevelopVersion);
			LoggingService.LogInfo ("Build Information{0}{1}", Environment.NewLine, SystemInformation.GetBuildInformation ());
			LoggingService.LogInfo ("Running on {0}", RuntimeVersionInfo.GetRuntimeInfo ());

			//ensure native libs initialized before we hit anything that p/invokes
			Platform.Initialize ();
			IdeStartupTracker.StartupTracker.MarkSection ("PlatformInitialization");

			GettextCatalog.Initialize ();
			IdeStartupTracker.StartupTracker.MarkSection ("GettextInitialization");

			LoggingService.LogInfo ("Operating System: {0}", SystemInformation.GetOperatingSystemDescription ());

			// The assembly resolver for MSBuild 15 assemblies needs to be defined early on.
			// Whilst Runtime.Initialize loads the MSBuild 15 assemblies from Mono this seems
			// to be too late to prevent the MEF composition and the static registrar from
			// failing to load the MonoDevelop.Ide assembly which now uses MSBuild 15 assemblies.
			ResolveMSBuildAssemblies ();

			Counters.Initialization.BeginTiming ();

			if (options.PerfLog) {
				string logFile = Path.Combine (Environment.CurrentDirectory, "monodevelop.perf-log");
				LoggingService.LogInfo ("Logging instrumentation service data to file: " + logFile);
				InstrumentationService.StartAutoSave (logFile, 1000);
			}

			Counters.Initialization.Trace ("Initializing GTK");
			if (Platform.IsWindows && !CheckWindowsGtk ())
				return 1;
			SetupExceptionManager ();

			// explicit GLib type system initialization for GLib < 2.36 before any other type system access
			GLib.GType.Init ();

			var args = options.RemainingArgs.ToArray ();

			IdeTheme.InitializeGtk (BrandingService.ApplicationName, ref args);

			startupInfo = new StartupInfo (options, args);

			IdeApp.Customizer = options.IdeCustomizer ?? new IdeCustomizer ();
			try {
				IdeApp.Customizer.Initialize (startupInfo);
			} catch (UnauthorizedAccessException ua) {
				LoggingService.LogError ("Unauthorized access: " + ua.Message);
				return 1;
			}

			try {
				GLibLogging.Enabled = true;
			} catch (Exception ex) {
				LoggingService.LogError ("Error initialising GLib logging.", ex);
			}

			IdeStartupTracker.StartupTracker.MarkSection ("GtkInitialization");
			LoggingService.LogInfo ("Using GTK+ {0}", IdeVersionInfo.GetGtkVersion ());

			// XWT initialization
			FilePath p = typeof (IdeStartup).Assembly.Location;
			Runtime.LoadAssemblyFrom (p.ParentDirectory.Combine ("Xwt.Gtk.dll"));
			Xwt.Application.InitializeAsGuest (Xwt.ToolkitType.Gtk);
			Xwt.Toolkit.CurrentEngine.RegisterBackend<IExtendedTitleBarWindowBackend, GtkExtendedTitleBarWindowBackend> ();
			Xwt.Toolkit.CurrentEngine.RegisterBackend<IExtendedTitleBarDialogBackend, GtkExtendedTitleBarDialogBackend> ();
			IdeTheme.SetupXwtTheme ();

			IdeStartupTracker.StartupTracker.MarkSection ("XwtInitialization");

			//default to Windows IME on Windows
			if (Platform.IsWindows && GtkWorkarounds.GtkMinorVersion >= 16) {
				var settings = Gtk.Settings.Default;
				var val = GtkWorkarounds.GetProperty (settings, "gtk-im-module");
				if (string.IsNullOrEmpty (val.Val as string))
					GtkWorkarounds.SetProperty (settings, "gtk-im-module", new GLib.Value ("ime"));
			}

			DispatchService.Initialize ();

			// Set a synchronization context for the main gtk thread
			SynchronizationContext.SetSynchronizationContext (DispatchService.SynchronizationContext);
			Runtime.MainSynchronizationContext = SynchronizationContext.Current;

			IdeStartupTracker.StartupTracker.MarkSection ("DispatchInitialization");

			// Initialize Roslyn's synchronization context
			RoslynServices.RoslynService.Initialize ();

			IdeStartupTracker.StartupTracker.MarkSection ("RoslynInitialization");

			AddinManager.AddinLoadError += OnAddinError;

			Counters.Initialization.Trace ("Initializing Runtime");
			Runtime.Initialize (true);

			// Register services used by the IDE

			RegisterServices ();

			// If a combine was specified, force --newwindow.

			if (!options.NewWindow && startupInfo.HasFiles) {
				foreach (var file in startupInfo.RequestedFileList) {
					if (MonoDevelop.Projects.Services.ProjectService.IsWorkspaceItemFile (file.FileName)) {
						options.NewWindow = true;
						break;
					}
				}
			}

			instanceConnection = new IdeInstanceConnection ();
			instanceConnection.Initialize (options.IpcTcp);

			// If not opening a combine, connect to existing monodevelop and pass filename(s) and exit
			if (!options.NewWindow && startupInfo.HasFiles && instanceConnection.TryConnect (startupInfo))
				return 0;

			IdeStartupTracker.StartupTracker.MarkSection ("RuntimeInitialization");

			bool restartRequested = PropertyService.Get ("MonoDevelop.Core.RestartRequested", false);
			startupInfo.Restarted = restartRequested;
			PropertyService.Set ("MonoDevelop.Core.RestartRequested", false);

			Counters.Initialization.Trace ("Initializing theme");

			IdeTheme.SetupGtkTheme ();

			IdeApp.Customizer.OnCoreInitialized ();

			IdeStartupTracker.StartupTracker.MarkSection ("ThemeInitialized");

			IdeApp.IsRunning = true;

			// Run the main loop
			Gtk.Application.Invoke ((s, e) => {
				MainLoop (options, startupInfo).Ignore ();
			});
			Gtk.Application.Run ();

			IdeApp.IsRunning = false;

			IdeApp.Customizer.OnIdeShutdown ();

			instanceConnection.Dispose ();

			lockupCheckRunning = false;
			Runtime.Shutdown ();

			IdeApp.Customizer.OnCoreShutdown ();

			InstrumentationService.Stop ();

			MonoDevelop.Components.GtkWorkarounds.Terminate ();

			return 0;
		}

		async Task<int> MainLoop (MonoDevelopOptions options, StartupInfo startupInfo)
		{
			ProgressMonitor monitor = new MonoDevelop.Core.ProgressMonitoring.ConsoleProgressMonitor ();
			
			monitor.BeginTask (GettextCatalog.GetString ("Starting {0}", BrandingService.ApplicationName), 2);

			//make sure that the platform service is initialised so that the Mac platform can subscribe to open-document events
			Counters.Initialization.Trace ("Initializing Platform Service");
			await Runtime.GetService<DesktopService> ();

			IdeStartupTracker.StartupTracker.MarkSection ("PlatformInitialization");

			monitor.Step (1);

			Counters.Initialization.Trace ("Checking System");

			CheckFileWatcher ();

			IdeStartupTracker.StartupTracker.MarkSection ("FileWatcherInitialization");

			Exception error = null;
			int reportedFailures = 0;

			try {
				Counters.Initialization.Trace ("Loading Icons");
				//force initialisation before the workbench so that it can register stock icons for GTK before they get requested
				ImageService.Initialize ();

				IdeStartupTracker.StartupTracker.MarkSection ("ImageInitialization");

				// If we display an error dialog before the main workbench window on OS X then a second application menu is created
				// which is then replaced with a second empty Apple menu.
				// XBC #33699
				Counters.Initialization.Trace ("Initializing IdeApp");

				hideWelcomePage = options.NoStartWindow || startupInfo.HasFiles || IdeApp.Preferences.StartupBehaviour.Value != OnStartupBehaviour.ShowStartWindow;
				await IdeApp.Initialize (monitor);

				IdeStartupTracker.StartupTracker.MarkSection ("AppInitialization");

				if (errorsList.Count > 0) {
					using (AddinLoadErrorDialog dlg = new AddinLoadErrorDialog (errorsList.ToArray (), false)) {
						if (!dlg.Run ())
							return 1;
					}
					reportedFailures = errorsList.Count;
				}

				if (!CheckSCPlugin ())
					return 1;

				// Load requested files
				Counters.Initialization.Trace ("Opening Files");

				// load previous combine
				RecentFile openedProject = null;
				if (IdeApp.Preferences.StartupBehaviour.Value == OnStartupBehaviour.LoadPreviousSolution && !startupInfo.HasSolutionFile && !IdeApp.Workspace.WorkspaceItemIsOpening && !IdeApp.Workspace.IsOpen) {
					openedProject = IdeServices.DesktopService.RecentFiles.MostRecentlyUsedProject;
					if (openedProject != null) {
						var metadata = GetOpenWorkspaceOnStartupMetadata ();
						IdeApp.Workspace.OpenWorkspaceItem (openedProject.FileName, true, true, metadata).ContinueWith (t => IdeApp.OpenFiles (startupInfo.RequestedFileList, metadata), TaskScheduler.FromCurrentSynchronizationContext ()).Ignore();
						startupInfo.OpenedRecentProject = true;
					}
				}
				if (openedProject == null) {
					IdeApp.OpenFiles (startupInfo.RequestedFileList, GetOpenWorkspaceOnStartupMetadata ());
					startupInfo.OpenedFiles = startupInfo.HasFiles;
				}
				
				monitor.Step (1);
			
			} catch (Exception e) {
				error = e;
			} finally {
				monitor.Dispose ();
			}
			
			if (error != null) {
				string message = BrandingService.BrandApplicationName (GettextCatalog.GetString ("MonoDevelop failed to start"));
				message = message + "\n\n" + error.Message;
				MessageService.ShowFatalError (message, null, error);

				return 1;
			}

			if (errorsList.Count > reportedFailures) {
				using (AddinLoadErrorDialog dlg = new AddinLoadErrorDialog (errorsList.ToArray (), true))
					dlg.Run ();
			}
			
			errorsList = null;
			AddinManager.AddinLoadError -= OnAddinError;

			IdeStartupTracker.StartupTracker.MarkSection ("BasicInitializationCompleted");

			instanceConnection.FileOpenRequested += (sender, a) => {
				foreach (var e in a)
					OpenFile (e.FileName);
			};

			instanceConnection.StartListening ();

			IdeStartupTracker.StartupTracker.MarkSection ("SocketInitialization");

			initialized = true;
			MessageService.RootWindow = IdeApp.Workbench.RootWindow;
			Xwt.MessageDialog.RootWindow = Xwt.Toolkit.CurrentEngine.WrapWindow (IdeApp.Workbench.RootWindow);

			IdeStartupTracker.StartupTracker.MarkSection ("WindowOpened");

			Thread.CurrentThread.Name = "GUI Thread";
			Counters.Initialization.Trace ("Running IdeApp");
			Counters.Initialization.EndTiming ();
				
			AddinManager.AddExtensionNodeHandler("/MonoDevelop/Ide/InitCompleteHandlers", OnExtensionChanged);
			StartLockupTracker ();

			// This call is important so the current event loop is run before we run the main loop.
			// On Mac, the OpenDocuments event gets handled here, so we need to get the timeout
			// it queues before the OnIdle event so we can start opening a solution before
			// we show the main window.
			await Task.Yield ();

			IdeStartupTracker.StartupTracker.MarkSection ("PumpEventLoop");
			IdeStartupTracker.StartupTracker.Stop (startupInfo);

			GLib.Idle.Add (OnIdle);

			return 0;
		}

		void RegisterServices ()
		{
			Runtime.RegisterServiceType<ProgressMonitorManager, IdeProgressMonitorManager> ();
			Runtime.RegisterServiceType<CommandManager, IdeCommandManager> ();
			Runtime.RegisterServiceType<IShell, DefaultWorkbench> ();
		}

		/// <summary>
		/// Resolves MSBuild assemblies that are used by MonoDevelop.Ide and are included with Mono.
		/// </summary>
		void ResolveMSBuildAssemblies ()
		{
			if (Platform.IsWindows)
				return;

			var currentRuntime = MonoRuntimeInfo.FromCurrentRuntime ();
			if (currentRuntime != null) {
				var rootMSBuildBinDir = Path.Combine (currentRuntime.Prefix, "lib", "mono", "msbuild");
				msbuildBinDir = Path.Combine (rootMSBuildBinDir, "Current", "bin");
				if (Directory.Exists (msbuildBinDir)) {
					AppDomain.CurrentDomain.AssemblyResolve += MSBuildAssemblyResolve;
					return;
				}
				// Fallback to MSBuild 15.0
				msbuildBinDir = Path.Combine (rootMSBuildBinDir, "15.0", "bin");
				if (Directory.Exists (msbuildBinDir)) {
					AppDomain.CurrentDomain.AssemblyResolve += MSBuildAssemblyResolve;
				}
			}
		}

		string msbuildBinDir;

		string[] msbuildAssemblies = new string [] {
			"Microsoft.Build",
			"Microsoft.Build.Engine",
			"Microsoft.Build.Framework",
			"Microsoft.Build.Tasks.Core",
			"Microsoft.Build.Utilities.Core"
		};

		Assembly MSBuildAssemblyResolve (object sender, ResolveEventArgs args)
		{
			var asmName = new AssemblyName (args.Name);
			if (!msbuildAssemblies.Any (msbuildAssembly => StringComparer.OrdinalIgnoreCase.Equals (msbuildAssembly, asmName.Name)))
				return null;

			string fullPath = Path.Combine (msbuildBinDir, asmName.Name + ".dll");
			if (File.Exists (fullPath)) {
				return Assembly.LoadFrom (fullPath);
			}

			return null;
		}

		static bool OnIdle ()
		{
			// Make sure the composition manager started initializing
			Runtime.GetService<CompositionManager> ();

			// OpenDocuments appears when the app is idle.
			if (!hideWelcomePage && !WelcomePage.WelcomePageService.HasWindowImplementation) {
				WelcomePage.WelcomePageService.ShowWelcomePage ();
				Counters.Initialization.Trace ("Showed welcome page");
				IdeApp.Workbench.Show ();
			} else if (hideWelcomePage && !startupInfo.OpenedFiles) {
				IdeApp.Workbench.Show ();
			}

			return false;
		}

		static DateTime lastIdle;
		static bool lockupCheckRunning = true;

		[Conditional("DEBUG")]
		static void StartLockupTracker ()
		{
			if (Platform.IsWindows)
				return;
			if (!string.Equals (Environment.GetEnvironmentVariable ("MD_LOCKUP_TRACKER"), "ON", StringComparison.OrdinalIgnoreCase))
				return;
			GLib.Timeout.Add (2000, () => {
				lastIdle = DateTime.Now;
				return true;
			});
			lastIdle = DateTime.Now;
			var lockupCheckThread = new Thread (delegate () {
				while (lockupCheckRunning) {
					const int waitTimeout = 5000;
					const int maxResponseTime = 10000;
					Thread.Sleep (waitTimeout); 
					if ((DateTime.Now - lastIdle).TotalMilliseconds > maxResponseTime) {
						var pid = Process.GetCurrentProcess ().Id;
						Mono.Unix.Native.Syscall.kill (pid, Mono.Unix.Native.Signum.SIGQUIT); 
						return;
					}
				}
			});
			lockupCheckThread.Name = "Lockup check";
			lockupCheckThread.IsBackground = true;
			lockupCheckThread.Start (); 
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
				// Run handlers in different UI loops to avoid freezing the UI for too much time
				Xwt.Application.Invoke (() => {
					try {
						if (args.ExtensionObject is CommandHandler handler) {
							handler.InternalRun ();
						} else {
							LoggingService.LogError ("Type " + args.ExtensionObject.GetType () + " must be a subclass of MonoDevelop.Components.Commands.CommandHandler");
						}
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				});
			}
		}
		
		void OnAddinError (object s, AddinErrorEventArgs args)
		{
			if (errorsList != null)
				errorsList.Add (new AddinError (args.AddinId, args.Message, args.Exception, false));
		}

		static bool OpenFile (string file) 
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
					IdeApp.Workbench.OpenDocument (file, null, line, column, OpenDocumentOptions.DefaultInternal);
				}
			} catch {
			}
			IdeApp.Workbench.Present ();
			return false;
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

		bool CheckSCPlugin ()
		{
			if (Platform.IsMac && Directory.Exists ("/Library/Contextual Menu Items/SCFinderPlugin.plugin")) {
				string message = "SCPlugin not supported";
				string detail = "MonoDevelop has detected that SCPlugin (scplugin.tigris.org) is installed. " +
				                "SCPlugin is a Subversion extension for Finder that is known to cause crashes in MonoDevelop and" +
				                "other applications running on Mac OSX 10.9 (Mavericks) or upper. Please uninstall SCPlugin " +
				                "before proceeding.";
				var close = new AlertButton (BrandingService.BrandApplicationName (GettextCatalog.GetString ("Close MonoDevelop")));
				var info = new AlertButton (GettextCatalog.GetString ("More Information"));
				var cont = new AlertButton (GettextCatalog.GetString ("Continue Anyway"));
				while (true) {
					var res = MessageService.GenericAlert (Gtk.Stock.DialogWarning, message, BrandingService.BrandApplicationName (detail), info, cont, close);
					if (res == close) {
						LoggingService.LogInternalError ("SCPlugin detected", new Exception ("SCPlugin detected. Closing."));
						return false;
					}
					if (res == info)
						IdeServices.DesktopService.ShowUrl ("https://bugzilla.xamarin.com/show_bug.cgi?id=21755");
					if (res == cont) {
						bool exists = Directory.Exists ("/Library/Contextual Menu Items/SCFinderPlugin.plugin");
						LoggingService.LogInternalError ("SCPlugin detected", new Exception ("SCPlugin detected. Continuing " + (exists ? "Installed." : "Uninstalled.")));
						return true;
					}
				}
			}
			return true;
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
			System.Windows.Threading.Dispatcher.CurrentDispatcher.UnhandledException += (sender, e) => {
				HandleException (e.Exception, false);
				e.Handled = true;
			};
		}

		static void HandleException (Exception ex, bool willShutdown)
		{
			var msg = String.Format ("An unhandled exception has occurred. Terminating {0}? {1}", BrandingService.ApplicationName, willShutdown);
			var aggregateException = ex as AggregateException;
			if (aggregateException != null) {
				aggregateException.Flatten ().Handle (innerEx => {
					HandleException (innerEx, willShutdown);
					return true;
				});
				return;
			}

			if (willShutdown) {
				var metadata = new UnhandledExceptionMetadata {
					Exception = ex
				};
				LoggingService.LogFatalError (msg, ex);
				Counters.UnhandledExceptions.Inc (metadata);
			} else {
				LoggingService.LogInternalError (msg, ex);
			}
		}
		
		public static int Main (string[] args, IdeCustomizer customizer = null)
		{

			IdeStartupTracker.StartupTracker.Start ();

			var options = MonoDevelopOptions.Parse (args);
			if (options.ShowHelp || options.Error != null)
				return options.Error != null? -1 : 0;
			
			LoggingService.Initialize (options.RedirectOutput);

			if (customizer == null)
				customizer = LoadBrandingCustomizer ();
			options.IdeCustomizer = customizer;

			if (!Platform.IsWindows) {
				// Limit maximum threads when running on mono
				int threadCount = 125;
				ThreadPool.SetMaxThreads (threadCount, threadCount);
			}

			int ret = -1;
			try {
				var exename = Path.GetFileNameWithoutExtension (Assembly.GetEntryAssembly ().Location);
				if (!Platform.IsMac && !Platform.IsWindows)
					exename = exename.ToLower ();
				Runtime.SetProcessName (exename);

				IdeStartupTracker.StartupTracker.MarkSection ("mainInitialization");

				var app = new IdeStartup ();
				ret = app.Run (options);
			} catch (Exception ex) {
				LoggingService.LogFatalError (
					string.Format (
						"{0} failed to start. Some of the assemblies required to run {0} (for example gtk-sharp)" +
						"may not be properly installed in the GAC.",
						BrandingService.ApplicationName
					), ex);
			} finally {
				Runtime.Shutdown ();
			}

			LoggingService.Shutdown ();

			return ret;
		}

		static IdeCustomizer LoadBrandingCustomizer ()
		{
			var pathsString = BrandingService.GetString ("CustomizerAssemblyPath");
			if (string.IsNullOrEmpty (pathsString))
				return null;

			var paths = pathsString.Split (new [] {';'}, StringSplitOptions.RemoveEmptyEntries);
			var type = BrandingService.GetString ("CustomizerType");
			if (!string.IsNullOrEmpty (type)) {
				foreach (var path in paths) {
					var file = BrandingService.GetFile (path.Replace ('/',Path.DirectorySeparatorChar));
					if (File.Exists (file)) {
						Assembly asm = Runtime.LoadAssemblyFrom (file);
						var t = asm.GetType (type, true);
						var c = Activator.CreateInstance (t) as IdeCustomizer;
						if (c == null)
							throw new InvalidOperationException ("Customizer class specific in the branding file is not an IdeCustomizer subclass");
						return c;
					}
				}
			}
			return null;
		}

		internal static OpenWorkspaceItemMetadata GetOpenWorkspaceOnStartupMetadata ()
		{
			var metadata = new OpenWorkspaceItemMetadata {
				OnStartup = true
			};
			return metadata;
		}
	}
}
