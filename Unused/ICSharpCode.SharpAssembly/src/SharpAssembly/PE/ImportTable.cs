// ImportTable.cs
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

namespace MonoDevelop.SharpAssembly.PE {
	
	public class ImportTable
	{
		const int UNUSED_SIZE = 20;
		
		uint importLookupTable  = 0;
		uint dateTimeStamp      = 0;
		uint forwarderChain     = 0;
		uint importTableName    = 0;
		uint importAddressTable = 0;
		byte[] unused           = new byte[UNUSED_SIZE];
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			importLookupTable  = binaryReader.ReadUInt32();
			dateTimeStamp      = binaryReader.ReadUInt32();
			forwarderChain     = binaryReader.ReadUInt32();
			importTableName    = binaryReader.ReadUInt32();
			importAddressTable = binaryReader.ReadUInt32();
			binaryReader.Read(unused, 0, UNUSED_SIZE);
		}
	}
}
