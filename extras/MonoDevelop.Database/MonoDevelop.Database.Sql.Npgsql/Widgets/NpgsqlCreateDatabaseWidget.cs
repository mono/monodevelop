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
using System.Text;

namespace MonoDevelop.Database.Sql.Npgsql
{
	[System.ComponentModel.Category("MonoDevelop.Database.Sql.Npgsql")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class NpgsqlCreateDatabaseWidget : Gtk.Bin
	{
		ListStore ownersStore;
		ListStore templatesStore;
		ListStore encodingsStore;
		ListStore tablespacesStore;
		
		public NpgsqlCreateDatabaseWidget()
		{
			this.Build();
			ownersStore = new ListStore (typeof (string), typeof (UserSchema));
			templatesStore = new ListStore (typeof (string), typeof (DatabaseSchema));
			encodingsStore = new ListStore (typeof (string), typeof (NpgsqlEncoding));
			tablespacesStore = new ListStore (typeof (string), typeof(NpgsqlTablespace));
			
			comboOwner.Model = ownersStore;
			comboTemplate.Model = templatesStore;
			comboEncoding.Model = encodingsStore;
			comboTablespace.Model = tablespacesStore;
		}
		
		private void ClearCombos ()
		{
			ownersStore.Clear ();
			templatesStore.Clear ();
			encodingsStore.Clear ();
			tablespacesStore.Clear ();
			
		}
		
		public void Initialize (NpgsqlSchemaProvider provider)
		{
			UserSchemaCollection users = provider.GetUsers ();
			DatabaseSchemaCollection databases = provider.GetDatabases ();
			NpgsqlEncodingCollection encodings = provider.GetEncodings ();
			NpgsqlTablespaceCollection tablespaces = provider.GetTablespaces ();
			
			foreach (UserSchema user in users) 
				ownersStore.AppendValues (user.Name, user);
			
			foreach (DatabaseSchema db in databases)
				templatesStore.AppendValues (db.Name, db);
			
			foreach (NpgsqlEncoding enc in encodings) {
				StringBuilder encName = new StringBuilder (enc.Name);
				encName.AppendFormat (" - {0} - {1}", enc.Description, enc.Language);
				if (enc.Aliases != string.Empty)
					encName.AppendFormat (" ({0})", enc.Aliases);
				encodingsStore.AppendValues (encName.ToString (), enc);
			}
			
			foreach (NpgsqlTablespace ts in tablespaces)
				tablespacesStore.AppendValues (ts.Name, ts);
		}

		public void SetDatabaseOptions (NpgsqlDatabaseSchema schema)
		{
			TreeIter iter;
			
			if (comboOwner.GetActiveIter (out iter))
				schema.Owner = (UserSchema)ownersStore.GetValue (iter,1);
			else if (comboOwner.ActiveText != String.Empty) {
				Console.WriteLine ("Elegido");
				UserSchema user = new UserSchema (schema.SchemaProvider);
				user.Name = comboOwner.ActiveText;
			}
			
			if (comboTemplate.GetActiveIter (out iter))
				schema.Template = (DatabaseSchema)templatesStore.GetValue (iter,1);
			else if (comboTemplate.ActiveText != string.Empty) {
				DatabaseSchema db = new DatabaseSchema (schema.SchemaProvider);
				db.Name = comboTemplate.ActiveText;
			}
			
			if (comboEncoding.GetActiveIter (out iter))
				schema.Encoding = (NpgsqlEncoding)encodingsStore.GetValue (iter, 1);
			else if (comboEncoding.ActiveText != string.Empty) {
				NpgsqlEncoding enc = new  NpgsqlEncoding (schema.SchemaProvider);
				enc.Name = comboEncoding.ActiveText;
			}
			
			if (comboTablespace.GetActiveIter (out iter))
				schema.Tablespace = (NpgsqlTablespace)tablespacesStore.GetValue (iter, 1);
			else if (comboTablespace.ActiveText != string.Empty) {
				NpgsqlTablespace ts = new NpgsqlTablespace (schema.SchemaProvider);
				ts.Name = comboTablespace.ActiveText;
			}
		}
		
	}
}
