using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents the base class of expressions and statements.
	/// </summary>
	public abstract class JSAstNode
	{
		static JSAstNode[] emptyNodeList = new JSAstNode[0];

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public virtual IEnumerable<JSAstNode> ChildNodes {
			get { return emptyNodeList; }
		}

		/// <summary>
		/// Determines if this node or any of this node's children are of the given type.
		/// </summary>
		/// <typeparam name="T"> The type of AstNode to search for. </typeparam>
		/// <returns> <c>true</c> if this node or any of this node's children are of the given
		/// type; <c>false</c> otherwise. </returns>
		public bool ContainsNodeOfType<T> () where T : JSAstNode
		{
			if (this is T)
				return true;
			foreach (var child in this.ChildNodes)
				if (child.ContainsNodeOfType<T> ())
					return true;
			return false;
		}
	}
}
