// created on 06.08.2003 at 12:35

using MonoDevelop.Projects.Parser;
using JRefactory.Parser;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class Constructor : DefaultMethod
	{
		public Constructor(Modifier m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = "ctor";
			this.region     = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
	}
}
