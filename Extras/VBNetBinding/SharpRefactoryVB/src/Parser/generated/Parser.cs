
#line  1 "VBNET.ATG" 
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using ICSharpCode.SharpRefactory.Parser.AST.VB;
using ICSharpCode.SharpRefactory.Parser.VB;
using System;
using System.Reflection;

namespace ICSharpCode.SharpRefactory.Parser.VB {



public class Parser
{
	const int maxT = 188;

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


#line  10 "VBNET.ATG" 
private string assemblyName = null;
public CompilationUnit compilationUnit;
private ArrayList importedNamespaces = null;
private Stack withStatements;
private bool isLabel = false;
private LabelStatement labelStatement = null;

public string ContainingAssembly
{
	set { assemblyName = value; }
}

Token t
{
	get {
		return lexer.Token;
	}
}
Token la
{
	get {
		return lexer.LookAhead;
	}
}

void updateLabelStatement(Statement stmt)
{
	if(isLabel) {
		labelStatement.EmbeddedStatement = stmt;
		isLabel = false;
	} else {
		compilationUnit.AddChild(stmt);
	}
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

bool IsEndStmtAhead()
{
	int peek = Peek(1).kind;
	return la.kind == Tokens.End && (peek == Tokens.EOL || peek == Tokens.Colon);
}

bool IsNotClosingParenthesis() {
	return la.kind != Tokens.CloseParenthesis;
}

/*
	True, if ident is followed by "="
*/
bool IdentAndAsgn () {
	if(la.kind == Tokens.Identifier) {
		if(Peek(1).kind == Tokens.Assign) return true;
		if(Peek(1).kind == Tokens.Colon && Peek(2).kind == Tokens.Assign) return true;
	}
	return false;
}

/*
	True, if ident is followed by "=" or by ":" and "="
*/
bool IsNamedAssign() {
//	if(Peek(1).kind == Tokens.Assign) return true; // removed: not in the lang spec
	if(Peek(1).kind == Tokens.Colon && Peek(2).kind == Tokens.Assign) return true;
	return false;
}

bool IsObjectCreation() {
	return la.kind == Tokens.As && Peek(1).kind == Tokens.New;
}

/*
	True, if "<" is followed by the ident "assembly" or "module"
*/
bool IsGlobalAttrTarget () {
	Token pt = Peek(1);
	return la.kind == Tokens.LessThan && ( pt.val.ToLower() == "assembly" || pt.val.ToLower() == "module");
}

/*
	True if the next token is a "(" and is followed by "," or ")"
*/
bool IsRank()
{
	int peek = Peek(1).kind;
	return la.kind == Tokens.OpenParenthesis
						&& (peek == Tokens.Comma || peek == Tokens.CloseParenthesis);
}

bool IsDims()
{
	int peek = Peek(1).kind;
	int peek_n = Peek(2).kind;
	return la.kind == Tokens.OpenParenthesis
						&& (peek == Tokens.LiteralInteger && peek_n == Tokens.CloseParenthesis);
}

bool IsSize()
{
	return la.kind == Tokens.OpenParenthesis;
}

/*
	True, if the comma is not a trailing one,
	like the last one in: a, b, c,
*/
bool NotFinalComma() {
	int peek = Peek(1).kind;
	return la.kind == Tokens.Comma &&
		   peek != Tokens.CloseCurlyBrace && peek != Tokens.CloseSquareBracket;
}

/*
	True, if the next token is "Else" and this one
	if followed by "If"
*/
bool IsElseIf()
{
	int peek = Peek(1).kind;
	return la.kind == Tokens.Else && peek == Tokens.If;
}

/*
	True if the next token is goto and this one is
	followed by minus ("-") (this is allowd in in
	error clauses)
*/
bool IsNegativeLabelName()
{
	int peek = Peek(1).kind;
	return la.kind == Tokens.GoTo && peek == Tokens.Minus;
}

/*
	True if the next statement is a "Resume next" statement
*/
bool IsResumeNext()
{
	int peek = Peek(1).kind;
	return la.kind == Tokens.Resume && peek == Tokens.Next;
}

/*
	True, if ident/literal integer is followed by ":"
*/
bool IsLabel()
{
	return (la.kind == Tokens.Identifier || la.kind == Tokens.LiteralInteger)
			&& Peek(1).kind == Tokens.Colon;
}

bool IsNotStatementSeparator()
{
	return la.kind == Tokens.Colon && Peek(1).kind == Tokens.EOL;
}

bool IsAssignment ()
{
	return IdentAndAsgn();
}

bool IsMustOverride(Modifiers m)
{
	return m.Contains(Modifier.MustOverride);
}

/*
	True, if lookahead is a local attribute target specifier,
	i.e. one of "event", "return", "field", "method",
	"module", "param", "property", or "type"
*/
bool IsLocalAttrTarget() {
	// TODO
	return false;
}



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
	
	void VBNET() {

#line  431 "VBNET.ATG" 
		compilationUnit = new CompilationUnit();
		withStatements = new Stack();
		
		while (la.kind == 1) {
			lexer.NextToken();
		}
		while (la.kind == 137) {
			OptionStmt();
		}
		while (la.kind == 109) {
			ImportsStmt();
		}
		while (
#line  437 "VBNET.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void OptionStmt() {

#line  442 "VBNET.ATG" 
		INode node = null; bool val = true; 
		Expect(137);

#line  443 "VBNET.ATG" 
		Point startPos = t.Location; 
		if (la.kind == 96) {
			lexer.NextToken();
			if (la.kind == 135 || la.kind == 136) {
				OptionValue(
#line  445 "VBNET.ATG" 
ref val);
			}

#line  446 "VBNET.ATG" 
			node = new OptionExplicitDeclaration(val); 
		} else if (la.kind == 166) {
			lexer.NextToken();
			if (la.kind == 135 || la.kind == 136) {
				OptionValue(
#line  448 "VBNET.ATG" 
ref val);
			}

#line  449 "VBNET.ATG" 
			node = new OptionStrictDeclaration(val); 
		} else if (la.kind == 71) {
			lexer.NextToken();
			if (la.kind == 52) {
				lexer.NextToken();

#line  451 "VBNET.ATG" 
				node = new OptionCompareDeclaration(CompareType.Binary); 
			} else if (la.kind == 171) {
				lexer.NextToken();

#line  452 "VBNET.ATG" 
				node = new OptionCompareDeclaration(CompareType.Text); 
			} else SynErr(189);
		} else SynErr(190);
		EndOfStmt();

#line  457 "VBNET.ATG" 
		node.StartLocation = startPos;
		node.EndLocation   = t.Location;
		compilationUnit.AddChild(node);
		
	}

	void ImportsStmt() {

#line  481 "VBNET.ATG" 
		ArrayList importClauses = new ArrayList();
		importedNamespaces = new ArrayList();
		object importClause;
		
		Expect(109);

#line  487 "VBNET.ATG" 
		Point startPos = t.Location;
		ImportsStatement importsStatement = new ImportsStatement(null);
		
		ImportClause(
#line  490 "VBNET.ATG" 
out importClause);

#line  490 "VBNET.ATG" 
		importClauses.Add(importClause); 
		while (la.kind == 12) {
			lexer.NextToken();
			ImportClause(
#line  492 "VBNET.ATG" 
out importClause);

#line  492 "VBNET.ATG" 
			importClauses.Add(importClause); 
		}
		EndOfStmt();

#line  496 "VBNET.ATG" 
		importsStatement.ImportClauses = importClauses;
		importsStatement.StartLocation = startPos;
		importsStatement.EndLocation   = t.Location;
		compilationUnit.AddChild(importsStatement);
		
	}

	void GlobalAttributeSection() {

#line  1840 "VBNET.ATG" 
		Point startPos = t.Location; 
		Expect(28);
		if (la.kind == 50) {
			lexer.NextToken();
		} else if (la.kind == 122) {
			lexer.NextToken();
		} else SynErr(191);

#line  1843 "VBNET.ATG" 
		string attributeTarget = t.val.ToLower();
		ArrayList attributes = new ArrayList();
		ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attribute;
		
		Expect(13);
		Attribute(
#line  1847 "VBNET.ATG" 
out attribute);

#line  1847 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  1848 "VBNET.ATG" 
NotFinalComma()) {
			Expect(12);
			Attribute(
#line  1848 "VBNET.ATG" 
out attribute);

#line  1848 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(27);
		EndOfStmt();

#line  1853 "VBNET.ATG" 
		AttributeSection section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  526 "VBNET.ATG" 
		Modifiers m = new Modifiers(this);
		AttributeSection section;
		ArrayList attributes = new ArrayList();
		string qualident;
		
		if (la.kind == 127) {
			lexer.NextToken();

#line  533 "VBNET.ATG" 
			Point startPos = t.Location;
			
			Qualident(
#line  535 "VBNET.ATG" 
out qualident);

#line  537 "VBNET.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			compilationUnit.AddChild(node);
			compilationUnit.BlockStart(node);
			
			Expect(1);
			NamespaceBody();

#line  545 "VBNET.ATG" 
			node.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 28) {
				AttributeSection(
#line  549 "VBNET.ATG" 
out section);

#line  549 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  550 "VBNET.ATG" 
m);
			}
			NonModuleDeclaration(
#line  550 "VBNET.ATG" 
m, attributes);
		} else SynErr(192);
	}

	void OptionValue(
#line  463 "VBNET.ATG" 
ref bool val) {
		if (la.kind == 136) {
			lexer.NextToken();

#line  465 "VBNET.ATG" 
			val = true; 
		} else if (la.kind == 135) {
			lexer.NextToken();

#line  467 "VBNET.ATG" 
			val = true; 
		} else SynErr(193);
	}

	void EndOfStmt() {
		if (la.kind == 1) {
			lexer.NextToken();
		} else if (la.kind == 13) {
			lexer.NextToken();
		} else SynErr(194);
	}

	void ImportClause(
#line  503 "VBNET.ATG" 
out object importClause) {

#line  505 "VBNET.ATG" 
		string qualident = null;
		string aliasident = null;
		importClause = null;
		
		if (
#line  509 "VBNET.ATG" 
IsAssignment()) {
			Identifier();

#line  509 "VBNET.ATG" 
			aliasident = t.val;  
			Expect(11);
		}
		Qualident(
#line  510 "VBNET.ATG" 
out qualident);

#line  512 "VBNET.ATG" 
		if (qualident != null && qualident.Length > 0) {
		if (aliasident != null) {
			importClause = new ImportsAliasDeclaration(aliasident, qualident);
		} else {
			importedNamespaces.Add(qualident);
			importClause = new ImportsDeclaration(qualident);
		}
		}
		
	}

	void Identifier() {
		if (la.kind == 2) {
			lexer.NextToken();
		} else if (la.kind == 171) {
			lexer.NextToken();
		} else if (la.kind == 52) {
			lexer.NextToken();
		} else if (la.kind == 71) {
			lexer.NextToken();
		} else SynErr(195);
	}

	void Qualident(
#line  2527 "VBNET.ATG" 
out string qualident) {

#line  2528 "VBNET.ATG" 
		string name = String.Empty; 
		Identifier();

#line  2529 "VBNET.ATG" 
		StringBuilder qualidentBuilder = new StringBuilder(t.val); 
		while (la.kind == 10) {
			lexer.NextToken();
			IdentifierOrKeyword(
#line  2531 "VBNET.ATG" 
out name);

#line  2531 "VBNET.ATG" 
			qualidentBuilder.Append('.');
			  qualidentBuilder.Append(name); 
			
		}

#line  2535 "VBNET.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void NamespaceBody() {
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(89);
		Expect(127);
		Expect(1);
	}

	void AttributeSection(
#line  1911 "VBNET.ATG" 
out AttributeSection section) {

#line  1913 "VBNET.ATG" 
		string attributeTarget = "";
		ArrayList attributes = new ArrayList();
		ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attribute;
		
		
		Expect(28);

#line  1918 "VBNET.ATG" 
		Point startPos = t.Location; 
		if (
#line  1919 "VBNET.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 94) {
				lexer.NextToken();

#line  1920 "VBNET.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 156) {
				lexer.NextToken();

#line  1921 "VBNET.ATG" 
				attributeTarget = "return";
			} else {
				Identifier();

#line  1924 "VBNET.ATG" 
				string val = t.val.ToLower();
				if (val != "field"	|| val != "method" ||
					val != "module" || val != "param"  ||
					val != "property" || val != "type")
				Error("attribute target specifier (event, return, field," +
						"method, module, param, property, or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(13);
		}
		Attribute(
#line  1934 "VBNET.ATG" 
out attribute);

#line  1934 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  1935 "VBNET.ATG" 
NotFinalComma()) {
			Expect(12);
			Attribute(
#line  1935 "VBNET.ATG" 
out attribute);

#line  1935 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(27);

#line  1939 "VBNET.ATG" 
		section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		
	}

	void TypeModifier(
#line  2715 "VBNET.ATG" 
Modifiers m) {
		switch (la.kind) {
		case 150: {
			lexer.NextToken();

#line  2716 "VBNET.ATG" 
			m.Add(Modifier.Public); 
			break;
		}
		case 149: {
			lexer.NextToken();

#line  2717 "VBNET.ATG" 
			m.Add(Modifier.Protected); 
			break;
		}
		case 100: {
			lexer.NextToken();

#line  2718 "VBNET.ATG" 
			m.Add(Modifier.Friend); 
			break;
		}
		case 147: {
			lexer.NextToken();

#line  2719 "VBNET.ATG" 
			m.Add(Modifier.Private); 
			break;
		}
		case 160: {
			lexer.NextToken();

#line  2720 "VBNET.ATG" 
			m.Add(Modifier.Shared); 
			break;
		}
		case 159: {
			lexer.NextToken();

#line  2721 "VBNET.ATG" 
			m.Add(Modifier.Shadows); 
			break;
		}
		case 123: {
			lexer.NextToken();

#line  2722 "VBNET.ATG" 
			m.Add(Modifier.MustInherit); 
			break;
		}
		case 132: {
			lexer.NextToken();

#line  2723 "VBNET.ATG" 
			m.Add(Modifier.NotInheritable); 
			break;
		}
		default: SynErr(196); break;
		}
	}

	void NonModuleDeclaration(
#line  554 "VBNET.ATG" 
Modifiers m, ArrayList attributes) {

#line  556 "VBNET.ATG" 
		string name = String.Empty;
		ArrayList names = null;
		
		switch (la.kind) {
		case 68: {

#line  559 "VBNET.ATG" 
			m.Check(Modifier.Classes); 
			lexer.NextToken();

#line  562 "VBNET.ATG" 
			TypeDeclaration newType = new TypeDeclaration();
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			newType.Type = Types.Class;
			newType.Modifier = m.Modifier;
			newType.Attributes = attributes;
			
			Identifier();

#line  570 "VBNET.ATG" 
			newType.Name = t.val; newType.StartLocation = t.EndLocation; 
			EndOfStmt();
			if (la.kind == 111) {
				ClassBaseType(
#line  572 "VBNET.ATG" 
out name);

#line  572 "VBNET.ATG" 
				newType.BaseType = name; 
			}
			while (la.kind == 108) {
				TypeImplementsClause(
#line  573 "VBNET.ATG" 
out names);

#line  573 "VBNET.ATG" 
				newType.BaseInterfaces = names; 
			}
			ClassBody(
#line  574 "VBNET.ATG" 
newType);

#line  576 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 122: {
			lexer.NextToken();

#line  580 "VBNET.ATG" 
			m.Check(Modifier.Modules);
			TypeDeclaration newType = new TypeDeclaration();
			newType.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = t.Location;
			newType.Type = Types.Module;
			newType.Modifier = m.Modifier;
			newType.Attributes = attributes;
			
			Identifier();

#line  590 "VBNET.ATG" 
			newType.Name = t.val; newType.StartLocation = t.EndLocation;  
			Expect(1);
			ModuleBody(
#line  592 "VBNET.ATG" 
newType);

#line  594 "VBNET.ATG" 
			newType.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
			break;
		}
		case 168: {
			lexer.NextToken();

#line  599 "VBNET.ATG" 
			m.Check(Modifier.Structures);
			TypeDeclaration newType = new TypeDeclaration();
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			newType.StartLocation = t.Location;
			newType.Type = Types.Structure;
			newType.Modifier = m.Modifier;
			newType.Attributes = attributes;
			ArrayList baseInterfaces = new ArrayList();
			
			Identifier();

#line  610 "VBNET.ATG" 
			newType.Name = t.val; newType.StartLocation = t.EndLocation; 
			Expect(1);
			while (la.kind == 108) {
				TypeImplementsClause(
#line  611 "VBNET.ATG" 
out baseInterfaces);
			}
			StructureBody(
#line  612 "VBNET.ATG" 
newType);

#line  614 "VBNET.ATG" 
			newType.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
			break;
		}
		case 91: {
			lexer.NextToken();

#line  620 "VBNET.ATG" 
			m.Check(Modifier.Enums);
			TypeDeclaration newType = new TypeDeclaration();
			newType.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			
			newType.Type = Types.Enum;
			newType.Modifier = m.Modifier;
			newType.Attributes = attributes;
			
			Identifier();

#line  631 "VBNET.ATG" 
			newType.Name = t.val; newType.StartLocation = t.EndLocation; 
			if (la.kind == 49) {
				lexer.NextToken();
				PrimitiveTypeName(
#line  632 "VBNET.ATG" 
out name);

#line  632 "VBNET.ATG" 
				newType.BaseType = name; 
			}
			Expect(1);
			EnumBody(
#line  634 "VBNET.ATG" 
newType);

#line  636 "VBNET.ATG" 
			newType.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
			break;
		}
		case 113: {
			lexer.NextToken();

#line  642 "VBNET.ATG" 
			m.Check(Modifier.Interfaces);
			TypeDeclaration newType = new TypeDeclaration();
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			
			newType.Type = Types.Interface;
			newType.Modifier = m.Modifier;
			newType.Attributes = attributes;
			ArrayList baseInterfaces = new ArrayList();
			
			Identifier();

#line  653 "VBNET.ATG" 
			newType.Name = t.val; newType.StartLocation = t.EndLocation; 
			EndOfStmt();
			while (la.kind == 111) {
				InterfaceBase(
#line  654 "VBNET.ATG" 
out baseInterfaces);

#line  654 "VBNET.ATG" 
				newType.BaseInterfaces = baseInterfaces; 
			}
			InterfaceBody(
#line  655 "VBNET.ATG" 
newType);

#line  657 "VBNET.ATG" 
			newType.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
			break;
		}
		case 81: {
			lexer.NextToken();

#line  663 "VBNET.ATG" 
			m.Check(Modifier.Delegates);
			DelegateDeclaration delegateDeclr = new DelegateDeclaration();
			ArrayList p = null;
			TypeReference type = null;
			delegateDeclr.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			delegateDeclr.StartLocation = t.Location;
			delegateDeclr.Modifier = m.Modifier;
			delegateDeclr.Attributes = attributes;
			
			if (la.kind == 169) {
				lexer.NextToken();
				Identifier();

#line  673 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  674 "VBNET.ATG" 
out p);
					}
					Expect(26);

#line  674 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
			} else if (la.kind == 101) {
				lexer.NextToken();
				Identifier();

#line  676 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  677 "VBNET.ATG" 
out p);
					}
					Expect(26);

#line  677 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
				if (la.kind == 49) {
					lexer.NextToken();
					TypeName(
#line  678 "VBNET.ATG" 
out type);

#line  678 "VBNET.ATG" 
					delegateDeclr.ReturnType = type; 
				}
			} else SynErr(197);

#line  680 "VBNET.ATG" 
			delegateDeclr.EndLocation = t.EndLocation; 
			Expect(1);

#line  683 "VBNET.ATG" 
			compilationUnit.AddChild(delegateDeclr);
			
