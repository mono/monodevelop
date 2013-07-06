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
using Xwt;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Components.Docking;

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
			Document.DocumentParsed -= HandleDocumentParsed; 
			base.Dispose ();
		}

		CancellationTokenSource src = new CancellationTokenSource ();

		void HandleDocumentParsed (object sender, EventArgs e)
		{
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

					foreach (var result in visitor.FoundTests) {
						if (token.IsCancellationRequested)
							return;

						document.Editor.Document.AddMarker (result.LineNumber, new UnitTestMarker (result.UnitTestIdentifier, document));
					}
				});
			});
		}

		List<UnitTestMarker> currentMarker = new List<UnitTestMarker>();

		class UnitTestMarker : MarginMarker
		{
			readonly string id;
			readonly MonoDevelop.Ide.Gui.Document doc;

			public UnitTestMarker(string id, MonoDevelop.Ide.Gui.Document doc)
			{
				this.id = id;
				this.doc = doc;
			}

			public override bool CanDrawForeground (Margin margin)
			{
				return margin is ActionMargin;
			}

			public override void InformMousePress (TextEditor editor, Margin margin, MarginMouseEventArgs args)
			{
				if (IdeApp.ProjectOperations.IsBuilding (IdeApp.ProjectOperations.CurrentSelectedSolution) || 
				    IdeApp.ProjectOperations.IsRunning (IdeApp.ProjectOperations.CurrentSelectedSolution))
					return;
				var buildOperation = IdeApp.ProjectOperations.Build (IdeApp.ProjectOperations.CurrentSelectedSolution);

				buildOperation.Completed += delegate {
					var test = NUnitService.Instance.SearchTestById (id);
					if (test != null) {
						NUnitService.ResetResult (test.RootTest);
						NUnitService.Instance.RunTest (test, null).Completed += delegate {
							Application.Invoke (delegate { doc.Editor.Parent.QueueDraw (); });
						};
					}
				};
			}


			public override void DrawForeground (TextEditor editor, Cairo.Context cr, MarginDrawMetrics metrics)
			{
				cr.Arc (metrics.X + metrics.Width / 2, metrics.Y + metrics.Height / 2, metrics.Height / 3, 0, Math.PI * 2);
				var test = NUnitService.Instance.SearchTestById (id);
				if (test != null) {
					var result = test.GetLastResult ();
					if (result == null ||  result.IsNotRun || test.IsHistoricResult) {
						cr.Color = new Cairo.Color (0.5, 0.5, 0.5);
					} else
					if (result.IsSuccess) {
						cr.Color = new Cairo.Color (0, 1, 0);
					} else if (result.IsFailure) {
						cr.Color = new Cairo.Color (1, 0, 0);
					} else if (result.IsInconclusive) {
						cr.Color = new Cairo.Color (0, 1, 1);
					} 
				} else {
					cr.Color = new Cairo.Color (0.5, 0.5, 0.5);
				}
				cr.Fill ();
			}
		}

		class NUnitVisitor : DepthFirstAstVisitor
		{
			readonly CSharpAstResolver resolver;
			MethodDeclaration currentMethod;
			Stack<TypeDeclaration> currentType = new Stack<TypeDeclaration> ();
			List<UnitTest> foundTests = new List<UnitTest> ();

			public IList<UnitTest> FoundTests {
				get {
					return foundTests;
				}
			}

			public class UnitTest
			{
				public int LineNumber { get; set; }

				public string UnitTestIdentifier { get; set; }

				public UnitTest (int lineNumber, string unitTestIdentifier)
				{
					this.LineNumber = lineNumber;
					this.UnitTestIdentifier = unitTestIdentifier;
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

			public override void VisitAttribute (ICSharpCode.NRefactory.CSharp.Attribute attribute)
			{
				var result = resolver.Resolve (attribute);
				if (result.Type.ReflectionName == "NUnit.Framework.TestFixtureAttribute") {
					foundTests.Add (new UnitTest (attribute.StartLocation.Line, GetFullName (currentType.Peek ())));
				}

				if (result.Type.ReflectionName == "NUnit.Framework.TestAttribute") {
					foundTests.Add (new UnitTest (attribute.StartLocation.Line, GetFullName (currentType.Peek ()) + "." + currentMethod.Name));
				}
			}

			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
			{
				currentMethod = methodDeclaration;
				base.VisitMethodDeclaration (methodDeclaration);
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				currentType.Push (typeDeclaration);
				base.VisitTypeDeclaration (typeDeclaration);
				currentType.Pop ();
			}

			public override void VisitBlockStatement (BlockStatement blockStatement)
			{
			}
		}
	}
}

