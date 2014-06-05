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
using Mono.TextEditor;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.NUnit
{
	public abstract class AbstractUnitTestTextEditorExtension : TextEditorExtension
	{
		public override void Initialize ()
		{
			base.Initialize ();
			Document.DocumentParsed += HandleDocumentParsed; 
			if (IdeApp.Workbench == null)
				return;
			NUnitService.Instance.TestSessionCompleted += HandleTestSessionCompleted;
		}

		void HandleTestSessionCompleted (object sender, EventArgs e)
		{
			if (document.Editor == null)
				return;
			document.Editor.Parent.TextArea.RedrawMargin (document.Editor.Parent.TextArea.ActionMargin);
		}

		public override void Dispose ()
		{
			NUnitService.Instance.TestSessionCompleted -= HandleTestSessionCompleted;
			RemoveHandler ();
			Document.DocumentParsed -= HandleDocumentParsed; 
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
					var editor = document.Editor;
					if (editor == null)
						return;
					var textEditor = editor.Parent;
					if (textEditor == null)
						return;
					var actionMargin = textEditor.ActionMargin;
					if (actionMargin == null)
						return;
					if (actionMargin.IsVisible ^ (foundTests.Count > 0))
						textEditor.QueueDraw ();
					actionMargin.IsVisible |= foundTests.Count > 0;
					foreach (var oldMarker in currentMarker)
						editor.Document.RemoveMarker (oldMarker);

					foreach (var foundTest in foundTests) {
						if (token.IsCancellationRequested)
							return;
						var unitTestMarker = new UnitTestMarker (foundTest, document);
						currentMarker.Add (unitTestMarker);
						editor.Document.AddMarker (foundTest.LineNumber, unitTestMarker);
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

		List<UnitTestMarker> currentMarker = new List<UnitTestMarker>();

		class UnitTestMarker : MarginMarker
		{
			readonly UnitTestLocation unitTest;
			readonly MonoDevelop.Ide.Gui.Document doc;

			public UnitTestMarker(UnitTestLocation unitTest, MonoDevelop.Ide.Gui.Document doc)
			{
				this.unitTest = unitTest;
				this.doc = doc;
			}

			public override bool CanDrawForeground (Margin margin)
			{
				return margin is ActionMargin;
			}

			public override void InformMouseHover (TextEditor editor, Margin margin, MarginMouseEventArgs args)
			{
				if (!(margin is ActionMargin))
					return;
				string toolTip;
				if (unitTest.IsFixture) {
					if (isFailed) {
						toolTip = GettextCatalog.GetString ("NUnit Fixture failed (click to run)");
						if (!string.IsNullOrEmpty (failMessage))
							toolTip += Environment.NewLine + failMessage.TrimEnd ();
					} else {
						toolTip = GettextCatalog.GetString ("NUnit Fixture (click to run)");
					}
				} else {
					if (isFailed) {
						toolTip = GettextCatalog.GetString ("NUnit Test failed (click to run)");
						if (!string.IsNullOrEmpty (failMessage))
							toolTip += Environment.NewLine + failMessage.TrimEnd ();
						foreach (var id in unitTest.TestCases) {
							var test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier + id);
							if (test != null) {
								var result = test.GetLastResult ();
								if (result != null && result.IsFailure) {
									if (!string.IsNullOrEmpty (result.Message)) {
										toolTip += Environment.NewLine + "Test" + id +":";
										toolTip += Environment.NewLine + result.Message.TrimEnd ();
									}
								}
							}

						}
					} else {
						toolTip = GettextCatalog.GetString ("NUnit Test (click to run)");
					}

				}
				editor.TooltipText = toolTip;
			}

			static Menu menu;

			public override void InformMousePress (TextEditor editor, Margin margin, MarginMouseEventArgs args)
			{
				if (!(margin is ActionMargin))
					return;
				if (menu != null) {
					menu.Destroy ();
				}
				var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode ();

				menu = new Menu ();
				if (unitTest.IsFixture) {
					var menuItem = new MenuItem ("_Run All");
					menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, false).Run;
					menu.Add (menuItem);
					if (debugModeSet != null) {
						menuItem = new MenuItem ("_Debug All");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Run;
						menu.Add (menuItem);
					}
					menuItem = new MenuItem ("_Select in Test Pad");
					menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Select;
					menu.Add (menuItem);
				} else {
					if (unitTest.TestCases.Count == 0) {
						var menuItem = new MenuItem ("_Run");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, false).Run;
						menu.Add (menuItem);
						if (debugModeSet != null) {
							menuItem = new MenuItem ("_Debug");
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Run;
							menu.Add (menuItem);
						}
						menuItem = new MenuItem ("_Select in Test Pad");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Select;
						menu.Add (menuItem);
					} else {
						var menuItem = new MenuItem ("_Run All");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, false).Run;
						menu.Add (menuItem);
						if (debugModeSet != null) {
							menuItem = new MenuItem ("_Debug All");
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Run;
							menu.Add (menuItem);
						}
						menu.Add (new SeparatorMenuItem ());
						foreach (var id in unitTest.TestCases) {
							var submenu = new Menu ();
							menuItem = new MenuItem ("_Run");
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier + id, false).Run;
							submenu.Add (menuItem);
							if (debugModeSet != null) {
								menuItem = new MenuItem ("_Debug");
								menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier + id, true).Run;
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
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier + id, true).Select;
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
				editor.TextArea.ResetMouseState (); 
				GtkWorkarounds.ShowContextMenu (menu, editor, new Gdk.Rectangle ((int)args.X, (int)args.Y, 1, 1));
			}

			class TestRunner
			{
				//				readonly MonoDevelop.Ide.Gui.Document doc;
				readonly string testCase;
				readonly bool debug;

				public TestRunner (MonoDevelop.Ide.Gui.Document doc, string testCase, bool debug)
				{
					//					this.doc = doc;
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

			bool isFailed;
			string failMessage;
			public override void DrawForeground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
			{
				isFailed = false;
				var test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier);
				bool searchCases = false;

				Xwt.Drawing.Image icon = null;

				if (test != null) {
					icon = test.StatusIcon;
					var result = test.GetLastResult ();
					if (result == null) {
						searchCases = true;
					} else if (result.IsFailure) {
						failMessage = result.Message;
						isFailed = true;
					}
				} else {
					searchCases = true;
				}

				if (searchCases) {
					foreach (var caseId in unitTest.TestCases) {
						test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier + caseId);
						if (test != null) {
							icon = test.StatusIcon;
							var result = test.GetLastResult ();
							if (result != null && result.IsFailure) {
								failMessage = result.Message;
								isFailed = true;
								break;
							} 
						}
					}
				}

				if (icon != null) {
					if (icon.Width > metrics.Width || icon.Height > metrics.Height)
						icon = icon.WithBoxSize (metrics.Width, metrics.Height);
					cr.DrawImage (editor, icon, Math.Truncate (metrics.X + metrics.Width / 2 - icon.Width / 2), Math.Truncate (metrics.Y + metrics.Height / 2 - icon.Height / 2));
				}
			}
		}
		public class UnitTestLocation
		{
			public int LineNumber { get; set; }
			public bool IsFixture { get; set; }
			public string UnitTestIdentifier { get; set; }
			public bool IsIgnored { get; set; }

			public List<string> TestCases = new List<string> ();

			public UnitTestLocation (int lineNumber)
			{
				LineNumber = lineNumber;
			}
		}
	}
}

