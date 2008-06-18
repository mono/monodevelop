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

namespace MonoDevelop.Projects.Dom
{
	public class DomType : AbstractMember, IType
	{
		protected object sourceProject;
		protected ICompilationUnit compilationUnit;
		protected IReturnType baseType;
		protected List<TypeParameter> typeParameters = new List<TypeParameter> ();
		protected List<IMember> members = new List<IMember> ();
		protected List<IType> parts = new List<IType> ();
		protected List<IReturnType> implementedInterfaces = new List<IReturnType> ();
		protected ClassType classType = ClassType.Unknown;
		protected string namesp;
		
		public override string FullName {
			get {
				return !String.IsNullOrEmpty (namesp) ? namesp + "." + name : name;
			}
		}
		
		public string Namespace {
			get {
				return namesp;
			}
			set {
				namesp = value;
			}
		}
		
		public object SourceProject {
			get {
				return sourceProject;
			}
			set {
				sourceProject = value;
			}
		}

		public ICompilationUnit CompilationUnit {
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
		
		public virtual ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get {
				return implementedInterfaces.AsReadOnly ();
			}
		}
		
		public virtual ReadOnlyCollection<TypeParameter> TypeParameters {
			get {
				return typeParameters.AsReadOnly ();
			}
		}
		
		public virtual IEnumerable<IMember> Members {
			get {
				return members;
			}
		}
		
		public IEnumerable<IType> InnerTypes {
			get {
				foreach (IMember item in Members)
					if (item is IType)
						yield return (IType)item;
			}
		}

		public IEnumerable<IField> Fields {
			get {
				foreach (IMember item in Members)
					if (item is IField)
						yield return (IField)item;
			}
		}

		public IEnumerable<IProperty> Properties {
			get {
				foreach (IMember item in Members)
					if (item is IProperty)
						yield return (IProperty)item;
			}
		}

		public IEnumerable<IMethod> Methods {
			get {
				foreach (IMember item in Members)
					if (item is IMethod)
						yield return (IMethod)item;
			}
		}

		public IEnumerable<IEvent> Events {
			get {
				foreach (IMember item in Members)
					if (item is IEvent)
						yield return (IEvent)item;
			}
		}
		
		public virtual IEnumerable<IType> Parts { 
			get {
				return parts;
			}
		}
		public bool HasParts {
			get {
				return parts != null && parts.Count > 0;
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
		
		public override string StockIcon {
			get {
				return iconTable[(int)ClassType, ModifierToOffset (Modifiers)];
			}
		}
		
		public DomType ()
		{
		}
		
		public DomType (ICompilationUnit compilationUnit, ClassType classType, string name, DomLocation location, string namesp, DomRegion region, List<IMember> members)
		{
			this.compilationUnit = compilationUnit;
			this.classType   = classType;
			this.name        = name;
			this.namesp      = namesp;
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
			this.name        = name;
			this.namesp      = namesp;
			this.bodyRegion  = region;
			this.members     = members;
			this.location    = location;
		}
		
		public static DomType CreateDelegate (ICompilationUnit compilationUnit, string name, DomLocation location, IReturnType type, List<IParameter> parameters)
		{
			DomType result = new DomType ();
			result.compilationUnit = compilationUnit;
			result.name = name;
			result.classType = MonoDevelop.Projects.Dom.ClassType.Delegate;
			result.members.Add (new DomMethod ("Invoke", Modifiers.None, false, location, DomRegion.Empty, type, parameters));
			return result;
		}
		
		public void Add (IReturnType interf)
		{
			this.implementedInterfaces.Add (interf);
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
		
		public void AddInterfaceImplementation (IReturnType interf)
		{
			implementedInterfaces.Add (interf);
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
					result.PointerNestingLevel = type.PointerNestingLevel;
					result.IsNullable = type.IsNullable;
					foreach (IReturnType param in type.GenericArguments) {
						result.AddTypeParameter (param);
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
			result.attributes    = DomAttribute.Resolve (type.Attributes, typeResolver);
						
			if (type.BaseType != null)
				result.baseType      = DomReturnType.Resolve (type.BaseType, typeResolver);
			foreach (IReturnType iface in type.ImplementedInterfaces) {
				result.implementedInterfaces.Add (DomReturnType.Resolve (iface, typeResolver));
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
