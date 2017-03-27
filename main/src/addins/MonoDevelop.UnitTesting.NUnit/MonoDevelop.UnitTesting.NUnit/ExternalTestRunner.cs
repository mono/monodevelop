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
using MonoDevelop.Core.Assemblies;
using System.Threading.Tasks;

namespace MonoDevelop.UnitTesting.NUnit.External
{
	class ExternalTestRunner: IDisposable
	{
		RemoteProcessConnection connection;
		IRemoteEventListener listener;

		public ExternalTestRunner ()
		{
		}

		public Task Connect (NUnitVersion version, IExecutionHandler executionHandler = null, OperationConsole console = null)
		{
			var exePath = Path.Combine (Path.GetDirectoryName (GetType ().Assembly.Location), version.ToString (), "NUnitRunner.exe");
			connection = new RemoteProcessConnection (exePath, executionHandler, console, Runtime.MainSynchronizationContext);
			connection.AddListener (this);
			return connection.Connect ();
		}

		public Task Disconnect ()
		{
			return connection.Disconnect ();
		}

		public async Task<UnitTestResult> Run (IRemoteEventListener listener, string[] nameFilter, string path, string suiteName, List<string> supportAssemblies, string testRunnerType, string testRunnerAssembly, string crashLogFile)
		{
			this.listener = listener;

			var msg = new RunRequest {
				NameFilter = nameFilter,
				Path = path,
				SuiteName = suiteName,
				SupportAssemblies = supportAssemblies.ToArray (),
				TestRunnerType = testRunnerType,
				TestRunnerAssembly = testRunnerAssembly,
				CrashLogFile = crashLogFile
			};

			var r = (await connection.SendMessage (msg)).Result;

			await connection.ProcessPendingMessages ();

			return ToUnitTestResult (r);
		}

		UnitTestResult ToUnitTestResult (RemoteTestResult r)
		{
			if (r == null)
				return null;
			
			return new UnitTestResult {
				TestDate = r.TestDate,
				Status = (ResultStatus) (int)r.Status,
				Passed = r.Passed,
				Errors = r.Errors,
				Failures = r.Failures,
				Inconclusive = r.Inconclusive,
				NotRunnable = r.NotRunnable,
				Skipped = r.Skipped,
				Ignored = r.Ignored,
				Time = r.Time,
				Message = r.Message,
				StackTrace = r.StackTrace,
				ConsoleOutput = r.ConsoleOutput,
				ConsoleError = r.ConsoleError
			};
		}
		
		public async Task<NunitTestInfo> GetTestInfo (string path, List<string> supportAssemblies)
		{
			var msg = new GetTestInfoRequest {
				Path = path,
				SupportAssemblies = supportAssemblies.ToArray ()
			};

			return (await connection.SendMessage (msg)).Result;
		}

		[MessageHandler]
		public void OnTestStarted (TestStartedMessage msg)
		{
			listener.TestStarted (msg.TestCase);
		}

		[MessageHandler]
		public void OnTestFinished (TestFinishedMessage msg)
		{
			listener.TestFinished (msg.TestCase, ToUnitTestResult (msg.Result));
		}

		[MessageHandler]
		public void OnSuiteStarted (SuiteStartedMessage msg)
		{
			listener.SuiteStarted (msg.Suite);
		}

		[MessageHandler]
		public void OnSuiteFinished (SuiteFinishedMessage msg)
		{
			listener.SuiteFinished (msg.Suite, ToUnitTestResult (msg.Result));
		}

		public void Dispose ()
		{
			connection.Disconnect ().Ignore ();
		}
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

		public LocalTestMonitor (TestContext context, UnitTest rootTest, string rootFullName, bool singleTestRun)
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

		static readonly string FailedMessage = GettextCatalog.GetString ("Test failed");
		static readonly string IgnoredMessage = GettextCatalog.GetString ("Test ignored");
		static readonly string SuccededMessage = GettextCatalog.GetString ("Test successful") + "\n\n";
		void ProcessResult (UnitTestResult res)
		{
			if (string.IsNullOrEmpty (res.Message)) {
				if (res.IsFailure)
					res.Message = SuccededMessage;
				else if (res.IsNotRun)
					res.Message = IgnoredMessage;
				else {
					res.Message = SuccededMessage + GettextCatalog.GetString ("Execution time: {0:0.00}ms", res.Time.TotalMilliseconds);
				}
			}
		}
			
		void IRemoteEventListener.TestFinished (string test, UnitTestResult result)
		{
			if (Canceled)
				return;

			ProcessResult (result);

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
			
			ProcessResult (result);

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

	public interface IRemoteEventListener
	{
		void TestStarted (string testCase);
		void TestFinished (string test, UnitTestResult result);
		void SuiteStarted (string suite);
		void SuiteFinished (string suite, UnitTestResult result);
	}

	public enum NUnitVersion
	{
		Unknown,
		NUnit2,
		NUnit3
	}
}

