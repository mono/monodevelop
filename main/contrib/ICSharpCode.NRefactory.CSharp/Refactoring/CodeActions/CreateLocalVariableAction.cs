// 
// CreateLocalVariable.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Create local variable", Description = "Creates a local variable for a undefined variable.")]
	public class CreateLocalVariableAction : ICodeActionProvider
	{
		public List<IdentifierExpression> GetUnresolvedArguments (RefactoringContext context)
		{
			var expressions = new List<IdentifierExpression> ();
			
			var invocation = GetInvocation (context);
			if (invocation != null) {
				foreach (var arg in invocation.Arguments) {
					IdentifierExpression identifier;
					if (arg is DirectionExpression) {
						identifier = ((DirectionExpression)arg).Expression as IdentifierExpression;
					} else if (arg is NamedArgumentExpression) {
						identifier = ((NamedArgumentExpression)arg).Expression as IdentifierExpression;
					} else {
						identifier = arg as IdentifierExpression;
					}
					if (identifier == null)
						continue;
						
					if (context.Resolve (identifier) == null && GuessType (context, identifier) != null)
						expressions.Insert (0, identifier);
				}
			}
			return expressions;
		}
		
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			if (GetUnresolvedArguments(context).Count <= 0) {
				yield break;
			}
			var identifier = CreateFieldAction.GetIdentifier(context);
			if (identifier == null) {
				yield break;
			}
			if (context.GetNode<Statement>() == null) {
				yield break;
			}
			if (!(context.Resolve(identifier).IsError && GuessType(context, identifier) != null)) {
				yield break;
			}
			yield return new CodeAction (context.TranslateString("Create local variable"), script => {
				var stmt = context.GetNode<Statement> ();
				var unresolvedArguments = GetUnresolvedArguments (context);
				if (unresolvedArguments.Count > 0) {
					foreach (var id in unresolvedArguments) {
						script.InsertBefore (stmt, GenerateLocalVariableDeclaration (context, id));
					}
					return;
				}
				
				script.InsertBefore (stmt, GenerateLocalVariableDeclaration (context, CreateFieldAction.GetIdentifier (context)));
			});
		}

		AstNode GenerateLocalVariableDeclaration (RefactoringContext context, IdentifierExpression identifier)
		{
			return new VariableDeclarationStatement () {
				Type = GuessType (context, identifier),
				Variables = { new VariableInitializer (identifier.Identifier) }
			};
		}
		
		InvocationExpression GetInvocation (RefactoringContext context)
		{
			return context.GetNode<InvocationExpression> ();
		}
		
		AstType GuessType (RefactoringContext context, IdentifierExpression identifier)
		{
			var type = CreateFieldAction.GuessType (context, identifier);
			if (type != null)
				return type;
			
			if (identifier != null && (identifier.Parent is InvocationExpression || identifier.Parent.Parent is InvocationExpression)) {
				var invocation = (identifier.Parent as InvocationExpression) ?? (identifier.Parent.Parent as InvocationExpression);
				var result = context.Resolve (invocation).Type.GetDelegateInvokeMethod ();
				if (result == null)
					return null;
				int i = 0;
				foreach (var arg in invocation.Arguments) {
					if (arg.Contains (identifier.StartLocation))
						break;
					i++;
				}
				if (result.Parameters.Count < i)
					return null;
				return context.CreateShortType (result.Parameters[i].Type);
			}
			return null;
		}
	}
}

