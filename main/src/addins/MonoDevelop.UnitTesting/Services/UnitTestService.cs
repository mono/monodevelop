//
// NUnitService.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using System.IO;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement;

namespace MonoDevelop.UnitTesting
{
	public static class UnitTestService
	{
		static ArrayList providers = new ArrayList ();
		static UnitTest[] rootTests = Array.Empty<UnitTest> ();
		static bool packageEventsInitialized;

		static UnitTestService ()
		{
			IdeApp.Workspace.WorkspaceItemOpened += OnWorkspaceChanged;
			IdeApp.Workspace.WorkspaceItemClosed += OnWorkspaceChanged;
			IdeApp.Workspace.ActiveConfigurationChanged += OnWorkspaceChanged;

			IdeApp.Workspace.ItemAddedToSolution += OnItemsChangedInSolution;
			IdeApp.Workspace.ItemRemovedFromSolution += OnItemsChangedInSolution;
			IdeApp.Workspace.ReferenceAddedToProject += OnReferenceChangedInProject;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferenceChangedInProject;
			
			IdeApp.Workspace.FirstWorkspaceItemOpened += (s,a) => {
				if (!packageEventsInitialized) {
					packageEventsInitialized = true;
					PackageManagementServices.ProjectOperations.PackageReferenceAdded += ProjectOperations_PackageReferencesModified;
					PackageManagementServices.ProjectOperations.PackageReferenceRemoved += ProjectOperations_PackageReferencesModified;
					PackageManagementServices.ProjectOperations.PackagesRestored += ProjectOperations_PackageReferencesModified;
				}
			};
			
			Mono.Addins.AddinManager.AddExtensionNodeHandler ("/MonoDevelop/UnitTesting/TestProviders", OnExtensionChange);

			RebuildTests ();
		}

