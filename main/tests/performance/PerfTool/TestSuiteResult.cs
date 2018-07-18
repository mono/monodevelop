//
// TestResult.cs
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
using System.Xml;
using System.Linq;
using System.Globalization;
using System.Xml.Serialization;
using PerfTool.TestModel;
using System.IO;

namespace PerfTool
{
	public class TestSuiteResult
	{
		Dictionary<string,TestCase> resultsByTestId = new Dictionary<string, TestCase> ();
		TestResults results;

		public TestSuiteResult ()
		{
		}

		public bool HasErrors {
			get { return results.Errors + results.Failures > 0; }
		}

		public void Read (string file)
		{
			var serializer = new XmlSerializer (typeof (TestResults));
			using (var sr = new StreamReader (file)) {
				results = (TestResults) serializer.Deserialize (sr);
			}

			CollectResults (results.TestSuite);
		}

		private void CollectResults (TestSuite testSuite)
		{
			foreach (var testCase in testSuite.Results.TestCases) {
				resultsByTestId [testCase.Name] = testCase;
				var timeProp = testCase.Properties?.FirstOrDefault (p => p.Name == "Time");
				if (timeProp != null) {
					if (!string.IsNullOrEmpty (timeProp.Value) && double.TryParse (timeProp.Value, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double customTime))
						testCase.Time = customTime;
					testCase.Properties.Remove (timeProp);
				}
			}
			foreach (var ts in testSuite.Results.TestSuites)
				CollectResults (ts);
		}

		public void Write (string file)
		{
			using (var w = XmlWriter.Create (file, new XmlWriterSettings { Indent = true })) {
				var serializer = new XmlSerializer (typeof (TestResults));
				serializer.Serialize (w, results);
			}
		}

		public IEnumerable<TestCase> GetRegressions (TestSuiteResult baseline)
		{
			foreach (var testResult in resultsByTestId.Values) {
				if (baseline.resultsByTestId.TryGetValue (testResult.Name, out var baselineResult)) {
					if (IsRegression (baselineResult, testResult))
						yield return testResult;
				}
			}
		}

		public IEnumerable<TestCase> GetImprovements (TestSuiteResult baseline)
		{
			foreach (var testResult in resultsByTestId.Values) {
				if (baseline.resultsByTestId.TryGetValue (testResult.Name, out var baselineResult)) {
					if (IsImprovement (baselineResult, testResult))
						yield return testResult;
				}
			}
		}

		public bool UpgradeToBaseline (TestSuiteResult oldBaseline)
		{
			bool changed = false;
			foreach (var oldBaselineResult in oldBaseline.resultsByTestId.Values) {
				if (resultsByTestId.TryGetValue (oldBaselineResult.Name, out var currentResult) && !IsImprovement (oldBaselineResult, currentResult)) {
					currentResult.Time = oldBaselineResult.Time;
					changed = true;
				}
			}
			return changed;
		}

		public List<TestCase> RegisterPerformanceRegressions (TestSuiteResult baseline, out List<TestCase> regressions, out List<TestCase> improvements)
		{
			regressions = new List<TestCase> ();
			improvements = new List<TestCase> ();
			foreach (var testResult in resultsByTestId.Values) {
				if (testResult.Success && baseline.resultsByTestId.TryGetValue (testResult.Name, out var baselineResult)) {
					if (IsRegression (baselineResult, testResult)) {
						testResult.Success = false;
						testResult.Result = "Error";
						testResult.Failure = new Failure {
							Message = $"Performance regression. Baseline: {baselineResult.Time}, Result: {testResult.Time} (+{(testResult.Time/baselineResult.Time) - 1:0.00})"
						};
						regressions.Add (testResult);
						results.Errors++;
					} else if (IsImprovement (baselineResult, testResult)) {
						testResult.Improvement = new Improvement {
							Message = $"Performance improvement. Baseline: {baselineResult.Time}, Result: {testResult.Time}",
							OldTime = baselineResult.Time,
							Time = testResult.Time
						};
						improvements.Add (testResult);
					}
				}
			}
			return regressions;
		}

		public bool IsRegression (TestCase baselineTestCase, TestCase testCase)
		{
			return testCase.Time - baselineTestCase.Time > GetThreshold (testCase);
		}

		public bool IsImprovement (TestCase baselineTestCase, TestCase testCase)
		{
			return baselineTestCase.Time - testCase.Time > GetThreshold (testCase);
		}

		public double GetThreshold (TestCase testCase)
		{
			double tolerance = 0.1;
			var toleranceProp = testCase.Properties?.FirstOrDefault (p => p.Name == "Tolerance");
			if (toleranceProp != null && double.TryParse (toleranceProp.Value, NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double customTolerance))
				tolerance = customTolerance;
			
			return testCase.Time * tolerance;
		}

		public TestCase ResultByTestId (string id)
		{
			if (resultsByTestId.TryGetValue (id, out TestCase result)) {
				return result;
			}

			return null;
		}
	}
}
