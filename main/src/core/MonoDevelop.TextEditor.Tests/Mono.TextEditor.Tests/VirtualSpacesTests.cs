// 
// VirtualSpacesTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using NUnit.Framework;
using Gtk;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	class VirtualSpacesTests : TextEditorTestBase
	{

		/// <summary>
		/// Bug 615196 - Pasting a chunk of text into the virtual whitespace fracks everything up
		/// </summary>
		[Test()]
		public void TestBug615196 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.IndentationTracker = null;
			data.Options.IndentStyle = IndentStyle.Auto;
			data.Document.Text = "\n\nHello World\n";
			data.Caret.Offset = 1; // 2nd.Line
			data.Caret.AllowCaretBehindLineEnd = true;
			data.Caret.Column = DocumentLocation.MinColumn + 4;
			Clipboard clipboard = Clipboard.Get (Mono.TextEditor.ClipboardActions.CopyOperation.CLIPBOARD_ATOM);
			clipboard.Text = "Test";
			
			ClipboardActions.Paste (data);
			
			Assert.AreEqual ("\n    Test\nHello World\n", data.Document.Text);
		}
		
		/// <summary>
		/// Bug 613770 - Backspace inconsistently deletes entire lines when using "virtual spaces"
		/// </summary>
		[Test()]
		public void TestBug613770 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.IndentationTracker = null;
			data.Options.IndentStyle = IndentStyle.Auto;
			data.Document.Text = "\n\n\n";
			data.Caret.Offset = 1; // 2nd.Line
			data.Caret.AllowCaretBehindLineEnd = true;
			data.Caret.Column = DocumentLocation.MinColumn + 4;
			DeleteActions.Backspace (data);
			
			Assert.AreEqual ("\n   \n\n", data.Document.Text);
		}
		
		[Test()]
		public void TestReturnKeyBehavior ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.IndentationTracker = null;
			data.Options.IndentStyle = IndentStyle.Auto;
			data.Document.Text = "\n\n\n";
			data.Caret.Offset = 1; // 2nd.Line
			data.Caret.AllowCaretBehindLineEnd = true;
			data.Caret.Column = DocumentLocation.MinColumn + 4;
			MiscActions.InsertNewLine (data);
			
			Assert.AreEqual ("\n    \n    \n\n", data.Document.Text);
		}
		
		/// <summary>
		/// Bug 615624 - Pressing DOWN after RETURN moves cursor to beginning of blank lines instead of to the indent
		/// </summary>
		[Test()]
		public void TestBug615624 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Options.IndentStyle = IndentStyle.Auto;
			data.Document.Text = "\n \n\n";
			data.Caret.AllowCaretBehindLineEnd = true;
			data.Caret.Offset = 2; // 2nd.Line
			Assert.AreEqual (DocumentLocation.MinColumn + 1, data.Caret.Column);
			TextDocument.RemoveTrailingWhitespaces (data, data.Document.GetLine (2));
			Assert.AreEqual ("\n\n\n", data.Document.Text);
			Assert.AreEqual (DocumentLocation.MinColumn + 1, data.Caret.Column);
		}

		[Test()]
		public void TestCaretRightBehavior ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Options.IndentStyle = IndentStyle.Auto;
			data.Document.Text = "\n\n\n";
			data.Caret.AllowCaretBehindLineEnd = true;

			CaretMoveActions.Right (data);
			Assert.AreEqual (2, data.Caret.Column);
			CaretMoveActions.Right (data);
			Assert.AreEqual (3, data.Caret.Column);
			Assert.AreEqual ("\n\n\n", data.Document.Text);
		}

		[Test()]
		public void TestCaretLeftBehavior ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Options.IndentStyle = IndentStyle.Auto;
			data.Document.Text = "\n\n\n";
			data.Caret.AllowCaretBehindLineEnd = true;
			data.Caret.Column = 4;

			CaretMoveActions.Left (data);
			Assert.AreEqual (3, data.Caret.Column);
			CaretMoveActions.Left (data);
			Assert.AreEqual (2, data.Caret.Column);
			Assert.AreEqual ("\n\n\n", data.Document.Text);
		}
		
	}
}

