using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ForeachStatement : Statement
	{
		LoopControlVariableExpression loopControlVariable;
		Expression    expression;
		Statement     embeddedStatement;
		Expression    element;
		
		public LoopControlVariableExpression LoopControlVariable
		{
			get {
				return loopControlVariable;
			}
			set {
				loopControlVariable = value;
			}
		}
		
		public Expression Element {
			get {
				return element;
			}
			set {
				element = value;
			}
		}
		
		public Expression Expression {
			get {
				return expression;
			}
			set {
				expression = value;
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
		
		public ForeachStatement(LoopControlVariableExpression loopControlVariable , Expression expression, Statement embeddedStatement, Expression element)
		{
			this.loopControlVariable = loopControlVariable;
			this.expression        = expression;
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
