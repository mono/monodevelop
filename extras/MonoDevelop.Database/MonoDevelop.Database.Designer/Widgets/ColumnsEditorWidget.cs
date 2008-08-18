//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;
using System;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ColumnsEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		
		private ListStore storeColumns;
		private ListStore storeTypes;
		
		private const int colPKIndex = 0;
		private const int colNameIndex = 1;
		private const int colTypeIndex = 2;
		private const int colLengthIndex = 3;
		private const int colNullableIndex = 4;
		private const int colCommentIndex = 5;
		private const int colObjIndex = 6;
		
		private ColumnSchemaCollection columns;
		private ConstraintSchemaCollection constraints;
		private DataTypeSchemaCollection dataTypes;
		private ISchemaProvider schemaProvider;
		private TableSchema table;
		
		private SchemaActions action;
		private ColumnEditorSettings settings;
		
		public ColumnsEditorWidget (ISchemaProvider schemaProvider, SchemaActions action, ColumnEditorSettings settings)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			if (settings == null)
				throw new ArgumentNullException ("settings");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
			this.settings = settings;

			this.Build();

			storeTypes = new ListStore (typeof (string), typeof (object));
			storeColumns = new ListStore (typeof (bool), typeof (string), typeof (string), typeof (string), typeof (bool), typeof (string), typeof (object));
			treeColumns.Model = storeColumns;
			treeColumns.Selection.Changed += new EventHandler (OnSelectionChanged);

			//TODO: cols for scale, precision, ... ?
			TreeViewColumn colPK = new TreeViewColumn ();
			TreeViewColumn colName = new TreeViewColumn ();
			TreeViewColumn colType = new TreeViewColumn ();
			TreeViewColumn colLength = new TreeViewColumn ();
			TreeViewColumn colNullable = new TreeViewColumn ();
			TreeViewColumn colComment = new TreeViewColumn ();
			
			colPK.Title = AddinCatalog.GetString ("PK");
			colName.Title = AddinCatalog.GetString ("Name");
			colType.Title = AddinCatalog.GetString ("Type");
			colLength.Title = AddinCatalog.GetString ("Length");
			colNullable.Title = AddinCatalog.GetString ("Nullable");
			colComment.Title = AddinCatalog.GetString ("Comment");
			
			colType.MinWidth = 120; //request a bigger width

			CellRendererToggle pkRenderer = new CellRendererToggle ();
			CellRendererText nameRenderer = new CellRendererText ();
			CellRendererCombo typeRenderer = new CellRendererCombo ();
			CellRendererText lengthRenderer = new CellRendererText ();
			CellRendererToggle nullableRenderer = new CellRendererToggle ();
			CellRendererText commentRenderer = new CellRendererText ();

			nameRenderer.Editable = true;
			nameRenderer.Edited += new EditedHandler (NameEdited);
			
			typeRenderer.Model = storeTypes;
			typeRenderer.TextColumn = 0;
			typeRenderer.Editable = true;
			typeRenderer.Edited += new EditedHandler (TypeEdited);
			
			lengthRenderer.Editable = true;
			lengthRenderer.Edited += new EditedHandler (LengthEdited);
			
			pkRenderer.Activatable = true;
			pkRenderer.Toggled += new ToggledHandler (PkToggled);
			
			nullableRenderer.Activatable = true;
			nullableRenderer.Toggled += new ToggledHandler (NullableToggled);
			
			commentRenderer.Editable = true;
			commentRenderer.Edited += new EditedHandler (CommentEdited);
			
			colPK.PackStart (pkRenderer, true);
			colName.PackStart (nameRenderer, true);
			colType.PackStart (typeRenderer, true);
			colLength.PackStart (lengthRenderer, true);
			colNullable.PackStart (nullableRenderer, true);
			colComment.PackStart (commentRenderer, true);

			colPK.AddAttribute (pkRenderer, "active", colPKIndex);
			colName.AddAttribute (nameRenderer, "text", colNameIndex);
			colType.AddAttribute (typeRenderer, "text", colTypeIndex);
			colLength.AddAttribute (lengthRenderer, "text", colLengthIndex);
			colNullable.AddAttribute (nullableRenderer, "active", colNullableIndex);
			colComment.AddAttribute (commentRenderer, "text", colCommentIndex);

			if (settings.ShowPrimaryKeyColumn)
				treeColumns.AppendColumn (colPK);
			if (settings.ShowNameColumn)
				treeColumns.AppendColumn (colName);
			if (settings.ShowTypeColumn)
				treeColumns.AppendColumn (colType);
			if (settings.ShowLengthColumn)
				treeColumns.AppendColumn (colLength);
			if (settings.ShowNullableColumn)
				treeColumns.AppendColumn (colNullable);
			if (settings.ShowCommentColumn)
				treeColumns.AppendColumn (colComment);

			treeColumns.Reorderable = false;
			treeColumns.HeadersClickable = false;
			treeColumns.HeadersVisible = true;
			//Gtk# 2.10:treeColumns.EnableGridLines = TreeViewGridLines.Both;
			treeColumns.EnableSearch = false;
			
			if (action == SchemaActions.Alter) {
				buttonAdd.Sensitive = settings.ShowAddButton;
				buttonRemove.Sensitive = settings.ShowRemoveButton;
				buttonUp.Sensitive = settings.AllowReorder;
			}

			ShowAll ();
		}
		
		public void Initialize (TableSchema table, ColumnSchemaCollection columns, ConstraintSchemaCollection constraints, DataTypeSchemaCollection dataTypes)
		{
			if (columns == null)
				throw new ArgumentNullException ("columns");
			if (constraints == null)
				throw new ArgumentNullException ("constraints");
			if (table == null)
				throw new ArgumentNullException ("table");
			if (dataTypes == null)
				throw new ArgumentNullException ("dataTypes");

			this.table = table;
			this.columns = columns;
			this.constraints = constraints;
			this.dataTypes = dataTypes;
			
			foreach (ColumnSchema column in columns)
				AppendColumnSchema (column);
			
			foreach (DataTypeSchema dataType in dataTypes)
				storeTypes.AppendValues (dataType.Name, storeTypes);
		}
		
		private void AppendColumnSchema (ColumnSchema column)
		{
			bool pk = column.Constraints.GetConstraint (ConstraintType.PrimaryKey) != null;
			storeColumns.AppendValues (pk, column.Name, column.DataType.Name, column.DataType.LengthRange.Default.ToString (), column.IsNullable, column.Comment, column);
		}

		protected virtual void AddClicked (object sender, EventArgs e)
		{
			int index = 1;
			string name = null;
			do {
				name = "column" + index;
				index++;
			} while (columns.Contains (name));
			
//			ColumnSchema column = schemaProvider.GetNewColumnSchema (name, table);
//
//			TreeIter iter;
//			if (storeTypes.GetIterFirst (out iter))
//				column.DataTypeName = storeTypes.GetValue (iter, 0) as string;
//			
//			columns.Add (column);
//			AppendColumnSchema (column);
//			EmitContentChanged ();
		}

		protected virtual void RemoveClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (treeColumns.Selection.GetSelected (out iter)) {
				ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;
				
				//TODO: also check for attached constraints
				
				bool result = MessageService.Confirm (
					AddinCatalog.GetString ("Are you sure you want to remove column '{0}'", column.Name),
					AlertButton.Remove
				);
				
				if (result) {
					storeColumns.Remove (ref iter);
					EmitContentChanged ();
				}
			}
		}
		
		private void PkToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (storeColumns.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) storeColumns.GetValue (iter, colPKIndex);
	 			storeColumns.SetValue (iter, colPKIndex, !val);
				EmitContentChanged ();
	 		}
		}
		
		private void NullableToggled (object sender, ToggledArgs args)
		{
	 		TreeIter iter;
			if (storeColumns.GetIterFromString (out iter, args.Path)) {
	 			bool val = (bool) storeColumns.GetValue (iter, colNullableIndex);
				ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;
	 			storeColumns.SetValue (iter, colNullableIndex, !val);
				column.IsNullable = !val;
				EmitContentChanged ();
	 		}
		}
		
		private void NameEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (storeColumns.GetIterFromString (out iter, args.Path)) {
				if (!string.IsNullOrEmpty (args.NewText)) {
					storeColumns.SetValue (iter, colNameIndex, args.NewText);
					ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;
					column.Name = args.NewText;
					EmitContentChanged ();
				} else {
					string oldText = storeColumns.GetValue (iter, colNameIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void TypeEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (storeColumns.GetIterFromString (out iter, args.Path)) {
				if (!string.IsNullOrEmpty (args.NewText)) {
					ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;
		
					int len = int.Parse (storeColumns.GetValue (iter, colLengthIndex) as string);
					if (column.DataType.LengthRange.Default == len) {
						//change the length if it is still the default length
						DataTypeSchema dtNew = schemaProvider.GetDataType (args.NewText);
						storeColumns.SetValue (iter, colLengthIndex, dtNew.LengthRange.Default.ToString ());
					}
					
					storeColumns.SetValue (iter, colTypeIndex, args.NewText);
					column.DataTypeName = args.NewText;
					EmitContentChanged ();
				} else {
					string oldText = storeColumns.GetValue (iter, colTypeIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void LengthEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (storeColumns.GetIterFromString (out iter, args.Path)) {
				int len;
				if (!string.IsNullOrEmpty (args.NewText) && int.TryParse (args.NewText, out len)) {
					storeColumns.SetValue (iter, colLengthIndex, args.NewText);
					ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;
					column.DataType.LengthRange.Default = int.Parse (args.NewText);
					EmitContentChanged ();
				} else {
					string oldText = storeColumns.GetValue (iter, colLengthIndex) as string;
					(sender as CellRendererText).Text = oldText;
				}
			}
		}
		
		private void CommentEdited (object sender, EditedArgs args)
		{
			TreeIter iter;
			if (storeColumns.GetIterFromString (out iter, args.Path)) {
				storeColumns.SetValue (iter, colCommentIndex, args.NewText);
				ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;
				column.Comment = args.NewText;
				EmitContentChanged ();
			}
		}

		protected virtual void DownClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (treeColumns.Selection.GetSelected (out iter)) {
				TreePath path = storeColumns.GetPath (iter);
				int x = path.Indices[0];
				columns.Swap (x, x + 1);
			}
		}

		protected virtual void UpClicked (object sender, EventArgs e)
		{
			TreeIter iter;
			if (treeColumns.Selection.GetSelected (out iter)) {
				TreePath path = storeColumns.GetPath (iter);
				int x = path.Indices[0];
				columns.Swap (x, x - 1);
			}
		}
		
		private void OnSelectionChanged (object sender, EventArgs e)
		{
			IDbFactory fac = schemaProvider.ConnectionPool.DbFactory;
			//TODO: check Append if "next" is the last row
			TreeIter iter;
			bool sel = settings.ShowRemoveButton;
			bool next = settings.AllowReorder;
			bool prev = next;
			
			if (treeColumns.Selection.GetSelected (out iter)) {
				TreePath path = storeColumns.GetPath (iter);
				int index = path.Indices[0];
				
				sel &= true;
				prev &= index > 0;
				next &= storeColumns.IterNext (ref iter);
			}
			
			buttonUp.Sensitive = prev;
			buttonDown.Sensitive = next;
			buttonRemove.Sensitive = sel;
		}
		
		protected virtual void EmitContentChanged ()
		{
			if (ContentChanged != null)
				ContentChanged (this, EventArgs.Empty);
		}
		
		public virtual bool ValidateSchemaObjects (out string msg)
		{ 
			TreeIter iter;
			if (storeColumns.GetIterFirst (out iter)) {
				bool isPk = constraints.GetConstraint (ConstraintType.PrimaryKey) != null;
				do {
					string name = storeColumns.GetValue (iter, colNameIndex) as string;
					string type = storeColumns.GetValue (iter, colTypeIndex) as string;
					int len = int.Parse (storeColumns.GetValue (iter, colLengthIndex) as string);
					if (!isPk)
						isPk = (bool)storeColumns.GetValue (iter, colPKIndex);
		
					DataTypeSchema dt = schemaProvider.GetDataType (type);
					if (dt == null) {
						msg = AddinCatalog.GetString ("Unknown data type '{0}' applied to column '{1}'.", type, name);
						return false;
					}
					
					//TODO: enable when all providers have good datatype info
//					if (!dt.LengthRange.IsInRange (len)) {
//						msg = AddinCatalog.GetString ("Invalid length for '{0}'.", name);
//						return false;
//					}
				} while (storeColumns.IterNext (ref iter));
				
				if (!isPk) {
					msg = AddinCatalog.GetString ("Table '{0}' must contain at least one primary key.", table.Name);
					return false;
				} else {
					msg = null;
					return true;
				}
			}
			msg = AddinCatalog.GetString ("Table '{0}' must contain at least 1 column.", table.Name);
			return false;
		}
		
		public virtual void FillSchemaObjects ()
		{
			TreeIter iter;
			if (storeColumns.GetIterFirst (out iter)) {
				do {
					ColumnSchema column = storeColumns.GetValue (iter, colObjIndex) as ColumnSchema;

					column.Name = storeColumns.GetValue (iter, colNameIndex) as string;
					column.DataTypeName = storeColumns.GetValue (iter, colTypeIndex) as string;
					column.DataType.LengthRange.Default = int.Parse (storeColumns.GetValue (iter, colLengthIndex) as string);
					column.IsNullable = (bool)storeColumns.GetValue (iter, colNullableIndex);
					column.Comment = storeColumns.GetValue (iter, colCommentIndex) as string;
					
					if ((bool)storeColumns.GetValue (iter, colPKIndex)) {
						PrimaryKeyConstraintSchema pk = schemaProvider.CreatePrimaryKeyConstraintSchema ("pk_" + column.Name);
						column.Constraints.Add (pk);
					}
				} while (storeColumns.IterNext (ref iter));
			}
		}
	}
	
	public class ColumnEditorSettings
	{
		private bool showAddButton = true;
		private bool showRemoveButton = true;
		private bool allowReorder = true;
		
		private bool showPrimaryKeyColumn = true;
		private bool showNameColumn = true;
		private bool showTypeColumn = true;
		private bool showLengthColumn = true;
		private bool showNullableColumn = true;
		private bool showCommentColumn = true;
		
		public bool ShowAddButton {
			get { return showAddButton; }
			set { showAddButton = value; }
		}
		
		public bool ShowRemoveButton {
			get { return showRemoveButton; }
			set { showRemoveButton = value; }
		}
		
		public bool AllowReorder {
			get { return allowReorder; }
			set { allowReorder = value; }
		}
		
		public bool ShowPrimaryKeyColumn {
			get { return showPrimaryKeyColumn; }
			set { showPrimaryKeyColumn = value; }
		}
		public bool ShowNameColumn {
			get { return showNameColumn; }
			set { showNameColumn = value; }
		}
		public bool ShowTypeColumn {
			get { return showTypeColumn; }
			set { showTypeColumn = value; }
		}
		public bool ShowLengthColumn {
			get { return showLengthColumn; }
			set { showLengthColumn = value; }
		}
		public bool ShowNullableColumn {
			get { return showNullableColumn; }
			set { showNullableColumn = value; }
		}

		public bool ShowCommentColumn {
			get { return showCommentColumn; }
			set { showCommentColumn = value; }
		}
	}
}
