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

namespace Mono.TextEditor
{
	public class LineSplitter : ILineSplitter
	{
		public IEnumerable<LineSegment> Lines {
			get {
				var iter = GetNode (0).Iter;
				do {
					yield return iter.Current;
				} while (iter.MoveNext ());
			}
		}

		public LineSplitter ()
		{
			tree.ChildrenChanged += (sender, args) => UpdateNode (args.Node);
			tree.NodeRotateLeft += (sender, args) =>
			{
				UpdateNode (args.Node);
				UpdateNode (args.Node.Parent);
			};
			tree.NodeRotateRight += (sender, args) =>
			{
				UpdateNode (args.Node);
				UpdateNode (args.Node.Parent);
			};
			Clear ();
		}

		public IEnumerable<LineSegment> GetLinesBetween (int startLine, int endLine)
		{
			var startNode = GetNode (startLine - 1);
			if (startNode == null)
				yield break;
			var iter = startNode.Iter;
			int curLine = startLine;
			do {
				yield return iter.Current;
			} while (iter.MoveNext () && curLine++ < endLine);
		}

		public IEnumerable<LineSegment> GetLinesStartingAt (int startLine)
		{
			var startNode = GetNode (startLine - 1);
			if (startNode == null)
				yield break;
			var iter = startNode.Iter;
			do {
				yield return iter.Current;
			} while (iter.MoveNext ());
		}

		public IEnumerable<LineSegment> GetLinesReverseStartingAt (int startLine)
		{
			var startNode = GetNode (startLine);
			if (startNode == null)
				yield break;
			var iter = startNode.Iter;
			do {
				yield return iter.Current;
			} while (iter.MoveBack ());
		}

		public LineSegment Get (int number)
		{
			return GetNode (number - 1);
		}

		public LineSegment GetLineByOffset (int offset)
		{
			return Get (OffsetToLineNumber (offset));
		}

		public void TextReplaced (object sender, ReplaceEventArgs args)
		{
			if (args.Count > 0)
				TextRemove (args.Offset, args.Count);
			if (!string.IsNullOrEmpty (args.Value))
				TextInsert (args.Offset, args.Value);
		}

		public void TextRemove (int offset, int length)
		{
			if (length == 0 || (Count == 1 && Length == 0))
				return;
			
			var startNode = GetNodeAtOffset (offset);
			int charsRemoved = startNode.EndOffset - offset;
			if (offset + length < startNode.EndOffset) {
				ChangeLength (startNode, startNode.Length - length);
				return;
			}
			var endNode = GetNodeAtOffset (offset + length);
			if (endNode == null)
				return;
			int charsLeft = endNode.EndOffset - (offset + length);
			if (startNode == endNode) {
				ChangeLength (startNode, startNode.Length - length);
				return;
			}
			var iter = startNode.Iter;
			iter.MoveNext ();
			TreeNode line;
			do {
				line = iter.Current;
				iter.MoveNext ();
				RemoveLine (line);
			} while (line != endNode);
			ChangeLength (startNode, startNode.Length - charsRemoved + charsLeft, endNode.DelimiterLength);
		}

		bool inInit;
		public void Initalize (string text)
		{
			Clear ();
			
			var nodes = new List<TreeNode> ();
			int offset = 0;
			foreach (var delimiter in FindDelimiter (text)) {
				int delimiterEndOffset = delimiter.Offset + delimiter.Length;
				var newLine = new TreeNode (delimiterEndOffset - offset, delimiter.Length);
				nodes.Add (newLine);
				offset = delimiterEndOffset;
			}
			var lastLine = new TreeNode (text.Length - offset, 0);
			nodes.Add (lastLine);
			
			int height = GetTreeHeight (nodes.Count);
			
			var newRoot = BuildTree (nodes, 0, nodes.Count, height);
			if (newRoot != null) {
				tree.Root = newRoot;
				tree.Root.Color = Mono.TextEditor.RedBlackTree<TreeNode>.Black;
				tree.Count = nodes.Count;
			}
		}
		
