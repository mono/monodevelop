//
// TestSingleThreadSynchronizationContext.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Concurrent;
using System.Threading;
namespace UnitTests
{
	/// <summary>
	/// A <see cref="SynchronizationContext"/> that can be used to test whether actions happen on it.
	/// </summary>
	public class TestSingleThreadSynchronizationContext : SynchronizationContext, IDisposable
	{
		public int CallCount { get; private set; }
		public Thread Thread { get; } = new Thread (ProcessQueue);
		readonly BlockingCollection<Tuple<SendOrPostCallback, object, ManualResetEvent>> queue =
			new BlockingCollection<Tuple<SendOrPostCallback, object, ManualResetEvent>> ();

		public TestSingleThreadSynchronizationContext ()
		{
			Thread.Start (queue);
		}

		public void Dispose ()
		{
			queue.CompleteAdding ();
		}

		static void ProcessQueue (object state)
		{
			var queue = (BlockingCollection<Tuple<SendOrPostCallback, object, ManualResetEvent>>)state;
			Tuple<SendOrPostCallback, object, ManualResetEvent> tuple;

			while (queue.TryTake (out tuple, Timeout.Infinite)) {
				tuple.Item1 (tuple.Item2);
				tuple.Item3?.Set ();
			}
		}

		public override void Send (SendOrPostCallback d, object state)
		{
			CallCount++;
			var mre = new ManualResetEvent (false);
			queue.Add (Tuple.Create (d, state, mre));
			mre.WaitOne ();
		}

		public override void Post (SendOrPostCallback d, object state)
		{
			CallCount++;
			queue.Add (Tuple.Create (d, state, default (ManualResetEvent)));
		}
	}
}
