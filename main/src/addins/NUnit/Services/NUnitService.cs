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
using System.IO;
using System.Collections;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using Mono.Addins;
using MonoDevelop.Projects;
using NUnit.Core;
using MonoDevelop.Ide.Gui;
using GuiServices = MonoDevelop.Core.Gui.Services;

namespace MonoDevelop.NUnit
{
	public class NUnitService : AbstractService
	{
		ArrayList providers = new ArrayList ();
		UnitTest rootTest;
		
		public override void InitializeService ()
		{
			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) DispatchService.GuiDispatch (new CombineEventHandler (OnOpenCombine));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) DispatchService.GuiDispatch (new CombineEventHandler (OnCloseCombine));
			IdeApp.ProjectOperations.ReferenceAddedToProject += new ProjectReferenceEventHandler (OnReferenceAddedToProject);
			IdeApp.ProjectOperations.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (OnReferenceRemovedFromProject);
			
			IProjectService ps = MonoDevelop.Projects.Services.ProjectService;
			ps.DataContext.IncludeType (typeof(UnitTestOptionsSet));
			ps.DataContext.RegisterProperty (typeof(AbstractConfiguration), "UnitTestInformation", typeof(UnitTestOptionsSet));
			
			Mono.Addins.AddinManager.AddExtensionNodeHandler ("/MonoDevelop/NUnit/TestProviders", OnExtensionChange);
			
			if (IdeApp.ProjectOperations.CurrentOpenCombine != null) {
				rootTest = BuildTest (IdeApp.ProjectOperations.CurrentOpenCombine);
				NotifyTestSuiteChanged ();
			}
		}
		
		void OnExtensionChange (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				IProjectService ps = MonoDevelop.Projects.Services.ProjectService;
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
		
		public IAsyncOperation RunTest (UnitTest test)
		{
			Pad resultsPad = IdeApp.Workbench.GetPad <TestResultsPad>();
			if (resultsPad == null) {
				resultsPad = IdeApp.Workbench.ShowPad (new TestResultsPad (), "MonoDevelop.NUnit.TestResultsPad", GettextCatalog.GetString ("Test results"), "Bottom", "md-solution");
			}
			
			resultsPad.BringToFront ();
			TestSession session = new TestSession (test, (TestResultsPad) resultsPad.Content);
			session.Start ();
			return session;
		}
		
		public UnitTest SearchTest (string fullName)
		{
			return SearchTest (rootTest, fullName);
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
		
		protected virtual void OnOpenCombine (object sender, CombineEventArgs e)
		{
			rootTest = BuildTest (e.Combine);
			NotifyTestSuiteChanged ();
		}

		protected virtual void OnCloseCombine (object sender, CombineEventArgs e)
		{
			if (rootTest != null) {
				((IDisposable)rootTest).Dispose ();
				rootTest = null;
			}
			NotifyTestSuiteChanged ();
		}
		
		void OnReferenceAddedToProject (object sender, ProjectReferenceEventArgs e)
		{
			RebuildTests ();
		}
		
		void OnReferenceRemovedFromProject (object sender, ProjectReferenceEventArgs e)
		{
			RebuildTests ();
		}
		
		void RebuildTests ()
		{
			if (rootTest != null)
				((IDisposable)rootTest).Dispose ();
				
			rootTest = BuildTest (IdeApp.ProjectOperations.CurrentOpenCombine);

			NotifyTestSuiteChanged ();
		}
		
		public UnitTest BuildTest (CombineEntry entry)
		{
			foreach (ITestProvider p in providers) {
				UnitTest t = p.CreateUnitTest (entry);
				if (t != null) return t;
			}
			return null;
		}
		
		public UnitTest RootTest {
			get { return rootTest; }
		}
		
		public static void ShowOptionsDialog (UnitTest test)
		{
			UnitTestOptionsDialog optionsDialog = new UnitTestOptionsDialog (IdeApp.Workbench.RootWindow, test);
			optionsDialog.Run ();
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
		
		public TestSession (UnitTest test, TestResultsPad resultsPad)
		{
			this.test = test;
			this.monitor = new TestMonitor (resultsPad);
		}
		
		public void Start ()
		{
			runThread = new Thread (new ThreadStart (RunTests));
			runThread.IsBackground = true;
			runThread.Start ();
		}
		
		void RunTests ()
		{
			try {
				ResetResult (test);
				monitor.InitializeTestRun (test);
				TestContext ctx = new TestContext (monitor, DateTime.Now);
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
		
		void ResetResult (UnitTest test)
		{
			test.ResetLastResult ();
			UnitTestGroup group = test as UnitTestGroup;
			if (group == null) return;
			foreach (UnitTest t in group.Tests)
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

		public event OperationHandler Completed;
		
		public event TestHandler CancelRequested {
			add { monitor.CancelRequested += value; }
			remove { monitor.CancelRequested -= value; }
		}
	}
}

