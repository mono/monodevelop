//
// ReadonlyTextDocumentTestBase.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.Text;
using System.Text;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public abstract class ReadonlyTextDocumentTestBase : TextSourceTestBase
	{
		protected sealed override ITextSource CreateTextSource (string text, Encoding enc = null)
		{
			return CreateReadonlyTextDocument (text, enc);
		}

		protected abstract IReadonlyTextDocument CreateReadonlyTextDocument (string text, Encoding enc = null);


		[Test]
		public void TestLineCount()
		{
			var doc = CreateReadonlyTextDocument ("aaa\nbbb\nccc\n");
			Assert.AreEqual (4, doc.LineCount);
		}


		[Test]
		[Ignore ("Broken Bug #54245")]
		// see https://bugzilla.xamarin.com/show_bug.cgi?id=54245
		public void TestLocationToOffset()
		{
			var doc = CreateReadonlyTextDocument ("aaa\nbbb\nccc\n");
			Assert.AreEqual (0, doc.LocationToOffset (1, 1));
			Assert.AreEqual (4, doc.LocationToOffset (2, 1));
			Assert.AreEqual (11, doc.LocationToOffset (3, 4));
		}

		[Test]
		public void TestOffsetToLocation()
		{
			var doc = CreateReadonlyTextDocument ("aaa\nbbb\nccc\n");
			Assert.AreEqual (new DocumentLocation (1, 1), doc.OffsetToLocation (0));
			Assert.AreEqual (new DocumentLocation (2, 1), doc.OffsetToLocation (4));
			Assert.AreEqual (new DocumentLocation (3, 4), doc.OffsetToLocation (11));
		}

		[Test]
		public void TestGetLine()
		{
			var doc = CreateReadonlyTextDocument ("aaa\nbbb\nccc\n");
			var line1 = doc.GetLine (1);
			Assert.AreEqual (0, line1.Offset);
			Assert.AreEqual (3, line1.Length);
			Assert.AreEqual (1, line1.DelimiterLength);
			Assert.AreEqual (1, line1.LineNumber);

			var line2 = doc.GetLine (2);
			Assert.AreEqual (4, line2.Offset);
			Assert.AreEqual (3, line2.Length);
			Assert.AreEqual (1, line2.DelimiterLength);
			Assert.AreEqual (2, line2.LineNumber);

			var line3 = doc.GetLine (3);
			Assert.AreEqual (8, line3.Offset);
			Assert.AreEqual (3, line3.Length);
			Assert.AreEqual (1, line3.DelimiterLength);
			Assert.AreEqual (3, line3.LineNumber);

			var line4 = doc.GetLine (4);
			Assert.AreEqual (12, line4.Offset);
			Assert.AreEqual (0, line4.Length);
			Assert.AreEqual (0, line4.DelimiterLength);
			Assert.AreEqual (4, line4.LineNumber);
		}

		[Test]
		public void GetLineByOffset()
		{
			var doc = CreateReadonlyTextDocument ("aaa\nbbb\nccc\n");
			for (int i = 0; i < 3; i++) {
				var line1 = doc.GetLineByOffset (0 + i);
				Assert.AreEqual (0, line1.Offset);
				Assert.AreEqual (3, line1.Length);
				Assert.AreEqual (1, line1.DelimiterLength);
				Assert.AreEqual (1, line1.LineNumber);
			}

			for (int i = 0; i < 3; i++) {
				var line2 = doc.GetLineByOffset (4 + i);
				Assert.AreEqual (4, line2.Offset);
				Assert.AreEqual (3, line2.Length);
				Assert.AreEqual (1, line2.DelimiterLength);
				Assert.AreEqual (2, line2.LineNumber);
			}

			for (int i = 0; i < 3; i++) {
				var line3 = doc.GetLineByOffset (8 + i);
				Assert.AreEqual (8, line3.Offset);
				Assert.AreEqual (3, line3.Length);
				Assert.AreEqual (1, line3.DelimiterLength);
				Assert.AreEqual (3, line3.LineNumber);
			}

			var line4 = doc.GetLineByOffset (12);
			Assert.AreEqual (12, line4.Offset);
			Assert.AreEqual (0, line4.Length);
			Assert.AreEqual (0, line4.DelimiterLength);
			Assert.AreEqual (4, line4.LineNumber);
		}

		[Test]
		public void TestLineParsingLineEndings()
		{
			var doc = CreateReadonlyTextDocument ("1\n2\r\n3\r4\u00855\u000B6\u000C7\u20288\u2029");
			Assert.AreEqual (UnicodeNewline.LF, doc.GetLine (1).UnicodeNewline);
			Assert.AreEqual (1, doc.GetLine (1).DelimiterLength);

			Assert.AreEqual (UnicodeNewline.CRLF, doc.GetLine (2).UnicodeNewline);
			Assert.AreEqual (2, doc.GetLine (2).DelimiterLength);

			Assert.AreEqual (UnicodeNewline.CR, doc.GetLine (3).UnicodeNewline);
			Assert.AreEqual (1, doc.GetLine (3).DelimiterLength);

			Assert.AreEqual (UnicodeNewline.NEL, doc.GetLine (4).UnicodeNewline);
			Assert.AreEqual (1, doc.GetLine (4).DelimiterLength);

			//Assert.AreEqual (UnicodeNewline.VT, doc.GetLine (5).UnicodeNewline);
			//Assert.AreEqual (1, doc.GetLine (5).DelimiterLength);

			//Assert.AreEqual (UnicodeNewline.FF, doc.GetLine (6).UnicodeNewline);
			//Assert.AreEqual (1, doc.GetLine (6).DelimiterLength);

			Assert.AreEqual (UnicodeNewline.LS, doc.GetLine (5).UnicodeNewline);
			Assert.AreEqual (1, doc.GetLine (5).DelimiterLength);

			Assert.AreEqual (UnicodeNewline.PS, doc.GetLine (6).UnicodeNewline);
			Assert.AreEqual (1, doc.GetLine (6).DelimiterLength);
		}

	}
}