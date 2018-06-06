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
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Ide.BuildOutputView
{
	internal static class Counters
	{
		public static Counter OpenedFromIDE = InstrumentationService.CreateCounter ("Times opened from IDE", "Build Output", id: "BuildOutput.OpenedFromIde");
		public static Counter OpenedFromFile = InstrumentationService.CreateCounter ("Times opened from file", "Build Ouptut", id: "BuildOutput.OpenedFromFile");
		public static Counter SavedToFile = InstrumentationService.CreateCounter ("Times saved to file", "Build Output", id: "BuildOutput.SavedToFile");

		public static TimerCounter<BuildOutputCounterMetadata> ProcessBuildLog = InstrumentationService.CreateTimerCounter<BuildOutputCounterMetadata> ("Process binlog file", "Build Output", id: "BuildOutput.ProcessBuildLog");
		public static TimerCounter SearchBuildLog = InstrumentationService.CreateTimerCounter ("Search binlog", "Build Output", id: "BuildOutput.SearchBuildLog");
	}

	internal class BuildOutputCounterMetadata : CounterMetadata
	{
		public MSBuildVerbosity Verbosity {
			get => GetProperty<MSBuildVerbosity> ("Verbosity");
			set => SetProperty (value);
		}

		public int BuildCount {
			get => GetProperty<int> ("BuildCount");
			set => SetProperty (value);
		}

		public int RootNodesCount {
			get => GetProperty<int> ("RootNodesCount");
			set => SetProperty (value);
		}

		public long OnDiskSize {
			get => GetProperty<long> ("OnDiskSize");
			set => SetProperty (value);
		}
	}
}
