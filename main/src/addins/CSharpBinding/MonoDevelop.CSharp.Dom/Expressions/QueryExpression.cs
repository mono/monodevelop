// 
// QueryExpression.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Dom
{
	public class QueryExpressionFromClause : AbstractCSharpNode
	{
		public IReturnType Type {
			get {
				return (IReturnType)GetChildByRole (Roles.ReturnType);
			}
		}
		
		public string Identifier {
			get {
				return QueryIdentifier.Name;
			}
		}
		
		public Identifier QueryIdentifier {
			get {
				return (Identifier)GetChildByRole (Roles.Identifier);
			}
		}
		
		public INode Expression {
			get { return GetChildByRole (Roles.Expression); }
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionFromClause (this, data);
		}
	}
	
	public class QueryExpressionJoinClause : QueryExpressionFromClause
	{
		public const int OnExpressionRole     = 100;
		public const int EqualsExpressionRole = 101;
		public const int IntoIdentifierRole   = 102;
		
		public INode OnExpression {
			get {
				return GetChildByRole (OnExpressionRole);
			}
		}
		
		public INode EqualsExpression {
			get {
				return GetChildByRole (EqualsExpressionRole);
			}
		}
		
		public string IntoIdentifier {
			get {
				return IntoIdentifierIdentifier.Name;
			}
		}
		
		public Identifier IntoIdentifierIdentifier {
			get {
				return (Identifier)GetChildByRole (IntoIdentifierRole);
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionJoinClause (this, data);
		}
	}
	
	public class QueryExpressionGroupClause : AbstractCSharpNode
	{
		public const int ProjectionExpressionRole = 100;
		public const int GroupByExpressionRole    = 101;
		
		public INode Projection {
			get {
				return GetChildByRole (ProjectionExpressionRole);
			}
		}
		
		public INode GroupBy {
			get {
				return GetChildByRole (GroupByExpressionRole);
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionGroupClause (this, data);
		}
	}
	
	public class QueryExpressionLetClause : AbstractCSharpNode 
	{
		public string Identifier {
			get {
				return QueryIdentifier.Name;
			}
		}
		
		public Identifier QueryIdentifier {
			get {
				return (Identifier)GetChildByRole (Roles.Identifier);
			}
		}
		
		public INode Expression {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionLetClause (this, data);
		}
	}
	
	public class QueryExpressionOrderClause : AbstractCSharpNode
	{
		public const int OrderingRole = 100;
		
		public IEnumerable<QueryExpressionOrdering> Orderings {
			get {
				return GetChildrenByRole (OrderingRole).Cast<QueryExpressionOrdering> ();
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionOrderClause (this, data);
		}
	}
	
	public class QueryExpressionOrdering : AbstractCSharpNode
	{
		public QueryExpressionOrderingDirection Direction {
			get;
			set;
		}
		
		public INode Criteria {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionOrdering (this, data);
		}
	}
	
	public enum QueryExpressionOrderingDirection
	{
		Unknown,
		Ascending,
		Descending
	}
	
	public class QueryExpressionSelectClause : AbstractCSharpNode 
	{
		public INode Projection {
			get {
				return GetChildByRole (Roles.Expression);
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionSelectClause (this, data);
		}
	}
	
	public class QueryExpressionWhereClause : AbstractCSharpNode 
	{
		public INode Condition {
			get {
				return GetChildByRole (Roles.Condition);
			}
		}
		
		public override S AcceptVisitor<T, S> (ICSharpDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitQueryExpressionWhereClause (this, data);
		}
	}
	
}