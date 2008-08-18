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
	public partial class UniqueConstraintEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		
		private ISchemaProvider schemaProvider;
		private ColumnSchemaCollection columns;
		private ConstraintSchemaCollection constraints;
		private TableSchema table;
		
		private SchemaActions action;
		
		private ListStore store;
		
		private const int colNameIndex = 0;
		private const int colIsColumnConstraintIndex = 1;
		private const int colColumnsIndex = 2;
		private const int colObjIndex = 3;
		
		public UniqueConstraintEditorWidget (ISchemaProvider schemaProvider, SchemaActions action)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
			
			this.Build();
			
			store = new ListStore (typeof (string), typeof (bool), typeof (string), typeof (object));
			listUnique.Model = store;
			listUnique.Selection.Changed += new EventHandler (SelectionChanged);
			columnSelecter.ColumnToggled += new EventHandler (ColumnToggled);
			
			TreeViewColumn colName = new TreeViewColumn ();
			TreeViewColumn colIsColConstraint = new TreeViewColumn ();

			colName.Title = AddinCatalog.GetString ("Name");
			colIsColConstraint.Title = AddinCatalog.GetString ("Column Constraint");
			
			CellRendererText nameRenderer = new CellRendererText ();
			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			
			nameRenderer.Editable = true;
			nameRenderer.Edited += new EditedHandler (NameEdited);
			
			toggleRenderer.Activatable = true;
			toggleRenderer.Toggled += new ToggledHandler (IsColumnConstraintToggled);
			
			colName.PackStart (nameRenderer, true);
			colIsColConstraint.PackStart (toggleRenderer, true);
			
			colName.AddAttribute (nameRenderer, "text", colNameIndex);
			colIsColConstraint.AddAttribute (toggleRenderer, "active", colIsColumnConstraintIndex);

			listUnique.AppendColumn (colName);
			listUnique.AppendColumn (colIsColConstraint);
			
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
			
			foreach (UniqueConstraintSchema uni in constraints.GetConstraints (ConstraintType.Unique))
				AddConstraint (uni);
			//TODO: also col constraints
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
		
		private void IsColumnConstraintToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (store.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) store.GetValue (iter, colIsColumnConstraintIndex);
	 			store.SetValue (iter, colIsColumnConstraintIndex, !val);
				SetSelectionFromIter (iter);
				EmitContentChanged ();
	 		}
		}
		
		private void SelectionChanged (object sender, EventArgs args)
		{
			columnSelecter.DeselectAll ();
			
			TreeIter iter;
			if (listUnique.Selection.GetSelected (out iter)) {
				columnSelecter.Sensitive = true;
				SetSelectionFromIter (iter);
			} else {
				columnSelecter.Sensitive = false;
			}
		}
		
		private void SetSelectionFromIter (TreeIter iter)
		{
			bool iscolc = (bool)store.GetValue (iter, colIsColumnConstraintIndex);
			columnSelecter.SingleCheck = iscolc;
			
			string colstr = store.GetValue (iter, colColumnsIndex) as string;
			string[] cols = colstr.Split (',');
			foreach (string col in cols)
				columnSelecter.Select (col);
		}
		
		private void ColumnToggled (object sender, EventArgs args)
		{
			TreeIter iter;
			if (listUnique.Selection.GetSelected (out iter)) {
				store.SetValue (iter, colColumnsIndex, GetColumnsString (columnSelecter.CheckedColumns));
				EmitContentChanged ();
			}
		}
		
		protected virtual void AddClicked (object sender, EventArgs e)
		{
			UniqueConstraintSchema uni = schemaProvider.CreateUniqueConstraintSchema ("uni_new");
			int index = 1;
			while (constraints.Contains (uni.Name))
				uni.Name = "uni_new" + (index++);
			constraints.Add (uni);
			AddConstraint (uni);
			EmitContentChanged ();
		}

		protected virtual void RemoveClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (listUnique.Selection.GetSelected (out iter)) {
				UniqueConstraintSchema uni = store.GetValue (iter, colObjIndex) as UniqueConstraintSchema;
				
				if (MessageService.Confirm (
					AddinCatalog.GetString ("Are you sure you want to remove constraint '{0}'?", uni.Name),
					AlertButton.Remove
				)) {
					store.Remove (ref iter);
					constraints.Remove (uni);
					EmitContentChanged ();
				}
			}
		}
		
		private void AddConstraint (UniqueConstraintSchema uni)
		{
			string colstr = GetColumnsString (uni.Columns);
			store.AppendValues (uni.Name, uni.IsColumnConstraint, colstr, uni);
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
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					UniqueConstraintSchema uni = store.GetValue (iter, colObjIndex) as UniqueConstraintSchema;

					uni.Name = store.GetValue (iter, colNameIndex) as string;
					uni.IsColumnConstraint = (bool)store.GetValue (iter, colIsColumnConstraintIndex);
					
					string colstr = store.GetValue (iter, colColumnsIndex) as string;
					string[] cols = colstr.Split (',');
					foreach (string col in cols) {
						ColumnSchema column = columns.Search (col);
						uni.Columns.Add (column);
					}
					
					table.Constraints.Add (uni);
				} while (store.IterNext (ref iter));
			}
		}
		
		protected virtual void EmitContentChanged ()
		{
			if (ContentChanged != null)
				ContentChanged (this, EventArgs.Empty);
		}
	}
}
