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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.CodeActions;
using MonoDevelop.Ide.Editor.Extension;
using NUnit.Framework;

namespace MonoDevelop.Refactoring.Tests
{
	public class CodeActionData
	{
		public string Message;
	}

	public class ExpectedCodeFixes
	{
		public CodeActionData [] CodeFixData;
		public CodeActionData [] CodeRefactoringData;
	}

	public abstract class CodeActionEditorExtensionTestBase : ResultsEditorExtensionTestBase
	{
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			foreach (var ext in base.GetEditorExtensions ())
				yield return ext;
			yield return new CodeActionEditorExtension ();
		}

		protected static async Task AssertExpectedCodeFixes (ExpectedCodeFixes expected, Ide.Gui.Document doc)
		{
			var fixes = await doc.GetContent<CodeActionEditorExtension> ().GetCurrentFixesAsync (CancellationToken.None);
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
