// 
// EditActionsTest.cs
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
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class MiscActionsTest : TextEditorTestBase
	{
		/// <summary>
		/// Bug 615191 - When using multiline selection to indent/outdent, the indenter selects too much
		/// </summary>
		[Test()]
		public void TestInsertTabBug615196_IndentCase ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Options = new TextEditorOptions () { IndentStyle = IndentStyle.Smart };
			data.Document.Text = "\n\n\n\n\n";
			data.Caret.Offset = data.Document.GetLine (2).Offset; // 2nd.Line
			data.MainSelection = new Selection (2, 1, 4, 1);
			MiscActions.InsertTab (data);
			MiscActions.InsertTab (data);
			
			Assert.AreEqual ("\n\t\t\n\t\t\n\n\n", data.Document.Text);
		}
		
		[Test()]
		public void TestRemoveTabBug615196_UnIndentCase ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = "\n\t\t\n\t\t\n\t\t\n\n";
			data.Caret.Offset = data.Document.GetLine (2).Offset; // 2nd.Line
			data.MainSelection = new Selection (2, 1, 4, 1);
			MiscActions.RemoveTab (data);
			MiscActions.RemoveTab (data);
			
			Assert.AreEqual ("\n\n\n\t\t\n\n", data.Document.Text);
		}
	
		[Test()]
		public void TestInsertNewLine ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Document.Text = "Hello World!";
			data.Caret.Location = new DocumentLocation (1, "Hello".Length + 1);
			MiscActions.InsertNewLine (data);
			Assert.AreEqual (2, data.Document.LineCount);
			Assert.AreEqual (2, data.Caret.Line);
			Assert.AreEqual (1, data.Caret.Column);
			Assert.AreEqual ("Hello" + Environment.NewLine + " World!", data.Document.Text);
		}

		/// <summary>
		/// Bug 25602 - "Automatic" Indentation adds or removes Tabs!
		/// </summary>
		[Test]
		public void TestBug25602 ()
		{
			var data = Create (@"
using System;

#if DEBUG
namespace TestBadFormating
#endif
{
	// Some test code here bla baaa...$
	class MainClass
	{
");
			MiscActions.InsertNewLine (data);
			Assert.AreEqual (2, data.Caret.Column);
		}



		[Test()]
		public void TestUndo ()
		{
			// Undo/Redo subsystem is tested separately, this only checks if the action is working.
			var data = Create ("foo$");
			data.InsertAtCaret ("bar");
			Check (data, "foobar$");
			MiscActions.Undo (data);
			Check (data, "foo$");
		}

		[Test()]
		public void TestRedo ()
		{
			// Undo/Redo subsystem is tested separately, this only checks if the action is working.
			var data = Create ("foo$");
			data.InsertAtCaret ("bar");
			Check (data, "foobar$");
			MiscActions.Undo (data);
			Check (data, "foo$");
			MiscActions.Redo (data);
			Check (data, "foobar$");
		}
	
		[Test()]
		public void TestInsertNewLineAtEnd ()
		{
			var data = Create ("foo$bar");
			MiscActions.InsertNewLineAtEnd (data);
			Check (data, "foobar" + data.EolMarker + "$");
		}

		[Test()]
		public void TestInsertNewLinePreserveCaretPosition ()
		{
			var data = Create ("foo$bar");
			MiscActions.InsertNewLinePreserveCaretPosition (data);
			Check (data, "foo$" + data.EolMarker + "bar");
		}

		[Test()]
		public void TestSwitchCaretMode ()
		{
			var data = Create ("foo$bar");
			Assert.IsTrue (data.Caret.IsInInsertMode);
			MiscActions.SwitchCaretMode (data);
			Assert.IsFalse (data.Caret.IsInInsertMode);
			MiscActions.SwitchCaretMode (data);
			Assert.IsTrue (data.Caret.IsInInsertMode);
		}

		[Test()]
		public void TestMoveBlockDown ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
cccccccccc
dddddd$dddd
eeeeeeeeee
ffffffffff");
			MiscActions.MoveBlockDown (data);
			Check (data, @"aaaaaaaaa
bbbbbbbbbb
cccccccccc
eeeeeeeeee
dddddd$dddd
ffffffffff");
		}

		[Test()]
		public void TestMoveBlockDownSelection ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
ccc<-ccccccc
dddddd->$dddd
eeeeeeeeee
ffffffffff");
			MiscActions.MoveBlockDown (data);
			Check (data, @"aaaaaaaaa
bbbbbbbbbb
eeeeeeeeee
ccc<-ccccccc
dddddd->$dddd
ffffffffff");
		}

		[Test()]
		public void TestMoveBlockUp ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
cccccccccc
dddddd$dddd
eeeeeeeeee
ffffffffff");
			MiscActions.MoveBlockUp (data);
			Check (data, @"aaaaaaaaa
bbbbbbbbbb
dddddd$dddd
cccccccccc
eeeeeeeeee
ffffffffff");
		}
		
		[Test()]
		public void TestMoveBlockUpSelection ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
ccc<-ccccccc
dddddd->$dddd
eeeeeeeeee
ffffffffff");
			MiscActions.MoveBlockUp (data);
			Check (data, @"aaaaaaaaa
ccc<-ccccccc
dddddd->$dddd
bbbbbbbbbb
eeeeeeeeee
ffffffffff");
		}

		[Test()]
		public void TestDuplicateLines ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
cccccccccc
dddddd$dddd
eeeeeeeeee
ffffffffff");
			MiscActions.DuplicateLine (data);
			Check (data, @"aaaaaaaaa
bbbbbbbbbb
cccccccccc
dddddddddd
dddddd$dddd
eeeeeeeeee
ffffffffff");
		}

		/// <summary>
		/// Bug 29193 - Last line of code is duplicated on the same line 
		/// </summary>
		[Test()]
		public void TestDuplicateLines_Bug29193 ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
cccccccccc
dddddddddd
eeeeeeeeee
ffffffffff$");
			MiscActions.DuplicateLine (data);
			Check (data, @"aaaaaaaaa
bbbbbbbbbb
cccccccccc
dddddddddd
eeeeeeeeee
ffffffffff
ffffffffff$");
		}

		[Test()]
		public void TestDuplicateSelectedText ()
		{
			var data = Create (@"aaaaaaaaa
bbbbbbbbbb
cccccccc<-cc
dddddd->$dddd
eeeeeeeeee
ffffffffff");
			MiscActions.DuplicateLine (data);
			Check (data, @"aaaaaaaaa
bbbbbbbbbb
cccccccccc
ddddddcc
dddddd$dddd
eeeeeeeeee
ffffffffff");
		}
	}
}

