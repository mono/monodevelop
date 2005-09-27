using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.AST.VB;
using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB {
	
	public class NamedArgumentExpression : Expression
	{
		string parametername;
		Expression     expression;
		
		public string Parametername {
			get {
				return parametername;
			}
			set {
				parametername = value;
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
		
		public NamedArgumentExpression(string parametername, Expression expression)
		{
			this.parametername = parametername;
			this.expression = expression;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}

