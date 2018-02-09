//
// StressTestOptions.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.StressTest
{
	public class StressTestOptions
	{
		const string IterationsArgument = "--iterations:";
		const string MonoDevelopBinPathArgument = "--mdbinpath:";
		const string UseInstalledAppArgument = "--useinstalledapp";
		const string ProfilerArgument = "--profiler:";

		public void Parse (string[] args)
		{
			foreach (string arg in args) {
				if (arg.StartsWith (IterationsArgument)) {
					ParseIterations (arg);
				} else if (arg.StartsWith (MonoDevelopBinPathArgument)) {
					ParseMonoDevelopBinPath (arg);
				} else if (arg.StartsWith (ProfilerArgument)) {
					ParseProfilerOptions (arg);
				} else if (arg == UseInstalledAppArgument) {
					UseInstalledApplication = true;
				} else {
					Help = true;
					break;
				}
			}
		}

		public class ProfilerOptions
		{
			public enum ProfilerType
			{
				Disabled,
				HeapOnly,
				All,
				Custom
			}
			[Flags]
			public enum PrintReport
			{
				ObjectsDiff = 1 << 0,
				ObjectsTotal = 1 << 1,
			}
			public ProfilerType Type { get; set; } = ProfilerType.Disabled;
			public PrintReport PrintReportTypes { get; set; }
			public int MaxFrames { get; set; }
			public string MlpdOutputPath { get; set; }
			public string CustomProfilerArguments { get; set; }
		}

		public bool Help { get; set; }
		public int Iterations { get; set; } = 1;
		public string MonoDevelopBinPath { get; set; }
		public bool UseInstalledApplication { get; set; }
		public ProfilerOptions Profiler { get; } = new ProfilerOptions ();

		public void ShowHelp ()
		{
			Console.WriteLine ("Usage: StressTest [options]");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			Console.WriteLine ("  --iterations:number     Number of times the stress test will be run");
			Console.WriteLine ("  --mdbinpath:path        Path to MonoDevelop.exe or VisualStudio.exe");
			Console.WriteLine ("  --useinstalledapp       Use installed Visual Studio.app");
			Console.WriteLine ("  --profiler:             Use profiler to make more detailed leak reporting");
		}

		void ParseIterations (string arg)
		{
			string numberString = arg.Substring (IterationsArgument.Length);

			if (int.TryParse (numberString, out int number)) {
				Iterations = number;
			} else {
				throw new UserException (string.Format ("Unable to parse iterations argument: '{0}'", arg));
			}
		}

		void ParseMonoDevelopBinPath (string arg)
		{
			MonoDevelopBinPath = arg.Substring (MonoDevelopBinPathArgument.Length);
		}

		void ParseProfilerOptions (string arg)
		{
			var options = arg.Substring (ProfilerArgument.Length).Split (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var o in options) {
				var nameValuePair = o.Split (new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				switch (nameValuePair[0]) {
					case "type":
						if (Profiler.Type != ProfilerOptions.ProfilerType.Disabled)
							throw new Exception ("--profiler:type= can be defined only once.");
						switch (nameValuePair[1]) {
							case "heaponly":
								Profiler.Type = ProfilerOptions.ProfilerType.HeapOnly;
								break;
							case "all":
								Profiler.Type = ProfilerOptions.ProfilerType.All;
								break;
							case "custom":
								Profiler.Type = ProfilerOptions.ProfilerType.Custom;
								break;
							default:
								PrintProfilerHelpAndExit ($"Unknown --profiler=type:{nameValuePair[1]}");
								break;
						}
						break;
					case "printreport":
						switch (nameValuePair[1]) {
							case "objectsdiff":
								Profiler.PrintReportTypes |= ProfilerOptions.PrintReport.ObjectsDiff;
								break;
							case "objectstotal":
								Profiler.PrintReportTypes |= ProfilerOptions.PrintReport.ObjectsTotal;
								break;
							default:
								PrintProfilerHelpAndExit ($"Unknown --profiler=printreport:{nameValuePair[1]}");
								break;
						}
						break;
					case "output":
						Profiler.MlpdOutputPath = nameValuePair[1];
						break;
					case "maxframes":
						Profiler.MaxFrames = int.Parse (nameValuePair[1]);
						break;
					case "custom":
						Profiler.CustomProfilerArguments = nameValuePair[1];
						break;
					default:
						PrintProfilerHelpAndExit ($"Unknown --profiler= option:{nameValuePair[0]}");
						break;
				}
			}
			if (Profiler.Type == ProfilerOptions.ProfilerType.Disabled)
				Profiler.Type = ProfilerOptions.ProfilerType.HeapOnly;//Default value
		}

		void PrintProfilerHelpAndExit (string reason = null)
		{
			if (!string.IsNullOrEmpty (reason))
				Console.WriteLine (reason);
			Console.WriteLine ("Usage: --profiler=[<option>=<value>,...]");
			Console.WriteLine ("Available options:");
			Console.WriteLine ("Option type");
			Console.WriteLine ("  type=<type>             Defines how detailed logging is. Possible options:");
			Console.WriteLine ("                            'heaponly' is default value, and logs minimal needed to get number of objects after each iteration");
			Console.WriteLine ("                            'all' tracks also allocations which can later be used in UI profiler to further analyse leak reasons");
			Console.WriteLine ("                            'custom' is for advanced usages where user supplies mono profiler parameters to fine tune logging");
			Console.WriteLine ("  printreport=<name>      Defines what to print between interations. Can be used multiple times. Possible options:");
			Console.WriteLine ("                            'objectsdiff' prints object count difference between iterations for each class");
			Console.WriteLine ("                            'objectstotal' prints object count difference between iterations for each class");
			Console.WriteLine ("  output=<output>         Output .mlpd file path");
			Console.WriteLine ("  maxframes=<maxframes>   How many stackframes should be logged on each allocation, used only with 'all'");
			Console.WriteLine ("  custom=<custom>         This value will be passed directly to mono --profiler=<custom>, used only with 'custom'");
			Console.WriteLine ("Usage example: --profiler:type=all,maxframes=15,output=/tmp/profile.mlpd");
			System.Environment.Exit (33);
		}
	}
}
