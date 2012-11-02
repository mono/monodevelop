// 
// StandardFields.cs
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
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.Core.CustomAssemblyReader
{
	class StandardFields
	{
		const ushort MAGIC = 0x10B;
		
		byte lMajor;

		public byte LMajor {
			get {
				return lMajor;
			}
		}

		byte lMinor;

		public byte LMinor {
			get {
				return lMinor;
			}
		}

		uint codeSize;

		public uint CodeSize {
			get {
				return codeSize;
			}
		}

		uint initializedDataSize;

		public uint InitializedDataSize {
			get {
				return initializedDataSize;
			}
		}

		uint uninitializedDataSize;

		public uint UninitializedDataSize {
			get {
				return uninitializedDataSize;
			}
		}

		uint entryPointRVA;

		public uint EntryPointRVA {
			get {
				return entryPointRVA;
			}
		}

		uint baseOfCode;

		public uint BaseOfCode {
			get {
				return baseOfCode;
			}
		}

		uint baseOfData;

		public uint BaseOfData {
			get {
				return baseOfData;
			}
		}
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			ushort magic = binaryReader.ReadUInt16();
			if (magic != MAGIC) {
				Console.WriteLine("Warning OptionalHeader.StandardFields != " + MAGIC + " was " + magic);
			}
			lMajor = binaryReader.ReadByte();
			Debug.Assert(lMajor == 6 || lMajor == 7);
			lMinor = binaryReader.ReadByte();
			Debug.Assert(lMinor == 0 || lMinor == 10);
			codeSize = binaryReader.ReadUInt32();
			initializedDataSize = binaryReader.ReadUInt32();
			uninitializedDataSize = binaryReader.ReadUInt32();
			entryPointRVA = binaryReader.ReadUInt32();
			baseOfCode = binaryReader.ReadUInt32();
			baseOfData = binaryReader.ReadUInt32();
		}
	}
	
}
