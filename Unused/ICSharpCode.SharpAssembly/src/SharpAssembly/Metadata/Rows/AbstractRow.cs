// AbstractRow.cs
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
	
	public abstract class AbstractRow
	{
		protected BinaryReader  binaryReader;
		protected MetadataTable metadataTable;
		
		public BinaryReader BinaryReader {
			get {
				return binaryReader;
			}
			set {
				binaryReader = value;
			}
		}
		
		public MetadataTable MetadataTable {
			get {
				return metadataTable;
			}
			set {
				metadataTable = value;
			}
		}
		
		protected bool BaseIsFlagSet(uint flags, uint flag, uint flag_mask)
		{
			return ((flags & flag_mask) == flag);
		}
		
		protected bool BaseIsFlagSet(uint flags, uint flag)
		{
			return ((flags & flag) == flag);
		}
		
		protected uint ReadCodedIndex(CodedIndex codedIndex)
		{
			uint number = 0;
			int bits   = 0;
			switch (codedIndex) {
				case CodedIndex.TypeDefOrRef:
					number = metadataTable.GetMaxRowCount(TypeDef.TABLE_ID, TypeRef.TABLE_ID, TypeSpec.TABLE_ID);
					bits = 2;
					break;
				case CodedIndex.HasConstant:
					number = metadataTable.GetMaxRowCount(Field.TABLE_ID, Param.TABLE_ID, Property.TABLE_ID);
					bits = 2;
					break;
				case CodedIndex.HasCustomAttribute:
					number = metadataTable.GetMaxRowCount(Method.TABLE_ID, Field.TABLE_ID, TypeRef.TABLE_ID,
					                                   TypeDef.TABLE_ID, Param.TABLE_ID, InterfaceImpl.TABLE_ID,
					                                   MemberRef.TABLE_ID, Module.TABLE_ID, DeclSecurity.TABLE_ID,
					                                   Property.TABLE_ID, Event.TABLE_ID, StandAloneSig.TABLE_ID,
					                                   ModuleRef.TABLE_ID, TypeSpec.TABLE_ID, Assembly.TABLE_ID,
					                                   AssemblyRef.TABLE_ID, File.TABLE_ID, ExportedType.TABLE_ID, 
					                                   ManifestResource.TABLE_ID);
					bits = 5;
					break;
				case CodedIndex.HasFieldMarshall:
					number = metadataTable.GetMaxRowCount(Field.TABLE_ID, Param.TABLE_ID);
					bits = 1;
					break;
				case CodedIndex.HasDeclSecurity:
					number = metadataTable.GetMaxRowCount(TypeDef.TABLE_ID, Method.TABLE_ID, Assembly.TABLE_ID);
					bits = 2;
					break;
				case CodedIndex.MemberRefParent:
					number = metadataTable.GetMaxRowCount(TypeDef.TABLE_ID, TypeRef.TABLE_ID, ModuleRef.TABLE_ID, Method.TABLE_ID, TypeSpec.TABLE_ID);
					bits = 3;
					break;
				case CodedIndex.HasSemantics:
					number = metadataTable.GetMaxRowCount(Event.TABLE_ID, Property.TABLE_ID);
					bits = 1;
					break;
				case CodedIndex.MethodDefOrRef:
					number = metadataTable.GetMaxRowCount(Method.TABLE_ID, MemberRef.TABLE_ID);
					bits = 1;
					break;
				case CodedIndex.MemberForwarded:
					number = metadataTable.GetMaxRowCount(Field.TABLE_ID, Method.TABLE_ID);
					bits = 1;
					break;
				case CodedIndex.Implementation:
					number = metadataTable.GetMaxRowCount(File.TABLE_ID, AssemblyRef.TABLE_ID, ExportedType.TABLE_ID);
					bits = 2;
					break;
				case CodedIndex.CustomAttributeType:
					//number = metadataTable.GetMaxRowCount(TypeRef.TABLE_ID, TypeDef.TABLE_ID, Method.TABLE_ID, MemberRef.TABLE_ID/* TODO : , String ? */);
					number = metadataTable.GetMaxRowCount(Method.TABLE_ID, MemberRef.TABLE_ID);
					bits = 3;
					break;
				case CodedIndex.ResolutionScope:
					number = metadataTable.GetMaxRowCount(Module.TABLE_ID, ModuleRef.TABLE_ID, AssemblyRef.TABLE_ID, TypeRef.TABLE_ID);
					bits = 2;
					break;
			}
			if (number > 1 << (16 - bits)) {
				return binaryReader.ReadUInt32();
			}
			return binaryReader.ReadUInt16();
		}
		
		protected uint ReadSimpleIndex(int tableID)
		{
			uint rowCount = metadataTable.GetRowCount(tableID);
			if (rowCount >= (1 << 16)) {
				return binaryReader.ReadUInt32();
			}
			return binaryReader.ReadUInt16();
		}
		
		protected uint LoadStringIndex()
		{
			if (metadataTable.FourByteStringIndices) {
				return binaryReader.ReadUInt32();
			} 
			return binaryReader.ReadUInt16();
		}
		
		protected uint LoadBlobIndex()
		{
			if (metadataTable.FourByteBlobIndices) {
				return binaryReader.ReadUInt32();
			} 
			return binaryReader.ReadUInt16();
		}
		
		protected uint LoadGUIDIndex()
		{
			if (metadataTable.FourByteGUIDIndices) {
				return binaryReader.ReadUInt32();
			} 
			return binaryReader.ReadUInt16();
		}
		
		public abstract void LoadRow();
	}
}
