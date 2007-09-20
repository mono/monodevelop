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
using System.Collections.Generic;using MonoDevelop.Database.Sql;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Components
{
	public class ColumnMappingWidget : ScrolledWindow
	{
		private TreeView list;
		private ListStore store;

		private const int columnSelected = 0;
		private const int columnObj = 1;
		
		public ColumnMappingWidget (bool showCheckBoxes)
		{
			store = new ListStore (typeof (bool), typeof (ColumnContainer));
			list = new TreeView (store);
			
			TreeViewColumn colName = new TreeViewColumn ();
			colName.Title = GettextCatalog.GetString ("Name");

			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Activatable = true;
			toggleRenderer.Toggled += new ToggledHandler (SelectToggled);
			colName.PackStart (toggleRenderer, false);
			CellRendererText nameRenderer = new CellRendererText ();
			colName.PackStart (nameRenderer, true);

			TreeViewColumn colType = new TreeViewColumn ();
			colType.Title = GettextCatalog.GetString ("Type");
			CellRendererText typeRenderer = new CellRendererText ();
			colType.PackStart (typeRenderer, true);
			
			TreeViewColumn colPropName = new TreeViewColumn ();
			colPropName.Title = GettextCatalog.GetString ("Property Name");
			CellRendererText propNameRenderer = new CellRendererText ();
			propNameRenderer.Editable = true;
			propNameRenderer.Edited += new EditedHandler (PropNameEdited);
			colPropName.PackStart (propNameRenderer, true);
			
			TreeViewColumn colPropType = new TreeViewColumn ();
			colPropType.Title = GettextCatalog.GetString ("Property Type");
			CellRendererTypeCombo propTypeRenderer = new CellRendererTypeCombo ();
			colPropType.PackStart (propTypeRenderer, true);
			
			TreeViewColumn colNullable = new TreeViewColumn ();
			colNullable.Title = GettextCatalog.GetString ("Nullable");
			CellRendererToggle nullableRenderer = new CellRendererToggle ();
			colNullable.PackStart (nullableRenderer, false);
			
			TreeViewColumn colSetter = new TreeViewColumn ();
			colSetter.Title = GettextCatalog.GetString ("Create Setter");
			CellRendererToggle setterRenderer = new CellRendererToggle ();
			setterRenderer.Activatable = true;
			setterRenderer.Toggled += new ToggledHandler (SetterToggled);
			colSetter.PackStart (setterRenderer, false);
			
			TreeViewColumn colCtor = new TreeViewColumn ();
			colCtor.Title = GettextCatalog.GetString ("Ctor Parameter");
			CellRendererToggle ctorParamRenderer = new CellRendererToggle ();
			ctorParamRenderer.Activatable = true;
			ctorParamRenderer.Toggled += new ToggledHandler (CtorParamToggled);
			colCtor.PackStart (ctorParamRenderer, false);
			
			colName.SetCellDataFunc (nameRenderer, new CellLayoutDataFunc (NameDataFunc));
			colType.SetCellDataFunc (typeRenderer, new CellLayoutDataFunc (TypeDataFunc));
			colPropName.SetCellDataFunc (propNameRenderer, new CellLayoutDataFunc (PropNameDataFunc));
			colPropType.SetCellDataFunc (propTypeRenderer, new CellLayoutDataFunc (PropTypeDataFunc));
			colNullable.SetCellDataFunc (nullableRenderer, new CellLayoutDataFunc (NullableDataFunc));
			colSetter.SetCellDataFunc (setterRenderer, new CellLayoutDataFunc (SetterDataFunc));
			colCtor.SetCellDataFunc (ctorParamRenderer, new CellLayoutDataFunc (CtorDataFunc));

			list.AppendColumn (colName);
			list.AppendColumn (colType);
			list.AppendColumn (colPropName);
			list.AppendColumn (colPropType);
			list.AppendColumn (colNullable);
			list.AppendColumn (colSetter);
			list.AppendColumn (colCtor);
			list.HeadersVisible = true;
			
			this.Add (list);
		}
		
		public void Append (IEnumerable<ColumnContainer> columns)
		{
			foreach (ColumnContainer column in columns) {
				//TODO: make up a nice property name
				store.AppendValues (true, column);
			}
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
		
		private void PropNameDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			textRenderer.Text = container.PropertyName;
		}
		
		private void PropTypeDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			//TODO: map the DB datatype to a .NET datatype
		}
		
		private void NullableDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle toggleRenderer = cell as CellRendererToggle;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			toggleRenderer.Active = container.ColumnSchema.IsNullable;
		}
		
		private void SetterDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle toggleRenderer = cell as CellRendererToggle;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			toggleRenderer.Active = container.HasSetter;
		}
		
		private void CtorDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle toggleRenderer = cell as CellRendererToggle;
			ColumnContainer container = model.GetValue (iter, columnObj) as ColumnContainer;
			toggleRenderer.Active = container.IsCtorParameter;
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
		
		private void CtorParamToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				ColumnContainer container = store.GetValue (iter, columnObj) as ColumnContainer;
				container.IsCtorParameter = !container.IsCtorParameter;
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
	}
}