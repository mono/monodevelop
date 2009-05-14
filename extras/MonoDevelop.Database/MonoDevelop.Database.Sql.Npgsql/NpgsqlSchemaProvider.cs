//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Npgsql;
using MonoDevelop.Core;
namespace MonoDevelop.Database.Sql.Npgsql
{
	public class NpgsqlSchemaProvider : AbstractEditSchemaProvider
	{
		public NpgsqlSchemaProvider (IConnectionPool connectionPool)
			: base (connectionPool)
		{
			AddSupportedSchemaActions (SchemaType.Database, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.Table, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.View, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.Procedure, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.TableColumn, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.ProcedureParameter, SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.Trigger, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.PrimaryKeyConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.ForeignKeyConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.CheckConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.UniqueConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.Constraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.User, SchemaActions.Schema);
		}

		public override TableSchemaCollection GetTables ()
		{
			TableSchemaCollection tables = new TableSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT DISTINCT c.relname, n.nspname, u.usename "
				+ "FROM pg_class c, pg_namespace n, pg_user u "
				+ "WHERE c.relnamespace = n.oid "
				+ "AND c.relowner = u.usesysid "
				+ "AND c.relkind='r' AND NOT EXISTS "
				+ "   (SELECT 1 FROM pg_rewrite r "
				+ "      WHERE r.ev_class = c.oid AND r.ev_type = '1') "
				+ "ORDER BY relname;"
			);		
			
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							TableSchema table = new TableSchema (this);
		
							table.Name = r.GetString (0);
							table.IsSystemTable = table.Name.StartsWith ("pg_") || table.Name.StartsWith ("sql_");
							table.SchemaName = r.GetString (1);
							table.OwnerName = r.GetString (2);
							
//							StringBuilder sb = new StringBuilder();
//							sb.AppendFormat ("-- Table: {0}\n", table.Name);
//							sb.AppendFormat ("-- DROP TABLE {0};\n\n", table.Name);
//							sb.AppendFormat ("CREATE TABLE {0} (\n", table.Name);
//							
//							ColumnSchema[] columns = table.Columns;
//							string[] parts = new string[columns.Length];
//							for (int i = 0; i < parts.Length; i++) {
//								parts[i] = "\t" + columns[i].Definition;
//							}
//							sb.Append (String.Join (",\n", parts));
//							
//							ConstraintSchema[] cons = table.Constraints;
//							parts = new string[cons.Length];
//							if (cons.Length > 0)
//								sb.Append (",\n");
//							for (int i = 0; i < parts.Length; i++) {
//								parts[i] = "\t" + cons[i].Definition;
//							}
//							sb.Append (String.Join (",\n", parts));
//							
//							sb.Append ("\n);\n");
//							sb.AppendFormat ("COMMENT ON TABLE {0} IS '{1}';", table.Name, table.Comment);
//							table.Definition = sb.ToString();
							
							tables.Add (table);
						}
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return tables;
		}
		
		public override ColumnSchemaCollection GetTableColumns (TableSchema table)
		{
			ColumnSchemaCollection columns = new ColumnSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT a.attname, a.attnotnull, a.attlen, "
				+ "typ.typname, adef.adsrc "
				+ "FROM "
				+ "  pg_catalog.pg_attribute a LEFT JOIN "
				+ "  pg_catalog.pg_attrdef adef "
				+ "  ON a.attrelid=adef.adrelid "
				+ "  AND a.attnum=adef.adnum "
				+ "  LEFT JOIN pg_catalog.pg_type t ON a.atttypid=t.oid, "
				+ "  pg_catalog.pg_type typ "
				+ "WHERE "
				+ "  a.attrelid = (SELECT oid FROM pg_catalog.pg_class "
				+ "  WHERE relname='" + table.Name + "') "
				+ "AND a.attnum > 0 AND NOT a.attisdropped "
				+ "AND a.atttypid = typ.oid "
				+ "ORDER BY a.attnum;"
			);
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ColumnSchema column = new ColumnSchema (this, table);
			
							column.Name = r.GetString (0);
							column.DataTypeName = r.GetString (3);
							column.IsNullable = r.GetBoolean (1);
							column.DefaultValue = r.IsDBNull (4) ? null : r.GetString (4);
							// column.DataType.LengthRange.Default = (int)r.GetValue (2);
					
