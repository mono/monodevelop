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
using NUnit.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;

namespace MonoDevelop.NUnit
{
	public class NUnitService
	{
		static NUnitService instance;
		
		ArrayList providers = new ArrayList ();
		UnitTest[] rootTests;
		
		private NUnitService ()
		{
			IdeApp.Workspace.ReferenceAddedToProject += OnWorkspaceChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnWorkspaceChanged;
			IdeApp.Workspace.WorkspaceItemOpened += OnWorkspaceChanged;
			IdeApp.Workspace.WorkspaceItemClosed += OnWorkspaceChanged;
			IdeApp.Workspace.ItemAddedToSolution += OnWorkspaceChanged;
			IdeApp.Workspace.ItemRemovedFromSolution += OnWorkspaceChanged;

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
				
				Type[] types = provider.GetOptionTypes ();
				if (types != null) {
					foreach (Type t in types) {
						if (!typeof(ICloneable).IsAssignableFrom (t)) {
							LoggingService.LogError ("Option types must implement ICloneable: " + t);
							continue;
						}
						ps.DataContext.IncludeType (t);
					}
				}
			}
			else {
				ITestProvider provider = args.ExtensionObject as ITestProvider;
				providers.Remove (provider);
				
				// The types returned by provider.GetOptionTypes should probably be unregistered
				// from the DataContext, but DataContext does not allow unregisterig.
				// This is not a big issue anyway.
			}
		}
		
		public IAsyncOperation RunTest (UnitTest test, IExecutionHandler context)
		{
			return RunTest (test, context, IdeApp.Preferences.BuildBeforeExecuting);
		}
		
		public IAsyncOperation RunTest (UnitTest test, IExecutionHandler context, bool buildOwnerObject)
		{
			string testName = test.FullName;
			
			if (buildOwnerObject) {
				IBuildTarget bt = test.OwnerObject as IBuildTarget;
				if (bt != null && bt.NeedsBuilding (IdeApp.Workspace.ActiveConfiguration)) {
					if (!IdeApp.ProjectOperations.CurrentRunOperation.IsCompleted) {
						MonoDevelop.Ide.Commands.StopHandler.StopBuildOperations ();
						IdeApp.ProjectOperations.CurrentRunOperation.WaitForCompleted ();
					}
	
					AsyncOperation retOper = new AsyncOperation ();
					
					IAsyncOperation op = IdeApp.ProjectOperations.Build (bt);
					retOper.TrackOperation (op, false);
						
					op.Completed += delegate {
						// The completed event of the build operation is run in the gui thread,
						// so we need a new thread, because refreshing must be async
						System.Threading.ThreadPool.QueueUserWorkItem (delegate {
							if (op.Success) {
								RefreshTests ();
								test = SearchTest (testName);
								if (test != null) {
									Gtk.Application.Invoke (delegate {
										// RunTest must run in the gui thread
										retOper.TrackOperation (RunTest (test, context, false), true);
									});
								}
								else
									retOper.SetCompleted (false);
							}
						});
					};
					
					return retOper;
				}
			}
			
			if (!IdeApp.ProjectOperations.ConfirmExecutionOperation ())
				return NullProcessAsyncOperation.Failure;
			
			Pad resultsPad = IdeApp.Workbench.GetPad <TestResultsPad>();
			if (resultsPad == null) {
				resultsPad = IdeApp.Workbench.ShowPad (new TestResultsPad (), "MonoDevelop.NUnit.TestResultsPad", GettextCatalog.GetString ("Test results"), "Bottom", "md-solution");
			}
			
			// Make the pad sticky while the tests are runnig, so the results pad is always visible (even if minimized)
			// That's required since when running in debug mode, the layout is automatically switched to debug.
			
			resultsPad.Sticky = true;
			resultsPad.BringToFront ();
			
			TestSession session = new TestSession (test, context, (TestResultsPad) resultsPad.Content);
			
			session.Completed += delegate {
				Gtk.Application.Invoke (delegate {
					resultsPad.Sticky = false;
				});
			};
			
			session.Start ();
			
			IdeApp.ProjectOperations.CurrentRunOperation = session;
			
			return session;
		}
		
		public void RefreshTests ()
		{
			foreach (UnitTest t in RootTests)
				t.Refresh ().WaitForCompleted ();
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
		
		public UnitTest FindRootTest (IWorkspaceObject item)
		{
			return FindRootTest (RootTests, item);
		}
		
		public UnitTest FindRootTest (IEnumerable<UnitTest> tests, IWorkspaceObject item)
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
		
		public UnitTest BuildTest (IWorkspaceObject entry)
		{
			foreach (ITestProvider p in providers) {
				UnitTest t = p.CreateUnitTest (entry);
				if (t != null) return t;
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
			if (TestSuiteChanged != null)
				TestSuiteChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler TestSuiteChanged;
	}
	
	
	class TestSession: IAsyncOperation, ITestProgressMonitor
	{
		UnitTest test;
		TestMonitor monitor;
		Thread runThread;
		bool success;
		ManualResetEvent waitEvent;
		IExecutionHandler context;
		
		public TestSession (UnitTest test, IExecutionHandler context, TestResultsPad resultsPad)
		{
			this.test = test;
			this.context = context;
			this.monitor = new TestMonitor (resultsPad);
		}
		
		public void Start ()
		{
			runThread = new Thread (new ThreadStart (RunTests));
			runThread.Name = "NUnit test runner";
			runThread.IsBackground = true;
			runThread.Start ();
		}
		
		void RunTests ()
		{
			try {
				ResetResult (test);
				monitor.InitializeTestRun (test);
				TestContext ctx = new TestContext (monitor, context, DateTime.Now);
				test.Run (ctx);
				test.SaveResults ();
				success = true;
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				monitor.ReportRuntimeError (null, ex);
				success = false;
			} finally {
				monitor.FinishTestRun ();
				runThread = null;
			}
			lock (this) {
				if (waitEvent != null)
					waitEvent.Set ();
			}
			if (Completed != null)
				Completed (this);
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
		
		bool ITestProgressMonitor.IsCancelRequested {
			get { return monitor.IsCancelRequested; }
		}
		
		void IAsyncOperation.Cancel ()
		{
			monitor.Cancel ();
		}
		
		public void WaitForCompleted ()
		{
			if (IsCompleted) return;
			
			if (DispatchService.IsGuiThread) {
				while (!IsCompleted) {
					while (Gtk.Application.EventsPending ())
						Gtk.Application.RunIteration ();
					Thread.Sleep (100);
				}
			} else {
				lock (this) {
					if (waitEvent == null)
						waitEvent = new ManualResetEvent (false);
				}
				waitEvent.WaitOne ();
			}
		}
		
		public bool IsCompleted {
			get { return runThread == null; }
		}
		
		public bool Success {
			get { return success; }
		}

		public bool SuccessWithWarnings {
			get { return false; }
		}

		public event OperationHandler Completed;
		
		public event TestHandler CancelRequested {
			add { monitor.CancelRequested += value; }
			remove { monitor.CancelRequested -= value; }
		}
	}
}

