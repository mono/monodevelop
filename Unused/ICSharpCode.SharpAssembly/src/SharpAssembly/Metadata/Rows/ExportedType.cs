// ExportedType.cs
// Copyright (C) 2003 Mike Krueger
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.IO;

namespace MonoDevelop.SharpAssembly.Metadata.Rows {
	
	public class ExportedType : AbstractRow
	{
		public static readonly int TABLE_ID = 0x27;
		
		uint flags;
		uint typeDefId; // 4 byte index into a TypeDef table of another module in this Assembly
		uint typeName;  // index into the String heap
		uint typeNamespace; // index into the String heap
		uint implementation; // index see 21.14
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		public uint TypeDefId {
			get {
				return typeDefId;
			}
			set {
				typeDefId = value;
			}
		}
		public uint TypeName {
			get {
				return typeName;
			}
			set {
				typeName = value;
			}
		}
		public uint TypeNamespace {
			get {
				return typeNamespace;
			}
			set {
				typeNamespace = value;
			}
		}
		public uint Implementation {
			get {
				return implementation;
			}
			set {
				implementation = value;
			}
		}
		
		
		public override void LoadRow()
		{
			flags         = binaryReader.ReadUInt32();
			typeDefId     = binaryReader.ReadUInt32();
			typeName      = LoadStringIndex();
			typeNamespace = LoadStringIndex();
			
			// todo 32 bit indices ?
			implementation = binaryReader.ReadUInt16();
		}
	}
}
