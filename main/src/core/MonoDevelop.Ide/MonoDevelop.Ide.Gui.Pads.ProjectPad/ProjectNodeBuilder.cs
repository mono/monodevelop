//
// ProjectNodeBuilder.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class ProjectNodeBuilder: FolderNodeBuilder
	{
		ProjectFileEventHandler fileAddedHandler;
		ProjectFileEventHandler fileRemovedHandler;
		ProjectFileRenamedEventHandler fileRenamedHandler;
		ProjectFileEventHandler filePropertyChangedHandler;
		SolutionItemModifiedEventHandler projectChanged;
		
		public override Type NodeDataType {
			get { return typeof(Project); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectNodeCommandHandler); }
		}
		
		protected override void Initialize ()
		{
			fileAddedHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (OnAddFile));
			fileRemovedHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (OnRemoveFile));
			filePropertyChangedHandler = (ProjectFileEventHandler) DispatchService.GuiDispatch (new ProjectFileEventHandler (OnFilePropertyChanged));
			fileRenamedHandler = (ProjectFileRenamedEventHandler) DispatchService.GuiDispatch (new ProjectFileRenamedEventHandler (OnRenameFile));
			projectChanged = (SolutionItemModifiedEventHandler) DispatchService.GuiDispatch (new SolutionItemModifiedEventHandler (OnProjectModified));
			
			IdeApp.Workspace.FileAddedToProject += fileAddedHandler;
			IdeApp.Workspace.FileRemovedFromProject += fileRemovedHandler;
			IdeApp.Workspace.FileRenamedInProject += fileRenamedHandler;
			IdeApp.Workspace.FilePropertyChangedInProject += filePropertyChangedHandler;
			
			IdeApp.Workspace.ActiveConfigurationChanged += IdeAppWorkspaceActiveConfigurationChanged;
		}

		public override void Dispose ()
		{
			IdeApp.Workspace.FileAddedToProject -= fileAddedHandler;
			IdeApp.Workspace.FileRemovedFromProject -= fileRemovedHandler;
			IdeApp.Workspace.FileRenamedInProject -= fileRenamedHandler;
			IdeApp.Workspace.FilePropertyChangedInProject -= filePropertyChangedHandler;
			IdeApp.Workspace.ActiveConfigurationChanged -= IdeAppWorkspaceActiveConfigurationChanged;
		}

		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			Project project = (Project) dataObject;
			project.Modified += projectChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			Project project = (Project) dataObject;
			project.Modified -= projectChanged;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Project)dataObject).Name;
		}
		
		public override string GetFolderPath (object dataObject)
		{
			return ((Project)dataObject).BaseDirectory;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			base.BuildNode (treeBuilder, dataObject, ref label, ref icon, ref closedIcon);

			Project p = dataObject as Project;
			
			string escapedProjectName = GLib.Markup.EscapeText (p.Name);
			string iconName;
			
			if (p is DotNetProject && ((DotNetProject)p).LanguageBinding == null) {
				iconName = Gtk.Stock.DialogError;
				label = GettextCatalog.GetString ("{0} <span foreground='red' size='small'>(Unknown language '{1}')</span>", escapedProjectName, ((DotNetProject)p).LanguageName);
			} else if (p is UnknownProject) {
				iconName = Gtk.Stock.DialogError;
				label = GettextCatalog.GetString ("{0} <span foreground='red' size='small'>(Unknown project type)</span>", escapedProjectName);
			} else {
				iconName = p.StockIcon;
				if (p.ParentSolution != null && p.ParentSolution.SingleStartup && p.ParentSolution.StartupItem == p)
					label = "<b>" + escapedProjectName + "</b>";
				else
					label = escapedProjectName;
			}
			
			icon = Context.GetIcon (iconName);
			
			// Gray out the project name if it is not selected in the current build configuration
			
			SolutionConfiguration conf = p.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
			if (conf == null || !conf.BuildEnabledForItem (p)) {
				Gdk.Pixbuf ticon = Context.GetComposedIcon (icon, "project-no-build");
				if (ticon == null)
					ticon = Context.CacheComposedIcon (icon, "project-no-build", ImageService.MakeTransparent (icon, 0.5));
				icon = ticon;
				label = "<span foreground='gray'>" + label + " <small>(not built in active configuration)</small></span>";
			}
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project) dataObject;
			if (project is DotNetProject) {
				builder.AddChild (((DotNetProject)project).References);
			}
			
			base.BuildChildNodes (builder, dataObject);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override object GetParentObject (object dataObject)
		{
			SolutionItem it = (SolutionItem) dataObject;
			if (it.ParentFolder == null)
				return null;
			
			return it.ParentFolder.IsRoot ? (object) it.ParentSolution : (object) it.ParentFolder;
		}
		
		void OnAddFile (object sender, ProjectFileEventArgs args)
		{
			if (args.CommonProject != null && args.Count > 2 && args.SingleVirtualDirectory) {
				ITreeBuilder tb = GetFolder (args.CommonProject, args.CommonVirtualRootDirectory);
				if (tb != null)
					tb.UpdateChildren ();
			}
			else {
				foreach (ProjectFileEventInfo e in args)
					AddFile (e.ProjectFile, e.Project);
			}
		}
		
		void OnRemoveFile (object sender, ProjectFileEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args)
				RemoveFile (e.ProjectFile, e.Project);
		}
		
		void AddFile (ProjectFile file, Project project)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();
			
			if (file.DependsOnFile != null) {
				if (!tb.MoveToObject (file.DependsOnFile)) {
					// The parent is not in the tree. Add it now, and it will add this file as a child.
					AddFile (file.DependsOnFile, project);
				}
				else
					tb.AddChild (file);
				return;
			}
			
			object data;
			if (file.Subtype == Subtype.Directory)
				data = new ProjectFolder (file.Name, project);
			else
				data = file;
				
			// Already there?
			if (tb.MoveToObject (data))
				return;
			
			string filePath = file.IsLink
				? project.BaseDirectory.Combine (file.ProjectVirtualPath).ParentDirectory
				: file.FilePath.ParentDirectory;
			
			tb = GetFolder (project, filePath);
			if (tb != null)
				tb.AddChild (data);
		}
		
		ITreeBuilder GetFolder (Project project, FilePath filePath)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();
			if (filePath != project.BaseDirectory) {
				if (tb.MoveToObject (new ProjectFolder (filePath, project))) {
					return tb;
				}
				else {
					// Make sure there is a path to that folder
					tb = FindParentFolderNode (filePath, project);
					if (tb != null) {
						tb.UpdateChildren ();
						return null;
					}
				}
			} else {
				if (tb.MoveToObject (project))
					return tb;
			}
			return null;
		}
		
		ITreeBuilder FindParentFolderNode (string path, Project project)
		{
			int i = path.LastIndexOf (Path.DirectorySeparatorChar);
			if (i == -1) return null;
			
			string basePath = path.Substring (0, i);
			
			if (basePath == project.BaseDirectory)
				return Context.GetTreeBuilder (project);
				
			ITreeBuilder tb = Context.GetTreeBuilder (new ProjectFolder (basePath, project));
			if (tb != null) return tb;
			
			return FindParentFolderNode (basePath, project);
		}
		
		void RemoveFile (ProjectFile file, Project project)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();
			
			if (file.Subtype == Subtype.Directory) {
				if (!tb.MoveToObject (new ProjectFolder (file.Name, project)))
					return;
				tb.MoveToParent ();
				tb.UpdateAll ();
				return;
			} else {
				if (tb.MoveToObject (file)) {
					tb.Remove (true);
				} else {
					// We can't use IsExternalToProject here since the ProjectFile has
					// already been removed from the project
					string parentPath = file.IsLink
						? project.BaseDirectory.Combine (file.Link.IsNullOrEmpty? file.FilePath.FileName : file.Link.ToString ()).ParentDirectory
						: file.FilePath.ParentDirectory;
					
					if (!tb.MoveToObject (new ProjectFolder (parentPath, project)))
						return;
				}
			}
			
			while (tb.DataItem is ProjectFolder) {
				ProjectFolder f = (ProjectFolder) tb.DataItem;
				if (!Directory.Exists (f.Path) && !project.Files.GetFilesInVirtualPath (f.Path.ToRelative (project.BaseDirectory)).Any ())
					tb.Remove (true);
				else
					break;
			}
		}
		
		void OnRenameFile (object sender, ProjectFileRenamedEventArgs args)
		{
			foreach (ProjectFileEventInfo e in args) {
				ITreeBuilder tb = Context.GetTreeBuilder (e.ProjectFile);
				if (tb != null) tb.Update ();
			}
		}
		
		void OnProjectModified (object sender, SolutionItemModifiedEventArgs args)
		{
			foreach (SolutionItemModifiedEventInfo e in args) {
				if (e.Hint == "References" || e.Hint == "Files")
					continue;
				ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
				if (tb != null) {
					if (e.Hint == "BaseDirectory" || e.Hint == "TargetFramework")
						tb.UpdateAll ();
					else
						tb.Update ();
				}
			}
		}
		
		void OnFilePropertyChanged (object sender, ProjectFileEventArgs e)
		{
			foreach (ProjectFileEventInfo args in e) {
				ITreeBuilder tb = Context.GetTreeBuilder (args.Project);
				if (tb != null) tb.UpdateAll ();
			}
		}
		
		void IdeAppWorkspaceActiveConfigurationChanged (object sender, EventArgs e)
		{
			foreach (Project p in IdeApp.Workspace.GetAllProjects ()) {
				ITreeBuilder tb = Context.GetTreeBuilder (p);
				if (tb != null) {
					tb.Update ();
					SolutionConfiguration conf = p.ParentSolution.GetConfiguration (IdeApp.Workspace.ActiveConfiguration);
					if (conf == null || !conf.BuildEnabledForItem (p))
						tb.Expanded = false;
				}
			}
		}
		
	}
	
	class ProjectNodeCommandHandler: FolderCommandHandler
	{
		public override string GetFolderPath (object dataObject)
		{
			return ((Project)dataObject).BaseDirectory;
		}
		
		public override void RenameItem (string newName)
		{
			Project project = (Project) CurrentNode.DataItem;
			IdeApp.ProjectOperations.RenameItem (project, newName);
		}
		
		public override void ActivateItem ()
		{
			Project project = (Project) CurrentNode.DataItem;
			IdeApp.ProjectOperations.ShowOptions (project);
		}
		
		[CommandHandler (ProjectCommands.SetAsStartupProject)]
		public void SetAsStartupProject ()
		{
			Project project = CurrentNode.DataItem as Project;
			project.ParentSolution.SingleStartup = true;
			project.ParentSolution.StartupItem = project;
			IdeApp.ProjectOperations.Save (project.ParentSolution);
		}
		
		public override void DeleteItem ()
		{
			Project prj = CurrentNode.DataItem as Project;
			IdeApp.ProjectOperations.RemoveSolutionItem (prj);
		}
		
		[CommandHandler (ProjectCommands.AddReference)]
		public void AddReferenceToProject ()
		{
			DotNetProject p = (DotNetProject) CurrentNode.DataItem;
			if (IdeApp.ProjectOperations.AddReferenceToProject (p))
				IdeApp.ProjectOperations.Save (p);
		}
		
		[CommandUpdateHandler (ProjectCommands.AddReference)]
		public void UpdateAddReferenceToProject (CommandInfo ci)
		{
			ci.Visible = CurrentNode.DataItem is DotNetProject;
		}
		
		[CommandHandler (ProjectCommands.Reload)]
		[AllowMultiSelection]
		public void OnReload ()
		{
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
				m.BeginTask (null, CurrentNodes.Length);
				foreach (ITreeNavigator nav in CurrentNodes) {
					Project p = (Project) nav.DataItem;
					p.ParentFolder.ReloadItem (m, p);
					m.Step (1);
				}
				m.EndTask ();
			}
		}
		
		[CommandUpdateHandler (ProjectCommands.Reload)]
		public void OnUpdateReload (CommandInfo info)
		{
			foreach (ITreeNavigator nav in CurrentNodes) {
				Project p = (Project) nav.DataItem;
				if (p.ParentFolder == null || !p.NeedsReload) {
					info.Visible = false;
					return;
				}
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return base.CanDropNode (dataObject, operation);
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			base.OnNodeDrop (dataObject, operation);
		}
	}
}
