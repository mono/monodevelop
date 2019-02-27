//
// ProgressMonitorTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
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
using MonoDevelop.Projects;
using NUnit.Framework;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading;
using System.Text;
using System.Collections.Concurrent;
using UnitTests;
using System.Linq;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class ProgressMonitorTests
	{
		[Test]
		public void TestReportObject ()
		{
			var m = new ReportObjectMonitor ();
			var foo = new object ();
			m.ReportObject (foo);
			Assert.IsTrue (m.ReportedObjects.Contains (foo));
		}

		[Test]
		public void TestReportObjectToFollowers ()
		{
			var main = new ProgressMonitor ();
			var m = new ReportObjectMonitor ();
			var agr = new AggregatedProgressMonitor (main, m);
			var foo = new object ();
			agr.ReportObject (foo);
			Assert.IsTrue (m.ReportedObjects.Contains (foo));
		}

		[Test]
		public void TestLogObject ()
		{
			var m = new ReportObjectMonitor ();
			var foo = new object ();
			m.Log.Write ("one");
			m.LogObject (foo);
			m.Log.Write ("two");
			Assert.AreEqual (3, m.LoggedObjects.Count);
			Assert.AreEqual ("one", m.LoggedObjects [0]);
			Assert.AreSame (foo, m.LoggedObjects [1]);
			Assert.AreEqual ("two", m.LoggedObjects [2]);
		}

		[Test]
		public void TestLogObjectToFollowers ()
		{
			var main = new ProgressMonitor ();
			var m = new ReportObjectMonitor ();
			var agr = new AggregatedProgressMonitor (main, m);
			var foo = new object ();
			agr.Log.Write ("one");
			agr.LogObject (foo);
			agr.Log.Write ("two");
			Assert.AreEqual (3, m.LoggedObjects.Count);
			Assert.AreEqual ("one", m.LoggedObjects [0]);
			Assert.AreSame (foo, m.LoggedObjects [1]);
			Assert.AreEqual ("two", m.LoggedObjects [2]);
		}

		[Test]
		public void TestLogObjectSplit ()
		{
			var m = new ReportObjectMonitor ();
			m.BeginTask (2);
			var p1 = m.BeginAsyncStep (1);
			var p2 = m.BeginAsyncStep (1);

			var foo = new object ();
			p2.LogObject (foo);
			p2.Log.Write ("two");
			p1.Log.Write ("one");

			p1.Dispose ();
			p2.Dispose ();

			Assert.AreEqual (3, m.LoggedObjects.Count);
			Assert.AreEqual ("one", m.LoggedObjects [0]);
			Assert.AreSame (foo, m.LoggedObjects [1]);
			Assert.AreEqual ("two", m.LoggedObjects [2]);
		}

		[Test]
		public void TestLogSynchronizationContextThroughWrites ()
		{
			using (var ctx = new TestSingleThreadSynchronizationContext ()) {
				int offset;

				using (var mon = new ChainedProgressMonitor (ctx)) {
					// These call once into Write.
					mon.Log.Write ("a");
					Assert.AreEqual (1, ctx.CallCount);

					mon.Log.Write ('a');
					Assert.AreEqual (2, ctx.CallCount);

					mon.Log.Write (new [] { 'a' }, 0, 1);
					Assert.AreEqual (3, ctx.CallCount);

					offset = 3;

					// This calls twice into Write in newer versions of the BCL,
					//  and once in older versions.
					mon.Log.WriteLine ("a");
					Assert.IsTrue (ctx.CallCount - offset <= 2, "WriteLine should produce 1-2 calls");
					offset = ctx.CallCount;

					// These 2 call twice into Write.
					mon.Log.WriteLine ('a');
					Assert.AreEqual (2, ctx.CallCount - offset);
					offset = ctx.CallCount;

					mon.Log.WriteLine (new [] { 'a' }, 0, 1);
					Assert.AreEqual (2, ctx.CallCount - offset);
					offset = ctx.CallCount;

					// Flush performs one call
					mon.Log.Flush ();
					Assert.AreEqual (1, ctx.CallCount - offset);
					offset = ctx.CallCount;
				}

				// Once for completed, once for Dispose.
				Assert.AreEqual (2, ctx.CallCount - offset);
			}
		}

		[Test]
		public void TestParentChildTask ()
		{
			var monitor = new ProgressMonitor ();

			var mainTask = monitor.BeginTask ("Task", 1) as ProgressTask;

			Assert.AreSame (mainTask, monitor.CurrentTask);
			Assert.AreEqual (mainTask.Name, monitor.CurrentTaskName);
			Assert.IsTrue (monitor.GetRootTasks ().Contains (mainTask));

			var childTask = monitor.BeginTask ("ChildTask", 1) as ProgressTask;

			// test task hierarchy
			Assert.NotNull (mainTask);
			Assert.NotNull (childTask);
			Assert.AreSame (mainTask, childTask.ParentTask);
			Assert.AreSame (childTask, monitor.CurrentTask);
			Assert.AreEqual (childTask.Name, monitor.CurrentTaskName);
			Assert.IsTrue (monitor.GetRootTasks ().Contains (mainTask));
			Assert.IsTrue (mainTask.GetChildrenTasks ().Contains (childTask));

			// ensure that the child is not a root task
			Assert.IsFalse (monitor.GetRootTasks ().Contains (childTask));

			monitor.EndTask ();

			Assert.AreSame (mainTask, monitor.CurrentTask);
			Assert.AreEqual (mainTask.Name, monitor.CurrentTaskName);
			Assert.IsTrue (monitor.GetRootTasks ().Contains (mainTask));
			Assert.IsFalse (mainTask.GetChildrenTasks ().Contains (childTask));

			monitor.EndTask ();

			Assert.IsNull (monitor.CurrentTask);
			Assert.IsFalse (monitor.GetRootTasks ().Any ());
		}

		[Test, Combinatorial]
		public void TestUsingMultipleParentChildTasks ([Range (1, 2)] int rootTasks, [Range (1, 2)] int childTasks)
		{
			var monitor = new ProgressMonitor ();

			for (int i = 1; i <= rootTasks; i++) {
				using (var mainTask = monitor.BeginTask ("Task" + i, 1) as ProgressTask) {
					for (int j = 1; j <= rootTasks; j++) {
						using (var childTask = monitor.BeginTask ("ChildTask" + j, 1) as ProgressTask) {
							// test task hierarchy
							Assert.AreSame (mainTask, childTask.ParentTask);
							Assert.AreSame (childTask, monitor.CurrentTask);
							Assert.IsTrue (monitor.GetRootTasks ().Contains (mainTask));
							Assert.IsTrue (mainTask.GetChildrenTasks ().Contains (childTask));

							// ensure that the child is not a root task
							Assert.IsFalse (monitor.GetRootTasks ().Contains (childTask));
						}
						Assert.AreSame (mainTask, monitor.CurrentTask);
						Assert.IsTrue (monitor.GetRootTasks ().Contains (mainTask));
						Assert.IsFalse (mainTask.GetChildrenTasks ().Any ());
					}
				}

				Assert.IsNull (monitor.CurrentTask);
				Assert.IsFalse (monitor.GetRootTasks ().Any ());
			}
		}

		static readonly double taskProgressTestPrecision = 0.0001;

		[TestCase (true, TestName = "With Completion")]
		[TestCase (true, TestName = "Without Completion")]
		public void TestSingleTaskProgress (bool stepsToFinish)
		{
			int steps = 10;
			// we test 6 steps + 1 for finish test,
			// just make sure that we start the task with enough steps
			Assert.Greater (steps, 7);

			double stepProgress = 1d / steps;
			var monitor = new ProgressMonitor ();
			Assert.AreSame (monitor.BeginTask ("Task", steps), monitor.CurrentTask);

			var task = monitor.CurrentTask;
			Assert.NotNull (task);
			Assert.AreEqual ("Task", monitor.CurrentTaskName);
			Assert.AreEqual (0, monitor.Progress);
			Assert.AreEqual (task.Progress, monitor.Progress);
			Assert.AreEqual (steps, task.TotalWork);
			Assert.IsTrue (string.IsNullOrEmpty (task.StatusMessage));

			int stepped = 0;

			// 1 step
			monitor.Step ();
			stepped += 1;
			Assert.That (monitor.Progress, Is.EqualTo (stepProgress * stepped).Within (taskProgressTestPrecision));
			Assert.AreEqual (monitor.Progress, task.Progress);

			// 2 steps
			monitor.Step (2);
			stepped += 2;
			Assert.That (monitor.Progress, Is.EqualTo (stepProgress * stepped).Within (taskProgressTestPrecision));
			Assert.AreEqual (monitor.Progress, task.Progress);

			// Begin/End 1 step
			monitor.BeginStep ();
			Assert.That (monitor.Progress, Is.EqualTo (stepProgress * stepped).Within (taskProgressTestPrecision), "Task progress changed before the task ended");
			Assert.AreEqual (monitor.Progress, task.Progress);
			monitor.EndStep ();
			stepped += 1;
			Assert.That (monitor.Progress, Is.EqualTo (stepProgress * stepped).Within (taskProgressTestPrecision));
			Assert.AreEqual (monitor.Progress, task.Progress);

			// Begin/End 2 steps
			monitor.BeginStep (2);
			Assert.That (monitor.Progress, Is.EqualTo (stepProgress * stepped).Within (taskProgressTestPrecision), "Task progress changed before the task ended");
			Assert.AreEqual (monitor.Progress, task.Progress);
			monitor.EndStep ();
			stepped += 2;
			Assert.That (monitor.Progress, Is.EqualTo (stepProgress * stepped).Within (taskProgressTestPrecision));
			Assert.AreEqual (monitor.Progress, task.Progress);

			// Run to end
			if (stepsToFinish)
				// Step to End
				monitor.Step (steps - stepped);
			else
				// End Task
				monitor.EndTask ();

			Assert.That (monitor.Progress, Is.EqualTo (1).Within (taskProgressTestPrecision));
			Assert.AreEqual (monitor.Progress, task.Progress);
		}

		[Test]
		public void TestSingleTaskProgressWithSteps ([Values (1, 10, 100, 1000)] int stepsCount)
		{
			var monitor = new ProgressMonitor ();
			var mainTask = monitor.BeginTask ("Task", stepsCount) as ProgressTask;

			Assert.AreEqual (stepsCount, mainTask.TotalWork);

			for (int i = 1; i <= stepsCount; i++) {
				monitor.Step ();
				var expectedProgress = (1d / stepsCount) * i;
				Assert.That (mainTask.Progress, Is.EqualTo (expectedProgress).Within (taskProgressTestPrecision));
				Assert.That (monitor.Progress, Is.EqualTo (expectedProgress).Within (taskProgressTestPrecision));
			}

			Assert.AreEqual (1, monitor.Progress);
			Assert.AreEqual (mainTask.Progress, monitor.Progress);
		}
	}

	class ChainedProgressMonitor : ProgressMonitor
	{
		readonly CustomWriter underlyingLog;
		public ChainedProgressMonitor (TestSingleThreadSynchronizationContext ctx) : base (ctx)
		{
			Log = underlyingLog = new CustomWriter (ctx);
		}

		protected override void OnDispose (bool disposing)
		{
			underlyingLog.Dispose ();
			base.OnDispose (disposing);
		}

		class CustomWriter : System.IO.TextWriter
		{
			TestSingleThreadSynchronizationContext ctx;
			public CustomWriter (TestSingleThreadSynchronizationContext ctx)
			{
				this.ctx = ctx;
			}

			public override void Flush ()
			{
				Assert.AreEqual (ctx.Thread, Thread.CurrentThread);
				base.Flush ();
			}

			public override void Write (char value)
			{
				Assert.AreEqual (ctx.Thread, Thread.CurrentThread);
				base.Write (value);
			}

			public override void Close ()
			{
				Assert.AreEqual (ctx.Thread, Thread.CurrentThread);
				base.Close ();
			}

			public override void Write (string value)
			{
				Assert.AreEqual (ctx.Thread, Thread.CurrentThread);
				base.Write (value);
			}

			public override void Write (char [] buffer, int index, int count)
			{
				Assert.AreEqual (ctx.Thread, Thread.CurrentThread);
				base.Write (buffer, index, count);
			}

			public override Encoding Encoding => Encoding.Default;
		}
	}

	class ReportObjectMonitor: ProgressMonitor
	{
		public List<object> ReportedObjects = new List<object> ();

		public List<object> LoggedObjects = new List<object> ();

        protected override void OnObjectReported(object statusObject)
        {
			ReportedObjects.Add (statusObject);
			base.OnObjectReported(statusObject);
        }

        protected override void OnWriteLog(string message)
        {
			LoggedObjects.Add (message);
			base.OnWriteLog(message);
        }

        protected override void OnWriteLogObject(object logObject)
        {
			LoggedObjects.Add (logObject);
			base.OnWriteLogObject(logObject);
        }
    }
}
