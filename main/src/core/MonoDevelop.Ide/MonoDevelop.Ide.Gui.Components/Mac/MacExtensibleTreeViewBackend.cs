//
// MacExtensibleTreeViewBackend.cs
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

#if MAC

using System;
using AppKit;
using System.Linq;
using MonoDevelop.Components.Mac;

namespace MonoDevelop.Ide.Gui.Components.Internal
{
	class MacExtensibleTreeViewBackend: ExtensibleTreeViewBackend
	{
		IExtensibleTreeViewFrontend frontend;
		TreeView tree;
		TreeSource source;
		NSScrollView scroll;
		int lastVersion;

		public MacExtensibleTreeViewBackend ()
		{
		}

		public override void Initialize (IExtensibleTreeViewFrontend frontend)
		{
			this.frontend = frontend;
			source = new TreeSource (frontend);
			tree = new TreeView (frontend, source);
			scroll = new NSScrollView ();
			scroll.DocumentView = tree;
			scroll.BorderType = NSBorderType.NoBorder;
			scroll.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
			scroll.AutoresizesSubviews = true;
			tree.HeaderView = null;

			var tcol = new NSTableColumn ();
			tcol.Editable = false;
			var c = new CompositeCell ();
			tcol.DataCell = c;
			tree.AddColumn (tcol);
			tree.OutlineTableColumn = tcol;

			tree.DataSource = source;
			lastVersion = source.Version;
		}

		public override bool AllowsMultipleSelection {
			get { return tree.AllowsMultipleSelection; }
			set { tree.AllowsMultipleSelection = value; }
		}

		public override object CreateWidget ()
		{
			return scroll;
		}

		public override TreeNodeNavigator CreateNavigator ()
		{
			return new MacTreeNodeNavigator (frontend, tree, source, null);
		}

		public override NodePosition GetSelectedNode ()
		{
			var row = tree.SelectedRow;
			if (row == -1)
				return null;

			return source.GetItemPosition ((TreeItem)tree.ItemAtRow (row));
		}

		public override bool MultipleNodesSelected ()
		{
			return tree.SelectedRowCount > 1;
		}

		public override NodePosition[] GetSelectedNodes ()
		{
			if (tree.SelectedRowCount == 0)
				return new NodePosition[0];
			else
				return tree.SelectedRows.Select (r => (TreeItem)tree.ItemAtRow ((int)r)).Where (i => i != null).Select (r => source.GetItemPosition (r)).ToArray<NodePosition> ();
		}

		public override TreeNodeNavigator GetNodeAtPosition (NodePosition position)
		{
			return new MacTreeNodeNavigator (frontend, tree, source, (MacNodePosition) position);
		}

		public override TreeNodeNavigator GetRootNode ()
		{
			var root = source.GetRootPosition ();
			if (root != null)
				return GetNodeAtPosition (root);
			else
				return null;
		}

		public override void StartLabelEdit ()
		{
		}

		public override void CollapseTree ()
		{
			foreach (var n in source.RootNodes)
				tree.CollapseItem (n);
		}

		public override void ScrollToCell (NodePosition pos)
		{
			tree.ScrollRowToVisible (tree.RowForItem (source.GetItem (pos)));
		}

		public override bool IsChildPosition (NodePosition parent, NodePosition potentialChild, bool recursive)
		{
			var child = (MacNodePosition)potentialChild;
			while (source.MoveToParent (ref child)) {
				if (child.Equals (parent))
					return true;
			}
			return false;
		}


		bool showSelectionPopupButton;

		public override bool ShowSelectionPopupButton {
			get { return showSelectionPopupButton; }
			set {
				showSelectionPopupButton = value;
			}
		}

		bool isReloading;

		public override void EndTreeUpdate ()
		{
			if (isReloading)
				return;

			if (source.Version != lastVersion) {
				lastVersion = source.Version;
				try {
					isReloading = true;
					tree.ReloadData ();
				} finally {
					isReloading = false;
				}
			}
		}
	}

	class TreeView: NSOutlineView
	{
		IExtensibleTreeViewFrontend frontend;
		TreeSource source;

		public TreeView (IExtensibleTreeViewFrontend frontend, TreeSource source)
		{
			this.frontend = frontend;
			this.source = source;
			RowHeight += 4; // Add some space for the overlays
		}

		public override NSMenu MenuForEvent (NSEvent theEvent)
		{
			var m = frontend.CreateContextMenu ();
			if (m != null)
				return IdeApp.CommandService.CreateNSMenu (m);
			else
				return null;
		}

		public override void MouseDown (NSEvent theEvent)
		{
			base.MouseDown (theEvent);
			if (theEvent.ClickCount == 2)
				frontend.OnCurrentItemActivated ();
		}
	}
}

#endif