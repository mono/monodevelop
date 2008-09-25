//
// LineSplitterTests.cs
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
using System.Text;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class LineSplitterTests
	{
		[Test()]
		public void TestLastLineCreation ()
		{
			IBuffer buffer = new Mono.TextEditor.GapBuffer ();
			LineSplitter splitter = new Mono.TextEditor.LineSplitter (buffer);
			buffer.Text = "1\n2\n3\n";
			splitter.TextReplaced (null, new ReplaceEventArgs (0, 0, new StringBuilder (buffer.Text)));
			Assert.AreEqual (4, splitter.LineCount);
			for (int i = 0; i < 3; i++) {
				Assert.AreEqual (i * 2, splitter.Get (i).Offset);
				Assert.AreEqual (1, splitter.Get (i).EditableLength);
				Assert.AreEqual (1, splitter.Get (i).DelimiterLength);
				Assert.AreEqual (2, splitter.Get (i).Length);
			}
			Assert.AreEqual (3 * 2, splitter.Get (3).Offset);
			Assert.AreEqual (0, splitter.Get (3).EditableLength);
			Assert.AreEqual (0, splitter.Get (3).DelimiterLength);
			Assert.AreEqual (0, splitter.Get (3).Length);
		}
		
		[Test()]
		public void TestLastLineRemove ()
		{
			IBuffer buffer = new Mono.TextEditor.GapBuffer ();
			LineSplitter splitter = new Mono.TextEditor.LineSplitter (buffer);
			buffer.Text = "1\n2\n3\n";
			splitter.TextReplaced (null, new ReplaceEventArgs (0, 0, new StringBuilder (buffer.Text)));
			
			LineSegment lastLine = splitter.Get (2);
			splitter.TextReplaced (null, new ReplaceEventArgs (lastLine.Offset, lastLine.Length, new StringBuilder ("")));
			
			Assert.AreEqual (3, splitter.LineCount);
			
			Assert.AreEqual (2 * 2, splitter.Get (2).Offset);
			Assert.AreEqual (0, splitter.Get (2).EditableLength);
			Assert.AreEqual (0, splitter.Get (2).DelimiterLength);
			Assert.AreEqual (0, splitter.Get (2).Length);
		}
	}
}
