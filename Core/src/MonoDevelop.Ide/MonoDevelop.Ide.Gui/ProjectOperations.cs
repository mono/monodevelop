
using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.CodeDom.Compiler;
using System.Collections.Specialized;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.ProgressMonitoring;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class ProjectOperations
	{
		IProjectService projectService = MonoDevelop.Projects.Services.ProjectService;
		IAsyncOperation currentBuildOperation = NullAsyncOperation.Success;
		IAsyncOperation currentRunOperation = NullAsyncOperation.Success;
		
		GuiHelper guiHelper = new GuiHelper ();
		SelectReferenceDialog selDialog = null;
		
		Project currentProject = null;
		Combine  currentCombine = null;
		Combine  openCombine    = null;
		IParserDatabase parserDatabase;

		ICompilerResult lastResult = new DefaultCompilerResult ();
		
		internal ProjectOperations ()
		{
			Services.FileService.FileRemoved += new FileEventHandler(CheckFileRemove);
			Services.FileService.FileRenamed += new FileEventHandler(CheckFileRename);
			
			parserDatabase = Services.ParserService.CreateParserDatabase ();
			parserDatabase.TrackFileChanges = true;
			parserDatabase.ParseProgressMonitorFactory = new ParseProgressMonitorFactory (); 
		}
		
		public IParserDatabase ParserDatabase {
			get { return parserDatabase; }
		}

		public ICompilerResult LastCompilerResult {
			get { return lastResult; }
		}
		
		bool IsDirtyFileInCombine {
			get {
				CombineEntryCollection projects = openCombine.GetAllProjects();
				
				foreach (Project projectEntry in projects) {
					foreach (ProjectFile fInfo in projectEntry.ProjectFiles) {
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
		
		public bool NeedsCompiling {
			get {
				if (openCombine == null) {
					return false;
				}
				return openCombine.NeedsBuilding || IsDirtyFileInCombine;
			}
		}
		
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
		
		public void SaveCombinePreferences ()
		{
			if (CurrentOpenCombine != null)
				SaveCombinePreferences (CurrentOpenCombine);
		}
		
		public void CloseCombine()
		{
			CloseCombine (true);
		}

		public void CloseCombine (bool saveCombinePreferencies)
		{
			if (CurrentOpenCombine != null) {
				if (saveCombinePreferencies)
					SaveCombinePreferences ();
				Combine closedCombine = CurrentOpenCombine;
				CurrentSelectedProject = null;
				CurrentOpenCombine = CurrentSelectedCombine = null;
				IdeApp.Workbench.CloseAllDocuments ();
				
				parserDatabase.Unload (closedCombine);

				OnCombineClosed(new CombineEventArgs(closedCombine));
				closedCombine.Dispose();
			}
		}
		
		public IAsyncOperation OpenCombine(string filename)
		{
			if (openCombine != null) {
				SaveCombine();
				CloseCombine();
			}

			if (filename.StartsWith ("file://"))
				filename = filename.Substring (7);

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor ();
			
			object[] data = new object[] { filename, monitor };
			Services.DispatchService.BackgroundDispatch (new StatefulMessageHandler (backgroundLoadCombine), data);
			return monitor.AsyncOperation;
		}
		
		void backgroundLoadCombine (object arg)
		{
			object[] data = (object[]) arg;
			string filename = data[0] as string;
			IProgressMonitor monitor = data [1] as IProgressMonitor;
			
			try {
				if (!File.Exists (filename)) {
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
			
				CombineEntry entry = projectService.ReadFile (filename, monitor);
				if (!(entry is Combine)) {
					Combine loadingCombine = new Combine();
					loadingCombine.Entries.Add (entry);
					loadingCombine.Name = entry.Name;
					loadingCombine.Save (validcombine, monitor);
					entry = loadingCombine;
				}
			
				openCombine = (Combine) entry;
				
				IdeApp.Workbench.RecentOpen.AddLastProject (filename, openCombine.Name);
				
				openCombine.FileAddedToProject += new ProjectFileEventHandler (NotifyFileAddedToProject);
				openCombine.FileRemovedFromProject += new ProjectFileEventHandler (NotifyFileRemovedFromProject);
				openCombine.FileRenamedInProject += new ProjectFileRenamedEventHandler (NotifyFileRenamedInProject);
				openCombine.FileChangedInProject += new ProjectFileEventHandler (NotifyFileChangedInProject);
				openCombine.ReferenceAddedToProject += new ProjectReferenceEventHandler (NotifyReferenceAddedToProject);
				openCombine.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (NotifyReferenceRemovedFromProject);
				
				SearchForNewFiles ();

				parserDatabase.Load (openCombine);
				OnCombineOpened(new CombineEventArgs(openCombine));

				Services.DispatchService.GuiDispatch (new StatefulMessageHandler (RestoreCombinePreferences), CurrentOpenCombine);
				
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
					SearchNewFiles (p);
			}
		}
		
		void SearchNewFiles (Project project)
		{
			StringCollection newFiles   = new StringCollection();
			StringCollection collection = Runtime.FileUtilityService.SearchDirectory (project.BaseDirectory, "*");

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
						project.ProjectFiles.Add(newFile);
					}
				} else {
					Services.DispatchService.GuiDispatch (new MessageHandler (new IncludeFilesDialog (project, newFiles).ShowDialog));
				}
			}
		}				
		
		public void SaveCombine()
		{
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor ();
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

		void CheckFileRemove(object sender, FileEventArgs e)
		{
			if (openCombine != null)
				openCombine.RemoveFileFromProjects (e.FileName);
		}
		
		void CheckFileRename(object sender, FileEventArgs e)
		{
			if (openCombine != null)
				openCombine.RenameFileInProjects (e.SourceFile, e.TargetFile);
		}
		
		public void Deploy (Project project)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.IsDirty)
					doc.Save();
			}
			DeployInformation.Deploy (project);
		}

		public void ShowOptions (CombineEntry entry)
		{
			if (entry is Project) {
				Project selectedProject = (Project) entry;
				
				IAddInTreeNode generalOptionsNode          = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/ProjectOptions/GeneralOptions");
				IAddInTreeNode configurationPropertiesNode = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/ProjectOptions/ConfigurationProperties");
				
				ProjectOptionsDialog optionsDialog = new ProjectOptionsDialog (IdeApp.Workbench.RootWindow, selectedProject, generalOptionsNode, configurationPropertiesNode);
				if (optionsDialog.Run() == (int)Gtk.ResponseType.Ok) {
					selectedProject.NeedsBuilding = true;
				}
			} else if (entry is Combine) {
				Combine combine = (Combine) entry;
				
				IAddInTreeNode generalOptionsNode          = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/CombineOptions/GeneralOptions");
				IAddInTreeNode configurationPropertiesNode = AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/CombineOptions/ConfigurationProperties");
				
				CombineOptionsDialog optionsDialog = new CombineOptionsDialog (IdeApp.Workbench.RootWindow, combine, generalOptionsNode, configurationPropertiesNode);
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
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor ();
				try {
					if (createCombine)
						res = parentCombine.AddEntry (npdlg.NewCombineLocation, monitor);
					else
						res = parentCombine.AddEntry (npdlg.NewProjectLocation, monitor);
				}
				catch {
					Services.MessageService.ShowError (string.Format (GettextCatalog.GetString ("The file '{0}' could not be loaded."), npdlg.NewProjectLocation));
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
						using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor ()) {
							res = parentCombine.AddEntry (fdiag.Filename, monitor);
						}
					}
					catch {
						Services.MessageService.ShowError (string.Format (GettextCatalog.GetString ("The file '{0}' could not be loaded."), fdiag.Filename));
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

			Document doc = IdeApp.Workbench.ActiveDocument;
			int count = 1;
				
			string baseName  = Path.GetFileNameWithoutExtension (doc.Window.ViewContent.UntitledName);
			string extension = Path.GetExtension (doc.Window.ViewContent.UntitledName);
				
			// first try the default untitled name of the viewcontent filename
			string fileName = Path.Combine (basePath, baseName +  extension);
				
			// if it is already in the project, or it does exists we try to get a name that is
			// untitledName + Numer + extension
			while (parentProject.IsFileInProject (fileName) || System.IO.File.Exists (fileName)) {
				fileName = Path.Combine (basePath, baseName + count.ToString() + extension);
				++count;
			}

			// now we have a valid filename which we could use
			doc.SaveAs (fileName);
				
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
						string fileName = Runtime.FileUtilityService.RelativeToAbsolutePath(combinepath, el.Attributes["filename"].InnerText);
						if (File.Exists(fileName)) {
							IdeApp.Workbench.OpenDocument (fileName, false);
						}
					}
				}
				
				if (root["Views"] != null) {
					foreach (XmlElement el in root["Views"].ChildNodes) {
						foreach (Pad pad in IdeApp.Workbench.Pads) {
							if (el.GetAttribute ("Id") == pad.Id && pad.Content is IMementoCapable && el.ChildNodes.Count > 0) {
								IMementoCapable m = (IMementoCapable) pad.Content; 
								m.SetMemento((IXmlConvertable)m.CreateMemento().FromXmlElement((XmlElement)el.ChildNodes[0]));
							}
						}
					}
				}
				
				if (root["Properties"] != null) {
					IProperties properties = (IProperties)new DefaultProperties().FromXmlElement((XmlElement)root["Properties"].ChildNodes[0]);
					string name = properties.GetProperty("ActiveWindow", "");
					foreach (Document document in IdeApp.Workbench.Documents) {
						if (document.FileName != null &&
							document.FileName == name) {
							Services.DispatchService.GuiDispatch (new MessageHandler (document.Select));
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
				Services.FileService.CreateDirectory(directory);
			}
			string combinepath = Path.GetDirectoryName(combinefilename);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<?xml version=\"1.0\"?>\n<UserCombinePreferences/>");
			
			XmlAttribute fileNameAttribute = doc.CreateAttribute("filename");
			fileNameAttribute.InnerText = combinefilename;
			doc.DocumentElement.Attributes.Append(fileNameAttribute);
			
			XmlElement filesnode = doc.CreateElement("Files");
			doc.DocumentElement.AppendChild(filesnode);
			
			foreach (Document document in IdeApp.Workbench.Documents) {
				if (document.FileName != null) {
					XmlElement el = doc.CreateElement("File");
					
					XmlAttribute attr = doc.CreateAttribute("filename");
					attr.InnerText = Runtime.FileUtilityService.AbsoluteToRelativePath (combinepath, document.FileName);
					el.Attributes.Append(attr);
					
					filesnode.AppendChild (el);
				}
			}
			
			XmlElement viewsnode = doc.CreateElement("Views");
			doc.DocumentElement.AppendChild(viewsnode);
			
			foreach (Pad pad in IdeApp.Workbench.Pads) {
				if (pad.Content is IMementoCapable) {
					XmlElement el = doc.CreateElement("ViewMemento");
					el.SetAttribute ("Id", pad.Id);
					el.AppendChild(((IMementoCapable)pad.Content).CreateMemento().ToXmlElement(doc));
					viewsnode.AppendChild(el);
				}
			}
			
			IProperties properties = new DefaultProperties();
			string name = IdeApp.Workbench.ActiveDocument == null ? String.Empty : IdeApp.Workbench.ActiveDocument.FileName;
			properties.SetProperty("ActiveWindow", name == null ? String.Empty : name);
			properties.SetProperty("ActiveConfiguration", combine.ActiveConfiguration == null ? String.Empty : combine.ActiveConfiguration.Name);
			
			XmlElement propertynode = doc.CreateElement("Properties");
			doc.DocumentElement.AppendChild(propertynode);
			
			propertynode.AppendChild(properties.ToXmlElement(doc));
			
			Runtime.FileUtilityService.ObservedSave(new NamedFileOperationDelegate(doc.Save), directory + Path.DirectorySeparatorChar + combine.Name + ".xml", FileErrorPolicy.ProvideAlternative);
		}

		public IAsyncOperation Execute (CombineEntry entry)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);

			Services.DispatchService.ThreadDispatch (new StatefulMessageHandler (ExecuteCombineEntryAsync), new object[] {entry, monitor, context});
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
			if (Services.DebuggingService == null)
				return NullAsyncOperation.Failure;
			
			if (currentRunOperation != null && !currentRunOperation.IsCompleted)
				return currentRunOperation;
			
			guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (Services.DebuggingService.GetExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			
			Services.DispatchService.ThreadDispatch (new StatefulMessageHandler (DebugCombineEntryAsync), new object[] {entry, monitor, context});
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
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Debug (tempProject);
				ProjectOperationHandler h = new ProjectOperationHandler ();
				h.Project = tempProject;
				aop.Completed += new OperationHandler (h.Run);
				return aop;
			} else {
				Services.MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation DebugApplication (string executableFile)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;
			if (Services.DebuggingService == null) return NullAsyncOperation.Failure;
			
			guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();

			Services.DebuggingService.Run ((IConsole) monitor, new string[] { executableFile });
			
			DebugApplicationStopper disposer = new DebugApplicationStopper ();
			disposer.Monitor = monitor;
			Services.DebuggingService.StoppedEvent += new EventHandler (disposer.Run);
			
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
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Build (tempProject);
				ProjectOperationHandler h = new ProjectOperationHandler ();
				h.Project = tempProject;
				aop.Completed += new OperationHandler (h.Run);
				return aop;
			} else {
				Services.MessageService.ShowError (string.Format (GettextCatalog.GetString ("The file {0} can't be compiled."), file));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation ExecuteFile (string file)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject);
				ProjectOperationHandler h = new ProjectOperationHandler ();
				h.Project = tempProject;
				aop.Completed += new OperationHandler (h.Run);
				return aop;
			} else {
				Services.MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
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
			
			DoBeforeCompileAction ();

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
			Services.DispatchService.ThreadDispatch (new StatefulMessageHandler (BuildCombineEntryAsync), new object[] {entry, monitor});
			currentBuildOperation = monitor.AsyncOperation;
			return currentBuildOperation;
		}
		
		void BuildCombineEntryAsync (object ob)
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

		void DoBeforeCompileAction ()
		{
			BeforeCompileAction action = (BeforeCompileAction)Runtime.Properties.GetProperty("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", BeforeCompileAction.SaveAllFiles);
			
			switch (action) {
				case BeforeCompileAction.Nothing:
					break;
				case BeforeCompileAction.PromptForSave:
					foreach (Document doc in IdeApp.Workbench.Documents) {
						if (doc.IsDirty) {
							if (Services.MessageService.AskQuestion(GettextCatalog.GetString ("Save changed files?"))) {
								MarkFileDirty (doc.FileName);
								doc.Save ();
							}
							else
								break;
						}
					}
					break;
				case BeforeCompileAction.SaveAllFiles:
					IdeApp.Workbench.SaveAll ();
					break;
				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
		}

		void BeginBuild ()
		{
			Services.TaskService.ClearTasks();
			OnStartBuild ();
		}
		
		void BuildDone (IProgressMonitor monitor, ICompilerResult result)
		{
			lastResult = result;
			monitor.Log.WriteLine ();
			monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("---------------------- Done ----------------------")));
			
			foreach (CompilerError err in result.CompilerResults.Errors) {
				Services.TaskService.AddTask (new Task(null, err));
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
					Services.FileService.CreateDirectory (newFolder);
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
						Services.FileService.CreateDirectory (fileDir);
					Services.FileService.CopyFile (sourceFile, newFile);
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
							projectFile.Name = newFile;
							targetProject.ProjectFiles.Add (projectFile);
						}
					}
				}
				
				if (removeFromSource) {
					try {
						Services.FileService.RemoveFile (sourceFile);
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
					Services.FileService.RemoveFile (sourcePath);
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

		void OnBeforeStartProject()
		{
			if (BeforeStartProject != null) {
				BeforeStartProject(this, null);
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
		
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		
		public event EventHandler StartBuild;
		public event ProjectCompileEventHandler EndBuild;
		public event EventHandler BeforeStartProject;
		
		public event CombineEventHandler CombineOpened;
		public event CombineEventHandler CombineClosed;
		public event CombineEventHandler CurrentSelectedCombineChanged;
		
		public event ProjectEventHandler CurrentProjectChanged;
		
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;

		
		// All methods inside this class are gui thread safe
		
		class GuiHelper: GuiSyncObject
		{
			public void SetWorkbenchContext (WorkbenchContext ctx)
			{
				IdeApp.Workbench.Context = ctx;
			}
		}
	}
	
	class ParseProgressMonitorFactory: IProgressMonitorFactory
	{
		public IProgressMonitor CreateProgressMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Code Completion Database Generation", "gtk-execute");
		}
	}
}
