// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision: 4482 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Sets the parent property on all nodes in the tree.
	/// </summary>
	public class SetParentVisitor : NodeTrackingAstVisitor
	{
		Stack<INode> nodeStack = new Stack<INode>();
		
		public SetParentVisitor()
		{
			nodeStack.Push(null);
		}
		
		protected override void BeginVisit(INode node)
		{
			node.Parent = nodeStack.Peek();
			nodeStack.Push(node);
		}
		
		protected override void EndVisit(INode node)
		{
			nodeStack.Pop();
		}
	}
}
