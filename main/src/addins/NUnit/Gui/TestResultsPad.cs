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
using System.Collections;
using System.Collections.Generic;
using Gtk;
using Gdk;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.NUnit.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using System.Text.RegularExpressions;

namespace MonoDevelop.NUnit
{
	class TestResultsPad: IPadContent, ITestProgressMonitor
	{
		NUnitService testService = NUnitService.Instance;
		
		IPadWindow window;
		VBox panel;
		HPaned book;
		
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
		
		Button buttonStop;
		Button buttonRun;
		
		ToggleButton buttonSuccess;
		ToggleButton buttonFailures;
		ToggleButton buttonIgnored;
		ToggleButton buttonOutput;
		
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
			
			// Results notebook
			
			book = new HPaned ();
			panel.PackStart (book, true, true, 0);
			panel.FocusChain = new Gtk.Widget [] { book };
			
			// Failures tree
			failuresTreeView = new TreeView ();
			failuresTreeView.HeadersVisible = false;
			failuresStore = new TreeStore (typeof(Pixbuf), typeof (string), typeof(object), typeof(string));
			var pr = new CellRendererPixbuf ();
			CellRendererText tr = new CellRendererText ();
			TreeViewColumn col = new TreeViewColumn ();
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", 0);
			col.PackStart (tr, false);
			col.AddAttribute (tr, "markup", 1);
			failuresTreeView.AppendColumn (col);
			failuresTreeView.Model = failuresStore;
		
			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add(failuresTreeView);
			book.Pack1 (sw, true, true);
			
			outputView = new TextView();
			outputView.Editable = false;
			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			outputView.Buffer.TagTable.Add (bold);
			sw = new Gtk.ScrolledWindow ();
			sw.ShadowType = ShadowType.None;
			sw.Add(outputView);
			book.Pack2 (sw, true, true);
			outputViewScrolled = sw;
			
			failuresTreeView.ButtonReleaseEvent += new Gtk.ButtonReleaseEventHandler (OnPopupMenu);
			failuresTreeView.RowActivated += OnRowActivated;
			failuresTreeView.Selection.Changed += OnRowSelected;
			
			Control.ShowAll ();
			
			outputViewScrolled.Hide ();
		}
		
		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
			
			DockItemToolbar toolbar = window.GetToolbar (PositionType.Top);
			
			buttonSuccess = new ToggleButton ();
			buttonSuccess.Label = GettextCatalog.GetString ("Successful Tests");
			buttonSuccess.Active = false;
			buttonSuccess.Image = new Gtk.Image (CircleImage.Success);
			buttonSuccess.Image.Show ();
			buttonSuccess.Toggled += new EventHandler (OnShowSuccessfulToggled);
			buttonSuccess.TooltipText = GettextCatalog.GetString ("Show Successful Tests");
			toolbar.Add (buttonSuccess);
			
			buttonFailures = new ToggleButton ();
			buttonFailures.Label = GettextCatalog.GetString ("Failed Tests");
			buttonFailures.Active = true;
			buttonFailures.Image = new Gtk.Image (CircleImage.Failure);
			buttonFailures.Image.Show ();
			buttonFailures.Toggled += new EventHandler (OnShowFailuresToggled);
			buttonFailures.TooltipText = GettextCatalog.GetString ("Show Failed Tests");
			toolbar.Add (buttonFailures);
			
			buttonIgnored = new ToggleButton ();
			buttonIgnored.Label = GettextCatalog.GetString ("Ignored Tests");
			buttonIgnored.Active = true;
			buttonIgnored.Image = new Gtk.Image (CircleImage.NotRun);
			buttonIgnored.Image.Show ();
			buttonIgnored.Toggled += new EventHandler (OnShowIgnoredToggled);
			buttonIgnored.TooltipText = GettextCatalog.GetString( "Show Ignored Tests");
			toolbar.Add (buttonIgnored);
			
			buttonOutput = new ToggleButton ();
			buttonOutput.Label = GettextCatalog.GetString ("Output");
			buttonOutput.Active = false;
			buttonOutput.Image = ImageService.GetImage (MonoDevelop.Ide.Gui.Stock.OutputIcon, IconSize.Menu);
			buttonOutput.Image.Show ();
			buttonOutput.Toggled += new EventHandler (OnShowOutputToggled);
			buttonOutput.TooltipText = GettextCatalog.GetString ("Show Output");
			toolbar.Add (buttonOutput);
			
