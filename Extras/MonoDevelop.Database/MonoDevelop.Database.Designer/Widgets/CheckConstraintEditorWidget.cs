//
// Authors:
//    Ben Motmans  <ben.motmans@gmail.com>
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
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	public partial class CheckConstraintEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		
		private ISchemaProvider schemaProvider;
		private TableSchema table;
		private ColumnSchemaCollection columns;
		private ConstraintSchemaCollection constraints;
		
		private ListStore store;
		private SortedColumnListStore storeColumns;
		
		private const int colNameIndex = 0;
		private const int colColumnNameIndex = 1;
		private const int colIsColumnConstraintIndex = 2;
		private const int colSourceIndex = 3;
		private const int colObjIndex = 4;
		
		private bool columnConstraintsSupported;
		private bool tableConstraintsSupported;
		
		private SchemaActions action;
		
		public CheckConstraintEditorWidget (ISchemaProvider schemaProvider, SchemaActions action, TableSchema table, ColumnSchemaCollection columns, ConstraintSchemaCollection constraints)
		{
			if (columns == null)
				throw new ArgumentNullException ("columns");
			if (table == null)
				throw new ArgumentNullException ("table");
			if (constraints == null)
				throw new ArgumentNullException ("constraints");
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.table = table;
			this.columns = columns;
			this.constraints = constraints;
			this.action = action;
			
			this.Build();

			store = new ListStore (typeof (string), typeof (string), typeof (bool), typeof (string), typeof (object));
			storeColumns = new SortedColumnListStore (columns);

			listCheck.Model = store;

			TreeViewColumn colName = new TreeViewColumn ();
			TreeViewColumn colColumn = new TreeViewColumn ();
			TreeViewColumn colIsColumnConstraint = new TreeViewColumn ();
			
			colName.Title = GettextCatalog.GetString ("Name");
			colColumn.Title = GettextCatalog.GetString ("Column");
			colIsColumnConstraint.Title = GettextCatalog.GetString ("Column Constraint");
			
			colColumn.MinWidth = 120; //request a bigger width
			
			CellRendererText nameRenderer = new CellRendererText ();
			CellRendererCombo columnRenderer = new CellRendererCombo ();
			CellRendererToggle isColumnConstraintRenderer = new CellRendererToggle ();

			nameRenderer.Editable = true;
			nameRenderer.Edited += new EditedHandler (NameEdited);
			
			columnRenderer.Model = storeColumns.Store;
			columnRenderer.TextColumn = SortedColumnListStore.ColNameIndex;
			columnRenderer.Editable = true;
			columnRenderer.Edited += new EditedHandler (ColumnEdited);

			isColumnConstraintRenderer.Activatable = true;
			isColumnConstraintRenderer.Toggled += new ToggledHandler (IsColumnConstraintToggled);
			
			colName.PackStart (nameRenderer, true);
			colColumn.PackStart (columnRenderer, true);
			colIsColumnConstraint.PackStart (isColumnConstraintRenderer, true);

			colName.AddAttribute (nameRenderer, "text", colNameIndex);
			colColumn.AddAttribute (columnRenderer, "text", colColumnNameIndex);
			colIsColumnConstraint.AddAttribute (isColumnConstraintRenderer, "active", colIsColumnConstraintIndex);

			IDbFactory fac = schemaProvider.ConnectionPool.DbFactory;
			columnConstraintsSupported = fac.IsCapabilitySupported ("TableColumn", action, TableCapabilities.CheckConstraint);
			tableConstraintsSupported = fac.IsCapabilitySupported ("Table", action, TableCapabilities.CheckConstraint);
			
			listCheck.AppendColumn (colName);
			if (columnConstraintsSupported)
				listCheck.AppendColumn (colColumn);
			if (columnConstraintsSupported && tableConstraintsSupported)
				listCheck.AppendColumn (colIsColumnConstraint);
			
			listCheck.Selection.Changed += new EventHandler (OnSelectionChanged);
			sqlEditor.TextChanged += new EventHandler (SourceChanged);
			
			foreach (CheckConstraintSchema check in constraints.GetConstraints (ConstraintType.Check))
				AddConstraint (check);
			//TODO: also col constraints
			
			ShowAll ();
		}

		protected virtual void AddClicked (object sender, EventArgs e)
		{
			CheckConstraintSchema check = schemaProvider.GetNewCheckConstraintSchema ("check_new");
			int index = 1;
			while (constraints.Contains (check.Name))
				check.Name = "check_new" + (index++); 
			constraints.Add (check);
			AddConstraint (check);
			EmitContentChanged ();
		}

		protected virtual void RemoveClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (listCheck.Selection.GetSelected (out iter)) {
				CheckConstraintSchema check = store.GetValue (iter, colObjIndex) as CheckConstraintSchema;
				
				if (Services.MessageService.AskQuestion (
					GettextCatalog.GetString ("Are you sure you want to remove constraint '{0}'?", check.Name),
					GettextCatalog.GetString ("Remove Constraint")
				)) {
					store.Remove (ref iter);
					constraints.Remove (check);
					EmitContentChanged ();
				}
			}
		}
		
		protected virtual void OnSelectionChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			if (listCheck.Selection.GetSelected (out iter)) {
				buttonRemove.Sensitive = true;
				sqlEditor.Editable = true;
				
				CheckConstraintSchema check = store.GetValue (iter, colObjIndex) as CheckConstraintSchema;
				sqlEditor.Text = check.Source;
			} else {
				buttonRemove.Sensitive = false;
				sqlEditor.Editable = false;
				sqlEditor.Text = String.Empty;
			}
		}
		
		private void IsColumnConstraintToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, colIsColumnConstraintIndex);
	 			store.SetValue (iter, colIsColumnConstraintIndex, !val);
				EmitContentChanged ();
	 		}
		}
		
		private void NameEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				if (!string.IsNullOrEmpty (args.NewText)) {
					store.SetValue (iter, colNameIndex, args.NewText);
					EmitContentChanged ();
				} else {
					string oldText = store.GetValue (iter, colNameIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void ColumnEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				if (columns.Contains (args.NewText)) { //only allow existing columns
					store.SetValue (iter, colColumnNameIndex, args.NewText);
					EmitContentChanged ();
				} else {
					string oldText = store.GetValue (iter, colColumnNameIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void SourceChanged (object sender, EventArgs args)
		{
			TreeIter iter;
			if (listCheck.Selection.GetSelected (out iter)) {
				CheckConstraintSchema check = store.GetValue (iter, colObjIndex) as CheckConstraintSchema;
				check.Source = sqlEditor.Text;
				store.SetValue (iter, colSourceIndex, sqlEditor.Text);
				EmitContentChanged ();
			}
		}
		
		private void AddConstraint (CheckConstraintSchema check)
		{
			store.AppendValues (check.Name, String.Empty, false, String.Empty, check);
		}
		
		protected virtual void EmitContentChanged ()
		{
			if (ContentChanged != null)
				ContentChanged (this, EventArgs.Empty);
		}
		
		public virtual bool ValidateSchemaObjects (out string msg)
		{ 
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					string name = store.GetValue (iter, colNameIndex) as string;
					string column = store.GetValue (iter, colColumnNameIndex) as string;
					string source = store.GetValue (iter, colSourceIndex) as string;
					bool iscolc = (bool)store.GetValue (iter, colIsColumnConstraintIndex);
					
					if (String.IsNullOrEmpty (source)) {
						msg = GettextCatalog.GetString ("Checked constraint '{0}' does not contain a check statement.", name);
						return false;
					}
					
					if (iscolc && String.IsNullOrEmpty (column)) {
						msg = GettextCatalog.GetString ("Checked constraint '{0}' is marked as a column constraint but is not applied to a column.", name);
						return false;
					}
				} while (store.IterNext (ref iter));
			}
			msg = null;
			return true;
		}
		
		public virtual void FillSchemaObjects ()
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					CheckConstraintSchema check = store.GetValue (iter, colObjIndex) as CheckConstraintSchema;
					
					check.Name = store.GetValue (iter, colNameIndex) as string;
					check.Source = store.GetValue (iter, colSourceIndex) as string;
					check.IsColumnConstraint = (bool)store.GetValue (iter, colIsColumnConstraintIndex);
					
					if (check.IsColumnConstraint) {
						string column = store.GetValue (iter, colColumnNameIndex) as string;
						check.Columns.Add (columns.Search (column));
					}
					
					table.Constraints.Add (check);
				} while (store.IterNext (ref iter));
			}
		}
	}
}
