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

		public ExternalTestRunner ()
		{
			// In some cases MS.NET can't properly resolve assemblies even if they
			// are already loaded. For example, when deserializing objects from remoting.
			AppDomain.CurrentDomain.AssemblyResolve += delegate (object s, ResolveEventArgs args) {
				foreach (Assembly am in AppDomain.CurrentDomain.GetAssemblies ()) {
					if (am.GetName ().FullName == args.Name)
						return am;
				}
				return null;
			};
			
			// Add standard services to ServiceManager
			ServiceManager.Services.AddService (new DomainManager ());
			ServiceManager.Services.AddService (new ProjectService ());
			ServiceManager.Services.AddService (new AddinRegistry ());
			ServiceManager.Services.AddService (new AddinManager ());
			ServiceManager.Services.AddService (new TestAgency ());
			
			// Initialize services
			ServiceManager.Services.InitializeServices ();
			
			// Preload the runner assembly. Required because TestNameFilter is implemented there
			string asm = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), "NUnitRunner.dll");
			Assembly.LoadFrom (asm);
		}

		public UnitTestResult Run (IRemoteEventListener listener, ITestFilter filter, string path, string suiteName, List<string> supportAssemblies, string testRunnerType, string testRunnerAssembly)
		{
			NUnitTestRunner runner = GetRunner (path);
			EventListenerWrapper listenerWrapper = listener != null ? new EventListenerWrapper (listener) : null;
			
			TestResult res = runner.Run (listenerWrapper, filter, path, suiteName, supportAssemblies, testRunnerType, testRunnerAssembly);
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
			
			AppDomain domain = Services.DomainManager.CreateDomain (package);
			string asm = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), "NUnitRunner.dll");
			runner = (NUnitTestRunner)domain.CreateInstanceFromAndUnwrap (asm, "MonoDevelop.NUnit.External.NUnitTestRunner");
			runner.PreloadAssemblies (typeof(NF.Assert).Assembly.Location, typeof (NC.TestSuiteBuilder).Assembly.Location, typeof(NC.Test).Assembly.Location);
			runner.Initialize ();
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
		
		public void SuiteFinished (TestResult result)
		{
			wrapped.SuiteFinished (GetTestName (result.Test), GetLocalTestResult (result));
		}

		public void SuiteStarted (TestName suite)
		{
			wrapped.SuiteStarted (GetTestName (suite));
		}
		
		public void TestFinished (TestResult result)
		{
			wrapped.TestFinished (GetTestName (result.Test), GetLocalTestResult (result));
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
		
		string GetTestName (ITest t)
		{
			if (t == null)
				return null;
			return t.TestName.FullName;
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
			var summary = new ResultSummarizer (t);
			res.Failures = summary.Failures;
			res.Errors = summary.Errors;
			res.Ignored = summary.Ignored;
			res.Inconclusive = summary.Inconclusive;
			res.NotRunnable = summary.NotRunnable;
			res.Passed = summary.Passed;
			res.StackTrace = t.StackTrace;
			res.Time = TimeSpan.FromSeconds (t.Time);

			res.Message = t.Message;
			if (string.IsNullOrEmpty (res.Message)) {
				if (res.IsFailure)
					res.Message = GettextCatalog.GetString ("Test failed");
				else if (!t.Executed)
					res.Message = GettextCatalog.GetString ("Test ignored");
				else {
					res.Message = GettextCatalog.GetString ("Test successful") + "\n\n";
					res.Message += GettextCatalog.GetString ("Execution time: {0:0.00}ms", t.Time);
				}
			}

			if (consoleOutput != null) {
				res.ConsoleOutput = consoleOutput.ToString ();
				res.ConsoleError = consoleError.ToString ();
				consoleOutput = null;
				consoleError = null;
			}
			
			return res;
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
//		string rootFullName;
		UnitTest runningTest;
		bool singleTestRun;
		UnitTestResult singleTestResult;
		public bool Canceled;

		public LocalTestMonitor (TestContext context, ExternalTestRunner runner, UnitTest rootTest, string rootFullName, bool singleTestRun)
		{
//			this.rootFullName = rootFullName;
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
			if (sname == null)
				return null;
			if (sname == "<root>")
				return rootTest;
			/*
			if (sname.StartsWith (rootFullName)) {
				sname = sname.Substring (rootFullName.Length);
			}
			if (sname.StartsWith ("."))
				sname = sname.Substring (1);*/
			UnitTest tt = FindTest (rootTest, sname);
			return tt;
		}
		
		UnitTest FindTest (UnitTest t, string testPath)
		{
			var group = t as UnitTestGroup;
			if (group == null)
				return null;
			return SearchRecursive (group, testPath);
		}

		UnitTest SearchRecursive (UnitTestGroup group, string testPath)
		{
			UnitTest result;
			foreach (var t in group.Tests) {
				if (t.TestId == testPath)
					return t;
				var childGroup = t as UnitTestGroup;
				if (childGroup != null) {
					result = SearchRecursive (childGroup, testPath);
					if (result != null)
						return result;
				}
			}
			return null;
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}

	}	
}

