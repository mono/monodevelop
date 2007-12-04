// created on 04.08.2003 at 18:06

using MonoDevelop.Projects.Parser;
using JRefactory.Parser;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class Field : DefaultField
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
