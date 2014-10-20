//
// GtkTreeNodeNavigator.cs
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
using System.Collections;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class GtkTreeNodeNavigator: TreeNodeNavigator
	{
		ExtensibleTreeGtkBackend pad;
		Gtk.TreeView tree;
		Gtk.TreeStore store;
		Gtk.TreeIter currentIter;
		object dataItem;
		GtkNodePosition position;

		public GtkTreeNodeNavigator (ExtensibleTreeGtkBackend backend) : this (backend, Gtk.TreeIter.Zero)
		{
		}

		public GtkTreeNodeNavigator (ExtensibleTreeGtkBackend backend, Gtk.TreeIter it) : base (backend.Frontend)
		{
			this.pad = backend;
			tree = pad.Tree;
			store = pad.Store;
			MoveToIter (it);
		}

		protected override void AssertIsValid ()
		{
			if (!pad.sorting && !currentIter.Equals (Gtk.TreeIter.Zero) && !store.IterIsValid (currentIter)) {
				if (dataItem == null || !MoveToObject (dataItem))
					throw new InvalidOperationException ("Tree iterator has been invalidated.");
			}
		}

		void InitIter (Gtk.TreeIter it, object dataObject)
		{
			currentIter = it;
			position = null;
			dataItem = dataObject;
			OnPositionChanged ();
		}

		public void MoveToIter (Gtk.TreeIter iter)
		{
			currentIter = iter;
			position = null;
			if (!iter.Equals (Gtk.TreeIter.Zero))
				dataItem = GetStoreValue (ExtensibleTreeGtkBackend.DataItemColumn);
			else
				dataItem = null;
			OnPositionChanged ();
		}

		public override ITreeNavigator Clone ()
		{
			return new GtkTreeNodeNavigator (pad, currentIter);
		}

		public override bool MoveToPosition (NodePosition position)
		{
			MoveToIter (position.GetIter ());
			return true;
		}

		public override bool MoveToRoot ()
		{
			AssertIsValid ();
			Gtk.TreeIter it;
			if (!store.GetIterFirst (out it))
				return false;

			MoveToIter (it);
			return true;
		}

		public override bool MoveToParent ()
		{
			AssertIsValid ();
			Gtk.TreeIter it;
			if (store.IterParent (out it, currentIter)) {
				MoveToIter (it);
				return true;
			} else
				return false;
		}

		public override bool MoveToParent (Type dataType)
		{
			AssertIsValid ();
			Gtk.TreeIter newIter = currentIter;
			while (store.IterParent (out newIter, newIter)) {
				object data = store.GetValue (newIter, ExtensibleTreeGtkBackend.DataItemColumn);
				if (dataType.IsInstanceOfType (data)) {
					MoveToIter (newIter);
					return true;
				}
			}
			return false;
		}

		public override bool MoveToFirstChild ()
		{
			EnsureFilled ();
			Gtk.TreeIter it;
			if (!store.IterChildren (out it, currentIter))
				return false;

			MoveToIter (it);
			return true;
		}

		public override bool MoveNext ()
		{
			AssertIsValid ();
			Gtk.TreeIter it = currentIter;
			if (!store.IterNext (ref it))
				return false;
			MoveToIter (it);
			return true;
		}

		public override bool HasChildren ()
		{
			EnsureFilled ();
			Gtk.TreeIter it;
			return store.IterChildren (out it, currentIter);
		}

		public override void ExpandToNode ()
		{
			Gtk.TreeIter it;
			AssertIsValid ();
			if (store.IterParent (out it, currentIter)) {
				Gtk.TreePath path = store.GetPath (it);
				tree.ExpandToPath (path);
			}
		}

		public override object GetParentDataItem (Type type, bool includeCurrent)
		{
			if (includeCurrent && type.IsInstanceOfType (DataItem))
				return DataItem;

			Gtk.TreeIter it = currentIter;
			while (store.IterParent (out it, it)) {
				object data = store.GetValue (it, ExtensibleTreeGtkBackend.DataItemColumn);
				if (type.IsInstanceOfType (data))
					return data;
			}
			return null;
		}

		protected override void OnUpdate (NodeAttributes ats, NodeInfo nodeInfo)
		{
			bool isNew = false;

			var ni = (NodeInfo)store.GetValue (currentIter, ExtensibleTreeView.NodeInfoColumn);
			if (ni == null || ni.IsShared) {
				ni = new NodeInfo ();
				isNew = true;
			}
			else
				ni.Reset ();

			if (isNew)
				store.SetValue (currentIter, ExtensibleTreeView.NodeInfoColumn, ni);
			else
				store.EmitRowChanged (store.GetPath (currentIter), currentIter);
		}

		void SetNodeInfo (Gtk.TreeIter it, NodeInfo nodeInfo)
		{
			store.SetValue (it, ExtensibleTreeGtkBackend.NodeInfoColumn, nodeInfo);
			pad.Tree.QueueDraw ();
		}

		public override void ResetChildren ()
		{
			pad.RemoveChildren (currentIter);
			store.AppendNode (currentIter);	// Dummy node
			store.SetValue (currentIter, ExtensibleTreeGtkBackend.FilledColumn, false);
		}

		public override void Remove (bool moveToParent)
		{
			if (moveToParent) {
				Gtk.TreeIter parent;
				store.IterParent (out parent, currentIter);
				Remove ();
				MoveToIter (parent);
			} else
				Remove ();
		}

		protected override void PrepareBulkChildrenAdd ()
		{
			pad.Tree.FreezeChildNotify ();
			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (NullSortFunc);
		}

		protected override void FinishBulkChildrenAdd ()
		{
			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (pad.CompareNodes);
			pad.Tree.ThawChildNotify ();
		}

		static int NullSortFunc (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			return 0;
		}

		void BuildNode (Gtk.TreeIter it, NodeBuilder[] chain, NodeAttributes ats, object dataObject, NodeInfo nodeInfo, bool filled)
		{
			Gtk.TreeIter oldIter = currentIter;
			object oldItem = DataItem;

			InitIter (it, dataObject);

			// It is *critical* that we set this first. We will
			// sort after this call, so we must give as much info
			// to the sort function as possible.
			store.SetValue (it, ExtensibleTreeGtkBackend.DataItemColumn, dataObject);
			store.SetValue (it, ExtensibleTreeGtkBackend.BuilderChainColumn, chain);

			SetNodeInfo (it, nodeInfo);

			store.SetValue (currentIter, ExtensibleTreeGtkBackend.FilledColumn, filled);

			if (!filled)
				store.AppendNode (currentIter);	// Dummy node

			InitIter (oldIter, oldItem);
		}

		protected override void OnAddChild (object dataObject, NodeBuilder[] chain, NodeAttributes ats, NodeInfo nodeInfo, bool filled, bool isRoot)
		{
			Gtk.TreeIter it;
			if (!isRoot)
				it = store.AppendValues (currentIter, nodeInfo, dataObject, chain, filled, false);
			else
				it = store.AppendValues (nodeInfo, dataObject, chain, filled, false);

			BuildNode (it, chain, ats, dataObject, nodeInfo, filled);
			InitIter (it, dataObject);
		}

		public override object DataItem {
			get {
				return dataItem;
			}
		}

		public override bool Selected {
			get {
				AssertIsValid ();
				return tree.Selection.IterIsSelected (currentIter);
			}
			set {
				AssertIsValid ();
				if (value != Selected) {
					ExpandToNode ();
					try {
						tree.Selection.SelectIter (currentIter);
						tree.SetCursor (store.GetPath (currentIter), pad.CompleteColumn, false);
					} catch (Exception) {}
					//						tree.ScrollToCell (store.GetPath (currentIter), null, false, 0, 0);
				}
			}
		}

		public override NodePosition CurrentPosition {
			get {
				AssertIsValid ();
				if (position == null) {
					position = new GtkNodePosition (store, currentIter);
				}
				return position;
			}
		}

		public override bool Expanded {
			get {
				AssertIsValid ();
				return tree.GetRowExpanded (store.GetPath (currentIter));
			}
			set {
				AssertIsValid ();
				if (value && !Expanded) {
					Gtk.TreePath path = store.GetPath (currentIter);
					tree.ExpandRow (path, false);
				}
				else if (!value && Expanded) {
					Gtk.TreePath path = store.GetPath (currentIter);
					tree.CollapseRow (path);
				}
			}
		}

		protected override string StoredNodeName {
			get {
				return GetStoreNodeInfo ().Label;
			}
		}

		public override NodeBuilder[] NodeBuilderChain {
			get {
				AssertIsValid ();
				return (NodeBuilder[]) GetStoreValue (ExtensibleTreeGtkBackend.BuilderChainColumn);
			}
		}

		public override bool Filled {
			get {
				AssertIsValid ();
				return (bool) GetStoreValue (ExtensibleTreeGtkBackend.FilledColumn);
			}
		}

		public override void FillNode ()
		{
			store.SetValue (currentIter, ExtensibleTreeGtkBackend.FilledColumn, true);
			pad.RemoveChildren (currentIter);

			object dataObject = store.GetValue (currentIter, ExtensibleTreeGtkBackend.DataItemColumn);
			CreateChildren (dataObject);
		}

		object GetStoreValue (int column)
		{
			return store.GetValue (currentIter, column);
		}

		NodeInfo GetStoreNodeInfo ()
		{
			return (NodeInfo) store.GetValue (currentIter, 0);
		}
	}
}

