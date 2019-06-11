//
// GLibLoggingTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Logging;
using MonoDevelop.Core.LogReporting;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Gui
{
	[TestFixture]
	public class GLibLoggingTests
	{
		[Test]
		public void ValidateCrashIsSentForGLibExceptions ()
		{
			var old = GLibLogging.Enabled;
			var crashReporter = new CapturingCrashReporter ();

			try {
				GLibLogging.Enabled = true;

				LoggingService.RegisterCrashReporter (crashReporter);

				GLib.Log.Write ("Gtk", GLib.LogLevelFlags.Warning, "{0}", "should not be captured");
				Assert.IsNull (crashReporter.LastException);

				GLib.Log.Write ("Gtk", GLib.LogLevelFlags.Critical, "{0}", "critical should be captured");
				Assert.That (crashReporter.LastException.Message, Contains.Substring ("critical should be captured"));
				Assert.That (crashReporter.LastException.Source, Is.Not.Null);

				var stacktrace = crashReporter.LastException.StackTrace;
				AssertGLibStackTrace (stacktrace);

				// Error will cause the application to exit, so we can't test for that, but it follows the same code as Critical.
			} finally {
				LoggingService.UnregisterCrashReporter (crashReporter);
				GLibLogging.Enabled = old;
			}
		}

		[Test]
		public void GLibLoggingHaveFullStacktracesInLog ()
		{
			var old = GLibLogging.Enabled;

			try {
				GLibLogging.Enabled = true;

				var flagsToCheck = new [] {
					(GLib.LogLevelFlags.Critical, LogLevel.Error),
					(GLib.LogLevelFlags.Debug, LogLevel.Debug),
					(GLib.LogLevelFlags.Info, LogLevel.Info),
					//(GLib.LogLevelFlags.Error, LogLevel.Fatal),
				};

				foreach (var (glibLevel, coreLevel) in flagsToCheck) {
					var logger = new CapturingLogger ();

					LoggingService.AddLogger (logger);
					try {
						GLib.Log.Write ("Gtk", glibLevel, "{0}: should be captured", glibLevel);
						var (_, message) = logger.LogMessages.Single (x => x.Level == coreLevel);
						AssertGLibStackTrace (message);
					} finally {
						LoggingService.RemoveLogger (logger.Name);
					}
				}
			} finally {
				GLibLogging.Enabled = old;
			}
		}

		static void AssertGLibStackTrace(string stacktrace)
		{
			Assert.That (stacktrace, Contains.Substring ("at MonoDevelopProcessHost.Main"));
			Assert.That (stacktrace, Contains.Substring ("at GLib.Log.g_log"));
			Assert.That (stacktrace, Contains.Substring ("at MonoDevelop.Ide.Gui.GLibLogging.LoggerMethod"));
			Assert.That (stacktrace, Contains.Substring ("at MonoDevelop.Ide.Gui.GLibLoggingTests"));
		}
	}
}
