//
// CSSBaseListener.cs
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

namespace MonoDevelop.CSSParser
{
	using IErrorNode = Antlr4.Runtime.Tree.IErrorNode;
	using ITerminalNode = Antlr4.Runtime.Tree.ITerminalNode;
	using IToken = Antlr4.Runtime.IToken;
	using ParserRuleContext = Antlr4.Runtime.ParserRuleContext;

	public partial class CSSBaseListener : ICSSListener
	{
		public virtual void EnterOperatorx(CSSParser.OperatorxContext context) { }
		public virtual void ExitOperatorx(CSSParser.OperatorxContext context) { }

		public virtual void EnterSelector(CSSParser.SelectorContext context) { }
		public virtual void ExitSelector(CSSParser.SelectorContext context) { }

		public virtual void EnterElementName(CSSParser.ElementNameContext context) { }
		public virtual void ExitElementName(CSSParser.ElementNameContext context) { }

		public virtual void EnterBodyset(CSSParser.BodysetContext context) { }
		public virtual void ExitBodyset(CSSParser.BodysetContext context) { }

		public virtual void EnterCharSet(CSSParser.CharSetContext context) { }
		public virtual void ExitCharSet(CSSParser.CharSetContext context) { }

		public virtual void EnterPseudo(CSSParser.PseudoContext context) { }
		public virtual void ExitPseudo(CSSParser.PseudoContext context) { }

		public virtual void EnterExpr(CSSParser.ExprContext context) { }
		public virtual void ExitExpr(CSSParser.ExprContext context) { }

		public virtual void EnterPage(CSSParser.PageContext context) { }
		public virtual void ExitPage(CSSParser.PageContext context) { }

		public virtual void EnterElementSubsequent(CSSParser.ElementSubsequentContext context) { }
		public virtual void ExitElementSubsequent(CSSParser.ElementSubsequentContext context) { }

		public virtual void EnterDeclaration(CSSParser.DeclarationContext context) { }
		public virtual void ExitDeclaration(CSSParser.DeclarationContext context) { }

		public virtual void EnterPseudoPage(CSSParser.PseudoPageContext context) { }
		public virtual void ExitPseudoPage(CSSParser.PseudoPageContext context) { }

		public virtual void EnterCombinator(CSSParser.CombinatorContext context) { }
		public virtual void ExitCombinator(CSSParser.CombinatorContext context) { }

		public virtual void EnterProperty(CSSParser.PropertyContext context) { }
		public virtual void ExitProperty(CSSParser.PropertyContext context) { }

		public virtual void EnterAttrib(CSSParser.AttribContext context) { }
		public virtual void ExitAttrib(CSSParser.AttribContext context) { }

		public virtual void EnterMedium(CSSParser.MediumContext context) { }
		public virtual void ExitMedium(CSSParser.MediumContext context) { }

		public virtual void EnterImports(CSSParser.ImportsContext context) { }
		public virtual void ExitImports(CSSParser.ImportsContext context) { }

		public virtual void EnterUnaryOperator(CSSParser.UnaryOperatorContext context) { }
		public virtual void ExitUnaryOperator(CSSParser.UnaryOperatorContext context) { }

		public virtual void EnterStyleSheet(CSSParser.StyleSheetContext context) { }
		public virtual void ExitStyleSheet(CSSParser.StyleSheetContext context) { }

		public virtual void EnterHexColor(CSSParser.HexColorContext context) { }
		public virtual void ExitHexColor(CSSParser.HexColorContext context) { }

		public virtual void EnterBodylist(CSSParser.BodylistContext context) { }
		public virtual void ExitBodylist(CSSParser.BodylistContext context) { }

		public virtual void EnterTerm(CSSParser.TermContext context) { }
		public virtual void ExitTerm(CSSParser.TermContext context) { }

		public virtual void EnterCssClass(CSSParser.CssClassContext context) { }
		public virtual void ExitCssClass(CSSParser.CssClassContext context) { }

		public virtual void EnterComment(CSSParser.CommentContext context) { }
		public virtual void ExitComment(CSSParser.CommentContext context) { }

		public virtual void EnterPrio(CSSParser.PrioContext context) { }
		public virtual void ExitPrio(CSSParser.PrioContext context) { }

		public virtual void EnterMedia(CSSParser.MediaContext context) { }
		public virtual void ExitMedia(CSSParser.MediaContext context) { }

		public virtual void EnterSimpleSelector(CSSParser.SimpleSelectorContext context) { }
		public virtual void ExitSimpleSelector(CSSParser.SimpleSelectorContext context) { }

		public virtual void EnterRuleSet(CSSParser.RuleSetContext context) { }
		public virtual void ExitRuleSet(CSSParser.RuleSetContext context) { }

		public virtual void EnterEveryRule(ParserRuleContext context) { }
		public virtual void ExitEveryRule(ParserRuleContext context) { }
		public virtual void VisitTerminal(ITerminalNode node) { }
		public virtual void VisitErrorNode(IErrorNode node) { }
	}
} // namespace CSSParserAntlr
