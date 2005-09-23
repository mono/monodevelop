//
// Providers/MySqlDbProvider.cs
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

using ByteFX.Data.MySqlClient;

namespace Mono.Data.Sql
{
	[Serializable]
	public class MySqlDbProvider : DbProviderBase
	{
		protected MySqlConnection connection = null;
		protected MySqlDataAdapter adapter = new MySqlDataAdapter ();
		protected bool isConnectionStringWrong = false;
		
		public override string ProviderName {
			get {
				return "MySQL Database";
			}
		}
		
		public override IDbConnection Connection {
			get {
				if (connection == null)
					connection = new MySqlConnection ();
				
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
		
		public override bool SupportsSchemaType(Type type)
		{
			if (type == typeof(TableSchema))
				return true;
			if (type == typeof(ColumnSchema))
				return true;
			else if (type == typeof(UserSchema))
				return true;
			else
				return false;
		}
		
		public override DataTable ExecuteSQL (string SQLText)
		{
			try {
				MySqlCommand command = new MySqlCommand ();
				command.Connection = Connection;
				command.CommandText = SQLText;
				
				DataSet resultSet = null;
				
				lock (adapter) {
					adapter.SelectCommand = command;
					adapter.Fill (resultSet);
				}
				
				return resultSet.Tables[0];
			} catch {
				return null;
			}
		}
		
		public override TableSchema[] GetTables ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			using (MySqlCommand command = new MySqlCommand ()) {
				command.Connection = Connection;
				command.CommandText =
					"SHOW TABLES;";
				MySqlDataReader r = command.ExecuteReader ();
				
				while (r.Read ()) {
					TableSchema table = new TableSchema ();
					table.Provider = this;
					
					table.Name = r.GetString (0);
					
					collection.Add (table);
				}
				
				r.Close ();
			}
			
			return (TableSchema[]) collection.ToArray (typeof (TableSchema));
		}
		
		public override ColumnSchema[] GetTableColumns (TableSchema table)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			using (MySqlCommand command = new MySqlCommand ()) {
				command.Connection = Connection;

				// XXX: Use String.Format cause mysql parameters suck assmar.
				command.CommandText =
					String.Format ("DESCRIBE {0}", table.Name);
				MySqlDataReader r = command.ExecuteReader ();
				
				while (r.Read ()) {
					ColumnSchema column = new ColumnSchema ();
					column.Provider = this;
					
					column.Name = r.GetString (0);
					column.DataTypeName = r.GetString (1);
					column.NotNull = r.IsDBNull (2);
					column.Default = r.GetString (4);
					column.Options["extra"] = r.GetString (5);
					
					collection.Add (column);
				}
				
				r.Close ();
			}
			
			return (ColumnSchema[]) collection.ToArray (typeof (ColumnSchema));
		}
		
		public override ConstraintSchema[] GetTableConstraints (TableSchema table)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			using (MySqlCommand command = new MySqlCommand ()) {
				command.Connection = Connection;
				command.CommandText =
					"";
				MySqlDataReader r = command.ExecuteReader ();
				
				while (r.Read ()) {
					ConstraintSchema constraint = new ConstraintSchema ();
					constraint.Provider = this;
					
					// TODO: Implement
					
					collection.Add (constraint);
				}
				
				r.Close ();
			}
			
			return (ConstraintSchema[]) collection.ToArray (
				typeof (ConstraintSchema));
		}

		public override UserSchema[] GetUsers ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");

			ArrayList collection = new ArrayList ();

			using (MySqlCommand command = new MySqlCommand ()) {
				command.Connection = connection;
				command.CommandText =
					"SELECT DISTINCT user from mysql.user where user != '';";
				MySqlDataReader r = command.ExecuteReader ();

				while (r.Read ()) {
					UserSchema user = new UserSchema ();
					user.Provider = this;
					user.Name = r.GetString (0);

					collection.Add (user);
				}

				r.Close ();
			}

			return (UserSchema[]) collection.ToArray (typeof (UserSchema));
		}
	}
}
