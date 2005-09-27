// TypeDeclaration.cs
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
using System.Collections.Specialized;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class TypeDeclaration : AbstractNode
	{
		// Children of Enum : [Field]
		string name;
		Modifier modifier;
		Types type; // Class | Interface | Structure | Enum | Module
		string baseType = null;
		ArrayList attributes;
		ArrayList baseInterfaces;
		
		public string BaseType {
			get {
				return baseType;
			}
			set {
				baseType = value;
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
		public Modifier Modifier {
			get {
				return modifier;
			}
			set {
				modifier = value;
			}
		}
		public Types Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		public ArrayList BaseInterfaces  {
			get {
				return baseInterfaces;
			}
			set {
				baseInterfaces = value;
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
		
//		public TypeDeclaration(string name, Modifier modifier, Types type, StringCollection bases, ArrayList attributes)
//		{
//			this.name = name;
//			this.modifier = modifier;
//			this.type = type;
//			this.bases = bases;
//			this.attributes = attributes;
//		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[TypeDeclaration: Name={0}, Modifier={1}, Type={2}, BaseType={3}]",
			                     name,
			                     modifier,
			                     type,
			                     baseType
//			                     ,GetCollectionString(bases)
			                     );
		}
		
	}
}
