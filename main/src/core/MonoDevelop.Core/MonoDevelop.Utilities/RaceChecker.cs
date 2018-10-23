//
// RaceChecker.cs
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
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.Utilities
{
	abstract class RaceChecker
	{
		string lockHolderStacktrace;
		readonly object lockObject;
		readonly Disposer lockRelease;

		protected RaceChecker ()
		{
			lockObject = new object ();
			lockRelease = new Disposer (lockObject);
		}

		public IDisposable Lock ()
		{
			if (Monitor.TryEnter (lockObject)) {
				lockHolderStacktrace = Environment.StackTrace;
			} else {
				Monitor.Enter (lockObject);
				var thisTrace = Environment.StackTrace;
				OnRace (lockHolderStacktrace, thisTrace);
				lockHolderStacktrace = thisTrace;
			}

			return lockRelease;
		}

		protected abstract void OnRace (string trace1, string trace2);

		class Disposer : IDisposable
		{
			readonly object lockObject;

			public Disposer (object lockObject)
			{
				this.lockObject = lockObject;
			}

			public void Dispose ()
			{
				Monitor.Pulse (lockObject);
				Monitor.Exit (lockObject);
			}
		}
	}

	class LoggingRaceChecker : RaceChecker
	{
		protected override void OnRace (string trace1, string trace2)
		{
			LoggingService.LogError ($"Detected race:{Environment.NewLine}Stacktrace 1:{Environment.NewLine}{trace1}{Environment.NewLine}Stacktrace 2:{Environment.NewLine}{trace2}");
		}
	}
}
