//
// TestFileWatcherHandlers.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.PerformanceTesting;
using MonoDevelop.UserInterfaceTesting;
using NUnit.Framework;

namespace MonoDevelop.Ide.PerfTests
{
	[TestFixture]
	[BenchmarkCategory]
	public class TestFileWatcherHandlers : UITestBase
	{
		public override void SetUp ()
		{
			InstrumentationService.Enabled = true;
			PreStart ();
		}

		[Test]
		[Benchmark (Tolerance = 0.20)]
		public void TestHandlers ()
		{
			const int fileCount = 1000;
			OpenApplicationAndWait ();

			var solutionDirectory = Path.GetDirectoryName (OpenExampleSolutionAndWait (out _));
			var stressDirectory = Path.Combine (solutionDirectory, "sdk-library", "stress_fsw");

			// Wait for the Workspace to finish loading.
			if (Session.GetTimerCount ("Ide.Workspace.RoslynWorkspaceLoaded") == 0) {
				Session.RunAndWaitForTimer (() => { }, "Ide.Workspace.RoslynWorkspaceLoaded", 100 * 1000);
			}

			StressFileWatchers (stressDirectory, fileCount);

			var totalTime = TimeSpan.Zero;

			foreach (var kvp in trackedCounters) {
				const string UIThreadMethod = "MonoDevelop.Core.FileService.eventQueue.GetTimings";
				const string BackgroundMethod = "MonoDevelop.Projects.FileWatcherService.Timings.GetTimings";

				var enumValue = kvp.Key;
				var counterId = kvp.Value;
				var counterValue = Session.WaitForCounterToExceed (counterId, 0);

				var durationUI = GetDuration (UIThreadMethod, enumValue);
				var durationBackground = GetDuration (BackgroundMethod, enumValue);

				Console.WriteLine (
					"Processed {0} events {1} in UI '{2}' BG '{3}'",
					counterValue.ToString (),
					counterId,
					durationUI.ToString (),
					durationBackground.ToString ()
				);

				totalTime += durationUI + durationBackground;
			}

			Benchmark.SetTime (totalTime.TotalSeconds);
		}

		const int Created = 0;
		const int Changed = 1;
		const int Removed = 4;
		const int Renamed = 5;
		static readonly Dictionary<int, string> trackedCounters = new Dictionary<int, string> {
			{ Created, "FileService.FilesCreated" },
			{ Changed, "FileService.FilesChanged" },
			{ Removed, "FileService.FilesRemoved" },
			{ Renamed, "FileService.FilesRenamed" },
		};

		void StressFileWatchers (string stressDirectory, int fileCount)
		{
			// Initial stabilization might take a bit.
			foreach (var counter in trackedCounters) {
				Session.WaitForCounterToStabilize (counter.Value, 40 * 1000, 1000);
			}

			Directory.CreateDirectory (stressDirectory);

			// Create file.txt
			Run (file => File.WriteAllText (file, file), trackedCounters [Created]);

			// Modify file.txt
			Run (file => File.SetLastWriteTimeUtc (file, DateTime.UtcNow), trackedCounters [Changed]);

			// Rename file.txt -> file.txt2
			Run (file => FileService.SystemRename (file, file + "2"), trackedCounters [Renamed]);

			// Delete file.txt2
			Run (file => File.Delete (file + "2"), trackedCounters [Removed]);

			// Delete the directory.
			Directory.Delete (stressDirectory, true);

			void Run (Action<string> action, string counterId)
			{
				var initial = Session.WaitForCounterToStabilize (counterId, 10 * 1000, 1000);

				for (int i = 0; i < fileCount; ++i) {
					var file = Path.Combine (stressDirectory, i.ToString () + ".txt");
					action (file);
				}

				Session.WaitForCounterToExceed (counterId, initial + fileCount, 2 * 60 * 1000);
			}
		}

		TimeSpan GetDuration (string counter, int enumValue)
		{
			Array values = typeof (FileService).GetNestedType ("EventDataKind", System.Reflection.BindingFlags.NonPublic).GetEnumValues ();
			return Session.GlobalInvoke<TimeSpan> (counter, values.GetValue (enumValue));
		}
	}
}
