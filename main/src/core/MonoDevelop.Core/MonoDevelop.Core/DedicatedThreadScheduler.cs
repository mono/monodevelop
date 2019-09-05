//
// DedicatedThreadScheduler.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2019 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Core
{
	public class DedicatedThreadScheduler : TaskScheduler, IDisposable
	{
		readonly BlockingCollection<Task> taskQueue = new BlockingCollection<Task> ();
		readonly string threadName;
		readonly CancellationTokenSource cancellation = new CancellationTokenSource ();
		CancellationToken cancellationToken;
		volatile bool isDisposed;
		Thread dedicatedThread;

		public Thread DedicatedThread {
			get {
				EnsureThread ();
				return dedicatedThread;
			}
		}

		public override int MaximumConcurrencyLevel => 1;

		public DedicatedThreadScheduler ()
		{
		}

		public DedicatedThreadScheduler (string dedicatedThreadName)
		{
			threadName = dedicatedThreadName;
		}

		void EnsureThread ()
		{
			AssertDisposed (false);
			if (dedicatedThread == null) {
				cancellationToken = cancellation.Token;
				dedicatedThread = new Thread (Run) { IsBackground = true };
				if (!string.IsNullOrEmpty (threadName))
					dedicatedThread.Name = threadName;
				dedicatedThread.Start ();
			}
		}

		void AssertDisposed (bool shouldBeDisposed)
		{
			if (shouldBeDisposed != isDisposed) {
				if (!shouldBeDisposed)
					throw new ObjectDisposedException (nameof (DedicatedThreadScheduler));
				else
					throw new InvalidOperationException ($"{nameof (DedicatedThreadScheduler)} not disposed as expected");
			}
		}

		void Run ()
		{
			AssertDisposed (false);
			try {
				while (!isDisposed) {
					try {
						var task = taskQueue.Take (cancellationToken);
						TryExecuteTask (task);
					} catch (OperationCanceledException) {
						AssertDisposed (true);
					}
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Scheduler loop failed unexpectedly", ex);
			} finally {
				taskQueue.Dispose ();
			}
		}

		protected override IEnumerable<Task> GetScheduledTasks ()
		{
			AssertDisposed (false);
			return taskQueue;
		}

		protected override void QueueTask (Task task)
		{
			EnsureThread ();
			taskQueue.Add (task);
		}

		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{
			if (Thread.CurrentThread == dedicatedThread) {
				return TryExecuteTask (task);
			}
			return false;
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!isDisposed) {
				isDisposed = true;
				if (disposing) {
					cancellation.Cancel ();
					cancellation.Dispose ();
				}
			}
		}

		~DedicatedThreadScheduler ()
		{
			Dispose (false);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
	}
}
