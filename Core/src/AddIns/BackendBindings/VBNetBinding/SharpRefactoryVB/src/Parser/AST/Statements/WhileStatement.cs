using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class WhileStatement : Statement
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
		
		public WhileStatement(Expression condition, Statement embeddedStatement)
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
			return String.Format("[WhileStatement: Condition={0}, EmbeddedStatement={1}]", 
			                     condition,
			                     embeddedStatement);
		}
	}
}