			break;
		}
		default: SynErr(198); break;
		}
	}

	void ClassBaseType(
#line  873 "VBNET.ATG" 
out string name) {

#line  875 "VBNET.ATG" 
		TypeReference type;
		name = String.Empty;
		
		Expect(111);
		TypeName(
#line  879 "VBNET.ATG" 
out type);

#line  879 "VBNET.ATG" 
		name = type.Type; 
		EndOfStmt();
	}

	void TypeImplementsClause(
#line  1408 "VBNET.ATG" 
out ArrayList baseInterfaces) {

#line  1410 "VBNET.ATG" 
		baseInterfaces = new ArrayList();
		TypeReference type = null;
		
		Expect(108);
		TypeName(
#line  1413 "VBNET.ATG" 
out type);

#line  1415 "VBNET.ATG" 
		baseInterfaces.Add(type);
		
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  1418 "VBNET.ATG" 
out type);

#line  1419 "VBNET.ATG" 
			baseInterfaces.Add(type); 
		}
		EndOfStmt();
	}

	void ClassBody(
#line  693 "VBNET.ATG" 
TypeDeclaration newType) {

#line  694 "VBNET.ATG" 
		AttributeSection section; 
		while (StartOf(5)) {

#line  697 "VBNET.ATG" 
			ArrayList attributes = new ArrayList();
			Modifiers m = new Modifiers(this);
			
			while (la.kind == 28) {
				AttributeSection(
#line  700 "VBNET.ATG" 
out section);

#line  700 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(6)) {
				MemberModifier(
#line  701 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  702 "VBNET.ATG" 
m, attributes);
		}
		Expect(89);
		Expect(68);

#line  704 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void ModuleBody(
#line  724 "VBNET.ATG" 
TypeDeclaration newType) {

#line  725 "VBNET.ATG" 
		AttributeSection section; 
		while (StartOf(5)) {

#line  728 "VBNET.ATG" 
			ArrayList attributes = new ArrayList();
			Modifiers m = new Modifiers(this);
			
			while (la.kind == 28) {
				AttributeSection(
#line  731 "VBNET.ATG" 
out section);

#line  731 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(6)) {
				MemberModifier(
#line  732 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  733 "VBNET.ATG" 
m, attributes);
		}
		Expect(89);
		Expect(122);

#line  735 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void StructureBody(
#line  708 "VBNET.ATG" 
TypeDeclaration newType) {

#line  709 "VBNET.ATG" 
		AttributeSection section; 
		while (StartOf(5)) {

#line  712 "VBNET.ATG" 
			ArrayList attributes = new ArrayList();
			Modifiers m = new Modifiers(this);
			
			while (la.kind == 28) {
				AttributeSection(
#line  715 "VBNET.ATG" 
out section);

#line  715 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(6)) {
				MemberModifier(
#line  716 "VBNET.ATG" 
m);
			}
			StructureMemberDecl(
#line  717 "VBNET.ATG" 
m, attributes);
		}
		Expect(89);
		Expect(168);

#line  719 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void PrimitiveTypeName(
#line  2693 "VBNET.ATG" 
out string type) {

#line  2694 "VBNET.ATG" 
		type = String.Empty; 
		switch (la.kind) {
		case 53: {
			lexer.NextToken();

#line  2695 "VBNET.ATG" 
			type = "Boolean"; 
			break;
		}
		case 77: {
			lexer.NextToken();

#line  2696 "VBNET.ATG" 
			type = "Date"; 
			break;
		}
		case 66: {
			lexer.NextToken();

#line  2697 "VBNET.ATG" 
			type = "Char"; 
			break;
		}
		case 167: {
			lexer.NextToken();

#line  2698 "VBNET.ATG" 
			type = "String"; 
			break;
		}
		case 78: {
			lexer.NextToken();

#line  2699 "VBNET.ATG" 
			type = "Decimal"; 
			break;
		}
		case 55: {
			lexer.NextToken();

#line  2700 "VBNET.ATG" 
			type = "Byte"; 
			break;
		}
		case 161: {
			lexer.NextToken();

#line  2701 "VBNET.ATG" 
			type = "Short"; 
			break;
		}
		case 112: {
			lexer.NextToken();

#line  2702 "VBNET.ATG" 
			type = "Integer"; 
			break;
		}
		case 118: {
			lexer.NextToken();

#line  2703 "VBNET.ATG" 
			type = "Long"; 
			break;
		}
		case 162: {
			lexer.NextToken();

#line  2704 "VBNET.ATG" 
			type = "Single"; 
			break;
		}
		case 85: {
			lexer.NextToken();

#line  2705 "VBNET.ATG" 
			type = "Double"; 
			break;
		}
		default: SynErr(199); break;
		}
	}

	void EnumBody(
#line  739 "VBNET.ATG" 
TypeDeclaration newType) {

#line  740 "VBNET.ATG" 
		FieldDeclaration f; 
		while (StartOf(7)) {
			EnumMemberDecl(
#line  742 "VBNET.ATG" 
out f);

#line  742 "VBNET.ATG" 
			compilationUnit.AddChild(f); 
		}
		Expect(89);
		Expect(91);

#line  744 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void InterfaceBase(
#line  1393 "VBNET.ATG" 
out ArrayList bases) {

#line  1395 "VBNET.ATG" 
		TypeReference type;
		bases = new ArrayList();
		
		Expect(111);
		TypeName(
#line  1399 "VBNET.ATG" 
out type);

#line  1399 "VBNET.ATG" 
		bases.Add(type); 
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  1402 "VBNET.ATG" 
out type);

#line  1402 "VBNET.ATG" 
			bases.Add(type); 
		}
		Expect(1);
	}

	void InterfaceBody(
#line  748 "VBNET.ATG" 
TypeDeclaration newType) {
		while (StartOf(8)) {
			InterfaceMemberDecl();
		}
		Expect(89);
		Expect(113);

#line  750 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		Expect(1);
	}

	void FormalParameterList(
#line  1946 "VBNET.ATG" 
out ArrayList parameter) {

#line  1948 "VBNET.ATG" 
		parameter = new ArrayList();
		ParameterDeclarationExpression p;
		AttributeSection section;
		ArrayList attributes = new ArrayList();
		
		while (la.kind == 28) {
			AttributeSection(
#line  1953 "VBNET.ATG" 
out section);

#line  1953 "VBNET.ATG" 
			attributes.Add(section); 
		}
		FormalParameter(
#line  1955 "VBNET.ATG" 
out p);

#line  1957 "VBNET.ATG" 
		bool paramsFound = false;
		p.Attributes = attributes;
		parameter.Add(p);
		
		while (la.kind == 12) {
			lexer.NextToken();

#line  1962 "VBNET.ATG" 
			attributes = new ArrayList(); if (paramsFound) Error("params array must be at end of parameter list"); 
			while (la.kind == 28) {
				AttributeSection(
#line  1963 "VBNET.ATG" 
out section);

#line  1963 "VBNET.ATG" 
				attributes.Add(section); 
			}
			FormalParameter(
#line  1965 "VBNET.ATG" 
out p);

#line  1965 "VBNET.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		}
	}

	void TypeName(
#line  1758 "VBNET.ATG" 
out TypeReference typeref) {

#line  1760 "VBNET.ATG" 
		ArrayList rank = null;
		
		NonArrayTypeName(
#line  1762 "VBNET.ATG" 
out typeref);
		ArrayTypeModifiers(
#line  1763 "VBNET.ATG" 
out rank);

#line  1765 "VBNET.ATG" 
		typeref = new TypeReference(typeref == null ? "UNKNOWN" : typeref.Type, rank);
		
	}

	void MemberModifier(
#line  2726 "VBNET.ATG" 
Modifiers m) {
		switch (la.kind) {
		case 123: {
			lexer.NextToken();

#line  2727 "VBNET.ATG" 
			m.Add(Modifier.MustInherit);
			break;
		}
		case 80: {
			lexer.NextToken();

#line  2728 "VBNET.ATG" 
			m.Add(Modifier.Default);
			break;
		}
		case 100: {
			lexer.NextToken();

#line  2729 "VBNET.ATG" 
			m.Add(Modifier.Friend);
			break;
		}
		case 159: {
			lexer.NextToken();

#line  2730 "VBNET.ATG" 
			m.Add(Modifier.Shadows);
			break;
		}
		case 144: {
			lexer.NextToken();

#line  2731 "VBNET.ATG" 
			m.Add(Modifier.Overrides);
			break;
		}
		case 124: {
			lexer.NextToken();

#line  2732 "VBNET.ATG" 
			m.Add(Modifier.MustOverride);
			break;
		}
		case 147: {
			lexer.NextToken();

#line  2733 "VBNET.ATG" 
			m.Add(Modifier.Private);
			break;
		}
		case 149: {
			lexer.NextToken();

#line  2734 "VBNET.ATG" 
			m.Add(Modifier.Protected);
			break;
		}
		case 150: {
			lexer.NextToken();

#line  2735 "VBNET.ATG" 
			m.Add(Modifier.Public);
			break;
		}
		case 132: {
			lexer.NextToken();

#line  2736 "VBNET.ATG" 
			m.Add(Modifier.NotInheritable);
			break;
		}
		case 133: {
			lexer.NextToken();

#line  2737 "VBNET.ATG" 
			m.Add(Modifier.NotOverridable);
			break;
		}
		case 160: {
			lexer.NextToken();

#line  2738 "VBNET.ATG" 
			m.Add(Modifier.Shared);
			break;
		}
		case 142: {
			lexer.NextToken();

#line  2739 "VBNET.ATG" 
			m.Add(Modifier.Overridable);
			break;
		}
		case 141: {
			lexer.NextToken();

#line  2740 "VBNET.ATG" 
			m.Add(Modifier.Overloads);
			break;
		}
		case 152: {
			lexer.NextToken();

#line  2741 "VBNET.ATG" 
			m.Add(Modifier.ReadOnly);
			break;
		}
		case 186: {
			lexer.NextToken();

#line  2742 "VBNET.ATG" 
			m.Add(Modifier.WriteOnly);
			break;
		}
		case 185: {
			lexer.NextToken();

#line  2743 "VBNET.ATG" 
			m.Add(Modifier.WithEvents);
			break;
		}
		case 82: {
			lexer.NextToken();

#line  2744 "VBNET.ATG" 
			m.Add(Modifier.Dim);
			break;
		}
		default: SynErr(200); break;
		}
	}

	void ClassMemberDecl(
#line  869 "VBNET.ATG" 
Modifiers m, ArrayList attributes) {
		StructureMemberDecl(
#line  870 "VBNET.ATG" 
m, attributes);
	}

	void StructureMemberDecl(
#line  884 "VBNET.ATG" 
Modifiers m, ArrayList attributes) {

#line  886 "VBNET.ATG" 
		TypeReference type = null;
		ArrayList p = null;
		Statement stmt = null;
		ArrayList variableDeclarators = new ArrayList();
		
		switch (la.kind) {
		case 68: case 81: case 91: case 113: case 122: case 168: {
			NonModuleDeclaration(
#line  891 "VBNET.ATG" 
m, attributes);
			break;
		}
		case 169: {
			lexer.NextToken();

#line  895 "VBNET.ATG" 
			Point startPos = t.Location;
			ArrayList comments = lexer.SpecialTracker.RetreiveComments();
			
			if (StartOf(9)) {

#line  900 "VBNET.ATG" 
				string name = String.Empty;
				MethodDeclaration methodDeclaration;
				HandlesClause handlesClause = null;
				ImplementsClause implementsClause = null;
				
				Identifier();

#line  907 "VBNET.ATG" 
				name = t.val;
				m.Check(Modifier.Methods);
				
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  910 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				if (la.kind == 106 || la.kind == 108) {
					if (la.kind == 108) {
						ImplementsClause(
#line  913 "VBNET.ATG" 
out implementsClause);
					} else {
						HandlesClause(
#line  915 "VBNET.ATG" 
out handlesClause);
					}
				}

#line  918 "VBNET.ATG" 
				Point endLocation = t.EndLocation; 
				Expect(1);
				if (
#line  922 "VBNET.ATG" 
IsMustOverride(m)) {

#line  924 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration(name, m.Modifier,  null, p, attributes);
					methodDeclaration.Specials["before"] = comments;
					methodDeclaration.StartLocation = startPos;
					methodDeclaration.EndLocation   = endLocation;
					
					methodDeclaration.HandlesClause = handlesClause;
					methodDeclaration.ImplementsClause = implementsClause;
					
					compilationUnit.AddChild(methodDeclaration);
					
				} else if (StartOf(10)) {

#line  936 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration(name, m.Modifier,  null, p, attributes);
					methodDeclaration.Specials["before"] = comments;
					methodDeclaration.StartLocation = startPos;
					methodDeclaration.EndLocation   = endLocation;
					
					methodDeclaration.HandlesClause = handlesClause;
					methodDeclaration.ImplementsClause = implementsClause;
					
					compilationUnit.AddChild(methodDeclaration);
					compilationUnit.BlockStart(methodDeclaration);
					
					Block(
#line  947 "VBNET.ATG" 
out stmt);

#line  949 "VBNET.ATG" 
					compilationUnit.BlockEnd();
					methodDeclaration.Body  = (BlockStatement)stmt;
					
					Expect(89);
					Expect(169);

#line  952 "VBNET.ATG" 
					methodDeclaration.Body.EndLocation = t.EndLocation; 
					Expect(1);
				} else SynErr(201);
			} else if (la.kind == 128) {
				lexer.NextToken();
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  955 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}

#line  956 "VBNET.ATG" 
				m.Check(Modifier.Constructors); 

#line  957 "VBNET.ATG" 
				Point constructorEndLocation = t.EndLocation; 
				Expect(1);
				Block(
#line  959 "VBNET.ATG" 
out stmt);
				Expect(89);
				Expect(169);

#line  960 "VBNET.ATG" 
				Point endLocation = t.EndLocation; 
				Expect(1);

#line  962 "VBNET.ATG" 
				ConstructorDeclaration cd = new ConstructorDeclaration("New", m.Modifier, p, attributes); 
				cd.StartLocation = startPos;
				cd.Specials["before"] = comments;
				cd.EndLocation   = constructorEndLocation;
				cd.Body = (BlockStatement)stmt;
				cd.Body.EndLocation   = endLocation;
				compilationUnit.AddChild(cd);
				
			} else SynErr(202);
			break;
		}
		case 101: {
			lexer.NextToken();

#line  975 "VBNET.ATG" 
			m.Check(Modifier.Methods);
			string name = String.Empty;
			Point startPos = t.Location;
			MethodDeclaration methodDeclaration;
			HandlesClause handlesClause = null;
			ImplementsClause implementsClause = null;
			AttributeSection attributeSection = null;
			
			Identifier();

#line  983 "VBNET.ATG" 
			name = t.val; 
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  984 "VBNET.ATG" 
out p);
				}
				Expect(26);
			}
			if (la.kind == 49) {
				lexer.NextToken();
				if (la.kind == 28) {
					AttributeSection(
#line  985 "VBNET.ATG" 
out attributeSection);
				}
				TypeName(
#line  985 "VBNET.ATG" 
out type);
			}