		static void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				ProjectService ps = MonoDevelop.Projects.Services.ProjectService;
				ITestProvider provider = args.ExtensionObject as ITestProvider;
				providers.Add (provider);
			}
			else {
				ITestProvider provider = args.ExtensionObject as ITestProvider;
				providers.Remove (provider);
				provider.Dispose ();
			}
		}

		public static UnitTest CurrentSelectedTest {
			get {
				var pad = IdeApp.Workbench.GetPad<TestPad> ();
				pad.BringToFront ();
				TestPad testPad = (TestPad)pad.Content;
				ITreeNavigator nav = testPad.TreeView.GetSelectedNode ();
				if (nav != null)
					return nav.DataItem as UnitTest;
				return null;
			}
			set {
				var pad = IdeApp.Workbench.GetPad<TestPad> ();
				pad.BringToFront ();
				var content = (TestPad)pad.Content;
				content.SelectTest (value);
			}
		}

		public static AsyncOperation RunTest (UnitTest test, MonoDevelop.Projects.ExecutionContext context)
		{
			var result = RunTest (test, context, true);
			result.Task.ContinueWith (t => OnTestSessionCompleted (), TaskScheduler.FromCurrentSynchronizationContext ());
			return result;
		}

		public static AsyncOperation RunTest (UnitTest test, MonoDevelop.Projects.ExecutionContext context, bool buildOwnerObject)
		{
			var cs = new CancellationTokenSource ();
			return new AsyncOperation (RunTests (new UnitTest [] { test }, context, buildOwnerObject, true, cs), cs);
		}

		internal static Task RunTest (UnitTest test, MonoDevelop.Projects.ExecutionContext context, bool buildOwnerObject, bool checkCurrentRunOperation, CancellationTokenSource cs)
		{
			return RunTests (new UnitTest [] { test }, context, buildOwnerObject, checkCurrentRunOperation, cs);
		}

		internal static async Task RunTests (IEnumerable<UnitTest> tests, MonoDevelop.Projects.ExecutionContext context, bool buildOwnerObject, bool checkCurrentRunOperation, CancellationTokenSource cs)
		{
			if (buildOwnerObject) {
				var build_targets = new HashSet<IBuildTarget> ();
				foreach (var t in tests) {
					if (t.OwnerObject is IBuildTarget bt)
						build_targets.Add (bt);
				}
				if (IdeApp.Preferences.BuildBeforeRunningTests) {
					var res = await IdeApp.ProjectOperations.CheckAndBuildForExecute (
						build_targets, IdeApp.Workspace.ActiveConfiguration, true,
						false, null, cs.Token);

					if (!res)
						return;
				}

				var test_names = new HashSet<string> (tests.Select ((v) => v.FullName));

				await RefreshTests (cs.Token);

				tests = test_names.Select ((fullName) => SearchTest (fullName)).Where ((t) => t != null).ToList ();

				if (tests.Any ())
					await RunTests (tests, context, false, checkCurrentRunOperation, cs);
				return;
			}

			if (checkCurrentRunOperation && !IdeApp.ProjectOperations.ConfirmExecutionOperation ())
				return;

			Pad resultsPad = GetTestResultsPad ();

			var test = tests.Count () == 1 ? tests.First () : new UnitTestSelection (tests, tests.First ().OwnerObject);
			TestSession session = new TestSession (test, context, (TestResultsPad) resultsPad.Content, cs);

			OnTestSessionStarting (new TestSessionEventArgs { Session = session, Test = test });

			if (checkCurrentRunOperation)
				IdeApp.ProjectOperations.AddRunOperation (session);

			try {
				await session.Start ();
			} finally {
				resultsPad.Sticky = false;
			}
		}

		public static AsyncOperation RunTests (IEnumerable<UnitTest> tests, MonoDevelop.Projects.ExecutionContext context)
		{
			var result = RunTests (tests, context, true);
			result.Task.ContinueWith (t => OnTestSessionCompleted (), TaskScheduler.FromCurrentSynchronizationContext ());
			return result;
		}

		public static AsyncOperation RunTests (IEnumerable<UnitTest> tests, MonoDevelop.Projects.ExecutionContext context, bool buildOwnerObject)
		{
			var cs = new CancellationTokenSource ();
			return new AsyncOperation (RunTests (tests, context, buildOwnerObject, true, cs), cs);
		}

		/// <summary>
		/// For each node already in the test tree, it checks if there is any change. If there is, it reloads the test.
		/// </summary>
		public static Task RefreshTests (CancellationToken ct)
		{
			return Task.WhenAll (RootTests.Select (t => t.Refresh (ct)));
		}

		/// <summary>
		/// Reloads the test tree, creating new test branches if necessary
		/// </summary>
		public static void ReloadTests ()
		{
			foreach (var t in RootTests.OfType<UnitTestGroup> ())
				t.UpdateTests ();
		}

		public static void ReportExecutionError (string message)
		{
			Pad resultsPad = GetTestResultsPad ();
			var monitor = (TestResultsPad)resultsPad.Content;
			monitor.InitializeTestRun (null, null);
			monitor.ReportExecutionError (message);
			monitor.FinishTestRun ();
		}

		static Pad GetTestResultsPad ()
		{
			Pad resultsPad = IdeApp.Workbench.GetPad<TestResultsPad> ();
			if (resultsPad == null) {
				resultsPad = IdeApp.Workbench.ShowPad (new TestResultsPad (), "MonoDevelop.UnitTesting.TestResultsPad", GettextCatalog.GetString ("Test results"), "Bottom", "md-solution");
			}

			// Make the pad sticky while the tests are runnig, so the results pad is always visible (even if minimized)
			// That's required since when running in debug mode, the layout is automatically switched to debug.

			resultsPad.Sticky = true;
			resultsPad.BringToFront ();
			return resultsPad;
		}

		public static UnitTest SearchTest (string fullName)
		{
			foreach (UnitTest t in RootTests) {
				UnitTest r = SearchTest (t, fullName);
				if (r != null)
					return r;
			}
			return null;
		}

		public static UnitTest SearchTestById (string id)
		{
			foreach (UnitTest t in RootTests) {
				UnitTest r = SearchTestById (t, id);
				if (r != null)
					return r;
			}
			return null;
		}

		public static UnitTest SearchTestByDocumentId (string id)
		{
			foreach (UnitTest t in RootTests) {
				UnitTest r = SearchTestByDocumentId (t, id);
				if (r != null)
					return r;
			}
			return null;
		}


		static UnitTest SearchTest (UnitTest test, string fullName)
		{
			if (test == null)
				return null;
			if (test.FullName == fullName)
				return test;

			UnitTestGroup group = test as UnitTestGroup;
			if (group != null)  {
				foreach (UnitTest t in group.Tests) {
					UnitTest result = SearchTest (t, fullName);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		static UnitTest SearchTestById (UnitTest test, string id)
		{
			if (test == null)
				return null;
			if (test.TestId == id)
				return test;

			UnitTestGroup group = test as UnitTestGroup;
			if (group != null) {
				foreach (UnitTest t in group.Tests) {
					UnitTest result = SearchTestById (t, id);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		static UnitTest SearchTestByDocumentId (UnitTest test, string id)
		{
			if (test == null)
				return null;
			if (test.TestSourceCodeDocumentId == id)
				return test;

			UnitTestGroup group = test as UnitTestGroup;
			if (group != null) {
				foreach (UnitTest t in group.Tests) {
					UnitTest result = SearchTestByDocumentId (t, id);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		public static UnitTest FindRootTest (WorkspaceObject item)
		{
			return FindRootTest (RootTests, item);
		}

		public static UnitTest FindRootTest (IEnumerable<UnitTest> tests, WorkspaceObject item)
		{
			foreach (UnitTest t in tests) {
				if (t.OwnerObject == item)
					return t;
				UnitTestGroup tg = t as UnitTestGroup;
				if (tg != null) {
					UnitTest ct = FindRootTest (tg.Tests, item);
					if (ct != null)
						return ct;
				}
			}
			return null;
		}

		static void OnWorkspaceChanged (object sender, EventArgs e)
		{
			RebuildTests ();
		}

		static void OnReferenceChangedInProject (object sender, ProjectReferenceEventArgs e)
		{
			if (!IsSolutionGroupPresent (e.Project.ParentSolution, rootTests))
				RebuildTests ();
		}

		static void OnItemsChangedInSolution (object sender, SolutionItemChangeEventArgs e)
		{
			if (!IsSolutionGroupPresent (e.Solution, rootTests))
				RebuildTests ();
		}

		static CancellationTokenSource throttling = new CancellationTokenSource ();

		static void ProjectOperations_PackageReferencesModified(object sender, EventArgs e)
		{
			RebuildTests ();
		}

		static bool IsSolutionGroupPresent (Solution sol, IEnumerable<UnitTest> tests)
		{
			foreach (var t in tests) {
				var tg = t as SolutionFolderTestGroup;
				if (tg != null && ((SolutionFolder)tg.OwnerObject).ParentSolution == sol)
					return true;
				var g = t as UnitTestGroup;
				if (g != null && g.HasTests) {
					if (IsSolutionGroupPresent (sol, g.Tests))
						return true;
				}
			}
			return false;
		}


		static CancellationTokenSource rebuildTestsCts = new CancellationTokenSource ();
		const int ThrottlingTimeout = 2000;

		static async void RebuildTests ()
		{
			throttling.Cancel ();
			throttling = new CancellationTokenSource ();
			try {
				await Task.Delay (ThrottlingTimeout, throttling.Token);
				if (throttling.Token.IsCancellationRequested)
					return;

				if (rootTests != null) {
					foreach (IDisposable t in rootTests)
						t.Dispose ();
				}
				List<UnitTest> list = new List<UnitTest> ();
				rebuildTestsCts.Cancel ();
				rebuildTestsCts = new CancellationTokenSource ();
				var token = rebuildTestsCts.Token;
				var items = IdeApp.Workspace.Items.ToArray ();
				await Task.Run (() => {
					foreach (WorkspaceItem it in items) {
						if (token.IsCancellationRequested)
							return;
						UnitTest t = BuildTest (it);
						if (t != null)
							list.Add (t);
					}
				}, token);
				if (token.IsCancellationRequested)
					return;
				rootTests = list.ToArray ();
				NotifyTestSuiteChanged ();
			} catch (OperationCanceledException) {
			} catch (Exception ex) {
				LoggingService.LogError ("Exception gathering unit tests.", ex);
			}
		}

		public static UnitTest BuildTest (WorkspaceObject entry)
		{
			foreach (ITestProvider p in providers) {
				try {
					UnitTest t = p.CreateUnitTest (entry);
					if (t != null)
						return t;
				} catch {
				}
			}
			return null;
		}

		public static string GetTestResultsDirectory (string baseDirectory)
		{
			var newCache = IdeApp.TypeSystemService.GetCacheDirectory (baseDirectory, false);
			if (newCache == null) {
				newCache = IdeApp.TypeSystemService.GetCacheDirectory (baseDirectory, true);
				var oldDirectory = Path.Combine (baseDirectory, "test-results");
				var newDirectory = Path.Combine (newCache, "test-results");
				try {
					Directory.CreateDirectory (newDirectory);
					if (Directory.Exists (oldDirectory)) {
						foreach (string file in Directory.GetFiles(oldDirectory, "*.*"))
							File.Copy (file, file.Replace (oldDirectory, newDirectory));
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while copying old test-results", e);
				}
				return newDirectory;
			}

			return Path.Combine (newCache, "test-results");
		}


		public static UnitTest[] RootTests {
			get { return rootTests; }
		}

		static void NotifyTestSuiteChanged ()
		{
			Runtime.RunInMainThread (() => {
				if (TestSuiteChanged != null)
					TestSuiteChanged (null, EventArgs.Empty);
			});
		}

		public static void ResetResult (UnitTest test)
		{
			test?.ResetLastResult ();
		}

		public static event EventHandler TestSuiteChanged;

		static void OnTestSessionCompleted ()
		{
			var handler = TestSessionCompleted;
			if (handler != null)
				handler (null, EventArgs.Empty);
		}

		public static event EventHandler TestSessionCompleted;

		static void OnTestSessionStarting (TestSessionEventArgs args)
		{
			if (TestSessionStarting != null)
				TestSessionStarting (null, args);
		}

		/// <summary>
		/// Occurs just before a test session is started
		/// </summary>
		public static event EventHandler<TestSessionEventArgs> TestSessionStarting;
	}



	class TestSession: AsyncOperation
	{
		UnitTest test;
		TestMonitor monitor;
		MonoDevelop.Projects.ExecutionContext context;
		TestResultsPad resultsPad;

		public TestSession (UnitTest test, MonoDevelop.Projects.ExecutionContext context, TestResultsPad resultsPad, CancellationTokenSource cs)
		{
			this.test = test;
			if (context != null)
				this.context = new Projects.ExecutionContext (context.ExecutionHandler, new CustomConsoleFactory (context.ConsoleFactory, cs), context.ExecutionTarget);
			CancellationTokenSource = cs;
			this.monitor = new TestMonitor (resultsPad, CancellationTokenSource);
			this.resultsPad = resultsPad;
			resultsPad.InitializeTestRun (test, cs);
			Task = new Task ((Action)RunTests);
		}

		public Task Start ()
		{
			Task.Start ();
			return Task;
		}

		void RunTests ()
		{
			try {
				UnitTestService.ResetResult (test);

				TestContext ctx = new TestContext (monitor, context, DateTime.Now);
				test.Run (ctx);
				test.SaveResults ();
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				monitor.ReportRuntimeError (null, ex);
			} finally {
				monitor.FinishTestRun ();
			}
		}
	}

	public class TestSessionEventArgs: EventArgs
	{
		public AsyncOperation Session { get; set; }
		public UnitTest Test { get; set; }
	}

	class CustomConsoleFactory : OperationConsoleFactory
	{
		OperationConsoleFactory factory;
		CancellationTokenSource cancelSource;

		public CustomConsoleFactory (OperationConsoleFactory factory, CancellationTokenSource cs)
		{
			this.factory = factory;
			cancelSource = cs;
		}

		protected override OperationConsole OnCreateConsole (CreateConsoleOptions options)
		{
			return factory.CreateConsole (options.WithBringToFront (false)).WithCancelCallback (cancelSource.Cancel);
		}
	}
}

