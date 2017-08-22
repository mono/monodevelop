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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using System.IO;
using System.Linq;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class SolutionNodeBuilder: TypeNodeBuilder
	{
		public SolutionNodeBuilder ()
		{
			IdeApp.Workspace.ItemAddedToSolution += OnSolutionItemAddedRemoved;
			IdeApp.Workspace.ItemRemovedFromSolution += OnSolutionItemAddedRemoved;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			IdeApp.Workspace.ItemAddedToSolution -= OnSolutionItemAddedRemoved;
			IdeApp.Workspace.ItemRemovedFromSolution -= OnSolutionItemAddedRemoved;
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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			Solution solution = dataObject as Solution;
			nodeInfo.Label = GLib.Markup.EscapeText (solution.Name);
			nodeInfo.Icon = Context.GetIcon (Stock.Solution);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			Solution solution = (Solution) dataObject;
			ctx.AddChildren (solution.RootFolder.Items);
			ctx.AddChildren (solution.RootFolder.Files.Select (f => new SolutionFolderFileNode (f, solution.RootFolder)));
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			Solution sol = (Solution) dataObject;
			return sol.RootFolder.Items.Count > 0 || sol.RootFolder.Files.Count > 0;
		}
		
		public override object GetParentObject (object dataObject)
		{
			return ((Solution) dataObject).ParentWorkspace;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			Solution solution = (Solution) dataObject;
			solution.NameChanged += OnCombineRenamed;
			solution.StartupItemChanged += OnStartupChanged;
			solution.RunConfigurationsChanged += OnStartupChanged;
			solution.StartupConfigurationChanged += OnStartupChanged;
			solution.RootFolder.ItemAdded += OnEntryAdded;
			solution.RootFolder.ItemRemoved += OnEntryRemoved;
			solution.RootFolder.SolutionItemFileAdded += OnFileAdded;
			solution.RootFolder.SolutionItemFileRemoved += OnFileRemoved;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Solution solution = (Solution) dataObject;
			solution.NameChanged -= OnCombineRenamed;
			solution.StartupItemChanged -= OnStartupChanged;
			solution.RunConfigurationsChanged -= OnStartupChanged;
			solution.StartupConfigurationChanged -= OnStartupChanged;
			solution.RootFolder.ItemAdded -= OnEntryAdded;
			solution.RootFolder.ItemRemoved -= OnEntryRemoved;
			solution.RootFolder.SolutionItemFileAdded -= OnFileAdded;
			solution.RootFolder.SolutionItemFileRemoved -= OnFileRemoved;
		}
		
		void OnStartupChanged (object sender, EventArgs args)
		{
			foreach (SolutionItem it in IdeApp.Workspace.GetAllSolutionItems ()) {
				ITreeBuilder tb = Context.GetTreeBuilder (it);
				if (tb != null)
					tb.Update ();
			}
		}
		
		void OnSolutionItemAddedRemoved (object sender, SolutionItemChangeEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Solution);
			if (tb != null)
				tb.Update ();	// Update the entry count
		}

		void OnEntryAdded (object sender, SolutionItemChangeEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem.ParentSolution);
			if (tb != null) {
				if (e.Reloading)
					// When reloading we ignore the removed event, and we do an UpdateAll here. This will
					// replace the reloaded instance and will preserve the tree status
					tb.UpdateAll ();
				else {
					tb.AddChild (e.SolutionItem, true);
					tb.Expanded = true;
				}
			}
		}

		void OnEntryRemoved (object sender, SolutionItemChangeEventArgs e)
		{
			// If reloading, ignore the event. We handle it in OnEntryAdded.
			if (e.Reloading)
				return;
			
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null)
				tb.Remove ();
		}
		
		void OnCombineRenamed (object sender, WorkspaceItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Item);
			if (tb != null) tb.Update ();
		}
				
		void OnFileAdded (object s, SolutionItemFileEventArgs args)
		{
			SolutionFolder folder = (SolutionFolder) s;
			ITreeBuilder tb = Context.GetTreeBuilder (folder.ParentSolution);
			if (tb != null)
				tb.AddChild (new SolutionFolderFileNode (args.File, folder));
		}
		
		void OnFileRemoved (object s, SolutionItemFileEventArgs args)
		{
			SolutionFolder folder = (SolutionFolder) s;
			ITreeBuilder tb = Context.GetTreeBuilder (folder.ParentSolution);
			if (tb != null) {
				if (tb.MoveToChild (args.File, typeof(SolutionFolderFileNode)))
					tb.Remove ();
			}
		}
	}
	
	class SolutionNodeCommandHandler: NodeCommandHandler
	{
		public override void RenameItem (string newName)
		{
			Solution sol = (Solution) CurrentNode.DataItem;
			if (sol.Name != newName)
				IdeApp.ProjectOperations.RenameItem (sol, newName);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			return (dataObject is SolutionFolderItem) || (dataObject is IFileItem);
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			Solution sol = CurrentNode.DataItem as Solution;
			if (dataObject is SolutionFolderItem) {
				SolutionFolderItem it = (SolutionFolderItem) dataObject;
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to move the item '{0}' to the root node of the solution?", it.Name), AlertButton.Move))
					return;
	
				// If the items belongs to another folder, it will be automatically removed from it
				sol.RootFolder.Items.Add (it);
			}
			else {
				SolutionFolderNodeCommandHandler.DropFile (sol.DefaultSolutionFolder, (IFileItem) dataObject, operation);
			}
			IdeApp.ProjectOperations.SaveAsync (sol);
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
					if (IdeApp.Workspace.RequestItemUnload (solution)) {
						parent.Items.Remove (solution);
						solution.Dispose ();
						items.Add (parent);
					}
				}
			}
			IdeApp.ProjectOperations.SaveAsync (items);
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
			SolutionFolderItem ce = IdeApp.ProjectOperations.CreateProject (solution.RootFolder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddProject)]
		public async void AddProjectToCombine()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			var res = await IdeApp.ProjectOperations.AddSolutionItem (solution.RootFolder);
			if (res == null) return;
			Tree.AddNodeInsertCallback (res, new TreeNodeCallback (OnEntryInserted));
			var node = Tree.GetNodeAtObject (solution);
			if (node != null)
				node.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddSolutionFolder)]
		public void AddFolder()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			var ce = new SolutionFolder ();
			ce.Name = GettextCatalog.GetString ("New Folder");
			solution.RootFolder.Items.Add (ce);
			Tree.AddNodeInsertCallback (ce, OnFolderInserted);
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.Reload)]
		[AllowMultiSelection]
		public void OnReload ()
		{
			using (ProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
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

		[CommandHandler (ProjectCommands.EditSolutionItem)]
		public void OnEditSolution ()
		{
			var solution = (Solution) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (solution.FileName, project:null);
		}

		[CommandUpdateHandler (ProjectCommands.EditSolutionItem)]
		public void OnEditSolutionUpdate (CommandInfo info)
		{
			var solution = (Solution) CurrentNode.DataItem;
			info.Visible = info.Enabled = !string.IsNullOrEmpty (solution.FileName) && File.Exists (solution.FileName);
		}
		
/*		[CommandHandler (ProjectCommands.AddNewFiles)]
		protected void OnAddNewFiles ()
		{
			Solution sol = (Solution) CurrentNode.DataItem;
			if (IdeApp.ProjectOperations.CreateProjectFile (null, sol.BaseDirectory)) {
				IdeApp.ProjectOperations.Save (sol);
				CurrentNode.Expanded = true;
			}
		}*/
		
		[CommandHandler (ProjectCommands.AddFiles)]
		protected void OnAddFiles ()
		{
			Solution sol = (Solution) CurrentNode.DataItem;
			if (IdeApp.ProjectOperations.AddFilesToSolutionFolder (sol.RootFolder))
				IdeApp.ProjectOperations.SaveAsync (sol);
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
				IdeApp.Workspace.CloseWorkspaceItem (solution).Ignore();
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
