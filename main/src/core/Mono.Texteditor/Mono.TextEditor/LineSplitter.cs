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
using System.Linq;

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
		
		internal LineSegmentTree LineSegmentTree {
			get {
				return lines;
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
			return lines.GetNode (number);
		}
		
		public LineSegment GetLineByOffset (int offset)
		{
			return Get (OffsetToLineNumber (offset));
		}
		
		
		public void TextReplaced (object sender, ReplaceEventArgs args)
		{
			if (args.Count > 0)
				TextRemove (args.Offset, args.Count);
			if (args.Value != null && args.Value.Length > 0)
				TextInsert (args.Offset, args.Value);
		}

		void TextRemove (int offset, int length)
		{
			if (length == 0 || (lines.Count == 1 && lines.Length == 0)) 
				return; 
			LineSegmentTree.TreeNode startNode = lines.GetNodeAtOffset (offset);
			int charsRemoved = startNode.EndOffset - offset;
			if (offset + length < startNode.EndOffset) {
				lines.ChangeLength (startNode, startNode.Length - length);
				return;
			}
			LineSegmentTree.TreeNode endNode = lines.GetNodeAtOffset (offset + length);
			if (endNode == null)
				return;
			int charsLeft = endNode.EndOffset - (offset + length);
			if (startNode == endNode) {
				lines.ChangeLength (startNode, startNode.Length - length);
				return;
			}
			RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = startNode.Iter;
			iter.MoveNext ();
			LineSegment line;
			int cnt = 0;
			do {
				line = iter.Current;
				iter.MoveNext ();
				lines.RemoveLine (line);
				++cnt;
			} while (line != endNode);
			lines.ChangeLength (startNode, startNode.Length - charsRemoved + charsLeft, endNode.DelimiterLength);
		}
		
		void TextInsert (int offset, string text)
		{
			if (text == null || text.Length == 0)
				return;
			
			LineSegment line = lines.GetNodeAtOffset (offset);
			int textOffset = 0;
			int lineOffset = line.Offset;
			foreach (var delimiter in FindDelimiter (text)) {
				int newLineLength = lineOffset + line.Length - (offset + textOffset);
				int delimiterEndOffset = delimiter.Offset + delimiter.Length;
				int curLineLength = offset + delimiterEndOffset - lineOffset;
				int oldDelimiterLength = line.DelimiterLength;
				lines.ChangeLength (line, curLineLength, delimiter.Length);
				
				line = this.lines.InsertAfter (line, newLineLength, oldDelimiterLength);
				textOffset = delimiterEndOffset;
				lineOffset += curLineLength;
			}
			
			if (textOffset != text.Length) { 
				lines.ChangeLength (line, line.Length + text.Length - textOffset);
			}
		}
		
		public int OffsetToLineNumber (int offset)
		{
			return lines.OffsetToLineNumber (offset);
		}
		
		struct Delimiter 
		{
			public readonly int Offset;
			public readonly int Length;
			
			public Delimiter (int offset, int length)
			{
				this.Offset = offset;
				this.Length = length;
			}
		}
		
		static IEnumerable<Delimiter> FindDelimiter (string text) 
		{
			for (int i = 0; i < text.Length; i++) {
				switch (text[i]) {
				case '\r':
					if (i + 1 < text.Length && text[i + 1] == '\n') {
						yield return new Delimiter (i, 2);
						i++;
					} else {
						yield return new Delimiter (i, 1);
					}
					break;
				case '\n':
					yield return new Delimiter (i, 1);
					break;
				}
			}
		}
		
		internal static int CountLines (string text)
		{
			return FindDelimiter (text).Count ();
		}
	}
}