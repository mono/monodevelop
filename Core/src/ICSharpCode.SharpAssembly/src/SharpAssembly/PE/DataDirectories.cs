// DataDirectories.cs
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
	
	public class DataDirectories
	{
		uint exportTable;
		uint exportTableSize;
		uint importTable;
		uint importTableSize;
		uint resourceTable;
		uint resourceTableSize;
		uint exceptionTable;
		uint exceptionTableSize;
		uint certificateTable;
		uint certificateTableSize;
		uint baseRelocationTable;
		uint baseRelocationTableSize;
		uint debugTable;
		uint debugTableSize;
		uint copyrightTable;
		uint copyrightTableSize;
		uint globalPtr;
		uint globalPtrSize;
		uint tlsTable;
		uint tlsTableSize;
		uint loadConfigTable;
		uint loadConfigTableSize;
		uint boundImport;
		uint boundImportSize;
		uint iAT;
		uint iATSize;
		uint delayImportDescriptor;
		uint delayImportDescriptorSize;
		uint cliHeader;
		uint cliHeaderSize;
		uint reserved;
		uint reservedSize;
		
		public uint ExportTable {
			get {
				return exportTable;
			}
			set {
				exportTable = value;
			}
		}
		public uint ExportTableSize {
			get {
				return exportTableSize;
			}
			set {
				exportTableSize = value;
			}
		}
		public uint ImportTable {
			get {
				return importTable;
			}
			set {
				importTable = value;
			}
		}
		public uint ImportTableSize {
			get {
				return importTableSize;
			}
			set {
				importTableSize = value;
			}
		}
		public uint ResourceTable {
			get {
				return resourceTable;
			}
			set {
				resourceTable = value;
			}
		}
		public uint ResourceTableSize {
			get {
				return resourceTableSize;
			}
			set {
				resourceTableSize = value;
			}
		}
		public uint ExceptionTable {
			get {
				return exceptionTable;
			}
			set {
				exceptionTable = value;
			}
		}
		public uint ExceptionTableSize {
			get {
				return exceptionTableSize;
			}
			set {
				exceptionTableSize = value;
			}
		}
		public uint CertificateTable {
			get {
				return certificateTable;
			}
			set {
				certificateTable = value;
			}
		}
		public uint CertificateTableSize {
			get {
				return certificateTableSize;
			}
			set {
				certificateTableSize = value;
			}
		}
		public uint BaseRelocationTable {
			get {
				return baseRelocationTable;
			}
			set {
				baseRelocationTable = value;
			}
		}
		public uint BaseRelocationTableSize {
			get {
				return baseRelocationTableSize;
			}
			set {
				baseRelocationTableSize = value;
			}
		}
		public uint DebugTable {
			get {
				return debugTable;
			}
			set {
				debugTable = value;
			}
		}
		public uint DebugTableSize {
			get {
				return debugTableSize;
			}
			set {
				debugTableSize = value;
			}
		}
		public uint CopyrightTable {
			get {
				return copyrightTable;
			}
			set {
				copyrightTable = value;
			}
		}
		public uint CopyrightTableSize {
			get {
				return copyrightTableSize;
			}
			set {
				copyrightTableSize = value;
			}
		}
		public uint GlobalPtr {
			get {
				return globalPtr;
			}
			set {
				globalPtr = value;
			}
		}
		public uint GlobalPtrSize {
			get {
				return globalPtrSize;
			}
			set {
				globalPtrSize = value;
			}
		}
		public uint TlsTable {
			get {
				return tlsTable;
			}
			set {
				tlsTable = value;
			}
		}
		public uint TlsTableSize {
			get {
				return tlsTableSize;
			}
			set {
				tlsTableSize = value;
			}
		}
		public uint LoadConfigTable {
			get {
				return loadConfigTable;
			}
			set {
				loadConfigTable = value;
			}
		}
		public uint LoadConfigTableSize {
			get {
				return loadConfigTableSize;
			}
			set {
				loadConfigTableSize = value;
			}
		}
		public uint BoundImport {
			get {
				return boundImport;
			}
			set {
				boundImport = value;
			}
		}
		public uint BoundImportSize {
			get {
				return boundImportSize;
			}
			set {
				boundImportSize = value;
			}
		}
		public uint IAT {
			get {
				return iAT;
			}
			set {
				iAT = value;
			}
		}
		public uint IATSize {
			get {
				return iATSize;
			}
			set {
				iATSize = value;
			}
		}
		public uint DelayImportDescriptor {
			get {
				return delayImportDescriptor;
			}
			set {
				delayImportDescriptor = value;
			}
		}
		public uint DelayImportDescriptorSize {
			get {
				return delayImportDescriptorSize;
			}
			set {
				delayImportDescriptorSize = value;
			}
		}
		public uint CliHeader {
			get {
				return cliHeader;
			}
			set {
				cliHeader = value;
			}
		}
		public uint CliHeaderSize {
			get {
				return cliHeaderSize;
			}
			set {
				cliHeaderSize = value;
			}
		}
		public uint Reserved {
			get {
				return reserved;
			}
			set {
				reserved = value;
			}
		}
		public uint ReservedSize {
			get {
				return reservedSize;
			}
			set {
				reservedSize = value;
			}
		}
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			exportTable     = binaryReader.ReadUInt32();
			exportTableSize = binaryReader.ReadUInt32();
			
			importTable     = binaryReader.ReadUInt32();
			importTableSize = binaryReader.ReadUInt32();
			
			resourceTable     = binaryReader.ReadUInt32();
			resourceTableSize = binaryReader.ReadUInt32();
			
			exceptionTable     = binaryReader.ReadUInt32();
			exceptionTableSize = binaryReader.ReadUInt32();
			
			certificateTable     = binaryReader.ReadUInt32();
			certificateTableSize = binaryReader.ReadUInt32();
			
			baseRelocationTable     = binaryReader.ReadUInt32();
			baseRelocationTableSize = binaryReader.ReadUInt32();
			
			debugTable     = binaryReader.ReadUInt32();
			debugTableSize = binaryReader.ReadUInt32();
			
			copyrightTable     = binaryReader.ReadUInt32();
			copyrightTableSize = binaryReader.ReadUInt32();
			
			globalPtr     = binaryReader.ReadUInt32();
			globalPtrSize = binaryReader.ReadUInt32();
			
			tlsTable     = binaryReader.ReadUInt32();
			tlsTableSize = binaryReader.ReadUInt32();
			
			loadConfigTable     = binaryReader.ReadUInt32();
			loadConfigTableSize = binaryReader.ReadUInt32();
			
			boundImport     = binaryReader.ReadUInt32();
			boundImportSize = binaryReader.ReadUInt32();
			
			iAT     = binaryReader.ReadUInt32();
			iATSize = binaryReader.ReadUInt32();
			
			delayImportDescriptor     = binaryReader.ReadUInt32();
			delayImportDescriptorSize = binaryReader.ReadUInt32();
			
			cliHeader     = binaryReader.ReadUInt32();
			cliHeaderSize = binaryReader.ReadUInt32();
			
			reserved     = binaryReader.ReadUInt32();
			reservedSize = binaryReader.ReadUInt32();
		}
	}
}
