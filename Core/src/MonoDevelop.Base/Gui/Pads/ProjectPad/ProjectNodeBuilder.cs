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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui.Pads.ProjectPad
{
	public class ProjectNodeBuilder: FolderNodeBuilder
	{
		ProjectFileEventHandler fileAddedHandler;
		ProjectFileEventHandler fileRemovedHandler;
		ProjectFileRenamedEventHandler fileRenamedHandler;
		CombineEntryRenamedEventHandler projectNameChanged;
		Hashtable projectsByPath = new Hashtable ();
		
		public override Type NodeDataType {
			get { return typeof(Project); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectNodeCommandHandler); }
		}
		
		protected override void Initialize ()
		{
			fileAddedHandler = (ProjectFileEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectFileEventHandler (OnAddFile));
			fileRemovedHandler = (ProjectFileEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectFileEventHandler (OnRemoveFile));
			fileRenamedHandler = (ProjectFileRenamedEventHandler) Runtime.DispatchService.GuiDispatch (new ProjectFileRenamedEventHandler (OnRenameFile));
			projectNameChanged = (CombineEntryRenamedEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEntryRenamedEventHandler (OnProjectRenamed));
			
			Runtime.ProjectService.FileAddedToProject += fileAddedHandler;
			Runtime.ProjectService.FileRemovedFromProject += fileRemovedHandler;
			Runtime.ProjectService.FileRenamedInProject += fileRenamedHandler;
		}
		
		public override void Dispose ()
		{
			Runtime.ProjectService.FileAddedToProject -= fileAddedHandler;
			Runtime.ProjectService.FileRemovedFromProject -= fileRemovedHandler;
			Runtime.ProjectService.FileRenamedInProject -= fileRenamedHandler;
		}

		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			Project project = (Project) dataObject;
			project.NameChanged += projectNameChanged;
			projectsByPath.Remove (project.BaseDirectory);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			Project project = (Project) dataObject;
			project.NameChanged -= projectNameChanged;
			projectsByPath [project.BaseDirectory] = project;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Project)dataObject).Name;
		}
		
		public override string GetFolderPath (object dataObject)
		{
			return ((Project)dataObject).BaseDirectory;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/ProjectBrowserNode"; }
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			base.BuildNode (treeBuilder, dataObject, ref label, ref icon, ref closedIcon);

			Project p = dataObject as Project;
			label = p.Name;
			string iconName = Runtime.Gui.Icons.GetImageForProjectType (p.ProjectType);
			icon = Context.GetIcon (iconName);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project) dataObject;
			builder.AddChild (project.ProjectReferences);
			builder.AddChild (new ResourceFolder (project));
			
			base.BuildChildNodes (builder, dataObject);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override object GetParentObject (object dataObject)
		{
			return ((CombineEntry) dataObject).ParentCombine;
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
			if (file.BuildAction != BuildAction.EmbedAsResource)
			{
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
				}
			}
			else {
				if (tb.MoveToObject (new ResourceFolder (project)))
					tb.AddChild (file);
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
			
			if (file.Subtype == Subtype.Directory) {
				if (!tb.MoveToObject (new ProjectFolder (file.Name, project)))
					return;
				tb.MoveToParent ();
				tb.UpdateAll ();
				return;
			} else {
				if (tb.MoveToObject (file)) {
					tb.Remove (true);
					if (file.BuildAction == BuildAction.EmbedAsResource)
						return;
				} else {
					string parentPath = Path.GetDirectoryName (file.Name);
					if (!tb.MoveToObject (new ProjectFolder (parentPath, project)))
						return;
				}
			}
			
			while (tb.DataItem is ProjectFolder) {
				ProjectFolder f = (ProjectFolder) tb.DataItem;
				if (!Directory.Exists (f.Path) || project.ProjectFiles.GetFilesInPath (f.Path).Length == 0)
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
		
		void OnProjectRenamed (object sender, CombineEntryRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.CombineEntry);
			if (tb != null) tb.Update ();
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
			if (newName.IndexOfAny (new char [] { '\'', '(', ')', '"', '{', '}', '|' } ) != -1) {
				Runtime.MessageService.ShowError (String.Format (GettextCatalog.GetString ("Project name may not contain any of the following characters: {0}"), "', (, ), \", {, }, |"));
				return;
			}
			
			Project project = (Project) CurrentNode.DataItem;
			project.Name = newName;
			Runtime.ProjectService.SaveCombine();
		}
		
		public override void ActivateItem ()
		{
		}
		
		[CommandHandler (ProjectCommands.Options)]
		public void OnProjectOptions ()
		{
			Project selectedProject = CurrentNode.DataItem as Project;
			Runtime.ProjectService.ShowOptions (selectedProject);
		}
		
		[CommandHandler (ProjectCommands.Deploy)]
		public void OnProjectDeploy ()
		{
			Project selectedProject = CurrentNode.DataItem as Project;
			Runtime.ProjectService.Deploy (selectedProject);
		}
		
		[CommandHandler (ProjectCommands.SetAsStartupProject)]
		public void SetAsStartupProject ()
		{
			Project project = CurrentNode.DataItem as Project;
			Combine combine = CurrentNode.GetParentDataItem (typeof(Combine), false) as Combine;
			
			combine.StartupEntry = project;
			combine.SingleStartupProject = true;
			Runtime.ProjectService.SaveCombine ();
		}
		
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			Combine cmb = CurrentNode.GetParentDataItem (typeof(Combine), false) as Combine;;
			Project prj = CurrentNode.DataItem as Project;
			
			bool yes = Runtime.MessageService.AskQuestion (String.Format (GettextCatalog.GetString ("Do you really want to remove project {0} from solution {1}"), prj.Name, cmb.Name));
			if (yes) {
				cmb.RemoveEntry (prj);
				Runtime.ProjectService.SaveCombine();
			}
		}
		
		[CommandHandler (ProjectCommands.AddReference)]
		public void AddReferenceToProject ()
		{
			Project p = (Project) CurrentNode.DataItem;
			if (Runtime.ProjectService.AddReferenceToProject (p))
				Runtime.ProjectService.SaveCombine();
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
