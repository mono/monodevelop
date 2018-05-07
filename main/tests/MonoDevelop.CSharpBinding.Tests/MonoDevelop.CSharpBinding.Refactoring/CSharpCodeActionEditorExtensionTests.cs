//
// CSharpCodeActionEditorExtensionTests.cs
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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring.Tests;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding.Refactoring
{
	[TestFixture]
	public class CSharpCodeActionEditorExtensionTests : CodeActionEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		const string SimpleClass = @"class MyClass
{
}
";
		// These tests can hang if we don't get enough updates (i.e. code changes)
		// So to not break CI, add a timeout to the test. These tests should take around 20s.
		[Test]
		public async Task FixesAreReportedByExtension ()
		{
			var diagnostic = new ExpectedDiagnostic [] {
				new ExpectedDiagnostic (6, DiagnosticSeverity.Hidden, "Accessibility modifiers required"),
			};

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

			await RunTest (1, SimpleClass, async (remainingUpdates, doc) => {
				if (remainingUpdates == 0) {
					AssertExpectedDiagnostics (diagnostic, doc);

					doc.Editor.CaretOffset = diagnostic [0].Location;
					await AssertExpectedCodeFixes (expected, doc);
				}
			});
		}

		const string IDisposableImplement = "class MyClass : System.IDisposable {}";

		[Test]
		public async Task FixesAreReportedForCompilerErrors ()
		{
			var diagnostics = new ExpectedDiagnostic [] {
				new ExpectedDiagnostic (6, DiagnosticSeverity.Hidden, "Accessibility modifiers required"),
				new ExpectedDiagnostic (16, DiagnosticSeverity.Error, "'MyClass' does not implement interface member 'IDisposable.Dispose()'"),
			};

			var expected = new ExpectedCodeFixes {
				CodeFixData = new CodeActionData [] {
					new CodeActionData { Message = "Implement interface" },
					new CodeActionData { Message = "Implement interface with Dispose pattern" },
					new CodeActionData { Message = "Implement interface explicitly" },
					new CodeActionData { Message = "Implement interface explicitly with Dispose pattern" },
				},
				CodeRefactoringData = new CodeActionData [] {
					new CodeActionData { Message = "To public" },
				},
			};

			await RunTest (2, IDisposableImplement, async (remainingUpdates, doc) => {
				if (remainingUpdates == 0) {
					AssertExpectedDiagnostics (diagnostics, doc);

					doc.Editor.CaretOffset = diagnostics [1].Location;
					await AssertExpectedCodeFixes (expected, doc);
				}
			});
		}
	}
}
