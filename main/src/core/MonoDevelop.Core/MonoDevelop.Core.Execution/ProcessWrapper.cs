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
		bool disposed;
		readonly object lockObj = new object ();
		ProcessAsyncOperation operation;
		IDisposable customCancelToken;
		IDisposable completionRegistration;
		readonly TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int> ();
		
		public ProcessWrapper ()
		{
			EnableRaisingEvents = true;
			Exited += OnExited;
		}

		public bool CancelRequested { get; private set; }

		public Task Task => taskCompletionSource.Task;
		public ProcessAsyncOperation ProcessAsyncOperation => operation;

		public new void Start ()
		{
			CheckDisposed ();

			base.Start ();

			var cs = new CancellationTokenSource ();
			completionRegistration = cs.Token.Register (() => {
				taskCompletionSource.TrySetResult (operation.ExitCode);
				Cancel ();
			});

			// We need these wrappers, as the alternatives are not good enough.
			// OutputDataReceived does not persist newlines.
			if (OutputStreamChanged != null) {
				Task.Run (CaptureOutput, cs.Token);
			}

			if (ErrorStreamChanged != null) {
				Task.Run (CaptureError, cs.Token);
			}

			operation = new ProcessAsyncOperation (Task, cs) {
				ProcessId = Id,
			};
		}

		async Task CaptureOutput ()
		{
			char [] buffer = new char [1024];
			int nr;
			while ((nr = await StandardOutput.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false)) > 0) {
				OutputStreamChanged?.Invoke (this, new string (buffer, 0, nr));
			}
		}

		async Task CaptureError ()
		{
			char [] buffer = new char [1024];
			int nr;
			while ((nr = await StandardError.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false)) > 0) {
				ErrorStreamChanged?.Invoke (this, new string (buffer, 0, nr));
			}
		}

		public void SetCancellationToken (CancellationToken cancelToken)
		{
			customCancelToken = cancelToken.Register (Cancel);
		}
		
		public void WaitForOutput (int milliseconds)
		{
			CheckDisposed ();
			WaitForExit (milliseconds);
		}
		
		public void WaitForOutput ()
		{
			WaitForOutput (-1);
		}
		
		protected override void Dispose (bool disposing)
		{
			lock (lockObj) {
				Cancel ();
				disposed = true;
			}

			base.Dispose (disposing);
		}
		
		void CheckDisposed ()
		{
			lock (lockObj)
				if (disposed)
					throw new ObjectDisposedException ("ProcessWrapper");
		}
		
		public void Cancel ()
		{
			try {
				if (!HasExited) {
					CancelRequested = true;
					this.KillProcessTree ();
				}
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
		}

		static void OnExited (object sender, EventArgs args)
		{
			var pw = (ProcessWrapper)sender;
			pw.OnProcessWrapperExited ();
		}

		void OnProcessWrapperExited ()
		{
			customCancelToken?.Dispose ();
			customCancelToken = null;

			try {
				if (!HasExited)
					WaitForExit ();
				taskCompletionSource.TrySetResult (operation.ExitCode = ExitCode);
			} catch {
				// Ignore
			}
			completionRegistration?.Dispose ();
			completionRegistration = null;
		}
		
		public event ProcessEventHandler OutputStreamChanged;
		public event ProcessEventHandler ErrorStreamChanged;
	}
}
