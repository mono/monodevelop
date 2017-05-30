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
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Extensions;
using System.Linq;
using MonoDevelop.Ide.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ExtensibleTreeView : Control, ICommandRouter
	{
		internal const int NodeInfoColumn = 0;
		internal const int DataItemColumn = 1;
		internal const int BuilderChainColumn = 2;
		internal const int FilledColumn = 3;
		internal const int ShowPopupColumn = 4;

		NodeBuilder[] builders;
		Dictionary<Type, NodeBuilder[]> builderChains = new Dictionary<Type, NodeBuilder[]> ();
		NodeHashtable nodeHash = new NodeHashtable ();

		ExtensibleTreeViewWidget widget;
		ExtensibleTreeViewTree tree;
		Gtk.TreeStore store;
		Gtk.TreeViewColumn complete_column;
		ZoomableCellRendererPixbuf pix_render;
		CustomCellRendererText text_render;
		TreeBuilderContext builderContext;
		Hashtable callbacks = new Hashtable ();
		bool editingText = false;

		TreePadOption[] options;
		TreeOptions globalOptions;

		TreeNodeNavigator workNode;
		TreeNodeNavigator compareNode1;
		TreeNodeNavigator compareNode2;

		internal bool sorting;

		object[] copyObjects;
		DragOperation currentTransferOperation;

		TransactedNodeStore transactionStore;
		int updateLockCount;
		string contextMenuPath;
		IDictionary<string,string> contextMenuTypeNameAliases;

		public IDictionary<string,string> ContextMenuTypeNameAliases {
			get { return contextMenuTypeNameAliases; }
			set { contextMenuTypeNameAliases = value; }
		}

		internal Gtk.TreeStore Store {
			get {
				return this.store;
			}
		}

		internal Gtk.TreeView Tree {
			get {
				return tree;
			}
		}

		public event EventHandler SelectionChanged;

		public bool AllowsMultipleSelection {
			get {
				return Tree.Selection.Mode == Gtk.SelectionMode.Multiple;
			}
			set {
				if (value)
					Tree.Selection.Mode = Gtk.SelectionMode.Multiple;
				else
					Tree.Selection.Mode = Gtk.SelectionMode.Single;
			}
		}

		public string Id { get; set; }


		class ExtensibleTreeViewWidget : CompactScrolledWindow, ICommandRouter
		{
			ExtensibleTreeView control;

			public ExtensibleTreeViewWidget (ExtensibleTreeView control)
			{
				this.control = control;
				ShadowType = Gtk.ShadowType.None;
				ShowBorderLine = false;
			}

			protected override void OnStyleSet (Gtk.Style previous_style)
			{
				base.OnStyleSet (previous_style);
				control.UpdateFont ();
			}

			protected override bool OnScrollEvent (Gdk.EventScroll evnt)
			{
				control.HideStatusMessage ();
				var modifier = !Platform.IsMac? Gdk.ModifierType.ControlMask
				                    //Mac window manager already uses control-scroll, so use command
				                    //Command might be either meta or mod1, depending on GTK version
				                    : (Gdk.ModifierType.MetaMask | Gdk.ModifierType.Mod1Mask);

				if ((evnt.State & modifier) !=0) {
					if (evnt.Direction == Gdk.ScrollDirection.Up)
						control.ZoomIn ();
					else if (evnt.Direction == Gdk.ScrollDirection.Down)
						control.ZoomOut ();

					return true;
				}
				return base.OnScrollEvent (evnt);
			}

			protected override void OnDestroyed ()
			{
				control.Destroy ();
				base.OnDestroyed ();
			}

			public object GetNextCommandTarget ()
			{
				return control;
			}
		}

		protected override object CreateNativeWidget<T> ()
		{
			return widget;
		}

		public ExtensibleTreeView ()
		{
			widget = new ExtensibleTreeViewWidget (this);
			tree = new ExtensibleTreeViewTree (this);
		}

		public ExtensibleTreeView (NodeBuilder[] builders, TreePadOption[] options) : this ()
		{
			Initialize (builders, options);
		}

		void CustomFontPropertyChanged (object sender, EventArgs a)
		{
			UpdateFont ();
		}

		void UpdateFont ()
		{
			text_render.CustomFont = IdeApp.Preferences.CustomPadFont ?? tree.Style.FontDescription;
			tree.ColumnsAutosize ();
		}

		public void Initialize (NodeBuilder [] builders, TreePadOption [] options, string contextMenuPath = null)
		{
			OnInitialize (builders, options, contextMenuPath);
		}

		protected virtual void OnInitialize (NodeBuilder[] builders, TreePadOption[] options, string contextMenuPath)
		{
			this.contextMenuPath = contextMenuPath;
			builderContext = new TreeBuilderContext (this);

			SetBuilders (builders, options);

			store = new Gtk.TreeStore (typeof(NodeInfo), typeof(object), typeof(object), typeof(bool), typeof(bool));
			tree.Model = store;
			tree.Selection.Mode = Gtk.SelectionMode.Multiple;

			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (CompareNodes);
			store.SetSortColumnId (/* GTK_TREE_SORTABLE_DEFAULT_SORT_COLUMN_ID */ -1, Gtk.SortType.Ascending);

			tree.HeadersVisible = false;
			tree.EnableSearch = false;
			complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "column";

			pix_render = new ZoomableCellRendererPixbuf ();
			pix_render.Xpad = 0;
			complete_column.PackStart (pix_render, false);

			text_render = new CustomCellRendererText (this);
			text_render.Ypad = 0;
			IdeApp.Preferences.CustomPadFont.Changed += CustomFontPropertyChanged;
			text_render.EditingStarted += HandleEditingStarted;
			text_render.Edited += HandleOnEdit;
			text_render.EditingCanceled += HandleOnEditCancelled;
			complete_column.PackStart (text_render, true);

			complete_column.SetCellDataFunc (pix_render, SetIconCellData);
			complete_column.SetCellDataFunc (text_render, SetTextCellData);

			tree.AppendColumn (complete_column);

			tree.TestExpandRow += OnTestExpandRow;
			tree.RowActivated += OnNodeActivated;
			tree.DoPopupMenu += ShowPopup;

			// Add an extra action handler to the tree to handle Press actions
			var actionHandler = tree.ActionHandler;
			actionHandler.PerformPress += OnPerformPress;

			workNode = new TreeNodeNavigator (this);
			compareNode1 = new TreeNodeNavigator (this);
			compareNode2 = new TreeNodeNavigator (this);

			tree.CursorChanged += OnSelectionChanged;
			tree.KeyPressEvent += OnKeyPress;
			tree.MotionNotifyEvent += HandleMotionNotifyEvent;
			tree.LeaveNotifyEvent += HandleLeaveNotifyEvent;

			if (GtkGestures.IsSupported) {
				tree.AddGestureMagnifyHandler ((sender, args) => {
					Zoom += Zoom * (args.Magnification / 4d);
				});
			}

			for (int n=3; n<16; n++) {
				Gtk.Rc.ParseString ("style \"MonoDevelop.ExtensibleTreeView_" + n + "\" {\n GtkTreeView::expander-size = " + n + "\n }\n");
				Gtk.Rc.ParseString ("widget \"*.MonoDevelop.ExtensibleTreeView_" + n + "\" style  \"MonoDevelop.ExtensibleTreeView_" + n + "\"\n");
			}

			if (!string.IsNullOrEmpty (Id))
				Zoom = PropertyService.Get<double> ("MonoDevelop.Ide.ExtensibleTreeView.Zoom." + Id, 1d);
			else
				Zoom = 1d;

			widget.Add (tree);
			widget.ShowAll ();

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

		static void SetIconCellData (Gtk.TreeViewColumn col, Gtk.CellRenderer renderer, Gtk.TreeModel model, Gtk.TreeIter it)
		{
			if (model == null)
				return;

			var info = (NodeInfo)model.GetValue (it, NodeInfoColumn);
			var cell = (ZoomableCellRendererPixbuf)renderer;

			var img = info.Icon != null && info.Icon != CellRendererImage.NullImage && info.DisabledStyle ? info.Icon.WithAlpha (0.5) : info.Icon;
			cell.Image = img;
			cell.ImageExpanderOpen = img;
			cell.ImageExpanderClosed = info.ClosedIcon != null && info.ClosedIcon != CellRendererImage.NullImage && info.DisabledStyle ? info.ClosedIcon.WithAlpha (0.5) : info.ClosedIcon;
			cell.OverlayBottomLeft = info.OverlayBottomLeft;
			cell.OverlayBottomRight = info.OverlayBottomRight;
			cell.OverlayTopLeft = info.OverlayTopLeft;
			cell.OverlayTopRight = info.OverlayTopRight;
		}

		static void SetTextCellData (Gtk.TreeViewColumn col, Gtk.CellRenderer renderer, Gtk.TreeModel model, Gtk.TreeIter it)
		{
			if (model == null)
				return;

			var info = (NodeInfo)model.GetValue (it, NodeInfoColumn);
			var cell = (CustomCellRendererText)renderer;
			SetTextCellData (cell, info);
		}

		static void SetTextCellData (CustomCellRendererText cell, NodeInfo info)
		{
			cell.DisabledStyle = info.DisabledStyle;
			cell.TextMarkup = info.Label;
			cell.SecondaryTextMarkup = info.SecondaryLabel;

			cell.StatusIcon = info.StatusIconInternal;
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
					if (tnb.UseReferenceEquality)
						nodeHash.RegisterByRefType (tnb.NodeDataType);
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

		public void EnableDragUriSource (Func<object,string> nodeToUri)
		{
			tree.EnableDragUriSource (nodeToUri);
		}

		object[] GetDragObjects (out Xwt.Drawing.Image icon)
		{
			ITreeNavigator[] navs = GetSelectedNodes ();
			if (navs.Length == 0) {
				icon = null;
				return null;
			}
			var dragObjects = new object [navs.Length];
			for (int n=0; n<navs.Length; n++)
				dragObjects [n] = navs [n].DataItem;
			icon = ((NodeInfo) store.GetValue (navs[0].CurrentPosition._iter, NodeInfoColumn)).Icon;
			return dragObjects;
		}

		bool CheckAndDrop (int x, int y, bool drop, Gdk.DragContext ctx, object[] obj)
		{
			Gtk.TreePath path;
			Gtk.TreeViewDropPosition pos;
			if (!tree.GetDestRowAtPos (x, y, out path, out pos))
				return false;

			Gtk.TreeIter iter;
			if (!store.GetIter (out iter, path))
				return false;

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
			} catch (Exception ex) {
				// We're now in an indeterminate state, so report the exception
				// and exit.
				GLib.ExceptionManager.RaiseUnhandledException (ex, true);
				return false;
			} finally {
				if (updatesLocked)
					UnlockUpdates ();
			}
			return foundHandler;
		}

		[GLib.ConnectBefore]
		void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			Gtk.TreePath path;
			int cx, cy;
			Gtk.TreeViewColumn col;
			bool popupShown = false;

			if (tree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col, out cx, out cy)) {
				Gtk.TreeIter it;
				if (store.GetIter (out it, path)) {
					var info = (NodeInfo)store.GetValue (it, NodeInfoColumn);
					if (info.StatusIconInternal != CellRendererImage.NullImage && info.StatusIconInternal != null) {
						var cellArea = tree.GetCellArea (path, tree.Columns [0]);
						int sp, w;
						col.CellGetPosition (text_render, out sp, out w);
						cellArea.X += sp;
						cellArea.X++; // GetCellArea is off by 1px compared to the cellArea passed to cell renderers directly
						cellArea.Width = w;
						SetTextCellData (text_render, info); // update cell renderer data for size calculations
						var rect = text_render.GetStatusIconArea (tree, cellArea);
						if (cx >= rect.X && cx <= rect.Right) {
							tree.ConvertBinWindowToWidgetCoords (rect.X, rect.Y, out rect.X, out rect.Y);
							ShowStatusMessage (it, rect, info);
							popupShown = true;
						}
					}
				}
			}
			if (!popupShown)
				HideStatusMessage ();
		}

		bool statusMessageVisible;
		Gtk.TreeIter statusIconIter;
		TooltipPopoverWindow statusPopover;

		void ShowStatusMessage (Gtk.TreeIter it, Gdk.Rectangle rect, NodeInfo info)
		{
			if (statusMessageVisible && store.GetPath (it).Equals (store.GetPath (statusIconIter)))
				return;
			if (statusPopover != null)
				statusPopover.Destroy ();
			statusMessageVisible = true;
			statusIconIter = it;

			statusPopover = TooltipPopoverWindow.Create ();
			statusPopover.ShowArrow = true;
			statusPopover.Text = info.StatusMessage;
			statusPopover.Severity = info.StatusSeverity;
			rect.Y += 2;
			statusPopover.ShowPopup (this, rect, PopupPosition.Top);
		}

		void HideStatusMessage ()
		{
			if (statusMessageVisible) {
				statusMessageVisible = false;
				statusPopover.Destroy ();
				statusPopover = null;
			}
		}

		[GLib.ConnectBefore]
		void HandleLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
			HideStatusMessage ();
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

		internal Gtk.TreeViewColumn CompleteColumn {
			get {
				return complete_column;
			}
		}

		NodeHashtable NodeHash {
			get {
				return nodeHash;
			}
		}

		public ITreeBuilderContext BuilderContext {
			get {
				return builderContext;
			}
		}

		internal object[] CopyObjects {
			get {
				return copyObjects;
			}
			set {
				copyObjects = value;
			}
		}

		internal DragOperation CurrentTransferOperation {
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
			copyObjects = tree.dragObjects = null;

			object[] obs = new object [nodeHash.Count];
			nodeHash.Keys.CopyTo (obs, 0);

			foreach (object dataObject in obs)
				NotifyNodeRemoved (dataObject, null);

			nodeHash = new NodeHashtable ();
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
			object it;
			if (!nodeHash.TryGetValue (dataObject, out it)) {
				if (createTreeBranch) {
					TypeNodeBuilder tnb = GetTypeNodeBuilder (dataObject.GetType());
					if (tnb == null) return null;

					object parent = tnb.GetParentObject (dataObject);
					if (parent == null || parent == dataObject || dataObject.Equals (parent)) return null;

					ITreeNavigator pnav = GetNodeAtObject (parent, true);
					if (pnav == null) return null;

					pnav.MoveToFirstChild ();

					// The child should be now in the this. Try again.
					if (!nodeHash.TryGetValue (dataObject, out it))
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

		internal new object GetNextCommandTarget ()
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

				IEnumerable<SelectionGroup> nodeGroups = GetSelectedNodesGrouped ();
				if (nodeGroups.Count () == 1) {
					SelectionGroup grp = nodeGroups.First ();

					if (grp.Nodes.Count () == 1) {
						ITreeNavigator node = grp.Nodes.First ();
						if (node.Expanded) {
							grp.SavePositions ();
							node.Selected = false;
							if (node.MoveToFirstChild ())
								node.Selected = true;

							// This exit statement is so that it doesn't do 2 actions at a time.
							// As in, navigate, then expand.
							return;
						}
					}
				}

				foreach (SelectionGroup grp in nodeGroups) {
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

				IEnumerable<SelectionGroup> nodeGroups = GetSelectedNodesGrouped ();
				if (nodeGroups.Count () == 1) {
					SelectionGroup grp = nodeGroups.First ();

					if (grp.Nodes.Count () == 1)
					{
						ITreeNavigator node = grp.Nodes.First ();
						if (!node.HasChildren () || !node.Expanded) {
							grp.SavePositions ();
							node.Selected = false;
							if (node.MoveToParent ())
								node.Selected = true;

							// This exit statement is so that it doesn't do 2 actions at a time.
							// As in, navigate, then collapse.
							return;
						}
					}
				}

				foreach (SelectionGroup grp in nodeGroups) {
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
		public void ActivateCurrentItem ()
		{
			OnActivateCurrentItem ();
		}

		protected virtual void OnActivateCurrentItem ()
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
				OnCurrentItemActivated ();
			} finally {
				UnlockUpdates ();
			}
		}

		public void DeleteCurrentItem ()
		{
			OnDeleteCurrentItem ();
		}

		protected virtual void OnDeleteCurrentItem ()
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

		[CommandHandler (ViewCommands.RefreshTree)]
		public void RefreshCurrentItem ()
		{
			OnRefreshCurrentItem ();
		}

		protected virtual void OnRefreshCurrentItem ()
		{
			try {
				LockUpdates ();
				foreach (SelectionGroup grp in GetSelectedNodesGrouped ()) {
					NodeBuilder[] chain = grp.BuilderChain;
					grp.SavePositions ();
					foreach (NodeBuilder b in chain) {
						NodeCommandHandler handler = b.CommandHandler;
						handler.SetCurrentNodes (grp.Nodes.ToArray ());
						if (!grp.RestorePositions ())
							return;
						handler.RefreshMultipleItems ();
						if (!grp.RestorePositions ())
							return;
					}
				}
			} finally {
				UnlockUpdates ();
			}
			RefreshTree ();
		}

		protected virtual void OnCurrentItemActivated ()
		{
			if (CurrentItemActivated != null)
				CurrentItemActivated (this, EventArgs.Empty);
		}

		public event EventHandler CurrentItemActivated;

		#region Zoom

		const double ZOOM_FACTOR = 1.1f;
		const int ZOOM_MIN_POW = -4;
		const int ZOOM_MAX_POW = 8;
		static readonly double ZOOM_MIN = System.Math.Pow (ZOOM_FACTOR, ZOOM_MIN_POW);
		static readonly double ZOOM_MAX = System.Math.Pow (ZOOM_FACTOR, ZOOM_MAX_POW);
		double zoom;

		public double Zoom {
			get {
				 return zoom;
			}
			set {
				value = System.Math.Min (ZOOM_MAX, System.Math.Max (ZOOM_MIN, value));
				if (value > ZOOM_MAX || value < ZOOM_MIN)
					return;
				//snap to one, if within 0.001d
				if ((System.Math.Abs (value - 1d)) < 0.001d) {
					value = 1d;
				}
				if (zoom != value) {
					zoom = value;
					OnZoomChanged (value);
				}
			}
		}

		void OnZoomChanged (double value)
		{
			pix_render.Zoom = value;
			text_render.Zoom = value;

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
			}
		}

		[CommandHandler (ViewCommands.ZoomIn)]
		public void ZoomIn ()
		{
			int oldPow = (int)System.Math.Round (System.Math.Log (zoom) / System.Math.Log (ZOOM_FACTOR));
			Zoom = System.Math.Pow (ZOOM_FACTOR, oldPow + 1);
		}

		[CommandHandler (ViewCommands.ZoomOut)]
		public void ZoomOut ()
		{
			int oldPow = (int)System.Math.Round (System.Math.Log (zoom) / System.Math.Log (ZOOM_FACTOR));
			Zoom = System.Math.Pow (ZOOM_FACTOR, oldPow - 1);
		}

		[CommandHandler (ViewCommands.ZoomReset)]
		public void ZoomReset ()
		{
			Zoom = 1d;
		}

		[CommandUpdateHandler (ViewCommands.ZoomIn)]
		protected void UpdateZoomIn (CommandInfo cinfo)
		{
			cinfo.Enabled = zoom < ZOOM_MAX - 0.000001d;
		}

		[CommandUpdateHandler (ViewCommands.ZoomOut)]
		protected void UpdateZoomOut (CommandInfo cinfo)
		{
			cinfo.Enabled = zoom > ZOOM_MIN + 0.000001d;
		}

		[CommandUpdateHandler (ViewCommands.ZoomReset)]
		protected void UpdateZoomReset (CommandInfo cinfo)
		{
			cinfo.Enabled = zoom != 1d;
		}

		#endregion Zoom

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
		internal void UpdateCopyCurrentItem (CommandInfo info)
		{
			if (editingText) {
				info.Bypass = true;
				return;
			}
			info.Enabled = CanTransferCurrentItem (DragOperation.Copy);
		}

		[CommandUpdateHandler (EditCommands.Cut)]
		internal void UpdateCutCurrentItem (CommandInfo info)
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
		internal void UpdatePasteToCurrentItem (CommandInfo info)
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

		NodeInfo GetNodeInfo (Gtk.TreeIter it)
		{
			return (NodeInfo)store.GetValue (it, NodeInfoColumn);
		}

		void StartLabelEditInternal()
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

			GetNodeInfo (iter).Label = GLib.Markup.EscapeText (nodeName);
			store.EmitRowChanged (store.GetPath (iter), iter);

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
			// Ensure we set all our state variables before calling SetCursor
			// as this may directly invoke HandleOnEditCancelled
			text_render.Editable = true;
			editingText = true;
			tree.SetCursor (store.GetPath (iter), complete_column, true);
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
								LoggingService.LogInternalError (ex);
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
				LoggingService.LogInternalError ("The item could not be renamed", ex);
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

			var rootState = NodeState.CreateRoot ();
			List<NodeState> children = new List<NodeState> ();
			rootState.ChildrenState = children;

			var s = new Dictionary<string, bool> ();
			foreach (TreePadOption opt in options) {
				bool val;
				if (globalOptions.TryGetValue (opt.Id, out val) && val != opt.DefaultValue)
					s [opt.Id] = val;
			}
			if (s.Count != 0)
				rootState.Options = s;

			do {
				rootState.ChildrenState.Add (root.SaveState ());
			} while (root.MoveNext ());

			return rootState;
		}

		public void RestoreTreeState (NodeState state)
		{
			if (state == null)
				return;

			ITreeNavigator nav = GetRootNode ();
			if (nav == null)
				return;

			if (state.IsRoot) {
				if (state.ChildrenState != null) {
					var pos = nav.CurrentPosition;
					foreach (NodeState ces in state.ChildrenState) {
						do {
							if (nav.NodeName == ces.NodeName) {
								nav.RestoreState (ces);
								break;
							}
						} while (nav.MoveNext ());
						nav.MoveToPosition (pos);
					}
				}
			} else
				nav.RestoreState (state);

			globalOptions = new TreeOptions ();
			foreach (TreePadOption opt in options) {
				bool val = false;
				if (state.Options == null || !state.Options.TryGetValue (opt.Id, out val))
					val = opt.DefaultValue;
				globalOptions[opt.Id] = val;
			}
			globalOptions.Pad = this;
			RefreshTree ();
		}

		TypeNodeBuilder GetTypeNodeBuilder (Type type)
		{
			NodeBuilder[] chain = GetBuilderChain (type);
			if (chain == null) return null;
			return (TypeNodeBuilder) chain [0];
		}

		internal NodeBuilder[] GetBuilderChain (Type type)
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

				NodeBuilder [] chain2 = (NodeBuilder [])store.GetValue (b, BuilderChainColumn);
				if (chain2 == null) return 1;

				compareNode1.MoveToIter (a);
				compareNode2.MoveToIter (b);

				var i1 = GetSortIndex (chain1, compareNode1);
				var i2 = GetSortIndex (chain2, compareNode2);

				if (i1 < i2)
					return -1;
				else if (i1 > i2)
					return 1;

				int sort = CompareObjects (chain1, compareNode1, compareNode2);
				if (sort != TypeNodeBuilder.DefaultSort) return sort;

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
				compareNode1.MoveToIter (Gtk.TreeIter.Zero);
				compareNode2.MoveToIter (Gtk.TreeIter.Zero);
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

		int GetSortIndex (NodeBuilder [] chain, ITreeNavigator node)
		{
			int result = 0;
			for (int n = 0; n < chain.Length; n++) {
				int sort = chain [n].GetSortIndex (node);
				if (sort != NodeBuilder.DefaultSort)
					result = sort;
			}
			return result;
		}

		internal bool GetFirstNode (object dataObject, out Gtk.TreeIter iter)
		{
			object it;
			if (!nodeHash.TryGetValue (dataObject, out it)) {
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
			object it;
			if (!nodeHash.TryGetValue (dataObject, out it))
				return false;
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
			object currentIt;
			if (!nodeHash.TryGetValue (dataObject, out currentIt)) {
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

			if (tree.dragObjects != null) {
				int i = Array.IndexOf (tree.dragObjects, dataObject);
				if (i != -1) {
					ArrayList list = new ArrayList (tree.dragObjects);
					list.RemoveAt (i);
					if (list.Count > 0)
						tree.dragObjects = list.ToArray ();
					else
						tree.dragObjects = null;
				}
			}

			object currentIt;
			nodeHash.TryGetValue (dataObject, out currentIt);
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
			return nodeHash.ContainsKey (dataObject);
		}

		internal void NotifyInserted (Gtk.TreeIter it, object dataObject)
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

			workNode.MoveToIter (Gtk.TreeIter.Zero);

			return sb.ToString ();
		}

		void RefreshNode (Gtk.TreeIter iter)
		{
			ITreeBuilder builder = CreateBuilder (iter);
			builder.UpdateAll ();
		}

		public void RefreshNode (ITreeNavigator nav)
		{
			RefreshNode (nav.CurrentPosition._iter);
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
		internal void UpdateStartLabelEdit (CommandInfo info)
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
			var entryset = BuildEntrySet ();
			if (entryset == null)
				return;

			tree.ShowContextMenu (evt, entryset, this);
		}

		CommandEntrySet BuildEntrySet ()
		{
			ITreeNavigator tnav = GetSelectedNode ();
			if (tnav == null)
				return null;
			TypeNodeBuilder nb = GetTypeNodeBuilder (tnav.CurrentPosition._iter);
			string menuPath = nb != null && nb.ContextMenuAddinPath != null ? nb.ContextMenuAddinPath : contextMenuPath;
			if (menuPath == null) {
				if (options.Length > 0) {
					CommandEntrySet opset = new CommandEntrySet ();
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
					return opset;
				}
				return null;
			} else {
				ExtensionContext ctx = AddinManager.CreateExtensionContext ();
				ctx.RegisterCondition ("ItemType", new ItemTypeCondition (tnav.DataItem.GetType (), contextMenuTypeNameAliases));
				CommandEntrySet eset = IdeApp.CommandService.CreateCommandEntrySet (ctx, menuPath);

				eset.AddItem (Command.Separator);
				if (!tnav.Clone ().MoveToParent ()) {
					CommandEntrySet opset = eset.AddItemSet (GettextCatalog.GetString ("Display Options"));
					opset.AddItem (ViewCommands.TreeDisplayOptionList);
					opset.AddItem (Command.Separator);
					opset.AddItem (ViewCommands.ResetTreeDisplayOptions);
				//	opset.AddItem (ViewCommands.CollapseAllTreeNodes);
				}
				eset.AddItem (ViewCommands.RefreshTree);
				return eset;
			}
		}

		[CommandUpdateHandler (ViewCommands.TreeDisplayOptionList)]
		internal void BuildTreeOptionsMenu (CommandArrayInfo info)
		{
			foreach (TreePadOption op in options) {
				CommandInfo ci = new CommandInfo (op.Label);
				ci.Checked = globalOptions [op.Id];
				info.Add (ci, op.Id);
			}
		}

		[CommandHandler (ViewCommands.TreeDisplayOptionList)]
		internal void OptionToggled (string optionId)
		{
			globalOptions [optionId] = !globalOptions [optionId];
			RefreshRoots ();
		}

		[CommandHandler (ViewCommands.ResetTreeDisplayOptions)]
		public void ResetOptions ()
		{
			foreach (TreePadOption op in options)
				globalOptions [op.Id] = op.DefaultValue;

			RefreshRoots ();
		}

		void RefreshRoots ()
		{
			Gtk.TreeIter it;
			if (!store.GetIterFirst (out it))
				return;
			do {
				ITreeBuilder tb = CreateBuilder (it);
				tb.UpdateAll ();
			} while (store.IterNext (ref it));
		}

		public void RefreshTree ()
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
		public void CollapseTree ()
		{
			tree.CollapseAll();
		}

		[GLib.ConnectBefore]
		void OnKeyPress (object o, Gtk.KeyPressEventArgs args)
		{
			HideStatusMessage ();
			
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

			if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter || args.Event.Key == Gdk.Key.ISO_Enter) {
				ActivateCurrentItem ();
				args.RetVal = true;
				return;
			}
		}

		[CommandHandler (SearchCommands.Find)]
		void HandleSearchCommand ()
		{
			IdeApp.CommandService.DispatchCommand (MonoDevelop.Components.MainToolbar.Commands.NavigateTo);
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

		void OnNodeActivated (object sender, Gtk.RowActivatedArgs args)
		{
			ActivateCurrentItem ();
		}

		void OnPerformPress (object sender, EventArgs args)
		{
			ActivateCurrentItem ();
		}

		void OnSelectionChanged (object sender, EventArgs args)
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
			OnSelectionChanged ();
		}

		protected virtual void OnSelectionChanged ()
		{
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		void Destroy ()
		{
			IdeApp.Preferences.CustomPadFont.Changed -= CustomFontPropertyChanged;
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
		}

		object ICommandRouter.GetNextCommandTarget ()
		{
			return widget.Parent;
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

			public Xwt.Drawing.Image GetIcon (string id)
			{
				Xwt.Drawing.Image icon = icons [id] as Xwt.Drawing.Image;
				if (icon == null) {
					icon = ImageService.GetIcon (id).WithSize (Gtk.IconSize.Menu);
					icons [id] = icon;
				}
				return icon;
			}

			public Xwt.Drawing.Image GetComposedIcon (Xwt.Drawing.Image baseIcon, object compositionKey)
			{
				Hashtable itable = composedIcons [baseIcon] as Hashtable;
				if (itable == null) return null;
				return itable [compositionKey] as Xwt.Drawing.Image;
			}

			public Xwt.Drawing.Image CacheComposedIcon (Xwt.Drawing.Image baseIcon, object compositionKey, Xwt.Drawing.Image composedIcon)
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

		class ExtensibleTreeViewTree : ContextMenuTreeView
		{
			ExtensibleTreeView tv;

			public ExtensibleTreeViewTree (ExtensibleTreeView tv)
			{
				this.tv = tv;
				EnableModelDragDest (targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
				Gtk.Drag.SourceSet (this, Gdk.ModifierType.Button1Mask, targetTable, Gdk.DragAction.Copy | Gdk.DragAction.Move);
			}

			static Gtk.TargetEntry [] targetTable = new Gtk.TargetEntry [] {
				new Gtk.TargetEntry ("text/uri-list", 0, 11 ),
				new Gtk.TargetEntry ("text/plain", 0, 22),
				new Gtk.TargetEntry ("application/x-rootwindow-drop", 0, 33)
			};

			public object[] dragObjects = null;
			bool dropping = false;
			Func<object,string> nodeToUri;

			public void EnableDragUriSource (Func<object,string> nodeToUri)
			{
				this.nodeToUri = nodeToUri;
			}

			protected override void OnDragBegin (Gdk.DragContext context)
			{
				Xwt.Drawing.Image dragIcon;
				dragObjects = tv.GetDragObjects (out dragIcon);
				Gtk.Drag.SetIconPixbuf (context, dragIcon != null ? dragIcon.ToPixbuf (Gtk.IconSize.Menu) : null, -10, -10);

				base.OnDragBegin (context);
			}

			protected override void OnDragEnd (Gdk.DragContext context)
			{
				dragObjects = null;
				base.OnDragEnd (context);
			}

			protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
			{
				//OnDragDataReceived callback loses x/y values, so stash them
				this.x = x;
				this.y = y;

				if (dragObjects == null) {
					//it's a drag from outside, need to retrieve the data. This will cause OnDragDataReceived to be called.
					Gdk.Atom atom = Gtk.Drag.DestFindTarget (this, context, null);
					Gtk.Drag.GetData (this, context, atom, time);
				} else {
					//it's from inside, can call OnDragDataReceived directly
					OnDragDataReceived (context, x, y, null, 0, time);
				}
				return true;
			}

			int x, y;

			protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y, Gtk.SelectionData selection_data, uint info, uint time)
			{
				x = this.x;
				y = this.y;

				object[] data = dragObjects ?? new object[] { selection_data };
				bool canDrop = tv.CheckAndDrop (x, y, dropping, context, data);
				if (dropping) {
					dropping = false;
					SetDragDestRow (null, 0);
					Gtk.Drag.Finish (context, canDrop, true, time);
					return;
				}

				//let default handler handle hover-to-expand, autoscrolling, etc
				base.OnDragMotion (context, x, y, time);

				//if we can't handle it, flag as not droppable and remove the drop marker
				if (!canDrop) {
					Gdk.Drag.Status (context, (Gdk.DragAction)0, time);
					SetDragDestRow (null, 0);
				}
			}

			protected override bool OnDragDrop (Gdk.DragContext context, int x, int y, uint time_)
			{
				dropping = true;
				return base.OnDragDrop (context, x, y, time_);
			}

			protected override void OnDragDataGet (Gdk.DragContext context, Gtk.SelectionData selection_data, uint info, uint time_)
			{
				if (dragObjects == null || nodeToUri == null)
					return;

				uint uriListTarget = targetTable [0].Info;
				if (info == uriListTarget) {
					var sb = new StringBuilder ();
					foreach (var dobj in dragObjects) {
						var val = nodeToUri (dobj);
						if (val != null) {
							sb.AppendLine (val);
						}
					}
					selection_data.Set (selection_data.Target, selection_data.Format, Encoding.UTF8.GetBytes (sb.ToString ()));
				}
			}
		}

		class CustomCellRendererText: Gtk.CellRendererText
		{
			double zoom;
			Pango.Layout layout;
			Pango.FontDescription scaledFont, customFont;

			ExtensibleTreeView parent;
			Gdk.Rectangle buttonScreenRect;
			Gdk.Rectangle buttonAllocation;
			string markup;
			string secondarymarkup;

			const int StatusIconSpacing = 4;

			public bool Pushed { get; set; }

			//using this instead of FontDesc property, FontDesc seems to be broken
			public Pango.FontDescription CustomFont {
				get {
					return customFont;
				}
				set {
					if (scaledFont != null) {
						scaledFont.Dispose ();
						scaledFont = null;
					}
					customFont = value;
				}
			}

			static CustomCellRendererText ()
			{
			}

			[GLib.Property ("text-markup")]
			public string TextMarkup {
				get { return markup; }
				set {
					markup = value;
					if (!string.IsNullOrEmpty (secondarymarkup))
						Markup = markup + " " + secondarymarkup;
					else
						Markup = markup;
				}
			}

			[GLib.Property ("secondary-text-markup")]
			public string SecondaryTextMarkup {
				get { return secondarymarkup; }
				set {
					secondarymarkup = value;
					if (!string.IsNullOrEmpty (secondarymarkup))
						Markup = markup + " " + secondarymarkup;
					else
						Markup = markup;
				}
			}

			public bool DisabledStyle { get; set; }

			[GLib.Property ("status-icon")]
			public Xwt.Drawing.Image StatusIcon { get; set; }

			public CustomCellRendererText (ExtensibleTreeView parent)
			{
				this.parent = parent;
			}

			static Xwt.Size defaultIconSize = Gtk.IconSize.Menu.GetSize ();

			static Xwt.Size GetZoomedIconSize (Xwt.Drawing.Image icon, double zoom)
			{
				if (icon == null || icon == CellRendererImage.NullImage)
						return defaultIconSize;

				var size = icon.HasFixedSize ? icon.Size : defaultIconSize;

				if (zoom == 1)
					return size;

				int w = (int) (zoom * (double) size.Width);
				int h = (int) (zoom * (double) size.Height);
				if (w == 0) w = 1;
				if (h == 0) h = 1;
				return new Xwt.Size (w, h);
			}

			static Xwt.Drawing.Image GetResized (Xwt.Drawing.Image icon, double zoom)
			{
				var size = GetZoomedIconSize (icon, zoom);
				return icon.WithSize (size);
			}

			void SetupLayout (Gtk.Widget widget, Gtk.CellRendererState flags = 0)
			{
				if (scaledFont == null) {
					if (scaledFont != null)
						scaledFont.Dispose ();
					scaledFont = (customFont ?? parent.widget.Style.FontDesc).Copy ();
					scaledFont.Size = (int)(customFont.Size * Zoom);
					if (layout != null)
						layout.FontDescription = scaledFont;
				}

				if (layout == null || layout.Context != widget.PangoContext) {
					if (layout != null)
						layout.Dispose ();
					layout = new Pango.Layout (widget.PangoContext);
					layout.FontDescription = scaledFont;
				}

				string newmarkup = TextMarkup;
				if (DisabledStyle) {
					Gdk.Color fgColor;
					if (Platform.IsMac && flags.HasFlag (Gtk.CellRendererState.Selected))
						fgColor = widget.Style.Text (IdeTheme.UserInterfaceTheme == Theme.Light ? Gtk.StateType.Selected : Gtk.StateType.Normal);
					else
						fgColor = widget.Style.Text (Gtk.StateType.Insensitive);
					newmarkup = "<span foreground='" + fgColor.GetHex () + "'>" + TextMarkup + "</span>";
				}

				if (!string.IsNullOrEmpty (SecondaryTextMarkup)) {
					if (Platform.IsMac && flags.HasFlag (Gtk.CellRendererState.Selected))
						newmarkup += " <span foreground='" + Styles.SecondarySelectionTextColor.ToHexString (false) + "'>" + SecondaryTextMarkup + "</span>";
					else
						newmarkup += " <span foreground='" + Styles.SecondaryTextColor.ToHexString (false) + "'>" + SecondaryTextMarkup + "</span>";
				}

				layout.SetMarkup (newmarkup);
			}

			protected override void Render (Gdk.Drawable window, Gtk.Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, Gtk.CellRendererState flags)
			{
				Gtk.StateType st = Gtk.StateType.Normal;
				if ((flags & Gtk.CellRendererState.Prelit) != 0)
					st = Gtk.StateType.Prelight;
				if ((flags & Gtk.CellRendererState.Focused) != 0)
					st = Gtk.StateType.Normal;
				if ((flags & Gtk.CellRendererState.Insensitive) != 0)
					st = Gtk.StateType.Insensitive;
				if ((flags & Gtk.CellRendererState.Selected) != 0)
					st = widget.HasFocus ? Gtk.StateType.Selected : Gtk.StateType.Active;

				SetupLayout (widget, flags);

				int w, h;
				layout.GetPixelSize (out w, out h);

				int tx = cell_area.X + (int)Xpad;
				int ty = cell_area.Y + (cell_area.Height - h) / 2;

				bool hasStatusIcon = StatusIcon != CellRendererImage.NullImage && StatusIcon != null;

				if (hasStatusIcon) {
					var img = GetResized (StatusIcon, zoom);
					if (st == Gtk.StateType.Selected)
						img = img.WithStyles ("sel");
					var x = tx + w + StatusIconSpacing;
					using (var ctx = Gdk.CairoHelper.Create (window)) {
						ctx.DrawImage (widget, img, x, cell_area.Y + (cell_area.Height - img.Height) / 2);
					}
				}

				window.DrawLayout (widget.Style.TextGC (st), tx, ty, layout);

				hasStatusIcon = false;
			}

			public Gdk.Rectangle GetStatusIconArea (Gtk.Widget widget, Gdk.Rectangle cell_area)
			{
				SetupLayout (widget);

				int w, h;
				layout.GetPixelSize (out w, out h);

				var iconSize = GetZoomedIconSize (StatusIcon, zoom);
				int tx = cell_area.X + (int)Xpad;
				var x = tx + w + StatusIconSpacing;
				return new Gdk.Rectangle (x, cell_area.Y, (int) iconSize.Width, (int) cell_area.Height);
			}

			public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				SetupLayout (widget);

				x_offset = y_offset = 0;

				layout.GetPixelSize (out width, out height);
				width += (int)Xpad * 2;

				if (StatusIcon != CellRendererImage.NullImage && StatusIcon != null) {
					var iconSize = GetZoomedIconSize (StatusIcon, zoom);
					width += (int)iconSize.Width + StatusIconSpacing;
				}
			}

			protected override void OnEditingStarted (Gtk.CellEditable editable, string path)
			{
				var entry = editable as Gtk.Entry;
				if (entry != null && scaledFont != null)
					entry.ModifyFont (scaledFont);
				base.OnEditingStarted (editable, path);
			}

			public double Zoom {
				get {
					return zoom;
				}
				set {
					if (scaledFont != null) {
						scaledFont.Dispose ();
						scaledFont = null;
					}
					zoom = value;
				}
			}

			public bool PointerInButton (int px, int py)
			{
				return buttonScreenRect.Contains (px, py);
			}

			public Gdk.Point PointerPosition { get; set; }

			public Gdk.Rectangle PopupAllocation {
				get { return buttonAllocation; }
			}

			protected override void OnDestroyed ()
			{
				base.OnDestroyed ();
				if (scaledFont != null)
					scaledFont.Dispose ();
				if (layout != null)
					layout.Dispose ();
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

	class ZoomableCellRendererPixbuf: CellRendererImage
	{
		double zoom = 1f;

		Dictionary<Xwt.Drawing.Image,Xwt.Drawing.Image> resizedCache = new Dictionary<Xwt.Drawing.Image, Xwt.Drawing.Image> ();

		Xwt.Drawing.Image overlayBottomLeft;
		Xwt.Drawing.Image overlayBottomRight;
		Xwt.Drawing.Image overlayTopLeft;
		Xwt.Drawing.Image overlayTopRight;

		public ZoomableCellRendererPixbuf () : base ()
		{
		}

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

		public override Xwt.Drawing.Image Image {
			get {
				return base.Image;
			}
			set {
				base.Image = GetResized (value);
			}
		}

		public override Xwt.Drawing.Image ImageExpanderOpen {
			get {
				return base.ImageExpanderOpen;
			}
			set {
				base.ImageExpanderOpen = GetResized (value);
			}
		}

		public override Xwt.Drawing.Image ImageExpanderClosed {
			get {
				return base.ImageExpanderClosed;
			}
			set {
				base.ImageExpanderClosed = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-top-left")]
		public Xwt.Drawing.Image OverlayTopLeft {
			get {
				return overlayTopLeft;
			}
			set {
				overlayTopLeft = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-top-right")]
		public Xwt.Drawing.Image OverlayTopRight {
			get {
				return overlayTopRight;
			}
			set {
				overlayTopRight = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-bottom-left")]
		public Xwt.Drawing.Image OverlayBottomLeft {
			get {
				return overlayBottomLeft;
			}
			set {
				overlayBottomLeft = GetResized (value);
			}
		}

		[GLib.Property ("overlay-image-bottom-right")]
		public Xwt.Drawing.Image OverlayBottomRight {
			get {
				return overlayBottomRight;
			}
			set {
				overlayBottomRight = GetResized (value);
			}
		}

		Xwt.Drawing.Image GetResized (Xwt.Drawing.Image value)
		{
			//this can happen during solution deserialization if the project is unrecognized
			//because a line is added into the treeview with no icon
			if (value == null || value == CellRendererImage.NullImage)
				return null;

			var img = value.HasFixedSize ? value : value.WithSize (Gtk.IconSize.Menu);

			if (zoom == 1)
				return img;

			Xwt.Drawing.Image resized;
			if (resizedCache.TryGetValue (img, out resized))
				return resized;

			int w = (int) (zoom * (double) img.Width);
			int h = (int) (zoom * (double) img.Height);
			if (w == 0) w = 1;
			if (h == 0) h = 1;
			resized = img.WithSize (w, h);
			resizedCache [img] = resized;
			return resized;
		}

		public override void GetSize (Gtk.Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			base.GetSize (widget, ref cell_area, out x_offset, out y_offset, out width, out height);
			/*			if (overlayBottomLeft != null || overlayBottomRight != null)
				height += overlayOverflow;
			if (overlayTopLeft != null || overlayTopRight != null)
				height += overlayOverflow;
			if (overlayBottomRight != null || overlayTopRight != null)
				width += overlayOverflow;*/
		}

		const int overlayOverflow = 2;

		protected override void Render (Gdk.Drawable window, Gtk.Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, Gtk.CellRendererState flags)
		{
			base.Render (window, widget, background_area, cell_area, expose_area, flags);

			if (overlayBottomLeft != null || overlayBottomRight != null || overlayTopLeft != null || overlayTopRight != null) {
				int x, y;
				Xwt.Drawing.Image image;
				GetImageInfo (cell_area, out image, out x, out y);

				if (image == null)
					return;

				bool selected = (flags & Gtk.CellRendererState.Selected) != 0;

				using (var ctx = Gdk.CairoHelper.Create (window)) {
					if (overlayBottomLeft != null && overlayBottomLeft != NullImage) {
						var img = selected ? overlayBottomLeft.WithStyles ("sel") : overlayBottomLeft;
						ctx.DrawImage (widget, img, x - overlayOverflow, y + image.Height - img.Height + overlayOverflow);
					}
					if (overlayBottomRight != null && overlayBottomRight != NullImage) {
						var img = selected ? overlayBottomRight.WithStyles ("sel") : overlayBottomRight;
						ctx.DrawImage (widget, img, x + image.Width - img.Width + overlayOverflow, y + image.Height - img.Height + overlayOverflow);
					}
					if (overlayTopLeft != null && overlayTopLeft != NullImage) {
						var img = selected ? overlayTopLeft.WithStyles ("sel") : overlayTopLeft;
						ctx.DrawImage (widget, img, x - overlayOverflow, y - overlayOverflow);
					}
					if (overlayTopRight != null && overlayTopRight != NullImage) {
						var img = selected ? overlayTopRight.WithStyles ("sel") : overlayTopRight;
						ctx.DrawImage (widget, img, x + image.Width - img.Width + overlayOverflow, y - overlayOverflow);
					}
				}
			}
		}
	}

	class NodeHashtable: Dictionary<object,object>
	{
		// This dictionary can be configured to use object reference equality
		// instead of regular object equality for a specific set of types

		NodeComparer nodeComparer;

		public NodeHashtable (): base (new NodeComparer ())
		{
			nodeComparer = (NodeComparer)Comparer;
		}

		/// <summary>
		/// Sets that the objects of the specified type have to be compared
		/// using object reference equality
		/// </summary>
		public void RegisterByRefType (Type type)
		{
			nodeComparer.byRefTypes.Add (type);
		}

		class NodeComparer: IEqualityComparer<object>
		{
			public HashSet<Type> byRefTypes = new HashSet<Type> ();
			public Dictionary<Type,bool> typeData = new Dictionary<Type, bool> ();

			bool IEqualityComparer<object>.Equals (object x, object y)
			{
				if (CompareByRef (x.GetType ()))
				    return x == y;
				else
					return x.Equals (y);
			}

			int IEqualityComparer<object>.GetHashCode (object obj)
			{
				if (CompareByRef (obj.GetType ()))
					return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode (obj);
				else
					return obj.GetHashCode ();
			}

			bool CompareByRef (Type type)
			{
				if (byRefTypes.Count == 0)
					return false;

				bool compareRef;
				if (!typeData.TryGetValue (type, out compareRef)) {
					compareRef = false;
					var t = type;
					while (t != null) {
						if (byRefTypes.Contains (t)) {
							compareRef = true;
							break;
						}
						t = t.BaseType;
					}
					typeData [type] = compareRef;
				}
				return compareRef;
			}
		}
	}
}
