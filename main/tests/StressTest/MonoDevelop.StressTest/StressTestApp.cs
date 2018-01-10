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
		StressTestOptions.ProfilerOptions ProfilerOptions;

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
		}

		public string MonoDevelopBinPath { get; set; }
		public int Iterations { get; set; } = 1;
		ProfilerProcessor profilerProcessor;

		public void Start ()
		{
			ValidateMonoDevelopBinPath ();
			SetupIdeLogFolder ();

			string profilePath = Util.CreateTmpDir ();

			FoldersToClean.Add (profilePath);
			if (ProfilerOptions.Type != StressTestOptions.ProfilerOptions.ProfilerType.Disabled) {
				if (ProfilerOptions.MlpdOutputPath == null)
					ProfilerOptions.MlpdOutputPath = Path.Combine (profilePath, "profiler.mlpd");
				profilerProcessor = new ProfilerProcessor (ProfilerOptions);
				string monoPath = Environment.GetEnvironmentVariable ("PATH")
											 .Split (Path.PathSeparator)
											 .Select (p => Path.Combine (p, "mono"))
											 .FirstOrDefault (s => File.Exists (s));
				TestService.StartSession (monoPath, profilePath, $"{profilerProcessor.GetMonoArguments ()} \"{MonoDevelopBinPath}\"");
				Console.WriteLine ($"Profler is logging into {ProfilerOptions.MlpdOutputPath}");
			} else {
				TestService.StartSession (MonoDevelopBinPath, profilePath);
			}
			TestService.Session.DebugObject = new UITestDebug ();

			TestService.Session.WaitForElement (IdeQuery.DefaultWorkbench);

			scenario = TestScenarioProvider.GetTestScenario ();

			for (int i = 0; i < Iterations; ++i) {
				scenario.Run ();
				ReportMemoryUsage (i);
			}
		}

		public void Stop ()
		{
			UserInterfaceTests.Ide.CloseAll ();
			TestService.EndSession ();
			OnCleanUp ();
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
			return Path.GetFullPath (Path.Combine ("..", ".."));
		}

		string GetDefaultMonoDevelopBinPath ()
		{
			return Path.Combine (GetMonoDevelopMainPath (), "build", "bin", "MonoDevelop.exe");
		}

		string GetInstalledVisualStudioBinPath ()
		{
			return "/Applications/Visual Studio.app/Contents/Resources/lib/monodevelop/bin/VisualStudio.exe";
		}

		void SetupIdeLogFolder ()
		{
			string rootDirectory = Path.GetDirectoryName (GetType ().Assembly.Location);
			string ideLogDirectory = Path.Combine (rootDirectory, "Log");
			Directory.CreateDirectory (ideLogDirectory);

			FoldersToClean.Add (ideLogDirectory);

			string ideLogFileName = Path.Combine (ideLogDirectory, "StressTest.log");
			Environment.SetEnvironmentVariable ("MONODEVELOP_LOG_FILE", ideLogFileName);
			Environment.SetEnvironmentVariable ("MONODEVELOP_FILE_LOG_LEVEL", "UpToInfo");
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
			UserInterfaceTests.Ide.WaitForIdeIdle ();//Make sure IDE stops doing what it was doing
			if (profilerProcessor != null) {
				profilerProcessor.TakeHeapshotAndMakeReport ().Wait ();
			}

			var memoryStats = TestService.Session.MemoryStats;

			Console.WriteLine ("Run {0}", iteration + 1);

			Console.WriteLine ("  NonPagedSystemMemory: " + memoryStats.NonPagedSystemMemory);
			Console.WriteLine ("  PagedMemory: " + memoryStats.PagedMemory);
			Console.WriteLine ("  PagedSystemMemory: " + memoryStats.PagedSystemMemory);
			Console.WriteLine ("  PeakVirtualMemory: " + memoryStats.PeakVirtualMemory);
			Console.WriteLine ("  PrivateMemory: " + memoryStats.PrivateMemory);
			Console.WriteLine ("  VirtualMemory: " + memoryStats.VirtualMemory);
			Console.WriteLine ("  WorkingSet: " + memoryStats.WorkingSet);

			Console.WriteLine ();
		}
	}
}
