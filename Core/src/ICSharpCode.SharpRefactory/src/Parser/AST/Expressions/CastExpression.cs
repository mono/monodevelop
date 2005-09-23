using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class CastExpression : Expression
	{
		TypeReference castTo;
		Expression expression;
		
		
		public TypeReference CastTo {
			get {
				return castTo;
			}
			set {
				castTo = value;
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
		
		public CastExpression(TypeReference castTo)
		{
			this.castTo = castTo;
		}
		
		public CastExpression(TypeReference castTo, Expression expression)
		{
			this.castTo = castTo;
			this.expression = expression;
		}
		
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[CastExpression: CastTo={0}, Expression={1}]",
			                     castTo,
			                     expression);
		}
	}
}
