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
using System.Threading;

using MonoDevelop.Services;
using NUnit.Core;

namespace MonoDevelop.NUnit
{
	class ExternalTestRunner: RemoteProcessObject
	{
		string assemblyName;
		StringWriter stdout = new StringWriter ();
		StringWriter stderr = new StringWriter ();
		
		public TestResult Run (EventListener listener, IFilter filter, string path, string suiteName, string testName)
		{
			TestSuite rootTS = LoadTestSuite (path, suiteName);

			TextWriter origStdout = Console.Out;
			TextWriter origStderr = Console.Error;
			Console.SetOut (stdout);
			Console.SetError (stderr);
			
			string cdir = Environment.CurrentDirectory;
			Environment.CurrentDirectory = Path.GetDirectoryName (path);
		
			try {
				Test nt = null;
				if (testName != null) {
					foreach (Test t in rootTS.Tests)
						if (t.Name == testName) {
							nt = t;
							break;
						}
				} else
					nt = rootTS;
					
				if (nt == null)
					throw new Exception ("Test " + suiteName + "." + testName + " not found.");
					
				return nt.Run (listener, filter);
			} finally {
				Environment.CurrentDirectory = cdir;
				Console.SetOut (origStdout);
				Console.SetError (origStderr);
			}
		}
		
		public string ResetTestConsoleOutput ()
		{
			string s = stdout.ToString ();
			stdout = new StringWriter ();
			Console.SetOut (stdout);
			return s;
		}
			
		public string ResetTestConsoleError ()
		{
			string s = stderr.ToString ();
			stderr = new StringWriter ();
			Console.SetError (stderr);
			return s;
		}
			
		TestSuite LoadTestSuite (string path, string fullName)
		{
			ResolveEventHandler reh = new ResolveEventHandler (TryLoad);
			AppDomain.CurrentDomain.AssemblyResolve += reh;
			assemblyName = path;

			try {
				if (fullName != "")
					return new TestSuiteBuilder ().Build (path, fullName);
				else
					return new TestSuiteBuilder ().Build (path);
			} finally {
				AppDomain.CurrentDomain.AssemblyResolve -= reh;
			}
		}

		Assembly TryLoad (object sender, ResolveEventArgs args)
		{
			try {
				// NUnit2 uses Assembly.Load on the filename without extension.
				// This is done just to allow loading from a full path name.
				return Assembly.LoadFrom (assemblyName);
			} catch { }

			return null;
		}
		
		public TestInfo GetTestInfo (string path)
		{
			TestSuite rootTS = LoadTestSuite (path, "");
			return BuildTestInfo (rootTS);
		}
		
		TestInfo BuildTestInfo (Test test)
		{
			TestInfo ti = new TestInfo ();
			ti.Name = test.Name;
			int i = test.FullName.LastIndexOf ('.');
			if (i != -1)
				ti.PathName = test.FullName.Substring (0,i);
			else
				ti.PathName = null;
				
			if (test.Tests != null && test.Tests.Count > 0) {
				ti.Tests = new TestInfo [test.Tests.Count];
				for (int n=0; n<test.Tests.Count; n++)
					ti.Tests [n] = BuildTestInfo ((Test)test.Tests [n]);
			}
			return ti;
		}
	}
	
	[Serializable]
	class TestInfo
	{
		public string Name;
		public string PathName;
		public TestInfo[] Tests;
	}
	
	class LocalTestMonitor: MarshalByRefObject, EventListener
	{
		TestContext context;
		UnitTest rootTest;
		string rootFullName;
		ExternalTestRunner runner;
		UnitTest runningTest;
		
		public LocalTestMonitor (TestContext context, ExternalTestRunner runner, UnitTest rootTest, string rootFullName)
		{
			this.runner = runner;
			this.rootFullName = rootFullName;
			this.rootTest = rootTest;
			this.context = context;
		}
		
		public UnitTest RunningTest {
			get { return runningTest; }
		}
		
		void EventListener.RunStarted (Test [] tests)
		{
		}

		void EventListener.RunFinished (TestResult [] results)
		{
		}

		void EventListener.UnhandledException (Exception exception)
		{
		}

		void EventListener.RunFinished (Exception exc)
		{
		}

		void EventListener.TestStarted (TestCase testCase)
		{
			UnitTest t = GetLocalTest (testCase);
			runningTest = t;
			context.Monitor.BeginTest (t);
			t.Status = TestStatus.Running;
		}
			
		void EventListener.TestFinished (TestCaseResult result)
		{
			UnitTest t = GetLocalTest ((Test) result.Test);
			UnitTestResult res = GetLocalTestResult (result);
			res.ConsoleOutput = runner.ResetTestConsoleOutput ();
			res.ConsoleError = runner.ResetTestConsoleError ();
			t.RegisterResult (context, res);
			context.Monitor.EndTest (t, res);
			t.Status = TestStatus.Ready;
			runningTest = null;
		}

		void EventListener.SuiteStarted (TestSuite suite)
		{
			UnitTest t = GetLocalTest (suite);
			t.Status = TestStatus.Running;
			context.Monitor.BeginTest (t);
		}

		void EventListener.SuiteFinished (TestSuiteResult result)
		{
			UnitTest t = GetLocalTest ((Test) result.Test);
			UnitTestResult res = GetLocalTestResult (result);
			t.RegisterResult (context, res);
			t.Status = TestStatus.Ready;
			context.Monitor.EndTest (t, res);
		}
		
		UnitTest GetLocalTest (Test t)
		{
			if (t == null) return null;
			if (t.Parent == null) return rootTest;
			
			string fn = t.FullName;
			string sname = fn.Substring (rootFullName.Length);
			if (sname.StartsWith (".")) sname = sname.Substring (1);
			UnitTest tt = FindTest (rootTest, sname);
			return tt;
		}
		
		UnitTest FindTest (UnitTest t, string testPath)
		{
			if (testPath == "")
				return t;

			string[] path = testPath.Split ('.');
			foreach (string part in path) {
				UnitTestGroup group = t as UnitTestGroup;
				if (group == null)
					return null;
				
				t = group.Tests [part];
				if (t == null)
					return null;
			}
			return t;
		}
		
		public UnitTestResult GetLocalTestResult (TestResult t)
		{
			UnitTestResult res = new UnitTestResult ();
			
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
			}
			
			res.Message = t.Message;
			res.StackTrace = t.StackTrace;
			res.Time = TimeSpan.FromSeconds (t.Time);
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
	}	
}

