// 
// ExtractMethodAstTransformer.cs
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
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using System.Diagnostics;

namespace MonoDevelop.Refactoring.ExtractMethod
{
	class ExtractMethodAstTransformer : AbstractAstTransformer
	{
		List<VariableDescriptor> variablesToGenerate;
		Stack<BlockStatement> blocks = new Stack<BlockStatement> ();
		Stack<List<INode>> curStatementList = new Stack<List<INode>> ();
		
		public ExtractMethodAstTransformer (List<VariableDescriptor> variablesToGenerate)
		{
			this.variablesToGenerate = variablesToGenerate;
		}
		
		public override object VisitBlockStatement (BlockStatement blockStatement, object data)
		{
			blocks.Push (blockStatement);
			List<INode> statements = new List<INode> ();
			curStatementList.Push (statements);
			for (int i = 0; i < blockStatement.Children.Count; i++) {
				INode o = blockStatement.Children[i];
				Debug.Assert (o != null);
				nodeStack.Push (o);
				o.AcceptVisitor (this, data);
				o = nodeStack.Pop ();
				if (o == null) {
				} else {
					statements.Add (o);
				}
			}
			blockStatement.Children = statements;
			blocks.Pop ();
			return null;
		}
		
		public override object VisitLocalVariableDeclaration (ICSharpCode.NRefactory.Ast.LocalVariableDeclaration localVariableDeclaration, object data)
		{
			object result = base.VisitLocalVariableDeclaration (localVariableDeclaration, data);
			if (localVariableDeclaration.Variables.Count == 0)
				RemoveCurrentNode ();
			return result;
		}

		public override object VisitVariableDeclaration (ICSharpCode.NRefactory.Ast.VariableDeclaration variableDeclaration, object data)
		{
			if (variablesToGenerate.Any (v => v.Name == variableDeclaration.Name)) {
				RemoveCurrentNode ();
				if (variableDeclaration.Initializer != null && !variableDeclaration.Initializer.IsNull) {
				//	BlockStatement block = blocks.Peek ();
					curStatementList.Peek ().Add (new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (variableDeclaration.Name), AssignmentOperatorType.Assign, variableDeclaration.Initializer)));
				}
				
				return null;
			}
			
			return base.VisitVariableDeclaration (variableDeclaration, data);
		}


	}
}
