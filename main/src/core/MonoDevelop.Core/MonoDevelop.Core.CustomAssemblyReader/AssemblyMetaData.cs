// 
// AssemblyMetadata.cs
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
