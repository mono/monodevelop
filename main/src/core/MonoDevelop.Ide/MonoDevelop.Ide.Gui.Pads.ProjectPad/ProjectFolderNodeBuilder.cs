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
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectFolderNodeBuilder: FolderNodeBuilder
	{
		Gdk.Pixbuf folderOpenIcon;
		Gdk.Pixbuf folderClosedIcon;
		
		EventHandler<FileCopyEventArgs> fileRenamedHandler;
		EventHandler<FileEventArgs> fileRemovedHandler;
		
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
		
		protected override void Initialize ()
		{
			base.Initialize ();

			folderOpenIcon = Context.GetIcon (Stock.OpenFolder);
			folderClosedIcon = Context.GetIcon (Stock.ClosedFolder);
			
			fileRenamedHandler = DispatchService.GuiDispatch<EventHandler<FileCopyEventArgs>> (OnFolderRenamed);
			fileRemovedHandler = DispatchService.GuiDispatch<EventHandler<FileEventArgs>> (OnFolderRemoved);
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
		
		void OnFolderRenamed (object sender, FileCopyEventArgs e)
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
			
			//show directory in red if doesn't exist and doesn't have virtual linked children
			if (!Directory.Exists (folder.Path) && !treeBuilder.HasChildren ()) {
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
					if (!FileService.IsValidPath (newFoldername)) {
						MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
					} else if (File.Exists (newFoldername) || Directory.Exists (newFoldername)) {
						MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
					} else {
						FileService.RenameDirectory (oldFoldername, newName);
						if (folder.Project != null)
							IdeApp.ProjectOperations.Save (folder.Project);
					}
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				} catch (System.IO.IOException ex) {
					MessageService.ShowException (ex, GettextCatalog.GetString ("There was an error renaming the directory."));
				}
			}
		}
		
		public override void DeleteMultipleItems ()
		{
			var projects = new HashSet<SolutionEntityItem> ();

			var confirmMsg = new ConfirmationMessage () {
				ConfirmButton = AlertButton.Delete,
				AllowApplyToAll = CurrentNodes.Length > 1,
			};

			foreach (ITreeNavigator node in CurrentNodes) {
				var folder = node.DataItem as ProjectFolder;
				var project = folder.Project;
				
				confirmMsg.Text = GettextCatalog.GetString ("Are you sure you want to permanently delete folder {0}?", folder.Path);
				if (!MessageService.Confirm (confirmMsg))
					continue;

				var folderRelativePath = folder.Path.ToRelative (project.BaseDirectory);
				var files = project.Files.GetFilesInVirtualPath (folder.Path).ToList ();
				var folderPf = project.Files.GetFileWithVirtualPath (folderRelativePath);

				try {
					if (Directory.Exists (folder.Path))
						// Indirect events will remove the files from the project
						FileService.DeleteDirectory (folder.Path);
				} catch (Exception ex) {
					MessageService.ShowError (GettextCatalog.GetString ("The folder {0} could not be deleted from disk: {1}", folder.Path, ex.Message));
				}

				// even if we removed the directory successfully there may still be link files
				// so make sure we remove all the files
				foreach (var f in files)
					project.Files.Remove (f);

				// also remove the folder's own ProjectFile, if it exists 
				if (folderPf != null)
					project.Files.Remove (folderPf);

				// If it's the last item in the parent folder, make sure we keep a reference to the parent 
				// folder, so it is not deleted from the tree.
				var inParentFolder = project.Files.GetFilesInVirtualPath (folderRelativePath.ParentDirectory);
				if (!inParentFolder.Skip (1).Any()) {
					project.Files.Add (new ProjectFile (folder.Path.ParentDirectory) {
						Subtype = Subtype.Directory,
					});
				}

				projects.Add (project);
			}
			IdeApp.ProjectOperations.Save (projects);
		}

		[CommandHandler (ProjectCommands.IncludeToProject)]
		[AllowMultiSelection]
		public void IncludeToProject ()
		{
			Set<SolutionEntityItem> projects = new Set<SolutionEntityItem> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				Project project = node.GetParentDataItem (typeof(Project), true) as Project;
				if (node.HasChildren ()) {
					List<SystemFile> filesToAdd = new List<SystemFile> ();
					ITreeNavigator nav = node.Clone ();
					GetFiles (nav, filesToAdd);
	
					foreach (SystemFile file in filesToAdd)
						project.AddFile (file.Path);

					projects.Add (project);
				} else {
					ProjectFolder pf = node.DataItem as ProjectFolder;
					if (pf != null) {
						project.AddDirectory (FileService.AbsoluteToRelativePath (project.BaseDirectory, pf.Path));
						projects.Add (project);
					}
				}
			}
			IdeApp.ProjectOperations.Save (projects);
		}

		[CommandUpdateHandler (ProjectCommands.IncludeToProject)]
		protected void IncludeToProjectUpdate (CommandInfo item)
		{
			foreach (ITreeNavigator nav in CurrentNodes) {
				Project project = nav.GetParentDataItem (typeof (Project), true) as Project;
				string thisPath = GetFolderPath (nav.DataItem);
				if (project == null || PathExistsInProject (project, thisPath)) {
					item.Visible = false;
					return;
				}
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

		private void GetFiles (ITreeNavigator nav, List<SystemFile> filesToAdd)
		{
			nav.MoveToFirstChild ();
			do {
				if (nav.HasChildren ()) {
					ProjectFolder pf = nav.DataItem as ProjectFolder;
					if (pf != null && (File.GetAttributes (pf.Path) & FileAttributes.Hidden) == 0) {
						ITreeNavigator newNav = nav.Clone ();
						GetFiles (newNav, filesToAdd);
					}
				} else if (nav.DataItem is SystemFile) {
					filesToAdd.Add ((SystemFile) nav.DataItem);
				}
			} while (nav.MoveNext ());
			nav.MoveToParent ();
		}
		
		internal static bool PathExistsInProject (Project project, string path)
		{
			string basePath = path;
			foreach (ProjectFile f in project.Files)
				if (f.Name.StartsWith (basePath)
				    && (f.Name.Length == basePath.Length || f.Name[basePath.Length] == Path.DirectorySeparatorChar))
					return true;
			return false;
		}
		
	}
}
