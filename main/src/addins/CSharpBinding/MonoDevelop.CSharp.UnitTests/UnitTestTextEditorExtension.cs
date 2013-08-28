//
// UnitTestTextEditorExtension.cs
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Collections.Generic;
using System.Threading;
using Mono.TextEditor;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using Gtk;
using System.Text;
using MonoDevelop.AnalysisCore;

namespace MonoDevelop.CSharp
{
	class UnitTestTextEditorExtension : TextEditorExtension
	{
		TestPad testPad;

		public override void Initialize ()
		{
			base.Initialize ();
			Document.DocumentParsed += HandleDocumentParsed; 

			var pad = IdeApp.Workbench.GetPad<TestPad> ();
			testPad = (TestPad)pad.Content;
			if (testPad != null)
				testPad.TestSessionCompleted += HandleTestSessionCompleted;
		}

		void HandleTestSessionCompleted (object sender, EventArgs e)
		{
			if (document.Editor == null)
				return;
			document.Editor.Parent.TextArea.RedrawMargin (document.Editor.Parent.TextArea.ActionMargin);
		}

		public override void Dispose ()
		{
			if (testPad != null) {
				testPad.TestSessionCompleted -= HandleTestSessionCompleted;
			}

			RemoveHandler ();
			Document.DocumentParsed -= HandleDocumentParsed; 
			base.Dispose ();
		}

		CancellationTokenSource src = new CancellationTokenSource ();

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			if (!AnalysisOptions.EnableUnitTestEditorIntegration)
				return;
			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			ThreadPool.QueueUserWorkItem (delegate {
				var resolver = document.GetSharedResolver ();
				if (resolver == null || resolver.Result == null)
					return;
				var parsedDocument = document.ParsedDocument;
				if (parsedDocument == null)
					return;
				var syntaxTree = parsedDocument.GetAst<SyntaxTree> ();
				if (syntaxTree == null)
					return;
				var visitor = new NUnitVisitor (resolver.Result);
				try {
					visitor.VisitSyntaxTree (syntaxTree);
				} catch (Exception ex) {
					LoggingService.LogError ("Exception while analyzing ast for unit tests.", ex);
					return;
				}
				if (token.IsCancellationRequested)
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
					if (actionMargin.IsVisible ^ (visitor.FoundTests.Count > 0))
						textEditor.QueueDraw ();
					actionMargin.IsVisible = visitor.FoundTests.Count > 0;
					foreach (var oldMarker in currentMarker)
						editor.Document.RemoveMarker (oldMarker);

					foreach (var foundTest in visitor.FoundTests) {
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
			readonly NUnitVisitor.UnitTest unitTest;
			readonly MonoDevelop.Ide.Gui.Document doc;

			public UnitTestMarker(NUnitVisitor.UnitTest unitTest, MonoDevelop.Ide.Gui.Document doc)
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

					Stack<UnitTest> tests = new Stack<UnitTest> ();
					foreach (var test in NUnitService.Instance.RootTests) {
						tests.Push (test);
					}
					while (tests.Count > 0) {
						var test = tests.Pop ();

						if (test is SolutionFolderTestGroup) {
							foreach (var test2 in ((SolutionFolderTestGroup)test).Tests) {
								tests.Push (test2); 
							}
							continue;
						}
						if (test is NUnitProjectTestSuite)
							testSuites.Add ((NUnitProjectTestSuite)test); 
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

					var pad = IdeApp.Workbench.GetPad<TestPad> ();
					var content = (TestPad)pad.Content;
					content.RunTest (test, ctx);
				}
			}

			bool isFailed;
			string failMessage;
			public override void DrawForeground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
			{
				cr.Arc (metrics.X + metrics.Width / 2 + 2, metrics.Y + metrics.Height / 2, 7 * editor.Options.Zoom, 0, Math.PI * 2);
				isFailed = false;
				var test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier);
				bool searchCases = false;

				if (unitTest.IsIgnored) {
					cr.SetSourceRGB (0.9, 0.9, 0);
				} else {

					if (test != null) {
						var result = test.GetLastResult ();
						if (result == null) {
							cr.SetSourceRGB (0.5, 0.5, 0.5);
							searchCases = true;

						} else if (result.IsNotRun) {
							cr.SetSourceRGBA (0.9, 0.9, 0, test.IsHistoricResult ? 0.5 : 1.0);
						} else if (result.IsSuccess) {
							cr.SetSourceRGBA (0, 1, 0, test.IsHistoricResult ? 0.2 : 1.0);
						} else if (result.IsFailure) {
							cr.SetSourceRGBA (1, 0, 0, test.IsHistoricResult ? 0.2 : 1.0);
							failMessage = result.Message;
							isFailed = true;
						} else if (result.IsInconclusive) {
							cr.SetSourceRGBA (0, 1, 1, test.IsHistoricResult ? 0.2 : 1.0);
						} 
					} else {
						cr.SetSourceRGB (0.5, 0.5, 0.5);
						searchCases = true;
					}
					if (searchCases) {
						foreach (var caseId in unitTest.TestCases) {
							test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier + caseId);
							if (test != null) {
								var result = test.GetLastResult ();
								if (result == null || result.IsNotRun || test.IsHistoricResult) {
								} else if (result.IsNotRun) {
									cr.SetSourceRGB (0.9, 0.9, 0);
								} else if (result.IsSuccess) {
									cr.SetSourceRGB (0, 1, 0);
								} else if (result.IsFailure) {
									cr.SetSourceRGB (1, 0, 0);
									failMessage = result.Message;
									isFailed = true;
									break;
								} else if (result.IsInconclusive) {
									cr.SetSourceRGB (0, 1, 1);
								} 
							}
						}
					}
				}

				cr.FillPreserve ();
				if (unitTest.IsIgnored) {
					cr.SetSourceRGB (0.4, 0.4, 0);
					cr.Stroke ();

				} else {
					if (test != null) {
						var result = test.GetLastResult ();
						if (result == null) {
							cr.SetSourceRGB (0.2, 0.2, 0.2);
							cr.Stroke ();
						} else if (result.IsNotRun && !test.IsHistoricResult) {
							cr.SetSourceRGB (0.4, 0.4, 0);
							cr.Stroke ();
						} else if (result.IsSuccess && !test.IsHistoricResult) {
							cr.SetSourceRGB (0, 0.5, 0);
							cr.Stroke ();
						} else if (result.IsFailure && !test.IsHistoricResult) {
							cr.SetSourceRGB (0.5, 0, 0);
							cr.Stroke ();
						} else if (result.IsInconclusive && !test.IsHistoricResult) {
							cr.SetSourceRGB (0, 0.7, 0.7);
							cr.Stroke ();
						} 
					}
				}
				cr.NewPath ();
			}
		}

