// 
// HeightTree.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor.Utils;

namespace Mono.TextEditor
{
	/// <summary>
	/// The height tree stores the heights of lines and provides a performant y <--> lineNumber conversion.
	/// It takes care of message bubble heights and the height of folded sections.
	/// </summary>
	public class HeightTree
	{
		// TODO: Add support for line word wrap to the text editor - with the height tree this is possible.
		
		RedBlackTree<HeightNode> tree = new RedBlackTree<HeightNode> ();
		TextEditor editor;
		
		public double TotalHeight {
			get {
				return tree.Root.totalHeight;
			}
		}
		
		public HeightTree (TextEditor editor)
		{
			this.editor = editor;
		}
		
		public void Rebuild ()
		{
			tree.Count = 1;
			double h = editor.LineCount * editor.LineHeight;
			tree.Root = new HeightNode () {
				height = h,
				totalHeight = h,
				count = editor.LineCount
			};
			
			foreach (LineSegment extendedTextMarkerLine in editor.Document.LinesWithExtendingTextMarkers) {
				int lineNumber = editor.OffsetToLineNumber (extendedTextMarkerLine.Offset);
				double height = editor.GetLineHeight (extendedTextMarkerLine);
				SetLineHeight (lineNumber, height);
			}
			
			foreach (var segment in editor.Document.FoldedSegments) {
				int start = editor.OffsetToLineNumber (segment.StartLine.Offset);
				int end = editor.OffsetToLineNumber (segment.EndLine.Offset);
				Fold (start, end - start);
			}
		}
		
		public void SetLineHeight (int lineNumber, double height)
		{
			var node = GetNodeByLine (lineNumber);
			if (node == null)
				throw new Exception ("No node for line number " + lineNumber + " found. (maxLine=" + tree.Root.totalCount + ")");
			
			int nodeStartLine = node.GetLineNumber ();
			int remainingLineCount;
			
			if (nodeStartLine == lineNumber) {
				double nodeHeight = node.height;
				remainingLineCount = node.count - 1;
				ChangeHeight (node, 1, height);
				if (remainingLineCount > 0) {
					InsertAfter (node, new HeightNode () {
						count = remainingLineCount,
						height = editor.LineHeight * remainingLineCount
					});
				}
			} else {
				int newLineCount = lineNumber - nodeStartLine;
				remainingLineCount = node.count - newLineCount - 1;
				if (newLineCount != node.count) {
					double newHeight = editor.LineHeight * newLineCount;
					ChangeHeight (node, newLineCount, newHeight);
				}
				
				var newNode = new HeightNode () {
					count = 1,
					height = height
				};
				InsertAfter (node, newNode);
				
				if (remainingLineCount > 0) {
					InsertAfter (newNode, new HeightNode () {
						count = remainingLineCount,
						height = editor.LineHeight * remainingLineCount
					});
				}
			}
		}
		
		public int YToLineNumber (double y)
		{
			var node = GetNodeByY (y);
			if (node == null)
				return 0;
			int lineOffset = 0;
			if (node.foldLevel == 0) {
				double delta = y - node.GetY ();
				lineOffset = (int)(node.count * delta / node.height);
			}
			return node.GetLineNumber () + lineOffset;
		}
		
		public void Fold (int lineNumber, int count)
		{
			lineNumber++;
			for (int i = lineNumber; i < lineNumber + count; i++) {
				SetLineHeight (i, editor.GetLineHeight (i));
			}
			
			for (int i = lineNumber; i < lineNumber + count; i++) {
				var node = GetNodeByLine (i);
				node.foldLevel++;
				node.UpdateAugmentedData ();
			}
		}

		public void Unfold (int lineNumber, int count)
		{
			lineNumber++;
			for (int i = lineNumber; i < lineNumber + count; i++) {
				SetLineHeight (i, editor.GetLineHeight (i));
			}
			
			for (int i = lineNumber; i < lineNumber + count; i++) {
				var node = GetNodeByLine (i);
				node.foldLevel--;
				if (node.foldLevel < 0)
					throw new InvalidOperationException ("foldlevel < 0");
				node.UpdateAugmentedData ();
			}
		}

