// 
// CaretMoveActionsTest.cs
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
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class CaretMoveActionTests
	{
		internal static TextEditorData Create (string text)
		{
			TextEditorData result = new TextEditorData ();
			if (text.IndexOf ('$') >= 0) {
				int caretOffset = text.IndexOf ('$');
				result.Document.Text = text.Remove (caretOffset, 1);
				result.Caret.Offset = caretOffset;
			} else {
				result.Document.Text = text;
			}
			return result;
			
		}
		
		[Test()]
		public void TestCaretLeft ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (2, 3), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLeftCase1 ()
		{
			TextEditorData data = Create (@"1234567890
$1234567890
1234567890
1234567890
1234567890");
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (0, 10), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLeftCase2 ()
		{
			TextEditorData data = Create (@"$1234567890
1234567890
1234567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (0, 0), data.Caret.Location);
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (0, 0), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretRight ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (2, 5), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretRightCase1 ()
		{
			TextEditorData data = Create (@"1234567890$
1234567890
1234567890
1234567890
1234567890");
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (1, 0), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretRightCase2 ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234567890
1234567890
1234567890$");
			Assert.AreEqual (new DocumentLocation (4, 10), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (4, 10), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretUp ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (2, 4), data.Caret.Location);
			CaretMoveActions.Up (data);
			Assert.AreEqual (new DocumentLocation (1, 4), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretUpCase1 ()
		{
			TextEditorData data = Create (@"12$34567890
1234567890
1234567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (0, 2), data.Caret.Location);
			CaretMoveActions.Up (data);
			Assert.AreEqual (new DocumentLocation (0, 0), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretDown ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (2, 4), data.Caret.Location);
			CaretMoveActions.Down (data);
			Assert.AreEqual (new DocumentLocation (3, 4), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretDownCase1 ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234567890
1234567890
123$4567890");
			Assert.AreEqual (new DocumentLocation (4, 3), data.Caret.Location);
			CaretMoveActions.Down (data);
			Assert.AreEqual (new DocumentLocation (4, 10), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLineHome ()
		{
			TextEditorData data = Create (@"  345$67890");
			Assert.AreEqual (new DocumentLocation (0, 5), data.Caret.Location);
			CaretMoveActions.LineHome (data);
			Assert.AreEqual (new DocumentLocation (0, 2), data.Caret.Location);
			CaretMoveActions.LineHome (data);
			Assert.AreEqual (new DocumentLocation (0, 0), data.Caret.Location);
			CaretMoveActions.LineHome (data);
			Assert.AreEqual (new DocumentLocation (0, 2), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLineEnd ()
		{
			TextEditorData data = Create (@"12345$67890");
			Assert.AreEqual (new DocumentLocation (0, 5), data.Caret.Location);
			CaretMoveActions.LineEnd (data);
			Assert.AreEqual (new DocumentLocation (0, 10), data.Caret.Location);
		}
		
		[Test()]
		public void TestToDocumentStart ()
		{
			TextEditorData data = Create (@"12345$67890");
			Assert.AreEqual (new DocumentLocation (0, 5), data.Caret.Location);
			CaretMoveActions.ToDocumentStart (data);
			Assert.AreEqual (new DocumentLocation (0, 0), data.Caret.Location);
		}
		
		[Test()]
		public void TestToDocumentEnd ()
		{
			TextEditorData data = Create (@"12345$67890");
			Assert.AreEqual (new DocumentLocation (0, 5), data.Caret.Location);
			CaretMoveActions.ToDocumentEnd (data);
			Assert.AreEqual (new DocumentLocation (0, 10), data.Caret.Location);
		}
		
		[TestFixtureSetUp] 
		public void SetUp()
		{
			Gtk.Application.Init ();
		}
		
		[TestFixtureTearDown] 
		public void Dispose()
		{
		}
	}
}
