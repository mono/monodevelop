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
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
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
			get { return "/MonoDevelop/Ide/ContextMenu/ProjectPad/Folder"; }
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();

			folderOpenIcon = Context.GetIcon (Stock.OpenFolder);
			folderClosedIcon = Context.GetIcon (Stock.ClosedFolder);
			
			fileRenamedHandler = (FileEventHandler) DispatchService.GuiDispatch (new FileEventHandler (OnFolderRenamed));
			fileRemovedHandler = (FileEventHandler) DispatchService.GuiDispatch (new FileEventHandler (OnFolderRemoved));
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
			if (!Directory.Exists (folder.Path)) {
				label = "<span foreground='red'>" + label + "</span>";
			}

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
					
					if (Runtime.FileService.IsValidFileName (newFoldername)) {
						Runtime.FileService.RenameDirectory (oldFoldername, newName);
						IdeApp.ProjectOperations.SaveCombine();
					}
				} catch (System.IO.IOException) {   // assume duplicate file
					Services.MessageService.ShowError(GettextCatalog.GetString ("File or directory name is already in use, choose a different one."));
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					Services.MessageService.ShowError(GettextCatalog.GetString ("The file name you have chosen contains illegal characters. Please choose a different file name."));
				}
			}
		}
		
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			ProjectFolder folder = (ProjectFolder) CurrentNode.DataItem as ProjectFolder;
			Project project = folder.Project;
			ProjectFile[] files = folder.Project.ProjectFiles.GetFilesInPath (folder.Path);
			
			if (files.Length == 0) {
				bool yes = Services.MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to permanently delete the folder {0}?", folder.Path));
				if (!yes) return;

				try {
					Runtime.FileService.DeleteDirectory (folder.Path);
				} catch {
					Services.MessageService.ShowError (GettextCatalog.GetString ("The folder {0} could not be deleted", folder.Path));
				}
			}
			else {
				bool yes = Services.MessageService.AskQuestion (GettextCatalog.GetString ("Do you want to remove folder {0}?", folder.Name));
				if (!yes) return;
				
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

				IdeApp.ProjectOperations.SaveCombine();
			}
		}

		[CommandHandler (ProjectCommands.IncludeToProject)]
		public void IncludeToProject ()
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			if (CurrentNode.HasChildren ()) {
				List<SystemFile> filesToAdd = new List<SystemFile> ();
				ITreeNavigator nav = CurrentNode.Clone ();
				GetFiles (nav, filesToAdd);

				foreach (SystemFile file in filesToAdd) {
					if (project.IsCompileable (file.Path))
						project.AddFile (file.Path, BuildAction.Compile);
					else
						project.AddFile (file.Path, BuildAction.Nothing);
				}

				IdeApp.ProjectOperations.SaveProject (project);
			}
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

		[CommandUpdateHandler (ProjectCommands.IncludeToProject)]
		protected void IncludeToProjectUpdate (CommandInfo item)
		{
			Project project = CurrentNode.GetParentDataItem (typeof (Project), true) as Project;
			string thisPath = GetFolderPath (CurrentNode.DataItem);
			item.Visible = !HasProjectFiles (project, thisPath);
		}

		private void GetFiles (ITreeNavigator nav, List<SystemFile> filesToAdd)
		{
			nav.MoveToFirstChild ();
			do {
				if (nav.HasChildren ()) {
					ITreeNavigator newNav = nav.Clone ();
					GetFiles (newNav, filesToAdd);
				} else if (nav.DataItem is SystemFile) {
					filesToAdd.Add ((SystemFile) nav.DataItem);
				}
			} while (nav.MoveNext ());
			nav.MoveToParent ();
		}
		
		bool HasProjectFiles (Project project, string path)
		{
			string basePath = path + Path.DirectorySeparatorChar;
			foreach (ProjectFile f in project.ProjectFiles)
				if (f.Name.StartsWith (basePath))
					return true;
			return false;
		}
		
	}
}
