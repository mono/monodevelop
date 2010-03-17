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
using MonoDevelop.Core;
 
using MonoDevelop.Database.Sql;
using MonoDevelop.Database.Components;
using MonoDevelop.Database.Designer;

namespace MonoDevelop.Database.Sql.Sqlite
{
	public class SqliteCreateDatabaseDialog : CreateDatabaseDialog
	{
		ConnectionSettingsWidget connectionWidget;
		public SqliteCreateDatabaseDialog (IDbFactory factory)
			: base (factory)
		{
			
		}
		
		protected override ConnectionSettingsWidget CreateConnectionSettingsWidget (IDbFactory factory)
		{
			connectionWidget = new SqliteConnectionSettingsWidget (factory);
			connectionWidget.ShowSettings (factory.GetDefaultConnectionSettings ());
			connectionWidget.EnableOpenButton = true;
			connectionWidget.EnableTestButton = false;
			
			DatabaseConnectionSettings settings = new DatabaseConnectionSettings(connectionWidget.ConnectionSettings);
			// Set a temp database to avoid exception of the default connection pool.
			settings.Database = System.IO.Path.GetTempFileName ();
			// Create Context, Pool, Connection 
			DatabaseConnectionContext ctx = new DatabaseConnectionContext (settings, true);
			ctx.ConnectionPool.Initialize ();
			this.DatabaseConnection = ctx;
			return connectionWidget;
		}
	}
}
