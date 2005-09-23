// Property.cs
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
	
	public class Property : AbstractRow
	{
		public static readonly int TABLE_ID = 0x17;
		
		public static readonly ushort FLAG_SPECIALNAME   = 0x0200;
		public static readonly ushort FLAG_RTSPECIALNAME = 0x0400;
		public static readonly ushort FLAG_HASDEFAULT    = 0x1000;
		public static readonly ushort FLAG_UNUSED        = 0xe9ff;
		
		ushort flags;
		uint   name; // index into String heap
		uint   type; // index into Blob heap
		
		public ushort Flags {
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
		
		public uint Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public bool IsFlagSet(uint flag)
		{
			return base.BaseIsFlagSet(this.flags, flag);
		}
		
		public override void LoadRow()
		{
			flags = binaryReader.ReadUInt16();
			name  = LoadStringIndex();
			type  = LoadBlobIndex();
		}
	}
}