		class NUnitVisitor : DepthFirstAstVisitor
		{
			readonly CSharpAstResolver resolver;
			List<UnitTest> foundTests = new List<UnitTest> ();

			public IList<UnitTest> FoundTests {
				get {
					return foundTests;
				}
			}

			public class UnitTest
			{
				public int LineNumber { get; set; }
				public bool IsFixture { get; set; }
				public string UnitTestIdentifier { get; set; }
				public bool IsIgnored { get; set; }

				public List<string> TestCases = new List<string> ();

				public UnitTest (int lineNumber)
				{
					this.LineNumber = lineNumber;
				}
			}

			public NUnitVisitor (CSharpAstResolver resolver)
			{
				this.resolver = resolver;
			}

			static string GetFullName (TypeDeclaration typeDeclaration)
			{
				var parts = new List<string> ();
				while (true) {
					parts.Add (typeDeclaration.Name);
					if (typeDeclaration.Parent is TypeDeclaration) {
						typeDeclaration = (TypeDeclaration)typeDeclaration.Parent;
					}
					else {
						break;
					}
				}
				;
				var ns = typeDeclaration.Parent as NamespaceDeclaration;
				if (ns != null)
					parts.Add (ns.FullName);
				parts.Reverse ();
				return string.Join (".", parts);
			}

			static void AppendConstant (StringBuilder sb, object constantValue)
			{
				if (constantValue is string)
					sb.Append ('"');
				if (constantValue is char)
					sb.Append ('\"');
				sb.Append (constantValue);
				if (constantValue is string)
					sb.Append ('"');
				if (constantValue is char)
					sb.Append ('\"');
			}

			static string BuildArguments (IAttribute attr)
			{
				var sb = new StringBuilder ();
				foreach (var arg in attr.PositionalArguments) {
					if (sb.Length > 0)
						sb.Append (", ");
					var cr = arg as ConversionResolveResult;
					if (cr != null) {
						AppendConstant (sb, cr.Input.ConstantValue);
						continue;
					}
					AppendConstant (sb, arg.ConstantValue);
				}
				return sb.ToString ();
			}

			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
			{
				var result = resolver.Resolve (methodDeclaration) as MemberResolveResult;
				if (result == null)
					return;
				var method = result.Member as IMethod;

				UnitTest test = null;
				foreach (var attr in method.Attributes) {
					if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestAttribute") {
						if (test == null) {
							test = new UnitTest (methodDeclaration.NameToken.StartLocation.Line);
							test.UnitTestIdentifier = GetFullName ((TypeDeclaration)methodDeclaration.Parent) + "." + methodDeclaration.Name;
							foundTests.Add (test);
						}
					}
				}
				if (test != null) {
					foreach (var attr in method.Attributes) {
						if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestCaseAttribute") {
							test.TestCases.Add ("(" + BuildArguments (attr) + ")");
						} else if (attr.AttributeType.ReflectionName == "NUnit.Framework.IgnoreAttribute") {
							test.IsIgnored = true;
						}
					}
				}
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				var result = resolver.Resolve (typeDeclaration);
				if (result == null || result.Type.GetDefinition () == null)
					return;
				UnitTest unitTest = null;
				bool isIgnored = false;

				foreach (var attr in result.Type.GetDefinition ().Attributes) {
					
					if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestFixtureAttribute") {
						unitTest = new UnitTest (typeDeclaration.NameToken.StartLocation.Line);
						unitTest.IsFixture = true;
						unitTest.UnitTestIdentifier = GetFullName (typeDeclaration);
						foundTests.Add (unitTest);
					}
					else if (attr.AttributeType.ReflectionName == "NUnit.Framework.IgnoreAttribute") {
						isIgnored = true;
					}
				}
				if (unitTest != null) {
					unitTest.IsIgnored = isIgnored;
					base.VisitTypeDeclaration (typeDeclaration);
				}
			}

			public override void VisitBlockStatement (BlockStatement blockStatement)
			{
			}
		}
	}
}

