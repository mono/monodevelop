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
using System.Collections.Generic;
using Mono.Data.SybaseClient;
namespace MonoDevelop.Database.Sql
{
	public class SybaseSchemaProvider : AbstractSchemaProvider
	{
		public SybaseSchemaProvider (IConnectionProvider connectionProvider)
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
				"SELECT su.name AS owner, so.name as table_name, so.id as table_id, " +
				" so.crdate as created_date, so.type as table_type " +
				"FROM dbo.sysobjects so, dbo.sysusers su " +
				"WHERE type IN ('S','U') " +
				"AND su.uid = so.uid " +
				"ORDER BY 1, 2"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						TableSchema table = new TableSchema (this);
	
						table.Name = r.GetString(1);
						table.IsSystemTable = r.GetString (4) == "S" ? true : false;
						
						table.SchemaName = r.GetString (0);
						table.OwnerName = r.GetString (0);
						table.Comment = "";
						
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
						
						sb.Append ("\n);\n");
						//sb.AppendFormat ("COMMENT ON TABLE {0} IS '{1}';", table.Name, table.Comment);
						table.Definition = sb.ToString();
						
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
				"select su.name as owner, so.name as table_name, sc.name as column_name,  " +
				" st.name as date_type, sc.length as column_length,  " +
				" sc.prec as data_preceision, sc.scale as data_scale, " +
				" 0 as isnullable, sc.colid as column_id " +
				"from dbo.syscolumns sc, dbo.sysobjects so, " +
				"     dbo.systypes st, dbo.sysusers su " +
				"where sc.id = so.id " +
				"and so.type in ('U','S') " +
				"and so.name = '" + table.Name + "' " + 
				"and su.name = '" + table.OwnerName + "' " + 
				"and su.uid = so.uid " +
				"and sc.usertype = st.usertype " +
				"order by sc.colid"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ColumnSchema column = new ColumnSchema (this);
						
						column.Name = r.GetString (2);
						column.DataTypeName = r.GetString (3);
						column.OwnerName = table.OwnerName;
						column.NotNull = r.GetInt32 (7) == 8 ? true : false;
						column.Length = GetCheckedInt32 (r, 4);
						column.Precision = GetCheckedInt32 (r, 5);
						column.Scale = GetCheckedInt32 (r, 6);

						StringBuilder sb = new StringBuilder ();
						sb.AppendFormat ("{0} {1}{2}",
							column.Name,
							column.DataTypeName,
							(column.Length > 0) ? ("(" + column.Length + ")") : "");
						sb.AppendFormat (" {0}", column.NotNull ? "NOT NULL" : "NULL");
						if (column.Default.ToString ().Length > 0)
							sb.AppendFormat (" DEFAULT {0}", column.Default.ToString ());
						column.Definition = sb.ToString ();
		
						columns.Add (column);
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return columns;
		}

		public override ICollection<ViewSchema> GetViews ()
		{
			CheckConnectionState ();
			List<ViewSchema> views = new List<ViewSchema> ();

			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT su.name AS owner, so.name as table_name, so.id as table_id, " +
				" so.crdate as created_date, so.type as table_type " +
				"FROM dbo.sysobjects so, dbo.sysusers su " +
				"WHERE type = 'V' " +
				"AND su.uid = so.uid " +
				"ORDER BY 1, 2"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ViewSchema view = new ViewSchema (this);
	
						view.Name = r.GetString (1);
						view.SchemaName = r.GetString (0);
						view.OwnerName = r.GetString (0);
						view.IsSystemView = r.GetString (4).Trim ().Equals ("S");
						
						StringBuilder sb = new StringBuilder ();
						sb.AppendFormat ("-- View: {0}\n", view.Name);
						sb.AppendFormat ("-- DROP VIEW {0};\n\n", view.Name);
						string source = GetSource (view.Owner + "." + view.Name);
						sb.AppendFormat ("  {0}\n);", source);
						view.Definition = sb.ToString ();
						//view.Comment = r.GetString(5);
						
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

							column.Name = r.GetName (i);
							column.DataTypeName = r.GetDataTypeName (i);
							column.OwnerName = view.OwnerName;
							column.SchemaName = view.OwnerName;
			
							columns.Add (column);
						}
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return columns;
		}

		public override ICollection<ProcedureSchema> GetProcedures ()
		{
			CheckConnectionState ();
			List<ProcedureSchema> procedures = new List<ProcedureSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT su.name AS owner, so.name as proc_name, so.id as proc_id, "
				+ "so.crdate as created_date, so.type as proc_type " 
				+ "FROM dbo.sysobjects so, dbo.sysusers su " 
				+ "WHERE type = 'P' AND su.uid = so.uid " 
				+ "ORDER BY 1, 2"
			);
			
			using (command) {
			    	using (IDataReader r = command.ExecuteReader()) {
			    		while (r.Read ()) {
			    			ProcedureSchema procedure = new ProcedureSchema (this);

						procedure.Name = r.GetString (1);
						procedure.OwnerName = r.GetString (0);
						procedure.IsSystemProcedure = r.GetString (4).Trim ().Equals ("S");
						
						StringBuilder sb = new StringBuilder ();
						sb.AppendFormat ("-- Procedure: {0}\n", procedure.Name);
						sb.AppendFormat ("-- DROP PROCEDURE {0};\n\n", procedure.Name);
						string source = GetSource (procedure.Owner + "." + procedure.Name);
						sb.AppendFormat ("  {0}\n);", source);
						procedure.Definition = sb.ToString ();
			    			
			    			procedures.Add (procedure);
			    		}
			    		r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}
			
			return procedures;
		}

		public override ICollection<ColumnSchema> GetProcedureColumns (ProcedureSchema procedure)
		{
			CheckConnectionState ();
			List<ColumnSchema> columns = new List<ColumnSchema> ();
			
			
			IDbCommand command = connectionProvider.CreateCommand (
				"sp_sproc_columns"
			);
			SybaseParameter owner = new SybaseParameter ("@procedure_owner", procedure.OwnerName);
			SybaseParameter name = new SybaseParameter ("@procedure_name", procedure.Name);
			command.Parameters.Add (owner);
			command.Parameters.Add (name);
			
			using (command) {
			    	using (IDataReader r = command.ExecuteReader()) {
			    		while (r.Read ()) {	
						ColumnSchema column = new ColumnSchema (this);
						column.Name = (string)r["COLUMN_NAME"];
						column.DataTypeName = (string)r["TYPE_NAME"];
						columns.Add (column);
			    		}
			    		r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}
			
			return columns;
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
	    
		protected string GetSource (string objectName) 
		{
			CheckConnectionState ();

			IDbCommand command = connectionProvider.CreateCommand (
				String.Format ("EXEC [master].[dbo].[sp_helptext] '{0}', null", objectName)
			);
			StringBuilder sb = new StringBuilder ();
			using (command) {
				using (IDataReader r = command.ExecuteReader ()) {
					while (r.Read ())
						sb.Append (r.GetString (0));
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return sb.ToString ();
		}
	}
}
