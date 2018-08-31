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
using System.Threading;
using MonoDevelop.Ide;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	class TextEditorKeyPressTimingsTests : TextEditorTestBase
	{
		[Test]
		public void TestSimpleTimer ()
		{
			var timings = new TextEditorKeyPressTimings ();

			var telemetry = DesktopService.PlatformTelemetry;
			if (telemetry == null)
				Assert.Ignore ("Platform does not implement telemetry details");

			var time = (long)telemetry.TimeSinceMachineStart.TotalMilliseconds;
			timings.StartTimer (time);
			Thread.Sleep (800);
			timings.EndTimer ();

			var metadata = timings.GetTypingTimingMetadata (null);
			Assert.That (metadata.First, Is.GreaterThanOrEqualTo (800.0));
			Assert.That (metadata.First, Is.LessThanOrEqualTo (1600));
		}
	}
}
