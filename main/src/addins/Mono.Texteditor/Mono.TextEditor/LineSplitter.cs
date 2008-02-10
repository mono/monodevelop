// LineSplitter.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Mono.TextEditor
{
	public class LineSplitter
	{
		LineSegmentTree lines = new LineSegmentTree ();

		public int LineCount {
			get {
				return lines.Count;
			}
		}
		
		public IEnumerable<LineSegment> Lines {
			get {
				RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = lines.GetNode (0).Iter;
				do {
					yield return iter.Current;
				} while (iter.MoveNext ());
			}
		}
		
		public void Clear ()
		{
			lines.Clear ();
		}
		public LineSplitter (IBuffer buffer)
		{
			lines.Clear ();
		}
		
		public LineSegment Get (int number)
		{
			LineSegmentTree.TreeNode node = lines.GetNode (number); 
			return node != null ? node : new LineSegment(0, 0);
		}
		
		public LineSegment GetByOffset (int offset)
		{
			return Get (OffsetToLineNumber (offset));
		}
		
		public int OffsetToLineNumber (int offset)
		{
			for (int i = 0; i < lines.Count - 1; ++i) {
				LineSegment line = Get (i);
				if (line.Offset <= offset && offset < line.EndOffset)
					return i;
			}
			
			if (lines.Count > 0 && Get (lines.Count - 1).Offset <= offset && offset <= Get (lines.Count - 1).EndOffset)
				return lines.Count - 1;
			return 0;
		}
		
		internal void TextReplaced (object sender, ReplaceEventArgs args)
		{
			if (args.Count > 0)
				TextRemove (args.Offset, args.Count);
			if (args.Value != null && args.Value.Length > 0)
				TextInsert (args.Offset, args.Value);
		}

		void TextRemove (int offset, int length)
		{
			if (length == 0 || (lines.Count == 1 && lines.Length == 0))
				return;
			LineSegmentTree.TreeNode startNode = lines.GetNodeAtOffset (offset);
			int charsRemoved = startNode.EndOffset - offset;
			if (offset + length < startNode.EndOffset) {
				lines.ChangeLength (startNode, startNode.Length - length);
				OnLineLenghtChanged (new LineEventArgs (startNode));
				return;
			}
			LineSegmentTree.TreeNode endNode = lines.GetNodeAtOffset (offset + length);
			int charsLeft = endNode.EndOffset - (offset + length);
			if (startNode == endNode) {
				lines.ChangeLength (startNode, startNode.Length - length);
				OnLineLenghtChanged (new LineEventArgs (startNode));
				return;
			}
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = startNode.Iter;
			iter.MoveNext ();
			LineSegment line;
			int cnt = 0;
			do  {
				line = iter.Current;
				iter.MoveNext ();
				lines.RemoveLine (line);
				++cnt;
			} while (line != endNode);
			if (LinesRemoved != null)
				LinesRemoved (this, new LineEventArgs (startNode));
			lines.ChangeLength (startNode, startNode.Length - charsRemoved + charsLeft, endNode.DelimiterLength);
			OnLineLenghtChanged (new LineEventArgs (startNode));
		}

		void TextInsert (int offset, StringBuilder text)
		{
			if (text == null || text.Length == 0)
				return;
			LineSegment line  = lines.GetNodeAtOffset (offset);
			LineSegment first = lines.GetNodeAtOffset (offset);
			int textOffset = 0;
			bool inserted = false; 
			ISegment delimiter;
			while ((delimiter = FindDelimiter (text, textOffset)) != null) {
				int newLineLength = line.EndOffset - (offset + textOffset);
				int oldDelimiterLength = line.DelimiterLength;
				lines.ChangeLength (line, offset + delimiter.EndOffset - line.Offset, delimiter.Length);
				line = this.lines.InsertAfter (line, newLineLength, oldDelimiterLength);
//				OnLineLenghtChanged (new LineEventArgs (line));
				textOffset = delimiter.EndOffset;
				inserted = true;
			}
			if (inserted && LinesInserted != null)
				LinesInserted (this, new LineEventArgs (first));
			if (textOffset != text.Length) { 
				lines.ChangeLength (line, line.Length + text.Length - textOffset);
				OnLineLenghtChanged (new LineEventArgs (line));
			}
		}
		
		internal void FireLineLenghtChange (LineSegment line)
		{
			OnLineLenghtChanged (new LineEventArgs (line));
		}
		
		protected virtual void OnLineLenghtChanged (LineEventArgs args)
		{
			if (LineLenghtChanged != null)
				LineLenghtChanged (this, args);
		}
		
		public event EventHandler<LineEventArgs> LineLenghtChanged;
		public event EventHandler<LineEventArgs> LinesInserted;
		public event EventHandler<LineEventArgs> LinesRemoved;
			
		public int GetLineNumberForOffset (int offset)
		{
			for (int i = 0; i < this.lines.Count; ++i) {
				if (this.Get(i).Offset <= offset && offset < this.Get(i).EndOffset)
					return i;
			}
			if (offset < 0)
				return 0;
			return this.LineCount;
		}
		
		Segment FindDelimiter (StringBuilder text, int startOffset) 
		{
			for (int i = startOffset; i < text.Length; i++) {
				switch (text[i]) {
					case '\r':
						return new Segment (i, i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1);
					case '\n':
						return new Segment (i, 1);
				}
			}
			return null;
		}
	}
}