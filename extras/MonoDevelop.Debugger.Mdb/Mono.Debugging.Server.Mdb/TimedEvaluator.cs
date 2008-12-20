// EvaluationContext.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Threading;

namespace DebuggerServer
{
	public class TimedEvaluator
	{
		int runTimeout = DebuggerServer.DefaultAsyncSwitchTimeout;
		int maxThreads = 1;

		object runningLock = new object ();
		Queue<Task> pendingTasks = new Queue<Task> ();
		AutoResetEvent newTaskEvent = new AutoResetEvent (false);
		Task currentTask;
		int runningThreads;
		bool mainThreadBusy;
		bool useTimeout;
		
		public TimedEvaluator (): this (true)
		{
		}

		public TimedEvaluator (bool useTimeout)
		{
			this.useTimeout = useTimeout;
		}

		public bool Run (EvaluatorDelegate evaluator, EvaluatorDelegate delayedDoneCallback)
		{
			if (!useTimeout) {
				SafeRun (evaluator);
				return true;
			}
			
			Task task = new Task ();
			task.Evaluator = evaluator;
			task.FinishedCallback = delayedDoneCallback;
			
			lock (runningLock) {
				if (mainThreadBusy || runningThreads == 0) {
					if (runningThreads < maxThreads) {
						runningThreads++;
						Thread tr = new Thread (Runner);
						tr.IsBackground = true;
						tr.Start ();
					} else {
						// The main thread is busy evaluating and we can't tell
						// how much time it will take, so we can't wait for it.
						task.TimedOut = true;
						pendingTasks.Enqueue (task);
						return false;
					}
				}
				mainThreadBusy = true;
			}
			
			currentTask = task;
			newTaskEvent.Set ();
			task.RunningEvent.WaitOne ();
			
			lock (task) {
				if (!task.RunFinishedEvent.WaitOne (runTimeout, false)) {
					task.TimedOut = true;
					return false;
				} else {
					lock (runningLock) {
						mainThreadBusy = false;
						Monitor.PulseAll (runningLock);
					}
				}
			}
			return true;
		}

		void Runner ()
		{
			Task threadTask = null;
			
			while (true) {

				if (threadTask == null) {
					newTaskEvent.WaitOne ();
					threadTask = currentTask;
					currentTask = null;
				}
				
				threadTask.RunningEvent.Set ();
				DateTime t = DateTime.Now;
				SafeRun (threadTask.Evaluator);
				threadTask.RunFinishedEvent.Set ();
				
				lock (threadTask) {
					if (threadTask.TimedOut)
						SafeRun (threadTask.FinishedCallback);
					else {
						threadTask = null;
						continue; // Done. Keep waiting for more tasks.
					}
				}
				
				threadTask = null;

				// The task timed out, so more threads may already have
				// been created while this one was busy.
				
				lock (runningLock) {
					Monitor.PulseAll (runningLock);
					if (pendingTasks.Count > 0) {
						// There is pending work to do.
						threadTask = pendingTasks.Dequeue ();
					}
					else if (mainThreadBusy) {
						// More threads have been created and all are busy.
						// This will now be the main thread.
						mainThreadBusy = false;
					}
					else {
						// More threads have been created and one of them is waiting for tasks
						// This thread is not needed anymore, die
						runningThreads--;
						break;
					}
				}
			}
		}

		public void CancelAll ()
		{
			lock (runningLock) {
				pendingTasks.Clear ();
			}
		}

		public void WaitForStopped ()
		{
			lock (runningLock) {
				while (mainThreadBusy)
					Monitor.Wait (runningLock);
			}
		}

		void SafeRun (EvaluatorDelegate del)
		{
			try {
				del ();
			} catch {
			}
		}
		
		class Task
		{
			public AutoResetEvent RunningEvent = new AutoResetEvent (false);
			public AutoResetEvent RunFinishedEvent = new AutoResetEvent (false);
			public EvaluatorDelegate Evaluator;
			public EvaluatorDelegate FinishedCallback;
			public bool TimedOut;
		}
	}

	public delegate void EvaluatorDelegate ();
}
