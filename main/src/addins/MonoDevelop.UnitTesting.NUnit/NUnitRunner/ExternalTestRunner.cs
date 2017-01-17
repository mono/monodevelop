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

using NUnit.Core;
using NUnit.Util;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.UnitTesting.NUnit.External
{
	public class RemoteNUnitTestRunner: MessageListener
	{
		NUnitTestRunner runner;
		RemoteProcessServer server;

		public RemoteNUnitTestRunner (RemoteProcessServer server)
		{
			this.server = server;

			// Add standard services to ServiceManager
			ServiceManager.Services.AddService (new DomainManager ());
			ServiceManager.Services.AddService (new ProjectService ());
			ServiceManager.Services.AddService (new AddinRegistry ());
			ServiceManager.Services.AddService (new AddinManager ());
			ServiceManager.Services.AddService (new TestAgency ());
			
			// Initialize services
			ServiceManager.Services.InitializeServices ();
		}

		[MessageHandler]
		public RunResponse Run (RunRequest r)
		{
			var res = Run (r.NameFilter, r.Path, r.SuiteName, r.SupportAssemblies, r.TestRunnerType, r.TestRunnerAssembly, r.CrashLogFile);
			return new RunResponse () { Result = res };
		}

		public RemoteTestResult Run (string[] nameFilter, string path, string suiteName, string[] supportAssemblies, string testRunnerType, string testRunnerAssembly, string crashLogFile)
		{
			NUnitTestRunner runner = GetRunner (path, false);
			EventListenerWrapper listenerWrapper = new EventListenerWrapper (server);
			
			UnhandledExceptionEventHandler exceptionHandler = (object sender, UnhandledExceptionEventArgs e) => {
				var ex = e.ExceptionObject;
				File.WriteAllText (crashLogFile, e.ToString ());
			};

			AppDomain.CurrentDomain.UnhandledException += exceptionHandler;
			try {
				TestResult res = runner.Run (listenerWrapper, nameFilter, path, suiteName, supportAssemblies, testRunnerType, testRunnerAssembly);
				return listenerWrapper.GetLocalTestResult (res);
			} finally {
				AppDomain.CurrentDomain.UnhandledException -= exceptionHandler;
			}
		}

		[MessageHandler]
		public GetTestInfoResponse GetTestInfo (GetTestInfoRequest req)
		{
			NUnitTestRunner runner = GetRunner (req.Path, true);
			var r = runner.GetTestInfo (req.Path, req.SupportAssemblies);
			return new GetTestInfoResponse { Result = r };
		}
		
		NUnitTestRunner GetRunner (string assemblyPath, bool forQuery)
		{
			string basePath = Path.GetDirectoryName (GetType ().Assembly.Location);

			TestPackage package = new TestPackage (assemblyPath);
			package.Settings ["ShadowCopyFiles"] = false;

			// This is a workaround for what could be a Mono bug (hard to tell).
			// For the test runner to be able to load the app.config file,
			// the BasePath of the domain needs to be set to the location
			// of the test assembly (see bug #41541 - App Config is not read in Unit Tests).
			// However, when doing that, the test runner crashes in some cases
			// (for example when loading the MD unit tests). It crashes because
			// Mono gets confused and tries to load assemblies from two different
			// locations. As a workaround, we set the test assebmly directory
			// as base path only when running, which seems to work.

			if (forQuery)
				package.BasePath = basePath;
			else
				package.BasePath = Path.GetDirectoryName (assemblyPath);

			AppDomain domain = Services.DomainManager.CreateDomain (package);
			string asm = Path.Combine (basePath, "NUnitRunner.exe");
			runner = (NUnitTestRunner)domain.CreateInstanceFromAndUnwrap (asm, "MonoDevelop.UnitTesting.NUnit.External.NUnitTestRunner");
			runner.Initialize ();
			return runner;
		}
	}
	
	class EventListenerWrapper: MarshalByRefObject, EventListener
	{
		RemoteProcessServer server;
		StringBuilder consoleOutput;
		StringBuilder consoleError;
		
		public EventListenerWrapper (RemoteProcessServer server)
		{
			this.server = server;
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
			server.SendMessage (new SuiteFinishedMessage {
				Suite = GetTestName (result.Test),
				Result = GetLocalTestResult (result)
			});
		}

		public void SuiteStarted (TestName suite)
		{
			server.SendMessage (new SuiteStartedMessage {
				Suite = GetTestName (suite)
			});
		}
		
		public void TestFinished (TestResult result)
		{
			server.SendMessage (new TestFinishedMessage {
				TestCase = GetTestName (result.Test),
				Result = GetLocalTestResult (result)
			});
		}
		
		public void TestOutput (TestOutput testOutput)
		{
			Console.WriteLine (testOutput.Text);
			if (consoleOutput == null)
				return;
			else if (testOutput.Type == TestOutputType.Out)
				consoleOutput.Append (testOutput.Text);
			else
				consoleError.Append (testOutput.Text);
		}
		
		public void TestStarted (TestName testCase)
		{
			server.SendMessage (new TestStartedMessage {
				TestCase = GetTestName (testCase)
			});
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
		
		public RemoteTestResult GetLocalTestResult (TestResult t)
		{
			RemoteTestResult res = new RemoteTestResult ();
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
	
	public interface IRemoteEventListener
	{
		void TestStarted (string testCase);
		void TestFinished (string test, RemoteTestResult result);
		void SuiteStarted (string suite);
		void SuiteFinished (string suite, RemoteTestResult result);
	}
}

