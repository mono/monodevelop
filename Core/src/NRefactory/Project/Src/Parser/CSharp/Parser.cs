
#line  1 "cs.ATG" 
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;
using ASTAttribute = ICSharpCode.NRefactory.Parser.AST.Attribute;
using Types = ICSharpCode.NRefactory.Parser.AST.ClassType;
/*
  Parser.frame file for NRefactory.
 */
using System;
using System.Reflection;

namespace ICSharpCode.NRefactory.Parser.CSharp {



internal class Parser : AbstractParser
{
	const int maxT = 125;

	const  bool   T            = true;
	const  bool   x            = false;
	

#line  14 "cs.ATG" 
string        assemblyName     = null;
StringBuilder qualidentBuilder = new StringBuilder();

public string ContainingAssembly {
	set {
		assemblyName = value;
	}
}

Token t {
	[System.Diagnostics.DebuggerStepThrough]
	get {
		return lexer.Token;
	}
}

Token la {
	[System.Diagnostics.DebuggerStepThrough]
	get {
		return lexer.LookAhead;
	}
}

public void Error(string s)
{
	if (errDist >= minErrDist) {
		errors.Error(la.line, la.col, s);
	}
	errDist = 0;
}

public override Expression ParseExpression()
{
	lexer.NextToken();
	Expression expr;
	Expr(out expr);
	return expr;
}
// Begin ISTypeCast
bool IsTypeCast()
{
	if (la.kind != Tokens.OpenParenthesis) {
		return false;
	}
	if (IsSimpleTypeCast()) {
		return true;
	}
	return GuessTypeCast();
}

// "(" ( typeKW [ "[" {","} "]" | "*" ] | void  ( "[" {","} "]" | "*" ) ) ")"
// only for built-in types, all others use GuessTypeCast!
bool IsSimpleTypeCast ()
{
	// assert: la.kind == _lpar
	lexer.StartPeek();
	Token pt = lexer.Peek();
	
	if (!IsTypeKWForTypeCast(ref pt)) {
		return false;
	}
	if (pt.kind == Tokens.Question)
		pt = lexer.Peek();
	return pt.kind == Tokens.CloseParenthesis;
}

/* !!! Proceeds from current peek position !!! */
bool IsTypeKWForTypeCast(ref Token pt)
{
	if (Tokens.TypeKW[pt.kind]) {
		pt = lexer.Peek();
		if (pt.kind == Tokens.Times || pt.kind == Tokens.OpenSquareBracket) {
			return IsPointerOrDims(ref pt);
		} else {
		  return true;
		}
	} else if (pt.kind == Tokens.Void) {
		pt = lexer.Peek();
		return IsPointerOrDims(ref pt);
	}
	return false;
}

/* !!! Proceeds from current peek position !!! */
bool IsTypeNameOrKWForTypeCast(ref Token pt)
{
	if (IsTypeKWForTypeCast(ref pt))
		return true;
	else
		return IsTypeNameForTypeCast(ref pt);
}

// TypeName = ident [ "::" ident ] {  "." ident } ["<" TypeNameOrKW { "," TypeNameOrKW } ">" ] PointerOrDims
/* !!! Proceeds from current peek position !!! */
bool IsTypeNameForTypeCast(ref Token pt)
{
	// ident
	if (pt.kind != Tokens.Identifier) {
		return false;
	}	
	pt = Peek();
	// "::" ident
	if (pt.kind == Tokens.DoubleColon) {
		pt = Peek();
		if (pt.kind != Tokens.Identifier) {
			return false;
		}
		pt = Peek();
	}
	// { "." ident }
	while (pt.kind == Tokens.Dot) {
		pt = Peek();
		if (pt.kind != Tokens.Identifier) {
			return false;
		}
		pt = Peek();
	}
	if (pt.kind == Tokens.LessThan) {
		do {
			pt = Peek();
			if (!IsTypeNameOrKWForTypeCast(ref pt)) {
				return false;
			}
		} while (pt.kind == Tokens.Comma);
		if (pt.kind != Tokens.GreaterThan) {
			return false;
		}
		pt = Peek();
	}
	if (pt.kind == Tokens.Question) {
		pt = Peek();
	}
	if (pt.kind == Tokens.Times || pt.kind == Tokens.OpenSquareBracket) {
		return IsPointerOrDims(ref pt);
	}
	return true;
}

// "(" TypeName ")" castFollower
bool GuessTypeCast ()
{
	// assert: la.kind == _lpar
	StartPeek();
	Token pt = Peek();

	if (!IsTypeNameForTypeCast(ref pt)) {
		return false;
	}
	
	// ")"
	if (pt.kind != Tokens.CloseParenthesis) {
		return false;
	}
	// check successor
	pt = Peek();
	return Tokens.CastFollower[pt.kind] || (Tokens.TypeKW[pt.kind] && lexer.Peek().kind == Tokens.Dot);
}
// END IsTypeCast

/* Checks whether the next sequences of tokens is a qualident *
 * and returns the qualident string                           */
/* !!! Proceeds from current peek position !!! */
bool IsQualident(ref Token pt, out string qualident)
{
	if (pt.kind == Tokens.Identifier) {
		qualidentBuilder.Length = 0; qualidentBuilder.Append(pt.val);
		pt = Peek();
	while (pt.kind == Tokens.Dot || pt.kind == Tokens.DoubleColon) {
			pt = Peek();
			if (pt.kind != Tokens.Identifier) {
				qualident = String.Empty;
				return false;
			}
			qualidentBuilder.Append('.');
			qualidentBuilder.Append(pt.val);
			pt = Peek();
		}
		qualident = qualidentBuilder.ToString();
		return true;
	}
	qualident = String.Empty;
	return false;
}

/* Skips generic type extensions */
/* !!! Proceeds from current peek position !!! */

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

/* True, if "[" is followed by "," or "]" */
/* or if the current token is "*"         */
bool TimesOrLBrackAndCommaOrRBrack () {
	return la.kind == Tokens.Times || LBrackAndCommaOrRBrack();
}
bool IsPointerOrDims () { return TimesOrLBrackAndCommaOrRBrack(); }
bool IsPointer () { return la.kind == Tokens.Times; }


bool SkipGeneric(ref Token pt)
{
	if (pt.kind == Tokens.LessThan) {
		int braces = 1;
		while (braces != 0) {
			pt = Peek();
			if (pt.kind == Tokens.GreaterThan) {
				--braces;
			} else if (pt.kind == Tokens.LessThan) {
				++braces;
			} else if (pt.kind == Tokens.Semicolon || pt.kind == Tokens.OpenCurlyBrace || pt.kind == Tokens.CloseCurlyBrace || pt.kind == Tokens.EOF) {
				return false;
			}
		}
		pt = Peek();
	}
	return true;
}
bool SkipQuestionMark(ref Token pt)
{
	if (pt.kind == Tokens.Question) {
		pt = Peek();
	}
	return true;
}

/* True, if lookahead is a primitive type keyword, or */
/* if it is a type declaration followed by an ident   */
bool IsLocalVarDecl () {
	if (IsYieldStatement()) {
		return false;
	}
	if ((Tokens.TypeKW[la.kind] && Peek(1).kind != Tokens.Dot) || la.kind == Tokens.Void) {
		return true;
	}
	
	StartPeek();
	Token pt = la ;
	string ignore;
	if (!IsQualident(ref pt, out ignore)) return false;
	if (!SkipGeneric(ref pt)) return false;
	while (pt.kind == Tokens.Dot) {
		pt = Peek();
		if (!IsQualident(ref pt, out ignore)) return false;
		if (!SkipGeneric(ref pt)) return false;
	}
	return SkipQuestionMark(ref pt) && IsPointerOrDims(ref pt) && pt.kind == Tokens.Identifier;
}

/* True if lookahead is type parameters (<...>) followed by the specified token */
bool IsGenericFollowedBy(int token)
{
	Token t = la;
	if (t.kind != Tokens.LessThan) return false;
	StartPeek();
	return SkipGeneric(ref t) && t.kind == token;
}

bool IsExplicitInterfaceImplementation()
{
	StartPeek();
	Token pt = la;
	pt = Peek();
	if (pt.kind == Tokens.Dot || pt.kind == Tokens.DoubleColon)
		return true;
	if (pt.kind == Tokens.LessThan) {
		if (SkipGeneric(ref pt))
			return pt.kind == Tokens.Dot;
	}
	return false;
}

/* True, if lookahead ident is "where" */
bool IdentIsWhere () {
	return la.kind == Tokens.Identifier && la.val == "where";
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

bool IsNotYieldStatement () {
	return !IsYieldStatement();
}
/* True, if lookahead ident is "yield" and than follows a break or return */
bool IsYieldStatement () {
	return la.kind == Tokens.Identifier && la.val == "yield" && (Peek(1).kind == Tokens.Return || Peek(1).kind == Tokens.Break);
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

bool IsShiftRight() 
{
	Token next = Peek(1);
	// TODO : Add col test (seems not to work, lexer bug...) :  && la.col == next.col - 1
	return (la.kind == Tokens.GreaterThan && next.kind == Tokens.GreaterThan);
}

bool IsTypeReferenceExpression(Expression expr)
{
	if (expr is TypeReferenceExpression) return ((TypeReferenceExpression)expr).TypeReference.GenericTypes.Count == 0;
	while (expr is FieldReferenceExpression) {
		expr = ((FieldReferenceExpression)expr).TargetObject;
		if (expr is TypeReferenceExpression) return true;
	}
	return expr is IdentifierExpression;
}

TypeReferenceExpression GetTypeReferenceExpression(Expression expr, List<TypeReference> genericTypes)
{
	TypeReferenceExpression	tre = expr as TypeReferenceExpression;
	if (tre != null) {
		return new TypeReferenceExpression(new TypeReference(tre.TypeReference.Type, tre.TypeReference.PointerNestingLevel, tre.TypeReference.RankSpecifier, genericTypes));
	}
	StringBuilder b = new StringBuilder();
	if (!WriteFullTypeName(b, expr)) {
		// there is some TypeReferenceExpression hidden in the expression
		while (expr is FieldReferenceExpression) {
			expr = ((FieldReferenceExpression)expr).TargetObject;
		}
		tre = expr as TypeReferenceExpression;
		if (tre != null) {
			TypeReference typeRef = tre.TypeReference;
			if (typeRef.GenericTypes.Count == 0) {
				typeRef = typeRef.Clone();
				typeRef.Type += "." + b.ToString();
				typeRef.GenericTypes.AddRange(genericTypes);
			} else {
				typeRef = new InnerClassTypeReference(typeRef, b.ToString(), genericTypes);
			}
			return new TypeReferenceExpression(typeRef);
		}
	}
	return new TypeReferenceExpression(new TypeReference(b.ToString(), 0, null, genericTypes));
}

/* Writes the type name represented through the expression into the string builder. */
/* Returns true when the expression was converted successfully, returns false when */
/* There was an unknown expression (e.g. TypeReferenceExpression) in it */
bool WriteFullTypeName(StringBuilder b, Expression expr)
{
	FieldReferenceExpression fre = expr as FieldReferenceExpression;
	if (fre != null) {
		bool result = WriteFullTypeName(b, fre.TargetObject);
		if (b.Length > 0) b.Append('.');
		b.Append(fre.FieldName);
		return result;
	} else if (expr is IdentifierExpression) {
		b.Append(((IdentifierExpression)expr).Identifier);
		return true;
	} else {
		return false;
	}
}

/*------------------------------------------------------------------------*
 *----- LEXER TOKEN LIST  ------------------------------------------------*
 *------------------------------------------------------------------------*/

/* START AUTOGENERATED TOKENS SECTION */


/*

*/

	void CS() {

#line  657 "cs.ATG" 
		lexer.NextToken(); /* get the first token */
		compilationUnit = new CompilationUnit(); 
		while (la.kind == 120) {
			UsingDirective();
		}
		while (
#line  661 "cs.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void UsingDirective() {

#line  668 "cs.ATG" 
		string qualident = null; TypeReference aliasedType = null;
		
		Expect(120);

#line  671 "cs.ATG" 
		Point startPos = t.Location; 
		Qualident(
#line  672 "cs.ATG" 
out qualident);
		if (la.kind == 3) {
			lexer.NextToken();
			NonArrayType(
#line  673 "cs.ATG" 
out aliasedType);
		}
		Expect(11);

#line  675 "cs.ATG" 
		if (qualident != null && qualident.Length > 0) {
		 INode node;
		 if (aliasedType != null) {
		     node = new UsingDeclaration(qualident, aliasedType);
		 } else {
		     node = new UsingDeclaration(qualident);
		 }
		 node.StartLocation = startPos;
		 node.EndLocation   = t.EndLocation;
		 compilationUnit.AddChild(node);
		}
		
	}

	void GlobalAttributeSection() {
		Expect(18);

#line  691 "cs.ATG" 
		Point startPos = t.Location; 
		Expect(1);

#line  692 "cs.ATG" 
		if (t.val != "assembly") Error("global attribute target specifier (\"assembly\") expected");
		string attributeTarget = t.val;
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(9);
		Attribute(
#line  697 "cs.ATG" 
out attribute);

#line  697 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  698 "cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  698 "cs.ATG" 
out attribute);

#line  698 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  700 "cs.ATG" 
		AttributeSection section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  791 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		Modifiers m = new Modifiers();
		string qualident;
		
		if (la.kind == 87) {
			lexer.NextToken();

#line  797 "cs.ATG" 
			Point startPos = t.Location; 
			Qualident(
#line  798 "cs.ATG" 
out qualident);

#line  798 "cs.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			compilationUnit.AddChild(node);
			compilationUnit.BlockStart(node);
			
			Expect(16);
			while (la.kind == 120) {
				UsingDirective();
			}
			while (StartOf(1)) {
				NamespaceMemberDecl();
			}
			Expect(17);
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  807 "cs.ATG" 
			node.EndLocation   = t.EndLocation;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 18) {
				AttributeSection(
#line  811 "cs.ATG" 
out section);

#line  811 "cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  812 "cs.ATG" 
m);
			}
			TypeDecl(
#line  813 "cs.ATG" 
m, attributes);
		} else SynErr(126);
	}

	void Qualident(
#line  935 "cs.ATG" 
out string qualident) {
		Expect(1);

#line  937 "cs.ATG" 
		qualidentBuilder.Length = 0; qualidentBuilder.Append(t.val); 
		while (
#line  938 "cs.ATG" 
DotAndIdent()) {
			Expect(15);
			Expect(1);

#line  938 "cs.ATG" 
			qualidentBuilder.Append('.');
			qualidentBuilder.Append(t.val); 
			
		}

#line  941 "cs.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void NonArrayType(
#line  1050 "cs.ATG" 
out TypeReference type) {

#line  1052 "cs.ATG"
		Point startPos = la.Location;
		string name;
		int pointer = 0;
		type = null;
		
		if (la.kind == 1 || la.kind == 90 || la.kind == 107) {
			ClassType(
#line  1057 "cs.ATG" 
out type, false);
		} else if (StartOf(4)) {
			SimpleType(
#line  1058 "cs.ATG" 
out name);

#line  1058 "cs.ATG" 
			type = new TypeReference(name); 
		} else if (la.kind == 122) {
			lexer.NextToken();
			Expect(6);

#line  1059 "cs.ATG" 
			pointer = 1; type = new TypeReference("void"); 
		} else SynErr(127);
		if (la.kind == 12) {
			NullableQuestionMark(
#line  1062 "cs.ATG" 
ref type);
		}
		while (
#line  1064 "cs.ATG" 
IsPointer()) {
			Expect(6);

#line  1065 "cs.ATG" 
			++pointer; 
		}

#line  1067 "cs.ATG" 
		if (type != null) {
			type.PointerNestingLevel = pointer;
			type.EndLocation = t.EndLocation;
			type.StartLocation = startPos;
		} 
	}

	void Attribute(
#line  707 "cs.ATG" 
out ASTAttribute attribute) {

#line  708 "cs.ATG" 
		string qualident;
		string alias = null;
		
		if (
#line  712 "cs.ATG" 
la.kind == Tokens.Identifier && Peek(1).kind == Tokens.DoubleColon) {
			lexer.NextToken();

#line  713 "cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  716 "cs.ATG" 
out qualident);

#line  717 "cs.ATG" 
		List<Expression> positional = new List<Expression>();
		List<NamedArgumentExpression> named = new List<NamedArgumentExpression>();
		string name = (alias != null && alias != "global") ? alias + "." + qualident : qualident;
		
		if (la.kind == 20) {
			AttributeArguments(
#line  721 "cs.ATG" 
positional, named);
		}

#line  721 "cs.ATG" 
		attribute  = new ICSharpCode.NRefactory.Parser.AST.Attribute(name, positional, named);
	}

