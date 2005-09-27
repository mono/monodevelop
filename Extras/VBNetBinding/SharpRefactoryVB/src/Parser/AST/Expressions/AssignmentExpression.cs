using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class AssignmentExpression : Expression
	{
		Expression left;
		AssignmentOperatorType op;
		Expression right;
		
		public Expression Left {
			get {
				return left;
			}
			set {
				left = value;
			}
		}
		
		public AssignmentOperatorType Op {
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
		
		public AssignmentExpression(Expression left, AssignmentOperatorType op, Expression right)
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
			return String.Format("[AssignmentExpression: Op={0}, Left={1}, Right={2}]",
			                     op,
			                     left,
			                     right);
		}
	}
}
