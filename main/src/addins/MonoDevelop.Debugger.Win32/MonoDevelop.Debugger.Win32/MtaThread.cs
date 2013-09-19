using System;
using System.Threading;

namespace MonoDevelop.Debugger.Win32
{
	static class MtaThread
	{
		static readonly AutoResetEvent wordDoneEvent = new AutoResetEvent (false);
		static Action workDelegate;
		static readonly object workLock = new object ();
		static Thread workThread;
		static Exception workError;
		static readonly object threadLock = new object ();

		public static R Run<R> (Func<R> ts)
		{
			if (Thread.CurrentThread.GetApartmentState () == ApartmentState.MTA)
				return ts ();

			R res = default (R);
			Run (delegate
			{
				res = ts ();
			});
			return res;
		}

		public static void Run (Action ts)
		{
			if (Thread.CurrentThread.GetApartmentState () == ApartmentState.MTA) {
				ts ();
				return;
			}
			lock (workLock) {
				lock (threadLock) {
					workDelegate = ts;
					workError = null;
					if (workThread == null) {
						workThread = new Thread (MtaRunner);
						workThread.Name = "Win32 Debugger MTA Thread";
						workThread.SetApartmentState (ApartmentState.MTA);
						workThread.IsBackground = true;
						workThread.Start ();
					}
					else
						// Awaken the existing thread
						Monitor.Pulse (threadLock);
				}
				wordDoneEvent.WaitOne ();
			}
			if (workError != null)
				throw new Exception ("Debugger operation failed", workError);
		}

		static void MtaRunner ()
		{
			lock (threadLock) {
				do {
					try {
						workDelegate ();
					}
					catch (Exception ex) {
						workError = ex;
					}
					wordDoneEvent.Set ();
				}
				while (Monitor.Wait (threadLock, 60000));

				workThread = null;
			}
		}
	}
}
