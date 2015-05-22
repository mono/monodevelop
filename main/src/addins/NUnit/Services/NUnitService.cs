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

namespace MonoDevelop.NUnit
{
	public class NUnitService
	{
		static NUnitService instance;
		
		ArrayList providers = new ArrayList ();
		UnitTest[] rootTests;
		
		private NUnitService ()
		{
			IdeApp.Workspace.WorkspaceItemOpened += OnWorkspaceChanged;
			IdeApp.Workspace.WorkspaceItemClosed += OnWorkspaceChanged;
			IdeApp.Workspace.ActiveConfigurationChanged += OnWorkspaceChanged;

			IdeApp.Workspace.ItemAddedToSolution += OnItemsChangedInSolution;;
			IdeApp.Workspace.ItemRemovedFromSolution += OnItemsChangedInSolution;
			IdeApp.Workspace.ReferenceAddedToProject += OnReferenceChangedInProject;;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferenceChangedInProject;

			Mono.Addins.AddinManager.AddExtensionNodeHandler ("/MonoDevelop/NUnit/TestProviders", OnExtensionChange);
		}

		public static NUnitService Instance {
			get {
				if (instance == null) {
					instance = new NUnitService ();
					instance.RebuildTests ();
				}
				return instance;
			}
		}

		void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				ProjectService ps = MonoDevelop.Projects.Services.ProjectService;
				ITestProvider provider = args.ExtensionObject as ITestProvider;
				providers.Add (provider);
			}
			else {
				ITestProvider provider = args.ExtensionObject as ITestProvider;
				providers.Remove (provider);
				
				// The types returned by provider.GetOptionTypes should probably be unregistered
				// from the DataContext, but DataContext does not allow unregisterig.
				// This is not a big issue anyway.
			}
		}
		
		public AsyncOperation RunTest (UnitTest test, IExecutionHandler context)
		{
			var result = RunTest (test, context, IdeApp.Preferences.BuildBeforeRunningTests);
			result.Task.ContinueWith (t => OnTestSessionCompleted (), TaskScheduler.FromCurrentSynchronizationContext ());
			return result;
		}
		
		public AsyncOperation RunTest (UnitTest test, IExecutionHandler context, bool buildOwnerObject)
		{
			var cs = new CancellationTokenSource ();
			return new AsyncOperation (RunTest (test, context, buildOwnerObject, true, cs), cs);
		}

		internal async Task RunTest (UnitTest test, IExecutionHandler context, bool buildOwnerObject, bool checkCurrentRunOperation, CancellationTokenSource cs)
		{
			string testName = test.FullName;
			
			if (buildOwnerObject) {
				IBuildTarget bt = test.OwnerObject as IBuildTarget;
				if (bt != null && bt.NeedsBuilding (IdeApp.Workspace.ActiveConfiguration)) {
					if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
						MonoDevelop.Ide.Commands.StopHandler.StopBuildOperations ();
						await IdeApp.ProjectOperations.CurrentRunOperation.Task;
					}
	
					var res = await IdeApp.ProjectOperations.Build (bt, cs.Token).Task;
					if (res.HasErrors)
						return;

					await RefreshTests (cs.Token);
					test = SearchTest (testName);
					if (test != null)
						await RunTest (test, context, false, checkCurrentRunOperation, cs);
					return;
				}
			}
			
			if (checkCurrentRunOperation && !IdeApp.ProjectOperations.ConfirmExecutionOperation ())
				return;
			
			Pad resultsPad = IdeApp.Workbench.GetPad <TestResultsPad>();
			if (resultsPad == null) {
				resultsPad = IdeApp.Workbench.ShowPad (new TestResultsPad (), "MonoDevelop.NUnit.TestResultsPad", GettextCatalog.GetString ("Test results"), "Bottom", "md-solution");
			}
			
			// Make the pad sticky while the tests are runnig, so the results pad is always visible (even if minimized)
			// That's required since when running in debug mode, the layout is automatically switched to debug.
			
			resultsPad.Sticky = true;
			resultsPad.BringToFront ();
			
			TestSession session = new TestSession (test, context, (TestResultsPad) resultsPad.Content, cs);
			
			OnTestSessionStarting (new TestSessionEventArgs { Session = session, Test = test });

			try {
				await session.Start ();
			} finally {
				resultsPad.Sticky = false;
			}

			if (checkCurrentRunOperation)
				IdeApp.ProjectOperations.CurrentRunOperation = session;
		}
		
		public Task RefreshTests (CancellationToken ct)
		{
			return Task.WhenAll (RootTests.Select (t => t.Refresh (ct)));
		}
		
		public UnitTest SearchTest (string fullName)
		{
			foreach (UnitTest t in RootTests) {
				UnitTest r = SearchTest (t, fullName);
				if (r != null)
					return r;
			}
			return null;
		}

		public UnitTest SearchTestById (string id)
		{
			foreach (UnitTest t in RootTests) {
				UnitTest r = SearchTestById (t, id);
				if (r != null)
					return r;
			}
			return null;
		}

		
		UnitTest SearchTest (UnitTest test, string fullName)
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

		UnitTest SearchTestById (UnitTest test, string id)
		{
			if (test == null)
				return null;
			if (test.TestId == id)
				return test;

			UnitTestGroup group = test as UnitTestGroup;
			if (group != null)  {
				foreach (UnitTest t in group.Tests) {
					UnitTest result = SearchTestById (t, id);
					if (result != null)
						return result;
				}
			}
			return null;
		}

		public UnitTest FindRootTest (WorkspaceObject item)
		{
			return FindRootTest (RootTests, item);
		}
		
		public UnitTest FindRootTest (IEnumerable<UnitTest> tests, WorkspaceObject item)
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
		
		void OnWorkspaceChanged (object sender, EventArgs e)
		{
			RebuildTests ();
		}

		void OnReferenceChangedInProject (object sender, ProjectReferenceEventArgs e)
		{
			if (!IsSolutionGroupPresent (e.Project.ParentSolution, rootTests))
				RebuildTests ();
		}

		void OnItemsChangedInSolution (object sender, SolutionItemChangeEventArgs e)
		{
			if (!IsSolutionGroupPresent (e.Solution, rootTests))
				RebuildTests ();
		}

		bool IsSolutionGroupPresent (Solution sol, IEnumerable<UnitTest> tests)
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

		void RebuildTests ()
		{
			if (rootTests != null) {
				foreach (IDisposable t in rootTests)
					t.Dispose ();
			}

			List<UnitTest> list = new List<UnitTest> ();
			foreach (WorkspaceItem it in IdeApp.Workspace.Items) {
				UnitTest t = BuildTest (it);
				if (t != null)
					list.Add (t);
			}

			rootTests = list.ToArray ();
			NotifyTestSuiteChanged ();
		}
		
		public UnitTest BuildTest (WorkspaceObject entry)
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
		
		public UnitTest[] RootTests {
			get { return rootTests; }
		}
		
		public static void ShowOptionsDialog (UnitTest test)
		{
			Properties properties = new Properties ();
			properties.Set ("UnitTest", test);
			MessageService.ShowCustomDialog (new UnitTestOptionsDialog (IdeApp.Workbench.RootWindow, properties));
		}
		
		void NotifyTestSuiteChanged ()
		{
			Runtime.RunInMainThread (() => {
				if (TestSuiteChanged != null)
					TestSuiteChanged (this, EventArgs.Empty);
			});
		}

		public static void ResetResult (UnitTest test)
		{
			if (test == null)
				return;
			test.ResetLastResult ();
			UnitTestGroup group = test as UnitTestGroup;
			if (group == null) 
				return;
			foreach (UnitTest t in new List<UnitTest> (group.Tests))
				ResetResult (t);
		}

		public event EventHandler TestSuiteChanged;

		void OnTestSessionCompleted ()
		{
			var handler = TestSessionCompleted;
			if (handler != null)
				handler (this, EventArgs.Empty);
		}

		public event EventHandler TestSessionCompleted;

		void OnTestSessionStarting (TestSessionEventArgs args)
		{
			if (TestSessionStarting != null)
				TestSessionStarting (this, args);
		}

		/// <summary>
		/// Occurs just before a test session is started
		/// </summary>
		public event EventHandler<TestSessionEventArgs> TestSessionStarting;
	}
	


	class TestSession: AsyncOperation, ITestProgressMonitor
	{
		UnitTest test;
		TestMonitor monitor;
		IExecutionHandler context;
		TestResultsPad resultsPad;

		public TestSession (UnitTest test, IExecutionHandler context, TestResultsPad resultsPad, CancellationTokenSource cs)
		{
			this.test = test;
			this.context = context;
			CancellationTokenSource = cs;
			this.monitor = new TestMonitor (resultsPad, CancellationTokenSource);
			this.resultsPad = resultsPad;
			resultsPad.InitializeTestRun (test);
			Task = new Task (RunTests);
		}
		
		public Task Start ()
		{
			Task.Start ();
			return Task;
		}
		
		void RunTests ()
		{
			try {
				NUnitService.ResetResult (test);

				TestContext ctx = new TestContext (monitor, resultsPad, context, DateTime.Now);
				test.Run (ctx);
				test.SaveResults ();
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				monitor.ReportRuntimeError (null, ex);
			} finally {
				monitor.FinishTestRun ();
			}
		}
		
		void ITestProgressMonitor.BeginTest (UnitTest test)
		{
			monitor.BeginTest (test);
		}
		
		void ITestProgressMonitor.EndTest (UnitTest test, UnitTestResult result)
		{
			monitor.EndTest (test, result);
		}
		
		void ITestProgressMonitor.ReportRuntimeError (string message, Exception exception)
		{
			monitor.ReportRuntimeError (message, exception);
		}
		
		void ITestProgressMonitor.WriteGlobalLog (string message)
		{
			monitor.WriteGlobalLog (message);
		}

		bool ITestProgressMonitor.IsCancelRequested {
			get { return monitor.IsCancelRequested; }
		}

		public event TestHandler CancelRequested {
			add { monitor.CancelRequested += value; }
			remove { monitor.CancelRequested -= value; }
		}
	}

	public class TestSessionEventArgs: EventArgs
	{
		public AsyncOperation Session { get; set; }
		public UnitTest Test { get; set; }
	}
}

