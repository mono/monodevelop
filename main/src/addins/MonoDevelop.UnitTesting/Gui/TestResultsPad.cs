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
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Commands;
using MonoDevelop.UnitTesting.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using System.Text.RegularExpressions;
using MonoDevelop.Components;
using System.Threading;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Components.AutoTest;
using System.ComponentModel;

namespace MonoDevelop.UnitTesting
{
	class TestResultsPad: PadContent, ITestProgressMonitor
	{
		IPadWindow window;
		VBox panel;
		HPaned book;
		
		Label infoLabel = new Label ();
		Label infoCurrent = new Label ();
		HBox labels;
		
		Label resultLabel = new Label ();
		
		ProgressBar progressBar = new ProgressBar ();
		MonoDevelop.Ide.Gui.Components.PadTreeView failuresTreeView;
		TreeStore failuresStore;
		TextView outputView;
		TextTag bold;
		Dictionary<UnitTest,int> outIters = new Dictionary<UnitTest,int> ();
		Widget outputViewScrolled;
		VSeparator infoSep;
		Gtk.TreeIter startMessageIter;
		
		Button buttonStop;
		Button buttonRun;
		
		ToggleButton buttonSuccess;
		ToggleButton buttonFailures;
		ToggleButton buttonIgnored;
		ToggleButton buttonInconclusive;
		ToggleButton buttonOutput;
		
		bool running;
		int testsToRun;
		int testsRun;

		UnitTestResult resultSummary;

		UnitTest rootTest;
		CancellationTokenSource cancellationSource;
		string configuration;
		ArrayList results = new ArrayList ();
		
		Exception error;
		string errorMessage;
		
		bool cancel;
		
		public class ResultRecord
		{
			public UnitTest Test;
			public UnitTestResult Result;
		}
		
		public TestResultsPad ()
		{
			UnitTestService.TestSuiteChanged += new EventHandler (OnTestSuiteChanged);
			IdeApp.Workspace.WorkspaceItemClosed += OnWorkspaceItemClosed;

			panel = new VBox { Name = "testResultBox" };
			
			// Results notebook
			
			book = new HPaned ();
			panel.PackStart (book, true, true, 0);
			panel.FocusChain = new Gtk.Widget [] { book };

			// Failures tree
			failuresTreeView = new MonoDevelop.Ide.Gui.Components.PadTreeView { Name = "testResultsTree" };
			failuresTreeView.HeadersVisible = false;
			failuresStore = new TreeStore (typeof(Xwt.Drawing.Image), typeof(string), typeof(object), typeof(string), typeof(int), typeof(int));
			SemanticModelAttribute modelAttr = new SemanticModelAttribute ("store__Image", "store__Message","store__RootTest",
				"store__FileName", "store__FileNumber", "store__ErrorOrStackTrace");
			TypeDescriptor.AddAttributes (failuresStore, modelAttr);
			
			var pr = new CellRendererImage ();
			CellRendererText tr = new CellRendererText ();
			TreeViewColumn col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "image", 0);
			col.PackStart (tr, false);
			col.AddAttribute (tr, "markup", 1);
			failuresTreeView.AppendColumn (col);
			failuresTreeView.Model = failuresStore;
		
			var sw = new MonoDevelop.Components.CompactScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (failuresTreeView);
			book.Pack1 (sw, true, true);

			outputView = new MonoDevelop.Ide.Gui.Components.LogView.LogTextView { Name = "testResultOutput" };
			outputView.ModifyFont (FontService.MonospaceFont);
			outputView.Editable = false;
			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			outputView.Buffer.TagTable.Add (bold);
			sw = new MonoDevelop.Components.CompactScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add (outputView);
			book.Pack2 (sw, true, true);
			outputViewScrolled = sw;
			
			failuresTreeView.RowActivated += OnRowActivated;
			failuresTreeView.Selection.Changed += OnRowSelected;
			failuresTreeView.DoPopupMenu = delegate (EventButton evt) {
				IdeApp.CommandService.ShowContextMenu (failuresTreeView, evt,
					"/MonoDevelop/UnitTesting/ContextMenu/TestResultsPad");
			};
			
			panel.ShowAll ();
			
			outputViewScrolled.Hide ();
		}
		
