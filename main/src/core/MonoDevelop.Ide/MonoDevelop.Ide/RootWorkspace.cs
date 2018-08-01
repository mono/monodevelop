// RootWorkspace.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using System.Runtime.CompilerServices;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core.Execution;
using System.Threading.Tasks;

namespace MonoDevelop.Ide
{
	public sealed class RootWorkspace: WorkspaceObject, IBuildTarget
	{
		RootWorkspaceItemCollection items;
//		IParserDatabase parserDatabase;
		string activeConfiguration;
		bool useDefaultRuntime;

		internal RootWorkspace ()
		{
			items = new RootWorkspaceItemCollection (this);

			currentWorkspaceLoadTask = new TaskCompletionSource<bool> ();
			currentWorkspaceLoadTask.SetResult (true);

			FileService.FileRenamed += CheckFileRename;
			
			// Set the initial active runtime
			UseDefaultRuntime = true;
			IdeApp.Preferences.DefaultTargetRuntime.Changed += delegate {
				// If the default runtime changes and current active is default, update it
				if (UseDefaultRuntime) {
					Runtime.SystemAssemblyService.DefaultRuntime = IdeApp.Preferences.DefaultTargetRuntime;
					useDefaultRuntime = true;
				}
			};
		}
		
		public RootWorkspaceItemCollection Items {
			get {
				return items; 
			}
		}
		/*
		public IParserDatabase ParserDatabase {
			get { 
				if (parserDatabase == null) {
					parserDatabase = Services.ParserService.CreateParserDatabase ();
					parserDatabase.TrackFileChanges = true;
					parserDatabase.ParseProgressMonitorFactory = new ParseProgressMonitorFactory (); 
				}
				return parserDatabase; 
			}
		}*/
		
		public string ActiveConfigurationId {
			get {
				return activeConfiguration;
			}
			set {
				if (activeConfiguration != value) {
					activeConfiguration = value;
					OnActiveConfigurationChanged ();
				}
			}
		}

		void OnActiveConfigurationChanged ()
		{
			if (ActiveConfigurationChanged != null)
				ActiveConfigurationChanged (this, EventArgs.Empty);
		}

		public ExecutionTarget GetActiveExecutionTarget (SolutionItem project)
		{
			return (activeExecutionTarget as MultiProjectExecutionTarget)?.GetTarget (project) ?? activeExecutionTarget;
		}

		ExecutionTarget activeExecutionTarget;
		public ExecutionTarget ActiveExecutionTarget {
			get { return activeExecutionTarget; }
			set {
				if (activeExecutionTarget != value) {
					activeExecutionTarget = value;
					OnActiveExecutionTargetChanged ();
				}
			}
		}

		void OnActiveExecutionTargetChanged ()
		{
			if (ActiveExecutionTargetChanged != null)
				ActiveExecutionTargetChanged (this, EventArgs.Empty);
		}

		public ConfigurationSelector ActiveConfiguration {
			get { return new SolutionConfigurationSelector (activeConfiguration); }
		}
		
		public TargetRuntime ActiveRuntime {
			get {
				return Runtime.SystemAssemblyService.DefaultRuntime;
			}
			set {
				useDefaultRuntime = false;
				Runtime.SystemAssemblyService.DefaultRuntime = value;
			}
		}
		
		public bool UseDefaultRuntime {
			get { return useDefaultRuntime; }
			set {
				if (useDefaultRuntime != value) {
					useDefaultRuntime = value;
					if (value)
						Runtime.SystemAssemblyService.DefaultRuntime = IdeApp.Preferences.DefaultTargetRuntime;
				}
			}
		}
		
		public bool IsOpen {
			get { return Items.Count > 0; }
		}
		
		[ThreadSafe]
		protected override string OnGetName ()
		{
			return "MonoDevelop Workspace";
		}

		protected override string OnGetBaseDirectory ()
		{
			return IdeApp.Preferences.ProjectsDefaultPath;
		}

		protected override string OnGetItemDirectory ()
		{
			return BaseDirectory;
		}

		protected override IEnumerable<WorkspaceObject> OnGetChildren ()
		{
			return Items;
		}

		public IEnumerable<IBuildTarget> GetExecutionDependencies ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				return IdeApp.ProjectOperations.CurrentSelectedSolution.GetExecutionDependencies ();
			else
				return new IBuildTarget [0];
		}


#region Model queries
		
		public IEnumerable<SolutionItem> GetAllSolutionItems ()
		{
			return GetAllItems<SolutionItem> ();
		}
		
		public IEnumerable<Project> GetAllProjects ()
		{
			return GetAllItems<Project> ();
		}
		
		public IEnumerable<Solution> GetAllSolutions ()
		{
			return GetAllItems<Solution> ();
		}
			
		public IEnumerable<Project> GetProjectsContainingFile (string fileName)
		{
			foreach (WorkspaceItem it in Items) {
				foreach (Project p in it.GetProjectsContainingFile (fileName)) {
					yield return p;
				}
			}
		}

#endregion
		
#region Build and run operations
		
		public async Task SaveAsync ()
		{
			ProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				await SaveAsync (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Workspace saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}

		bool IBuildTarget.CanBuild (ConfigurationSelector configuration)
		{
			return true;
		}

		bool IBuildTarget.CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				return IdeApp.ProjectOperations.CurrentSelectedSolution.CanExecute (context, configuration);
			else {
				return false;
			}
		}
		
