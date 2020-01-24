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
using Foundation;
using CoreGraphics;

using Mono.Debugging.Client;
using Mono.Debugging.Evaluation;

using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Debugger
{
	public class MacObjectValueTreeView : NSOutlineView, IObjectValueTreeView
	{
		static readonly NSFont DefaultSystemFont = NSFont.UserFontOfSize (0);

		const int MinimumNameColumnWidth = 45;
		const int MinimumValueColumnWidth = 75;
		const int MinimumTypeColumnWidth = 30;

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

		PreviewButtonIcon currentHoverIcon;
		nint currentHoverRow = -1;
		bool allowEditing;
		bool disposed;

		public MacObjectValueTreeView (
			IObjectValueDebuggerService debuggerService,
			ObjectValueTreeViewController controller,
			bool allowEditing,
			ObjectValueTreeViewFlags flags)
		{
			DebuggerService = debuggerService;
			Controller = controller;

			this.rootPinVisible = (flags & ObjectValueTreeViewFlags.RootPinVisible) != 0;
			this.allowPopupMenu = (flags & ObjectValueTreeViewFlags.AllowPopupMenu) != 0;
			this.compactView = (flags & ObjectValueTreeViewFlags.CompactView) != 0;
			this.allowEditing = allowEditing;

			DataSource = dataSource = new MacObjectValueTreeViewDataSource (this, controller.Root, controller.AllowWatchExpressions);
			Delegate = treeViewDelegate = new MacObjectValueTreeViewDelegate (this);
			ColumnAutoresizingStyle = compactView ? NSTableViewColumnAutoresizingStyle.None : NSTableViewColumnAutoresizingStyle.Uniform;
			AllowsSelection = (flags & ObjectValueTreeViewFlags.AllowSelection) != 0;
			treeViewDelegate.SelectionChanged += OnSelectionChanged;
			UsesAlternatingRowBackgroundColors = true;
			FocusRingType = NSFocusRingType.None;
			AllowsColumnResizing = !compactView;
			AutoresizesOutlineColumn = false;
			SetCustomFont (null);

			var resizingMask = compactView ? NSTableColumnResizing.None : NSTableColumnResizing.UserResizingMask | NSTableColumnResizing.Autoresizing;

			nameColumn = new NSTableColumn ("name") { Editable = controller.AllowWatchExpressions, MinWidth = MinimumNameColumnWidth, ResizingMask = resizingMask };
			nameColumn.Title = GettextCatalog.GetString ("Name");
			nameColumn.Width = MinimumNameColumnWidth * 2;
			AddColumn (nameColumn);

			OutlineTableColumn = nameColumn;

			valueColumn = new NSTableColumn ("value") { Editable = controller.AllowEditing, MinWidth = MinimumValueColumnWidth, ResizingMask = resizingMask };
			valueColumn.Title = GettextCatalog.GetString ("Value");
			valueColumn.Width = MinimumValueColumnWidth * 2;
			if (compactView)
				valueColumn.MaxWidth = 800;
			AddColumn (valueColumn);

			if (!compactView) {
				typeColumn = new NSTableColumn ("type") { Editable = false, MinWidth = MinimumTypeColumnWidth, ResizingMask = resizingMask };
				typeColumn.Title = GettextCatalog.GetString ("Type");
				typeColumn.Width = MinimumTypeColumnWidth * 2;
				AddColumn (typeColumn);
			}

			if ((flags & ObjectValueTreeViewFlags.AllowPinning) != 0) {
				pinColumn = new NSTableColumn ("pin") { Editable = false, ResizingMask = NSTableColumnResizing.None };
				pinColumn.MinWidth = pinColumn.MaxWidth = pinColumn.Width = MacDebuggerObjectPinView.MinWidth;
				AddColumn (pinColumn);
			}

			if ((flags & ObjectValueTreeViewFlags.HeadersVisible) != 0) {
				HeaderView.AlphaValue = 1.0f;
			} else {
				HeaderView = null;
			}

			PreviewWindowManager.WindowClosed += OnPreviewWindowClosed;

			// disable implicit animations
			WantsLayer = true;
			Layer.Actions = new NSDictionary (
				"actions", NSNull.Null,
				"contents", NSNull.Null,
				"hidden", NSNull.Null,
				"onLayout", NSNull.Null,
				"onOrderIn", NSNull.Null,
				"onOrderOut", NSNull.Null,
				"position", NSNull.Null,
				"sublayers", NSNull.Null,
				"transform", NSNull.Null,
				"bounds", NSNull.Null);
		}

		public string UIElementName {
			get; set;
		}

		public ObjectValueTreeViewController Controller {
			get; private set;
		}

		public bool AllowsSelection {
			get; private set;
		}

		public bool CompactView {
			get { return compactView; }
		}

		internal NSFont CustomFont {
			get; private set;
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
				if (pinnedWatch != value && pinColumn != null) {
					pinnedWatch = value;
					Runtime.RunInMainThread (() => {
						if (pinColumn == null)
							return;
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

		/// <summary>
		/// Gets the optimal tooltip window width in order to display the name/value/pin columns w/o truncation.
		/// </summary>
		public nfloat OptimalTooltipWidth {
			get; private set;
		}

		// Note: this resizing method is the one used by debugger tooltips and pinned watches in the editor
		void OptimizeColumnSizes ()
		{
			if (!compactView || Superview == null || RowCount == 0)
				return;

			nfloat nameWidth = MinimumNameColumnWidth;
			nfloat valueWidth = MinimumValueColumnWidth;

			for (nint row = 0; row < RowCount; row++) {
				var item = (MacObjectValueNode) ItemAtRow (row);

				item.Measure (this);

				var totalNameWidth = item.OptimalXOffset + item.OptimalNameWidth;
				if (totalNameWidth > nameWidth)
					nameWidth = NMath.Min (totalNameWidth, nameColumn.MaxWidth);

				if (item.OptimalValueWidth > valueWidth)
					valueWidth = NMath.Min (item.OptimalValueWidth, valueColumn.MaxWidth);
			}

			bool changed = false;

			if ((int) nameColumn.Width != (int) nameWidth) {
				nameColumn.Width = nameWidth;
				changed = true;
			}

			if ((int) valueColumn.Width != (int) valueWidth) {
				valueColumn.Width = valueWidth;
				changed = true;
			}

			if (changed) {
				var optimalTooltipWidth = nameWidth + valueWidth + pinColumn.Width + IntercellSpacing.Width * 2;
				OptimalTooltipWidth = optimalTooltipWidth;
			}

			// we almost always need to recalculate the size - particularly if the widths don't
			// change but the row count did.
			OnResized ();
			SetNeedsDisplayInRect (Frame);
		}

		static NSFont GetNSFontFromPangoFontDescription (Pango.FontDescription fontDescription)
		{
			if (fontDescription == null)
				return null;

			return NSFontManager.SharedFontManager.FontWithFamilyWorkaround (
				fontDescription.Family,
				fontDescription.Style == Pango.Style.Italic || fontDescription.Style == Pango.Style.Oblique
					? NSFontTraitMask.Italic
					: 0,
				NormalizeWeight (fontDescription.Weight),
				fontDescription.Size / (nfloat) Pango.Scale.PangoScale);

			/// <summary>
			/// Normalizes a Pango font weight (100-1000 scale) to a weight
			/// suitable for NSFontDescription.FontWithFamily (0-15 scale).
			/// </summary>
			int NormalizeWeight (Pango.Weight pangoWeight)
			{
				double Normalize (double value, double inMin, double inMax, double outMin, double outMax)
					=> (outMax - outMin) / (inMax - inMin) * (value - inMax) + outMax;

				return (int) Math.Round (Normalize ((int) pangoWeight, 100, 1000, 0, 15));
			}
		}

		nfloat CalculateRowHeight (NSFont font)
		{
			using (var layoutManager = new NSLayoutManager ()) {
				layoutManager.TypesetterBehavior = NSTypesetterBehavior.Specific_10_4;
				layoutManager.UsesScreenFonts = false;

				return layoutManager.DefaultLineHeightForFont (font);
			}
		}

		internal void SetCustomFont (Pango.FontDescription fontDescription)
		{
			if (fontDescription != null) {
				CustomFont = GetNSFontFromPangoFontDescription (fontDescription);
			} else {
				CustomFont = DefaultSystemFont;
			}

			// Note: We need a minimum of 16px for the icons and an added 2px for vertical spacing
			RowHeight = NMath.Max (CalculateRowHeight (CustomFont), 18.0f);
			ReloadData ();
		}

		internal void QueueResize ()
		{
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();
			OptimizeColumnSizes ();
		}

		public override void ViewDidUnhide ()
		{
			base.ViewDidHide ();
			OptimizeColumnSizes ();
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

		public override void ExpandItem (NSObject item, bool expandChildren)
		{
			base.ExpandItem (item, expandChildren);
			OptimizeColumnSizes ();
		}

		public override void ExpandItem (NSObject item)
		{
			base.ExpandItem (item);
			OptimizeColumnSizes ();
		}

		/// <summary>
		/// Triggered when the view tries to collapse a node.
		/// </summary>
		public event EventHandler<ObjectValueNodeEventArgs> NodeCollapse;

		public void CollapseNode (ObjectValueNode node)
		{
			NodeCollapse?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		public override void CollapseItem (NSObject item, bool collapseChildren)
		{
			base.CollapseItem (item, collapseChildren);
			OptimizeColumnSizes ();
		}

		public override void CollapseItem (NSObject item)
		{
			base.CollapseItem (item);
			OptimizeColumnSizes ();
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

			Counters.PinnedWatch.Inc (1);
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
			var metadata = new Dictionary<string, object> ();
			metadata["UIElementName"] = UIElementName;
			metadata["ObjectValue.Type"] = node.TypeName;

			Counters.OpenedVisualizer.Inc (1, null, metadata);

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
			OptimizeColumnSizes ();
		}

		public void LoadEvaluatedNode (ObjectValueNode node, ObjectValueNode[] replacementNodes)
		{
			OnEvaluationCompleted (node, replacementNodes);
		}

		void OnChildrenLoaded (ObjectValueNode node, int startIndex, int count)
		{
			if (disposed)
				return;

			dataSource.LoadChildren (node, startIndex, count);
			OptimizeColumnSizes ();
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

				// TODO: all this scrolling kind of seems awkward
				//if (path != null)
				//	ScrollToCell (path, expCol, true, 0f, 0f);

				var parent = node.Parent;
				int level = 0;
				while (parent != null) {
					parent = parent.Parent;
					level++;
				}

				var metadata = new Dictionary<string, object> ();
				metadata["UIElementName"] = UIElementName;
				metadata["NodeLevel"] = level;

				Counters.ExpandedNode.Inc (1, null, metadata);
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
			if (row >= RowCount)
				return;

			var rowView = GetRowView (row, false);

			if (rowView != null) {
				var nameView = (MacDebuggerObjectNameView) rowView.ViewAtColumn (0);

				nameView?.SetPreviewButtonIcon (icon);
			}
		}

		void UpdatePinIcon (nint row, bool hover)
		{
			if (row >= RowCount)
				return;

			if (pinColumn == null)
				return;

			var rowView = GetRowView (row, false);

			if (rowView != null) {
				var pinView = (MacDebuggerObjectPinView) rowView.ViewAtColumn (ColumnCount - 1);

				pinView?.SetMouseHover (hover);
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

		void OnPreviewWindowClosed (object sender, EventArgs args)
		{
			if (currentHoverRow != -1) {
				UpdatePreviewIcon (currentHoverRow, PreviewButtonIcon.Hidden);
				currentHoverIcon = PreviewButtonIcon.Hidden;
			}
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

		void OnResized ()
		{
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

			var builder = new StringBuilder ();
			var needsNewLine = false;

			var selectedRows = SelectedRows;
			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);

				if (item.Target is AddNewExpressionObjectValueNode ||
					item.Target is ShowMoreValuesObjectValueNode ||
					item.Target is LoadingObjectValueNode)
					break;

				if (needsNewLine)
					builder.AppendLine ();

				needsNewLine = true;

				var value = item.Target.DisplayValue;
				var type = item.Target.TypeName;

				if (type == "string") {
					var objVal = item.Target.GetDebuggerObjectValue ();

					if (objVal != null) {
						try {
							// HACK: we need a better abstraction of the stack frame, better yet would be to not really need it in the view
							var opt = DebuggerService.Frame.GetStackFrame ().DebuggerSession.Options.EvaluationOptions.Clone ();
							opt.EllipsizeStrings = false;

							var rawValue = objVal.GetRawValue (opt);
							var str = rawValue as string;

							if (str == null && rawValue is RawValueString rawValueString)
								str = rawValueString.Value;

							if (str != null)
								value = '"' + ExpressionEvaluator.EscapeString (str) + '"';
						} catch (EvaluatorException) {
							// fall back to using the DisplayValue that we would have used anyway...
						}
					}
				}

				builder.Append (value);
			}

			var clipboard = NSPasteboard.GeneralPasteboard;

			clipboard.ClearContents ();
			clipboard.SetStringForType (builder.ToString (), NSPasteboard.NSPasteboardTypeString);

			//Gtk.Clipboard.Get (Gdk.Selection.Clipboard).Text = str.ToString ();
		}

		void OnCopy (object sender, EventArgs args)
		{
			OnCopy ();
		}

		[CommandHandler (EditCommands.Delete)]
		[CommandHandler (EditCommands.DeleteKey)]
		protected void OnDelete ()
		{
			var nodesToDelete = new List<ObjectValueNode> ();
			var selectedRows = SelectedRows;

			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);

				// The user is only allowed to delete top-level nodes. It doesn't make sense to allow
				// deleting child nodes of anything else.
				if (!(item.Target.Parent is RootObjectValueNode))
					continue;

				nodesToDelete.Add (item.Target);
			}

			foreach (var node in nodesToDelete)
				NodeRemoved?.Invoke (this, new ObjectValueNodeEventArgs (node));
		}

		void OnDelete (object sender, EventArgs args)
		{
			OnDelete ();
		}

		bool CanDelete (out bool enabled)
		{
			enabled = false;

			if (!AllowWatchExpressions)
				return false;

			if (SelectedRowCount == 0)
				return false;

			enabled = true;

			var selectedRows = SelectedRows;
			foreach (var row in selectedRows) {
				var item = (MacObjectValueNode) ItemAtRow ((nint) row);

				if (!(item.Target.Parent is RootObjectValueNode)) {
					enabled = false;
					break;
				}
			}

			return true;
		}

		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.DeleteKey)]
		protected void OnUpdateDelete (CommandInfo cinfo)
		{
			cinfo.Visible = CanDelete (out bool enabled);
			cinfo.Enabled = enabled;
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

			var metadata = new Dictionary<string, object> ();
			metadata["ExpressionCount"] = expressions.Count;

			Counters.AddedWatchFromLocals.Inc (1, null, metadata);
		}

		void OnAddWatch (object sender, EventArgs args)
		{
			OnAddWatch ();
		}

		bool CanAddWatch (out bool enabled)
		{
			enabled = SelectedRowCount > 0;

			return true;
		}

		[CommandUpdateHandler (DebugCommands.AddWatch)]
		protected void OnUpdateAddWatch (CommandInfo cinfo)
		{
			cinfo.Visible = CanAddWatch (out bool enabled);
			cinfo.Enabled = enabled;
		}

		[CommandHandler (EditCommands.Rename)]
		protected void OnRename ()
		{
			if (SelectedRowCount != 1 || SelectedRow < 0)
				return;

			var nameView = (MacDebuggerObjectNameView) GetView (0, SelectedRow, false);

			nameView.Edit ();
		}

		void OnRename (object sender, EventArgs args)
		{
			OnRename ();
		}

		bool CanRename (out bool enabled)
		{
			if (SelectedRowCount == 1 && SelectedRow >= 0) {
				var item = (MacObjectValueNode) ItemAtRow (SelectedRow);

				enabled = item.Target.Parent is RootObjectValueNode || item.Target is AddNewExpressionObjectValueNode;
			} else {
				enabled = false;
			}

			return AllowWatchExpressions;
		}

		[CommandUpdateHandler (EditCommands.Rename)]
		protected void OnUpdateRename (CommandInfo cinfo)
		{
			cinfo.Visible = CanRename (out bool enabled);
			cinfo.Enabled = enabled;
		}

		public override NSMenu MenuForEvent (NSEvent theEvent)
		{
			if (!allowPopupMenu)
				return null;

			var point = ConvertPointFromEvent (this, theEvent);
			var row = GetRow (point);

			if (row < 0)
				return null;

			var menu = new NSMenu {
				AutoEnablesItems = false
			};
			bool enabled;

			if (CanAddWatch (out enabled)) {
				menu.AddItem (new NSMenuItem (GettextCatalog.GetString ("Add Watch"), OnAddWatch) {
					Enabled = enabled
				});
				menu.AddItem (NSMenuItem.SeparatorItem);
			}

			menu.AddItem (new NSMenuItem (GettextCatalog.GetString ("Copy"), OnCopy));

			if (CanRename (out enabled)) {
				menu.AddItem (new NSMenuItem (GettextCatalog.GetString ("Rename"), OnRename) {
					Enabled = enabled
				});
			}

			if (CanDelete (out enabled)) {
				menu.AddItem (new NSMenuItem (GettextCatalog.GetString ("Delete"), OnDelete) {
					Enabled = enabled
				});
			}

			return menu;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				PreviewWindowManager.WindowClosed -= OnPreviewWindowClosed;
				PreviewWindowManager.DestroyWindow ();
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
