// AsyncOperation.cs
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
using System.Threading;

namespace MonoDevelop.Core
{
	public class AsyncOperation: IAsyncOperation
	{
		bool canceled;
		bool completed;
		bool success;
		bool successWithWarnings;
		IAsyncOperation trackedOperation;
		object lck = new object ();
		
		OperationHandler completedEvent;
		
		public event OperationHandler Completed {
			add {
				bool done = false;
				lock (lck) {
					if (completed)
						done = true;
					else
						completedEvent += value;
				}
				if (done)
					value (this);
			}
			remove {
				lock (lck) {
					completedEvent -= value;
				}
			}
		}
		
		public event OperationHandler CancelRequested;
		
		public bool Canceled {
			get { return canceled; }
		}
		
		public void Cancel ()
		{
			canceled = true;
			if (trackedOperation != null)
				trackedOperation.Cancel ();
			if (CancelRequested != null)
				CancelRequested (this);
		}
		
		void IAsyncOperation.WaitForCompleted ()
		{
			lock (lck) {
				if (!completed)
					Monitor.Wait (lck);
			}
		}
		
		public bool IsCompleted {
			get {
				return completed;
			}
		}
		
		public bool Success {
			get {
				return success;
			}
		}
		
		public bool SuccessWithWarnings {
			get {
				return successWithWarnings;
			}
		}
		
		public void TrackOperation (IAsyncOperation oper, bool isFinal)
		{
			if (trackedOperation != null)
				throw new InvalidOperationException ("An operation is already being tracked.");
			trackedOperation = oper;
			oper.Completed += delegate {
				if (!oper.Success || isFinal)
					SetCompleted (oper.Success, oper.SuccessWithWarnings);
				trackedOperation = null;
			};
		}
		
		public void SetCompleted (bool success)
		{
			SetCompleted (success, false);
		}
		
		public void SetCompleted (bool success, bool hasWarnings)
		{
			lock (lck) {
				completed = true;
				this.success = success;
				if (success && hasWarnings)
					successWithWarnings = true;
				Monitor.PulseAll (lck);
				if (completedEvent != null)
					completedEvent (this);
			}
		}
	}
}
