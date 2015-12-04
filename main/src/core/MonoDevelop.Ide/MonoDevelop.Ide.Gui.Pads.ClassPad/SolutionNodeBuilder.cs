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
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class SolutionNodeBuilder: TypeNodeBuilder
	{
		EventHandler<WorkspaceItemRenamedEventArgs> combineNameChanged;
		EventHandler startupChanged;
		
		public SolutionNodeBuilder ()
		{
			combineNameChanged = (EventHandler<WorkspaceItemRenamedEventArgs>) DispatchService.GuiDispatch (new EventHandler<WorkspaceItemRenamedEventArgs> (OnCombineRenamed));
			startupChanged = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnStartupChanged));
			
			IdeApp.Workspace.ItemAddedToSolution += OnEntryAdded;
			IdeApp.Workspace.ItemRemovedFromSolution += OnEntryRemoved;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			IdeApp.Workspace.ItemAddedToSolution -= OnEntryAdded;
			IdeApp.Workspace.ItemRemovedFromSolution -= OnEntryRemoved;
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
			int count = 0;
			foreach (SolutionItem e in solution.GetAllSolutionItems ())
				if (!(e is SolutionFolder))
					count++;
			
			switch (count) {
				case 0:
					nodeInfo.Label = GettextCatalog.GetString ("Solution {0}", solution.Name);
					break;
				case 1:
					nodeInfo.Label = GettextCatalog.GetString ("Solution {0} (1 entry)", solution.Name);
					break;
				default:
					nodeInfo.Label = GettextCatalog.GetString ("Solution {0} ({1} entries)", solution.Name, count);
					break;
			}

			nodeInfo.Icon = Context.GetIcon (Stock.Solution);
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
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null)
				tb.UpdateAll ();
		}
		
		void OnEntryAdded (object sender, SolutionItemEventArgs e)
		{
			ITreeBuilder tb;
			if (e.SolutionItem.ParentFolder == null)
				tb = Context.GetTreeBuilder (e.SolutionItem.ParentSolution);
			else
				tb = Context.GetTreeBuilder (e.SolutionItem.ParentFolder);
			
			if (tb != null)
				tb.AddChild (e.SolutionItem, true);
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) {
				tb.Remove (true);
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
			
		public override void ActivateItem ()
		{
			Solution sol = CurrentNode.DataItem as Solution;
			IdeApp.ProjectOperations.ShowOptions (sol);
		}
		
		[CommandHandler (EditCommands.Delete)]
		public void RemoveItem ()
		{
			Solution solution = CurrentNode.DataItem as Solution;
			Workspace parent = CurrentNode.GetParentDataItem (typeof(Workspace), false) as Workspace;
			if (parent == null) return;
			
			AlertButton res = MessageService.AskQuestion (GettextCatalog.GetString ("Do you really want to remove solution {0} from workspace {1}?", solution.Name, parent.Name), AlertButton.Remove);
			if (res == AlertButton.Remove) {
				parent.Items.Remove (solution);
				solution.Dispose ();
				IdeApp.Workspace.Save();
			}
		}
		
		[CommandUpdateHandler (EditCommands.Delete)]
		public void OnUpdateRemoveItem (CommandInfo info)
		{
			Workspace parent = CurrentNode.GetParentDataItem (typeof(Workspace), false) as Workspace;
			info.Enabled = parent != null;
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
		public void OnReload ()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
				solution.ParentWorkspace.ReloadItem (m, solution);
			}
		}
		
		[CommandUpdateHandler (ProjectCommands.Reload)]
		public void OnUpdateReload (CommandInfo info)
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			info.Visible = (solution.ParentWorkspace != null) && solution.NeedsReload;
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
		
		[CommandHandler (FileCommands.OpenContainingFolder)]
		public void OnOpenFolder ()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			DesktopService.OpenFolder (solution.BaseDirectory, solution.FileName);
		}
		
		[CommandHandler (SearchCommands.FindInFiles)]
		public void OnFindInFiles ()
		{
			Solution solution = (Solution) CurrentNode.DataItem;
			FindInFilesDialog.FindInPath (solution.BaseDirectory);
		}
	}
}
