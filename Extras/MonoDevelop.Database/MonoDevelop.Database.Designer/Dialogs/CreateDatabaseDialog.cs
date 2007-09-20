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
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;

namespace MonoDevelop.Database.Designer
{
	public partial class CreateDatabaseDialog : Gtk.Dialog
	{
		private DatabaseConnectionContext context;
		
		//TODO: validate db name + check for duplicates
		public CreateDatabaseDialog()
		{
			this.Build();
		}
		
		public DatabaseConnectionContext DatabaseConnection {
			get { return context; }
		}

		protected virtual void OkClicked (object sender, System.EventArgs e)
		{
			context = comboConnections.DatabaseConnection;
			if (context.IsTemporary) {
				//make it a real connection context and fill in the database
				context.ConnectionSettings.Database = entryDatabase.Text;
				context.ConnectionSettings.Name = entryName.Text;
				context.IsTemporary = false;
			} else {
				//create a copy of the settings and create a new context
				DatabaseConnectionSettings settings = new DatabaseConnectionSettings (context.ConnectionSettings);
				settings.Database = entryDatabase.Text;
				settings.Name = entryName.Text;
				context = new DatabaseConnectionContext (settings);
			}

			Respond (ResponseType.Ok);
			Destroy ();
		}

		protected virtual void CancelClicked (object sender, System.EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Destroy ();
		}

		protected virtual void SaveAsClicked (object sender, System.EventArgs e)
		{
			DatabaseConnectionContext context = comboConnections.DatabaseConnection;
			if (context == null)
				return;

			IDbFactory fac = context.DbFactory;
			
			string database = null;
			if (fac.GuiProvider.ShowSelectDatabaseDialog (true, out database))
				entryDatabase.Text = database;
		}

		protected virtual void NewClicked (object sender, System.EventArgs e)
		{
			DatabaseConnectionSettingsDialog dlg = new DatabaseConnectionSettingsDialog (true);
			if (dlg.Run () == (int)ResponseType.Ok) {
				DatabaseConnectionContext context = new DatabaseConnectionContext (dlg.ConnectionSettings, true);
				context.IsTemporary = true;
				comboConnections.AddDatabaseConnectionContext (context);
				comboConnections.DatabaseConnection = context;
			}
		}

		protected virtual void DatabaseNameChanged (object sender, EventArgs e)
		{
			CheckOkState ();
		}

		protected virtual void ConnectionChanged (object sender, EventArgs e)
		{
			CheckOkState ();
			
			if (entryName.Text.Length == 0) {
				DatabaseConnectionContext context = comboConnections.DatabaseConnection;
				entryName.Text = context.ConnectionSettings.Name;
			}
		}
		
		private void CheckOkState ()
		{
			DatabaseConnectionContext context = comboConnections.DatabaseConnection;
			if (context != null)
				buttonSelect.Sensitive = context.DbFactory.IsCapabilitySupported ("ConnectionSettings", SchemaActions.Schema, ConnectionSettingsCapabilities.SelectDatabase);
			buttonOk.Sensitive = context != null &&
				entryDatabase.Text.Length > 0 && entryName.Text.Length > 0;
		}

		protected virtual void NameChanged (object sender, System.EventArgs e)
		{
			CheckOkState ();
		}
	}
}
