// Generated from CSS.g4 by ANTLR 4.0.1-SNAPSHOT
namespace CSSParserAntlr {
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using DFA = Antlr4.Runtime.Dfa.DFA;

public partial class CSSParser : Parser {
	public const int
		COMMENT=1, CDO=2, CDC=3, INCLUDES=4, DASHMATCH=5, GREATER=6, LBRACE=7, 
		RBRACE=8, LBRACKET=9, RBRACKET=10, OPEQ=11, SEMI=12, COLON=13, SOLIDUS=14, 
		MINUS=15, PLUS=16, STAR=17, LPAREN=18, RPAREN=19, COMMA=20, DOT=21, STRING=22, 
		IDENT=23, HASH=24, IMPORT_SYM=25, PAGE_SYM=26, MEDIA_SYM=27, CHARSET_SYM=28, 
		IMPORTANT_SYM=29, EMS=30, EXS=31, LENGTH=32, TIME=33, ANGLE=34, FREQ=35, 
		DIMENSION=36, PERCENTAGE=37, NUMBER=38, URI=39, WS=40, NL=41;
	public static readonly string[] tokenNames = {
		"<INVALID>", "COMMENT", "'<!--'", "'-->'", "'~='", "'|='", "'>'", "'{'", 
		"'}'", "'['", "']'", "'='", "';'", "':'", "'/'", "'-'", "'+'", "'*'", 
		"'('", "')'", "','", "'.'", "STRING", "IDENT", "HASH", "IMPORT_SYM", "PAGE_SYM", 
		"MEDIA_SYM", "'@charset '", "IMPORTANT_SYM", "EMS", "EXS", "LENGTH", "TIME", 
		"ANGLE", "FREQ", "DIMENSION", "PERCENTAGE", "NUMBER", "URI", "WS", "NL"
	};
	public const int
		RULE_styleSheet = 0, RULE_charSet = 1, RULE_imports = 2, RULE_media = 3, 
		RULE_medium = 4, RULE_bodylist = 5, RULE_bodyset = 6, RULE_page = 7, RULE_pseudoPage = 8, 
		RULE_operatorx = 9, RULE_combinator = 10, RULE_unaryOperator = 11, RULE_property = 12, 
		RULE_ruleSet = 13, RULE_selector = 14, RULE_simpleSelector = 15, RULE_elementSubsequent = 16, 
		RULE_cssClass = 17, RULE_elementName = 18, RULE_attrib = 19, RULE_pseudo = 20, 
		RULE_declaration = 21, RULE_prio = 22, RULE_expr = 23, RULE_term = 24, 
		RULE_hexColor = 25;
	public static readonly string[] ruleNames = {
		"styleSheet", "charSet", "imports", "media", "medium", "bodylist", "bodyset", 
		"page", "pseudoPage", "operatorx", "combinator", "unaryOperator", "property", 
		"ruleSet", "selector", "simpleSelector", "elementSubsequent", "cssClass", 
		"elementName", "attrib", "pseudo", "declaration", "prio", "expr", "term", 
		"hexColor"
	};

	public override string GrammarFileName { get { return "CSS.g4"; } }

	public override string[] TokenNames { get { return tokenNames; } }

	public override string[] RuleNames { get { return ruleNames; } }


		protected const int EOF = Eof;

