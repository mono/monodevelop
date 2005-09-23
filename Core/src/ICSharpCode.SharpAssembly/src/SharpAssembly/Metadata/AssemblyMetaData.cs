// AssemblyMetaData.cs
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
using MonoDevelop.SharpAssembly.PE;

namespace MonoDevelop.SharpAssembly.Metadata
{
	
	public class AssemblyMetadata
	{
		const uint MAGIC_SIGN = 0x424A5342;
		ushort majorVersion;
		ushort minorVersion;
		uint   reserved;
		uint   length;
		string versionString;
		ushort flags;
		ushort numerOfStreams;
		
		StreamHeader[] streamHeaders;
		
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
		public uint Reserved {
			get {
				return reserved;
			}
			set {
				reserved = value;
			}
		}
		public uint Length {
			get {
				return length;
			}
			set {
				length = value;
			}
		}
		public string VersionString {
			get {
				return versionString;
			}
			set {
				versionString = value;
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
		public ushort NumerOfStreams {
			get {
				return numerOfStreams;
			}
			set {
				numerOfStreams = value;
			}
		}
		public StreamHeader[] StreamHeaders {
			get {
				return streamHeaders;
			}
			set {
				streamHeaders = value;
			}
		}
		
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			uint signature = binaryReader.ReadUInt32();
			if (signature != MAGIC_SIGN) {
				Console.WriteLine("WARNING signature != MAGIC_SIGN ");
			}
			
			majorVersion = binaryReader.ReadUInt16();
			minorVersion = binaryReader.ReadUInt16();
			reserved = binaryReader.ReadUInt32();
			length = binaryReader.ReadUInt32();
			byte[] versionStringBytes = new byte[length];
			binaryReader.Read(versionStringBytes, 0, (int)length);
			versionString = System.Text.Encoding.UTF8.GetString(versionStringBytes);
			flags = binaryReader.ReadUInt16();
			numerOfStreams = binaryReader.ReadUInt16();
			streamHeaders = new StreamHeader[numerOfStreams];
			for (int i = 0; i < numerOfStreams; ++i) {
				streamHeaders[i] = new StreamHeader();
				streamHeaders[i].LoadFrom(binaryReader);
			}
			
		}
	}
}