	void AttributeArguments(
#line  724 "cs.ATG" 
List<Expression> positional, List<NamedArgumentExpression> named) {

#line  726 "cs.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(20);
		if (StartOf(5)) {
			if (
#line  734 "cs.ATG" 
IsAssignment()) {

#line  734 "cs.ATG" 
				nameFound = true; 
				lexer.NextToken();

#line  735 "cs.ATG" 
				name = t.val; 
				Expect(3);
			}
			Expr(
#line  737 "cs.ATG" 
out expr);

#line  737 "cs.ATG" 
			if (expr != null) {if(name == "") positional.Add(expr);
			else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			}
			
			while (la.kind == 14) {
				lexer.NextToken();
				if (
#line  745 "cs.ATG" 
IsAssignment()) {

#line  745 "cs.ATG" 
					nameFound = true; 
					Expect(1);

#line  746 "cs.ATG" 
					name = t.val; 
					Expect(3);
				} else if (StartOf(5)) {

#line  748 "cs.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(128);
				Expr(
#line  749 "cs.ATG" 
out expr);

#line  749 "cs.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				}
				
			}
		}
		Expect(21);
	}

	void Expr(
#line  2086 "cs.ATG" 
out Expression expr) {

#line  2087 "cs.ATG" 
		expr = null; Expression expr1 = null, expr2 = null; AssignmentOperatorType op; 
		UnaryExpr(
#line  2089 "cs.ATG" 
out expr);
		if (StartOf(6)) {
			AssignmentOperator(
#line  2092 "cs.ATG" 
out op);
			Expr(
#line  2092 "cs.ATG" 
out expr1);

#line  2092 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (
#line  2093 "cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			AssignmentOperator(
#line  2094 "cs.ATG" 
out op);
			Expr(
#line  2094 "cs.ATG" 
out expr1);

#line  2094 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (StartOf(7)) {
			ConditionalOrExpr(
#line  2096 "cs.ATG" 
ref expr);
			if (la.kind == 13) {
				lexer.NextToken();
				Expr(
#line  2097 "cs.ATG" 
out expr1);

#line  2097 "cs.ATG" 
				expr = new BinaryOperatorExpression(expr, BinaryOperatorType.NullCoalescing, expr1); 
			}
			if (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  2098 "cs.ATG" 
out expr1);
				Expect(9);
				Expr(
#line  2098 "cs.ATG" 
out expr2);

#line  2098 "cs.ATG" 
				expr = new ConditionalExpression(expr, expr1, expr2);  
			}
		} else SynErr(129);
	}

	void AttributeSection(
#line  758 "cs.ATG" 
out AttributeSection section) {

#line  760 "cs.ATG" 
		string attributeTarget = "";
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		
		Expect(18);

#line  766 "cs.ATG" 
		Point startPos = t.Location; 
		if (
#line  767 "cs.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 68) {
				lexer.NextToken();

#line  768 "cs.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 100) {
				lexer.NextToken();

#line  769 "cs.ATG" 
				attributeTarget = "return";
			} else {
				lexer.NextToken();

#line  770 "cs.ATG" 
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
#line  780 "cs.ATG" 
out attribute);

#line  780 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  781 "cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  781 "cs.ATG" 
out attribute);

#line  781 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  783 "cs.ATG" 
		section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		
	}

	void TypeModifier(
#line  1137 "cs.ATG" 
Modifiers m) {
		switch (la.kind) {
		case 88: {
			lexer.NextToken();

#line  1139 "cs.ATG" 
			m.Add(Modifier.New, t.Location); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  1140 "cs.ATG" 
			m.Add(Modifier.Public, t.Location); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  1141 "cs.ATG" 
			m.Add(Modifier.Protected, t.Location); 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  1142 "cs.ATG" 
			m.Add(Modifier.Internal, t.Location); 
			break;
		}
		case 95: {
			lexer.NextToken();

#line  1143 "cs.ATG" 
			m.Add(Modifier.Private, t.Location); 
			break;
		}
		case 118: {
			lexer.NextToken();

#line  1144 "cs.ATG" 
			m.Add(Modifier.Unsafe, t.Location); 
			break;
		}
		case 48: {
			lexer.NextToken();

#line  1145 "cs.ATG" 
			m.Add(Modifier.Abstract, t.Location); 
			break;
		}
		case 102: {
			lexer.NextToken();

#line  1146 "cs.ATG" 
			m.Add(Modifier.Sealed, t.Location); 
			break;
		}
		case 106: {
			lexer.NextToken();

#line  1147 "cs.ATG" 
			m.Add(Modifier.Static, t.Location); 
			break;
		}
		case 1: {
			lexer.NextToken();

#line  1148 "cs.ATG" 
			if (t.val == "partial") { m.Add(Modifier.Partial, t.Location); } else { Error("Unexpected identifier"); } 
			break;
		}
		default: SynErr(130); break;
		}
	}

	void TypeDecl(
#line  816 "cs.ATG" 
Modifiers m, List<AttributeSection> attributes) {

#line  818 "cs.ATG" 
		TypeReference type;
		List<TypeReference> names;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		string name;
		List<TemplateDefinition> templates;
		
		if (la.kind == 58) {

#line  824 "cs.ATG" 
			m.Check(Modifier.Classes); 
			lexer.NextToken();

#line  825 "cs.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			templates = newType.Templates;
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			
			newType.Type = Types.Class;
			
			Expect(1);

#line  833 "cs.ATG" 
			newType.Name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  836 "cs.ATG" 
templates);
			}
			if (la.kind == 9) {
				ClassBase(
#line  838 "cs.ATG" 
out names);

#line  838 "cs.ATG" 
				newType.BaseTypes = names; 
			}
			while (
#line  841 "cs.ATG" 
IdentIsWhere()) {
				TypeParameterConstraintsClause(
#line  841 "cs.ATG" 
templates);
			}

#line  843 "cs.ATG" 
			newType.BodyStartLocation = t.EndLocation; 
			ClassBody();
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  845 "cs.ATG" 
			newType.EndLocation = t.Location; 
			compilationUnit.BlockEnd();
			
		} else if (StartOf(8)) {

#line  848 "cs.ATG" 
			m.Check(Modifier.StructsInterfacesEnumsDelegates); 
			if (la.kind == 108) {
				lexer.NextToken();

#line  849 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.Type = Types.Struct; 
				
				Expect(1);

#line  856 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  859 "cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					StructInterfaces(
#line  861 "cs.ATG" 
out names);

#line  861 "cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (
#line  864 "cs.ATG" 
IdentIsWhere()) {
					TypeParameterConstraintsClause(
#line  864 "cs.ATG" 
templates);
				}

#line  867 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				StructBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  869 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 82) {
				lexer.NextToken();

#line  873 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Interface;
				
				Expect(1);

#line  880 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  883 "cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					InterfaceBase(
#line  885 "cs.ATG" 
out names);

#line  885 "cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (
#line  888 "cs.ATG" 
IdentIsWhere()) {
					TypeParameterConstraintsClause(
#line  888 "cs.ATG" 
templates);
				}

#line  890 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				InterfaceBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  892 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 67) {
				lexer.NextToken();

#line  896 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Enum;
				
				Expect(1);

#line  902 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					lexer.NextToken();
					IntegralType(
#line  903 "cs.ATG" 
out name);

#line  903 "cs.ATG" 
					newType.BaseTypes.Add(new TypeReference(name)); 
				}

#line  905 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				EnumBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  907 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else {
				lexer.NextToken();

#line  911 "cs.ATG" 
				DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
				templates = delegateDeclr.Templates;
				delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
				
				if (
#line  915 "cs.ATG" 
NotVoidPointer()) {
					Expect(122);

#line  915 "cs.ATG" 
					delegateDeclr.ReturnType = new TypeReference("void", 0, null); 
				} else if (StartOf(9)) {
					Type(
#line  916 "cs.ATG" 
out type);

#line  916 "cs.ATG" 
					delegateDeclr.ReturnType = type; 
				} else SynErr(131);
				Expect(1);

#line  918 "cs.ATG" 
				delegateDeclr.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  921 "cs.ATG" 
templates);
				}
				Expect(20);
				if (StartOf(10)) {
					FormalParameterList(
#line  923 "cs.ATG" 
p);

#line  923 "cs.ATG" 
					delegateDeclr.Parameters = p; 
				}
				Expect(21);
				while (
#line  927 "cs.ATG" 
IdentIsWhere()) {
					TypeParameterConstraintsClause(
#line  927 "cs.ATG" 
templates);
				}
				Expect(11);

#line  929 "cs.ATG" 
				delegateDeclr.EndLocation = t.Location;
				compilationUnit.AddChild(delegateDeclr);
				
			}
		} else SynErr(132);
	}

	void TypeParameterList(
#line  2489 "cs.ATG" 
List<TemplateDefinition> templates) {

#line  2491 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		Expect(23);
		while (la.kind == 18) {
			AttributeSection(
#line  2495 "cs.ATG" 
out section);

#line  2495 "cs.ATG" 
			attributes.Add(section); 
		}
		Expect(1);

#line  2496 "cs.ATG" 
		templates.Add(new TemplateDefinition(t.val, attributes)); 
		while (la.kind == 14) {
			lexer.NextToken();
			while (la.kind == 18) {
				AttributeSection(
#line  2497 "cs.ATG" 
out section);

#line  2497 "cs.ATG" 
				attributes.Add(section); 
			}
			Expect(1);

#line  2498 "cs.ATG" 
			templates.Add(new TemplateDefinition(t.val, attributes)); 
		}
		Expect(22);
	}

	void ClassBase(
#line  944 "cs.ATG" 
out List<TypeReference> names) {

#line  946 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		ClassType(
#line  950 "cs.ATG" 
out typeRef, false);

#line  950 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  951 "cs.ATG" 
out typeRef, false);

#line  951 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void TypeParameterConstraintsClause(
#line  2502 "cs.ATG" 
List<TemplateDefinition> templates) {

#line  2503 "cs.ATG" 
		string name = ""; TypeReference type; 
		Expect(1);

#line  2505 "cs.ATG" 
		if (t.val != "where") Error("where expected"); 
		Expect(1);

#line  2506 "cs.ATG" 
		name = t.val; 
		Expect(9);
		TypeParameterConstraintsClauseBase(
#line  2508 "cs.ATG" 
out type);

#line  2509 "cs.ATG" 
		TemplateDefinition td = null;
		foreach (TemplateDefinition d in templates) {
			if (d.Name == name) {
				td = d;
				break;
			}
		}
		if ( td != null) { td.Bases.Add(type); }
		
		while (la.kind == 14) {
			lexer.NextToken();
			TypeParameterConstraintsClauseBase(
#line  2518 "cs.ATG" 
out type);

#line  2519 "cs.ATG" 
			td = null;
			foreach (TemplateDefinition d in templates) {
				if (d.Name == name) {
					td = d;
					break;
				}
			}
			if ( td != null) { td.Bases.Add(type); }
			
		}
	}

	void ClassBody() {

#line  955 "cs.ATG" 
		AttributeSection section; 
		Expect(16);
		while (StartOf(11)) {

#line  958 "cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			Modifiers m = new Modifiers();
			
			while (la.kind == 18) {
				AttributeSection(
#line  961 "cs.ATG" 
out section);

#line  961 "cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  962 "cs.ATG" 
m);
			ClassMemberDecl(
#line  963 "cs.ATG" 
m, attributes);
		}
		Expect(17);
	}

	void StructInterfaces(
#line  968 "cs.ATG" 
out List<TypeReference> names) {

#line  970 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  974 "cs.ATG" 
out typeRef, false);

#line  974 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  975 "cs.ATG" 
out typeRef, false);

#line  975 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void StructBody() {

#line  979 "cs.ATG" 
		AttributeSection section; 
		Expect(16);
		while (StartOf(12)) {

#line  982 "cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			Modifiers m = new Modifiers();
			
			while (la.kind == 18) {
				AttributeSection(
#line  985 "cs.ATG" 
out section);

#line  985 "cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  986 "cs.ATG" 
m);
			StructMemberDecl(
#line  987 "cs.ATG" 
m, attributes);
		}
		Expect(17);
	}

	void InterfaceBase(
#line  992 "cs.ATG" 
out List<TypeReference> names) {

#line  994 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  998 "cs.ATG" 
out typeRef, false);

#line  998 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  999 "cs.ATG" 
out typeRef, false);

