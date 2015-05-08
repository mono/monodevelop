//
// AnalysisJobQueue.cs
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
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.CodeIssues
{
	public class AnalysisJobQueue
	{
		readonly object _lock = new object();

		/// <summary>
		/// The list of items in the queue.
		/// </summary>
		readonly List<JobSlice> slices = new List<JobSlice>();

		/// <summary>
		/// Indicates whether queueItems is sorted.
		/// </summary>
		bool sorted;

		/// <summary>
		/// Adds the specified job to the queue.
		/// </summary>
		/// <param name="job">The job.</param>
		public void Add (IAnalysisJob job)
		{
			lock (_lock) {
				var jobStatus = new JobStatus (job);
				foreach (var file in job.GetFiles()) {
					JobSlice slice = slices.FirstOrDefault (j => j.File == file);
					if (slice == null) {
						slice = new JobSlice (file);
						slices.Add (slice);
					}
					jobStatus.AddSlice (slice);
					slice.AddJob (job, jobStatus);
				}
				InvalidateSort ();
			}
		}

		/// <summary>
		/// Remove the specified job from the queue.
		/// </summary>
		/// <param name="job">The job to remove.</param>
		public void Remove (IAnalysisJob job)
		{
			lock (_lock) {
				foreach (var file in job.GetFiles()) {
					JobSlice queueItem = slices.FirstOrDefault (j => j.File == file);
					if (queueItem == null) 
						// The file might have been processed already, carry on
						continue;
					queueItem.RemoveJob (job);
					if (!queueItem.GetJobs ().Any ())
						slices.Remove (queueItem);
				}
				InvalidateSort ();
			}
		}

		/// <summary>
		/// Dequeues a number of elements less than or equal to <paramref name="maxNumber"/>.
		/// </summary>
		/// <param name="maxNumber">The index.</param>
		public IEnumerable<JobSlice> Dequeue (int maxNumber)
		{
			lock (_lock) {
				EnsureSorted ();
				var taken = slices.Take (maxNumber).ToList ();
				foreach (var item in taken)
					slices.Remove (item);
				return taken;
			}
		}

		/// <summary>
		/// Notifies the rest of the class that <see cref="slices"/> is no longer sorted.
		/// </summary>
		void InvalidateSort ()
		{
			sorted = false;
		}

		/// <summary>
		/// Ensures that <see cref="slices"/> is sorted.
		/// </summary>
		void EnsureSorted ()
		{
			if (!sorted) {
				slices.Sort ();
				sorted = true;
			}
		}
	}
}

