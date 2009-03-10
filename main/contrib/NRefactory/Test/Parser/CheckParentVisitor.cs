// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>
using NUnit.Framework;
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	/// <summary>
	/// Ensures that all nodes have the Parent property correctly set.
	/// </summary>
	public class CheckParentVisitor : NodeTrackingAstVisitor
	{
		Stack<INode> nodeStack = new Stack<INode>();
		
		public CheckParentVisitor()
		{
			nodeStack.Push(null);
		}
		
		protected override void BeginVisit(INode node)
		{
			nodeStack.Push(node);
		}
		
		protected override void EndVisit(INode node)
		{
			Assert.AreSame(node, nodeStack.Pop(), "nodeStack was corrupted!");
			Assert.AreSame(nodeStack.Peek(), node.Parent, "node " + node + " is missing parent: " + nodeStack.Peek());
		}
	}
}
