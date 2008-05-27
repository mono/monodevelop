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
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui
{
	public class RootWorkspace: IBuildTarget, IWorkspaceObject
	{
		WorkspaceItemCollection items;
		IParserDatabase parserDatabase;
		string activeConfiguration;
		Dictionary<WorkspaceItem, PropertyBag> userPrefs;
		SolutionEntityItem startupItem;
		
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
			
			FileService.FileRemoved += (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs> (CheckFileRemove));
			FileService.FileRenamed += (EventHandler<FileCopyEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileCopyEventArgs> (CheckFileRename));
			
			userPrefs = new Dictionary<WorkspaceItem,PropertyBag> ();
			
			GLib.Timeout.Add (2000, OnRunProjectChecks);
		}
		
		public WorkspaceItemCollection Items {
			get {
				if (items == null)
					items = new RootWorkspaceItemCollection (this);
				return items; 
			}
		}
		
		public IParserDatabase ParserDatabase {
			get { 
				if (parserDatabase == null) {
					parserDatabase = Services.ParserService.CreateParserDatabase ();
					parserDatabase.TrackFileChanges = true;
					parserDatabase.ParseProgressMonitorFactory = new ParseProgressMonitorFactory (); 
				}
				return parserDatabase; 
			}
		}
		
		public string ActiveConfiguration {
			get {
				return activeConfiguration;
			}
			set {
				if (activeConfiguration != value) {
					activeConfiguration = value;
					if (ActiveConfigurationChanged != null)
						ActiveConfigurationChanged (this, EventArgs.Empty);
				}
			}
		}
		
		public bool IsOpen {
			get { return Items.Count > 0; }
		}
		
		public CodeRefactorer GetCodeRefactorer (Solution solution) 
		{
			CodeRefactorer refactorer = new CodeRefactorer (solution, ParserDatabase);
			refactorer.TextFileProvider = new OpenDocumentFileProvider ();
			return refactorer;
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

		public string BaseDirectory {
			get {
				return IdeApp.ProjectOperations.ProjectsDefaultPath;
			}
		}
		
		string IWorkspaceObject.BaseDirectory {
			get {
				return BaseDirectory;
			}
			set {
				throw new NotSupportedException ();
			}
		}
		
		public SolutionEntityItem StartupItem {
			get {
				return startupItem; 
			}
			set {
				startupItem = value;
				if (startupItem != null) {
					WorkspaceItem pit = startupItem.ParentSolution;
					while (pit != null) {
						pit.ExtendedProperties ["__StartupItem"] = startupItem;
						pit = pit.ParentWorkspace;
					}
				}
				if (StartupItemChanged != null)
					StartupItemChanged (this, EventArgs.Empty);
			}
		}
		
		internal void SetBestStartupProject ()
		{
			if (!IsOpen) {
				StartupItem = null;
				return;
			}
			if (!SetBestStartupProject (true) && !SetBestStartupProject (false))
				StartupItem = null;
		}
		
		bool SetBestStartupProject (bool findExe)
		{
			Solution sol = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (sol != null && SetBestStartupProjectInSolution (sol, findExe))
				return true;
			
			WorkspaceItem it = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem;
			if (it != null && SetBestStartupProjectInWorkspace (it, findExe))
				return true;

			foreach (WorkspaceItem wit in Items) {
				foreach (WorkspaceItem cit in wit.GetAllItems ()) {
					if (SetBestStartupProjectInWorkspace (cit, findExe))
						return true;
				}
			}
			return false;
		}
		
		bool SetBestStartupProjectInWorkspace (WorkspaceItem it, bool findExe)
		{
			foreach (Solution sol in it.GetAllSolutions ()) {
				if (SetBestStartupProjectInSolution (sol, findExe))
					return true;
			}
			return false;
		}
		
		bool SetBestStartupProjectInSolution (Solution sol, bool findExe)
		{
			if (!findExe) {
				System.Collections.ObjectModel.ReadOnlyCollection<Project> ps = sol.GetAllProjects ();
				if (ps.Count > 0) {
					StartupItem = ps [0];
					return true;
				}
			} else {
				foreach (DotNetProject p in sol.GetAllSolutionItems<DotNetProject> ()) {
					if (p.CompileTarget == CompileTarget.Exe || p.CompileTarget == CompileTarget.WinExe) {
						StartupItem = p;
						return true;
					}
				}
			}
			return false;
		}
		
		public PropertyBag GetUserPreferences (WorkspaceItem item)
		{
			PropertyBag props;
			if (userPrefs.TryGetValue (item, out props))
				return props;
			props = new PropertyBag ();
			userPrefs [item] = props;
			return props;
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
			if (startupItem != null)
				return IdeApp.ProjectOperations.Execute (startupItem);
			else {
				MessageService.ShowError (GettextCatalog.GetString ("Startup project not set"), GettextCatalog.GetString ("To set a startup project, select the project in the solution pad an click on the option 'Set as Startup Project' in the contextual menu."));
				return null;
			}
		}
		
		public void Dispose ()
		{
		}

		public void Save (IProgressMonitor monitor)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Saving Workspace..."), Items.Count);
			foreach (WorkspaceItem it in Items) {
				it.Save (monitor);
				monitor.Step (1);
			}
			monitor.EndTask ();
		}
		
		BuildResult IBuildTarget.RunTarget (IProgressMonitor monitor, string target, string configuration)
		{
			BuildResult result = null;
			foreach (WorkspaceItem it in Items) {
				BuildResult res = it.RunTarget (monitor, target, configuration);
				if (res != null) {
					if (result == null)
						result = new BuildResult ();
					result.Append (res);
				}
			}
			return result;
		}

		public void Execute (MonoDevelop.Core.IProgressMonitor monitor, ExecutionContext context, string configuration)
		{
			if (startupItem != null)
				startupItem.Execute (monitor, context, configuration);
			else
				throw new UserException (GettextCatalog.GetString ("Startup project not set"));
		}
		
		public bool NeedsBuilding ()
		{
			return NeedsBuilding (IdeApp.Workspace.ActiveConfiguration) || IsDirtyFileInCombine;
		}

		public bool NeedsBuilding (string configuration)
		{
			foreach (WorkspaceItem it in Items) {
				if (it.NeedsBuilding (configuration))
					return true;
			}
			return false;
		}

		public void SetNeedsBuilding (bool needsBuilding, string configuration)
		{
			foreach (WorkspaceItem it in Items)
				it.SetNeedsBuilding (needsBuilding, configuration);
		}

		bool IsDirtyFileInCombine {
			get {
				foreach (Project projectEntry in GetAllProjects()) {
					foreach (ProjectFile fInfo in projectEntry.Files) {
						foreach (Document doc in IdeApp.Workbench.Documents) {
							if (doc.IsDirty && doc.FileName == fInfo.Name) {
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
		
		public void Close ()
		{
			Close (true);
		}

		public void Close (bool saveWorkspacePreferencies)
		{
			if (Items.Count > 0) {
				if (saveWorkspacePreferencies)
					SavePreferences ();

				Document[] docs = new Document [IdeApp.Workbench.Documents.Count];
				IdeApp.Workbench.Documents.CopyTo (docs, 0);
				foreach (Document doc in docs) {
					if (doc.HasProject)
						doc.Close ();
				}
				foreach (WorkspaceItem it in new List<WorkspaceItem> (Items)) {
					try {
						Items.Remove (it);
					} catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("Could not close solution '{0}.'", it.Name));
					}
				}
			}
		}
		
		public void CloseWorkspaceItem (WorkspaceItem item)
		{
			if (!Items.Contains (item))
				throw new InvalidOperationException ("Only top level items can be closed.");
			
			if (WorkspaceItemClosing != null) {
				try {
					WorkspaceItemClosing (this, new WorkspaceItemEventArgs (item));
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in WorkspaceItemClosing.", ex);
				}
			}
			
			Items.Remove (item);
		}
		
		public IAsyncOperation OpenWorkspaceItem (string filename)
		{
			return OpenWorkspaceItem (filename, true);
		}
		
		public IAsyncOperation OpenWorkspaceItem (string filename, bool closeCurrent)
		{
			if (closeCurrent)
				Close ();

			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true);
			
			object[] data = new object[] { filename, monitor };
			DispatchService.BackgroundDispatch (delegate {
				BackgroundLoadWorkspace (monitor, filename);
			});
			return monitor.AsyncOperation;
		}
		
		void BackgroundLoadWorkspace (IProgressMonitor monitor, string filename)
		{
			WorkspaceItem item = null;
			
			try {
				if (!File.Exists (filename)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", filename), null);
					monitor.Dispose ();
					return;
				}
				
				if (!Services.ProjectService.IsWorkspaceItemFile (filename)) {
					if (!Services.ProjectService.IsSolutionItemFile (filename)) {
						monitor.ReportError (GettextCatalog.GetString ("File is not a project or solution: {0}", filename), null);
						monitor.Dispose ();
						return;
					}
					
					// It is a project, not a solution. Try to create a dummy solution and add the project to it
					
					item = IdeApp.Services.ProjectService.GetWrapperSolution (monitor, filename);
				}
				
				if (item == null) {
					item = Services.ProjectService.ReadWorkspaceItem (monitor, filename);
					if (monitor.IsCancelRequested) {
						monitor.Dispose ();
						return;
					}
				}

				IdeApp.Workbench.RecentOpen.AddLastProject (item.FileName, item.Name);
				
				Items.Add (item);
				
				SearchForNewFiles ();

			} catch (Exception ex) {
				monitor.ReportError ("Load operation failed.", ex);
				
				// Don't use 'finally' to dispose the monitor, since it has to be disposed later
				monitor.Dispose ();
				return;
			}
			
			Gtk.Application.Invoke (delegate {
				using (monitor) {
					if (Items.Count == 1)
						RestoreWorkspacePreferences (item);
					if (StartupItem == null)
						SetBestStartupProject ();
					monitor.ReportSuccess (GettextCatalog.GetString ("Solution loaded."));
				}
			});
		}
		
		void SearchForNewFiles ()
		{
			foreach (Project p in GetAllProjects()) {
				if (p.NewFileSearch != NewFileSearch.None)
					SearchNewFiles (p);
			}
		}

		
		void SearchNewFiles (Project project)
		{
			StringCollection newFiles   = new StringCollection();
			string[] collection = Directory.GetFiles (project.BaseDirectory, "*", SearchOption.AllDirectories);

			foreach (string sfile in collection) {
				string extension = Path.GetExtension(sfile).ToUpper();
				string file = Path.GetFileName (sfile);

				if (!project.IsFileInProject(sfile) &&
					extension != ".SCC" &&  // source safe control files -- Svante Lidmans
					extension != ".DLL" &&
					extension != ".PDB" &&
					extension != ".EXE" &&
					extension != ".CMBX" &&
					extension != ".PRJX" &&
					extension != ".SWP" &&
					extension != ".MDSX" &&
					extension != ".MDS" &&
					extension != ".MDP" && 
					extension != ".PIDB" &&
					!file.EndsWith ("make.sh") &&
					!file.EndsWith ("~") &&
					!file.StartsWith (".") &&
					!(Path.GetDirectoryName(sfile).IndexOf("CVS") != -1) &&
					!(Path.GetDirectoryName(sfile).IndexOf(".svn") != -1) &&
					!file.StartsWith ("Makefile") &&
					!Path.GetDirectoryName(file).EndsWith("ProjectDocumentation")) {

					newFiles.Add(sfile);
				}
			}
			
			if (newFiles.Count > 0) {
				if (project.NewFileSearch == NewFileSearch.OnLoadAutoInsert) {
					foreach (string file in newFiles) {
						ProjectFile newFile = new ProjectFile(file);
						newFile.BuildAction = project.IsCompileable(file) ? BuildAction.Compile : BuildAction.Nothing;
						project.Files.Add(newFile);
					}		
				} else {
					DispatchService.GuiDispatch (
						delegate (object state) {
							NewFilesMessage message = (NewFilesMessage) state;
							new IncludeFilesDialog (message.Project, message.NewFiles).ShowDialog ();
						},
						new NewFilesMessage (project, newFiles)
					);
				}
			}
		}
		
		private class NewFilesMessage
		{
			public Project Project;
			public StringCollection NewFiles;
			public NewFilesMessage (Project p, StringCollection newFiles)
			{
				this.Project = p;
				this.NewFiles = newFiles;
			}
		}
		
		void RestoreWorkspacePreferences (WorkspaceItem item)
		{
			string preferencesFileName = GetPreferencesFileName (item);
			if (!File.Exists(preferencesFileName))
				return;
			
			PropertyBag props = null;
			XmlTextReader reader = new XmlTextReader (preferencesFileName);
			try {
				reader.MoveToContent ();
				if (reader.LocalName != "Properties")
					return;

				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				ser.SerializationContext.BaseFile = preferencesFileName;
				props = (PropertyBag) ser.Deserialize (reader, typeof(PropertyBag));
			} catch (Exception e) {
				LoggingService.LogError ("Exception while loading user solution preferences.", e);
				return;
			} finally {
				reader.Close ();
			}

			// Restore local configuration data
			
			try {
				WorkspaceUserData data = props.GetValue<WorkspaceUserData> ("MonoDevelop.Ide.Workspace");
				if (data != null) {
					ActiveConfiguration = data.ActiveConfiguration;
					if (data.StartupItem != null && StartupItem == null)
						StartupItem = FindSolutionItem (data.StartupItem);
				}
			}
			catch (Exception ex) {
				LoggingService.LogError ("Exception while loading user solution preferences.", ex);
			}
			
			// Allow add-ins to restore preferences
			
			if (LoadingUserPreferences != null) {
				UserPreferencesEventArgs args = new UserPreferencesEventArgs (item, props);
				try {
					LoadingUserPreferences (this, args);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in LoadingUserPreferences.", ex);
				}
			}
		} 
		
		string GetPreferencesFileName (WorkspaceItem item)
		{
			return Path.Combine (Path.GetDirectoryName (item.FileName), Path.ChangeExtension (item.FileName, ".userprefs"));
		}
		
		void SavePreferences (WorkspaceItem item)
		{
			PropertyBag props = GetUserPreferences (item);
			
			// Local configuration info
			
			WorkspaceUserData data = new WorkspaceUserData ();
			data.ActiveConfiguration = ActiveConfiguration;
			
			SolutionEntityItem sit = (SolutionEntityItem) item.ExtendedProperties ["__StartupItem"];
			if (sit != null)
				data.StartupItem = sit.FileName;
			
			props.SetValue ("MonoDevelop.Ide.Workspace", data);
			
			// Allow add-ins to fill-up data
			
			if (StoringUserPreferences != null) {
				UserPreferencesEventArgs args = new UserPreferencesEventArgs (item, props);
				try {
					StoringUserPreferences (this, args);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in UserPreferencesRequested.", ex);
				}
			}
			
			// Save the file
			
			string file = GetPreferencesFileName (item);
			
			if (props.IsEmpty) {
				if (File.Exists (file))
					File.Delete (file);
				return;
			}
			
			XmlTextWriter writer = new XmlTextWriter (file, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
			ser.SerializationContext.BaseFile = file;
		
			try {
				ser.Serialize (writer, props, typeof(PropertyBag));
			} catch (Exception e) {
				LoggingService.LogWarning ("Could not save solution preferences: " + GetPreferencesFileName (item), e);
			} finally {
				writer.Close ();
			}
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
				if (AllowReload (item.GetAllProjects ())) {
					if (item.ParentWorkspace == null) {
						string file = item.FileName;
						CloseWorkspaceItem (item);
						OpenWorkspaceItem (file);
					}
					else {
						using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true)) {
							item.ParentWorkspace.ReloadItem (m, item);
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
				
				if (AllowReload (projects)) {
					using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true)) {
						// Root folders never need to reload
						entry.ParentFolder.ReloadItem (m, entry);
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
		
		bool AllowReload (IEnumerable projects)
		{
			if (projects == null)
				return true;
			
			Project projectWarn = null;
			foreach (Project p in projects) {
				if (HasOpenDocuments (p, false)) {
					projectWarn = p;
					break;
				}
			}
			
			return (projectWarn == null || MessageService.Confirm (GettextCatalog.GetString ("The project '{0}' has been modified by an external application. Do you want to reload it? All project files will be closed.", projectWarn.Name), AlertButton.Reload));
		}
		
		internal bool HasOpenDocuments (Project project, bool modifiedOnly)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.Project == project && (!modifiedOnly || doc.IsDirty))
					return true;
			}
			return false;
		}
		
		
#endregion
		
#region Event handling
		
		internal void NotifyItemAdded (WorkspaceItem item)
		{
			if (DispatchService.IsGuiThread)
				NotifyItemAddedGui (item);
			else
				Gtk.Application.Invoke (delegate {
					NotifyItemAddedGui (item);
				});
		}
		
		void NotifyItemAddedGui (WorkspaceItem item)
		{
//			MonoDevelop.Projects.Dom.Parser.ProjectDomService.Load (item);
			try {
				ParserDatabase.Load (item);
			}
			catch (Exception ex) {
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
			if (Items.Count == 1 && FirstWorkspaceItemOpened != null)
				FirstWorkspaceItemOpened (this, args);
		}
		
		internal void NotifyItemRemoved (WorkspaceItem item)
		{
			if (DispatchService.IsGuiThread)
				NotifyItemRemovedGui (item);
			else
				Gtk.Application.Invoke (delegate {
					NotifyItemRemovedGui (item);
				});
		}
		
		internal void NotifyItemRemovedGui (WorkspaceItem item)
		{
			Workspace ws = item as Workspace;
			if (ws != null) {
				ws.DescendantItemAdded -= descendantItemAddedHandler;
				ws.DescendantItemRemoved -= descendantItemRemovedHandler;
			}
			item.ConfigurationsChanged -= configurationsChanged;
			
			if (Items.Count == 0 && LastWorkspaceItemClosed != null)
				LastWorkspaceItemClosed (this, EventArgs.Empty);
			
			WorkspaceItemEventArgs args = new WorkspaceItemEventArgs (item);
			NotifyConfigurationsChanged (null, args);
			
			if (WorkspaceItemClosed != null)
				WorkspaceItemClosed (this, args);
			
//			MonoDevelop.Projects.Dom.Parser.ProjectDomService.Unload (item);
			ParserDatabase.Unload (item);
			
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
		
		void NotifyItemAddedToSolution (object sender, SolutionItemEventArgs args)
		{
			// Delay the notification of this event to ensure that the new project is properly
			// registered in the parser database when it is fired
			
			Gtk.Application.Invoke (delegate {
				if (ItemAddedToSolution != null)
					ItemAddedToSolution (sender, args);
			});
		}
		
		void NotifyItemRemovedFromSolution (object sender, SolutionItemEventArgs args)
		{
			NotifyItemRemovedFromSolutionRec (sender, args.SolutionItem);
		}
		
		void NotifyItemRemovedFromSolutionRec (object sender, SolutionItem e)
		{
			if (e == IdeApp.ProjectOperations.CurrentSelectedSolutionItem)
				IdeApp.ProjectOperations.CurrentSelectedSolutionItem = null;
			if (e == startupItem)
				SetBestStartupProject ();
				
			if (e is SolutionFolder) {
				foreach (SolutionItem ce in ((SolutionFolder)e).Items)
					NotifyItemRemovedFromSolutionRec (sender, ce);
			}
			if (ItemRemovedFromSolution != null)
				ItemRemovedFromSolution (sender, new SolutionItemEventArgs (e));
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
				userPrefs.Remove (item);
				if (WorkspaceItemUnloaded != null)
					WorkspaceItemUnloaded (this, new WorkspaceItemEventArgs (item));
				if (item is Solution && SolutionUnloaded != null)
					SolutionUnloaded (this, new SolutionEventArgs ((Solution)item));
				if (startupItem != null && ProjectOperations.ContainsTarget (item, startupItem)) {
					SetBestStartupProject ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error in SolutionClosed event.", ex);
			}
		}

		void CheckFileRemove(object sender, FileEventArgs e)
		{
			foreach (Solution sol in GetAllSolutions ())
				sol.RootFolder.RemoveFileFromProjects (e.FileName);
		}
		
		void CheckFileRename(object sender, FileCopyEventArgs e)
		{
			foreach (Solution sol in GetAllSolutions ())
				sol.RootFolder.RenameFileInProjects (e.SourceFile, e.TargetFile);
		}
		
#endregion

#region Event declaration
		
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		
		public event EventHandler<SolutionEventArgs> SolutionLoaded;
		public event EventHandler<SolutionEventArgs> SolutionUnloaded;
		public event EventHandler<WorkspaceItemEventArgs> FirstWorkspaceItemOpened;
		public event EventHandler LastWorkspaceItemClosed;
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemLoaded;
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemUnloaded;
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemOpened;
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemClosing;
		public event EventHandler<WorkspaceItemEventArgs> WorkspaceItemClosed;
		public event EventHandler<UserPreferencesEventArgs> StoringUserPreferences;
		public event EventHandler<UserPreferencesEventArgs> LoadingUserPreferences;
		
		public event EventHandler<SolutionEventArgs> CurrentSelectedSolutionChanged;
		
		public event ProjectEventHandler CurrentProjectChanged;
		
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		
		// Fired just before an entry is added to a combine
		public event SolutionItemEventHandler ItemAddedToSolution;
		public event SolutionItemEventHandler ItemRemovedFromSolution;

		public event EventHandler ActiveConfigurationChanged;
		public event EventHandler ConfigurationsChanged;
		public event EventHandler StartupItemChanged;
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
		
		[ProjectPathItemProperty]
		public string StartupItem;
	}
}