#line  999 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void InterfaceBody() {
		Expect(16);
		while (StartOf(13)) {
			InterfaceMemberDecl();
		}
		Expect(17);
	}

	void IntegralType(
#line  1159 "cs.ATG" 
out string name) {

#line  1159 "cs.ATG" 
		name = ""; 
		switch (la.kind) {
		case 101: {
			lexer.NextToken();

#line  1161 "cs.ATG" 
			name = "sbyte"; 
			break;
		}
		case 53: {
			lexer.NextToken();

#line  1162 "cs.ATG" 
			name = "byte"; 
			break;
		}
		case 103: {
			lexer.NextToken();

#line  1163 "cs.ATG" 
			name = "short"; 
			break;
		}
		case 119: {
			lexer.NextToken();

#line  1164 "cs.ATG" 
			name = "ushort"; 
			break;
		}
		case 81: {
			lexer.NextToken();

#line  1165 "cs.ATG" 
			name = "int"; 
			break;
		}
		case 115: {
			lexer.NextToken();

#line  1166 "cs.ATG" 
			name = "uint"; 
			break;
		}
		case 86: {
			lexer.NextToken();

#line  1167 "cs.ATG" 
			name = "long"; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  1168 "cs.ATG" 
			name = "ulong"; 
			break;
		}
		case 56: {
			lexer.NextToken();

#line  1169 "cs.ATG" 
			name = "char"; 
			break;
		}
		default: SynErr(133); break;
		}
	}

	void EnumBody() {

#line  1008 "cs.ATG" 
		FieldDeclaration f; 
		Expect(16);
		if (la.kind == 1 || la.kind == 18) {
			EnumMemberDecl(
#line  1011 "cs.ATG" 
out f);

#line  1011 "cs.ATG" 
			compilationUnit.AddChild(f); 
			while (
#line  1012 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				EnumMemberDecl(
#line  1013 "cs.ATG" 
out f);

#line  1013 "cs.ATG" 
				compilationUnit.AddChild(f); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);
	}

	void Type(
#line  1018 "cs.ATG" 
out TypeReference type) {
		TypeWithRestriction(
#line  1020 "cs.ATG" 
out type, true, false);
	}

	void FormalParameterList(
#line  1081 "cs.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  1084 "cs.ATG" 
		ParameterDeclarationExpression p;
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		while (la.kind == 18) {
			AttributeSection(
#line  1089 "cs.ATG" 
out section);

#line  1089 "cs.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(14)) {
			FixedParameter(
#line  1091 "cs.ATG" 
out p);

#line  1091 "cs.ATG" 
			bool paramsFound = false;
			p.Attributes = attributes;
			parameter.Add(p);
			
			while (la.kind == 14) {
				lexer.NextToken();

#line  1096 "cs.ATG" 
				attributes = new List<AttributeSection>(); if (paramsFound) Error("params array must be at end of parameter list"); 
				while (la.kind == 18) {
					AttributeSection(
#line  1097 "cs.ATG" 
out section);

#line  1097 "cs.ATG" 
					attributes.Add(section); 
				}
				if (StartOf(14)) {
					FixedParameter(
#line  1099 "cs.ATG" 
out p);

#line  1099 "cs.ATG" 
					p.Attributes = attributes; parameter.Add(p); 
				} else if (la.kind == 94) {
					ParameterArray(
#line  1100 "cs.ATG" 
out p);

#line  1100 "cs.ATG" 
					paramsFound = true; p.Attributes = attributes; parameter.Add(p); 
				} else SynErr(134);
			}
		} else if (la.kind == 94) {
			ParameterArray(
#line  1103 "cs.ATG" 
out p);

#line  1103 "cs.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		} else SynErr(135);
	}

	void ClassType(
#line  1151 "cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  1152 "cs.ATG" 
		TypeReference r; typeRef = null; 
		if (la.kind == 1) {
			TypeName(
#line  1154 "cs.ATG" 
out r, canBeUnbound);

#line  1154 "cs.ATG" 
			typeRef = r; 
		} else if (la.kind == 90) {
			lexer.NextToken();

#line  1155 "cs.ATG" 
			typeRef = new TypeReference("object"); 
		} else if (la.kind == 107) {
			lexer.NextToken();

#line  1156 "cs.ATG" 
			typeRef = new TypeReference("string"); 
		} else SynErr(136);
	}

	void TypeName(
#line  2432 "cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  2433 "cs.ATG" 
		List<TypeReference> typeArguments = null;
		string alias = null;
		string qualident;
		
		if (
#line  2438 "cs.ATG" 
la.kind == Tokens.Identifier && Peek(1).kind == Tokens.DoubleColon) {
			lexer.NextToken();

#line  2439 "cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  2442 "cs.ATG" 
out qualident);
		if (la.kind == 23) {
			TypeArgumentList(
#line  2443 "cs.ATG" 
out typeArguments, canBeUnbound);
		}

#line  2445 "cs.ATG" 
		if (alias == null) {
		typeRef = new TypeReference(qualident, typeArguments);
		} else if (alias == "global") {
			typeRef = new TypeReference(qualident, typeArguments);
			typeRef.IsGlobal = true;
		} else {
			typeRef = new TypeReference(alias + "." + qualident, typeArguments);
		}
		
		while (
#line  2454 "cs.ATG" 
DotAndIdent()) {
			Expect(15);

#line  2455 "cs.ATG" 
			typeArguments = null; 
			Qualident(
#line  2456 "cs.ATG" 
out qualident);
			if (la.kind == 23) {
				TypeArgumentList(
#line  2457 "cs.ATG" 
out typeArguments, canBeUnbound);
			}

#line  2458 "cs.ATG" 
			typeRef = new InnerClassTypeReference(typeRef, qualident, typeArguments); 
		}
	}

	void MemberModifiers(
#line  1172 "cs.ATG" 
Modifiers m) {
		while (StartOf(15) || 
#line  1189 "cs.ATG" 
la.kind == Tokens.Identifier && la.val == "partial") {
			if (la.kind == 48) {
				lexer.NextToken();

#line  1175 "cs.ATG" 
				m.Add(Modifier.Abstract, t.Location); 
			} else if (la.kind == 70) {
				lexer.NextToken();

#line  1176 "cs.ATG" 
				m.Add(Modifier.Extern, t.Location); 
			} else if (la.kind == 83) {
				lexer.NextToken();

#line  1177 "cs.ATG" 
				m.Add(Modifier.Internal, t.Location); 
			} else if (la.kind == 88) {
				lexer.NextToken();

#line  1178 "cs.ATG" 
				m.Add(Modifier.New, t.Location); 
			} else if (la.kind == 93) {
				lexer.NextToken();

#line  1179 "cs.ATG" 
				m.Add(Modifier.Override, t.Location); 
			} else if (la.kind == 95) {
				lexer.NextToken();

#line  1180 "cs.ATG" 
				m.Add(Modifier.Private, t.Location); 
			} else if (la.kind == 96) {
				lexer.NextToken();

#line  1181 "cs.ATG" 
				m.Add(Modifier.Protected, t.Location); 
			} else if (la.kind == 97) {
				lexer.NextToken();

#line  1182 "cs.ATG" 
				m.Add(Modifier.Public, t.Location); 
			} else if (la.kind == 98) {
				lexer.NextToken();

#line  1183 "cs.ATG" 
				m.Add(Modifier.ReadOnly, t.Location); 
			} else if (la.kind == 102) {
				lexer.NextToken();

#line  1184 "cs.ATG" 
				m.Add(Modifier.Sealed, t.Location); 
			} else if (la.kind == 106) {
				lexer.NextToken();

#line  1185 "cs.ATG" 
				m.Add(Modifier.Static, t.Location); 
			} else if (la.kind == 118) {
				lexer.NextToken();

#line  1186 "cs.ATG" 
				m.Add(Modifier.Unsafe, t.Location); 
			} else if (la.kind == 121) {
				lexer.NextToken();

#line  1187 "cs.ATG" 
				m.Add(Modifier.Virtual, t.Location); 
			} else if (la.kind == 123) {
				lexer.NextToken();

#line  1188 "cs.ATG" 
				m.Add(Modifier.Volatile, t.Location); 
			} else {
				Expect(1);

#line  1190 "cs.ATG" 
				m.Add(Modifier.Partial, t.Location); 
			}
		}
	}

	void ClassMemberDecl(
#line  1465 "cs.ATG" 
Modifiers m, List<AttributeSection> attributes) {

#line  1466 "cs.ATG" 
		Statement stmt = null; 
		if (StartOf(16)) {
			StructMemberDecl(
#line  1468 "cs.ATG" 
m, attributes);
		} else if (la.kind == 27) {

#line  1469 "cs.ATG" 
			m.Check(Modifier.Destructors); Point startPos = t.Location; 
			lexer.NextToken();
			Expect(1);

#line  1470 "cs.ATG" 
			DestructorDeclaration d = new DestructorDeclaration(t.val, m.Modifier, attributes); 
			d.Modifier = m.Modifier;
			d.StartLocation = m.GetDeclarationLocation(startPos);
			
			Expect(20);
			Expect(21);

#line  1474 "cs.ATG" 
			d.EndLocation = t.EndLocation; 
			if (la.kind == 16) {
				Block(
#line  1474 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(137);

#line  1475 "cs.ATG" 
			d.Body = (BlockStatement)stmt;
			compilationUnit.AddChild(d);
			
		} else SynErr(138);
	}

	void StructMemberDecl(
#line  1195 "cs.ATG" 
Modifiers m, List<AttributeSection> attributes) {

#line  1197 "cs.ATG" 
		string qualident = null;
		TypeReference type;
		Expression expr;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<VariableDeclaration> variableDeclarators = new List<VariableDeclaration>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		TypeReference explicitInterface = null;
		
		if (la.kind == 59) {

#line  1207 "cs.ATG" 
			m.Check(Modifier.Constants); 
			lexer.NextToken();

#line  1208 "cs.ATG" 
			Point startPos = t.Location; 
			Type(
#line  1209 "cs.ATG" 
out type);
			Expect(1);

#line  1209 "cs.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier | Modifier.Const);
			fd.StartLocation = m.GetDeclarationLocation(startPos);
			VariableDeclaration var = new VariableDeclaration (t.val);
			var.StartLocation = t.Location;
			var.TypeReference = type;
			fd.Fields.Add (var);
			
			Expect(3);
			Expr(
#line  1214 "cs.ATG" 
out expr);

#line  1214 "cs.ATG"
			var.EndLocation = t.EndLocation;
			var.Initializer = expr;
			while (la.kind == 14) {
				var.EndLocation = t.Location;
				lexer.NextToken();
				Expect(1);

#line  1215 "cs.ATG"
				var = new VariableDeclaration (t.val);
				var.StartLocation = t.Location;
				var.TypeReference = type;
				fd.Fields.Add (var);
				
				Expect(3);
				Expr(
#line  1218 "cs.ATG" 
out expr);

#line  1218 "cs.ATG"
				var.EndLocation = t.EndLocation;
				var.Initializer = expr;
			}
			Expect(11);

#line  1219 "cs.ATG"
			fd.EndLocation = t.EndLocation;
			compilationUnit.AddChild(fd); 
		} else if (
#line  1223 "cs.ATG" 
NotVoidPointer()) {

#line  1223 "cs.ATG" 
			m.Check(Modifier.PropertysEventsMethods); 
			Expect(122);

#line  1224 "cs.ATG" 
			Point startPos = t.Location; 
			if (
#line  1225 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  1226 "cs.ATG" 
out explicitInterface, false);

#line  1227 "cs.ATG" 
				if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
				 } 
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  1230 "cs.ATG" 
				qualident = t.val; 
			} else SynErr(139);
			if (la.kind == 23) {
				TypeParameterList(
#line  1233 "cs.ATG" 
templates);
			}
			Expect(20);
			if (StartOf(10)) {
				FormalParameterList(
#line  1236 "cs.ATG" 
p);
			}
			Expect(21);

#line  1237 "cs.ATG" 
			MethodDeclaration methodDeclaration = new MethodDeclaration(qualident,
			                                                         m.Modifier,
			                                                         new TypeReference("void"),
			                                                         p,
			                                                         attributes);
			methodDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
			methodDeclaration.EndLocation   = t.EndLocation;
			methodDeclaration.Templates = templates;
			if (explicitInterface != null)
			methodDeclaration.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
			compilationUnit.AddChild(methodDeclaration);
			compilationUnit.BlockStart(methodDeclaration);
			
			while (
#line  1252 "cs.ATG" 
IdentIsWhere()) {
				TypeParameterConstraintsClause(
#line  1252 "cs.ATG" 
templates);
			}
			if (la.kind == 16) {
				Block(
#line  1254 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(140);

#line  1254 "cs.ATG" 
			compilationUnit.BlockEnd();
			methodDeclaration.Body  = (BlockStatement)stmt;
			
		} else if (la.kind == 68) {

#line  1258 "cs.ATG" 
			m.Check(Modifier.PropertysEventsMethods); 
			lexer.NextToken();

#line  1259 "cs.ATG" 
			EventDeclaration eventDecl = new EventDeclaration(null, null, m.Modifier, attributes, null);
			eventDecl.StartLocation = t.Location;
			compilationUnit.AddChild(eventDecl);
			compilationUnit.BlockStart(eventDecl);
			EventAddRegion addBlock = null;
			EventRemoveRegion removeBlock = null;
			
			Type(
#line  1266 "cs.ATG" 
out type);

#line  1266 "cs.ATG" 
			eventDecl.TypeReference = type; 
			if (
#line  1267 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  1268 "cs.ATG" 
out explicitInterface, false);

#line  1269 "cs.ATG" 
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface); 

#line  1270 "cs.ATG" 
				eventDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident)); 
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  1272 "cs.ATG" 
				qualident = t.val; 
			} else SynErr(141);

#line  1274 "cs.ATG" 
			eventDecl.Name = qualident; eventDecl.EndLocation = t.EndLocation; 
			if (la.kind == 16) {
				lexer.NextToken();

#line  1275 "cs.ATG" 
				eventDecl.BodyStart = t.Location; 
				EventAccessorDecls(
#line  1276 "cs.ATG" 
out addBlock, out removeBlock);
				Expect(17);

#line  1277 "cs.ATG" 
				eventDecl.BodyEnd   = t.EndLocation; 
			}
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  1280 "cs.ATG" 
			compilationUnit.BlockEnd();
			eventDecl.AddRegion = addBlock;
			eventDecl.RemoveRegion = removeBlock;
			
		} else if (
#line  1286 "cs.ATG" 
IdentAndLPar()) {

#line  1286 "cs.ATG" 
			m.Check(Modifier.Constructors | Modifier.StaticConstructors); 
			Expect(1);

#line  1287 "cs.ATG" 
			string name = t.val; Point startPos = t.Location; 
			Expect(20);
			if (StartOf(10)) {

#line  1287 "cs.ATG" 
				m.Check(Modifier.Constructors); 
				FormalParameterList(
#line  1288 "cs.ATG" 
p);
			}
			Expect(21);

#line  1290 "cs.ATG" 
			ConstructorInitializer init = null;  
			if (la.kind == 9) {

#line  1291 "cs.ATG" 
				m.Check(Modifier.Constructors); 
				ConstructorInitializer(
#line  1292 "cs.ATG" 
out init);
			}

#line  1294 "cs.ATG" 
			ConstructorDeclaration cd = new ConstructorDeclaration(name, m.Modifier, p, init, attributes); 
			cd.StartLocation = startPos;
			cd.EndLocation   = t.EndLocation;
			
			if (la.kind == 16) {
				Block(
#line  1299 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(142);

#line  1299 "cs.ATG" 
			cd.Body = (BlockStatement)stmt; compilationUnit.AddChild(cd); 
		} else if (la.kind == 69 || la.kind == 79) {

#line  1302 "cs.ATG" 
			m.Check(Modifier.Operators);
			if (m.isNone) Error("at least one modifier must be set"); 
			bool isImplicit = true;
			Point startPos = Point.Empty;
			
			if (la.kind == 79) {
				lexer.NextToken();

#line  1307 "cs.ATG" 
				startPos = t.Location; 
			} else {
				lexer.NextToken();

#line  1307 "cs.ATG" 
				isImplicit = false; startPos = t.Location; 
			}
			Expect(91);
			Type(
#line  1308 "cs.ATG" 
out type);

#line  1308 "cs.ATG" 
			TypeReference operatorType = type; 
			Expect(20);
			Type(
#line  1309 "cs.ATG" 
out type);
			Expect(1);

#line  1309 "cs.ATG" 
			string varName = t.val; 
			Expect(21);

#line  1310 "cs.ATG" 
			Point endPos = t.Location; 
			if (la.kind == 16) {
				Block(
#line  1311 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();

#line  1311 "cs.ATG" 
				stmt = null; 
			} else SynErr(143);

#line  1314 "cs.ATG" 
			List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
			parameters.Add(new ParameterDeclarationExpression(type, varName));
			OperatorDeclaration operatorDeclaration = new OperatorDeclaration(m.Modifier, 
			                                                                  attributes, 
			                                                                  parameters, 
			                                                                  operatorType,
			                                                                  isImplicit ? ConversionType.Implicit : ConversionType.Explicit
			                                                                  );
			operatorDeclaration.Body = (BlockStatement)stmt;
			operatorDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
			operatorDeclaration.EndLocation = endPos;
			compilationUnit.AddChild(operatorDeclaration);
			
		} else if (StartOf(17)) {
			TypeDecl(
#line  1330 "cs.ATG" 
m, attributes);
		} else if (StartOf(9)) {
			Type(
#line  1332 "cs.ATG" 
out type);

#line  1332 "cs.ATG" 
			Point startPos = t.Location;  
			if (la.kind == 91) {

#line  1334 "cs.ATG" 
				OverloadableOperatorType op;
				m.Check(Modifier.Operators);
				if (m.isNone) Error("at least one modifier must be set");
				
				lexer.NextToken();
				OverloadableOperator(
#line  1338 "cs.ATG" 
out op);

#line  1338 "cs.ATG" 
				TypeReference firstType, secondType = null; string secondName = null; 
				Expect(20);
				Type(
#line  1339 "cs.ATG" 
out firstType);
				Expect(1);

#line  1339 "cs.ATG" 
				string firstName = t.val; 
				if (la.kind == 14) {
					lexer.NextToken();
					Type(
#line  1340 "cs.ATG" 
out secondType);
					Expect(1);

#line  1340 "cs.ATG" 
					secondName = t.val; 
				} else if (la.kind == 21) {
				} else SynErr(144);

#line  1348 "cs.ATG" 
				Point endPos = t.Location; 
				Expect(21);
				if (la.kind == 16) {
					Block(
#line  1349 "cs.ATG" 
out stmt);
				} else if (la.kind == 11) {
					lexer.NextToken();
				} else SynErr(145);

#line  1351 "cs.ATG" 
				List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
				parameters.Add(new ParameterDeclarationExpression(firstType, firstName));
				if (secondType != null) {
					parameters.Add(new ParameterDeclarationExpression(secondType, secondName));
				}
				OperatorDeclaration operatorDeclaration = new OperatorDeclaration(m.Modifier,
				                                                                  attributes,
				                                                                  parameters,
				                                                                  type,
				                                                                  op);
				operatorDeclaration.Body = (BlockStatement)stmt;
				operatorDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				operatorDeclaration.EndLocation = endPos;
				compilationUnit.AddChild(operatorDeclaration);
				
			} else if (
#line  1368 "cs.ATG" 
IsVarDecl()) {

#line  1368 "cs.ATG" 
				m.Check(Modifier.Fields); 
				FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
				fd.StartLocation = m.GetDeclarationLocation(startPos); 
				
				VariableDeclarator(
#line  1372 "cs.ATG" 
variableDeclarators);
				while (la.kind == 14) {
					lexer.NextToken();
					VariableDeclarator(
#line  1373 "cs.ATG" 
variableDeclarators);
				}
				Expect(11);

#line  1374 "cs.ATG" 
				fd.EndLocation = t.EndLocation; fd.Fields = variableDeclarators; compilationUnit.AddChild(fd); 
			} else if (la.kind == 110) {

#line  1377 "cs.ATG" 
				m.Check(Modifier.Indexers); 
				lexer.NextToken();
				Expect(18);
				FormalParameterList(
#line  1378 "cs.ATG" 
p);
				Expect(19);

#line  1378 "cs.ATG" 
				Point endLocation = t.EndLocation; 
				Expect(16);

#line  1379 "cs.ATG" 
				IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
				indexer.StartLocation = startPos;
				indexer.EndLocation   = endLocation;
				indexer.BodyStart     = t.Location;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  1386 "cs.ATG" 
out getRegion, out setRegion);
				Expect(17);

#line  1387 "cs.ATG" 
				indexer.BodyEnd    = t.EndLocation;
				indexer.GetRegion = getRegion;
				indexer.SetRegion = setRegion;
				compilationUnit.AddChild(indexer);
				
			} else if (
#line  1392 "cs.ATG" 
la.kind == Tokens.Identifier) {
				if (
#line  1393 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
					TypeName(
#line  1394 "cs.ATG" 
out explicitInterface, false);

#line  1395 "cs.ATG" 
					if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
					qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
					 } 
				} else if (la.kind == 1) {
					lexer.NextToken();

#line  1398 "cs.ATG" 
					qualident = t.val; 
				} else SynErr(146);

#line  1400 "cs.ATG" 
				Point qualIdentEndLocation = t.EndLocation; 
				if (la.kind == 16 || la.kind == 20 || la.kind == 23) {
					if (la.kind == 20 || la.kind == 23) {

#line  1404 "cs.ATG" 
						m.Check(Modifier.PropertysEventsMethods); 
						if (la.kind == 23) {
							TypeParameterList(
#line  1406 "cs.ATG" 
templates);
						}
						Expect(20);
						if (StartOf(10)) {
							FormalParameterList(
#line  1407 "cs.ATG" 
p);
						}
						Expect(21);

#line  1408 "cs.ATG" 
						MethodDeclaration methodDeclaration = new MethodDeclaration(qualident,
						                                                           m.Modifier, 
						                                                           type, 
						                                                           p, 
						                                                           attributes);
						if (explicitInterface != null)
							methodDeclaration.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
						methodDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
						methodDeclaration.EndLocation   = t.EndLocation;
						methodDeclaration.Templates = templates;
						compilationUnit.AddChild(methodDeclaration);
						                                      
						while (
#line  1420 "cs.ATG" 
IdentIsWhere()) {
							TypeParameterConstraintsClause(
#line  1420 "cs.ATG" 
templates);
						}
						if (la.kind == 16) {
							Block(
#line  1421 "cs.ATG" 
out stmt);
						} else if (la.kind == 11) {
							lexer.NextToken();
						} else SynErr(147);

#line  1421 "cs.ATG" 
						methodDeclaration.Body  = (BlockStatement)stmt; 
					} else {
						lexer.NextToken();

#line  1424 "cs.ATG" 
						PropertyDeclaration pDecl = new PropertyDeclaration(qualident, type, m.Modifier, attributes); 
						if (explicitInterface != null)
						pDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
						      pDecl.StartLocation = m.GetDeclarationLocation(startPos);
						      pDecl.EndLocation   = qualIdentEndLocation;
						      pDecl.BodyStart   = t.Location;
						      PropertyGetRegion getRegion;
						      PropertySetRegion setRegion;
						   
						AccessorDecls(
#line  1433 "cs.ATG" 
out getRegion, out setRegion);
						Expect(17);

#line  1435 "cs.ATG" 
						pDecl.GetRegion = getRegion;
						pDecl.SetRegion = setRegion;
						pDecl.BodyEnd = t.EndLocation;
						compilationUnit.AddChild(pDecl);
						
					}
				} else if (la.kind == 15) {

#line  1443 "cs.ATG" 
					m.Check(Modifier.Indexers); 
					lexer.NextToken();
					Expect(110);
					Expect(18);
					FormalParameterList(
#line  1444 "cs.ATG" 
p);
					Expect(19);

#line  1445 "cs.ATG" 
					IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
					indexer.StartLocation = m.GetDeclarationLocation(startPos);
					indexer.EndLocation   = t.EndLocation;
					if (explicitInterface != null)
					indexer.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, "this"));
					      PropertyGetRegion getRegion;
					      PropertySetRegion setRegion;
					    
					Expect(16);

#line  1453 "cs.ATG" 
					Point bodyStart = t.Location; 
					AccessorDecls(
#line  1454 "cs.ATG" 
out getRegion, out setRegion);
					Expect(17);

#line  1455 "cs.ATG" 
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

#line  1482 "cs.ATG" 
		TypeReference type;
		
		AttributeSection section;
		Modifier mod = Modifier.None;
		List<AttributeSection> attributes = new List<AttributeSection>();
		List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
		string name;
		PropertyGetRegion getBlock;
		PropertySetRegion setBlock;
		Point startLocation = new Point(-1, -1);
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		
		while (la.kind == 18) {
			AttributeSection(
#line  1495 "cs.ATG" 
out section);

#line  1495 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 88) {
			lexer.NextToken();

#line  1496 "cs.ATG" 
			mod = Modifier.New; startLocation = t.Location; 
		}
		if (
#line  1499 "cs.ATG" 
NotVoidPointer()) {
			Expect(122);

#line  1499 "cs.ATG" 
			if (startLocation.X == -1) startLocation = t.Location; 
			Expect(1);

#line  1499 "cs.ATG" 
			name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  1500 "cs.ATG" 
templates);
			}
			Expect(20);
			if (StartOf(10)) {
				FormalParameterList(
#line  1501 "cs.ATG" 
parameters);
			}
			Expect(21);
			while (
#line  1502 "cs.ATG" 
IdentIsWhere()) {
				TypeParameterConstraintsClause(
#line  1502 "cs.ATG" 
templates);
			}
			Expect(11);

#line  1504 "cs.ATG" 
			MethodDeclaration md = new MethodDeclaration(name, mod, new TypeReference("void"), parameters, attributes);
			md.StartLocation = startLocation;
			md.EndLocation = t.EndLocation;
			md.Templates = templates;
			compilationUnit.AddChild(md);
			
		} else if (StartOf(18)) {
			if (StartOf(9)) {
				Type(
#line  1511 "cs.ATG" 
out type);

#line  1511 "cs.ATG" 
				if (startLocation.X == -1) startLocation = t.Location; 
				if (la.kind == 1) {
					lexer.NextToken();

#line  1513 "cs.ATG" 
					name = t.val; Point qualIdentEndLocation = t.EndLocation; 
					if (la.kind == 20 || la.kind == 23) {
						if (la.kind == 23) {
							TypeParameterList(
#line  1517 "cs.ATG" 
templates);
						}
						Expect(20);
						if (StartOf(10)) {
							FormalParameterList(
#line  1518 "cs.ATG" 
parameters);
						}
						Expect(21);
						while (
#line  1520 "cs.ATG" 
IdentIsWhere()) {
							TypeParameterConstraintsClause(
#line  1520 "cs.ATG" 
templates);
						}
						Expect(11);

#line  1521 "cs.ATG" 
						MethodDeclaration md = new MethodDeclaration(name, mod, type, parameters, attributes);
						md.StartLocation = startLocation;
						md.EndLocation = t.EndLocation;
						md.Templates = templates;
						compilationUnit.AddChild(md);
						
					} else if (la.kind == 16) {

#line  1528 "cs.ATG" 
						PropertyDeclaration pd = new PropertyDeclaration(name, type, mod, attributes); compilationUnit.AddChild(pd); 
						lexer.NextToken();

#line  1529 "cs.ATG" 
						Point bodyStart = t.Location;
						InterfaceAccessors(
#line  1529 "cs.ATG" 
out getBlock, out setBlock);
						Expect(17);

#line  1529 "cs.ATG" 
						pd.GetRegion = getBlock; pd.SetRegion = setBlock; pd.StartLocation = startLocation; pd.EndLocation = qualIdentEndLocation; pd.BodyStart = bodyStart; pd.BodyEnd = t.EndLocation; 
					} else SynErr(151);
				} else if (la.kind == 110) {
					lexer.NextToken();
					Expect(18);
					FormalParameterList(
#line  1532 "cs.ATG" 
parameters);
					Expect(19);

#line  1532 "cs.ATG" 
					Point bracketEndLocation = t.EndLocation; 

#line  1532 "cs.ATG" 
					IndexerDeclaration id = new IndexerDeclaration(type, parameters, mod, attributes); compilationUnit.AddChild(id); 
					Expect(16);

#line  1533 "cs.ATG" 
					Point bodyStart = t.Location;
					InterfaceAccessors(
#line  1533 "cs.ATG" 
out getBlock, out setBlock);
					Expect(17);

#line  1533 "cs.ATG" 
					id.GetRegion = getBlock; id.SetRegion = setBlock; id.StartLocation = startLocation;  id.EndLocation = bracketEndLocation; id.BodyStart = bodyStart; id.BodyEnd = t.EndLocation;
				} else SynErr(152);
			} else {
				lexer.NextToken();

#line  1536 "cs.ATG" 
				if (startLocation.X == -1) startLocation = t.Location; 
				Type(
#line  1536 "cs.ATG" 
out type);
				Expect(1);

#line  1536 "cs.ATG" 
				EventDeclaration ed = new EventDeclaration(type, t.val, mod, attributes, null);
				compilationUnit.AddChild(ed);
				
				Expect(11);

#line  1539 "cs.ATG" 
				ed.StartLocation = startLocation; ed.EndLocation = t.EndLocation; 
			}
		} else SynErr(153);
	}

	void EnumMemberDecl(
#line  1544 "cs.ATG" 
out FieldDeclaration f) {

#line  1546 "cs.ATG" 
		Expression expr = null;
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration var = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1552 "cs.ATG" 
out section);

