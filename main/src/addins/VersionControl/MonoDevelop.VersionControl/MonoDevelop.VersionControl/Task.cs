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
		
		protected abstract string GetDescription();
		
		// This occurs in the background.
		protected abstract void Run();
		
		// This occurs on the main thread when the background
		// task is complete.
		protected virtual void Finished()
		{
		}

		protected Task() {
			threadnotify = new ThreadNotify(new ReadyEvent(Wakeup));
			
			tracker = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("Version Control", "md-version-control", true, true);
		}
		
		protected IProgressMonitor Monitor {
			get { return tracker; }
		}
		
		public void Start() {
			tracker.BeginTask(GetDescription(), 0);
			new Thread(new ThreadStart(BackgroundWorker)) {
				Name = "VCS background tasks",
				IsBackground = true,
			}.Start();
		}
		
		void BackgroundWorker() {
			try {
				Run();
			} catch (DllNotFoundException e) {
				tracker.ReportError("The operation could not be completed because a shared library is missing: " + e.Message, null);
			} catch (Exception e) {
				string msg = GettextCatalog.GetString ("Version control operation failed: ");
				msg += e.Message;
				tracker.ReportError (msg, null);
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
