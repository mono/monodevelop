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
using System.Data;
using FirebirdSql.Data.Firebird;
using System.Collections.Generic;
namespace MonoDevelop.Database.Sql
{
	// see:
	// http://firebird.sourceforge.net/index.php?op=devel&sub=netprovider&id=examples
	// http://www.alberton.info/firebird_sql_meta_info.html
	public class FirebirdSchemaProvider : AbstractSchemaProvider
	{
		public FirebirdSchemaProvider (IConnectionProvider connectionProvider)
			: base (connectionProvider)
		{
		}

		public override bool SupportsSchemaType (Type type)
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

		public override ICollection<TableSchema> GetTables ()
		{
			CheckConnectionState ();
			List<TableSchema> tables = new List<TableSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT RDB$RELATION_NAME, RDB$SYSTEM_FLAG, RDB$OWNER_NAME, RDB$DESCRIPTION FROM RDB$RELATIONS "+
				"WHERE RDB$VIEW_BLR IS NULL;"
			);

			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						TableSchema table = new TableSchema (this);
	
						table.Name = r.GetString (0);
						table.IsSystemTable = (!r.IsDBNull (1) && r.GetInt32 (1) != 0);
						table.OwnerName = r.GetString (2);
						table.Comment = r.GetString (3);
						
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
			return GetTableOrViewColumns (table.Name);
		}

		public override ICollection<ViewSchema> GetViews ()
		{
			CheckConnectionState ();
			List<ViewSchema> views = new List<ViewSchema> ();

			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT RDB$RELATION_NAME, RDB$SYSTEM_FLAG, RDB$OWNER_NAME, RDB$DESCRIPTION, RDB$VIEW_SOURCE FROM RDB$RELATIONS "+
				"WHERE RDB$VIEW_SOURCE IS NOT NULL;"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ViewSchema view = new ViewSchema (this);
	
						view.Name = r.GetString (0);
						view.IsSystemView = (!r.IsDBNull (1) && r.GetInt32 (1) != 0);
						view.OwnerName = r.GetString (2);
						view.Comment = r.GetString (3);
						//TODO: view.Definition = 4 (ascii blob)

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
			return GetTableOrViewColumns (view.Name);
		}

		public override ICollection<ProcedureSchema> GetProcedures ()
		{
			CheckConnectionState ();
			List<ProcedureSchema> procedures = new List<ProcedureSchema> ();

			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT RDB$PROCEDURE_NAME, RDB$SYSTEM_FLAG, RDB$OWNER_NAME, RDB$DESCRIPTION, RDB$PROCEDURE_SOURCE FROM RDB$PROCEDURES;"
			);
			
