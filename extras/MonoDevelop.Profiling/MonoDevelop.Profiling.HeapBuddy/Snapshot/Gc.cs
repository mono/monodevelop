//
// Gc.cs
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

using System;
using System.IO;

namespace MonoDevelop.Profiling.HeapBuddy
{
	public struct GcData
	{
		public Backtrace Backtrace;
		public ObjectStats ObjectStats;
	}

	public class Gc
	{
		public int Generation;

		public long TimeT;
		public DateTime Timestamp;

		public long PreGcLiveBytes;
		public int  PreGcLiveObjects;
		public long PostGcLiveBytes;
		public int  PostGcLiveObjects;

		private GcData [] gc_data;
		OutfileReader reader;

		/////////////////////////////////////////////////////////////////

		public Gc (OutfileReader reader)
		{
			this.reader = reader;
		}

		/////////////////////////////////////////////////////////////////

		public long FreedBytes {
			get { return PreGcLiveBytes - PostGcLiveBytes; }
		}

		public int FreedObjects {
			get { return PreGcLiveObjects - PostGcLiveObjects; }
		}

		public double FreedBytesPercentage {
			get { return PreGcLiveBytes == 0 ? 0 : 100.0 * FreedBytes / PreGcLiveBytes; }
		}

		public double FreedObjectsPercentage {
			get { return PreGcLiveObjects == 0 ? 0 : 100.0 * FreedObjects / PreGcLiveObjects; }
		}

		public GcData [] GcData {
			get { 
				if (gc_data == null)
					gc_data = reader.GetGcData (Generation);
				return gc_data; 
			}
			
			set { gc_data = value; }
		}
	}
}