#line  987 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object");
			}
			type.Attributes = attributeSection;
			
			if (la.kind == 106 || la.kind == 108) {
				if (la.kind == 108) {
					ImplementsClause(
#line  994 "VBNET.ATG" 
out implementsClause);
				} else {
					HandlesClause(
#line  996 "VBNET.ATG" 
out handlesClause);
				}
			}
			Expect(1);
			if (
#line  1002 "VBNET.ATG" 
IsMustOverride(m)) {

#line  1004 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration(name, m.Modifier,  type, p, attributes);
				methodDeclaration.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				methodDeclaration.StartLocation = startPos;
				methodDeclaration.EndLocation   = t.EndLocation;
				methodDeclaration.HandlesClause = handlesClause;
				methodDeclaration.ImplementsClause = implementsClause;
				compilationUnit.AddChild(methodDeclaration);
				
			} else if (StartOf(10)) {

#line  1014 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration(name, m.Modifier,  type, p, attributes);
				methodDeclaration.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				methodDeclaration.StartLocation = startPos;
				methodDeclaration.EndLocation   = t.EndLocation;
				
				methodDeclaration.HandlesClause = handlesClause;
				methodDeclaration.ImplementsClause = implementsClause;
				
				compilationUnit.AddChild(methodDeclaration);
				compilationUnit.BlockStart(methodDeclaration);
				
				Block(
#line  1025 "VBNET.ATG" 
out stmt);

#line  1027 "VBNET.ATG" 
				compilationUnit.BlockEnd();
				methodDeclaration.Body  = (BlockStatement)stmt;
				
				Expect(89);
				Expect(101);

#line  1032 "VBNET.ATG" 
				methodDeclaration.Body.StartLocation = methodDeclaration.EndLocation;
				methodDeclaration.Body.EndLocation   = t.EndLocation;
				
				Expect(1);
			} else SynErr(203);
			break;
		}
		case 79: {
			lexer.NextToken();

#line  1041 "VBNET.ATG" 
			m.Check(Modifier.ExternalMethods);
			Point startPos = t.Location;
			CharsetModifier charsetModifer = CharsetModifier.None;
			string library = String.Empty;
			string alias = null;
			string name = String.Empty;
			
			if (StartOf(11)) {
				Charset(
#line  1048 "VBNET.ATG" 
out charsetModifer);
			}
			if (la.kind == 169) {
				lexer.NextToken();
				Identifier();

#line  1051 "VBNET.ATG" 
				name = t.val; 
				Expect(116);
				Expect(3);

#line  1052 "VBNET.ATG" 
				library = t.val; 
				if (la.kind == 45) {
					lexer.NextToken();
					Expect(3);

#line  1053 "VBNET.ATG" 
					alias = t.val; 
				}
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  1054 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				Expect(1);

#line  1057 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, null, p, attributes, library, alias, charsetModifer);
				declareDeclaration.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				declareDeclaration.StartLocation = startPos;
				declareDeclaration.EndLocation   = t.EndLocation;
				compilationUnit.AddChild(declareDeclaration);
				
			} else if (la.kind == 101) {
				lexer.NextToken();
				Identifier();

#line  1065 "VBNET.ATG" 
				name = t.val; 
				Expect(116);
				Expect(3);

#line  1066 "VBNET.ATG" 
				library = t.val; 
				if (la.kind == 45) {
					lexer.NextToken();
					Expect(3);

#line  1067 "VBNET.ATG" 
					alias = t.val; 
				}
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  1068 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				if (la.kind == 49) {
					lexer.NextToken();
					TypeName(
#line  1069 "VBNET.ATG" 
out type);
				}
				Expect(1);

#line  1072 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, type, p, attributes, library, alias, charsetModifer);
				declareDeclaration.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				declareDeclaration.StartLocation = startPos;
				declareDeclaration.EndLocation   = t.EndLocation;
				compilationUnit.AddChild(declareDeclaration);
				
			} else SynErr(204);
			break;
		}
		case 94: {
			lexer.NextToken();

#line  1083 "VBNET.ATG" 
			m.Check(Modifier.Events);
			Point startPos = t.Location;
			EventDeclaration eventDeclaration;
			string name = String.Empty;
			ImplementsClause implementsClause = null;
			
			Identifier();

#line  1089 "VBNET.ATG" 
			name= t.val; 
			if (la.kind == 49) {
				lexer.NextToken();
				TypeName(
#line  1091 "VBNET.ATG" 
out type);
			} else if (la.kind == 1 || la.kind == 25 || la.kind == 108) {
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  1093 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
			} else SynErr(205);
			if (la.kind == 108) {
				ImplementsClause(
#line  1095 "VBNET.ATG" 
out implementsClause);
			}

#line  1097 "VBNET.ATG" 
			eventDeclaration = new EventDeclaration(type, m.Modifier, p, attributes, name, implementsClause);
			eventDeclaration.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
			eventDeclaration.StartLocation = startPos;
			eventDeclaration.EndLocation = t.EndLocation;
			compilationUnit.AddChild(eventDeclaration);
			
			Expect(1);
			break;
		}
		case 2: case 52: case 71: case 171: {

#line  1105 "VBNET.ATG" 
			Point startPos = t.Location; 

#line  1107 "VBNET.ATG" 
			m.Check(Modifier.Fields);
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
			ArrayList comments = lexer.SpecialTracker.RetreiveComments();
			fd.StartLocation = startPos;
			
			VariableDeclarator(
#line  1112 "VBNET.ATG" 
variableDeclarators);

#line  1114 "VBNET.ATG" 
			((INode)variableDeclarators[0]).Specials["before"] = comments;
			
			while (la.kind == 12) {
				lexer.NextToken();
				VariableDeclarator(
#line  1116 "VBNET.ATG" 
variableDeclarators);
			}
			Expect(1);

#line  1119 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			fd.Fields = variableDeclarators;
			compilationUnit.AddChild(fd);
			
			break;
		}
		case 72: {

#line  1124 "VBNET.ATG" 
			m.Check(Modifier.Fields); 
			lexer.NextToken();

#line  1125 "VBNET.ATG" 
			m.Add(Modifier.Constant);  

#line  1127 "VBNET.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
			fd.StartLocation = t.Location;
			ArrayList comments = lexer.SpecialTracker.RetreiveComments();
			ArrayList constantDeclarators = new ArrayList();
			
			ConstantDeclarator(
#line  1132 "VBNET.ATG" 
constantDeclarators);

#line  1134 "VBNET.ATG" 
			((INode)constantDeclarators[0]).Specials["before"] = comments;
			
			while (la.kind == 12) {
				lexer.NextToken();
				ConstantDeclarator(
#line  1136 "VBNET.ATG" 
constantDeclarators);
			}

#line  1138 "VBNET.ATG" 
			fd.Fields = constantDeclarators;
			fd.EndLocation = t.Location;
			
			Expect(1);

#line  1143 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			compilationUnit.AddChild(fd);
			
			break;
		}
		case 148: {
			lexer.NextToken();

#line  1149 "VBNET.ATG" 
			m.Check(Modifier.Properties);
			Point startPos = t.Location;
			ImplementsClause implementsClause = null;
			
			Identifier();

#line  1153 "VBNET.ATG" 
			string propertyName = t.val; 
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1154 "VBNET.ATG" 
out p);
				}
				Expect(26);
			}
			if (la.kind == 49) {
				lexer.NextToken();
				TypeName(
#line  1155 "VBNET.ATG" 
out type);
			}

#line  1157 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object");
			}
			
			if (la.kind == 108) {
				ImplementsClause(
#line  1161 "VBNET.ATG" 
out implementsClause);
			}
			Expect(1);
			if (
#line  1165 "VBNET.ATG" 
IsMustOverride(m)) {

#line  1167 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				pDecl.StartLocation = startPos;
				pDecl.EndLocation   = t.Location;
				pDecl.TypeReference = type;
				pDecl.ImplementsClause = implementsClause;
				pDecl.Parameters = p;
				compilationUnit.AddChild(pDecl);
				
			} else if (la.kind == 28 || la.kind == 102 || la.kind == 158) {

#line  1178 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				pDecl.StartLocation = startPos;
				pDecl.EndLocation   = t.Location;
				pDecl.BodyStart   = t.Location;
				pDecl.TypeReference = type;
				pDecl.ImplementsClause = implementsClause;
				pDecl.Parameters = p;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  1189 "VBNET.ATG" 
out getRegion, out setRegion);
				Expect(89);
				Expect(148);
				Expect(1);

#line  1193 "VBNET.ATG" 
				pDecl.GetRegion = getRegion;
				pDecl.SetRegion = setRegion;
				pDecl.BodyEnd = t.EndLocation;
				compilationUnit.AddChild(pDecl);
				
			} else SynErr(206);
			break;
		}
		default: SynErr(207); break;
		}
	}

	void EnumMemberDecl(
#line  849 "VBNET.ATG" 
out FieldDeclaration f) {

#line  851 "VBNET.ATG" 
		Expression expr = null;
		ArrayList attributes = new ArrayList();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 28) {
			AttributeSection(
#line  856 "VBNET.ATG" 
out section);

#line  856 "VBNET.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  859 "VBNET.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl = new VariableDeclaration(t.val);
		varDecl.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
		f.Fields.Add(varDecl);
		f.StartLocation = t.Location;
		
		if (la.kind == 11) {
			lexer.NextToken();
			Expr(
#line  865 "VBNET.ATG" 
out expr);

#line  865 "VBNET.ATG" 
			varDecl.Initializer = expr; 
		}
		Expect(1);
	}

	void InterfaceMemberDecl() {

#line  760 "VBNET.ATG" 
		TypeReference type =null;
		ArrayList p = null;
		AttributeSection section;
		Modifiers mod = new Modifiers(this);
		ArrayList attributes = new ArrayList();
		/*ArrayList parameters = new ArrayList();*/
		string name;
		
		if (StartOf(12)) {
			while (la.kind == 28) {
				AttributeSection(
#line  768 "VBNET.ATG" 
out section);

#line  768 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(6)) {
				MemberModifier(
#line  772 "VBNET.ATG" 
mod);
			}
			if (la.kind == 94) {
				lexer.NextToken();

#line  775 "VBNET.ATG" 
				mod.Check(Modifier.InterfaceEvents); 
				Identifier();

#line  776 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  777 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				if (la.kind == 49) {
					lexer.NextToken();
					TypeName(
#line  778 "VBNET.ATG" 
out type);
				}
				Expect(1);

#line  781 "VBNET.ATG" 
				EventDeclaration ed = new EventDeclaration(type, mod.Modifier, p, attributes, name, null);
				ed.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				compilationUnit.AddChild(ed);
				ed.EndLocation = t.EndLocation;
				
			} else if (la.kind == 169) {
				lexer.NextToken();

#line  789 "VBNET.ATG" 
				mod.Check(Modifier.InterfaceMethods);
				ArrayList comments = lexer.SpecialTracker.RetreiveComments();
				
				Identifier();

#line  792 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  793 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				Expect(1);

#line  796 "VBNET.ATG" 
				MethodDeclaration md = new MethodDeclaration(name, mod.Modifier, null, p, attributes);
				md.Specials["before"] = comments;
				md.EndLocation = t.EndLocation;
				compilationUnit.AddChild(md);
				
			} else if (la.kind == 101) {
				lexer.NextToken();

#line  804 "VBNET.ATG" 
				mod.Check(Modifier.InterfaceMethods);
				AttributeSection attributeSection = null;
				
				Identifier();

#line  807 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  808 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				if (la.kind == 49) {
					lexer.NextToken();
					if (la.kind == 28) {
						AttributeSection(
#line  809 "VBNET.ATG" 
out attributeSection);
					}
					TypeName(
#line  809 "VBNET.ATG" 
out type);
				}

#line  811 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object");
				}
				type.Attributes = attributeSection;
				MethodDeclaration md = new MethodDeclaration(name, mod.Modifier, type, p, attributes);
				md.Specials["before"] = lexer.SpecialTracker.RetreiveComments();
				md.EndLocation = t.EndLocation;
				compilationUnit.AddChild(md);
				
				Expect(1);
			} else if (la.kind == 148) {
				lexer.NextToken();

#line  824 "VBNET.ATG" 
				mod.Check(Modifier.InterfaceProperties);
				ArrayList comments = lexer.SpecialTracker.RetreiveComments();
				
				Identifier();

#line  827 "VBNET.ATG" 
				name = t.val;  
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  828 "VBNET.ATG" 
out p);
					}
					Expect(26);
				}
				if (la.kind == 49) {
					lexer.NextToken();
					TypeName(
#line  829 "VBNET.ATG" 
out type);
				}

#line  831 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object");
				}
				
				Expect(1);

