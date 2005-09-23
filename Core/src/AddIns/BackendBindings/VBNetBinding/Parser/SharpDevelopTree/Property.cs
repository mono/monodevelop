// created on 06.08.2003 at 12:36

using MonoDevelop.Internal.Parser;
using ICSharpCode.SharpRefactory.Parser.VB;

namespace VBBinding.Parser.SharpDevelopTree
{
	public class Property : AbstractProperty
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Property(string fullyQualifiedName, ReturnType type, Modifier m, IRegion region, IRegion bodyRegion)
		{
			this.FullyQualifiedName = fullyQualifiedName;
			returnType = type;
			this.region = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
	}
}
