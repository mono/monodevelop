//
// PathTree.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Core;

namespace MonoDevelop.FSW
{
	class PathTree
	{
		internal readonly PathTreeNode rootNode;

		public PathTree ()
		{
			rootNode = new PathTreeNode ("", 0, 1);
			if (!Platform.IsWindows) {
				rootNode.FirstChild = new PathTreeNode ("/", 0, 0) {
					Parent = rootNode,
				};
				rootNode.ChildrenCount = 1;
			}
		}

		public PathTreeNode FindNode (string path)
		{
			TryFind(path, out var result, out _, out _, out _);
			return result;
		}

		public IEnumerable<PathTreeNode> Normalize (int maxLeafs)
		{
			// We want to use an algorithm similar to BFS by using the following logic:
			// If the node is live, we can return it
			// Otherwise, we keep looking for live nodes in a node's children.
			// If the amount of children a node has exceeds the maximum amount of leaves
			// we want, we just return the node itself, even if it's not live.

			var queue = new Queue<PathTreeNode>(maxLeafs);

			int yielded = 0;
			var child = rootNode.FirstChild;
			while (child != null)
			{
				if (child.IsLive)
				{
					yielded++;
					yield return child;
				} else
					queue.Enqueue(child);

				child = child.Next;
			}
			if (queue.Count == 0)
				yield break;

			while (yielded <= maxLeafs && queue.Count != 0)
			{
				var node = queue.Dequeue();

				if (node.ChildrenCount + yielded - 1 < maxLeafs)
				{
					child = node.FirstChild;
					while (child != null)
					{
						if (child.IsLive)
						{
							yielded++;
							yield return child;
						}
						else
							queue.Enqueue(child);
						child = child.Next;
					}
				}
				else
				{
					yielded++;
					yield return node;
				}
			}
		}

		bool TryFind (string path, out PathTreeNode result, out PathTreeNode parent, out PathTreeNode previousNode, out int lastIndex)
		{
			lastIndex = 0;

			parent = rootNode;
			var currentNode = parent.FirstChild;
			previousNode = null;

			while (currentNode != null)
			{
				int currentIndex = path.IndexOf(Path.DirectorySeparatorChar, lastIndex);
				int comparisonResult = string.Compare(currentNode.FullPath, currentNode.Start, path, lastIndex, currentNode.Length);

				// We need to insert in this node's position.
				if (comparisonResult > 0)
					break;

				// Keep searching.
				if (comparisonResult < 0)
				{
					previousNode = currentNode;
					currentNode = currentNode.Next;
					continue;
				}

				// We found this segment in the tree.
				lastIndex = currentIndex + 1;

				// We found the node already, register the ID.
				if (currentIndex == -1 || lastIndex == path.Length)
				{
					result = currentNode;
					return true;
				}

				// We go to the first child of this segment and repeat the algorithm.
				parent = currentNode;
				previousNode = null;
				currentNode = parent.FirstChild;
			}

			result = null;
			return false;
		}

		public PathTreeNode AddNode (string path, object id)
		{
			if (TryFind(path, out var result, out var parent, out var previousNode, out var lastIndex))
			{
				result.RegisterId(id);
				return result;
			}

			// At this point, we need to create a new node.
			var (first, leaf) = PathTreeNode.CreateSubTree(path, lastIndex);
			if (id != null)
				leaf.RegisterId(id);

			InsertNode(first, parent, previousNode);

			return leaf;
		}

		public PathTreeNode RemoveNode(string path, object id)
		{
			if (!TryFind (path, out PathTreeNode result, out var parent, out var previousNode, out _))
				return null;

			if (result.UnregisterId(id) && !result.IsLive)
			{
				var nodeToRemove = result;
				var lastToRemove = Platform.IsWindows ? rootNode : rootNode.FirstChild;

				while (nodeToRemove != lastToRemove && IsDeadSubtree (nodeToRemove)) {
					parent.ChildrenCount -= 1;

					if (parent.FirstChild == nodeToRemove)
						parent.FirstChild = nodeToRemove.Next;

					if (nodeToRemove.Previous != null)
						nodeToRemove.Previous.Next = nodeToRemove.Next;
					
					if (nodeToRemove.Next != null)
						nodeToRemove.Next.Previous = nodeToRemove.Previous;

					nodeToRemove.Next = null;
					nodeToRemove.Previous = null;
					nodeToRemove.Parent = null;

					nodeToRemove = parent;
					parent = nodeToRemove.Parent;
				}
			}

			return result;
		}

		bool IsDeadSubtree (PathTreeNode node)
		{
			// We do a DFS here, looking for any live node in a tree.
			// We know that leaves are live, so DFS works better here.
			var stack = new Stack<PathTreeNode> ();
			stack.Push (node);

			while (stack.Count != 0) {
				node = stack.Pop ();
				if (node.IsLive)
					return false;

				var child = node.FirstChild;

				while (child != null) {
					stack.Push (child);
					child = child.Next;
				}
			}
			return true;
		}

		void InsertNode(PathTreeNode node, PathTreeNode parentNode, PathTreeNode previousNode)
		{
			parentNode.ChildrenCount += 1;

			node.Parent = parentNode;
			if (previousNode == null)
			{
				// We're inserting at the beginning.
				var insertBefore = parentNode.FirstChild;

				node.Next = insertBefore;
				if (insertBefore != null)
					insertBefore.Previous = node;
				parentNode.FirstChild = node;
				return;
			}

			// We are appending inbetween other nodes
			var next = previousNode.Next;
			previousNode.Next = node;
			node.Previous = previousNode;

			node.Next = next;
			if (next != null)
				next.Previous = node;
		}
	}
}