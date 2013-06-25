//
// TextTemplatingFileGenerator.cs
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide.CustomTools;
using MonoDevelop.Core;
using System.Threading;

namespace MonoDevelop.Ide.CustomTools
{
	public class ThreadAsyncOperation : IAsyncOperation
	{
		Thread thread;
		bool cancelled;
		SingleFileCustomToolResult result;
		Action task;

		public ThreadAsyncOperation (Action task, SingleFileCustomToolResult result)
		{
			if (result == null)
				throw new ArgumentNullException ("result");

			this.task = task;
			this.result = result;
			thread = new Thread (Run);
			thread.Start ();
		}

		void Run ()
		{
			try {
				task ();
			} catch (ThreadAbortException ex) {
				result.UnhandledException = ex;
				Thread.ResetAbort ();
			} catch (Exception ex) {
				result.UnhandledException = ex;
			}
			if (Completed != null)
				Completed (this);
		}

		public event OperationHandler Completed;

		public void Cancel ()
		{
			cancelled = true;
			thread.Abort ();
		}

		public void WaitForCompleted ()
		{
			thread.Join ();
		}

		public bool IsCompleted {
			get { return !thread.IsAlive; }
		}

		public bool Success {
			get { return !cancelled && result.Success; }
		}

		public bool SuccessWithWarnings {
			get { return !cancelled && result.SuccessWithWarnings; }
		}
	}
}
