
#line  1 "cs.ATG" 
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;
using System;
using System.Reflection;

namespace ICSharpCode.SharpRefactory.Parser {



public class Parser
{
	const int maxT = 125;

	const  bool   T            = true;
	const  bool   x            = false;
	const  int    minErrDist   = 2;
	const  string errMsgFormat = "-- line {0} col {1}: {2}";  // 0=line, 1=column, 2=text
	int    errDist             = minErrDist;
	Errors errors;
	Lexer  lexer;

	public Errors Errors {
		get {
			return errors;
		}
	}


#line  10 "cs.ATG" 
string assemblyName = null;

public CompilationUnit compilationUnit;

public string ContainingAssembly {
	set {
		assemblyName = value;
	}
}

Token t {
	get {
		return lexer.Token;
	}
}
Token la {
	get {
		return lexer.LookAhead;
	}
}

Hashtable typeStrings     = null;
ArrayList usingNamespaces = null;
ArrayList externAliases = null;

public void Error(string s)
{
	if (errDist >= minErrDist) {
		errors.Error(la.line, la.col, s);
	}
	errDist = 0;
}

public Expression ParseExpression(Lexer lexer)
{
	this.errors = lexer.Errors;
	this.lexer = lexer;
	errors.SynErr = new ErrorCodeProc(SynErr);
	lexer.NextToken();
	Expression expr;
	Expr(out expr);
	return expr;
}

bool IsTypeCast()
{
	if (IsSimpleTypeCast()) {
		return true;
	}
	
	if (assemblyName != null) {
		return CheckTypeCast();
	}
	
	return GuessTypeCast();
}

bool IsSimpleTypeCast()
{
	// check: "(" pointer or array of keyword type ")"
	
	if (la.kind != Tokens.OpenParenthesis) {
		return false;
	}

	StartPeek();
	Token pt1 = Peek();
	Token pt  = Peek();
	
	return ParserUtil.IsTypeKW(pt1) && IsPointerOrDims(ref pt) &&
	       pt.kind == Tokens.CloseParenthesis;
}

bool CheckTypeCast()
{
	// check: leading "(" pointer or array of some type ")"

	if (la.kind != Tokens.OpenParenthesis) {
		return false;
	}
	
	string qualident;
	
	StartPeek();
	Token pt = Peek();
	
	return IsQualident(ref pt, out qualident) && IsPointerOrDims(ref pt) && 
	       pt.kind == Tokens.CloseParenthesis && IsType(qualident);		
}

bool IsType(string qualident)
{
	if (typeStrings == null) {
		CreateTypeStrings();
	}
	
	if (typeStrings.ContainsValue(qualident)) {
		return true;
	}
	
	foreach (string ns in usingNamespaces) {
		if (typeStrings.ContainsValue(ns + "." + qualident)) {
			return true;
		}
	}
	return false;
}

bool GuessTypeCast()
{
	// check: "(" pointer or array of some type ")" possible type cast successor
	
	if (la.kind != Tokens.OpenParenthesis) return false;
	
	string qualident;
	
	StartPeek();
	Token pt = Peek();
	
	if (IsQualident(ref pt, out qualident) && IsPointerOrDims(ref pt) && 
	    pt.kind == Tokens.CloseParenthesis) {
		// check successor
		pt = Peek();
		return pt.kind == Tokens.Identifier || pt.kind == Tokens.Literal   ||
		       pt.kind == Tokens.OpenParenthesis   || ParserUtil.IsUnaryOperator(pt)         ||
		       pt.kind == Tokens.New        || pt.kind == Tokens.This      ||
		       pt.kind == Tokens.Base       || pt.kind == Tokens.Null      ||
		       pt.kind == Tokens.Checked    || pt.kind == Tokens.Unchecked ||
		       pt.kind == Tokens.Typeof     || pt.kind == Tokens.Sizeof    ||
		       (ParserUtil.IsTypeKW(pt) && Peek().kind == Tokens.Dot);
	} else return false;
}

void CreateTypeStrings()
{
	Assembly a;
	Type[] types;
	AssemblyName [] aNames;
	
	if (assemblyName != null && assemblyName.Length > 0) {    /* AW 2002-12-30 add check for length > 0 */
		typeStrings = new Hashtable();
		a = Assembly.LoadFrom(assemblyName);
		types = a.GetTypes();
		foreach (Type t in types) 
			typeStrings.Add(t.FullName.GetHashCode(), t.FullName);
		aNames = a.GetReferencedAssemblies();
		
		for (int i = 0; i < aNames.Length; i++) {
			a = Assembly.LoadFrom(aNames[i].Name);
			types = a.GetExportedTypes();
			
			foreach(Type t in types)
				if (usingNamespaces.Contains(t.FullName.Substring(0, t.FullName.LastIndexOf('.'))))
					typeStrings.Add(t.FullName.GetHashCode(), t.FullName);
		}
	}
}

/* Checks whether the next sequences of tokens is a qualident *
 * and returns the qualident string                           */
/* !!! Proceeds from current peek position !!! */
bool IsQualident (ref Token pt, out string qualident)
{
	qualident = "";
	if (pt.kind == Tokens.Identifier) {
		qualident = pt.val;
		pt = Peek();
		while (pt.kind == Tokens.Dot) {
			pt = Peek();
			if (pt.kind != Tokens.Identifier) return false;
			qualident += "." + pt.val;
			pt = Peek();
		}
		return true;
	} else return false;
}

/* skip: { "*" | "[" { "," } "]" } */
/* !!! Proceeds from current peek position !!! */
bool IsPointerOrDims (ref Token pt)
{
	for (;;) {
		if (pt.kind == Tokens.OpenSquareBracket) {
			do pt = Peek();
			while (pt.kind == Tokens.Comma);
			if (pt.kind != Tokens.CloseSquareBracket) return false;
		} else if (pt.kind != Tokens.Times) break;
		pt = Peek();
	}
	return true;
}

/* Return the n-th token after the current lookahead token */
void StartPeek()
{
	lexer.StartPeek();
}

Token Peek()
{
	return lexer.Peek();
}

Token Peek (int n)
{
	lexer.StartPeek();
	Token x = la;
	while (n > 0) {
		x = lexer.Peek();
		n--;
	}
	return x;
}

/*-----------------------------------------------------------------*
 * Resolver routines to resolve LL(1) conflicts:                   *                                                  *
 * These resolution routine return a boolean value that indicates  *
 * whether the alternative at hand shall be choosen or not.        *
 * They are used in IF ( ... ) expressions.                        *       
 *-----------------------------------------------------------------*/

/* True, if ident is followed by "=" */
bool IdentAndAsgn ()
{
	return la.kind == Tokens.Identifier && Peek(1).kind == Tokens.Assign;
}

bool IsAssignment () { return IdentAndAsgn(); }

/* True, if ident is followed by ",", "=", or ";" */
bool IdentAndCommaOrAsgnOrSColon () {
	int peek = Peek(1).kind;
	return la.kind == Tokens.Identifier && 
	       (peek == Tokens.Comma || peek == Tokens.Assign || peek == Tokens.Semicolon);
}
bool IsVarDecl () { return IdentAndCommaOrAsgnOrSColon(); }

/* True, if the comma is not a trailing one, *
 * like the last one in: a, b, c,            */
bool NotFinalComma () {
	int peek = Peek(1).kind;
	return la.kind == Tokens.Comma &&
	       peek != Tokens.CloseCurlyBrace && peek != Tokens.CloseSquareBracket;
}

/* True, if "void" is followed by "*" */
bool NotVoidPointer () {
	return la.kind == Tokens.Void && Peek(1).kind != Tokens.Times;
}

/* True, if "checked" or "unchecked" are followed by "{" */
bool UnCheckedAndLBrace () {
	return la.kind == Tokens.Checked || la.kind == Tokens.Unchecked &&
	       Peek(1).kind == Tokens.OpenCurlyBrace;
}

/* True, if "." is followed by an ident */
bool DotAndIdent () {
	return la.kind == Tokens.Dot && Peek(1).kind == Tokens.Identifier;
}

/* True, if ident is followed by ":" */
bool IdentAndColon () {
	return la.kind == Tokens.Identifier && Peek(1).kind == Tokens.Colon;
}

bool IsLabel () { return IdentAndColon(); }

/* True, if ident is followed by "(" */
bool IdentAndLPar () {
	return la.kind == Tokens.Identifier && Peek(1).kind == Tokens.OpenParenthesis;
}

/* True, if "catch" is followed by "(" */
bool CatchAndLPar () {
	return la.kind == Tokens.Catch && Peek(1).kind == Tokens.OpenParenthesis;
}
bool IsTypedCatch () { return CatchAndLPar(); }

/* True, if "[" is followed by the ident "assembly" */
bool IsGlobalAttrTarget () {
	Token pt = Peek(1);
	return la.kind == Tokens.OpenSquareBracket && 
	       pt.kind == Tokens.Identifier && pt.val == "assembly";
}

/* True, if "[" is followed by "," or "]" */
bool LBrackAndCommaOrRBrack () {
	int peek = Peek(1).kind;
	return la.kind == Tokens.OpenSquareBracket &&
	       (peek == Tokens.Comma || peek == Tokens.CloseSquareBracket);
}

bool IsDims () { return LBrackAndCommaOrRBrack(); }

/* True, if "[" is followed by "," or "]" *
 * or if the current token is "*"         */
bool TimesOrLBrackAndCommaOrRBrack () {
	return la.kind == Tokens.Times || LBrackAndCommaOrRBrack();
}
bool IsPointerOrDims () { return TimesOrLBrackAndCommaOrRBrack(); }
bool IsPointer () { return la.kind == Tokens.Times; }

/* True, if lookahead is a primitive type keyword, or *
 * if it is a type declaration followed by an ident   */
bool IsLocalVarDecl () {
	if ((ParserUtil.IsTypeKW(la) && Peek(1).kind != Tokens.Dot) || la.kind == Tokens.Void) return true;
	
	StartPeek();
	Token pt = la ;  // peek token
	string ignore;
	
	return IsQualident(ref pt, out ignore) && IsPointerOrDims(ref pt) && 
	       pt.kind == Tokens.Identifier;
}

/* True, if lookahead ident is "yield" */
bool IdentIsYield () {
	return la.kind == Tokens.Identifier && la.val == "yield";
}

/* True, if lookahead ident is "partial" */
bool IdentIsPartial () {
	return la.kind == Tokens.Identifier && la.val == "partial";
}

/* True, if lookahead ident is "get" */
bool IdentIsGet () {
	return la.kind == Tokens.Identifier && la.val == "get";
}

/* True, if lookahead ident is "set" */
bool IdentIsSet () {
	return la.kind == Tokens.Identifier && la.val == "set";
}

/* True, if lookahead ident is "add" */
bool IdentIsAdd () {
	return la.kind == Tokens.Identifier && la.val == "add";
}

/* True, if lookahead ident is "remove" */
bool IdentIsRemove () {
	return la.kind == Tokens.Identifier && la.val == "remove";
}

/* True, if lookahead is a local attribute target specifier, *
 * i.e. one of "event", "return", "field", "method",         *
 *             "module", "param", "property", or "type"      */
bool IsLocalAttrTarget () {
	int cur = la.kind;
	string val = la.val;

	return (cur == Tokens.Event || cur == Tokens.Return ||
	        (cur == Tokens.Identifier &&
	         (val == "field" || val == "method"   || val == "module" ||
	          val == "param" || val == "property" || val == "type"))) &&
	       Peek(1).kind == Tokens.Colon;
}


/*------------------------------------------------------------------------*
 *----- LEXER TOKEN LIST  ------------------------------------------------*
 *------------------------------------------------------------------------*/


/*

*/
	void SynErr(int n)
	{
		if (errDist >= minErrDist) {
			errors.SynErr(lexer.LookAhead.line, lexer.LookAhead.col, n);
		}
		errDist = 0;
	}

	public void SemErr(string msg)
	{
		if (errDist >= minErrDist) {
			errors.Error(lexer.Token.line, lexer.Token.col, msg);
		}
		errDist = 0;
	}
	
	void Expect(int n)
	{
		if (lexer.LookAhead.kind == n) {
			lexer.NextToken();
		} else {
			SynErr(n);
		}
	}
	
	bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	void ExpectWeak(int n, int follow)
	{
		if (lexer.LookAhead.kind == n) {
			lexer.NextToken();
		} else {
			SynErr(n);
			while (!StartOf(follow)) {
				lexer.NextToken();
			}
		}
	}
	
	bool WeakSeparator(int n, int syFol, int repFol)
	{
		bool[] s = new bool[maxT + 1];
		
		if (lexer.LookAhead.kind == n) {
			lexer.NextToken();
			return true; 
		} else if (StartOf(repFol)) {
			return false;
		} else {
			for (int i = 0; i <= maxT; i++) {
				s[i] = set[syFol, i] || set[repFol, i] || set[0, i];
			}
			SynErr(n);
			while (!s[lexer.LookAhead.kind]) {
				lexer.NextToken();
			}
			return StartOf(syFol);
		}
	}
	
	void CS() {

#line  527 "cs.ATG" 
		compilationUnit = new CompilationUnit(); 
		while (la.kind == 70) {
			ExternAlias();
		}
		while (la.kind == 120) {
			UsingDirective();
		}
		while (
#line  531 "cs.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void ExternAlias() {

#line  538 "cs.ATG" 
		externAliases = new ArrayList ();
		
		Expect(70);
		Expect(1);

#line  541 "cs.ATG" 
		if (t.val != "alias") Error ("alias expected"); 
		Expect(1);

#line  542 "cs.ATG" 
		externAliases.Add (new ExternAliasDeclaration (t.val)); 
		Expect(10);
	}

	void UsingDirective() {

#line  548 "cs.ATG" 
		usingNamespaces = new ArrayList();
		string qualident = null, aliasident = null;
		
		Expect(120);

#line  552 "cs.ATG" 
		Point startPos = t.Location;
		INode node     = null; 
		
		if (
#line  555 "cs.ATG" 
IsAssignment()) {
			lexer.NextToken();

#line  555 "cs.ATG" 
			aliasident = t.val; 
			Expect(3);
		}
		Qualident(
#line  556 "cs.ATG" 
out qualident);

#line  556 "cs.ATG" 
		if (qualident != null && qualident.Length > 0) {
		 if (aliasident != null) {
		   node = new UsingAliasDeclaration(aliasident, qualident);
		 } else {
		     usingNamespaces.Add(qualident);
		     node = new UsingDeclaration(qualident);
		 }
		}
		
		Expect(10);

#line  565 "cs.ATG" 
		node.StartLocation = startPos;
		node.EndLocation   = t.EndLocation;
		compilationUnit.AddChild(node);
		
	}

	void GlobalAttributeSection() {
		Expect(16);

#line  574 "cs.ATG" 
		Point startPos = t.Location; 
		Expect(1);

#line  574 "cs.ATG" 
		if (t.val != "assembly") Error("global attribute target specifier (\"assembly\") expected");
		string attributeTarget = t.val;
		ArrayList attributes = new ArrayList();
		ICSharpCode.SharpRefactory.Parser.AST.Attribute attribute;
		
		Expect(9);
		Attribute(
#line  579 "cs.ATG" 
out attribute);

#line  579 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  580 "cs.ATG" 
NotFinalComma()) {
			Expect(12);
			Attribute(
#line  580 "cs.ATG" 
out attribute);

#line  580 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(17);

#line  582 "cs.ATG" 
		AttributeSection section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  668 "cs.ATG" 
		AttributeSection section;
		ArrayList attributes = new ArrayList();
		ModifierCollection m = new ModifierCollection ();
		string qualident;
		
		if (la.kind == 87) {
			lexer.NextToken();

#line  674 "cs.ATG" 
			Point startPos = t.Location; 
			Qualident(
#line  675 "cs.ATG" 
out qualident);

#line  675 "cs.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			compilationUnit.AddChild(node);
			compilationUnit.BlockStart(node);
			
			Expect(14);
			while (la.kind == 120) {
				UsingDirective();
			}
			while (StartOf(1)) {
				NamespaceMemberDecl();
			}
			Expect(15);
			if (la.kind == 10) {
				lexer.NextToken();
			}

#line  684 "cs.ATG" 
			node.EndLocation   = t.EndLocation;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 16) {
				AttributeSection(
#line  688 "cs.ATG" 
out section);

#line  688 "cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  689 "cs.ATG" 
m);
			}

#line  690 "cs.ATG" 
			bool partial = false; 
			if (
#line  691 "cs.ATG" 
IdentIsPartial ()) {
				lexer.NextToken();

#line  691 "cs.ATG" 
				partial = true; 
			}
			TypeDecl(
#line  692 "cs.ATG" 
m, attributes, partial);
		} else SynErr(126);
	}

