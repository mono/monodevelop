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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Projects;

namespace MonoDevelop.Projects.Dom.Parser
{
	public abstract class ProjectDom
	{	
		protected List<ProjectDom> references;
		Dictionary<string, IType> instantiatedTypeCache = new Dictionary<string, IType> ();
		
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

		// This method checks all modified source files, parses them and updates
		// the database. Calling this method is in general not required since
		// updating is automatically done in the background. However, in some
		// cases an up-to-date database is required to do some operation,
		// and this method will ensure that everything is up-to-date.
		public virtual void ForceUpdate ()
		{
			ForceUpdate (false);
		}
		
		public void ForceUpdate (bool updateReferences)
		{
			HashSet<ProjectDom> visited = new HashSet<ProjectDom> ();
			ForceUpdateRec (visited);
		}

		void ForceUpdateRec (HashSet<ProjectDom> visited)
		{
			if (!visited.Add (this))
				return;
			foreach (ProjectDom dom in References)
				dom.ForceUpdateRec (visited);
			ForceUpdate ();
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
			type = ResolveType (type);
			if (type == null)
				yield break;
			
			HashSet<string> alreadyTaken = new HashSet<string> ();
			Stack<IType> types = new Stack<IType> ();
			types.Push (type);
			while (types.Count > 0) {
				IType cur = types.Pop ();
				if (cur == null)
					continue;
				
				string fullName = DomType.GetNetFullName (cur);
				if (!alreadyTaken.Add (fullName))
					continue;
				
				yield return cur;
				
				foreach (IReturnType baseType in cur.BaseTypes) {
					// There is no need to resolve baseType here, since 'cur' is an already resolved
					// type, so all types it references are already resolved too
					IType resolvedType = GetType (baseType);
					if (resolvedType != null)
						types.Push (resolvedType);
				}

				if (cur.BaseType == null && cur.FullName != "System.Object") 
					types.Push (this.GetType (DomReturnType.Object));
			}
		}

		public IType ResolveType (IType type)
		{
			if (!((DomType)type).Resolved) {
				InstantiatedType itype = type as InstantiatedType;
				IType rtype;
				if (itype == null)
					rtype = GetType (type.FullName, type.TypeParameters.Count, true, true);
				else
					rtype = GetType (itype.UninstantiatedType.FullName, itype.GenericParameters, true, true);
				if (rtype != null)
					return rtype;
			}
			return type;
		}
		
		public virtual IType SearchType (SearchTypeRequest request)
		{
			return SearchType (request.Name, request.CallingType, request.CurrentCompilationUnit, request.GenericParameters);
		}
		
