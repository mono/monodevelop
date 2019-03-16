//
// StressTestApp.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using MonoDevelop.Core;
using UserInterfaceTests;

namespace MonoDevelop.StressTest
{
	public class StressTestApp
	{
		List<string> FoldersToClean = new List<string> ();
		ITestScenario scenario;
		ProfilerOptions ProfilerOptions;

		LeakProcessor leakProcessor;

		public StressTestApp (StressTestOptions options)
		{
			Iterations = options.Iterations;
			ProfilerOptions = options.Profiler;
			if (!string.IsNullOrEmpty (options.MonoDevelopBinPath)) {
				MonoDevelopBinPath = options.MonoDevelopBinPath;
			}

			if (options.UseInstalledApplication) {
				MonoDevelopBinPath = GetInstalledVisualStudioBinPath ();
			}

			provider = options.Provider;
		}

		public string MonoDevelopBinPath { get; set; }
		public int Iterations { get; set; } = 1;
		readonly ITestScenarioProvider provider;
		ProfilerProcessor profilerProcessor;

		const int setupIteration = -1;
		const int cleanupIteration = int.MinValue;

		public void Start ()
		{
			ValidateMonoDevelopBinPath ();
			var logFile = SetupIdeLogFolder ();

			string profilePath = Util.CreateTmpDir ();

			FoldersToClean.Add (profilePath);

			scenario = provider.GetTestScenario ();

			if (!StartWithProfiler (profilePath, logFile))
				TestService.StartSession (MonoDevelopBinPath, profilePath, logFile);

			TestService.Session.DebugObject = new UITestDebug ();

			TestService.Session.WaitForElement (IdeQuery.DefaultWorkbench);

			leakProcessor = new LeakProcessor (scenario, ProfilerOptions);

			ReportMemoryUsage (setupIteration);
			for (int i = 0; i < Iterations; ++i) {
				scenario.Run ();
				ReportMemoryUsage (i);
			}

			UserInterfaceTests.Ide.CloseAll (exit: false);
			ReportMemoryUsage (cleanupIteration);
		}

		bool StartWithProfiler (string profilePath, string logFile)
		{
			if (ProfilerOptions.Type == ProfilerOptions.ProfilerType.Disabled)
				return false;

			if (ProfilerOptions.MlpdOutputPath == null)
				ProfilerOptions.MlpdOutputPath = Path.Combine (profilePath, "profiler.mlpd");
			if (File.Exists (ProfilerOptions.MlpdOutputPath))
				File.Delete (ProfilerOptions.MlpdOutputPath);
			profilerProcessor = new ProfilerProcessor (scenario, ProfilerOptions);
			string monoPath = Environment.GetEnvironmentVariable ("PATH")
										 .Split (Path.PathSeparator)
										 .Select (p => Path.Combine (p, "mono"))
										 .FirstOrDefault (s => File.Exists (s));

			TestService.StartSession (monoPath, profilePath, logFile, $"{profilerProcessor.GetMonoArguments ()} \"{MonoDevelopBinPath}\"");
			Console.WriteLine ($"Profler is logging into {ProfilerOptions.MlpdOutputPath}");
			return true;
		}

		public void Stop ()
		{
			UserInterfaceTests.Ide.CloseAll ();
			TestService.EndSession ();
			OnCleanUp ();

			leakProcessor.ReportResult ();
		}

		void ValidateMonoDevelopBinPath ()
		{
			if (string.IsNullOrEmpty (MonoDevelopBinPath)) {
				MonoDevelopBinPath = GetDefaultMonoDevelopBinPath ();
			}

			MonoDevelopBinPath = Path.GetFullPath (MonoDevelopBinPath);

			if (!File.Exists (MonoDevelopBinPath)) {
				throw new UserException (string.Format ("MonoDevelop binary not found: '{0}'", MonoDevelopBinPath));
			}
		}

		public static string GetMonoDevelopMainPath ()
		{
			return Path.GetFullPath (Path.Combine ("..", "..", "..", ".."));
		}

		string GetDefaultMonoDevelopBinPath ()
		{
			return Path.Combine (GetMonoDevelopMainPath (), "build", "bin", "MonoDevelop.exe");
		}

		string GetInstalledVisualStudioBinPath ()
		{
			return "/Applications/Visual Studio.app/Contents/Resources/lib/monodevelop/bin/VisualStudio.exe";
		}

		string SetupIdeLogFolder ()
		{
			string rootDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);
			string ideLogDirectory = Path.Combine (rootDirectory, "Log");
			Directory.CreateDirectory (ideLogDirectory);

			string ideLogFileName = Path.Combine (ideLogDirectory, "StressTest.log");
			return ideLogFileName;
		}

		void OnCleanUp ()
		{
			profilerProcessor?.Stop ();
			foreach (string folder in FoldersToClean) {
				try {
					if (folder != null && Directory.Exists (folder)) {
						Directory.Delete (folder, true);
					}
				} catch (IOException ex) {
					TestService.Session.DebugObject.Debug ("Cleanup failed\n" + ex);
				} catch (UnauthorizedAccessException ex) {
					TestService.Session.DebugObject.Debug (string.Format ("Unable to clean directory: {0}\n", folder) + ex);
				}
			}
		}

		void ReportMemoryUsage (int iteration)
		{
			//Make sure IDE stops doing what it was doing
			UserInterfaceTests.Ide.WaitForIdeIdle ();

			// This is to prevent leaking of AppQuery instances.
			TestService.Session.DisconnectQueries ();
			Heapshot heapshot = null;
			if (profilerProcessor != null) {
				heapshot = profilerProcessor.TakeHeapshotAndMakeReport ().Result;
			}

			var memoryStats = TestService.Session.MemoryStats;

			string iterationName;
			if (iteration == cleanupIteration) {
				iterationName = "Cleanup";
			} else if (iteration == setupIteration) {
				iterationName = "Setup";
			} else {
				iterationName = string.Format ("Run_{0}", iteration + 1);
			}

			Console.WriteLine (iterationName);

			Console.WriteLine ("  NonPagedSystemMemory: " + memoryStats.NonPagedSystemMemory);
			Console.WriteLine ("  PagedMemory: " + memoryStats.PagedMemory);
			Console.WriteLine ("  PagedSystemMemory: " + memoryStats.PagedSystemMemory);
			Console.WriteLine ("  PeakVirtualMemory: " + memoryStats.PeakVirtualMemory);
			Console.WriteLine ("  PrivateMemory: " + memoryStats.PrivateMemory);
			Console.WriteLine ("  VirtualMemory: " + memoryStats.VirtualMemory);
			Console.WriteLine ("  WorkingSet: " + memoryStats.WorkingSet);

			Console.WriteLine ();

			leakProcessor.Process (heapshot, iteration == cleanupIteration, iterationName, memoryStats);
		}
	}
}
