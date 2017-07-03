//
// PackageManagementBackgroundDispatcher.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Todd Berman  <tberman@off.net>
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	class PackageManagementBackgroundDispatcher
	{
		static Queue<Action> backgroundQueue = new Queue<Action> ();
		static ManualResetEvent backgroundThreadWait = new ManualResetEvent (false);
		static Thread backgroundThread;
		static Action action;

		public static void Initialize ()
		{
			backgroundThread = new Thread (new ThreadStart (RunDispatcher)) {
				Name = "NuGet background dispatcher",
				IsBackground = true,
				Priority = ThreadPriority.Lowest,
			};
			backgroundThread.Start ();
		}

		static void RunDispatcher ()
		{
			while (true) {
				action = null;
				bool wait = false;
				lock (backgroundQueue) {
					if (backgroundQueue.Count == 0) {
						backgroundThreadWait.Reset ();
						wait = true;
					} else
						action = backgroundQueue.Dequeue ();
				}

				if (wait) {
					backgroundThreadWait.WaitOne ();
					continue;
				}

				if (action != null) {
					try {
						action ();
					} catch (Exception ex) {
						LoggingService.LogError ("BackgroundDispatcher error.", ex);
					}
				}
			}
		}

		public static void Dispatch (Action action)
		{
			QueueBackground (action);
		}

		static void QueueBackground (Action action)
		{
			lock (backgroundQueue) {
				backgroundQueue.Enqueue (action);
				if (backgroundQueue.Count == 1)
					backgroundThreadWait.Set ();
			}
		}

		public static void Clear ()
		{
			lock (backgroundQueue) {
				backgroundQueue.Clear ();
			}
		}

		public static bool IsDispatching ()
		{
			return backgroundQueue.Count > 0 || action != null;
		}
	}
}

