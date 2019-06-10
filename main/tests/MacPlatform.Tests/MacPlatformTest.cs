// 
// MacPlatformTest.cs
//  
// Author:
//       Alan McGovenrn <alan@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using MonoDevelop.MacIntegration;
using MonoDevelop.MacInterop;
using MonoDevelop.Ide;
using UnitTests;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Core.Logging;
using Foundation;
using System.Runtime.InteropServices;
using System.Linq;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class MacPlatformTest : IdeTestBase
	{
		[Test]
		public void TestPartialStaticRegistrar ()
		{
			var runtimeType = typeof (ObjCRuntime.Runtime);
			var optionsField = runtimeType.GetField ("options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

			if (optionsField.GetValue (null) == null) {
				Assert.Inconclusive ("This may have been run without the stub launcher, abort.");
			}
		}

		[Test]
		public void TestStaticRegistrar ()
		{
			var entryAssembly = System.Reflection.Assembly.GetEntryAssembly ();
			var mdtoolDirectory = Path.GetDirectoryName (entryAssembly.Location);
			if (!File.Exists (Path.Combine (mdtoolDirectory, "libvsmregistrar.dylib"))) {
				// We don't have a full static registrar
				return;
			}

			var @class = new ObjCRuntime.Class (typeof (MonoDevelop.MacIntegration.MainToolbar.StatusIcon));

			var findType = typeof (ObjCRuntime.Class).GetMethod ("FindType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

			var result = (Type)findType.Invoke (null, new object [] { @class.Handle, null });
			Assert.IsNotNull (result);
		}

		[Test]
		public void GetMimeType_text ()
		{
			// Verify no exception is thrown
			IdeServices.DesktopService.GetMimeTypeForUri ("test.txt");
		}

		[Test]
		public void GetMimeType_NoExtension ()
		{
			// Verify no exception is thrown
			IdeServices.DesktopService.GetMimeTypeForUri ("test");
		}

		[Test]
		public void GetMimeType_Null ()
		{
			// Verify no exception is thrown
			IdeServices.DesktopService.GetMimeTypeForUri (null);
		}

		[Test]
		public void MacHasProperMonitor ()
		{
			Assert.That (IdeServices.DesktopService.MemoryMonitor, Is.TypeOf<MacPlatformService.MacMemoryMonitor> ());
		}

		[Test, Timeout(20000)]
		public async Task TestMacMemoryMonitorLifetime ()
		{
			var tcs = new TaskCompletionSource<bool> ();

			using (var macMonitor = new MacPlatformService.MacMemoryMonitor ()) {
				// Cancellation is async.
				macMonitor.DispatchSource.SetCancelHandler (() => tcs.SetResult (true));
			}

			Assert.AreEqual (true, await tcs.Task, "Expected cancel handler to be called");
		}

		[Test]
		public void TestIOKitPInvokes ()
		{
			// Test the pinvokes don't crash. Don't care about the details returned

			var matchingDict = MacTelemetryDetails.IOServiceMatching ("IOService");

			// IOServiceGetMatchingServices takes ownership of matchingDict, so no need to CFRelease it
			var success = MacTelemetryDetails.IOServiceGetMatchingServices (0, matchingDict, out var iter);

			if (MacTelemetryDetails.IOIteratorIsValid (iter) == 0) {
				// An invalid iter isn't a test failure, but it means we can't really test anything else
				// so just return
				return;
			}

			var entry = MacTelemetryDetails.IOIteratorNext (iter);
			if (entry == 0) {
				MacTelemetryDetails.IOObjectRelease (iter);
				return;
			}

			success = MacTelemetryDetails.IORegistryEntryGetChildIterator (entry, "IOService", out var childIter);
			if (success != 0) {
				MacTelemetryDetails.IOObjectRelease (entry);
				return;
			}

			MacTelemetryDetails.IOObjectRelease (childIter);

			var name = new Foundation.NSString ("testService");
			var namePtr = MacTelemetryDetails.IORegistryEntrySearchCFProperty (entry, "IOService", name.Handle, IntPtr.Zero, 0x0);

			MacTelemetryDetails.IOObjectRelease (entry);
			MacTelemetryDetails.IOObjectRelease (iter);

			MacTelemetryDetails.CFRelease (namePtr);
		}

		[DllImport ("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
		static extern void void_objc_msgSend (IntPtr receiver, IntPtr selector);

		[Test]
		public void TestNSExceptionLogging ()
		{
			var crashReporter = new CapturingCrashReporter ();

			try {
				LoggingService.RegisterCrashReporter (crashReporter);

				var x = new NSException ("Test", "should be captured", null);
				var selector = ObjCRuntime.Selector.GetHandle ("raise");

				Assert.Throws<ObjCException> (() => void_objc_msgSend (x.Handle, selector));

				Assert.That (crashReporter.LastException.Message, Contains.Substring ("should be captured"));
				Assert.That (crashReporter.LastException.Source, Is.Not.Null);

				var stacktrace = crashReporter.LastException.StackTrace;
				AssertMacPlatformStacktrace (stacktrace);
			} finally {
				LoggingService.UnregisterCrashReporter (crashReporter);
			}
		}

		[Test]
		public void CriticalErrorsExceptionsHaveFullStacktracesInLog ()
		{
			var logger = new CapturingLogger {
				EnabledLevel = EnabledLoggingLevel.Fatal,
			};

			try {
				LoggingService.AddLogger (logger);

				var ex = new NSException ("Test", "should be captured", null);
				var selector = ObjCRuntime.Selector.GetHandle ("raise");

				Assert.Throws<ObjCException> (() => void_objc_msgSend (ex.Handle, selector));

				var (_, message) = logger.LogMessages.Single (x => x.Level == LogLevel.Fatal);
				AssertMacPlatformStacktrace (message);
			} finally {
				LoggingService.RemoveLogger (logger.Name);
			}
		}

		static void AssertMacPlatformStacktrace (string stacktrace)
		{
			Assert.That (stacktrace, Contains.Substring ("at MonoDevelopProcessHost.Main"));
			Assert.That (stacktrace, Contains.Substring ("at MacPlatform.Tests.MacPlatformTest.void_objc_msgSend"));
			Assert.That (stacktrace, Contains.Substring ("at MonoDevelop.MacIntegration.MacPlatformService.HandleUncaughtException"));

		}
	}
}

