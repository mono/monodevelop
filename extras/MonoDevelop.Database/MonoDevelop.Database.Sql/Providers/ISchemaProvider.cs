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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public interface ISchemaProvider
	{
		bool CanEdit { get; }
		
		IConnectionPool ConnectionPool { get; }
		
		DatabaseSchemaCollection GetDatabases ();

		TableSchemaCollection GetTables ();	
		ColumnSchemaCollection GetTableColumns (TableSchema table);
		ConstraintSchemaCollection GetTableConstraints (TableSchema table);
		IndexSchemaCollection GetTableIndexes (TableSchema table);
		ColumnSchemaCollection GetTableIndexColumns (TableSchema table, IndexSchema index);
		TriggerSchemaCollection GetTableTriggers (TableSchema table);
		
		ConstraintSchemaCollection GetColumnConstraints (TableSchema table, ColumnSchema column);

		ViewSchemaCollection GetViews ();
		ColumnSchemaCollection GetViewColumns (ViewSchema view);

		ProcedureSchemaCollection GetProcedures ();
		ParameterSchemaCollection GetProcedureParameters (ProcedureSchema procedure);

		UserSchemaCollection GetUsers ();
		
		DataTypeSchemaCollection GetDataTypes ();		
		DataTypeSchema GetDataType (string name);

		string GetTableCreateStatement (TableSchema table);

		bool IsValidName (string name);
		
		bool IsSchemaActionSupported (SchemaType type, SchemaActions action);
		
		AggregateSchema CreateAggregateSchema (string name);
		CheckConstraintSchema CreateCheckConstraintSchema (string name);
		ColumnSchema CreateColumnSchema (ISchema parent, string name);
		DatabaseSchema CreateDatabaseSchema (string name);
		ForeignKeyConstraintSchema CreateForeignKeyConstraintSchema (string name);
		GroupSchema CreateGroupSchema (string name);
		IndexSchema CreateIndexSchema (string name);
		LanguageSchema CreateLanguageSchema (string name);
		OperatorSchema CreateOperatorSchema (string name);
		ParameterSchema CreateParameterSchema (string name);
		PrimaryKeyConstraintSchema CreatePrimaryKeyConstraintSchema (string name);
		PrivilegeSchema CreatePrivilegeSchema (string name);
		ProcedureSchema CreateProcedureSchema (string name);
		RoleSchema CreateRoleSchema (string name);
		RuleSchema CreateRuleSchema (string name);
		SchemaSchema CreateSchemaSchema (string name);
		SequenceSchema CreateSequenceSchema (string name);
		TableSchema CreateTableSchema (string name);
		TriggerSchema CreateTriggerSchema (string name);
		UniqueConstraintSchema CreateUniqueConstraintSchema (string name);
		UserSchema CreateUserSchema (string name);
		ViewSchema CreateViewSchema (string name);
		string GetMimeType ();
		string GetSelectQuery (TableSchema table);
		string GetInsertQuery (TableSchema table);
		string GetUpdateQuery (TableSchema table);
		string GetDeleteQuery (TableSchema table);
	}
}
