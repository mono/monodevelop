// TransactedTreeBuilder.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Collections;

namespace MonoDevelop.Ide.Gui.Components
{
/*
  TransactedTreeBuilder is a ITreeBuilder which does not directly modify the tree, but instead
  it stores all changes in a special node store and. Those changes can be later applied all
  together to the tree.
 */
	
	public partial class ExtensibleTreeView
	{
		internal class TransactedTreeBuilder: ITreeBuilder
		{
			TreeNodeNavigator navigator;
			TreeNode node;
			TransactedNodeStore tstore;
			ExtensibleTreeView tree;

			public TransactedTreeBuilder (ExtensibleTreeView tree, TransactedNodeStore tstore, Gtk.TreeIter it)
			{
				this.tree = tree;
				this.tstore = tstore;
				navigator = new TreeNodeNavigator (tree, it);
				CheckNode ();
			}
			
			public object DataItem {
				get {
					return (node != null) ? node.DataItem : navigator.DataItem;
				}
			}
	
			public string NodeName {
				get {
					return (node != null) ? node.Name : navigator.NodeName;
				}
			}
	
			public bool Selected {
				get {
					return (node != null) ? node.Selected : navigator.Selected;
				}
				set {
					CreateNode ();
					node.Selected = value;
				}
			}
	
			public bool Expanded {
				get {
					return (node != null) ? node.Expanded : navigator.Expanded;
				}
				set {
					CreateNode ();
					node.Expanded = value;
					if (value)
						EnsureFilled ();
				}
			}
	
			public ITreeOptions Options {
				get {
					return tree.globalOptions;
				}
			}
	
			public TypeNodeBuilder TypeNodeBuilder {
				get {
					return (node != null) ? node.TypeNodeBuilder : navigator.TypeNodeBuilder;
				}
			}
	
			public NodePosition CurrentPosition {
				get {
					if (node != null) {
						NodePosition pos = new NodePosition ();
						pos._node = node;
						return pos;
					} else
						return navigator.CurrentPosition;
				}
			}
	
			public bool Filled {
				get {
					if (node != null) {
						if (node.Reset || !node.HasIter)
							return node.Filled;
						navigator.MoveToIter (node.NodeIter);
					}
					return navigator.Filled;
				}
			}
	
			public object GetParentDataItem (Type type, bool includeCurrent)
			{
				if (includeCurrent && type.IsInstanceOfType (DataItem))
					return DataItem;
				
				NodePosition pos = CurrentPosition;
				try {
					while (MoveToParent ()) {
						if (type.IsInstanceOfType (DataItem))
							return DataItem;
					}
					return null;
				} finally {
					MoveToPosition (pos);
				}
			}
	
			public void ExpandToNode ()
			{
				if (node != null) {
					NodePosition pos = CurrentPosition;
					try {
						if (!MoveToParent ())
							return;
						ExpandToNode ();
					} finally {
						MoveToPosition (pos);
						Expanded = true;
					}
				} else
					navigator.ExpandToNode ();
			}
	
			public NodeState SaveState ()
			{
				return NodeState.SaveState (tree, this);
			}
	
			public void RestoreState (NodeState state)
			{
				NodeState.RestoreState (tree, this, state);
			}
	
			public bool MoveToPosition (NodePosition position)
			{
				if (position._node != null) {
					node = (TreeNode) position._node;
					return !node.Deleted;
				}
				else {
					node = null;
					return navigator.MoveToPosition (position) && CheckNode ();
				}
			}
	
			public bool MoveToParent ()
			{
				if (node != null && node.Parent != null) {
					node = node.Parent;
					return true;
				}
				NodePosition oldPos = CurrentPosition;
				InitNavigator ();
				if (navigator.MoveToParent () && CheckNode ())
					return true;
				MoveToPosition (oldPos);
				return false;
			}
	
			public bool MoveToParent (Type type)
			{
				while (MoveToParent ()) {
					if (type.IsInstanceOfType (DataItem))
						return true;
				}
				return false;
			}
	
			public bool MoveToRoot ()
			{
				Gtk.TreeIter it;
				if (!tree.Store.GetIterFirst (out it))
					return false;

				navigator.MoveToIter (it);
				if (!CheckNode ())
					return MoveNext ();
				else
					return true;
			}
	
