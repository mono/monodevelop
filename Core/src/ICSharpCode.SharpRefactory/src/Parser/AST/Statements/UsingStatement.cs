using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class UsingStatement : Statement
	{
		Statement  usingStatement;
		Statement  embeddedStatement;
		
		public Statement UsingStmnt {
			get {
				return usingStatement;
			}
			set {
				usingStatement = value;
			}
		}
		
		
		public Statement EmbeddedStatement {
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public UsingStatement(Statement usingStatement, Statement embeddedStatement)
		{
			this.usingStatement = usingStatement;
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[UsingStatement: UsingStmnt={0}, EmbeddedStatement={1}]", 
			                     usingStatement,
			                     embeddedStatement);
		}

	}
}
