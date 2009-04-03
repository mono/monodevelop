//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Daniel Morgan <danielmorgan@verizon.net>
//	Sureshkumar T <tsureshkumar@novell.com>
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
// Copyright (c) 2007-2008 Ben Motmans
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
using System.Text.RegularExpressions;
using System.Data;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using MonoDevelop.Core;
namespace MonoDevelop.Database.Sql.MySql
{
	public class MySqlSchemaProvider : AbstractEditSchemaProvider
	{
		public MySqlSchemaProvider (IConnectionPool connectionPool)
			: base (connectionPool)
		{
			AddSupportedSchemaActions (SchemaType.Database, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.Table, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.View, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.TableColumn, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.Trigger, SchemaActions.All);
			AddSupportedSchemaActions (SchemaType.PrimaryKeyConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.ForeignKeyConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.CheckConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.UniqueConstraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.Constraint, SchemaActions.Create | SchemaActions.Drop | SchemaActions.Rename | SchemaActions.Schema);
			AddSupportedSchemaActions (SchemaType.User, SchemaActions.Schema);
			
			if (connectionPool.DatabaseVersion.Major > 4) {
				AddSupportedSchemaActions (SchemaType.Procedure, SchemaActions.All);
				AddSupportedSchemaActions (SchemaType.ProcedureParameter, SchemaActions.Schema);
			}
		}

		public override DatabaseSchemaCollection GetDatabases ()
		{
			DatabaseSchemaCollection databases = new DatabaseSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand ("SHOW DATABASES;");
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							DatabaseSchema db = new DatabaseSchema (this);
							db.Name = r.GetString (0);
							databases.Add (db);
							
						}
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return databases;
		}

		// see: http://dev.mysql.com/doc/refman/5.1/en/tables-table.html
		// // see: http://dev.mysql.com/doc/refman/5.1/en/show-create-table.html
		public override TableSchemaCollection GetTables ()
		{
			TableSchemaCollection tables = new TableSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand ("SHOW TABLES;");
			try {
				using (command) {
					if (GetMainVersion (command) >= 5) {
						//in mysql 5.x we can use an sql query to provide the comment
						command.CommandText = "SELECT TABLE_NAME, TABLE_SCHEMA, TABLE_TYPE, TABLE_COMMENT FROM `information_schema`.`TABLES` "
							+ "WHERE TABLE_TYPE='BASE TABLE' AND TABLE_SCHEMA='"
							+ command.Connection.Database
							+ "' ORDER BY TABLE_NAME;";
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								TableSchema table = new TableSchema (this);
								table.Name = r.GetString (0);
								table.SchemaName = r.GetString (1);
								table.Comment = r.IsDBNull (3) ? null : r.GetString (3);
								
								IPooledDbConnection conn2 = connectionPool.Request ();
								IDbCommand command2 = conn2.CreateCommand ("SHOW CREATE TABLE `" + table.Name + "`;");
								using (IDataReader r2 = command2.ExecuteReader()) {
									r2.Read ();
									table.Definition = r2.GetString (1);
								}
								conn2.Release ();
								tables.Add (table);
							}
							r.Close ();
						}
					} else {
						//use the default command for mysql 4.x and 3.23
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								TableSchema table = new TableSchema (this);
			
								table.Name = r.GetString (0);
								table.SchemaName = command.Connection.Database;
								
								IPooledDbConnection conn2 = connectionPool.Request ();
								IDbCommand command2 = conn2.CreateCommand ("SHOW CREATE TABLE `" + table.Name + "`;");
								using (IDataReader r2 = command2.ExecuteReader()) {
									r2.Read ();
									table.Definition = r2.GetString (1);
								}
								conn2.Release ();
								
								tables.Add (table);
							}
							r.Close ();
						}
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
			IDbCommand command = conn.CreateCommand (String.Format ("DESCRIBE {0}", table.Name));
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ColumnSchema column = new ColumnSchema (this, table);

							column.Name = r.GetString (0);
							column.DataTypeName = r.GetString (1);
							column.IsNullable = String.Compare (r.GetString (2), "YES", true) == 0;
							column.DefaultValue = r.IsDBNull (4) ? null : r.GetString (4);
							//TODO: if r.GetString (5) constains "auto_increment"
							column.OwnerName = table.Name;
			
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

