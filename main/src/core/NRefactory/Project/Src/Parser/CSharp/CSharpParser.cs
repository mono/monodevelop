// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 2638 $</version>
// </file>

using System;
using System.Collections.Generic;
using System.Text;

using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Parser.CSharp
{
	internal sealed partial class Parser : AbstractParser
	{
		Lexer lexer;
		
		public Parser(ILexer lexer) : base(lexer)
		{
			this.lexer = (Lexer)lexer;
		}
		
		StringBuilder qualidentBuilder = new StringBuilder();

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
			if (errDist >= MinErrDist) {
				this.Errors.Error(la.line, la.col, s);
			}
			errDist = 0;
		}

		public override Expression ParseExpression()
		{
			lexer.NextToken();
			Expression expr;
			Expr(out expr);
			// SEMICOLON HACK : without a trailing semicolon, parsing expressions does not work correctly
			if (la.kind == Tokens.Semicolon) lexer.NextToken();
			Expect(Tokens.EOF);
			return expr;
		}
		
		public override BlockStatement ParseBlock()
		{
			lexer.NextToken();
			compilationUnit = new CompilationUnit();
			
			BlockStatement blockStmt = new BlockStatement();
			blockStmt.StartLocation = la.Location;
			compilationUnit.BlockStart(blockStmt);
			
			while (la.kind != Tokens.EOF) {
				Token oldLa = la;
				Statement();
				if (la == oldLa) {
					// did not advance lexer position, we cannot parse this as a statement block
					return null;
				}
			}
			
			compilationUnit.BlockEnd();
			Expect(Tokens.EOF);
			return blockStmt;
		}
		
		public override List<INode> ParseTypeMembers()
		{
			lexer.NextToken();
			compilationUnit = new CompilationUnit();
			
			TypeDeclaration newType = new TypeDeclaration(Modifiers.None, null);
			compilationUnit.BlockStart(newType);
			ClassBody();
			compilationUnit.BlockEnd();
			Expect(Tokens.EOF);
			return newType.Children;
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
				return IsPointerOrDims(ref pt) && SkipQuestionMark(ref pt);
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

		// TypeName = ident [ "::" ident ] { ["<" TypeNameOrKW { "," TypeNameOrKW } ">" ] "." ident } ["?"] PointerOrDims
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
			// { ["<" TypeNameOrKW { "," TypeNameOrKW } ">" ] "." ident }
			while (true) {
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
				if (pt.kind != Tokens.Dot)
					break;
				pt = Peek();
				if (pt.kind != Tokens.Identifier) {
					return false;
				}
				pt = Peek();
			}
			// ["?"]
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

		/* True, if ident is followed by ",", "=", "[" or ";" */
		bool IsVarDecl () {
			int peek = Peek(1).kind;
			return la.kind == Tokens.Identifier &&
				(peek == Tokens.Comma || peek == Tokens.Assign || peek == Tokens.Semicolon || peek == Tokens.OpenSquareBracket);
		}

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
				do {
					pt = Peek();
					if (!IsTypeNameOrKWForTypeCast(ref pt)) return false;
				} while (pt.kind == Tokens.Comma);
				if (pt.kind != Tokens.GreaterThan) return false;
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
			Token pt = la;
			return IsTypeNameOrKWForTypeCast(ref pt) && pt.kind == Tokens.Identifier;
		}

		bool IsTokenWithGenericParameters ()
		{
			StartPeek();
			Token t = Peek (1);
			if (t.kind != Tokens.LessThan) 
				return false;
			return SkipGeneric(ref t);
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
		
		bool IsMostNegativeIntegerWithoutTypeSuffix()
		{
			Token token = la;
			if (token.kind == Tokens.Literal) {
				return token.val == "2147483648" || token.val == "9223372036854775808";
			} else {
				return false;
			}
		}
		
		bool LastExpressionIsUnaryMinus(System.Collections.ArrayList expressions)
		{
			if (expressions.Count == 0) return false;
			UnaryOperatorExpression uoe = expressions[expressions.Count - 1] as UnaryOperatorExpression;
			if (uoe != null) {
				return uoe.Op == UnaryOperatorType.Minus;
			} else {
				return false;
			}
		}
	}
}
