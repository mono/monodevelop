// FieldDeclaration.cs
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
	public class FieldDeclaration : AbstractNode
	{
		ArrayList       attributes = null;
		TypeReference   typeReference = null;
		Modifier        modifier;
		ArrayList       fields = new ArrayList(); // [VariableDeclaration]
		
		public ArrayList Attributes {
			get {
				return attributes;
			}
			set {
				attributes = value;
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
		public ArrayList Fields {
			get {
				return fields;
			}
			set {
				fields = value;
			}
		}
		
		// for enum members
		public FieldDeclaration(ArrayList attributes)
		{
			this.attributes = attributes;
		}
		
		// for all other cases
		public FieldDeclaration(ArrayList attributes, TypeReference typeReference, Modifier modifier)
		{
			this.attributes    = attributes;
			this.typeReference = typeReference;
			this.modifier      = modifier;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public VariableDeclaration GetVariableDeclaration(string variableName)
		{
			foreach (VariableDeclaration variableDeclaration in Fields) {
				if (variableDeclaration.Name == variableName) {
					return variableDeclaration;
				}
			}
			return null;
		}
	}
}
