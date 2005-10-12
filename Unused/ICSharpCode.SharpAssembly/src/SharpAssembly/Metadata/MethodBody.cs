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

namespace MonoDevelop.SharpAssembly.Metadata.Rows
{
	
	public class MethodBody
	{
		public const byte CorILMethod_Fat        = 0x3;
		public const byte CorILMethod_TinyFormat = 0x2;
		public const byte CorILMethod_MoreSects  = 0x8;
		public const byte CorILMethod_InitLocals = 0x10;
		
		uint   flags          = 0;
		uint   headerSize     = 0;
		ushort maxStack       = 8;
		uint   codeSize       = 0;
		uint   localVarSigTok = 0;
		
		byte[] methodData;
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		public uint HeaderSize {
			get {
				return headerSize;
			}
			set {
				headerSize = value;
			}
		}
		public ushort MaxStack {
			get {
				return maxStack;
			}
			set {
				maxStack = value;
			}
		}
		public uint CodeSize {
			get {
				return codeSize;
			}
			set {
				codeSize = value;
			}
		}
		public uint LocalVarSigTok {
			get {
				return localVarSigTok;
			}
			set {
				localVarSigTok = value;
			}
		}
		public byte[] MethodData {
			get {
				return methodData;
			}
			set {
				methodData = value;
			}
		}
		
		
		public void Load(BinaryReader reader)
		{
			byte flagByte = reader.ReadByte();
			Console.Write("flagByte : " + flagByte.ToString("X"));
			
			switch (flagByte & 0x03) {
				case CorILMethod_Fat:
					byte nextByte       = reader.ReadByte();
					Console.WriteLine("  nextByte : " + nextByte.ToString("X"));
				
					flags        = (uint)(flagByte & ((nextByte & 0x0F) << 8));
					headerSize   = (uint)(nextByte >> 4);
					maxStack     = reader.ReadUInt16();
					codeSize     = reader.ReadUInt32();
					localVarSigTok = reader.ReadUInt32();
					// TODO : CorILMethod_MoreSects
					break;
				case CorILMethod_TinyFormat:
					flags      = (uint)flagByte & 0x03;
					codeSize   = (uint)flagByte >> 2;
					break;
				default:
					throw new System.NotSupportedException("not supported method body flag " + flagByte);
			}
			methodData = new byte[codeSize];
			reader.Read(methodData, 0, (int)codeSize);
		}
	}
}
