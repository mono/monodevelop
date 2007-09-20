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
using System.Text;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractSchemaProvider : ISchemaProvider
	{
		protected IConnectionPool connectionPool;
		
		protected string databasesCollectionString = "Databases";
		protected string tablesCollectionString = "Tables";
		protected string viewsCollectionString = "Views";
		protected string proceduresCollectionString = "Procedures";
		protected string tableColumnsCollectionString = "Columns";
		protected string viewColumnsCollectionString = "ViewColumns";
		protected string procedureParametersCollectionString = "Procedure Parameters";
		protected string usersCollectionString = "Users";
		protected string indexesCollectionString = "Indexes";
		protected string indexColumnsCollectionString = "IndexColumns";
		protected string foreignKeysCollectionString = "Foreign Keys";
		protected string triggersCollectionString = "Triggers";
		protected string dataTypesCollectionString = "DataTypes";
		
		protected string[] databaseItemStrings = new string[] { "DATABASE_NAME" };
		protected string[] tableItemStrings = new string[] { "TABLE_SCHEMA", "TABLE_NAME", "TABLE_COMMENT" };
		protected string[] viewItemStrings = new string[] { "TABLE_SCHEMA", "TABLE_NAME", "TABLE_COMMENT" };
		protected string[] procedureItemStrings = new string[] { "ROUTINE_SCHEMA", "ROUTINE_NAME" };
		protected string[] tableColumnItemStrings = new string[] { "COLUMN_NAME", "COLUMN_DEFAULT", "COLUMN_HASDEFAULT", "IS_NULLABLE", "ORDINAL_POSITION", "NUMERIC_PRECISION", "NUMERIC_SCALE", "DATA_TYPE" };
		protected string[] viewColumnItemStrings = new string[] { "COLUMN_NAME" };
		protected string[] procedureParameterItemStrings = new string[] { "PARAMETER_NAME", "DATA_TYPE", "ORDINAL_POSITION", "PARAMETER_MODE" };
		protected string[] userItemStrings = new string[] { "user_name" };
		protected string[] indexItemStrings = new string[] {  };
		protected string[] indexColumnItemStrings = new string[] {  };
		protected string[] foreignKeyItemStrings = new string[] {  };
		protected string[] triggerItemStrings = new string[] {  };
		protected string[] dataTypeItemStrings = new string[] { "TypeName", "ColumnSize", "CreateFormat", "CreateParameters", "DataType", "IsAutoIncrementable", "IsFixedLength", "IsNullable", "MaximumScale", "MinimumScale" };
		
		protected AbstractSchemaProvider (IConnectionPool connectionPool)
		{
			if (connectionPool == null)
				throw new ArgumentNullException ("connectionPool");
			
			this.connectionPool = connectionPool;
		}
		
		public IConnectionPool ConnectionPool {
			get { return connectionPool; }
		}
		
		public virtual DatabaseSchemaCollection GetDatabases ()
		{
			DatabaseSchemaCollection collection = new DatabaseSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: name
				DataTable dt = conn.GetSchema (databasesCollectionString);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetDatabase (row));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			
			conn.Release ();
			
			return collection;
		}
		
		protected virtual DatabaseSchema GetDatabase (DataRow row)
		{
			DatabaseSchema schema = new DatabaseSchema (this);
			schema.Name = GetRowString (row, databaseItemStrings[0]);
			return schema;
		}

		public virtual TableSchemaCollection GetTables ()
		{
			TableSchemaCollection collection = new TableSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, table, table type
				DataTable dt = conn.GetSchema (tablesCollectionString, null, connectionPool.ConnectionContext.ConnectionSettings.Database);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetTable (row));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual TableSchema GetTable (DataRow row)
		{
			TableSchema schema = new TableSchema (this);

			schema.SchemaName = GetRowString (row, tableItemStrings[0]);
			schema.Name = GetRowString (row, tableItemStrings[1]);
			schema.Comment = GetRowString (row, tableItemStrings[2]);
			
			return schema;
		}

		public virtual ColumnSchemaCollection GetTableColumns (TableSchema table)
		{
			ColumnSchemaCollection collection = new ColumnSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			//restrictions: database, schema, table, column
			try {
				DataTable dt = conn.GetSchema (tableColumnsCollectionString, null, table.SchemaName, table.Name);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetTableColumn (row, table));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ColumnSchema GetTableColumn (DataRow row, TableSchema table)
		{
			ColumnSchema schema = new ColumnSchema (this, table);

			schema.SchemaName = table.SchemaName;
			schema.Name = GetRowString (row, tableColumnItemStrings[0]);
			schema.DefaultValue = GetRowString (row, tableColumnItemStrings[1]);
			schema.HasDefaultValue = GetRowBool (row, tableColumnItemStrings[2]);
			schema.IsNullable = GetRowBool (row, tableColumnItemStrings[3]);
			schema.Position = GetRowInt (row, tableColumnItemStrings[4]);
			schema.DataTypeName = GetRowString (row, tableColumnItemStrings[7]);
			schema.DataType.ScaleRange.Default = GetRowInt (row, tableColumnItemStrings[6]);
			schema.DataType.PrecisionRange.Default = GetRowInt (row, tableColumnItemStrings[5]);
			
			return schema;
		}

		public virtual ViewSchemaCollection GetViews ()
		{
			ViewSchemaCollection collection = new ViewSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, table
				DataTable dt = conn.GetSchema (viewsCollectionString, null, connectionPool.ConnectionContext.ConnectionSettings.Database);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetView (row));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ViewSchema GetView (DataRow row)
		{
			ViewSchema schema = new ViewSchema (this);
	
			schema.SchemaName = GetRowString (row, viewItemStrings[0]);
			schema.Name = GetRowString (row, viewItemStrings[1]);
			schema.Comment = GetRowString (row, viewItemStrings[2]);
			
			return schema;
		}

		public virtual ColumnSchemaCollection GetViewColumns (ViewSchema view)
		{
			ColumnSchemaCollection collection = new ColumnSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, table, column
				DataTable dt = conn.GetSchema (viewColumnsCollectionString, null, view.SchemaName, view.Name);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetViewColumn (row, view));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ColumnSchema GetViewColumn (DataRow row, ViewSchema view)
		{
			ColumnSchema schema = new ColumnSchema (this, view);
			
			schema.SchemaName = view.SchemaName;
			schema.Name = GetRowString (row, viewColumnItemStrings[0]);
			
			return schema;
		}

		public virtual ProcedureSchemaCollection GetProcedures ()
		{
			ProcedureSchemaCollection collection = new ProcedureSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, name, type
				DataTable dt = conn.GetSchema (proceduresCollectionString, null, connectionPool.ConnectionContext.ConnectionSettings.Database);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetProcedure (row));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ProcedureSchema GetProcedure (DataRow row)
		{
			ProcedureSchema schema = new ProcedureSchema (this);
			
			schema.SchemaName = GetRowString (row, procedureItemStrings[0]);
			schema.Name = GetRowString (row, procedureItemStrings[1]);
			
			return schema;
		}
		
		public virtual ParameterSchemaCollection GetProcedureParameters (ProcedureSchema procedure)
		{
			ParameterSchemaCollection collection = new ParameterSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, name, type, parameter
				DataTable dt = conn.GetSchema (procedureParametersCollectionString, null, procedure.SchemaName, procedure.Name);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetProcedureParameter (row, procedure));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ParameterSchema GetProcedureParameter (DataRow row, ProcedureSchema procedure)
		{
			ParameterSchema schema = new ParameterSchema (this);
			
			schema.SchemaName = procedure.SchemaName;
			schema.Name = GetRowString (row, procedureParameterItemStrings[0]);
			schema.DataTypeName = GetRowString (row, procedureParameterItemStrings[1]);
			schema.Position = GetRowInt (row, procedureParameterItemStrings[2]);
			
			string paramType = GetRowString (row, procedureParameterItemStrings[3]);
			schema.ParameterType = String.Compare (paramType, "IN", true) == 0 ?
				ParameterType.In : (String.Compare (paramType, "OUT", true) == 0 ?
				ParameterType.Out : ParameterType.InOut);
			
			return schema;
		}

		public virtual ConstraintSchemaCollection GetTableConstraints (TableSchema table)
		{
			ConstraintSchemaCollection collection = new ConstraintSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, table, name
				DataTable dt = conn.GetSchema (foreignKeysCollectionString, null, connectionPool.ConnectionContext.ConnectionSettings.Database);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetTableConstraint (row, table));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ConstraintSchema GetTableConstraint (DataRow row, TableSchema table)
		{
			return null;
		}
		
		public virtual ColumnSchemaCollection GetTableIndexColumns (TableSchema table, IndexSchema index)
		{
			ColumnSchemaCollection collection = new ColumnSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, table, ConstraintName, column
				DataTable dt = conn.GetSchema (indexColumnsCollectionString, null, table.SchemaName, table.Name, index.Name);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetTableIndexColumn (row, table, index));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual ColumnSchema GetTableIndexColumn (DataRow row, TableSchema table, IndexSchema index)
		{
			ColumnSchema schema = new ColumnSchema (this, table);
			
			return schema;
		}
		
		public virtual IndexSchemaCollection GetTableIndexes (TableSchema table)
		{
			IndexSchemaCollection collection = new IndexSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, table, name
				DataTable dt = conn.GetSchema (indexesCollectionString, null, table.SchemaName, table.Name);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetTableIndex (row, table));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual IndexSchema GetTableIndex (DataRow row, TableSchema table)
		{
			IndexSchema schema = new IndexSchema (this);
			
			return schema;
		}
		
		public virtual ConstraintSchemaCollection GetColumnConstraints (TableSchema table, ColumnSchema column)
		{
			throw new NotImplementedException ();
		}

		public virtual UserSchemaCollection GetUsers ()
		{
			UserSchemaCollection collection = new UserSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: name
				DataTable dt = conn.GetSchema (usersCollectionString);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetUser (row));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual UserSchema GetUser (DataRow row)
		{
			UserSchema schema = new UserSchema (this);
			schema.Name = GetRowString (row, userItemStrings[0]);
			return schema;
		}
		
		public virtual DataTypeSchemaCollection GetDataTypes ()
		{
			DataTypeSchemaCollection collection = new DataTypeSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: name
				DataTable dt = conn.GetSchema (dataTypesCollectionString);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetDataType (row));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			
			conn.Release ();
			
			return collection;
		}
		
		protected virtual DataTypeSchema GetDataType (DataRow row)
		{
			DataTypeSchema schema = new DataTypeSchema (this);
			schema.Name = GetRowString (row, dataTypeItemStrings[0]);
			schema.LengthRange = new Range (GetRowInt (row, dataTypeItemStrings[1]));
			schema.CreateFormat = GetRowString (row, dataTypeItemStrings[2]);
			schema.CreateParameters = GetRowString (row, dataTypeItemStrings[3]);
			schema.DataType = Type.GetType (GetRowString (row, dataTypeItemStrings[4]), false, false);
			schema.IsAutoincrementable = GetRowBool (row, dataTypeItemStrings[5]);
			schema.IsFixedLength = GetRowBool (row, dataTypeItemStrings[6]);
			schema.IsNullable = GetRowBool (row, dataTypeItemStrings[7]);
			schema.ScaleRange = new Range (GetRowInt (row, dataTypeItemStrings[9]), GetRowInt (row, dataTypeItemStrings[8]));
			schema.PrecisionRange = new Range (0);
			
			ProvideDataTypeInformation (schema);
			
			return schema;
		}

		protected virtual void ProvideDataTypeInformation (DataTypeSchema schema)
		{
		}

		public virtual DataTypeSchema GetDataType (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			
			DataTypeSchema schema = null;
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: name
				DataTable dt = conn.GetSchema (dataTypesCollectionString, name);
				if (dt.Rows.Count > 0)
					schema = GetDataType (dt.Rows[0]);
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return schema;
		}
		
		public virtual TriggerSchemaCollection GetTableTriggers (TableSchema table)
		{
			TriggerSchemaCollection collection = new TriggerSchemaCollection ();
			
			IPooledDbConnection conn = connectionPool.Request ();
			try {
				//restrictions: database, schema, name, EventObjectTable
				DataTable dt = conn.GetSchema (triggersCollectionString, null, table.SchemaName, null, table.Name);
				for (int r = 0; r < dt.Rows.Count; r++) {
					DataRow row = dt.Rows[r];
					collection.Add (GetTableTrigger (row, table));
				}
			} catch (Exception e) {
				QueryService.RaiseException (e);
			}
			conn.Release ();
			
			return collection;
		}
		
		protected virtual TriggerSchema GetTableTrigger (DataRow row, TableSchema table)
		{
			TriggerSchema schema = new TriggerSchema (this);
			schema.TableName = table.Name;
			
			return schema;
		}
		
		public virtual void CreateDatabase (DatabaseSchema database)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateTable (TableSchema table)
		{
			string sql = GetTableCreateStatement (table);
			ExecuteNonQuery (sql);
		}

		public virtual void CreateView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateProcedure (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void CreateTrigger (TriggerSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public virtual void CreateUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void AlterDatabase (DatabaseSchema database)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterTable (TableSchema table)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterProcedure (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void AlterTrigger (TriggerSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public virtual void AlterUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void DropDatabase (DatabaseSchema database)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropTable (TableSchema table)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropView (ViewSchema view)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropProcedure (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropIndex (IndexSchema index)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void DropTrigger (TriggerSchema trigger)
		{
			throw new NotImplementedException ();
		}

		public virtual void DropUser (UserSchema user)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void RenameDatabase (DatabaseSchema database, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameTable (TableSchema table, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameView (ViewSchema view, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameProcedure (ProcedureSchema procedure, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameIndex (IndexSchema index, string name)
		{
			throw new NotImplementedException ();
		}
		
		public virtual void RenameTrigger (TriggerSchema trigger, string name)
		{
			throw new NotImplementedException ();
		}

		public virtual void RenameUser (UserSchema user, string name)
		{
			throw new NotImplementedException ();
		}
		
		public virtual DatabaseSchema GetNewDatabaseSchema (string name)
		{
			DatabaseSchema schema = new DatabaseSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual TableSchema GetNewTableSchema (string name)
		{
			TableSchema schema = new TableSchema (this, name);
			return schema;
		}

		public virtual ViewSchema GetNewViewSchema (string name)
		{
			ViewSchema schema = new ViewSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual ProcedureSchema GetNewProcedureSchema (string name)
		{
			ProcedureSchema schema = new ProcedureSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual ColumnSchema GetNewColumnSchema (string name, ISchema parent)
		{
			ColumnSchema schema = new ColumnSchema (this, parent, name);
			return schema;
		}
	
		public virtual ParameterSchema GetNewParameterSchema (string name)
		{
			ParameterSchema schema = new ParameterSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual CheckConstraintSchema GetNewCheckConstraintSchema (string name)
		{
			CheckConstraintSchema schema = new CheckConstraintSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual UniqueConstraintSchema GetNewUniqueConstraintSchema (string name)
		{
			UniqueConstraintSchema schema = new UniqueConstraintSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual PrimaryKeyConstraintSchema GetNewPrimaryKeyConstraintSchema (string name)
		{
			PrimaryKeyConstraintSchema schema = new PrimaryKeyConstraintSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual ForeignKeyConstraintSchema GetNewForeignKeyConstraintSchema (string name)
		{
			ForeignKeyConstraintSchema schema = new ForeignKeyConstraintSchema (this);
			schema.Name = name;
			return schema;
		}

		public virtual UserSchema GetNewUserSchema (string name)
		{
			UserSchema schema = new UserSchema (this);
			schema.Name = name;
			return schema;
		}
		
		public virtual TriggerSchema GetNewTriggerSchema (string name)
		{
			TriggerSchema schema = new TriggerSchema (this);
			schema.Name = name;
			return schema;
		}
		
		public virtual string GetTableCreateStatement (TableSchema table)
		{
			throw new NotImplementedException ();
		}
		
		public virtual string GetTableAlterStatement (TableSchema table)
		{
			throw new NotImplementedException ();
		}
		
		public virtual string GetViewAlterStatement (ViewSchema view)
		{
			throw new NotImplementedException ();
		}
		
		public virtual string GetProcedureAlterStatement (ProcedureSchema procedure)
		{
			throw new NotImplementedException ();
		}
		
		public virtual bool IsValidName (string name)
		{
			return true;
		}
		
		protected int GetCheckedInt32 (IDataReader reader, int field)
		{
			if (reader.IsDBNull (field))
				return 0;

			object o = reader.GetValue (field);
			int res = 0;
			if (int.TryParse (o.ToString (), out res))
				return res;
			return 0;
		}
			    
		protected string GetCheckedString (IDataReader reader, int field)
		{
			if (reader.IsDBNull (field))
				return null;

			return reader.GetValue (field).ToString ();
		}
		
		protected virtual object GetRowObject (DataRow row, string name)
		{
			try {
				return row[name];
			} catch {
				return null;
			}
		}
		
		protected virtual string GetRowString (DataRow row, string name)
		{
			try {
				return row[name].ToString ();
			} catch {
				return null;
			}
		}
		
		protected virtual int GetRowInt (DataRow row, string name)
		{
			try {
				return (int)row[name];
			} catch {
				return 0;
			}
		}
		
		protected virtual bool GetRowBool (DataRow row, string name)
		{
			try {
				return (bool)row[name];
			} catch {
				return false;
			}
		}
		
		protected virtual string GetColumnsString (ColumnSchemaCollection columns, bool includeParens)
		{
			StringBuilder sb = new StringBuilder ();
			bool first = true;
			if (includeParens)
				sb.Append ("(");
			foreach (ColumnSchema column in columns) {
				if (first)
					first = false;
				else
					sb.Append (",");
				sb.Append (column.Name);
			}
			if (includeParens)
				sb.Append (")");
			return sb.ToString ();
		}
		
		protected virtual int ExecuteNonQuery (string sql)
		{
			IPooledDbConnection conn = connectionPool.Request ();
			int result = conn.ExecuteNonQuery (sql);
			conn.Release ();
			return result;
		}
	}
}
