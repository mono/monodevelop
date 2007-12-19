//
// HeapResize.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace MonoDevelop.Profiling.HeapBuddy
{
	public class Resize
	{		
		// The GC Generation during which the resize happened
		public int Generation;

		private long time_t;
		public DateTime Timestamp;

		public long PreviousSize;

		public long NewSize;

		public long TotalLiveBytes;

		public int TotalLiveObjects;

		public double PreResizeCapacity {
			get { return PreviousSize == 0 ? 0 : 100.0 * TotalLiveBytes / PreviousSize; }
		}

		public double PostResizeCapacity {
			get { return PreviousSize == 0 ? 0 : 100.0 * TotalLiveBytes / NewSize; }
		}


		// You need to set PreviousSize by hand.
		public void Read (BinaryReader reader, int generation)
		{
			if (generation < 0)
				Generation = reader.ReadInt32 ();
			else
				Generation = generation;
			time_t = reader.ReadInt64 ();
			Timestamp = Util.ConvertTimeT (time_t);
			NewSize = reader.ReadInt64 ();
			TotalLiveBytes = reader.ReadInt64 ();
			TotalLiveObjects = reader.ReadInt32 ();
		}

		public void Write (BinaryWriter writer)
		{
			writer.Write (Generation);
			writer.Write (time_t);
			writer.Write (NewSize);
			writer.Write (TotalLiveBytes);
			writer.Write (TotalLiveObjects);
		}
	}
}