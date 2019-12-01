﻿//
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
using MonoDevelop.Ide.CustomTools;
using System.Linq;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Templates;
using System.Threading.Tasks;
using MonoDevelop.Ide.RoslynServices;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Composition;
using System.Diagnostics;

namespace MonoDevelop.Ide
{
	public static class IdeApp
	{
		static bool isInitialized;
		static Workbench workbench;
		static CommandManager commandService;
		static TypeSystemService typeSystemService;
		static TaskCompletionSource<bool> initializationTask = new TaskCompletionSource<bool> ();

		static bool isInitialRun;
		static bool isInitialRunAfterUpgrade;
		static Version upgradedFromVersion;
		
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

		static EventHandler startupCompleted;
		public static event EventHandler StartupCompleted {
			add {
				Runtime.RunInMainThread (() => {
					startupCompleted += value;
				});
			}
			remove {
				Runtime.RunInMainThread (() => {
					startupCompleted -= value;
				});
			}
		}

		internal static void OnStartupCompleted ()
		{
			startupCompleted?.Invoke (null, EventArgs.Empty);
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

		public static Workbench Workbench {
			get { return workbench; }
		}

		public static ProjectOperations ProjectOperations => IdeServices.ProjectOperations;

		public static RootWorkspace Workspace => Runtime.PeekService<RootWorkspace> ();

		public static CommandManager CommandService {
			get { return commandService; }
		}

		public static TypeSystemService TypeSystemService {
			get {
				if (typeSystemService == null)
					typeSystemService = Runtime.GetService<TypeSystemService> ().Result;
				return typeSystemService;
			}
		}

		public static IdePreferences Preferences { get; } = new IdePreferences ();

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
		public static Version UpgradedFromVersion {
			get { return upgradedFromVersion; }
		}
		
		public static Version Version {
			get {
				return Runtime.Version;
			}
		}

		public enum LaunchType
		{
			Unknown,
			Normal,
			LaunchedFromFileManager
		}

		static LaunchType launchType = LaunchType.Unknown;
		public static LaunchType LaunchReason {
			get => launchType;
			internal set {
				launchType = value;

				if (!LaunchCompletionSource.TrySetResult (value)) {
					LoggingService.LogWarning ($"LaunchReason is already set to {launchType}.");
				}
			}
		}

		internal static TaskCompletionSource<LaunchType> LaunchCompletionSource { get; } = new TaskCompletionSource<LaunchType>();

		public static async Task Initialize (ProgressMonitor monitor, bool hideWelcomePage = false)
		{
			// Already done in IdeSetup, but called again since unit tests don't use IdeSetup.
			DispatchService.Initialize ();

			// Set initial run flags
			Counters.InitializationTracker.Trace ("Upgrading Settings");

			if (PropertyService.Get ("MonoDevelop.Core.FirstRun", true)) {
				isInitialRun = true;
				PropertyService.Set ("MonoDevelop.Core.FirstRun", false);
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", Runtime.Version.ToString ());
				PropertyService.SaveProperties ();
			}

			string lastVersionString = PropertyService.Get ("MonoDevelop.Core.LastRunVersion", "1.0");
			Version.TryParse (lastVersionString, out var lastVersion);

			if (Runtime.Version > lastVersion && !isInitialRun) {
				isInitialRunAfterUpgrade = true;
				upgradedFromVersion = lastVersion;
				PropertyService.Set ("MonoDevelop.Core.LastRunVersion", Runtime.Version.ToString ());
				PropertyService.SaveProperties ();
			}

			Counters.InitializationTracker.Trace ("Initializing WelcomePage service");
			WelcomePage.WelcomePageService.Initialize (hideWelcomePage).Ignore ();

			// Pump the UI thread to make the start window visible

			await Task.Yield ();

			Counters.InitializationTracker.Trace ("Creating Services");

			var serviceInitialization = Task.WhenAll (
				Runtime.GetService<DesktopService> (),
				Runtime.GetService<FontService> (),
				Runtime.GetService<TaskService> (),
				Runtime.GetService<ProjectOperations> (),
				Runtime.GetService<TextEditorService> (),
				Runtime.GetService<NavigationHistoryService> (),
				Runtime.GetService<DisplayBindingService> (),
				Runtime.GetService<RootWorkspace> (),
				Runtime.GetService<HelpOperations> (),
				Runtime.GetService<HelpService> ()
			);

			commandService = await Runtime.GetService<CommandManager> ();

			await serviceInitialization;

			Counters.InitializationTracker.Trace ("Creating Workbench");
			workbench = new Workbench ();

			Counters.InitializationTracker.Trace ("Creating Root Workspace");

			CustomToolService.Init ();
			
			FileService.ErrorHandler = FileServiceErrorHandler;

			monitor.BeginTask (GettextCatalog.GetString("Loading Workbench"), 3);

			// Before startup commands.
			Counters.InitializationTracker.Trace ("Running Pre-Startup Commands");
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/PreStartupHandlers", OnExtensionChanged);
			monitor.Step (1);

			Counters.InitializationTracker.Trace ("Initializing Workbench");
			await workbench.Initialize (monitor);
			monitor.Step (1);

			MessageService.RootWindow = workbench.RootWindow;
			Xwt.MessageDialog.RootWindow = Xwt.Toolkit.CurrentEngine.WrapWindow (workbench.RootWindow);
		
			commandService.EnableIdleUpdate = true;

			if (Customizer != null)
				Customizer.OnIdeInitialized ();

			monitor.EndTask ();

			UpdateInstrumentationIcon ();
			IdeApp.Preferences.EnableInstrumentation.Changed += delegate {
				UpdateInstrumentationIcon ();
			};
			AutoTestService.Start (commandService, Preferences.EnableAutomatedTesting);
			AutoTestService.NotifyEvent ("MonoDevelop.Ide.IdeStart");

			Gtk.LinkButton.SetUriHook ((button, uri) => Xwt.Desktop.OpenUrl (uri));

			// Start initializing the type system service in the background
			Runtime.GetService<TypeSystemService> ().Ignore ();

			// The ide is now initialized
			OnInitialized ();
		}

		static void OnInitialized ()
		{
			// The ide is now initialized

			isInitialized = true;

			if (initializedEvent != null) {
				initializedEvent (null, EventArgs.Empty);
				initializedEvent = null;
			}

			initializationTask.SetResult (true);

			// Startup commands
			Counters.InitializationTracker.Trace ("Running Startup Commands");
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/StartupHandlers", OnExtensionChanged);

			Runtime.GetService<CompositionManager> ().Ignore ();
		}

		public static Task EnsureInitializedAsync ()
		{
			return initializationTask.Task;
		}

		public static void BringToFront ()
		{
			Initialized += (sender, e) => {
				if (WelcomePage.WelcomePageService.HasWindowImplementation && !Workbench.RootWindow.Visible) {
					WelcomePage.WelcomePageService.ShowWelcomeWindow (new Ide.WelcomePage.WelcomeWindowShowOptions (true));
				} else {
					Workbench.Present ();
				}
			};
		}

		//this method is MIT/X11, 2009, Michael Hutchinson / (c) Novell

		public static void OpenFiles (IEnumerable<FileOpenInformation> files)
		{
			OpenFilesAsync (files, null).Ignore ();
		}

		public static Task<bool> OpenFilesAsync (IEnumerable<FileOpenInformation> files)
		{
			return OpenFilesAsync (files, null);
		}

		//this method is MIT/X11, 2009, Michael Hutchinson / (c) Novell
		internal static async Task<bool> OpenFilesAsync (IEnumerable<FileOpenInformation> files, OpenWorkspaceItemMetadata metadata)
		{
			if (!files.Any ())
				return false;

			await initializationTask.Task;

			Workbench.Present ();

			var filteredFiles = new List<FileOpenInformation> ();

			Gdk.ModifierType mtype = Components.GtkWorkarounds.GetCurrentKeyModifiers ();
			bool closeCurrent = !mtype.HasFlag (Gdk.ModifierType.ControlMask);
			if (Platform.IsMac && closeCurrent)
				closeCurrent = !mtype.HasFlag (Gdk.ModifierType.MetaMask);

			foreach (var file in files) {
				if (Services.ProjectService.IsWorkspaceItemFile (file.FileName) ||
				    Services.ProjectService.IsSolutionItemFile (file.FileName)) {
					try {
						// Close the current solution, but only for the first solution we open.
						// If more than one solution is specified in the list we want to open all them together.
						await Workspace.OpenWorkspaceItem (file.FileName, closeCurrent, true, metadata);
						closeCurrent = false;
					} catch (Exception ex) {
						MessageService.ShowError (GettextCatalog.GetString ("Could not load solution: {0}", file.FileName), ex);
					}
				} else if (file.FileName.HasExtension ("mpack")) {
					var service = new SetupService (AddinManager.Registry);
					AddinManagerWindow.RunToInstallFile (Workbench.RootWindow.Visible ? Workbench.RootWindow : null,
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

			for (int n = 0; n < filteredFiles.Count; n++) {
				var file = filteredFiles [n];
				if (n == 0)
					file.Options |= OpenDocumentOptions.BringToFront;
				else
					file.Options &= ~OpenDocumentOptions.BringToFront;
				IdeServices.DocumentManager.OpenDocument (file).Ignore ();
			}
			return true;
		}

		static bool FileServiceErrorHandler (string message, Exception ex)
		{
			MessageService.ShowError (message, ex);
			return true;
		}

		static readonly Stopwatch startupCommandsStopwatch = new Stopwatch ();
		static readonly CommandInfo reusableCommandInfo = new CommandInfo ();
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				// Run handlers in different UI loops to avoid freezing the UI for too much time
				Xwt.Application.Invoke (() => {
					try {
						if (!(args.ExtensionObject is CommandHandler handler)) {
							LoggingService.LogError ("Type " + args.ExtensionObject.GetType () + " must be a subclass of MonoDevelop.Components.Commands.CommandHandler");
							return;
						}

						startupCommandsStopwatch.Restart ();
						handler.InternalRun ();
						startupCommandsStopwatch.Stop ();

						if (args.ExtensionNode is TypeExtensionNode node) {
							commandService.OnCommandActivated (node.TypeName, reusableCommandInfo, null, null, CommandSource.Startup, startupCommandsStopwatch.Elapsed);
#if DEBUG
							LoggingService.LogDebug ("Startup command handler: {0}", node.TypeName);
#endif
						}
					} catch (Exception ex) {
						LoggingService.LogError ($"Error while running startup handler {args.ExtensionObject.GetType ()}", ex);
					}
				});
			}
		}
		
		public static bool IsRunning { get; internal set; }

		public static bool IsExiting { get; private set; }

		/// <summary>
		/// Exits MonoDevelop. Returns false if the user cancels exiting.
		/// </summary>
		public static async Task<bool> Exit ()
		{
			IsExiting = true;
			if (await workbench.Close ()) {
				Gtk.Application.Quit ();
				return true;
			}
			IsExiting = false;
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
				// Log that we restarted ourselves
				PropertyService.Set ("MonoDevelop.Core.RestartRequested", true);

				try {
					IdeServices.DesktopService.RestartIde (reopenWorkspace);
				} catch (Exception ex) {
					LoggingService.LogError ("Restarting IDE failed", ex);
				}
				// return true here even if IdeServices.DesktopService.RestartIde has failed,
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
			if (IdeServices.DesktopService.IsModalDialogRunning ()) {
				DispatchIdleActions (1000);
				return;
			}

			// If the user interacted with the IDE just a moment ago, wait a bit more time before
			// running the action
			var interactionSpan = Math.Max (0, (DateTime.Now - commandService.LastUserInteraction).TotalMilliseconds);
			if (interactionSpan < 500) {
				DispatchIdleActions (500 - (int) interactionSpan);
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
			var exiting = Exiting;
			if (exiting != null) {
				bool haveAnyCancelled = false;
				var args = new ExitEventArgs ();
				foreach (ExitEventHandler handler in exiting.GetInvocationList ()) {
					try {
						handler (null, args);
						haveAnyCancelled |= args.Cancel;
					} catch (Exception ex) {
						LoggingService.LogError ("Exception processing IdeApp.Exiting handler.", ex);
					}
				}
				return !haveAnyCancelled;
			}
			return true;
		}
		
		internal static void OnExited ()
		{
			if (Exited != null)
				Exited (null, EventArgs.Empty);
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
}
