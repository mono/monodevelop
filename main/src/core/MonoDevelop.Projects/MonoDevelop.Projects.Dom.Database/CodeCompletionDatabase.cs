// CodeCompletionDatabase.cs
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
	public class CodeCompletionDatabase : IDisposable 
	{
		HyenaSqliteConnection connection;
		internal HyenaSqliteConnection Connection {
			get {
				return connection;
			}
		}
		
		public CodeCompletionDatabase (string fileName)
		{
			try {
				connection = new HyenaSqliteConnection (fileName);
				CheckTables ();
			} catch (Exception e) {
				if (connection != null)
					connection.Dispose ();
				System.IO.File.Delete (fileName);
				connection = new HyenaSqliteConnection (fileName);
				CheckTables ();
			}
		}
		
		public long GetUnitId (string name)
		{
			return connection.Query<long> (String.Format (@"SELECT UnitID FROM {0} WHERE Name = '{1}'", CompilationUnitTable, name));
		}
		
		public void GetNamespaceContents (List<IMember> result, long compilationUnitId, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			StringBuilder nb = new StringBuilder ();
			foreach (string ns in subNamespaces) {
				if (nb.Length > 0) 
					nb.Append (" OR ");
				nb.Append ("Name = '");
				nb.Append (ns);
				nb.Append ("'");
			}
			
			StringBuilder sb = new StringBuilder ();
			string query = String.Format (@"SELECT NamespaceID, Name FROM {0} WHERE ({1})",  NamespaceTable, nb.ToString ());
			IDataReader reader = connection.Query (query);
			
			Dictionary<long, string> namespaceTable = new Dictionary<long, string> ();
			if (reader != null) {
				try {
					while (reader.Read ()) {
						long   namespaceID = (long)SqliteUtils.FromDbFormat (typeof (long), reader[0]);
						string name        = (string)SqliteUtils.FromDbFormat (typeof (string), reader[1]);
						namespaceTable[namespaceID] = name;
						if (sb.Length > 0) 
							sb.Append (" OR ");
						sb.Append ("NamespaceID = ");
						sb.Append (namespaceID);
					}
				} finally {
					reader.Dispose ();
				}
			}
			
			if (sb.Length > 0) {
				if (compilationUnitId < 0) {
					query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE ({1})",  DatabaseType.Table, sb.ToString ());
				} else {
					query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE UnitID = {1} AND ({2})",  DatabaseType.Table, compilationUnitId, sb.ToString ());
				}
				
				reader = connection.Query (query);
				if (reader != null) {
					try {
						while (reader.Read ()) {
							long nsId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[1]);
							string nsName = namespaceTable.ContainsKey (nsId) ? namespaceTable[nsId] : GetNamespaceName (nsId);
							result.Add (CreateDomType (reader, nsName));
						}
					} finally {
						reader.Dispose ();
					}
				}
			}
			
			Dictionary<string, bool> insertedNamespaces = new Dictionary<string, bool> ();
			foreach (string definedNamespace in connection.QueryEnumerable<string> (String.Format (@"SELECT Name FROM {0}", NamespaceTable))) {
				if (String.IsNullOrEmpty (definedNamespace))
					continue;
				foreach (string subNamespace in subNamespaces) {
					if (subNamespace.Length >= definedNamespace.Length)
						continue;
					if (definedNamespace.StartsWith (subNamespace, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)) {
						string tmp = subNamespace.Length > 0 ? definedNamespace.Substring (subNamespace.Length + 1) : definedNamespace;
						int idx = tmp.IndexOf('.');
						string namespaceName = idx > 0 ? tmp.Substring (0, idx) : tmp;
						if (!insertedNamespaces.ContainsKey (namespaceName)) {
							Namespace ns = new Namespace (namespaceName);
							if (!result.Contains (ns))
								result.Add (ns);
							insertedNamespaces[namespaceName] = true;
						}
					}
				}
			}
		}
		
		public IEnumerable<IType> GetTypeList (long compilationUnitId)
		{
			string query;
			if (compilationUnitId >= 0) {
				query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE UnitID = '{1}'",  DatabaseType.Table, compilationUnitId);
			} else {
				query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0}",  DatabaseType.Table);
			}
			IDataReader reader = connection.Query (query);
			if (reader != null) {
				try {
					while (reader.Read ()) {
						yield return CreateDomType (reader, null);
					}
				} finally {
					reader.Dispose ();
				}
			}
		}
		
		string GetNamespaceName (long namespaceId)
		{
			return connection.Query<string> (String.Format (@"SELECT Name FROM {0} WHERE NamespaceID = {1}", NamespaceTable, namespaceId));
		}
		
		internal DatabaseType CreateDomType (IDataReader reader, string nsName)
		{
			long typeId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[0]);
			if (nsName == null) {
				long nsId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[2]);
				nsName = GetNamespaceName (nsId);
			}
			long unitId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[1]);
			string name = (string)SqliteUtils.FromDbFormat (typeof (string), reader[3]);
			ClassType classType = (ClassType)((long)SqliteUtils.FromDbFormat (typeof (long), reader[4]));
			return new DatabaseType (this, unitId, typeId, nsName, name, classType);
		}
		
		public IEnumerable<IType> GetTypes (IEnumerable<string> subNamespaces, string fullName, bool caseSensitive)
		{
			StringBuilder nb = new StringBuilder ();
			if (subNamespaces != null) {
				foreach (string ns in subNamespaces) {
					if (nb.Length > 0) 
						nb.Append (" OR ");
					nb.Append ("Name = '");
					if (String.IsNullOrEmpty (ns)) {
						nb.Append (DomReturnType.SplitFullName (fullName).Key);
					} else {
						nb.Append (DomReturnType.SplitFullName (ns + "." + fullName).Key);
					}
					nb.Append ("'");
				}
			} else {
				nb.Append ("Name = '");
				nb.Append (DomReturnType.SplitFullName (fullName).Key);
				nb.Append ("'");
			}
			
			StringBuilder sb = new StringBuilder ();
			string query = String.Format (@"SELECT NamespaceID, Name FROM {0} WHERE ({1})",  NamespaceTable, nb.ToString ());
			IDataReader reader = connection.Query (query);
			
			if (reader != null) {
				try {
					while (reader.Read ()) {
						long   namespaceID = (long)SqliteUtils.FromDbFormat (typeof (long), reader[0]);
						string name        = (string)SqliteUtils.FromDbFormat (typeof (string), reader[1]);
						if (sb.Length > 0) 
							sb.Append (" OR ");
						sb.Append ("NamespaceID=");
						sb.Append (namespaceID);
					}
				} finally {
					reader.Dispose ();
				}
			}
			
			if (sb.Length > 0) {
				string typeName = DomReturnType.SplitFullName (fullName).Value;
				query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE Name='{1}' AND ({2})",  DatabaseType.Table, typeName, sb.ToString ());
				reader = connection.Query (query);
				while (reader.Read ()) {
					yield return CreateDomType (reader, null);
				}
			}
		}
		
		internal long GetNamespaceID (string namespaceName)
		{
			long result = connection.Query<long> (String.Format (@"SELECT NamespaceID FROM {0} WHERE Name='{1}'", NamespaceTable, namespaceName ?? ""));
			if (result <= 0)
				result = connection.Execute (String.Format (@"INSERT INTO {0} (Name) VALUES ('{1}')", NamespaceTable, namespaceName ?? ""));
			return result;
		}
		
		public bool ContainsCompilationUnit (string name)
		{
			return GetUnitId (name) != 0;
		}
		
		internal IReturnType ReadReturnType (long returnTypeID)
		{
			IDataReader reader = connection.Query (String.Format (@"SELECT FullName, Modifiers, PointerNestingLevel, ArrayDimensions, GenericArgumentCount FROM {0} WHERE ReturnTypeID={1}",  ReturnTypeTable, returnTypeID));
			if (reader != null && reader.Read ()) {
				DomReturnType result = new DomReturnType();
				result.FullName            = SqliteUtils.FromDbFormat<string> (reader[0]);
				result.Modifiers           = (ReturnTypeModifiers)SqliteUtils.FromDbFormat<long> (reader[1]);
				result.PointerNestingLevel = (int)SqliteUtils.FromDbFormat<long> (reader[2]);
				result.ArrayDimensions     = (int)SqliteUtils.FromDbFormat<long> (reader[3]);
				long genericArgumentCount  = SqliteUtils.FromDbFormat<long> (reader[4]);
				if (genericArgumentCount > 0) {
					// TODO: Read generic arguments
					for (int i = 0; i < genericArgumentCount; i++) {
						result.AddTypeParameter (new DomReturnType ("Arg" + i));
					}
				}
				return result;
			}
			return null;
		}
		
		internal long GetReturnTypeID (IReturnType returnType)
		{
			if (returnType == null)
				return -1;
			return connection.Execute (String.Format (@"INSERT INTO {0} (FullName, Modifiers, PointerNestingLevel, ArrayDimensions, GenericArgumentCount) VALUES ('{1}', {2}, {3}, {4}, {5})", 
					ReturnTypeTable,
					returnType.FullName,
					(long)returnType.Modifiers,
					returnType.PointerNestingLevel,
					returnType.ArrayDimensions,
					returnType.GenericArguments == null ? 0 : returnType.GenericArguments.Count
				));
		}
		
		internal long InsertMember (IMember member)
		{
			long returnTypeID = GetReturnTypeID (member.ReturnType);
			return connection.Execute (String.Format (@"INSERT INTO {0} (Name, ReturnTypeID, Documentation, Location, BodyRegion, Modifiers) VALUES ('{1}', {2}, '{3}', '{4}', '{5}', {6})", 
				MemberTable,
				member.Name,
				returnTypeID,
				member.Documentation,
				member.Location.ToInvariantString (),
				member.BodyRegion.ToInvariantString (),
				(long)member.Modifiers
			));
		}
		
		public void DeleteMember (long memberId)
		{
			connection.Execute (String.Format (@"DELETE FROM {0} WHERE MemberID={1}", MemberTable, memberId));
		}
		
		public DateTime GetCompilationUnitParseTime (string name)
		{
			return connection.Query<DateTime> (String.Format (@"SELECT ParseTime FROM {0} WHERE Name = '{1}'", CompilationUnitTable, name));
		}
		
		public void UpdateCompilationUnit (ICompilationUnit unit, string name)
		{
			long unitID = GetUnitId (name);
			if (unitID >= 0) {
//				connection.Execute ("BEGIN TRANSACTION");
				connection.Execute (String.Format (@"DELETE FROM {0} WHERE UnitID={1}", CompilationUnitTable, unitID));
				foreach (DatabaseType type in GetTypeList (unitID)) {
					type.Delete ();
				}
//				connection.Execute ("END TRANSACTION");
			}
			InsertCompilationUnit (unit, name);
		}
		
		public void InsertCompilationUnit (ICompilationUnit unit, string name)
		{
			connection.Execute ("BEGIN TRANSACTION");
			long unitId = connection.Execute (String.Format (@"INSERT INTO {0} (Name, ParseTime) VALUES ('{1}', {2})", CompilationUnitTable, name, SqliteUtils.ToDbFormat (unit.ParseTime)));
			foreach (IType type in unit.Types) {
				DatabaseType.Insert (this, unitId, type);
			}
			connection.Execute ("END TRANSACTION");
		}
		
		internal const string CompilationUnitTable = "CompilationUnits";
		internal const string MemberTable          = "Members";
		internal const string ReturnTypeTable      = "ReturnTypes";
		internal const string GenericReturnTypeArgumentTable = "ReturnTypeGenArgs";
		
		internal const string NamespaceTable        = "Namespaces";
		
		void CheckTables ()
		{
			if (!connection.TableExists (CompilationUnitTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						UnitID INTEGER PRIMARY KEY AUTOINCREMENT,
						Name TEXT,
						ParseTime INTEGER)", CompilationUnitTable
				));
			}
			
			if (!connection.TableExists (NamespaceTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						NamespaceID INTEGER PRIMARY KEY AUTOINCREMENT,
						Name TEXT
					)", NamespaceTable
				));
			}
			
			if (!connection.TableExists (MemberTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						MemberID INTEGER PRIMARY KEY AUTOINCREMENT,
						ReturnTypeID INTEGER,
						Name TEXT,
						Documentation TEXT,
						Location TEXT,
						BodyRegion TEXT,
						Modifiers INTEGER
					)", MemberTable
				));
			}
			
			if (!connection.TableExists (ReturnTypeTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						ReturnTypeID INTEGER PRIMARY KEY AUTOINCREMENT,
						FullName TEXT,
						Modifiers INTEGER,
						PointerNestingLevel INTEGER,
						ArrayDimensions INTEGER,
						GenericArgumentCount INTEGER
						)", ReturnTypeTable
				));
			}
			if (!connection.TableExists (GenericReturnTypeArgumentTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						ReturnTypeID,
						GenericArgumentID
						)", GenericReturnTypeArgumentTable
				));
			}
			
			DatabaseType.CheckTables (this);
			DatabaseMethod.CheckTables (this);
			DatabaseField.CheckTables (this);
			DatabaseProperty.CheckTables (this);
			DatabaseEvent.CheckTables (this);
			DatabaseParameter.CheckTables (this);
		}
		
		~CodeCompletionDatabase ()
		{
			Dispose ();
		}
		
		public void Dispose ()
		{
			if (connection != null) {
				connection.Dispose ();
				connection = null;
			}
		}
	}
}
