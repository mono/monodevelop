// NameTable.cs
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
	
	public class NameTable
	{
		ushort hint;
		string name;
		
		public ushort Hint {
			get {
				return hint;
			}
			set {
				hint = value;
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
			hint = binaryReader.ReadUInt16();
			
			name = String.Empty;
			byte b = binaryReader.ReadByte();
			while (b != 0) {
				name += (char)b;
				b = binaryReader.ReadByte();
			}
		}
	}
}
