//
// Provider/FirebirdDbProvider.cs
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

using FirebirdSql.Data.Firebird;

namespace Mono.Data.Sql
{
	/// <summary>
	/// Mono.Data.Sql provider for PostgreSQL databases.
	/// </summary>
	[Serializable]
	public class FirebirdDbProvider : DbProviderBase
	{
		protected FbConnection connection = null;
		protected FbDataAdapter adapter = new FbDataAdapter();
		protected bool isConnectionStringWrong = false;
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		public FirebirdDbProvider () : base ()
		{
		}
		
		public override string ProviderName {
			get {
				return "Firebird Database";
			}
		}
		
		/// <summary>
		/// Constructor with ADO.NET Npgsql connection.
		/// </summary>
		public FirebirdDbProvider (FbConnection conn)
		{
			connection = conn;
		}
		
		/// <summary>
		/// ADO.NET Connection
		/// </summary>
		public override IDbConnection Connection {
			get {
				if (connection == null)
					connection = new FbConnection();
				
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
				FbCommand command = new FbCommand();
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

			DataTable dataTable = connection.GetSchema ("Tables", new string[] {null, null, null, "TABLE"});

			for (int r = 0; r < dataTable.Rows.Count; r++) {
				DataRow row = dataTable.Rows[r];
				string tableName = row["TABLE_NAME"].ToString();

				TableSchema table = new TableSchema();
				table.Provider = this;
				table.Name = tableName;
				table.IsSystemTable = false; // TODO
				
				table.SchemaName = String.Empty;
				table.OwnerName = String.Empty;
				table.Comment = String.Empty;
				
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat ("-- Table: {0}\n", table.Name);
				sb.AppendFormat ("-- DROP TABLE {0};\n\n", table.Name);
				sb.AppendFormat ("CREATE TABLE {0} (\n", table.Name);
				
				ColumnSchema[] columns = table.Columns;
				string[] parts = new string[columns.Length];
				for (int i = 0; i < parts.Length; i++) {
					parts[i] = "\t" + columns[i].Definition;
				}
				sb.Append (String.Join (",\n", parts));
				
				//ConstraintSchema[] cons = table.Constraints;
				/*
				parts = new string[cons.Length];
				if (cons.Length > 0)
					sb.Append (",\n");
				for (int i = 0; i < parts.Length; i++) {
					parts[i] = "\t" + cons[i].Definition;
				}
				sb.Append (String.Join (",\n", parts));
				*/
				
				sb.Append ("\n);\n");
				//sb.AppendFormat ("COMMENT ON TABLE {0} IS '{1}';", table.Name, table.Comment);
				table.Definition = sb.ToString();
				collection.Add (table);

			}
			
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
			
			DataTable table2 = connection.GetSchema ("Columns", new string[] {null, null, table.Name, null});
			for (int r = 0; r < table2.Rows.Count; r++) {
				DataRow row2 = table2.Rows[r];

				string columnName =	row2["COLUMN_NAME"].ToString();
				string dataType = row2["COLUMN_DATA_TYPE"].ToString();

				int columnSize = 0;
				if (row2["COLUMN_SIZE"] != DBNull.Value)
					columnSize = (int) row2["COLUMN_SIZE"];

				int precision = 0;
				if (row2["NUMERIC_PRECISION"] != DBNull.Value)
					precision = (int) row2["NUMERIC_PRECISION"];
					
				int scale = 0;
				if (row2["NUMERIC_SCALE"] != DBNull.Value)
					scale = (int) row2["NUMERIC_SCALE"];

				//bool isNullable = false; // FIXME: is nullable
				//short n = 0;
				//if (row2["IS_NULLABLE"] != DBNull.Value)
				//	n = (short) row2["IS_NULLABLE"];
				//	
				//if (n == 1)
				//	isNullable = true;

				//int pos = 0; // FIXME: ordinal position
				//if (row2["ORDINAL_POSITION"] != DBNull.Value)
				//	pos = (int) row2["ORDINAL_POSITION"];

				ColumnSchema column = new ColumnSchema();
				
				column.Name = columnName;
				column.Provider = this;
				column.DataTypeName = dataType;
				column.Default = "";
				column.Comment = "";
				column.OwnerName = "";
				column.SchemaName = table.SchemaName;
				column.NotNull = false; // TODO
				column.Length = columnSize;
				column.Precision = precision;
				column.Scale = scale;
				
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("{0} {1}{2}",
					column.Name,
					column.DataTypeName,
					(column.Length > 0) ? ("(" + column.Length + ")") : "");
				sb.AppendFormat(" {0}", column.NotNull ? "NOT NULL" : "NULL");
				if (column.Default.Length > 0)
					sb.AppendFormat(" DEFAULT {0}", column.Default);
				column.Definition = sb.ToString();
				
				collection.Add(column);
			}
			
			return (ColumnSchema[]) collection.ToArray(typeof(ColumnSchema));
		}

		/// <summary>
		/// Get a collection of views from the system.
		/// </summary>
		public override ViewSchema[] GetViews()
		{
			ArrayList collection = new ArrayList();

			DataTable table2 = connection.GetSchema ("Views", new string[] {null, null, null});
			for (int r = 0; r < table2.Rows.Count; r++) {
				DataRow row2 = table2.Rows[r];
				string viewName = row2["VIEW_NAME"].ToString();

				ViewSchema view = new ViewSchema();
				view.Provider = this;
				
				view.Name = viewName;
				view.SchemaName = "";
				view.OwnerName = "";
				
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat ("-- View: {0}\n", view.Name);
				sb.AppendFormat ("-- DROP VIEW {0};\n\n", view.Name);
				sb.AppendFormat ("CREATE VIEW {0} AS (\n", view.Name);
				view.Definition = "";
				
				view.IsSystemView = false; 
				view.Comment = "";

				collection.Add(view);
			}
			
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
			
			// TODO: get view columns

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

			// TODO: get constraints
			
			return (ConstraintSchema[]) collection.ToArray (typeof(ConstraintSchema));
		}
		
		public override UserSchema[] GetUsers ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();

			// TODO: get users
			
			return (UserSchema[]) collection.ToArray (typeof (UserSchema));
		}
		
