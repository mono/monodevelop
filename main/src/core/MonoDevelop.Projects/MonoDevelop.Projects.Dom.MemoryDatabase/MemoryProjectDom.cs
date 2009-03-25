// 
// MemoryProjectDom.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom.MemoryDatabase
{
	public class MemoryProjectDom : ProjectDom 
	{
		List<ICompilationUnit> units = new List<ICompilationUnit> ();
		NamespaceEntry rootNamespace = new NamespaceEntry ();
		
		public MemoryProjectDom ()
		{
		}
		
		public MemoryProjectDom (Project p)
		{
			this.Project = p;
		}
		
		Dictionary<string, int> files = new Dictionary<string, int> ();
		internal override TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit cu)
		{
			List<IType> resolved;
			int unresolvedCount = ProjectDomService.ResolveTypes (this, cu, cu.Types, out resolved);
		//	totalUnresolvedCount += unresolvedCount;
			units.Add (cu);
			TypeUpdateInformation res = UpdateTypeInformation (resolved, cu.FileName);
			int fileParseErrorRetries = 0;
			files.TryGetValue (cu.FileName, out fileParseErrorRetries);
			
			if (unresolvedCount > 0) {
				if (fileParseErrorRetries != 1) {
					files[cu.FileName] = 1;
					
					// Enqueue the file for quickly reparse. Types can't be resolved most probably because
					// the file that implements them is not yet parsed.
					ProjectDomService.QueueParseJob (this, 
					                                 delegate { UpdateFromParseInfo (cu); },
					                                 cu.FileName);
				}
			} else {
				files[cu.FileName] = 0;
			}
			
			return res;
		}
		
		public TypeUpdateInformation UpdateTypeInformation (IList<IType> newClasses, string fileName)
		{
			TypeUpdateInformation res = new TypeUpdateInformation ();
			
			for (int n = 0; n < newClasses.Count; n++) {
				((DomType)newClasses[n]).SourceProjectDom = this;
			}
			
			for (int n=0; n < newClasses.Count; n++) {
				IType c = newClasses[n]; //CopyClass ();
				NamespaceEntry entry = rootNamespace.FindNamespace (c.Namespace, true);
				Console.WriteLine ("add:" + c);
				Console.WriteLine (c.Namespace + "/" + entry);
				entry.Add (c);
				ResetInstantiatedTypes (c);
			}
			Console.WriteLine ("root:" + rootNamespace);
			return res;
		}
		
		public override IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			Console.WriteLine ("Get Type: " + typeName);
			int genericArgumentCount = ExtractGenericArgCount (ref typeName);
			if (genericArguments != null)
				genericArgumentCount = genericArguments.Count;
			
			NamespaceEntry entry = rootNamespace.FindNamespace (typeName, false);
			Console.WriteLine (entry);
			foreach (IType type in entry.ContainingTypes.Values) {
				if (NamespaceEntry.GetDecoratedName (type) == typeName) {
					Console.WriteLine ("Found type:" + type);
					return CreateInstantiatedGenericType (type, genericArguments);
				}
				if (typeName.StartsWith (NamespaceEntry.GetDecoratedName (type))) {
					Console.WriteLine ("try inner");
					IType inner = SearchInnerType (type, typeName, genericArgumentCount, caseSensitive);
					if (inner != null) {
						Console.WriteLine ("Found inner:" + inner);
						return inner;
					}
				}
			}
			
			
			return null;
		}

		public override IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			if (genericArgumentsCount > 0)
				typeName += "`" + genericArgumentsCount;
			return GetType (typeName, null, deepSearchReferences, caseSensitive);
		}
		
		public override IEnumerable<IType> Types {
			get {
				Stack<NamespaceEntry> entries = new Stack<NamespaceEntry> ();
				entries.Push (rootNamespace);
				while (entries.Count > 0) {
					NamespaceEntry cur = entries.Pop ();
					foreach (IType type in cur.ContainingTypes.Values) {
						yield return type;
					}
					foreach (NamespaceEntry subEntry in cur.SubEntries.Values) {
						entries.Push (subEntry);
					}
				}
			}
		}
		
		protected override IEnumerable<IType> InternalGetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			yield return type;
			foreach (IType t in Types) {
				if (namespaces != null && !namespaces.Contains (t.Namespace))
					continue;
				if (t.BaseTypes.Any (b => b.FullName == type.FullName)) {
					foreach (IType sub in InternalGetSubclasses (t, searchDeep, namespaces)) {
						yield return sub;
					}
				}
			}
		}
		
		internal override void GetNamespaceContentsInternal (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			if (subNamespaces == null) {
				foreach (NamespaceEntry entry in rootNamespace.SubEntries.Values) {
					result.Add (new Namespace (entry.Name));
				}
				foreach (IMember member in rootNamespace.ContainingTypes.Values) {
					result.Add (member);
				}
				return;
			}
			foreach (string subNamespace in subNamespaces) {
				NamespaceEntry entry = rootNamespace.FindNamespace (subNamespace, false, true);
				if (entry == null)
					continue;
				foreach (NamespaceEntry subEntry in entry.SubEntries.Values) {
					Console.WriteLine ("Add: " + new Namespace (subEntry.Name));
					result.Add (new Namespace (subEntry.Name));
				}
				foreach (IMember member in entry.ContainingTypes.Values) {
					result.Add (member);
				}
			}
		}
		
		public override bool NeedCompilation (string fileName)
		{
			return !units.Any (u => u.FileName == fileName);
			//FileEntry entry = database.GetFile (fileName);
			//return entry != null ? entry.IsModified : true;
		}

		internal override IEnumerable<string> OnGetReferences ()
		{
			yield break;
		}
	}
}
