// created on 04.08.2003 at 18:08

using MonoDevelop.Projects.Parser;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class ReturnType : DefaultReturnType
	{
		public ReturnType(string fullyQualifiedName)
		{
			base.FullyQualifiedName = fullyQualifiedName;
		}
		
		public ReturnType(string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel)
		{
			this.FullyQualifiedName  = fullyQualifiedName;
			this.arrayDimensions     = arrayDimensions;
			this.pointerNestingLevel = pointerNestingLevel;
		}
		
		public ReturnType (JRefactory.Parser.AST.TypeReference type)
		{
			base.FullyQualifiedName  = type.SystemType;
			base.arrayDimensions     = type.RankSpecifier == null ? new int[] { } : type.RankSpecifier;
			base.pointerNestingLevel = type.PointerNestingLevel;
		}
		public ReturnType Clone()
		{
			return new ReturnType(FullyQualifiedName, arrayDimensions, pointerNestingLevel);
		}
	}
}
