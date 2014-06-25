//
// TransactedNodeStore.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Collections;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
/*
  TransactedTreeBuilder is a ITreeBuilder which does not directly modify the tree, but instead
  it stores all changes in a special node store and. Those changes can be later applied all
  together to the tree.
 */

	internal class TransactedNodeStore
	{
		Dictionary<NodePosition, TreeNode> iterToNode = new Dictionary<NodePosition, TreeNode> ();
		Dictionary<object, List<TreeNode>> objects = new Dictionary<object,List<TreeNode>> ();
		ExtensibleTreeViewBackend backend;

		public ExtensibleTreeView Tree { get; private set; }
		
		public TransactedNodeStore (ExtensibleTreeView tree, ExtensibleTreeViewBackend backend)
		{
			this.Tree = tree;
			this.backend = backend;
		}
		
		public TreeNode GetNode (NodePosition pos)
		{
			TreeNode node;
			if (iterToNode.TryGetValue (pos, out node))
				return node;
			else
				return null;
		}

		public bool IsDeleted (ITreeNavigator nav)
		{
			TreeNode n = GetNode (nav.CurrentPosition);
			return n != null && n.Deleted;
		}

		public void RegisterNode (TreeNode node)
		{
			if (node.HasPosition)
				iterToNode.Add (node.NodePosition, node);
			else {
				List<TreeNode> nodes;
				if (!objects.TryGetValue (node.DataItem, out nodes)) {
					nodes = new List<TreeNode> ();
					objects [node.DataItem] = nodes;

					// We can't wait for the node commit to fire the added event. A node builder may subscribe
					// events in OnNodeAdded which may be fired during the transaction.
					foreach (NodeBuilder nb in node.BuilderChain) {
						try {
							nb.OnNodeAdded (node.DataItem);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
					}
				}
				nodes.Add (node);
			}
		}

		public void RemoveNode (TreeNode node)
		{
			node.Deleted = true;
			if (node.Children != null) {
				foreach (TreeNode cn in node.Children)
					RemoveNode (cn);
			}

			List<TreeNode> list = null;

			if (!node.HasPosition && objects.TryGetValue (node.DataItem, out list)) {
				list.Remove (node);
				if (list.Count == 0) {
					objects.Remove (node.DataItem);
					foreach (NodeBuilder nb in node.BuilderChain) {
						try {
							nb.OnNodeRemoved (node.DataItem);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
					}
				}
			}
		}

		public TreeNode GetNodeForObject (object dataObject)
		{
			List<TreeNode> nods;
			if (objects.TryGetValue (dataObject, out nods))
				return nods [0];
			else
				return null;
		}

		public TreeNode GetNextNode (object dataObject, TreeNode node)
		{
			List<TreeNode> nods;
			if (objects.TryGetValue (node.DataItem, out nods)) {
				int i = nods.IndexOf (node);
				if (i == -1 || i == nods.Count - 1)
					return null;
				else
					return nods [i + 1];
			}
			else
				return null;
		}
		
		public IEnumerable<TreeNode> GetAllNodes (object dataObject)
		{
			List<TreeNode> nods;
			if (objects.TryGetValue (dataObject, out nods))
				return nods;
			else
				return new TreeNode [0];
		}

		public void CommitChanges ()
		{
			// First of all, mark as deleted the children of deleted nodes
			// This avoids trying to delete nodes that have already been deleted (it sets the DeletedDone flag)
			foreach (TreeNode node in iterToNode.Values) {
				if (node.Deleted || (node.Reset && !node.Filled))
					MarkChildrenDeleted (node);
			}
			TreeNodeNavigator nav = backend.CreateNavigator ();
			foreach (TreeNode node in iterToNode.Values) {
				CommitNode (nav, node);
			}
		}

		protected void CommitNode (TreeNodeNavigator nav, TreeNode node)
		{
			if (node.Deleted) {
				if (node.DeleteDone)
					// It means that a parent node has been deleted, so we can skip this one
					return;

				MarkDeleted (node);
			}

			var pos = node.NodePosition;
			if (pos != null)
				nav.MoveToPosition (pos);

			if (node.Deleted) {
				if (pos != null)
					nav.Remove ();
				return;
			}
			else if (node.Modified) {
				if (pos != null)
					nav.Update (node.NodeInfo);
			}
			if (node.Children != null) {
				foreach (TreeNode cn in node.Children) {
					nav.AddChild (cn.DataItem, cn.BuilderChain, cn.NodeInfo, cn.Filled, true);
					((IExtensibleTreeViewFrontend)Tree).RegisterNode (nav.CurrentPosition, cn.DataItem, cn.BuilderChain, false);
					cn.NodePosition = nav.CurrentPosition;
					cn.Modified = false;
					node.Reset = false;
					CommitNode (nav, cn);
					Tree.NotifyInserted (nav, cn.DataItem);
					nav.MoveToPosition (pos);
				}
			}
			if (node.Reset && !node.Filled) {
				nav.ResetChildren ();
			}
			if (node.Expanded)
				nav.Expanded = true;
		}

		void MarkChildrenDeleted (TreeNode node)
		{
			// Marks all the children of the node as deleted, and unregisters them from the store
			if (node.ChildrenDeleted)
				return;
			node.ChildrenDeleted = true;
			if (node.Children != null) {
				foreach (TreeNode cn in node.Children)
					MarkDeleted (cn);
			}
			// It may have children not instantiated as TreeNode
			if (node.HasPosition)
				MarkChildrenDeleted (node.NodePosition);
		}

		void MarkDeleted (TreeNode node)
		{
			// Marks the node as deleted and unregisters it from the store
			// It also marks is children as deleted
			if (node.DeleteDone)
				return;
			node.DeleteDone = true;
			((IExtensibleTreeViewFrontend)Tree).UnregisterNode (node.DataItem, node.NodePosition, node.BuilderChain, true);
			MarkChildrenDeleted (node);
		}

		void MarkChildrenDeleted (NodePosition pos)
		{
			var nav = Tree.GetNodeAtPosition (pos);
			if (!nav.Filled || !nav.MoveToFirstChild ())
				return;

			do {
				TreeNode node;
				if (iterToNode.TryGetValue (nav.CurrentPosition, out node)) {
					MarkDeleted (node);
				} else {
					MarkChildrenDeleted (nav.CurrentPosition);
					object childData = nav.DataItem;
					if (childData != null)
						((IExtensibleTreeViewFrontend)Tree).UnregisterNode (childData, nav.CurrentPosition, null, true);
				}
			}
			while (nav.MoveNext ());
		}
	}
}
