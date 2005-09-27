// Field.cs
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
	public class VariableDeclaration : AbstractNode
	{
		string     name;
		Expression initializer = null;
		TypeReference type;
		
		public TypeReference Type {
			get {
				return type;
			}
			set {
				type = value;
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
		
		public Expression Initializer {
			get {
				return initializer;
			}
			set {
				initializer = value;
			}
		}
		
		public VariableDeclaration(string name)
		{
			this.name = name;
		}
		
		public VariableDeclaration(string name, Expression initializer)
		{
			this.name = name;
			this.initializer = initializer;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[VariableDeclaration: Name={0}, Initializer={1}]", name, initializer);
		}
	}
}
