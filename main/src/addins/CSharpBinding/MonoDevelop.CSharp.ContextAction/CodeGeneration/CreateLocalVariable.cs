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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Ide;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.ContextAction
{
	public class CreateLocalVariable : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			if (GetUnresolvedArguments (context).Count > 0)
				return GettextCatalog.GetString ("Create local variable declarations for arguments");
			
			var identifier = GetIdentifier (context);
			return string.Format (GettextCatalog.GetString ("Create local variable '{0}'"), identifier);
		}
		
		List<IdentifierExpression> GetUnresolvedArguments (CSharpContext context)
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
						
					if (context.IsUnresolved (identifier) && GuessType (context, identifier) != null)
						expressions.Add (identifier);
				}
			}
			return expressions;
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			if (GetUnresolvedArguments (context).Count > 0)
				return true;
			var identifier = GetIdentifier (context);
			if (identifier == null)
				return false;
			if (context.GetNode<Statement> () == null)
				return false;
			var result = context.Resolve (identifier);
			if (result == null || result.ResolvedType == null || string.IsNullOrEmpty (result.ResolvedType.DecoratedFullName))
				return GuessType (context, identifier) != null;
			return false;
		}
		
		protected override void Run (CSharpContext context)
		{
			var stmt = context.GetNode<Statement> ();
			var unresolvedArguments = GetUnresolvedArguments (context);
			if (unresolvedArguments.Count > 0) {
				foreach (var id in unresolvedArguments) {
					context.DoInsert (context.Document.Editor.LocationToOffset (stmt.StartLocation.Line, 1), 
						context.OutputNode (GenerateLocalVariableDeclaration (context, id), context.GetIndentLevel (stmt)) + context.Document.Editor.EolMarker);
				}
				return;
			}
			
			var identifier = GetIdentifier (context);
			
			context.DoInsert (context.Document.Editor.LocationToOffset (stmt.StartLocation.Line, 1), 
				context.OutputNode (GenerateLocalVariableDeclaration (context, identifier), context.GetIndentLevel (stmt)) + context.Document.Editor.EolMarker);
		}
		
		AstNode GenerateLocalVariableDeclaration (CSharpContext context, IdentifierExpression identifier)
		{
			return new VariableDeclarationStatement () {
				Type = GuessType (context, identifier),
				Variables = { new VariableInitializer (identifier.Identifier) }
			};
		}
		
		IdentifierExpression GetIdentifier (CSharpContext context)
		{
			return context.GetNode<IdentifierExpression> ();
		}
		
		InvocationExpression GetInvocation (CSharpContext context)
		{
			return context.GetNode<InvocationExpression> ();
		}

		AstType GuessType (CSharpContext context, IdentifierExpression identifier)
		{
			AstType type = CreateField.GuessType (context, identifier);
			if (type != null)
				return type;
			
			if (identifier != null && (identifier.Parent is InvocationExpression || identifier.Parent.Parent is InvocationExpression)) {
				var invocation = (identifier.Parent as InvocationExpression) ?? (identifier.Parent.Parent as InvocationExpression);
				var result = context.Resolve (invocation) as MethodResolveResult;
				if (result == null || result.ResolvedType == null || string.IsNullOrEmpty (result.ResolvedType.FullName))
					return null;
				int i = 0;
				foreach (var arg in invocation.Arguments) {
					if (arg.Contains (identifier.StartLocation))
						break;
					i++;
				}
				if (result.MostLikelyMethod == null || result.MostLikelyMethod.Parameters == null || result.MostLikelyMethod.Parameters.Count < i)
					return null;
				return ShortenTypeName (context.Document, result.MostLikelyMethod.Parameters[i].ReturnType);
			}
			return null;
		}
	}
}