#line  1552 "cs.ATG" 
			attributes.Add(section); 
		}
		Expect(1);

#line  1553 "cs.ATG" 
		f = new FieldDeclaration(attributes);
		var = new VariableDeclaration (t.val);
		var.EndLocation = t.EndLocation;
		var.StartLocation = t.Location;
		f.StartLocation = t.Location;
		f.Fields.Add (var);
		
		if (la.kind == 3) {
			lexer.NextToken();
			Expr(
#line  1558 "cs.ATG" 
out expr);

#line  1558 "cs.ATG"
			var.EndLocation = t.EndLocation;
			var.Initializer = expr; 
		}
	}

	void TypeWithRestriction(
#line  1023 "cs.ATG" 
out TypeReference type, bool allowNullable, bool canBeUnbound) {

#line  1025 "cs.ATG"
		Point startPos = la.Location;
		string name;
		int pointer = 0;
		type = null;
		
		if (la.kind == 1 || la.kind == 90 || la.kind == 107) {
			ClassType(
#line  1030 "cs.ATG" 
out type, canBeUnbound);
		} else if (StartOf(4)) {
			SimpleType(
#line  1031 "cs.ATG" 
out name);

#line  1031 "cs.ATG" 
			type = new TypeReference(name); 
		} else if (la.kind == 122) {
			lexer.NextToken();
			Expect(6);

#line  1032 "cs.ATG" 
			pointer = 1; type = new TypeReference("void"); 
		} else SynErr(154);

#line  1033 "cs.ATG" 
		List<int> r = new List<int>(); 
		if (
#line  1035 "cs.ATG" 
allowNullable && la.kind == Tokens.Question) {
			NullableQuestionMark(
#line  1035 "cs.ATG" 
ref type);
		}
		while (
#line  1037 "cs.ATG" 
IsPointerOrDims()) {

#line  1037 "cs.ATG" 
			int i = 0; 
			if (la.kind == 6) {
				lexer.NextToken();

#line  1038 "cs.ATG" 
				++pointer; 
			} else if (la.kind == 18) {
				lexer.NextToken();
				while (la.kind == 14) {
					lexer.NextToken();

#line  1039 "cs.ATG" 
					++i; 
				}
				Expect(19);

#line  1039 "cs.ATG" 
				r.Add(i); 
			} else SynErr(155);
		}

#line  1042 "cs.ATG" 
		if (type != null) {
			type.RankSpecifier = r.ToArray ();
			type.PointerNestingLevel = pointer;
			type.EndLocation = t.EndLocation;
			type.StartLocation = startPos;
		}
		
	}

	void SimpleType(
#line  1070 "cs.ATG" 
out string name) {

#line  1071 "cs.ATG" 
		name = String.Empty; 
		if (StartOf(19)) {
			IntegralType(
#line  1073 "cs.ATG" 
out name);
		} else if (la.kind == 74) {
			lexer.NextToken();

#line  1074 "cs.ATG" 
			name = "float"; 
		} else if (la.kind == 65) {
			lexer.NextToken();

#line  1075 "cs.ATG" 
			name = "double"; 
		} else if (la.kind == 61) {
			lexer.NextToken();

#line  1076 "cs.ATG" 
			name = "decimal"; 
		} else if (la.kind == 51) {
			lexer.NextToken();

#line  1077 "cs.ATG" 
			name = "bool"; 
		} else SynErr(156);
	}

	void NullableQuestionMark(
#line  2463 "cs.ATG" 
ref TypeReference typeRef) {

#line  2464 "cs.ATG" 
		List<TypeReference> typeArguments = new List<TypeReference>(1); 
		Expect(12);

#line  2468 "cs.ATG" 
		if (typeRef != null) typeArguments.Add(typeRef);
		typeRef = new TypeReference("System.Nullable", typeArguments);
		
	}

	void FixedParameter(
#line  1107 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  1109 "cs.ATG" 
		TypeReference type;
		ParamModifier mod = ParamModifier.In;
		System.Drawing.Point start = t.Location;
		
		if (la.kind == 92 || la.kind == 99) {
			if (la.kind == 99) {
				lexer.NextToken();

#line  1115 "cs.ATG" 
				mod = ParamModifier.Ref; 
			} else {
				lexer.NextToken();

#line  1116 "cs.ATG" 
				mod = ParamModifier.Out; 
			}
		}
		Type(
#line  1118 "cs.ATG" 
out type);
		Expect(1);

#line  1118 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, mod); p.StartLocation = start; p.EndLocation = t.Location; 
	}

	void ParameterArray(
#line  1121 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  1122 "cs.ATG" 
		TypeReference type; 
		Expect(94);
		Type(
#line  1124 "cs.ATG" 
out type);
		Expect(1);

#line  1124 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, ParamModifier.Params); 
	}

	void AccessorModifiers(
#line  1127 "cs.ATG" 
out Modifiers m) {

#line  1128 "cs.ATG" 
		m = new Modifiers(); 
		if (la.kind == 95) {
			lexer.NextToken();

#line  1130 "cs.ATG" 
			m.Add(Modifier.Private, t.Location); 
		} else if (la.kind == 96) {
			lexer.NextToken();

#line  1131 "cs.ATG" 
			m.Add(Modifier.Protected, t.Location); 
			if (la.kind == 83) {
				lexer.NextToken();

#line  1132 "cs.ATG" 
				m.Add(Modifier.Internal, t.Location); 
			}
		} else if (la.kind == 83) {
			lexer.NextToken();

#line  1133 "cs.ATG" 
			m.Add(Modifier.Internal, t.Location); 
			if (la.kind == 96) {
				lexer.NextToken();

#line  1134 "cs.ATG" 
				m.Add(Modifier.Protected, t.Location); 
			}
		} else SynErr(157);
	}

	void Block(
#line  1683 "cs.ATG" 
out Statement stmt) {
		Expect(16);

#line  1685 "cs.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		blockStmt.StartLocation = t.EndLocation;
		compilationUnit.BlockStart(blockStmt);
		if (!parseMethodContents) lexer.SkipCurrentBlock();
		
		while (StartOf(20)) {
			Statement();
		}
		Expect(17);

#line  1692 "cs.ATG" 
		stmt = blockStmt;
		blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void EventAccessorDecls(
#line  1618 "cs.ATG" 
out EventAddRegion addBlock, out EventRemoveRegion removeBlock) {

#line  1619 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		Statement stmt;
		addBlock = null;
		removeBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1626 "cs.ATG" 
out section);

#line  1626 "cs.ATG" 
			attributes.Add(section); 
		}
		if (
#line  1628 "cs.ATG" 
IdentIsAdd()) {

#line  1628 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); 
			AddAccessorDecl(
#line  1629 "cs.ATG" 
out stmt);

#line  1629 "cs.ATG" 
			attributes = new List<AttributeSection>(); addBlock.Block = (BlockStatement)stmt; 
			while (la.kind == 18) {
				AttributeSection(
#line  1630 "cs.ATG" 
out section);

#line  1630 "cs.ATG" 
				attributes.Add(section); 
			}
			RemoveAccessorDecl(
#line  1631 "cs.ATG" 
out stmt);

#line  1631 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; 
		} else if (
#line  1632 "cs.ATG" 
IdentIsRemove()) {
			RemoveAccessorDecl(
#line  1633 "cs.ATG" 
out stmt);

#line  1633 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; attributes = new List<AttributeSection>(); 
			while (la.kind == 18) {
				AttributeSection(
#line  1634 "cs.ATG" 
out section);

#line  1634 "cs.ATG" 
				attributes.Add(section); 
			}
			AddAccessorDecl(
#line  1635 "cs.ATG" 
out stmt);

#line  1635 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); addBlock.Block = (BlockStatement)stmt; 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1636 "cs.ATG" 
			Error("add or remove accessor declaration expected"); 
		} else SynErr(158);
	}

	void ConstructorInitializer(
#line  1714 "cs.ATG" 
out ConstructorInitializer ci) {

#line  1715 "cs.ATG" 
		Expression expr; ci = new ConstructorInitializer(); 
		Expect(9);
		if (la.kind == 50) {
			lexer.NextToken();

#line  1719 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.Base; 
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  1720 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.This; 
		} else SynErr(159);
		Expect(20);
		if (StartOf(21)) {
			Argument(
#line  1723 "cs.ATG" 
out expr);

#line  1723 "cs.ATG" 
			if (expr != null) { ci.Arguments.Add(expr); } 
			while (la.kind == 14) {
				lexer.NextToken();
				Argument(
#line  1723 "cs.ATG" 
out expr);

#line  1723 "cs.ATG" 
				if (expr != null) { ci.Arguments.Add(expr); } 
			}
		}
		Expect(21);
	}

	void OverloadableOperator(
#line  1737 "cs.ATG" 
out OverloadableOperatorType op) {

#line  1738 "cs.ATG" 
		op = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 4: {
			lexer.NextToken();

#line  1740 "cs.ATG" 
			op = OverloadableOperatorType.Add; 
			break;
		}
		case 5: {
			lexer.NextToken();

#line  1741 "cs.ATG" 
			op = OverloadableOperatorType.Subtract; 
			break;
		}
		case 24: {
			lexer.NextToken();

#line  1743 "cs.ATG" 
			op = OverloadableOperatorType.Not; 
			break;
		}
		case 27: {
			lexer.NextToken();

#line  1744 "cs.ATG" 
			op = OverloadableOperatorType.BitNot; 
			break;
		}
		case 31: {
			lexer.NextToken();

#line  1746 "cs.ATG" 
			op = OverloadableOperatorType.Increment; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1747 "cs.ATG" 
			op = OverloadableOperatorType.Decrement; 
			break;
		}
		case 112: {
			lexer.NextToken();

#line  1749 "cs.ATG" 
			op = OverloadableOperatorType.True; 
			break;
		}
		case 71: {
			lexer.NextToken();

#line  1750 "cs.ATG" 
			op = OverloadableOperatorType.False; 
			break;
		}
		case 6: {
			lexer.NextToken();

#line  1752 "cs.ATG" 
			op = OverloadableOperatorType.Multiply; 
			break;
		}
		case 7: {
			lexer.NextToken();

#line  1753 "cs.ATG" 
			op = OverloadableOperatorType.Divide; 
			break;
		}
		case 8: {
			lexer.NextToken();

#line  1754 "cs.ATG" 
			op = OverloadableOperatorType.Modulus; 
			break;
		}
		case 28: {
			lexer.NextToken();

#line  1756 "cs.ATG" 
			op = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 29: {
			lexer.NextToken();

#line  1757 "cs.ATG" 
			op = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1758 "cs.ATG" 
			op = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1760 "cs.ATG" 
			op = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 33: {
			lexer.NextToken();

#line  1761 "cs.ATG" 
			op = OverloadableOperatorType.Equality; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1762 "cs.ATG" 
			op = OverloadableOperatorType.InEquality; 
			break;
		}
		case 23: {
			lexer.NextToken();

#line  1763 "cs.ATG" 
			op = OverloadableOperatorType.LessThan; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1764 "cs.ATG" 
			op = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1765 "cs.ATG" 
			op = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 22: {
			lexer.NextToken();

#line  1766 "cs.ATG" 
			op = OverloadableOperatorType.GreaterThan; 
			if (la.kind == 22) {
				lexer.NextToken();

#line  1766 "cs.ATG" 
				op = OverloadableOperatorType.ShiftRight; 
			}
			break;
		}
		default: SynErr(160); break;
		}
	}

	void VariableDeclarator(
#line  1676 "cs.ATG" 
List<VariableDeclaration> fieldDeclaration) {

#line  1677 "cs.ATG" 
		Expression expr = null; 
		Expect(1);

#line  1679 "cs.ATG" 
		VariableDeclaration var = new VariableDeclaration (t.val);
		var.StartLocation = t.Location;
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1680 "cs.ATG" 
out expr);

#line  1680 "cs.ATG" 
			var.Initializer = expr; 
		}

#line  1680 "cs.ATG"
		var.EndLocation = t.EndLocation;
		fieldDeclaration.Add (var); 
	}

	void AccessorDecls(
#line  1562 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1564 "cs.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>(); 
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		Modifiers modifiers = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1571 "cs.ATG" 
out section);

