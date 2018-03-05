//
// AsyncCriticalSection.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	/// <summary>
	/// A critical section object which can be awaited
	/// </summary>
	/// <remarks>This critical section is not reentrant.</remarks>
	public class AsyncCriticalSection
	{
		class CriticalSectionDisposer: IDisposable
		{
			public AsyncCriticalSection AsyncCriticalSection;

			public void Dispose ()
			{
				AsyncCriticalSection.Exit ();
			}
		}

		IDisposable criticalSectionDisposer;
		Queue<TaskCompletionSource<IDisposable>> queue = new Queue<TaskCompletionSource<IDisposable>> ();
		bool locked;

		public AsyncCriticalSection ()
		{
			criticalSectionDisposer = new CriticalSectionDisposer { AsyncCriticalSection = this };
		}

		public IDisposable Enter ()
		{
			return EnterAsync ().Result;
		}

		public Task<IDisposable> EnterAsync ()
		{
			lock (queue) {
				if (!locked) {
					locked = true;
					return Task.FromResult (criticalSectionDisposer);
				}
				var s = new TaskCompletionSource<IDisposable> ();
				queue.Enqueue (s);
				return s.Task;
			}
		}

		void Exit ()
		{
			TaskCompletionSource<IDisposable> cs = null;
			lock (queue) {
				if (queue.Count > 0) {
					cs = queue.Dequeue ();
				} else
					locked = false;
			}
			// Set the result outside the lock, otherwise this can lead to stack overflow on task continuations.
			cs?.SetResult (criticalSectionDisposer);
		}
	}
}

