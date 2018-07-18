//
// TestSolutionBuild.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using NUnit.Framework;

using MonoDevelop.UserInterfaceTesting;
using MonoDevelop.PerformanceTesting;

using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Ide.PerfTests
{
	[TestFixture ()]
	[Benchmark (Tolerance = 0.1)]
	public class TestSolutionBuild : UITestBase
	{
		public override void SetUp ()
		{
			InstrumentationService.Enabled = true;
			PreStart ();
		}

		[Test ()]
		public void TestBuild ()
		{
			OpenApplicationAndWait ();

			OpenExampleSolutionAndWait (out var waitForPackages);

			if (waitForPackages) {
				// The package system emits signals on the Solution object, but we don't have access to that,
				// so we watch the statusbar for notification that packages are updated.
				UserInterfaceTesting.Ide.WaitForStatusMessage (new [] {"Packages successfully restored."});
			}
			Session.RunAndWaitForTimer (() => Session.ExecuteCommand (Commands.ProjectCommands.BuildSolution), "Ide.Shell.ProjectBuilt", 60000);

			var t = Session.GetTimerDuration ("Ide.Shell.ProjectBuilt");

			Benchmark.SetTime ((double)t.TotalMilliseconds / 1000d);
		}
	}
}
