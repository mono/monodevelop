//
// FileNodeBuilder.cs
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
using MonoDevelop.Core.Gui.Utils;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Projects.Item;

namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class FileNodeBuilder : TypeNodeBuilder
	{
		public FileNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(FileNode); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/ProjectFileNode"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(FileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			FileNode fileNode = dataObject as FileNode;
			if (fileNode == null) 
				return "FileNode";
			
			return Path.GetFileName (fileNode.FileName);
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
			FileNode fileNode = dataObject as FileNode;
			if (fileNode == null) 
				return;
			label = Path.GetFileName (fileNode.FileName);
			string ic = Services.Icons.GetImageForFile (fileNode.FileName);
			if (ic != Stock.MiscFiles || !File.Exists (fileNode.FileName))
				icon = Context.GetIcon (ic);
			else
				icon = FileIconLoader.GetPixbufForFile (fileNode.FileName, 16);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			// todo
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			FileNode fileNode = dataObject as FileNode;
			if (fileNode == null) 
				return false;
			
			// todo: code behind files
			return false;
		}
		
	}
	
	public class FileNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			FileNode fileNode = CurrentNode.DataItem as FileNode;
			if (fileNode == null) 
				return;
			
			string oldPath = fileNode.FileName;
			string newPath = Path.Combine (Path.GetDirectoryName (oldPath), newName);
			
			if (oldPath != newPath) {
				try {
					if (Runtime.FileService.IsValidFileName (newPath)) {
						Runtime.FileService.RenameFile (oldPath, newName);
						ProjectService.SaveSolution ();
					}
				} catch (System.IO.IOException) {   // assume duplicate file
					Services.MessageService.ShowError (GettextCatalog.GetString ("File or directory name is already in use, choose a different one."));
				} catch (System.ArgumentException) { // new file name with wildcard (*, ?) characters in it
					Services.MessageService.ShowError (GettextCatalog.GetString ("The file name you have chosen contains illegal characters. Please choose a different file name."));
				}
			}
		}
		
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			FileNode fileNode = CurrentNode.DataItem as FileNode;
			if (fileNode == null) 
				return;
			DeleteFileDialog deleteDialog = new DeleteFileDialog (GettextCatalog.GetString ("Are you sure you want to remove file {0} from project {1}?", Path.GetFileName (fileNode.FileName), fileNode.Project.Name));
			try {
				bool dialogResult = deleteDialog.Run ();
				if (!dialogResult) 
					return;
				Runtime.FileService.DeleteFile (fileNode.FileName);
				ProjectService.SaveSolution ();
			} finally {
				deleteDialog.Destroy ();
			}
		}
		
		[CommandHandler (ProjectCommands.ExcludeFromProject)]
		public void ExcludeFromProject ()
		{
			FileNode fileNode = CurrentNode.DataItem as FileNode;
			if (fileNode == null) 
				return;
			foreach (ProjectItem item in fileNode.Project.Project.Items) {
				string fileName = Path.GetFullPath (Path.Combine (fileNode.Project.Project.BasePath, SolutionProject.NormalizePath (item.Include)));
				if (fileName == fileNode.FileName) {
					fileNode.Project.Project.Items.Remove (item);
					ProjectService.SaveProject (fileNode.Project.Project);
					break;
				}
			}
		}
		
		public override void ActivateItem ()
		{
			FileNode fileNode = CurrentNode.DataItem as FileNode;
			if (fileNode != null) 
				IdeApp.Workbench.OpenDocument (fileNode.FileName);
		}
	}
}