			public bool MoveToFirstChild ()
			{
				EnsureFilled ();
				if (node != null && node.Children != null && node.Children.Count > 0) {
					node = node.Children [0];
					return true;
				}

				if (node != null && (node.Reset || !node.HasIter))
					return false;
				
				NodePosition oldPos = CurrentPosition;
				InitNavigator ();
				if (!navigator.MoveToFirstChild ()) {
					MoveToPosition (oldPos);
					return false;
				}
				if (CheckNode ())
					return true;
				if (MoveNext ())
					return true;
				MoveToPosition (oldPos);
				return false;
			}
	
			public bool MoveToChild (string name, Type dataType)
			{
				NodePosition pos = CurrentPosition;
				
				if (!MoveToFirstChild ())
					return false;

				do {
					if ((name == null || NodeName == name) && (dataType == null || (DataItem != null && dataType.IsInstanceOfType (DataItem))))
						return true;
				} while (MoveNext ());

				MoveToPosition (pos);
				return false;
			}
	
			public bool HasChild (string name, Type dataType)
			{
				NodePosition pos = CurrentPosition;
				try {
					return MoveToChild (name, dataType);
				}
				finally {
					MoveToPosition (pos);
				}
			}
	
			public bool HasChildren ()
			{
				NodePosition pos = CurrentPosition;
				try {
					return MoveToFirstChild ();
				} finally {
					MoveToPosition (pos);
				}
			}
	
			public bool MoveNext ()
			{
				NodePosition oldPos = CurrentPosition;
				if (node != null && node.Parent != null) {
					int i = node.Parent.Children.IndexOf (node);
					if (i != -1 && i < node.Parent.Children.Count - 1) {
						node = node.Parent.Children [i + 1];
						return true;
					}
					// Position the navigator at the first child of the parent
					if (!node.Parent.HasIter)
						return false;
					node = node.Parent;
					InitNavigator ();
					if (!navigator.MoveToFirstChild ()) {
						MoveToPosition (oldPos);
						return false;
					}
					if (CheckNode ())
						return true;
				}
				InitNavigator ();
				while (navigator.MoveNext ()) {
					if (CheckNode ())
						return true;
				}
				MoveToPosition (oldPos);
				return false;
			}
	
			public bool MoveToObject (object dataObject)
			{
				node = tstore.GetNodeForObject (dataObject);
				if (node != null)
					return true;
				if (!navigator.MoveToObject (dataObject))
					return false;
				if (CheckNodeAndAncestors ())
					return true;
				else
					return MoveToNextObject ();
			}
	
			public bool MoveToNextObject ()
			{
				if (node != null && !node.HasIter) {
					object data = DataItem;
					node = tstore.GetNextNode (data, node);
					if (node != null)
						return true;
					if (!navigator.MoveToObject (data))
						return false;
					if (CheckNodeAndAncestors ())
						return true;
				}
				NodePosition oldPos = CurrentPosition;
				InitNavigator ();
				while (navigator.MoveToNextObject ()) {
					if (CheckNodeAndAncestors ())
						return true;
				}
				MoveToPosition (oldPos);
				return false;
			}

			void InitNavigator ()
			{
				if (node != null) {
					navigator.MoveToIter (node.NodeIter);
					node = null;
				}
			}
	
			public bool FindChild (object dataObject)
			{
				return FindChild (dataObject, false);
			}
	
			public bool FindChild (object dataObject, bool recursive)
			{
				foreach (TreeNode nod in tstore.GetAllNodes (dataObject)) {
					if (IsChildNode (nod, recursive)) {
						node = nod;
						return true;
					}
				}

				if (node != null && !node.HasIter)
					return false;

				NodePosition oldPos = CurrentPosition;
				InitNavigator ();

				Gtk.TreeIter piter = navigator.CurrentPosition._iter;
				if (!navigator.MoveToObject (dataObject)) {
					MoveToPosition (oldPos);
					return false;
				}

				do {
					if (!CheckNodeAndAncestors ())
						continue;
					if (IsChildIter (piter, navigator.CurrentPosition._iter, recursive))
						return true;
				} while (navigator.MoveToNextObject ());

				MoveToPosition (oldPos);
				return false;
			}
			
