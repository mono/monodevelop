//
// TextDocumentTestBase.cs
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
using System.Linq;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public abstract class TextDocumentTestBase : ReadonlyTextDocumentTestBase
	{
		protected sealed override IReadonlyTextDocument CreateReadonlyTextDocument (string text, Encoding enc = null)
		{
			return CreateTextDocument (text, enc);
		}

		protected abstract ITextDocument CreateTextDocument (string text, Encoding enc = null);

		[Test]
		public void InsertTextTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.InsertText (3, "Hello");
			Assert.AreEqual (textDoc.Text, "123Hello45");
		}

		[Test]
		public void InsertText_TextSourceTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.InsertText (3, new StringTextSource ("Hello"));
			Assert.AreEqual (textDoc.Text, "123Hello45");
		}

		[Test]
		public void RemoveText_TextSourceTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.RemoveText (2, 2);
			Assert.AreEqual (textDoc.Text, "125");
		}

		[Test]
		public void RemoveText_Segment_TextSourceTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.RemoveText (new TextSegment (2, 2));
			Assert.AreEqual (textDoc.Text, "125");
		}

		[Test]
		public void ReplaceTextTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.ReplaceText (2, 2, "Hello");
			Assert.AreEqual (textDoc.Text, "12Hello5");
		}

		[Test]
		public void Replace_Segment_TextTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.ReplaceText (new TextSegment (2, 2), "Hello");
			Assert.AreEqual (textDoc.Text, "12Hello5");
		}


		[Test]
		public void Replace_TextSourceTextTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.ReplaceText (2, 2, new StringTextSource ("Hello"));
			Assert.AreEqual (textDoc.Text, "12Hello5");
		}

		[Test]
		public void Replace_TextSource_Segment_TextTest()
		{
			var textDoc = CreateTextDocument ("12345");
			textDoc.ReplaceText (new TextSegment (2, 2), new StringTextSource ("Hello"));
			Assert.AreEqual (textDoc.Text, "12Hello5");
		}

		[Test]
		public void TestUndoOperation()
		{
			var textDoc = CreateTextDocument ("12345");
			Assert.IsFalse (textDoc.IsInAtomicUndo);
			using (var undo = textDoc.OpenUndoGroup ()) {
				Assert.IsTrue (textDoc.IsInAtomicUndo);
			}
			Assert.IsFalse (textDoc.IsInAtomicUndo);
		}


		[Test]
		public void TestTextChanging()
		{
			var textDoc = CreateTextDocument ("12345");
			TextChangeEventArgs changeArgs = null;
			textDoc.TextChanging += delegate(object sender, TextChangeEventArgs e) {
				changeArgs = e;
				Assert.AreEqual (textDoc.Text, "12345");
				var ca = changeArgs.TextChanges.First ();
				Assert.AreEqual (ca.Offset, 2);
				Assert.AreEqual (ca.RemovalLength, 2);
				Assert.AreEqual (ca.RemovedText.Text, "34");
				Assert.AreEqual (ca.InsertionLength, "Hello".Length);
				Assert.AreEqual (ca.InsertedText.Text, "Hello");
			};
			textDoc.ReplaceText (2, 2, "Hello");
		}

		[Test]
		public void TestTextChanged()
		{
			var textDoc = CreateTextDocument ("12345");
			TextChangeEventArgs changeArgs = null;
			string text = null;
			textDoc.TextChanged += delegate(object sender, TextChangeEventArgs e) {
				changeArgs = e;
				text = textDoc.Text;
			};
			textDoc.ReplaceText (2, 2, "Hello");
			Assert.AreEqual (textDoc.Text, "12Hello5");
			Assert.AreEqual (text, "12Hello5");
			var ca = changeArgs.TextChanges.First ();
			Assert.AreEqual (ca.Offset, 2);
			Assert.AreEqual (ca.RemovalLength, 2);
			Assert.AreEqual (ca.RemovedText.Text, "34");
			Assert.AreEqual (ca.InsertionLength, "Hello".Length);
			Assert.AreEqual (ca.InsertedText.Text, "Hello");
		}

//		[Test]
//		public void TestLineInserted()
//		{
//			var textDoc = CreateTextDocument ("12345");
//			LineEventArgs changeArgs = null;
//			textDoc.LineInserted += (sender, e) => changeArgs = e;
//			textDoc.InsertText (0, "foo\n");
//			Assert.AreEqual (1, changeArgs.Line.LineNumber);
//			Assert.AreEqual (changeArgs.Line.Length, "foo".Length);
//		}

	}
}