	public CSSParser(ITokenStream input)
		: base(input)
	{
		_interp = new ParserATNSimulator(this,_ATN);
	}
	public partial class StyleSheetContext : ParserRuleContext {
		public ImportsContext imports(int i) {
			return GetRuleContext<ImportsContext>(i);
		}
		public BodylistContext bodylist() {
			return GetRuleContext<BodylistContext>(0);
		}
		public CharSetContext charSet() {
			return GetRuleContext<CharSetContext>(0);
		}
		public ITerminalNode EOF() { return GetToken(CSSParser.EOF, 0); }
		public IReadOnlyList<ImportsContext> imports() {
			return GetRuleContexts<ImportsContext>();
		}
		public StyleSheetContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_styleSheet; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterStyleSheet(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitStyleSheet(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitStyleSheet(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public StyleSheetContext styleSheet() {
		StyleSheetContext _localctx = new StyleSheetContext(_ctx, State);
		EnterRule(_localctx, 0, RULE_styleSheet);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 52; charSet();
			State = 56;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while (_la==IMPORT_SYM) {
				{
				{
				State = 53; imports();
				}
				}
				State = 58;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			State = 59; bodylist();
			State = 60; Match(EOF);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class CharSetContext : ParserRuleContext {
		public ITerminalNode CHARSET_SYM() { return GetToken(CSSParser.CHARSET_SYM, 0); }
		public ITerminalNode SEMI() { return GetToken(CSSParser.SEMI, 0); }
		public ITerminalNode STRING() { return GetToken(CSSParser.STRING, 0); }
		public CharSetContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_charSet; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterCharSet(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitCharSet(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCharSet(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public CharSetContext charSet() {
		CharSetContext _localctx = new CharSetContext(_ctx, State);
		EnterRule(_localctx, 2, RULE_charSet);
		try {
			State = 66;
			switch (_input.La(1)) {
			case CHARSET_SYM:
				EnterOuterAlt(_localctx, 1);
				{
				State = 62; Match(CHARSET_SYM);
				State = 63; Match(STRING);
				State = 64; Match(SEMI);
				}
				break;
			case EOF:
			case LBRACKET:
			case COLON:
			case STAR:
			case DOT:
			case IDENT:
			case HASH:
			case IMPORT_SYM:
			case PAGE_SYM:
			case MEDIA_SYM:
				EnterOuterAlt(_localctx, 2);
				{
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ImportsContext : ParserRuleContext {
		public MediumContext medium(int i) {
			return GetRuleContext<MediumContext>(i);
		}
		public ITerminalNode COMMA(int i) {
			return GetToken(CSSParser.COMMA, i);
		}
		public IReadOnlyList<ITerminalNode> COMMA() { return GetTokens(CSSParser.COMMA); }
		public ITerminalNode IMPORT_SYM() { return GetToken(CSSParser.IMPORT_SYM, 0); }
		public ITerminalNode SEMI() { return GetToken(CSSParser.SEMI, 0); }
		public IReadOnlyList<MediumContext> medium() {
			return GetRuleContexts<MediumContext>();
		}
		public ITerminalNode STRING() { return GetToken(CSSParser.STRING, 0); }
		public ITerminalNode URI() { return GetToken(CSSParser.URI, 0); }
		public ImportsContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_imports; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterImports(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitImports(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitImports(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ImportsContext imports() {
		ImportsContext _localctx = new ImportsContext(_ctx, State);
		EnterRule(_localctx, 4, RULE_imports);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 68; Match(IMPORT_SYM);
			State = 69;
			_la = _input.La(1);
			if ( !(_la==STRING || _la==URI) ) {
			_errHandler.RecoverInline(this);
			}
			Consume();
			State = 78;
			_la = _input.La(1);
			if (_la==IDENT) {
				{
				State = 70; medium();
				State = 75;
				_errHandler.Sync(this);
				_la = _input.La(1);
				while (_la==COMMA) {
					{
					{
					State = 71; Match(COMMA);
					State = 72; medium();
					}
					}
					State = 77;
					_errHandler.Sync(this);
					_la = _input.La(1);
				}
				}
			}

			State = 80; Match(SEMI);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class MediaContext : ParserRuleContext {
		public MediumContext medium(int i) {
			return GetRuleContext<MediumContext>(i);
		}
		public ITerminalNode COMMA(int i) {
			return GetToken(CSSParser.COMMA, i);
		}
		public ITerminalNode RBRACE() { return GetToken(CSSParser.RBRACE, 0); }
		public IReadOnlyList<ITerminalNode> COMMA() { return GetTokens(CSSParser.COMMA); }
		public ITerminalNode MEDIA_SYM() { return GetToken(CSSParser.MEDIA_SYM, 0); }
		public RuleSetContext ruleSet() {
			return GetRuleContext<RuleSetContext>(0);
		}
		public ITerminalNode LBRACE() { return GetToken(CSSParser.LBRACE, 0); }
		public IReadOnlyList<MediumContext> medium() {
			return GetRuleContexts<MediumContext>();
		}
		public MediaContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_media; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterMedia(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitMedia(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitMedia(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public MediaContext media() {
		MediaContext _localctx = new MediaContext(_ctx, State);
		EnterRule(_localctx, 6, RULE_media);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 82; Match(MEDIA_SYM);
			State = 83; medium();
			State = 88;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while (_la==COMMA) {
				{
				{
				State = 84; Match(COMMA);
				State = 85; medium();
				}
				}
				State = 90;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			State = 91; Match(LBRACE);
			State = 92; ruleSet();
			State = 93; Match(RBRACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class MediumContext : ParserRuleContext {
		public ITerminalNode IDENT() { return GetToken(CSSParser.IDENT, 0); }
		public MediumContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_medium; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterMedium(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitMedium(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitMedium(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public MediumContext medium() {
		MediumContext _localctx = new MediumContext(_ctx, State);
		EnterRule(_localctx, 8, RULE_medium);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 95; Match(IDENT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class BodylistContext : ParserRuleContext {
		public BodysetContext bodyset(int i) {
			return GetRuleContext<BodysetContext>(i);
		}
		public IReadOnlyList<BodysetContext> bodyset() {
			return GetRuleContexts<BodysetContext>();
		}
		public BodylistContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_bodylist; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterBodylist(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitBodylist(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitBodylist(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public BodylistContext bodylist() {
		BodylistContext _localctx = new BodylistContext(_ctx, State);
		EnterRule(_localctx, 10, RULE_bodylist);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 100;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << LBRACKET) | (1L << COLON) | (1L << STAR) | (1L << DOT) | (1L << IDENT) | (1L << HASH) | (1L << PAGE_SYM) | (1L << MEDIA_SYM))) != 0)) {
				{
				{
				State = 97; bodyset();
				}
				}
				State = 102;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class BodysetContext : ParserRuleContext {
		public PageContext page() {
			return GetRuleContext<PageContext>(0);
		}
		public MediaContext media() {
			return GetRuleContext<MediaContext>(0);
		}
		public RuleSetContext ruleSet() {
			return GetRuleContext<RuleSetContext>(0);
		}
		public BodysetContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_bodyset; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterBodyset(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitBodyset(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitBodyset(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public BodysetContext bodyset() {
		BodysetContext _localctx = new BodysetContext(_ctx, State);
		EnterRule(_localctx, 12, RULE_bodyset);
		try {
			State = 106;
			switch (_input.La(1)) {
			case LBRACKET:
			case COLON:
			case STAR:
			case DOT:
			case IDENT:
			case HASH:
				EnterOuterAlt(_localctx, 1);
				{
				State = 103; ruleSet();
				}
				break;
			case MEDIA_SYM:
				EnterOuterAlt(_localctx, 2);
				{
				State = 104; media();
				}
				break;
			case PAGE_SYM:
				EnterOuterAlt(_localctx, 3);
				{
				State = 105; page();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class PageContext : ParserRuleContext {
		public ITerminalNode PAGE_SYM() { return GetToken(CSSParser.PAGE_SYM, 0); }
		public IReadOnlyList<DeclarationContext> declaration() {
			return GetRuleContexts<DeclarationContext>();
		}
		public ITerminalNode RBRACE() { return GetToken(CSSParser.RBRACE, 0); }
		public PseudoPageContext pseudoPage() {
			return GetRuleContext<PseudoPageContext>(0);
		}
		public DeclarationContext declaration(int i) {
			return GetRuleContext<DeclarationContext>(i);
		}
		public IReadOnlyList<ITerminalNode> SEMI() { return GetTokens(CSSParser.SEMI); }
		public ITerminalNode SEMI(int i) {
			return GetToken(CSSParser.SEMI, i);
		}
		public ITerminalNode LBRACE() { return GetToken(CSSParser.LBRACE, 0); }
		public PageContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_page; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterPage(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitPage(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitPage(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public PageContext page() {
		PageContext _localctx = new PageContext(_ctx, State);
		EnterRule(_localctx, 14, RULE_page);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 108; Match(PAGE_SYM);
			State = 110;
			_la = _input.La(1);
			if (_la==COLON) {
				{
				State = 109; pseudoPage();
				}
			}

			State = 112; Match(LBRACE);
			State = 113; declaration();
			State = 114; Match(SEMI);
			State = 120;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while (_la==IDENT) {
				{
				{
				State = 115; declaration();
				State = 116; Match(SEMI);
				}
				}
				State = 122;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			State = 123; Match(RBRACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class PseudoPageContext : ParserRuleContext {
		public ITerminalNode COLON() { return GetToken(CSSParser.COLON, 0); }
		public ITerminalNode IDENT() { return GetToken(CSSParser.IDENT, 0); }
		public PseudoPageContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_pseudoPage; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterPseudoPage(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitPseudoPage(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitPseudoPage(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public PseudoPageContext pseudoPage() {
		PseudoPageContext _localctx = new PseudoPageContext(_ctx, State);
		EnterRule(_localctx, 16, RULE_pseudoPage);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 125; Match(COLON);
			State = 126; Match(IDENT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class OperatorxContext : ParserRuleContext {
		public ITerminalNode COMMA() { return GetToken(CSSParser.COMMA, 0); }
		public ITerminalNode SOLIDUS() { return GetToken(CSSParser.SOLIDUS, 0); }
		public OperatorxContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_operatorx; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterOperatorx(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitOperatorx(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitOperatorx(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public OperatorxContext operatorx() {
		OperatorxContext _localctx = new OperatorxContext(_ctx, State);
		EnterRule(_localctx, 18, RULE_operatorx);
		try {
			State = 131;
			switch (_input.La(1)) {
			case SOLIDUS:
				EnterOuterAlt(_localctx, 1);
				{
				State = 128; Match(SOLIDUS);
				}
				break;
			case COMMA:
				EnterOuterAlt(_localctx, 2);
				{
				State = 129; Match(COMMA);
				}
				break;
			case MINUS:
			case PLUS:
			case STRING:
			case IDENT:
			case HASH:
			case EMS:
			case EXS:
			case LENGTH:
			case TIME:
			case ANGLE:
			case FREQ:
			case PERCENTAGE:
			case NUMBER:
			case URI:
				EnterOuterAlt(_localctx, 3);
				{
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class CombinatorContext : ParserRuleContext {
		public ITerminalNode PLUS() { return GetToken(CSSParser.PLUS, 0); }
		public ITerminalNode GREATER() { return GetToken(CSSParser.GREATER, 0); }
		public CombinatorContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_combinator; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterCombinator(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitCombinator(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCombinator(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public CombinatorContext combinator() {
		CombinatorContext _localctx = new CombinatorContext(_ctx, State);
		EnterRule(_localctx, 20, RULE_combinator);
		try {
			State = 136;
			switch (_input.La(1)) {
			case PLUS:
				EnterOuterAlt(_localctx, 1);
				{
				State = 133; Match(PLUS);
				}
				break;
			case GREATER:
				EnterOuterAlt(_localctx, 2);
				{
				State = 134; Match(GREATER);
				}
				break;
			case LBRACKET:
			case COLON:
			case STAR:
			case DOT:
			case IDENT:
			case HASH:
				EnterOuterAlt(_localctx, 3);
				{
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class UnaryOperatorContext : ParserRuleContext {
		public ITerminalNode PLUS() { return GetToken(CSSParser.PLUS, 0); }
		public ITerminalNode MINUS() { return GetToken(CSSParser.MINUS, 0); }
		public UnaryOperatorContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_unaryOperator; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterUnaryOperator(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitUnaryOperator(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitUnaryOperator(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public UnaryOperatorContext unaryOperator() {
		UnaryOperatorContext _localctx = new UnaryOperatorContext(_ctx, State);
		EnterRule(_localctx, 22, RULE_unaryOperator);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 138;
			_la = _input.La(1);
			if ( !(_la==MINUS || _la==PLUS) ) {
			_errHandler.RecoverInline(this);
			}
			Consume();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class PropertyContext : ParserRuleContext {
		public ITerminalNode IDENT() { return GetToken(CSSParser.IDENT, 0); }
		public PropertyContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_property; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterProperty(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitProperty(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitProperty(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public PropertyContext property() {
		PropertyContext _localctx = new PropertyContext(_ctx, State);
		EnterRule(_localctx, 24, RULE_property);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 140; Match(IDENT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class RuleSetContext : ParserRuleContext {
		public IReadOnlyList<SelectorContext> selector() {
			return GetRuleContexts<SelectorContext>();
		}
		public IReadOnlyList<DeclarationContext> declaration() {
			return GetRuleContexts<DeclarationContext>();
		}
		public ITerminalNode COMMA(int i) {
			return GetToken(CSSParser.COMMA, i);
		}
		public SelectorContext selector(int i) {
			return GetRuleContext<SelectorContext>(i);
		}
		public ITerminalNode RBRACE() { return GetToken(CSSParser.RBRACE, 0); }
		public IReadOnlyList<ITerminalNode> COMMA() { return GetTokens(CSSParser.COMMA); }
		public DeclarationContext declaration(int i) {
			return GetRuleContext<DeclarationContext>(i);
		}
		public IReadOnlyList<ITerminalNode> SEMI() { return GetTokens(CSSParser.SEMI); }
		public ITerminalNode SEMI(int i) {
			return GetToken(CSSParser.SEMI, i);
		}
		public ITerminalNode LBRACE() { return GetToken(CSSParser.LBRACE, 0); }
		public RuleSetContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_ruleSet; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterRuleSet(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitRuleSet(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitRuleSet(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public RuleSetContext ruleSet() {
		RuleSetContext _localctx = new RuleSetContext(_ctx, State);
		EnterRule(_localctx, 26, RULE_ruleSet);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 142; selector();
			State = 147;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while (_la==COMMA) {
				{
				{
				State = 143; Match(COMMA);
				State = 144; selector();
				}
				}
				State = 149;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			State = 150; Match(LBRACE);
			State = 151; declaration();
			State = 152; Match(SEMI);
			State = 158;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while (_la==IDENT) {
				{
				{
				State = 153; declaration();
				State = 154; Match(SEMI);
				}
				}
				State = 160;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			State = 161; Match(RBRACE);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class SelectorContext : ParserRuleContext {
		public SimpleSelectorContext simpleSelector(int i) {
			return GetRuleContext<SimpleSelectorContext>(i);
		}
		public CombinatorContext combinator(int i) {
			return GetRuleContext<CombinatorContext>(i);
		}
		public IReadOnlyList<CombinatorContext> combinator() {
			return GetRuleContexts<CombinatorContext>();
		}
		public IReadOnlyList<SimpleSelectorContext> simpleSelector() {
			return GetRuleContexts<SimpleSelectorContext>();
		}
		public SelectorContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_selector; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterSelector(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitSelector(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitSelector(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public SelectorContext selector() {
		SelectorContext _localctx = new SelectorContext(_ctx, State);
		EnterRule(_localctx, 28, RULE_selector);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 163; simpleSelector();
			State = 169;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << GREATER) | (1L << LBRACKET) | (1L << COLON) | (1L << PLUS) | (1L << STAR) | (1L << DOT) | (1L << IDENT) | (1L << HASH))) != 0)) {
				{
				{
				State = 164; combinator();
				State = 165; simpleSelector();
				}
				}
				State = 171;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class SimpleSelectorContext : ParserRuleContext {
		public ElementSubsequentContext elementSubsequent(int i) {
			return GetRuleContext<ElementSubsequentContext>(i);
		}
		public ElementNameContext elementName() {
			return GetRuleContext<ElementNameContext>(0);
		}
		public IReadOnlyList<ElementSubsequentContext> elementSubsequent() {
			return GetRuleContexts<ElementSubsequentContext>();
		}
		public SimpleSelectorContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_simpleSelector; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterSimpleSelector(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitSimpleSelector(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitSimpleSelector(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public SimpleSelectorContext simpleSelector() {
		SimpleSelectorContext _localctx = new SimpleSelectorContext(_ctx, State);
		EnterRule(_localctx, 30, RULE_simpleSelector);
		try {
			int _alt;
			State = 184;
			switch (_input.La(1)) {
			case STAR:
			case IDENT:
				EnterOuterAlt(_localctx, 1);
				{
				State = 172; elementName();
				State = 176;
				_errHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(_input,14,_ctx);
				while ( _alt!=2 && _alt!=-1 ) {
					if ( _alt==1 ) {
						{
						{
						State = 173; elementSubsequent();
						}
						} 
					}
					State = 178;
					_errHandler.Sync(this);
					_alt = Interpreter.AdaptivePredict(_input,14,_ctx);
				}
				}
				break;
			case LBRACKET:
			case COLON:
			case DOT:
			case HASH:
				EnterOuterAlt(_localctx, 2);
				{
				State = 180;
				_errHandler.Sync(this);
				_alt = Interpreter.AdaptivePredict(_input,15,_ctx);
				do {
					switch (_alt) {
					case 1:
						{
						{
						State = 179; elementSubsequent();
						}
						}
						break;
					default:
						throw new NoViableAltException(this);
					}
					State = 182;
					_errHandler.Sync(this);
					_alt = Interpreter.AdaptivePredict(_input,15,_ctx);
				} while ( _alt!=2 && _alt!=-1 );
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ElementSubsequentContext : ParserRuleContext {
		public ITerminalNode HASH() { return GetToken(CSSParser.HASH, 0); }
		public PseudoContext pseudo() {
			return GetRuleContext<PseudoContext>(0);
		}
		public CssClassContext cssClass() {
			return GetRuleContext<CssClassContext>(0);
		}
		public AttribContext attrib() {
			return GetRuleContext<AttribContext>(0);
		}
		public ElementSubsequentContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_elementSubsequent; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterElementSubsequent(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitElementSubsequent(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitElementSubsequent(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ElementSubsequentContext elementSubsequent() {
		ElementSubsequentContext _localctx = new ElementSubsequentContext(_ctx, State);
		EnterRule(_localctx, 32, RULE_elementSubsequent);
		try {
			State = 190;
			switch (_input.La(1)) {
			case HASH:
				EnterOuterAlt(_localctx, 1);
				{
				State = 186; Match(HASH);
				}
				break;
			case DOT:
				EnterOuterAlt(_localctx, 2);
				{
				State = 187; cssClass();
				}
				break;
			case LBRACKET:
				EnterOuterAlt(_localctx, 3);
				{
				State = 188; attrib();
				}
				break;
			case COLON:
				EnterOuterAlt(_localctx, 4);
				{
				State = 189; pseudo();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class CssClassContext : ParserRuleContext {
		public ITerminalNode IDENT() { return GetToken(CSSParser.IDENT, 0); }
		public ITerminalNode DOT() { return GetToken(CSSParser.DOT, 0); }
		public CssClassContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_cssClass; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterCssClass(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitCssClass(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitCssClass(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public CssClassContext cssClass() {
		CssClassContext _localctx = new CssClassContext(_ctx, State);
		EnterRule(_localctx, 34, RULE_cssClass);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 192; Match(DOT);
			State = 193; Match(IDENT);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ElementNameContext : ParserRuleContext {
		public ITerminalNode IDENT() { return GetToken(CSSParser.IDENT, 0); }
		public ITerminalNode STAR() { return GetToken(CSSParser.STAR, 0); }
		public ElementNameContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_elementName; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterElementName(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitElementName(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitElementName(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ElementNameContext elementName() {
		ElementNameContext _localctx = new ElementNameContext(_ctx, State);
		EnterRule(_localctx, 36, RULE_elementName);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 195;
			_la = _input.La(1);
			if ( !(_la==STAR || _la==IDENT) ) {
			_errHandler.RecoverInline(this);
			}
			Consume();
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class AttribContext : ParserRuleContext {
		public ITerminalNode INCLUDES() { return GetToken(CSSParser.INCLUDES, 0); }
		public ITerminalNode LBRACKET() { return GetToken(CSSParser.LBRACKET, 0); }
		public IReadOnlyList<ITerminalNode> IDENT() { return GetTokens(CSSParser.IDENT); }
		public ITerminalNode DASHMATCH() { return GetToken(CSSParser.DASHMATCH, 0); }
		public ITerminalNode OPEQ() { return GetToken(CSSParser.OPEQ, 0); }
		public ITerminalNode RBRACKET() { return GetToken(CSSParser.RBRACKET, 0); }
		public ITerminalNode IDENT(int i) {
			return GetToken(CSSParser.IDENT, i);
		}
		public ITerminalNode STRING() { return GetToken(CSSParser.STRING, 0); }
		public AttribContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_attrib; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterAttrib(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitAttrib(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitAttrib(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public AttribContext attrib() {
		AttribContext _localctx = new AttribContext(_ctx, State);
		EnterRule(_localctx, 38, RULE_attrib);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 197; Match(LBRACKET);
			State = 198; Match(IDENT);
			State = 201;
			_la = _input.La(1);
			if ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << INCLUDES) | (1L << DASHMATCH) | (1L << OPEQ))) != 0)) {
				{
				State = 199;
				_la = _input.La(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << INCLUDES) | (1L << DASHMATCH) | (1L << OPEQ))) != 0)) ) {
				_errHandler.RecoverInline(this);
				}
				Consume();
				State = 200;
				_la = _input.La(1);
				if ( !(_la==STRING || _la==IDENT) ) {
				_errHandler.RecoverInline(this);
				}
				Consume();
				}
			}

			State = 203; Match(RBRACKET);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class PseudoContext : ParserRuleContext {
		public ITerminalNode COLON() { return GetToken(CSSParser.COLON, 0); }
		public ITerminalNode RPAREN() { return GetToken(CSSParser.RPAREN, 0); }
		public IReadOnlyList<ITerminalNode> IDENT() { return GetTokens(CSSParser.IDENT); }
		public ITerminalNode IDENT(int i) {
			return GetToken(CSSParser.IDENT, i);
		}
		public ITerminalNode LPAREN() { return GetToken(CSSParser.LPAREN, 0); }
		public PseudoContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_pseudo; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterPseudo(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitPseudo(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitPseudo(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public PseudoContext pseudo() {
		PseudoContext _localctx = new PseudoContext(_ctx, State);
		EnterRule(_localctx, 40, RULE_pseudo);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 205; Match(COLON);
			State = 206; Match(IDENT);
			State = 212;
			_la = _input.La(1);
			if (_la==LPAREN) {
				{
				State = 207; Match(LPAREN);
				State = 209;
				_la = _input.La(1);
				if (_la==IDENT) {
					{
					State = 208; Match(IDENT);
					}
				}

				State = 211; Match(RPAREN);
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class DeclarationContext : ParserRuleContext {
		public ITerminalNode COLON() { return GetToken(CSSParser.COLON, 0); }
		public ExprContext expr() {
			return GetRuleContext<ExprContext>(0);
		}
		public PropertyContext property() {
			return GetRuleContext<PropertyContext>(0);
		}
		public PrioContext prio() {
			return GetRuleContext<PrioContext>(0);
		}
		public DeclarationContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_declaration; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterDeclaration(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitDeclaration(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitDeclaration(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public DeclarationContext declaration() {
		DeclarationContext _localctx = new DeclarationContext(_ctx, State);
		EnterRule(_localctx, 42, RULE_declaration);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 214; property();
			State = 215; Match(COLON);
			State = 216; expr();
			State = 218;
			_la = _input.La(1);
			if (_la==IMPORTANT_SYM) {
				{
				State = 217; prio();
				}
			}

			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class PrioContext : ParserRuleContext {
		public ITerminalNode IMPORTANT_SYM() { return GetToken(CSSParser.IMPORTANT_SYM, 0); }
		public PrioContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_prio; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterPrio(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitPrio(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitPrio(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public PrioContext prio() {
		PrioContext _localctx = new PrioContext(_ctx, State);
		EnterRule(_localctx, 44, RULE_prio);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 220; Match(IMPORTANT_SYM);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class ExprContext : ParserRuleContext {
		public IReadOnlyList<OperatorxContext> operatorx() {
			return GetRuleContexts<OperatorxContext>();
		}
		public IReadOnlyList<TermContext> term() {
			return GetRuleContexts<TermContext>();
		}
		public OperatorxContext operatorx(int i) {
			return GetRuleContext<OperatorxContext>(i);
		}
		public TermContext term(int i) {
			return GetRuleContext<TermContext>(i);
		}
		public ExprContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_expr; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterExpr(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitExpr(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitExpr(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public ExprContext expr() {
		ExprContext _localctx = new ExprContext(_ctx, State);
		EnterRule(_localctx, 46, RULE_expr);
		int _la;
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 222; term();
			State = 228;
			_errHandler.Sync(this);
			_la = _input.La(1);
			while ((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << SOLIDUS) | (1L << MINUS) | (1L << PLUS) | (1L << COMMA) | (1L << STRING) | (1L << IDENT) | (1L << HASH) | (1L << EMS) | (1L << EXS) | (1L << LENGTH) | (1L << TIME) | (1L << ANGLE) | (1L << FREQ) | (1L << PERCENTAGE) | (1L << NUMBER) | (1L << URI))) != 0)) {
				{
				{
				State = 223; operatorx();
				State = 224; term();
				}
				}
				State = 230;
				_errHandler.Sync(this);
				_la = _input.La(1);
			}
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class TermContext : ParserRuleContext {
		public ITerminalNode TIME() { return GetToken(CSSParser.TIME, 0); }
		public ITerminalNode RPAREN() { return GetToken(CSSParser.RPAREN, 0); }
		public ITerminalNode ANGLE() { return GetToken(CSSParser.ANGLE, 0); }
		public ITerminalNode EMS() { return GetToken(CSSParser.EMS, 0); }
		public ExprContext expr() {
			return GetRuleContext<ExprContext>(0);
		}
		public ITerminalNode EXS() { return GetToken(CSSParser.EXS, 0); }
		public ITerminalNode NUMBER() { return GetToken(CSSParser.NUMBER, 0); }
		public UnaryOperatorContext unaryOperator() {
			return GetRuleContext<UnaryOperatorContext>(0);
		}
		public ITerminalNode IDENT() { return GetToken(CSSParser.IDENT, 0); }
		public HexColorContext hexColor() {
			return GetRuleContext<HexColorContext>(0);
		}
		public ITerminalNode FREQ() { return GetToken(CSSParser.FREQ, 0); }
		public ITerminalNode PERCENTAGE() { return GetToken(CSSParser.PERCENTAGE, 0); }
		public ITerminalNode LENGTH() { return GetToken(CSSParser.LENGTH, 0); }
		public ITerminalNode LPAREN() { return GetToken(CSSParser.LPAREN, 0); }
		public ITerminalNode STRING() { return GetToken(CSSParser.STRING, 0); }
		public ITerminalNode URI() { return GetToken(CSSParser.URI, 0); }
		public TermContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_term; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterTerm(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitTerm(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitTerm(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public TermContext term() {
		TermContext _localctx = new TermContext(_ctx, State);
		EnterRule(_localctx, 48, RULE_term);
		int _la;
		try {
			State = 245;
			switch (_input.La(1)) {
			case MINUS:
			case PLUS:
			case EMS:
			case EXS:
			case LENGTH:
			case TIME:
			case ANGLE:
			case FREQ:
			case PERCENTAGE:
			case NUMBER:
				EnterOuterAlt(_localctx, 1);
				{
				State = 232;
				_la = _input.La(1);
				if (_la==MINUS || _la==PLUS) {
					{
					State = 231; unaryOperator();
					}
				}

				State = 234;
				_la = _input.La(1);
				if ( !((((_la) & ~0x3f) == 0 && ((1L << _la) & ((1L << EMS) | (1L << EXS) | (1L << LENGTH) | (1L << TIME) | (1L << ANGLE) | (1L << FREQ) | (1L << PERCENTAGE) | (1L << NUMBER))) != 0)) ) {
				_errHandler.RecoverInline(this);
				}
				Consume();
				}
				break;
			case STRING:
				EnterOuterAlt(_localctx, 2);
				{
				State = 235; Match(STRING);
				}
				break;
			case IDENT:
				EnterOuterAlt(_localctx, 3);
				{
				State = 236; Match(IDENT);
				State = 241;
				_la = _input.La(1);
				if (_la==LPAREN) {
					{
					State = 237; Match(LPAREN);
					State = 238; expr();
					State = 239; Match(RPAREN);
					}
				}

				}
				break;
			case URI:
				EnterOuterAlt(_localctx, 4);
				{
				State = 243; Match(URI);
				}
				break;
			case HASH:
				EnterOuterAlt(_localctx, 5);
				{
				State = 244; hexColor();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public partial class HexColorContext : ParserRuleContext {
		public ITerminalNode HASH() { return GetToken(CSSParser.HASH, 0); }
		public HexColorContext(ParserRuleContext parent, int invokingState)
			: base(parent, invokingState)
		{
		}
		public override int GetRuleIndex() { return RULE_hexColor; }
		public override void EnterRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.EnterHexColor(this);
		}
		public override void ExitRule(IParseTreeListener listener) {
			ICSSListener typedListener = listener as ICSSListener;
			if (typedListener != null) typedListener.ExitHexColor(this);
		}
		public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor) {
			ICSSVisitor<TResult> typedVisitor = visitor as ICSSVisitor<TResult>;
			if (typedVisitor != null) return typedVisitor.VisitHexColor(this);
			else return visitor.VisitChildren(this);
		}
	}

	[RuleVersion(0)]
	public HexColorContext hexColor() {
		HexColorContext _localctx = new HexColorContext(_ctx, State);
		EnterRule(_localctx, 50, RULE_hexColor);
		try {
			EnterOuterAlt(_localctx, 1);
			{
			State = 247; Match(HASH);
			}
		}
		catch (RecognitionException re) {
			_localctx.exception = re;
			_errHandler.ReportError(this, re);
			_errHandler.Recover(this, re);
		}
		finally {
			ExitRule();
		}
		return _localctx;
	}

	public static readonly string _serializedATN =
		"\x5\x3+\xFC\x4\x2\t\x2\x4\x3\t\x3\x4\x4\t\x4\x4\x5\t\x5\x4\x6\t\x6\x4"+
		"\a\t\a\x4\b\t\b\x4\t\t\t\x4\n\t\n\x4\v\t\v\x4\f\t\f\x4\r\t\r\x4\xE\t\xE"+
		"\x4\xF\t\xF\x4\x10\t\x10\x4\x11\t\x11\x4\x12\t\x12\x4\x13\t\x13\x4\x14"+
		"\t\x14\x4\x15\t\x15\x4\x16\t\x16\x4\x17\t\x17\x4\x18\t\x18\x4\x19\t\x19"+
		"\x4\x1A\t\x1A\x4\x1B\t\x1B\x3\x2\x3\x2\a\x2\x39\n\x2\f\x2\xE\x2<\v\x2"+
		"\x3\x2\x3\x2\x3\x2\x3\x3\x3\x3\x3\x3\x3\x3\x5\x3\x45\n\x3\x3\x4\x3\x4"+
		"\x3\x4\x3\x4\x3\x4\a\x4L\n\x4\f\x4\xE\x4O\v\x4\x5\x4Q\n\x4\x3\x4\x3\x4"+
		"\x3\x5\x3\x5\x3\x5\x3\x5\a\x5Y\n\x5\f\x5\xE\x5\\\v\x5\x3\x5\x3\x5\x3\x5"+
		"\x3\x5\x3\x6\x3\x6\x3\a\a\a\x65\n\a\f\a\xE\ah\v\a\x3\b\x3\b\x3\b\x5\b"+
		"m\n\b\x3\t\x3\t\x5\tq\n\t\x3\t\x3\t\x3\t\x3\t\x3\t\x3\t\a\ty\n\t\f\t\xE"+
		"\t|\v\t\x3\t\x3\t\x3\n\x3\n\x3\n\x3\v\x3\v\x3\v\x5\v\x86\n\v\x3\f\x3\f"+
		"\x3\f\x5\f\x8B\n\f\x3\r\x3\r\x3\xE\x3\xE\x3\xF\x3\xF\x3\xF\a\xF\x94\n"+
		"\xF\f\xF\xE\xF\x97\v\xF\x3\xF\x3\xF\x3\xF\x3\xF\x3\xF\x3\xF\a\xF\x9F\n"+
		"\xF\f\xF\xE\xF\xA2\v\xF\x3\xF\x3\xF\x3\x10\x3\x10\x3\x10\x3\x10\a\x10"+
		"\xAA\n\x10\f\x10\xE\x10\xAD\v\x10\x3\x11\x3\x11\a\x11\xB1\n\x11\f\x11"+
		"\xE\x11\xB4\v\x11\x3\x11\x6\x11\xB7\n\x11\r\x11\xE\x11\xB8\x5\x11\xBB"+
		"\n\x11\x3\x12\x3\x12\x3\x12\x3\x12\x5\x12\xC1\n\x12\x3\x13\x3\x13\x3\x13"+
		"\x3\x14\x3\x14\x3\x15\x3\x15\x3\x15\x3\x15\x5\x15\xCC\n\x15\x3\x15\x3"+
		"\x15\x3\x16\x3\x16\x3\x16\x3\x16\x5\x16\xD4\n\x16\x3\x16\x5\x16\xD7\n"+
		"\x16\x3\x17\x3\x17\x3\x17\x3\x17\x5\x17\xDD\n\x17\x3\x18\x3\x18\x3\x19"+
		"\x3\x19\x3\x19\x3\x19\a\x19\xE5\n\x19\f\x19\xE\x19\xE8\v\x19\x3\x1A\x5"+
		"\x1A\xEB\n\x1A\x3\x1A\x3\x1A\x3\x1A\x3\x1A\x3\x1A\x3\x1A\x3\x1A\x5\x1A"+
		"\xF4\n\x1A\x3\x1A\x3\x1A\x5\x1A\xF8\n\x1A\x3\x1B\x3\x1B\x3\x1B\x2\x2\x2"+
		"\x1C\x2\x2\x4\x2\x6\x2\b\x2\n\x2\f\x2\xE\x2\x10\x2\x12\x2\x14\x2\x16\x2"+
		"\x18\x2\x1A\x2\x1C\x2\x1E\x2 \x2\"\x2$\x2&\x2(\x2*\x2,\x2.\x2\x30\x2\x32"+
		"\x2\x34\x2\x2\b\x4\x18\x18))\x3\x11\x12\x4\x13\x13\x19\x19\x4\x6\a\r\r"+
		"\x3\x18\x19\x4 %\'(\x103\x2\x36\x3\x2\x2\x2\x4\x44\x3\x2\x2\x2\x6\x46"+
		"\x3\x2\x2\x2\bT\x3\x2\x2\x2\n\x61\x3\x2\x2\x2\f\x66\x3\x2\x2\x2\xEl\x3"+
		"\x2\x2\x2\x10n\x3\x2\x2\x2\x12\x7F\x3\x2\x2\x2\x14\x85\x3\x2\x2\x2\x16"+
		"\x8A\x3\x2\x2\x2\x18\x8C\x3\x2\x2\x2\x1A\x8E\x3\x2\x2\x2\x1C\x90\x3\x2"+
		"\x2\x2\x1E\xA5\x3\x2\x2\x2 \xBA\x3\x2\x2\x2\"\xC0\x3\x2\x2\x2$\xC2\x3"+
		"\x2\x2\x2&\xC5\x3\x2\x2\x2(\xC7\x3\x2\x2\x2*\xCF\x3\x2\x2\x2,\xD8\x3\x2"+
		"\x2\x2.\xDE\x3\x2\x2\x2\x30\xE0\x3\x2\x2\x2\x32\xF7\x3\x2\x2\x2\x34\xF9"+
		"\x3\x2\x2\x2\x36:\x5\x4\x3\x2\x37\x39\x5\x6\x4\x2\x38\x37\x3\x2\x2\x2"+
		"\x39<\x3\x2\x2\x2:\x38\x3\x2\x2\x2:;\x3\x2\x2\x2;=\x3\x2\x2\x2<:\x3\x2"+
		"\x2\x2=>\x5\f\a\x2>?\a\x1\x2\x2?\x3\x3\x2\x2\x2@\x41\a\x1E\x2\x2\x41\x42"+
		"\a\x18\x2\x2\x42\x45\a\xE\x2\x2\x43\x45\x3\x2\x2\x2\x44@\x3\x2\x2\x2\x44"+
		"\x43\x3\x2\x2\x2\x45\x5\x3\x2\x2\x2\x46G\a\x1B\x2\x2GP\t\x2\x2\x2HM\x5"+
		"\n\x6\x2IJ\a\x16\x2\x2JL\x5\n\x6\x2KI\x3\x2\x2\x2LO\x3\x2\x2\x2MK\x3\x2"+
		"\x2\x2MN\x3\x2\x2\x2NQ\x3\x2\x2\x2OM\x3\x2\x2\x2PH\x3\x2\x2\x2PQ\x3\x2"+
		"\x2\x2QR\x3\x2\x2\x2RS\a\xE\x2\x2S\a\x3\x2\x2\x2TU\a\x1D\x2\x2UZ\x5\n"+
		"\x6\x2VW\a\x16\x2\x2WY\x5\n\x6\x2XV\x3\x2\x2\x2Y\\\x3\x2\x2\x2ZX\x3\x2"+
		"\x2\x2Z[\x3\x2\x2\x2[]\x3\x2\x2\x2\\Z\x3\x2\x2\x2]^\a\t\x2\x2^_\x5\x1C"+
		"\xF\x2_`\a\n\x2\x2`\t\x3\x2\x2\x2\x61\x62\a\x19\x2\x2\x62\v\x3\x2\x2\x2"+
		"\x63\x65\x5\xE\b\x2\x64\x63\x3\x2\x2\x2\x65h\x3\x2\x2\x2\x66\x64\x3\x2"+
		"\x2\x2\x66g\x3\x2\x2\x2g\r\x3\x2\x2\x2h\x66\x3\x2\x2\x2im\x5\x1C\xF\x2"+
		"jm\x5\b\x5\x2km\x5\x10\t\x2li\x3\x2\x2\x2lj\x3\x2\x2\x2lk\x3\x2\x2\x2"+
		"m\xF\x3\x2\x2\x2np\a\x1C\x2\x2oq\x5\x12\n\x2po\x3\x2\x2\x2pq\x3\x2\x2"+
		"\x2qr\x3\x2\x2\x2rs\a\t\x2\x2st\x5,\x17\x2tz\a\xE\x2\x2uv\x5,\x17\x2v"+
		"w\a\xE\x2\x2wy\x3\x2\x2\x2xu\x3\x2\x2\x2y|\x3\x2\x2\x2zx\x3\x2\x2\x2z"+
		"{\x3\x2\x2\x2{}\x3\x2\x2\x2|z\x3\x2\x2\x2}~\a\n\x2\x2~\x11\x3\x2\x2\x2"+
		"\x7F\x80\a\xF\x2\x2\x80\x81\a\x19\x2\x2\x81\x13\x3\x2\x2\x2\x82\x86\a"+
		"\x10\x2\x2\x83\x86\a\x16\x2\x2\x84\x86\x3\x2\x2\x2\x85\x82\x3\x2\x2\x2"+
		"\x85\x83\x3\x2\x2\x2\x85\x84\x3\x2\x2\x2\x86\x15\x3\x2\x2\x2\x87\x8B\a"+
		"\x12\x2\x2\x88\x8B\a\b\x2\x2\x89\x8B\x3\x2\x2\x2\x8A\x87\x3\x2\x2\x2\x8A"+
		"\x88\x3\x2\x2\x2\x8A\x89\x3\x2\x2\x2\x8B\x17\x3\x2\x2\x2\x8C\x8D\t\x3"+
		"\x2\x2\x8D\x19\x3\x2\x2\x2\x8E\x8F\a\x19\x2\x2\x8F\x1B\x3\x2\x2\x2\x90"+
		"\x95\x5\x1E\x10\x2\x91\x92\a\x16\x2\x2\x92\x94\x5\x1E\x10\x2\x93\x91\x3"+
		"\x2\x2\x2\x94\x97\x3\x2\x2\x2\x95\x93\x3\x2\x2\x2\x95\x96\x3\x2\x2\x2"+
		"\x96\x98\x3\x2\x2\x2\x97\x95\x3\x2\x2\x2\x98\x99\a\t\x2\x2\x99\x9A\x5"+
		",\x17\x2\x9A\xA0\a\xE\x2\x2\x9B\x9C\x5,\x17\x2\x9C\x9D\a\xE\x2\x2\x9D"+
		"\x9F\x3\x2\x2\x2\x9E\x9B\x3\x2\x2\x2\x9F\xA2\x3\x2\x2\x2\xA0\x9E\x3\x2"+
		"\x2\x2\xA0\xA1\x3\x2\x2\x2\xA1\xA3\x3\x2\x2\x2\xA2\xA0\x3\x2\x2\x2\xA3"+
		"\xA4\a\n\x2\x2\xA4\x1D\x3\x2\x2\x2\xA5\xAB\x5 \x11\x2\xA6\xA7\x5\x16\f"+
		"\x2\xA7\xA8\x5 \x11\x2\xA8\xAA\x3\x2\x2\x2\xA9\xA6\x3\x2\x2\x2\xAA\xAD"+
		"\x3\x2\x2\x2\xAB\xA9\x3\x2\x2\x2\xAB\xAC\x3\x2\x2\x2\xAC\x1F\x3\x2\x2"+
		"\x2\xAD\xAB\x3\x2\x2\x2\xAE\xB2\x5&\x14\x2\xAF\xB1\x5\"\x12\x2\xB0\xAF"+
		"\x3\x2\x2\x2\xB1\xB4\x3\x2\x2\x2\xB2\xB0\x3\x2\x2\x2\xB2\xB3\x3\x2\x2"+
		"\x2\xB3\xBB\x3\x2\x2\x2\xB4\xB2\x3\x2\x2\x2\xB5\xB7\x5\"\x12\x2\xB6\xB5"+
		"\x3\x2\x2\x2\xB7\xB8\x3\x2\x2\x2\xB8\xB6\x3\x2\x2\x2\xB8\xB9\x3\x2\x2"+
		"\x2\xB9\xBB\x3\x2\x2\x2\xBA\xAE\x3\x2\x2\x2\xBA\xB6\x3\x2\x2\x2\xBB!\x3"+
		"\x2\x2\x2\xBC\xC1\a\x1A\x2\x2\xBD\xC1\x5$\x13\x2\xBE\xC1\x5(\x15\x2\xBF"+
		"\xC1\x5*\x16\x2\xC0\xBC\x3\x2\x2\x2\xC0\xBD\x3\x2\x2\x2\xC0\xBE\x3\x2"+
		"\x2\x2\xC0\xBF\x3\x2\x2\x2\xC1#\x3\x2\x2\x2\xC2\xC3\a\x17\x2\x2\xC3\xC4"+
		"\a\x19\x2\x2\xC4%\x3\x2\x2\x2\xC5\xC6\t\x4\x2\x2\xC6\'\x3\x2\x2\x2\xC7"+
		"\xC8\a\v\x2\x2\xC8\xCB\a\x19\x2\x2\xC9\xCA\t\x5\x2\x2\xCA\xCC\t\x6\x2"+
		"\x2\xCB\xC9\x3\x2\x2\x2\xCB\xCC\x3\x2\x2\x2\xCC\xCD\x3\x2\x2\x2\xCD\xCE"+
		"\a\f\x2\x2\xCE)\x3\x2\x2\x2\xCF\xD0\a\xF\x2\x2\xD0\xD6\a\x19\x2\x2\xD1"+
		"\xD3\a\x14\x2\x2\xD2\xD4\a\x19\x2\x2\xD3\xD2\x3\x2\x2\x2\xD3\xD4\x3\x2"+
		"\x2\x2\xD4\xD5\x3\x2\x2\x2\xD5\xD7\a\x15\x2\x2\xD6\xD1\x3\x2\x2\x2\xD6"+
		"\xD7\x3\x2\x2\x2\xD7+\x3\x2\x2\x2\xD8\xD9\x5\x1A\xE\x2\xD9\xDA\a\xF\x2"+
		"\x2\xDA\xDC\x5\x30\x19\x2\xDB\xDD\x5.\x18\x2\xDC\xDB\x3\x2\x2\x2\xDC\xDD"+
		"\x3\x2\x2\x2\xDD-\x3\x2\x2\x2\xDE\xDF\a\x1F\x2\x2\xDF/\x3\x2\x2\x2\xE0"+
		"\xE6\x5\x32\x1A\x2\xE1\xE2\x5\x14\v\x2\xE2\xE3\x5\x32\x1A\x2\xE3\xE5\x3"+
		"\x2\x2\x2\xE4\xE1\x3\x2\x2\x2\xE5\xE8\x3\x2\x2\x2\xE6\xE4\x3\x2\x2\x2"+
		"\xE6\xE7\x3\x2\x2\x2\xE7\x31\x3\x2\x2\x2\xE8\xE6\x3\x2\x2\x2\xE9\xEB\x5"+
		"\x18\r\x2\xEA\xE9\x3\x2\x2\x2\xEA\xEB\x3\x2\x2\x2\xEB\xEC\x3\x2\x2\x2"+
		"\xEC\xF8\t\a\x2\x2\xED\xF8\a\x18\x2\x2\xEE\xF3\a\x19\x2\x2\xEF\xF0\a\x14"+
		"\x2\x2\xF0\xF1\x5\x30\x19\x2\xF1\xF2\a\x15\x2\x2\xF2\xF4\x3\x2\x2\x2\xF3"+
		"\xEF\x3\x2\x2\x2\xF3\xF4\x3\x2\x2\x2\xF4\xF8\x3\x2\x2\x2\xF5\xF8\a)\x2"+
		"\x2\xF6\xF8\x5\x34\x1B\x2\xF7\xEA\x3\x2\x2\x2\xF7\xED\x3\x2\x2\x2\xF7"+
		"\xEE\x3\x2\x2\x2\xF7\xF5\x3\x2\x2\x2\xF7\xF6\x3\x2\x2\x2\xF8\x33\x3\x2"+
		"\x2\x2\xF9\xFA\a\x1A\x2\x2\xFA\x35\x3\x2\x2\x2\x1C:\x44MPZ\x66lpz\x85"+
		"\x8A\x95\xA0\xAB\xB2\xB8\xBA\xC0\xCB\xD3\xD6\xDC\xE6\xEA\xF3\xF7";
	public static readonly ATN _ATN =
		ATNSimulator.Deserialize(_serializedATN.ToCharArray());
}
} // namespace CSSParserAntlr
