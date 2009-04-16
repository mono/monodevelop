// ParserDatabase.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//


using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Utility;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class ParserDatabase: IParserDatabase
	{
		StringNameTable nameTable;
		
		static readonly string[] sharedNameTable = new string[] {
			"", // 505195
			"System.Void", // 116020
			"To be added", // 78598
			"System.Int32", // 72669
			"System.String", // 72097
			"System.Object", // 48530
			"System.Boolean", // 46200
			".ctor", // 39938
			"System.IntPtr", // 35184
			"To be added.", // 19082
			"value", // 11906
			"System.Byte", // 8524
			"To be added: an object of type 'string'", // 7928
			"e", // 7858
			"raw", // 7830
			"System.IAsyncResult", // 7760
			"System.Type", // 7518
			"name", // 7188
			"object", // 6982
			"System.UInt32", // 6966
			"index", // 6038
			"To be added: an object of type 'int'", // 5196
			"System.Int64", // 4166
			"callback", // 4158
			"System.EventArgs", // 4140
			"method", // 4030
			"System.Enum", // 3980
			"value__", // 3954
			"Invoke", // 3906
			"result", // 3856
			"System.AsyncCallback", // 3850
			"System.MulticastDelegate", // 3698
			"BeginInvoke", // 3650
			"EndInvoke", // 3562
			"node", // 3416
			"sender", // 3398
			"context", // 3310
			"System.EventHandler", // 3218
			"System.Double", // 3206
			"type", // 3094
			"x", // 3056
			"System.Single", // 2940
			"data", // 2930
			"args", // 2926
			"System.Char", // 2813
			"Gdk.Key", // 2684
			"ToString", // 2634
			"'a", // 2594
			"System.Drawing.Color", // 2550
			"y", // 2458
			"To be added: an object of type 'object'", // 2430
			"System.DateTime", // 2420
			"message", // 2352
			"GLib.GType", // 2292
			"o", // 2280
			"a <see cref=\"T:System.Int32\" />", // 2176
			"path", // 2062
			"obj", // 2018
			"Nemerle.Core.list`1", // 1950
			"System.Windows.Forms", // 1942
			"System.Collections.ArrayList", // 1918
			"a <see cref=\"T:System.String\" />", // 1894
			"key", // 1868
			"Add", // 1864
			"arg0", // 1796
			"System.IO.Stream", // 1794
			"s", // 1784
			"arg1", // 1742
			"provider", // 1704
			"System.UInt64", // 1700
			"System.Drawing.Rectangle", // 1684
			"System.IFormatProvider", // 1684
			"gch", // 1680
			"System.Exception", // 1652
			"Equals", // 1590
			"System.Drawing.Pen", // 1584
			"count", // 1548
			"System.Collections.IEnumerator", // 1546
			"info", // 1526
			"Name", // 1512
			"System.Attribute", // 1494
			"gtype", // 1470
			"To be added: an object of type 'Type'", // 1444
			"System.Collections.Hashtable", // 1416
			"array", // 1380
			"System.Int16", // 1374
			"Gtk", // 1350
			"System.ComponentModel.ITypeDescriptorContext", // 1344
			"System.Collections.ICollection", // 1330
			"Dispose", // 1330
			"Gtk.Widget", // 1326
			"System.Runtime.Serialization.StreamingContext", // 1318
			"Nemerle.Compiler.Parsetree.PExpr", // 1312
			"System.Guid", // 1310
			"i", // 1302
			"Gtk.TreeIter", // 1300
			"text", // 1290
			"System.Runtime.Serialization.SerializationInfo", // 1272
			"state", // 1264
			"Remove" // 1256
		};
		
		public ParserDatabase ()
		{
			nameTable = new StringNameTable (sharedNameTable);
		}

		public void Initialize ()
		{
			DeleteObsoleteDatabases ();
		}


		public ProjectDom LoadSingleFileDom (string file)
		{
			return new DatabaseProjectDom (this, new SimpleCodeCompletionDatabase (file, this));
		}

		public ProjectDom LoadAssemblyDom (TargetRuntime runtime, string file)
		{
			return new DatabaseProjectDom (this, new AssemblyCodeCompletionDatabase (runtime, file, this));
		}

		public ProjectDom LoadProjectDom (Project project)
		{
			DatabaseProjectDom dom = new DatabaseProjectDom (this, new ProjectCodeCompletionDatabase (project, this));
			dom.Project = project;
			return dom;
		}
		
		void DeleteObsoleteDatabases ()
		{
			string[] files = Directory.GetFiles (ProjectDomService.CodeCompletionPath, "*.pidb");
			foreach (string file in files)
			{
				string name = Path.GetFileNameWithoutExtension (file);
				string baseDir = Path.GetDirectoryName (file);
				AssemblyCodeCompletionDatabase.CleanDatabase (baseDir, name);
			}
		}
		

		
#region Default Parser Layer dependent functions

		public IType GetClass (SerializationCodeCompletionDatabase db, string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			if (deepSearchReferences)
				return DeepGetClass (db, typeName, genericArguments, caseSensitive);
			else
				return GetClass (db, typeName, genericArguments, caseSensitive);
		}
		
		IType GetClass (SerializationCodeCompletionDatabase db, string typeName, IList<IReturnType> genericArguments, bool caseSensitive)
		{
			if (db != null) {
				IType c = db.GetClass (typeName, genericArguments, caseSensitive);
				if (c != null) return c;
				foreach (ReferenceEntry re in db.References)
				{
					SerializationCodeCompletionDatabase cdb = GetDatabase (re.Uri);
					if (cdb == null) continue;
					c = cdb.GetClass (typeName, genericArguments, caseSensitive);
					if (c != null) return c;
				}
			}
			return null;
		}
		
		public IType DeepGetClass (SerializationCodeCompletionDatabase db, string typeName, IList<IReturnType> genericArguments, bool caseSensitive)
		{
			ArrayList visited = new ArrayList ();
			IType c = DeepGetClassRec (visited, db, typeName, genericArguments, caseSensitive);
			return c;
		}
		
		internal IType DeepGetClassRec (ArrayList visitedDbs, SerializationCodeCompletionDatabase db, string typeName, IList<IReturnType> genericArguments, bool caseSensitive)
		{
			if (db == null) return null;
			if (visitedDbs.Contains (db)) return null;
			
			visitedDbs.Add (db);
			
			IType c = db.GetClass (typeName, genericArguments, caseSensitive);
			if (c != null) return c;
			
			foreach (ReferenceEntry re in db.References)
			{
				SerializationCodeCompletionDatabase cdb = GetDatabase (re.Uri);
				if (cdb == null) continue;
				c = DeepGetClassRec (visitedDbs, cdb, typeName, genericArguments, caseSensitive);
				if (c != null) return c;
			}
			return null;
		}
		
/*		public string SearchNamespace (SerializationCodeCompletionDatabase db, IUsing usin, string partitialNamespaceName)
		{
			return SearchNamespace (db, usin, partitialNamespaceName, true);
		}
		
		public string SearchNamespace (SerializationCodeCompletionDatabase db, IUsing usin, string partitialNamespaceName, bool caseSensitive)
		{
//			LoggingService.LogDebug ("SearchNamespace : >{0}<", partitialNamespaceName);
			if (NamespaceExists (db, partitialNamespaceName, caseSensitive)) {
				return partitialNamespaceName;
			}
			
			// search for partitial namespaces
			IReturnType alias;
			if (usin.Aliases.TryGetValue ("", out alias)) {
				string declaringNamespace = alias.FullName;
				while (declaringNamespace.Length > 0) {
					if ((caseSensitive ? declaringNamespace.EndsWith(partitialNamespaceName) : declaringNamespace.ToLower().EndsWith(partitialNamespaceName.ToLower()) ) && NamespaceExists (db, declaringNamespace, caseSensitive)) {
						return declaringNamespace;
					}
					int index = declaringNamespace.IndexOf('.');
					if (index > 0) {
						declaringNamespace = declaringNamespace.Substring(0, index);
					} else {
						break;
					}
				}
			}
			
			// Remember:
			//     Each namespace has an own using object
			//     The namespace name is an alias which has the key ""
			foreach (string aliasString in usin.Aliases.Keys) {
				if (caseSensitive ? partitialNamespaceName.StartsWith (aliasString) : partitialNamespaceName.ToLower().StartsWith(aliasString.ToLower())) {
					if (aliasString.Length > 0) {
						string nsName = String.Concat (usin.Aliases [aliasString], partitialNamespaceName.Remove(0, aliasString.Length));
						if (NamespaceExists (db, nsName, caseSensitive)) {
							return nsName;
						}
					}
				}
			}
			return null;
		}
*/
		
		public IEnumerable<IType> GetSubclassesTree (SerializationCodeCompletionDatabase db,
		                                             IType cls,
		                                             bool deepSearchReferences, 
		                                             IList<string> namespaces)
		{
			if (cls.FullName == "System.Object") {
				// Just return all classes
				if (!deepSearchReferences)
					return db.GetClassList (true, namespaces);
				else
					return GetAllClassesRec (new HashSet<SerializationCodeCompletionDatabase> (), db, namespaces);
			}
			else {
				var visited = new Dictionary<SerializationCodeCompletionDatabase, HashSet<IType>> ();
				SearchSubclasses (visited, cls, namespaces, db);
	
				if (deepSearchReferences) {
					List<IType> types = new List<IType> ();
					foreach (HashSet<IType> list in visited.Values) {
						// Don't use AddRange here. It won't work due to a bug in mono (#459816).
						foreach (IType tt in list)
							types.Add (tt);
					}
					return types;
				} else {
					return visited [db];
				}
			}
		}

		IEnumerable<IType> GetAllClassesRec (HashSet<SerializationCodeCompletionDatabase> visited, SerializationCodeCompletionDatabase db, IList<string> namespaces)
		{
			if (visited.Add (db)) {
				foreach (IType dsub in db.GetClassList (true, namespaces))
					yield return dsub;
				foreach (ReferenceEntry re in db.References) {
					SerializationCodeCompletionDatabase cdb = GetDatabase (re.Uri);
					if (cdb == null) continue;
					foreach (IType dsub in GetAllClassesRec (visited, cdb, namespaces))
						yield return dsub;
				}
			}
		}

		HashSet<IType> SearchSubclasses (Dictionary<SerializationCodeCompletionDatabase, HashSet<IType>> visited, IType btype, IList<string> namespaces, SerializationCodeCompletionDatabase db)
		{
			HashSet<IType> types;
			if (visited.TryGetValue (db, out types))
				return types;

			types = new HashSet<IType> (GetSubclassesTree (db, btype, namespaces));
			visited [db] = types;

			// For each reference, get the list of subclasses implemented in that reference,
			// then look for subclasses of any of those in the current db

			// A project can only have subclasses of classes implemented in assemblies it references
			
			foreach (ReferenceEntry re in db.References) {
				SerializationCodeCompletionDatabase cdb = GetDatabase (re.Uri);
				if (cdb != null && cdb != db) {
					HashSet<IType> refTypes = SearchSubclasses (visited, btype, namespaces, cdb);
					foreach (IType t in refTypes)
						foreach (IType st in GetSubclassesTree (db, t, namespaces))
							types.Add (st);
				}
			}

			return types;
		}

		IEnumerable<IType> GetSubclassesTree (SerializationCodeCompletionDatabase db, IType btype, IList<string> namespaces)
		{
			foreach (IType dsub in db.GetSubclasses (btype, namespaces)) {
				yield return dsub;
				foreach (IType sub in GetSubclassesTree (db, dsub, namespaces))
					yield return sub;
			}
		}
		
		public SerializationCodeCompletionDatabase GetDatabase (string uri)
		{
			DatabaseProjectDom dom = ProjectDomService.GetDomForUri (uri) as DatabaseProjectDom;
			if (dom != null)
				return dom.Database;
			else
				return null;
		}
		
#endregion

		internal static string GetDecoratedName (string name, int genericArgumentCount)
		{
			if (genericArgumentCount <= 0)
				return name;
			return name + "`" + genericArgumentCount;
		}
		
		internal static string GetDecoratedName (ClassEntry entry)
		{
			return GetDecoratedName (entry.Name, entry.TypeParameterCount);
		}

		internal static string GetDecoratedName (IType type)
		{
			return GetDecoratedName (type.FullName, type.TypeParameters.Count);
		}

		internal static string GetDecoratedName (IReturnType type)
		{
			return ((DomReturnType)type).DecoratedFullName;
		}

		
		////////////////////////////////////
		
		internal INameEncoder DefaultNameEncoder {
			get { return nameTable; }
		}

		internal INameDecoder DefaultNameDecoder {
			get { return nameTable; }
		}

	}
}