	void Qualident(
#line  783 "cs.ATG" 
out string qualident) {
		Expect(1);

#line  785 "cs.ATG" 
		StringBuilder qualidentBuilder = new StringBuilder(t.val); 
		while (
#line  786 "cs.ATG" 
DotAndIdent()) {
			Expect(13);
			Expect(1);

#line  786 "cs.ATG" 
			qualidentBuilder.Append('.');
			qualidentBuilder.Append(t.val); 
			
		}

#line  789 "cs.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void Attribute(
#line  589 "cs.ATG" 
out ICSharpCode.SharpRefactory.Parser.AST.Attribute attribute) {

#line  590 "cs.ATG" 
		string qualident; Point start = t.Location; 
		Qualident(
#line  592 "cs.ATG" 
out qualident);

#line  592 "cs.ATG" 
		ArrayList positional = new ArrayList();
		ArrayList named      = new ArrayList();
		string name = qualident;
		
		if (la.kind == 18) {
			AttributeArguments(
#line  596 "cs.ATG" 
ref positional, ref named);
		}

#line  597 "cs.ATG" 
		attribute  = new ICSharpCode.SharpRefactory.Parser.AST.Attribute(name, positional, named);
		attribute.StartLocation = start;
		attribute.EndLocation = t.EndLocation;
		
	}

	void AttributeArguments(
#line  603 "cs.ATG" 
ref ArrayList positional, ref ArrayList named) {

#line  605 "cs.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(18);
		if (StartOf(4)) {
			if (
#line  613 "cs.ATG" 
IsAssignment()) {

#line  613 "cs.ATG" 
				nameFound = true; 
				lexer.NextToken();

#line  614 "cs.ATG" 
				name = t.val; 
				Expect(3);
			}
			Expr(
#line  616 "cs.ATG" 
out expr);

#line  616 "cs.ATG" 
			if(name == "") positional.Add(expr);
			else { named.Add(new NamedArgument(name, expr)); name = ""; }
			
			while (la.kind == 12) {
				lexer.NextToken();
				if (
#line  623 "cs.ATG" 
IsAssignment()) {

#line  623 "cs.ATG" 
					nameFound = true; 
					Expect(1);

#line  624 "cs.ATG" 
					name = t.val; 
					Expect(3);
				} else if (StartOf(4)) {

#line  626 "cs.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(127);
				Expr(
#line  627 "cs.ATG" 
out expr);

#line  627 "cs.ATG" 
				if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgument(name, expr)); name = ""; }
				
			}
		}
		Expect(19);
	}

	void Expr(
#line  1790 "cs.ATG" 
out Expression expr) {

#line  1791 "cs.ATG" 
		expr = null; Expression expr1 = null, expr2 = null; 
		UnaryExpr(
#line  1793 "cs.ATG" 
out expr);
		if (StartOf(5)) {
			ConditionalOrExpr(
#line  1796 "cs.ATG" 
ref expr);
			if (la.kind == 11) {
				lexer.NextToken();
				Expr(
#line  1796 "cs.ATG" 
out expr1);
				Expect(9);
				Expr(
#line  1796 "cs.ATG" 
out expr2);

#line  1796 "cs.ATG" 
				expr = new ConditionalExpression(expr, expr1, expr2);  
			}
		} else if (StartOf(6)) {

#line  1798 "cs.ATG" 
			AssignmentOperatorType op; Expression val; 
			AssignmentOperator(
#line  1798 "cs.ATG" 
out op);
			Expr(
#line  1798 "cs.ATG" 
out val);

#line  1798 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, val); 
		} else SynErr(128);
	}

	void AttributeSection(
#line  635 "cs.ATG" 
out AttributeSection section) {

#line  637 "cs.ATG" 
		string attributeTarget = "";
		ArrayList attributes = new ArrayList();
		ICSharpCode.SharpRefactory.Parser.AST.Attribute attribute;
		
		
		Expect(16);

#line  643 "cs.ATG" 
		Point startPos = t.Location; 
		if (
#line  644 "cs.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 68) {
				lexer.NextToken();

#line  645 "cs.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 100) {
				lexer.NextToken();

#line  646 "cs.ATG" 
				attributeTarget = "return";
			} else {
				lexer.NextToken();

#line  647 "cs.ATG" 
				if (t.val != "field"    || t.val != "method" ||
				  t.val != "module"   || t.val != "param"  ||
				  t.val != "property" || t.val != "type")
				Error("attribute target specifier (event, return, field," +
				      "method, module, param, property, or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(9);
		}
		Attribute(
#line  657 "cs.ATG" 
out attribute);

#line  657 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  658 "cs.ATG" 
NotFinalComma()) {
			Expect(12);
			Attribute(
#line  658 "cs.ATG" 
out attribute);

#line  658 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(17);

#line  660 "cs.ATG" 
		section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		
	}

	void TypeModifier(
#line  954 "cs.ATG" 
ModifierCollection m) {
		switch (la.kind) {
		case 88: {
			lexer.NextToken();

#line  956 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.New, t.Location, t.EndLocation), this); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  957 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Public, t.Location, t.EndLocation), this); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  958 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Protected, t.Location, t.EndLocation), this); 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  959 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Internal, t.Location, t.EndLocation), this); 
			break;
		}
		case 95: {
			lexer.NextToken();

#line  960 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Private, t.Location, t.EndLocation), this); 
			break;
		}
		case 118: {
			lexer.NextToken();

#line  961 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Unsafe, t.Location, t.EndLocation), this); 
			break;
		}
		case 48: {
			lexer.NextToken();

#line  962 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Abstract, t.Location, t.EndLocation), this); 
			break;
		}
		case 102: {
			lexer.NextToken();

#line  963 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Sealed, t.Location, t.EndLocation), this); 
			break;
		}
		case 106: {
			lexer.NextToken();

#line  964 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Static, t.Location, t.EndLocation), this); 
			break;
		}
		default: SynErr(129); break;
		}
	}

	void TypeDecl(
#line  695 "cs.ATG" 
ModifierCollection m, ArrayList attributes, bool partial) {

#line  697 "cs.ATG" 
		TypeReference type;
		StringCollection names;
		ArrayList p; string name;
		
		if (la.kind == 58) {

#line  701 "cs.ATG" 
			m.Check(ModifierFlags.Classes, this); 
			lexer.NextToken();

#line  702 "cs.ATG" 
			TypeDeclaration newType = new TypeDeclaration (partial);
			newType.DeclarationStartLocation = t.Location;
			                        compilationUnit.AddChild(newType);
			                        compilationUnit.BlockStart(newType);
			                        
			                        newType.Type = Types.Class;
			                        newType.Modifiers = m;
			                        newType.Attributes = attributes;
			                      
			Expect(1);

#line  711 "cs.ATG" 
			newType.Name = t.val; 
			if (la.kind == 9) {
				ClassBase(
#line  712 "cs.ATG" 
out names);

#line  712 "cs.ATG" 
				newType.BaseTypes = names; 
			}

#line  712 "cs.ATG" 
			newType.StartLocation = t.EndLocation; 
			ClassBody();
			if (la.kind == 10) {
				lexer.NextToken();
			}

#line  714 "cs.ATG" 
			newType.EndLocation = t.Location; 
			compilationUnit.BlockEnd();
			
		} else if (StartOf(7)) {

#line  717 "cs.ATG" 
			m.Check(ModifierFlags.StructsInterfacesEnumsDelegates, this); 
			if (la.kind == 108) {
				lexer.NextToken();

#line  718 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(partial);
				newType.DeclarationStartLocation = t.Location;
				                        compilationUnit.AddChild(newType);
				                        compilationUnit.BlockStart(newType);
				                        newType.Type = Types.Struct; 
				                        newType.Modifiers = m;
				                        newType.Attributes = attributes;
				                      
				Expect(1);

#line  726 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					StructInterfaces(
#line  727 "cs.ATG" 
out names);

#line  727 "cs.ATG" 
					newType.BaseTypes = names; 
				}

#line  727 "cs.ATG" 
				newType.StartLocation = t.EndLocation; 
				StructBody();
				if (la.kind == 10) {
					lexer.NextToken();
				}

#line  729 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 82) {
				lexer.NextToken();

#line  733 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(partial);
				newType.DeclarationStartLocation = t.Location;
				                        compilationUnit.AddChild(newType);
				                        compilationUnit.BlockStart(newType);
				                        newType.Type = Types.Interface;
				                        newType.Attributes = attributes;
				                        newType.Modifiers = m;
				Expect(1);

#line  740 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					InterfaceBase(
#line  741 "cs.ATG" 
out names);

#line  741 "cs.ATG" 
					newType.BaseTypes = names; 
				}

#line  741 "cs.ATG" 
				newType.StartLocation = t.EndLocation; 
				InterfaceBody();
				if (la.kind == 10) {
					lexer.NextToken();
				}

#line  743 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 67) {
				lexer.NextToken();

#line  747 "cs.ATG" 
				if (partial) Error ("partial is not allowed for enum types");
				TypeDeclaration newType = new TypeDeclaration();
				 	newType.DeclarationStartLocation = t.Location;
				                           compilationUnit.AddChild(newType);
				                           compilationUnit.BlockStart(newType);
				                           newType.Type = Types.Enum;
				                           newType.Attributes = attributes;
				                           newType.Modifiers = m;
				Expect(1);

#line  755 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					lexer.NextToken();
					IntegralType(
#line  756 "cs.ATG" 
out name);

#line  756 "cs.ATG" 
					newType.BaseTypes = new StringCollection(); 
					newType.BaseTypes.Add(name);
					
				}

#line  759 "cs.ATG" 
				newType.StartLocation = t.EndLocation; 
				EnumBody();
				if (la.kind == 10) {
					lexer.NextToken();
				}

#line  761 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else {
				lexer.NextToken();

#line  765 "cs.ATG" 
				if (partial) Error ("partial is not allowed for delegate types");
				DelegateDeclaration delegateDeclr = new DelegateDeclaration();
				 	delegateDeclr.DeclarationStartLocation = t.Location;
				                           delegateDeclr.Modifiers = m;
				                           delegateDeclr.Attributes = attributes;
				                        
				if (
#line  771 "cs.ATG" 
NotVoidPointer()) {
					Expect(122);

#line  771 "cs.ATG" 
					delegateDeclr.ReturnType = new TypeReference("void", 0, null); 
				} else if (StartOf(8)) {
					Type(
#line  772 "cs.ATG" 
out type);

#line  772 "cs.ATG" 
					delegateDeclr.ReturnType = type; 
				} else SynErr(130);
				Expect(1);

#line  774 "cs.ATG" 
				delegateDeclr.Name = t.val; 
				Expect(18);
				if (StartOf(9)) {
					FormalParameterList(
#line  775 "cs.ATG" 
out p);

#line  775 "cs.ATG" 
					delegateDeclr.Parameters = p; 
				}
				Expect(19);
				Expect(10);

