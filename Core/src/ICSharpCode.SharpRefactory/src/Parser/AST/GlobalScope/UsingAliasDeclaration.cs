// UsingAliasDeclaration.cs
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
	public class UsingAliasDeclaration : AbstractNode
	{
		string    alias;
		string    nameSpace;
		
		public string Alias {
			get {
				return alias;
			}
			set {
				alias = value;
			}
		}
		
		public string Namespace {
			get {
				return nameSpace;
			}
			set {
				nameSpace = value;
			}
		}
		
		public UsingAliasDeclaration(string alias, string nameSpace)
		{
			this.alias     = alias;
			this.nameSpace = nameSpace;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[UsingAliasDeclaration: Namespace={0}, Alias={1}]",
			                     nameSpace,
			                     alias);
		}
	}
}
