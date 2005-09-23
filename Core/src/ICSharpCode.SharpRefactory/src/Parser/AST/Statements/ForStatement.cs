using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class ForStatement : Statement
	{
		ArrayList  initializers; // EmbeddedStatement OR list of StatmentExpressions
		Expression condition;
		ArrayList  iterator;     // list of StatmentExpressions
		Statement  embeddedStatement;
		
		public ArrayList Initializers {
			get {
				return initializers;
			}
			set {
				initializers = value;
			}
		}
		
		public Expression Condition {
			get {
				return condition;
			}
			set {
				condition = value;
			}
		}
		
		public ArrayList Iterator {
			get {
				return iterator;
			}
			set {
				iterator = value;
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
		
		public ForStatement(ArrayList initializers, Expression condition, ArrayList iterator, Statement embeddedStatement)
		{
			this.initializers = initializers;
			this.condition = condition;
			this.iterator = iterator;
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
