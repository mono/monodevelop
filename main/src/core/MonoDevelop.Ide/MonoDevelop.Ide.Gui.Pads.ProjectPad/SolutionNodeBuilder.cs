// SolutionNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections;
using System.Collections.Generic;

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
	public class SolutionNodeBuilder: TypeNodeBuilder
	{
		SolutionItemEventHandler combineEntryAdded;
		SolutionItemEventHandler combineEntryRemoved;
		EventHandler<WorkspaceItemRenamedEventArgs> combineNameChanged;
		EventHandler startupChanged;
		
		public SolutionNodeBuilder ()
		{
			combineEntryAdded = (SolutionItemEventHandler) DispatchService.GuiDispatch (new SolutionItemEventHandler (OnEntryAdded));
			combineEntryRemoved = (SolutionItemEventHandler) DispatchService.GuiDispatch (new SolutionItemEventHandler (OnEntryRemoved));
			combineNameChanged = (EventHandler<WorkspaceItemRenamedEventArgs>) DispatchService.GuiDispatch (new EventHandler<WorkspaceItemRenamedEventArgs> (OnCombineRenamed));
			startupChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnStartupChanged));
			
			IdeApp.Workspace.ItemAddedToSolution += combineEntryAdded;
			IdeApp.Workspace.ItemRemovedFromSolution += combineEntryRemoved;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			IdeApp.Workspace.ItemAddedToSolution -= combineEntryAdded;
			IdeApp.Workspace.ItemRemovedFromSolution -= combineEntryRemoved;
		}


		public override Type NodeDataType {
			get { return typeof(Solution); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SolutionNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Solution)dataObject).Name;
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Solution solution = dataObject as Solution;
			int count = 0;
			foreach (SolutionItem e in solution.GetAllSolutionItems ())
				if (!(e is SolutionFolder))
					count++;
			
			switch (count) {
				case 0:
					label = GettextCatalog.GetString ("Solution {0}", solution.Name);
					break;
				case 1:
					label = GettextCatalog.GetString ("Solution {0} (1 entry)", solution.Name);
					break;
				default:
					label = GettextCatalog.GetString ("Solution {0} ({1} entries)", solution.Name, count);
					break;
			}

			icon = Context.GetIcon (Stock.Solution);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			Solution solution = (Solution) dataObject;
			foreach (SolutionItem entry in solution.RootFolder.Items)
				ctx.AddChild (entry);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((Solution) dataObject).RootFolder.Items.Count > 0;
		}
		
		public override object GetParentObject (object dataObject)
		{
			return ((Solution) dataObject).ParentWorkspace;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			Solution solution = (Solution) dataObject;
			solution.NameChanged += combineNameChanged;
			solution.StartupItemChanged += startupChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Solution solution = (Solution) dataObject;
			solution.NameChanged -= combineNameChanged;
			solution.StartupItemChanged -= startupChanged;
		}
		
		void OnStartupChanged (object sender, EventArgs args)
		{
			foreach (SolutionEntityItem it in IdeApp.Workspace.GetAllSolutionItems<SolutionEntityItem> ()) {
				ITreeBuilder tb = Context.GetTreeBuilder (it);
				if (tb != null)
					tb.Update ();
			}
		}
		
		void OnEntryAdded (object sender, SolutionItemEventArgs e)
		{
			ITreeBuilder tb;
			if (e.SolutionItem.ParentFolder.IsRoot)
				tb = Context.GetTreeBuilder (e.SolutionItem.ParentSolution);
			else
				tb = Context.GetTreeBuilder (e.SolutionItem.ParentFolder);
			
			if (tb != null) {
				tb.Update ();	// Update the entry count
				tb.AddChild (e.SolutionItem, true);
				tb.Expanded = true;
			}
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) {
				ITreeBuilder tbs = Context.GetTreeBuilder (e.SolutionItem);
				if (tbs.MoveToParent ())
					tbs.Update (); // Update the entry count
				tb.Remove ();
			}
		}
		
		void OnCombineRenamed (object sender, WorkspaceItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Item);
			if (tb != null) tb.Update ();
		}
	}
	
	public class SolutionNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			if (newName.IndexOfAny (new char [] { '\'', '(', ')', '"', '{', '}', '|' } ) != -1) {
				MessageService.ShowError (GettextCatalog.GetString ("Solution name may not contain any of the following characters: {0}", "', (, ), \", {, }, |"));
				return;
			}
			
			Solution sol = (Solution) CurrentNode.DataItem;
			sol.Name = newName;
			IdeApp.Workspace.Save();
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return dataObject is SolutionItem;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
		}
			
		public override void ActivateMultipleItems ()
		{
			Solution sol = CurrentNode.DataItem as Solution;
			IdeApp.ProjectOperations.ShowOptions (sol);
		}
		
		public override void DeleteMultipleItems ()
		{
			Set<IWorkspaceFileObject> items = new Set<IWorkspaceFileObject> ();
			foreach (ITreeNavigator node in CurrentNodes) {
				Solution solution = node.DataItem as Solution;
				Workspace parent = node.GetParentDataItem (typeof(Workspace), false) as Workspace;
				if (parent == null) return;
				
				if (MessageService.Confirm (GettextCatalog.GetString ("Do you really want to remove solution {0} from workspace {1}?", solution.Name, parent.Name), AlertButton.Remove)) {
					parent.Items.Remove (solution);
					solution.Dispose ();
					items.Add (parent);
				}
			}
			IdeApp.ProjectOperations.Save (items);
		}
		
		public override bool CanDeleteItem ()
		{
			Workspace parent = CurrentNode.GetParentDataItem (typeof(Workspace), false) as Workspace;
			return parent != null;
		}
		
		[CommandHandler (ProjectCommands.AddNewProject)]
		public void AddNewProjectToSolution ()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			SolutionItem ce = IdeApp.ProjectOperations.CreateProject (solution.RootFolder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddProject)]
		public void AddProjectToCombine()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			SolutionItem ce = IdeApp.ProjectOperations.AddSolutionItem (solution.RootFolder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddSolutionFolder)]
		public void AddFolder()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			SolutionItem ce = new SolutionFolder ();
			ce.Name = GettextCatalog.GetString ("New Folder");
			solution.RootFolder.Items.Add (ce);
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
					Solution solution = (Solution) node.DataItem;
					solution.ParentWorkspace.ReloadItem (m, solution);
					m.Step (1);
				}
				m.EndTask ();
			}
		}
		
		[CommandUpdateHandler (ProjectCommands.Reload)]
		public void OnUpdateReload (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Solution solution = (Solution) node.DataItem;
				if (solution.ParentWorkspace == null || !solution.NeedsReload) {
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
		
		[CommandHandler (FileCommands.CloseWorkspaceItem)]
		[AllowMultiSelection]
		public void OnCloseItem ()
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Solution solution = (Solution) node.DataItem;
				IdeApp.Workspace.CloseWorkspaceItem (solution);
			}
		}
		
		[CommandUpdateHandler (FileCommands.CloseWorkspaceItem)]
		public void OnUpdateCloseItem (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Solution solution = (Solution) node.DataItem;
				if (solution.ParentWorkspace != null) {
					info.Visible = false;
					return;
				}
			}
		}
	}
}
