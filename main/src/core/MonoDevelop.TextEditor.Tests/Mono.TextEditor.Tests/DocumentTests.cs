//
// DocumentTests.cs
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
using System.IO;
using System.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class DocumentTests
	{
		[Test()]
		public void TestDocumentCreation ()
		{
			var document = new Mono.TextEditor.TextDocument ();

			string text = 
			"1234567890\n" +
			"12345678\n" +
			"1234567\n" +
			"123456\n" +
			"12345\n" +
			"1234\n" +
			"123\n" +
			"12\n" +
			"1\n" +
			"\n";
			document.Text = text;
			
			Assert.AreEqual (text, document.Text);
			Assert.AreEqual (11, document.LineCount);
		}
		
		[Test]
		public void TestDocumentInsert ()
		{
			var document = new Mono.TextEditor.TextDocument ();
			
			string top  = "1234567890\n";
			string text =
			"12345678\n" +
			"1234567\n" +
			"123456\n" +
			"12345\n" +
			"1234\n" +
			"123\n" +
			"12\n" +
			"1\n" +
			"\n";
			
			document.Text = top;
			document.InsertText (top.Length, text);
			Assert.AreEqual (top + text, document.Text);
		}
		
		[Test]
		public void TestDocumentRemove ()
		{
			var document = new Mono.TextEditor.TextDocument ();
			
			string top      = "1234567890\n";
			string testText =
			"12345678\n" +
			"1234567\n" +
			"123456\n" +
			"12345\n" +
			"1234\n" +
			"123\n" +
			"12\n" +
			"1\n" +
			"\n";
			document.Text = top + testText;
			document.RemoveText (0, top.Length);
			Assert.AreEqual (document.Text, testText);
			
			document.RemoveText (0, document.Length);
			var line = document.GetLine (1);
			Assert.AreEqual (0, line.Offset);
			Assert.AreEqual (0, line.LengthIncludingDelimiter);
			Assert.AreEqual (0, document.Length);
			Assert.AreEqual (1, document.LineCount);
		}
		
		[Test]
		public void TestDocumentBug1Test()
		{
			var document = new Mono.TextEditor.TextDocument ();
						
			string top    = "1234567890";
			document.Text = top;
			
			Assert.AreEqual (document.GetLine (1).LengthIncludingDelimiter, document.Length);
			
			document.RemoveText(0, document.Length);
			
			var line = document.GetLine (1);
			Assert.AreEqual(0, line.Offset);
			Assert.AreEqual(0, line.LengthIncludingDelimiter);
			Assert.AreEqual(0, document.Length);
			Assert.AreEqual(1, document.LineCount);
		}
		
		[Test]
		public void TestDocumentBug2Test()
		{
			var document = new Mono.TextEditor.TextDocument ();

			string top      = "123\n456\n789\n0";
			string testText = "Hello World!";

			document.Text = top;

			document.InsertText (top.Length, testText);

			DocumentLine line = document.GetLine (document.LineCount);

			Assert.AreEqual (top.Length - 1, line.Offset);
			Assert.AreEqual (testText.Length + 1, line.LengthIncludingDelimiter);
		}
		
		[Test]
		public void SplitterTest ()
		{
			var document = new Mono.TextEditor.TextDocument ();
			for (int i = 0; i < 100; i++) {
				document.InsertText (0, new string ('c', i) + Environment.NewLine);
			}
			Assert.AreEqual (101, document.LineCount);
			for (int i = 0; i < 100; i++) {
				DocumentLine line = document.GetLine (i + 1 );
				Assert.AreEqual (99 - i, line.Length);
				Assert.AreEqual (Environment.NewLine.Length, line.DelimiterLength);
			}
			
			for (int i = 0; i < 100; i++) {
				DocumentLine line = document.GetLine (1);
				document.RemoveText (line.Length, line.DelimiterLength);
			}
			Assert.AreEqual (1, document.LineCount);
		}

		[Test]
		public void TestBufferCreationIssue()
		{
			var document = new Mono.TextEditor.TextDocument ();

			for (int i = 1; i < 1000; i++) {
				var text = new string ('a', i);
				document.Text = text;
				Assert.AreEqual (i, document.Length);
				Assert.AreEqual (text, document.Text);
			}
		}

		/// <summary>
		/// Bug 53380 - [Webtools] Editor inserts BOMs sometimes
		/// </summary>
		[Test]
		public void TestBug53380 ()
		{
			var path = Path.GetTempFileName ();
			File.WriteAllText (path, "Hello World", Encoding.ASCII);
			try {
				var document = new TextDocument (path, "text");

				Assert.AreEqual (0, document.Encoding.GetPreamble ().Length);
			} finally {
				File.Delete (path);
			}
		}

		/// <summary>
		/// VSTS 524616 can't refresh the website successfully after delete the content in '.cshtml' file
		/// </summary>
		[Test]
		public void TestVSTS524616 ()
		{
			var document = new Mono.TextEditor.TextDocument ();
			document.Text = "test";
			string txt;
			document.TextChanging += delegate {
				txt = document.Text;
			};
			document.InsertText (0, "test");
			Assert.AreEqual ("testtest", document.Text);
		}
	}
}
