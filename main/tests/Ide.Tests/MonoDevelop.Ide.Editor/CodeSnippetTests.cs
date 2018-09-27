//
// CodeSnippetTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using UnitTests;
using NUnit.Framework.Internal;
using MonoDevelop.Core.Text;
using System.Text;
using NUnit.Framework;
using System.IO;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.SourceEditor;
using MonoDevelop.Ide.Editor.TextMate;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class CodeSnippetTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{ // need to instantiate a random text editor extension for the editor chain.
			yield return new TextMateCompletionTextEditorExtension ();
		}


		//Bug 620177: [Feedback] VS for Mac: code snippet problem
		[Test]
		public async Task TestBug620177 ()
		{
			var snippet = new CodeTemplate ();
			snippet.Code = "class Foo:Test\n{\n    $end$\n}";

			var editor = await RunSnippet (snippet);

			Assert.AreEqual ("class Foo : Test\n{\n\n}", editor.Text);
			Assert.AreEqual ("class Foo : Test\n{\n".Length, editor.CaretOffset);
		}

		async Task<TextEditor> RunSnippet (CodeTemplate snippet)
		{
			using (var testCase = await SetupTestCase ("")) {
				var doc = testCase.Document;
				doc.Editor.Options = new CustomEditorOptions (doc.Editor.Options) {
					IndentStyle = IndentStyle.Smart,
					RemoveTrailingWhitespaces = true
				};
				doc.Editor.IndentationTracker = new TestIndentTracker ("    ");
				await doc.UpdateParseDocument ();
				snippet.Insert (doc);
				return doc.Editor;
			}
		}

		[Test]
		public async Task TestVSTS685153 ()
		{
			using (var testCase = await SetupTestCase ("namespace Foo { Exception")) {
				var doc = testCase.Document;
				doc.Editor.CaretOffset = doc.Editor.Length;
				var extensibleEditor = doc.Editor.GetContent<SourceEditorView> ().TextEditor;
				Assert.IsTrue (extensibleEditor.DoInsertTemplate (doc.Editor, testCase.Document));
			}
		}


		class TestIndentTracker : IndentationTracker
		{
			string indentString;
			Dictionary<int, string> definedIndents = new Dictionary<int, string> ();

			public override IndentationTrackerFeatures SupportedFeatures {
				get {
					return IndentationTrackerFeatures.All;
				}
			}

			public TestIndentTracker (string indentString = "\t\t")
			{
				this.indentString = indentString;
			}

			public override string GetIndentationString (int lineNumber)
			{
				if (definedIndents.TryGetValue (lineNumber, out string result))
					return result;
				return indentString;
			}

			public void SetIndent (int lineNumber, string indentationString)
			{
				definedIndents [lineNumber] = indentationString;
			}
		}

	}
}

