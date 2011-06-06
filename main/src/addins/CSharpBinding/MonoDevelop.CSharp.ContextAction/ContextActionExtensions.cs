// 
// ContextActionExtensions.cs
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
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Dom;
using MonoDevelop.CSharp.Resolver;

namespace MonoDevelop.CSharp.ContextAction
{
	public static class ContextActionExtensions
	{
		public static int CalcIndentLevel (this MonoDevelop.Ide.Gui.Document doc, string indent)
		{
			int col = MonoDevelop.CSharp.Refactoring.CSharpNRefactoryASTProvider.GetColumn (indent, 0, doc.Editor.Options.TabSize);
			return System.Math.Max (0, col / doc.Editor.Options.TabSize);
		}
		
		public static NRefactoryResolver GetResolver (this MonoDevelop.Ide.Gui.Document doc)
		{
			return new NRefactoryResolver (doc.Dom, doc.CompilationUnit, doc.Editor, doc.FileName); 
		}
		
		public static ResolveResult Resolve (this AstNode node, MonoDevelop.Ide.Gui.Document doc)
		{
			return doc.GetResolver ().Resolve (node.ToString (), new DomLocation (node.StartLocation.Line, node.StartLocation.Column));
		}
		public static ResolveResult ResolveExpression (this AstNode node, MonoDevelop.Ide.Gui.Document doc, NRefactoryResolver resolver, DocumentLocation loc)
		{
			if (resolver == null)
				return null;
			resolver.SetupResolver (new DomLocation (loc.Line, loc.Column));
			return resolver.ResolveExpression (node.AcceptVisitor (exVisitor, null), new DomLocation (node.StartLocation.Line, node.StartLocation.Column));
		}
		static ExpressionVisitor exVisitor = new ExpressionVisitor ();
		class ExpressionVisitor : DepthFirstAstVisitor<object, ICSharpCode.OldNRefactory.Ast.Expression>
		{
			public override ICSharpCode.OldNRefactory.Ast.Expression VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
			{
				return new ICSharpCode.OldNRefactory.Ast.IdentifierExpression (identifierExpression.Identifier);
			}
			
			public override ICSharpCode.OldNRefactory.Ast.Expression VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, object data)
			{
				return new ICSharpCode.OldNRefactory.Ast.MemberReferenceExpression (memberReferenceExpression.Target.AcceptVisitor (this, null), memberReferenceExpression.MemberName);
			}
			
			public override ICSharpCode.OldNRefactory.Ast.Expression VisitThisReferenceExpression (ThisReferenceExpression thisReferenceExpression, object data)
			{
				return new ICSharpCode.OldNRefactory.Ast.ThisReferenceExpression ();
			}
			
			public override ICSharpCode.OldNRefactory.Ast.Expression VisitBaseReferenceExpression (BaseReferenceExpression baseReferenceExpression, object data)
			{
				return new ICSharpCode.OldNRefactory.Ast.BaseReferenceExpression ();
			}
			
			
		}
		
		public static void FormatText (this AstNode node, MonoDevelop.Ide.Gui.Document doc)
		{
			doc.UpdateParseDocument ();
			MonoDevelop.CSharp.Formatting.OnTheFlyFormatter.Format (doc, doc.Dom, new DomLocation (node.StartLocation.Line, node.StartLocation.Column));
		}
	}
}

