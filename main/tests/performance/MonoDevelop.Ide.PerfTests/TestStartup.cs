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
using NUnit.Framework;

using MonoDevelop.Components.AutoTest;
using MonoDevelop.UserInterfaceTesting;
using MonoDevelop.PerformanceTesting;
using System.IO;
using System.Linq;

namespace MonoDevelop.Ide.PerfTests
{
	[TestFixture]
	public class TestStartup : UITestBase
	{
		// Override the setup so it only sets up the environment for running
		// because we want to time the start up
		public override void SetUp ()
		{
			PreStart ();
		}

		[Test]
		[Benchmark(Tolerance=0.1)]
		public void TestStartupTime ()
		{
			var mdProfileDir = Util.CreateTmpDir ();
			FoldersToClean.Add (mdProfileDir);

			var t = System.Diagnostics.Stopwatch.StartNew ();

			StartSession (mdProfileDir);
			Session.WaitForElement (IdeQuery.DefaultWorkbench);

			Benchmark.SetTime ((double)t.ElapsedMilliseconds / 1000d);
		}

		[Test]
		[Benchmark(Tolerance = 0.1)]
		public void TestTimeToCode ()
		{
			var mdProfileDir = Util.CreateTmpDir ();
			FoldersToClean.Add (mdProfileDir);

			var binDir = Path.GetDirectoryName (typeof(AutoTestClientSession).Assembly.Location);
			var sln = Path.Combine (MainPath, "build/tests/TestSolutions/ExampleFormsSolution/ExampleFormsSolution.sln");

			// Fallback to local for running inside MD
			if (!File.Exists (sln)) {
				sln = Path.Combine (MainPath, "Main.sln");
			}
			var t = System.Diagnostics.Stopwatch.StartNew ();

			StartSession (mdProfileDir);
			Session.WaitForElement (IdeQuery.DefaultWorkbench);

			// Load Solution
			Session.RunAndWaitForTimer (() => Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workspace.OpenWorkspaceItem", (Core.FilePath)sln), "Ide.Shell.SolutionOpened", 60000);

			// Wait until intellisense has finished
			Session.RunAndWaitForTimer (() => {}, "Ide.CodeAnalysis", 180000);

			Benchmark.SetTime ((double)t.ElapsedMilliseconds / 1000d);
		}
	}
}