		public async Task SaveAsync (ProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Saving Workspace..."), Items.Count);
			List<WorkspaceItem> items = new List<WorkspaceItem> (Items);
			foreach (WorkspaceItem it in items) {
				await it.SaveAsync (monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}
		
		async Task<BuildResult> IBuildTarget.Build (ProgressMonitor monitor, ConfigurationSelector configuration, bool buildReferences, OperationContext operationContext)
		{
			BuildResult result = null;
			var items = Items.OfType<IBuildTarget> ().ToList ();
			foreach (var it in items) {
				BuildResult res = await it.Build (monitor, configuration, buildReferences, operationContext);
				if (res != null) {
					if (result == null)
						result = new BuildResult ();
					result.Append (res);
				}
			}
			return result;
		}

		async Task<BuildResult> IBuildTarget.Clean (ProgressMonitor monitor, ConfigurationSelector configuration, OperationContext operationContext)
		{
			BuildResult result = null;
			var items = Items.OfType<IBuildTarget> ().ToList ();
			foreach (var it in items) {
				BuildResult res = await it.Clean (monitor, configuration, operationContext);
				if (res != null) {
					if (result == null)
						result = new BuildResult ();
					result.Append (res);
				}
			}
			return result;
		}

		Task IBuildTarget.PrepareExecution (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			return Task.FromResult (0);
		}

		public Task Execute (ProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution ?? GetAllSolutions ().FirstOrDefault ();
			if (sol != null)
				return sol.Execute (monitor, context, configuration);
			else
				throw new UserException (GettextCatalog.GetString ("No solution has been selected."));
		}
		
		[Obsolete ("This method will be removed in future releases")]
		public bool NeedsBuilding ()
		{
			return true;
		}

		[Obsolete ("This method will be removed in future releases")]
		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			return true;
		}

		[Obsolete ("This method will be removed in future releases")]
		public void SetNeedsBuilding (bool needsBuilding, ConfigurationSelector configuration)
		{
		}

		bool IsDirtyFileInCombine {
			get {
				foreach (Project projectEntry in GetAllProjects()) {
					foreach (ProjectFile fInfo in projectEntry.Files) {
						foreach (Document doc in IdeApp.Workbench.Documents) {
							if (doc.IsDirty && doc.FileName == fInfo.FilePath) {
								return true;
							}
						}
					}
				}
				return false;
			}
		}
		
		public ReadOnlyCollection<string> GetConfigurations ()
		{
			List<string> configs = new List<string> ();
			foreach (WorkspaceItem it in Items) {
				foreach (string conf in it.GetConfigurations ()) {
					if (!configs.Contains (conf))
						configs.Add (conf);
				}
			}
			return configs.AsReadOnly ();
		}
#endregion
		
#region Opening and closing

		public void SavePreferences ()
		{
			foreach (WorkspaceItem it in Items)
				SavePreferences (it);
		}
		
		public async Task<bool> Close ()
		{
			return await Close (true);
		}

		public async Task<bool> Close (bool saveWorkspacePreferencies)
		{
			return await Close (saveWorkspacePreferencies, true);
		}
		
		internal async Task<bool> Close (bool saveWorkspacePreferencies, bool closeProjectFiles)
		{
			if (Items.Count > 0) {
				ITimeTracker timer = Counters.CloseWorkspaceTimer.BeginTiming ();
				try {
					// Request permission for unloading the items
					foreach (WorkspaceItem it in new List<WorkspaceItem> (Items)) {
						if (!RequestItemUnload (it))
							return false;
					}

					if (saveWorkspacePreferencies)
						SavePreferences ();

					if (closeProjectFiles) {
						foreach (Document doc in IdeApp.Workbench.Documents.ToArray ()) {
							if (!await doc.Close ())
								return false;
						}
					}

					foreach (WorkspaceItem it in new List<WorkspaceItem> (Items)) {
						try {
							Items.Remove (it);
							it.Dispose ();
						} catch (Exception ex) {
							MessageService.ShowError (GettextCatalog.GetString ("Could not close solution '{0}'.", it.Name), ex);
						}
					}
				} finally {
					timer.End ();
				}
			}
			return true;
		}

		public async Task CloseWorkspaceItem (WorkspaceItem item, bool closeItemFiles = true)
		{
			if (!Items.Contains (item))
				throw new InvalidOperationException ("Only top level items can be closed.");

			if (Items.Count == 1 && closeItemFiles) {
				// There is only one item, close the whole workspace
				await Close (true, closeItemFiles);
				return;
			}

			if (RequestItemUnload (item)) {
				if (closeItemFiles) {
					var projects = item.GetAllItems<Project> ();
					foreach (Document doc in IdeApp.Workbench.Documents.Where (d => d.Project != null && projects.Contains (d.Project)).ToArray ()) {
						if (!await doc.Close ())
							return;
					}
				}
				Items.Remove (item);
				item.Dispose ();
			}
		}

		public bool RequestItemUnload (WorkspaceObject item)
		{
			var itemUnloading = ItemUnloading;
			if (itemUnloading != null) {
				try {
					bool haveAnyCancelled = false;
					ItemUnloadingEventArgs args = new ItemUnloadingEventArgs (item);
					foreach (EventHandler<ItemUnloadingEventArgs> handler in itemUnloading.GetInvocationList ()) {
						handler (this, args);
						haveAnyCancelled |= args.Cancel;
					}
					return !haveAnyCancelled;
				}
				catch (Exception ex) {
					LoggingService.LogError ("Exception in ItemUnloading.", ex);
				}
			}
			return true;
		}

		System.Threading.CancellationTokenSource openingItemCancellationSource;
		TaskCompletionSource<bool> currentWorkspaceLoadTask;
		int loadOperationsCount;
		object loadLock = new object ();

		internal bool WorkspaceItemIsOpening {
			get { return loadOperationsCount > 0; }
		}

		/// <summary>
		/// Gets the task that is currently loading a solution
		/// </summary>
		internal Task CurrentWorkspaceLoadTask {
			get { return currentWorkspaceLoadTask.Task; }
		}

		public Task<bool> OpenWorkspaceItem (FilePath file)
		{
			return OpenWorkspaceItem (file, true);
		}
		
		public Task<bool> OpenWorkspaceItem (FilePath file, bool closeCurrent)
		{
			return OpenWorkspaceItem (file, closeCurrent, true);
		}

		public Task<bool> OpenWorkspaceItem (FilePath file, bool closeCurrent, bool loadPreferences)
		{
			return OpenWorkspaceItem (file, closeCurrent, loadPreferences, null);
		}

		internal async Task<bool> OpenWorkspaceItem (FilePath file, bool closeCurrent, bool loadPreferences, OpenWorkspaceItemMetadata metadata)
		{
			lock (loadLock) {
				if (++loadOperationsCount == 1)
					currentWorkspaceLoadTask = new TaskCompletionSource<bool> ();
				else {
					// If there is a load operation in progress, cancel it
					if (openingItemCancellationSource != null && closeCurrent) {
						openingItemCancellationSource.Cancel ();
						openingItemCancellationSource = null;
					}
				}
				if (openingItemCancellationSource == null)
					openingItemCancellationSource = new System.Threading.CancellationTokenSource ();
			}

			try {
				return await OpenWorkspaceItemInternal (file, closeCurrent, loadPreferences, metadata, null);
			}
			finally {
				lock (loadLock) {
					if (--loadOperationsCount == 0) {
						openingItemCancellationSource = null;
						currentWorkspaceLoadTask.SetResult (true);
					}
				}
			}
		}

		public Task<bool> OpenWorkspaceItemInternal (FilePath file, bool closeCurrent, bool loadPreferences)
		{
			return OpenWorkspaceItemInternal (file, closeCurrent, loadPreferences, null, null);
		}

		internal async Task<bool> OpenWorkspaceItemInternal (FilePath file, bool closeCurrent, bool loadPreferences, OpenWorkspaceItemMetadata metadata, ProgressMonitor loadMonitor)
		{
			var item = GetAllItems<WorkspaceItem> ().FirstOrDefault (w => w.FileName == file.FullPath);
			if (item != null) {
				IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem = item;
				IdeApp.Workbench.StatusBar.ShowWarning (GettextCatalog.GetString ("{0} is already opened", item.FileName.FileName));
				return true;
			}

			if (closeCurrent) {
				if (!await Close ())
					return false;
			}

			var monitor = loadMonitor ?? IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true);
			bool reloading = IsReloading;

			monitor = monitor.WithCancellationSource (openingItemCancellationSource);

			IdeApp.Workbench.LockGui ();
			metadata = GetOpenWorkspaceItemMetadata (metadata);
			ITimeTracker timer = Counters.OpenWorkspaceItemTimer.BeginTiming (metadata);

			try {
				var oper = BackgroundLoadWorkspace (monitor, file, loadPreferences, reloading, metadata, timer);
				return await oper;
			} finally {
				timer.End ();

				monitor.Dispose ();
				IdeApp.Workbench.UnlockGui ();
			}
		}
		
