//
// MacObjectValueTreeView.cs
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
using System.Text;
using System.Collections.Generic;

using AppKit;
using CoreGraphics;

using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Debugger
{
	public class MacObjectValueTreeView : NSOutlineView, IObjectValueTreeView /*, ICompletionWidget*/
	{
		const int MinimumColumnWidth = 100;

		MacObjectValueTreeViewDelegate treeViewDelegate;
		MacObjectValueTreeViewDataSource dataSource;

		readonly NSTableColumn nameColumn;
		readonly NSTableColumn valueColumn;
		readonly NSTableColumn typeColumn;
		readonly NSTableColumn pinColumn;
		readonly bool allowPopupMenu;
		readonly bool rootPinVisible;
		readonly bool compactView;

		PinnedWatch pinnedWatch;

		double nameColumnWidth;
		double valueColumnWidth;
		double typeColumnWidth;

		PreviewButtonIcon currentHoverIcon;
		nint currentHoverRow = -1;

		bool allowEditing;
		bool disposed;

		public MacObjectValueTreeView (
			IObjectValueDebuggerService debuggerService,
			ObjectValueTreeViewController controller,
			bool allowEditing,
			bool headersVisible,
			bool compactView,
			bool allowPinning,
			bool allowPopupMenu,
			bool rootPinVisible)
		{
			DebuggerService = debuggerService;
			Controller = controller;

			this.rootPinVisible = rootPinVisible;
			this.allowPopupMenu = allowPopupMenu;
			this.allowEditing = allowEditing;
			this.compactView = compactView;
			ResetColumnSizes ();

			DataSource = dataSource = new MacObjectValueTreeViewDataSource (this, controller.Root, controller.AllowWatchExpressions);
			Delegate = treeViewDelegate = new MacObjectValueTreeViewDelegate (this);
			ColumnAutoresizingStyle = NSTableViewColumnAutoresizingStyle.Sequential;
			treeViewDelegate.SelectionChanged += OnSelectionChanged;
			UsesAlternatingRowBackgroundColors = true;
			FocusRingType = NSFocusRingType.None;
			AutoresizesOutlineColumn = false;
			AllowsColumnResizing = true;

			nameColumn = new NSTableColumn ("name") { Editable = controller.AllowWatchExpressions, MinWidth = MinimumColumnWidth, ResizingMask = NSTableColumnResizing.None };
			nameColumn.Title = GettextCatalog.GetString ("Name");
			AddColumn (nameColumn);

			OutlineTableColumn = nameColumn;

			valueColumn = new NSTableColumn ("value") { Editable = controller.AllowEditing, MinWidth = MinimumColumnWidth, ResizingMask = NSTableColumnResizing.None };
			valueColumn.Title = GettextCatalog.GetString ("Value");
			if (compactView)
				valueColumn.MaxWidth = 800;
			AddColumn (valueColumn);

			if (!compactView) {
				typeColumn = new NSTableColumn ("type") { Editable = false, MinWidth = MinimumColumnWidth, ResizingMask = NSTableColumnResizing.None };
				typeColumn.Title = GettextCatalog.GetString ("Type");
				AddColumn (typeColumn);
			}

			if (allowPinning) {
				pinColumn = new NSTableColumn ("pin") { Editable = false, ResizingMask = NSTableColumnResizing.None };
				pinColumn.MinWidth = pinColumn.MaxWidth = pinColumn.Width = MacDebuggerObjectPinView.MinWidth;
				AddColumn (pinColumn);
			}

			if (!headersVisible)
				HeaderView = null;

			AdjustColumnSizes ();
		}

		public ObjectValueTreeViewController Controller {
			get; private set;
		}

		public bool CompactView {
			get { return compactView; }
		}

		public IObjectValueDebuggerService DebuggerService {
			get; private set;
		}

		/// <summary>
		/// Gets a value indicating whether the user should be able to edit values in the tree
		/// </summary>
		public bool AllowEditing {
			get => allowEditing;
			set {
				if (allowEditing != value) {
					allowEditing = value;
					ReloadData ();
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether or not the user should be able to expand nodes in the tree.
		/// </summary>
		public bool AllowExpanding { get; set; }

		/// <summary>
		/// Gets a value indicating whether the user should be able to add watch expressions to the tree
		/// </summary>
		public bool AllowWatchExpressions {
			get { return dataSource.AllowWatchExpressions; }
		}

		/// <summary>
		/// Gets or sets the pinned watch for the view. When a watch is pinned, the view should display only this value
		/// </summary>
		public PinnedWatch PinnedWatch {
			get => pinnedWatch;
			set {
				if (pinnedWatch != value) {
					pinnedWatch = value;
					Runtime.RunInMainThread (() => {
						if (value == null) {
							pinColumn.MinWidth = pinColumn.MaxWidth = pinColumn.Width = MacDebuggerObjectPinView.MinWidth;
						} else {
							pinColumn.MinWidth = pinColumn.MaxWidth = pinColumn.Width = MacDebuggerObjectPinView.MaxWidth;
						}
					}).Ignore ();
				}
			}
		}

		/// <summary>
		/// Gets a value indicating the offset required for pinned watches
		/// </summary>
		public int PinnedWatchOffset {
			get {
				return (int) Frame.Height;
			}
		}

		void ResetColumnSizes ()
		{
			nameColumnWidth = 0.3;
			valueColumnWidth = 0.5;
			typeColumnWidth = 0.2;
		}

		void AdjustColumnSizes ()
		{
			if (Hidden || compactView)
				return;

			double width;

			// immediate parent would be an NSClipView if we are a child of an NSScrollView
			if (Superview?.Superview is NSScrollView scrollView) {
				width = scrollView.Bounds.Width;
			} else {
				width = Bounds.Width;
			}

			var available = width;
			int columnWidth;

			columnWidth = Math.Max ((int) (width * valueColumnWidth), MinimumColumnWidth);
			valueColumn.Width = columnWidth;
			available -= columnWidth;

			columnWidth = Math.Max ((int) (width * nameColumnWidth), MinimumColumnWidth);
			nameColumn.Width = columnWidth;
			available -= columnWidth;

			columnWidth = Math.Max ((int) available, MinimumColumnWidth);
			typeColumn.Width = columnWidth;
		}

		internal void CompactColumns ()
		{
			if (!compactView)
				return;

			//var widths = new nfloat[ColumnCount];
			//var columns = TableColumns ();

			//for (nint i = 0; i < ColumnCount; i++)
			//	widths[i] = columns[i].MinWidth;

			//for (nint row = 0; row < RowCount; row++) {
			//	var rowView = GetRowView (row, true);

			//	for (nint i = 0; i < ColumnCount; i++) {
			//		var cellView = rowView.ViewAtColumn (i);
			//		var width = cellView.FittingSize.Width;

			//		widths[i] = NMath.Max (widths[i], width);
			//	}
			//}

			//for (nint i = 0; i < ColumnCount; i++)
			//	columns[i].Width = NMath.Min (widths[i], columns[i].MaxWidth);
		}

		internal void SetCustomFont (Pango.FontDescription font)
		{
			// TODO: set fonts for all cell views
		}

		public override void ViewDidMoveToSuperview ()
		{
			base.ViewDidMoveToSuperview ();
			AdjustColumnSizes ();
			CompactColumns ();
		}

		public override void ViewDidEndLiveResize ()
		{
			base.ViewDidEndLiveResize ();
			AdjustColumnSizes ();
			CompactColumns ();
		}

		public override void ViewDidUnhide ()
		{
			base.ViewDidHide ();
			AdjustColumnSizes ();
			CompactColumns ();
		}

		/// <summary>
		/// Triggered when the view tries to expand a node. This may trigger a load of
		/// the node's children
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeExpand;

		public void ExpandNode (ObjectValueNode node)
		{
			NodeExpand?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		/// <summary>
		/// Triggered when the view tries to collapse a node.
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeCollapse;

		public void CollapseNode (ObjectValueNode node)
		{
			NodeCollapse?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		/// <summary>
		/// Triggered when the view requests a node to fetch more of it's children
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeLoadMoreChildren;

		internal void LoadMoreChildren (ObjectValueNode node)
		{
			NodeLoadMoreChildren?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		/// <summary>
		/// Triggered when the view needs the node to be refreshed
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeRefresh;

		internal void Refresh (ObjectValueNode node)
		{
			NodeRefresh?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		/// <summary>
		/// Triggered when the view needs to know if the node can be edited
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeGetCanEdit;

		internal bool GetCanEditNode (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeGetCanEdit?.Invoke (this, args);
			return args.Response is bool b && b;
		}

		/// <summary>
		/// Triggered when the node's value has been edited by the user
		/// </summary>
		public event EventHandler<ObjectValueEditEventArgs> NodeEditValue;

		internal bool GetEditValue (ObjectValueNode node, string newText)
		{
			var args = new ObjectValueEditEventArgs (node, newText);
			NodeEditValue?.Invoke (this, args);
			return args.Response is bool b && b;
		}

		/// <summary>
		/// Triggered when the user removes a node (an expression)
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeRemoved;

		/// <summary>
		/// Triggered when the user pins the node
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodePinned;

		void CreatePinnedWatch (ObjectValueNode node)
		{
			var expression = node.Expression;

			if (string.IsNullOrEmpty (expression))
				return;

			if (PinnedWatch != null) {
				// Note: the row that the user just pinned will no longer be visible once
				// all of the root children are collapsed.
				currentHoverRow = -1;

				foreach (var child in dataSource.Root.Children)
					CollapseItem (child, true);
			}

			NodePinned?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		public void Pin (ObjectValueNode node)
		{
			CreatePinnedWatch (node);
		}

		/// <summary>
		/// Triggered when the pinned watch is removed by the user
		/// </summary>
		public event EventHandler<EventArgs> NodeUnpinned;

		public void Unpin (ObjectValueNode node)
		{
			NodeUnpinned?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Triggered when the visualiser for the node should be shown
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeShowVisualiser;

		internal bool ShowVisualizer (ObjectValueNode node)
		{
			var args = new ObjectValueNodeEventArgs (node);
			NodeShowVisualiser?.Invoke (this, args);
			return args.Response is bool b && b;
		}

		/// <summary>
		/// Triggered when an expression is added to the tree by the user
		/// </summary>
		public event EventHandler<ObjectValueExpressionEventArgs> ExpressionAdded;

		internal void OnExpressionAdded (string expression)
		{
			ExpressionAdded?.Invoke (this, new ObjectValueExpressionEventArgs (null, expression));
		}

		/// <summary>
		/// Triggered when an expression is edited by the user
		/// </summary>
		public event EventHandler<ObjectValueExpressionEventArgs> ExpressionEdited;

		internal void OnExpressionEdited (ObjectValueNode node, string expression)
		{
			ExpressionEdited?.Invoke (this, new ObjectValueExpressionEventArgs (node, expression));
		}

		/// <summary>
		/// Triggered when the user starts editing a node
		/// </summary>
		public event EventHandler StartEditing;

		internal void OnStartEditing ()
		{
			StartEditing?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Triggered when the user stops editing a node
		/// </summary>
		public new event EventHandler EndEditing;

		internal void OnEndEditing ()
		{
			EndEditing?.Invoke (this, EventArgs.Empty);
		}

		void OnEvaluationCompleted (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			if (disposed)
				return;

			dataSource.Replace (node, replacementNodes);
			CompactColumns ();
			Resize ();
		}

		public void LoadEvaluatedNode (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			OnEvaluationCompleted (node, replacementNodes);
		}

		void OnChildrenLoaded (ObjectValueNode node, int startIndex, int count)
		{
			if (disposed)
				return;

			dataSource.ReloadChildren (node);
			CompactColumns ();
			Resize ();
		}

		public void LoadNodeChildren (ObjectValueNode node, int startIndex, int count)
		{
			OnChildrenLoaded (node, startIndex, count);
		}

		public void OnNodeExpanded (ObjectValueNode node)
		{
			if (disposed)
				return;

			if (node.IsExpanded) {
				// if the node is _still_ expanded then adjust UI and scroll
				if (dataSource.TryGetValue (node, out var item)) {
					if (!IsItemExpanded (item))
						ExpandItem (item);
				}

				CompactColumns ();

				// TODO: all this scrolling kind of seems awkward
				//if (path != null)
				//	ScrollToCell (path, expCol, true, 0f, 0f);
			}
		}

		void IObjectValueTreeView.Cleared ()
		{
			dataSource.Clear ();
		}

		void IObjectValueTreeView.Appended (ObjectValueNode node)
		{
			dataSource.Append (node);
		}

		void IObjectValueTreeView.Appended (IList<ObjectValueNode> nodes)
		{
			dataSource.Append (nodes);
		}

		static CGPoint ConvertPointFromEvent (NSView view, NSEvent theEvent)
		{
			var point = theEvent.LocationInWindow;

			if (view.Window != null && theEvent.WindowNumber != view.Window.WindowNumber) {
				var rect = theEvent.Window.ConvertRectToScreen (new CGRect (point, new CGSize (1, 1)));
				rect = view.Window.ConvertRectFromScreen (rect);
				point = rect.Location;
			}

			return view.ConvertPointFromView (point, null);
		}

		void UpdatePreviewIcon (nint row, PreviewButtonIcon icon)
		{
			var rowView = GetRowView (row, false);

			if (rowView != null) {
				var nameView = (MacDebuggerObjectNameView) rowView.ViewAtColumn (0);

				nameView.SetPreviewButtonIcon (icon);
			}
		}

		void UpdatePinIcon (nint row, bool hover)
		{
			if (pinColumn == null)
				return;

			var rowView = GetRowView (row, false);

			if (rowView != null) {
				var pinView = (MacDebuggerObjectPinView) rowView.ViewAtColumn (ColumnCount - 1);

				pinView.SetMouseHover (hover);
			}
		}

		void UpdateCellViewIcons (NSEvent theEvent)
		{
			var point = ConvertPointFromEvent (this, theEvent);
			var row = GetRow (point);

			if (row != currentHoverRow) {
				if (currentHoverRow != -1) {
					UpdatePreviewIcon (currentHoverRow, PreviewButtonIcon.Hidden);
					currentHoverIcon = PreviewButtonIcon.Hidden;
					UpdatePinIcon (currentHoverRow, false);
				}
				currentHoverRow = row;
			}

			if (row == -1)
				return;

			PreviewButtonIcon icon;

			if (GetColumn (point) == 0) {
				icon = PreviewButtonIcon.Hover;
			} else {
				icon = PreviewButtonIcon.RowHover;
			}

			currentHoverIcon = icon;

			if (IsRowSelected (row))
				icon = PreviewButtonIcon.Selected;

			UpdatePreviewIcon (row, icon);
			UpdatePinIcon (row, true);
		}

		public override void MouseEntered (NSEvent theEvent)
		{
			UpdateCellViewIcons (theEvent);
			base.MouseEntered (theEvent);
		}

		public override void MouseExited (NSEvent theEvent)
		{
			if (currentHoverRow != -1) {
				UpdatePreviewIcon (currentHoverRow, PreviewButtonIcon.Hidden);
				currentHoverIcon = PreviewButtonIcon.Hidden;
				currentHoverRow = -1;

				UpdatePinIcon (currentHoverRow, false);
			}

			base.MouseExited (theEvent);
		}

		public override void MouseMoved (NSEvent theEvent)
		{
			UpdateCellViewIcons (theEvent);
			base.MouseMoved (theEvent);
		}

		internal static bool ValidObjectForPreviewIcon (ObjectValueNode node)
		{
			var obj = node.GetDebuggerObjectValue ();
			if (obj == null)
				return false;

			if (obj.IsNull)
				return false;

			if (obj.IsPrimitive) {
				//obj.DisplayValue.Contains ("|") is special case to detect enum with [Flags]
				return obj.TypeName == "string" || (obj.DisplayValue != null && obj.DisplayValue.Contains ("|"));
			}

			if (string.IsNullOrEmpty (obj.TypeName))
				return false;

			return true;
		}

		void OnSelectionChanged (object sender, EventArgs e)
		{
			if (currentHoverRow == -1)
				return;

			var row = SelectedRow;

			if (SelectedRowCount == 0 || row != currentHoverRow) {
				// reset back to what the unselected icon would be
				UpdatePreviewIcon (currentHoverRow, currentHoverIcon);
				return;
			}

			UpdatePreviewIcon (currentHoverRow, PreviewButtonIcon.Selected);
		}

		public event EventHandler Resized;

		public void Resize ()
		{
			NeedsLayout = true;
			LayoutSubtreeIfNeeded ();
			Resized?.Invoke (this, EventArgs.Empty);
		}

		[CommandUpdateHandler (EditCommands.SelectAll)]
		protected void UpdateSelectAll (CommandInfo cmd)
		{
			cmd.Enabled = Controller.Root.Children.Count > 0;
		}

		[CommandHandler (EditCommands.SelectAll)]
		protected void OnSelectAll ()
		{
			SelectAll (this);
		}

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			if (SelectedRowCount == 0)
				return;

			var str = new StringBuilder ();
			var needsNewLine = false;

			var selectedRows = SelectedRows;
			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);

				if (item.Target is AddNewExpressionObjectValueNode ||
					item.Target is ShowMoreValuesObjectValueNode ||
					item.Target is LoadingObjectValueNode)
					break;

				if (needsNewLine)
					str.AppendLine ();

				needsNewLine = true;

				var value = item.Target.DisplayValue;
				var type = item.Target.TypeName;

				if (type == "string") {
					var objVal = item.Target.GetDebuggerObjectValue ();

					if (objVal != null) {
						// HACK: we need a better abstraction of the stack frame, better yet would be to not really need it in the view
						var opt = DebuggerService.Frame.GetStackFrame ().DebuggerSession.Options.EvaluationOptions.Clone ();
						opt.EllipsizeStrings = false;
						value = '"' + Mono.Debugging.Evaluation.ExpressionEvaluator.EscapeString ((string)objVal.GetRawValue (opt)) + '"';
					}
				}

				str.Append (value);
			}

			var clipboard = NSPasteboard.GeneralPasteboard;

			clipboard.SetStringForType (str.ToString (), NSPasteboard.NSPasteboardTypeString);
		}

		[CommandHandler (EditCommands.Delete)]
		[CommandHandler (EditCommands.DeleteKey)]
		protected void OnDelete ()
		{
			var nodesToDelete = new List<ObjectValueNode> ();
			var selectedRows = SelectedRows;

			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);

				nodesToDelete.Add (item.Target);
			}

			foreach (var node in nodesToDelete)
				NodeRemoved?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.DeleteKey)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			if (!AllowWatchExpressions) {
				cinfo.Visible = false;
				return;
			}

			if (SelectedRowCount == 0) {
				cinfo.Enabled = false;
				return;
			}

			var selectedRows = SelectedRows;
			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);

				if (!(item.Target.Parent is RootObjectValueNode)) {
					cinfo.Enabled = false;
					break;
				}
			}
		}

		[CommandHandler (DebugCommands.AddWatch)]
		protected void OnAddWatch ()
		{
			var expressions = new List<string> ();
			var selectedRows = SelectedRows;

			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);
				var expression = item.Target.Expression;

				if (!string.IsNullOrEmpty (expression))
					expressions.Add (expression);
			}

			foreach (var expression in expressions)
				DebuggingService.AddWatch (expression);
		}

		[CommandUpdateHandler (DebugCommands.AddWatch)]
		protected void OnUpdateAddWatch (CommandInfo cinfo)
		{
			cinfo.Enabled = SelectedRowCount > 0;
		}

		[CommandHandler (EditCommands.Rename)]
		protected void OnRename ()
		{
			var nameView = (MacDebuggerObjectNameView) GetView (0, SelectedRow, false);

			nameView.TextField.BecomeFirstResponder ();
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		protected void OnUpdateRename (CommandInfo cinfo)
		{
			cinfo.Visible = AllowWatchExpressions;
			cinfo.Enabled = SelectedRowCount == 1;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				treeViewDelegate.SelectionChanged -= OnSelectionChanged;
				treeViewDelegate.Dispose ();
				treeViewDelegate = null;
				dataSource.Dispose ();
				dataSource = null;
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
