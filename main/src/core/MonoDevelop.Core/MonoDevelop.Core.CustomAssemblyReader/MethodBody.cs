// 
// MethodBody.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Relicensed from SharpAssembly (c) 2003 by Mike Krüger
//
// Copyright (c) 2012 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;

namespace MonoDevelop.Core.CustomAssemblyReader
{
	class MethodBody
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
