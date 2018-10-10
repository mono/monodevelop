//
// RaceCheckerTests.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MonoDevelop.Utilities
{
	[TestFixture]
	public class RaceCheckerTests
	{
		class CaptureRaceChecker : RaceChecker
		{
			public List<(string, string)> Traces = new List<(string, string)> ();

			protected override void OnRace (string trace1, string trace2)
			{
				Traces.Add ((trace1, trace2));
			}
		}

		[Test]
		public async Task CheckRaceDetected ()
		{
			const int count = 3;

			var tasks = new Task [count];
			var raceChecker = new CaptureRaceChecker ();

			for (int i = 0; i < count; ++i) {
				tasks [i] = Task.Run (() => {
					using (raceChecker.Lock ()) {
						// Do not use await Task.Delay, as we need to be on the same thread.
						Thread.Sleep (100);
					}
				});
			}

			await Task.WhenAll (tasks);
			Assert.AreEqual (2, raceChecker.Traces.Count);
		}
	}
}
