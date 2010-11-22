// 
// Buffer.cs
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
	public class BufferHeader
	{
		const int BUF_ID = 0x4D504C01;
		public readonly int BufId; // constant value: BUF_ID
		public readonly int Length; // size of the data following the buffer header
		public readonly ulong TimeBase; // time base in nanoseconds since an unspecified epoch
		public readonly long PtrBase; // base value for pointers
		public readonly long ObjBase; // base value for object addresses
		public readonly long ThreadId; // system-specific thread ID (pthread_t for example)
		public readonly long MethodBase; // base value for MonoMethod pointers

		BufferHeader (BinaryReader reader)
		{
			BufId = reader.ReadInt32 ();
			if (BufId != BUF_ID)
				throw new IOException (string.Format ("Incorrect buffer id: 0x{0:X}", BufId));
			Length = reader.ReadInt32 ();
			TimeBase = reader.ReadUInt64 ();
			PtrBase = reader.ReadInt64 ();
			ObjBase = reader.ReadInt64 ();
			ThreadId = reader.ReadInt64 ();
			MethodBase = reader.ReadInt64 ();
			System.Console.WriteLine ("Buffer: Thread:{0:X}, len:{1}, time: {2}", ThreadId, Length, TimeBase);
		}

		public static BufferHeader Read (BinaryReader reader)
		{
			return new BufferHeader (reader);
		}
	}

	public class Buffer
	{
		public readonly BufferHeader Header;
		public readonly List<Event> Events = new List<Event> ();
		
		Buffer (BinaryReader reader)
		{
			Header = BufferHeader.Read (reader);
			var endPos = reader.BaseStream.Position + Header.Length;
			while (reader.BaseStream.Position < endPos) {
				Events.Add (Event.Read (reader));
			}
			System.Console.WriteLine ("Buffer loaded.");
		}
		
		public void RunVisitor (EventVisitor visitor)
		{
			Events.ForEach (e => e.Accept (visitor));
		}

		public static Buffer Read (BinaryReader reader)
		{
			return new Buffer (reader);
		}
	}
}