		void ReattachDocumentProjects (IEnumerable<string> closedDocs)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.Project == null && doc.IsFile) {
					Project p = GetProjectsContainingFile (doc.FileName).FirstOrDefault ();
					if (p != null)
						doc.SetProject (p);
				}
			}
			if (closedDocs != null) {
				foreach (string doc in closedDocs) {
					IdeApp.Workbench.OpenDocument (doc, null, false);
				}
			}
		}
		
		async Task<bool> BackgroundLoadWorkspace (ProgressMonitor monitor, FilePath file, bool loadPreferences, bool reloading, OpenWorkspaceItemMetadata metadata, ITimeTracker timer)
		{
			WorkspaceItem item = null;

			try {
				if (reloading)
					SetReloading (true);

				if (!File.Exists (file)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", file), null);
					return false;
				}

				if (!Services.ProjectService.IsWorkspaceItemFile (file)) {
					if (!Services.ProjectService.IsSolutionItemFile (file)) {
						monitor.ReportError (GettextCatalog.GetString ("File is not a project or solution: {0}", file), null);
						return false;
					}

					// It is a project, not a solution. Try to create a dummy solution and add the project to it

					if (File.Exists (Path.ChangeExtension (file, ".sln"))) {
						metadata.Reason = OpenWorkspaceItemMetadata.OpenReason.OpenProject;
					} else {
						metadata.Reason = OpenWorkspaceItemMetadata.OpenReason.CreateSolution;
					}

					timer.Trace ("Getting wrapper solution");
					item = await IdeApp.Services.ProjectService.GetWrapperSolution (monitor, file);
				}
				
				if (item == null) {
					timer.Trace ("Reading item");
					item = await Services.ProjectService.ReadWorkspaceItem (monitor, file);
					if (monitor.CancellationToken.IsCancellationRequested)
						return false;
				}

				timer.Trace ("Registering to recent list");
				DesktopService.RecentFiles.AddProject (item.FileName, item.Name);
				
			} catch (Exception ex) {
				LoggingService.LogError ("Load operation failed", ex);
				monitor.ReportError ("Load operation failed.", ex);
				
				if (item != null)
					item.Dispose ();
				return false;
			} finally {
				if (reloading)
					SetReloading (false);
			}

			using (monitor) {
				// Add the item in the GUI thread. It is not safe to do it in the background thread.
				if (!monitor.CancellationToken.IsCancellationRequested) {

					// Set the active configuration before adding the solution to the workspace, in this way
					// roslyn data will be loaded using the stored configuration instead of the default.
					if (Items.Count == 0)
						ActiveConfigurationId = GetStoredActiveConfiguration (item, loadPreferences);

					item.SetShared ();
					Items.Add (item);
					await FileWatcherService.Add (item);
				}
				else {
					item.Dispose ();
					return false;
				}
				if (IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem == null)
					IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem = GetAllSolutions ().FirstOrDefault ();

				Document.IsInProjectSettingLoadingProcess = true;
				try {
					if (Items.Count == 1 && loadPreferences) {
						timer.Trace ("Restoring workspace preferences");
						await RestoreWorkspacePreferences (item);
					}

					if (Items.Count == 1 && !reloading)
						FirstWorkspaceItemRestored?.Invoke (this, new WorkspaceItemEventArgs (item));

					timer.Trace ("Reattaching documents");
					ReattachDocumentProjects (null);
					monitor.ReportSuccess (GettextCatalog.GetString ("Solution loaded."));

					UpdateOpenWorkspaceItemMetadata (metadata, item);
				} finally {
					Document.IsInProjectSettingLoadingProcess = false;
				}
			}
			return true;
		}

		static OpenWorkspaceItemMetadata GetOpenWorkspaceItemMetadata (OpenWorkspaceItemMetadata metadata)
		{
			if (metadata == null) {
				metadata = new OpenWorkspaceItemMetadata ();
				metadata.OnStartup = false;
			}

			// Will be set to true after a successful load.
			metadata.LoadSucceed = false;
			metadata.Reason = OpenWorkspaceItemMetadata.OpenReason.OpenSolution;

			return metadata;
		}

		static void UpdateOpenWorkspaceItemMetadata (OpenWorkspaceItemMetadata metadata, WorkspaceItem item)
		{
			// Is this a workspace or a solution?
			metadata.IsSolution = (item is Solution);
			metadata.LoadSucceed = true;
			metadata.TotalProjectCount = item.GetAllItems<Project> ().Count ();
		}

		string GetStoredActiveConfiguration (WorkspaceItem item, bool loadPreferences)
		{
			WorkspaceUserData data = loadPreferences ? item.UserProperties.GetValue<WorkspaceUserData> ("MonoDevelop.Ide.Workspace") : null;
			if (data != null) {
				if (item.GetConfigurations ().Contains (data.ActiveConfiguration))
					return data.ActiveConfiguration;
			}
			return GetBestDefaultConfiguration (item);
		}

		async Task RestoreWorkspacePreferences (WorkspaceItem item)
		{
			// Restore local configuration data
			
			try {
				WorkspaceUserData data = item.UserProperties.GetValue<WorkspaceUserData> ("MonoDevelop.Ide.Workspace");
				if (data != null) {
					ActiveExecutionTarget = null;

					if (string.IsNullOrEmpty (data.ActiveRuntime))
						UseDefaultRuntime = true;
					else {
						TargetRuntime tr = Runtime.SystemAssemblyService.GetTargetRuntime (data.ActiveRuntime);
						if (tr != null)
							ActiveRuntime = tr;
						else
							UseDefaultRuntime = true;
					}
				}
			}
			catch (Exception ex) {
				LoggingService.LogError ("Exception while loading user solution preferences.", ex);
			}
			
			// Allow add-ins to restore preferences
			
			if (LoadingUserPreferences != null) {
				UserPreferencesEventArgs args = new UserPreferencesEventArgs (item, item.UserProperties);
				try {
					foreach (AsyncEventHandler<UserPreferencesEventArgs> d in LoadingUserPreferences.GetInvocationList ())
						await d (this, args);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in LoadingUserPreferences.", ex);
				}
			}
		}
		
		string GetBestDefaultConfiguration (WorkspaceItem item)
		{
			// 'Debug' is always the best candidate. If there is no debug, pick
			// the configuration with the highest number of built projects.
			int nbuilds = 0;
			string bestConfig = null;
			foreach (Solution sol in item.GetAllItems<Solution> ()) {
				foreach (string conf in sol.GetConfigurations ()) {
					if (conf == "Debug")
						return conf;
					SolutionConfiguration sconf = sol.GetConfiguration (new SolutionConfigurationSelector (conf));
					int c = 0;
					foreach (var sce in sconf.Configurations)
						if (sce.Build) c++;
					if (c > nbuilds) {
						nbuilds = c;
						bestConfig = conf;
					}
				}
			}
			return bestConfig;
		}
		
		public Task SavePreferences (WorkspaceItem item)
		{
			// Local configuration info
			
			WorkspaceUserData data = new WorkspaceUserData ();
			data.ActiveConfiguration = ActiveConfigurationId;
			data.ActiveRuntime = UseDefaultRuntime ? null : ActiveRuntime.Id;
			item.UserProperties.SetValue ("MonoDevelop.Ide.Workspace", data);
			
			// Allow add-ins to fill-up data
			
			if (StoringUserPreferences != null) {
				UserPreferencesEventArgs args = new UserPreferencesEventArgs (item, item.UserProperties);
				try {
					StoringUserPreferences (this, args);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in UserPreferencesRequested.", ex);
				}
			}
			
			// Save the file
			
			return item.SaveUserProperties ();
		}

		[Obsolete ("FileService will generate events for all workspace files.")]
		public FileStatusTracker GetFileStatusTracker ()
		{
			FileStatusTracker fs = new FileStatusTracker ();
			fs.AddFiles (GetKnownFiles ());
			return fs;
		}
		
		static IEnumerable<FilePath> GetKnownFiles ()
		{
			foreach (WorkspaceItem item in IdeApp.Workspace.Items) {
				foreach (FilePath file in item.GetItemFiles (true))
					yield return file;
			}
		}

		int reloadingCount;

		internal bool IsReloading {
			get { return reloadingCount > 0; }
		}

		void SetReloading (bool doingIt)
		{
			if (doingIt)
				reloadingCount++;
			else
				reloadingCount--;
		}

		void SolutionReloadRequired (object sender, WorkspaceItemEventArgs e)
		{
			OnCheckWorkspaceItem (e.Item).Ignore ();
		}

		void SolutionItemReloadRequired (object sender, SolutionItemEventArgs e)
		{
			OnCheckProject (e.SolutionItem).Ignore ();
		}
		
		async Task OnCheckWorkspaceItem (WorkspaceItem item)
		{
			if (item.NeedsReload) {
				var result = await AllowReload (item.GetAllItems<Project> ());
				bool allowReload = result.Item1;
				IEnumerable<string> closedDocs = result.Item2;
				if (result.Item1) {
					if (item.ParentWorkspace == null) {
						string file = item.FileName;
						try {
							SetReloading (true);
							SavePreferences ();
							await CloseWorkspaceItem (item, false);
							await OpenWorkspaceItem (file, false, false);
						} finally {
							SetReloading (false);
						}
					}
					else {
						using (ProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true)) {
							await item.ParentWorkspace.ReloadItem (m, item);
							if (closedDocs != null)
								ReattachDocumentProjects (closedDocs);
						}
					}

					return;
				} else
					item.NeedsReload = false;
			}

			if (item is Workspace) {
				Workspace ws = (Workspace) item;
				List<WorkspaceItem> items = new List<WorkspaceItem> (ws.Items);
				foreach (WorkspaceItem it in items)
					await OnCheckWorkspaceItem (it);
			}
			else if (item is Solution) {
				Solution sol = (Solution) item;
				await OnCheckProject (sol.RootFolder);
			}
		}
		
		async Task OnCheckProject (SolutionFolderItem entry)
		{
			if (entry.NeedsReload) {
				IEnumerable projects = null;
				if (entry is Project) {
					projects = new Project [] { (Project) entry };
				} else if (entry is SolutionFolder) {
					projects = ((SolutionFolder)entry).GetAllProjects ();
				}

				var result = await AllowReload (projects);
				bool allowReload = result.Item1;
				IEnumerable<string> closedDocs = result.Item2;
				
				if (allowReload) {
					using (ProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
						// Root folders never need to reload
						await entry.ParentFolder.ReloadItem (m, entry);
						if (closedDocs != null)
							ReattachDocumentProjects (closedDocs);
					}
					return;
				} else
					entry.NeedsReload = false;
			}
			
			if (entry is SolutionFolder) {
				var ens = new List<SolutionFolderItem> ();
				foreach (SolutionFolderItem ce in ((SolutionFolder)entry).Items)
					ens.Add (ce);
				foreach (SolutionFolderItem ce in ens)
					await OnCheckProject (ce);
			}
		}
		
