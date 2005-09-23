using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class YieldStatement : Statement
	{
		Expression yieldExpression;
		
		public Expression YieldExpression {
			get { return yieldExpression; }
			set { yieldExpression = value; }
		}
		
		public YieldStatement (Expression yieldExpression)
		{
			this.yieldExpression = yieldExpression;
		}
		
		public override object AcceptVisitor (IASTVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}
		
		public override string ToString ()
		{
			return String.Format ("[YieldStatement: YieldExpression={0}]", yieldExpression);
		}
	}
}

