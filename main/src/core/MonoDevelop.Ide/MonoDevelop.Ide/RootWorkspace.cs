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

namespace MonoDevelop.Ide
{
	public class RootWorkspace: IBuildTarget, IWorkspaceObject
	{
		WorkspaceItemCollection items;
//		IParserDatabase parserDatabase;
		string activeConfiguration;
		bool useDefaultRuntime;
		string preferredActiveExecutionTarget;

		ProjectFileEventHandler fileAddedToProjectHandler;
		ProjectFileEventHandler fileRemovedFromProjectHandler;
		ProjectFileRenamedEventHandler fileRenamedInProjectHandler;
		ProjectFileEventHandler fileChangedInProjectHandler;
		ProjectFileEventHandler filePropertyChangedInProjectHandler;
		ProjectReferenceEventHandler referenceAddedToProjectHandler;
		ProjectReferenceEventHandler referenceRemovedFromProjectHandler;
		SolutionItemChangeEventHandler itemAddedToSolutionHandler;
		SolutionItemChangeEventHandler itemRemovedFromSolutionHandler;
		EventHandler<WorkspaceItemChangeEventArgs> descendantItemAddedHandler;
		EventHandler<WorkspaceItemChangeEventArgs> descendantItemRemovedHandler;
		EventHandler configurationsChanged;
		
