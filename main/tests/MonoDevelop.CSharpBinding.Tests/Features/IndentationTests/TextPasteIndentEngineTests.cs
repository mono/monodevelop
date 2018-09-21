//
// TextPasteIndentEngineTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using System.IO;
using System;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;

namespace ICSharpCode.NRefactory6.IndentationTests
{
	[TestFixture]
	class TextPasteIndentEngineTests : TextEditorExtensionTestBase
	{
		internal async Task<TextEditorExtensionTestCase> CreateEngine (string text)
		{
			var sb = new StringBuilder ();
			int offset = 0;
			for (int i = 0; i < text.Length; i++) {
				var ch = text [i];
				if (ch == '$') {
					offset = i;
					continue;
				}
				sb.Append (ch);
			}

			return await SetupTestCase (sb.ToString (), offset);
		}

		static OptionSet CreateInvariantOptions ()
		{
			return null;
		}

		CSharpTextPasteHandler CreateTextPasteIndentEngine (TextEditorExtensionTestCase testCase, OptionSet optionSet)
		{
			var indent = testCase.GetContent<CSharpTextEditorIndentation> ();
			return new CSharpTextPasteHandler (indent, optionSet) {
				InUnitTestMode = true
			};
		}

		[Test]
		public async Task TestSimplePaste ()
		{
			using (var testCase = await CreateEngine (@"
class Foo
{
	void Bar ()
	{
		System.Console.WriteLine ($);
	}
}")) {
				var handler = CreateTextPasteIndentEngine (testCase, FormattingOptionsFactory.CreateMono ());
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "Foo", null);
				Assert.AreEqual ("Foo", text);
			}
		}

		[Test]
		public async Task PasteVerbatimString ()
		{
			using (var testCase = await CreateEngine (@"
class Foo
{
void Bar ()
{
	
}
}")) {
				var handler = CreateTextPasteIndentEngine (testCase, FormattingOptionsFactory.CreateMono ());
				var str = "string str = @\"\n1\n\t2 \n\t\t3\n\";";
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, str, null);
				Assert.AreEqual (str, text);
			}
		}

		[Ignore ("This option isn't part of the roslyn option set")]
		[Test]
		public async Task TestPasteBlankLinesAndIndent ()
		{
			using (var testCase = await CreateEngine ("class Foo\n{\n\tvoid Bar ()\n\t{\n\t\tSystem.Console.WriteLine ($);\n\t}\n}")) {
				var options = FormattingOptionsFactory.CreateMono ();
				//			options.EmptyLineFormatting = EmptyLineFormatting.Indent;
				var handler = CreateTextPasteIndentEngine (testCase, options);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "\n\n\n", null);
				Assert.AreEqual ("\n\t\t\t\n\t\t\t\n\t\t\t", text);
			}
		}

		[Test]
		public async Task TestPasteComments ()
		{
			using (var testCase = await CreateEngine (@"
class Foo
{
	$
}")) {
				var handler = CreateTextPasteIndentEngine (testCase, FormattingOptionsFactory.CreateMono ());
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "// Foo\n\t// Foo 2\n\t// Foo 3", null);
				Assert.AreEqual ("// Foo\n\t// Foo 2\n\t// Foo 3", text);
			}
		}

		[Ignore ("This option isn't part of the roslyn option set")]
		[Test]
		public async Task PastePreProcessorDirectivesNoIndent ()
		{
			var opt = FormattingOptionsFactory.CreateMono ();
			//			opt.IndentPreprocessorDirectives = false;

			using (var testCase = await CreateEngine (@"
class Foo
{
$
}")) {
				var handler = CreateTextPasteIndentEngine (testCase, opt);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "#if DEBUG\n\tvoid Foo()\n\t{\n\t}\n#endif", null);
				Assert.AreEqual ("#if DEBUG\n\tvoid Foo()\n\t{\n\t}\n#endif", text);
			}
		}

		[Test]
		public async Task PasteInUnterminatedString ()
		{
			var opt = FormattingOptionsFactory.CreateMono ();
			//	opt.IndentPreprocessorDirectives = false;

			using (var testCase = await CreateEngine (@"
var foo = ""hello$
")) {
				var handler = CreateTextPasteIndentEngine (testCase, opt);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "Hi \" + username;", null);
				Assert.AreEqual ("Hi \" + username;", text);
			}
		}

		[Test]
		public async Task PasteInTerminatedString ()
		{
			var opt = FormattingOptionsFactory.CreateMono ();
			//opt.IndentPreprocessorDirectives = false;

			using (var testCase = await CreateEngine (@"
var foo = ""hello$"";
")) {
				var handler = CreateTextPasteIndentEngine (testCase, opt);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "Hi \" + username;", null);
				Assert.AreEqual ("Hi \\\" + username;", text);
			}
		}

		[Test]
		public async Task PasteInUnterminatedVerbatimString ()
		{
			var opt = FormattingOptionsFactory.CreateMono ();
			//opt.IndentPreprocessorDirectives = false;

			using (var testCase = await CreateEngine (@"
var foo = @""hello$
")) {
				var handler = CreateTextPasteIndentEngine (testCase, opt);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "Hi \" + username;", null);
				Assert.AreEqual ("Hi \" + username;", text);
			}
		}

		[Test]
		public async Task PasteInTerminatedVerbatimString ()
		{
			var opt = FormattingOptionsFactory.CreateMono ();
			//opt.IndentPreprocessorDirectives = false;

			using (var testCase = await CreateEngine (@"
var foo = @""hello$"";
")) {
				var handler = CreateTextPasteIndentEngine (testCase, opt);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "Hi \" + username;", null);
				Assert.AreEqual ("Hi \"\" + username;", text);
			}
		}


		/// <summary>
		/// Cut and paste is broken in the editor #6029
		/// </summary>
		[Test]
		public async Task TestIssue6029 ()
		{
			var opt = FormattingOptionsFactory.CreateMono ();
			using (var testCase = await CreateEngine (@"
	string[] test = new [] {
$		""foo"",
		""foo"",
	};
")) {
				var handler = CreateTextPasteIndentEngine (testCase, opt);
				var text = handler.FormatPlainText (testCase.Content.CursorPosition, "\t\t\"foo\"", null);
				Assert.AreEqual ("\t\t\"foo\"", text);
			}
		}


		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield return new CSharpTextEditorIndentation ();
		}

		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;
	}
}