		public Mono.TextEditor.RedBlackTree<TreeNode>.RedBlackTreeNode BuildTree (System.Collections.Generic.List<TreeNode> nodes, int start, int end, int subtreeHeight)
		{
			if (start == end)
				return null;
			int middle = (start + end) / 2;
			var node = new RedBlackTree<TreeNode>.RedBlackTreeNode (nodes[middle]);
			nodes[middle].treeNode = node;
			node.Left = BuildTree (nodes, start, middle, subtreeHeight - 1);
			node.Right = BuildTree (nodes, middle + 1, end, subtreeHeight - 1);
			if (node.Left != null)
				node.Left.Parent = node;
			if (node.Right != null)
				node.Right.Parent = node;
			if (subtreeHeight == 1)
				node.Color = Mono.TextEditor.RedBlackTree<TreeNode>.Red;
			UpdateNode (node);
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
			foreach (var delimiter in FindDelimiter (text)) {
				int newLineLength = lineOffset + line.Length - (offset + textOffset);
				int delimiterEndOffset = delimiter.Offset + delimiter.Length;
				int curLineLength = offset + delimiterEndOffset - lineOffset;
				int oldDelimiterLength = line.DelimiterLength;
				ChangeLength (line, curLineLength, delimiter.Length);
				
				line = InsertAfter (line, newLineLength, oldDelimiterLength);
				textOffset = delimiterEndOffset;
				lineOffset += curLineLength;
			}
			
			if (textOffset != text.Length)
				ChangeLength (line, line.Length + text.Length - textOffset);
		}

		internal struct Delimiter
		{
			public readonly int Offset;
			public readonly int Length;

			public int EndOffset {
				get { return Offset + Length; }
			}

			public Delimiter (int offset, int length)
			{
				Offset = offset;
				Length = length;
			}
		}

		static internal IEnumerable<Delimiter> FindDelimiter (string text)
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

		static internal int CountLines (string text)
		{
			return FindDelimiter (text).Count ();
		}

		#region Line segment tree
		public class TreeNode : LineSegment
		{
			internal RedBlackTree<TreeNode>.RedBlackTreeNode treeNode;

			public RedBlackTree<TreeNode>.RedBlackTreeIterator Iter {
				get { return new RedBlackTree<TreeNode>.RedBlackTreeIterator (treeNode); }
			}

			public override int Offset {
				get { return treeNode != null ? GetOffsetFromNode (treeNode) : -1; }
				set {
					throw new NotSupportedException ();
				}
			}


			public int Count = 1;
			public int TotalLength;

			public TreeNode (int length, int delimiterLength) : base(length, delimiterLength)
			{
			}

			public override string ToString ()
			{
				return String.Format ("[TreeNode: Line={0}, Count={1}, TotalLength={2}]", base.ToString (), Count, TotalLength);
			}
		}

		readonly RedBlackTree<TreeNode> tree = new RedBlackTree<TreeNode> ();

		public int Count {
			get { return tree.Count; }
		}

		public int Length {
			get { return tree.Root.Value.TotalLength; }
		}

		static void UpdateNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
		{
			if (node == null)
				return;
			
			int count = 1;
			int totalLength = node.Value.Length;
			
			if (node.Left != null) {
				count += node.Left.Value.Count;
				totalLength += node.Left.Value.TotalLength;
			}
			
			if (node.Right != null) {
				count += node.Right.Value.Count;
				totalLength += node.Right.Value.TotalLength;
			}
			if (count != node.Value.Count || totalLength != node.Value.TotalLength) {
				node.Value.Count = count;
				node.Value.TotalLength = totalLength;
				UpdateNode (node.Parent);
			}
		}

		public void Clear ()
		{
			tree.Root = new RedBlackTree<TreeNode>.RedBlackTreeNode (new TreeNode (0, 0));
			tree.Root.Value.treeNode = tree.Root;
			tree.Count = 1;
		}

		public TreeNode InsertAfter (TreeNode segment, int length, int delimiterLength)
		{
			var result = new TreeNode (length, delimiterLength) { StartSpan = segment.StartSpan };
			var newNode = new RedBlackTree<TreeNode>.RedBlackTreeNode (result);
			var iter = segment.Iter;
			if (iter == null) {
				tree.Root = newNode;
				result.treeNode = tree.Root;
				tree.Count = 1;
				return result;
			}
			
			if (iter.Node.Right == null) {
				tree.Insert (iter.Node, newNode, false);
			} else {
				tree.Insert (iter.Node.Right.OuterLeft, newNode, true);
			}
			result.treeNode = newNode;
			UpdateNode (newNode);
			OnLineChanged (new LineEventArgs (result));
			return result;
		}