			using (command) {
			    	using (IDataReader r = command.ExecuteReader()) {
			    		while (r.Read ()) {
			    			ProcedureSchema procedure = new ProcedureSchema (this);
						
						procedure.Name = r.GetString (0);
						procedure.IsSystemProcedure = (!r.IsDBNull (1) && r.GetInt32 (1) != 0);
						procedure.OwnerName = r.GetString (2);
						procedure.Comment = r.GetString (3);
						//TODO: procedure.Definition = 4 (ascii blob)
			    			
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
				"SELECT RDB$PARAMETER_NAME, RDB$PARAMETER_NUMBER, RDB$DESCRIPTION, RDB$SYSTEM_FLAG "
				+ "FROM RDB$PROCEDURE_PARAMETERS "
				+ "WHERE RDB$PROCEDURE_NAME = '" + procedure.Name + "' "
				+ "AND RDB$PARAMETER_TYPE = 1 "
				+ "ORDER BY 2;"
			);
			
			using (command) {
			    	using (IDataReader r = command.ExecuteReader()) {
			    		while (r.Read ()) {
						ColumnSchema column = new ColumnSchema (this);
			    			
						column.Name = r.GetString (0);
			    			column.OwnerName = procedure.Name;
						column.Comment = r.GetString (2);
						//TODO: data type
			    				
			    			columns.Add (column);
			    		}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}
			
			return columns;
		}

		public override ICollection<ParameterSchema> GetProcedureParameters (ProcedureSchema procedure)
		{
			CheckConnectionState ();
			List<ParameterSchema> parameters = new List<ParameterSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT RDB$PARAMETER_NAME, RDB$PARAMETER_NUMBER, RDB$DESCRIPTION, RDB$SYSTEM_FLAG "
				+ "FROM RDB$PROCEDURE_PARAMETERS "
				+ "WHERE RDB$PROCEDURE_NAME = '" + procedure.Name + "' "
				+ "AND RDB$PARAMETER_TYPE = 0 "
				+ "ORDER BY 2;"
			);
			
			using (command) {
			    	using (IDataReader r = command.ExecuteReader()) {
			    		while (r.Read ()) {
						ParameterSchema parameter = new ParameterSchema (this);
			    			
						parameter.Name = r.GetString (0);
			    			parameter.OwnerName = procedure.Name;
						parameter.Comment = r.GetString (2);
						//TODO: data type
			    				
			    			parameters.Add (parameter);
			    		}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}
			
			return parameters;
		}

		//TODO:
		public override ICollection<ConstraintSchema> GetTableConstraints (TableSchema table)
		{
			CheckConnectionState ();
			List<ConstraintSchema> constraints = new List<ConstraintSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand ("SHOW TABLE STATUS FROM `" + table.OwnerName + "`;");
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
//					ConstraintSchema constraint = new ConstraintSchema (this);
//					constraint.PrimaryKey = pkColumn;
//					constraint.ForeignKey = fkColumn;
//							
//					constraints.Add (constraint);
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return constraints;
		}

		public override ICollection<UserSchema> GetUsers ()
		{
			CheckConnectionState ();
			List<UserSchema> users = new List<UserSchema> ();

			IDbCommand command = connectionProvider.CreateCommand ("SELECT DISTINCT RDB$USER FROM RDB$USER_PRIVILEGES;");
			using (command) {
				using (IDataReader r = command.ExecuteReader ()) {
					while (r.Read ()) {
						UserSchema user = new UserSchema (this);
						user.Name = r.GetString (0);
						users.Add (user);
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return users;
		}
		
		public override ICollection<TriggerSchema> GetTriggers ()
		{
			CheckConnectionState ();
			List<TriggerSchema> triggers = new List<TriggerSchema> ();

			IDbCommand command = connectionProvider.CreateCommand ("SELECT RDB$TRIGGER_NAME, RDB$RELATION_NAME, RDB$TRIGGER_TYPE FROM RDB$TRIGGERS;");
			using (command) {
				using (IDataReader r = command.ExecuteReader ()) {
					while (r.Read ()) {
						TriggerSchema trigger = new TriggerSchema (this);
						trigger.Name = r.GetString (0);
						//TODO: table and type
						triggers.Add (trigger);
					}
					r.Close ();
				}
				connectionProvider.Close (command.Connection);
			}

			return triggers;
		}
		
		//TODO:
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
						
		private ICollection<ColumnSchema> GetTableOrViewColumns (string name)
		{
			CheckConnectionState ();
			List<ColumnSchema> columns = new List<ColumnSchema> ();
			
			IDbCommand command = connectionProvider.CreateCommand (
				"SELECT r.RDB$FIELD_NAME AS field_name, r.RDB$DESCRIPTION AS field_description, "
				+ "r.RDB$DEFAULT_VALUE AS field_default_value, r.RDB$NULL_FLAG AS field_not_null_constraint, "
				+ "f.RDB$FIELD_LENGTH AS field_length, f.RDB$FIELD_PRECISION AS field_precision, "
				+ "f.RDB$FIELD_SCALE AS field_scale, "
				+ "CASE f.RDB$FIELD_TYPE WHEN 261 THEN 'BLOB' WHEN 14 THEN 'CHAR' WHEN 40 THEN 'CSTRING' "
				+ "WHEN 11 THEN 'D_FLOAT' WHEN 27 THEN 'DOUBLE' WHEN 10 THEN 'FLOAT' WHEN 16 THEN 'INT64' "
				+ "WHEN 8 THEN 'INTEGER' WHEN 9 THEN 'QUAD' WHEN 7 THEN 'SMALLINT' WHEN 12 THEN 'DATE' "
				+ "WHEN 13 THEN 'TIME' WHEN 35 THEN 'TIMESTAMP' WHEN 37 THEN 'VARCHAR' ELSE 'UNKNOWN' "
				+ "END AS field_type, f.RDB$FIELD_SUB_TYPE AS field_subtype, "
				+ "coll.RDB$COLLATION_NAME AS field_collation, cset.RDB$CHARACTER_SET_NAME AS field_charset "
				+ "FROM RDB$RELATION_FIELDS r LEFT JOIN RDB$FIELDS f ON r.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME "
				+ "LEFT JOIN RDB$COLLATIONS coll ON f.RDB$COLLATION_ID = coll.RDB$COLLATION_ID "
				+ "LEFT JOIN RDB$CHARACTER_SETS cset ON f.RDB$CHARACTER_SET_ID = cset.RDB$CHARACTER_SET_ID "
				+ "WHERE r.RDB$RELATION_NAME='" + name + "' ORDER BY r.RDB$FIELD_POSITION;"
			);
			using (command) {
				using (IDataReader r = command.ExecuteReader()) {
					while (r.Read ()) {
						ColumnSchema column = new ColumnSchema (this);
		
						column.Name = r.GetString (0);
						column.DataTypeName = r.GetString (8);
						column.NotNull = (!r.IsDBNull (3) && r.GetInt32 (3) == 1);
						column.Default = r.GetString (2);
						column.Comment = r.GetString (1);
						column.OwnerName = name;
						column.Length = r.GetInt32 (4);
						column.Precision = r.GetInt32 (5);
						column.Scale = r.GetInt32 (6);
		
						columns.Add (column);
					}
					r.Close ();
				};
				connectionProvider.Close (command.Connection);
			}

			return columns;
		}
	}
}
