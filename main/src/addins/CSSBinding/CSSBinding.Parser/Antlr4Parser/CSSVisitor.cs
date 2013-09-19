//
// CSSVisitor.cs
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

	/// <summary>
	/// Iiterface CSSvisitor.
	/// </summary>
	public interface ICSSVisitor<Result> : IParseTreeVisitor<Result>
	{
		Result VisitOperatorx(CSSParser.OperatorxContext context);

		Result VisitSelector(CSSParser.SelectorContext context);

		Result VisitElementName(CSSParser.ElementNameContext context);

		Result VisitBodyset(CSSParser.BodysetContext context);

		Result VisitCharSet(CSSParser.CharSetContext context);

		Result VisitPseudo(CSSParser.PseudoContext context);

		Result VisitExpr(CSSParser.ExprContext context);

		Result VisitPage(CSSParser.PageContext context);

		Result VisitElementSubsequent(CSSParser.ElementSubsequentContext context);

		Result VisitDeclaration(CSSParser.DeclarationContext context);

		Result VisitPseudoPage(CSSParser.PseudoPageContext context);

		Result VisitCombinator(CSSParser.CombinatorContext context);

		Result VisitProperty(CSSParser.PropertyContext context);

		Result VisitAttrib(CSSParser.AttribContext context);

		Result VisitMedium(CSSParser.MediumContext context);

		Result VisitImports(CSSParser.ImportsContext context);

		Result VisitUnaryOperator(CSSParser.UnaryOperatorContext context);

		Result VisitStyleSheet(CSSParser.StyleSheetContext context);

		Result VisitHexColor(CSSParser.HexColorContext context);

		Result VisitBodylist(CSSParser.BodylistContext context);

		Result VisitTerm(CSSParser.TermContext context);

		Result VisitCssClass(CSSParser.CssClassContext context);

		Result VisitComment(CSSParser.CommentContext context);

		Result VisitPrio(CSSParser.PrioContext context);

		Result VisitMedia(CSSParser.MediaContext context);

		Result VisitSimpleSelector(CSSParser.SimpleSelectorContext context);

		Result VisitRuleSet(CSSParser.RuleSetContext context);
	}
} // namespace CSSParserAntlr
