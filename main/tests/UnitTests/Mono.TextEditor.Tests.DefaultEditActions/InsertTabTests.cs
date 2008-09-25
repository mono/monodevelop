// Test.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class InsertTabTests
	{
		
		public static ISegment GetSelection (TextEditorData data, bool reverse)
		{
			int offset1 = data.Document.Text.IndexOf ('[');
			int offset2 = data.Document.Text.IndexOf (']');
			return new Segment (offset1, offset2 - offset1);
		}

		public static void SetSelection (TextEditorData data, bool reverse)
		{
			ISegment selection = GetSelection (data, reverse);
			
			if (reverse) {
				data.Caret.Offset = selection.Offset;
				data.SelectionAnchor = selection.EndOffset;
				data.ExtendSelectionTo (selection.Offset);
			} else {
				data.Caret.Offset = selection.EndOffset;
				data.SelectionAnchor = selection.Offset;
				data.ExtendSelectionTo (selection.EndOffset);
			}
		}
		
		[Test()]
		public void TestInsertTabLine ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123456789
123[456789
123d456789
123]456789
123456789
123456789";
			SetSelection (data, false);
			
			new InsertTab ().Run (data);
			ISegment currentSelection = GetSelection (data, false);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.EndOffset, data.Caret.Offset);
			
			Assert.IsTrue (data.Document.GetLine(1).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(2).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(3).Length > data.Document.GetLine(0).Length);
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(4).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(3).Length);
		}
		
		[Test()]
		public void TestInsertTabLineReverse ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123456789
123[456789
123d456789
123]456789
123456789
123456789";
			SetSelection (data, true);
			
			new InsertTab ().Run (data);
			ISegment currentSelection = GetSelection (data, true);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.Offset, data.Caret.Offset);
			
			Assert.IsTrue (data.Document.GetLine(1).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(2).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(3).Length > data.Document.GetLine(0).Length);
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(4).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(3).Length);
		}
		
		[Test()]
		public void TestInsertTabLineCase2 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123d456789
123[456789
123d456789
]123456789
123456789
123456789";
			SetSelection (data, false);
			
			new InsertTab ().Run (data);
			ISegment currentSelection = GetSelection (data, false);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.EndOffset, data.Caret.Offset);
			
			Assert.IsTrue (data.Document.GetLine(1).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(2).Length > data.Document.GetLine(0).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(3).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
		}
		
		[Test()]
		public void TestInsertTabLineCase3 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123d456789
123[456789
123d456789
]123456789
123456789
123456789";
			SetSelection (data, false);
			
			new InsertTab ().Run (data);
			ISegment currentSelection = GetSelection (data, false);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.EndOffset, data.Caret.Offset);
			
			Assert.IsTrue (data.Document.GetLine(1).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(2).Length > data.Document.GetLine(0).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(3).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
		}
		
		[Test()]
		public void TestInsertTabLineCase3Reverse ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123d456789
123[456789
123d456789
]123456789
123456789
123456789";
			SetSelection (data, true);
			
			new InsertTab ().Run (data);
			ISegment currentSelection = GetSelection (data, true);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.Offset, data.Caret.Offset);
			
			Assert.IsTrue (data.Document.GetLine(1).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(2).Length > data.Document.GetLine(0).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(3).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
		}
		
		[Test()]
		public void TestInsertTabLineCase4 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"123d456789
[123456789
123d456789
123]456789
123456789
123456789";
			SetSelection (data, false);
			
			new InsertTab ().Run (data);
			ISegment currentSelection = GetSelection (data, false);
			
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.EndOffset, data.Caret.Offset);
			
			Assert.AreEqual (data.Document.GetLine(1).Offset, data.SelectionRange.Offset);
			
			Assert.IsTrue (data.Document.GetLine(1).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(2).Length > data.Document.GetLine(0).Length);
			Assert.IsTrue (data.Document.GetLine(3).Length > data.Document.GetLine(0).Length);
			
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
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
