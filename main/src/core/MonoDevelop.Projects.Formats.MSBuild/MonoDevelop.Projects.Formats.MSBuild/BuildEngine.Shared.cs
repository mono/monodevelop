// 
// ProjectBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.Runtime.Remoting;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

//this is the builder for the deprecated build engine API
#pragma warning disable 618

namespace MonoDevelop.Projects.Formats.MSBuild
{
	partial class BuildEngine: MarshalByRefObject, IBuildEngine
	{
		static readonly AutoResetEvent workDoneEvent = new AutoResetEvent (false);
		static ThreadStart workDelegate;
		static readonly object workLock = new object ();
		static Thread workThread;
		static Exception workError;

		static List<int> cancelledTasks = new List<int> ();
		static int currentTaskId;

		readonly ManualResetEvent doneEvent = new ManualResetEvent (false);

		public void Dispose ()
		{
			doneEvent.Set ();
		}
		
		internal WaitHandle WaitHandle {
			get { return doneEvent; }
		}

		public void Ping ()
		{
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}

		public void CancelTask (int taskId)
		{
			lock (cancelledTasks) {
				if (currentTaskId == taskId)
					AbortCurrentTask ();
				else
					cancelledTasks.Add (taskId);
			}
		}

		static bool IsTaskCancelled (int taskId)
		{
			lock (cancelledTasks) {
				return cancelledTasks.Contains (taskId);
			}
		}

		static bool SetCurrentTask (int taskId)
		{
			lock (cancelledTasks) {
				if (cancelledTasks.Contains (taskId))
					return false;
				currentTaskId = taskId;
				return true;
			}
		}

		static void ResetCurrentTask ()
		{
			lock (cancelledTasks) {
				currentTaskId = -1;
			}
		}

		static void AbortCurrentTask ()
		{
			workThread.Abort ();
			workThread = null;
			workDoneEvent.Set ();
		}

		internal static void RunSTA (ThreadStart ts)
		{
			RunSTA (-1, ts);
		}

		internal static void RunSTA (int taskId, ThreadStart ts)
		{
			lock (workLock) {
				if (IsTaskCancelled (taskId))
					return;
				lock (threadLock) {
					// Last chance to check for canceled task before the thread is started
					if (IsTaskCancelled (taskId))
						return;
					
					workDelegate = ts;
					workError = null;
					if (workThread == null) {
						workThread = new Thread (STARunner);
						workThread.SetApartmentState (ApartmentState.STA);
						workThread.IsBackground = true;
						workThread.CurrentUICulture = uiCulture;
						workThread.Start ();
					}
					else
						// Awaken the existing thread
						Monitor.Pulse (threadLock);
				}
				if (!SetCurrentTask (taskId)) {
					// The task was aborted after all. Since the thread is already running we need to abort it
					AbortCurrentTask ();
					return;
				}

				workDoneEvent.WaitOne ();

				ResetCurrentTask ();
			}
			if (workError != null)
				throw new Exception ("MSBuild operation failed", workError);
		}

		static readonly object threadLock = new object ();
		
		static void STARunner ()
		{
			lock (threadLock) {
				do {
					try {
						workDelegate ();
					}
					catch (Exception ex) {
						workError = ex;
					}
					workDoneEvent.Set ();
				}
				while (Monitor.Wait (threadLock, 60000));
				
				workThread = null;
			}
		}
	}
}