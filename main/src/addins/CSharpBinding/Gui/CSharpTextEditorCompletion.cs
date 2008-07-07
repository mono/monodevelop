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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.CSharpBinding.Gui
{
	public class CSharpTextEditorCompletion : CompletionTextEditorExtension
	{
		public CSharpTextEditorCompletion ()
		{
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			System.Console.WriteLine(completionChar + " —-- " + ((int)completionChar));
			CSharpExpressionFinder expressionFinder = new CSharpExpressionFinder ();
			ExpressionResult result = expressionFinder.FindExpression (Editor.Text, Editor.CursorPosition);
			System.Console.WriteLine("found:" + result);
			if (result == null)
				return null;
			
			NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (Document.Project,
			                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
			                                                                                Editor,
			                                                                                Document.FileName);
			
			switch (completionChar) {
			case '.':
				ResolveResult resolveResult = resolver.Resolve (result);
				return CreateCompletionData (resolveResult, result);
				
			case ' ':
				int i = completionContext.TriggerOffset;
				return HandleKeywordCompletion (result, GetPreviousToken (ref i, false));
			}
			return null;
		}
		
		public ICompletionDataProvider HandleKeywordCompletion (ExpressionResult result, string word)
		{
			switch (word) {
			case "using":
				result.ExpressionContext = ExpressionContext.Using;
				return CreateCompletionData (new NamespaceResolveResult (""), result);
			case "is":
			case "as":
				System.Console.WriteLine("IsAs");
				return null;
			case "override":
				System.Console.WriteLine("Override!!!");
				return null;
			case "new":
				System.Console.WriteLine("New!!!");
				return null;
//			case "case":
//				return null;
//			case "return":
//				return null;
			}
			return null;
		}
		
		string GetPreviousToken (ref int i, bool allowLineChange)
		{
			char c;
			
			if (i <= 0)
				return null;
			
			do {
				c = Editor.GetCharAt (--i);
			} while (i > 0 && char.IsWhiteSpace (c) && (allowLineChange ? true : c != '\n'));
			
			if (i == 0)
				return null;
			
			if (!char.IsLetterOrDigit (c))
				return new string (c, 1);
			
			int endOffset = i + 1;
			
			do {
				c = Editor.GetCharAt (i - 1);
				if (!(char.IsLetterOrDigit (c) || c == '_'))
					break;
				
				i--;
			} while (i > 0);
			
			return Editor.GetText (i, endOffset);
		}
		
		/*
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			CSharpExpressionFinder expressionFinder = new CSharpExpressionFinder ();
			ExpressionResult result = expressionFinder.FindExpression (Editor.Text, Editor.CursorPosition);
			if (result == null)
				return base.KeyPress (key, keyChar, modifier);
			switch (keyChar) {
			case '.':
				NRefactoryResolver resolver = new MonoDevelop.CSharpBinding.NRefactoryResolver (Document.Project,
				                                                                                ICSharpCode.NRefactory.SupportedLanguage.CSharp,
				                                                                                Editor,
				                                                                                Document.FileName);
				ResolveResult resolveResult = resolver.Resolve (result);
				System.Console.WriteLine (resolveResult);
				
				ShowCompletion (CreateCompletionData (resolveResult), 0, '.');
				
//				Resolver res = new Resolver (parserContext);
//				ResolveResult results = res.Resolve (expression, caretLineNumber, caretColumn, FileName, Editor.Text);
//				completionProvider.AddResolveResults (results, false, res.CreateTypeNameResolver ());
				break;
			}
			
			return base.KeyPress (key, keyChar, modifier);
		}*/
		
		ICompletionDataProvider CreateCompletionData (ResolveResult resolveResult, ExpressionResult expressionResult)
		{
			if (resolveResult == null || expressionResult == null)
				return null;
			CodeCompletionDataProvider result = new CodeCompletionDataProvider (null, null);
			ProjectDom dom = ProjectDomService.GetDom (Document.Project);
			if (dom == null)
				return null;
			IEnumerable<object> objects = resolveResult.CreateResolveResult (dom);
			if (objects != null) {
				foreach (object obj in objects) {
					if (expressionResult.ExpressionContext != null && expressionResult.ExpressionContext.FilterEntry (obj))
						continue;
					Namespace ns = obj as Namespace;
					if (ns != null) {
						result.AddCompletionData (new CodeCompletionData (ns.Name, ns.StockIcon, ns.Documentation));
						continue;
					}
				
					IMember member = obj as IMember;
					if (member != null) {
						result.AddCompletionData (new CodeCompletionData (member.Name, member.StockIcon, member.Documentation));
						continue;
					}
				}
			}
			
			return result;
		}

	}
}
