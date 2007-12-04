//
// Provider/OdbcDbProvider.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Daniel Morgan <danielmorgan@verizon.net>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (C) 2005 Daniel Morgan
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
using System.Text;
using System.Text.RegularExpressions;

using System.Data.Odbc;

namespace Mono.Data.Sql
{
	/// <summary>
	/// Mono.Data.Sql provider for PostgreSQL databases.
	/// </summary>
	[Serializable]
	public class OdbcDbProvider : DbProviderBase
	{
		protected OdbcConnection connection = null;
		protected OdbcDataAdapter adapter = new OdbcDataAdapter();
		protected bool isConnectionStringWrong = false;
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		public OdbcDbProvider () : base ()
		{
		}
		
		public override string ProviderName {
			get {
				return "Provider for ODBC Data Sources";
			}
		}
		
		/// <summary>
		/// Constructor with ADO.NET ODBC connection.
		/// </summary>
		public OdbcDbProvider (OdbcConnection conn)
		{
			connection = conn;
		}
		
		/// <summary>
		/// ADO.NET Connection
		/// </summary>
		public override IDbConnection Connection {
			get {
				if (connection == null)
					connection = new OdbcConnection();
				
				return (IDbConnection) connection;
			}
		}
		
		/// <summary>
		/// Connection String
		/// </summary>
		public override string ConnectionString {
			get {
				return Connection.ConnectionString;
			}
			set {
				if (IsOpen == true)
					Close();
				
				Connection.ConnectionString = value;
				isConnectionStringWrong = false;
			}
		}
		
		/// <summary>
		/// Is the connection open
		/// </summary>
		public override bool IsOpen {
			get {
				return Connection.State == ConnectionState.Open;
			}
		}
		
		/// <summary>
		/// Is the last used connection string wrong
		/// </summary>
		public override bool IsConnectionStringWrong {
			get {
				return isConnectionStringWrong;
			}
		}
		
		/// <summary>
		/// Open the connection. Returns true on success.
		/// </summary>
		public override bool Open()
		{
			try {
				Connection.Open();
			} catch {
				isConnectionStringWrong = true;
			}
			OnOpen ();
			return IsOpen;
		}
		
		/// <summary>
		/// Close the database connection.
		/// </summary>
		public override void Close()
		{
			Connection.Close();
			OnClose();
		}
		
		/// <summary>
		/// Do we support the passed schema type
		/// </summary>
		public override bool SupportsSchemaType(Type type)
		{
			if (type == typeof(TableSchema))
				return true;
			else if (type == typeof(ViewSchema))
				return true;
			else if (type == typeof(ProcedureSchema))
				return true;
			else if (type == typeof(AggregateSchema))
				return true;
			else if (type == typeof(GroupSchema))
				return true;
			else if (type == typeof(UserSchema))
				return true;
			else if (type == typeof(LanguageSchema))
				return true;
			else if (type == typeof(OperatorSchema))
				return true;
			else if (type == typeof(RoleSchema))
				return true;
			else if (type == typeof(SequenceSchema))
				return true;
			else if (type == typeof(DataTypeSchema))
				return true;
			else if (type == typeof(TriggerSchema))
				return true;
			else if (type == typeof(ColumnSchema))
				return true;
			else if (type == typeof(ConstraintSchema))
				return true;
			else if (type == typeof(RuleSchema))
				return true;
			else
				return false;
		}
		
		/// <summary>
		/// Thread safe SQL execution.
		/// </summary>
		public override DataTable ExecuteSQL(string SQLText)
		{
			try {
				OdbcCommand command = new OdbcCommand();
				command.Connection = connection;
				command.CommandText = SQLText;

				DataSet resultSet = new DataSet ();

				lock(adapter) {
					adapter.SelectCommand = command;
					adapter.Fill(resultSet);
				}

				return resultSet.Tables[0];
			} catch {
				return null;
			}
		}
		
		/// <summary>
		/// Get a list of tables in the system.
		/// </summary>
		public override TableSchema[] GetTables()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			// TODO:
			return (TableSchema[]) collection.ToArray (typeof (TableSchema));
		}
		
		/// <summary>
		/// Get columns for a table.
		/// </summary>
		public override ColumnSchema[] GetTableColumns(TableSchema table)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList();
			// TODO:
			return (ColumnSchema[]) collection.ToArray(typeof(ColumnSchema));
		}

		/// <summary>
		/// Get a collection of views from the system.
		/// </summary>
		public override ViewSchema[] GetViews()
		{
			ArrayList collection = new ArrayList();
			// TODO:
			return (ViewSchema[]) collection.ToArray (typeof (ViewSchema));
		}
		
		/// <summary>
		/// Get a collection of columns within a view
		/// </summary>
		public override ColumnSchema[] GetViewColumns(ViewSchema view)
		{
			if (IsOpen == false && Open() == false)
				throw new Exception ("No connection to database");
			
			ArrayList collection = new ArrayList();
			// TODO:			
			
			return (ColumnSchema[]) collection.ToArray (typeof(ColumnSchema));
		}
		
		/// <summary>
		/// Get a collection of constraints within a a table.
		/// </summary>
		public override ConstraintSchema[] GetTableConstraints (TableSchema table)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			// TODO:			
			
			return (ConstraintSchema[]) collection.ToArray (typeof(ConstraintSchema));
		}
		
		public override UserSchema[] GetUsers ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			// TODO:			
			return (UserSchema[]) collection.ToArray (typeof (UserSchema));
		}
		
		public override ProcedureSchema[] GetProcedures ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			// TODO:			
			return (ProcedureSchema[]) collection.ToArray (typeof (ProcedureSchema)); 
		}
		
		public override ColumnSchema[] GetProcedureColumns (ProcedureSchema schema)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			// TODO:						
			return (ColumnSchema[]) collection.ToArray (typeof (ColumnSchema));
		}
	}
}

