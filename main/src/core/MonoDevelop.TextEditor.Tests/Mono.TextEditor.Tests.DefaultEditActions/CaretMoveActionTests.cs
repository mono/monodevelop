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
using MonoDevelop.Ide.Editor;
using NUnit.Framework;

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class CaretMoveActionTests : TextEditorTestBase
	{
		[Test()]
		public void TestCaretLeft ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (3, 4), data.Caret.Location);
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
			Assert.AreEqual (new DocumentLocation (1, 11), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLeftCase2 ()
		{
			TextEditorData data = Create (@"$1234567890
1234567890
1234567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (1, 1), data.Caret.Location);
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (1, 1), data.Caret.Location);
		}

		[Test()]
		public void TestCaretLeftAfterFolding ()
		{
			var data = Create (
@"
	+[Fold]$
");
			CaretMoveActions.Left (data);
			Check (data, 
@"
	$+[Fold]
");
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
			Assert.AreEqual (new DocumentLocation (3, 6), data.Caret.Location);
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
			Assert.AreEqual (new DocumentLocation (2, 1), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretRightCase2 ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234567890
1234567890
1234567890$");
			Assert.AreEqual (new DocumentLocation (5, 11), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (5, 11), data.Caret.Location);
		}

		[Test()]
		public void TestCaretRightBeforeFolding ()
		{
			var data = Create (
@"
	$+[Fold]
");
			CaretMoveActions.Right (data);
			Check (data, 
@"
	+[Fold]$
");
		}
		
		[Test()]
		public void TestCaretUp ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (3, 5), data.Caret.Location);
			CaretMoveActions.Up (data);
			Assert.AreEqual (new DocumentLocation (2, 5), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretUpCase1 ()
		{
			TextEditorData data = Create (@"12$34567890
1234567890
1234567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			CaretMoveActions.Up (data);
			Assert.AreEqual (new DocumentLocation (1, 1), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretDown ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234$567890
1234567890
1234567890");
			Assert.AreEqual (new DocumentLocation (3, 5), data.Caret.Location);
			CaretMoveActions.Down (data);
			Assert.AreEqual (new DocumentLocation (4, 5), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretDownCase1 ()
		{
			TextEditorData data = Create (@"1234567890
1234567890
1234567890
1234567890
123$4567890");
			Assert.AreEqual (new DocumentLocation (5, 4), data.Caret.Location);
			CaretMoveActions.Down (data);
			Assert.AreEqual (new DocumentLocation (5, 11), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLineHome ()
		{
			TextEditorData data = Create (@"  345$67890");
			Assert.AreEqual (new DocumentLocation (1, 6), data.Caret.Location);
			CaretMoveActions.LineHome (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
			CaretMoveActions.LineHome (data);
			Assert.AreEqual (new DocumentLocation (1, 1), data.Caret.Location);
			CaretMoveActions.LineHome (data);
			Assert.AreEqual (new DocumentLocation (1, 3), data.Caret.Location);
		}

		[Test()]
		public void TestCaretLineHomeWithFolding ()
		{
			var data = Create (@"
	Hello
	+[Hello
	Hello
	Hello]$
	Hello
");
			CaretMoveActions.LineHome (data);
			Check (data, @"
	Hello
$	+[Hello
	Hello
	Hello]
	Hello
");
		}

		[Test()]
		public void TestCaretLineEndWithFolding ()
		{
			var data = Create (@"
	Hello
$	+[Hello
	Hello
	Hello]
	Hello
");
			CaretMoveActions.LineEnd (data);
			Check (data, @"
	Hello
	+[Hello
	Hello
	Hello]$
	Hello
");
		}
		

		[Test()]
		public void TestCaretLineEnd ()
		{
			TextEditorData data = Create (@"12345$67890");
			Assert.AreEqual (new DocumentLocation (1, 6), data.Caret.Location);
			CaretMoveActions.LineEnd (data);
			Assert.AreEqual (new DocumentLocation (1, 11), data.Caret.Location);
		}
		
		[Test()]
		public void TestToDocumentStart ()
		{
			TextEditorData data = Create (@"12345$67890");
			Assert.AreEqual (new DocumentLocation (1, 6), data.Caret.Location);
			CaretMoveActions.ToDocumentStart (data);
			Assert.AreEqual (new DocumentLocation (1, 1), data.Caret.Location);
		}
		
		[Test()]
		public void TestToDocumentEnd ()
		{
			TextEditorData data = Create (@"12345$67890");
			Assert.AreEqual (new DocumentLocation (1, 6), data.Caret.Location);
			CaretMoveActions.ToDocumentEnd (data);
			Assert.AreEqual (new DocumentLocation (1, 11), data.Caret.Location);
		}

		[Test()]
		public void TestPreviousWord ()
		{
			var data = Create (@"word1 word2 word3$");
			CaretMoveActions.PreviousWord (data);
			Check (data, @"word1 word2 $word3");
			CaretMoveActions.PreviousWord (data);
			Check (data, @"word1 $word2 word3");
			CaretMoveActions.PreviousWord (data);
			Check (data, @"$word1 word2 word3");
		}

		[Test()]
		public void TestNextWord ()
		{
			var data = Create (@"$word1 word2 word3");
			CaretMoveActions.NextWord (data);
			Check (data, @"word1$ word2 word3");
			CaretMoveActions.NextWord (data);
			Check (data, @"word1 word2$ word3");
			CaretMoveActions.NextWord (data);
			Check (data, @"word1 word2 word3$");
		}

		
		[Test()]
		public void TestPreviousSubword ()
		{
			var data = Create (@"someLongWord$");
			CaretMoveActions.PreviousSubword (data);
			Check (data, @"someLong$Word");
			CaretMoveActions.PreviousSubword (data);
			Check (data, @"some$LongWord");
			CaretMoveActions.PreviousSubword (data);
			Check (data, @"$someLongWord");
		}

		[Test()]
		public void TestNextSubword ()
		{
			var data = Create (@"$someLongWord");
			CaretMoveActions.NextSubword (data);
			Check (data, @"some$LongWord");
			CaretMoveActions.NextSubword (data);
			Check (data, @"someLong$Word");
			CaretMoveActions.NextSubword (data);
			Check (data, @"someLongWord$");
		}

		[Test()]
		public void TestLineStart ()
		{
			var data = Create (@"      someLongWord$");
			CaretMoveActions.LineStart (data);
			Check (data, @"$      someLongWord");
			CaretMoveActions.LineStart (data);
			Check (data, @"$      someLongWord");
		}

		[Test()]
		public void TestLineFirstNonWhitespace ()
		{
			var data = Create (@"      someLongWord$");
			CaretMoveActions.LineFirstNonWhitespace (data);
			Check (data, @"      $someLongWord");
			CaretMoveActions.LineFirstNonWhitespace (data);
			Check (data, @"      $someLongWord");
		}

		[Test()]
		public void TestUpLineStart ()
		{
			var data = Create (@"line1
someLongWord$");
			CaretMoveActions.UpLineStart (data);
			Check (data, @"$line1
someLongWord");
		}
		
		[Test()]
		public void TestDownLineEnd ()
		{
			var data = Create (@"$line1
someLongWord");
			CaretMoveActions.DownLineEnd (data);
			Check (data, @"line1
someLongWord$");
		}

		[Test()]
		public void TestPageDown ()
		{
			var data = Create (@"$1
2
3
4
5
6");
			data.VAdjustment = new Gtk.Adjustment (
				0, 
				0, 
				data.LineCount * data.LineHeight,
				data.LineHeight,
				2 * data.LineHeight,
				2 * data.LineHeight);
			CaretMoveActions.PageDown (data);
			Check (data, @"1
2
$3
4
5
6");
		}

		[Test()]
		public void TestPageUp ()
		{
			var data = Create (@"1
2
3
$4
5
6");
			data.VAdjustment = new Gtk.Adjustment (
				0, 
				0, 
				data.LineCount * data.LineHeight,
				data.LineHeight,
				2 * data.LineHeight,
				2 * data.LineHeight);
			CaretMoveActions.PageUp (data);
			Check (data, @"1
$2
3
4
5
6");
		}

		/// <summary>
		/// Bug 4683 - Text editor navigation issue 
		/// </summary>
		[Test()]
		public void TestBug4683 ()
		{
			var data = Create (@"
IEnumerable<string> GetFileExtensions (string filename)
{
	int lastSeparator = filename.Length;
		do {$
			filename.LastIndexOf ('.', 0, lastSeparator);

	}
"
			);
			CaretMoveActions.Down (data);
			CaretMoveActions.Up (data);
			Check (data, @"
IEnumerable<string> GetFileExtensions (string filename)
{
	int lastSeparator = filename.Length;
		do {$
			filename.LastIndexOf ('.', 0, lastSeparator);

	}
");
		}

		[Test()]
		public void TestDesiredColumnBeyondEOL ()
		{
			var data = Create (@"
IEnumerable<string> GetFileExtensions (string filename)
{
	int lastSeparator = filename.Length;
		do {
			fi$lename.LastIndexOf ('.', 0, lastSeparator);

	}
"
			);
			CaretMoveActions.Up (data);
			Check (data, @"
IEnumerable<string> GetFileExtensions (string filename)
{
	int lastSeparator = filename.Length;
		do {$
			filename.LastIndexOf ('.', 0, lastSeparator);

	}
");
		}


	}
}
