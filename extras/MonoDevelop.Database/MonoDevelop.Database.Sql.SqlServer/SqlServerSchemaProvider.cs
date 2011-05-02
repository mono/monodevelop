//
// Authors:
//	Christian Hergert  <chris@mosaix.net>
//	Daniel Morgan <danielmorgan@verizon.net>
//	Sureshkumar T <tsureshkumar@novell.com>
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THEC
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Data;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Data.SqlClient;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Sql.SqlServer
{
	// see:
	// http://www.alberton.info/sql_server_meta_info.html + msdn
	public class SqlServerSchemaProvider : AbstractEditSchemaProvider
	{
		string[] system_procs;
		string[] system_tables;
		

		public SqlServerSchemaProvider (IConnectionPool connectionPool)
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
			
			// TODO: XDocument.Load(XmlTextReader) isn't working on Mono 2.4. Should be fixed in 2.6
			/* using (System.IO.Stream stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("SysObjects.xml")) {
				XmlTextReader reader = new XmlTextReader (stream);
				XDocument doc = XDocument.Load (reader);
				
				var sysSP = from item in doc.Elements("SysObjects").Descendants () 
					where item.Attribute ("type") != null && item.Attribute("type").Value == "SP" 
					select item;
				reader.Close ();
			}
			*/
			
			using (System.IO.Stream stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream ("SysObjects.xml")) {
				XmlDocument doc = new XmlDocument ();
				doc.Load (stream);
				List<string> sps = new List<string> ();
				List<string> tables = new List<string> ();
				foreach (XmlNode node in doc.FirstChild.ChildNodes)
					if (node.Attributes["type"].Value == "SP") 
						sps.Add (node.InnerText);
					else if (node.Attributes["type"].Value == "Table") 
						tables.Add (node.InnerText);
				
				system_procs = sps.ToArray ();
				system_tables = tables.ToArray ();
			}
			
		}
		
		public override DatabaseSchemaCollection GetDatabases ()
		{
			DatabaseSchemaCollection databases = new DatabaseSchemaCollection ();
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				//we don't have to change it back afterwards, since the connectionpool will do this for us
				conn.DbConnection.ChangeDatabase ("master"); 
				using (IDbCommand command = conn.CreateCommand ("select name from sysdatabases")) {
					try {
						using (command)
							using (IDataReader r = command.ExecuteReader()) {
								while (r.Read()) {
									DatabaseSchema db = new DatabaseSchema (this);
									db.Name = r.GetString (0);
									databases.Add (db);
								}
								r.Close ();
							}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
				}
			}
			return databases;
		}
		
		public virtual SqlServerCollationSchemaCollection GetCollations ()
		{
			SqlServerCollationSchemaCollection collations = new SqlServerCollationSchemaCollection();
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				
				conn.DbConnection.ChangeDatabase ("master"); 
				using (IDbCommand command = conn.CreateCommand ("SELECT * FROM ::fn_helpcollations()")) {
					try {
						using (IDataReader reader = command.ExecuteReader ()) {
							while (reader.Read ()) {
								SqlServerCollationSchema coll = new SqlServerCollationSchema (this);
								coll.Name = reader.GetString (0);
								coll.Description = reader.GetString (1);
								collations.Add (coll);
							}
							reader.Close ();
						}
					 } catch (IOException ioex) {
						//FIXME: Avoid an IOException AND ObjectDisposedException (https://bugzilla.novell.com/show_bug.cgi?id=556406)
					} catch (ObjectDisposedException dex) {
					}
					catch (Exception e) {
					 	QueryService.RaiseException (e);
					 } finally {
						connectionPool.Release(conn);
					 }
				}
			}
			return collations;			
		}
		

		public override TableSchemaCollection GetTables ()
		{
			TableSchemaCollection tables = new TableSchemaCollection ();
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (@"SELECT 
															su.name AS owner, 
															so.name as table_name, 
															so.id as table_id,										
															so.crdate as created_date, 
															so.xtype as table_type
														FROM dbo.sysobjects so, 
															dbo.sysusers su 
														WHERE
															xtype IN ('S','U')
															AND su.uid = so.uid
														ORDER BY 1, 2")) {
					try {
						using (command) {
							using (IDataReader r = command.ExecuteReader()) {
								while (r.Read()) {
									TableSchema table = new TableSchema (this);
									table.Name = r.GetString(1);
									if (r.GetString(4) == "S")
										table.IsSystemTable = true;
									else 
										if (Array.Exists (system_tables, delegate (string s) {return s == table.Name; }))
											table.IsSystemTable = true;
										else 
											table.IsSystemTable = false;
									table.OwnerName = r.GetString(0);
									table.Definition = GetTableDefinition (table);
									tables.Add (table);
								}
								r.Close ();
							}
						}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
				}
			
			}
			return tables;
		}
		
		public override ColumnSchemaCollection GetTableColumns (TableSchema table)
		{
			ColumnSchemaCollection columns = new ColumnSchemaCollection ();
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (string.Format(@"SELECT 
																			su.name as owner, 
																			so.name as table_name,
																			sc.name as column_name,
																			st.name as date_type, 
																			sc.length as column_length, 
																			sc.xprec as data_precision, 
																			sc.xscale as data_scale,
																			sc.isnullable, 
																			sc.colid as column_id
																		FROM 
																			dbo.syscolumns sc, 
																			dbo.sysobjects so, 
																			dbo.systypes st, dbo.sysusers su
																		WHERE 
																			sc.id = so.id 
																			AND so.xtype in ('U','S')
																			AND so.name = '{0}' 
																			AND su.name = '{1}'
																			AND sc.xusertype = st.xusertype
																			AND su.uid = so.uid
																		ORDER BY sc.colid", table.Name, table.OwnerName)))
					try {
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read()) {
								ColumnSchema column = new ColumnSchema (this, table);
								
								column.Name = r.GetString (2);
								column.DataTypeName = r.GetString (3);
								column.DefaultValue = String.Empty;
								column.Comment = String.Empty;
								column.OwnerName = table.OwnerName;
								column.SchemaName = table.SchemaName;
								column.IsNullable = r.GetValue (7).ToString () == "0" ? true : false;
								column.DataType.LengthRange.Default = r.GetInt16 (4);
								column.DataType.PrecisionRange.Default = r.IsDBNull (5) ? 0 : (int)r.GetByte (5);
								column.DataType.ScaleRange.Default = r.IsDBNull (6) ? 0 : (int)r.GetByte (6);
								column.Definition = String.Concat (column.Name, " ", column.DataTypeName, " ",
								column.DataType.LengthRange.Default > 0 ? "(" + column.DataType.LengthRange.Default + ")" : "",
								column.IsNullable ? " NULL" : " NOT NULL");
								//TODO: append " DEFAULT ..." if column.Default.Length > 0
								columns.Add (column);
							}
							r.Close ();
						}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
			}
			return columns;
		}
		
		public override TriggerSchemaCollection GetTableTriggers (TableSchema table)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			
			TriggerSchemaCollection triggers = new TriggerSchemaCollection ();				
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				string sql = string.Format(@"SELECT 
							 					Tables.Name TableName,
	      										Triggers.name TriggerName,
	      										Triggers.crdate TriggerCreatedDate,
	      										Comments.Text TriggerText
											FROM sysobjects Triggers
											INNER JOIN sysobjects Tables On
	      										 Triggers.parent_obj = Tables.id
											INNER JOIN syscomments Comments On 
	      										Triggers.id = Comments.id
											WHERE 
												Triggers.xtype = 'TR'
												AND Tables.xtype = 'U' 
												AND Tables.Name = '{0}'
											ORDER BY 
												Tables.Name, 
												Triggers.name", table.Name);
				using (IDbCommand command = conn.CreateCommand (sql)) {
					using (IDataReader r = command.ExecuteReader ()) {
						while (r.Read ()) {
								System.Text.RegularExpressions.Regex parseRegEx = new System.Text.RegularExpressions.Regex
															(string.Concat (
							                					@"((CREATE\s*(Temp|Temporary)?\s*TRIGGER){1}\s?(\w+)\s?(IF NOT",
																@" EXISTS)?\s?(BEFORE|AFTER|INSTEAD OF){1}\s?(\w+)\s*ON(\s+\w*",
																@")\s*(FOR EACH ROW){1}\s*(BEGIN){1})\s+(\w|\W)*(END)"));
								TriggerSchema trigger = new TriggerSchema (this);
								trigger.TableName = table.Name;
								trigger.Name = r.GetString (r.GetOrdinal ("TriggerName"));
								sql = r.GetString (r.GetOrdinal ("TriggerText"));
								System.Text.RegularExpressions.MatchCollection matchs = parseRegEx.Matches (sql);
								if (matchs.Count > 0) {
									trigger.TriggerFireType = TriggerFireType.ForEachRow;
									switch (matchs[0].Groups[7].Value.ToLower ()) {
										case "insert":
											trigger.TriggerEvent = TriggerEvent.Insert;
											break;
										case "update":
											trigger.TriggerEvent = TriggerEvent.Update;
											break;
										case "delete":
											trigger.TriggerEvent = TriggerEvent.Delete;
											break;
										default:
											throw new NotImplementedException ();
									}
									switch (matchs[0].Groups[7].Value.ToLower ()) {
										case "before":
											trigger.TriggerType = TriggerType.Before;
											break;
										case "after":
											trigger.TriggerType = TriggerType.After;
											break;
										default:
											throw new NotImplementedException ();
									}
									StringBuilder sbSource = new StringBuilder ();
									foreach (System.Text.RegularExpressions.Capture c in matchs[0].Groups[11].Captures)
										sbSource.Append (c.Value);
									trigger.Source = sbSource.ToString ();
								}
								triggers.Add (trigger);
						}
					}
				}
				conn.Release ();
			}
			return triggers;
		}
		
		public override ViewSchemaCollection GetViews ()
		{
			ViewSchemaCollection views = new ViewSchemaCollection ();

			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (@"SELECT 
																	su.name AS owner, 
																	so.name as table_name, 
																	so.id as table_id, 
																	so.crdate as created_date, 
																	so.xtype as table_type
																FROM dbo.sysobjects so, 
																	dbo.sysusers su
																WHERE 
																	xtype = 'V'
																	AND su.uid = so.uid
																ORDER BY 1, 2"))
					try {
						using (command) {
							using (IDataReader r = command.ExecuteReader()) {
								while (r.Read()) {
									ViewSchema view = new ViewSchema (this);
									
									view.Name = r.GetString (1);
									view.SchemaName = r.GetString (0);
									view.OwnerName = r.GetString (0);
									
									StringBuilder sb = new StringBuilder();
									sb.AppendFormat ("-- View: {0}\n", view.Name);
									sb.AppendFormat ("-- DROP VIEW {0};\n\n", view.Name);
									sb.AppendFormat ("  {0}\n);", GetSource ("[" + view.OwnerName + "].[" + view.Name + "]"));
									view.Definition = sb.ToString ();
									
									views.Add (view);
								}
								r.Close ();
							}
						}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					}
					finally {
						conn.Release ();
					}
			}
			return views;
		}

		public override ColumnSchemaCollection GetViewColumns (ViewSchema view)
		{
			ColumnSchemaCollection columns = new ColumnSchemaCollection ();
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				
				using (IDbCommand command = conn.CreateCommand (string.Format("SELECT * FROM \"{0}\" WHERE 1 = 0", 
																view.Name)))
					try {
						using (IDataReader r = command.ExecuteReader()) {
							for (int i = 0; i < r.FieldCount; i++) {
								ColumnSchema column = new ColumnSchema (this, view);
								
								column.Name = r.GetName(i);
								column.DataTypeName = r.GetDataTypeName(i);
								column.DefaultValue = "";
								column.Definition = "";
								column.OwnerName = view.OwnerName;
								column.SchemaName = view.OwnerName;
								columns.Add (column);
							}
							r.Close ();
						}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
			}
			return columns;
		}

		public override ProcedureSchemaCollection GetProcedures ()
		{
			ProcedureSchemaCollection procedures = new ProcedureSchemaCollection ();
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (@"SELECT 
																	su.name AS owner, 
																	so.name as proc_name, 
																	so.id as proc_id,
																	so.crdate as created_date, 
																	so.xtype as proc_type
																FROM dbo.sysobjects so, 
																	dbo.sysusers su
																WHERE xtype = 'P' 
																	AND su.uid = so.uid
																ORDER BY 1, 2"))
					try {
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								ProcedureSchema procedure = new ProcedureSchema (this);
								procedure.Name = r.GetString (1);
								procedure.OwnerName = r.GetString (0);
								procedure.LanguageName = "TSQL";
								procedure.Definition = GetSource ("[" + procedure.OwnerName + "].[" + procedure.Name + "]");
								if (Array.Exists (system_procs, delegate (string s) {return s == procedure.Name; }))
									procedure.IsSystemProcedure = true;
								else 
									procedure.IsSystemProcedure = false;
								procedures.Add (procedure);
							}
							r.Close ();
						}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
			}			
			return procedures; 
		}

		public override ConstraintSchemaCollection GetTableConstraints (TableSchema table)
		{
			ConstraintSchemaCollection constraints = new ConstraintSchemaCollection ();
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (string.Format (@"select 
																					sysobjects.name, 
																					sysobjects.xtype 
																				from sysobjects 
																				inner join sysobjects sysobjectsParents ON 
																					sysobjectsParents.id = sysobjects.parent_obj
																				where 
																					sysobjectsParents.name = '{0}' and 
				                                                        			sysobjects.xtype in ('C', 'UQ', 'F','PK','CK')", 
																				table.Name)))
					try {
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								ConstraintSchema constraint = null;
								switch (r.GetString (1)) {
									case "F": //foreign key
										constraint = new ForeignKeyConstraintSchema (this);
										break;
									case "PK": //primary key
										constraint = new PrimaryKeyConstraintSchema (this);
										break;
									case "C":
									case "CK": //check constraint
										constraint = new CheckConstraintSchema (this);
										break;
									case "UQ":
										constraint = new UniqueConstraintSchema (this);
										break;
									default:
										break;
								}
									
								if (constraint != null) {
									constraint.Name = r.GetString (0);
									constraints.Add (constraint);
								}
							}
							r.Close ();
						}
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
			}
			return constraints;
		}
		
		// see:
		// http://www.firebirdsql.org/manual/migration-mssql-data-types.html
		// http://webcoder.info/reference/MSSQLDataTypes.html
		// http://www.tar.hu/sqlbible/sqlbible0022.html
		// http://msdn2.microsoft.com/en-us/library/aa258876(SQL.80).aspx
		public override DataTypeSchema GetDataType (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			name = name.ToLower ();

			DataTypeSchema dts = new DataTypeSchema (this);
			dts.Name = name;
			switch (name) {
				case "bigint":
					dts.LengthRange = new Range (8);
					dts.PrecisionRange = new Range (1, 19);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "binary":
					dts.LengthRange = new Range (1, 8004);
					dts.PrecisionRange = new Range (1, 8000);
					dts.DataTypeCategory = DataTypeCategory.Binary;
					break;
				case "bit":
					dts.LengthRange = new Range (1);
					dts.DataTypeCategory = DataTypeCategory.Bit;
					break;
				case "char":
					dts.LengthRange = new Range (1, 8000);
					dts.PrecisionRange = new Range (1, 8000);
					dts.DataTypeCategory = DataTypeCategory.Char;
					break;
				case "datetime":
					dts.LengthRange = new Range (8);
					dts.DataTypeCategory = DataTypeCategory.DateTime;
					break;
				case "decimal":
					dts.LengthRange = new Range (5, 17);
					dts.PrecisionRange = new Range (1, 38);
					dts.ScaleRange = new Range (0, 37);
					dts.DataTypeCategory = DataTypeCategory.Float;
					break;
				case "float":
					dts.LengthRange = new Range (8);
					dts.ScaleRange = new Range (1, 15);
					dts.DataTypeCategory = DataTypeCategory.Float;
					break;
				case "image":
					dts.LengthRange = new Range (0, int.MaxValue);
					dts.PrecisionRange = new Range (0, int.MaxValue);
					dts.DataTypeCategory = DataTypeCategory.VarBinary;
					break;
				case "int":
					dts.LengthRange = new Range (4);
					dts.PrecisionRange = new Range (1, 10);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "money":
					dts.LengthRange = new Range (8);
					dts.PrecisionRange = new Range (1, 19);
					dts.ScaleRange = new Range (4);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "nchar":
					dts.LengthRange = new Range (2, 8000);
					dts.PrecisionRange = new Range (1, 4000);
					dts.DataTypeCategory = DataTypeCategory.NChar;
					break;
				case "ntext":
					dts.LengthRange = new Range (0, int.MaxValue);
					dts.PrecisionRange = new Range (0, 1073741823);
					dts.DataTypeCategory = DataTypeCategory.NVarChar;
					break;
				case "numeric":
					dts.LengthRange = new Range (5, 17);
					dts.PrecisionRange = new Range (1, 38);
					dts.ScaleRange = new Range (0, 37);
					dts.DataTypeCategory = DataTypeCategory.Float;
					break;
				case "nvarchar":
					dts.LengthRange = new Range (0, 8000);
					dts.PrecisionRange = new Range (0, 4000);
					dts.DataTypeCategory = DataTypeCategory.NVarChar;
					break;
				case "real":
					dts.LengthRange = new Range (4);
					dts.ScaleRange = new Range (7);
					dts.DataTypeCategory = DataTypeCategory.Float;
					break;
				case "smalldatetime":
					dts.LengthRange = new Range (4);
					dts.DataTypeCategory = DataTypeCategory.DateTime;
					break;
				case "smallint":
					dts.LengthRange = new Range (2);
					dts.PrecisionRange = new Range (5);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "smallmoney":
					dts.LengthRange = new Range (4);
					dts.PrecisionRange = new Range (10);
					dts.ScaleRange = new Range (4);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "text":
					dts.LengthRange = new Range (0, int.MaxValue);
					dts.PrecisionRange = new Range (0, int.MaxValue);
					dts.DataTypeCategory = DataTypeCategory.VarChar;
					break;
				case "timestamp":
					dts.LengthRange = new Range (1, 8);
					dts.DataTypeCategory = DataTypeCategory.TimeStamp;
					break;
				case "tinyint":
					dts.LengthRange = new Range (1);
					dts.PrecisionRange = new Range (1, 3);
					dts.DataTypeCategory = DataTypeCategory.Integer;
					break;
				case "varbinary":
					dts.LengthRange = new Range (1, 8004);
					dts.PrecisionRange = new Range (0, 8000);
					dts.DataTypeCategory = DataTypeCategory.VarBinary;
					break;
				case "varchar":
					dts.LengthRange = new Range (1, 8000);
					dts.PrecisionRange = new Range (0, 8000);
					dts.DataTypeCategory = DataTypeCategory.VarChar;
					break;
				case "uniqueidentifier":
					dts.LengthRange = new Range (16);
					dts.DataTypeCategory = DataTypeCategory.Uid;
					break;
				case "xml":
					dts.LengthRange = new Range (0, int.MaxValue);
					dts.PrecisionRange = new Range (0, int.MaxValue);
					dts.DataTypeCategory = DataTypeCategory.VarChar;
					break;
				case "cursor":
				case "table":
				case "sql_variant":
					dts.DataTypeCategory = DataTypeCategory.Other;
					break;
				default:
					break;
			}
			
			return dts;
		}
		
		public override DatabaseSchema CreateDatabaseSchema (string name)
		{
			SqlServerDatabaseSchema schema = new SqlServerDatabaseSchema (this);
			schema.Name = name;
			return schema;
		}

		
		//http://msdn2.microsoft.com/en-us/library/aa258257(SQL.80).aspx
		public override void CreateDatabase (DatabaseSchema database)
		{
			SqlServerDatabaseSchema schema = (SqlServerDatabaseSchema)database;
			StringBuilder db = new StringBuilder ("CREATE DATABASE ");
			string newLine = Environment.NewLine;
			db.Append (schema.Name);
			if (schema.FileName != string.Empty && schema.Name != string.Empty) {
				db.AppendLine ();
				db.Append ("ON ");
				db.AppendFormat ("{0}(NAME = {1},", newLine, schema.LogicalName);
				db.AppendFormat ("{0}FILENAME = '{1}'", newLine, schema.FileName);
				if (schema.Size.Size > 0)
					db.AppendFormat(",{0}SIZE = {1}{2}", newLine, schema.Size.Size.ToString (), schema.Size.Type);
				if (schema.MaxSize.Size > 0)
					db.AppendFormat(",{0}MAXSIZE = {1}{2}", newLine, schema.MaxSize.Size.ToString (), schema.MaxSize.Type);
				if (schema.FileGrowth.Size > 0)
					db.AppendFormat(",{0}FILEGROWTH = {1}{2}", newLine, schema.FileGrowth.Size.ToString (), schema.FileGrowth.Type == SizeType.PERCENTAGE ? "%" : schema.FileGrowth.Type.ToString ());
				db.Append (")");
			}
			if (schema.Collation != null)
				db.AppendFormat ("{0}COLLATE {1}{0}", newLine, schema.Collation.Name);
			
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (db.ToString ()))
					try {
							command.ExecuteNonQuery ();
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
			}
		}

		//http://msdn2.microsoft.com/en-us/library/aa258255(SQL.80).aspx
		public override string GetTableCreateStatement (TableSchema table)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("CREATE TABLE ");
			sb.Append (table.Name);
			sb.Append (" (");
			
			bool first = true;
			foreach (ColumnSchema column in table.GetColumns()) {
				if (first)
					first = false;
				else
					sb.Append ("," + Environment.NewLine);
				
				sb.Append (column.Name);
				sb.Append (' ');
				sb.Append (column.DataType.GetCreateString (column));

				if (column.HasDefaultValue) {
					sb.Append (" DEFAULT ");
					if (column.DefaultValue == null)
						sb.Append ("NULL");
					else
						sb.Append (column.DefaultValue);
				}
				if (!column.IsNullable)
					sb.Append (" NOT NULL");
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
				
				//TODO: col comment
			}
			//TODO: table comment
			
			foreach (ConstraintSchema constraint in table.Constraints) {
				sb.Append ("," + Environment.NewLine);
				sb.Append (GetConstraintString (constraint));
			}
			
			sb.Append (");");
			
			foreach (TriggerSchema trigger in table.Triggers) {
				sb.Append (Environment.NewLine);
				sb.Append (GetTriggerCreateStatement (trigger));				
			}

			return sb.ToString ();
		}
		
		protected virtual string GetConstraintString (ConstraintSchema constraint)
		{
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
			case ConstraintType.Check:
				sb.Append ("CHECK (");
				sb.Append ((constraint as CheckConstraintSchema).Source);
				sb.Append (")");
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
			default:
				throw new NotImplementedException ();
			}
			
			return sb.ToString ();
		}

		//http://msdn2.microsoft.com/en-us/library/aa258254(SQL.80).aspx
		public override void CreateView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		//http://msdn2.microsoft.com/en-us/library/aa258259(SQL.80).aspx
		public override void CreateProcedure (ProcedureSchema procedure)
		{
			ExecuteNonQuery (procedure.Definition);
		}

		//http://msdn2.microsoft.com/en-us/library/aa258259(SQL.80).aspx
		public override void CreateIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		//http://msdn2.microsoft.com/en-us/library/aa258254(SQL.80).aspx
		public override void CreateTrigger (TriggerSchema trigger)
		{
			string sql = GetTriggerCreateStatement (trigger);
			ExecuteNonQuery (sql);
		}
		
		protected virtual string GetTriggerCreateStatement (TriggerSchema trigger)
		{
			StringBuilder sb = new StringBuilder ();

			sb.Append ("GO");
			sb.Append (Environment.NewLine);
			sb.Append ("CREATE TRIGGER ");
			sb.Append (trigger.Name);
			sb.Append (" ON ");
			sb.Append (trigger.TableName);

			if (trigger.TriggerType == TriggerType.Before)
				sb.Append (" FOR ");
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

			sb.Append (" AS ");
			sb.Append (Environment.NewLine);
			sb.Append (trigger.Source);
			sb.Append (";");
			
			return sb.ToString ();
		}
		
		//http://msdn2.microsoft.com/en-us/library/aa275464(SQL.80).aspx
		public override void AlterDatabase (DatabaseAlterSchema database)
		{
			throw new NotImplementedException ();
		}

		//http://msdn2.microsoft.com/en-us/library/aa225939(SQL.80).aspx
		public override void AlterTable (TableAlterSchema table)
		{
			throw new NotImplementedException ();
		}

		//http://msdn2.microsoft.com/en-us/library/aa225939(SQL.80).aspx
		public override void AlterView (ViewAlterSchema view)
		{
			//ExecuteNonQuery (view.Definition);  //FIXME:
		}

		//http://msdn2.microsoft.com/en-us/library/aa225939(SQL.80).aspx
		public override void AlterProcedure (ProcedureAlterSchema procedure)
		{
			ExecuteNonQuery (procedure.NewSchema.Definition);
		}

		public override void AlterIndex (IndexAlterSchema index)
		{
			throw new NotImplementedException ();
		}
		
		//http://msdn2.microsoft.com/en-us/library/aa225939(SQL.80).aspx
		public override void AlterTrigger (TriggerAlterSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public override void AlterUser (UserAlterSchema user)
		{
			throw new NotImplementedException ();
		}
		
		//http://msdn2.microsoft.com/en-us/library/aa258843(SQL.80).aspx
		public override void DropDatabase (DatabaseSchema database)
		{
			using (IPooledDbConnection conn = connectionPool.Request ()) {
				using (IDbCommand command = conn.CreateCommand (string.Concat("DROP DATABASE ", database.Name)))
					try {
							command.ExecuteNonQuery ();
					} catch (Exception e) {
						QueryService.RaiseException (e);
					} finally {
						conn.Release ();
					}
			}
		}

		//http://msdn2.microsoft.com/en-us/library/aa258841(SQL.80).aspx
		public override void DropTable (TableSchema table)
		{
			ExecuteNonQuery (string.Concat("DROP TABLE ", table.Name));
		}

		//http://msdn2.microsoft.com/en-us/library/aa258835(SQL.80).aspx
		public override void DropView (ViewSchema view)
		{
			ExecuteNonQuery (string.Concat("DROP VIEW ", view.Name));
		}

		//http://msdn2.microsoft.com/en-us/library/aa258830(SQL.80).aspx
		public override void DropProcedure (ProcedureSchema procedure)
		{
			ExecuteNonQuery (string.Concat("DROP PROCEDURE ", procedure.Name));
		}

		//http://msdn2.microsoft.com/en-us/library/aa225939(SQL.80).aspx
		public override void DropIndex (IndexSchema index)
		{
			ExecuteNonQuery (string.Concat("DROP INDEX '", index.TableName, ".", index.Name, "'"));
		}
		
		//http://msdn2.microsoft.com/en-us/library/aa258846(SQL.80).aspx
		public override void DropTrigger (TriggerSchema trigger)
		{
			ExecuteNonQuery (string.Concat("DROP TRIGGER ", trigger.Name));
		}
		
		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		public override void RenameDatabase (DatabaseSchema database, string name)
		{
			Rename (database.Name, name, "DATABASE");
			
			database.Name = name;
			connectionPool.ConnectionContext.ConnectionSettings.Database = name;
		}

		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		public override void RenameTable (TableSchema table, string name)
		{
			Rename (table.Name, name, "OBJECT");
			table.Name = name;
		}

		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		public override void RenameView (ViewSchema view, string name)
		{
			Rename (view.Name, name, "OBJECT");
			view.Name = name;
		}

		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		public override void RenameProcedure (ProcedureSchema procedure, string name)
		{
			Rename (procedure.Name, name, "OBJECT");
			procedure.Name = name;
		}

		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		public override void RenameIndex (IndexSchema index, string name)
		{
			Rename (index.Name, name, "INDEX");
			index.Name = name;
		}
		
		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		public override void RenameTrigger (TriggerSchema trigger, string name)
		{
			Rename (trigger.Name, name, "OBJECT");
			trigger.Name = name;
		}
		
		public override string GetViewAlterStatement (ViewSchema view)
		{
			return String.Concat ("DROP VIEW ", view.Name, "; ", Environment.NewLine, view.Definition); 
		}
		
		public override string GetProcedureAlterStatement (ProcedureSchema procedure)
		{
			string sp;
			if (procedure.Definition.Substring (0, 6).ToLower () == "create")
				sp = string.Concat("ALTER", procedure.Definition.Substring (6));
			else
				sp = procedure.Definition;
			return sp;
		}
		
		protected string GetTableDefinition (TableSchema table)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat ("-- Table: {0}\n", table.Name);
			sb.AppendFormat ("-- DROP TABLE {0};\n\n", table.Name);
			sb.AppendFormat ("CREATE TABLE {0} (\n", table.Name);

			ColumnSchemaCollection columns = table.GetColumns();
			string[] parts = new string[columns.Count];
			int i = 0;
			foreach (ColumnSchema col in columns)
				parts[i++] = col.Definition;
			sb.Append (String.Join (",\n", parts));
				
			ConstraintSchemaCollection constraints = table.Constraints;
			parts = new string[constraints.Count];
			if (constraints.Count > 0)
				sb.Append (",\n");
			i = 0;
			foreach (ConstraintSchema constr in constraints)
				parts[i++] = "\t" + constr.Definition;
			sb.Append (String.Join (",\n", parts));
				
			sb.Append ("\n);\n");
			//sb.AppendFormat ("COMMENT ON TABLE {0} IS '{1}';", table.Name, table.Comment);
			return sb.ToString ();
		}

		private string GetSource (string objectName) 
		{
			LoggingService.LogDebug ("GetSource: " + objectName);
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				String.Format ("EXEC sp_helptext '{0}'", objectName)
			);
			StringBuilder sb = new StringBuilder ();
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ())
							sb.Append (r.GetString (0));
						r.Close ();
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();

			return sb.ToString ();
		}
		
		//http://msdn2.microsoft.com/en-US/library/aa238878(SQL.80).aspx
		private void Rename (string oldName, string newName, string type) 
		{
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateStoredProcedure (
				String.Format ("EXEC sp_rename '{0}', '{1}', '{2}'", oldName, newName, type)
			);
			try {
				using (command)
					command.ExecuteNonQuery ();
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
		}
		
		public override string GetMimeType ()
		{
			return "text/x-sqlserver";
		}

	}
}
