//
// CSharpResultsEditorExtensionTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring.Tests;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture]
	public class CSharpResultsEditorExtensionTests : ResultsEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

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
		static readonly ExpectedDiagnostic [] OneFromEachDiagnostics = {
			// Compiler diagnostics
			new ExpectedDiagnostic (251, DiagnosticSeverity.Error, "; expected"),
			new ExpectedDiagnostic (254, DiagnosticSeverity.Error, "} expected"),

			new ExpectedDiagnostic (68, DiagnosticSeverity.Hidden, "Accessibility modifiers required"),
			new ExpectedDiagnostic (248, DiagnosticSeverity.Error, "The name 'cls' does not exist in the current context"),
			new ExpectedDiagnostic (144, DiagnosticSeverity.Info, "Empty constructor is redundant"),
		};

		// These tests can hang if we don't get enough updates (i.e. code changes)
		// So to not break CI, add a timeout to the test. These tests should take around 20s.
		[Test]
		public async Task DiagnosticsAreReportedByExtension ()
		{
			await RunTest (4, OneFromEach, (remainingUpdates, doc) => {
				if (remainingUpdates == 0) {
					AssertExpectedDiagnostics (OneFromEachDiagnostics, doc);
				}
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task DiagnosticEnableSourceAnalysisChanged ()
		{
			await RunTest (5, OneFromEach, (remainingUpdates, doc) => {
				if (remainingUpdates == 4) {
					AssertExpectedDiagnostics (OneFromEachDiagnostics.Take (2), doc);
				}

				if (remainingUpdates == 1) {
					AssertExpectedDiagnostics (OneFromEachDiagnostics, doc);
					IdeApp.Preferences.EnableSourceAnalysis.Value = false;
				}

				if (remainingUpdates == 0) {
					AssertExpectedDiagnostics (new ExpectedDiagnostic [0], doc);
				}
				return Task.CompletedTask;
			});
		}
	}
}
