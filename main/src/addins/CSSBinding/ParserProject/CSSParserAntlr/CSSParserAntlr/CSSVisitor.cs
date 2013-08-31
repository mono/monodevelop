// Generated from CSS.g4 by ANTLR 4.0.1-SNAPSHOT
namespace CSSParserAntlr {
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

public interface ICSSVisitor<Result> : IParseTreeVisitor<Result> {
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

	Result VisitPrio(CSSParser.PrioContext context);

	Result VisitMedia(CSSParser.MediaContext context);

	Result VisitSimpleSelector(CSSParser.SimpleSelectorContext context);

	Result VisitRuleSet(CSSParser.RuleSetContext context);
}
} // namespace CSSParserAntlr
