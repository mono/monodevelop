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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectNodeBuilder: FolderNodeBuilder
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
		}
		
		public override void Dispose ()
		{
			IdeApp.Workspace.FileAddedToProject -= fileAddedHandler;
			IdeApp.Workspace.FileRemovedFromProject -= fileRemovedHandler;
			IdeApp.Workspace.FileRenamedInProject -= fileRenamedHandler;
			IdeApp.Workspace.FilePropertyChangedInProject -= filePropertyChangedHandler;
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
			
			string iconName;
			if (p is DotNetProject && ((DotNetProject)p).LanguageBinding == null) {
				iconName = Gtk.Stock.DialogError;
				label = GettextCatalog.GetString ("{0} <span foreground='red' size='small'>(Unknown language '{1}')</span>", p.Name, ((DotNetProject)p).LanguageName);
			} else {
				iconName = Services.Icons.GetImageForProjectType (p.ProjectType);
				if (p.ParentSolution != null && p.ParentSolution.SingleStartup && p.ParentSolution.StartupItem == p)
					label = "<b>" + p.Name + "</b>";
				else
					label = p.Name;
			}
			
			icon = Context.GetIcon (iconName);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project) dataObject;
			if (project is DotNetProject) {
				builder.AddChild (((DotNetProject)project).References);
			}
			
			foreach (ProjectFile file in project.Files) {
				if (file.IsExternalToProject) {
					builder.AddChild (new LinkedFilesFolder (project));
					break;
				}
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
			return it.ParentFolder.IsRoot ? (object) it.ParentSolution : (object) it.ParentFolder;
		}
		
		void OnAddFile (object sender, ProjectFileEventArgs e)
		{
			AddFile (e.ProjectFile, e.Project);
		}
		
		void OnRemoveFile (object sender, ProjectFileEventArgs e)
		{
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
			
			if (file.IsExternalToProject) {
				// Files from outside the project folder are added in a special folder
				if (!tb.MoveToObject (new LinkedFilesFolder (project))) {
					// This will fill the folder, so there is no need to add the file again
					if (tb.MoveToObject (project))
						tb.AddChild (new LinkedFilesFolder (project));
					return;
				}
				tb.AddChild (file);
			}
			else {
				// It's a regular file, add it to the correct folder
				string filePath = Path.GetDirectoryName (file.Name);
				
				object data;
				if (file.Subtype == Subtype.Directory)
					data = new ProjectFolder (file.Name, project);
				else
					data = file;
					
				// Already there?
				if (tb.MoveToObject (data))
					return;
				
				if (filePath != project.BaseDirectory) {
					if (tb.MoveToObject (new ProjectFolder (filePath, project)))
						tb.AddChild (data);
					else {
						// Make sure there is a path to that folder
						tb = FindParentFolderNode (filePath, project);
						if (tb != null)
							tb.UpdateChildren ();
					}
				} else {
					if (tb.MoveToObject (project))
						tb.AddChild (data);
					tb.UpdateChildren ();
				}
			}
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
			
			// We can't use IsExternalToProject here since the ProjectFile has
			// already been removed from the project
			
			if (!file.Name.StartsWith (project.BaseDirectory)) {
				// This ensures that the linked files folder is deleted if there are
				// no more external files
				if (tb.MoveToObject (project))
					tb.UpdateAll ();
				return;
			}
			
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
					string parentPath = Path.GetDirectoryName (file.Name);
					if (!tb.MoveToObject (new ProjectFolder (parentPath, project)))
						return;
				}
			}
			
			while (tb.DataItem is ProjectFolder) {
				ProjectFolder f = (ProjectFolder) tb.DataItem;
				if (!Directory.Exists (f.Path) || project.Files.GetFilesInPath (f.Path).Length == 0)
					tb.Remove (true);
				else
					break;
			}
		}
		
		void OnRenameFile (object sender, ProjectFileRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.ProjectFile);
			if (tb != null) tb.Update ();
		}
		
		void OnProjectModified (object sender, SolutionItemModifiedEventArgs e)
		{
			if (e.Hint == "References" || e.Hint == "Files")
				return;
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) {
				if (e.Hint == "BaseDirectory" || e.Hint == "TargetFramework")
					tb.UpdateAll ();
				else
					tb.Update ();
			}
		}
		
		void OnFilePropertyChanged (object sender, ProjectFileEventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (args.Project);
			if (tb != null) tb.UpdateAll ();
		}
	}
	
	public class ProjectNodeCommandHandler: FolderCommandHandler
	{
		public override string GetFolderPath (object dataObject)
		{
			return ((Project)dataObject).BaseDirectory;
		}
		
		public override void RenameItem (string newName)
		{
			if (!FileService.IsValidFileName(newName)) {
				MessageService.ShowError (GettextCatalog.GetString ("Illegal project name.\nOnly use letters, digits, space, '.' or '_'."));
				return;
			}
			
			Project project = (Project) CurrentNode.DataItem;
			project.Name = newName;
			IdeApp.Workspace.Save();
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
		}
		
		public override void DeleteItem ()
		{
			Project prj = CurrentNode.DataItem as Project;
			bool yes = MessageService.Confirm (GettextCatalog.GetString ("Do you really want to remove project '{0}' from '{1}'?", prj.Name, prj.ParentFolder.Name), AlertButton.Remove);

			if (yes) {
				Solution sol = prj.ParentSolution;
				prj.ParentFolder.Items.Remove (prj);
				prj.Dispose ();
				IdeApp.ProjectOperations.Save (sol);
			}
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
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
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
