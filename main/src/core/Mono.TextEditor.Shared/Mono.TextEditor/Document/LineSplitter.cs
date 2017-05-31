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
using System.Collections.Generic;
using System.Linq;
using Mono.TextEditor.Utils;
using System.Diagnostics;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor
{
	class LineSplitter : ILineSplitter
	{
		public IEnumerable<DocumentLine> Lines {
			get {
				foreach (DocumentLine line in tree)
					yield return line;
			}
		}

		public LineSplitter ()
		{
			Clear ();
		}

		public IEnumerable<DocumentLine> GetLinesBetween (int startLine, int endLine)
		{
			var startNode = GetNode (startLine - 1);
			if (startNode == null)
				yield break;
			int curLine = startLine;
			do {
				yield return startNode;
				startNode = startNode.GetNextNode ();
			} while (startNode != null && curLine++ < endLine);
		}

		public IEnumerable<DocumentLine> GetLinesStartingAt (int startLine)
		{
			var startNode = GetNode (startLine - 1);
			if (startNode == null)
				yield break;
			do {
				yield return startNode;
				startNode = startNode.GetNextNode ();
			} while (startNode != null);
		}

		public IEnumerable<DocumentLine> GetLinesReverseStartingAt (int startLine)
		{
			var startNode = GetNode (startLine);
			if (startNode == null)
				yield break;
			do {
				yield return startNode;
				startNode = startNode.GetPrevNode ();
			} while (startNode != null);
		}

		public DocumentLine Get (int number)
		{
			return GetNode (number - 1);
		}

		public DocumentLine GetLineByOffset (int offset)
		{
			var lineNumber = OffsetToLineNumber (offset);
			return lineNumber >= 1 ? Get (lineNumber) : null;
		}

		public void TextReplaced (object sender, TextChangeEventArgs args)
		{
			for (int i = 0; i < args.TextChanges.Count; ++i) {
				var change = args.TextChanges[i];
				if (change.RemovalLength > 0)
					TextRemove (change.Offset, change.RemovalLength);
				if (change.InsertionLength > 0)
					TextInsert (change.Offset, change.InsertedText.Text);
			}
		}

		public void TextRemove (int offset, int length)
		{
			if (length == 0 || (Count == 1 && Length == 0))
				return;
			
			var startNode = GetNodeAtOffset (offset);
			int charsRemoved = startNode.EndOffsetIncludingDelimiter - offset;
			if (offset + length < startNode.EndOffsetIncludingDelimiter) {
				ChangeLength (startNode, startNode.LengthIncludingDelimiter - length);
				return;
			}
			var endNode = GetNodeAtOffset (offset + length);
			if (endNode == null)
				return;
			int charsLeft = endNode.EndOffsetIncludingDelimiter - (offset + length);
			if (startNode == endNode) {
				ChangeLength (startNode, startNode.LengthIncludingDelimiter - length);
				return;
			}
			var iter = startNode;
			iter = iter.GetNextNode ();
			int lineNumber = iter.LineNumber;
			TreeNode line;
			do {
				line = iter;
				iter = iter.GetNextNode ();
				RemoveLine (line, lineNumber);
			} while (line != endNode);
			ChangeLength (startNode, startNode.LengthIncludingDelimiter - charsRemoved + charsLeft, endNode.UnicodeNewline);
		}

		public bool LineEndingMismatch {
			get;
			set;
		}

		//bool inInit;
		public void Initalize (string text, out DocumentLine longestLine)
		{
			LineEndingMismatch = false;
			Clear ();
			longestLine = Get (1);
			if (string.IsNullOrEmpty (text))
				return;
			var nodes = new List<TreeNode> ();

			var delimiterType = UnicodeNewline.Unknown;
			int offset = 0, maxLength = 0;
			while (true) {
				var delimiter = NextDelimiter (text, offset);
				if (delimiter.IsInvalid)
					break;
				int delimiterEndOffset = delimiter.Offset + delimiter.Length;
				var length = delimiterEndOffset - offset;
				var newLine = new TreeNode (length, delimiter.UnicodeNewline);
				nodes.Add (newLine);
				if (length > maxLength) {
					maxLength = length;
					longestLine = newLine;
				}
				if (offset > 0) {
					LineEndingMismatch |= delimiterType != delimiter.UnicodeNewline;
				} else {
					delimiterType = delimiter.UnicodeNewline;
				}
				offset = delimiterEndOffset;
			}
			var lastLine = new TreeNode (text.Length - offset, 0);
			nodes.Add (lastLine);
			
			int height = GetTreeHeight (nodes.Count);

			var newRoot = BuildTree (nodes, 0, nodes.Count, height);
			if (newRoot != null) {
				tree.Root = newRoot;
				tree.Root.Color = RedBlackColor.Black;
				tree.Count = nodes.Count;
			}
		}
		
		TreeNode BuildTree (System.Collections.Generic.List<TreeNode> nodes, int start, int end, int subtreeHeight)
		{
			if (start == end)
				return null;
			int middle = (start + end) / 2;
			var node = nodes [middle];
			node.left = BuildTree (nodes, start, middle, subtreeHeight - 1);
			node.right = BuildTree (nodes, middle + 1, end, subtreeHeight - 1);
			if (node.left != null)
				node.left.parent = node;
			if (node.right != null)
				node.right.parent = node;
			if (subtreeHeight == 1)
				node.Color = RedBlackColor.Red;
			((IRedBlackTreeNode)node).UpdateAugmentedData ();
			return node;
		}
		
		static int GetTreeHeight (int size)
		{
			return size == 0 ? 0 : GetTreeHeight (size / 2) + 1;
		}

		public void TextInsert (int offset, string text)
		{
			if (string.IsNullOrEmpty (text))
				return;
			
			var line = GetNodeAtOffset (offset);
			int textOffset = 0;
			int lineOffset = line.Offset;

			while (true) {
				var delimiter = NextDelimiter (text, textOffset);
				if (delimiter.IsInvalid)
					break;

				int newLineLength = lineOffset + line.LengthIncludingDelimiter - (offset + textOffset);
				int delimiterEndOffset = delimiter.Offset + delimiter.Length;
				int curLineLength = offset + delimiterEndOffset - lineOffset;
				var oldDelimiterLength = line.UnicodeNewline;
				ChangeLength (line, curLineLength, delimiter.UnicodeNewline);
				line = InsertAfter (line, newLineLength, oldDelimiterLength);
				textOffset = delimiterEndOffset;
				lineOffset += curLineLength;
			}

			if (textOffset != text.Length)
				ChangeLength (line, line.LengthIncludingDelimiter + text.Length - textOffset);
		}

		internal struct Delimiter
		{
			public static readonly Delimiter Invalid = new Delimiter (-1, 0);

			public readonly int Offset;
			public readonly UnicodeNewline UnicodeNewline;

			public int Length {
				get {
					return UnicodeNewline == UnicodeNewline.CRLF ? 2 : 1;
				}
			}

			public int EndOffset {
				get { return Offset + Length; }
			}

			public bool IsInvalid {
				get {
					return Offset < 0;
				}
			}

			public Delimiter (int offset, UnicodeNewline unicodeNewline)
			{
				Offset = offset;
				UnicodeNewline = unicodeNewline;
			}
		}

		static unsafe internal Delimiter NextDelimiter (string text, int offset)
		{
			fixed (char* start = text) {
				char* p = start + offset;
				char* endPtr = start + text.Length;

				while (p < endPtr) {
					switch (*p) {
					case NewLine.CR:
						char* nextp = p + 1;
						if (nextp < endPtr && *nextp == NewLine.LF)
							return new Delimiter ((int)(p - start), UnicodeNewline.CRLF);
						return new Delimiter ((int)(p - start), UnicodeNewline.CR);
					case NewLine.LF:
						return new Delimiter ((int)(p - start), UnicodeNewline.LF);
					case NewLine.NEL:
						return new Delimiter ((int)(p - start), UnicodeNewline.NEL);
					//case NewLine.VT:
					//	return new Delimiter ((int)(p - start), UnicodeNewline.VT);
					//case NewLine.FF:
					//	return new Delimiter ((int)(p - start), UnicodeNewline.FF);
					case NewLine.LS:
						return new Delimiter ((int)(p - start), UnicodeNewline.LS);
					case NewLine.PS:
						return new Delimiter ((int)(p - start), UnicodeNewline.PS);
					}
					p++;
				}
				return Delimiter.Invalid;
			}
		}

		#region Line segment tree
		class TreeNode : DocumentLine, IRedBlackTreeNode
		{
			public override int LineNumber {
				get {
					var node = this;
					int index = left != null ? left.Count : 0;
					while (node.parent != null) {
						if (node == node.parent.right) {
							if (node.parent.left != null)
								index += node.parent.left.Count;
							index++;
						}
						node = node.parent;
					}
					return index + 1;
				}
			}

			public override int Offset {
				get {
					var node = this;
					int offset = node.left != null ? node.left.TotalLength : 0;
					while (node.parent != null) {
						if (node == node.parent.right) {
							if (node.parent.left != null)
								offset += node.parent.left.TotalLength;
							if (node.parent != null)
								offset += node.parent.LengthIncludingDelimiter;
						}
						node = node.parent;
					}
					return offset;
				}
				set {
					throw new NotSupportedException ();
				}
			}

			public override DocumentLine NextLine {
				get {
					if (right != null)
						return right.GetOuterLeft ();
					TreeNode lastNode;
					TreeNode node = this;
					do {
						lastNode = node;
						node = node.parent;
					} while (node != null && node.right == lastNode);
					return node;
				}
			}

			public override DocumentLine PreviousLine {
				get {
					if (left != null)
						return left.GetOuterRight ();
					TreeNode lastNode;
					TreeNode node = this;
					do {
						lastNode = node;
						node = node.parent;
					} while (node != null && node.left == lastNode);
					return node;
				}
			}
	
			public int Count = 1;
			public int TotalLength;

			public TreeNode (int length, UnicodeNewline newLine) : base(length, newLine)
			{
			}

			public override string ToString ()
			{
				return String.Format ("[TreeNode: Line={0}, Count={1}, TotalLength={2}]", base.ToString (), Count, TotalLength);
			}

			#region IRedBlackTreeNode implementation
			public void UpdateAugmentedData ()
			{
				int count = 1;
				int totalLength = LengthIncludingDelimiter;
			
				if (left != null) {
					count += left.Count;
					totalLength += left.TotalLength;
				}
			
				if (right != null) {
					count += right.Count;
					totalLength += right.TotalLength;
				}
				
				if (count != Count || totalLength != TotalLength) {
					Count = count;
					TotalLength = totalLength;
					if (parent != null)
						parent.UpdateAugmentedData ();
				}
			}
			
			internal TreeNode parent, left, right;
			
			Mono.TextEditor.Utils.IRedBlackTreeNode Mono.TextEditor.Utils.IRedBlackTreeNode.Parent {
				get {
					return parent;
				}
				set {
					parent = (TreeNode)value;
				}
			}

			Mono.TextEditor.Utils.IRedBlackTreeNode Mono.TextEditor.Utils.IRedBlackTreeNode.Left {
				get {
					return left;
				}
				set {
					left = (TreeNode)value;
				}
			}

			Mono.TextEditor.Utils.IRedBlackTreeNode Mono.TextEditor.Utils.IRedBlackTreeNode.Right {
				get {
					return right;
				}
				set {
					right = (TreeNode)value;
				}
			}

			public RedBlackColor Color {
				get;
				set;
			}
			#endregion
		}

		readonly Mono.TextEditor.Utils.RedBlackTree<TreeNode> tree = new Mono.TextEditor.Utils.RedBlackTree<TreeNode> ();

		public int Count {
			get { return tree.Count; }
		}

		public int Length {
			get { return tree.Root.TotalLength; }
		}

		public void Clear ()
		{
			tree.Root = new TreeNode (0, 0);
			tree.Count = 1;
		}

		TreeNode InsertAfter (TreeNode segment, int length, UnicodeNewline newLine)
		{
			var result = new TreeNode (length, newLine);
			if (segment == null) {
				tree.Root = result;
				tree.Count = 1;
				return result;
			}
			
			tree.InsertAfter (segment, result);
			result.UpdateAugmentedData ();
			OnLineInserted (new LineEventArgs (result));
			return result;
		}

		public override string ToString ()
		{
			return tree.ToString ();
		}

		void ChangeLength (TreeNode line, int newLength)
		{
			ChangeLength (line, newLength, line.UnicodeNewline);
		}

		void ChangeLength (TreeNode line, int newLength, UnicodeNewline newLine)
		{
			line.LengthIncludingDelimiter = newLength;
			line.UnicodeNewline = newLine;
			OnLineChanged (new LineEventArgs (line));
			line.UpdateAugmentedData ();
		}

		protected virtual void OnLineChanged (LineEventArgs e)
		{
			EventHandler<LineEventArgs> handler = LineChanged;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler<LineEventArgs> LineChanged;

		protected virtual void OnLineInserted (LineEventArgs e)
		{
			EventHandler<LineEventArgs> handler = LineInserted;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler<LineEventArgs> LineInserted;

		protected virtual void OnLineRemoved (LineEventArgs e)
		{
			EventHandler<LineEventArgs> handler = LineRemoved;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler<LineEventArgs> LineRemoved;

		TreeNode GetNodeAtOffset (int offset)
		{
			if (offset == tree.Root.TotalLength)
				return tree.Root.GetOuterRight ();
			var node = tree.Root;
			int i = offset;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && i < node.left.TotalLength) {
					node = node.left;
				} else {
					if (node.left != null)
						i -= node.left.TotalLength;
					i -= node.LengthIncludingDelimiter;
					if (i < 0)
						return node;
					node = node.right;
				}
			}
		}

		public int OffsetToLineNumber (int offset)
		{
			var node = GetNodeAtOffset (offset);
			if (node == null)
				return 0;
			int result = node.left != null ? node.left.Count : 0;
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null)
						result += node.parent.left.Count;
					result++;
				}
				node = node.parent;
			}
			return result + 1;
		}

		void RemoveLine (TreeNode line, int lineNumber)
		{
			var parent = line.parent;
			tree.Remove (line);
			if (parent != null)
				parent.UpdateAugmentedData ();
			OnLineRemoved (new LineEventArgs (line, lineNumber));
		}

		TreeNode GetNode (int index)
		{
			#if DEBUG
			if (index < 0)
				Debug.Assert (false, "index must be >=0 but was " + index + "." + Environment.NewLine + "Stack trace:" + Environment.StackTrace);
			#endif
			var node = tree.Root;
			int i = index;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && i < node.left.Count) {
					node = node.left;
				} else {
					if (node.left != null) {
						i -= node.left.Count;
					}
					if (i <= 0)
						return node;
					i--;
					node = node.right;
				}
			}
		}
		#endregion
	}
}
