/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>ProgressMonitor that batches update events.</summary>
	/// <remarks>ProgressMonitor that batches update events.</remarks>
	public abstract class BatchingProgressMonitor : ProgressMonitor
	{
		private static readonly ScheduledThreadPoolExecutor alarmQueue;

		internal static readonly object alarmQueueKiller;

		static BatchingProgressMonitor()
		{
			// To support garbage collection, start our thread but
			// swap out the thread factory. When our class is GC'd
			// the alarmQueueKiller will finalize and ask the executor
			// to shutdown, ending the worker.
			//
			int threads = 1;
			alarmQueue = new ScheduledThreadPoolExecutor(threads, new _ThreadFactory_66());
			alarmQueue.SetContinueExistingPeriodicTasksAfterShutdownPolicy(false);
			alarmQueue.SetExecuteExistingDelayedTasksAfterShutdownPolicy(false);
			alarmQueue.PrestartAllCoreThreads();
			// Now that the threads are running, its critical to swap out
			// our own thread factory for one that isn't in the ClassLoader.
			// This allows the class to GC.
			//
			alarmQueue.SetThreadFactory(Executors.DefaultThreadFactory());
			alarmQueueKiller = new _object_87();
		}

		private sealed class _ThreadFactory_66 : ThreadFactory
		{
			public _ThreadFactory_66()
			{
				this.baseFactory = Executors.DefaultThreadFactory();
			}

			private readonly ThreadFactory baseFactory;

			public Sharpen.Thread NewThread(Runnable taskBody)
			{
				Sharpen.Thread thr = this.baseFactory.NewThread(taskBody);
				thr.SetName("JGit-AlarmQueue");
				thr.SetDaemon(true);
				return thr;
			}
		}

		private sealed class _object_87 : object
		{
			public _object_87()
			{
			}

			~_object_87()
			{
				BatchingProgressMonitor.alarmQueue.ShutdownNow();
			}
		}

		private long delayStartTime;

		private TimeUnit delayStartUnit = TimeUnit.MILLISECONDS;

		private BatchingProgressMonitor.Task task;

		/// <summary>Set an optional delay before the first output.</summary>
		/// <remarks>Set an optional delay before the first output.</remarks>
		/// <param name="time">
		/// how long to wait before output. If 0 output begins on the
		/// first
		/// <see cref="Update(int)">Update(int)</see>
		/// call.
		/// </param>
		/// <param name="unit">
		/// time unit of
		/// <code>time</code>
		/// .
		/// </param>
		public virtual void SetDelayStart(long time, TimeUnit unit)
		{
			delayStartTime = time;
			delayStartUnit = unit;
		}

		public override void Start(int totalTasks)
		{
		}

		// Ignore the number of tasks.
		public override void BeginTask(string title, int work)
		{
			EndTask();
			task = new BatchingProgressMonitor.Task(title, work);
			if (delayStartTime != 0)
			{
				task.Delay(delayStartTime, delayStartUnit);
			}
		}

		public override void Update(int completed)
		{
			if (task != null)
			{
				task.Update(this, completed);
			}
		}

		public override void EndTask()
		{
			if (task != null)
			{
				task.End(this);
				task = null;
			}
		}

		public override bool IsCancelled()
		{
			return false;
		}

		/// <summary>Update the progress monitor if the total work isn't known,</summary>
		/// <param name="taskName">name of the task.</param>
		/// <param name="workCurr">number of units already completed.</param>
		protected internal abstract void OnUpdate(string taskName, int workCurr);

		/// <summary>Finish the progress monitor when the total wasn't known in advance.</summary>
		/// <remarks>Finish the progress monitor when the total wasn't known in advance.</remarks>
		/// <param name="taskName">name of the task.</param>
		/// <param name="workCurr">total number of units processed.</param>
		protected internal abstract void OnEndTask(string taskName, int workCurr);

		/// <summary>Update the progress monitor when the total is known in advance.</summary>
		/// <remarks>Update the progress monitor when the total is known in advance.</remarks>
		/// <param name="taskName">name of the task.</param>
		/// <param name="workCurr">number of units already completed.</param>
		/// <param name="workTotal">estimated number of units to process.</param>
		/// <param name="percentDone">
		/// <code>workCurr * 100 / workTotal</code>
		/// .
		/// </param>
		protected internal abstract void OnUpdate(string taskName, int workCurr, int workTotal
			, int percentDone);

		/// <summary>Finish the progress monitor when the total is known in advance.</summary>
		/// <remarks>Finish the progress monitor when the total is known in advance.</remarks>
		/// <param name="taskName">name of the task.</param>
		/// <param name="workCurr">total number of units processed.</param>
		/// <param name="workTotal">estimated number of units to process.</param>
		/// <param name="percentDone">
		/// <code>workCurr * 100 / workTotal</code>
		/// .
		/// </param>
		protected internal abstract void OnEndTask(string taskName, int workCurr, int workTotal
			, int percentDone);

		private class Task : Runnable
		{
			/// <summary>Title of the current task.</summary>
			/// <remarks>Title of the current task.</remarks>
			private readonly string taskName;

			/// <summary>
			/// Number of work units, or
			/// <see cref="ProgressMonitor.UNKNOWN">ProgressMonitor.UNKNOWN</see>
			/// .
			/// </summary>
			private readonly int totalWork;

			/// <summary>True when timer expires and output should occur on next update.</summary>
			/// <remarks>True when timer expires and output should occur on next update.</remarks>
			private volatile bool display;

			/// <summary>Scheduled timer, supporting cancellation if task ends early.</summary>
			/// <remarks>Scheduled timer, supporting cancellation if task ends early.</remarks>
			private Future<object> timerFuture;

			/// <summary>True if the task has displayed anything.</summary>
			/// <remarks>True if the task has displayed anything.</remarks>
			private bool output;

			/// <summary>Number of work units already completed.</summary>
			/// <remarks>Number of work units already completed.</remarks>
			private int lastWork;

			/// <summary>
			/// Percentage of
			/// <see cref="totalWork">totalWork</see>
			/// that is done.
			/// </summary>
			private int lastPercent;

			internal Task(string taskName, int totalWork)
			{
				this.taskName = taskName;
				this.totalWork = totalWork;
				this.display = true;
			}

			internal virtual void Delay(long time, TimeUnit unit)
			{
				display = false;
				timerFuture = alarmQueue.Schedule(this, time, unit);
			}

			public virtual void Run()
			{
				display = true;
			}

			internal virtual void Update(BatchingProgressMonitor pm, int completed)
			{
				lastWork += completed;
				if (totalWork == UNKNOWN)
				{
					// Only display once per second, as the alarm fires.
					if (display)
					{
						pm.OnUpdate(taskName, lastWork);
						output = true;
						RestartTimer();
					}
				}
				else
				{
					// Display once per second or when 1% is done.
					int currPercent = lastWork * 100 / totalWork;
					if (display)
					{
						pm.OnUpdate(taskName, lastWork, totalWork, currPercent);
						output = true;
						RestartTimer();
						lastPercent = currPercent;
					}
					else
					{
						if (currPercent != lastPercent)
						{
							pm.OnUpdate(taskName, lastWork, totalWork, currPercent);
							output = true;
							lastPercent = currPercent;
						}
					}
				}
			}

			private void RestartTimer()
			{
				display = false;
				timerFuture = alarmQueue.Schedule(this, 1, TimeUnit.SECONDS);
			}

			internal virtual void End(BatchingProgressMonitor pm)
			{
				if (output)
				{
					if (totalWork == UNKNOWN)
					{
						pm.OnEndTask(taskName, lastWork);
					}
					else
					{
						int pDone = lastWork * 100 / totalWork;
						pm.OnEndTask(taskName, lastWork, totalWork, pDone);
					}
				}
				if (timerFuture != null)
				{
					timerFuture.Cancel(false);
				}
			}
		}
	}
}
