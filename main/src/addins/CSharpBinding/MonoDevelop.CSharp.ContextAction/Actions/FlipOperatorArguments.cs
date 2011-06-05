// 
// FlipOperatorArguments.cs
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
	public class FlipOperatorArguments : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			var binop = GetBinaryOperatorExpression (context);
			string op;
			switch (binop.Operator) {
			case BinaryOperatorType.Equality:
				op = "==";
				break;
			case BinaryOperatorType.InEquality:
				op = "!=";
				break;
			default:
				throw new InvalidOperationException ();
			}
			return string.Format (GettextCatalog.GetString ("Flip '{0}' operator arguments"), op);
		}

		BinaryOperatorExpression GetBinaryOperatorExpression (CSharpContext context)
		{
			var node = context.GetNode ();
			
			if (node is CSharpTokenNode)
				node = node.Parent;
			
			var result = node as BinaryOperatorExpression;
			if (result == null || (result.Operator != BinaryOperatorType.Equality && result.Operator != BinaryOperatorType.InEquality))
				return null;
			return result;
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			return GetBinaryOperatorExpression (context) != null;
		}
		
		protected override void Run (CSharpContext context)
		{
			var binop = GetBinaryOperatorExpression (context);
			
			context.DoReplace (binop.Left, binop.Right);
			context.DoReplace (binop.Right, binop.Left);
		}
	}
}

