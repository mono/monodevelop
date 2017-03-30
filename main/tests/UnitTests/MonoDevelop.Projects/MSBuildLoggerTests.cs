//
// MSBuildLoggerTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Text;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildLoggerTests: TestBase
	{
		[Test]
		public async Task BasicEvents ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var myLogger = new MSBuildLogger ();
			TargetEvaluationContext ctx = new TargetEvaluationContext ();
			ctx.Loggers.Add (myLogger);
			myLogger.EnabledEvents = MSBuildEvent.BuildStarted | MSBuildEvent.BuildFinished | MSBuildEvent.TargetStarted;

			int started = 0, finished = 0, targetStarted = 0;

			myLogger.EventRaised += (sender, e) => {
				switch (e.Event) {
					case MSBuildEvent.BuildStarted: started++; break;
					case MSBuildEvent.BuildFinished: finished++; break;
					case MSBuildEvent.TargetStarted: targetStarted++; break;
					default: throw new InvalidOperationException ("Unexpected event: " + e.Event);
				}
			};
			await item.Build (Util.GetMonitor (), "Debug", ctx);
			Assert.AreEqual (1, started);
			Assert.AreEqual (1, finished);
			Assert.IsTrue (targetStarted > 0);

			item.Dispose ();
		}

		[Test]
		public async Task NoEvents ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var myLogger = new MSBuildLogger ();
			TargetEvaluationContext ctx = new TargetEvaluationContext ();
			ctx.Loggers.Add (myLogger);
			myLogger.EnabledEvents = MSBuildEvent.None;

			int events = 0;

			myLogger.EventRaised += (sender, e) => events++;
			await item.Build (Util.GetMonitor (), "Debug", ctx);
			Assert.AreEqual (0, events);

			item.Dispose ();
		}

		[Test]
		public async Task AllEvents ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var myLogger = new MSBuildLogger ();
			TargetEvaluationContext ctx = new TargetEvaluationContext ();
			ctx.Loggers.Add (myLogger);
			myLogger.EnabledEvents = MSBuildEvent.All;

			int buildStarted = 0, buildFinished = 0, targetStarted = 0, targetFinished = 0, projectStarted = 0,
			projectFinished = 0, taskStarted = 0, taskFinished = 0;

			myLogger.EventRaised += (sender, e) => {
				switch (e.Event) {
				case MSBuildEvent.BuildStarted: buildStarted++; break;
				case MSBuildEvent.BuildFinished: buildFinished++; break;
				case MSBuildEvent.TargetStarted: targetStarted++; break;
				case MSBuildEvent.TargetFinished: targetFinished++; break;
				case MSBuildEvent.ProjectStarted: projectStarted++; break;
				case MSBuildEvent.ProjectFinished: projectFinished++; break;
				case MSBuildEvent.TaskStarted: taskStarted++; break;
				case MSBuildEvent.TaskFinished: taskFinished++; break;
				}
			};
			var mon = new StringMonitor ();
			await item.Build (mon, "Debug", ctx);

			Assert.AreEqual (1, buildStarted);
			Assert.AreEqual (1, buildFinished);
			Assert.AreEqual (1, projectStarted);
			Assert.AreEqual (1, projectFinished);
			Assert.AreEqual (taskStarted, taskFinished);
			Assert.AreEqual (targetStarted, targetFinished);
			Assert.AreNotEqual (string.Empty, mon.GetLogText ());

			item.Dispose ();
		}

		[Test]
		public async Task NoLog ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			TargetEvaluationContext ctx = new TargetEvaluationContext ();
			ctx.LogVerbosity = MSBuildVerbosity.Quiet;
			var mon = new StringMonitor ();
			await item.Build (mon, "Debug", ctx);
			Assert.AreEqual (string.Empty, mon.GetLogText ());

			item.Dispose ();
		}
	}

	class StringMonitor: ProgressMonitor
	{
		StringBuilder sb = new StringBuilder ();

		protected override void OnWriteLog (string message)
		{
			sb.Append (message);
		}

		public string GetLogText ()
		{
			return sb.ToString ();
		}
	}
}
