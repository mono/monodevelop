// created on 06.08.2003 at 12:35
using System;
using MonoDevelop.Projects.Parser;
using JRefactory.Parser;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class Method : DefaultMethod
	{
		public Method(string name, ReturnType type, Modifier m, IRegion region, IRegion bodyRegion)
		{
			FullyQualifiedName = name;
			returnType = type;
			this.region     = region;
			this.bodyRegion = bodyRegion;
			modifiers = (ModifierEnum)m;
		}
	}
}
