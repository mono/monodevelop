using System;
using System.Collections;

namespace ICSharpCode.SharpRefactory.Parser.AST 
{
	public class TryCatchStatement : Statement
	{
		Statement statementBlock;
		ArrayList catchClauses;
		Statement finallyBlock;
		
		public Statement StatementBlock {
			get {
				return statementBlock;
			}
			set {
				statementBlock = value;
			}
		}
		
		public ArrayList CatchClauses {
			get {
				return catchClauses;
			}
			set {
				catchClauses = value;
			}
		}
		
		public Statement FinallyBlock {
			get {
				return finallyBlock;
			}
			set {
				finallyBlock = value;
			}
		}
		
		public TryCatchStatement(Statement statementBlock, ArrayList catchClauses, Statement finallyBlock)
		{
			this.statementBlock = statementBlock;
			this.catchClauses = catchClauses;
			this.finallyBlock = finallyBlock;
		}
		
		public TryCatchStatement(Statement statementBlock, ArrayList catchClauses)
		{
			this.statementBlock = statementBlock;
			this.catchClauses = catchClauses;
			this.finallyBlock = null;
		}
		
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[TryCatchStatement: StatementBlock={0}, CatchClauses={1}, FinallyBlock={2}]",
			                     statementBlock,
			                     GetCollectionString(catchClauses),
			                     finallyBlock);
		}
	}
	
	public class CatchClause
	{
		string type;
		string variableName;
		Statement       statementBlock;
		
		public string Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public string VariableName {
			get {
				return variableName;
			}
			set {
				variableName = value;
			}
		}
		
		public Statement StatementBlock {
			get {
				return statementBlock;
			}
			set {
				statementBlock = value;
			}
		}
		
		public CatchClause(string type, string variableName, Statement statementBlock)
		{
			this.type = type;
			this.variableName = variableName;
			this.statementBlock = statementBlock;
		}
		
		public CatchClause(Statement statementBlock)
		{
			this.type         = null;
			this.variableName = null;
			this.statementBlock = statementBlock;
		}
		
		public override string ToString()
		{
			return String.Format("[CatchClause: Type={0}, VariableName={1}, StatementBlock={2}]", 
			                     type,
			                     variableName,
			                     statementBlock);
		}
	}
}