		public double LineNumberToY (int lineNumber)
		{
			var node = GetNodeByLine (lineNumber);
			if (node == null)
				return 0;
			var n = node;
			while (n != null && n.foldLevel > 0) {
				lineNumber -= n.count;
				n = n.GetPrevNode ();
				node = n;
			}
			double result = node.Left != null ? ((HeightNode)node.Left).totalHeight : 0;
			if (node.count > 1) {
				int lineDelta = lineNumber - node.GetLineNumber ();
				if (lineDelta > 0)
					result += node.height * lineDelta / (double)node.count;
			}
			
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null)
						result += node.parent.left.totalHeight;
					if (node.parent.foldLevel == 0)
						result += node.parent.height;
				}
				node = node.parent;
			}
			return result;
		}

		void InsertAfter (HeightNode node, HeightNode newNode)
		{
			if (node.right == null) {
				tree.InsertRight (node, newNode);
			} else {
				tree.InsertLeft (node.GetOuterLeft (), newNode);
			}
			newNode.UpdateAugmentedData ();
		}
		
		void ChangeHeight (HeightNode node, int newCount, double newHeight)
		{
			node.count = newCount;
			node.height = newHeight;
			node.UpdateAugmentedData ();
		}

		public HeightNode GetNodeByLine (int lineNumber)
		{
			var node = tree.Root;
			int i = lineNumber - 1;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && i < node.left.totalCount) {
					node = node.left;
				} else {
					if (node.left != null)
						i -= node.left.totalCount;
					i -= node.count;
					if (i < 0)
						return node;
					node = node.right;
				}
			}
		}
		
		public HeightNode GetNodeByY (double y)
		{
			var node = tree.Root;
			double h = y;
			while (true) {
				if (node == null)
					return null;
				if (node.left != null && h < node.left.totalHeight) {
					node = node.left;
				} else {
					if (node.left != null)
						h -= node.left.totalHeight;
					
					if (node.foldLevel == 0) {
						h -= node.height;
						if (h < 0) {
							return node;
						}
					}
					/*
					} else {
						double deltaH = 0;
						var n = node.GetNextNode ();
						while (n != null && n.foldLevel > 0) {
							deltaH += n.height;
							n = n.GetNextNode ();
						}
					
						if (h - deltaH < 0) {
							return node;
						}
					}*/
					node = node.right;
				}
			}
		}
		
		public class HeightNode : IRedBlackTreeNode
		{
			public double totalHeight;
			public double height;
			
			public int totalCount;
			public int count = 1;
			
			public int foldLevel;
			
			public int GetLineNumber ()
			{
				int lineNumber = left != null ? left.totalCount : 0;
				var node = this;
				while (node.parent != null) {
					if (node == node.parent.right) {
						if (node.parent.left != null)
							lineNumber += node.parent.left.totalCount;
						lineNumber += node.parent.count;
					}
					
					node = node.parent;
				}
				return lineNumber + 1;
			}

			public double GetY ()
			{
				double result = left != null ? left.totalHeight : 0;
				var node = this;
				while (node.parent != null) {
					if (node == node.parent.right) {
						if (node.parent.left != null)
							result += node.parent.left.totalHeight;
						if (node.parent.foldLevel == 0)
							result += node.parent.height;
					}
					node = node.parent;
				}
				return result;
			}
			
			public override string ToString ()
			{
				return string.Format ("[HeightNode: totalHeight={0}, height={1}, totalCount={2}, count={3}, foldLevel={4}]", totalHeight, height, totalCount, count, foldLevel);
			}
			
			#region IRedBlackTreeNode implementation
			public void UpdateAugmentedData ()
			{
				double newHeight = foldLevel == 0 ? height : 0;
				int newCount = count;
				
				if (left != null) {
					newHeight += left.totalHeight;
					newCount += left.totalCount;
				}
				
				if (right != null) {
					newHeight += right.totalHeight;
					newCount += right.totalCount;
				}
				
				if (newHeight != totalHeight || newCount != totalCount) {
					this.totalHeight = newHeight;
					this.totalCount = newCount;
					if (Parent != null)
						Parent.UpdateAugmentedData ();
				}
			}
			public HeightNode parent;
			public Mono.TextEditor.Utils.IRedBlackTreeNode Parent {
				get {
					return parent;
				}
				set {
					parent = (HeightNode)value;
				}
			}
			
			public HeightNode left;
			public Mono.TextEditor.Utils.IRedBlackTreeNode Left {
				get {
					return left;
				}
				set {
					left = (HeightNode)value;
				}
			}
			
			public HeightNode right;
			public Mono.TextEditor.Utils.IRedBlackTreeNode Right {
				get {
					return right;
				}
				set {
					right = (HeightNode)value;
				}
			}

			public RedBlackColor Color {
				get;
				set;
			}
			#endregion
			
		}

	}
}

