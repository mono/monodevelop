
using System;
using Mono.Addins;

namespace MonoDevelop.Core.ProgressMonitoring
{
	public class ProgressStatusMonitor: MarshalByRefObject, IProgressStatus, IDisposable
	{
		IProgressMonitor monitor;
		int step;
		
		public ProgressStatusMonitor (IProgressMonitor monitor)
		{
			this.monitor = monitor;
			monitor.BeginTask ("", 100);
		}
		
		public void SetMessage (string msg)
		{
			monitor.EndTask ();
			monitor.BeginTask (msg, 100 - step);
		}
		
		public void SetProgress (double progress)
		{
			int ns = (int) (progress * 100);
			monitor.Step (ns - step);
			step = ns;
		}
		
		public void Log (string msg)
		{
			monitor.Log.WriteLine (msg);
		}
		
		public void ReportWarning (string message)
		{
			monitor.ReportWarning (message);
		}
		
		public void ReportError (string message, Exception exception)
		{
			monitor.ReportError (message, exception);
		}
		
		public bool IsCanceled {
			get { return monitor.IsCancelRequested; }
		}
		
		public int LogLevel {
			get { return 1; }
		}
		
		public void Cancel ()
		{
			monitor.AsyncOperation.Cancel ();
		}
		
		public void Dispose ()
		{
			monitor.EndTask ();
		}
	}
}

