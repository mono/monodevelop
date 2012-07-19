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
			return ((SolutionFolderFileNode)dataObject).FileName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SolutionFolderFileNode file = (SolutionFolderFileNode) dataObject;
			label = file.FileName.FileName;
			if (!System.IO.File.Exists (file.FileName))
				label = "<span foreground='red'>" + label + "</span>";
			icon = DesktopService.GetPixbufForFile (file.FileName, Gtk.IconSize.Menu);
		}
		
		public override object GetParentObject (object dataObject)
		{
			SolutionFolderFileNode file = (SolutionFolderFileNode) dataObject;
			if (file.Parent.IsRoot)
				return file.Parent.ParentSolution;
			else
				return file.Parent;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is SolutionFolderFileNode)
				return DefaultSort;
			else
				return -1;
		}
	}
	
	class SolutionFolderFileNodeCommandHandler: NodeCommandHandler
	{
		public override void DeleteMultipleItems ()
		{
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
					msg.Text = GettextCatalog.GetString ("Are you sure you want to remove the file {0} from the solution folder {1}?", file.FileName.FileName, file.Parent.Name);
				else
					msg.Text = GettextCatalog.GetString ("Are you sure you want to remove the file {0} from the solution {1}?", file.FileName.FileName, file.Parent.ParentSolution.Name);
				AlertButton result = MessageService.AskQuestion (msg);
				if (result == AlertButton.Cancel)
					return;
				
				file.Parent.Files.Remove (file.FileName);
				
				if (result == AlertButton.Delete) {
					FileService.DeleteFile (file.FileName);
				}
			}
		}
		
		public override void ActivateItem ()
		{
			SolutionFolderFileNode file = (SolutionFolderFileNode) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (file.FileName);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Copy | DragOperation.Move;
		}
	}
	
	class SolutionFolderFileNode: IFileItem
	{
		FilePath file;
		SolutionFolder parent;
		
		public SolutionFolderFileNode (FilePath file, SolutionFolder parent)
		{
			this.file = file;
			this.parent = parent;
		}
		
		public FilePath FileName {
			get { return this.file; }
			set { this.file = value; }
		}

		public SolutionFolder Parent {
			get { return this.parent; }
			set { this.parent = value; }
		}
		
		public override bool Equals (object obj)
		{
			SolutionFolderFileNode other = obj as SolutionFolderFileNode;
			if (other == null)
				return false;
			return file == other.file && parent == other.parent;
		}

		public override int GetHashCode ()
		{
			unchecked { 
				return file.GetHashCode () + parent.GetHashCode ();
			}
		}
	}
}

