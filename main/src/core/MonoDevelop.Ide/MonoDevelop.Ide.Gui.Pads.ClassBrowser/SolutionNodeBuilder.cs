//
// SolutionNodeBuilder
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
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ClassBrowser
{
	public class SolutionNodeBuilder : TypeNodeBuilder
	{
		SolutionItemChangeEventHandler SolutionItemAdded;
		SolutionItemChangeEventHandler SolutionItemRemoved;
		EventHandler<WorkspaceItemRenamedEventArgs> combineNameChanged;
		
		public SolutionNodeBuilder ()
		{
			SolutionItemAdded   = (SolutionItemChangeEventHandler) DispatchService.GuiDispatch (new SolutionItemChangeEventHandler (OnEntryAdded));
			SolutionItemRemoved = (SolutionItemChangeEventHandler) DispatchService.GuiDispatch (new SolutionItemChangeEventHandler (OnEntryRemoved));
			combineNameChanged  = (EventHandler<WorkspaceItemRenamedEventArgs>) DispatchService.GuiDispatch (new EventHandler<WorkspaceItemRenamedEventArgs> (OnCombineRenamed));
		}
			
		public override Type NodeDataType {
			get { return typeof(Solution); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Combine"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Solution)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Solution solution = dataObject as Solution;
			label = GettextCatalog.GetString ("Solution {0}", solution.Name);
			icon = Context.GetIcon (Stock.Solution);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Solution solution = (Solution) dataObject;
			foreach (SolutionItem entry in solution.Items) {
				builder.AddChild (entry);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((Solution) dataObject).Items.Count > 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is Solution)
				return DefaultSort;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			Solution sol = (Solution) dataObject;
			sol.SolutionItemAdded += SolutionItemAdded;
			sol.SolutionItemRemoved += SolutionItemRemoved;
			sol.NameChanged += combineNameChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Solution sol = (Solution) dataObject;
			sol.SolutionItemAdded -= SolutionItemAdded;
			sol.SolutionItemRemoved -= SolutionItemRemoved;
			sol.NameChanged -= combineNameChanged;
		}
		
		void OnEntryAdded (object sender, SolutionItemEventArgs e)
		{
			DispatchService.GuiDispatch (OnAddEntry, e.SolutionItem);
		}
		
		void OnAddEntry (object newEntry)
		{
			SolutionItem item = (SolutionItem) newEntry;
			ITreeBuilder tb = Context.GetTreeBuilder (item.ParentSolution);
			if (tb != null) {
				tb.AddChild (item, true);
				tb.Expanded = true;
			}
		}

		void OnEntryRemoved (object sender, SolutionItemEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Remove ();
		}
		
		void OnCombineRenamed (object sender, WorkspaceItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.Item);
			if (tb != null) 
				tb.Update ();
		}
	}
}
