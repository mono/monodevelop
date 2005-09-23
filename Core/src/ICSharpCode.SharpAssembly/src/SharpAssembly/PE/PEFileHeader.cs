// PEFileHeader.cs
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
using System.Diagnostics;
using System.IO;

namespace MonoDevelop.SharpAssembly.PE {
	
	public class PEFileHeader
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
