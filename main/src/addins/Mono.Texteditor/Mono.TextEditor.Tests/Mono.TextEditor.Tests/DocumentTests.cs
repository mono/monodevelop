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

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class DocumentTests
	{
		[Test()]
		public void TestDocumentCreation ()
		{
			Document document = new Mono.TextEditor.Document ();
			
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
			Document document = new Mono.TextEditor.Document ();
			
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
			document.Insert (top.Length, text);
			Assert.AreEqual (top + text, document.Text);
		}
		
		[Test]
		public void TestDocumentRemove ()
		{
			Document document = new Mono.TextEditor.Document ();
			
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
			document.Remove (0, top.Length);
			Assert.AreEqual (document.Text, testText);
			
			document.Remove (0, document.Length);
			LineSegment line = document.GetLine (0);
			Assert.AreEqual (0, line.Offset);
			Assert.AreEqual (0, line.Length);
			Assert.AreEqual (0, document.Length);
			Assert.AreEqual (1, document.LineCount);
		}
		
		[Test]
		public void TestDocumentBug1Test()
		{
			Document document = new Mono.TextEditor.Document ();
						
			string top    = "1234567890";
			document.Text = top;
			
			Assert.AreEqual (document.GetLine (0).Length, document.Length);
			
			document.Remove(0, document.Length);
			
			LineSegment line = document.GetLine (0);
			Assert.AreEqual(0, line.Offset);
			Assert.AreEqual(0, line.Length);
			Assert.AreEqual(0, document.Length);
			Assert.AreEqual(1, document.LineCount);
		}
		
		[Test]
		public void TestDocumentBug2Test()
		{
			Document document = new Mono.TextEditor.Document ();
			
			string top      = "123\n456\n789\n0";
			string testText = "Hello World!";
			
			document.Text = top;
			
			document.Insert (top.Length, testText);
			
			LineSegment line = document.GetLine (document.LineCount - 1);
			
			Assert.AreEqual (top.Length - 1, line.Offset);
			Assert.AreEqual (testText.Length + 1, line.Length);
		}
		
		[Test]
		public void SplitterTest ()
		{
			Document document = new Mono.TextEditor.Document ();
			for (int i = 0; i < 100; i++) {
				document.Insert (0, new string ('c', i) + Environment.NewLine);
			}
			Assert.AreEqual (101, document.LineCount);
			for (int i = 0; i < 100; i++) {
				LineSegment line = document.GetLine (i);
				Assert.AreEqual (99 - i, line.EditableLength);
				Assert.AreEqual (Environment.NewLine.Length, line.DelimiterLength);
			}
			
			for (int i = 0; i < 100; i++) {
				LineSegment line = document.GetLine (0);
				document.Remove (line.EditableLength, line.DelimiterLength);
			}
			Assert.AreEqual (1, document.LineCount);
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
