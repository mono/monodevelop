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

namespace Mono.Debugging.Evaluation
{
	public class TimedEvaluator
	{
		int maxThreads = 1;

		object runningLock = new object ();
		Queue<Task> pendingTasks = new Queue<Task> ();
		AutoResetEvent newTaskEvent = new AutoResetEvent (false);
		Task currentTask;
		int runningThreads;
		bool mainThreadBusy;
		bool useTimeout;
		bool disposed;
		
		public TimedEvaluator (): this (true)
		{
		}

		public TimedEvaluator (bool useTimeout)
		{
			RunTimeout = 1000;
			this.useTimeout = useTimeout;
		}

		public int RunTimeout { get; set; }

		public bool IsEvaluating {
			get {
				lock (runningLock) {
					return pendingTasks.Count > 0 || mainThreadBusy;
				}
			}
		}
		
		void OnStartEval ()
		{
//			Console.WriteLine ("Eval Started");
		}
		
		void OnEndEval ()
		{
//			lock (runningLock) {
//				Console.WriteLine ("Eval Finished ({0} pending)", pendingTasks.Count);
//			}
		}
		
		/// <summary>
		/// Executes the provided evaluator. If a result is obtained before RunTimeout milliseconds,
		/// the method ends returning True.
		/// If it does not finish after RunTimeout milliseconds, the method ends retuning False, although
		/// the evaluation continues in the background. In that case, when evaluation ends, the provided
		/// delayedDoneCallback delegate is called.
		/// </summary>
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
				if (disposed)
					return false;
				if (mainThreadBusy || runningThreads == 0) {
					if (runningThreads < maxThreads) {
						runningThreads++;
						Thread tr = new Thread (Runner);
						tr.Name = "Debugger evaluator";
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
				currentTask = task;
			}
			
			OnStartEval ();
			newTaskEvent.Set ();
			task.RunningEvent.WaitOne ();
			
			lock (task) {
				if (!task.RunFinishedEvent.WaitOne (RunTimeout, false)) {
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
			
			while (!disposed) {

				if (threadTask == null) {
					newTaskEvent.WaitOne ();
					
					lock (runningLock) {
						if (disposed) {
							runningThreads--;
							return;
						}
						threadTask = currentTask;
						currentTask = null;
					}
				}
				
				threadTask.RunningEvent.Set ();
				SafeRun (threadTask.Evaluator);
				threadTask.RunFinishedEvent.Set ();

				Task curTask = threadTask;
				threadTask = null;
				
				OnEndEval ();

				lock (runningLock) {
					if (disposed) {
						runningThreads--;
						return;
					}
				}
				
				lock (curTask) {
					if (!curTask.TimedOut)
						continue; // Done. Keep waiting for more tasks.
					
					SafeRun (curTask.FinishedCallback);
				}

				// The task timed out, so more threads may already have
				// been created while this one was busy.
				
				lock (runningLock) {
					Monitor.PulseAll (runningLock);
					if (pendingTasks.Count > 0) {
						// There is pending work to do.
						OnStartEval ();
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
		
		public void Dispose ()
		{
			lock (runningLock) {
				disposed = true;
				CancelAll ();
				newTaskEvent.Set ();
			}
		}

		public void CancelAll ()
		{
			lock (runningLock) {
				// If there is a task waiting the be picked by the runner,
				// set the task wait events to avoid deadlocking the caller.
				if (currentTask != null) {
					currentTask.RunningEvent.Set ();
					currentTask.RunFinishedEvent.Set ();
				}
				pendingTasks.Clear ();
				Monitor.PulseAll (runningLock);
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
