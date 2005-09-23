//
// Provider/OracleDbProvider.cs
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

using System.Data.OracleClient;

namespace Mono.Data.Sql
{
	/// <summary>
	/// Mono.Data.Sql provider for Orace databases.
	/// </summary>
	[Serializable]
	public class OracleDbProvider : DbProviderBase
	{
		protected OracleConnection connection = null;
		protected OracleDataAdapter adapter = new OracleDataAdapter();
		protected bool isConnectionStringWrong = false;

		public override string ProviderName {
			get {
				return "Oracle 8i/9i/10g";
			}
		}
		
		/// <summary>
		/// ADO.NET Connection
		/// </summary>
		public override IDbConnection Connection {
			get {
				if (connection == null) {
					connection = new OracleConnection();
				}
				
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
			OnOpen();
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
			else if (type == typeof(ColumnSchema))
				return true;
			else if (type == typeof(ViewSchema))
				return true;
			else if (type == typeof(ProcedureSchema))
				return true;
			else if (type == typeof(UserSchema))
				return true;
			else if (type == typeof(SequenceSchema))
				return true;
			else if (type == typeof(TriggerSchema))
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
				OracleCommand command = new OracleCommand();
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

		private bool IsSystem(string owner) 
		{
			switch(owner) {
				case "SYSTEM":
				case "SYS":
				case "DRSYS":
				case "CTXSYS":
				case "MDSYS":
				case "WKSYS":
					return true;
			}

			return false;
		}


		/// <summary>
		/// Get a list of tables in the system.
		/// </summary>
		public override TableSchema[] GetTables()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection.");
			
			ArrayList collection = new ArrayList();
			
			OracleCommand command = new OracleCommand();
			command.Connection = connection;
			command.CommandText =
				"SELECT OWNER, TABLE_NAME, TABLESPACE_NAME " +
				"FROM ALL_TABLES " +
				"ORDER BY OWNER, TABLE_NAME";
			OracleDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				TableSchema table = new TableSchema();
				table.Provider = this;
				table.OwnerName = r.GetValue(0).ToString();
				table.SchemaName = r.GetValue(0).ToString();
				table.Name = r.GetString(1).ToString();
				table.IsSystemTable = IsSystem(table.OwnerName);
				table.TableSpaceName = r.GetValue(2).ToString();
				
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
				
				ConstraintSchema[] cons = table.Constraints;
				parts = new string[cons.Length];
				if (cons.Length > 0)
					sb.Append (",\n");
				for (int i = 0; i < parts.Length; i++) {
					parts[i] = "\t" + cons[i].Definition;
				}
				sb.Append (String.Join (",\n", parts));
				
				//sb.AppendFormat ("\n) COMMENT '{0}';", table.Comment);
				table.Definition = "";
				collection.Add (table);
			}
			
			return (TableSchema[]) collection.ToArray(typeof(TableSchema));
		}

		private int GetInt (IDataReader reader, int field) 
		{
			if (reader.IsDBNull(field) == true)
				return 0;
			
			object v = reader.GetValue(field);
			string ds = v.ToString();
			int iss = Int32.Parse(ds);
			return iss;
		}
		
		/// <summary>
		/// Get columns for a table.
		/// </summary>
		public override ColumnSchema[] GetTableColumns(TableSchema table)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection.");
			
			ArrayList collection = new ArrayList();
			
			OracleCommand command = new OracleCommand();
			command.Connection = connection;
			command.CommandText = 
				"SELECT OWNER, TABLE_NAME, COLUMN_NAME, " +
				"       DATA_TYPE, DATA_LENGTH, DATA_PRECISION, DATA_SCALE, " +
				"       NULLABLE, COLUMN_ID, DEFAULT_LENGTH, DATA_DEFAULT " +
				"FROM ALL_TAB_COLUMNS " +
				"WHERE OWNER = '" + table.OwnerName + "' " + 
				"AND TABLE_NAME = '" + table.Name + "' " +
				"ORDER BY OWNER, TABLE_NAME, COLUMN_ID";
			OracleDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				ColumnSchema column = new ColumnSchema();
				