#line  777 "cs.ATG" 
				delegateDeclr.EndLocation = t.Location;
				compilationUnit.AddChild(delegateDeclr);
				
			}
		} else SynErr(131);
	}

	void ClassBase(
#line  792 "cs.ATG" 
out StringCollection names) {

#line  794 "cs.ATG" 
		string qualident;
		names = new StringCollection(); 
		
		Expect(9);
		ClassType(
#line  798 "cs.ATG" 
out qualident);

#line  798 "cs.ATG" 
		names.Add(qualident); 
		while (la.kind == 12) {
			lexer.NextToken();
			Qualident(
#line  799 "cs.ATG" 
out qualident);

#line  799 "cs.ATG" 
			names.Add(qualident); 
		}
	}

	void ClassBody() {

#line  803 "cs.ATG" 
		AttributeSection section; 
		Expect(14);
		while (StartOf(10)) {

#line  806 "cs.ATG" 
			ArrayList attributes = new ArrayList();
			ModifierCollection m = new ModifierCollection ();
			
			while (la.kind == 16) {
				AttributeSection(
#line  809 "cs.ATG" 
out section);

#line  809 "cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(11)) {
				MemberModifier(
#line  810 "cs.ATG" 
m);
			}
			ClassMemberDecl(
#line  811 "cs.ATG" 
m, attributes);
		}
		Expect(15);
	}

	void StructInterfaces(
#line  816 "cs.ATG" 
out StringCollection names) {

#line  818 "cs.ATG" 
		string qualident; 
		names = new StringCollection();
		
		Expect(9);
		Qualident(
#line  822 "cs.ATG" 
out qualident);

#line  822 "cs.ATG" 
		names.Add(qualident); 
		while (la.kind == 12) {
			lexer.NextToken();
			Qualident(
#line  823 "cs.ATG" 
out qualident);

#line  823 "cs.ATG" 
			names.Add(qualident); 
		}
	}

	void StructBody() {

#line  827 "cs.ATG" 
		AttributeSection section; 
		Expect(14);
		while (StartOf(12)) {

#line  830 "cs.ATG" 
			ArrayList attributes = new ArrayList();
			ModifierCollection m = new ModifierCollection();
			
			while (la.kind == 16) {
				AttributeSection(
#line  833 "cs.ATG" 
out section);

#line  833 "cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(11)) {
				MemberModifier(
#line  834 "cs.ATG" 
m);
			}
			StructMemberDecl(
#line  835 "cs.ATG" 
m, attributes);
		}
		Expect(15);
	}

	void InterfaceBase(
#line  840 "cs.ATG" 
out StringCollection names) {

#line  842 "cs.ATG" 
		string qualident;
		names = new StringCollection();
		
		Expect(9);
		Qualident(
#line  846 "cs.ATG" 
out qualident);

#line  846 "cs.ATG" 
		names.Add(qualident); 
		while (la.kind == 12) {
			lexer.NextToken();
			Qualident(
#line  847 "cs.ATG" 
out qualident);

#line  847 "cs.ATG" 
			names.Add(qualident); 
		}
	}

	void InterfaceBody() {
		Expect(14);
		while (StartOf(13)) {
			InterfaceMemberDecl();
		}
		Expect(15);
	}

	void IntegralType(
#line  974 "cs.ATG" 
out string name) {

#line  974 "cs.ATG" 
		name = ""; 
		switch (la.kind) {
		case 101: {
			lexer.NextToken();

#line  976 "cs.ATG" 
			name = "sbyte"; 
			break;
		}
		case 53: {
			lexer.NextToken();

#line  977 "cs.ATG" 
			name = "byte"; 
			break;
		}
		case 103: {
			lexer.NextToken();

#line  978 "cs.ATG" 
			name = "short"; 
			break;
		}
		case 119: {
			lexer.NextToken();

#line  979 "cs.ATG" 
			name = "ushort"; 
			break;
		}
		case 81: {
			lexer.NextToken();

#line  980 "cs.ATG" 
			name = "int"; 
			break;
		}
		case 115: {
			lexer.NextToken();

#line  981 "cs.ATG" 
			name = "uint"; 
			break;
		}
		case 86: {
			lexer.NextToken();

#line  982 "cs.ATG" 
			name = "long"; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  983 "cs.ATG" 
			name = "ulong"; 
			break;
		}
		case 56: {
			lexer.NextToken();

#line  984 "cs.ATG" 
			name = "char"; 
			break;
		}
		default: SynErr(132); break;
		}
	}

	void EnumBody() {

#line  853 "cs.ATG" 
		FieldDeclaration f; 
		Expect(14);
		if (la.kind == 1 || la.kind == 16) {
			EnumMemberDecl(
#line  855 "cs.ATG" 
out f);

#line  855 "cs.ATG" 
			compilationUnit.AddChild(f); 
			while (
#line  856 "cs.ATG" 
NotFinalComma()) {
				Expect(12);
				EnumMemberDecl(
#line  856 "cs.ATG" 
out f);

#line  856 "cs.ATG" 
				compilationUnit.AddChild(f); 
			}
			if (la.kind == 12) {
				lexer.NextToken();
			}
		}
		Expect(15);
	}

	void Type(
#line  861 "cs.ATG" 
out TypeReference type) {

#line  863 "cs.ATG" 
		string name = "";
		int pointer = 0;
		
		if (la.kind == 1 || la.kind == 90 || la.kind == 107) {
			ClassType(
#line  867 "cs.ATG" 
out name);
		} else if (StartOf(14)) {
			SimpleType(
#line  868 "cs.ATG" 
out name);
		} else if (la.kind == 122) {
			lexer.NextToken();
			Expect(6);

#line  869 "cs.ATG" 
			pointer = 1; name = "void"; 
		} else SynErr(133);

#line  870 "cs.ATG" 
		ArrayList r = new ArrayList(); 
		while (
#line  871 "cs.ATG" 
IsPointerOrDims()) {

#line  871 "cs.ATG" 
			int i = 1; 
			if (la.kind == 6) {
				lexer.NextToken();

#line  872 "cs.ATG" 
				++pointer; 
			} else if (la.kind == 16) {
				lexer.NextToken();
				while (la.kind == 12) {
					lexer.NextToken();

#line  873 "cs.ATG" 
					++i; 
				}
				Expect(17);

#line  873 "cs.ATG" 
				r.Add(i); 
			} else SynErr(134);
		}

#line  875 "cs.ATG" 
		int[] rank = new int[r.Count]; r.CopyTo(rank); 
		type = new TypeReference(name, pointer, rank);
		
	}

	void FormalParameterList(
#line  909 "cs.ATG" 
out ArrayList parameter) {

#line  911 "cs.ATG" 
		parameter = new ArrayList();
		ParameterDeclarationExpression p;
		AttributeSection section;
		ArrayList attributes = new ArrayList();
		
		while (la.kind == 16) {
			AttributeSection(
#line  917 "cs.ATG" 
out section);

#line  917 "cs.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(15)) {
			FixedParameter(
#line  919 "cs.ATG" 
out p);

#line  919 "cs.ATG" 
			bool paramsFound = false;
			p.Attributes = attributes;
			parameter.Add(p);
			
			while (la.kind == 12) {
				lexer.NextToken();

#line  924 "cs.ATG" 
				attributes = new ArrayList(); if (paramsFound) Error("params array must be at end of parameter list"); 
				while (la.kind == 16) {
					AttributeSection(
#line  925 "cs.ATG" 
out section);

#line  925 "cs.ATG" 
					attributes.Add(section); 
				}
				if (StartOf(15)) {
					FixedParameter(
#line  927 "cs.ATG" 
out p);

#line  927 "cs.ATG" 
					p.Attributes = attributes; parameter.Add(p); 
				} else if (la.kind == 94) {
					ParameterArray(
#line  928 "cs.ATG" 
out p);

#line  928 "cs.ATG" 
					paramsFound = true; p.Attributes = attributes; parameter.Add(p); 
				} else SynErr(135);
			}
		} else if (la.kind == 94) {
			ParameterArray(
#line  931 "cs.ATG" 
out p);

#line  931 "cs.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		} else SynErr(136);
	}

	void ClassType(
#line  967 "cs.ATG" 
out string name) {

#line  967 "cs.ATG" 
		string qualident; name = "";
		if (la.kind == 1) {
			Qualident(
#line  969 "cs.ATG" 
out qualident);

#line  969 "cs.ATG" 
			name = qualident; 
		} else if (la.kind == 90) {
			lexer.NextToken();

#line  970 "cs.ATG" 
			name = "object"; 
		} else if (la.kind == 107) {
			lexer.NextToken();

#line  971 "cs.ATG" 
			name = "string"; 
		} else SynErr(137);
	}

	void MemberModifier(
#line  987 "cs.ATG" 
ModifierCollection m) {
		switch (la.kind) {
		case 48: {
			lexer.NextToken();

#line  989 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Abstract, t.Location, t.EndLocation), this); 
			break;
		}
		case 70: {
			lexer.NextToken();

#line  990 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Extern, t.Location, t.EndLocation), this); 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  991 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Internal, t.Location, t.EndLocation), this); 
			break;
		}
		case 88: {
			lexer.NextToken();

#line  992 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.New, t.Location, t.EndLocation), this); 
			break;
		}
		case 93: {
			lexer.NextToken();

#line  993 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Override, t.Location, t.EndLocation), this); 
			break;
		}
		case 95: {
			lexer.NextToken();

#line  994 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Private, t.Location, t.EndLocation), this); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  995 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Protected, t.Location, t.EndLocation), this); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  996 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Public, t.Location, t.EndLocation), this); 
			break;
		}
		case 98: {
			lexer.NextToken();

#line  997 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Readonly, t.Location, t.EndLocation), this); 
			break;
		}
		case 102: {
			lexer.NextToken();

#line  998 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Sealed, t.Location, t.EndLocation), this); 
			break;
		}
		case 106: {
			lexer.NextToken();

#line  999 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Static, t.Location, t.EndLocation), this); 
			break;
		}
		case 118: {
			lexer.NextToken();

#line  1000 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Unsafe, t.Location, t.EndLocation), this); 
			break;
		}
		case 121: {
			lexer.NextToken();

#line  1001 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Virtual, t.Location, t.EndLocation), this); 
			break;
		}
		case 123: {
			lexer.NextToken();

#line  1002 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Volatile, t.Location, t.EndLocation), this); 
			break;
		}
		default: SynErr(138); break;
		}
	}

	void ClassMemberDecl(
#line  1235 "cs.ATG" 
ModifierCollection m, ArrayList attributes) {

#line  1236 "cs.ATG" 
		Statement stmt = null; 
		if (StartOf(16)) {
			StructMemberDecl(
#line  1238 "cs.ATG" 
m, attributes);
		} else if (la.kind == 25) {

#line  1239 "cs.ATG" 
			m.Check(ModifierFlags.Destructors, this); Point startPos = t.Location; 
			lexer.NextToken();
			Expect(1);

#line  1240 "cs.ATG" 
			DestructorDeclaration d = new DestructorDeclaration(t.val, attributes); 
			d.Modifiers = m;
			d.StartLocation = startPos;
			
			Expect(18);
			Expect(19);
			if (la.kind == 14) {
				Block(
#line  1244 "cs.ATG" 
out stmt);
			} else if (la.kind == 10) {
				lexer.NextToken();
			} else SynErr(139);

#line  1244 "cs.ATG" 
			d.EndLocation = t.EndLocation; 
			d.Body = (BlockStatement)stmt;
			compilationUnit.AddChild(d);
			
		} else SynErr(140);
	}

	void StructMemberDecl(
#line  1013 "cs.ATG" 
ModifierCollection m, ArrayList attributes) {

#line  1015 "cs.ATG" 
		string qualident = null;
		TypeReference type;
		Expression expr;
		ArrayList p = new ArrayList();
		Statement stmt = null;
		ArrayList variableDeclarators = new ArrayList();
		
		if (la.kind == 59) {

#line  1023 "cs.ATG" 
			m.Check(ModifierFlags.Constants, this); 
			lexer.NextToken();
			Type(
#line  1025 "cs.ATG" 
out type);
			Expect(1);

#line  1025 "cs.ATG" 
			m.Add (new Modifier (ModifierFlags.Const, t.Location, t.EndLocation), this);
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m);
			                              VariableDeclaration f = new VariableDeclaration(t.val);
			                              fd.Attributes = attributes;
			                              fd.Fields.Add(f);
			                           
			Expect(3);
			Expr(
#line  1031 "cs.ATG" 
out expr);

#line  1031 "cs.ATG" 
			f.Initializer = expr; 
			while (la.kind == 12) {
				lexer.NextToken();
				Expect(1);

#line  1032 "cs.ATG" 
				f = new VariableDeclaration(t.val);
				fd.Fields.Add(f);
				
				Expect(3);
				Expr(
#line  1035 "cs.ATG" 
out expr);

#line  1035 "cs.ATG" 
				f.Initializer = expr; 
			}
			Expect(10);

#line  1036 "cs.ATG" 
			fd.EndLocation = t.EndLocation; compilationUnit.AddChild(fd); 
		} else if (
#line  1039 "cs.ATG" 
NotVoidPointer()) {

#line  1039 "cs.ATG" 
			m.Check(ModifierFlags.PropertysEventsMethods, this); 
			Expect(122);

#line  1040 "cs.ATG" 
			Point startPos = t.Location; 
			Qualident(
#line  1041 "cs.ATG" 
out qualident);
			Expect(18);
			if (StartOf(9)) {
				FormalParameterList(
#line  1042 "cs.ATG" 
out p);
			}
			Expect(19);

#line  1042 "cs.ATG" 
			MethodDeclaration methodDeclaration = new MethodDeclaration(qualident, 
			                                                           m, 
			                                                           new TypeReference("void"), 
			                                                           p, 
			                                                           attributes);
			methodDeclaration.StartLocation = startPos;
			methodDeclaration.EndLocation   = t.EndLocation;
			methodDeclaration.Modifiers = m;
			compilationUnit.AddChild(methodDeclaration);
			compilationUnit.BlockStart(methodDeclaration);
			
			if (la.kind == 14) {
				Block(
#line  1053 "cs.ATG" 
out stmt);
			} else if (la.kind == 10) {
				lexer.NextToken();
			} else SynErr(141);

#line  1053 "cs.ATG" 
			compilationUnit.BlockEnd();
			methodDeclaration.Body  = (BlockStatement)stmt;
			
		} else if (la.kind == 68) {

#line  1057 "cs.ATG" 
			m.Check(ModifierFlags.PropertysEventsMethods, this); 
			lexer.NextToken();

#line  1058 "cs.ATG" 
			EventDeclaration eventDecl = new EventDeclaration(m, attributes);
			eventDecl.StartLocation = t.Location;
			compilationUnit.AddChild(eventDecl);
			compilationUnit.BlockStart(eventDecl);
			EventAddRegion addBlock = null;
			EventRemoveRegion removeBlock = null;
			
			Type(
#line  1065 "cs.ATG" 
out type);

#line  1065 "cs.ATG" 
			eventDecl.TypeReference = type; 
			if (
#line  1067 "cs.ATG" 
IsVarDecl()) {
				VariableDeclarator(
#line  1067 "cs.ATG" 
variableDeclarators);
				while (la.kind == 12) {
					lexer.NextToken();
					VariableDeclarator(
#line  1068 "cs.ATG" 
variableDeclarators);
				}
				Expect(10);

#line  1068 "cs.ATG" 
				eventDecl.VariableDeclarators = variableDeclarators; eventDecl.EndLocation = t.EndLocation;  
			} else if (la.kind == 1) {
				Qualident(
#line  1069 "cs.ATG" 
out qualident);

#line  1069 "cs.ATG" 
				eventDecl.Name = qualident; eventDecl.EndLocation = t.EndLocation;  
				Expect(14);

#line  1070 "cs.ATG" 
				eventDecl.BodyStart = t.Location; 
				EventAccessorDecls(
#line  1071 "cs.ATG" 
out addBlock, out removeBlock);
				Expect(15);

#line  1072 "cs.ATG" 
				eventDecl.BodyEnd   = t.EndLocation; 
			} else SynErr(142);

#line  1073 "cs.ATG" 
			compilationUnit.BlockEnd();
				                                           eventDecl.AddRegion = addBlock;
			eventDecl.RemoveRegion = removeBlock;
			
		} else if (
#line  1080 "cs.ATG" 
IdentAndLPar()) {

#line  1080 "cs.ATG" 
			m.Check(ModifierFlags.Constructors | ModifierFlags.StaticConstructors, this); 
			Expect(1);

#line  1081 "cs.ATG" 
			string name = t.val; Point startPos = t.Location; 
			Expect(18);
			if (StartOf(9)) {

#line  1081 "cs.ATG" 
				m.Check(ModifierFlags.Constructors, this); 
				FormalParameterList(
#line  1082 "cs.ATG" 
out p);
			}
			Expect(19);

#line  1084 "cs.ATG" 
			ConstructorInitializer init = null;  
			if (la.kind == 9) {

#line  1085 "cs.ATG" 
				m.Check(ModifierFlags.Constructors, this); 
				ConstructorInitializer(
#line  1086 "cs.ATG" 
out init);
			}

#line  1088 "cs.ATG" 
			ConstructorDeclaration cd = new ConstructorDeclaration(name, m, p, init, attributes); 
			cd.StartLocation = startPos;
			cd.EndLocation   = t.EndLocation;
			
			if (la.kind == 14) {
				Block(
#line  1093 "cs.ATG" 
out stmt);
			} else if (la.kind == 10) {
				lexer.NextToken();
			} else SynErr(143);

#line  1093 "cs.ATG" 
			cd.Body = (BlockStatement)stmt; compilationUnit.AddChild(cd); 
		} else if (la.kind == 69 || la.kind == 79) {

#line  1096 "cs.ATG" 
			m.Check(ModifierFlags.Operators, this);
			if (m.isNone) Error("at least one modifier must be set"); 
			bool isImplicit = true;
			
			if (la.kind == 79) {
				lexer.NextToken();
			} else {
				lexer.NextToken();

#line  1100 "cs.ATG" 
				isImplicit = false; 
			}
			Expect(91);
			Type(
#line  1101 "cs.ATG" 
out type);

#line  1101 "cs.ATG" 
			TypeReference operatorType = type; 
			Expect(18);
			Type(
#line  1102 "cs.ATG" 
out type);
			Expect(1);

#line  1102 "cs.ATG" 
			string varName = t.val; 
			Expect(19);
			if (la.kind == 14) {
				Block(
#line  1102 "cs.ATG" 
out stmt);
			} else if (la.kind == 10) {
				lexer.NextToken();

#line  1102 "cs.ATG" 
				stmt = null; 
			} else SynErr(144);

#line  1105 "cs.ATG" 
			OperatorDeclarator operatorDeclarator = new OperatorDeclarator(isImplicit ? OperatorType.Implicit : OperatorType.Explicit,
			                                                              operatorType,
			                                                              type,
			                                                              varName);
			OperatorDeclaration operatorDeclaration = new OperatorDeclaration(operatorDeclarator, m, attributes);
			operatorDeclaration.Body = stmt;
			compilationUnit.AddChild(operatorDeclaration);
			
		} else if (StartOf(17)) {

#line  1115 "cs.ATG" 
			bool partial = false; 
			if (
#line  1116 "cs.ATG" 
IdentIsPartial ()) {
				lexer.NextToken();

#line  1116 "cs.ATG" 
				partial = true; 
			}
			TypeDecl(
#line  1117 "cs.ATG" 
m, attributes, partial);
		} else if (StartOf(8)) {
			Type(
#line  1118 "cs.ATG" 
out type);

#line  1119 "cs.ATG" 
			Point startPos = t.Location;
			if (type.Type.IndexOf (".") > 0)
			{ 	// We need to fix the position since t.Location would give us
				// the last dot position :(
				string text = type.Type;
				text = text.Substring (0, text.LastIndexOf ("."));
				startPos.X -= text.Length + 1 ;
			} 
			
			if (la.kind == 91) {

#line  1129 "cs.ATG" 
				Token op;
				m.Check(ModifierFlags.Operators, this);
				if (m.isNone) Error("at least one modifier must be set");
				
				lexer.NextToken();
				OverloadableOperator(
#line  1133 "cs.ATG" 
out op);

#line  1133 "cs.ATG" 
				TypeReference firstType, secondType = null; string secondName = null; 
				Expect(18);
				Type(
#line  1134 "cs.ATG" 
out firstType);
				Expect(1);

#line  1134 "cs.ATG" 
				string firstName = t.val; 
				if (la.kind == 12) {
					lexer.NextToken();
					Type(
#line  1135 "cs.ATG" 
out secondType);
					Expect(1);

#line  1135 "cs.ATG" 
					secondName = t.val; 

#line  1135 "cs.ATG" 
					if (ParserUtil.IsUnaryOperator(op) && !ParserUtil.IsBinaryOperator(op))
					Error("too many operands for unary operator"); 
					
				} else if (la.kind == 19) {

#line  1138 "cs.ATG" 
					if (ParserUtil.IsBinaryOperator(op))
					Error("too few operands for binary operator");
					
				} else SynErr(145);
				Expect(19);
				if (la.kind == 14) {
					Block(
#line  1142 "cs.ATG" 
out stmt);
				} else if (la.kind == 10) {
					lexer.NextToken();
				} else SynErr(146);

#line  1144 "cs.ATG" 
				OperatorDeclarator operatorDeclarator = new OperatorDeclarator(secondType != null ? OperatorType.Binary : OperatorType.Unary, 
				                                                              type,
				                                                              op.kind,
				                                                              firstType,
				                                                              firstName,
				                                                              secondType,
				                                                              secondName);
				OperatorDeclaration operatorDeclaration = new OperatorDeclaration(operatorDeclarator, m, attributes);
				operatorDeclaration.Body = stmt;
				compilationUnit.AddChild(operatorDeclaration);
				
			} else if (
#line  1157 "cs.ATG" 
IsVarDecl()) {

#line  1157 "cs.ATG" 
				m.Check(ModifierFlags.Fields, this); 
				FieldDeclaration fd = new FieldDeclaration(attributes, type, m);
				fd.StartLocation = startPos; 
				
				VariableDeclarator(
#line  1161 "cs.ATG" 
variableDeclarators);
				while (la.kind == 12) {
					lexer.NextToken();
					VariableDeclarator(
#line  1162 "cs.ATG" 
variableDeclarators);
				}
				Expect(10);

#line  1163 "cs.ATG" 
				fd.EndLocation = t.EndLocation; fd.Fields = variableDeclarators; compilationUnit.AddChild(fd); 
			} else if (la.kind == 110) {

#line  1166 "cs.ATG" 
				m.Check(ModifierFlags.Indexers, this); 
				lexer.NextToken();
				Expect(16);
				FormalParameterList(
#line  1167 "cs.ATG" 
out p);
				Expect(17);

#line  1167 "cs.ATG" 
				Point endLocation = t.EndLocation; 
				Expect(14);

#line  1168 "cs.ATG" 
				IndexerDeclaration indexer = new IndexerDeclaration(type, p, m, attributes);
				indexer.StartLocation = startPos;
				indexer.EndLocation   = endLocation;
				indexer.BodyStart     = t.Location;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  1175 "cs.ATG" 
out getRegion, out setRegion);
				Expect(15);

#line  1176 "cs.ATG" 
				indexer.BodyEnd    = t.EndLocation;
				indexer.GetRegion = getRegion;
				indexer.SetRegion = setRegion;
				compilationUnit.AddChild(indexer);
				
			} else if (la.kind == 1) {
				Qualident(
#line  1181 "cs.ATG" 
out qualident);

#line  1181 "cs.ATG" 
				Point qualIdentEndLocation = t.EndLocation; 
				if (la.kind == 14 || la.kind == 18) {
					if (la.kind == 18) {

#line  1184 "cs.ATG" 
						m.Check(ModifierFlags.PropertysEventsMethods, this); 
						lexer.NextToken();
						if (StartOf(9)) {
							FormalParameterList(
#line  1185 "cs.ATG" 
out p);
						}
						Expect(19);

#line  1185 "cs.ATG" 
						MethodDeclaration methodDeclaration = new MethodDeclaration(qualident, 
						                                                     m, 
						                                                     type, 
						                                                     p, 
						                                                     attributes);
						     methodDeclaration.StartLocation = startPos;
						     methodDeclaration.EndLocation   = t.EndLocation;
						     compilationUnit.AddChild(methodDeclaration);
						  
						if (la.kind == 14) {
							Block(
#line  1194 "cs.ATG" 
out stmt);
						} else if (la.kind == 10) {
							lexer.NextToken();
						} else SynErr(147);

#line  1194 "cs.ATG" 
						methodDeclaration.Body  = (BlockStatement)stmt; 
					} else {
						lexer.NextToken();

#line  1197 "cs.ATG" 
						PropertyDeclaration pDecl = new PropertyDeclaration(qualident, type, m, attributes); 
						pDecl.StartLocation = startPos;
						pDecl.EndLocation   = qualIdentEndLocation;
						pDecl.BodyStart   = t.Location;
						PropertyGetRegion getRegion;
						PropertySetRegion setRegion;
						
						AccessorDecls(
#line  1204 "cs.ATG" 
out getRegion, out setRegion);
						Expect(15);

#line  1206 "cs.ATG" 
						pDecl.GetRegion = getRegion;
						pDecl.SetRegion = setRegion;
						pDecl.BodyEnd = t.EndLocation;
						compilationUnit.AddChild(pDecl);
						
					}
				} else if (la.kind == 13) {

#line  1214 "cs.ATG" 
					m.Check(ModifierFlags.Indexers, this); 
					lexer.NextToken();
					Expect(110);
					Expect(16);
					FormalParameterList(
#line  1215 "cs.ATG" 
out p);
					Expect(17);

#line  1216 "cs.ATG" 
					IndexerDeclaration indexer = new IndexerDeclaration(type, p, m, attributes);
					indexer.StartLocation = startPos;
					indexer.EndLocation   = t.EndLocation;
					indexer.NamespaceName = qualident;
					PropertyGetRegion getRegion;
					PropertySetRegion setRegion;
					
					Expect(14);

#line  1223 "cs.ATG" 
					Point bodyStart = t.Location; 
					AccessorDecls(
#line  1224 "cs.ATG" 
out getRegion, out setRegion);
					Expect(15);

#line  1225 "cs.ATG" 
					indexer.BodyStart = bodyStart;
					indexer.BodyEnd   = t.EndLocation;
					indexer.GetRegion = getRegion;
					indexer.SetRegion = setRegion;
					compilationUnit.AddChild(indexer);
					
				} else SynErr(148);
			} else SynErr(149);
		} else SynErr(150);
	}

	void InterfaceMemberDecl() {

#line  1252 "cs.ATG" 
		TypeReference type;
		ArrayList p;
		AttributeSection section;
		ModifierCollection mod = new ModifierCollection ();
		ArrayList attributes = new ArrayList();
		ArrayList parameters = new ArrayList();
		string name;
		PropertyGetRegion getBlock;
		PropertySetRegion setBlock;
		Point startLocation = new Point(-1, -1);
		
		while (la.kind == 16) {
			AttributeSection(
#line  1264 "cs.ATG" 
out section);

#line  1264 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 88) {
			lexer.NextToken();

#line  1265 "cs.ATG" 
			mod.Add (new Modifier (ModifierFlags.New, t.Location, t.EndLocation), this); startLocation = t.Location; 
		}
		if (
#line  1268 "cs.ATG" 
NotVoidPointer()) {
			Expect(122);

#line  1268 "cs.ATG" 
			if (startLocation.X == -1) startLocation = t.Location; 
			Expect(1);

#line  1268 "cs.ATG" 
			name = t.val; 
			Expect(18);
			if (StartOf(9)) {
				FormalParameterList(
#line  1269 "cs.ATG" 
out parameters);
			}
			Expect(19);
			Expect(10);

#line  1269 "cs.ATG" 
			MethodDeclaration md = new MethodDeclaration(name, mod, new TypeReference("void"), parameters, attributes);
			md.StartLocation = startLocation;
			md.EndLocation = t.EndLocation;
			compilationUnit.AddChild(md);
			
		} else if (StartOf(18)) {
			if (StartOf(8)) {
				Type(
#line  1275 "cs.ATG" 
out type);

#line  1275 "cs.ATG" 
				if (startLocation.X == -1) startLocation = t.Location; 
				if (la.kind == 1) {
					lexer.NextToken();

#line  1277 "cs.ATG" 
					name = t.val; Point qualIdentEndLocation = t.EndLocation; 
					if (la.kind == 18) {
						lexer.NextToken();
						if (StartOf(9)) {
							FormalParameterList(
#line  1280 "cs.ATG" 
out parameters);
						}
						Expect(19);
						Expect(10);

#line  1280 "cs.ATG" 
						MethodDeclaration md = new MethodDeclaration(name, mod, type, parameters, attributes);
						md.StartLocation = startLocation;
						md.EndLocation = t.EndLocation;
						compilationUnit.AddChild(md);
						
					} else if (la.kind == 14) {

#line  1286 "cs.ATG" 
						PropertyDeclaration pd = new PropertyDeclaration(name, type, mod, attributes); compilationUnit.AddChild(pd); 
						lexer.NextToken();

#line  1287 "cs.ATG" 
						Point bodyStart = t.Location;
						InterfaceAccessors(
#line  1287 "cs.ATG" 
out getBlock, out setBlock);
						Expect(15);

#line  1287 "cs.ATG" 
						pd.GetRegion = getBlock; pd.SetRegion = setBlock; pd.StartLocation = startLocation; pd.EndLocation = qualIdentEndLocation; pd.BodyStart = bodyStart; pd.BodyEnd = t.EndLocation; 
					} else SynErr(151);
				} else if (la.kind == 110) {
					lexer.NextToken();
					Expect(16);
					FormalParameterList(
#line  1290 "cs.ATG" 
out p);
					Expect(17);

#line  1290 "cs.ATG" 
					Point bracketEndLocation = t.EndLocation; 

#line  1290 "cs.ATG" 
					IndexerDeclaration id = new IndexerDeclaration(type, p, mod, attributes); compilationUnit.AddChild(id); 
					Expect(14);

#line  1291 "cs.ATG" 
					Point bodyStart = t.Location;
					InterfaceAccessors(
#line  1291 "cs.ATG" 
out getBlock, out setBlock);
					Expect(15);

#line  1291 "cs.ATG" 
					id.GetRegion = getBlock; id.SetRegion = setBlock; id.StartLocation = startLocation;  id.EndLocation = bracketEndLocation; id.BodyStart = bodyStart; id.BodyEnd = t.EndLocation;
				} else SynErr(152);
			} else {
				lexer.NextToken();

#line  1294 "cs.ATG" 
				if (startLocation.X == -1) startLocation = t.Location; 
				Type(
#line  1294 "cs.ATG" 
out type);
				Expect(1);

#line  1294 "cs.ATG" 
				EventDeclaration ed = new EventDeclaration(type, t.val, mod, attributes);
				compilationUnit.AddChild(ed);
				
				Expect(10);

#line  1297 "cs.ATG" 
				ed.StartLocation = startLocation; ed.EndLocation = t.EndLocation; 
			}
		} else SynErr(153);
	}

	void EnumMemberDecl(
#line  1302 "cs.ATG" 
out FieldDeclaration f) {

#line  1304 "cs.ATG" 
		Expression expr = null;
		ArrayList attributes = new ArrayList();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 16) {
			AttributeSection(
#line  1310 "cs.ATG" 
out section);

#line  1310 "cs.ATG" 
			attributes.Add(section); 
		}
		Expect(1);

