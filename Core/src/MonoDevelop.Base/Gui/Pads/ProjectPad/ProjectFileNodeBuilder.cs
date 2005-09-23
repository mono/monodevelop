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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Commands;

namespace MonoDevelop.Gui.Pads.ProjectPad
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
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/ProjectFileNode"; }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ProjectFile file = (ProjectFile) dataObject;
			label = Path.GetFileName (file.FilePath);
			icon = Context.GetIcon (Runtime.Gui.Icons.GetImageForFile (file.FilePath));
		}
		
		public override object GetParentObject (object dataObject)
		{
			ProjectFile file = (ProjectFile) dataObject;
			string dir = Path.GetDirectoryName (file.FilePath);
			if (dir == file.Project.BaseDirectory)
				return file.Project;
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
	}
	
	public class ProjectFileNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			ProjectFile file = CurrentNode.DataItem as ProjectFile;
			string oldname = file.Name;

			string newname = Path.Combine (Path.GetDirectoryName (oldname), newName);
			if (oldname != newname) {
				try {
					if (Runtime.FileUtilityService.IsValidFileName (newname)) {
						Runtime.FileService.RenameFile (oldname, newname);
						Runtime.ProjectService.SaveCombine();
					}
				} catch (System.IO.IOException) {   // assume duplicate file
					Runtime.MessageService.ShowError (GettextCatalog.GetString ("File or directory name is already in use, choose a different one."));
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					Runtime.MessageService.ShowError (GettextCatalog.GetString ("The file name you have chosen contains illegal characters. Please choose a different file name."));
				}
			}
		}
		
		public override void ActivateItem ()
		{
			ProjectFile file = CurrentNode.DataItem as ProjectFile;
			Runtime.FileService.OpenFile (file.FilePath);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return dataObject is CombineEntry;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
		}
		
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			ProjectFile file = CurrentNode.DataItem as ProjectFile;
			Project project = CurrentNode.GetParentDataItem (typeof(Project), false) as Project;
			
			bool yes = Runtime.MessageService.AskQuestion (String.Format (GettextCatalog.GetString ("Are you sure you want to remove file {0} from project {1}?"), Path.GetFileName (file.Name), project.Name));
			if (!yes) return;

			ProjectFile[] inFolder = project.ProjectFiles.GetFilesInPath (Path.GetDirectoryName (file.Name));
			if (inFolder.Length == 1 && inFolder [0] == file) {
				// This is the last project file in the folder. Make sure we keep
				// a reference to the folder, so it is not deleted from the tree.
				ProjectFile folderFile = new ProjectFile (Path.GetDirectoryName (file.Name));
				folderFile.Subtype = Subtype.Directory;
				project.ProjectFiles.Add (folderFile);
			}
			project.ProjectFiles.Remove (file);
			Runtime.ProjectService.SaveCombine();
		}
		
		[CommandUpdateHandler (ProjectCommands.IncludeInBuild)]
		public void OnUpdateIncludeInBuild (CommandInfo info)
		{
			ProjectFile file = CurrentNode.DataItem as ProjectFile;
			info.Checked = (file.BuildAction == BuildAction.Compile);
		}
		
		[CommandHandler (ProjectCommands.IncludeInBuild)]
		public void OnIncludeInBuild ()
		{
			ProjectFile finfo = CurrentNode.DataItem as ProjectFile;
			if (finfo.BuildAction == BuildAction.Compile) {
				finfo.BuildAction = BuildAction.Nothing;
			} else {
				finfo.BuildAction = BuildAction.Compile;
			}
			Runtime.ProjectService.SaveCombine();
		}
		
		[CommandUpdateHandler (ProjectCommands.IncludeInDeploy)]
		public void OnUpdateIncludeInDeploy (CommandInfo info)
		{
			Project project = (Project) CurrentNode.GetParentDataItem (typeof(Project), false);
			ProjectFile finfo = CurrentNode.DataItem as ProjectFile;
			info.Checked = !project.DeployInformation.IsFileExcluded (finfo.Name);
		}
		
		[CommandHandler (ProjectCommands.IncludeInDeploy)]
		public void OnIncludeInDeploy ()
		{
			ProjectFile finfo = CurrentNode.DataItem as ProjectFile;
			Project project = (Project) CurrentNode.GetParentDataItem (typeof(Project), false);

			if (project.DeployInformation.IsFileExcluded (finfo.Name)) {
				project.DeployInformation.RemoveExcludedFile (finfo.Name);
			} else {
				project.DeployInformation.AddExcludedFile (finfo.Name);
			}
			Runtime.ProjectService.SaveCombine();
		}
	}
}
