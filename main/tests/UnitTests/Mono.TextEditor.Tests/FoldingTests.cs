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
					FoldSegment segment = new FoldSegment ("...", i, 0, FoldingType.None);
					segment.IsFolded = ch == '+';
					foldSegments.Push (segment);
				} else if (ch == ']' && foldSegments.Count > 0) {
					FoldSegment segment = foldSegments.Pop ();
					segment.Length = i - segment.Offset + 1;
					result.Add (segment);
					System.Console.WriteLine("Add:" + segment);
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
			document.UpdateFoldSegments (GetFoldSegments (document));
			do {
				Gtk.Application.RunIteration ();
			} while (!document.HasFoldSegments);
			Assert.AreEqual (4, document.LogicalToVisualLine (12));
			Assert.AreEqual (6, document.LogicalToVisualLine (16));
			Assert.AreEqual (7, document.LogicalToVisualLine (17));
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
			document.UpdateFoldSegments (GetFoldSegments (document));
			do {
				Gtk.Application.RunIteration ();
			} while (!document.HasFoldSegments);
			Assert.AreEqual (12, document.VisualToLogicalLine (4));
			Assert.AreEqual (17, document.VisualToLogicalLine (7));
		}
		
		
	}
}
