// Field.cs
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
	
	public class Field : AbstractRow
	{
		public static readonly int TABLE_ID = 0x04;
		
		public static readonly ushort FLAG_FIELDACCESSMASK    = 0x0007;
		public static readonly ushort FLAG_COMPILERCONTROLLED = 0x0000;
		public static readonly ushort FLAG_PRIVATE            = 0x0001;
		public static readonly ushort FLAG_FAMANDASSEM        = 0x0002;
		public static readonly ushort FLAG_ASSEMBLY           = 0x0003;
		public static readonly ushort FLAG_FAMILY             = 0x0004;
		public static readonly ushort FLAG_FAMORASSEM         = 0x0005;
		public static readonly ushort FLAG_PUBLIC             = 0x0006;
		public static readonly ushort FLAG_STATIC             = 0x0010;
		public static readonly ushort FLAG_INITONLY           = 0x0020;
		public static readonly ushort FLAG_LITERAL            = 0x0040;
		public static readonly ushort FLAG_NOTSERIALIZED      = 0x0080;
		public static readonly ushort FLAG_SPECIALNAME        = 0x0200;
		public static readonly ushort FLAG_PINVOKEIMPL        = 0x2000;
		public static readonly ushort FLAG_RTSPECIALNAME      = 0x0400;
		public static readonly ushort FLAG_HASFIELDMARSHAL    = 0x1000;
		public static readonly ushort FLAG_HASDEFAULT         = 0x8000;
		public static readonly ushort FLAG_HASFIELDRVA        = 0x0100;
		
		ushort flags;
		uint   name;      // index into String heap
		uint   signature; // index into Blob heap
		
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
		
		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		public bool IsFlagSet(uint flag)
		{
			return base.BaseIsFlagSet(this.flags, flag);
		}
		
		public bool IsMaskedFlagSet(uint flag, uint flag_mask)
		{
			return base.BaseIsFlagSet(this.flags, flag, flag_mask);
		}
		
		public override void LoadRow()
		{
			flags     = binaryReader.ReadUInt16();
			name      = LoadStringIndex();
			signature = LoadBlobIndex();
		}
	}
}