#line  1571 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 83 || la.kind == 95 || la.kind == 96) {
			AccessorModifiers(
#line  1572 "cs.ATG" 
out modifiers);
		}
		if (
#line  1574 "cs.ATG" 
IdentIsGet()) {
			GetAccessorDecl(
#line  1575 "cs.ATG" 
out getBlock, attributes);

#line  1576 "cs.ATG" 
			if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(22)) {

#line  1577 "cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1578 "cs.ATG" 
out section);

#line  1578 "cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 83 || la.kind == 95 || la.kind == 96) {
					AccessorModifiers(
#line  1579 "cs.ATG" 
out modifiers);
				}
				SetAccessorDecl(
#line  1580 "cs.ATG" 
out setBlock, attributes);

#line  1581 "cs.ATG" 
				if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (
#line  1583 "cs.ATG" 
IdentIsSet()) {
			SetAccessorDecl(
#line  1584 "cs.ATG" 
out setBlock, attributes);

#line  1585 "cs.ATG" 
			if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(22)) {

#line  1586 "cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1587 "cs.ATG" 
out section);

#line  1587 "cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 83 || la.kind == 95 || la.kind == 96) {
					AccessorModifiers(
#line  1588 "cs.ATG" 
out modifiers);
				}
				GetAccessorDecl(
#line  1589 "cs.ATG" 
out getBlock, attributes);

#line  1590 "cs.ATG" 
				if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1592 "cs.ATG" 
			Error("get or set accessor declaration expected"); 
		} else SynErr(161);
	}

	void InterfaceAccessors(
#line  1640 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1642 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		getBlock = null; setBlock = null;
		PropertyGetSetRegion lastBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1648 "cs.ATG" 
out section);

#line  1648 "cs.ATG" 
			attributes.Add(section); 
		}

#line  1649 "cs.ATG" 
		Point startLocation = la.Location; 
		if (
#line  1651 "cs.ATG" 
IdentIsGet()) {
			Expect(1);

#line  1651 "cs.ATG" 
			getBlock = new PropertyGetRegion(null, attributes); 
		} else if (
#line  1652 "cs.ATG" 
IdentIsSet()) {
			Expect(1);

#line  1652 "cs.ATG" 
			setBlock = new PropertySetRegion(null, attributes); 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1653 "cs.ATG" 
			Error("set or get expected"); 
		} else SynErr(162);
		Expect(11);

#line  1656 "cs.ATG" 
		if (getBlock != null) { getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; }
		if (setBlock != null) { setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; }
		attributes = new List<AttributeSection>(); 
		if (la.kind == 1 || la.kind == 18) {
			while (la.kind == 18) {
				AttributeSection(
#line  1660 "cs.ATG" 
out section);

#line  1660 "cs.ATG" 
				attributes.Add(section); 
			}

#line  1661 "cs.ATG" 
			startLocation = la.Location; 
			if (
#line  1663 "cs.ATG" 
IdentIsGet()) {
				Expect(1);

#line  1663 "cs.ATG" 
				if (getBlock != null) Error("get already declared");
				else { getBlock = new PropertyGetRegion(null, attributes); lastBlock = getBlock; }
				
			} else if (
#line  1666 "cs.ATG" 
IdentIsSet()) {
				Expect(1);

#line  1666 "cs.ATG" 
				if (setBlock != null) Error("set already declared");
				else { setBlock = new PropertySetRegion(null, attributes); lastBlock = setBlock; }
				
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  1669 "cs.ATG" 
				Error("set or get expected"); 
			} else SynErr(163);
			Expect(11);

#line  1672 "cs.ATG" 
			if (lastBlock != null) { lastBlock.StartLocation = startLocation; lastBlock.EndLocation = t.EndLocation; } 
		}
	}

	void GetAccessorDecl(
#line  1596 "cs.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1597 "cs.ATG" 
		Statement stmt = null; 
		Expect(1);

#line  1600 "cs.ATG" 
		if (t.val != "get") Error("get expected"); 

#line  1601 "cs.ATG" 
		Point startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1602 "cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(164);

#line  1603 "cs.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 

#line  1604 "cs.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
	}

	void SetAccessorDecl(
#line  1607 "cs.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1608 "cs.ATG" 
		Statement stmt = null; 
		Expect(1);

#line  1611 "cs.ATG" 
		if (t.val != "set") Error("set expected"); 

#line  1612 "cs.ATG" 
		Point startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1613 "cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(165);

#line  1614 "cs.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes); 

#line  1615 "cs.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
	}

	void AddAccessorDecl(
#line  1698 "cs.ATG" 
out Statement stmt) {

#line  1699 "cs.ATG" 
		stmt = null;
		Expect(1);

#line  1702 "cs.ATG" 
		if (t.val != "add") Error("add expected"); 
		Block(
#line  1703 "cs.ATG" 
out stmt);
	}

	void RemoveAccessorDecl(
#line  1706 "cs.ATG" 
out Statement stmt) {

#line  1707 "cs.ATG" 
		stmt = null;
		Expect(1);

#line  1710 "cs.ATG" 
		if (t.val != "remove") Error("remove expected"); 
		Block(
#line  1711 "cs.ATG" 
out stmt);
	}

	void VariableInitializer(
#line  1727 "cs.ATG" 
out Expression initializerExpression) {

#line  1728 "cs.ATG" 
		TypeReference type = null; Expression expr = null; initializerExpression = null; 
		if (StartOf(5)) {
			Expr(
#line  1730 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 16) {
			ArrayInitializer(
#line  1731 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 105) {
			lexer.NextToken();
			Type(
#line  1732 "cs.ATG" 
out type);
			Expect(18);
			Expr(
#line  1732 "cs.ATG" 
out expr);
			Expect(19);

#line  1732 "cs.ATG" 
			initializerExpression = new StackAllocExpression(type, expr); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1734 "cs.ATG" 
out type);
			Expect(21);

#line  1734 "cs.ATG" 
			initializerExpression = new DefaultValueExpression(type); 
		} else SynErr(166);
	}

	void Statement() {

#line  1846 "cs.ATG" 
		TypeReference type;
		Expression expr;
		Statement stmt = null;
		Point startPos = la.Location;
		
		if (
#line  1854 "cs.ATG" 
IsLabel()) {
			Expect(1);

#line  1854 "cs.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val)); 
			Expect(9);
			Statement();
		} else if (la.kind == 59) {
			lexer.NextToken();
			Type(
#line  1857 "cs.ATG" 
out type);

#line  1857 "cs.ATG" 
			LocalVariableDeclaration vars = new LocalVariableDeclaration (type, Modifier.Const);
			vars.StartLocation = t.Location;
			VariableDeclaration var;
			string ident = null;
			Point varStart;
			Expect(1);

#line  1858 "cs.ATG"
			varStart = t.Location;
			ident = t.val; 
			Expect(3);
			Expr(
#line  1859 "cs.ATG" 
out expr);

#line  1859 "cs.ATG"
			var = new VariableDeclaration (ident, expr);
			var.EndLocation = t.EndLocation;
			var.StartLocation = varStart;
			var.TypeReference = type;
			vars.Variables.Add (var); 
			while (la.kind == 14) {
				lexer.NextToken();
				Expect(1);

#line  1860 "cs.ATG"
				varStart = t.Location;
				ident = t.val; 
				Expect(3);
				Expr(
#line  1860 "cs.ATG" 
out expr);

#line  1860 "cs.ATG"
				var = new VariableDeclaration (ident, expr);
				var.EndLocation = t.EndLocation;
				var.StartLocation = varStart;
				var.TypeReference = type;
				vars.Variables.Add (var);
			}
			Expect(11);

#line  1861 "cs.ATG"
			vars.EndLocation = t.EndLocation;
			compilationUnit.AddChild (vars);
		} else if (
#line  1863 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1863 "cs.ATG" 
out stmt);
			Expect(11);

#line  1863 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else if (StartOf(23)) {
			EmbeddedStatement(
#line  1864 "cs.ATG" 
out stmt);

#line  1864 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(167);

#line  1870 "cs.ATG" 
		if (stmt != null) {
			stmt.StartLocation = startPos;
			stmt.EndLocation = t.EndLocation;
		}
		
	}

	void Argument(
#line  1769 "cs.ATG" 
out Expression argumentexpr) {

#line  1771 "cs.ATG" 
		Expression expr;
		FieldDirection fd = FieldDirection.None;
		
		if (la.kind == 92 || la.kind == 99) {
			if (la.kind == 99) {
				lexer.NextToken();

#line  1776 "cs.ATG" 
				fd = FieldDirection.Ref; 
			} else {
				lexer.NextToken();

#line  1777 "cs.ATG" 
				fd = FieldDirection.Out; 
			}
		}
		Expr(
#line  1779 "cs.ATG" 
out expr);

#line  1779 "cs.ATG" 
		argumentexpr = fd != FieldDirection.None ? argumentexpr = new DirectionExpression(fd, expr) : expr; 
	}

	void ArrayInitializer(
#line  1799 "cs.ATG" 
out Expression outExpr) {

#line  1801 "cs.ATG" 
		Expression expr = null;
		ArrayInitializerExpression initializer = new ArrayInitializerExpression();
		
		Expect(16);
		if (StartOf(24)) {
			VariableInitializer(
#line  1806 "cs.ATG" 
out expr);

#line  1807 "cs.ATG" 
			if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			while (
#line  1808 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				VariableInitializer(
#line  1809 "cs.ATG" 
out expr);

#line  1810 "cs.ATG" 
				if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);

#line  1814 "cs.ATG" 
		outExpr = initializer; 
	}

	void AssignmentOperator(
#line  1782 "cs.ATG" 
out AssignmentOperatorType op) {

#line  1783 "cs.ATG" 
		op = AssignmentOperatorType.None; 
		if (la.kind == 3) {
			lexer.NextToken();

#line  1785 "cs.ATG" 
			op = AssignmentOperatorType.Assign; 
		} else if (la.kind == 38) {
			lexer.NextToken();

#line  1786 "cs.ATG" 
			op = AssignmentOperatorType.Add; 
		} else if (la.kind == 39) {
			lexer.NextToken();

#line  1787 "cs.ATG" 
			op = AssignmentOperatorType.Subtract; 
		} else if (la.kind == 40) {
			lexer.NextToken();

#line  1788 "cs.ATG" 
			op = AssignmentOperatorType.Multiply; 
		} else if (la.kind == 41) {
			lexer.NextToken();

#line  1789 "cs.ATG" 
			op = AssignmentOperatorType.Divide; 
		} else if (la.kind == 42) {
			lexer.NextToken();

#line  1790 "cs.ATG" 
			op = AssignmentOperatorType.Modulus; 
		} else if (la.kind == 43) {
			lexer.NextToken();

#line  1791 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseAnd; 
		} else if (la.kind == 44) {
			lexer.NextToken();

#line  1792 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseOr; 
		} else if (la.kind == 45) {
			lexer.NextToken();

#line  1793 "cs.ATG" 
			op = AssignmentOperatorType.ExclusiveOr; 
		} else if (la.kind == 46) {
			lexer.NextToken();

#line  1794 "cs.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
		} else if (
#line  1795 "cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			Expect(22);
			Expect(35);

#line  1796 "cs.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
		} else SynErr(168);
	}

	void LocalVariableDecl(
#line  1817 "cs.ATG" 
out Statement stmt) {

#line  1819 "cs.ATG" 
		TypeReference type;
		VariableDeclaration var = null;
		LocalVariableDeclaration localVariableDeclaration; 
		Point startPos = t.Location;
		
		Type(
#line  1824 "cs.ATG" 
out type);

#line  1824 "cs.ATG" 
		localVariableDeclaration = new LocalVariableDeclaration(type);
		localVariableDeclaration.StartLocation = startPos; 
		LocalVariableDeclarator(
#line  1825 "cs.ATG" 
out var);

#line  1825 "cs.ATG" 
		var.TypeReference = type;
		localVariableDeclaration.Variables.Add(var);
		while (la.kind == 14) {
			lexer.NextToken();
			LocalVariableDeclarator(
#line  1826 "cs.ATG" 
out var);

#line  1826 "cs.ATG"
			var.TypeReference = type;
			localVariableDeclaration.Variables.Add(var);
		}

#line  1827 "cs.ATG" 
		stmt = localVariableDeclaration;
		stmt.EndLocation = t.EndLocation;
	}

	void LocalVariableDeclarator(
#line  1830 "cs.ATG" 
out VariableDeclaration var) {

#line  1831 "cs.ATG" 
		Expression expr = null; 
		Expect(1);

#line  1834 "cs.ATG" 
		var = new VariableDeclaration (t.val);
		var.EndLocation = t.EndLocation;
		var.StartLocation = t.Location;
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1834 "cs.ATG" 
out expr);

#line  1834 "cs.ATG"
			var.EndLocation = t.EndLocation;
			var.Initializer = expr; 
		}
	}

	void EmbeddedStatement(
#line  1877 "cs.ATG" 
out Statement statement) {

#line  1879 "cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		
		if (la.kind == 16) {
			Block(
#line  1885 "cs.ATG" 
out statement);
		} else if (la.kind == 11) {
			lexer.NextToken();

#line  1887 "cs.ATG" 
			statement = new EmptyStatement(); 
		} else if (
#line  1889 "cs.ATG" 
UnCheckedAndLBrace()) {

#line  1889 "cs.ATG" 
			Statement block; bool isChecked = true; 
			if (la.kind == 57) {
				lexer.NextToken();
			} else if (la.kind == 117) {
				lexer.NextToken();

#line  1890 "cs.ATG" 
				isChecked = false;
			} else SynErr(169);
			Block(
#line  1891 "cs.ATG" 
out block);

#line  1891 "cs.ATG" 
			statement = isChecked ? (Statement)new CheckedStatement(block) : (Statement)new UncheckedStatement(block); 
		} else if (la.kind == 78) {
			lexer.NextToken();

#line  1893 "cs.ATG" 
			Statement elseStatement = null; 
			Expect(20);
			Expr(
#line  1894 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1895 "cs.ATG" 
out embeddedStatement);
			if (la.kind == 66) {
				lexer.NextToken();
				EmbeddedStatement(
#line  1896 "cs.ATG" 
out elseStatement);
			}

#line  1897 "cs.ATG" 
			statement = elseStatement != null ? new IfElseStatement(expr, embeddedStatement, elseStatement) : new IfElseStatement(expr, embeddedStatement); 

#line  1898 "cs.ATG" 
			if (elseStatement is IfElseStatement && (elseStatement as IfElseStatement).TrueStatement.Count == 1) {
			/* else if-section (otherwise we would have a BlockStatment) */
			(statement as IfElseStatement).ElseIfSections.Add(
			             new ElseIfSection((elseStatement as IfElseStatement).Condition,
			                               (elseStatement as IfElseStatement).TrueStatement[0]));
			(statement as IfElseStatement).ElseIfSections.AddRange((elseStatement as IfElseStatement).ElseIfSections);
			(statement as IfElseStatement).FalseStatement = (elseStatement as IfElseStatement).FalseStatement;
			} 
		} else if (la.kind == 109) {
			lexer.NextToken();

#line  1906 "cs.ATG" 
			List<SwitchSection> switchSections = new List<SwitchSection>(); SwitchSection switchSection; 
			Expect(20);
			Expr(
#line  1907 "cs.ATG" 
out expr);
			Expect(21);
			Expect(16);
			while (la.kind == 54 || la.kind == 62) {
				SwitchSection(
#line  1908 "cs.ATG" 
out switchSection);

#line  1908 "cs.ATG" 
				switchSections.Add(switchSection); 
			}
			Expect(17);

#line  1909 "cs.ATG" 
			statement = new SwitchStatement(expr, switchSections); 
		} else if (la.kind == 124) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1911 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1913 "cs.ATG" 
out embeddedStatement);

