//
// ProjectOperations.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.CodeDom.Compiler;
using System.Collections.Specialized;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Mono.Addins;
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
		
		CombineEntry currentEntry = null;
		Project currentProject = null;
		Combine currentCombine = null;
		Combine openCombine = null;
		object currentItem;
		
		IParserDatabase parserDatabase;
		CodeRefactorer refactorer;

		ICompilerResult lastResult = new DefaultCompilerResult ();
		
		ProjectFileEventHandler fileAddedToProjectHandler;
		ProjectFileEventHandler fileRemovedFromProjectHandler;
		ProjectFileRenamedEventHandler fileRenamedInProjectHandler;
		ProjectFileEventHandler fileChangedInProjectHandler;
		ProjectFileEventHandler filePropertyChangedInProjectHandler;
		ProjectReferenceEventHandler referenceAddedToProjectHandler;
		ProjectReferenceEventHandler referenceRemovedFromProjectHandler;
		CombineEntryChangeEventHandler entryAddedToCombineHandler;
		CombineEntryChangeEventHandler entryRemovedFromCombineHandler;
		
		internal ProjectOperations ()
		{
			fileAddedToProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFileAddedToProject));
			fileRemovedFromProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFileRemovedFromProject));
			fileRenamedInProjectHandler = (ProjectFileRenamedEventHandler) DispatchService.GuiDispatch (new ProjectFileRenamedEventHandler (NotifyFileRenamedInProject));
			fileChangedInProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFileChangedInProject));
			filePropertyChangedInProjectHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (NotifyFilePropertyChangedInProject));
			referenceAddedToProjectHandler = (ProjectReferenceEventHandler) DispatchService.GuiDispatch (new ProjectReferenceEventHandler (NotifyReferenceAddedToProject));
			referenceRemovedFromProjectHandler = (ProjectReferenceEventHandler) DispatchService.GuiDispatch (new ProjectReferenceEventHandler (NotifyReferenceRemovedFromProject));
		
			entryAddedToCombineHandler = (CombineEntryChangeEventHandler) DispatchService.GuiDispatch (new CombineEntryChangeEventHandler (NotifyEntryAddedToCombine));
			entryRemovedFromCombineHandler = (CombineEntryChangeEventHandler) DispatchService.GuiDispatch (new CombineEntryChangeEventHandler (NotifyEntryRemovedFromCombine));
			
			FileService.FileRemoved += (EventHandler<FileEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileEventArgs> (CheckFileRemove));
			FileService.FileRenamed += (EventHandler<FileCopyEventArgs>) DispatchService.GuiDispatch (new EventHandler<FileCopyEventArgs> (CheckFileRename));
			
			GLib.Timeout.Add (2000, OnRunProjectChecks);
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
		
		public CodeRefactorer CodeRefactorer {
			get {
				if (refactorer == null) {
					refactorer = new CodeRefactorer (openCombine, ParserDatabase);
					refactorer.TextFileProvider = new OpenDocumentFileProvider ();
				}
				
				return refactorer;
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
			internal set {
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
			internal set {
				if (value != currentCombine) {
					System.Diagnostics.Debug.Assert(openCombine != null);
					currentCombine = value;
					OnCurrentSelectedCombineChanged(new CombineEventArgs(currentCombine));
				}
			}
		}
		
		public CombineEntry CurrentSelectedCombineEntry {
			get {
				return currentEntry;
			}
			internal set {
				currentEntry = value;
			}
		}
		
		public object CurrentSelectedItem {
			get {
				return currentItem;
			}
			internal set {
				currentItem = value;
			}
		}
		
		public string ProjectsDefaultPath {
			get {
				return PropertyService.Get ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Projects"));
			}
			set {
				PropertyService.Set ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", value);
			}
		}
		
		public Project GetProjectContaining (string fileName)
		{
			if (this.openCombine == null)
				return null;
			foreach (Project p in openCombine.GetAllProjects ())
				if (p.GetProjectFile (fileName) != null)
					return p;
			return null;
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
		
		string GetDeclaredFile(ILanguageItem item)
		{			
			if (item is IMember) {
				IMember mem = (IMember) item;				
				if (mem.Region == null)
					return null;
				else if (mem.Region.FileName != null)
					return mem.Region.FileName;
				else if (mem.DeclaringType != null) {
					foreach (IClass c in mem.DeclaringType.Parts) {
						if ((mem is IField && c.Fields.Contains((IField)mem)) ||
						    (mem is IEvent && c.Events.Contains((IEvent)mem)) || 
						    (mem is IProperty  && c.Properties.Contains((IProperty)mem)) ||
						    (mem is IMethod && c.Methods.Contains((IMethod)mem))) {
							return GetClassFileName(c);							
						}                                   
					}
				}
			} else if (item is IClass) {
				IClass cls = (IClass) item;
				return GetClassFileName (cls);
			} else if (item is LocalVariable) {
				LocalVariable cls = (LocalVariable) item;
				return cls.Region.FileName;
			}
			return null;
		}
		
		public bool CanJumpToDeclaration (ILanguageItem item)
		{
			return (GetDeclaredFile(item) != null);
		}
		
		public void JumpToDeclaration (ILanguageItem item)
		{
			String file;
			if ((file = GetDeclaredFile(item)) == null)
				return;
			if (item is IMember) {
				IMember mem = (IMember) item;
				IdeApp.Workbench.OpenDocument (file, mem.Region.BeginLine, mem.Region.BeginColumn, true);
			} else if (item is IClass) {
				IClass cls = (IClass) item;
				IdeApp.Workbench.OpenDocument (file, cls.Region.BeginLine, cls.Region.BeginColumn, true);
			} else if (item is LocalVariable) {
				LocalVariable lvar = (LocalVariable) item;
				IdeApp.Workbench.OpenDocument (file, lvar.Region.BeginLine, lvar.Region.BeginColumn, true);
			}
		}
		
		string GetClassFileName (IClass cls)
		{
			if (cls.Region != null && cls.Region.FileName != null)
				return cls.Region.FileName;
			if (cls.DeclaredIn is IClass)
				return GetClassFileName ((IClass) cls.DeclaredIn);
			else
				return null;
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
				
				//stop all operations associated with this combine
				if (!CurrentBuildOperation.IsCompleted)
					CurrentBuildOperation.Cancel ();
				if (!CurrentRunOperation.IsCompleted)
					CurrentRunOperation.Cancel ();

				closedCombine.FileAddedToProject -= fileAddedToProjectHandler;
				closedCombine.FileRemovedFromProject -= fileRemovedFromProjectHandler;
				closedCombine.FileRenamedInProject -= fileRenamedInProjectHandler;
				closedCombine.FileChangedInProject -= fileChangedInProjectHandler;
				closedCombine.FilePropertyChangedInProject -= filePropertyChangedInProjectHandler;
				closedCombine.ReferenceAddedToProject -= referenceAddedToProjectHandler;
				closedCombine.ReferenceRemovedFromProject -= referenceRemovedFromProjectHandler;
				closedCombine.EntryAddedToCombine -= entryAddedToCombineHandler;
				closedCombine.EntryRemovedFromCombine -= entryRemovedFromCombineHandler;

				CurrentOpenCombine = CurrentSelectedCombine = null;
				CurrentSelectedCombineEntry = null;
				refactorer = null;
				
				Document[] docs = new Document [IdeApp.Workbench.Documents.Count];
				IdeApp.Workbench.Documents.CopyTo (docs, 0);
				foreach (Document doc in docs) {
					if (doc.HasProject)
						doc.Close ();
				}
				
				ParserDatabase.Unload (closedCombine);

				OnCombineClosed(new CombineEventArgs(closedCombine));
				OnEntryUnloaded (closedCombine);
				closedCombine.Dispose();
			}
		}
		
		public IAsyncOperation OpenCombine(string filename)
		{
			if (openCombine != null)
				CloseCombine();

			if (filename.StartsWith ("file://"))
				filename = new Uri(filename).LocalPath;

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true);
			
			object[] data = new object[] { filename, monitor };
			DispatchService.BackgroundDispatch (new StatefulMessageHandler (backgroundLoadCombine), data);
			return monitor.AsyncOperation;
		}
		
		void backgroundLoadCombine (object arg)
		{
			object[] data = (object[]) arg;
			string filename = data[0] as string;
			IProgressMonitor monitor = data [1] as IProgressMonitor;
			
			try {
				if (!File.Exists (filename)) {
					monitor.ReportError (GettextCatalog.GetString ("File not found: {0}", filename), null);
					monitor.Dispose ();
					return;
				}
				
				string validcombine = Path.ChangeExtension (filename, ".mds");
				if (Path.GetExtension (filename).ToLower () == ".mdp") {
					if (File.Exists (validcombine))
						filename = validcombine;
				}
			
				CombineEntry entry = projectService.ReadCombineEntry (filename, monitor);
				if (monitor.IsCancelRequested) {
					monitor.Dispose ();
					return;
				}

				if (!(entry is Combine)) {
					Combine loadingCombine = new Combine();
					loadingCombine.Entries.Add (entry);
					loadingCombine.Name = entry.Name;
					loadingCombine.Save (validcombine, monitor);
					entry = loadingCombine;
				}
			
				openCombine = (Combine) entry;
				
				IdeApp.Workbench.RecentOpen.AddLastProject (filename, openCombine.Name);
		
				openCombine.FileAddedToProject += fileAddedToProjectHandler;
				openCombine.FileRemovedFromProject += fileRemovedFromProjectHandler;
				openCombine.FileRenamedInProject += fileRenamedInProjectHandler;
				openCombine.FileChangedInProject += fileChangedInProjectHandler;
				openCombine.FilePropertyChangedInProject += filePropertyChangedInProjectHandler;
				openCombine.ReferenceAddedToProject += referenceAddedToProjectHandler;
				openCombine.ReferenceRemovedFromProject += referenceRemovedFromProjectHandler;
				openCombine.EntryAddedToCombine += entryAddedToCombineHandler;
				openCombine.EntryRemovedFromCombine += entryRemovedFromCombineHandler;
				
				SearchForNewFiles ();

				ParserDatabase.Load (openCombine);
				
			} catch (Exception ex) {
				monitor.ReportError ("Load operation failed.", ex);
				monitor.Dispose ();
				return;
			}
			
			Gtk.Application.Invoke (delegate {
				using (monitor) {
					OnEntryLoaded (openCombine);
					OnCombineOpened (new CombineEventArgs (openCombine));
					RestoreCombinePreferences (openCombine);
					monitor.ReportSuccess (GettextCatalog.GetString ("Solution loaded."));
				}
			});
		}
		
		void OnEntryLoaded (CombineEntry entry)
		{
			if (entry is Combine) {
				foreach (CombineEntry ce in ((Combine)entry).Entries)
					OnEntryLoaded (ce);
			}
		}
		
		void OnEntryUnloaded (CombineEntry entry)
		{
			if (entry is Combine) {
				foreach (CombineEntry ce in ((Combine)entry).Entries)
					OnEntryUnloaded (ce);
			}
		}
		
		bool OnRunProjectChecks ()
		{
			// If any project has been modified, reload it
			if (openCombine != null)
				OnCheckProject (openCombine);
			return true;
		}
		
		void OnCheckProject (CombineEntry entry)
		{
			if (entry.NeedsReload) {
				bool warn = false;
				if (entry is Project) {
					warn = HasOpenDocuments ((Project) entry, false);
				} else if (entry is Combine) {
					foreach (Project p in ((Combine)entry).GetAllProjects ()) {
						if (HasOpenDocuments (p, false)) {
							warn = true;
							break;
						}
					}
				}
				
				if (!warn || MessageService.Confirm (GettextCatalog.GetString ("The project '{0}' has been modified by an external application. Do you want to reload it? All project files will be closed.", entry.Name), AlertButton.Reload)) {
					if (entry == openCombine) {
						string file = openCombine.FileName;
						CloseCombine (true);
						OpenCombine (file);
					}
					else {
						using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true)) {
							entry.ParentCombine.ReloadEntry (m, entry);
						}
					}

					if (entry is Combine)
						return;
				} else
					entry.NeedsReload = false;
			}
			
			if (entry is Combine) {
				ArrayList ens = new ArrayList ();
				foreach (CombineEntry ce in ((Combine)entry).Entries)
					ens.Add (ce);
				foreach (CombineEntry ce in ens)
					OnCheckProject (ce);
			}
		}
		
		internal bool HasOpenDocuments (Project project, bool modifiedOnly)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.Project == project && (!modifiedOnly || doc.IsDirty))
					return true;
			}
			return false;
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
						project.ProjectFiles.Add(newFile);
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
		
		public void Export (CombineEntry entry)
		{
			Export (entry, null);
		}
		
		public void Export (CombineEntry entry, IFileFormat format)
		{
			ExportProjectDialog dlg = new ExportProjectDialog (entry, format);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					
					using (IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (GettextCatalog.GetString ("Export Project"), null, true, true)) {
						string folder = dlg.TargetFolder;
						
						Services.ProjectService.Export (mon, entry.FileName, folder, dlg.Format);
					}
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		public void SaveCombine()
		{
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				openCombine.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Solution saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void SaveCombineEntry (CombineEntry entry)
		{
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				entry.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Project saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void SaveProject (Project project)
		{
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				project.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Project saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void MarkFileDirty (string filename)
		{
			if (openCombine != null) {
				Project entry = openCombine.GetProjectContainingFile (filename);
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
		
		void CheckFileRename(object sender, FileCopyEventArgs e)
		{
			if (openCombine != null)
				openCombine.RenameFileInProjects (e.SourceFile, e.TargetFile);
		}
		
		public void ShowOptions (CombineEntry entry)
		{
			ShowOptions (entry, null);
		}
		
		public void ShowOptions (CombineEntry entry, string panelId)
		{
			if (entry is Project) {
				Project selectedProject = (Project) entry;
				
				ExtensionNode generalOptionsNode = AddinManager.GetExtensionNode ("/MonoDevelop/ProjectModel/Gui/ProjectOptions/GeneralOptions");
				ExtensionNode configurationPropertiesNode = AddinManager.GetExtensionNode ("/MonoDevelop/ProjectModel/Gui/ProjectOptions/ConfigurationOptions");
				
				using (ProjectOptionsDialog optionsDialog = new ProjectOptionsDialog (IdeApp.Workbench.RootWindow, selectedProject, generalOptionsNode, configurationPropertiesNode)) {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					
					if (optionsDialog.Run() == (int)Gtk.ResponseType.Ok) {
						selectedProject.NeedsBuilding = true;
						SaveProject (selectedProject);
					}
				}
			} else if (entry is Combine) {
				Combine combine = (Combine) entry;
				
				ExtensionNode generalOptionsNode = AddinManager.GetExtensionNode ("/MonoDevelop/ProjectModel/Gui/CombineOptions/GeneralOptions");
				ExtensionNode configurationPropertiesNode = AddinManager.GetExtensionNode ("/MonoDevelop/ProjectModel/Gui/CombineOptions/ConfigurationOptions");
				
				using (CombineOptionsDialog optionsDialog = new CombineOptionsDialog (IdeApp.Workbench.RootWindow, combine, generalOptionsNode, configurationPropertiesNode)) {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (optionsDialog.Run () == (int) Gtk.ResponseType.Ok)
						SaveCombine ();
				}
			}
		}
		
		public void NewProject ()
		{
			NewProjectDialog pd = new NewProjectDialog (null, true, true, null);
			pd.Run ();
			pd.Destroy ();
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
			string basePath = parentCombine != null ? parentCombine.BaseDirectory : null;
			NewProjectDialog npdlg = new NewProjectDialog (parentCombine, parentCombine == null, createCombine, basePath);
			if (createCombine && parentCombine != null)
				npdlg.SelectTemplate ("MonoDevelop.BlankSolution");

			npdlg.Run ();
			npdlg.Destroy ();
			return res;
		}

		public CombineEntry AddCombineEntry (Combine parentCombine)
		{
			CombineEntry res = null;
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Solution"));
			try {
				fdiag.SetCurrentFolder (parentCombine.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					try {
						res = AddCombineEntry (parentCombine, fdiag.Filename);
					}
					catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", fdiag.Filename));
					}
				}
			} finally {
				fdiag.Destroy ();
			}
			
			if (res != null)
				SaveCombine ();

			return res;
		}
		
		public CombineEntry AddCombineEntry (Combine combine, string entryFileName)
		{
			AddEntryEventArgs args = new AddEntryEventArgs (combine, entryFileName);
			if (AddingEntryToCombine != null)
				AddingEntryToCombine (this, args);
			if (args.Cancel)
				return null;
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
				return combine.AddEntry (args.FileName, monitor);
			}
		}

		public void CreateProjectFile (Project parentProject, string basePath)
		{
			CreateProjectFile (parentProject, basePath, null);
		}
		
		public void CreateProjectFile (Project parentProject, string basePath, string selectedTemplateId)
		{
			using (NewFileDialog nfd = new NewFileDialog (parentProject, basePath)) {
				if (selectedTemplateId != null)
					nfd.SelectTemplate (selectedTemplateId);
				nfd.Run ();
				nfd.Dispose ();
			}
		}

		public bool AddReferenceToProject (Project project)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
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
					
					return true;
				}
				else
					return false;
			} finally {
				selDialog.Hide ();
			}
		}
		
		public bool SelectProjectReferences (ProjectReferenceCollection references, ClrVersion targetVersion)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
				selDialog.SetReferenceCollection (references, targetVersion);

				if (selDialog.Run() == (int)Gtk.ResponseType.Ok) {
					references.Clear ();
					references.AddRange (selDialog.ReferenceInformations);
					return true;
				}
				else
					return false;
			} finally {
				if (selDialog != null)
					selDialog.Hide ();
			}
		}
		
		const string UserCombinePreferencesNode = "UserCombinePreferences";
		const string VersionAttribute           = "version";
		const string Version                    = "1.0";
		const string FilesNode                  = "Files";
		const string FileNode                   = "File";
		const string FileNameAttribute          = "name";
		const string FileLineAttribute          = "line";
		const string FileColumnAttribute        = "column";
		const string ViewsNode                  = "Views";
		const string ViewMementoNode            = "ViewMemento";
		const string IdAttribute                = "id";

		void RestoreCombinePreferences (object data)
		{
			Combine combine = (Combine) data;
			string preferencesFileName = GetPreferencesFileName (combine);
			if (!File.Exists(preferencesFileName))
				return;
			XmlTextReader reader = new XmlTextReader (preferencesFileName);
			try {
				bool invalid = false;
				XmlReadHelper.ReadList (reader, UserCombinePreferencesNode, delegate() {
					if (invalid)
						return true;
					switch (reader.LocalName) {
						case UserCombinePreferencesNode:
							if (reader.GetAttribute (VersionAttribute) != Version)
								invalid = true;
							return true;
						case FilesNode:
							XmlReadHelper.ReadList (reader, FilesNode, delegate() {
								switch (reader.LocalName) {
								case FileNode:
									string fileName = FileService.RelativeToAbsolutePath (Path.GetDirectoryName (combine.FileName), reader.GetAttribute (FileNameAttribute));
									int lin=0, col=0;
									int.TryParse (reader.GetAttribute (FileLineAttribute), out lin);
									int.TryParse (reader.GetAttribute (FileColumnAttribute), out col);
									if (File.Exists(fileName))
										IdeApp.Workbench.OpenDocument (fileName, lin, col, false);
									return true;
								}
								return false;
							});
							return true;
						case ViewsNode:
							XmlReadHelper.ReadList (reader, ViewsNode, delegate() {
								switch (reader.LocalName) {
								case ViewMementoNode:
									string id = reader.GetAttribute (IdAttribute);
									string raw = reader.ReadInnerXml ();
									foreach (Pad pad in IdeApp.Workbench.Pads) {
										if (id == pad.Id && pad.Content is IMementoCapable) {
											IMementoCapable m = (IMementoCapable) pad.Content; 
											XmlReader innerReader = new XmlTextReader (new MemoryStream (System.Text.Encoding.UTF8.GetBytes (raw)));
											try {
												while (innerReader.Read () && innerReader.NodeType != XmlNodeType.Element) 
													;
												m.SetMemento ((ICustomXmlSerializer)m.CreateMemento ().ReadFrom (innerReader));
											} finally {
												innerReader.Close ();
											}
										}
									}
									return true;
								}
								return false;
							});
							return true;
						case Properties.Node:
							Properties properties = Properties.Read (reader);
							string name = properties.Get ("ActiveWindow", "");
							Gtk.Application.Invoke (delegate {
								foreach (Document document in IdeApp.Workbench.Documents) {
									if (document.FileName != null &&
										document.FileName == name) {
										DispatchService.GuiDispatch (new MessageHandler (document.Select));
										break;
									}
								}
							});
							string cname = properties.Get ("ActiveConfiguration", "");
							IConfiguration conf = combine.GetConfiguration (cname);
							if (conf != null)
								combine.ActiveConfiguration = conf;
							return true;
						}
						return true;
				});
			} catch (Exception e) {
				LoggingService.LogError ("Exception while loading user combine preferences.", e);
			} finally {
				reader.Close ();
			}
		} 
		
		string GetPreferencesFileName (Combine combine)
		{
			return Path.Combine (Path.GetDirectoryName (combine.FileName), Path.ChangeExtension (combine.FileName, ".userprefs"));
		}
		
		void SaveCombinePreferences (Combine combine)
		{
			XmlTextWriter writer = new XmlTextWriter (GetPreferencesFileName (combine), System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			try {
				writer.WriteStartElement (UserCombinePreferencesNode);
				writer.WriteAttributeString (VersionAttribute, Version); 
				writer.WriteAttributeString ("filename", combine.FileName); 
				
				writer.WriteStartElement (FilesNode);
				foreach (Document document in IdeApp.Workbench.Documents) {
					if (!String.IsNullOrEmpty (document.FileName)) {
						writer.WriteStartElement (FileNode);
						writer.WriteAttributeString (FileNameAttribute, FileService.AbsoluteToRelativePath (Path.GetDirectoryName (combine.FileName), document.FileName)); 
						if (document.TextEditor != null) {
							writer.WriteAttributeString (FileLineAttribute, document.TextEditor.CursorLine.ToString ());
							writer.WriteAttributeString (FileColumnAttribute, document.TextEditor.CursorColumn.ToString ());
						}
						writer.WriteEndElement (); // File
					}
				}
				writer.WriteEndElement (); // FilesNode
				
				writer.WriteStartElement (ViewsNode);
				foreach (Pad pad in IdeApp.Workbench.Pads) {
					if (pad.Content is IMementoCapable) {
						writer.WriteStartElement (ViewMementoNode);
						writer.WriteAttributeString (IdAttribute, pad.Id); 
						
						((ICustomXmlSerializer)((IMementoCapable)pad.Content).CreateMemento ()).WriteTo (writer);
						writer.WriteEndElement (); // ViewMementoNode
					}
				}
				writer.WriteEndElement (); // Views
				
				Properties properties = new Properties ();
				string name = IdeApp.Workbench.ActiveDocument == null ? String.Empty : IdeApp.Workbench.ActiveDocument.FileName;
				properties.Set ("ActiveWindow", name == null ? String.Empty : name);
				properties.Set ("ActiveConfiguration", combine.ActiveConfiguration == null ? String.Empty : combine.ActiveConfiguration.Name);
			
				properties.Write (writer);
				
				writer.WriteEndElement (); // UserCombinePreferencesNode
			} catch (Exception e) {
				LoggingService.LogWarning ("Could not save solution preferences: " + GetPreferencesFileName (combine), e);
			} finally {
				writer.Close ();
			}
		}
		
		public IAsyncOperation Execute (CombineEntry entry)
		{
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			return Execute (entry, context);
		}
		
		public IAsyncOperation Execute (CombineEntry entry, ExecutionContext context)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();

			DispatchService.ThreadDispatch (new StatefulMessageHandler (ExecuteCombineEntryAsync), new object[] {entry, monitor, context});
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
			if (currentRunOperation != null && !currentRunOperation.IsCompleted)
				return currentRunOperation;
			
			guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (IdeApp.Services.DebuggingService.GetExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			
			DispatchService.ThreadDispatch (delegate {
				DebugCombineEntryAsync (monitor, entry, context, null);
			}, null);
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		void DebugCombineEntryAsync (IProgressMonitor monitor, CombineEntry entry, ExecutionContext context, WorkbenchContext oldContext)
		{
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
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation DebugApplication (string executableFile)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;
			
			guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRunProgressMonitor ();

			IAsyncOperation oper = IdeApp.Services.DebuggingService.Run (executableFile, (IConsole) monitor);
			oper.Completed += delegate {
				monitor.Dispose ();
			};
			
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		public void Clean (CombineEntry entry)
		{
			entry.Clean (new NullProgressMonitor ());
		}
		
		public IAsyncOperation BuildFile (string file)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Build (tempProject);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError (GettextCatalog.GetString ("The file {0} can't be compiled.", file));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation ExecuteFile (string file)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation ExecuteFile (string file, ExecutionContext context)
		{
			Project tempProject = projectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject, context);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public IAsyncOperation Rebuild (CombineEntry entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;

			Clean (entry);
			return Build (entry);
		}

		public IAsyncOperation Build (CombineEntry entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;
			
			DoBeforeCompileAction ();
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
			
			BeginBuild (monitor);

			DispatchService.ThreadDispatch (new StatefulMessageHandler (BuildCombineEntryAsync), new object[] {entry, monitor});
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
				result = entry.Build (monitor);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Build failed."), ex);
			}
			DispatchService.GuiDispatch (
				delegate {
					BuildDone (monitor, result);	// BuildDone disposes the monitor
			});
		}

		void DoBeforeCompileAction ()
		{
			BeforeCompileAction action = (BeforeCompileAction)PropertyService.Get("SharpDevelop.Services.DefaultParserService.BeforeCompileAction", BeforeCompileAction.SaveAllFiles);
			
			switch (action) {
				case BeforeCompileAction.Nothing:
					break;
				case BeforeCompileAction.PromptForSave:
					foreach (Document doc in IdeApp.Workbench.Documents) {
						if (doc.IsDirty) {
							if (MessageService.AskQuestion(GettextCatalog.GetString ("Save changed files?"), AlertButton.CloseWithoutSave, AlertButton.Save) == AlertButton.Save) {
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

		void BeginBuild (IProgressMonitor monitor)
		{
			Services.TaskService.ClearExceptCommentTasks ();
			if (StartBuild != null) {
				StartBuild (this, new BuildEventArgs (monitor, true));
			}
		}
		
		void BuildDone (IProgressMonitor monitor, ICompilerResult result)
		{
			Task[] tasks = null;
		
			try {
				if (result != null) {
					lastResult = result;
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("---------------------- Done ----------------------"));
					
					tasks = new Task [result.CompilerResults.Errors.Count];
					for (int n=0; n<tasks.Length; n++)
						tasks [n] = new Task (null, result.CompilerResults.Errors [n]);

					Services.TaskService.AddRange (tasks);
					
					string errorString = GettextCatalog.GetPluralString("{0} error", "{0} errors", result.ErrorCount, result.ErrorCount);
					string warningString = GettextCatalog.GetPluralString("{0} warning", "{0} warnings", result.WarningCount, result.WarningCount);

					if (result.ErrorCount == 0 && result.WarningCount == 0 && lastResult.FailedBuildCount == 0) {
						monitor.ReportSuccess (GettextCatalog.GetString ("Build successful."));
					} else if (result.ErrorCount == 0 && result.WarningCount > 0) {
						monitor.ReportWarning(GettextCatalog.GetString("Build: ") + errorString + ", " + warningString);
					} else if (result.ErrorCount > 0) {
						monitor.ReportError(GettextCatalog.GetString("Build: ") + errorString + ", " + warningString, null);
					} else {
						monitor.ReportError(GettextCatalog.GetString("Build failed."), null);
					}
					OnEndBuild (monitor, lastResult.FailedBuildCount == 0);
				} else
					OnEndBuild (monitor, false);
			}
			finally {
				monitor.Dispose ();
			}
			
			// If there is at least an error or warning, show the error list pad.
			if (tasks != null && tasks.Length > 0) {
				try {
					Pad errorsPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
					if ((bool) PropertyService.Get ("SharpDevelop.ShowTaskListAfterBuild", true)) {
						errorsPad.Visible = true;
						errorsPad.BringToFront ();
					}
				} catch {}
			}
		}
		
		public string[] AddFilesToProject (Project project, string[] files, string targetDirectory)
		{
			int action = -1;
			IProgressMonitor monitor = null;
			
			if (files.Length > 10) {
				monitor = new MonoDevelop.Core.Gui.ProgressMonitoring.MessageDialogProgressMonitor (true);
				monitor.BeginTask (GettextCatalog.GetString("Adding files..."), files.Length);
			}
			
			List<string> newFileList = new List<string> ();
			
			using (monitor) {
				
				foreach (string file in files) {
					if (monitor != null)
						monitor.Log.WriteLine (file);
					if (file.StartsWith (project.BaseDirectory)) {
						newFileList.Add (MoveCopyFile (project, targetDirectory, file, true, true));
					} else {
						Gtk.MessageDialog md = new Gtk.MessageDialog (
							 IdeApp.Workbench.RootWindow,
							 Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
							 Gtk.MessageType.Question, Gtk.ButtonsType.None,
							 GettextCatalog.GetString ("{0} is outside the project directory, what should I do?", file));

						try {
							Gtk.CheckButton remember = null;
							if (files.Length > 1) {
								remember = new Gtk.CheckButton (GettextCatalog.GetString ("Use the same action for all selected files."));
								md.VBox.PackStart (remember, false, false, 0);
							}
							
							int LINK_VALUE = 3;
							int COPY_VALUE = 1;
							int MOVE_VALUE = 2;
							
							md.AddButton (GettextCatalog.GetString ("_Link"), LINK_VALUE);
							md.AddButton (Gtk.Stock.Copy, COPY_VALUE);
							md.AddButton (GettextCatalog.GetString ("_Move"), MOVE_VALUE);
							md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
							md.VBox.ShowAll ();
							
							int ret = -1;
							if (action < 0) {
								ret = md.Run ();
								if (ret < 0)
									return newFileList.ToArray ();
								if (remember != null && remember.Active) action = ret;
							} else {
								ret = action;
							}
							
							try {
								string nf = MoveCopyFile (project, targetDirectory, file,
											  (ret == MOVE_VALUE) || (ret == LINK_VALUE), ret == LINK_VALUE);
								newFileList.Add (nf);
							}
							catch (Exception ex) {
								MessageService.ShowException (ex, GettextCatalog.GetString ("An error occurred while attempt to move/copy that file. Please check your permissions."));
								newFileList.Add (null);
							}
						} finally {
							md.Destroy ();
						}
					}
					if (monitor != null)
						monitor.Step (1);
				}
			}
			return newFileList.ToArray ();
		}
		
		string MoveCopyFile (Project project, string baseDirectory, string filename, bool move, bool alreadyInPlace)
		{
			if (FileService.IsDirectory (filename))
			    return null;

			string name = System.IO.Path.GetFileName (filename);
			string newfilename = alreadyInPlace ? filename : Path.Combine (baseDirectory, name);

			if (filename != newfilename) {
				if (File.Exists (newfilename)) {
					if (!MessageService.Confirm (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to replace it?", newfilename), AlertButton.OverwriteFile))
						return null;
				}
				FileService.CopyFile (filename, newfilename);
				if (move)
					FileService.DeleteFile (filename);
			}
			
			if (project.IsCompileable (newfilename)) {
				project.AddFile (newfilename, BuildAction.Compile);
			} else {
				project.AddFile (newfilename, BuildAction.Nothing);
			}
			return newfilename;
		}		

		public void TransferFiles (IProgressMonitor monitor, Project sourceProject, string sourcePath, Project targetProject, string targetPath, bool removeFromSource, bool copyOnlyProjectFiles)
		{
			// When transfering directories, targetPath is the directory where the source
			// directory will be transfered, not including the destination directory name.
			// For example, if sourcePath is /a1/a2/a3 and targetPath is /b1/b2, the
			// new folder will be /b1/b2/a3
			
			if (targetProject == null)
				throw new ArgumentNullException ("targetProject");

			if (!targetPath.StartsWith (targetProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + targetPath);

			if (sourceProject != null && !sourcePath.StartsWith (sourceProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + sourcePath);
				
			if (copyOnlyProjectFiles && sourceProject == null)
				throw new ArgumentException ("A source project must be specified if copyOnlyProjectFiles is True");

			bool movingFolder = (removeFromSource && Directory.Exists (sourcePath) && (
					!copyOnlyProjectFiles ||
					IsDirectoryHierarchyEmpty (sourcePath)));

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
				monitor.ReportError (GettextCatalog.GetString ("Could not get any file from '{0}'.", sourcePath), ex);
				return;
			}
			
			// Ensure that the destination folder is created, even if no files
			// are copied
			
			string newFolder = Path.Combine (targetPath, Path.GetFileName (sourcePath));
			try {
				if (Directory.Exists (sourcePath) && !Directory.Exists (newFolder) && !movingFolder)
					FileService.CreateDirectory (newFolder);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not create directory '{0}'.", targetPath), ex);
				return;
			}

			// Transfer files
			// If moving a folder, do it all at once
			
			if (movingFolder) {
				try {
					FileService.MoveDirectory (sourcePath, newFolder);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Directory '{0}' could not be deleted.", sourcePath), ex);
				}
			}

			string basePath = Path.GetDirectoryName (sourcePath);
			monitor.BeginTask (GettextCatalog.GetString ("Copying files..."), filesToMove.Count);
			
			foreach (ProjectFile file in filesToMove) {
				string sourceFile = file.Name;
				string newFile = targetPath + sourceFile.Substring (basePath.Length);
				
				ProjectFile oldProjectFile = sourceProject != null ? sourceProject.ProjectFiles.GetFile (sourceFile) : null;
				
				if (!movingFolder) {
					try {
						string fileDir = Path.GetDirectoryName (newFile);
						if (!Directory.Exists (fileDir))
							FileService.CreateDirectory (fileDir);
						if (removeFromSource)
							FileService.MoveFile (sourceFile, newFile);
						else
							FileService.CopyFile (sourceFile, newFile);
					} catch (Exception ex) {
						monitor.ReportError (GettextCatalog.GetString ("File '{0}' could not be created.", newFile), ex);
						monitor.Step (1);
						continue;
					}
				}
				
				if (oldProjectFile != null) {
					if (removeFromSource && sourceProject.ProjectFiles.Contains (oldProjectFile))
						sourceProject.ProjectFiles.Remove (oldProjectFile);
					if (targetProject.ProjectFiles.GetFile (newFile) == null) {
						ProjectFile projectFile = (ProjectFile) oldProjectFile.Clone ();
						projectFile.Name = newFile;
						targetProject.ProjectFiles.Add (projectFile);
					}
				}
				
				monitor.Step (1);
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
		
		internal void NotifyFilePropertyChangedInProject (object sender, ProjectFileEventArgs e)
		{
			OnFilePropertyChangedInProject (e);
		}		
		
		internal void NotifyReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			OnReferenceAddedToProject (e);
		}
		
		internal void NotifyReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			OnReferenceRemovedFromProject (e);
		}
		
		void NotifyEntryAddedToCombine (object sender, CombineEntryEventArgs args)
		{
			OnEntryLoaded (args.CombineEntry);
			if (EntryAddedToCombine != null)
				EntryAddedToCombine (sender, args);
		}
		
		void NotifyEntryRemovedFromCombine (object sender, CombineEntryEventArgs args)
		{
			OnEntryUnloaded (args.CombineEntry);
			NotifyEntryRemovedFromCombineRec (args.CombineEntry);
		}
		
		void NotifyEntryRemovedFromCombineRec (CombineEntry e)
		{
			if (e == CurrentSelectedProject)
				CurrentSelectedProject = null;
				
			if (e == CurrentSelectedCombine)
				CurrentSelectedCombine = null;
				
			if (e is Combine) {
				foreach (CombineEntry ce in ((Combine)e).Entries)
					NotifyEntryRemovedFromCombineRec (ce);
			}
			if (EntryRemovedFromCombine != null)
				EntryRemovedFromCombine (this, new CombineEntryEventArgs (e));
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
		
		protected virtual void OnFilePropertyChangedInProject (ProjectFileEventArgs e)
		{
			if (FilePropertyChangedInProject != null) {
				FilePropertyChangedInProject (this, e);
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

		void OnEndBuild (IProgressMonitor monitor, bool success)
		{
			if (EndBuild != null) {
				EndBuild (this, new BuildEventArgs (monitor, success));
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
				StringParserService.Properties["PROJECTNAME"] = CurrentSelectedProject.Name;
			}
			if (CurrentProjectChanged != null) {
				CurrentProjectChanged(this, e);
			}
		}
		
		public event ProjectFileEventHandler FileRemovedFromProject;
		public event ProjectFileEventHandler FileAddedToProject;
		public event ProjectFileEventHandler FileChangedInProject;
		public event ProjectFileEventHandler FilePropertyChangedInProject;
		public event ProjectFileRenamedEventHandler FileRenamedInProject;
		
		public event BuildEventHandler StartBuild;
		public event BuildEventHandler EndBuild;
		public event EventHandler BeforeStartProject;
		
		public event CombineEventHandler CombineOpened;
		public event CombineEventHandler CombineClosed;
		public event CombineEventHandler CurrentSelectedCombineChanged;
		
		public event ProjectEventHandler CurrentProjectChanged;
		
		public event ProjectReferenceEventHandler ReferenceAddedToProject;
		public event ProjectReferenceEventHandler ReferenceRemovedFromProject;
		
		// Fired just before an entry is added to a combine
		public event AddEntryEventHandler AddingEntryToCombine;
		public event CombineEntryEventHandler EntryAddedToCombine;
		public event CombineEntryEventHandler EntryRemovedFromCombine;

		
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
			return IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Code Completion Database Generation", "md-parser");
		}
	}
	
	class OpenDocumentFileProvider: ITextFileProvider
	{
		public IEditableTextFile GetEditableTextFile (string filePath)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					IEditableTextFile ef = doc.GetContent<IEditableTextFile> ();
					if (ef != null) return ef;
				}
			}
			return null;
		}
	}
}
