// DatabaseType.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Data;
using System.Text;

using Mono.Data.SqliteClient;
using Hyena.Data.Sqlite;

namespace MonoDevelop.Projects.Dom.Database
{
	public class DatabaseType : DomType
	{
		internal const string Table          = "Types";
		internal const string InnerTypeTable = "InnerTypes";
		internal const string SubTypeTable   = "SubTypes";
		
		CodeCompletionDatabase db;
		CompilationUnit unit;
		long typeId;
		long unitId;
		
		public override ICompilationUnit CompilationUnit {
			get {
				return unit;
			}
		}
		
		internal CodeCompletionDatabase Database {
			get {
				return db;
			}
		}
		
		internal long TypeId {
			get {
				return typeId;
			}
		}
		
		public long UnitId {
			get {
				return unitId;
			}
		}
		
		public DatabaseType (CodeCompletionDatabase db, long unitId, long typeId, string namespaceName, string name, ClassType classType)
		{
			this.db        = db;
			this.typeId    = typeId;
			this.unitId    = unitId;
			this.Namespace = namespaceName;
			this.Name      = name;
			this.ClassType = classType;
			
			IDataReader reader = db.Connection.Query (String.Format (@"SELECT Name FROM {0} WHERE UnitID={1}", CodeCompletionDatabase.CompilationUnitTable, unitId));
			if (reader.Read ()) {
				unit = new CompilationUnit (SqliteUtils.FromDbFormat<string> (reader[0]));
			} else {
				unit = null;
			}
		}
		
		public override IEnumerable<IMember> Members {
			get {
				foreach (IMember member in InnerTypes) {
					yield return member;
				}
				foreach (IMember member in Fields) {
					yield return member;
				}
				foreach (IMember member in Properties) {
					yield return member;
				}
				foreach (IMember member in Methods) {
					yield return member;
				}
				foreach (IMember member in Events) {
					yield return member;
				}
			}
		}
		
		public override IEnumerable<IType> InnerTypes {
			get {
				return ReadInnerTypeList (this);
			}
		}

		public override IEnumerable<IField> Fields {
			get {
				return DatabaseField.ReadList (this);
			}
		}

		public override IEnumerable<IProperty> Properties {
			get {
				return DatabaseProperty.ReadList (this);
			}
		}

		public override IEnumerable<IMethod> Methods {
			get {
				return DatabaseMethod.ReadList (this);
			}
		}

		public override IEnumerable<IEvent> Events {
			get {
				return DatabaseEvent.ReadList (this);
			}
		}

		public static void CheckTables (CodeCompletionDatabase db)
		{
			if (!db.Connection.TableExists (Table)) {
				db.Connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						TypeID INTEGER PRIMARY KEY AUTOINCREMENT,
						UnitID INTEGER,
						NamespaceID INTEGER,
						Name TEXT,
						ClassType INTEGER
						)", Table
				));
			}
			
			if (!db.Connection.TableExists (InnerTypeTable)) {
				db.Connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						TypeID INTEGER,
						InnerTypeID INTEGER
					)", InnerTypeTable
				));
			}
			
			if (!db.Connection.TableExists (SubTypeTable)) {
				db.Connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						BaseTypeID INTEGER,
						TypeID INTEGER
					)", SubTypeTable
				));
			}
		}
		
		public void Delete ()
		{
			foreach (DatabaseField field in Fields) {
				field.Delete ();
			}
			
			foreach (DatabaseMethod method in Methods) {
				method.Delete ();
			}
			
			foreach (DatabaseProperty property in Properties) {
				property.Delete ();
			}
			
			foreach (DatabaseEvent evt in Events) {
				evt.Delete ();
			}
			
			foreach (DatabaseType innerType in InnerTypes) {
				innerType.Delete ();
			}
			
			db.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1}", SubTypeTable, typeId));
			db.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1}", InnerTypeTable, typeId));
			db.Connection.Execute (String.Format (@"DELETE FROM {0} WHERE TypeID={1}", Table, typeId));
		}
		
		public static long Insert (CodeCompletionDatabase db, long unitId, IType type)
		{
			long namespaceID = db.GetNamespaceID (type.Namespace);
			long typeId = db.Connection.Execute (String.Format (@"INSERT INTO {0} (UnitID, NamespaceID, Name, ClassType) VALUES ({1}, {2}, '{3}', {4})", 
				Table,
				unitId,
				namespaceID,
				type.Name,
				(byte)type.ClassType
			));
			
			foreach (IReturnType baseType in type.BaseTypes) {
				db.Connection.Execute (String.Format (@"INSERT INTO {0} (BaseTypeID, TypeID) VALUES ({1}, {2})", 
					SubTypeTable,
					db.GetReturnTypeID (baseType),
					typeId
				));
			}
			
			foreach (IField field in type.Fields) {
				DatabaseField.Insert (db, typeId, field);
			}
			
			foreach (IMethod method in type.Methods) {
				DatabaseMethod.Insert (db, typeId, method);
			}
			
			foreach (IProperty property in type.Properties) {
				DatabaseProperty.Insert (db, typeId, property);
			}
			
			foreach (IEvent evt in type.Events) {
				DatabaseEvent.Insert (db, typeId, evt);
			}
			
			foreach (IType innerType in type.InnerTypes) {
				long innerTypeId = Insert (db, unitId, innerType);
				db.Connection.Execute (String.Format (@"INSERT INTO {0} (TypeID, InnerTypeID) VALUES ({1}, {2})", 
					InnerTypeTable,
					typeId,
					innerTypeId
				));
			}
			return typeId;
		}
		
		public static DatabaseType ReadType (CodeCompletionDatabase db, long typeID)
		{
			IDataReader reader = db.Connection.Query (String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE TypeID={1}",  DatabaseType.Table, typeID));
			if (reader.Read ()) 
				return db.CreateDomType (reader, null);
			return null;
		}
		
		public static IEnumerable<IType> ReadInnerTypeList (DatabaseType declaringType)
		{
			IDataReader reader = declaringType.Database.Connection.Query (String.Format (@"SELECT InnerTypeID FROM {0} WHERE TypeId={1}", InnerTypeTable, declaringType.TypeId));
			if (reader != null) {
				try {
					while (reader.Read ()) {
						long innerTypeID = SqliteUtils.FromDbFormat<long> (reader[0]);
						DatabaseType type = ReadType (declaringType.Database, innerTypeID);
						if (type != null) {
							type.DeclaringType = declaringType;
							yield return type;
						}
					}
				} finally {
					reader.Dispose ();
				}
			}
		}
	}
}
