//
// XUnitAssemblyTestSuite.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
using MonoDevelop.NUnit;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.IO;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Leaf node in the test tree. Receives events from executor
	/// and reports them to parent nodes using execution session.
	/// </summary>
	public class XUnitTestCase: UnitTest, IExecutableTest
	{
		XUnitAssemblyTestSuite rootSuite;
		XUnitTestExecutor executor;
		public XUnitTestInfo TestInfo { get; private set; }

		XUnitExecutionSession session;

		public XUnitTestCase (XUnitAssemblyTestSuite rootSuite, XUnitTestExecutor executor, XUnitTestInfo testInfo): base (testInfo.Name)
		{
			this.rootSuite = rootSuite;
			TestInfo = testInfo;
			this.executor = executor;
		}

		protected override bool OnCanRun (IExecutionHandler executionContext)
		{
			return rootSuite.CanRun (executionContext);
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return executor.RunTestCase (rootSuite, this, testContext);
		}

		public override SourceCodeLocation SourceCodeLocation {
			get {
				return rootSuite.GetSourceCodeLocation (this);
			}
		}

		public XUnitExecutionSession CreateExecutionSession (bool reportToMonitor)
		{
			session = new XUnitExecutionSession (this, reportToMonitor);
			return session;
		}

		public void OnStarting (TestContext context, string id)
		{
			session.Begin (context);
		}

		public void OnFinished (TestContext context, string id)
		{
			session.End ();
		}

		public void OnFailed (TestContext context, string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			UnitTestResult result = session.Result;
			VirtualTest test = null;

			int count = CountResults (session.Result);

			if (count > 0) {
				if (count < 2) {
					test = new VirtualTest (this, count);
					result = CopyResult (session.Result);

					session.Context.Monitor.BeginTest (test);
					session.Context.Monitor.EndTest (test, result);
					test.RegisterResult (session.Context, result);

					session.Result.Add (result);
					count++;
				}

				test = new VirtualTest (this, count);
				result = new UnitTestResult ();
			}

			result.Status = ResultStatus.Failure;
			result.Failures = 1;

			result.Time = TimeSpan.FromSeconds (Decimal.ToDouble (executionTime));
			result.ConsoleOutput = output;

			if (messages.Length > 0)
				result.Message = messages [0];

			if (stackTraces.Length > 0)
				result.StackTrace = stackTraces [0];

			if (test != null) {
				session.Context.Monitor.BeginTest (test);
				session.Context.Monitor.EndTest (test, result);
				test.RegisterResult (session.Context, result);

				session.Result.Add (result);
			}
		}

		public void OnPassed (TestContext context, string id, decimal executionTime, string output)
		{
			var result = session.Result;
			VirtualTest test = null;

			int count = CountResults (session.Result);

			if (count > 0) {
				if (count < 2) {
					test = new VirtualTest (this, count);
					result = CopyResult (session.Result);

					session.Context.Monitor.BeginTest (test);
					session.Context.Monitor.EndTest (test, result);
					test.RegisterResult (session.Context, result);

					session.Result.Add (result);
					count++;
				}

				test = new VirtualTest (this, count);
				result = new UnitTestResult ();
			}

			result.Status = ResultStatus.Success;
			result.Passed = 1;

			result.Time = TimeSpan.FromSeconds (Decimal.ToDouble (executionTime));
			result.ConsoleOutput = output;

			if (test != null) {
				session.Context.Monitor.BeginTest (test);
				session.Context.Monitor.EndTest (test, result);
				test.RegisterResult (session.Context, result);

				session.Result.Add (result);
			}
		}

		public void OnSkipped (TestContext context, string id, string reason)
		{
			var result = session.Result;
			VirtualTest test = null;

			int count = CountResults (session.Result);

			if (count > 0) {
				if (count < 2) {
					test = new VirtualTest (this, count);
					result = CopyResult (session.Result);

					session.Context.Monitor.BeginTest (test);
					session.Context.Monitor.EndTest (test, result);
					test.RegisterResult (session.Context, result);

					session.Result.Add (result);
					count++;
				}

				test = new VirtualTest (this, count);
				result = new UnitTestResult ();
			}

			result.Status = ResultStatus.Ignored;
			result.Ignored = 1;

			if (reason != null)
				result.Message = reason;

			if (test != null) {
				session.Context.Monitor.BeginTest (test);
				session.Context.Monitor.EndTest (test, result);
				test.RegisterResult (session.Context, result);

				session.Result.Add (result);
			}
		}

		int CountResults (UnitTestResult result)
		{
			int count = 0;
			count += result.Passed;
			count += result.Errors;
			count += result.Failures;
			count += result.Ignored;
			count += result.Inconclusive;
			count += result.Skipped;
			return count;
		}

		UnitTestResult CopyResult (UnitTestResult origin)
		{
			var result = new UnitTestResult ();

			result.Status = origin.Status;
			result.Time = origin.Time;
			result.ConsoleOutput = origin.ConsoleOutput;
			result.Message = origin.Message;
			result.StackTrace = origin.StackTrace;

			result.Passed = session.Result.Passed;
			result.Errors = session.Result.Errors;
			result.Failures = session.Result.Failures;
			result.Ignored = session.Result.Ignored;
			result.Inconclusive = session.Result.Inconclusive;
			result.Skipped = session.Result.Skipped;

			return result;
		}

		/// <summary>
		/// Test used only for results reporting. Does not appear in the test tree.
		/// Used when a test case executed multiple times.
		/// </summary>
		class VirtualTest : UnitTest
		{
			XUnitTestCase testCase;

			public VirtualTest (XUnitTestCase testCase, int i): base (string.Format("{0} ({1})", testCase.FullName, i))
			{
				this.testCase = testCase;
			}

			protected override UnitTestResult OnRun (TestContext testContext)
			{
				return null;
			}

			public override SourceCodeLocation SourceCodeLocation {
				get {
					return testCase.SourceCodeLocation;
				}
			}
		}
	}
}
