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
			mapping[node] = value;

			parent.Children.Add (value);

			if (treeView.AllowExpanding) {
				foreach (var child in node.Children)
					Add (value, child);

				if (node.HasChildren && !node.ChildrenLoaded)
					Add (value, new LoadingObjectValueNode (node));
			}
		}

		void Insert (MacObjectValueNode parent, int index, ObjectValueNode node)
		{
			var value = new MacObjectValueNode (parent, node);
			mapping[node] = value;

			if (index < parent.Children.Count)
				parent.Children.Insert (index, value);
			else
				parent.Children.Add (value);

			if (treeView.AllowExpanding) {
				foreach (var child in node.Children)
					Add (value, child);

				if (node.HasChildren && !node.ChildrenLoaded)
					Add (value, new LoadingObjectValueNode (node));
			}
		}

		void Remove (MacObjectValueNode node, List<MacObjectValueNode> removed)
		{
			foreach (var child in node.Children)
				Remove (child, removed);

			mapping.Remove (node.Target);
			node.Children.Clear ();
			removed.Add (node);
		}

		void RestoreExpandedState (ObjectValueNode node)
		{
			if (treeView.Controller.GetNodeWasExpandedAtLastCheckpoint (node)) {
				if (TryGetValue (node, out var item))
					treeView.ExpandItem (item);
			}
		}

		void RestoreExpandedState (IEnumerable<ObjectValueNode> nodes)
		{
			foreach (var node in nodes)
				RestoreExpandedState (node);
		}

		public void Replace (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			if (!TryGetValue (node, out var item))
				return;

			var parent = item.Parent;
			int index = -1;

			if (parent == null)
				return;

			for (int i = 0; i < parent.Children.Count; i++) {
				if (parent.Children[i] == item) {
					index = i;
					break;
				}
			}

			if (index == -1)
				return;

			var removed = new List<MacObjectValueNode> ();
			parent.Children.RemoveAt (index);
			Remove (item, removed);

			treeView.BeginUpdates ();

			try {
				var indexes = new NSIndexSet (index);

				if (parent.Target is RootObjectValueNode)
					treeView.RemoveItems (indexes, null, NSTableViewAnimation.None);
				else
					treeView.RemoveItems (indexes, parent, NSTableViewAnimation.None);

				if (replacementNodes.Length > 0) {
					for (int i = 0; i < replacementNodes.Length; i++)
						Insert (parent, index + i, replacementNodes[i]);

					var range = new NSRange (index, replacementNodes.Length);
					indexes = NSIndexSet.FromNSRange (range);

					if (parent.Target is RootObjectValueNode)
						treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
					else
						treeView.InsertItems (indexes, parent, NSTableViewAnimation.None);

					RestoreExpandedState (replacementNodes);
				}
			} finally {
				treeView.EndUpdates ();
			}

			for (int i = 0; i < removed.Count; i++)
				removed[i].Dispose ();
		}

		public void LoadChildren (ObjectValueNode node, int startIndex, int count)
		{
			if (!TryGetValue (node, out var parent))
				return;

			treeView.BeginUpdates ();

			try {
				int lastIndex = parent.Children.Count - 1;
				bool needShowMore = !node.ChildrenLoaded;
				bool haveShowMore = false;
				NSIndexSet indexes = null;
				NSRange range;

				if (lastIndex >= 0 && parent.Children[lastIndex].Target is ShowMoreValuesObjectValueNode)
					haveShowMore = true;

				if (startIndex < parent.Children.Count) {
					// Note: This can only happen if we have either a "Loading..." node or a "Show More" node.
					var removed = new List<MacObjectValueNode> ();
					int extra = parent.Children.Count - startIndex;

					if (lastIndex == 0 && parent.Children[0].Target is LoadingObjectValueNode) {
						// Remove the "Loading..." node
						indexes = NSIndexSet.FromIndex (0);
						Remove (parent.Children[0], removed);
						parent.Children.Clear ();
					} else if (haveShowMore && extra == 1) {
						// Only remove the "Show More" node if we don't need it anymore...
						if (!needShowMore) {
							indexes = NSIndexSet.FromIndex (lastIndex);
							Remove (parent.Children[lastIndex], removed);
							parent.Children.RemoveAt (lastIndex);
						}
					} else {
						// Unexpected, but let's try to deal with this...
						range = new NSRange (startIndex, extra);
						indexes = NSIndexSet.FromNSRange (range);

						for (int i = parent.Children.Count - 1; i >= startIndex; i--) {
							Remove (parent.Children[i], removed);
							parent.Children.RemoveAt (i);
						}

						haveShowMore = false;
					}

					if (indexes != null) {
						if (parent.Target is RootObjectValueNode)
							treeView.RemoveItems (indexes, null, NSTableViewAnimation.None);
						else
							treeView.RemoveItems (indexes, parent, NSTableViewAnimation.None);

						for (int i = 0; i < removed.Count; i++)
							removed[i].Dispose ();
					}
				}

				for (int i = startIndex; i < startIndex + count; i++)
					Insert (parent, i, node.Children[i]);

				// Add a "Show More" node only if we need one and don't already have one.
				if (needShowMore && !haveShowMore) {
					Add (parent, new ShowMoreValuesObjectValueNode (node));
					count++;
				}

				range = new NSRange (startIndex, count);
				indexes = NSIndexSet.FromNSRange (range);

				if (parent.Target is RootObjectValueNode)
					treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
				else
					treeView.InsertItems (indexes, parent, NSTableViewAnimation.None);

				// if we loaded children and discovered that the node does not actually have any children,
				// update the node and reload the data.
				// TOOD: it would be nice to know this before the node is expanded so we don't see the "loading" node flash
				if (!node.HasChildren) {
					treeView.ReloadItem (parent);
				} else {
					RestoreExpandedState (node.Children);
				}
			} finally {
				treeView.EndUpdates ();
			}
		}

		public void Clear ()
		{
			int count = Root.Children.Count;

			if (AllowWatchExpressions)
				count--;

			if (count <= 0)
				return;

			var removed = new List<MacObjectValueNode> ();
			for (int i = count - 1; i >= 0; i--) {
				var child = Root.Children[i];
				Root.Children.RemoveAt (i);
				Remove (child, removed);
			}

			var range = new NSRange (0, count);
			var indexes = NSIndexSet.FromNSRange (range);

			treeView.BeginUpdates ();

			treeView.RemoveItems (indexes, null, NSTableViewAnimation.None);

			for (int i = 0; i < removed.Count; i++)
				removed[i].Dispose ();

			treeView.EndUpdates ();
		}

		public void Append (ObjectValueNode node)
		{
			int index;

			if (AllowWatchExpressions) {
				index = Root.Children.Count - 1;
				Core.LoggingService.LogInfo ("MacObjectValueTreeViewDataSource.Append: Inserting '{0}' at index {1}", node.Name, index);
				Insert (Root, index, node);
			} else {
				index = Root.Children.Count;
				Core.LoggingService.LogInfo ("MacObjectValueTreeViewDataSource.Append: Adding '{0}' at index {1}", node.Name, index);
				Add (Root, node);
			}

			treeView.BeginUpdates ();

			try {
				var indexes = NSIndexSet.FromIndex (index);

				treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
				RestoreExpandedState (node.Children);
			} finally {
				treeView.EndUpdates ();
			}
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

			treeView.BeginUpdates ();

			try {
				var range = new NSRange (index, nodes.Count);
				var indexes = NSIndexSet.FromNSRange (range);

				treeView.InsertItems (indexes, null, NSTableViewAnimation.None);
				RestoreExpandedState (nodes);
			} finally {
				treeView.EndUpdates ();
			}
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

			return node.Children[(int) childIndex];
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
