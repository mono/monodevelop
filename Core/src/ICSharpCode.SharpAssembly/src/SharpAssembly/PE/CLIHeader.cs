// CLIHeader.cs
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
	
	public class CLIHeader
	{
		uint   cb;
		ushort majorRuntimeVersion;
		ushort minorRuntimeVersion;
		uint   metaData;
		uint   metaDataSize;
		uint   flags;
		uint   entryPointToken;
		uint   resources;
		uint   resourcesSize;
		ulong strongNameSignature;
		ulong codeManagerTable;
		ulong vTableFixups;
		ulong exportAddressTableJumps;
		ulong managedNativeHeader;
			
		const uint COMIMAGE_FLAGS_ILONLY           = 0x01;
		const uint COMIMAGE_FLAGS_32BITREQUIRED    = 0x02;
		const uint COMIMAGE_FLAGS_STRONGNAMESIGNED = 0x08;
		const uint COMIMAGE_FLAGS_TRACKDEBUGDATA   = 0x010000;
		
		public uint Cb {
			get {
				return cb;
			}
			set {
				cb = value;
			}
		}
		public ushort MajorRuntimeVersion {
			get {
				return majorRuntimeVersion;
			}
			set {
				majorRuntimeVersion = value;
			}
		}
		public ushort MinorRuntimeVersion {
			get {
				return minorRuntimeVersion;
			}
			set {
				minorRuntimeVersion = value;
			}
		}
		public uint MetaData {
			get {
				return metaData;
			}
			set {
				metaData = value;
			}
		}
		public uint MetaDataSize {
			get {
				return metaDataSize;
			}
			set {
				metaDataSize = value;
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
		public uint EntryPointToken {
			get {
				return entryPointToken;
			}
			set {
				entryPointToken = value;
			}
		}
		public uint Resources {
			get {
				return resources;
			}
			set {
				resources = value;
			}
		}
		public uint ResourcesSize {
			get {
				return resourcesSize;
			}
			set {
				resourcesSize = value;
			}
		}
		public ulong StrongNameSignature {
			get {
				return strongNameSignature;
			}
			set {
				strongNameSignature = value;
			}
		}
		public ulong CodeManagerTable {
			get {
				return codeManagerTable;
			}
			set {
				codeManagerTable = value;
			}
		}
		public ulong VTableFixups {
			get {
				return vTableFixups;
			}
			set {
				vTableFixups = value;
			}
		}
		public ulong ExportAddressTableJumps {
			get {
				return exportAddressTableJumps;
			}
			set {
				exportAddressTableJumps = value;
			}
		}
		public ulong ManagedNativeHeader {
			get {
				return managedNativeHeader;
			}
			set {
				managedNativeHeader = value;
			}
		}
		
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			cb                      = binaryReader.ReadUInt32();
			majorRuntimeVersion     = binaryReader.ReadUInt16();
			minorRuntimeVersion     = binaryReader.ReadUInt16();
			metaData                = binaryReader.ReadUInt32();
			metaDataSize            = binaryReader.ReadUInt32();
			flags                   = binaryReader.ReadUInt32();
			entryPointToken         = binaryReader.ReadUInt32();
			resources               = binaryReader.ReadUInt32();
			resourcesSize           = binaryReader.ReadUInt32();
			strongNameSignature     = binaryReader.ReadUInt64();
			codeManagerTable        = binaryReader.ReadUInt64();
			vTableFixups            = binaryReader.ReadUInt64();
			exportAddressTableJumps = binaryReader.ReadUInt64();
			managedNativeHeader     = binaryReader.ReadUInt64();
		}
	}
}
