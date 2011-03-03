using System;
using System.Collections.Generic;
using ST = System.Threading;

namespace Sharpen
{
	class ThreadPoolExecutor
	{
		ThreadFactory tf;
		int corePoolSize;
		int maxPoolSize;
		List<Thread> pool = new List<Thread> ();
		int runningThreads;
		int freeThreads;
		bool shutdown;
		Queue<Runnable> pendingTasks = new Queue<Runnable> ();
		
		public ThreadPoolExecutor (int corePoolSize, ThreadFactory factory)
		{
			this.corePoolSize = corePoolSize;
			maxPoolSize = corePoolSize;
			tf = factory;
		}
		
		public void SetMaximumPoolSize (int size)
		{
			maxPoolSize = size;
		}
		
		public bool IsShutdown ()
		{
			return shutdown;
		}
		
		public virtual bool IsTerminated ()
		{
			lock (pendingTasks) {
				return shutdown && pendingTasks.Count == 0;
			}
		}
		
		public virtual bool IsTerminating ()
		{
			lock (pendingTasks) {
				return shutdown && !IsTerminated ();
			}
		}
		
		public int GetCorePoolSize ()
		{
			return corePoolSize;
		}
		
		public void PrestartAllCoreThreads ()
		{
			lock (pendingTasks) {
				while (runningThreads < corePoolSize)
					StartPoolThread ();
			}
		}
		
		public void SetThreadFactory (ThreadFactory f)
		{
			tf = f;
		}
		
		public void Execute (Runnable r)
		{
			InternalExecute (r, true);
		}
		
		internal void InternalExecute (Runnable r, bool checkShutdown)
		{
			lock (pendingTasks) {
				if (shutdown && checkShutdown)
					throw new InvalidOperationException ();
				if (runningThreads < corePoolSize) {
					StartPoolThread ();
				}
				else if (freeThreads > 0) {
					freeThreads--;
				}
				else if (runningThreads < maxPoolSize) {
					StartPoolThread ();
				}
				pendingTasks.Enqueue (r);
				ST.Monitor.PulseAll (pendingTasks);
			}
		}
		
		void StartPoolThread ()
		{
			runningThreads++;
			pool.Add (tf.NewThread (new RunnableAction (RunPoolThread)));
		}
		
		public void RunPoolThread ()
		{
			while (!IsTerminated ()) {
				try {
					Runnable r = null;
					lock (pendingTasks) {
						freeThreads++;
						while (!IsTerminated () && pendingTasks.Count == 0)
							ST.Monitor.Wait (pendingTasks);
						if (IsTerminated ())
							break;
						r = pendingTasks.Dequeue ();
					}
					if (r != null)
						r.Run ();
				}
				catch (ST.ThreadAbortException) {
					ST.Thread.ResetAbort ();
				}
				catch {
				}
			}
		}
		
		public virtual void Shutdown ()
		{
			lock (pendingTasks) {
				shutdown = true;
				ST.Monitor.PulseAll (pendingTasks);
			}
		}
		
		public virtual List<Runnable> ShutdownNow ()
		{
			lock (pendingTasks) {
				shutdown = true;
				foreach (var t in pool) {
					try {
						t.Abort ();
					} catch {}
				}
				pool.Clear ();
				freeThreads = 0;
				runningThreads = 0;
				var res = new List<Runnable> (pendingTasks);
				pendingTasks.Clear ();
				return res;
			}
		}
	}
	
	class RunnableAction: Runnable
	{
		Action action;
		
		public RunnableAction (Action a)
		{
			action = a;
		}
		
		public void Run ()
		{
			action ();
		}
	}
}
