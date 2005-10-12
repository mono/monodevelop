// StreamHeader.cs
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
	
	public class StreamHeader
	{
		uint offset;
		uint size;
		string name;
		
		public uint Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		
		public uint Size {
			get {
				return size;
			}
			set {
				size = value;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			offset = binaryReader.ReadUInt32();
			size   = binaryReader.ReadUInt32();
			int bytesRead = 1;
			byte b = binaryReader.ReadByte();
			while (b != 0) {
				name += (char)b;
				b = binaryReader.ReadByte();
				++bytesRead;
			}
			// name is filled to 4 byte blocks
			int filler = bytesRead % 4 == 0 ? 0 :  4 - (bytesRead % 4);
			for (int i = 0; i < filler; ++i) {
				binaryReader.ReadByte();
			}
			
		}
	}
}
