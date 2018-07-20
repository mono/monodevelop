//
// UnitTestGroupTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Linq;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.UnitTesting.Tests
{
	[TestFixture]
	public class UnitTestGroupTests
	{
		[Test]
		public void TestStatusUpdateCount ()
		{
			// root - Failure
			//      + grp1 - Failure
			//             + a - Failure
			//             + b - Success
			//      + grp2 - Success
			//             + c - Success
			MockTestGroup testGroup = new MockTestGroup (nameof(testGroup), 1, 1);

			MockTestGroup testSubGroup1 = new MockTestGroup (nameof (testSubGroup1), 1, 1);
			testGroup.Tests.Add (testSubGroup1);

			MockTest test1 = new MockTest (nameof (test1), ResultStatus.Failure);
			testSubGroup1.Tests.Add (test1);

			MockTest test2 = new MockTest (nameof (test2), ResultStatus.Success);
			testSubGroup1.Tests.Add (test2);

			MockTestGroup testSubGroup2 = new MockTestGroup (nameof(testSubGroup2), 1, 0);
			testGroup.Tests.Add (testSubGroup2);

			MockTest test3 = new MockTest (nameof (test3), ResultStatus.Success);
			testSubGroup2.Tests.Add (test3);

			Assert.AreEqual (0, test3.ReadyUpdateCount);
			Assert.AreEqual (0, test2.ReadyUpdateCount);
			Assert.AreEqual (0, test1.ReadyUpdateCount);
			Assert.AreEqual (0, testSubGroup2.ReadyUpdateCount);
			Assert.AreEqual (0, testSubGroup1.ReadyUpdateCount);
			Assert.AreEqual (0, testGroup.ReadyUpdateCount);

			var testContext = GetRunContext ();
			testGroup.Run (testContext);

			// One update for Running -> Ready
			// One update for setting the actual result.

			Assert.AreEqual (2, test3.ReadyUpdateCount);
			Assert.AreEqual (2, test2.ReadyUpdateCount);
			Assert.AreEqual (2, test1.ReadyUpdateCount);
			Assert.AreEqual (2, testSubGroup2.ReadyUpdateCount);
			Assert.AreEqual (2, testSubGroup1.ReadyUpdateCount);
			Assert.AreEqual (2, testGroup.ReadyUpdateCount);

			// Assert we got all the values.
			foreach (var test in GetAllTests (testGroup)) {
				var actual = test.GetLastResult ();

				if (test is MockTest mock) {
					Assert.AreEqual (mock.Result, actual.Status);
				} else if (test is MockTestGroup mockGroup) {
					Assert.AreEqual (mockGroup.PassCount, actual.Passed);
					Assert.AreEqual (mockGroup.FailureCount, actual.Failures);
				}
			}

			test2.Run (testContext);

			Assert.AreEqual (2, test3.ReadyUpdateCount);
			Assert.AreEqual (3, test2.ReadyUpdateCount);
			Assert.AreEqual (2, test1.ReadyUpdateCount);
			Assert.AreEqual (2, testSubGroup2.ReadyUpdateCount);
			Assert.AreEqual (4, testSubGroup1.ReadyUpdateCount);
			Assert.AreEqual (4, testGroup.ReadyUpdateCount);
		}

		IEnumerable<UnitTest> GetAllTests (UnitTestGroup group)
		{
			foreach (var test in group.Tests) {
				yield return test;

				if (test is UnitTestGroup grp) {
					foreach (var subTest in GetAllTests (grp))
						yield return subTest;
				}
			}
		}

		static TestContext GetRunContext ()
		{
			var monitor = new TestProgressMonitor ();
			var executionContext = new ExecutionContext ((IExecutionHandler)null, null, null);
			var testContext = new TestContext (monitor, executionContext, DateTime.Now);

			return testContext;
		}

		class MockTestGroup : UnitTestGroup
		{
			public int ReadyUpdateCount { get; private set; }
			public int PassCount { get; }
			public int FailureCount { get; }

			public MockTestGroup (string name, int passCount, int failureCount) : base (name)
			{
				PassCount = passCount;
				FailureCount = failureCount;
				ResultsStore = MockResultStore.Instance;
			}

			protected override void OnTestStatusChanged ()
			{
				if (Status == TestStatus.Ready)
					ReadyUpdateCount++;
				base.OnTestStatusChanged ();
			}
		}

		class MockTest : UnitTest
		{
			public int ReadyUpdateCount { get; private set; }
			public ResultStatus Result { get; }

			public MockTest (string name, ResultStatus result) : base (name)
			{
				Result = result;
				ResultsStore = MockResultStore.Instance;
			}

			protected override void OnTestStatusChanged ()
			{
				if (Status == TestStatus.Ready)
					ReadyUpdateCount++;
				base.OnTestStatusChanged ();
			}

			protected override UnitTestResult OnRun (TestContext testContext)
			{
				return new UnitTestResult {
					Passed = Result == ResultStatus.Success ? 1 : 0,
					Failures = Result == ResultStatus.Failure ? 1 : 0,
					Status = Result,
				};
			}
		}

		class MockResultStore : IResultsStore
		{
			public static MockResultStore Instance { get; } = new MockResultStore ();

			Dictionary<UnitTest, UnitTestResult> cache = new Dictionary<UnitTest, UnitTestResult> ();

			public UnitTestResult GetLastResult (string configuration, UnitTest test, DateTime date)
				=> cache.TryGetValue (test, out var result) ? result : null;

			public UnitTestResult GetNextResult (string configuration, UnitTest test, DateTime date)
				=> cache.TryGetValue (test, out var result) ? result : null;

			public UnitTestResult GetPreviousResult (string configuration, UnitTest test, DateTime date)
				=> cache.TryGetValue (test, out var result) ? result : null;

			public UnitTestResult [] GetResults (string configuration, UnitTest test, DateTime startDate, DateTime endDate)
				=> cache.Select (x => x.Value).ToArray ();

			public UnitTestResult [] GetResultsToDate (string configuration, UnitTest test, DateTime endDate, int count)
				=> cache.Select (x => x.Value).ToArray ();

			public void RegisterResult (string configuration, UnitTest test, UnitTestResult result)
				=> cache.Add (test, result);

			public void Save () { }
		}
	}
}
