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
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public class ColumnMappingWidget : ScrolledWindow
	{
		private TreeView list;
		private ListStore store;

		private const int columnSelected = 0;
		private const int columnObj = 1;
		
		public ColumnMappingWidget ()
			: this (true)
		{
		}
		
		public ColumnMappingWidget (bool showCheckBoxes)
			: base ()
		{
			store = new ListStore (typeof (bool), typeof (ColumnContainer));
			list = new TreeView (store);
			
			TreeViewColumn colName = new TreeViewColumn ();
			colName.Title = AddinCatalog.GetString ("Name");

			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Activatable = true;
			toggleRenderer.Toggled += new ToggledHandler (SelectToggled);
			colName.PackStart (toggleRenderer, false);
			CellRendererText nameRenderer = new CellRendererText ();
			colName.PackStart (nameRenderer, true);

			TreeViewColumn colType = new TreeViewColumn ();
			colType.Title = AddinCatalog.GetString ("Db Type");
			CellRendererText typeRenderer = new CellRendererText ();
			colType.PackStart (typeRenderer, true);
			
			TreeViewColumn colPropType = new TreeViewColumn ();
			colPropType.Title = AddinCatalog.GetString ("Type");
			CellRendererText propTypeRenderer = new CellRendererText ();
			colPropType.PackStart (propTypeRenderer, true);
			
			TreeViewColumn colFieldName = new TreeViewColumn ();
			colFieldName.Title = AddinCatalog.GetString ("Field Name");
			CellRendererText fieldNameRenderer = new CellRendererText ();
			fieldNameRenderer.Editable = true;
			fieldNameRenderer.Edited += new EditedHandler (FieldNameEdited);
			colFieldName.PackStart (fieldNameRenderer, true);
			
			TreeViewColumn colPropName = new TreeViewColumn ();
			colPropName.Title = AddinCatalog.GetString ("Property Name");
			CellRendererText propNameRenderer = new CellRendererText ();
			propNameRenderer.Editable = true;
			propNameRenderer.Edited += new EditedHandler (PropNameEdited);
			colPropName.PackStart (propNameRenderer, true);
	
			TreeViewColumn colSetter = new TreeViewColumn ();
			colSetter.Title = AddinCatalog.GetString ("Setter");
			CellRendererToggle setterRenderer = new CellRendererToggle ();
			setterRenderer.Activatable = true;
			setterRenderer.Toggled += new ToggledHandler (SetterToggled);
			colSetter.PackStart (setterRenderer, false);
			
			colName.SetCellDataFunc (nameRenderer, new CellLayoutDataFunc (NameDataFunc));
			colType.SetCellDataFunc (typeRenderer, new CellLayoutDataFunc (TypeDataFunc));
			colPropName.SetCellDataFunc (fieldNameRenderer, new CellLayoutDataFunc (FieldNameDataFunc));
			colPropName.SetCellDataFunc (propNameRenderer, new CellLayoutDataFunc (PropNameDataFunc));
			colPropType.SetCellDataFunc (propTypeRenderer, new CellLayoutDataFunc (PropTypeDataFunc));
			colSetter.SetCellDataFunc (setterRenderer, new CellLayoutDataFunc (SetterDataFunc));

			list.AppendColumn (colName);
			list.AppendColumn (colType);
			list.AppendColumn (colFieldName);
			list.AppendColumn (colPropName);
			list.AppendColumn (colPropType);
			list.AppendColumn (colSetter);;
			list.HeadersVisible = true;
			
			this.Add (list);
		}
		
		public void Append (IEnumerable<ColumnContainer> columns)
		{
			foreach (ColumnContainer column in columns)
				store.AppendValues (true, column);
		}
		
		public IEnumerable<ColumnContainer> CheckedColumns {
			get {
				TreeIter iter;
				if (store.GetIterFirst (out iter)) {
					do {
						bool chk = (bool)store.GetValue (iter, columnSelected);
						if (chk)
							yield return store.GetValue (iter, columnObj) as ColumnContainer;
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
					store.SetValue (iter, columnSelected, state);
				} while (store.IterNext (ref iter));
			}	
		}
		
		private void NameDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			textRenderer.Text = container.ColumnSchema.Name;
		}
		
		private void TypeDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			textRenderer.Text = container.ColumnSchema.DataTypeName;
		}
		
		private void FieldNameDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			textRenderer.Text = container.FieldName;
		}
		
		private void PropNameDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			textRenderer.Text = container.PropertyName;
		}
		
		private void PropTypeDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			DataTypeSchema dt = container.ColumnSchema.DataType;
			
			ISchemaProvider provider = dt.SchemaProvider;
			Type type = dt.DotNetType;
			textRenderer.Text = type.Name;
		}
		
		private void SetterDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle toggleRenderer = cell as CellRendererToggle;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			toggleRenderer.Active = container.HasSetter;
		}
		
		private void SelectToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, columnSelected);
	 			store.SetValue (iter, columnSelected, !val);
	 		}
		}
		
		private void SetterToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				ColumnContainer container = store.GetValue (iter, columnObj) as ColumnContainer;
				container.HasSetter = !container.HasSetter;
	 		}
		}
		
		private void PropNameEdited (object sender, EditedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				ColumnContainer container = store.GetValue (iter, columnObj) as ColumnContainer;
				
				if (args.NewText != null && args.NewText.Length > 0) {
					container.PropertyName = args.NewText;
				} else {
					//restore old name if new one is empty
					(sender as CellRendererText).Text = container.PropertyName;
				}
			}
		}
		
		private void FieldNameEdited (object sender, EditedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				ColumnContainer container = store.GetValue (iter, columnObj) as ColumnContainer;
				
				if (args.NewText != null && args.NewText.Length > 0) {
					container.FieldName = args.NewText;
				} else {
					//restore old name if new one is empty
					(sender as CellRendererText).Text = container.FieldName;
				}
			}
		}
	}
}