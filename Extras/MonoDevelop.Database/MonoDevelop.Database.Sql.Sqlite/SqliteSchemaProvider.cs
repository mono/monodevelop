//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//   Ankit Jain  <radical@corewars.org>
//   Ben Motmans  <ben.motmans@gmail.com>
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
using System.Text;
using System.Data;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using MonoDevelop.Core;

namespace MonoDevelop.Database.Sql
{
	// see: http://www.sqlite.org/faq.html
	// http://www.sqlite.org/google-talk-slides/page-021.html
	public class SqliteSchemaProvider : AbstractSchemaProvider
	{
		public SqliteSchemaProvider (IConnectionPool connectionPool)
			: base (connectionPool)
		{
		}

		public override TableSchemaCollection GetTables ()
		{
			TableSchemaCollection tables = new TableSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT name, sql FROM sqlite_master WHERE type = 'table'"
			);
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							TableSchema table = new TableSchema (this);
		
							table.SchemaName = "main";
							table.Name = r.GetString (0);
							table.IsSystemTable = table.Name.StartsWith ("sqlite_");
							table.Definition = r.GetString (1);
							
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
				"PRAGMA table_info('" +  table.Name + "')"
			);
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ColumnSchema column = new ColumnSchema (this, table);

							column.Position = r.GetInt32 (0);
							column.Name = r.GetString (1);
							column.DataTypeName = r.GetString (2);
							column.IsNullable = r.GetInt32 (3) != 0;
							column.DefaultValue = r.IsDBNull (4) ? null : r.GetValue (4).ToString ();
			
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
		
		public override ViewSchemaCollection GetViews ()
		{
			ViewSchemaCollection views = new ViewSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (
				"SELECT name, sql FROM sqlite_master WHERE type = 'views'"
			);
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ViewSchema view = new ViewSchema (this);
		
							view.SchemaName = "main";
							view.Name = r.GetString (0);
							view.Definition = r.GetString (1);
							
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
		
		public override ConstraintSchemaCollection GetTableConstraints (TableSchema table)
		{
			return GetConstraints (table, null);
		}
		
		public override ConstraintSchemaCollection GetColumnConstraints (TableSchema table, ColumnSchema column)
		{
			return GetConstraints (table, column);
		}

		//http://www.sqlite.org/pragma.html
		public virtual ConstraintSchemaCollection GetConstraints (TableSchema table, ColumnSchema column)
		{
			if (table == null)
				throw new ArgumentNullException ("table");
			string columnName = column == null ? null : column.Name;
			
			ConstraintSchemaCollection constraints = new ConstraintSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			
			//fk and unique
			IDbCommand command = conn.CreateCommand ("SELECT name, tbl_name FROM sqlite_master WHERE sql IS NULL AND type = 'index'");
			try {
				using (command) {
					using (IDataReader r = command.ExecuteReader()) {
						while (r.Read ()) {
							ConstraintSchema constraint = null;
							
							if (r.IsDBNull (1) || r.GetString (1) == null) {
								constraint = new UniqueConstraintSchema (this);
							} else {
								ForeignKeyConstraintSchema fkc = new ForeignKeyConstraintSchema (this);
								fkc.ReferenceTableName = r.GetString (1);
								
								constraint = fkc;
							}
							constraint.Name = r.GetString (0);

							constraints.Add (constraint);
						}
						r.Close ();
					}
				}
				
				//pk, column
				if (columnName != null) {
					command = conn.CreateCommand (
						"PRAGMA table_info('" +  table.Name + "')"
					);
					using (command) {
						using (IDataReader r = command.ExecuteReader()) {
							while (r.Read ()) {
								if (r.GetInt32 (5) == 1 && r.GetString (1) == columnName) {
									PrimaryKeyConstraintSchema constraint = new PrimaryKeyConstraintSchema (this);
								
									ColumnSchema priColumn = new ColumnSchema (this, table);
									priColumn.Name = r.GetString (1);
									
									constraint.Columns.Add (priColumn);
									constraint.IsColumnConstraint = true;
									constraint.Name = "pk_" + table.Name + "_" + priColumn.Name;
									
									constraints.Add (constraint);
								}
							}
							r.Close ();
						}
					}
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			
			conn.Release ();

			return constraints;
		}
		
		public override void CreateDatabase (DatabaseSchema database)
		{
			//TODO: error if db exists
			Runtime.LoggingService.Error ("CREATE START");
			
			SqliteConnection conn = new SqliteConnection ("URI=file:" + database.Name + ";Version=3;");
			conn.Open ();
			conn.Close ();
			
			Runtime.LoggingService.Error ("CREATE STOP");
		}

		//http://www.sqlite.org/lang_createtable.html
		public override string GetTableCreateStatement (TableSchema table)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("CREATE TABLE ");
			sb.Append (table.Name);
			sb.Append (" ( ");

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
				if (column.HasDefaultValue)
					sb.Append (" DEFAULT " + column.DefaultValue == null ? "NULL" : column.DefaultValue.ToString ()); //TODO: '' chars if string
				
				//list all column constrains for this type
				foreach (ConstraintSchema constraint in column.Constraints) {
					sb.Append (" ");
					sb.Append (GetConstraintString (constraint));
				}
			}
			
			//table constraints
			foreach (ConstraintSchema constraint in table.Constraints) {
				sb.Append (", ");
				sb.Append (GetConstraintString (constraint));
			}
			
			sb.Append (" );");
			
			foreach (TriggerSchema trigger in table.Triggers) {
				sb.Append (Environment.NewLine);
				sb.Append (GetTriggerCreateStatement (trigger));				
			}
			
			return sb.ToString ();
		}
		
		protected virtual string GetConstraintString (ConstraintSchema constraint)
		{
			//PRIMARY KEY [sort-order] [ conflict-clause ] [AUTOINCREMENT]
			//UNIQUE [ conflict-clause ]
			//CHECK ( expr )
			//COLLATE collation-name
			
			StringBuilder sb = new StringBuilder ();
			sb.Append ("CONSTRAINT ");
			sb.Append (constraint.Name);
			sb.Append (' ');
			
			switch (constraint.ConstraintType) {
			case ConstraintType.PrimaryKey:
				//PrimaryKeyConstraintSchema pk = constraint as PrimaryKeyConstraintSchema;
				sb.Append ("PRIMARY KEY"); //TODO: auto inc + sort
				break;
			case ConstraintType.Unique:
				//UniqueConstraintSchema u = constraint as UniqueConstraintSchema;
				sb.Append ("UNIQUE");
				break;
			case ConstraintType.Check:
				CheckConstraintSchema chk = constraint as CheckConstraintSchema;
				sb.Append ("CHECK (");
				sb.Append (chk.Source);
				sb.Append (")");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			return sb.ToString ();
		}

		//http://www.sqlite.org/lang_createview.html
		public override void CreateView (ViewSchema view)
		{
			IPooledDbConnection conn = connectionPool.Request ();
			IDbCommand command = conn.CreateCommand (view.Definition);
			using (command)
				conn.ExecuteNonQuery (command);
			conn.Release ();
		}

		//http://www.sqlite.org/lang_createindex.html
		public override void CreateIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		//http://www.sqlite.org/lang_createtrigger.html
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
			case TriggerFireType.ForEachStatement:
				sb.Append (" FOR EACH ROW ");
				break;
			default:
				throw new NotImplementedException ();
			}
			
			sb.Append (Environment.NewLine);
			sb.Append ("BEGIN ");
			sb.Append (Environment.NewLine);
			sb.Append (trigger.Source);
			sb.Append (' ');
			sb.Append (Environment.NewLine);
			sb.Append ("END;");
			
			return sb.ToString ();
		}

