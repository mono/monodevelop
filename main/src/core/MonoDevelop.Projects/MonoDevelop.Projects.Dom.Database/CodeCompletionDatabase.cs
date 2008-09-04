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
		public const long Version = 6;
		
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
				System.Console.WriteLine("Error while creating connection to:" + fileName + ". Tying to recreate database.");
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
		
		public long GetProjectId (string name)
		{
			string query = String.Format (@"SELECT ProjectId FROM {0} WHERE Name='{1}'", ProjectsTable, name);
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
			result.Append ("Name IN (");
			int startLen = result.Length;
			foreach (string namespaceName in subNamespaces) {
				if (result.Length > startLen) 
					result.Append (",");
				result.Append ("'");
				result.Append (namespaceName);
				result.Append ("'");
			}
			result.Append (")");
			return result.ToString ();
		}
		
		string CompileProjectIds (IEnumerable<long> projectIds)
		{
			if (projectIds == null)
				return "";
			StringBuilder result = new StringBuilder ();
			result.Append ("(");
			foreach (long projectId in projectIds) {
				if (projectId <= 0)
					continue;
				if (result.Length > 1) 
					result.Append (", ");
				result.Append (projectId);
			}
			result.Append (")");
			return result.ToString ();
		}
		
		public void GetNamespaceContents (List<IMember> result, IEnumerable<long> compilationUnitIds, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			try {
				StringBuilder sb = new StringBuilder ();
				string query = String.Format (@"SELECT NamespaceID, Name FROM {0} WHERE ({1})",  NamespaceTable, CompileNamespaces (subNamespaces));
				IDataReader reader = connection.Query (query);
				
				Dictionary<long, string> namespaceTable = new Dictionary<long, string> ();
				bool foundNamespace = false;
				if (reader != null) {
					try {
						sb.Append ("NamespaceID IN (");
						while (reader.Read ()) {
							long   namespaceID = (long)SqliteUtils.FromDbFormat (typeof (long), reader[0]);
							string name        = (string)SqliteUtils.FromDbFormat (typeof (string), reader[1]);
							namespaceTable[namespaceID] = name;
							if (foundNamespace) 
								sb.Append (", ");
							sb.Append (namespaceID);
							foundNamespace = true;
						}
						sb.Append (")");
					} finally {
						reader.Dispose ();
					}
				}
				
				string projectIds = CompileProjectIds (compilationUnitIds);
				query = String.Format (@"SELECT UnitID FROM {0} WHERE ProjectId IN {1} ",  CompilationUnitTable, projectIds);
				StringBuilder unitIds = new StringBuilder ();
				unitIds.Append ("UnitID IN (");
				int startLen = unitIds.Length;
				foreach (long unitId in connection.QueryEnumerable<long> (query)) {
					if (unitIds.Length > startLen)
						unitIds.Append (", ");
					unitIds.Append (unitId);
				}
				unitIds.Append (")");
				
				if (foundNamespace) {
					query = String.Format (@"SELECT TypeID, UnitID, MemberID, NamespaceID, ClassType, BaseTypeID FROM {0} WHERE {1} AND {2}",  DatabaseType.Table, unitIds.ToString (),sb.ToString ());
					reader = connection.Query (query);
					if (reader != null) {
						try {
							while (reader.Read ()) {
								long nsId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[3]);
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
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Error while GetNamespaceContents" , e);
			}
		}
		
		public IEnumerable<IReturnType> GetSubclasses (IType type, IEnumerable<long> projectIds)
		{
			if (type == null)
				return new IReturnType[] { };
			DatabaseType databaseType = type as DatabaseType;
			if (databaseType == null) {
				foreach (DatabaseType t in GetTypes (new string [] { type.Namespace }, projectIds, type.FullName, true)) {
					databaseType = t;
					break;
				}
			}
			if (databaseType != null)
				return databaseType.GetSubclasses (projectIds);
			if (type.FullName != "System.Object")
				return new IReturnType[] { new DomReturnType ("System.Object") };
			return new IReturnType[] { };
		}
		
		public IEnumerable<IType> GetTypeList (IEnumerable<long> projectIds)
		{
			string query;
			string compiledIds = CompileProjectIds (projectIds);
			
			if (!string.IsNullOrEmpty (compiledIds)) {
				query = String.Format (@"SELECT TypeID, {0}.UnitID, MemberID, NamespaceID, ClassType, BaseTypeID FROM {1}, {0} WHERE {1}.ProjectId IN {2} AND {0}.UnitID={1}.UnitID",  DatabaseType.Table, CompilationUnitTable, compiledIds);
			} else {
				query = String.Format (@"SELECT TypeID, UnitID, MemberID, NamespaceID, ClassType, BaseTypeID FROM {0}",  DatabaseType.Table);
			}
			
			IDataReader reader = null;
			
			try {
				reader = connection.Query (query);
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Error while GetTypeList:" + query, e);
			}
			
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
				long nsId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[3]);
				nsName = GetNamespaceName (nsId);
			}
			long unitId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[1]);
			long memberId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[2]);
			
			ClassType classType = (ClassType)((long)SqliteUtils.FromDbFormat (typeof (long), reader[4]));
			long baseTypeId = (long)SqliteUtils.FromDbFormat (typeof (long), reader[5]);

			return new DatabaseType (this, unitId, memberId, typeId, nsName, classType, baseTypeId);
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
			}
			
			if (nb.Length > 0) 
				nb.Append (" OR ");
			nb.Append ("Name = '");
			nb.Append (DomReturnType.SplitFullName (fullName).Key);
			nb.Append ("'");
			
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
				string projectIds = CompileProjectIds (compilationUnitIds);
				if (!string.IsNullOrEmpty (projectIds)) {
					query = String.Format (@"SELECT TypeID, {0}.UnitID, {0}.MemberID, NamespaceID, ClassType, BaseTypeID FROM {0}, {2}, {1} WHERE {0}.UnitID={2}.UnitID AND {2}.ProjectId IN {5} AND ({0}.MemberID={1}.MemberID AND {1}.Name='{3}') AND ({4})",  DatabaseType.Table, MemberTable, CompilationUnitTable, typeName, sb.ToString (), projectIds.ToString ());
				} else {
					query = String.Format (@"SELECT TypeID, UnitID, {0}.MemberID, NamespaceID, ClassType, BaseTypeID FROM {0}, {1} WHERE ({0}.MemberID={1}.MemberID AND {1}.Name='{2}') AND ({3})",  DatabaseType.Table, MemberTable, typeName, sb.ToString ());
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
		static int i = 0, j = 0;
		Dictionary<string, long> returnTypes = new Dictionary<string,long> ();
		internal long GetReturnTypeID (IReturnType returnType)
		{
			if (returnType == null)
				return -1;
			long result;
			string invariantString = returnType.ToInvariantString ();
			if (returnTypes.TryGetValue (invariantString, out result)) {
				return result;
			}
			result = connection.Query<long> (String.Format (@"SELECT ReturnTypeID FROM {0} WHERE InvariantString='{1}'", ReturnTypeTable, invariantString));
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
			return connection.Query<DateTime> (String.Format (@"SELECT ParseTime FROM {0} WHERE Name='{1}'", CompilationUnitTable, name));
		}
		
		public void UpdateCompilationUnit (ICompilationUnit unit, long projectId, string name)
		{
			long unitID = GetUnitId (name);
			try {
				if (unitID > 0) {
					connection.Execute (String.Format (@"DELETE FROM {0} WHERE UnitID={1}", CompilationUnitTable, unitID));
					foreach (DatabaseType type in GetTypeList (new long [] {unitID})) {
						type.Delete ();
					}
				}
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Error while UpdateCompilationUnit:" + name, e);
				return;
			}
			InsertCompilationUnit (unit, projectId, name);
		}
		
		public long InsertCompilationUnit (ICompilationUnit unit, long projectId, string name)
		{
			try {
				lock (connection) {
					connection.Execute ("BEGIN TRANSACTION");
					long unitId = connection.Execute (String.Format (@"INSERT INTO {0} (Name, ProjectId, ParseTime) VALUES ('{1}', {2}, {3})", CompilationUnitTable, name, projectId, SqliteUtils.ToDbFormat (unit.ParseTime)));
					foreach (IUsing u in unit.Usings) {
						if (u.Namespaces != null) {
							foreach (string nsName in u.Namespaces) {
								connection.Execute (String.Format (@"INSERT INTO {0} (UnitID, Namespace, Region) VALUES ({1}, '{2}', '{3}')", UsingTable, unitId, nsName, u.Region != null ? u.Region.ToInvariantString () : ""));
							}
						}
					}
					foreach (IType type in unit.Types) {
						DatabaseType.Insert (this, unitId, type);
					}
					connection.Execute ("END TRANSACTION");
					return unitId;
				}
			} catch (Exception e) {
				try {
					connection.Execute ("ROLLBACK");
				} catch (Exception)  {
				}
				MonoDevelop.Core.LoggingService.LogError ("Database error while inserting compilation unit " + name, e);
			}
			return -1;
		}
		
		public long InsertProject (string name)
		{
			try {
				long projectId = connection.Execute (String.Format (@"INSERT INTO {0} (Name) VALUES ('{1}')", ProjectsTable, name));
//			System.Console.WriteLine (projectId);
				return projectId;
			} catch (Exception e) {
				MonoDevelop.Core.LoggingService.LogError ("Error while InsertProject(" + name+")", e);
				return -1;
			}
				
		}
		
		internal const string ProjectsTable                  = "Projects";
		internal const string CompilationUnitTable           = "CompilationUnits";
		internal const string UsingTable                     = "Usings";
		internal const string NamespaceTable                 = "Namespaces";
		internal const string MemberTable                    = "Members";
		internal const string ReturnTypeTable                = "ReturnTypes";
		
		void CheckTables ()
		{
			if (!connection.TableExists (ProjectsTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						ProjectID INTEGER PRIMARY KEY AUTOINCREMENT,
						Name TEXT)", ProjectsTable
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
				connection.Execute (String.Format (@"CREATE INDEX IDX_{0}_ProjectID ON {0}(ProjectID)", CompilationUnitTable));
			}
			
			if (!connection.TableExists (UsingTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						UnitID INTEGER,
						Namespace TEXT,
						Region TEXT)", UsingTable
				));
			}
			
			if (!connection.TableExists (NamespaceTable)) {
				connection.Execute (String.Format (@"
					CREATE TABLE {0} (
						NamespaceID INTEGER PRIMARY KEY AUTOINCREMENT,
						Name TEXT
					)", NamespaceTable
				));
				connection.Execute (String.Format (@"CREATE INDEX IDX_{0}_Name ON {0}(Name)", NamespaceTable));
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
