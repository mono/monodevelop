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
		
		private IEditSchemaProvider schemaProvider;
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
		TableEditorSettings settings;
		
		public TableEditorDialog (IEditSchemaProvider schemaProvider, bool create, TableEditorSettings settings)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.action = create ? SchemaActions.Create : SchemaActions.Alter;
			this.settings = settings;
			
			this.Build();
			
			if (create)
				Title = AddinCatalog.GetString ("Create Table");
			else
				Title = AddinCatalog.GetString ("Alter Table");
			
			notebook = new Notebook ();
			vboxContent.PackStart (notebook, true, true, 0);

			notebook.Sensitive = false;
			ThreadPool.QueueUserWorkItem (new WaitCallback (InitializeThreaded));
			vboxContent.ShowAll ();
		}
		
		public void Initialize (TableSchema table)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			
			this.originalTable = table;
			this.table = table;
			
			SetWarning (null);
			columnEditor = new ColumnsEditorWidget (schemaProvider, action, settings.ColumnSettings);
			columnEditor.ContentChanged += new EventHandler (OnContentChanged);
			// When primary Key are selected on the "Column Editor", it has to refresh the "Primary Key" Widget.
			columnEditor.PrimaryKeyChanged += delegate(object sender, EventArgs e) {
				if (constraintEditor != null)
					constraintEditor.RefreshConstraints ();
			};
			
			notebook.AppendPage (columnEditor, new Label (AddinCatalog.GetString ("Columns")));
			
			if (settings.ShowConstraints) {
				constraintEditor = new ConstraintsEditorWidget (schemaProvider, action, settings.ConstraintSettings);
				constraintEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (constraintEditor, new Label (AddinCatalog.GetString ("Constraints")));
				// If Primary Key are changed on it has to refresh the "Column Editor" Widget to select the correct 
				// columns
				constraintEditor.PrimaryKeyChanged += delegate(object sender, EventArgs e) {
					columnEditor.RefreshConstraints ();
				};
			}

			//TODO: Implement Index
			/*
			if (settings.ShowIndices) {
				indexEditor = new IndicesEditorWidget (schemaProvider, action);
				indexEditor.ContentChanged += OnContentChanged;
				notebook.AppendPage (indexEditor, new Label (AddinCatalog.GetString ("Indexes")));
			}
			*/
						
			if (settings.ShowTriggers) {
				triggerEditor = new TriggersEditorWidget (schemaProvider, action);
				triggerEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (triggerEditor, new Label (AddinCatalog.GetString ("Triggers")));
			}
			
			if (settings.ShowComment) {
				commentEditor = new CommentEditorWidget ();
				notebook.AppendPage (commentEditor, new Label (AddinCatalog.GetString ("Comment")));
			}
			notebook.Page = 0;

			entryName.Text = originalTable.Name;

			WaitDialog.ShowDialog ("Loading table data ...");
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
			
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();
			builder.Append ("Loading editor for TABLE ");
			builder.Append (originalTable.Name);
			builder.AppendLine ();
			builder.Append ("    columns = ");
			builder.Append (columns.Count);
			builder.AppendLine ();
			builder.Append ("constraints = ");
			builder.Append (constraints.Count);
			builder.AppendLine ();

			try {
				foreach (ColumnSchema col in columns) {				
					int dummy = col.Constraints.Count; //get column constraints
					builder.Append ("CONSTRAINTS ");
					builder.Append (col.Name);
					builder.Append (" ");
					builder.Append (dummy);
					builder.AppendLine ();
				}
				LoggingService.LogDebug (builder.ToString ());
			} catch (Exception ee) {
				LoggingService.LogDebug (builder.ToString ());
				LoggingService.LogError (ee.ToString ());
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
			
			LoggingService.LogDebug ("TableEditorDialog: entering InitializeGui");
			columnEditor.Initialize (table, columns, constraints, dataTypes);
			
			if (constraintEditor != null)
				constraintEditor.Initialize (tables, table, columns, constraints, dataTypes);
			if (triggerEditor != null)
				triggerEditor.Initialize (table, triggers);
			LoggingService.LogDebug ("TableEditorDialog: leaving InitializeGui");
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
//			else
//				table.Definition = schemaProvider.GetTableAlterStatement (table);

			if (checkPreview.Active) {
				// Preview Dialog: If it's canceled the response to the previous dialog should be None to know that it
				// isn't OK and don't close the table editor dialog.
				PreviewDialog dlg = new PreviewDialog (table.Definition);
				if (dlg.Run () == (int)ResponseType.Ok) {
					table.Definition = dlg.Text;
					Respond (ResponseType.Ok);
					Hide ();
				} else
					 Respond (ResponseType.None);
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
				msg = AddinCatalog.GetString ("No name specified.");
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
	
	public class TableEditorSettings
	{
		private bool showConstraints = true;
		private bool showComment = false;
		private bool showTriggers = true;
		private bool showIndices = true;
		
		private ConstraintEditorSettings constraintSettings;
		private ColumnEditorSettings columnSettings;
		
		public TableEditorSettings ()
		{
			constraintSettings = new ConstraintEditorSettings ();
			columnSettings = new ColumnEditorSettings ();
		}
		
		public ConstraintEditorSettings ConstraintSettings {
			get { return constraintSettings; }
		}
		
		public ColumnEditorSettings ColumnSettings {
			get { return columnSettings; }
		}
		
		public bool ShowConstraints {
			get {
				return constraintSettings.ShowPrimaryKeyConstraints |
					constraintSettings.ShowForeignKeyConstraints |
					constraintSettings.ShowCheckConstraints |
					constraintSettings.ShowUniqueConstraints;
			}
		}

		public bool ShowComment {
			get { return showComment; }
			set { showComment = value; }
		}

		public bool ShowTriggers {
			get { return showTriggers; }
			set { showTriggers = value; }
		}

		public bool ShowIndices {
			get { return showIndices; }
			set { showIndices = value; }
		}
	}
}
