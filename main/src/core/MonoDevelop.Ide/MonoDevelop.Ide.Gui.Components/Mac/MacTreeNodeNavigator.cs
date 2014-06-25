//
// MacTreeNodeNavigator.cs
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

#if MAC

using System;
using AppKit;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class MacTreeNodeNavigator: TreeNodeNavigator
	{
		TreeSource source;
		NSOutlineView tree;
		MacNodePosition currentPosition;
		MacNodePosition immutableCurrentPosition;

		public MacTreeNodeNavigator (IExtensibleTreeViewFrontend frontend, NSOutlineView tree, TreeSource source, MacNodePosition pos): base (frontend)
		{
			this.source = source;
			this.tree = tree;
			currentPosition = pos ?? new MacNodePosition ();
		}

		bool NotifyPositionChanged ()
		{
			immutableCurrentPosition = null;
			OnPositionChanged ();
			return true;
		}

		#region implemented abstract members of TreeNodeNavigator

		public override ITreeNavigator Clone ()
		{
			return new MacTreeNodeNavigator (Frontend, tree, source, currentPosition != null ? currentPosition.Clone () : null);
		}

		public override bool MoveToPosition (NodePosition position)
		{
			currentPosition = (MacNodePosition) position;
			NotifyPositionChanged ();
			return true;
		}

		public override bool MoveToRoot ()
		{
			return source.MoveToRoot (ref currentPosition) && NotifyPositionChanged ();
		}

		public override bool MoveToParent ()
		{
			return source.MoveToParent (ref currentPosition) && NotifyPositionChanged ();
		}

		public override bool MoveToFirstChild ()
		{
			EnsureFilled ();
			return source.MoveToFirstChild (ref currentPosition) && NotifyPositionChanged ();
		}

		public override bool MoveNext ()
		{
			return source.MoveNext (ref currentPosition) && NotifyPositionChanged ();
		}

		public override bool HasChildren ()
		{
			return source.HasChildren (currentPosition);
		}

		public override void ExpandToNode ()
		{
			if (currentPosition == null)
				return;
			var pos = currentPosition.Clone ();
			while (source.MoveToParent (ref pos))
				tree.ExpandItem (source.GetItem (pos));
		}

		public override object GetParentDataItem (Type type, bool includeCurrent)
		{
			if (currentPosition == null)
				return null;

			if (includeCurrent && type.IsInstanceOfType (DataItem))
				return DataItem;

			var pos = currentPosition.Clone ();
			while (source.MoveToParent (ref pos)) {
				var node = source.GetItem (pos);
				if (type.IsInstanceOfType (node.DataItem))
					return node.DataItem;
			}
			return null;
		}

		public override void FillNode ()
		{
			var node = source.ClearChildren (currentPosition);
			CreateChildren (node.DataItem);
		}

		protected override void OnUpdate (NodeAttributes ats, NodeInfo nodeInfo)
		{
			var item = source.GetItem (currentPosition);
			item.LoadFrom (nodeInfo);
		}

		public override void Remove (bool moveToParent)
		{
			if (moveToParent) {
				var toDelete = (MacNodePosition) CurrentPosition;
				if (moveToParent) {
					if (!source.MoveToParent (ref currentPosition))
						currentPosition.Invalidate ();
				}
				RemoveItem (ref toDelete);
			} else {
				RemoveItem (ref currentPosition);
				currentPosition.Invalidate ();
			}
			NotifyPositionChanged ();
		}

		protected override void OnAddChild (object dataObject, NodeBuilder[] chain, NodeAttributes ats, NodeInfo nodeInfo, bool filled, bool isRoot)
		{
			var item = source.AddChild (ref currentPosition);
			item.NodeBuilderChain = chain;
			item.DataItem = dataObject;
			item.Filled = filled;
			item.LoadFrom (nodeInfo);
			NotifyPositionChanged ();
		}

		public override void ResetChildren ()
		{
			var it = source.GetItem (currentPosition);
			RemoveChildren (it);
			source.ResetChildren (currentPosition);
		}

		void RemoveItem (ref MacNodePosition pos)
		{
			var it = source.GetItem (pos);
			RemoveChildren (it);
			Frontend.UnregisterNode (it.DataItem, it.Position, it.NodeBuilderChain);
			source.Remove (ref pos);
		}

		void RemoveChildren (TreeItem it)
		{
			if (it.Children != null) {
				foreach (var c in it.Children) {
					RemoveChildren (c);
					Frontend.UnregisterNode (c.DataItem, c.Position, c.NodeBuilderChain);
				}
			}
		}

		public override object DataItem {
			get {
				return source.GetItem (currentPosition).DataItem;
			}
		}

		public override bool Selected {
			get {
				return tree.IsRowSelected (tree.RowForItem (source.GetItem (currentPosition)));
			}
			set {
				if (value)
					tree.SelectRow ((int)tree.RowForItem (source.GetItem (currentPosition)), false);
				else
					tree.DeselectRow (tree.RowForItem (source.GetItem (currentPosition)));
			}
		}

		public override NodePosition CurrentPosition {
			get {
				if (currentPosition == null)
					return null;
				if (currentPosition.Frozen)
					return currentPosition;
				if (immutableCurrentPosition != null)
					return immutableCurrentPosition;
				currentPosition.Frozen = true;
				return immutableCurrentPosition = currentPosition;
			}
		}

		public override bool Expanded {
			get {
				var node = source.GetItem (currentPosition);
				return tree.IsItemExpanded (node);
			}
			set {
				var node = source.GetItem (currentPosition);
				if (value)
					tree.ExpandItem (node);
				else
					tree.CollapseItem (node);
			}
		}

		protected override string StoredNodeName {
			get {
				var node = source.GetItem (currentPosition);
				return node.Label;
			}
		}

		public override NodeBuilder[] NodeBuilderChain {
			get {
				var node = source.GetItem (currentPosition);
				return node.NodeBuilderChain;
			}
		}

		public override bool Filled {
			get {
				var node = source.GetItem (currentPosition);
				return node.Filled;
			}
		}

		#endregion
	}
}

#endif