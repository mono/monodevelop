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
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	public partial class DatabaseConnectionSettingsDialog : Gtk.Dialog
	{
		protected ConnectionSettingsWidget settingsWidget;
		protected bool isEditMode;
		protected DatabaseConnectionSettings settings;
		
		protected DatabaseConnectionSettingsDialog (IDbFactory factory, bool isEditMode)
		{
			this.isEditMode = isEditMode;
			
			this.Build ();

			if (isEditMode)
				Title = AddinCatalog.GetString ("Edit Database Connection");
			else
				Title = AddinCatalog.GetString ("Add Database Connection");
			
			settingsWidget = CreateConnectionSettingsWidget (factory, isEditMode);
			settingsWidget.NeedsValidation += delegate (object sender, EventArgs args) {
				buttonOk.Sensitive = settingsWidget.ValidateFields ();
			};
			vbox.PackStart (settingsWidget);
			vbox.ShowAll ();
		}

		public DatabaseConnectionSettingsDialog (IDbFactory factory)
			: this (factory, false)
		{
			settingsWidget.ShowSettings (factory.GetDefaultConnectionSettings ());
		}
		
		public DatabaseConnectionSettingsDialog (IDbFactory factory, DatabaseConnectionSettings settings)
			: this (factory, true)
		{
			if (settings == null)
				throw new ArgumentNullException ("settings");

			settingsWidget.ShowSettings (settings);
			settingsWidget.AppendDatabase (settings);
		}
		
		public DatabaseConnectionSettings ConnectionSettings {
			get { 
				if (settings == null)
					return settingsWidget.ConnectionSettings;
				else
					return settings;
			}
		}
		
		public ConnectionSettingsWidget ConnectionSettingsWidget {
			get { return settingsWidget; }
		}
		
		protected virtual ConnectionSettingsWidget CreateConnectionSettingsWidget (IDbFactory factory, bool isEditMode)
		{
			return new ConnectionSettingsWidget (factory, isEditMode);
		}

		protected virtual void OnOkClicked (object sender, System.EventArgs e)
		{
			if (!isEditMode)
				ConnectionContextService.AddDatabaseConnectionContext (ConnectionSettings);
			settings = settingsWidget.ConnectionSettings;
			
			Respond (ResponseType.Ok);
			Hide ();
		}

		protected virtual void OnCancelClicked (object sender, System.EventArgs e)
		{
			Respond (ResponseType.Cancel);
			Destroy ();
		}
	}
}
