using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class DoWhileStatement : Statement
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
		public DoWhileStatement(Expression condition, Statement embeddedStatement)
		{
			this.condition = condition;
			this.embeddedStatement = embeddedStatement;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[DoWhileStatement: Condition={0}, EmbeddedStatement={1}]", 
			                     condition,
			                     embeddedStatement);
		}
	}
}
