using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST
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
		
		public IdentifierExpression(string identifier, System.Drawing.Point location)
		{
			this.identifier = identifier;
			this.StartLocation = location;
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
