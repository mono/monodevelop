//
// ObjectStats.cs
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

using System.IO;

namespace MonoDevelop.Profiling.HeapBuddy
{
	public struct ObjectStats
	{		
		public uint AllocatedCount;
		public uint AllocatedTotalBytes;
		public uint AllocatedTotalAge;
		public uint AllocatedTotalWeight;
		
		public double AllocatedAverageBytes {
			get { return AllocatedCount != 0 ? AllocatedTotalBytes / (double) AllocatedCount : 0; }
		}

		public double AllocatedAverageAge {
			get { return AllocatedCount != 0 ? AllocatedTotalAge / (double) AllocatedCount : 0; }
		}

		public uint LiveCount;
		public uint LiveTotalBytes;
		public uint LiveTotalAge;
		public uint LiveTotalWeight;

		public double LiveAverageBytes {
			get { return LiveCount != 0 ? LiveTotalBytes / (double) LiveCount : 0; }
		}

		public double LiveAverageAge {
			get { return LiveCount != 0 ? LiveTotalAge / (double) LiveCount : 0; }
		}


		public uint DeadCount {
			get { return AllocatedCount - LiveCount; }
		}
		
		public uint DeadTotalBytes {
			get { return AllocatedTotalBytes - LiveTotalBytes; }
		}

		public uint DeadTotalAge {
			get { return AllocatedTotalAge - LiveTotalAge; }
		}

		public double DeadAverageBytes {
			get { return DeadCount != 0 ? DeadTotalBytes / (double) DeadCount : 0; }
		}

		public double DeadAverageAge {
			get { return DeadCount != 0 ? DeadTotalAge / (double) DeadCount : 0; }
		}

		public uint DeadTotalWeight {
			get { return AllocatedTotalWeight - LiveTotalWeight; }
		}

		/////////////////////////////////////////////////////

		public void Read (BinaryReader reader)
		{
			AllocatedCount = reader.ReadUInt32 ();
			AllocatedTotalBytes = reader.ReadUInt32 ();
			AllocatedTotalAge = reader.ReadUInt32 ();
			AllocatedTotalWeight = reader.ReadUInt32 ();

			LiveCount = reader.ReadUInt32 ();
			LiveTotalBytes = reader.ReadUInt32 ();
			LiveTotalAge = reader.ReadUInt32 ();
			LiveTotalWeight = reader.ReadUInt32 ();
		}

		public void Write (BinaryWriter writer)
		{
			writer.Write (AllocatedCount);
			writer.Write (AllocatedTotalBytes);
			writer.Write (AllocatedTotalAge);
			writer.Write (AllocatedTotalWeight);

			writer.Write (LiveCount);
			writer.Write (LiveTotalBytes);
			writer.Write (LiveTotalAge);
			writer.Write (LiveTotalWeight);
		}

		/////////////////////////////////////////////////////

		public void AddAllocatedOnly (ObjectStats other)
		{
			this.AllocatedCount += other.AllocatedCount;
			this.AllocatedTotalBytes += other.AllocatedTotalBytes;
			this.AllocatedTotalAge += other.AllocatedTotalAge;
			this.AllocatedTotalWeight += other.AllocatedTotalWeight;
		}

		/////////////////////////////////////////////////////

		static public ObjectStats operator + (ObjectStats a, ObjectStats b)
		{
			ObjectStats c = new ObjectStats ();

			c.AllocatedCount = a.AllocatedCount + b.AllocatedCount;
			c.AllocatedTotalBytes = a.AllocatedTotalBytes + b.AllocatedTotalBytes;
			c.AllocatedTotalAge = a.AllocatedTotalAge + b.AllocatedTotalAge;
			c.AllocatedTotalWeight = a.AllocatedTotalWeight + b.AllocatedTotalWeight;

			c.LiveCount = a.LiveCount + b.LiveCount;
			c.LiveTotalBytes = a.LiveTotalBytes + b.LiveTotalBytes;
			c.LiveTotalAge = a.LiveTotalAge + b.LiveTotalAge;
			c.LiveTotalWeight = a.LiveTotalWeight + b.LiveTotalWeight;

			return c;
		}

		static public ObjectStats operator - (ObjectStats a, ObjectStats b)
		{
			ObjectStats c = new ObjectStats ();

			c.AllocatedCount = a.AllocatedCount - b.AllocatedCount;
			c.AllocatedTotalBytes = a.AllocatedTotalBytes - b.AllocatedTotalBytes;
			c.AllocatedTotalAge = a.AllocatedTotalAge - b.AllocatedTotalAge;
			c.AllocatedTotalWeight = a.AllocatedTotalWeight - b.AllocatedTotalWeight;

			c.LiveCount = a.LiveCount - b.LiveCount;
			c.LiveTotalBytes = a.LiveTotalBytes - b.LiveTotalBytes;
			c.LiveTotalAge = a.LiveTotalAge - b.LiveTotalAge;
			c.LiveTotalWeight = a.LiveTotalWeight - b.LiveTotalWeight;

			return c;
		}
	}
}