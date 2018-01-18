//
// CodeActionEditorExtensionTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.CodeActions;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.AnalysisCore;
using MonoDevelop.CodeIssues;

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture]
	class CodeActionEditorExtensionTests : Ide.IdeTestBase
	{
		static async Task<Tuple<CodeActionEditorExtension, Projects.Solution>> GatherFixesNoDispose<T> (string input, Action<ResultsEditorExtension, TaskCompletionSource<T>> callback, params int[] caretLocations)
		{
			await Ide.Composition.CompositionManager.InitializeAsync ();
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";

			var doc = new Ide.Gui.Document (tww);

			var text = input;
			content.Text = text;

			var project = Projects.Services.ProjectService.CreateProject ("C#");
			project.Name = "test";
			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile (content.ContentName, BuildAction.Compile));
			var solution = new Projects.Solution ();
			solution.AddConfiguration ("", true);
			solution.DefaultSolutionFolder.AddItem (project);
			content.Project = project;
			doc.SetProject (project);

			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);

			var resultsExt = new ResultsEditorExtension ();
			resultsExt.Initialize (doc.Editor, doc);
			content.Contents.Add (resultsExt);

			var compExt = new CodeActionEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			var tcs = new TaskCompletionSource<T> ();

			var cts = new CancellationTokenSource ();
			cts.CancelAfter (60 * 1000);
			cts.Token.Register (() => tcs.TrySetCanceled ());

			resultsExt.TasksUpdated += delegate {
				callback (resultsExt, tcs);
			};

			await doc.UpdateParseDocument ();

			await Task.Run (() => tcs.Task);

			return Tuple.Create (compExt, solution);
		}

		static async Task<CodeActionEditorExtension> GatherFixes<T> (string input, Action<ResultsEditorExtension, TaskCompletionSource<T>> callback)
		{
			var tuple = await GatherFixesNoDispose (input, callback);
			TypeSystemService.Unload (tuple.Item2);
			return tuple.Item1;
		}

		const string OneFromEach = @"class MyClass
{
}
";
		class CodeActionData
		{
			public string Message;
		}

		class ExpectedCodeFixes
		{
			public CodeActionData [] CodeFixData;
			public CodeActionData [] CodeRefactoringData;
		}
		// These tests can hang if we don't get enough updates (i.e. code changes)
		// So to not break CI, add a timeout to the test. These tests should take around 20s.
		[Test]
		public async Task FixesAreReportedByExtension ()
		{
			var expected = new ExpectedCodeFixes {
				CodeFixData = new CodeActionData [] {
					new CodeActionData { Message = "Add accessibility modifiers" },
				},
				CodeRefactoringData = new CodeActionData [] {
					new CodeActionData { Message = "To public" },
					new CodeActionData { Message = "Generate overrides..." },
					new CodeActionData { Message = "Generate constructor 'MyClass()'" },
					new CodeActionData { Message = "Rename file to MyClass.cs" },
					new CodeActionData { Message = "Rename type to a" },
				}
			};

			Projects.Solution sol = null;
			var old = IdeApp.Preferences.EnableSourceAnalysis;
			try {
				IdeApp.Preferences.EnableSourceAnalysis.Value = true;

				int expectedUpdates = 1;
				ImmutableArray<QuickTask> qt = ImmutableArray<QuickTask>.Empty;
				var tuple = await GatherFixesNoDispose<bool> (OneFromEach, (resultExt, tcs) => {
					if (--expectedUpdates == 0) {
						qt = resultExt.QuickTasks;
						tcs.SetResult (true);
					}
				});

				var ext = tuple.Item1;
				sol = tuple.Item2;

				Assert.AreEqual (1, qt.Length);	

				ext.Editor.CaretOffset = qt[0].Location;

				var fixes = await ext.GetCurrentFixesAsync (CancellationToken.None);
				AssertCodeFixes (fixes, expected);

			} finally {
				IdeApp.Preferences.EnableSourceAnalysis.Value = old;
				TypeSystemService.Unload (sol);
			}
		}

		const string IDisposableImplement = "class MyClass : System.IDisposable {}";
			
		[Test]
		public async Task FixesAreReportedForCompilerErrors ()
		{
			var expected = new ExpectedCodeFixes {
				CodeFixData = new CodeActionData [] {
					new CodeActionData { Message = "Implement interface" },
					new CodeActionData { Message = "Implement interface with Dispose pattern" },
					new CodeActionData { Message = "Implement interface explicitly" },
					new CodeActionData { Message = "Implement interface explicitly with Dispose pattern" },
				},
				CodeRefactoringData = new CodeActionData[] {
					new CodeActionData { Message = "To public" },
				},
			};

			Projects.Solution sol = null;
			var old = IdeApp.Preferences.EnableSourceAnalysis;
			try {
				IdeApp.Preferences.EnableSourceAnalysis.Value = true;

				int expectedUpdates = 2;
				ImmutableArray<Result> qt = ImmutableArray<Result>.Empty;
				var tuple = await GatherFixesNoDispose<bool> (IDisposableImplement, (resultExt, tcs) => {
					if (--expectedUpdates == 0) {
						qt = resultExt.GetResults ().ToImmutableArray ();
						tcs.SetResult (true);
					}
				});

				var ext = tuple.Item1;
				sol = tuple.Item2;

				Assert.AreEqual (2, qt.Length);

				var diag = qt.OfType<DiagnosticResult> ().Single (x => x.Diagnostic.Id == "CS0535");
				ext.Editor.CaretOffset = diag.Region.Start;

				var fixes = await ext.GetCurrentFixesAsync (CancellationToken.None);
				AssertCodeFixes (fixes, expected);

			} finally {
				IdeApp.Preferences.EnableSourceAnalysis.Value = old;
				TypeSystemService.Unload (sol);
			}
		}

		static void AssertCodeFixes (CodeActionContainer fixes, ExpectedCodeFixes expected)
		{
			var fixActions = fixes.CodeFixActions.SelectMany (x => x.Fixes).ToArray ();

			Assert.AreEqual (expected.CodeFixData.Length, fixActions.Length);
			for (int j = 0; j < expected.CodeFixData.Length; ++j) {
				Assert.AreEqual (expected.CodeFixData [j].Message, fixActions [j].Action.Message);
			}

			var fixRefactorings = fixes.CodeRefactoringActions.SelectMany (x => x.Actions).ToArray ();

			Assert.AreEqual (expected.CodeRefactoringData.Length, fixRefactorings.Length);
			for (int j = 0; j < expected.CodeRefactoringData.Length; ++j) {
				Assert.AreEqual (expected.CodeRefactoringData [j].Message, fixRefactorings [j].Message);
			}
		}
	}
}
