//
// Provider/NpgsqlDbProvider.cs
//
// Authors:
//   Christian Hergert	<chris@mosaix.net>
//
// Copyright (C) 2005 Mosaix Communications, Inc.
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

using Npgsql;

namespace Mono.Data.Sql
{
	/// <summary>
	/// Mono.Data.Sql provider for PostgreSQL databases.
	/// </summary>
	[Serializable]
	public class NpgsqlDbProvider : DbProviderBase
	{
		protected NpgsqlConnection connection = null;
		protected NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
		protected bool isConnectionStringWrong = false;
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		public NpgsqlDbProvider () : base ()
		{
		}
		
		public override string ProviderName {
			get {
				return "PostgreSQL Database";
			}
		}
		
		/// <summary>
		/// Constructor with ADO.NET Npgsql connection.
		/// </summary>
		public NpgsqlDbProvider (NpgsqlConnection conn)
		{
			connection = conn;
		}
		
		/// <summary>
		/// ADO.NET Connection
		/// </summary>
		public override IDbConnection Connection {
			get {
				if (connection == null)
					connection = new NpgsqlConnection();
				
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
		/// Last system OID used in postgres to monitor system vs user
		/// objects. This varies based on the connections Server Version.
		/// </summary>
		protected int LastSystemOID {
			get {
				int major = connection.ServerVersion.Major;
				int minor = connection.ServerVersion.Minor;
				
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
				NpgsqlCommand command = new NpgsqlCommand();
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
			
			NpgsqlCommand command = new NpgsqlCommand();
			command.Connection = connection;
			command.CommandText =
				"SELECT c.relname, n.nspname, u.usename, d.description "
				+ "FROM pg_class c "
				+ " LEFT JOIN pg_description d ON c.oid = d.objoid, "
				+ "pg_namespace n, pg_user u "
				+ "WHERE c.relnamespace = n.oid "
				+ "AND c.relowner = u.usesysid "
				+ "AND c.relkind='r' AND NOT EXISTS "
				+ "   (SELECT 1 FROM pg_rewrite r "
				+ "      WHERE r.ev_class = c.oid AND r.ev_type = '1') "
				+ "ORDER BY relname;";
			NpgsqlDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				TableSchema table = new TableSchema();
				table.Provider = this;
				table.Name = r.GetString(0);
				
				if (table.Name.Substring(0, 3) == "pg_" ||
					table.Name.Substring(0, 4) == "sql_")
				{
					table.IsSystemTable = true;
				}
				
				try { table.SchemaName = r.GetString(1); } catch {}
				try { table.OwnerName = r.GetString(2); } catch {}
				try { table.Comment = r.GetString(3); } catch {}
				
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
				
				sb.Append ("\n);\n");
				sb.AppendFormat ("COMMENT ON TABLE {0} IS '{1}';", table.Name, table.Comment);
				table.Definition = sb.ToString();
				collection.Add (table);
			}
			
			r.Close ();
			
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
			
			NpgsqlCommand command = new NpgsqlCommand();
			command.Connection = connection;
			command.CommandText = "SELECT a.attname, a.attnotnull, a.attlen, "
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
				+ "ORDER BY a.attnum;";
			NpgsqlDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				ColumnSchema column = new ColumnSchema();
				
				try { column.Name = r.GetString(0); } catch {}
				column.Provider = this;
				try { column.DataTypeName = r.GetString(3); } catch {}
				try { column.Default = r.GetString(4); } catch {}
				column.Comment = "";
				column.OwnerName = "";
				column.SchemaName = table.SchemaName;
				try { column.NotNull = r.GetBoolean(1); } catch {}
				try { column.Length = r.GetInt32(2); } catch {}
				
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
			
			r.Close ();
			
			return (ColumnSchema[]) collection.ToArray(typeof(ColumnSchema));
		}

		/// <summary>
		/// Get a collection of views from the system.
		/// </summary>
		public override ViewSchema[] GetViews()
		{
			ArrayList collection = new ArrayList();
			
			NpgsqlCommand command = new NpgsqlCommand();
			command.Connection = connection;
			command.CommandText =
				"SELECT v.schemaname, v.viewname, v.viewowner, v.definition,"
				+ " (c.oid <= " + LastSystemOID + "), "
				+ "(SELECT description from pg_description pd, "
				+ " pg_class pc WHERE pc.oid=pd.objoid AND pc.relname="
				+ " v.viewname) "
				+ "FROM pg_views v, pg_class c "
				+ "WHERE v.viewname = c.relname "
				+ "ORDER BY viewname";
			NpgsqlDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				ViewSchema view = new ViewSchema();
				view.Provider = this;
				
				try {
					view.Name = r.GetString(1);
					view.SchemaName = r.GetString(0);
					view.OwnerName = r.GetString(2);
					
					StringBuilder sb = new StringBuilder();
					sb.AppendFormat ("-- View: {0}\n", view.Name);
					sb.AppendFormat ("-- DROP VIEW {0};\n\n", view.Name);
					sb.AppendFormat ("CREATE VIEW {0} AS (\n", view.Name);
					string core = r.GetString(3);
					sb.AppendFormat ("  {0}\n);", core.Substring (0, core.Length-1));
					view.Definition = sb.ToString ();
					
					view.IsSystemView = (r.GetBoolean(4));
					view.Comment = r.GetString(5);
				} catch (Exception e) {
				}

				collection.Add(view);
			}
			
			r.Close ();
			
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
			
			NpgsqlCommand command = new NpgsqlCommand ();
			command.Connection = connection;
			command.CommandText =
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
				+ "     ORDER BY a.attnum;";
			NpgsqlDataReader r = command.ExecuteReader();
			
			while (r.Read()) {
				ColumnSchema column = new ColumnSchema();
				
				try {
					column.Name = r.GetString(0);
					column.Provider = this;
					column.DataTypeName = r.GetString(1);
					column.Default = "";
					column.SchemaName = view.SchemaName;
					column.Definition = "";
					column.NotNull = r.GetBoolean(3);
					column.Length = r.GetInt32(2);
				} catch {
				} finally {
					collection.Add(column);
				}
			}
			
			r.Close ();
			
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
			
			NpgsqlCommand command = new NpgsqlCommand ();
			command.Connection = connection;
			command.CommandText = String.Format (
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
				+ "1;", table.Name, table.SchemaName);
			NpgsqlDataReader r = command.ExecuteReader ();
			
			while (r.Read ()) {
				ConstraintSchema constraint = null;

				// XXX: Add support for Check constraints.
				switch (r.GetString(2)) {
					case "f":
						string match = @".*REFERENCES (.+)\(.*\).*";
						constraint = new ForeignKeyConstraintSchema ();
						if (Regex.IsMatch (r.GetString (1), match))
							(constraint as ForeignKeyConstraintSchema).ReferenceTableName
								= Regex.Match (r.GetString (1), match).Groups[0].Captures[0].Value;
						break;
					case "u":
						constraint = new UniqueConstraintSchema ();
						break;
					case "p":
					default:
						constraint = new PrimaryKeyConstraintSchema ();
						break;
				}
				
				constraint.Name = r.GetString (0);
				constraint.Definition = r.GetString (1);
				
				collection.Add (constraint);
			}
			
			r.Close ();
			
			return (ConstraintSchema[]) collection.ToArray (typeof(ConstraintSchema));
		}
		
		public override UserSchema[] GetUsers ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			NpgsqlCommand command = new NpgsqlCommand ();
			command.Connection = connection;
			command.CommandText = "SELECT * FROM pg_user;";
			NpgsqlDataReader r = command.ExecuteReader ();
			
			while (r.Read ()) {
				UserSchema user = new UserSchema ();
				
				user.Name = r.GetString (0);
				user.UserId = String.Format ("{0}", r.GetInt32(1));
				
				try   { user.Expires = r.GetDateTime (6); }
				catch { user.Expires = DateTime.MinValue; }
				
				user.Options["createdb"] = r.GetBoolean (2);
				user.Options["createuser"] = r.GetBoolean (3);
				user.Password = r.GetString (5);
				
				StringBuilder sb = new StringBuilder ();
				sb.AppendFormat ("-- User: \"{0}\"\n\n", user.Name);
				sb.AppendFormat ("-- DROP USER {0};\n\n", user.Name);
				sb.AppendFormat ("CREATE USER {0}", user.Name);
				sb.AppendFormat ("  WITH SYSID {0}", user.UserId);
				if (user.Password != "********")
					sb.AppendFormat (" ENCRYPTED PASSWORD {0}", user.Password);
				sb.AppendFormat (((bool) user.Options["createdb"]) ?
					" CREATEDB" : " NOCREATEDB");
				sb.AppendFormat (((bool) user.Options["createuser"]) ?
					" CREATEUSER" : " NOCREATEUSER");
				if (user.Expires != DateTime.MinValue)
					sb.AppendFormat (" VALID UNTIL {0}", user.Expires);
				sb.Append (";");
				user.Definition = sb.ToString ();
				
				collection.Add (user);
			}
			
			r.Close ();
			
			return (UserSchema[]) collection.ToArray (typeof (UserSchema));
		}
		
