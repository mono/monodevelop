//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Daniel Morgan <danielmorgan@verizon.net>
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (C) 2005 Daniel Morgan
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

using System;
using System.Data;
using FirebirdSql.Data.Firebird;
using System.Collections.Generic;
namespace MonoDevelop.Database.Sql
{
	public class FirebirdConnectionProvider : AbstractConnectionProvider
	{
		public FirebirdConnectionProvider (IDbFactory factory, ConnectionSettings settings)
			: base (factory, settings)
		{
		}
		
		public override DataSet ExecuteQueryAsDataSet (string sql)
		{
			if (String.IsNullOrEmpty ("sql"))
				throw new ArgumentException ("sql");

			DataSet set = new DataSet ();
			using (IDbCommand command = CreateCommand (sql)) {
				using (FbDataAdapter adapter = new FbDataAdapter (command as FbCommand)) {
					try {
						adapter.Fill (set);
					} catch {
					} finally {
						command.Connection.Close ();
					}
				}
			}
			return set;
		}

		public override DataTable ExecuteQueryAsDataTable (string sql)
		{
			if (String.IsNullOrEmpty ("sql"))
				throw new ArgumentException ("sql");

			DataTable table = new DataTable ();
			using (IDbCommand command = CreateCommand (sql)) {
				using (FbDataAdapter adapter = new FbDataAdapter (command as FbCommand)) {
					try {
						adapter.Fill (table);
					} catch {
					} finally {
						command.Connection.Close ();
					}
				}
			}
			return table;
		}

		public override IDbConnection Open (out string errorMessage)
		{
			FbConnectionStringBuilder builder = null;
			try {	
				if (settings.UseConnectionString) {
					builder = new FbConnectionStringBuilder (settings.ConnectionString);
				} else {
					builder = new FbConnectionStringBuilder ();
					builder.Database = settings.Database;
					builder.UserID = settings.Username;
					builder.Password = settings.Password;					builder.Port = settings.Port;
					builder.DataSource = settings.Server;
				}
				builder.Pooling = settings.EnablePooling;
				builder.MinPoolSize = settings.MinPoolSize;
				builder.MaxPoolSize = settings.MaxPoolSize;
				connection = new FbConnection (builder.ToString ());
				connection.Open ();
				
				errorMessage = String.Empty;
				isConnectionError = false;
				return connection;
			} catch {
				isConnectionError = true;
				errorMessage = String.Format ("Unable to connect. (CS={0})", builder == null ? "NULL" : builder.ToString ());
				return null;
			}
		}
	}
}