//							StringBuilder sb = new StringBuilder();
//							sb.AppendFormat("{0} {1}{2}",
//								column.Name,
//								column.DataTypeName,
//								(column.DataType.LengthRange.Default > 0) ? ("(" + column.DataType.LengthRange.Default + ")") : "");
//							sb.AppendFormat(" {0}", column.IsNullable ? "NULL" : "NOT NULL");
//							if (column.DefaultValue.Length > 0)
//								sb.AppendFormat(" DEFAULT {0}", column.DefaultValue);
//							column.Definition = sb.ToString();
			
							columns.Add (column);
						}
						r.Close ();
					};
				}
			} catch (NpgsqlException ex) {
				// Don't raise error, if the table doesn't exists return an empty collection
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return columns;
		}

		public override ViewSchemaCollection GetViews ()
		{
			ViewSchemaCollection views = new ViewSchemaCollection ();

			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT v.schemaname, v.viewname, v.viewowner, v.definition,"
				+ " (c.oid <= " + LastSystemOID + "), "
				+ "(SELECT description from pg_description pd, "
				+ " pg_class pc WHERE pc.oid=pd.objoid AND pc.relname="
				+ " v.viewname) "
				+ "FROM pg_views v, pg_class c "
				+ "WHERE v.viewname = c.relname "
				+ "ORDER BY viewname"
			);

			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ViewSchema view = new ViewSchema (this);
		
							view.Name = r.GetString (1);
							view.OwnerName = r.GetString (2);
							view.SchemaName = r.GetString (0);
							view.IsSystemView = r.GetBoolean (4);
							view.Comment = r.IsDBNull (5) ? null : r.GetString (5);
							