		public override ProcedureSchema[] GetProcedures ()
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			NpgsqlCommand command = new NpgsqlCommand ();
			command.Connection = connection;
			command.CommandText =
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
				+ "AND pc.prolang = pl.oid;";
			NpgsqlDataReader r = command.ExecuteReader ();
			
			while (r.Read ()) {
				ProcedureSchema procedure = new ProcedureSchema ();
				
				procedure.Provider = this;
				procedure.Name = r.GetString (0);
				procedure.Definition = r.GetString (3);
				procedure.LanguageName = r.GetString (2);
				
				try {
					if (r.GetInt32 (1) <= LastSystemOID)
						procedure.IsSystemProcedure = true;
				} catch {}
				
				collection.Add (procedure);
			}
			
			r.Close ();
			
			return (ProcedureSchema[]) collection.ToArray (typeof (ProcedureSchema)); 
		}
		
		public override ColumnSchema[] GetProcedureColumns (ProcedureSchema schema)
		{
			if (IsOpen == false && Open () == false)
				throw new InvalidOperationException ("Invalid connection");
			
			ArrayList collection = new ArrayList ();
			
			// FIXME: Won't work properly with overload functions.
			// Maybe check the number of columns in the parameters for
			// proper match.
			NpgsqlCommand command = new NpgsqlCommand ();
			command.Connection = connection;
			command.CommandText = String.Format (
				  "SELECT format_type (prorettype, NULL) "
				+ "FROM pg_proc pc, pg_language pl "
				+ "WHERE pc.prolang = pl.oid "
				+ "AND pc.proname = '{0}';", schema.Name);
			NpgsqlDataReader r = command.ExecuteReader ();
			
			while (r.Read ()) {
				ColumnSchema column = new ColumnSchema ();
				column.Provider = this;
				
				column.DataTypeName = r.GetString (0);
				column.Name = r.GetString (0);
				
				collection.Add (column);
			}
			
			r.Close ();
			
			return (ColumnSchema[]) collection.ToArray (typeof (ColumnSchema));
		}
	}
}
