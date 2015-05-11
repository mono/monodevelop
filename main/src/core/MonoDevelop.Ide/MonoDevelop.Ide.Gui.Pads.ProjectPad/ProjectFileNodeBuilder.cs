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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using MonoDevelop.Components;

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
				nodeInfo.Label = "<span foreground='red'>" + nodeInfo.Label + "</span>";
			}
			
			nodeInfo.Icon = DesktopService.GetIconForFile (file.FilePath, Gtk.IconSize.Menu);
			
			if (file.IsLink && nodeInfo.Icon != null) {
				var overlay = ImageService.GetIcon ("md-link-overlay").WithSize (Xwt.IconSize.Small);
				var cached = Context.GetComposedIcon (nodeInfo.Icon, overlay);
				if (cached != null)
					nodeInfo.Icon = cached;
				else {
					var ib = new Xwt.Drawing.ImageBuilder (nodeInfo.Icon.Width, nodeInfo.Icon.Height);
					ib.Context.DrawImage (nodeInfo.Icon, 0, 0);
					ib.Context.DrawImage (overlay, 0, 0);
					var res = ib.ToVectorImage ();
					ib.Dispose ();
					Context.CacheComposedIcon (nodeInfo.Icon, overlay, res);
					nodeInfo.Icon = res;
				}
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
			}
			
			if (dir == file.Project.BaseDirectory)
				return file.Project;

			return new ProjectFolder (dir, file.Project, null);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectFolder)
				return 1;

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
			return file.HasChildren;
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			base.BuildChildNodes (treeBuilder, dataObject);
			ProjectFile file = (ProjectFile) dataObject;
			if (file.HasChildren)
				foreach (ProjectFile pf in file.DependentChildren)
					treeBuilder.AddChild (pf);
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
			ProjectFile newProjectFile = null;
			var file = (ProjectFile) CurrentNode.DataItem;
			
			FilePath oldPath, newPath, newLink = FilePath.Null;
			if (file.IsLink) {
				var oldLink = file.ProjectVirtualPath;
				newLink = oldLink.ParentDirectory.Combine (newName);
				oldPath = file.Project.BaseDirectory.Combine (oldLink);
				newPath = file.Project.BaseDirectory.Combine (newLink);
			} else {
				oldPath = file.Name;
				newPath = oldPath.ParentDirectory.Combine (newName);	
			}
			
			try {
				if (file.Project != null)
					newProjectFile = file.Project.Files.GetFileWithVirtualPath (newPath.ToRelative (file.Project.BaseDirectory));

				if (!FileService.IsValidPath (newPath)) {
					MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				} else if (newProjectFile != null && newProjectFile != file) {
					// If there is already a file under the newPath which is *different*, then throw an exception
					MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
				} else {
					if (file.IsLink) {
						file.Link = newLink;
					} else {
						// This could throw an exception if we try to replace another file during the rename.
						FileService.RenameFile (oldPath, newName);
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
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			var target = (ProjectFile) CurrentNode.DataItem;
			var pf = dataObject as ProjectFile;

			return pf != null && pf != target && !pf.HasChildren && target.DependsOn == null;
		}

		void Drop (ProjectFile pf, DragOperation operation, HashSet<SolutionItem> projectsToSave)
		{
			var target = (ProjectFile) CurrentNode.DataItem;
			var targetDirectory = target.FilePath.ParentDirectory;

			// file dependencies only work if they are in the same physical folder
			if (pf.FilePath.ParentDirectory != targetDirectory) {
				var targetPath = targetDirectory.Combine (pf.FilePath.FileName);

				// if copying to the same directory, make a copy with a different name
				if (targetPath == pf.FilePath)
					targetPath = ProjectOperations.GetTargetCopyName (targetPath, false);

				if (File.Exists (targetPath))
					if (!MessageService.Confirm (GettextCatalog.GetString ("The file '{0}' already exists. Do you want to overwrite it?", targetPath.FileName), AlertButton.OverwriteFile))
						return;

				// unlink the project file from its current parent
				pf.DependsOn = null;

				projectsToSave.Add (pf.Project);

				bool move = operation == DragOperation.Move;
				var opText = move ? GettextCatalog.GetString ("Moving file...") : GettextCatalog.GetString ("Copying file...");

				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (opText, Stock.StatusWorking, true))
					IdeApp.ProjectOperations.TransferFiles (monitor, pf.Project, pf.FilePath, target.Project, targetPath, move, true);

				pf = target.Project.Files.GetFile (targetPath);
			}

			// the dropped project file now depends on the file it was just dropped onto
			pf.DependsOn = target.FilePath.FileName;
			projectsToSave.Add (pf.Project);
		}

		public override void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation)
		{
			var projectsToSave = new HashSet<SolutionItem> ();

			foreach (var dataObject in dataObjects)
				Drop ((ProjectFile) dataObject, operation, projectsToSave);

			IdeApp.ProjectOperations.SaveAsync (projectsToSave);
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
				if (pf.HasChildren)
					hasChildren = true;
				files.Add (pf);
			}

			var removeFromProject = new AlertButton (GettextCatalog.GetString ("_Remove from Project"), Gtk.Stock.Remove);
			string question;
			string secondaryText = null;

			bool filesExist = CheckAnyFileExists (files);
			if (filesExist) {
				secondaryText = GettextCatalog.GetString ("The Delete option permanently removes the file from your hard disk. " +
					"Click Remove from Project if you only want to remove it from your current solution.");
			}
			
			if (hasChildren) {
				if (files.Count == 1)
					question = GettextCatalog.GetString ("Are you sure you want to remove the file {0} and " + 
					                                     "its code-behind children from project {1}?",
					                                     Path.GetFileName (files[0].Name), files[0].Project.Name);
				else
					question = GettextCatalog.GetString ("Are you sure you want to remove the selected files and " + 
					                                     "their code-behind children from the project?");
			} else {
				if (files.Count == 1)
					question = GettextCatalog.GetString ("Are you sure you want to remove file {0} from project {1}?",
					                                     Path.GetFileName (files[0].Name), files[0].Project.Name);
				else
					question = GettextCatalog.GetString ("Are you sure you want to remove the selected files from the project?");
			}

			var result = MessageService.AskQuestion (question, secondaryText, GetDeleteConfirmationButtons (filesExist, removeFromProject));
			if (result != removeFromProject && result != AlertButton.Delete) 
				return;

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

				if (file.HasChildren) {
					foreach (var child in file.DependentChildren.ToArray()) {
						project.Files.Remove (child);
						if (result == AlertButton.Delete)
							FileService.DeleteFile (child.Name);
					}
				}
			
				project.Files.Remove (file);
				if (result == AlertButton.Delete && !file.IsLink)
					FileService.DeleteFile (file.Name);
			}

			IdeApp.ProjectOperations.SaveAsync (projects);
		}

		static bool CheckAnyFileExists (IEnumerable<ProjectFile> files)
		{
			foreach (ProjectFile file in files) {
				if (!file.IsLink && File.Exists (file.Name))
					return true;

				if (file.HasChildren) {
					foreach (var child in file.DependentChildren.ToArray()) {
						if (File.Exists (child.Name))
							return true;
					}
				}
			}
			return false;
		}

		static AlertButton[] GetDeleteConfirmationButtons (bool includeDelete, AlertButton removeFromProject)
		{
			if (includeDelete)
				return new [] { AlertButton.Delete, AlertButton.Cancel, removeFromProject };
			return new [] { AlertButton.Cancel, removeFromProject };
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			//don't allow removing children from parents. The parent can be removed and will remove the whole group.
			info.Enabled = CanDeleteMultipleItems ();
			info.Text = GettextCatalog.GetString ("Remove");
		}
		
		[CommandHandler (ViewCommands.OpenWithList)]
		public void OnOpenWith (object ob)
		{
			ProjectFile finfo = (ProjectFile) CurrentNode.DataItem;
			((FileViewer)ob).OpenFile (finfo.Name);
		}
		
		[CommandUpdateHandler (ViewCommands.OpenWithList)]
		public void OnOpenWithUpdate (CommandArrayInfo info)
		{
			var pf = (ProjectFile) CurrentNode.DataItem;
			PopulateOpenWithViewers (info, pf.Project, pf.FilePath);
		}
		
		internal static void PopulateOpenWithViewers (CommandArrayInfo info, Project project, string filePath)
		{
			var viewers = DisplayBindingService.GetFileViewers (filePath, project).ToList ();
			
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

			foreach (var node in CurrentNodes) {
				var finfo = (ProjectFile) node.DataItem;
				
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
			
			foreach (string action in proj.GetBuildActions ()) {
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
