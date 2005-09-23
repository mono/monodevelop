using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST {
	
	public class DirectionExpression : Expression
	{
		FieldDirection fieldDirection;
		Expression     expression;
		
		public FieldDirection FieldDirection {
			get {
				return fieldDirection;
			}
			set {
				fieldDirection = value;
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
		
		public DirectionExpression(FieldDirection fieldDirection, Expression expression)
		{
			this.fieldDirection = fieldDirection;
			this.expression = expression;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}

