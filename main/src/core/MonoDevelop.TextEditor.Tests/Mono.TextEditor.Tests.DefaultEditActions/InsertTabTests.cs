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
using MonoDevelop.Core.Text;
using NUnit.Framework;

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class InsertTabTests : TextEditorTestBase
	{
		public static TextEditorData Create (string input, bool reverse)
		{
			TextEditorData data = new Mono.TextEditor.TextEditorData ();
			
			int offset1 = input.IndexOf ('[');
			int offset2 = input.IndexOf (']');
			var selection = new TextSegment (offset1, offset2 - offset1 - 1);

			data.Text = input.Substring (0, offset1) + input.Substring (offset1 + 1, (offset2 - offset1) - 1) + input.Substring (offset2 + 1);
			if (reverse) {
				data.Caret.Offset = selection.Offset;
				data.SelectionAnchor = selection.EndOffset;
				data.ExtendSelectionTo (selection.Offset);
			} else {
				data.Caret.Offset = selection.EndOffset;
				data.SelectionAnchor = selection.Offset;
				data.ExtendSelectionTo (selection.EndOffset);
			}
			return data;
		}

		public static void Check (TextEditorData data, string output, bool reverse)
		{
			int offset1 = output.IndexOf ('[');
			int offset2 = output.IndexOf (']');
			string expected = output.Substring (0, offset1) + output.Substring (offset1 + 1, (offset2 - offset1) - 1) + output.Substring (offset2 + 1);
			offset2--;
			Assert.AreEqual (expected, data.Text);
			Assert.AreEqual (data.OffsetToLocation (reverse ? offset2 : offset1), data.MainSelection.Anchor);
			Assert.AreEqual (data.OffsetToLocation (reverse ? offset1 : offset2), data.MainSelection.Lead);
		}
		
		[TestCase(false)]
		[TestCase(true)]
		public void TestInsertTabLine (bool reverse)
		{
			var data = Create (@"123456789
123[456789
123d456789
123]456789
123456789
123456789", reverse);

			MiscActions.InsertTab (data);
			
			Check (data, @"123456789
	123[456789
	123d456789
	123]456789
123456789
123456789", reverse);
		}
		

		[TestCase(false)]
		[TestCase(true)]
		public void TestInsertTabLineCase2 (bool reverse)
		{
			var data = Create (@"123d456789
123[456789
123d456789
]123456789
123456789
123456789", reverse);

			MiscActions.InsertTab (data);
			Check (data, @"123d456789
	123[456789
	123d456789
]123456789
123456789
123456789", reverse);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void TestInsertTabLineCase3 (bool reverse)
		{
			var data = Create (@"123d456789
[123456789
123d456789
123]456789
123456789
123456789", reverse);

			MiscActions.InsertTab (data);

			Check (data, @"123d456789
	[123456789
	123d456789
	123]456789
123456789
123456789", reverse);
		}

		/// <summary>
		/// Bug 5223 - Tab to indent with tab-to-spaces does not adjust selection correctly
		/// </summary>
		[Test]
		public void TestBug5223 ()
		{
			var data = Create (@"    123d456789
    123$<-456789
    123d456789
    ->123456789
    123456789
    123456789", new TextEditorOptions () { TabsToSpaces = true } );

			MiscActions.InsertTab (data);
			Check (data, @"    123d456789
        123$<-456789
        123d456789
        ->123456789
    123456789
    123456789");
		}


		/// <summary>
		/// Bug 5373 - Indenting selected block should not indent blank lines in it
		/// </summary>
		[Test]
		public void TestBug5373 ()
		{
			var data = Create (@"	123d456789
	123$<-456789

	123d456789

	->123456789", new TextEditorOptions () { IndentStyle = IndentStyle.Virtual } );
			data.IndentationTracker = SmartIndentModeTests.IndentTracker;

			MiscActions.InsertTab (data);
			Check (data, @"	123d456789
		123$<-456789

		123d456789

		->123456789");
		}
	}
}
