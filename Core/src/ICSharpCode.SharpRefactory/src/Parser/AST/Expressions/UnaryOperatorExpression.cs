using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class UnaryOperatorExpression : Expression
	{
		Expression        expression;
		UnaryOperatorType op;
		
		public Expression Expression {
			get {
				return expression;
			}
			set {
				expression = value;
			}
		}
		public UnaryOperatorType Op {
			get {
				return op;
			}
			set {
				op = value;
			}
		}
		
		public UnaryOperatorExpression(UnaryOperatorType op)
		{
			this.op    = op;
		}
		
		public UnaryOperatorExpression(Expression expression, UnaryOperatorType op)
		{
			this.expression  = expression;
			this.op    = op;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[UnaryOperatorExpression: Op={0}, Expression={1}]",
			                     op,
			                     expression);
		}
	}
}
