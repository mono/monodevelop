using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class EraseStatement : Statement
	{
		ArrayList expressions;
		
		public ArrayList Expressions
		{
			get {
				return expressions;
			} set {
				expressions = value;
			}
		}
		
		public EraseStatement(ArrayList expressions)
		{
			this.expressions = expressions;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
