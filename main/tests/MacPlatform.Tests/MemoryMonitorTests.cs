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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AppKit;
using CoreFoundation;
using Foundation;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.MacIntegration;
using NUnit.Framework;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class MemoryMonitorTests : IdeTestBase
	{
		[Test]
		[Ignore (@"We either need administrative privileges or to keep the system at high memory allocation pressure for a long time.
The main problem with writing a manual test is that it's hard to not get the memory into compressed memory.'")]
		public async Task TestMemoryMonitorWithSimulatedValues ()
		{
			using (var monitor = new MacPlatformService.MacMemoryMonitor ()) {
				var tcs = new TaskCompletionSource<bool> ();
				int statusChangedCount = 0;

				monitor.StatusChanged += (o, args) => {
					if (statusChangedCount == 0)
						Assert.AreEqual (PlatformMemoryStatus.Low, args.MemoryStatus);
					else if (statusChangedCount == 1) {
						Assert.AreEqual (PlatformMemoryStatus.Critical, args.MemoryStatus);
					} else if (statusChangedCount == 2) {
						Assert.AreEqual (PlatformMemoryStatus.Normal, args.MemoryStatus);
						tcs.SetResult (true);
					} else
						throw new Exception ("Should not be reached");
					statusChangedCount++;
				};

				await SimulateMemoryPressure ("warn");
				await SimulateMemoryPressure ("critical");
				await SimulateMemoryPressure ("normal");

				await tcs.Task;
			}
		}

		// We need root for this to work.
		static Task SimulateMemoryPressure (string kind)
		{
			var url = NSUrl.FromFilename ("/usr/bin/memory_pressure");
			var config = new NSMutableDictionary ();

			var args = new NSMutableArray<NSString> (3);
			args.Add (new NSString ("-S"));
			args.Add (new NSString ("-l"));
			args.Add (new NSString (kind));

			config.Add (NSWorkspace.LaunchConfigurationArguments, args);

			var done = new TaskCompletionSource<bool> ();
			var app = NSWorkspace.SharedWorkspace.LaunchApplication (url, NSWorkspaceLaunchOptions.NewInstance, config, out var error);

			NSNotificationCenter.DefaultCenter.AddObserver (NSWorkspace.DidTerminateApplicationNotification, notification => {
				done.SetResult (true);
			});

			return done.Task;
		}
	}
}
