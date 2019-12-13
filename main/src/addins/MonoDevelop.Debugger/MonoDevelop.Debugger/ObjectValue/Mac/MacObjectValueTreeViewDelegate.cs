//
// MacObjectValueTreeViewDelegate.cs
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

using AppKit;
using Foundation;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The worker delegate for the Cocoa implementation of the ObjectValueTreeView.
	/// </summary>
	class MacObjectValueTreeViewDelegate : NSOutlineViewDelegate
	{
		static readonly NSString NSObjectKey = new NSString ("NSObject");
		readonly MacObjectValueTreeView treeView;

		public MacObjectValueTreeViewDelegate (MacObjectValueTreeView treeView)
		{
			this.treeView = treeView;
		}

		public override NSView GetView (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			var view = (MacDebuggerObjectCellViewBase) outlineView.MakeView (tableColumn.Identifier, this);

			switch (tableColumn.Identifier) {
			case "name":
				if (view == null)
					view = new MacDebuggerObjectNameView (treeView);
				break;
			case "value":
				if (view == null)
					view = new MacDebuggerObjectValueView (treeView);
				break;
			case "type":
				if (view == null)
					view = new MacDebuggerObjectTypeView (treeView);
				break;
			case "pin":
				if (view == null)
					view = new MacDebuggerObjectPinView (treeView);
				break;
			default:
				return null;
			}

			view.Row = outlineView.RowForItem (item);
			view.ObjectValue = item;

			return view;
		}

#if false
		public override nfloat GetSizeToFitColumnWidth (NSOutlineView outlineView, nint column)
		{
			var columns = outlineView.TableColumns ();
			var col = columns[column];
			var columnWidth = col.MinWidth;

			for (nint row = 0; row < outlineView.RowCount; row++) {
				var rowView = outlineView.GetRowView (row, true);
				var columnView = rowView.ViewAtColumn (column);
				var width = columnView.Frame.Width;

				if (column == 0)
					width += columnView.Frame.X;

				if (width > columnWidth)
					columnWidth = NMath.Min (width, col.MaxWidth);
			}

			return columnWidth;
		}
#endif

		public override void ItemDidCollapse (NSNotification notification)
		{
			//var outlineView = (NSOutlineView) notification.Object;

			if (!notification.UserInfo.TryGetValue (NSObjectKey, out var value))
				return;

			var node = (value as MacObjectValueNode)?.Target;

			if (node == null)
				return;

			treeView.CollapseNode (node);
		}

		public override void ItemDidExpand (NSNotification notification)
		{
			//var outlineView = (NSOutlineView) notification.Object;

			if (!notification.UserInfo.TryGetValue (NSObjectKey, out var value))
				return;

			var node = value as MacObjectValueNode;

			if (node == null)
				return;

			node.HideValueButton = true;
			treeView.ReloadItem (node, false);
			treeView.ExpandNode (node.Target);
		}

		public override bool ShouldExpandItem (NSOutlineView outlineView, NSObject item)
		{
			if (!treeView.AllowExpanding)
				return false;

			var node = (item as MacObjectValueNode)?.Target;

			return node != null && node.HasChildren;
		}

		public override bool ShouldSelectItem (NSOutlineView outlineView, NSObject item)
		{
			return treeView.AllowsSelection;
		}

		public event EventHandler SelectionChanged;

		public override void SelectionDidChange (NSNotification notification)
		{
			SelectionChanged?.Invoke (this, EventArgs.Empty);
		}
	}
}
