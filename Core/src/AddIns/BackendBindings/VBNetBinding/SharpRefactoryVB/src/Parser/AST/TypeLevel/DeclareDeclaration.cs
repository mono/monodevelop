// DeclareDeclaration.cs
// Copyright (C) 2003 Markus Palme (markuspalme@gmx.de)
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
	public class DeclareDeclaration : AbstractNode
	{
		string          name;
		string          alias;
		string          library;
		CharsetModifier charset = CharsetModifier.None;
		Modifier modifier;
		TypeReference   returnType;
		ArrayList       parameters;
		ArrayList       attributes;
		
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
		
		public TypeReference ReturnType {
			get {
				return returnType;
			}
			set {
				returnType = value;
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
		
		public CharsetModifier Charset
		{
			get {
				return charset;
			}
			set {
				charset = value;
			}
		}
		
		public string Alias
		{
			get {
				return alias;
			}
			set {
				alias = value;
			}
		}
		
		public string Library
		{
			get {
				return library;
			}
			set {
				library = value;
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
		
		public DeclareDeclaration(string name, Modifier modifier, TypeReference returnType, ArrayList parameters, ArrayList attributes, string library, string alias, CharsetModifier charset)
		{
			this.name = name;
			this.modifier = modifier;
			this.returnType = returnType;
			this.parameters = parameters;
			this.attributes = attributes;
			this.library = library;
			this.alias = alias;
			this.charset = charset;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
