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
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom.Parser
{
	public abstract class ProjectDom: IDomObjectTable
	{	
		protected List<ProjectDom> references; 
		Dictionary<string, IType> instantiatedTypeCache = new Dictionary<string, IType> ();
		Dictionary<string, IReturnType> returnTypeCache = new Dictionary<string, IReturnType> ();
		
		public Project Project;
		internal int ReferenceCount;
		internal string Uri;
		
		public static readonly ProjectDom Empty = new EmptyProjectDom ();

		public virtual ReadOnlyCollection<ProjectDom> References {
			get {
				if (references == null)
					UpdateReferences ();
				return references.AsReadOnly ();
			}
		}
		
		public abstract IEnumerable<IType> Types { get; }

		public virtual IEnumerable<IAttribute> Attributes {
			get {
				yield break;
			}
		}

		protected virtual IEnumerable<string> InternalResolvePossibleNamespaces (IReturnType returnType)
		{
			if (returnType == null) 
				yield break;
			
			foreach (IType type in Types) {
				if (type.DecoratedFullName == type.Namespace + "." + returnType.DecoratedFullName) {
					yield return type.Namespace;
				}
			}
		}
		
		public virtual IEnumerable<string> ResolvePossibleNamespaces (IReturnType returnType)
		{
			foreach (string ns in InternalResolvePossibleNamespaces (returnType)) {
				yield return ns;
			}
			foreach (ProjectDom refDom in References) {
				foreach (string ns in refDom.InternalResolvePossibleNamespaces (returnType)) {
					yield return ns;
				}
			}
		}

		public virtual IList<Tag> GetSpecialComments (FilePath fileName)
		{
			return new Tag[0];
		}
		
		public virtual void UpdateTagComments (FilePath fileName, IList<Tag> tags)
		{
		
		}

		// This method checks all modified source files, parses them and updates
		// the database. Calling this method is in general not required since
		// updating is automatically done in the background. However, in some
		// cases an up-to-date database is required to do some operation,
		// and this method will ensure that everything is up-to-date.
		public virtual void ForceUpdate ()
		{
			//HACK: stetic depends on the old, broken behaviour, so this overload of ForceUpdate uses
			//the old behaviour
			ForceUpdateBROKEN ();
			//ForceUpdate (false);
		}
		
		protected virtual void ForceUpdateBROKEN ()
		{
			HashSet<ProjectDom> visited = new HashSet<ProjectDom> ();
			ForceUpdateRecBROKEN (visited);
		}

		void ForceUpdateRecBROKEN (HashSet<ProjectDom> visited)
		{
			if (!visited.Add (this))
				return;
			foreach (ProjectDom dom in References)
				dom.ForceUpdateRecBROKEN (visited);
			ForceUpdateBROKEN ();
		}
		
		public virtual void ForceUpdate (bool updateReferences)
		{
			if (updateReferences) {
				HashSet<ProjectDom> visited = new HashSet<ProjectDom> ();
				ForceUpdateRec (visited);
			} else {
				CheckModifiedFiles ();
			}
			
			ProjectDomService.WaitForParseQueue ();
		}

		void ForceUpdateRec (HashSet<ProjectDom> visited)
		{
			if (!visited.Add (this))
				return;
			CheckModifiedFiles ();
			foreach (ProjectDom dom in References)
				dom.ForceUpdateRec (visited);
		}
		
		public virtual string GetDocumentation (IMember member)
		{
			return member != null ? member.Documentation : null;
		}
		
		public virtual IEnumerable<IType> GetTypes (FilePath fileName)
		 {
			foreach (IType type in Types) {
				if (type.CompilationUnit.FileName == fileName)
					yield return type;
			}
		}
		
		public virtual IEnumerable<IType> GetInheritanceTree (IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
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
				if (!alreadyTaken.Add (fullName)) {
					continue;
				}
				yield return cur;
				
				foreach (IReturnType baseType in cur.BaseTypes) {
					// There is no need to resolve baseType here, since 'cur' is an already resolved
					// type, so all types it references are already resolved too
					IType resolvedType = GetType (baseType);
					if (resolvedType != null) {
						types.Push (resolvedType);
					}
				}
				if (cur.BaseType == null && cur.FullName != "System.Object") {
					if (cur.ClassType == ClassType.Enum) {
						types.Push (this.GetType (DomReturnType.Enum));
					} else if (cur.ClassType == ClassType.Struct) {
						types.Push (this.GetType (DomReturnType.ValueType));
					} else {
						types.Push (this.GetType (DomReturnType.Object));
					}
				}
			}
		}

		public virtual IType ResolveType (IType type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
			if (!((DomType)type).Resolved) {
				InstantiatedType itype = type as InstantiatedType;
				IType rtype;
				if (itype == null)
					rtype = GetType (type.FullName, type.TypeParameters.Count, true, true);
				else
					rtype = GetType (itype.UninstantiatedType.FullName, itype.GenericParameters, true, true);
				
				rtype = MergeTypeWithTemporary (rtype);
				
				if (type is CompoundType && rtype is CompoundType) {
					IType mainPart = ((CompoundType)type).Parts.First ();
					((CompoundType)rtype).SetMainPart (mainPart.CompilationUnit.FileName, mainPart.Location);
				}
				
				if (rtype != null)
					return rtype;
			}
			return type;
		}
		
		public virtual IType SearchType (ICompilationUnit unit, IType callingClass, DomLocation lookupLocation, string decoratedFullName)
		{
			if (string.IsNullOrEmpty (decoratedFullName))
				return null;
			return SearchType (unit, callingClass, lookupLocation, decoratedFullName, null);
		}
		
		public virtual IType SearchType (ICompilationUnit unit, IType callingClass, DomLocation lookupLocation, IReturnType returnType)
		{
			if (returnType == null)
				return null;
			return SearchType (unit, callingClass, lookupLocation, returnType.DecoratedFullName,returnType.GenericArguments);
		}
		
/*		public virtual IType SearchType (SearchTypeRequest request)
		{
			return SearchType (request.Name, request.CallingType, request.CurrentCompilationUnit, request.GenericParameters);
		}*/
		
		internal IType SearchType (ICompilationUnit unit, IType callingClass, DomLocation lookupLocation, string name, IList<IReturnType> genericParameters)
		{
			// TODO dom check generic parameter count
			if (string.IsNullOrEmpty (name))
				return null;
			
			IType result = null;
			// It may be one of the generic parameters in the calling class
			if (genericParameters == null || genericParameters.Count == 0) {
				if (callingClass != null) {
					result = FindGenericParameter (unit, ResolveType (callingClass), name);
					if (result != null)
						return result;
				}
			}
			
			// A known type?
			result = GetType (name, genericParameters, false, true);
			if (result != null)
				return result;

			// Maybe an inner type that isn't fully qualified?
			if (callingClass != null) {
				IType t = ResolveType (callingClass);
				result = SearchInnerType (t, name.Split ('.'), 0, genericParameters != null ? genericParameters.Count : 0, true);
				if (result != null) {
					if (genericParameters != null && genericParameters.Count > 0) 
						return CreateInstantiatedGenericType (result, genericParameters);
					return result;
				}
			}
			// If the name matches an alias, try using the alias first.
			if (unit != null) {
				IReturnType ualias = FindAlias (name, unit.Usings);
				if (ualias != null) {
					// Don't provide the compilation unit when trying to resolve the alias,
					// since aliases are not affected by other 'using' directives.
					result = GetType (ualias.FullName, ualias.GenericArguments, false, true);
					if (result != null)
						return result;
				}
			}
			
			// The enclosing namespace has preference over the using directives.
			// Check it now.
			if (callingClass != null) {
				List<int> indices = new List<int> ();
				string str = callingClass.FullName;
				for (int i = 0; i < str.Length; i++) {
					if (str[i] == '.')
						indices.Add (i);
				}
				for (int n = indices.Count - 1; n >= 0; n--) {
					if (n < 1)
						continue;
					string curnamespace = str.Substring (0, indices[n] + 1);
					result = GetType (curnamespace + name, genericParameters, false, true);
					if (result != null)
						return result;
				}
			}
			
			// Now try to find the class using the included namespaces
			if (unit != null) {
				// it's a difference having using A; namespace B { } or namespace B { using A;  }, when a type 
				// request equals a namespace name; for example the type B could be resolved in the 2nd case when a type
				// A.B exists but not in the 1st.
				foreach (IUsing u in unit.Usings.Reverse ()) {
					if (u.Namespaces.Contains (name)) 
						return null;
					if (callingClass != null && !u.ValidRegion.Contains (lookupLocation))
						continue;
					if (u != null) {
						result = SearchType (u, name, genericParameters, true);
						if (result != null)
							return result;
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
		
		IType FindGenericParameter (ICompilationUnit cu, ITypeParameterMember callingClass, string name)
		{
			foreach (TypeParameter tp in callingClass.TypeParameters) {
				if (tp.Name == name)
					return CreateInstantiatedParameterType (callingClass, tp);
			}
			if (callingClass.DeclaringType != null)
				return FindGenericParameter (cu, callingClass.DeclaringType, name);
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
		
		public virtual List<IMember> GetNamespaceContents (string subNamespace, bool includeReferences, bool caseSensitive)
		{
			return GetNamespaceContents (new string[] { subNamespace }, includeReferences, caseSensitive);
		}
		public virtual List<IMember> GetNamespaceContents (IEnumerable<string> subNamespaces, bool includeReferences, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			HashSet<string> uniqueNamespaces = new HashSet<string> (subNamespaces);
			GetNamespaceContentsInternal (result, uniqueNamespaces, caseSensitive);
			if (includeReferences) {
				foreach (ProjectDom reference in References) {
					reference.GetNamespaceContentsInternal (result, uniqueNamespaces, caseSensitive);
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
			foreach (IMember member in members) {
				IType t = member as IType;
				if (t == null)
					continue;
				if (t.Namespace == namespaceName || t.Namespace.StartsWith (namespaceName + "."))
					return true;
			}
			return false;
		}

		public virtual bool NeedCompilation (FilePath fileName)
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
		
		protected abstract IEnumerable<IType> InternalGetSubclasses (IType type, bool searchDeep, IList<string> namespaces);
		/*
		IEnumerable<IType> GetSubclassesInternalSafe (IType btype, bool includeReferences)
		{
			string decoratedName = btype.DecoratedFullName;
			string genericName = decoratedName;
			if (btype is InstantiatedType)
				genericName = ((InstantiatedType)btype).UninstantiatedType.DecoratedFullName;
			
			
			Stack<IType> types = new Stack <IType> (Types);
			while (types.Count > 0) {
				IType t = types.Pop ();
				foreach (var inner in t.InnerTypes)
					types.Push (inner);
				System.Console.WriteLine (t.DecoratedFullName);
				foreach (var v in t.SourceProjectDom.GetInheritanceTree (t)) {
					if (v.DecoratedFullName == t.DecoratedFullName)
						continue;
					if (t.DecoratedFullName.StartsWith ("Library2.GenericBin["))
						System.Console.WriteLine (v);
					bool isInTree;
					if (v is InstantiatedType) {
						isInTree = v.DecoratedFullName == decoratedName;
					} else {
						isInTree = v.DecoratedFullName == genericName;
					}
					if (isInTree) {
						yield return t;
					}
				}
			}
			
			if (includeReferences) {
				foreach (var refDom in References) {
					foreach (var t in refDom.GetSubclassesInternalSafe (btype, false))
						yield return t;
				}
			}
		}*/
		
		public virtual IEnumerable<IType> GetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			foreach (IType subType in InternalGetSubclasses (type, searchDeep, null)) {
				yield return subType;
			}
			if (type is InstantiatedType) {
				InstantiatedType iType = type as InstantiatedType;
				
				if (iType.UninstantiatedType.FullName == "System.Collections.Generic.IEnumerable" ||
				    iType.UninstantiatedType.FullName == "System.Collections.Generic.ICollection" ||
				    iType.UninstantiatedType.FullName == "System.Collections.Generic.IList") {
					if (iType.GenericParameters != null && iType.GenericParameters.Count > 0) {
						yield return GetArrayType (iType.GenericParameters[0]);
					}
				}
			}
			if (type.FullName == "System.Collections.IEnumerable" || type.FullName == "System.Collections.ICollection" || type.FullName == "System.Collections.IList") {
				foreach (IType t in GetSubclasses (GetType(DomReturnType.Object), true, namespaces)) {
					yield return GetArrayType (new DomReturnType (t));
				}
			}
		}
		
		public IType GetArrayType (IReturnType elementType)
		{
			return GetArrayType (elementType, MonoDevelop.Projects.Dom.Output.AmbienceService.DefaultAmbience);
		}
		
		
		public IType GetArrayType (IReturnType elementType, MonoDevelop.Projects.Dom.Output.Ambience ambience)
		{
			// Create a fake class which sublcasses System.Array and implements IList<T>
			DomType t = new DomType (ambience.GetString (elementType, MonoDevelop.Projects.Dom.Output.OutputFlags.UseFullName) + "[]");
			
			// set the compilation unit of the array type to that of the element type - it's required for jumping to the declaration of the type.
			IType eType = GetType (elementType);
			if (eType != null)
				t.CompilationUnit = eType.CompilationUnit;
			
			t.Resolved = true;
			t.BaseType = new DomReturnType ("System.Array");
			t.ClassType = ClassType.Class;
			t.Modifiers = Modifiers.Public;
			t.SourceProjectDom = this;
			DomProperty indexer = new DomProperty ();
			indexer.Name = "Item";
			indexer.SetterModifier = indexer.GetterModifier = Modifiers.Public;
			indexer.PropertyModifier |= PropertyModifier.IsIndexer;
			indexer.Add (new DomParameter(indexer, "index", DomReturnType.Int32));
			indexer.ReturnType = elementType;
			t.Add (indexer);
			DomReturnType listType = new DomReturnType ("System.Collections.Generic.IList", false, new IReturnType [] { elementType });
			
			t.AddInterfaceImplementation (listType);
			return t;
		}
		
		public virtual IType GetType (IReturnType returnType)
		{
			if (returnType == null)
				return null;
			
			if (returnType.ArrayDimensions > 0) {
				DomReturnType newType = new DomReturnType (returnType.FullName);
				// dimensions are correctly updated when cropped
				newType.ArrayDimensions = returnType.ArrayDimensions - 1;
				newType.PointerNestingLevel = returnType.PointerNestingLevel;
				return GetArrayType (newType);
			}
			
			IType type = returnType.Type ?? GetType (((DomReturnType)returnType).DecoratedFullName, returnType.GenericArguments, true, true);
			if (type != null)  {
				if (type.Kind == TypeKind.GenericInstantiation || type.Kind == TypeKind.GenericParameter)
					return type;
				if (!returnType.Parts.Any (part => part.GenericArguments.Count != 0))
					return type;
				List<IReturnType> aggregatedGenerics = new List<IReturnType> ();
				foreach (IReturnTypePart part in returnType.Parts) {
					aggregatedGenerics.AddRange (part.GenericArguments);
				}
				
				return CreateInstantiatedGenericType (type, aggregatedGenerics);
			}
			return type;
/*			
			IReturnTypePart part = returnType.Parts [0];
			string name = !string.IsNullOrEmpty (returnType.Namespace) ? returnType.Namespace + "." + part.Name : part.Name;
			IType ptype = GetType (name, part.GenericArguments, true, true);
			if (ptype == null)
				return null;
			for (int n=1; n < returnType.Parts.Count; n++) {
				part = returnType.Parts [n];
				ptype = SearchInnerType (ptype, part.Name, part.GenericArguments.Count, true);
				if (ptype != null)
					break;
				if (ptype == null)
					return null;
				if (part.GenericArguments.Count > 0)
					ptype = CreateInstantiatedGenericType (ptype, part.GenericArguments);
			}
			return ptype;
			*/
			
			
		}
		/*
		public IType GetType (IReturnType returnType, bool searchDeep)
		{
			if (returnType == null)
				return null;
			if (returnType.Type != null)
				return returnType.Type;
			if (returnType.Parts.Count == 1)
				return GetType (returnType.FullName, returnType.GenericArguments, searchDeep, true);
			
			IReturnTypePart part = returnType.Parts [0];
			string name = returnType.Namespace.Length > 0 ? returnType.Namespace + "." + part.Name : part.Name;
			IType ptype = GetType (name, part.GenericArguments, searchDeep, true);
			if (ptype == null)
				return null;
			for (int n=1; n < returnType.Parts.Count; n++) {
				part = returnType.Parts [n];
				ptype = SearchInnerType (ptype, part.Name, part.GenericArguments.Count, true);
				if (ptype != null)
					break;
				if (ptype == null)
					return null;
				if (part.GenericArguments.Count > 0)
					ptype = CreateInstantiatedGenericType (ptype, part.GenericArguments);
			}
			return ptype;
		}*/
		
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

		
		internal static int ExtractGenericArgCount (ref string name)
		{
			int i = name.LastIndexOf ('`');
			if (i != -1) {
				int typeParams;
				if (int.TryParse (name.Substring (i + 1), out typeParams)) {
					name = name.Substring (0, i);
					return typeParams;
				}
			}
			return 0;
		}
		
		internal IType SearchInnerType (IType outerType, string[] names, int firstIndex, int finalTypeArgCount, bool caseSensitive)
		{
			int nextPos = firstIndex;
			int len = names.Length;
			IType c = outerType;
			while (nextPos < len) {
				string name = names[nextPos];
				int partArgsCount = nextPos == len-1 ? finalTypeArgCount : ExtractGenericArgCount (ref name);
				IType nextc = SearchInnerType (c, name, partArgsCount, caseSensitive);
				if (nextc == null) 
					return null;
				c = nextc;
				nextPos++;
			}
			return c;
		}
		
		internal IType SearchInnerType (IType outerType, string name, int typeArgCount, bool caseSensitive)
		{
			foreach (IType inheritedType in GetInheritanceTree (outerType)) {
				IType c = FindInnerTypeInClass (inheritedType, name, typeArgCount, caseSensitive);
				if (c != null) {
					return c;
				}
			}
			if (outerType.DeclaringType != null) {
				IType c = SearchInnerType (outerType.DeclaringType, name, typeArgCount, caseSensitive);
				if (c != null) {
					return c;
				}
			}
			return null;
		}
		
/*		IType FindInnerTypeInClass (IType outerType, string[] names, int firstIndex, int finalTypeArgCount, bool caseSensitive)
		{
			int nextPos = firstIndex;
			int len = names.Length;
			IType c = outerType;
			while (nextPos < len) {
				string name = names[nextPos];
				int partArgsCount = nextPos == len-1 ? finalTypeArgCount : ExtractGenericArgCount (ref name);
				IType nextc = FindInnerTypeInClass (c, name, partArgsCount, caseSensitive);
				if (nextc == null) return null;
				c = nextc;
				nextPos++;
			}
			return c;
		}*/
		IType FindInnerTypeInClass (IType outerType, string name, int typeArgCount, bool caseSensitive)
		{
			foreach (IType innerc in outerType.InnerTypes) {
				if (string.Compare (innerc.Name, name, !caseSensitive) == 0 && innerc.TypeParameters.Count == typeArgCount)
					return innerc;
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
		
		public virtual TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit unit, bool isFromFile)
		{
			return null;
		}
		
		Dictionary<string, ICompilationUnit> temporaryCompilationUnits = new Dictionary<string, ICompilationUnit> ();
		public void RemoveTemporaryCompilationUnit (ICompilationUnit unit)
		{
			if (temporaryCompilationUnits.ContainsKey (unit.FileName))
				temporaryCompilationUnits.Remove (unit.FileName);
		}
		
		public virtual void UpdateTemporaryCompilationUnit (ICompilationUnit unit)
		{
			temporaryCompilationUnits[unit.FileName] = unit;
		}
		
		protected IType GetTemporaryType (string typeName, IList<IReturnType> genericArguments, bool deepSearchReferences, bool caseSensitive)
		{
			foreach (var unit in temporaryCompilationUnits.Values) {
				var result = unit.GetType (typeName, genericArguments != null ? genericArguments.Count : 0);
				if (result == null)
					continue;
				return genericArguments == null || genericArguments.Count == 0 ? result : CreateInstantiatedGenericType (result, genericArguments);
			}
			if (deepSearchReferences) {
				foreach (var r in References) {
					var result = r.GetTemporaryType (typeName, genericArguments, false, caseSensitive);
					if (result != null)
						return result;
				}
			}
			return null;
		}
		
		protected IType GetTemporaryType (string typeName, int genericArgumentsCount, bool deepSearchReferences, bool caseSensitive)
		{
			foreach (var unit in temporaryCompilationUnits.Values) {
				var result = unit.GetType (typeName, genericArgumentsCount);
				if (result == null)
					continue;
				return result;
			}
			
			if (deepSearchReferences) {
				foreach (var r in References) {
					var result = r.GetTemporaryType (typeName, genericArgumentsCount, false, caseSensitive);
					if (result != null)
						return result;
				}
			}
			return null;
		}
		
		public IType MergeTypeWithTemporary (IType type)
		{
			if (type == null || type.CompilationUnit == null || string.IsNullOrEmpty (type.CompilationUnit.FileName))
				return type;
			
			if (temporaryCompilationUnits.ContainsKey (type.CompilationUnit.FileName)) {
				var unit = temporaryCompilationUnits[type.CompilationUnit.FileName];
				IType tmpType = unit.Types.FirstOrDefault (t => t.Location == type.Location || t.DecoratedFullName == type.DecoratedFullName);
				
				if (tmpType != null) {
					CompoundType result = new CompoundType ();
					result.AddPart (type);
					result.AddPart (tmpType);
					return result;
				}
			}
			return type;
		}

		internal virtual void Unload ()
		{
			if (references != null) {
				foreach (ProjectDom dom in references)
					ProjectDomService.UnrefDom (dom.Uri);
			}
			OnUnloaded ();
			references.Clear ();
			instantiatedTypeCache.Clear ();
			returnTypeCache.Clear ();
			Project = null;
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
			OnReferencesUpdated ();
		}
		
		object IDomObjectTable.GetSharedObject (object ob)
		{
			if (ob is IReturnType)
				return GetSharedReturnType ((IReturnType)ob);
			else
				return ob;
		}
		
		internal IEnumerable<IReturnType> GetSharedReturnTypes ()
		{
			return returnTypeCache.Values;
		}
		
		internal IReturnType GetSharedReturnType (IReturnType rt)
		{
			string id = rt.ToInvariantString ();
			IReturnType s;
			if (!returnTypeCache.TryGetValue (id, out s)) {
				s = DomReturnType.GetSharedReturnType (rt, true);
				if (s == null) {
					s = rt;
					returnTypeCache [id] = rt;
				}
			}
			return s;
		}
		
		internal virtual ProjectDomStats GetStats ()
		{
			ProjectDomStats stats = new ProjectDomStats ();
			
			StatsVisitor v = new StatsVisitor (stats);
			v.SharedTypes = GetSharedReturnTypes ().ToArray ();
			foreach (IType t in instantiatedTypeCache.Values) {
				stats.InstantiatedTypes++;
				v.Reset ();
				v.Visit (t, "Instantiated/");
				if (v.Failures.Count > 0) {
					stats.UnsharedReturnTypes += v.Failures.Count;
					stats.ClassesWithUnsharedReturnTypes++;
				}
			}
			return stats;
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
		
		public virtual IType CreateInstantiatedGenericType (IType type, IList<IReturnType> genericArguments)
		{
			if (genericArguments == null || type == null || type is InstantiatedType)
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
		
		public virtual IType CreateInstantiatedParameterType (ITypeParameterMember typeParameterMember, TypeParameter tp)
		{
			return new InstantiatedParameterType (this, typeParameterMember, tp);
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
		
		void OnUnloaded ()
		{
			var evt = Unloaded;
			if (evt != null)
				evt (this, EventArgs.Empty);
		}
		
		void OnReferencesUpdated ()
		{
			var evt = ReferencesUpdated;
			if (evt != null)
				evt (this, EventArgs.Empty);
		}
		
		public event EventHandler Unloaded;
		public event EventHandler ReferencesUpdated;
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
			List<IType> result = new List<IType> ();
			Stack<IType> typeStack = new Stack<IType> ();
			foreach (IType curType in Types) {
				typeStack.Push (curType);
				while (typeStack.Count > 0) {
					IType type = typeStack.Pop ();
					if (type.FullName == typeName)
						result.Add (type);
					foreach (IType inner in type.InnerTypes) {
						typeStack.Push (inner);
					}
				}
			}
			if (result.Count == 1)
				return result[0];
			if (result.Count > 1)
				return new CompoundType (result);
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

		public override IEnumerable<IAttribute> Attributes {
			get {
				foreach (ICompilationUnit unit in units) {
					foreach (var att in unit.Attributes) {
						yield return att;
					}
				}
			}
		}
		
		protected override IEnumerable<IType> InternalGetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
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

		protected override IEnumerable<IType> InternalGetSubclasses (IType type, bool searchDeep, IList<string> namespaces)
		{
			yield break;
		}

		internal override IEnumerable<string> OnGetReferences ()
		{
			yield break;
		}
	}
}
