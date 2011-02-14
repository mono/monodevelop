using System;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Ast.Utils
{
	class TreeTraversal
	{
		public static IEnumerable<AstNode> PreOrder (IEnumerable<AstNode> nodes, Func<AstNode,IEnumerable<AstNode>> selectChildren)
		{
			foreach (var node in nodes) {
				yield return node;
				foreach (var p in PreOrder (selectChildren (node), selectChildren))
					yield return p;
			}
		}

		public static IEnumerable<AstNode> PreOrder (AstNode node)
		{
			AstNode current = node.FirstChild;
			if (current == null)
				yield break;
			while (true) {
				yield return current;
				if (current.FirstChild != null) {
					current = current.FirstChild;
					continue;
				}
				if (current.NextSibling != null) {
					current = current.NextSibling;
					continue;
				}
				while (current.Parent != null && current.Parent != node) {
					current = current.Parent;
					if (current.NextSibling != null) {
						current = current.NextSibling;
						continue;
					}
				}
				break;
			}
		}
	}
}
