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
using Mono.Addins.Gui;
using Mono.Addins.Setup;
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
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Templates;
using System.Threading.Tasks;

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

		public const int CurrentRevision = 5;

		static bool isMainRunning;
		static bool isInitialRun;
		static bool isInitialRunAfterUpgrade;
		static int upgradedFromRevision;
		
		public static event ExitEventHandler Exiting;
		public static event EventHandler Exited;
		
		static EventHandler initializedEvent;
		public static event EventHandler Initialized {
			add {
				Runtime.RunInMainThread (() => {
					if (isInitialized) value (null, EventArgs.Empty);
					else initializedEvent += value;
				});
			}
			remove {
				Runtime.RunInMainThread (() => {
					initializedEvent -= value;
				});
			}
		}

		internal static IdeCustomizer Customizer { get; set; }

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
				return Runtime.Version;
			}
		}
		
		public static void Initialize (ProgressMonitor monitor)
		{
			// Already done in IdeSetup, but called again since unit tests don't use IdeSetup.
			DispatchService.Initialize ();

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
			
			commandService.CommandTargetScanStarted += CommandServiceCommandTargetScanStarted;
			commandService.CommandTargetScanFinished += CommandServiceCommandTargetScanFinished;
			commandService.KeyBindingFailed += KeyBindingFailed;

			KeyBindingService.LoadBindingsFromExtensionPath ("/MonoDevelop/Ide/KeyBindingSchemes");
			KeyBindingService.LoadCurrentBindings ("MD2");

			commandService.CommandError += delegate (object sender, CommandErrorArgs args) {
				LoggingService.LogInternalError (args.ErrorMessage, args.Exception);
			};
			
			FileService.ErrorHandler = FileServiceErrorHandler;
		
			monitor.BeginTask (GettextCatalog.GetString("Loading Workbench"), 6);
			Counters.Initialization.Trace ("Loading Commands");
			
			commandService.LoadCommands ("/MonoDevelop/Ide/Commands");
			monitor.Step (1);

			// Before startup commands.
			Counters.Initialization.Trace ("Running Pre-Startup Commands");
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/PreStartupHandlers", OnExtensionChanged);
			monitor.Step (1);

			Counters.Initialization.Trace ("Initializing Workbench");
			workbench.Initialize (monitor);
			monitor.Step (1);
			
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
			Xwt.MessageDialog.RootWindow = Xwt.Toolkit.CurrentEngine.WrapWindow (workbench.RootWindow);
		
			commandService.EnableIdleUpdate = true;

			if (Customizer != null)
				Customizer.OnIdeInitialized ();
			
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
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", BuildInfo.Version);
				PropertyService.Set ("MonoDevelop.Core.LastRunRevision", CurrentRevision);
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
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", BuildInfo.Version);
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
			
			if (initializedEvent != null) {
				initializedEvent (null, EventArgs.Empty);
				initializedEvent = null;
			}
			
			//FIXME: we should really make this on-demand. consumers can display a "loading help cache" message like VS
			MonoDevelop.Projects.HelpService.AsyncInitialize ();
			
			UpdateInstrumentationIcon ();
			IdeApp.Preferences.EnableInstrumentation.Changed += delegate {
				UpdateInstrumentationIcon ();
			};
			AutoTestService.Start (commandService, Preferences.EnableAutomatedTesting);
			AutoTestService.NotifyEvent ("MonoDevelop.Ide.IdeStart");

			Gtk.LinkButton.SetUriHook ((button, uri) => Xwt.Desktop.OpenUrl (uri));
		}

		static void KeyBindingFailed (object sender, KeyBindingFailedEventArgs e)
		{
			Ide.IdeApp.Workbench.StatusBar.ShowWarning (e.Message);
		}
		
		//this method is MIT/X11, 2009, Michael Hutchinson / (c) Novell
		public static async void OpenFiles (IEnumerable<FileOpenInformation> files)
		{
			if (!files.Any ())
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
			bool closeCurrent = true;

			foreach (var file in files) {
				if (Services.ProjectService.IsWorkspaceItemFile (file.FileName) ||
				    Services.ProjectService.IsSolutionItemFile (file.FileName)) {
					try {
						// Close the current solution, but only for the first solution we open.
						// If more than one solution is specified in the list we want to open all them together.
						await Workspace.OpenWorkspaceItem (file.FileName, closeCurrent);
						closeCurrent = false;
					} catch (Exception ex) {
						MessageService.ShowError (GettextCatalog.GetString ("Could not load solution: {0}", file.FileName), ex);
					}
				} else if (file.FileName.HasExtension ("mpack")) {
					var service = new SetupService (AddinManager.Registry);
					AddinManagerWindow.RunToInstallFile (Workbench.RootWindow,
					                                     service,
					                                     file.FileName.FullPath);
				} else {
					filteredFiles.Add (file);
				}
			}

			// Wait for active load operations to be finished (there might be a solution already being loaded
			// when OpenFiles was called). This will ensure that files opened as part of the solution status
			// restoration won't steal the focus from the files we are explicitly loading here.
			await Workspace.CurrentWorkspaceLoadTask;

			foreach (var file in filteredFiles) {
				Workbench.OpenDocument (file.FileName, null, file.Line, file.Column, file.Options).ContinueWith (t => {
					if (t.IsFaulted)
						MessageService.ShowError (GettextCatalog.GetString ("Could not open file: {0}", file.FileName), t.Exception);
				}, TaskScheduler.FromCurrentSynchronizationContext ()).Ignore ();
			}

			Workbench.Present ();
		}

		static bool FileServiceErrorHandler (string message, Exception ex)
		{
			MessageService.ShowError (message, ex);
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
			isMainRunning = true;
			Gtk.Application.Run ();
		}

		public static bool IsRunning {
			get { return isMainRunning; }
		}

		/// <summary>
		/// Exits MonoDevelop. Returns false if the user cancels exiting.
		/// </summary>
		public static async Task<bool> Exit ()
		{
			if (await workbench.Close ()) {
				Gtk.Application.Quit ();
				isMainRunning = false;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Restarts MonoDevelop
		/// </summary>
		/// <returns> false if the user cancels exiting. </returns>
		/// <param name="reopenWorkspace"> true to reopen current workspace. </param>
		/// <remarks>
		/// Starts a new MonoDevelop instance in a new process and 
		/// stops the current MonoDevelop instance.
		/// </remarks>
		public static async Task<bool> Restart (bool reopenWorkspace = false)
		{
			if (await Exit ()) {
				try {
					DesktopService.RestartIde (reopenWorkspace);
				} catch (Exception ex) {
					LoggingService.LogError ("Restarting IDE failed", ex);
				}
				// return true here even if DesktopService.RestartIde has failed,
				// because the Ide has already been closed.
				return true;
			}
			return false;
		}

		static int idleActionsDisabled = 0;
		static Queue<Action> idleActions = new Queue<Action> ();

		/// <summary>
		/// Runs an action when the IDE is idle, that is, when the user is not performing any action.
		/// </summary>
		/// <param name="action">Action to execute</param>
		/// <remarks>
		/// This method should be used, for example, to show a notification to the user. The method will ensure
		/// that the dialog is shown when the user is not interacting with the IDE.
		/// </remarks>
		public static void RunWhenIdle (Action action)
		{
			Runtime.AssertMainThread ();
			idleActions.Enqueue (action);

			if (idleActionsDisabled == 0)
				DispatchIdleActions ();
		}

		/// <summary>
		/// Prevents the execution of idle actions
		/// </summary>
		public static void DisableIdleActions ()
		{
			Runtime.AssertMainThread ();
			idleActionsDisabled++;
		}

		/// <summary>
		/// Resumes the execution of idle actions
		/// </summary>
		public static void EnableIdleActions ()
		{
			Runtime.AssertMainThread ();

			if (idleActionsDisabled == 0) {
				LoggingService.LogError ("EnableIdleActions() call without corresponding DisableIdleActions() call");
				return;
			}

			if (--idleActionsDisabled == 0 && idleActions.Count > 0) {
				// After enabling idle actions, run them after a short pause, so for example if they were disabled
				// by the main menu, the actions won't execute right away after closing
				DispatchIdleActions (500);
			}
		}

		static void DispatchIdleActions (int withMsDelay = 0)
		{
			if (withMsDelay > 0) {
				Xwt.Application.TimeoutInvoke (withMsDelay, () => { DispatchIdleActions (0); return false; });
				return;
			}

			// If idle actions are disabled, this method will be called again when they are re-enabled.
			if (idleActionsDisabled > 0 || idleActions.Count == 0)
				return;

			// If a modal dialog is open, try again later
			if (DesktopService.IsModalDialogRunning ()) {
				DispatchIdleActions (1000);
				return;
			}

			// If the user interacted with the IDE just a moment ago, wait a bit more time before
			// running the action
			var interactionSpan = (int)(DateTime.Now - commandService.LastUserInteraction).TotalMilliseconds;
			if (interactionSpan < 500) {
				DispatchIdleActions (500 - interactionSpan);
				return;
			}

			var action = idleActions.Dequeue ();

			// Disable idle actions while running an idle action
			idleActionsDisabled++;
			try {
				action ();
			} catch (Exception ex) {
				LoggingService.LogError ("Idle action execution failed", ex);
			}
			idleActionsDisabled--;

			// If there are more actions to execute, do it after a short pause
			if (idleActions.Count > 0)
				DispatchIdleActions (500);
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
			SetInitialLayout ();
		}

		static void OnUpgraded (int previousRevision)
		{
			if (previousRevision <= 3) {
				// Reset the current runtime when upgrading from <2.2, to ensure the default runtime is not stuck to an old mono install
				IdeApp.Preferences.DefaultTargetRuntime.Value = Runtime.SystemAssemblyService.CurrentRuntime;
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
					instrumentationStatusIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.StatusInstrumentation));
					instrumentationStatusIcon.Title = GettextCatalog.GetString ("Instrumentation");
					instrumentationStatusIcon.ToolTip = GettextCatalog.GetString ("Instrumentation service enabled");
					instrumentationStatusIcon.Help = GettextCatalog.GetString ("Information about the Instrumentation Service");
					instrumentationStatusIcon.Clicked += delegate {
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
		readonly Lazy<TemplatingService> templatingService = new Lazy<TemplatingService> (() => new TemplatingService ());

		public ProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}

		public TemplatingService TemplatingService {
			get { return templatingService.Value; }
		}
	}
}
