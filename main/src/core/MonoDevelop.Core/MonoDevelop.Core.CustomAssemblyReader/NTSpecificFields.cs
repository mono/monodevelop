// 
// NTSpecificFields.cs
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
	public class NTSpecificFields
	{
		const uint IMAGE_BASE        = 0x400000;
		const uint SECTION_ALIGNMENT = 0x2000;
		
//		uint fileAlignment; // either 0x200 or 0x1000
//		ushort osMajor;
//		ushort osMinor;
//		ushort userMajor;
//		ushort userMinor;
//		ushort subSysMajor;
//		ushort subSysMinor;
//		uint   reserved;
//		uint   imageSize;
//		uint   headerSize;
//		uint   fileChecksum;
//		ushort subSystem;
//		ushort dllFlags;
//		uint   stackReserveSize;
//		uint   stackCommitSize;
//		uint   heapReserveSize;
//		uint   heapCommitSize;
//		uint   loaderFlags;
//		uint   numberOfDataDirectories;
		
		public void LoadFrom(BinaryReader binaryReader)
		{
			// not needed
			byte[] buffer = new byte[68];
			binaryReader.Read(buffer, 0, 68);
		}
	}
}
