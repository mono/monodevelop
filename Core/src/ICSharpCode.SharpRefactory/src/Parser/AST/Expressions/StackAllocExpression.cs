using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class StackAllocExpression : Expression
	{
		TypeReference type;
		Expression expression;
		
		public TypeReference Type {
			get {
				return type;
			}
			set {
				type = value;
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
		public StackAllocExpression(TypeReference type, Expression expression)
		{
			this.type = type;
			this.expression = expression;
		}
		
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[StackAllocExpression: Type={0}, Expression={1}]",
			                     type,
			                     expression);
		}
	}
}
