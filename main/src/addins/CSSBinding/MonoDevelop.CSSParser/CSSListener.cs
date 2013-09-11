//
// CSSListener.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

namespace MonoDevelop.CSSParser
{

	public interface ICSSListener : IParseTreeListener
	{
		void EnterOperatorx(CSSParser.OperatorxContext context);
		void ExitOperatorx(CSSParser.OperatorxContext context);

		void EnterSelector(CSSParser.SelectorContext context);
		void ExitSelector(CSSParser.SelectorContext context);

		void EnterElementName(CSSParser.ElementNameContext context);
		void ExitElementName(CSSParser.ElementNameContext context);

		void EnterBodyset(CSSParser.BodysetContext context);
		void ExitBodyset(CSSParser.BodysetContext context);

		void EnterCharSet(CSSParser.CharSetContext context);
		void ExitCharSet(CSSParser.CharSetContext context);

		void EnterPseudo(CSSParser.PseudoContext context);
		void ExitPseudo(CSSParser.PseudoContext context);

		void EnterExpr(CSSParser.ExprContext context);
		void ExitExpr(CSSParser.ExprContext context);

		void EnterPage(CSSParser.PageContext context);
		void ExitPage(CSSParser.PageContext context);

		void EnterElementSubsequent(CSSParser.ElementSubsequentContext context);
		void ExitElementSubsequent(CSSParser.ElementSubsequentContext context);

		void EnterDeclaration(CSSParser.DeclarationContext context);
		void ExitDeclaration(CSSParser.DeclarationContext context);

		void EnterPseudoPage(CSSParser.PseudoPageContext context);
		void ExitPseudoPage(CSSParser.PseudoPageContext context);

		void EnterCombinator(CSSParser.CombinatorContext context);
		void ExitCombinator(CSSParser.CombinatorContext context);

		void EnterProperty(CSSParser.PropertyContext context);
		void ExitProperty(CSSParser.PropertyContext context);

		void EnterAttrib(CSSParser.AttribContext context);
		void ExitAttrib(CSSParser.AttribContext context);

		void EnterMedium(CSSParser.MediumContext context);
		void ExitMedium(CSSParser.MediumContext context);

		void EnterImports(CSSParser.ImportsContext context);
		void ExitImports(CSSParser.ImportsContext context);

		void EnterUnaryOperator(CSSParser.UnaryOperatorContext context);
		void ExitUnaryOperator(CSSParser.UnaryOperatorContext context);

		void EnterStyleSheet(CSSParser.StyleSheetContext context);
		void ExitStyleSheet(CSSParser.StyleSheetContext context);

		void EnterHexColor(CSSParser.HexColorContext context);
		void ExitHexColor(CSSParser.HexColorContext context);

		void EnterBodylist(CSSParser.BodylistContext context);
		void ExitBodylist(CSSParser.BodylistContext context);

		void EnterTerm(CSSParser.TermContext context);
		void ExitTerm(CSSParser.TermContext context);

		void EnterCssClass(CSSParser.CssClassContext context);
		void ExitCssClass(CSSParser.CssClassContext context);

		void EnterComment(CSSParser.CommentContext context);
		void ExitComment(CSSParser.CommentContext context);

		void EnterPrio(CSSParser.PrioContext context);
		void ExitPrio(CSSParser.PrioContext context);

		void EnterMedia(CSSParser.MediaContext context);
		void ExitMedia(CSSParser.MediaContext context);

		void EnterSimpleSelector(CSSParser.SimpleSelectorContext context);
		void ExitSimpleSelector(CSSParser.SimpleSelectorContext context);

		void EnterRuleSet(CSSParser.RuleSetContext context);
		void ExitRuleSet(CSSParser.RuleSetContext context);
	}
} // namespace CSSParserAntlr
