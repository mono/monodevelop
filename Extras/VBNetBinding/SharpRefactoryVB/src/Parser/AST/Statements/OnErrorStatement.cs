using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class OnErrorStatement : Statement
	{
		Statement embeddedStatement;
		
		public Statement EmbeddedStatement
		{
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public OnErrorStatement(Statement embeddedStatement)
		{
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
