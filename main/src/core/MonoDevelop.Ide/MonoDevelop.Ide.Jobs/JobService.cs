// 
// Job.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
namespace MonoDevelop.Ide.Jobs
{
public static class JobService
	{
		static Queue<JobInstance> jobHistory = new Queue<JobInstance> ();
		
		static JobService ()
		{
			IdeApp.Workspace.LastWorkspaceItemClosed += HandleIdeAppWorkspaceLastWorkspaceItemClosed;
		}

		static void HandleIdeAppWorkspaceLastWorkspaceItemClosed (object sender, EventArgs e)
		{
			List<JobInstance> jobs;
			lock (jobHistory) {
				jobs = new List<JobInstance> (jobHistory);
				jobHistory.Clear ();
			}
			foreach (var ji in jobs) {
				Remove (ji);
			}
		}
		
		public static int MaxHistoryJobs {
			get { return PropertyService.Get ("MonoDevelop.Ide.MaxHistoryJobs", 60); }
			set {
				PropertyService.Set ("MonoDevelop.Ide.MaxHistoryJobs", value);
				FixQueueSize ();
			}
		}
		
		public static int MaxHistoryJobStatusViews {
			get { return PropertyService.Get ("MonoDevelop.Ide.MaxHistoryJobStatusViews", 10); }
			set {
				PropertyService.Set ("MonoDevelop.Ide.MaxHistoryJobStatusViews", value);
				FixQueueSize ();
			}
		}
		
		public static JobInstance CurrentJob {
			get {
				lock (jobHistory) {
					return jobHistory.Peek (); 
				}
			}
		}
		
		public static IEnumerable<JobInstance> GetJobHistory ()
		{
			lock (jobHistory) {
				var list = new List<JobInstance> (jobHistory);
				list.Reverse ();
				return list;
			}
		}
		
		internal static void RegisterJob (JobInstance job)
		{
			if (job.Job.SaveInJobHistory) {
				lock (jobHistory) {
					jobHistory.Enqueue (job);
				}
			}
			DispatchService.GuiDispatch (delegate {
				if (JobStarted != null)
					JobStarted (null, new JobEventArgs () { Job = job });
				if (job.Job.SaveInJobHistory) {
					if (JobAdded != null)
						JobAdded (null, new JobEventArgs () { Job = job });
				}
			});
			FixQueueSize ();
		}
		
		static void FixQueueSize ()
		{
			DispatchService.GuiDispatch (delegate {
				lock (jobHistory) {
					while (jobHistory.Count > MaxHistoryJobs)
						Remove (jobHistory.Dequeue ());
					int viewsCount = 0;
					foreach (JobInstance jiba in GetJobHistory ()) {
						if (jiba.HasStatusView) {
							if (viewsCount > MaxHistoryJobStatusViews)
								jiba.DisposeStatusView ();
							else
								viewsCount++;
						}
					}
				}
			});
		}
		
		static void Remove (JobInstance job)
		{
			if (JobRemoved != null)
				JobRemoved (null, new JobEventArgs () { Job = job });
			job.Dispose ();
		}
		
		internal static Gdk.Pixbuf GetComposedIcon (Gdk.Pixbuf s1, Gdk.Pixbuf s2, int gap)
		{
			int wi = System.Math.Max (s1.Width, s2.Width + gap);
			Gdk.Pixbuf res = new Gdk.Pixbuf (s2.Colorspace, s2.HasAlpha, s2.BitsPerSample, wi, System.Math.Max (s1.Height, s2.Height));
			res.Fill (0);
			
			s2.CopyArea (0, 0, s2.Width, s2.Height, res, gap, 0);
			s1.Composite (res, 0, 0, s1.Width, s1.Height, 0, 0, 1, 1, Gdk.InterpType.Bilinear, 255);
			return res;
		}
		
		public static event EventHandler<JobEventArgs> JobStarted;
		public static event EventHandler<JobEventArgs> JobAdded;
		public static event EventHandler<JobEventArgs> JobRemoved;
	}
}
