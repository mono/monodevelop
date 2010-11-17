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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Instrumentation;
using Mono.TextEditor;

namespace MonoDevelop.Ide
{
	/// <summary>
	/// This is the basic interface to the workspace.
	/// </summary>
	public class ProjectOperations
	{
		IAsyncOperation currentBuildOperation = NullAsyncOperation.Success;
		IAsyncOperation currentRunOperation = NullAsyncOperation.Success;
		IBuildTarget currentBuildOperationOwner;
		IBuildTarget currentRunOperationOwner;
		
		SelectReferenceDialog selDialog = null;
		
		SolutionItem currentSolutionItem = null;
		WorkspaceItem currentWorkspaceItem = null;
		object currentItem;
		
		BuildResult lastResult = new BuildResult ();
		
		internal ProjectOperations ()
		{
			IdeApp.Workspace.WorkspaceItemUnloaded += OnWorkspaceItemUnloaded;
			IdeApp.Workspace.ItemUnloading += IdeAppWorkspaceItemUnloading;
			
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
			set { currentRunOperation = value; }
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
			else if (target is RootWorkspace)
				return ContainsTarget (owner, IdeApp.ProjectOperations.CurrentSelectedSolution);
			else if (owner is WorkspaceItem)
				return ((WorkspaceItem)owner).ContainsItem (target);
			return false;
		}
		/*
		string GetDeclaredFile(IMember item)
		{			
			if (item is IMember) {
				IMember mem = (IMember) item;				
				if (mem.Region == null)
					return null;
				else if (mem.Region.FileName != null)
					return mem.Region.FileName;
				else if (mem.DeclaringType != null) {
					foreach (IType c in mem.DeclaringType.Parts) {
						if ((mem is IField && c.Fields.Contains((IField)mem)) ||
						    (mem is IEvent && c.Events.Contains((IEvent)mem)) || 
						    (mem is IProperty  && c.Properties.Contains((IProperty)mem)) ||
						    (mem is IMethod && c.Methods.Contains((IMethod)mem))) {
							return GetClassFileName(c);							
						}                                   
					}
				}
			} else if (item is IType) {
				IType cls = (IType) item;
				return GetClassFileName (cls);
			} else if (item is MonoDevelop.Projects.Parser.LocalVariable) {
				MonoDevelop.Projects.Parser.LocalVariable cls = (MonoDevelop.Projects.Parser.LocalVariable) item;
				return cls.Region.FileName;
			}
			return null;
		}
		
		public bool CanJumpToDeclaration (IMember item)
		{
			return (GetDeclaredFile(item) != null);
		}*/
		
		public bool CanJumpToDeclaration (MonoDevelop.Projects.Dom.INode visitable)
		{
			if (visitable is MonoDevelop.Projects.Dom.IType) 
				return ((MonoDevelop.Projects.Dom.IType)visitable).CompilationUnit != null;
			if (visitable is LocalVariable)
				return true;
			if (visitable is IParameter)
				return true;
			IMember member = visitable as MonoDevelop.Projects.Dom.IMember;
			if (member == null || member.DeclaringType == null) 
				return false ;
			return member.DeclaringType.CompilationUnit != null;
		}

		public void JumpToDeclaration (MonoDevelop.Projects.Dom.INode visitable)
		{
			if (visitable is LocalVariable) {
				LocalVariable localVar = (LocalVariable)visitable;
				IdeApp.Workbench.OpenDocument (localVar.FileName,
				                               localVar.Region.Start.Line,
				                               localVar.Region.Start.Column,
				                               true);
				return;
			}
			
			if (visitable is IParameter) {
				IParameter para = (IParameter)visitable;
				IdeApp.Workbench.OpenDocument (para.DeclaringMember.DeclaringType.CompilationUnit.FileName,
				                               para.Location.Line,
				                               para.Location.Column,
				                               true);
				return;
			}
			
			IMember member = visitable as MonoDevelop.Projects.Dom.IMember;
			if (member == null) 
				return;
			string fileName;
			if (member is MonoDevelop.Projects.Dom.IType) {
				try {
					fileName = ((MonoDevelop.Projects.Dom.IType)member).CompilationUnit.FileName;
				} catch (Exception e) {
					LoggingService.LogError ("Can't get file name for type:" + member + ". Try to restart monodevelop.", e);
					fileName = null;
				}
			} else {
				if (member.DeclaringType == null) 
					return;
				IType declaringType = SearchContainingPart (member);
				fileName = declaringType.CompilationUnit.FileName;
			}
			var doc = IdeApp.Workbench.OpenDocument (fileName, member.Location.Line, member.Location.Column, true);
			if (doc != null) {
				MonoDevelop.Ide.Gui.Content.IUrlHandler handler = doc.ActiveView as MonoDevelop.Ide.Gui.Content.IUrlHandler;
				if (handler != null)
					handler.Open (member.HelpUrl);
			}
		}

		static IType SearchContainingPart (IMember member)
		{
			IType declaringType = member.DeclaringType;
			if (member is ExtensionMethod)
				declaringType = ((ExtensionMethod)member).OriginalMethod.DeclaringType;
			
			if (declaringType is InstantiatedType)
				declaringType = ((InstantiatedType)declaringType).UninstantiatedType;
			if (declaringType.HasParts) {
				foreach (IType part in declaringType.Parts) {
					IMember searchedMember = part.SearchMember (member.Name, true).FirstOrDefault (m => m.Location == member.Location);
					if (searchedMember != null) 
						return part;
				}
			}
			
			return declaringType;
		}

		
		public void RenameItem (IWorkspaceFileObject item, string newName)
		{
			ProjectOptionsDialog.RenameItem (item, newName);
			if (item is SolutionItem) {
				Save (((SolutionItem)item).ParentSolution);
			} else {
				IdeApp.Workspace.Save ();
				IdeApp.Workspace.SavePreferences ();
			}
		}
		
