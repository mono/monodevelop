using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class GetTypeExpression : Expression
	{
		TypeReference type;
		
		public TypeReference Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public GetTypeExpression(TypeReference type)
		{
			this.type = type;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}
