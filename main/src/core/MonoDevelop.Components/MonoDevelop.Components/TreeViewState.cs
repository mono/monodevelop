// TreeViewState.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;
using System.Collections.Generic;

namespace MonoDevelop.Components
{
	public class TreeViewState
	{
		TreeView tree;
		int idColumn;
		List<NodeInfo> state;
		
		class NodeInfo {
			public object Id;
			public bool Expanded;
			public bool Selected;
			public List<NodeInfo> ChildInfo;
		}
		
		public TreeViewState (TreeView tree, int idColumn)
		{
			this.tree = tree;
			this.idColumn = idColumn;
		}
		
		public void Save ()
		{
			TreeIter it;
			state = new List<NodeInfo> ();
			if (!tree.Model.GetIterFirst (out it))
				return;
			Save (state, it);
		}
		
		void Save (List<NodeInfo> info, TreeIter it)
		{
			do {
				object id = tree.Model.GetValue (it, idColumn);
				NodeInfo ni = new NodeInfo ();
				ni.Id = id;
				ni.Expanded = tree.GetRowExpanded (tree.Model.GetPath (it));
				ni.Selected = tree.Selection.IterIsSelected (it);
				info.Add (ni);
				TreeIter cit;
				if (tree.Model.IterChildren (out cit, it)) {
					ni.ChildInfo = new List<NodeInfo> ();
					Save (ni.ChildInfo, cit);
				}
			}
			while (tree.Model.IterNext (ref it));
		}
		
		public void Load ()
		{
			if (state == null)
				throw new InvalidOperationException ("State not saved");
			TreeIter it;
			if (!tree.Model.GetIterFirst (out it))
				return;
			Load (state, it);
			state = null;
		}
		
		void Load (List<NodeInfo> info, TreeIter it)
		{
			do {
				object id = tree.Model.GetValue (it, idColumn);
				NodeInfo ni = ExtractNodeInfo (info, id);
				if (ni != null) {
					if (ni.Expanded)
						tree.ExpandRow (tree.Model.GetPath (it), false);
					else
						tree.CollapseRow (tree.Model.GetPath (it));
					if (ni.Selected)
						tree.Selection.SelectIter (it);
					else
						tree.Selection.UnselectIter (it);
					
					if (ni.ChildInfo != null) {
						TreeIter cit;
						if (tree.Model.IterChildren (out cit, it))
							Load (ni.ChildInfo, cit);
					}
				}
			}
			while (tree.Model.IterNext (ref it));
		}
		
		NodeInfo ExtractNodeInfo (List<NodeInfo> info, object id)
		{
			for (int n=0; n<info.Count; n++) {
				NodeInfo ni = (NodeInfo) info [n];
				if (object.Equals (ni.Id, id)) {
					info.RemoveAt (n);
					return ni;
				}
			}
			return null;
		}
	}
}