#line  1913 "cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
		} else if (la.kind == 64) {
			lexer.NextToken();
			EmbeddedStatement(
#line  1914 "cs.ATG" 
out embeddedStatement);
			Expect(124);
			Expect(20);
			Expr(
#line  1915 "cs.ATG" 
out expr);
			Expect(21);
			Expect(11);

#line  1915 "cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.End); 
		} else if (la.kind == 75) {
			lexer.NextToken();

#line  1916 "cs.ATG" 
			List<Statement> initializer = null; List<Statement> iterator = null; 
			Expect(20);
			if (StartOf(5)) {
				ForInitializer(
#line  1917 "cs.ATG" 
out initializer);
			}
			Expect(11);
			if (StartOf(5)) {
				Expr(
#line  1918 "cs.ATG" 
out expr);
			}
			Expect(11);
			if (StartOf(5)) {
				ForIterator(
#line  1919 "cs.ATG" 
out iterator);
			}
			Expect(21);
			EmbeddedStatement(
#line  1920 "cs.ATG" 
out embeddedStatement);

#line  1920 "cs.ATG" 
			statement = new ForStatement(initializer, expr, iterator, embeddedStatement); 
		} else if (la.kind == 76) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1921 "cs.ATG" 
out type);
			Expect(1);

#line  1921 "cs.ATG" 
			string varName = t.val; Point start = t.Location;
			Expect(80);
			Expr(
#line  1922 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1923 "cs.ATG" 
out embeddedStatement);

#line  1923 "cs.ATG" 
			statement = new ForeachStatement(type, varName , expr, embeddedStatement); 
			statement.EndLocation = t.EndLocation;
			
		} else if (la.kind == 52) {
			lexer.NextToken();
			Expect(11);

#line  1927 "cs.ATG" 
			statement = new BreakStatement(); 
		} else if (la.kind == 60) {
			lexer.NextToken();
			Expect(11);

#line  1928 "cs.ATG" 
			statement = new ContinueStatement(); 
		} else if (la.kind == 77) {
			GotoStatement(
#line  1929 "cs.ATG" 
out statement);
		} else if (
#line  1930 "cs.ATG" 
IsYieldStatement()) {
			Expect(1);
			if (la.kind == 100) {
				lexer.NextToken();
				Expr(
#line  1930 "cs.ATG" 
out expr);

#line  1930 "cs.ATG" 
				statement = new YieldStatement(new ReturnStatement(expr)); 
			} else if (la.kind == 52) {
				lexer.NextToken();

#line  1931 "cs.ATG" 
				statement = new YieldStatement(new BreakStatement()); 
			} else SynErr(170);
			Expect(11);
		} else if (la.kind == 100) {
			lexer.NextToken();
			if (StartOf(5)) {
				Expr(
#line  1932 "cs.ATG" 
out expr);
			}
			Expect(11);

#line  1932 "cs.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 111) {
			lexer.NextToken();
			if (StartOf(5)) {
				Expr(
#line  1933 "cs.ATG" 
out expr);
			}
			Expect(11);

#line  1933 "cs.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (StartOf(5)) {
			StatementExpr(
#line  1936 "cs.ATG" 
out statement);
			Expect(11);
		} else if (la.kind == 113) {
			TryStatement(
#line  1938 "cs.ATG" 
out statement);
		} else if (la.kind == 85) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1940 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1941 "cs.ATG" 
out embeddedStatement);

#line  1941 "cs.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 120) {

#line  1943 "cs.ATG" 
			Statement resourceAcquisitionStmt = null; 
			lexer.NextToken();
			Expect(20);
			ResourceAcquisition(
#line  1945 "cs.ATG" 
out resourceAcquisitionStmt);
			Expect(21);
			EmbeddedStatement(
#line  1946 "cs.ATG" 
out embeddedStatement);

#line  1946 "cs.ATG" 
			statement = new UsingStatement(resourceAcquisitionStmt, embeddedStatement); 
		} else if (la.kind == 118) {
			lexer.NextToken();
			Block(
#line  1948 "cs.ATG" 
out embeddedStatement);

#line  1948 "cs.ATG" 
			statement = new UnsafeStatement(embeddedStatement); 
		} else if (la.kind == 73) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1951 "cs.ATG" 
out type);

#line  1951 "cs.ATG" 
			if (type.PointerNestingLevel == 0) Error("can only fix pointer types");
			List<VariableDeclaration> pointerDeclarators = new List<VariableDeclaration>(1);
			
			Expect(1);

#line  1954 "cs.ATG" 
			Point varStart = t.Location;
			VariableDeclaration var;
			string ident = t.val;
			Expect(3);
			Expr(
#line  1955 "cs.ATG" 
out expr);

#line  1955 "cs.ATG"
			var = new VariableDeclaration (ident, expr);
			var.EndLocation = t.EndLocation;
			var.StartLocation = varStart;
			var.TypeReference = type;
			pointerDeclarators.Add (var);
			while (la.kind == 14) {
				lexer.NextToken();
				Expect(1);

#line  1957 "cs.ATG"
				varStart = t.Location;
				ident = t.val; 
				Expect(3);
				Expr(
#line  1958 "cs.ATG" 
out expr);

#line  1958 "cs.ATG"
				var = new VariableDeclaration (ident, expr);
				var.EndLocation = t.EndLocation;
				var.StartLocation = varStart;
				var.TypeReference = type;
				pointerDeclarators.Add (var);
			}
			Expect(21);
			EmbeddedStatement(
#line  1960 "cs.ATG" 
out embeddedStatement);

#line  1960 "cs.ATG" 
			statement = new FixedStatement(type, pointerDeclarators, embeddedStatement); 
		} else SynErr(171);
	}

	void SwitchSection(
#line  1982 "cs.ATG" 
out SwitchSection stmt) {

#line  1984 "cs.ATG" 
		SwitchSection switchSection = new SwitchSection();
		CaseLabel label;
		
		SwitchLabel(
#line  1988 "cs.ATG" 
out label);

#line  1988 "cs.ATG" 
		if (label != null) { switchSection.SwitchLabels.Add(label); } 
		while (la.kind == 54 || la.kind == 62) {
			SwitchLabel(
#line  1990 "cs.ATG" 
out label);

#line  1990 "cs.ATG" 
			if (label != null) { switchSection.SwitchLabels.Add(label); } 
		}

#line  1992 "cs.ATG" 
		compilationUnit.BlockStart(switchSection); 
		Statement();
		while (StartOf(20)) {
			Statement();
		}

#line  1995 "cs.ATG" 
		compilationUnit.BlockEnd();
		stmt = switchSection;
		
	}

	void ForInitializer(
#line  1963 "cs.ATG" 
out List<Statement> initializer) {

#line  1965 "cs.ATG" 
		Statement stmt; 
		initializer = new List<Statement>();
		
		if (
#line  1969 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1969 "cs.ATG" 
out stmt);

#line  1969 "cs.ATG" 
			initializer.Add(stmt);
		} else if (StartOf(5)) {
			StatementExpr(
#line  1970 "cs.ATG" 
out stmt);

#line  1970 "cs.ATG" 
			initializer.Add(stmt);
			while (la.kind == 14) {
				lexer.NextToken();
				StatementExpr(
#line  1970 "cs.ATG" 
out stmt);

#line  1970 "cs.ATG" 
				initializer.Add(stmt);
			}
		} else SynErr(172);
	}

	void ForIterator(
#line  1973 "cs.ATG" 
out List<Statement> iterator) {

#line  1975 "cs.ATG" 
		Statement stmt; 
		iterator = new List<Statement>();
		
		StatementExpr(
#line  1979 "cs.ATG" 
out stmt);

#line  1979 "cs.ATG" 
		iterator.Add(stmt);
		while (la.kind == 14) {
			lexer.NextToken();
			StatementExpr(
#line  1979 "cs.ATG" 
out stmt);

#line  1979 "cs.ATG" 
			iterator.Add(stmt); 
		}
	}

	void GotoStatement(
#line  2050 "cs.ATG" 
out Statement stmt) {

#line  2051 "cs.ATG" 
		Expression expr; stmt = null; 
		Expect(77);
		if (la.kind == 1) {
			lexer.NextToken();

#line  2055 "cs.ATG" 
			stmt = new GotoStatement(t.val); 
			Expect(11);
		} else if (la.kind == 54) {
			lexer.NextToken();
			Expr(
#line  2056 "cs.ATG" 
out expr);
			Expect(11);

#line  2056 "cs.ATG" 
			stmt = new GotoCaseStatement(expr); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(11);

#line  2057 "cs.ATG" 
			stmt = new GotoCaseStatement(null); 
		} else SynErr(173);
	}

	void StatementExpr(
#line  2077 "cs.ATG" 
out Statement stmt) {

#line  2078 "cs.ATG" 
		Expression expr; 
		Expr(
#line  2080 "cs.ATG" 
out expr);

#line  2083 "cs.ATG" 
		stmt = new StatementExpression(expr); 
	}

	void TryStatement(
#line  2007 "cs.ATG" 
out Statement tryStatement) {

#line  2009 "cs.ATG" 
		Statement blockStmt = null, finallyStmt = null;
		List<CatchClause> catchClauses = null;
		
		Expect(113);
		Block(
#line  2013 "cs.ATG" 
out blockStmt);
		if (la.kind == 55) {
			CatchClauses(
#line  2015 "cs.ATG" 
out catchClauses);
			if (la.kind == 72) {
				lexer.NextToken();
				Block(
#line  2015 "cs.ATG" 
out finallyStmt);
			}
		} else if (la.kind == 72) {
			lexer.NextToken();
			Block(
#line  2016 "cs.ATG" 
out finallyStmt);
		} else SynErr(174);

#line  2019 "cs.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
			
	}

	void ResourceAcquisition(
#line  2061 "cs.ATG" 
out Statement stmt) {

#line  2063 "cs.ATG" 
		stmt = null;
		Expression expr;
		
		if (
#line  2068 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  2068 "cs.ATG" 
out stmt);
		} else if (StartOf(5)) {
			Expr(
#line  2069 "cs.ATG" 
out expr);

#line  2073 "cs.ATG" 
			stmt = new StatementExpression(expr); 
		} else SynErr(175);
	}

	void SwitchLabel(
#line  2000 "cs.ATG" 
out CaseLabel label) {

#line  2001 "cs.ATG" 
		Expression expr = null; label = null; 
		if (la.kind == 54) {
			lexer.NextToken();
			Expr(
#line  2003 "cs.ATG" 
out expr);
			Expect(9);

#line  2003 "cs.ATG" 
			label =  new CaseLabel(expr); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(9);

#line  2004 "cs.ATG" 
			label =  new CaseLabel(); 
		} else SynErr(176);
	}

	void CatchClauses(
#line  2024 "cs.ATG" 
out List<CatchClause> catchClauses) {

#line  2026 "cs.ATG" 
		catchClauses = new List<CatchClause>();
		
		Expect(55);

#line  2029 "cs.ATG" 
		string identifier;
		Statement stmt;
		TypeReference typeRef;
		
		if (la.kind == 16) {
			Block(
#line  2035 "cs.ATG" 
out stmt);

#line  2035 "cs.ATG" 
			catchClauses.Add(new CatchClause(stmt)); 
		} else if (la.kind == 20) {
			lexer.NextToken();
			ClassType(
#line  2037 "cs.ATG" 
out typeRef, false);

#line  2037 "cs.ATG" 
			identifier = null; 
			if (la.kind == 1) {
				lexer.NextToken();

#line  2038 "cs.ATG" 
				identifier = t.val; 
			}
			Expect(21);
			Block(
#line  2039 "cs.ATG" 
out stmt);

#line  2040 "cs.ATG" 
			catchClauses.Add(new CatchClause(typeRef, identifier, stmt)); 
			while (
#line  2041 "cs.ATG" 
IsTypedCatch()) {
				Expect(55);
				Expect(20);
				ClassType(
#line  2041 "cs.ATG" 
out typeRef, false);

#line  2041 "cs.ATG" 
				identifier = null; 
				if (la.kind == 1) {
					lexer.NextToken();

#line  2042 "cs.ATG" 
					identifier = t.val; 
				}
				Expect(21);
				Block(
#line  2043 "cs.ATG" 
out stmt);

#line  2044 "cs.ATG" 
				catchClauses.Add(new CatchClause(typeRef, identifier, stmt)); 
			}
			if (la.kind == 55) {
				lexer.NextToken();
				Block(
#line  2046 "cs.ATG" 
out stmt);

#line  2046 "cs.ATG" 
				catchClauses.Add(new CatchClause(stmt)); 
			}
		} else SynErr(177);
	}

	void UnaryExpr(
#line  2104 "cs.ATG" 
out Expression uExpr) {

#line  2106 "cs.ATG" 
		TypeReference type = null;
		Expression expr;
		ArrayList  expressions = new ArrayList();
		uExpr = null;
		
		while (StartOf(25) || 
#line  2128 "cs.ATG" 
IsTypeCast()) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  2115 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Plus)); 
			} else if (la.kind == 5) {
				lexer.NextToken();

#line  2116 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Minus)); 
			} else if (la.kind == 24) {
				lexer.NextToken();

#line  2117 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Not)); 
			} else if (la.kind == 27) {
				lexer.NextToken();

#line  2118 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitNot)); 
			} else if (la.kind == 6) {
				lexer.NextToken();

#line  2119 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Star)); 
			} else if (la.kind == 31) {
				lexer.NextToken();

#line  2120 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Increment)); 
			} else if (la.kind == 32) {
				lexer.NextToken();

#line  2121 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Decrement)); 
			} else if (la.kind == 28) {
				lexer.NextToken();

#line  2122 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitWiseAnd)); 
			} else {
				Expect(20);
				Type(
#line  2128 "cs.ATG" 
out type);
				Expect(21);

#line  2128 "cs.ATG" 
				expressions.Add(new CastExpression(type)); 
			}
		}
		PrimaryExpr(
#line  2132 "cs.ATG" 
out expr);

