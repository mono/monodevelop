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
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
/*
  TransactedTreeBuilder is a ITreeBuilder which does not directly modify the tree, but instead
  it stores all changes in a special node store and. Those changes can be later applied all
  together to the tree.
 */
	
	internal sealed class TransactedTreeBuilder: ITreeBuilder
	{
		TreeNodeNavigator navigator;
		TreeNode node;
		TransactedNodeStore tstore;
		IExtensibleTreeViewFrontend frontend;

		public TransactedTreeBuilder (IExtensibleTreeViewFrontend tree, TransactedNodeStore tstore, TreeNodeNavigator navigator)
		{
			this.frontend = tree;
			this.tstore = tstore;
			this.navigator = navigator;
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
				return frontend.GlobalOptions;
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
					if (node.Reset || !node.HasPosition)
						return node.Filled;
					navigator.MoveToPosition (node.NodePosition);
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
			return null;
			//				return NodeState.SaveState (tree, this);
		}

		public void RestoreState (NodeState state)
		{
			// NodeState.RestoreState (tree, this, state);
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
			if (!navigator.MoveToRoot ())
				return false;

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

			if (node != null && (node.Reset || !node.HasPosition))
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
				if (!node.Parent.HasPosition)
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
			if (node != null && !node.HasPosition) {
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
				navigator.MoveToPosition (node.NodePosition);
				node = null;
			}
		}
	
		public void ScrollToNode ()
		{
			frontend.Backend.ScrollToCell (node.NodePosition);
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

			if (node != null && !node.HasPosition)
				return false;

			NodePosition oldPos = CurrentPosition;
			InitNavigator ();

			var piter = navigator.CurrentPosition;
			if (!navigator.MoveToObject (dataObject)) {
				MoveToPosition (oldPos);
				return false;
			}

			do {
				if (!CheckNodeAndAncestors ())
					continue;
				if (frontend.Backend.IsChildPosition (piter, navigator.CurrentPosition, recursive))
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
				if (!node.HasPosition)
					return false;
				else
					return frontend.Backend.IsChildPosition (node.NodePosition, n.NodePosition, recursive);
			}
			else
				return frontend.Backend.IsChildPosition (navigator.CurrentPosition, n.NodePosition, recursive);
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
			NodeAttributes ats = frontend.GetAttributes (this, node.BuilderChain, node.DataItem);
			UpdateNode (node, node.BuilderChain, ats, node.DataItem);
		}

		public void UpdateChildren ()
		{
			CreateNode ();
			
			if (!Filled) {
				node.Reset = true;
				node.Filled = !frontend.HasChildNodes (this, node.BuilderChain, node.DataItem);
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

			if (node.HasPosition && !node.Reset) {
				TreeNode piter = node;
				navigator.MoveToPosition (node.NodePosition);
				RemoveChildren ();
				node = piter;
			}
			if (node.Children != null) {
				foreach (TreeNode cn in node.Children)
					tstore.RemoveNode (cn);
			}

			node.Children = null;
			node.Reset = true;
			node.Filled = !frontend.HasChildNodes (this, node.BuilderChain, node.DataItem);
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
				if (node.HasPosition && !node.Reset) {
					navigator.MoveToPosition (node.NodePosition);
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
			n.BuilderChain = navigator.NodeBuilderChain;
			n.DataItem = navigator.DataItem;
			n.Expanded = navigator.Expanded;
			n.Filled = navigator.Filled;
			n.Name = navigator.NodeName;
			n.NodePosition = navigator.CurrentPosition;
			n.Selected = navigator.Selected;
			n.TypeNodeBuilder = navigator.TypeNodeBuilder;
			tstore.RegisterNode (n);
			node = n;
		}

		bool CheckNode ()
		{
			// Checks that the current node is valid (it has not been deleted)
			if (!navigator.CurrentPosition.IsValid) {
				node = null;
				return true;
			}
			node = tstore.GetNode (navigator.CurrentPosition);
			return node == null || !node.Deleted;
		}
		
		bool CheckNodeAndAncestors ()
		{
			// Checks that the current node is valid (it has not been deleted)
			// and that no parent nodes have been deleted
			if (!CheckNode ())
				return false;
			var nav = navigator.Clone ();
			while (nav.MoveToParent ()) {
				TreeNode n = tstore.GetNode (nav.CurrentPosition);
				if (n != null && n.Deleted)
					return false;
			}
			return true;
		}
		
		TreeNode CreateNode (object dataObject)
		{
			if (dataObject == null) throw new ArgumentNullException ("dataObject");
			
			NodeBuilder[] chain = frontend.GetBuilderChain (dataObject.GetType ());
			if (chain == null) return null;

			NodeAttributes ats;
			NodePosition pos = CurrentPosition;
			try {
				ats = frontend.GetAttributes (this, chain, dataObject);
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

			n.Filled = !frontend.HasChildNodes (this, chain, dataObject);
			
			tstore.RegisterNode (n);
			return n;
		}

		void UpdateNode (TreeNode n, NodeBuilder[] chain, NodeAttributes ats, object dataObject)
		{
			n.NodeInfo = frontend.GetNodeInfo (this, chain, dataObject);

			if (chain != null && chain.Length > 0)
				n.Name = ((TypeNodeBuilder)chain[0]).GetNodeName (this, n.DataItem);
			else
				n.Name = n.NodeInfo.Label;
			
			n.Modified = true;
		}
	}


}
