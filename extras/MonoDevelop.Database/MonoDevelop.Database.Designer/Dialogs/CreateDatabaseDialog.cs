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
		
		private ConnectionSettingsWidget settingsWidget;
		private CreateDatabaseWidget dbWidget;

		//TODO: on page switch, create a temp connection setting context
		public CreateDatabaseDialog (IDbFactory factory)
		{
			this.Build();
			
			settingsWidget = CreateConnectionSettingsWidget (factory);
			dbWidget = CreateCreateDatabaseWidget (factory);
			
			vboxConnection.PackStart (settingsWidget, true, true, 0);
			vboxDatabase.PackStart (dbWidget, true, true, 0);
			
			settingsWidget.NeedsValidation += Validate;
			dbWidget.NeedsValidation += Validate;
			
			settingsWidget.EnableOpenButton = false;
			settingsWidget.EnableRefreshButton = false;
			
			ShowAll ();
		}
		
		public DatabaseConnectionContext DatabaseConnection {
			get { return context; }
		}
		
		protected virtual ConnectionSettingsWidget CreateConnectionSettingsWidget (IDbFactory factory)
		{
			return new ConnectionSettingsWidget (factory);
		}
		
		protected virtual CreateDatabaseWidget CreateCreateDatabaseWidget (IDbFactory factory)
		{
			return new CreateDatabaseWidget ();
		}
		
		protected virtual void OkClicked (object sender, EventArgs e)
		{
//			context = comboConnections.DatabaseConnection;
//			if (context.IsTemporary) {
//				//make it a real connection context and fill in the database
//				context.ConnectionSettings.Database = entryDatabase.Text;
//				context.ConnectionSettings.Name = entryName.Text;
//				context.IsTemporary = false;
//			} else {
//				//create a copy of the settings and create a new context
//				DatabaseConnectionSettings settings = new DatabaseConnectionSettings (context.ConnectionSettings);
//				settings.Database = entryDatabase.Text;
//				settings.Name = entryName.Text;
//				context = new DatabaseConnectionContext (settings);
//			}
//
//			Respond (ResponseType.Ok);
//			Destroy ();
		}

		protected virtual void CancelClicked (object sender, EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Destroy ();
		}
		
		protected virtual void Validate (object sender, EventArgs e)
		{
			buttonOk.Sensitive = settingsWidget.ValidateFields () && dbWidget.ValidateFields ();
		}
	}
}
