//
// AnalysisJobQueueTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using MonoDevelop.CodeIssues;
using MonoDevelop.Projects;
using System.Linq;

namespace MonoDevelop.Refactoring
{
	[TestFixture]
	public class AnalysisJobQueueTests
	{
		AnalysisJobQueue queue;

		ProjectFile file1;

		ProjectFile file2;

		ProjectFile file3;

		SimpleAnalysisJob job;

		[SetUp]
		public void SetUp ()
		{
			queue = new AnalysisJobQueue ();
			file1 = new ProjectFile ("file1.cs");
			file2 = new ProjectFile ("file2.cs");
			file3 = new ProjectFile ("file3.cs");
			job = new SimpleAnalysisJob (new [] { file1, file2 });
		}

		[Test]
		public void SimpleEnqueueDequeue ()
		{
			queue.Add (job);
			var dequeued = queue.Dequeue (1).ToList ();
			Assert.AreEqual (1, dequeued.Count(), "Wrong number of items dequeued");
			var queueItem = dequeued.First ();
			Assert.AreEqual (1, queueItem.GetJobs ().Count (), "Wrong number of jobs for the file");
			Assert.AreEqual (job, queueItem.GetJobs ().First (), "Wrong job");
		}

		[Test]
		public void MultipleJobsPerFile ()
		{
			queue.Add (job);
			queue.Add (new SimpleAnalysisJob (new [] { file1 }));
			queue.Add (new SimpleAnalysisJob (new [] { file2 }));
			var dequeued = queue.Dequeue (1);
			Assert.AreEqual (1, dequeued.Count(), "Wrong number of items dequeued");
		}

		[Test]
		public void MergesItemsAffectingSameFile ()
		{
			queue.Add (job);
			queue.Add (new SimpleAnalysisJob (new [] { file1 }));
			queue.Add (new SimpleAnalysisJob (new [] { file2 }));
			var two = queue.Dequeue (3);
			Assert.AreEqual (2, two.Count(), "Wrong number of items dequeued");
		}

		[Test]
		public void DequeueMultiple ()
		{
			queue.Add (job);
			queue.Add (new SimpleAnalysisJob (new [] { file1 }));
			queue.Add (new SimpleAnalysisJob (new [] { file2 }));
			queue.Add (new SimpleAnalysisJob (new [] { file3 }));
			var two = queue.Dequeue (2);
			Assert.AreEqual (2, two.Count(), "Wrong number of items dequeued when more than need items are available");
			var one = queue.Dequeue (2);
			Assert.AreEqual (1, one.Count(), "Wrong number of items dequeued when enough items are not available");
		}

		[Test]
		public void SetsJobAsCompletedAfterAllSlicesHaveBeenDisposed ()
		{
			queue.Add (job);
			queue.Dequeue (1).First ().Dispose ();
			Assert.IsFalse (job.IsCompleted, "should now be completed yet");
			queue.Dequeue (1).First ().Dispose ();
			Assert.IsTrue (job.IsCompleted, "should be completed");

		}
	}
}

