// 
// TryCatchStatement.cs
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

using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.CSharp.Dom
{
	public class TryCatchStatement : DomNode
	{
		public const int TryKeywordRole     = 100;
		public const int FinallyKeywordRole = 101;
		public const int TryBlockRole       = 102;
		public const int FinallyBlockRole   = 103;
		public const int CatchClauseRole    = 104;
		
		public override NodeType NodeType {
			get {
				return NodeType.Statement;
			}
		}

		public CSharpTokenNode TryKeyword {
			get { return (CSharpTokenNode)GetChildByRole (TryKeywordRole) ?? CSharpTokenNode.Null; }
		}
		
		public CSharpTokenNode FinallyKeyword {
			get { return (CSharpTokenNode)GetChildByRole (FinallyKeywordRole) ?? CSharpTokenNode.Null; }
		}
		
		public BlockStatement TryBlock {
			get { return (BlockStatement)GetChildByRole (TryBlockRole) ?? BlockStatement.Null; }
		}
		
		public BlockStatement FinallyBlock {
			get { return (BlockStatement)GetChildByRole (FinallyBlockRole) ?? BlockStatement.Null; }
		}
		
		public IEnumerable<CatchClause> CatchClauses {
			get { return GetChildrenByRole (CatchClauseRole).Cast<CatchClause> (); }
		}
		
		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTryCatchStatement (this, data);
		}
	}
	
	public class CatchClause : DomNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public DomNode ReturnType {
			get { return GetChildByRole (Roles.ReturnType); }
		}
		
		public string VariableName {
			get { return VariableNameIdentifier.Name; }
		}

		public Identifier VariableNameIdentifier {
			get { return (Identifier)GetChildByRole (Roles.Identifier); }
		}
		
		public BlockStatement Block {
			get { return (BlockStatement)GetChildByRole (Roles.Body); }
		}
		
		public CSharpTokenNode LPar {
			get { return (CSharpTokenNode)GetChildByRole (Roles.LPar); }
		}
		
		public CSharpTokenNode RPar {
			get { return (CSharpTokenNode)GetChildByRole (Roles.RPar); }
		}
		
		public CSharpTokenNode CatchKeyword {
			get { return (CSharpTokenNode)GetChildByRole (Roles.Keyword); }
		}
		
		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCatchClause (this, data);
		}
	}
}