		protected override void Initialize (IPadWindow window)
		{
			this.window = window;
			
			DockItemToolbar toolbar = window.GetToolbar (DockPositionType.Top);
			
			buttonSuccess = new ToggleButton ();
			buttonSuccess.Label = GettextCatalog.GetString ("Successful Tests");
			buttonSuccess.Accessible.Name = "TestResultsPad.SuccessfulTests";
			buttonSuccess.Accessible.Description = GettextCatalog.GetString ("Show the results for the successful tests");
			buttonSuccess.Active = false;
			buttonSuccess.Image = new ImageView (TestStatusIcon.Success);
			buttonSuccess.Image.Show ();
			buttonSuccess.Toggled += new EventHandler (OnShowSuccessfulToggled);
			buttonSuccess.TooltipText = GettextCatalog.GetString ("Show Successful Tests");
			toolbar.Add (buttonSuccess);

			buttonInconclusive = new ToggleButton ();
			buttonInconclusive.Label = GettextCatalog.GetString ("Inconclusive Tests");
			buttonInconclusive.Accessible.Name = "TestResultsPad.InconclusiveTests";
			buttonInconclusive.Accessible.Description = GettextCatalog.GetString ("Show the results for the inconclusive tests");
			buttonInconclusive.Active = true;
			buttonInconclusive.Image = new ImageView (TestStatusIcon.Inconclusive);
			buttonInconclusive.Image.Show ();
			buttonInconclusive.Toggled += new EventHandler (OnShowInconclusiveToggled);
			buttonInconclusive.TooltipText = GettextCatalog.GetString ("Show Inconclusive Tests");
			toolbar.Add (buttonInconclusive);
			
			buttonFailures = new ToggleButton ();
			buttonFailures.Label = GettextCatalog.GetString ("Failed Tests");
			buttonFailures.Accessible.Name = "TestResultsPad.FailedTests";
			buttonFailures.Accessible.Description = GettextCatalog.GetString ("Show the results for the failed tests");
			buttonFailures.Active = true;
			buttonFailures.Image = new ImageView (TestStatusIcon.Failure);
			buttonFailures.Image.Show ();
			buttonFailures.Toggled += new EventHandler (OnShowFailuresToggled);
			buttonFailures.TooltipText = GettextCatalog.GetString ("Show Failed Tests");
			toolbar.Add (buttonFailures);

			buttonIgnored = new ToggleButton ();
			buttonIgnored.Label = GettextCatalog.GetString ("Ignored Tests");
			buttonIgnored.Accessible.Name = "TestResultsPad.IgnoredTests";
			buttonIgnored.Accessible.Description = GettextCatalog.GetString ("Show the results for the ignored tests");
			buttonIgnored.Active = true;
			buttonIgnored.Image = new ImageView (TestStatusIcon.NotRun);
			buttonIgnored.Image.Show ();
			buttonIgnored.Toggled += new EventHandler (OnShowIgnoredToggled);
			buttonIgnored.TooltipText = GettextCatalog.GetString ("Show Ignored Tests");
			toolbar.Add (buttonIgnored);
			
			buttonOutput = new ToggleButton ();
			buttonOutput.Label = GettextCatalog.GetString ("Output");
			buttonOutput.Accessible.Name = "TestResultsPad.Output";
			buttonOutput.Accessible.Description = GettextCatalog.GetString ("Show the test output");
			buttonOutput.Active = false;
			buttonOutput.Image = new ImageView (MonoDevelop.Ide.Gui.Stock.OutputIcon, IconSize.Menu);
			buttonOutput.Image.Show ();
			buttonOutput.Toggled += new EventHandler (OnShowOutputToggled);
			buttonOutput.TooltipText = GettextCatalog.GetString ("Show Output");
			toolbar.Add (buttonOutput);
			
			toolbar.Add (new SeparatorToolItem ());
			
			buttonRun = new Button ();
			buttonRun.Label = GettextCatalog.GetString ("Rerun Tests");
			buttonRun.Accessible.Name = "TestResultsPad.Run";
			buttonRun.Accessible.Description = GettextCatalog.GetString ("Start a test run and run all the tests");
			buttonRun.Image = new ImageView ("md-execute-all", IconSize.Menu);
			buttonRun.Image.Show ();
			buttonRun.Sensitive = false;
			toolbar.Add (buttonRun);
			
			buttonStop = new Button (new ImageView (Ide.Gui.Stock.Stop, Gtk.IconSize.Menu));
			buttonStop.Accessible.Name = "TestResultsPad.Stop";
			buttonStop.Accessible.SetTitle (GettextCatalog.GetString ("Stop"));
			buttonStop.Accessible.Description = GettextCatalog.GetString ("Stop the current test run");
			toolbar.Add (buttonStop);
			toolbar.ShowAll ();
			
			buttonStop.Clicked += new EventHandler (OnStopClicked);
			buttonRun.Clicked += new EventHandler (OnRunClicked);
			
			// Run panel
			
			DockItemToolbar runPanel = window.GetToolbar (DockPositionType.Bottom);
			
			infoSep = new VSeparator ();
			
			resultLabel.UseMarkup = true;
			infoCurrent.Ellipsize = Pango.EllipsizeMode.Start;
			infoCurrent.WidthRequest = 0;
			runPanel.Add (resultLabel);
			runPanel.Add (progressBar);
			runPanel.Add (infoCurrent, true, 10);	
			
			labels = new HBox (false, 10);
			
			infoLabel.UseMarkup = true;

			labels.PackStart (infoLabel, true, false, 0);

			runPanel.Add (new Gtk.Label (), true);
			runPanel.Add (labels);
			runPanel.Add (infoSep, false, 10);
			
			progressBar.HeightRequest = infoLabel.SizeRequest ().Height;
			runPanel.ShowAll ();
			progressBar.Hide ();
			infoSep.Hide ();
			resultSummary = new UnitTestResult ();
			UpdateCounters ();
		}
		
