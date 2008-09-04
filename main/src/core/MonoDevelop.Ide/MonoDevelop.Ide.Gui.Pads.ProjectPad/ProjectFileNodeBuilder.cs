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
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ProjectFileNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectFile); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ProjectFileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return Path.GetFileName (((ProjectFile)dataObject).Name);
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ProjectPad/ProjectFile"; }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			ProjectFile file = (ProjectFile) dataObject;
			if (file.DependsOnFile != null) {
				attributes = NodeAttributes.None;
			} else {
				attributes |= NodeAttributes.AllowRename;
			}
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ProjectFile file = (ProjectFile) dataObject;

			label = Path.GetFileName (file.FilePath);
			if (!File.Exists (file.FilePath)) {
				label = "<span foreground='red'>" + label + "</span>";
			}

			string ic = Services.Icons.GetImageForFile (file.FilePath);
			if (ic != Stock.MiscFiles || !File.Exists (file.FilePath))
				icon = Context.GetIcon (ic);
			else
				icon = IdeApp.Services.PlatformService.GetPixbufForFile (file.FilePath, Gtk.IconSize.Menu);
		}
		
		public override object GetParentObject (object dataObject)
		{
			ProjectFile file = (ProjectFile) dataObject;
			string dir = Path.GetDirectoryName (file.FilePath);
			
			if (!string.IsNullOrEmpty (file.DependsOn)) {
				ProjectFile groupUnder = file.Project.Files.GetFile (Path.Combine (dir, file.DependsOn));
				if (groupUnder != null)
					return groupUnder;
			}
			
			if (dir == file.Project.BaseDirectory)
				return file.Project;
			else if (file.IsExternalToProject)
				return new LinkedFilesFolder (file.Project);
			else
			    return new ProjectFolder (dir, file.Project, null);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectFolder)
				return 1;
			else
				return DefaultSort;
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
	
	public class ProjectFileNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			ProjectFile file = (ProjectFile) CurrentNode.DataItem;
			string oldPath = file.Name;
			string newPath = Path.Combine (Path.GetDirectoryName (oldPath), newName);
			
			if (oldPath != newPath) {
				try {
					if (FileService.IsValidFileName (newPath)) {
						FileService.RenameFile (oldPath, newName);
						IdeApp.Workspace.Save();
					}
				} catch (System.IO.IOException) {   // assume duplicate file
					MessageService.ShowError (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					MessageService.ShowError (GettextCatalog.GetString ("The file name you have chosen contains illegal characters. Please choose a different file name."));
				}
			}
		}
		
		public override void ActivateItem ()
		{
			ProjectFile file = (ProjectFile) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (file.FilePath);
		}
		
		public override DragOperation CanDragNode ()
		{
			if (((ProjectFile) CurrentNode.DataItem).DependsOnFile == null)
				return DragOperation.Copy | DragOperation.Move;
			else
				return DragOperation.None;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return (dataObject is SolutionItem && ((ProjectFile) CurrentNode.DataItem).DependsOnFile == null);
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
		}
		
		[CommandHandler (FileCommands.OpenContainingFolder)]
		[AllowMultiSelection]
		public void OnOpenFolder ()
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				string path = System.IO.Path.GetDirectoryName (file.FilePath);
				System.Diagnostics.Process.Start ("file://" + path);
			}
		}
		
		public override bool CanDeleteItem ()
		{
			return ((ProjectFile) CurrentNode.DataItem).DependsOnFile == null;
		}
		
		[CommandHandler (EditCommands.Delete)]
		[AllowMultiSelection]
		public override void DeleteItem ()
		{
			bool hasChildren = false;
			List<ProjectFile> files = new List<ProjectFile> ();
			Set<SolutionEntityItem> projects = new Set<SolutionEntityItem> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile pf = (ProjectFile) node.DataItem;
				projects.Add (pf.Project);
				if (pf.HasChildren)
					hasChildren = true;
				files.Add (pf);
			}
			
			AlertButton removeFromProject = new AlertButton (GettextCatalog.GetString ("_Remove from Project"), Gtk.Stock.Remove);
			
			string question, secondaryText;
			if (hasChildren) {
				if (files.Count == 1)
					question = GettextCatalog.GetString ("Are you sure you want to remove the file {0} and " + 
					                                     "its code-behind children from project {1}?",
					                                     Path.GetFileName (files[0].Name), files[0].Project.Name);
				else
					question = GettextCatalog.GetString ("Are you sure you want to remove the selected files and " + 
					                                     "their code-behind children from the project?");
				secondaryText = GettextCatalog.GetString ("Delete physically removes the files from disc.");
			} else {
				if (files.Count == 1)
					question = GettextCatalog.GetString ("Are you sure you want to remove file {0} from project {1}?",
					                                     Path.GetFileName (files[0].Name), files[0].Project.Name);
				else
					question = GettextCatalog.GetString ("Are you sure you want to remove the selected files from the project?");
				secondaryText = GettextCatalog.GetString ("Delete physically removes the file from disc.");
			}
			
			AlertButton result = MessageService.AskQuestion (question, secondaryText,
			                                                 AlertButton.Delete, AlertButton.Cancel, removeFromProject);
			if (result != removeFromProject && result != AlertButton.Delete) 
				return;
			   
			foreach (ProjectFile file in files) {
				Project project = file.Project;
				if (!file.IsExternalToProject) {
					ProjectFile[] inFolder = project.Files.GetFilesInPath (Path.GetDirectoryName (file.Name));
					if (inFolder.Length == 1 && inFolder [0] == file) {
						// This is the last project file in the folder. Make sure we keep
						// a reference to the folder, so it is not deleted from the tree.
						ProjectFile folderFile = new ProjectFile (Path.GetDirectoryName (file.Name));
						folderFile.Subtype = Subtype.Directory;
						project.Files.Add (folderFile);
					}
				}
				
				if (file.HasChildren) {
					foreach (ProjectFile f in file.DependentChildren) {
						project.Files.Remove (f);
						if (result == AlertButton.Delete)
							FileService.DeleteFile (f.Name);
					}
				}
			
				project.Files.Remove (file);
				if (result == AlertButton.Delete)
					FileService.DeleteFile (file.Name);
			}

			IdeApp.ProjectOperations.Save (projects);
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		public void UpdateRemoveItem (CommandInfo info)
		{
			//don't allow removing children from parents. The parent can be removed and will remove the whole group.
			info.Enabled = CanDeleteMultipleItems ();
			info.Text = GettextCatalog.GetString ("Remove");
		}
		
		void UpdateBuildActionToggle (CommandInfo info, BuildAction action)
		{
			int val = -1;
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile file = (ProjectFile) node.DataItem;
				if (val == -1)
					val = (int) file.BuildAction;
				else if ((int) file.BuildAction != val) {
					val = -2;
					break;
				}
			}
			info.Checked = ((BuildAction) val) == action;
			info.CheckedInconsistent = val == -2;
		}

		void BuildActionToggle (BuildAction action)
		{
			ProjectFile firstFile = null;
			Set<SolutionEntityItem> projects = new Set<SolutionEntityItem> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				ProjectFile finfo = (ProjectFile) node.DataItem;
				if (firstFile == null) {
					finfo.BuildAction = (finfo.BuildAction == action) ? BuildAction.Nothing : action;
					firstFile = finfo;
				} else
					finfo.BuildAction = firstFile.BuildAction;
				projects.Add (finfo.Project);
			}
			IdeApp.ProjectOperations.Save (projects);
		}
		
		[CommandUpdateHandler (ProjectCommands.IncludeInBuild)]
		public void OnUpdateIncludeInBuild (CommandInfo info)
		{
			UpdateBuildActionToggle (info, BuildAction.Compile);
		}
		
		[CommandHandler (ProjectCommands.IncludeInBuild)]
		[AllowMultiSelection]
		public void OnIncludeInBuild ()
		{
			BuildActionToggle (BuildAction.Compile);
		}
		
		[CommandUpdateHandler (ProjectCommands.IncludeInDeploy)]
		public void OnUpdateIncludeInDeploy (CommandInfo info)
		{
			UpdateBuildActionToggle (info, BuildAction.FileCopy);
		}
		
		[CommandHandler (ProjectCommands.IncludeInDeploy)]
		[AllowMultiSelection]
		public void OnIncludeInDeploy ()
		{
			BuildActionToggle (BuildAction.FileCopy);
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
			ProjectFile finfo = (ProjectFile) CurrentNode.DataItem;
			FileViewer prev = null; 
			foreach (FileViewer fv in IdeApp.Workbench.GetFileViewers (finfo.Name)) {
				if (prev != null && fv.IsExternal != prev.IsExternal)
					info.AddSeparator ();
				CommandInfo ci = info.Add (fv.Title, fv);
				ci.Description = GettextCatalog.GetString ("Open with '{0}'", fv.Title);
				prev = fv;
			}
		}
	}
}
