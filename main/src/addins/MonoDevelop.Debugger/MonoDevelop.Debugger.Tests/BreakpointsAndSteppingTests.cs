//
// BreakpointStoreTests.cs
//
// Author:
//       Greg Munn <gregm@microsoft.com>
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
using NUnit.Framework;
using Mono.Debugging.Client;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mono.Debugging.Tests
{
	[TestFixture]
	public class BreakpointStoreTests
	{

		[Test]
		public async Task WhenStoreModifiedExternallyThenIteratingOverBreakpointsShouldNotThrow ()
		{
			var store = new BreakpointStore ();

			Action<int> threadAddAction = (x) => {
				store.Add (new Breakpoint ($"fileName{x}.cs", 10));
			};

			Action threadEnumerateAction = () => {
				foreach (var bk in store.GetBreakpointsAtFile ("fileName1.cs")) {
				}
			};

			List<Task> tasks = new List<Task> ();

			for (int i = 0; i < 10; i++) {
				tasks.Add (Task.Run (() => {
					threadAddAction (i);
				}));

				tasks.Add (Task.Run (() => {
					threadEnumerateAction ();
				}));
			}

			foreach (var task in tasks) {
				await task;
			}
		}

		[Test]
		public void GetBreakpointsAtFileReturnsCorrectFiles ()
		{
			var store = new BreakpointStore ();

			store.Add (new Breakpoint ("fileName1.cs", 10));
			store.Add (new Breakpoint ("fileName1.cs", 20));
			store.Add (new Breakpoint ("fileName2.cs", 10));
			store.Add (new Breakpoint ("fileName3.cs", 15));

			int count = 0;
			foreach (var bk in store.GetBreakpointsAtFile ("fileName1.cs")) {
				count++;
				Assert.AreEqual ("fileName1.cs", bk.FileName);
			}

			Assert.AreEqual (2, count);
		}
	}
}