//		bool AllowReload (IEnumerable projects)
//		{
//			IEnumerable<string> closedDocs;
//			return AllowReload (projects, out closedDocs);
//		}
		
		async Task<Tuple<bool, IEnumerable<string>>> AllowReload (IEnumerable projects)
		{
			IEnumerable<string> closedDocs = null;
			
			if (projects == null)
				return Tuple.Create (true, closedDocs);
			
			List<Document> docs = new List<Document> ();
			foreach (Project p in projects) {
				docs.AddRange (GetOpenDocuments (p, false));
			}
			
			if (docs.Count == 0)
				return Tuple.Create (true, closedDocs);
			
			// Find a common project reload capability
			
			bool hasUnsaved = false;
			bool hasNoFiles = false;
			ProjectReloadCapability prc = ProjectReloadCapability.Full;
			foreach (Document doc in docs) {
				if (doc.IsDirty)
					hasUnsaved = true;
				if (!doc.IsFile)
					hasNoFiles = true;
				var c = doc.ProjectReloadCapability;
				if ((int) c < (int) prc)
					prc = c;
			}

			string msg = null;
			
			switch (prc) {
				case ProjectReloadCapability.None:
					if (hasNoFiles && hasUnsaved)
						msg = GettextCatalog.GetString ("WARNING: Some documents may need to be closed, and unsaved data will be lost. You will be asked to save the unsaved documents.");
					else if (hasNoFiles)
						msg = GettextCatalog.GetString ("WARNING: Some documents may need to be reloaded or closed, and unsaved data will be lost. You will be asked to save the unsaved documents.");
					else if (hasUnsaved)
						msg = GettextCatalog.GetString ("WARNING: Some files may need to be reloaded, and unsaved data will be lost. You will be asked to save the unsaved files.");
					else
						goto case ProjectReloadCapability.UnsavedData;
					break;
					
				case ProjectReloadCapability.UnsavedData:
					msg = GettextCatalog.GetString ("Some files may need to be reloaded, and editing status for those files (such as the undo queue) will be lost.");
					break;
			}
			if (msg != null) {
				if (!MessageService.Confirm (GettextCatalog.GetString ("The project '{0}' has been modified by an external application. Do you want to reload it?", docs[0].Project.Name), msg, AlertButton.Reload))
					return Tuple.Create (true, closedDocs);
			}
			
			List<string> closed = new List<string> ();
			
			foreach (Document doc in docs) {
				if (doc.IsDirty)
					hasUnsaved = true;
				if (doc.ProjectReloadCapability != ProjectReloadCapability.None)
					doc.SetProject (null);
				else {
					FilePath file = doc.IsFile ? doc.FileName : FilePath.Null;
					EventHandler saved = delegate {
						if (doc.IsFile)
							file = doc.FileName;
					};
					doc.Saved += saved;
					try {
						if (!await doc.Close ())
							return Tuple.Create (true, closedDocs);
						else if (!file.IsNullOrEmpty && File.Exists (file))
							closed.Add (file);
					} finally {
						doc.Saved -= saved;
					}
				}
			}
			closedDocs = closed;

			return Tuple.Create (true, closedDocs);
		}
		
		internal static List<Document> GetOpenDocuments (Project project, bool modifiedOnly)
		{
			List<Document> docs = new List<Document> ();
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.Project == project && (!modifiedOnly || doc.IsDirty)) {
					docs.Add (doc);
				}
			}
			return docs;
		}
		
		
