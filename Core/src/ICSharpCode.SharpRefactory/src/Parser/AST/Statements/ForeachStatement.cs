using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class ForeachStatement : Statement
	{
		TypeReference typeReference;
		string        variableName;
		Expression    expression;
		Statement     embeddedStatement;
		
		public TypeReference TypeReference {
			get {
				return typeReference;
			}
			set {
				typeReference = value;
			}
		}
		
		public string VariableName {
			get {
				return variableName;
			}
			set {
				variableName = value;
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
		
		public Statement EmbeddedStatement {
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public ForeachStatement(TypeReference typeReference, string variableName, Expression expression, Statement embeddedStatement)
		{
			this.typeReference     = typeReference;
			this.variableName      = variableName;
			this.expression        = expression;
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
