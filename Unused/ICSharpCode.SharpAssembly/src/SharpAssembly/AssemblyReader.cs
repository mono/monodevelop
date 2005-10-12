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
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.IO;
using MonoDevelop.SharpAssembly.Metadata.Rows;
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.PE;

namespace MonoDevelop.SharpAssembly.Assembly {
	
	public class AssemblyReader
	{
		PEFileHeader   header;
		CLIHeader      cliHeader;
		SectionTable[] sections;
		MetadataTable  metadataTable = new MetadataTable();
		
		byte[] stringHeap;
		byte[] userStringHeap;
		byte[] guidHeap;
		byte[] blobHeap;
		byte[] rawSectionData;
		
		string filename;
		
		public PEFileHeader PEHeader {
			get {
				return header;
			}
		}
		
		public CLIHeader CliHeader {
			get {
				return cliHeader;
			}
		}
		
		public string FileName {
			get {
				return filename;
			}
		}
		
		public MetadataTable MetadataTable {
			get {
				return metadataTable;
			}
		}
		
		public byte[] StringHeap {
			get {
				return stringHeap;
			}
		}
		
		public byte[] UserStringHeap {
			get {
				return userStringHeap;
			}
		}
		
		public byte[] GuidHeap {
			get {
				return guidHeap;
			}
		}
		
		public byte[] BlobHeap {
			get {
				return blobHeap;
			}
		}
		
		public byte[] RawSectionData {
			get {
				return rawSectionData;
			}
		}
		
		public static int GetCompressedInt(byte[] heap, ref uint index)
		{
			if (index < 0 || index >= heap.Length) {
				return -1;
			}
			int first = heap[index++];
			switch (first & 0xC0) {
				case 0xC0:
					first &= ~0xC0;
					return first << 24 | heap[index++] << 16 | heap[index++] << 8 | heap[index++];
				case 0x80:
					first &= ~0x80;
					return first << 8 | heap[index++];
				default:
					return first;
			}
		}
		
		public int LoadBlob(ref uint index)
		{
			return GetCompressedInt(blobHeap, ref index);
		}
		
		public byte[] GetBlobFromHeap(uint index)
		{
			if (index < 0 || index >= blobHeap.Length) {
				return new byte[0];
			}
			int length = LoadBlob(ref index);
			
			byte[] dest = new byte[length];
			Array.Copy(blobHeap, index, dest, 0, length);
			
			return dest;
		}

		public string GetUserStringFromHeap(uint index)
		{
			if (index < 0 || index >= userStringHeap.Length) {
				return "";
			}
			
			int length = GetCompressedInt(userStringHeap, ref index);

			return System.Text.Encoding.Unicode.GetString(userStringHeap, (int)index, length);
		}
		
		public string GetStringFromHeap(uint index)
		{
			if (index < 0 || index >= stringHeap.Length) {
				return "";
			}
			
			uint endIndex = index;
			while (endIndex < stringHeap.Length && stringHeap[endIndex] != 0) {
				++endIndex;
			}
			
			return System.Text.Encoding.UTF8.GetString(stringHeap, (int)index, (int)(endIndex - index));
		}
		
		public uint LookupRVA(uint address)
		{
			foreach (SectionTable section in sections) {
				if (section.VirtualAddress <= address && address <= section.VirtualAddress + section.VirtualSize) {
					return section.PointerToRawData + address - section.VirtualAddress;
				}
			}
			return 0;
		}
		
		public Stream OpenStream(uint rva)
		{
			uint offset = LookupRVA(rva);
			MemoryStream ms = new MemoryStream(rawSectionData);
			ms.Seek(offset, SeekOrigin.Begin);
			return ms;
		}
		
		public MethodBody LoadMethodBody(uint rva)
		{
			BinaryReader binaryReader = new BinaryReader(OpenStream(rva));
			MethodBody body = new MethodBody();
			body.Load(binaryReader);
			binaryReader.Close();
			return body;
		}
		
