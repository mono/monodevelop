//
// PriorityList.cs
//
// Authors:
//  Helmut Duregger <helmutduregger@gmx.at>
//
// Copyright (c) 2010 Helmut Duregger
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

using System;
using Gtk;

namespace MonoDevelop.Ide.Gui.Components
{
	/// <summary>
	/// Provides a list of items that can be reordered by pressing
	/// up and down buttons.
	/// </summary>
	/// <remarks>
	/// Uses a Gtk.ListStore as underlying model. Button sensitivity
	/// state is updated according to the selected item. For instance the
	/// last item in the list can not be moved further downward. Consequently the
	/// down button will be greyed out.
	/// This is based on the original DebuggerOptionsPanelWidget priority list
	/// design and code. We expose it here for reuse in other GUIs.
	/// </remarks>
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PriorityList : Bin
	{
		//
		// Store a list store explicitly to guarantee a
		// list store as the underlying model of the tree
		// view.
		//

		ListStore listStore;

		/// <remarks>
		/// The priority list listens for changes in this model and
		/// updates the up and down button greyed out states whenever
		/// rows are inserted, deleted or reordered.
		/// </remarks>
		public ListStore Model {

			get {
				return listStore;
			}

			set {

				if (listStore != null) {
					listStore.RowInserted   -= HandleRowInserted;
					listStore.RowDeleted    -= HandleRowDeleted;
					listStore.RowsReordered -= HandleRowsReordered;
				}

				listStore      = value;
				treeview.Model = value;

				listStore.RowInserted   += HandleRowInserted;
				listStore.RowDeleted    += HandleRowDeleted;
				listStore.RowsReordered += HandleRowsReordered;
			}
		}

		/// <remarks>
		/// Registers a selection handler that updates up and down button states
		/// depending on the current selected item.
		/// </remarks>
		public PriorityList ()
		{
			this.Build ();

			treeview.Selection.Changed += HandleSelectionChanged;
		}

		/// <summary>
		/// Updates the greyed out state of the up and down buttons
		/// depending on the currently selected item.
		/// </summary>
		protected void UpdatePriorityButtons ()
		{
			TreePath[] paths = treeview.Selection.GetSelectedRows ();
			if (paths.Length > 0) {
				TreePath p = paths [0];
				TreeIter it;
				listStore.GetIter (out it, p);
				buttonDown.Sensitive = listStore.IterNext (ref it);
				buttonUp.Sensitive = p.Prev ();
			} else {
				buttonDown.Sensitive = buttonUp.Sensitive = false;
			}
		}

		//
		// Expose some useful TreeView methods.
		//
		// Unfortunately we can only limit a TreeView to only accept ListStores by
		//
		//     a. throwing an Exception some time after a TreeStore was set
		//     b. encapsulating the TreeView
		//
		// Here we do the latter. There is many methods in the TreeView signature but
		// we will only expose the most useful ones for now.
		//

		/// <seealso cref="TreeView"/>
		public int AppendColumn (TreeViewColumn column)
		{
			return treeview.AppendColumn (column);
		}

		/// <seealso cref="TreeView"/>
		public TreeViewColumn AppendColumn (string title, CellRenderer cellRenderer, CellLayoutDataFunc cellLayoutDataFunction)
		{
			return treeview.AppendColumn (title, cellRenderer, cellLayoutDataFunction);
		}

		/// <seealso cref="TreeView"/>
		public TreeViewColumn AppendColumn (string title, CellRenderer cellRenderer, TreeCellDataFunc treeCellDataFunction)
		{
			return treeview.AppendColumn (title, cellRenderer, treeCellDataFunction);
		}

		/// <seealso cref="TreeView"/>
		public TreeViewColumn AppendColumn (string title, CellRenderer cellRenderer, params object[] attributes)
		{
			return treeview.AppendColumn (title, cellRenderer, attributes);
		}

		/// <summary>
		/// Updates the button greyed out states whenever the selection changes.
		/// </summary>
		protected void HandleSelectionChanged (object sender, EventArgs e)
		{
			UpdatePriorityButtons ();
		}

		/// <summary>
		/// Updates the button greyed out states whenever an item is inserted.
		/// </summary>
		protected void HandleRowInserted (object sender, RowInsertedArgs a)
		{
			UpdatePriorityButtons ();
		}

		/// <summary>
		/// Updates the button greyed out states whenever an item is deleted.
		/// </summary>
		protected void HandleRowDeleted (object sender, RowDeletedArgs a)
		{
			UpdatePriorityButtons ();
		}

		/// <summary>
		/// Updates the button greyed out states whenever items are reordered.
		/// </summary>
		/// <remarks>
		/// This is required for ListStore.Swap ().
		/// </remarks>
		protected void HandleRowsReordered (object sender, RowsReorderedArgs a)
		{
			UpdatePriorityButtons ();
		}

		/// <summary>
		/// Moves the selected item up if possible.
		/// </summary>
		protected void OnButtonUpClicked (object sender, System.EventArgs e)
		{
			TreePath[] paths = treeview.Selection.GetSelectedRows ();
			if (paths.Length > 0) {
				TreePath p = paths [0];
				TreeIter it1, it2;
				listStore.GetIter (out it2, p);
				if (p.Prev () && listStore.GetIter (out it1, p)) {
					listStore.Swap (it1, it2);
				}
			}
		}

		/// <summary>
		/// Moves the selected item down if possible.
		/// </summary>
		protected void OnButtonDownClicked (object sender, System.EventArgs e)
		{
			TreeIter i1;
			if (treeview.Selection.GetSelected (out i1)) {
				TreeIter i2 = i1;
				if (listStore.IterNext (ref i2)) {
					listStore.Swap (i1, i2);
				}
			}
		}
	}
}

