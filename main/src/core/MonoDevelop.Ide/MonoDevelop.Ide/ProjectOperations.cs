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
using System.Diagnostics;
using ICSharpCode.NRefactory.Documentation;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Text;

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
			set {
				currentRunOperation = value ?? NullAsyncOperation.Success;
				OnCurrentRunOperationChanged (EventArgs.Empty);
			}
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
		
		public bool CanJumpToDeclaration (object element)
		{
			if (element is ICSharpCode.NRefactory.TypeSystem.IVariable)
				return true;
			var entity = element as ICSharpCode.NRefactory.TypeSystem.IEntity;
			if (entity == null && element is ICSharpCode.NRefactory.TypeSystem.IType)
				entity = ((ICSharpCode.NRefactory.TypeSystem.IType)element).GetDefinition ();
			if (entity == null)
				return false;
			if (entity.Region.IsEmpty) {
				var parentAssembly = entity.ParentAssembly;
				if (parentAssembly == null)
					return false;
				return !string.IsNullOrEmpty (parentAssembly.UnresolvedAssembly.Location);
			}
			return true;
		}

		static MonoDevelop.Ide.FindInFiles.SearchResult GetJumpTypePartSearchResult (ICSharpCode.NRefactory.TypeSystem.IUnresolvedTypeDefinition part)
		{
			var provider = new MonoDevelop.Ide.FindInFiles.FileProvider (part.Region.FileName);
			var doc = new Mono.TextEditor.TextDocument ();
			doc.Text = provider.ReadString ();
			int position = doc.LocationToOffset (part.Region.BeginLine, part.Region.BeginColumn);
			while (position + part.Name.Length < doc.TextLength) {
				if (doc.GetTextAt (position, part.Name.Length) == part.Name)
					break;
				position++;
			}
			return new MonoDevelop.Ide.FindInFiles.SearchResult (provider, position, part.Name.Length);
		}

		public void JumpToDeclaration (ICSharpCode.NRefactory.TypeSystem.INamedElement visitable, bool askIfMultipleLocations = true)
		{
			if (askIfMultipleLocations) {
				var type = visitable as ICSharpCode.NRefactory.TypeSystem.IType;
				if (type != null && type.GetDefinition () != null && type.GetDefinition ().Parts.Count > 1) {
					using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
						foreach (var part in type.GetDefinition ().Parts)
							monitor.ReportResult (GetJumpTypePartSearchResult (part));
					}
					return;
				}
			}

			JumpToDeclaration (visitable);
		}

		void JumpToDeclaration (ICSharpCode.NRefactory.TypeSystem.INamedElement element)
		{
			var entity = element as ICSharpCode.NRefactory.TypeSystem.IEntity;

			if (entity == null && element is ICSharpCode.NRefactory.TypeSystem.IType)
				entity = ((ICSharpCode.NRefactory.TypeSystem.IType)element).GetDefinition ();
			if (entity is SpecializedMember) 
				entity = ((SpecializedMember)entity).MemberDefinition;

			if (entity == null) {
				LoggingService.LogError ("Unknown element:" + element);
				return;
			}
			string fileName;
			bool isCecilProjectContent = entity.Region.IsEmpty;
			if (isCecilProjectContent) {
				fileName = entity.ParentAssembly.UnresolvedAssembly.Location;
			} else {
				fileName = entity.Region.FileName;
			}
			var doc = IdeApp.Workbench.OpenDocument (fileName,
			                               entity.Region.BeginLine,
			                               entity.Region.BeginColumn);

			if (isCecilProjectContent && doc != null) {
				doc.RunWhenLoaded (delegate {
					var handler = doc.PrimaryView.GetContent<MonoDevelop.Ide.Gui.Content.IUrlHandler> ();
					if (handler != null)
						handler.Open (entity.GetIdString ());
				});
			}
		}

		public void JumpToDeclaration (ICSharpCode.NRefactory.TypeSystem.IVariable entity)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");
			string fileName = entity.Region.FileName;
			IdeApp.Workbench.OpenDocument (fileName, entity.Region.BeginLine, entity.Region.BeginColumn);
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
		
		public void Export (WorkspaceItem item)
		{
			Export (item, null);
		}
		
		public void Export (WorkspaceItem item, FileFormat format)
		{
			ExportSolutionDialog dlg = new ExportSolutionDialog (item, format);
			
			try {
				if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
					using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetToolOutputProgressMonitor (true)) {
						Services.ProjectService.Export (monitor, item.FileName, dlg.TargetFolder, dlg.Format);
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
			if (HasChanged (item)) {
				return MessageService.Confirm (
					GettextCatalog.GetString (
						"Some project files have been changed from outside {0}. Do you want to overwrite them?",
						BrandingService.ApplicationName
					),
					GettextCatalog.GetString (
						"Changes done in those files will be overwritten by {0}.",
						BrandingService.ApplicationName
					),
					AlertButton.OverwriteFile);
			} else {
				return true;
			}
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
				optionsDialog.CurrentPlatform = conf != null ? conf.Platform : null;
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
			
			var dlg = new SelectFileDialog () {
				Action = Gtk.FileChooserAction.Open,
				CurrentFolder = parentWorkspace.BaseDirectory,
				SelectMultiple = false,
			};
		
			dlg.AddAllFilesFilter ();
			dlg.DefaultFilter = dlg.AddFilter (GettextCatalog.GetString ("Solution Files"), "*.mds", "*.sln");
			
			if (dlg.Run ()) {
				try {
					res = AddWorkspaceItem (parentWorkspace, dlg.SelectedFile);
				} catch (Exception ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", dlg.SelectedFile));
				}
			}

			return res;
		}
		
		public WorkspaceItem AddWorkspaceItem (Workspace parentWorkspace, string itemFileName)
		{
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
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
			string basePath = parentFolder != null ? parentFolder.BaseDirectory : null;
			NewProjectDialog npdlg = new NewProjectDialog (parentFolder, false, basePath);
			if (MessageService.ShowCustomDialog (npdlg) == (int)Gtk.ResponseType.Ok) {
				var item = npdlg.NewItem as SolutionItem;
				if ((item is Project) && ProjectCreated != null)
					ProjectCreated (this, new ProjectCreatedEventArgs (item as Project));
				return item;
			}
			return null;
		}

		public SolutionItem AddSolutionItem (SolutionFolder parentFolder)
		{
			SolutionItem res = null;
			
			var dlg = new SelectFileDialog () {
				Action = Gtk.FileChooserAction.Open,
				CurrentFolder = parentFolder.BaseDirectory,
				SelectMultiple = false,
			};
		
			dlg.AddAllFilesFilter ();
			dlg.DefaultFilter = dlg.AddFilter (GettextCatalog.GetString ("Project Files"), "*.*proj", "*.mdp");
			
			if (dlg.Run ()) {
				try {
					res = AddSolutionItem (parentFolder, dlg.SelectedFile);
				} catch (Exception ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("The file '{0}' could not be loaded.", dlg.SelectedFile));
				}
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
			using (IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
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
					
					var editEventArgs = new EditReferencesEventArgs (project);
					foreach (ProjectReference refInfo in project.References)
						if (!newRefs.Contains (refInfo))
							editEventArgs.ReferencesToRemove.Add (refInfo);

					foreach (ProjectReference refInfo in selDialog.ReferenceInformations)
						if (!project.References.Contains (refInfo))
							editEventArgs.ReferencesToAdd.Add(refInfo);

					if (BeforeEditReferences != null)
						BeforeEditReferences (this, editEventArgs);

					foreach (var reference in editEventArgs.ReferencesToRemove)
						project.References.Remove (reference);

					foreach (var reference in editEventArgs.ReferencesToAdd)
						project.References.Add (reference);

					return editEventArgs.ReferencesToAdd.Count > 0 || editEventArgs.ReferencesToRemove.Count > 0;
				}
				else
					return false;
			} finally {
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
			foreach (var doc in IdeApp.Workbench.Documents.Where (d => d.Project == prj).ToArray ())
				doc.Close ();
			Solution sol = prj.ParentSolution;
			prj.ParentFolder.Items.Remove (prj);
			prj.Dispose ();
			IdeApp.ProjectOperations.Save (sol);
		}
		
		/// <summary>
		/// Checks if an execution operation can start (asking the user if necessary)
		/// </summary>
		/// <returns>
		/// True if execution can continue, false otherwise
		/// </returns>
		/// <remarks>
		/// This method must be called before starting an execution operation. If there is already an execution in
		/// progress, MonoDevelop will ask confirmation for stopping the current operation.
		/// </remarks>
		public bool ConfirmExecutionOperation ()
		{
			if (!currentRunOperation.IsCompleted) {
				if (MessageService.Confirm (GettextCatalog.GetString ("An application is already running and will have to be stopped. Do you want to continue?"), AlertButton.Yes)) {
					if (currentRunOperation != null && !currentRunOperation.IsCompleted)
						currentRunOperation.Cancel ();
					return true;
				} else
					return false;
			} else
				return true;
		}

		public bool CanExecute (IBuildTarget entry)
		{
			ExecutionContext context = new ExecutionContext (Runtime.ProcessService.DefaultExecutionHandler, IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);
			return CanExecute (entry, context);
		}
		
		public bool CanExecute (IBuildTarget entry, IExecutionHandler handler)
		{
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);
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
			ExecutionContext context = new ExecutionContext (handler, IdeApp.Workbench.ProgressMonitors, IdeApp.Workspace.ActiveExecutionTarget);
			return Execute (entry, context);
		}
		
		public IAsyncOperation Execute (IBuildTarget entry, ExecutionContext context)
		{
			if (currentRunOperation != null && !currentRunOperation.IsCompleted) return currentRunOperation;

			NullProgressMonitor monitor = new NullProgressMonitor ();

			DispatchService.ThreadDispatch (delegate {
				ExecuteSolutionItemAsync (monitor, entry, context);
			});
			CurrentRunOperation = monitor.AsyncOperation;
			currentRunOperationOwner = entry;
			currentRunOperation.Completed += delegate {
			 	DispatchService.GuiDispatch (() => {
					var error = monitor.Errors.FirstOrDefault ();
					if (error != null)
						IdeApp.Workbench.StatusBar.ShowError (error.Message);
					currentRunOperationOwner = null;
				});
			};
			return currentRunOperation;
		}
		
		void ExecuteSolutionItemAsync (IProgressMonitor monitor, IBuildTarget entry, ExecutionContext context)
		{
			try {
				OnBeforeStartProject ();
				entry.Execute (monitor, context, IdeApp.Workspace.ActiveConfiguration);
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Execution failed."), ex);
				LoggingService.LogError ("Execution failed", ex);
			} finally {
				monitor.Dispose ();
			}
		}
		
		public IAsyncOperation Clean (IBuildTarget entry)
		{
			if (currentBuildOperation != null && !currentBuildOperation.IsCompleted) return currentBuildOperation;
			
			ITimeTracker tt = Counters.BuildItemTimer.BeginTiming ("Cleaning " + entry.Name);
			try {
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetCleanProgressMonitor ();
				OnStartClean (monitor, tt);
				DispatchService.ThreadDispatch (() => CleanAsync (entry, monitor, tt, false));
				
				currentBuildOperation = monitor.AsyncOperation;
				currentBuildOperationOwner = entry;
				currentBuildOperation.Completed += delegate {
					currentBuildOperationOwner = null;
				};
			}
			catch {
				tt.End ();
				throw;
			}
			
			return currentBuildOperation;
		}
		
		void CleanAsync (IBuildTarget entry, IProgressMonitor monitor, ITimeTracker tt, bool isRebuilding)
		{
			try {
				tt.Trace ("Cleaning item");
				SolutionItem it = entry as SolutionItem;
				if (it != null) {
					it.Clean (monitor, IdeApp.Workspace.ActiveConfiguration);
				}
				else {
					entry.RunTarget (monitor, ProjectService.CleanTarget, IdeApp.Workspace.ActiveConfiguration);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Clean failed."), ex);
			} finally {
				tt.Trace ("Done cleaning");
			}
			
			if (isRebuilding) {
				if (EndClean != null) {
					DispatchService.GuiSyncDispatch (() => OnEndClean (monitor, tt));
				}
			} else {
				DispatchService.GuiDispatch (() => CleanDone (monitor, entry, tt));
			}
		}
		
		void CleanDone (IProgressMonitor monitor, IBuildTarget entry, ITimeTracker tt)
		{
			tt.Trace ("Begin reporting clean result");
			try {
				monitor.Log.WriteLine ();
				monitor.Log.WriteLine (GettextCatalog.GetString ("---------------------- Done ----------------------"));
				tt.Trace ("Reporting result");			
				monitor.ReportSuccess (GettextCatalog.GetString ("Clean successful."));
				OnEndClean (monitor, tt);
			} finally {
				monitor.Dispose ();
				tt.End ();
			}
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
			else {
				var cmd = Runtime.ProcessService.CreateCommand (file);
				if (context.ExecutionHandler.CanExecute (cmd))
					return true;
			}
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
			
			ITimeTracker tt = Counters.BuildItemTimer.BeginTiming ("Rebuilding " + entry.Name);
			try {
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetRebuildProgressMonitor ();
				OnStartClean (monitor, tt);

				DispatchService.ThreadDispatch (delegate {
					CleanAsync (entry, monitor, tt, true);
					if (monitor.AsyncOperation.IsCompleted && !monitor.AsyncOperation.Success) {
						tt.End ();
						monitor.Dispose ();
						return;
					}
					if (StartBuild != null) {
						DispatchService.GuiSyncDispatch (() => BeginBuild (monitor, tt, true));
					}
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
				IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor ();
				BeginBuild (monitor, tt, false);
				DispatchService.ThreadDispatch (() => BuildSolutionItemAsync (entry, monitor, tt));
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
				tt.Trace ("Pre-build operations");
				result = DoBeforeCompileAction ();

				if (result.ErrorCount == 0) {
					tt.Trace ("Building item");
					SolutionItem it = entry as SolutionItem;
					if (it != null)
						result = it.Build (monitor, IdeApp.Workspace.ActiveConfiguration, true);
					else
						result = entry.RunTarget (monitor, ProjectService.BuildTarget, IdeApp.Workspace.ActiveConfiguration);
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Build failed."), ex);
				result.AddError ("Build failed. See the build log for details.");
			} finally {
				tt.Trace ("Done building");
			}
			DispatchService.GuiDispatch (
				delegate {
					BuildDone (monitor, result, entry, tt);	// BuildDone disposes the monitor
			});
		}
		
		// Note: This must run in the main thread
		void PromptForSave (BuildResult result)
		{
			var couldNotSaveError = "The build has been aborted as the file '{0}' could not be saved";
			
			foreach (var doc in IdeApp.Workbench.Documents) {
				if (doc.IsDirty && doc.Project != null) {
					if (MessageService.AskQuestion (GettextCatalog.GetString ("Save changed documents before building?"),
					                                GettextCatalog.GetString ("Some of the open documents have unsaved changes."),
					                                AlertButton.BuildWithoutSave, AlertButton.Save) == AlertButton.Save) {
						MarkFileDirty (doc.FileName);
						doc.Save ();
						if (doc.IsDirty)
							result.AddError (string.Format (couldNotSaveError, Path.GetFileName (doc.FileName)), doc.FileName);
					} else
						break;
				}
			}
		}
		
		// Note: This must run in the main thread
		void SaveAllFiles (BuildResult result)
		{
			var couldNotSaveError = "The build has been aborted as the file '{0}' could not be saved";
			
			foreach (var doc in new List<MonoDevelop.Ide.Gui.Document> (IdeApp.Workbench.Documents)) {
				if (doc.IsDirty && doc.Project != null) {
					doc.Save ();
					if (doc.IsDirty)
						result.AddError (string.Format (couldNotSaveError, Path.GetFileName (doc.FileName)), doc.FileName);
				}
			}
		}

		BuildResult DoBeforeCompileAction ()
		{
			BeforeCompileAction action = IdeApp.Preferences.BeforeBuildSaveAction;
			var result = new BuildResult ();
			
			switch (action) {
			case BeforeCompileAction.Nothing: break;
			case BeforeCompileAction.PromptForSave: DispatchService.GuiSyncDispatch (delegate { PromptForSave (result); }); break;
			case BeforeCompileAction.SaveAllFiles: DispatchService.GuiSyncDispatch (delegate { SaveAllFiles (result); }); break;
			default: System.Diagnostics.Debug.Assert (false); break;
			}
			
			return result;
		}

		void BeginBuild (IProgressMonitor monitor, ITimeTracker tt, bool isRebuilding)
		{
			tt.Trace ("Start build event");
			if (!isRebuilding) {
				TaskService.Errors.ClearByOwner (this);
			}
			if (StartBuild != null) {
				StartBuild (this, new BuildEventArgs (monitor, true));
			}
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

					if (monitor.IsCancelRequested) {
						monitor.ReportError (GettextCatalog.GetString ("Build canceled."), null);
					} else if (result.ErrorCount == 0 && result.WarningCount == 0 && lastResult.FailedBuildCount == 0) {
						monitor.ReportSuccess (GettextCatalog.GetString ("Build successful."));
					} else if (result.ErrorCount == 0 && result.WarningCount > 0) {
						monitor.ReportWarning(GettextCatalog.GetString("Build: ") + errorString + ", " + warningString);
					} else if (result.ErrorCount > 0) {
						monitor.ReportError(GettextCatalog.GetString("Build: ") + errorString + ", " + warningString, null);
					} else {
						monitor.ReportError(GettextCatalog.GetString("Build failed."), null);
					}
					tt.Trace ("End build event");
					OnEndBuild (monitor, lastResult.FailedBuildCount == 0, lastResult, entry as SolutionItem);
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
				
				if (tasks != null) {
					Task jumpTask = null;
					switch (IdeApp.Preferences.JumpToFirstErrorOrWarning) {
					case JumpToFirst.Error:
						jumpTask = tasks.FirstOrDefault (t => t.Severity == TaskSeverity.Error && TaskStore.IsProjectTaskFile (t));
						break;
					case JumpToFirst.ErrorOrWarning:
						jumpTask = tasks.FirstOrDefault (t => (t.Severity == TaskSeverity.Error || t.Severity == TaskSeverity.Warning) && TaskStore.IsProjectTaskFile (t));
						break;
					}
					if (jumpTask != null) {
						tt.Trace ("Jumping to first result position");
						jumpTask.JumpToPosition ();
					}
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
					// If there is no existing folder, create one
					var itemsFolder = (SolutionFolder) folder.Items.Where (item => item.Name == "Solution Items").FirstOrDefault ();
					if (itemsFolder == null) {
						itemsFolder = new SolutionFolder ();
						itemsFolder.Name = "Solution Items";
						folder.AddItem (itemsFolder);
					}
					folder = itemsFolder;
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
		
		public IList<ProjectFile> AddFilesToProject (Project project, FilePath[] files, FilePath targetDirectory,
			string buildAction)
		{
			Debug.Assert (targetDirectory.CanonicalPath == project.BaseDirectory.CanonicalPath
				|| targetDirectory.IsChildPathOf (project.BaseDirectory));
			
			var targetPaths = new FilePath[files.Length];
			for (int i = 0; i < files.Length; i++)
				targetPaths[i] = targetDirectory.Combine (files[i].FileName);
			
			return AddFilesToProject (project, files, targetPaths, buildAction);
		}

		/// <summary>
		/// Adds files to a project, potentially asking the user whether to move, copy or link the files.
		/// </summary>
		public IList<ProjectFile> AddFilesToProject (Project project, FilePath[] files, FilePath[] targetPaths,
			string buildAction)
		{
			Debug.Assert (project != null);
			Debug.Assert (files != null);
			Debug.Assert (targetPaths != null);
			Debug.Assert (files.Length == targetPaths.Length);
			
			AddAction action = AddAction.Copy;
			bool applyToAll = true;
			bool dialogShown = false;
			
			IProgressMonitor monitor = null;
			
			if (files.Length > 10) {
				monitor = new MessageDialogProgressMonitor (true);
				monitor.BeginTask (GettextCatalog.GetString("Adding files..."), files.Length);
			}
			
			var newFileList = new List<ProjectFile> ();
			
			//project.AddFile (string) does linear search for duplicate file, so instead we use this HashSet and 
			//and add the ProjectFiles directly. With large project and many files, this should really help perf.
			//Also, this is a better check because we handle vpaths and links.
			//FIXME: it would be really nice if project.Files maintained these hashmaps
			var vpathsInProject = new Dictionary<FilePath, ProjectFile> ();
			var filesInProject = new Dictionary<FilePath,ProjectFile> ();
			foreach (var pf in project.Files) {
				filesInProject [pf.FilePath] = pf;
				vpathsInProject [pf.ProjectVirtualPath] = pf;
			}

			using (monitor)
			{
				for (int i = 0; i < files.Length; i++) {
					FilePath file = files[i];
					
					if (monitor != null) {
						monitor.Log.WriteLine (file);
						monitor.Step (1);
					}
					
					if (FileService.IsDirectory (file)) {
						//FIXME: warning about skipping?
						newFileList.Add (null);
						continue;
					}
					
					FilePath targetPath = targetPaths[i].CanonicalPath;
					Debug.Assert (targetPath.IsChildPathOf (project.BaseDirectory));

					ProjectFile vfile;
					var vpath = targetPath.ToRelative (project.BaseDirectory);
					if (vpathsInProject.TryGetValue (vpath, out vfile)) {
						if (vfile.FilePath != file)
							MessageService.ShowWarning (GettextCatalog.GetString (
								"There is a already a file or link in the project with the name '{0}'", vpath));
						continue;
					}
					
					string fileBuildAction = buildAction;
					if (string.IsNullOrEmpty (buildAction))
						fileBuildAction = project.GetDefaultBuildAction (targetPath);
					
					//files in the target directory get added directly in their current location without moving/copying
					if (file.CanonicalPath == targetPath) {
						AddFileToFolder (newFileList, vpathsInProject, filesInProject, file, fileBuildAction);
						continue;
					}
					
					//for files outside the project directory, we ask the user whether to move, copy or link
					
					AddExternalFileDialog addExternalDialog = null;
					
					if (!dialogShown || !applyToAll) {
						addExternalDialog = new AddExternalFileDialog (file);
						if (files.Length > 1) {
							addExternalDialog.ApplyToAll = applyToAll;
							addExternalDialog.ShowApplyAll = true;
						}
						if (file.IsChildPathOf (targetPath.ParentDirectory))
							addExternalDialog.ShowKeepOption (file.ParentDirectory.ToRelative (targetPath.ParentDirectory));
						else {
							if (action == AddAction.Keep)
								action = AddAction.Copy;
							addExternalDialog.SelectedAction = action;
						}
					}
					
					try {
						if (!dialogShown || !applyToAll) {
							if (MessageService.RunCustomDialog (addExternalDialog) == (int) Gtk.ResponseType.Cancel) {
								project.Files.AddRange (newFileList.Where (f => f != null));
								return newFileList;
							}
							action = addExternalDialog.SelectedAction;
							applyToAll = addExternalDialog.ApplyToAll;
							dialogShown = true;
						}
						
						if (action == AddAction.Keep) {
							AddFileToFolder (newFileList, vpathsInProject, filesInProject, file, fileBuildAction);
							continue;
						}
						
						if (action == AddAction.Link) {
							ProjectFile pf = new ProjectFile (file, fileBuildAction) {
								Link = vpath
							};
							vpathsInProject [pf.ProjectVirtualPath] = pf;
							filesInProject [pf.FilePath] = pf;
							newFileList.Add (pf);
							continue;
						}
						
						try {
							if (!Directory.Exists (targetPath.ParentDirectory))
								FileService.CreateDirectory (targetPath.ParentDirectory);
							
							if (MoveCopyFile (file, targetPath, action == AddAction.Move)) {
								var pf = new ProjectFile (targetPath, fileBuildAction);
								vpathsInProject [pf.ProjectVirtualPath] = pf;
								filesInProject [pf.FilePath] = pf;
								newFileList.Add (pf);
							}
							else {
								newFileList.Add (null);
							}
						}
						catch (Exception ex) {
							MessageService.ShowException (ex, GettextCatalog.GetString (
								"An error occurred while attempt to move/copy that file. Please check your permissions."));
							newFileList.Add (null);
						}
					} finally {
						if (addExternalDialog != null)
							addExternalDialog.Destroy ();
					}
				}
			}
			project.Files.AddRange (newFileList.Where (f => f != null));
			return newFileList;
		}
		
		void AddFileToFolder (List<ProjectFile> newFileList, Dictionary<FilePath, ProjectFile> vpathsInProject, Dictionary<FilePath, ProjectFile> filesInProject, FilePath file, string fileBuildAction)
		{
			//FIXME: MD project system doesn't cope with duplicate includes - project save/load will remove the file
			ProjectFile pf;
			if (filesInProject.TryGetValue (file, out pf)) {
				var link = pf.Link;
				MessageService.ShowWarning (GettextCatalog.GetString (
					"The link '{0}' in the project already includes the file '{1}'", link, file));
				return;
			}
			pf = new ProjectFile (file, fileBuildAction);
			vpathsInProject [pf.ProjectVirtualPath] = pf;
			filesInProject [pf.FilePath] = pf;
			newFileList.Add (pf);
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

			// We need to remove all files + directories from the source project
			// but when dealing with the VCS addins we need to process only the
			// files so we do not create a 'file' in the VCS which corresponds
			// to a directory in the project and blow things up.
			List<ProjectFile> filesToRemove = null;
			List<ProjectFile> filesToMove = null;
			try {
				//get the real ProjectFiles
				if (sourceProject != null) {
					if (sourceIsFolder) {
						var virtualPath = sourcePath.ToRelative (sourceProject.BaseDirectory);
						// Grab all the child nodes of the folder we just dragged/dropped
						filesToRemove = sourceProject.Files.GetFilesInVirtualPath (virtualPath).ToList ();
						// Add the folder itself so we can remove it from the soruce project if its a Move operation
						var folder = sourceProject.Files.Where (f => f.ProjectVirtualPath == virtualPath).FirstOrDefault ();
						if (folder != null)
							filesToRemove.Add (folder);
					} else {
						filesToRemove = new List<ProjectFile> ();
						var pf = sourceProject.Files.GetFileWithVirtualPath (sourceProject.GetRelativeChildPath (sourcePath));
						if (pf != null)
							filesToRemove.Add (pf);
					}
				}
				//get all the non-project files and create fake ProjectFiles
				if (!copyOnlyProjectFiles || sourceProject == null) {
					var col = new List<ProjectFile> ();
					GetAllFilesRecursive (sourcePath, col);
					if (sourceProject != null) {
						var names = new HashSet<string> (filesToRemove.Select (f => sourceProject.BaseDirectory.Combine (f.ProjectVirtualPath).ToString ()));
						foreach (var f in col)
							if (names.Add (f.Name))
							    filesToRemove.Add (f);
					} else {
						filesToRemove = col;
					}
				}
			} catch (Exception ex) {
				monitor.ReportError (GettextCatalog.GetString ("Could not get any file from '{0}'.", sourcePath), ex);
				return;
			}
			
			// Strip out all the directories to leave us with just the files.
			filesToMove = filesToRemove.Where (f => f.Subtype != Subtype.Directory).ToList ();
			
			// If copying a single file, bring any grouped children along
			ProjectFile sourceParent = null;
			if (filesToMove.Count == 1 && sourceProject != null) {
				var pf = filesToMove[0];
				if (pf != null && pf.HasChildren) {
					foreach (ProjectFile child in pf.DependentChildren) {
						filesToRemove.Add (child);
						filesToMove.Add (child);
					}
				}
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

			if (removeFromSource)
				monitor.BeginTask (GettextCatalog.GetString ("Moving files..."), filesToMove.Count);
			else
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
						if (removeFromSource) {
							// File.Move() does not have an overwrite argument and will fail if the destFile path exists, however, the user
							// has already chosen to overwrite the destination file.
							if (File.Exists (newFile))
								File.Delete (newFile);

							FileService.MoveFile (sourceFile, newFile);
						} else
							FileService.CopyFile (sourceFile, newFile);
					} catch (Exception ex) {
						if (removeFromSource)
							monitor.ReportError (GettextCatalog.GetString ("File '{0}' could not be moved.", sourceFile), ex);
						else
							monitor.ReportError (GettextCatalog.GetString ("File '{0}' could not be copied.", sourceFile), ex);
						monitor.Step (1);
						continue;
					}
				}
				
				if (sourceProject != null) {
					if (fileIsLink) {
						var linkFile = (ProjectFile) file.Clone ();
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
			
			if (removeFromSource) {
				// Remove all files and directories under 'sourcePath'
				foreach (var v in filesToRemove)
					sourceProject.Files.Remove (v);
			}
			
			var pfolder = sourcePath.ParentDirectory;
			
			// If this was the last item in the folder, make sure we keep
			// a reference to the folder, so it is not deleted from the tree.
			if (removeFromSource && sourceProject != null && pfolder.CanonicalPath != sourceProject.BaseDirectory.CanonicalPath && pfolder.IsChildPathOf (sourceProject.BaseDirectory)) {
				pfolder = pfolder.ToRelative (sourceProject.BaseDirectory);
				if (!sourceProject.Files.GetFilesInVirtualPath (pfolder).Any ()) {
					var folderFile = new ProjectFile (sourceProject.BaseDirectory.Combine (pfolder));
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

		void OnEndBuild (IProgressMonitor monitor, bool success, BuildResult result = null, SolutionItem item = null)
		{
			if (EndBuild == null)
				return;
					
			var args = new BuildEventArgs (monitor, success) {
				SolutionItem = item
			};
			if (result != null) {
				args.WarningCount = result.WarningCount;
				args.ErrorCount = result.ErrorCount;
				args.BuildCount = result.BuildCount;
				args.FailedBuildCount = result.FailedBuildCount;
			}
			EndBuild (this, args);
		}
		
		void OnStartClean (IProgressMonitor monitor, ITimeTracker tt)
		{
			tt.Trace ("Start clean event");
			TaskService.Errors.ClearByOwner (this);
			if (StartClean != null) {
				StartClean (this, new CleanEventArgs (monitor));
			}
		}
		
		void OnEndClean (IProgressMonitor monitor, ITimeTracker tt)
		{
			tt.Trace ("End clean event");
			if (EndClean != null) {
				EndClean (this, new CleanEventArgs (monitor));
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
		public event CleanEventHandler StartClean;
		public event CleanEventHandler EndClean;
		
		public event EventHandler<SolutionEventArgs> CurrentSelectedSolutionChanged;
		public event ProjectEventHandler CurrentProjectChanged;
		public event EventHandler<ProjectCreatedEventArgs> ProjectCreated;
		
		// Fired just before an entry is added to a combine
		public event AddEntryEventHandler AddingEntryToCombine;

		public event EventHandler CurrentRunOperationChanged;
		public event EventHandler<EditReferencesEventArgs> BeforeEditReferences;
		protected virtual void OnCurrentRunOperationChanged (EventArgs e)
		{
			var handler = CurrentRunOperationChanged;
			if (handler != null)
				handler (this, e);
		}
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
			string encoding;
			bool bom;
			
			public ProviderProxy (TextEditorData data, string encoding, bool bom)
			{
				this.data = data;
				this.encoding = encoding;
				this.bom = bom;
			}

			public TextEditorData GetTextEditorData ()
			{
				return data;
			}
			
			void Save ()
			{
				TextFile.WriteFile (Name, Text, encoding, bom);
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
				Save ();
				
				return result;
			}
			
			public void DeleteText (int position, int length)
			{
				data.Remove (position, length);
				Save ();
			}
			
			public string Text {
				get {
					return data.Text;
				}
				set {
					data.Text = value;
					Save ();
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
			
			TextFile file = TextFile.ReadFile (filePath);
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = filePath;
			data.Text = file.Text;
			
			return new ProviderProxy (data, file.SourceEncoding, file.HadBOM);
		}

		/// <summary>
		/// Performs an edit operation on a text file regardless of it's open in the IDE or not.
		/// </summary>
		/// <returns><c>true</c>, if file operation was saved, <c>false</c> otherwise.</returns>
		/// <param name="filePath">File path.</param>
		/// <param name="operation">The operation.</param>
		public bool EditFile (FilePath filePath, Action<TextEditorData> operation)
		{
			if (operation == null)
				throw new ArgumentNullException ("operation");
			bool hadBom;
			Encoding encoding;
			bool isOpen;
			var data = GetTextEditorData (filePath, out hadBom, out encoding, out isOpen);
			operation (data);
			if (!isOpen) {
				try { 
					Mono.TextEditor.Utils.TextFileUtility.WriteText (filePath, data.Text, encoding, hadBom);
				} catch (Exception e) {
					LoggingService.LogError ("Error while saving changes to : " + filePath, e);
					return false;
				}
			}
			return true;
		}

		public TextEditorData GetTextEditorData (FilePath filePath)
		{
			bool isOpen;
			return GetTextEditorData (filePath, out isOpen);
		}

		public TextEditorData GetTextEditorData (FilePath filePath, out bool isOpen)
		{
			bool hadBom;
			Encoding encoding;
			return GetTextEditorData (filePath, out hadBom, out encoding, out isOpen);
		}

		public TextEditorData GetTextEditorData (FilePath filePath, out bool hadBom, out Encoding encoding, out bool isOpen)
		{
			foreach (var doc in IdeApp.Workbench.Documents) {
				if (doc.FileName == filePath) {
					isOpen = true;
					hadBom = false;
					encoding = Encoding.Default;
					return doc.Editor;
				}
			}

			var text = Mono.TextEditor.Utils.TextFileUtility.ReadAllText (filePath, out hadBom, out encoding);
			TextEditorData data = new TextEditorData ();
			data.Document.FileName = filePath;
			data.Text = text;
			isOpen = false;
			return data;
		}
	}
}
