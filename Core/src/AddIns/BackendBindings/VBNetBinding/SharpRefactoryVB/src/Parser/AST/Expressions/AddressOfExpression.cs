using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class AddressOfExpression : Expression
	{
		Expression procedure;
		
		public Expression Procedure {
			get {
				return procedure;
			}
			set {
				procedure = value;
			}
		}
		
		public AddressOfExpression(Expression Procedure)
		{
			this.Procedure = Procedure;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
