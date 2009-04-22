


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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ForeignKeyConstraintEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		
		private ISchemaProvider schemaProvider;
		private TableSchema table;
		private TableSchemaCollection tables;
		private ColumnSchemaCollection columns;
		private ConstraintSchemaCollection constraints;
		TableSchema refTable = null;
		
		private const int colNameIndex = 0;
		private const int colReferenceTableIndex = 1;
		private const int colIsColumnConstraintIndex = 2;
		private const int colColumnsIndex = 3;
		private const int colReferenceColumnsIndex = 4;
		private const int colDeleteActionIndex = 5;
		private const int colUpdateActionIndex = 6;
		private const int colObjIndex = 7;
		
		private ListStore store;
		private ListStore storeActions;
		private ListStore storeTables;
		
		private SchemaActions action;
		private ForeignKeyConstraintEditorSettings settings;

		//TODO: difference between columns and reference columns + combo events
		public ForeignKeyConstraintEditorWidget (ISchemaProvider schemaProvider, SchemaActions action, ForeignKeyConstraintEditorSettings settings)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			if (settings == null)
				throw new ArgumentNullException ("settings");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
			this.settings = settings;

			this.Build();

			store = new ListStore (typeof (string), typeof (string), typeof (bool), typeof (string), typeof (string), typeof (string), typeof (string), typeof (object));
			listFK.Model = store;
			
			storeActions = new ListStore (typeof (string), typeof (int));
			storeTables = new ListStore (typeof (string), typeof(TableSchema));
			
			if (settings.SupportsCascade)
				storeActions.AppendValues ("Cascade", (int)ForeignKeyAction.Cascade);
			if (settings.SupportsRestrict)
				storeActions.AppendValues ("Restrict", (int)ForeignKeyAction.Restrict);
			if (settings.SupportsNoAction)
				storeActions.AppendValues ("No Action", (int)ForeignKeyAction.NoAction);
			if (settings.SupportsSetNull)
				storeActions.AppendValues ("Set Null", (int)ForeignKeyAction.SetNull);
			if (settings.SupportsSetDefault)
				storeActions.AppendValues ("Set Default", (int)ForeignKeyAction.SetDefault);

			TreeViewColumn colName = new TreeViewColumn ();
			TreeViewColumn colRefTable = new TreeViewColumn ();
			TreeViewColumn colIsColumnConstraint = new TreeViewColumn ();
			TreeViewColumn colDeleteAction = new TreeViewColumn ();
			TreeViewColumn colUpdateAction = new TreeViewColumn ();
			
			colName.Title = AddinCatalog.GetString ("Name");
			colRefTable.Title = AddinCatalog.GetString ("Reference Table");
			colIsColumnConstraint.Title = AddinCatalog.GetString ("Column Constraint");
			colDeleteAction.Title = AddinCatalog.GetString ("Delete Action");
			colUpdateAction.Title = AddinCatalog.GetString ("Update Action");
			
			colRefTable.MinWidth = 120;
			
			CellRendererText nameRenderer = new CellRendererText ();
			CellRendererCombo refTableRenderer = new CellRendererCombo ();
			CellRendererToggle isColumnConstraintRenderer = new CellRendererToggle ();
			CellRendererCombo deleteActionRenderer = new CellRendererCombo ();
			CellRendererCombo updateActionRenderer = new CellRendererCombo ();
			
			nameRenderer.Editable = true;
			nameRenderer.Edited += new EditedHandler (NameEdited);
			
			refTableRenderer.Model = storeTables;
			refTableRenderer.TextColumn = 0;
			refTableRenderer.Editable = true;
			refTableRenderer.Edited += new EditedHandler (RefTableEdited);
			
			deleteActionRenderer.Model = storeActions;
			deleteActionRenderer.TextColumn = 0;
			deleteActionRenderer.Editable = true;
			deleteActionRenderer.Edited += new EditedHandler (DeleteActionEdited);
			
			updateActionRenderer.Model = storeActions;
			updateActionRenderer.TextColumn = 0;
			updateActionRenderer.Editable = true;
			updateActionRenderer.Edited += new EditedHandler (UpdateActionEdited);

			colName.PackStart (nameRenderer, true);
			colRefTable.PackStart (refTableRenderer, true);
			colIsColumnConstraint.PackStart (isColumnConstraintRenderer, true);
			colDeleteAction.PackStart (deleteActionRenderer, true);
			colUpdateAction.PackStart (updateActionRenderer, true);

			colName.AddAttribute (nameRenderer, "text", colNameIndex);
			colRefTable.AddAttribute (refTableRenderer, "text", colReferenceTableIndex);
			colIsColumnConstraint.AddAttribute (isColumnConstraintRenderer, "active", colIsColumnConstraintIndex);
			colDeleteAction.AddAttribute (deleteActionRenderer, "text", colDeleteActionIndex);			
			colUpdateAction.AddAttribute (updateActionRenderer, "text", colUpdateActionIndex);
			
			colIsColumnConstraint.Visible = false;
			listFK.AppendColumn (colName);
			listFK.AppendColumn (colRefTable);
			listFK.AppendColumn (colIsColumnConstraint);
			listFK.AppendColumn (colDeleteAction);
			listFK.AppendColumn (colUpdateAction);
			
			columnSelecter.ColumnToggled += new EventHandler (ColumnToggled);
			referenceColumnSelecter.ColumnToggled += new EventHandler (ReferenceColumnToggled);
			listFK.Selection.Changed += new EventHandler (SelectionChanged);
			
			ShowAll ();
		}
		
		public void Initialize (TableSchemaCollection tables, TableSchema table, ColumnSchemaCollection columns, ConstraintSchemaCollection constraints)
		{
			if (columns == null)
				throw new ArgumentNullException ("columns");
			if (table == null)
				throw new ArgumentNullException ("table");
			if (constraints == null)
				throw new ArgumentNullException ("constraints");
			if (tables == null)
				throw new ArgumentNullException ("tables");
			
			this.table = table;
			this.tables = tables;
			this.columns = columns;
			this.constraints = constraints;
			columnSelecter.Initialize (columns);
			foreach (TableSchema tbl in tables)
				if (tbl.Name != table.Name)
					storeTables.AppendValues (tbl.Name, tbl);
		}
		
		protected virtual void AddClicked (object sender, EventArgs e)
		{
			ForeignKeyConstraintSchema fk = schemaProvider.CreateForeignKeyConstraintSchema (string.Concat (table.Name, 
			                                                                                                "_",
			                                                                                                "fk_new"));
			int index = 1;
			
			while (constraints.Contains (fk.Name))
				fk.Name = string.Concat (table.Name, "_", "fk_new", (index++).ToString ()); 
			
			constraints.Add (fk);
			AddConstraint (fk);
			EmitContentChanged ();
		}

		protected virtual void RemoveClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (listFK.Selection.GetSelected (out iter)) {
				ForeignKeyConstraintSchema fk = store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema;
				
				if (MessageService.Confirm (
					AddinCatalog.GetString ("Are you sure you want to remove foreign key constraint '{0}'?", fk.Name),
					AlertButton.Remove
				)) {
					store.Remove (ref iter);
					constraints.Remove (fk);
					EmitContentChanged ();
				}
			}
		}
		
		private void SelectionChanged (object sender, EventArgs args)
		{
			columnSelecter.DeselectAll ();
			referenceColumnSelecter.Clear ();
			
			TreeIter iter;
			if (listFK.Selection.GetSelected (out iter)) {
				FillReferenceColumnSelector (iter, store.GetValue (iter, colReferenceTableIndex).ToString ());

				buttonRemove.Sensitive = true;
				columnSelecter.Sensitive = true;
				SetSelectionFromIter (iter);
			} else {
				buttonRemove.Sensitive = false;
				columnSelecter.Sensitive = false;
			}
		}
		
		private void SetSelectionFromIter (TreeIter iter)
		{
			bool iscolc = (bool)store.GetValue (iter, colIsColumnConstraintIndex);
			columnSelecter.SingleCheck = iscolc;
			
			string colstr = store.GetValue (iter, colColumnsIndex) as string;
			if (colstr != String.Empty) {
				string[] cols = colstr.Split (',');
				foreach (string col in cols)
					columnSelecter.Select (col);
			}
			
			colstr = store.GetValue (iter, colReferenceColumnsIndex) as string;
			if (colstr != string.Empty) {
				string[] cols = colstr.Split (',');
				foreach (string col in cols)
					referenceColumnSelecter.Select (col);
			}
		}
		
		private void RefTableEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				FillReferenceColumnSelector (iter, args.NewText);
			}
		}
		
		private void FillReferenceColumnSelector (TreeIter iter, string table)
		{
			if (tables.Contains (table)) {
				refTable = tables.Search (table);
				if (refTable != null) {
					referenceColumnSelecter.Initialize (refTable.Columns);
					referenceColumnSelecter.Sensitive = true;
					store.SetValue (iter, colReferenceTableIndex, table);
					SetSelectionFromIter (iter);
				} else {
					referenceColumnSelecter.Sensitive = false;
					referenceColumnSelecter.Clear ();
				}
				EmitContentChanged ();
				
			} 
		}
		
		private void ColumnToggled (object sender, EventArgs args)
		{
			TreeIter iter;
			if (listFK.Selection.GetSelected (out iter)) {
				(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).Columns.Clear ();
				foreach (ColumnSchema col in columnSelecter.CheckedColumns)
					(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).Columns.Add (col);
				store.SetValue (iter, colColumnsIndex, GetColumnsString (columnSelecter.CheckedColumns));
				EmitContentChanged ();
			}
		}
		
		private void ReferenceColumnToggled (object sender, EventArgs args)
		{
			TreeIter iter;
			if (listFK.Selection.GetSelected (out iter)) {
				if (refTable != null) {
					(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).ReferenceTable = refTable;
					(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).ReferenceColumns.Clear ();
					foreach (ColumnSchema col in referenceColumnSelecter.CheckedColumns)
						(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).ReferenceColumns.Add (col);
					store.SetValue (iter, colReferenceColumnsIndex, GetColumnsString (referenceColumnSelecter.CheckedColumns));
					EmitContentChanged ();
				}
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
		
		private void UpdateActionEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				if (IsValidForeignKeyAction (args.NewText)) {
					store.SetValue (iter, colUpdateActionIndex, args.NewText);
					(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).UpdateAction = 
						GetForeignKeyAction (iter, colUpdateActionIndex);
					
					EmitContentChanged ();
				} else {
					string oldText = store.GetValue (iter, colUpdateActionIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void DeleteActionEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
				if (IsValidForeignKeyAction (args.NewText)) {
					store.SetValue (iter, colDeleteActionIndex, args.NewText);
					(store.GetValue (iter, colObjIndex) as ForeignKeyConstraintSchema).DeleteAction = 
						GetForeignKeyAction (iter, colDeleteActionIndex);
					EmitContentChanged ();
				} else {
					string oldText = store.GetValue (iter, colDeleteActionIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private bool IsValidForeignKeyAction (string name)
		{
			foreach (string item in Enum.GetNames (typeof (ForeignKeyAction))) {
				if (item == name.Replace (" ", ""))
					return true;
			}
			return false;
		}
		
		private void AddConstraint (ForeignKeyConstraintSchema fk)
		{
			store.AppendValues (fk.Name, String.Empty, false, String.Empty, String.Empty,
				fk.DeleteAction.ToString (), fk.UpdateAction.ToString (), fk
			);
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
					string columns = store.GetValue (iter, colColumnsIndex) as string;
					
					if (String.IsNullOrEmpty (columns)) {
						msg = AddinCatalog.GetString ("Unique Key constraint '{0}' must be applied to one or more columns.", name);
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
			 * This code isn't needed anymore, beacause Foreign Key's constraint are added on demand when clicking 
			 * Add Button.
			 */
		}
		
		private string GetColumnsString (IEnumerable<ColumnSchema> collection)
		{
			bool first = true;
			StringBuilder sb = new StringBuilder ();
			foreach (ColumnSchema column in collection) {
				if (first)
					first = false;
				else
					sb.Append (',');
				
				sb.Append (column.Name);
			}
			return sb.ToString ();
		}
		
		private ForeignKeyAction GetForeignKeyAction (TreeIter colIter, int colIndex)
		{
			string name = store.GetValue (colIter, colIndex) as string;

			TreeIter iter;
			if (storeActions.GetIterFirst (out iter)) {
				do {
					string actionName = storeActions.GetValue (iter, 0) as string;
					if (actionName == name)
						return (ForeignKeyAction)storeActions.GetValue (iter, 1);
				} while (storeActions.IterNext (ref iter));
			}
			return ForeignKeyAction.None;
		}
	}
	
	public class ForeignKeyConstraintEditorSettings
	{
		private bool supportsCascade = true;
		private bool supportsRestrict = true;
		private bool supportsNoAction = true;
		private bool supportsSetNull = true;
		private bool supportsSetDefault = true;

		public bool SupportsCascade {
			get { return supportsCascade; }
			set { supportsCascade = value; }
		}
		
		public bool SupportsRestrict {
			get { return supportsRestrict; }
			set { supportsRestrict = value; }
		}
		
		public bool SupportsNoAction {
			get { return supportsNoAction; }
			set { supportsNoAction = value; }
		}
		
		public bool SupportsSetNull {
			get { return supportsSetNull; }
			set { supportsSetNull = value; }
		}
		
		public bool SupportsSetDefault {
			get { return supportsSetDefault; }
			set { supportsSetDefault = value; }
		}
	}
}
