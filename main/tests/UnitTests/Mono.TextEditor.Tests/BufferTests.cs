// 
// BufferTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class BufferTests
	{
		[Test()]
		public void TestSearchForwardMany ()
		{
			GapBuffer buffer = new GapBuffer ();
			buffer.Text = new string ('a', 100);
			Assert.AreEqual (100, buffer.SearchForward ("a", 0).Count ());
		}
		
		[Test()]
		public void TestSearchBackwardMany ()
		{
			GapBuffer buffer = new GapBuffer ();
			buffer.Text = new string ('a', 100);
			Assert.AreEqual (100, buffer.SearchBackward ("a", buffer.Length).Count ());
		}
		
		[Test()]
		public void TestSearchForward ()
		{
			GapBuffer buffer = new GapBuffer ();
			for (int i = 0; i < 100; i++) {
				buffer.Insert (0, "a");
			}
			var idx = new List<int> (new [] { 0,  buffer.Length / 2, buffer.Length });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx[0], 1, buffer.GetCharAt (idx[0]).ToString ());
			
			List<int> results = new List<int> (buffer.SearchForward ("test", 0));
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
			
			// move gap to the middle
			buffer.Replace (idx[1], 1, buffer.GetCharAt (idx[1]).ToString ());
			
			results = new List<int> (buffer.SearchForward ("test", 0));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (idx[2], 1, buffer.GetCharAt (idx[2]).ToString ());
			
			results = new List<int> (buffer.SearchForward ("test", 0));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (buffer.Length - 1, 1, buffer.GetCharAt (buffer.Length - 1).ToString ());
			
			results = new List<int> (buffer.SearchForward ("test", 0));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
		}
		
		[Test()]
		public void TestSearchForwardIgnoreCase ()
		{
			GapBuffer buffer = new GapBuffer ();
			for (int i = 0; i < 100; i++) {
				buffer.Insert (0, "a");
			}
			var idx = new List<int> (new [] { 0,  buffer.Length / 2, buffer.Length });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx[0], 1, buffer.GetCharAt (idx[0]).ToString ());
			
			List<int> results = new List<int> (buffer.SearchForwardIgnoreCase ("TEST", 0));
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
			
			// move gap to the middle
			buffer.Replace (idx[1], 1, buffer.GetCharAt (idx[1]).ToString ());
			
			results = new List<int> (buffer.SearchForwardIgnoreCase ("TEST", 0));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (idx[2], 1, buffer.GetCharAt (idx[2]).ToString ());
			
			results = new List<int> (buffer.SearchForwardIgnoreCase ("TEST", 0));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (buffer.Length - 1, 1, buffer.GetCharAt (buffer.Length - 1).ToString ());
			
			results = new List<int> (buffer.SearchForwardIgnoreCase ("TEST", 0));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[i], results[i], (i + 1) +". match != " + idx[i] +  " was " + results[i]);
		}
		
		[Test()]
		public void TestSearchBackward ()
		{
			GapBuffer buffer = new GapBuffer ();
			for (int i = 0; i < 100; i++) {
				buffer.Insert (0, "a");
			}
			var idx = new List<int> (new [] { 0,  buffer.Length / 2, buffer.Length });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx[0], 1, buffer.GetCharAt (idx[0]).ToString ());
			
			List<int> results = new List<int> (buffer.SearchBackward ("test", buffer.Length));
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
			
			// move gap to the middle
			buffer.Replace (idx[1], 1, buffer.GetCharAt (idx[1]).ToString ());
			
			results = new List<int> (buffer.SearchBackward ("test", buffer.Length));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (idx[2], 1, buffer.GetCharAt (idx[2]).ToString ());
			
			results = new List<int> (buffer.SearchBackward ("test", buffer.Length));
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (buffer.Length - 1, 1, buffer.GetCharAt (buffer.Length - 1).ToString ());
			
			results = new List<int> (buffer.SearchBackward ("test", buffer.Length));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
		}
		
		[Test()]
		public void TestSearchBackwardIgnoreCase ()
		{
			GapBuffer buffer = new GapBuffer ();
			for (int i = 0; i < 100; i++) {
				buffer.Insert (0, "a");
			}
			var idx = new List<int> (new [] { 0,  buffer.Length / 2, buffer.Length });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx[0], 1, buffer.GetCharAt (idx[0]).ToString ());
			
			List<int> results = new List<int> (buffer.SearchBackwardIgnoreCase ("TEST", buffer.Length));
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
			
			// move gap to the middle
			buffer.Replace (idx[1], 1, buffer.GetCharAt (idx[1]).ToString ());
			
			results = new List<int> (buffer.SearchBackwardIgnoreCase ("TEST", buffer.Length));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (idx[2], 1, buffer.GetCharAt (idx[2]).ToString ());
			
			results = new List<int> (buffer.SearchBackwardIgnoreCase ("TEST", buffer.Length));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
			
			// move gap to the end
			buffer.Replace (buffer.Length - 1, 1, buffer.GetCharAt (buffer.Length - 1).ToString ());
			
			results = new List<int> (buffer.SearchBackwardIgnoreCase ("TEST", buffer.Length));
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
		}
		

		
	}
}