#line  1311 "cs.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl         = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = t.Location;
		
		if (la.kind == 3) {
			lexer.NextToken();
			Expr(
#line  1316 "cs.ATG" 
out expr);

#line  1316 "cs.ATG" 
			varDecl.Initializer = expr; 
		}
	}

	void SimpleType(
#line  898 "cs.ATG" 
out string name) {

#line  899 "cs.ATG" 
		name = String.Empty; 
		if (StartOf(19)) {
			IntegralType(
#line  901 "cs.ATG" 
out name);
		} else if (la.kind == 74) {
			lexer.NextToken();

#line  902 "cs.ATG" 
			name = t.val; 
		} else if (la.kind == 65) {
			lexer.NextToken();

#line  903 "cs.ATG" 
			name = t.val; 
		} else if (la.kind == 61) {
			lexer.NextToken();

#line  904 "cs.ATG" 
			name = t.val; 
		} else if (la.kind == 51) {
			lexer.NextToken();

#line  905 "cs.ATG" 
			name = t.val; 
		} else SynErr(154);
	}

	void NonArrayType(
#line  880 "cs.ATG" 
out TypeReference type) {

#line  882 "cs.ATG" 
		string name = "";
		int pointer = 0;
		
		if (la.kind == 1 || la.kind == 90 || la.kind == 107) {
			ClassType(
#line  886 "cs.ATG" 
out name);
		} else if (StartOf(14)) {
			SimpleType(
#line  887 "cs.ATG" 
out name);
		} else if (la.kind == 122) {
			lexer.NextToken();
			Expect(6);

#line  888 "cs.ATG" 
			pointer = 1; name = "void"; 
		} else SynErr(155);
		while (
#line  890 "cs.ATG" 
IsPointer()) {
			Expect(6);

#line  891 "cs.ATG" 
			++pointer; 
		}

#line  894 "cs.ATG" 
		type = new TypeReference(name, pointer, null);
		
	}

	void FixedParameter(
#line  935 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  937 "cs.ATG" 
		TypeReference type;
		ParamModifiers mod = ParamModifiers.In;
		
		if (la.kind == 92 || la.kind == 99) {
			if (la.kind == 99) {
				lexer.NextToken();

#line  942 "cs.ATG" 
				mod = ParamModifiers.Ref; 
			} else {
				lexer.NextToken();

#line  943 "cs.ATG" 
				mod = ParamModifiers.Out; 
			}
		}
		Type(
#line  945 "cs.ATG" 
out type);
		Expect(1);

#line  945 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, mod); p.StartLocation = t.Location; 
	}

	void ParameterArray(
#line  948 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  949 "cs.ATG" 
		TypeReference type; 
		Expect(94);
		Type(
#line  951 "cs.ATG" 
out type);
		Expect(1);

#line  951 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, ParamModifiers.Params); p.StartLocation = t.Location; 
	}

	void AccessorModifier(
#line  1005 "cs.ATG" 
ModifierCollection m) {
		if (la.kind == 83) {
			lexer.NextToken();

#line  1007 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Internal, t.Location, t.EndLocation), this); 
		} else if (la.kind == 97) {
			lexer.NextToken();

#line  1008 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Public, t.Location, t.EndLocation), this); 
		} else if (la.kind == 96) {
			lexer.NextToken();

#line  1009 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Protected, t.Location, t.EndLocation), this); 
		} else if (la.kind == 95) {
			lexer.NextToken();

#line  1010 "cs.ATG" 
			m.Add(new Modifier (ModifierFlags.Private, t.Location, t.EndLocation), this); 
		} else SynErr(156);
	}

	void Block(
#line  1432 "cs.ATG" 
out Statement stmt) {
		Expect(14);

#line  1434 "cs.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		blockStmt.StartLocation = t.Location;
		compilationUnit.BlockStart(blockStmt);
		
		while (StartOf(20)) {
			Statement();
		}
		Expect(15);

#line  1439 "cs.ATG" 
		stmt = blockStmt;
		blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void VariableDeclarator(
#line  1425 "cs.ATG" 
ArrayList fieldDeclaration) {

#line  1426 "cs.ATG" 
		Expression expr = null; 
		Expect(1);

#line  1428 "cs.ATG" 
		VariableDeclaration f = new VariableDeclaration(t.val); 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1429 "cs.ATG" 
out expr);

#line  1429 "cs.ATG" 
			f.Initializer = expr; 
		}

