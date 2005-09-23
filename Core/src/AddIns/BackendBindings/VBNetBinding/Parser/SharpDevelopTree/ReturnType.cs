// created on 04.08.2003 at 18:08
using System;
using MonoDevelop.Internal.Parser;

namespace VBBinding.Parser.SharpDevelopTree
{
	public class ReturnType : AbstractReturnType
	{
		public new int PointerNestingLevel {
			get {
				return base.pointerNestingLevel;
			}
			set {
				base.pointerNestingLevel = value;
			}
		}
		
		public new int[] ArrayDimensions {
			get {
				return base.arrayDimensions;
			}
			set {
				base.arrayDimensions = value;
			}
		}
		
		public ReturnType(string fullyQualifiedName)
		{
			base.FullyQualifiedName = fullyQualifiedName;
//			Console.WriteLine("NEW RETURN TYPE WITH : " + fullyQualifiedName);
		}
		
		public ReturnType(string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel)
		{
			this.FullyQualifiedName  = fullyQualifiedName;
			this.arrayDimensions     = arrayDimensions;
			this.pointerNestingLevel = pointerNestingLevel;
		}
		
		public ReturnType(ICSharpCode.SharpRefactory.Parser.AST.VB.TypeReference type)
		{
			base.FullyQualifiedName  = type.SystemType == null ? type.Type : type.SystemType;
			base.arrayDimensions     = type.RankSpecifier == null ? new int[] { } : (int[])type.RankSpecifier.ToArray(typeof(int));
			base.pointerNestingLevel = 0;
		}
		
		public ReturnType Clone()
		{
			return new ReturnType(FullyQualifiedName, arrayDimensions, pointerNestingLevel);
		}
	}
}
