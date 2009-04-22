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
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class PrimaryKeyConstraintEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		public event EventHandler PrimaryKeyChanged;
		
		private ISchemaProvider schemaProvider;
		private TableSchema table;
		private ColumnSchemaCollection columns;
		private ConstraintSchemaCollection constraints;
		
		private SchemaActions action;
		
		private ListStore store;
		
		private const int colNameIndex = 0;
		private const int colColumnsIndex = 1;
		private const int colObjIndex = 2;
		
		public PrimaryKeyConstraintEditorWidget (ISchemaProvider schemaProvider, SchemaActions action)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
			
			this.Build();
			store = new ListStore (typeof (string), typeof (string), typeof (object));
			listPK.Model = store;
			
			TreeViewColumn colName = new TreeViewColumn ();
			
			colName.Title = AddinCatalog.GetString ("Name");
			CellRendererText nameRenderer = new CellRendererText ();
			
			nameRenderer.Editable = true;
			nameRenderer.Edited += new EditedHandler (NameEdited);
			
			colName.PackStart (nameRenderer, true);
			colName.AddAttribute (nameRenderer, "text", colNameIndex);
			listPK.AppendColumn (colName);
			
			listPK.Selection.Changed += new EventHandler (SelectionChanged);
			columnSelecter.ColumnToggled += new EventHandler (ColumnToggled);
			
			ShowAll ();
		}
		
		public void Initialize (TableSchema table, ColumnSchemaCollection columns, ConstraintSchemaCollection constraints)
		{
			if (columns == null)
				throw new ArgumentNullException ("columns");
			if (table == null)
				throw new ArgumentNullException ("table");
			if (constraints == null)
				throw new ArgumentNullException ("constraints");
			
			this.table = table;
			this.columns = columns;
			this.constraints = constraints;

			columnSelecter.Initialize (columns);
			RefreshConstraints ();
		}
		
		public virtual void RefreshConstraints ()
		{
			store.Clear ();
			foreach (PrimaryKeyConstraintSchema pk in constraints.GetConstraints (ConstraintType.PrimaryKey))
				AddConstraint (pk);
		}
		
		
		private void NameEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				if (!string.IsNullOrEmpty (args.NewText) && !constraints.Contains (args.NewText)) {
					store.SetValue (iter, colNameIndex, args.NewText);
					EmitContentChanged ();
				} else {
					string oldText = store.GetValue (iter, colNameIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void SelectionChanged (object sender, EventArgs args)
		{
			columnSelecter.DeselectAll ();
			
			TreeIter iter;
			if (listPK.Selection.GetSelected (out iter)) {
				columnSelecter.Sensitive = true;
				buttonRemove.Sensitive = true;

				string colstr = store.GetValue (iter, colColumnsIndex) as string;
				string[] cols = colstr.Split (',');
				foreach (string col in cols)
					columnSelecter.Select (col);
			} else {
				columnSelecter.Sensitive = false;
				buttonRemove.Sensitive = false;
			}
		}
		
		private void ColumnToggled (object sender, EventArgs args)
		{
			TreeIter iter;
			if (listPK.Selection.GetSelected (out iter)) {
				bool first = true;
				StringBuilder sb = new StringBuilder ();
				ConstraintSchema constraint = (ConstraintSchema)store.GetValue (iter, colObjIndex);
				constraint.Columns.Clear ();
				
				foreach (ColumnSchema column in columnSelecter.CheckedColumns) {
					constraint.Columns.Add (column);
					if (first)
						first = false;
					else
						sb.Append (',');
					
					sb.Append (column.Name);
				}
				store.SetValue (iter, colColumnsIndex, sb.ToString ());
				EmitContentChanged ();
				OnPrimaryKeyChanged ();
			}
		}

		protected virtual void OnPrimaryKeyChanged ()
		{ 
			if (PrimaryKeyChanged != null)
				PrimaryKeyChanged (this, new EventArgs ());
		}
		
		protected virtual void AddClicked (object sender, System.EventArgs e)
		{
			PrimaryKeyConstraintSchema pk = 
				schemaProvider.CreatePrimaryKeyConstraintSchema (
				                                                 string.Concat (
				                                                                table.Name,
				                                                                "_", 
				                                                                AddinCatalog.GetString ("pk_new")));
			int index = 1;
			while (constraints.Contains (pk.Name))
				pk.Name = string.Concat (table.Name,"_", AddinCatalog.GetString ("pk_new"), (index++).ToString ()); 
			constraints.Add (pk);
			AddConstraint (pk);
			EmitContentChanged ();
		}

		protected virtual void RemoveClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (listPK.Selection.GetSelected (out iter)) {
				PrimaryKeyConstraintSchema pk = store.GetValue (iter, colObjIndex) as PrimaryKeyConstraintSchema;
				
				if (MessageService.Confirm (
					AddinCatalog.GetString ("Are you sure you want to remove constraint '{0}'?", pk.Name),
					AlertButton.Remove
				)) {
					store.Remove (ref iter);
					constraints.Remove (pk);
					EmitContentChanged ();
				}
			}
		}
				
		private void AddConstraint (PrimaryKeyConstraintSchema pk)
		{
			System.Text.StringBuilder pk_cols = new System.Text.StringBuilder ();
			foreach (ColumnSchema col in pk.Columns) {
				if (pk_cols.Length > 0)
					pk_cols.Append (",");
				pk_cols.Append (col.Name);
			}
			TreeIter iter = store.AppendValues (pk.Name, pk_cols.ToString (), pk);
		}
		
		public virtual bool ValidateSchemaObjects (out string msg)
		{ 
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					string name = store.GetValue (iter, colNameIndex) as string;
					string columns = store.GetValue (iter, colColumnsIndex) as string;
					
					if (String.IsNullOrEmpty (columns)) {
						msg = AddinCatalog.GetString ("Primary Key constraint '{0}' must be applied to one or more columns.", name);
						return false;
					}
				} while (store.IterNext (ref iter));
			}
			msg = null;
			return true;
		}
		
		public virtual void FillSchemaObjects ()
		{
			/*
			 * This code isn't needed anymore, beacause PK's are added on demand in
			 * order to mantain the exact same thing between "ColumnsEditorWiget" and PrimaryKeyConstraintEditorWidget
			 */
		}
		
		
		protected virtual void EmitContentChanged ()
		{
			if (ContentChanged != null)
				ContentChanged (this, EventArgs.Empty);
		}
	}
}
