using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class TypeOfExpression : Expression
	{
		TypeReference type;
		
		Expression expression;
		
		public Expression Expression {
			get {
				return expression;
			}
			set {
				expression = value;
			}
		}
		
		public TypeReference Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public TypeOfExpression(Expression expression, TypeReference type)
		{
			this.type = type;
			this.expression = expression;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