		// see: http://dev.mysql.com/doc/refman/5.1/en/views-table.html
		public override ViewSchemaCollection GetViews ()
		{
			ViewSchemaCollection views = new ViewSchemaCollection ();

			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT TABLE_NAME, TABLE_SCHEMA FROM information_schema.VIEWS where TABLE_SCHEMA = '"
				+ ConnectionPool.ConnectionContext.ConnectionSettings.Database +
				"' ORDER BY TABLE_NAME"
			);
			try {
				using (command) {
					if (GetMainVersion (command) >= 5) {
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								ViewSchema view = new ViewSchema (this);
			
								view.Name = r.GetString (0);
								view.OwnerName = r.GetString (1);
								
								IPooledDbConnection conn2 = connectionPool.Request ();
								IDbCommand command2 = conn2.CreateCommand ("SHOW CREATE TABLE `" + view.Name + "`;");
								using (IDataReader r2 = command2.ExecuteReader()) {
									r2.Read ();
									view.Definition = r2.GetString (1);
								}
								conn2.Release ();
								
								views.Add (view);
							}
							r.Close ();
						}
					} //else: do nothing, since views are only supported since mysql 5.x
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
				IDbCommand command = conn.CreateCommand (String.Format ("DESCRIBE {0}", view.Name));
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ColumnSchema column = new ColumnSchema (this, view);
			
							column.Name = r.GetString (0);
							column.DataTypeName = r.GetString (1);
							column.IsNullable = r.IsDBNull (2);
							column.DefaultValue = r.GetString (4);
							column.Comment = r.GetString (5);
							column.OwnerName = view.Name;
			
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

		// see: http://dev.mysql.com/doc/refman/5.1/en/routines-table.html
		public override ProcedureSchemaCollection GetProcedures ()
		{
			ProcedureSchemaCollection procedures = new ProcedureSchemaCollection ();
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (string.Concat (
																	"SELECT ROUTINE_NAME, ROUTINE_SCHEMA, ROUTINE_TYPE FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA ='",
																	ConnectionPool.ConnectionContext.ConnectionSettings.Database,
																	"' ORDER BY ROUTINE_NAME"));
			try {
				using (command) {
					if (GetMainVersion (command) >= 5) {
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								ProcedureSchema procedure = new ProcedureSchema (this);
								procedure.Name = r.GetString (0);
								procedure.OwnerName = r.GetString (1);
								procedure.IsSystemProcedure = (r.GetString (2).IndexOf ("system", StringComparison.OrdinalIgnoreCase) > -1);
								procedure.IsFunction = (r.GetString (2).IndexOf ("function", StringComparison.OrdinalIgnoreCase) > -1);
									
								IPooledDbConnection conn2 = connectionPool.Request ();
								IDbCommand command2;
								if (!procedure.IsFunction)
									command2 = conn2.CreateCommand ("SHOW CREATE PROCEDURE `" + procedure.Name + "`;");
								else
									command2 = conn2.CreateCommand ("SHOW CREATE FUNCTION `" + procedure.Name + "`;");
								using (IDataReader r2 = command2.ExecuteReader()) {
									r2.Read ();
									procedure.Definition = r2.GetString (2);
								}
					    		conn2.Release ();
					    		procedures.Add (procedure);
					    	}
							r.Close ();
						}
					} //else: do nothing, since procedures are only supported since mysql 5.x
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
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT param_list FROM mysql.proc where name = '" + procedure.Name + "'"
			);
			try {
				using (command) {
					if (GetMainVersion (command) >= 5) {
					    	using (IDataReader r = command.ExecuteReader()) {
					    		while (r.Read ()) {
					    			if (r.IsDBNull (0))
					    				continue;
					
					    			string[] field = Encoding.ASCII.GetString ((byte[])r.GetValue (0)).Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					    			foreach (string chunk in field) {
									ParameterSchema param = new ParameterSchema (this);
									param.Definition = chunk;
					    				
									string[] tmp = chunk.TrimStart (new char[] { ' ' }).Split (new char[] { ' ' });
									int nameIndex = 0;
									if (String.Compare (tmp[0], "OUT", true) == 0) {
										nameIndex = 1;
										param.ParameterType = ParameterType.Out;
									} else if (String.Compare (tmp[0], "INOUT", true) == 0) {
										nameIndex = 1;
										param.ParameterType = ParameterType.InOut;
									} else {
										param.ParameterType = ParameterType.In;
									}

					    				param.Name = tmp[nameIndex];
					    				param.OwnerName = procedure.Name;
									param.DataTypeName = tmp[nameIndex + 1];
					    				
					    				parameters.Add (param);
					    			}
					    		}
							r.Close ();
						}
					} //else: do nothing, since procedures are only supported since mysql 5.x
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return parameters;
		}

		private static Regex constraintRegex = new Regex (@"`([\w ]+)`", RegexOptions.Compiled);
		public override ConstraintSchemaCollection GetTableConstraints (TableSchema table)
		{
			ConstraintSchemaCollection constraints = new ConstraintSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand ("SHOW TABLE STATUS FROM `" + table.SchemaName + "`;");
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							string[] chunks = ((string)r["Comment"]).Split (';');

							//the values we are looking for are in the format (`table`) REFER `database\table2` (`table2`)
							foreach (string chunk in chunks) {
								if (constraintRegex.IsMatch (chunk)) {
									MatchCollection matches = constraintRegex.Matches (chunk);
			
									ForeignKeyConstraintSchema constraint = new ForeignKeyConstraintSchema (this);
									constraint.ReferenceTableName = matches[1].Groups[1].ToString ();
									constraint.Name = matches[0].Groups[1].ToString ();

									constraints.Add (constraint);
								}
							}
						}
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return constraints;
		}
		
