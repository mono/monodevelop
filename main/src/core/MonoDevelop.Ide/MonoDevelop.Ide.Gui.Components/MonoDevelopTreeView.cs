//
// MonoDevelopTreeView.cs
//
// Author:
//   Lluis Sanchez Gual
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;
using System.Resources;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.Ide.Gui.Components
{
	public class MonoDevelopTreeView : Gtk.ScrolledWindow
	{
		internal const int TextColumn         = 0;
		internal const int OpenIconColumn     = 1;
		internal const int ClosedIconColumn   = 2;
		internal const int DataItemColumn     = 3;
		internal const int BuilderChainColumn = 4;
		internal const int FilledColumn       = 5;
		
		NodeBuilder[] builders;
		Hashtable builderChains = new Hashtable ();
		Hashtable nodeHash = new Hashtable ();
		
		Gtk.TreeView tree = new Gtk.TreeView ();
		Gtk.TreeStore store;
		internal Gtk.TreeViewColumn complete_column;
		internal Gtk.CellRendererText text_render;
		TreeBuilderContext builderContext;
		Hashtable callbacks = new Hashtable ();
		bool editingText = false;
		
		TreePadOption[] options;
		TreeOptions globalOptions;
		Hashtable nodeOptions = new Hashtable ();
		
		TreeNodeNavigator workNode;
		TreeNodeNavigator compareNode1;
		TreeNodeNavigator compareNode2;
		internal bool sorting;
		
		object[] dragObjects;
		object[] copyObjects;
		DragOperation currentTransferOperation;
		
		private static Gtk.TargetEntry [] target_table = new Gtk.TargetEntry [] {
			new Gtk.TargetEntry ("text/uri-list", 0, 11 ),
			new Gtk.TargetEntry ("text/plain", 0, 22),
			new Gtk.TargetEntry ("application/x-rootwindow-drop", 0, 33)
		};
	
		public Gtk.TreeStore Store {
			get {
				return this.store;
			}
		}
		
		public Gtk.TreeView Tree {
			get {
				return tree;
			}
		}
		
		public MonoDevelopTreeView ()
		{
		}
		
		public MonoDevelopTreeView (NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (builders, options);
		}
		
		void PropertyChanged (object sender, MonoDevelop.Core.PropertyChangedEventArgs prop)
		{
			string name;
			
			switch (prop.Key) {
			case "MonoDevelop.Core.Gui.Pads.UseCustomFont":
				name = tree.Style.FontDescription.ToString ();
				
				if ((bool) prop.NewValue)
					name = PropertyService.Get ("MonoDevelop.Core.Gui.Pads.CustomFont", name);
				
				text_render.FontDesc = Pango.FontDescription.FromString (name);
				tree.ColumnsAutosize ();
				break;
			case "MonoDevelop.Core.Gui.Pads.CustomFont":
				if (!(PropertyService.Get<bool> ("MonoDevelop.Core.Gui.Pads.UseCustomFont")))
					break;
				
				name = (string) prop.NewValue;
				text_render.FontDesc = Pango.FontDescription.FromString (name);
				tree.ColumnsAutosize ();
				break;
			}
		}
		
		public virtual void Initialize (NodeBuilder[] builders, TreePadOption[] options)
		{
			builderContext = new TreeBuilderContext (this);
		
			SetBuilders (builders, options);
			/*
			0 -- Text
			1 -- Icon (Open)
			2 -- Icon (Closed)
			3 -- Node Data
			4 -- Builder chain
			5 -- Expanded
			*/
			store = new Gtk.TreeStore (typeof (string), typeof (Gdk.Pixbuf), typeof (Gdk.Pixbuf), typeof (object), typeof (object), typeof(bool));
			tree.Model = store;
			tree.Selection.Mode = Gtk.SelectionMode.Multiple;

			tree.EnableModelDragDest (target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			Gtk.Drag.SourceSet (tree, Gdk.ModifierType.Button1Mask, target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			
			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (CompareNodes);
			store.SetSortColumnId (/* GTK_TREE_SORTABLE_DEFAULT_SORT_COLUMN_ID */ -1, Gtk.SortType.Ascending);
			
			tree.HeadersVisible = false;
			tree.SearchColumn = 0;
			tree.EnableSearch = true;
			complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "column";

			Gtk.CellRendererPixbuf pix_render = new Gtk.CellRendererPixbuf ();
			complete_column.PackStart (pix_render, false);
			complete_column.AddAttribute (pix_render, "pixbuf", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "pixbuf-expander-open", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "pixbuf-expander-closed", ClosedIconColumn);
			
			text_render = new Gtk.CellRendererText ();
			if (PropertyService.Get ("MonoDevelop.Core.Gui.Pads.UseCustomFont", false)) {
				string name = tree.Style.FontDescription.ToString ();
				name = PropertyService.Get ("MonoDevelop.Core.Gui.Pads.CustomFont", name);
				text_render.FontDesc = Pango.FontDescription.FromString (name);
			}
			text_render.Ypad = 1;
			PropertyService.PropertyChanged += new EventHandler<MonoDevelop.Core.PropertyChangedEventArgs> (PropertyChanged);
			text_render.Edited += new Gtk.EditedHandler (HandleOnEdit);
			text_render.EditingCanceled += new EventHandler (HandleOnEditCancelled);
			
			complete_column.PackStart (text_render, true);
			complete_column.AddAttribute (text_render, "markup", TextColumn);
			
			tree.AppendColumn (complete_column);
			
			tree.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			tree.RowActivated += new Gtk.RowActivatedHandler(OnNodeActivated);
			
			tree.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnButtonRelease);
			tree.ButtonPressEvent += new Gtk.ButtonPressEventHandler (OnButtonPress);
			
			tree.PopupMenu += new Gtk.PopupMenuHandler (OnPopupMenu);
			workNode = new TreeNodeNavigator (this);
			compareNode1 = new TreeNodeNavigator (this);
			compareNode2 = new TreeNodeNavigator (this);
			
			tree.DragBegin += new Gtk.DragBeginHandler (OnDragBegin);
			tree.DragDataReceived += new Gtk.DragDataReceivedHandler (OnDragDataReceived);
			tree.DragDrop += new Gtk.DragDropHandler (OnDragDrop);
			tree.DragEnd += new Gtk.DragEndHandler (OnDragEnd);
			tree.DragMotion += new Gtk.DragMotionHandler (OnDragMotion);
			
			tree.CursorChanged += new EventHandler (OnSelectionChanged);
			tree.KeyPressEvent += OnKeyPress;
				
			this.Add (tree);
			this.ShowAll ();
		}
		
		public void UpdateBuilders (NodeBuilder[] builders, TreePadOption[] options)
		{
			// Save the current state
			ITreeNavigator root = GetRootNode ();
			NodeState state = root != null ? root.SaveState () : null;
			object obj = root != null ? root.DataItem : null;
			
			Clear ();
			
			// Clean cached builder chains
			builderChains.Clear ();
			
			// Update the builders
			SetBuilders (builders, options);

			// Restore the this
			if (obj != null)
				LoadTree (obj);
			
			root = GetRootNode ();
			if (root != null && state != null)
				root.RestoreState (state);
		}
		
		void SetBuilders (NodeBuilder[] buildersArray, TreePadOption[] options)
		{
			// Create default options
			
			List<NodeBuilder> builders = new List<NodeBuilder> ();
			foreach (NodeBuilder nb in buildersArray) {
				if (!(nb is TreeViewItemBuilder))
					builders.Add (nb);
			}
			builders.Add (new TreeViewItemBuilder ());
			
			this.options = options;
			globalOptions = new TreeOptions ();
			foreach (TreePadOption op in options)
				globalOptions [op.Id] = op.DefaultValue;
				
			globalOptions.Pad = this;
			
			// Check that there is only one TypeNodeBuilder per type
			
			Hashtable bc = new Hashtable ();
			foreach (NodeBuilder nb in builders) {
				TypeNodeBuilder tnb = nb as TypeNodeBuilder;
				if (tnb != null) {
					TypeNodeBuilder other = (TypeNodeBuilder) bc [tnb.NodeDataType];
					if (other != null)
						throw new ApplicationException (string.Format ("The type node builder {0} can't be used in this context because the type {1} is already handled by {2}", nb.GetType(), tnb.NodeDataType, other.GetType()));
					bc [tnb.NodeDataType] = tnb;
				}
				else if (!(nb is NodeBuilderExtension))
					throw new InvalidOperationException (string.Format ("Invalid NodeBuilder type: {0}. NodeBuilders must inherit either from TypeNodeBuilder or NodeBuilderExtension", nb.GetType()));
			}
			
			NodeBuilders = builders.ToArray ();
			
			foreach (NodeBuilder nb in builders)
				nb.SetContext (builderContext);
		}

		void OnDragBegin (object o, Gtk.DragBeginArgs arg)
		{
			ITreeNavigator[] navs = GetSelectedNodes ();
			if (navs.Length == 0) return;
			dragObjects = new object [navs.Length];
			for (int n=0; n<navs.Length; n++)
				dragObjects [n] = navs [n].DataItem;
			Gdk.Pixbuf px = (Gdk.Pixbuf) store.GetValue (navs[0].CurrentPosition._iter, OpenIconColumn);
			Gtk.Drag.SetIconPixbuf (arg.Context, px, -10, -10);
		}
		
		void OnDragDataReceived (object o, Gtk.DragDataReceivedArgs args)
		{
			if (dragObjects != null) {
				bool res = CheckAndDrop (args.X, args.Y, true, args.Context, dragObjects);
				Gtk.Drag.Finish (args.Context, res, true, args.Time);
			} else {
				bool res = CheckAndDrop (args.X, args.Y, true, args.Context, new object[] { args.SelectionData });
//				string fullData = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);
				Gtk.Drag.Finish (args.Context, res, true, args.Time);
			}
		}
		
		void OnDragDrop (object o, Gtk.DragDropArgs args)
		{
			if (dragObjects != null) {
				bool res = CheckAndDrop (args.X, args.Y, true, args.Context, dragObjects);
				Gtk.Drag.Finish (args.Context, res, true, args.Time);
			}
		}
		
		void OnDragEnd (object o, Gtk.DragEndArgs args)
		{
			dragObjects = null;
		}
		
		[GLib.ConnectBefore]
		void OnDragMotion (object o, Gtk.DragMotionArgs args)
		{
			if (dragObjects != null) {
				if (!CheckAndDrop (args.X, args.Y, false, args.Context, dragObjects)) {
					Gdk.Drag.Status (args.Context, (Gdk.DragAction)0, args.Time);
					args.RetVal = true;
				}
			}
		}
		
		bool CheckAndDrop (int x, int y, bool drop, Gdk.DragContext ctx, object[] obj)
		{
			Gtk.TreePath path;
			Gtk.TreeViewDropPosition pos;
			if (!tree.GetDestRowAtPos (x, y, out path, out pos)) return false;
			
			Gtk.TreeIter iter;
			if (!store.GetIter (out iter, path)) return false;
			
			TreeNodeNavigator nav = new TreeNodeNavigator (this, iter);
			NodeBuilder[] chain = nav.BuilderChain;
			bool foundHandler = false;
			
			DragOperation oper = ctx.Action == Gdk.DragAction.Copy ? DragOperation.Copy : DragOperation.Move;
			
			foreach (NodeBuilder nb in chain) {
				try {
					NodeCommandHandler handler = nb.CommandHandler;
					handler.SetCurrentNode (nav);
					if (handler.CanDropMultipleNodes (obj, oper)) {
						foundHandler = true;
						if (drop)
							handler.OnMultipleNodeDrop (obj, oper);
					}
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
			return foundHandler;
		}


		public override void Dispose ()
		{
			Clear ();
			foreach (NodeBuilder nb in builders) {
				try {
					nb.Dispose ();
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
		}
		
		protected NodeBuilder[] NodeBuilders {
			get { return builders; }
			set { builders = value; }
		}

		public Gtk.TreeViewColumn CompleteColumn {
			get {
				return complete_column;
			}
		}

		public Hashtable NodeHash {
			get {
				return nodeHash;
			}
		}

		internal TreeBuilderContext BuilderContext {
			get {
				return builderContext;
			}
		}

		public object[] CopyObjects {
			get {
				return copyObjects;
			}
			set {
				copyObjects = value;
			}
		}

		public DragOperation CurrentTransferOperation {
			get {
				return currentTransferOperation;
			}
		}
		
		public void LoadTree (object nodeObject)
		{
			Clear ();
			TreeBuilder builder = new TreeBuilder (this);
			builder.AddChild (nodeObject, true);
			builder.Expanded = true;
		}
		
		public void AddChild (object nodeObject)
		{
			TreeBuilder builder = new TreeBuilder (this);
			builder.AddChild (nodeObject, true);
			builder.Expanded = true;
		}
		
		public void RemoveChild (object nodeObject)
		{
			TreeBuilder builder = new TreeBuilder (this);
			if (builder.MoveToObject (nodeObject))
				builder.Remove ();
		}
		
		public void Clear ()
		{
			copyObjects = dragObjects = null;
			
			object[] obs = new object [nodeHash.Count];
			nodeHash.Keys.CopyTo (obs, 0);
			
			foreach (object dataObject in obs)
				NotifyNodeRemoved (dataObject, null);
			
			nodeHash = new Hashtable ();
			nodeOptions = new Hashtable ();
			store.Clear ();
		}
		
		public ITreeNavigator GetSelectedNode ()
		{
			Gtk.TreePath[] sel = tree.Selection.GetSelectedRows ();
			if (sel.Length == 0)
				return null;
			Gtk.TreeIter iter;
			if (store.GetIter (out iter, sel[0]))
				return new TreeNodeNavigator (this, iter);
			else
				return null;
		}

		class SelectionGroup
		{
			public NodeBuilder[] BuilderChain;
			public List<ITreeNavigator> Nodes;
			public Gtk.TreeStore store;
			
			NodePosition[] savedPos;
			object[] dataItems;

			public object[] DataItems {
				get {
					if (dataItems == null) {
						dataItems = new object [Nodes.Count];
						for (int n=0; n<Nodes.Count; n++)
							dataItems [n] = Nodes [n].DataItem;
					}
					return dataItems;
				}
			}

			public void SavePositions ()
			{
				savedPos = new NodePosition [Nodes.Count];
				for (int n=0; n<Nodes.Count; n++)
					savedPos [n] = Nodes [n].CurrentPosition;
			}

			public bool RestorePositions ()
			{
				for (int n=0; n<Nodes.Count; n++) {
					if (store.IterIsValid (savedPos[n]._iter))
						Nodes [n].MoveToPosition (savedPos [n]);
					else
						return false;
				}
				return true;
			}
		}
		
		IEnumerable<SelectionGroup> GetSelectedNodesGrouped ()
		{
			Gtk.TreePath[] paths = tree.Selection.GetSelectedRows ();
			if (paths.Length == 0) {
				return new SelectionGroup [0];
			}
			if (paths.Length == 1) {
				Gtk.TreeIter it;
				store.GetIter (out it, paths [0]);
				SelectionGroup grp = new SelectionGroup ();
				TreeNodeNavigator nav = new TreeNodeNavigator (this, it);
				grp.BuilderChain = nav.BuilderChain;
				grp.Nodes = new List<ITreeNavigator> ();
				grp.Nodes.Add (nav);
				grp.store = store;
				return new SelectionGroup [] { grp };
			}
			
			Dictionary<NodeBuilder[], SelectionGroup> dict = new Dictionary<NodeBuilder[],SelectionGroup> ();
			for (int n=0; n<paths.Length; n++) {
				Gtk.TreeIter it;
				store.GetIter (out it, paths [n]);
				SelectionGroup grp;
				TreeNodeNavigator nav = new TreeNodeNavigator (this, it);
				if (!dict.TryGetValue (nav.BuilderChain, out grp)) {
					grp = new SelectionGroup ();
					grp.BuilderChain = nav.BuilderChain;
					grp.Nodes = new List<ITreeNavigator> ();
					grp.store = store;
					dict [nav.BuilderChain] = grp;
				}
				grp.Nodes.Add (nav);
			}
			return dict.Values;
		}

		public bool MultipleNodesSelected ()
		{
			return tree.Selection.GetSelectedRows ().Length > 1;
		}

		public ITreeNavigator[] GetSelectedNodes ()
		{
			Gtk.TreePath[] paths = tree.Selection.GetSelectedRows ();
			ITreeNavigator [] navs = new ITreeNavigator [paths.Length];
			for (int n=0; n<paths.Length; n++) {
				Gtk.TreeIter it;
				store.GetIter (out it, paths [n]);
				navs [n] = new TreeNodeNavigator (this, it);
			}
			return navs;
		}

		public ITreeNavigator GetNodeAtPosition (NodePosition position)
		{
			return new TreeNodeNavigator (this, position._iter);
		}
		
		public ITreeNavigator GetNodeAtObject (object dataObject)
		{
			return GetNodeAtObject (dataObject, false);
		}
		
		public ITreeNavigator GetNodeAtObject (object dataObject, bool createTreeBranch)
		{
			object it = nodeHash [dataObject];
			if (it == null) {
				if (createTreeBranch) {
					TypeNodeBuilder tnb = GetTypeNodeBuilder (dataObject.GetType());
					if (tnb == null) return null;
					
					object parent = tnb.GetParentObject (dataObject);
					if (parent == null) return null;
					
					ITreeNavigator pnav = GetNodeAtObject (parent, true);
					if (pnav == null) return null;
					
					pnav.MoveToFirstChild ();
					
					// The child should be now in the this. Try again.
					it = nodeHash [dataObject];
					if (it == null)
						return null;
				} else
					return null;
			}
			
			if (it is Gtk.TreeIter[])
				return new TreeNodeNavigator (this, ((Gtk.TreeIter[])it)[0]);
			else
				return new TreeNodeNavigator (this, (Gtk.TreeIter)it);
		}
		
		public ITreeNavigator GetRootNode ()
		{
			Gtk.TreeIter iter;
			if (!store.GetIterFirst (out iter)) return null;
			return new TreeNodeNavigator (this, iter);
		}
		
		public void AddNodeInsertCallback (object dataObject, TreeNodeCallback callback)
		{
			if (IsRegistered (dataObject)) {
				callback (GetNodeAtObject (dataObject));
				return;
			}
				
			ArrayList list = callbacks [dataObject] as ArrayList;
			if (list != null)
				list.Add (callback);
			else {
				list = new ArrayList ();
				list.Add (callback);
				callbacks [dataObject] = list;
			}
		}
		
		internal object GetNextCommandTarget ()
		{
			return null;
		}

		class MulticastNodeRouter: IMultiCastCommandRouter
		{
			ArrayList targets;

			public MulticastNodeRouter (ArrayList targets)
			{
				this.targets = targets;
			}
			
			public IEnumerable GetCommandTargets ()
			{
				return targets;
			}
		}

		internal object GetDelegatedCommandTarget ()
		{
			// If a node is being edited, don't delegate commands to the
			// node builders, since what's selected is not the node,
			// but the node label. In this way commands such as Delete
			// will be handled by the node Entry.
			if (editingText)
				return null;

			ArrayList targets = new ArrayList ();
			
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				if (chain.Length > 0) {
					ITreeNavigator[] nodes = grp.Nodes.ToArray ();
					NodeCommandTargetChain targetChain = null;
					NodeCommandTargetChain lastNode = null;
					foreach (NodeBuilder nb in chain) {
						NodeCommandTargetChain newNode = new NodeCommandTargetChain (nb.CommandHandler, nodes);
						if (lastNode == null)
							targetChain = lastNode = newNode;
						else {
							lastNode.Next = newNode;
							lastNode = newNode;
						}
					}

					if (targetChain != null)
						targets.Add (targetChain);
				}
			}
			if (targets.Count == 1)
				return targets [0];
			else if (targets.Count > 1)
				return new MulticastNodeRouter (targets);
			else
				return null;
		}

		[CommandHandler (ViewCommands.Open)]
		public virtual void ActivateCurrentItem ()
		{
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				grp.SavePositions ();
				foreach (NodeBuilder b in grp.BuilderChain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNodes (grp.Nodes.ToArray ());
					handler.ActivateItem ();
					if (!grp.RestorePositions ())
						break;
				}
			}
			OnCurrentItemActivated (EventArgs.Empty);
		}
		
		public virtual void DeleteCurrentItem ()
		{
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				grp.SavePositions ();
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNodes (grp.Nodes.ToArray ());
					if (handler.CanDeleteMultipleItems ()) {
						if (!grp.RestorePositions ())
							return;
						handler.DeleteMultipleItems ();
						// FIXME: fixes bug #396566, but it is not 100% correct
						// It can only be fully fixed if updates to the tree are delayed
						break;
					}
					if (!grp.RestorePositions ())
						return;
				}
			}
		}
		
		protected virtual bool CanDeleteCurrentItem ()
		{
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				grp.SavePositions ();
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNodes (grp.Nodes.ToArray ());
					if (handler.CanDeleteMultipleItems ())
						return true;
					if (!grp.RestorePositions ())
						return false;
				}
			}
			return false;
		}
		
		protected virtual void OnCurrentItemActivated (EventArgs args)
		{
			if (CurrentItemActivated != null)
				CurrentItemActivated (this, args);
		}
		
		public event EventHandler CurrentItemActivated;

		[CommandHandler (EditCommands.Copy)]
		public void CopyCurrentItem ()
		{
			CancelTransfer ();
			TransferCurrentItem (DragOperation.Copy);
		}

		[CommandHandler (EditCommands.Cut)]
		public void CutCurrentItem ()
		{
			CancelTransfer ();
			TransferCurrentItem (DragOperation.Move);
			
			if (copyObjects != null) {
				foreach (object ob in copyObjects) {
					TreeBuilder tb = new TreeBuilder (this);
					if (tb.MoveToObject (ob))
						tb.Update ();
				}
			}
		}
		
		[CommandUpdateHandler (EditCommands.Copy)]
		protected void UpdateCopyCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}
			info.Enabled = CanTransferCurrentItem (DragOperation.Copy);
		}

		[CommandUpdateHandler (EditCommands.Cut)]
		protected void UpdateCutCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}
			info.Enabled = CanTransferCurrentItem (DragOperation.Move);
		}
		
		void TransferCurrentItem (DragOperation oper)
		{
			foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
				NodeBuilder[] chain = grp.BuilderChain;
				grp.SavePositions ();
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNodes (grp.Nodes.ToArray ());
						if ((handler.CanDragNode () & oper) != 0) {
							grp.RestorePositions ();
							copyObjects = grp.DataItems;
							currentTransferOperation = oper;
							break;
						}
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					grp.RestorePositions ();
				}
			}
		}
		
		bool CanTransferCurrentItem (DragOperation oper)
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if ((handler.CanDragNode () & oper) != 0)
							return true;
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					node.MoveToPosition (pos);
				}
			}
			return false;
		}
		
		[CommandHandler (EditCommands.Paste)]
		public void PasteToCurrentItem ()
		{
			if (copyObjects == null) return;
			
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode (node);
					if (handler.CanDropMultipleNodes (copyObjects, currentTransferOperation)) {
						node.MoveToPosition (pos);
						handler.OnMultipleNodeDrop (copyObjects, currentTransferOperation);
					}
					node.MoveToPosition (pos);
				}
			}
			CancelTransfer ();
		}

		[CommandUpdateHandler (EditCommands.Paste)]
		protected void UpdatePasteToCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}
			
			if (copyObjects != null) {
				TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
				if (node != null) {
					NodeBuilder[] chain = node.NodeBuilderChain;
					NodePosition pos = node.CurrentPosition;
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if (handler.CanDropMultipleNodes (copyObjects, currentTransferOperation)) {
							info.Enabled = true;
							return;
						}
						node.MoveToPosition (pos);
					}
				}
			}
			info.Enabled = false;
		}

		void CancelTransfer ()
		{
			if (copyObjects != null) {
				object[] oldCopyObjects = copyObjects;
				copyObjects = null;
				if (currentTransferOperation == DragOperation.Move) {
					foreach (object ob in oldCopyObjects) {
						TreeBuilder tb = new TreeBuilder (this);
						if (tb.MoveToObject (ob))
							tb.Update ();
					}
				}
			}
		}

		public void StartLabelEditInternal()
		{
			if (editingText)
				return;

			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node == null)
				return;
			
			Gtk.TreeIter iter = node.CurrentPosition._iter;
			object dataObject = node.DataItem;
			NodeAttributes attributes = NodeAttributes.None;
			
			ITreeNavigator parentNode = node.Clone ();
			parentNode.MoveToParent ();
			NodePosition pos = parentNode.CurrentPosition;
			
			foreach (NodeBuilder b in node.NodeBuilderChain) {
				try {
					b.GetNodeAttributes (parentNode, dataObject, ref attributes);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
				parentNode.MoveToPosition (pos);
			}
			
			if ((attributes & NodeAttributes.AllowRename) == 0)
				return;

			node.ExpandToNode (); //make sure the parent of the node that is being edited is expanded
			
			store.SetValue (iter, MonoDevelopTreeView.TextColumn, node.NodeName);
			
			text_render.Editable = true;
			tree.SetCursor (store.GetPath (iter), complete_column, true);
			
			editingText = true;
		}

		void HandleOnEdit (object o, Gtk.EditedArgs e)
		{
			try {
				editingText = false;
				text_render.Editable = false;
				
				Gtk.TreeIter iter;
				if (!store.GetIterFromString (out iter, e.Path))
					throw new Exception("Error calculating iter for path " + e.Path);

				if (e.NewText != null && e.NewText.Length > 0) {
					ITreeNavigator nav = new TreeNodeNavigator (this, iter);
					NodePosition pos = nav.CurrentPosition;

					NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (iter, BuilderChainColumn);
					foreach (NodeBuilder b in chain) {
						try {
							NodeCommandHandler handler = b.CommandHandler;
							handler.SetCurrentNode (nav);
							handler.RenameItem (e.NewText);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
						nav.MoveToPosition (pos);
					}
				}
				
				// Get the iter again since the this node may have been replaced.
				if (!store.GetIterFromString (out iter, e.Path))
					return;

				ITreeBuilder builder = new TreeBuilder (this, iter);
				builder.Update ();
			}
			catch (Exception ex) {
				MessageService.ShowException (ex, "The item could not be renamed");
			}
		}
		
		void HandleOnEditCancelled (object s, EventArgs args)
		{
			editingText = false;
			text_render.Editable = false;
			
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node == null)
				return;
			
			// Restore the original node label
			Gtk.TreeIter iter = node.CurrentPosition._iter;
			ITreeBuilder builder = new TreeBuilder (this, iter);
			builder.Update ();
		}
		
		public NodeState SaveTreeState ()
		{
			ITreeNavigator root = GetRootNode ();
			if (root == null) 
				return null;

			return root.SaveState ();
		}
		
		public void RestoreTreeState (NodeState state)
		{
			ITreeNavigator nav = GetRootNode ();
			
			if (nav == null)
				return;
			
			if (state != null) {
				nav.RestoreState (state);
			}
		}
		
		TypeNodeBuilder GetTypeNodeBuilder (Type type)
		{
			NodeBuilder[] chain = GetBuilderChain (type);
			if (chain == null) return null;
			return (TypeNodeBuilder) chain [0];
		}
		
		public NodeBuilder[] GetBuilderChain (Type type)
		{
			NodeBuilder[] chain = builderChains [type] as NodeBuilder[];
			if (chain == null)
			{
				ArrayList list = new ArrayList ();
				
				// Find the most specific node builder type.
				TypeNodeBuilder bestTypeNodeBuilder = null;
				Type bestNodeType = null;
				
				foreach (NodeBuilder nb in builders) {
					if (nb is TypeNodeBuilder) {
						TypeNodeBuilder tnb = (TypeNodeBuilder) nb;
						if (tnb.NodeDataType.IsAssignableFrom (type)) {
							if (bestNodeType == null || bestNodeType.IsAssignableFrom (tnb.NodeDataType)) {
								bestNodeType = tnb.NodeDataType;
								bestTypeNodeBuilder = tnb;
							}
						}
					}
					else {
						try {
							if (((NodeBuilderExtension)nb).CanBuildNode (type))
								list.Add (nb);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
					}
				}
				
				if (bestTypeNodeBuilder != null) {
					list.Insert (0, bestTypeNodeBuilder);
					chain = (NodeBuilder[]) list.ToArray (typeof(NodeBuilder));
				} else
					chain = null;
				
				builderChains [type] = chain;
			}
			return chain;
		}
		
		TypeNodeBuilder GetTypeNodeBuilder (Gtk.TreeIter iter)
		{
			NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (iter, MonoDevelopTreeView.BuilderChainColumn);
			if (chain != null && chain.Length > 0)
				return chain[0] as TypeNodeBuilder;
			return null;
		}
		
		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			sorting = true;
			
			try {
				NodeBuilder[] chain1 = (NodeBuilder[]) store.GetValue (a, BuilderChainColumn);
				if (chain1 == null) return -1;
				
				compareNode1.MoveToIter (a);
				compareNode2.MoveToIter (b);
				
				TypeNodeBuilder tb1 = (TypeNodeBuilder) chain1[0];
				int sort = tb1.CompareObjects (compareNode1, compareNode2);
				if (sort != TypeNodeBuilder.DefaultSort) return sort;
				
				NodeBuilder[] chain2 = (NodeBuilder[]) store.GetValue (b, BuilderChainColumn);
				if (chain2 == null) return 1;
				TypeNodeBuilder tb2 = (TypeNodeBuilder) chain2[0];
				
				if (chain1 != chain2) {
					sort = tb2.CompareObjects (compareNode2, compareNode1);
					if (sort != TypeNodeBuilder.DefaultSort) return sort * -1;
				}
				
				object o1 = store.GetValue (a, DataItemColumn);
				object o2 = store.GetValue (b, DataItemColumn);
				return string.Compare (tb1.GetNodeName (compareNode1, o1), tb2.GetNodeName (compareNode2, o2), true);
			} finally {
				sorting = false;
			}
		}
		
		internal bool GetFirstNode (object dataObject, out Gtk.TreeIter iter)
		{
			object it = nodeHash [dataObject];
			if (it == null) {
				iter = Gtk.TreeIter.Zero;
				return false;
			}
			else if (it is Gtk.TreeIter)
				iter = (Gtk.TreeIter) it;
			else
				iter = ((Gtk.TreeIter[])it)[0];
			return true;
		}
		
		internal bool GetNextNode (object dataObject, ref Gtk.TreeIter iter)
		{
			object it = nodeHash [dataObject];
			if (it == null) {
				return false;
			}
			else if (it is Gtk.TreeIter)
				return false; // There is only one node, GetFirstNode returned it
			else {
				Gtk.TreeIter[] its = (Gtk.TreeIter[]) it;
				for (int n=0; n<its.Length; n++) {
					if (its [n].Equals (iter)) {
						if (n < its.Length - 1) {
							iter = its [n+1];
							return true;
						}
					}
				}
				return false;
			}
		}
		
		internal void RegisterNode (Gtk.TreeIter it, object dataObject, NodeBuilder[] chain)
		{
			object currentIt = nodeHash [dataObject];
			if (currentIt == null) {
				nodeHash [dataObject] = it;
				if (chain == null) chain = GetBuilderChain (dataObject.GetType());
				foreach (NodeBuilder nb in chain) {
					try {
						nb.OnNodeAdded (dataObject);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
			} else {
				if (currentIt is Gtk.TreeIter[]) {
					Gtk.TreeIter[] arr = (Gtk.TreeIter[]) currentIt;
					Gtk.TreeIter[] newArr = new Gtk.TreeIter [arr.Length + 1];
					arr.CopyTo (newArr, 0);
					newArr [arr.Length] = it;
					nodeHash [dataObject] = newArr;
				} else {
					nodeHash [dataObject] = new Gtk.TreeIter [] { it, (Gtk.TreeIter) currentIt};
				}
			}
		}
		
		internal void UnregisterNode (object dataObject, Gtk.TreeIter iter, NodeBuilder[] chain)
		{
			// Remove object from copy list

			if (copyObjects != null) {
				int i = Array.IndexOf (copyObjects, dataObject);
				if (i != -1) {
					ArrayList list = new ArrayList (copyObjects);
					list.RemoveAt (i);
					if (list.Count > 0)
						copyObjects = list.ToArray ();
					else
						copyObjects = null;
				}
			}
				
			// Remove object from drag list

			if (dragObjects != null) {
				int i = Array.IndexOf (dragObjects, dataObject);
				if (i != -1) {
					ArrayList list = new ArrayList (dragObjects);
					list.RemoveAt (i);
					if (list.Count > 0)
						dragObjects = list.ToArray ();
					else
						dragObjects = null;
				}
			}
			
			nodeOptions.Remove (iter);
			object currentIt = nodeHash [dataObject];
			if (currentIt is Gtk.TreeIter[]) {
				Gtk.TreeIter[] arr = (Gtk.TreeIter[]) currentIt;
				int i = Array.IndexOf (arr, iter);
				if (arr.Length > 2) {
					Gtk.TreeIter[] newArr = new Gtk.TreeIter[arr.Length - 1];
					Array.Copy (arr, 0, newArr, 0, i);
					if (i < newArr.Length)
						Array.Copy (arr, i+1, newArr, i, arr.Length - i - 1);
					nodeHash [dataObject] = newArr;
				} else {
					if (i == 0) nodeHash [dataObject] = arr[1];
					else nodeHash [dataObject] = arr[0];
				}
			} else {
				nodeHash.Remove (dataObject);
				NotifyNodeRemoved (dataObject, chain);
			}
		}
				
		void NotifyNodeRemoved (object dataObject, NodeBuilder[] chain)
		{
			if (chain == null)
				chain = GetBuilderChain (dataObject.GetType());
			foreach (NodeBuilder nb in chain) {
				try {
					nb.OnNodeRemoved (dataObject);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
		}
		
		internal bool IsRegistered (object dataObject)
		{
			return nodeHash.Contains (dataObject);
		}
		
		public void NotifyInserted (Gtk.TreeIter it, object dataObject)
		{
			if (callbacks.Count > 0) {
				ArrayList list = callbacks [dataObject] as ArrayList;
				if (list != null) {
					ITreeNavigator nav = new TreeNodeNavigator (this, it);
					NodePosition pos = nav.CurrentPosition;
					foreach (TreeNodeCallback callback in list) {
						callback (nav);
						nav.MoveToPosition (pos);
					}
					callbacks.Remove (dataObject);
				}
			}
		}
		
		internal TreeOptions GetOptions (Gtk.TreeIter iter, bool createSpecificOptions)
		{
			if (nodeOptions.Count == 0) {
				if (createSpecificOptions) {
					TreeOptions ops = globalOptions.CloneOptions (iter);
					nodeOptions [iter] = ops;
					return ops;
				}
				else
					return globalOptions;
			}
			
			TreeOptions result = null;
			Gtk.TreeIter it = iter;
			do {
				result = nodeOptions [it] as TreeOptions;
			} while (result == null && store.IterParent (out it, it));

			if (result == null)
				result = globalOptions;
			
			if (createSpecificOptions && !it.Equals (iter)) {
				TreeOptions ops = result.CloneOptions (iter);
				nodeOptions [iter] = ops;
				return ops;
			} else
				return result;
		}
		
		internal void ClearOptions (Gtk.TreeIter iter)
		{
			if (nodeOptions.Count == 0)
				return;
				
			ArrayList toDelete = new ArrayList ();
			string path = store.GetPath (iter).ToString () + ":";
			
			foreach (Gtk.TreeIter nit in nodeOptions.Keys) {
				string npath = store.GetPath (nit).ToString () + ":";
				if (npath.StartsWith (path))
					toDelete.Add (nit);
			}

			foreach (object ob in toDelete)
				nodeOptions.Remove (ob);
		}
		
		internal TreeOptions GetIterOptions (Gtk.TreeIter iter)
		{
			return nodeOptions [iter] as TreeOptions;
		}

		internal void SetIterOptions (Gtk.TreeIter iter, TreeOptions ops)
		{
			ops.Pad = this;
			ops.Iter = iter;
			nodeOptions [iter] = ops;
		}

		internal string GetNamePathFromIter (Gtk.TreeIter iter)
		{
			workNode.MoveToIter (iter);
			StringBuilder sb = new StringBuilder ();
			do {
				string name = workNode.NodeName;
				if (sb.Length > 0) sb.Insert (0, '/');
				name = name.Replace ("%","%%");
				name = name.Replace ("/","_%_");
				sb.Insert (0, name);
			} while (workNode.MoveToParent ());

			return sb.ToString ();
		}
		
		public void RefreshNode (Gtk.TreeIter iter)
		{
			ITreeBuilder builder = new TreeBuilder (this, iter);
			builder.UpdateAll ();
		}
		
		public void ResetState (Gtk.TreeIter iter)
		{
			TreeBuilder builder = new TreeBuilder (this, iter);
			builder.ResetState ();
		}
		
		internal bool GetIterFromNamePath (string path, out Gtk.TreeIter iter)
		{
			if (!store.GetIterFirst (out iter))
				return false;
				
			TreeNodeNavigator nav = new TreeNodeNavigator (this, iter);
			string[] names = path.Split ('/');

			int n = 0;
			bool more;
			do {
				string name = names [n].Replace ("_%_","/");
				name = name.Replace ("%%","%");
				
				if (nav.NodeName == name) {
					iter = nav.CurrentPosition._iter;
					if (++n == names.Length) return true;
					more = nav.MoveToFirstChild ();
				} else
					more = nav.MoveNext ();
			} while (more);

			return false;
		}

		/// <summary>
		/// If you want to edit a node label. Select the node you want to edit and then
		/// call this method, instead of using the LabelEdit Property and the BeginEdit
		/// Method directly.
		/// </summary>
		[CommandHandler (EditCommands.Rename)]
		public void StartLabelEdit ()
		{
			GLib.Timeout.Add (20, new GLib.TimeoutHandler (wantFocus));
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		public void UpdateStartLabelEdit (CommandInfo info)
		{
			info.Enabled = GetSelectedNodes ().Length == 1;
		}
		
		bool wantFocus ()
		{
			tree.GrabFocus ();
			StartLabelEditInternal ();
			return false;
		}

		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, FilledColumn);
			if (!filled) {
				TreeBuilder nb = new TreeBuilder (this, args.Iter);
				args.RetVal = !nb.FillNode ();
			} else
				args.RetVal = false;
		}

		void ShowPopup ()
		{
			ITreeNavigator tnav = GetSelectedNode ();
			TypeNodeBuilder nb = GetTypeNodeBuilder (tnav.CurrentPosition._iter);
			if (nb == null || nb.ContextMenuAddinPath == null) {
				if (options.Length > 0) {
					CommandEntrySet opset = new CommandEntrySet ();
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
					IdeApp.CommandService.ShowContextMenu (opset, this);
				}
			} else {
				CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet (nb.ContextMenuAddinPath);
				eset.AddItem (Command.Separator);
				CommandEntrySet opset = eset.AddItemSet (GettextCatalog.GetString ("Display Options"));
				opset.AddItem (ViewCommands.TreeDisplayOptionList);
				opset.AddItem (Command.Separator);
				opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
				opset.AddItem (ViewCommands.RefreshTree);
				opset.AddItem (ViewCommands.CollapseAllTreeNodes);
				IdeApp.CommandService.ShowContextMenu (eset, this);
			}
		}
		
		[CommandUpdateHandler (ViewCommands.TreeDisplayOptionList)]
		protected void BuildTreeOptionsMenu (CommandArrayInfo info)
		{
			ITreeNavigator tnav = GetSelectedNode ();
			ITreeOptions currentOptions = tnav.Options;
			foreach (TreePadOption op in options) {
				CommandInfo ci = new CommandInfo (op.Label);
				ci.Checked = currentOptions [op.Id];
				info.Add (ci, op.Id);
			}
		}
		
		[CommandHandler (ViewCommands.TreeDisplayOptionList)]
		protected void OptionToggled (string optionId)
		{
			foreach (TreeNodeNavigator node in GetSelectedNodes ()) {
				node.Options [optionId] = !node.Options [optionId];
			}
		}
		
		[CommandHandler (ViewCommands.ResetTreeDisplayOptions)]
		protected void ResetOptions ()
		{
			foreach (TreeNodeNavigator node in GetSelectedNodes ()) {
				Gtk.TreeIter it = node.CurrentPosition._iter;
				if (store.IterIsValid (it)) {
					ClearOptions (it);
					TreeBuilder tb = new TreeBuilder (this, it);
					tb.UpdateAll ();
				}
			}
		}

		[CommandHandler (ViewCommands.RefreshTree)]
		protected void RefreshTree ()
		{
			foreach (TreeNodeNavigator node in GetSelectedNodes ()) {
				Gtk.TreeIter it = node.CurrentPosition._iter;
				if (store.IterIsValid (it)) {
					TreeBuilder tb = new TreeBuilder (this, it);
					tb.UpdateAll ();
				}
			}
		}
		
		[CommandHandler (ViewCommands.CollapseAllTreeNodes)]
		protected void CollapseTree ()
		{
			tree.CollapseAll();
		}
		
		[GLib.ConnectBefore]
		void OnKeyPress (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.F10 && (args.Event.State & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask)
				ShowPopup ();
			if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.KP_Delete)
				DeleteCurrentItem ();
			
			//HACK: to work around "Bug 377810 - Many errors when expanding MonoDevelop treeviews with keyboard"
			//  The shift-right combo recursively expands all child nodes but the OnTestExpandRow callback
			//  modifies tree and successive calls get passed an invalid iter. Using the path to regenerate the iter 
			//  causes a Gtk-Fatal.
			bool shift = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0;
			if (args.Event.Key == Gdk.Key.asterisk || args.Event.Key == Gdk.Key.KP_Multiply || (shift && (args.Event.Key == Gdk.Key.Right || args.Event.Key == Gdk.Key.KP_Right || args.Event.Key == Gdk.Key.plus || args.Event.Key == Gdk.Key.KP_Add))) {
				Gtk.TreeIter iter;
				foreach (Gtk.TreePath path in tree.Selection.GetSelectedRows ()) {
					store.GetIter (out iter, path);
					Expand (iter);
				}
				args.RetVal = true;
			}
			
		}

		void Expand (Gtk.TreeIter it)
		{
			tree.ExpandRow (store.GetPath (it), false);
			Gtk.TreeIter ci;
			if (store.IterChildren (out ci, it)) {
				do {
					Expand (ci);
				} while (store.IterNext (ref ci));
			}
		}
			
		void OnPopupMenu (object o, Gtk.PopupMenuArgs args)
		{
			if (GetSelectedNode () != null)
				ShowPopup ();
		}

		[GLib.ConnectBefore]
		private void OnButtonPress (object sender, Gtk.ButtonPressEventArgs args)
		{
			bool withModifider = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0 || (args.Event.State & Gdk.ModifierType.ControlMask) != 0;
			if (IsClickedNodeSelected ((int)args.Event.X, (int)args.Event.Y) && MultipleNodesSelected () && !withModifider) {
				args.RetVal = true;
			}
		}

		private void OnButtonRelease (object sender, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3 && GetSelectedNode() != null) {
				ShowPopup ();
			}
			
			bool withModifider = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0 || (args.Event.State & Gdk.ModifierType.ControlMask) != 0;
			if (args.Event.Button == 1 && MultipleNodesSelected () && !withModifider) {
				tree.Selection.UnselectAll ();
				Gtk.TreePath path;
				if (tree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path))
					tree.Selection.SelectPath (path);
			}
		}

		bool IsClickedNodeSelected (int x, int y)
		{
			Gtk.TreePath path;
			if (tree.GetPathAtPos (x, y, out path))
				return tree.Selection.PathIsSelected (path);
			else
				return false;
		}

		protected virtual void OnNodeActivated (object sender, Gtk.RowActivatedArgs args)
		{
			ActivateCurrentItem ();
		}
		
		protected virtual void OnSelectionChanged (object sender, EventArgs args)
		{
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node != null) {
				NodeBuilder[] chain = node.NodeBuilderChain;
				NodePosition pos = node.CurrentPosition;
				foreach (NodeBuilder b in chain) {
					try {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						handler.OnItemSelected ();
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
					node.MoveToPosition (pos);
				}
			}
		}
		
		internal class PadCheckMenuItem: Gtk.CheckMenuItem
		{
			internal string Id;
			
			public PadCheckMenuItem (string label, string id): base (label) {
				Id = id;
			}
		}
		
		internal class TreeBuilderContext: ITreeBuilderContext
		{
			MonoDevelopTreeView pad;
			Hashtable icons = new Hashtable ();
			Hashtable composedIcons = new Hashtable ();
			
			internal TreeBuilderContext (MonoDevelopTreeView pad)
			{
				this.pad = pad;
			}
			
			public ITreeBuilder GetTreeBuilder ()
			{
				Gtk.TreeIter iter;
				if (!pad.store.GetIterFirst (out iter))
					return new TreeBuilder (pad, Gtk.TreeIter.Zero);
				else
					return new TreeBuilder (pad, iter);
			}
			
			public ITreeBuilder GetTreeBuilder (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return null;
				return new TreeBuilder (pad, iter);
			}
			
			public ITreeBuilder GetTreeBuilder (ITreeNavigator navigator)
			{
				return new TreeBuilder (pad, navigator.CurrentPosition._iter);
			}
		
			public Gdk.Pixbuf GetIcon (string id)
			{
				Gdk.Pixbuf icon = icons [id] as Gdk.Pixbuf;
				if (icon == null) {
					icon = pad.tree.RenderIcon (id, Gtk.IconSize.Menu, "");
					icons [id] = icon;
				}
				return icon;
			}
			
			public Gdk.Pixbuf GetComposedIcon (Gdk.Pixbuf baseIcon, object compositionKey)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) return null;
				return itable [compositionKey] as Gdk.Pixbuf;
			}
			
			public void CacheComposedIcon (Gdk.Pixbuf baseIcon, object compositionKey, Gdk.Pixbuf composedIcon)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) {
					itable = new Hashtable ();
					composedIcons [baseIcon] = itable;
				}
				itable [compositionKey] = composedIcon;
			}
			
			public ITreeNavigator GetTreeNavigator (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return null;
				return new TreeNodeNavigator (pad, iter);
			}
			
			public MonoDevelopTreeView Tree {
				get { return pad; }
			}
		}
		
		internal class TreeNodeNavigator: ITreeNavigator, ITreeOptions
		{
			protected MonoDevelopTreeView pad;
			protected Gtk.TreeView tree;
			protected Gtk.TreeStore store;
			Gtk.TreeIter currentNavIter;
			object dataItem;
			
			public TreeNodeNavigator (MonoDevelopTreeView pad): this (pad, Gtk.TreeIter.Zero)
			{
			}
			
			public TreeNodeNavigator (MonoDevelopTreeView pad, Gtk.TreeIter iter)
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
				if (!pad.sorting && !store.IterIsValid (currentIter)) {
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
					NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, MonoDevelopTreeView.BuilderChainColumn);
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
		
			bool ITreeOptions.this [string name] {
				get {
					AssertIsValid ();
					return pad.GetOptions (currentIter, false) [name]; 
				}
				set {
					AssertIsValid ();
					pad.GetOptions (currentIter, true) [name] = value;
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
					dataItem = store.GetValue (currentNavIter, MonoDevelopTreeView.DataItemColumn);
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
					object data = store.GetValue (newIter, MonoDevelopTreeView.DataItemColumn);
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
				object it = pad.NodeHash [dataObject];
				
				if (it == null)
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
				while (store.IterParent (out cit, cit)) {
					if (cit.Equals (pit)) return true;
					if (!recursive) return false;
				}
				return false;
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
				get { return this; }
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
					else return store.GetValue (currentIter, MonoDevelopTreeView.TextColumn) as string;
				}
			}
			
			public NodeBuilder[] BuilderChain {
				get {
					AssertIsValid ();
					return (NodeBuilder[]) store.GetValue (currentIter, MonoDevelopTreeView.BuilderChainColumn);
				}
			}
			
			public object GetParentDataItem (Type type, bool includeCurrent)
			{
				if (includeCurrent && type.IsInstanceOfType (DataItem))
					return DataItem;

				Gtk.TreeIter it = currentIter;
				while (store.IterParent (out it, it)) {
					object data = store.GetValue (it, MonoDevelopTreeView.DataItemColumn);
					if (type.IsInstanceOfType (data))
						return data;
				}
				return null;
			}
		
			protected virtual void EnsureFilled ()
			{
				AssertIsValid ();
				if (!(bool) store.GetValue (currentIter, MonoDevelopTreeView.FilledColumn))
					new TreeBuilder (pad, currentIter).FillNode ();
			}
			
			public bool Filled {
				get {
					AssertIsValid ();
					return (bool) store.GetValue (currentIter, MonoDevelopTreeView.FilledColumn);
				}
			}
		}
		
		internal class TreeBuilder: TreeNodeNavigator, ITreeBuilder
		{
			public TreeBuilder (MonoDevelopTreeView pad) : base (pad)
			{
			}

			public TreeBuilder (MonoDevelopTreeView pad, Gtk.TreeIter iter): base (pad, iter)
			{
			}
			
			protected override void EnsureFilled ()
			{
				if (!(bool) store.GetValue (currentIter, MonoDevelopTreeView.FilledColumn))
					FillNode ();
			}
			
			public bool FillNode ()
			{
				store.SetValue (currentIter, MonoDevelopTreeView.FilledColumn, true);
				
				Gtk.TreeIter child;
				if (store.IterChildren (out child, currentIter))
					store.Remove (ref child);
				
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, MonoDevelopTreeView.BuilderChainColumn);
				object dataObject = store.GetValue (currentIter, MonoDevelopTreeView.DataItemColumn);
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
				object data = store.GetValue (currentIter, MonoDevelopTreeView.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, MonoDevelopTreeView.BuilderChainColumn);
				
				NodeAttributes ats = GetAttributes (chain, data);
				UpdateNode (chain, ats, data);
			}
			
			public void ResetState ()
			{
				Update ();
				RemoveChildren (currentIter);

				object data = store.GetValue (currentIter, MonoDevelopTreeView.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, MonoDevelopTreeView.BuilderChainColumn);

				if (!HasChildNodes (chain, data))
					FillNode ();
				else {
					RemoveChildren (currentIter);
					store.AppendNode (currentIter);	// Dummy node
					store.SetValue (currentIter, MonoDevelopTreeView.FilledColumn, false);
				}
			}
			
			public void UpdateChildren ()
			{
				object data = store.GetValue (currentIter, MonoDevelopTreeView.DataItemColumn);
				NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (currentIter, MonoDevelopTreeView.BuilderChainColumn);
				
				if (!(bool) store.GetValue (currentIter, MonoDevelopTreeView.FilledColumn)) {
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
					object childData = store.GetValue (child, MonoDevelopTreeView.DataItemColumn);
					if (childData != null)
						pad.UnregisterNode (childData, child, null);
					store.Remove (ref child);
				}
			}
			
			public void Remove ()
			{
				RemoveChildren (currentIter);
				object data = store.GetValue (currentIter, MonoDevelopTreeView.DataItemColumn);
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
				store.SetValue (it, MonoDevelopTreeView.DataItemColumn, dataObject);
				store.SetValue (it, MonoDevelopTreeView.BuilderChainColumn, chain);
				
				UpdateNode (chain, ats, dataObject);
				
				bool hasChildren = HasChildNodes (chain, dataObject);
				store.SetValue (currentIter, MonoDevelopTreeView.FilledColumn, !hasChildren);
				
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
				store.SetValue (it, MonoDevelopTreeView.TextColumn, text);
				if (icon != null) store.SetValue (it, MonoDevelopTreeView.OpenIconColumn, icon);
				if (closedIcon != null) store.SetValue (it, MonoDevelopTreeView.ClosedIconColumn, closedIcon);
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
		
		internal class TreeOptions : Hashtable, ITreeOptions
		{
			MonoDevelopTreeView pad;
			Gtk.TreeIter iter;
			
			public TreeOptions ()
			{
			}
			
			public TreeOptions (MonoDevelopTreeView pad, Gtk.TreeIter iter)
			{
				this.pad = pad;
				this.iter = iter;
			}
			
			public MonoDevelopTreeView Pad {
				get { return pad; }
				set { pad = value; }
			}

			public Gtk.TreeIter Iter {
				get { return iter; }
				set { iter = value; }
			}

			public bool this [string name] {
				get {
					object op = base [name];
					if (op == null) return false;
					return (bool) op;
				}
				set {
					base [name] = value;
					if (pad != null)
						pad.RefreshNode (iter);
				}
			}
			
			public TreeOptions CloneOptions (Gtk.TreeIter newIter)
			{
				TreeOptions ops = new TreeOptions (pad, newIter);
				foreach (DictionaryEntry de in this)
					ops [de.Key] = de.Value;
				return ops;
			}
		}
	}

	class NodeCommandTargetChain: ICommandDelegatorRouter
	{
		NodeCommandHandler target;
		ITreeNavigator[] nodes;
		internal NodeCommandTargetChain Next;

		public NodeCommandTargetChain (NodeCommandHandler target, ITreeNavigator[] nodes)
		{
			this.nodes = nodes;
			this.target = target;
		}
		
		public object GetNextCommandTarget ()
		{
			return Next;
		}
		
		public object GetDelegatedCommandTarget ()
		{
			target.SetCurrentNodes (nodes);
			return target;
		}
	}
}
