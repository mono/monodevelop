using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class CheckedStatement : Statement
	{
		Statement block;
		
		public Statement Block {
			get {
				return block;
			}
			set {
				block = value;
			}
		}
		
		public CheckedStatement(Statement block)
		{
			this.block = block;
		}
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[CheckedStatement: Block={0}]", 
			                     block);
		}
	}
}