#endregion
		
#region Event handling
		
		internal void NotifyItemAdded (WorkspaceItem item)
		{
			MonoDevelop.Ide.TypeSystem.TypeSystemService.Load (item, null).ContinueWith(t => {
				if (t.IsFaulted)
					LoggingService.LogError("Could not load parser database.", t.Exception);
			});
			if (Runtime.IsMainThread)
				NotifyItemAddedGui (item, IsReloading);
			else {
				bool reloading = IsReloading;
				Gtk.Application.Invoke ((o, args) => {
					NotifyItemAddedGui (item, reloading);
				});
			}
		}

		void NotifyItemAddedGui (WorkspaceItem item, bool reloading)
		{
			Workspace ws = item as Workspace;
			if (ws != null) {
				ws.DescendantItemAdded += NotifyDescendantItemAdded;
				ws.DescendantItemRemoved += NotifyDescendantItemRemoved;
			}
			item.ConfigurationsChanged += NotifyConfigurationsChanged;
			
			WorkspaceItemEventArgs args = new WorkspaceItemEventArgs (item);
			NotifyDescendantItemAdded (this, args);
			NotifyConfigurationsChanged (null, args);

			if (Items.Count == 1 && !reloading) {
				IdeApp.Workbench.CurrentLayout = "Solution";
				if (FirstWorkspaceItemOpened != null)
					FirstWorkspaceItemOpened (this, args);
			}
			if (WorkspaceItemOpened != null)
				WorkspaceItemOpened (this, args);
		}
		
		internal void NotifyItemRemoved (WorkspaceItem item)
		{
			if (Runtime.IsMainThread)
				NotifyItemRemovedGui (item, IsReloading);
			else {
				bool reloading = IsReloading;
				Gtk.Application.Invoke ((o, args) => {
					NotifyItemRemovedGui (item, reloading);
				});
			}
		}
		
		internal void NotifyItemRemovedGui (WorkspaceItem item, bool reloading)
		{
			Workspace ws = item as Workspace;
			if (ws != null) {
				ws.DescendantItemAdded -= NotifyDescendantItemAdded;
				ws.DescendantItemRemoved -= NotifyDescendantItemRemoved;
			}
			item.ConfigurationsChanged -= NotifyConfigurationsChanged;
			
			WorkspaceItemEventArgs args = new WorkspaceItemEventArgs (item);
			NotifyConfigurationsChanged (null, args);
			
			if (WorkspaceItemClosed != null)
				WorkspaceItemClosed (this, args);

			bool lastWorkspaceItemClosing = Items.Count == 0 && !reloading;
			if (lastWorkspaceItemClosing) {
				if (LastWorkspaceItemClosed != null)
					LastWorkspaceItemClosed (this, EventArgs.Empty);
			}
			MonoDevelop.Ide.TypeSystem.TypeSystemService.Unload (item);

			NotifyDescendantItemRemoved (this, args);
		}
		
		void SubscribeSolution (Solution sol)
		{
			sol.FileAddedToProject += NotifyFileAddedToProject;
			sol.FileRemovedFromProject += NotifyFileRemovedFromProject;
			sol.FileRenamedInProject += NotifyFileRenamedInProject;
			sol.FileChangedInProject += NotifyFileChangedInProject;
			sol.FilePropertyChangedInProject += NotifyFilePropertyChangedInProject;
			sol.ReferenceAddedToProject += NotifyReferenceAddedToProject;
			sol.ReferenceRemovedFromProject += NotifyReferenceRemovedFromProject;
			sol.SolutionItemAdded += NotifyItemAddedToSolution;
			sol.SolutionItemRemoved += NotifyItemRemovedFromSolution;
			sol.ReloadRequired += SolutionReloadRequired;
			sol.ItemReloadRequired += SolutionItemReloadRequired;
		}
		
		void UnsubscribeSolution (Solution solution)
		{
			solution.FileAddedToProject -= NotifyFileAddedToProject;
			solution.FileRemovedFromProject -= NotifyFileRemovedFromProject;
			solution.FileRenamedInProject -= NotifyFileRenamedInProject;
			solution.FileChangedInProject -= NotifyFileChangedInProject;
			solution.FilePropertyChangedInProject -= NotifyFilePropertyChangedInProject;
			solution.ReferenceAddedToProject -= NotifyReferenceAddedToProject;
			solution.ReferenceRemovedFromProject -= NotifyReferenceRemovedFromProject;
			solution.SolutionItemAdded -= NotifyItemAddedToSolution;
			solution.SolutionItemRemoved -= NotifyItemRemovedFromSolution;
			solution.ReloadRequired -= SolutionReloadRequired;
			solution.ItemReloadRequired -= SolutionItemReloadRequired;
		}
		
		void NotifyConfigurationsChanged (object s, EventArgs a)
		{
			if (ConfigurationsChanged != null)
				ConfigurationsChanged (this, a);
		}
		
		void NotifyFileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject(this, e);
			}
		}
		
		void NotifyFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}

		internal void NotifyFileRenamedInProject (object sender, ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}		
		
		internal void NotifyFileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}		
		
		internal void NotifyFilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
			}
		}		
		
		internal void NotifyReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}
		
		internal void NotifyReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		void NotifyItemAddedToSolution (object sender, SolutionItemChangeEventArgs args)
		{
			// Delay the notification of this event to ensure that the new project is properly
			// registered in the parser database when it is fired
			
			Gtk.Application.Invoke ((o2, a2) => {
				if (ItemAddedToSolution != null)
					ItemAddedToSolution (sender, args);
			});
		}
		
		void NotifyItemRemovedFromSolution (object sender, SolutionItemChangeEventArgs args)
		{
			NotifyItemRemovedFromSolutionRec (sender, args.SolutionItem, args.Solution, args);
		}
		
		void NotifyItemRemovedFromSolutionRec (object sender, SolutionFolderItem e, Solution sol, SolutionItemChangeEventArgs originalArgs)
		{
			if (e == IdeApp.ProjectOperations.CurrentSelectedSolutionItem)
				IdeApp.ProjectOperations.CurrentSelectedSolutionItem = null;
				
			if (e is SolutionFolder) {
				foreach (SolutionFolderItem ce in ((SolutionFolder)e).Items)
					NotifyItemRemovedFromSolutionRec (sender, ce, sol, null);
			}

			// For the root item send the original args, since they contain reload information

			if (ItemRemovedFromSolution != null)
				ItemRemovedFromSolution (sender, originalArgs ?? new SolutionItemChangeEventArgs (e, sol, false));
		}
		
		void NotifyDescendantItemAdded (object s, WorkspaceItemEventArgs args)
		{
			// If a top level item has been moved to a child item, remove it from
			// the top
			if (s != this && Items.Contains (args.Item))
				Items.Remove (args.Item);
			foreach (WorkspaceItem item in args.Item.GetAllItems<WorkspaceItem> ()) {
				if (item is Solution)
					SubscribeSolution ((Solution)item);
				OnItemLoaded (item);
			}
		}
		
		void NotifyDescendantItemRemoved (object s, WorkspaceItemEventArgs args)
		{
			foreach (WorkspaceItem item in args.Item.GetAllItems<WorkspaceItem> ()) {
				OnItemUnloaded (item);
				if (item is Solution)
					UnsubscribeSolution ((Solution)item);
			}
		}
		
		void OnItemLoaded (WorkspaceItem item)
		{
			try {
				if (WorkspaceItemLoaded != null)
					WorkspaceItemLoaded (this, new WorkspaceItemEventArgs (item));
				if (item is Solution && SolutionLoaded != null)
					SolutionLoaded (this, new SolutionEventArgs ((Solution)item));
			} catch (Exception ex) {
				LoggingService.LogError ("Error in SolutionOpened event.", ex);
			}
		}
		
		void OnItemUnloaded (WorkspaceItem item)
		{
			try {
				if (WorkspaceItemUnloaded != null)
					WorkspaceItemUnloaded (this, new WorkspaceItemEventArgs (item));
				if (item is Solution && SolutionUnloaded != null)
					SolutionUnloaded (this, new SolutionEventArgs ((Solution)item));
			} catch (Exception ex) {
				LoggingService.LogError ("Error in SolutionClosed event.", ex);
			}
		}
		
		void CheckFileRename(object sender, FileCopyEventArgs args)
		{
			foreach (Solution sol in GetAllSolutions ()) {
				foreach (FileCopyEventInfo e in args)
					sol.RootFolder.RenameFileInProjects (e.SourceFile, e.TargetFile);
			}
		}
		
