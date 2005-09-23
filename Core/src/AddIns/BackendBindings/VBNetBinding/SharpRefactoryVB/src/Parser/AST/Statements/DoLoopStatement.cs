using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class DoLoopStatement : Statement
	{
		Expression condition;
		Statement  embeddedStatement;
		ConditionType conditionType;
		ConditionPosition conditionPosition;
		
		public ConditionPosition ConditionPosition {
			get {
				return conditionPosition;
			}
			set {
				conditionPosition = value;
			}
		}
		
		public ConditionType ConditionType {
			get {
				return conditionType;
			}
			set {
				conditionType = value;
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
		
		public Statement EmbeddedStatement {
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public DoLoopStatement(Expression condition, Statement embeddedStatement, ConditionType conditionType, ConditionPosition conditionPosition)
		{
			this.condition = condition;
			this.embeddedStatement = embeddedStatement;
			this.conditionType = conditionType;
			this.conditionPosition = conditionPosition;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
