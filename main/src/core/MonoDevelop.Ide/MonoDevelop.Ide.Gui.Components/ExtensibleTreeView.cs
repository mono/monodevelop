//
// ExtensibleTreeGtkBackend2.cs
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
using System.Collections.Generic;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using Mono.TextEditor;


namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class ExtensibleTreeGtkBackend: ExtensibleTreeViewBackend
	{
		internal const int TextColumn         = 0;
		internal const int OpenIconColumn     = 1;
		internal const int ClosedIconColumn   = 2;
		internal const int DataItemColumn     = 3;
		internal const int BuilderChainColumn = 4;
		internal const int FilledColumn       = 5;
		internal const int ShowPopupColumn    = 6;
		internal const int OverlayBottomRightColumn = 7;
		internal const int OverlayBottomLeftColumn  = 8;
		internal const int OverlayTopLeftColumn     = 9;
		internal const int OverlayTopRightColumn    = 10;

		ExtensibleTreeViewTree tree;
		Gtk.TreeStore store;
		Gtk.TreeViewColumn complete_column;
		ZoomableCellRendererPixbuf pix_render;
		CustomCellRendererText text_render;
		bool editingText = false;
		bool showSelectionPopupButton;
		Gtk.TreeIter? lastPopupButtonIter;

		GtkTreeNodeNavigator compareNode1;
		GtkTreeNodeNavigator compareNode2;
		internal bool sorting;

		CompactScrolledWindow scrollWindow;

		IExtensibleTreeViewFrontend frontend;

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

		public IExtensibleTreeViewFrontend Frontend {
			get { return frontend; }
		}

		public override void Initialize (IExtensibleTreeViewFrontend frontend)
		{
			this.frontend = frontend;

			/*
			0 -- Text
			1 -- Icon (Open)
			2 -- Icon (Closed)
			3 -- Node Data
			4 -- Builder chain
			5 -- Expanded
			*/
			tree = new ExtensibleTreeViewTree (this);
			store = new Gtk.TreeStore (typeof(string), typeof(Xwt.Drawing.Image), typeof(Xwt.Drawing.Image), typeof(object), typeof(object), typeof(bool), typeof(bool), typeof(Xwt.Drawing.Image), typeof(Xwt.Drawing.Image), typeof(Xwt.Drawing.Image), typeof(Xwt.Drawing.Image));
			tree.Model = store;
			tree.Selection.Mode = Gtk.SelectionMode.Multiple;

			store.DefaultSortFunc = new Gtk.TreeIterCompareFunc (CompareNodes);
			store.SetSortColumnId (/* GTK_TREE_SORTABLE_DEFAULT_SORT_COLUMN_ID */ -1, Gtk.SortType.Ascending);

			tree.HeadersVisible = false;
			tree.SearchColumn = 0;
			tree.EnableSearch = true;
			complete_column = new Gtk.TreeViewColumn ();
			complete_column.Title = "column";

			pix_render = new ZoomableCellRendererPixbuf ();
			pix_render.Xpad = 0;
			complete_column.PackStart (pix_render, false);
			complete_column.AddAttribute (pix_render, "image", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "image-expander-open", OpenIconColumn);
			complete_column.AddAttribute (pix_render, "image-expander-closed", ClosedIconColumn);
			complete_column.AddAttribute (pix_render, "overlay-image-bottom-left", OverlayBottomLeftColumn);
			complete_column.AddAttribute (pix_render, "overlay-image-bottom-right", OverlayBottomRightColumn);
			complete_column.AddAttribute (pix_render, "overlay-image-top-left", OverlayTopLeftColumn);
			complete_column.AddAttribute (pix_render, "overlay-image-top-right", OverlayTopRightColumn);

			text_render = new CustomCellRendererText (this);
			text_render.Ypad = 0;
			text_render.EditingStarted += HandleEditingStarted;
			text_render.Edited += HandleOnEdit;
			text_render.EditingCanceled += HandleOnEditCancelled;

			complete_column.PackStart (text_render, true);
			complete_column.AddAttribute (text_render, "text-markup", TextColumn);
			complete_column.AddAttribute (text_render, "show-popup-button", ShowPopupColumn);

			tree.AppendColumn (complete_column);

			tree.TestExpandRow += OnTestExpandRow;
			tree.RowActivated += OnNodeActivated;
			tree.DoPopupMenu += ShowPopup;
			compareNode1 = new GtkTreeNodeNavigator (this);
			compareNode2 = new GtkTreeNodeNavigator (this);

			tree.CursorChanged += OnSelectionChanged;
			tree.KeyPressEvent += OnKeyPress;
			tree.ButtonPressEvent += HandleButtonPressEvent;
			tree.MotionNotifyEvent += HandleMotionNotifyEvent;
			tree.LeaveNotifyEvent += HandleLeaveNotifyEvent;

			for (int n=3; n<16; n++) {
				Gtk.Rc.ParseString ("style \"MonoDevelop.ExtensibleTreeView_" + n + "\" {\n GtkTreeView::expander-size = " + n + "\n }\n");
				Gtk.Rc.ParseString ("widget \"*.MonoDevelop.ExtensibleTreeView_" + n + "\" style  \"MonoDevelop.ExtensibleTreeView_" + n + "\"\n");
			}

			scrollWindow = new CompactScrolledWindow ();
			scrollWindow.Add (tree);
			scrollWindow.ShowAll ();

			scrollWindow.Destroyed += HandleDestroyed;
			scrollWindow.StyleSet += delegate {
				UpdateFont ();
			};
		}

		public override bool AllowsMultipleSelection {
			get { return tree.Selection.Mode == Gtk.SelectionMode.Multiple; }
			set {
				if (value)
					tree.Selection.Mode = Gtk.SelectionMode.Multiple;
				else
					tree.Selection.Mode = Gtk.SelectionMode.Single;
			}
		}

		void HandleDestroyed (object sender, EventArgs e)
		{
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
		}

		public override void UpdateFont ()
		{
			text_render.CustomFont = IdeApp.Preferences.CustomPadFont ?? tree.Style.FontDescription;
			tree.ColumnsAutosize ();
		}

		public Gtk.TreeViewColumn CompleteColumn {
			get {
				return complete_column;
			}
		}

		public override void EnableDragUriSource (Func<object,string> nodeToUri)
		{
			tree.EnableDragUriSource (nodeToUri);
		}

		object[] GetDragObjects (out Xwt.Drawing.Image icon)
		{
			icon = null;
			Gtk.TreePath[] paths = tree.Selection.GetSelectedRows ();
			if (paths.Length == 0)
				return null;

			var dragObjects = new object [paths.Length];
			for (int n = 0; n < paths.Length; n++) {
				Gtk.TreeIter it;
				store.GetIter (out it, paths [n]);
				dragObjects [n] = store.GetValue (it, ExtensibleTreeGtkBackend.DataItemColumn);
				if (icon == null)
					icon = (Xwt.Drawing.Image) store.GetValue (it, OpenIconColumn);
			}
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

			var nav = new GtkTreeNodeNavigator (this, iter);
			DragOperation oper = ctx.Action == Gdk.DragAction.Copy ? DragOperation.Copy : DragOperation.Move;

			DropPosition dropPos;
			if (pos == Gtk.TreeViewDropPosition.After)
				dropPos = DropPosition.After;
			else if (pos == Gtk.TreeViewDropPosition.Before)
				dropPos = DropPosition.Before;
			else
				dropPos = DropPosition.Into;


			return frontend.CheckAndDrop (nav, oper, dropPos, drop, obj);
		}


		[GLib.ConnectBefore]
		void HandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (ShowSelectionPopupButton && text_render.PointerInButton ((int)args.Event.XRoot, (int)args.Event.YRoot)) {
				text_render.Pushed = true;
				args.RetVal = true;
				var menu = frontend.CreateContextMenu ();
				if (menu != null) {
					var m = IdeApp.CommandService.CreateMenu (menu);
					m.Hidden += HandleMenuHidden;
					GtkWorkarounds.ShowContextMenu (m, tree, text_render.PopupAllocation);
				}
			}
		}

		[GLib.ConnectBefore]
		void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			if (ShowSelectionPopupButton) {
				text_render.PointerPosition = new Gdk.Point ((int)args.Event.XRoot, (int)args.Event.YRoot);
				Gtk.TreePath path;
				if (tree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path)) {
					var area = tree.GetCellArea (path, tree.Columns[0]);
					tree.QueueDrawArea (area.X, area.Y, area.Width, area.Height);
				}
			}
		}

		[GLib.ConnectBefore]
		void HandleLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
		}

		void HandleMenuHidden (object sender, EventArgs e)
		{
			((Gtk.Menu)sender).Hidden -= HandleMenuHidden;
			text_render.Pushed = false;
			scrollWindow.QueueDraw ();
		}

		public override object CreateWidget ()
		{
			return scrollWindow;
		}

		public override TreeNodeNavigator CreateNavigator ()
		{
			return new GtkTreeNodeNavigator (this, Gtk.TreeIter.Zero);
		}

		public override void Clear ()
		{
			tree.dragObjects = null;
			store.Clear ();
		}

		public override NodePosition GetSelectedNode ()
		{
			Gtk.TreePath[] sel = tree.Selection.GetSelectedRows ();
			if (sel.Length == 0)
				return null;
			Gtk.TreeIter iter;
			if (store.GetIter (out iter, sel[0]))
				return new GtkNodePosition (store, iter);
			else
				return null;
		}

		public override bool MultipleNodesSelected ()
		{
			return tree.Selection.GetSelectedRows ().Length > 1;
		}

		public override NodePosition[] GetSelectedNodes ()
		{
			Gtk.TreePath[] paths = tree.Selection.GetSelectedRows ();
			NodePosition[] navs = new NodePosition [paths.Length];
			for (int n=0; n<paths.Length; n++) {
				Gtk.TreeIter it;
				store.GetIter (out it, paths [n]);
				navs [n] = new GtkNodePosition (store, it);
			}
			return navs;
		}

		public override TreeNodeNavigator GetNodeAtPosition (NodePosition position)
		{
			return new GtkTreeNodeNavigator (this, position.GetIter());
		}

		public override TreeNodeNavigator GetRootNode ()
		{
			Gtk.TreeIter iter;
			if (!store.GetIterFirst (out iter)) return null;
			return new GtkTreeNodeNavigator (this, iter);
		}

		public override void StartLabelEdit ()
		{
			if (editingText)
				return;

			var spos = GetSelectedNode ();
			if (spos == null)
				return;

			TreeNodeNavigator node = GetNodeAtPosition (spos);

			Gtk.TreeIter iter = node.CurrentPosition.GetIter ();
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
			store.SetValue (iter, ExtensibleTreeGtkBackend.TextColumn, nodeName);

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
					var nav = new GtkTreeNodeNavigator (this, iter);
					frontend.RenameNode (nav, e.NewText);
				}

				// Get the iter again since the this node may have been replaced.
				if (!store.GetIterFromString (out iter, e.Path))
					return;

				ITreeBuilder builder = new GtkTreeNodeNavigator (this, iter);
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

			var spos = GetSelectedNode ();
			if (spos == null)
				return;

			TreeNodeNavigator node = GetNodeAtPosition (spos);
			if (node == null)
				return;

			// Restore the original node label
			Gtk.TreeIter iter = node.CurrentPosition.GetIter ();
			ITreeBuilder builder = new GtkTreeNodeNavigator (this, iter);
			builder.Update ();
		}

		public override void OnNodeUnregistered (object dataObject)
		{
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
		}

		internal void RemoveChildren (Gtk.TreeIter it)
		{
			Gtk.TreeIter child;
			while (store.IterChildren (out child, it)) {
				RemoveChildren (child);
				object childData = store.GetValue (child, ExtensibleTreeGtkBackend.DataItemColumn);
				if (childData != null)
					frontend.UnregisterNode (childData, new GtkNodePosition (store, child), null, true);
				store.Remove (ref child);
			}
		}

		public override bool ShowSelectionPopupButton {
			get { return showSelectionPopupButton; }
			set {
				showSelectionPopupButton = value;
				UpdateSelectionPopupButton ();
			}
		}

		void UpdateSelectionPopupButton ()
		{
			if (editingText)
				return;

			if (lastPopupButtonIter != null) {
				if (store.IterIsValid (lastPopupButtonIter.Value))
					tree.Model.SetValue (lastPopupButtonIter.Value, ShowPopupColumn, false);
				lastPopupButtonIter = null;
			}

			if (showSelectionPopupButton) {
				var sel = Tree.Selection.GetSelectedRows ();
				if (sel.Length > 0) {
					Gtk.TreeIter it;
					if (store.GetIter (out it, sel[0])) {
						lastPopupButtonIter = it;
						tree.Model.SetValue (it, ShowPopupColumn, true);
					}
				}
			}
		}

		[GLib.ConnectBefore]
		void OnKeyPress (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Delete || args.Event.Key == Gdk.Key.KP_Delete) {
				frontend.OnCurrentItemDeleted ();
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
				frontend.ExpandCurrentItem ();
				args.RetVal = true;
				return;
			}

			if (args.Event.Key == Gdk.Key.Left || args.Event.Key == Gdk.Key.KP_Left) {
				frontend.CollapseCurrentItem ();
				args.RetVal = true;
				return;
			}

			if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter || args.Event.Key == Gdk.Key.ISO_Enter) {
				frontend.OnCurrentItemActivated ();
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

		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, FilledColumn);
			if (!filled) {
				var nb = new GtkTreeNodeNavigator (this, args.Iter);
				nb.FillNode ();
				args.RetVal = !nb.HasChildren ();
			} else
				args.RetVal = false;
		}

		void ShowPopup (Gdk.EventButton evt)
		{
			var menu = frontend.CreateContextMenu ();
			if (menu != null)
				IdeApp.CommandService.ShowContextMenu (scrollWindow, evt, menu, scrollWindow);
		}

		protected virtual void OnNodeActivated (object sender, Gtk.RowActivatedArgs args)
		{
			frontend.OnCurrentItemActivated ();
		}

		protected virtual void OnSelectionChanged (object sender, EventArgs args)
		{
			UpdateSelectionPopupButton ();

			var spos = GetSelectedNode ();
			if (spos != null) {
				TreeNodeNavigator node = GetNodeAtPosition (spos);
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

		public override void CollapseTree ()
		{
			tree.CollapseAll();
		}

		public override void ScrollToCell (NodePosition pos)
		{
			Gtk.TreePath treePath = store.GetPath (pos.GetIter());
			Tree.ScrollToCell (treePath, null, true, 0, 0);
		}

		public override bool IsChildPosition (NodePosition parent, NodePosition potentialChild, bool recursive)
		{
			Gtk.TreePath pitPath = store.GetPath (parent.GetIter());
			Gtk.TreePath citPath = store.GetPath (potentialChild.GetIter());

			if (!citPath.Up ())
				return false;

			if (citPath.Equals (pitPath))
				return true;

			return recursive && pitPath.IsAncestor (citPath);
		}

		internal int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			sorting = true;
			try {
				NodeBuilder[] chain1 = (NodeBuilder[]) store.GetValue (a, BuilderChainColumn);
				if (chain1 == null) return -1;

				compareNode1.MoveToIter (a);
				compareNode2.MoveToIter (b);

				int sort = frontend.CompareObjects (chain1, compareNode1, compareNode2);
				if (sort != TypeNodeBuilder.DefaultSort) return sort;

				NodeBuilder[] chain2 = (NodeBuilder[]) store.GetValue (b, BuilderChainColumn);
				if (chain2 == null) return 1;

				if (chain1 != chain2) {
					sort = frontend.CompareObjects (chain2, compareNode2, compareNode1);
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

		class ExtensibleTreeViewTree : ContextMenuTreeView
		{
			ExtensibleTreeGtkBackend tv;

			public ExtensibleTreeViewTree (ExtensibleTreeGtkBackend tv)
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
			double zoom = 1;
			Pango.Layout layout;
			Pango.FontDescription scaledFont, customFont;

			static Xwt.Drawing.Image popupIcon;
			static Xwt.Drawing.Image popupIconDown;
			static Xwt.Drawing.Image popupIconHover;
			bool bound;
			ExtensibleTreeGtkBackend parent;
			Gdk.Rectangle buttonScreenRect;
			Gdk.Rectangle buttonAllocation;
			string markup;

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
				popupIcon = Xwt.Drawing.Image.FromResource ("tree-popup-button-light.png");
				popupIconDown = Xwt.Drawing.Image.FromResource ("tree-popup-button-down-light.png");
				popupIconHover = Xwt.Drawing.Image.FromResource ("tree-popup-button-hover-light.png");
			}

			[GLib.Property ("text-markup")]
			public string TextMarkup {
				get { return markup; }
				set { Markup = markup = value; }
			}

			[GLib.Property ("show-popup-button")]
			public bool ShowPopupButton { get; set; }

			public CustomCellRendererText (ExtensibleTreeGtkBackend parent)
			{
				this.parent = parent;
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

				if (scaledFont == null) {
					if (scaledFont != null)
						scaledFont.Dispose ();
					scaledFont = (customFont ?? parent.scrollWindow.Style.FontDesc).Copy ();
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

				layout.SetMarkup (TextMarkup);

				int w, h;
				layout.GetPixelSize (out w, out h);

				int tx = cell_area.X + (int)Xpad;
				int ty = cell_area.Y + (cell_area.Height - h) / 2;

				window.DrawLayout (widget.Style.TextGC (st), tx, ty, layout);

				if (ShowPopupButton) {
					if (!bound) {
						bound = true;
						((Gtk.ScrolledWindow)widget.Parent).Hadjustment.ValueChanged += delegate {
							foreach (var r in parent.Tree.Selection.GetSelectedRows ()) {
								var rect = parent.Tree.GetCellArea (r, parent.Tree.Columns [0]);
								parent.Tree.QueueDrawArea (rect.X, rect.Y, rect.Width, rect.Height);
							}
						};
					}

					if ((flags & Gtk.CellRendererState.Selected) != 0) {
						var icon = Pushed ? popupIconDown : popupIcon;
						var dy = (cell_area.Height - (int)icon.Height) / 2 - 1;
						var y = cell_area.Y + dy;
						var x = cell_area.X + cell_area.Width - (int)icon.Width - dy;

						var sw = (Gtk.ScrolledWindow)widget.Parent;
						int ox, oy, ow, oh;
						sw.GdkWindow.GetOrigin (out ox, out oy);
						sw.GdkWindow.GetSize (out ow, out oh);
						ox += sw.Allocation.X;
						oy += sw.Allocation.Y;
						if (sw.VScrollbar.Visible)
							ow -= sw.VScrollbar.Allocation.Width;

						int cx, cy, cw, ch;
						((Gdk.Window)window).GetOrigin (out cx, out cy);
						((Gdk.Window)window).GetSize (out cw, out ch);
						cx += widget.Allocation.X;
						cy += widget.Allocation.Y;

						int rp = ox + ow;
						int diff = rp - (cx + cw);

						if (diff < 0) {
							x += diff;
							if (x < cell_area.X + 20)
								x = cell_area.X + 20;
						}

						buttonScreenRect = new Gdk.Rectangle (cx + x, cy + y, (int)popupIcon.Width, (int)popupIcon.Height);

						buttonAllocation = new Gdk.Rectangle (x, y, (int)popupIcon.Width, (int)popupIcon.Height);
						buttonAllocation = GtkUtil.ToScreenCoordinates (widget, ((Gdk.Window)window), buttonAllocation);
						buttonAllocation = GtkUtil.ToWindowCoordinates (widget, widget.GdkWindow, buttonAllocation);

						bool mouseOver = (flags & Gtk.CellRendererState.Prelit) != 0 && buttonScreenRect.Contains (PointerPosition);
						if (mouseOver && !Pushed)
							icon = popupIconHover;

						using (var ctx = Gdk.CairoHelper.Create (window)) {
							ctx.DrawImage (widget, icon, x, y);
						}
					}
				}
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
}

