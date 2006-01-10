// created on 08.09.2003 at 16:17

using MonoDevelop.Projects.Parser;
using System.Collections;
using System.CodeDom;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class AttributeSection : AbstractAttributeSection
	{
		public AttributeSection(AttributeTarget attributeTarget, IRegion region)
		{
			this.attributeTarget = attributeTarget;
			this.region = region;
		}
	}
	public class Attribute : AbstractAttribute
	{
		public Attribute (string name, CodeExpression[] positionalArguments, NamedAttributeArgument[] namedArguments, IRegion region)
		{
			this.name = name;
			this.positionalArguments = positionalArguments;
			this.namedArguments = namedArguments;
			this.region = region;
		}
	}
}
