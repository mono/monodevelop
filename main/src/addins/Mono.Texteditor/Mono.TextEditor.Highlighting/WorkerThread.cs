//
// WorkerThread.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

namespace Mono.TextEditor
{
	public abstract class WorkerThread
	{
		readonly object syncObject = new object();
		
		bool isStopping = false;
		bool isStopped  = false;
		
		public bool IsStopping {
			get {
				lock (syncObject) {
					return isStopping;
				}
			}
		}
		
		public bool IsStopped {
			get {
				lock (syncObject) {
					return isStopped;
				}
			}
		}
		Thread thread;
		
		public void Start ()
		{
			thread = new Thread (new ThreadStart (Run));
			thread.Priority = ThreadPriority.Lowest;
			thread.IsBackground = true;
			thread.Start ();
		}
		
		public void Stop ()
		{
			lock (syncObject) {
				isStopping = true;
			}
		}
		
		public void WaitForFinish ()
		{
			if (thread != null && !IsStopped)
				thread.Join (500);
		}
		
		void SetStopped ()
		{
			lock (syncObject) {
				isStopped = true;
			}
		}
		
		protected abstract void InnerRun ();
		
		void Run ()
		{
			try {
				while (!IsStopping) {
					InnerRun ();
				}
			} catch (Exception ex) {
				System.Console.WriteLine ("Exception in highlighting worker thread:", ex);
			} finally {
				SetStopped ();
			}
		}
	}
}
