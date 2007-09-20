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
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	public partial class TableEditorDialog : Gtk.Dialog
	{
		private SchemaActions action;
		
		private ISchemaProvider schemaProvider;
		private TableSchemaCollection tables;
		private TableSchema table;
		private TableSchema originalTable;
		private ColumnSchemaCollection columns;
		private ConstraintSchemaCollection constraints;
		private IndexSchemaCollection indexes;
		private TriggerSchemaCollection triggers;
		private DataTypeSchemaCollection dataTypes;

		private Notebook notebook;

		private ColumnsEditorWidget columnEditor;
		private ConstraintsEditorWidget constraintEditor;
		private IndicesEditorWidget indexEditor;
		private TriggersEditorWidget triggerEditor;
		private CommentEditorWidget commentEditor;
		
		public TableEditorDialog (ISchemaProvider schemaProvider, TableSchema table, bool create)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			if (table == null)
				throw new ArgumentNullException ("table");
			
			this.schemaProvider = schemaProvider;
			this.originalTable = table;
			this.table = table;
			this.action = create ? SchemaActions.Create : SchemaActions.Alter;
			
			this.Build();
			
			if (create)
				Title = GettextCatalog.GetString ("Create Table");
			else
				Title = GettextCatalog.GetString ("Alter Table");
			
			notebook = new Notebook ();
			vboxContent.PackStart (notebook, true, true, 0);
			
			columnEditor = new ColumnsEditorWidget (schemaProvider, action);
			columnEditor.ContentChanged += new EventHandler (OnContentChanged);
			notebook.AppendPage (columnEditor, new Label (GettextCatalog.GetString ("Columns")));
			
			//TODO: there is a diff between col and table constraints
			IDbFactory fac = schemaProvider.ConnectionPool.DbFactory;
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.Constraints)) {
				constraintEditor = new ConstraintsEditorWidget (schemaProvider, action);
				constraintEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (constraintEditor, new Label (GettextCatalog.GetString ("Constraints")));
			}

			//TODO:
			//indexEditor = new IndicesEditorWidget (schemaProvider);
			//notebook.AppendPage (indexEditor, new Label (GettextCatalog.GetString ("Indexes")));
			
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.Trigger)) {
				triggerEditor = new TriggersEditorWidget (schemaProvider, action);
				triggerEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (triggerEditor, new Label (GettextCatalog.GetString ("Triggers")));
			}
			
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.Comment)) {
				commentEditor = new CommentEditorWidget ();
				notebook.AppendPage (commentEditor, new Label (GettextCatalog.GetString ("Comment")));
			}

			notebook.Page = 0;

			entryName.Text = originalTable.Name;

			WaitDialog.ShowDialog ("Loading table data ...");

			notebook.Sensitive = false;
			ThreadPool.QueueUserWorkItem (new WaitCallback (InitializeThreaded));
			
			vboxContent.ShowAll ();
			SetWarning (null);
		}
		
		private void InitializeThreaded (object state)
		{
			tables = schemaProvider.GetTables ();
			dataTypes = schemaProvider.GetDataTypes ();
			columns = originalTable.Columns;
			constraints = originalTable.Constraints;
			triggers = originalTable.Triggers;
			//TODO: indices
			indexes = new IndexSchemaCollection ();
			
			Runtime.LoggingService.Error ("TABLE " + originalTable.Name);
			Runtime.LoggingService.Error ("   columns = " + columns.Count);
			Runtime.LoggingService.Error ("   constraints = " + constraints.Count);

			try {
			foreach (ColumnSchema col in columns) {				
				int dummy = col.Constraints.Count; //get column constraints
				Runtime.LoggingService.Error ("CONSTRAINTS " + col.Name + " " + dummy);
			}
			} catch (Exception ee) {
				Runtime.LoggingService.Error (ee);
				Runtime.LoggingService.Error (ee.StackTrace);
			}

			if (action == SchemaActions.Alter) //make a duplicate if we are going to alter the table
				this.table = originalTable.Clone () as TableSchema;

			DispatchService.GuiDispatch (delegate () {
				InitializeGui ();
			});
		}
		
		private void InitializeGui ()
		{
			notebook.Sensitive = true;
			WaitDialog.HideDialog ();
			
			Runtime.LoggingService.Error ("TED: InitializeGui");
			
			columnEditor.Initialize (table, columns, constraints, dataTypes);
			if (constraintEditor != null)
				constraintEditor.Initialize (tables, table, columns, constraints, dataTypes);
			if (triggerEditor != null)
				triggerEditor.Initialize (table, triggers);
			Runtime.LoggingService.Error ("TED: InitializeGui 2");
		}

		protected virtual void CancelClicked (object sender, System.EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Hide ();
		}

		protected virtual void OkClicked (object sender, System.EventArgs e)
		{
			columnEditor.FillSchemaObjects ();
			if (constraintEditor != null)
				constraintEditor.FillSchemaObjects ();
			if (triggerEditor != null)
				triggerEditor.FillSchemaObjects ();
			if (commentEditor != null)
				table.Comment = commentEditor.Comment;
			
			if (action == SchemaActions.Create)
				table.Definition = schemaProvider.GetTableCreateStatement (table);
			else
				table.Definition = schemaProvider.GetTableAlterStatement (table);

			if (checkPreview.Active) {
				PreviewDialog dlg = new PreviewDialog (table.Definition);
				if (dlg.Run () == (int)ResponseType.Ok) {
					table.Definition = dlg.Text;
					
					Respond (ResponseType.Ok);
					Hide ();
				}
				dlg.Destroy ();
			} else {
				Respond (ResponseType.Ok);
				Hide ();
			}
		}

		protected virtual void NameChanged (object sender, System.EventArgs e)
		{
			table.Name = entryName.Text;
		}
		
		protected virtual void OnContentChanged (object sender, EventArgs args)
		{
			string msg;
			
			bool val = entryName.Text.Length > 0;
			if (!val) {
				msg = GettextCatalog.GetString ("No name specified.");
				goto sens;
			}
			
			val &= columnEditor.ValidateSchemaObjects (out msg);
			if (!val) goto sens;
			
			if (constraintEditor != null) {
				val &= constraintEditor.ValidateSchemaObjects (out msg);
				if (!val) goto sens;
			}
			
			if (triggerEditor != null) {
				val &= triggerEditor.ValidateSchemaObjects (out msg);
				if (!val) goto sens;
			}
			
			//TODO: validate indexEditor
		 sens:
			SetWarning (msg);
			buttonOk.Sensitive = val;
		}
		
		protected virtual void SetWarning (string msg)
		{
			if (msg == null) {
				hboxWarning.Hide ();
				labelWarning.Text = "";
			} else {
				hboxWarning.ShowAll ();
				labelWarning.Text = msg;
			}
		}
	}
}
