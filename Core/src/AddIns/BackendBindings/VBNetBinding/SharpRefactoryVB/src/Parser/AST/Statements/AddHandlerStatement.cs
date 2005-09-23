using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class AddHandlerStatement : Statement
	{
		Expression eventExpression;
		Expression handlerExpression;
		
		public Expression EventExpression {
			get {
				return eventExpression;
			}
			set {
				eventExpression = value;
			}
		}
		public Expression HandlerExpression {
			get {
				return handlerExpression;
			}
			set {
				handlerExpression = value;
			}
		}
		
		public AddHandlerStatement(Expression eventExpression, Expression handlerExpression)
		{
			this.eventExpression = eventExpression;
			this.handlerExpression = handlerExpression;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
