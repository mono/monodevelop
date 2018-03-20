//
// DiffTrackerTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using Mono.TextEditor.Utils;
using System.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class DiffTrackerTests
	{
		static TextDocument GetDocument ()
		{
			TextDocument doc = new TextDocument ();
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < 10; i++)
				sb.AppendLine ("1234567890");
			doc.Text = sb.ToString ();
			doc.DiffTracker.SetBaseDocument (doc.CreateDocumentSnapshot ());

			return doc;
		}

		[Test]
		public void TestInsertChanged ()
		{
			var doc = GetDocument ();
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			doc.InsertText (doc.GetLine (5).Offset, "Hello");
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (5)));
		}

		[Test]
		public void TestRemoveChanged ()
		{
			var doc = GetDocument ();
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			doc.RemoveText (doc.GetLine (5).Offset, 1);
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (5)));
		}

		[Test]
		public void TestInsertLine ()
		{
			var doc = GetDocument ();
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			doc.InsertText (doc.GetLine (5).Offset, "Hello\n");
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (5)));
		}

		[Test]
		public void TestRemoveLine ()
		{
			var doc = GetDocument ();
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			doc.RemoveText (doc.GetLine (5).Offset, doc.GetLine (5).LengthIncludingDelimiter);
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (5)));
		}

		[Test]
		public void TestLowerLineChangeOnInsert ()
		{
			var doc = GetDocument ();
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			doc.InsertText (doc.GetLine (7).Offset, "Hello\n");
			doc.InsertText (doc.GetLine (5).Offset, "Hello\n");
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (7)));
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (8)));
		}


		[Test]
		public void TestLowerLineChangeOnRemove ()
		{
			var doc = GetDocument ();
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			doc.InsertText (doc.GetLine (7).Offset, "Hello\n");
			doc.RemoveText (doc.GetLine (5).Offset, doc.GetLine (5).LengthIncludingDelimiter);
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (5)));
			Assert.AreEqual (TextDocument.LineState.Unchanged, doc.DiffTracker.GetLineState (doc.GetLine (7)));
			Assert.AreEqual (TextDocument.LineState.Dirty, doc.DiffTracker.GetLineState (doc.GetLine (6)));
		}

	}
}
