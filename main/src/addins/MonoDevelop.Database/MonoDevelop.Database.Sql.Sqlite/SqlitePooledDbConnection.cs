//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
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

using System;
using System.Data;
using System.Collections.Generic;
using Mono.Data.Sqlite;

namespace MonoDevelop.Database.Sql
{
	public class SqlitePooledDbConnection : AbstractPooledDbConnection
	{
		public SqlitePooledDbConnection (IConnectionPool connectionPool, IDbConnection connection)
			: base (connectionPool, connection)
		{
		}
		
		public override DataSet ExecuteSet (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentException ("command");

			DataSet set = new DataSet ();
			using (command) {
				using (SqliteDataAdapter adapter = new SqliteDataAdapter (command as SqliteCommand)) {
					try {
						adapter.Fill (set);
					} catch (Exception e) {
						QueryService.RaiseException (e);
					}
				}
			}
			return set;
		}

		public override DataTable ExecuteTable (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentException ("command");

			DataTable table = new DataTable ();
			using (command) {
				using (SqliteDataAdapter adapter = new SqliteDataAdapter (command as SqliteCommand)) {
					try {
						adapter.Fill (table);
					} catch (Exception e) {
						QueryService.RaiseException (e);
					}
				}
			}
			return table;
		}
		
		public override DataTable GetSchema (string collectionName, params string[] restrictionValues)
		{
			return (connection as SqliteConnection).GetSchema (collectionName, restrictionValues);
		}
	}
}
