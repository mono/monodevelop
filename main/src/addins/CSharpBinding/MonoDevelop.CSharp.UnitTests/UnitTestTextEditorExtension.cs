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
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Components.Docking;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Components.Commands;
using Gdk;
using Gtk;
using System.Text;
using MonoDevelop.AnalysisCore;

namespace MonoDevelop.CSharp
{
	class UnitTestTextEditorExtension : TextEditorExtension
	{
		public override void Initialize ()
		{
			base.Initialize ();
			Document.DocumentParsed += HandleDocumentParsed; 
		}

		public override void Dispose ()
		{
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
				if (resolver.Result == null)
					return;
				var visitor = new NUnitVisitor (resolver.Result);
				try {
					visitor.VisitSyntaxTree (document.ParsedDocument.GetAst<SyntaxTree> ());
				} catch (Exception ex) {
					LoggingService.LogError ("Exception while analyzing ast for unit tests.", ex);
					return;
				}
				if (token.IsCancellationRequested)
					return;
				Application.Invoke (delegate {
					if (document.Editor.Parent.ActionMargin.IsVisible ^ (visitor.FoundTests.Count > 0))
						document.Editor.Parent.QueueDraw ();
					document.Editor.Parent.ActionMargin.IsVisible = visitor.FoundTests.Count > 0;

					foreach (var oldMarker in currentMarker)
						document.Editor.Document.RemoveMarker (oldMarker);

					foreach (var foundTest in visitor.FoundTests) {
						if (token.IsCancellationRequested)
							return;
						var unitTestMarker = new UnitTestMarker (foundTest, document);
						currentMarker.Add (unitTestMarker);
						document.Editor.Document.AddMarker (foundTest.LineNumber, unitTestMarker);
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
								if (result.IsFailure) {
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

			static Gtk.Menu menu;

			public override void InformMousePress (TextEditor editor, Margin margin, MarginMouseEventArgs args)
			{
				if (menu != null) {
					menu.Destroy ();
				}
				var debugModeSet = Runtime.ProcessService.GetDebugExecutionMode ();

				menu = new Gtk.Menu ();
				if (unitTest.IsFixture) {
					var menuItem = new Gtk.MenuItem ("_Run All");
					menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, false).Run;
					menu.Add (menuItem);
					if (debugModeSet != null) {
						menuItem = new Gtk.MenuItem ("_Debug All");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Run;
						menu.Add (menuItem);
					}
				} else {
					if (unitTest.TestCases.Count == 0) {
						var menuItem = new Gtk.MenuItem ("_Run");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, false).Run;
						menu.Add (menuItem);
						if (debugModeSet != null) {
							menuItem = new Gtk.MenuItem ("_Debug");
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Run;
							menu.Add (menuItem);
						}
					} else {
						var menuItem = new Gtk.MenuItem ("_Run All");
						menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, false).Run;
						menu.Add (menuItem);
						if (debugModeSet != null) {
							menuItem = new Gtk.MenuItem ("_Debug All");
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier, true).Run;
							menu.Add (menuItem);
						}
						menu.Add (new Gtk.SeparatorMenuItem ());
						foreach (var id in unitTest.TestCases) {
							var submenu = new Gtk.Menu ();
							menuItem = new Gtk.MenuItem ("_Run");
							menuItem.Activated += new TestRunner (doc, unitTest.UnitTestIdentifier + id, false).Run;
							submenu.Add (menuItem);
							if (debugModeSet != null) {
								menuItem = new Gtk.MenuItem ("_Debug");
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

							var subMenuItem = new Gtk.MenuItem (label);
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
				readonly MonoDevelop.Ide.Gui.Document doc;
				readonly string testCase;
				readonly bool debug;

				public TestRunner (MonoDevelop.Ide.Gui.Document doc, string testCase, bool debug)
				{
					this.doc = doc;
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


				internal void Run (object sender, EventArgs e)
				{
					menu.Destroy ();
					menu = null;
					if (IdeApp.ProjectOperations.IsBuilding (IdeApp.ProjectOperations.CurrentSelectedSolution) || 
					    IdeApp.ProjectOperations.IsRunning (IdeApp.ProjectOperations.CurrentSelectedSolution))
						return;
					var buildOperation = IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedSolution);
					buildOperation.Completed += delegate {
						if (!buildOperation.Success)
							return;
						RemoveHandler ();
						timeoutHandler = GLib.Timeout.Add (200, TimeoutHandler);
					};
				}

				void RunTest (UnitTest test)
				{
					NUnitService.ResetResult (test.RootTest);
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
					NUnitService.Instance.RunTest (test, ctx).Completed += delegate {
						Application.Invoke (delegate {
							doc.Editor.Parent.QueueDraw ();
						});
					};
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
				if (test != null) {
					var result = test.GetLastResult ();
					if (result == null || result.IsNotRun) {
						cr.Color = new Cairo.Color (0.5, 0.5, 0.5);
						searchCases = true;
					} else if (result.IsSuccess) {
						cr.Color = new Cairo.Color (0, 1, 0, test.IsHistoricResult ? 0.2 : 1.0);
					} else if (result.IsFailure) {
						cr.Color = new Cairo.Color (1, 0, 0, test.IsHistoricResult ? 0.2 : 1.0);
						failMessage = result.Message;
						isFailed = true;
					} else if (result.IsInconclusive) {
						cr.Color = new Cairo.Color (0, 1, 1, test.IsHistoricResult ? 0.2 : 1.0);
					} 
				} else {
					cr.Color = new Cairo.Color (0.5, 0.5, 0.5);
					searchCases = true;
				}

				if (searchCases) {
					foreach (var caseId in unitTest.TestCases) {
						test = NUnitService.Instance.SearchTestById (unitTest.UnitTestIdentifier + caseId);
						if (test != null) {
							var result = test.GetLastResult ();
							if (result == null || result.IsNotRun || test.IsHistoricResult) {
							} else if (result.IsSuccess) {
								cr.Color = new Cairo.Color (0, 1, 0);
							} else if (result.IsFailure) {
								cr.Color = new Cairo.Color (1, 0, 0);
								failMessage = result.Message;
								isFailed = true;
								break;
							} else if (result.IsInconclusive) {
								cr.Color = new Cairo.Color (0, 1, 1);
							} 
						}
					}
				}

				cr.FillPreserve ();

				if (test != null) {
					var result = test.GetLastResult ();
					if (result == null || result.IsNotRun) {
						cr.Color = new Cairo.Color (0.2, 0.2, 0.2);
						cr.Stroke ();
					} else if (result.IsSuccess && !test.IsHistoricResult) {
						cr.Color = new Cairo.Color (0, 0.5, 0);
						cr.Stroke ();
					} else if (result.IsFailure && !test.IsHistoricResult) {
						cr.Color = new Cairo.Color (0.5, 0, 0);
						cr.Stroke ();
					} else if (result.IsInconclusive && !test.IsHistoricResult) {
						cr.Color = new Cairo.Color (0, 0.7, 0.7);
						cr.Stroke ();
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

			string GetFullName (TypeDeclaration typeDeclaration)
			{
				var parts = new List<string> ();

				while (true) {
					parts.Add (typeDeclaration.Name);
					if (typeDeclaration.Parent is TypeDeclaration) {
						typeDeclaration = (TypeDeclaration)typeDeclaration.Parent;
					} else {
						break;
					}
				};

				var ns = typeDeclaration.Parent as NamespaceDeclaration;
				if (ns != null)
					parts.Add (ns.FullName);
				parts.Reverse ();
				return string.Join (".", parts);
			}

			void AppendConstant (StringBuilder sb, object constantValue)
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

			string BuildArguments (IAttribute attr)
			{
				StringBuilder sb = new StringBuilder ();
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
					} else if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestCaseAttribute") {
						test.TestCases.Add ("(" + BuildArguments (attr) + ")");
					}
				}
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				var result = resolver.Resolve (typeDeclaration);

				foreach (var attr in result.Type.GetDefinition ().Attributes) {
					
					if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestFixtureAttribute") {
						var unitTest = new UnitTest (typeDeclaration.NameToken.StartLocation.Line);
						unitTest.IsFixture = true;
						unitTest.UnitTestIdentifier = GetFullName (typeDeclaration);
						foundTests.Add (unitTest);
					}
				}
				base.VisitTypeDeclaration (typeDeclaration);
			}

			public override void VisitBlockStatement (BlockStatement blockStatement)
			{
			}
		}
	}
}

