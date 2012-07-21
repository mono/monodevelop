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
		SolutionItemRenamedEventHandler nameChanged;
		SolutionItemChangeEventHandler entryAdded;
		SolutionItemChangeEventHandler entryRemoved;
		EventHandler<SolutionItemFileEventArgs> fileAdded;
		EventHandler<SolutionItemFileEventArgs> fileRemoved;
		
		public SolutionFolderNodeBuilder ()
		{
			nameChanged = DispatchService.GuiDispatch<SolutionItemRenamedEventHandler> (OnSolutionFolderRenamed);
			entryAdded = DispatchService.GuiDispatch<SolutionItemChangeEventHandler> (OnEntryAdded);
			entryRemoved = DispatchService.GuiDispatch<SolutionItemChangeEventHandler> (OnEntryRemoved);
			fileAdded = DispatchService.GuiDispatch<EventHandler<SolutionItemFileEventArgs>> (OnFileAdded);
			fileRemoved =  DispatchService.GuiDispatch<EventHandler<SolutionItemFileEventArgs>> (OnFileRemoved);
		}

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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SolutionFolder folder = dataObject as SolutionFolder;
			label = GLib.Markup.EscapeText (folder.Name);
			icon = Context.GetIcon (Stock.SolutionFolderOpen);
			closedIcon = Context.GetIcon (Stock.SolutionFolderClosed);
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SolutionFolder folder = (SolutionFolder) dataObject;
			foreach (SolutionItem entry in folder.Items)
				ctx.AddChild (entry);
			foreach (FilePath file in folder.Files)
				ctx.AddChild (new SolutionFolderFileNode (file, folder));
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
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is SolutionFolder)
				return DefaultSort;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			SolutionFolder folder = (SolutionFolder) dataObject;
			folder.NameChanged += nameChanged;
			folder.ItemAdded += entryAdded;
			folder.ItemRemoved += entryRemoved;
			folder.SolutionItemFileAdded += fileAdded;
			folder.SolutionItemFileRemoved += fileRemoved;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			SolutionFolder folder = (SolutionFolder) dataObject;
			folder.NameChanged -= nameChanged;
			folder.ItemAdded -= entryAdded;
			folder.ItemRemoved -= entryRemoved;
			folder.SolutionItemFileAdded -= fileAdded;
			folder.SolutionItemFileRemoved -= fileRemoved;
		}
		
		void OnSolutionFolderRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}
		
		void OnEntryAdded (object sender, SolutionItemEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem.ParentFolder);
			if (tb != null) {
				tb.AddChild (e.SolutionItem, true);
				tb.Expanded = true;
			}
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs e)
		{
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
			if (dataObject is IFileItem)
				return true;
			SolutionItem it = dataObject as SolutionItem;
			return it != null && operation == DragOperation.Move;
		}
		
		public override void OnNodeDrop (object dataObject, DragOperation operation)
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			if (dataObject is SolutionItem) {
				SolutionItem it = (SolutionItem) dataObject;
				if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to move the item '{0}' to the solution folder '{1}'?", it.Name, folder.Name), AlertButton.Move))
					return;
	
				// If the items belongs to another folder, it will be automatically removed from it
				folder.Items.Add (it);
			}
			else {
				DropFile (folder, (IFileItem) dataObject, operation);
			}
			
		    IdeApp.ProjectOperations.Save (folder.ParentSolution);
		}
		
		internal static void DropFile (SolutionFolder folder, IFileItem fileItem, DragOperation operation)
		{
			FilePath dest = folder.BaseDirectory.Combine (fileItem.FileName.FileName);
			if (operation == DragOperation.Copy)
				FileService.CopyFile (fileItem.FileName, dest);
			else
				FileService.MoveFile (fileItem.FileName, dest);
			folder.Files.Add (dest);
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
		public void AddNewProjectToSolutionFolder()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			SolutionItem ce = IdeApp.ProjectOperations.CreateProject (folder);
			if (ce == null) return;
			Tree.AddNodeInsertCallback (ce, new TreeNodeCallback (OnEntryInserted));
			CurrentNode.Expanded = true;
		}
		
		[CommandHandler (ProjectCommands.AddProject)]
		public void AddProjectToSolutionFolder()
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
			using (IProgressMonitor m = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (true)) {
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
		
/*		[CommandHandler (ProjectCommands.AddNewFiles)]
		protected void OnAddNewFiles ()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			if (IdeApp.ProjectOperations.CreateProjectFile (null, folder.BaseDirectory)) {
				IdeApp.ProjectOperations.Save (folder.ParentSolution);
				CurrentNode.Expanded = true;
			}
		}*/
		
		[CommandHandler (ProjectCommands.AddFiles)]
		protected void OnAddFiles ()
		{
			SolutionFolder folder = (SolutionFolder) CurrentNode.DataItem;
			if (IdeApp.ProjectOperations.AddFilesToSolutionFolder (folder)) {
				CurrentNode.Expanded = true;
				IdeApp.ProjectOperations.Save (folder.ParentSolution);
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