		public void Export (IWorkspaceObject item)
		{
			Export (item, null);
		}
		
		public void Export (IWorkspaceObject entry, FileFormat format)
		{
			ExportProjectDialog dlg = new ExportProjectDialog (entry, format);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					
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
		
		public void Save (IEnumerable<SolutionEntityItem> entries)
		{
			List<IWorkspaceFileObject> items = new List<IWorkspaceFileObject> ();
			foreach (IWorkspaceFileObject it in entries)
				items.Add (it);
			Save (items);
		}
		
		public void Save (SolutionEntityItem entry)
		{
			if (!entry.FileFormat.CanWrite (entry)) {
				IWorkspaceFileObject itemContainer = GetContainer (entry);
				if (SelectValidFileFormat (itemContainer))
					Save (itemContainer);
				return;
			}
			
			if (!AllowSave (entry))
				return;
			
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
			if (!item.FileFormat.CanWrite (item)) {
				if (!SelectValidFileFormat (item))
					return;
			}
			
			if (!AllowSave (item))
				return;
			
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
		
		public void Save (IEnumerable<IWorkspaceFileObject> items)
		{
			int count = items.Count ();
			if (count == 0)
				return;
			
			// Verify that the file format for each item is still valid
			
			HashSet<IWorkspaceFileObject> fixedItems = new HashSet<IWorkspaceFileObject> ();
			HashSet<IWorkspaceFileObject> failedItems = new HashSet<IWorkspaceFileObject> ();
			
			foreach (IWorkspaceFileObject entry in items) {
				IWorkspaceFileObject itemContainer = GetContainer (entry);
				if (fixedItems.Contains (itemContainer) || failedItems.Contains (itemContainer))
					continue;
				if (!entry.FileFormat.CanWrite (entry)) {
					// Can't save the project using this format. Try to find a valid format for the whole solution
					if (SelectValidFileFormat (itemContainer))
						fixedItems.Add (itemContainer);
					else
						failedItems.Add (itemContainer);
				}
			}
			if (fixedItems.Count > 0)
				Save (fixedItems);
			
			if (failedItems.Count > 0 || fixedItems.Count > 0) {
				// Some file format changes were required, and some items were saved.
				// Get a list of items not yet saved.
				List<IWorkspaceFileObject> notSavedEntries = new List<IWorkspaceFileObject> ();
				foreach (IWorkspaceFileObject entry in items) {
					IWorkspaceFileObject itemContainer = GetContainer (entry);
					if (!fixedItems.Contains (itemContainer) && !failedItems.Contains (itemContainer))
						notSavedEntries.Add (entry);
				}
				items = notSavedEntries;
			}
			
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetSaveProgressMonitor (true);
			try {
				monitor.BeginTask (null, count);
				foreach (IWorkspaceFileObject item in items) {
					if (AllowSave (item))
						item.Save (monitor);
					monitor.Step (1);
				}
				monitor.EndTask ();
				monitor.ReportSuccess (GettextCatalog.GetString ("Items saved."));
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
			
			if (!item.FileFormat.CanWrite (item)) {
				IWorkspaceFileObject ci = GetContainer (item);
				if (SelectValidFileFormat (ci))
					Save (ci);
				return;
			}
			
			if (!AllowSave (item))
				return;
			
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
		
		bool AllowSave (IWorkspaceFileObject item)
		{
			if (HasChanged (item))
				return MessageService.Confirm (
				    GettextCatalog.GetString ("Some project files have been changed from outside MonoDevelop. Do you want to overwrite them?"),
				    GettextCatalog.GetString ("Changes done in those files will be overwritten by MonoDevelop."),
				    AlertButton.OverwriteFile);
			else
				return true;
		}
		
		bool HasChanged (IWorkspaceFileObject item)
		{
			if (item.ItemFilesChanged)
				return true;
			if (item is WorkspaceItem) {
				foreach (SolutionEntityItem eitem in ((WorkspaceItem)item).GetAllSolutionItems<SolutionEntityItem> ())
					if (eitem.ItemFilesChanged)
						return true;
			}
			return false;
		}

		IWorkspaceFileObject GetContainer (IWorkspaceFileObject item)
		{
			SolutionEntityItem si = item as SolutionEntityItem;
			if (si != null && si.ParentSolution != null && !si.ParentSolution.FileFormat.SupportsMixedFormats)
				return si.ParentSolution;
			else
				return item;
		}
		
		bool SelectValidFileFormat (IWorkspaceFileObject item)
		{
			var dlg = new SelectFileFormatDialog (item);
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok && dlg.Format != null) {
					item.ConvertToFormat (dlg.Format, true);
					return true;
				}
				return false;
			} finally {
				dlg.Destroy ();
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
			if (entry is SolutionEntityItem) {
				var selectedProject = (SolutionEntityItem) entry;
				
				var optionsDialog = new ProjectOptionsDialog (IdeApp.Workbench.RootWindow, selectedProject);
				var conf = selectedProject.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
				optionsDialog.CurrentConfig = conf != null ? conf.Name : null;
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					
					if (MessageService.RunCustomDialog (optionsDialog) == (int)Gtk.ResponseType.Ok) {
						selectedProject.SetNeedsBuilding (true);
						foreach (object ob in optionsDialog.ModifiedObjects) {
							if (ob is Solution) {
								Save ((Solution)ob);
								return;
							}
						}
						Save (selectedProject);
						IdeApp.Workspace.SavePreferences ();
						IdeApp.Workbench.ReparseOpenDocuments ();
					}
				} finally {
					optionsDialog.Destroy ();
				}
			} else if (entry is Solution) {
				Solution solution = (Solution) entry;
				
				var optionsDialog = new CombineOptionsDialog (IdeApp.Workbench.RootWindow, solution);
				optionsDialog.CurrentConfig = IdeApp.Workspace.ActiveConfigurationId;
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (MessageService.RunCustomDialog (optionsDialog) == (int) Gtk.ResponseType.Ok) {
						Save (solution);
						IdeApp.Workspace.SavePreferences (solution);
					}
				} finally {
					optionsDialog.Destroy ();
				}
			}
			else {
				ItemOptionsDialog optionsDialog = new ItemOptionsDialog (IdeApp.Workbench.RootWindow, entry);
				try {
					if (panelId != null)
						optionsDialog.SelectPanel (panelId);
					if (MessageService.RunCustomDialog (optionsDialog) == (int) Gtk.ResponseType.Ok) {
						if (entry is IBuildTarget)
							((IBuildTarget)entry).SetNeedsBuilding (true, IdeApp.Workspace.ActiveConfiguration);
						if (entry is IWorkspaceFileObject)
							Save ((IWorkspaceFileObject) entry);
						else {
							SolutionItem si = entry as SolutionItem;
							if (si.ParentSolution != null)
								Save (si.ParentSolution);
						}
						IdeApp.Workspace.SavePreferences ();
					}
				} finally {
					optionsDialog.Destroy ();
				}
			}
		}
		
		public void NewSolution ()
		{
			NewSolution (null);
		}
		
		public void NewSolution (string defaultTemplate)
		{
			NewProjectDialog pd = new NewProjectDialog (null, true, null);
			if (defaultTemplate != null)
				pd.SelectTemplate (defaultTemplate);
			MessageService.ShowCustomDialog (pd);
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
				if (MessageService.RunCustomDialog (npdlg) == (int) Gtk.ResponseType.Ok && npdlg.NewItem != null) {
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
				if (MessageService.RunCustomDialog (fdiag) == (int) Gtk.ResponseType.Ok) {
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
			MessageService.ShowCustomDialog (npdlg);
			return res;
		}

		public SolutionItem AddSolutionItem (SolutionFolder parentFolder)
		{
			SolutionItem res = null;
			
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Add to Solution"));
			try {
				fdiag.SetCurrentFolder (parentFolder.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (MessageService.RunCustomDialog (fdiag) == (int) Gtk.ResponseType.Ok) {
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
				return folder.AddItem (monitor, args.FileName, true);
			}
		}

		public bool CreateProjectFile (Project parentProject, string basePath)
		{
			return CreateProjectFile (parentProject, basePath, null);
		}
		
		public bool CreateProjectFile (Project parentProject, string basePath, string selectedTemplateId)
		{
			NewFileDialog nfd = new NewFileDialog (parentProject, basePath);
			if (selectedTemplateId != null)
				nfd.SelectTemplate (selectedTemplateId);
			return MessageService.ShowCustomDialog (nfd) == (int) Gtk.ResponseType.Ok;
		}

		public bool AddReferenceToProject (DotNetProject project)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
				selDialog.SetProject (project);

				if (MessageService.RunCustomDialog (selDialog) == (int)Gtk.ResponseType.Ok) {
					var newRefs = selDialog.ReferenceInformations;
					
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
		
		public bool SelectProjectReferences (ProjectReferenceCollection references, AssemblyContext ctx, TargetFramework targetVersion)
		{
			try {
				if (selDialog == null)
					selDialog = new SelectReferenceDialog ();
				
				selDialog.SetReferenceCollection (references, ctx, targetVersion);

				if (MessageService.RunCustomDialog (selDialog) == (int)Gtk.ResponseType.Ok) {
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
		
		public void RemoveSolutionItem (SolutionItem item)
		{
			string question = GettextCatalog.GetString ("Do you really want to remove project '{0}' from '{1}'?", item.Name, item.ParentFolder.Name);
			string secondaryText = GettextCatalog.GetString ("The Remove option remove the project from the solution, but it will not physically delete any file from disk.");
			
			SolutionEntityItem prj = item as SolutionEntityItem;
			if (prj == null) {
				if (MessageService.Confirm (question, AlertButton.Remove) && IdeApp.Workspace.RequestItemUnload (item))
					RemoveItemFromSolution (prj);
				return;
			}
			
			AlertButton delete = new AlertButton (GettextCatalog.GetString ("Delete from Disk"));
			AlertButton result = MessageService.AskQuestion (question, secondaryText,
			                                                 delete, AlertButton.Cancel, AlertButton.Remove);
			if (result == delete) {
				if (!IdeApp.Workspace.RequestItemUnload (prj))
					return;
				ConfirmProjectDeleteDialog dlg = new ConfirmProjectDeleteDialog (prj);
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					
					// Remove the project before removing the files to avoid unnecessary events
					RemoveItemFromSolution (prj);
					
					List<FilePath> files = dlg.GetFilesToDelete ();
					dlg.Destroy ();
					using (IProgressMonitor monitor = new MessageDialogProgressMonitor (true)) {
						monitor.BeginTask (GettextCatalog.GetString ("Deleting Files..."), files.Count);
						foreach (FilePath file in files) {
							try {
								if (Directory.Exists (file))
									FileService.DeleteDirectory (file);
								else
									FileService.DeleteFile (file);
							} catch (Exception ex) {
								monitor.ReportError (GettextCatalog.GetString ("The file or directory '{0}' could not be deleted.", file), ex);
							}
							monitor.Step (1);
						}
						monitor.EndTask ();
					}
				} else
					dlg.Destroy ();
			}
			else if (result == AlertButton.Remove && IdeApp.Workspace.RequestItemUnload (prj)) {
				RemoveItemFromSolution (prj);
			}
		}
		
		void RemoveItemFromSolution (SolutionItem prj)
		{
			Solution sol = prj.ParentSolution;
			prj.ParentFolder.Items.Remove (prj);
			prj.Dispose ();
			IdeApp.ProjectOperations.Save (sol);
		}

		public bool CanExecute (IBuildTarget entry)
		{
			ExecutionContext context = new ExecutionContext (Runtime.ProcessService.DefaultExecutionHandler, IdeApp.Workbench.ProgressMonitors);
			return CanExecute (entry, context);
		}
		
		public bool CanExecute (IBuildTarget entry, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return entry.CanExecute (context, IdeApp.Workspace.ActiveConfiguration);
		}
		
		public bool CanExecute (IBuildTarget entry, ExecutionContext context)
		{
			return entry.CanExecute (context, IdeApp.Workspace.ActiveConfiguration);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry)
		{
			return Execute (entry, Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
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
		
		public void Clean (IBuildTarget entry)
		{
			entry.RunTarget (new NullProgressMonitor (), ProjectService.CleanTarget, IdeApp.Workspace.ActiveConfiguration);
		}
		
		public IAsyncOperation BuildFile (string file)
		{
			Project tempProject = MonoDevelop.Projects.Services.ProjectService.CreateSingleFileProject (file);
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
			Project tempProject = MonoDevelop.Projects.Services.ProjectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				IAsyncOperation aop = Execute (tempProject);
				aop.Completed += delegate { tempProject.Dispose (); };
				return aop;
			} else {
				MessageService.ShowError(GettextCatalog.GetString ("No runnable executable found."));
				return NullAsyncOperation.Failure;
			}
		}
		
		public bool CanExecuteFile (string file)
		{
			return CanExecuteFile (file, Runtime.ProcessService.DefaultExecutionHandler);
		}
		
		public bool CanExecuteFile (string file, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return CanExecuteFile (file, context);
		}
		
		public bool CanExecuteFile (string file, ExecutionContext context)
		{
			Project tempProject = MonoDevelop.Projects.Services.ProjectService.CreateSingleFileProject (file);
			if (tempProject != null) {
				bool res = CanExecute (tempProject, context);
				tempProject.Dispose ();
				return res;
			}
			else
				return false;
		}
		
		public IAsyncOperation ExecuteFile (string file, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors);
			return ExecuteFile (file, context);
		}
		
		public IAsyncOperation ExecuteFile (string file, ExecutionContext context)
		{
			Project tempProject = MonoDevelop.Projects.Services.ProjectService.CreateSingleFileProject (file);
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
//		bool errorPadInitialized = false;
		public IAsyncOperation Build (IBuildTarget entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;
			/*
			if (!errorPadInitialized) {
				try {
					Pad errorsPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
					errorsPad.Window.PadHidden += delegate {
						content.IsOpenedAutomatically = false;
					};
					
					Pad monitorPad = IdeApp.Workbench.Pads.FirstOrDefault (pad => pad.Content == ((OutputProgressMonitor)((AggregatedProgressMonitor)monitor).MasterMonitor).OutputPad);
					monitorPad.Window.PadHidden += delegate {
						monitorPad.IsOpenedAutomatically = false;
					};
				} finally {
					errorPadInitialized = true;
				}
			}
			*/
			
			ITimeTracker tt = Counters.BuildItemTimer.BeginTiming ("Building " + entry.Name);
			try {
				tt.Trace ("Pre-build operations");
				DoBeforeCompileAction ();
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
			
				tt.Trace ("Start build event");
				BeginBuild (monitor);

				DispatchService.ThreadDispatch (delegate {
					BuildSolutionItemAsync (entry, monitor, tt);
				}, null);
				currentBuildOperation = monitor.AsyncOperation;
				currentBuildOperationOwner = entry;
				currentBuildOperation.Completed += delegate { currentBuildOperationOwner = null; };
			} catch {
				tt.End ();
				throw;
			}
			return currentBuildOperation;
		}
		
		void BuildSolutionItemAsync (IBuildTarget entry, IProgressMonitor monitor, ITimeTracker tt)
		{
			BuildResult result = null;
			try {
				tt.Trace ("Building item");
				SolutionItem it = entry as SolutionItem;
				if (it != null)
					result = it.Build (monitor, IdeApp.Workspace.ActiveConfiguration, true);
				else
					result = entry.RunTarget (monitor, ProjectService.BuildTarget, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Build failed."), ex);
			} finally {
				tt.Trace ("Done building");
			}
			DispatchService.GuiDispatch (
				delegate {
					BuildDone (monitor, result, entry, tt);	// BuildDone disposes the monitor
			});
		}

		void DoBeforeCompileAction ()
		{
			BeforeCompileAction action = IdeApp.Preferences.BeforeBuildSaveAction;
			
			switch (action) {
				case BeforeCompileAction.Nothing:
					break;
				case BeforeCompileAction.PromptForSave:
					foreach (var doc in IdeApp.Workbench.Documents) {
						if (doc.IsDirty && doc.Project != null) {
							if (MessageService.AskQuestion (
						            GettextCatalog.GetString ("Save changed documents before building?"),
							        GettextCatalog.GetString ("Some of the open documents have unsaved changes."),
							                                AlertButton.BuildWithoutSave, AlertButton.Save) == AlertButton.Save) {
								MarkFileDirty (doc.FileName);
								doc.Save ();
							}
							else
								break;
						}
					}
					break;
				case BeforeCompileAction.SaveAllFiles:
					foreach (var doc in new List<MonoDevelop.Ide.Gui.Document> (IdeApp.Workbench.Documents))
						if (doc.IsDirty && doc.Project != null)
							doc.Save ();
					break;
				default:
					System.Diagnostics.Debug.Assert(false);
					break;
			}
		}

		void BeginBuild (IProgressMonitor monitor)
		{
			TaskService.Errors.ClearByOwner (this);
			if (StartBuild != null)
				StartBuild (this, new BuildEventArgs (monitor, true));
		}
		
		void BuildDone (IProgressMonitor monitor, BuildResult result, IBuildTarget entry, ITimeTracker tt)
		{
			Task[] tasks = null;
			tt.Trace ("Begin reporting build result");
			try {
				if (result != null) {
					lastResult = result;
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine (GettextCatalog.GetString ("---------------------- Done ----------------------"));
					
					tt.Trace ("Updating task service");
					tasks = new Task [result.Errors.Count];
					for (int n=0; n<tasks.Length; n++) {
						tasks [n] = new Task (result.Errors [n]);
						tasks [n].Owner = this;
					}

					TaskService.Errors.AddRange (tasks);
					TaskService.Errors.ResetLocationList ();
					IdeApp.Workbench.ActiveLocationList = TaskService.Errors;
					
					tt.Trace ("Reporting result");
					
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
					tt.Trace ("End build event");
					OnEndBuild (monitor, lastResult.FailedBuildCount == 0);
				} else {
					tt.Trace ("End build event");
					OnEndBuild (monitor, false);
				}
				
				tt.Trace ("Showing results pad");
				
				try {
					Pad errorsPad = IdeApp.Workbench.GetPad<MonoDevelop.Ide.Gui.Pads.ErrorListPad> ();
					switch (IdeApp.Preferences.ShowErrorPadAfterBuild) {
					case BuildResultStates.Always:
						if (!errorsPad.Visible)
							errorsPad.IsOpenedAutomatically = true;
						errorsPad.Visible = true;
						errorsPad.BringToFront ();
						break;
					case BuildResultStates.Never:
						break;
					case BuildResultStates.OnErrors:
						if (TaskService.Errors.Any (task => task.Severity == TaskSeverity.Error))
							goto case BuildResultStates.Always;
						goto case BuildResultStates.Never;
					case BuildResultStates.OnErrorsOrWarnings:
						if (TaskService.Errors.Any (task => task.Severity == TaskSeverity.Error || task.Severity == TaskSeverity.Warning))
							goto case BuildResultStates.Always;
						goto case BuildResultStates.Never;
					}
				} catch {}
				
				Task jumpTask = null;
				switch (IdeApp.Preferences.JumpToFirstErrorOrWarning) {
				case JumpToFirst.Error:
					jumpTask = tasks.FirstOrDefault (t => t.Severity == TaskSeverity.Error);
					break;
				case JumpToFirst.ErrorOrWarning:
					jumpTask = tasks.FirstOrDefault (t => t.Severity == TaskSeverity.Error || t.Severity == TaskSeverity.Warning);
					break;
				}
				if (jumpTask != null) {
					tt.Trace ("Jumping to first result position");
					jumpTask.JumpToPosition ();
				}
				
			} finally {
				monitor.Dispose ();
				tt.End ();
			}
		}
		
		public bool AddFilesToSolutionFolder (SolutionFolder folder)
		{
			var dlg = new SelectFileDialog () {
				SelectMultiple = true,
				Action = Gtk.FileChooserAction.Open,
				CurrentFolder = folder.BaseDirectory,
				TransientFor = MessageService.RootWindow,
			};
			if (dlg.Run ())
				return AddFilesToSolutionFolder (folder, dlg.SelectedFiles);
			else
				return false;
		}
		
		public bool AddFilesToSolutionFolder (SolutionFolder folder, FilePath[] files)
		{
			return AddFilesToSolutionFolder (folder, files.ToStringArray ());
		}
		
		public bool AddFilesToSolutionFolder (SolutionFolder folder, string[] files)
		{
			QuestionMessage msg = new QuestionMessage ();
			AlertButton keepButton = new AlertButton (GettextCatalog.GetString ("Keep file path"));
			msg.Buttons.Add (keepButton);
			msg.Buttons.Add (AlertButton.Copy);
			msg.Buttons.Add (AlertButton.Move);
			msg.Buttons.Add (AlertButton.Cancel);
			msg.AllowApplyToAll = true;
			
			bool someAdded = false;
			
			foreach (string file in files) {
				FilePath fp = file;
				FilePath dest = folder.BaseDirectory.Combine (fp.FileName);
				
				if (folder.IsRoot) {
					// Don't allow adding files to the root folder. VS doesn't allow it
					SolutionFolder newFolder = new SolutionFolder ();
					newFolder.Name = "Solution Items";
					folder.AddItem (newFolder);
					folder = newFolder;
				}
				
				if (!fp.IsChildPathOf (folder.BaseDirectory)) {
					msg.Text = GettextCatalog.GetString ("The file {0} is outside the folder directory. What do you want to do?", fp.FileName);
					AlertButton res = MessageService.AskQuestion (msg);
					if (res == AlertButton.Cancel)
						return someAdded;
					if (res == AlertButton.Copy) {
						FileService.CopyFile (file, dest);
						fp = dest;
					} else if (res == AlertButton.Move) {
						FileService.MoveFile (file, dest);
						fp = dest;
					}
				}
				folder.Files.Add (fp);
				someAdded = true;
			}
			return someAdded;
		}
		
		public IList<ProjectFile> AddFilesToProject (Project project, string[] files, FilePath targetDirectory)
		{
			return AddFilesToProject (project, files.ToFilePathArray (), targetDirectory);
		}
		
		public IList<ProjectFile> AddFilesToProject (Project project, FilePath[] files, FilePath targetDirectory)
		{
			return AddFilesToProject (project, files, targetDirectory, null);
		}
		
		/// <summary>
		/// Adds files to a project, potentially asking the user whether to move, copy or link the files.
		/// </summary>
		public IList<ProjectFile> AddFilesToProject (Project project, FilePath[] files, FilePath targetDirectory,
			string buildAction)
		{
			int action = -1;
			IProgressMonitor monitor = null;
			
			if (files.Length > 10) {
				monitor = new MessageDialogProgressMonitor (true);
				monitor.BeginTask (GettextCatalog.GetString("Adding files..."), files.Length);
			}
			
			var newFileList = new List<ProjectFile> ();
			
			using (monitor) {
				foreach (FilePath file in files) {
					if (monitor != null) {
						monitor.Log.WriteLine (file);
						monitor.Step (1);
					}
					
					if (FileService.IsDirectory (file)) {
						//FIXME: warning about skipping?
						newFileList.Add (null);
						continue;
					}
					
					//files in the project directory get added directly in their current location without moving/copying
					if (file.IsChildPathOf (project.BaseDirectory)) {
						newFileList.Add (project.AddFile (file, buildAction));
						continue;
					}
					
					//for files outside the project directory, we ask the user whether to move, copy or link
					var md = new Gtk.MessageDialog (
						 IdeApp.Workbench.RootWindow,
						 Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
						 Gtk.MessageType.Question, Gtk.ButtonsType.None,
						 GettextCatalog.GetString ("The file {0} is outside the project directory. What would you like to do?", file));

					try {
						Gtk.CheckButton remember = null;
						if (files.Length > 1) {
							remember = new Gtk.CheckButton (GettextCatalog.GetString ("Use the same action for all selected files."));
							md.VBox.PackStart (remember, false, false, 0);
						}
						
						const int ACTION_LINK = 3;
						const int ACTION_COPY = 1;
						const int ACTION_MOVE = 2;
						
						md.AddButton (GettextCatalog.GetString ("_Link"), ACTION_LINK);
						md.AddButton (Gtk.Stock.Copy, ACTION_COPY);
						md.AddButton (GettextCatalog.GetString ("_Move"), ACTION_MOVE);
						md.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
						md.VBox.ShowAll ();
						
						int ret = -1;
						if (action < 0) {
							ret = MessageService.RunCustomDialog (md);
							if (ret < 0)
								return newFileList;
							if (remember != null && remember.Active) action = ret;
						} else {
							ret = action;
						}
						
						var targetName = targetDirectory.Combine (file.FileName);
						
						if (ret == ACTION_LINK) {
							var pf = project.AddFile (file, buildAction);
							pf.Link = project.GetRelativeChildPath (targetName);
							newFileList.Add (pf);
							continue;
						}
						
						try {
							if (MoveCopyFile (file, targetName, ret == ACTION_MOVE))
								newFileList.Add (project.AddFile (targetName, buildAction));
							else
								newFileList.Add (null);
						}
						catch (Exception ex) {
							MessageService.ShowException (ex, GettextCatalog.GetString (
								"An error occurred while attempt to move/copy that file. Please check your permissions."));
							newFileList.Add (null);
						}
					} finally {
						md.Destroy ();
					}
				}
			}
			return newFileList;
		}
		
		bool MoveCopyFile (string filename, string targetFilename, bool move)
		{
			if (filename != targetFilename) {
				if (File.Exists (targetFilename)) {
					if (!MessageService.Confirm (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to replace it?",
					                                                       targetFilename), AlertButton.OverwriteFile))
						return false;
				}
				FileService.CopyFile (filename, targetFilename);
				if (move)
					FileService.DeleteFile (filename);
			}
			return true;
		}		
		
		public void TransferFiles (IProgressMonitor monitor, Project sourceProject, FilePath sourcePath, Project targetProject,
		                           FilePath targetPath, bool removeFromSource, bool copyOnlyProjectFiles)
		{
			// When transfering directories, targetPath is the directory where the source
			// directory will be transfered, including the destination directory or file name.
			// For example, if sourcePath is /a1/a2/a3 and targetPath is /b1/b2, the
			// new folder or file will be /b1/b2
			
			if (targetProject == null)
				throw new ArgumentNullException ("targetProject");

			if (!targetPath.IsChildPathOf (targetProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + targetPath);

			if (sourceProject != null && !sourcePath.IsChildPathOf (sourceProject.BaseDirectory))
				throw new ArgumentException ("Invalid project folder: " + sourcePath);
				
			if (copyOnlyProjectFiles && sourceProject == null)
				throw new ArgumentException ("A source project must be specified if copyOnlyProjectFiles is True");
			
			bool sourceIsFolder = Directory.Exists (sourcePath);

			bool movingFolder = (removeFromSource && sourceIsFolder && (
					!copyOnlyProjectFiles ||
					IsDirectoryHierarchyEmpty (sourcePath)));

			// Get the list of files to copy

			List<ProjectFile> filesToMove = null;
			try {
				//get the real ProjectFiles
				if (sourceProject != null) {
					var virtualPath = sourcePath.ToRelative (sourceProject.BaseDirectory);
					filesToMove = sourceProject.Files.GetFilesInVirtualPath (virtualPath).ToList ();
				}
				//get all the non-project files and create fake ProjectFiles
				if (!copyOnlyProjectFiles || sourceProject == null) {
					var col = new List<ProjectFile> ();
					GetAllFilesRecursive (sourcePath, col);
					if (sourceProject != null) {
						var names = new HashSet<string> (filesToMove.Select (f => sourceProject.BaseDirectory.Combine (f.ProjectVirtualPath).ToString ()));
						foreach (var f in col)
							if (names.Add (f.Name))
							    filesToMove.Add (f);
					} else {
						filesToMove = col;
					}
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not get any file from '{0}'.", sourcePath), ex);
				return;
			}
			
			// If copying a single file, bring any grouped children along
			ProjectFile sourceParent = null;
			if (filesToMove.Count == 1 && sourceProject != null) {
				var pf = filesToMove[0];
				if (pf != null && pf.HasChildren)
					foreach (ProjectFile child in pf.DependentChildren)
						filesToMove.Add (child);
				sourceParent = pf;
			}
			
			// Ensure that the destination folder is created, even if no files
			// are copied
			
			try {
				if (sourceIsFolder && !Directory.Exists (targetPath) && !movingFolder)
					FileService.CreateDirectory (targetPath);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not create directory '{0}'.", targetPath), ex);
				return;
			}

			// Transfer files
			// If moving a folder, do it all at once
			
			if (movingFolder) {
				try {
					FileService.MoveDirectory (sourcePath, targetPath);
				} catch (Exception ex) {
					monitor.ReportError (GettextCatalog.GetString ("Directory '{0}' could not be moved.", sourcePath), ex);
					return;
				}
			}
			
			monitor.BeginTask (GettextCatalog.GetString ("Copying files..."), filesToMove.Count);
			
			ProjectFile targetParent = null;
			foreach (ProjectFile file in filesToMove) {
				bool fileIsLink = file.Project != null && file.IsLink;
				
				var sourceFile = fileIsLink
					? file.Project.BaseDirectory.Combine (file.ProjectVirtualPath)
					: file.FilePath;
				
				FilePath newFile;
				if (sourceIsFolder)
					newFile = targetPath.Combine (sourceFile.ToRelative (sourcePath));
				else if (sourceFile == sourcePath)
					newFile = targetPath;
				else if (sourceFile.ParentDirectory != targetPath.ParentDirectory)
					newFile = targetPath.ParentDirectory.Combine (sourceFile.ToRelative (sourcePath.ParentDirectory));
				else
					newFile = GetTargetCopyName (sourceFile, false);
				
				if (!movingFolder && !fileIsLink) {
					try {
						FilePath fileDir = newFile.ParentDirectory;
						if (!Directory.Exists (fileDir) && !file.IsLink)
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
				
				if (sourceProject != null) {
					if (removeFromSource && sourceProject.Files.Contains (file))
						sourceProject.Files.Remove (file);
					if (fileIsLink) {
						var linkFile = (sourceProject == targetProject)? file : (ProjectFile) file.Clone ();
						if (movingFolder) {
							var abs = linkFile.Link.ToAbsolute (sourceProject.BaseDirectory);
							var relSrc = abs.ToRelative (sourcePath);
							var absTarg = relSrc.ToAbsolute (targetPath);
							linkFile.Link = absTarg.ToRelative (targetProject.BaseDirectory);
						} else {
							linkFile.Link = newFile.ToRelative (targetProject.BaseDirectory);
						}
						targetProject.Files.Add (linkFile);
					} else if (targetProject.Files.GetFile (newFile) == null) {
						ProjectFile projectFile = (ProjectFile) file.Clone ();
						projectFile.Name = newFile;
						if (targetParent == null) {
							if (file == sourceParent)
								targetParent = projectFile;
						} else if (sourceParent != null) {
							if (projectFile.DependsOn == sourceParent.Name)
								projectFile.DependsOn = targetParent.Name;
						}
						targetProject.Files.Add (projectFile);
					}
				}
				
				monitor.Step (1);
			}
			
			// If this was the last item in the folder, make sure we keep
			// a reference to the folder, so it is not deleted from the tree.
			if (removeFromSource && sourceProject != null) {
				var folder = sourcePath.ParentDirectory;
				if (!sourceProject.Files.GetFilesInVirtualPath (folder).Any ()) {
					var folderFile = new ProjectFile (sourceProject.BaseDirectory.Combine (folder));
					folderFile.Subtype = Subtype.Directory;
					sourceProject.Files.Add (folderFile);
				}
			}
			
			monitor.EndTask ();
		}
		
		internal static FilePath GetTargetCopyName (FilePath path, bool isFolder)
		{
			int n=1;
			// First of all try to find an existing copy tag
			string fn = path.FileNameWithoutExtension;
			for (int i=1; i<100; i++) {
				string copyTag = GetCopyTag (i); 
				if (fn.EndsWith (copyTag)) {
					string newfn = fn.Substring (0, fn.Length - copyTag.Length);
					if (newfn.Trim ().Length > 0) {
						n = i + 1;
						path = path.ParentDirectory.Combine (newfn + path.Extension);
						break;
					}
				}
			}
			FilePath basePath = path;
			while ((!isFolder && File.Exists (path)) || (isFolder && Directory.Exists (path))) {
				string copyTag = GetCopyTag (n);
				path = basePath.ParentDirectory.Combine (basePath.FileNameWithoutExtension + copyTag + basePath.Extension);
				n++;
			}
			return path;
		}
		
		static string GetCopyTag (int n)
		{
			string sc;
			switch (n) {
				case 1: sc = GettextCatalog.GetString ("copy"); break;
				case 2: sc = GettextCatalog.GetString ("another copy"); break;
				case 3: sc = GettextCatalog.GetString ("3rd copy"); break;
				case 4: sc = GettextCatalog.GetString ("4th copy"); break;
				case 5: sc = GettextCatalog.GetString ("5th copy"); break;
				case 6: sc = GettextCatalog.GetString ("6th copy"); break;
				case 7: sc = GettextCatalog.GetString ("7th copy"); break;
				case 8: sc = GettextCatalog.GetString ("8th copy"); break;
				case 9: sc = GettextCatalog.GetString ("9th copy"); break;
				default: sc = GettextCatalog.GetString ("copy {0}"); break;
			}
			return " (" + string.Format (sc, n) + ")";
		}
		
		void GetAllFilesRecursive (string path, List<ProjectFile> files)
		{
			if (File.Exists (path)) {
				files.Add (new ProjectFile (path));
				return;
			}
			
			if (Directory.Exists (path)) {
				foreach (string file in Directory.GetFiles (path))
					files.Add (new ProjectFile (file));
				
				foreach (string dir in Directory.GetDirectories (path))
					GetAllFilesRecursive (dir, files);
			}
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

		void IdeAppWorkspaceItemUnloading (object sender, ItemUnloadingEventArgs args)
		{
			if (IsBuilding (args.Item))
				CurrentBuildOperation.Cancel ();
			if (IsRunning (args.Item)) {
				if (MessageService.Confirm (GettextCatalog.GetString (
						"The project '{0}' is currently running and will have to be stopped. Do you want to continue closing it?",
						currentRunOperationOwner.Name),
						new AlertButton (GettextCatalog.GetString ("Close Project")))) {
					CurrentRunOperation.Cancel ();
				} else
					args.Cancel = true;
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
	}
	
	class ParseProgressMonitorFactory: IProgressMonitorFactory
	{
		public IProgressMonitor CreateProgressMonitor ()
		{
			return new BackgroundProgressMonitor (GettextCatalog.GetString ("Code completion database generation"), "md-parser");
		}
	}
	
	public class TextFileProvider : ITextFileProvider
	{
		static TextFileProvider instance = new TextFileProvider ();
		public static TextFileProvider Instance {
			get {
				return instance;
			}
		}
		
		TextFileProvider ()
		{
		}
		
		class ProviderProxy : ITextEditorDataProvider, IEditableTextFile
		{
			TextEditorData data;
			public ProviderProxy (TextEditorData data)
			{
				this.data = data;
			}

			public TextEditorData GetTextEditorData ()
			{
				return data;
			}
			
			#region IEditableTextFile implementation
			public FilePath Name { get { return data.Document.FileName; } }

			public int Length { get { return data.Length; } }
		
			public string GetText (int startPosition, int endPosition)
			{
				return data.GetTextBetween (startPosition, endPosition);
			}
			public char GetCharAt (int position)
			{
				return data.GetCharAt (position);
			}
			
			public int GetPositionFromLineColumn (int line, int column)
			{
				return data.Document.LocationToOffset (line, column);
			}
			
			public void GetLineColumnFromPosition (int position, out int line, out int column)
			{
				var loc = data.Document.OffsetToLocation (position);
				line = loc.Line;
				column = loc.Column;
			}
			
			public int InsertText (int position, string text)
			{
				int result = data.Insert (position, text);
				File.WriteAllText (Name, Text);
				return result;
			}
			
			
			public void DeleteText (int position, int length)
			{
				data.Remove (position, length);
				File.WriteAllText (Name, Text);
			}
			
			public string Text {
				get {
					return data.Text;
				}
				set {
					data.Text = value;
				}
			}
			
			#endregion
		}
		
		public IEditableTextFile GetEditableTextFile (FilePath filePath)
		{
			foreach (var doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					IEditableTextFile ef = doc.GetContent<IEditableTextFile> ();
					if (ef != null) return ef;
				}
			}
			
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = filePath;
			data.Text = File.ReadAllText (filePath);
			return new ProviderProxy (data);
		}
		
	}
}
