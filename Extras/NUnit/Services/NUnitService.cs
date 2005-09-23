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
using System.Threading;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Internal.Project;
using NUnit.Core;

namespace MonoDevelop.NUnit
{
	public class NUnitService : AbstractService
	{
		ArrayList providers = new ArrayList ();
		UnitTest rootTest;
		TestResultsPad resultsPad;
		
		public NUnitService ()
		{
		}
		
		public override void InitializeService ()
		{
			RegisterTestProvider (new SystemTestProvider ());
			Runtime.ProjectService.CombineOpened += new CombineEventHandler (OnOpenCombine);
			Runtime.ProjectService.CombineClosed += new CombineEventHandler (OnCloseCombine);
			
			Runtime.ProjectService.DataContext.IncludeType (typeof(UnitTestOptionsSet));
			Runtime.ProjectService.DataContext.RegisterProperty (typeof(AbstractConfiguration), "UnitTestInformation", typeof(UnitTestOptionsSet));
		}
		
		public IAsyncOperation RunTest (UnitTest test)
		{
			if (resultsPad == null) {
				resultsPad = new TestResultsPad ();
				Runtime.Gui.Workbench.ShowPad (resultsPad);
			}
			
			Runtime.Gui.Workbench.BringToFront (resultsPad);
			TestSession session = new TestSession (test, resultsPad);
			session.Start ();
			return session;
		}
		
		
		protected virtual void OnOpenCombine (object sender, CombineEventArgs e)
		{
			rootTest = BuildTest (e.Combine);
			e.Combine.ReferenceAddedToProject += new ProjectReferenceEventHandler (OnReferenceAddedToProject);
			e.Combine.ReferenceRemovedFromProject += new ProjectReferenceEventHandler (OnReferenceRemovedFromProject);
			
			if (TestSuiteChanged != null)
				TestSuiteChanged (this, EventArgs.Empty);
		}

		protected virtual void OnCloseCombine (object sender, CombineEventArgs e)
		{
			e.Combine.ReferenceAddedToProject -= new ProjectReferenceEventHandler (OnReferenceAddedToProject);
			e.Combine.ReferenceRemovedFromProject -= new ProjectReferenceEventHandler (OnReferenceRemovedFromProject);
			
			if (rootTest != null) {
				((IDisposable)rootTest).Dispose ();
				rootTest = null;
			}
			if (TestSuiteChanged != null)
				TestSuiteChanged (this, EventArgs.Empty);
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
				
			rootTest = BuildTest (Runtime.ProjectService.CurrentOpenCombine);

			if (TestSuiteChanged != null)
				TestSuiteChanged (this, EventArgs.Empty);
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
		
		public void RegisterTestProvider (ITestProvider provider)
		{
			providers.Add (provider);
			Type[] types = provider.GetOptionTypes ();
			if (types != null) {
				foreach (Type t in types) {
					if (!typeof(ICloneable).IsAssignableFrom (t))
						throw new InvalidOperationException ("Option types must implement ICloneable: " + t);
					Runtime.ProjectService.DataContext.IncludeType (t);
				}
			}
		}
		
		public static void ShowOptionsDialog (UnitTest test)
		{
			UnitTestOptionsDialog optionsDialog = new UnitTestOptionsDialog ((Gtk.Window)WorkbenchSingleton.Workbench, test);
			optionsDialog.Run ();
		}
		
		public event EventHandler TestSuiteChanged;
	}
	
	
	class TestSession: IAsyncOperation, ITestProgressMonitor
	{
		UnitTest test;
		ITestProgressMonitor monitor;
		TestResultsPad resultsPad;
		Thread runThread;
		bool success;
		ManualResetEvent waitEvent;
		
		public TestSession (UnitTest test, TestResultsPad resultsPad)
		{
			this.test = test;
			this.monitor = resultsPad;
			this.resultsPad = resultsPad;
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
				resultsPad.InitializeTestRun (test);
				TestContext ctx = new TestContext (monitor, DateTime.Now);
				test.Run (ctx);
				test.SaveResults ();
				success = true;
			} catch (Exception ex) {
				Console.WriteLine (ex);
				resultsPad.ReportRuntimeError (null, ex);
				success = false;
			} finally {
				resultsPad.FinishTestRun ();
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
			resultsPad.Cancel ();
		}
		
		public void WaitForCompleted ()
		{
			if (IsCompleted) return;
			
			if (Runtime.DispatchService.IsGuiThread) {
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

