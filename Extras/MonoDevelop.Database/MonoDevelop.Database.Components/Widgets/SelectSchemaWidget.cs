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

namespace MonoDevelop.Database.Components
{
	public class SelectSchemaWidget : ScrolledWindow
	{
		private TreeView list;
		private ListStore store;

		private const int columnSelected = 0;
		private const int columnObj = 1;
		
		public SelectSchemaWidget ()
		{
			store = new ListStore (typeof (bool), typeof (ISchemaContainer));
			list = new TreeView (store);
			
			TreeViewColumn col = new TreeViewColumn ();

			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Activatable = true;
			toggleRenderer.Toggled += new ToggledHandler (ItemToggled);
			col.PackStart (toggleRenderer, false);
			
			CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
			col.PackStart (pixbufRenderer, false);

			CellRendererText textRenderer = new CellRendererText ();
			col.PackStart (textRenderer, true);

			col.SetCellDataFunc (textRenderer, new CellLayoutDataFunc (TextDataFunc));
			col.SetCellDataFunc (pixbufRenderer, new CellLayoutDataFunc (PixbufDataFunc));

			list.AppendColumn (col);
			list.HeadersVisible = false;
			
			this.Add (list);
		}
		
		public void Append (IEnumerable<TableSchema> tables)
		{
			if (tables == null)
				throw new ArgumentNullException ("tables");
			
			List<ISchemaContainer> containers = new List<ISchemaContainer> ();
			foreach (TableSchema table in tables)
				containers.Add (new TableSchemaContainer (table));
			PopulateList (containers);
		}
		
		public void Append (IEnumerable<ViewSchema> views)
		{
			if (views == null)
				throw new ArgumentNullException ("views");
			
			List<ISchemaContainer> containers = new List<ISchemaContainer> ();
			foreach (ViewSchema view in views)
				containers.Add (new ViewSchemaContainer (view));
			PopulateList (containers);
		}
		
		public void Append (IEnumerable<ProcedureSchema> procedures)
		{
			if (procedures == null)
				throw new ArgumentNullException ("procedures");
			
			List<ISchemaContainer> containers = new List<ISchemaContainer> ();
			foreach (ProcedureSchema procedure in procedures)
				containers.Add (new ProcedureSchemaContainer (procedure));
			PopulateList (containers);
		}

		public IEnumerable<ISchemaContainer> CheckedSchemas {
			get {
				TreeIter iter;
				if (store.GetIterFirst (out iter)) {
					do {
						bool chk = (bool)store.GetValue (iter, columnSelected);
						if (chk)
							yield return store.GetValue (iter, columnObj) as ISchemaContainer;
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
		
		private void PopulateList (IEnumerable<ISchemaContainer> containers)
		{
			foreach (ISchemaContainer container in containers)
				store.AppendValues (true, container);
		}
		
		private void TextDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textRenderer = cell as CellRendererText;
			ISchemaContainer schema = model.GetValue (iter, columnObj) as ISchemaContainer;
			textRenderer.Text = schema.Schema.Name;
		}
		
		private void PixbufDataFunc (CellLayout layout, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererPixbuf pixbufRenderer = cell as CellRendererPixbuf;
			ISchemaContainer schema = model.GetValue (iter, columnObj) as ISchemaContainer;
			
			string iconString = null;
			switch (schema.SchemaContainerType) {
				case SchemaContainerType.Table:
					iconString = "md-db-table";
					break;
				case SchemaContainerType.View:
					iconString = "md-db-view";
					break;
				case SchemaContainerType.Procedure:
					iconString = "md-db-procedure";
					break;
				case SchemaContainerType.Query:
					//TODO: iconString = Stock.Execute;
					break;
			}

			if (iconString != null)
				pixbufRenderer.Pixbuf = MonoDevelop.Core.Gui.Services.Resources.GetIcon (iconString);
		}
		
		private void ItemToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, columnSelected);
	 			store.SetValue (iter, columnSelected, !val);
	 		}
		}
	}
}