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
using System.Collections.ObjectModel;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Dom.Parser
{
	public abstract class ProjectDom
	{	
		protected List<ProjectDom> references = new List<ProjectDom> ();
		
		public Project Project;
		internal int ReferenceCount;
		internal string Uri;

		public static readonly ProjectDom Empty = new EmptyProjectDom ();

		public ReadOnlyCollection<ProjectDom> References {
			get {
				if (references == null)
					UpdateReferences ();
				return references.AsReadOnly ();
			}
		}
		
		public abstract IEnumerable<IType> Types { get; }

		public virtual IList<Tag> GetSpecialComments (string fileName)
		{
			return new List<Tag> ();
		}
		public virtual void UpdateTagComments (string fileName, IList<Tag> tags)
		{
		
		}
		
		public virtual IEnumerable<IType> GetTypes (string fileName)
		 {
			foreach (IType type in Types) {
				if (type.CompilationUnit.FileName == fileName)
					yield return type;
			}
		}
		
		public IEnumerable<IType> GetInheritanceTree (IType type)
		{
			Dictionary<string, bool> alreadyTaken = new Dictionary<string, bool> ();
			Stack<IType> types = new Stack<IType> ();
			types.Push (type);
			while (types.Count > 0) {
				IType cur = types.Pop ();
				if (cur == null)
					continue;
				
				string fullName = DomType.GetNetFullName (cur);
				if (alreadyTaken.ContainsKey (fullName))
					continue;
				alreadyTaken[fullName] = true;
				
				yield return cur;
				
				foreach (IReturnType baseType in cur.BaseTypes) {
					IType resolvedType = this.SearchType (new SearchTypeRequest (cur.CompilationUnit, baseType));
					if (resolvedType != null)
						types.Push (DomType.CreateInstantiatedGenericType (resolvedType, baseType.GenericArguments));
				}
				
				if (cur.BaseType == null && cur.FullName != "System.Object") 
					types.Push (this.GetType (DomReturnType.Object));
			}
		}
		
		public virtual IType SearchType (SearchTypeRequest request)
		{
			return SearchType (request.Name, request.CallingType, request.CurrentCompilationUnit, request.GenericParameters);
		}
		
		internal IType SearchType (string name, IType callingClass, ICompilationUnit unit, List<IReturnType> genericParameters)
		{
			// TODO dom check generic parameter count
			
			if (name == null || name == String.Empty)
				return null;
				
			IType c;
			c = GetType (name, genericParameters, false, true);
			if (c != null)
				return c;

			// If the name matches an alias, try using the alias first.
			if (unit != null) {
				IReturnType ualias = FindAlias (name, unit.Usings);
				if (ualias != null) {
					// Don't provide the compilation unit when trying to resolve the alias,
					// since aliases are not affected by other 'using' directives.
					c = GetType (ualias.FullName, ualias.GenericArguments, false, true);
					if (c != null)
						return c;
				}
			}
			
			// The enclosing namespace has preference over the using directives.
			// Check it now.

			if (callingClass != null) {
				string fullname = callingClass.FullName;
				string[] namespaces = fullname.Split(new char[] {'.'});
				string curnamespace = "";
				int i = 0;
				
				do {
					curnamespace += namespaces[i] + '.';
					c = GetType (curnamespace + name, genericParameters, false, true);
					if (c != null) {
						return c;
					}
					i++;
				}
				while (i < namespaces.Length);
			}
			
			// Now try to find the class using the included namespaces
			
			if (unit != null) {
				foreach (IUsing u in unit.Usings) {
					if (u != null) {
						c = SearchType (u, name, genericParameters, true);
						if (c != null) {
							return c;
						}
					}
				}
			}
			
			return null;
		}
		
		IReturnType FindAlias (string name, IEnumerable<IUsing> usings)
		{
			// If the name matches an alias, try using the alias first.
			if (usings == null)
				return null;
				
			foreach (IUsing u in usings) {
				if (u != null) {
					IReturnType a;
					if (u.Aliases.TryGetValue (name, out a))
						return a;
				}
			}
			return null;
		}
		
		public IType SearchType (IUsing iusing, string partitialTypeName, IList<IReturnType> genericArguments, bool caseSensitive)
		{
			IType c = GetType (partitialTypeName, genericArguments, false, caseSensitive);
			if (c != null) {
				return c;
			}
			
			foreach (string str in iusing.Namespaces) {
				string possibleType = String.Concat(str, ".", partitialTypeName);
				c = GetType (possibleType, genericArguments, false, caseSensitive);
				if (c != null)
					return c;
			}

			IReturnType alias;
			// search class in partial namespaces
			if (iusing.Aliases.TryGetValue ("", out alias)) {
				string declaringNamespace = alias.FullName;
				while (declaringNamespace.Length > 0) {
					string className = String.Concat(declaringNamespace, ".", partitialTypeName);
					c = GetType (className, genericArguments, false, caseSensitive);
					if (c != null)
						return c;
					int index = declaringNamespace.IndexOf('.');
					if (index > 0) {
						declaringNamespace = declaringNamespace.Substring(0, index);
					} else {
						break;
					}
				}
			}
			
			foreach (string aliasString in iusing.Aliases.Keys) {
				if (caseSensitive ? partitialTypeName.StartsWith(aliasString) : partitialTypeName.ToLower().StartsWith(aliasString.ToLower())) {
					string className = null;
					if (aliasString.Length > 0) {
						IReturnType rt = iusing.Aliases [aliasString];
						className = String.Concat (rt.FullName, partitialTypeName.Remove (0, aliasString.Length));
						c = GetType (className, genericArguments, false, caseSensitive);
						if (c != null)
							return c;
					}
				}
			}
			
			return null;
		}
		
		internal virtual void GetNamespaceContentsInternal (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
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
		public virtual List<IMember> GetNamespaceContents (IEnumerable<string> subNamespaces, bool includeReferences, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			GetNamespaceContentsInternal (result, subNamespaces, caseSensitive);
			if (includeReferences) {
				foreach (ProjectDom reference in references) {
					reference.GetNamespaceContentsInternal (result, subNamespaces, caseSensitive);
				}
			}
			return result;
		}
		
		public bool NamespaceExists (string namespaceName)
		{
			return NamespaceExists (namespaceName, false);
		}
		
		public bool NamespaceExists (string namespaceName, bool searchDeep)
		{
			return NamespaceExists (namespaceName, searchDeep, true);
		}
		
		public virtual bool NamespaceExists (string namespaceName, bool searchDeep, bool caseSensitive)
		{
			List<IMember> members = GetNamespaceContents (namespaceName, searchDeep, caseSensitive);
			return members != null && members.Count > 0;
		}
		
		public virtual bool NeedCompilation (string fileName)
		{
			return false;
		}
		
		public abstract IEnumerable<IType> GetSubclasses (IType type);
		
		
		public IType GetType (IReturnType returnType)
		{
			if (returnType.Type != null)  {
				if (returnType.GenericArguments == null || returnType.GenericArguments.Count == 0)
					return returnType.Type;
				return DomType.CreateInstantiatedGenericType (returnType.Type, returnType.GenericArguments);
			}
			return GetType (returnType.FullName, returnType.GenericArguments, true, true);
		}
		
		public IType GetType (IReturnType returnType, bool searchDeep)
		{
			if (returnType == null)
				return null;
			if (returnType.Type != null)
				return returnType.Type ;
			return GetType (returnType.FullName, returnType.GenericArguments, searchDeep, true);
		}
		
		public IType GetType (string typeName)
		{
			return GetType (typeName, null, true, true);
		}
		
		public IType GetType (string typeName, IList<IReturnType> genericArguments)
		{
			return GetType (typeName, genericArguments, true, true);
		}
		
		public IType GetType (string typeName, bool deepSearchReferences)
		{
			return GetType (typeName, null, deepSearchReferences, true);
		}
		
		public IType GetType (string typeName, bool deepSearchReferences, bool caseSensitive)
		{
			return GetType (typeName, null, deepSearchReferences, caseSensitive);
		}
		
		public abstract IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive);

		
		internal virtual TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit unit)
		{
			return null;
		}

		internal virtual void Unload ()
		{
		}
		
		internal void FireLoaded ()
		{
			if (Loaded != null) {
				Loaded (this, EventArgs.Empty);
			}
		}

		internal void UpdateReferences ()
		{
			if (references == null)
				references = new List<ProjectDom> ();

			List<ProjectDom> refs = new List<ProjectDom> ();
			foreach (string uri in OnGetReferences ()) {
				int curRefCount = ReferenceCount;
				ProjectDom dom = ProjectDomService.GetDom (uri, true);
				ReferenceCount = curRefCount;
				if (dom == this)
					continue;
				if (dom != null)
					refs.Add (dom);
			}
			List<ProjectDom> oldRefs = references;
			references = refs;
			foreach (ProjectDom dom in oldRefs)
				ProjectDomService.UnrefDom (dom.Uri); 
		}

		internal IReturnType GetSharedReturnType (IReturnType rt)
		{
			return DomReturnType.GetSharedReturnType (rt);
		}

		internal abstract IEnumerable<string> OnGetReferences ();

		internal virtual void OnProjectReferenceAdded (ProjectReference pref)
		{
			ProjectDom dom = ProjectDomService.GetDom (pref.Reference, true);
			if (dom != null)
				this.references.Add (dom);	
		}

		internal virtual void OnProjectReferenceRemoved (ProjectReference pref)
		{
			ProjectDom dom = ProjectDomService.GetDom (pref.Reference);
			if (dom != null) {
				this.references.Remove (dom);
				ProjectDomService.UnrefDom (dom.Uri); 
			}
		}

		// This method has to check all modified files and start parsing jobs if needed
		internal virtual void CheckModifiedFiles ()
		{
		}

		// This method can be overriden to flush cached data into the database
		internal virtual void Flush ()
		{
		}
		
		public event EventHandler Loaded;
	}

	public class SimpleProjectDom : ProjectDom
	{
		List<ICompilationUnit> units = new List<ICompilationUnit> ();

		public void Add (ICompilationUnit unit)
		{
			this.units.Add (unit);
		}
		
		public override IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			Stack<IType> typeStack = new Stack<IType> ();
			foreach (IType curType in Types) {
				typeStack.Push (curType);
				while (typeStack.Count > 0) {
					IType type = typeStack.Pop ();
					if (type.FullName == typeName) 
						return type;
					foreach (IType inner in type.InnerTypes) {
						typeStack.Push (inner);
					}
				}
			}
			return null;
		}

		public override IEnumerable<IType> Types {
			get {
				foreach (ICompilationUnit unit in units) {
					foreach (IType type in unit.Types) {
						yield return type;
					}
				}
			}
		}

		public override IEnumerable<IType> GetSubclasses (IType type)
		{
			yield return type;
		}

		internal override IEnumerable<string> OnGetReferences ()
		{
			yield break;
		}
	}
	public class EmptyProjectDom: ProjectDom
	{
		public override IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			return null;
		}

		public override IEnumerable<IType> Types {
			get {
				yield break;
			}
		}

		public override IEnumerable<IType> GetSubclasses (IType type)
		{
			yield break;
		}

		internal override IEnumerable<string> OnGetReferences ()
		{
			yield break;
		}
	}
}
