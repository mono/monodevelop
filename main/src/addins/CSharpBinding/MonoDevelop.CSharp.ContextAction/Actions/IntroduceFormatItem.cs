// 
// IntroduceFormatItem.cs
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

namespace MonoDevelop.CSharp.ContextAction
{
	public class IntroduceFormatItem : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Introduce format item");
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			if (!context.Document.Editor.IsSomethingSelected)
				return false;
			var pExpr = context.GetNode<PrimitiveExpression> ();
			if (pExpr == null || !(pExpr.Value is string))
				return false;
			if (pExpr.LiteralValue.StartsWith ("@"))
				return pExpr.StartLocation < new AstLocation (context.Location.Line, context.Location.Column - 1) &&
					new AstLocation (context.Location.Line, context.Location.Column + 1) < pExpr.EndLocation;
			return pExpr.StartLocation < new AstLocation (context.Location.Line, context.Location.Column) &&
				new AstLocation (context.Location.Line, context.Location.Column) < pExpr.EndLocation;
		}
		
		protected override void Run (CSharpContext context)
		{
			var pExpr = context.GetNode<PrimitiveExpression> ();
			
			var invocation = context.GetNode<InvocationExpression> ();
			if (invocation != null && invocation.Target.IsMatch (new MemberReferenceExpression (new TypeReferenceExpression (new PrimitiveType ("string")), "Format"))) {
				AddFormatCall (context, pExpr, invocation);
				return;
			}
			var arg = CreateArg (context);
			var newInvocation = new InvocationExpression (new MemberReferenceExpression (new TypeReferenceExpression (new PrimitiveType ("string")), "Format")) {
				Arguments = { CreateFormat (context, pExpr, 0), arg }
			};
			
			context.DoReplace (pExpr, newInvocation);
		}
		
		void AddFormatCall (CSharpContext context, PrimitiveExpression pExpr, InvocationExpression invocation)
		{
			var newInvocation = (InvocationExpression)invocation.Clone ();
			
			newInvocation.Arguments.First ().ReplaceWith (CreateFormat (context, pExpr, newInvocation.Arguments.Count () - 1));
			newInvocation.Arguments.Add (CreateArg (context));
			context.DoReplace (invocation, newInvocation);
		}

		PrimitiveExpression CreateArg (CSharpContext context)
		{
			return new PrimitiveExpression (context.Document.Editor.SelectedText);
		}
		
		PrimitiveExpression CreateFormat (CSharpContext context, PrimitiveExpression pExpr, int argumentNumber)
		{
			var start = context.Document.Editor.LocationToOffset (pExpr.StartLocation.Line, pExpr.StartLocation.Column);
			var end = context.Document.Editor.LocationToOffset (pExpr.EndLocation.Line, pExpr.EndLocation.Column);
			return new PrimitiveExpression ("", context.Document.Editor.GetTextBetween (start, context.Document.Editor.SelectionRange.Offset) + "{" + argumentNumber + "}" + context.Document.Editor.GetTextBetween (context.Document.Editor.SelectionRange.EndOffset, end));
		}
	}
}