#endregion

#region Event declaration
		
		/// <summary>
		/// Fired when a file is removed from a project.
		/// </summary>
		public event ProjectFileEventHandler FileRemovedFromProject;
		
		/// <summary>
		/// Fired when a file is added to a project
		/// </summary>
		public event ProjectFileEventHandler FileAddedToProject;
		
		/// <summary>
		/// Fired when a file belonging to a project is modified.
		/// </summary>
		/// <remarks>
		/// If the file belongs to several projects, the event will be fired for each project
		/// </remarks>
		public event ProjectFileEventHandler FileChangedInProject;
		
		/// <summary>
		/// Fired when a property of a project file is modified
		/// </summary>
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		
		/// <summary>
		/// Fired when a project file is renamed
		/// </summary>
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		
		/// <summary>
		/// Fired when a solution is loaded in the workbench
		/// </summary>
		/// <remarks>
		/// This event is fired recursively for every solution
		/// opened in the IDE. For example, if the user opens a workspace
		/// which contains two solutions, this event will be fired once
		/// for each solution.
		/// </remarks>
		public event EventHandler<SolutionEventArgs> SolutionLoaded;
		
		/// <summary>
		/// Fired when a solution loaded in the workbench is unloaded
		/// </summary>
		public event EventHandler<SolutionEventArgs> SolutionUnloaded;
		
		/// <summary>
		/// Fired when a workspace item (a solution or workspace) is opened and there
		/// is no other item already open
		/// </summary>
		public event EventHandler<WorkspaceItemEventArgs> FirstWorkspaceItemOpened;

		/// <summary>
		/// Fired when a workspace item (a solution or workspace) is fully restored and there
		/// is no other item already open 
		/// </summary>
		internal event EventHandler<WorkspaceItemEventArgs> FirstWorkspaceItemRestored;
		
		/// <summary>
		/// Fired a workspace item loaded in the IDE is closed and there are no other
		/// workspace items opened.
		/// </summary>
		public event EventHandler LastWorkspaceItemClosed;
		
		/// <summary>
		/// Fired when a workspace item (a solution or workspace) is loaded.
		/// </summary>
		/// <remarks>
		/// This event is fired recursively for every solution and workspace 
		/// opened in the IDE. For example, if the user opens a workspace
		/// which contains two solutions, this event will be fired three times: 
		/// once for the workspace, and once for each solution.
		/// </remarks>
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemLoaded;

		/// <summary>
		/// Fired when a workspace item (a solution or workspace) is unloaded
		/// </summary>
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemUnloaded;
		
		/// <summary>
		/// Fired a workspace item (a solution or workspace) is opened in the IDE
		/// </summary>
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemOpened;
		
		/// <summary>
		/// Fired when a workspace item (a solution or workspace) is closed in the IDE
		/// </summary>
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemClosed;
		
		/// <summary>
		/// Fired when user preferences for the active solution are being stored
		/// </summary>
		/// <remarks>
		/// Add-ins can subscribe to this event to store custom user preferences
		/// for a solution. Preferences can be stored in the PropertyBag provided
		/// in the event arguments object.
		/// </remarks>
		public event EventHandler<UserPreferencesEventArgs> StoringUserPreferences;
		
		/// <summary>
		/// Fired when user preferences for a solution are being loaded
		/// </summary>
		/// <remarks>
		/// Add-ins can subscribe to this event to load preferences previously
		/// stored in the StoringUserPreferences event.
		/// </remarks>
		public event AsyncEventHandler<UserPreferencesEventArgs> LoadingUserPreferences;
		
		/// <summary>
		/// Fired when an item (a project, solution or workspace) is going to be unloaded.
		/// </summary>
		/// <remarks>
		/// This event is fired before unloading the item, and the unload operation can
		/// be cancelled by setting the Cancel property of the ItemUnloadingEventArgs
		/// object to True.
		/// </remarks>
		public event EventHandler<ItemUnloadingEventArgs> ItemUnloading;
		
		/// <summary>
		/// Fired when an assembly reference is added to a .NET project
		/// </summary>
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		
		/// <summary>
		/// Fired when an assembly reference is added to a .NET project
		/// </summary>
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		
		/// <summary>
		/// Fired just before a project is added to a solution
		/// </summary>
		public event SolutionItemChangeEventHandler ItemAddedToSolution;
		
		/// <summary>
		/// Fired after a project is removed from a solution
		/// </summary>
		public event SolutionItemChangeEventHandler ItemRemovedFromSolution;
		
		/// <summary>
		/// Fired when the active solution configuration has changed
		/// </summary>
		public event EventHandler ActiveConfigurationChanged;

		/// <summary>
		/// Fired when the active execution target has changed
		/// </summary>
		public event EventHandler ActiveExecutionTargetChanged;
		
		/// <summary>
		/// Fired when the list of solution configurations has changed
		/// </summary>
		public event EventHandler ConfigurationsChanged;
		
		/// <summary>
		/// Fired when the list of available .NET runtimes has changed
		/// </summary>
		public event EventHandler RuntimesChanged {
			add { Runtime.SystemAssemblyService.RuntimesChanged += value; }
			remove { Runtime.SystemAssemblyService.RuntimesChanged -= value; }
		}
		
		/// <summary>
		/// Fired when the active .NET runtime has changed
		/// </summary>
		public event EventHandler ActiveRuntimeChanged {
			add { Runtime.SystemAssemblyService.DefaultRuntimeChanged += value; }
			remove { Runtime.SystemAssemblyService.DefaultRuntimeChanged -= value; }
		}
