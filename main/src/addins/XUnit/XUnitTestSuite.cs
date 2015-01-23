//
// XUnitTestSuite.cs
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
using MonoDevelop.Core.Execution;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Non-leaf node in the test tree. When executed listens for it's children
	/// events using execution session.
	/// </summary>
	public class XUnitTestSuite: UnitTestGroup, IExecutableTest
	{
		XUnitAssemblyTestSuite rootSuite;
		XUnitTestExecutor executor;
		public XUnitTestInfo TestInfo { get; private set; }

		XUnitExecutionSession session;

		public XUnitTestSuite (XUnitAssemblyTestSuite rootSuite, XUnitTestExecutor executor, XUnitTestInfo testInfo): base (testInfo.Name)
		{
			this.rootSuite = rootSuite;
			TestInfo = testInfo;
			this.executor = executor;
		}

		public XUnitExecutionSession CreateExecutionSession (bool reportToMonitor)
		{
			session = new XUnitExecutionSession (this, reportToMonitor);

			foreach (var test in Tests) {
				var xunitTest = test as IExecutableTest;
				if (xunitTest != null) {
					var childSession = xunitTest.CreateExecutionSession (reportToMonitor);
					session.AddChildSession (childSession);
				}
			}

			return session;
		}

		public override bool HasTests {
			get {
				return true;
			}
		}

		protected override void OnCreateTests ()
		{
			if (TestInfo.Tests == null)
				return;

			foreach (var info in TestInfo.Tests) {
				UnitTest test;
				if (info.Tests != null)
					test = new XUnitTestSuite (rootSuite, executor, info);
				else
					test = new XUnitTestCase (rootSuite, executor, info);
				Tests.Add (test);
			}
		}

		protected override bool OnCanRun (IExecutionHandler executionContext)
		{
			return rootSuite.CanRun (executionContext);
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return executor.RunTestSuite (rootSuite, this, testContext);
		}

		public override SourceCodeLocation SourceCodeLocation {
			get {
				return rootSuite.GetSourceCodeLocation (this);
			}
		}
	}
}
