//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	public class SelectColumnWidget : ScrolledWindow
	{
		public event EventHandler ColumnToggled;
		
		protected TreeView list;
		protected SortedColumnListStore store;
		
		private ColumnSchemaCollection columns;
		
		public SelectColumnWidget ()
			: this (true)
		{
		}
		
		public SelectColumnWidget (bool showCheckBoxes)
		{
			list = new TreeView ();
			list.HeadersVisible = true;
			
			InitializeColumns (showCheckBoxes);
			
			this.Add (list);
		}
		
		public bool SingleCheck {
			get { return store.SingleCheck; }
			set {
				if (store == null)
					return; //when init isn't called yet
				store.SingleCheck = value;
			}
		}

		protected virtual void InitializeColumns (bool showCheckBoxes)
		{
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Column");

			if (showCheckBoxes) {
				CellRendererToggle toggleRenderer = new CellRendererToggle ();
				toggleRenderer.Activatable = true;
				toggleRenderer.Toggled += new ToggledHandler (ItemToggled);
				col.PackStart (toggleRenderer, false);
				col.AddAttribute (toggleRenderer, "active", SortedColumnListStore.ColSelectIndex);
			}

			CellRendererText textRenderer = new CellRendererText ();
			col.PackStart (textRenderer, true);
			col.AddAttribute (textRenderer, "text", SortedColumnListStore.ColNameIndex);

			list.AppendColumn (col);
		}
		
		public void Initialize (ColumnSchemaCollection columns)
		{
			this.columns = columns;
			
			store = new SortedColumnListStore (columns);
			store.ColumnToggled += delegate (object sender, EventArgs args) {
				if (ColumnToggled != null)
					ColumnToggled (this, args);
			};
			list.Model = store.Store;
		}
		
		public ColumnSchema SelectedColumn {
			get {
				TreeIter iter;
				if (list.Selection.GetSelected (out iter))
					return store.GetColumnSchema (iter);
				return null;
			}
		}
		
		public IEnumerable<ColumnSchema> CheckedColumns {
			get { return store.CheckedColumns; }
		}
		
		public bool IsColumnChecked {
			get { return store.IsColumnChecked; }
		}
		
		public void SelectAll ()
		{
			store.SelectAll ();
		}
		
		public void DeselectAll ()
		{
			store.DeselectAll ();
		}
		
		public void Select (string name)
		{
			store.Select (name);
		}
		
		public void Select (ColumnSchema column)
		{
			store.Select (column);
		}
		
		private void ItemToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.Store.GetIterFromString (out iter, args.Path))
	 			store.ToggleSelect (iter);
		}
	}
}