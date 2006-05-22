// created on 06.08.2003 at 12:30

using MonoDevelop.Projects.Parser;
using ICSharpCode.NRefactory.Parser;
using ModifierFlags = ICSharpCode.NRefactory.Parser.AST.Modifier;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Event : AbstractEvent
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Event (IClass declaringType, string name, ReturnType type, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = name;
			returnType         = type;
			this.region        = region;
			this.bodyRegion    = bodyRegion;
			this.declaringType = declaringType;
			modifiers          = (ModifierEnum)m;
		}
	}
}
