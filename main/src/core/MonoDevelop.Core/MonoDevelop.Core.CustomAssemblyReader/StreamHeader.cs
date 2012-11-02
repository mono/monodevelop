// 
// StreamHeader.cs
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
	public class StreamHeader
	{
		uint offset;
		uint size;
		string name;
		
		public uint Offset {
			get {
				return offset;
			}
			set {
				offset = value;
			}
		}
		
		public uint Size {
			get {
				return size;
			}
			set {
				size = value;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			offset = binaryReader.ReadUInt32();
			size   = binaryReader.ReadUInt32();
			int bytesRead = 1;
			byte b = binaryReader.ReadByte();
			while (b != 0) {
				name += (char)b;
				b = binaryReader.ReadByte();
				++bytesRead;
			}
			// name is filled to 4 byte blocks
			int filler = bytesRead % 4 == 0 ? 0 :  4 - (bytesRead % 4);
			for (int i = 0; i < filler; ++i) {
				binaryReader.ReadByte();
			}
			
		}
	}
}
