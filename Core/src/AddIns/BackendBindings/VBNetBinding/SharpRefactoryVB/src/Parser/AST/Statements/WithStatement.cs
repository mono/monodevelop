using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class WithStatement : Statement
	{
		Expression withExpression;
		BlockStatement body = null;
		
		public Expression WithExpression {
			get {
				return withExpression;
			}
		}
		
		public BlockStatement Body {
			get {
				return body;
			}
			set {
				body = value;
			}
		}
		
		public WithStatement(Expression withExpression)
		{
			this.withExpression = withExpression;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		public override string ToString()
		{
			return String.Format("[WithStatment: WidthExpression={0}, Body={1}]", 
			                     withExpression,
			                     body);
		}
	}
}
