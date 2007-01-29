
using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui.Completion;
using CSharpBinding.Parser;

namespace CSharpBinding
{
	public class CSharpTextEditorExtension: TextEditorExtension
	{
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext ctx, char charTyped)
		{
			if (charTyped != '.' && charTyped != ' ')
				return null;
			
			int caretLineNumber = ctx.TriggerLine + 1;
			int caretColumn = ctx.TriggerLineOffset + 1;

			ExpressionFinder expressionFinder = new ExpressionFinder (null);
			
			int i = ctx.TriggerOffset;
			if (GetPreviousToken ("new", ref i)) {
				if (GetPreviousToken ("=", ref i)) {
					IParserContext pctx = GetParserContext ();
					string ex = expressionFinder.FindExpression (Editor.GetText (0, i), i - 2).Expression;
					CodeCompletionDataProvider cp = new CodeCompletionDataProvider (pctx, GetAmbience ());
					caretColumn -= (i - ctx.TriggerOffset);
					
					// Find the type of the variable that will hold the object
					Resolver res = new Resolver (pctx);
					IReturnType rt = res.internalResolve (ex, caretLineNumber, caretColumn, FileName, Editor.Text);
					if (rt == null)
						return null;
					
					cp.DefaultCompletionString = rt.Name;
					cp.AddResolveResults (pctx.IsAsResolve (ex, caretLineNumber, caretColumn, FileName, Editor.Text));
					
					// Add the variable type itself to the results list (IsAsResolve only returns subclasses)
					IClass cls = pctx.GetClass (rt.FullyQualifiedName, rt.GenericArguments);
					if (cls != null)
						cp.AddResolveResult (cls);
					
					return cp;
				}
			}

			string expression = expressionFinder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 2).Expression;
			if (expression == null)
				return null;

			IParserContext parserContext = GetParserContext ();
			CodeCompletionDataProvider completionProvider = new CodeCompletionDataProvider (parserContext, GetAmbience ());
			
			if (charTyped == ' ') {
				if (expression == "is" || expression == "as") {
					string expr = expressionFinder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 5).Expression;
					completionProvider.AddResolveResults (parserContext.IsAsResolve (expr, caretLineNumber, caretColumn, FileName, Editor.Text));
				}
				else if (expression == "using" || expression.EndsWith(" using") || expression.EndsWith("\tusing")|| expression.EndsWith("\nusing")|| expression.EndsWith("\rusing")) {
					string[] namespaces = parserContext.GetNamespaceList ("", true, true);
					completionProvider.AddResolveResults (new ResolveResult(namespaces));
				}
			} else {
				ResolveResult results = parserContext.Resolve (expression, caretLineNumber, caretColumn, FileName, Editor.Text);
				completionProvider.AddResolveResults (results);
			}
			
			if (completionProvider.IsEmpty)
				return null;
			
			return completionProvider;
		}
		
		bool GetPreviousToken (string token, ref int i)
		{
			string s = Editor.GetText (i-1, i);
			while (s.Length > 0 && (s[0] == ' ' || s[0] == '\t')) {
				i--;
				s = Editor.GetText (i-1, i);
			}
			if (s.Length == 0)
				return false;
			
			i -= token.Length;
			return Editor.GetText (i, i + token.Length) == token;
		}
	}
}
