// TreeBuilder.cs
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
using System.Collections;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ExtensibleTreeView
	{
		class TreeBuilder: TreeNodeNavigator, ITreeBuilder
		{
			public TreeBuilder (ExtensibleTreeView pad) : base (pad)
			{
			}

			public TreeBuilder (ExtensibleTreeView pad, Gtk.TreeIter iter): base (pad, iter)
			{
			}
			
			protected override void EnsureFilled ()
			{
				if (!(bool) store.GetValue (currentIter, ExtensibleTreeView.FilledColumn))
					FillNode ();
			}
			
			public bool FillNode ()
			{
				store.SetValue (currentIter, ExtensibleTreeView.FilledColumn, true);
				
				Gtk.TreeIter child;
				if (store.IterChildren (out child, currentIter))
					store.Remove (ref child);
				
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, ExtensibleTreeView.BuilderChainColumn);
				object dataObject = store.GetValue (currentIter, ExtensibleTreeView.DataItemColumn);
				CreateChildren (chain, dataObject);
				return store.IterHasChild (currentIter);
			}
			
			public void UpdateAll ()
			{
				Update ();
				UpdateChildren ();
			}
			
			public void Update ()
			{
				object data = store.GetValue (currentIter, ExtensibleTreeView.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, ExtensibleTreeView.BuilderChainColumn);
				
				NodeAttributes ats = GetAttributes (chain, data);
				UpdateNode (chain, ats, data);
			}
			
			public void ResetState ()
			{
				Update ();
				RemoveChildren (currentIter);

				object data = store.GetValue (currentIter, ExtensibleTreeView.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, ExtensibleTreeView.BuilderChainColumn);

				if (!HasChildNodes (chain, data))
					FillNode ();
				else {
					RemoveChildren (currentIter);
					store.AppendNode (currentIter);	// Dummy node
					store.SetValue (currentIter, ExtensibleTreeView.FilledColumn, false);
				}
			}
			
			public void UpdateChildren ()
			{
				object data = store.GetValue (currentIter, ExtensibleTreeView.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, ExtensibleTreeView.BuilderChainColumn);
				
				if (!(bool) store.GetValue (currentIter, ExtensibleTreeView.FilledColumn)) {
					if (!HasChildNodes (chain, data))
						FillNode ();
					return;
				}
				
				NodeState ns = SaveState ();
				RestoreState (ns);
			}
			
			void RemoveChildren (Gtk.TreeIter it)
			{
				Gtk.TreeIter child;
				while (store.IterChildren (out child, it)) {
					RemoveChildren (child);
					object childData = store.GetValue (child, ExtensibleTreeView.DataItemColumn);
					if (childData != null)
						pad.UnregisterNode (childData, child, null);
					store.Remove (ref child);
				}
			}
			
			public void Remove ()
			{
				RemoveChildren (currentIter);
				object data = store.GetValue (currentIter, ExtensibleTreeView.DataItemColumn);
				pad.UnregisterNode (data, currentIter, null);
				Gtk.TreeIter it = currentIter;
				if (store.Remove (ref it) && !it.Equals (Gtk.TreeIter.Zero))
					MoveToIter (it);
			}
			
			public void Remove (bool moveToParent)
			{
				Gtk.TreeIter parent;
				store.IterParent (out parent, currentIter);

				Remove ();

				if (moveToParent) {
					MoveToIter (parent);
				}
			}
			
			public void AddChild (object dataObject)
			{
				AddChild (dataObject, false);
			}
			
			NodeAttributes GetAttributes (NodeBuilder[] chain, object dataObject)
			{
				Gtk.TreeIter oldIter = currentIter;
				NodeAttributes ats = NodeAttributes.None;
				
				foreach (NodeBuilder nb in chain) {
					try {
						nb.GetNodeAttributes (this, dataObject, ref ats);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					MoveToIter (oldIter);
				}
				return ats;
			}
			
			public void AddChild (object dataObject, bool moveToChild)
			{
				if (dataObject == null) throw new ArgumentNullException ("dataObject");
				
				NodeBuilder[] chain = pad.GetBuilderChain (dataObject.GetType ());
				if (chain == null) return;
				
				Gtk.TreeIter oldIter = currentIter;
				NodeAttributes ats = GetAttributes (chain, dataObject);
				if ((ats & NodeAttributes.Hidden) != 0)
					return;
				
				Gtk.TreeIter it;
				if (!currentIter.Equals (Gtk.TreeIter.Zero)) {
					if (!Filled) return;
					it = store.AppendValues (currentIter, "", null, null, dataObject, chain, false);
				}
				else
					it = store.AppendValues ("", null, null, dataObject, chain, false);
				
				pad.RegisterNode (it, dataObject, chain);
				
				BuildNode (it, chain, ats, dataObject);
				if (moveToChild)
					MoveToIter (it);
				else
					MoveToIter (oldIter);

				pad.NotifyInserted (it, dataObject);
			}
			
			void BuildNode (Gtk.TreeIter it, NodeBuilder[] chain, NodeAttributes ats, object dataObject)
			{
				Gtk.TreeIter oldIter = currentIter;
				object oldItem = DataItem;

				InitIter (it, dataObject);
				
				// It is *critical* that we set this first. We will
				// sort after this call, so we must give as much info
				// to the sort function as possible.
				store.SetValue (it, ExtensibleTreeView.DataItemColumn, dataObject);
				store.SetValue (it, ExtensibleTreeView.BuilderChainColumn, chain);
				
				UpdateNode (chain, ats, dataObject);
				
				bool hasChildren = HasChildNodes (chain, dataObject);
				store.SetValue (currentIter, ExtensibleTreeView.FilledColumn, !hasChildren);
				
				if (hasChildren)
					store.AppendNode (currentIter);	// Dummy node

				InitIter (oldIter, oldItem);
			}
			
			bool HasChildNodes (NodeBuilder[] chain, object dataObject)
			{
				Gtk.TreeIter citer = currentIter;
				foreach (NodeBuilder nb in chain) {
					try {
						bool res = nb.HasChildNodes (this, dataObject);
						if (res) return true;
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					} finally {
						MoveToIter (citer);
					}
				}
				return false;
			}
			
			void UpdateNode (NodeBuilder[] chain, NodeAttributes ats, object dataObject)
			{
				Gdk.Pixbuf icon = null;
				Gdk.Pixbuf closedIcon = null;
				string text = string.Empty;
				Gtk.TreeIter citer = currentIter;
				
				foreach (NodeBuilder builder in chain) {
					try {
						builder.BuildNode (this, dataObject, ref text, ref icon, ref closedIcon);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					MoveToIter (citer);
				}
					
				if (closedIcon == null) closedIcon = icon;
				
				if (pad.CopyObjects != null && ((IList)pad.CopyObjects).Contains (dataObject) && pad.CurrentTransferOperation == DragOperation.Move) {
					Gdk.Pixbuf gicon = pad.BuilderContext.GetComposedIcon (icon, "fade");
					if (gicon == null) {
						gicon = Services.Icons.MakeTransparent (icon, 0.5);
						pad.BuilderContext.CacheComposedIcon (icon, "fade", gicon);
					}
					icon = gicon;
					gicon = pad.BuilderContext.GetComposedIcon (closedIcon, "fade");
					if (gicon == null) {
						gicon = Services.Icons.MakeTransparent (closedIcon, 0.5);
						pad.BuilderContext.CacheComposedIcon (closedIcon, "fade", gicon);
					}
					closedIcon = gicon;
				}
				
				SetNodeInfo (currentIter, ats, text, icon, closedIcon);
			}
			
			void SetNodeInfo (Gtk.TreeIter it, NodeAttributes ats, string text, Gdk.Pixbuf icon, Gdk.Pixbuf closedIcon)
			{
				store.SetValue (it, ExtensibleTreeView.TextColumn, text);
				if (icon != null) store.SetValue (it, ExtensibleTreeView.OpenIconColumn, icon);
				if (closedIcon != null) store.SetValue (it, ExtensibleTreeView.ClosedIconColumn, closedIcon);
				pad.Tree.QueueDraw ();
			}

			void CreateChildren (NodeBuilder[] chain, object dataObject)
			{
				Gtk.TreeIter it = currentIter;
				foreach (NodeBuilder builder in chain) {
					try {
						builder.BuildChildNodes (this, dataObject);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					MoveToIter (it);
				}
			}
		}
	}
}
