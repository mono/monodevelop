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

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class CodeCommentTests : IdeTestBase
	{
		internal static TextEditor CreateTextEditor (string input)
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.FileName = "a.cs";
			editor.MimeType = "text/x-csharp";

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
			return editor;
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

		[Test]
		public void TestAddComment()
		{
			var editor = CreateTextEditor (@"class Foo
{
	<-void Bar ()
	{

	}->
}");
			GetExtension (editor).AddCodeComment ();
			AssertEditorState (editor, @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}");
		}

		static DefaultCommandTextEditorExtension GetExtension (TextEditor editor)
		{
			var ext = new DefaultCommandTextEditorExtension ();
			var tww = new TestWorkbenchWindow { ViewContent = new TestViewContent () };
			ext.Initialize (editor, new TestDocument (tww)); 
			return ext;
		}

		[Test]
		public void TestRemoveComment()
		{
			var editor = CreateTextEditor ( @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}");
			GetExtension (editor).RemoveCodeComment ();
			AssertEditorState (editor,@"class Foo
{
	<-void Bar ()
	{

	}->
}");
		}

		[Test]
		public async Task TestToggle_Visible ()
		{
			IdeApp.Initialize (new Core.ProgressMonitor ());
			//This dummyEditor is just so we can reuse CreateTextEditor code
			//to resolve offset for us
			var dummyEditor = CreateTextEditor (@"class Foo
{
	void Bar ()
	{
		//$test
	}
}");
			//We need to create full document and not just editor
			//so extensions are initialized which set custom C#
			//tagger based syntax highligthing
			var document = IdeApp.Workbench.NewDocument ("a.cs", "text/x-csharp", dummyEditor.Text);
			document.Editor.CaretOffset = dummyEditor.CaretOffset;
			//Call UpdateParseDocument so AdHock Roslyn Workspace is created for file
			await document.UpdateParseDocument ();
			var info = new Components.Commands.CommandInfo ();
			//Finnaly call command Update so it sets values which we assert
			GetExtension (document.Editor).OnUpdateToggleComment (info);
			Assert.AreEqual (true, info.Visible);
			Assert.AreEqual (true, info.Enabled);
		}

		[Test]
		public void TestToggle_Add()
		{
			var editor = CreateTextEditor (@"class Foo
{
	<-void Bar ()
	{

	}->
}");
			GetExtension (editor).ToggleCodeComment ();
			AssertEditorState (editor, @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}");
		}
	
		[Test]
		public void TestToggle_Remove()
		{
			var editor = CreateTextEditor ( @"class Foo
{
	//<-void Bar ()
	//{

	//}->
}");
			GetExtension (editor).ToggleCodeComment ();
			AssertEditorState (editor,@"class Foo
{
	<-void Bar ()
	{

	}->
}");
		}


		[Test]
		public void TestToggle_Bug()
		{
			var editor = CreateTextEditor (@"<-class Foo
{
	void Bar ()
	{

	}
}
->");
			GetExtension (editor).ToggleCodeComment ();
			AssertEditorState (editor, @"//class Foo
//{
//	void Bar ()
//	{

//	}
//}
");
		}

		[Test]
		public void TestToggleWithoutSelection()
		{
			var editor = CreateTextEditor ( @"class Foo
{
	$void Bar ()
	{
	}
}");
			GetExtension (editor).ToggleCodeComment ();
			AssertEditorState (editor,@"class Foo
{
	//void Bar ()
	{
	}
}");
		}


		/// <summary>
		/// Bug 38355 - comment selected lines puts a comment on too many lines! 
		/// </summary>
		[Test]
		public void TestBug38355()
		{
			var editor = CreateTextEditor (@"class Foo
{
<-	void Bar ()
	{
->		Bar();
	}
}");
			GetExtension (editor).ToggleCodeComment ();
			AssertEditorState (editor, @"class Foo
{
	//void Bar ()
	//{
		Bar();
	}
}");
		}

	}
}

