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

namespace MonoDevelop.CSharp.ContextAction
{
	public class AddAnotherAccessor : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Add another accessor");
		}
		
		PropertyDeclaration GetPropertyDeclaration (CSharpContext context)
		{
			var astNode = context.GetNode ();
			if (astNode == null)
				return null;
			return astNode.Parent as PropertyDeclaration;
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			var pDecl = GetPropertyDeclaration (context);
			if (pDecl == null)
				return false;
			var type = pDecl.Parent as TypeDeclaration;
			if (type != null && type.ClassType == ICSharpCode.NRefactory.TypeSystem.ClassType.Interface)
				return false;
			
			return pDecl.Setter.IsNull || pDecl.Getter.IsNull;
		}
		
		protected override void Run (CSharpContext context)
		{
			var pDecl = GetPropertyDeclaration (context);
			
			Statement accessorStatement = null;
			
			if (pDecl.Setter.IsNull && !pDecl.Getter.IsNull) {
				var field = RemoveBackingStore.ScanGetter (context, pDecl);
				if (field != null) 
					accessorStatement = new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (field.Name), AssignmentOperatorType.Assign, new IdentifierExpression ("value")));
			}
			
			if (!pDecl.Setter.IsNull && pDecl.Getter.IsNull) {
				var field = RemoveBackingStore.ScanSetter (context, pDecl);
				if (field != null) 
					accessorStatement = new ReturnStatement (new IdentifierExpression (field.Name));
			}
			
			if (accessorStatement == null)
				accessorStatement = new ThrowStatement (new ObjectCreateExpression (new SimpleType ("System.NotImplementedException")));
			
			Accessor accessor = new Accessor () {
				Body = new BlockStatement {
					accessorStatement 
				}
			};
			
			pDecl.AddChild (accessor, pDecl.Setter.IsNull ? PropertyDeclaration.SetterRole : PropertyDeclaration.GetterRole);
			
			var editor = context.Document.Editor;
			var offset = editor.LocationToOffset (pDecl.RBraceToken.StartLocation.Line, pDecl.RBraceToken.StartLocation.Column - 1);
			string text = context.OutputNode (accessor, context.GetIndentLevel (pDecl) + 1) + editor.EolMarker;
			
			editor.Document.BeginAtomicUndo ();
			
			editor.Insert (offset, text);
			
			int i1 = text.IndexOf ("{");
			int i2 = text.IndexOf (";") + 1;
			
			i1++;
			while (i1 < i2 && char.IsWhiteSpace (text[i1]))
				i1++;
			editor.Caret.Offset = offset + i2;
			editor.SetSelection (offset + i1, offset + i2);
			
			context.FormatText (ctx => GetPropertyDeclaration (context));
			editor.Document.EndAtomicUndo ();
		}
	}
}
