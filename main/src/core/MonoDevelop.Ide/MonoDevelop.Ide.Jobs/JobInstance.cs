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
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;


namespace MonoDevelop.Ide.Jobs
{
	public class JobInstance: IDisposable
	{
		IProgressMonitor monitor;
		Gtk.Widget statusWidget;
		Job job;
		JobProgressMonitor tracker;
		JobStatusViewWindow currentStatusViewWindow;
		string statusMessage;
		string statusIcon;
		
		public event EventHandler<JobEventArgs> ProgressChanged;
		
		public JobInstance (AggregatedProgressMonitor monitor, Gtk.Widget statusWidget, Job job)
		{
			tracker = new JobProgressMonitor () { Job = this };
			monitor.AddSlaveMonitor (tracker);
			this.monitor = monitor;
			this.statusWidget = statusWidget;
			this.job = job;
			monitor.AsyncOperation.Completed += HandleMonitorAsyncOperationCompleted;
		}
		
		void HandleMonitorAsyncOperationCompleted (IAsyncOperation op)
		{
			if (tracker.Errors.Length > 0 || tracker.Warnings.Length > 0) {
				if (job.ShowErrorsDialog) {
					MultiMessageDialog resultDialog = new MultiMessageDialog ();
					foreach (ProgressError m in tracker.Errors)
						resultDialog.AddError (m.Message);
					foreach (string m in tracker.Warnings)
						resultDialog.AddWarning (m);
					resultDialog.TransientFor = IdeApp.Workbench.RootWindow;
					resultDialog.Run ();
					resultDialog.Destroy ();
				}
			}
		}

		public IProgressMonitor Monitor {
			get { return this.monitor; }
		}

		public Gtk.Widget StatusWidget {
			get { return tracker.HasOutput ? this.statusWidget : null; }
		}

		public Job Job {
			get { return this.job; }
		}
		
		public void ShowStatusView ()
		{
			ShowStatusView (null, Gtk.PositionType.Top, 0, false);
		}
		
		public void ToggleStatusView (Gtk.Widget referenceWidget, Gtk.PositionType relPosition, int gap, bool animate)
		{
			if (StatusViewVisible)
				HideStatusView ();
			else
				ShowStatusView (referenceWidget, relPosition, gap, animate);
		}
		
		public void ShowStatusView (Gtk.Widget referenceWidget, Gtk.PositionType relPosition, int gap, bool animate)
		{
			if (statusWidget == null)
				return;
			if (currentStatusViewWindow == null) {
				currentStatusViewWindow = new JobStatusViewWindow (this);
				currentStatusViewWindow.Resize (800, 400);
			}
			if (referenceWidget != null) {
				int x, y, w, h;
				referenceWidget.GdkWindow.GetOrigin (out x, out y);
				currentStatusViewWindow.GetSize (out w, out h);
				switch (relPosition) {
					case Gtk.PositionType.Left: x -= w; x += gap; break;
					case Gtk.PositionType.Top: y -= h; y += gap; break;
					case Gtk.PositionType.Bottom: y += referenceWidget.Allocation.Height; y += gap; break;
					case Gtk.PositionType.Right: x += referenceWidget.Allocation.Width; x += gap; break;
				}
				currentStatusViewWindow.ShowAndMove (x, y, relPosition, false);
			} else
				currentStatusViewWindow.Show ();
		}
		
		public void HideStatusView ()
		{
			if (currentStatusViewWindow != null)
				currentStatusViewWindow.Hide ();
		}
		
		public bool HasStatusView {
			get { return StatusWidget != null; }
		}
		
		public bool StatusViewVisible {
			get { return currentStatusViewWindow != null && currentStatusViewWindow.Visible; }
		}
		
		public void Dispose ()
		{
			if (statusWidget != null) {
				if (currentStatusViewWindow != null)
					currentStatusViewWindow.Destroy ();
				statusWidget.Destroy ();
			}
		}
		
		public double ProgressFraction {
			get { return tracker.InternalTracker.GlobalWork; }
		}
		
		public string StatusMessage {
			get {
				if (statusMessage != null)
					return statusMessage;
				else if (!monitor.AsyncOperation.IsCompleted) {
					if (job.ShowTaskTitle)
						return tracker.InternalTracker.CurrentTask;
					else
						return job.Title;
				}
				else if (tracker.ResultMessage != null)
					return tracker.ResultMessage;
				else
					return GettextCatalog.GetString ("Done");
			}
			set {
				statusMessage = value;
				NotifyProgressChanged ();
			}
		}
		
		public string StatusIcon {
			get {
				if (!monitor.AsyncOperation.IsCompleted)
					return job.Icon;
				if (!monitor.AsyncOperation.Success)
					return Gtk.Stock.DialogError;
				else if (monitor.AsyncOperation.SuccessWithWarnings)
					return Gtk.Stock.DialogWarning;
				else
					return job.Icon;
			}
		}
		
		public string ComposedStatusIcon {
			get {
				if (!monitor.AsyncOperation.IsCompleted)
					return job.Icon;
				if (statusIcon != null)
					return statusIcon;
				if (!monitor.AsyncOperation.Success)
					statusIcon = Gtk.Stock.DialogError;
				else if (monitor.AsyncOperation.SuccessWithWarnings)
					statusIcon = Gtk.Stock.DialogWarning;
				else {
					statusIcon = job.Icon;
					return statusIcon;
				}
				Gdk.Pixbuf s2 = ImageService.GetPixbuf (statusIcon, Gtk.IconSize.Menu);
				Gdk.Pixbuf s1 = ImageService.GetPixbuf (job.Icon, Gtk.IconSize.Menu);
				Gdk.Pixbuf res = JobService.GetComposedIcon (s1, s2, 12);
				statusIcon = ImageService.GetStockId (res, Gtk.IconSize.Menu);
				return statusIcon;
			}
		}
		
		internal void NotifyProgressChanged ()
		{
			DispatchService.GuiDispatch (delegate {
				if (ProgressChanged != null)
					ProgressChanged (this, new JobEventArgs () { Job = this });
			});
		}

		public void DisposeStatusView ()
		{
			if (statusWidget == null)
				return;
			if (currentStatusViewWindow != null)
				currentStatusViewWindow.Destroy ();
			else
				statusWidget.Destroy ();
			statusWidget = null;
		}
	}

	// This class helps keeping track of the progress of the job.
	// The job instance needs to know when progress changes and when
	// the current task changes
	class JobProgressMonitor: SimpleProgressMonitor
	{
		public JobInstance Job;
		public string ResultMessage;
		public bool HasOutput;
		
		public ProgressTracker InternalTracker {
			get { return Tracker; }
		}
		
		protected override void OnProgressChanged ()
		{
			Job.NotifyProgressChanged ();
		}
		
		public override void ReportSuccess (string message)
		{
			ResultMessage = message;
			base.ReportSuccess (message);
		}
		
		public override void ReportError (string message, Exception ex)
		{
			ResultMessage = message;
			base.ReportError (message, ex);
		}

		public override void ReportWarning (string message)
		{
			ResultMessage = message;
			base.ReportWarning (message);
		}
		
		public override void BeginStepTask (string name, int totalWork, int stepSize)
		{
			HasOutput = true;
			base.BeginStepTask (name, totalWork, stepSize);
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			HasOutput = true;
			base.BeginTask (name, totalWork);
		}
		
		public override System.IO.TextWriter Log {
			get {
				HasOutput = true;
				return base.Log;
			}
		}
	}
}
