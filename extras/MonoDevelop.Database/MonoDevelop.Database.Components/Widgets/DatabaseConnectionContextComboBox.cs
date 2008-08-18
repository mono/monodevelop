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
using System.Collections.Generic;
using MonoDevelop.Database.Sql;

namespace MonoDevelop.Database.Components
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public class DatabaseConnectionContextComboBox : ComboBox
	{
		private ListStore store;
		
		public DatabaseConnectionContextComboBox ()
		{
			store = new ListStore (typeof (string), typeof (object));
			Model = store;
			
			CellRendererText textRenderer = new CellRendererText ();
			PackStart (textRenderer, true);
			AddAttribute (textRenderer, "text", 0);
			
			foreach (DatabaseConnectionContext context in ConnectionContextService.DatabaseConnections)
				store.AppendValues (context.ConnectionSettings.Name, context);
			TreeIter iter;
			if (store.GetIterFirst (out iter))
				SetActiveIter (iter);
			
			ConnectionContextService.ConnectionContextAdded += new DatabaseConnectionContextEventHandler (OnConnectionAdded);
			ConnectionContextService.ConnectionContextRemoved += new DatabaseConnectionContextEventHandler (OnConnectionRemoved);
			ConnectionContextService.ConnectionContextEdited += new DatabaseConnectionContextEventHandler (OnConnectionEdited);
			ConnectionContextService.ConnectionContextRefreshed += new DatabaseConnectionContextEventHandler (OnConnectionRefreshed);
		}

		public DatabaseConnectionContext DatabaseConnection {
			get {
				TreeIter iter;
				if (GetActiveIter (out iter))
					return store.GetValue (iter, 1) as DatabaseConnectionContext;
				return null;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ("DatabaseConnection");
				
				TreeIter iter = GetTreeIter (value);
				if (!iter.Equals (TreeIter.Zero))
					SetActiveIter (iter);
			}
		}
		
		public void AddDatabaseConnectionContext (DatabaseConnectionContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");
			
			store.AppendValues (context.ConnectionSettings.Name, context);
		}
		
		private void OnConnectionAdded (object sender, DatabaseConnectionContextEventArgs args)
		{
			TreeIter newIter = store.AppendValues (args.ConnectionContext.ConnectionSettings.Name, args.ConnectionContext);
			TreeIter iter;
			if (!GetActiveIter (out iter))
				SetActiveIter (newIter);
		}
		
		private void OnConnectionRemoved (object sender, DatabaseConnectionContextEventArgs args)
		{
			TreeIter iter = GetTreeIter (args.ConnectionContext);
			TreeIter selected;
			if (GetActiveIter (out selected)) {
				if (iter.Equals (selected)) {
					store.Remove (ref iter);
					if (store.GetIterFirst (out iter))
						SetActiveIter (iter);
				}
			}
			store.Remove (ref iter);
		}
		
		private void OnConnectionEdited (object sender, DatabaseConnectionContextEventArgs args)
		{
			TreeIter iter = GetTreeIter (args.ConnectionContext);
			store.SetValue (iter, 0, args.ConnectionContext.ConnectionSettings.Name);
		}
		
		private void OnConnectionRefreshed (object sender, DatabaseConnectionContextEventArgs args)
		{
			TreeIter iter = GetTreeIter (args.ConnectionContext);
			store.SetValue (iter, 0, args.ConnectionContext.ConnectionSettings.Name);
		}
		
		private TreeIter GetTreeIter (DatabaseConnectionContext context)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter)) {
				do {
					object obj = store.GetValue (iter, 1);
					if (obj == context)
						return iter;
				} while (store.IterNext (ref iter));
			}
			return TreeIter.Zero;
		}
	}
}