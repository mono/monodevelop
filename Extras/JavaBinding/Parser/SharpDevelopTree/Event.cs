// created on 06.08.2003 at 12:30

using MonoDevelop.Internal.Parser;
using JRefactory.Parser;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class Event : AbstractEvent
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Event(string name, ReturnType type, Modifier m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = name;
			returnType         = type;
			this.region        = region;
			this.bodyRegion    = bodyRegion;
			modifiers          = (ModifierEnum)m;
		}
	}
}
