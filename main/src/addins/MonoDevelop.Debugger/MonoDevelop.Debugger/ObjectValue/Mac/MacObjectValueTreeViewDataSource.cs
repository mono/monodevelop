//
// MacObjectValueTreeViewDataSource.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

using AppKit;
using Foundation;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The data source for the Cocoa implementation of the ObjectValueTreeView.
	/// </summary>
	class MacObjectValueTreeViewDataSource : NSOutlineViewDataSource
	{
		readonly Dictionary<ObjectValueNode, MacObjectValueNode> mapping = new Dictionary<ObjectValueNode, MacObjectValueNode> ();
		readonly MacObjectValueTreeView treeView;

		public MacObjectValueTreeViewDataSource (MacObjectValueTreeView treeView, ObjectValueNode root, bool allowWatchExpressions)
		{
			AllowWatchExpressions = allowWatchExpressions;
			this.treeView = treeView;

			Root = new MacObjectValueNode (null, root);
			mapping.Add (root, Root);

			foreach (var child in root.Children)
				Add (Root, child);

			if (allowWatchExpressions)
				Add (Root, new AddNewExpressionObjectValueNode ());
		}

		public MacObjectValueNode Root {
			get; private set;
		}

		public bool AllowWatchExpressions {
			get; private set;
		}

		public bool TryGetValue (ObjectValueNode node, out MacObjectValueNode value)
		{
			return mapping.TryGetValue (node, out value);
		}

		void Add (MacObjectValueNode parent, ObjectValueNode node)
		{
			var value = new MacObjectValueNode (parent, node);
			mapping [node] = value;

			parent.Children.Add (value);

			foreach (var child in node.Children)
				Add (value, child);

			if (node.HasChildren && !node.ChildrenLoaded)
				Add (value, new LoadingObjectValueNode (node));
		}

		void Insert (MacObjectValueNode parent, int index, ObjectValueNode node)
		{
			var value = new MacObjectValueNode (parent, node);
			mapping [node] = value;

			parent.Children.Insert (index, value);

			foreach (var child in node.Children)
				Add (value, child);

			if (node.HasChildren && !node.ChildrenLoaded)
				Add (value, new LoadingObjectValueNode (node));
		}

		void Remove (MacObjectValueNode node)
		{
			foreach (var child in node.Children)
				Remove (child);

			mapping.Remove (node.Target);
			node.Children.Clear ();
			node.Dispose ();
		}

		public void Replace (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			if (!TryGetValue (node, out var item))
				return;

			var parent = item.Parent;
			int index = -1;

			for (int i = 0; i < parent.Children.Count; i++) {
				if (parent.Children[i] == item) {
					index = i;
					break;
				}
			}

			if (index == -1)
				return;

			parent.Children.RemoveAt (index);
			mapping.Remove (item.Target);
			item.Dispose ();

			var indexes = new NSIndexSet (index);

			if (parent.Target is RootObjectValueNode)
				treeView.RemoveItems (indexes, null, NSTableViewAnimation.None);
			else
				treeView.RemoveItems (indexes, parent, NSTableViewAnimation.None);

			if (replacementNodes.Length > 0) {
				for (int i = 0; i < replacementNodes.Length; i++)
					Insert (parent, index + i, replacementNodes [i]);

				var range = new NSRange (index, replacementNodes.Length);
				indexes = NSIndexSet.FromNSRange (range);

				if (parent.Target is RootObjectValueNode)
					treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
				else
					treeView.InsertItems (indexes, parent, NSTableViewAnimation.None);
			}
		}

		public void ReloadChildren (ObjectValueNode node)
		{
			if (!TryGetValue (node, out var parent))
				return;

			NSIndexSet indexes;
			NSRange range;

			if (parent.Children.Count > 0) {
				range = new NSRange (0, parent.Children.Count);
				indexes = NSIndexSet.FromNSRange (range);

				foreach (var child in parent.Children) {
					mapping.Remove (child.Target);
					child.Dispose ();
				}

				parent.Children.Clear ();

				if (parent.Target is RootObjectValueNode)
					treeView.RemoveItems (indexes, null, NSTableViewAnimation.None);
				else
					treeView.RemoveItems (indexes, parent, NSTableViewAnimation.None);
			}

			for (int i = 0; i < node.Children.Count; i++)
				Add (parent, node.Children[i]);

			// if we did not load all the children, add a Show More node
			if (!node.ChildrenLoaded)
				Add (parent, new ShowMoreValuesObjectValueNode (node));

			range = new NSRange (0, parent.Children.Count);
			indexes = NSIndexSet.FromNSRange (range);

			if (parent.Target is RootObjectValueNode)
				treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
			else
				treeView.InsertItems (indexes, parent, NSTableViewAnimation.None);
		}

		public void Clear ()
		{
			int count = Root.Children.Count;

			if (AllowWatchExpressions)
				count--;

			for (int i = count - 1; i >= 0; i--) {
				var child = Root.Children[i];
				Root.Children.RemoveAt (i);
				Remove (child);
			}

			if (count <= 0)
				return;

			var range = new NSRange (0, count);
			var indexes = NSIndexSet.FromNSRange (range);

			treeView.RemoveItems (indexes, null, NSTableViewAnimation.None);
		}

		public void Append (ObjectValueNode node)
		{
			int index;

			if (AllowWatchExpressions) {
				index = Root.Children.Count - 1;
				Insert (Root, index, node);
			} else {
				index = Root.Children.Count;
				Add (Root, node);
			}

			var indexes = NSIndexSet.FromIndex (index);

			treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
		}

		public void Append (IList<ObjectValueNode> nodes)
		{
			int index;

			if (AllowWatchExpressions) {
				index = Root.Children.Count - 1;
				for (int i = 0; i < nodes.Count; i++)
					Insert (Root, index + i, nodes[i]);
			} else {
				index = Root.Children.Count;
				for (int i = 0; i < nodes.Count; i++)
					Add (Root, nodes[i]);
			}

			var range = new NSRange (index, nodes.Count);
			var indexes = NSIndexSet.FromNSRange (range);

			treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
		}

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			var node = (item as MacObjectValueNode) ?? Root;

			if (node == null)
				return 0;

			return node.Children.Count;
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
		{
			var node = (item as MacObjectValueNode) ?? Root;

			if (node == null || childIndex >= node.Children.Count)
				return null;

			return node.Children [(int) childIndex];
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			var node = (item as MacObjectValueNode) ?? Root;

			return node != null && node.Children.Count > 0;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				foreach (var kvp in mapping)
					kvp.Value.Dispose ();
				mapping.Clear ();
				Root = null;
			}

			base.Dispose (disposing);
		}
	}
}
