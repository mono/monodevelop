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
using System.Collections.Generic;
using PerfTool.TestModel;

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
			} else if (command == "update-baseline") {
				if (args.Length == 5) {
					return UpdateBaseLine (args [1], args [2], args [3], args [4]);
				} else if (args.Length == 4) {
					return UpdateBaseLine (args [1], args [2], args[3], null);
				} else {
					return PrintHelp ();
				}
			} else
				return PrintHelp ();
		}

		static int GenerateResults (string baseFile, string inputFile, string resultsFile)
		{
			var baseTestSuite = new TestSuiteResult ();
			baseTestSuite.Read (baseFile);

			var inputTestSuite = new TestSuiteResult ();
			inputTestSuite.Read (inputFile);

			inputTestSuite.RegisterPerformanceRegressions (baseTestSuite, out List<TestCase> regressions, out List<TestCase> improvements);
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

			if (improvements.Count > 0) {
				Console.WriteLine ("Performance Improvements:");
				for (int n = 0; n < improvements.Count; n++) {
					var imp = improvements [n];
					var number = (n+1) + ") ";
					Console.WriteLine (number + imp.Name);
					Console.WriteLine (new string (' ', number.Length) + imp.Improvement.Message);
				}
				Console.WriteLine ();
			}

			return inputTestSuite.HasErrors ? 1 : 0;
		}

		static int UpdateBaseLine (string baseFile, string inputFile, string outputFile, string resultName)
		{
			var baseTestSuite = new TestSuiteResult ();
			baseTestSuite.Read (baseFile);

			var inputTestSuite = new TestSuiteResult ();
			inputTestSuite.Read (inputFile);

			inputTestSuite.RegisterPerformanceRegressions (baseTestSuite, out List<TestCase> regressions, out List<TestCase> improvements);

			List<Tuple <TestCase, TestCase>> resultsToUpdate = new List<Tuple<TestCase, TestCase>> ();;
			if (!string.IsNullOrEmpty (resultName)) {
				var updateResult = inputTestSuite.ResultByTestId (resultName);
				if (updateResult.Improvement == null) {
					return 0;
				}

				var result = baseTestSuite.ResultByTestId (resultName);
				if (result == null) {
					Console.WriteLine ($"Unknown result {resultName}");
					return 1;
				}

				resultsToUpdate.Add (new Tuple<TestCase, TestCase> (result, updateResult));
			} else {
				foreach (var updateResult in improvements) {
					var result = baseTestSuite.ResultByTestId (updateResult.Name);
					resultsToUpdate.Add (new Tuple<TestCase, TestCase> (result, updateResult));
				}
			}

			foreach (var r in resultsToUpdate) {
				var baseline = r.Item1;
				var update = r.Item2;

				baseline.Time = update.Time;
				baseline.Result = update.Result;
			}

			baseTestSuite.Write (outputFile);
			return 0;
		}

		static int PrintHelp ()
		{
			Console.WriteLine ("Usage:");
			Console.WriteLine ("generate-results <base-file> <input-file> <output-file>");
			Console.WriteLine ("    Detects regressions in input-file when compared to base-file.");
			Console.WriteLine ("    It generates an NUnit test results file with test failures.");
			Console.WriteLine ("update-baseline <base-file> <input-file> <output-file> [testcase]");
			Console.WriteLine ("    Updates the results in base-file that have improved in input-file");
			return 0;
		}
	}
}
