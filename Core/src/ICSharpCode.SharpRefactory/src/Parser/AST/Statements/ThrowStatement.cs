using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class ThrowStatement : Statement
	{
		Expression throwExpression;
		
		public Expression ThrowExpression {
			get {
				return throwExpression;
			}
			set {
				throwExpression = value;
			}
		}
		
		public ThrowStatement(Expression throwExpression)
		{
			this.throwExpression = throwExpression;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[ThrowStatement: ThrowExpression={0}]", 
			                     throwExpression);
		}
	}
}
