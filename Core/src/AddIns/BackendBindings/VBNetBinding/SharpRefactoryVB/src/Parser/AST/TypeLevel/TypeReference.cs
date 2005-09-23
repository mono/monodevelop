// TypeReference.cs
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
using System.Text;
using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class TypeReference : AbstractNode
	{
		string type;
		string systemType;
		ArrayList rankSpecifier;
		ArrayList dimension;
		AttributeSection attributes = null;
		
		static Hashtable types = new Hashtable();
		static TypeReference()
		{
			types.Add("boolean", "System.Boolean");
			types.Add("byte",    "System.Byte");
			types.Add("date",	 "System.DateTime");
			types.Add("char",    "System.Char");
			types.Add("decimal", "System.Decimal");
			types.Add("double",  "System.Double");
			types.Add("single",  "System.Single");
			types.Add("integer", "System.Int32");
			types.Add("long",    "System.Int64");
			types.Add("object",  "System.Object");
			types.Add("short",   "System.Int16");
			types.Add("string",  "System.String");
		}
		
		public AttributeSection Attributes
		{
			get {
				return attributes;
			}
			set {
				attributes = value;
			}
		}
		
		public static ICollection PrimitiveTypes
		{
			get {
				return types.Keys;
			}
		}
		
		public string Type
		{
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public string SystemType
		{
			get {
				return systemType;
			}
		}
		
		public ArrayList RankSpecifier
		{
			get {
				return rankSpecifier;
			}
			set {
				rankSpecifier = value;
			}
		}
		
		public bool IsArrayType
		{
			get {
				return rankSpecifier != null && rankSpecifier.Count> 0;
			}
		}
		
		public ArrayList Dimension
		{
			get {
				return dimension;
			}
			set {
				dimension = value;
			}
		}
		
		string GetSystemType(string type)
		{
			return (string)types[type.ToLower()];
		}
		
		public TypeReference(string type)
		{
			this.systemType = GetSystemType(type);
			this.type = type;
		}
		
		public TypeReference(string type, ArrayList rankSpecifier)
		{
			this.type = type;
			this.systemType = GetSystemType(type);
			this.rankSpecifier = rankSpecifier;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[TypeReference: Type={0}, RankSpeifier={1}]", type, AbstractNode.GetCollectionString(rankSpecifier));
		}
	}
}
