// ManifestResource.cs
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
	
	public class ManifestResource : AbstractRow
	{
		public static readonly int TABLE_ID = 0x28;
		
		public static readonly uint FLAG_VISIBILITYMASK = 0x0007;
		public static readonly uint FLAG_PUBLIC         = 0x0001;
		public static readonly uint FLAG_PRIVATE        = 0x0002;
		
		uint offset;
		uint flags;
		uint name; // index into String heap
		uint implementation; // index into File table, or AssemblyRef table, or  null; more precisely, an Implementation coded index
		
		public uint Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
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
			offset         = binaryReader.ReadUInt32();
			flags          = binaryReader.ReadUInt32();
			name           = LoadStringIndex();
			implementation = ReadCodedIndex(CodedIndex.Implementation);
		}
	}
}