			toolbar.Add (new SeparatorToolItem ());
			
			buttonRun = new Button ();
			buttonRun.Label = GettextCatalog.GetString ("Run Test");
			buttonRun.Image = new Gtk.Image (Gtk.Stock.Execute, IconSize.Menu);
			buttonRun.Image.Show ();
			buttonRun.Sensitive = false;
			toolbar.Add (buttonRun);
			
			buttonStop = new Button (new Gtk.Image (Gtk.Stock.Stop, Gtk.IconSize.Menu));
			toolbar.Add (buttonStop);
			toolbar.ShowAll ();
			
			buttonStop.Clicked += new EventHandler (OnStopClicked);
			buttonRun.Clicked += new EventHandler (OnRunClicked);
			
			// Run panel
			
			DockItemToolbar runPanel = window.GetToolbar (PositionType.Bottom);
			
			infoSep = new VSeparator ();
			
			resultLabel.UseMarkup = true;
			infoCurrent.Ellipsize = Pango.EllipsizeMode.Start;
			infoCurrent.WidthRequest = 0;
			runPanel.Add (resultLabel);
			runPanel.Add (progressBar);
			runPanel.Add (infoCurrent, true, 10);	
			
			labels = new HBox (false, 10);
			
			infoFailed.UseMarkup = true;
			infoIgnored.UseMarkup = true;
			
			labels.PackStart (infoFailed, true, false, 0);
			labels.PackStart (infoIgnored, true, false, 0);
			
			runPanel.Add (new Gtk.Label (), true);
			runPanel.Add (labels);
			runPanel.Add (infoSep, false, 10);
			
			progressBar.HeightRequest = infoFailed.SizeRequest().Height;
			runPanel.ShowAll ();
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
				window.IsWorking = value;
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
			
			configuration = IdeApp.Workspace.ActiveConfigurationId;
			
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
			AddStackTrace (row, error.StackTrace, null);
		}
		
		void AddStackTrace (TreeIter row, string stackTrace, UnitTest test)
		{
			string[] stackLines = stackTrace.Replace ("\r","").Split ('\n');
			foreach (string line in stackLines) {
				Regex r = new Regex (@".*?\(.*?\)\s\[.*?\]\s.*?\s(?<file>.*)\:(?<line>\d*)");
				Match m = r.Match (line);
				string file;
				if (m.Groups["file"] != null && m.Groups["line"] != null)
					file = m.Groups["file"].Value + ":" + m.Groups["line"].Value;
				else
					file = null;
				failuresStore.AppendValues (row, null, Escape (line), test, file);
			}
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
			if (running)
				Cancel ();
		}
		
		void OnRunClicked (object sender, EventArgs args)
		{
			if (rootTest == null)
				return;
			NUnitService.Instance.RunTest (rootTest, null);
		}
		
		void OnPopupMenu (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3) {
				IdeApp.CommandService.ShowContextMenu ("/MonoDevelop/NUnit/ContextMenu/TestResultsPad");
			}
		}

		void OnRowActivated (object s, EventArgs a)
		{
			Gtk.TreeIter iter;
			if (failuresTreeView.Selection.GetSelected (out iter)) {
				string file = (string) failuresStore.GetValue (iter, 3);
				if (file != null) {
					int i = file.LastIndexOf (':');
					if (i != -1) {
						int line;
						if (int.TryParse (file.Substring (i+1), out line)) {
							IdeApp.Workbench.OpenDocument (file.Substring (0, i), line, -1, true);
							return;
						}
					}
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
			if (loc != null)
				IdeApp.Workbench.OpenDocument (loc.FileName, loc.Line, loc.Column, true);
		}
		
		[CommandHandler (TestCommands.ShowTestCode)]
		protected void OnShowTestCode ()
		{
			UnitTest test = GetSelectedTest ();
			if (test == null)
				return;
			SourceCodeLocation loc = test.SourceCodeLocation;
			if (loc != null)
				IdeApp.Workbench.OpenDocument (loc.FileName, loc.Line, loc.Column, true);
		}
		
		[CommandUpdateHandler (TestCommands.ShowTestCode)]
		[CommandUpdateHandler (TestCommands.GoToFailure)]
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
					AddStackTrace (row, result.StackTrace, test);
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
			if (cancel)
				return;
			cancel = true;
			Gtk.Application.Invoke (delegate {
				failuresStore.AppendValues (CircleImage.Failure, GettextCatalog.GetString ("Test execution cancelled."), null);
			});
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

