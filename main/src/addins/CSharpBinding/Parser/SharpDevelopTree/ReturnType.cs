
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;

namespace CSharpBinding.Parser.SharpDevelopTree
{/*
	public class ReturnType : DefaultReturnType
	{
		static Dictionary<string, string> types = new Dictionary<string, string>();
		
		static ReturnType ()
		{
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
		}
		
		public ReturnType (string fullyQualifiedName): base (fullyQualifiedName)
		{
		}
		
		public ReturnType(string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel, ReturnTypeList genericArguments, bool fixDimensions)
		: base (fullyQualifiedName, arrayDimensions, pointerNestingLevel, genericArguments)
		{
			if (fixDimensions)
				SetArrayDimensions (arrayDimensions);
		}
		
		public ReturnType (ICSharpCode.NRefactory.Ast.TypeReference type): this (type, null)
		{
		}
		
		public ReturnType (ICSharpCode.NRefactory.Ast.TypeReference type, IType resolvedClass)
		{
			this.FullyQualifiedName  = resolvedClass != null ? resolvedClass.FullyQualifiedName : GetSystemType (type.Type);
			this.pointerNestingLevel = type.PointerNestingLevel;
			SetArrayDimensions (type.RankSpecifier);
			
			// Now get generic arguments
			if (type.GenericTypes != null && type.GenericTypes.Count > 0) {
				this.genericArguments = new ReturnTypeList ();
				
				// Decorate the name
				if (resolvedClass == null)
					this.FullyQualifiedName = string.Concat (this.FullyQualifiedName, "`", type.GenericTypes.Count);

				// Now go get them!
				foreach (ICSharpCode.NRefactory.Ast.TypeReference tr in type.GenericTypes) {
					this.genericArguments.Add (new ReturnType(tr));
				}
			}
		}
		
		public static string GetFullTypeName (ICSharpCode.NRefactory.Ast.TypeReference type)
		{
			if (type.GenericTypes != null && type.GenericTypes.Count > 0)
				return string.Concat (GetSystemType (type.Type), "`", type.GenericTypes.Count);
			else
				return GetSystemType (type.Type);
		}
		
		void SetArrayDimensions (int[] dimensions)
		{
			// The parser returns the number of dimensions - 1.
			// So, for the array int[,] it would return 1. It has to be fixed.
			
			if (dimensions != null && dimensions.Length > 0) {
				this.arrayDimensions = new int [dimensions.Length];
				for (int n=0; n<dimensions.Length; n++)
					arrayDimensions [n] = dimensions [n] + 1;
			} else
				this.arrayDimensions = null;
		}
		
		public ReturnType Clone()
		{
			return new ReturnType (FullyQualifiedName, arrayDimensions, pointerNestingLevel, genericArguments, false);
		}
		
		internal static ReturnType Convert (MonoDevelop.Projects.Parser.GenericParameter gp)
		{
			return new ReturnType (gp.Name, null, 0, null, false);
		}
		
		internal static string GetSystemType (string type)
		{
			string val;
			if (types.TryGetValue (type, out val))
				return val;
			else
				return type;
		}
	}*/
}
