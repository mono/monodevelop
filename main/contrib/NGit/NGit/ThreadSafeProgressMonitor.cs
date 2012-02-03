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

using System;
using System.Threading;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Wrapper around the general
	/// <see cref="ProgressMonitor">ProgressMonitor</see>
	/// to make it thread safe.
	/// Updates to the underlying ProgressMonitor are made only from the thread that
	/// allocated this wrapper. Callers are responsible for ensuring the allocating
	/// thread uses
	/// <see cref="PollForUpdates()">PollForUpdates()</see>
	/// or
	/// <see cref="WaitForCompletion()">WaitForCompletion()</see>
	/// to
	/// update the underlying ProgressMonitor.
	/// Only
	/// <see cref="Update(int)">Update(int)</see>
	/// ,
	/// <see cref="IsCancelled()">IsCancelled()</see>
	/// , and
	/// <see cref="EndWorker()">EndWorker()</see>
	/// may be invoked from a worker thread. All other methods of the ProgressMonitor
	/// interface can only be called from the thread that allocates this wrapper.
	/// </summary>
	public class ThreadSafeProgressMonitor : ProgressMonitor
	{
		private readonly ProgressMonitor pm;

		private readonly ReentrantLock Lock;

		private readonly Sharpen.Thread mainThread;

		private readonly AtomicInteger workers;

		private readonly AtomicInteger pendingUpdates;

		private readonly Semaphore process;

		/// <summary>Wrap a ProgressMonitor to be thread safe.</summary>
		/// <remarks>Wrap a ProgressMonitor to be thread safe.</remarks>
		/// <param name="pm">the underlying monitor to receive events.</param>
		public ThreadSafeProgressMonitor(ProgressMonitor pm)
		{
			this.pm = pm;
			this.Lock = new ReentrantLock();
			this.mainThread = Sharpen.Thread.CurrentThread();
			this.workers = new AtomicInteger(0);
			this.pendingUpdates = new AtomicInteger(0);
			this.process = Sharpen.Extensions.CreateSemaphore(0);
		}

		public override void Start(int totalTasks)
		{
			if (!IsMainThread())
			{
				throw new InvalidOperationException();
			}
			pm.Start(totalTasks);
		}

		public override void BeginTask(string title, int totalWork)
		{
			if (!IsMainThread())
			{
				throw new InvalidOperationException();
			}
			pm.BeginTask(title, totalWork);
		}

		/// <summary>Notify the monitor a worker is starting.</summary>
		/// <remarks>Notify the monitor a worker is starting.</remarks>
		public virtual void StartWorker()
		{
			StartWorkers(1);
		}

		/// <summary>Notify the monitor of workers starting.</summary>
		/// <remarks>Notify the monitor of workers starting.</remarks>
		/// <param name="count">the number of worker threads that are starting.</param>
		public virtual void StartWorkers(int count)
		{
			workers.AddAndGet(count);
		}

		/// <summary>Notify the monitor a worker is finished.</summary>
		/// <remarks>Notify the monitor a worker is finished.</remarks>
		public virtual void EndWorker()
		{
			if (workers.DecrementAndGet() == 0)
			{
				process.Release();
			}
		}

		/// <summary>Non-blocking poll for pending updates.</summary>
		/// <remarks>
		/// Non-blocking poll for pending updates.
		/// This method can only be invoked by the same thread that allocated this
		/// ThreadSafeProgressMonior.
		/// </remarks>
		public virtual void PollForUpdates()
		{
			//assert isMainThread();
			DoUpdates();
		}

		/// <summary>Process pending updates and wait for workers to finish.</summary>
		/// <remarks>
		/// Process pending updates and wait for workers to finish.
		/// This method can only be invoked by the same thread that allocated this
		/// ThreadSafeProgressMonior.
		/// </remarks>
		/// <exception cref="System.Exception">
		/// if the main thread is interrupted while waiting for
		/// completion of workers.
		/// </exception>
		public virtual void WaitForCompletion()
		{
			//assert isMainThread();
			while (0 < workers.Get())
			{
				DoUpdates();
				process.WaitOne();
			}
			DoUpdates();
		}

		private void DoUpdates()
		{
			int cnt = pendingUpdates.GetAndSet(0);
			if (0 < cnt)
			{
				pm.Update(cnt);
			}
		}

		public override void Update(int completed)
		{
			int old = pendingUpdates.GetAndAdd(completed);
			if (IsMainThread())
			{
				DoUpdates();
			}
			else
			{
				if (old == 0)
				{
					process.Release();
				}
			}
		}

		public override bool IsCancelled()
		{
			Lock.Lock();
			try
			{
				return pm.IsCancelled();
			}
			finally
			{
				Lock.Unlock();
			}
		}

		public override void EndTask()
		{
			if (!IsMainThread())
			{
				throw new InvalidOperationException();
			}
			pm.EndTask();
		}

		private bool IsMainThread()
		{
			return Sharpen.Thread.CurrentThread() == mainThread;
		}
	}
}