		public override ProcedureSchema[] GetProcedures ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();

			DataTable table2 = null;
			DataRow row2 = null;
			table2 = connection.GetSchema ("Procedures", new string[] {null, null, null});
			for (int r = 0; r < table2.Rows.Count; r++) {
				row2 = table2.Rows[r];
				ProcedureSchema procedure = new ProcedureSchema ();		
				procedure.Provider = this;
				procedure.Name = row2["PROCEDURE_NAME"].ToString();
				procedure.Definition = "";
				procedure.LanguageName = "";
				procedure.IsSystemProcedure = false;
				collection.Add (procedure);
				row2 = null;
			}
			table2 = null;

			table2 = connection.GetSchema ("Functions", new string[] {null, null, null, null});
			for (int r = 0; r < table2.Rows.Count; r++) {
				row2 = table2.Rows[r];
				ProcedureSchema procedure = new ProcedureSchema ();		
				procedure.Provider = this;
				procedure.Name = row2["FUNCTION_NAME"].ToString();
				procedure.Definition = "";
				procedure.LanguageName = "";
				procedure.IsSystemProcedure = false;
				collection.Add (procedure);
				row2 = null;
			}
			table2 = null;
			
			return (ProcedureSchema[]) collection.ToArray (typeof (ProcedureSchema)); 
		}
		
		public override ColumnSchema[] GetProcedureColumns (ProcedureSchema schema)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			// TODO
			
			return (ColumnSchema[]) collection.ToArray (typeof (ColumnSchema));
		}
	}
}
