//
// DotNetCoreProjectTestSuite.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting;

namespace MonoDevelop.DotNetCore.UnitTesting
{
	class DotNetCoreProjectTestSuite : UnitTestGroup, IDotNetCoreTestProvider, IDotNetCoreTestRunner
	{
		DotNetProject project;
		DotNetCoreTestPlatformAdapter testPlatformAdapter;
		DateTime? lastBuildTime;
		UnitTest[] oldTests;

		public DotNetCoreProjectTestSuite (DotNetCoreProjectExtension dotNetCoreProject)
			: base (dotNetCoreProject.Project.Name, dotNetCoreProject.Project)
		{
			project = dotNetCoreProject.Project;
			lastBuildTime = GetAssemblyLastWriteTime ();

			CreateResultsStore ();

			testPlatformAdapter = new DotNetCoreTestPlatformAdapter ();
			testPlatformAdapter.DiscoveryCompleted += TestDiscoveryCompleted;
			testPlatformAdapter.DiscoveryFailed += TestDiscoveryFailed;

			IdeApp.ProjectOperations.EndBuild += AfterBuild;
		}

		void CreateResultsStore ()
		{
			string storeId = Path.GetFileName (project.FileName);
			string resultsPath = UnitTestService.GetTestResultsDirectory (project.BaseDirectory);
			ResultsStore = new BinaryResultsStore (resultsPath, storeId);
		}

		public override bool HasTests {
			get { return true; }
		}

		public bool CanRunTests (IExecutionHandler executionContext)
		{
			return OnCanRun (executionContext);
		}

		protected override bool OnCanRun (IExecutionHandler executionContext)
		{
			FilePath assemblyFileName = GetAssemblyFileName ();
			if (assemblyFileName.IsNull)
				return false;

			var command = new DotNetCoreExecutionCommand (
				assemblyFileName.ParentDirectory,
				assemblyFileName.FileName,
				string.Empty);

			return executionContext.CanExecute (command);
		}

		protected override void OnCreateTests ()
		{
			if (DotNetCoreRuntime.IsMissing) {
				LoggingService.LogError (".NET Core not installed.");
				Status = TestStatus.LoadError;
				testPlatformAdapter.HasDiscoveryFailed = true;
				return;
			}

			if (!testPlatformAdapter.IsDiscoveringTests) {
				AddOldTests ();
				string assemblyFileName = GetAssemblyFileName ();
				if (File.Exists (assemblyFileName)) {
					Status = TestStatus.Loading;
					testPlatformAdapter.StartDiscovery (assemblyFileName);
				}
			}
		}

		string GetAssemblyFileName ()
		{
			return project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
		}

		void AddOldTests ()
		{
			if (oldTests != null) {
				foreach (var test in oldTests) {
					Tests.Add (test);
				}
			}
		}

		public override void Dispose ()
		{
			IdeApp.ProjectOperations.EndBuild -= AfterBuild;

			testPlatformAdapter.DiscoveryFailed -= TestDiscoveryFailed;
			testPlatformAdapter.DiscoveryCompleted -= TestDiscoveryCompleted;
			testPlatformAdapter.Stop ();
			base.Dispose ();
		}

		void TestDiscoveryCompleted (object sender, EventArgs e)
		{
			var discoveredTests = testPlatformAdapter.DiscoveredTests;

			var tests = discoveredTests.BuildTestInfo (this);

			Runtime.RunInMainThread (() => {
				Status = TestStatus.Ready;

				Tests.Clear ();

				foreach (UnitTest test in tests) {
					Tests.Add (test);
				}

				OnTestChanged ();
			});
		}

		void AfterBuild (object sender, BuildEventArgs args)
		{
			DateTime? buildTime = GetAssemblyLastWriteTime ();
			if (RefreshRequired (buildTime)) {
				lastBuildTime = buildTime;

				SaveOldTests ();

				UpdateTests ();
			}
		}

		bool RefreshRequired (DateTime? buildTime)
		{
			if (buildTime.HasValue) {
				if (lastBuildTime.HasValue) {
					return buildTime > lastBuildTime;
				}
				return true;
			}

			return false;
		}

		void SaveOldTests ()
		{
			if (Tests.Count > 0) {
				oldTests = new UnitTest[Tests.Count];
				Tests.CopyTo (oldTests, 0);
			}
		}

		DateTime? GetAssemblyLastWriteTime ()
		{
			string path = GetAssemblyFileName ();
			if (File.Exists (path))
				return File.GetLastWriteTime (path);

			return null;
		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			return RunTest (testContext, this, GetAssemblyFileName ());
		}

		UnitTestResult RunTest (
			TestContext testContext,
			IDotNetCoreTestProvider testProvider,
			string testAssemblyPath)
		{
			if (testPlatformAdapter.HasDiscoveryFailed) {
				return ReportLoadError (testContext);
			}

			if (!File.Exists (testAssemblyPath)) {
				return HandleMissingAssemblyOnRun (testContext, testAssemblyPath);
			}

			if (testContext.ExecutionContext.ExecutionHandler == null)
				testPlatformAdapter.RunTests (testContext, testProvider, testAssemblyPath);
			else
				testPlatformAdapter.DebugTests (testContext, testProvider, testAssemblyPath);

			while (testPlatformAdapter.IsRunningTests) {
				if (testContext.Monitor.CancellationToken.IsCancellationRequested) {
					testPlatformAdapter.CancelTestRun ();
					break;
				}

				Thread.Sleep (100);
			}
			Status = TestStatus.Ready;
			return testPlatformAdapter.TestResult;
		}

		UnitTestResult ReportLoadError (TestContext testContext)
		{
			var exception = new UserException (
				GettextCatalog.GetString ("Unable to run tests. Test discovery failed."));

			return ReportRunFailure (testContext, exception);
		}

		UnitTestResult HandleMissingAssemblyOnRun (TestContext testContext, string testAssemblyPath)
		{
			var exception = new FileNotFoundException (
				GettextCatalog.GetString ("Unable to run tests. Assembly not found '{0}'", testAssemblyPath),
				testAssemblyPath);

			return ReportRunFailure (testContext, exception);
		}

		UnitTestResult ReportRunFailure (TestContext testContext, Exception exception)
		{
			testContext.Monitor.ReportRuntimeError (exception.Message, exception);
			return UnitTestResult.CreateFailure (exception);
		}

		public UnitTestResult RunTest (TestContext testContext, IDotNetCoreTestProvider testProvider)
		{
			return RunTest (testContext, testProvider, GetAssemblyFileName ());
		}

		public IEnumerable<TestCase> GetTests ()
		{
			return null;
		}

		void TestDiscoveryFailed (object sender, EventArgs e)
		{
			Runtime.RunInMainThread (() => {
				Status = TestStatus.LoadError;
			});
		}
	}
}
