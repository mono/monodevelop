//
// XUnitTestExecutor.cs
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
using MonoDevelop.Projects;
using System.Linq;
using System.IO;

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Wrapper around XUnitTestRunner. It extracts all information needed to
	/// run tests, then dispatches rusults back to tests.
	/// </summary>
	public class XUnitTestExecutor
	{
		public UnitTestResult RunTestCase (XUnitAssemblyTestSuite rootSuite, XUnitTestCase testCase, TestContext context)
		{
			return Run (new List<XUnitTestCase> { testCase }, rootSuite, testCase, context);
		}

		public UnitTestResult RunTestSuite (XUnitAssemblyTestSuite rootSuite, XUnitTestSuite testSuite, TestContext context)
		{
			var testCases = new List<XUnitTestCase> ();
			CollectTestCases (testCases, testSuite);

			return Run (testCases, rootSuite, testSuite, context);
		}

		public UnitTestResult RunAssemblyTestSuite (XUnitAssemblyTestSuite assemblyTestSuite, TestContext context)
		{
			var testCases = new List<XUnitTestCase> ();

			foreach (var test in assemblyTestSuite.Tests) {
				var testSuite = test as XUnitTestSuite;
				if (testSuite != null)
					CollectTestCases (testCases, testSuite);
			}

			return Run (testCases, assemblyTestSuite, assemblyTestSuite, context);
		}

		void CollectTestCases (List<XUnitTestCase> testCases, XUnitTestSuite testSuite)
		{
			foreach (var test in testSuite.Tests) {
				if (test is XUnitTestCase)
					testCases.Add ((XUnitTestCase)test);
				else
					CollectTestCases (testCases, (XUnitTestSuite)test);
			}
		}

		UnitTestResult Run (List<XUnitTestCase> testCases, XUnitAssemblyTestSuite rootSuite, IExecutableTest test, TestContext context)
		{
			using (var session = test.CreateExecutionSession ()) {
				var executionListener = new RemoteExecutionListener (new LocalExecutionListener (context, testCases));
				System.Runtime.Remoting.RemotingServices.Marshal (executionListener, null, typeof (IXUnitExecutionListener));

				XUnitTestRunner runner = (XUnitTestRunner)Runtime.ProcessService.CreateExternalProcessObject (typeof(XUnitTestRunner),
					context.ExecutionContext, rootSuite.SupportAssemblies);

				try {
					runner.Execute (rootSuite.AssemblyPath, testCases.Select (tc => tc.TestInfo).ToArray (), executionListener);
				} finally {
					runner.Dispose ();
				}

				return session.Result;
			}
		}
	}

	public class RemoteExecutionListener: MarshalByRefObject, IXUnitExecutionListener
	{
		IXUnitExecutionListener localListener;

		public bool IsCancelRequested {
			get {
				return localListener.IsCancelRequested;
			}
		}

		public RemoteExecutionListener (IXUnitExecutionListener localListener)
		{
			this.localListener = localListener;
		}

		public void OnTestCaseStarting (string id)
		{
			localListener.OnTestCaseStarting (id);
		}

		public void OnTestCaseFinished (string id)
		{
			localListener.OnTestCaseFinished (id);
		}

		public void OnTestFailed (string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			localListener.OnTestFailed (id, executionTime, output, exceptionTypes, messages, stackTraces);
		}

		public void OnTestPassed (string id, decimal executionTime, string output)
		{
			localListener.OnTestPassed (id, executionTime, output);
		}

		public void OnTestSkipped (string id, string reason)
		{
			localListener.OnTestSkipped (id, reason);
		}
	}

	public class LocalExecutionListener: IXUnitExecutionListener
	{
		TestContext context;
		Dictionary<string, XUnitTestCase> lookup;

		public LocalExecutionListener (TestContext context, List<XUnitTestCase> testCases)
		{
			this.context = context;

			// create a lookup table so later we can identify a test case by it's id
			lookup = testCases.ToDictionary (tc => tc.TestInfo.Id);
		}

		public bool IsCancelRequested {
			get {
				return context.Monitor.IsCancelRequested;
			}
		}

		public void OnTestCaseStarting (string id)
		{
			var testCase = lookup [id];
			testCase.OnStarting (context);
		}

		public void OnTestCaseFinished (string id)
		{
			var testCase = lookup [id];
			testCase.OnFinished (context);
		}

		public void OnTestFailed (string id, decimal executionTime, string output, string[] exceptionTypes, string[] messages, string[] stackTraces)
		{
			var testCase = lookup [id];
			testCase.OnFailed (context, executionTime, output, exceptionTypes, messages, stackTraces);
		}

		public void OnTestPassed (string id, decimal executionTime, string output)
		{
			var testCase = lookup [id];
			testCase.OnPassed (context, executionTime, output);
		}

		public void OnTestSkipped (string id, string reason)
		{
			var testCase = lookup [id];
			testCase.OnSkipped (context, reason);
		}
	}
}
