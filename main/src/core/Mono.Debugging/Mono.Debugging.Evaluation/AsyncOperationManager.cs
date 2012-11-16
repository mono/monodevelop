// RuntimeInvokeManager.cs
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
using ST = System.Threading;
using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	public class AsyncOperationManager: IDisposable
	{
		List<AsyncOperation> operationsToCancel = new List<AsyncOperation> ();
		internal bool Disposing;

		public void Invoke (AsyncOperation methodCall, int timeout)
		{
			methodCall.Aborted = false;
			methodCall.Manager = this;

			lock (operationsToCancel) {
				operationsToCancel.Add (methodCall);
				methodCall.Invoke ();
			}

			if (timeout > 0) {
				if (!methodCall.WaitForCompleted (timeout)) {
					bool wasAborted = methodCall.Aborted;
					methodCall.InternalAbort ();
					lock (operationsToCancel) {
						operationsToCancel.Remove (methodCall);
						ST.Monitor.PulseAll (operationsToCancel);
					}
					if (wasAborted)
						throw new EvaluatorAbortedException ();
					else
						throw new TimeOutException ();
				}
			}
			else {
				methodCall.WaitForCompleted (System.Threading.Timeout.Infinite);
			}

			lock (operationsToCancel) {
				operationsToCancel.Remove (methodCall);
				ST.Monitor.PulseAll (operationsToCancel);
				if (methodCall.Aborted) {
					throw new EvaluatorAbortedException ();
				}
			}

			if (!string.IsNullOrEmpty (methodCall.ExceptionMessage)) {
				throw new Exception (methodCall.ExceptionMessage);
			}
		}
		
		public void Dispose ()
		{
			Disposing = true;
			lock (operationsToCancel) {
				foreach (AsyncOperation op in operationsToCancel) {
					op.InternalShutdown ();
				}
				operationsToCancel.Clear ();
			}
		}

		public void AbortAll ()
		{
			lock (operationsToCancel) {
				foreach (AsyncOperation op in operationsToCancel)
					op.InternalAbort ();
			}
		}
		
		public void EnterBusyState (AsyncOperation oper)
		{
			BusyStateEventArgs args = new BusyStateEventArgs ();
			args.IsBusy = true;
			args.Description = oper.Description;
			if (BusyStateChanged != null)
				BusyStateChanged (this, args);
		}
		
		public void LeaveBusyState (AsyncOperation oper)
		{
			BusyStateEventArgs args = new BusyStateEventArgs ();
			args.IsBusy = false;
			args.Description = oper.Description;
			if (BusyStateChanged != null)
				BusyStateChanged (this, args);
		}
		
		public event EventHandler<BusyStateEventArgs> BusyStateChanged;
	}

	public abstract class AsyncOperation
	{
		internal bool Aborted;
		internal AsyncOperationManager Manager;
		
		public bool Aborting { get; internal set; }
		
		internal void InternalAbort ()
		{
			ST.Monitor.Enter (this);
			if (Aborted) {
				ST.Monitor.Exit (this);
				return;
			}
			
			if (Aborting) {
				// Somebody else is aborting this. Just wait for it to finish.
				ST.Monitor.Exit (this);
				WaitForCompleted (ST.Timeout.Infinite);
				return;
			}
			
			Aborting = true;
			
			int abortState = 0;
			int abortRetryWait = 100;
			bool abortRequested = false;
			
			do {
				if (abortState > 0)
					ST.Monitor.Enter (this);
				
				try {
					if (!Aborted && !abortRequested) {
						// The Abort() call doesn't block. WaitForCompleted is used below to wait for the abort to succeed
						Abort ();
						abortRequested = true;
					}
					// Short wait for the Abort to finish. If this wait is not enough, it will wait again in the next loop
					if (WaitForCompleted (100)) {
						ST.Monitor.Exit (this);
						break;
					}
				} catch {
					// If abort fails, try again after a short wait
				}
				abortState++;
				if (abortState == 6) {
					// Several abort calls have failed. Inform the user that the debugger is busy
					abortRetryWait = 500;
					try {
						Manager.EnterBusyState (this);
					} catch (Exception ex) {
						Console.WriteLine (ex);
					}
				}
				ST.Monitor.Exit (this);
			} while (!Aborted && !WaitForCompleted (abortRetryWait) && !Manager.Disposing);
			
			if (Manager.Disposing) {
				InternalShutdown ();
			}
			else {
				lock (this) {
					Aborted = true;
					if (abortState >= 6)
						Manager.LeaveBusyState (this);
				}
			}
		}
		
		internal void InternalShutdown ()
		{
			lock (this) {
				if (Aborted)
					return;
				try {
					Aborted = true;
					Shutdown ();
				} catch {
					// Ignore
				}
			}
		}
		
		/// <summary>
		/// Message of the exception, if the execution failed. 
		/// </summary>
		public string ExceptionMessage { get; set; }

		/// <summary>
		/// Returns a short description of the operation, to be shown in the Debugger Busy Dialog
		/// when it blocks the execution of the debugger. 
		/// </summary>
		public abstract string Description { get; }
		
		/// <summary>
		/// Called to invoke the operation. The execution must be asynchronous (it must return immediatelly).
		/// </summary>
		public abstract void Invoke ( );

		/// <summary>
		/// Called to abort the execution of the operation. It has to throw an exception
		/// if the operation can't be aborted. This operation must not block. The engine
		/// will wait for the operation to be aborted by calling WaitForCompleted.
		/// </summary>
		public abstract void Abort ();

		/// <summary>
		/// Waits until the operation has been completed or aborted.
		/// </summary>
		public abstract bool WaitForCompleted (int timeout);
		
		/// <summary>
		/// Called when the debugging session has been disposed.
		/// I must cause any call to WaitForCompleted to exit, even if the operation
		/// has not been completed or can't be aborted.
		/// </summary>
		public abstract void Shutdown ();
	}
}
