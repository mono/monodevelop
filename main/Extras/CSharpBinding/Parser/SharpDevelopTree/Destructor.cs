using System;
using MonoDevelop.Projects.Parser;
using ICSharpCode.NRefactory.Parser;
using ModifierFlags = ICSharpCode.NRefactory.Ast.Modifiers;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class Destructor : DefaultMethod
	{
		public Destructor (string className, ModifierFlags m, IRegion region, IRegion bodyRegion)
		{
			Name = "~" + className;
			this.region     = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
	}
}
