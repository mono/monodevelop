//
// ExternalTestRunner.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using NUnit.Core;
using NUnit.Util;
using NF = NUnit.Framework;
using NC = NUnit.Core;

namespace MonoDevelop.NUnit.External
{
	class ExternalTestRunner: RemoteProcessObject
	{
		NUnitTestRunner runner;
		
		public UnitTestResult Run (IRemoteEventListener listener, ITestFilter filter, string path, string suiteName, List<string> supportAssemblies)
		{
			NUnitTestRunner runner = GetRunner (path);
			EventListenerWrapper listenerWrapper = listener != null ? new EventListenerWrapper (listener) : null;
			
			TestResult res = runner.Run (listenerWrapper, filter, path, suiteName, supportAssemblies);
			return listenerWrapper.GetLocalTestResult (res);
		}
		
		public NunitTestInfo GetTestInfo (string path, List<string> supportAssemblies)
		{
			NUnitTestRunner runner = GetRunner (path);
			return runner.GetTestInfo (path, supportAssemblies);
		}
		
		NUnitTestRunner GetRunner (string assemblyPath)
		{
			TestPackage package = new TestPackage (assemblyPath);
			package.Settings ["ShadowCopyFiles"] = false;
			DomainManager dm = new DomainManager ();
			AppDomain domain = dm.CreateDomain (package);
			string asm = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), "NUnitRunner.dll");
			runner = (NUnitTestRunner) domain.CreateInstanceFromAndUnwrap (asm, "MonoDevelop.NUnit.External.NUnitTestRunner");
			runner.Initialize (typeof(NF.Assert).Assembly.Location, typeof(NC.Test).Assembly.Location);
			return runner;
		}
	}
	
	class EventListenerWrapper: MarshalByRefObject, EventListener
	{
		IRemoteEventListener wrapped;
		StringBuilder consoleOutput;
		StringBuilder consoleError;
		
		public EventListenerWrapper (IRemoteEventListener wrapped)
		{
			this.wrapped = wrapped;
		}
		
		public void RunFinished (Exception exception)
		{
		}
		
		public void RunFinished (TestResult results)
		{
		}
		
		public void RunStarted (string name, int testCount)
		{
		}
		
		public void SuiteFinished (TestSuiteResult result)
		{
			wrapped.SuiteFinished (GetTestName (result.Test.TestName), GetLocalTestResult (result));
		}
		
		public void SuiteStarted (TestName suite)
		{
			wrapped.SuiteStarted (GetTestName (suite));
		}
		
		public void TestFinished (TestCaseResult result)
		{
			wrapped.TestFinished (GetTestName (result.Test.TestName), GetLocalTestResult (result));
		}
		
		public void TestOutput (TestOutput testOutput)
		{
			if (consoleOutput == null) {
				Console.WriteLine (testOutput.Text);
				return;
			}
			else if (testOutput.Type == TestOutputType.Out)
				consoleOutput.Append (testOutput.Text);
			else
				consoleError.Append (testOutput.Text);
		}
		
		public void TestStarted (TestName testCase)
		{
			wrapped.TestStarted (GetTestName (testCase));
			consoleOutput = new StringBuilder ();
			consoleError = new StringBuilder ();
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}
		
		public string GetTestName (TestName t)
		{
			if (t == null)
				return null;
			return t.FullName;
		}
		
		public UnitTestResult GetLocalTestResult (TestResult t)
		{
			UnitTestResult res = new UnitTestResult ();
			res.Message = t.Message;
			
			if (t is TestSuiteResult) {
				int s=0, f=0, i=0;
				CountResults ((TestSuiteResult)t, ref s, ref f, ref i);
				res.TotalFailures = f;
				res.TotalSuccess = s;
				res.TotalIgnored = i;
				if (f > 0)
					res.Status |= ResultStatus.Failure;
				if (s > 0)
					res.Status |= ResultStatus.Success;
				if (i > 0)
					res.Status |= ResultStatus.Ignored;
			} else {
				if (t.IsFailure) {
					res.Status = ResultStatus.Failure;
					res.TotalFailures = 1;
				}
				else if (!t.Executed) {
					res.Status = ResultStatus.Ignored;
					res.TotalIgnored = 1;
				}
				else {
					res.Status = ResultStatus.Success;
					res.TotalSuccess = 1;
				}
			
				if (string.IsNullOrEmpty (res.Message)) {
					if (t.IsFailure)
						res.Message = GettextCatalog.GetString ("Test failed");
					else if (!t.Executed)
						res.Message = GettextCatalog.GetString ("Test ignored");
					else {
						res.Message = GettextCatalog.GetString ("Test successful") + "\n\n";
						res.Message += GettextCatalog.GetString ("Execution time: {0:0.00}ms", t.Time);
					}
				}
			}
			res.StackTrace = t.StackTrace;
			res.Time = TimeSpan.FromSeconds (t.Time);
			
			if (consoleOutput != null) {
				res.ConsoleOutput = consoleOutput.ToString ();
				res.ConsoleError = consoleError.ToString ();
				consoleOutput = null;
				consoleError = null;
			}
			
			return res;
		}		
		
		void CountResults (TestSuiteResult ts, ref int s, ref int f, ref int i)
		{
			if (ts.Results == null)
				return;

			foreach (TestResult t in ts.Results) {
				if (t is TestCaseResult) {
					if (t.IsFailure)
						f++;
					else if (!t.Executed)
						i++;
					else
						s++;
				} else if (t is TestSuiteResult) {
					CountResults ((TestSuiteResult) t, ref s, ref f, ref i);
				}
			}
		}
		
		public void UnhandledException (Exception exception)
		{
		}
	}
	
	interface IRemoteEventListener
	{
		void TestStarted (string testCase);
		void TestFinished (string test, UnitTestResult result);
		void SuiteStarted (string suite);
		void SuiteFinished (string suite, UnitTestResult result);
	}
	
	class LocalTestMonitor: MarshalByRefObject, IRemoteEventListener
	{
		TestContext context;
		UnitTest rootTest;
		string rootFullName;
		UnitTest runningTest;
		bool singleTestRun;
		UnitTestResult singleTestResult;
		public bool Canceled;
		
		public LocalTestMonitor (TestContext context, ExternalTestRunner runner, UnitTest rootTest, string rootFullName, bool singleTestRun)
		{
			this.rootFullName = rootFullName;
			this.rootTest = rootTest;
			this.context = context;
			this.singleTestRun = singleTestRun;
		}
		
		public UnitTest RunningTest {
			get { return runningTest; }
		}
		
		internal UnitTestResult SingleTestResult {
			get {
				if (singleTestResult == null)
					singleTestResult = new UnitTestResult ();
				return singleTestResult;
			}
			set {
				singleTestResult = value;
			}
		}
		
		void IRemoteEventListener.TestStarted (string testCase)
		{
			if (singleTestRun || Canceled)
				return;
			
			UnitTest t = GetLocalTest (testCase);
			if (t == null)
				return;
			
			runningTest = t;
			context.Monitor.BeginTest (t);
			t.Status = TestStatus.Running;
		}
			
		void IRemoteEventListener.TestFinished (string test, UnitTestResult result)
		{
			if (Canceled)
				return;
			if (singleTestRun) {
				SingleTestResult = result;
				return;
			}
			
			UnitTest t = GetLocalTest (test);
			if (t == null)
				return;
			
			t.RegisterResult (context, result);
			context.Monitor.EndTest (t, result);
			t.Status = TestStatus.Ready;
			runningTest = null;
		}

		void IRemoteEventListener.SuiteStarted (string suite)
		{
			if (singleTestRun || Canceled)
				return;
			
			UnitTest t = GetLocalTest (suite);
			if (t == null)
				return;
			
			t.Status = TestStatus.Running;
			context.Monitor.BeginTest (t);
		}

		void IRemoteEventListener.SuiteFinished (string suite, UnitTestResult result)
		{
			if (singleTestRun || Canceled)
				return;
			
			UnitTest t = GetLocalTest (suite);
			if (t == null)
				return;
			
			t.RegisterResult (context, result);
			t.Status = TestStatus.Ready;
			context.Monitor.EndTest (t, result);
		}
		
		UnitTest GetLocalTest (string sname)
		{
			if (sname == null) return null;
			if (sname == "<root>") return rootTest;
			
			if (sname.StartsWith (rootFullName)) {
				sname = sname.Substring (rootFullName.Length);
			}
			if (sname.StartsWith (".")) sname = sname.Substring (1);
			UnitTest tt = FindTest (rootTest, sname);
			return tt;
		}
		
		UnitTest FindTest (UnitTest t, string testPath)
		{
			if (testPath == "")
				return t;

			UnitTestGroup group = t as UnitTestGroup;
			if (group == null)
				return null;

			UnitTest returnTest = group.Tests [testPath];
			if (returnTest != null)
				return returnTest;

			string[] paths = testPath.Split (new char[] {'.'}, 2);
			if (paths.Length == 2) {
				string nextPathSection = paths[0];
				string nextTestCandidate = paths[1];

				UnitTest childTest = group.Tests [nextPathSection];
				if (childTest != null)
					return FindTest (childTest, nextTestCandidate);
			}
			return null;
		}
	}	
}

