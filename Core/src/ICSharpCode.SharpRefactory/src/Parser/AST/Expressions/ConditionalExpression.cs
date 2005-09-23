using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST {
	
	public class ConditionalExpression : Expression
	{
		Expression testCondition;
		Expression trueExpression;
		Expression falseExpression;
		
		public Expression TestCondition {
			get {
				return testCondition;
			}
			set {
				testCondition = value;
			}
		}
		public Expression TrueExpression {
			get {
				return trueExpression;
			}
			set {
				trueExpression = value;
			}
		}
		public Expression FalseExpression {
			get {
				return falseExpression;
			}
			set {
				falseExpression = value;
			}
		}
		
		public ConditionalExpression(Expression testCondition, Expression trueExpression, Expression falseExpression)
		{
			this.testCondition = testCondition;
			this.trueExpression = trueExpression;
			this.falseExpression = falseExpression;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		public override string ToString()
		{
			return String.Format("[ConditionalExpression: TestCondition={0}, TrueExpression={1}, FalseExpression{2}]",
			                     testCondition,
			                     trueExpression,
			                     falseExpression);
		}
	}
}
