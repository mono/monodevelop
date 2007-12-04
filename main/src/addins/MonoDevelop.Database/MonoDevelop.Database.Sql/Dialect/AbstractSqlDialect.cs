//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Text;
using System.Data;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public abstract class AbstractSqlDialect : ISqlDialect
	{
		public abstract string QuoteIdentifier (string identifier);
		
		public abstract string MarkAsParameter (string identifier);
		
		public virtual string GetSql (IStatement statement)
		{
			if (statement == null)
				throw new ArgumentNullException ("statement");
			
			Type type = statement.GetType ();
			if (type == typeof (SelectStatement))
				return GetStatementSql (statement as SelectStatement);
			else if (type == typeof (InsertStatement))
				return GetStatementSql (statement as InsertStatement);	
			else if (type == typeof (UpdateStatement))
				return GetStatementSql (statement as UpdateStatement);
			else if (type == typeof (DeleteStatement))
				return GetStatementSql (statement as DeleteStatement);
			else if (type == typeof (DropStatement))
				return GetStatementSql (statement as DropStatement);		
			else if (type == typeof (TruncateStatement))
				return GetStatementSql (statement as TruncateStatement);
			else
				throw new NotImplementedException (type.FullName);
		}
		
		public virtual string GetSql (IClause clause)
		{
			if (clause == null)
				throw new ArgumentNullException ("clause");
			
			Type type = clause.GetType ();
			if (type == typeof (FromSelectClause))
				return GetClauseSql (clause as FromSelectClause);
			else if (type == typeof (FromTableClause))
				return GetClauseSql (clause as FromTableClause);
			else if (type == typeof (WhereClause))
				return GetClauseSql (clause as WhereClause);
			else if (type == typeof (HavingClause))
				return GetClauseSql (clause as HavingClause);
			else if (type == typeof (JoinClause))
				return GetClauseSql (clause as JoinClause);
			else if (type == typeof (OrderByClause))
				return GetClauseSql (clause as OrderByClause);		
			else if (type == typeof (GroupByClause))
				return GetClauseSql (clause as GroupByClause);
			else if (type == typeof (UnionClause))
				return GetClauseSql (clause as UnionClause);
			else
				throw new NotImplementedException (type.FullName);
		}
		
		public virtual string GetSql (IExpression expr)
		{
			if (expr == null)
				throw new ArgumentNullException ("expr");
			
			Type type = expr.GetType ();
			if (type == typeof (AliasedIdentifierExpression))
				return GetExpressionSql (expr as AliasedIdentifierExpression);
			else if (type == typeof (IdentifierExpression))
				return GetExpressionSql (expr as IdentifierExpression);	
			else if (type == typeof (BooleanExpression))
				return GetExpressionSql (expr as BooleanExpression);
			else if (type == typeof (OperatorExpression))
				return GetExpressionSql (expr as OperatorExpression);
			else if (type == typeof (ParameterExpression))
				return GetExpressionSql (expr as ParameterExpression);
			else
				throw new NotImplementedException (type.FullName);
		}
		
		public virtual string GetSql (ILiteral literal)
		{
			if (literal == null)
				throw new ArgumentNullException ("literal");
			
			Type type = literal.GetType ();
			if (type == typeof (StringLiteral))
				return GetLiteralSql (literal as StringLiteral);
			else if (type == typeof (NumericLiteral))
				return GetLiteralSql (literal as NumericLiteral);	
			else if (type == typeof (BooleanLiteral))
				return GetLiteralSql (literal as BooleanLiteral);
			else if (type == typeof (NullLiteral))
				return GetLiteralSql (literal as NullLiteral);
			else if (type == typeof (TrueLiteral))
				return GetLiteralSql (literal as TrueLiteral);
			else if (type == typeof (FalseLiteral))
				return GetLiteralSql (literal as FalseLiteral);
			else if (type == typeof (HexLiteral))
				return GetLiteralSql (literal as HexLiteral);
			else if (type == typeof (BitLiteral))
				return GetLiteralSql (literal as BitLiteral);
			else
				throw new NotImplementedException (type.FullName);
		}

		protected virtual string GetStatementSql (SelectStatement statement)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("SELECT ");
			bool first = true;
			foreach (IdentifierExpression expr in statement.Columns) {
				if (first)
					first = false;
				else
					sb.Append (',');
				
				sb.Append (GetSql (expr));
			}
			
			sb.Append (' ');
			sb.Append (Environment.NewLine);
			sb.Append (GetSql (statement.From));
			
			if (statement.Where != null) {
				sb.Append (GetSql (statement.Where));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			if (statement.OrderBy != null) {
				sb.Append (GetSql (statement.OrderBy));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			if (statement.GroupBy != null) {
				sb.Append (GetSql (statement.GroupBy));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			if (statement.Having != null) {
				sb.Append (GetSql (statement.Having));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			if (statement.Union != null) {
				sb.Append (GetSql (statement.Union));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			if (statement.Join != null) {
				sb.Append (GetSql (statement.Join));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			sb.Append (';');
			
			return sb.ToString ();
		}
		
		protected virtual string GetStatementSql (InsertStatement statement)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("INSERT INTO ");
			sb.Append (GetSql (statement.Identifier));

			if (statement.Columns.Count > 0) {
				sb.Append (Environment.NewLine);
				sb.Append (" (");
				int columnCount = statement.Columns.Count;
				for (int i=0; i<columnCount; i++) {
					sb.Append (GetSql (statement.Columns[i]));
					sb.Append (i == (columnCount - 1) ? ", " : "");
				}
				sb.Append (")");
			}
			
			sb.Append (Environment.NewLine);
			sb.Append (" VALUES ");
			
			int rowCount = statement.Values.Count;
			for (int j=0; j<rowCount; j++) {
				sb.Append ("(");
				List<IExpression> expr = statement.Values[j];
				
				int columnCount = expr.Count;
				for (int i=0; i<columnCount; i++) {
					sb.Append (GetSql (expr[i]));
					sb.Append (i == (columnCount - 1) ? ", " : "");
				}

				sb.Append (")");
				sb.Append (j == (rowCount - 1) ? "," + Environment.NewLine : "");
			}

			sb.Append (';');
			return sb.ToString ();
		}
		
		protected virtual string GetStatementSql (UpdateStatement statement)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("UPDATE ");
			sb.Append (GetSql (statement.Identifier));
			sb.Append (' ');
			sb.Append (Environment.NewLine);
			
			sb.Append ("SET ");
			
			int columnCount = statement.Columns.Count;
			for (int i=0; i<columnCount; i++) {
				OperatorExpression expr = new OperatorExpression (statement.Columns[i], Operator.Equals, statement.Values[i]);
				sb.Append (expr);
				sb.Append (i == (columnCount - 1) ? ", " : "");
			}
			
			if (statement.Where != null) {
				sb.Append (GetSql (statement.Where));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			sb.Append (';');
			return sb.ToString ();
		}
		
		protected virtual string GetStatementSql (DeleteStatement statement)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("DELETE ");
			sb.Append (GetSql (statement.From));
			
			if (statement.Where != null) {
				sb.Append (GetSql (statement.Where));
				sb.Append (' ');
				sb.Append (Environment.NewLine);
			}
			
			sb.Append (';');
			
			return sb.ToString ();
		}
		
		protected virtual string GetStatementSql (DropStatement statement)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("DROP ");
			switch (statement.DropType) {
				case DropStatementType.Database:
					sb.Append ("DATABASE ");
					break;
				case DropStatementType.Table:
					sb.Append ("TABLE ");
					break;
				case DropStatementType.Index:
					sb.Append ("INDEX ");
					break;
				case DropStatementType.Procedure:
					sb.Append ("PROCEDURE ");
					break;
				case DropStatementType.View:
					sb.Append ("VIEW ");
					break;
			}
			
			sb.Append (GetSql (statement.Identifier));
			sb.Append (';');
			
			return sb.ToString ();
		}
		
		protected virtual string GetStatementSql (TruncateStatement statement)
		{
			StringBuilder sb = new StringBuilder ();
			
			sb.Append ("TRUNCATE ");
			sb.Append (GetSql (statement.Identifier));
			sb.Append (';');
			
			return sb.ToString ();
		}
		
		protected virtual string GetClauseSql (FromSelectClause clause)
		{
			return String.Concat ("FROM ", GetStatementSql (clause.Source));
		}
		
		protected virtual string GetClauseSql (FromTableClause clause)
		{
			return String.Concat ("FROM ", GetExpressionSql (clause.Source));
		}
		
		protected virtual string GetClauseSql (WhereClause clause)
		{
			return String.Concat ("WHERE ", GetSql (clause.Condition));
		}
		
		protected virtual string GetClauseSql (OrderByClause clause)
		{
			throw new NotImplementedException ();
		}
		
		protected virtual string GetClauseSql (GroupByClause clause)
		{
			throw new NotImplementedException ();
		}
		
		protected virtual string GetClauseSql (UnionClause clause)
		{
			throw new NotImplementedException ();
		}
		
		protected virtual string GetClauseSql (JoinClause clause)
		{
			throw new NotImplementedException ();
		}
		
		protected virtual string GetClauseSql (HavingClause clause)
		{
			throw new NotImplementedException ();
		}
		
		protected virtual string GetLiteralSql (StringLiteral literal)
		{
			return String.Concat ("'", literal.Value, "'");
		}
		
		protected virtual string GetLiteralSql (NumericLiteral literal)
		{
			return literal.Value;
		}
		
		protected virtual string GetLiteralSql (NullLiteral literal)
		{
			return "NULL";
		}
		
		protected virtual string GetLiteralSql (TrueLiteral literal)
		{
			return "TRUE";
		}
		
		protected virtual string GetLiteralSql (FalseLiteral literal)
		{
			return "FALSE";
		}
		
		protected virtual string GetLiteralSql (HexLiteral literal)
		{
			// X'ABCD'
			return String.Concat ("X'", literal.Value, "'");
		}
		
		protected virtual string GetLiteralSql (BitLiteral literal)
		{
			// B'01011010'
			return String.Concat ("B'", literal.Value, "'");
		}
		
		protected virtual string GetLiteralSql (BooleanLiteral literal)
		{
			if (literal.Value)
				return "TRUE";
			return "FALSE";
		}
		
		protected virtual string GetExpressionSql (AliasedIdentifierExpression expr)
		{
			return String.Concat (GetIdentifierName (expr.Name), " AS ", GetIdentifierName (expr.Alias));
		}
		
		protected virtual string GetExpressionSql (IdentifierExpression expr)
		{
			return GetIdentifierName (expr.Name);
		}
		
		protected virtual string GetIdentifierName (string identifier)
		{
			return QuoteIdentifier (identifier);
		}
		
		protected virtual string GetExpressionSql (BooleanExpression expr)
		{
			return String.Concat ("(", GetSql (expr.Left), " ",
				GetOperatorSql (expr.Operator), " ",
				GetSql (expr.Right), ")");
		}
		
		protected virtual string GetExpressionSql (OperatorExpression expr)
		{
			return String.Concat ("(", GetSql (expr.Left), " ",
				GetOperatorSql (expr.Operator), " ",
				GetSql (expr.Right), ")");
		}
		
		protected virtual string GetExpressionSql (ParameterExpression expr)
		{
			return MarkAsParameter (expr.Name);
		}
		
		protected virtual string GetOperatorSql (Operator op)
		{
			switch (op) {
				case Operator.Equals:
					return "=";
				case Operator.NotEqual:
					return "!=";
				case Operator.GreaterThanOrEqual:
					return ">=";
				case Operator.GreaterThan:
					return ">";
				case Operator.LessThanOrEqual:
					return "<=";
				case Operator.LessThan:
					return "<";
				case Operator.Plus:
					return "+";
				case Operator.Minus:
					return "-";
				case Operator.Divide:
					return "/";
				case Operator.Multiply:
					return "*";
				case Operator.Modus:
					return "%";
				case Operator.Is:
					return "IS";
				case Operator.IsNot:
					return "IS NOT";
				case Operator.In:
					return "IN";
				case Operator.Like:
					return "LIKE";
				default:
					throw new NotImplementedException ();
			}
		}
		
		protected virtual string GetOperatorSql (BooleanOperator op)
		{
			bool not = ((op & BooleanOperator.Not) == BooleanOperator.Not);
			bool and = ((op & BooleanOperator.And) == BooleanOperator.And);
			bool or = ((op & BooleanOperator.Or) == BooleanOperator.Or);
			
			if (and)
				if (not) return "AND NOT"; else return "AND";
			else if (or)
				if (not) return "OR NOT"; else return "OR";
			else return "NOT";
		}
	}
}