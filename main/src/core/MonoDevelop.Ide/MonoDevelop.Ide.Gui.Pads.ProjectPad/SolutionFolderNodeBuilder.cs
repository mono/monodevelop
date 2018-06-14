//
// SolutionFolderNodeBuilder.cs
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
using System.Linq;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class SolutionFolderNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(SolutionFolder); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SolutionFolderNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((SolutionFolder)dataObject).Name;
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			SolutionFolder folder = dataObject as SolutionFolder;
			nodeInfo.Label = GLib.Markup.EscapeText (folder.Name);
			nodeInfo.Icon = Context.GetIcon (Stock.SolutionFolderOpen);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.SolutionFolderClosed);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SolutionFolder folder = (SolutionFolder) dataObject;
			ctx.AddChildren (folder.Items);
			ctx.AddChildren (folder.Files.Select (file => new SolutionFolderFileNode (file, folder)));
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			SolutionFolder sf = (SolutionFolder) dataObject;
			return sf.Items.Count > 0 || sf.Files.Count > 0;
		}
		
		public override object GetParentObject (object dataObject)
		{
			SolutionFolder sf = (SolutionFolder) dataObject;
			return sf.IsRoot || sf.ParentFolder.IsRoot ? (object) sf.ParentSolution : (object) sf.ParentFolder;
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -1000;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			SolutionFolder folder = (SolutionFolder) dataObject;
			folder.NameChanged += OnSolutionFolderRenamed;
			folder.ItemAdded += OnEntryAdded;
			folder.ItemRemoved += OnEntryRemoved;
			folder.SolutionItemFileAdded += OnFileAdded;
			folder.SolutionItemFileRemoved += OnFileRemoved;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			SolutionFolder folder = (SolutionFolder) dataObject;
			folder.NameChanged -= OnSolutionFolderRenamed;
			folder.ItemAdded -= OnEntryAdded;
			folder.ItemRemoved -= OnEntryRemoved;
			folder.SolutionItemFileAdded -= OnFileAdded;
			folder.SolutionItemFileRemoved -= OnFileRemoved;
		}
		
		void OnSolutionFolderRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}
		
		void OnEntryAdded (object sender, SolutionItemChangeEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem.ParentFolder);
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
				
		void OnFileAdded (object s, SolutionItemFileEventArgs args)
		{
			SolutionFolder folder = (SolutionFolder) s;
			ITreeBuilder tb = Context.GetTreeBuilder (folder);
			if (tb != null) {
				tb.AddChild (new SolutionFolderFileNode (args.File, folder));
			}
		}
		
		void OnFileRemoved (object s, SolutionItemFileEventArgs args)
		{
			SolutionFolder folder = (SolutionFolder) s;
			ITreeBuilder tb = Context.GetTreeBuilder (folder);
			if (tb != null) {
				if (tb.MoveToChild (args.File, typeof(SolutionFolderFileNode)))
					tb.Remove ();
			}
		}
	}
	
	class SolutionFolderNodeCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			CurrentNode.Expanded = !CurrentNode.Expanded;
		}

		public async override void RenameItem (string newName)
		{
			if (newName.IndexOfAny (new char [] { '\'', '(', ')', '"', '{', '}', '|' } ) != -1) {
				MessageService.ShowError (GettextCatalog.GetString ("Solution name may not contain any of the following characters: {0}", "', (, ), \", {, }, |"));
				return;
			}
			
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			if (folder.Name != newName) {
				folder.Name = newName;
				await IdeApp.Workspace.SaveAsync ();
			}
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.Move;
		}
		
		public override bool CanDropNode (object dataObject, DragOperation operation)
		{
			if (dataObject is IFileItem)
				return true;
			SolutionFolderItem it = dataObject as SolutionFolderItem;
			return it != null && operation == DragOperation.Move;
		}
		
		public async override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			if (dataObject is SolutionFolderItem) {
				SolutionFolderItem it = (SolutionFolderItem) dataObject;
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to move the item '{0}' to the solution folder '{1}'?", it.Name, folder.Name), AlertButton.Move))
					return;
	
				// If the items belongs to another folder, it will be automatically removed from it
				folder.Items.Add (it);
			}
			else {
				DropFile (folder, (IFileItem) dataObject, operation);
			}
			
			await IdeApp.ProjectOperations.SaveAsync (folder.ParentSolution);
		}
		
		internal static void DropFile (SolutionFolder folder, IFileItem fileItem, DragOperation operation)
		{
			FilePath dest = folder.BaseDirectory.Combine (fileItem.FileName.FileName);
			if (operation == DragOperation.Copy) {
				if (dest == fileItem.FileName)
					dest = ProjectOperations.GetTargetCopyName (dest, false);
				FileService.CopyFile (fileItem.FileName, dest);
			}
			else {
				var pf = fileItem as ProjectFile;
				if (pf != null && pf.Project != null)
					pf.Project.Files.Remove (pf);
				var fn = fileItem as SolutionFolderFileNode;
				if (fn != null) {
					if (fn.Parent == folder)
						return;
					fn.Parent.Files.Remove (fileItem.FileName);
				}
				FileService.MoveFile (fileItem.FileName, dest);
			}
			folder.Files.Add (dest);
		}
			
		public override void ActivateMultipleItems ()
		{
			SolutionFolder folder = CurrentNode.DataItem as SolutionFolder;
			IdeApp.ProjectOperations.ShowOptions (folder);
		}
		
		public async override void DeleteItem ()
		{
			SolutionFolder folder = CurrentNode.DataItem as SolutionFolder;
			SolutionFolder parent = folder.ParentFolder;
			if (parent == null) return;
			
			bool yes = MessageService.Confirm (GettextCatalog.GetString ("Do you really want to remove the folder '{0}' from '{1}'?", folder.Name, parent.Name), AlertButton.Remove);
			if (yes) {
				Solution sol = folder.ParentSolution;
				parent.Items.Remove (folder);
				folder.Dispose ();
				await IdeApp.ProjectOperations.SaveAsync (sol);
			}
		}
		
		[CommandHandler (ProjectCommands.AddNewProject)]
		public void AddNewProjectToSolutionFolder()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionFolderItem ce = IdeApp.ProjectOperations.CreateProject (folder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddProject)]
		public async void AddProjectToSolutionFolder()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			var item = await IdeApp.ProjectOperations.AddSolutionItem (folder);
			if (item != null) {
				Tree.AddNodeInsertCallback (item, new TreeNodeCallback (OnEntryInserted));
				var node = Tree.GetNodeAtObject (folder);
				if (node != null)
					node.Expanded = true;
			}
		}
		
		[CommandHandler (ProjectCommands.AddSolutionFolder)]
		public void AddFolder()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionFolder ce = new SolutionFolder ();
			ce.Name = GettextCatalog.GetString ("New Folder");
			folder.Items.Add (ce);
			Tree.AddNodeInsertCallback (ce, OnFolderInserted);
			var node = Tree.GetNodeAtObject (folder);
			if (node != null)
				node.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.Reload)]
		[AllowMultiSelection]
		public void OnReload ()
		{
			using (ProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
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
		
		[CommandHandler (ProjectCommands.AddNewFiles)]
		protected void OnAddNewFiles ()
		{
			var folder = (SolutionFolder) CurrentNode.DataItem;
			if (!IdeApp.ProjectOperations.CreateSolutionFolderFile (folder)) {
				return;
			}
			CurrentNode.Expanded = true;
			if (IdeApp.Workbench.ActiveDocument != null)
				IdeApp.Workbench.ActiveDocument.Window.SelectWindow ();
		}

		[CommandHandler (ProjectCommands.AddFiles)]
		protected async void OnAddFiles ()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			if (IdeApp.ProjectOperations.AddFilesToSolutionFolder (folder)) {
				CurrentNode.Expanded = true;
				await IdeApp.ProjectOperations.SaveAsync (folder.ParentSolution);
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
