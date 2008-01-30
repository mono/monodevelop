//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
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

using System;
using System.Data;
using System.Data.Odbc;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql.Odbc
{
	public class OdbcPooledDbConnection : AbstractPooledDbConnection
	{
		public OdbcPooledDbConnection (IConnectionPool connectionPool, IDbConnection connection)
			: base (connectionPool, connection)
		{
		}
		
		public override Version DatabaseVersion {
			get {
				OdbcConnection connection = DbConnection as OdbcConnection;
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				//try to read as much characters and dots as possible, in the hope that it's a valid version
				int dots = 0;
				foreach (char c in connection.ServerVersion) {
					if (char.IsNumber (c)) {
						sb.Append (c);
					} else if (c == '.') {
						if (++dots <= 3)
							sb.Append (c);
						else
							break;
					} else {
						break;
					}
				}
				try {
					return new Version (sb.ToString ());
				} catch {
					return new Version (1, 0, 0);
				}
			}
		}
		
		public override DataSet ExecuteSet (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentException ("command");

			DataSet set = new DataSet ();
			using (command) {
				using (OdbcDataAdapter adapter = new OdbcDataAdapter (command as OdbcCommand)) {
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
				using (OdbcDataAdapter adapter = new OdbcDataAdapter (command as OdbcCommand)) {
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
			return (connection as OdbcConnection).GetSchema (collectionName, restrictionValues);
		}
	}
}
