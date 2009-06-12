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

namespace Mono.Debugging.Evaluation
{
	public class AsyncOperationManager
	{
		List<ST.WaitCallback> operationsToCancel = new List<ST.WaitCallback> ();

		public void Invoke (AsyncOperation methodCall, int timeout)
		{
			bool aborted = false;
			ST.WaitCallback abortOper;

			lock (operationsToCancel) {

				abortOper = delegate {
					lock (operationsToCancel) {
						if (!aborted) {
							aborted = true;
							methodCall.Abort ();
						}
					}
				};
				operationsToCancel.Add (abortOper);

				methodCall.Invoke ();
			}

			if (timeout != -1) {
				if (!methodCall.WaitForCompleted (timeout)) {
					lock (operationsToCancel) {
						operationsToCancel.Remove (abortOper);
						ST.Monitor.PulseAll (operationsToCancel);
						if (aborted) {
							throw new EvaluatorException ("Aborted.");
						}
						else
							aborted = true;
						methodCall.Abort ();
						throw new TimeOutException ();
					}
				}
			}
			else {
				methodCall.WaitForCompleted (0);
			}

			lock (operationsToCancel) {
				operationsToCancel.Remove (abortOper);
				ST.Monitor.PulseAll (operationsToCancel);
				if (aborted) {
					throw new EvaluatorException ("Aborted.");
				}
			}

			if (!string.IsNullOrEmpty (methodCall.ExceptionMessage)) {
				throw new Exception (methodCall.ExceptionMessage);
			}

		}

		public void AbortAll ( )
		{
			lock (operationsToCancel) {
				foreach (ST.WaitCallback cb in operationsToCancel) {
					try {
						cb (null);
					}
					catch {
						// Ignore
					}
				}
				operationsToCancel.Clear ();
			}
		}

		public void WaitForAll ( )
		{
			lock (operationsToCancel) {
				while (operationsToCancel.Count > 0)
					ST.Monitor.Wait (operationsToCancel);
			}
		}
	}

	public abstract class AsyncOperation
	{
		public string ExceptionMessage { get; set; }

		public abstract void Invoke ( );

		public abstract void Abort ( );

		public abstract bool WaitForCompleted (int timeout);
	}
}
