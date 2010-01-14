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
using MonoDevelop.Core.Gui.Dialogs;
using Gtk;

namespace MonoDevelop.Ide.Jobs
{
	public class JobEventArgs: EventArgs
	{
		public JobInstance Job { get; internal set; }
	}
	
	public abstract class Job: IDisposable
	{
		public Job ()
		{
			Icon = MonoDevelop.Core.Gui.Stock.OutputIcon;
			SaveInJobHistory = true;
			ShowTaskTitle = true;
		}
		
		public string Title { get; set; }
		
		public string Icon { get; set; }
		
		public string Description { get; set; }
		
		public bool SaveInJobHistory { get; set; }
		
		/// <summary>
		/// When set, the IDE workbench is locked while the job is running
		/// </summary>
		public bool LockGui { get; set; }
		
		/// <summary>
		/// When set, errors (if any) are shown in a dialog when the job ends
		/// </summary>
		public bool ShowErrorsDialog { get; set; }
		
		/// <summary>
		/// When set, the status message is the description of the current task, instead of the title of the job.
		/// </summary>
		public bool ShowTaskTitle { get; set; }
		
		public JobInstance Run ()
		{
			if (LockGui)
				IdeApp.Workbench.LockGui ();
			try {
				JobInstance job = OnRun ();
				if (LockGui) {
					job.Monitor.AsyncOperation.Completed += delegate {
						IdeApp.Workbench.UnlockGui ();
					};
				}
				JobService.RegisterJob (job);
				return job;
			} catch {
				IdeApp.Workbench.UnlockGui ();
				throw;
			}
		}

		protected abstract JobInstance OnRun ();
		
		public virtual bool Reusable {
			get {
				return false;
			}
		}
		
		public virtual void Dispose ()
		{
		}
		
		public object OwnerObject {
			get;
			protected set;
		}
		
		public virtual void FillExtendedStatusPanel (JobInstance jobi, Gtk.HBox expandedPanel, out Gtk.Widget mainWidget)
		{
			Gtk.Button showOutputButton = new Gtk.Button ("Show Log");
			showOutputButton.Relief = ReliefStyle.None;
			showOutputButton.Clicked += delegate {
				if (!jobi.StatusViewVisible)
					jobi.ShowStatusView (showOutputButton, Gtk.PositionType.Top, 0, true);
				else
					jobi.HideStatusView ();
			};
			showOutputButton.Show ();
			expandedPanel.PackStart (showOutputButton, false, false, 0);
			
			Gtk.Button stopButton = new Gtk.Button (Gtk.Stock.Stop);
			stopButton.Relief = ReliefStyle.None;
			stopButton.Clicked += delegate {
				jobi.Monitor.AsyncOperation.Cancel ();
			};
			stopButton.Show ();
			expandedPanel.PackStart (stopButton, false, false, 0);
			expandedPanel.Realized += delegate {
				showOutputButton.Sensitive = jobi.HasStatusView;
				stopButton.Sensitive = !jobi.Monitor.AsyncOperation.IsCompleted;
			};
			mainWidget = showOutputButton;
		}
	}
}
