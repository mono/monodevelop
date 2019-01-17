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
	class ProjectFolderNodeBuilder: FolderNodeBuilder
	{
		Xwt.Drawing.Image folderOpenIcon;
		Xwt.Drawing.Image folderClosedIcon;
		
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
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			base.OnNodeAdded (dataObject);
			ProjectFolder folder = (ProjectFolder) dataObject;
			folder.FolderRenamed += OnFolderRenamed;
			folder.FolderRemoved += OnFolderRemoved;
			folder.TrackChanges = true;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			base.OnNodeRemoved (dataObject);
			ProjectFolder folder = (ProjectFolder) dataObject;
			folder.FolderRenamed -= OnFolderRenamed;
			folder.FolderRemoved -= OnFolderRemoved;
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
			var tb = Context.GetTreeBuilder (sender);
			if (tb == null)
				return;
			if (!tb.HasChildren ()) {
				tb.Remove ();
			} else {
				//this may have been removed but HasChildren could still be false, not sure why
				//but fully updating the parent's children works
				tb.MoveToParent ();
				tb.UpdateChildren ();
			}
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			// Before items, but after references and other collections
			return -100;
		}
	
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			base.BuildNode (treeBuilder, dataObject, nodeInfo);

			ProjectFolder folder = (ProjectFolder) dataObject;

			nodeInfo.Label = GLib.Markup.EscapeText (folder.Name);
			nodeInfo.Icon = folderOpenIcon;
			nodeInfo.ClosedIcon = folderClosedIcon;
		}
		
		public override object GetParentObject (object dataObject)
		{
			ProjectFolder folder = (ProjectFolder) dataObject;
			return folder.Parent;
		}
	}
	
	class ProjectFolderCommandHandler: FolderCommandHandler
	{
		public override void ActivateItem ()
		{
			CurrentNode.Expanded = !CurrentNode.Expanded;
		}

		public override string GetFolderPath (object dataObject)
		{
			return ((ProjectFolder)dataObject).Path;
		}
		
		public async override void RenameItem (string newName)
		{
			ProjectFolder folder = (ProjectFolder) CurrentNode.DataItem as ProjectFolder;
			string oldFoldername = folder.Path;
			string newFoldername = Path.Combine (Path.GetDirectoryName(oldFoldername), newName);
			
			if (oldFoldername != newFoldername) {
				try {
					if (!FileService.IsValidPath (newFoldername) || ContainsDirectorySeparator (newName)) {
						MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
						return;
					} 
					if (File.Exists (newFoldername)) {
						MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
						return;
					}
					// Don't use Directory.Exists because we want to check for the exact case in case-insensitive file systems
					var di = Directory.EnumerateDirectories (Path.GetDirectoryName (newFoldername), Path.GetFileName (newFoldername)).FirstOrDefault ();
					if (di != null) {
						MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
						return;
					}

					FileService.RenameDirectory (oldFoldername, newName);
					if (folder.Project != null)
						await IdeApp.ProjectOperations.SaveAsync (folder.Project);

				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				} catch (System.IO.IOException ex) {
					MessageService.ShowError (GettextCatalog.GetString ("There was an error renaming the directory."), ex.Message, ex);
				}
			}
		}

		public override void DeleteMultipleItems ()
		{
			var projects = new Set<SolutionItem> ();
			var folders = new List<ProjectFolder> ();
			foreach (ITreeNavigator node in CurrentNodes)
				folders.Add ((ProjectFolder) node.DataItem);
			
			var removeButton = new AlertButton (GettextCatalog.GetString ("_Remove from Project"), Gtk.Stock.Remove);
			var question = new QuestionMessage () {
				AllowApplyToAll = folders.Count > 1,
				SecondaryText = GettextCatalog.GetString (
				"The Delete option permanently removes the directory and any files it contains from your hard disk. " +
				"Click Remove from Project if you only want to remove it from your current solution.")
			};
			question.Buttons.Add (AlertButton.Delete);
			question.Buttons.Add (removeButton);
			question.Buttons.Add (AlertButton.Cancel);

			var deleteOnlyQuestion = new QuestionMessage () {
				AllowApplyToAll = folders.Count > 1,
				SecondaryText = GettextCatalog.GetString ("The directory and any files it contains will be permanently removed from your hard disk. ")
			};
			deleteOnlyQuestion.Buttons.Add (AlertButton.Delete);
			deleteOnlyQuestion.Buttons.Add (AlertButton.Cancel);
			
			foreach (var folder in folders) {
				var project = folder.Project;

				AlertButton result;

				if (project == null) {
					deleteOnlyQuestion.Text = GettextCatalog.GetString ("Are you sure you want to remove directory {0}?", folder.Name);
					result = MessageService.AskQuestion (deleteOnlyQuestion);
					if (result == AlertButton.Delete) {
						DeleteFolder (folder);
						continue;
					} else
						break;
				}

				var folderRelativePath = folder.Path.ToRelative (project.BaseDirectory);
				var files = project.Files.GetFilesInVirtualPath (folderRelativePath).ToList ();
				var folderPf = project.Files.GetFileWithVirtualPath (folderRelativePath);
				bool isProjectFolder = files.Count == 0 && folderPf == null;

				//if the parent directory has already been removed, there may be nothing to do
				if (isProjectFolder) {
					deleteOnlyQuestion.Text = GettextCatalog.GetString ("Are you sure you want to remove directory {0}?", folder.Name);
					result = MessageService.AskQuestion (deleteOnlyQuestion);
					if (result != AlertButton.Delete) 
						break;
				}
				else {
					question.Text = GettextCatalog.GetString ("Are you sure you want to remove directory {0} from project {1}?",
						folder.Name, project.Name);
					result = MessageService.AskQuestion (question);
					if (result != removeButton && result != AlertButton.Delete) 
						break;
					
					projects.Add (project);
					
					//remove the files and link files in the directory
					foreach (var f in files)
						project.Files.Remove (f);
					
					// also remove the folder's own ProjectFile, if it exists 
					// FIXME: it probably was already in the files list
					if (folderPf != null)
						project.Files.Remove (folderPf);
				}
				
				if (result == AlertButton.Delete) {
					DeleteFolder (folder);
				} else {
					//explictly remove the node from the tree, since it currently only tracks real folder deletions
					folder.Remove ();
				}
				
				if (isProjectFolder && folder.Path.ParentDirectory != project.BaseDirectory) {
					// If it's the last item in the parent folder, make sure we keep a reference to the parent 
					// folder, so it is not deleted from the tree.
					var inParentFolder = project.Files.GetFilesInVirtualPath (folderRelativePath.ParentDirectory);
					if (!inParentFolder.Skip (1).Any ()) {
						project.Files.Add (new ProjectFile (folder.Path.ParentDirectory) {
							Subtype = Subtype.Directory,
						});
					}
				}
			}
			IdeApp.ProjectOperations.SaveAsync (projects);
		}

		static void DeleteFolder (ProjectFolder folder)
		{
			try {
				if (Directory.Exists (folder.Path))
					// FileService events should remove remaining files from the project
					FileService.DeleteDirectory (folder.Path);
			}
			catch (Exception ex) {
				MessageService.ShowError (GettextCatalog.GetString ("The folder {0} could not be deleted from disk: {1}", folder.Path, ex.Message));
			}
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			info.Enabled = CanDeleteMultipleItems ();
			info.Text = GettextCatalog.GetString ("Remove");
		}

		[CommandHandler (ProjectCommands.IncludeToProject)]
		[AllowMultiSelection]
		public void IncludeToProject ()
		{
			Set<SolutionItem> projects = new Set<SolutionItem> ();
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
			IdeApp.ProjectOperations.SaveAsync (projects);
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

		internal static bool ContainsDirectorySeparator (string name)
		{
			return name.Contains (Path.DirectorySeparatorChar) || name.Contains (Path.AltDirectorySeparatorChar);
		}
	}
}
