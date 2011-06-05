// 
// SplitDeclarationAndAssignment.cs
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
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;

namespace MonoDevelop.CSharp.ContextAction
{
	// MISSING: Using statments.
	public class SplitDeclarationAndAssignment : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Split declaration and assignment");
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			MonoDevelop.Projects.Dom.ResolveResult resolveResult;
			return GetVariableDeclarationStatement (context, out resolveResult) != null;
		}
		
		protected override void Run (CSharpContext context)
		{
			MonoDevelop.Projects.Dom.ResolveResult resolveResult;
			var varDecl = GetVariableDeclarationStatement (context, out resolveResult);
			
			var assign = new AssignmentExpression (new IdentifierExpression (varDecl.Variables.First ().Name), AssignmentOperatorType.Assign, varDecl.Variables.First ().Initializer.Clone ());
			
			if (varDecl.Parent is ForStatement) {
				context.DoReplace (varDecl, assign);
			} else {
				context.DoReplace (varDecl, new ExpressionStatement (assign));
			}
			
			var newVarDecl = (VariableDeclarationStatement)varDecl.Clone ();
			
			if (newVarDecl.Type.IsMatch (new SimpleType ("var")))
				newVarDecl.Type = ShortenTypeName (context.Document, resolveResult.ResolvedType);
			
			newVarDecl.Variables.First ().Initializer = Expression.Null;
			string text = context.OutputNode (newVarDecl, context.GetIndentLevel (varDecl)) + context.Document.Editor.EolMarker;
			
			context.DoInsert (context.Document.Editor.LocationToOffset (varDecl.StartLocation.Line, 1), text);
		}
		
		VariableDeclarationStatement GetVariableDeclarationStatement (CSharpContext context, out MonoDevelop.Projects.Dom.ResolveResult resolveResult)
		{
			var result = context.GetNode<VariableDeclarationStatement> ();
			if (result != null && result.Variables.Count == 1 && !result.Variables.First ().Initializer.IsNull && result.Variables.First ().NameToken.Contains (context.Location.Line, context.Location.Column)) {
				var resolver = context.Resolver;
				resolveResult = resolver.Resolve (result.Variables.First ().Initializer.ToString (), context.Location);
				if (resolveResult == null || resolveResult.ResolvedType == null || string.IsNullOrEmpty (resolveResult.ResolvedType.FullName))
					return null;
				return result;
			}
			resolveResult = null;
			return null;
		}
	}
}

