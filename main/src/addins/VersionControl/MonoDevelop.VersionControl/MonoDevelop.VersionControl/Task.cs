using System;
using System.Threading;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	internal abstract class Task 
	{
		IProgressMonitor tracker;
		ThreadNotify threadnotify;

		protected VersionControlOperationType OperationType { get; set; }
		
		protected abstract string GetDescription();
		
		// This occurs in the background.
		protected abstract void Run();
		
		// This occurs on the main thread when the background
		// task is complete.
		protected virtual void Finished()
		{
		}

		protected Task()
		{
			OperationType = VersionControlOperationType.Other;
			threadnotify = new ThreadNotify(new ReadyEvent(Wakeup));
		}
		
		protected IProgressMonitor Monitor {
			get { return tracker; }
		}
		
		protected virtual IProgressMonitor CreateProgressMonitor ()
		{
			return VersionControlService.GetProgressMonitor (GetDescription (), OperationType);
		}
		
		public void Start() {
			tracker = CreateProgressMonitor ();
			tracker.BeginTask(GetDescription(), 1);
			ThreadPool.QueueUserWorkItem (BackgroundWorker);
		}
		
		void BackgroundWorker (object state)
		{
			try {
				Run();
			} catch (DllNotFoundException e) {
				tracker.ReportError("The operation could not be completed because a shared library is missing: " + e.Message, null);
			} catch (Exception e) {
				string msg = GettextCatalog.GetString ("Version control operation failed: ");
				tracker.ReportError (msg, e);
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
