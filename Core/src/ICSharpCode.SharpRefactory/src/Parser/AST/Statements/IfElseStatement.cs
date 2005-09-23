using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class IfElseStatement : Statement
	{
		Expression condition;
		Statement  embeddedStatement;
		Statement  embeddedElseStatement;
		
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
		
		public Statement EmbeddedElseStatement {
			get {
				return embeddedElseStatement;
			}
			set {
				embeddedElseStatement = value;
			}
		}
		
		public IfElseStatement(Expression condition, Statement embeddedStatement, Statement embeddedElseStatement)
		{
			this.condition = condition;
			this.embeddedStatement = embeddedStatement;
			this.embeddedElseStatement = embeddedElseStatement;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
