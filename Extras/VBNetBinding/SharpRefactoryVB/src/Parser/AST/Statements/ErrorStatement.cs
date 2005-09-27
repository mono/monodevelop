using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ErrorStatement : Statement
	{
		Expression expression;
		
		public ErrorStatement(Expression expression)
		{
			this.expression = expression;
		}
		
		public Expression Expression
		{
			get {
				return expression;
			}set {
				expression = value;
			}
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
