//
// TestResultBuilderTests.cs
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting.VsTest;
using NUnit.Framework;

namespace MonoDevelop.UnitTesting.Tests
{
	[TestFixture]
	public class TestResultBuilderTests
	{
		TestResultBuilder builder;
		TestContext context;
		TestProgressMonitor monitor;

		void CreateTestResultBuilder (IVsTestTestProvider testProvider)
		{
			var executionContext = new ExecutionContext ((IExecutionHandler)null, null, null);
			monitor = new TestProgressMonitor ();
			context = new TestContext (monitor, null, executionContext, DateTime.Now);

			builder = new TestResultBuilder (context, testProvider);
		}

		VsTestUnitTest CreateVsUnitTest (string fullyQualifedName)
		{
			var testCase = new TestCase {
				DisplayName = fullyQualifedName,
				FullyQualifiedName = fullyQualifedName,
				CodeFilePath = "test.cs"
			};
			return new VsTestUnitTest (null, testCase, null);
		}

		TestRunChangedEventArgs CreateTestRunChangedEventArgsWithTestResults (params TestResult[] newTestResults)
		{
			return new TestRunChangedEventArgs (null, newTestResults, null);
		}

		[Test]
		public void ConsoleOutputText_SingleTestUsingTestOutputHelperWriteLine_ConsoleOutputInTestResultOutput ()
		{
			var unitTest = CreateVsUnitTest ("MyNamespace.MyTest");
			CreateTestResultBuilder (unitTest);
			var testCase = new TestCase ();
			var testResult = new TestResult (testCase);
			testResult.Messages.Add (new TestResultMessage ("Category", "Line 1"));
			testResult.Messages.Add (new TestResultMessage ("Category", "Line 2"));
			var eventArgs = CreateTestRunChangedEventArgsWithTestResults (testResult);

			builder.OnTestRunChanged (eventArgs);

			string expectedConsoleOutputMessage = "Line 1" + Environment.NewLine + "Line 2";
			Assert.AreEqual (expectedConsoleOutputMessage, builder.TestResult.ConsoleOutput);
		}

		[Test]
		public void SingleTest_Failure_TestResultIndicatesFailure ()
		{
			var unitTest = CreateVsUnitTest ("MyNamespace.MyTest");
			CreateTestResultBuilder (unitTest);
			var testCase = new TestCase ();
			var testResult = new TestResult (testCase) {
				ErrorMessage = "Error Message",
				ErrorStackTrace = "Error Stack Trace",
				Outcome = TestOutcome.Failed,
				StartTime = new DateTimeOffset (new DateTime (2000, 1, 2, 12, 58, 30)),
				Duration = new TimeSpan (0, 1, 30)
			};
			var eventArgs = CreateTestRunChangedEventArgsWithTestResults (testResult);

			builder.OnTestRunChanged (eventArgs);

			Assert.AreEqual ("Error Message", builder.TestResult.ConsoleError);
			Assert.AreEqual ("Error Message", builder.TestResult.Message);
			Assert.AreEqual (ResultStatus.Failure, builder.TestResult.Status);
			Assert.AreEqual ("Error Stack Trace", builder.TestResult.StackTrace);
			Assert.AreEqual (testResult.StartTime.DateTime, builder.TestResult.TestDate);
			Assert.AreEqual (testResult.Duration, builder.TestResult.Time);
			Assert.AreEqual (1, builder.TestResult.Failures);
			Assert.AreEqual (0, builder.TestResult.Ignored);
			Assert.AreEqual (0, builder.TestResult.Passed);
			Assert.AreEqual (0, builder.TestResult.Inconclusive);
		}
	}
}
