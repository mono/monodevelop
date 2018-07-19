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
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class EditorConfigTests : IdeTestBase
	{

		static async Task InvokeEditConfigTest (string editConfig, Action<TextEditor> test)
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
				string fileName = Path.Combine (tempPath, "a.cs");
				var ctx = await EditorConfigService.GetEditorConfigContext (fileName);
				((DefaultSourceEditorOptions)editor.Options).SetContext (ctx);
				test (editor);
			} finally {
				Directory.Delete (tempPath, true);
			}
		}

		[Test]
		public async Task Test_indent_style ()
		{
			await InvokeEditConfigTest (@"
root = true
[*.cs]
indent_style = space
", editor => {
				Assert.AreEqual (true, editor.Options.TabsToSpaces);
			});
			await InvokeEditConfigTest (@"
root = true
[*.cs]
indent_style = tab
", editor => {
				Assert.AreEqual (false, editor.Options.TabsToSpaces);
			});
		}

		[Test]
		public async Task Test_indent_size ()
		{
			await InvokeEditConfigTest (@"
root = true
[*.cs]
indent_size = 42
", editor => {
				Assert.AreEqual (42, editor.Options.IndentationSize);
			});
		}

		[Test]
		public async Task Test_tab_width ()
		{
			await InvokeEditConfigTest (@"
root = true
[*.cs]
tab_width = 42
", editor => {
				Assert.AreEqual (42, editor.Options.TabSize);
			});
		}

		[Test]
		public async Task Test_end_of_line ()
		{
			await InvokeEditConfigTest (@"
root = true
[*.cs]
end_of_line = lf
", editor => {
				Assert.AreEqual ("\n", editor.Options.DefaultEolMarker);
			});
			await InvokeEditConfigTest (@"
root = true
[*.cs]
end_of_line = cr
", editor => {
				Assert.AreEqual ("\r", editor.Options.DefaultEolMarker);
			});
			await InvokeEditConfigTest (@"
root = true
[*.cs]
end_of_line = crlf
", editor => {
				Assert.AreEqual ("\r\n", editor.Options.DefaultEolMarker);
			});
		}

		[Test]
		public async Task Test_trim_trailing_whitespace ()
		{
			await InvokeEditConfigTest (@"
root = true
[*.cs]
trim_trailing_whitespace = true
", editor => {
				Assert.AreEqual (true, editor.Options.RemoveTrailingWhitespaces);
			});

			await InvokeEditConfigTest (@"
root = true
[*.cs]
trim_trailing_whitespace = false
", editor => {
				Assert.AreEqual (false, editor.Options.RemoveTrailingWhitespaces);
			});
		}

		[Test]
		public async Task Test_max_line_length ()
		{
			await InvokeEditConfigTest (@"
root = true
[*.cs]
max_line_length = off
", editor => {
				Assert.AreEqual (false, editor.Options.ShowRuler);
			});

			await InvokeEditConfigTest (@"
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
