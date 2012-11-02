// 
// ImportTable.cs
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
	class ImportTable
	{
		const int UNUSED_SIZE = 20;
		
		uint importLookupTable  = 0;

		public uint ImportLookupTable {
			get {
				return importLookupTable;
			}
		}

		uint dateTimeStamp      = 0;

		public uint DateTimeStamp {
			get {
				return dateTimeStamp;
			}
		}

		uint forwarderChain     = 0;

		public uint ForwarderChain {
			get {
				return forwarderChain;
			}
		}

		uint importTableName    = 0;

		public uint ImportTableName {
			get {
				return importTableName;
			}
		}

		uint importAddressTable = 0;

		public uint ImportAddressTable {
			get {
				return importAddressTable;
			}
		}

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