			bool IsChildNode (TreeNode n, bool recursive)
			{
				TreeNode cn = n.Parent;
				while (cn != null) {
					if (cn == node) return true;
					if (!recursive) return false;
					n = cn;
					cn = cn.Parent;
				}
				if (node != null) {
					if (!node.HasIter)
						return false;
					else
						return IsChildIter (node.NodeIter, n.NodeIter, recursive);
				}
				else
					return IsChildIter (navigator.CurrentPosition._iter, n.NodeIter, recursive);
			}
			
			bool IsChildIter (Gtk.TreeIter pit, Gtk.TreeIter cit, bool recursive)
			{
				Gtk.TreePath pitPath = tree.Store.GetPath (pit);
				Gtk.TreePath citPath = tree.Store.GetPath (cit);

				if (!citPath.Up ())
					return false;

				if (citPath.Equals (pitPath))
					return true;

				return recursive && pitPath.IsAncestor (citPath);
			}
	
			public ITreeNavigator Clone ()
			{
				TransactedTreeBuilder tb = (TransactedTreeBuilder) MemberwiseClone ();
				tb.navigator = (TreeNodeNavigator) tb.navigator.Clone ();
				return tb;
			}
	
			public void UpdateAll ()
			{
				Update ();
				UpdateChildren ();
			}
	
			public void Update ()
			{
				CreateNode ();
				NodeAttributes ats = TreeBuilder.GetAttributes (this, node.BuilderChain, node.DataItem);
				UpdateNode (node, node.BuilderChain, ats, node.DataItem);
			}
	
			public void UpdateChildren ()
			{
				CreateNode ();
				
				if (!Filled) {
					node.Reset = true;
					node.Filled = !TreeBuilder.HasChildNodes (this, node.BuilderChain, node.DataItem);
					return;
				}

				NodePosition pos = CurrentPosition;
				NodeState ns = SaveState ();
				MoveToPosition (pos);
				RestoreState (ns);
			}

			internal void ResetState ()
			{
				CreateNode ();

				if (node.HasIter && !node.Reset) {
					TreeNode piter = node;
					navigator.MoveToIter (node.NodeIter);
					RemoveChildren ();
					node = piter;
				}
				if (node.Children != null) {
					foreach (TreeNode cn in node.Children)
						tstore.RemoveNode (cn);
				}

				node.Children = null;
				node.Reset = true;
				node.Filled = !TreeBuilder.HasChildNodes (this, node.BuilderChain, node.DataItem);
			}

			void RemoveChildren ()
			{
				if (!navigator.Filled)
					return;
				if (navigator.MoveToFirstChild ()) {
					// Mark all children as removed
					do {
						node = null;
						if (CheckNode ()) {
							CreateNode ();
							tstore.RemoveNode (node);
							RemoveChildren ();
						}
					}
					while (navigator.MoveNext ());
					navigator.MoveToParent ();
				}
			}
	
