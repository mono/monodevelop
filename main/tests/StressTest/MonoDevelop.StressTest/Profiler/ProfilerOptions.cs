//
// StressTestOptions.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Generic;

namespace MonoDevelop.StressTest
{
	public class ProfilerOptions
	{
		public enum ProfilerType
		{
			Disabled,
			HeapOnly,
			All,
			Custom
		}

		[Flags]
		public enum PrintReport
		{
			ObjectsDiff = 1 << 0,
			ObjectsTotal = 1 << 1,
			// OnlyOnLast
		}

		public ProfilerType Type { get; set; } = ProfilerType.Disabled;
		public PrintReport PrintReportTypes { get; set; }
		public int MaxFrames { get; set; }
		public string MlpdOutputPath { get; set; }
		public string CustomProfilerArguments { get; set; }
		public HashSet<string> PrintReportObjectNames { get; } = new HashSet<string>(StringComparer.Ordinal);
	}
}
