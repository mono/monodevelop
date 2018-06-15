//
// CodeCommentTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using NUnit.Framework;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Collections.Generic;
using System;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class CodeCommentTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			foreach (var ext in base.GetEditorExtensions ())
				yield return ext;
			yield return new DefaultCommandTextEditorExtension ();
		}

		internal static void SetupInput (TextEditor editor, string input)
		{
			editor.Options = new CustomEditorOptions ();

			var sb = new StringBuilder ();
			int cursorPosition = 0, selectionStart = -1, selectionEnd = -1;

			for (int i = 0; i < input.Length; i++) {
				var ch = input [i];
				switch (ch) {
				case '$':
					cursorPosition = sb.Length;
					break;
				case '<':
					if (i + 1 < input.Length) {
						if (input [i + 1] == '-') {
							selectionStart = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				case '-':
					if (i + 1 < input.Length) {
						var next = input [i + 1];
						if (next == '>') {
							selectionEnd = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				default:
					sb.Append (ch);
					break;
				}
			}
			editor.Text = sb.ToString ();
			editor.CaretOffset = cursorPosition;
			if (selectionStart >= 0 && selectionEnd >= 0)
				editor.SetSelection (selectionStart, selectionEnd);
		}

		static void AssertEditorState (TextEditor editor, string input)
		{
			var sb = new StringBuilder ();
			int cursorPosition = -1, selectionStart = -1, selectionEnd = -1;

			for (int i = 0; i < input.Length; i++) {
				var ch = input [i];
				switch (ch) {
				case '$':
					cursorPosition = sb.Length;
					break;
				case '<':
					if (i + 1 < input.Length) {
						if (input [i + 1] == '-') {
							selectionStart = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				case '-':
					if (i + 1 < input.Length) {
						var next = input [i + 1];
						if (next == '>') {
							selectionEnd = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				default:
					sb.Append (ch);
					break;
				}
			}
			if (cursorPosition >= 0)
				Assert.AreEqual (cursorPosition, editor.CaretOffset, "Cursor position mismatch.");
			if (selectionStart >= 0) {
				Assert.AreEqual (selectionStart, editor.SelectionRange.Offset, "Selection start mismatch.");
				Assert.AreEqual (selectionEnd, editor.SelectionRange.EndOffset, "Selection end mismatch.");
			}
			Assert.AreEqual (sb.ToString (), editor.Text, "Editor text doesn't match.");
		}

		async Task Run (string input, string expected, Action<DefaultCommandTextEditorExtension> cb)
		{
			using (var testCase = await SetupTestCase ("")) {
				var editor = testCase.Document.Editor;
				SetupInput (editor, input);
				cb (testCase.GetContent<DefaultCommandTextEditorExtension> ());
				AssertEditorState (editor, expected);
			}
		}

		[Test]
		public async Task TestAddComment()
		{
			const string input = @"class Foo
{
	<-void Bar ()
	{

	}->
}";
			const string expected = @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}";
			await Run (input, expected, ext => ext.AddCodeComment ());
		}

		[Test]
		public async Task TestRemoveComment()
		{
			const string input = @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}";
			const string expected = @"class Foo
{
	<-void Bar ()
	{

	}->
}";
			await Run (input, expected, ext => ext.RemoveCodeComment ());
		}

		[Test]
		public async Task TestToggle_Visible ()
		{
			//We need to create full document and not just editor
			//so extensions are initialized which set custom C#
			//tagger based syntax highligthing
			const string input = @"class Foo
{
	void Bar ()
	{
		//$test
	}
}";
			using (var testCase = await SetupTestCase ("", wrap: true)) {
				var editor = testCase.Document.Editor;
				SetupInput (editor, input);

				//Call UpdateParseDocument so AdHock Roslyn Workspace is created for file
				await testCase.Document.UpdateParseDocument ();
			
				//Finnaly call command Update so it sets values which we assert
				var info = new Components.Commands.CommandInfo ();
				testCase.GetContent<DefaultCommandTextEditorExtension> ().OnUpdateToggleComment (info);
				Assert.AreEqual (true, info.Visible);
				Assert.AreEqual (true, info.Enabled);
			}
		}



		[Test]
		public async Task TestToggle_Visible_StartOfLine ()
		{
			//We need to create full document and not just editor
			//so extensions are initialized which set custom C#
			//tagger based syntax highligthing
			const string input = @"class Foo
{
	void Bar ()
$	{
		//test
	}
}";
			using (var testCase = await SetupTestCase ("", wrap: true)) {
				var editor = testCase.Document.Editor;
				SetupInput (editor, input);

				//Call UpdateParseDocument so AdHock Roslyn Workspace is created for file
				await testCase.Document.UpdateParseDocument ();

				//Finnaly call command Update so it sets values which we assert
				var info = new Components.Commands.CommandInfo ();
				testCase.GetContent<DefaultCommandTextEditorExtension> ().OnUpdateToggleComment (info);
				Assert.AreEqual (true, info.Visible);
				Assert.AreEqual (true, info.Enabled);
			}
		}

		[Test]
		public async Task TestToggle_AddAsync ()
		{
			const string input = @"class Foo
{
	<-void Bar ()
	{

	}->
}";
			const string expected = @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}";
			await Run (input, expected, ext => ext.ToggleCodeComment ());
		}
	
		[Test]
		public async Task TestToggle_RemoveAsync ()
		{
			const string input = @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}";
			const string expected = @"class Foo
{
	<-void Bar ()
	{

	}->
}";
			await Run (input, expected, ext => ext.ToggleCodeComment ());
		}


		[Test]
		public async Task TestToggle_BugAsync ()
		{
			const string input = @"<-class Foo
{
	void Bar ()
	{

	}
}
->";
			const string expected = @"//class Foo
//{
//	void Bar ()
//	{

//	}
//}
";
			await Run (input, expected, ext => ext.ToggleCodeComment ());
		}

		[Test]
		public async Task TestToggleWithoutSelectionAsync ()
		{
			const string input = @"class Foo
{
	$void Bar ()
	{
	}
}";
			const string expected = @"class Foo
{
	//void Bar ()
	{
	}
}";

			await Run (input, expected, ext => ext.ToggleCodeComment ());
		}


		/// <summary>
		/// Bug 38355 - comment selected lines puts a comment on too many lines! 
		/// </summary>
		[Test]
		public async Task TestBug38355Async ()
		{
			const string input = @"class Foo
{
<-	void Bar ()
	{
->		Bar();
	}
}";
			const string expected = @"class Foo
{
	//void Bar ()
	//{
		Bar();
	}
}";

			await Run (input, expected, ext => ext.ToggleCodeComment ());
		}
	}
}

