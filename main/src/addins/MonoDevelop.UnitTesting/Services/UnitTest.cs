//
// UnitTest.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.UnitTesting
{
	public abstract class UnitTest: IDisposable
	{
		string name;
		IResultsStore resultsStore;
		internal UnitTestResult lastResult;
		UnitTest parent;
		TestStatus status;
		WorkspaceObject ownerSolutionItem;
		SolutionItem ownerSolutionEntityItem;
		UnitTestResultsStore results;
		bool historicResult;
		bool resultLoaded;

		public virtual bool CanMergeWithParent => false;

		public string FixtureTypeNamespace {
			get;
			set;
		}
		
		public string FixtureTypeName {
			get;
			set;
		}

		public bool IsExplicit {
			get;
			set;
		}

		protected UnitTest (string name)
		{
			this.name = name;
		}
		
		protected UnitTest (string name, WorkspaceObject ownerSolutionItem)
		{
			this.name = name;
			this.ownerSolutionItem = ownerSolutionItem;
			ownerSolutionEntityItem = ownerSolutionItem as SolutionItem;
			if (ownerSolutionEntityItem != null)
				ownerSolutionEntityItem.DefaultConfigurationChanged += OnConfugurationChanged;
		}
		
		public virtual void Dispose ()
		{
			if (ownerSolutionEntityItem != null)
				ownerSolutionEntityItem.DefaultConfigurationChanged -= OnConfugurationChanged;
		}
		
		internal void SetParent (UnitTest t)
		{
			parent = t;
		}
		
		public virtual string ActiveConfiguration {
			get {
				if (ownerSolutionEntityItem != null) {
					if (ownerSolutionEntityItem.DefaultConfiguration == null)
						return "";
					return ownerSolutionEntityItem.DefaultConfiguration.Id;
				} else if (Parent != null) {
					return Parent.ActiveConfiguration;
				} else {
					return "default";
				}
			}
		}
		
		public virtual string[] GetConfigurations ()
		{
			if (ownerSolutionEntityItem != null) {
				string[] res = new string [ownerSolutionEntityItem.Configurations.Count];
				for (int n=0; n<ownerSolutionEntityItem.Configurations.Count; n++)
					res [n] = ownerSolutionEntityItem.Configurations [n].Id;
				return res;
			} else if (Parent != null) {
				return Parent.GetConfigurations ();
			} else {
				return new string [] { "default" };
			}
		}

		public UnitTestResultsStore Results {
			get {
				if (results == null) {
					results = new UnitTestResultsStore (this, GetResultsStore ());
				}
				return results;
			}
		}
		
		public UnitTestResult GetLastResult ()
		{
			if (!resultLoaded) {
				resultLoaded = true;
				lastResult = Results.GetLastResult (DateTime.Now);
				if (lastResult != null)
					historicResult = true;
			}
			return lastResult;
		}
		
		public virtual void ResetLastResult ()
		{
			historicResult = true;
			OnTestStatusChanged ();
		}

		public bool IsHistoricResult {
			get { return historicResult; }
			internal set { historicResult = value; }
		}
		
		public UnitTestCollection GetRegressions (DateTime fromDate, DateTime toDate)
		{
			UnitTestCollection list = new UnitTestCollection ();
			FindRegressions (list, fromDate, toDate);
			return list;
		}
		
		public virtual int CountTestCases ()
		{
			return 1;
		}
		
		public virtual SourceCodeLocation SourceCodeLocation {
			get { return null; }
		}
		
		public UnitTest Parent {
			get { return parent; }
		}
		
		public UnitTest RootTest {
			get {
				if (parent != null)
					return parent.RootTest;
				else
					return this;
			}
		}
		
		public virtual string Name {
			get { return name; }
		}
		
		public virtual string Title {
			get { return Name; }
		}
		
		public TestStatus Status {
			get { return status; }
			set {
				status = value;
				OnTestStatusChanged ();
				(Parent as UnitTestGroup)?.UpdateStatusFromChildren ();
			}
		}

		public Xwt.Drawing.Image StatusIcon {
			get {
				if (Status == TestStatus.Running) {
					return TestStatusIcon.Running;
				} else if (Status == TestStatus.Loading) {
					return TestStatusIcon.Loading;
				} else if (Status == TestStatus.LoadError) {
					return TestStatusIcon.Failure;
				} else {
					UnitTestResult res = GetLastResult ();
					if (res == null)
						return TestStatusIcon.None;
					else if (res.Status == ResultStatus.Ignored)
						return TestStatusIcon.NotRun;
					else if (res.ErrorsAndFailures > 0 && res.Passed > 0)
						return IsHistoricResult ? TestStatusIcon.OldSuccessAndFailure : TestStatusIcon.SuccessAndFailure;
					else if (res.IsInconclusive)
						return IsHistoricResult ? TestStatusIcon.OldInconclusive : TestStatusIcon.Inconclusive;
					else if (res.IsFailure)
						return IsHistoricResult ? TestStatusIcon.OldFailure : TestStatusIcon.Failure;
					else if (res.IsSuccess)
						return IsHistoricResult ? TestStatusIcon.OldSuccess : TestStatusIcon.Success;
					else if (res.IsNotRun || res.Ignored > 0)
						return TestStatusIcon.NotRun;
					else
						return TestStatusIcon.None;
				}
			}
		}

		public string TestId {
			get;
			protected set;
		}
		
		public string FullName {
			get {
				if (parent != null)
					return parent.FullName + "." + Name;
				else
					return Name;
			}
		}
		
		protected WorkspaceObject OwnerSolutionItem {
			get { return ownerSolutionItem; }
		}
		
		public WorkspaceObject OwnerObject {
			get {
				if (ownerSolutionItem != null)
					return ownerSolutionItem;
				else if (parent != null)
					return parent.OwnerObject;
				else
					return null;
			}
		}
		
		internal string StoreRelativeName {
			get {
				if (resultsStore != null || Parent == null)
					return "";
				else if (Parent.resultsStore != null)
					return Name;
				else
					return Parent.StoreRelativeName + "." + Name;
			}
		}
		
		// Forces the reloading of tests, if they have changed
		public virtual Task Refresh (CancellationToken ct)
		{
			return Task.FromResult (0);
		}
		
		public UnitTestResult Run (TestContext testContext)
		{
			testContext.Monitor.BeginTest (this);
			UnitTestResult res = null;
			object ctx = testContext.ContextData;
			
			try {
				Status = TestStatus.Running;
				res = OnRun (testContext);
			} catch (Exception ex) {
				res = UnitTestResult.CreateFailure (ex);
			} finally {
				Status = TestStatus.Ready;
				testContext.Monitor.EndTest (this, res);
			}
			RegisterResult (testContext, res);
			testContext.ContextData = ctx;
			return res;
		}
		
		public bool CanRun (IExecutionHandler executionContext)
		{
			if (executionContext == null)
				executionContext = Runtime.ProcessService.DefaultExecutionHandler;
			return OnCanRun (executionContext);
		}
		
		protected abstract UnitTestResult OnRun (TestContext testContext);
		
		protected virtual bool OnCanRun (IExecutionHandler executionContext)
		{
			return true;
		}

		bool building;

		/// <summary>
		/// Builds the project that contains this unit test or group of unit tests.
		/// It returns when the project has been built and the tests have been updated. 
		/// </summary>
		public Task Build ()
		{
			return OnBuild ();
		}

		protected virtual Task OnBuild ()
		{
			if (parent != null)
				return parent.Build ();
			return Task.FromResult (true);
		}
		
		public void RegisterResult (TestContext context, UnitTestResult result)
		{
			// Avoid registering results twice
			if (lastResult != null && lastResult.TestDate == context.TestDate)
				return;

			result.TestDate = context.TestDate;
//			if ((int)result.Status == 0)
//				result.Status = ResultStatus.Ignored;

			lastResult = result;
			historicResult = false;
			resultLoaded = true;

			IResultsStore store = GetResultsStore ();
			if (store != null)
				store.RegisterResult (ActiveConfiguration, this, result);
				OnTestStatusChanged ();
		}
		
		IResultsStore GetResultsStore ()
		{
			if (resultsStore != null)
				return resultsStore;
			if (Parent != null)
				return Parent.GetResultsStore ();
			else
				return null;
		}
		
		protected IResultsStore ResultsStore {
			get { return resultsStore; }
			set { resultsStore = value; }
		}
		
		public virtual void SaveResults ()
		{
			IResultsStore store = GetResultsStore ();
			if (store != null)
				store.Save ();
		}
		
		internal virtual void FindRegressions (UnitTestCollection list, DateTime fromDate, DateTime toDate)
		{
			UnitTestResult res1 = Results.GetLastResult (fromDate);
			UnitTestResult res2 = Results.GetLastResult (toDate);
			if ((res1 == null || res1.IsSuccess) && (res2 != null && !res2.IsSuccess))
				list.Add (this);
		}

		void GetOwnerSolutionItem (UnitTest t, out IConfigurationTarget c, out string path)
		{
			if (OwnerSolutionItem is SolutionItem) {
				c = OwnerSolutionItem as SolutionItem;
				path = "";
			} else if (parent != null) {
				parent.GetOwnerSolutionItem (t, out c, out path);
				if (c == null) return;
				if (path.Length > 0)
					path += "/" + t.Name;
				else
					path = t.Name;
			} else {
				c = null;
				path = null;
			}
		}
		
		void OnConfugurationChanged (object ob, ConfigurationEventArgs args)
		{
			OnActiveConfigurationChanged ();
		}
		
		protected virtual void OnActiveConfigurationChanged ()
		{
			OnTestChanged ();
		}
		
		protected virtual void OnTestChanged ()
		{
			Gtk.Application.Invoke ((o, args) => {
				// Run asynchronously in the UI thread
				if (TestChanged != null)
					TestChanged (this, EventArgs.Empty);
			});
		}
		
		protected virtual void OnTestStatusChanged ()
		{
			Gtk.Application.Invoke ((o, args) => {
				// Run asynchronously in the UI thread
				if (TestStatusChanged != null)
					TestStatusChanged (this, EventArgs.Empty);
			});
		}
		
		public event EventHandler TestChanged;
		public event EventHandler TestStatusChanged;
	}
	
	public class SourceCodeLocation
	{
		string fileName;
		int line;
		int column;
		
		public SourceCodeLocation (string fileName, int line, int column)
		{
			this.fileName = fileName;
			this.line = line;
			this.column = column;
		}
		
		public string FileName {
			get { return fileName; }
		}
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return column; }
		}
	}
}

