//
// NUnitAssemblyTestSuite.cs
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
using System.Runtime.Serialization.Formatters.Binary;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.UnitTesting.NUnit.External;
using MonoDevelop.Ide;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace MonoDevelop.UnitTesting.NUnit
{
	public abstract class NUnitAssemblyTestSuite: UnitTestGroup
	{
		object locker = new object ();
		UnitTest[] oldList;
		TestInfoCache testInfoCache = new TestInfoCache ();
		bool cacheLoaded;
		DateTime lastAssemblyTime;

		static Queue<LoadData> loadQueue = new Queue<LoadData> ();
		static bool loaderRunning;

		public virtual IList<string> UserAssemblyPaths {
			get {
				return null;
			}
		}

		public NUnitAssemblyTestSuite (string name): base (name)
		{
		}

		public NUnitAssemblyTestSuite (string name, WorkspaceObject ownerSolutionItem): base (name, ownerSolutionItem)
		{
		}
		
		public override void Dispose ()
		{
			try {
				if (TestInfoCachePath != null) {
					testInfoCache.Write (TestInfoCachePath);
				}
			} catch {
			}
			base.Dispose ();
		}

		public NUnitVersion NUnitVersion { get; set; }

		public override bool HasTests {
			get {
				return true;
			}
		}

		public virtual void GetCustomTestRunner (out string assembly, out string type)
		{
			assembly = type = null;
		}

		public virtual void GetCustomConsoleRunner (out string command, out string args)
		{
			command = args = null;
		}

		ProcessExecutionCommand GetCustomConsoleRunnerCommand ()
		{
			string file, args;

			GetCustomConsoleRunner (out file, out args);
			file = file != null ? file.Trim () : null;
			if (string.IsNullOrEmpty (file))
				return null;

			var cmd = Runtime.ProcessService.CreateCommand (file);
			cmd.Arguments = args;
			return cmd;
		}

		protected override void OnActiveConfigurationChanged ()
		{
			UpdateTests ();
			base.OnActiveConfigurationChanged ();
		}
		
		internal SourceCodeLocation GetSourceCodeLocation (UnitTest test)
		{
			return GetSourceCodeLocation (test.FixtureTypeNamespace, test.FixtureTypeName, test.Name);
		}
		
		protected virtual SourceCodeLocation GetSourceCodeLocation (string fixtureTypeNamespace, string fixtureTypeName, string testName)
		{
			return null;
		}
		
		public override int CountTestCases ()
		{
			lock (locker) {
				if (Status == TestStatus.Loading)
					Monitor.Wait (locker, 10000);
			}
			return base.CountTestCases ();
		}
		
		protected bool RefreshRequired {
			get {
				return lastAssemblyTime != GetAssemblyTime ();
			}
		}

		public override Task Refresh (CancellationToken ct)
		{
			return Task.Run (delegate {
				lock (locker) {
					try {
						while (Status == TestStatus.Loading) {
							Monitor.Wait (locker);
						}
						if (RefreshRequired) {
							lastAssemblyTime = GetAssemblyTime ();
							UpdateTests ();
							OnCreateTests (); // Force loading
							while (Status == TestStatus.Loading) {
								Monitor.Wait (locker);
							}
						}
					} catch {
					}
				}
			});
		}
		
		DateTime GetAssemblyTime ()
		{
			string path = AssemblyPath;
			if (File.Exists (path))
				return File.GetLastWriteTime (path);
			else
				return DateTime.MinValue;
		}

		
		protected override void OnCreateTests ()
		{
			lock (locker) {
				if (Status == TestStatus.Loading)
					return;
					
				NunitTestInfo ti = testInfoCache.GetInfo (AssemblyPath);
				if (ti != null) {
					FillTests (ti);
					return;
				}
				
				Status = TestStatus.Loading;
			}
			
			lastAssemblyTime = GetAssemblyTime ();
			
			if (oldList != null) {
				foreach (UnitTest t in oldList)
					Tests.Add (t);
			}

			OnTestStatusChanged ();

			LoadData ld = new LoadData ();
			ld.Path = AssemblyPath;
			ld.TestInfoCachePath = cacheLoaded ? null : TestInfoCachePath;
			ld.Callback = delegate {
				Runtime.RunInMainThread (delegate {
					if (ld.Error != null)
						this.ErrorMessage = ld.Error.Message;
					AsyncCreateTests (ld);
				});
			};
			ld.SupportAssemblies = new List<string> (SupportAssemblies);
			ld.NUnitVersion = NUnitVersion;
			
			AsyncLoadTest (ld);

			// Read the cache from disk only once
			cacheLoaded = true;
		}
		
		void AsyncCreateTests (object ob)
		{
			TestStatus newStatus = TestStatus.Ready;
			
			try {
				LoadData loadData = (LoadData) ob;
				
				if (loadData.Error != null) {
					newStatus = TestStatus.LoadError;
					return;
				}
				
				Tests.Clear ();

				if (loadData.Info == null) {
					oldList = new UnitTest [0];
					return;
				}

				FillTests (loadData.Info);
				
				// If the async loader has loaded a cache, reuse it.
				if (loadData.InfoCache != null)
					testInfoCache = loadData.InfoCache;
				
				testInfoCache.SetInfo (AssemblyPath, loadData.Info);
			}
			catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				newStatus = TestStatus.LoadError;
			}
			finally {
				lock (locker) {
					Status = newStatus;
					Monitor.PulseAll (locker);
				}
				OnTestChanged ();
			}
		}
		
		void FillTests (NunitTestInfo ti)
		{
			if (ti.Tests == null)
				return;
			foreach (NunitTestInfo test in ti.Tests) {
				UnitTest newTest;
				if (test.Tests != null)
					newTest = new NUnitTestSuite (this, test);
				else
					newTest = new NUnitTestCase (this, test, test.PathName);
				newTest.FixtureTypeName = test.FixtureTypeName;
				newTest.FixtureTypeNamespace = test.FixtureTypeNamespace;
				Tests.Add (newTest);

			}
			oldList = new UnitTest [Tests.Count];
			Tests.CopyTo (oldList, 0);
		}
		
		static void AsyncLoadTest (LoadData ld)
		{
			lock (loadQueue) {
				if (!loaderRunning) {
					Thread t = new Thread (new ThreadStart (RunAsyncLoadTest));
					t.Name = "NUnit test loader";
					t.IsBackground = true;
					t.Start ();
					loaderRunning = true;
				}
				loadQueue.Enqueue (ld);
				Monitor.Pulse (loadQueue);
			}
		}
		
		static void RunAsyncLoadTest ()
		{
			while (true) {
				LoadData ld;
				lock (loadQueue) {
					if (loadQueue.Count == 0) {
						if (!Monitor.Wait (loadQueue, 5000, true)) {
							loaderRunning = false;
							return;
						}
					}
					ld = (LoadData)loadQueue.Dequeue ();
				}
				
				try {
					// If the information is cached in a file and it is up to date information,
					// there is no need to parse again the assembly.

					if (ld.TestInfoCachePath != null && File.Exists (ld.TestInfoCachePath)) {
						ld.InfoCache = TestInfoCache.Read (ld.TestInfoCachePath);
						NunitTestInfo info = ld.InfoCache.GetInfo (ld.Path);
						if (info != null) {
							ld.Info = info;
							ld.Callback (ld);
							continue;
						}
					}
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}

				ExternalTestRunner runner = null;

				try {
					if (File.Exists (ld.Path)) {
						runner = new ExternalTestRunner ();
						runner.Connect (ld.NUnitVersion).Wait ();
						ld.Info = runner.GetTestInfo (ld.Path, ld.SupportAssemblies).Result;
					}
				} catch (AggregateException exception){
					var baseException = exception.GetBaseException ();
					Console.WriteLine (baseException);
					ld.Error = baseException;
				} catch (Exception exception) {
					Console.WriteLine (exception);
					ld.Error = exception;
				}
				finally {
					try {
						if (runner != null)
							runner.Dispose ();
					} catch {}
				}

				try {
					ld.Callback (ld);
				} catch {
				}
			}
		}
		
		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return RunUnitTest (this, "", "", null, testContext);
		}

		protected override bool OnCanRun (MonoDevelop.Core.Execution.IExecutionHandler executionContext)
		{
			var runnerCmd = GetCustomConsoleRunnerCommand ();
			if (runnerCmd != null) {
				return executionContext.CanExecute (runnerCmd);
			}
			return Runtime.ProcessService.IsValidForRemoteHosting (executionContext);
		}

		public string[] CollectTests (UnitTestGroup group)
		{
			List<string> result = new List<string> ();
			foreach (var t in group.Tests) {
				if (t.IsExplicit)
					continue;
				if (t is UnitTestGroup) {
					result.AddRange (CollectTests ((UnitTestGroup)t));
				} else {
					result.Add (t.TestId);
				}
			}
			return result.ToArray ();
		}

		internal UnitTestResult RunUnitTest (UnitTest test, string suiteName, string pathName, string testName, TestContext testContext)
		{
			var runnerExe = GetCustomConsoleRunnerCommand ();
			if (runnerExe != null)
				return RunWithConsoleRunner (runnerExe, test, suiteName, pathName, testName, testContext);

			var console = testContext.ExecutionContext.ConsoleFactory.CreateConsole (
				OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (GettextCatalog.GetString ("Unit Tests")));

			ExternalTestRunner runner = new ExternalTestRunner ();
			runner.Connect (NUnitVersion, testContext.ExecutionContext.ExecutionHandler, console).Wait ();
			LocalTestMonitor localMonitor = new LocalTestMonitor (testContext, test, suiteName, testName != null);

			string[] filter = null;
			if (test != null) {
				if (test is UnitTestGroup && NUnitVersion == NUnitVersion.NUnit2) {
					filter = CollectTests ((UnitTestGroup)test);
				} else if (test.TestId != null) {
					filter = new string [] { test.TestId };
				}
			}

			RunData rd = new RunData ();
			rd.Runner = runner;
			rd.Test = this;
			rd.LocalMonitor = localMonitor;

			var cancelReg = testContext.Monitor.CancellationToken.Register (rd.Cancel);

			UnitTestResult result;
			var crashLogFile = Path.GetTempFileName ();

			try {
				if (string.IsNullOrEmpty (AssemblyPath)) {
					string msg = GettextCatalog.GetString ("Could not get a valid path to the assembly. There may be a conflict in the project configurations.");
					throw new Exception (msg);
				}

				string testRunnerAssembly, testRunnerType;
				GetCustomTestRunner (out testRunnerAssembly, out testRunnerType);

				testContext.Monitor.CancellationToken.ThrowIfCancellationRequested ();
					
				result = runner.Run (localMonitor, filter, AssemblyPath, "", new List<string> (SupportAssemblies), testRunnerType, testRunnerAssembly, crashLogFile).Result;
				if (testName != null)
					result = localMonitor.SingleTestResult;
				
				ReportCrash (testContext, crashLogFile);
				
			} catch (Exception ex) {
				if (ReportCrash (testContext, crashLogFile)) {
					result = UnitTestResult.CreateFailure (GettextCatalog.GetString ("Unhandled exception"), null);
				}
				else if (!localMonitor.Canceled) {
					LoggingService.LogError (ex.ToString ());
					if (localMonitor.RunningTest != null) {
						RuntimeErrorCleanup (testContext, localMonitor.RunningTest, ex);
					} else {
						testContext.Monitor.ReportRuntimeError (null, ex);
						throw;
					}
					result = UnitTestResult.CreateFailure (ex);
				} else {
					result = UnitTestResult.CreateFailure (GettextCatalog.GetString ("Canceled"), null);
				}
			} finally {
				// Dispose the runner before the console, to make sure the console is available until the runner is disposed.
				runner.Disconnect ().Wait ();
				runner.Dispose ();
				if (console != null)
					console.Dispose ();
				cancelReg.Dispose ();
				File.Delete (crashLogFile);
			}
			
			return result;
		}

		bool ReportCrash (TestContext testContext, string crashLogFile)
		{
			var crash = File.ReadAllText (crashLogFile);
			if (crash.Length == 0)
				return false;

			var ex = RemoteUnhandledException.Parse (crash);
			testContext.Monitor.ReportRuntimeError (GettextCatalog.GetString ("Unhandled exception"), ex);
			return true;
		}

		void RuntimeErrorCleanup (TestContext testContext, UnitTest t, Exception ex)
		{
			UnitTestResult result = UnitTestResult.CreateFailure (ex);
			t.RegisterResult (testContext, result);
			while (t != null && t != this) {
				testContext.Monitor.EndTest (t, result);
				t.Status = TestStatus.Ready;
				t = t.Parent;
			}
		}

		UnitTestResult RunWithConsoleRunner (ProcessExecutionCommand cmd, UnitTest test, string suiteName, string pathName, string testName, TestContext testContext)
		{
			var outFile = Path.GetTempFileName ();
			var xmlOutputConsole = new LocalConsole ();
			var appDebugOutputConsole = testContext.ExecutionContext.ConsoleFactory.CreateConsole (
				OperationConsoleFactory.CreateConsoleOptions.Default.WithTitle (GettextCatalog.GetString ("Unit Tests")));
			OperationConsole cons;
			if (appDebugOutputConsole != null) {
				cons = new MultipleOperationConsoles (appDebugOutputConsole, xmlOutputConsole);
			} else {
				cons = xmlOutputConsole;
			}
			try {
				MonoDevelop.UnitTesting.NUnit.External.TcpTestListener tcpListener = null;
				LocalTestMonitor localMonitor = new LocalTestMonitor (testContext, test, suiteName, testName != null);

				if (!string.IsNullOrEmpty (cmd.Arguments))
					cmd.Arguments += " ";
				cmd.Arguments += "\"-xml=" + outFile + "\" " + AssemblyPath;

				bool automaticUpdates = cmd.Command != null && (cmd.Command.Contains ("GuiUnit") || (cmd.Command.Contains ("mdtool.exe") && cmd.Arguments.Contains ("run-md-tests")));
				if (!string.IsNullOrEmpty(pathName))
					cmd.Arguments += " -run=\"" + test.TestId.Replace("\"", "\\\"") + "\"";
				if (automaticUpdates) {
					tcpListener = new MonoDevelop.UnitTesting.NUnit.External.TcpTestListener (localMonitor, suiteName);
					cmd.Arguments += " -port=" + tcpListener.Port;
				}
				cmd.WorkingDirectory = Path.GetDirectoryName (AssemblyPath);

				// Note that we always dispose the tcp listener as we don't want it listening
				// forever if the test runner does not try to connect to it
				using (tcpListener) {
					var handler = testContext.ExecutionContext.ExecutionHandler;

					if (handler == null)
						handler = Runtime.ProcessService.DefaultExecutionHandler;
					
					var p = handler.Execute (cmd, cons);
					using (testContext.Monitor.CancellationToken.Register (p.Cancel))
						p.Task.Wait ();

					if (new FileInfo (outFile).Length == 0)
						throw new Exception ("Command failed");
				}

				// mdtool.exe does not necessarily guarantee we get automatic updates. It just guarantees
				// that if guiunit is being used then it will give us updates. If you have a regular test
				// assembly compiled against nunit.framework.dll 
				if (automaticUpdates && tcpListener.HasReceivedConnection) {
					if (testName != null)
						return localMonitor.SingleTestResult;
					return test.GetLastResult ();
				}

				XDocument doc = XDocument.Load (outFile);

				if (doc.Root != null) {
					var root = doc.Root.Elements ("test-suite").FirstOrDefault ();
					if (root != null) {
						xmlOutputConsole.SetDone ();
						var ot = xmlOutputConsole.OutReader.ReadToEnd ();
						var et = xmlOutputConsole.ErrorReader.ReadToEnd ();
						testContext.Monitor.WriteGlobalLog (ot);
						if (!string.IsNullOrEmpty (et)) {
							testContext.Monitor.WriteGlobalLog ("ERROR:\n");
							testContext.Monitor.WriteGlobalLog (et);
						}

						bool macunitStyle = doc.Root.Element ("environment") != null && doc.Root.Element ("environment").Attribute ("macunit-version") != null;
						var result = ReportXmlResult (localMonitor, root, "", macunitStyle);
						if (testName != null)
							result = localMonitor.SingleTestResult;
						return result;
					}
				}
				throw new Exception ("Test results could not be parsed.");
			} catch (Exception ex) {
				xmlOutputConsole.SetDone ();
				var ot = xmlOutputConsole.OutReader.ReadToEnd ();
				var et = xmlOutputConsole.ErrorReader.ReadToEnd ();
				testContext.Monitor.WriteGlobalLog (ot);
				if (!string.IsNullOrEmpty (et)) {
					testContext.Monitor.WriteGlobalLog ("ERROR:\n");
					testContext.Monitor.WriteGlobalLog (et);
				}
				testContext.Monitor.ReportRuntimeError ("Test execution failed.\n" + ot + "\n" + et, ex);
				return UnitTestResult.CreateIgnored ("Test execution failed");
			} finally {
				File.Delete (outFile);
				cons.Dispose ();
			}
		}

		UnitTestResult ReportXmlResult (IRemoteEventListener listener, XElement elem, string testPrefix, bool macunitStyle)
		{
			UnitTestResult result = new UnitTestResult ();
			var time = (string)elem.Attribute ("time");
			if (time != null)
				result.Time = TimeSpan.FromSeconds (double.Parse (time, CultureInfo.InvariantCulture));
			result.TestDate = DateTime.Now;

			var reason = elem.Element ("reason");
			if (reason != null)
				result.Message = (string) reason;

			var failure = elem.Element ("failure");
			if (failure != null) {
				var msg = failure.Element ("message");
				if (msg != null)
					result.Message = (string)msg;
				var stack = failure.Element ("stack-trace");
				if (stack != null)
					result.StackTrace = (string)stack;
			}

			switch ((string)elem.Attribute ("result")) {
			case "Error":
			case "Failure":
				result.Status = ResultStatus.Failure;
				break;
			case "Success":
				result.Status = ResultStatus.Success;
				break;
			case "Ignored":
				result.Status = ResultStatus.Ignored;
				break;
			default:
				result.Status = ResultStatus.Inconclusive;
				break;
			}

			if (elem.Name == "test-suite") {
				// nunitlite does not emit <test-suite type="Namespace" elements so we need to fake
				// them by deconstructing the full type name and emitting the suite started events manually
				var names = new List<string> ();
				if (!macunitStyle || (string)elem.Attribute ("type") == "Assembly")
					names.Add ("<root>");
				else
					names.AddRange (elem.Attribute ("name").Value.Split ('.'));

				for (int i = 0; i < names.Count; i ++)
					listener.SuiteStarted (testPrefix + string.Join (".", names.Take (i + 1)));

				var name = (string)elem.Attribute ("type") == "Assembly" ? "<root>" : (string) elem.Attribute ("name");
				var cts = elem.Element ("results");
				if (cts != null) {
					foreach (var ct in cts.Elements ()) {
						var r = ReportXmlResult (listener, ct, name != "<root>" ? testPrefix + name + "." : "", macunitStyle);
						result.Add (r);
					}
				}
				for (int i = 0; i < names.Count; i ++)
					listener.SuiteFinished (testPrefix + string.Join (".", names.Take (i + 1)), result);
			} else {
				string name = (string)elem.Attribute ("name");
				switch (result.Status) {
				case ResultStatus.Success:
					result.Passed++;
					break;
				case ResultStatus.Failure:
					result.Failures++;
					break;
				case ResultStatus.Ignored:
					result.Ignored++;
					break;
				case ResultStatus.Inconclusive:
					result.Inconclusive++;
					break;
				}

				listener.TestStarted (name);
				listener.TestFinished (name, result);
			}
			return result;
		}
		
		protected abstract string AssemblyPath {
			get;
		}
		
		protected virtual IEnumerable<string> SupportAssemblies {
			get { yield break; }
		}
		
		// File where cached test info for this test suite will be saved
		// Returns null by default which means that test info will not be saved.
		protected virtual string TestInfoCachePath {
			get { return null; }
		}
		
		class LoadData
		{
			public string Path;
			public string TestInfoCachePath;
			public Exception Error;
			public NunitTestInfo Info;
			public TestInfoCache InfoCache;
			public WaitCallback Callback;
			public List<string> SupportAssemblies;
			public NUnitVersion NUnitVersion;
		}
		
		class RunData
		{
			public ExternalTestRunner Runner;
			public UnitTest Test;
			public LocalTestMonitor LocalMonitor;
			
			public void Cancel ()
			{
				LocalMonitor.Canceled = true;
				Runner.Dispose ();
				ClearRunningStatus (Test);
			}
			
			void ClearRunningStatus (UnitTest t)
			{
				t.Status = TestStatus.Ready;
				UnitTestGroup group = t as UnitTestGroup;
				if (group == null) return;
				foreach (UnitTest ct in group.Tests)
					ClearRunningStatus (ct);
			}
		}
		
		[Serializable]
		class TestInfoCache
		{
			Hashtable table = new Hashtable ();
			
			[NonSerialized]
			bool modified;
			
			public void SetInfo (string path, NunitTestInfo info)
			{
				if (File.Exists (path)) {
					CachedTestInfo cti = new CachedTestInfo ();
					cti.LastWriteTime = File.GetLastWriteTime (path);
					cti.Info = info;
					table [path] = cti;
					modified = true;
				}
			}
			
			public NunitTestInfo GetInfo (string path)
			{
				CachedTestInfo cti = (CachedTestInfo) table [path];
				if (cti != null && File.Exists (path) && File.GetLastWriteTime (path) == cti.LastWriteTime)
					return cti.Info;
				else
					return null;
			}
			
			public static TestInfoCache Read (string file)
			{
				BinaryFormatter bf = new BinaryFormatter ();
				Stream s = new FileStream (file, FileMode.Open, FileAccess.Read);
				try {
					return (TestInfoCache) bf.Deserialize (s);
				} finally {
					s.Close ();
				}
			}
			
			public void Write (string file)
			{
				if (modified) {
					BinaryFormatter bf = new BinaryFormatter ();
					Stream s = new FileStream (file, FileMode.Create, FileAccess.Write);
					try {
						bf.Serialize (s, this);
					} finally {
						s.Close ();
					}
				}
			}
		}
		
		[Serializable]
		class CachedTestInfo
		{
			public DateTime LastWriteTime;
			public NunitTestInfo Info;
		}
	}
}

