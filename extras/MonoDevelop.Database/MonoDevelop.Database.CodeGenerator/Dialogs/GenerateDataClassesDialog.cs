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
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

namespace MonoDevelop.Database.CodeGenerator
{
	[System.ComponentModel.Category("MonoDevelop.Database.CodeGenerator")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class GenerateDataClassesDialog : Gtk.Dialog
	{
		private DatabaseConnectionContextComboBox comboDatabase;
		private ProjectDirectoryComboBox comboLocation;
		
		private ColumnMappingWidget columnMapping;
		//private TableMappingWidget tableMapping;
		
		private DatabaseConnectionContext selectedDatabase;
		
		private TableSchemaCollection tables;
		
		public GenerateDataClassesDialog ()
		{
			this.Build();
			
			notebook.ChangeCurrentPage += ChangeCurrentNotebookPage;
			
			comboLocation = new ProjectDirectoryComboBox ();
			comboDatabase = new DatabaseConnectionContextComboBox ();
			
			tableGeneral.Attach (comboLocation, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
			tableGeneral.Attach (comboDatabase, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Shrink, 0, 0);
			
			columnMapping = new ColumnMappingWidget (true);
			//tableMapping = new TableMappingWidget ();
			
			tableTables.Attach (columnMapping, 0, 1, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
			//tableTables.Attach (tableMapping, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
			
			ShowAll ();
		}
		
		private void ChangeCurrentNotebookPage (object o, ChangeCurrentPageArgs args)
		{
			if (notebook.Page == 1) {
				//we are switching to the "classes" page, refresh the content if the selected database changed
				DatabaseConnectionContext db = comboDatabase.DatabaseConnection;
				if (db != selectedDatabase) {
					selectedDatabase = db;
					FillClassesPage ();
				}
			}
		}
		
		private void FillClassesPage ()
		{
			//TODO: visually show that there is a BG thread retrieving the DB info
			QueryService.EnsureConnection (selectedDatabase, new DatabaseConnectionContextCallback (ExecuteQueryCallback), null);
		}
		
		private void ExecuteQueryCallback (DatabaseConnectionContext context, bool connected, object state)
		{
			if (!connected) {
				MessageService.ShowError (
					AddinCatalog.GetString ("Unable to connect to database '{0}'"), context.ConnectionSettings.Name);
				return;
			}
			
			ISchemaProvider provider = context.SchemaProvider;
			tables = provider.GetTables ();

			DispatchService.GuiDispatch (delegate () {
				//TODO: initialize the table mapper
			});
		}
	}
}
