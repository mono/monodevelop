//
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
using System.Threading;
using Gtk;
using Gdk;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;

namespace MonoDevelop.NUnit
{
	public class TestPad: TreeViewPad
	{
		NUnitService testService = (NUnitService) ServiceManager.GetService (typeof(NUnitService));
		
		IAsyncOperation runningTestOperation;
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
		
		EventHandler testChangedHandler;
		VBox detailsPad;
		
		ArrayList testNavigationHistory = new ArrayList ();
		
		public override void Initialize (string label, string icon, NodeBuilder[] builders, TreePadOption[] options)
		{
			base.Initialize (label, icon, builders, options);
			
			testChangedHandler = (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnDetailsTestChanged));
			testService.TestSuiteChanged += (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (OnTestSuiteChanged));
			
			paned = new VPaned ();
			paned.Pack1 (base.Control, true, true);
			
			detailsPad = new VBox ();
			
			EventBox eb = new EventBox ();
			HBox header = new HBox ();
			eb.Add (header);

			detailLabel = new HeaderLabel ();
			detailLabel.Padding = 6;
			
			Button hb = new Button (new Gtk.Image ("gtk-close", IconSize.SmallToolbar));
			hb.Relief = ReliefStyle.None;
			hb.Clicked += new EventHandler (OnCloseDetails);
			header.PackEnd (hb, false, false, 0);
			
			hb = new Button (new Gtk.Image ("gtk-go-back", IconSize.SmallToolbar));
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
			chart.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnChartPopupMenu);
			chart.SelectionChanged += new EventHandler (OnChartDateChanged);
			chart.HeightRequest = 50;
			
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
			detailsTree = new TreeView ();
			
			detailsTree.HeadersVisible = true;
			detailsTree.RulesHint = true;
			detailsStore = new ListStore (typeof(object), typeof(string), typeof (string), typeof (string), typeof (string));
			
			CellRendererText trtest = new CellRendererText ();
			CellRendererText tr;
			
			TreeViewColumn col3 = new TreeViewColumn ();
			col3.Expand = false;
//			col3.Alignment = 0.5f;
			col3.Widget = new Gtk.Image (CircleImage.Success);
			col3.Widget.Show ();
			tr = new CellRendererText ();
//			tr.Xalign = 0.5f;
			col3.PackStart (tr, false);
			col3.AddAttribute (tr, "markup", 2);
			detailsTree.AppendColumn (col3);
			
			TreeViewColumn col4 = new TreeViewColumn ();
			col4.Expand = false;
//			col4.Alignment = 0.5f;
			col4.Widget = new Gtk.Image (CircleImage.Failure);
			col4.Widget.Show ();
			tr = new CellRendererText ();
//			tr.Xalign = 0.5f;
			col4.PackStart (tr, false);
			col4.AddAttribute (tr, "markup", 3);
			detailsTree.AppendColumn (col4);
			
			TreeViewColumn col5 = new TreeViewColumn ();
			col5.Expand = false;
//			col5.Alignment = 0.5f;
			col5.Widget = new Gtk.Image (CircleImage.NotRun);
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
			
			TestSummaryPage = infoBook.AddPage ("Summary", tf);
			
			// Info - Regressions list
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			regressionTree = new TreeView ();
			regressionTree.HeadersVisible = false;
			regressionTree.RulesHint = true;
			regressionStore = new ListStore (typeof(object), typeof(string), typeof (Pixbuf));
			
			CellRendererText trtest2 = new CellRendererText ();
			CellRendererPixbuf pr = new CellRendererPixbuf ();
			
			TreeViewColumn col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", 2);
			col.PackStart (trtest2, false);
			col.AddAttribute (trtest2, "markup", 1);
			regressionTree.AppendColumn (col);
			regressionTree.Model = regressionStore;
			sw.Add (regressionTree);
			tf.ShowAll ();
			
			TestRegressionsPage = infoBook.AddPage ("Regressions", tf);
			
			// Info - Failed tests list
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			failedTree = new TreeView ();
			failedTree.HeadersVisible = false;
			failedTree.RulesHint = true;
			failedStore = new ListStore (typeof(object), typeof(string), typeof (Pixbuf));
			
			trtest2 = new CellRendererText ();
			pr = new CellRendererPixbuf ();
			
			col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", 2);
			col.PackStart (trtest2, false);
			col.AddAttribute (trtest2, "markup", 1);
			failedTree.AppendColumn (col);
			failedTree.Model = failedStore;
			sw.Add (failedTree);
			tf.ShowAll ();
			
			TestFailuresPage = infoBook.AddPage ("Failed tests", tf);
			
