// DelegateDeclaration.cs
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
	public class DelegateDeclaration : AbstractNode
	{
		string          name;
		ModifierCollection modifiers;
		TypeReference   returnType;
		ArrayList       parameters = new ArrayList(); // [ParameterDeclarationExpression]
		ArrayList       attributes = new ArrayList();
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public ModifierCollection Modifiers {
			get {
				return modifiers;
			}
			set {
				modifiers = value;
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
		
		public ArrayList Attributes {
			get {
				return attributes;
			}
			set {
				attributes = value;
			}
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}

		public override string ToString()
		{
			return String.Format("[DelegateDeclaration: Name={0}, Modifier={1}, ReturnType={2}, parameters={3}, attributes={4}]",
			                     name,
			                     modifiers,
			                     returnType,
			                     GetCollectionString(parameters),
			                     GetCollectionString(attributes));
		}
		
		#region Declaration Points
		System.Drawing.Point declarationStartLocation;
		public System.Drawing.Point DeclarationStartLocation
		{
			get {
				return declarationStartLocation;
			}
			set {
				declarationStartLocation = value;
			}
		}
		
		System.Drawing.Point declarationEndLocation;
		public System.Drawing.Point DeclarationEndLocation
		{
			get {
				if (declarationEndLocation.IsEmpty)
					return this.StartLocation;
				else
					return declarationEndLocation;
			}
			set {
				declarationEndLocation = value;
			}
		}
		#endregion
	}
}
