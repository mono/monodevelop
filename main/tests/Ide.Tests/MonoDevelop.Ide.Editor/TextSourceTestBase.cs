//
// TextSourceTestBase.cs
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
using UnitTests;
using NUnit.Framework.Internal;
using MonoDevelop.Core.Text;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public abstract class TextSourceTestBase : IdeTestBase
	{
		protected abstract ITextSource CreateTextSource (string text, Encoding enc = null);

		[Test]
		public void TestTextProperty()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			Assert.AreEqual (txt, test.Text);
		}

		[Test]
		public void TestLengthProperty()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			Assert.AreEqual (txt.Length, test.Length);
		}

		[Test]
		public void TestGetCharAt()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			for (int i = 0; i < txt.Length; i++) {
				Assert.AreEqual (txt[i], test.GetCharAt (i));
			}
		}

		[Test]
		public void TestTextAt()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			for (int i = 0; i < txt.Length; i++) {
				Assert.AreEqual (txt.Substring (0, i), test.GetTextAt (0, i));
			}
		}

		[Test]
		public void TestTextAt_Segment ()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			for (int i = 0; i < txt.Length; i++) {
				Assert.AreEqual (txt.Substring (0, i), test.GetTextAt (new TextSegment (0, i)));
			}
		}

		[Test]
		public void TestCreateReader()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			var readText = test.CreateReader ().ReadToEnd ();
			Assert.AreEqual (txt, readText);
		}

		[Test]
		public void TestCreateReaderAt()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			var readText = test.CreateReader (2, 2).ReadToEnd ();
			Assert.AreEqual (txt.Substring (2, 2), readText);
		}

		[Test]
		public void TestCreateReaderAt_Segment()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			var readText = test.CreateReader (new TextSegment (2, 2)).ReadToEnd ();
			Assert.AreEqual (txt.Substring (2, 2), readText);
		}

		[Test]
		public void TestWriteTextTo()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			var writer = new StringWriter ();
			test.WriteTextTo (writer);
			Assert.AreEqual (txt, writer.ToString ());
		}

		[Test]
		public void TestWriteTextToAt()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			var writer = new StringWriter ();
			test.WriteTextTo (writer, 2, 2);
			Assert.AreEqual (txt.Substring (2, 2), writer.ToString ());
		}

		[Test]
		public void TestWriteTextToAt_Segment()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			var writer = new StringWriter ();
			test.WriteTextTo (writer, new TextSegment (2, 2));
			Assert.AreEqual (txt.Substring (2, 2), writer.ToString ());
		}

		[Test]
		public void TestCreateSnapshotAt()
		{
			const string txt = "test";
			var test = CreateTextSource (txt).CreateSnapshot (2, 2);
			Assert.AreEqual (txt.Substring (2, 2), test.Text);
		}

		[Test]
		public void TestCreateSnapshotAt_Segment()
		{
			const string txt = "test";
			var test = CreateTextSource (txt).CreateSnapshot (new TextSegment (2, 2));
			Assert.AreEqual (txt.Substring (2, 2), test.Text);
		}

		[Test]
		public void TestGetTextBetween()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			Assert.AreEqual (txt.Substring (1, txt.Length - 2), test.GetTextBetween (1, txt.Length - 1));
		}

		[Test]
		public void TestCopyTo()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			char[] dest_whole = new char[txt.Length];
			test.CopyTo (0, dest_whole, 0, dest_whole.Length);
			Assert.AreEqual (txt, new string(dest_whole));

			char[] dest = new char[2];
			for (int i = 0; i < txt.Length - dest.Length; i++) {
				test.CopyTo (i, dest, 0, dest.Length);
				Assert.AreEqual (txt.Substring (i, dest.Length), new string (dest));
			}
		}


		/// <summary>
		/// Bug 40522 - Can't enter comment on blank last line of file
		/// </summary>
		[Test]
		public void TestBug40522 ()
		{
			const string txt = "test";
			var test = CreateTextSource (txt);
			Assert.AreEqual ("", test.GetTextAt (new TextSegment (txt.Length, 0)));
		}
	}
}

