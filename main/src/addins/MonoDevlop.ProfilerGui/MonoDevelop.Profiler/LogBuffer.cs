// 
// LogBuffer.cs
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
using System.Collections.Generic;

namespace MonoDevelop.Profiler
{
	public class LogBuffer
	{
		public readonly Header Header;
		public readonly List<Buffer> buffers = new List<Buffer> ();

		public static LogBuffer Read (string fileName)
		{
			if (!File.Exists (fileName))
				return null;
			BinaryReader reader = new BinaryReader (File.OpenRead (fileName));
			LogBuffer result = new LogBuffer (reader);
			reader.Close ();
			return result;
		}

		LogBuffer (BinaryReader reader)
		{
			this.Header = Header.Read (reader);
			while (reader.BaseStream.Position < reader.BaseStream.Length) {
				buffers.Add (Buffer.Read (reader));
			}
		}
	}
}

