// 
// InsertAnonymousMethodSignature.cs
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
using System.Text;

namespace MonoDevelop.CSharp.ContextAction
{
	public class InsertAnonymousMethodSignature : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Insert signature");
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			IType type;
			return GetAnonymousMethodExpression (context, out type) != null;
		}
		
		protected override void Run (CSharpContext context)
		{
			IType type;
			var anonymousMethodExpression = GetAnonymousMethodExpression (context, out type);
			
			var delegateMethod = type.Methods.First ();
			
			var sb = new StringBuilder ("(");
			for (int k = 0; k < delegateMethod.Parameters.Count; k++) {
				if (k > 0)
					sb.Append (", ");
				var parameterType = context.Document.Dom.GetType (delegateMethod.Parameters [k].ReturnType);
				IReturnType returnType = parameterType != null ? new DomReturnType (parameterType) : delegateMethod.Parameters [k].ReturnType;
				sb.Append (context.OutputNode (ShortenTypeName (context.Document, returnType), 0));
				sb.Append (" ");
				sb.Append (delegateMethod.Parameters [k].Name);
			}
			sb.Append (")");
			
			context.DoInsert (context.Document.Editor.LocationToOffset (anonymousMethodExpression.DelegateToken.EndLocation.Line, anonymousMethodExpression.DelegateToken.EndLocation.Column), sb.ToString ());
		}
		
		
		AnonymousMethodExpression GetAnonymousMethodExpression (CSharpContext context, out IType delegateType)
		{
			delegateType = null;
			
			var anonymousMethodExpression = context.GetNode<AnonymousMethodExpression> ();
			if (anonymousMethodExpression == null || !anonymousMethodExpression.DelegateToken.Contains (context.Location.Line, context.Location.Column) || anonymousMethodExpression.HasParameterList)
				return null;
			MonoDevelop.Projects.Dom.ResolveResult resolveResult = null;
			var parent = anonymousMethodExpression.Parent;
			if (parent is AssignmentExpression) {
				resolveResult = context.Resolver.Resolve (((AssignmentExpression)parent).Left.ToString (), context.Location);
			} else if (parent is VariableDeclarationStatement) {
				resolveResult = context.Resolver.Resolve (((VariableDeclarationStatement)parent).Type.ToString (), context.Location);
			} else if (parent is InvocationExpression) {
				// TODO: handle invocations
			}
			
			if (resolveResult == null || resolveResult.ResolvedType == null)
				return null;
			delegateType = context.Document.Dom.GetType (resolveResult.ResolvedType);
			if (delegateType == null || delegateType.ClassType != ClassType.Delegate) 
				return null;
			
			return anonymousMethodExpression;
		}

	}
}

