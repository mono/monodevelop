//
// SystemFileNodeBuilder.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class SystemFileNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(SystemFile); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SystemFileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return Path.GetFileName (((SystemFile)dataObject).Name);
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			SystemFile file = (SystemFile) dataObject;
			nodeInfo.Label = GLib.Markup.EscapeText (file.Name);
			
			nodeInfo.Icon = DesktopService.GetIconForFile (file.Path, Gtk.IconSize.Menu);
			
			if (file.ShowTransparent) {
				var gicon = Context.GetComposedIcon (nodeInfo.Icon, "fade");
				if (gicon == null) {
					gicon = nodeInfo.Icon.WithAlpha (0.5);
					Context.CacheComposedIcon (nodeInfo.Icon, "fade", gicon);
				}
				nodeInfo.Icon = gicon;
				nodeInfo.Label = "<span foreground='dimgrey'>" + nodeInfo.Label + "</span>";
			}
		}
	}
	
	class SystemFileNodeCommandHandler: NodeCommandHandler
	{
		public override void OnRenameStarting (ref int selectionStart, ref int selectionLength)
		{
			string name = CurrentNode.NodeName;
			selectionStart = 0;
			selectionLength = Path.GetFileNameWithoutExtension (name).Length;
		}

		public override void RenameItem (string newName)
		{
			var file = (SystemFile)CurrentNode.DataItem;
			if (RenameFileWithConflictCheck (file.Path, newName, out string newPath)) {
				file.Path = newPath;
			}
		}

		public static bool RenameFileWithConflictCheck (FilePath oldPath, string newName, out string newPath)
		{
			newPath = oldPath.ParentDirectory.Combine (newName);
			if (oldPath == newPath) {
				return false;
			}
			try {
				if (!FileService.IsValidPath (newPath) || ProjectFolderCommandHandler.ContainsDirectorySeparator (newName)) {
					MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				} else if (File.Exists (newPath) || Directory.Exists (newPath)) {
					MessageService.ShowWarning (GettextCatalog.GetString ("File or directory name is already in use. Please choose a different one."));
				} else {
					FileService.RenameFile (oldPath, newPath);
					return true;
				}
			} catch (ArgumentException) { // new file name with wildcard (*, ?) characters in it
				MessageService.ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
			} catch (IOException ex) {
				MessageService.ShowError (GettextCatalog.GetString ("There was an error renaming the file."), ex);
			}
			return false;
		}
		
		public override void ActivateItem ()
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			IdeApp.Workbench.OpenDocument (file.Path, project: null);
		}
		
		public override void DeleteMultipleItems ()
		{
			if (CurrentNodes.Length == 1) {
				SystemFile file = (SystemFile)CurrentNodes[0].DataItem;
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to permanently delete the file {0}?", file.Path), AlertButton.Delete))
					return;
			} else {
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to permanently delete all selected files?"), AlertButton.Delete))
					return;
			}
			foreach (SystemFile file in CurrentNodes.Select (n => (SystemFile)n.DataItem)) {
				try {
					FileService.DeleteFile (file.Path);
				} catch {
					MessageService.ShowError (GettextCatalog.GetString ("The file {0} could not be deleted", file.Path));
				}
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
		
		[CommandHandler (ProjectCommands.IncludeToProject)]
		[AllowMultiSelection]
		public void IncludeFileToProject ()
		{
			Set<IWorkspaceFileObject> projects = new Set<IWorkspaceFileObject> ();
			var nodesByProject = CurrentNodes.GroupBy (n => n.GetParentDataItem (typeof(Project), true) as Project);
			
			foreach (var projectGroup in nodesByProject) {
				Project project = projectGroup.Key;
				List<FilePath> newFiles = new List<FilePath> ();
				foreach (ITreeNavigator node in projectGroup) {
					SystemFile file = (SystemFile) node.DataItem;
					if (project != null) {
						newFiles.Add (file.Path);
						projects.Add (project);
					}
					else {
						SolutionFolder folder = node.GetParentDataItem (typeof(SolutionFolder), true) as SolutionFolder;
						if (folder != null) {
							folder.Files.Add (file.Path);
							projects.Add (folder.ParentSolution);
						}
						else {
							Solution sol = node.GetParentDataItem (typeof(Solution), true) as Solution;
							sol.RootFolder.Files.Add (file.Path);
							projects.Add (sol);
						}
					}
				}
				if (newFiles.Count > 0)
					project.AddFiles (newFiles);
			}
			IdeApp.ProjectOperations.SaveAsync (projects);
		}
		
		[CommandUpdateHandler (ProjectCommands.IncludeToProject)]
		public void UpdateIncludeFileToProject (CommandInfo info)
		{
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			if (project != null)
				return;
			if (CurrentNode.GetParentDataItem (typeof(Solution), true) != null) {
				info.Text = GettextCatalog.GetString ("Include to Solution");
				return;
			}
			info.Visible = false;
		}
		
		[CommandHandler (ViewCommands.OpenWithList)]
		public void OnOpenWith (object ob)
		{
			SystemFile file = CurrentNode.DataItem as SystemFile;
			((FileViewer)ob).OpenFile (file.Path);
		}
		
		[CommandUpdateHandler (ViewCommands.OpenWithList)]
		public void OnOpenWithUpdate (CommandArrayInfo info)
		{
			ProjectFileNodeCommandHandler.PopulateOpenWithViewers (info, null, ((SystemFile) CurrentNode.DataItem).Path);
		}
	}
}
