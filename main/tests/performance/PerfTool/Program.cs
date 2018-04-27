//
// Program.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

namespace PerfTool
{
	class MainClass
	{
		public static int Main (string [] args)
		{
			if (args.Length == 0)
				return PrintHelp ();

			var command = args [0];
			if (command == "generate-results" && args.Length == 4) {
				return GenerateResults (args [1], args [2], args [3]);
			} else
				return PrintHelp ();
		}

		static int GenerateResults (string baseFile, string inputFile, string resultsFile)
		{
			var baseTestSuite = new TestSuiteResult ();
			baseTestSuite.Read (baseFile);

			var inputTestSuite = new TestSuiteResult ();
			inputTestSuite.Read (inputFile);

			var regressions = inputTestSuite.RegisterPerformanceRegressions (baseTestSuite);
			inputTestSuite.Write (resultsFile);

			if (regressions.Count > 0) {
				Console.WriteLine ("Performance Regressions:");
				for (int n = 0; n < regressions.Count; n++) {
					var reg = regressions [n];
					var number = (n+1) + ") ";
					Console.WriteLine (number + reg.Name);
					Console.WriteLine (new string (' ', number.Length) + reg.Failure.Message);
				}
				Console.WriteLine ();
			}
			return inputTestSuite.HasErrors ? 1 : 0;
		}

		static int PrintHelp ()
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("generate-results <base-file> <input-file> <output-file>");
			Console.WriteLine ("    Detects regressions in input-file when compared to base-file.");
			Console.WriteLine ("    It generates an NUnit test results file with test failures.");
			return 0;
		}
	}
}
