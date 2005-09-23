// Method.cs
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
	
	public class Method : AbstractRow
	{
		public static readonly int TABLE_ID = 0x06;
		
		public static readonly ushort FLAG_MEMBERACCESSMASK   = 0X0007;
		public static readonly ushort FLAG_COMPILERCONTROLLED = 0X0000;
		public static readonly ushort FLAG_PRIVATE            = 0X0001;
		public static readonly ushort FLAG_FAMANDASSEM        = 0X0002;
		public static readonly ushort FLAG_ASSEM              = 0X0003;
		public static readonly ushort FLAG_FAMILY             = 0X0004;
		public static readonly ushort FLAG_FAMORASSEM         = 0X0005;
		public static readonly ushort FLAG_PUBLIC             = 0X0006;
		public static readonly ushort FLAG_STATIC             = 0X0010;
		public static readonly ushort FLAG_FINAL              = 0X0020;
		public static readonly ushort FLAG_VIRTUAL            = 0X0040;
		public static readonly ushort FLAG_HIDEBYSIG          = 0X0080;
		public static readonly ushort FLAG_VTABLELAYOUTMASK   = 0X0100;
		public static readonly ushort FLAG_REUSESLOT          = 0X0000;
		public static readonly ushort FLAG_NEWSLOT            = 0X0100;
		public static readonly ushort FLAG_ABSTRACT           = 0X0400;
		public static readonly ushort FLAG_SPECIALNAME        = 0X0800;
		public static readonly ushort FLAG_PINVOKEIMPL        = 0X2000;
		public static readonly ushort FLAG_UNMANAGEDEXPORT    = 0X0008;
		public static readonly ushort FLAG_RTSPECIALNAME      = 0X1000;
		public static readonly ushort FLAG_HASSECURITY        = 0X4000;
		public static readonly ushort FLAG_REQUIRESECOBJECT   = 0X8000;
		
		public static readonly ushort IMPLFLAG_CODETYPEMASK     = 0X0003;
		public static readonly ushort IMPLFLAG_IL               = 0X0000;
		public static readonly ushort IMPLFLAG_NATIVE           = 0X0001;
		public static readonly ushort IMPLFLAG_OPTIL            = 0X0002;
		public static readonly ushort IMPLFLAG_RUNTIME          = 0X0003;
		public static readonly ushort IMPLFLAG_MANAGEDMASK      = 0X0004;
		public static readonly ushort IMPLFLAG_UNMANAGED        = 0X0004;
		public static readonly ushort IMPLFLAG_MANAGED          = 0X0000;
		public static readonly ushort IMPLFLAG_FORWARDREF       = 0X0010;
		public static readonly ushort IMPLFLAG_PRESERVESIG      = 0X0080;
		public static readonly ushort IMPLFLAG_INTERNALCALL     = 0X1000;
		public static readonly ushort IMPLFLAG_SYNCHRONIZED     = 0X0020;
		public static readonly ushort IMPLFLAG_NOINLINING       = 0X0008;
		public static readonly ushort IMPLFLAG_MAXMETHODIMPLVAL = 0XFFFF;
		
		uint   rva;
		ushort implFlags;
		ushort flags;
		uint   name;      // index into String heap
		uint   signature; // index into Blob heap
		uint   paramList; // index into Param table
		
		public uint RVA {
			get {
				return rva;
			}
			set {
				rva = value;
			}
		}
		
		public ushort ImplFlags {
			get {
				return implFlags;
			}
			set {
				implFlags = value;
			}
		}
		
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
		
		public uint ParamList {
			get {
				return paramList;
			}
			set {
				paramList = value;
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
		
		public bool IsImplFlagSet(uint flag)
		{
			return base.BaseIsFlagSet(this.implFlags, flag);
		}
		
		public override void LoadRow()
		{
			rva       = binaryReader.ReadUInt32();
			implFlags = binaryReader.ReadUInt16();
			flags     = binaryReader.ReadUInt16();
			name      = LoadStringIndex();
			signature = LoadBlobIndex();
			
			paramList = ReadSimpleIndex(Param.TABLE_ID);
		}
	}
}
