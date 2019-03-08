//
// AsyncCriticalSectionTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using NUnit.Framework;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class AsyncCriticalSectionTests
	{
		AsyncCriticalSection referenceCacheLock;
		ManualResetEvent unlockEvent;
		ManualResetEvent doneEvent;
		const int maxLoadProjectCalls = 10000;

		[Test]
		public void StackOverflowTest ()
		{
			referenceCacheLock = new AsyncCriticalSection ();
			unlockEvent = new ManualResetEvent (false);
			doneEvent = new ManualResetEvent (false);

			for (int i = 0; i < maxLoadProjectCalls; ++i) {
				Run (i);
			}

			Thread.Sleep (500);

			unlockEvent.Set ();
			bool result = doneEvent.WaitOne (5000);
			if (!result)
				Assert.Fail ("Done event not fired.");
		}

		void Run (int i)
		{
			Task.Run (async () => {
				await LoadProject (i);
			});
		}

		async Task LoadProject (int i)
		{
			using (await referenceCacheLock.EnterAsync ().ConfigureAwait (false)) {
				unlockEvent.WaitOne ();

				if (i == maxLoadProjectCalls - 1)
					doneEvent.Set ();
			}
		}
	}
}
