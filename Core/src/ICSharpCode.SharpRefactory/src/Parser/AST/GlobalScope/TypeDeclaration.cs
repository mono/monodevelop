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

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class TypeDeclaration : AbstractNode
	{
		// Children of Enum : [Field]
		string name;
		Modifier modifier;
		Types type; // Class | Interface | Struct | Enum
		StringCollection bases;
		ArrayList attributes;
		bool partial;
		
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
		public StringCollection BaseTypes {
			get {
				return bases;
			}
			set {
				bases = value;
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

		// only valid for class, struct, and interface
		public bool IsPartial {
			get { return partial; }
		}

		// only valid for classes
		public bool IsStatic {
			get { return type == Types.Class && (modifier & Modifier.Static) != 0; }
		}

		public TypeDeclaration () : this (false)
		{
		}

		public TypeDeclaration (bool partial)
		{
			this.partial = partial;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[TypeDeclaration: Name={0}, Modifier={1}, Type={2}, BaseTypes={3}]",
			                     name,
			                     modifier,
			                     type,
			                     GetCollectionString(bases));
		}
		
	}
}
