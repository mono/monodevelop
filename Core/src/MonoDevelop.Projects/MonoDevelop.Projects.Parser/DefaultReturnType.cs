// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultReturnType : IReturnType
	{
		protected int    pointerNestingLevel;
		protected int[]  arrayDimensions;
		protected object declaredin = null;
		protected ReturnTypeList genericArguments;
		protected bool   byRef;
		string fname;
		
		static readonly int[] zeroDimensions = new int[0];
		static readonly int[] oneDimensions = new int[] { 1 };
		
		public DefaultReturnType ()
		{
		}
		
		public DefaultReturnType (string fullyQualifiedName)
		{
			FullyQualifiedName = fullyQualifiedName;
		}
		
		public DefaultReturnType (string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel, ReturnTypeList genericArguments)
		{
			this.FullyQualifiedName  = fullyQualifiedName;
			this.pointerNestingLevel = pointerNestingLevel;
			this.genericArguments    = genericArguments;
			ArrayDimensions = arrayDimensions;
		}
		
		public virtual string FullyQualifiedName {
			get {
				return fname;
			}
			set {
				if (value == null)
					fname = value;
				else {
					fname = AbstractNamedEntity.GetSharedString (value);
				}
			}
		}

		public virtual string Name {
			get {
				if (FullyQualifiedName == null) {
					return null;
				}
				string[] name = FullyQualifiedName.Split(new char[] {'.'});
				return name[name.Length - 1];
			}
		}

		public virtual string Namespace {
			get {
				if (FullyQualifiedName == null) {
					return null;
				}
				int index = FullyQualifiedName.LastIndexOf('.');
				return index < 0 ? String.Empty : FullyQualifiedName.Substring(0, index);
			}
		}

		public virtual bool IsRootType {
			get { return FullyQualifiedName == "System.Object"; }
		}

/*
		string name;
		string ns;
		public virtual string FullyQualifiedName {
			get {
				if (ns == null || ns.Length == 0)
					return name;
				else if (name != null)
					return string.Concat (ns, ".", name);
				else
					return null;
			}
			set {
				if (value == null) {
					ns = null;
					name = null;
					return;
				}
				int i = value.LastIndexOf ('.');
				if (i == -1) {
					ns = null;
					name = value;
				} else {
					ns = value.Substring (0, i);
					name = value.Substring (i+1);
				}
			}
		}

		public virtual string Name {
			get { return name; }
			set { name = value; }
		}

		public virtual string Namespace {
			get { return ns ?? string.Empty; }
			set { ns = value; }
		}
*/

		public virtual int PointerNestingLevel {
			get { return pointerNestingLevel; }
			set { pointerNestingLevel = value; }
		}

		public int ArrayCount {
			get {
				return ArrayDimensions.Length;
			}
		}

		public virtual int[] ArrayDimensions {
			get {
				if (arrayDimensions == null)
					return zeroDimensions;
				return arrayDimensions;
			}
			set {
				if (value == null || value.Length == 0)
					arrayDimensions = zeroDimensions;
				else if (value != null && value.Length == 1 && value[0] == 1)
					arrayDimensions = oneDimensions;
				else
					arrayDimensions = value;
			}
		}
		 		
		/// <summary>
		/// Indicates whether the return type is passed by reference.
		/// </summary>
		public virtual bool ByRef {
			get { return byRef; }
			set { byRef = value; }
		}
		 		
		/// <summary>
		/// Contains values (types) of actual parameters (arguments) to a
		/// generic type.
		/// </summary>
		public virtual ReturnTypeList GenericArguments {
			get {
				return genericArguments;
			}
			set {
				genericArguments = value;
			}
		}

		public virtual int CompareTo (object ob) 
		{
			int cmp;
			IReturnType value = (IReturnType) ob;
			
			if (FullyQualifiedName != null) {
				if (value.FullyQualifiedName == null)
					return -1;
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.FullyQualifiedName != null)
				return 1;
			
			cmp = (PointerNestingLevel - value.PointerNestingLevel);
			if (cmp != 0) {
				return cmp;
			}
			
			if (ArrayDimensions != value.ArrayDimensions) {
				cmp = DiffUtility.Compare(ArrayDimensions, value.ArrayDimensions);
				if (cmp != 0)
					return cmp;
			}
			
			if (GenericArguments == value.GenericArguments)
				return 0;
			else
				return DiffUtility.Compare(GenericArguments, value.GenericArguments);
		}
		
		int IComparable.CompareTo(object value)
		{
			return CompareTo((IReturnType)value);
		}
		
		public override bool Equals (object ob)
		{
			IReturnType other = ob as IReturnType;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = PointerNestingLevel;
			if (ArrayDimensions != null)
				for (int n=0; n<ArrayDimensions.Length; n++)
					c += arrayDimensions [n];
			if (FullyQualifiedName != null)
				c += FullyQualifiedName.GetHashCode ();
			return c;
		}
		
		public virtual object DeclaredIn {
			get {
				return declaredin;
			}
		}
		
		public override string ToString ()
		{
			return ToString (this);
		}
		
		public static string ToString (IReturnType type)
		{
			StringBuilder sb = new StringBuilder (DefaultClass.GetInstantiatedTypeName (type.FullyQualifiedName, type.GenericArguments));
			
			if (type.PointerNestingLevel > 0)
				sb.Append (new string ('*', type.PointerNestingLevel));

			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				foreach (int dim in type.ArrayDimensions) {
					sb.Append ('[').Append (new string (',', dim - 1)).Append (']');
				}
			}
			
			return sb.ToString ();
		}
		
		// Checks if subType is assignable to baseType. Returns -1 if it is not, 0 for an exact match,
		// or a number > 0 which is the distance in the inheritance hierarchy between both types.
		public static int IsTypeAssignable (IParserContext ctx, IReturnType baseType, IReturnType subType)
		{
			if (baseType.FullyQualifiedName == "System.Object" && baseType.ArrayCount == 0)
				return 100;
			
			if (DiffUtility.Compare (baseType.ArrayDimensions, subType.ArrayDimensions) != 0)
				return -1;
			
			if (baseType.GenericArguments != subType.GenericArguments &&
				DiffUtility.Compare (baseType.GenericArguments, subType.GenericArguments) != 0)
				return -1;
			
			if (baseType.ByRef != subType.ByRef)
				return -1;
				
			if (baseType.PointerNestingLevel != subType.PointerNestingLevel)
				return -1;
				
			IClass baseClass = ctx.GetClass (baseType.FullyQualifiedName, baseType.GenericArguments, true, true);
			if (baseClass == null)
				return -1;

			return FindSuperClass (ctx, baseClass, subType, 0);
		}
		
		static int FindSuperClass (IParserContext ctx, IClass baseClass, IReturnType type, int currentLevel)
		{
			IClass subClass = ctx.GetClass (type.FullyQualifiedName, type.GenericArguments, true, true);
			if (subClass == null)
				return -1;
				
			// Is this the class we are looking for?
			if (subClass.FullyQualifiedName == baseClass.FullyQualifiedName)
				return currentLevel;
			
			// Check super classes, and store the best level
			int bestLevel = -1;
			foreach (IReturnType bt in subClass.BaseTypes) {
				int lev = FindSuperClass (ctx, baseClass, bt, currentLevel + 1);
				if (lev != -1 && (bestLevel == -1 || lev < bestLevel))
					bestLevel = lev;
			}
			return bestLevel;
		}
		
		
		static Dictionary<string, IReturnType> commonTypes;
		
		internal static IReturnType GetSharedType (IReturnType type)
		{
			if (type.ArrayCount > 0 || type.ByRef || (type.GenericArguments != null && type.GenericArguments.Count > 0) || type.PointerNestingLevel > 0)
				return type;

			if (commonTypes == null)
				InitializeCommonTypes ();
			
			IReturnType res;
			if (commonTypes.TryGetValue (type.FullyQualifiedName, out res))
				return res;
			else
				return type;
		}
		
		static void InitializeCommonTypes ()
		{
			commonTypes = new Dictionary <string,IReturnType> ();
			
			AddCommonType ("System.Void");
			AddCommonType ("System.Object");
			AddCommonType ("System.Boolean");
			AddCommonType ("System.Byte");
			AddCommonType ("System.SByte");
			AddCommonType ("System.Char");
			AddCommonType ("System.Enum");
			AddCommonType ("System.Int16");
			AddCommonType ("System.Int32");
			AddCommonType ("System.Int64");
			AddCommonType ("System.UInt16");
			AddCommonType ("System.UInt32");
			AddCommonType ("System.UInt64");
			AddCommonType ("System.Single");
			AddCommonType ("System.Double");
			AddCommonType ("System.Decimal");
			AddCommonType ("System.String");
			AddCommonType ("System.DateTime");
			AddCommonType ("System.IntPtr");
			AddCommonType ("System.Enum");
			AddCommonType ("System.Type");
			AddCommonType ("System.IO.Stream");
			AddCommonType ("System.EventArgs");
		}
		
		static void AddCommonType (string type)
		{
			commonTypes [type] = new DefaultReturnType (type);
		}
	}
	
}
