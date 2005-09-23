using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class LockStatement : Statement
	{
		Expression lockExpression;
		Statement  embeddedStatement;
		
		public Expression LockExpression {
			get {
				return lockExpression;
			}
			set {
				lockExpression = value;
			}
		}
		public Statement EmbeddedStatement {
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		public LockStatement(Expression lockExpression, Statement embeddedStatement)
		{
			this.lockExpression = lockExpression;
			this.embeddedStatement = embeddedStatement;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[LockStatement: LockExpression={0}, EmbeddedStatement={1}]", 
			                     lockExpression,
			                     embeddedStatement);
		}
	}
}