				try { column.Name = r.GetValue(2).ToString(); } catch {}
				column.Provider = this;
				try { column.DataTypeName = r.GetValue(3).ToString(); } catch {}
				column.Default = "";
				column.Comment = "";
				column.OwnerName = table.OwnerName;
				column.SchemaName = table.OwnerName;
				
				try { column.NotNull = r.GetValue(7).ToString() == "Y" ? true : false; } catch {}
				
				try { column.Length = GetInt(r, 4); } catch {}
				try { column.Precision = GetInt(r, 5); } catch {}
				try { column.Scale = GetInt(r, 6); } catch {}

				try { column.ColumnID = GetInt(r, 8); } catch {}
				
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat("{0} {1}{2}",
					column.Name,
					column.DataTypeName,
					(column.Length > 0) ? ("(" + column.Length + ")") : "");
				sb.AppendFormat(" {0}", column.NotNull ? "NOT NULL" : "NULL");
				//if (column.Default.Length > 0)
				//	sb.AppendFormat(" DEFAULT {0}", column.Default);
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
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection.");
			
			ArrayList collection = new ArrayList();
			
			OracleCommand command = new OracleCommand();
			command.Connection = connection;
			command.CommandText =
				"SELECT OWNER, VIEW_NAME, TEXT " +
				"FROM ALL_VIEWS " +
				"ORDER BY OWNER, VIEW_NAME";
			OracleDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				ViewSchema view = new ViewSchema();
				view.Provider = this;
				
				try {
					view.Name = r.GetString(1);
					view.SchemaName = r.GetString(0);
					view.OwnerName = r.GetString(0);
					view.Definition = r.GetString(2);
					view.IsSystemView = IsSystem (view.OwnerName);
					view.Comment = "";
				} catch (Exception e) {
				}
				
				collection.Add(view);
			}
			
			return (ViewSchema[]) collection.ToArray (typeof(ViewSchema));
		}
		
		/// <summary>
		/// Get a collection of columns within a view
		/// </summary>
		public override ColumnSchema[] GetViewColumns(ViewSchema view)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection.");
			
			ArrayList collection = new ArrayList();
			
			OracleCommand command = new OracleCommand ();
			command.Connection = connection;
			command.CommandText =
				"SELECT * " +
				" FROM " + view.Name +
				" WHERE 1 = 0";
			OracleDataReader r = command.ExecuteReader();

			for (int i = 0; i < r.FieldCount; i++) {
				ColumnSchema column = new ColumnSchema();
				
				column.Name = r.GetName(i);
				column.DataTypeName = r.GetDataTypeName(i);
				column.Default = "";
				column.Definition = "";
				column.OwnerName = view.OwnerName;
				column.SchemaName = view.OwnerName;
				
				collection.Add(column);
			}
			
			return (ColumnSchema[]) collection.ToArray (typeof(ColumnSchema));
		}
		
		/// <summary>
		/// Get a collection of constraints within a a table.
		/// </summary>
		public override ConstraintSchema[] GetTableConstraints (TableSchema table)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection.");
			
			ArrayList collection = new ArrayList ();
			
			OracleCommand command = new OracleCommand ();
			command.Connection = connection;
			command.CommandText = 
				"SELECT k.owner, k.table_name, k.constraint_name, " +
				"       k.constraint_type, k.status, k.validated " +
				"FROM all_constraints k " +
				"WHERE k.owner = '" + table.OwnerName + "' " +
				"AND k.table_name = '" + table.Name + "' " +
				"and k.constraint_type = 'P'";
			OracleDataReader r = command.ExecuteReader ();
			
			while (r.Read ()) {
				ConstraintSchema constraint = null;
				switch (r.GetString(4)) {
					case "P":
					default:
						constraint = new PrimaryKeyConstraintSchema();
						break;
				}
				
				constraint.Name = r.GetString (3);
				constraint.Definition = "";
				
				collection.Add (constraint);
			}
			
			return (ConstraintSchema[]) collection.ToArray (typeof(ConstraintSchema));
		}
	}
}
