using System;
using System.Threading;
using System.Collections.Generic;
using SThread = System.Threading.Thread;

namespace Sharpen
{
	internal class Executors
	{
		static ThreadFactory defaultThreadFactory = new ThreadFactory ();
		
		public static ExecutorService NewFixedThreadPool (int threads)
		{
			return new FixedThreadPoolExecutorService ();
		}
		
		public static ThreadFactory DefaultThreadFactory ()
		{
			return defaultThreadFactory;
		}
	}
	
	internal class FixedThreadPoolExecutorService: ExecutorService
	{
		List<WaitHandle> tasks = new List<WaitHandle> ();
		bool shuttingDown;
		
		#region ExecutorService implementation
		public bool AwaitTermination (long n, Sharpen.TimeUnit unit)
		{
			WaitHandle[] handles;
			lock (tasks) {
				if (tasks.Count == 0)
					return true;
				handles = tasks.ToArray ();
			}
			return WaitHandle.WaitAll (handles, (int) unit.Convert (n, TimeUnit.MILLISECONDS));
		}
	
		public void Shutdown ()
		{
			lock (tasks) {
				shuttingDown = true;
			}
		}
	
		public Future<T> Submit<T> (Sharpen.Callable<T> c)
		{
			TaskFuture<T> future = new TaskFuture<T> (this);
			lock (tasks) {
				if (shuttingDown)
					throw new RejectedExecutionException ();
				tasks.Add (future.DoneEvent);
				ThreadPool.QueueUserWorkItem (delegate {
					future.Run (c);
				});
			}
			return future;
		}
		
		internal void RemoveTask (WaitHandle handle)
		{
			lock (tasks) {
				tasks.Remove (handle);
			}
		}
		
		#endregion
	
		#region Executor implementation
		public void Execute (Sharpen.Runnable runnable)
		{
			throw new System.NotImplementedException ();
		}
		#endregion
	}
	
	internal interface FutureBase
	{
	}
	
	class TaskFuture<T>: Future<T>, FutureBase
	{
		SThread t;
		T result;
		ManualResetEvent doneEvent = new ManualResetEvent (false);
		Exception error;
		bool canceled;
		bool started;
		bool done;
		FixedThreadPoolExecutorService service;
		
		public TaskFuture (FixedThreadPoolExecutorService service)
		{
			this.service = service;
		}
		
		public WaitHandle DoneEvent {
			get { return doneEvent; }
		}
		
		public void Run (Callable<T> c)
		{
			try {
				lock (this) {
					if (canceled)
						return;
					t = SThread.CurrentThread;
					started = true;
				}
				result = c.Call ();
			} catch (ThreadAbortException ex) {
				SThread.ResetAbort ();
				error = ex;
			} catch (Exception ex) {
				error = ex;
			} finally {
				lock (this) {
					done = true;
					service.RemoveTask (doneEvent);
				}
				doneEvent.Set ();
			}
		}
		
		public bool Cancel (bool mayInterruptIfRunning)
		{
			lock (this) {
				if (done || canceled)
					return false;
				canceled = true;
				doneEvent.Set ();
				if (started) {
					if (mayInterruptIfRunning) {
						try {
							t.Abort ();
						} catch {}
					}
					else
						return false;
				}
				return true;
			}
		}
		
		public T Get ()
		{
			doneEvent.WaitOne ();
			if (canceled)
				throw new CancellationException ();
			
			if (error != null)
				throw new ExecutionException (error);
			else
				return result;
		}
	}
	
	internal class CancellationException: Exception
	{
	}
	
	internal class RejectedExecutionException: Exception
	{
	}
}