		public void Load(string fileName)
		{
			Stream fs = System.IO.File.OpenRead(fileName);
			fs.Seek(128, SeekOrigin.Begin);
			
			filename = fileName;
			
			BinaryReader binaryReader = new BinaryReader(fs);
			
			header = new PEFileHeader();
			header.LoadFrom(binaryReader);
			
			sections = new SectionTable[header.NumberOfSections];
			for (int i = 0; i < header.NumberOfSections; ++i) {
				sections[i] = new SectionTable();
				sections[i].LoadFrom(binaryReader);
			}
			
			uint rawDataSize = 0;
			for (int i = 0; i < header.NumberOfSections; ++i) {
				rawDataSize += sections[i].SizeOfRawData;
			}
			
			// read all sections to a memory buffer and relocate all pointer in the sections
			// to raw data indices in the buffer
			rawSectionData = new byte[rawDataSize];
			int curOffset = 0;
			for (int i = 0; i < header.NumberOfSections; ++i) {
				fs.Seek((int)sections[i].PointerToRawData, SeekOrigin.Begin);
				fs.Read(rawSectionData, curOffset, (int)sections[i].SizeOfRawData);
				sections[i].PointerToRawData = (uint)curOffset;
				curOffset += (int)sections[i].SizeOfRawData;
			}
			binaryReader.Close();
			fs.Close();
			
			fs           = new MemoryStream(rawSectionData);
			binaryReader = new BinaryReader(fs);
			
			uint cliHeaderPos = LookupRVA(header.DataDirectories.CliHeader);
			fs.Seek((int)cliHeaderPos, SeekOrigin.Begin);
			cliHeader = new CLIHeader();
			cliHeader.LoadFrom(binaryReader);
			
			uint metaDataPos = LookupRVA(cliHeader.MetaData);
			fs.Seek((int)metaDataPos, SeekOrigin.Begin);
			AssemblyMetadata met = new AssemblyMetadata();
			met.LoadFrom(binaryReader);
			
			foreach (StreamHeader streamHeader in met.StreamHeaders) {
				uint offset = LookupRVA(cliHeader.MetaData + streamHeader.Offset);
				fs.Seek((int)offset, SeekOrigin.Begin);
				switch (streamHeader.Name) {
					case "#~":
						metadataTable.LoadFrom(binaryReader);
						break;
					case "#Strings":
						stringHeap = new byte[streamHeader.Size];
						fs.Read(stringHeap, 0, stringHeap.Length);
						break;
					case "#US":
						userStringHeap = new byte[streamHeader.Size];
						fs.Read(userStringHeap, 0, userStringHeap.Length);
						break;
					case "#GUID":
						guidHeap = new byte[streamHeader.Size];
						fs.Read(guidHeap, 0, guidHeap.Length);
						break;
					case "#Blob":
						blobHeap = new byte[streamHeader.Size];
						fs.Read(blobHeap, 0, blobHeap.Length);
						break;
				}
			}
			

		}
		
		public int GetCodedIndexTable(CodedIndex index, ref uint val)
		{
			int bits = 0;
			
			switch (index) {
				case CodedIndex.HasConstant:
				case CodedIndex.TypeDefOrRef:
				case CodedIndex.HasDeclSecurity:
				case CodedIndex.Implementation:
				case CodedIndex.ResolutionScope:
					bits = 2;
					break;
				case CodedIndex.HasCustomAttribute:
					bits = 5;
					break;
				case CodedIndex.HasFieldMarshall:
				case CodedIndex.HasSemantics:
				case CodedIndex.MethodDefOrRef:
				case CodedIndex.MemberForwarded:
					bits = 1;
					break;
				case CodedIndex.MemberRefParent:
				case CodedIndex.CustomAttributeType:
					bits = 3;
					break;
			}
			
			uint origval = val;
			val = origval >> bits;
			
			return (int)(origval & ((int)Math.Pow(2, bits) - 1));
		}

	}
}
