// 
// SqlServerCreateDatabaseDialog.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2009 
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

using Gtk;
using System;
using MonoDevelop.Ide;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Designer;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Sql.SqlServer
{

	public class SqlServerCreateDatabaseDialog:CreateDatabaseDialog
	{
		ConnectionSettingsWidget connectionWidget;
		SqlServerCreateDatabaseWidget createDBWidget;
		
		public SqlServerCreateDatabaseDialog (IDbFactory factory):base(factory)
		{
			createDBWidget = new SqlServerCreateDatabaseWidget ();
			Notebook.AppendPage (createDBWidget, 
			                     new Label (AddinCatalog.GetString ("Database Properties")));
			Notebook.ShowTabs = true;
			Gtk.Notebook nb = Notebook;
			
			nb.SwitchPage += delegate(object o, SwitchPageArgs args) {
				if (nb.CurrentPage == 1)
					if (!connectionWidget.ValidateFields ())
					{
						nb.CurrentPage = 0;
						MessageService.ShowError (this, 
							AddinCatalog.GetString ("Set the connection properties before the database properties."));
					} else {
						Initialize (factory);
						if (DatabaseConnection.ConnectionPool.HasErrors) {
							MessageService.ShowError (DatabaseConnection.ConnectionPool.Error);
							nb.CurrentPage = 0;
							return;
						}					
						createDBWidget.Initialize ((SqlServerSchemaProvider)DatabaseConnection.SchemaProvider);
					}
			};
			
			Notebook.ShowAll ();
			
		}
		
		private void Initialize (IDbFactory factory)
		{
			if (DatabaseConnection != null)
				DatabaseConnection.ConnectionPool.Close ();
			DatabaseConnectionSettings settings = new DatabaseConnectionSettings(connectionWidget.ConnectionSettings);
			settings.Database = "master"; 
			// Create Context, Pool, Connection 
			DatabaseConnectionContext ctx = new DatabaseConnectionContext (settings, true);
			ctx.ConnectionPool.Initialize ();
			this.DatabaseConnection = ctx;
		}
		
		
		protected override ConnectionSettingsWidget CreateConnectionSettingsWidget (IDbFactory factory)
		{
			connectionWidget = new ConnectionSettingsWidget (factory);
			connectionWidget.ShowSettings (factory.GetDefaultConnectionSettings ());
			connectionWidget.EnableTestButton = false;
			return connectionWidget;
		}
	
		
		protected override void OnBeforeDatabaseCreation (DatabaseSchema schema)
		{
			createDBWidget.SetDatabaseOptions ((SqlServerDatabaseSchema)schema);
			base.OnBeforeDatabaseCreation (schema);
		}
		
		
	}
}
