//
// VsTestProjectTestSuite.cs
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.UnitTesting;

namespace MonoDevelop.UnitTesting.VsTest
{
	class VsTestProjectTestSuite : UnitTestGroup, IVsTestTestProvider, IVsTestTestRunner
	{
		public Project Project { get; private set; }
		DateTime? lastBuildTime;
		UnitTest [] oldTests;

		public VsTestProjectTestSuite (Project project)
			: base (project.Name, project)
		{
			Project = project;
			lastBuildTime = GetAssemblyLastWriteTime ();

			CreateResultsStore ();

			IdeApp.ProjectOperations.EndBuild += AfterBuild;
		}

		void CreateResultsStore ()
		{
			string storeId = Path.GetFileName (Project.FileName);
			string resultsPath = UnitTestService.GetTestResultsDirectory (Project.BaseDirectory);
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
			if (Project is DotNetProject dnp) {
				if (Project.HasFlavor<DotNetCoreProjectExtension> () && dnp.TargetFramework.IsNetCoreApp())
					return executionContext.CanExecute (new DotNetCoreExecutionCommand (Path.GetDirectoryName (assemblyFileName), assemblyFileName, ""));
				else
					return executionContext.CanExecute (new DotNetExecutionCommand ());
			}
			return true;
		}

		protected override async void OnCreateTests ()
		{
			try {
				AddOldTests ();
				Status = TestStatus.Loading;

				var discoveredTests = await VsTestDiscoveryAdapter.Instance.DiscoverTestsAsync (Project);

				var tests = discoveredTests.BuildTestInfo (this);

				Status = TestStatus.Ready;

				Tests.Clear ();

				foreach (UnitTest test in tests) {
					Tests.Add (test);
				}

				OnTestChanged ();
			} catch (Exception e) {
				LoggingService.LogError ("Failed to discover unit tests.", e);
				Status = TestStatus.LoadError;
			}
		}

		string GetAssemblyFileName ()
		{
			return Project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
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
			base.Dispose ();
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
				oldTests = new UnitTest [Tests.Count];
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
			return RunTest (testContext, this).Result;
		}

		public async Task<UnitTestResult> RunTest (
			TestContext testContext,
			IVsTestTestProvider testProvider)
		{
			var result = await VsTestRunAdapter.Instance.RunTests (this, testContext, testProvider);
			Status = TestStatus.Ready;
			return result;
		}

		public IEnumerable<TestCase> GetTests ()
		{
			return null;
		}
	}
}
