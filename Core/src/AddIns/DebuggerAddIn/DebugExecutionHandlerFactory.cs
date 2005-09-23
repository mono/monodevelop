//
// DebugExecutionHandlerFactory.cs
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
using MonoDevelop.Services;

namespace MonoDevelop.Debugger
{
	internal class DebugExecutionHandlerFactory: IExecutionHandlerFactory
	{
		DebuggingService service;
		
		public DebugExecutionHandlerFactory (DebuggingService service)
		{
			this.service = service;
		}
		
		public IExecutionHandler CreateExecutionHandler (string platformId)
		{
			if (Runtime.DebuggingService == null)
				return null;

			if (platformId == "Mono")
				return new DebugExecutionHandler (service);
			else
				return null;
		}
	}
	
	class DebugExecutionHandler: IExecutionHandler, IProcessAsyncOperation
	{
		bool done;
		ManualResetEvent stopEvent;
		DebuggingService service;
		
		public DebugExecutionHandler (DebuggingService service)
		{
			this.service = service;
			service.StoppedEvent += new EventHandler (OnStopDebug);
		}
		
		public IProcessAsyncOperation Execute (string command, string arguments, string workingDirectory, IConsole console)
		{
			service.Run (console, new string[] { command } );
			return this;
		}
		
		public void Cancel ()
		{
			service.Stop ();
		}
		
		public void WaitForCompleted ()
		{
			lock (this) {
				if (done) return;
				if (stopEvent == null)
					stopEvent = new ManualResetEvent (false);
			}
			stopEvent.WaitOne ();
		}
		
		public int ExitCode {
			get { return 0; }
		}
		
		public bool IsCompleted {
			get { return done; }
		}
		
		public bool Success {
			get { return true; }
		}

		void OnStopDebug (object sender, EventArgs args)
		{
			lock (this) {
				done = true;
				if (stopEvent != null)
					stopEvent.Set ();
				if (completedEvent != null)
					completedEvent (this);
			}

			service.StoppedEvent -= new EventHandler (OnStopDebug);
		}
		
		event OperationHandler IAsyncOperation.Completed {
			add {
				bool raiseNow = false;
				lock (this) {
					if (done)
						raiseNow = true;
					else
						completedEvent += value;
				}
				if (raiseNow)
					value (this);
			}
			remove {
				lock (this) {
					completedEvent -= value;
				}
			}
		}
		
		event OperationHandler completedEvent;
	}
}
