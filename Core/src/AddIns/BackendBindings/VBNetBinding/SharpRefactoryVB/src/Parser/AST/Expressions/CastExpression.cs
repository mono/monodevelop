using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class CastExpression : Expression
	{
		TypeReference castTo;
		Expression expression;
		bool       isSpecializedCast = false;
		
		
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
		
		public bool IsSpecializedCast {
			get {
				return isSpecializedCast;
			}
			set {
				isSpecializedCast = value;
			}
		}
		
		
		public CastExpression(TypeReference castTo, Expression expression)
		{
			this.castTo = castTo;
			this.expression = expression;
		}
		
		public CastExpression(TypeReference castTo, Expression expression, bool isSpecializedCast)
		{
			this.castTo = castTo;
			this.expression = expression;
			this.isSpecializedCast = isSpecializedCast;
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
