//
// BackgroundDispatcher.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	class BackgroundDispatcher
	{
		Queue<Action> backgroundQueue = new Queue<Action> ();
		ManualResetEvent backgroundThreadWait = new ManualResetEvent (false);
		Thread backgroundThread;
		bool stopped;

		public void Start (string name)
		{
			backgroundThread = new Thread (new ThreadStart (RunDispatcher)) {
				Name = name,
				IsBackground = true,
				Priority = ThreadPriority.Lowest,
			};
			backgroundThread.Start ();
		}

		void RunDispatcher ()
		{
			while (!stopped) {
				Action action = null;
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

		public void Dispatch (Action action)
		{
			QueueBackground (action);
		}

		void QueueBackground (Action action)
		{
			lock (backgroundQueue) {
				backgroundQueue.Enqueue (action);
				if (backgroundQueue.Count == 1)
					backgroundThreadWait.Set ();
			}
		}

		public void Stop ()
		{
			stopped = true;
			backgroundThreadWait.Set ();
		}
	}
}

