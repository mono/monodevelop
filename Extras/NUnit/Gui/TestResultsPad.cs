//
// TestResultsPad.cs
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
	public class TestResultsPad: GuiSyncObject, IPadContent, ITestProgressMonitor
	{
		NUnitService testService = (NUnitService) ServiceManager.GetService (typeof(NUnitService));
		
		string title;
		VBox panel;
		HPaned book;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		Label infoFailed = new Label ("<b>Failed</b>: 0");
		Label infoIgnored = new Label ("<b>Ignored</b>: 0");
		Label infoCurrent = new Label ();
		HBox labels;
		
		Label resultLabel = new Label ();
		
		ProgressBar progressBar = new ProgressBar ();
		TreeView failuresTreeView;
		TreeStore failuresStore;
		TextView outputView;
		Widget outputViewScrolled;
		VSeparator infoSep;
		ToolButton buttonStop;
		
		ToggleToolButton buttonSuccess;
		ToggleToolButton buttonFailures;
		ToggleToolButton buttonIgnored;
		ToggleToolButton buttonOutput;
		
		bool running;
		int testsToRun;
		int testsRun;
		int testsFailed;
		int testsIgnored;
		
		UnitTest rootTest;
		string configuration;
		ArrayList results = new ArrayList ();
		
		Exception error;
		string errorMessage;
		
		bool cancel;
		
		public class ResultRecord {
			public UnitTest Test;
			public UnitTestResult Result;
		}
		
		public TestResultsPad ()
		{
			title = "Test results";
			
			testService.TestSuiteChanged += new EventHandler (OnTestSuiteChanged);
			
			panel = new VBox ();
			
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			panel.PackStart (toolbar, false, false, 0);
			
			buttonSuccess = new ToggleToolButton ();
			buttonSuccess.Label = "Successful Tests";
			buttonSuccess.Active = false;
			buttonSuccess.IconWidget = new Gtk.Image (CircleImage.Success);
			buttonSuccess.IsImportant = true;
			buttonSuccess.Toggled += new EventHandler (OnShowSuccessfulToggled);
			buttonSuccess.SetTooltip (tips, "Show Successful Tests", "Show Successful Tests");
			toolbar.Insert (buttonSuccess, -1);
			
			buttonFailures = new ToggleToolButton ();
			buttonFailures.Label = "Failed Tests";
			buttonFailures.Active = true;
			buttonFailures.IconWidget = new Gtk.Image (CircleImage.Failure);
			buttonFailures.IsImportant = true;
			buttonFailures.Toggled += new EventHandler (OnShowFailuresToggled);
			buttonFailures.SetTooltip (tips, "Show Failed Tests", "Show Failed Tests");
			toolbar.Insert (buttonFailures, -1);
			
			buttonIgnored = new ToggleToolButton ();
			buttonIgnored.Label = "Ignored Tests";
			buttonIgnored.Active = true;
			buttonIgnored.IconWidget = new Gtk.Image (CircleImage.NotRun);
			buttonIgnored.Toggled += new EventHandler (OnShowIgnoredToggled);
			buttonIgnored.IsImportant = true;
			buttonIgnored.SetTooltip (tips, "Show Ignored Tests", "Show Ignored Tests");
			toolbar.Insert (buttonIgnored, -1);
			
			buttonOutput = new ToggleToolButton ();
			buttonOutput.Label = "Output";
			buttonOutput.Active = false;
			buttonOutput.IconWidget = Runtime.Gui.Resources.GetImage (MonoDevelop.Gui.Stock.OutputIcon, IconSize.SmallToolbar);
			buttonOutput.Toggled += new EventHandler (OnShowOutputToggled);
			buttonOutput.IsImportant = true;
			buttonOutput.SetTooltip (tips, "Show Output", "Show Output");
			toolbar.Insert (buttonOutput, -1);
			
			toolbar.Insert (new SeparatorToolItem (), -1);
			
			buttonStop = new ToolButton ("gtk-stop");
			toolbar.Insert (buttonStop, -1);
			
			toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
			toolbar.ShowArrow = false;
			
			// Results notebook
			
			book = new HPaned ();
			panel.PackStart (book, true, true, 0);
			
			// Failures tree
			failuresTreeView = new TreeView ();
			failuresTreeView.HeadersVisible = false;
			failuresStore = new TreeStore (typeof(Pixbuf), typeof (string), typeof(object));
			CellRendererPixbuf pr = new CellRendererPixbuf ();
			CellRendererText tr = new CellRendererText ();
			TreeViewColumn col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", 0);
			col.PackStart (tr, false);
			col.AddAttribute (tr, "markup", 1);
			failuresTreeView.AppendColumn (col);
			failuresTreeView.Model = failuresStore;
		
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.Add(failuresTreeView);
			Frame frame = new Frame ();
			frame.Add (sw);
			book.Pack1 (frame, true, true);
			
			outputView = new TextView();
			outputView.Editable = false;
			sw = new Gtk.ScrolledWindow ();
			sw.Add(outputView);
			frame = new Frame ();
			frame.Add (sw);
			book.Pack2 (frame, true, true);
			outputViewScrolled = frame;
			
			// Run panel
			
			HBox runPanel = new HBox ();
			runPanel.BorderWidth = 5;
			
			infoSep = new VSeparator ();
			
			resultLabel.UseMarkup = true;
			runPanel.PackStart (resultLabel, false, false, 0);
			runPanel.PackStart (progressBar, false, false, 0);
			runPanel.PackStart (infoCurrent, true, true, 10);	
			
			labels = new HBox (false, 10);
			
			infoFailed.UseMarkup = true;
			infoIgnored.UseMarkup = true;
			
			labels.PackStart (infoFailed, true, false, 0);
			labels.PackStart (infoIgnored, true, false, 0);
			
			runPanel.PackEnd (labels, false, false, 0);
			runPanel.PackEnd (infoSep, false, false, 10);
			
			panel.PackStart (runPanel, false, false, 0);
			progressBar.HeightRequest = infoFailed.SizeRequest().Height;
			
			buttonStop.Clicked += new EventHandler (OnStopClicked);
			failuresTreeView.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnPopupMenu);
			
			Control.ShowAll ();
			
			outputViewScrolled.Hide ();
		}
		
		public void Dispose ()
		{
		}
		
		public void OnTestSuiteChanged (object sender, EventArgs e)
		{
			results.Clear ();
			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			progressBar.Fraction = 0;
			progressBar.Text = "";
			testsRun = 0;
			testsFailed = 0;
			testsIgnored = 0;
			UpdateCounters ();
		}
		
		public string Id {
			get { return "MonoDevelop.NUnit.TestResultsPad"; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public string Title {
			get {
				if (running) 
					return "<span foreground=\"blue\">" + title + "</span>";
				else
					return title;
			}
		}
		
		public string Icon {
			get { return "md-combine-icon"; }
		}
		
		public Gtk.Widget Control {
			get {
				return panel;
			}
		}
		
		public void RedrawContent ()
		{
		}
		
		public event EventHandler TitleChanged;
		public event EventHandler IconChanged;
		
		void UpdateCounters ()
		{
			infoFailed.Markup = "<b>Failed</b>: " + testsFailed;
			infoIgnored.Markup = "<b>Ignored</b>: " + testsIgnored;
		}
		
		public void InitializeTestRun (UnitTest test)
		{
			rootTest = test;
			results.Clear ();
			testsToRun = test.CountTestCases ();
			progressBar.Fraction = 0;
			progressBar.Text = "";
			progressBar.Text = "0 / " + testsToRun;

			testsRun = 0;
			testsFailed = 0;
			testsIgnored = 0;
			UpdateCounters ();
			
			infoSep.Show ();
			infoCurrent.Show ();
			progressBar.Show ();
			resultLabel.Hide ();
			labels.Show ();
			buttonStop.Sensitive = true;
			
			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			cancel = false;
			running = true;
			
			configuration = rootTest.ActiveConfiguration;
			
			AddStartMessage ();
			OnTitleChanged ();
		}
		
		public void AddStartMessage ()
		{
			Gdk.Pixbuf infoIcon = failuresTreeView.RenderIcon (Gtk.Stock.DialogInfo, Gtk.IconSize.SmallToolbar, "");
			string msg = string.Format (GettextCatalog.GetString ("Running tests for <b>{0}</b> configuration <b>{1}</b>"), rootTest.Name, configuration);
			failuresStore.AppendValues (infoIcon, msg, rootTest);
		}

		public void ReportRuntimeError (string message, Exception exception)
		{
			error = exception;
			errorMessage = message;
			AddErrorMessage ();
		}
		
		public void AddErrorMessage ()
		{
			string msg = "Internal error";
			if (errorMessage != null)
				msg += ": " + errorMessage;

			Gdk.Pixbuf stock = failuresTreeView.RenderIcon (Gtk.Stock.DialogError, Gtk.IconSize.SmallToolbar, "");
			TreeIter testRow = failuresStore.AppendValues (stock, msg, null);
			failuresStore.AppendValues (testRow, null, Escape (error.GetType().Name + ": " + error.Message), null);
			TreeIter row = failuresStore.AppendValues (testRow, null, "Stack Trace", null);
			failuresStore.AppendValues (row, null, Escape (error.StackTrace), null);
		}
		
		public void FinishTestRun ()
		{
			infoCurrent.Text = "";
			progressBar.Fraction = 1;
			progressBar.Text = "";
			
			infoSep.Hide ();
			infoCurrent.Hide ();
			progressBar.Hide ();
			resultLabel.Show ();
			labels.Hide ();
			buttonStop.Sensitive = false;
			
			resultLabel.Markup = "<b>Tests</b>: " + testsRun + "  <b>Failed</b>: " + testsFailed + "  <b>Ignored</b>: " + testsIgnored;
			
			running = false;
			OnTitleChanged ();
		}
		
		void OnStopClicked (object sender, EventArgs args)
		{
			Cancel ();
			failuresStore.AppendValues (CircleImage.Failure, "Test execution cancelled.", null);
		}
		
		void OnPopupMenu (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				Runtime.Gui.CommandService.ShowContextMenu ("/SharpDevelop/Views/TestChart/ContextMenu");
			}
		}
		
		[CommandHandler (TestCommands.SelectTestInTree)]
		protected void OnSelectTestInTree ()
		{
			TestPad pad = (TestPad) Runtime.Gui.Workbench.GetPad (typeof (TestPad));
			pad.SelectTest (GetSelectedTest ());
		}
		
		[CommandUpdateHandler (TestCommands.SelectTestInTree)]
		protected void OnUpdateSelectTestInTree (CommandInfo info)
		{
			UnitTest test = GetSelectedTest ();
			info.Enabled = test != null;
		}
		
		[CommandHandler (TestCommands.ShowTestCode)]
		protected void OnShowTest ()
		{
			UnitTest test = GetSelectedTest ();
			if (test == null)
				return;
			SourceCodeLocation loc = test.SourceCodeLocation;
			if (loc != null)
				Runtime.FileService.OpenFile (loc.FileName, loc.Line, loc.Column, true);
		}
		
		[CommandUpdateHandler (TestCommands.ShowTestCode)]
		protected void OnUpdateRunTest (CommandInfo info)
		{
			UnitTest test = GetSelectedTest ();
			info.Enabled = test != null && test.SourceCodeLocation != null;
		}
		
		UnitTest GetSelectedTest ()
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!failuresTreeView.Selection.GetSelected (out foo, out iter))
				return null;
				
			UnitTest t = (UnitTest) failuresStore.GetValue (iter, 2);
			return t;
		}
		
		void OnShowSuccessfulToggled (object sender, EventArgs args)
		{
			RefreshList ();
		}

		void OnShowFailuresToggled (object sender, EventArgs args)
		{
			RefreshList ();
		}
		
		void OnShowIgnoredToggled (object sender, EventArgs args)
		{
			RefreshList ();
		}
		
		void OnShowOutputToggled (object sender, EventArgs args)
		{
			outputViewScrolled.Visible = buttonOutput.Active;
		}
		
		void RefreshList ()
		{
			failuresStore.Clear ();
			AddStartMessage ();
				
			foreach (ResultRecord res in results) {
				ShowTestResult (res.Test, res.Result);
			}
			
			if (error != null)
				AddErrorMessage ();
		}
		
		void ShowTestResult (UnitTest test, UnitTestResult result)
		{
			if (result.IsSuccess) {
				if (!buttonSuccess.Active)
					return;
				TreeIter testRow = failuresStore.AppendValues (CircleImage.Success, Escape (test.FullName), test);
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.IsFailure) {
				if (!buttonFailures.Active)
					return;
				TreeIter testRow = failuresStore.AppendValues (CircleImage.Failure, Escape (test.FullName), test);
				bool hasMessage = result.Message != null && result.Message.Length > 0;
				if (hasMessage)
					failuresStore.AppendValues (testRow, null, Escape (result.Message), test);
				if (result.StackTrace != null && result.StackTrace.Length > 0) {
					TreeIter row = testRow;
					if (hasMessage)
						row = failuresStore.AppendValues (testRow, null, "Stack Trace", test);
					failuresStore.AppendValues (row, null, Escape (result.StackTrace), test);
				}
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.IsIgnored) {
				if (!buttonIgnored.Active)
					return;
				TreeIter testRow = failuresStore.AppendValues (CircleImage.NotRun, Escape (test.FullName), test);
				failuresStore.AppendValues (testRow, null, Escape (result.Message), test);
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.ConsoleOutput != null)
				outputView.Buffer.InsertAtCursor (result.ConsoleOutput);
			if (result.ConsoleError != null)
				outputView.Buffer.InsertAtCursor (result.ConsoleError);
			outputView.ScrollMarkOnscreen (outputView.Buffer.InsertMark);
		}
		
		string Escape (string s)
		{
			return s.Replace ("<","&lt;").Replace (">","&gt;");
		}
		
		void ITestProgressMonitor.EndTest (UnitTest test, UnitTestResult result)
		{
			if (test is UnitTestGroup)
				return;
			
			testsRun++;
			ResultRecord rec = new ResultRecord ();
			rec.Test = test;
			rec.Result = result;
			
			if (result.IsFailure) {
				testsFailed++;
			}
			if (result.IsIgnored) {
				testsIgnored++;
			}
			results.Add (rec);
			
			ShowTestResult (test, result);
			
			UpdateCounters ();
			progressBar.Fraction = ((double)testsRun / (double)testsToRun);
			progressBar.Text = testsRun + " / " + testsToRun;
		}
		
		void ITestProgressMonitor.BeginTest (UnitTest test)
		{
			infoCurrent.Text = "Running " + test.FullName;
			infoCurrent.Xalign = 0;
		}
		
		public void Cancel ()
		{
			cancel = true;
			if (CancelRequested != null)
				CancelRequested ();
		}
		
		bool ITestProgressMonitor.IsCancelRequested {
			get { return cancel; }
		}
		
		public event TestHandler CancelRequested;

		protected virtual void OnTitleChanged ()
		{
			if (TitleChanged != null) {
				TitleChanged(this, null);
			}
		}
	}
}

