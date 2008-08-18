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
	public partial class ConstraintsEditorWidget : Gtk.Bin
	{
		public event EventHandler ContentChanged;
		
		private Notebook notebook;
		
		private ISchemaProvider schemaProvider;
		
		private PrimaryKeyConstraintEditorWidget pkEditor;
		private ForeignKeyConstraintEditorWidget fkEditor;
		private CheckConstraintEditorWidget checkEditor;
		private UniqueConstraintEditorWidget uniqueEditor;
		
		private SchemaActions action;
		
		public ConstraintsEditorWidget (ISchemaProvider schemaProvider, SchemaActions action, ConstraintEditorSettings settings)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
	
			this.Build();
			
			notebook = new Notebook ();
			Add (notebook);

			if (settings.ShowPrimaryKeyConstraints) {
				//not for column constraints, since they are already editable in the column editor
				pkEditor = new PrimaryKeyConstraintEditorWidget (schemaProvider, action);
				pkEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (pkEditor, new Label (AddinCatalog.GetString ("Primary Key")));
			}
			
			if (settings.ShowForeignKeyConstraints) {
				fkEditor = new ForeignKeyConstraintEditorWidget (schemaProvider, action, settings.ForeignKeySettings);
				fkEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (fkEditor, new Label (AddinCatalog.GetString ("Foreign Key")));
			}
			
			if (settings.ShowCheckConstraints) {
				checkEditor = new CheckConstraintEditorWidget (schemaProvider, action, settings.CheckSettings);
				checkEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (checkEditor, new Label (AddinCatalog.GetString ("Check")));
			}
			
			if (settings.ShowUniqueConstraints) {
				uniqueEditor = new UniqueConstraintEditorWidget (schemaProvider, action);
				uniqueEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (uniqueEditor, new Label (AddinCatalog.GetString ("Unique")));
			}

			ShowAll ();
		}
		
		public void Initialize (TableSchemaCollection tables, TableSchema table, ColumnSchemaCollection columns, ConstraintSchemaCollection constraints, DataTypeSchemaCollection dataTypes)
		{
			if (columns == null)
				throw new ArgumentNullException ("columns");
			if (constraints == null)
				throw new ArgumentNullException ("constraints");
			if (table == null)
				throw new ArgumentNullException ("table");
			if (tables == null)
				throw new ArgumentNullException ("tables");

			if (pkEditor != null)
				pkEditor.Initialize (table, columns, constraints);
			if (fkEditor != null)
				fkEditor.Initialize (tables, table, columns, constraints);
			if (checkEditor != null)
				checkEditor.Initialize (table, columns, constraints);
			if (uniqueEditor != null)
				uniqueEditor.Initialize (table, columns, constraints);
		}
		
		private void OnContentChanged (object sender, EventArgs args)
		{
			if (ContentChanged != null)
				ContentChanged (this, args);
		}
		
		public virtual bool ValidateSchemaObjects (out string msg)
		{
			msg = null;
			bool ret = true;

			if (pkEditor != null)
				ret &= pkEditor.ValidateSchemaObjects (out msg);
			if (!ret) return ret;
			
			if (fkEditor != null)
				ret &= fkEditor.ValidateSchemaObjects (out msg);
			if (!ret) return ret;
			
			if (checkEditor != null)
				ret &= checkEditor.ValidateSchemaObjects (out msg);
			if (!ret) return ret;
			
			if (uniqueEditor != null)
				ret &= uniqueEditor.ValidateSchemaObjects (out msg);
			if (!ret) return ret;
			
			return ret;
		}
		
		public virtual void FillSchemaObjects ()
		{
			if (pkEditor != null)
				pkEditor.FillSchemaObjects ();
			if (fkEditor != null)
				fkEditor.FillSchemaObjects ();
			if (checkEditor != null)
				checkEditor.FillSchemaObjects ();
			if (uniqueEditor != null)
				uniqueEditor.FillSchemaObjects ();
		}
	}
	
	public class ConstraintEditorSettings
	{
		private bool showPrimaryKeyConstraints = true;
		private bool showForeignKeyConstraints = true;
		private bool showCheckConstraints = true;
		private bool showUniqueConstraints = true;
		
		private ForeignKeyConstraintEditorSettings foreignKeySettings;
		private CheckConstraintEditorSettings checkSettings;
		
		public ConstraintEditorSettings ()
		{
			foreignKeySettings = new ForeignKeyConstraintEditorSettings ();
			checkSettings = new CheckConstraintEditorSettings ();
		}
		
		public ForeignKeyConstraintEditorSettings ForeignKeySettings {
			get { return foreignKeySettings; }
		}
		
		public CheckConstraintEditorSettings CheckSettings {
			get { return checkSettings; }
		}

		public bool ShowPrimaryKeyConstraints {
			get { return showPrimaryKeyConstraints; }
			set { showPrimaryKeyConstraints = value; }
		}
		
		public bool ShowForeignKeyConstraints {
			get { return showForeignKeyConstraints; }
			set { showForeignKeyConstraints = value; }
		}

		public bool ShowCheckConstraints {
			get { return showCheckConstraints; }
			set { showCheckConstraints = value; }
		}

		public bool ShowUniqueConstraints {
			get { return showUniqueConstraints; }
			set { showUniqueConstraints = value; }
		}
	}
}
