// created on 06.08.2003 at 12:35

using MonoDevelop.Internal.Parser;
using ICSharpCode.SharpRefactory.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Constructor : AbstractMethod
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Constructor(Modifier m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = "ctor";
			this.region     = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
	}
}
