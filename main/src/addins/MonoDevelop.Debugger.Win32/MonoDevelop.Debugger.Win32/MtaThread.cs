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

		public static R Run<R> (Func<R> ts, int timeout = 15000)
		{
			if (Thread.CurrentThread.GetApartmentState () == ApartmentState.MTA)
				return ts ();

			R res = default (R);
			Run (delegate {
				res = ts ();
			}, timeout);
			return res;
		}

		public static void Run (Action ts, int timeout = 15000)
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
					} else
						// Awaken the existing thread
						Monitor.Pulse (threadLock);
				}
				if (!wordDoneEvent.WaitOne (timeout)) {
					workThread.Abort ();
					throw new Exception ("Debugger operation timeout on MTA thread.");
				}
			}
			if (workError != null)
				throw new Exception ("Debugger operation failed", workError);
		}

		static void MtaRunner ()
		{
			try {
				lock (threadLock) {
					do {
						try {
							workDelegate ();
						} catch (ThreadAbortException) {
							return;
						} catch (Exception ex) {
							workError = ex;
						} finally {
							workDelegate = null;
						}
						wordDoneEvent.Set ();
					} while (Monitor.Wait (threadLock, 60000));

				}
			} catch {
				//Just in case if we abort just in moment when it leaves workDelegate ();
			} finally {
				workThread = null;
			}
		}
	}
}
