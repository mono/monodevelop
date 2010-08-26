/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Eclipse Foundation, Inc. nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GitSharp.Core.Util
{

	// TODO: [henon] this approach does not work in .net. Either the calling thread must be aborted (which is problematic) or the IO stream closed. 
	// See how TimeoutStream uses a timer to abort IO.

	/// <summary>
	///  Triggers an interrupt on the calling thread if it doesn't complete a block.
	///  <para/>
	///  Classes can use this to trip an alarm interrupting the calling thread if it
	///  doesn't complete a block within the specified timeout. Typical calling
	///  pattern is:
	/// 
	/// <code>
	///  private InterruptTimer myTimer = ...;
	///  void foo() {
	///    try {
	///      myTimer.begin(timeout);
	///      // work
	///    } finally {
	///      myTimer.end();
	///    }
	///  }
	/// </code>
	/// <para/>
	///  An InterruptTimer is not recursive. To implement recursive timers,
	///  independent InterruptTimer instances are required. A single InterruptTimer
	///  may be shared between objects which won't recursively call each other.
	///  <para/>
	///  Each InterruptTimer spawns one background thread to sleep the specified time
	///  and interrupt the thread which called {@link #begin(int)}. It is up to the
	///  caller to ensure that the operations within the work block between the
	///  matched begin and end calls tests the interrupt flag (most IO operations do).
	///  <para/>
	///  To terminate the background thread, use {@link #terminate()}. If the
	///  application fails to terminate the thread, it will (eventually) terminate
	///  itself when the InterruptTimer instance is garbage collected.
	/// 
	///  <see cref="TimeoutInputStream"/>
	/// </summary>
	public class InterruptTimer
	{
		private readonly AlarmState state;

		private readonly AlarmThread thread;

		private readonly AutoKiller autoKiller;

		/// <summary> Create a new timer with a default thread name./// </summary>
		public InterruptTimer()
			: this("JGit-InterruptTimer")
		{
		}

		/// <summary>
		///  Create a new timer to signal on interrupt on the caller.
		///  <para/>
		///  The timer thread is created in the calling thread's ThreadGroup.
		/// 
		///  <param name="threadName"> name of the timer thread.</param>
		/// </summary>
		public InterruptTimer(String threadName)
		{
			state = new AlarmState();
			autoKiller = new AutoKiller(state);
			thread = new AlarmThread(threadName, state);
			thread.Start();
		}

		/// <summary>
		///  Arm the interrupt timer before entering a blocking operation.
		/// 
		///  <param name="timeout">
		///             number of milliseconds before the interrupt should trigger.
		///             Must be > 0.</param>
		/// </summary>
		public void begin(int timeout)
		{
			if (timeout <= 0)
				throw new ArgumentException("Invalid timeout: " + timeout);
			//Thread.interrupted();
			state.begin(timeout);
		}

		/// <summary> Disable the interrupt timer, as the operation is complete./// </summary>
		public void end()
		{
			state.end();
		}

		/// <summary> Shutdown the timer thread, and wait for it to terminate./// </summary>
		public void terminate()
		{
			state.terminate();
			//try {
			thread.Join();
			//} catch (InterruptedException e) {
			//   //
			//}
		}
	}

	public class AlarmThread : WorkerThread
	{
		public AlarmThread(String name, AlarmState q) 
			: base(q, name)
		{
		}
	}
	
	public interface IWork
	{
		void Start();
	}
	
	public class WorkerThread
	{
		private Thread workThread;
		
		public bool IsAlive
		{
			get { return workThread.IsAlive; }
		}
		
		public WorkerThread(IWork work, string name) : this(new ThreadStart(work.Start), name)
		{
		}
		
		public WorkerThread(ThreadStart threadStart, string name) : this(new Thread(threadStart), name)
		{
		}
		
		public WorkerThread(Thread thread, string name)
		{
			workThread = thread;
			workThread.IsBackground = true;
			workThread.Name = name;
		}
		
		public void Start()
		{
			workThread.Start();
		}
		
		public void Interrupt()
		{
			workThread.Interrupt();
		}
		
		public void Join()
		{
			workThread.Join();
		}
		
		public void NotifyAll()
		{
			Monitor.PulseAll(this);
		}
		
		public static void Sleep(int timeout)
		{
			Thread.Sleep(timeout);
		}
		
		public static WorkerThread CurrentThread()
		{
			Thread currentThread = Thread.CurrentThread;
			
			return new WorkerThread(currentThread, currentThread.Name);
		}
	}

	// The trick here is, the AlarmThread does not have a reference to the
	// AutoKiller instance, only the InterruptTimer itself does. Thus when
	// the InterruptTimer is GC'd, the AutoKiller is also unreachable and
	// can be GC'd. When it gets finalized, it tells the AlarmThread to
	// terminate, triggering the thread to exit gracefully.
	//
	internal class AutoKiller
	{
		private AlarmState state;

		public AutoKiller(AlarmState s)
		{
			state = s;
		}

		~AutoKiller()
		{
			state.terminate();
		}
	}

	public class AlarmState : IWork
	{


		private WorkerThread callingThread;

		private long deadline;

		private bool terminated;

		public AlarmState()
		{
			callingThread = WorkerThread.CurrentThread();
		}

		public void Start()
		{
			lock (this)
			{
				while (!terminated && callingThread.IsAlive)
				{
					//try
					//{
					if (0 < deadline)
					{
						long delay = deadline - now();
						if (delay <= 0)
						{
							deadline = 0;
							callingThread.Interrupt();
						}
						else
						{
							WorkerThread.Sleep((int)delay);
						}
					}
					else
					{
						wait(1000);
					}
					//}
					//catch (InterruptedException e) // Note: [henon] Thread does not throw an equivalent exception in C# ??
					//{
					//   // Treat an interrupt as notice to examine state.
					//}
				}
			}
		}

		public void begin(int timeout)
		{
			lock (this)
			{
				if (terminated)
					throw new InvalidOperationException("Timer already terminated");
				callingThread = WorkerThread.CurrentThread();
				deadline = now() + timeout;
				notifyAll();
			}
		}

		public void end()
		{
			lock (this)
			{
				//if (0 == deadline)
				//   Thread.interrupted(); // <-- Note: [henon] this code does nothing but reset an irrelevant java thread internal flag AFAIK (which is not supported by our thread implementation)
				//else
				deadline = 0;
				notifyAll();
			}
		}

		public void terminate()
		{
			lock (this)
			{
				if (!terminated)
				{
					deadline = 0;
					terminated = true;
					notifyAll();
				}
			}
		}

		private static long now()
		{
			return DateTime.Now.ToMillisecondsSinceEpoch();
		}

		#region --> Java helpers

		// Note: [henon] to simulate java's builtin wait and notifyAll we use a waithandle
		private AutoResetEvent wait_handle = new AutoResetEvent(false);

		private void wait(int milliseconds)
		{
			wait_handle.WaitOne(milliseconds);
		}

		private void notifyAll()
		{
			wait_handle.Set();
		}

		#endregion
	}

}
