//
// TestStartup.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2018 
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

using System.IO;

using NUnit.Framework;

using MonoDevelop.Components.AutoTest;
using MonoDevelop.UserInterfaceTesting;
using MonoDevelop.PerformanceTesting;

using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Ide.PerfTests
{
	[TestFixture]
	public class TestStartup : UITestBase
	{
		// Override the setup so it only sets up the environment for running
		// because we want to time the start up
		public override void SetUp ()
		{
			InstrumentationService.Enabled = true;
			PreStart ();
		}

		[Test]
		[Benchmark(Tolerance=0.1)]
		public void TestStartupTime ()
		{
			OpenApplicationAndWait ();

			var t = Session.GetCounterMetadataValue<long> ("Ide.Startup", "CorrectedStartupTime");
			Benchmark.SetTime ((double)t / 1000d);
		}

		[Test]
		[Benchmark(Tolerance = 0.1)]
		public void TestTimeToCode ()
		{
			OpenApplicationAndWait ();

			OpenExampleSolutionAndWait (out _);

			var t = Session.GetCounterMetadataValue<long>("Ide.TimeToCode", "CorrectedDuration");
			Benchmark.SetTime ((double)t / 1000d);
		}
	}
}
