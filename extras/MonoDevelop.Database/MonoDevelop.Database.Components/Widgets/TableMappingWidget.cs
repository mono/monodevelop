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
using MonoDevelop.Database.Sql;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Components
{
	[System.ComponentModel.Category("MonoDevelop.Database.Components")]
	[System.ComponentModel.ToolboxItem(true)]
	public class TableMappingWidget : ScrolledWindow
	{
		private TreeView list;
		private ListStore store;

		private const int tableSelected = 0;
		private const int tableObj = 1;
		
		public TableMappingWidget ()
			: this (true)
		{
		}
		
		public TableMappingWidget (bool showCheckBoxes)
		{
			store = new ListStore (typeof (bool), typeof (TableContainer));
			list = new TreeView (store);
			
			TreeViewColumn colName = new TreeViewColumn ();
			colName.Title = AddinCatalog.GetString ("Name");

			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Activatable = true;
			toggleRenderer.Toggled += new ToggledHandler (SelectToggled);
			colName.PackStart (toggleRenderer, false);
			CellRendererText nameRenderer = new CellRendererText ();
			colName.PackStart (nameRenderer, true);

			TreeViewColumn colClassName = new TreeViewColumn ();
			colClassName.Title = AddinCatalog.GetString ("Class Name");
			CellRendererText propNameRenderer = new CellRendererText ();
			propNameRenderer.Editable = true;
			propNameRenderer.Edited += new EditedHandler (ClassNameEdited);
			colClassName.PackStart (propNameRenderer, true);
			
			colName.SetCellDataFunc (nameRenderer, new CellLayoutDataFunc (NameDataFunc));
			colClassName.SetCellDataFunc (propNameRenderer, new CellLayoutDataFunc (ClassNameDataFunc));

			list.AppendColumn (colName);
			list.AppendColumn (colClassName);
			list.HeadersVisible = true;
			
			this.Add (list);
		}
		
		public void Append (IEnumerable<TableContainer> tables)
		{
			foreach (TableContainer table in tables)
				store.AppendValues (true, table);
		}
		
		public IEnumerable<TableContainer> CheckedTables {
			get {
				TreeIter iter;
				if (store.GetIterFirst (out iter)) {
					do {
						bool chk = (bool)store.GetValue (iter, tableSelected);
						if (chk)
							yield return store.GetValue (iter, tableObj) as TableContainer;
					} while (store.IterNext (ref iter));
				}
			}
		}
		
		public void SelectAll ()
		{
			SetSelectState (true);
		}
		
		public void DeselectAll ()
		{
			SetSelectState (false);
		}

		private void SetSelectState (bool state)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					store.SetValue (iter, tableSelected, state);
				} while (store.IterNext (ref iter));
			}	
		}
		
		private void NameDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			TableContainer container = model.GetValue (iter, tableObj) as TableContainer;
			textRenderer.Text = container.TableSchema.Name;
		}
		
		private void ClassNameDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			TableContainer container = model.GetValue (iter, tableObj) as TableContainer;
			textRenderer.Text = container.ClassName;
		}
		
		private void SelectToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, tableSelected);
	 			store.SetValue (iter, tableSelected, !val);
	 		}
		}
		
		private void ClassNameEdited (object sender, EditedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				TableContainer container = store.GetValue (iter, tableObj) as TableContainer;
				
				if (args.NewText != null && args.NewText.Length > 0) {
					//TODO: check if valid class name
					container.ClassName = args.NewText;
				} else {
					//restore old name if new one is empty
					(sender as CellRendererText).Text = container.ClassName;
				}
			}
		}
	}
}