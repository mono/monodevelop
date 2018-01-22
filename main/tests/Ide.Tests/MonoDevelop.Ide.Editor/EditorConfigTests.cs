//
// EditorConfigTests.cs
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

using System.IO;
using NUnit.Framework;
using UnitTests;
using System.Threading;
using GuiUnit;
using System;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class EditorConfigTests : IdeTestBase
	{

		static void InvokeEditConfigTest (string editConfig, Action<TextEditor> test)
		{
			string tempPath;
			int i = 0;
			while (true) {
				tempPath = Path.Combine (Path.GetTempPath (), Path.GetRandomFileName ());
				if (!File.Exists (tempPath))
					break;
				if (i++ > 100)
					Assert.Fail ("Can't create random path.");
			}

			Directory.CreateDirectory (tempPath);
			try {
				string editorConfigFile = Path.Combine (tempPath, ".editorconfig");
				File.WriteAllText (editorConfigFile, editConfig);
				var editor = TextEditorFactory.CreateNewEditor ();
				var viewContent = editor.GetViewContent ();
				editor.OptionsChanged += delegate {
					test (editor);
				};
				viewContent.ContentName = Path.Combine (tempPath, "a.cs");

			} finally {
				Directory.Delete (tempPath, true);
			}
		}

		[Test]
		public void Test_indent_style ()
		{
			InvokeEditConfigTest (@"
root = true
[*.cs]
indent_style = space
", editor => {
				Assert.AreEqual (true, editor.Options.TabsToSpaces);
			});
			InvokeEditConfigTest (@"
root = true
[*.cs]
indent_style = tab
", editor => {
				Assert.AreEqual (false, editor.Options.TabsToSpaces);
			});
		}

		[Test]
		public void Test_indent_size ()
		{
			InvokeEditConfigTest (@"
root = true
[*.cs]
indent_size = 42
", editor => {
				Assert.AreEqual (42, editor.Options.IndentationSize);
			});
		}

		[Test]
		public void Test_tab_width ()
		{
			InvokeEditConfigTest (@"
root = true
[*.cs]
tab_width = 42
", editor => {
				Assert.AreEqual (42, editor.Options.TabSize);
			});
		}

		[Test]
		public void Test_end_of_line ()
		{
			InvokeEditConfigTest (@"
root = true
[*.cs]
end_of_line = lf
", editor => {
				Assert.AreEqual ("\n", editor.Options.DefaultEolMarker);
			});
			InvokeEditConfigTest (@"
root = true
[*.cs]
end_of_line = cr
", editor => {
				Assert.AreEqual ("\r", editor.Options.DefaultEolMarker);
			});
			InvokeEditConfigTest (@"
root = true
[*.cs]
end_of_line = crlf
", editor => {
				Assert.AreEqual ("\r\n", editor.Options.DefaultEolMarker);
			});
		}

		[Test]
		public void Test_trim_trailing_whitespace ()
		{
			InvokeEditConfigTest (@"
root = true
[*.cs]
trim_trailing_whitespace = true
", editor => {
				Assert.AreEqual (true, editor.Options.RemoveTrailingWhitespaces);
			});

			InvokeEditConfigTest (@"
root = true
[*.cs]
trim_trailing_whitespace = false
", editor => {
				Assert.AreEqual (false, editor.Options.RemoveTrailingWhitespaces);
			});
		}

		[Test]
		public void Test_max_line_length ()
		{
			InvokeEditConfigTest (@"
root = true
[*.cs]
max_line_length = off
", editor => {
				Assert.AreEqual (false, editor.Options.ShowRuler);
			});

			InvokeEditConfigTest (@"
root = true
[*.cs]
max_line_length = 42
", editor => {
				Assert.AreEqual (true, editor.Options.ShowRuler);
				Assert.AreEqual (42, editor.Options.RulerColumn);
			});
		}




	}
}
