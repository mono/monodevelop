//
// Test.cs
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
using MonoDevelop.AnalysisCore;
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

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture]
	class ResultsEditorExtensionTests : Ide.IdeTestBase
	{
		static async Task<Tuple<T, Projects.Solution>> GatherDiagnosticsNoDispose<T> (string input, Action<ResultsEditorExtension, TaskCompletionSource<T>> callback)
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
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);
			content.Project = project;
			doc.SetProject (project);

			var compExt = new ResultsEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			var tcs = new TaskCompletionSource<T> ();

			var cts = new CancellationTokenSource ();
			cts.CancelAfter (60 * 1000);
			cts.Token.Register (() => tcs.TrySetCanceled ());

			compExt.TasksUpdated += delegate {
				callback (compExt, tcs);
			};

			await doc.UpdateParseDocument ();

			return Tuple.Create (await Task.Run (() => tcs.Task), solution);
		}

		static async Task<T> GatherDiagnostics<T> (string input, Action<ResultsEditorExtension, TaskCompletionSource<T>> callback)
		{
			var tuple = await GatherDiagnosticsNoDispose (input, callback);
			TypeSystemService.Unload (tuple.Item2);
			return tuple.Item1;
		}

		const string OneFromEach = @"
// Roslyn will say accesibility modifiers are required here.
class MyClass
{
	// RefactoringEssentials will mark the constructor as redundant
	public MyClass() {}

	public void CompilerError ()
	{
		// Roslyn compiler will mark this as an error
		cls
	}
";
		static readonly ExpectedDiagnostic[] OneFromEachDiagnostics = {
			new ExpectedDiagnostic (68, DiagnosticSeverity.Hidden, "Accessibility modifiers required"),
			new ExpectedDiagnostic (248, DiagnosticSeverity.Error, "The name 'cls' does not exist in the current context"),
			new ExpectedDiagnostic (144, DiagnosticSeverity.Info, "Empty constructor is redundant"),
		};

		struct ExpectedDiagnostic
		{
			public int Location;
			public DiagnosticSeverity Severity;
			public string Description;

			public ExpectedDiagnostic(int location, DiagnosticSeverity severity, string description)
			{
				Location = location;
				Severity = severity;
				Description = description;
			}
		}

		void AssertExpectedDiagnostic(ExpectedDiagnostic expected, QuickTask actual)
		{
			Assert.AreEqual (expected.Location, actual.Location);
			Assert.AreEqual (expected.Severity, actual.Severity);
			Assert.AreEqual (expected.Description, actual.Description);
		}

		// These tests can hang if we don't get enough updates (i.e. code changes)
		// So to not break CI, add a timeout to the test. These tests should take around 20s.
		[Test]
		public async Task DiagnosticsAreReportedByExtension ()
		{
			var old = IdeApp.Preferences.EnableSourceAnalysis;
			try {
				IdeApp.Preferences.EnableSourceAnalysis.Value = true;

				int expectedUpdates = 4;
				var diagnostics = await GatherDiagnostics<ImmutableArray<QuickTask>> (OneFromEach, (ext, tcs) => {
					if (--expectedUpdates == 0)
						tcs.SetResult (ext.QuickTasks);
				});

				Assert.AreEqual (OneFromEachDiagnostics.Length, diagnostics.Length);

				for (int i = 0; i < 3; ++i) {
					AssertExpectedDiagnostic (OneFromEachDiagnostics [i], diagnostics [i]);
				}
			} finally {
				IdeApp.Preferences.EnableSourceAnalysis.Value = old;
			}
		}

		[Test]
		public async Task DiagnosticEnableSourceAnalysisChanged ()
		{
			var old = IdeApp.Preferences.EnableSourceAnalysis;
			Projects.Solution sol = null;
			try {
				int expectedUpdates = 5;
				IdeApp.Preferences.EnableSourceAnalysis.Value = true;

				var tuple = await GatherDiagnosticsNoDispose<bool> (OneFromEach, (ext, tcs) => {
					--expectedUpdates;
					if (expectedUpdates == 4) {
						Assert.AreEqual (0, ext.QuickTasks.Length);
					}

					if (expectedUpdates == 1) {
						IdeApp.Preferences.EnableSourceAnalysis.Value = false;

						Assert.AreEqual (3, ext.QuickTasks.Length);
						for (int i = 0; i < 3; ++i) {
							AssertExpectedDiagnostic (OneFromEachDiagnostics [i], ext.QuickTasks [i]);
						}
					}
					if (expectedUpdates == 0) {
						Assert.AreEqual (0, ext.QuickTasks.Length);
						tcs.SetResult (true);
					}
				});
				sol = tuple.Item2;
			} finally {
				IdeApp.Preferences.EnableSourceAnalysis.Value = old;
				if (sol != null)
					TypeSystemService.Unload (sol);
			}
		}
	}
}
