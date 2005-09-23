using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class BlockStatement : Statement
	{
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[BlockStatement: Children={0}]", 
			                     GetCollectionString(base.Children));
		}
	}
}
