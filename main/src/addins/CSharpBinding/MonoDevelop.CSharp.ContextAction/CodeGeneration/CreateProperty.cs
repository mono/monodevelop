// 
// CreateProperty.cs
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
	public class CreateProperty : MDRefactoringContextAction
	{
		protected override string GetMenuText (MDRefactoringContext context)
		{
			var identifier = GetIdentifier (context);
			return string.Format (GettextCatalog.GetString ("Create property '{0}'"), identifier);
		}
		
		protected override bool IsValid (MDRefactoringContext context)
		{
			var identifier = GetIdentifier (context);
			if (identifier == null)
				return false;
			var result = context.Resolve (identifier);
			if (result == null || result.ResolvedType == null || string.IsNullOrEmpty (result.ResolvedType.DecoratedFullName))
				return CreateField.GuessType (context, identifier) != null;
			return false;
		}
		
		protected override void Run (MDRefactoringContext context)
		{
//			var identifier = GetIdentifier (context);
//			context.InsertionMode (GettextCatalog.GetString ("<b>Create property -- Targeting</b>"), 
//				() => context.OutputNode (GeneratePropertyDeclaration (context, identifier), context.GetIndentLevel (identifier) - 1));
		}
		
		AstNode GeneratePropertyDeclaration (MDRefactoringContext context, IdentifierExpression identifier)
		{
			return new PropertyDeclaration () {
				ReturnType = CreateField.GuessType (context, identifier),
				Name = identifier.Identifier,
				Getter = new Accessor (),
				Setter = new Accessor ()
			};
		}
		
		IdentifierExpression GetIdentifier (MDRefactoringContext context)
		{
			return context.GetNode<IdentifierExpression> ();
		}
	}
}

