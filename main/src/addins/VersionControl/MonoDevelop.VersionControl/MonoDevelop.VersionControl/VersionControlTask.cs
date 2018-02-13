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
		
		async void BackgroundWorker ()
		{
			try {
				await Task.Run (() => Run ());
			} catch (DllNotFoundException e) {
				string msg = GettextCatalog.GetString ("The operation could not be completed because a shared library is missing: ");
				tracker.ReportError (msg + e.Message, null);
				LoggingService.LogError ("Version Control command failed: ", e);
			} catch (VersionControlException e) {
				string msg = GettextCatalog.GetString ("Version control operation failed: ");
				tracker.ReportError (msg + e.Message, e);
				LoggingService.LogError ("Version Control command failed: ", e);
			} catch (Exception e) {
				string msg = GettextCatalog.GetString ("Version control operation failed: ");
				tracker.ReportError (msg, e);
				LoggingService.LogError ("Version Control command failed: ", e);
			} finally {
				Wakeup ();
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