//							StringBuilder sb = new StringBuilder();
//							sb.AppendFormat ("-- View: {0}\n", view.Name);
//							sb.AppendFormat ("-- DROP VIEW {0};\n\n", view.Name);
//							sb.AppendFormat ("CREATE VIEW {0} AS (\n", view.Name);
//							string core = r.GetString(3);
//							sb.AppendFormat ("  {0}\n);", core.Substring (0, core.Length-1));
//							view.Definition = sb.ToString ();
							
							views.Add (view);
						}
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return views;
		}

		public override ColumnSchemaCollection GetViewColumns (ViewSchema view)
		{
			ColumnSchemaCollection columns = new ColumnSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT attname, typname, attlen, attnotnull "
				+ "FROM "
				+ "  pg_catalog.pg_attribute a LEFT JOIN pg_catalog.pg_attrdef adef "
				+ "  ON a.attrelid=adef.adrelid "
				+ "  AND a.attnum=adef.adnum "
				+ "  LEFT JOIN pg_catalog.pg_type t ON a.atttypid=t.oid "
				+ "WHERE "
				+ "  a.attrelid = (SELECT oid FROM pg_catalog.pg_class WHERE relname='"
				+ view.Name + "') "
				+ "  AND a.attnum > 0 AND NOT a.attisdropped "
				+ "     ORDER BY a.attnum;"
			);
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ColumnSchema column = new ColumnSchema (this, view);

							column.Name = r.GetString(0);
							column.DataTypeName = r.GetString (1);
							column.SchemaName = view.SchemaName;
							column.IsNullable = r.GetBoolean (3);
							column.DataType.LengthRange.Default = r.GetInt32 (2);
			
							columns.Add (column);
						}
						r.Close ();
					};
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return columns;
		}

		public override ProcedureSchemaCollection GetProcedures ()
		{
			ProcedureSchemaCollection procedures = new ProcedureSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT pc.proname, pc.oid::integer, pl.lanname, pc.prosrc "
				+ "FROM "
				+ " pg_proc pc, "
				+ " pg_user pu, "
				+ " pg_type pt, "
				+ " pg_language pl "
				+ "WHERE pc.proowner = pu.usesysid "
				+ "AND pc.prorettype = pt.oid "
				+ "AND pc.prolang = pl.oid "
				+ "UNION "
				+ "SELECT pc.proname, pt.oid::integer, pl.lanname, pc.prosrc "
				+ "FROM "
				+ " pg_proc pc, "
				+ " pg_user pu, "
				+ " pg_type pt, "
				+ " pg_language pl "
				+ "WHERE pc.proowner = pu.usesysid "
				+ "AND pc.prorettype = 0 "
				+ "AND pc.prolang = pl.oid;"
			);
			try {
				using (command) {
				    	using (IDataReader r = command.ExecuteReader()) {
				    		while (r.Read ()) {
				    			ProcedureSchema procedure = new ProcedureSchema (this);
							
							procedure.Name = r.GetString (0);
							procedure.Definition = r.GetString (3);
							procedure.LanguageName = r.GetString (2);
							
							if (!r.IsDBNull (1) && r.GetInt32 (1) <= LastSystemOID)
								procedure.IsSystemProcedure = true;
				    			
				    			procedures.Add (procedure);
				    		}
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return procedures;
		}
		
		public override ParameterSchemaCollection GetProcedureParameters (ProcedureSchema procedure)
		{
			ParameterSchemaCollection parameters = new ParameterSchemaCollection ();
			
			// FIXME: Won't work properly with overload functions.
			// Maybe check the number of columns in the parameters for
			// proper match.
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (String.Format (
				"SELECT format_type (prorettype, NULL) "
				+ "FROM pg_proc pc, pg_language pl "
				+ "WHERE pc.prolang = pl.oid "
				+ "AND pc.proname = '{0}';", procedure.Name
			));
			try {
			using (command) {
			    	using (IDataReader r = command.ExecuteReader()) {
			    		while (r.Read ()) {	
						ParameterSchema param = new ParameterSchema (this);

						param.DataTypeName = r.GetString (0);
						param.Name = r.GetString (0);
						parameters.Add (param);
			    		}
					r.Close ();
				}
			}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return parameters;
		}

		public override ConstraintSchemaCollection GetTableConstraints (TableSchema table)
		{
			ConstraintSchemaCollection constraints = new ConstraintSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (String.Format (
				"SELECT "
				+ "pc.conname, "
				+ "pg_catalog.pg_get_constraintdef(pc.oid, true) AS consrc, "
				+ "pc.contype, "
				+ "CASE WHEN pc.contype='u' OR pc.contype='p' THEN ( "
				+ "	SELECT "
				+ "		indisclustered "
				+ "	FROM "
				+ "		pg_catalog.pg_depend pd, "
				+ "		pg_catalog.pg_class pl, "
				+ "		pg_catalog.pg_index pi "
				+ "	WHERE "
				+ "		pd.refclassid=pc.tableoid "
				+ "		AND pd.refobjid=pc.oid "
				+ "		AND pd.objid=pl.oid "
				+ "		AND pl.oid=pi.indexrelid "
				+ ") ELSE "
				+ "	NULL "
				+ "END AS indisclustered "
				+ "FROM "
				+ "pg_catalog.pg_constraint pc "
				+ "WHERE "
				+ "pc.conrelid = (SELECT oid FROM pg_catalog.pg_class WHERE relname='{0}' "
				+ "	AND relnamespace = (SELECT oid FROM pg_catalog.pg_namespace "
				+ "	WHERE nspname='{1}')) "
				+ "ORDER BY "
				+ "1;", table.Name, table.SchemaName
			));
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {	
							ConstraintSchema constraint = null;
											
							//TODO: Add support for Check constraints.
							switch (r.GetString (2)) {
								case "f":
									string match = @".*REFERENCES (.+)\(.*\).*";
									constraint = new ForeignKeyConstraintSchema (this);
									if (Regex.IsMatch (r.GetString (1), match))
										(constraint as ForeignKeyConstraintSchema).ReferenceTableName
											= Regex.Match (r.GetString (1), match).Groups[0].Captures[0].Value;
									break;
								case "u":
									constraint = new UniqueConstraintSchema (this);
									break;
								case "p":
								default:
									constraint = new PrimaryKeyConstraintSchema (this);
									break;
							}
						
							constraint.Name = r.GetString (0);
							constraint.Definition = r.GetString (1);
							
							int parenOpen = constraint.Definition.IndexOf ('(');
							if (parenOpen > 0) {
								int parenClose = constraint.Definition.IndexOf (')');
								string colstr = constraint.Definition.Substring (parenOpen + 1, parenClose - parenOpen - 1);
								foreach (string col in colstr.Split (',')) {
									ColumnSchema column = new ColumnSchema (this, table);
									column.Name = col.Trim ();
									constraint.Columns.Add (column);
								}
							}
							
							constraints.Add (constraint);
						}
						r.Close ();
					}
				}
			} catch (Exception e) {
				// Don't raise error, if the table doesn't exists return an empty collection
			}
			conn.Release ();

			return constraints;
		}

		public override UserSchemaCollection GetUsers ()
		{
			UserSchemaCollection users = new UserSchemaCollection ();

			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand ("SELECT * FROM pg_user;");

			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader ()) {
						while (r.Read ()) {
							UserSchema user = new UserSchema (this);
							
							user.Name = r.GetString (0);
							user.UserId = String.Format ("{0}", r.GetValue (1));
							user.Expires = r.IsDBNull (6) ? DateTime.MinValue : r.GetDateTime (6);
							//user.Options["createdb"] = r.GetBoolean (2);
							//user.Options["createuser"] = r.GetBoolean (3);
							user.Password = r.GetString (5);
							
							StringBuilder sb = new StringBuilder ();
							sb.AppendFormat ("-- User: \"{0}\"\n\n", user.Name);
							sb.AppendFormat ("-- DROP USER {0};\n\n", user.Name);
							sb.AppendFormat ("CREATE USER {0}", user.Name);
							sb.AppendFormat ("  WITH SYSID {0}", user.UserId);
							if (user.Password != "********")
								sb.AppendFormat (" ENCRYPTED PASSWORD {0}", user.Password);
							//sb.AppendFormat (((bool) user.Options["createdb"]) ?
							//	" CREATEDB" : " NOCREATEDB");
							//sb.AppendFormat (((bool) user.Options["createuser"]) ?
							//	" CREATEUSER" : " NOCREATEUSER");
							if (user.Expires != DateTime.MinValue)
								sb.AppendFormat (" VALID UNTIL {0}", user.Expires);
							sb.Append (";");
							user.Definition = sb.ToString ();
		
							users.Add (user);
						}
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return users;
		}
		
		public override DataTypeSchemaCollection GetDataTypes ()
		{
			DataTypeSchemaCollection collection = new DataTypeSchemaCollection ();
			
			#region Types
			// ENUM
			DataTypeSchema schema = new DataTypeSchema (this);
			schema.Name = "smallint";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(Int16);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Integer
			schema = new DataTypeSchema (this);
			schema.Name = "integer";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(int);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Big Int
			schema = new DataTypeSchema (this);
			schema.Name = "bigint";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(long);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Serial
			schema = new DataTypeSchema (this);
			schema.Name = "serial";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(long);
			schema.IsAutoincrementable = true;
			schema.IsFixedLength = true;
			schema.IsNullable = false;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Big Serial
			schema = new DataTypeSchema (this);
			schema.Name = "bigserial";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(long);
			schema.IsAutoincrementable = true;
			schema.IsFixedLength = true;
			schema.IsNullable = false;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Numeric
			schema = new DataTypeSchema (this);
			schema.Name = "numeric";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(float);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Decimal
			schema = new DataTypeSchema (this);
			schema.Name = "decimal";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(float);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// real
			schema = new DataTypeSchema (this);
			schema.Name = "real";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(float);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// double precision
			schema = new DataTypeSchema (this);
			schema.Name = "double precision";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(float);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// money
			schema = new DataTypeSchema (this);
			schema.Name = "money";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(float);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// character varying
			schema = new DataTypeSchema (this);
			schema.Name = "character varying";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// varying
			schema = new DataTypeSchema (this);
			schema.Name = "varying";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// varchar
			schema = new DataTypeSchema (this);
			schema.Name = "varchar";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// text
			schema = new DataTypeSchema (this);
			schema.Name = "text";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// character
			schema = new DataTypeSchema (this);
			schema.Name = "character";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// char
			schema = new DataTypeSchema (this);
			schema.Name = "char";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// bytea
			schema = new DataTypeSchema (this);
			schema.Name = "bytea";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(byte);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);			

			// timeSpan
			schema = new DataTypeSchema (this);
			schema.Name = "timespan";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(object);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);

			// interval
			schema = new DataTypeSchema (this);
			schema.Name = "interval";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(TimeSpan);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Date
			schema = new DataTypeSchema (this);
			schema.Name = "date";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(DateTime);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// Time
			schema = new DataTypeSchema (this);
			schema.Name = "time";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(DateTime);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// boolean
			schema = new DataTypeSchema (this);
			schema.Name = "boolean";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(bool);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// bit
			schema = new DataTypeSchema (this);
			schema.Name = "bit";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(bool);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// uuid
			schema = new DataTypeSchema (this);
			schema.Name = "bit";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(Guid);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// xml
			schema = new DataTypeSchema (this);
			schema.Name = "xml";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(Guid);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// point
			schema = new DataTypeSchema (this);
			schema.Name = "point";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// line
			schema = new DataTypeSchema (this);
			schema.Name = "line";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// lseg
			schema = new DataTypeSchema (this);
			schema.Name = "lseg";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// box
			schema = new DataTypeSchema (this);
			schema.Name = "box";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// polygon
			schema = new DataTypeSchema (this);
			schema.Name = "polygon";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// circle
			schema = new DataTypeSchema (this);
			schema.Name = "circle";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			// inet
			schema = new DataTypeSchema (this);
			schema.Name = "inet";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			
			// cidr
			schema = new DataTypeSchema (this);
			schema.Name = "cidr";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(string);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = true;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			schema = new DataTypeSchema (this);
			schema.Name = "enum";
			schema.LengthRange = new Range (0);
			schema.DotNetType = typeof(object);
			schema.IsAutoincrementable = false;
			schema.IsFixedLength = false;
			schema.IsNullable = true;
			schema.ScaleRange = new Range (0, 0);
			schema.PrecisionRange = new Range (0, 0);
			collection.Add (schema);
			
			#endregion 
			return collection;
		}
		
		public override TriggerSchemaCollection GetTableTriggers (TableSchema table)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			TriggerSchemaCollection triggers = new TriggerSchemaCollection ();
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (string.Format (
															@"SELECT * FROM 
															information_schema.triggers
			                                                WHERE event_object_table = '{0}' order by trigger_name", 
															table.Name));
			try {
				using (IDataReader r = command.ExecuteReader ()) {
					while (r.Read ()) {
						TriggerSchema trigger = new TriggerSchema (this);
						trigger.Name = r.GetString (r.GetOrdinal ("trigger_name"));
						trigger.Source = r.GetString (r.GetOrdinal ("action_statement"));
						trigger.TriggerType = (TriggerType)Enum.Parse (typeof(TriggerType), 
																		r.GetString (r.GetOrdinal ("condition_timing")));
						trigger.TriggerEvent = (TriggerEvent)Enum.Parse (typeof(TriggerEvent), 
																		r.GetString (r.GetOrdinal ("event_manipulation")));
						trigger.TriggerFireType = TriggerFireType.ForEachRow;
						triggers.Add (trigger);
					}
				}
			} catch (NpgsqlException e) {
				QueryService.RaiseException (e);
			}
			return triggers;
		}
		

		public override DataTypeSchema GetDataType (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			if (name == null)
				throw new ArgumentNullException ("name");

			string type = null;
			int length = 0;
			int scale = 0;
			ParseType (name, out type, out length, out scale);

			DataTypeSchema dts = new DataTypeSchema (this);
			dts.Name = type;
			switch (type) {
				case "enum":
				case "smallint":
				case "integer":
				case "bigint":
				case "serial":
				case "bigserial":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Integer;
				break;
				case "numeric":
				case "decimal":
				case "real":
				case "double precision":
				case "money":
					dts.LengthRange = new Range (length);
					dts.ScaleRange = new Range (scale);
					dts.DataTypeCategory = DataTypeCategory.Float;
					break;
				break;
				case "character varying":
				case "varying":
				case "varchar":
				case "text":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.VarChar;
					break;
				case "character":
				case "char":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Char;
					break;
				case "bytea":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Binary;
					break;
				case "timestamp":
					dts.DataTypeCategory = DataTypeCategory.TimeStamp;
					break;
				case "interval":
					dts.DataTypeCategory = DataTypeCategory.Interval;
					break;
				case "date":
					dts.DataTypeCategory = DataTypeCategory.Date;
					break;
				case "time":
					dts.DataTypeCategory = DataTypeCategory.Time;
					break;
				case "boolean":
					dts.DataTypeCategory = DataTypeCategory.Bit;
					break;
				case "point":
				case "line":
				case "lseg":
				case "box":
				case "polygon":
				case "circle":
				case "inet":
				case "cidr":
					// Research this
					dts.DataTypeCategory = DataTypeCategory.VarChar;
					break;
				case "bit":
					dts.DataTypeCategory = DataTypeCategory.Bit;
					break;
				case "uuid":
					dts.DataTypeCategory = DataTypeCategory.Uid;
					break;
				case "xml":
					dts.DataTypeCategory = DataTypeCategory.Xml;
					break;
				default:
					dts.DataTypeCategory = DataTypeCategory.Other;
					break;
			}
			
			return dts;
		}
		
		private void ParseType (string str, out string type, out int length, out int scale)
		{
			int parenOpen = str.IndexOf ('(');
			int parenClose = str.IndexOf (')');
			int commaPos = -1;
			if (parenOpen > 0)
				commaPos = str.IndexOf (',', parenOpen);

			if (parenOpen > 0) {
				type = str.Substring (0, parenOpen).Trim ();
				
				string lengthString = null;
				if (commaPos > 0) {
					lengthString = str.Substring (parenOpen + 1, commaPos - parenOpen - 1);
					string scaleString = str.Substring (commaPos + 1, parenClose - commaPos - 1).Trim ();
					int.TryParse (scaleString, out scale);
				} else {
					lengthString = str.Substring (parenOpen + 1, parenClose - parenOpen - 1);
					scale = 0;
				}
				int.TryParse (lengthString, out length);
			} else {
				type = str;
				length = 1;
				scale = 0;
			}
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-createdatabase.html
		public override void CreateDatabase (DatabaseSchema database)
		{
			ExecuteNonQuery ("CREATE DATABASE " + database.Name);
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-createtable.html
		public override string GetTableCreateStatement (TableSchema table)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("CREATE TABLE ");
			sb.Append (table.Name);
			sb.Append (" (");

			bool first = true;
			foreach (ColumnSchema column in table.Columns) {
				if (first)
					first = false;
				else
					sb.Append ("," + Environment.NewLine);
				
				sb.Append (column.Name);
				sb.Append (' ');
				sb.Append (column.DataType.GetCreateString (column));
				
				if (!column.IsNullable)
					sb.Append (" NOT NULL ");
				
				if (column.HasDefaultValue) {
					sb.Append (" DEFAULT ");
					if (column.DefaultValue == null)
						sb.Append (" NULL ");
					else
						sb.Append (column.DefaultValue);
				}
				
				foreach (ConstraintSchema constraint in column.Constraints)
					sb.Append (GetConstraintString (constraint, false));
			}

			foreach (ConstraintSchema constraint in table.Constraints) {
				sb.Append ("," + Environment.NewLine);
				sb.Append (GetConstraintString (constraint, true));
			}
			
			sb.Append (")");
			
			if (table.TableSpaceName != null) {
				sb.Append (" TABLESPACE ");
				sb.Append (table.TableSpaceName);
				sb.Append (';');
			}
			sb.Append (';');
			foreach (TriggerSchema trigger in table.Triggers) {
				sb.Append (Environment.NewLine);
				sb.Append (GetTriggerCreateStatement (trigger));				
			}
			
			return sb.ToString ();
		}
					
		protected virtual string GetConstraintString (ConstraintSchema constraint, bool isTableConstraint)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("CONSTRAINT ");
			sb.Append (constraint.Name);
			sb.Append (' ');
			
			switch (constraint.ConstraintType) {
			case ConstraintType.PrimaryKey:
				sb.Append ("PRIMARY KEY ");
				if (isTableConstraint)
					sb.Append (GetColumnsString (constraint.Columns, true));
				break;
			case ConstraintType.Unique:
				sb.Append ("UNIQUE ");
				if (isTableConstraint)
					sb.Append (GetColumnsString (constraint.Columns, true));
				break;
			case ConstraintType.ForeignKey:
				sb.Append ("FOREIGN KEY ");
				sb.Append (GetColumnsString (constraint.Columns, true));
				sb.Append (" REFERENCES ");
				
				ForeignKeyConstraintSchema fk = constraint as ForeignKeyConstraintSchema;
				sb.Append (fk.ReferenceTableName);
				sb.Append (' ');
				if (fk.ReferenceColumns != null)
					sb.Append (GetColumnsString (fk.ReferenceColumns, true));
				break;
			case ConstraintType.Check:
				sb.Append ("CHECK (");
				sb.Append ((constraint as CheckConstraintSchema).Source);
				sb.Append (")");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			return sb.ToString ();
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-createview.html
		public override void CreateView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-createindex.html
		//http://www.postgresql.org/docs/8.2/interactive/sql-createconstraint.html
		public override void CreateIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-createtrigger.html
		public override void CreateTrigger (TriggerSchema trigger)
		{
			string sql = GetTriggerCreateStatement (trigger);
			ExecuteNonQuery (sql);
		}
		
		protected virtual string GetTriggerCreateStatement (TriggerSchema trigger)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("CREATE TRIGGER ");
			sb.Append (trigger.Name);
			
			switch (trigger.TriggerType) {
			case TriggerType.Before:
				sb.Append (" BEFORE");
				break;
			case TriggerType.After:
				sb.Append (" AFTER");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			switch (trigger.TriggerEvent) {
			case TriggerEvent.Insert:
				sb.Append (" INSERT ");
				break;
			case TriggerEvent.Update:
				sb.Append (" UPDATE ");
				break;
			case TriggerEvent.Delete:
				sb.Append (" DELETE ");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			sb.Append ("ON ");
			sb.Append (trigger.TableName);
			sb.Append (' ');
			sb.Append (Environment.NewLine);
			
			switch (trigger.TriggerFireType) {
			case TriggerFireType.ForEachRow:
				sb.Append (" FOR EACH ROW ");
				break;
			case TriggerFireType.ForEachStatement:
				sb.Append (" FOR EACH STATEMENT ");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			sb.Append (Environment.NewLine);
			sb.Append ("EXECUTE PROCEDURE ");
			sb.Append (trigger.Source);
			sb.Append (";");
			
			return sb.ToString ();
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-createuser.html
		public override void CreateUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-alterdatabase.html
		public override void AlterDatabase (DatabaseAlterSchema database)
		{
			throw new NotImplementedException ();
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-altertable.html
		public override void AlterTable (TableAlterSchema table)
		{
			throw new NotImplementedException ();
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-alterindex.html
		public override void AlterIndex (IndexAlterSchema index)
		{
			throw new NotImplementedException ();
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-altertrigger.html
		public override void AlterTrigger (TriggerAlterSchema trigger)
		{
			throw new NotImplementedException ();
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-alteruser.html
		public override void AlterUser (UserAlterSchema user)
		{
			throw new NotImplementedException ();
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-dropdatabase.html
		public override void DropDatabase (DatabaseSchema database)
		{
			ExecuteNonQuery ("DROP DATABASE IF EXISTS " + database.Name + ";");
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-droptable.html
		public override void DropTable (TableSchema table)
		{
			ExecuteNonQuery ("DROP TABLE IF EXISTS " + table.Name + ";");
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-dropview.html
		public override void DropView (ViewSchema view)
		{
			ExecuteNonQuery ("DROP VIEW IF EXISTS " + view.Name + ";");
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-dropindex.html
		public override void DropIndex (IndexSchema index)
		{
			ExecuteNonQuery ("DROP INDEX IF EXISTS " + index.Name + " ON " + index.TableName + ";");
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-droptrigger.html
		public override void DropTrigger (TriggerSchema trigger)
		{
			ExecuteNonQuery ("DROP TRIGGER IF EXISTS " + trigger.Name + " ON " + trigger.TableName + ";");
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-dropuser.html
		public override void DropUser (UserSchema user)
		{
			ExecuteNonQuery ("DROP USER IF EXISTS " + user.Name + ";");
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-alterdatabase.html
		public override void RenameDatabase (DatabaseSchema database, string name)
		{
			ExecuteNonQuery ("ALTER DATABASE " + database.Name + " RENAME TO " + name + ";");
			
			connectionPool.ConnectionContext.ConnectionSettings.Database = name;
			database.Name = name;
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-altertable.html
		public override void RenameTable (TableSchema table, string name)
		{
			ExecuteNonQuery ("ALTER TABLE " + table.Name + " RENAME TO " + name + ";");
			
			table.Name = name;
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-altertable.html
		public override void RenameView (ViewSchema view, string name)
		{
			//this is no copy paste error, it really is "ALTER TABLE"
			ExecuteNonQuery ("ALTER TABLE " + view.Name + " RENAME TO " + name + ";");
			
			view.Name = name;
		}
		
		//http://www.postgresql.org/docs/8.2/interactive/sql-altertrigger.html
		public override void RenameTrigger (TriggerSchema trigger, string name)
		{
			ExecuteNonQuery ("ALTER TRIGGER " + trigger.Name + " ON " + trigger.TableName + " RENAME TO " + name + ";");
			
			trigger.Name = name;
		}

		//http://www.postgresql.org/docs/8.2/interactive/sql-alteruser.html
		public override void RenameUser (UserSchema user, string name)
		{
			ExecuteNonQuery ("ALTER USER " + user.Name + " RENAME TO " + name + ";");
			
			user.Name = name;
		}
		
		public override string GetViewAlterStatement (ViewSchema view)
		{
			//'CREATE ' <-- after this we insert
			return view.Definition.Insert (6, "OR REPLACE ");
		}
		
		public override string GetProcedureAlterStatement (ProcedureSchema procedure)
		{
			//'CREATE ' <-- after this we insert
			return procedure.Definition.Insert (6, "OR REPLACE ");
		}
						
		/// <summary>
		/// Last system OID used in postgres to monitor system vs user
		/// objects. This varies based on the connections Server Version.
		/// </summary>
		protected int LastSystemOID {
			get {
				IPooledDbConnection conn = connectionPool.Request ();
				NpgsqlConnection internalConn = conn.DbConnection as NpgsqlConnection;
				int major = internalConn.ServerVersion.Major;
				int minor = internalConn.ServerVersion.Minor;
				conn.Release ();
				
				if (major == 8)
					return 17137;
				else if (major == 7 && minor == 1)
					return 18539;
				else if (major == 7 && minor == 2)
					return 16554;
				else if (major == 7 && minor == 3)
					return 16974;
				else if (major == 7 && minor == 4)
					return 17137;
				else
					return 17137;
			}
		}
	}
}
