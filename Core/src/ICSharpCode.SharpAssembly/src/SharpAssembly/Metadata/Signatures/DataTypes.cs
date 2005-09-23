// Assembly.cs
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

namespace MonoDevelop.SharpAssembly.Metadata
{
	public enum DataType
	{
		End            = 0x00,
		Void           = 0x01,
		Boolean        = 0x02,
		Char           = 0x03,
		SByte          = 0x04,
		Byte           = 0x05,
		Int16          = 0x06,
		UInt16         = 0x07,
		Int32          = 0x08,
		UInt32         = 0x09,
		Int64          = 0x0A,
		UInt64         = 0x0B,
		Single         = 0x0C,
		Double         = 0x0D,
		
		String         = 0x0E,
		Ptr            = 0x0F,
		ByRef          = 0x10,
		ValueType      = 0x11,
		Class          = 0x12,
		Array          = 0x14,
		
		TypeReference  = 0x16,
		IntPtr         = 0x18,
		UIntPtr        = 0x19,
		FnPtr          = 0x1B,
		Object         = 0x1C,
		SZArray        = 0x1D,
		
		CModReq        = 0x1F,
		CModOpt        = 0x20,
		Internal       = 0x21,
		
		Modifier       = 0x40,
		Sentinel       = 0x41,
		Pinned         = 0x45
		
	}
}
