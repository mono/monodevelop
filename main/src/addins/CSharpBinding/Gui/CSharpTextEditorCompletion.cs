//
// CSharpTextEditorCompletion.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.CSharpBinding.Gui
{
	public class CSharpTextEditorCompletion : CompletionTextEditorExtension
	{
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			CSharpExpressionFinder expressionFinder = new CSharpExpressionFinder ();
			ExpressionResult result = expressionFinder.FindExpression (Editor.Text, Editor.CursorPosition);
			if (result == null)
				return true;
			switch (keyChar) {
			case '.':
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (Document.Project,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				ResolveResult resolveResult = resolver.Resolve (result);
				
				ShowCompletion (CreateCompletionData (resolveResult), 0, '.');
				
//				Resolver res = new Resolver (parserContext);
//				ResolveResult results = res.Resolve (expression, caretLineNumber, caretColumn, FileName, Editor.Text);
//				completionProvider.AddResolveResults (results, false, res.CreateTypeNameResolver ());
				break;
			}
			
			return true;
		}
		
		ICompletionDataProvider CreateCompletionData (ResolveResult resolveResult)
		{
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, null);
//			ProjectDom dom = ProjectDomService.GetDom (Document.Project);
//			IType type = dom.GetType (resolveResult.ResolvedType);
//			if (type != null) {
//				// Add accessible members.
//			}
			
			return result;
		}

	}
}
