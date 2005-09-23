// created on 08.09.2003 at 16:17

using MonoDevelop.Internal.Parser;
using System.Collections;

namespace CSharpBinding.Parser.SharpDevelopTree
{
	public class AttributeSection : AbstractAttributeSection
	{
		public AttributeSection(AttributeTarget attributeTarget,
		                        AttributeCollection attributes) {
			this.attributeTarget = attributeTarget;
			this.attributes = attributes;
		}
	}
	public class ASTAttribute : AbstractAttribute
	{
		public ASTAttribute(string name, ArrayList positionalArguments, SortedList namedArguments)
		{
			this.name = name;
			this.positionalArguments = positionalArguments;
			this.namedArguments = namedArguments;
		}
	}
}
