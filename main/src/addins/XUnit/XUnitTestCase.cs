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

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return executor.RunTestCase (rootSuite, this, testContext);
		}

		public XUnitExecutionSession CreateExecutionSession ()
		{
			session = new XUnitExecutionSession (this);
			return session;
		}

		public void OnStarting (TestContext context)
		{
			session.Begin (context);
		}

		public void OnFinished (TestContext context)
		{
			session.End (context);
		}

		public void OnFailed (TestContext context, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			var result = new UnitTestResult ();
			result.Status = ResultStatus.Failure;
			result.Time = TimeSpan.FromSeconds (Decimal.ToDouble (executionTime));
			result.Failures = 1;
			result.ConsoleOutput = output;
			if (messages.Length > 0)
				result.Message = messages [0];
			if (stackTraces.Length > 0)
				result.StackTrace = stackTraces [0];
			session.Result.Add (result);
		}

		public void OnPassed (TestContext context, decimal executionTime, string output)
		{
			var result = UnitTestResult.CreateSuccess ();
			result.Time = TimeSpan.FromSeconds (Decimal.ToDouble (executionTime));
			result.Passed = 1;
			result.ConsoleOutput = output;
			session.Result.Add (result);
		}

		public void OnSkipped (TestContext context, string reason)
		{
			var result = UnitTestResult.CreateIgnored (reason);
			result.Ignored = 1;
			session.Result.Add (result);
		}
	}
}
