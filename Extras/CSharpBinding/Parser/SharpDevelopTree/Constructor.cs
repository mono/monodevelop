// created on 06.08.2003 at 12:35

using MonoDevelop.Projects.Parser;
using ICSharpCode.NRefactory.Parser;
using ModifierFlags = ICSharpCode.NRefactory.Parser.AST.Modifier;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Constructor : AbstractMethod
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Constructor (IClass declaringType, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = "ctor";
			this.region     = region;
			this.bodyRegion = bodyRegion;
			this.declaringType = declaringType;
			modifiers = (ModifierEnum)m;
		}
	}
}