#line  2132 "cs.ATG" 
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
#line  2303 "cs.ATG" 
ref Expression outExpr) {

#line  2304 "cs.ATG" 
		Expression expr;   
		ConditionalAndExpr(
#line  2306 "cs.ATG" 
ref outExpr);
		while (la.kind == 26) {
			lexer.NextToken();
			UnaryExpr(
#line  2306 "cs.ATG" 
out expr);
			ConditionalAndExpr(
#line  2306 "cs.ATG" 
ref expr);

#line  2306 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalOr, expr);  
		}
	}

	void PrimaryExpr(
#line  2149 "cs.ATG" 
out Expression pexpr) {

#line  2151 "cs.ATG" 
		TypeReference type = null;
		List<TypeReference> typeList = null;
		bool isArrayCreation = false;
		Expression expr;
		pexpr = null;
		
		if (la.kind == 112) {
			lexer.NextToken();

#line  2159 "cs.ATG" 
			pexpr = new PrimitiveExpression(true, "true");  
		} else if (la.kind == 71) {
			lexer.NextToken();

#line  2160 "cs.ATG" 
			pexpr = new PrimitiveExpression(false, "false"); 
		} else if (la.kind == 89) {
			lexer.NextToken();

#line  2161 "cs.ATG" 
			pexpr = new PrimitiveExpression(null, "null");  
		} else if (la.kind == 2) {
			lexer.NextToken();

#line  2162 "cs.ATG" 
			pexpr = new PrimitiveExpression(t.literalValue, t.val);  
		} else if (
#line  2163 "cs.ATG" 
la.kind == Tokens.Identifier && Peek(1).kind == Tokens.DoubleColon) {
			Expect(1);

#line  2164 "cs.ATG" 
			type = new TypeReference(t.val); 
			Expect(10);

#line  2165 "cs.ATG" 
			pexpr = new TypeReferenceExpression(type); 
			Expect(1);

#line  2166 "cs.ATG" 
			if (type.Type == "global") { type.IsGlobal = true; type.Type = (t.val ?? "?"); } else type.Type += "." + (t.val ?? "?"); 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  2168 "cs.ATG" 
			pexpr = new IdentifierExpression(t.val); 
			pexpr.StartLocation = t.Location;
		} else if (la.kind == 20) {
			lexer.NextToken();
			Expr(
#line  2170 "cs.ATG" 
out expr);
			Expect(21);

#line  2170 "cs.ATG" 
			pexpr = new ParenthesizedExpression(expr); 
		} else if (StartOf(26)) {

#line  2172 "cs.ATG" 
			string val = null; 
			switch (la.kind) {
			case 51: {
				lexer.NextToken();

#line  2174 "cs.ATG" 
				val = "bool"; 
				break;
			}
			case 53: {
				lexer.NextToken();

#line  2175 "cs.ATG" 
				val = "byte"; 
				break;
			}
			case 56: {
				lexer.NextToken();

#line  2176 "cs.ATG" 
				val = "char"; 
				break;
			}
			case 61: {
				lexer.NextToken();

#line  2177 "cs.ATG" 
				val = "decimal"; 
				break;
			}
			case 65: {
				lexer.NextToken();

#line  2178 "cs.ATG" 
				val = "double"; 
				break;
			}
			case 74: {
				lexer.NextToken();

#line  2179 "cs.ATG" 
				val = "float"; 
				break;
			}
			case 81: {
				lexer.NextToken();

#line  2180 "cs.ATG" 
				val = "int"; 
				break;
			}
			case 86: {
				lexer.NextToken();

#line  2181 "cs.ATG" 
				val = "long"; 
				break;
			}
			case 90: {
				lexer.NextToken();

#line  2182 "cs.ATG" 
				val = "object"; 
				break;
			}
			case 101: {
				lexer.NextToken();

#line  2183 "cs.ATG" 
				val = "sbyte"; 
				break;
			}
			case 103: {
				lexer.NextToken();

#line  2184 "cs.ATG" 
				val = "short"; 
				break;
			}
			case 107: {
				lexer.NextToken();

#line  2185 "cs.ATG" 
				val = "string"; 
				break;
			}
			case 115: {
				lexer.NextToken();

#line  2186 "cs.ATG" 
				val = "uint"; 
				break;
			}
			case 116: {
				lexer.NextToken();

#line  2187 "cs.ATG" 
				val = "ulong"; 
				break;
			}
			case 119: {
				lexer.NextToken();

#line  2188 "cs.ATG" 
				val = "ushort"; 
				break;
			}
			}

#line  2189 "cs.ATG" 
			t.val = ""; 
			Expect(15);
			Expect(1);

#line  2189 "cs.ATG" 
			pexpr = new FieldReferenceExpression(new TypeReferenceExpression(val), t.val); 
			pexpr.StartLocation = t.Location;
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  2191 "cs.ATG" 
			pexpr = new ThisReferenceExpression(); 
		} else if (la.kind == 50) {
			lexer.NextToken();

#line  2193 "cs.ATG" 
			Expression retExpr = new BaseReferenceExpression(); 
			if (la.kind == 15) {
				lexer.NextToken();
				Expect(1);

#line  2195 "cs.ATG" 
				retExpr = new FieldReferenceExpression(retExpr, t.val); 
				retExpr.StartLocation = t.Location;
			} else if (la.kind == 18) {
				lexer.NextToken();
				Expr(
#line  2196 "cs.ATG" 
out expr);

#line  2196 "cs.ATG" 
				List<Expression> indices = new List<Expression>(); if (expr != null) { indices.Add(expr); } 
				while (la.kind == 14) {
					lexer.NextToken();
					Expr(
#line  2197 "cs.ATG" 
out expr);

#line  2197 "cs.ATG" 
					if (expr != null) { indices.Add(expr); } 
				}
				Expect(19);

#line  2198 "cs.ATG" 
				retExpr = new IndexerExpression(retExpr, indices); 
			} else SynErr(178);

#line  2199 "cs.ATG" 
			pexpr = retExpr; 
		} else if (la.kind == 88) {
			lexer.NextToken();
			NonArrayType(
#line  2200 "cs.ATG" 
out type);

#line  2201 "cs.ATG" 
			List<Expression> parameters = new List<Expression>(); 
			if (la.kind == 20) {
				lexer.NextToken();

#line  2206 "cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters);
				if (StartOf(21)) {
					Argument(
#line  2207 "cs.ATG" 
out expr);

#line  2207 "cs.ATG" 
					if (expr != null) { parameters.Add(expr); } 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  2208 "cs.ATG" 
out expr);

#line  2208 "cs.ATG" 
						if (expr != null) { parameters.Add(expr); } 
					}
				}
				Expect(21);

#line  2210 "cs.ATG" 
				pexpr = oce; 
			} else if (la.kind == 18) {
				lexer.NextToken();

#line  2212 "cs.ATG" 
				isArrayCreation = true; ArrayCreateExpression ace = new ArrayCreateExpression(type); pexpr = ace; 

#line  2213 "cs.ATG" 
				int dims = 0; List<int> ranks = new List<int>(); 
				if (la.kind == 14 || la.kind == 19) {
					while (la.kind == 14) {
						lexer.NextToken();

#line  2215 "cs.ATG" 
						dims += 1; 
					}
					Expect(19);

#line  2216 "cs.ATG" 
					ranks.Add(dims); dims = 0; 
					while (la.kind == 18) {
						lexer.NextToken();
						while (la.kind == 14) {
							lexer.NextToken();

#line  2217 "cs.ATG" 
							++dims; 
						}
						Expect(19);

#line  2217 "cs.ATG" 
						ranks.Add(dims); dims = 0; 
					}

#line  2218 "cs.ATG" 
					ace.CreateType.RankSpecifier = ranks.ToArray(); 
					ArrayInitializer(
#line  2219 "cs.ATG" 
out expr);

#line  2219 "cs.ATG" 
					ace.ArrayInitializer = (ArrayInitializerExpression)expr; 
				} else if (StartOf(5)) {
					Expr(
#line  2220 "cs.ATG" 
out expr);

#line  2220 "cs.ATG" 
					if (expr != null) parameters.Add(expr); 
					while (la.kind == 14) {
						lexer.NextToken();

#line  2221 "cs.ATG" 
						dims += 1; 
						Expr(
#line  2222 "cs.ATG" 
out expr);

#line  2222 "cs.ATG" 
						if (expr != null) parameters.Add(expr); 
					}
					Expect(19);

#line  2224 "cs.ATG" 
					ranks.Add(dims); ace.Arguments = parameters; dims = 0; 
					while (la.kind == 18) {
						lexer.NextToken();
						while (la.kind == 14) {
							lexer.NextToken();

#line  2225 "cs.ATG" 
							++dims; 
						}
						Expect(19);

#line  2225 "cs.ATG" 
						ranks.Add(dims); dims = 0; 
					}

#line  2226 "cs.ATG" 
					ace.CreateType.RankSpecifier = ranks.ToArray(); 
					if (la.kind == 16) {
						ArrayInitializer(
#line  2227 "cs.ATG" 
out expr);

#line  2227 "cs.ATG" 
						ace.ArrayInitializer = (ArrayInitializerExpression)expr; 
					}
				} else SynErr(179);
			} else SynErr(180);
		} else if (la.kind == 114) {
			lexer.NextToken();
			Expect(20);
			if (
#line  2232 "cs.ATG" 
NotVoidPointer()) {
				Expect(122);

#line  2232 "cs.ATG" 
				type = new TypeReference("void"); 
			} else if (StartOf(9)) {
				TypeWithRestriction(
#line  2233 "cs.ATG" 
out type, true, true);
			} else SynErr(181);
			Expect(21);

#line  2234 "cs.ATG" 
			pexpr = new TypeOfExpression(type); 
		} else if (
#line  2236 "cs.ATG" 
la.kind == Tokens.Default && Peek(1).kind == Tokens.OpenParenthesis) {
			Expect(62);
			Expect(20);
			Type(
#line  2238 "cs.ATG" 
out type);
			Expect(21);

#line  2238 "cs.ATG" 
			pexpr = new DefaultValueExpression(type); 
		} else if (la.kind == 104) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  2239 "cs.ATG" 
out type);
			Expect(21);

#line  2239 "cs.ATG" 
			pexpr = new SizeOfExpression(type); 
		} else if (la.kind == 57) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  2240 "cs.ATG" 
out expr);
			Expect(21);

#line  2240 "cs.ATG" 
			pexpr = new CheckedExpression(expr); 
		} else if (la.kind == 117) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  2241 "cs.ATG" 
out expr);
			Expect(21);

#line  2241 "cs.ATG" 
			pexpr = new UncheckedExpression(expr); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			AnonymousMethodExpr(
#line  2242 "cs.ATG" 
out expr);

#line  2242 "cs.ATG" 
			pexpr = expr; 
		} else SynErr(182);
		while (StartOf(27) || 
#line  2253 "cs.ATG" 
IsGenericFollowedBy(Tokens.Dot) && IsTypeReferenceExpression(pexpr) || 
#line  2262 "cs.ATG" 
IsGenericFollowedBy(Tokens.OpenParenthesis)) {
			if (la.kind == 31 || la.kind == 32) {
				if (la.kind == 31) {
					lexer.NextToken();

#line  2246 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostIncrement); 
				} else if (la.kind == 32) {
					lexer.NextToken();

#line  2247 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostDecrement); 
				} else SynErr(183);
			} else if (la.kind == 47) {
				lexer.NextToken();
				Expect(1);

#line  2250 "cs.ATG" 
				pexpr = new PointerReferenceExpression(pexpr, t.val); 
			} else if (la.kind == 15) {
				lexer.NextToken();
				Expect(1);

#line  2251 "cs.ATG" 
				pexpr = new FieldReferenceExpression(pexpr, t.val);
				pexpr.StartLocation = t.Location;
			} else if (
#line  2253 "cs.ATG" 
IsGenericFollowedBy(Tokens.Dot) && IsTypeReferenceExpression(pexpr)) {
				TypeArgumentList(
#line  2254 "cs.ATG" 
out typeList, false);
				Expect(15);
				Expect(1);

#line  2256 "cs.ATG" 
				pexpr = new FieldReferenceExpression(GetTypeReferenceExpression(pexpr, typeList), t.val);
				pexpr.StartLocation = t.Location;
			} else if (la.kind == 20) {
				lexer.NextToken();

#line  2258 "cs.ATG" 
				List<Expression> parameters = new List<Expression>(); 
				if (StartOf(21)) {
					Argument(
#line  2259 "cs.ATG" 
out expr);

#line  2259 "cs.ATG" 
					if (expr != null) {parameters.Add(expr);} 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  2260 "cs.ATG" 
out expr);

#line  2260 "cs.ATG" 
						if (expr != null) {parameters.Add(expr);} 
					}
				}
				Expect(21);

#line  2261 "cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters); 
			} else if (
#line  2262 "cs.ATG" 
IsGenericFollowedBy(Tokens.OpenParenthesis)) {
				TypeArgumentList(
#line  2262 "cs.ATG" 
out typeList, false);
				Expect(20);

#line  2263 "cs.ATG" 
				List<Expression> parameters = new List<Expression>(); 
				if (StartOf(21)) {
					Argument(
#line  2264 "cs.ATG" 
out expr);

#line  2264 "cs.ATG" 
					if (expr != null) {parameters.Add(expr);} 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  2265 "cs.ATG" 
out expr);

#line  2265 "cs.ATG" 
						if (expr != null) {parameters.Add(expr);} 
					}
				}
				Expect(21);

