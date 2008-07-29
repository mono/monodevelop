// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public delegate void InformNode(INode node);
	
	public class NodeTracker
	{
		IAstVisitor callVisitor;
		
		public IAstVisitor CallVisitor {
			get {
				return callVisitor;
			}
		}
		
		public NodeTracker(IAstVisitor callVisitor)
		{
			this.callVisitor = callVisitor;
		}
		
		public void BeginNode(INode node)
		{
			if (NodeVisiting != null) {
				NodeVisiting(node);
			}
		}
		
		public void EndNode(INode node)
		{
			if (NodeVisited != null) {
				NodeVisited(node);
			}
		}
		
		public object TrackedVisit(INode node, object data)
		{
			BeginNode(node);
			object ret = node.AcceptVisitor(callVisitor, data);
			EndNode(node);
			return ret;
		}
		
		public object TrackedVisitChildren(INode node, object data)
		{
			foreach (INode child in node.Children) {
				TrackedVisit(child, data);
			}
			if (NodeChildrenVisited != null) {
				NodeChildrenVisited(node);
			}
			return data;
		}
		
		public event InformNode NodeVisiting;
		public event InformNode NodeChildrenVisited;
		public event InformNode NodeVisited;
	}
}
