// created on 04.08.2003 at 18:08

using MonoDevelop.Projects.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class ReturnType : DefaultReturnType
	{
		public ReturnType (string fullyQualifiedName): base (fullyQualifiedName)
		{
		}
		
		public ReturnType(string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel, ReturnTypeList genericArguments, bool fixDimensions)
		: base (fullyQualifiedName, arrayDimensions, pointerNestingLevel, genericArguments)
		{
			if (fixDimensions)
				SetArrayDimensions (arrayDimensions);
		}
		
		public ReturnType (ICSharpCode.NRefactory.Parser.AST.TypeReference type): this (type, null)
		{
		}
		
		public ReturnType (ICSharpCode.NRefactory.Parser.AST.TypeReference type, IClass resolvedClass)
		{
			this.FullyQualifiedName  = resolvedClass != null ? resolvedClass.FullyQualifiedName : type.SystemType;
			this.pointerNestingLevel = type.PointerNestingLevel;
			SetArrayDimensions (type.RankSpecifier);
			
			// Now get generic arguments
			if (type.GenericTypes != null && type.GenericTypes.Count > 0) {
				this.genericArguments = new ReturnTypeList ();
				
				// Decorate the name
				if (resolvedClass == null)
					this.FullyQualifiedName = string.Concat (this.FullyQualifiedName, "`", type.GenericTypes.Count);

				// Now go get them!
				foreach (ICSharpCode.NRefactory.Parser.AST.TypeReference tr in type.GenericTypes) {
					this.genericArguments.Add (new ReturnType(tr));
				}
			}
		}
		
		public static string GetFullTypeName (ICSharpCode.NRefactory.Parser.AST.TypeReference type)
		{
			if (type.GenericTypes != null && type.GenericTypes.Count > 0)
				return string.Concat (type.SystemType, "`", type.GenericTypes.Count);
			else
				return type.SystemType;
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
	}
}
