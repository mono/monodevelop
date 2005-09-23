using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class PointerReferenceExpression : Expression
	{
		Expression expression;
		string     identifier;
		
		public Expression Expression {
			get {
				return expression;
			}
			set {
				expression = value;
			}
		}
		
		public string Identifier {
			get {
				return identifier;
			}
			set {
				identifier = value;
			}
		}
		
		public PointerReferenceExpression(Expression expression, string identifier)
		{
			this.expression = expression;
			this.identifier = identifier;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
