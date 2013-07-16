//
// IdeApp.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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


using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using Mono.Addins;
using MonoDevelop.Components.Commands;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.CustomTools;
using System.Linq;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Desktop;
using System.Collections.Generic;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide
{
	public static class IdeApp
	{
		static bool isInitialized;
		static Workbench workbench;
		static ProjectOperations projectOperations;
		static HelpOperations helpOperations;
		static CommandManager commandService;
		static IdeServices ideServices;
		static RootWorkspace workspace;
		static IdePreferences preferences;
		static Version version;

		public const int CurrentRevision = 5;
		
		static bool isInitialRun;
		static bool isInitialRunAfterUpgrade;
		static int upgradedFromRevision;
		
		public static event ExitEventHandler Exiting;
		public static event EventHandler Exited;
		
		static EventHandler initializedEvent;
		public static event EventHandler Initialized {
			add {
				if (isInitialized) value (null, EventArgs.Empty);
				else initializedEvent += value;
			}
			remove { 
				initializedEvent -= value;
			}
		}
		
		/// <summary>
		/// Fired when the IDE gets the focus
		/// </summary>
		public static event EventHandler FocusIn {
			add { CommandService.ApplicationFocusIn += value; }
			remove { CommandService.ApplicationFocusIn -= value; }
		}
		
		/// <summary>
		/// Fired when the IDE loses the focus
		/// </summary>
		public static event EventHandler FocusOut {
			add { CommandService.ApplicationFocusOut += value; }
			remove { CommandService.ApplicationFocusOut -= value; }
		}

		/// <summary>
		/// Gets a value indicating whether the IDE has the input focus
		/// </summary>
		public static bool HasInputFocus {
			get { return CommandService.ApplicationHasFocus; }
		}

		static IdeApp ()
		{
			preferences = new IdePreferences ();
		}
		
		public static Workbench Workbench {
			get { return workbench; }
		}
		
		public static ProjectOperations ProjectOperations {
			get { return projectOperations; }
		}
		
		public static RootWorkspace Workspace {
			get { return workspace; }
		}
		
		public static HelpOperations HelpOperations {
			get { return helpOperations; }
		}
		
		public static CommandManager CommandService {
			get { return commandService; }
		}
		
		public static IdeServices Services {
			get { return ideServices; }
		}

		public static IdePreferences Preferences {
			get { return preferences; }
		}

		public static bool IsInitialized {
			get {
				return isInitialized;
			}
		}

		// Returns true if MD is running for the first time after installing
		public static bool IsInitialRun {
			get { return isInitialRun; }
		}
		
		// Returns true if MD is running for the first time after being upgraded from a previous version
		public static bool IsInitialRunAfterUpgrade {
			get { return isInitialRunAfterUpgrade; }
		}
		
		// If IsInitialRunAfterUpgrade is true, returns the previous version
		public static int UpgradedFromRevision {
			get { return upgradedFromRevision; }
		}
		
		public static Version Version {
			get {
				if (version == null) {
					version = new Version (BuildVariables.PackageVersion);
					var relId = SystemInformation.GetReleaseId ();
					if (relId != null && relId.Length >= 9) {
						int rev;
						int.TryParse (relId.Substring (relId.Length - 4), out rev);
						version = new Version (Math.Max (version.Major, 0), Math.Max (version.Minor, 0), Math.Max (version.Build, 0), Math.Max (rev, 0));
					}
				}
				return version;
			}
		}
		
		public static void Initialize (IProgressMonitor monitor)
		{
			Counters.Initialization.Trace ("Creating Workbench");
			workbench = new Workbench ();
			Counters.Initialization.Trace ("Creating Root Workspace");
			workspace = new RootWorkspace ();
			Counters.Initialization.Trace ("Creating Services");
			projectOperations = new ProjectOperations ();
			helpOperations = new HelpOperations ();
			commandService = new CommandManager ();
			ideServices = new IdeServices ();
			CustomToolService.Init ();
			AutoTestService.Start (commandService, Preferences.EnableAutomatedTesting);
			
			commandService.CommandTargetScanStarted += CommandServiceCommandTargetScanStarted;
			commandService.CommandTargetScanFinished += CommandServiceCommandTargetScanFinished;
			commandService.KeyBindingFailed += KeyBindingFailed;

			KeyBindingService.LoadBindingsFromExtensionPath ("/MonoDevelop/Ide/KeyBindingSchemes");
			KeyBindingService.LoadCurrentBindings ("MD2");

			commandService.CommandError += delegate (object sender, CommandErrorArgs args) {
				LoggingService.LogError (args.ErrorMessage, args.Exception);
				MessageService.ShowException (args.Exception, args.ErrorMessage);
			};
			
			FileService.ErrorHandler = FileServiceErrorHandler;
		
			monitor.BeginTask (GettextCatalog.GetString("Loading Workbench"), 5);
			Counters.Initialization.Trace ("Loading Commands");
			
			commandService.LoadCommands ("/MonoDevelop/Ide/Commands");
			monitor.Step (1);

			Counters.Initialization.Trace ("Initializing Workbench");
			workbench.Initialize (monitor);
			monitor.Step (1);
			
			InternalLog.EnableErrorNotification ();

			MonoDevelop.Ide.WelcomePage.WelcomePageService.Initialize ();
			MonoDevelop.Ide.WelcomePage.WelcomePageService.ShowWelcomePage ();

			monitor.Step (1);

			Counters.Initialization.Trace ("Restoring Workbench State");
			workbench.Show ("SharpDevelop.Workbench.WorkbenchMemento");
			monitor.Step (1);
			
			Counters.Initialization.Trace ("Flushing GUI events");
			DispatchService.RunPendingEvents ();
			Counters.Initialization.Trace ("Flushed GUI events");
			
			MessageService.RootWindow = workbench.RootWindow;
		
			commandService.EnableIdleUpdate = true;
			
			// Default file format
			MonoDevelop.Projects.Services.ProjectServiceLoaded += delegate(object sender, EventArgs e) {
				((ProjectService)sender).DefaultFileFormatId = IdeApp.Preferences.DefaultProjectFileFormat;
			};
			
			IdeApp.Preferences.DefaultProjectFileFormatChanged += delegate {
				IdeApp.Services.ProjectService.DefaultFileFormatId = IdeApp.Preferences.DefaultProjectFileFormat;
			};

			// Perser service initialization
			TypeSystemService.TrackFileChanges = true;
			TypeSystemService.ParseProgressMonitorFactory = new ParseProgressMonitorFactory (); 

			
			// Startup commands
			Counters.Initialization.Trace ("Running Startup Commands");
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/StartupHandlers", OnExtensionChanged);
			monitor.Step (1);
			monitor.EndTask ();

			// Set initial run flags
			Counters.Initialization.Trace ("Upgrading Settings");

			if (PropertyService.Get("MonoDevelop.Core.FirstRun", false)) {
				isInitialRun = true;
				PropertyService.Set ("MonoDevelop.Core.FirstRun", false);
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", BuildVariables.PackageVersion);
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", CurrentRevision);
				PropertyService.SaveProperties ();
			}

			string lastVersion = PropertyService.Get ("MonoDevelop.Core.LastRunVersion", "1.9.1");
			int lastRevision = PropertyService.Get ("MonoDevelop.Core.LastRunRevision", 0);
			if (lastRevision != CurrentRevision && !isInitialRun) {
				isInitialRunAfterUpgrade = true;
				if (lastRevision == 0) {
					switch (lastVersion) {
						case "1.0": lastRevision = 1; break;
						case "2.0": lastRevision = 2; break;
						case "2.2": lastRevision = 3; break;
						case "2.2.1": lastRevision = 4; break;
					}
				}
				upgradedFromRevision = lastRevision;
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", BuildVariables.PackageVersion);
				PropertyService.Set ("MonoDevelop.Core.LastRunRevision", CurrentRevision);
				PropertyService.SaveProperties ();
			}
			
			// The ide is now initialized

			isInitialized = true;
			
			if (isInitialRun) {
				try {
					OnInitialRun ();
				} catch (Exception e) {
					LoggingService.LogError ("Error found while initializing the IDE", e);
				}
			}

			if (isInitialRunAfterUpgrade) {
				try {
					OnUpgraded (upgradedFromRevision);
				} catch (Exception e) {
					LoggingService.LogError ("Error found while initializing the IDE", e);
				}
			}
			
			if (initializedEvent != null)
				initializedEvent (null, EventArgs.Empty);
			
			// load previous combine
			if ((bool)PropertyService.Get("SharpDevelop.LoadPrevProjectOnStartup", false)) {
				var proj = DesktopService.RecentFiles.GetProjects ().FirstOrDefault ();
				if (proj != null) { 
					IdeApp.Workspace.OpenWorkspaceItem (proj.FileName).WaitForCompleted ();
				}
			}
			
			//FIXME: we should really make this on-demand. consumers can display a "loading help cache" message like VS
			MonoDevelop.Projects.HelpService.AsyncInitialize ();
			
			UpdateInstrumentationIcon ();
			IdeApp.Preferences.EnableInstrumentationChanged += delegate {
				UpdateInstrumentationIcon ();
			};
			AutoTestService.NotifyEvent ("MonoDevelop.Ide.IdeStart");
		}

		static void KeyBindingFailed (object sender, KeyBindingFailedEventArgs e)
		{
			Ide.IdeApp.Workbench.StatusBar.ShowMessage (e.Message);
		}
		
		//this method is MIT/X11, 2009, Michael Hutchinson / (c) Novell
		public static void OpenFiles (IEnumerable<FileOpenInformation> files)
		{
			if (files.Count() == 0)
				return;
			
			if (!IsInitialized) {
				EventHandler onInit = null;
				onInit = delegate {
					Initialized -= onInit;
					OpenFiles (files);
				};
				Initialized += onInit;
				return;
			}
			
			var filteredFiles = new List<FileOpenInformation> ();
			
			//open the firsts sln/workspace file, and remove the others from the list
		 	//FIXME: can we handle multiple slns?
			bool foundSln = false;
			foreach (var file in files) {
				if (Services.ProjectService.IsWorkspaceItemFile (file.FileName) ||
				    Services.ProjectService.IsSolutionItemFile (file.FileName)) {
					if (!foundSln) {
						try {
							Workspace.OpenWorkspaceItem (file.FileName);
							foundSln = true;
						} catch (Exception ex) {
							LoggingService.LogError ("Unhandled error opening solution/workspace \"" + file.FileName + "\"", ex);
							MessageService.ShowException (ex, "Could not load solution: " + file.FileName);
						}
					}
				} else {
					filteredFiles.Add (file);
				}
			}
			
			foreach (var file in filteredFiles) {
				try {
					Workbench.OpenDocument (file.FileName, file.Line, file.Column, file.Options);
				} catch (Exception ex) {
					LoggingService.LogError ("Unhandled error opening file \"" + file.FileName + "\"", ex);
					MessageService.ShowException (ex, "Could not open file: " + file.FileName);
				}
			}
			
			Workbench.Present ();
		}
		
		static bool FileServiceErrorHandler (string message, Exception ex)
		{
			MessageService.ShowException (ex, message);
			return true;
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
		
		public static void Run ()
		{
			// finally run the workbench window ...
			Gtk.Application.Run ();
		}
		
		
		/// <summary>
		/// Exits MonoDevelop. Returns false if the user cancels exiting.
		/// </summary>
		public static bool Exit ()
		{
			if (workbench.Close ()) {
				Gtk.Application.Quit ();
				return true;
			}
			return false;
		}
		
		internal static bool OnExit ()
		{
			if (Exiting != null) {
				ExitEventArgs args = new ExitEventArgs ();
				Exiting (null, args);
				return !args.Cancel;
			}
			return true;
		}
		
		internal static void OnExited ()
		{
			if (Exited != null)
				Exited (null, EventArgs.Empty);
		}

		static void OnInitialRun ()
		{
			Workbench.ResetToolbars ();
			SetInitialLayout ();
		}

		static void OnUpgraded (int previousRevision)
		{
			// Upgrade to latest msbuild version
			if (IdeApp.Preferences.DefaultProjectFileFormat.StartsWith ("MSBuild"))
				IdeApp.Preferences.DefaultProjectFileFormat = MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectService.DefaultFormat;
			
			if (previousRevision <= 3) {
				// Reset the current runtime when upgrading from <2.2, to ensure the default runtime is not stuck to an old mono install
				IdeApp.Preferences.DefaultTargetRuntime = Runtime.SystemAssemblyService.CurrentRuntime;
			}
			if (previousRevision < 5)
				SetInitialLayout ();
		}
		
		static void SetInitialLayout ()
		{
			if (!IdeApp.Workbench.Layouts.Contains ("Solution")) {
				// Create the Solution layout, based on Default
				IdeApp.Workbench.CurrentLayout = "Solution";
				IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ProjectPad.ProjectSolutionPad> ().Visible = false;
				IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ClassBrowser.ClassBrowserPad> ().Visible = false;
				foreach (Pad p in IdeApp.Workbench.Pads) {
					if (p.Visible)
						p.AutoHide = true;
				}
			}
		}

		static ITimeTracker commandTimeCounter;
			
		static void CommandServiceCommandTargetScanStarted (object sender, EventArgs e)
		{
			commandTimeCounter = Counters.CommandTargetScanTime.BeginTiming ();
		}

		static void CommandServiceCommandTargetScanFinished (object sender, EventArgs e)
		{
			commandTimeCounter.End ();
		}
		
		static StatusBarIcon instrumentationStatusIcon;
		static void UpdateInstrumentationIcon ()
		{
			if (IdeApp.Preferences.EnableInstrumentation) {
				if (instrumentationStatusIcon == null) {
					instrumentationStatusIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (ImageService.GetPixbuf (Gtk.Stock.DialogInfo));
					instrumentationStatusIcon.ToolTip = "Instrumentation service enabled";
					instrumentationStatusIcon.EventBox.ButtonPressEvent += delegate {
						InstrumentationService.StartMonitor ();
					};
				}
			} else if (instrumentationStatusIcon != null) {
				instrumentationStatusIcon.Dispose ();
			}
		}
	}
	
	public class IdeServices
	{
		public ProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}
	}
}
