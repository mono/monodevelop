// 
// BookmarkActions.cs
// 
// Author:
//   Mike Krüger <mkrueger@novell.com>
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Text;

using Gtk;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public class BookmarkActions
	{
		static int GetNextOffset (Document document, int lineNumber)
		{
			LineSegment startLine = document.GetLine (lineNumber);
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = startLine.Iter;
			while (iter.MoveNext ()) {
				LineSegment line = iter.Current;
				if (line.IsBookmarked) {
					return line.Offset;
				}
			}
			return -1;
		}
		
		public static void GotoNext (TextEditorData data)
		{
			int offset = GetNextOffset (data.Document, data.Caret.Line);
			if (offset < 0)
				offset = GetNextOffset (data.Document, 0);
			if (offset >= 0)
				data.Caret.Offset = offset;
		}
		
		static int GetPrevOffset (Document document, int lineNumber)
		{
			LineSegment startLine = document.GetLine (lineNumber);
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = startLine.Iter;
			while (iter.MoveBack ()) {
				LineSegment line = iter.Current;
				if (line.IsBookmarked) {
					return line.Offset;
				}
			}
			return -1;
		}
		
		public static void GotoPrevious (TextEditorData data)
		{
			int offset = GetPrevOffset (data.Document, data.Caret.Line);
			if (offset < 0)
				offset = GetPrevOffset (data.Document, data.Document.LineCount - 1);
			if (offset >= 0)
				data.Caret.Offset = offset;
		}
		
		public static void ClearAll (TextEditorData data)
		{
			bool redraw = false;
			foreach (LineSegment line in data.Document.Lines) {
				redraw |= line.IsBookmarked;
				line.IsBookmarked = false;
			}
			if (redraw) {
				data.Document.RequestUpdate (new UpdateAll ());
				data.Document.CommitDocumentUpdate ();
			}
		}
	}
}