		//http://www.sqlite.org/lang_altertable.html
		//http://www.sqlite.org/lang_vacuum.html
		public override void AlterTable (TableSchema table)
		{
			throw new NotImplementedException ();
		}
		
		public override void DropDatabase (DatabaseSchema db)
		{
			connectionPool.Close ();
			System.IO.File.Delete (db.Name);
		}

		//http://www.sqlite.org/lang_droptable.html
		public override void DropTable (TableSchema table)
		{
			ExecuteNonQuery ("DROP TABLE IF EXISTS " + table.Name);
		}

		//http://www.sqlite.org/lang_dropview.html
		public override void DropView (ViewSchema view)
		{
			ExecuteNonQuery ("DROP VIEW IF EXISTS " + view.Name);
		}

		//http://www.sqlite.org/lang_dropindex.html
		public override void DropIndex (IndexSchema index)
		{
			ExecuteNonQuery ("DROP INDEX IF EXISTS " + index.Name);
		}
		
		//http://www.sqlite.org/lang_droptrigger.html
		public override void DropTrigger (TriggerSchema trigger)
		{
			ExecuteNonQuery ("DROP TRIGGER IF EXISTS " + trigger.Name);
		}

		//http://www.sqlite.org/lang_altertable.html
		public override void RenameTable (TableSchema table, string name)
		{
			ExecuteNonQuery ("ALTER TABLE " + table.Name + " RENAME TO " + name);
			
			table.Name = name;
		}
		
		public override string GetViewAlterStatement (ViewSchema view)
		{
			return String.Concat ("DROP VIEW IF EXISTS ", view.Name, "; ", Environment.NewLine, view.Definition); 
		}
	}
}
