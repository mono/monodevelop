//
// ExtensibleTreeView.cs
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

//#define TREE_VERIFY_INTEGRITY

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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Extensions;
using Mono.TextEditor;

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ExtensibleTreeView : CompactScrolledWindow
	{
		internal const int TextColumn         = 0;
		internal const int OpenIconColumn     = 1;
		internal const int ClosedIconColumn   = 2;
		internal const int DataItemColumn     = 3;
		internal const int BuilderChainColumn = 4;
		internal const int FilledColumn       = 5;
		
		NodeBuilder[] builders;
		Dictionary<Type, NodeBuilder[]> builderChains = new Dictionary<Type, NodeBuilder[]> ();
		Hashtable nodeHash = new Hashtable ();
		
		ContextMenuTreeView tree = new ContextMenuTreeView ();
		Gtk.TreeStore store;
		internal Gtk.TreeViewColumn complete_column;
		internal ZoomableCellRendererPixbuf pix_render;
		internal Gtk.CellRendererText text_render;
		TreeBuilderContext builderContext;
		Hashtable callbacks = new Hashtable ();
		bool editingText = false;
		int customFontSize = -1;
		
		TreePadOption[] options;
		TreeOptions globalOptions;
		Dictionary<Gtk.TreeIter, TreeOptions> nodeOptions;
		
		TreeNodeNavigator workNode;
		TreeNodeNavigator compareNode1;
		TreeNodeNavigator compareNode2;
		internal bool sorting;
		
		object[] dragObjects;
		object[] copyObjects;
		DragOperation currentTransferOperation;

		TransactedNodeStore transactionStore;
		int updateLockCount;
		string contextMenuPath;
		IDictionary<string,string> contextMenuTypeNameAliases;
		
		private static Gtk.TargetEntry [] target_table = new Gtk.TargetEntry [] {
			new Gtk.TargetEntry ("text/uri-list", 0, 11 ),
			new Gtk.TargetEntry ("text/plain", 0, 22),
			new Gtk.TargetEntry ("application/x-rootwindow-drop", 0, 33)
		};
		
		public IDictionary<string,string> ContextMenuTypeNameAliases {
			get { return contextMenuTypeNameAliases; }
			set { contextMenuTypeNameAliases = value; }
		}
	
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
		
		public string Id { get; set; }

		internal object[] DragObjects {
			get {
				return this.dragObjects;
			}
		}
		
		public ExtensibleTreeView ()
		{
		}
		
		public ExtensibleTreeView (NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (builders, options);
		}
		
		void CustomFontPropertyChanged (object sender, MonoDevelop.Core.PropertyChangedEventArgs prop)
		{
			string val = (string)prop.NewValue;
			string name = !string.IsNullOrEmpty (val) ? val : tree.Style.FontDescription.ToString ();
			UpdateCustomFont (name);
		}
		
		void UpdateCustomFont (string name)
		{
			Pango.FontDescription customFont = Pango.FontDescription.FromString (name);
			customFontSize = customFont.Size;
			if (Zoom != 1)
				customFont.Size = (int) (((double) customFont.Size) * Zoom);
			text_render.Family = customFont.Family;
			text_render.Size = customFont.Size;
			customFont.Dispose ();
			tree.ColumnsAutosize ();
		}
		
		protected override void OnStyleSet (Gtk.Style previous_style)
		{
			base.OnStyleSet (previous_style);
			UpdateCustomFont (IdeApp.Preferences.CustomPadFont ?? tree.Style.FontDescription.ToString ());
		}
		
		public void Initialize (NodeBuilder[] builders, TreePadOption[] options)
		{
			Initialize (builders, options, null);
		}
		
		public virtual void Initialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			this.contextMenuPath = contextMenuPath;
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
			store = new Gtk.TreeStore (typeof(string), typeof(Gdk.Pixbuf), typeof(Gdk.Pixbuf), typeof(object), typeof(object), typeof(bool));
			tree.Model = store;
			tree.Selection.Mode = Gtk.SelectionMode.Multiple;
			nodeOptions = new Dictionary<Gtk.TreeIter, TreeOptions> (new IterComparer (store));

			tree.EnableModelDragDest (target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			Gtk.Drag.SourceSet (tree, Gdk.ModifierType.Button1Mask, target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			
			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (CompareNodes);
			store.SetSortColumnId (/* GTK_TREE_SORTABLE_DEFAULT_SORT_COLUMN_ID */ -1, Gtk.SortType.Ascending);
			
			tree.HeadersVisible = false;
			tree.SearchColumn = 0;
			tree.EnableSearch = true;
			complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "column";

			pix_render = new ZoomableCellRendererPixbuf ();
			complete_column.PackStart (pix_render, false);
			complete_column.AddAttribute (pix_render, "image", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "image-expander-open", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "image-expander-closed", ClosedIconColumn);
			
			text_render = new Gtk.CellRendererText ();
			var customFontName = IdeApp.Preferences.CustomPadFont;
			if (customFontName != null) {
				Pango.FontDescription customFont = Pango.FontDescription.FromString (customFontName);
				text_render.Family = customFont.Family;
				text_render.Size = customFont.Size;
				customFontSize = customFont.Size;
				customFont.Dispose ();
			}
			text_render.Ypad = 0;
			IdeApp.Preferences.CustomPadFontChanged += CustomFontPropertyChanged;;
			text_render.EditingStarted += HandleEditingStarted;
			text_render.Edited += HandleOnEdit;
			text_render.EditingCanceled += HandleOnEditCancelled;
			
			complete_column.PackStart (text_render, true);
			complete_column.AddAttribute (text_render, "markup", TextColumn);
			
			tree.AppendColumn (complete_column);
			
			tree.TestExpandRow += OnTestExpandRow;
			tree.RowActivated += OnNodeActivated;
			tree.DoPopupMenu += ShowPopup;
			workNode = new TreeNodeNavigator (this);
			compareNode1 = new TreeNodeNavigator (this);
			compareNode2 = new TreeNodeNavigator (this);
			
			tree.DragBegin += OnDragBegin;
			tree.DragDataReceived += OnDragDataReceived;
			tree.DragDrop += OnDragDrop;
			tree.DragEnd += OnDragEnd;
			tree.DragMotion += OnDragMotion;
			
			tree.CursorChanged += OnSelectionChanged;
			tree.KeyPressEvent += OnKeyPress;

			for (int n=3; n<16; n++) {
				Gtk.Rc.ParseString ("style \"MonoDevelop.ExtensibleTreeView_" + n + "\" {\n GtkTreeView::expander-size = " + n + "\n }\n");
				Gtk.Rc.ParseString ("widget \"*.MonoDevelop.ExtensibleTreeView_" + n + "\" style  \"MonoDevelop.ExtensibleTreeView_" + n + "\"\n");
			}
			
			if (!string.IsNullOrEmpty (Id))
				Zoom = PropertyService.Get<double> ("MonoDevelop.Ide.ExtensibleTreeView.Zoom." + Id, 1d);
			
			this.Add (tree);
			this.ShowAll ();

#if TREE_VERIFY_INTEGRITY
			GLib.Timeout.Add (3000, Checker);
#endif
		}
	
#if TREE_VERIFY_INTEGRITY
		// Verifies the consistency of the tree view. Disabled by default
		HashSet<object> ochecked = new HashSet<object> ();
		bool Checker ()
		{
			int nodes = 0;
			foreach (DictionaryEntry e in nodeHash) {
				if (e.Value is Gtk.TreeIter) {
					nodes++;
					if (!store.IterIsValid ((Gtk.TreeIter)e.Value) && ochecked.Add (e.Key)) {
						Console.WriteLine ("Found invalid iter in tree pad - Object: " + e.Key);
						MessageService.ShowError ("Found invalid iter in tree pad", "Object: " + e.Key);
					}
				} else {
					Gtk.TreeIter[] iters = (Gtk.TreeIter[]) e.Value;
					for (int n=0; n<iters.Length; n++) {
						Gtk.TreeIter it = iters [n];
						if (!store.IterIsValid (it) && ochecked.Add (e.Key)) {
							Console.WriteLine ("Found invalid iter in tree pad - Object: " + e.Key + ", index:" + n);
							MessageService.ShowError ("Found invalid iter in tree pad", "Object: " + e.Key + ", index:" + n);
						}
						nodes++;
					}
				}
			}
			return true;
		}
#endif
		
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
			DropPosition dropPos;
			if (pos == Gtk.TreeViewDropPosition.After)
				dropPos = DropPosition.After;
			else if (pos == Gtk.TreeViewDropPosition.Before)
				dropPos = DropPosition.Before;
			else
				dropPos = DropPosition.Into;
			
			bool updatesLocked = false;

			try {
				foreach (NodeBuilder nb in chain) {
					try {
						NodeCommandHandler handler = nb.CommandHandler;
						handler.SetCurrentNode (nav);
						if (handler.CanDropMultipleNodes (obj, oper, dropPos)) {
							foundHandler = true;
							if (drop) {
								if (!updatesLocked) {
									LockUpdates ();
									updatesLocked = true;
								}
								handler.OnMultipleNodeDrop (obj, oper, dropPos);
							}
						}
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
			} finally {
				if (updatesLocked)
					UnlockUpdates ();
			}
			return foundHandler;
		}

		internal void LockUpdates ()
		{
			if (++updateLockCount == 1)
				transactionStore = new TransactedNodeStore (this);
		}

		internal void UnlockUpdates ()
		{
			if (--updateLockCount == 0) {
				TransactedNodeStore store = transactionStore;
				transactionStore = null;
				store.CommitChanges ();
				CleanupNodeOptions ();
			}
		}

		internal ITreeBuilder CreateBuilder ()
		{
			return CreateBuilder (Gtk.TreeIter.Zero);
		}

		internal ITreeBuilder CreateBuilder (Gtk.TreeIter it)
		{
			if (transactionStore != null)
				return new TransactedTreeBuilder (this, transactionStore, it);
			else
				return new TreeBuilder (this, it);
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

		public ITreeBuilderContext BuilderContext {
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
		
		public ITreeBuilder LoadTree (object nodeObject)
		{
			Clear ();
			TreeBuilder builder = new TreeBuilder (this);
			builder.AddChild (nodeObject, true);
			builder.Expanded = true;
			InitialSelection ();
			return builder;
		}
		
		public ITreeBuilder AddChild (object nodeObject)
		{
			TreeBuilder builder = new TreeBuilder (this);
			builder.AddChild (nodeObject, true);
			builder.Expanded = true;
			InitialSelection ();
			return builder;
		}
		
		public void RemoveChild (object nodeObject)
		{
			TreeBuilder builder = new TreeBuilder (this);
			if (builder.MoveToObject (nodeObject)) {
				builder.Remove ();
				InitialSelection ();
			}
		}
		
		void InitialSelection ()
		{
			if (tree.Selection.CountSelectedRows () == 0) {
				Gtk.TreeIter it;
				if (store.GetIterFirst (out it)) {
					tree.Selection.SelectIter (it);
					tree.SetCursor (store.GetPath (it), tree.Columns [0], false);
				}
			}
		}
		
		public void Clear ()
		{
			copyObjects = dragObjects = null;
			
			object[] obs = new object [nodeHash.Count];
			nodeHash.Keys.CopyTo (obs, 0);
			
			foreach (object dataObject in obs)
				NotifyNodeRemoved (dataObject, null);
			
			nodeHash = new Hashtable ();
			nodeOptions.Clear ();
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
					if (parent == null || parent == dataObject || dataObject.Equals (parent)) return null;
					
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
				return targets[0];
			else if (targets.Count > 1)
				return new MulticastNodeRouter (targets);
			else
				return null;
		}
		
		void ExpandCurrentItem ()
		{
			try {
				LockUpdates ();
				foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
					grp.SavePositions ();
					foreach (var node in grp.Nodes) {
						node.Expanded = true;
					}
				}
			} finally {
				UnlockUpdates ();
			}
		}

		void CollapseCurrentItem ()
		{
			try {
				LockUpdates ();
				foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
					grp.SavePositions ();
					foreach (var node in grp.Nodes) {
						node.Expanded = false;
					}
				}
			} finally {
				UnlockUpdates ();
			}
		}

		[CommandHandler (ViewCommands.Open)]
		public virtual void ActivateCurrentItem ()
		{
			try {
				LockUpdates ();
				foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
					grp.SavePositions ();
					foreach (NodeBuilder b in grp.BuilderChain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNodes (grp.Nodes.ToArray ());
						handler.ActivateMultipleItems ();
						if (!grp.RestorePositions ())
							break;
					}
				}
				OnCurrentItemActivated (EventArgs.Empty);
			} finally {
				UnlockUpdates ();
			}
		}
		
		public virtual void DeleteCurrentItem ()
		{
			try {
				LockUpdates ();
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
			} finally {
				UnlockUpdates ();
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
		
		#region Zoom
		
		const int ZOOM_MIN = -4;
		const int ZOOM_MAX = 8;
		const double ZOOM_FACTOR = 1.1f;
		
		int zoomPow = 0;
		double zoom = 1;
		
		public double Zoom {
			get {
				 return zoom;
			}
			set {
				ZoomPow = (int) System.Math.Round (System.Math.Log (value) / System.Math.Log (ZOOM_FACTOR));
			}
		}
		
		int ZoomPow {
			get {
				return zoomPow;
			}
			set {
				value = System.Math.Min (ZOOM_MAX, System.Math.Max (ZOOM_MIN, value));
				if (zoomPow != value) {
					zoomPow = value;
					zoom = System.Math.Pow (ZOOM_FACTOR, zoomPow);
					OnZoomChanged (zoom);
				}
			}
		}
		
		void OnZoomChanged (double value)
		{
			pix_render.Zoom = value;
			if (customFontSize != -1) {
				int newSize = customFontSize;
				if (value != 1)
					newSize = (int) (((double) customFontSize) * value);
				text_render.Size = newSize;
			}
			int expanderSize = (int) (12 * Zoom);
			if (expanderSize < 3) expanderSize = 3;
			if (expanderSize > 15) expanderSize = 15;
			if (expanderSize != 12)
				tree.Name = "MonoDevelop.ExtensibleTreeView_" + expanderSize;
			else
				tree.Name = "";
			tree.ColumnsAutosize ();
			if (!string.IsNullOrEmpty (Id)) {
				PropertyService.Set ("MonoDevelop.Ide.ExtensibleTreeView.Zoom." + Id, Zoom);
				PropertyService.SaveProperties ();
			}
		}
		
		[CommandHandler (ViewCommands.ZoomIn)]
		public void ZoomIn ()
		{
			ZoomPow++;
		}

		[CommandHandler (ViewCommands.ZoomOut)]
		public void ZoomOut ()
		{
			ZoomPow--;
		}
		
		[CommandHandler (ViewCommands.ZoomReset)]
		public void ZoomReset ()
		{
			ZoomPow = 0;
		}

		[CommandUpdateHandler (ViewCommands.ZoomIn)]
		protected void UpdateZoomIn (CommandInfo cinfo)
		{
			cinfo.Enabled = ZoomPow <= ZOOM_MAX;
		}

		[CommandUpdateHandler (ViewCommands.ZoomOut)]
		protected void UpdateZoomOut (CommandInfo cinfo)
		{
			cinfo.Enabled = ZoomPow >= ZOOM_MIN;
		}
		
		[CommandUpdateHandler (ViewCommands.ZoomReset)]
		protected void UpdateZoomReset (CommandInfo cinfo)
		{
			cinfo.Enabled = ZoomPow != 0;
		}
		
		#endregion

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
					ITreeBuilder tb = CreateBuilder ();
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

			try {
				LockUpdates ();
				TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
				if (node != null) {
					NodeBuilder[] chain = node.NodeBuilderChain;
					NodePosition pos = node.CurrentPosition;
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNode (node);
						if (handler.CanDropMultipleNodes (copyObjects, currentTransferOperation, DropPosition.Into)) {
							node.MoveToPosition (pos);
							handler.OnMultipleNodeDrop (copyObjects, currentTransferOperation, DropPosition.Into);
						}
						node.MoveToPosition (pos);
					}
				}
				if (currentTransferOperation == DragOperation.Move)
					CancelTransfer ();
			} finally {
				UnlockUpdates ();
			}
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
						if (handler.CanDropMultipleNodes (copyObjects, currentTransferOperation, DropPosition.Into)) {
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
						ITreeBuilder tb = CreateBuilder ();
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
			
			string nodeName = node.NodeName;
			store.SetValue (iter, ExtensibleTreeView.TextColumn, nodeName);
			
			// Get and validate the initial text selection
			int nameLength = nodeName != null ? nodeName.Length : 0,
				selectionStart = 0, selectionLength = nameLength;
			foreach (NodeBuilder b in node.NodeBuilderChain) {
				try {
					NodeCommandHandler handler = b.CommandHandler;
					handler.SetCurrentNode(node);
					handler.OnRenameStarting(ref selectionStart, ref selectionLength);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
			if (selectionStart < 0 || selectionStart >= nameLength)
				selectionStart = 0;
			if (selectionStart + selectionLength > nameLength)
				selectionLength = nameLength - selectionStart;
			// This will apply the selection as soon as possible
			GLib.Idle.Add (() => {
				var editable = currentLabelEditable;
				if (editable == null)
					return false;

				editable.SelectRegion (selectionStart, selectionStart + selectionLength);
				return false;
			});
			text_render.Editable = true;
			tree.SetCursor (store.GetPath (iter), complete_column, true);
			
			editingText = true;
		}

		Gtk.Editable currentLabelEditable;
		void HandleEditingStarted (object o, Gtk.EditingStartedArgs e)
		{
			currentLabelEditable = e.Editable as Gtk.Entry;
		}

		void HandleOnEdit (object o, Gtk.EditedArgs e)
		{
			try {
				editingText = false;
				text_render.Editable = false;
				currentLabelEditable = null;
				
				Gtk.TreeIter iter;
				if (!store.GetIterFromString (out iter, e.Path))
					throw new Exception("Error calculating iter for path " + e.Path);

				if (e.NewText != null && e.NewText.Length > 0) {
					ITreeNavigator nav = new TreeNodeNavigator (this, iter);
					NodePosition pos = nav.CurrentPosition;

					try {
						LockUpdates ();
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
					} finally {
						UnlockUpdates ();
					}
				}
				
				// Get the iter again since the this node may have been replaced.
				if (!store.GetIterFromString (out iter, e.Path))
					return;

				ITreeBuilder builder = CreateBuilder (iter);
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
			currentLabelEditable = null;
			
			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			if (node == null)
				return;
			
			// Restore the original node label
			Gtk.TreeIter iter = node.CurrentPosition._iter;
			ITreeBuilder builder = CreateBuilder (iter);
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
			NodeBuilder[] chain;
			builderChains.TryGetValue (type, out chain);
			if (chain == null) {
				List<NodeBuilder> list = new List<NodeBuilder> ();
				
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
					} else {
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
					chain = list.ToArray ();
				} else
					chain = null;
				
				builderChains [type] = chain;
			}
			return chain;
		}
		
		TypeNodeBuilder GetTypeNodeBuilder (Gtk.TreeIter iter)
		{
			NodeBuilder[] chain = (NodeBuilder[]) store.GetValue (iter, ExtensibleTreeView.BuilderChainColumn);
			if (chain != null && chain.Length > 0)
				return chain[0] as TypeNodeBuilder;
			return null;
		}
		
		internal int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			sorting = true;
			try {
				NodeBuilder[] chain1 = (NodeBuilder[]) store.GetValue (a, BuilderChainColumn);
				if (chain1 == null) return -1;
				
				compareNode1.MoveToIter (a);
				compareNode2.MoveToIter (b);
				
				int sort = CompareObjects (chain1, compareNode1, compareNode2);
				if (sort != TypeNodeBuilder.DefaultSort) return sort;
				
				NodeBuilder[] chain2 = (NodeBuilder[]) store.GetValue (b, BuilderChainColumn);
				if (chain2 == null) return 1;
				
				if (chain1 != chain2) {
					sort = CompareObjects (chain2, compareNode2, compareNode1);
					if (sort != TypeNodeBuilder.DefaultSort) return sort * -1;
				}
				
				TypeNodeBuilder tb1 = (TypeNodeBuilder) chain1[0];
				TypeNodeBuilder tb2 = (TypeNodeBuilder) chain2[0];
				object o1 = store.GetValue (a, DataItemColumn);
				object o2 = store.GetValue (b, DataItemColumn);
				return string.Compare (tb1.GetNodeName (compareNode1, o1), tb2.GetNodeName (compareNode2, o2), true);
			} finally {
				sorting = false;
			}
		}
		
		int CompareObjects (NodeBuilder[] chain, ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			int result = NodeBuilder.DefaultSort;
			for (int n=0; n<chain.Length; n++) {
				int sort = chain[n].CompareObjects (thisNode, otherNode);
				if (sort != NodeBuilder.DefaultSort)
					result = sort;
			}
			return result;
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
				Gtk.TreePath iterPath = store.GetPath (iter);
				for (int n=0; n<its.Length; n++) {
					if (store.GetPath (its[n]).Equals (iterPath)) {
						if (n < its.Length - 1) {
							iter = its [n+1];
							return true;
						}
					}
				}
				return false;
			}
		}
		
		internal void RegisterNode (Gtk.TreeIter it, object dataObject, NodeBuilder[] chain, bool fireAddedEvent)
		{
			object currentIt = nodeHash [dataObject];
			if (currentIt == null) {
				nodeHash [dataObject] = it;
				if (chain == null) chain = GetBuilderChain (dataObject.GetType());
				if (fireAddedEvent) {
					foreach (NodeBuilder nb in chain) {
						try {
							nb.OnNodeAdded (dataObject);
						} catch (Exception ex) {
							LoggingService.LogError (ex.ToString ());
						}
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
		
		internal void UnregisterNode (object dataObject, Gtk.TreeIter iter, NodeBuilder[] chain, bool fireRemovedEvent)
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
			
			if (store.IterIsValid (iter))
				nodeOptions.Remove (iter);
			object currentIt = nodeHash [dataObject];
			if (currentIt is Gtk.TreeIter[]) {
				Gtk.TreeIter[] arr = (Gtk.TreeIter[]) currentIt;
				Gtk.TreePath path = null;
				List<Gtk.TreeIter> iters = new List<Gtk.TreeIter> ();
				if (store.IterIsValid (iter))
					path = store.GetPath (iter);
				
				// Iters can't be directly compared (TreeIter.Equals is broken), so we have
				// to compare paths.
				foreach (Gtk.TreeIter it in arr) {
					if (store.IterIsValid (it) && (path == null || !path.Equals (store.GetPath (it))))
						iters.Add (it);
				}
				if (iters.Count > 1)
					nodeHash [dataObject] = iters.ToArray ();
				else if (iters.Count == 1)
					nodeHash [dataObject] = iters[0];
				else
					nodeHash.Remove (dataObject);
			} else {
				nodeHash.Remove (dataObject);
				if (fireRemovedEvent)
					NotifyNodeRemoved (dataObject, chain);
			}
		}
		
		internal void RemoveChildren (Gtk.TreeIter it)
		{
			Gtk.TreeIter child;
			while (store.IterChildren (out child, it)) {
				RemoveChildren (child);
				object childData = store.GetValue (child, ExtensibleTreeView.DataItemColumn);
				if (childData != null)
					UnregisterNode (childData, child, null, true);
				store.Remove (ref child);
			}
		}
		
		internal void CleanupNodeOptions ()
		{
			Dictionary<Gtk.TreeIter, TreeOptions> newOps = new Dictionary<Gtk.TreeIter, TreeOptions> ();
			foreach (KeyValuePair<Gtk.TreeIter,TreeOptions> val in nodeOptions) {
				if (store.IterIsValid (val.Key))
					newOps [val.Key] = val.Value;
			}
			nodeOptions = newOps;
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
				nodeOptions.TryGetValue (it, out result);
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

			List<Gtk.TreeIter> toDelete = new List<Gtk.TreeIter> ();
			string path = store.GetPath (iter).ToString () + ":";
			
			foreach (Gtk.TreeIter nit in nodeOptions.Keys) {
				string npath = store.GetPath (nit).ToString () + ":";
				if (npath.StartsWith (path))
					toDelete.Add (nit);
			}

			foreach (Gtk.TreeIter ob in toDelete)
				nodeOptions.Remove (ob);
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
			ITreeBuilder builder = CreateBuilder (iter);
			builder.UpdateAll ();
		}
		
		internal void ResetState (ITreeNavigator nav)
		{
			if (nav is TreeBuilder)
				((TreeBuilder)nav).ResetState ();
			else if (nav is TransactedTreeBuilder)
				((TransactedTreeBuilder)nav).ResetState ();
			else {
				ITreeBuilder builder = CreateBuilder (nav.CurrentPosition._iter);
				ResetState (builder);
			}
		}

		internal void SetNodeOptions (ITreeNavigator nav, TreeOptions ops)
		{
			if (nav.CurrentPosition._node != null)
				return;
			
			// TODO transaction
			Gtk.TreeIter iter = nav.CurrentPosition._iter;
			ops.Pad = this;
			ops.Iter = iter;
			nodeOptions [iter] = ops;
		}

		internal TreeOptions GetNodeOptions (ITreeNavigator nav)
		{
			TreeNode node = nav.CurrentPosition._node as TreeNode;
			if (node != null && node.HasIter) {
				TreeOptions ops;
				if (nodeOptions.TryGetValue (node.NodeIter, out ops))
					return ops;
				else
					return null;
			}
			else if (node != null)
				return null;
			else {
				TreeOptions ops;
				if (nodeOptions.TryGetValue (nav.CurrentPosition._iter, out ops))
					return ops;
				else
					return null;
			}
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
			if (editingText || GetSelectedNodes ().Length != 1) {
				info.Visible = false;
				return;
			}

			TreeNodeNavigator node = (TreeNodeNavigator) GetSelectedNode ();
			NodeAttributes attributes = GetNodeAttributes (node);
			if ((attributes & NodeAttributes.AllowRename) == 0) {
				info.Visible = false;
				return;
			}
		}
		
		NodeAttributes GetNodeAttributes (TreeNodeNavigator node)
		{
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
			return attributes;
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

		void ShowPopup (Gdk.EventButton evt)
		{
			ITreeNavigator tnav = GetSelectedNode ();
			if (tnav == null)
				return;
			TypeNodeBuilder nb = GetTypeNodeBuilder (tnav.CurrentPosition._iter);
			string menuPath = nb != null && nb.ContextMenuAddinPath != null ? nb.ContextMenuAddinPath : contextMenuPath;
			if (menuPath == null) {
				if (options.Length > 0) {
					CommandEntrySet opset = new CommandEntrySet ();
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
					IdeApp.CommandService.ShowContextMenu (this, evt, opset, this);
				}
			} else {
				ExtensionContext ctx = AddinManager.CreateExtensionContext ();
				ctx.RegisterCondition ("ItemType", new ItemTypeCondition (tnav.DataItem.GetType (), contextMenuTypeNameAliases));
				CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);
				
				eset.AddItem (Command.Separator);
				CommandEntrySet opset = eset.AddItemSet (GettextCatalog.GetString ("Display Options"));
				opset.AddItem (ViewCommands.TreeDisplayOptionList);
				opset.AddItem (Command.Separator);
				opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
				opset.AddItem (ViewCommands.RefreshTree);
				opset.AddItem (ViewCommands.CollapseAllTreeNodes);
				IdeApp.CommandService.ShowContextMenu (this, evt, eset, this);
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
					ITreeBuilder tb = CreateBuilder (it);
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
					ITreeBuilder tb = CreateBuilder (it);
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
			if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.KP_Delete) {
				DeleteCurrentItem ();
				args.RetVal = true;
				return;
			}
			
			//HACK: to work around "Bug 377810 - Many errors when expanding MonoDevelop treeviews with keyboard"
			//  The shift-right combo recursively expands all child nodes but the OnTestExpandRow callback
			//  modifies tree and successive calls get passed an invalid iter. Using the path to regenerate the iter 
			//  causes a Gtk-Fatal.
			bool shift = (args.Event.State & Gdk.ModifierType.ShiftMask) != 0;
			if (args.Event.Key == Gdk.Key.asterisk || args.Event.Key == Gdk.Key.KP_Multiply
				|| (shift && (args.Event.Key == Gdk.Key.Right || args.Event.Key == Gdk.Key.KP_Right
					|| args.Event.Key == Gdk.Key.plus || args.Event.Key == Gdk.Key.KP_Add)))
			{
				Gtk.TreeIter iter;
				foreach (Gtk.TreePath path in tree.Selection.GetSelectedRows ()) {
					store.GetIter (out iter, path);
					Expand (iter);
				}
				args.RetVal = true;
				return;
			}
			
			if (args.Event.Key == Gdk.Key.Right || args.Event.Key == Gdk.Key.KP_Right) {
				ExpandCurrentItem ();
				args.RetVal = true;
				return;
			}
			
			if (args.Event.Key == Gdk.Key.Left || args.Event.Key == Gdk.Key.KP_Left) {
				CollapseCurrentItem ();
				args.RetVal = true;
				return;
			}

			if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter) {
				ActivateCurrentItem ();
				args.RetVal = true;
				return;
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
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			var modifier = !Platform.IsMac? Gdk.ModifierType.ControlMask
				//Mac window manager already uses control-scroll, so use command
				//Command might be either meta or mod1, depending on GTK version
				: (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);
			
			if ((evnt.State & modifier) !=0) {
				if (evnt.Direction == Gdk.ScrollDirection.Up)
					ZoomIn ();
				else if (evnt.Direction == Gdk.ScrollDirection.Down)
					ZoomOut ();
				
				return true;
			}
			return base.OnScrollEvent (evnt);
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
		
		protected override void OnDestroyed ()
		{
			IdeApp.Preferences.CustomPadFontChanged -= CustomFontPropertyChanged;;
			if (pix_render != null) {
				pix_render.Destroy ();
				pix_render = null;
			}
			if (complete_column != null) {
				complete_column.Destroy ();
				complete_column = null;
			}
			if (text_render != null) {
				text_render.Destroy ();
				text_render = null;
			}
			
			if (store != null) {
				Clear ();
				store.Dispose ();
				store = null;
			}
			
			if (builders != null) {
				foreach (NodeBuilder nb in builders) {
					try {
						nb.Dispose ();
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
				builders = null;
			}
			builderChains.Clear ();
			
			base.OnDestroyed ();
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
			ExtensibleTreeView pad;
			Hashtable icons = new Hashtable ();
			Hashtable composedIcons = new Hashtable ();
			
			internal TreeBuilderContext (ExtensibleTreeView pad)
			{
				this.pad = pad;
			}
			
			public ITreeBuilder GetTreeBuilder ()
			{
				Gtk.TreeIter iter;
				if (!pad.store.GetIterFirst (out iter))
					return pad.CreateBuilder (Gtk.TreeIter.Zero);
				else
					return pad.CreateBuilder (iter);
			}
			
			public ITreeBuilder GetTreeBuilder (object dataObject)
			{
				ITreeBuilder tb = pad.CreateBuilder ();
				if (tb.MoveToObject (dataObject))
					return tb;
				else
					return null;
			}
			
			public ITreeBuilder GetTreeBuilder (ITreeNavigator navigator)
			{
				return pad.CreateBuilder (navigator.CurrentPosition._iter);
			}
		
			public Gdk.Pixbuf GetIcon (string id)
			{
				Gdk.Pixbuf icon = icons [id] as Gdk.Pixbuf;
				if (icon == null) {
					icon = ImageService.GetPixbuf (id, Gtk.IconSize.Menu);
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
			
			public Gdk.Pixbuf CacheComposedIcon (Gdk.Pixbuf baseIcon, object compositionKey, Gdk.Pixbuf composedIcon)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) {
					itable = new Hashtable ();
					composedIcons [baseIcon] = itable;
				}
				itable [compositionKey] = composedIcon;
				return composedIcon;
			}
			
			public ITreeNavigator GetTreeNavigator (object dataObject)
			{
				Gtk.TreeIter iter;
				if (!pad.GetFirstNode (dataObject, out iter)) return null;
				return new TreeNodeNavigator (pad, iter);
			}
			
			public ExtensibleTreeView Tree {
				get { return pad; }
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
			target.SetCurrentNodes (null);
			return Next;
		}
		
		public object GetDelegatedCommandTarget ()
		{
			target.SetCurrentNodes (nodes);
			return target;
		}
	}

	class IterComparer: IEqualityComparer<Gtk.TreeIter>
	{
		Gtk.TreeStore store;

		public IterComparer (Gtk.TreeStore store)
		{
			this.store = store;
		}
		public bool Equals (Gtk.TreeIter x, Gtk.TreeIter y)
		{
			if (!store.IterIsValid (x) || !store.IterIsValid (y))
				return false;
			Gtk.TreePath px = store.GetPath (x);
			Gtk.TreePath py = store.GetPath (y);
			if (px == null || py == null)
				return false;
			return px.Equals (py);
		}

		public int GetHashCode (Gtk.TreeIter obj)
		{
			if (!store.IterIsValid (obj))
				return 0;
			Gtk.TreePath p = store.GetPath (obj);
			if (p == null)
				return 0;
			else
				return p.ToString ().GetHashCode ();
		}
	}
	
	class ZoomableCellRendererPixbuf: Gtk.CellRendererPixbuf
	{
		Gdk.Pixbuf image;
		Gdk.Pixbuf imageOpen;
		Gdk.Pixbuf imageClosed;
		double zoom = 1f;
		
		Dictionary<Gdk.Pixbuf,Gdk.Pixbuf> resizedCache = new Dictionary<Gdk.Pixbuf, Gdk.Pixbuf> ();
		
		public double Zoom {
			get { return zoom; }
			set {
				if (zoom != value) {
					zoom = value;
					resizedCache.Clear ();
					Notify ("image");
				}
			}
		}
		
		[GLib.Property ("image")]
		public Gdk.Pixbuf Image {
			get {
				return image;
			}
			set {
				image = value;
				Pixbuf = GetResized (image);
			}
		}
		
		[GLib.Property ("image-expander-open")]
		public Gdk.Pixbuf ImageExpanderOpen {
			get {
				return imageOpen;
			}
			set {
				imageOpen = value;
				PixbufExpanderOpen = GetResized (imageOpen);
			}
		}
		
		[GLib.Property ("image-expander-closed")]
		public Gdk.Pixbuf ImageExpanderClosed {
			get {
				return imageClosed;
			}
			set {
				imageClosed = value;
				PixbufExpanderClosed = GetResized (imageClosed);
			}
		}
		
		Gdk.Pixbuf GetResized (Gdk.Pixbuf value)
		{
			if (zoom == 1)
				return value;
			
			//this can happen during solution deserialization if the project is unrecognized
			//because a line is added into the treeview with no icon
			if (value == null)
				return null;

			Gdk.Pixbuf resized;
			if (resizedCache.TryGetValue (value, out resized))
				return resized;
			
			int w = (int) (zoom * (double) value.Width);
			int h = (int) (zoom * (double) value.Height);
			if (w == 0) w = 1;
			if (h == 0) h = 1;
			resized = value.ScaleSimple (w, h, Gdk.InterpType.Hyper);
			resizedCache [value] = resized;
			return resized;
		}
	}
}