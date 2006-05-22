// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1389 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ICSharpCode.NRefactory.Parser.AST
{
	public class TypeReference : AbstractNode, INullable, ICloneable
	{
		string type = "";
		string systemType = "";
		int    pointerNestingLevel = 0;
		int[]  rankSpecifier = null;
		List<TypeReference> genericTypes = new List<TypeReference>();
		bool isGlobal = false;
		
		static Dictionary<string, string> types   = new Dictionary<string, string>();
		static Dictionary<string, string> vbtypes = new Dictionary<string, string>();
		
		static TypeReference()
		{
			// C# types
			types.Add("bool",    "System.Boolean");
			types.Add("byte",    "System.Byte");
			types.Add("char",    "System.Char");
			types.Add("decimal", "System.Decimal");
			types.Add("double",  "System.Double");
			types.Add("float",   "System.Single");
			types.Add("int",     "System.Int32");
			types.Add("long",    "System.Int64");
			types.Add("object",  "System.Object");
			types.Add("sbyte",   "System.SByte");
			types.Add("short",   "System.Int16");
			types.Add("string",  "System.String");
			types.Add("uint",    "System.UInt32");
			types.Add("ulong",   "System.UInt64");
			types.Add("ushort",  "System.UInt16");
			types.Add("void",    "System.Void");
			
			// VB.NET types
			vbtypes.Add("boolean", "System.Boolean");
			vbtypes.Add("byte",    "System.Byte");
			vbtypes.Add("sbyte",   "System.SByte");
			vbtypes.Add("date",	   "System.DateTime");
			vbtypes.Add("char",    "System.Char");
			vbtypes.Add("decimal", "System.Decimal");
			vbtypes.Add("double",  "System.Double");
			vbtypes.Add("single",  "System.Single");
			vbtypes.Add("integer", "System.Int32");
			vbtypes.Add("long",    "System.Int64");
			vbtypes.Add("uinteger","System.UInt32");
			vbtypes.Add("ulong",   "System.UInt64");
			vbtypes.Add("object",  "System.Object");
			vbtypes.Add("short",   "System.Int16");
			vbtypes.Add("ushort",  "System.UInt16");
			vbtypes.Add("string",  "System.String");
		}
		
		public static IEnumerable<KeyValuePair<string, string>> GetPrimitiveTypesCSharp()
		{
			return types;
		}
		
		public static IEnumerable<KeyValuePair<string, string>> GetPrimitiveTypesVB()
		{
			return vbtypes;
		}
		
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		
		public virtual TypeReference Clone()
		{
			TypeReference c = new TypeReference(type, systemType);
			CopyFields(this, c);
			return c;
		}
		
		/// <summary>
		/// Copies the pointerNestingLevel, RankSpecifier, GenericTypes and IsGlobal flag
		/// from <paramref name="from"/> to <paramref name="to"/>.
		/// </summary>
		/// <remarks>
		/// If <paramref name="to"/> already contains generics, the new generics are appended to the list.
		/// </remarks>
		protected static void CopyFields(TypeReference from, TypeReference to)
		{
			to.pointerNestingLevel = from.pointerNestingLevel;
			if (from.rankSpecifier != null) {
				to.rankSpecifier = (int[])from.rankSpecifier.Clone();
			}
			foreach (TypeReference r in from.genericTypes) {
				to.genericTypes.Add(r.Clone());
			}
			to.isGlobal = from.isGlobal;
		}
		
		public string Type {
			get {
				return type;
			}
			set {
				Debug.Assert(value != null);
				type = value;
				systemType = GetSystemType(type);
			}
		}
		
		/// <summary>
		/// Removes the last identifier from the type.
		/// e.g. "System.String.Length" becomes "System.String" or
		/// "System.Collections.IEnumerable(of string).Current" becomes "System.Collections.IEnumerable(of string)"
		/// This is used for explicit interface implementation in VB.
		/// </summary>
		public static string StripLastIdentifierFromType(ref TypeReference tr)
		{
			if (tr is InnerClassTypeReference && ((InnerClassTypeReference)tr).Type.IndexOf('.') < 0) {
				string ident = ((InnerClassTypeReference)tr).Type;
				tr = ((InnerClassTypeReference)tr).BaseType;
				return ident;
			} else {
				int pos = tr.Type.LastIndexOf('.');
				if (pos < 0)
					return tr.Type;
				string ident = tr.Type.Substring(pos + 1);
				tr.Type = tr.Type.Substring(0, pos);
				return ident;
			}
		}
		
		public string SystemType {
			get {
				return systemType;
			}
		}
		
		public int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
			set {
				pointerNestingLevel = value;
			}
		}
		
		/// <summary>
		/// The rank of the array type.
		/// For "object[]", this is { 0 }; for "object[,]", it is {1}.
		/// For "object[,][,,][]", it is {1, 2, 0}.
		/// For non-array types, this property is null or {}.
		/// </summary>
		public int[] RankSpecifier {
			get {
				return rankSpecifier;
			}
			set {
				rankSpecifier = value;
			}
		}
		
		public List<TypeReference> GenericTypes {
			get {
				return genericTypes;
			}
		}
		
		public bool IsArrayType {
			get {
				return rankSpecifier != null && rankSpecifier.Length > 0;
			}
		}
		
		public static TypeReference CheckNull(TypeReference typeReference)
		{
			return typeReference ?? NullTypeReference.Instance;
		}
		
		public static NullTypeReference Null {
			get {
				return NullTypeReference.Instance;
			}
		}
		
		public virtual bool IsNull {
			get {
				return false;
			}
		}
		
		/// <summary>
		/// Gets/Sets if the type reference had a "global::" prefix.
		/// </summary>
		public bool IsGlobal {
			get {
				return isGlobal;
			}
			set {
				isGlobal = value;
			}
		}
		
		string GetSystemType(string type)
		{
			if (types.ContainsKey(type)) {
				return types[type];
			}
			string lowerType = type.ToLower(CultureInfo.InvariantCulture);
			if (vbtypes.ContainsKey(lowerType)) {
				return vbtypes[lowerType];
			}
			return type;
		}
		
		public TypeReference(string type)
		{
			this.Type = type;
		}
		
		public TypeReference(string type, string systemType)
		{
			this.type       = type;
			this.systemType = systemType;
		}
		
		public TypeReference(string type, List<TypeReference> genericTypes) : this(type)
		{
			if (genericTypes != null) {
				this.genericTypes = genericTypes;
			}
		}
		
		public TypeReference(string type, int[] rankSpecifier) : this(type, 0, rankSpecifier)
		{
		}
		
		public TypeReference(string type, int pointerNestingLevel, int[] rankSpecifier) : this(type, pointerNestingLevel, rankSpecifier, null)
		{
		}
		
		public TypeReference(string type, int pointerNestingLevel, int[] rankSpecifier, List<TypeReference> genericTypes)
		{
			Debug.Assert(type != null);
			this.type = type;
			this.systemType = GetSystemType(type);
			this.pointerNestingLevel = pointerNestingLevel;
			this.rankSpecifier = rankSpecifier;
			if (genericTypes != null) {
				this.genericTypes = genericTypes;
			}
		}
		
		protected TypeReference()
		{}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder(type);
			if (genericTypes != null && genericTypes.Count > 0) {
				b.Append('<');
				for (int i = 0; i < genericTypes.Count; i++) {
					if (i > 0) b.Append(',');
					b.Append(genericTypes[i].ToString());
				}
				b.Append('>');
			}
			if (pointerNestingLevel > 0) {
				b.Append('*', pointerNestingLevel);
			}
			if (IsArrayType) {
				foreach (int rank in rankSpecifier) {
					b.Append('[');
					if (rank < 0)
						b.Append('`', -rank);
					else
						b.Append(',', rank);
					b.Append(']');
				}
			}
			return b.ToString();
		}
	}
	
	public class NullTypeReference : TypeReference
	{
		static NullTypeReference nullTypeReference = new NullTypeReference();
		public override bool IsNull {
			get {
				return true;
			}
		}
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return data;
		}
		public static NullTypeReference Instance {
			get {
				return nullTypeReference;
			}
		}
		NullTypeReference() {}
		public override string ToString()
		{
			return String.Format("[NullTypeReference]");
		}
	}
	
	/// <summary>
	/// We need this special type reference for cases like
	/// OuterClass(Of T1).InnerClass(Of T2) (in expression or type context)
	/// or Dictionary(Of String, NamespaceStruct).KeyCollection (in type context, otherwise it's a
	/// MemberReferenceExpression)
	/// </summary>
	public class InnerClassTypeReference: TypeReference
	{
		TypeReference baseType;
		
		public TypeReference BaseType {
			get {
				return baseType;
			}
		}
		
		public override TypeReference Clone()
		{
			InnerClassTypeReference c = new InnerClassTypeReference(baseType.Clone(), Type, GenericTypes);
			CopyFields(this, c);
			return c;
		}
		
		public InnerClassTypeReference(TypeReference outerClass, string innerType, List<TypeReference> innerGenericTypes)
			: base(innerType, innerGenericTypes)
		{
			this.baseType = outerClass;
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		/// <summary>
		/// Creates a type reference where all type parameters are specified for the innermost class.
		/// Namespace.OuterClass(of string).InnerClass(of integer).InnerInnerClass
		/// becomes Namespace.OuterClass.InnerClass.InnerInnerClass(of string, integer)
		/// </summary>
		public TypeReference CombineToNormalTypeReference()
		{
			TypeReference tr = (baseType is InnerClassTypeReference)
				? ((InnerClassTypeReference)baseType).CombineToNormalTypeReference()
				: baseType.Clone();
			CopyFields(this, tr);
			tr.Type += "." + Type;
			return tr;
		}
		
		public override string ToString()
		{
			return "[InnerClassTypeReference: (" + baseType.ToString() + ")." + base.ToString() + "]";
		}
	}
}
