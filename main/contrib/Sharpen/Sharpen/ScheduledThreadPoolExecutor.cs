using System;
using System.Linq;
using System.Collections.Generic;
using ST = System.Threading;

namespace Sharpen
{
	internal class ScheduledThreadPoolExecutor: ThreadPoolExecutor
	{
		bool continueExistingPeriodicTasksAfterShutdownPolicy;
		bool executeExistingDelayedTasksAfterShutdownPolicy = true;
		
		class Task<T>: Runnable, Future<T>, IScheduledITask
		{
			Thread thread;
			bool canceled;
			bool completed;
			ST.ManualResetEvent doneEvent = new ST.ManualResetEvent (false);
			
			public Runnable Action;
			public DateTime DueTime { get; set; }
			
			public ScheduledThreadPoolExecutor Executor { get; set; }
			
			public object Owner {
				get { return Executor; }
			}
			
			public void Start ()
			{
				lock (this) {
					if (canceled)
						return;
					Executor.InternalExecute (this, false);
				}
			}
			
			public void Run ()
			{
				lock (this) {
					thread = Thread.CurrentThread();
				}
				try {
					if (!canceled)
						Action.Run ();
				} finally {
					lock (this) {
						thread = null;
						doneEvent.Set ();
						completed = true;
					}
				}
			}
			
			public bool Cancel (bool mayInterruptIfRunning)
			{
				lock (this) {
					if (canceled || completed)
						return false;
					canceled = completed = true;
					if (mayInterruptIfRunning && thread != null) {
						try {
							thread.Abort ();
						} catch {}
						thread = null;
					}
					doneEvent.Set ();
					return true;
				}
			}
			
			public T Get ()
			{
				doneEvent.WaitOne ();
				return default(T);
			}
		}
		
		public ScheduledThreadPoolExecutor (int corePoolSize, ThreadFactory factory): base (corePoolSize, factory)
		{
		}
		
		public override List<Runnable> ShutdownNow ()
		{
			lock (this) {
				Scheduler.Instance.Shutdown (this, false, false);
				return base.ShutdownNow ();
			}
		}
		
		public override void Shutdown ()
		{
			lock (this) {
				if (!continueExistingPeriodicTasksAfterShutdownPolicy || !executeExistingDelayedTasksAfterShutdownPolicy)
					Scheduler.Instance.Shutdown (this, continueExistingPeriodicTasksAfterShutdownPolicy, executeExistingDelayedTasksAfterShutdownPolicy);
				base.Shutdown ();
			}
		}
		
		public override bool IsTerminated ()
		{
			return base.IsTerminated () && !Scheduler.Instance.HasTasks (this);
		}
		
		public void SetContinueExistingPeriodicTasksAfterShutdownPolicy (bool cont)
		{
			continueExistingPeriodicTasksAfterShutdownPolicy = cont;
		}
		
		public void SetExecuteExistingDelayedTasksAfterShutdownPolicy (bool exec)
		{
			executeExistingDelayedTasksAfterShutdownPolicy = exec;
		}
		
		public Future<object> Schedule (Runnable r, long delay, TimeUnit unit)
		{
			return Schedule<object> (r, delay, unit);
		}
		
		public Future<T> Schedule<T> (Runnable r, long delay, TimeUnit unit)
		{
			DateTime now = DateTime.Now;
			lock (this) {
				if (IsShutdown ())
					return null;
				Task<T> t = new Task<T> () {
					Executor = this,
					Action = r,
					DueTime = now + TimeSpan.FromMilliseconds (unit.Convert (delay, TimeUnit.MILLISECONDS))
				};
				Scheduler.Instance.AddTask (t);
				return t;
			}
		}
	}
	
	interface IScheduledITask
	{
		void Start ();
		DateTime DueTime { get; set; }
		object Owner { get; }
		bool Cancel (bool mayInterruptIfRunning);
	}
		
	class Scheduler
	{
		internal static Scheduler Instance = new Scheduler ();
		
		List<IScheduledITask> tasks = new List<IScheduledITask> ();
		ST.Thread scheduler;
		ST.AutoResetEvent newTask = new ST.AutoResetEvent (false);
		
		public void Shutdown (object owner, bool continueExistingPeriodicTasks, bool executeExistingDelayedTasks)
		{
			if (!executeExistingDelayedTasks) {
				lock (tasks) {
					for (int n=0; n<tasks.Count; n++) {
						IScheduledITask t = tasks [n];
						if (t.Owner == owner) {
							tasks.RemoveAt (n);
							n--;
						}
					}
					newTask.Set ();
				}
			}
		}
		
		public void AddTask (IScheduledITask t)
		{
			lock (tasks) {
				int n;
				for (n=0; n<tasks.Count; n++) {
					if (t.DueTime < tasks [n].DueTime)
						break;
				}
				tasks.Insert (n, t);
				if (n == 0)
					newTask.Set ();
				
				if (scheduler == null) {
					scheduler = new ST.Thread (SchedulerThread);
					scheduler.IsBackground = true;
					scheduler.Start ();
				}
			}
		}
		
		public bool HasTasks (object owner)
		{
			lock (tasks) {
				return tasks.Any (t => t.Owner == owner);
			}
		}
		
		void SchedulerThread ()
		{
			int nextWait = ST.Timeout.Infinite;
			while (true) {
				if (nextWait != ST.Timeout.Infinite)
					nextWait = Math.Max (0, nextWait);
				newTask.WaitOne (nextWait);
				lock (tasks) {
					DateTime now = DateTime.Now;
					int n;
					for (n=0; n < tasks.Count && tasks[n].DueTime <= now; n++) {
						tasks[n].Start ();
						tasks.RemoveAt (n);
						n--;
					}
					if (n < tasks.Count)
						nextWait = (int) Math.Ceiling ((tasks[n].DueTime - DateTime.Now).TotalMilliseconds);
					else
						nextWait = ST.Timeout.Infinite;
				}
			}
		}
	}
}
