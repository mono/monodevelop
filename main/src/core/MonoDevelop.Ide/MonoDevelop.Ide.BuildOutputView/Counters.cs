//
// Counters.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018 Microsoft Corp. (http://microsoft.com)
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
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Ide.BuildOutputView
{
	internal static class Counters
	{
		public static Counter BuildOutputTimesOpened = InstrumentationService.CreateCounter ("Times opened", "Build Output", id: "BuildOutput.TimesOpened");
		public static Counter NormalViewSelected = InstrumentationService.CreateCounter ("Normal view selected", "Build Output", id: "BuildOutput.NormalViewSelected");
		public static Counter DiagnosticsViewSelected = InstrumentationService.CreateCounter ("Diagnostics view selected", "Build Ouptut", id "BuildOutput.DiagnosticsViewSelected");

		public static TimerCounter ProcessBuildLog = InstrumentationService.CreateTimerCounter ("Process binlog file", "Build Output", id: "BuildOutput.ProcessBuildLog");
		public static TimerCounter SearchBuildLog = InstrumentationService.CreateTimerCounter ("Search binlog", "Build Output", id: "BuildOutput.SearchBuildLog");
	}
}
