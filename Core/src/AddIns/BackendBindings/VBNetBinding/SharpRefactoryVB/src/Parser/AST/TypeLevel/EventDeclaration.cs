// EventDeclaration.cs
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
using System.Drawing;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class EventDeclaration : AbstractNode
	{
		TypeReference typeReference;
		Modifier modifier;
		ArrayList parameters;
		ArrayList attributes;
		string name;
		ImplementsClause implementsClause;
		
		public ArrayList Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
			}
		}
		public ImplementsClause ImplementsClause {
			get {
				return implementsClause;
			}
			set {
				implementsClause = value;
			}
		}
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = value;
			}
		}
		
		public Modifier Modifier {
			get {
				return modifier;
			}
			set {
				modifier = value;
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
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		
		
		public EventDeclaration(TypeReference typeReference, Modifier modifier, ArrayList parameters, ArrayList attributes, string name, ImplementsClause implementsClause)
		{
			this.typeReference = typeReference;
			this.modifier = modifier;
			this.parameters = parameters;
			this.attributes = attributes;
			this.name = name;
			this.implementsClause = implementsClause;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[EventDeclaration: typeReference = {0}, modifier = {1}, parameters = {2}, attributes = {3}, name = {4}, implementsClause = {5}]",
			                     typeReference,
			                     modifier,
			                     parameters,
			                     attributes,
			                     name,
			                     implementsClause);
		}
		
	}
}