#line  837 "VBNET.ATG" 
				PropertyDeclaration pd = new PropertyDeclaration(name, type, mod.Modifier, attributes);
				pd.Parameters = p;
				pd.Specials["before"] = comments;
				pd.EndLocation = t.EndLocation;
				compilationUnit.AddChild(pd);
				
			} else SynErr(208);
		} else if (StartOf(13)) {
			NonModuleDeclaration(
#line  845 "VBNET.ATG" 
mod, attributes);
		} else SynErr(209);
	}

	void Expr(
#line  1447 "VBNET.ATG" 
out Expression expr) {

#line  1448 "VBNET.ATG" 
		expr = new Expression(); 
		ConditionalOrExpr(
#line  1449 "VBNET.ATG" 
out expr);
		while (StartOf(14)) {

#line  1452 "VBNET.ATG" 
			AssignmentOperatorType op; Expression val; 
			AssignmentOperator(
#line  1453 "VBNET.ATG" 
out op);
			Expr(
#line  1453 "VBNET.ATG" 
out val);

#line  1453 "VBNET.ATG" 
			expr = new AssignmentExpression(expr, op, val); 
		}
	}

	void ImplementsClause(
#line  1425 "VBNET.ATG" 
out ImplementsClause clause) {

#line  1427 "VBNET.ATG" 
		clause = new ImplementsClause();
		string typename = String.Empty;
		string first;
		
		Expect(108);
		Identifier();

#line  1431 "VBNET.ATG" 
		first = t.val; 
		Expect(10);
		Qualident(
#line  1431 "VBNET.ATG" 
out typename);

#line  1431 "VBNET.ATG" 
		((ImplementsClause)clause).BaseMembers.Add(first + "." + typename); 
		while (la.kind == 12) {
			lexer.NextToken();
			Identifier();

#line  1432 "VBNET.ATG" 
			first = t.val; 
			Expect(10);
			Qualident(
#line  1432 "VBNET.ATG" 
out typename);

#line  1432 "VBNET.ATG" 
			((ImplementsClause)clause).BaseMembers.Add(first + "." + typename); 
		}
	}

	void HandlesClause(
#line  1383 "VBNET.ATG" 
out HandlesClause handlesClause) {

#line  1385 "VBNET.ATG" 
		handlesClause = new HandlesClause();
		string name;
		
		Expect(106);
		EventMemberSpecifier(
#line  1388 "VBNET.ATG" 
out name);

#line  1388 "VBNET.ATG" 
		handlesClause.EventNames.Add(name); 
		while (la.kind == 12) {
			lexer.NextToken();
			EventMemberSpecifier(
#line  1389 "VBNET.ATG" 
out name);

#line  1389 "VBNET.ATG" 
			handlesClause.EventNames.Add(name); 
		}
	}

	void Block(
#line  2004 "VBNET.ATG" 
out Statement stmt) {

#line  2007 "VBNET.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		blockStmt.StartLocation = t.Location;
		compilationUnit.BlockStart(blockStmt);
		
		while (StartOf(15) || 
#line  2012 "VBNET.ATG" 
IsEndStmtAhead()) {
			if (StartOf(15)) {
				Statement();
				EndOfStmt();
			} else {
				Expect(89);
				EndOfStmt();

#line  2012 "VBNET.ATG" 
				compilationUnit.AddChild(new EndStatement()); 
			}
		}

#line  2015 "VBNET.ATG" 
		stmt = blockStmt;
		blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void Charset(
#line  1375 "VBNET.ATG" 
out CharsetModifier charsetModifier) {

#line  1376 "VBNET.ATG" 
		charsetModifier = CharsetModifier.None; 
		if (la.kind == 101 || la.kind == 169) {
		} else if (la.kind == 48) {
			lexer.NextToken();

#line  1377 "VBNET.ATG" 
			charsetModifier = CharsetModifier.ANSI; 
		} else if (la.kind == 51) {
			lexer.NextToken();

#line  1378 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Auto; 
		} else if (la.kind == 178) {
			lexer.NextToken();

#line  1379 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Unicode; 
		} else SynErr(210);
	}

	void VariableDeclarator(
#line  1276 "VBNET.ATG" 
ArrayList fieldDeclaration) {

#line  1278 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		ObjectCreateExpression oce = null;
		ArrayCreateExpression ace = null;
		ArrayList rank = null;
		ArrayList dimension = null;
		
		Identifier();

#line  1287 "VBNET.ATG" 
		VariableDeclaration f = new VariableDeclaration(t.val);
		
		if (
#line  1289 "VBNET.ATG" 
IsRank()) {
			ArrayTypeModifiers(
#line  1289 "VBNET.ATG" 
out rank);
		}
		if (
#line  1290 "VBNET.ATG" 
IsSize()) {
			ArrayInitializationModifier(
#line  1290 "VBNET.ATG" 
out dimension);
		}
		if (
#line  1292 "VBNET.ATG" 
IsObjectCreation()) {
			Expect(49);
			ObjectCreateExpression(
#line  1292 "VBNET.ATG" 
out expr);

#line  1294 "VBNET.ATG" 
			if(expr is ArrayCreateExpression) {
			ace = expr as ArrayCreateExpression;
			f.Initializer = ace.ArrayInitializer;
			
			} else {
				oce = expr as ObjectCreateExpression;
				f.Initializer = oce;
				if(oce.CreateType != null) {
					f.Type = oce.CreateType;
				}
			}
			
		} else if (StartOf(16)) {
			if (la.kind == 49) {
				lexer.NextToken();
				TypeName(
#line  1307 "VBNET.ATG" 
out type);
			}

#line  1309 "VBNET.ATG" 
			if(type != null) {
			type.Dimension = dimension;
			}
			f.Type = type;
			if (type != null && rank != null) {
				if(type.RankSpecifier != null) {
					Error("array rank only allowed one time");
				} else {
					type.RankSpecifier = rank;
				}
			}
			
			if (la.kind == 11) {
				lexer.NextToken();
				VariableInitializer(
#line  1321 "VBNET.ATG" 
out expr);

#line  1321 "VBNET.ATG" 
				f.Initializer = expr; 
			}
		} else SynErr(211);

#line  1323 "VBNET.ATG" 
		fieldDeclaration.Add(f); 
	}

	void ConstantDeclarator(
#line  1259 "VBNET.ATG" 
ArrayList constantDeclaration) {

#line  1261 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		string name = String.Empty;
		
		Identifier();

#line  1265 "VBNET.ATG" 
		name = t.val; 
		if (la.kind == 49) {
			lexer.NextToken();
			TypeName(
#line  1266 "VBNET.ATG" 
out type);
		}
		Expect(11);
		Expr(
#line  1267 "VBNET.ATG" 
out expr);

#line  1269 "VBNET.ATG" 
		VariableDeclaration f = new VariableDeclaration(name, expr);
		f.Type = type;
		constantDeclaration.Add(f);
		
	}

	void AccessorDecls(
#line  1202 "VBNET.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1204 "VBNET.ATG" 
		ArrayList attributes = new ArrayList(); 
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		
		while (la.kind == 28) {
			AttributeSection(
#line  1209 "VBNET.ATG" 
out section);

#line  1209 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 102) {
			GetAccessorDecl(
#line  1211 "VBNET.ATG" 
out getBlock, attributes);
			if (la.kind == 28 || la.kind == 158) {

#line  1213 "VBNET.ATG" 
				attributes = new ArrayList(); 
				while (la.kind == 28) {
					AttributeSection(
#line  1214 "VBNET.ATG" 
out section);

#line  1214 "VBNET.ATG" 
					attributes.Add(section); 
				}
				SetAccessorDecl(
#line  1215 "VBNET.ATG" 
out setBlock, attributes);
			}
		} else if (la.kind == 158) {
			SetAccessorDecl(
#line  1218 "VBNET.ATG" 
out setBlock, attributes);
			if (la.kind == 28 || la.kind == 102) {

#line  1220 "VBNET.ATG" 
				attributes = new ArrayList(); 
				while (la.kind == 28) {
					AttributeSection(
#line  1221 "VBNET.ATG" 
out section);

#line  1221 "VBNET.ATG" 
					attributes.Add(section); 
				}
				GetAccessorDecl(
#line  1222 "VBNET.ATG" 
out getBlock, attributes);
			}
		} else SynErr(212);
	}

	void GetAccessorDecl(
#line  1228 "VBNET.ATG" 
out PropertyGetRegion getBlock, ArrayList attributes) {

#line  1229 "VBNET.ATG" 
		Statement stmt = null; 
		Expect(102);
		Expect(1);
		Block(
#line  1232 "VBNET.ATG" 
out stmt);

#line  1234 "VBNET.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes);
		
		Expect(89);
		Expect(102);
		Expect(1);
	}

	void SetAccessorDecl(
#line  1241 "VBNET.ATG" 
out PropertySetRegion setBlock, ArrayList attributes) {

#line  1243 "VBNET.ATG" 
		Statement stmt = null;
		ArrayList p = null;
		
		Expect(158);
		if (la.kind == 25) {
			lexer.NextToken();
			if (StartOf(4)) {
				FormalParameterList(
#line  1247 "VBNET.ATG" 
out p);
			}
			Expect(26);
		}
		Expect(1);
		Block(
#line  1249 "VBNET.ATG" 
out stmt);

#line  1251 "VBNET.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes);
		setBlock.Parameters = p;
		
		Expect(89);
		Expect(158);
		Expect(1);
	}

	void ArrayTypeModifiers(
#line  1813 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  1815 "VBNET.ATG" 
		arrayModifiers = new ArrayList();
		int i = 0;
		
		while (
#line  1818 "VBNET.ATG" 
IsRank()) {
			Expect(25);
			if (la.kind == 12 || la.kind == 26) {
				RankList(
#line  1820 "VBNET.ATG" 
out i);
			}

#line  1822 "VBNET.ATG" 
			arrayModifiers.Add(i);
			
			Expect(26);
		}

#line  1827 "VBNET.ATG" 
		if(arrayModifiers.Count == 0) {
		 arrayModifiers = null;
		}
		
	}

	void ArrayInitializationModifier(
#line  1327 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  1329 "VBNET.ATG" 
		arrayModifiers = null;
		
		Expect(25);
		InitializationRankList(
#line  1331 "VBNET.ATG" 
out arrayModifiers);
		Expect(26);
	}

	void ObjectCreateExpression(
#line  1701 "VBNET.ATG" 
out Expression oce) {

#line  1703 "VBNET.ATG" 
		TypeReference type = null;
		Expression initializer = null;
		ArrayList arguments = null;
		oce = null;
		
		Expect(128);
		ArrayTypeName(
#line  1708 "VBNET.ATG" 
out type);
		if (la.kind == 25) {
			lexer.NextToken();
			if (StartOf(17)) {
				ArgumentList(
#line  1710 "VBNET.ATG" 
out arguments);
			}
			Expect(26);
		}
		if (la.kind == 21) {
			ArrayInitializer(
#line  1714 "VBNET.ATG" 
out initializer);
		}

#line  1717 "VBNET.ATG" 
		if(initializer == null) {
		oce = new ObjectCreateExpression(type, arguments);
		} else {
			ArrayCreateExpression ace = new ArrayCreateExpression(type, initializer as ArrayInitializerExpression);
			ace.Parameters = arguments;
			oce = ace;
		}
		
	}

	void VariableInitializer(
#line  1347 "VBNET.ATG" 
out Expression initializerExpression) {

#line  1349 "VBNET.ATG" 
		initializerExpression = null;
		
		if (StartOf(18)) {
			Expr(
#line  1351 "VBNET.ATG" 
out initializerExpression);
		} else if (la.kind == 21) {
			ArrayInitializer(
#line  1352 "VBNET.ATG" 
out initializerExpression);
		} else SynErr(213);
	}

	void InitializationRankList(
#line  1335 "VBNET.ATG" 
out ArrayList rank) {

#line  1337 "VBNET.ATG" 
		rank = null;
		Expression expr = null;
		
		Expr(
#line  1340 "VBNET.ATG" 
out expr);

#line  1340 "VBNET.ATG" 
		rank = new ArrayList(); rank.Add(expr); 
		while (la.kind == 12) {
			lexer.NextToken();
			Expr(
#line  1342 "VBNET.ATG" 
out expr);

#line  1342 "VBNET.ATG" 
			rank.Add(expr); 
		}
	}

	void ArrayInitializer(
#line  1356 "VBNET.ATG" 
out Expression outExpr) {

#line  1358 "VBNET.ATG" 
		Expression expr = null;
		ArrayInitializerExpression initializer = new ArrayInitializerExpression();
		
		Expect(21);
		if (StartOf(19)) {
			VariableInitializer(
#line  1363 "VBNET.ATG" 
out expr);

#line  1365 "VBNET.ATG" 
			initializer.CreateExpressions.Add(expr);
			
			while (
#line  1368 "VBNET.ATG" 
NotFinalComma()) {
				Expect(12);
				VariableInitializer(
#line  1368 "VBNET.ATG" 
out expr);

#line  1369 "VBNET.ATG" 
				initializer.CreateExpressions.Add(expr); 
			}
		}
		Expect(22);

#line  1372 "VBNET.ATG" 
		outExpr = initializer; 
	}

	void EventMemberSpecifier(
#line  1435 "VBNET.ATG" 
out string name) {

#line  1436 "VBNET.ATG" 
		string type; name = String.Empty; 
		if (StartOf(9)) {
			Identifier();

#line  1437 "VBNET.ATG" 
			type = t.val; 
			Expect(10);
			Identifier();

#line  1439 "VBNET.ATG" 
			name = type + "." + t.val; 
		} else if (la.kind == 125) {
			lexer.NextToken();
			Expect(10);
			if (StartOf(9)) {
				Identifier();

#line  1442 "VBNET.ATG" 
				name = "MyBase." + t.val; 
			} else if (la.kind == 93) {
				lexer.NextToken();

#line  1443 "VBNET.ATG" 
				name = "MyBase.Error"; 
			} else SynErr(214);
		} else SynErr(215);
	}

	void ConditionalOrExpr(
#line  1581 "VBNET.ATG" 
out Expression outExpr) {

#line  1582 "VBNET.ATG" 
		Expression expr; 
		ConditionalAndExpr(
#line  1583 "VBNET.ATG" 
out outExpr);
		while (la.kind == 140) {
			lexer.NextToken();
			ConditionalAndExpr(
#line  1583 "VBNET.ATG" 
out expr);

#line  1583 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BooleanOr, expr);  
		}
	}

	void AssignmentOperator(
#line  1478 "VBNET.ATG" 
out AssignmentOperatorType op) {

#line  1479 "VBNET.ATG" 
		op = AssignmentOperatorType.None; 
		switch (la.kind) {
		case 11: {
			lexer.NextToken();

#line  1480 "VBNET.ATG" 
			op = AssignmentOperatorType.Assign; 
			break;
		}
		case 42: {
			lexer.NextToken();

#line  1481 "VBNET.ATG" 
			op = AssignmentOperatorType.ConcatString; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1482 "VBNET.ATG" 
			op = AssignmentOperatorType.Add; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1483 "VBNET.ATG" 
			op = AssignmentOperatorType.Subtract; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1484 "VBNET.ATG" 
			op = AssignmentOperatorType.Multiply; 
			break;
		}
		case 38: {
			lexer.NextToken();

#line  1485 "VBNET.ATG" 
			op = AssignmentOperatorType.Divide; 
			break;
		}
		case 39: {
			lexer.NextToken();

#line  1486 "VBNET.ATG" 
			op = AssignmentOperatorType.DivideInteger; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1487 "VBNET.ATG" 
			op = AssignmentOperatorType.Power; 
			break;
		}
		case 40: {
			lexer.NextToken();

#line  1488 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
			break;
		}
		case 41: {
			lexer.NextToken();

#line  1489 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
			break;
		}
		default: SynErr(216); break;
		}
	}

	void UnaryExpr(
#line  1457 "VBNET.ATG" 
out Expression uExpr) {

#line  1459 "VBNET.ATG" 
		Expression expr;
		UnaryOperatorType uop = UnaryOperatorType.None;
		bool isUOp = false;
		
		while (la.kind == 14 || la.kind == 15 || la.kind == 16) {
			if (la.kind == 14) {
				lexer.NextToken();

#line  1463 "VBNET.ATG" 
				uop = UnaryOperatorType.Plus; isUOp = true; 
			} else if (la.kind == 15) {
				lexer.NextToken();

#line  1464 "VBNET.ATG" 
				uop = UnaryOperatorType.Minus; isUOp = true; 
			} else {
				lexer.NextToken();

#line  1466 "VBNET.ATG" 
				uop = UnaryOperatorType.Star;  isUOp = true;
			}
		}
		SimpleExpr(
#line  1468 "VBNET.ATG" 
out expr);

#line  1470 "VBNET.ATG" 
		if (isUOp) {
		uExpr = new UnaryOperatorExpression(expr, uop);
		} else {
			uExpr = expr;
		}
		
	}

	void SimpleExpr(
#line  1493 "VBNET.ATG" 
out Expression pexpr) {

#line  1495 "VBNET.ATG" 
		Expression expr;
		TypeReference type = null;
		string name = String.Empty;
		pexpr = null;
		
		if (StartOf(20)) {
			switch (la.kind) {
			case 3: {
				lexer.NextToken();

#line  1503 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 4: {
				lexer.NextToken();

#line  1504 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 7: {
				lexer.NextToken();

#line  1505 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 6: {
				lexer.NextToken();

#line  1506 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 5: {
				lexer.NextToken();

#line  1507 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 9: {
				lexer.NextToken();

#line  1508 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 8: {
				lexer.NextToken();

#line  1509 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val);  
				break;
			}
			case 175: {
				lexer.NextToken();

#line  1511 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(true, "true");  
				break;
			}
			case 97: {
				lexer.NextToken();

#line  1512 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(false, "false"); 
				break;
			}
			case 131: {
				lexer.NextToken();

#line  1513 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(null, "null");  
				break;
			}
			case 25: {
				lexer.NextToken();
				Expr(
#line  1514 "VBNET.ATG" 
out expr);
				Expect(26);

#line  1514 "VBNET.ATG" 
				pexpr = new ParenthesizedExpression(expr); 
				break;
			}
			case 2: case 52: case 71: case 171: {
				Identifier();

#line  1515 "VBNET.ATG" 
				pexpr = new IdentifierExpression(t.val); 
				break;
			}
			case 53: case 55: case 66: case 77: case 78: case 85: case 112: case 118: case 161: case 162: case 167: {

#line  1516 "VBNET.ATG" 
				string val = String.Empty; 
				PrimitiveTypeName(
#line  1516 "VBNET.ATG" 
out val);
				Expect(10);
				Identifier();

#line  1517 "VBNET.ATG" 
				pexpr = new FieldReferenceOrInvocationExpression(new TypeReferenceExpression(val), t.val); 
				break;
			}
			case 120: {
				lexer.NextToken();

#line  1518 "VBNET.ATG" 
				pexpr = new ThisReferenceExpression(); 
				break;
			}
			case 125: case 126: {

#line  1519 "VBNET.ATG" 
				Expression retExpr = null; 
				if (la.kind == 125) {
					lexer.NextToken();

#line  1520 "VBNET.ATG" 
					retExpr = new BaseReferenceExpression(); 
				} else if (la.kind == 126) {
					lexer.NextToken();

#line  1521 "VBNET.ATG" 
					retExpr = new ClassReferenceExpression(); 
				} else SynErr(217);
				Expect(10);
				IdentifierOrKeyword(
#line  1523 "VBNET.ATG" 
out name);

#line  1523 "VBNET.ATG" 
				pexpr = new FieldReferenceOrInvocationExpression(retExpr, name); 
				break;
			}
			case 128: {
				ObjectCreateExpression(
#line  1524 "VBNET.ATG" 
out expr);

#line  1524 "VBNET.ATG" 
				pexpr = expr; 
				break;
			}
			case 76: case 83: {
				if (la.kind == 83) {
					lexer.NextToken();
				} else if (la.kind == 76) {
					lexer.NextToken();
				} else SynErr(218);
				Expect(25);
				Expr(
#line  1525 "VBNET.ATG" 
out expr);
				Expect(12);
				TypeName(
#line  1525 "VBNET.ATG" 
out type);
				Expect(26);

#line  1525 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr); 
				break;
			}
			case 60: case 61: case 62: case 63: case 64: case 65: case 67: case 69: case 70: case 73: case 74: case 75: {
				CastTarget(
#line  1526 "VBNET.ATG" 
out type);
				Expect(25);
				Expr(
#line  1526 "VBNET.ATG" 
out expr);
				Expect(26);

#line  1526 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, true); 
				break;
			}
			case 44: {
				lexer.NextToken();
				Expr(
#line  1527 "VBNET.ATG" 
out expr);

#line  1527 "VBNET.ATG" 
				pexpr = new AddressOfExpression(expr); 
				break;
			}
			case 103: {
				lexer.NextToken();
				Expect(25);
				TypeName(
#line  1528 "VBNET.ATG" 
out type);
				Expect(26);

#line  1528 "VBNET.ATG" 
				pexpr = new GetTypeExpression(type); 
				break;
			}
			case 177: {
				lexer.NextToken();
				SimpleExpr(
#line  1529 "VBNET.ATG" 
out expr);
				Expect(114);
				TypeName(
#line  1529 "VBNET.ATG" 
out type);

#line  1529 "VBNET.ATG" 
				pexpr = new TypeOfExpression(expr, type); 
				break;
			}
			}
			while (la.kind == 10 || la.kind == 25) {
				if (la.kind == 10) {
					lexer.NextToken();
					IdentifierOrKeyword(
#line  1532 "VBNET.ATG" 
out name);

#line  1532 "VBNET.ATG" 
					pexpr = new FieldReferenceOrInvocationExpression(pexpr, name); 
				} else {
					lexer.NextToken();

#line  1533 "VBNET.ATG" 
					ArrayList parameters = new ArrayList(); 
					if (StartOf(21)) {

#line  1535 "VBNET.ATG" 
						expr = null; 
						if (StartOf(18)) {
							Argument(
#line  1535 "VBNET.ATG" 
out expr);
						}

#line  1535 "VBNET.ATG" 
						parameters.Add(expr); 
						while (la.kind == 12) {
							lexer.NextToken();

#line  1537 "VBNET.ATG" 
							expr = null; 
							if (StartOf(18)) {
								Argument(
#line  1538 "VBNET.ATG" 
out expr);
							}

#line  1538 "VBNET.ATG" 
							parameters.Add(expr); 
						}
					}
					Expect(26);

#line  1541 "VBNET.ATG" 
					pexpr = new InvocationExpression(pexpr, parameters); 
				}
			}
		} else if (la.kind == 10) {
			lexer.NextToken();
			IdentifierOrKeyword(
#line  1545 "VBNET.ATG" 
out name);

#line  1545 "VBNET.ATG" 
			pexpr = new FieldReferenceOrInvocationExpression(pexpr, name);
			while (la.kind == 10 || la.kind == 25) {
				if (la.kind == 10) {
					lexer.NextToken();
					IdentifierOrKeyword(
#line  1547 "VBNET.ATG" 
out name);

#line  1547 "VBNET.ATG" 
					pexpr = new FieldReferenceOrInvocationExpression(pexpr, name); 
				} else {
					lexer.NextToken();

#line  1548 "VBNET.ATG" 
					ArrayList parameters = new ArrayList(); 
					if (StartOf(21)) {

#line  1550 "VBNET.ATG" 
						expr = null; 
						if (StartOf(18)) {
							Argument(
#line  1550 "VBNET.ATG" 
out expr);
						}

#line  1550 "VBNET.ATG" 
						parameters.Add(expr); 
						while (la.kind == 12) {
							lexer.NextToken();

#line  1552 "VBNET.ATG" 
							expr = null; 
							if (StartOf(18)) {
								Argument(
#line  1553 "VBNET.ATG" 
out expr);
							}

#line  1553 "VBNET.ATG" 
							parameters.Add(expr); 
						}
					}
					Expect(26);

#line  1556 "VBNET.ATG" 
					pexpr = new InvocationExpression(pexpr, parameters); 
				}
			}
		} else SynErr(219);
	}

	void IdentifierOrKeyword(
#line  2547 "VBNET.ATG" 
out string name) {

#line  2549 "VBNET.ATG" 
		name = String.Empty;
		
		switch (la.kind) {
		case 2: case 52: case 71: case 171: {
			Identifier();

#line  2551 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 43: {
			lexer.NextToken();

#line  2552 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 44: {
			lexer.NextToken();

#line  2553 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 45: {
			lexer.NextToken();

#line  2554 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 46: {
			lexer.NextToken();

#line  2555 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 47: {
			lexer.NextToken();

#line  2556 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 48: {
			lexer.NextToken();

#line  2557 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 49: {
			lexer.NextToken();

#line  2558 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 50: {
			lexer.NextToken();

#line  2559 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 51: {
			lexer.NextToken();

#line  2560 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 53: {
			lexer.NextToken();

#line  2561 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 54: {
			lexer.NextToken();

#line  2562 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 55: {
			lexer.NextToken();

#line  2563 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 56: {
			lexer.NextToken();

#line  2564 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 57: {
			lexer.NextToken();

#line  2565 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 58: {
			lexer.NextToken();

#line  2566 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 59: {
			lexer.NextToken();

#line  2567 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 60: {
			lexer.NextToken();

#line  2568 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 61: {
			lexer.NextToken();

#line  2569 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 62: {
			lexer.NextToken();

#line  2570 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 63: {
			lexer.NextToken();

#line  2571 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 64: {
			lexer.NextToken();

#line  2572 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 65: {
			lexer.NextToken();

#line  2573 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 66: {
			lexer.NextToken();

#line  2574 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 67: {
			lexer.NextToken();

#line  2575 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 68: {
			lexer.NextToken();

#line  2576 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 69: {
			lexer.NextToken();

#line  2577 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 70: {
			lexer.NextToken();

#line  2578 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 72: {
			lexer.NextToken();

#line  2579 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 73: {
			lexer.NextToken();

#line  2580 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 74: {
			lexer.NextToken();

#line  2581 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 75: {
			lexer.NextToken();

#line  2582 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 76: {
			lexer.NextToken();

#line  2583 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 77: {
			lexer.NextToken();

#line  2584 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 78: {
			lexer.NextToken();

#line  2585 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 79: {
			lexer.NextToken();

#line  2586 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 80: {
			lexer.NextToken();

#line  2587 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 81: {
			lexer.NextToken();

#line  2588 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 82: {
			lexer.NextToken();

#line  2589 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  2590 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 84: {
			lexer.NextToken();

#line  2591 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 85: {
			lexer.NextToken();

#line  2592 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 86: {
			lexer.NextToken();

#line  2593 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 87: {
			lexer.NextToken();

#line  2594 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 88: {
			lexer.NextToken();

#line  2595 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 89: {
			lexer.NextToken();

#line  2596 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 90: {
			lexer.NextToken();

#line  2597 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 91: {
			lexer.NextToken();

#line  2598 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 92: {
			lexer.NextToken();

#line  2599 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 93: {
			lexer.NextToken();

#line  2600 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 94: {
			lexer.NextToken();

#line  2601 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 95: {
			lexer.NextToken();

#line  2602 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  2603 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 98: {
			lexer.NextToken();

#line  2604 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 99: {
			lexer.NextToken();

#line  2605 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 100: {
			lexer.NextToken();

#line  2606 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 101: {
			lexer.NextToken();

#line  2607 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 102: {
			lexer.NextToken();

#line  2608 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 103: {
			lexer.NextToken();

#line  2609 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 104: {
			lexer.NextToken();

#line  2610 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 105: {
			lexer.NextToken();

#line  2611 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 106: {
			lexer.NextToken();

#line  2612 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 107: {
			lexer.NextToken();

#line  2613 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 108: {
			lexer.NextToken();

#line  2614 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 109: {
			lexer.NextToken();

#line  2615 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 110: {
			lexer.NextToken();

#line  2616 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 111: {
			lexer.NextToken();

#line  2617 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 112: {
			lexer.NextToken();

#line  2618 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 113: {
			lexer.NextToken();

#line  2619 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 114: {
			lexer.NextToken();

#line  2620 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 115: {
			lexer.NextToken();

#line  2621 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  2622 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 117: {
			lexer.NextToken();

#line  2623 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 118: {
			lexer.NextToken();

#line  2624 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 119: {
			lexer.NextToken();

#line  2625 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 120: {
			lexer.NextToken();

#line  2626 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 121: {
			lexer.NextToken();

#line  2627 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 122: {
			lexer.NextToken();

#line  2628 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 123: {
			lexer.NextToken();

#line  2629 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 124: {
			lexer.NextToken();

#line  2630 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 125: {
			lexer.NextToken();

#line  2631 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 126: {
			lexer.NextToken();

#line  2632 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 127: {
			lexer.NextToken();

#line  2633 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 128: {
			lexer.NextToken();

#line  2634 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 129: {
			lexer.NextToken();

#line  2635 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 130: {
			lexer.NextToken();

#line  2636 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 131: {
			lexer.NextToken();

#line  2637 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 132: {
			lexer.NextToken();

#line  2638 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 133: {
			lexer.NextToken();

#line  2639 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 134: {
			lexer.NextToken();

#line  2640 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 136: {
			lexer.NextToken();

#line  2641 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 137: {
			lexer.NextToken();

#line  2642 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 138: {
			lexer.NextToken();

#line  2643 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 139: {
			lexer.NextToken();

#line  2644 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 140: {
			lexer.NextToken();

#line  2645 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 141: {
			lexer.NextToken();

#line  2646 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 142: {
			lexer.NextToken();

#line  2647 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 144: {
			lexer.NextToken();

#line  2648 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 145: {
			lexer.NextToken();

#line  2649 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 146: {
			lexer.NextToken();

#line  2650 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 147: {
			lexer.NextToken();

#line  2651 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 148: {
			lexer.NextToken();

#line  2652 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 149: {
			lexer.NextToken();

#line  2653 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 150: {
			lexer.NextToken();

#line  2654 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 151: {
			lexer.NextToken();

#line  2655 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 152: {
			lexer.NextToken();

#line  2656 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 153: {
			lexer.NextToken();

#line  2657 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 154: {
			lexer.NextToken();

#line  2658 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 155: {
			lexer.NextToken();

#line  2659 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 156: {
			lexer.NextToken();

#line  2660 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 157: {
			lexer.NextToken();

#line  2661 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 158: {
			lexer.NextToken();

#line  2662 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 159: {
			lexer.NextToken();

#line  2663 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 160: {
			lexer.NextToken();

#line  2664 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 161: {
			lexer.NextToken();

#line  2665 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 162: {
			lexer.NextToken();

#line  2666 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 163: {
			lexer.NextToken();

#line  2667 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 164: {
			lexer.NextToken();

#line  2668 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 165: {
			lexer.NextToken();

#line  2669 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 167: {
			lexer.NextToken();

#line  2670 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 168: {
			lexer.NextToken();

#line  2671 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 169: {
			lexer.NextToken();

#line  2672 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 170: {
			lexer.NextToken();

#line  2673 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 172: {
			lexer.NextToken();

#line  2674 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 173: {
			lexer.NextToken();

#line  2675 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 174: {
			lexer.NextToken();

#line  2676 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 175: {
			lexer.NextToken();

#line  2677 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 176: {
			lexer.NextToken();

#line  2678 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 177: {
			lexer.NextToken();

#line  2679 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 178: {
			lexer.NextToken();

#line  2680 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 179: {
			lexer.NextToken();

#line  2681 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 180: {
			lexer.NextToken();

#line  2682 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 181: {
			lexer.NextToken();

#line  2683 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 182: {
			lexer.NextToken();

#line  2684 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 183: {
			lexer.NextToken();

#line  2685 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 184: {
			lexer.NextToken();

#line  2686 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 185: {
			lexer.NextToken();

#line  2687 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 186: {
			lexer.NextToken();

#line  2688 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		case 187: {
			lexer.NextToken();

#line  2689 "VBNET.ATG" 
			name = t.val; 
			break;
		}
		default: SynErr(220); break;
		}
	}

	void CastTarget(
#line  1563 "VBNET.ATG" 
out TypeReference type) {

#line  1565 "VBNET.ATG" 
		type = null;
		
		switch (la.kind) {
		case 60: {
			lexer.NextToken();

#line  1567 "VBNET.ATG" 
			type = new TypeReference("System.Boolean"); 
			break;
		}
		case 61: {
			lexer.NextToken();

#line  1568 "VBNET.ATG" 
			type = new TypeReference("System.Byte"); 
			break;
		}
		case 62: {
			lexer.NextToken();

#line  1569 "VBNET.ATG" 
			type = new TypeReference("System.Char"); 
			break;
		}
		case 63: {
			lexer.NextToken();

#line  1570 "VBNET.ATG" 
			type = new TypeReference("System.DateTime"); 
			break;
		}
		case 65: {
			lexer.NextToken();

#line  1571 "VBNET.ATG" 
			type = new TypeReference("System.Decimal"); 
			break;
		}
		case 64: {
			lexer.NextToken();

#line  1572 "VBNET.ATG" 
			type = new TypeReference("System.Double"); 
			break;
		}
		case 67: {
			lexer.NextToken();

#line  1573 "VBNET.ATG" 
			type = new TypeReference("System.Int32"); 
			break;
		}
		case 69: {
			lexer.NextToken();

#line  1574 "VBNET.ATG" 
			type = new TypeReference("System.Int64"); 
			break;
		}
		case 70: {
			lexer.NextToken();

#line  1575 "VBNET.ATG" 
			type = new TypeReference("System.Object"); 
			break;
		}
		case 73: {
			lexer.NextToken();

#line  1576 "VBNET.ATG" 
			type = new TypeReference("System.Int16"); 
			break;
		}
		case 74: {
			lexer.NextToken();

#line  1577 "VBNET.ATG" 
			type = new TypeReference("System.Single"); 
			break;
		}
		case 75: {
			lexer.NextToken();

#line  1578 "VBNET.ATG" 
			type = new TypeReference("System.String"); 
			break;
		}
		default: SynErr(221); break;
		}
	}

	void Argument(
#line  1743 "VBNET.ATG" 
out Expression argumentexpr) {

#line  1745 "VBNET.ATG" 
		Expression expr;
		argumentexpr = null;
		string name;
		
		if (
#line  1749 "VBNET.ATG" 
IsNamedAssign()) {
			Identifier();

#line  1749 "VBNET.ATG" 
			name = t.val;  
			Expect(13);
			Expect(11);
			Expr(
#line  1749 "VBNET.ATG" 
out expr);

#line  1751 "VBNET.ATG" 
			argumentexpr = new NamedArgumentExpression(name, expr);
			
		} else if (StartOf(18)) {
			Expr(
#line  1754 "VBNET.ATG" 
out argumentexpr);
		} else SynErr(222);
	}

	void ConditionalAndExpr(
#line  1586 "VBNET.ATG" 
out Expression outExpr) {

#line  1587 "VBNET.ATG" 
		Expression expr; 
		InclusiveOrExpr(
#line  1588 "VBNET.ATG" 
out outExpr);
		while (la.kind == 47) {
			lexer.NextToken();
			InclusiveOrExpr(
#line  1588 "VBNET.ATG" 
out expr);

#line  1588 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BooleanAnd, expr);  
		}
	}

	void InclusiveOrExpr(
#line  1591 "VBNET.ATG" 
out Expression outExpr) {

#line  1592 "VBNET.ATG" 
		Expression expr; 
		ExclusiveOrExpr(
#line  1593 "VBNET.ATG" 
out outExpr);
		while (la.kind == 187) {
			lexer.NextToken();
			ExclusiveOrExpr(
#line  1593 "VBNET.ATG" 
out expr);

#line  1593 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.ExclusiveOr, expr);  
		}
	}

	void ExclusiveOrExpr(
#line  1596 "VBNET.ATG" 
out Expression outExpr) {

#line  1597 "VBNET.ATG" 
		Expression expr; 
		AndExpr(
#line  1598 "VBNET.ATG" 
out outExpr);
		while (la.kind == 139) {
			lexer.NextToken();
			AndExpr(
#line  1598 "VBNET.ATG" 
out expr);

#line  1598 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseOr, expr);  
		}
	}

	void AndExpr(
#line  1601 "VBNET.ATG" 
out Expression outExpr) {

#line  1602 "VBNET.ATG" 
		Expression expr; 
		NotExpr(
#line  1603 "VBNET.ATG" 
out outExpr);
		while (la.kind == 46) {
			lexer.NextToken();
			NotExpr(
#line  1603 "VBNET.ATG" 
out expr);

#line  1603 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseAnd, expr);  
		}
	}

	void NotExpr(
#line  1606 "VBNET.ATG" 
out Expression outExpr) {

#line  1607 "VBNET.ATG" 
		UnaryOperatorType uop = UnaryOperatorType.None; 
		while (la.kind == 130) {
			lexer.NextToken();

#line  1608 "VBNET.ATG" 
			uop = UnaryOperatorType.Not; 
		}
		EqualityExpr(
#line  1609 "VBNET.ATG" 
out outExpr);

#line  1610 "VBNET.ATG" 
		if (uop != UnaryOperatorType.None)
		outExpr = new UnaryOperatorExpression(outExpr, uop);
		
	}

	void EqualityExpr(
#line  1615 "VBNET.ATG" 
out Expression outExpr) {

#line  1617 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		RelationalExpr(
#line  1620 "VBNET.ATG" 
out outExpr);
		while (la.kind == 11 || la.kind == 29 || la.kind == 117) {
			if (la.kind == 29) {
				lexer.NextToken();

#line  1623 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
			} else if (la.kind == 11) {
				lexer.NextToken();

#line  1624 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
			} else {
				lexer.NextToken();

#line  1625 "VBNET.ATG" 
				op = BinaryOperatorType.Like; 
			}
			RelationalExpr(
#line  1627 "VBNET.ATG" 
out expr);

#line  1627 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void RelationalExpr(
#line  1631 "VBNET.ATG" 
out Expression outExpr) {

#line  1633 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  1636 "VBNET.ATG" 
out outExpr);
		while (StartOf(22)) {
			if (StartOf(23)) {
				if (la.kind == 28) {
					lexer.NextToken();

#line  1639 "VBNET.ATG" 
					op = BinaryOperatorType.LessThan; 
				} else if (la.kind == 27) {
					lexer.NextToken();

#line  1640 "VBNET.ATG" 
					op = BinaryOperatorType.GreaterThan; 
				} else if (la.kind == 31) {
					lexer.NextToken();

#line  1641 "VBNET.ATG" 
					op = BinaryOperatorType.LessThanOrEqual; 
				} else if (la.kind == 30) {
					lexer.NextToken();

#line  1642 "VBNET.ATG" 
					op = BinaryOperatorType.GreaterThanOrEqual; 
				} else SynErr(223);
				ShiftExpr(
#line  1644 "VBNET.ATG" 
out expr);

#line  1644 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
			} else {
				lexer.NextToken();

#line  1647 "VBNET.ATG" 
				op = BinaryOperatorType.IS; 
				Expr(
#line  1648 "VBNET.ATG" 
out expr);

#line  1648 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
			}
		}
	}

	void ShiftExpr(
#line  1652 "VBNET.ATG" 
out Expression outExpr) {

#line  1654 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		AdditiveExpr(
#line  1657 "VBNET.ATG" 
out outExpr);
		while (la.kind == 32 || la.kind == 33) {
			if (la.kind == 32) {
				lexer.NextToken();

#line  1660 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				lexer.NextToken();

#line  1661 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			AdditiveExpr(
#line  1663 "VBNET.ATG" 
out expr);

#line  1663 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AdditiveExpr(
#line  1667 "VBNET.ATG" 
out Expression outExpr) {

#line  1669 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		MultiplicativeExpr(
#line  1672 "VBNET.ATG" 
out outExpr);
		while (la.kind == 14 || la.kind == 15 || la.kind == 19) {
			if (la.kind == 14) {
				lexer.NextToken();

#line  1675 "VBNET.ATG" 
				op = BinaryOperatorType.Add; 
			} else if (la.kind == 15) {
				lexer.NextToken();

#line  1676 "VBNET.ATG" 
				op = BinaryOperatorType.Subtract; 
			} else {
				lexer.NextToken();

#line  1677 "VBNET.ATG" 
				op = BinaryOperatorType.Concat; 
			}
			MultiplicativeExpr(
#line  1679 "VBNET.ATG" 
out expr);

#line  1679 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void MultiplicativeExpr(
#line  1683 "VBNET.ATG" 
out Expression outExpr) {

#line  1685 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		UnaryExpr(
#line  1688 "VBNET.ATG" 
out outExpr);
		while (StartOf(24)) {
			if (la.kind == 16) {
				lexer.NextToken();

#line  1691 "VBNET.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else if (la.kind == 17) {
				lexer.NextToken();

#line  1692 "VBNET.ATG" 
				op = BinaryOperatorType.Divide; 
			} else if (la.kind == 18) {
				lexer.NextToken();

#line  1693 "VBNET.ATG" 
				op = BinaryOperatorType.DivideInteger; 
			} else if (la.kind == 121) {
				lexer.NextToken();

#line  1694 "VBNET.ATG" 
				op = BinaryOperatorType.Modulus; 
			} else {
				lexer.NextToken();

#line  1695 "VBNET.ATG" 
				op = BinaryOperatorType.Power; 
			}
			UnaryExpr(
#line  1697 "VBNET.ATG" 
out expr);

#line  1697 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void ArrayTypeName(
#line  1770 "VBNET.ATG" 
out TypeReference typeref) {

#line  1772 "VBNET.ATG" 
		ArrayList rank = null;
		
		NonArrayTypeName(
#line  1774 "VBNET.ATG" 
out typeref);
		ArrayInitializationModifiers(
#line  1775 "VBNET.ATG" 
out rank);

#line  1777 "VBNET.ATG" 
		typeref = new TypeReference(typeref == null ? "UNKNOWN" : typeref.Type, rank);
		
	}

	void ArgumentList(
#line  1728 "VBNET.ATG" 
out ArrayList arguments) {

#line  1730 "VBNET.ATG" 
		arguments = new ArrayList();
		Expression expr = null;
		
		if (StartOf(18)) {
			Argument(
#line  1734 "VBNET.ATG" 
out expr);

#line  1734 "VBNET.ATG" 
			arguments.Add(expr); 
			while (la.kind == 12) {
				lexer.NextToken();
				Argument(
#line  1737 "VBNET.ATG" 
out expr);

#line  1737 "VBNET.ATG" 
				arguments.Add(expr); 
			}
		}
	}

	void NonArrayTypeName(
#line  1782 "VBNET.ATG" 
out TypeReference typeref) {

#line  1784 "VBNET.ATG" 
		string name;
		typeref = null;
		
		if (StartOf(9)) {
			Qualident(
#line  1787 "VBNET.ATG" 
out name);

#line  1787 "VBNET.ATG" 
			typeref = new TypeReference(name); 
		} else if (la.kind == 134) {
			lexer.NextToken();

#line  1788 "VBNET.ATG" 
			typeref = new TypeReference("System.Object"); 
		} else if (StartOf(25)) {
			PrimitiveTypeName(
#line  1789 "VBNET.ATG" 
out name);

#line  1789 "VBNET.ATG" 
			typeref = new TypeReference(name); 
		} else SynErr(224);
	}

	void ArrayInitializationModifiers(
#line  1792 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  1794 "VBNET.ATG" 
		arrayModifiers = new ArrayList();
		ArrayList dim = new ArrayList();
		
		while (
#line  1798 "VBNET.ATG" 
IsDims()) {
			Expect(25);
			if (StartOf(18)) {
				InitializationRankList(
#line  1799 "VBNET.ATG" 
out dim);
			}

#line  1801 "VBNET.ATG" 
			arrayModifiers.Add(dim);
			
			Expect(26);
		}

#line  1806 "VBNET.ATG" 
		if(arrayModifiers.Count == 0) {
		 arrayModifiers = null;
		}
		
	}

	void RankList(
#line  1834 "VBNET.ATG" 
out int i) {

#line  1835 "VBNET.ATG" 
		i = 0; 
		while (la.kind == 12) {
			lexer.NextToken();

#line  1836 "VBNET.ATG" 
			++i; 
		}
	}

	void Attribute(
#line  1861 "VBNET.ATG" 
out ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attribute) {

#line  1862 "VBNET.ATG" 
		string qualident; 
		Qualident(
#line  1863 "VBNET.ATG" 
out qualident);

#line  1865 "VBNET.ATG" 
		ArrayList positional = new ArrayList();
		ArrayList named      = new ArrayList();
		string name = qualident;
		
		if (la.kind == 25) {
			AttributeArguments(
#line  1869 "VBNET.ATG" 
ref positional, ref named);
		}

#line  1871 "VBNET.ATG" 
		attribute  = new ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute(name, positional, named);
		
	}

	void AttributeArguments(
#line  1876 "VBNET.ATG" 
ref ArrayList positional, ref ArrayList named) {

#line  1878 "VBNET.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(25);
		if (
#line  1884 "VBNET.ATG" 
IsNotClosingParenthesis()) {
			if (
#line  1886 "VBNET.ATG" 
IsNamedAssign()) {

#line  1886 "VBNET.ATG" 
				nameFound = true; 
				IdentifierOrKeyword(
#line  1887 "VBNET.ATG" 
out name);
				if (la.kind == 13) {
					lexer.NextToken();
				}
				Expect(11);
			}
			Expr(
#line  1889 "VBNET.ATG" 
out expr);

#line  1891 "VBNET.ATG" 
			if(name == "") positional.Add(expr);
			else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			
			while (la.kind == 12) {
				lexer.NextToken();
				if (
#line  1897 "VBNET.ATG" 
IsNamedAssign()) {

#line  1897 "VBNET.ATG" 
					nameFound = true; 
					IdentifierOrKeyword(
#line  1898 "VBNET.ATG" 
out name);
					if (la.kind == 13) {
						lexer.NextToken();
					}
					Expect(11);
				} else if (StartOf(18)) {

#line  1900 "VBNET.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(225);
				Expr(
#line  1901 "VBNET.ATG" 
out expr);

#line  1901 "VBNET.ATG" 
				if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				
			}
		}
		Expect(26);
	}

	void FormalParameter(
#line  1971 "VBNET.ATG" 
out ParameterDeclarationExpression p) {

#line  1973 "VBNET.ATG" 
		TypeReference type = null;
		ParamModifiers mod = new ParamModifiers(this);
		Expression expr = null;
		p = null;
		ArrayList arrayModifiers = null;
		
		while (StartOf(26)) {
			ParameterModifier(
#line  1979 "VBNET.ATG" 
mod);
		}
		Identifier();

#line  1980 "VBNET.ATG" 
		string parameterName = t.val; 
		if (
#line  1981 "VBNET.ATG" 
IsRank()) {
			ArrayTypeModifiers(
#line  1981 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 49) {
			lexer.NextToken();
			TypeName(
#line  1982 "VBNET.ATG" 
out type);
		}

#line  1984 "VBNET.ATG" 
		if(type != null) {
		if (arrayModifiers != null) {
			if (type.RankSpecifier != null) {
				Error("array rank only allowed one time");
			} else {
				type.RankSpecifier = arrayModifiers;
			}
		}
		} else {
			type = new TypeReference("System.Object", arrayModifiers);
		}
		
		if (la.kind == 11) {
			lexer.NextToken();
			Expr(
#line  1996 "VBNET.ATG" 
out expr);
		}

#line  1998 "VBNET.ATG" 
		mod.Check();
		p = new ParameterDeclarationExpression(type, parameterName, mod, expr);
		
	}

	void ParameterModifier(
#line  2708 "VBNET.ATG" 
ParamModifiers m) {
		if (la.kind == 56) {
			lexer.NextToken();

#line  2709 "VBNET.ATG" 
			m.Add(ParamModifier.ByVal); 
		} else if (la.kind == 54) {
			lexer.NextToken();

#line  2710 "VBNET.ATG" 
			m.Add(ParamModifier.ByRef); 
		} else if (la.kind == 138) {
			lexer.NextToken();

#line  2711 "VBNET.ATG" 
			m.Add(ParamModifier.Optional); 
		} else if (la.kind == 145) {
			lexer.NextToken();

#line  2712 "VBNET.ATG" 
			m.Add(ParamModifier.ParamArray); 
		} else SynErr(226);
	}

	void Statement() {

#line  2023 "VBNET.ATG" 
		Statement stmt;
		string label = String.Empty;
		
		
		if (
#line  2027 "VBNET.ATG" 
IsLabel()) {
			LabelName(
#line  2027 "VBNET.ATG" 
out label);

#line  2029 "VBNET.ATG" 
			labelStatement = new LabelStatement(t.val);
			compilationUnit.AddChild(labelStatement);
			
			Expect(13);
			if (StartOf(15)) {

#line  2032 "VBNET.ATG" 
				isLabel = true; 
				Statement();
			}
		} else if (StartOf(27)) {
			EmbeddedStatement(
#line  2033 "VBNET.ATG" 
out stmt);

#line  2033 "VBNET.ATG" 
			updateLabelStatement(stmt); 
		} else if (StartOf(28)) {
			LocalDeclarationStatement(
#line  2034 "VBNET.ATG" 
out stmt);

#line  2034 "VBNET.ATG" 
			updateLabelStatement(stmt); 
		} else SynErr(227);
	}

	void LabelName(
#line  2396 "VBNET.ATG" 
out string name) {

#line  2398 "VBNET.ATG" 
		name = String.Empty;
		
		if (StartOf(9)) {
			Identifier();

#line  2400 "VBNET.ATG" 
			name = t.val; 
		} else if (la.kind == 5) {
			lexer.NextToken();

#line  2401 "VBNET.ATG" 
			name = t.val; 
		} else SynErr(228);
	}

	void EmbeddedStatement(
#line  2071 "VBNET.ATG" 
out Statement statement) {

#line  2073 "VBNET.ATG" 
		Statement embeddedStatement = null;
		statement = null;
		Expression expr = null;
		string name = String.Empty;
		ArrayList p = null;
		
		switch (la.kind) {
		case 95: {
			lexer.NextToken();

#line  2079 "VBNET.ATG" 
			ExitType exitType = ExitType.None; 
			switch (la.kind) {
			case 169: {
				lexer.NextToken();

#line  2081 "VBNET.ATG" 
				exitType = ExitType.Sub; 
				break;
			}
			case 101: {
				lexer.NextToken();

#line  2083 "VBNET.ATG" 
				exitType = ExitType.Function; 
				break;
			}
			case 148: {
				lexer.NextToken();

#line  2085 "VBNET.ATG" 
				exitType = ExitType.Property; 
				break;
			}
			case 84: {
				lexer.NextToken();

#line  2087 "VBNET.ATG" 
				exitType = ExitType.Do; 
				break;
			}
			case 99: {
				lexer.NextToken();

#line  2089 "VBNET.ATG" 
				exitType = ExitType.For; 
				break;
			}
			case 176: {
				lexer.NextToken();

#line  2091 "VBNET.ATG" 
				exitType = ExitType.Try; 
				break;
			}
			case 183: {
				lexer.NextToken();

#line  2093 "VBNET.ATG" 
				exitType = ExitType.While; 
				break;
			}
			case 157: {
				lexer.NextToken();

#line  2095 "VBNET.ATG" 
				exitType = ExitType.Select; 
				break;
			}
			default: SynErr(229); break;
			}

#line  2097 "VBNET.ATG" 
			statement = new ExitStatement(exitType); 
			break;
		}
		case 176: {
			TryStatement(
#line  2098 "VBNET.ATG" 
out statement);
			break;
		}
		case 173: {
			lexer.NextToken();
			if (StartOf(18)) {
				Expr(
#line  2100 "VBNET.ATG" 
out expr);
			}

#line  2100 "VBNET.ATG" 
			statement = new ThrowStatement(expr); 
			break;
		}
		case 156: {
			lexer.NextToken();
			if (StartOf(18)) {
				Expr(
#line  2102 "VBNET.ATG" 
out expr);
			}

#line  2102 "VBNET.ATG" 
			statement = new ReturnStatement(expr); 
			break;
		}
		case 170: {
			lexer.NextToken();
			Expr(
#line  2104 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  2104 "VBNET.ATG" 
out embeddedStatement);
			Expect(89);
			Expect(170);

#line  2105 "VBNET.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
			break;
		}
		case 151: {
			lexer.NextToken();
			Identifier();

#line  2107 "VBNET.ATG" 
			name = t.val; 
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(17)) {
					ArgumentList(
#line  2108 "VBNET.ATG" 
out p);
				}
				Expect(26);
			}

#line  2109 "VBNET.ATG" 
			statement = new RaiseEventStatement(name, p); 
			break;
		}
		case 184: {
			WithStatement(
#line  2111 "VBNET.ATG" 
out statement);
			break;
		}
		case 43: {
			lexer.NextToken();

#line  2113 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  2114 "VBNET.ATG" 
out expr);
			Expect(12);
			Expr(
#line  2114 "VBNET.ATG" 
out handlerExpr);

#line  2116 "VBNET.ATG" 
			statement = new AddHandlerStatement(expr, handlerExpr);
			
			break;
		}
		case 154: {
			lexer.NextToken();

#line  2119 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  2120 "VBNET.ATG" 
out expr);
			Expect(12);
			Expr(
#line  2120 "VBNET.ATG" 
out handlerExpr);

#line  2122 "VBNET.ATG" 
			statement = new RemoveHandlerStatement(expr, handlerExpr);
			
			break;
		}
		case 183: {
			lexer.NextToken();
			Expr(
#line  2125 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  2126 "VBNET.ATG" 
out embeddedStatement);
			Expect(89);
			Expect(183);

#line  2128 "VBNET.ATG" 
			statement = new WhileStatement(expr, embeddedStatement);
			
			break;
		}
		case 84: {
			lexer.NextToken();

#line  2133 "VBNET.ATG" 
			ConditionType conditionType = ConditionType.None;
			
			if (la.kind == 179 || la.kind == 183) {
				WhileOrUntil(
#line  2136 "VBNET.ATG" 
out conditionType);
				Expr(
#line  2136 "VBNET.ATG" 
out expr);
				EndOfStmt();
				Block(
#line  2137 "VBNET.ATG" 
out embeddedStatement);
				Expect(119);

#line  2140 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, embeddedStatement, conditionType, ConditionPosition.Start);
				
			} else if (la.kind == 1 || la.kind == 13) {
				EndOfStmt();
				Block(
#line  2144 "VBNET.ATG" 
out embeddedStatement);
				Expect(119);
				if (la.kind == 179 || la.kind == 183) {
					WhileOrUntil(
#line  2145 "VBNET.ATG" 
out conditionType);
					Expr(
#line  2145 "VBNET.ATG" 
out expr);
				}

#line  2147 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, embeddedStatement, conditionType, ConditionPosition.End);
				
			} else SynErr(230);
			break;
		}
		case 99: {
			lexer.NextToken();

#line  2152 "VBNET.ATG" 
			Expression group = null;
			LoopControlVariableExpression loopControlExpr = null;
			
			if (la.kind == 86) {
				lexer.NextToken();
				LoopControlVariable(
#line  2157 "VBNET.ATG" 
out loopControlExpr);
				Expect(110);
				Expr(
#line  2158 "VBNET.ATG" 
out group);
				EndOfStmt();
				Block(
#line  2159 "VBNET.ATG" 
out embeddedStatement);
				Expect(129);
				if (StartOf(18)) {
					Expr(
#line  2160 "VBNET.ATG" 
out expr);
				}

#line  2162 "VBNET.ATG" 
				statement = new ForeachStatement(loopControlExpr, group, embeddedStatement, expr);
				
			} else if (StartOf(9)) {

#line  2166 "VBNET.ATG" 
				Expression start = null;
				Expression end = null;
				Expression step = null;
				Expression nextExpr = null;
				ArrayList nextExpressions = null;
				
				LoopControlVariable(
#line  2172 "VBNET.ATG" 
out loopControlExpr);
				Expect(11);
				Expr(
#line  2173 "VBNET.ATG" 
out start);
				Expect(174);
				Expr(
#line  2173 "VBNET.ATG" 
out end);
				if (la.kind == 164) {
					lexer.NextToken();
					Expr(
#line  2173 "VBNET.ATG" 
out step);
				}
				EndOfStmt();
				Block(
#line  2174 "VBNET.ATG" 
out embeddedStatement);
				Expect(129);
				if (StartOf(18)) {
					Expr(
#line  2177 "VBNET.ATG" 
out nextExpr);

#line  2177 "VBNET.ATG" 
					nextExpressions = new ArrayList(); nextExpressions.Add(nextExpr); 
					while (la.kind == 12) {
						lexer.NextToken();
						Expr(
#line  2178 "VBNET.ATG" 
out nextExpr);

#line  2178 "VBNET.ATG" 
						nextExpressions.Add(nextExpr); 
					}
				}

#line  2181 "VBNET.ATG" 
				statement = new ForStatement(loopControlExpr, start, end, step, embeddedStatement, nextExpressions);
				
			} else SynErr(231);
			break;
		}
		case 93: {
			lexer.NextToken();
			Expr(
#line  2185 "VBNET.ATG" 
out expr);

#line  2185 "VBNET.ATG" 
			statement = new ErrorStatement(expr); 
			break;
		}
		case 153: {
			lexer.NextToken();

#line  2187 "VBNET.ATG" 
			Expression clause = null; 
			if (la.kind == 146) {
				lexer.NextToken();
			}
			Expr(
#line  2188 "VBNET.ATG" 
out clause);

#line  2190 "VBNET.ATG" 
			ArrayList clauses = new ArrayList();
			clauses.Add(clause);
			/*ReDimStatement reDimStatement = new ReDimStatement(clauses);*/
			
			while (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  2194 "VBNET.ATG" 
out clause);

#line  2194 "VBNET.ATG" 
				clauses.Add(clause); 
			}
			break;
		}
		case 92: {
			lexer.NextToken();
			Expr(
#line  2197 "VBNET.ATG" 
out expr);

#line  2199 "VBNET.ATG" 
			ArrayList arrays = new ArrayList();
			arrays.Add(expr);
			EraseStatement eraseStatement = new EraseStatement(arrays);
			
			
			while (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  2204 "VBNET.ATG" 
out expr);

#line  2204 "VBNET.ATG" 
				arrays.Add(expr); 
			}

