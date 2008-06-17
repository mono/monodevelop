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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.NUnit.Commands;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.NUnit
{
	class TestResultsPad: IPadContent, ITestProgressMonitor
	{
		NUnitService testService = (NUnitService) ServiceManager.GetService (typeof(NUnitService));
		
		IPadWindow window;
		VBox panel;
		HPaned book;
		Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		Label infoFailed = new Label (GettextCatalog.GetString ("<b>Failed</b>: {0}", 0));
		Label infoIgnored = new Label (GettextCatalog.GetString ("<b>Ignored</b>: {0}", 0));
		Label infoCurrent = new Label ();
		HBox labels;
		
		Label resultLabel = new Label ();
		
		ProgressBar progressBar = new ProgressBar ();
		TreeView failuresTreeView;
		TreeStore failuresStore;
		TextView outputView;
		TextTag bold;
		Dictionary<UnitTest,int> outIters = new Dictionary<UnitTest,int> ();
		Widget outputViewScrolled;
		VSeparator infoSep;
		
		ToolButton buttonStop;
		ToolButton buttonRun;
		
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
			testService.TestSuiteChanged += new EventHandler (OnTestSuiteChanged);
			
			panel = new VBox ();
			
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
			panel.PackStart (toolbar, false, false, 0);
			
			buttonSuccess = new ToggleToolButton ();
			buttonSuccess.Label = GettextCatalog.GetString ("Successful Tests");
			buttonSuccess.Active = false;
			buttonSuccess.IconWidget = new Gtk.Image (CircleImage.Success);
			buttonSuccess.IsImportant = true;
			buttonSuccess.Toggled += new EventHandler (OnShowSuccessfulToggled);
			buttonSuccess.SetTooltip (tips, GettextCatalog.GetString ("Show Successful Tests"), GettextCatalog.GetString ("Show Successful Tests"));
			toolbar.Insert (buttonSuccess, -1);
			
			buttonFailures = new ToggleToolButton ();
			buttonFailures.Label = GettextCatalog.GetString ("Failed Tests");
			buttonFailures.Active = true;
			buttonFailures.IconWidget = new Gtk.Image (CircleImage.Failure);
			buttonFailures.IsImportant = true;
			buttonFailures.Toggled += new EventHandler (OnShowFailuresToggled);
			buttonFailures.SetTooltip (tips, GettextCatalog.GetString ("Show Failed Tests"), GettextCatalog.GetString ("Show Failed Tests"));
			toolbar.Insert (buttonFailures, -1);
			
			buttonIgnored = new ToggleToolButton ();
			buttonIgnored.Label = GettextCatalog.GetString ("Ignored Tests");
			buttonIgnored.Active = true;
			buttonIgnored.IconWidget = new Gtk.Image (CircleImage.NotRun);
			buttonIgnored.Toggled += new EventHandler (OnShowIgnoredToggled);
			buttonIgnored.IsImportant = true;
			buttonIgnored.SetTooltip (tips, GettextCatalog.GetString( "Show Ignored Tests"), GettextCatalog.GetString ("Show Ignored Tests"));
			toolbar.Insert (buttonIgnored, -1);
			
			buttonOutput = new ToggleToolButton ();
			buttonOutput.Label = GettextCatalog.GetString ("Output");
			buttonOutput.Active = false;
			buttonOutput.IconWidget = Services.Resources.GetImage (MonoDevelop.Core.Gui.Stock.OutputIcon, IconSize.Menu);
			buttonOutput.Toggled += new EventHandler (OnShowOutputToggled);
			buttonOutput.IsImportant = true;
			buttonOutput.SetTooltip (tips, GettextCatalog.GetString ("Show Output"), GettextCatalog.GetString ("Show Output"));
			toolbar.Insert (buttonOutput, -1);
			
			toolbar.Insert (new SeparatorToolItem (), -1);
			
			buttonRun = new ToolButton (new Gtk.Image (Gtk.Stock.Execute, IconSize.Menu), GettextCatalog.GetString ("Run Test"));
			buttonRun.IsImportant = true;
			buttonRun.Sensitive = false;
			toolbar.Insert (buttonRun, -1);
			
			buttonStop = new ToolButton (Gtk.Stock.Stop);
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
			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			outputView.Buffer.TagTable.Add (bold);
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
			infoCurrent.Ellipsize = Pango.EllipsizeMode.Start;
			infoCurrent.WidthRequest = 0;
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
			buttonRun.Clicked += new EventHandler (OnRunClicked);
			failuresTreeView.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnPopupMenu);
			failuresTreeView.RowActivated += OnRowActivated;
			failuresTreeView.Selection.Changed += OnRowSelected;
			
			Control.ShowAll ();
			
			outputViewScrolled.Hide ();
		}
		
		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
		}
		
		public void Dispose ()
		{
		}
		
		public void OnTestSuiteChanged (object sender, EventArgs e)
		{
			results.Clear ();
			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			outIters.Clear ();
			progressBar.Fraction = 0;
			progressBar.Text = "";
			testsRun = 0;
			testsFailed = 0;
			testsIgnored = 0;
			UpdateCounters ();
			if (rootTest != null) {
				rootTest = testService.SearchTest (rootTest.FullName);
				if (rootTest == null)
					buttonRun.Sensitive = false;
			}
		}
		
		bool Running {
			get { return running; }
			set {
				running = value;
				string title = GettextCatalog.GetString ("Test results");
				if (running) 
					window.Title = "<span foreground=\"blue\">" + title + "</span>";
				else
					window.Title = title;
			}
		}
		
		public Gtk.Widget Control {
			get {
				return panel;
			}
		}
		
		public void RedrawContent ()
		{
		}
		
		void UpdateCounters ()
		{
			infoFailed.Markup = GettextCatalog.GetString ("<b>Failed</b>: {0}", testsFailed);
			infoIgnored.Markup = GettextCatalog.GetString ("<b>Ignored</b>: {0}", testsIgnored);
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
			buttonRun.Sensitive = false;
			
			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			outIters.Clear ();
			cancel = false;
			Running = true;
			
			configuration = IdeApp.Workspace.ActiveConfiguration;
			
			AddStartMessage ();
		}
		
		public void AddStartMessage ()
		{
			if (rootTest != null) {
				Gdk.Pixbuf infoIcon = failuresTreeView.RenderIcon (Gtk.Stock.DialogInfo, Gtk.IconSize.Menu, "");
				string msg = string.Format (GettextCatalog.GetString ("Running tests for <b>{0}</b> configuration <b>{1}</b>"), rootTest.Name, configuration);
				failuresStore.AppendValues (infoIcon, msg, rootTest);
			}
		}

		public void ReportRuntimeError (string message, Exception exception)
		{
			error = exception;
			errorMessage = message;
			AddErrorMessage ();
		}
		
		public void AddErrorMessage ()
		{
			string msg = GettextCatalog.GetString ("Internal error");
			if (errorMessage != null)
				msg += ": " + errorMessage;

			Gdk.Pixbuf stock = failuresTreeView.RenderIcon (Gtk.Stock.DialogError, Gtk.IconSize.Menu, "");
			TreeIter testRow = failuresStore.AppendValues (stock, msg, null);
			failuresStore.AppendValues (testRow, null, Escape (error.GetType().Name + ": " + error.Message), null);
			TreeIter row = failuresStore.AppendValues (testRow, null, GettextCatalog.GetString ("Stack Trace"), null);
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
			buttonRun.Sensitive = true;
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (GettextCatalog.GetString ("<b>Tests</b>: {0}", testsRun)).Append ("  ");
			sb.Append (GettextCatalog.GetString ("<b>Failed</b>: {0}", testsFailed)).Append ("  ");
			sb.Append (GettextCatalog.GetString ("<b>Ignored</b>: {0}", testsIgnored));
			resultLabel.Markup = sb.ToString ();
			
			Running = false;
		}
		
		void OnStopClicked (object sender, EventArgs args)
		{
			if (running) {
				Cancel ();
				failuresStore.AppendValues (CircleImage.Failure, GettextCatalog.GetString ("Test execution cancelled."), null);
			}
		}
		
		void OnRunClicked (object sender, EventArgs args)
		{
			if (rootTest == null)
				return;
			NUnitService testService = (NUnitService) ServiceManager.GetService (typeof(NUnitService));
			testService.RunTest (rootTest);
		}
		
		void OnPopupMenu (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				IdeApp.CommandService.ShowContextMenu ("/MonoDevelop/NUnit/ContextMenu/TestResultsPad");
			}
		}

		void OnRowActivated (object s, EventArgs a)
		{
			OnShowTest ();
		}
		
		void OnRowSelected (object s, EventArgs a)
		{
			UnitTest test = GetSelectedTest ();
			if (test != null) {
				int offset;
				if (outIters.TryGetValue (test, out offset)) {
					TextIter it = outputView.Buffer.GetIterAtOffset (offset);
					outputView.Buffer.MoveMark (outputView.Buffer.InsertMark, it);
					outputView.Buffer.MoveMark (outputView.Buffer.SelectionBound, it);
					outputView.ScrollToMark (outputView.Buffer.InsertMark, 0.0, true, 0.0, 0.0);
				}
			}
		}
		
		[CommandHandler (TestCommands.SelectTestInTree)]
		protected void OnSelectTestInTree ()
		{
			Pad pad = IdeApp.Workbench.GetPad<TestPad> ();
			pad.BringToFront ();
			TestPad content = (TestPad) pad.Content;
			content.SelectTest (GetSelectedTest ());
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
			UnitTestResult res = test.GetLastResult ();
			string stack = res != null ? res.StackTrace : null;
			SourceCodeLocation loc = GetSourceCodeLocation (test, stack);
			if (loc != null)
				IdeApp.Workbench.OpenDocument (loc.FileName, loc.Line, loc.Column, true);
		}
		
		[CommandUpdateHandler (TestCommands.ShowTestCode)]
		protected void OnUpdateRunTest (CommandInfo info)
		{
			UnitTest test = GetSelectedTest ();
			info.Enabled = test != null && test.SourceCodeLocation != null;
		}
		
		SourceCodeLocation GetSourceCodeLocation (UnitTest test, string stackTrace)
		{
			if (!String.IsNullOrEmpty (stackTrace)) {
				Match match = Regex.Match (stackTrace, @"\sin\s(.*?):(\d+)", RegexOptions.Multiline);
				while (match.Success) {
					try	{
						int line = Int32.Parse (match.Groups[2].Value);
						return new SourceCodeLocation (match.Groups[1].Value, line, 1);
					} catch (Exception) {
					}
					match = match.NextMatch ();
				}
			}
			return test.SourceCodeLocation;
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
			outputView.Buffer.Clear ();
			outIters.Clear ();
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
						row = failuresStore.AppendValues (testRow, null, GettextCatalog.GetString ("Stack Trace"), test);
					failuresStore.AppendValues (row, null, Escape (result.StackTrace), test);
				}
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.IsIgnored) {
				if (!buttonIgnored.Active)
					return;
				TreeIter testRow = failuresStore.AppendValues (CircleImage.NotRun, Escape (test.FullName), test);
				if (result.Message != null)
					failuresStore.AppendValues (testRow, null, Escape (result.Message), test);
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			
			string msg = GettextCatalog.GetString ("Running {0} ...", test.FullName);
			TextIter it = outputView.Buffer.GetIterAtMark (outputView.Buffer.InsertMark);
			outIters [test] = it.Offset;
			outputView.Buffer.InsertWithTags (ref it, msg + "\n", bold);
			if (result.ConsoleOutput != null)
				outputView.Buffer.InsertAtCursor (result.ConsoleOutput);
			if (result.ConsoleError != null)
				outputView.Buffer.InsertAtCursor (result.ConsoleError);
			outputView.ScrollMarkOnscreen (outputView.Buffer.InsertMark);
		}
		
		string Escape (string s)
		{
			return GLib.Markup.EscapeText (s);
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

			double frac;
			if (testsToRun != 0)
				frac = ((double)testsRun / (double)testsToRun);
			else
				frac = 1;
			progressBar.Fraction = frac;
			progressBar.Text = testsRun + " / " + testsToRun;
		}
		
		void ITestProgressMonitor.BeginTest (UnitTest test)
		{
			infoCurrent.Text = GettextCatalog.GetString ("Running ") + test.FullName;
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
	}
	
	class TestMonitor: GuiSyncObject, ITestProgressMonitor
	{
		// TestResultsPad can't be a GuiSyncObject because there are some
		// object identity issues. If the pad is registered using the
		// proxy, it will get different hash codes depending on the caller.
		
		ITestProgressMonitor monitor;
		TestResultsPad pad;
		
		public TestMonitor (TestResultsPad pad) {
			this.pad = pad;
			this.monitor = pad;
		}
		public void InitializeTestRun (UnitTest test) {
			pad.InitializeTestRun (test);
		}
		public void FinishTestRun () {
			pad.FinishTestRun ();
		}
		public void Cancel () {
			pad.Cancel ();
		}
		public void BeginTest (UnitTest test) {
			monitor.BeginTest (test);
		}
		public void EndTest (UnitTest test, UnitTestResult result) {
			monitor.EndTest (test, result);
		}
		public void ReportRuntimeError (string message, Exception exception) {
			monitor.ReportRuntimeError (message, exception);
		}
		public bool IsCancelRequested {
			get { return monitor.IsCancelRequested; }
		}
		public event TestHandler CancelRequested {
			add { monitor.CancelRequested += value; }
			remove { monitor.CancelRequested -= value; }
		}
	}
}

