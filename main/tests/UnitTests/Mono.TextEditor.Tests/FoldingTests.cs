//
// FoldingTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C)  2009  Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class FoldingTests
	{

		[TestFixtureSetUp] 
		public void SetUp()
		{
			Gtk.Application.Init ();
		}
		
		[TestFixtureTearDown] 
		public void Dispose()
		{
		}		
		static List<FoldSegment> GetFoldSegments (Document doc)
		{
			List<FoldSegment> result = new List<FoldSegment> ();
			Stack<FoldSegment> foldSegments = new Stack<FoldSegment> ();
			
			for (int i = 0; i < doc.Length - 1; ++i) {
				char ch = doc.GetCharAt (i);
				
				if ((ch == '+' || ch == '-') && doc.GetCharAt(i + 1) == '[') {
					FoldSegment segment = new FoldSegment (doc, "...", i, 0, FoldingType.None);
					segment.IsFolded = ch == '+';
					foldSegments.Push (segment);
				} else if (ch == ']' && foldSegments.Count > 0) {
					FoldSegment segment = foldSegments.Pop ();
					segment.Length = i - segment.Offset;
					result.Add (segment);
				}
			}
			return result;
		}
		
		[Test()]
		public void TestVisualToLogicalLine ()
		{
			Document document = new Mono.TextEditor.Document ();
			document.Text = 
@"-[0
+[1
2
]3
4
+[5
+[6
+[7
8
]9
]10
]11
]12
13
+[14
15
]16
17";
			document.UpdateFoldSegments (GetFoldSegments (document), false);
			Assert.AreEqual (4, document.LogicalToVisualLine (12));
			Assert.AreEqual (6, document.LogicalToVisualLine (16));
			Assert.AreEqual (7, document.LogicalToVisualLine (17));
		}

		[Test()]
		public void TestUpdateFoldSegmentBug ()
		{
			Document document = new Mono.TextEditor.Document ();
			document.Text = 
@"-[0
1
+[2
3
4
5
6
7
8
9
10]
11
]12
13
+[14
15
16
17
18
19
20
21
22]
23
24
25
26";
			var segments = GetFoldSegments (document);
			document.UpdateFoldSegments (segments, false);
			Assert.AreEqual (25, document.VisualToLogicalLine (9));
			segments.RemoveAt (1);
			
			document.UpdateFoldSegments (segments, false);
			
			Assert.AreEqual (17, document.LogicalToVisualLine (25));
			segments.RemoveAt (1);
			document.UpdateFoldSegments (segments, false);
			Assert.AreEqual (25, document.LogicalToVisualLine (25));
		}
		
		/// <summary>
		/// Bug 682466 - Rendering corruption and jumping in text editor
		/// </summary>
		[Test()]
		public void TestBug682466 ()
		{
			Document document = new Mono.TextEditor.Document ();
			document.Text = 
@"0
1
2
+[3
4
5
6]
7
8
9
10";
			var segments = GetFoldSegments (document);
			document.UpdateFoldSegments (segments, false);
			Assert.AreEqual (true, document.FoldSegments.FirstOrDefault ().IsFolded);
			segments = GetFoldSegments (document);
			segments[0].IsFolded = false;
			document.UpdateFoldSegments (segments, false);
			Assert.AreEqual (5, document.LogicalToVisualLine (8));
		}
		
		[Test()]
		public void TestLogicalToVisualLine ()
		{
			Document document = new Mono.TextEditor.Document ();
			document.Text = 
@"-[0
+[1
2
]3
4
+[5
+[6
+[7
8
]9
]10
]11
]12
13
+[14
15
]16
17";
			document.UpdateFoldSegments (GetFoldSegments (document), false);
			Assert.AreEqual (13, document.VisualToLogicalLine (5));
			Assert.AreEqual (18, document.VisualToLogicalLine (8));
		}
		
		[Test()]
		public void TestCaretRight ()
		{
			var data = CaretMoveActionTests.Create (
@"1234567890
1234567890
123$4+[567890
1234]567890
1234567890");
			data.Document.UpdateFoldSegments (GetFoldSegments (data.Document), false);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (3, 5), data.Caret.Location);
			CaretMoveActions.Right (data);
			Assert.AreEqual (new DocumentLocation (4, 6), data.Caret.Location);
		}
		
		[Test()]
		public void TestCaretLeft ()
		{
			var data = CaretMoveActionTests.Create (
@"1234567890
1234567890
1234+[567890
1234]5$67890
1234567890");
			data.Document.UpdateFoldSegments (GetFoldSegments (data.Document), false);
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (4, 6), data.Caret.Location);
			CaretMoveActions.Left (data);
			Assert.AreEqual (new DocumentLocation (3, 5), data.Caret.Location);
		}
		
		
		[Test()]
		public void TestUpdateFoldSegmentBug2 ()
		{
			Document document = new Mono.TextEditor.Document ();
			document.Text = 
@"-[0
1
+[2
3]
4
+[5
6]
7
8
9
10
11
12
13]
14
15";
			var segments = GetFoldSegments (document);
			document.UpdateFoldSegments (segments, false);
			Assert.AreEqual (10, document.VisualToLogicalLine (8));
			int start = document.GetLine (2).Offset;
			int end = document.GetLine (8).Offset;
			((IBuffer)document).Remove (start, end - start);
			Assert.AreEqual (10, document.LogicalToVisualLine (10));
		}

	}
}
