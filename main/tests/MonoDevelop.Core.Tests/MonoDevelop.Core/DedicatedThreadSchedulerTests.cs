//
// DedicatedThreadSchedulerTests.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2019 
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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class DedicatedThreadSchedulerTests
	{
		const string DedicatedThreadName = "Test Scheduler Thread";

		DedicatedThreadScheduler TestScheduler;
		TaskFactory TestFactory;

		[SetUp]
		public void Setup ()
		{
			TestScheduler = new DedicatedThreadScheduler (DedicatedThreadName);
			TestFactory = new TaskFactory (TestScheduler);
		}

		[Test]
		public void ConcurrencyLevel ()
		{
			Assert.AreEqual (1, TestScheduler.MaximumConcurrencyLevel);
		}

		static void AssertQueuedOnScheduler (DedicatedThreadScheduler scheduler)
		{
			Assert.AreSame (TaskScheduler.Current, scheduler);
			Assert.AreSame (Thread.CurrentThread, scheduler.DedicatedThread);
		}

		[Test]
		[Timeout (5000)]
		public void DedicatedThreadStartStop ()
		{
			var mainThread = Thread.CurrentThread;

			var tcs = new TaskCompletionSource<bool> ();
			var task = TestFactory.StartNew (() => {
				AssertQueuedOnScheduler (TestScheduler);
				Assert.AreEqual (DedicatedThreadName, Thread.CurrentThread.Name);
				Assert.AreNotSame (mainThread, TestScheduler.DedicatedThread);
				Assert.DoesNotThrow (tcs.Task.Wait);
			});

			var testThread = TestScheduler.DedicatedThread;

			Assert.IsTrue (testThread.IsBackground);
			Assert.IsTrue (testThread.IsAlive);
			Assert.IsFalse (testThread.IsThreadPoolThread);
			Assert.AreEqual (DedicatedThreadName, testThread.Name);

			Assert.IsTrue (tcs.TrySetResult (true));

			Assert.DoesNotThrow (task.Wait);
			Assert.IsTrue (task.IsCompleted);

			TestScheduler.Dispose ();

			Assert.IsTrue (testThread.Join (50)); // wait a bit for the thread to dispose of the queue
			Assert.Throws<ObjectDisposedException> (() => TestScheduler.DedicatedThread.Join (0));
			Assert.IsFalse (testThread.IsAlive);
			Assert.AreEqual (ThreadState.Stopped, testThread.ThreadState);
		}

		[Test]
		[Timeout (20000)]
		public async Task TestTasksExecuteSequentially ()
		{
			int runTasks = 50;
			int mdelay = 2;
			var results = new List<int> (runTasks);
			var tasks = new Task [runTasks];

			for (int i = 0; i < runTasks; i++) {
				var task = TestFactory.StartNew ((counter) => {
					AssertQueuedOnScheduler (TestScheduler);
					// decrease the delay of scheduled tasks
					var result = runTasks - (int)counter - 1;
					Thread.Sleep (result * mdelay);
					results.Add (result);
				}, i);
				tasks [i] = task;
			}

			await Task.WhenAll (tasks);

			Assert.AreEqual (runTasks, results.Count);
			foreach (var result in results) {
				Assert.AreEqual (--runTasks, result);
			}
		}

		[TearDown]
		public void TearDown ()
		{
			if (TestScheduler != null) {
				try {
					if (TestScheduler.DedicatedThread.IsAlive)
						TestScheduler.DedicatedThread.Abort ();
					TestScheduler.Dispose ();
				} catch (ObjectDisposedException) {
				} finally {
					TestScheduler = null;
				}
			}
		}
	}
}
