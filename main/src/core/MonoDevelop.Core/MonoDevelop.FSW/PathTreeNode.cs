using System;
using System.Collections.Generic;
using System.IO;

namespace MonoDevelop.FSW
{
	sealed class PathTreeNode
	{
		public PathTreeNode Parent { get; set; }
		public PathTreeNode FirstChild { get; set; }
		public PathTreeNode Previous { get; set; }
		public PathTreeNode Next { get; set; }
		public int ChildrenCount { get; set; }

		readonly string fullPath;
		readonly int start;
		readonly int length;

		readonly List<object> ids = new List<object> ();
		internal void RegisterId (object id) => ids.Add (id);
		internal bool UnregisterId (object id) => ids.Remove (id);

		public bool IsLive => ids.Count != 0;
		public ReadOnlySpan<char> GetPath () => fullPath.AsSpan (0, start + length);
		internal ReadOnlySpan<char> GetSegment () => fullPath.AsSpan (start, length);
		internal string Segment => fullPath.Substring (start, length);

		internal PathTreeNode LastChild {
			get {
				var child = FirstChild;
				while (child != null && child.Next != null)
					child = child.Next;
				return child;
			}
		}

		public PathTreeNode (string fullPath, int start, int length)
		{
			this.fullPath = fullPath;
			this.start = start;
			this.length = length;
		}

		internal static (PathTreeNode root, PathTreeNode leaf) CreateSubTree (string path, int start)
		{
			PathTreeNode lastNode = null, rootNode = null;

			while (start < path.Length) {
				var nextSep = path.IndexOf (Path.DirectorySeparatorChar, start);
				int length = nextSep == -1 ? path.Length - start : nextSep - start;

				if (length != 0) {
					var node = new PathTreeNode (path, start, length);

					if (lastNode != null) {
						lastNode.FirstChild = node;
						node.Parent = lastNode;
						lastNode.ChildrenCount = 1;
					} else
						rootNode = node;

					lastNode = node;
				}

				start = start + length + 1;
			}

			return (rootNode, lastNode);
		}
	}
}