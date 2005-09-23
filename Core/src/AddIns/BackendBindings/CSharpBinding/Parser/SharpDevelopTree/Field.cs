// created on 04.08.2003 at 18:06

using MonoDevelop.Internal.Parser;
using ICSharpCode.SharpRefactory.Parser;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Field : AbstractField
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Field(ReturnType type, string fullyQualifiedName, Modifier m, IRegion region)
		{
			this.returnType = type;
			this.FullyQualifiedName = fullyQualifiedName;
			this.region = region;
			modifiers = (ModifierEnum)m;
		}
	}
}
