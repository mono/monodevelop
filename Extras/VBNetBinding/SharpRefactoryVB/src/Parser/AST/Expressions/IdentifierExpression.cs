using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class IdentifierExpression : Expression
	{
		string identifier;
		
		public string Identifier {
			get {
				return identifier;
			}
			set {
				identifier = value;
			}
		}
		
		public IdentifierExpression(string identifier)
		{
			this.identifier = identifier;
		}
		
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[IdentifierExpression: Identifier={0}]",
			                     identifier);
		}
	}
}
