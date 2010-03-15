//
// DomType.cs
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
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Dom
{
	public class DomType : AbstractMember, IType
	{
		protected ProjectDom sourceProjectDom;
		protected ICompilationUnit compilationUnit;
		protected IReturnType baseType;
		
		static readonly ReadOnlyCollection<ITypeParameter> emptyParamList = new List<ITypeParameter> ().AsReadOnly ();
		static readonly ReadOnlyCollection<IReturnType> emptyTypeList = new List<IReturnType> ().AsReadOnly ();
		
		protected List<ITypeParameter> typeParameters      = null;

		List<IReturnType> implementedInterfaces  = null;
		
		protected ClassType classType = ClassType.Unknown;
		protected string nameSpace;
		
		public override MemberType MemberType {
			get {
				return MemberType.Type;
			}
		}

		public virtual TypeKind Kind {
			get {
				return TypeKind.Definition;
			}
		}
		
		public virtual TypeModifier TypeModifier {
			get;
			set;
		}
		
		protected override string CalculateFullName ()
		{
			base.fullNameIsDirty = false;
			if (DeclaringType != null) 
				return DeclaringType.FullName + "." + Name;
			return !string.IsNullOrEmpty (Namespace) ? Namespace + "." + Name : Name;
		}
		
		public string DecoratedFullName {
			get {
				StringBuilder result = new StringBuilder ();
				if (DeclaringType != null) {
					result.Append (DeclaringType.DecoratedFullName);
				} else {
					if (!string.IsNullOrEmpty (Namespace))
						result.Append (Namespace);
				}
				if (result.Length > 0)
					result.Append (".");
				result.Append (Name);
				if (TypeParameters.Count > 0) {
					result.Append ('`');
					result.Append (TypeParameters.Count);
				}
				
				return result.ToString ();
			}
		}
		protected void SetName (string fullName)
		{
			int idx = fullName.LastIndexOf ('.');
			if (idx >= 0) {
				Namespace = fullName.Substring (0, idx);
				Name      = fullName.Substring (idx + 1);
			} else {
				Namespace = "";
				Name      = fullName;
			}
		}
		
		internal bool Resolved { get; set; }
		
		public virtual string Namespace {
			get {
				if (DeclaringType != null) 
					return DeclaringType.Namespace;
				return nameSpace ?? "";
			}
			set {
				nameSpace = value;
				base.fullNameIsDirty = true;
			}
		}
		
		public virtual ProjectDom SourceProjectDom {
			get {
				if (sourceProjectDom == null && DeclaringType != null)
					return DeclaringType.SourceProjectDom;
				return sourceProjectDom;
			}
			set {
				sourceProjectDom = value;
			}
		}
		
		public SolutionItem SourceProject {
			get {
				return SourceProjectDom != null ? SourceProjectDom.Project : null;
			}
		}

		public virtual ICompilationUnit CompilationUnit {
			get {
				if (DeclaringType != null)
					return DeclaringType.CompilationUnit;
				return compilationUnit;
			}
			set {
				Parent = compilationUnit = value;
			}
		}
		
		public virtual ClassType ClassType {
			get {
				return classType;
			}
			set {
				classType = value;
			}
		}
		
		public virtual IReturnType BaseType {
			get {
				return baseType;
			}
			set {
				baseType = value;
			}
		}
		
		public virtual IEnumerable<IReturnType> BaseTypes {
			get {
				IReturnType baseType = BaseType;
				if (baseType != null)
					yield return baseType;
				for (int i = 0; i < ImplementedInterfaces.Count; i++) {
					yield return ImplementedInterfaces[i];
				}
			}
		}
		
		public virtual ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get {
				return implementedInterfaces != null ? implementedInterfaces.AsReadOnly () : emptyTypeList;
			}
		}
		
		public virtual ReadOnlyCollection<ITypeParameter> TypeParameters {
			get {
				return typeParameters != null ? typeParameters.AsReadOnly () : emptyParamList;
			}
		}
		
		public virtual IEnumerable<IMember> Members {
			get {
				INode child = FirstChild;
				while (child != null) {
					yield return (IMember)child;
					child = child.NextSibling;
				}
			}
		}
		
		public virtual IEnumerable<IType> InnerTypes {
			get {
				foreach (IMember item in Members)
					if (item.MemberType == MemberType.Type)
						yield return (IType)item;
			}
		}

		public virtual IEnumerable<IField> Fields {
			get {
				foreach (IMember item in Members)
					if (item.MemberType == MemberType.Field)
						yield return (IField)item;
			}
		}

		public virtual IEnumerable<IProperty> Properties {
			get {
				foreach (IMember item in Members)
					if (item.MemberType == MemberType.Property)
						yield return (IProperty)item;
			}
		}

		public virtual IEnumerable<IMethod> Methods {
			get {
				foreach (IMember item in Members)
					if (item.MemberType == MemberType.Method)
						yield return (IMethod)item;
			}
		}

		public virtual IEnumerable<IEvent> Events {
			get {
				foreach (IMember item in Members)
					if (item.MemberType == MemberType.Event)
						yield return (IEvent)item;
			}
		}
		
		public virtual IEnumerable<IType> Parts { 
			get {
				return new IType[] { this };
			}
		}
		
		public virtual bool HasParts {
			get {
				return false;
			}
		}
		
		public virtual bool HasExtensionMethods {
			get {
				foreach (IMethod m in Methods) {
					if (m.IsExtension)
						return true;
				}
				return false;
			}
		}
		
		
		static IconId[,] iconTable = new IconId[,] {
			{Stock.Error,     Stock.Error,            Stock.Error,              Stock.Error},             // unknown
			{Stock.Class,     Stock.PrivateClass,     Stock.ProtectedClass,     Stock.InternalClass},     // class
			{Stock.Enum,      Stock.PrivateEnum,      Stock.ProtectedEnum,      Stock.InternalEnum},      // enum
			{Stock.Interface, Stock.PrivateInterface, Stock.ProtectedInterface, Stock.InternalInterface}, // interface
			{Stock.Struct,    Stock.PrivateStruct,    Stock.ProtectedStruct,    Stock.InternalStruct},    // struct
			{Stock.Delegate,  Stock.PrivateDelegate,  Stock.ProtectedDelegate,  Stock.InternalDelegate}   // delegate
		};
		
		public override string HelpUrl {
			get {
				return "T:" + GetNetFullName (this);
			}
		}
		
		public override IconId StockIcon {
			get {
				return iconTable[(int)ClassType, ModifierToOffset (Modifiers)];
			}
		}
		
		public DomType ()
		{
		}
		
		public DomType (string fullName)
		{
			int idx = fullName.LastIndexOf ('.');
			if (idx < 0) {
				this.Name = fullName;
			} else {
				this.Namespace = fullName.Substring (0, idx); 
				this.Name = fullName.Substring (idx + 1);
			}
		}
		
		public DomType (ICompilationUnit compilationUnit, ClassType classType, string name, DomLocation location, string namesp, DomRegion region, List<IMember> members)
		{
			this.compilationUnit = compilationUnit;
			this.classType = classType;
			this.Name = name;
			this.Namespace = namesp;
			this.BodyRegion = region;
			this.Location = location;
			foreach (IMember member in members) {
				AddChild (member);
				((AbstractMember)member).DeclaringType = this;
			}
		}
		
		public DomType (ICompilationUnit compilationUnit, 
		                ClassType classType, 
		                Modifiers modifiers,
		                string name, 
		                DomLocation location, 
		                string namesp, 
		                DomRegion region)
		{
			this.compilationUnit = compilationUnit;
			this.classType   = classType;
			this.Modifiers   = modifiers;
			this.Name        = name;
			this.Namespace   = namesp;
			this.BodyRegion  = region;
			this.Location    = location;
		}
		
		System.Xml.XmlDocument helpXml;
		public System.Xml.XmlDocument HelpXml {
			get {
				if (helpXml == null && ProjectDomService.HelpTree != null)
					helpXml = ProjectDomService.HelpTree.GetHelpXml (this.HelpUrl);
				return helpXml;
			}
		}
		
		public override System.Xml.XmlNode GetMonodocDocumentation ()
		{
			if (HelpXml != null) {
				System.Xml.XmlNode result = HelpXml.SelectSingleNode ("/Type/Docs");
				return result;
			}
			return null;
		}
		
		public List<IMember> SearchMember (string name, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			StringComparison comp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			foreach (IMember member in this.Members) {
				if (0 == String.Compare (name, member.Name, comp)) {
					result.Add (member);
				}
			}
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[DomType: FullName={0}, #TypeArguments={2}, #Members={1}]", this.FullName, this.Members.Count (), TypeParameters.Count);
		}
		
		bool IsEqual (ReadOnlyCollection<IParameter> c1, ReadOnlyCollection<IParameter> c2)
		{
			if (c1 == null && c2 == null)
				return true;
			if (c1 == null || c2 == null || c1.Count != c2.Count)
				return false;
			for (int i = 0; i < c1.Count; i++) {
				if (c1[i].ReturnType.FullName != c2[i].ReturnType.FullName)
					return false;
			}
			return true;
		}
		bool HasOverriden (IMethod method)
		{
			foreach (IMethod m in Methods) {
				if (method.Name == m.Name && IsEqual (method.Parameters, m.Parameters))
					return true;
			}
			return false;
		}
		
		bool HasOverriden (IProperty prop)
		{
			foreach (IProperty p in Properties) {
				if (prop.Name == p.Name)
					return true;
			}
			return false;
		}
		
		public bool HasOverriden (IMember member)
		{
			switch (member.MemberType) {
				case MemberType.Method:
					return HasOverriden (member as IMethod);
				case MemberType.Property:
					return HasOverriden (member as IProperty);
				default:
					return false;
			}
		}
		
		public bool IsBaseType (IReturnType type)
		{
			if (type == null)
				return false;
			if (this.SourceProjectDom == null) {
				throw new NullReferenceException ("SourceProjectDom not set for type :" + this +" - IsBaseType not allowed. StackTrace:" + Environment.StackTrace);
			}
			Stack<IReturnType> typeStack = new Stack<IReturnType> ();
			Dictionary<string, bool> alreadyTaken = new Dictionary<string, bool> ();
			typeStack.Push (new DomReturnType (this));
			string typeFullName = type.FullName;
			while (typeStack.Count > 0) {
				IReturnType curType = typeStack.Pop ();
				IType resolvedType = this.SourceProjectDom.GetType (curType);
				if (resolvedType == null) {
					continue;
				}
				
				string fullName = resolvedType.FullName;
				if (alreadyTaken.ContainsKey (fullName))
					continue;
				alreadyTaken[fullName] = true;
				
				if (fullName == typeFullName) {
					return true;
				}
				foreach (IReturnType baseType in resolvedType.BaseTypes) {
					typeStack.Push (baseType);
				}
			}
			return false;
		}

		public override bool IsAccessibleFrom (MonoDevelop.Projects.Dom.Parser.ProjectDom dom, IType calledType, IMember member, bool includeProtected)
		{
			if (calledType != null) {
				foreach (IType baseType in dom.GetInheritanceTree (calledType)) {
					if (baseType.FullName == calledType.FullName) 
						return true;
				}
			}
			return base.IsAccessibleFrom (dom, calledType, member, includeProtected);
		}

		public static DomType CreateDelegate (ICompilationUnit compilationUnit, string name, DomLocation location, IReturnType type, IEnumerable<IParameter> parameters)
		{
			DomType result = new DomType ();
			result.compilationUnit = compilationUnit;
			result.Name = name;
			result.classType = MonoDevelop.Projects.Dom.ClassType.Delegate;
			DomMethod delegateMethod = new DomMethod ("Invoke", Modifiers.None, MethodModifier.None, location, DomRegion.Empty, type);
			delegateMethod.Add (parameters);
			result.Add (delegateMethod);
			return result;
		}
		
		protected int fieldCount       = 0;
		protected int methodCount      = 0;
		protected int constructorCount = 0;
		protected int indexerCount     = 0;
		protected int propertyCount    = 0;
		protected int eventCount       = 0;
		protected int innerTypeCount   = 0;
		
		public virtual int PropertyCount {
			get {
				return propertyCount;
			}
		}
		public virtual int FieldCount {
			get {
				return fieldCount;
			}
		}
		public virtual int MethodCount {
			get {
				return methodCount;
			}
		}
		public virtual int ConstructorCount {
			get {
				return constructorCount;
			}
		}
		public virtual int IndexerCount {
			get {
				return indexerCount;
			}
		}
		public virtual int EventCount {
			get {
				return eventCount;
			}
		}
		public virtual int InnerTypeCount {
			get {
				return innerTypeCount;
			}
		}
		
		public void Add (IMember member)
		{
			member.DeclaringType = this;
			
			switch (member.MemberType) {
			case MemberType.Field:
				fieldCount++;
				break;
			case MemberType.Method:
				if (((IMethod)member).IsConstructor) {
					constructorCount++;
				} else {
					methodCount++;
				}
				break;
			case MemberType.Property:
				if (((IProperty)member).IsIndexer) {
					indexerCount++;
				} else {
					propertyCount++;
				}
				break;
			case MemberType.Event:
				eventCount++;
				break;
			case MemberType.Type:
				innerTypeCount++;
				break;
			default:
				throw new InvalidOperationException ();
			}
			AddChild (member);
		}
		
		protected void ClearInterfaceImplementations ()
		{
			if (implementedInterfaces != null)
				implementedInterfaces.Clear ();
		}
		public void AddInterfaceImplementation (IReturnType interf)
		{
			if (implementedInterfaces == null)
				implementedInterfaces = new List<IReturnType> ();
			implementedInterfaces.Add (interf);
		}
		public void AddInterfaceImplementations (IEnumerable<IReturnType> interfaces)
		{
			if (interfaces == null)
				return;
			foreach (IReturnType interf in interfaces) {
				AddInterfaceImplementation (interf);
			}
		}
		
		protected void ClearTypeParameter ()
		{
			if (typeParameters != null)
				typeParameters.Clear ();
		}
		
		public void AddTypeParameter (ITypeParameter parameter)
		{
			if (typeParameters == null)
				typeParameters = new List<ITypeParameter> ();
			typeParameters.Add (parameter);
		}
		public void AddTypeParameter (IEnumerable<ITypeParameter> parameters)
		{
			if (parameters == null)
				return;
			foreach (ITypeParameter parameter in parameters) {
				AddTypeParameter (parameter);
			}
		}
		
		public override S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.Visit (this, data);
		}
		
		public static string GetInstantiatedTypeName (string typeName, IList<IReturnType> genericArguments)
		{
			if (genericArguments == null || genericArguments.Count == 0)
				return typeName;
			
			StringBuilder sb = new StringBuilder (typeName);
				
			sb.Append ('[');
			for (int i = 0;  i < genericArguments.Count; i++) {
				if (i > 0) 
					sb.Append (',');
				sb.Append (DomReturnType.ConvertToString (genericArguments [i]));
			}
			sb.Append (']');
			return sb.ToString ();
		}
		
		public static string GetNetFullName (IType type)
		{
			if (type.TypeParameters.Count == 0)
				return type.FullName;
			return type.FullName + "~" + type.TypeParameters.Count;
		}
		
		public static IType CreateInstantiatedGenericTypeInternal (IType type, IList<IReturnType> genericArguments)
		{
			// This method is now internal. The public one has been moved to ProjectDom, which take cares of caching
			// instantiated generic types.
			if (type is InstantiatedType)
				return type;
			string name = GetInstantiatedTypeName (type.Name, genericArguments);
			GenericTypeInstanceResolver resolver = new GenericTypeInstanceResolver ();
			if (genericArguments != null) {
				int j = genericArguments.Count - 1;
				IType curType = type;
				while (curType != null) {
					string fullTypeName = curType.DecoratedFullName;
					for (int i = curType.TypeParameters.Count - 1; i >= 0 && j >= 0; i--, j--) {
						resolver.Add (fullTypeName + "." + curType.TypeParameters[i].Name, genericArguments[j]);
					}
					curType = curType.DeclaringType;
				}
			}
			InstantiatedType result = (InstantiatedType) type.AcceptVisitor (resolver, type);
			if (result.typeParameters != null)
				result.typeParameters.Clear ();
			result.Name = name;
			result.SourceProjectDom = type.SourceProjectDom;
			result.Resolved = (type is DomType) ? ((DomType)type).Resolved : false;
			result.GenericParameters = genericArguments;
			result.UninstantiatedType = type;
			result.DeclaringType = type.DeclaringType;
			CreateInstantiatedSubtypes (result, type, genericArguments);
			return result;
		}

		static void CreateInstantiatedSubtypes (InstantiatedType result, IType curType, IList<IReturnType> genericArguments)
		{
			foreach (IType innerType in curType.InnerTypes) {

				List<IReturnType> newArguments = new List<IReturnType> ();
				List<int> removeInheritedArguments = new List<int> ();
				for (int i = 0; i < innerType.TypeParameters.Count; i++) {
					ITypeParameter curParameter = innerType.TypeParameters[i];
					bool found = false;
					for (int j = curType.TypeParameters.Count - 1; j >= 0 ; j--) {
						if (curType.TypeParameters[j].Name == curParameter.Name) {
							removeInheritedArguments.Add (newArguments.Count);
							newArguments.Add (genericArguments[j]);
							found = true;
							break;
						}
					}
					if (!found)
						newArguments.Add (new DomReturnType (curParameter.Name));
				}

				InstantiatedType innerInstantiatedType = (InstantiatedType)CreateInstantiatedGenericTypeInternal (innerType, newArguments);
				for (int i = 0, j = 0; i < innerInstantiatedType.TypeParameters.Count && j < innerInstantiatedType.TypeParameters.Count; i++,j++) {
					if (curType.TypeParameters[i].Name == innerInstantiatedType.TypeParameters[j].Name) {
						innerInstantiatedType.typeParameters.RemoveAt (j);
						j--;
					}
				}

				result.Add (innerInstantiatedType);
				CreateInstantiatedSubtypes (innerInstantiatedType, innerType, newArguments);
				foreach (int i in removeInheritedArguments) {
					if (i >= 0 && i < newArguments.Count)
						newArguments.RemoveAt (i);
				}
			}
		}
		
		internal class GenericTypeInstanceResolver: CopyDomVisitor<IType>
		{
			public Dictionary<string, IReturnType> typeTable = new Dictionary<string,IReturnType> ();
			
			public void Add (string name, IReturnType type)
			{
				typeTable.Add (name, type);
			}
			
			IType currentType = null;
			public override INode Visit (IType type, IType data)
			{
				if (currentType != null)
					return null;
				currentType = type;
				return base.Visit (type, data);
			}
			
			IReturnType LookupReturnType (string decoratedName, IReturnType type, IType typeToInstantiate) 
			{
				IReturnType res;
				if (typeTable.TryGetValue (decoratedName, out res)) {
					if (type.ArrayDimensions == 0 && type.PointerNestingLevel == 0) {
						return res;
					}
					DomReturnType copy = (DomReturnType)base.Visit (res, typeToInstantiate);
					copy.PointerNestingLevel = type.PointerNestingLevel;
					copy.SetDimensions (type.GetDimensions ());
					return copy;
				}
				return null;
			}
			
			public override INode Visit (IReturnType type, IType typeToInstantiate)
			{
				DomReturnType copyFrom = (DomReturnType) type; 
				string decoratedName = copyFrom.DecoratedFullName;
				IReturnType result = LookupReturnType (decoratedName, type, typeToInstantiate);
				IType curType = currentType;
				while (result == null && curType != null) {
					result = LookupReturnType (curType.DecoratedFullName + "." + decoratedName, type, typeToInstantiate);
					curType = curType.DeclaringType;
				}
				return result ?? base.Visit (type, typeToInstantiate);
			}
			
			protected override DomType CreateInstance (IType type, IType typeToInstantiate)
			{
				if (type == typeToInstantiate)
					return new InstantiatedType ();
				else
					return base.CreateInstance (type, typeToInstantiate);
			}
		}
		
		/// <summary>
		/// Returns a list of types that contains extension methods
		/// </summary>
		public static List<IType> GetAccessibleExtensionTypes (ProjectDom dom, ICompilationUnit unit)
		{
			List<IType> result = new List<IType> ();
			
			if (unit != null) {
				List<string> namespaceList = new List<string> ();
				namespaceList.Add ("");
				if (unit != null && unit.Usings != null) {
					foreach (IUsing u in unit.Usings) {
						if (u.Namespaces == null)
							continue;
						foreach (string ns in u.Namespaces) {
							namespaceList.Add (ns);
						}
					}
				}
				
				foreach (object o in dom.GetNamespaceContents (namespaceList, true, true)) {
					IType type = o as IType;
					if (type != null && type.ClassType == ClassType.Class && type.HasExtensionMethods) {
						result.Add (type);
					}
				}
			} else {
				foreach (IType type in dom.Types) {
					if (type.ClassType == ClassType.Class && type.HasExtensionMethods) {
						result.Add (type);
					}
				}
				foreach (var refDom in dom.References) {
					foreach (IType type in refDom.Types) {
						if (type.ClassType == ClassType.Class && type.HasExtensionMethods) 
							result.Add (type);
					}
				}
			}
			return result;
		}

		public List<IMethod> GetExtensionMethods (List<IType> accessibleExtensionTypes)
		{
			List<IMethod> result = new List<IMethod> ();
			foreach (IType staticType in accessibleExtensionTypes) {
				foreach (IMethod method in staticType.Methods) {
					IMethod extMethod = method.Extends (this.SourceProjectDom, this);
					if (extMethod != null) {
						result.Add (extMethod);
					}
				}
			}
			return result;
		}

		public static bool IncludeProtected (ProjectDom dom, IType type, IType callingType)
		{
			if (type == null || callingType == null)
				return false;
			foreach (IType t in dom.GetInheritanceTree (type)) {
				if (t.FullName == callingType.FullName)
					return true;
			}
			return false;
		}
		
		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals (this, obj))
				return true;
			if (!(obj is IType))
				return false;
			return Equals ((IType)obj);
		}

		public override int GetHashCode ()
		{
			IType a = this is InstantiatedType ? ((InstantiatedType)this).UninstantiatedType : this;
			return a.DecoratedFullName.GetHashCode ();
		}
		
		public static IReturnType GetComponentType (ProjectDom dom, IReturnType returnType)
		{
			if (dom == null || returnType == null)
				return null;
			if (returnType.FullName == DomReturnType.String.FullName && returnType.ArrayDimensions == 0)
				return DomReturnType.Char;
			if (returnType.ArrayDimensions > 0)
				return new DomReturnType (returnType.FullName);
			
			IType resolvedType = dom.GetType (returnType);
			if (resolvedType != null) {
				foreach (IType curType in dom.GetInheritanceTree (resolvedType)) {
					foreach (IReturnType baseType in curType.BaseTypes) {
						if (baseType.FullName == "System.Collections.Generic.IEnumerable" && baseType.GenericArguments.Count == 1)
							return baseType.GenericArguments[0];
					}
					
					foreach (IProperty property in curType.Properties) {
						if (property.IsIndexer && !property.IsExplicitDeclaration)
							return property.ReturnType;
					}
				}
			}

			if (returnType.GenericArguments.Count > 0)
				return returnType.GenericArguments[0];

			return null;
		}
		
		public virtual IMember GetMemberAt (int line, int column)
		{
			IMember result = Members.FirstOrDefault (member => member.BodyRegion.Contains (line, column));
			if (result is IType) {
				IMember containingMember = ((IType)result).GetMemberAt (line, column);
				if (containingMember != null)
					return containingMember;
			}
			if (result == null)
				result = Members.FirstOrDefault (member => member.Location.Line == line);
			return result;
		}
		
		public bool Equals (IType other)
		{
			IType         a = this is InstantiatedType ? ((InstantiatedType)this).UninstantiatedType : this;
			int typeParamsA = this is InstantiatedType ? ((InstantiatedType)this).UninstantiatedType.TypeParameters.Count : a.TypeParameters.Count;
			
			IType         b = other is InstantiatedType ? ((InstantiatedType)other).UninstantiatedType : other;
			int typeParamsB = other is InstantiatedType ? ((InstantiatedType)other).UninstantiatedType.TypeParameters.Count : other.TypeParameters.Count;
			return typeParamsA == typeParamsB && a.FullName == b.FullName;
		}
	}
	
	internal sealed class Stock 
	{
		public static readonly IconId Error = "gtk-dialog-error";
		public static readonly IconId Class = "md-class";
		public static readonly IconId Enum = "md-enum";
		public static readonly IconId Event = "md-event";
		public static readonly IconId Field = "md-field";
		public static readonly IconId Interface = "md-interface";
		public static readonly IconId Method = "md-method";
		public static readonly IconId ExtensionMethod = "md-extensionmethod";
		public static readonly IconId Property = "md-property";
		public static readonly IconId Struct = "md-struct";
		public static readonly IconId Delegate = "md-delegate";
		public static readonly IconId Namespace = "md-name-space";
		
		public static readonly IconId InternalClass = "md-internal-class";
		public static readonly IconId InternalDelegate = "md-internal-delegate";
		public static readonly IconId InternalEnum = "md-internal-enum";
		public static readonly IconId InternalEvent = "md-internal-event";
		public static readonly IconId InternalField = "md-internal-field";
		public static readonly IconId InternalInterface = "md-internal-interface";
		public static readonly IconId InternalMethod = "md-internal-method";
		public static readonly IconId InternalExtensionMethod = "md-internal-extensionmethod";
		public static readonly IconId InternalProperty = "md-internal-property";
		public static readonly IconId InternalStruct = "md-internal-struct";
		
		public static readonly IconId PrivateClass = "md-private-class";
		public static readonly IconId PrivateDelegate = "md-private-delegate";
		public static readonly IconId PrivateEnum = "md-private-enum";
		public static readonly IconId PrivateEvent = "md-private-event";
		public static readonly IconId PrivateField = "md-private-field";
		public static readonly IconId PrivateInterface = "md-private-interface";
		public static readonly IconId PrivateMethod = "md-private-method";
		public static readonly IconId PrivateExtensionMethod = "md-private-extensionmethod";
		public static readonly IconId PrivateProperty = "md-private-property";
		public static readonly IconId PrivateStruct = "md-private-struct";
		
		public static readonly IconId ProtectedClass = "md-protected-class";
		public static readonly IconId ProtectedDelegate = "md-protected-delegate";
		public static readonly IconId ProtectedEnum = "md-protected-enum";
		public static readonly IconId ProtectedEvent = "md-protected-event";
		public static readonly IconId ProtectedField = "md-protected-field";
		public static readonly IconId ProtectedInterface = "md-protected-interface";
		public static readonly IconId ProtectedMethod = "md-protected-method";
		public static readonly IconId ProtectedExtensionMethod = "md-protected-extensionmethod";
		public static readonly IconId ProtectedProperty = "md-protected-property";
		public static readonly IconId ProtectedStruct = "md-protected-struct";
		
	}
}	
