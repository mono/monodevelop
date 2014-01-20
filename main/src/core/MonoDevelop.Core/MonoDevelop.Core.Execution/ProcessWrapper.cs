// created on 12/18/2004 at 16:28
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MonoDevelop.Core.Execution
{
	public delegate void ProcessEventHandler(object sender, string message);

	[System.ComponentModel.DesignerCategory ("Code")]
	public class ProcessWrapper : Process
	{
		private Thread captureErrorThread;
		ManualResetEvent endEventOut = new ManualResetEvent (false);
		ManualResetEvent endEventErr = new ManualResetEvent (false);
		bool done;
		object lockObj = new object ();
		Task task;
		ProcessAsyncOperation operation;
		IDisposable customCancelToken;
		
		public ProcessWrapper ()
		{
		}
		public bool CancelRequested { get; private set; }

		public Task Task {
			get { return task; }
		}

		public ProcessAsyncOperation ProcessAsyncOperation {
			get { return operation; }
		}

		public new void Start ()
		{
			CheckDisposed ();
			base.Start ();

			task = Task.Factory.StartNew (CaptureOutput, TaskCreationOptions.LongRunning);
			var cs = new CancellationTokenSource ();
			operation = new ProcessAsyncOperation (task, cs);
			cs.Token.Register (Cancel);

			if (ErrorStreamChanged != null) {
				captureErrorThread = new Thread (new ThreadStart(CaptureError));
				captureErrorThread.Name = "Process error reader";
				captureErrorThread.IsBackground = true;
				captureErrorThread.Start ();
			} else {
				endEventErr.Set ();
			}
			operation.ProcessId = Id;
		}

		public void SetCancellationToken (CancellationToken cancelToken)
		{
			customCancelToken = cancelToken.Register (Cancel);
		}
		
		public void WaitForOutput (int milliseconds)
		{
			CheckDisposed ();
			WaitForExit (milliseconds);
			endEventOut.WaitOne ();
		}
		
		public void WaitForOutput ()
		{
			WaitForOutput (-1);
		}
		
		private void CaptureOutput ()
		{
			Thread.CurrentThread.Name = "Process output reader";
			try {
				if (OutputStreamChanged != null) {
					char[] buffer = new char [1024];
					int nr;
					while ((nr = StandardOutput.Read (buffer, 0, buffer.Length)) > 0) {
						if (OutputStreamChanged != null)
							OutputStreamChanged (this, new string (buffer, 0, nr));
					}
				}
			} catch (ThreadAbortException) {
				// There is no need to keep propagating the abort exception
				Thread.ResetAbort ();
			} finally {
				// WORKAROUND for "Bug 410743 - wapi leak in System.Diagnostic.Process"
				// Process leaks when an exit event is registered
				if (endEventErr != null)
					endEventErr.WaitOne ();

				if (HasExited)
					operation.ExitCode = ExitCode;

				OnExited (this, EventArgs.Empty);

				lock (lockObj) {
					//call this AFTER the exit event, or the ProcessWrapper may get disposed and abort this thread
					if (endEventOut != null)
						endEventOut.Set ();
				}
			}
		}
		
		private void CaptureError ()
		{
			try {
				char[] buffer = new char [1024];
				int nr;
				while ((nr = StandardError.Read (buffer, 0, buffer.Length)) > 0) {
					if (ErrorStreamChanged != null)
						ErrorStreamChanged (this, new string (buffer, 0, nr));
				}					
			} finally {
				lock (lockObj) {
					if (endEventErr != null)
						endEventErr.Set ();
				}
			}
		}
		
		protected override void Dispose (bool disposing)
		{
			lock (lockObj) {
				if (endEventOut == null)
					return;
				
				if (!done)
					Cancel ();
				
				captureErrorThread = null;
				endEventOut.Close ();
				endEventErr.Close ();
				endEventOut = endEventErr = null;
			}

			// HACK: try/catch is a workaround for broken Process.Dispose implementation in Mono < 3.2.7
			// https://bugzilla.xamarin.com/show_bug.cgi?id=10883
			try {
				base.Dispose (disposing);
			} catch {
				if (disposing)
					throw;
			}
		}
		
		void CheckDisposed ()
		{
			if (endEventOut == null)
				throw new ObjectDisposedException ("ProcessWrapper");
		}
		
		public void Cancel ()
		{
			try {
				if (!done) {
					try {
						CancelRequested = true;
						this.KillProcessTree ();
					} catch {
						// Ignore
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}

		void OnExited (object sender, EventArgs args)
		{
			if (customCancelToken != null) {
				customCancelToken.Dispose ();
				customCancelToken = null;
			}
			try {
				if (!HasExited)
					WaitForExit ();
			} catch {
				// Ignore
			} finally {
				lock (lockObj) {
					done = true;
				}
			}
		}
		
		public event ProcessEventHandler OutputStreamChanged;
		public event ProcessEventHandler ErrorStreamChanged;
	}
}
