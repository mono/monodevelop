// 
// SelectionActionTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Editor;
using NUnit.Framework;

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class SelectionActionTests : TextEditorTestBase
	{
		[Test()]
		public void TestMoveLeft ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveLeft (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn +  4, DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 3), data.MainSelection);
		}

		[Test()]
		public void TestMoveRight ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveRight (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 5), data.MainSelection);
		}
		
		[Test()]
		public void TestMoveDown ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveDown (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine + 3, DocumentLocation.MinColumn + 4), data.MainSelection);
		}
		
		[Test()]
		public void TestMoveUp ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveUp (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine + 1, DocumentLocation.MinColumn + 4), data.MainSelection);
		}
		
		[Test()]
		public void TestMoveToDocumentStart ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveToDocumentStart (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine, DocumentLocation.MinColumn), data.MainSelection);
		}
		
		[Test()]
		public void TestMoveToDocumentEnd ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveToDocumentEnd (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine + 4, DocumentLocation.MinColumn + 10), data.MainSelection);
		}
		
		[Test()]
		public void TestMoveLineEnd ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveLineEnd (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 10), data.MainSelection);
		}
		
		[Test()]
		public void TestMoveLineHome ()
		{
			TextEditorData data = CaretMoveActionTests.Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.MoveLineHome (data);
			Assert.AreEqual (new Selection (DocumentLocation.MinLine + 2, DocumentLocation.MinColumn + 4, DocumentLocation.MinLine + 2, DocumentLocation.MinColumn), data.MainSelection);
		}
		
		[Test()]
		public void TestSelectAll ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123456789
123456789
123456789
123456789
123456789
123456789";
			
			Assert.IsFalse (data.IsSomethingSelected);
			SelectionActions.SelectAll (data);
			Assert.IsTrue (data.IsSomethingSelected);
			
			Assert.AreEqual (data.SelectionRange.Offset, 0);
			Assert.AreEqual (data.SelectionRange.EndOffset, data.Document.Length);
		}
		
		
		// Bug 362983 - Text selected with Select All can't be unselected
		// Open a file, press ctrl+a to select all text. Move the cursor or click anywhere
		// in the file. The selection is never erased.
		[Test()]
		public void TestSelectAllBug362983 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			data.Document.Text = "Test";
			Assert.IsFalse (data.IsSomethingSelected);
			SelectionActions.SelectAll (data);
			Assert.IsTrue (data.IsSomethingSelected);
			data.Caret.Offset++;
			Assert.IsFalse (data.IsSomethingSelected);
		}

		[Test()]
		public void TestSelectAllCaretMovement ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
				@"123456789
123456789
123456789
123456789
123456789
123456789";
			
			Assert.IsFalse (data.IsSomethingSelected);
			var loc = new DocumentLocation (3, 3);
			data.Caret.Location = loc;
			SelectionActions.SelectAll (data);
			Assert.IsTrue (data.IsSomethingSelected);
			
			Assert.AreEqual (data.SelectionRange.Offset, 0);
			Assert.AreEqual (data.SelectionRange.EndOffset, data.Document.Length);
			Assert.AreEqual (data.SelectionRange.EndOffset, data.Caret.Offset);
		}

		[Test()]
		public void ExpandSelectionToLine ()
		{
			var data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			SelectionActions.ExpandSelectionToLine (data);
			Check (data, @"1234567890
1234567890
<-1234567890
->$1234567890
1234567890");
		}

		[Test()]
		public void ExpandSelectionToLineWithSelection ()
		{
			var data = Create (@"1234567890
1234567890
<-1234567890
->$1234567890
1234567890");
			SelectionActions.ExpandSelectionToLine (data);
			Check (data, @"1234567890
1234567890
<-1234567890
1234567890
->$1234567890");
		}

		[Test()]
		public void ExpandSelectionToLineWithSelectionCase2 ()
		{
			var data = Create (@"1234567890
1234567890
12345$<-67890
->1234567890
1234567890");
			SelectionActions.ExpandSelectionToLine (data);
			Check (data, @"1234567890
1234567890
<-1234567890
->$1234567890
1234567890");
		}

		[Test]
		public void TestClearSelection ()
		{
			var data = Create (@"1234567890
1234567890
12345$<-67890
->1234567890
1234567890");
			SelectionActions.ClearSelection (data);
			Check (data, @"1234567890
1234567890
12345$67890
1234567890
1234567890");
		}
	}
}
