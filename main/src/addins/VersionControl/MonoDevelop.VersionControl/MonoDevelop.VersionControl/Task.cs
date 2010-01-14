using System;
using System.Collections;
using System.Threading;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Jobs;

namespace MonoDevelop.VersionControl
{
	internal abstract class Task: OutputPadJob
	{
		IProgressMonitor tracker;
		ThreadNotify threadnotify;
		
		protected abstract string GetDescription();
		
		// This occurs in the background.
		protected abstract void RunTask ();
		
		// This occurs on the main thread when the background
		// task is complete.
		protected virtual void Finished()
		{
		}

		protected Task()
		{
			Title = "Version Control";
			Icon = "md-version-control";
			threadnotify = new ThreadNotify(new ReadyEvent(Wakeup));
		}
		
		protected IProgressMonitor GetProgressMonitor ()
		{
			return tracker;
		}
		
		protected override void OnRun (IProgressMonitor monitor)
		{
			tracker = monitor;
			tracker.BeginTask(GetDescription(), 0);
			new Thread(new ThreadStart(BackgroundWorker)) {
				Name = "VCS background tasks",
				IsBackground = true,
			}.Start();
		}
		
		void BackgroundWorker() {
			try {
				RunTask();
				tracker.ReportSuccess(GettextCatalog.GetString ("Done."));
			} catch (DllNotFoundException e) {
				tracker.ReportError("The operation could not be completed because a shared library is missing: " + e.Message, null);
			} catch (Exception e) {
				tracker.ReportError(e.Message, null);
				Console.Error.WriteLine(e);
			} finally {			
				threadnotify.WakeupMain();
			}
		}
	
		public void Wakeup() {
			try {
				tracker.EndTask();
				tracker.Dispose();
			} finally {
				Finished();
			}
		}
		
		protected void Log(string logtext) {
			tracker.Log.WriteLine(logtext);
		}
		
		protected void Warn(string logtext) {
			tracker.ReportWarning(logtext);
		}
	}
}
