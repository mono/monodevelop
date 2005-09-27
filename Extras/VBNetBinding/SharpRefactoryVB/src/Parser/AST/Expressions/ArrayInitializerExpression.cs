using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
{
	public class ArrayInitializerExpression : Expression
	{
		ArrayList     createExpressions = new ArrayList();
		
		public ArrayList CreateExpressions {
			get {
				return createExpressions;
			}
			set {
				createExpressions = value;
			}
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[ArrayInitializerExpression: CreateExpressions={0}]", 
			                     GetCollectionString(createExpressions));
		}
		
	}
}