		public override ConstraintSchemaCollection GetColumnConstraints (TableSchema table, ColumnSchema column)
		{
			ConstraintSchemaCollection constraints = new ConstraintSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (String.Format ("DESCRIBE {0}", table.Name));
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							if (r.IsDBNull (3) || String.Compare (r.GetString (0), column.Name, true) != 0)
								continue;
							
							string key = r.GetString (3).ToUpper ();
							
							ConstraintSchema constraint = null;
							if (key.Contains ("PRI")) {
								constraint = CreatePrimaryKeyConstraintSchema ("pk_" + column.Name);
							} else if (key.Contains ("UNI")) {
								constraint = CreateUniqueConstraintSchema ("uni_" + column.Name);
							} else {
								continue;
							}
							constraint.IsColumnConstraint = true;
							constraint.OwnerName = r.GetString (0);

							constraints.Add (constraint);
						}
						r.Close ();
					};
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return constraints;
		}

		public override UserSchemaCollection GetUsers ()
		{
			UserSchemaCollection users = new UserSchemaCollection ();

			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand ("SELECT DISTINCT user from mysql.user where user != '';");
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader ()) {
						while (r.Read ()) {
							UserSchema user = new UserSchema (this);
							user.Name = r.GetString (0);
		
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
		
		// see:
		// http://www.htmlite.com/mysql003.php
		// http://kimbriggs.com/computers/computer-notes/mysql-notes/mysql-data-types.file
		// http://dev.mysql.com/doc/refman/5.1/en/data-type-overview.html
		public override DataTypeSchema GetDataType (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			string type = null;
			int length = 0;
			int scale = 0;
			ParseType (name, out type, out length, out scale);

			DataTypeSchema dts = new DataTypeSchema (this);
			dts.Name = type;
			switch (type.ToLower ()) {
				case "tinyint":
				case "smallint":
				case "mediumint":
				case "int":
				case "integer":
				case "bigint":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "bit":
					dts.LengthRange = new Range (length); //in bits
					dts.DataTypeCategory = DataTypeCategory.Bit;
					break;
				case "bool":
				case "boolean":
					dts.LengthRange = new Range (1); //in bits
					dts.DataTypeCategory = DataTypeCategory.Boolean;
					break;
				case "float":
				case "double":
				case "double precision":
				case "decimal":
				case "dec":
					dts.LengthRange = new Range (length);
					dts.ScaleRange = new Range (scale);
					dts.DataTypeCategory = DataTypeCategory.Boolean;
					break;
				case "date":
					dts.DataTypeCategory = DataTypeCategory.Date;
					break;
				case "datetime":
					dts.DataTypeCategory = DataTypeCategory.DateTime;
					break;
				case "timestamp":
					dts.DataTypeCategory = DataTypeCategory.TimeStamp;
					break;
				case "time":
					dts.DataTypeCategory = DataTypeCategory.Time;
					break;
				case "year":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "binary":
				case "char byte":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Binary;
					break;
				case "varbinary":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.VarBinary;
					break;
				case "tinyblob":
				case "mediumblob":
				case "longblob":
				case "blob":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Binary;
					break;
				case "tinytext":
				case "mediumtext":
				case "longtext":
				case "text":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.NChar;
					break;
				case "national char":
				case "nchar":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.NChar;
					break;
				case "national varchar":
				case "nvarchar":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.NVarChar;
					break;
				case "varchar":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.VarChar;
					break;
				case "char":
					dts.LengthRange = new Range (length);
					dts.DataTypeCategory = DataTypeCategory.Char;
					break;
				case "set":
				case "enum":
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				default:
					dts = null;
					break;
			}
			
			return dts;
		}
		
		//http://dev.mysql.com/doc/refman/5.0/en/charset-mysql.html
		public MySqlCharacterSetSchemaCollection GetCharacterSets ()
		{
			MySqlCharacterSetSchemaCollection characterSets = new MySqlCharacterSetSchemaCollection ();

			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand ("SHOW CHARACTER SET;");
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader ()) {
						while (r.Read ()) {
							MySqlCharacterSetSchema charset = new MySqlCharacterSetSchema (this);
							charset.Name = r.GetString (0);
							charset.Comment = r.GetString (1);
							charset.DefaultCollactionName = r.GetString (2);
							charset.MaxLength = r.GetInt32 (3);
		
							characterSets.Add (charset);
						}
		
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return characterSets;
		}
		
		public MySqlCollationSchemaCollection GetCollations (MySqlCharacterSetSchema characterSet)
		{
			MySqlCollationSchemaCollection collations = new MySqlCollationSchemaCollection ();

			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (String.Format ("SHOW COLLATION LIKE '{0}%';", characterSet.Name));
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader ()) {
						while (r.Read ()) {
							MySqlCollationSchema collation = new MySqlCollationSchema (this);
							collation.Name = r.GetString (0);
							collation.CharacterSetName = r.GetString (1);
							collation.Id = r.GetInt32 (2);
							collation.IsDefaultCollation = r.GetBoolean (3);
							collation.IsCompiled = r.GetBoolean (4);
							collation.SortLength = r.GetInt32 (5);
		
							collations.Add (collation);
						}
		
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return collations;
		}
		
		//http://dev.mysql.com/doc/refman/5.1/en/create-database.html
		public override void CreateDatabase (DatabaseSchema database)
		{
			ExecuteNonQuery ("CREATE DATABASE " + database.Name);
		}

		//http://dev.mysql.com/doc/refman/5.1/en/create-table.html
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
					sb.Append (" NOT NULL");
				if (column.HasDefaultValue) {
					sb.Append (" DEFAULT ");
					if (column.DefaultValue == null)
						sb.Append ("NULL");
					else
						sb.Append (column.DefaultValue);
				}
				//TODO: AUTO_INCREMENT
				
				foreach (ConstraintSchema constraint in column.Constraints) {
					switch (constraint.ConstraintType) {
					case ConstraintType.Unique:
						sb.Append (" UNIQUE");
						break;
					case ConstraintType.PrimaryKey:
						sb.Append (" PRIMARY KEY");
						break;
					default:
						throw new NotImplementedException ();
					}
				}
				
				if (column.Comment != null) {
					sb.Append (" COMMENT '");
					sb.Append (column.Comment);
					sb.Append ("'");
				}
			}
			//TODO: table comment
			
			foreach (ConstraintSchema constraint in table.Constraints) {
				sb.Append ("," + Environment.NewLine);
				sb.Append (GetConstraintString (constraint));
			}
			
			sb.Append (")");
			
			if (table.TableSpaceName != null) {
				sb.Append (", TABLESPACE ");
				sb.Append (table.TableSpaceName);
				sb.Append (" STORAGE DISK");
			}
			
			sb.Append (";");
			
			foreach (TriggerSchema trigger in table.Triggers) {
				sb.Append (Environment.NewLine);
				sb.Append (GetTriggerCreateStatement (trigger));				
			}

			return sb.ToString ();
		}
		
		protected virtual string GetConstraintString (ConstraintSchema constraint)
		{
			if (constraint.ConstraintType == ConstraintType.Check)
				return String.Format ("CHECK ({0})", (constraint as CheckConstraintSchema).Source);
			
			StringBuilder sb = new StringBuilder ();
			sb.Append ("CONSTRAINT ");
			sb.Append (constraint.Name);
			sb.Append (' ');

			switch (constraint.ConstraintType) {
			case ConstraintType.PrimaryKey:
				sb.Append ("PRIMARY KEY ");
				sb.Append (GetColumnsString (constraint.Columns, true));
				break;
			case ConstraintType.Unique:
				sb.Append ("UNIQUE ");
				sb.Append (GetColumnsString (constraint.Columns, true));
				break;
			case ConstraintType.ForeignKey:
				sb.Append ("FOREIGN KEY ");
				sb.Append (GetColumnsString (constraint.Columns, true));
				sb.Append (" REFERENCES ");
				
				ForeignKeyConstraintSchema fk = constraint as ForeignKeyConstraintSchema;
				sb.Append (fk.ReferenceTable);
				sb.Append (' ');
				if (fk.ReferenceColumns != null)
					sb.Append (GetColumnsString (fk.ReferenceColumns, true));
				break;
			default:
				throw new NotImplementedException ();
			}
			
			return sb.ToString ();
		}

		//http://dev.mysql.com/doc/refman/5.1/en/create-view.html
		public override void CreateView (ViewSchema view)
		{
			ExecuteNonQuery (view.Definition);
		}

		//http://dev.mysql.com/doc/refman/5.1/en/create-procedure.html
		public override void CreateProcedure (ProcedureSchema procedure)
		{
			ExecuteNonQuery (procedure.Definition);
		}

		//http://dev.mysql.com/doc/refman/5.1/en/create-index.html
		public override void CreateIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		//http://dev.mysql.com/doc/refman/5.1/en/create-trigger.html
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
			if (trigger.TriggerType == TriggerType.Before)
				sb.Append (" BEFORE ");
			else
				sb.Append (" AFTER ");
			
			switch (trigger.TriggerEvent) {
			case TriggerEvent.Delete:
				sb.Append ("DELETE");
				break;
			case TriggerEvent.Insert:
				sb.Append ("INSERT");
				break;
			case TriggerEvent.Update:
				sb.Append ("UPDATE");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			sb.Append (" ON ");
			sb.Append (trigger.TableName);
			sb.Append (" FOR EACH ROW ");
			sb.Append (Environment.NewLine);
			sb.Append (trigger.Source);
			sb.Append (";");
			
			return sb.ToString ();
		}

		//http://dev.mysql.com/doc/refman/5.1/en/create-user.html
		public override void CreateUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		//http://dev.mysql.com/doc/refman/5.1/en/alter-database.html
		public override void AlterDatabase (DatabaseAlterSchema database)
		{
			throw new NotImplementedException ();
		}

		//http://dev.mysql.com/doc/refman/5.1/en/alter-table.html
		public override void AlterTable (TableAlterSchema table)
		{
			throw new NotImplementedException ();
		}

		//http://dev.mysql.com/doc/refman/5.1/en/alter-view.html
		public override void AlterView (ViewAlterSchema view)
		{
//TODO: fixme
//			ExecuteNonQuery (view.Definition);
		}

		//http://dev.mysql.com/doc/refman/5.1/en/alter-procedure.html
		public override void AlterProcedure (ProcedureAlterSchema procedure)
		{
			//ExecuteNonQuery (procedure.Definition); //TODO: fixme
		}
		
		//http://dev.mysql.com/doc/refman/5.1/en/drop-database.html
		public override void DropDatabase (DatabaseSchema database)
		{
			ExecuteNonQuery ("DROP DATABASE IF EXISTS " + database.Name + ";");
		}

		//http://dev.mysql.com/doc/refman/5.1/en/drop-table.html
		public override void DropTable (TableSchema table)
		{
			ExecuteNonQuery ("DROP TABLE IF EXISTS " + table.Name + ";");
		}

		//http://dev.mysql.com/doc/refman/5.1/en/drop-view.html
		public override void DropView (ViewSchema view)
		{
			ExecuteNonQuery ("DROP VIEW IF EXISTS " + view.Name + ";");
		}

		//http://dev.mysql.com/doc/refman/5.1/en/drop-procedure.html
		public override void DropProcedure (ProcedureSchema procedure)
		{
			ExecuteNonQuery ("DROP PROCEDURE IF EXISTS " + procedure.Name + ";");
		}

		//http://dev.mysql.com/doc/refman/5.1/en/drop-index.html
		public override void DropIndex (IndexSchema index)
		{
			ExecuteNonQuery ("DROP INDEX " + index.Name + " ON " + index.TableName + ";");
		}
		
		//http://dev.mysql.com/doc/refman/5.1/en/drop-trigger.html
		public override void DropTrigger (TriggerSchema trigger)
		{
			ExecuteNonQuery ("DROP TRIGGER IF EXISTS " + trigger.Name + ";");
		}

		//http://dev.mysql.com/doc/refman/5.1/en/drop-user.html
		public override void DropUser (UserSchema user)
		{
			ExecuteNonQuery ("DROP USER " + user.Name + ";");
		}
		
		//http://dev.mysql.com/doc/refman/5.1/en/rename-database.html
		public override void RenameDatabase (DatabaseSchema database, string name)
		{
			ExecuteNonQuery ("RENAME DATABASE " + database.Name + " TO " + name + ";");
			
			connectionPool.ConnectionContext.ConnectionSettings.Database = name;
			database.Name = name;
		}

		//http://dev.mysql.com/doc/refman/5.1/en/rename-table.html
		public override void RenameTable (TableSchema table, string name)
		{
			ExecuteNonQuery ("RENAME TABLE " + table.Name + " TO " + name + ";");
			
			table.Name = name;
		}

		//http://dev.mysql.com/doc/refman/5.1/en/rename-table.html
		public override void RenameView (ViewSchema view, string name)
		{
			//this is no copy paste error, it really is "RENAME TABLE"
			ExecuteNonQuery ("RENAME TABLE " + view.Name + " TO " + name + ";");
			
			view.Name = name;
		}

		//http://dev.mysql.com/doc/refman/5.1/en/rename-user.html
		public override void RenameUser (UserSchema user, string name)
		{
			ExecuteNonQuery ("RENAME USER " + user.Name + " TO " + name + ";");
			
			user.Name = name;
		}
		
		public override DatabaseSchema CreateDatabaseSchema (string name)
		{
			MySqlDatabaseSchema schema = new MySqlDatabaseSchema (this);
			schema.Name = name;
			return schema;
		}
		
//		public override string GetViewAlterStatement (ViewAlterSchema view)
//		{
//			return String.Concat ("DROP VIEW IF EXISTS ", view.Name, "; ", Environment.NewLine, view.Definition); 
//		}
//		
		public override string GetProcedureAlterStatement (ProcedureSchema procedure)
		{
			if (!procedure.IsFunction)
				return String.Concat ("DROP PROCEDURE IF EXISTS ", procedure.Name, "; ", Environment.NewLine, procedure.Definition);
			else
				return String.Concat ("DROP FUNCTION IF EXISTS ", procedure.Name, "; ", Environment.NewLine, procedure.Definition);
		}
		
//TODO: remove this and use the Version provided by the connection pool
		private int GetMainVersion (IDbCommand command)
		{
			string str = (command.Connection as MySqlConnection).ServerVersion;
			int version = -1;
			if (int.TryParse (str.Substring (0, str.IndexOf (".")), out version))
				return version;
			return -1;
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
	}
}