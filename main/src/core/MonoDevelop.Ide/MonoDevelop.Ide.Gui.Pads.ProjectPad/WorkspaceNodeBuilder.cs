// WorkspaceNodeBuilder.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class WorkspaceNodeBuilder: TypeNodeBuilder
	{
		EventHandler<WorkspaceItemChangeEventArgs> combineEntryAdded;
		EventHandler<WorkspaceItemChangeEventArgs> combineEntryRemoved;
		EventHandler<WorkspaceItemRenamedEventArgs> combineNameChanged;
		
		public WorkspaceNodeBuilder ()
		{
			combineEntryAdded = OnEntryAdded;
			combineEntryRemoved = OnEntryRemoved;
			combineNameChanged = OnCombineRenamed;
			combineEntryAdded = DispatchService.GuiDispatch (combineEntryAdded);
			combineEntryRemoved = DispatchService.GuiDispatch (combineEntryRemoved);
			combineNameChanged = DispatchService.GuiDispatch (combineNameChanged);
		}

		public override Type NodeDataType {
			get { return typeof(Workspace); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(WorkspaceNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Workspace)dataObject).Name;
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Workspace workspace = dataObject as Workspace;
			label = workspace.Name;
			icon = Context.GetIcon (Stock.Workspace);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			Workspace ws = (Workspace) dataObject;
			foreach (WorkspaceItem entry in ws.Items)
				ctx.AddChild (entry);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((Workspace) dataObject).Items.Count > 0;
		}
		
		public override object GetParentObject (object dataObject)
		{
			return ((Workspace) dataObject).ParentWorkspace;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			Workspace ws = (Workspace) dataObject;
			ws.ItemAdded += combineEntryAdded;
			ws.ItemRemoved += combineEntryRemoved;
			ws.NameChanged += combineNameChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Workspace ws = (Workspace) dataObject;
			ws.ItemAdded -= combineEntryAdded;
			ws.ItemRemoved -= combineEntryRemoved;
			ws.NameChanged -= combineNameChanged;
		}
		
		void OnEntryAdded (object sender, WorkspaceItemEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Item.ParentWorkspace);
			if (tb != null) {
				tb.AddChild (e.Item, true);
				tb.Expanded = true;
			}
		}

		void OnEntryRemoved (object sender, WorkspaceItemEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Item);
			if (tb != null)
				tb.Remove ();
		}
		
		void OnCombineRenamed (object sender, WorkspaceItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Item);
			if (tb != null) tb.Update ();
		}
	}
	
	class WorkspaceNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			Workspace sol = (Workspace) CurrentNode.DataItem;
			IdeApp.ProjectOperations.RenameItem (sol, newName);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			if (operation == DragOperation.Move) {
				WorkspaceItem it = dataObject as WorkspaceItem;
				if (it != null) {
					Workspace ws = (Workspace) CurrentNode.DataItem;
					return it != ws && !ws.Items.Contains (it);
				}
			}
			return false;
		}
		
		public override void OnMultipleNodeDrop (object[] dataObjects, DragOperation operation)
		{
			Set<IWorkspaceFileObject> toSave = new Set<IWorkspaceFileObject> ();
			foreach (object dataObject in dataObjects) {
				Workspace ws = (Workspace) CurrentNode.DataItem;
				WorkspaceItem it = (WorkspaceItem) dataObject;
				
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to move the item '{0}' to the workspace '{1}'?", it.Name, ws.Name), AlertButton.Move))
					return;
				
				if (it.ParentWorkspace != null) {
					it.ParentWorkspace.Items.Remove (it);
					ws.Items.Add (it);
					toSave.Add (it.ParentWorkspace);
					toSave.Add (ws);
				} else {
					IdeApp.Workspace.Items.Remove (it);
					ws.Items.Add (it);
					toSave.Add (ws);
				}
			}
			IdeApp.ProjectOperations.Save (toSave);
		}
			
		public override void ActivateItem ()
		{
			Workspace ws = CurrentNode.DataItem as Workspace;
			IdeApp.ProjectOperations.ShowOptions (ws);
		}

		[CommandHandler (EditCommands.Delete)]
		[AllowMultiSelection]
		public void RemoveItem ()
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Workspace ws = node.DataItem as Workspace;
				Workspace parent = node.GetParentDataItem (typeof(Workspace), false) as Workspace;
				if (parent == null) return;
				
				if (MessageService.Confirm (GettextCatalog.GetString ("Do you really want to remove the item '{0}' from workspace '{1}'?", ws.Name, parent.Name), AlertButton.Remove)) {
					parent.Items.Remove (ws);
					ws.Dispose ();
				}
			}
			IdeApp.Workspace.Save();
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		public void OnUpdateRemoveItem (CommandInfo info)
		{
			info.Text = GettextCatalog.GetString ("Remove");
			foreach (ITreeNavigator node in CurrentNodes) {
				Workspace parent = node.GetParentDataItem (typeof(Workspace), false) as Workspace;
				if (parent == null) {
					info.Enabled = false;
					return;
				}
			}
		}
		
		[CommandHandler (ProjectCommands.AddNewSolution)]
		public void AddNewSolutionToWorkspace ()
		{
			Workspace ws = (Workspace) CurrentNode.DataItem;
			WorkspaceItem ce = IdeApp.ProjectOperations.AddNewWorkspaceItem (ws);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddNewWorkspace)]
		public void AddNewWorkspaceToWorkspace ()
		{
			Workspace ws = (Workspace) CurrentNode.DataItem;
			WorkspaceItem ce = IdeApp.ProjectOperations.AddNewWorkspaceItem (ws, "MonoDevelop.Workspace");
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddItem)]
		public void AddProjectToCombine()
		{
			Workspace ws = (Workspace) CurrentNode.DataItem;
			WorkspaceItem ce = IdeApp.ProjectOperations.AddWorkspaceItem (ws);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.Reload)]
		[AllowMultiSelection]
		public void OnReload ()
		{
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
				m.BeginTask (null, CurrentNodes.Length);
				foreach (ITreeNavigator node in CurrentNodes) {
					Workspace ws = (Workspace) node.DataItem;
					ws.ParentWorkspace.ReloadItem (m, ws);
					m.Step (1);
				}
				m.EndTask ();
			}
		}
		
		[CommandUpdateHandler (ProjectCommands.Reload)]
		public void OnUpdateReload (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Workspace ws = (Workspace) node.DataItem;
				if (ws.ParentWorkspace == null || !ws.NeedsReload) {
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
		
		[CommandHandler (FileCommands.CloseWorkspaceItem)]
		[AllowMultiSelection]
		public void OnCloseItem ()
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Workspace ws = (Workspace) node.DataItem;
				IdeApp.Workspace.CloseWorkspaceItem (ws);
			}
		}
		
		[CommandUpdateHandler (FileCommands.CloseWorkspaceItem)]
		public void OnUpdateCloseItem (CommandInfo info)
		{
			foreach (ITreeNavigator node in CurrentNodes) {
				Workspace ws = (Workspace) node.DataItem;
				if (ws.ParentWorkspace != null) {
					info.Visible = false;
					return;
				}
			}
		}
	}
}
