// 
// DatabaseConnectionEditorWidget.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2010 Lucian0
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
using MonoDevelop.Database.Components;
using MonoDevelop.Database.Sql;
using System;

namespace MonoDevelop.Database.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class DatabaseConnectionEditorWidget : Gtk.Bin
	{
		public event EventHandler SelectedDatabaseChanged;
		
		public DatabaseConnectionEditorWidget ()
		{
			this.Build ();
			this.comboConnection.Active = -1;
		}
		
		public DatabaseConnectionContext DatabaseConnection {
			get {
				return comboConnection.DatabaseConnection;
			}
		}
		
		protected virtual void OnButtonEditClicked (object sender, System.EventArgs e)
		{
			DatabaseConnectionSettings settings = null;
			DatabaseConnectionContext ctx;
			if (comboConnection.DatabaseConnection != null) {
				ctx = comboConnection.DatabaseConnection;
				if (ctx.DbFactory.GuiProvider.ShowEditConnectionDialog (comboConnection.DatabaseConnection.DbFactory,
				                                                    comboConnection.DatabaseConnection.ConnectionSettings,
				                                                    out settings)) {
					DatabaseConnectionContext newContext = new DatabaseConnectionContext (settings);
					ConnectionContextService.RemoveDatabaseConnectionContext (ctx);
					ConnectionContextService.AddDatabaseConnectionContext (newContext);
				}
			}
		}
		
		protected virtual void OnButtonNewClicked (object sender, System.EventArgs e)
		{
			DatabaseAvailableProvidersDialog provs = new DatabaseAvailableProvidersDialog ();
			if (provs.Run () == (int)ResponseType.Ok) {
				if (provs.SelectedProvider != null)
					provs.SelectedProvider.GuiProvider.ShowAddConnectionDialog (provs.SelectedProvider);
			}
			provs.Destroy ();

		}
		
		protected virtual void OnComboConnectionChanged (object sender, System.EventArgs e)
		{
			if (SelectedDatabaseChanged != null)
				SelectedDatabaseChanged (this, new EventArgs ());
		}
		
		
	}
}

