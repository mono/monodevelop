// 
// TextEditorTestBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace Mono.TextEditor.Tests
{
	public class TextEditorTestBase
	{
		static bool firstRun = true;
		
		[TestFixtureSetUp]
		public virtual void Setup ()
		{
			if (firstRun) {
				Gtk.Application.Init ();
				firstRun = false;
			}
		}

		[TestFixtureTearDown]
		public virtual void TearDown ()
		{
		}



		public static TextEditorData Create (string content, ITextEditorOptions options = null)
		{
			var data = new TextEditorData ();
			if (options != null)
				data.Options = options;
			var sb = new StringBuilder ();
			int caretIndex = -1, selectionStart = -1, selectionEnd = -1;
			var foldSegments = new List<FoldSegment> ();
			var foldStack = new Stack<FoldSegment> ();

			for (int i = 0; i < content.Length; i++) {
				var ch = content [i];
				switch (ch) {
				case '$':
					caretIndex = sb.Length;
					break;
				case '<':
					if (i + 1 < content.Length) {
						if (content [i + 1] == '-') {
							selectionStart = sb.Length;
							i++;
							break;
						}
					}
					goto default;
				case '-':
					if (i + 1 < content.Length) {
						var next = content [i + 1];
						if (next == '>') {
							selectionEnd = sb.Length;
							i++;
							break;
						}
						if (next == '[') {
							var segment = new FoldSegment (data.Document, "...", sb.Length, 0, FoldingType.None);
							segment.IsFolded = false;
							foldStack.Push (segment);
							i++;
							break;
						}
					}
					goto default;
				case '+':
					if (i + 1 < content.Length) {
						var next = content [i + 1];
						if (next == '[') {
							var segment = new FoldSegment (data.Document, "...", sb.Length, 0, FoldingType.None);
							segment.IsFolded = true;
							foldStack.Push (segment);
							i++;
							break;
						}
					}
					goto default;
				case ']':
					if (foldStack.Count > 0) {
						FoldSegment segment = foldStack.Pop ();
						segment.Length = sb.Length - segment.Offset;
						foldSegments.Add (segment);
						break;
					}
					goto default;
				default:
					sb.Append (ch);
					break;
				}
			}

			data.Text = sb.ToString ();

			if (caretIndex >= 0)
				data.Caret.Offset = caretIndex;
			if (selectionStart >= 0) {
				if (caretIndex == selectionStart) {
					data.SetSelection (selectionEnd, selectionStart);
				} else {
					data.SetSelection (selectionStart, selectionEnd);
					if (caretIndex < 0)
						data.Caret.Offset = selectionEnd;
				}
			}
			if (foldSegments.Count > 0)
				data.Document.UpdateFoldSegments (foldSegments);
			return data;
		}

		public static void Check (TextEditorData data, string content)
		{
			var checkDocument = Create (content);
			if (checkDocument.Text != data.Text) {
				Console.WriteLine ("was:");
				Console.WriteLine (data.Text);
				Console.WriteLine ("expected:");
				Console.WriteLine (checkDocument.Text);
			}
			Assert.AreEqual (checkDocument.Text, data.Text);
			Assert.AreEqual (checkDocument.Caret.Offset, data.Caret.Offset, "Caret offset mismatch.");
			if (data.IsSomethingSelected || checkDocument.IsSomethingSelected)
				Assert.AreEqual (checkDocument.SelectionRange, data.SelectionRange, "Selection mismatch.");
			if (checkDocument.Document.HasFoldSegments || data.Document.HasFoldSegments) {
				var list1 = new List<FoldSegment> (checkDocument.Document.FoldSegments);
				var list2 = new List<FoldSegment> (data.Document.FoldSegments);
				Assert.AreEqual (list1.Count, list2.Count, "Fold segment count mismatch.");
				for (int i = 0; i < list1.Count; i++) {
					Assert.AreEqual (list1 [i].Segment, list2 [i].Segment, "Fold " + i + " segment mismatch.");
					Assert.AreEqual (list1 [i].IsFolded, list2 [i].IsFolded, "Fold " + i + " isFolded mismatch.");
				}
			}
		}
	}
}
