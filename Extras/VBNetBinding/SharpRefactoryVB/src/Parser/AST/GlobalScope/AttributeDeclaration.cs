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

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class Attribute : AbstractNode
	{
		string name;
		ArrayList positionalArguments; // [Expression]
		ArrayList namedArguments; // [NamedArgumentExpression]
		
		public Attribute(string name, ArrayList positionalArguments, ArrayList namedArguments)
		{
			this.name = name;
			this.positionalArguments = positionalArguments;
			this.namedArguments = namedArguments;
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
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
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