#endregion
	}
	
	public class RootWorkspaceItemCollection: ItemCollection<WorkspaceItem>
	{
		RootWorkspace parent;
		
		internal RootWorkspaceItemCollection (RootWorkspace parent)
		{
			this.parent = parent;
		}


		protected override void OnItemsRemoved (IEnumerable<WorkspaceItem> items)
		{
			base.OnItemsRemoved (items);
			if (parent != null) {
				foreach (WorkspaceItem it in items)
					parent.NotifyItemRemoved (it);
			}
		}

		protected override void OnItemsAdded (IEnumerable<WorkspaceItem> items)
		{
			base.OnItemsAdded (items);
			if (parent != null) {
				foreach (var item in items)
					parent.NotifyItemAdded (item);
			}
		}
	}
	
	public class UserPreferencesEventArgs: WorkspaceItemEventArgs
	{
		PropertyBag properties;
		
		public PropertyBag Properties {
			get {
				return properties;
			}
		}
		
		public UserPreferencesEventArgs (WorkspaceItem item, PropertyBag properties): base (item)
		{
			this.properties = properties;
		}
	}
	
	[DataItem ("Workspace")]
	class WorkspaceUserData
	{
		[ItemProperty]
		public string ActiveConfiguration;
		[ItemProperty]
		public string ActiveRuntime;
	}
	
	public class ItemUnloadingEventArgs: EventArgs
	{
		WorkspaceObject item;
		
		public bool Cancel { get; set; }
		
		public WorkspaceObject Item {
			get {
				return item;
			}
		}
		
		public ItemUnloadingEventArgs (WorkspaceObject item)
		{
			this.item = item;
		}
	}
	
	public class FileStatusTracker: IDisposable
	{
		class FileData
		{
			public FileData (FilePath file, DateTime time)
			{
				this.File = file;
				this.Time = time;
			}
			
			public FilePath File;
			public DateTime Time;
		}
		
		List<FileData> fileStatus = new List<FileData> ();
		
		internal void AddFiles (IEnumerable<FilePath> files)
		{
			foreach (var file in files) {
				try {
					FileInfo fi = new FileInfo (file);
					FileData fd = new FileData (file, fi.Exists ? fi.LastWriteTime : DateTime.MinValue);
					fileStatus.Add (fd);
				} catch {
					// Ignore
				}
			}
		}
		
		public void NotifyChanges ()
		{
			List<FilePath> modified = new List<FilePath> ();
			foreach (FileData fd in fileStatus) {
				try {
					FileInfo fi = new FileInfo (fd.File);
					if (fi.Exists) {
						DateTime wt = fi.LastWriteTime;
						if (wt != fd.Time) {
							modified.Add (fd.File);
							fd.Time = wt;
						}
					} else if (fd.Time != DateTime.MinValue) {
						FileService.NotifyFileRemoved (fd.File);
						fd.Time = DateTime.MinValue;
					}
				} catch {
					// Ignore
				}
			}
			if (modified.Count > 0)
				FileService.NotifyFilesChanged (modified);
		}
		
		public void Dispose ()
		{
			NotifyChanges ();
		}
	}


	class OpenWorkspaceItemMetadata : CounterMetadata
	{
		public enum OpenReason
		{
			Unknown,
			OpenProject,
			OpenSolution,
			CreateSolution
		};

		public OpenWorkspaceItemMetadata ()
		{
		}

		public bool OnStartup {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public bool LoadSucceed {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public OpenReason Reason {
			get {
				var rs = GetProperty<string> ();
				if (Enum.TryParse<OpenReason> (rs, out var result)) {
					return result;
				}

				return OpenReason.Unknown;
			}

			set => SetProperty (value.ToString ());
		}

		public bool IsSolution {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public int TotalProjectCount {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}
	}
}

namespace Mono.Profiler {
	public class RuntimeControls {
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void TakeHeapSnapshot ();
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void EnableProfiler ();
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public static extern void DisableProfiler ();
	}
}
