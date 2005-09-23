using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class BinaryOperatorExpression : Expression
	{
		Expression left;
		BinaryOperatorType op;
		Expression right;
		
		public Expression Left {
			get {
				return left;
			}
			set {
				left = value;
			}
		}
		
		public BinaryOperatorType Op {
			get {
				return op;
			}
			set {
				op = value;
			}
		}
		
		public Expression Right {
			get {
				return right;
			}
			set {
				right = value;
			}
		}
		
		public BinaryOperatorExpression(Expression left, BinaryOperatorType op, Expression right)
		{
			this.left  = left;
			this.op    = op;
			this.right = right;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[BinaryOperatorExpression: Op={0}, Left={1}, Right={2}]",
			                     op,
			                     left,
			                     right);
		}
	}
}
