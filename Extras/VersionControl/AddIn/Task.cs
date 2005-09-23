using System;
using System.Collections;
using System.Threading;

using Gtk;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;

namespace VersionControlPlugin {

	public abstract class Task {
		IProgressMonitor tracker;
		ThreadNotify threadnotify;
		
		protected abstract string GetDescription();
		
		// This occurs in the background.
		protected abstract void Run();
		
		// This occurs on the main thread when the background
		// task is complete.
		protected abstract void Finished();

		protected Task() {
			threadnotify = new ThreadNotify(new ReadyEvent(Wakeup));
			
			/*tracker = ((TaskService)ServiceManager.GetService(typeof(TaskService)))
				.GetStatusProgressMonitor("Version Control", null, true);*/
			tracker = ((TaskService)ServiceManager.GetService(typeof(TaskService)))
				.GetOutputProgressMonitor("Version Control", null, true, true);
		}
		
		public void Start() {
			tracker.BeginTask(GetDescription(), 0);
			new Thread(new ThreadStart(BackgroundWorker)).Start();
		}
		
		void BackgroundWorker() {
			try {
				Run();
				tracker.ReportSuccess("Done.");
			} catch (Exception e) {
				tracker.ReportError(e.Message, null);
				Console.Error.WriteLine(e);
			} finally {			
				threadnotify.WakeupMain();
			}
		}
	
		public void Wakeup() {
			tracker.EndTask();
			tracker.Dispose();
			Finished();
		}
		
		protected void Log(string logtext) {
			tracker.Log.WriteLine(logtext);
		}
		
		protected void Warn(string logtext) {
			tracker.ReportWarning(logtext);
		}
		
	}
}
