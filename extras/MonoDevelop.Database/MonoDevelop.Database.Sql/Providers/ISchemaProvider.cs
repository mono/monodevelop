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
		IConnectionPool ConnectionPool { get; }

		DatabaseSchemaCollection GetDatabases ();

		TableSchemaCollection GetTables ();
		
		ColumnSchemaCollection GetTableColumns (TableSchema table);

		ViewSchemaCollection GetViews ();

		ColumnSchemaCollection GetViewColumns (ViewSchema view);

		ProcedureSchemaCollection GetProcedures ();
		
		ParameterSchemaCollection GetProcedureParameters (ProcedureSchema procedure);

		ConstraintSchemaCollection GetTableConstraints (TableSchema table);
		
		ConstraintSchemaCollection GetColumnConstraints (TableSchema table, ColumnSchema column);
		
		IndexSchemaCollection GetTableIndexes (TableSchema table);
		
		ColumnSchemaCollection GetTableIndexColumns (TableSchema table, IndexSchema index);
		
		TriggerSchemaCollection GetTableTriggers (TableSchema table);

		UserSchemaCollection GetUsers ();
		
		DataTypeSchemaCollection GetDataTypes ();
		
		DataTypeSchema GetDataType (string name);
		
		void CreateDatabase (DatabaseSchema database);
		void CreateTable (TableSchema table);
		void CreateView (ViewSchema view);
		void CreateProcedure (ProcedureSchema procedure);
		void CreateIndex (IndexSchema index);
		void CreateTrigger (TriggerSchema trigger);
		void CreateUser (UserSchema user);
		
		void AlterDatabase (DatabaseSchema database);
		void AlterTable (TableSchema table);
		void AlterView (ViewSchema view);
		void AlterProcedure (ProcedureSchema procedure);
		void AlterIndex (IndexSchema index);
		void AlterTrigger (TriggerSchema trigger);
		void AlterUser (UserSchema user);
		
		void DropDatabase (DatabaseSchema database);
		void DropTable (TableSchema table);
		void DropView (ViewSchema view);
		void DropProcedure (ProcedureSchema procedure);
		void DropIndex (IndexSchema index);
		void DropTrigger (TriggerSchema trigger);
		void DropUser (UserSchema user);

		void RenameDatabase (DatabaseSchema database, string name);
		void RenameTable (TableSchema table, string name);
		void RenameView (ViewSchema view, string name);
		void RenameProcedure (ProcedureSchema procedure, string name);
		void RenameIndex (IndexSchema index, string name);
		void RenameTrigger (TriggerSchema trigger, string name);
		void RenameUser (UserSchema user, string name);
		
		string GetTableCreateStatement (TableSchema table);
		string GetTableAlterStatement (TableSchema table);
		string GetViewAlterStatement (ViewSchema view);
		string GetProcedureAlterStatement (ProcedureSchema procedure);
		
		DatabaseSchema GetNewDatabaseSchema (string name);
		TableSchema GetNewTableSchema (string name);
		ViewSchema GetNewViewSchema (string name);
		ProcedureSchema GetNewProcedureSchema (string name);
		ColumnSchema GetNewColumnSchema (string name, ISchema parent);
		ParameterSchema GetNewParameterSchema (string name);
		CheckConstraintSchema GetNewCheckConstraintSchema (string name);
		UniqueConstraintSchema GetNewUniqueConstraintSchema (string name);
		PrimaryKeyConstraintSchema GetNewPrimaryKeyConstraintSchema (string name);
		ForeignKeyConstraintSchema GetNewForeignKeyConstraintSchema (string name);
		UserSchema GetNewUserSchema (string name);
		TriggerSchema GetNewTriggerSchema (string name);
		
		bool IsValidName (string name);
	}
}
