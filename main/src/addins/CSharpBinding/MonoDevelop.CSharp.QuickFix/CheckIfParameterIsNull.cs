// 
// CheckIfParameterIsNullQuickFix.cs
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

namespace MonoDevelop.CSharp.QuickFix
{
	public class CheckIfParameterIsNull : CSharpQuickFix
	{
		public CheckIfParameterIsNull ()
		{
			MenuText = GettextCatalog.GetString ("Check if parameter is null");
			Description = GettextCatalog.GetString ("Checks function parameter is not null.");
		}
		
		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var pDecl = GetParameterDeclaration (document.ParsedDocument, loc);
			
			var bodyStatement = pDecl.Parent.GetChildByRole (AstNode.Roles.Body);
			
			var statement = new IfElseStatement () {
				Condition = new BinaryOperatorExpression (new IdentifierExpression (pDecl.Name), BinaryOperatorType.Equality, new NullReferenceExpression ()),
				TrueStatement = new ThrowStatement (new ObjectCreateExpression (new SimpleType ("System.ArgumentNullException"), new PrimitiveExpression (pDecl.Name)))
			};
			var editor = document.Editor;
			var offset = editor.LocationToOffset (bodyStatement.StartLocation.Line, bodyStatement.StartLocation.Column + 1);
			
			string text = editor.EolMarker + OutputNode (document.Dom, statement, editor.GetLineIndent (bodyStatement.StartLocation.Line));
			
			editor.Insert (offset, text);
		}
		
		public bool HasNullCheck (ParameterDeclaration pDecl)
		{
			var visitor = new CheckNullVisitor (pDecl);
			pDecl.Parent.AcceptVisitor (visitor, null);
			return visitor.ContainsNullCheck;
		}
		
		class CheckNullVisitor : DepthFirstAstVisitor<object, object>
		{
			ParameterDeclaration pDecl;
			
			public bool ContainsNullCheck { 
				get;
				set;
			}
			
			public CheckNullVisitor (ParameterDeclaration pDecl)
			{
				this.pDecl = pDecl;
			}
			
			public override object VisitIfElseStatement (IfElseStatement ifElseStatement, object data)
			{
				if (ifElseStatement.Condition is BinaryOperatorExpression) {
					var binOp = ifElseStatement.Condition as BinaryOperatorExpression;
					if ((binOp.Operator == BinaryOperatorType.Equality || binOp.Operator == BinaryOperatorType.InEquality) &&
						binOp.Left.IsMatch (new IdentifierExpression (pDecl.Name)) && binOp.Right.IsMatch (new NullReferenceExpression ()) || 
						binOp.Right.IsMatch (new IdentifierExpression (pDecl.Name)) && binOp.Left.IsMatch (new NullReferenceExpression ())) {
						ContainsNullCheck = true;
					}
				}
				
				return base.VisitIfElseStatement (ifElseStatement, data);
			}
		}
		
		ParameterDeclaration GetParameterDeclaration (ParsedDocument doc, DomLocation loc)
		{
			var unit = doc.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			AstNode astNode = unit.GetNodeAt (loc.Line, loc.Column);
			while (astNode != null && !(astNode is ParameterDeclaration)) {
				astNode = astNode.Parent;
			}
			
			return astNode as ParameterDeclaration;
		}
		
		public override bool IsValid (ParsedDocument doc, DomLocation loc)
		{
			var pDecl = GetParameterDeclaration (doc, loc);
			if (pDecl == null)
				return false;
			
			if (pDecl.Type is PrimitiveType) {
				return (((PrimitiveType)pDecl.Type).Keyword == "object" || ((PrimitiveType)pDecl.Type).Keyword == "string") && !HasNullCheck (pDecl);
			}
			
			// todo: check for structs
			return !HasNullCheck (pDecl);
		}
	}
}