#line  2205 "VBNET.ATG" 
			statement = eraseStatement; 
			break;
		}
		case 165: {
			lexer.NextToken();

#line  2207 "VBNET.ATG" 
			statement = new StopStatement(); 
			break;
		}
		case 107: {
			lexer.NextToken();
			Expr(
#line  2209 "VBNET.ATG" 
out expr);
			if (la.kind == 172) {
				lexer.NextToken();
			}
			if (
#line  2211 "VBNET.ATG" 
IsEndStmtAhead()) {
				Expect(89);

#line  2211 "VBNET.ATG" 
				statement = new IfStatement(expr, new EndStatement()); 
			} else if (la.kind == 1 || la.kind == 13) {
				EndOfStmt();
				Block(
#line  2214 "VBNET.ATG" 
out embeddedStatement);

#line  2216 "VBNET.ATG" 
				ArrayList elseIfSections = new ArrayList();
				IfStatement ifStatement = new IfStatement(expr, embeddedStatement);
				
				while (la.kind == 88 || 
#line  2221 "VBNET.ATG" 
IsElseIf()) {
					if (
#line  2221 "VBNET.ATG" 
IsElseIf()) {
						Expect(87);
						Expect(107);
					} else {
						lexer.NextToken();
					}

#line  2224 "VBNET.ATG" 
					Expression condition = null; Statement block = null; 
					Expr(
#line  2225 "VBNET.ATG" 
out condition);
					if (la.kind == 172) {
						lexer.NextToken();
					}
					EndOfStmt();
					Block(
#line  2226 "VBNET.ATG" 
out block);

#line  2228 "VBNET.ATG" 
					ElseIfSection elseIfSection = new ElseIfSection(condition, block);
					elseIfSections.Add(elseIfSection);
					
				}
				if (la.kind == 87) {
					lexer.NextToken();
					EndOfStmt();
					Block(
#line  2234 "VBNET.ATG" 
out embeddedStatement);

#line  2236 "VBNET.ATG" 
					ifStatement.EmbeddedElseStatement = embeddedStatement;
					
				}
				Expect(89);
				Expect(107);

#line  2240 "VBNET.ATG" 
				ifStatement.ElseIfStatements = elseIfSections;
				statement = ifStatement;
				
			} else if (StartOf(27)) {
				EmbeddedStatement(
#line  2244 "VBNET.ATG" 
out embeddedStatement);

#line  2246 "VBNET.ATG" 
				SimpleIfStatement ifStatement = new SimpleIfStatement(expr);
				ArrayList statements = new ArrayList();
				statements.Add(embeddedStatement);
				ifStatement.Statements = statements;
				
				while (la.kind == 13) {
					lexer.NextToken();
					EmbeddedStatement(
#line  2251 "VBNET.ATG" 
out embeddedStatement);

#line  2251 "VBNET.ATG" 
					statements.Add(embeddedStatement); 
				}
				if (la.kind == 87) {
					lexer.NextToken();
					if (StartOf(27)) {
						EmbeddedStatement(
#line  2253 "VBNET.ATG" 
out embeddedStatement);
					}

#line  2255 "VBNET.ATG" 
					ArrayList elseStatements = new ArrayList();
					elseStatements.Add(embeddedStatement);
					ifStatement.ElseStatements = elseStatements;
					
					while (la.kind == 13) {
						lexer.NextToken();
						EmbeddedStatement(
#line  2260 "VBNET.ATG" 
out embeddedStatement);

#line  2261 "VBNET.ATG" 
						elseStatements.Add(embeddedStatement); 
					}
				}

#line  2264 "VBNET.ATG" 
				statement = ifStatement; 
			} else SynErr(232);
			break;
		}
		case 157: {
			lexer.NextToken();
			if (la.kind == 58) {
				lexer.NextToken();
			}
			Expr(
#line  2267 "VBNET.ATG" 
out expr);
			EndOfStmt();

#line  2269 "VBNET.ATG" 
			ArrayList selectSections = new ArrayList();
			Statement block = null;
			
			while (la.kind == 58) {

#line  2273 "VBNET.ATG" 
				ArrayList caseClauses = null; 
				lexer.NextToken();
				CaseClauses(
#line  2274 "VBNET.ATG" 
out caseClauses);
				if (
#line  2274 "VBNET.ATG" 
IsNotStatementSeparator()) {
					lexer.NextToken();
				}
				EndOfStmt();

#line  2276 "VBNET.ATG" 
				SelectSection selectSection = new SelectSection();
				selectSection.CaseClauses = caseClauses;
				compilationUnit.BlockStart(selectSection);
				
				Block(
#line  2280 "VBNET.ATG" 
out block);

#line  2282 "VBNET.ATG" 
				selectSection.EmbeddedStatement = block;
				compilationUnit.BlockEnd();
				selectSections.Add(selectSection);
				
			}

#line  2287 "VBNET.ATG" 
			statement = new SelectStatement(expr, selectSections); 
			Expect(89);
			Expect(157);
			break;
		}
		case 136: {

#line  2289 "VBNET.ATG" 
			OnErrorStatement onErrorStatement = null; 
			OnErrorStatement(
#line  2290 "VBNET.ATG" 
out onErrorStatement);

#line  2290 "VBNET.ATG" 
			statement = onErrorStatement; 
			break;
		}
		case 105: {

#line  2291 "VBNET.ATG" 
			GoToStatement goToStatement = null; 
			GoToStatement(
#line  2292 "VBNET.ATG" 
out goToStatement);

#line  2292 "VBNET.ATG" 
			statement = goToStatement; 
			break;
		}
		case 155: {

#line  2293 "VBNET.ATG" 
			ResumeStatement resumeStatement = null; 
			ResumeStatement(
#line  2294 "VBNET.ATG" 
out resumeStatement);

#line  2294 "VBNET.ATG" 
			statement = resumeStatement; 
			break;
		}
		case 2: case 3: case 4: case 5: case 6: case 7: case 8: case 9: case 10: case 14: case 15: case 16: case 25: case 44: case 52: case 53: case 55: case 60: case 61: case 62: case 63: case 64: case 65: case 66: case 67: case 69: case 70: case 71: case 73: case 74: case 75: case 76: case 77: case 78: case 83: case 85: case 97: case 103: case 112: case 118: case 120: case 125: case 126: case 128: case 131: case 161: case 162: case 167: case 171: case 175: case 177: {

#line  2297 "VBNET.ATG" 
			Expression val = null;
			AssignmentOperatorType op;
			
			bool mustBeAssignment = la.kind == Tokens.Plus  || la.kind == Tokens.Minus ||
			                        la.kind == Tokens.Not   || la.kind == Tokens.Times;
			
			UnaryExpr(
#line  2303 "VBNET.ATG" 
out expr);
			if (StartOf(14)) {
				AssignmentOperator(
#line  2305 "VBNET.ATG" 
out op);
				Expr(
#line  2305 "VBNET.ATG" 
out val);

#line  2305 "VBNET.ATG" 
				expr = new AssignmentExpression(expr, op, val); 
			} else if (la.kind == 1 || la.kind == 13 || la.kind == 87) {

#line  2306 "VBNET.ATG" 
				if (mustBeAssignment) Error("error in assignment."); 
			} else SynErr(233);

#line  2309 "VBNET.ATG" 
			// a field reference expression that stands alone is a
			// invocation expression without parantheses and arguments
			if(expr is FieldReferenceOrInvocationExpression) {
				expr = new InvocationExpression(expr, new ArrayList());
			}
			statement = new StatementExpression(expr);
			
			break;
		}
		case 57: {
			lexer.NextToken();
			UnaryExpr(
#line  2316 "VBNET.ATG" 
out expr);

#line  2316 "VBNET.ATG" 
			statement = new StatementExpression(expr); 
			break;
		}
		default: SynErr(234); break;
		}
	}

	void LocalDeclarationStatement(
#line  2038 "VBNET.ATG" 
out Statement statement) {

#line  2040 "VBNET.ATG" 
		Modifiers m = new Modifiers(this);
		ArrayList vars = new ArrayList();
		LocalVariableDeclaration localVariableDeclaration;
		bool dimfound = false;
		
		while (la.kind == 72 || la.kind == 82 || la.kind == 163) {
			if (la.kind == 72) {
				lexer.NextToken();

#line  2047 "VBNET.ATG" 
				m.Add(Modifier.Constant); 
			} else if (la.kind == 163) {
				lexer.NextToken();

#line  2048 "VBNET.ATG" 
				m.Add(Modifier.Static); 
			} else {
				lexer.NextToken();

#line  2049 "VBNET.ATG" 
				dimfound = true; 
			}
		}

#line  2052 "VBNET.ATG" 
		if(dimfound && (m.Modifier & Modifier.Constant) != 0) {
		Error("Dim is not allowed on constants.");
		}
		
		if(m.isNone && dimfound == false) {
			Error("Const, Dim or Static expected");
		}
		
		localVariableDeclaration = new LocalVariableDeclaration(m.Modifier);
		localVariableDeclaration.StartLocation = t.Location;
		
		VariableDeclarator(
#line  2063 "VBNET.ATG" 
vars);
		while (la.kind == 12) {
			lexer.NextToken();
			VariableDeclarator(
#line  2064 "VBNET.ATG" 
vars);
		}

#line  2066 "VBNET.ATG" 
		localVariableDeclaration.Variables = vars;
		statement = localVariableDeclaration;
		
	}

	void TryStatement(
#line  2489 "VBNET.ATG" 
out Statement tryStatement) {

#line  2491 "VBNET.ATG" 
		Statement blockStmt = null, finallyStmt = null;
		ArrayList catchClauses = null;
		
		Expect(176);
		EndOfStmt();
		Block(
#line  2495 "VBNET.ATG" 
out blockStmt);
		if (la.kind == 59 || la.kind == 89 || la.kind == 98) {
			CatchClauses(
#line  2497 "VBNET.ATG" 
out catchClauses);
			if (la.kind == 98) {
				lexer.NextToken();
				EndOfStmt();
				Block(
#line  2498 "VBNET.ATG" 
out finallyStmt);
			}
		} else if (la.kind == 98) {
			lexer.NextToken();
			EndOfStmt();
			Block(
#line  2499 "VBNET.ATG" 
out finallyStmt);
		} else SynErr(235);
		Expect(89);
		Expect(176);

#line  2503 "VBNET.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		
	}

	void WithStatement(
#line  2467 "VBNET.ATG" 
out Statement withStatement) {

#line  2469 "VBNET.ATG" 
		Statement blockStmt = null;
		Expression expr = null;
		
		Expect(184);

#line  2472 "VBNET.ATG" 
		Point start = t.Location; 
		Expr(
#line  2473 "VBNET.ATG" 
out expr);
		EndOfStmt();

#line  2475 "VBNET.ATG" 
		withStatement = new WithStatement(expr);
		withStatement.StartLocation = start;
		withStatements.Push(withStatement);
		
		Block(
#line  2479 "VBNET.ATG" 
out blockStmt);

#line  2481 "VBNET.ATG" 
		((WithStatement)withStatement).Body = (BlockStatement)blockStmt;
		withStatements.Pop();
		
		Expect(89);
		Expect(184);

#line  2485 "VBNET.ATG" 
		withStatement.EndLocation = t.Location; 
	}

	void WhileOrUntil(
#line  2460 "VBNET.ATG" 
out ConditionType conditionType) {

#line  2461 "VBNET.ATG" 
		conditionType = ConditionType.None; 
		if (la.kind == 183) {
			lexer.NextToken();

#line  2462 "VBNET.ATG" 
			conditionType = ConditionType.While; 
		} else if (la.kind == 179) {
			lexer.NextToken();

#line  2463 "VBNET.ATG" 
			conditionType = ConditionType.Until; 
		} else SynErr(236);
	}

	void LoopControlVariable(
#line  2320 "VBNET.ATG" 
out LoopControlVariableExpression loopExpr) {

#line  2322 "VBNET.ATG" 
		loopExpr = null;
		//Expression expr = null;
		TypeReference type = null;
		ArrayList arrayModifiers = null;
		string name;
		
		Qualident(
#line  2328 "VBNET.ATG" 
out name);
		if (
#line  2329 "VBNET.ATG" 
IsRank()) {
			ArrayTypeModifiers(
#line  2329 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 49) {
			lexer.NextToken();
			TypeName(
#line  2330 "VBNET.ATG" 
out type);

#line  2330 "VBNET.ATG" 
			if (name.IndexOf('.') > 0) { Error("No type def for 'for each' member indexer allowed."); } 
		}

#line  2332 "VBNET.ATG" 
		if(type != null) {
		if(type.RankSpecifier != null && arrayModifiers != null) {
			Error("array rank only allowed one time");
		} else {
			type.RankSpecifier = arrayModifiers;
		}
		} else {
			type = new TypeReference("Integer", arrayModifiers);
		}
		loopExpr = new LoopControlVariableExpression(name, type);
		
	}

	void CaseClauses(
#line  2416 "VBNET.ATG" 
out ArrayList caseClauses) {

#line  2418 "VBNET.ATG" 
		caseClauses = null;
		CaseClause caseClause = null;
		
		CaseClause(
#line  2421 "VBNET.ATG" 
out caseClause);

#line  2423 "VBNET.ATG" 
		caseClauses = new ArrayList();
		caseClauses.Add(caseClause);
		
		while (la.kind == 12) {
			lexer.NextToken();
			CaseClause(
#line  2426 "VBNET.ATG" 
out caseClause);

#line  2426 "VBNET.ATG" 
			caseClauses.Add(caseClause); 
		}
	}

	void OnErrorStatement(
#line  2346 "VBNET.ATG" 
out OnErrorStatement stmt) {

#line  2348 "VBNET.ATG" 
		stmt = null;
		GoToStatement goToStatement = null;
		
		Expect(136);
		Expect(93);
		if (
#line  2354 "VBNET.ATG" 
IsNegativeLabelName()) {
			Expect(105);
			Expect(15);
			Expect(5);

#line  2356 "VBNET.ATG" 
			long intLabel = Int64.Parse(t.val);
			if(intLabel != 1) {
				Error("invalid label in on error statement.");
			}
			stmt = new OnErrorStatement(new GoToStatement((intLabel * -1).ToString()));
			
		} else if (la.kind == 105) {
			GoToStatement(
#line  2362 "VBNET.ATG" 
out goToStatement);

#line  2364 "VBNET.ATG" 
			string val = goToStatement.LabelName;
			
			// if value is numeric, make sure that is 0
			try {
				long intLabel = Int64.Parse(val);
				if(intLabel != 0) {
					Error("invalid label in on error statement.");
				}
			} catch {
			}
			stmt = new OnErrorStatement(goToStatement);
			
		} else if (la.kind == 155) {
			lexer.NextToken();
			Expect(129);

#line  2378 "VBNET.ATG" 
			stmt = new OnErrorStatement(new ResumeStatement(true));
			
		} else SynErr(237);
	}

	void GoToStatement(
#line  2384 "VBNET.ATG" 
out GoToStatement goToStatement) {

#line  2386 "VBNET.ATG" 
		string label = String.Empty;
		
		Expect(105);
		LabelName(
#line  2389 "VBNET.ATG" 
out label);

#line  2391 "VBNET.ATG" 
		goToStatement = new GoToStatement(label);
		
	}

	void ResumeStatement(
#line  2405 "VBNET.ATG" 
out ResumeStatement resumeStatement) {

#line  2407 "VBNET.ATG" 
		resumeStatement = null;
		string label = String.Empty;
		
		if (
#line  2410 "VBNET.ATG" 
IsResumeNext()) {
			Expect(155);
			Expect(129);

#line  2411 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(true); 
		} else if (la.kind == 155) {
			lexer.NextToken();
			if (StartOf(29)) {
				LabelName(
#line  2412 "VBNET.ATG" 
out label);
			}

#line  2412 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(label); 
		} else SynErr(238);
	}

	void CaseClause(
#line  2430 "VBNET.ATG" 
out CaseClause caseClause) {

#line  2432 "VBNET.ATG" 
		Expression expr = null;
		Expression sexpr = null;
		BinaryOperatorType op = BinaryOperatorType.None;
		caseClause = null;
		
		if (la.kind == 87) {
			lexer.NextToken();

#line  2438 "VBNET.ATG" 
			caseClause = new CaseClause(true); 
		} else if (StartOf(30)) {
			if (la.kind == 114) {
				lexer.NextToken();
			}
			switch (la.kind) {
			case 28: {
				lexer.NextToken();

#line  2442 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 27: {
				lexer.NextToken();

#line  2443 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 31: {
				lexer.NextToken();

#line  2444 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 30: {
				lexer.NextToken();

#line  2445 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 11: {
				lexer.NextToken();

#line  2446 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 29: {
				lexer.NextToken();

#line  2447 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			default: SynErr(239); break;
			}
			Expr(
#line  2449 "VBNET.ATG" 
out expr);

#line  2451 "VBNET.ATG" 
			caseClause = new CaseClause(op, expr);
			
		} else if (StartOf(18)) {
			Expr(
#line  2453 "VBNET.ATG" 
out expr);
			if (la.kind == 174) {
				lexer.NextToken();
				Expr(
#line  2453 "VBNET.ATG" 
out sexpr);
			}

#line  2455 "VBNET.ATG" 
			caseClause = new CaseClause(expr, sexpr);
			
		} else SynErr(240);
	}

	void CatchClauses(
#line  2508 "VBNET.ATG" 
out ArrayList catchClauses) {

#line  2510 "VBNET.ATG" 
		catchClauses = new ArrayList();
		TypeReference type = null;
		Statement blockStmt = null;
		Expression expr = null;
		string name = String.Empty;
		
		while (la.kind == 59) {
			lexer.NextToken();
			if (StartOf(9)) {
				Identifier();

#line  2518 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 49) {
					lexer.NextToken();
					TypeName(
#line  2518 "VBNET.ATG" 
out type);
				}
			}
			if (la.kind == 182) {
				lexer.NextToken();
				Expr(
#line  2519 "VBNET.ATG" 
out expr);
			}
			EndOfStmt();
			Block(
#line  2521 "VBNET.ATG" 
out blockStmt);

#line  2522 "VBNET.ATG" 
			catchClauses.Add(new CatchClause(type, name, blockStmt, expr)); 
		}
	}



	public void Parse(Lexer lexer)
	{
		this.errors = lexer.Errors;
		this.lexer = lexer;
		errors.SynErr = new ErrorCodeProc(SynErr);
		lexer.NextToken();
		VBNET();

	}

	void SynErr(int line, int col, int errorNumber)
	{
		errors.count++; 
		string s;
		switch (errorNumber) {
			case 0: s = "EOF expected"; break;
			case 1: s = "EOL expected"; break;
			case 2: s = "ident expected"; break;
			case 3: s = "LiteralString expected"; break;
			case 4: s = "LiteralCharacter expected"; break;
			case 5: s = "LiteralInteger expected"; break;
			case 6: s = "LiteralDouble expected"; break;
			case 7: s = "LiteralSingle expected"; break;
			case 8: s = "LiteralDecimal expected"; break;
			case 9: s = "LiteralDate expected"; break;
			case 10: s = "\".\" expected"; break;
			case 11: s = "\"=\" expected"; break;
			case 12: s = "\",\" expected"; break;
			case 13: s = "\":\" expected"; break;
			case 14: s = "\"+\" expected"; break;
			case 15: s = "\"-\" expected"; break;
			case 16: s = "\"*\" expected"; break;
			case 17: s = "\"/\" expected"; break;
			case 18: s = "\"\\\\\" expected"; break;
			case 19: s = "\"&\" expected"; break;
			case 20: s = "\"^\" expected"; break;
			case 21: s = "\"{\" expected"; break;
			case 22: s = "\"}\" expected"; break;
			case 23: s = "\"[\" expected"; break;
			case 24: s = "\"]\" expected"; break;
			case 25: s = "\"(\" expected"; break;
			case 26: s = "\")\" expected"; break;
			case 27: s = "\">\" expected"; break;
			case 28: s = "\"<\" expected"; break;
			case 29: s = "\"<>\" expected"; break;
			case 30: s = "\">=\" expected"; break;
			case 31: s = "\"<=\" expected"; break;
			case 32: s = "\"<<\" expected"; break;
			case 33: s = "\">>\" expected"; break;
			case 34: s = "\"+=\" expected"; break;
			case 35: s = "\"^=\" expected"; break;
			case 36: s = "\"-=\" expected"; break;
			case 37: s = "\"*=\" expected"; break;
			case 38: s = "\"/=\" expected"; break;
			case 39: s = "\"\\\\=\" expected"; break;
			case 40: s = "\"<<=\" expected"; break;
			case 41: s = "\">>=\" expected"; break;
			case 42: s = "\"&=\" expected"; break;
			case 43: s = "\"AddHandler\" expected"; break;
			case 44: s = "\"AddressOf\" expected"; break;
			case 45: s = "\"Alias\" expected"; break;
			case 46: s = "\"And\" expected"; break;
			case 47: s = "\"AndAlso\" expected"; break;
			case 48: s = "\"Ansi\" expected"; break;
			case 49: s = "\"As\" expected"; break;
			case 50: s = "\"Assembly\" expected"; break;
			case 51: s = "\"Auto\" expected"; break;
			case 52: s = "\"Binary\" expected"; break;
			case 53: s = "\"Boolean\" expected"; break;
			case 54: s = "\"ByRef\" expected"; break;
			case 55: s = "\"Byte\" expected"; break;
			case 56: s = "\"ByVal\" expected"; break;
			case 57: s = "\"Call\" expected"; break;
			case 58: s = "\"Case\" expected"; break;
			case 59: s = "\"Catch\" expected"; break;
			case 60: s = "\"CBool\" expected"; break;
			case 61: s = "\"CByte\" expected"; break;
			case 62: s = "\"CChar\" expected"; break;
			case 63: s = "\"CDate\" expected"; break;
			case 64: s = "\"CDbl\" expected"; break;
			case 65: s = "\"CDec\" expected"; break;
			case 66: s = "\"Char\" expected"; break;
			case 67: s = "\"CInt\" expected"; break;
			case 68: s = "\"Class\" expected"; break;
			case 69: s = "\"CLng\" expected"; break;
			case 70: s = "\"CObj\" expected"; break;
			case 71: s = "\"Compare\" expected"; break;
			case 72: s = "\"Const\" expected"; break;
			case 73: s = "\"CShort\" expected"; break;
			case 74: s = "\"CSng\" expected"; break;
			case 75: s = "\"CStr\" expected"; break;
			case 76: s = "\"CType\" expected"; break;
			case 77: s = "\"Date\" expected"; break;
			case 78: s = "\"Decimal\" expected"; break;
			case 79: s = "\"Declare\" expected"; break;
			case 80: s = "\"Default\" expected"; break;
			case 81: s = "\"Delegate\" expected"; break;
			case 82: s = "\"Dim\" expected"; break;
			case 83: s = "\"DirectCast\" expected"; break;
			case 84: s = "\"Do\" expected"; break;
			case 85: s = "\"Double\" expected"; break;
			case 86: s = "\"Each\" expected"; break;
			case 87: s = "\"Else\" expected"; break;
			case 88: s = "\"ElseIf\" expected"; break;
			case 89: s = "\"End\" expected"; break;
			case 90: s = "\"EndIf\" expected"; break;
			case 91: s = "\"Enum\" expected"; break;
			case 92: s = "\"Erase\" expected"; break;
			case 93: s = "\"Error\" expected"; break;
			case 94: s = "\"Event\" expected"; break;
			case 95: s = "\"Exit\" expected"; break;
			case 96: s = "\"Explicit\" expected"; break;
			case 97: s = "\"False\" expected"; break;
			case 98: s = "\"Finally\" expected"; break;
			case 99: s = "\"For\" expected"; break;
			case 100: s = "\"Friend\" expected"; break;
			case 101: s = "\"Function\" expected"; break;
			case 102: s = "\"Get\" expected"; break;
			case 103: s = "\"GetType\" expected"; break;
			case 104: s = "\"GoSub\" expected"; break;
			case 105: s = "\"GoTo\" expected"; break;
			case 106: s = "\"Handles\" expected"; break;
			case 107: s = "\"If\" expected"; break;
			case 108: s = "\"Implements\" expected"; break;
			case 109: s = "\"Imports\" expected"; break;
			case 110: s = "\"In\" expected"; break;
			case 111: s = "\"Inherits\" expected"; break;
			case 112: s = "\"Integer\" expected"; break;
			case 113: s = "\"Interface\" expected"; break;
			case 114: s = "\"Is\" expected"; break;
			case 115: s = "\"Let\" expected"; break;
			case 116: s = "\"Lib\" expected"; break;
			case 117: s = "\"Like\" expected"; break;
			case 118: s = "\"Long\" expected"; break;
			case 119: s = "\"Loop\" expected"; break;
			case 120: s = "\"Me\" expected"; break;
			case 121: s = "\"Mod\" expected"; break;
			case 122: s = "\"Module\" expected"; break;
			case 123: s = "\"MustInherit\" expected"; break;
			case 124: s = "\"MustOverride\" expected"; break;
			case 125: s = "\"MyBase\" expected"; break;
			case 126: s = "\"MyClass\" expected"; break;
			case 127: s = "\"Namespace\" expected"; break;
			case 128: s = "\"New\" expected"; break;
			case 129: s = "\"Next\" expected"; break;
			case 130: s = "\"Not\" expected"; break;
			case 131: s = "\"Nothing\" expected"; break;
			case 132: s = "\"NotInheritable\" expected"; break;
			case 133: s = "\"NotOverridable\" expected"; break;
			case 134: s = "\"Object\" expected"; break;
			case 135: s = "\"Off\" expected"; break;
			case 136: s = "\"On\" expected"; break;
			case 137: s = "\"Option\" expected"; break;
			case 138: s = "\"Optional\" expected"; break;
			case 139: s = "\"Or\" expected"; break;
			case 140: s = "\"OrElse\" expected"; break;
			case 141: s = "\"Overloads\" expected"; break;
			case 142: s = "\"Overridable\" expected"; break;
			case 143: s = "\"Override\" expected"; break;
			case 144: s = "\"Overrides\" expected"; break;
			case 145: s = "\"ParamArray\" expected"; break;
			case 146: s = "\"Preserve\" expected"; break;
			case 147: s = "\"Private\" expected"; break;
			case 148: s = "\"Property\" expected"; break;
			case 149: s = "\"Protected\" expected"; break;
			case 150: s = "\"Public\" expected"; break;
			case 151: s = "\"RaiseEvent\" expected"; break;
			case 152: s = "\"ReadOnly\" expected"; break;
			case 153: s = "\"ReDim\" expected"; break;
			case 154: s = "\"RemoveHandler\" expected"; break;
			case 155: s = "\"Resume\" expected"; break;
			case 156: s = "\"Return\" expected"; break;
			case 157: s = "\"Select\" expected"; break;
			case 158: s = "\"Set\" expected"; break;
			case 159: s = "\"Shadows\" expected"; break;
			case 160: s = "\"Shared\" expected"; break;
			case 161: s = "\"Short\" expected"; break;
			case 162: s = "\"Single\" expected"; break;
			case 163: s = "\"Static\" expected"; break;
			case 164: s = "\"Step\" expected"; break;
			case 165: s = "\"Stop\" expected"; break;
			case 166: s = "\"Strict\" expected"; break;
			case 167: s = "\"String\" expected"; break;
			case 168: s = "\"Structure\" expected"; break;
			case 169: s = "\"Sub\" expected"; break;
			case 170: s = "\"SyncLock\" expected"; break;
			case 171: s = "\"Text\" expected"; break;
			case 172: s = "\"Then\" expected"; break;
			case 173: s = "\"Throw\" expected"; break;
			case 174: s = "\"To\" expected"; break;
			case 175: s = "\"True\" expected"; break;
			case 176: s = "\"Try\" expected"; break;
			case 177: s = "\"TypeOf\" expected"; break;
			case 178: s = "\"Unicode\" expected"; break;
			case 179: s = "\"Until\" expected"; break;
			case 180: s = "\"Variant\" expected"; break;
			case 181: s = "\"Wend\" expected"; break;
			case 182: s = "\"When\" expected"; break;
			case 183: s = "\"While\" expected"; break;
			case 184: s = "\"With\" expected"; break;
			case 185: s = "\"WithEvents\" expected"; break;
			case 186: s = "\"WriteOnly\" expected"; break;
			case 187: s = "\"Xor\" expected"; break;
			case 188: s = "??? expected"; break;
			case 189: s = "invalid OptionStmt"; break;
			case 190: s = "invalid OptionStmt"; break;
			case 191: s = "invalid GlobalAttributeSection"; break;
			case 192: s = "invalid NamespaceMemberDecl"; break;
			case 193: s = "invalid OptionValue"; break;
			case 194: s = "invalid EndOfStmt"; break;
			case 195: s = "invalid Identifier"; break;
			case 196: s = "invalid TypeModifier"; break;
			case 197: s = "invalid NonModuleDeclaration"; break;
			case 198: s = "invalid NonModuleDeclaration"; break;
			case 199: s = "invalid PrimitiveTypeName"; break;
			case 200: s = "invalid MemberModifier"; break;
			case 201: s = "invalid StructureMemberDecl"; break;
			case 202: s = "invalid StructureMemberDecl"; break;
			case 203: s = "invalid StructureMemberDecl"; break;
			case 204: s = "invalid StructureMemberDecl"; break;
			case 205: s = "invalid StructureMemberDecl"; break;
			case 206: s = "invalid StructureMemberDecl"; break;
			case 207: s = "invalid StructureMemberDecl"; break;
			case 208: s = "invalid InterfaceMemberDecl"; break;
			case 209: s = "invalid InterfaceMemberDecl"; break;
			case 210: s = "invalid Charset"; break;
			case 211: s = "invalid VariableDeclarator"; break;
			case 212: s = "invalid AccessorDecls"; break;
			case 213: s = "invalid VariableInitializer"; break;
			case 214: s = "invalid EventMemberSpecifier"; break;
			case 215: s = "invalid EventMemberSpecifier"; break;
			case 216: s = "invalid AssignmentOperator"; break;
			case 217: s = "invalid SimpleExpr"; break;
			case 218: s = "invalid SimpleExpr"; break;
			case 219: s = "invalid SimpleExpr"; break;
			case 220: s = "invalid IdentifierOrKeyword"; break;
			case 221: s = "invalid CastTarget"; break;
			case 222: s = "invalid Argument"; break;
			case 223: s = "invalid RelationalExpr"; break;
			case 224: s = "invalid NonArrayTypeName"; break;
			case 225: s = "invalid AttributeArguments"; break;
			case 226: s = "invalid ParameterModifier"; break;
			case 227: s = "invalid Statement"; break;
			case 228: s = "invalid LabelName"; break;
			case 229: s = "invalid EmbeddedStatement"; break;
			case 230: s = "invalid EmbeddedStatement"; break;
			case 231: s = "invalid EmbeddedStatement"; break;
			case 232: s = "invalid EmbeddedStatement"; break;
			case 233: s = "invalid EmbeddedStatement"; break;
			case 234: s = "invalid EmbeddedStatement"; break;
			case 235: s = "invalid TryStatement"; break;
			case 236: s = "invalid WhileOrUntil"; break;
			case 237: s = "invalid OnErrorStatement"; break;
			case 238: s = "invalid ResumeStatement"; break;
			case 239: s = "invalid CaseClause"; break;
			case 240: s = "invalid CaseClause"; break;

			default: s = "error " + errorNumber; break;
		}
		errors.Error(line, col, s);
	}

	static bool[,] set = {
	{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,T, x,x,x,T, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, T,x,x,x, x,x,x,T, T,T,T,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,T,T,x, T,x,x,T, T,T,T,x, T,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,T,T,x, T,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,T,T,x, T,x,x,T, T,T,T,x, T,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,T,x,x, x,T,x,x, T,T,x,T, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,x,T, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, T,T,x,x, x,T,T,T, x,T,x,T, x,x,T,T, x,T,x,T, T,T,x,x, x,x,x,T, T,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,T,T,x, T,x,x,T, T,T,T,x, T,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,T,x,x, x,x,x,x, T,T,x,T, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,x,T, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, T,T,x,x, x,T,T,T, x,T,x,T, x,x,T,T, x,T,x,T, T,T,x,x, x,x,x,T, T,x,x,x, x,x},
	{x,T,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, T,T,x,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,T, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, T,T,x,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,T, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, T,T,x,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,T, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, T,T,x,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,T, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, T,x,T,T, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, T,T,x,T, x,x,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,T, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,T, T,T,T,T, T,T,T,x, x,x,T,T, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, T,T,x,T, x,T,x,x, T,T,T,T, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,x,x,T, T,T,x,x, x,x,x,x, T,T,x,T, x,T,x,T, x,x,x,T, x,T,x,T, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,T,T,x, T,x,x,T, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,T,T, T,T,x,x, x,T,T,x, x,T,x,T, x,x,T,T, x,T,x,T, T,T,x,x, x,x,x,T, T,x,x,x, x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x},
	{x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x}

	};
} // end Parser

}