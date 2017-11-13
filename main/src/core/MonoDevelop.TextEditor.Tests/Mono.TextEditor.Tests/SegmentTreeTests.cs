// 
// SegmentTreeTests.cs
//  
// Author:
//       mkrueger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;
using NUnit.Framework;
using System.Linq;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class SegmentTreeTests
	{
		[Test()]
		public void TestSimpleAdd ()
		{
			var collection = new SegmentTree<TreeSegment> ();
			
			collection.Add (new TreeSegment (10, 10));
			collection.Add (new TreeSegment (12, 2));
			collection.Add (new TreeSegment (14, 2));
			collection.Add (new TreeSegment (11, 5));
			
			Assert.AreEqual (4, collection.Segments.Count ());
		}
		
		[Test()]
		public void TestGetSegmentsAt ()
		{
			var collection = new SegmentTree<TreeSegment> ();
			
			collection.Add (new TreeSegment (10, 10));
			collection.Add (new TreeSegment (12, 2));
			collection.Add (new TreeSegment (14, 2));
			collection.Add (new TreeSegment (11, 5));
			
			Assert.AreEqual (0, collection.GetSegmentsAt (9).Count ());
			Assert.AreEqual (0, collection.GetSegmentsAt (21).Count ());
			Assert.AreEqual (1, collection.GetSegmentsAt (10).Count ());
			Assert.AreEqual (2, collection.GetSegmentsAt (11).Count ());
			Assert.AreEqual (3, collection.GetSegmentsAt (12).Count ());
			Assert.AreEqual (3, collection.GetSegmentsAt (15).Count ());
		}
		
		[Test()]
		public void TestGetSegmentsOverlapping ()
		{
			var collection = new SegmentTree<TreeSegment> ();
			
			collection.Add (new TreeSegment (10, 10));
			collection.Add (new TreeSegment (12, 2));
			collection.Add (new TreeSegment (14, 2));
			collection.Add (new TreeSegment (11, 5));
			
			Assert.AreEqual (4, collection.GetSegmentsOverlapping (12, 4).Count ());
			Assert.AreEqual (2, collection.GetSegmentsOverlapping (10, 1).Count ());
		}
		
		[Test()]
		public void TestUpdateOnTextReplace ()
		{
			var collection = new SegmentTree<TreeSegment> ();
			
			collection.Add (new TreeSegment (0, 89));
			collection.Add (new TreeSegment (92, 51));
			collection.Add (new TreeSegment (42, 77));
			collection.Add (new TreeSegment (36, 128));
			collection.UpdateOnTextReplace (this, new TextChangeEventArgs (0, 0, new string (' ', 355), null));

			Assert.AreEqual (0, collection.Count);
		}
		
		[Test()]
		public void TestSimpleRemove ()
		{
			var collection = new SegmentTree<TreeSegment> ();
			var seg1 = new TreeSegment (10, 10);
			collection.Add (seg1);
			var seg2 = new TreeSegment (12, 2);
			collection.Add (seg2);
			var seg3 = new TreeSegment (14, 2);
			collection.Add (seg3);
			var seg4 = new TreeSegment (11, 5);
			collection.Add (seg4);
			collection.Remove (seg2);
			Assert.AreEqual (3, collection.Segments.Count ());
			collection.Remove (seg4);
			Assert.AreEqual (2, collection.Segments.Count ());
		}

		[Test ()]
		public void TestInsertAtEnd ()
		{
			var collection = new SegmentTree<TreeSegment> ();

			collection.Add (new TreeSegment (10, 0));

			collection.UpdateOnTextReplace (this, new TextChangeEventArgs (10, 10, null, "\n"));
			var seg = collection.Segments.First ();
			Assert.AreEqual (10, seg.Offset);
		}

	}
}
