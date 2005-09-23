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

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class TypeReference
	{
		string type;
		string systemType;
		int    pointerNestingLevel = 0;
		int[]  rankSpecifier;
		
		static Hashtable types = new Hashtable();
		static TypeReference()
		{
			types.Add("bool",    "System.Boolean");
			types.Add("byte",    "System.Byte");
			types.Add("char",    "System.Char");
			types.Add("decimal", "System.Decimal");
			types.Add("double",  "System.Double");
			types.Add("float",   "System.Single");
			types.Add("int",     "System.Int32");
			types.Add("long",    "System.Int64");
			types.Add("object",  "System.Object");
			types.Add("sbyte",   "System.SByte");
			types.Add("short",   "System.Int16");
			types.Add("string",  "System.String");
			types.Add("uint",    "System.UInt32");
			types.Add("ulong",   "System.UInt64");
			types.Add("ushort",  "System.UInt16");
			types.Add("void",    "System.Void");
		}

		public static ICollection PrimitiveTypes {
			get {
				return types.Keys;
			}
		}
		
		public string Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public string SystemType {
			get {
				return systemType;
			}
		}
		
		public int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
			set {
				pointerNestingLevel = value;
			}
		}
		
		public int[] RankSpecifier {
			get {
				return rankSpecifier;
			}
		}
		
		public bool IsArrayType {
			get {
				return rankSpecifier != null && rankSpecifier.Length > 0;
			}
		}
		
		string GetSystemType(string type)
		{
			if (types.ContainsKey(type)) {
				return (string)types[type];
			}
			return type;
		}
		
		public TypeReference(string type)
		{
			this.systemType = GetSystemType(type);
			this.type = type;
		}
		
		public TypeReference(string type, int pointerNestingLevel, int[] rankSpecifier)
		{
			this.type = type;
			this.systemType = GetSystemType(type);
			this.pointerNestingLevel = pointerNestingLevel;
			this.rankSpecifier = rankSpecifier;
		}
		
		public override string ToString()
		{
			return String.Format("[TypeReference: Type={0}, PointerNestingLevel={1}, RankSpecifier={2}]", type, pointerNestingLevel, rankSpecifier);
		}
	}
}