#line  1429 "cs.ATG" 
		fieldDeclaration.Add(f); 
	}

	void EventAccessorDecls(
#line  1374 "cs.ATG" 
out EventAddRegion addBlock, out EventRemoveRegion removeBlock) {

#line  1375 "cs.ATG" 
		AttributeSection section;
		ArrayList attributes = new ArrayList();
		Statement stmt;
		addBlock = null;
		removeBlock = null;
		
		while (la.kind == 16) {
			AttributeSection(
#line  1382 "cs.ATG" 
out section);

#line  1382 "cs.ATG" 
			attributes.Add(section); 
		}
		if (
#line  1384 "cs.ATG" 
IdentIsAdd()) {

#line  1384 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); 
			AddAccessorDecl(
#line  1385 "cs.ATG" 
out stmt);

#line  1385 "cs.ATG" 
			attributes = new ArrayList(); addBlock.Block = (BlockStatement)stmt; 
			while (la.kind == 16) {
				AttributeSection(
#line  1386 "cs.ATG" 
out section);

#line  1386 "cs.ATG" 
				attributes.Add(section); 
			}
			RemoveAccessorDecl(
#line  1387 "cs.ATG" 
out stmt);

#line  1387 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; 
		} else if (
#line  1388 "cs.ATG" 
IdentIsRemove()) {
			RemoveAccessorDecl(
#line  1389 "cs.ATG" 
out stmt);

#line  1389 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; attributes = new ArrayList(); 
			while (la.kind == 16) {
				AttributeSection(
#line  1390 "cs.ATG" 
out section);

#line  1390 "cs.ATG" 
				attributes.Add(section); 
			}
			AddAccessorDecl(
#line  1391 "cs.ATG" 
out stmt);

#line  1391 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); addBlock.Block = (BlockStatement)stmt; 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1392 "cs.ATG" 
			Error("add or remove accessor declaration expected"); 
		} else SynErr(157);
	}

	void ConstructorInitializer(
#line  1461 "cs.ATG" 
out ConstructorInitializer ci) {

#line  1462 "cs.ATG" 
		Expression expr; ci = new ConstructorInitializer(); 
		Expect(9);
		if (la.kind == 50) {
			lexer.NextToken();

#line  1466 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.Base; 
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  1467 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.This; 
		} else SynErr(158);
		Expect(18);
		if (StartOf(21)) {
			Argument(
#line  1470 "cs.ATG" 
out expr);

#line  1470 "cs.ATG" 
			ci.Arguments.Add(expr); 
			while (la.kind == 12) {
				lexer.NextToken();
				Argument(
#line  1470 "cs.ATG" 
out expr);

#line  1470 "cs.ATG" 
				ci.Arguments.Add(expr); 
			}
		}
		Expect(19);
	}

	void OverloadableOperator(
#line  1482 "cs.ATG" 
out Token op) {
		switch (la.kind) {
		case 4: {
			lexer.NextToken();
			break;
		}
		case 5: {
			lexer.NextToken();
			break;
		}
		case 22: {
			lexer.NextToken();
			break;
		}
		case 25: {
			lexer.NextToken();
			break;
		}
		case 29: {
			lexer.NextToken();
			break;
		}
		case 30: {
			lexer.NextToken();
			break;
		}
		case 112: {
			lexer.NextToken();
			break;
		}
		case 71: {
			lexer.NextToken();
			break;
		}
		case 6: {
			lexer.NextToken();
			break;
		}
		case 7: {
			lexer.NextToken();
			break;
		}
		case 8: {
			lexer.NextToken();
			break;
		}
		case 26: {
			lexer.NextToken();
			break;
		}
		case 27: {
			lexer.NextToken();
			break;
		}
		case 28: {
			lexer.NextToken();
			break;
		}
		case 35: {
			lexer.NextToken();
			break;
		}
		case 36: {
			lexer.NextToken();
			break;
		}
		case 31: {
			lexer.NextToken();
			break;
		}
		case 32: {
			lexer.NextToken();
			break;
		}
		case 20: {
			lexer.NextToken();
			break;
		}
		case 21: {
			lexer.NextToken();
			break;
		}
		case 33: {
			lexer.NextToken();
			break;
		}
		case 34: {
			lexer.NextToken();
			break;
		}
		default: SynErr(159); break;
		}

#line  1491 "cs.ATG" 
		op = t; 
	}

	void AccessorDecls(
#line  1320 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1322 "cs.ATG" 
		ArrayList attributes = new ArrayList(); 
		ModifierCollection m = new ModifierCollection ();
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		
		while (la.kind == 16) {
			AttributeSection(
#line  1329 "cs.ATG" 
out section);

#line  1329 "cs.ATG" 
			attributes.Add(section); 
		}
		while (StartOf(22)) {
			AccessorModifier(
#line  1330 "cs.ATG" 
m);
		}
		if (
#line  1332 "cs.ATG" 
IdentIsGet()) {
			GetAccessorDecl(
#line  1333 "cs.ATG" 
out getBlock, attributes, m);
			if (StartOf(23)) {

#line  1334 "cs.ATG" 
				attributes = new ArrayList(); m = new ModifierCollection (); 
				while (la.kind == 16) {
					AttributeSection(
#line  1335 "cs.ATG" 
out section);

#line  1335 "cs.ATG" 
					attributes.Add(section); 
				}
				while (StartOf(22)) {
					AccessorModifier(
#line  1336 "cs.ATG" 
m);
				}
				SetAccessorDecl(
#line  1337 "cs.ATG" 
out setBlock, attributes, m);
			}
		} else if (
#line  1339 "cs.ATG" 
IdentIsSet()) {
			SetAccessorDecl(
#line  1340 "cs.ATG" 
out setBlock, attributes, m);
			if (StartOf(23)) {

#line  1341 "cs.ATG" 
				attributes = new ArrayList(); m = new ModifierCollection (); 
				while (la.kind == 16) {
					AttributeSection(
#line  1342 "cs.ATG" 
out section);

#line  1342 "cs.ATG" 
					attributes.Add(section); 
				}
				while (StartOf(22)) {
					AccessorModifier(
#line  1343 "cs.ATG" 
m);
				}
				GetAccessorDecl(
#line  1344 "cs.ATG" 
out getBlock, attributes, m);
			}
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1346 "cs.ATG" 
			Error("get or set accessor declaration expected"); 
		} else SynErr(160);
	}

	void InterfaceAccessors(
#line  1396 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1398 "cs.ATG" 
		AttributeSection section;
		ArrayList attributes = new ArrayList();
		getBlock = null; setBlock = null;
		
		while (la.kind == 16) {
			AttributeSection(
#line  1403 "cs.ATG" 
out section);

#line  1403 "cs.ATG" 
			attributes.Add(section); 
		}
		if (
#line  1405 "cs.ATG" 
IdentIsGet()) {
			Expect(1);

#line  1405 "cs.ATG" 
			getBlock = new PropertyGetRegion(null, attributes); 
		} else if (
#line  1406 "cs.ATG" 
IdentIsSet()) {
			Expect(1);

#line  1406 "cs.ATG" 
			setBlock = new PropertySetRegion(null, attributes); 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1407 "cs.ATG" 
			Error("set or get expected"); 
		} else SynErr(161);
		Expect(10);

#line  1409 "cs.ATG" 
		attributes = new ArrayList(); 
		if (la.kind == 1 || la.kind == 16) {
			while (la.kind == 16) {
				AttributeSection(
#line  1411 "cs.ATG" 
out section);

#line  1411 "cs.ATG" 
				attributes.Add(section); 
			}
			if (
#line  1413 "cs.ATG" 
IdentIsGet()) {
				Expect(1);

#line  1413 "cs.ATG" 
				if (getBlock != null) Error("get already declared");
				else getBlock = new PropertyGetRegion(null, attributes);
				
			} else if (
#line  1416 "cs.ATG" 
IdentIsSet()) {
				Expect(1);

#line  1416 "cs.ATG" 
				if (setBlock != null) Error("set already declared");
				else setBlock = new PropertySetRegion(null, attributes);
				
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  1419 "cs.ATG" 
				Error("set or get expected"); 
			} else SynErr(162);
			Expect(10);
		}
	}

	void GetAccessorDecl(
#line  1350 "cs.ATG" 
out PropertyGetRegion getBlock, ArrayList attributes, ModifierCollection m) {

#line  1351 "cs.ATG" 
		Statement stmt = null; 
		Expect(1);

#line  1354 "cs.ATG" 
		Point startLocation = t.Location; 

#line  1355 "cs.ATG" 
		if (t.val != "get") Error("get expected"); 
		if (la.kind == 14) {
			Block(
#line  1356 "cs.ATG" 
out stmt);
		} else if (la.kind == 10) {
			lexer.NextToken();
		} else SynErr(163);

#line  1357 "cs.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes, m); 

#line  1358 "cs.ATG" 
		getBlock.StartLocation = startLocation; 

#line  1359 "cs.ATG" 
		getBlock.EndLocation = t.Location; 
	}

	void SetAccessorDecl(
#line  1362 "cs.ATG" 
out PropertySetRegion setBlock, ArrayList attributes, ModifierCollection m) {

#line  1363 "cs.ATG" 
		Statement stmt = null; 
		Expect(1);

#line  1366 "cs.ATG" 
		Point startLocation = t.Location; 

#line  1367 "cs.ATG" 
		if (t.val != "set") Error("set expected"); 
		if (la.kind == 14) {
			Block(
#line  1368 "cs.ATG" 
out stmt);
		} else if (la.kind == 10) {
			lexer.NextToken();
		} else SynErr(164);

#line  1369 "cs.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes, m); 

#line  1370 "cs.ATG" 
		setBlock.StartLocation = startLocation; 

#line  1371 "cs.ATG" 
		setBlock.EndLocation = t.Location; 
	}

	void AddAccessorDecl(
#line  1445 "cs.ATG" 
out Statement stmt) {

#line  1446 "cs.ATG" 
		stmt = null;
		Expect(1);

#line  1449 "cs.ATG" 
		if (t.val != "add") Error("add expected"); 
		Block(
#line  1450 "cs.ATG" 
out stmt);
	}

	void RemoveAccessorDecl(
#line  1453 "cs.ATG" 
out Statement stmt) {

#line  1454 "cs.ATG" 
		stmt = null;
		Expect(1);

#line  1457 "cs.ATG" 
		if (t.val != "remove") Error("remove expected"); 
		Block(
#line  1458 "cs.ATG" 
out stmt);
	}

	void VariableInitializer(
#line  1474 "cs.ATG" 
out Expression initializerExpression) {

#line  1475 "cs.ATG" 
		TypeReference type = null; Expression expr = null; initializerExpression = null; 
		if (StartOf(4)) {
			Expr(
#line  1477 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 14) {
			ArrayInitializer(
#line  1478 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 105) {
			lexer.NextToken();
			Type(
#line  1479 "cs.ATG" 
out type);
			Expect(16);
			Expr(
#line  1479 "cs.ATG" 
out expr);
			Expect(17);

#line  1479 "cs.ATG" 
			initializerExpression = new StackAllocExpression(type, expr); 
		} else SynErr(165);
	}

	void Statement() {

#line  1562 "cs.ATG" 
		TypeReference type;
		Expression expr;
		Statement stmt;
		
		if (
#line  1568 "cs.ATG" 
IsLabel()) {
			Expect(1);

#line  1568 "cs.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val)); 
			Expect(9);
			Statement();
		} else if (la.kind == 59) {
			lexer.NextToken();
			Type(
#line  1571 "cs.ATG" 
out type);

#line  1572 "cs.ATG" 
			ModifierCollection m = new ModifierCollection ();
			m.Add (new Modifier (ModifierFlags.Const));
			LocalVariableDeclaration var = new LocalVariableDeclaration(type, m); string ident = null; var.StartLocation = t.Location; 
			Expect(1);

#line  1575 "cs.ATG" 
			ident = t.val; 
			Expect(3);
			Expr(
#line  1576 "cs.ATG" 
out expr);

#line  1576 "cs.ATG" 
			var.Variables.Add(new VariableDeclaration(ident, expr)); 
			while (la.kind == 12) {
				lexer.NextToken();
				Expect(1);

#line  1577 "cs.ATG" 
				ident = t.val; 
				Expect(3);
				Expr(
#line  1577 "cs.ATG" 
out expr);

#line  1577 "cs.ATG" 
				var.Variables.Add(new VariableDeclaration(ident, expr)); 
			}
			Expect(10);

#line  1578 "cs.ATG" 
			compilationUnit.AddChild(var); 
		} else if (
#line  1580 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1580 "cs.ATG" 
out stmt);
			Expect(10);

#line  1580 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else if (StartOf(24)) {
			EmbeddedStatement(
#line  1581 "cs.ATG" 
out stmt);

#line  1581 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(166);
	}

	void Argument(
#line  1494 "cs.ATG" 
out Expression argumentexpr) {

#line  1496 "cs.ATG" 
		Expression expr;
		FieldDirection fd = FieldDirection.None;
		
		if (la.kind == 92 || la.kind == 99) {
			if (la.kind == 99) {
				lexer.NextToken();

#line  1501 "cs.ATG" 
				fd = FieldDirection.Ref; 
			} else {
				lexer.NextToken();

#line  1502 "cs.ATG" 
				fd = FieldDirection.Out; 
			}
		}
		Expr(
#line  1504 "cs.ATG" 
out expr);

#line  1504 "cs.ATG" 
		argumentexpr = fd != FieldDirection.None ? argumentexpr = new DirectionExpression(fd, expr) : expr; 
	}

	void ArrayInitializer(
#line  1523 "cs.ATG" 
out Expression outExpr) {

#line  1525 "cs.ATG" 
		Expression expr = null;
		ArrayInitializerExpression initializer = new ArrayInitializerExpression();
		
		Expect(14);
		if (StartOf(25)) {
			VariableInitializer(
#line  1530 "cs.ATG" 
out expr);

#line  1530 "cs.ATG" 
			initializer.CreateExpressions.Add(expr); 
			while (
#line  1530 "cs.ATG" 
NotFinalComma()) {
				Expect(12);
				VariableInitializer(
#line  1530 "cs.ATG" 
out expr);

#line  1530 "cs.ATG" 
				initializer.CreateExpressions.Add(expr); 
			}
			if (la.kind == 12) {
				lexer.NextToken();
			}
		}
		Expect(15);

#line  1531 "cs.ATG" 
		outExpr = initializer; 
	}

	void AssignmentOperator(
#line  1507 "cs.ATG" 
out AssignmentOperatorType op) {

#line  1508 "cs.ATG" 
		op = AssignmentOperatorType.None; 
		switch (la.kind) {
		case 3: {
			lexer.NextToken();

#line  1510 "cs.ATG" 
			op = AssignmentOperatorType.Assign; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1511 "cs.ATG" 
			op = AssignmentOperatorType.Add; 
			break;
		}
		case 38: {
			lexer.NextToken();

#line  1512 "cs.ATG" 
			op = AssignmentOperatorType.Subtract; 
			break;
		}
		case 39: {
			lexer.NextToken();

#line  1513 "cs.ATG" 
			op = AssignmentOperatorType.Multiply; 
			break;
		}
		case 40: {
			lexer.NextToken();

#line  1514 "cs.ATG" 
			op = AssignmentOperatorType.Divide; 
			break;
		}
		case 41: {
			lexer.NextToken();

#line  1515 "cs.ATG" 
			op = AssignmentOperatorType.Modulus; 
			break;
		}
		case 42: {
			lexer.NextToken();

#line  1516 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseAnd; 
			break;
		}
		case 43: {
			lexer.NextToken();

#line  1517 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseOr; 
			break;
		}
		case 44: {
			lexer.NextToken();

#line  1518 "cs.ATG" 
			op = AssignmentOperatorType.ExclusiveOr; 
			break;
		}
		case 45: {
			lexer.NextToken();

#line  1519 "cs.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
			break;
		}
		case 46: {
			lexer.NextToken();

#line  1520 "cs.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
			break;
		}
		default: SynErr(167); break;
		}
	}

	void LocalVariableDecl(
#line  1534 "cs.ATG" 
out Statement stmt) {

#line  1536 "cs.ATG" 
		TypeReference type;
		VariableDeclaration      var = null;
		LocalVariableDeclaration localVariableDeclaration; 
		
		Type(
#line  1541 "cs.ATG" 
out type);

#line  1541 "cs.ATG" 
		localVariableDeclaration = new LocalVariableDeclaration(type); localVariableDeclaration.StartLocation = t.Location; 
		LocalVariableDeclarator(
#line  1542 "cs.ATG" 
out var);

#line  1542 "cs.ATG" 
		localVariableDeclaration.Variables.Add(var); 
		while (la.kind == 12) {
			lexer.NextToken();
			LocalVariableDeclarator(
#line  1543 "cs.ATG" 
out var);

#line  1543 "cs.ATG" 
			localVariableDeclaration.Variables.Add(var); 
		}

#line  1544 "cs.ATG" 
		stmt = localVariableDeclaration; 
	}

	void LocalVariableDeclarator(
#line  1547 "cs.ATG" 
out VariableDeclaration var) {

#line  1548 "cs.ATG" 
		Expression expr = null; 
		Expect(1);

#line  1550 "cs.ATG" 
		var = new VariableDeclaration(t.val); 
		if (la.kind == 3) {
			lexer.NextToken();
			LocalVariableInitializer(
#line  1550 "cs.ATG" 
out expr);

#line  1550 "cs.ATG" 
			var.Initializer = expr; 
		}
	}

	void LocalVariableInitializer(
#line  1553 "cs.ATG" 
out Expression expr) {

#line  1554 "cs.ATG" 
		expr = null; 
		if (StartOf(4)) {
			Expr(
#line  1556 "cs.ATG" 
out expr);
		} else if (la.kind == 14) {
			ArrayInitializer(
#line  1557 "cs.ATG" 
out expr);
		} else SynErr(168);
	}

	void EmbeddedStatement(
#line  1587 "cs.ATG" 
out Statement statement) {

#line  1589 "cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		
		if (la.kind == 14) {
			Block(
#line  1595 "cs.ATG" 
out statement);
		} else if (la.kind == 10) {
			lexer.NextToken();

#line  1597 "cs.ATG" 
			statement = new EmptyStatement(); 
		} else if (
#line  1599 "cs.ATG" 
IdentIsYield ()) {
			Expect(1);
			if (la.kind == 100) {
				lexer.NextToken();
				Expr(
#line  1600 "cs.ATG" 
out expr);
			} else if (la.kind == 52) {
				lexer.NextToken();
			} else SynErr(169);
			Expect(10);

#line  1600 "cs.ATG" 
			statement = new YieldStatement(expr); 
		} else if (
#line  1602 "cs.ATG" 
UnCheckedAndLBrace()) {

#line  1602 "cs.ATG" 
			Statement block; bool isChecked = true; 
			if (la.kind == 57) {
				lexer.NextToken();
			} else if (la.kind == 117) {
				lexer.NextToken();

#line  1603 "cs.ATG" 
				isChecked = false;
			} else SynErr(170);
			Block(
#line  1604 "cs.ATG" 
out block);

#line  1604 "cs.ATG" 
			statement = isChecked ? (Statement)new CheckedStatement(block) : (Statement)new UncheckedStatement(block); 
		} else if (StartOf(4)) {
			StatementExpr(
#line  1606 "cs.ATG" 
out statement);
			Expect(10);
		} else if (la.kind == 78) {
			lexer.NextToken();

#line  1608 "cs.ATG" 
			Statement elseStatement = null; 
			Expect(18);
			Expr(
#line  1609 "cs.ATG" 
out expr);
			Expect(19);
			EmbeddedStatement(
#line  1610 "cs.ATG" 
out embeddedStatement);
			if (la.kind == 66) {
				lexer.NextToken();
				EmbeddedStatement(
#line  1611 "cs.ATG" 
out elseStatement);
			}

#line  1612 "cs.ATG" 
			statement = elseStatement != null ? (Statement)new IfElseStatement(expr, embeddedStatement, elseStatement) :  (Statement)new IfStatement(expr, embeddedStatement); 
		} else if (la.kind == 109) {
			lexer.NextToken();

#line  1613 "cs.ATG" 
			ArrayList switchSections = new ArrayList(); 
			Expect(18);
			Expr(
#line  1614 "cs.ATG" 
out expr);
			Expect(19);
			Expect(14);
			while (la.kind == 54 || la.kind == 62) {
				SwitchSection(
#line  1615 "cs.ATG" 
out statement);

#line  1615 "cs.ATG" 
				switchSections.Add(statement); 
			}
			Expect(15);

#line  1616 "cs.ATG" 
			statement = new SwitchStatement(expr, switchSections); 
		} else if (la.kind == 124) {
			lexer.NextToken();
			Expect(18);
			Expr(
#line  1618 "cs.ATG" 
out expr);
			Expect(19);
			EmbeddedStatement(
#line  1619 "cs.ATG" 
out embeddedStatement);

#line  1619 "cs.ATG" 
			statement = new WhileStatement(expr, embeddedStatement); 
		} else if (la.kind == 64) {
			lexer.NextToken();
			EmbeddedStatement(
#line  1620 "cs.ATG" 
out embeddedStatement);
			Expect(124);
			Expect(18);
			Expr(
#line  1621 "cs.ATG" 
out expr);
			Expect(19);
			Expect(10);

#line  1621 "cs.ATG" 
			statement = new DoWhileStatement(expr, embeddedStatement); 
		} else if (la.kind == 75) {
			lexer.NextToken();

#line  1622 "cs.ATG" 
			ArrayList initializer = null, iterator = null; 
			Expect(18);
			if (StartOf(4)) {
				ForInitializer(
#line  1623 "cs.ATG" 
out initializer);
			}
			Expect(10);
			if (StartOf(4)) {
				Expr(
#line  1624 "cs.ATG" 
out expr);
			}
			Expect(10);
			if (StartOf(4)) {
				ForIterator(
#line  1625 "cs.ATG" 
out iterator);
			}
			Expect(19);
			EmbeddedStatement(
#line  1626 "cs.ATG" 
out embeddedStatement);

#line  1626 "cs.ATG" 
			statement = new ForStatement(initializer, expr, iterator, embeddedStatement); 
		} else if (la.kind == 76) {
			lexer.NextToken();
			Expect(18);
			Type(
#line  1627 "cs.ATG" 
out type);
			Expect(1);

#line  1627 "cs.ATG" 
			string varName = t.val; 
			Expect(80);
			Expr(
#line  1628 "cs.ATG" 
out expr);
			Expect(19);
			EmbeddedStatement(
#line  1629 "cs.ATG" 
out embeddedStatement);

#line  1629 "cs.ATG" 
			statement = new ForeachStatement(type, varName , expr, embeddedStatement); 
			statement.EndLocation = t.EndLocation;
			
		} else if (la.kind == 52) {
			lexer.NextToken();
			Expect(10);

#line  1633 "cs.ATG" 
			statement = new BreakStatement(); 
		} else if (la.kind == 60) {
			lexer.NextToken();
			Expect(10);

#line  1634 "cs.ATG" 
			statement = new ContinueStatement(); 
		} else if (la.kind == 77) {
			GotoStatement(
#line  1635 "cs.ATG" 
out statement);
		} else if (la.kind == 100) {
			lexer.NextToken();
			if (StartOf(4)) {
				Expr(
#line  1636 "cs.ATG" 
out expr);
			}
			Expect(10);

#line  1636 "cs.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 111) {
			lexer.NextToken();
			if (StartOf(4)) {
				Expr(
#line  1637 "cs.ATG" 
out expr);
			}
			Expect(10);

#line  1637 "cs.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (la.kind == 113) {
			TryStatement(
#line  1640 "cs.ATG" 
out statement);
		} else if (la.kind == 85) {
			lexer.NextToken();
			Expect(18);
			Expr(
#line  1642 "cs.ATG" 
out expr);
			Expect(19);
			EmbeddedStatement(
#line  1643 "cs.ATG" 
out embeddedStatement);

#line  1643 "cs.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 120) {

#line  1645 "cs.ATG" 
			Statement resourceAcquisitionStmt = null; 
			lexer.NextToken();
			Expect(18);
			ResourceAcquisition(
#line  1647 "cs.ATG" 
out resourceAcquisitionStmt);
			Expect(19);
			EmbeddedStatement(
#line  1648 "cs.ATG" 
out embeddedStatement);

#line  1648 "cs.ATG" 
			statement = new UsingStatement(resourceAcquisitionStmt, embeddedStatement); 
		} else if (la.kind == 118) {
			lexer.NextToken();
			Block(
#line  1650 "cs.ATG" 
out embeddedStatement);

#line  1650 "cs.ATG" 
			statement = new UnsafeStatement(embeddedStatement); 
		} else if (la.kind == 73) {
			lexer.NextToken();
			Expect(18);
			Type(
#line  1653 "cs.ATG" 
out type);

#line  1653 "cs.ATG" 
			if (type.PointerNestingLevel == 0) Error("can only fix pointer types");
			FixedStatement fxStmt = new FixedStatement(type);
			string identifier = null;
			
			Expect(1);

#line  1657 "cs.ATG" 
			identifier = t.val; 
			Expect(3);
			Expr(
#line  1658 "cs.ATG" 
out expr);

#line  1658 "cs.ATG" 
			fxStmt.PointerDeclarators.Add(new VariableDeclaration(identifier, expr)); 
			while (la.kind == 12) {
				lexer.NextToken();
				Expect(1);

#line  1660 "cs.ATG" 
				identifier = t.val; 
				Expect(3);
				Expr(
#line  1661 "cs.ATG" 
out expr);

#line  1661 "cs.ATG" 
				fxStmt.PointerDeclarators.Add(new VariableDeclaration(identifier, expr)); 
			}
			Expect(19);
			EmbeddedStatement(
#line  1663 "cs.ATG" 
out embeddedStatement);

#line  1663 "cs.ATG" 
			fxStmt.EmbeddedStatement = embeddedStatement; statement = fxStmt;
		} else SynErr(171);
	}

	void StatementExpr(
#line  1771 "cs.ATG" 
out Statement stmt) {

#line  1776 "cs.ATG" 
		bool mustBeAssignment = la.kind == Tokens.Plus  || la.kind == Tokens.Minus ||
		                       la.kind == Tokens.Not   || la.kind == Tokens.BitwiseComplement ||
		                       la.kind == Tokens.Times || la.kind == Tokens.BitwiseAnd   || IsTypeCast();
		Expression expr = null;
		
		UnaryExpr(
#line  1782 "cs.ATG" 
out expr);
		if (StartOf(6)) {

#line  1785 "cs.ATG" 
			AssignmentOperatorType op; Expression val; 
			AssignmentOperator(
#line  1785 "cs.ATG" 
out op);
			Expr(
#line  1785 "cs.ATG" 
out val);

#line  1785 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, val); 
		} else if (la.kind == 10 || la.kind == 12 || la.kind == 19) {

#line  1786 "cs.ATG" 
			if (mustBeAssignment) Error("error in assignment."); 
		} else SynErr(172);

#line  1787 "cs.ATG" 
		stmt = new StatementExpression(expr); 
	}

	void SwitchSection(
#line  1685 "cs.ATG" 
out Statement stmt) {

#line  1687 "cs.ATG" 
		SwitchSection switchSection = new SwitchSection();
		Expression expr;
		
		SwitchLabel(
#line  1691 "cs.ATG" 
out expr);

#line  1691 "cs.ATG" 
		switchSection.SwitchLabels.Add(expr); 
		while (la.kind == 54 || la.kind == 62) {
			SwitchLabel(
#line  1691 "cs.ATG" 
out expr);

#line  1691 "cs.ATG" 
			switchSection.SwitchLabels.Add(expr); 
		}

#line  1692 "cs.ATG" 
		compilationUnit.BlockStart(switchSection); 
		Statement();
		while (StartOf(20)) {
			Statement();
		}

#line  1695 "cs.ATG" 
		compilationUnit.BlockEnd();
		stmt = switchSection;
		
	}

	void ForInitializer(
#line  1666 "cs.ATG" 
out ArrayList initializer) {

#line  1668 "cs.ATG" 
		Statement stmt; 
		initializer = new ArrayList();
		
		if (
#line  1672 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1672 "cs.ATG" 
out stmt);

#line  1672 "cs.ATG" 
			initializer.Add(stmt);
		} else if (StartOf(4)) {
			StatementExpr(
#line  1673 "cs.ATG" 
out stmt);

#line  1673 "cs.ATG" 
			initializer.Add(stmt);
			while (la.kind == 12) {
				lexer.NextToken();
				StatementExpr(
#line  1673 "cs.ATG" 
out stmt);

#line  1673 "cs.ATG" 
				initializer.Add(stmt);
			}

#line  1673 "cs.ATG" 
			initializer.Add(stmt);
		} else SynErr(173);
	}

	void ForIterator(
#line  1676 "cs.ATG" 
out ArrayList iterator) {

#line  1678 "cs.ATG" 
		Statement stmt; 
		iterator = new ArrayList();
		
		StatementExpr(
#line  1682 "cs.ATG" 
out stmt);

#line  1682 "cs.ATG" 
		iterator.Add(stmt);
		while (la.kind == 12) {
			lexer.NextToken();
			StatementExpr(
#line  1682 "cs.ATG" 
out stmt);

#line  1682 "cs.ATG" 
			iterator.Add(stmt); 
		}
	}

	void GotoStatement(
#line  1744 "cs.ATG" 
out Statement stmt) {

#line  1745 "cs.ATG" 
		Expression expr; stmt = null; 
		Expect(77);
		if (la.kind == 1) {
			lexer.NextToken();

#line  1749 "cs.ATG" 
			stmt = new GotoStatement(t.val); 
			Expect(10);
		} else if (la.kind == 54) {
			lexer.NextToken();
			Expr(
#line  1750 "cs.ATG" 
out expr);
			Expect(10);

#line  1750 "cs.ATG" 
			stmt = new GotoCaseStatement(expr); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(10);

#line  1751 "cs.ATG" 
			stmt = new GotoCaseStatement(null); 
		} else SynErr(174);
	}

	void TryStatement(
#line  1707 "cs.ATG" 
out Statement tryStatement) {

#line  1709 "cs.ATG" 
		Statement blockStmt = null, finallyStmt = null;
		ArrayList catchClauses = null;
		
		Expect(113);
		Block(
#line  1713 "cs.ATG" 
out blockStmt);
		if (la.kind == 55) {
			CatchClauses(
#line  1715 "cs.ATG" 
out catchClauses);
			if (la.kind == 72) {
				lexer.NextToken();
				Block(
#line  1715 "cs.ATG" 
out finallyStmt);
			}
		} else if (la.kind == 72) {
			lexer.NextToken();
			Block(
#line  1716 "cs.ATG" 
out finallyStmt);
		} else SynErr(175);

#line  1719 "cs.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		
	}

	void ResourceAcquisition(
#line  1755 "cs.ATG" 
out Statement stmt) {

#line  1757 "cs.ATG" 
		stmt = null;
		Expression expr;
		
		if (
#line  1762 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1762 "cs.ATG" 
out stmt);
		} else if (StartOf(4)) {
			Expr(
#line  1763 "cs.ATG" 
out expr);

#line  1767 "cs.ATG" 
			stmt = new StatementExpression(expr); 
		} else SynErr(176);
	}

	void SwitchLabel(
#line  1700 "cs.ATG" 
out Expression expr) {

#line  1701 "cs.ATG" 
		expr = null; 
		if (la.kind == 54) {
			lexer.NextToken();
			Expr(
#line  1703 "cs.ATG" 
out expr);
			Expect(9);
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(9);
		} else SynErr(177);
	}

	void CatchClauses(
#line  1724 "cs.ATG" 
out ArrayList catchClauses) {

#line  1726 "cs.ATG" 
		catchClauses = new ArrayList();
		
		Expect(55);

#line  1729 "cs.ATG" 
		string name;
		string identifier;
		Statement stmt; 
		
		if (la.kind == 14) {
			Block(
#line  1735 "cs.ATG" 
out stmt);

#line  1735 "cs.ATG" 
			catchClauses.Add(new CatchClause(stmt)); 
		} else if (la.kind == 18) {
			lexer.NextToken();
			ClassType(
#line  1737 "cs.ATG" 
out name);

#line  1737 "cs.ATG" 
			identifier = null; 
			if (la.kind == 1) {
				lexer.NextToken();

#line  1737 "cs.ATG" 
				identifier = t.val; 
			}
			Expect(19);
			Block(
#line  1737 "cs.ATG" 
out stmt);

#line  1737 "cs.ATG" 
			catchClauses.Add(new CatchClause(name, identifier, stmt)); 
			while (
#line  1738 "cs.ATG" 
IsTypedCatch()) {
				Expect(55);
				Expect(18);
				ClassType(
#line  1738 "cs.ATG" 
out name);

#line  1738 "cs.ATG" 
				identifier = null; 
				if (la.kind == 1) {
					lexer.NextToken();

#line  1738 "cs.ATG" 
					identifier = t.val; 
				}
				Expect(19);
				Block(
#line  1738 "cs.ATG" 
out stmt);

#line  1738 "cs.ATG" 
				catchClauses.Add(new CatchClause(name, identifier, stmt)); 
			}
			if (la.kind == 55) {
				lexer.NextToken();
				Block(
#line  1740 "cs.ATG" 
out stmt);

#line  1740 "cs.ATG" 
				catchClauses.Add(new CatchClause(stmt)); 
			}
		} else SynErr(178);
	}

	void UnaryExpr(
#line  1803 "cs.ATG" 
out Expression uExpr) {

#line  1805 "cs.ATG" 
		TypeReference type = null;
		Expression expr;
		ArrayList  expressions = new ArrayList();
		uExpr = null;
		
		while (StartOf(26) || 
#line  1827 "cs.ATG" 
IsTypeCast()) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  1812 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Plus)); 
			} else if (la.kind == 5) {
				lexer.NextToken();

#line  1813 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Minus)); 
			} else if (la.kind == 22) {
				lexer.NextToken();

#line  1814 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Not)); 
			} else if (la.kind == 25) {
				lexer.NextToken();

#line  1815 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitNot)); 
			} else if (la.kind == 6) {
				lexer.NextToken();

#line  1816 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Star)); 
			} else if (la.kind == 29) {
				lexer.NextToken();

#line  1817 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Increment)); 
			} else if (la.kind == 30) {
				lexer.NextToken();

#line  1818 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Decrement)); 
			} else if (la.kind == 26) {
				lexer.NextToken();

#line  1819 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitWiseAnd)); 
			} else {
				Expect(18);
				Type(
#line  1827 "cs.ATG" 
out type);
				Expect(19);

#line  1827 "cs.ATG" 
				expressions.Add(new CastExpression(type)); 
			}
		}
		PrimaryExpr(
#line  1830 "cs.ATG" 
out expr);