		public void OnTestSuiteChanged (object sender, EventArgs e)
		{
			if (rootTest != null) {
				rootTest = UnitTestService.SearchTest (rootTest.FullName);
				if (rootTest == null)
					buttonRun.Sensitive = false;
			}
		}

		void OnWorkspaceItemClosed (object sender, EventArgs e)
		{
			ClearResults ();
		}

		void ClearResults ()
		{
			if (failuresTreeView.IsRealized)
				failuresTreeView.ScrollToPoint (0, 0);

			results.Clear ();

			error = null;
			errorMessage = null;

			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			outIters.Clear ();
			progressBar.Fraction = 0;
			progressBar.Text = "";
			testsRun = 0;
			resultSummary = new UnitTestResult ();
			resultLabel.Markup = "";
			resultLabel.Hide ();
			labels.Show ();
			UpdateCounters ();
		}
		
		bool Running {
			get { return running; }
			set {
				running = value;
				window.IsWorking = value;
			}
		}
		
		public override Control Control {
			get {
				return panel;
			}
		}
		
		string GetResultsMarkup ()
		{
			return string.Format (GettextCatalog.GetString ("<b>Passed</b>: {0}   <b>Failed</b>: {1}   <b>Errors</b>: {2}   <b>Inconclusive</b>: {3}   <b>Invalid</b>: {4}   <b>Ignored</b>: {5}   <b>Skipped</b>: {6}   <b>Time</b>: {7}"), 
					                        resultSummary.Passed, resultSummary.Failures, resultSummary.Errors, resultSummary.Inconclusive, resultSummary.NotRunnable, resultSummary.Ignored, resultSummary.Skipped, resultSummary.Time);
		}
		void UpdateCounters ()
		{
			infoLabel.Markup = GetResultsMarkup ();
		}

		public void InitializeTestRun (UnitTest test, CancellationTokenSource cs)
		{
			rootTest = test;

			cancellationSource = cs;
			if (cs != null)
				cs.Token.Register (OnCancel);
			
			results.Clear ();

			testsToRun = test != null ? test.CountTestCases () : 0;
			error = null;
			errorMessage = null;
			
			progressBar.Fraction = 0;
			progressBar.Text = "";
			progressBar.Text = "0 / " + testsToRun;
			testsRun = 0;
			resultSummary = new UnitTestResult ();
			UpdateCounters ();
			
			infoSep.Show ();
			infoCurrent.Show ();
			progressBar.Show ();
			resultLabel.Hide ();
			labels.Show ();
			buttonStop.Sensitive = true;
			buttonRun.Sensitive = false;

			if (failuresTreeView.IsRealized)
				failuresTreeView.ScrollToPoint (0, 0);
			
			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			outIters.Clear ();
			cancel = false;
			Running = true;
			
			configuration = IdeApp.Workspace.ActiveConfigurationId;
			
			AddStartMessage ();
		}
		