			// Info - results
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			resultView = new TextView ();
			resultView.Editable = false;
			sw.Add (resultView);
			tf.ShowAll ();
			TestResultPage = infoBook.AddPage ("Result", tf);
			
			// Info - Output
			
			tf = new Frame ();
			sw = new ScrolledWindow ();
			tf.Add (sw);
			outputView = new TextView ();
			outputView.Editable = false;
			sw.Add (outputView);
			tf.ShowAll ();
			TestOutputPage = infoBook.AddPage ("Output", tf);
			
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
		}
		
		void OnTestSuiteChanged (object sender, EventArgs e)
		{
			if (testService.RootTest != null)
				LoadTree (testService.RootTest);
			else {
				Clear ();
				ClearDetails ();
			}
		}
		
		public void SelectTest (UnitTest t)
		{
			ITreeNavigator node = FindTestNode (t);
			node.ExpandToNode ();
			node.Selected = true;
		}
		
		ITreeNavigator FindTestNode (UnitTest t)
		{
			ITreeNavigator nav = GetNodeAtObject (t);
			if (nav != null)
				return nav;
			if (t.Parent == null)
				return null;
				
			nav = FindTestNode (t.Parent);
			
			if (nav == null)
				return null;
				
			nav.MoveToFirstChild ();	// Make sure the children are created
			return GetNodeAtObject (t);
		}
		
		public override Gtk.Widget Control {
			get {
				return paned;
			}
		}
		
		[CommandHandler (TestCommands.RunTest)]
		protected void OnRunTest ()
		{
			RunSelectedTest ();
		}
		
		[CommandUpdateHandler (TestCommands.RunTest)]
		protected void OnUpdateRunTest (CommandInfo info)
		{
			info.Enabled = runningTestOperation == null;
		}
		
		public override void ActivateCurrentItem ()
		{
			RunSelectedTest ();
		}
		
		void RunSelectedTest ()
		{
			ITreeNavigator nav = GetSelectedNode ();
			UnitTest test = (UnitTest) nav.DataItem;
			
			runningTestOperation = testService.RunTest (test);
			runningTestOperation.Completed += (OperationHandler) Runtime.DispatchService.GuiDispatch (new OperationHandler (TestSessionCompleted));
		}
		
		void TestSessionCompleted (IAsyncOperation op)
		{
			if (op.Success)
				RefreshDetails ();
			runningTestOperation = null;
		}
		
		protected override void OnSelectionChanged (object sender, EventArgs args)
		{
			base.OnSelectionChanged (sender, args);
			ITreeNavigator nav = GetSelectedNode ();
			UnitTest test = (UnitTest) nav.DataItem;
			FillDetails (test, false);
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
				
				ITreeNavigator nav = GetSelectedNode ();
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
				detailsTest.TestChanged -= testChangedHandler;
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
				detailsTest.TestChanged -= testChangedHandler;
			
			if (detailsTest != test) {
				detailsTest = test;
				if (selectInTree)
					SelectTest (test);
				testNavigationHistory.Add (test);
				if (testNavigationHistory.Count > 50)
					testNavigationHistory.RemoveAt (0);
			}
			detailsTest.TestChanged += testChangedHandler;
			
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
							detailsStore.AppendValues (t, t.Name, res.TotalSuccess.ToString(), res.TotalFailures.ToString(), res.TotalIgnored.ToString());
						else
							detailsStore.AppendValues (t, t.Name, "", "", "");
					}
				}
				else if (infoBook.Page == TestRegressionsPage) {
					regressionStore.Clear ();
					UnitTestCollection regs = detailsTest.GetRegressions (chart.ReferenceDate, chart.CurrentDate);
					if (regs.Count > 0) {
						foreach (UnitTest t in regs)
							regressionStore.AppendValues (t, t.Name, CircleImage.Failure);
					} else
						regressionStore.AppendValues (null, "No regressions found.");
				}
				else if (infoBook.Page == TestFailuresPage) {
					failedStore.Clear ();
					UnitTestCollection regs = group.GetFailedTests (chart.CurrentDate);
					if (regs.Count > 0) {
						foreach (UnitTest t in regs)
							failedStore.AppendValues (t, t.Name, CircleImage.Failure);
					} else
						failedStore.AppendValues (null, "No failed tests found.");
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
		
		void OnChartPopupMenu (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				Runtime.Gui.CommandService.ShowContextMenu ("/SharpDevelop/Views/TestChart/ContextMenu");
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
			Gdk.GC gc = new Gdk.GC (GdkWindow);
			gc.ClipRectangle = Allocation;
			GdkWindow.DrawLayout (gc, padding, padding, layout);
			return true;
		}
	}
}

