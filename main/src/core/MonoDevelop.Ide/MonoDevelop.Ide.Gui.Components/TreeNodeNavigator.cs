// TreeNodeNavigator.cs
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

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ExtensibleTreeView
	{
		class TreeNodeNavigator: ITreeNavigator
		{
			protected ExtensibleTreeView pad;
			protected Gtk.TreeView tree;
			protected Gtk.TreeStore store;
			Gtk.TreeIter currentNavIter;
			object dataItem;
			
			public TreeNodeNavigator (ExtensibleTreeView pad): this (pad, Gtk.TreeIter.Zero)
			{
			}
			
			public TreeNodeNavigator (ExtensibleTreeView pad, Gtk.TreeIter iter)
			{
				this.pad = pad;
				tree = pad.Tree;
				store = pad.Store;
				MoveToIter (iter);
			}
			
			protected Gtk.TreeIter currentIter {
				get { return currentNavIter; }
			}
			
			public ITreeNavigator Clone ()
			{
				return new TreeNodeNavigator (pad, currentIter);
			}
			
			void AssertIsValid ()
			{
				if (!pad.sorting && !currentIter.Equals (Gtk.TreeIter.Zero) && !store.IterIsValid (currentIter)) {
					if (dataItem == null || !MoveToObject (dataItem))
						throw new InvalidOperationException ("Tree iterator has been invalidated.");
				}
			}
			
			protected void InitIter (Gtk.TreeIter it, object dataObject)
			{
				currentNavIter = it;
				dataItem = dataObject;
			}
	
			
			public object DataItem {
				get {
					return dataItem;
				}
			}
			
			public TypeNodeBuilder TypeNodeBuilder {
				get {
					return pad.GetTypeNodeBuilder (CurrentPosition._iter);
				}
			}
			
			internal NodeBuilder[] NodeBuilderChain {
				get {
					AssertIsValid ();
					NodeBuilder[] chain = (NodeBuilder[]) GetStoreValue (ExtensibleTreeView.BuilderChainColumn);
					if (chain != null)
						return chain;
					else
						return new NodeBuilder [0];
				}
			}
			
			public bool Selected {
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
			
			public NodePosition CurrentPosition {
				get {
					AssertIsValid ();
					NodePosition pos = new NodePosition ();
					pos._iter = currentIter;
					return pos;
				}
			}
			
			public bool MoveToPosition (NodePosition position)
			{
				MoveToIter ((Gtk.TreeIter) position._iter);
				return true;
			}
			
			internal void MoveToIter (Gtk.TreeIter iter)
			{
				currentNavIter = iter;
				if (!iter.Equals (Gtk.TreeIter.Zero))
					dataItem = GetStoreValue (ExtensibleTreeView.DataItemColumn);
				else
					dataItem = null;
			}
			
			public bool MoveToRoot ()
			{
				AssertIsValid ();
				Gtk.TreeIter it;
				if (!store.GetIterFirst (out it))
					return false;
				
				MoveToIter (it);
				return true;
			}
		
			public bool MoveToObject (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return false;
				MoveToIter (iter);
				return true;
			}
		
			public bool MoveToNextObject ()
			{
				object dataItem = DataItem;
				if (dataItem == null)
					return false;
				if (currentIter.Equals (Gtk.TreeIter.Zero))
					return false;
				Gtk.TreeIter it = currentIter;
				if (!pad.GetNextNode (dataItem, ref it))
					return false;
				
				MoveToIter (it);
				return true;
			}
		
			public bool MoveToParent ()
			{
				AssertIsValid ();
				Gtk.TreeIter it;
				if (store.IterParent (out it, currentIter)) {
					MoveToIter (it);
					return true;
				} else
					return false;
			}
			
			public bool MoveToParent (Type dataType)
			{
				AssertIsValid ();
				Gtk.TreeIter newIter = currentIter;
				while (store.IterParent (out newIter, newIter)) {
					object data = store.GetValue (newIter, ExtensibleTreeView.DataItemColumn);
					if (dataType.IsInstanceOfType (data)) {
						MoveToIter (newIter);
						return true;
					}
				}
				return false;
			}
			
			public bool MoveToFirstChild ()
			{
				EnsureFilled ();
				Gtk.TreeIter it;
				if (!store.IterChildren (out it, currentIter))
					return false;
				
				MoveToIter (it);
				return true;
			}
			
			public bool MoveNext ()
			{
				AssertIsValid ();
				Gtk.TreeIter it = currentIter;
				if (!store.IterNext (ref it))
					return false;
				MoveToIter (it);
				return true;
			}
			
			public bool HasChild (string name, Type dataType)
			{
				if (MoveToChild (name, dataType)) {
					MoveToParent ();
					return true;
				} else
					return false;
			}
			
			public bool HasChildren ()
			{
				EnsureFilled ();
				Gtk.TreeIter it;
				return store.IterChildren (out it, currentIter);
			}
		
			public bool FindChild (object dataObject)
			{
				return FindChild (dataObject, false);
			}
			
			public bool FindChild (object dataObject, bool recursive)
			{
				AssertIsValid ();
				object it;
				
				if (!pad.NodeHash.TryGetValue (dataObject, out it))
					return false;
				else if (it is Gtk.TreeIter) {
					if (IsChildIter (currentIter, (Gtk.TreeIter)it, recursive)) {
						MoveToIter ((Gtk.TreeIter)it);
						return true;
					} else
						return false;
				} else {
					foreach (Gtk.TreeIter cit in (Gtk.TreeIter[])it) {
						if (IsChildIter (currentIter, cit, recursive)) {
							MoveToIter ((Gtk.TreeIter)cit);
							return true;
						}
					}
					return false;
				}
			}
			
			bool IsChildIter (Gtk.TreeIter pit, Gtk.TreeIter cit, bool recursive)
			{
				Gtk.TreePath pitPath = tree.Model.GetPath (pit);
				Gtk.TreePath citPath = tree.Model.GetPath (cit);

				if (!citPath.Up ())
					return false;

				if (citPath.Equals(pitPath))
					return true;

				return recursive && pitPath.IsAncestor (citPath);
			}
			
			public bool MoveToChild (string name, Type dataType)
			{
				EnsureFilled ();
				Gtk.TreeIter oldIter = currentIter;
	
				if (!MoveToFirstChild ()) {
					MoveToIter (oldIter);
					return false;
				}
	
				do {
					if (name == NodeName)
						return true;
				} while (MoveNext ());
	
				MoveToIter (oldIter);
				return false;
			}
			
			public bool Expanded {
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
	
			public ITreeOptions Options {
				get { return pad.globalOptions; }
			}
			
			public NodeState SaveState ()
			{
				AssertIsValid ();
				return NodeState.SaveState (pad, this);
			}
			
			public void RestoreState (NodeState state)
			{
				AssertIsValid ();
				NodeState.RestoreState (pad, this, state);
			}
		
			public void Refresh ()
			{
				ITreeBuilder builder = new TreeBuilder (pad, currentIter);
				builder.UpdateAll ();
			}
			
			public void ExpandToNode ()
			{
				Gtk.TreeIter it;
				AssertIsValid ();
				if (store.IterParent (out it, currentIter)) {
					Gtk.TreePath path = store.GetPath (it);
					tree.ExpandToPath (path);
				}
			}
			
			public string NodeName {
				get {
					object data = DataItem;
					NodeBuilder[] chain = BuilderChain;
					if (chain != null && chain.Length > 0) return ((TypeNodeBuilder)chain[0]).GetNodeName (this, data);
					else return GetStoreValue (ExtensibleTreeView.TextColumn) as string;
				}
			}
			
			public NodeBuilder[] BuilderChain {
				get {
					AssertIsValid ();
					return (NodeBuilder[]) GetStoreValue (ExtensibleTreeView.BuilderChainColumn);
				}
			}
			
			public object GetParentDataItem (Type type, bool includeCurrent)
			{
				if (includeCurrent && type.IsInstanceOfType (DataItem))
					return DataItem;
	
				Gtk.TreeIter it = currentIter;
				while (store.IterParent (out it, it)) {
					object data = store.GetValue (it, ExtensibleTreeView.DataItemColumn);
					if (type.IsInstanceOfType (data))
						return data;
				}
				return null;
			}
		
			public virtual void EnsureFilled ()
			{
				AssertIsValid ();
				if (!(bool) GetStoreValue (ExtensibleTreeView.FilledColumn))
					new TreeBuilder (pad, currentIter).FillNode ();
			}
			
			public bool Filled {
				get {
					AssertIsValid ();
					return (bool) GetStoreValue (ExtensibleTreeView.FilledColumn);
				}
			}

			object GetStoreValue (int column)
			{
				return store.GetValue (currentIter, column);
			}
		}
	}
}
