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
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.Components
{
	public class TreeViewState
	{
		List<NodeInfo> state;
		TreeView tree;
		int idColumn;
		
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
			TreeIter iter;

			state = new List<NodeInfo> ();
			if (!tree.Model.GetIterFirst (out iter))
				return;

			Save (state, iter);
		}
		
		void Save (ICollection<NodeInfo> collection, TreeIter iter)
		{
			do {
				TreeIter child;

				var node = new NodeInfo {
					Expanded = tree.GetRowExpanded (tree.Model.GetPath (iter)),
					Selected = tree.Selection.IterIsSelected (iter),
					Id = tree.Model.GetValue (iter, idColumn)
				};

				collection.Add (node);

				if (tree.Model.IterChildren (out child, iter)) {
					node.ChildInfo = new List<NodeInfo> ();
					Save (node.ChildInfo, child);
				}
			} while (tree.Model.IterNext (ref iter));
		}
		
		public void Load ()
		{
			if (state == null)
				throw new InvalidOperationException ("State not saved");

			TreeIter iter;
			if (!tree.Model.GetIterFirst (out iter))
				return;

			Load (state, iter);
			state = null;
		}
		
		void Load (List<NodeInfo> info, TreeIter iter)
		{
			var nodes = new Dictionary<NodeInfo, TreeIter> ();
			var infoCopy = new List<NodeInfo> (info);
			bool oneSelected = false;

			do {
				object id = tree.Model.GetValue (iter, idColumn);
				NodeInfo ni = ExtractNodeInfo (info, id);

				if (ni != null) {
					nodes[ni] = iter;

					if (ni.Expanded)
						tree.ExpandRow (tree.Model.GetPath (iter), false);
					else
						tree.CollapseRow (tree.Model.GetPath (iter));

					if (ni.Selected) {
						oneSelected = true;
						tree.Selection.SelectIter (iter);
					} else {
						tree.Selection.UnselectIter (iter);
					}

					if (ni.ChildInfo != null) {
						TreeIter child;

						if (tree.Model.IterChildren (out child, iter))
							Load (ni.ChildInfo, child);
					}
				}
			} while (tree.Model.IterNext (ref iter));
			
			// If this tree level had a selected node and this node has been deleted, then
			// try to select and adjacent node
			if (!oneSelected) {
				// 'info' contains the nodes that have not been inserted
				if (info.Any (n => n.Selected)) {
					NodeInfo adjacent = FindAdjacentNode (infoCopy, nodes, info[0]);

					if (adjacent != null) {
						iter = nodes [adjacent];
						tree.Selection.SelectIter (iter);
					}
				}
			}
		}
		
		static NodeInfo FindAdjacentNode (IList<NodeInfo> infos, IDictionary<NodeInfo,TreeIter> nodes, NodeInfo referenceNode)
		{
			int index = infos.IndexOf (referenceNode);

			for (int i = index; i < infos.Count; i++) {
				if (nodes.ContainsKey (infos[i]))
					return infos[i];
			}

			for (int i = index - 1; i >= 0; i--) {
				if (nodes.ContainsKey (infos[i]))
					return infos[i];
			}

			return null;
		}
		
		static NodeInfo ExtractNodeInfo (IList<NodeInfo> info, object id)
		{
			for (int i = 0; i < info.Count; i++) {
				var ni = info[i];

				if (object.Equals (ni.Id, id)) {
					info.RemoveAt (i);
					return ni;
				}
			}

			return null;
		}
	}
}
