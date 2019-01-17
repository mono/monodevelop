﻿//
// TestPad.cs
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
using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Commands;
using MonoDevelop.UnitTesting.Commands;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Ide.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Components.AutoTest;

using SCM = System.ComponentModel;

namespace MonoDevelop.UnitTesting
{
	class TestPad : TreeViewPad
	{
		AsyncOperation runningTestOperation;
		VPaned paned;
		TreeView detailsTree;
		ListStore detailsStore;
		HeaderLabel detailLabel;
		TestChart chart;
		UnitTest detailsTest;
		DateTime detailsDate;
		DateTime detailsReferenceDate;
		ButtonNotebook infoBook;
		TextView outputView;
		TextView resultView;
		TreeView regressionTree;
		ListStore regressionStore;
		TreeView failedTree;
		ListStore failedStore;
		
		int TestSummaryPage;
		int TestRegressionsPage;
		int TestFailuresPage;
		int TestResultPage;
		int TestOutputPage;
		
		VBox detailsPad;
		
		ArrayList testNavigationHistory = new ArrayList ();

		Button buttonRunAll, buttonStop;
		
		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options, string menuPath)
		{
			base.Initialize (builders, options, menuPath);
			
			UnitTestService.TestSuiteChanged += OnTestSuiteChanged;
			paned = new VPaned ();
			
			VBox vbox = new VBox ();
			DockItemToolbar topToolbar = Window.GetToolbar (DockPositionType.Top);

			var hbox = new HBox { Spacing = 6 };
			hbox.PackStart (new ImageView (ImageService.GetIcon ("md-execute-all", IconSize.Menu)), false, false, 0);
			hbox.PackStart (new Label (GettextCatalog.GetString ("Run All")), false, false, 0);
			buttonRunAll = new Button (hbox);
			buttonRunAll.Accessible.Name = "TestPad.RunAll";
			buttonRunAll.Accessible.Description = GettextCatalog.GetString ("Start a test run and run all the tests");
			buttonRunAll.Clicked += new EventHandler (OnRunAllClicked);
			buttonRunAll.Sensitive = true;
			buttonRunAll.TooltipText = GettextCatalog.GetString ("Run all tests");
			topToolbar.Add (buttonRunAll);
			
			buttonStop = new Button (new ImageView (Ide.Gui.Stock.Stop, IconSize.Menu));
			buttonStop.Clicked += new EventHandler (OnStopClicked);
			buttonStop.Sensitive = false;
			buttonStop.TooltipText = GettextCatalog.GetString ("Cancel running test");
			buttonStop.Accessible.Name = "TestPad.StopAll";
			buttonStop.Accessible.SetTitle (GettextCatalog.GetString (("Cancel")));
			buttonStop.Accessible.Description = GettextCatalog.GetString ("Stops the current test run");
			topToolbar.Add (buttonStop);
			topToolbar.ShowAll ();
			
			vbox.PackEnd (base.Control, true, true, 0);
			vbox.FocusChain = new Gtk.Widget [] { base.Control };
			
			paned.Pack1 (vbox, true, true);
			
			detailsPad = new VBox ();
			
			EventBox eb = new EventBox ();
			HBox header = new HBox ();
			eb.Add (header);

			detailLabel = new HeaderLabel ();
			detailLabel.Padding = 6;
			
			Button hb = new Button (new ImageView ("gtk-close", IconSize.SmallToolbar));
			hb.Relief = ReliefStyle.None;
			hb.Clicked += new EventHandler (OnCloseDetails);
			header.PackEnd (hb, false, false, 0);
			
			hb = new Button (new ImageView ("gtk-go-back", IconSize.SmallToolbar));
			hb.Relief = ReliefStyle.None;
			hb.Clicked += new EventHandler (OnGoBackTest);
			header.PackEnd (hb, false, false, 0);
			
			header.Add (detailLabel);
			Gdk.Color hcol = eb.Style.Background (StateType.Normal);
			hcol.Red = (ushort) (((double)hcol.Red) * 0.9);
			hcol.Green = (ushort) (((double)hcol.Green) * 0.9);
			hcol.Blue = (ushort) (((double)hcol.Blue) * 0.9);
		//	eb.ModifyBg (StateType.Normal, hcol);
			
			detailsPad.PackStart (eb, false, false, 0);
			
			VPaned panedDetails = new VPaned ();
			panedDetails.BorderWidth = 3;
			VBox boxPaned1 = new VBox ();
			
			chart = new TestChart ();
			chart.SelectionChanged += new EventHandler (OnChartDateChanged);

			var chartWidget = chart.GetNativeWidget<Widget> ();
			chartWidget.ButtonPressEvent += OnChartButtonPress;
			chartWidget.HeightRequest = 50;

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.ShowArrow = false;
			ToolButton but = new ToolButton ("gtk-zoom-in");
			but.Clicked += new EventHandler (OnZoomIn);
			toolbar.Insert (but, -1);
			
			but = new ToolButton ("gtk-zoom-out");
			but.Clicked += new EventHandler (OnZoomOut);
			toolbar.Insert (but, -1);
			
			but = new ToolButton ("gtk-go-back");
			but.Clicked += new EventHandler (OnChartBack);
			toolbar.Insert (but, -1);
			
			but = new ToolButton ("gtk-go-forward");
			but.Clicked += new EventHandler (OnChartForward);
			toolbar.Insert (but, -1);
			
			but = new ToolButton ("gtk-goto-last");
			but.Clicked += new EventHandler (OnChartLast);
			toolbar.Insert (but, -1);
			
			boxPaned1.PackStart (toolbar, false, false, 0);
			boxPaned1.PackStart (chart, true, true, 0);
			
			panedDetails.Pack1 (boxPaned1, false, false);
			
			// Detailed test information --------
			
			infoBook = new ButtonNotebook ();
			infoBook.PageLoadRequired += new EventHandler (OnPageLoadRequired);
			
			// Info - Group summary
			
			Frame tf = new Frame ();
			ScrolledWindow sw = new ScrolledWindow ();
			detailsTree = new TreeView { Name = "testPadDetailsTree" };
			
			detailsTree.HeadersVisible = true;
			detailsTree.RulesHint = true;
			detailsStore = new ListStore (typeof(object), typeof(string), typeof (string), typeof (string), typeof (string));
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("store__UnitTest", "store__Name","store__Passed",
				"store__ErrorsAndFailures", "store__Ignored");
			SCM.TypeDescriptor.AddAttributes (detailsStore, modelAttr);
			
			CellRendererText trtest = new CellRendererText ();
			CellRendererText tr;
			
			TreeViewColumn col3 = new TreeViewColumn ();
			col3.Expand = false;
//			col3.Alignment = 0.5f;
			col3.Widget = new ImageView (TestStatusIcon.Success);
			col3.Widget.Show ();
			tr = new CellRendererText ();
//			tr.Xalign = 0.5f;
			col3.PackStart (tr, false);
			col3.AddAttribute (tr, "markup", 2);
			detailsTree.AppendColumn (col3);
			
			TreeViewColumn col4 = new TreeViewColumn ();
			col4.Expand = false;
//			col4.Alignment = 0.5f;
			col4.Widget = new ImageView (TestStatusIcon.Failure);
			col4.Widget.Show ();
			tr = new CellRendererText ();
//			tr.Xalign = 0.5f;
			col4.PackStart (tr, false);
			col4.AddAttribute (tr, "markup", 3);
			detailsTree.AppendColumn (col4);
			
			TreeViewColumn col5 = new TreeViewColumn ();
			col5.Expand = false;
//			col5.Alignment = 0.5f;
			col5.Widget = new ImageView (TestStatusIcon.NotRun);
			col5.Widget.Show ();
			tr = new CellRendererText ();
//			tr.Xalign = 0.5f;
			col5.PackStart (tr, false);
			col5.AddAttribute (tr, "markup", 4);
			detailsTree.AppendColumn (col5);
			
			TreeViewColumn col1 = new TreeViewColumn ();
//			col1.Resizable = true;
//			col1.Expand = true;
			col1.Title = "Test";
			col1.PackStart (trtest, true);
			col1.AddAttribute (trtest, "markup", 1);
			detailsTree.AppendColumn (col1);
			
			detailsTree.Model = detailsStore;
			
			sw.Add (detailsTree);
			tf.Add (sw);
			tf.ShowAll ();
			
			TestSummaryPage = infoBook.AddPage (GettextCatalog.GetString ("Summary"), tf);
			
			// Info - Regressions list
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			regressionTree = new TreeView { Name = "testPadRegressionTree" };
			regressionTree.HeadersVisible = false;
			regressionTree.RulesHint = true;
			regressionStore = new ListStore (typeof(object), typeof(string), typeof (Xwt.Drawing.Image));
			SemanticModelAttribute regressionModelAttr = new SemanticModelAttribute ("store__UnitTest", "store__Name", "store__Icon");
			SCM.TypeDescriptor.AddAttributes (detailsStore, regressionModelAttr);
			
			CellRendererText trtest2 = new CellRendererText ();
			var pr = new CellRendererImage ();
			
			TreeViewColumn col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "image", 2);
			col.PackStart (trtest2, false);
			col.AddAttribute (trtest2, "markup", 1);
			regressionTree.AppendColumn (col);
			regressionTree.Model = regressionStore;
			sw.Add (regressionTree);
			tf.ShowAll ();
			
