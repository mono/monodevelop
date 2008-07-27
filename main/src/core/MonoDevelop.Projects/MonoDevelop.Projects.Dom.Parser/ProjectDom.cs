//
// ProjectDom.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Dom.Parser
{
	public class ProjectDom
	{	
		List<ProjectDom> references = new List<ProjectDom> ();
		Dictionary<string, IType> typeTable = new Dictionary<string, IType> ();
		
		public Project Project;
		
		public IEnumerable<IType> AllAccessibleTypes {
			get {
				foreach (IType type in Types) {
					yield return type;
				}
				
				foreach (ProjectDom reference in references) {
					foreach (IType type in reference.Types) {
						yield return type;
					}
				}
			}
		}
		
		public virtual IEnumerable<IType> Types {
			get {
				foreach (IType type in typeTable.Values) {
					yield return type;
				}
			}
		}
		
		public void AddReference (ProjectDom dom)
		{
			if (dom == null)
				return;
			references.Add (dom);
		}
		
		public IEnumerable<IType> GetInheritanceTree (IType type)
		{
			Stack<IType> types = new Stack<IType> ();
			types.Push (type);
			while (types.Count > 0) {
				IType cur = types.Pop ();
				if (cur == null)
					continue;
				yield return cur;
				if (cur.BaseType == null && cur.FullName != "System.Object") {
					types.Push (this.GetType (null, "System.Object", -1, true));
					continue;
				}
				foreach (IReturnType baseType in cur.BaseTypes) {
					IType resolvedType = this.GetType (baseType, true);
					if (resolvedType != null) 
						types.Push (resolvedType);
				}
			}
		}
		
		public SearchTypeResult SearchType (SearchTypeRequest request)
		{
			List<string> namespaces = new List<string> ();
			namespaces.Add ("");
			if (request.CurrentCompilationUnit != null) {
				foreach (IUsing u in request.CurrentCompilationUnit.Usings) {
					foreach (string ns in u.Namespaces) {
						namespaces.Add (ns);
					}
				}
			}
			IType type = GetType (namespaces, request.Name, request.GenericParameterCount, true, true);
			if (type != null)
				return new SearchTypeResult (type);
			return null;
		}
		
/*		public IEnumerable<IType> GetTypesFrom (string fileName)
		{
			if (Types != null) {
				foreach (IType type in Types) {
					if (type.Parts != null) {
						foreach (IType part in type.Parts) {
							if (part.CompilationUnit != null && part.CompilationUnit.FileName == fileName)
								yield return part;
						}
					}
					if (type.CompilationUnit != null && type.CompilationUnit.FileName == fileName)
						yield return type;
				}
			}
		}*/
		
		public virtual void UpdateFromParseInfo (ICompilationUnit unit, string fileName)
		{
			if (String.IsNullOrEmpty (fileName))
				return;
			foreach (IType type in unit.Types) {
				type.SourceProjectDom = this;
				typeTable[type.FullName] = type;
			}
			
		}
		
		public bool NamespaceExists (string name)
		{
			string ns = name + ".";
			foreach (IType type in AllAccessibleTypes) {
				if (type.FullName.StartsWith (ns))
					return true;
			}
			return false;
		}
		
		protected virtual void GetNamespaceContents (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			foreach (IType type in Types) {
				string fullName = type.FullName;
				foreach (string subNamespace in subNamespaces) {
					if (fullName.StartsWith (subNamespace, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)) {
						string tmp = subNamespace.Length > 0 ? fullName.Substring (subNamespace.Length + 1) : fullName;
						int idx = tmp.IndexOf('.');
						IMember newMember;
						if (idx > 0) {
							newMember = new Namespace (tmp.Substring (0, idx));
						} else {
							newMember = type;
						}
						if (!result.Contains (newMember))
							result.Add (newMember);
					}
				}
			}
		}
		
		public List<IMember> GetNamespaceContents (string subNamespace, bool includeReferences, bool caseSensitive)
		{
			return GetNamespaceContents (new string[] { subNamespace }, includeReferences, caseSensitive);
		}
		
		public List<IMember> GetNamespaceContents (IEnumerable<string> subNamespaces, bool includeReferences, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			GetNamespaceContents (result, subNamespaces, caseSensitive);
			if (includeReferences) {
				foreach (ProjectDom reference in references) {
					reference.GetNamespaceContents (result, subNamespaces, caseSensitive);
				}
			}
			return result;
		}
		
		public virtual bool NeedCompilation (string fileName)
		{
			return false;
		}
		
		
		public IType GetType (IReturnType returnType)
		{
			return GetType (returnType, true);
		}
		
		public IType GetType (IReturnType returnType, bool searchDeep)
		{
			if (returnType == null)
				return null;
			return GetType (returnType.FullName, -1, true, searchDeep);
		}
		
		protected virtual IType GetType (IEnumerable<string> subNamespaces, string fullName, int genericParameterCount, bool caseSensitive)
		{
			IType result;
			if (typeTable.TryGetValue (fullName, out result))
				return result;
			if (subNamespaces != null) {
				foreach (string ns in subNamespaces) {
					if (typeTable.TryGetValue (ns + "." + fullName, out result))
						return result;
				}
			}
			return null;
		}
		
		public IType GetType (string fullName, int genericParameterCount, bool caseSensitive, bool searchDeep)
		{
			return GetType (null, fullName, genericParameterCount, caseSensitive, searchDeep);
		}
		
		public IType GetType (IEnumerable<string> subNamespaces, string fullName, int genericParameterCount, bool caseSensitive, bool searchDeep)
		{
			if (String.IsNullOrEmpty (fullName))
				return null;
				
			IType result = GetType (subNamespaces, fullName, genericParameterCount, caseSensitive);
			if (result == null && searchDeep) {
				foreach (ProjectDom reference in references) {
					result = reference.GetType (subNamespaces, fullName, genericParameterCount, caseSensitive, false);
					if (result != null)
						break;
				}
			}
			return result;
		}
		
		internal void FireLoaded ()
		{
			if (Loaded != null) {
				Loaded (this, EventArgs.Empty);
			}
		}
		
		public event EventHandler Loaded;
	}
}
