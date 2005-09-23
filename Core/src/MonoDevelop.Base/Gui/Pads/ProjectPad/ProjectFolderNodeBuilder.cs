//
// ProjectFolderNodeBuilder.cs
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
	public class ProjectFolderNodeBuilder: FolderNodeBuilder
	{
		Gdk.Pixbuf folderOpenIcon;
		Gdk.Pixbuf folderClosedIcon;
		
		FileEventHandler fileRenamedHandler;
		FileEventHandler fileRemovedHandler;
		
		public override Type NodeDataType {
			get { return typeof(ProjectFolder); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectFolderCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProjectFolder)dataObject).Name;
		}
		
		public override string GetFolderPath (object dataObject)
		{
			return ((ProjectFolder)dataObject).Path;
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/DefaultDirectoryNode"; }
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();

			folderOpenIcon = Context.GetIcon (Stock.OpenFolder);
			folderClosedIcon = Context.GetIcon (Stock.ClosedFolder);
			
			fileRenamedHandler = (FileEventHandler) Runtime.DispatchService.GuiDispatch (new FileEventHandler (OnFolderRenamed));
			fileRemovedHandler = (FileEventHandler) Runtime.DispatchService.GuiDispatch (new FileEventHandler (OnFolderRemoved));
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			ProjectFolder folder = (ProjectFolder) dataObject;
			folder.FolderRenamed += fileRenamedHandler;
			folder.FolderRemoved += fileRemovedHandler;
			folder.TrackChanges = true;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			ProjectFolder folder = (ProjectFolder) dataObject;
			folder.FolderRenamed -= fileRenamedHandler;
			folder.FolderRemoved -= fileRemovedHandler;
			folder.Dispose ();
		}
		
		void OnFolderRenamed (object sender, FileEventArgs e)
		{
			ProjectFolder f = (ProjectFolder) sender;
			ITreeBuilder tb = Context.GetTreeBuilder (f.Parent);
			if (tb != null) tb.UpdateAll ();
		}
		
		void OnFolderRemoved (object sender, FileEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) {
				if (!tb.HasChildren())
					tb.Remove ();
				else
					tb.UpdateAll ();
			}
		}
	
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			base.BuildNode (treeBuilder, dataObject, ref label, ref icon, ref closedIcon);

			ProjectFolder folder = (ProjectFolder) dataObject;
			label = folder.Name;
			icon = folderOpenIcon;
			closedIcon = folderClosedIcon;
		}
		
		public override object GetParentObject (object dataObject)
		{
			ProjectFolder folder = (ProjectFolder) dataObject;
			return folder.Parent;
		}
	}
	
	public class ProjectFolderCommandHandler: FolderCommandHandler
	{
		public override string GetFolderPath (object dataObject)
		{
			return ((ProjectFolder)dataObject).Path;
		}
		
		public override void RenameItem (string newName)
		{
			ProjectFolder folder = (ProjectFolder) CurrentNode.DataItem as ProjectFolder;
			string oldFoldername = folder.Path;
			string newFoldername = Path.Combine (Path.GetDirectoryName(oldFoldername), newName);
			
			if (oldFoldername != newFoldername) {
				try {
					
					if (Runtime.FileUtilityService.IsValidFileName (newFoldername)) {
						Runtime.FileService.RenameFile (oldFoldername, newFoldername);
						Runtime.ProjectService.SaveCombine();
					}
				} catch (System.IO.IOException) {   // assume duplicate file
					Runtime.MessageService.ShowError(GettextCatalog.GetString ("File or directory name is already in use, choose a different one."));
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					Runtime.MessageService.ShowError(GettextCatalog.GetString ("The file name you have chosen contains illegal characters. Please choose a different file name."));
				}
			}
		}
		
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			ProjectFolder folder = (ProjectFolder) CurrentNode.DataItem as ProjectFolder;
			
			bool yes = Runtime.MessageService.AskQuestion (String.Format (GettextCatalog.GetString ("Do you want to remove folder {0}?"), folder.Name));
			if (!yes) return;
			
			Project project = folder.Project;
			ProjectFile[] files = folder.Project.ProjectFiles.GetFilesInPath (folder.Path);
			ProjectFile[] inParentFolder = project.ProjectFiles.GetFilesInPath (Path.GetDirectoryName (folder.Path));
			
			if (inParentFolder.Length == files.Length) {
				// This is the last folder in the parent folder. Make sure we keep
				// a reference to the folder, so it is not deleted from the tree.
				ProjectFile folderFile = new ProjectFile (Path.GetDirectoryName (folder.Path));
				folderFile.Subtype = Subtype.Directory;
				project.ProjectFiles.Add (folderFile);
			}
			
			foreach (ProjectFile file in files)
				folder.Project.ProjectFiles.Remove (file);

//			folder.Remove ();
			Runtime.ProjectService.SaveCombine();
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Move | DragOperation.Copy;
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
