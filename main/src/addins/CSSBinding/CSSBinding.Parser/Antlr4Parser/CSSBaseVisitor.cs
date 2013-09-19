//
// CSSBaseVisitor.cs
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
using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

namespace MonoDevelop.CSSParser
{
	/// <summary>
	/// CSS base visitor.
	/// </summary>
	public partial class CSSBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, ICSSVisitor<Result>
	{
		public virtual Result VisitOperatorx(CSSParser.OperatorxContext context) { return VisitChildren(context); }

		public virtual Result VisitSelector(CSSParser.SelectorContext context) { return VisitChildren(context); }

		public virtual Result VisitElementName(CSSParser.ElementNameContext context) { return VisitChildren(context); }

		public virtual Result VisitBodyset(CSSParser.BodysetContext context) { return VisitChildren(context); }

		public virtual Result VisitCharSet(CSSParser.CharSetContext context) { return VisitChildren(context); }

		public virtual Result VisitPseudo(CSSParser.PseudoContext context) { return VisitChildren(context); }

		public virtual Result VisitExpr(CSSParser.ExprContext context) { return VisitChildren(context); }

		public virtual Result VisitPage(CSSParser.PageContext context) { return VisitChildren(context); }

		public virtual Result VisitElementSubsequent(CSSParser.ElementSubsequentContext context) { return VisitChildren(context); }

		public virtual Result VisitDeclaration(CSSParser.DeclarationContext context) { return VisitChildren(context); }

		public virtual Result VisitPseudoPage(CSSParser.PseudoPageContext context) { return VisitChildren(context); }

		public virtual Result VisitCombinator(CSSParser.CombinatorContext context) { return VisitChildren(context); }

		public virtual Result VisitProperty(CSSParser.PropertyContext context) { return VisitChildren(context); }

		public virtual Result VisitAttrib(CSSParser.AttribContext context) { return VisitChildren(context); }

		public virtual Result VisitMedium(CSSParser.MediumContext context) { return VisitChildren(context); }

		public virtual Result VisitImports(CSSParser.ImportsContext context) { return VisitChildren(context); }

		public virtual Result VisitUnaryOperator(CSSParser.UnaryOperatorContext context) { return VisitChildren(context); }

		public virtual Result VisitStyleSheet(CSSParser.StyleSheetContext context) { return VisitChildren(context); }

		public virtual Result VisitHexColor(CSSParser.HexColorContext context) { return VisitChildren(context); }

		public virtual Result VisitBodylist(CSSParser.BodylistContext context) { return VisitChildren(context); }

		public virtual Result VisitTerm(CSSParser.TermContext context) { return VisitChildren(context); }

		public virtual Result VisitCssClass(CSSParser.CssClassContext context) { return VisitChildren(context); }

		public virtual Result VisitComment(CSSParser.CommentContext context) { return VisitChildren(context); }

		public virtual Result VisitPrio(CSSParser.PrioContext context) { return VisitChildren(context); }

		public virtual Result VisitMedia(CSSParser.MediaContext context) { return VisitChildren(context); }

		public virtual Result VisitSimpleSelector(CSSParser.SimpleSelectorContext context) { return VisitChildren(context); }

		public virtual Result VisitRuleSet(CSSParser.RuleSetContext context) { return VisitChildren(context); }
	}
} // namespace CSSParserAntlr
