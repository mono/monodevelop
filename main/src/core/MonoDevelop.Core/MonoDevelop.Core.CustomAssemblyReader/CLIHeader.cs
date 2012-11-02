// 
// CLIHeader.cs
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
