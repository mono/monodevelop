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
		ProjectService projectService = MonoDevelop.Projects.Services.ProjectService;
		IAsyncOperation currentBuildOperation = NullAsyncOperation.Success;
		IAsyncOperation currentRunOperation = NullAsyncOperation.Success;
		IBuildTarget currentBuildOperationOwner;
		IBuildTarget currentRunOperationOwner;
		
		GuiHelper guiHelper = new GuiHelper ();
		SelectReferenceDialog selDialog = null;
		
		SolutionItem currentSolutionItem = null;
		WorkspaceItem currentWorkspaceItem = null;
		object currentItem;
		
		BuildResult lastResult = new BuildResult ();
		
		internal ProjectOperations ()
		{
			IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
		}
		
		public BuildResult LastCompilerResult {
			get { return lastResult; }
		}
		
		public Project CurrentSelectedProject {
			get {
				return currentSolutionItem as Project;
			}
		}
		
		public Solution CurrentSelectedSolution {
			get {
				return currentWorkspaceItem as Solution;
			}
		}
		
		public IBuildTarget CurrentSelectedBuildTarget {
			get {
				if (currentSolutionItem != null)
					return currentSolutionItem;
				return currentWorkspaceItem;
			}
		}
		
		public WorkspaceItem CurrentSelectedWorkspaceItem {
			get {
				return currentWorkspaceItem;
			}
			internal set {
				if (value != currentWorkspaceItem) {
					WorkspaceItem oldValue = currentWorkspaceItem;
					currentWorkspaceItem = value;
					if (oldValue is Solution || value is Solution)
						OnCurrentSelectedSolutionChanged(new SolutionEventArgs (currentWorkspaceItem as Solution));
				}
			}
		}
		
		public SolutionItem CurrentSelectedSolutionItem {
			get {
				if (currentSolutionItem == null && CurrentSelectedSolution != null)
					return CurrentSelectedSolution.RootFolder;
				return currentSolutionItem;
			}
			internal set {
				if (value != currentSolutionItem) {
					SolutionItem oldValue = currentSolutionItem;
					currentSolutionItem = value;
					if (oldValue is Project || value is Project)
						OnCurrentProjectChanged (new ProjectEventArgs(currentSolutionItem as Project));
				}
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
		
		public IAsyncOperation CurrentBuildOperation {
			get { return currentBuildOperation; }
		}
		
		public IAsyncOperation CurrentRunOperation {
			get { return currentRunOperation; }
		}
		
		public bool IsBuilding (IBuildTarget target)
		{
			return !currentBuildOperation.IsCompleted && ContainsTarget (target, currentBuildOperationOwner);
		}
		
		public bool IsRunning (IBuildTarget target)
		{
			return !currentRunOperation.IsCompleted && ContainsTarget (target, currentRunOperationOwner);
		}
		
		internal static bool ContainsTarget (IBuildTarget owner, IBuildTarget target)
		{
			if (owner == target)
				return true;
			else if (owner is WorkspaceItem)
				return ((WorkspaceItem)owner).ContainsItem (target);
			return false;
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
		
		public void JumpToDeclaration (MonoDevelop.Projects.Dom.IMember member)
		{
			if (member == null) 
				return;
			if (member is MonoDevelop.Projects.Dom.IType) {
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (((MonoDevelop.Projects.Dom.IType)member).CompilationUnit.FileName,
				                                                   member.Location.Line,
				                                                   member.Location.Column,
				                                                   true);
			}
			if (member.DeclaringType == null) 
				return;
			MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (member.DeclaringType.CompilationUnit.FileName, 
			                                                   member.Location.Line,
			                                                   member.Location.Column,
			                                                   true);
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

		
		public void Export (IWorkspaceObject item)
		{
			Export (item, null);
		}
		
		public void Export (IWorkspaceObject entry, FileFormat format)
		{
			ExportProjectDialog dlg = new ExportProjectDialog (entry, format);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					
					using (IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (GettextCatalog.GetString ("Export Project"), null, true, true)) {
						string folder = dlg.TargetFolder;
						
						string file = entry is WorkspaceItem ? ((WorkspaceItem)entry).FileName : ((SolutionEntityItem)entry).FileName;
						Services.ProjectService.Export (mon, file, folder, dlg.Format);
					}
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		public void Save (SolutionEntityItem entry)
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
		
		public void Save (Solution item)
		{
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				item.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Solution saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void Save (IWorkspaceFileObject item)
		{
			if (item is SolutionEntityItem)
				Save ((SolutionEntityItem) item);
			else if (item is Solution)
				Save ((Solution)item);
			
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				item.Save (monitor);
				monitor.ReportSuccess (GettextCatalog.GetString ("Item saved."));
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Save failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public void MarkFileDirty (string filename)
		{
			Project entry = IdeApp.Workspace.GetProjectContainingFile (filename);
			if (entry != null) {
				entry.SetNeedsBuilding (true);
			}
		}
		
		public void ShowOptions (IWorkspaceObject entry)
		{
			ShowOptions (entry, null);
		}
		
		public void ShowOptions (IWorkspaceObject entry, string panelId)
		{
			if (entry is Project) {
				Project selectedProject = (Project) entry;
				
				ProjectOptionsDialog optionsDialog = new ProjectOptionsDialog (IdeApp.Workbench.RootWindow, selectedProject);
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					
					if (optionsDialog.Run() == (int)Gtk.ResponseType.Ok) {
						selectedProject.SetNeedsBuilding (true);
						Save (selectedProject);
					}
				} finally {
					optionsDialog.Destroy ();
				}
			} else if (entry is Solution) {
				Solution solution = (Solution) entry;
				
				CombineOptionsDialog optionsDialog = new CombineOptionsDialog (IdeApp.Workbench.RootWindow, solution);
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (optionsDialog.Run () == (int) Gtk.ResponseType.Ok)
						IdeApp.Workspace.Save ();
				} finally {
					optionsDialog.Destroy ();
				}
			}
			else {
				ItemOptionsDialog optionsDialog = new ItemOptionsDialog (IdeApp.Workbench.RootWindow, entry);
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (optionsDialog.Run () == (int) Gtk.ResponseType.Ok) {
						if (entry is IBuildTarget)
							((IBuildTarget)entry).SetNeedsBuilding (true, IdeApp.Workspace.ActiveConfiguration);
						if (entry is IWorkspaceFileObject)
							Save ((IWorkspaceFileObject) entry);
						else {
							SolutionItem si = entry as SolutionItem;
							if (si.ParentSolution != null)
								Save (si.ParentSolution);
						}
					}
				} finally {
					optionsDialog.Destroy ();
				}
			}
		}
		
		public void NewSolution ()
		{
			NewProjectDialog pd = new NewProjectDialog (null, true, null);
			pd.Run ();
			pd.Destroy ();
		}
		
		public WorkspaceItem AddNewWorkspaceItem (Workspace parentWorkspace)
		{
			return AddNewWorkspaceItem (parentWorkspace, null);
		}
		
		public WorkspaceItem AddNewWorkspaceItem (Workspace parentWorkspace, string defaultItemId)
		{
			NewProjectDialog npdlg = new NewProjectDialog (null, false, parentWorkspace.BaseDirectory);
			npdlg.SelectTemplate (defaultItemId);
			try {
				if (npdlg.Run () == (int) Gtk.ResponseType.Ok && npdlg.NewItem != null) {
					parentWorkspace.Items.Add ((WorkspaceItem) npdlg.NewItem);
					Save (parentWorkspace);
					return (WorkspaceItem) npdlg.NewItem;
				}
			} finally {
				npdlg.Destroy ();
			}
			return null;
		}
		
		public WorkspaceItem AddWorkspaceItem (Workspace parentWorkspace)
		{
			WorkspaceItem res = null;
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Workspace"));
			try {
				fdiag.SetCurrentFolder (parentWorkspace.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					try {
						res = AddWorkspaceItem (parentWorkspace, fdiag.Filename);
					}
					catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", fdiag.Filename));
					}
				}
			} finally {
				fdiag.Destroy ();
			}
			
			return res;
		}
		
		public WorkspaceItem AddWorkspaceItem (Workspace parentWorkspace, string itemFileName)
		{
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
				WorkspaceItem it = Services.ProjectService.ReadWorkspaceItem (monitor, itemFileName);
				if (it != null) {
					parentWorkspace.Items.Add (it);
					Save (parentWorkspace);
				}
				return it;
			}
		}
		
		public SolutionItem CreateProject (SolutionFolder parentFolder)
		{
			SolutionItem res = null;
			string basePath = parentFolder != null ? parentFolder.BaseDirectory : null;
			NewProjectDialog npdlg = new NewProjectDialog (parentFolder, false, basePath);
			npdlg.Run ();
			npdlg.Destroy ();
			return res;
		}

		public SolutionItem AddSolutionItem (SolutionFolder parentFolder)
		{
			SolutionItem res = null;
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Solution"));
			try {
				fdiag.SetCurrentFolder (parentFolder.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					try {
						res = AddSolutionItem (parentFolder, fdiag.Filename);
					}
					catch (Exception ex) {
						MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", fdiag.Filename));
					}
				}
			} finally {
				fdiag.Destroy ();
			}
			
			if (res != null)
				IdeApp.Workspace.Save ();

			return res;
		}
		
		public SolutionItem AddSolutionItem (SolutionFolder folder, string entryFileName)
		{
			AddEntryEventArgs args = new AddEntryEventArgs (folder, entryFileName);
			if (AddingEntryToCombine != null)
				AddingEntryToCombine (this, args);
			if (args.Cancel)
				return null;
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
				return folder.AddItem (args.FileName, monitor);
			}
		}

		public void CreateProjectFile (Project parentProject, string basePath)
		{
			CreateProjectFile (parentProject, basePath, null);
		}
		
		public void CreateProjectFile (Project parentProject, string basePath, string selectedTemplateId)
		{
			NewFileDialog nfd = null;
			try {
				nfd = new NewFileDialog (parentProject, basePath);
				if (selectedTemplateId != null)
					nfd.SelectTemplate (selectedTemplateId);
				nfd.Run ();
			} finally {
				if (nfd != null) nfd.Destroy ();
			}
		}

		public bool AddReferenceToProject (DotNetProject project)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
				selDialog.SetProject (project);

				if (selDialog.Run() == (int)Gtk.ResponseType.Ok) {
					ProjectReferenceCollection newRefs = selDialog.ReferenceInformations;
					
					ArrayList toDelete = new ArrayList ();
					foreach (ProjectReference refInfo in project.References)
						if (!newRefs.Contains (refInfo))
							toDelete.Add (refInfo);
					
					foreach (ProjectReference refInfo in toDelete)
							project.References.Remove (refInfo);

					foreach (ProjectReference refInfo in selDialog.ReferenceInformations)
						if (!project.References.Contains (refInfo))
							project.References.Add(refInfo);
					
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

		
		public IAsyncOperation Execute (IBuildTarget entry)
		{
			ExecutionContext context = new ExecutionContext (new DefaultExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			return Execute (entry, context);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry, ExecutionContext context)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();

			DispatchService.ThreadDispatch (delegate {
				ExecuteSolutionItemAsync (monitor, entry, context);
			});
			currentRunOperation = monitor.AsyncOperation;
			currentRunOperationOwner = entry;
			currentRunOperation.Completed += delegate { currentRunOperationOwner = null; };
			return currentRunOperation;
		}
		
		void ExecuteSolutionItemAsync (IProgressMonitor monitor, IBuildTarget entry, ExecutionContext context)
		{
			try {
				OnBeforeStartProject ();
				entry.Execute (monitor, context, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public IAsyncOperation Debug (IBuildTarget entry)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted)
				return currentRunOperation;
			
			//guiHelper.SetWorkbenchContext (WorkbenchContext.Debug);

			IProgressMonitor monitor = new MessageDialogProgressMonitor ();
			ExecutionContext context = new ExecutionContext (IdeApp.Services.DebuggingService.GetExecutionHandlerFactory (), IdeApp.Workbench.ProgressMonitors);
			
			DispatchService.ThreadDispatch (delegate {
				DebugSolutionItemAsync (monitor, entry, context, null);
			}, null);
			currentRunOperation = monitor.AsyncOperation;
			return currentRunOperation;
		}
		
		void DebugSolutionItemAsync (IProgressMonitor monitor, IBuildTarget entry, ExecutionContext context, WorkbenchContext oldContext)
		{
			try {
				entry.Execute (monitor, context, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
			} finally {
				monitor.Dispose ();
			}
			//guiHelper.SetWorkbenchContext (WorkbenchContext.Edit);
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
		
		public void Clean (IBuildTarget entry)
		{
			entry.RunTarget (new NullProgressMonitor (), ProjectService.CleanTarget, IdeApp.Workspace.ActiveConfiguration);
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
		
		public IAsyncOperation Rebuild (IBuildTarget entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;

			Clean (entry);
			return Build (entry);
		}

		public IAsyncOperation Build (IBuildTarget entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;
			
			DoBeforeCompileAction ();
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
			
			BeginBuild (monitor);

			DispatchService.ThreadDispatch (delegate {
				BuildSolutionItemAsync (entry, monitor);
			}, null);
			currentBuildOperation = monitor.AsyncOperation;
			currentBuildOperationOwner = entry;
			currentBuildOperation.Completed += delegate { currentBuildOperationOwner = null; };
			return currentBuildOperation;
		}
		
		void BuildSolutionItemAsync (IBuildTarget entry, IProgressMonitor monitor)
		{
			BuildResult result = null;
			try {
				SolutionItem it = entry as SolutionItem;
				if (it != null)
					result = it.Build (monitor, IdeApp.Workspace.ActiveConfiguration, true);
				else
					result = entry.RunTarget (monitor, ProjectService.BuildTarget, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Build failed."), ex);
			}
			DispatchService.GuiDispatch (
				delegate {
					BuildDone (monitor, result, entry);	// BuildDone disposes the monitor
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
			if (StartBuild != null)
				StartBuild (this, new BuildEventArgs (monitor, true));
		}
		
		void BuildDone (IProgressMonitor monitor, BuildResult result, IBuildTarget entry)
		{
			Task[] tasks = null;
			Solution solution;
			
			if (entry is SolutionItem)
				solution = ((SolutionItem)entry).ParentSolution;
			else
				solution = entry as Solution;
		
			try {
				if (result != null) {
					lastResult = result;
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("---------------------- Done ----------------------"));
					
					tasks = new Task [result.Errors.Count];
					for (int n=0; n<tasks.Length; n++)
						tasks [n] = new Task (result.Errors [n]);

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
					filesToMove = sourceProject.Files.GetFilesInPath (sourcePath);
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
				
				ProjectFile oldProjectFile = sourceProject != null ? sourceProject.Files.GetFile (sourceFile) : null;
				
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
					if (removeFromSource && sourceProject.Files.Contains (oldProjectFile))
						sourceProject.Files.Remove (oldProjectFile);
					if (targetProject.Files.GetFile (newFile) == null) {
						ProjectFile projectFile = (ProjectFile) oldProjectFile.Clone ();
						projectFile.Name = newFile;
						targetProject.Files.Add (projectFile);
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
		
		void OnWorkspaceItemUnloaded (object s, WorkspaceItemEventArgs args)
		{
			if (ContainsTarget (args.Item, currentSolutionItem))
				CurrentSelectedSolutionItem = null;
			if (ContainsTarget (args.Item, currentWorkspaceItem))
				CurrentSelectedWorkspaceItem = null;
			if ((currentItem is IBuildTarget) && ContainsTarget (args.Item, ((IBuildTarget)currentItem)))
				CurrentSelectedItem = null;
			if (IsBuilding (args.Item))
				CurrentBuildOperation.Cancel ();
			if (IsRunning (args.Item))
				CurrentRunOperation.Cancel ();
		}
		
		protected virtual void OnCurrentSelectedSolutionChanged(SolutionEventArgs e)
		{
			if (CurrentSelectedSolutionChanged != null) {
				CurrentSelectedSolutionChanged (this, e);
			}
		}
		
		protected virtual void OnCurrentProjectChanged(ProjectEventArgs e)
		{
			if (CurrentSelectedProject != null) {
				StringParserService.Properties["PROJECTNAME"] = CurrentSelectedProject.Name;
			}
			if (CurrentProjectChanged != null) {
				CurrentProjectChanged (this, e);
			}
		}
		
		public event BuildEventHandler StartBuild;
		public event BuildEventHandler EndBuild;
		public event EventHandler BeforeStartProject;
		
		public event EventHandler<SolutionEventArgs> CurrentSelectedSolutionChanged;
		public event ProjectEventHandler CurrentProjectChanged;
		
		// Fired just before an entry is added to a combine
		public event AddEntryEventHandler AddingEntryToCombine;

		
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
