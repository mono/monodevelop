// 
// SolutionFolderFileNodeBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class SolutionFolderFileNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof(SolutionFolderFileNode);
			}
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SolutionFolderFileNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((SolutionFolderFileNode)dataObject).Path;
		}

		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			SolutionFolderFileNode file = (SolutionFolderFileNode) dataObject;
			nodeInfo.Label = file.Name;
			if (!System.IO.File.Exists (file.Path))
				nodeInfo.Label = "<span foreground='" + Styles.ErrorForegroundColor.ToHexString (false) + "'>" + nodeInfo.Label + "</span>";
			nodeInfo.Icon = DesktopService.GetIconForFile (file.Path, Gtk.IconSize.Menu);
		}
		
		public override object GetParentObject (object dataObject)
		{
			SolutionFolderFileNode file = (SolutionFolderFileNode) dataObject;
			if (file.Parent.IsRoot)
				return file.Parent.ParentSolution;
			else
				return file.Parent;
		}
	}
	
	class SolutionFolderFileNodeCommandHandler: NodeCommandHandler
	{
		public override void DeleteMultipleItems ()
		{
			var modifiedSolutionsToSave = new HashSet<Solution> ();
			QuestionMessage msg = new QuestionMessage ();
			msg.SecondaryText = GettextCatalog.GetString ("The Delete option permanently removes the file from your hard disk. Click Remove from Solution if you only want to remove it from your current solution.");
			AlertButton removeFromSolution = new AlertButton (GettextCatalog.GetString ("_Remove from Solution"), Gtk.Stock.Remove);
			msg.Buttons.Add (AlertButton.Delete);
			msg.Buttons.Add (AlertButton.Cancel);
			msg.Buttons.Add (removeFromSolution);
			msg.AllowApplyToAll = true;

			foreach (ITreeNavigator nav in CurrentNodes) {
				SolutionFolderFileNode file = (SolutionFolderFileNode) nav.DataItem;
				if (file.Parent.IsRoot)
					msg.Text = GettextCatalog.GetString ("Are you sure you want to remove the file {0} from the solution folder {1}?", file.Name, file.Parent.Name);
				else
					msg.Text = GettextCatalog.GetString ("Are you sure you want to remove the file {0} from the solution {1}?", file.Name, file.Parent.ParentSolution.Name);
				AlertButton result = MessageService.AskQuestion (msg);
				if (result == AlertButton.Cancel)
					return;
				
				file.Parent.Files.Remove (file.Path);
				
				if (result == AlertButton.Delete) {
					FileService.DeleteFile (file.Path);
				}

				if (file.Parent != null && file.Parent.ParentSolution != null) {
					modifiedSolutionsToSave.Add (file.Parent.ParentSolution);
				}
			}
				
			IdeApp.ProjectOperations.SaveAsync (modifiedSolutionsToSave);
		}
		
		public override void ActivateItem ()
		{
			SolutionFolderFileNode file = (SolutionFolderFileNode) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (file.Path, project: null);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}

		[CommandHandler (ViewCommands.OpenWithList)]
		public void OnOpenWith (object ob)
		{
			var finfo = (SolutionFolderFileNode)CurrentNode.DataItem;
			((FileViewer)ob).OpenFile (finfo.Path);
		}

		[CommandUpdateHandler (ViewCommands.OpenWithList)]
		public void OnOpenWithUpdate (CommandArrayInfo info)
		{
			var pf = (SolutionFolderFileNode)CurrentNode.DataItem;
			ProjectFileNodeCommandHandler.PopulateOpenWithViewers (info, null, pf.Path);
		}

		public override void OnRenameStarting (ref string startingText, ref int selectionStart, ref int selectionLength)
		{
			var file = (SolutionFolderFileNode)CurrentNode.DataItem;
			startingText = file.Name;
			selectionStart = 0;
			selectionLength = Path.GetFileNameWithoutExtension (startingText).Length;
		}

		public async override void RenameItem (string newName)
		{
			var file = (SolutionFolderFileNode)CurrentNode.DataItem;
			var oldPath = file.Path;
			if (SystemFileNodeCommandHandler.RenameFileWithConflictCheck (oldPath, newName, out string newPath)) {
				//FIXME: implement this as a rename rather than an add/remove
				file.Parent.Files.Remove (oldPath);
				file.Parent.Files.Add (newPath);
				await IdeApp.ProjectOperations.SaveAsync (file.Parent.ParentSolution);
			}
		}
	}

	class SolutionFolderFileNode: IFileItem
	{
		FilePath path;
		SolutionFolder parent;
		
		public SolutionFolderFileNode (FilePath path, SolutionFolder parent)
		{
			this.Path = path;
			this.parent = parent;
		}

		public FilePath Path {
			get { return path; }
			set { path = value; }
		}

		public string Name {
			get { return path.FileName; }
		}

		//this is named misleadingly, so hide it away
		FilePath IFileItem.FileName {
			get { return path; }
		}

		public SolutionFolder Parent {
			get { return this.parent; }
			set { this.parent = value; }
		}
		
		public override bool Equals (object obj)
		{
			var other = obj as SolutionFolderFileNode;
			if (other == null)
				return false;
			return path == other.path && parent == other.parent;
		}

		public override int GetHashCode ()
		{
			unchecked { 
				return path.GetHashCode () | parent.GetHashCode ();
			}
		}
	}
}