		public void AddStartMessage (bool isRunning = true)
		{
			if (rootTest != null) {
				var infoIcon = ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.Information, Gtk.IconSize.Menu);
				string msg = string.Format (isRunning ? GettextCatalog.GetString ("Running tests for <b>{0}</b> configuration <b>{1}</b>") : GettextCatalog.GetString ("Test results for <b>{0}</b> configuration <b>{1}</b>"), rootTest.Name, configuration);
				startMessageIter = failuresStore.AppendValues (infoIcon, msg, rootTest, null, 0);
			} else {
				startMessageIter = Gtk.TreeIter.Zero;
			}
		}

		public void ReportRuntimeError (string message, Exception exception)
		{
			error = exception;
			errorMessage = message;
			AddErrorMessage ();
		}
		const int ErrorMessage = 1;
		const int StackTrace = 2;

		public void AddErrorMessage ()
		{
			string msg;
			if (error is RemoteUnhandledException)
				msg = Escape (errorMessage);
			else {
				msg = GettextCatalog.GetString ("Internal error");
				if (errorMessage != null)
					msg += ": " + Escape (errorMessage);
			}

			var stock = ImageService.GetIcon (Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			TreeIter testRow = failuresStore.AppendValues (stock, msg, null, null, 0);
			string name = error is RemoteUnhandledException ? ((RemoteUnhandledException)error).RemoteExceptionName : error.GetType ().Name;
			failuresStore.AppendValues (testRow, null, Escape (name + ": " + error.Message), null);
			TreeIter row = failuresStore.AppendValues (testRow, null, GettextCatalog.GetString ("Stack Trace"), null, null, 0);
			AddStackTrace (row, error.StackTrace, null);
		}

		public void ReportExecutionError (string message)
		{
			var stock = ImageService.GetIcon (Ide.Gui.Stock.Error, Gtk.IconSize.Menu);
			TreeIter testRow = failuresStore.AppendValues (stock, message, null, null, 0);
		}

		readonly static Regex stackTraceLineRegex = new Regex (@".*\s(?<file>.*)\:\D*\s?(?<line>\d+)", RegexOptions.Compiled);
		
		public static bool TryParseLocationFromStackTrace (string stackTraceLine, out string fileName, out int lineNumber)
		{
			var match = stackTraceLineRegex.Match (stackTraceLine);

			if (!match.Success) {
				fileName = null;
				lineNumber = -1;
				return false;
			}
			try {
				fileName = match.Groups ["file"].Value;
				lineNumber = int.Parse (match.Groups ["line"].Value);
			} catch (Exception) {
				fileName = null;
				lineNumber = -1;
				return false;
			}
			return true;
		}

		void AddStackTrace (TreeIter row, string stackTrace, UnitTest test)
		{
			string[] stackLines = stackTrace.Replace ("\r", "").Split ('\n');
			foreach (string line in stackLines) {
				string fileName;
				int lineNumber;
				TryParseLocationFromStackTrace (line, out fileName, out lineNumber);
				failuresStore.AppendValues (row, null, Escape (line), test, fileName, lineNumber, StackTrace);
			}
		}
		
		public void FinishTestRun ()
		{
			if (!Gtk.TreeIter.Zero.Equals (startMessageIter)) {
				string msg = string.Format (GettextCatalog.GetString ("Test results for <b>{0}</b> configuration <b>{1}</b>"), rootTest != null ? rootTest.Name : "null", configuration);
				failuresStore.SetValue (startMessageIter, 1, msg);
				startMessageIter = Gtk.TreeIter.Zero;
			}
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
			
			resultLabel.Markup = GetResultsMarkup ();

			Running = false;
		}
		
		void OnStopClicked (object sender, EventArgs args)
		{
			if (running)
				Cancel ();
		}
		
		void OnRunClicked (object sender, EventArgs args)
		{
			if (rootTest == null)
				return;
			UnitTestService.RunTest (rootTest, null);
		}

		void OnRowActivated (object s, EventArgs a)
		{
			Gtk.TreeIter iter;
			if (failuresTreeView.Selection.GetSelected (out iter)) {
				string file = (string)failuresStore.GetValue (iter, 3);
				int line = (int)failuresStore.GetValue (iter, 4);
				try {
					if (file != null && File.Exists (file)) {
						IdeApp.Workbench.OpenDocument (file, null, line, -1);
						return;
					}
				} catch (Exception) {
				}
			}
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

		[CommandHandler (EditCommands.Copy)]
		protected void OnCopy ()
		{
			UnitTest test = GetSelectedTest ();
			if (test != null) {
				var last = test.GetLastResult ();
				if (last == null)
					return;

				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!failuresTreeView.Selection.GetSelected (out foo, out iter))
					return;

				int type = (int)failuresStore.GetValue (iter, 5);

				var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				switch (type) {
					case ErrorMessage:
					clipboard.Text = last.Message;
					break;
					case StackTrace:
					clipboard.Text = last.StackTrace;
					break;
					default:
					clipboard.Text = last.Message + Environment.NewLine + GettextCatalog.GetString("Stack trace:") + Environment.NewLine + last.StackTrace;
					break;
				}
			} else {
				if (error == null)
					return;
				var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));

				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!failuresTreeView.Selection.GetSelected (out foo, out iter))
					return;

				int type = (int)failuresStore.GetValue (iter, 5);

				switch (type) {
				case ErrorMessage:
					clipboard.Text = error.Message;
					break;
				case StackTrace:
					clipboard.Text = error.StackTrace;
					break;
				default:
					clipboard.Text = error.Message + Environment.NewLine + GettextCatalog.GetString("Stack trace:") + Environment.NewLine + error.StackTrace;
					break;
				}
			}
		}

		[CommandUpdateHandler (EditCommands.Copy)]
		protected void OnUpdateCopy (CommandInfo info)
		{
			UnitTest test = GetSelectedTest ();
			if (test != null) {
				var last = test.GetLastResult ();

				Gtk.TreeModel foo;
				Gtk.TreeIter iter;
				if (!failuresTreeView.Selection.GetSelected (out foo, out iter)) {
					info.Enabled = false;
					return;
				}

				int type = (int)failuresStore.GetValue (iter, 5);
				switch (type) {
				case ErrorMessage:
					info.Enabled = !string.IsNullOrEmpty (last.Message);
					return;
				case StackTrace:
				default:
					info.Enabled = !string.IsNullOrEmpty (last.StackTrace);
					return;
				}
			}
			info.Enabled = error != null;
		}

		[CommandHandler (TestCommands.SelectTestInTree)]
		protected void OnSelectTestInTree ()
		{
			Pad pad = IdeApp.Workbench.GetPad<TestPad> ();
			pad.BringToFront ();
			TestPad content = (TestPad)pad.Content;
			content.SelectTest (GetSelectedTest ());
		}
		
		[CommandUpdateHandler (TestCommands.SelectTestInTree)]
		protected void OnUpdateSelectTestInTree (CommandInfo info)
		{
			UnitTest test = GetSelectedTest ();
			info.Enabled = test != null;
		}
		
		[CommandHandler (TestCommands.GoToFailure)]
		protected void OnShowTest ()
		{
			UnitTest test = GetSelectedTest ();
			if (test == null)
				return;
			SourceCodeLocation loc = null;
			UnitTestResult res = test.GetLastResult ();
			if (res != null && res.IsFailure)
				loc = res.GetFailureLocation ();
			if (loc == null)
				loc = test.SourceCodeLocation;
			if (loc != null) {
				IdeApp.Workbench.OpenDocument (loc.FileName, null, loc.Line, loc.Column);
			} else {
				LoggingService.LogError ("Can't get source code location for test : "+ test);
				MessageService.ShowError (GettextCatalog.GetString ("Can't get source code location for : {0}", test.Name));
			}
		}

		[CommandHandler (TestCommands.ShowTestCode)]
		protected void OnShowTestCode ()
		{
			UnitTest test = GetSelectedTest ();
			if (test == null)
				return;
			SourceCodeLocation loc = test.SourceCodeLocation;
			if (loc != null) {
				IdeApp.Workbench.OpenDocument (loc.FileName, null, loc.Line, loc.Column);
			}  else {
				LoggingService.LogError ("Can't get source code location for test : "+ test);
				MessageService.ShowError (GettextCatalog.GetString ("Can't get source code location for : {0}", test.Name));
			}
		}

		[CommandHandler (TestCommands.RerunTest)]
		protected void OnRerunTest ()
		{
			UnitTest test = GetSelectedTest ();
			if (test == null)
				return;
			UnitTestService.RunTest (test, null);
		}
		
		UnitTest GetSelectedTest ()
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!failuresTreeView.Selection.GetSelected (out foo, out iter))
				return null;

			UnitTest t = (UnitTest)failuresStore.GetValue (iter, 2);
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
		
		void OnShowInconclusiveToggled (object sender, EventArgs args)
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
			if (failuresTreeView.IsRealized)
				failuresTreeView.ScrollToPoint (0, 0);

			failuresStore.Clear ();
			outputView.Buffer.Clear ();
			outIters.Clear ();
			AddStartMessage (running);
				
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
				TreeIter testRow = failuresStore.AppendValues (TestStatusIcon.Success, Escape (test.FullName), test);
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.IsFailure) {
				if (!buttonFailures.Active)
					return;
				string file = test.SourceCodeLocation != null ? test.SourceCodeLocation.FileName + ":" + test.SourceCodeLocation.Line : null;
				TreeIter testRow = failuresStore.AppendValues (TestStatusIcon.Failure, Escape (test.FullName), test, file);
				bool hasMessage = !string.IsNullOrEmpty (result.Message);

				if (hasMessage)
					failuresStore.AppendValues (testRow, null, "<span font='" + FontService.MonospaceFontName + "'>"+Escape (result.Message) + "</span>", test, null, 0, ErrorMessage);
				if (!string.IsNullOrEmpty (result.StackTrace)) {
					TreeIter row = testRow;
					if (hasMessage)
						row = failuresStore.AppendValues (testRow, null, GettextCatalog.GetString ("Stack Trace"), test, null, 0, StackTrace);
					AddStackTrace (row, result.StackTrace, test);
				}
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.IsNotRun) {
				if (!buttonIgnored.Active)
					return;
				TreeIter testRow = failuresStore.AppendValues (TestStatusIcon.NotRun, Escape (test.FullName), test);
				if (result.Message != null)
					failuresStore.AppendValues (testRow, null, Escape (result.Message), test);
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			if (result.IsInconclusive) {
				if (!buttonInconclusive.Active)
					return;
				TreeIter testRow = failuresStore.AppendValues (TestStatusIcon.Inconclusive, Escape (test.FullName), test);
				if (result.Message != null)
					failuresStore.AppendValues (testRow, null, Escape (result.Message), test);
				failuresTreeView.ScrollToCell (failuresStore.GetPath (testRow), null, false, 0, 0);
			}
			
			string msg = GettextCatalog.GetString ("Running {0} ...", test.FullName);
			TextIter it = outputView.Buffer.EndIter;
			outIters [test] = it.Offset;
			outputView.Buffer.InsertWithTags (ref it, msg, bold);
			outputView.Buffer.Insert (ref it, "\n");
			if (result.ConsoleOutput != null)
				outputView.Buffer.Insert (ref it, result.ConsoleOutput);
			if (result.ConsoleError != null)
				outputView.Buffer.Insert (ref it, result.ConsoleError);
			outputView.ScrollMarkOnscreen (outputView.Buffer.InsertMark);
		}

		void ITestProgressMonitor.WriteGlobalLog (string message)
		{
			TextIter it = outputView.Buffer.EndIter;
			outputView.Buffer.Insert (ref it, message);
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
			
			resultSummary.Add (result);
			results.Add (rec);
			
			ShowTestResult (test, result);
			
			UpdateCounters ();

			double frac;
			if (testsToRun != 0)
				frac = ((double)testsRun / (double)testsToRun);
			else
				frac = 1;

			frac = Math.Min (1, Math.Max (0, frac));

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
			if (cancel)
				return;
			cancel = true;
			cancellationSource.Cancel ();
		}

		void OnCancel ()
		{
			Gtk.Application.Invoke ((o, args) => {
				failuresStore.AppendValues (TestStatusIcon.Failure, GettextCatalog.GetString ("Test execution cancelled."), null);
			});
		}
		
		CancellationToken ITestProgressMonitor.CancellationToken {
			get { return cancellationSource.Token; }
		}
	}
	
	class TestMonitor: ITestProgressMonitor
	{
		ITestProgressMonitor monitor;
		TestResultsPad pad;

		public TestMonitor (TestResultsPad pad, CancellationTokenSource cs)
		{
			this.pad = pad;
			this.monitor = pad;
		}
		public void FinishTestRun ()
		{
			Runtime.RunInMainThread (delegate {
				pad.FinishTestRun ();
			});
		}
		public void BeginTest (UnitTest test)
		{
			Runtime.RunInMainThread (delegate {
				monitor.BeginTest (test);
			});
		}
		public void EndTest (UnitTest test, UnitTestResult result)
		{
			Runtime.RunInMainThread (delegate {
				monitor.EndTest (test, result);
			});
		}
		public void ReportRuntimeError (string message, Exception exception)
		{
			Runtime.RunInMainThread (delegate {
				monitor.ReportRuntimeError (message, exception);
			});
		}
		public void WriteGlobalLog (string message)
		{
			Runtime.RunInMainThread (delegate {
				monitor.WriteGlobalLog (message);
			});
		}

		public CancellationToken CancellationToken {
			get { return monitor.CancellationToken; }
		}
	}
}

