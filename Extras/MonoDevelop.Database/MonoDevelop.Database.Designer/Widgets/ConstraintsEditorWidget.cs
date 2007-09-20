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
		
		public ConstraintsEditorWidget (ISchemaProvider schemaProvider, SchemaActions action)
		{
			if (schemaProvider == null)
				throw new ArgumentNullException ("schemaProvider");
			
			this.schemaProvider = schemaProvider;
			this.action = action;
			
			//TODO: enable/disable features based on schema provider metadata
			
			this.Build();
			
			notebook = new Notebook ();
			Add (notebook);
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

			IDbFactory fac = schemaProvider.ConnectionPool.DbFactory;
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.PrimaryKeyConstraint)) {
				//not for column constraints, since they are already editable in the column editor
				pkEditor = new PrimaryKeyConstraintEditorWidget (schemaProvider, action, table, columns, constraints);
				pkEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (pkEditor, new Label (GettextCatalog.GetString ("Primary Key")));
			}
			
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.ForeignKeyConstraint)
				|| fac.IsCapabilitySupported ("TableColumn", action, TableCapabilities.ForeignKeyConstraint)) {
				fkEditor = new ForeignKeyConstraintEditorWidget (schemaProvider, action, tables, table, columns, constraints);
				fkEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (fkEditor, new Label (GettextCatalog.GetString ("Foreign Key")));
			}
			
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.CheckConstraint)
				|| fac.IsCapabilitySupported ("TableColumn", action, TableCapabilities.CheckConstraint)) {
				checkEditor = new CheckConstraintEditorWidget (schemaProvider, action, table, columns, constraints);
				checkEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (checkEditor, new Label (GettextCatalog.GetString ("Check")));
			}
			
			if (fac.IsCapabilitySupported ("Table", action, TableCapabilities.UniqueConstraint)
				|| fac.IsCapabilitySupported ("TableColumn", action, TableCapabilities.CheckConstraint)) {
				uniqueEditor = new UniqueConstraintEditorWidget (schemaProvider, action, table, columns, constraints);
				uniqueEditor.ContentChanged += new EventHandler (OnContentChanged);
				notebook.AppendPage (uniqueEditor, new Label (GettextCatalog.GetString ("Unique")));
			}

			ShowAll ();
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
}
