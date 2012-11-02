// 
// DataType.cs
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
	enum DataType
	{
		End            = 0x00,
		Void           = 0x01,
		Boolean        = 0x02,
		Char           = 0x03,
		SByte          = 0x04,
		Byte           = 0x05,
		Int16          = 0x06,
		UInt16         = 0x07,
		Int32          = 0x08,
		UInt32         = 0x09,
		Int64          = 0x0A,
		UInt64         = 0x0B,
		Single         = 0x0C,
		Double         = 0x0D,
		
		String         = 0x0E,
		Ptr            = 0x0F,
		ByRef          = 0x10,
		ValueType      = 0x11,
		Class          = 0x12,
		Array          = 0x14,
		
		TypeReference  = 0x16,
		IntPtr         = 0x18,
		UIntPtr        = 0x19,
		FnPtr          = 0x1B,
		Object         = 0x1C,
		SZArray        = 0x1D,
		
		CModReq        = 0x1F,
		CModOpt        = 0x20,
		Internal       = 0x21,
		
		Modifier       = 0x40,
		Sentinel       = 0x41,
		Pinned         = 0x45
		
	}
}
