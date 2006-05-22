// created on 04.08.2003 at 18:06

using MonoDevelop.Projects.Parser;
using ICSharpCode.NRefactory.Parser;
using ModifierFlags = ICSharpCode.NRefactory.Parser.AST.Modifier;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Field : AbstractField
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Field (IClass declaringType, ReturnType type, string fullyQualifiedName, ModifierFlags m, IRegion region)
		{
			this.returnType = type;
			this.FullyQualifiedName = fullyQualifiedName;
			this.region = region;
			this.declaringType = declaringType;
			modifiers = (ModifierEnum)m;
		}
	}
}
