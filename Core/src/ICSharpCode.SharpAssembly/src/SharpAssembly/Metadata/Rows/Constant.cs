// Constant.cs
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
	
	public class Constant : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0B;
		
		byte type;   // a 1 byte constant, followed by a 1-byte padding zero
		uint parent; // index into the Param or Field or Property table; more precisely, a HasConst coded index
		uint val;    // index into Blob heap
		
		public byte Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		public uint Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}
		public uint Val {
			get {
				return val;
			}
			set {
				val = value;
			}
		}
		
		public override void LoadRow()
		{
			type = binaryReader.ReadByte();
			/*byte paddingZero =*/ binaryReader.ReadByte();
//			if (paddingZero != 0) {
//				Console.WriteLine("padding zero != 0");
//			}
			parent = ReadCodedIndex(CodedIndex.HasConstant);
			val    = LoadBlobIndex();
		}
	}
}
