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

namespace Mono.TextEditor
{
	public class LineSplitter : ILineSplitter
	{
		public IEnumerable<LineSegment> Lines {
			get {
				foreach (LineSegment line in tree)
					yield return line;
			}
		}

		public LineSplitter ()
		{
			Clear ();
		}

		public IEnumerable<LineSegment> GetLinesBetween (int startLine, int endLine)
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

		public IEnumerable<LineSegment> GetLinesStartingAt (int startLine)
		{
			var startNode = GetNode (startLine - 1);
			if (startNode == null)
				yield break;
			do {
				yield return startNode;
				startNode = startNode.GetNextNode ();
			} while (startNode != null);
		}

		public IEnumerable<LineSegment> GetLinesReverseStartingAt (int startLine)
		{
			var startNode = GetNode (startLine);
			if (startNode == null)
				yield break;
			do {
				yield return startNode;
				startNode = startNode.GetPrevNode ();
			} while (startNode != null);
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
			var iter = startNode;
			iter = iter.GetNextNode ();
			TreeNode line;
			do {
				line = iter;
				iter = iter.GetNextNode ();
				RemoveLine (line);
			} while (line != endNode);
			ChangeLength (startNode, startNode.Length - charsRemoved + charsLeft, endNode.DelimiterLength);
		}

		//bool inInit;
		public void Initalize (string text)
		{
			Clear ();
			if (string.IsNullOrEmpty (text))
				return;
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
		class TreeNode : LineSegment, IRedBlackTreeNode
		{
			public override int Offset {
				get {
					var node = this;
					int offset = node.left != null ? node.left.TotalLength : 0;
					while (node.parent != null) {
						if (node == node.parent.right) {
							if (node.parent.left != null)
								offset += node.parent.left.TotalLength;
							if (node.parent != null)
								offset += node.parent.Length;
						}
						node = node.parent;
					}
					return offset;
				}
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

			#region IRedBlackTreeNode implementation
			public void UpdateAugmentedData ()
			{
				int count = 1;
				int totalLength = Length;
			
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

		TreeNode InsertAfter (TreeNode segment, int length, int delimiterLength)
		{
			var result = new TreeNode (length, delimiterLength) { StartSpan = segment.StartSpan };
			if (segment == null) {
				tree.Root = result;
				tree.Count = 1;
				return result;
			}
			
			tree.InsertAfter (segment, result);
			result.UpdateAugmentedData ();
			OnLineChanged (new LineEventArgs (result));
			return result;
		}

		public override string ToString ()
		{
			return tree.ToString ();
		}


		void ChangeLength (TreeNode line, int newLength)
		{
			ChangeLength (line, newLength, line.DelimiterLength);
		}

		void ChangeLength (TreeNode line, int newLength, int delimiterLength)
		{
			line.Length = newLength;
			line.DelimiterLength = delimiterLength;
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
					i -= node.Length;
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

		void RemoveLine (TreeNode line)
		{
			var parent = line.parent;
			tree.Remove (line);
			if (parent != null)
				parent.UpdateAugmentedData ();
			OnLineRemoved (new LineEventArgs (line));
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
