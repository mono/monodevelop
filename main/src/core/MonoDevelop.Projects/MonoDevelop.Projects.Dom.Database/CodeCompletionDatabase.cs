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
		public const long Version = 2;
		
		HyenaSqliteConnection connection;
		internal HyenaSqliteConnection Connection {
			get {
				return connection;
			}
		}
		
		public CodeCompletionDatabase (string fileName)
		{
			try {
				connection = new MonoDevelopDatabaseConnection (fileName);
				CheckTables ();
			} catch (Exception e) {
				if (connection != null)
					connection.Dispose ();
				System.IO.File.Delete (fileName);
				connection = new MonoDevelopDatabaseConnection (fileName);
				CheckTables ();
			}
		}
		
		public long GetUnitId (string name)
		{
			string query = String.Format (@"SELECT UnitID FROM {0} WHERE Name='{1}'", CompilationUnitTable, name);
			//System.Console.WriteLine(query);
			return connection.Query<long> (query);
		}
		
		public void GetNamespaceContents (List<IMember> result, long compilationUnitId, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			GetNamespaceContents (result, compilationUnitId > 0 ? new long[] { compilationUnitId } : null, subNamespaces, caseSensitive);
		}
		
		string CompileNamespaces (IEnumerable<string> subNamespaces)
		{
			StringBuilder result = new StringBuilder ();
			foreach (string namespaceName in subNamespaces) {
				if (result.Length > 0) 
					result.Append (" OR ");
				result.Append ("Name='");
				result.Append (namespaceName);
				result.Append ("'");
			}
			return result.ToString ();
		}
		
		string CompileCompilationUnits (IEnumerable<long> compilationUnitIds)
		{
			StringBuilder result = new StringBuilder ();
			if (compilationUnitIds != null) {
				foreach (long unitId in compilationUnitIds) {
					if (unitId <= 0)
							continue;
					if (result.Length > 0) 
						result.Append (" OR ");
					result.Append ("UnitID=");
					result.Append (unitId);
				}
			}
			return result.ToString ();
		}
		
		public void GetNamespaceContents (List<IMember> result, IEnumerable<long> compilationUnitIds, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			StringBuilder sb = new StringBuilder ();
			string query = String.Format (@"SELECT NamespaceID, Name FROM {0} WHERE ({1})",  NamespaceTable, CompileNamespaces (subNamespaces));
			
			IDataReader reader = connection.Query (query);
			
			Dictionary<long, string> namespaceTable = new Dictionary<long, string> ();
			if (reader != null) {
				try {
					while (reader.Read ()) {
						long   namespaceID = (long)SqliteUtils.FromDbFormat (typeof (long), reader[0]);
						string name        = (string)SqliteUtils.FromDbFormat (typeof (string), reader[1]);
						System.Console.WriteLine(namespaceID + " / " + name);
						namespaceTable[namespaceID] = name;
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
				string units = CompileCompilationUnits (compilationUnitIds);
				if (string.IsNullOrEmpty (units)) {
					query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE ({1})",  DatabaseType.Table, sb.ToString ());
				} else {
					query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE ({1}) AND ({2})",  DatabaseType.Table, units, sb.ToString ());
				}
				reader = connection.Query (query);
				if (reader != null) {
					try {
						while (reader.Read ()) {
							long nsId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[2]);
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
		
		public IEnumerable<IReturnType> GetSubclasses (IType type, IEnumerable<long> compilationUnitIds)
		{
			DatabaseType databaseType = (DatabaseType)type;
			if (databaseType == null) {
				foreach (DatabaseType t in GetTypes (new string [] { type.Namespace }, compilationUnitIds, type.FullName, true)) {
					databaseType = t;
					break;
				}
			}
			if (databaseType != null)
				return databaseType.GetSubclasses (compilationUnitIds);
			if (type.FullName != "System.Object")
				return new IReturnType[] { new DomReturnType ("System.Object") };
			return new IReturnType[] { };
		}
		
		public IEnumerable<IType> GetTypeList (IEnumerable<long> compilationUnitIds)
		{
			string query;
			string units = CompileCompilationUnits (compilationUnitIds);
			
			if (!string.IsNullOrEmpty (units)) {
				query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE {1}",  DatabaseType.Table, units);
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
		
		public IEnumerable<IType> GetTypes (IEnumerable<string> subNamespaces, IEnumerable<long> compilationUnitIds, string fullName, bool caseSensitive)
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
				string units = CompileCompilationUnits (compilationUnitIds);
				if (!string.IsNullOrEmpty (units)) {
					query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE Name='{1}' AND ({2}) AND ({3})",  DatabaseType.Table, typeName, sb.ToString (), units.ToString ());
				} else {
					query = String.Format (@"SELECT TypeID, UnitID, NamespaceID, Name, ClassType FROM {0} WHERE Name='{1}' AND ({2})",  DatabaseType.Table, typeName, sb.ToString ());
				}
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
			IDataReader reader = connection.Query (String.Format (@"SELECT InvariantString FROM {0} WHERE ReturnTypeID={1}",  ReturnTypeTable, returnTypeID));
			if (reader == null)
				return null;
			try {
				if (reader != null && reader.Read ()) {
/*					DomReturnType result = DomReturnType.FromInvariantString (SqliteUtils.FromDbFormat<string> (reader[0]));
					result.Modifiers           = (ReturnTypeModifiers)SqliteUtils.FromDbFormat<long> (reader[1]);
					result.PointerNestingLevel = (int)SqliteUtils.FromDbFormat<long> (reader[2]);
					result.ArrayDimensions     = (int)SqliteUtils.FromDbFormat<long> (reader[3]);
					long genericArgumentCount  = SqliteUtils.FromDbFormat<long> (reader[4]);
					if (genericArgumentCount > 0) {
						foreach (long genReturnTypeID in connection.QueryEnumerable<long> (String.Format (@"SELECT GenericArgumentID FROM {0} WHERE ReturnTypeID={1}",
						                                                                               GenericReturnTypeArgumentTable,
						                                                                               returnTypeID))) {
							result.AddTypeParameter (ReadReturnType (genReturnTypeID));
						}
					}*/
					return DomReturnType.FromInvariantString (SqliteUtils.FromDbFormat<string> (reader[0]));
				}
			} finally {
				reader.Dispose();
			}
			return null;
		}
		
		Dictionary<string, long> returnTypes = new Dictionary<string,long> ();
		internal long GetReturnTypeID (IReturnType returnType)
		{
			if (returnType == null)
				return -1;
			long result;
			string invariantString = returnType.ToInvariantString ();
			if (returnTypes.TryGetValue (invariantString, out result))
				return result;
			
			result = connection.Query<long> (String.Format (@"SELECT ReturnTypeID FROM {0} WHERE InvariantString='{1}' ", ReturnTypeTable, invariantString));
			if (result > 0) {
				returnTypes [invariantString] = result;
				return result;
			}
			
			result = connection.Execute (String.Format (@"INSERT INTO {0} (InvariantString) VALUES ('{1}')", ReturnTypeTable, invariantString));
			returnTypes [invariantString] = result;
			return result;
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
				connection.Execute (String.Format (@"DELETE FROM {0} WHERE UnitID={1}", CompilationUnitTable, unitID));
				foreach (DatabaseType type in GetTypeList (new long [] {unitID})) {
					type.Delete ();
				}
			}
			InsertCompilationUnit (unit, name);
		}
		
		public long InsertCompilationUnit (ICompilationUnit unit, string name)
		{
			try {
				connection.Execute ("BEGIN TRANSACTION");
				long unitId = connection.Execute (String.Format (@"INSERT INTO {0} (Name, ParseTime) VALUES ('{1}', {2})", CompilationUnitTable, name, SqliteUtils.ToDbFormat (unit.ParseTime)));
				foreach (IType type in unit.Types) {
					DatabaseType.Insert (this, unitId, type);
				}
				connection.Execute ("END TRANSACTION");
				return unitId;
			} catch (Exception e) {
				connection.Execute ("ROLLBACK");
				MonoDevelop.Core.LoggingService.LogError ("Database error while inserting compilation unit " + name, e);
			}
			return -1;
		}
		
		internal const string ProjectsTable                  = "Projects";
		internal const string CompilationUnitTable           = "CompilationUnits";
		internal const string NamespaceTable                 = "Namespaces";
		internal const string MemberTable                    = "Members";
		internal const string ReturnTypeTable                = "ReturnTypes";
		
		void CheckTables ()
		{
			if (!connection.TableExists (ProjectsTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						ProjectID INTEGER PRIMARY KEY AUTOINCREMENT,
						Name TEXT,
						ProjectType INTEGER,
						LastOpenTime INTEGER)", ProjectsTable
				));
			}
			if (!connection.TableExists (CompilationUnitTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						UnitID INTEGER PRIMARY KEY AUTOINCREMENT,
						ProjectID INTEGER,
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
						InvariantString TEXT)", ReturnTypeTable
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
