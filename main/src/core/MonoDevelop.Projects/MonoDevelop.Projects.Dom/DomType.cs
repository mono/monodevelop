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
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public class DomType : AbstractMember, IType
	{
		protected ProjectDom sourceProjectDom;
		protected ICompilationUnit compilationUnit;
		protected IReturnType baseType;
		
		List<TypeParameter> typeParameters      = null;
		List<IMember> members                   = new List<IMember> ();
		List<IReturnType> implementedInterfaces = null;
		
		protected ClassType classType = ClassType.Unknown;
		protected string nameSpace;
		
		protected override void CalculateFullName ()
		{
			if (DeclaringType != null) {
				fullName = DeclaringType.FullName + "." + Name;
			} else {
				fullName = !String.IsNullOrEmpty (Namespace) ? Namespace + "." + Name : Name;
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
		
		public string Namespace {
			get {
				return nameSpace;
			}
			set {
				nameSpace = value;
				CalculateFullName ();
			}
		}
		
		public ProjectDom SourceProjectDom {
			get {
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
				return compilationUnit;
			}
			set {
				compilationUnit = value;
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
				if (implementedInterfaces != null) {
					for (int i = 0; i < implementedInterfaces.Count; i++) {
						yield return implementedInterfaces[i];
					}
				}
			}
		}
		
		public virtual ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get {
				return implementedInterfaces != null ? implementedInterfaces.AsReadOnly () : null;
			}
		}
		
		public virtual ReadOnlyCollection<TypeParameter> TypeParameters {
			get {
				return typeParameters != null ? typeParameters.AsReadOnly () : null;
			}
		}
		
		public virtual IEnumerable<IMember> Members {
			get {
				return members;
			}
		}
		
		public virtual IEnumerable<IType> InnerTypes {
			get {
				foreach (IMember item in Members)
					if (item is IType)
						yield return (IType)item;
			}
		}

		public virtual IEnumerable<IField> Fields {
			get {
				foreach (IMember item in Members)
					if (item is IField)
						yield return (IField)item;
			}
		}

		public virtual IEnumerable<IProperty> Properties {
			get {
				foreach (IMember item in Members)
					if (item is IProperty)
						yield return (IProperty)item;
			}
		}

		public virtual IEnumerable<IMethod> Methods {
			get {
				foreach (IMember item in Members)
					if (item is IMethod)
						yield return (IMethod)item;
			}
		}

		public virtual IEnumerable<IEvent> Events {
			get {
				foreach (IMember item in Members)
					if (item is IEvent)
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
		
		
		static string[,] iconTable = new string[,] {
			{Stock.Error,     Stock.Error,            Stock.Error,              Stock.Error},             // unknown
			{Stock.Class,     Stock.PrivateClass,     Stock.ProtectedClass,     Stock.InternalClass},     // class
			{Stock.Enum,      Stock.PrivateEnum,      Stock.ProtectedEnum,      Stock.InternalEnum},      // enum
			{Stock.Interface, Stock.PrivateInterface, Stock.ProtectedInterface, Stock.InternalInterface}, // interface
			{Stock.Struct,    Stock.PrivateStruct,    Stock.ProtectedStruct,    Stock.InternalStruct},    // struct
			{Stock.Delegate,  Stock.PrivateDelegate,  Stock.ProtectedDelegate,  Stock.InternalDelegate}   // delegate
		};
		
		public override string HelpUrl {
			get {
				return "T:" + this.FullName;
			}
		}
		
		public override string StockIcon {
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
			this.classType   = classType;
			this.Name        = name;
			this.Namespace   = namesp;
			this.bodyRegion  = region;
			this.members     = members;
			this.location    = location;
			
			foreach (IMember member in members) {
				member.DeclaringType = this;
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
			this.modifiers   = modifiers;
			this.Name        = name;
			this.Namespace   = namesp;
			this.bodyRegion  = region;
			this.members     = members;
			this.location    = location;
		}
		
		public override System.Xml.XmlNode GetMonodocDocumentation ()
		{
			System.Xml.XmlDocument doc = ProjectDomService.HelpTree.GetHelpXml (this.HelpUrl);
			if (doc != null)
				return doc.SelectSingleNode ("/Type/Docs");
			return null;
		}
		
		public List<IMember> SearchMember (string name, bool caseSensitive)
		{
			List<IMember> result = new List<IMember> ();
			foreach (IMember member in this.Members) {
				if (0 == String.Compare (name, member.Name, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase)) {
					result.Add (member);
				}
			}
			return result;
		}
		
		public override string ToString ()
		{
			return String.Format ("[DomType: FullName={0}, #Members={1}]", this.FullName, this.members.Count);
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
			if (member is IMethod)
				return HasOverriden (member as IMethod);
			if (member is IProperty)
				return HasOverriden (member as IProperty);
			return false;
		}
		
		public bool IsBaseType (IReturnType type)
		{
			if (type == null)
				return false;
			if (FullName == type.FullName)
				return true;
			foreach (IReturnType baseType in BaseTypes) {
				if (baseType.FullName == type.FullName)
					return true;
			}
			SearchTypeRequest request = new SearchTypeRequest (this.CompilationUnit, -1, -1, null);
			foreach (IReturnType baseType in BaseTypes) {
				request.Name = baseType.FullName;
				SearchTypeResult searchTypeResult = this.SourceProjectDom.SearchType (request);
				if (searchTypeResult == null)
					continue;
				IReturnType resolvedType = searchTypeResult.Result ?? baseType;
				IType resolvedBaseType = this.SourceProjectDom.GetType (resolvedType);
				if (resolvedBaseType != null && resolvedBaseType.IsBaseType (type))
					return true;
			}
			return false;
		}
		
		public static DomType CreateDelegate (ICompilationUnit compilationUnit, string name, DomLocation location, IReturnType type, List<IParameter> parameters)
		{
			DomType result = new DomType ();
			result.compilationUnit = compilationUnit;
			result.Name = name;
			result.classType = MonoDevelop.Projects.Dom.ClassType.Delegate;
			result.members.Add (new DomMethod ("Invoke", Modifiers.None, false, location, DomRegion.Empty, type, parameters));
			return result;
		}
		
		protected int fieldCount       = 0;
		protected int methodCount      = 0;
		protected int constructorCount = 0;
		protected int indexerCount     = 0;
		protected int propertyCount    = 0;
		protected int eventCount       = 0;
		protected int innerTypeCount   = 0;
		
		public int PropertyCount {
			get {
				return propertyCount;
			}
		}
		public int FieldCount {
			get {
				return fieldCount;
			}
		}
		public int MethodCount {
			get {
				return methodCount;
			}
		}
		public int ConstructorCount {
			get {
				return constructorCount;
			}
		}
		public int IndexerCount {
			get {
				return indexerCount;
			}
		}
		public int EventCount {
			get {
				return eventCount;
			}
		}
		public int InnerTypeCount {
			get {
				return innerTypeCount;
			}
		}
		
		public void Add (IField member)
		{
			fieldCount++;
			this.members.Add (member);
		}
		public void Add (IMethod member)
		{
			if (member.IsConstructor) {
				constructorCount++;
			} else {
				methodCount++;
			}
			this.members.Add (member);
		}
		public void Add (IProperty member)
		{
			if (member.IsIndexer) {
				indexerCount++;
			} else {
				propertyCount++;
			}
			this.members.Add (member);
		}
		public void Add (IEvent member)
		{
			eventCount++;
			this.members.Add (member);
		}
		public void Add (IType member)
		{
			innerTypeCount++;
			this.members.Add (member);
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
		
		public void AddTypeParameter (TypeParameter parameter)
		{
			if (typeParameters == null)
				typeParameters = new List<TypeParameter> ();
			typeParameters.Add (parameter);
		}
		public void AddTypeParameter (IEnumerable<TypeParameter> parameters)
		{
			if (parameters == null)
				return;
			foreach (TypeParameter parameter in parameters) {
				AddTypeParameter (parameter);
			}
		}
		
		public override object AcceptVisitior (IDomVisitor visitor, object data)
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
		
		
		public static IType CreateInstantiatedGenericType (IType type, IList<IReturnType> genericArguments)
		{
			string name = GetInstantiatedTypeName (type.Name, genericArguments);
			GenericTypeInstanceResolver resolver = new GenericTypeInstanceResolver ();
			for (int i = 0; i < type.TypeParameters.Count; i++)
				resolver.Add (type.TypeParameters[i].Name, genericArguments[i]);
			
			DomType result = (DomType)Resolve (type, resolver);
			result.Name = name;
			result.typeParameters.Clear ();
			return result;
		}
		
		class GenericTypeInstanceResolver: ITypeResolver
		{
			public Dictionary<string, IReturnType> typeTable = new Dictionary<string,IReturnType> ();
			
			public void Add (string name, IReturnType type)
			{
				typeTable.Add (name, type);
			}
			
			public IReturnType Resolve (IReturnType type)
			{
				DomReturnType result = null;
				if (typeTable.ContainsKey (type.FullName)) {
					if (type.GenericArguments == null || type.GenericArguments.Count == 0)
						return typeTable [type.FullName];
					
					result = new DomReturnType ();
					
					IReturnType retType = typeTable [type.FullName];
					result.Name      = retType.Name;
					result.Namespace = retType.Namespace;
					result.Type      = retType.Type;
					result.ArrayDimensions = retType.ArrayDimensions;
					result.PointerNestingLevel = retType.PointerNestingLevel;
					result.IsNullable  = retType.IsNullable;
					foreach (IReturnType param in retType.GenericArguments) {
						result.AddTypeParameter (Resolve (param));
					}
				} else {
					result = new DomReturnType ();
					result.Name      = type.Name;
					result.Namespace = type.Namespace;
					result.ArrayDimensions = type.ArrayDimensions;
					result.Type       = type.Type;
					result.PointerNestingLevel = type.PointerNestingLevel;
					result.IsNullable = type.IsNullable;
					foreach (IReturnType param in type.GenericArguments) {
						result.AddTypeParameter (Resolve (param));
					}
				}
				return result;
			}
		}
		
		
		public static IType Resolve (IType type, ITypeResolver typeResolver)
		{
			DomType result = new DomType ();
			result.CompilationUnit = type.CompilationUnit;
			result.Name          = type.Name;
			result.Namespace     = type.Namespace;
			result.Documentation = type.Documentation;
			result.ClassType     = type.ClassType;
			result.Modifiers     = type.Modifiers;
			
			result.Location      = type.Location;
			result.bodyRegion    = type.BodyRegion;
			result.AddRange (DomAttribute.Resolve (type.Attributes, typeResolver));
			
			if (type.BaseType != null)
				result.baseType = DomReturnType.Resolve (type.BaseType, typeResolver);
			
			if (type.ImplementedInterfaces != null) {
				foreach (IReturnType iface in type.ImplementedInterfaces) {
					result.AddInterfaceImplementation (DomReturnType.Resolve (iface, typeResolver));
				}
			}
			
			foreach (IType innerType in type.InnerTypes) {
				result.Add (Resolve (innerType, typeResolver));
			}
			foreach (IField field in type.Fields) {
				result.Add (DomField.Resolve (field, typeResolver));
			}
			foreach (IProperty property in type.Properties) {
				result.Add (DomProperty.Resolve (property, typeResolver));
			}
			foreach (IMethod method in type.Methods) {
				result.Add (DomMethod.Resolve (method, typeResolver));
			}
			foreach (IEvent evt in type.Events) {
				result.Add (DomEvent.Resolve (evt, typeResolver));
			}
			
			return result;
		}
	}
	
	internal sealed class Stock 
	{
		public static readonly string Error = "gtk-error";
		public static readonly string Class = "md-class";
		public static readonly string Enum = "md-enum";
		public static readonly string Event = "md-event";
		public static readonly string Field = "md-field";
		public static readonly string Interface = "md-interface";
		public static readonly string Method = "md-method";
		public static readonly string Property = "md-property";
		public static readonly string Struct = "md-struct";
		public static readonly string Delegate = "md-delegate";
		public static readonly string Namespace = "md-name-space";
		
		public static readonly string InternalClass = "md-internal-class";
		public static readonly string InternalDelegate = "md-internal-delegate";
		public static readonly string InternalEnum = "md-internal-enum";
		public static readonly string InternalEvent = "md-internal-event";
		public static readonly string InternalField = "md-internal-field";
		public static readonly string InternalInterface = "md-internal-interface";
		public static readonly string InternalMethod = "md-internal-method";
		public static readonly string InternalProperty = "md-internal-property";
		public static readonly string InternalStruct = "md-internal-struct";
		
		public static readonly string PrivateClass = "md-private-class";
		public static readonly string PrivateDelegate = "md-private-delegate";
		public static readonly string PrivateEnum = "md-private-enum";
		public static readonly string PrivateEvent = "md-private-event";
		public static readonly string PrivateField = "md-private-field";
		public static readonly string PrivateInterface = "md-private-interface";
		public static readonly string PrivateMethod = "md-private-method";
		public static readonly string PrivateProperty = "md-private-property";
		public static readonly string PrivateStruct = "md-private-struct";
		
		public static readonly string ProtectedClass = "md-protected-class";
		public static readonly string ProtectedDelegate = "md-protected-delegate";
		public static readonly string ProtectedEnum = "md-protected-enum";
		public static readonly string ProtectedEvent = "md-protected-event";
		public static readonly string ProtectedField = "md-protected-field";
		public static readonly string ProtectedInterface = "md-protected-interface";
		public static readonly string ProtectedMethod = "md-protected-method";
		public static readonly string ProtectedProperty = "md-protected-property";
		public static readonly string ProtectedStruct = "md-protected-struct";
		
	}
}	
