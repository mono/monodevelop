// NTSpecificFields.cs
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
	
	public class NTSpecificFields
	{
		const uint IMAGE_BASE        = 0x400000;
		const uint SECTION_ALIGNMENT = 0x2000;
		
//		uint fileAlignment; // either 0x200 or 0x1000
//		ushort osMajor;
//		ushort osMinor;
//		ushort userMajor;
//		ushort userMinor;
//		ushort subSysMajor;
//		ushort subSysMinor;
//		uint   reserved;
//		uint   imageSize;
//		uint   headerSize;
//		uint   fileChecksum;
//		ushort subSystem;
//		ushort dllFlags;
//		uint   stackReserveSize;
//		uint   stackCommitSize;
//		uint   heapReserveSize;
//		uint   heapCommitSize;
//		uint   loaderFlags;
//		uint   numberOfDataDirectories;
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			// TODO
			byte[] buffer = new byte[68];
			binaryReader.Read(buffer, 0, 68);
		}
	}
}
