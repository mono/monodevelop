//
// FolderNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.IO;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class FolderNodeBuilder : TypeNodeBuilder
	{
		public FolderNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(FolderNode); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(FolderNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/DefaultDirectoryNode"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			FolderNode folderNode = dataObject as FolderNode;
			if (folderNode == null) 
				return "FolderNode";
			
			return Path.GetFileName (folderNode.Path);
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
/*		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/CombineBrowserNode"; }
		}*/
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			FolderNode folderNode = dataObject as FolderNode;
			if (folderNode == null) 
				return;
			label = Path.GetFileName (folderNode.Path);
			
			icon       = Context.GetIcon (Stock.OpenFolder);
			closedIcon = Context.GetIcon (Stock.ClosedFolder);
		}
		
		public static bool IsFileInProject (IProject project, string fileName)
		{
			foreach (ProjectItem item in project.Items) {
				string fullName = Path.GetFullPath (Path.Combine (project.BasePath, SolutionProject.NormalizePath (item.Include)));
				if (fullName == fileName) 
					return true;
			}
			return false;
		}
		
		public static bool IsDirectoryInProject (IProject project, string directoryName)
		{
			foreach (ProjectItem item in project.Items) {
				string fullName = Path.GetFullPath (Path.Combine (project.BasePath, SolutionProject.NormalizePath (item.Include)));
				if (fullName.StartsWith(directoryName + Path.DirectorySeparatorChar)) 
					return true;
				if (item is ProjectFile && ((ProjectFile)item).FileType == FileType.Folder && fullName == directoryName)
					return true;
			}
			return false;
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			FolderNode folderNode = dataObject as FolderNode;
			if (folderNode == null) 
				return;
			
			string basePath = folderNode.Path;
			
			foreach (string fileName in Directory.GetFiles(basePath)) {
				bool isInProject = IsFileInProject(folderNode.Project.Project, fileName);
				
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) { 
					if (isInProject)
						ctx.AddChild (new FileNode (folderNode.Project, fileName));
					else
						ctx.AddChild (new SystemFileNode (folderNode.Project, fileName));
				}
			}
			
			foreach (string directoryName in Directory.GetDirectories(basePath)) {
				bool isInProject = IsDirectoryInProject(folderNode.Project.Project, directoryName);
				if (ProjectSolutionPad.Instance.ShowAllFiles || isInProject) {
					if (isInProject)
						ctx.AddChild (new FolderNode (folderNode.Project, directoryName));
					else
						ctx.AddChild (new SystemFolderNode (folderNode.Project, directoryName));
				}
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			FolderNode folderNode = dataObject as FolderNode;
			if (folderNode == null) 
				return false;
			return Directory.GetFiles(folderNode.Path).Length > 0 || Directory.GetDirectories(folderNode.Path).Length > 0;
		}
	}
	
	public class FolderNodeCommandHandler: NodeCommandHandler
	{
		protected virtual string GetPath(object dataItem)
		{
			FolderNode folderNode = dataItem as FolderNode;
			if (folderNode == null) 
				return null;
			return folderNode.Path;
		}
		
		protected virtual SolutionProject GetProject(object dataItem)
		{
			FolderNode folderNode = dataItem as FolderNode;
			if (folderNode == null) 
				return null;
			return folderNode.Project;
		}
		
		[CommandHandler (ProjectCommands.AddNewFiles)]
		public void AddNewFileToProject()
		{
			string   path    = GetPath(CurrentNode.DataItem);
			SolutionProject project = GetProject(CurrentNode.DataItem);
			if (path == null || project == null)
				return;
			
			using (AddNewFilesToProjectDialog dialog = new AddNewFilesToProjectDialog ("C#", path)) {
				dialog.Run ();
				if (dialog.CreatedFiles != null) 
					foreach (string fileName in dialog.CreatedFiles) 
						project.Project.Add (new ProjectFile (fileName, SystemFileNodeCommandHandler.GetFileType (fileName, project)));
			}
			ProjectService.SaveProject (project.Project);
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.NewFolder)]
		public void AddNewFolder ()
		{
			string   path    = GetPath(CurrentNode.DataItem);
			SolutionProject project = GetProject(CurrentNode.DataItem);
			if (path == null || project == null)
				return;
			
			string baseFolderPath = path;
			string directoryName = Path.Combine (baseFolderPath, GettextCatalog.GetString("New Folder"));
			int index = -1;

			if (Directory.Exists(directoryName)) {
				while (Directory.Exists(directoryName + (++index + 1))) 
					;
			}
			
			if (index >= 0) {
				directoryName += index + 1;
			}
			
			Directory.CreateDirectory (directoryName);
			project.Project.Add (new ProjectFile (directoryName, FileType.Folder));

			ProjectService.SaveProject (project.Project);
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (SearchCommands.FindInFiles)]
		public void OnFindInFiles ()
		{
			string path = GetPath(CurrentNode.DataItem);
			if (path == null)
				return;
			SearchReplaceInFilesManager.SearchOptions.SearchDirectory = path;
			SearchReplaceInFilesManager.ShowFindDialog ();
		}
		
	}
}

