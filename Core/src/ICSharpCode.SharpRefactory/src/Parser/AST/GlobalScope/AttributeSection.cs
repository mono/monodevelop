// AttributeDeclaration.cs
// Copyright (C) 2003 Mike Krueger (mike@icsharpcode.net)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class NamedArgument : AbstractNode
	{
		string name;
		Expression expr;
		
		public string Name {
			get {
				return name;
			}
		}
		public Expression Expr {
			get {
				return expr;
			}
		}
		
		public NamedArgument(string name, Expression expr)
		{
			this.name = name;
			this.expr = expr;
		}
	}
	
	public class Attribute : AbstractNode
	{
		string name;
		ArrayList positionalArguments;
		ArrayList namedArguments;
		
		public Attribute(string name, ArrayList positionalArguments, ArrayList namedArguments)
		{
			this.name = name;
			this.positionalArguments = positionalArguments; //[Expression]
			this.namedArguments      = namedArguments; //[NamedArgument]
		}
		
		public string Name {
			get {
				return name;
			}
		}
		public ArrayList PositionalArguments {
			get {
				return positionalArguments;
			}
		}
		public ArrayList NamedArguments {
			get {
				return namedArguments;
			}
		}
	}
	
	public class AttributeSection : AbstractNode
	{
		string    attributeTarget;
		ArrayList attributes; // [Attribute]
		
		public string AttributeTarget {
			get {
				return attributeTarget;
			}
			set {
				attributeTarget = value;
			}
		}
		
		public ArrayList Attributes {
			get {
				return attributes;
			}
			set {
				attributes = value;
			}
		}
		
		public AttributeSection(string attributeTarget, ArrayList attributes)
		{
			this.attributeTarget = attributeTarget;
			this.attributes = attributes;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
