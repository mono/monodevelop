using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB
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
		TypeReference type;
		string variableName;
		Statement       statementBlock;
		Expression condition;
		
		public Expression Condition {
			get {
				return condition;
			}
			set {
				condition = value;
			}
		}
		
		public TypeReference Type {
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
		
		public CatchClause(TypeReference type, string variableName, Statement statementBlock, Expression condition)
		{
			this.type = type;
			this.variableName = variableName;
			this.statementBlock = statementBlock;
			this.condition = condition;
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
