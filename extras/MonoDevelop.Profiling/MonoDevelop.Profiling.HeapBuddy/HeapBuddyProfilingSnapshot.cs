//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (C) 2007 Ben Motmans
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
using System.ComponentModel;
using MonoDevelop.Profiling;

namespace MonoDevelop.Profiling.HeapBuddy
{
	public class HeapBuddyProfilingSnapshot : AbstractProfilingSnapshot
	{
		private OutfileReader outfile;
		
		public HeapBuddyProfilingSnapshot (HeapBuddyProfiler profiler, string filename)
			: base (profiler, filename)
		{
		}
		
		[Browsable (false)]
		public OutfileReader Outfile {
			get {
				if (outfile == null)
					outfile = new OutfileReader (filename);
				return outfile;
			}
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Allocated Objects Size")]
		[Description ("The size of the allocated objects.")]
		[Browsable (true)]
		public string AllocatedObjectsSize {
			get { return ProfilingService.PrettySize (outfile.TotalAllocatedBytes); }
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Allocated Objects Count")]
		[Description ("The number of the allocated objects.")]
		[Browsable (true)]
		public int AllocatedObjects {
			get { return outfile.TotalAllocatedObjects; }
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("GCs")]
		[Description ("The number of times the garbage collector was executed.")]
		[Browsable (true)]
		public int GCs {
			get { return outfile.Gcs.Length; }
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Resizes")]
		[Description ("The number of times the heap was resized.")]
		[Browsable (true)]
		public int Resizes {
			get { return outfile.Resizes.Length; }
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Final Heap Size")]
		[Description ("The size of the heap when the application was closed.")]
		[Browsable (true)]
		public string FinalHeapSize {
			get { return ProfilingService.PrettySize (outfile.LastResize.NewSize); }
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Distinct Types")]
		[Description ("The number of unique types.")]
		[Browsable (true)]
		public int DistinctTypes {
			get { return outfile.Types.Length; }
		}
		
		[DefaultValue (false)]
		[Category ("Summary")]
		[DisplayName ("Backtraces")]
		[Description ("The number of backtraces.")]
		[Browsable (true)]
		public int Backtraces {
			get { return outfile.Backtraces.Length; }
		}
	}
}
