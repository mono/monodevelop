//
// TextMateDocumentIndentEngineTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.TextMate;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using UnitTests;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class TextMateDocumentIndentEngineTests : TestBase
	{
		[Test]
		public void TestIncreaseIndent ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();

			editor.Text = @"do
";

			var engine = new CacheIndentEngine (new TextMateDocumentIndentEngine (
				editor,
				new Regex ("^\\s*do\\s*$"),
				null,
				null,
				null
			));

			engine.Update (editor, 2);

			Assert.AreEqual ("", engine.CurrentIndent);
			Assert.AreEqual ("\t", engine.ThisLineIndent);
			Assert.AreEqual ("\t", engine.NextLineIndent);
		}


		[Test]
		public void TestDecreaseIndent ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();

			editor.Text = @"do
end
";

			var engine = new CacheIndentEngine (new TextMateDocumentIndentEngine (
				editor,
				new Regex ("^\\s*do\\s*$"),
				new Regex ("^\\s*end\\s*$"),
				null,
				null
			));

			engine.Update (editor, 3);

			Assert.AreEqual ("", engine.CurrentIndent);
			Assert.AreEqual ("", engine.ThisLineIndent);
			Assert.AreEqual ("", engine.NextLineIndent);
		}

		[Test]
		public void TestIndentNextLine ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();

			editor.Text = @"do
bar
baz";

			var engine = new CacheIndentEngine (new TextMateDocumentIndentEngine (
				editor,
				null,
				null,
				new Regex ("^\\s*do\\s*$"),
				null
			));

			engine.Update (editor, 2);

			Assert.AreEqual ("", engine.CurrentIndent);
			Assert.AreEqual ("\t", engine.ThisLineIndent);
			Assert.AreEqual ("", engine.NextLineIndent);
		}


		[Test]
		public void TestUnindentedLinePattern ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();

			editor.Text = @"do
foo
";

			var engine = new CacheIndentEngine (new TextMateDocumentIndentEngine (
				editor,
				new Regex ("^\\s*do\\s*$"),
				null,
				null,
				new Regex ("^\\s*foo\\s*$")
			));

			engine.Update (editor, 2);

			Assert.AreEqual ("", engine.CurrentIndent);
			Assert.AreEqual ("", engine.ThisLineIndent);
			Assert.AreEqual ("", engine.NextLineIndent);
		}
	}
}
