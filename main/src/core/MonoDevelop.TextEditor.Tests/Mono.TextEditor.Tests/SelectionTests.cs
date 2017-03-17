// SelectionTests.cs
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
using System.Linq;
using MonoDevelop.Core.Text;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class SelectionTests
	{
		static TextEditorData Create(string content)
		{
			return new TextEditorData (new TextDocument (content));
		}

		static ISegment GetSelection (TextEditorData data, bool reverse)
		{
			int offset1 = data.Document.Text.IndexOf ('[');
			int offset2 = data.Document.Text.IndexOf (']');
			return new TextSegment (offset1, offset2 - offset1);
		}

		static void SetSelection (TextEditorData data, bool reverse)
		{
			var selection = GetSelection (data, reverse);
			
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
		public void TestExtendSelectionTo ()
		{
			TextEditorData data = Create (
				@"123456789
123456789
123456789
123456789
123456789
123456789"
			);

			data.SelectionAnchor = 3;
			DocumentLine line = data.Document.GetLine (3);
			
			Assert.IsFalse (data.IsSomethingSelected);
			data.ExtendSelectionTo (line.Offset + 3);
			
			Assert.IsTrue (data.IsSomethingSelected);
			
			Assert.AreEqual (3, data.SelectionRange.Offset);
			Assert.AreEqual (line.Offset + 3, data.SelectionRange.EndOffset);
		}
		
		[Test()]
		public void TestSelectedLines ()
		{
			TextEditorData data = Create (
				@"123456789
123[456789
123456789
1234]56789
123456789
123456789");

			SetSelection (data, false);
			int lines = data.SelectedLines.Count ();
			Assert.AreEqual (3, lines);
		}
		
		[Test()]
		public void TestSelectedLinesReverse ()
		{
			TextEditorData data = Create (
				@"123456789
123[456789
123456789
1234]56789
123456789
123456789");
			SetSelection (data, true);
			Assert.AreEqual (3, data.SelectedLines.Count ());
		}
		
		[Test()]
		public void TestSelectedLinesCase2 ()
		{
			TextEditorData data = Create (
				@"123456789
123[456789
123456789
123456789
]123456789
123456789");
			SetSelection (data, false);
			Assert.AreEqual (3, data.SelectedLines.Count ());
		}
		
		[Test()]
		public void TestSelectedLinesCase2Reverse ()
		{
			TextEditorData data = Create (
				@"123456789
123[456789
123456789
123456789
]123456789
123456789");
			SetSelection (data, true);
			Assert.AreEqual (3, data.SelectedLines.Count ());
		}
		
		[Test()]
		public void TestSelectedLinesCase3 ()
		{
			TextEditorData data = Create (
				@"123456789
[123456789
123456789
123]456789
123456789
123456789");
			SetSelection (data, false);
			Assert.AreEqual (3, data.SelectedLines.Count ());
		}
		
		[Test()]
		public void TestSelectedLinesCase3Reverse ()
		{
			TextEditorData data = Create (
				@"123456789
[123456789
123456789
123]456789
123456789
123456789");
			SetSelection (data, true);
			Assert.AreEqual (3, data.SelectedLines.Count ());
		}
		
		[Test()]
		public void TestSetSelectedLines ()
		{
			TextEditorData data = Create (
				@"1
2
3
");
			data.SetSelectLines (1, 4);
			Assert.AreEqual (0, data.SelectionAnchor);
			Assert.AreEqual (0, data.SelectionRange.Offset);
			Assert.AreEqual (data.Document.Length, data.SelectionRange.EndOffset);
		}
	}
}