		internal RootWorkspace ()
		{
			fileAddedToProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFileAddedToProject));
			fileRemovedFromProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFileRemovedFromProject));
			fileRenamedInProjectHandler = (ProjectFileRenamedEventHandler) DispatchService.GuiDispatch (new ProjectFileRenamedEventHandler (NotifyFileRenamedInProject));
			fileChangedInProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFileChangedInProject));
			filePropertyChangedInProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFilePropertyChangedInProject));
			referenceAddedToProjectHandler = (ProjectReferenceEventHandler) DispatchService.GuiDispatch (new ProjectReferenceEventHandler (NotifyReferenceAddedToProject));
			referenceRemovedFromProjectHandler = (ProjectReferenceEventHandler) DispatchService.GuiDispatch (new ProjectReferenceEventHandler (NotifyReferenceRemovedFromProject));
		
			itemAddedToSolutionHandler = (SolutionItemChangeEventHandler) DispatchService.GuiDispatch (new SolutionItemChangeEventHandler (NotifyItemAddedToSolution));
			itemRemovedFromSolutionHandler = (SolutionItemChangeEventHandler) DispatchService.GuiDispatch (new SolutionItemChangeEventHandler (NotifyItemRemovedFromSolution));
			
			descendantItemAddedHandler = (EventHandler<WorkspaceItemChangeEventArgs>) DispatchService.GuiDispatch (new EventHandler<WorkspaceItemChangeEventArgs> (NotifyDescendantItemAdded));
			descendantItemRemovedHandler = (EventHandler<WorkspaceItemChangeEventArgs>) DispatchService.GuiDispatch (new EventHandler<WorkspaceItemChangeEventArgs> (NotifyDescendantItemRemoved));
			configurationsChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (NotifyConfigurationsChanged));
			
			FileService.FileRenamed += (EventHandler<FileCopyEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileCopyEventArgs> (CheckFileRename));
			
			// Set the initial active runtime
			UseDefaultRuntime = true;
			IdeApp.Preferences.DefaultTargetRuntimeChanged += delegate {
				// If the default runtime changes and current active is default, update it
				if (UseDefaultRuntime) {
					Runtime.SystemAssemblyService.DefaultRuntime = IdeApp.Preferences.DefaultTargetRuntime;
					useDefaultRuntime = true;
				}
			};
			
			FileService.FileChanged += (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs> (CheckWorkspaceItems));;
		}
		
		public WorkspaceItemCollection Items {
			get {
				if (items == null)
					items = new RootWorkspaceItemCollection (this);
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

		public ExecutionTarget ActiveExecutionTarget { get; set; }

		internal string PreferredActiveExecutionTarget {
			get { return ActiveExecutionTarget != null ? ActiveExecutionTarget.Id : preferredActiveExecutionTarget; }
			set { preferredActiveExecutionTarget = value; }
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
		
		IDictionary IExtendedDataItem.ExtendedProperties {
			get {
				throw new NotSupportedException ("Root namespace can't have extended properties.");
			}
		}

		string IWorkspaceObject.Name {
			get {
				return "MonoDevelop Workspace";
			}
			set {
				throw new NotSupportedException ("Can't change the name of the root workspace.");
			}
		}

		public FilePath BaseDirectory {
			get {
				return IdeApp.ProjectOperations.ProjectsDefaultPath;
			}
		}
		
		FilePath IWorkspaceObject.BaseDirectory {
			get {
				return BaseDirectory;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		FilePath IWorkspaceObject.ItemDirectory {
			get {
				return BaseDirectory;
			}
		}
		
#region Model queries
		
		public SolutionEntityItem FindSolutionItem (string fileName)
		{
			foreach (WorkspaceItem it in Items) {
				SolutionEntityItem si = it.FindSolutionItem (fileName);
				if (si != null)
					return si;
			}
			return null;
		}
		
		public ReadOnlyCollection<SolutionItem> GetAllSolutionItems ()
		{
			return GetAllSolutionItems<SolutionItem> ();
		}
		
		public virtual ReadOnlyCollection<T> GetAllSolutionItems<T> () where T: SolutionItem
		{
			List<T> list = new List<T> ();
			foreach (WorkspaceItem it in Items) {
				list.AddRange (it.GetAllSolutionItems<T> ());
			}
			return list.AsReadOnly ();
		}
		
		public ReadOnlyCollection<Project> GetAllProjects ()
		{
			return GetAllSolutionItems<Project> ();
		}
		
		public ReadOnlyCollection<Solution> GetAllSolutions ()
		{
			return GetAllItems<Solution> ();
		}
			
		public ReadOnlyCollection<T> GetAllItems<T> () where T:WorkspaceItem
		{
			List<T> list = new List<T> ();
			foreach (WorkspaceItem it in Items)
				GetAllItems<T> (list, it);
			return list.AsReadOnly ();
		}
		
		void GetAllItems<T> (List<T> list, WorkspaceItem item) where T: WorkspaceItem
		{
			if (item is T)
				list.Add ((T) item);
			
			if (item is Workspace) {
				foreach (WorkspaceItem citem in ((Workspace)item).Items)
					GetAllItems<T> (list, citem);
			}
		}

		public Project GetProjectContainingFile (string fileName)
		{
			foreach (WorkspaceItem it in Items) {
				Project p = it.GetProjectContainingFile (fileName);
				if (p != null)
					return p;
			}
			return null;
		}
		
#endregion
		
#region Build and run operations
		
		public void Save ()
		{
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Workspace saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public IAsyncOperation Build ()
		{
			return IdeApp.ProjectOperations.Build (this);
		}

		public void Clean ()
		{
			IdeApp.ProjectOperations.Clean (this);
		}

		public IAsyncOperation Execute ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				return IdeApp.ProjectOperations.Execute (IdeApp.ProjectOperations.CurrentSelectedSolution);
			else {
				MessageService.ShowError (GettextCatalog.GetString ("No solution has been selected"), GettextCatalog.GetString ("The solution to be executed must be selected in the solution pad."));
				return null;
			}
		}

		public bool CanExecute ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				return IdeApp.ProjectOperations.CanExecute (IdeApp.ProjectOperations.CurrentSelectedSolution);
			else {
				return false;
			}
		}

		bool IBuildTarget.CanExecute (ExecutionContext context, ConfigurationSelector configuration)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null)
				return IdeApp.ProjectOperations.CurrentSelectedSolution.CanExecute (context, configuration);
			else {
				return false;
			}
		}
		
		public void Dispose ()
		{
		}

		public void Save (IProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Saving Workspace..."), Items.Count);
			List<WorkspaceItem> items = new List<WorkspaceItem> (Items);
			foreach (WorkspaceItem it in items) {
				it.Save (monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}
		
		BuildResult IBuildTarget.RunTarget (IProgressMonitor monitor, string target, ConfigurationSelector configuration)
		{
			BuildResult result = null;
			List<WorkspaceItem> items = new List<WorkspaceItem> (Items);
			foreach (WorkspaceItem it in items) {
				BuildResult res = it.RunTarget (monitor, target, configuration);
				if (res != null) {
					if (result == null)
						result = new BuildResult ();
					result.Append (res);
				}
			}
			return result;
		}

		public void Execute (MonoDevelop.Core.IProgressMonitor monitor, ExecutionContext context, ConfigurationSelector configuration)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol == null) {
				ReadOnlyCollection<Solution> sols = GetAllSolutions ();
				if (sols.Count > 0)
					sol = sols [0];
			}
			if (sol != null)
				sol.Execute (monitor, context, configuration);
			else
				throw new UserException (GettextCatalog.GetString ("No solution has been selected."));
		}
		
		public bool NeedsBuilding ()
		{
			return NeedsBuilding (IdeApp.Workspace.ActiveConfiguration) || IsDirtyFileInCombine;
		}

		public bool NeedsBuilding (ConfigurationSelector configuration)
		{
			foreach (WorkspaceItem it in Items) {
				if (it.NeedsBuilding (configuration))
					return true;
			}
			return false;
		}

		public void SetNeedsBuilding (bool needsBuilding, ConfigurationSelector configuration)
		{
			foreach (WorkspaceItem it in Items)
				it.SetNeedsBuilding (needsBuilding, configuration);
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
		
		public bool Close ()
		{
			return Close (true);
		}

		public bool Close (bool saveWorkspacePreferencies)
		{
			return Close (saveWorkspacePreferencies, true);
		}
		
		internal bool Close (bool saveWorkspacePreferencies, bool closeProjectFiles)
		{
			if (Items.Count > 0) {
				
				// Request permission for unloading the items
				foreach (WorkspaceItem it in new List<WorkspaceItem> (Items)) {
					if (!RequestItemUnload (it))
						return false;
				}
				
				if (saveWorkspacePreferencies)
					SavePreferences ();

				if (closeProjectFiles) {
					foreach (Document doc in IdeApp.Workbench.Documents.ToArray ()) {
						if (!doc.Close ())
							return false;
					}
				}
				
				foreach (WorkspaceItem it in new List<WorkspaceItem> (Items)) {
					try {
						Items.Remove (it);
						it.Dispose ();
					} catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("Could not close solution '{0}'.", it.Name));
					}
				}
			}
			return true;
		}
		
		public void CloseWorkspaceItem (WorkspaceItem item, bool closeItemFiles = true)
		{
			if (!Items.Contains (item))
				throw new InvalidOperationException ("Only top level items can be closed.");

			if (Items.Count == 1 && closeItemFiles) {
				// There is only one item, close the whole workspace
				Close (true, closeItemFiles);
				return;
			}

			if (RequestItemUnload (item)) {
				if (closeItemFiles) {
					var projects = item.GetAllProjects ();
					foreach (Document doc in IdeApp.Workbench.Documents.Where (d => d.Project != null && projects.Contains (d.Project)).ToArray ()) {
						if (!doc.Close ())
							return;
					}
				}
				Items.Remove (item);
			}
		}
		
		public bool RequestItemUnload (IBuildTarget item)
		{
			if (ItemUnloading != null) {
				try {
					ItemUnloadingEventArgs args = new ItemUnloadingEventArgs (item);
					ItemUnloading (this, args);
					return !args.Cancel;
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in ItemUnloading.", ex);
				}
			}
			return true;
		}
		
		public IAsyncOperation OpenWorkspaceItem (string filename)
		{
			return OpenWorkspaceItem (filename, true);
		}
		
		public IAsyncOperation OpenWorkspaceItem (string filename, bool closeCurrent)
		{
			return OpenWorkspaceItem (filename, closeCurrent, true);
		}
		
		public IAsyncOperation OpenWorkspaceItem (string filename, bool closeCurrent, bool loadPreferences)
		{
			if (closeCurrent) {
				if (!Close ())
					return MonoDevelop.Core.ProgressMonitoring.NullAsyncOperation.Failure;
			}

			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;

			var monitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true);
			bool reloading = IsReloading;

			DispatchService.BackgroundDispatch (delegate {
				BackgroundLoadWorkspace (monitor, filename, loadPreferences, reloading);
			});
			return monitor.AsyncOperation;
		}
		
		void ReattachDocumentProjects (IEnumerable<string> closedDocs)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.Project == null && doc.IsFile) {
					Project p = GetProjectContainingFile (doc.FileName);
					if (p != null)
						doc.SetProject (p);
				}
			}
			if (closedDocs != null) {
				foreach (string doc in closedDocs) {
					IdeApp.Workbench.OpenDocument (doc, false);
				}
			}
		}
		
		void BackgroundLoadWorkspace (IProgressMonitor monitor, string filename, bool loadPreferences, bool reloading)
		{
			WorkspaceItem item = null;
			ITimeTracker timer = Counters.OpenWorkspaceItemTimer.BeginTiming ();
			
			try {
				if (reloading)
					SetReloading (true);

				if (!File.Exists (filename)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", filename), null);
					monitor.Dispose ();
					return;
				}

				for (int i = 0; i < Items.Count; i++) {
					if (Items[i].FileName == filename) {
						IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem = Items[i];
						monitor.Dispose ();
						return;
					}
				}
				
				if (!Services.ProjectService.IsWorkspaceItemFile (filename)) {
					if (!Services.ProjectService.IsSolutionItemFile (filename)) {
						monitor.ReportError (GettextCatalog.GetString ("File is not a project or solution: {0}", filename), null);
						monitor.Dispose ();
						return;
					}
					
					// It is a project, not a solution. Try to create a dummy solution and add the project to it
					
					timer.Trace ("Getting wrapper solution");
					item = IdeApp.Services.ProjectService.GetWrapperSolution (monitor, filename);
				}
				
				if (item == null) {
					timer.Trace ("Reading item");
					item = Services.ProjectService.ReadWorkspaceItem (monitor, filename);
					if (monitor.IsCancelRequested) {
						monitor.Dispose ();
						return;
					}
				}

				timer.Trace ("Registering to recent list");
				DesktopService.RecentFiles.AddProject (item.FileName, item.Name);
				
				timer.Trace ("Adding to items list");
				Items.Add (item);
				
				timer.Trace ("Searching for new files");
				SearchForNewFiles ();

			} catch (Exception ex) {
				monitor.ReportError ("Load operation failed.", ex);
				
				// Don't use 'finally' to dispose the monitor, since it has to be disposed later
				monitor.Dispose ();
				timer.End ();
				return;
			} finally {
				if (reloading)
					SetReloading (false);
			}
			
			Gtk.Application.Invoke (delegate {
				using (monitor) {
					try {
						if (IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem == null)
							IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem = GetAllSolutions ().FirstOrDefault ();
						if (Items.Count == 1 && loadPreferences) {
							timer.Trace ("Restoring workspace preferences");
							RestoreWorkspacePreferences (item);
						}
						timer.Trace ("Reattaching documents");
						ReattachDocumentProjects (null);
						monitor.ReportSuccess (GettextCatalog.GetString ("Solution loaded."));
					} finally {
						timer.End ();
					}
				}
			});
		}
		
		void SearchForNewFiles ()
		{
			foreach (Project p in GetAllProjects ()) {
				if (p.NewFileSearch != NewFileSearch.None)
					SearchNewFiles (p);
			}
		}
		
		void SearchNewFiles (Project project)
		{
			var newFiles = new List<string> ();
			string[] collection = Directory.GetFiles (project.BaseDirectory, "*", SearchOption.AllDirectories);
			
			var projectFileNames = new HashSet<string> ();
			foreach (var file in project.GetItemFiles (true))
				projectFileNames.Add (file);
			
			//also ignore files that would conflict with links
			foreach (var f in project.Files)
				projectFileNames.Add (f.ProjectVirtualPath.ToAbsolute (project.BaseDirectory));

			foreach (string sfile in collection) {
				if (projectFileNames.Contains (Path.GetFullPath (sfile)))
					continue;
				if (IdeApp.Services.ProjectService.IsSolutionItemFile (sfile) || IdeApp.Services.ProjectService.IsWorkspaceItemFile (sfile))
					continue;
				if (IgnoreFileInSearch (sfile))
					continue;
				newFiles.Add (sfile);
			}
			
			if (newFiles.Count == 0)
				return;
			
			if (project.NewFileSearch == NewFileSearch.OnLoadAutoInsert) {
				foreach (string file in newFiles) {
					project.AddFile (file);
				}
				
				return;
			}
			
			DispatchService.GuiDispatch (delegate {
				var dialog = new IncludeNewFilesDialog (
					GettextCatalog.GetString ("Found new files in {0}", project.Name),
					project.BaseDirectory
				);
				dialog.AddFiles (newFiles);
				if (MessageService.ShowCustomDialog (dialog) != (int)Gtk.ResponseType.Ok)
					return;
				
				foreach (var file in dialog.IgnoredFiles) {
					var projectFile = project.AddFile (file, BuildAction.None);
					if (projectFile != null)
						projectFile.Visible = false;
				}
				foreach (var file in dialog.SelectedFiles) {
					project.AddFile (file);
				}
				IdeApp.ProjectOperations.Save (project);
			});
		}
		
		bool IgnoreFileInSearch (string sfile)
		{
			string extension = Path.GetExtension (sfile).ToUpper();
			string file = Path.GetFileName (sfile);
			
			if (file.StartsWith (".") || file.EndsWith ("~"))
				return true;
			
			string[] ignoredExtensions = new string [] {
				".SCC", ".DLL", ".PDB", ".MDB", ".EXE", ".SLN", ".CMBX", ".PRJX",
				".SWP", ".MDSX", ".MDS", ".MDP", ".PIDB", ".PIDB-JOURNAL",
			};
			if (ignoredExtensions.Contains (extension))
				return true;
			
			string directory = Path.GetDirectoryName (sfile);
			if (directory.IndexOf (".svn") != -1 || directory.IndexOf (".git") != -1 || directory.IndexOf ("CVS") != -1)
				return true;
			
			if (directory.IndexOf (Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) != -1
				|| directory.IndexOf (Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) != -1)
				return true;
			
			if (file.EndsWith ("make.sh") || file.StartsWith ("Makefile") || directory.EndsWith ("ProjectDocumentation"))
				return true;
			
			return false;			
		}
		
		void RestoreWorkspacePreferences (WorkspaceItem item)
		{
			// Restore local configuration data
			
			try {
				WorkspaceUserData data = item.UserProperties.GetValue<WorkspaceUserData> ("MonoDevelop.Ide.Workspace");
				if (data != null) {
					PreferredActiveExecutionTarget = data.PreferredExecutionTarget;
					ActiveExecutionTarget = null;

					if (GetConfigurations ().Contains (data.ActiveConfiguration))
						activeConfiguration = data.ActiveConfiguration;
					else
						activeConfiguration = GetBestDefaultConfiguration ();

					if (string.IsNullOrEmpty (data.ActiveRuntime))
						UseDefaultRuntime = true;
					else {
						TargetRuntime tr = Runtime.SystemAssemblyService.GetTargetRuntime (data.ActiveRuntime);
						if (tr != null)
							ActiveRuntime = tr;
						else
							UseDefaultRuntime = true;
					}
					OnActiveConfigurationChanged ();
				}
				else {
					ActiveConfigurationId = GetBestDefaultConfiguration ();
				}
			}
			catch (Exception ex) {
				LoggingService.LogError ("Exception while loading user solution preferences.", ex);
			}
			
			// Allow add-ins to restore preferences
			
			if (LoadingUserPreferences != null) {
				UserPreferencesEventArgs args = new UserPreferencesEventArgs (item, item.UserProperties);
				try {
					LoadingUserPreferences (this, args);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in LoadingUserPreferences.", ex);
				}
			}
		}
		
		string GetBestDefaultConfiguration ()
		{
			// 'Debug' is always the best candidate. If there is no debug, pick
			// the configuration with the highest number of built projects.
			int nbuilds = 0;
			string bestConfig = null;
			foreach (Solution sol in GetAllSolutions ()) {
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
		
		public void SavePreferences (WorkspaceItem item)
		{
			// Local configuration info
			
			WorkspaceUserData data = new WorkspaceUserData ();
			data.ActiveConfiguration = ActiveConfigurationId;
			data.ActiveRuntime = UseDefaultRuntime ? null : ActiveRuntime.Id;
			if (ActiveExecutionTarget != null)
				data.PreferredExecutionTarget = ActiveExecutionTarget.Id;
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
			
			item.SaveUserProperties ();
		}
		
		public FileStatusTracker GetFileStatusTracker ()
		{
			FileStatusTracker fs = new FileStatusTracker ();
			fs.AddFiles (GetKnownFiles ());
			return fs;
		}
		
		IEnumerable<FilePath> GetKnownFiles ()
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

		void CheckWorkspaceItems (object sender, FileEventArgs args)
		{
			List<FilePath> files = args.Select (e => e.FileName.CanonicalPath).ToList ();
			foreach (Solution s in GetAllSolutions ().Where (sol => files.Contains (sol.FileName.CanonicalPath)))
				OnCheckWorkspaceItem (s);
			
			foreach (Project p in GetAllProjects ().Where (proj => files.Contains (proj.FileName.CanonicalPath)))
				OnCheckProject (p);
		}
		
		bool OnRunProjectChecks ()
		{
			// If any project has been modified, reload it
			foreach (WorkspaceItem it in new List<WorkspaceItem> (Items))
				OnCheckWorkspaceItem (it);
			return true;
		}

		void OnCheckWorkspaceItem (WorkspaceItem item)
		{
			if (item.NeedsReload) {
				IEnumerable<string> closedDocs;
				if (AllowReload (item.GetAllProjects (), out closedDocs)) {
					if (item.ParentWorkspace == null) {
						string file = item.FileName;
						try {
							SetReloading (true);
							SavePreferences ();
							CloseWorkspaceItem (item, false);
							OpenWorkspaceItem (file, false, false);
						} finally {
							SetReloading (false);
						}
					}
					else {
						using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true)) {
							item.ParentWorkspace.ReloadItem (m, item);
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
					OnCheckWorkspaceItem (it);
			}
			else if (item is Solution) {
				Solution sol = (Solution) item;
				OnCheckProject (sol.RootFolder);
			}
		}
		
		void OnCheckProject (SolutionItem entry)
		{
			if (entry.NeedsReload) {
				IEnumerable projects = null;
				if (entry is Project) {
					projects = new Project [] { (Project) entry };
				} else if (entry is SolutionFolder) {
					projects = ((SolutionFolder)entry).GetAllProjects ();
				}
				
				IEnumerable<string> closedDocs;
				
				if (AllowReload (projects, out closedDocs)) {
					using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
						// Root folders never need to reload
						entry.ParentFolder.ReloadItem (m, entry);
						ReattachDocumentProjects (closedDocs);
					}
					return;
				} else
					entry.NeedsReload = false;
			}
			
			if (entry is SolutionFolder) {
				ArrayList ens = new ArrayList ();
				foreach (SolutionItem ce in ((SolutionFolder)entry).Items)
					ens.Add (ce);
				foreach (SolutionItem ce in ens)
					OnCheckProject (ce);
			}
		}
		
//		bool AllowReload (IEnumerable projects)
//		{
//			IEnumerable<string> closedDocs;
//			return AllowReload (projects, out closedDocs);
//		}
		
		bool AllowReload (IEnumerable projects, out IEnumerable<string> closedDocs)
		{
			closedDocs = null;
			
			if (projects == null)
				return true;
			
			List<Document> docs = new List<Document> ();
			foreach (Project p in projects) {
				docs.AddRange (GetOpenDocuments (p, false));
			}
			
			if (docs.Count == 0)
				return true;
			
			// Find a common project reload capability
			
			bool hasUnsaved = false;
			bool hasNoFiles = false;
			ProjectReloadCapability prc = ProjectReloadCapability.Full;
			foreach (Document doc in docs) {
				if (doc.IsDirty)
					hasUnsaved = true;
				if (!doc.IsFile)
					hasNoFiles = true;
				ISupportsProjectReload pr = doc.GetContent<ISupportsProjectReload> ();
				if (pr != null) {
					ProjectReloadCapability c = pr.ProjectReloadCapability;
					if ((int) c < (int) prc)
						prc = c;
				}
				else
					prc = ProjectReloadCapability.None;
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
					return false;
			}
			
			List<string> closed = new List<string> ();
			
			foreach (Document doc in docs) {
				if (doc.IsDirty)
					hasUnsaved = true;
				ISupportsProjectReload pr = doc.GetContent<ISupportsProjectReload> ();
				if (pr != null)
					doc.SetProject (null);
				else {
					FilePath file = doc.IsFile ? doc.FileName : FilePath.Null;
					EventHandler saved = delegate {
						if (doc.IsFile)
							file = doc.FileName;
					};
					doc.Saved += saved;
					try {
						if (!doc.Close ())
							return false;
						else if (!file.IsNullOrEmpty && File.Exists (file))
							closed.Add (file);
					} finally {
						doc.Saved -= saved;
					}
				}
			}
			closedDocs = closed;

			return true;
		}
		
		internal List<Document> GetOpenDocuments (Project project, bool modifiedOnly)
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
			if (DispatchService.IsGuiThread)
				NotifyItemAddedGui (item, IsReloading);
			else {
				bool reloading = IsReloading;
				Gtk.Application.Invoke (delegate {
					NotifyItemAddedGui (item, reloading);
				});
			}
		}
		
		void NotifyItemAddedGui (WorkspaceItem item, bool reloading)
		{
			try {
//				Mono.Profiler.RuntimeControls.EnableProfiler ();
				MonoDevelop.Ide.TypeSystem.TypeSystemService.Load (item);
//				Mono.Profiler.RuntimeControls.DisableProfiler ();
//				Console.WriteLine ("PARSE LOAD: " + (DateTime.Now - t).TotalMilliseconds);
			} catch (Exception ex) {
				LoggingService.LogError ("Could not load parser database.", ex);
			}

			Workspace ws = item as Workspace;
			if (ws != null) {
				ws.DescendantItemAdded += descendantItemAddedHandler;
				ws.DescendantItemRemoved += descendantItemRemovedHandler;
			}
			item.ConfigurationsChanged += configurationsChanged;
			
			WorkspaceItemEventArgs args = new WorkspaceItemEventArgs (item);
			NotifyDescendantItemAdded (this, args);
			NotifyConfigurationsChanged (null, args);
			
			if (WorkspaceItemOpened != null)
				WorkspaceItemOpened (this, args);
			if (Items.Count == 1 && !reloading) {
				IdeApp.Workbench.CurrentLayout = "Solution";
				if (FirstWorkspaceItemOpened != null)
					FirstWorkspaceItemOpened (this, args);
			}
		}
		
		internal void NotifyItemRemoved (WorkspaceItem item)
		{
			if (DispatchService.IsGuiThread)
				NotifyItemRemovedGui (item, IsReloading);
			else {
				bool reloading = IsReloading;
				Gtk.Application.Invoke (delegate {
					NotifyItemRemovedGui (item, reloading);
				});
			}
		}
		
		internal void NotifyItemRemovedGui (WorkspaceItem item, bool reloading)
		{
			Workspace ws = item as Workspace;
			if (ws != null) {
				ws.DescendantItemAdded -= descendantItemAddedHandler;
				ws.DescendantItemRemoved -= descendantItemRemovedHandler;
			}
			item.ConfigurationsChanged -= configurationsChanged;
			
			if (Items.Count == 0 && !reloading) {
				if (LastWorkspaceItemClosed != null)
					LastWorkspaceItemClosed (this, EventArgs.Empty);
			}
			
			WorkspaceItemEventArgs args = new WorkspaceItemEventArgs (item);
			NotifyConfigurationsChanged (null, args);
			
			if (WorkspaceItemClosed != null)
				WorkspaceItemClosed (this, args);
			
			MonoDevelop.Ide.TypeSystem.TypeSystemService.Unload (item);
//			ParserDatabase.Unload (item);
			
			NotifyDescendantItemRemoved (this, args);
		}
		
		void SubscribeSolution (Solution sol)
		{
			sol.FileAddedToProject += fileAddedToProjectHandler;
			sol.FileRemovedFromProject += fileRemovedFromProjectHandler;
			sol.FileRenamedInProject += fileRenamedInProjectHandler;
			sol.FileChangedInProject += fileChangedInProjectHandler;
			sol.FilePropertyChangedInProject += filePropertyChangedInProjectHandler;
			sol.ReferenceAddedToProject += referenceAddedToProjectHandler;
			sol.ReferenceRemovedFromProject += referenceRemovedFromProjectHandler;
			sol.SolutionItemAdded += itemAddedToSolutionHandler;
			sol.SolutionItemRemoved += itemRemovedFromSolutionHandler;
		}
		
		void UnsubscribeSolution (Solution solution)
		{
			solution.FileAddedToProject -= fileAddedToProjectHandler;
			solution.FileRemovedFromProject -= fileRemovedFromProjectHandler;
			solution.FileRenamedInProject -= fileRenamedInProjectHandler;
			solution.FileChangedInProject -= fileChangedInProjectHandler;
			solution.FilePropertyChangedInProject -= filePropertyChangedInProjectHandler;
			solution.ReferenceAddedToProject -= referenceAddedToProjectHandler;
			solution.ReferenceRemovedFromProject -= referenceRemovedFromProjectHandler;
			solution.SolutionItemAdded -= itemAddedToSolutionHandler;
			solution.SolutionItemRemoved -= itemRemovedFromSolutionHandler;
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
			
			Gtk.Application.Invoke (delegate {
				if (ItemAddedToSolution != null)
					ItemAddedToSolution (sender, args);
			});
		}
		
		void NotifyItemRemovedFromSolution (object sender, SolutionItemChangeEventArgs args)
		{
			NotifyItemRemovedFromSolutionRec (sender, args.SolutionItem, args.Solution);
		}
		
		void NotifyItemRemovedFromSolutionRec (object sender, SolutionItem e, Solution sol)
		{
			if (e == IdeApp.ProjectOperations.CurrentSelectedSolutionItem)
				IdeApp.ProjectOperations.CurrentSelectedSolutionItem = null;
				
			if (e is SolutionFolder) {
				foreach (SolutionItem ce in ((SolutionFolder)e).Items)
					NotifyItemRemovedFromSolutionRec (sender, ce, sol);
			}
			if (ItemRemovedFromSolution != null)
				ItemRemovedFromSolution (sender, new SolutionItemChangeEventArgs (e, sol, false));
		}
		
		void NotifyDescendantItemAdded (object s, WorkspaceItemEventArgs args)
		{
			// If a top level item has been moved to a child item, remove it from
			// the top
			if (s != this && Items.Contains (args.Item))
				Items.Remove (args.Item);
			foreach (WorkspaceItem item in args.Item.GetAllItems ()) {
				if (item is Solution)
					SubscribeSolution ((Solution)item);
				OnItemLoaded (item);
			}
		}
		
		void NotifyDescendantItemRemoved (object s, WorkspaceItemEventArgs args)
		{
			foreach (WorkspaceItem item in args.Item.GetAllItems ()) {
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
		public event EventHandler<UserPreferencesEventArgs> LoadingUserPreferences;
		
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
	
	class RootWorkspaceItemCollection: WorkspaceItemCollection
	{
		RootWorkspace parent;
		
		public RootWorkspaceItemCollection (RootWorkspace parent)
		{
			this.parent = parent;
		}
		
		protected override void ClearItems ()
		{
			if (parent != null) {
				List<WorkspaceItem> items = new List<WorkspaceItem> (this);
				foreach (WorkspaceItem it in items)
					parent.NotifyItemRemoved (it);
			}
			else
				base.ClearItems ();
		}
		
		protected override void InsertItem (int index, WorkspaceItem item)
		{
			base.InsertItem (index, item);
			if (parent != null)
				parent.NotifyItemAdded (item);
		}
		
		protected override void RemoveItem (int index)
		{
			WorkspaceItem item = this [index];
			base.RemoveItem (index);
			if (parent != null)
				parent.NotifyItemRemoved (item);
		}
		
		protected override void SetItem (int index, WorkspaceItem item)
		{
			WorkspaceItem oldItem = this [index];
			base.SetItem (index, item);
			if (parent != null) {
				parent.NotifyItemRemoved (oldItem);
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
		[ItemProperty]
		public string PreferredExecutionTarget;
	}
	
	public class ItemUnloadingEventArgs: EventArgs
	{
		IBuildTarget item;
		
		public bool Cancel { get; set; }
		
		public IBuildTarget Item {
			get {
				return item;
			}
		}
		
		public ItemUnloadingEventArgs (IBuildTarget item)
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
		
		void IDisposable.Dispose ()
		{
			NotifyChanges ();
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
