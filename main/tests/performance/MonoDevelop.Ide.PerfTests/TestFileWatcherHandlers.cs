//
// TestFileWatcherHandlers.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.PerformanceTesting;
using MonoDevelop.UserInterfaceTesting;
using NUnit.Framework;

namespace MonoDevelop.Ide.PerfTests
{
	[TestFixture ()]
	public class TestFileWatcherHandlers : UITestBase
	{
		public override void SetUp ()
		{
			InstrumentationService.Enabled = true;
			PreStart ();
		}

		[Test]
		[Benchmark (Tolerance = 0.3)]
		public void TestCreated ()
		{
			int [] trackedKinds = {
				0, // Created
				1, // Changed
				4, // Removed
				5, // Renamed
			};

			OpenApplicationAndWait ();

			var projectFolder = OpenExampleSolutionAndWait (out var waitForPackages);

			//var stressFolder = Path.Combine (projectFolder, "stress_fsw");
			//Directory.CreateDirectory (stressFolder);

			//// Create 10000 files.
			//for (int i = 0; i < 10000; ++i) {
			//	string fileName = Path.Combine (stressFolder, i.ToString () + ".txt");
			//	File.Create (fileName);
			//}

			//// Get timings from array
			//Session.GlobalInvoke<TimeSpan> ("MonoDevelop.Core.FileService.eventQueue.GetTimings", new object [] { 0 });

			//Session.WaitForCounterChange (() => Session.ExecuteCommand (Commands.ProjectCommands.BuildSolution), "Ide.Shell.ProjectBuilt", 60000);

			//var t = Session.GetTimerDuration ("Ide.Shell.ProjectBuilt");

			//Benchmark.SetTime (t.TotalSeconds);
		}

		[Test]
		[Benchmark (Tolerance = 0.3)]
		public void TestChanged ()
		{

		}
		
	}
}
