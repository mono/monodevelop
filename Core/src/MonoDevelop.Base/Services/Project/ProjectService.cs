// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;

using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Serialization;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Services
{
	
	public enum BeforeCompileAction {
		Nothing,
		SaveAllFiles,
		PromptForSave,
	}
	
	public class ProjectService : AbstractService, IProjectService
	{
		Project currentProject = null;
		Combine  currentCombine = null;
		Combine  openCombine    = null;
		DataContext dataContext = new DataContext ();
		ProjectBindingCodon[] projectBindings;
		IParserDatabase parserDatabase;
		
		IAsyncOperation currentBuildOperation = NullAsyncOperation.Success;
		IAsyncOperation currentRunOperation = NullAsyncOperation.Success;
		
		FileFormatManager formatManager = new FileFormatManager ();
		IFileFormat defaultProjectFormat = new MdpFileFormat ();
		IFileFormat defaultCombineFormat = new MdsFileFormat ();
		
		ICompilerResult lastResult = new DefaultCompilerResult ();
		
		GuiHelper guiHelper = new GuiHelper ();
		SelectReferenceDialog selDialog = null;
			
		public Project CurrentSelectedProject {
			get {
				return currentProject;
			}
			set {
				if (value != currentProject) {
					System.Diagnostics.Debug.Assert(openCombine != null);
					currentProject = value;
					OnCurrentProjectChanged(new ProjectEventArgs(currentProject));
				}
			}
		}
		
		public Combine CurrentSelectedCombine {
			get {
				return currentCombine;
			}
			set {
				if (value != currentCombine) {
					System.Diagnostics.Debug.Assert(openCombine != null);
					currentCombine = value;
					OnCurrentSelectedCombineChanged(new CombineEventArgs(currentCombine));
				}
			}
		}
		
		public CombineEntry CurrentSelectedCombineEntry {
			get {
				if (currentProject != null)
					return currentProject;
				else
					return currentCombine;
			}
		}
		
		public IAsyncOperation CurrentBuildOperation {
			get { return currentBuildOperation; }
		}
		
		public IAsyncOperation CurrentRunOperation {
			get { return currentRunOperation; }
		}
		
		public Combine CurrentOpenCombine {
			get {
				return openCombine;
			}
			set {
				openCombine = value;
			}
		}
		
		public ICompilerResult LastCompilerResult {
			get { return lastResult; }
		}
		
		bool IsDirtyFileInCombine {
			get {
				CombineEntryCollection projects = openCombine.GetAllProjects();
				
				foreach (Project projectEntry in projects) {
					foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
						foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
							if (content.IsDirty && content.ContentName == fInfo.Name) {
								return true;
							}
						}
					}
				}
				return false;
			}
		}
		
		public bool NeedsCompiling {
			get {
				if (openCombine == null) {
					return false;
				}
				return openCombine.NeedsBuilding || IsDirtyFileInCombine;
			}
		}
		
		public DataContext DataContext {
			get { return dataContext; }
		}
		
		public IParserDatabase ParserDatabase {
			get { return parserDatabase; }
		}

		
		public FileFormatManager FileFormats {
			get { return formatManager; }
		}
		
		public void SaveCombinePreferences()
		{
			if (CurrentOpenCombine != null)
				SaveCombinePreferences(CurrentOpenCombine);
		}
		
		public CombineEntry ReadFile (string file, IProgressMonitor monitor)
		{
			IFileFormat format = formatManager.GetFileFormat (file);

			if (format == null)
				throw new InvalidOperationException ("Unknown file format: " + file);
			
			CombineEntry obj = format.ReadFile (file, monitor) as CombineEntry;
			if (obj == null)
				throw new InvalidOperationException ("Invalid file format: " + file);
			
			if (obj.FileFormat == null)	
				obj.FileFormat = format;

			return obj;
		}
		
		public void WriteFile (string file, CombineEntry entry, IProgressMonitor monitor)
		{
			IFileFormat format = entry.FileFormat;
			if (format == null) {
				if (entry is Project) format = defaultProjectFormat;
				else if (entry is Combine) format = defaultCombineFormat;
				else format = formatManager.GetFileFormatForObject (entry);
				
				if (format == null)
					throw new InvalidOperationException ("FileFormat not provided for combine entry '" + entry.Name + "'");
				entry.FileName = format.GetValidFormatName (file);
			}
			entry.FileName = file;
			format.WriteFile (entry.FileName, entry, monitor);
		}
		
		public Project CreateSingleFileProject (string file)
		{
			foreach (ProjectBindingCodon projectBinding in projectBindings) {
				Project project = projectBinding.ProjectBinding.CreateSingleFileProject (file);
				if (project != null)
					return project;
			}
			return null;
		}
		
		public Project CreateProject (string type, ProjectCreateInformation info, XmlElement projectOptions)
		{
			foreach (ProjectBindingCodon projectBinding in projectBindings) {
				if (projectBinding.ProjectBinding.Name == type) {
					Project project = projectBinding.ProjectBinding.CreateProject (info, projectOptions);
					return project;
				}
			}
			return null;
		}
		
		public void CloseCombine()
		{
			CloseCombine(true);
		}

		public void CloseCombine (bool saveCombinePreferencies)
		{
			if (CurrentOpenCombine != null) {
				if (saveCombinePreferencies)
					SaveCombinePreferences (CurrentOpenCombine);
				Combine closedCombine = CurrentOpenCombine;
				CurrentSelectedProject = null;
				CurrentOpenCombine = CurrentSelectedCombine = null;
				WorkbenchSingleton.Workbench.CloseAllViews();
				
				parserDatabase.Unload (closedCombine);
				
				OnCombineClosed(new CombineEventArgs(closedCombine));
				closedCombine.Dispose();
			}
		}
		
		FileUtilityService fileUtilityService = Runtime.FileUtilityService;
		
		public bool IsCombineEntryFile (string filename)
		{
			if (filename.StartsWith ("file://"))
				filename = filename.Substring (7);
				
			IFileFormat format = formatManager.GetFileFormat (filename);
			return format != null;
		}
		
		public IAsyncOperation OpenCombine(string filename)
		{
			if (openCombine != null) {
				SaveCombine();
				CloseCombine();
			}

			if (filename.StartsWith ("file://"))
				filename = filename.Substring (7);

			IProgressMonitor monitor = Runtime.TaskService.GetLoadProgressMonitor ();
			
			object[] data = new object[] { filename, monitor };
			Runtime.DispatchService.BackgroundDispatch (new StatefulMessageHandler (backgroundLoadCombine), data);
			return monitor.AsyncOperation;
		}
		
		void backgroundLoadCombine (object arg)
		{
			object[] data = (object[]) arg;
			string filename = data[0] as string;
			IProgressMonitor monitor = data [1] as IProgressMonitor;
			
			try {
				if (!fileUtilityService.TestFileExists(filename)) {
					monitor.ReportError (string.Format (GettextCatalog.GetString ("File not found: {0}"), filename), null);
					return;
				}
				
				string validcombine = Path.ChangeExtension (filename, ".mds");
				
				if (Path.GetExtension (filename).ToLower() != ".mds") {
					if (File.Exists (validcombine))
						filename = validcombine;
				} else if (Path.GetExtension (filename).ToLower () != ".cmbx") {
					if (File.Exists (Path.ChangeExtension (filename, ".cmbx")))
						filename = Path.ChangeExtension (filename, ".cmbx");
				}
			
				CombineEntry entry = ReadFile (filename, monitor);
				if (!(entry is Combine)) {
					Combine loadingCombine = new Combine();
					loadingCombine.Entries.Add (entry);
					loadingCombine.Name = entry.Name;
					loadingCombine.Save (validcombine, monitor);
					entry = loadingCombine;
				}
			
				openCombine = (Combine) entry;
				
				Runtime.FileService.RecentOpen.AddLastProject (filename, openCombine.Name);
				
				openCombine.FileAddedToProject += new ProjectFileEventHandler (NotifyFileAddedToProject);
				openCombine.FileRemovedFromProject += new ProjectFileEventHandler (NotifyFileRemovedFromProject);
				openCombine.FileRenamedInProject += new ProjectFileRenamedEventHandler (NotifyFileRenamedInProject);
				openCombine.FileChangedInProject += new ProjectFileEventHandler (NotifyFileChangedInProject);
				openCombine.ReferenceAddedToProject += new ProjectReferenceEventHandler (NotifyReferenceAddedToProject);
				openCombine.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (NotifyReferenceRemovedFromProject);
				
				SearchForNewFiles ();

				parserDatabase.Load (openCombine);
				OnCombineOpened (new CombineEventArgs(openCombine));

				Runtime.DispatchService.GuiDispatch (new StatefulMessageHandler (RestoreCombinePreferences), CurrentOpenCombine);
				
				SaveCombine ();
				monitor.ReportSuccess (GettextCatalog.GetString ("Combine loaded."));
			} catch (Exception ex) {
				monitor.ReportError ("Load operation failed.", ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		void SearchForNewFiles ()
		{
			foreach (Project p in openCombine.GetAllProjects()) {
				if (p.NewFileSearch != NewFileSearch.None)
					p.SearchNewFiles ();
			}
		}
		
		public void SaveCombine()
		{
			IProgressMonitor monitor = Runtime.TaskService.GetSaveProgressMonitor ();
			try {
				openCombine.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Combine saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void MarkFileDirty (string filename)
		{
			if (openCombine != null) {
				Project entry = openCombine.GetProjectEntryContaining (filename);
				if (entry != null) {
					entry.NeedsBuilding = true;
				}
			}
		}

		public IAsyncOperation Execute (CombineEntry entry)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), Runtime.TaskService);

			Runtime.DispatchService.ThreadDispatch (new StatefulMessageHandler (ExecuteCombineEntryAsync), new object[] {entry, monitor, context});
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		void ExecuteCombineEntryAsync (object ob)
		{
			object[] data = (object[]) ob;
			CombineEntry entry = (CombineEntry) data[0];
			IProgressMonitor monitor = (IProgressMonitor) data[1];
			ExecutionContext context = (ExecutionContext) data[2];
			OnBeforeStartProject ();
			try {
				entry.Execute (monitor, context);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public IAsyncOperation Debug (CombineEntry entry)
		{
			if (Runtime.DebuggingService == null) {
				return NullAsyncOperation.Failure;
			}

			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;
			
			guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (Runtime.DebuggingService.GetExecutionHandlerFactory (), Runtime.TaskService);
			
			Runtime.DispatchService.ThreadDispatch (new StatefulMessageHandler (DebugCombineEntryAsync), new object[] {entry, monitor, context});
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		void DebugCombineEntryAsync (object ob)
		{
			object[] data = (object[]) ob;
			CombineEntry entry = (CombineEntry) data[0];
			IProgressMonitor monitor = (IProgressMonitor) data[1];
			ExecutionContext context = (ExecutionContext) data[2];
			try {
				entry.Execute (monitor, context);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
			} finally {
				monitor.Dispose ();
			}
			guiHelper.SetWorkbenchContext (WorkbenchContext.Edit);
		}
		
		public IAsyncOperation DebugFile (string file)
		{
			Project tempProject = CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Debug (tempProject);
				ProjectOperationHandler h = new ProjectOperationHandler ();
				h.Project = tempProject;
				aop.Completed += new OperationHandler (h.Run);
				return aop;
			} else {
				Runtime.MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation DebugApplication (string executableFile)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;
			if (Runtime.DebuggingService == null) return NullAsyncOperation.Failure;
			
			guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = Runtime.TaskService.GetRunProgressMonitor ();

			Runtime.DebuggingService.Run ((IConsole) monitor, new string[] { executableFile });
			
			DebugApplicationStopper disposer = new DebugApplicationStopper ();
			disposer.Monitor = monitor;
			Runtime.DebuggingService.StoppedEvent += new EventHandler (disposer.Run);
			
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		class DebugApplicationStopper {
			public IProgressMonitor Monitor;
			public void Run (object sender, EventArgs e) { Monitor.Dispose (); }
		}
		
		class ProjectOperationHandler {
			public Project Project;
			public void Run (IAsyncOperation op) { Project.Dispose (); }
		}
		
		public IAsyncOperation BuildFile (string file)
		{
			Project tempProject = CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Build (tempProject);
				ProjectOperationHandler h = new ProjectOperationHandler ();
				h.Project = tempProject;
				aop.Completed += new OperationHandler (h.Run);
				return aop;
			} else {
				Runtime.MessageService.ShowError (string.Format (GettextCatalog.GetString ("The file {0} can't be compiled."), file));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation ExecuteFile (string file)
		{
			Project tempProject = CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject);
				ProjectOperationHandler h = new ProjectOperationHandler ();
				h.Project = tempProject;
				aop.Completed += new OperationHandler (h.Run);
				return aop;
			} else {
				Runtime.MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
	
		public IAsyncOperation Rebuild (CombineEntry entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;

			entry.Clean ();
			return Build (entry);
		}
		
		public IAsyncOperation Build (CombineEntry entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;
			
			BeforeCompile (entry);
				
			IProgressMonitor monitor = Runtime.TaskService.GetBuildProgressMonitor ();
			Runtime.DispatchService.ThreadDispatch (new StatefulMessageHandler (BuildCombineEntryAsync), new object[] {entry, monitor});
			currentBuildOperation = monitor.AsyncOperation;
			return currentBuildOperation;
		}
		
		public void BuildCombineEntryAsync (object ob)
		{
			object[] data = (object[]) ob;
			CombineEntry entry = (CombineEntry) data [0];
			IProgressMonitor monitor = (IProgressMonitor) data [1];
			ICompilerResult result = null;
			try {
				BeginBuild ();
				result = entry.Build (monitor);
				BuildDone (monitor, result);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Build failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		void BeginBuild ()
		{
			Runtime.TaskService.ClearTasks();
			OnStartBuild ();
		}
		
		void BuildDone (IProgressMonitor monitor, ICompilerResult result)
		{
			lastResult = result;
			monitor.Log.WriteLine ();
			monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("---------------------- Done ----------------------")));
			
			foreach (CompilerError err in result.CompilerResults.Errors) {
				Runtime.TaskService.AddTask (new Task(null, err));
			}
			
			if (result.ErrorCount == 0 && result.WarningCount == 0 && lastResult.FailedBuildCount == 0) {
				monitor.ReportSuccess (GettextCatalog.GetString ("Build successful."));
			} else if (result.ErrorCount == 0 && result.WarningCount > 0) {
				monitor.ReportWarning (String.Format (GettextCatalog.GetString ("Build: {0} errors, {1} warnings."), result.ErrorCount, result.WarningCount));
			} else if (result.ErrorCount > 0) {
				monitor.ReportError (String.Format (GettextCatalog.GetString ("Build: {0} errors, {1} warnings."), result.ErrorCount, result.WarningCount), null);
			} else {
				monitor.ReportError (String.Format (GettextCatalog.GetString ("Build failed.")), null);
			}
			
			OnEndBuild (lastResult.FailedBuildCount == 0);
		}
		
		void BeforeCompile (CombineEntry entry)
		{
			DoBeforeCompileAction();
			
			if (entry is Project) {
				Project project = (Project) entry;
				
				Runtime.StringParserService.Properties["Project"] = project.Name;
				
				string outputDir = ((AbstractProjectConfiguration)project.ActiveConfiguration).OutputDirectory;
				try {
					DirectoryInfo directoryInfo = new DirectoryInfo(outputDir);
					if (!directoryInfo.Exists) {
						directoryInfo.Create();
					}
				} catch (Exception e) {
					throw new ApplicationException("Can't create project output directory " + outputDir + " original exception:\n" + e.ToString());
				}
			}
		}
		
		void DoBeforeCompileAction()
		{
			BeforeCompileAction action = (BeforeCompileAction)Runtime.Properties.GetProperty("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", BeforeCompileAction.SaveAllFiles);
			
			switch (action) {
				case BeforeCompileAction.Nothing:
					break;
				case BeforeCompileAction.PromptForSave:
					bool save = false;
					foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
						if (content.ContentName != null && content.IsDirty) {
							if (!save) {
								if (Runtime.MessageService.AskQuestion(GettextCatalog.GetString ("Save changed files?"))) {
									save = true;
								} else {
									break;
								}
							}
							MarkFileDirty(content.ContentName);
							content.Save();
						}
					}
					break;
				case BeforeCompileAction.SaveAllFiles:
					foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
						if (content.ContentName != null && content.IsDirty) {
							MarkFileDirty(content.ContentName);
							content.Save();
						}
					}
					break;
				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
		}
		
		void RemoveFileFromAllProjects(string fileName)
		{
			CombineEntryCollection projects = openCombine.GetAllProjects();
			
			restart:
			foreach (Project projectEntry in projects) {
				foreach (ProjectReference rInfo in projectEntry.ProjectReferences) {
					if (rInfo.ReferenceType == ReferenceType.Assembly && rInfo.Reference == fileName) {
						projectEntry.ProjectReferences.Remove(rInfo);
						goto restart;
					}
				}
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name == fileName) {
						projectEntry.ProjectFiles.Remove(fInfo);
						goto restart;
					}
				}
			}
		}
		
		void RemoveAllInDirectory(string dirName)
		{
			CombineEntryCollection projects = openCombine.GetAllProjects();
			
			restart:
			foreach (Project projectEntry in projects) {
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name.StartsWith(dirName)) {
						projectEntry.ProjectFiles.Remove(fInfo);
						goto restart;
					}
				}
			}
		}
		
		void CheckFileRemove(object sender, FileEventArgs e)
		{
			if (openCombine != null) {
				if (e.IsDirectory) {
					RemoveAllInDirectory(e.FileName);
				} else {
					RemoveFileFromAllProjects(e.FileName);
				}
			}
		}
		
		void RenameFileInAllProjects(string oldName, string newName)
		{
			CombineEntryCollection projects = openCombine.GetAllProjects();
			
			foreach (Project projectEntry in projects) {
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name == oldName) {
						fInfo.Name = newName;
					}
				}
			}
		}

		void RenameDirectoryInAllProjects(string oldName, string newName)
		{
			CombineEntryCollection projects = openCombine.GetAllProjects();
			
			foreach (Project projectEntry in projects) {
				foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
					if (fInfo.Name.StartsWith(oldName)) {
						fInfo.Name = newName + fInfo.Name.Substring(oldName.Length);
					}
				}
			}
		}

		void CheckFileRename(object sender, FileEventArgs e)
		{
			System.Diagnostics.Debug.Assert(e.SourceFile != e.TargetFile);
			if (openCombine != null) {
				if (e.IsDirectory) {
					RenameDirectoryInAllProjects(e.SourceFile, e.TargetFile);
				} else {
					RenameFileInAllProjects(e.SourceFile, e.TargetFile);
				}
			}
		}
		
		public void Deploy (Project project)
		{
			foreach (IViewContent viewContent in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (viewContent.IsDirty) {
					viewContent.Save();
				}
			}
			DeployInformation.Deploy (project);
		}

		public void ShowOptions (CombineEntry entry)
		{
			if (entry is Project) {
				Project selectedProject = (Project) entry;
				
				IAddInTreeNode generalOptionsNode          = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/ProjectOptions/GeneralOptions");
				IAddInTreeNode configurationPropertiesNode = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/ProjectOptions/ConfigurationProperties");
				
				ProjectOptionsDialog optionsDialog = new ProjectOptionsDialog ((Gtk.Window)WorkbenchSingleton.Workbench, selectedProject, generalOptionsNode, configurationPropertiesNode);
				if (optionsDialog.Run() == (int)Gtk.ResponseType.Ok) {
					selectedProject.NeedsBuilding = true;
				}
			} else if (entry is Combine) {
				Combine combine = (Combine) entry;
				
				IAddInTreeNode generalOptionsNode          = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/CombineOptions/GeneralOptions");
				IAddInTreeNode configurationPropertiesNode = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/CombineOptions/ConfigurationProperties");
				
				CombineOptionsDialog optionsDialog = new CombineOptionsDialog ((Gtk.Window)WorkbenchSingleton.Workbench, combine, generalOptionsNode, configurationPropertiesNode);
				optionsDialog.Run ();
			}
			
			SaveCombine ();
		}
		
		public CombineEntry CreateProject (Combine parentCombine)
		{
			return CreateCombineEntry (parentCombine, false);
		}
		
		public CombineEntry CreateCombine (Combine parentCombine)
		{
			return CreateCombineEntry (parentCombine, true);
		}
		
		CombineEntry CreateCombineEntry (Combine parentCombine, bool createCombine)
		{
			CombineEntry res = null;
			NewProjectDialog npdlg = new NewProjectDialog (createCombine);
			if (npdlg.Run () == (int) Gtk.ResponseType.Ok) {
				IProgressMonitor monitor = Runtime.TaskService.GetLoadProgressMonitor ();
				try {
					if (createCombine)
						res = parentCombine.AddEntry (npdlg.NewCombineLocation, monitor);
					else
						res = parentCombine.AddEntry (npdlg.NewProjectLocation, monitor);
				}
				catch {
					Runtime.MessageService.ShowError (string.Format (GettextCatalog.GetString ("The file '{0}' could not be loaded."), npdlg.NewProjectLocation));
					res = null;
				}
				monitor.Dispose ();
			}
			
			npdlg = null;

			if (res != null)
				SaveCombine ();

			return res;
		}

		public CombineEntry AddCombineEntry (Combine parentCombine)
		{
			CombineEntry res = null;
			
			using (FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Solution"))) {
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					try {
						using (IProgressMonitor monitor = Runtime.TaskService.GetLoadProgressMonitor ()) {
							res = parentCombine.AddEntry (fdiag.Filename, monitor);
						}
					}
					catch {
						Runtime.MessageService.ShowError (string.Format (GettextCatalog.GetString ("The file '{0}' could not be loaded."), fdiag.Filename));
					}
				}

				fdiag.Hide ();
			}
			if (res != null)
				SaveCombine ();

			return res;
		}
		
		public ProjectFile CreateProjectFile (Project parentProject, string basePath)
		{
			NewFileDialog nfd = new NewFileDialog ();
			int res = nfd.Run ();
			nfd.Dispose ();
			if (res != (int) Gtk.ResponseType.Ok) return null;
			
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			int count = 1;
				
			string baseName  = Path.GetFileNameWithoutExtension(window.ViewContent.UntitledName);
			string extension = Path.GetExtension(window.ViewContent.UntitledName);
				
			// first try the default untitled name of the viewcontent filename
			string fileName = Path.Combine (basePath, baseName +  extension);
				
			// if it is already in the project, or it does exists we try to get a name that is
			// untitledName + Numer + extension
			while (parentProject.IsFileInProject (fileName) || System.IO.File.Exists (fileName)) {
				fileName = Path.Combine (basePath, baseName + count.ToString() + extension);
				++count;
			}

			// now we have a valid filename which we could use
			window.ViewContent.Save (fileName);
				
			ProjectFile newFileInformation = new ProjectFile(fileName, BuildAction.Compile);
			parentProject.ProjectFiles.Add (newFileInformation);
			return newFileInformation;
		}

		public bool AddReferenceToProject (Project project)
		{
			bool res = false;
			
			if (selDialog == null)
				selDialog = new SelectReferenceDialog(project);
			else
				selDialog.SetProject (project);

			if (selDialog.Run() == (int)Gtk.ResponseType.Ok) {
				ProjectReferenceCollection newRefs = selDialog.ReferenceInformations;
				
				ArrayList toDelete = new ArrayList ();
				foreach (ProjectReference refInfo in project.ProjectReferences)
					if (!newRefs.Contains (refInfo))
						toDelete.Add (refInfo);
				
				foreach (ProjectReference refInfo in toDelete)
						project.ProjectReferences.Remove (refInfo);

				foreach (ProjectReference refInfo in selDialog.ReferenceInformations)
					if (!project.ProjectReferences.Contains (refInfo))
						project.ProjectReferences.Add(refInfo);
				
				res = true;
			}
			selDialog.Hide ();
			return res;
		}
		
		public override void InitializeService()
		{
			base.InitializeService();

			formatManager.RegisterFileFormat (defaultProjectFormat);
			formatManager.RegisterFileFormat (defaultCombineFormat);
			
			FileFormatCodon[] formatCodons = (FileFormatCodon[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/ProjectFileFormats").BuildChildItems(null)).ToArray(typeof(FileFormatCodon));
			foreach (FileFormatCodon codon in formatCodons)
				formatManager.RegisterFileFormat (codon.FileFormat);
			
			DataContext.IncludeType (typeof(Combine));
			DataContext.IncludeType (typeof(Project));
			DataContext.IncludeType (typeof(DotNetProject));
			
			Runtime.FileService.FileRemoved += new FileEventHandler(CheckFileRemove);
			Runtime.FileService.FileRenamed += new FileEventHandler(CheckFileRename);
			
			projectBindings = (ProjectBindingCodon[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/ProjectBindings").BuildChildItems(null)).ToArray(typeof(ProjectBindingCodon));
			
			parserDatabase = Runtime.ParserService.CreateParserDatabase ();
			parserDatabase.TrackFileChanges = true;
			parserDatabase.ParseProgressMonitorFactory = new ParseProgressMonitorFactory (); 
		}

		void RestoreCombinePreferences (object data)
		{
			Combine combine = (Combine) data;
			string combinefilename = combine.FileName;
			string directory = Runtime.Properties.ConfigDirectory + "CombinePreferences";

			if (!Directory.Exists(directory)) {
				return;
			}
			
			string[] files = Directory.GetFiles(directory, combine.Name + "*.xml");
			
			if (files.Length > 0) {
				XmlDocument doc = new XmlDocument();
				try {
					doc.Load(files[0]);
				} catch (Exception) {
					return;
				}
				XmlElement root = doc.DocumentElement;
				string combinepath = Path.GetDirectoryName(combinefilename);
				if (root["Files"] != null) {
					foreach (XmlElement el in root["Files"].ChildNodes) {
						string fileName = fileUtilityService.RelativeToAbsolutePath(combinepath, el.Attributes["filename"].InnerText);
						if (File.Exists(fileName)) {
							Runtime.FileService.OpenFile (fileName, false);
						}
					}
				}
				
				if (root["Views"] != null) {
					foreach (XmlElement el in root["Views"].ChildNodes) {
						foreach (IPadContent view in WorkbenchSingleton.Workbench.PadContentCollection) {
							if (el.GetAttribute ("Id") == view.Id && view is IMementoCapable && el.ChildNodes.Count > 0) {
								IMementoCapable m = (IMementoCapable)view; 
								m.SetMemento((IXmlConvertable)m.CreateMemento().FromXmlElement((XmlElement)el.ChildNodes[0]));
							}
						}
					}
				}
				
				if (root["Properties"] != null) {
					IProperties properties = (IProperties)new DefaultProperties().FromXmlElement((XmlElement)root["Properties"].ChildNodes[0]);
					string name = properties.GetProperty("ActiveWindow", "");
					foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
						if (content.ContentName != null &&
							content.ContentName == name) {
							Runtime.DispatchService.GuiDispatch (new MessageHandler (content.WorkbenchWindow.SelectWindow));
							break;
						}
					}
					name = properties.GetProperty("ActiveConfiguration", "");
					IConfiguration conf = combine.GetConfiguration (name);
					if (conf != null)
						combine.ActiveConfiguration = conf;
				}
			} 
		}
		
		void SaveCombinePreferences (Combine combine)
		{
			string combinefilename = combine.FileName;
			string directory = Runtime.Properties.ConfigDirectory + "CombinePreferences";

			if (!Directory.Exists(directory)) {
				Runtime.FileService.CreateDirectory(directory);
			}
			string combinepath = Path.GetDirectoryName(combinefilename);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\"?>\n<UserCombinePreferences/>");
			
			XmlAttribute fileNameAttribute = doc.CreateAttribute("filename");
			fileNameAttribute.InnerText = combinefilename;
			doc.DocumentElement.Attributes.Append(fileNameAttribute);
			
			XmlElement filesnode = doc.CreateElement("Files");
			doc.DocumentElement.AppendChild(filesnode);
			
			foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (content.ContentName != null) {
					XmlElement el = doc.CreateElement("File");
					
					XmlAttribute attr = doc.CreateAttribute("filename");
					attr.InnerText = fileUtilityService.AbsoluteToRelativePath(combinepath, content.ContentName);
					el.Attributes.Append(attr);
					
					filesnode.AppendChild(el);
				}
			}
			
			XmlElement viewsnode = doc.CreateElement("Views");
			doc.DocumentElement.AppendChild(viewsnode);
			
			foreach (IPadContent view in WorkbenchSingleton.Workbench.PadContentCollection) {
				if (view is IMementoCapable) {
					XmlElement el = doc.CreateElement("ViewMemento");
					el.SetAttribute ("Id", view.Id);
					el.AppendChild(((IMementoCapable)view).CreateMemento().ToXmlElement(doc));
					viewsnode.AppendChild(el);
				}
			}
			
			IProperties properties = new DefaultProperties();
			string name = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow == null ? String.Empty : WorkbenchSingleton.Workbench.ActiveWorkbenchWindow.ViewContent.ContentName;
			properties.SetProperty("ActiveWindow", name == null ? String.Empty : name);
			properties.SetProperty("ActiveConfiguration", combine.ActiveConfiguration == null ? String.Empty : combine.ActiveConfiguration.Name);
			
			XmlElement propertynode = doc.CreateElement("Properties");
			doc.DocumentElement.AppendChild(propertynode);
			
			propertynode.AppendChild(properties.ToXmlElement(doc));
			
			fileUtilityService.ObservedSave(new NamedFileOperationDelegate(doc.Save), directory + Path.DirectorySeparatorChar + combine.Name + ".xml", FileErrorPolicy.ProvideAlternative);
		}
		
		//********* own events
		protected virtual void OnCombineOpened(CombineEventArgs e)
		{
			if (CombineOpened != null) {
				CombineOpened(this, e);
			}
		}

		protected virtual void OnCombineClosed(CombineEventArgs e)
		{
			if (CombineClosed != null) {
				CombineClosed(this, e);
			}
		}
		
		protected virtual void OnCurrentSelectedCombineChanged(CombineEventArgs e)
		{
			if (CurrentSelectedCombineChanged != null) {
				CurrentSelectedCombineChanged(this, e);
			}
		}
		
		protected virtual void OnCurrentProjectChanged(ProjectEventArgs e)
		{
			if (CurrentSelectedProject != null) {
				Runtime.StringParserService.Properties["PROJECTNAME"] = CurrentSelectedProject.Name;
			}
			if (CurrentProjectChanged != null) {
				CurrentProjectChanged(this, e);
			}
		}
		
		public Project GetProject (string projectName)
		{
			if (CurrentOpenCombine == null) return null;
			CombineEntryCollection allProjects = CurrentOpenCombine.GetAllProjects();
			foreach (Project project in allProjects) {
				if (project.Name == projectName)
					return project;
			}
			return null;
		}
		
		public void RemoveFileFromProject(string fileName)
		{
			if (openCombine != null) {
				if (Directory.Exists (fileName)) {
					RemoveAllInDirectory(fileName);
				} else {
					RemoveFileFromAllProjects(fileName);
				}
			}
		}
		
		public void TransferFiles (IProgressMonitor monitor, Project sourceProject, string sourcePath, Project targetProject, string targetPath, bool removeFromSource, bool copyOnlyProjectFiles)
		{
			if (targetProject == null)
				throw new ArgumentNullException ("targetProject");

			if (!targetPath.StartsWith (targetProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + targetPath);

			if (sourceProject != null && !sourcePath.StartsWith (sourceProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + sourcePath);
				
			if (copyOnlyProjectFiles && sourceProject == null)
				throw new ArgumentException ("A source project must be specified if copyOnlyProjectFiles is True");

			// Get the list of files to copy

			ICollection filesToMove;
			try {
				if (copyOnlyProjectFiles) {
					filesToMove = sourceProject.ProjectFiles.GetFilesInPath (sourcePath);
				} else {
					ProjectFileCollection col = new ProjectFileCollection ();
					GetAllFilesRecursive (sourcePath, col);
					filesToMove = col;
				}
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not get any file from '{0}'."), sourcePath), ex);
				return;
			}
			
			// Ensure that the destination folder is created, even if no files
			// are copied
			
			try {
				string newFolder = Path.Combine (targetPath, Path.GetFileName (sourcePath));
				if (Directory.Exists (sourcePath) && !Directory.Exists (newFolder))
					Runtime.FileService.CreateDirectory (newFolder);
			} catch (Exception ex) {
				monitor.ReportError (string.Format (GettextCatalog.GetString ("Could not create directory '{0}'."), targetPath), ex);
				return;
			}

			// Transfer files
			
			string basePath = Path.GetDirectoryName (sourcePath);
			monitor.BeginTask (GettextCatalog.GetString ("Copying files..."), filesToMove.Count);
			
			foreach (ProjectFile file in filesToMove) {
				string sourceFile = file.Name;
				string newFile = targetPath + sourceFile.Substring (basePath.Length);
				
				try {
					string fileDir = Path.GetDirectoryName (newFile);
					if (!Directory.Exists (fileDir))
						Runtime.FileService.CreateDirectory (fileDir);
					Runtime.FileService.CopyFile (sourceFile, newFile);
				} catch (Exception ex) {
					monitor.ReportError (string.Format (GettextCatalog.GetString ("File '{0}' could not be created."), newFile), ex);
					monitor.Step (1);
					continue;
				}
				
				if (sourceProject != null) {
					ProjectFile projectFile = sourceProject.ProjectFiles.GetFile (sourceFile);
					if (projectFile != null) {
						if (removeFromSource)
							sourceProject.ProjectFiles.Remove (projectFile);
						if (targetProject.ProjectFiles.GetFile (newFile) == null) {
							projectFile = (ProjectFile) projectFile.Clone ();
							projectFile.SetProject (null);
							projectFile.Name = newFile;
							targetProject.ProjectFiles.Add (projectFile);
						}
					}
				}
				
				if (removeFromSource) {
					try {
						Runtime.FileService.RemoveFile (sourceFile);
					} catch (Exception ex) {
						monitor.ReportError (string.Format (GettextCatalog.GetString ("File '{0}' could not be deleted."), sourceFile), ex);
					}
				}
				monitor.Step (1);
			}
			
			// If moving a folder, remove to source folder
			
			if (removeFromSource && Directory.Exists (sourcePath) && (
					!copyOnlyProjectFiles ||
					IsDirectoryHierarchyEmpty (sourcePath)))
			{
				try {
					Runtime.FileService.RemoveFile (sourcePath);
				} catch (Exception ex) {
					monitor.ReportError (string.Format (GettextCatalog.GetString ("Directory '{0}' could not be deleted."), sourcePath), ex);
				}
			}
			
			monitor.EndTask ();
		}
		
		void GetAllFilesRecursive (string path, ProjectFileCollection files)
		{
			if (File.Exists (path)) {
				files.Add (new ProjectFile (path));
				return;
			}
			
			foreach (string file in Directory.GetFiles (path))
				files.Add (new ProjectFile (file));
			
			foreach (string dir in Directory.GetDirectories (path))
				GetAllFilesRecursive (dir, files);
		}
		
		bool IsDirectoryHierarchyEmpty (string path)
		{
			if (Directory.GetFiles(path).Length > 0) return false;
			foreach (string dir in Directory.GetDirectories (path))
				if (!IsDirectoryHierarchyEmpty (dir)) return false;
			return true;
		}
		
		// All methods inside this class are gui thread safe
		
		class GuiHelper: GuiSyncObject
		{
			public void SetWorkbenchContext (WorkbenchContext ctx)
			{
				WorkbenchSingleton.Workbench.Context = ctx;
			}
		}

		void OnStartBuild()
		{
			if (StartBuild != null) {
				StartBuild(this, null);
			}
		}
		
		void OnEndBuild (bool success)
		{
			if (EndBuild != null) {
				EndBuild(success);
			}
		}
		
		void OnBeforeStartProject()
		{
			if (BeforeStartProject != null) {
				BeforeStartProject(this, null);
			}
		}
		
		void NotifyFileRemovedFromProject (object sender, ProjectFileEventArgs e)
		{
			OnFileRemovedFromProject (e);
		}
		
		void NotifyFileAddedToProject (object sender, ProjectFileEventArgs e)
		{
			OnFileAddedToProject (e);
		}

		internal void NotifyFileRenamedInProject (object sender, ProjectFileRenamedEventArgs e)
		{
			OnFileRenamedInProject (e);
		}		
		
		internal void NotifyFileChangedInProject (object sender, ProjectFileEventArgs e)
		{
			OnFileChangedInProject (e);
		}		
		
		internal void NotifyReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			OnReferenceAddedToProject (e);
		}
		
		internal void NotifyReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			OnReferenceRemovedFromProject (e);
		}
		
		protected virtual void OnFileRemovedFromProject (ProjectFileEventArgs e)
		{
			if (FileRemovedFromProject != null) {
				FileRemovedFromProject(this, e);
			}
		}

		protected virtual void OnFileAddedToProject (ProjectFileEventArgs e)
		{
			if (FileAddedToProject != null) {
				FileAddedToProject (this, e);
			}
		}

		protected virtual void OnFileRenamedInProject (ProjectFileRenamedEventArgs e)
		{
			if (FileRenamedInProject != null) {
				FileRenamedInProject (this, e);
			}
		}
		
		protected virtual void OnFileChangedInProject (ProjectFileEventArgs e)
		{
			if (FileChangedInProject != null) {
				FileChangedInProject (this, e);
			}
		}
		
		protected virtual void OnReferenceRemovedFromProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceRemovedFromProject != null) {
				ReferenceRemovedFromProject (this, e);
			}
		}
		
		protected virtual void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			if (ReferenceAddedToProject != null) {
				ReferenceAddedToProject (this, e);
			}
		}

		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		
		public event EventHandler     StartBuild;
		public event ProjectCompileEventHandler EndBuild;
		public event EventHandler     BeforeStartProject;
		
		
		public event CombineEventHandler CombineOpened;
		public event CombineEventHandler CombineClosed;
		public event CombineEventHandler CurrentSelectedCombineChanged;
		
		public event ProjectEventHandler       CurrentProjectChanged;
		
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
	}
	
	class ParseProgressMonitorFactory: IProgressMonitorFactory
	{
		public IProgressMonitor CreateProgressMonitor ()
		{
			return Runtime.TaskService.GetBackgroundProgressMonitor ("Code Completion Database Generation", "gtk-execute");
		}
	}
}
