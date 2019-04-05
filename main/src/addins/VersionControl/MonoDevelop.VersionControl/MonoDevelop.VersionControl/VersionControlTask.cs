using System;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl
{
	internal abstract class VersionControlTask 
	{
		ProgressMonitor tracker;

		protected VersionControlOperationType OperationType { get; set; }
		
		protected abstract string GetDescription();
		
		// This occurs in the background.
		protected abstract Task RunAsync ();
		
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

		internal Task StartAsync (CancellationToken cancellationToken)
		{
			if (tracker != null)
				tracker = tracker.WithCancellationToken (cancellationToken);
			return StartAsync ();
		}

		public Task StartAsync ()
		{
			tracker = CreateProgressMonitor ();
			tracker.BeginTask(GetDescription(), 1);

			// Sync invoke background worker which will end up doing async invoke on the internal run.
			return Task.Run (async () => await RunAsync ().ConfigureAwait (false)).ContinueWith (t => {
				if (t.IsFaulted) {
					var exception = t.Exception.FlattenAggregate ().InnerException;
					if (exception is DllNotFoundException) {
						var msg = GettextCatalog.GetString ("The operation could not be completed because a shared library is missing: ");
						tracker.ReportError (msg + exception.Message, null);
						LoggingService.LogError ("Version Control command failed: ", exception);
					} else if (exception is VersionControlException) {
						ReportError (exception.Message, exception);
					} else {
						ReportError (exception.Message, exception);
					}
				}
				Wakeup ();
			}, Runtime.MainTaskScheduler);
		}

		public void Wakeup() {
			try {
				tracker.EndTask();
				tracker.Dispose ();
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

		void ReportError (string message, Exception exception)
		{
			string msg = GettextCatalog.GetString ("Version control operation failed");
			tracker.ReportError ($"{msg}: {message}", exception);
			if (IdeApp.Workbench.RootWindow?.Visible == false)
				MessageService.ShowError (msg, message, exception);
		}
	}
}
