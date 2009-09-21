// 
// GetContainingEmbeddedStatementVisitor.cs
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
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Refactoring.DeclareLocal
{
	public class GetContainingEmbeddedStatementVisitor : NodeTrackingAstVisitor
	{
		public Location LookupLocation {
			get;
			set;
		}
		
		Stack<Statement> statementStack = new Stack<Statement> ();
		
		public Statement ContainingStatement {
			get {
				return statementStack.FirstOrDefault ();
			}
		}
		
		protected override void BeginVisit(ICSharpCode.NRefactory.Ast.INode node) 
		{
			StatementWithEmbeddedStatement embedded = node as StatementWithEmbeddedStatement;
			if (embedded != null) {
				if (ContainsLocation (embedded.EmbeddedStatement))
					statementStack.Push (embedded);
			}
			// if ... else ... is a special case
			if (node is IfElseStatement) {
				IfElseStatement ifElse = (IfElseStatement)node;
				if (ifElse.TrueStatement.Count > 0 && ContainsLocation (ifElse.TrueStatement[0]) || ifElse.FalseStatement.Count > 0 && ContainsLocation (ifElse.FalseStatement[0]))
					statementStack.Push (ifElse);
			}
		}

		public bool ContainsLocation (Statement statement)
		{
			if (statement is BlockStatement)
				return false;
			return statement.StartLocation <= LookupLocation && (LookupLocation <= statement.EndLocation || statement.EndLocation.IsEmpty);
		}
	}
}
