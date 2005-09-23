//
// NullProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Threading;
using System.IO;

namespace MonoDevelop.Services
{
	public class NullProgressMonitor: IProgressMonitor, IAsyncOperation
	{
		bool done, canceled, error;
		ManualResetEvent waitEvent;
		
		public object SyncRoot {
			get { return this; }
		}
		
		public virtual void BeginTask (string name, int totalWork)
		{
		}
		
		public virtual void EndTask ()
		{
		}
		
		public virtual void Step (int work)
		{
		}
		
		public virtual TextWriter Log {
			get { return TextWriter.Null; }
		}
		
		public virtual void ReportSuccess (string message)
		{
		}
		
		public virtual void ReportWarning (string message)
		{
		}
		
		public virtual void ReportError (string message, Exception ex)
		{
			error = true;
		}
		
		public bool IsCancelRequested {
			get { return canceled; }
		}
		
		public virtual void Dispose ()
		{
			lock (this) {
				if (done) return;
				done = true;
				if (waitEvent != null)
					waitEvent.Set ();
			}
			OnCompleted ();
		}
		
		public IAsyncOperation AsyncOperation
		{
			get { return this; }
		}
		
		void IAsyncOperation.Cancel ()
		{
			lock (this) {
				if (canceled) return;
				canceled = true;
			}
			if (cancelRequestedEvent != null)
				cancelRequestedEvent (this);
		}
		
		void IAsyncOperation.WaitForCompleted ()
		{
			lock (this) {
				if (done) return;
				if (waitEvent == null)
					waitEvent = new ManualResetEvent (false);
			}
			waitEvent.WaitOne ();
		}
		
		bool IAsyncOperation.IsCompleted
		{
			get { return done; }
		}
		
		bool IAsyncOperation.Success {
			get { return !error; }
		}
		
		public event OperationHandler Completed {
			add {
				bool alreadyCompleted = false;
				lock (this) {
					completedEvent += value;
					alreadyCompleted = done;
				}
				if (alreadyCompleted) value (this);
			}
			remove {
				lock (this) {
					completedEvent -= value;
				}
			}
		}
		
		public event MonitorHandler CancelRequested {
			add {
				bool alreadyCanceled = false;
				lock (this) {
					cancelRequestedEvent += value;
					alreadyCanceled = canceled;
				}
				if (alreadyCanceled) value (this);
			}
			remove {
				lock (this) {
					cancelRequestedEvent -= value;
				}
			}
		}		
		
		protected virtual void OnCompleted ()
		{
			if (completedEvent != null)
				completedEvent (AsyncOperation);
		}

		event MonitorHandler cancelRequestedEvent;
		event OperationHandler completedEvent;
	}
}
