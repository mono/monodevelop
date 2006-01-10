// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.CodeDom;
using System.Collections;

namespace MonoDevelop.Projects.Parser
{
	public interface IAttributeSection: IComparable
	{
		AttributeTarget AttributeTarget {
			get;
		}
		AttributeCollection Attributes {
			get;
		}
		IRegion Region {
			get;
		}
	}

	public interface IAttribute: IComparable
	{
		string Name {
			get;
		}
		CodeExpression[] PositionalArguments { // [expression]
			get;
		}
		NamedAttributeArgument[] NamedArguments { // string/expression
			get;
		}
	}
	
	[Serializable]
	public class NamedAttributeArgument
	{
		string name;
		CodeExpression expression;
		
		public NamedAttributeArgument (string name, CodeExpression expression)
		{
			this.name = name;
			this.expression = expression;
		}
		
		public string Name {
			get { return name; }
		}
		
		public CodeExpression Expression {
			get { return expression; }
		}
	}

	public enum AttributeTarget
	{
		None,
		Assembly,
		Field,
		Event,
		Method,
		Module,
		Param,
		Property,
		Return,
		Type
	}

}
