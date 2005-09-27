// ConstructorDeclaration.cs
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
	public class ConstructorDeclaration : AbstractNode
	{
		string          name;
		Modifier modifier;
		ArrayList       parameters = new ArrayList(); // [ParameterDeclarationExpression]
		ArrayList       attributes = new ArrayList();
		BlockStatement  blockStatement = null;
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
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
		
		public ArrayList Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
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
		
		public BlockStatement Body {
			get {
				return blockStatement;
			}
			set {
				blockStatement = value;
			}
		}
		
		public ConstructorDeclaration(string name, Modifier modifier, ArrayList parameters, ArrayList attributes)
		{
			this.name     = name;
			this.modifier = modifier;
			this.parameters = parameters;
			this.attributes = attributes;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}

