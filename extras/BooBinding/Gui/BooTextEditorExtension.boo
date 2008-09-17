#region license
// Copyright (c) 2007, Peter Johanson (latexer@gentoo.org)
// All rights reserved.
//
// BooBinding is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// BooBinding is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with BooBinding; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion
/*
namespace BooBinding.Gui

import System

import MonoDevelop.Projects.Dom.Parser
import MonoDevelop.Ide.Gui.Content
import MonoDevelop.Ide.Gui
import MonoDevelop.Projects.Gui.Completion

import BooBinding.Parser

public class BooTextEditorExtension (CompletionTextEditorExtension):

	override def ExtendsEditor (doc as Document, editor as IEditableTextBuffer) as bool:
		return System.IO.Path.GetExtension (doc.Title) == ".boo";

	override def HandleCodeCompletion (ctx as ICodeCompletionContext, typed_char as System.Char) as ICompletionDataProvider:
		return null if not typed_char in (char('.'), char(' '))
		
		expr_finder = ExpressionFinder ()

		caret_line = ctx.TriggerLine + 1;
		caret_col = ctx.TriggerLineOffset + 1;

		i = ctx.TriggerOffset
		if find_previous_token ("=", i):
			p_ctx = GetParserContext ()
			expr = expr_finder.FindExpression (Editor.GetText (0, i), i -2).Expression
			data_provider = CodeCompletionDataProvider (p_ctx, GetAmbience ())

			resolver = Resolver (p_ctx)

			return data_provider

		expr = expr_finder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 2).Expression;
		return null if not expr

		p_ctx = GetParserContext ()
		completion_prov = CodeCompletionDataProvider (p_ctx, GetAmbience ())

		if typed_char == char(' '):
			if expr in ("is", "as"):
				expr = expr_finder.FindExpression (Editor.GetText (0, ctx.TriggerOffset), ctx.TriggerOffset - 5).Expression
				if expr.Length > 0:
					res = Resolver (p_ctx)
					completion_prov.AddResolveResults (res.IsAsResolve (expr, caret_line, caret_col, FileName, Editor.Text, false))
			elif expr == "import" or expr.EndsWith (" import") or expr.EndsWith ("\timport") or expr.EndsWith ("\nimport") or expr.EndsWith ("\rimport"):
				namespaces = p_ctx.GetNamespaceList ("", true, true)
				completion_prov.AddResolveResults (ResolveResult(namespaces))
		else:
			resolve_result = p_ctx.Resolve (expr, caret_line, caret_col, FileName, Editor.Text)
			completion_prov.AddResolveResults (resolve_result, false)

		return null if completion_prov.IsEmpty

		return completion_prov
	
	private def find_previous_token (token as string, ref i as int):
		s = Editor.GetText (i-1, i)
		while s.Length > 0 and s[0] in (char(' '), char('\t')):
			i--
			s = Editor.GetText (i-1, i)

		return false if s.Length == 0

		i -= token.Length
		return Editor.GetText (i, i + token.Length) == token
*/