//
// ConnectionDialog.cs
//
// Author:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (C) 2005 Christian Hergert
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

using System;
using System.Reflection;

using Gtk;
using Glade;

using Mono.Data.Sql;

using MonoDevelop.Gui;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace MonoQuery
{
	public class ConnectionDialog
	{
		[Glade.Widget]
		protected Dialog newConnectionDialog;
		[Glade.Widget]
		protected ComboBox providersCombo;
		[Glade.Widget]
		protected Entry nameEntry;
		[Glade.Widget]
		protected Entry serverEntry;
		[Glade.Widget]
		protected Entry databaseEntry;
		[Glade.Widget]
		protected Entry useridEntry;
		[Glade.Widget]
		protected Entry passwordEntry;
		[Glade.Widget]
		protected Entry otherEntry;
		[Glade.Widget]
		protected TextView connectionStringTextView;
		
		public ConnectionDialog () : base ()
		{
			Glade.XML gxml = new Glade.XML (null, "monoquery.glade", "newConnectionDialog", null);
			gxml.Autoconnect (this);
			
			ListStore store = new ListStore (typeof (string), typeof (Type));
			
			MonoQueryService service = (MonoQueryService) ServiceManager.GetService (typeof (MonoQueryService));
			foreach (Type type in service.ProviderTypes) {
				Assembly asm = Assembly.GetAssembly (typeof (Mono.Data.Sql.DbProviderBase));
				DbProviderBase provider = (DbProviderBase) asm.CreateInstance (type.FullName);
				store.AppendValues (provider.ProviderName, type);
			}
			
			providersCombo.Clear ();
			
			CellRendererText ctext = new CellRendererText ();
			providersCombo.PackStart (ctext, false);
			providersCombo.AddAttribute (ctext, "text", 0);
			providersCombo.Model = store;
			providersCombo.Active = 0;
		}
		
		public virtual string ConnectionString {
			get {
				return connectionStringTextView.Buffer.Text;
			}
		}
		
		public virtual string ConnectionName {
			get {
				return nameEntry.Text;
			}
		}
		
		public virtual Type ConnectionType {
			get {
				TreeIter iter;
				providersCombo.GetActiveIter (out iter);
				return (Type) providersCombo.Model.GetValue (iter, 1);
			}
		}
		
		public int Run ()
		{
			return newConnectionDialog.Run ();
		}
		
		public void Destroy ()
		{
			newConnectionDialog.Destroy ();
		}
		
		protected void OnChanged (object sender, EventArgs args)
		{
			string connString = String.Empty;
			
			if (serverEntry.Text != String.Empty)
				connString += String.Format ("Server={0};", serverEntry.Text);
			if (databaseEntry.Text != String.Empty)
				connString += String.Format ("Database={0};", databaseEntry.Text);
			if (useridEntry.Text != String.Empty)
				connString += String.Format ("User ID={0};", useridEntry.Text);
			if (passwordEntry.Text != String.Empty)
				connString += String.Format ("Password={0};", passwordEntry.Text);
			if (otherEntry.Text != String.Empty)
				connString += otherEntry.Text;
			
			connectionStringTextView.Buffer.Text = connString;
		}
	}
}
