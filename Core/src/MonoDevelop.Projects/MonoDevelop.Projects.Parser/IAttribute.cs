//  IAttribute.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
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
