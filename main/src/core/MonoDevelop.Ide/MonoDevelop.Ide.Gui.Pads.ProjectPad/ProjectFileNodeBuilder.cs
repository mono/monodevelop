//
// ProjectFileNodeBuilder.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Projects.FileNesting;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class ProjectFileNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectFile); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectFileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var file = (ProjectFile) dataObject;
			return file.Link.IsNullOrEmpty ? file.FilePath.FileName : file.Link.FileName;
		}

		public override void GetNodeAttributes (ITreeNavigator parentNode, object dataObject, ref NodeAttributes attributes)
		{
			var file = (ProjectFile) dataObject;

			if ((file.Flags & ProjectItemFlags.Hidden) != 0) {
				attributes |= NodeAttributes.Hidden;
				return;
			}

			attributes |= NodeAttributes.AllowRename;

			if (!file.Visible && !parentNode.Options ["ShowAllFiles"])
				attributes |= NodeAttributes.Hidden;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			ProjectFile file = (ProjectFile) dataObject;

			nodeInfo.Label = GLib.Markup.EscapeText (file.Link.IsNullOrEmpty ? file.FilePath.FileName : file.Link.FileName);
			if (!File.Exists (file.FilePath)) {
				nodeInfo.Label = "<span foreground='" + Styles.ErrorForegroundColor.ToHexString (false) + "'>" + nodeInfo.Label + "</span>";
			}
			
			nodeInfo.Icon = IdeServices.DesktopService.GetIconForFile (file.FilePath, Gtk.IconSize.Menu);
			
			if (file.IsLink && nodeInfo.Icon != null) {
				var overlay = ImageService.GetIcon ("md-link-overlay").WithSize (Xwt.IconSize.Small);
				nodeInfo.OverlayBottomRight = overlay;
			}
		}
		
		public override object GetParentObject (object dataObject)
		{
			var file = (ProjectFile) dataObject;
			var dir = !file.IsLink ? file.FilePath.ParentDirectory : file.Project.BaseDirectory.Combine (file.ProjectVirtualPath).ParentDirectory;

			if (!string.IsNullOrEmpty (file.DependsOn)) {
				ProjectFile groupUnder = file.Project.Files.GetFile (file.FilePath.ParentDirectory.Combine (file.DependsOn));
				if (groupUnder != null)
					return groupUnder;
			} else {
				// File nesting
				var parentFile = FileNestingService.GetParentFile (file);
				if (parentFile != null) {
					return parentFile;
				}
			}
			
			if (dir == file.Project.BaseDirectory)
				return file.Project;

			return new ProjectFolder (dir, file.Project, null);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (!(thisNode.DataItem is ProjectFile))
				return DefaultSort;
			if (!(otherNode.DataItem is ProjectFile))
				return DefaultSort;

			string name1 = thisNode.NodeName;
			string name2 = otherNode.NodeName;

			//Compare filenames without extension
			string path1 = Path.GetFileNameWithoutExtension (name1);
			string path2 = Path.GetFileNameWithoutExtension (name2);
			int cmp = string.Compare (path1, path2, StringComparison.CurrentCultureIgnoreCase);
			if (cmp != 0)
				return cmp;
			//Compare extensions
			string ext1 = Path.GetExtension (name1);
			string ext2 = Path.GetExtension (name2);
			return string.Compare (ext1, ext2, StringComparison.CurrentCultureIgnoreCase);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			ProjectFile file = (ProjectFile) dataObject;
			return file.HasChildren || FileNestingService.HasChildren (file);
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			base.BuildChildNodes (treeBuilder, dataObject);
			ProjectFile file = (ProjectFile) dataObject;
			if (file.HasChildren)
				treeBuilder.AddChildren (file.DependentChildren);
			else {
				var children = FileNestingService.GetChildren (file);
				if ((children?.Count ?? 0) > 0) {
					treeBuilder.AddChildren (children);
				}
			}
		}
	}
	
	class ProjectFileNodeCommandHandler: NodeCommandHandler
	{
		public override void OnRenameStarting (ref int selectionStart, ref int selectionLength)
		{
			string name = CurrentNode.NodeName;
			selectionStart = 0;
			selectionLength = Path.GetFileNameWithoutExtension(name).Length;
		}

		public async override void RenameItem (string newName)
		{
			var file = (ProjectFile) CurrentNode.DataItem;

			string oldFileName = file.FilePath;
			string newFileName = Path.Combine (Path.GetDirectoryName (oldFileName), newName);
			if (oldFileName == newFileName)
				return;

			var dependentFilesToRename = ProjectOperations.GetDependentFilesToRename (file, newName);

			try {
				if (CanRenameFile (file, newName)) {
					if (dependentFilesToRename != null) {
						if (dependentFilesToRename.Any (f => !CanRenameFile (f.File, f.NewName))) {
							return;
						}
					}

					FileService.RenameFile (file.FilePath, newName);

					if (dependentFilesToRename != null) {
						foreach (var dependentFile in dependentFilesToRename) {
							FileService.RenameFile (dependentFile.File.FilePath, dependentFile.NewName);
						}
					}

					if (file.Project != null)
						await IdeApp.ProjectOperations.SaveAsync (file.Project);
				}
			} catch (ArgumentException) { // new file name with wildcard (*, ?) characters in it
				MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
			} catch (IOException ex) {
				MessageService.ShowError (GettextCatalog.GetString ("There was an error renaming the file."), ex);
			}
		}

		static FilePath GetRenamedFilePath (ProjectFile file, string newName)
		{
			if (file.IsLink) {
				var oldLink = file.ProjectVirtualPath;
				var newLink = oldLink.ParentDirectory.Combine (newName);
				return file.Project.BaseDirectory.Combine (newLink);
			}
			return file.FilePath.ParentDirectory.Combine (newName);	
		}

		static bool CanRenameFile (ProjectFile file, string newName)
		{
			ProjectFile newProjectFile = null;
			FilePath newPath = GetRenamedFilePath (file, newName);

			if (file.Project != null)
				newProjectFile = file.Project.Files.GetFileWithVirtualPath (newPath.ToRelative (file.Project.BaseDirectory));

			if (!FileService.IsValidPath (newPath) || ProjectFolderCommandHandler.ContainsDirectorySeparator (newName)) {
				MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				return false;
			} else if ((newProjectFile != null && newProjectFile != file) || FileExistsCaseSensitive (file.FilePath.ParentDirectory, newName)) {
				// If there is already a file under the newPath which is *different*, then throw an exception
				MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
				return false;
			}

			return true;
		}

		static bool FileExistsCaseSensitive (FilePath parentDirectory, string fileName)
		{
			if (!Directory.Exists (parentDirectory))
				return false;

			return Directory.EnumerateFiles (parentDirectory, fileName)
				.Any (file => Path.GetFileName (file) == fileName);
		}
		
		public override void ActivateItem ()
		{
			ProjectFile file = (ProjectFile) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (file.FilePath, file.Project);
		}
		
		public override void ActivateMultipleItems ()
		{
			ProjectFile file;
			for (int i = 0; i < CurrentNodes.Length; i++) {
				// Only bring the last file to the front
				file = (ProjectFile) CurrentNodes [i].DataItem;
				IdeApp.Workbench.OpenDocument (file.FilePath, file.Project, i == CurrentNodes.Length - 1);
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		public override bool CanDeleteItem ()
		{
			return true;
		}
		
		[CommandHandler (EditCommands.Delete)]
		[AllowMultiSelection]
		public override void DeleteMultipleItems ()
		{
			var projects = new Set<SolutionItem> ();
			var files = new List<ProjectFile> ();
			bool hasChildren = false;

			foreach (var node in CurrentNodes) {
				var pf = (ProjectFile) node.DataItem;
				projects.Add (pf.Project);
				if (pf.HasChildren || FileNestingService.HasChildren (pf))
					hasChildren = true;
				files.Add (pf);
			}

			string question;
			bool fileExists = CheckAnyFileExists(files);

			if (CheckAllLinkedFile (files)) {
				RemoveFilesFromProject (false, files);
			} else {
				if (hasChildren) {
					if (files.Count == 1) {
						if (fileExists)
							question = GettextCatalog.GetString ("Are you sure you want to delete the file {0} and " +
															 "its code-behind children from project {1}?",
															 Path.GetFileName (files [0].Name), files [0].Project.Name);
						else
							question = GettextCatalog.GetString ("Are you sure you want to remove the file {0} and " +
															 "its code-behind children from project {1}?",
															 Path.GetFileName (files [0].Name), files [0].Project.Name);
					} else {
						if (fileExists)
							question = GettextCatalog.GetString ("Are you sure you want to delete the selected files and " +
															 "their code-behind children from the project?");
						else
							question = GettextCatalog.GetString ("Are you sure you want to remove the selected files and " +
															 "their code-behind children from the project?");
					}
				} else {
					if (files.Count == 1) {
						if (fileExists)
							question = GettextCatalog.GetString ("Are you sure you want to delete file {0} from project {1}?",
															 Path.GetFileName (files [0].Name), files [0].Project.Name);
						else
							question = GettextCatalog.GetString ("Are you sure you want to remove file {0} from project {1}?",
															 Path.GetFileName (files [0].Name), files [0].Project.Name);

					} else {
						if (fileExists)
							question = GettextCatalog.GetString ("Are you sure you want to delete the selected files from the project?");
						else
							question = GettextCatalog.GetString ("Are you sure you want to remove the selected files from the project?");
					}
				}

				var result = MessageService.AskQuestion (question, new [] { AlertButton.Cancel, fileExists ? AlertButton.Delete : AlertButton.Remove });
				if (result == AlertButton.Cancel)
					return;
				else
					RemoveFilesFromProject (fileExists, files);
			}

			IdeApp.ProjectOperations.SaveAsync (projects);
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		[AllowMultiSelection]
		void OnUpdateDeleteMultipleItems (CommandInfo info)
		{
			var files = new List<ProjectFile> ();
			foreach (var node in CurrentNodes) {
				var pf = (ProjectFile)node.DataItem;
				files.Add (pf);
			}
			if (!CheckAnyFileExists(files))
				info.Text = GettextCatalog.GetString ("Remove");
		}

		[CommandHandler (ProjectCommands.ExcludeFromProject)]
		[AllowMultiSelection]
		void OnExcludeFilesFromProject ()
		{
			var projects = new Set<SolutionItem> ();
			var files = new List<ProjectFile> ();

			foreach (var node in CurrentNodes) {
				var pf = (ProjectFile)node.DataItem;
				projects.Add (pf.Project);
				files.Add (pf);
			}
			RemoveFilesFromProject (false, files);
			IdeApp.ProjectOperations.SaveAsync (projects);
		}

		public void RemoveFilesFromProject (bool delete, List<ProjectFile> files)
		{
			foreach (var file in files) {
				var project = file.Project;
				var inFolder = project.Files.GetFilesInVirtualPath (file.ProjectVirtualPath.ParentDirectory).ToList ();

				if (inFolder.Count == 1 && inFolder [0] == file && project.Files.GetFileWithVirtualPath (file.ProjectVirtualPath.ParentDirectory) == null) {
					// This is the last project file in the folder. Make sure we keep
					// a reference to the folder, so it is not deleted from the tree.
					var folderFile = new ProjectFile (project.BaseDirectory.Combine (file.ProjectVirtualPath.ParentDirectory));
					folderFile.Subtype = Subtype.Directory;
					project.Files.Add (folderFile);
				}
				var children = FileNestingService.GetDependentOrNestedTree (file);
				if (children != null) {
					foreach (var child in children.ToArray ()) {
						// Delete file before removing them from the project to avoid Remove items being added
						// if the project is currently being saved in memory or to disk.
						if (delete)
							FileService.DeleteFile (child.Name);
						project.Files.Remove (child);
					}
				}

				// Delete file before removing them from the project to avoid Remove items being added
				// if the project is currently being saved in memory or to disk.
				if (delete && !file.IsLink && File.Exists (file.Name))
					FileService.DeleteFile (file.Name);
				project.Files.Remove (file);
			}
		}

		[CommandUpdateHandler (ProjectCommands.ExcludeFromProject)]
		void UpdateExcludeFiles (CommandInfo info)
		{
			info.Enabled = CanDeleteMultipleItems ();
			foreach (var node in CurrentNodes) {
				var pf = (ProjectFile)node.DataItem;
				info.Visible = !pf.IsLink;
			}
		}

		static bool CheckAnyFileExists (IEnumerable<ProjectFile> files)
		{
			foreach (ProjectFile file in files) {
				if (!file.IsLink && File.Exists (file.Name))
					return true;

				var children = FileNestingService.GetDependentOrNestedChildren (file);
				if (children != null) {
					foreach (var child in children.ToArray ()) {
						if (File.Exists (child.Name))
							return true;
					}
				}
			}
			return false;
		}

		static bool CheckAllLinkedFile (IEnumerable<ProjectFile> files)
		{
			foreach (ProjectFile file in files) {
				if (!file.IsLink)
					return false;
			}
			return true;
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			//don't allow removing children from parents. The parent can be removed and will remove the whole group.
			info.Enabled = CanDeleteMultipleItems ();
		}
		
		[CommandHandler (ViewCommands.OpenWithList)]
		public void OnOpenWith (object ob)
		{
			ProjectFile finfo = (ProjectFile) CurrentNode.DataItem;
			((FileViewer)ob).OpenFile (finfo.Name);
		}
		
		[CommandUpdateHandler (ViewCommands.OpenWithList)]
		public async Task OnOpenWithUpdate (CommandArrayInfo info, CancellationToken cancellationToken)
		{
			var pf = (ProjectFile) CurrentNode.DataItem;
			await PopulateOpenWithViewers (info, pf.Project, pf.FilePath);
		}
		
		internal static async Task PopulateOpenWithViewers (CommandArrayInfo info, Project project, string filePath)
		{
			var viewers = (await IdeServices.DisplayBindingService.GetFileViewers (filePath, project)).ToList ();
			
			//show the default viewer first
			var def = viewers.FirstOrDefault (v => v.CanUseAsDefault) ?? viewers.FirstOrDefault (v => v.IsExternal);
			if (def != null) {
				CommandInfo ci = info.Add (def.Title, def);
				ci.Description = GettextCatalog.GetString ("Open with '{0}'", def.Title);
				if (viewers.Count > 1)
					info.AddSeparator ();
			}
			
			//then the builtins, followed by externals
			FileViewer prev = null; 
			foreach (FileViewer fv in viewers) {
				if (def != null && fv.Equals (def))
					continue;
				if (prev != null && fv.IsExternal != prev.IsExternal)
					info.AddSeparator ();
				CommandInfo ci = info.Add (fv.Title, fv);
				ci.Description = GettextCatalog.GetString ("Open with '{0}'", fv.Title);
				prev = fv;
			}
		}
		
		[CommandHandler (FileCommands.SetBuildAction)]
		[AllowMultiSelection]
		public void OnSetBuildAction (object ob)
		{
			Set<SolutionItem> projects = new Set<SolutionItem> ();
			string action = (string)ob;
			
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				file.BuildAction = action;
				projects.Add (file.Project);
			}
			IdeApp.ProjectOperations.SaveAsync (projects);
		}
		
		[CommandUpdateHandler (FileCommands.SetBuildAction)]
		public void OnSetBuildActionUpdate (CommandArrayInfo info)
		{
			var toggledActions = new Set<string> ();
			Project proj = null;
			ProjectFile finfo = null;

			foreach (var node in CurrentNodes) {
				finfo = (ProjectFile) node.DataItem;
				
				//disallow multi-slect on more than one project, since available build actions may differ
				if (proj == null && finfo.Project != null) {
					proj = finfo.Project;
				} else if (proj == null || proj != finfo.Project) {
					info.Clear ();
					return;
				}
				toggledActions.Add (finfo.BuildAction);
			}

			if (proj == null)
				return;
			
			foreach (string action in proj.GetBuildActions (finfo.FilePath)) {
				if (action == "--") {
					info.AddSeparator ();
				} else {
					CommandInfo ci = info.Add (action, action);
					ci.Checked = toggledActions.Contains (action);
					if (ci.Checked)
						ci.CheckedInconsistent = toggledActions.Count > 1;
				}
			}
		}
		
		[CommandHandler (FileCommands.ShowProperties)]
		[AllowMultiSelection]
		public void OnShowProperties ()
		{
			IdeApp.Workbench.Pads.PropertyPad.BringToFront (true);
		}
		
		//NOTE: This command is slightly odd, as it operates on a tri-state value, 
		//when only being a dual-state control. However, it's straightforward enough.
		// Enabled == (PreserveNewest | Always)
		// Disabled == None
		// Disabling == !None -> None
		// Enabling == None -> PreserveNewest
		//So there is no way to use.
		[CommandHandler (FileCommands.CopyToOutputDirectory)]
		[AllowMultiSelection]
		public void OnCopyToOutputDirectory ()
		{
			//if all of the selection is already checked, then toggle checks them off
			//else it turns them on. hence we need to find if they're all checked,
			bool allChecked = true;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				if (file.CopyToOutputDirectory == FileCopyMode.None) {
					allChecked = false;
					break;
				}
			}
			
			Set<SolutionItem> projects = new Set<SolutionItem> ();
			
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				projects.Add (file.Project);
				if (allChecked) {
					file.CopyToOutputDirectory = FileCopyMode.None;
				} else {
					file.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
				}
			}
				
			IdeApp.ProjectOperations.SaveAsync (projects);
		}
		
		[CommandUpdateHandler (FileCommands.CopyToOutputDirectory)]
		public void OnCopyToOutputDirectoryUpdate (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				if (file.CopyToOutputDirectory != FileCopyMode.None) {
					info.Checked = true;
				} else if (info.Checked) {
					info.CheckedInconsistent = true;
				}
			}
		}
	}
}