		public override string ToString ()
		{
			return tree.ToString ();
		}


		public void ChangeLength (TreeNode line, int newLength)
		{
			ChangeLength (line, newLength, line.DelimiterLength);
		}

		public void ChangeLength (TreeNode line, int newLength, int delimiterLength)
		{
			line.Length = newLength;
			line.DelimiterLength = delimiterLength;
			OnLineChanged (new LineEventArgs (line));
			UpdateNode (line.Iter.CurrentNode);
		}

		protected virtual void OnLineChanged (LineEventArgs e)
		{
			if (inInit)
				return;
			EventHandler<LineEventArgs> handler = LineChanged;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler<LineEventArgs> LineChanged;

		protected virtual void OnLineInserted (LineEventArgs e)
		{
			if (inInit)
				return;
			EventHandler<LineEventArgs> handler = LineInserted;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler<LineEventArgs> LineInserted;

		protected virtual void OnLineRemoved (LineEventArgs e)
		{
			if (inInit)
				return;
			EventHandler<LineEventArgs> handler = LineRemoved;
			if (handler != null)
				handler (this, e);
		}
		public event EventHandler<LineEventArgs> LineRemoved;

		public static int GetOffsetFromNode (RedBlackTree<TreeNode>.RedBlackTreeNode node)
		{
			int offset = node.Left != null ? node.Left.Value.TotalLength : 0;
			while (node.Parent != null) {
				if (node == node.Parent.Right) {
					if (node.Parent.Left != null && node.Parent.Left.Value != null)
						offset += node.Parent.Left.Value.TotalLength;
					if (node.Parent.Value != null)
						offset += node.Parent.Value.Length;
				}
				node = node.Parent;
			}
			return offset;
		}

		RedBlackTree<TreeNode>.RedBlackTreeNode GetTreeNodeAtOffset (int offset)
		{
			if (offset == tree.Root.Value.TotalLength)
				return tree.Root.OuterRight;
			RedBlackTree<TreeNode>.RedBlackTreeNode node = tree.Root;
			int i = offset;
			while (true) {
				if (node == null)
					return null;
				if (node.Left != null && i < node.Left.Value.TotalLength) {
					node = node.Left;
				} else {
					if (node.Left != null)
						i -= node.Left.Value.TotalLength;
					i -= node.Value.Length;
					if (i < 0)
						return node;
					node = node.Right;
				}
			}
		}

		public TreeNode GetNodeAtOffset (int offset)
		{
			var node = GetTreeNodeAtOffset (offset);
			return node != null ? node.Value : null;
		}

		public int OffsetToLineNumber (int offset)
		{
			var node = GetTreeNodeAtOffset (offset);
			if (node == null)
				return 0;
			int result = node.Left != null ? node.Left.Value.Count : 0;
			while (node.Parent != null) {
				if (node == node.Parent.Right) {
					if (node.Parent.Left != null)
						result += node.Parent.Left.Value.Count;
					result++;
				}
				node = node.Parent;
			}
			return result + 1;
		}

		public void RemoveLine (TreeNode line)
		{
			var parent = line.Iter.CurrentNode.Parent;
			tree.RemoveAt (line.Iter);
			UpdateNode (parent);
			OnLineRemoved (new LineEventArgs (line));
		}

		public TreeNode GetNode (int index)
		{
			#if DEBUG
			if (index < 0)
				Debug.Assert (false, "index must be >=0 but was " + index + "." + Environment.NewLine + "Stack trace:" + Environment.StackTrace);
			#endif
			RedBlackTree<TreeNode>.RedBlackTreeNode node = tree.Root;
			int i = index;
			while (true) {
				if (node == null)
					return null;
				if (node.Left != null && i < node.Left.Value.Count) {
					node = node.Left;
				} else {
					if (node.Left != null) {
						i -= node.Left.Value.Count;
					}
					if (i <= 0)
						return node.Value;
					i--;
					node = node.Right;
				}
			}
		}
		#endregion
	}
}
