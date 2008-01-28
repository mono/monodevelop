//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2008 Ben Motmans
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	public partial class ConnectionSettingsWidget : Gtk.Bin
	{
		public event EventHandler NeedsValidation;
		
		protected bool isDefaultSettings;

		protected ListStore storeDatabases;
		protected bool isDatabaseListEmpty;
		
		protected bool enablePasswordEntry;
		protected bool enableUsernameEntry;
		protected bool enableServerEntry;
		protected bool enablePortEntry;
		protected bool enableRefreshButton;
		protected bool enableOpenButton;
		
		protected IDbFactory factory;
		protected DatabaseConnectionSettings settings;

		public ConnectionSettingsWidget (IDbFactory factory)
		{
			if (factory == null)
				throw new ArgumentNullException ("factory");
			
			this.Build();
			
			textConnectionString.Buffer.Changed += new EventHandler (ConnectionStringChanged);
			checkCustom.Toggled += new EventHandler (CustomConnectionStringActivated);
			
			storeDatabases = comboDatabase.Model as ListStore;
			comboDatabase.TextColumn = 0;
			comboDatabase.Entry.Changed += new EventHandler (DatabaseChanged);
			
			EnableOpenButton = false;
		}
		
		public DatabaseConnectionSettings ConnectionSettings {
			get {
				FillDatabaseConnectionSettings (settings);
				return settings;
			}
		}
		
		public bool EnableServerEntry {
			get { return enableServerEntry; }
			set {
				enableServerEntry = value;
				entryServer.Sensitive = value;
			}
		}
		
		public bool EnablePortEntry {
			get { return enablePortEntry; }
			set {
				enablePortEntry = value;
				spinPort.Sensitive = value;
			}
		}
		
		public bool EnableUsernameEntry {
			get { return enableUsernameEntry; }
			set {
				enableUsernameEntry = value;
				entryUsername.Sensitive = value;
			}
		}
		
		public bool EnablePasswordEntry {
			get { return enablePasswordEntry; }
			set {
				enablePasswordEntry = value;
				entryPassword.Sensitive = value;
			}
		}
		
		public bool EnableRefreshButton {
			get { return enableRefreshButton; }
			set {
				enableRefreshButton = value;
				buttonRefresh.Sensitive = value;
			}
		}
		
		public bool EnableOpenButton {
			get { return enableOpenButton; }
			set {
				enableOpenButton = value;
				buttonOpen.Sensitive = value;
			}
		}
		
		protected ComboBoxEntry ComboDatabase {
			get { return comboDatabase; }
		}
		
		protected internal virtual void FillDatabaseConnectionSettings (DatabaseConnectionSettings settings)
		{
			settings.ConnectionString = textConnectionString.Buffer.Text;
			settings.UseConnectionString = checkCustom.Active;
			settings.MinPoolSize = (int)spinMinPoolSize.Value;
			settings.MaxPoolSize = (int)spinMaxPoolSize.Value;
			settings.Name = entryName.Text;
			settings.Username = entryUsername.Text;
			settings.Password = entryPassword.Text;
			settings.Server = entryServer.Text;
			settings.Port = (int)spinPort.Value;
			settings.Database = comboDatabase.Entry.Text;
			settings.SavePassword = checkSavePassword.Active;
		}
		
		protected internal virtual void ShowSettings (DatabaseConnectionSettings settings)
		{
			checkCustom.Active = settings.UseConnectionString;
			entryName.Text = String.IsNullOrEmpty (settings.Name) ? String.Empty : settings.Name;
			entryPassword.Text = String.IsNullOrEmpty (settings.Password) ? String.Empty : settings.Password;
			spinPort.Value = settings.Port > 0 ? settings.Port : spinPort.Value;
			entryServer.Text = String.IsNullOrEmpty (settings.Server) ? String.Empty : settings.Server;
			entryUsername.Text = String.IsNullOrEmpty (settings.Username) ? String.Empty : settings.Username;
			textConnectionString.Buffer.Text = String.IsNullOrEmpty (settings.ConnectionString) ? String.Empty : settings.ConnectionString;
			comboDatabase.Entry.Text = String.IsNullOrEmpty (settings.Database) ? String.Empty : settings.Database;
			spinMinPoolSize.Value = settings.MinPoolSize;
			spinMaxPoolSize.Value = settings.MaxPoolSize;
		}
		
		protected internal virtual void AppendDatabase (DatabaseConnectionSettings settings)
		{
			storeDatabases.AppendValues (settings.Database);
			isDatabaseListEmpty = false;
		}

		protected virtual void NameChanged (object sender, System.EventArgs e)
		{
			CheckSettings ();
		}

		protected virtual void ServerChanged (object sender, System.EventArgs e)
		{
			CheckSettings ();
		}

		protected virtual void PortChanged (object sender, System.EventArgs e)
		{
			CheckSettings ();
		}

		protected virtual void UsernameChanged (object sender, System.EventArgs e)
		{
			CheckSettings ();
		}

		protected virtual void PasswordChanged (object sender, System.EventArgs e)
		{
			CheckSettings ();
		}

		protected virtual void MinPoolSizeChanged (object sender, System.EventArgs e)
		{
			if (spinMinPoolSize.Value > spinMaxPoolSize.Value)
				spinMaxPoolSize.Value = spinMinPoolSize.Value;
		}

		protected virtual void MaxPoolSizeChanged (object sender, System.EventArgs e)
		{
			if (spinMaxPoolSize.Value < spinMinPoolSize.Value)
				spinMinPoolSize.Value = spinMaxPoolSize.Value;
		}
		
		protected virtual void CustomConnectionStringActivated (object sender, System.EventArgs e)
		{
			bool sens = !checkCustom.Active;
			
			entryPassword.Sensitive = sens && enablePasswordEntry;
			entryUsername.Sensitive = sens && enableUsernameEntry;
			entryServer.Sensitive = sens && enableServerEntry;
			spinPort.Sensitive = sens && enablePortEntry;
			comboDatabase.Sensitive = sens;
			buttonRefresh.Sensitive = sens && enableRefreshButton;
			scrolledwindow.Sensitive = !sens;
		}
		
		protected virtual void ConnectionStringChanged (object sender, EventArgs e)
		{
			CheckSettings ();
		}
		
		protected virtual void DatabaseChanged (object sender, EventArgs e)
		{
			if (isDatabaseListEmpty && comboDatabase.Entry.Text == AddinCatalog.GetString ("No databases found!")) {
				comboDatabase.Entry.Text = String.Empty;
			}
			
			CheckSettings ();
		}

		protected virtual void CheckSettings ()
		{
			isDefaultSettings = false;
			
			if (NeedsValidation != null)
				NeedsValidation (this, EventArgs.Empty);
		}
		
		public virtual bool ValidateFields ()
		{
			bool ok = false;
			if (checkCustom.Active) {
				ok = textConnectionString.Buffer.Text.Length > 0;
			} else {
				TreeIter iter;
				ok = entryName.Text.Length > 0
					&& (entryServer.Text.Length > 0 || !enableServerEntry)
					&& (entryUsername.Text.Length > 0 || !enableUsernameEntry)
					&& (comboDatabase.Entry.Text.Length > 0);
			}
			return ok;
		}
		
		protected virtual DatabaseConnectionSettings CreateDatabaseConnectionSettings ()
		{
			DatabaseConnectionSettings settings = new DatabaseConnectionSettings ();
			settings.ProviderIdentifier = factory.Identifier;
			FillDatabaseConnectionSettings (settings);
			return settings;
		}

		protected virtual void RefreshClicked (object sender, System.EventArgs e)
		{
			DatabaseConnectionSettings settingsCopy = CreateDatabaseConnectionSettings ();
			storeDatabases.Clear ();
			
			ThreadPool.QueueUserWorkItem (new WaitCallback (RefreshClickedThreaded), settingsCopy);
		}
		
		protected virtual void RefreshClickedThreaded (object state)
		{
			DatabaseConnectionSettings settings = state as DatabaseConnectionSettings;
			DatabaseConnectionContext context = new DatabaseConnectionContext (settings);
			IDbFactory fac = DbFactoryService.GetDbFactory (settings.ProviderIdentifier);
			try {
				FakeConnectionPool pool = new FakeConnectionPool (fac, fac.ConnectionProvider, context);
				pool.Initialize ();
				
				ISchemaProvider prov = fac.CreateSchemaProvider (pool);
				DatabaseSchemaCollection databases = prov.GetDatabases ();
				
				foreach (DatabaseSchema db in databases) {
					DispatchService.GuiDispatch (delegate () {
						storeDatabases.AppendValues (db.Name);
					});
				}
				isDatabaseListEmpty = databases.Count == 0;
			} catch {}

			if (isDatabaseListEmpty) {
				DispatchService.GuiDispatch (delegate () {
					storeDatabases.AppendValues (AddinCatalog.GetString ("No databases found!"));
				});
			} else {
				DispatchService.GuiDispatch (delegate () {
					TreeIter iter;
					if (storeDatabases.GetIterFirst (out iter))
						comboDatabase.SetActiveIter (iter);
				});
			}
		}
		
		protected internal void HideWidgets (bool hideServer, bool hidePort, bool hideUsername, bool hidePassword)
		{
			labelServer.Visible = !hideServer;
			entryServer.Visible = !hideServer;

			labelPort.Visible = !hidePort;
			spinPort.Visible = !hidePort;
	
			labelUsername.Visible = !hideUsername;
			entryUsername.Visible = !hideUsername;

			labelPassword.Visible = !hidePassword;
			hboxPassword.Visible = !hidePassword;
		}

		protected virtual void OpenClicked (object sender, System.EventArgs e)
		{
			//do nothing by default
		}

		protected virtual void TestClicked (object sender, System.EventArgs e)
		{
		}
	}
}
