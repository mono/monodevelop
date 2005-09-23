//
// Providers/SqliteDbProvider.cs
//
// Authors:
//   Christian Hergert <chris@mosaix.net>
//
// Copyright (c) 2005 Christian Hergert
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
using System.Collections;
using System.Data;

using Mono.Data.SqliteClient;

namespace Mono.Data.Sql
{
	[Serializable]
	public class SqliteDbProvider : DbProviderBase
	{
		protected SqliteConnection connection = null;
		protected SqliteDataAdapter adapter = new SqliteDataAdapter ();
		protected bool isConnectionStringWrong = false;
		
		public override string ProviderName {
			get {
				return "SQLite Database (Incomplete)";
			}
		}
		
		public override IDbConnection Connection {
			get {
				if (connection == null)
					connection = new SqliteConnection ();
				
				return (IDbConnection) connection;
			}
		}
		
		public override string ConnectionString {
			get {
				return Connection.ConnectionString;
			}
			set {
				if (IsOpen)
					Close ();
				
				Connection.ConnectionString = value;
				isConnectionStringWrong = false;
			}
		}
		
		public override bool IsOpen {
			get {
				return Connection.State == ConnectionState.Open;
			}
		}
		
		public override bool IsConnectionStringWrong {
			get {
				return isConnectionStringWrong;
			}
		}
		
		public override bool Open ()
		{
			try {
				Connection.Open ();
				OnOpen ();
			} catch (Exception e) {
				isConnectionStringWrong = true;
			}
			
			return IsOpen;
		}
		
		public override void Close ()
		{
			Connection.Close ();
			OnClose ();
		}
		
		public override ViewSchema[] GetViews ()
		{
			throw new NotImplementedException ();
		}
		
		public override TableSchema[] GetTables ()
		{
			throw new NotImplementedException ();
		}
		
		public override ColumnSchema[] GetTableColumns (TableSchema table)
		{
			throw new NotImplementedException ();
		}
		
		public override ColumnSchema[] GetViewColumns (ViewSchema view)
		{
			throw new NotImplementedException ();
		}
		
		public override ConstraintSchema[] GetTableConstraints (TableSchema table)
		{
			throw new NotImplementedException ();
		}
	}
}
