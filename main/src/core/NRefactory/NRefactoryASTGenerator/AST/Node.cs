// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 975 $</version>
// </file>

using System;
using System.Collections.Generic;

namespace NRefactoryASTGenerator.AST
{
	interface INode {}
	interface INullable {}
	struct Point {}
	
	enum Modifier { None }
	
	[CustomImplementation]
	abstract class AbstractNode : INode {}
	
	abstract class AttributedNode : AbstractNode
	{
		List<AttributeSection> attributes;
		Modifier               modifier;
		
		public AttributedNode(List<AttributeSection> attributes) {}
		public AttributedNode(Modifier modifier, List<AttributeSection> attributes) {}
	}
	
	abstract class ParametrizedNode : AttributedNode
	{
		string name;
		List<ParameterDeclarationExpression> parameters;
		
		public ParametrizedNode(Modifier modifier, List<AttributeSection> attributes,
		                        string name, List<ParameterDeclarationExpression> parameters)
			: base(modifier, attributes)
		{}
		
		public ParametrizedNode(Modifier modifier, List<AttributeSection> attributes)
			: base(modifier, attributes)
		{}
	}
	
	[CustomImplementation]
	class TypeReference : AbstractNode {}
	[CustomImplementation]
	class InnerClassTypeReference : TypeReference {}
	
	class AttributeSection : AbstractNode, INullable
	{
		string attributeTarget;
		List<Attribute> attributes;
		
		public AttributeSection(string attributeTarget, List<Attribute> attributes) {}
	}
	
	class Attribute : AbstractNode
	{
		string name;
		List<Expression> positionalArguments;
		List<NamedArgumentExpression> namedArguments;
		
		public Attribute(string name, List<Expression> positionalArguments, List<NamedArgumentExpression> namedArguments) {}
	}
}