#line  1830 "cs.ATG" 
		for (int i = 0; i < expressions.Count; ++i) {
		Expression nextExpression = i + 1 < expressions.Count ? (Expression)expressions[i + 1] : expr;
		if (expressions[i] is CastExpression) {
			((CastExpression)expressions[i]).Expression = nextExpression;
		} else {
			((UnaryOperatorExpression)expressions[i]).Expression = nextExpression;
		}
		}
		if (expressions.Count > 0) {
			uExpr = (Expression)expressions[0];
		} else {
			uExpr = expr;
		}
		
	}

	void ConditionalOrExpr(
#line  1932 "cs.ATG" 
ref Expression outExpr) {

#line  1933 "cs.ATG" 
		Expression expr;   
		ConditionalAndExpr(
#line  1935 "cs.ATG" 
ref outExpr);
		while (la.kind == 24) {
			lexer.NextToken();
			UnaryExpr(
#line  1935 "cs.ATG" 
out expr);
			ConditionalAndExpr(
#line  1935 "cs.ATG" 
ref expr);

#line  1935 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalOr, expr);  
		}
	}

	void PrimaryExpr(
#line  1847 "cs.ATG" 
out Expression pexpr) {

#line  1849 "cs.ATG" 
		TypeReference type = null;
		bool isArrayCreation = false;
		Expression expr;
		pexpr = null;
		
		switch (la.kind) {
		case 112: {
			lexer.NextToken();

#line  1856 "cs.ATG" 
			pexpr = new PrimitiveExpression(true, "true");  
			break;
		}
		case 71: {
			lexer.NextToken();

#line  1857 "cs.ATG" 
			pexpr = new PrimitiveExpression(false, "false"); 
			break;
		}
		case 89: {
			lexer.NextToken();

#line  1858 "cs.ATG" 
			pexpr = new PrimitiveExpression(null, "null");  
			break;
		}
		case 2: {
			lexer.NextToken();

#line  1859 "cs.ATG" 
			pexpr = new PrimitiveExpression(t.literalValue, t.val);  
			break;
		}
		case 1: {
			lexer.NextToken();

#line  1861 "cs.ATG" 
			pexpr = new IdentifierExpression(t.val, t.Location); 
			break;
		}
		case 18: {
			lexer.NextToken();
			Expr(
#line  1863 "cs.ATG" 
out expr);
			Expect(19);

#line  1863 "cs.ATG" 
			pexpr = new ParenthesizedExpression(expr); 
			break;
		}
		case 51: case 53: case 56: case 61: case 65: case 74: case 81: case 86: case 90: case 101: case 103: case 107: case 115: case 116: case 119: {
			switch (la.kind) {
			case 51: {
				lexer.NextToken();
				break;
			}
			case 53: {
				lexer.NextToken();
				break;
			}
			case 56: {
				lexer.NextToken();
				break;
			}
			case 61: {
				lexer.NextToken();
				break;
			}
			case 65: {
				lexer.NextToken();
				break;
			}
			case 74: {
				lexer.NextToken();
				break;
			}
			case 81: {
				lexer.NextToken();
				break;
			}
			case 86: {
				lexer.NextToken();
				break;
			}
			case 90: {
				lexer.NextToken();
				break;
			}
			case 101: {
				lexer.NextToken();
				break;
			}
			case 103: {
				lexer.NextToken();
				break;
			}
			case 107: {
				lexer.NextToken();
				break;
			}
			case 115: {
				lexer.NextToken();
				break;
			}
			case 116: {
				lexer.NextToken();
				break;
			}
			case 119: {
				lexer.NextToken();
				break;
			}
			}

#line  1869 "cs.ATG" 
			string val = t.val; t.val = ""; 
			Expect(13);
			Expect(1);

#line  1869 "cs.ATG" 
			pexpr = new FieldReferenceExpression(new TypeReferenceExpression(val), t.val, t.Location); 
			break;
		}
		case 110: {
			lexer.NextToken();

#line  1871 "cs.ATG" 
			pexpr = new ThisReferenceExpression(); 
			break;
		}
		case 50: {
			lexer.NextToken();

#line  1873 "cs.ATG" 
			Expression retExpr = new BaseReferenceExpression(); 
			if (la.kind == 13) {
				lexer.NextToken();
				Expect(1);

#line  1875 "cs.ATG" 
				retExpr = new FieldReferenceExpression(retExpr, t.val, t.Location); 
			} else if (la.kind == 16) {
				lexer.NextToken();
				Expr(
#line  1876 "cs.ATG" 
out expr);

#line  1876 "cs.ATG" 
				ArrayList indices = new ArrayList(); indices.Add(expr); 
				while (la.kind == 12) {
					lexer.NextToken();
					Expr(
#line  1877 "cs.ATG" 
out expr);

#line  1877 "cs.ATG" 
					indices.Add(expr); 
				}
				Expect(17);

#line  1878 "cs.ATG" 
				retExpr = new IndexerExpression(retExpr, indices); 
			} else SynErr(179);

#line  1879 "cs.ATG" 
			pexpr = retExpr; 
			break;
		}
		case 88: {
			lexer.NextToken();
			NonArrayType(
#line  1880 "cs.ATG" 
out type);

#line  1880 "cs.ATG" 
			ArrayList parameters = new ArrayList(); 
			if (la.kind == 18) {
				lexer.NextToken();

#line  1885 "cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters); 
				if (StartOf(21)) {
					Argument(
#line  1885 "cs.ATG" 
out expr);

#line  1885 "cs.ATG" 
					parameters.Add(expr); 
					while (la.kind == 12) {
						lexer.NextToken();
						Argument(
#line  1886 "cs.ATG" 
out expr);

#line  1886 "cs.ATG" 
						parameters.Add(expr); 
					}
				}
				Expect(19);

#line  1886 "cs.ATG" 
				pexpr = oce; 
			} else if (la.kind == 16) {

#line  1888 "cs.ATG" 
				isArrayCreation = true; ArrayCreateExpression ace = new ArrayCreateExpression(type); pexpr = ace; 
				lexer.NextToken();

#line  1889 "cs.ATG" 
				int dims = 0; ArrayList rank = new ArrayList(); ArrayList parameterExpression = new ArrayList(); 
				if (StartOf(4)) {
					Expr(
#line  1891 "cs.ATG" 
out expr);

#line  1891 "cs.ATG" 
					parameterExpression.Add(expr); 
					while (la.kind == 12) {
						lexer.NextToken();
						Expr(
#line  1891 "cs.ATG" 
out expr);

#line  1891 "cs.ATG" 
						parameterExpression.Add(expr); 
					}
					Expect(17);

#line  1891 "cs.ATG" 
					parameters.Add(new ArrayCreationParameter(parameterExpression)); ace.Parameters = parameters; 
					while (
#line  1892 "cs.ATG" 
IsDims()) {
						Expect(16);

#line  1892 "cs.ATG" 
						dims =0;
						while (la.kind == 12) {
							lexer.NextToken();

#line  1892 "cs.ATG" 
							dims++;
						}

#line  1892 "cs.ATG" 
						rank.Add(dims); parameters.Add(new ArrayCreationParameter(dims)); 
						Expect(17);
					}

#line  1893 "cs.ATG" 
					if (rank.Count > 0) { ace.Rank = (int[])rank.ToArray(typeof (int)); } 
					if (la.kind == 14) {
						ArrayInitializer(
#line  1894 "cs.ATG" 
out expr);

#line  1894 "cs.ATG" 
						ace.ArrayInitializer = (ArrayInitializerExpression)expr; 
					}
				} else if (la.kind == 12 || la.kind == 17) {
					while (la.kind == 12) {
						lexer.NextToken();

#line  1896 "cs.ATG" 
						dims++;
					}

#line  1896 "cs.ATG" 
					parameters.Add(new ArrayCreationParameter(dims)); 
					Expect(17);
					while (
#line  1896 "cs.ATG" 
IsDims()) {
						Expect(16);

#line  1896 "cs.ATG" 
						dims =0;
						while (la.kind == 12) {
							lexer.NextToken();

#line  1896 "cs.ATG" 
							dims++;
						}

#line  1896 "cs.ATG" 
						parameters.Add(new ArrayCreationParameter(dims)); 
						Expect(17);
					}
					ArrayInitializer(
#line  1896 "cs.ATG" 
out expr);

#line  1896 "cs.ATG" 
					ace.ArrayInitializer = (ArrayInitializerExpression)expr; ace.Parameters = parameters; 
				} else SynErr(180);
			} else SynErr(181);
			break;
		}
		case 114: {
			lexer.NextToken();
			Expect(18);
			if (
#line  1902 "cs.ATG" 
NotVoidPointer()) {
				Expect(122);

#line  1902 "cs.ATG" 
				type = new TypeReference("void"); 
			} else if (StartOf(8)) {
				Type(
#line  1903 "cs.ATG" 
out type);
			} else SynErr(182);
			Expect(19);

#line  1904 "cs.ATG" 
			pexpr = new TypeOfExpression(type); 
			break;
		}
		case 104: {
			lexer.NextToken();
			Expect(18);
			Type(
#line  1905 "cs.ATG" 
out type);
			Expect(19);

#line  1905 "cs.ATG" 
			pexpr = new SizeOfExpression(type); 
			break;
		}
		case 57: {
			lexer.NextToken();
			Expect(18);
			Expr(
#line  1906 "cs.ATG" 
out expr);
			Expect(19);

#line  1906 "cs.ATG" 
			pexpr = new CheckedExpression(expr); 
			break;
		}
		case 117: {
			lexer.NextToken();
			Expect(18);
			Expr(
#line  1907 "cs.ATG" 
out expr);
			Expect(19);

#line  1907 "cs.ATG" 
			pexpr = new CheckedExpression(expr); 
			break;
		}
		default: SynErr(183); break;
		}
		while (StartOf(27)) {
			if (la.kind == 29 || la.kind == 30) {
				if (la.kind == 29) {
					lexer.NextToken();

#line  1911 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostIncrement); 
				} else if (la.kind == 30) {
					lexer.NextToken();

#line  1912 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostDecrement); 
				} else SynErr(184);
			} else if (la.kind == 47) {
				lexer.NextToken();
				Expect(1);

#line  1915 "cs.ATG" 
				pexpr = new PointerReferenceExpression(pexpr, t.val); 
			} else if (la.kind == 13) {
				lexer.NextToken();
				Expect(1);

#line  1916 "cs.ATG" 
				pexpr = new FieldReferenceExpression(pexpr, t.val, t.Location);
			} else if (la.kind == 18) {
				lexer.NextToken();

#line  1918 "cs.ATG" 
				ArrayList parameters = new ArrayList(); 
				if (StartOf(21)) {
					Argument(
#line  1919 "cs.ATG" 
out expr);

#line  1919 "cs.ATG" 
					parameters.Add(expr); 
					while (la.kind == 12) {
						lexer.NextToken();
						Argument(
#line  1920 "cs.ATG" 
out expr);

#line  1920 "cs.ATG" 
						parameters.Add(expr); 
					}
				}
				Expect(19);

#line  1921 "cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters); 
			} else {

#line  1923 "cs.ATG" 
				if (isArrayCreation) Error("element access not allow on array creation");
				ArrayList indices = new ArrayList();
				
				lexer.NextToken();
				Expr(
#line  1926 "cs.ATG" 
out expr);

#line  1926 "cs.ATG" 
				indices.Add(expr); 
				while (la.kind == 12) {
					lexer.NextToken();
					Expr(
#line  1927 "cs.ATG" 
out expr);

#line  1927 "cs.ATG" 
					indices.Add(expr); 
				}
				Expect(17);

#line  1928 "cs.ATG" 
				pexpr = new IndexerExpression(pexpr, indices); 
			}
		}
	}

	void ConditionalAndExpr(
#line  1938 "cs.ATG" 
ref Expression outExpr) {

#line  1939 "cs.ATG" 
		Expression expr; 
		InclusiveOrExpr(
#line  1941 "cs.ATG" 
ref outExpr);
		while (la.kind == 23) {
			lexer.NextToken();
			UnaryExpr(
#line  1941 "cs.ATG" 
out expr);
			InclusiveOrExpr(
#line  1941 "cs.ATG" 
ref expr);

#line  1941 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalAnd, expr);  
		}
	}

	void InclusiveOrExpr(
#line  1944 "cs.ATG" 
ref Expression outExpr) {

#line  1945 "cs.ATG" 
		Expression expr; 
		ExclusiveOrExpr(
#line  1947 "cs.ATG" 
ref outExpr);
		while (la.kind == 27) {
			lexer.NextToken();
			UnaryExpr(
#line  1947 "cs.ATG" 
out expr);
			ExclusiveOrExpr(
#line  1947 "cs.ATG" 
ref expr);

#line  1947 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseOr, expr);  
		}
	}

	void ExclusiveOrExpr(
#line  1950 "cs.ATG" 
ref Expression outExpr) {

#line  1951 "cs.ATG" 
		Expression expr; 
		AndExpr(
#line  1953 "cs.ATG" 
ref outExpr);
		while (la.kind == 28) {
			lexer.NextToken();
			UnaryExpr(
#line  1953 "cs.ATG" 
out expr);
			AndExpr(
#line  1953 "cs.ATG" 
ref expr);

#line  1953 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.ExclusiveOr, expr);  
		}
	}

	void AndExpr(
#line  1956 "cs.ATG" 
ref Expression outExpr) {

#line  1957 "cs.ATG" 
		Expression expr; 
		EqualityExpr(
#line  1959 "cs.ATG" 
ref outExpr);
		while (la.kind == 26) {
			lexer.NextToken();
			UnaryExpr(
#line  1959 "cs.ATG" 
out expr);
			EqualityExpr(
#line  1959 "cs.ATG" 
ref expr);

#line  1959 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseAnd, expr);  
		}
	}

	void EqualityExpr(
#line  1962 "cs.ATG" 
ref Expression outExpr) {

#line  1964 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		RelationalExpr(
#line  1968 "cs.ATG" 
ref outExpr);
		while (la.kind == 31 || la.kind == 32) {
			if (la.kind == 32) {
				lexer.NextToken();

#line  1971 "cs.ATG" 
				op = BinaryOperatorType.InEquality; 
			} else {
				lexer.NextToken();

#line  1972 "cs.ATG" 
				op = BinaryOperatorType.Equality; 
			}
			UnaryExpr(
#line  1974 "cs.ATG" 
out expr);
			RelationalExpr(
#line  1974 "cs.ATG" 
ref expr);

#line  1974 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void RelationalExpr(
#line  1978 "cs.ATG" 
ref Expression outExpr) {

#line  1980 "cs.ATG" 
		TypeReference type;
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  1985 "cs.ATG" 
ref outExpr);
		while (StartOf(28)) {
			if (StartOf(29)) {
				if (la.kind == 21) {
					lexer.NextToken();

#line  1988 "cs.ATG" 
					op = BinaryOperatorType.LessThan; 
				} else if (la.kind == 20) {
					lexer.NextToken();

#line  1989 "cs.ATG" 
					op = BinaryOperatorType.GreaterThan; 
				} else if (la.kind == 34) {
					lexer.NextToken();

#line  1990 "cs.ATG" 
					op = BinaryOperatorType.LessThanOrEqual; 
				} else if (la.kind == 33) {
					lexer.NextToken();

#line  1991 "cs.ATG" 
					op = BinaryOperatorType.GreaterThanOrEqual; 
				} else SynErr(185);
				UnaryExpr(
#line  1993 "cs.ATG" 
out expr);
				ShiftExpr(
#line  1993 "cs.ATG" 
ref expr);

#line  1993 "cs.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
			} else {
				if (la.kind == 84) {
					lexer.NextToken();

#line  1996 "cs.ATG" 
					op = BinaryOperatorType.IS; 
				} else if (la.kind == 49) {
					lexer.NextToken();

#line  1997 "cs.ATG" 
					op = BinaryOperatorType.AS; 
				} else SynErr(186);
				Type(
#line  1999 "cs.ATG" 
out type);

#line  1999 "cs.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, new TypeReferenceExpression(type)); 
			}
		}
	}

	void ShiftExpr(
#line  2003 "cs.ATG" 
ref Expression outExpr) {

#line  2005 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		AdditiveExpr(
#line  2009 "cs.ATG" 
ref outExpr);
		while (la.kind == 35 || la.kind == 36) {
			if (la.kind == 35) {
				lexer.NextToken();

#line  2012 "cs.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				lexer.NextToken();

#line  2013 "cs.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			UnaryExpr(
#line  2015 "cs.ATG" 
out expr);
			AdditiveExpr(
#line  2015 "cs.ATG" 
ref expr);

#line  2015 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AdditiveExpr(
#line  2019 "cs.ATG" 
ref Expression outExpr) {

#line  2021 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		MultiplicativeExpr(
#line  2025 "cs.ATG" 
ref outExpr);
		while (la.kind == 4 || la.kind == 5) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  2028 "cs.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  2029 "cs.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			UnaryExpr(
#line  2031 "cs.ATG" 
out expr);
			MultiplicativeExpr(
#line  2031 "cs.ATG" 
ref expr);

