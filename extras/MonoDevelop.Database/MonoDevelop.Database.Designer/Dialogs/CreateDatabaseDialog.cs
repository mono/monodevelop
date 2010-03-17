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
using MonoDevelop.Ide;
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	public partial class CreateDatabaseDialog : Gtk.Dialog
	{
		private DatabaseConnectionContext context;
		
		private ConnectionSettingsWidget settingsWidget;
		
		public CreateDatabaseDialog (IDbFactory factory)
		{
			this.Build();
			
			settingsWidget = CreateConnectionSettingsWidget (factory);
			vboxConnection.PackStart (settingsWidget, true, true, 0);
			
			settingsWidget.NeedsValidation += Validate;
			settingsWidget.EnableRefreshButton = false;
			
			ShowAll ();
		}
		
		public DatabaseConnectionContext DatabaseConnection {
			get { return context; }
			set { context = value; }
			
		}
		
		protected internal Notebook Notebook {
			get { return notebook; }
		}
		
		protected virtual ConnectionSettingsWidget CreateConnectionSettingsWidget (IDbFactory factory)
		{
			return new ConnectionSettingsWidget (factory);
		}
		
		protected virtual void OkClicked (object sender, EventArgs e)
		{
			if (context.IsTemporary) {
				try {
					//make it a real connection context and fill in the database
					IConnectionPool pool = DbFactoryService.CreateConnectionPool (DatabaseConnection);
					pool.Initialize ();
					ISchemaProvider provider = DbFactoryService.CreateSchemaProvider (DatabaseConnection, 
					                                                                               pool);
					
					DatabaseSchema db = provider.CreateDatabaseSchema (settingsWidget.ConnectionSettings.Database);
					OnBeforeDatabaseCreation (db);
					((AbstractEditSchemaProvider)provider).CreateDatabase (db);
					
					context.ConnectionSettings.Database = settingsWidget.ConnectionSettings.Database;
					context.ConnectionSettings.Name = settingsWidget.ConnectionSettings.Name;
					context.IsTemporary = false;
					MessageService.ShowMessage (AddinCatalog.GetString ("Database has been created."));
					ConnectionContextService.AddDatabaseConnectionContext (context);
				} catch (Exception ex) {
					QueryService.RaiseException (ex);
					Respond (ResponseType.Close);
					return;
				}
			}
			Respond (ResponseType.Ok);
			
		}

		protected virtual void CancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Destroy ();
		}
		
		protected virtual void Validate (object sender, EventArgs e)
		{
			buttonOk.Sensitive = settingsWidget.ValidateFields ();
		}
		
		protected virtual void OnBeforeDatabaseCreation (DatabaseSchema schema)
		{
			
		}
	}
}
