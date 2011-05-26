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

namespace MonoDevelop.CSharp.QuickFix
{
	public class FlipOperatorArguments : CSharpQuickFix
	{
		public FlipOperatorArguments ()
		{
			Description = GettextCatalog.GetString ("Swaps left and right arguments.");
		}
		
		public override string GetMenuText (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var binop = GetBinaryOperatorExpression (document.ParsedDocument, loc);
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

		BinaryOperatorExpression GetBinaryOperatorExpression (ParsedDocument doc, DomLocation loc)
		{
			var unit = doc.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			var node = unit.GetNodeAt (loc.Line, loc.Column);
			
			if (node is CSharpTokenNode)
				node = node.Parent;
			
			var result = node as BinaryOperatorExpression;
			if (result == null || (result.Operator != BinaryOperatorType.Equality && result.Operator != BinaryOperatorType.InEquality))
				return null;
			return result;
		}
		
		public override bool IsValid (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var binop = GetBinaryOperatorExpression (document.ParsedDocument, loc);
			return binop != null;
		}
		
		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var binop = GetBinaryOperatorExpression (document.ParsedDocument, loc);
			
			int leftOffset = document.Editor.LocationToOffset (binop.Left.StartLocation.Line, binop.Left.StartLocation.Column);
			int leftEndOffset = document.Editor.LocationToOffset (binop.Left.EndLocation.Line, binop.Left.EndLocation.Column);
			
			int rightOffset = document.Editor.LocationToOffset (binop.Right.StartLocation.Line, binop.Right.StartLocation.Column);
			int rightEndOffset = document.Editor.LocationToOffset (binop.Right.EndLocation.Line, binop.Right.EndLocation.Column);
			
			string rightText = document.Editor.GetTextBetween (rightOffset, rightEndOffset);
			string leftText = document.Editor.GetTextBetween (leftOffset, leftEndOffset);
			document.Editor.Document.BeginAtomicUndo ();
			document.Editor.Replace (rightOffset, rightEndOffset - rightOffset, leftText);
			document.Editor.Replace (leftOffset, leftEndOffset - leftOffset, rightText);
			document.Editor.Document.EndAtomicUndo ();
			document.Editor.Document.CommitUpdateAll ();
		}
	}
}

