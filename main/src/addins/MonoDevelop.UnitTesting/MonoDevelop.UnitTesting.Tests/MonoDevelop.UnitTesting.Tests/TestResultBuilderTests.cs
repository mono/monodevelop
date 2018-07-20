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
			context = new TestContext (monitor, executionContext, DateTime.Now);

			builder = new TestResultBuilder (context, testProvider);
		}

		VsTestUnitTest CreateVsUnitTest (string fullyQualifiedName)
		{
			var testCase = CreateTestCase (fullyQualifiedName);
			return new VsTestUnitTest (null, testCase, null);
		}

		TestCase CreateTestCase (string fullyQualifiedName)
		{
			return new TestCase {
				DisplayName = fullyQualifiedName,
				FullyQualifiedName = fullyQualifiedName,
				CodeFilePath = "test.cs"
			};
		}

		TestRunChangedEventArgs CreateTestRunChangedEventArgsWithTestResults (params TestResult[] newTestResults)
		{
			return new TestRunChangedEventArgs (null, newTestResults, null);
		}

		VsTestNamespaceTestGroup CreateVsUnitTestNamespace (string name)
		{
			return new VsTestNamespaceTestGroup (null, null, null, name);
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

		[Test]
		public void AddTests_TestClassUsesRootNamespace ()
		{
			var testNamespace = CreateVsUnitTestNamespace ("RootNamespace");
			var testCase = CreateTestCase ("RootNamespace.MyClass.MyTest");

			testNamespace.AddTests (new [] { testCase });

			Assert.AreEqual ("RootNamespace", testNamespace.FixtureTypeNamespace);
			Assert.AreEqual ("RootNamespace", testNamespace.Name);
			Assert.AreEqual ("RootNamespace", testNamespace.FullName);
			Assert.IsTrue (testNamespace.HasTests);
			Assert.AreEqual (1, testNamespace.Tests.Count);

			var testClass = testNamespace.Tests [0] as VsTestTestClass;
			Assert.AreEqual ("MyClass", testClass.Name);
			Assert.AreEqual ("RootNamespace.MyClass", testClass.FullName);
			Assert.IsTrue (testClass.HasTests);
			Assert.AreEqual (1, testClass.Tests.Count);

			var test = testClass.Tests [0] as VsTestUnitTest;
			Assert.AreEqual ("MyTest", test.Name);
			Assert.AreEqual ("RootNamespace", test.FixtureTypeNamespace);
			Assert.AreEqual ("RootNamespace.MyClass.MyTest", test.FullName);
		}

		[Test]
		public void OnTestRunChanged_SingleTest_RunTestsFromNamespace_TestPasses ()
		{
			var testNamespace = CreateVsUnitTestNamespace ("RootNamespace");
			var testCase = CreateTestCase ("RootNamespace.MyClass.MyTest");
			testNamespace.AddTests (new [] { testCase });
			CreateTestResultBuilder (testNamespace);
			var testClass = testNamespace.Tests [0] as VsTestTestClass;
			var test = testClass.Tests [0] as VsTestUnitTest;
			testClass.Status = TestStatus.Running;
			test.Status = TestStatus.Running;
			var testResult = new TestResult (testCase) {
				Outcome = TestOutcome.Passed
			};
			var eventArgs = CreateTestRunChangedEventArgsWithTestResults (testResult);

			builder.OnTestRunChanged (eventArgs);

			var result = test.GetLastResult ();
			Assert.AreEqual (ResultStatus.Success, result.Status);
			result = testClass.GetLastResult ();
			Assert.AreEqual (ResultStatus.Success, result.Status);
			Assert.AreEqual (TestStatus.Ready, testClass.Status);
			Assert.AreEqual (TestStatus.Ready, test.Status);
		}

		[Test]
		public void OnTestRunChanged_TwoTests_RunOneTestInNamespace_TestPasses ()
		{
			var testNamespace = CreateVsUnitTestNamespace ("RootNamespace");
			var testCase1 = CreateTestCase ("RootNamespace.MyClass.MyTest1");
			testNamespace.AddTests (new [] { testCase1 });
			var testCase2 = CreateTestCase ("RootNamespace.MyClass.MyTest2");
			testNamespace.AddTests (new [] { testCase2 });
			var testClass = testNamespace.Tests [0] as VsTestTestClass;
			var test1 = testClass.Tests [0] as VsTestUnitTest;
			var test2 = testClass.Tests [1] as VsTestUnitTest;
			testClass.Status = TestStatus.Running;
			test1.Status = TestStatus.Running;
			test2.Status = TestStatus.Running;
			CreateTestResultBuilder (testNamespace);
			var testResult = new TestResult (testCase1) {
				Outcome = TestOutcome.Passed
			};
			var eventArgs = CreateTestRunChangedEventArgsWithTestResults (testResult);

			builder.OnTestRunChanged (eventArgs);

			var result = test1.GetLastResult ();
			Assert.AreEqual (ResultStatus.Success, result.Status);
			result = test2.GetLastResult ();
			Assert.IsNull (result);
			Assert.AreEqual (TestStatus.Running, testClass.Status);
			Assert.AreEqual (TestStatus.Ready, test1.Status);
			Assert.AreEqual (TestStatus.Running, test2.Status);
		}

		/// <summary>
		/// As above but a result is not obtained for the second test but its
		/// status is changed to Ready and then a test result is returned which
		/// will cause a null test result to be returned from the UnitTest's
		/// GetLastResult method. Not sure exactly how this happens in practice
		/// but all existing code that uses the GetLastResult method always checks
		/// for null which the TestResultBuilder was not doing.
		/// </summary>
		[Test]
		public void OnTestRunChanged_TwoTests_RunOneTestInNamespace_NullTestResult_TestPasses ()
		{
			var testNamespace = CreateVsUnitTestNamespace ("RootNamespace");
			var testCase1 = CreateTestCase ("RootNamespace.MyClass.MyTest1");
			testNamespace.AddTests (new [] { testCase1 });
			var testCase2 = CreateTestCase ("RootNamespace.MyClass.MyTest2");
			testNamespace.AddTests (new [] { testCase2 });
			var testClass = testNamespace.Tests [0] as VsTestTestClass;
			var test1 = testClass.Tests [0] as VsTestUnitTest;
			var test2 = testClass.Tests [1] as VsTestUnitTest;
			testClass.Status = TestStatus.Running;
			test1.Status = TestStatus.Running;
			test2.Status = TestStatus.Ready;
			CreateTestResultBuilder (testNamespace);
			var testResult = new TestResult (testCase1) {
				Outcome = TestOutcome.Passed
			};
			var eventArgs = CreateTestRunChangedEventArgsWithTestResults (testResult);

			builder.OnTestRunChanged (eventArgs);

			var result = test1.GetLastResult ();
			Assert.AreEqual (ResultStatus.Success, result.Status);
			result = test2.GetLastResult ();
			Assert.IsNull (result);
			Assert.AreEqual (TestStatus.Ready, test1.Status);
		}
	}
}
