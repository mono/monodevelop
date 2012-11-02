// 
// AbstractRow.cs
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
	abstract class AbstractRow
	{
		protected bool BaseIsFlagSet (uint flags, uint flag, uint flag_mask)
		{
			return ((flags & flag_mask) == flag);
		}
		
		protected bool BaseIsFlagSet (uint flags, uint flag)
		{
			return ((flags & flag) == flag);
		}
		
		protected uint ReadCodedIndex (MetadataTable metadataTable, BinaryReader binaryReader, CodedIndex codedIndex)
		{
			uint number = 0;
			int bits = 0;
			switch (codedIndex) {
			case CodedIndex.TypeDefOrRef:
				number = metadataTable.GetMaxRowCount (TypeDef.TABLE_ID, TypeRef.TABLE_ID, TypeSpec.TABLE_ID);
				bits = 2; 
				break;
			case CodedIndex.HasConstant:
				number = metadataTable.GetMaxRowCount (Field.TABLE_ID, Param.TABLE_ID, Property.TABLE_ID);
				bits = 2;
				break;
			case CodedIndex.HasCustomAttribute:
				number = metadataTable.GetMaxRowCount (Method.TABLE_ID, Field.TABLE_ID, TypeRef.TABLE_ID,
					                                   TypeDef.TABLE_ID, Param.TABLE_ID, InterfaceImpl.TABLE_ID,
					                                   MemberRef.TABLE_ID, Module.TABLE_ID, DeclSecurity.TABLE_ID,
					                                   Property.TABLE_ID, Event.TABLE_ID, StandAloneSig.TABLE_ID,
					                                   ModuleRef.TABLE_ID, TypeSpec.TABLE_ID, Assembly.TABLE_ID,
					                                   AssemblyRef.TABLE_ID, File.TABLE_ID, ExportedType.TABLE_ID, 
					                                   ManifestResource.TABLE_ID);
				bits = 5;
				break;
			case CodedIndex.HasFieldMarshall:
				number = metadataTable.GetMaxRowCount (Field.TABLE_ID, Param.TABLE_ID);
				bits = 1;
				break;
			case CodedIndex.HasDeclSecurity:
				number = metadataTable.GetMaxRowCount (TypeDef.TABLE_ID, Method.TABLE_ID, Assembly.TABLE_ID);
				bits = 2;
				break;
			case CodedIndex.MemberRefParent:
				number = metadataTable.GetMaxRowCount (TypeDef.TABLE_ID, TypeRef.TABLE_ID, ModuleRef.TABLE_ID, Method.TABLE_ID, TypeSpec.TABLE_ID);
				bits = 3;
				break;
			case CodedIndex.HasSemantics:
				number = metadataTable.GetMaxRowCount (Event.TABLE_ID, Property.TABLE_ID);
				bits = 1;
				break;
			case CodedIndex.MethodDefOrRef:
				number = metadataTable.GetMaxRowCount (Method.TABLE_ID, MemberRef.TABLE_ID);
				bits = 1;
				break;
			case CodedIndex.MemberForwarded:
				number = metadataTable.GetMaxRowCount (Field.TABLE_ID, Method.TABLE_ID);
				bits = 1;
				break;
			case CodedIndex.Implementation:
				number = metadataTable.GetMaxRowCount (File.TABLE_ID, AssemblyRef.TABLE_ID, ExportedType.TABLE_ID);
				bits = 2;
				break;
			case CodedIndex.CustomAttributeType:
					//number = metadataTable.GetMaxRowCount(TypeRef.TABLE_ID, TypeDef.TABLE_ID, Method.TABLE_ID, MemberRef.TABLE_ID/* TODO : , String ? */);
				number = metadataTable.GetMaxRowCount (Method.TABLE_ID, MemberRef.TABLE_ID);
				bits = 3;
				break;
			case CodedIndex.ResolutionScope:
				number = metadataTable.GetMaxRowCount (Module.TABLE_ID, ModuleRef.TABLE_ID, AssemblyRef.TABLE_ID, TypeRef.TABLE_ID);
				bits = 2;
				break;
			}
			if (number > 1 << (16 - bits)) {
				return binaryReader.ReadUInt32 ();
			}
			return binaryReader.ReadUInt16 ();
		}
		
		protected uint ReadSimpleIndex (MetadataTable metadataTable, BinaryReader binaryReader, int tableID)
		{
			uint rowCount = metadataTable.GetRowCount (tableID);
			if (rowCount >= (1 << 16)) {
				return binaryReader.ReadUInt32 ();
			}
			return binaryReader.ReadUInt16 ();
		}
		
		protected uint LoadStringIndex (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			if (metadataTable.FourByteStringIndices) {
				return binaryReader.ReadUInt32 ();
			} 
			return binaryReader.ReadUInt16 ();
		}
		
		protected uint LoadBlobIndex (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			if (metadataTable.FourByteBlobIndices) {
				return binaryReader.ReadUInt32 ();
			} 
			return binaryReader.ReadUInt16 ();
		}
		
		protected uint LoadGUIDIndex (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			if (metadataTable.FourByteGUIDIndices) {
				return binaryReader.ReadUInt32 ();
			} 
			return binaryReader.ReadUInt16 ();
		}
		
		public abstract void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader);
	}

	class Assembly : AbstractRow
	{
		public static readonly int TABLE_ID = 0x20;
		uint  hashAlgID;
		ushort majorVersion;
		ushort minorVersion;
		ushort buildNumber;
		ushort revisionNumber;
		uint   flags;
		uint publicKey; // index into the BLOB heap
		uint name;      // index into the string heap
		uint culture;   // index into the string heap
		
		public uint HashAlgID {
			get {
				return hashAlgID;
			}
			set {
				hashAlgID = value;
			}
		}

		public ushort MajorVersion {
			get {
				return majorVersion;
			}
			set {
				majorVersion = value;
			}
		}

		public ushort MinorVersion {
			get {
				return minorVersion;
			}
			set {
				minorVersion = value;
			}
		}

		public ushort BuildNumber {
			get {
				return buildNumber;
			}
			set {
				buildNumber = value;
			}
		}

		public ushort RevisionNumber {
			get {
				return revisionNumber;
			}
			set {
				revisionNumber = value;
			}
		}

		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		public uint PublicKey {
			get {
				return publicKey;
			}
			set {
				publicKey = value;
			}
		}

		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint Culture {
			get {
				return culture;
			}
			set {
				culture = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			hashAlgID = binaryReader.ReadUInt32 ();
			majorVersion = binaryReader.ReadUInt16 ();
			minorVersion = binaryReader.ReadUInt16 ();
			buildNumber = binaryReader.ReadUInt16 ();
			revisionNumber = binaryReader.ReadUInt16 ();
			flags = binaryReader.ReadUInt32 ();
			publicKey = LoadBlobIndex (metadataTable, binaryReader);
			name = LoadStringIndex (metadataTable, binaryReader);
			culture = LoadStringIndex (metadataTable, binaryReader);
		}
	}

	class AssemblyOS : AbstractRow
	{
		public static readonly int TABLE_ID = 0x22;
		uint osPlatformID;
		uint osMajorVersion;
		uint osMinorVersion;
		
		public uint OSPlatformID {
			get {
				return osPlatformID;
			}
			set {
				osPlatformID = value;
			}
		}

		public uint OSMajorVersion {
			get {
				return osMajorVersion;
			}
			set {
				osMajorVersion = value;
			}
		}

		public uint OSMinorVersion {
			get {
				return osMinorVersion;
			}
			set {
				osMinorVersion = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			osPlatformID = binaryReader.ReadUInt32 ();
			osMajorVersion = binaryReader.ReadUInt32 ();
			osMinorVersion = binaryReader.ReadUInt32 ();
		}
	}

	class AssemblyProcessor : AbstractRow
	{
		public static readonly int TABLE_ID = 0x21;
		uint processor;
		
		public uint Processor {
			get {
				return processor;
			}
			set {
				processor = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			processor = binaryReader.ReadUInt32 ();
		}
	}

	class AssemblyRef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x23;
		ushort major;
		ushort minor;
		ushort build;
		ushort revision;
		uint flags;
		uint publicKeyOrToken; // index into Blob heap
		uint name;    // index into String heap
		uint culture; // index into String heap
		uint hashValue; // index into Blob heap
		
		public ushort Major {
			get {
				return major;
			}
			set {
				major = value;
			}
		}

		public ushort Minor {
			get {
				return minor;
			}
			set {
				minor = value;
			}
		}
		
		public ushort Build {
			get {
				return build;
			}
			set {
				build = value;
			}
		}
		
		public ushort Revision {
			get {
				return revision;
			}
			set {
				revision = value;
			}
		}

		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		public uint PublicKeyOrToken {
			get {
				return publicKeyOrToken;
			}
			set {
				publicKeyOrToken = value;
			}
		}

		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint Culture {
			get {
				return culture;
			}
			set {
				culture = value;
			}
		}

		public uint HashValue {
			get {
				return hashValue;
			}
			set {
				hashValue = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			major = binaryReader.ReadUInt16 ();
			minor = binaryReader.ReadUInt16 ();
			build = binaryReader.ReadUInt16 ();
			revision = binaryReader.ReadUInt16 ();
			flags = binaryReader.ReadUInt32 ();
			publicKeyOrToken = LoadBlobIndex (metadataTable, binaryReader);
			name = LoadStringIndex (metadataTable, binaryReader);
			culture = LoadStringIndex (metadataTable, binaryReader);
			hashValue = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class AssemblyRefOS : AbstractRow
	{
		public static readonly int TABLE_ID = 0x25;
		uint osPlatformID;
		uint osMajorVersion;
		uint osMinorVersion;
		uint assemblyRefIndex; // index into the AssemblyRef table
		
		public uint OSPlatformID {
			get {
				return osPlatformID;
			}
			set {
				osPlatformID = value;
			}
		}

		public uint OSMajorVersion {
			get {
				return osMajorVersion;
			}
			set {
				osMajorVersion = value;
			}
		}

		public uint OSMinorVersion {
			get {
				return osMinorVersion;
			}
			set {
				osMinorVersion = value;
			}
		}

		public uint AssemblyRefIndex {
			get {
				return assemblyRefIndex;
			}
			set {
				assemblyRefIndex = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			osPlatformID = binaryReader.ReadUInt32 ();
			osMajorVersion = binaryReader.ReadUInt32 ();
			osMinorVersion = binaryReader.ReadUInt32 ();
			assemblyRefIndex = ReadSimpleIndex (metadataTable, binaryReader, AssemblyRef.TABLE_ID);
		}
	}

	class AssemblyRefProcessor : AbstractRow
	{
		public static readonly int TABLE_ID = 0x24;
		uint processor;
		uint assemblyRefIndex; // index into the AssemblyRef table
		
		public uint Processor {
			get {
				return processor;
			}
			set {
				processor = value;
			}
		}

		public uint AssemblyRefIndex {
			get {
				return assemblyRefIndex;
			}
			set {
				assemblyRefIndex = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			processor = binaryReader.ReadUInt32 ();
			assemblyRefIndex = ReadSimpleIndex (metadataTable, binaryReader, AssemblyRef.TABLE_ID);
		}
	}

	class ClassLayout : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0F;
		ushort packingSize;
		uint   classSize;
		uint   parent; // index into TypeDef table
		
		public ushort PackingSize {
			get {
				return packingSize;
			}
			set {
				packingSize = value;
			}
		}

		public uint ClassSize {
			get {
				return classSize;
			}
			set {
				classSize = value;
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
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			packingSize = binaryReader.ReadUInt16 ();
			classSize = binaryReader.ReadUInt32 ();
			parent = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
		}
	}

	class Constant : AbstractRow
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
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			type = binaryReader.ReadByte ();
			byte paddingZero = binaryReader.ReadByte ();
			if (paddingZero != 0) {
				Console.WriteLine("padding zero != 0");
			}
			parent = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.HasConstant);
			val = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class CustomAttribute : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0C;
		uint parent; // index into any metadata table, except the CustomAttribute table itself; more precisely, a HasCustomAttribute coded index
		uint type;   // index into the Method or MemberRef table; more precisely, a CustomAttributeType coded index
		uint val;    // index into Blob heap
		
		public uint Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}
		
		public uint Type {
			get {
				return type;
			}
			set {
				type = value;
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
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			parent = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.HasCustomAttribute);
			type = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.CustomAttributeType);
			val = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class DeclSecurity : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0E;
		ushort action;
		uint   parent; // index into the TypeDef, Method or Assembly table; more precisely, a HasDeclSecurity coded index
		uint   permissionSet; // index into Blob heap
		
		public ushort Action {
			get {
				return action;
			}
			set {
				action = value;
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

		public uint PermissionSet {
			get {
				return permissionSet;
			}
			set {
				permissionSet = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			action = binaryReader.ReadUInt16 ();
			parent = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.HasDeclSecurity);
			permissionSet = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class ENCLog : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1E;
		uint token;
		uint funcCode;
		
		public uint Token {
			get {
				return token;
			}
			set {
				token = value;
			}
		}

		public uint FuncCode {
			get {
				return funcCode;
			}
			set {
				funcCode = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			token = binaryReader.ReadUInt32 ();
			funcCode = binaryReader.ReadUInt32 ();
		}
	}

	class ENCMap : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1F;
		uint token;
		
		public uint Token {
			get {
				return token;
			}
			set {
				token = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			token = binaryReader.ReadUInt32 ();
		}
	}

	class Event : AbstractRow
	{
		public static readonly int TABLE_ID = 0x14;
		public static readonly ushort FLAG_SPECIALNAME = 0x0200;
		public static readonly ushort FLAG_RTSPECIALNAME = 0x0400;
		ushort eventFlags;
		uint   name;      // index into String heap
		uint   eventType; // index into TypeDef, TypeRef or TypeSpec tables; more precisely, a TypeDefOrRef coded index
		
		public ushort EventFlags {
			get {
				return eventFlags;
			}
			set {
				eventFlags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint EventType {
			get {
				return eventType;
			}
			set {
				eventType = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			eventFlags = binaryReader.ReadUInt16 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			eventType = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.TypeDefOrRef);
		}
	}

	class EventMap : AbstractRow
	{
		public static readonly int TABLE_ID = 0x12;
		uint   parent;    // index into the TypeDef table
		uint   eventList; // index into Event table
		
		public uint Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public uint EventList {
			get {
				return eventList;
			}
			set {
				eventList = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			parent = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
			eventList = ReadSimpleIndex (metadataTable, binaryReader, Event.TABLE_ID);
		}
	}

	class EventPtr : AbstractRow
	{
		public static readonly int TABLE_ID = 19;
		uint eventPtr;
		
		public uint Event {
			get {
				return eventPtr;
			}
			set {
				eventPtr = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			eventPtr = ReadSimpleIndex (metadataTable, binaryReader, CustomAssemblyReader.Event.TABLE_ID);
		}
	}

	class ExportedType : AbstractRow
	{
		public static readonly int TABLE_ID = 0x27;
		uint flags;
		uint typeDefId; // 4 byte index into a TypeDef table of another module in this Assembly
		uint typeName;  // index into the String heap
		uint typeNamespace; // index into the String heap
		uint implementation; // index see 21.14
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		public uint TypeDefId {
			get {
				return typeDefId;
			}
			set {
				typeDefId = value;
			}
		}

		public uint TypeName {
			get {
				return typeName;
			}
			set {
				typeName = value;
			}
		}

		public uint TypeNamespace {
			get {
				return typeNamespace;
			}
			set {
				typeNamespace = value;
			}
		}

		public uint Implementation {
			get {
				return implementation;
			}
			set {
				implementation = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			flags = binaryReader.ReadUInt32 ();
			typeDefId = binaryReader.ReadUInt32 ();
			typeName = LoadStringIndex (metadataTable, binaryReader);
			typeNamespace = LoadStringIndex (metadataTable, binaryReader);
			
			// todo 32 bit indices ?
			implementation = binaryReader.ReadUInt16 ();
		}
	}

	class Field : AbstractRow
	{
		public static readonly int TABLE_ID = 0x04;
		public static readonly ushort FLAG_FIELDACCESSMASK = 0x0007;
		public static readonly ushort FLAG_COMPILERCONTROLLED = 0x0000;
		public static readonly ushort FLAG_PRIVATE = 0x0001;
		public static readonly ushort FLAG_FAMANDASSEM = 0x0002;
		public static readonly ushort FLAG_ASSEMBLY = 0x0003;
		public static readonly ushort FLAG_FAMILY = 0x0004;
		public static readonly ushort FLAG_FAMORASSEM = 0x0005;
		public static readonly ushort FLAG_PUBLIC = 0x0006;
		public static readonly ushort FLAG_STATIC = 0x0010;
		public static readonly ushort FLAG_INITONLY = 0x0020;
		public static readonly ushort FLAG_LITERAL = 0x0040;
		public static readonly ushort FLAG_NOTSERIALIZED = 0x0080;
		public static readonly ushort FLAG_SPECIALNAME = 0x0200;
		public static readonly ushort FLAG_PINVOKEIMPL = 0x2000;
		public static readonly ushort FLAG_RTSPECIALNAME = 0x0400;
		public static readonly ushort FLAG_HASFIELDMARSHAL = 0x1000;
		public static readonly ushort FLAG_HASDEFAULT = 0x8000;
		public static readonly ushort FLAG_HASFIELDRVA = 0x0100;
		ushort flags;
		uint   name;      // index into String heap
		uint   signature; // index into Blob heap
		
		public ushort Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		public bool IsFlagSet (uint flag)
		{
			return base.BaseIsFlagSet (this.flags, flag);
		}
		
		public bool IsMaskedFlagSet (uint flag, uint flag_mask)
		{
			return base.BaseIsFlagSet (this.flags, flag, flag_mask);
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			flags = binaryReader.ReadUInt16 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			signature = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class FieldLayout : AbstractRow
	{
		public static readonly int TABLE_ID = 0x10;
		uint offset;
		uint field; // index into the field table
		
		public uint Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}

		public uint FieldIndex {
			get {
				return field;
			}
			set {
				field = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			offset = binaryReader.ReadUInt32 ();
			field = ReadSimpleIndex (metadataTable, binaryReader, Field.TABLE_ID);
		}
	}

	class FieldMarshal : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0D;
		uint parent;     // index into Field or Param table; more precisely, a HasFieldMarshal coded index
		uint nativeType; // index into the Blob heap
		
		public uint Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
			
		}

		public uint NativeType {
			get {
				return nativeType;
			}
			set {
				nativeType = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			parent = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.HasFieldMarshall);
			nativeType = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class FieldPtr : AbstractRow
	{
		public static readonly int TABLE_ID = 0x03;
		uint field;
		
		public uint Field {
			get {
				return field;
			}
			set {
				field = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			field = ReadSimpleIndex (metadataTable, binaryReader, CustomAssemblyReader.Field.TABLE_ID);
		}
	}

	class FieldRVA : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1D;
		uint rva;
		uint field; // index into Field table
		
		public uint RVA {
			get {
				return rva;
			}
			set {
				rva = value;
			}
		}
		
		public uint FieldIndex {
			get {
				return field;
			}
			set {
				field = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			rva = binaryReader.ReadUInt32 ();
			field = ReadSimpleIndex (metadataTable, binaryReader, Field.TABLE_ID);
		}
	}

	class File : AbstractRow
	{
		public static readonly int TABLE_ID = 0x26;
		public static readonly uint FLAG_CONTAINSMETADATA = 0x0000;
		public static readonly uint FLAG_CONTAINSNOMETADATA = 0x0001;
		uint flags;
		uint name; // index into String heap
		uint hashValue; // index into Blob heap
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint HashValue {
			get {
				return hashValue;
			}
			set {
				hashValue = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			flags = binaryReader.ReadUInt32 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			hashValue = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class ImplMap : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1C;
		public static readonly ushort FLAG_NOMANGLE = 0x0001;
		public static readonly ushort FLAG_CHARSETMASK = 0x0006;
		public static readonly ushort FLAG_CHARSETNOTSPEC = 0x0000;
		public static readonly ushort FLAG_CHARSETANSI = 0x0002;
		public static readonly ushort FLAG_CHARSETUNICODE = 0x0004;
		public static readonly ushort FLAG_CHARSETAUTO = 0x0006;
		public static readonly ushort FLAG_SUPPORTSLASTERROR = 0x0040;
		public static readonly ushort FLAG_CALLCONVMASK = 0x0700;
		public static readonly ushort FLAG_CALLCONVWINAPI = 0x0100;
		public static readonly ushort FLAG_CALLCONVCDECL = 0x0200;
		public static readonly ushort FLAG_CALLCONVSTDCALL = 0x0300;
		public static readonly ushort FLAG_CALLCONVTHISCALL = 0x0400;
		public static readonly ushort FLAG_CALLCONVFASTCALL = 0x0500;
		ushort mappingFlags;
		uint   memberForwarded; // index into the Field or Method table; more precisely, a MemberForwarded coded index.
		uint   importName; // index into the String heap
		uint   importScope; // index into the ModuleRef table
		
		public ushort MappingFlags {
			get {
				return mappingFlags;
			}
			set {
				mappingFlags = value;
			}
		}
		
		public uint MemberForwarded {
			get {
				return memberForwarded;
			}
			set {
				memberForwarded = value;
			}
		}
		
		public uint ImportName {
			get {
				return importName;
			}
			set {
				importName = value;
			}
		}
		
		public uint ImportScope {
			get {
				return importScope;
			}
			set {
				importScope = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			mappingFlags = binaryReader.ReadUInt16 ();
			memberForwarded = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.MemberForwarded);
			importName = LoadStringIndex (metadataTable, binaryReader);
			importScope = ReadSimpleIndex (metadataTable, binaryReader, ModuleRef.TABLE_ID);
		}
	}

	class InterfaceImpl : AbstractRow
	{
		public static readonly int TABLE_ID = 0x09;
		uint myClass; // index into the TypeDef table
		uint myInterface; // index into the TypeDef, TypeRef or TypeSpec table; more precisely, a TypeDefOrRef coded index
		
		public uint Class {
			get {
				return myClass;
			}
			set {
				myClass = value;
			}
		}
		
		public uint Interface {
			get {
				return myInterface;
			}
			set {
				myInterface = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			myClass = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
			myInterface = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.TypeDefOrRef);
		}
	}

	class ManifestResource : AbstractRow
	{
		public static readonly int TABLE_ID = 0x28;
		public static readonly uint FLAG_VISIBILITYMASK = 0x0007;
		public static readonly uint FLAG_PUBLIC = 0x0001;
		public static readonly uint FLAG_PRIVATE = 0x0002;
		uint offset;
		uint flags;
		uint name; // index into String heap
		uint implementation; // index into File table, or AssemblyRef table, or  null; more precisely, an Implementation coded index
		long streamOffset = -1;
		long streamLength = -1;
		
		public uint Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public uint Implementation {
			get {
				return implementation;
			}
			set {
				implementation = value;
			}
		}

		void GetStreamBounds (AssemblyReader assembly, string fileName)
		{
			using (var fs = System.IO.File.OpenRead (fileName)) {
				var offset = assembly.LookupRVA (assembly.CliHeader.Resources) + Offset;
				fs.Seek (streamOffset, SeekOrigin.Begin);
				using (var reader = new BinaryReader (fs)) {
					streamLength = reader.ReadUInt32 ();
					streamOffset = offset + 4;
				}
			}
		}

		public Stream Open (AssemblyReader assembly, string fileName)
		{
			if (streamOffset == -1)
				GetStreamBounds (assembly, fileName);

			return new BoundStream (System.IO.File.OpenRead (fileName), streamOffset, streamOffset + streamLength, true);
		}

		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			offset = binaryReader.ReadUInt32 ();
			flags = binaryReader.ReadUInt32 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			implementation = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.Implementation);
		}
	}

	class MemberRef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x0A;
		uint myClass;    // index into the TypeRef, ModuleRef, Method, TypeSpec or TypeDef tables; more precisely, a MemberRefParent coded index
		uint name;      // index into String heap
		uint signature; // index into Blob heap
		
		public uint Class {
			get {
				return myClass;
			}
			set {
				myClass = value;
			}
		}

		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			myClass = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.MemberRefParent);
			name = LoadStringIndex (metadataTable, binaryReader);
			signature = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class Method : AbstractRow
	{
		public static readonly int TABLE_ID = 0x06;
		public static readonly ushort FLAG_MEMBERACCESSMASK = 0X0007;
		public static readonly ushort FLAG_COMPILERCONTROLLED = 0X0000;
		public static readonly ushort FLAG_PRIVATE = 0X0001;
		public static readonly ushort FLAG_FAMANDASSEM = 0X0002;
		public static readonly ushort FLAG_ASSEM = 0X0003;
		public static readonly ushort FLAG_FAMILY = 0X0004;
		public static readonly ushort FLAG_FAMORASSEM = 0X0005;
		public static readonly ushort FLAG_PUBLIC = 0X0006;
		public static readonly ushort FLAG_STATIC = 0X0010;
		public static readonly ushort FLAG_FINAL = 0X0020;
		public static readonly ushort FLAG_VIRTUAL = 0X0040;
		public static readonly ushort FLAG_HIDEBYSIG = 0X0080;
		public static readonly ushort FLAG_VTABLELAYOUTMASK = 0X0100;
		public static readonly ushort FLAG_REUSESLOT = 0X0000;
		public static readonly ushort FLAG_NEWSLOT = 0X0100;
		public static readonly ushort FLAG_ABSTRACT = 0X0400;
		public static readonly ushort FLAG_SPECIALNAME = 0X0800;
		public static readonly ushort FLAG_PINVOKEIMPL = 0X2000;
		public static readonly ushort FLAG_UNMANAGEDEXPORT = 0X0008;
		public static readonly ushort FLAG_RTSPECIALNAME = 0X1000;
		public static readonly ushort FLAG_HASSECURITY = 0X4000;
		public static readonly ushort FLAG_REQUIRESECOBJECT = 0X8000;
		public static readonly ushort IMPLFLAG_CODETYPEMASK = 0X0003;
		public static readonly ushort IMPLFLAG_IL = 0X0000;
		public static readonly ushort IMPLFLAG_NATIVE = 0X0001;
		public static readonly ushort IMPLFLAG_OPTIL = 0X0002;
		public static readonly ushort IMPLFLAG_RUNTIME = 0X0003;
		public static readonly ushort IMPLFLAG_MANAGEDMASK = 0X0004;
		public static readonly ushort IMPLFLAG_UNMANAGED = 0X0004;
		public static readonly ushort IMPLFLAG_MANAGED = 0X0000;
		public static readonly ushort IMPLFLAG_FORWARDREF = 0X0010;
		public static readonly ushort IMPLFLAG_PRESERVESIG = 0X0080;
		public static readonly ushort IMPLFLAG_INTERNALCALL = 0X1000;
		public static readonly ushort IMPLFLAG_SYNCHRONIZED = 0X0020;
		public static readonly ushort IMPLFLAG_NOINLINING = 0X0008;
		public static readonly ushort IMPLFLAG_MAXMETHODIMPLVAL = 0XFFFF;
		uint   rva;
		ushort implFlags;
		ushort flags;
		uint   name;      // index into String heap
		uint   signature; // index into Blob heap
		uint   paramList; // index into Param table
		
		public uint RVA {
			get {
				return rva;
			}
			set {
				rva = value;
			}
		}
		
		public ushort ImplFlags {
			get {
				return implFlags;
			}
			set {
				implFlags = value;
			}
		}
		
		public ushort Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		public uint ParamList {
			get {
				return paramList;
			}
			set {
				paramList = value;
			}
		}
		
		public bool IsFlagSet (uint flag)
		{
			return base.BaseIsFlagSet (this.flags, flag);
		}
		
		public bool IsMaskedFlagSet (uint flag, uint flag_mask)
		{
			return base.BaseIsFlagSet (this.flags, flag, flag_mask);
		}
		
		public bool IsImplFlagSet (uint flag)
		{
			return base.BaseIsFlagSet (this.implFlags, flag);
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			rva = binaryReader.ReadUInt32 ();
			implFlags = binaryReader.ReadUInt16 ();
			flags = binaryReader.ReadUInt16 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			signature = LoadBlobIndex (metadataTable, binaryReader);
			
			paramList = ReadSimpleIndex (metadataTable, binaryReader, Param.TABLE_ID);
		}
	}

	class MethodImpl : AbstractRow
	{
		public static readonly int TABLE_ID = 0x19;
		uint myClass;           // index into TypeDef table
		uint methodBody;        // index into Method or MemberRef table; more precisely, a MethodDefOrRef coded index
		uint methodDeclaration; // index into Method or MemberRef table; more precisely, a MethodDefOrRef coded index
		
		public uint MyClass {
			get {
				return myClass;
			}
			set {
				myClass = value;
			}
		}

		public uint MethodBody {
			get {
				return methodBody;
			}
			set {
				methodBody = value;
			}
		}

		public uint MethodDeclaration {
			get {
				return methodDeclaration;
			}
			set {
				methodDeclaration = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			myClass = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
			methodBody = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.MethodDefOrRef);
			methodDeclaration = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.MethodDefOrRef);
		}
	}

	class MethodPtr : AbstractRow
	{
		public static readonly int TABLE_ID = 0x05;
		uint method;
		
		public uint Method {
			get {
				return method;
			}
			set {
				method = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			method = ReadSimpleIndex (metadataTable, binaryReader, CustomAssemblyReader.Method.TABLE_ID);
		}
	}

	class MethodSemantics : AbstractRow
	{
		public static readonly int TABLE_ID = 0x18;
		public static readonly ushort SEM_SETTER = 0x0001;
		public static readonly ushort SEM_GETTER = 0x0002;
		public static readonly ushort SEM_OTHER = 0x0004;
		public static readonly ushort SEM_ADDON = 0x0008;
		public static readonly ushort SEM_REMOVEON = 0x0010;
		public static readonly ushort SEM_FIRE = 0x0020;
		ushort semantics;
		uint   method;      // index into the Method table
		uint   association; // index into the Event or Property table; more precisely, a HasSemantics coded index
		
		public ushort Semantics {
			get {
				return semantics;
			}
			set {
				semantics = value;
			}
		}
		
		public uint Method {
			get {
				return method;
			}
			set {
				method = value;
			}
		}
		
		public uint Association {
			get {
				return association;
			}
			set {
				association = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			semantics = binaryReader.ReadUInt16 ();
			method = ReadSimpleIndex (metadataTable, binaryReader, CustomAssemblyReader.Method.TABLE_ID);
			association = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.HasSemantics);
		}
	}

	class Module : AbstractRow
	{
		public static readonly int TABLE_ID = 0x00;
		ushort generation;
		uint   name;      // index into String heap
		uint   mvid;      // index into Guid heap
		uint   encid;     // index into Guid heap, reserved, shall be zero
		uint   encbaseid; // index into Guid heap, reserved, shall be zero
		
		public ushort Generation {
			get {
				return generation;
			}
			set {
				generation = value;
			}
		}

		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint Mvid {
			get {
				return mvid;
			}
			set {
				mvid = value;
			}
		}

		public uint Encid {
			get {
				return encid;
			}
			set {
				encid = value;
			}
		}

		public uint Encbaseid {
			get {
				return encbaseid;
			}
			set {
				encbaseid = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			generation = binaryReader.ReadUInt16 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			mvid = LoadGUIDIndex (metadataTable, binaryReader);
			encid = LoadGUIDIndex (metadataTable, binaryReader);
			encbaseid = LoadGUIDIndex (metadataTable, binaryReader);
		}
	}

	class ModuleRef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1A;
		uint name;      // index into String heap
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			name = LoadStringIndex (metadataTable, binaryReader);
		}
	}

	class NestedClass : AbstractRow
	{
		public static readonly int TABLE_ID = 0x29;
		uint nestedClass; // index into the TypeDef table
		uint enclosingClass; // index into the TypeDef table
		
		public uint NestedClassIndex {
			get {
				return nestedClass;
			}
			set {
				nestedClass = value;
			}
		}

		public uint EnclosingClass {
			get {
				return enclosingClass;
			}
			set {
				enclosingClass = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			nestedClass = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
			enclosingClass = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
		}
	}

	class Param : AbstractRow
	{
		public static readonly int    TABLE_ID = 0x08;
		public static readonly ushort FLAG_IN = 0x0001;
		public static readonly ushort FLAG_OUT = 0x0002;
		public static readonly ushort FLAG_OPTIONAL = 0x0004;
		public static readonly ushort FLAG_HASDEFAULT = 0x1000;
		public static readonly ushort FLAG_HASFIELDMARSHAL = 0x2000;
		public static readonly ushort FLAG_UNUSED = 0xcfe0;
		ushort flags;
		ushort sequence;
		uint   name; // index into String heap
		
		public ushort Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		
		public ushort Sequence {
			get {
				return sequence;
			}
			set {
				sequence = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public bool IsFlagSet (uint flag)
		{
			return base.BaseIsFlagSet (this.flags, flag);
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			flags = binaryReader.ReadUInt16 ();
			sequence = binaryReader.ReadUInt16 ();
			name = LoadStringIndex (metadataTable, binaryReader);
		}
	}

	class ParamPtr : AbstractRow
	{
		public static readonly int TABLE_ID = 0x07;
		uint param;
		
		public uint Param {
			get {
				return param;
			}
			set {
				param = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			param = ReadSimpleIndex (metadataTable, binaryReader, CustomAssemblyReader.Param.TABLE_ID);
		}
	}

	class Property : AbstractRow
	{
		public static readonly int TABLE_ID = 0x17;
		public static readonly ushort FLAG_SPECIALNAME = 0x0200;
		public static readonly ushort FLAG_RTSPECIALNAME = 0x0400;
		public static readonly ushort FLAG_HASDEFAULT = 0x1000;
		public static readonly ushort FLAG_UNUSED = 0xe9ff;
		ushort flags;
		uint   name; // index into String heap
		uint   type; // index into Blob heap
		
		public ushort Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public uint Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public bool IsFlagSet (uint flag)
		{
			return base.BaseIsFlagSet (this.flags, flag);
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			flags = binaryReader.ReadUInt16 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			type = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class PropertyMap : AbstractRow
	{
		public static readonly int TABLE_ID = 0x15;
		uint parent;       // index into the TypeDef table
		uint propertyList; // index into Property table
		
		public uint Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public uint PropertyList {
			get {
				return propertyList;
			}
			set {
				propertyList = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			parent = ReadSimpleIndex (metadataTable, binaryReader, TypeDef.TABLE_ID);
			propertyList = ReadSimpleIndex (metadataTable, binaryReader, Property.TABLE_ID);
		}
	}

	class PropertyPtr : AbstractRow
	{
		public static readonly int TABLE_ID = 22;
		uint property;
		
		public uint Property {
			get {
				return property;
			}
			set {
				property = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			property = ReadSimpleIndex (metadataTable, binaryReader, CustomAssemblyReader.Property.TABLE_ID);
		}
	}

	class StandAloneSig : AbstractRow
	{
		public static readonly int TABLE_ID = 0x11;
		uint signature; // index into the Blob heap
		
		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			signature = LoadBlobIndex (metadataTable, binaryReader);
		}
	}

	class TypeDef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x02;
		
		// Visibility attributes
		public static readonly uint FLAG_VISIBILITYMASK = 0x00000007;
		public static readonly uint FLAG_NOTPUBLIC = 0x00000000;
		public static readonly uint FLAG_PUBLIC = 0x00000001;
		public static readonly uint FLAG_NESTEDPUBLIC = 0x00000002;
		public static readonly uint FLAG_NESTEDPRIVATE = 0x00000003;
		public static readonly uint FLAG_NESTEDFAMILY = 0x00000004;
		public static readonly uint FLAG_NESTEDASSEMBLY = 0x00000005;
		public static readonly uint FLAG_NESTEDFAMANDASSEM = 0x00000006;
		public static readonly uint FLAG_NESTEDFAMORASSEM = 0x00000007;
		
		//Class layout attributes
		public static readonly uint FLAG_LAYOUTMASK = 0x00000018;
		public static readonly uint FLAG_AUTOLAYOUT = 0x00000000;
		public static readonly uint FLAG_SEQUENTIALLAYOUT = 0x00000008;
		public static readonly uint FLAG_EXPLICITLAYOUT = 0x00000010;
		
		//Class semantics attributes
		public static readonly uint FLAG_CLASSSEMANTICSMASK = 0x00000020;
		public static readonly uint FLAG_CLASS = 0x00000000;
		public static readonly uint FLAG_INTERFACE = 0x00000020;
		
		// Special semantics in addition to class semantics
		public static readonly uint FLAG_ABSTRACT = 0x00000080;
		public static readonly uint FLAG_SEALED = 0x00000100;
		public static readonly uint FLAG_SPECIALNAME = 0x00000400;
		
		// Implementation Attributes
		public static readonly uint FLAG_IMPORT = 0x00001000;
		public static readonly uint FLAG_SERIALIZABLE = 0x00002000;
		
		//String formatting Attributes
		public static readonly uint FLAG_STRINGFORMATMASK = 0x00030000;
		public static readonly uint FLAG_ANSICLASS = 0x00000000;
		public static readonly uint FLAG_UNICODECLASS = 0x00010000;
		public static readonly uint FLAG_AUTOCLASS = 0x00020000;
		
		//Class Initialization Attributes
		public static readonly uint FLAG_BEFOREFIELDINIT = 0x00100000;
		
		//Additional Flags
		public static readonly uint FLAG_RTSPECIALNAME = 0x00000800;
		public static readonly uint FLAG_HASSECURITY = 0x00040000;
		uint flags;
		uint name;
		uint nSpace;
		uint extends;    // index into TypeDef, TypeRef or TypeSpec table; more precisely, a TypeDefOrRef coded index
		uint fieldList;  // index into Field table; it marks the first of a continguous run of Fields owned by this Type
		uint methodList; // index into Method table; it marks the first of a continguous run of Methods owned by this Type
		
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public uint NSpace {
			get {
				return nSpace;
			}
			set {
				nSpace = value;
			}
		}

		public uint Extends {
			get {
				return extends;
			}
			set {
				extends = value;
			}
		}

		public uint FieldList {
			get {
				return fieldList;
			}
			set {
				fieldList = value;
			}
		}

		public uint MethodList {
			get {
				return methodList;
			}
			set {
				methodList = value;
			}
		}
		
		public bool IsFlagSet (uint flag)
		{
			return base.BaseIsFlagSet (this.flags, flag);
		}
		
		public bool IsMaskedFlagSet (uint flag, uint flag_mask)
		{
			return base.BaseIsFlagSet (this.flags, flag, flag_mask);
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			flags = binaryReader.ReadUInt32 ();
			name = LoadStringIndex (metadataTable, binaryReader);
			nSpace = LoadStringIndex (metadataTable, binaryReader);
			extends = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.TypeDefOrRef);
			fieldList = ReadSimpleIndex (metadataTable, binaryReader, Field.TABLE_ID);
			methodList = ReadSimpleIndex (metadataTable, binaryReader, Method.TABLE_ID);
		}
	}

	class TypeRef : AbstractRow
	{
		public static readonly int TABLE_ID = 0x01;
		uint resolutionScope; // index into Module, ModuleRef, AssemblyRef or TypeRef tables, or null; more precisely, a ResolutionScope coded index
		uint name;
		uint nspace;
		
		public uint ResolutionScope {
			get {
				return resolutionScope;
			}
			set {
				resolutionScope = value;
			}
		}
		
		public uint Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public uint Nspace {
			get {
				return nspace;
			}
			set {
				nspace = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			resolutionScope = ReadCodedIndex (metadataTable, binaryReader, CodedIndex.ResolutionScope);
			name = LoadStringIndex (metadataTable, binaryReader);
			nspace = LoadStringIndex (metadataTable, binaryReader);
		}
	}

	class TypeSpec : AbstractRow
	{
		public static readonly int TABLE_ID = 0x1B;
		uint signature; // index into the Blob heap
		
		public uint Signature {
			get {
				return signature;
			}
			set {
				signature = value;
			}
		}
		
		public override void LoadRow (MetadataTable metadataTable, BinaryReader binaryReader)
		{
			signature = LoadBlobIndex (metadataTable, binaryReader);
		}
	}
}
