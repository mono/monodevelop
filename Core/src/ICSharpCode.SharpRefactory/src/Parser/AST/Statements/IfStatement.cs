using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class IfStatement : Statement
	{
		Expression condition;
		Statement  embeddedStatement;
		
		public Expression Condition {
			get {
				return condition;
			}
			set {
				condition = value;
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
		public IfStatement(Expression condition, Statement embeddedStatement)
		{
			this.condition = condition;
			this.embeddedStatement = embeddedStatement;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
