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

		public StressTestOptions ()
		{
		}

		public void Parse (string[] args)
		{
			foreach (string arg in args) {
				if (arg.StartsWith (IterationsArgument)) {
					ParseIterations (arg);
				} else if (arg.StartsWith (MonoDevelopBinPathArgument)) {
					ParseMonoDevelopBinPath (arg);
				} else if (arg == UseInstalledAppArgument) {
					UseInstalledApplication = true;
				} else {
					Help = true;
					break;
				}
			}
		}

		public bool Help { get; set; }
		public int Iterations { get; set; } = 1;
		public string MonoDevelopBinPath { get; set; }
		public bool UseInstalledApplication { get; set; }

		public void ShowHelp ()
		{
			Console.WriteLine ("Usage: StressTest [options]");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			Console.WriteLine ("  --iterations:number     Number of times the stress test will be run");
			Console.WriteLine ("  --mdbinpath:path        Path to MonoDevelop.exe or VisualStudio.exe");
			Console.WriteLine ("  --useinstalledapp       Use installed Visual Studio.app");
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
	}
}
