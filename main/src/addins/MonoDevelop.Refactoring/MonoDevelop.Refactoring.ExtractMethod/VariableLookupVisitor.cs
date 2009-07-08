// 
// VariableLookupVisitor.cs
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
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Refactoring.ExtractMethod
{
	public class VariableLookupVisitor : AbstractAstVisitor
	{
		List<KeyValuePair <string, IReturnType>> unknownVariables = new List<KeyValuePair <string, IReturnType>> ();
		HashSet<string> knownVariables = new HashSet<string> ();

		public List<KeyValuePair <string, IReturnType>> UnknownVariables {
			get {
				return unknownVariables;
			}
		}
		
		IResolver resolver;
		DomLocation position;
		public VariableLookupVisitor (IResolver resolver, DomLocation position)
		{
			this.resolver = resolver;
			this.position = position;
		}
		
		public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
		{
			foreach (VariableDeclaration varDecl in localVariableDeclaration.Variables) {
				knownVariables.Add (varDecl.Name);
			}
			return base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
		{
			if (!knownVariables.Contains (identifierExpression.Identifier)) {
				foreach (var v in unknownVariables) {
					if (v.Key == identifierExpression.Identifier) {
						return null;
					}
				}
				ExpressionResult expressionResult = new ExpressionResult (identifierExpression.Identifier);

				ResolveResult result = resolver.Resolve (expressionResult, position);
				
				// result.ResolvedType == null may be true for namespace names or undeclared variables
				if (result != null && result.ResolvedType != null && !(result is MethodResolveResult) && !(result is NamespaceResolveResult))
					unknownVariables.Add (new KeyValuePair <string, IReturnType> (identifierExpression.Identifier, result.ResolvedType));
			}
			return base.VisitIdentifierExpression (identifierExpression, data);
		}
	}
}