			void FillNode ()
			{
				node.Filled = true;
				NodePosition pos = CurrentPosition;
				foreach (NodeBuilder builder in node.BuilderChain) {
					try {
						builder.PrepareChildNodes (node.DataItem);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					MoveToPosition (pos);
				}
				foreach (NodeBuilder builder in node.BuilderChain) {
					try {
						builder.BuildChildNodes (this, node.DataItem);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					MoveToPosition (pos);
				}
			}
			
			public void Remove ()
			{
				Remove (false);
			}
	
			public void Remove (bool moveToParent)
			{
				CreateNode ();
				TreeNode n = node;
				if (moveToParent)
					MoveToParent ();
				tstore.RemoveNode (n);
				if (n.Parent != null)
					n.Parent.Children.Remove (n);
			}
	
			public void AddChild (object dataObject)
			{
				AddChild (dataObject, false);
			}
			
			public void AddChildren (IEnumerable dataObjects)
			{
				foreach (object dataObject in dataObjects) {
					AddChild (dataObject, false);
				}
			}

	
			public void AddChild (object dataObject, bool moveToChild)
			{
				CreateNode ();
				if (!Filled) {
					// Don't fill the parent now. The child node will be shown
					// when expanding the parent.
					UpdateChildren ();
					return;
				}
				TreeNode n = CreateNode (dataObject);
				if (n != null) {
					if (node.Children == null)
						node.Children = new List<TreeNode> ();
					node.Children.Add (n);
					n.Parent = node;
					if (moveToChild)
						node = n;
				}
			}
	
			void EnsureFilled ()
			{
				if (node != null) {
					if (node.HasIter && !node.Reset) {
						navigator.MoveToIter (node.NodeIter);
						navigator.EnsureFilled ();
					} else if (!node.Filled) {
						FillNode ();
					}
				} else
					navigator.EnsureFilled ();
			}

			void CreateNode ()
			{
				if (node != null)
					return;
				TreeNode n = new TreeNode ();
				n.BuilderChain = navigator.BuilderChain;
				n.DataItem = navigator.DataItem;
				n.Expanded = navigator.Expanded;
				n.Filled = navigator.Filled;
				n.Name = navigator.NodeName;
				n.NodeIter = navigator.CurrentPosition._iter;
				n.Selected = navigator.Selected;
				n.TypeNodeBuilder = navigator.TypeNodeBuilder;
				tstore.RegisterNode (n);
				node = n;
			}

			bool CheckNode ()
			{
				// Checks that the current node is valid (it has not been deleted)
				if (navigator.CurrentPosition._iter.Equals (Gtk.TreeIter.Zero)) {
					node = null;
					return true;
				}
				node = tstore.GetNode (navigator.CurrentPosition._iter);
				return node == null || !node.Deleted;
			}
			
			bool CheckNodeAndAncestors ()
			{
				// Checks that the current node is valid (it has not been deleted)
				// and that no parent nodes have been deleted
				if (!CheckNode ())
					return false;
				Gtk.TreeIter it = navigator.CurrentPosition._iter;
				while (tree.Store.IterParent (out it, it)) {
					TreeNode n = tstore.GetNode (it);
					if (n != null && n.Deleted)
						return false;
				}
				return true;
			}
			
			TreeNode CreateNode (object dataObject)
			{
				if (dataObject == null) throw new ArgumentNullException ("dataObject");
				
				NodeBuilder[] chain = tree.GetBuilderChain (dataObject.GetType ());
				if (chain == null) return null;

				NodeAttributes ats;
				NodePosition pos = CurrentPosition;
				try {
					ats = TreeBuilder.GetAttributes (this, chain, dataObject);
					if ((ats & NodeAttributes.Hidden) != 0)
						return null;
				} finally {
					MoveToPosition (pos);
				}
				
				TreeNode n = new TreeNode ();
				n.DataItem = dataObject;
				n.BuilderChain = chain;
				n.Filled = false;

				UpdateNode (n, chain, ats, dataObject);

				n.Filled = !TreeBuilder.HasChildNodes (this, chain, dataObject);
				
				tstore.RegisterNode (n);
				return n;
			}

			void UpdateNode (TreeNode n, NodeBuilder[] chain, NodeAttributes ats, object dataObject)
			{
				string text;
				Gdk.Pixbuf icon;
				Gdk.Pixbuf closedIcon;
				TreeBuilder.GetNodeInfo (tree, this, chain, dataObject, out text, out icon, out closedIcon);

				n.Text = text;
				n.Icon = icon;
				n.ClosedIcon = closedIcon;
				
				if (chain != null && chain.Length > 0)
					n.Name = ((TypeNodeBuilder)chain[0]).GetNodeName (this, n.DataItem);
				else
					n.Name = n.Text;
				
				n.Modified = true;
			}
		}

		internal class TreeNode
		{
			public string Text;
			public Gdk.Pixbuf Icon;
			public Gdk.Pixbuf ClosedIcon;
			
			public bool Selected;
			public bool Filled;
			public bool Expanded;
			public TypeNodeBuilder TypeNodeBuilder;
			public TreeNode Parent;
			public object DataItem;
			public string Name;
			public List<TreeNode> Children;
			public Gtk.TreeIter NodeIter;
			public NodeBuilder[] BuilderChain;
			public bool Deleted;
			public bool Modified;
			public bool Reset;
			public bool DeleteDone;
			public bool ChildrenDeleted;

			public bool HasIter {
				get { return !NodeIter.Equals (Gtk.TreeIter.Zero); }
			}
		}

		internal class TransactedNodeStore
		{
			Dictionary<Gtk.TreeIter, TreeNode> iterToNode;
			Dictionary<object, List<TreeNode>> objects = new Dictionary<object,List<TreeNode>> ();
			ExtensibleTreeView tree;
			
			public TransactedNodeStore (ExtensibleTreeView tree)
			{
				this.tree = tree;
				iterToNode = new Dictionary<Gtk.TreeIter,TreeNode> (new IterComparer (tree.store));
			}
			
			public TreeNode GetNode (Gtk.TreeIter it)
			{
				TreeNode node;
				if (iterToNode.TryGetValue (it, out node))
					return node;
				else
					return null;
			}

			public bool IsDeleted (ITreeNavigator nav)
			{
				TreeNode n = GetNode (nav.CurrentPosition._iter);
				return n != null && n.Deleted;
			}

			public void RegisterNode (TreeNode node)
			{
				if (node.HasIter)
					iterToNode.Add (node.NodeIter, node);
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
				
				if (!node.HasIter && objects.TryGetValue (node.DataItem, out list)) {
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
				foreach (TreeNode node in iterToNode.Values) {
					CommitNode (node);
				}
			}

			void CommitNode (TreeNode node)
			{
				if (node.Deleted) {
					if (node.DeleteDone)
						// It means that a parent node has been deleted, so we can skip this one
						return;
					
					MarkDeleted (node);
					Gtk.TreeIter it = node.NodeIter;
					if (tree.Store.IterIsValid (it))
						tree.Store.Remove (ref it);
					return;
				}
				else if (node.Modified) {
					if (!this.tree.store.IterIsValid (node.NodeIter)) {
						LoggingService.LogError ("Found invalid iter for node. " + node.DataItem);
						return;
					}
					tree.Store.SetValue (node.NodeIter, ExtensibleTreeView.TextColumn, node.Text);
					if (node.Icon != null)
						tree.Store.SetValue (node.NodeIter, ExtensibleTreeView.OpenIconColumn, node.Icon);
					if (node.ClosedIcon != null)
						tree.Store.SetValue (node.NodeIter, ExtensibleTreeView.ClosedIconColumn, node.ClosedIcon);
				}
				if (node.Children != null) {
					foreach (TreeNode cn in node.Children) {
						Gtk.TreeIter it = tree.Store.AppendValues (node.NodeIter, cn.Text, cn.Icon, cn.ClosedIcon, cn.DataItem, cn.BuilderChain, cn.Filled);
						if (!cn.Filled)
							tree.Store.AppendNode (it);	// Dummy node
						// The OnNodeAdded event was already fired when the node was added. There is no need to fire it again.
						tree.RegisterNode (it, cn.DataItem, cn.BuilderChain, false);
						cn.NodeIter = it;
						cn.Modified = false;
						node.Reset = false;
						CommitNode (cn);
						tree.NotifyInserted (it, cn.DataItem);
					}
				}
				if (node.Reset && !node.Filled) {
					// Remove old children
					Gtk.TreeIter ci;
					while (tree.Store.IterChildren (out ci, node.NodeIter))
						tree.Store.Remove (ref ci);
					tree.Store.AppendNode (node.NodeIter);	// Dummy node
					tree.Store.SetValue (node.NodeIter, ExtensibleTreeView.FilledColumn, false);
				}
				if (node.Expanded)
					tree.Tree.ExpandToPath (tree.Store.GetPath (node.NodeIter));
			}
			
			void MarkDeleted (TreeNode node)
			{
				// Marks the node as deleted and unregisters it from the store
				// It also marks is children as deleted
				if (node.DeleteDone)
					return;
				node.DeleteDone = true;
				tree.UnregisterNode (node.DataItem, node.NodeIter, node.BuilderChain, true);
				MarkChildrenDeleted (node);
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
				if (!node.NodeIter.Equals (Gtk.TreeIter.Zero))
					MarkChildrenDeleted (node.NodeIter);
			}
			
			void MarkChildrenDeleted (Gtk.TreeIter it)
			{
				Gtk.TreeIter child;
				if (!tree.store.IterChildren (out child, it))
					return;
				do {
					TreeNode node;
					if (iterToNode.TryGetValue (child, out node)) {
						MarkDeleted (node);
					} else {
						MarkChildrenDeleted (child);
						object childData = tree.store.GetValue (child, ExtensibleTreeView.DataItemColumn);
						if (childData != null)
							tree.UnregisterNode (childData, child, null, true);
					}
				}
				while (tree.store.IterNext (ref child));
			}
		}
	}
}
