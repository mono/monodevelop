// created on 08.09.2003 at 16:17

using MonoDevelop.Projects.Parser;
using System.Collections;

namespace JavaBinding.Parser.SharpDevelopTree
{
	public class AttributeSection : DefaultAttributeSection
	{
		public AttributeSection(AttributeTarget attributeTarget,
		                        AttributeCollection attributes) {
			this.attributeTarget = attributeTarget;
			this.attributes = attributes;
		}
	}
	public class ASTAttribute : DefaultAttribute
	{
		public ASTAttribute(string name, ArrayList positionalArguments, SortedList namedArguments)
		{
			this.name = name;
			this.positionalArguments = positionalArguments;
			this.namedArguments = namedArguments;
		}
	}
}
