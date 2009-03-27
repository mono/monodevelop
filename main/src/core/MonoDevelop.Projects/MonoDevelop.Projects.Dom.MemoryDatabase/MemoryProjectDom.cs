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
	public class MemoryProjectDom : ProjectDom, IDisposable
	{
		List<ICompilationUnit> units = new List<ICompilationUnit> ();
		NamespaceEntry rootNamespace = new NamespaceEntry ();
		
		public MemoryProjectDom ()
		{
		}
		
		public virtual void Dispose ()
		{
			units = null;
			rootNamespace = null;
		}
		
		internal class GenericTypeInstanceResolver: CopyDomVisitor<IType>
		{
			public Dictionary<string, IReturnType> typeTable = new Dictionary<string,IReturnType> ();
			
			public void Add (string name, IReturnType type)
			{
				//Console.WriteLine (name + " ==> " + type);
				typeTable.Add (name, type);
			}
			
			public override IDomVisitable Visit (IReturnType type, IType typeToInstantiate)
			{
				DomReturnType copyFrom = (DomReturnType) type;
				
				IReturnType res;
				//Console.WriteLine ("Transfer:" + copyFrom.DecoratedFullName);
				if (typeTable.TryGetValue (copyFrom.DecoratedFullName, out res)) {
					if (type.ArrayDimensions == 0 && type.GenericArguments.Count == 0)
						return res;
				}
				return base.Visit (type, typeToInstantiate);
			}
		}
		internal List<IType> ResolveTypeParameters (IEnumerable<IType> types)
		{
			List<IType> result = new List<IType> ();
			foreach (IType type in types) {
				if (type.TypeParameters.Count == 0) {
					result.Add (type);
					continue;
				}
				GenericTypeInstanceResolver resolver = new GenericTypeInstanceResolver ();
				string typeName = NamespaceEntry.GetDecoratedName (type);
				foreach (TypeParameter par in type.TypeParameters) {
					resolver.Add (par.Name, new DomReturnType (NamespaceEntry.ConcatNamespaces (typeName, par.Name)));
				}
				resolver.Visit (type, type);
				result.Add ((IType)type.AcceptVisitor (resolver, type));

			}
			return result;
		}
		Dictionary<string, int> files = new Dictionary<string, int> ();
		public override TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit cu)
		{
			//List<IType> types = new List<IType> (cu.Types);
			//ProjectDomService.ResolveTypes (this, cu, ResolveTypeParameters (cu.Types), out types);
			List<IType> types = ResolveTypeParameters (cu.Types);

		//	totalUnresolvedCount += unresolvedCount;
			units.Add (cu);
			TypeUpdateInformation res = UpdateTypeInformation (types, cu.FileName);
			int fileParseErrorRetries = 0;
			files.TryGetValue (cu.FileName, out fileParseErrorRetries);
			
		/*	if (unresolvedCount > 0) {
				if (fileParseErrorRetries != 1) {
					files[cu.FileName] = 1;
					
					ProjectDomService.ResolveTypes (this, cu, cu.Types, out resolved);
					res = UpdateTypeInformation (resolved, cu.FileName);
				}
			} else {
				files[cu.FileName] = 0;
			}*/
			
			return res;
		}
		
		public TypeUpdateInformation UpdateTypeInformation (IList<IType> newClasses, string fileName)
		{
			TypeUpdateInformation res = new TypeUpdateInformation ();
			//Console.WriteLine ("UPDATE TYPE INFO: " + newClasses.Count);
			for (int n = 0; n < newClasses.Count; n++) {
				IType c = newClasses[n];
				c.SourceProjectDom = this;
				
				// Remove file from compound types.
				NamespaceEntry entry = rootNamespace.FindNamespace (c.Namespace, true);
				IType t;
				if (entry.ContainingTypes.TryGetValue (NamespaceEntry.GetDecoratedName (c), out t))
					CompoundType.RemoveFile (t, c.CompilationUnit.FileName);
			}

			for (int n=0; n < newClasses.Count; n++) {
				IType c = newClasses[n];
				NamespaceEntry entry = rootNamespace.FindNamespace (c.Namespace, true);
				entry.Add (c);
				ResetInstantiatedTypes (c);
			}
			//Console.WriteLine ("root:" + rootNamespace);
			return res;
		}
		
		public override IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			int genericArgumentCount = ExtractGenericArgCount (ref typeName);
			if (genericArguments != null) {
				genericArgumentCount = genericArguments.Count;
			}
			string decoratedTypeName = NamespaceEntry.GetDecoratedName (typeName, genericArgumentCount);
			//Console.WriteLine ("Get Type: >" + typeName+"< " + genericArgumentCount + " in " + this.Uri +  " deep refs:" + deepSearchReferences + " type count:" + Types.Count ());
			
			NamespaceEntry entry = rootNamespace.FindNamespace (decoratedTypeName, false);
			//Console.WriteLine ("found entry:" + entry);
			IType searchType;
			if (entry.ContainingTypes.TryGetValue (decoratedTypeName, out searchType)) {
				IType result = CreateInstantiatedGenericType (searchType, genericArguments);
				//Console.WriteLine ("found type:" + result);
				return result;
			}
			// may be inner type
			foreach (IType type in entry.ContainingTypes.Values) {
				string name = NamespaceEntry.GetDecoratedName (type);
				if (decoratedTypeName.StartsWith (name)) {
					string innerClassName = decoratedTypeName.Substring (name.Length + 1);
					//Console.WriteLine ("icn:" + innerClassName);
					IType inner = SearchInnerType (type, innerClassName, genericArgumentCount, caseSensitive);
					if (inner != null) {
						//Console.WriteLine ("found inner:" + inner);
						return CreateInstantiatedGenericType (inner, genericArguments);
					}
				}
			}
			/*
			if (deepSearchReferences) {
				foreach (string uri in this.OnGetReferences ()) {
					ProjectDom dom = ProjectDomService.GetDomForUri (uri);
					if (dom != null) {
						IType result = dom.GetType (typeName, genericArguments, false, caseSensitive);
						if (result != null) {
							//Console.WriteLine ("found in :" + uri + " : " + result);
							return result;
						}
					}
				}
			}*/
			return null;
		}
		
		public override IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			return GetType (NamespaceEntry.GetDecoratedName (typeName, genericArgumentsCount), null, deepSearchReferences, caseSensitive);
		}
		
		public override bool NamespaceExists (string name, bool searchDeep, bool caseSensitive)
		{
			return rootNamespace.FindNamespace (name, false, true) != null;
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
					//Console.WriteLine ("Add: " + new Namespace (subEntry.Name));
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
