// StandardFields.cs
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
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.SharpAssembly.PE {
	
	public class StandardFields
	{
		const ushort MAGIC = 0x10B;
		
		byte lMajor;
		byte lMinor;
		uint codeSize;
		uint initializedDataSize;
		uint uninitializedDataSize;
		uint entryPointRVA;
		uint baseOfCode;
		uint baseOfData;
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			ushort magic = binaryReader.ReadUInt16();
			if (magic != MAGIC) {
				Console.WriteLine("Warning OptionalHeader.StandardFields != " + MAGIC + " was " + magic);
			}
			lMajor                = binaryReader.ReadByte();
			Debug.Assert(lMajor == 6);
			lMinor                = binaryReader.ReadByte();
			Debug.Assert(lMinor == 0);
			codeSize              = binaryReader.ReadUInt32();
			initializedDataSize   = binaryReader.ReadUInt32();
			uninitializedDataSize = binaryReader.ReadUInt32();
			entryPointRVA         = binaryReader.ReadUInt32();
			baseOfCode            = binaryReader.ReadUInt32();
			baseOfData            = binaryReader.ReadUInt32();
		}
	}
	
}
