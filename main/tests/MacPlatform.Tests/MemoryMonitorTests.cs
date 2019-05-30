//
// MemoryMonitorTests.cs
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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.MacIntegration;
using NUnit.Framework;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class MemoryMonitorTests : IdeTestBase
	{
		[Test]
		public async Task TestMemoryMonitorWithSimulatedValues ()
		{
			using (var monitor = new MacPlatformService.MacMemoryMonitor ()) {
				var countsReached = new Dictionary<PlatformMemoryStatus, int> ();

				monitor.StatusChanged += (o, args) => {
					if (!countsReached.TryGetValue (args.MemoryStatus, out int count))
						count = 0;
					countsReached [args.MemoryStatus] = ++count;
				};

				await SimulateMemoryPressure ("warn");
				await SimulateMemoryPressure ("critical");

				Assert.That (countsReached [PlatformMemoryStatus.Low], Is.GreaterThanOrEqualTo (1));
				Assert.That (countsReached [PlatformMemoryStatus.Critical], Is.GreaterThanOrEqualTo (1));
			}
		}

		// We need root for this to work.
		static async Task SimulateMemoryPressure (string kind)
		{
			using (var cts = new CancellationTokenSource (TimeSpan.FromSeconds (3))) {
				var done = new TaskCompletionSource<bool> ();

				var psi = new ProcessStartInfo ("/usr/bin/sudo", "-n /usr/bin/memory_pressure -S -l " + kind) {
				};
				
				using (var process = Process.Start (psi)) {
					process.Exited += (o, args) => done.SetResult (true);
					process.EnableRaisingEvents = true;

					await done.Task;
				}
			}
		}
	}
}
