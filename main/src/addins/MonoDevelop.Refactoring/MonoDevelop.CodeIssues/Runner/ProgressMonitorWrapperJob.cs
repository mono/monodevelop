//
// ProgressReportingWrapperJob.cs
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
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System;

namespace MonoDevelop.CodeIssues
{
	public class ProgressMonitorWrapperJob : IAnalysisJob
	{
		readonly IAnalysisJob wrappedJob;

		IProgressMonitor monitor;

		int reportingThinningFactor = 100;

		int completedWork;

		public ProgressMonitorWrapperJob (IAnalysisJob wrappedJob, string message)
		{
			this.wrappedJob = wrappedJob;
			monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (message, null, false);
			var work = wrappedJob.GetFiles ().Sum (f => wrappedJob.GetIssueProviders (f).Count ());
			
			monitor.BeginTask (message, work);
		}

		#region IAnalysisJob implementation

		public event EventHandler<CodeIssueEventArgs> CodeIssueAdded {
			add {
				wrappedJob.CodeIssueAdded += value;
			}
			remove {
				wrappedJob.CodeIssueAdded -= value;
			}
		}

		public IEnumerable<ProjectFile> GetFiles ()
		{
			return wrappedJob.GetFiles ();
		}

		public IEnumerable<BaseCodeIssueProvider> GetIssueProviders (ProjectFile file)
		{
			return wrappedJob.GetIssueProviders (file);
		}

		public void AddResult (ProjectFile file, BaseCodeIssueProvider provider, IEnumerable<CodeIssue> issues)
		{
			Step ();
			wrappedJob.AddResult (file, provider, issues);
		}

		public void AddError (ProjectFile file, BaseCodeIssueProvider provider)
		{
			Step ();
			wrappedJob.AddError (file, provider);
		}

		public event EventHandler<EventArgs> Completed {
			add {
				wrappedJob.Completed += value;
			}
			remove {
				wrappedJob.Completed -= value;
			}
		}

		public void SetCompleted ()
		{
			StopReporting ();
			wrappedJob.SetCompleted ();
		}

		void Step ()
		{
			completedWork++;
			if (monitor != null && completedWork % reportingThinningFactor == 0) {
				monitor.Step (reportingThinningFactor);
			}
		}

		void StopReporting ()
		{
			if (monitor != null) {
				monitor.Dispose ();
				monitor = null;
			}
		}

		public void NotifyCancelled ()
		{
			StopReporting ();
		}

		#endregion
	}
}

