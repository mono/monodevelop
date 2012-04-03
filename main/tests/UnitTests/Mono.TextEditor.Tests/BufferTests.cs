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
			int cnt = 0;
			int o = 0;
			while ((o = buffer.IndexOf ("a", o, buffer.TextLength - o, StringComparison.Ordinal)) >= 0) {
				cnt++;
				o++;
			}
			Assert.AreEqual (100, cnt);
		}
		
		[Test()]
		public void TestSearchBackwardMany ()
		{
			GapBuffer buffer = new GapBuffer ();
			buffer.Text = new string ('a', 100);
			int cnt = 0;
			int o = buffer.TextLength;
			while (o > 0 && (o = buffer.LastIndexOf ("a", o - 1, o, StringComparison.Ordinal)) != -1) {
				cnt++;
			}
			Assert.AreEqual (100, cnt);
		}
		
		[Test()]
		public void TestSearchForward ()
		{
			GapBuffer buffer = new GapBuffer ();
			for (int i = 0; i < 100; i++) {
				buffer.Insert (0, "a");
			}
			var idx = new List<int> (new [] { 0,  buffer.TextLength / 2, buffer.TextLength });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx [0], 1, buffer.GetCharAt (idx [0]).ToString ());
			
			List<int> results = new List<int> ();
			
			int o = 0;
			while ((o = buffer.IndexOf ("test", o, buffer.TextLength - o, StringComparison.Ordinal)) >= 0) {
				results.Add (o);
				o++;
			}
			

			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [i], results [i], (i + 1) + ". match != " + idx [i] + " was " + results [i]);
			
			// move gap to the middle
			buffer.Replace (idx [1], 1, buffer.GetCharAt (idx [1]).ToString ());
			
			results = new List<int> ();
			o = 0;
			while ((o = buffer.IndexOf ("test", o, buffer.TextLength - o, StringComparison.Ordinal)) >= 0) {
				results.Add (o);
				o++;
			}
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [i], results [i], (i + 1) + ". match != " + idx [i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (idx [2], 1, buffer.GetCharAt (idx [2]).ToString ());
			
			results = new List<int> ();
			o = 0;
			while ((o = buffer.IndexOf ("test", o, buffer.TextLength - o, StringComparison.Ordinal)) >= 0) {
				results.Add (o);
				o++;
			}
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [i], results [i], (i + 1) + ". match != " + idx [i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (buffer.TextLength - 1, 1, buffer.GetCharAt (buffer.TextLength - 1).ToString ());
			
			results = new List<int> ();
			o = 0;
			while ((o = buffer.IndexOf ("test", o, buffer.TextLength - o, StringComparison.Ordinal)) >= 0) {
				results.Add (o);
				o++;
			}
			
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
			var idx = new List<int> (new [] { 0,  buffer.TextLength / 2, buffer.TextLength });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx [0], 1, buffer.GetCharAt (idx [0]).ToString ());
			
			List<int> results = new List<int> ();
			int o = 0;
			while ((o = buffer.IndexOf ("TEST", o, buffer.TextLength - o, StringComparison.OrdinalIgnoreCase)) >= 0) {
				results.Add (o);
				o++;
			}
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [i], results [i], (i + 1) + ". match != " + idx [i] + " was " + results [i]);
			
			// move gap to the middle
			buffer.Replace (idx [1], 1, buffer.GetCharAt (idx [1]).ToString ());
			
			results = new List<int> ();
			o = 0;
			while ((o = buffer.IndexOf ("TEST", o, buffer.TextLength - o, StringComparison.OrdinalIgnoreCase)) >= 0) {
				results.Add (o);
				o++;
			}

			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [i], results [i], (i + 1) + ". match != " + idx [i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (idx [2], 1, buffer.GetCharAt (idx [2]).ToString ());
			
			results = new List<int> ();
			o = 0;
			while ((o = buffer.IndexOf ("TEST", o, buffer.TextLength - o, StringComparison.OrdinalIgnoreCase)) >= 0) {
				results.Add (o);
				o++;
			}
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [i], results [i], (i + 1) + ". match != " + idx [i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (buffer.TextLength - 1, 1, buffer.GetCharAt (buffer.TextLength - 1).ToString ());
			
			results = new List<int> ();
			o = 0;
			while ((o = buffer.IndexOf ("TEST", o, buffer.TextLength - o, StringComparison.OrdinalIgnoreCase)) >= 0) {
				results.Add (o);
				o++;
			}

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
			var idx = new List<int> (new [] { 0,  buffer.TextLength / 2, buffer.TextLength });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx [0], 1, buffer.GetCharAt (idx [0]).ToString ());
			
			List<int> results = new List<int> ();
			int o = buffer.TextLength;
			while (o > 0 && (o = buffer.LastIndexOf ("test", o - 1, o, StringComparison.Ordinal)) != -1) {
				results.Add (o);
			}
			

			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [idx.Count - 1 - i], results [i], (i + 1) + ". match != " + idx [idx.Count - 1 - i] + " was " + results [i]);
			
			// move gap to the middle
			buffer.Replace (idx [1], 1, buffer.GetCharAt (idx [1]).ToString ());
			
			results = new List<int> ();
			o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("test", o - 1, o, StringComparison.Ordinal)) != -1) {
				results.Add (o);
			}
			
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [idx.Count - 1 - i], results [i], (i + 1) + ". match != " + idx [idx.Count - 1 - i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (idx [2], 1, buffer.GetCharAt (idx [2]).ToString ());
			
			results = new List<int> ();
			o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("test", o - 1, o, StringComparison.Ordinal)) != -1) {
				results.Add (o);
			}

			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [idx.Count - 1 - i], results [i], (i + 1) + ". match != " + idx [idx.Count - 1 - i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (buffer.TextLength - 1, 1, buffer.GetCharAt (buffer.TextLength - 1).ToString ());
			
			results = new List<int> ();
			o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("test", o - 1, o, StringComparison.Ordinal)) != -1) {
				results.Add (o);
			}
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
			var idx = new List<int> (new [] { 0,  buffer.TextLength / 2, buffer.TextLength });
			
			idx.ForEach (i => buffer.Insert (i, "test"));
			
			// move gap to the beginning
			buffer.Replace (idx [0], 1, buffer.GetCharAt (idx [0]).ToString ());
			
			List<int> results = new List<int> ();
			int o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("TEST", o - 1, o, StringComparison.OrdinalIgnoreCase)) != -1) {
				results.Add (o);
				o--;
			}
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [idx.Count - 1 - i], results [i], (i + 1) + ". match != " + idx [idx.Count - 1 - i] + " was " + results [i]);
			
			// move gap to the middle
			buffer.Replace (idx [1], 1, buffer.GetCharAt (idx [1]).ToString ());
			
			results = new List<int> ();
			o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("TEST", o - 1, o, StringComparison.OrdinalIgnoreCase)) != -1) {
				results.Add (o);
				o--;
			}
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [idx.Count - 1 - i], results [i], (i + 1) + ". match != " + idx [idx.Count - 1 - i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (idx [2], 1, buffer.GetCharAt (idx [2]).ToString ());
			
			results = new List<int> ();
			o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("TEST", o - 1, o, StringComparison.OrdinalIgnoreCase)) != -1) {
				results.Add (o);
				o--;
			}
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx [idx.Count - 1 - i], results [i], (i + 1) + ". match != " + idx [idx.Count - 1 - i] + " was " + results [i]);
			
			// move gap to the end
			buffer.Replace (buffer.TextLength - 1, 1, buffer.GetCharAt (buffer.TextLength - 1).ToString ());
			
			results = new List<int> ();
			o = buffer.TextLength - 1;
			while (o > 0 && (o = buffer.LastIndexOf ("TEST", o - 1, o, StringComparison.OrdinalIgnoreCase)) != -1) {
				results.Add (o);
				o--;
			}
			Assert.AreEqual (idx.Count, results.Count, "matches != " + idx.Count + " - found:" + results.Count);
			for (int i = 0; i < idx.Count; i++)
				Assert.AreEqual (idx[idx.Count -  1 - i], results[i], (i + 1) +". match != " + idx[idx.Count -  1 - i] +  " was " + results[i]);
		}
	}
}

