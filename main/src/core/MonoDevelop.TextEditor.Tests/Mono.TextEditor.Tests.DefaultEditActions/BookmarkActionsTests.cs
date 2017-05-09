// 
// BookmarkTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;

namespace Mono.TextEditor.Tests.Actions
{
	[TestFixture()]
	class BookmarkActionsTests : TextEditorTestBase
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
			for (int i = 1; i <= result.Document.LineCount; i++) {
				if (result.Document.GetLineText (i).StartsWith ("@"))
					result.Document.SetIsBookmarked (result.GetLine (i), true);
			}
			return result;
			
		}
		
		[Test()]
		public void TestPrevBookmark ()
		{
			TextEditorData data = Create (@"1
@2
3
@4
5
@6
7
$8
9");
			BookmarkActions.GotoPrevious (data);
			Assert.AreEqual (6, data.Caret.Line);
			BookmarkActions.GotoPrevious (data);
			Assert.AreEqual (4, data.Caret.Line);
			BookmarkActions.GotoPrevious (data);
			Assert.AreEqual (2, data.Caret.Line);
			BookmarkActions.GotoPrevious (data);
			Assert.AreEqual (6, data.Caret.Line);
		}
		
		[Test()]
		public void TestNextBookmark ()
		{
			TextEditorData data = Create (@"1
@2
3
@4
5
@6
7
$8
9");
			BookmarkActions.GotoNext (data);
			Assert.AreEqual (2, data.Caret.Line);
			BookmarkActions.GotoNext (data);
			Assert.AreEqual (4, data.Caret.Line);
			BookmarkActions.GotoNext (data);
			Assert.AreEqual (6, data.Caret.Line);
			BookmarkActions.GotoNext (data);
			Assert.AreEqual (2, data.Caret.Line);
		}

		[Test()]
		public void TestClearAll ()
		{
			TextEditorData data = Create (@"1
@2
3
@4
5
@6
7
$8
9");
			Assert.AreEqual (3, data.Lines.Count (l => data.Document.IsBookmarked (l)));
			BookmarkActions.ClearAll (data);
			Assert.AreEqual (0, data.Lines.Count (l => data.Document.IsBookmarked (l)));
		}
	}
}
