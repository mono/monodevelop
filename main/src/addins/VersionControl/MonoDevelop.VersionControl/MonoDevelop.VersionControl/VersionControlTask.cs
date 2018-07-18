using System;
using System.Threading;
using System.Threading.Tasks;
using Gtk;

using MonoDevelop.Core;

namespace MonoDevelop.VersionControl
{
	internal abstract class VersionControlTask 
	{
		ProgressMonitor tracker;

		protected VersionControlOperationType OperationType { get; set; }
		
		protected abstract string GetDescription();
		
		// This occurs in the background.
		protected abstract void Run();
		
		// This occurs on the main thread when the background
		// task is complete.
		protected virtual void Finished()
		{
		}

		protected VersionControlTask()
		{
			OperationType = VersionControlOperationType.Other;
		}
		
		protected ProgressMonitor Monitor {
			get { return tracker; }
		}
		
		protected virtual ProgressMonitor CreateProgressMonitor ()
		{
			return VersionControlService.GetProgressMonitor (GetDescription (), OperationType);
		}
		
		public void Start() {
			tracker = CreateProgressMonitor ();
			tracker.BeginTask(GetDescription(), 1);

			// Sync invoke background worker which will end up doing async invoke on the internal run.
			BackgroundWorker ();
		}
		
		void BackgroundWorker ()
		{
			Task.Run (() => Run ()).ContinueWith (t => {
				if (t.IsFaulted) {
					var exception = t.Exception.FlattenAggregate ().InnerException;
					if (exception is DllNotFoundException) {
						var msg = GettextCatalog.GetString ("The operation could not be completed because a shared library is missing: ");
						tracker.ReportError (msg + exception.Message, null);
						LoggingService.LogError ("Version Control command failed: ", exception);
					} else if (exception is VersionControlException) {
						var msg = GettextCatalog.GetString ("Version control operation failed: ");
						tracker.ReportError (msg + exception.Message, exception);
					} else {
						var msg = GettextCatalog.GetString ("Version control operation failed: ");
						tracker.ReportError (msg, exception);
					}
				}
				Wakeup ();
			}, Runtime.MainTaskScheduler);
		}

		public void Wakeup() {
			try {
				tracker.EndTask();
				// Remove this when https://github.com/mono/monodevelop/issue/4751 is fixed.
				Runtime.MainSynchronizationContext.Post (o => ((ProgressMonitor)o).Dispose (), tracker);
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
