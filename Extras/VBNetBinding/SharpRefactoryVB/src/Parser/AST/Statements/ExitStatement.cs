using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ExitStatement : Statement
	{
		ExitType exitType;
		
		public ExitType ExitType {
			get {
				return exitType;
			}
			set {
				exitType = value;
			}
		}
		
		public ExitStatement(ExitType exitType)
		{
			this.exitType = exitType;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[ExitStatement]");
		}
	}
}