#line  2266 "cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters, typeList); 
			} else {

#line  2268 "cs.ATG" 
				if (isArrayCreation) Error("element access not allow on array creation");
				List<Expression> indices = new List<Expression>();
				
				lexer.NextToken();
				Expr(
#line  2271 "cs.ATG" 
out expr);

#line  2271 "cs.ATG" 
				if (expr != null) { indices.Add(expr); } 
				while (la.kind == 14) {
					lexer.NextToken();
					Expr(
#line  2272 "cs.ATG" 
out expr);

#line  2272 "cs.ATG" 
					if (expr != null) { indices.Add(expr); } 
				}
				Expect(19);

#line  2273 "cs.ATG" 
				pexpr = new IndexerExpression(pexpr, indices); 
			}
		}
	}

	void AnonymousMethodExpr(
#line  2277 "cs.ATG" 
out Expression outExpr) {

#line  2279 "cs.ATG" 
		AnonymousMethodExpression expr = new AnonymousMethodExpression();
		expr.StartLocation = t.Location;
		Statement stmt;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		outExpr = expr;
		
		if (la.kind == 20) {
			lexer.NextToken();
			if (StartOf(10)) {
				FormalParameterList(
#line  2288 "cs.ATG" 
p);

#line  2288 "cs.ATG" 
				expr.Parameters = p; 
			}
			Expect(21);
		}

#line  2293 "cs.ATG" 
		if (compilationUnit != null) { 
		Block(
#line  2294 "cs.ATG" 
out stmt);

#line  2294 "cs.ATG" 
		expr.Body  = (BlockStatement)stmt; 

#line  2295 "cs.ATG" 
		} else { 
		Expect(16);

#line  2297 "cs.ATG" 
		lexer.SkipCurrentBlock(); 
		Expect(17);

#line  2299 "cs.ATG" 
		} 

#line  2300 "cs.ATG" 
		expr.EndLocation = t.Location; 
	}

	void TypeArgumentList(
#line  2473 "cs.ATG" 
out List<TypeReference> types, bool canBeUnbound) {

#line  2475 "cs.ATG" 
		types = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(23);
		if (
#line  2480 "cs.ATG" 
canBeUnbound && (la.kind == Tokens.GreaterThan || la.kind == Tokens.Comma)) {

#line  2481 "cs.ATG" 
			types.Add(TypeReference.Null); 
			while (la.kind == 14) {
				lexer.NextToken();

#line  2482 "cs.ATG" 
				types.Add(TypeReference.Null); 
			}
		} else if (StartOf(9)) {
			Type(
#line  2483 "cs.ATG" 
out type);

#line  2483 "cs.ATG" 
			types.Add(type); 
			while (la.kind == 14) {
				lexer.NextToken();
				Type(
#line  2484 "cs.ATG" 
out type);

#line  2484 "cs.ATG" 
				types.Add(type); 
			}
		} else SynErr(184);
		Expect(22);
	}

	void ConditionalAndExpr(
#line  2309 "cs.ATG" 
ref Expression outExpr) {

#line  2310 "cs.ATG" 
		Expression expr; 
		InclusiveOrExpr(
#line  2312 "cs.ATG" 
ref outExpr);
		while (la.kind == 25) {
			lexer.NextToken();
			UnaryExpr(
#line  2312 "cs.ATG" 
out expr);
			InclusiveOrExpr(
#line  2312 "cs.ATG" 
ref expr);

#line  2312 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalAnd, expr);  
		}
	}

	void InclusiveOrExpr(
#line  2315 "cs.ATG" 
ref Expression outExpr) {

#line  2316 "cs.ATG" 
		Expression expr; 
		ExclusiveOrExpr(
#line  2318 "cs.ATG" 
ref outExpr);
		while (la.kind == 29) {
			lexer.NextToken();
			UnaryExpr(
#line  2318 "cs.ATG" 
out expr);
			ExclusiveOrExpr(
#line  2318 "cs.ATG" 
ref expr);

#line  2318 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseOr, expr);  
		}
	}

	void ExclusiveOrExpr(
#line  2321 "cs.ATG" 
ref Expression outExpr) {

#line  2322 "cs.ATG" 
		Expression expr; 
		AndExpr(
#line  2324 "cs.ATG" 
ref outExpr);
		while (la.kind == 30) {
			lexer.NextToken();
			UnaryExpr(
#line  2324 "cs.ATG" 
out expr);
			AndExpr(
#line  2324 "cs.ATG" 
ref expr);

#line  2324 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.ExclusiveOr, expr);  
		}
	}

	void AndExpr(
#line  2327 "cs.ATG" 
ref Expression outExpr) {

#line  2328 "cs.ATG" 
		Expression expr; 
		EqualityExpr(
#line  2330 "cs.ATG" 
ref outExpr);
		while (la.kind == 28) {
			lexer.NextToken();
			UnaryExpr(
#line  2330 "cs.ATG" 
out expr);
			EqualityExpr(
#line  2330 "cs.ATG" 
ref expr);

#line  2330 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseAnd, expr);  
		}
	}

	void EqualityExpr(
#line  2333 "cs.ATG" 
ref Expression outExpr) {

#line  2335 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		RelationalExpr(
#line  2339 "cs.ATG" 
ref outExpr);
		while (la.kind == 33 || la.kind == 34) {
			if (la.kind == 34) {
				lexer.NextToken();

#line  2342 "cs.ATG" 
				op = BinaryOperatorType.InEquality; 
			} else {
				lexer.NextToken();

#line  2343 "cs.ATG" 
				op = BinaryOperatorType.Equality; 
			}
			UnaryExpr(
#line  2345 "cs.ATG" 
out expr);
			RelationalExpr(
#line  2345 "cs.ATG" 
ref expr);

#line  2345 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void RelationalExpr(
#line  2349 "cs.ATG" 
ref Expression outExpr) {

#line  2351 "cs.ATG" 
		TypeReference type;
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  2356 "cs.ATG" 
ref outExpr);
		while (StartOf(28)) {
			if (StartOf(29)) {
				if (la.kind == 23) {
					lexer.NextToken();

#line  2358 "cs.ATG" 
					op = BinaryOperatorType.LessThan; 
				} else if (la.kind == 22) {
					lexer.NextToken();

#line  2359 "cs.ATG" 
					op = BinaryOperatorType.GreaterThan; 
				} else if (la.kind == 36) {
					lexer.NextToken();

#line  2360 "cs.ATG" 
					op = BinaryOperatorType.LessThanOrEqual; 
				} else if (la.kind == 35) {
					lexer.NextToken();

#line  2361 "cs.ATG" 
					op = BinaryOperatorType.GreaterThanOrEqual; 
				} else SynErr(185);
				UnaryExpr(
#line  2363 "cs.ATG" 
out expr);
				ShiftExpr(
#line  2364 "cs.ATG" 
ref expr);

#line  2365 "cs.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
			} else {
				if (la.kind == 84) {
					lexer.NextToken();
					TypeWithRestriction(
#line  2368 "cs.ATG" 
out type, false, false);
					if (
#line  2369 "cs.ATG" 
la.kind == Tokens.Question && Tokens.CastFollower[Peek(1).kind] == false) {
						NullableQuestionMark(
#line  2370 "cs.ATG" 
ref type);
					}

#line  2371 "cs.ATG" 
					outExpr = new TypeOfIsExpression(outExpr, type); 
				} else if (la.kind == 49) {
					lexer.NextToken();
					TypeWithRestriction(
#line  2373 "cs.ATG" 
out type, false, false);
					if (
#line  2374 "cs.ATG" 
la.kind == Tokens.Question && Tokens.CastFollower[Peek(1).kind] == false) {
						NullableQuestionMark(
#line  2375 "cs.ATG" 
ref type);
					}

#line  2376 "cs.ATG" 
					outExpr = new CastExpression(type, outExpr, CastType.TryCast); 
				} else SynErr(186);
			}
		}
	}

	void ShiftExpr(
#line  2381 "cs.ATG" 
ref Expression outExpr) {

#line  2383 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		AdditiveExpr(
#line  2387 "cs.ATG" 
ref outExpr);
		while (la.kind == 37 || 
#line  2390 "cs.ATG" 
IsShiftRight()) {
			if (la.kind == 37) {
				lexer.NextToken();

#line  2389 "cs.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				Expect(22);
				Expect(22);

#line  2391 "cs.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			UnaryExpr(
#line  2394 "cs.ATG" 
out expr);
			AdditiveExpr(
#line  2394 "cs.ATG" 
ref expr);

#line  2394 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AdditiveExpr(
#line  2398 "cs.ATG" 
ref Expression outExpr) {

#line  2400 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		MultiplicativeExpr(
#line  2404 "cs.ATG" 
ref outExpr);
		while (la.kind == 4 || la.kind == 5) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  2407 "cs.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  2408 "cs.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			UnaryExpr(
#line  2410 "cs.ATG" 
out expr);
			MultiplicativeExpr(
#line  2410 "cs.ATG" 
ref expr);

#line  2410 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void MultiplicativeExpr(
#line  2414 "cs.ATG" 
ref Expression outExpr) {

#line  2416 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		while (la.kind == 6 || la.kind == 7 || la.kind == 8) {
			if (la.kind == 6) {
				lexer.NextToken();

#line  2422 "cs.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else if (la.kind == 7) {
				lexer.NextToken();

#line  2423 "cs.ATG" 
				op = BinaryOperatorType.Divide; 
			} else {
				lexer.NextToken();

#line  2424 "cs.ATG" 
				op = BinaryOperatorType.Modulus; 
			}
			UnaryExpr(
#line  2426 "cs.ATG" 
out expr);

#line  2426 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void TypeParameterConstraintsClauseBase(
#line  2530 "cs.ATG" 
out TypeReference type) {

#line  2531 "cs.ATG" 
		TypeReference t; type = null; 
		if (la.kind == 108) {
			lexer.NextToken();

#line  2533 "cs.ATG" 
			type = new TypeReference("struct"); 
		} else if (la.kind == 58) {
			lexer.NextToken();

#line  2534 "cs.ATG" 
			type = new TypeReference("struct"); 
		} else if (la.kind == 88) {
			lexer.NextToken();
			Expect(20);
			Expect(21);

#line  2535 "cs.ATG" 
			type = new TypeReference("struct"); 
		} else if (StartOf(9)) {
			Type(
#line  2536 "cs.ATG" 
out t);

#line  2536 "cs.ATG" 
			type = t; 
		} else SynErr(187);
	}


	public Parser(ILexer lexer) : base(lexer)
	{
	}
	
	public override void Parse()
	{
		CS();

	}
	
	protected void ExpectWeak(int n, int follow)
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
	
	protected bool WeakSeparator(int n, int syFol, int repFol)
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
	
	protected override void SynErr(int line, int col, int errorNumber)
	{
		errors.count++; 
		string s;
		switch (errorNumber) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "Literal expected"; break;
			case 3: s = "\"=\" expected"; break;
			case 4: s = "\"+\" expected"; break;
			case 5: s = "\"-\" expected"; break;
			case 6: s = "\"*\" expected"; break;
			case 7: s = "\"/\" expected"; break;
			case 8: s = "\"%\" expected"; break;
			case 9: s = "\":\" expected"; break;
			case 10: s = "\"::\" expected"; break;
			case 11: s = "\";\" expected"; break;
			case 12: s = "\"?\" expected"; break;
			case 13: s = "\"??\" expected"; break;
			case 14: s = "\",\" expected"; break;
			case 15: s = "\".\" expected"; break;
			case 16: s = "\"{\" expected"; break;
			case 17: s = "\"}\" expected"; break;
			case 18: s = "\"[\" expected"; break;
			case 19: s = "\"]\" expected"; break;
			case 20: s = "\"(\" expected"; break;
			case 21: s = "\")\" expected"; break;
			case 22: s = "\">\" expected"; break;
			case 23: s = "\"<\" expected"; break;
			case 24: s = "\"!\" expected"; break;
			case 25: s = "\"&&\" expected"; break;
			case 26: s = "\"||\" expected"; break;
			case 27: s = "\"~\" expected"; break;
			case 28: s = "\"&\" expected"; break;
			case 29: s = "\"|\" expected"; break;
			case 30: s = "\"^\" expected"; break;
			case 31: s = "\"++\" expected"; break;
			case 32: s = "\"--\" expected"; break;
			case 33: s = "\"==\" expected"; break;
			case 34: s = "\"!=\" expected"; break;
			case 35: s = "\">=\" expected"; break;
			case 36: s = "\"<=\" expected"; break;
			case 37: s = "\"<<\" expected"; break;
			case 38: s = "\"+=\" expected"; break;
			case 39: s = "\"-=\" expected"; break;
			case 40: s = "\"*=\" expected"; break;
			case 41: s = "\"/=\" expected"; break;
			case 42: s = "\"%=\" expected"; break;
			case 43: s = "\"&=\" expected"; break;
			case 44: s = "\"|=\" expected"; break;
			case 45: s = "\"^=\" expected"; break;
			case 46: s = "\"<<=\" expected"; break;
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
			case 127: s = "invalid NonArrayType"; break;
			case 128: s = "invalid AttributeArguments"; break;
			case 129: s = "invalid Expr"; break;
			case 130: s = "invalid TypeModifier"; break;
			case 131: s = "invalid TypeDecl"; break;
			case 132: s = "invalid TypeDecl"; break;
			case 133: s = "invalid IntegralType"; break;
			case 134: s = "invalid FormalParameterList"; break;
			case 135: s = "invalid FormalParameterList"; break;
			case 136: s = "invalid ClassType"; break;
			case 137: s = "invalid ClassMemberDecl"; break;
			case 138: s = "invalid ClassMemberDecl"; break;
			case 139: s = "invalid StructMemberDecl"; break;
			case 140: s = "invalid StructMemberDecl"; break;
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
			case 154: s = "invalid TypeWithRestriction"; break;
			case 155: s = "invalid TypeWithRestriction"; break;
			case 156: s = "invalid SimpleType"; break;
			case 157: s = "invalid AccessorModifiers"; break;
			case 158: s = "invalid EventAccessorDecls"; break;
			case 159: s = "invalid ConstructorInitializer"; break;
			case 160: s = "invalid OverloadableOperator"; break;
			case 161: s = "invalid AccessorDecls"; break;
			case 162: s = "invalid InterfaceAccessors"; break;
			case 163: s = "invalid InterfaceAccessors"; break;
			case 164: s = "invalid GetAccessorDecl"; break;
			case 165: s = "invalid SetAccessorDecl"; break;
			case 166: s = "invalid VariableInitializer"; break;
			case 167: s = "invalid Statement"; break;
			case 168: s = "invalid AssignmentOperator"; break;
			case 169: s = "invalid EmbeddedStatement"; break;
			case 170: s = "invalid EmbeddedStatement"; break;
			case 171: s = "invalid EmbeddedStatement"; break;
			case 172: s = "invalid ForInitializer"; break;
			case 173: s = "invalid GotoStatement"; break;
			case 174: s = "invalid TryStatement"; break;
			case 175: s = "invalid ResourceAcquisition"; break;
			case 176: s = "invalid SwitchLabel"; break;
			case 177: s = "invalid CatchClauses"; break;
			case 178: s = "invalid PrimaryExpr"; break;
			case 179: s = "invalid PrimaryExpr"; break;
			case 180: s = "invalid PrimaryExpr"; break;
			case 181: s = "invalid PrimaryExpr"; break;
			case 182: s = "invalid PrimaryExpr"; break;
			case 183: s = "invalid PrimaryExpr"; break;
			case 184: s = "invalid TypeArgumentList"; break;
			case 185: s = "invalid RelationalExpr"; break;
			case 186: s = "invalid RelationalExpr"; break;
			case 187: s = "invalid TypeParameterConstraintsClauseBase"; break;

			default: s = "error " + errorNumber; break;
		}
		errors.Error(line, col, s);
	}
	
	protected bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	static bool[,] set = {
	{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,x,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, T,T,T,T, T,T,x,T, T,T,T,x, x,T,x,T, x,T,T,T, x,T,T,x, T,T,T,x, x,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,T, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,x,x, x,x,T,x, x,x,x,T, x,T,T,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, T,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, T,T,x,T, T,T,x,T, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,x,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, T,x,x,x, x,x,x,T, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, T,T,x,x, T,T,x,T, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,T,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x}

	};
} // end Parser

}