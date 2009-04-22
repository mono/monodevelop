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
using System.Data.SqlClient;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql.SqlServer
{
	public class SqlServerPooledDbConnection : AbstractPooledDbConnection
	{
		public SqlServerPooledDbConnection (IConnectionPool connectionPool, IDbConnection connection)
			: base (connectionPool, connection)
		{
		}
		
		public override Version DatabaseVersion {
			get {
				SqlConnection connection = DbConnection as SqlConnection;
				try {
					//http://msdn2.microsoft.com/en-us/library/system.data.sqlclient.sqlconnection.serverversion.aspx
					//##.##.#### major.minor.build
					return new Version (connection.ServerVersion);
				} catch {
					return new Version (7, 0);
				}
			}
		}
		
		public override DataSet ExecuteSet (IDbCommand command)
		{
			if (command == null)
				throw new ArgumentException ("command");

			DataSet set = new DataSet ();
			using (command) {
				using (SqlDataAdapter adapter = new SqlDataAdapter (command as SqlCommand)) {
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
				using (SqlDataAdapter adapter = new SqlDataAdapter (command as SqlCommand)) {
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
			return (connection as SqlConnection).GetSchema (collectionName, restrictionValues);
		}
		
		public override int ExecuteNonQuery (string sql)
		{
			int ret = 0;
			if(sql.IndexOf (String.Concat(Environment.NewLine,  "go", Environment.NewLine), 
			                 StringComparison.OrdinalIgnoreCase) < 0)
				return base.ExecuteNonQuery (sql); 
			else {
				// Divide the Sql In more than 1 command to avoid: ['CREATE TRIGGER' must be the first statement]
				// FIXME: This isn't in a transaction scope because Create table/trigger isn't 
				// affected by START/COMMIT/ROLLBACK transaction.
				string[] sep = new string[] {string.Concat (Environment.NewLine, "GO", Environment.NewLine)};
				string[] sqls = sql.Split (sep, StringSplitOptions.RemoveEmptyEntries);
				foreach (string s in sqls)
					ret = base.ExecuteNonQuery (s);
			}
			return ret;
		}
	}
	
}
