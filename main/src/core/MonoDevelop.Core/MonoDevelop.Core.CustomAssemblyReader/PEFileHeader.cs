// 
// PEFileHeader.cs
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
	class PEFileHeader
	{
		const ushort machineSign = 0x14C;
		const ushort IMAGE_FILE_DLL = 0x2000;
		
		ushort numberOfSections;
		uint   time;
		uint   ptrToSymbolTable;
		uint   numerOfSymbols;
		ushort optionalHeaderSize;
		ushort characteristics;
		
		// optional header:
		StandardFields   standardFields    = new StandardFields();
		NTSpecificFields ntSpecificFields  = new NTSpecificFields();
		DataDirectories  dataDirectories   = new DataDirectories();
		
		public ushort NumberOfSections {
			get {
				return numberOfSections;
			}
			set {
				numberOfSections = value;
			}
		}
		
		public uint Time {
			get {
				return time;
			}
			set {
				time = value;
			}
		}
		
		public uint PtrToSymbolTable {
			get {
				return ptrToSymbolTable;
			}
			set {
				ptrToSymbolTable = value;
			}
		}
		
		public uint NumerOfSymbols {
			get {
				return numerOfSymbols;
			}
			set {
				numerOfSymbols = value;
			}
		}
		
		public ushort OptionalHeaderSize {
			get {
				return optionalHeaderSize;
			}
			set {
				optionalHeaderSize = value;
			}
		}
		
		public bool IsDLL {
			get {
				return (characteristics & IMAGE_FILE_DLL) == IMAGE_FILE_DLL;
			}
			set {
				if (value) {
					characteristics |= IMAGE_FILE_DLL;
				} else {
					characteristics = (ushort)(characteristics & ~IMAGE_FILE_DLL);
				}
			}
		}
		
		public StandardFields StandardFields {
			get {
				return standardFields;
			}
		}
		
		public NTSpecificFields NtSpecificFields {
			get {
				return ntSpecificFields;
			}
		}
		
		public DataDirectories DataDirectories {
			get {
				return dataDirectories;
			}
		}
		
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			// pe signature (always PE\0\0)
			byte[] signature = new byte[4];
			binaryReader.Read(signature, 0, 4);
			if (signature[0] != (byte)'P' && signature[1] != (byte)'E' && signature[2] != 0 && signature[3] != 0) {
				Console.WriteLine("NO PE FILE");
				return;
			}
			ushort machine = binaryReader.ReadUInt16();
			
			if (machine != machineSign) {
				Console.WriteLine("Wrong machine : " + machineSign);
				return;
			}
			
			numberOfSections = binaryReader.ReadUInt16();
			time             = binaryReader.ReadUInt32();
			
			ptrToSymbolTable = binaryReader.ReadUInt32();
			if (ptrToSymbolTable != 0) {
				Console.WriteLine("warning: ptrToSymbolTable != 0");
			}
			
			numerOfSymbols = binaryReader.ReadUInt32();
			if (numerOfSymbols != 0) {
				Console.WriteLine("warning: numerOfSymbols != 0");
			}
			
			optionalHeaderSize = binaryReader.ReadUInt16();
			characteristics    = binaryReader.ReadUInt16();
			
			standardFields.LoadFrom(binaryReader);
			ntSpecificFields.LoadFrom(binaryReader);
			dataDirectories.LoadFrom(binaryReader);
		}
	}
}
