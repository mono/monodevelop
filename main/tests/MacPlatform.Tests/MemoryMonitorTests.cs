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
		[DllImport ("libc.dylib")]
		static extern IntPtr calloc (UIntPtr count, UIntPtr size);

		[DllImport ("libc.dylib")]
		static extern IntPtr valloc (UIntPtr size);

		[DllImport ("libc.dylib")]
		static extern void free (IntPtr ptr);

		[DllImport ("libc.dylib")]
		static extern void bzero (IntPtr ptr, UIntPtr size);

		// We just increase virtual memory here, need to figure out what's going wrong and why pagse don't get paged in
		[Test, Ignore]
		public async Task TestMemoryMonitorWithSimulatedValues ()
		{
			const uint allocSize = 1024 * 1024 * 1; // Allocate virtual memory in 1mb chunks.

			var totalMem = NSProcessInfo.ProcessInfo.PhysicalMemory;
			ulong pageCount = 0;
			ulong maxPageCount = totalMem / allocSize;
			IntPtr pages = calloc ((UIntPtr)maxPageCount, (UIntPtr)IntPtr.Size);

			try {
				using (var monitor = new MacPlatformService.MacMemoryMonitor ()) {
					var tcs = new TaskCompletionSource<bool> ();
					int count = 0;

					monitor.StatusChanged += (o, args) => {
						if (pageCount == 0)
							Assert.Ignore ("The system is already pressured at the time of the start of the test.");

						if (count == 0)
							Assert.AreEqual (PlatformMemoryStatus.Low, args.MemoryStatus);
						else if (count == 1) {
							Assert.AreEqual (PlatformMemoryStatus.Critical, args.MemoryStatus);
							Cleanup ();
						} else if (count == 2) {
							Assert.AreEqual (PlatformMemoryStatus.Normal, args.MemoryStatus);
							tcs.SetResult (true);
						} else
							throw new Exception ("Should not be reached");
						count++;
					};

					while (count < 2 && pageCount < maxPageCount) {
						IntPtr ptr = valloc ((UIntPtr)allocSize);
						if (ptr == IntPtr.Zero)
							break;

						bzero (ptr, (UIntPtr)allocSize);
						Marshal.WriteIntPtr (pages + (int)(pageCount * (ulong)IntPtr.Size), ptr);
					}

					await tcs.Task;
				}
			} finally {
				Cleanup ();
			}

			void Cleanup ()
			{
				IntPtr tempPage = pages;
				for (ulong i = 0; i < maxPageCount; ++i, tempPage += IntPtr.Size) {
					if (Marshal.ReadIntPtr (tempPage) == IntPtr.Zero)
						continue;
					Marshal.FreeHGlobal (tempPage);
				}
				Marshal.FreeHGlobal (pages);
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
