//
// BaseProgressMonitor.cs
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
using System.Collections.Specialized;
using System.IO;

namespace MonoDevelop.Services
{
	public class BaseProgressMonitor: GuiSyncObject, IProgressMonitor, IAsyncOperation
	{
		class MbrWrapper {
			public ManualResetEvent waitEvent;

			//workaround for "** ERROR **: file icall.c: line 2419 (ves_icall_InternalExecute): assertion failed" bug when
			//handling the CancelRequested event
			public event MonitorHandler cancelRequestedEvent;

			public void RaiseEvent (IProgressMonitor monitor) {
				if (cancelRequestedEvent != null)
					cancelRequestedEvent (monitor);
			}
		}
		
		MbrWrapper c = new MbrWrapper ();
		ProgressTracker progressTracker;
		LogTextWriter logger;
		bool canceled;
		
		event OperationHandler completedEvent;
		
		StringCollection errorsMessages = new StringCollection ();
		StringCollection successMessages = new StringCollection ();
		StringCollection warningMessages = new StringCollection ();
		Exception errorException;
		
		public BaseProgressMonitor ()
		{
			progressTracker = new ProgressTracker ();
			logger = new LogTextWriter ();
			logger.TextWritten += new LogTextEventHandler (WriteLogInternal);
		}
		
		[FreeDispatch]
		public object SyncRoot {
			get {
				// Dont return 'this'. Locking on proxies doesn't look like a good idea.
				return progressTracker;
			}
		}
		
		[AsyncDispatch]
		public virtual void BeginTask (string name, int totalWork)
		{
			progressTracker.BeginTask (name, totalWork);
			OnProgressChanged ();
		}
		
		[AsyncDispatch]
		public virtual void EndTask ()
		{
			progressTracker.EndTask ();
		}
		
		[AsyncDispatch]
		public virtual void Step (int work)
		{
			progressTracker.Step (work);
			OnProgressChanged ();
		}
		
		public TextWriter Log {
			[FreeDispatch]
			get {
				return logger;
			}
		}
		
		[FreeDispatch]
		public virtual void ReportSuccess (string message)
		{
			successMessages.Add (message);
		}
		
		[FreeDispatch]
		public virtual void ReportWarning (string message)
		{
			warningMessages.Add (message);
		}
		
		[FreeDispatch]
		public virtual void ReportError (string message, Exception ex)
		{
			if (message == null && ex != null)
				message = ex.Message;
			else if (message != null && ex != null) {
				if (!message.EndsWith (".")) message += ".";
				message += " " + ex.Message;
			}
			
			errorsMessages.Add (message);
			if (ex != null) {
				Runtime.LoggingService.Info (ex);
				errorException = ex;
			}
		}
		
		[FreeDispatch]
		public virtual bool IsCancelRequested {
			get { return canceled; }
		}
		
		[AsyncDispatch]
		public virtual void Dispose()
		{
			lock (progressTracker) {
				progressTracker.Done ();
				if (c.waitEvent != null)
					c.waitEvent.Set ();
			}
			OnCompleted ();
		}
		
		[FreeDispatch]
		public IAsyncOperation AsyncOperation
		{
			get { return this; }
		}
		
		[FreeDispatch]
		void IAsyncOperation.Cancel ()
		{
			OnCancelRequested ();
		}
		
		[FreeDispatch]
		void IAsyncOperation.WaitForCompleted ()
		{
			if (IsCompleted) return;
			
			if (Runtime.DispatchService.IsGuiThread) {
				while (!IsCompleted) {
					Runtime.DispatchService.RunPendingEvents ();
					Thread.Sleep (100);
				}
			} else {
				lock (progressTracker) {
					if (!progressTracker.InProgress) return;
					if (c.waitEvent == null)
						c.waitEvent = new ManualResetEvent (false);
				}
				c.waitEvent.WaitOne ();
			}
		}
		
		[FreeDispatch]
		bool IAsyncOperation.Success {
			get { return errorsMessages.Count == 0; }
		}
		
		[FreeDispatch]
		public bool IsCompleted
		{
			get { return !progressTracker.InProgress; }
		}
		
		public event OperationHandler Completed {
			add {
				bool alreadyCompleted = false;
				lock (progressTracker) {
					completedEvent += value;
					alreadyCompleted = !progressTracker.InProgress;
				}
				if (alreadyCompleted) value (this);
			}
			remove {
				lock (progressTracker) {
					completedEvent -= value;
				}
			}
		}
		
		public event MonitorHandler CancelRequested {
			add {
				bool alreadyCanceled = false;
				lock (progressTracker) {
					c.cancelRequestedEvent += value;
					alreadyCanceled = canceled;
				}
				if (alreadyCanceled) value (this);
			}
			remove {
				lock (progressTracker) {
					c.cancelRequestedEvent -= value;
				}
			}
		}		
		
		protected Exception ErrorException {
			get { return errorException; }
		}
		
		protected StringCollection Errors {
			get { return errorsMessages; }
		}
		
		protected StringCollection SuccessMessages {
			get { return successMessages; }
		}
		
		protected StringCollection Warnings {
			get { return warningMessages; }
		}
		
		protected string CurrentTask {
			get { return progressTracker.CurrentTask; }
		}
		
		protected double CurrentTaskWork {
			get { return progressTracker.CurrentTaskWork; }
		}
		
		protected double GlobalWork {
			get { return progressTracker.GlobalWork; }
		}
		
		protected bool UnknownWork {
			get { return progressTracker.UnknownWork; }
		}
		
		protected virtual void OnCompleted ()
		{
			if (completedEvent != null)
				completedEvent (AsyncOperation);
		}

		protected virtual void OnCancelRequested ()
		{
			lock (progressTracker) {
				canceled = true;
			}

			c.RaiseEvent(this);
		}

		[AsyncDispatch]
		void WriteLogInternal (string text)
		{
			OnWriteLog (text);
		}
			
		protected virtual void OnWriteLog (string text)
		{
		}
		
		protected virtual void OnProgressChanged ()
		{
		}
	}
}
