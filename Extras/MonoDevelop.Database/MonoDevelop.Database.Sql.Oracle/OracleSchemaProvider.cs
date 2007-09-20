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
using System.Text;
using System.Data;
using System.Data.OracleClient;
using System.Collections.Generic;
namespace MonoDevelop.Database.Sql
{
	public class OracleSchemaProvider : AbstractSchemaProvider
	{
		public OracleSchemaProvider (IConnectionProvider connectionProvider)
			: base (connectionProvider)
		{
		}
		
		public override bool SupportsSchemaType (Type type)
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

		public override ICollection<TableSchema> GetTables ()
		{
			CheckConnectionState ();
			List<TableSchema> tables = new List<TableSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT OWNER, TABLE_NAME, TABLESPACE_NAME " +
				"FROM ALL_TABLES " +
				"ORDER BY OWNER, TABLE_NAME"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						TableSchema table = new TableSchema (this);
	
						table.OwnerName = r.GetValue (0).ToString();
						table.SchemaName = r.GetValue (0).ToString();
						table.Name = r.GetString (1).ToString();
						table.IsSystemTable = IsSystem (table.OwnerName);
						table.TableSpaceName = r.GetValue (2).ToString();
						
						StringBuilder sb = new StringBuilder();
						sb.AppendFormat ("-- Table: {0}\n", table.Name);
						sb.AppendFormat ("-- DROP TABLE {0};\n\n", table.Name);
						sb.AppendFormat ("CREATE TABLE {0} (\n", table.Name);
						
						ICollection<ColumnSchema> columns = table.Columns;
						string[] parts = new string[columns.Count];
						int i = 0;
						foreach (ColumnSchema col in columns)
							parts[i++] = col.Definition;
						sb.Append (String.Join (",\n", parts));
						
						ICollection<ConstraintSchema> constraints = table.Constraints;
						parts = new string[constraints.Count];
						if (constraints.Count > 0)
							sb.Append (",\n");
						i = 0;
						foreach (ConstraintSchema constr in constraints)
							parts[i++] = "\t" + constr.Definition;
						sb.Append (String.Join (",\n", parts));
						
						//sb.AppendFormat ("\n) COMMENT '{0}';", table.Comment);
						table.Definition = sb.ToString ();
						
						tables.Add (table);
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return tables;
		}
		
		public override ICollection<ColumnSchema> GetTableColumns (TableSchema table)
		{
			CheckConnectionState ();
			List<ColumnSchema> columns = new List<ColumnSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT OWNER, TABLE_NAME, COLUMN_NAME, " +
				"       DATA_TYPE, DATA_LENGTH, DATA_PRECISION, DATA_SCALE, " +
				"       NULLABLE, COLUMN_ID, DEFAULT_LENGTH, DATA_DEFAULT " +
				"FROM ALL_TAB_COLUMNS " +
				"WHERE OWNER = '" + table.OwnerName + "' " + 
				"AND TABLE_NAME = '" + table.Name + "' " +
				"ORDER BY OWNER, TABLE_NAME, COLUMN_ID"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ColumnSchema column = new ColumnSchema (this);
		
						column.Name = GetCheckedString (r, 2);
						column.DataTypeName = GetCheckedString (r, 3);
						column.OwnerName = table.OwnerName;
						column.SchemaName = table.OwnerName;
						column.NotNull = GetCheckedString (r, 7) == "Y";
						column.Length = GetCheckedInt32 (r, 4);
						column.Precision = GetCheckedInt32 (r, 5);
						column.Scale = GetCheckedInt32 (r, 6);
						column.ColumnID = GetCheckedInt32 (r, 8);
						
						StringBuilder sb = new StringBuilder();
						sb.AppendFormat("{0} {1}{2}",
							column.Name,
							column.DataTypeName,
							(column.Length > 0) ? ("(" + column.Length + ")") : "");
						sb.AppendFormat(" {0}", column.NotNull ? "NOT NULL" : "NULL");
						//if (column.Default.Length > 0)
						//	sb.AppendFormat(" DEFAULT {0}", column.Default);
						column.Definition = sb.ToString();
		
						columns.Add (column);
					}
					r.Close ();
				};
				connectionProvider.Close (command.Connection);
			}

			return columns;
		}

		public override ICollection<ViewSchema> GetViews ()
		{
			CheckConnectionState ();
			List<ViewSchema> views = new List<ViewSchema> ();

			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT OWNER, VIEW_NAME, TEXT " +
				"FROM ALL_VIEWS " +
				"ORDER BY OWNER, VIEW_NAME"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ViewSchema view = new ViewSchema (this);
	
						view.Name = r.GetString(1);
						view.SchemaName = r.GetString (0);
						view.OwnerName = r.GetString (0);
						view.Definition = r.GetString (2);
						view.IsSystemView = IsSystem (view.OwnerName);
						
						views.Add (view);
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}
			return views;
		}

		public override ICollection<ColumnSchema> GetViewColumns (ViewSchema view)
		{
			CheckConnectionState ();
			List<ColumnSchema> columns = new List<ColumnSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT * " +
				" FROM " + view.Name +
				" WHERE 1 = 0"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						for (int i = 0; i < r.FieldCount; i++) {
							ColumnSchema column = new ColumnSchema (this);
							
							column.Name = r.GetString (0);
							column.DataTypeName = r.GetString (1);
							column.SchemaName = view.SchemaName;
							column.NotNull = r.GetBoolean (3);
							column.Length = r.GetInt32 (2);
							
							columns.Add (column);
						}
					}
					r.Close ();
				};
				connectionProvider.Close (command.Connection);
			}

			return columns;
		}

		public override ICollection<ConstraintSchema> GetTableConstraints (TableSchema table)
		{
			CheckConnectionState ();
			List<ConstraintSchema> constraints = new List<ConstraintSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT k.owner, k.table_name, k.constraint_name, " +
				"       k.constraint_type, k.status, k.validated " +
				"FROM all_constraints k " +
				"WHERE k.owner = '" + table.OwnerName + "' " +
				"AND k.table_name = '" + table.Name + "' " +
				"and k.constraint_type = 'P'"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ConstraintSchema constraint = null;
										
						switch (r.GetString(4)) {
							case "P":
							default:
								constraint = new PrimaryKeyConstraintSchema (this);
								break;
						}
						
						constraint.Name = r.GetString (3);
						constraint.Definition = "";
						
						constraints.Add (constraint);
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return constraints;
		}

		public override DataTypeSchema GetDataType (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			name = name.ToUpper ();

			DataTypeSchema dts = new DataTypeSchema (this);
			dts.Name = name;
			switch (name) {
					//TODO: IMPLEMENT
				case "":
					break;
				default:
					dts = null;
					break;
			}
			
			return dts;
		}
		
		private bool IsSystem (string owner) 
		{
			switch (owner) {
				case "SYSTEM":
				case "SYS":
				case "DRSYS":
				case "CTXSYS":
				case "MDSYS":
				case "WKSYS":
					return true;
				default:
					return false;
			}
		}
	}
}
