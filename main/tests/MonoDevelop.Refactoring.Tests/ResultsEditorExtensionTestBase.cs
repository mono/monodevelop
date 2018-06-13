//
// ResultsEditorExtensionTests.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using NUnit.Framework;

namespace MonoDevelop.Refactoring.Tests
{
	public struct ExpectedDiagnostic
	{
		public int Location;
		public DiagnosticSeverity Severity;
		public string Description;

		public ExpectedDiagnostic (int location, DiagnosticSeverity severity, string description)
		{
			Location = location;
			Severity = severity;
			Description = description;
		}
	}

	public abstract class ResultsEditorExtensionTestBase : TextEditorExtensionTestBase
	{
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield return new ResultsEditorExtension ();
		}

		protected void RegisterExtensionCallback<T> (Ide.Gui.Document doc, Action<T> registerCallback) where T:TextEditorExtension
		{
			var ext = doc.GetContent<T> ();
			registerCallback (ext);
		}

		protected async Task<Tuple<T, TextEditorExtensionTestCase>> GatherDiagnosticsNoDispose<T> (string input, Func<Ide.Gui.Document, TaskCompletionSource<T>, Task> callback)
		{
			var testCase = await SetupTestCase (input);
			var doc = testCase.Document;

			var tcs = new TaskCompletionSource<T> ();
			var resultsExt = doc.GetContent<ResultsEditorExtension> ();
			EventHandler handler = async (object o, EventArgs args) => {
				try {
					await callback (doc, tcs);
				} catch (Exception ex) {
					tcs.TrySetException (ex);
				}
			};
			resultsExt.TasksUpdated += handler;

			var cts = new CancellationTokenSource ();
			cts.Token.Register (() => tcs.TrySetCanceled ());
			cts.CancelAfter (60 * 1000);

			await doc.UpdateParseDocument ();

			var result = await Task.Run (() => tcs.Task).ConfigureAwait (false);
			resultsExt.TasksUpdated -= handler;

			return Tuple.Create (result, testCase);
		}

		protected async Task<T> GatherDiagnostics<T> (string input, Func<Ide.Gui.Document, TaskCompletionSource<T>, Task> callback)
		{
			var tuple = await GatherDiagnosticsNoDispose (input, callback);

			var testCase = tuple.Item2;
			testCase.Dispose ();

			return tuple.Item1;
		}

		protected static void AssertExpectedDiagnostics (IEnumerable<ExpectedDiagnostic> expected, Ide.Gui.Document doc)
		{
			var ext = doc.GetContent<ResultsEditorExtension> ();

			var actualDiagnostics = ext.QuickTasks;
			var expectedDiagnostics = expected.ToArray ();
			Assert.AreEqual (expectedDiagnostics.Length, actualDiagnostics.Length);

			for (int i = 0; i < expectedDiagnostics.Length; ++i) {
				AssertExpectedDiagnostic (expectedDiagnostics [i], ext, i);
			}
		}

		static void AssertExpectedDiagnostic (ExpectedDiagnostic expected, ResultsEditorExtension ext, int i)
		{
			var actual = ext.QuickTasks [i];
			Assert.AreEqual (expected.Location, actual.Location);
			Assert.AreEqual (expected.Severity, actual.Severity);
			Assert.AreEqual (expected.Description, actual.Description);
		}

		protected async Task RunTest (int expectedUpdates, string input, Func<int, Ide.Gui.Document, Task> onUpdate)
		{
			var old = IdeApp.Preferences.EnableSourceAnalysis;
			try {
				IdeApp.Preferences.EnableSourceAnalysis.Value = true;

				await OnRunTest (expectedUpdates, input, onUpdate);
			} finally {
				IdeApp.Preferences.EnableSourceAnalysis.Value = old;
			}
		}

		protected virtual Task OnRunTest (int expectedUpdates, string input, Func<int, Ide.Gui.Document, Task> onUpdate)
		{
			return GatherDiagnostics<bool> (input, async (doc, tcs) => {
				await onUpdate (--expectedUpdates, doc);

				if (expectedUpdates == 0)
					tcs.SetResult (true);
			});
		}
	}
}
