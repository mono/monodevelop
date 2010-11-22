// 
// Event.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.Collections.Generic;
namespace MonoDevelop.Profiler
{
	public static class Util
	{
		public static ulong ReadULeb128 (this BinaryReader reader)
		{
			ulong result = 0;
			int shift = 0;
			while (true) {
				byte b = reader.ReadByte ();
				result |= ((ulong)(b & 0x7f)) << shift;
				if ((b & 0x80) != 0x80)
					break;
				shift += 7;
			}
			return result;
		}
		
		public static long ReadSLeb128 (this BinaryReader reader)
		{
			long result = 0;
			int shift = 0;
			while (true) {
				byte b = reader.ReadByte ();
				result |= ((long)(b & 0x7f)) << shift;
				shift += 7;
				if ((b & 0x80) != 0x80) {
					if (shift < sizeof(long) * 8 && (b & 0x40) == 0x40)
						result |= -(1L << shift);
					break;
				}
			}
			return result;
		}
		
		public static string ReadNullTerminatedString (this BinaryReader reader)
		{
			List<byte> bytes = new List<byte> ();
			while (true) {
				byte b = reader.ReadByte ();
				if (b == 0)
					break;
				bytes.Add (b);
			}
			return Encoding.UTF8.GetString (bytes.ToArray ());
		}
		
	}
}
