// created on 04.08.2003 at 18:08

using MonoDevelop.Projects.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
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
		}
		
		public ReturnType(string fullyQualifiedName, int[] arrayDimensions, int pointerNestingLevel, ReturnTypeList genericArguments)
		{
			this.FullyQualifiedName  = fullyQualifiedName;
			this.arrayDimensions     = arrayDimensions;
			this.pointerNestingLevel = pointerNestingLevel;
			this.genericArguments    = genericArguments;
		}
		
		public ReturnType (ICSharpCode.NRefactory.Parser.AST.TypeReference type): this (type, null)
		{
		}
		
		public ReturnType (ICSharpCode.NRefactory.Parser.AST.TypeReference type, IClass resolvedClass)
		{
			this.FullyQualifiedName  = resolvedClass != null ? resolvedClass.FullyQualifiedName : type.SystemType;
			this.arrayDimensions     = type.RankSpecifier == null ? new int[] { } : type.RankSpecifier;
			this.pointerNestingLevel = type.PointerNestingLevel;
			
			// Now get generic arguments
			if (type.GenericTypes != null && type.GenericTypes.Count > 0) {
				this.genericArguments = new ReturnTypeList ();
				
				// Decorate the name
				this.FullyQualifiedName = string.Concat (this.FullyQualifiedName, "`", type.GenericTypes.Count);
				
				// Now go get them!
				foreach (ICSharpCode.NRefactory.Parser.AST.TypeReference tr in type.GenericTypes) {
					this.genericArguments.Add (new ReturnType(tr));
				}
			}
		}
		public ReturnType Clone()
		{
			return new ReturnType (FullyQualifiedName, arrayDimensions, pointerNestingLevel, genericArguments);
		}
		
		/// <summary>
		/// This method is used to convert classes to return types of indexers.
		/// </summary>
		internal static ReturnType Convert (IClass cls)
		{
			ReturnTypeList rtl = null;
			
			if (cls.GenericParameters != null && cls.GenericParameters.Count > 0) {
				rtl = new ReturnTypeList();
				
				foreach (MonoDevelop.Projects.Parser.GenericParameter gp in cls.GenericParameters) {
					rtl.Add ( ReturnType.Convert (gp));
				}
			}
			
			return new ReturnType (cls.FullyQualifiedName, new int[] { 0 }, 0, rtl);
		}
		
		internal static ReturnType Convert (MonoDevelop.Projects.Parser.GenericParameter gp)
		{
			return new ReturnType (gp.Name, new int[0], 0, null);
		}
	}
}