			TestRegressionsPage = infoBook.AddPage (GettextCatalog.GetString ("Regressions"), tf);
			
			// Info - Failed tests list
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			failedTree = new TreeView ();
			failedTree.HeadersVisible = false;
			failedTree.RulesHint = true;
			failedStore = new ListStore (typeof(object), typeof(string), typeof (Xwt.Drawing.Image));
			SemanticModelAttribute failedModelAttr = new SemanticModelAttribute ("store__UnitTest", "store__Name", "store__Icon");
			SCM.TypeDescriptor.AddAttributes (failedStore, failedModelAttr);
			
			trtest2 = new CellRendererText ();
			pr = new CellRendererImage ();
			
			col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "image", 2);
			col.PackStart (trtest2, false);
			col.AddAttribute (trtest2, "markup", 1);
			failedTree.AppendColumn (col);
			failedTree.Model = failedStore;
			sw.Add (failedTree);
			tf.ShowAll ();
			
			TestFailuresPage = infoBook.AddPage (GettextCatalog.GetString ("Failed tests"), tf);
			
			// Info - results
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			resultView = new TextView ();
			resultView.Editable = false;
			sw.Add (resultView);
			tf.ShowAll ();
			TestResultPage = infoBook.AddPage (GettextCatalog.GetString ("Result"), tf);
			
			// Info - Output
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			outputView = new TextView ();
			outputView.Editable = false;
			sw.Add (outputView);
			tf.ShowAll ();
			TestOutputPage = infoBook.AddPage (GettextCatalog.GetString ("Output"), tf);
			
			panedDetails.Pack2 (infoBook, true, true);
			detailsPad.PackStart (panedDetails, true, true, 0);
			paned.Pack2 (detailsPad, true, true);
			
			paned.ShowAll ();
			
			infoBook.HidePage (TestResultPage);
			infoBook.HidePage (TestOutputPage);
			infoBook.HidePage (TestSummaryPage);
			infoBook.HidePage (TestRegressionsPage);
			infoBook.HidePage (TestFailuresPage);
			
			detailsPad.Sensitive = false;
			detailsPad.Hide ();
			
			detailsTree.RowActivated += new Gtk.RowActivatedHandler (OnTestActivated);
			regressionTree.RowActivated += new Gtk.RowActivatedHandler (OnRegressionTestActivated);
			failedTree.RowActivated += new Gtk.RowActivatedHandler (OnFailedTestActivated);
			
			foreach (UnitTest t in UnitTestService.RootTests)
				TreeView.AddChild (t);
			
			base.TreeView.Tree.Name = "unitTestBrowserTree";
		}
		
		void OnTestSuiteChanged (object sender, EventArgs e)
		{
			if (UnitTestService.RootTests.Length > 0) {
				var s = TreeView.SaveTreeState ();
				TreeView.Clear ();
				foreach (UnitTest t in UnitTestService.RootTests)
					TreeView.AddChild (t);
				TreeView.RestoreTreeState (s);
			}
			else {
				TreeView.Clear ();
				ClearDetails ();
			}
		}
		
		public void SelectTest (UnitTest t)
		{
			ITreeNavigator node = FindTestNode (t);
			if (node != null) {
				node.ExpandToNode ();
				node.Selected = true;
			}
		}
		
		ITreeNavigator FindTestNode (UnitTest t)
		{
			ITreeNavigator nav = TreeView.GetNodeAtObject (t);
			if (nav != null)
				return nav;
			if (t.Parent == null)
				return null;
				
			nav = FindTestNode (t.Parent);
			
			if (nav == null)
				return null;
				
			nav.MoveToFirstChild ();	// Make sure the children are created
			return TreeView.GetNodeAtObject (t);
		}
		
		public override Control Control {
			get {
				return paned;
			}
		}
		
		[CommandHandler (TestCommands.RunTest)]
		protected void OnRunTest ()
		{
			RunSelectedTest (null);
		}
		
		[CommandUpdateHandler (TestCommands.RunTest)]
		protected void OnUpdateRunTest (CommandInfo info)
		{
			info.Enabled = runningTestOperation == null;
		}
		
		[CommandHandler (TestCommands.RunTestWith)]
		protected void OnRunTest (object data)
		{
			IExecutionHandler h = ExecutionModeCommandService.GetExecutionModeForCommand (data);
			if (h != null)
				RunSelectedTest (h);
		}
		
		[CommandUpdateHandler (TestCommands.RunTestWith)]
		protected void OnUpdateRunTest (CommandArrayInfo info)
		{
			UnitTest test = GetSelectedTest ();
			if (test != null) {
				SolutionItem item = test.OwnerObject as SolutionItem;
				ExecutionModeCommandService.GenerateExecutionModeCommands (
				    item,
				    test.CanRun,
				    info);

				foreach (var ci in info)
					ci.Enabled = runningTestOperation == null;
			}
		}
		
		[CommandHandler (TestCommands.DebugTest)]
		protected void OnDebugTest (object data)
		{
			var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode ();
			var mode = debugModeSet.ExecutionModes.First (m => m.Id == (string)data);
			RunSelectedTest (mode.ExecutionHandler);
		}

		[CommandUpdateHandler (TestCommands.DebugTest)]
		protected void OnUpdateDebugTest (CommandArrayInfo info)
		{
			var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode ();
			if (debugModeSet == null)
				return;

			UnitTest test = GetSelectedTest ();
			if (test == null)
				return;

			foreach (var mode in debugModeSet.ExecutionModes) {
				if (test.CanRun (mode.ExecutionHandler)) {
					var ci = info.Add (GettextCatalog.GetString ("Debug Test ({0})", mode.Name), mode.Id);
					ci.Enabled = runningTestOperation == null;
				}
			}

			if (info.Count == 1)
				info [0].Text = GettextCatalog.GetString ("Debug Test");
		}

		public TestPad ()
		{
			base.TreeView.CurrentItemActivated += delegate {
				RunSelectedTest (null);
			};
		}
		
		void OnStopClicked (object sender, EventArgs args)
		{
			StopRunningTests ();
		}

		void StopRunningTests ()
		{
			if (runningTestOperation != null)
				runningTestOperation.Cancel ();
		}

		UnitTest GetSelectedTest ()
		{
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			if (nav == null)
				return null;
			return nav.DataItem as UnitTest;
		}

		public AsyncOperation RunTest (UnitTest test, IExecutionHandler mode)
		{
			return RunTest (FindTestNode (test), mode, false);
		}

		AsyncOperation RunTests (ITreeNavigator[] navs, IExecutionHandler mode, bool bringToFront = true)
		{
			if (navs == null)
				return null;
			var tests = new List<UnitTest> ();
			WorkspaceObject ownerObject = null;
			foreach (var nav in navs) {
				var test = nav.DataItem as UnitTest;
				if (test != null) {
					tests.Add (test);
					ownerObject = test.OwnerObject;
				}
			}
			if (tests.Count == 0)
				return null;
			return RunTests (tests, mode, bringToFront);
		}

		AsyncOperation RunTest (ITreeNavigator nav, IExecutionHandler mode, bool bringToFront = true)
		{
			if (nav == null)
				return null;
			UnitTest test = nav.DataItem as UnitTest;
			if (test == null)
				return null;
			return RunTests (new UnitTest [] { test }, mode, bringToFront);
		}

		AsyncOperation RunTests (IEnumerable<UnitTest> tests, IExecutionHandler mode, bool bringToFront)
		{
			foreach (var test in tests)
				UnitTestService.ResetResult (test.RootTest);
			
			this.buttonRunAll.Sensitive = false;
			this.buttonStop.Sensitive = true;

			ExecutionContext context = new ExecutionContext (mode, IdeApp.Workbench.ProgressMonitors.ConsoleFactory, null);

			if (bringToFront)
				IdeApp.Workbench.GetPad<TestPad> ().BringToFront ();
			StopRunningTests ();
			runningTestOperation = UnitTestService.RunTests (tests, context);
			runningTestOperation.Task.ContinueWith (t => OnTestSessionCompleted (), TaskScheduler.FromCurrentSynchronizationContext ());
			return runningTestOperation;
		}
		
		void OnRunAllClicked (object sender, EventArgs args)
		{
			RunTest (TreeView.GetRootNode (), null);
		}
		
		void RunSelectedTest (IExecutionHandler mode)
		{
			RunTests (TreeView.GetSelectedNodes (), mode);
		}
		
		void OnTestSessionCompleted ()
		{
			RefreshDetails ();
			runningTestOperation = null;
			this.buttonRunAll.Sensitive = true;
			this.buttonStop.Sensitive = false;

		}


		protected override void OnSelectionChanged (object sender, EventArgs args)
		{
			base.OnSelectionChanged (sender, args);
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			if (nav != null) {
				UnitTest test = nav.DataItem as UnitTest;
				if (test != null)
					FillDetails (test, false);
			}
		}
		
		void OnGoBackTest (object sender, EventArgs args)
		{
			int c = testNavigationHistory.Count;
			if (c > 1) {
				UnitTest t = (UnitTest) testNavigationHistory [c - 2];
				testNavigationHistory.RemoveAt (c - 1);
				testNavigationHistory.RemoveAt (c - 2);
				FillDetails (t, true);
			}
		}
		
		void OnCloseDetails (object sender, EventArgs args)
		{
			detailsPad.Hide ();
		}
		
		[CommandHandler (TestCommands.ShowTestDetails)]
		protected void OnShowDetails ()
		{
			if (!detailsPad.Visible) {
				detailsPad.Show ();
				
				ITreeNavigator nav = TreeView.GetSelectedNode ();
				if (nav != null) {
					UnitTest test = (UnitTest) nav.DataItem;
					FillDetails (test, false);
				} else
					ClearDetails ();
			}
		}
		
		void ClearDetails ()
		{
			chart.Clear ();
			detailLabel.Markup = "";
			detailsStore.Clear ();
			if (detailsTest != null)
				detailsTest.TestChanged -= OnDetailsTestChanged;
			detailsTest = null;
			detailsDate = DateTime.MinValue;
			detailsReferenceDate = DateTime.MinValue;
			detailsPad.Sensitive = false;
		}
		
		void RefreshDetails ()
		{
			if (detailsTest != null)
				FillDetails (detailsTest, false);
		}
		
		void FillDetails (UnitTest test, bool selectInTree)
		{
			if (!detailsPad.Visible)
				return;

			detailsPad.Sensitive = true;
			
			if (detailsTest != null)
				detailsTest.TestChanged -= OnDetailsTestChanged;
			
			if (detailsTest != test) {
				detailsTest = test;
				if (selectInTree)
					SelectTest (test);
				testNavigationHistory.Add (test);
				if (testNavigationHistory.Count > 50)
					testNavigationHistory.RemoveAt (0);
			}
			detailsTest.TestChanged += OnDetailsTestChanged;
			
			if (test is UnitTestGroup) {
				infoBook.HidePage (TestResultPage);
				infoBook.HidePage (TestOutputPage);
				infoBook.ShowPage (TestSummaryPage);
				infoBook.ShowPage (TestRegressionsPage);
				infoBook.ShowPage (TestFailuresPage);
			} else {
				infoBook.HidePage (TestSummaryPage);
				infoBook.HidePage (TestRegressionsPage);
				infoBook.HidePage (TestFailuresPage);
				infoBook.ShowPage (TestResultPage);
				infoBook.ShowPage (TestOutputPage);
			}
			detailLabel.Markup = "<b>" + test.Name + "</b>";
			detailsDate = DateTime.MinValue;
			detailsReferenceDate = DateTime.MinValue;
			chart.Fill (test);
			infoBook.Reset ();
		}
		
		void FillTestInformation ()
		{
			if (!detailsPad.Visible)
				return;

			if (detailsTest is UnitTestGroup) {
				UnitTestGroup group = detailsTest as UnitTestGroup;
				if (infoBook.Page == TestSummaryPage) {
					detailsStore.Clear ();
					foreach (UnitTest t in group.Tests) {
						UnitTestResult res = t.Results.GetLastResult (chart.CurrentDate);
						if (res != null)
							detailsStore.AppendValues (t, t.Name, res.Passed.ToString (), res.ErrorsAndFailures.ToString (), res.Ignored.ToString());
						else
							detailsStore.AppendValues (t, t.Name, "", "", "");
					}
				}
				else if (infoBook.Page == TestRegressionsPage) {
					regressionStore.Clear ();
					UnitTestCollection regs = detailsTest.GetRegressions (chart.ReferenceDate, chart.CurrentDate);
					if (regs.Count > 0) {
						foreach (UnitTest t in regs)
							regressionStore.AppendValues (t, t.Name, TestStatusIcon.Failure);
					} else
						regressionStore.AppendValues (null, GettextCatalog.GetString ("No regressions found."));
				}
				else if (infoBook.Page == TestFailuresPage) {
					failedStore.Clear ();
					UnitTestCollection regs = group.GetFailedTests (chart.CurrentDate);
					if (regs.Count > 0) {
						foreach (UnitTest t in regs)
							failedStore.AppendValues (t, t.Name, TestStatusIcon.Failure);
					} else
						failedStore.AppendValues (null, GettextCatalog.GetString ("No failed tests found."));
				}
			} else {
				UnitTestResult res = detailsTest.Results.GetLastResult (chart.CurrentDate);
				if (infoBook.Page == TestOutputPage) {
					outputView.Buffer.Clear ();
					if (res != null)
						outputView.Buffer.InsertAtCursor (res.ConsoleOutput);
				} else if (infoBook.Page == TestResultPage) {
					resultView.Buffer.Clear ();
					if (res != null) {
						string msg = res.Message + "\n\n" + res.StackTrace;
						resultView.Buffer.InsertAtCursor (msg);
					}
				}
			}
		}
		
		void OnDetailsTestChanged (object sender, EventArgs e)
		{
			RefreshDetails ();
		}
		
		void OnChartDateChanged (object sender, EventArgs e)
		{
			if (detailsTest != null && (detailsDate != chart.CurrentDate || detailsReferenceDate != chart.ReferenceDate)) {
				detailsDate = chart.CurrentDate;
				detailsReferenceDate = chart.ReferenceDate;
				FillTestInformation ();
			}
		}
		
		void OnPageLoadRequired (object o, EventArgs args)
		{
			if (detailsTest != null)
				FillTestInformation ();
		}
		
		protected virtual void OnTestActivated (object sender, Gtk.RowActivatedArgs args)
		{
			TreeIter it;
			detailsStore.GetIter (out it, args.Path);
			UnitTest t = (UnitTest) detailsStore.GetValue (it, 0);
			if (t != null)
				FillDetails (t, true);
		}
		
		protected virtual void OnRegressionTestActivated (object sender, Gtk.RowActivatedArgs args)
		{
			TreeIter it;
			regressionStore.GetIter (out it, args.Path);
			UnitTest t = (UnitTest) regressionStore.GetValue (it, 0);
			if (t != null)
				FillDetails (t, true);
		}
		
		protected virtual void OnFailedTestActivated (object sender, Gtk.RowActivatedArgs args)
		{
			TreeIter it;
			failedStore.GetIter (out it, args.Path);
			UnitTest t = (UnitTest) failedStore.GetValue (it, 0);
			if (t != null)
				FillDetails (t, true);
		}
		
		void OnZoomIn (object sender, EventArgs a)
		{
			if (detailsTest != null)
				chart.ZoomIn ();
		}
		
		void OnZoomOut (object sender, EventArgs a)
		{
			if (detailsTest != null)
				chart.ZoomOut ();
		}
		
		void OnChartBack (object sender, EventArgs a)
		{
			if (detailsTest != null)
				chart.GoPrevious ();
		}
		
		void OnChartForward (object sender, EventArgs a)
		{
			if (detailsTest != null)
				chart.GoNext ();
		}
		
		void OnChartLast (object sender, EventArgs a)
		{
			if (detailsTest != null)
				chart.GoLast ();
		}
		
		void OnChartButtonPress (object o, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.TriggersContextMenu ()) {
				IdeApp.CommandService.ShowContextMenu (Control, args.Event, "/MonoDevelop/UnitTesting/ContextMenu/TestChart");
				args.RetVal = true;
			}
		}
		
		[CommandHandler (TestChartCommands.ShowResults)]
		protected void OnShowResults ()
		{
			chart.Type = TestChartType.Results;
		}
		
		[CommandUpdateHandler (TestChartCommands.ShowResults)]
		protected void OnUpdateShowResults (CommandInfo info)
		{
			info.Checked = chart.Type == TestChartType.Results;
		}
		
		[CommandHandler (TestChartCommands.ShowTime)]
		protected void OnShowTime ()
		{
			chart.Type = TestChartType.Time;
		}
		
		[CommandUpdateHandler (TestChartCommands.ShowTime)]
		protected void OnUpdateShowTime (CommandInfo info)
		{
			info.Checked = chart.Type == TestChartType.Time;
		}
		
		[CommandHandler (TestChartCommands.UseTimeScale)]
		protected void OnUseTimeScale ()
		{
			chart.UseTimeScale = !chart.UseTimeScale;
		}
		
		[CommandUpdateHandler (TestChartCommands.UseTimeScale)]
		protected void OnUpdateUseTimeScale (CommandInfo info)
		{
			info.Checked = chart.UseTimeScale;
		}
		
		[CommandHandler (TestChartCommands.SingleDayResult)]
		protected void OnSingleDayResult ()
		{
			chart.SingleDayResult = !chart.SingleDayResult;
		}
		
		[CommandUpdateHandler (TestChartCommands.SingleDayResult)]
		protected void OnUpdateSingleDayResult (CommandInfo info)
		{
			info.Checked = chart.SingleDayResult;
		}
		
		[CommandHandler (TestChartCommands.ShowSuccessfulTests)]
		protected void OnShowSuccessfulTests ()
		{
			chart.ShowSuccessfulTests = !chart.ShowSuccessfulTests;
		}
		
		[CommandUpdateHandler (TestChartCommands.ShowSuccessfulTests)]
		protected void OnUpdateShowSuccessfulTests (CommandInfo info)
		{
			info.Enabled = chart.Type == TestChartType.Results;
			info.Checked = chart.ShowSuccessfulTests;
		}
		
		[CommandHandler (TestChartCommands.ShowFailedTests)]
		protected void OnShowFailedTests ()
		{
			chart.ShowFailedTests = !chart.ShowFailedTests;
		}
		
		[CommandUpdateHandler (TestChartCommands.ShowFailedTests)]
		protected void OnUpdateShowFailedTests (CommandInfo info)
		{
			info.Enabled = chart.Type == TestChartType.Results;
			info.Checked = chart.ShowFailedTests;
		}
		
		[CommandHandler (TestChartCommands.ShowIgnoredTests)]
		protected void OnShowIgnoredTests ()
		{
			chart.ShowIgnoredTests = !chart.ShowIgnoredTests;
		}
		
		[CommandUpdateHandler (TestChartCommands.ShowIgnoredTests)]
		protected void OnUpdateShowIgnoredTests (CommandInfo info)
		{
			info.Enabled = chart.Type == TestChartType.Results;
			info.Checked = chart.ShowIgnoredTests;
		}
	}
	
	class ButtonNotebook: Notebook
	{
		ArrayList loadedPages = new ArrayList ();
		
		public void Reset ()
		{
			loadedPages.Clear ();
			OnPageLoadRequired ();
		}
		
		public int AddPage (string text, Widget widget)
		{
			return AppendPage (widget, new Label (text));
		}
		
		public void ShowPage (int n)
		{
			GetNthPage (n).Show ();
		}
		
		public void HidePage (int n)
		{
			GetNthPage (n).Hide ();
		}
		
		protected override void OnSwitchPage (NotebookPage page, uint n)
		{
			base.OnSwitchPage (page, n);
			if (!loadedPages.Contains (Page))
				OnPageLoadRequired ();
		}
		
		void OnPageLoadRequired ()
		{
			loadedPages.Add (Page);
			if (PageLoadRequired != null)
				PageLoadRequired (this, EventArgs.Empty);
		}
		
		public EventHandler PageLoadRequired;
	}
	
	class HeaderLabel: Widget
	{
		string text;
		Pango.Layout layout;
		int padding;
		
		public HeaderLabel ()
		{
			WidgetFlags |= WidgetFlags.NoWindow;
			layout = new Pango.Layout (this.PangoContext);
		}
		
		public string Markup {
			get { return text; }
			set {
				text = value;
				layout.SetMarkup (text);
				QueueDraw ();
			}
		}
		
		public int Padding {
			get { return padding; }
			set { padding = value; }
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			using (Gdk.GC gc = new Gdk.GC (GdkWindow)) {
				gc.ClipRectangle = Allocation;
				GdkWindow.DrawLayout (gc, padding, padding, layout);
			}
			return true;
		}
		protected override void OnDestroyed ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
			base.OnDestroyed ();
		}
	}
}

