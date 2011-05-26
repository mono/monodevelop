// 
// AddAnotherAccessor.cs
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
	public class AddAnotherAccessor : CSharpQuickFix
	{
		public AddAnotherAccessor ()
		{
			Description = GettextCatalog.GetString ("Adds second accessor to a property.");
		}
		
		PropertyDeclaration GetPropertyDeclaration (ParsedDocument doc, DomLocation loc)
		{
			var unit = doc.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			AstNode astNode = unit.GetNodeAt (loc.Line, loc.Column);
			return astNode.Parent as PropertyDeclaration;
		}
		
		public override string GetMenuText (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			return GettextCatalog.GetString ("Add another accessor");
		}
		
		public override bool IsValid (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var pDecl = GetPropertyDeclaration (document.ParsedDocument, loc);
			if (pDecl == null)
				return false;
			return pDecl.Setter.IsNull || pDecl.Getter.IsNull;
		}
		
		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var pDecl = GetPropertyDeclaration (document.ParsedDocument, loc);
			
			Accessor accessor = new Accessor () {
				Body = new BlockStatement {
					new ThrowStatement (new ObjectCreateExpression (new SimpleType ("System.NotImplementedException")))
				}
			};
			pDecl.AddChild (accessor, pDecl.Setter.IsNull ? PropertyDeclaration.SetterRole : PropertyDeclaration.GetterRole);
			
			var editor = document.Editor;
			var offset = editor.LocationToOffset (pDecl.RBraceToken.StartLocation.Line, pDecl.RBraceToken.StartLocation.Column - 1);
			string text = OutputNode (document.Dom, accessor, editor.GetLineIndent (pDecl.StartLocation.Line)) + editor.EolMarker;
			
			editor.Insert (offset, text);
			int i1 = text.IndexOf ("throw");
			int i2 = text.IndexOf (";") + 1;
			editor.Caret.Offset = offset + i2;
			editor.SetSelection (offset + i1, offset + i2);
		}
	}
}
