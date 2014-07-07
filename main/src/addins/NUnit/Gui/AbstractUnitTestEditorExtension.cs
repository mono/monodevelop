//
// AbstractUnitTestTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Ide.Gui.Content;
using System.Collections.Generic;
using System.Threading;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.NUnit
{
	public abstract class AbstractUnitTestTextEditorExtension : TextEditorExtension
	{
		protected override void Initialize ()
		{
			base.Initialize ();
			DocumentContext.DocumentParsed += HandleDocumentParsed; 
			if (IdeApp.Workbench == null)
				return;
			NUnitService.Instance.TestSessionCompleted += HandleTestSessionCompleted;
		}

		void HandleTestSessionCompleted (object sender, EventArgs e)
		{
			foreach (var marker in currentMarker)
				marker.UpdateState ();
		}

		public override void Dispose ()
		{
			NUnitService.Instance.TestSessionCompleted -= HandleTestSessionCompleted;
			RemoveHandler ();
			DocumentContext.DocumentParsed -= HandleDocumentParsed; 
			base.Dispose ();
		}

		CancellationTokenSource src = new CancellationTokenSource ();

		public abstract IList<UnitTestLocation> GatherUnitTests ();

		readonly static PropertyWrapper<bool> EnableUnitTestEditorIntegration = new PropertyWrapper<bool> ("Testing.EnableUnitTestEditorIntegration", false);

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			if (!EnableUnitTestEditorIntegration)
				return;
			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			ThreadPool.QueueUserWorkItem (delegate {
				if (token.IsCancellationRequested)
					return;
				var foundTests = GatherUnitTests ();
				if (foundTests == null)
					return;
				Application.Invoke (delegate {
					foreach (var oldMarker in currentMarker)
						Editor.RemoveMarker (oldMarker);

					foreach (var foundTest in foundTests) {
						if (token.IsCancellationRequested)
							return;
						var unitTestMarker = TextMarkerFactory.CreateUnitTestMarker (Editor, new UnitTestMarkerHostImpl (this), foundTest);
						currentMarker.Add (unitTestMarker);
						Editor.AddMarker (foundTest.LineNumber, unitTestMarker);
					}
				});
			});
		}

		static uint timeoutHandler;

		static void RemoveHandler ()
		{
			if (timeoutHandler != 0) {
				GLib.Source.Remove (timeoutHandler); 
				timeoutHandler = 0;
			}
		}

		List<IUnitTestMarker> currentMarker = new List<IUnitTestMarker>();

		class UnitTestMarkerHostImpl : UnitTestMarkerHost
		{
			static Menu menu;

			AbstractUnitTestTextEditorExtension ext;

			public UnitTestMarkerHostImpl (AbstractUnitTestTextEditorExtension ext)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				this.ext = ext;
			}

			#region implemented abstract members of UnitTestMarkerHost

			public override Xwt.Drawing.Image GetStatusIcon (string unitTestIdentifier, string caseId = null)
			{
				var test = NUnitService.Instance.SearchTestById (unitTestIdentifier + caseId);
				if (test != null)
					return test.StatusIcon;
				return null;
			}

			public override bool IsFailure (string unitTestIdentifier, string caseId = null)
			{
				var test = NUnitService.Instance.SearchTestById (unitTestIdentifier + caseId);
				if (test != null) {
					var result = test.GetLastResult ();
					if (result != null)
						return result.IsFailure;
				}
				return false;
			}

			public override string GetMessage (string unitTestIdentifier, string caseId = null)
			{
				var test = NUnitService.Instance.SearchTestById (unitTestIdentifier + caseId);
				if (test != null) {
					var result = test.GetLastResult ();
					if (result != null)
						return result.Message;
				}
				return null;
			}

			public override bool HasResult (string unitTestIdentifier, string caseId = null)
			{
				return NUnitService.Instance.SearchTestById (unitTestIdentifier + caseId) != null;
			}

			public override void PopupContextMenu (UnitTestLocation unitTest, int x, int y)
			{
				if (menu != null) {
					menu.Destroy ();
				}
				var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode ();

				menu = new Menu ();
				if (unitTest.IsFixture) {
					var menuItem = new MenuItem ("_Run All");
					menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, false).Run;
					menu.Add (menuItem);
					if (debugModeSet != null) {
						menuItem = new MenuItem ("_Debug All");
						menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, true).Run;
						menu.Add (menuItem);
					}
					menuItem = new MenuItem ("_Select in Test Pad");
					menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, true).Select;
					menu.Add (menuItem);
				} else {
					if (unitTest.TestCases.Count == 0) {
						var menuItem = new MenuItem ("_Run");
						menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, false).Run;
						menu.Add (menuItem);
						if (debugModeSet != null) {
							menuItem = new MenuItem ("_Debug");
							menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, true).Run;
							menu.Add (menuItem);
						}
						menuItem = new MenuItem ("_Select in Test Pad");
						menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, true).Select;
						menu.Add (menuItem);
					} else {
						var menuItem = new MenuItem ("_Run All");
						menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, false).Run;
						menu.Add (menuItem);
						if (debugModeSet != null) {
							menuItem = new MenuItem ("_Debug All");
							menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier, true).Run;
							menu.Add (menuItem);
						}
						menu.Add (new SeparatorMenuItem ());
						foreach (var id in unitTest.TestCases) {
							var submenu = new Menu ();
							menuItem = new MenuItem ("_Run");
							menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier + id, false).Run;
							submenu.Add (menuItem);
							if (debugModeSet != null) {
								menuItem = new MenuItem ("_Debug");
								menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier + id, true).Run;
								submenu.Add (menuItem);
							}

							var label = "Test" + id;
							string tooltip = null;
							var test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier + id);
							if (test != null) {
								var result = test.GetLastResult ();
								if (result != null && result.IsFailure) {
									tooltip = result.Message;
									label += "!";
								}
							}

							menuItem = new MenuItem ("_Select in Test Pad");
							menuItem.Activated += new TestRunner (unitTest.UnitTestIdentifier + id, true).Select;
							submenu.Add (menuItem);


							var subMenuItem = new MenuItem (label);
							if (!string.IsNullOrEmpty (tooltip))
								subMenuItem.TooltipText = tooltip;
							subMenuItem.Submenu = submenu;
							menu.Add (subMenuItem);
						}
					}
				}
				menu.ShowAll ();

				GtkWorkarounds.ShowContextMenu (menu, ext.Editor, new Gdk.Rectangle (x, y, 1, 1));

			}

			#endregion

			class TestRunner
			{
				readonly string testCase;
				readonly bool debug;

				public TestRunner (string testCase, bool debug)
				{
					this.testCase = testCase;
					this.debug = debug;
				}

				bool TimeoutHandler ()
				{
					var test = NUnitService.Instance.SearchTestById (testCase);
					if (test != null) {
						RunTest (test); 
						timeoutHandler = 0;
					} else {
						return true;
					}
					return false;
				}

				List<NUnitProjectTestSuite> testSuites = new List<NUnitProjectTestSuite>();
				internal void Run (object sender, EventArgs e)
				{
					menu.Destroy ();
					menu = null;
					if (IdeApp.ProjectOperations.IsBuilding (IdeApp.ProjectOperations.CurrentSelectedSolution) || 
						IdeApp.ProjectOperations.IsRunning (IdeApp.ProjectOperations.CurrentSelectedSolution))
						return;

					var foundTest = NUnitService.Instance.SearchTestById (testCase);
					if (foundTest != null) {
						RunTest (foundTest);
						return;
					}

					var tests = new Stack<UnitTest> ();
					foreach (var test in NUnitService.Instance.RootTests) {
						tests.Push (test);
					}
					while (tests.Count > 0) {
						var test = tests.Pop ();

						var solutionFolderTestGroup = test as SolutionFolderTestGroup;
						if (solutionFolderTestGroup != null) {
							foreach (var test2 in solutionFolderTestGroup.Tests) {
								tests.Push (test2); 
							}
							continue;
						}
						var nUnitProjectTestSuite = test as NUnitProjectTestSuite;
						if (nUnitProjectTestSuite != null)
							testSuites.Add (nUnitProjectTestSuite); 
					}

					foreach (var test in testSuites) {
						test.TestChanged += HandleTestChanged;
						test.ProjectBuiltWithoutTestChange += HandleTestChanged;
					}

					IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedSolution);
				}

				void HandleTestChanged (object sender, EventArgs e)
				{
					var foundTest = NUnitService.Instance.SearchTestById (testCase);
					if (foundTest != null) {
						foreach (var test in testSuites) {
							test.TestChanged -= HandleTestChanged;
							test.ProjectBuiltWithoutTestChange -= HandleTestChanged;
						}
						testSuites.Clear ();

						RunTest (foundTest); 
					}
				}

				internal void Select (object sender, EventArgs e)
				{
					menu.Destroy ();
					menu = null;
					var test = NUnitService.Instance.SearchTestById (testCase);
					if (test == null)
						return;
					var pad = IdeApp.Workbench.GetPad<TestPad> ();
					pad.BringToFront ();
					var content = (TestPad)pad.Content;
					content.SelectTest (test);
				}

				void RunTest (UnitTest test)
				{
					var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode ();
					MonoDevelop.Core.Execution.IExecutionHandler ctx = null;
					if (debug && debugModeSet != null) {
						foreach (var executionMode in debugModeSet.ExecutionModes) {
							if (test.CanRun (executionMode.ExecutionHandler)) {
								ctx = executionMode.ExecutionHandler;
								break;
							}
						}
					}
					// NUnitService.Instance.RunTest (test, ctx);
					var pad = IdeApp.Workbench.GetPad<TestPad> ();
					var content = (TestPad)pad.Content;
					content.RunTest (test, ctx);
				}
			}
		}
	}
}

