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
using Mono.TextEditor.Utils;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class LineSplitterTests
	{
		[Test()]
		public void TestLastLineCreation ()
		{
			var buffer = ImmutableText.Empty;
			LineSplitter splitter = new Mono.TextEditor.LineSplitter ();
			buffer = buffer.InsertText (0, "1\n2\n3\n");
			splitter.TextReplaced (null, new TextChangeEventArgs (0, 0, "", buffer.ToString ()));
			Assert.AreEqual (4, splitter.Count);
			for (int i = 0; i < 3; i++) {
				Assert.AreEqual (i * 2, splitter.Get (i + 1).Offset);
				Assert.AreEqual (1, splitter.Get (i + 1).Length);
				Assert.AreEqual (1, splitter.Get (i + 1).DelimiterLength);
				Assert.AreEqual (2, splitter.Get (i + 1).LengthIncludingDelimiter);
			}
			Assert.AreEqual (3 * 2, splitter.Get (4).Offset);
			Assert.AreEqual (0, splitter.Get (4).Length);
			Assert.AreEqual (0, splitter.Get (4).DelimiterLength);
			Assert.AreEqual (0, splitter.Get (4).LengthIncludingDelimiter);
		}
		
		[Test()]
		public void TestLastLineRemove ()
		{
			var buffer = ImmutableText.Empty;
			LineSplitter splitter = new Mono.TextEditor.LineSplitter ();
			buffer = buffer.InsertText (0, "1\n2\n3\n");
			splitter.TextReplaced (null, new TextChangeEventArgs (0, 0, "", buffer.ToString ()));
			
			DocumentLine lastLine = splitter.Get (2);
			splitter.TextReplaced (null, new TextChangeEventArgs (lastLine.Offset, lastLine.Offset, buffer.ToString (lastLine.Offset, lastLine.LengthIncludingDelimiter), ""));
			
			Assert.AreEqual (3, splitter.Count);
			
			Assert.AreEqual (2 * 2, splitter.Get (3).Offset);
			Assert.AreEqual (0, splitter.Get (3).Length);
			Assert.AreEqual (0, splitter.Get (3).DelimiterLength);
			Assert.AreEqual (0, splitter.Get (3).LengthIncludingDelimiter);
		}
	}
}
