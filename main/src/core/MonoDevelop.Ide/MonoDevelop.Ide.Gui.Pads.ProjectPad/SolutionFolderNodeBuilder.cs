//
// CombineNodeBuilder.cs
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
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Collections;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class SolutionFolderNodeBuilder: TypeNodeBuilder
	{
		SolutionItemRenamedEventHandler combineNameChanged;
		
		public SolutionFolderNodeBuilder ()
		{
			combineNameChanged = (SolutionItemRenamedEventHandler) DispatchService.GuiDispatch (new SolutionItemRenamedEventHandler (OnCombineRenamed));
		}

		public override Type NodeDataType {
			get { return typeof(SolutionFolder); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(CombineNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((SolutionFolder)dataObject).Name;
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SolutionFolder combine = dataObject as SolutionFolder;
			label = combine.Name;
			icon = Context.GetIcon (Stock.SolutionFolderOpen);
			closedIcon = Context.GetIcon (Stock.SolutionFolderClosed);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			foreach (SolutionItem entry in combine.Items)
				ctx.AddChild (entry);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((SolutionFolder) dataObject).Items.Count > 0;
		}
		
		public override object GetParentObject (object dataObject)
		{
			SolutionItem sf = (SolutionItem) dataObject;
			return sf.ParentFolder.IsRoot ? (object) sf.ParentSolution : (object) sf.ParentFolder;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is SolutionFolder)
				return DefaultSort;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			combine.NameChanged += combineNameChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			combine.NameChanged -= combineNameChanged;
		}
		
		void OnCombineRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}
	}
	
	public class CombineNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			if (newName.IndexOfAny (new char [] { '\'', '(', ')', '"', '{', '}', '|' } ) != -1) {
				MessageService.ShowError (GettextCatalog.GetString ("Solution name may not contain any of the following characters: {0}", "', (, ), \", {, }, |"));
				return;
			}
			
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			folder.Name = newName;
			IdeApp.Workspace.Save();
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			SolutionItem it = dataObject as SolutionItem;
			return it != null && operation == DragOperation.Move;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionItem it = (SolutionItem) dataObject;
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to move the item '{0}' to the solution folder '{1}'?", it.Name, folder.Name), AlertButton.Move))
				return;
			
			it.ParentFolder.Items.Remove (it);
			folder.Items.Add (it);
			IdeApp.ProjectOperations.Save (folder.ParentSolution);
		}
			
		public override void ActivateMultipleItems ()
		{
			SolutionFolder folder = CurrentNode.DataItem as SolutionFolder;
			IdeApp.ProjectOperations.ShowOptions (folder);
		}
		
		public override void DeleteItem ()
		{
			SolutionFolder folder = CurrentNode.DataItem as SolutionFolder;
			SolutionFolder parent = folder.ParentFolder;
			if (parent == null) return;
			
			bool yes = MessageService.Confirm (GettextCatalog.GetString ("Do you really want to remove the folder '{0}' from '{1}'?", folder.Name, parent.Name), AlertButton.Remove);
			if (yes) {
				Solution sol = folder.ParentSolution;
				parent.Items.Remove (folder);
				folder.Dispose ();
				IdeApp.ProjectOperations.Save (sol);
			}
		}
		
		[CommandHandler (ProjectCommands.AddNewProject)]
		public void AddNewProjectToCombine()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionItem ce = IdeApp.ProjectOperations.CreateProject (folder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddProject)]
		public void AddProjectToCombine()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionItem ce = IdeApp.ProjectOperations.AddSolutionItem (folder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddSolutionFolder)]
		public void AddFolder()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionFolder ce = new SolutionFolder ();
			ce.Name = GettextCatalog.GetString ("New Folder");
			folder.Items.Add (ce);
			Tree.AddNodeInsertCallback (ce, OnFolderInserted);
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.Reload)]
		[AllowMultiSelection]
		public void OnReload ()
		{
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetLoadProgressMonitor (true)) {
				m.BeginTask (null, CurrentNodes.Length);
				foreach (ITreeNavigator node in CurrentNodes) {
					SolutionFolder folder = (SolutionFolder) node.DataItem;
					folder.ParentFolder.ReloadItem (m, folder);
					m.Step (1);
				}
				m.EndTask ();
			}
		}
		
		[CommandUpdateHandler (ProjectCommands.Reload)]
		public void OnUpdateReload (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				SolutionFolder folder = (SolutionFolder) node.DataItem;
				if (folder.ParentFolder == null || !folder.NeedsReload) {
					info.Visible = false;
					return;
				}
			}
		}
		
		void OnEntryInserted (ITreeNavigator nav)
		{
			nav.Selected = true;
			nav.Expanded = true;
		}
		
		void OnFolderInserted (ITreeNavigator nav)
		{
			nav.Selected = true;
			nav.Expanded = true;
			Tree.StartLabelEdit ();
		}
	}
}
