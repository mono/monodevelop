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

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class EventDeclaration : AbstractNode
	{
		TypeReference   typeReference;
		ArrayList variableDeclarators; // [Field]
		Modifier modifier;
		ArrayList attributes;
		string name;
		EventAddRegion    addRegion;
		EventRemoveRegion removeRegion;
		Point           bodyStart;
		Point           bodyEnd;
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = value;
			}
		}
		public ArrayList VariableDeclarators {
			get {
				return variableDeclarators;
			}
			set {
				variableDeclarators = value;
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
		public EventAddRegion AddRegion {
			get {
				return addRegion;
			}
			set {
				addRegion = value;
			}
		}
		public EventRemoveRegion RemoveRegion {
			get {
				return removeRegion;
			}
			set {
				removeRegion = value;
			}
		}
		
		public bool HasAddRegion {
			get {
				return addRegion != null;
			}
		}
		
		public bool HasRemoveRegion {
			get {
				return removeRegion != null;
			}
		}
		public Point BodyStart {
			get {
				return bodyStart;
			}
			set {
				bodyStart = value;
			}
		}
		public Point BodyEnd {
			get {
				return bodyEnd;
			}
			set {
				bodyEnd = value;
			}
		}
		
		public EventDeclaration()
		{}
		
		public EventDeclaration(Modifier modifier, ArrayList attributes)
		{
			this.modifier = modifier;
			this.attributes = attributes;
		}
		
		public EventDeclaration(TypeReference typeReference, ArrayList variableDeclarators, Modifier modifier, ArrayList attributes)
		{
			this.typeReference = typeReference;
			this.name = null;
			this.variableDeclarators = variableDeclarators;
			this.modifier = modifier;
			this.attributes = attributes;
		}
		
		public EventDeclaration(TypeReference typeReference, string name, Modifier modifier, ArrayList attributes)
		{
			this.typeReference = typeReference;
			this.name = name;
			this.variableDeclarators = null;
			this.modifier = modifier;
			this.attributes = attributes;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
