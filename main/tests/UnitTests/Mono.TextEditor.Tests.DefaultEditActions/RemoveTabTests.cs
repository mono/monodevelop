//
// RemoveTabTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class RemoveTabTests
	{
		[Test()]
		public void TestRemoveTab ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"\t123456789
\t123[456789
\t123d456789
\t123]456789
\t123456789
\t123456789";
			InsertTabTests.SetSelection (data, false);
			
			new RemoveTab ().Run (data);
			ISegment currentSelection = InsertTabTests.GetSelection (data, false);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.EndOffset, data.Caret.Offset);
			Assert.AreEqual (currentSelection.Offset, data.SelectionAnchor);
			
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(1).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(2).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(3).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(4).Length);
			
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(3).Length);
		}
		
		[Test()]
		public void TestRemoveTabReverse ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"\t123456789
\t123[456789
\t123d456789
\t123]456789
\t123456789
\t123456789";
			InsertTabTests.SetSelection (data, true);
			
			new RemoveTab ().Run (data);
			ISegment currentSelection = InsertTabTests.GetSelection (data, true);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.Offset, data.Caret.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionAnchor);
			
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(1).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(2).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(3).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(4).Length);
			
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(3).Length);
		}
		
		[Test()]
		public void TestRemoveTabCase2 ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"\t123456789
[\t123456789
\t123d456789
\t123]456789
\t123456789
\t123456789";
			InsertTabTests.SetSelection (data, false);
			
			new RemoveTab ().Run (data);
			ISegment currentSelection = InsertTabTests.GetSelection (data, false);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.EndOffset, data.Caret.Offset);
			Assert.AreEqual (currentSelection.Offset, data.SelectionAnchor);
			Assert.AreEqual (currentSelection.Offset, data.Document.GetLine(1).Offset);
			
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(1).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(2).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(3).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(4).Length);
			
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(3).Length);
		}
		
		[Test()]
		public void TestRemoveTabCase2Reverse ()
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData  ();
			data.Document.Text = 
@"\t123456789
[\t123456789
\t123d456789
\t123]456789
\t123456789
\t123456789";
			InsertTabTests.SetSelection (data, true);
			
			new RemoveTab ().Run (data);
			ISegment currentSelection = InsertTabTests.GetSelection (data, true);
			
			Assert.AreEqual (currentSelection.Offset, data.SelectionRange.Offset);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionRange.EndOffset);
			Assert.AreEqual (currentSelection.Offset, data.Caret.Offset);
			Assert.AreEqual (0, data.Caret.Column);
			Assert.AreEqual (currentSelection.EndOffset, data.SelectionAnchor);
			
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(1).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(2).Length);
			Assert.IsTrue (data.Document.GetLine(0).Length < data.Document.GetLine(3).Length);
			
			Assert.AreEqual (data.Document.GetLine(0).Length, data.Document.GetLine(4).Length);
			
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(2).Length);
			Assert.AreEqual (data.Document.GetLine(1).Length, data.Document.GetLine(3).Length);
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