		internal IType SearchType (string name, IType callingClass, ICompilationUnit unit, IList<IReturnType> genericParameters)
		{
			// TODO dom check generic parameter count
			
			if (name == null || name == String.Empty)
				return null;
			
			IType c;
			// It may be one of the generic parameters in the calling class
			if (callingClass != null && (genericParameters == null || genericParameters.Count == 0)) {
				c = FindGenericParameter (unit, ResolveType (callingClass), name);
				if (c != null)
					return c;
			}

			// A known type?
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
				string[] namespaces = callingClass.FullName.Split ('.');
				for (int n = namespaces.Length - 1; n >= 0; n--) {
					string curnamespace = string.Join (".", namespaces, 0, n);
					c = GetType (curnamespace + "." + name, genericParameters, false, true);
					if (c != null)
						return c;
				}
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
		
		IType FindGenericParameter (ICompilationUnit cu, IType callingClass, string name)
		{
			foreach (TypeParameter tp in callingClass.TypeParameters) {
				if (tp.Name == name)
					return CreateInstantiatedParameterType (callingClass, tp);
			}
			if (callingClass.DeclaringType != null)
				return FindGenericParameter (cu, callingClass.DeclaringType, name);
			else
				return null;
		}
		
		IType SearchType (IUsing iusing, string partitialTypeName, IList<IReturnType> genericArguments, bool caseSensitive)
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
		
		void GetNamespaceContentsRec (List<IMember> result, HashSet<ProjectDom> visited, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			if (!visited.Add (this))
				return;
			GetNamespaceContentsInternal (result, subNamespaces, caseSensitive);
			foreach (ProjectDom reference in References)
				reference.GetNamespaceContentsRec (result, visited, subNamespaces, caseSensitive);
		}
		
		internal virtual void GetNamespaceContentsInternal (List<IMember> result, IEnumerable<string> subNamespaces, bool caseSensitive)
		{
			foreach (IType type in Types) {
				string fullName = type.FullName;
				foreach (string subNamespace in subNamespaces) {
					if (fullName.StartsWith (subNamespace, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)) {
						if (subNamespace.Length == fullName.Length)
							continue;
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
			HashSet<string> uniqueNamespaces = new HashSet<string> (subNamespaces);
			if (includeReferences)
				GetNamespaceContentsRec (result, new HashSet<ProjectDom> (), uniqueNamespaces, caseSensitive);
			else
				GetNamespaceContentsInternal (result, uniqueNamespaces, caseSensitive);
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
			foreach (IMember member in members) {
				IType t = member as IType;
				if (t == null)
					continue;
				if (t.Namespace == namespaceName || t.Namespace.StartsWith (namespaceName + "."))
					return true;
			}
			return false;
		}
		
		public virtual bool NeedCompilation (string fileName)
		{
			return false;
		}
		
		public IEnumerable<IType> GetSubclasses (IType type)
		{
			return GetSubclasses (type, true);
		}
		
		public IEnumerable<IType> GetSubclasses (IType type, bool searchDeep)
		{
			return GetSubclasses (type, searchDeep, null);
		}
		
		public abstract IEnumerable<IType> GetSubclasses (IType type, bool searchDeep, IList<string> namespaces);
		
		public IType GetType (IReturnType returnType)
		{
			if (returnType == null)
				return null;
			if (returnType.Type != null)  {
				if (returnType.GenericArguments == null || returnType.GenericArguments.Count == 0)
					return returnType.Type;
				return CreateInstantiatedGenericType (returnType.Type, returnType.GenericArguments);
			}
			return GetType (((DomReturnType)returnType).DecoratedFullName, returnType.GenericArguments, true, true);
		}
		
		public IType GetType (IReturnType returnType, bool searchDeep)
		{
			if (returnType == null)
				return null;
			if (returnType.Type != null)
				return returnType.Type;
			if (returnType.Parts.Count == 1)
				return GetType (returnType.FullName, returnType.GenericArguments, searchDeep, true);
			else {
				IReturnTypePart part = returnType.Parts [0];
				string name = returnType.Namespace.Length > 0 ? returnType.Namespace + "." + part.Name : part.Name;
				IType ptype = GetType (name, part.GenericArguments, searchDeep, true);
				if (ptype == null)
					return null;
				for (int n=1; n<returnType.Parts.Count; n++) {
					part = returnType.Parts [n];
					ptype = FindInnerType (ptype, part.Name, part.GenericArguments.Count, true);
					if (ptype == null)
						return null;
					if (part.GenericArguments.Count > 0)
						ptype = CreateInstantiatedGenericType (ptype, part.GenericArguments);
				}
				return ptype;
			}
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
		
		public IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences)
		{
			return GetType (typeName, genericArgumentsCount, deepSearchReferences, true);
		}
		
		public abstract IType GetType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive);
		public abstract IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive);

		internal IType FindInnerType (IType outerType, string name, int typeArgCount, bool caseSensitive)
		{
			foreach (IType innerc in outerType.InnerTypes)  {
				if (string.Compare (innerc.Name, name, !caseSensitive) == 0 && innerc.TypeParameters.Count == typeArgCount) {
					return innerc;
				}
			}
			// Check type parameters
			if (typeArgCount == 0) {
				foreach (TypeParameter tp in outerType.TypeParameters) {
					if (string.Compare (tp.Name, name, !caseSensitive) == 0)
						return CreateInstantiatedParameterType (outerType, tp);
				}
			}
			return null;
		}
		
		internal virtual TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit unit)
		{
			return null;
		}

		internal virtual void Unload ()
		{
			if (references != null) {
				foreach (ProjectDom dom in references)
					ProjectDomService.UnrefDom (dom.Uri);
			}
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
			if (dom != null && references != null)
				this.references.Add (dom);	
		}

		internal virtual void OnProjectReferenceRemoved (ProjectReference pref)
		{
			ProjectDom dom = ProjectDomService.GetDom (pref.Reference);
			if (dom != null && references != null) {
				this.references.Remove (dom);
				ProjectDomService.UnrefDom (dom.Uri); 
			}
		}
		
		public IType CreateInstantiatedGenericType (IType type, IList<IReturnType> genericArguments)
		{
			if (type is InstantiatedType)
				return type;
			
			string name = DomType.GetInstantiatedTypeName (type.FullName, genericArguments);
			
			lock (instantiatedTypeCache) {
				IType gtype;
				if (instantiatedTypeCache.TryGetValue (name, out gtype))
					return gtype;
				
				gtype = DomType.CreateInstantiatedGenericTypeInternal (type, genericArguments);
				instantiatedTypeCache [name] = gtype;
				return gtype;
			}
		}
		
		public IType CreateInstantiatedParameterType (IType outerType, TypeParameter tp)
		{
			return new InstantiatedParameterType (this, outerType, tp);
		}
		
		internal void ResetInstantiatedTypes (IType type)
		{
			string typePrefix = type.FullName + "[";
			lock (instantiatedTypeCache) {
				var toDelete = new List<string> ();
				foreach (string tname in instantiatedTypeCache.Keys) {
					if (tname.StartsWith (typePrefix))
						toDelete.Add (tname);
				}
				foreach (string td in toDelete)
					instantiatedTypeCache.Remove (td);
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
			int i = typeName.IndexOf ('`');
			if (i != -1)
				typeName = typeName.Substring (0, i);
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

		public override IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			return GetType (typeName, null, deepSearchReferences, caseSensitive);
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

		public override IEnumerable<IType> GetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			if (namespaces == null || namespaces.Contains (type.Namespace))
				yield return type;
			else
				yield break;
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

		public override IType GetType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			return null;
		}


		public override IEnumerable<IType> Types {
			get {
				yield break;
			}
		}

		public override IEnumerable<IType> GetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			yield break;
		}

		internal override IEnumerable<string> OnGetReferences ()
		{
			yield break;
		}
	}
}