#line  2031 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void MultiplicativeExpr(
#line  2035 "cs.ATG" 
ref Expression outExpr) {

#line  2037 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		while (la.kind == 6 || la.kind == 7 || la.kind == 8) {
			if (la.kind == 6) {
				lexer.NextToken();

#line  2043 "cs.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else if (la.kind == 7) {
				lexer.NextToken();

#line  2044 "cs.ATG" 
				op = BinaryOperatorType.Divide; 
			} else {
				lexer.NextToken();

#line  2045 "cs.ATG" 
				op = BinaryOperatorType.Modulus; 
			}
			UnaryExpr(
#line  2047 "cs.ATG" 
out expr);

#line  2047 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}



	public void Parse(Lexer lexer)
	{
		this.errors = lexer.Errors;
		this.lexer = lexer;
		errors.SynErr = new ErrorCodeProc(SynErr);
		lexer.NextToken();
		CS();

	}

	void SynErr(int line, int col, int errorNumber)
	{
		errors.count++; 
		string s;
		switch (errorNumber) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "literal expected"; break;
			case 3: s = "\"=\" expected"; break;
			case 4: s = "\"+\" expected"; break;
			case 5: s = "\"-\" expected"; break;
			case 6: s = "\"*\" expected"; break;
			case 7: s = "\"/\" expected"; break;
			case 8: s = "\"%\" expected"; break;
			case 9: s = "\":\" expected"; break;
			case 10: s = "\";\" expected"; break;
			case 11: s = "\"?\" expected"; break;
			case 12: s = "\",\" expected"; break;
			case 13: s = "\".\" expected"; break;
			case 14: s = "\"{\" expected"; break;
			case 15: s = "\"}\" expected"; break;
			case 16: s = "\"[\" expected"; break;
			case 17: s = "\"]\" expected"; break;
			case 18: s = "\"(\" expected"; break;
			case 19: s = "\")\" expected"; break;
			case 20: s = "\">\" expected"; break;
			case 21: s = "\"<\" expected"; break;
			case 22: s = "\"!\" expected"; break;
			case 23: s = "\"&&\" expected"; break;
			case 24: s = "\"||\" expected"; break;
			case 25: s = "\"~\" expected"; break;
			case 26: s = "\"&\" expected"; break;
			case 27: s = "\"|\" expected"; break;
			case 28: s = "\"^\" expected"; break;
			case 29: s = "\"++\" expected"; break;
			case 30: s = "\"--\" expected"; break;
			case 31: s = "\"==\" expected"; break;
			case 32: s = "\"!=\" expected"; break;
			case 33: s = "\">=\" expected"; break;
			case 34: s = "\"<=\" expected"; break;
			case 35: s = "\"<<\" expected"; break;
			case 36: s = "\">>\" expected"; break;
			case 37: s = "\"+=\" expected"; break;
			case 38: s = "\"-=\" expected"; break;
			case 39: s = "\"*=\" expected"; break;
			case 40: s = "\"/=\" expected"; break;
			case 41: s = "\"%=\" expected"; break;
			case 42: s = "\"&=\" expected"; break;
			case 43: s = "\"|=\" expected"; break;
			case 44: s = "\"^=\" expected"; break;
			case 45: s = "\"<<=\" expected"; break;
			case 46: s = "\">>=\" expected"; break;
			case 47: s = "\"->\" expected"; break;
			case 48: s = "\"abstract\" expected"; break;
			case 49: s = "\"as\" expected"; break;
			case 50: s = "\"base\" expected"; break;
			case 51: s = "\"bool\" expected"; break;
			case 52: s = "\"break\" expected"; break;
			case 53: s = "\"byte\" expected"; break;
			case 54: s = "\"case\" expected"; break;
			case 55: s = "\"catch\" expected"; break;
			case 56: s = "\"char\" expected"; break;
			case 57: s = "\"checked\" expected"; break;
			case 58: s = "\"class\" expected"; break;
			case 59: s = "\"const\" expected"; break;
			case 60: s = "\"continue\" expected"; break;
			case 61: s = "\"decimal\" expected"; break;
			case 62: s = "\"default\" expected"; break;
			case 63: s = "\"delegate\" expected"; break;
			case 64: s = "\"do\" expected"; break;
			case 65: s = "\"double\" expected"; break;
			case 66: s = "\"else\" expected"; break;
			case 67: s = "\"enum\" expected"; break;
			case 68: s = "\"event\" expected"; break;
			case 69: s = "\"explicit\" expected"; break;
			case 70: s = "\"extern\" expected"; break;
			case 71: s = "\"false\" expected"; break;
			case 72: s = "\"finally\" expected"; break;
			case 73: s = "\"fixed\" expected"; break;
			case 74: s = "\"float\" expected"; break;
			case 75: s = "\"for\" expected"; break;
			case 76: s = "\"foreach\" expected"; break;
			case 77: s = "\"goto\" expected"; break;
			case 78: s = "\"if\" expected"; break;
			case 79: s = "\"implicit\" expected"; break;
			case 80: s = "\"in\" expected"; break;
			case 81: s = "\"int\" expected"; break;
			case 82: s = "\"interface\" expected"; break;
			case 83: s = "\"internal\" expected"; break;
			case 84: s = "\"is\" expected"; break;
			case 85: s = "\"lock\" expected"; break;
			case 86: s = "\"long\" expected"; break;
			case 87: s = "\"namespace\" expected"; break;
			case 88: s = "\"new\" expected"; break;
			case 89: s = "\"null\" expected"; break;
			case 90: s = "\"object\" expected"; break;
			case 91: s = "\"operator\" expected"; break;
			case 92: s = "\"out\" expected"; break;
			case 93: s = "\"override\" expected"; break;
			case 94: s = "\"params\" expected"; break;
			case 95: s = "\"private\" expected"; break;
			case 96: s = "\"protected\" expected"; break;
			case 97: s = "\"public\" expected"; break;
			case 98: s = "\"readonly\" expected"; break;
			case 99: s = "\"ref\" expected"; break;
			case 100: s = "\"return\" expected"; break;
			case 101: s = "\"sbyte\" expected"; break;
			case 102: s = "\"sealed\" expected"; break;
			case 103: s = "\"short\" expected"; break;
			case 104: s = "\"sizeof\" expected"; break;
			case 105: s = "\"stackalloc\" expected"; break;
			case 106: s = "\"static\" expected"; break;
			case 107: s = "\"string\" expected"; break;
			case 108: s = "\"struct\" expected"; break;
			case 109: s = "\"switch\" expected"; break;
			case 110: s = "\"this\" expected"; break;
			case 111: s = "\"throw\" expected"; break;
			case 112: s = "\"true\" expected"; break;
			case 113: s = "\"try\" expected"; break;
			case 114: s = "\"typeof\" expected"; break;
			case 115: s = "\"uint\" expected"; break;
			case 116: s = "\"ulong\" expected"; break;
			case 117: s = "\"unchecked\" expected"; break;
			case 118: s = "\"unsafe\" expected"; break;
			case 119: s = "\"ushort\" expected"; break;
			case 120: s = "\"using\" expected"; break;
			case 121: s = "\"virtual\" expected"; break;
			case 122: s = "\"void\" expected"; break;
			case 123: s = "\"volatile\" expected"; break;
			case 124: s = "\"while\" expected"; break;
			case 125: s = "??? expected"; break;
			case 126: s = "invalid NamespaceMemberDecl"; break;
			case 127: s = "invalid AttributeArguments"; break;
			case 128: s = "invalid Expr"; break;
			case 129: s = "invalid TypeModifier"; break;
			case 130: s = "invalid TypeDecl"; break;
			case 131: s = "invalid TypeDecl"; break;
			case 132: s = "invalid IntegralType"; break;
			case 133: s = "invalid Type"; break;
			case 134: s = "invalid Type"; break;
			case 135: s = "invalid FormalParameterList"; break;
			case 136: s = "invalid FormalParameterList"; break;
			case 137: s = "invalid ClassType"; break;
			case 138: s = "invalid MemberModifier"; break;
			case 139: s = "invalid ClassMemberDecl"; break;
			case 140: s = "invalid ClassMemberDecl"; break;
			case 141: s = "invalid StructMemberDecl"; break;
			case 142: s = "invalid StructMemberDecl"; break;
			case 143: s = "invalid StructMemberDecl"; break;
			case 144: s = "invalid StructMemberDecl"; break;
			case 145: s = "invalid StructMemberDecl"; break;
			case 146: s = "invalid StructMemberDecl"; break;
			case 147: s = "invalid StructMemberDecl"; break;
			case 148: s = "invalid StructMemberDecl"; break;
			case 149: s = "invalid StructMemberDecl"; break;
			case 150: s = "invalid StructMemberDecl"; break;
			case 151: s = "invalid InterfaceMemberDecl"; break;
			case 152: s = "invalid InterfaceMemberDecl"; break;
			case 153: s = "invalid InterfaceMemberDecl"; break;
			case 154: s = "invalid SimpleType"; break;
			case 155: s = "invalid NonArrayType"; break;
			case 156: s = "invalid AccessorModifier"; break;
			case 157: s = "invalid EventAccessorDecls"; break;
			case 158: s = "invalid ConstructorInitializer"; break;
			case 159: s = "invalid OverloadableOperator"; break;
			case 160: s = "invalid AccessorDecls"; break;
			case 161: s = "invalid InterfaceAccessors"; break;
			case 162: s = "invalid InterfaceAccessors"; break;
			case 163: s = "invalid GetAccessorDecl"; break;
			case 164: s = "invalid SetAccessorDecl"; break;
			case 165: s = "invalid VariableInitializer"; break;
			case 166: s = "invalid Statement"; break;
			case 167: s = "invalid AssignmentOperator"; break;
			case 168: s = "invalid LocalVariableInitializer"; break;
			case 169: s = "invalid EmbeddedStatement"; break;
			case 170: s = "invalid EmbeddedStatement"; break;
			case 171: s = "invalid EmbeddedStatement"; break;
			case 172: s = "invalid StatementExpr"; break;
			case 173: s = "invalid ForInitializer"; break;
			case 174: s = "invalid GotoStatement"; break;
			case 175: s = "invalid TryStatement"; break;
			case 176: s = "invalid ResourceAcquisition"; break;
			case 177: s = "invalid SwitchLabel"; break;
			case 178: s = "invalid CatchClauses"; break;
			case 179: s = "invalid PrimaryExpr"; break;
			case 180: s = "invalid PrimaryExpr"; break;
			case 181: s = "invalid PrimaryExpr"; break;
			case 182: s = "invalid PrimaryExpr"; break;
			case 183: s = "invalid PrimaryExpr"; break;
			case 184: s = "invalid PrimaryExpr"; break;
			case 185: s = "invalid RelationalExpr"; break;
			case 186: s = "invalid RelationalExpr"; break;

			default: s = "error " + errorNumber; break;
		}
		errors.Error(line, col, s);
	}

	static bool[,] set = {
	{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, T,T,T,T, T,T,T,T, T,x,x,T, x,T,x,T, T,T,x,T, T,x,T,T, T,x,x,T, T,T,T,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,T, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,x,x, x,x,T,x, x,x,x,T, x,T,T,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, T,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,T,x, x,x,T,x, x,x,T,x, x,x,T,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, T,T,x,T, T,T,x,x, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, T,x,x,x, x,x,x,T, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,T,x, x,x,T,x, x,x,T,x, x,x,T,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, T,T,x,x, T,T,x,x, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,T,x, x,x,T,x, x,x,T,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,T,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x}

	};
} // end Parser

}