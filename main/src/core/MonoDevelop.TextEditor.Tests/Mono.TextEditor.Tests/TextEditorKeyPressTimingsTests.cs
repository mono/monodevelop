//
// TextEditorKeyPressTimingsTests.cs
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
using System.Security.Cryptography;
using System.Threading;
using MonoDevelop.Ide;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	class TextEditorKeyPressTimingsTests : TextEditorTestBase
	{
		const double TicksPerMillisecond = 1e4;

		static TypingTimingMetadata TimeAction (Action action)
		{
			var timings = new TextEditorKeyPressTimings (null);

			var telemetry = IdeServices.DesktopService.PlatformTelemetry;
			if (telemetry == null)
				Assert.Ignore ("Platform does not implement telemetry details");

			timings.StartTimer (telemetry.TimeSinceMachineStart);
			action ();
			timings.EndTimer ();

			return timings.GetTypingTimingMetadata (null, null, 0, 0);
		}

		[Test]
		public void TestSimpleTimer ()
		{
			var metadata = TimeAction (() => Thread.Sleep (800));
			Assert.That (metadata.First, Is.InRange (800, 1600));
			Assert.That (metadata.SessionLength, Is.GreaterThanOrEqualTo (0));
		}

		[Test]
		public void TestHighPrecision ()
		{
			const int rounds = 200;
			int assertPasses = 0;

			for (int i = 0; i < rounds; i++) {
				var metadata = TimeAction (() => {
					// just do a bunch of CPU busy work so each iteration has very different timings
					for (int j = 0; j < 100; j++) {
						var buffer = new byte [new Random ().Next (1, 4096)];
						RandomNumberGenerator.Create ().GetBytes (buffer);
					}
				});

				if (metadata.First - Math.Truncate (metadata.First) > 0)
					assertPasses++;
			}

			// at least 80% of action timings should have precision better than one millisecond
			Assert.That (assertPasses / (double)rounds, Is.GreaterThan (0.80));
		}
	}
}
