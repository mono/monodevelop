using System;
using System.Collections;

using ICSharpCode.SharpRefactory.Parser.VB;

namespace ICSharpCode.SharpRefactory.Parser.AST.VB 
{
	public class IfStatement : Statement
	{
		Expression condition;
		Statement  embeddedElseStatement;
		Statement  embeddedStatement;
		ArrayList elseIfStatements;
		
		public ArrayList ElseIfStatements
		{
			get {
				return elseIfStatements;
			}
			set {
				elseIfStatements = value;
			}
		}
		
		public Statement EmbeddedElseStatement
		{
			get {
				return embeddedElseStatement;
			}
			set {
				embeddedElseStatement = value;
			}
		}
		
		public Expression Condition
		{
			get {
				return condition;
			}
			set {
				condition = value;
			}
		}
		
		public Statement EmbeddedStatement
		{
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public IfStatement(Expression condition, Statement embeddedStatement)
		{
			this.condition = condition;
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
	
	public class ElseIfSection : Statement
	{
		Expression condition;
		Statement  embeddedStatement;
		
		public Expression Condition
		{
			get {
				return condition;
			}
			set {
				condition = value;
			}
		}
		public Statement EmbeddedStatement
		{
			get {
				return embeddedStatement;
			}
			set {
				embeddedStatement = value;
			}
		}
		
		public ElseIfSection(Expression condition, Statement embeddedStatement)
		{
			this.condition = condition;
			this.embeddedStatement = embeddedStatement;
		}
		
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
	
	public class SimpleIfStatement : Statement
	{
		Expression condition;
		ArrayList statements;
		ArrayList elseStatements;
		
		public ArrayList ElseStatements
		{
			get {
				return elseStatements;
			}
			set {
				elseStatements = value;
			}
		}
		
		public Expression Condition {
			get {
				return condition;
			}
			set {
				condition = value;
			}
		}
		public ArrayList Statements {
			get {
				return statements;
			}
			set {
				statements = value;
			}
		}
		
		public SimpleIfStatement(Expression condition)
		{
			this.condition = condition;
		}
	
		public override object AcceptVisitor(IASTVisitor visitor, object data)
		{
			return visitor.Visit(this, data);
		}
	}
}

