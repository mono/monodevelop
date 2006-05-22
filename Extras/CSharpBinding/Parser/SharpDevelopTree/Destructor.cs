using System;
using MonoDevelop.Projects.Parser;
using ICSharpCode.NRefactory.Parser;
using ModifierFlags = ICSharpCode.NRefactory.Parser.AST.Modifier;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Destructor : AbstractMethod
	{
		public void AddModifier(ModifierEnum m)
		{
			modifiers = modifiers | m;
		}
		
		public Destructor (IClass declaringType, string className, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = "~" + className;
			this.region     = region;
			this.bodyRegion = bodyRegion;
			this.declaringType = declaringType; 
			modifiers = (ModifierEnum)m;
		}
	}
}
