//
// JobStatus.cs
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
using System.Collections.Generic;

namespace MonoDevelop.CodeIssues
{
	/// <summary>
	/// Keeps track of the status of a possibly partially executed job.
	/// </summary>
	public class JobStatus
	{
		readonly object _lock = new object();

		/// <summary>
		/// The job.
		/// </summary>
		readonly IAnalysisJob job;

		/// <summary>
		/// The slices that the job has been split into.
		/// </summary>
		readonly ISet<JobSlice> slices = new HashSet<JobSlice> ();

		/// <summary>
		/// Initializes a new instance of the <see cref="MonoDevelop.CodeIssues.JobStatus"/> class.
		/// </summary>
		/// <param name="job">The job.</param>
		public JobStatus (IAnalysisJob job)
		{
			if (job == null)
				throw new ArgumentNullException ("job");
			
			this.job = job;
		}

		/// <summary>
		/// Adds another slice. This method should not be called after <see cref="MarkAsComplete"/> has been called.
		/// </summary>
		/// <param name="slice">Slice.</param>
		public void AddSlice (JobSlice slice)
		{
			lock (_lock) {
				slices.Add (slice);
			}
		}

		/// <summary>
		/// Marks a slice of the job as complete and marks the job as completed if all slices have been completed.
		/// </summary>
		/// <param name="slice">The completed slice.</param>
		public void MarkAsComplete(JobSlice slice)
		{
			lock (_lock) {
				slices.Remove (slice);
				if (slices.Count == 0) {
					job.SetCompleted ();
				}
			}
		}
	}
}

