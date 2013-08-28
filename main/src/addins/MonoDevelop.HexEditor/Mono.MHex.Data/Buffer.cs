// 
// Buffer.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace Mono.MHex.Data
{
	interface IBuffer
	{
		long Length {
			get;
		}
		byte[] Bytes {
			get;
		}
		
		byte[] GetBytes (long offset, int count);
	}
	
	class ArrayBuffer : IBuffer
	{
		byte[] content;

		public ArrayBuffer (byte[] buffer)
		{
			content = buffer;
		}
		
		public long Length {
			get {
				return content.Length;
			}
		}
		
		public byte[] Bytes {
			get {
				return content;
			}
		}
		
		public byte[] GetBytes (long offset, int count)
		{
			byte[] result = new byte[count];
			for (int i = 0; i < result.Length; i++) {
				result[i] = content[offset + i];
			}
			return result;
		}
		
		public static IBuffer Load (Stream stream)
		{
			int count = (int) stream.Length;
			byte[] buf= new byte[count];

			stream.Read (buf, 0, count);

			return new ArrayBuffer (buf);
		}
		
		public static IBuffer Load (string fileName)
		{
			using (Stream stream = File.OpenRead (fileName)) {
				return Load (stream);
			}
		}
	}
	
	class FileBuffer : IBuffer
	{
		FileStream stream;
		
		public long Length {
			get {
				return stream.Length;
			}
		}
		
		public byte[] Bytes {
			get {
				return GetBytes (0, (int)stream.Length);
			}
		}
		
		
		public byte[] GetBytes (long offset, int count)
		{
			stream.Position = offset;
			byte[] result = new byte[(int)count];
			if (count != stream.Read (result, 0, count))
				throw new IOException ("can't read enough bytes from input stream"); 
			return result;
		}
		
		public static IBuffer Load (string fileName)
		{
			FileBuffer result =  new FileBuffer ();
			result.stream = File.OpenRead (fileName);
			return result;
		}
	}
}
