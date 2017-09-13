//
// TestResultBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using MonoDevelop.UnitTesting;
using System.Threading.Tasks;

namespace MonoDevelop.UnitTesting.VsTest
{
	class TestResultBuilder
	{
		TestContext testContext;
		IVsTestTestProvider rootTest;
		bool runningSingleTest;

		public TaskCompletionSource<UnitTestResult> TaskSource = new TaskCompletionSource<UnitTestResult> ();

		public TestResultBuilder (TestContext testContext, IVsTestTestProvider rootTest)
		{
			this.testContext = testContext;
			this.rootTest = rootTest;
			runningSingleTest = rootTest is VsTestUnitTest;
			TestResult = UnitTestResult.CreateSuccess ();
		}

		public void CreateFailure (Exception ex)
		{
			TestResult = UnitTestResult.CreateFailure (ex);
			TaskSource.SetResult (TestResult);
		}

		public UnitTestResult TestResult { get; private set; }

		public void OnTestRunChanged (TestRunChangedEventArgs eventArgs)
		{
			if (eventArgs.ActiveTests != null) {
				foreach (TestCase test in eventArgs.ActiveTests) {
					OnActiveTest (test);
				}
			}

			if (eventArgs.NewTestResults != null) {
				foreach (TestResult result in eventArgs.NewTestResults) {
					OnTestResult (result);
				}
			}
		}

		public void OnTestRunComplete (TestRunCompletePayload testRunComplete)
		{
			if (testRunComplete.TestRunCompleteArgs.IsAborted || testRunComplete.TestRunCompleteArgs.IsCanceled)
				return;

			if (testRunComplete.LastRunTests != null) {
				OnTestRunChanged (testRunComplete.LastRunTests);
			}
			TaskSource.SetResult (TestResult);
		}

		void OnTestResult (TestResult result)
		{
			UnitTestResult convertedResult = AddTestResult (result);

			if (runningSingleTest) {
				// Ensure test error message is displayed in Test Results window.
				TestResult = convertedResult;
				return;
			}

			string testId = result.TestCase.Id.ToString ();
			UnitTest currentTest = FindTest (rootTest as UnitTest, testId);

			if (currentTest != null) {
				currentTest.RegisterResult (testContext, convertedResult);
				testContext.Monitor.EndTest (currentTest, convertedResult);
				currentTest.Status = TestStatus.Ready;
				UpdateParentStatus (currentTest);
			}
		}

		UnitTestResult AddTestResult (TestResult result)
		{
			var convertedTestResult = new UnitTestResult {
				ConsoleError = result.ErrorMessage,
				ConsoleOutput = GetConsoleOutput (result),
				Message = result.ErrorMessage,
				Status = ToResultStatus (result.Outcome),
				StackTrace = result.ErrorStackTrace,
				TestDate = result.StartTime.DateTime,
				Time = result.Duration
			};

			UpdateCounts (convertedTestResult);

			TestResult.Add (convertedTestResult);

			return convertedTestResult;
		}

		void UpdateCounts (UnitTestResult result)
		{
			UpdateCounts (result, result);
		}

		void UpdateCounts (UnitTestResult parentResult, UnitTestResult result)
		{
			switch (result.Status) {
				case ResultStatus.Failure:
				parentResult.Failures++;
				break;

				case ResultStatus.Ignored:
				parentResult.Ignored++;
				break;

				case ResultStatus.Success:
				parentResult.Passed++;
				break;

				case ResultStatus.Inconclusive:
				parentResult.Inconclusive++;
				break;
			}
		}

		static ResultStatus ToResultStatus (TestOutcome outcome)
		{
			switch (outcome) {
				case TestOutcome.Passed:
				return ResultStatus.Success;

				case TestOutcome.Failed:
				case TestOutcome.NotFound:
				return ResultStatus.Failure;

				case TestOutcome.None:
				return ResultStatus.Inconclusive;

				case TestOutcome.Skipped:
				return ResultStatus.Ignored;

				default:
				return ResultStatus.Inconclusive;
			}
		}

		string GetConsoleOutput (TestResult result)
		{
			if (result.Messages != null) {
				return string.Join (Environment.NewLine, result.Messages);
			}

			return string.Empty;
		}

		UnitTest FindTest (UnitTest test, string testId)
		{
			var testGroup = test as UnitTestGroup;
			if (testGroup == null) {
				if (test.TestId == testId) {
					return test;
				}
				return null;
			}

			foreach (UnitTest child in testGroup.Tests) {
				if (child.TestId == testId) {
					return child;
				}

				UnitTest foundTest = FindTest (child, testId);
				if (foundTest != null) {
					return foundTest;
				}
			}

			return null;
		}

		void UpdateParentStatus (UnitTest currentTest)
		{
			var parent = currentTest.Parent as UnitTestGroup;

			while (parent != null && parent != rootTest && !(parent is VsTestProjectTestSuite)) {
				if (currentTest.Status == TestStatus.Running) {
					OnChildTestRunning (parent);
				} else if (currentTest.Status == TestStatus.Ready) {
					OnChildTestReady (parent);
				}

				parent = parent.Parent as UnitTestGroup;
			} 
		}

		void OnChildTestRunning (UnitTestGroup parent)
		{
			if (parent.Status != TestStatus.Running) {
				testContext.Monitor.BeginTest (parent);
				parent.Status = TestStatus.Running;
			}
		}

		void OnChildTestReady (UnitTestGroup parent)
		{
			if (parent.Tests.All (TestHasBeenRun)) {
				UnitTestResult result = GenerateResultFromChildTests (parent);
				parent.RegisterResult (testContext, result);
				testContext.Monitor.EndTest (parent, result);
				parent.Status = TestStatus.Ready;
			}
		}

		bool TestHasBeenRun (UnitTest test)
		{
			return test.Status == TestStatus.Ready && !test.IsHistoricResult;
		}

		UnitTestResult GenerateResultFromChildTests (UnitTestGroup parent)
		{
			var result = UnitTestResult.CreateSuccess ();
			foreach (UnitTest test in parent.Tests) {
				UnitTestResult childResult = test.GetLastResult ();
				result.Add (childResult);
				UpdateCounts (result, childResult);
			}
			return result;
		}

		void OnActiveTest (TestCase test)
		{
			if (runningSingleTest)
				return;

			string testId = test.Id.ToString ();

			UnitTest currentTest = FindTest (rootTest as UnitTest, testId);
			if (currentTest != null) {
				testContext.Monitor.BeginTest (currentTest);
				currentTest.Status = TestStatus.Running;
				UpdateParentStatus (currentTest);
			}
		}
	}
}
