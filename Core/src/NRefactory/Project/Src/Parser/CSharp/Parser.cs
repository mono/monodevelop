
#line  1 "cs.ATG" 
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ASTAttribute = ICSharpCode.NRefactory.Ast.Attribute;
using Types = ICSharpCode.NRefactory.Ast.ClassType;
/*
  Parser.frame file for NRefactory.
 */
using System;
using System.Reflection;

namespace ICSharpCode.NRefactory.Parser.CSharp {



partial class Parser : AbstractParser
{
	const int maxT = 125;

	const  bool   T            = true;
	const  bool   x            = false;
	

#line  18 "cs.ATG" 


/*

*/

	void CS() {

#line  159 "cs.ATG" 
		lexer.NextToken(); /* get the first token */
		compilationUnit = new CompilationUnit(); 
		while (la.kind == 120) {
			UsingDirective();
		}
		while (
#line  163 "cs.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void UsingDirective() {

#line  170 "cs.ATG" 
		string qualident = null; TypeReference aliasedType = null;
		
		Expect(120);

#line  173 "cs.ATG" 
		Location startPos = t.Location; 
		Qualident(
#line  174 "cs.ATG" 
out qualident);
		if (la.kind == 3) {
			lexer.NextToken();
			NonArrayType(
#line  175 "cs.ATG" 
out aliasedType);
		}
		Expect(11);

#line  177 "cs.ATG" 
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

#line  193 "cs.ATG" 
		Location startPos = t.Location; 
		Expect(1);

#line  194 "cs.ATG" 
		if (t.val != "assembly") Error("global attribute target specifier (\"assembly\") expected");
		string attributeTarget = t.val;
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(9);
		Attribute(
#line  199 "cs.ATG" 
out attribute);

#line  199 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  200 "cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  200 "cs.ATG" 
out attribute);

#line  200 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  202 "cs.ATG" 
		AttributeSection section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  293 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		ModifierList m = new ModifierList();
		string qualident;
		
		if (la.kind == 87) {
			lexer.NextToken();

#line  299 "cs.ATG" 
			Location startPos = t.Location; 
			Qualident(
#line  300 "cs.ATG" 
out qualident);

#line  300 "cs.ATG" 
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

#line  309 "cs.ATG" 
			node.EndLocation   = t.EndLocation;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 18) {
				AttributeSection(
#line  313 "cs.ATG" 
out section);

#line  313 "cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  314 "cs.ATG" 
m);
			}
			TypeDecl(
#line  315 "cs.ATG" 
m, attributes);
		} else SynErr(126);
	}

	void Qualident(
#line  437 "cs.ATG" 
out string qualident) {
		Expect(1);

#line  439 "cs.ATG" 
		qualidentBuilder.Length = 0; qualidentBuilder.Append(t.val); 
		while (
#line  440 "cs.ATG" 
DotAndIdent()) {
			Expect(15);
			Expect(1);

#line  440 "cs.ATG" 
			qualidentBuilder.Append('.');
			qualidentBuilder.Append(t.val); 
			
		}

#line  443 "cs.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void NonArrayType(
#line  550 "cs.ATG" 
out TypeReference type) {

#line  552 "cs.ATG" 
		string name;
		int pointer = 0;
		type = null;
		
		if (la.kind == 1 || la.kind == 90 || la.kind == 107) {
			ClassType(
#line  557 "cs.ATG" 
out type, false);
		} else if (StartOf(4)) {
			SimpleType(
#line  558 "cs.ATG" 
out name);

#line  558 "cs.ATG" 
			type = new TypeReference(name); 
		} else if (la.kind == 122) {
			lexer.NextToken();
			Expect(6);

#line  559 "cs.ATG" 
			pointer = 1; type = new TypeReference("void"); 
		} else SynErr(127);
		if (la.kind == 12) {
			NullableQuestionMark(
#line  562 "cs.ATG" 
ref type);
		}
		while (
#line  564 "cs.ATG" 
IsPointer()) {
			Expect(6);

#line  565 "cs.ATG" 
			++pointer; 
		}

#line  567 "cs.ATG" 
		if (type != null) { type.PointerNestingLevel = pointer; } 
	}

	void Attribute(
#line  209 "cs.ATG" 
out ASTAttribute attribute) {

#line  210 "cs.ATG" 
		string qualident;
		string alias = null;
		
		if (
#line  214 "cs.ATG" 
la.kind == Tokens.Identifier && Peek(1).kind == Tokens.DoubleColon) {
			lexer.NextToken();

#line  215 "cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  218 "cs.ATG" 
out qualident);

#line  219 "cs.ATG" 
		List<Expression> positional = new List<Expression>();
		List<NamedArgumentExpression> named = new List<NamedArgumentExpression>();
		string name = (alias != null && alias != "global") ? alias + "." + qualident : qualident;
		
		if (la.kind == 20) {
			AttributeArguments(
#line  223 "cs.ATG" 
positional, named);
		}

#line  223 "cs.ATG" 
		attribute  = new ASTAttribute(name, positional, named);
	}

	void AttributeArguments(
#line  226 "cs.ATG" 
List<Expression> positional, List<NamedArgumentExpression> named) {

#line  228 "cs.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(20);
		if (StartOf(5)) {
			if (
#line  236 "cs.ATG" 
IsAssignment()) {

#line  236 "cs.ATG" 
				nameFound = true; 
				lexer.NextToken();

#line  237 "cs.ATG" 
				name = t.val; 
				Expect(3);
			}
			Expr(
#line  239 "cs.ATG" 
out expr);

#line  239 "cs.ATG" 
			if (expr != null) {if(name == "") positional.Add(expr);
			else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			}
			
			while (la.kind == 14) {
				lexer.NextToken();
				if (
#line  247 "cs.ATG" 
IsAssignment()) {

#line  247 "cs.ATG" 
					nameFound = true; 
					Expect(1);

#line  248 "cs.ATG" 
					name = t.val; 
					Expect(3);
				} else if (StartOf(5)) {

#line  250 "cs.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(128);
				Expr(
#line  251 "cs.ATG" 
out expr);

#line  251 "cs.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				}
				
			}
		}
		Expect(21);
	}

	void Expr(
#line  1609 "cs.ATG" 
out Expression expr) {

#line  1610 "cs.ATG" 
		expr = null; Expression expr1 = null, expr2 = null; AssignmentOperatorType op; 
		UnaryExpr(
#line  1612 "cs.ATG" 
out expr);
		if (StartOf(6)) {
			AssignmentOperator(
#line  1615 "cs.ATG" 
out op);
			Expr(
#line  1615 "cs.ATG" 
out expr1);

#line  1615 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (
#line  1616 "cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			AssignmentOperator(
#line  1617 "cs.ATG" 
out op);
			Expr(
#line  1617 "cs.ATG" 
out expr1);

#line  1617 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (StartOf(7)) {
			ConditionalOrExpr(
#line  1619 "cs.ATG" 
ref expr);
			if (la.kind == 13) {
				lexer.NextToken();
				Expr(
#line  1620 "cs.ATG" 
out expr1);

#line  1620 "cs.ATG" 
				expr = new BinaryOperatorExpression(expr, BinaryOperatorType.NullCoalescing, expr1); 
			}
			if (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  1621 "cs.ATG" 
out expr1);
				Expect(9);
				Expr(
#line  1621 "cs.ATG" 
out expr2);

#line  1621 "cs.ATG" 
				expr = new ConditionalExpression(expr, expr1, expr2);  
			}
		} else SynErr(129);
	}

	void AttributeSection(
#line  260 "cs.ATG" 
out AttributeSection section) {

#line  262 "cs.ATG" 
		string attributeTarget = "";
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		
		Expect(18);

#line  268 "cs.ATG" 
		Location startPos = t.Location; 
		if (
#line  269 "cs.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 68) {
				lexer.NextToken();

#line  270 "cs.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 100) {
				lexer.NextToken();

#line  271 "cs.ATG" 
				attributeTarget = "return";
			} else {
				lexer.NextToken();

#line  272 "cs.ATG" 
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
#line  282 "cs.ATG" 
out attribute);

#line  282 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  283 "cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  283 "cs.ATG" 
out attribute);

#line  283 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  285 "cs.ATG" 
		section = new AttributeSection(attributeTarget, attributes);
		section.StartLocation = startPos;
		section.EndLocation = t.EndLocation;
		
	}

	void TypeModifier(
#line  637 "cs.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 88: {
			lexer.NextToken();

#line  639 "cs.ATG" 
			m.Add(Modifiers.New, t.Location); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  640 "cs.ATG" 
			m.Add(Modifiers.Public, t.Location); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  641 "cs.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  642 "cs.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			break;
		}
		case 95: {
			lexer.NextToken();

#line  643 "cs.ATG" 
			m.Add(Modifiers.Private, t.Location); 
			break;
		}
		case 118: {
			lexer.NextToken();

#line  644 "cs.ATG" 
			m.Add(Modifiers.Unsafe, t.Location); 
			break;
		}
		case 48: {
			lexer.NextToken();

#line  645 "cs.ATG" 
			m.Add(Modifiers.Abstract, t.Location); 
			break;
		}
		case 102: {
			lexer.NextToken();

#line  646 "cs.ATG" 
			m.Add(Modifiers.Sealed, t.Location); 
			break;
		}
		case 106: {
			lexer.NextToken();

#line  647 "cs.ATG" 
			m.Add(Modifiers.Static, t.Location); 
			break;
		}
		case 1: {
			lexer.NextToken();

#line  648 "cs.ATG" 
			if (t.val == "partial") { m.Add(Modifiers.Partial, t.Location); } else { Error("Unexpected identifier"); } 
			break;
		}
		default: SynErr(130); break;
		}
	}

	void TypeDecl(
#line  318 "cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  320 "cs.ATG" 
		TypeReference type;
		List<TypeReference> names;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		string name;
		List<TemplateDefinition> templates;
		
		if (la.kind == 58) {

#line  326 "cs.ATG" 
			m.Check(Modifiers.Classes); 
			lexer.NextToken();

#line  327 "cs.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			templates = newType.Templates;
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			
			newType.Type = Types.Class;
			
			Expect(1);

#line  335 "cs.ATG" 
			newType.Name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  338 "cs.ATG" 
templates);
			}
			if (la.kind == 9) {
				ClassBase(
#line  340 "cs.ATG" 
out names);

#line  340 "cs.ATG" 
				newType.BaseTypes = names; 
			}
			while (
#line  343 "cs.ATG" 
IdentIsWhere()) {
				TypeParameterConstraintsClause(
#line  343 "cs.ATG" 
templates);
			}

#line  345 "cs.ATG" 
			newType.BodyStartLocation = t.EndLocation; 
			Expect(16);
			ClassBody();
			Expect(17);
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  347 "cs.ATG" 
			newType.EndLocation = t.Location; 
			compilationUnit.BlockEnd();
			
		} else if (StartOf(8)) {

#line  350 "cs.ATG" 
			m.Check(Modifiers.StructsInterfacesEnumsDelegates); 
			if (la.kind == 108) {
				lexer.NextToken();

#line  351 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.Type = Types.Struct; 
				
				Expect(1);

#line  358 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  361 "cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					StructInterfaces(
#line  363 "cs.ATG" 
out names);

#line  363 "cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (
#line  366 "cs.ATG" 
IdentIsWhere()) {
					TypeParameterConstraintsClause(
#line  366 "cs.ATG" 
templates);
				}

#line  369 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				StructBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  371 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 82) {
				lexer.NextToken();

#line  375 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Interface;
				
				Expect(1);

#line  382 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  385 "cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					InterfaceBase(
#line  387 "cs.ATG" 
out names);

#line  387 "cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (
#line  390 "cs.ATG" 
IdentIsWhere()) {
					TypeParameterConstraintsClause(
#line  390 "cs.ATG" 
templates);
				}

#line  392 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				InterfaceBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  394 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 67) {
				lexer.NextToken();

#line  398 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Enum;
				
				Expect(1);

#line  404 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					lexer.NextToken();
					IntegralType(
#line  405 "cs.ATG" 
out name);

#line  405 "cs.ATG" 
					newType.BaseTypes.Add(new TypeReference(name)); 
				}

#line  407 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				EnumBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  409 "cs.ATG" 
				newType.EndLocation = t.Location; 
				compilationUnit.BlockEnd();
				
			} else {
				lexer.NextToken();

#line  413 "cs.ATG" 
				DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
				templates = delegateDeclr.Templates;
				delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
				
				if (
#line  417 "cs.ATG" 
NotVoidPointer()) {
					Expect(122);

#line  417 "cs.ATG" 
					delegateDeclr.ReturnType = new TypeReference("void", 0, null); 
				} else if (StartOf(9)) {
					Type(
#line  418 "cs.ATG" 
out type);

#line  418 "cs.ATG" 
					delegateDeclr.ReturnType = type; 
				} else SynErr(131);
				Expect(1);

#line  420 "cs.ATG" 
				delegateDeclr.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  423 "cs.ATG" 
templates);
				}
				Expect(20);
				if (StartOf(10)) {
					FormalParameterList(
#line  425 "cs.ATG" 
p);

#line  425 "cs.ATG" 
					delegateDeclr.Parameters = p; 
				}
				Expect(21);
				while (
#line  429 "cs.ATG" 
IdentIsWhere()) {
					TypeParameterConstraintsClause(
#line  429 "cs.ATG" 
templates);
				}
				Expect(11);

#line  431 "cs.ATG" 
				delegateDeclr.EndLocation = t.Location;
				compilationUnit.AddChild(delegateDeclr);
				
			}
		} else SynErr(132);
	}

	void TypeParameterList(
#line  2026 "cs.ATG" 
List<TemplateDefinition> templates) {

#line  2028 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		Expect(23);
		while (la.kind == 18) {
			AttributeSection(
#line  2032 "cs.ATG" 
out section);

#line  2032 "cs.ATG" 
			attributes.Add(section); 
		}
		Expect(1);

#line  2033 "cs.ATG" 
		templates.Add(new TemplateDefinition(t.val, attributes)); 
		while (la.kind == 14) {
			lexer.NextToken();
			while (la.kind == 18) {
				AttributeSection(
#line  2034 "cs.ATG" 
out section);

#line  2034 "cs.ATG" 
				attributes.Add(section); 
			}
			Expect(1);

#line  2035 "cs.ATG" 
			templates.Add(new TemplateDefinition(t.val, attributes)); 
		}
		Expect(22);
	}

	void ClassBase(
#line  446 "cs.ATG" 
out List<TypeReference> names) {

#line  448 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		ClassType(
#line  452 "cs.ATG" 
out typeRef, false);

#line  452 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  453 "cs.ATG" 
out typeRef, false);

#line  453 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void TypeParameterConstraintsClause(
#line  2039 "cs.ATG" 
List<TemplateDefinition> templates) {

#line  2040 "cs.ATG" 
		string name = ""; TypeReference type; 
		Expect(1);

#line  2042 "cs.ATG" 
		if (t.val != "where") Error("where expected"); 
		Expect(1);

#line  2043 "cs.ATG" 
		name = t.val; 
		Expect(9);
		TypeParameterConstraintsClauseBase(
#line  2045 "cs.ATG" 
out type);

#line  2046 "cs.ATG" 
		TemplateDefinition td = null;
		foreach (TemplateDefinition d in templates) {
			if (d.Name == name) {
				td = d;
				break;
			}
		}
		if ( td != null && type != null) { td.Bases.Add(type); }
		
		while (la.kind == 14) {
			lexer.NextToken();
			TypeParameterConstraintsClauseBase(
#line  2055 "cs.ATG" 
out type);

#line  2056 "cs.ATG" 
			td = null;
			foreach (TemplateDefinition d in templates) {
				if (d.Name == name) {
					td = d;
					break;
				}
			}
			if ( td != null && type != null) { td.Bases.Add(type); }
			
		}
	}

	void ClassBody() {

#line  457 "cs.ATG" 
		AttributeSection section; 
		while (StartOf(11)) {

#line  459 "cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 18) {
				AttributeSection(
#line  462 "cs.ATG" 
out section);

#line  462 "cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  463 "cs.ATG" 
m);
			ClassMemberDecl(
#line  464 "cs.ATG" 
m, attributes);
		}
	}

	void StructInterfaces(
#line  468 "cs.ATG" 
out List<TypeReference> names) {

#line  470 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  474 "cs.ATG" 
out typeRef, false);

#line  474 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  475 "cs.ATG" 
out typeRef, false);

#line  475 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void StructBody() {

#line  479 "cs.ATG" 
		AttributeSection section; 
		Expect(16);
		while (StartOf(12)) {

#line  482 "cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 18) {
				AttributeSection(
#line  485 "cs.ATG" 
out section);

#line  485 "cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  486 "cs.ATG" 
m);
			StructMemberDecl(
#line  487 "cs.ATG" 
m, attributes);
		}
		Expect(17);
	}

	void InterfaceBase(
#line  492 "cs.ATG" 
out List<TypeReference> names) {

#line  494 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  498 "cs.ATG" 
out typeRef, false);

#line  498 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  499 "cs.ATG" 
out typeRef, false);

#line  499 "cs.ATG" 
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
#line  659 "cs.ATG" 
out string name) {

#line  659 "cs.ATG" 
		name = ""; 
		switch (la.kind) {
		case 101: {
			lexer.NextToken();

#line  661 "cs.ATG" 
			name = "sbyte"; 
			break;
		}
		case 53: {
			lexer.NextToken();

#line  662 "cs.ATG" 
			name = "byte"; 
			break;
		}
		case 103: {
			lexer.NextToken();

#line  663 "cs.ATG" 
			name = "short"; 
			break;
		}
		case 119: {
			lexer.NextToken();

#line  664 "cs.ATG" 
			name = "ushort"; 
			break;
		}
		case 81: {
			lexer.NextToken();

#line  665 "cs.ATG" 
			name = "int"; 
			break;
		}
		case 115: {
			lexer.NextToken();

#line  666 "cs.ATG" 
			name = "uint"; 
			break;
		}
		case 86: {
			lexer.NextToken();

#line  667 "cs.ATG" 
			name = "long"; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  668 "cs.ATG" 
			name = "ulong"; 
			break;
		}
		case 56: {
			lexer.NextToken();

#line  669 "cs.ATG" 
			name = "char"; 
			break;
		}
		default: SynErr(133); break;
		}
	}

	void EnumBody() {

#line  508 "cs.ATG" 
		FieldDeclaration f; 
		Expect(16);
		if (la.kind == 1 || la.kind == 18) {
			EnumMemberDecl(
#line  511 "cs.ATG" 
out f);

#line  511 "cs.ATG" 
			compilationUnit.AddChild(f); 
			while (
#line  512 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				EnumMemberDecl(
#line  513 "cs.ATG" 
out f);

#line  513 "cs.ATG" 
				compilationUnit.AddChild(f); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);
	}

	void Type(
#line  518 "cs.ATG" 
out TypeReference type) {
		TypeWithRestriction(
#line  520 "cs.ATG" 
out type, true, false);
	}

	void FormalParameterList(
#line  581 "cs.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  584 "cs.ATG" 
		ParameterDeclarationExpression p;
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		while (la.kind == 18) {
			AttributeSection(
#line  589 "cs.ATG" 
out section);

#line  589 "cs.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(14)) {
			FixedParameter(
#line  591 "cs.ATG" 
out p);

#line  591 "cs.ATG" 
			bool paramsFound = false;
			p.Attributes = attributes;
			parameter.Add(p);
			
			while (la.kind == 14) {
				lexer.NextToken();

#line  596 "cs.ATG" 
				attributes = new List<AttributeSection>(); if (paramsFound) Error("params array must be at end of parameter list"); 
				while (la.kind == 18) {
					AttributeSection(
#line  597 "cs.ATG" 
out section);

#line  597 "cs.ATG" 
					attributes.Add(section); 
				}
				if (StartOf(14)) {
					FixedParameter(
#line  599 "cs.ATG" 
out p);

#line  599 "cs.ATG" 
					p.Attributes = attributes; parameter.Add(p); 
				} else if (la.kind == 94) {
					ParameterArray(
#line  600 "cs.ATG" 
out p);

#line  600 "cs.ATG" 
					paramsFound = true; p.Attributes = attributes; parameter.Add(p); 
				} else SynErr(134);
			}
		} else if (la.kind == 94) {
			ParameterArray(
#line  603 "cs.ATG" 
out p);

#line  603 "cs.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		} else SynErr(135);
	}

	void ClassType(
#line  651 "cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  652 "cs.ATG" 
		TypeReference r; typeRef = null; 
		if (la.kind == 1) {
			TypeName(
#line  654 "cs.ATG" 
out r, canBeUnbound);

#line  654 "cs.ATG" 
			typeRef = r; 
		} else if (la.kind == 90) {
			lexer.NextToken();

#line  655 "cs.ATG" 
			typeRef = new TypeReference("object"); 
		} else if (la.kind == 107) {
			lexer.NextToken();

#line  656 "cs.ATG" 
			typeRef = new TypeReference("string"); 
		} else SynErr(136);
	}

	void TypeName(
#line  1969 "cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  1970 "cs.ATG" 
		List<TypeReference> typeArguments = null;
		string alias = null;
		string qualident;
		
		if (
#line  1975 "cs.ATG" 
la.kind == Tokens.Identifier && Peek(1).kind == Tokens.DoubleColon) {
			lexer.NextToken();

#line  1976 "cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  1979 "cs.ATG" 
out qualident);
		if (la.kind == 23) {
			TypeArgumentList(
#line  1980 "cs.ATG" 
out typeArguments, canBeUnbound);
		}

#line  1982 "cs.ATG" 
		if (alias == null) {
		typeRef = new TypeReference(qualident, typeArguments);
		} else if (alias == "global") {
			typeRef = new TypeReference(qualident, typeArguments);
			typeRef.IsGlobal = true;
		} else {
			typeRef = new TypeReference(alias + "." + qualident, typeArguments);
		}
		
		while (
#line  1991 "cs.ATG" 
DotAndIdent()) {
			Expect(15);

#line  1992 "cs.ATG" 
			typeArguments = null; 
			Qualident(
#line  1993 "cs.ATG" 
out qualident);
			if (la.kind == 23) {
				TypeArgumentList(
#line  1994 "cs.ATG" 
out typeArguments, canBeUnbound);
			}

#line  1995 "cs.ATG" 
			typeRef = new InnerClassTypeReference(typeRef, qualident, typeArguments); 
		}
	}

	void MemberModifiers(
#line  672 "cs.ATG" 
ModifierList m) {
		while (StartOf(15) || 
#line  690 "cs.ATG" 
la.kind == Tokens.Identifier && la.val == "partial") {
			if (la.kind == 48) {
				lexer.NextToken();

#line  675 "cs.ATG" 
				m.Add(Modifiers.Abstract, t.Location); 
			} else if (la.kind == 70) {
				lexer.NextToken();

#line  676 "cs.ATG" 
				m.Add(Modifiers.Extern, t.Location); 
			} else if (la.kind == 83) {
				lexer.NextToken();

#line  677 "cs.ATG" 
				m.Add(Modifiers.Internal, t.Location); 
			} else if (la.kind == 88) {
				lexer.NextToken();

#line  678 "cs.ATG" 
				m.Add(Modifiers.New, t.Location); 
			} else if (la.kind == 93) {
				lexer.NextToken();

#line  679 "cs.ATG" 
				m.Add(Modifiers.Override, t.Location); 
			} else if (la.kind == 95) {
				lexer.NextToken();

#line  680 "cs.ATG" 
				m.Add(Modifiers.Private, t.Location); 
			} else if (la.kind == 96) {
				lexer.NextToken();

#line  681 "cs.ATG" 
				m.Add(Modifiers.Protected, t.Location); 
			} else if (la.kind == 97) {
				lexer.NextToken();

#line  682 "cs.ATG" 
				m.Add(Modifiers.Public, t.Location); 
			} else if (la.kind == 98) {
				lexer.NextToken();

#line  683 "cs.ATG" 
				m.Add(Modifiers.ReadOnly, t.Location); 
			} else if (la.kind == 102) {
				lexer.NextToken();

#line  684 "cs.ATG" 
				m.Add(Modifiers.Sealed, t.Location); 
			} else if (la.kind == 106) {
				lexer.NextToken();

#line  685 "cs.ATG" 
				m.Add(Modifiers.Static, t.Location); 
			} else if (la.kind == 73) {
				lexer.NextToken();

#line  686 "cs.ATG" 
				m.Add(Modifiers.Fixed, t.Location); 
			} else if (la.kind == 118) {
				lexer.NextToken();

#line  687 "cs.ATG" 
				m.Add(Modifiers.Unsafe, t.Location); 
			} else if (la.kind == 121) {
				lexer.NextToken();

#line  688 "cs.ATG" 
				m.Add(Modifiers.Virtual, t.Location); 
			} else if (la.kind == 123) {
				lexer.NextToken();

#line  689 "cs.ATG" 
				m.Add(Modifiers.Volatile, t.Location); 
			} else {
				Expect(1);

#line  691 "cs.ATG" 
				m.Add(Modifiers.Partial, t.Location); 
			}
		}
	}

	void ClassMemberDecl(
#line  983 "cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  984 "cs.ATG" 
		Statement stmt = null; 
		if (StartOf(16)) {
			StructMemberDecl(
#line  986 "cs.ATG" 
m, attributes);
		} else if (la.kind == 27) {

#line  987 "cs.ATG" 
			m.Check(Modifiers.Destructors); Location startPos = t.Location; 
			lexer.NextToken();
			Expect(1);

#line  988 "cs.ATG" 
			DestructorDeclaration d = new DestructorDeclaration(t.val, m.Modifier, attributes); 
			d.Modifier = m.Modifier;
			d.StartLocation = m.GetDeclarationLocation(startPos);
			
			Expect(20);
			Expect(21);

#line  992 "cs.ATG" 
			d.EndLocation = t.EndLocation; 
			if (la.kind == 16) {
				Block(
#line  992 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(137);

#line  993 "cs.ATG" 
			d.Body = (BlockStatement)stmt;
			compilationUnit.AddChild(d);
			
		} else SynErr(138);
	}

	void StructMemberDecl(
#line  696 "cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  698 "cs.ATG" 
		string qualident = null;
		TypeReference type;
		Expression expr;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<VariableDeclaration> variableDeclarators = new List<VariableDeclaration>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		TypeReference explicitInterface = null;
		
		if (la.kind == 59) {

#line  708 "cs.ATG" 
			m.Check(Modifiers.Constants); 
			lexer.NextToken();

#line  709 "cs.ATG" 
			Location startPos = t.Location; 
			Type(
#line  710 "cs.ATG" 
out type);
			Expect(1);

#line  710 "cs.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier | Modifiers.Const);
			fd.StartLocation = m.GetDeclarationLocation(startPos);
			VariableDeclaration f = new VariableDeclaration(t.val);
			fd.Fields.Add(f);
			
			Expect(3);
			Expr(
#line  715 "cs.ATG" 
out expr);

#line  715 "cs.ATG" 
			f.Initializer = expr; 
			while (la.kind == 14) {
				lexer.NextToken();
				Expect(1);

#line  716 "cs.ATG" 
				f = new VariableDeclaration(t.val);
				fd.Fields.Add(f);
				
				Expect(3);
				Expr(
#line  719 "cs.ATG" 
out expr);

#line  719 "cs.ATG" 
				f.Initializer = expr; 
			}
			Expect(11);

#line  720 "cs.ATG" 
			fd.EndLocation = t.EndLocation; compilationUnit.AddChild(fd); 
		} else if (
#line  724 "cs.ATG" 
NotVoidPointer()) {

#line  724 "cs.ATG" 
			m.Check(Modifiers.PropertysEventsMethods); 
			Expect(122);

#line  725 "cs.ATG" 
			Location startPos = t.Location; 
			if (
#line  726 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  727 "cs.ATG" 
out explicitInterface, false);

#line  728 "cs.ATG" 
				if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
				 } 
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  731 "cs.ATG" 
				qualident = t.val; 
			} else SynErr(139);
			if (la.kind == 23) {
				TypeParameterList(
#line  734 "cs.ATG" 
templates);
			}
			Expect(20);
			if (StartOf(10)) {
				FormalParameterList(
#line  737 "cs.ATG" 
p);
			}
			Expect(21);

#line  738 "cs.ATG" 
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
#line  753 "cs.ATG" 
IdentIsWhere()) {
				TypeParameterConstraintsClause(
#line  753 "cs.ATG" 
templates);
			}
			if (la.kind == 16) {
				Block(
#line  755 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(140);

#line  755 "cs.ATG" 
			compilationUnit.BlockEnd();
			methodDeclaration.Body  = (BlockStatement)stmt;
			
		} else if (la.kind == 68) {

#line  759 "cs.ATG" 
			m.Check(Modifiers.PropertysEventsMethods); 
			lexer.NextToken();

#line  760 "cs.ATG" 
			EventDeclaration eventDecl = new EventDeclaration(null, null, m.Modifier, attributes, null);
			eventDecl.StartLocation = t.Location;
			compilationUnit.AddChild(eventDecl);
			compilationUnit.BlockStart(eventDecl);
			EventAddRegion addBlock = null;
			EventRemoveRegion removeBlock = null;
			
			Type(
#line  767 "cs.ATG" 
out type);

#line  767 "cs.ATG" 
			eventDecl.TypeReference = type; 
			if (
#line  768 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  769 "cs.ATG" 
out explicitInterface, false);

#line  770 "cs.ATG" 
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface); 

#line  771 "cs.ATG" 
				eventDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident)); 
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  773 "cs.ATG" 
				qualident = t.val; 
			} else SynErr(141);

#line  775 "cs.ATG" 
			eventDecl.Name = qualident; eventDecl.EndLocation = t.EndLocation; 
			if (la.kind == 3) {
				lexer.NextToken();
				Expr(
#line  776 "cs.ATG" 
out expr);

#line  776 "cs.ATG" 
				eventDecl.Initializer = expr; 
			}
			if (la.kind == 16) {
				lexer.NextToken();

#line  777 "cs.ATG" 
				eventDecl.BodyStart = t.Location; 
				EventAccessorDecls(
#line  778 "cs.ATG" 
out addBlock, out removeBlock);
				Expect(17);

#line  779 "cs.ATG" 
				eventDecl.BodyEnd   = t.EndLocation; 
			}
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  782 "cs.ATG" 
			compilationUnit.BlockEnd();
			eventDecl.AddRegion = addBlock;
			eventDecl.RemoveRegion = removeBlock;
			
		} else if (
#line  788 "cs.ATG" 
IdentAndLPar()) {

#line  788 "cs.ATG" 
			m.Check(Modifiers.Constructors | Modifiers.StaticConstructors); 
			Expect(1);

#line  789 "cs.ATG" 
			string name = t.val; Location startPos = t.Location; 
			Expect(20);
			if (StartOf(10)) {

#line  789 "cs.ATG" 
				m.Check(Modifiers.Constructors); 
				FormalParameterList(
#line  790 "cs.ATG" 
p);
			}
			Expect(21);

#line  792 "cs.ATG" 
			ConstructorInitializer init = null;  
			if (la.kind == 9) {

#line  793 "cs.ATG" 
				m.Check(Modifiers.Constructors); 
				ConstructorInitializer(
#line  794 "cs.ATG" 
out init);
			}

#line  796 "cs.ATG" 
			ConstructorDeclaration cd = new ConstructorDeclaration(name, m.Modifier, p, init, attributes); 
			cd.StartLocation = startPos;
			cd.EndLocation   = t.EndLocation;
			
			if (la.kind == 16) {
				Block(
#line  801 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(142);

#line  801 "cs.ATG" 
			cd.Body = (BlockStatement)stmt; compilationUnit.AddChild(cd); 
		} else if (la.kind == 69 || la.kind == 79) {

#line  804 "cs.ATG" 
			m.Check(Modifiers.Operators);
			if (m.isNone) Error("at least one modifier must be set"); 
			bool isImplicit = true;
			Location startPos = Location.Empty;
			
			if (la.kind == 79) {
				lexer.NextToken();

#line  809 "cs.ATG" 
				startPos = t.Location; 
			} else {
				lexer.NextToken();

#line  809 "cs.ATG" 
				isImplicit = false; startPos = t.Location; 
			}
			Expect(91);
			Type(
#line  810 "cs.ATG" 
out type);

#line  810 "cs.ATG" 
			TypeReference operatorType = type; 
			Expect(20);
			Type(
#line  811 "cs.ATG" 
out type);
			Expect(1);

#line  811 "cs.ATG" 
			string varName = t.val; 
			Expect(21);

#line  812 "cs.ATG" 
			Location endPos = t.Location; 
			if (la.kind == 16) {
				Block(
#line  813 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();

#line  813 "cs.ATG" 
				stmt = null; 
			} else SynErr(143);

#line  816 "cs.ATG" 
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
#line  832 "cs.ATG" 
m, attributes);
		} else if (StartOf(9)) {
			Type(
#line  834 "cs.ATG" 
out type);

#line  834 "cs.ATG" 
			Location startPos = t.Location;  
			if (la.kind == 91) {

#line  836 "cs.ATG" 
				OverloadableOperatorType op;
				m.Check(Modifiers.Operators);
				if (m.isNone) Error("at least one modifier must be set");
				
				lexer.NextToken();
				OverloadableOperator(
#line  840 "cs.ATG" 
out op);

#line  840 "cs.ATG" 
				TypeReference firstType, secondType = null; string secondName = null; 
				Expect(20);
				Type(
#line  841 "cs.ATG" 
out firstType);
				Expect(1);

#line  841 "cs.ATG" 
				string firstName = t.val; 
				if (la.kind == 14) {
					lexer.NextToken();
					Type(
#line  842 "cs.ATG" 
out secondType);
					Expect(1);

#line  842 "cs.ATG" 
					secondName = t.val; 
				} else if (la.kind == 21) {
				} else SynErr(144);

#line  850 "cs.ATG" 
				Location endPos = t.Location; 
				Expect(21);
				if (la.kind == 16) {
					Block(
#line  851 "cs.ATG" 
out stmt);
				} else if (la.kind == 11) {
					lexer.NextToken();
				} else SynErr(145);

#line  853 "cs.ATG" 
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
#line  870 "cs.ATG" 
IsVarDecl()) {

#line  871 "cs.ATG" 
				m.Check(Modifiers.Fields);
				FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
				fd.StartLocation = m.GetDeclarationLocation(startPos); 
				
				if (
#line  875 "cs.ATG" 
m.Contains(Modifiers.Fixed)) {
					VariableDeclarator(
#line  876 "cs.ATG" 
variableDeclarators);
					Expect(18);
					Expr(
#line  878 "cs.ATG" 
out expr);

#line  878 "cs.ATG" 
					if (variableDeclarators.Count > 0)
					variableDeclarators[variableDeclarators.Count-1].FixedArrayInitialization = expr; 
					Expect(19);
					while (la.kind == 14) {
						lexer.NextToken();
						VariableDeclarator(
#line  882 "cs.ATG" 
variableDeclarators);
						Expect(18);
						Expr(
#line  884 "cs.ATG" 
out expr);

#line  884 "cs.ATG" 
						if (variableDeclarators.Count > 0)
						variableDeclarators[variableDeclarators.Count-1].FixedArrayInitialization = expr; 
						Expect(19);
					}
				} else if (la.kind == 1) {
					VariableDeclarator(
#line  889 "cs.ATG" 
variableDeclarators);
					while (la.kind == 14) {
						lexer.NextToken();
						VariableDeclarator(
#line  890 "cs.ATG" 
variableDeclarators);
					}
				} else SynErr(146);
				Expect(11);

#line  892 "cs.ATG" 
				fd.EndLocation = t.EndLocation; fd.Fields = variableDeclarators; compilationUnit.AddChild(fd); 
			} else if (la.kind == 110) {

#line  895 "cs.ATG" 
				m.Check(Modifiers.Indexers); 
				lexer.NextToken();
				Expect(18);
				FormalParameterList(
#line  896 "cs.ATG" 
p);
				Expect(19);

#line  896 "cs.ATG" 
				Location endLocation = t.EndLocation; 
				Expect(16);

#line  897 "cs.ATG" 
				IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
				indexer.StartLocation = startPos;
				indexer.EndLocation   = endLocation;
				indexer.BodyStart     = t.Location;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  904 "cs.ATG" 
out getRegion, out setRegion);
				Expect(17);

#line  905 "cs.ATG" 
				indexer.BodyEnd    = t.EndLocation;
				indexer.GetRegion = getRegion;
				indexer.SetRegion = setRegion;
				compilationUnit.AddChild(indexer);
				
			} else if (
#line  910 "cs.ATG" 
la.kind == Tokens.Identifier) {
				if (
#line  911 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
					TypeName(
#line  912 "cs.ATG" 
out explicitInterface, false);

#line  913 "cs.ATG" 
					if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
					qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
					 } 
				} else if (la.kind == 1) {
					lexer.NextToken();

#line  916 "cs.ATG" 
					qualident = t.val; 
				} else SynErr(147);

#line  918 "cs.ATG" 
				Location qualIdentEndLocation = t.EndLocation; 
				if (la.kind == 16 || la.kind == 20 || la.kind == 23) {
					if (la.kind == 20 || la.kind == 23) {

#line  922 "cs.ATG" 
						m.Check(Modifiers.PropertysEventsMethods); 
						if (la.kind == 23) {
							TypeParameterList(
#line  924 "cs.ATG" 
templates);
						}
						Expect(20);
						if (StartOf(10)) {
							FormalParameterList(
#line  925 "cs.ATG" 
p);
						}
						Expect(21);

#line  926 "cs.ATG" 
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
#line  938 "cs.ATG" 
IdentIsWhere()) {
							TypeParameterConstraintsClause(
#line  938 "cs.ATG" 
templates);
						}
						if (la.kind == 16) {
							Block(
#line  939 "cs.ATG" 
out stmt);
						} else if (la.kind == 11) {
							lexer.NextToken();
						} else SynErr(148);

#line  939 "cs.ATG" 
						methodDeclaration.Body  = (BlockStatement)stmt; 
					} else {
						lexer.NextToken();

#line  942 "cs.ATG" 
						PropertyDeclaration pDecl = new PropertyDeclaration(qualident, type, m.Modifier, attributes); 
						if (explicitInterface != null)
						pDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
						      pDecl.StartLocation = m.GetDeclarationLocation(startPos);
						      pDecl.EndLocation   = qualIdentEndLocation;
						      pDecl.BodyStart   = t.Location;
						      PropertyGetRegion getRegion;
						      PropertySetRegion setRegion;
						   
						AccessorDecls(
#line  951 "cs.ATG" 
out getRegion, out setRegion);
						Expect(17);

#line  953 "cs.ATG" 
						pDecl.GetRegion = getRegion;
						pDecl.SetRegion = setRegion;
						pDecl.BodyEnd = t.EndLocation;
						compilationUnit.AddChild(pDecl);
						
					}
				} else if (la.kind == 15) {

#line  961 "cs.ATG" 
					m.Check(Modifiers.Indexers); 
					lexer.NextToken();
					Expect(110);
					Expect(18);
					FormalParameterList(
#line  962 "cs.ATG" 
p);
					Expect(19);

#line  963 "cs.ATG" 
					IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
					indexer.StartLocation = m.GetDeclarationLocation(startPos);
					indexer.EndLocation   = t.EndLocation;
					if (explicitInterface != null)
					indexer.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, "this"));
					      PropertyGetRegion getRegion;
					      PropertySetRegion setRegion;
					    
					Expect(16);

#line  971 "cs.ATG" 
					Location bodyStart = t.Location; 
					AccessorDecls(
#line  972 "cs.ATG" 
out getRegion, out setRegion);
					Expect(17);

#line  973 "cs.ATG" 
					indexer.BodyStart = bodyStart;
					indexer.BodyEnd   = t.EndLocation;
					indexer.GetRegion = getRegion;
					indexer.SetRegion = setRegion;
					compilationUnit.AddChild(indexer);
					
				} else SynErr(149);
			} else SynErr(150);
		} else SynErr(151);
	}

	void InterfaceMemberDecl() {

#line  1000 "cs.ATG" 
		TypeReference type;
		
		AttributeSection section;
		Modifiers mod = Modifiers.None;
		List<AttributeSection> attributes = new List<AttributeSection>();
		List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
		string name;
		PropertyGetRegion getBlock;
		PropertySetRegion setBlock;
		Location startLocation = new Location(-1, -1);
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		
		while (la.kind == 18) {
			AttributeSection(
#line  1013 "cs.ATG" 
out section);

#line  1013 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 88) {
			lexer.NextToken();

#line  1014 "cs.ATG" 
			mod = Modifiers.New; startLocation = t.Location; 
		}
		if (
#line  1017 "cs.ATG" 
NotVoidPointer()) {
			Expect(122);

#line  1017 "cs.ATG" 
			if (startLocation.X == -1) startLocation = t.Location; 
			Expect(1);

#line  1017 "cs.ATG" 
			name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  1018 "cs.ATG" 
templates);
			}
			Expect(20);
			if (StartOf(10)) {
				FormalParameterList(
#line  1019 "cs.ATG" 
parameters);
			}
			Expect(21);
			while (
#line  1020 "cs.ATG" 
IdentIsWhere()) {
				TypeParameterConstraintsClause(
#line  1020 "cs.ATG" 
templates);
			}
			Expect(11);

#line  1022 "cs.ATG" 
			MethodDeclaration md = new MethodDeclaration(name, mod, new TypeReference("void"), parameters, attributes);
			md.StartLocation = startLocation;
			md.EndLocation = t.EndLocation;
			md.Templates = templates;
			compilationUnit.AddChild(md);
			
		} else if (StartOf(18)) {
			if (StartOf(9)) {
				Type(
#line  1029 "cs.ATG" 
out type);

#line  1029 "cs.ATG" 
				if (startLocation.X == -1) startLocation = t.Location; 
				if (la.kind == 1) {
					lexer.NextToken();

#line  1031 "cs.ATG" 
					name = t.val; Location qualIdentEndLocation = t.EndLocation; 
					if (la.kind == 20 || la.kind == 23) {
						if (la.kind == 23) {
							TypeParameterList(
#line  1035 "cs.ATG" 
templates);
						}
						Expect(20);
						if (StartOf(10)) {
							FormalParameterList(
#line  1036 "cs.ATG" 
parameters);
						}
						Expect(21);
						while (
#line  1038 "cs.ATG" 
IdentIsWhere()) {
							TypeParameterConstraintsClause(
#line  1038 "cs.ATG" 
templates);
						}
						Expect(11);

#line  1039 "cs.ATG" 
						MethodDeclaration md = new MethodDeclaration(name, mod, type, parameters, attributes);
						md.StartLocation = startLocation;
						md.EndLocation = t.EndLocation;
						md.Templates = templates;
						compilationUnit.AddChild(md);
						
					} else if (la.kind == 16) {

#line  1046 "cs.ATG" 
						PropertyDeclaration pd = new PropertyDeclaration(name, type, mod, attributes); compilationUnit.AddChild(pd); 
						lexer.NextToken();

#line  1047 "cs.ATG" 
						Location bodyStart = t.Location;
						InterfaceAccessors(
#line  1047 "cs.ATG" 
out getBlock, out setBlock);
						Expect(17);

#line  1047 "cs.ATG" 
						pd.GetRegion = getBlock; pd.SetRegion = setBlock; pd.StartLocation = startLocation; pd.EndLocation = qualIdentEndLocation; pd.BodyStart = bodyStart; pd.BodyEnd = t.EndLocation; 
					} else SynErr(152);
				} else if (la.kind == 110) {
					lexer.NextToken();
					Expect(18);
					FormalParameterList(
#line  1050 "cs.ATG" 
parameters);
					Expect(19);

#line  1050 "cs.ATG" 
					Location bracketEndLocation = t.EndLocation; 

#line  1050 "cs.ATG" 
					IndexerDeclaration id = new IndexerDeclaration(type, parameters, mod, attributes); compilationUnit.AddChild(id); 
					Expect(16);

#line  1051 "cs.ATG" 
					Location bodyStart = t.Location;
					InterfaceAccessors(
#line  1051 "cs.ATG" 
out getBlock, out setBlock);
					Expect(17);

#line  1051 "cs.ATG" 
					id.GetRegion = getBlock; id.SetRegion = setBlock; id.StartLocation = startLocation;  id.EndLocation = bracketEndLocation; id.BodyStart = bodyStart; id.BodyEnd = t.EndLocation;
				} else SynErr(153);
			} else {
				lexer.NextToken();

#line  1054 "cs.ATG" 
				if (startLocation.X == -1) startLocation = t.Location; 
				Type(
#line  1054 "cs.ATG" 
out type);
				Expect(1);

#line  1054 "cs.ATG" 
				EventDeclaration ed = new EventDeclaration(type, t.val, mod, attributes, null);
				compilationUnit.AddChild(ed);
				
				Expect(11);

#line  1057 "cs.ATG" 
				ed.StartLocation = startLocation; ed.EndLocation = t.EndLocation; 
			}
		} else SynErr(154);
	}

	void EnumMemberDecl(
#line  1062 "cs.ATG" 
out FieldDeclaration f) {

#line  1064 "cs.ATG" 
		Expression expr = null;
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1070 "cs.ATG" 
out section);

#line  1070 "cs.ATG" 
			attributes.Add(section); 
		}
		Expect(1);

#line  1071 "cs.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl         = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = t.Location;
		
		if (la.kind == 3) {
			lexer.NextToken();
			Expr(
#line  1076 "cs.ATG" 
out expr);

#line  1076 "cs.ATG" 
			varDecl.Initializer = expr; 
		}
	}

	void TypeWithRestriction(
#line  523 "cs.ATG" 
out TypeReference type, bool allowNullable, bool canBeUnbound) {

#line  525 "cs.ATG" 
		string name;
		int pointer = 0;
		type = null;
		
		if (la.kind == 1 || la.kind == 90 || la.kind == 107) {
			ClassType(
#line  530 "cs.ATG" 
out type, canBeUnbound);
		} else if (StartOf(4)) {
			SimpleType(
#line  531 "cs.ATG" 
out name);

#line  531 "cs.ATG" 
			type = new TypeReference(name); 
		} else if (la.kind == 122) {
			lexer.NextToken();
			Expect(6);

#line  532 "cs.ATG" 
			pointer = 1; type = new TypeReference("void"); 
		} else SynErr(155);

#line  533 "cs.ATG" 
		List<int> r = new List<int>(); 
		if (
#line  535 "cs.ATG" 
allowNullable && la.kind == Tokens.Question) {
			NullableQuestionMark(
#line  535 "cs.ATG" 
ref type);
		}
		while (
#line  537 "cs.ATG" 
IsPointerOrDims()) {

#line  537 "cs.ATG" 
			int i = 0; 
			if (la.kind == 6) {
				lexer.NextToken();

#line  538 "cs.ATG" 
				++pointer; 
			} else if (la.kind == 18) {
				lexer.NextToken();
				while (la.kind == 14) {
					lexer.NextToken();

#line  539 "cs.ATG" 
					++i; 
				}
				Expect(19);

#line  539 "cs.ATG" 
				r.Add(i); 
			} else SynErr(156);
		}

#line  542 "cs.ATG" 
		if (type != null) {
		type.RankSpecifier = r.ToArray();
		type.PointerNestingLevel = pointer;
		  }
		
	}

	void SimpleType(
#line  570 "cs.ATG" 
out string name) {

#line  571 "cs.ATG" 
		name = String.Empty; 
		if (StartOf(19)) {
			IntegralType(
#line  573 "cs.ATG" 
out name);
		} else if (la.kind == 74) {
			lexer.NextToken();

#line  574 "cs.ATG" 
			name = "float"; 
		} else if (la.kind == 65) {
			lexer.NextToken();

#line  575 "cs.ATG" 
			name = "double"; 
		} else if (la.kind == 61) {
			lexer.NextToken();

#line  576 "cs.ATG" 
			name = "decimal"; 
		} else if (la.kind == 51) {
			lexer.NextToken();

#line  577 "cs.ATG" 
			name = "bool"; 
		} else SynErr(157);
	}

	void NullableQuestionMark(
#line  2000 "cs.ATG" 
ref TypeReference typeRef) {

#line  2001 "cs.ATG" 
		List<TypeReference> typeArguments = new List<TypeReference>(1); 
		Expect(12);

#line  2005 "cs.ATG" 
		if (typeRef != null) typeArguments.Add(typeRef);
		typeRef = new TypeReference("System.Nullable", typeArguments);
		
	}

	void FixedParameter(
#line  607 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  609 "cs.ATG" 
		TypeReference type;
		ParameterModifiers mod = ParameterModifiers.In;
		Location start = t.Location;
		
		if (la.kind == 92 || la.kind == 99) {
			if (la.kind == 99) {
				lexer.NextToken();

#line  615 "cs.ATG" 
				mod = ParameterModifiers.Ref; 
			} else {
				lexer.NextToken();

#line  616 "cs.ATG" 
				mod = ParameterModifiers.Out; 
			}
		}
		Type(
#line  618 "cs.ATG" 
out type);
		Expect(1);

#line  618 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, mod); p.StartLocation = start; p.EndLocation = t.Location; 
	}

	void ParameterArray(
#line  621 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  622 "cs.ATG" 
		TypeReference type; 
		Expect(94);
		Type(
#line  624 "cs.ATG" 
out type);
		Expect(1);

#line  624 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, ParameterModifiers.Params); 
	}

	void AccessorModifiers(
#line  627 "cs.ATG" 
out ModifierList m) {

#line  628 "cs.ATG" 
		m = new ModifierList(); 
		if (la.kind == 95) {
			lexer.NextToken();

#line  630 "cs.ATG" 
			m.Add(Modifiers.Private, t.Location); 
		} else if (la.kind == 96) {
			lexer.NextToken();

#line  631 "cs.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			if (la.kind == 83) {
				lexer.NextToken();

#line  632 "cs.ATG" 
				m.Add(Modifiers.Internal, t.Location); 
			}
		} else if (la.kind == 83) {
			lexer.NextToken();

#line  633 "cs.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			if (la.kind == 96) {
				lexer.NextToken();

#line  634 "cs.ATG" 
				m.Add(Modifiers.Protected, t.Location); 
			}
		} else SynErr(158);
	}

	void Block(
#line  1201 "cs.ATG" 
out Statement stmt) {
		Expect(16);

#line  1203 "cs.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		blockStmt.StartLocation = t.Location;
		compilationUnit.BlockStart(blockStmt);
		if (!ParseMethodBodies) lexer.SkipCurrentBlock(0);
		
		while (StartOf(20)) {
			Statement();
		}
		Expect(17);

#line  1210 "cs.ATG" 
		stmt = blockStmt;
		blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void EventAccessorDecls(
#line  1136 "cs.ATG" 
out EventAddRegion addBlock, out EventRemoveRegion removeBlock) {

#line  1137 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		Statement stmt;
		addBlock = null;
		removeBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1144 "cs.ATG" 
out section);

#line  1144 "cs.ATG" 
			attributes.Add(section); 
		}
		if (
#line  1146 "cs.ATG" 
IdentIsAdd()) {

#line  1146 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); 
			AddAccessorDecl(
#line  1147 "cs.ATG" 
out stmt);

#line  1147 "cs.ATG" 
			attributes = new List<AttributeSection>(); addBlock.Block = (BlockStatement)stmt; 
			while (la.kind == 18) {
				AttributeSection(
#line  1148 "cs.ATG" 
out section);

#line  1148 "cs.ATG" 
				attributes.Add(section); 
			}
			RemoveAccessorDecl(
#line  1149 "cs.ATG" 
out stmt);

#line  1149 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; 
		} else if (
#line  1150 "cs.ATG" 
IdentIsRemove()) {
			RemoveAccessorDecl(
#line  1151 "cs.ATG" 
out stmt);

#line  1151 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; attributes = new List<AttributeSection>(); 
			while (la.kind == 18) {
				AttributeSection(
#line  1152 "cs.ATG" 
out section);

#line  1152 "cs.ATG" 
				attributes.Add(section); 
			}
			AddAccessorDecl(
#line  1153 "cs.ATG" 
out stmt);

#line  1153 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); addBlock.Block = (BlockStatement)stmt; 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1154 "cs.ATG" 
			Error("add or remove accessor declaration expected"); 
		} else SynErr(159);
	}

	void ConstructorInitializer(
#line  1232 "cs.ATG" 
out ConstructorInitializer ci) {

#line  1233 "cs.ATG" 
		Expression expr; ci = new ConstructorInitializer(); 
		Expect(9);
		if (la.kind == 50) {
			lexer.NextToken();

#line  1237 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.Base; 
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  1238 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.This; 
		} else SynErr(160);
		Expect(20);
		if (StartOf(21)) {
			Argument(
#line  1241 "cs.ATG" 
out expr);

#line  1241 "cs.ATG" 
			if (expr != null) { ci.Arguments.Add(expr); } 
			while (la.kind == 14) {
				lexer.NextToken();
				Argument(
#line  1241 "cs.ATG" 
out expr);

#line  1241 "cs.ATG" 
				if (expr != null) { ci.Arguments.Add(expr); } 
			}
		}
		Expect(21);
	}

	void OverloadableOperator(
#line  1253 "cs.ATG" 
out OverloadableOperatorType op) {

#line  1254 "cs.ATG" 
		op = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 4: {
			lexer.NextToken();

#line  1256 "cs.ATG" 
			op = OverloadableOperatorType.Add; 
			break;
		}
		case 5: {
			lexer.NextToken();

#line  1257 "cs.ATG" 
			op = OverloadableOperatorType.Subtract; 
			break;
		}
		case 24: {
			lexer.NextToken();

#line  1259 "cs.ATG" 
			op = OverloadableOperatorType.Not; 
			break;
		}
		case 27: {
			lexer.NextToken();

#line  1260 "cs.ATG" 
			op = OverloadableOperatorType.BitNot; 
			break;
		}
		case 31: {
			lexer.NextToken();

#line  1262 "cs.ATG" 
			op = OverloadableOperatorType.Increment; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1263 "cs.ATG" 
			op = OverloadableOperatorType.Decrement; 
			break;
		}
		case 112: {
			lexer.NextToken();

#line  1265 "cs.ATG" 
			op = OverloadableOperatorType.IsTrue; 
			break;
		}
		case 71: {
			lexer.NextToken();

#line  1266 "cs.ATG" 
			op = OverloadableOperatorType.IsFalse; 
			break;
		}
		case 6: {
			lexer.NextToken();

#line  1268 "cs.ATG" 
			op = OverloadableOperatorType.Multiply; 
			break;
		}
		case 7: {
			lexer.NextToken();

#line  1269 "cs.ATG" 
			op = OverloadableOperatorType.Divide; 
			break;
		}
		case 8: {
			lexer.NextToken();

#line  1270 "cs.ATG" 
			op = OverloadableOperatorType.Modulus; 
			break;
		}
		case 28: {
			lexer.NextToken();

#line  1272 "cs.ATG" 
			op = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 29: {
			lexer.NextToken();

#line  1273 "cs.ATG" 
			op = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1274 "cs.ATG" 
			op = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1276 "cs.ATG" 
			op = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 33: {
			lexer.NextToken();

#line  1277 "cs.ATG" 
			op = OverloadableOperatorType.Equality; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1278 "cs.ATG" 
			op = OverloadableOperatorType.InEquality; 
			break;
		}
		case 23: {
			lexer.NextToken();

#line  1279 "cs.ATG" 
			op = OverloadableOperatorType.LessThan; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1280 "cs.ATG" 
			op = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1281 "cs.ATG" 
			op = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 22: {
			lexer.NextToken();

#line  1282 "cs.ATG" 
			op = OverloadableOperatorType.GreaterThan; 
			if (la.kind == 22) {
				lexer.NextToken();

#line  1282 "cs.ATG" 
				op = OverloadableOperatorType.ShiftRight; 
			}
			break;
		}
		default: SynErr(161); break;
		}
	}

	void VariableDeclarator(
#line  1194 "cs.ATG" 
List<VariableDeclaration> fieldDeclaration) {

#line  1195 "cs.ATG" 
		Expression expr = null; 
		Expect(1);

#line  1197 "cs.ATG" 
		VariableDeclaration f = new VariableDeclaration(t.val); 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1198 "cs.ATG" 
out expr);

#line  1198 "cs.ATG" 
			f.Initializer = expr; 
		}

#line  1198 "cs.ATG" 
		fieldDeclaration.Add(f); 
	}

	void AccessorDecls(
#line  1080 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1082 "cs.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>(); 
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		ModifierList modifiers = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1089 "cs.ATG" 
out section);

#line  1089 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 83 || la.kind == 95 || la.kind == 96) {
			AccessorModifiers(
#line  1090 "cs.ATG" 
out modifiers);
		}
		if (
#line  1092 "cs.ATG" 
IdentIsGet()) {
			GetAccessorDecl(
#line  1093 "cs.ATG" 
out getBlock, attributes);

#line  1094 "cs.ATG" 
			if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(22)) {

#line  1095 "cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1096 "cs.ATG" 
out section);

#line  1096 "cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 83 || la.kind == 95 || la.kind == 96) {
					AccessorModifiers(
#line  1097 "cs.ATG" 
out modifiers);
				}
				SetAccessorDecl(
#line  1098 "cs.ATG" 
out setBlock, attributes);

#line  1099 "cs.ATG" 
				if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (
#line  1101 "cs.ATG" 
IdentIsSet()) {
			SetAccessorDecl(
#line  1102 "cs.ATG" 
out setBlock, attributes);

#line  1103 "cs.ATG" 
			if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(22)) {

#line  1104 "cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1105 "cs.ATG" 
out section);

#line  1105 "cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 83 || la.kind == 95 || la.kind == 96) {
					AccessorModifiers(
#line  1106 "cs.ATG" 
out modifiers);
				}
				GetAccessorDecl(
#line  1107 "cs.ATG" 
out getBlock, attributes);

#line  1108 "cs.ATG" 
				if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1110 "cs.ATG" 
			Error("get or set accessor declaration expected"); 
		} else SynErr(162);
	}

	void InterfaceAccessors(
#line  1158 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1160 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		getBlock = null; setBlock = null;
		PropertyGetSetRegion lastBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1166 "cs.ATG" 
out section);

#line  1166 "cs.ATG" 
			attributes.Add(section); 
		}

#line  1167 "cs.ATG" 
		Location startLocation = la.Location; 
		if (
#line  1169 "cs.ATG" 
IdentIsGet()) {
			Expect(1);

#line  1169 "cs.ATG" 
			getBlock = new PropertyGetRegion(null, attributes); 
		} else if (
#line  1170 "cs.ATG" 
IdentIsSet()) {
			Expect(1);

#line  1170 "cs.ATG" 
			setBlock = new PropertySetRegion(null, attributes); 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1171 "cs.ATG" 
			Error("set or get expected"); 
		} else SynErr(163);
		Expect(11);

#line  1174 "cs.ATG" 
		if (getBlock != null) { getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; }
		if (setBlock != null) { setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; }
		attributes = new List<AttributeSection>(); 
		if (la.kind == 1 || la.kind == 18) {
			while (la.kind == 18) {
				AttributeSection(
#line  1178 "cs.ATG" 
out section);

#line  1178 "cs.ATG" 
				attributes.Add(section); 
			}

#line  1179 "cs.ATG" 
			startLocation = la.Location; 
			if (
#line  1181 "cs.ATG" 
IdentIsGet()) {
				Expect(1);

#line  1181 "cs.ATG" 
				if (getBlock != null) Error("get already declared");
				else { getBlock = new PropertyGetRegion(null, attributes); lastBlock = getBlock; }
				
			} else if (
#line  1184 "cs.ATG" 
IdentIsSet()) {
				Expect(1);

#line  1184 "cs.ATG" 
				if (setBlock != null) Error("set already declared");
				else { setBlock = new PropertySetRegion(null, attributes); lastBlock = setBlock; }
				
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  1187 "cs.ATG" 
				Error("set or get expected"); 
			} else SynErr(164);
			Expect(11);

#line  1190 "cs.ATG" 
			if (lastBlock != null) { lastBlock.StartLocation = startLocation; lastBlock.EndLocation = t.EndLocation; } 
		}
	}

	void GetAccessorDecl(
#line  1114 "cs.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1115 "cs.ATG" 
		Statement stmt = null; 
		Expect(1);

#line  1118 "cs.ATG" 
		if (t.val != "get") Error("get expected"); 

#line  1119 "cs.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1120 "cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(165);

#line  1121 "cs.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 

#line  1122 "cs.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
	}

	void SetAccessorDecl(
#line  1125 "cs.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1126 "cs.ATG" 
		Statement stmt = null; 
		Expect(1);

#line  1129 "cs.ATG" 
		if (t.val != "set") Error("set expected"); 

#line  1130 "cs.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1131 "cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(166);

#line  1132 "cs.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes); 

#line  1133 "cs.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
	}

	void AddAccessorDecl(
#line  1216 "cs.ATG" 
out Statement stmt) {

#line  1217 "cs.ATG" 
		stmt = null;
		Expect(1);

#line  1220 "cs.ATG" 
		if (t.val != "add") Error("add expected"); 
		Block(
#line  1221 "cs.ATG" 
out stmt);
	}

	void RemoveAccessorDecl(
#line  1224 "cs.ATG" 
out Statement stmt) {

#line  1225 "cs.ATG" 
		stmt = null;
		Expect(1);

#line  1228 "cs.ATG" 
		if (t.val != "remove") Error("remove expected"); 
		Block(
#line  1229 "cs.ATG" 
out stmt);
	}

	void VariableInitializer(
#line  1245 "cs.ATG" 
out Expression initializerExpression) {

#line  1246 "cs.ATG" 
		TypeReference type = null; Expression expr = null; initializerExpression = null; 
		if (StartOf(5)) {
			Expr(
#line  1248 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 16) {
			ArrayInitializer(
#line  1249 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 105) {
			lexer.NextToken();
			Type(
#line  1250 "cs.ATG" 
out type);
			Expect(18);
			Expr(
#line  1250 "cs.ATG" 
out expr);
			Expect(19);

#line  1250 "cs.ATG" 
			initializerExpression = new StackAllocExpression(type, expr); 
		} else SynErr(167);
	}

	void Statement() {

#line  1362 "cs.ATG" 
		TypeReference type;
		Expression expr;
		Statement stmt = null;
		Location startPos = la.Location;
		
		if (
#line  1370 "cs.ATG" 
IsLabel()) {
			Expect(1);

#line  1370 "cs.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val)); 
			Expect(9);
			Statement();
		} else if (la.kind == 59) {
			lexer.NextToken();
			Type(
#line  1373 "cs.ATG" 
out type);

#line  1373 "cs.ATG" 
			LocalVariableDeclaration var = new LocalVariableDeclaration(type, Modifiers.Const); string ident = null; var.StartLocation = t.Location; 
			Expect(1);

#line  1374 "cs.ATG" 
			ident = t.val; 
			Expect(3);
			Expr(
#line  1375 "cs.ATG" 
out expr);

#line  1375 "cs.ATG" 
			var.Variables.Add(new VariableDeclaration(ident, expr)); 
			while (la.kind == 14) {
				lexer.NextToken();
				Expect(1);

#line  1376 "cs.ATG" 
				ident = t.val; 
				Expect(3);
				Expr(
#line  1376 "cs.ATG" 
out expr);

#line  1376 "cs.ATG" 
				var.Variables.Add(new VariableDeclaration(ident, expr)); 
			}
			Expect(11);

#line  1377 "cs.ATG" 
			compilationUnit.AddChild(var); 
		} else if (
#line  1379 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1379 "cs.ATG" 
out stmt);
			Expect(11);

#line  1379 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else if (StartOf(23)) {
			EmbeddedStatement(
#line  1380 "cs.ATG" 
out stmt);

#line  1380 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(168);

#line  1386 "cs.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startPos;
		stmt.EndLocation = t.EndLocation;
		}
		
	}

	void Argument(
#line  1285 "cs.ATG" 
out Expression argumentexpr) {

#line  1287 "cs.ATG" 
		Expression expr;
		FieldDirection fd = FieldDirection.None;
		
		if (la.kind == 92 || la.kind == 99) {
			if (la.kind == 99) {
				lexer.NextToken();

#line  1292 "cs.ATG" 
				fd = FieldDirection.Ref; 
			} else {
				lexer.NextToken();

#line  1293 "cs.ATG" 
				fd = FieldDirection.Out; 
			}
		}
		Expr(
#line  1295 "cs.ATG" 
out expr);

#line  1295 "cs.ATG" 
		argumentexpr = fd != FieldDirection.None ? argumentexpr = new DirectionExpression(fd, expr) : expr; 
	}

	void ArrayInitializer(
#line  1315 "cs.ATG" 
out Expression outExpr) {

#line  1317 "cs.ATG" 
		Expression expr = null;
		ArrayInitializerExpression initializer = new ArrayInitializerExpression();
		
		Expect(16);
		if (StartOf(24)) {
			VariableInitializer(
#line  1322 "cs.ATG" 
out expr);

#line  1323 "cs.ATG" 
			if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			while (
#line  1324 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				VariableInitializer(
#line  1325 "cs.ATG" 
out expr);

#line  1326 "cs.ATG" 
				if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);

#line  1330 "cs.ATG" 
		outExpr = initializer; 
	}

	void AssignmentOperator(
#line  1298 "cs.ATG" 
out AssignmentOperatorType op) {

#line  1299 "cs.ATG" 
		op = AssignmentOperatorType.None; 
		if (la.kind == 3) {
			lexer.NextToken();

#line  1301 "cs.ATG" 
			op = AssignmentOperatorType.Assign; 
		} else if (la.kind == 38) {
			lexer.NextToken();

#line  1302 "cs.ATG" 
			op = AssignmentOperatorType.Add; 
		} else if (la.kind == 39) {
			lexer.NextToken();

#line  1303 "cs.ATG" 
			op = AssignmentOperatorType.Subtract; 
		} else if (la.kind == 40) {
			lexer.NextToken();

#line  1304 "cs.ATG" 
			op = AssignmentOperatorType.Multiply; 
		} else if (la.kind == 41) {
			lexer.NextToken();

#line  1305 "cs.ATG" 
			op = AssignmentOperatorType.Divide; 
		} else if (la.kind == 42) {
			lexer.NextToken();

#line  1306 "cs.ATG" 
			op = AssignmentOperatorType.Modulus; 
		} else if (la.kind == 43) {
			lexer.NextToken();

#line  1307 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseAnd; 
		} else if (la.kind == 44) {
			lexer.NextToken();

#line  1308 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseOr; 
		} else if (la.kind == 45) {
			lexer.NextToken();

#line  1309 "cs.ATG" 
			op = AssignmentOperatorType.ExclusiveOr; 
		} else if (la.kind == 46) {
			lexer.NextToken();

#line  1310 "cs.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
		} else if (
#line  1311 "cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			Expect(22);
			Expect(35);

#line  1312 "cs.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
		} else SynErr(169);
	}

	void LocalVariableDecl(
#line  1333 "cs.ATG" 
out Statement stmt) {

#line  1335 "cs.ATG" 
		TypeReference type;
		VariableDeclaration      var = null;
		LocalVariableDeclaration localVariableDeclaration; 
		
		Type(
#line  1340 "cs.ATG" 
out type);

#line  1340 "cs.ATG" 
		localVariableDeclaration = new LocalVariableDeclaration(type); localVariableDeclaration.StartLocation = t.Location; 
		LocalVariableDeclarator(
#line  1341 "cs.ATG" 
out var);

#line  1341 "cs.ATG" 
		localVariableDeclaration.Variables.Add(var); 
		while (la.kind == 14) {
			lexer.NextToken();
			LocalVariableDeclarator(
#line  1342 "cs.ATG" 
out var);

#line  1342 "cs.ATG" 
			localVariableDeclaration.Variables.Add(var); 
		}

#line  1343 "cs.ATG" 
		stmt = localVariableDeclaration; 
	}

	void LocalVariableDeclarator(
#line  1346 "cs.ATG" 
out VariableDeclaration var) {

#line  1347 "cs.ATG" 
		Expression expr = null; 
		Expect(1);

#line  1350 "cs.ATG" 
		var = new VariableDeclaration(t.val); 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1350 "cs.ATG" 
out expr);

#line  1350 "cs.ATG" 
			var.Initializer = expr; 
		}
	}

	void EmbeddedStatement(
#line  1393 "cs.ATG" 
out Statement statement) {

#line  1395 "cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		
		if (la.kind == 16) {
			Block(
#line  1401 "cs.ATG" 
out statement);
		} else if (la.kind == 11) {
			lexer.NextToken();

#line  1403 "cs.ATG" 
			statement = new EmptyStatement(); 
		} else if (
#line  1405 "cs.ATG" 
UnCheckedAndLBrace()) {

#line  1405 "cs.ATG" 
			Statement block; bool isChecked = true; 
			if (la.kind == 57) {
				lexer.NextToken();
			} else if (la.kind == 117) {
				lexer.NextToken();

#line  1406 "cs.ATG" 
				isChecked = false;
			} else SynErr(170);
			Block(
#line  1407 "cs.ATG" 
out block);

#line  1407 "cs.ATG" 
			statement = isChecked ? (Statement)new CheckedStatement(block) : (Statement)new UncheckedStatement(block); 
		} else if (la.kind == 78) {
			lexer.NextToken();

#line  1409 "cs.ATG" 
			Statement elseStatement = null; 
			Expect(20);
			Expr(
#line  1410 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1411 "cs.ATG" 
out embeddedStatement);
			if (la.kind == 66) {
				lexer.NextToken();
				EmbeddedStatement(
#line  1412 "cs.ATG" 
out elseStatement);
			}

#line  1413 "cs.ATG" 
			statement = elseStatement != null ? new IfElseStatement(expr, embeddedStatement, elseStatement) : new IfElseStatement(expr, embeddedStatement); 

#line  1414 "cs.ATG" 
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

#line  1422 "cs.ATG" 
			List<SwitchSection> switchSections = new List<SwitchSection>(); 
			Expect(20);
			Expr(
#line  1423 "cs.ATG" 
out expr);
			Expect(21);
			Expect(16);
			SwitchSections(
#line  1424 "cs.ATG" 
switchSections);
			Expect(17);

#line  1425 "cs.ATG" 
			statement = new SwitchStatement(expr, switchSections); 
		} else if (la.kind == 124) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1427 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1429 "cs.ATG" 
out embeddedStatement);

#line  1429 "cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
		} else if (la.kind == 64) {
			lexer.NextToken();
			EmbeddedStatement(
#line  1430 "cs.ATG" 
out embeddedStatement);
			Expect(124);
			Expect(20);
			Expr(
#line  1431 "cs.ATG" 
out expr);
			Expect(21);
			Expect(11);

#line  1431 "cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.End); 
		} else if (la.kind == 75) {
			lexer.NextToken();

#line  1432 "cs.ATG" 
			List<Statement> initializer = null; List<Statement> iterator = null; 
			Expect(20);
			if (StartOf(5)) {
				ForInitializer(
#line  1433 "cs.ATG" 
out initializer);
			}
			Expect(11);
			if (StartOf(5)) {
				Expr(
#line  1434 "cs.ATG" 
out expr);
			}
			Expect(11);
			if (StartOf(5)) {
				ForIterator(
#line  1435 "cs.ATG" 
out iterator);
			}
			Expect(21);
			EmbeddedStatement(
#line  1436 "cs.ATG" 
out embeddedStatement);

#line  1436 "cs.ATG" 
			statement = new ForStatement(initializer, expr, iterator, embeddedStatement); 
		} else if (la.kind == 76) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1437 "cs.ATG" 
out type);
			Expect(1);

#line  1437 "cs.ATG" 
			string varName = t.val; Location start = t.Location;
			Expect(80);
			Expr(
#line  1438 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1439 "cs.ATG" 
out embeddedStatement);

#line  1439 "cs.ATG" 
			statement = new ForeachStatement(type, varName , expr, embeddedStatement); 
			statement.EndLocation = t.EndLocation;
			
		} else if (la.kind == 52) {
			lexer.NextToken();
			Expect(11);

#line  1443 "cs.ATG" 
			statement = new BreakStatement(); 
		} else if (la.kind == 60) {
			lexer.NextToken();
			Expect(11);

#line  1444 "cs.ATG" 
			statement = new ContinueStatement(); 
		} else if (la.kind == 77) {
			GotoStatement(
#line  1445 "cs.ATG" 
out statement);
		} else if (
#line  1446 "cs.ATG" 
IsYieldStatement()) {
			Expect(1);
			if (la.kind == 100) {
				lexer.NextToken();
				Expr(
#line  1446 "cs.ATG" 
out expr);

#line  1446 "cs.ATG" 
				statement = new YieldStatement(new ReturnStatement(expr)); 
			} else if (la.kind == 52) {
				lexer.NextToken();

#line  1447 "cs.ATG" 
				statement = new YieldStatement(new BreakStatement()); 
			} else SynErr(171);
			Expect(11);
		} else if (la.kind == 100) {
			lexer.NextToken();
			if (StartOf(5)) {
				Expr(
#line  1448 "cs.ATG" 
out expr);
			}
			Expect(11);

#line  1448 "cs.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 111) {
			lexer.NextToken();
			if (StartOf(5)) {
				Expr(
#line  1449 "cs.ATG" 
out expr);
			}
			Expect(11);

#line  1449 "cs.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (StartOf(5)) {
			StatementExpr(
#line  1452 "cs.ATG" 
out statement);
			Expect(11);
		} else if (la.kind == 113) {
			TryStatement(
#line  1454 "cs.ATG" 
out statement);
		} else if (la.kind == 85) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1456 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1457 "cs.ATG" 
out embeddedStatement);

#line  1457 "cs.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 120) {

#line  1459 "cs.ATG" 
			Statement resourceAcquisitionStmt = null; 
			lexer.NextToken();
			Expect(20);
			ResourceAcquisition(
#line  1461 "cs.ATG" 
out resourceAcquisitionStmt);
			Expect(21);
			EmbeddedStatement(
#line  1462 "cs.ATG" 
out embeddedStatement);

#line  1462 "cs.ATG" 
			statement = new UsingStatement(resourceAcquisitionStmt, embeddedStatement); 
		} else if (la.kind == 118) {
			lexer.NextToken();
			Block(
#line  1464 "cs.ATG" 
out embeddedStatement);

#line  1464 "cs.ATG" 
			statement = new UnsafeStatement(embeddedStatement); 
		} else if (la.kind == 73) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1467 "cs.ATG" 
out type);

#line  1467 "cs.ATG" 
			if (type.PointerNestingLevel == 0) Error("can only fix pointer types");
			List<VariableDeclaration> pointerDeclarators = new List<VariableDeclaration>(1);
			
			Expect(1);

#line  1470 "cs.ATG" 
			string identifier = t.val; 
			Expect(3);
			Expr(
#line  1471 "cs.ATG" 
out expr);

#line  1471 "cs.ATG" 
			pointerDeclarators.Add(new VariableDeclaration(identifier, expr)); 
			while (la.kind == 14) {
				lexer.NextToken();
				Expect(1);

#line  1473 "cs.ATG" 
				identifier = t.val; 
				Expect(3);
				Expr(
#line  1474 "cs.ATG" 
out expr);

#line  1474 "cs.ATG" 
				pointerDeclarators.Add(new VariableDeclaration(identifier, expr)); 
			}
			Expect(21);
			EmbeddedStatement(
#line  1476 "cs.ATG" 
out embeddedStatement);

#line  1476 "cs.ATG" 
			statement = new FixedStatement(type, pointerDeclarators, embeddedStatement); 
		} else SynErr(172);
	}

	void SwitchSections(
#line  1498 "cs.ATG" 
List<SwitchSection> switchSections) {

#line  1500 "cs.ATG" 
		SwitchSection switchSection = new SwitchSection();
		CaseLabel label;
		
		SwitchLabel(
#line  1504 "cs.ATG" 
out label);

#line  1504 "cs.ATG" 
		if (label != null) { switchSection.SwitchLabels.Add(label); } 

#line  1505 "cs.ATG" 
		compilationUnit.BlockStart(switchSection); 
		while (StartOf(25)) {
			if (la.kind == 54 || la.kind == 62) {
				SwitchLabel(
#line  1507 "cs.ATG" 
out label);

#line  1508 "cs.ATG" 
				if (label != null) {
				if (switchSection.Children.Count > 0) {
					// open new section
					compilationUnit.BlockEnd(); switchSections.Add(switchSection);
					switchSection = new SwitchSection();
					compilationUnit.BlockStart(switchSection);
				}
				switchSection.SwitchLabels.Add(label);
				}
				
			} else {
				Statement();
			}
		}

#line  1520 "cs.ATG" 
		compilationUnit.BlockEnd(); switchSections.Add(switchSection); 
	}

	void ForInitializer(
#line  1479 "cs.ATG" 
out List<Statement> initializer) {

#line  1481 "cs.ATG" 
		Statement stmt; 
		initializer = new List<Statement>();
		
		if (
#line  1485 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1485 "cs.ATG" 
out stmt);

#line  1485 "cs.ATG" 
			initializer.Add(stmt);
		} else if (StartOf(5)) {
			StatementExpr(
#line  1486 "cs.ATG" 
out stmt);

#line  1486 "cs.ATG" 
			initializer.Add(stmt);
			while (la.kind == 14) {
				lexer.NextToken();
				StatementExpr(
#line  1486 "cs.ATG" 
out stmt);

#line  1486 "cs.ATG" 
				initializer.Add(stmt);
			}
		} else SynErr(173);
	}

	void ForIterator(
#line  1489 "cs.ATG" 
out List<Statement> iterator) {

#line  1491 "cs.ATG" 
		Statement stmt; 
		iterator = new List<Statement>();
		
		StatementExpr(
#line  1495 "cs.ATG" 
out stmt);

#line  1495 "cs.ATG" 
		iterator.Add(stmt);
		while (la.kind == 14) {
			lexer.NextToken();
			StatementExpr(
#line  1495 "cs.ATG" 
out stmt);

#line  1495 "cs.ATG" 
			iterator.Add(stmt); 
		}
	}

	void GotoStatement(
#line  1573 "cs.ATG" 
out Statement stmt) {

#line  1574 "cs.ATG" 
		Expression expr; stmt = null; 
		Expect(77);
		if (la.kind == 1) {
			lexer.NextToken();

#line  1578 "cs.ATG" 
			stmt = new GotoStatement(t.val); 
			Expect(11);
		} else if (la.kind == 54) {
			lexer.NextToken();
			Expr(
#line  1579 "cs.ATG" 
out expr);
			Expect(11);

#line  1579 "cs.ATG" 
			stmt = new GotoCaseStatement(expr); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(11);

#line  1580 "cs.ATG" 
			stmt = new GotoCaseStatement(null); 
		} else SynErr(174);
	}

	void StatementExpr(
#line  1600 "cs.ATG" 
out Statement stmt) {

#line  1601 "cs.ATG" 
		Expression expr; 
		Expr(
#line  1603 "cs.ATG" 
out expr);

#line  1606 "cs.ATG" 
		stmt = new ExpressionStatement(expr); 
	}

	void TryStatement(
#line  1530 "cs.ATG" 
out Statement tryStatement) {

#line  1532 "cs.ATG" 
		Statement blockStmt = null, finallyStmt = null;
		List<CatchClause> catchClauses = null;
		
		Expect(113);
		Block(
#line  1536 "cs.ATG" 
out blockStmt);
		if (la.kind == 55) {
			CatchClauses(
#line  1538 "cs.ATG" 
out catchClauses);
			if (la.kind == 72) {
				lexer.NextToken();
				Block(
#line  1538 "cs.ATG" 
out finallyStmt);
			}
		} else if (la.kind == 72) {
			lexer.NextToken();
			Block(
#line  1539 "cs.ATG" 
out finallyStmt);
		} else SynErr(175);

#line  1542 "cs.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
			
	}

	void ResourceAcquisition(
#line  1584 "cs.ATG" 
out Statement stmt) {

#line  1586 "cs.ATG" 
		stmt = null;
		Expression expr;
		
		if (
#line  1591 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1591 "cs.ATG" 
out stmt);
		} else if (StartOf(5)) {
			Expr(
#line  1592 "cs.ATG" 
out expr);

#line  1596 "cs.ATG" 
			stmt = new ExpressionStatement(expr); 
		} else SynErr(176);
	}

	void SwitchLabel(
#line  1523 "cs.ATG" 
out CaseLabel label) {

#line  1524 "cs.ATG" 
		Expression expr = null; label = null; 
		if (la.kind == 54) {
			lexer.NextToken();
			Expr(
#line  1526 "cs.ATG" 
out expr);
			Expect(9);

#line  1526 "cs.ATG" 
			label =  new CaseLabel(expr); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(9);

#line  1527 "cs.ATG" 
			label =  new CaseLabel(); 
		} else SynErr(177);
	}

	void CatchClauses(
#line  1547 "cs.ATG" 
out List<CatchClause> catchClauses) {

#line  1549 "cs.ATG" 
		catchClauses = new List<CatchClause>();
		
		Expect(55);

#line  1552 "cs.ATG" 
		string identifier;
		Statement stmt;
		TypeReference typeRef;
		
		if (la.kind == 16) {
			Block(
#line  1558 "cs.ATG" 
out stmt);

#line  1558 "cs.ATG" 
			catchClauses.Add(new CatchClause(stmt)); 
		} else if (la.kind == 20) {
			lexer.NextToken();
			ClassType(
#line  1560 "cs.ATG" 
out typeRef, false);

#line  1560 "cs.ATG" 
			identifier = null; 
			if (la.kind == 1) {
				lexer.NextToken();

#line  1561 "cs.ATG" 
				identifier = t.val; 
			}
			Expect(21);
			Block(
#line  1562 "cs.ATG" 
out stmt);

#line  1563 "cs.ATG" 
			catchClauses.Add(new CatchClause(typeRef, identifier, stmt)); 
			while (
#line  1564 "cs.ATG" 
IsTypedCatch()) {
				Expect(55);
				Expect(20);
				ClassType(
#line  1564 "cs.ATG" 
out typeRef, false);

#line  1564 "cs.ATG" 
				identifier = null; 
				if (la.kind == 1) {
					lexer.NextToken();

#line  1565 "cs.ATG" 
					identifier = t.val; 
				}
				Expect(21);
				Block(
#line  1566 "cs.ATG" 
out stmt);

#line  1567 "cs.ATG" 
				catchClauses.Add(new CatchClause(typeRef, identifier, stmt)); 
			}
			if (la.kind == 55) {
				lexer.NextToken();
				Block(
#line  1569 "cs.ATG" 
out stmt);

#line  1569 "cs.ATG" 
				catchClauses.Add(new CatchClause(stmt)); 
			}
		} else SynErr(178);
	}

	void UnaryExpr(
#line  1627 "cs.ATG" 
out Expression uExpr) {

#line  1629 "cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		ArrayList expressions = new ArrayList();
		uExpr = null;
		
		while (StartOf(26) || 
#line  1651 "cs.ATG" 
IsTypeCast()) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  1638 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Plus)); 
			} else if (la.kind == 5) {
				lexer.NextToken();

#line  1639 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Minus)); 
			} else if (la.kind == 24) {
				lexer.NextToken();

#line  1640 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Not)); 
			} else if (la.kind == 27) {
				lexer.NextToken();

#line  1641 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitNot)); 
			} else if (la.kind == 6) {
				lexer.NextToken();

#line  1642 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Star)); 
			} else if (la.kind == 31) {
				lexer.NextToken();

#line  1643 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Increment)); 
			} else if (la.kind == 32) {
				lexer.NextToken();

#line  1644 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Decrement)); 
			} else if (la.kind == 28) {
				lexer.NextToken();

#line  1645 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitWiseAnd)); 
			} else {
				Expect(20);
				Type(
#line  1651 "cs.ATG" 
out type);
				Expect(21);

#line  1651 "cs.ATG" 
				expressions.Add(new CastExpression(type)); 
			}
		}
		if (
#line  1656 "cs.ATG" 
LastExpressionIsUnaryMinus(expressions) && IsMostNegativeIntegerWithoutTypeSuffix()) {
			Expect(2);

#line  1659 "cs.ATG" 
			expressions.RemoveAt(expressions.Count - 1);
			if (t.literalValue is uint) {
				expr = new PrimitiveExpression(int.MinValue, int.MinValue.ToString());
			} else if (t.literalValue is ulong) {
				expr = new PrimitiveExpression(long.MinValue, long.MinValue.ToString());
			} else {
				throw new Exception("t.literalValue must be uint or ulong");
			}
			
		} else if (StartOf(27)) {
			PrimaryExpr(
#line  1668 "cs.ATG" 
out expr);
		} else SynErr(179);

#line  1670 "cs.ATG" 
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
#line  1840 "cs.ATG" 
ref Expression outExpr) {

#line  1841 "cs.ATG" 
		Expression expr;   
		ConditionalAndExpr(
#line  1843 "cs.ATG" 
ref outExpr);
		while (la.kind == 26) {
			lexer.NextToken();
			UnaryExpr(
#line  1843 "cs.ATG" 
out expr);
			ConditionalAndExpr(
#line  1843 "cs.ATG" 
ref expr);

#line  1843 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalOr, expr);  
		}
	}

	void PrimaryExpr(
#line  1687 "cs.ATG" 
out Expression pexpr) {

#line  1689 "cs.ATG" 
		TypeReference type = null;
		List<TypeReference> typeList = null;
		bool isArrayCreation = false;
		Expression expr;
		pexpr = null;
		
		if (la.kind == 112) {
			lexer.NextToken();

#line  1697 "cs.ATG" 
			pexpr = new PrimitiveExpression(true, "true");  
		} else if (la.kind == 71) {
			lexer.NextToken();

#line  1698 "cs.ATG" 
			pexpr = new PrimitiveExpression(false, "false"); 
		} else if (la.kind == 89) {
			lexer.NextToken();

#line  1699 "cs.ATG" 
			pexpr = new PrimitiveExpression(null, "null");  
		} else if (la.kind == 2) {
			lexer.NextToken();

#line  1700 "cs.ATG" 
			pexpr = new PrimitiveExpression(t.literalValue, t.val);  
		} else if (
#line  1701 "cs.ATG" 
la.kind == Tokens.Identifier && Peek(1).kind == Tokens.DoubleColon) {
			Expect(1);

#line  1702 "cs.ATG" 
			type = new TypeReference(t.val); 
			Expect(10);

#line  1703 "cs.ATG" 
			pexpr = new TypeReferenceExpression(type); 
			Expect(1);

#line  1704 "cs.ATG" 
			if (type.Type == "global") { type.IsGlobal = true; type.Type = (t.val ?? "?"); } else type.Type += "." + (t.val ?? "?"); 
		} else if (la.kind == 1) {
			lexer.NextToken();

#line  1706 "cs.ATG" 
			pexpr = new IdentifierExpression(t.val); 
		} else if (la.kind == 20) {
			lexer.NextToken();
			Expr(
#line  1708 "cs.ATG" 
out expr);
			Expect(21);

#line  1708 "cs.ATG" 
			pexpr = new ParenthesizedExpression(expr); 
		} else if (StartOf(28)) {

#line  1710 "cs.ATG" 
			string val = null; 
			switch (la.kind) {
			case 51: {
				lexer.NextToken();

#line  1712 "cs.ATG" 
				val = "bool"; 
				break;
			}
			case 53: {
				lexer.NextToken();

#line  1713 "cs.ATG" 
				val = "byte"; 
				break;
			}
			case 56: {
				lexer.NextToken();

#line  1714 "cs.ATG" 
				val = "char"; 
				break;
			}
			case 61: {
				lexer.NextToken();

#line  1715 "cs.ATG" 
				val = "decimal"; 
				break;
			}
			case 65: {
				lexer.NextToken();

#line  1716 "cs.ATG" 
				val = "double"; 
				break;
			}
			case 74: {
				lexer.NextToken();

#line  1717 "cs.ATG" 
				val = "float"; 
				break;
			}
			case 81: {
				lexer.NextToken();

#line  1718 "cs.ATG" 
				val = "int"; 
				break;
			}
			case 86: {
				lexer.NextToken();

#line  1719 "cs.ATG" 
				val = "long"; 
				break;
			}
			case 90: {
				lexer.NextToken();

#line  1720 "cs.ATG" 
				val = "object"; 
				break;
			}
			case 101: {
				lexer.NextToken();

#line  1721 "cs.ATG" 
				val = "sbyte"; 
				break;
			}
			case 103: {
				lexer.NextToken();

#line  1722 "cs.ATG" 
				val = "short"; 
				break;
			}
			case 107: {
				lexer.NextToken();

#line  1723 "cs.ATG" 
				val = "string"; 
				break;
			}
			case 115: {
				lexer.NextToken();

#line  1724 "cs.ATG" 
				val = "uint"; 
				break;
			}
			case 116: {
				lexer.NextToken();

#line  1725 "cs.ATG" 
				val = "ulong"; 
				break;
			}
			case 119: {
				lexer.NextToken();

#line  1726 "cs.ATG" 
				val = "ushort"; 
				break;
			}
			}

#line  1727 "cs.ATG" 
			t.val = ""; 
			Expect(15);
			Expect(1);

#line  1727 "cs.ATG" 
			pexpr = new FieldReferenceExpression(new TypeReferenceExpression(val), t.val); 
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  1729 "cs.ATG" 
			pexpr = new ThisReferenceExpression(); 
		} else if (la.kind == 50) {
			lexer.NextToken();

#line  1731 "cs.ATG" 
			Expression retExpr = new BaseReferenceExpression(); 
			if (la.kind == 15) {
				lexer.NextToken();
				Expect(1);

#line  1733 "cs.ATG" 
				retExpr = new FieldReferenceExpression(retExpr, t.val); 
			} else if (la.kind == 18) {
				lexer.NextToken();
				Expr(
#line  1734 "cs.ATG" 
out expr);

#line  1734 "cs.ATG" 
				List<Expression> indices = new List<Expression>(); if (expr != null) { indices.Add(expr); } 
				while (la.kind == 14) {
					lexer.NextToken();
					Expr(
#line  1735 "cs.ATG" 
out expr);

#line  1735 "cs.ATG" 
					if (expr != null) { indices.Add(expr); } 
				}
				Expect(19);

#line  1736 "cs.ATG" 
				retExpr = new IndexerExpression(retExpr, indices); 
			} else SynErr(180);

#line  1737 "cs.ATG" 
			pexpr = retExpr; 
		} else if (la.kind == 88) {
			lexer.NextToken();
			NonArrayType(
#line  1738 "cs.ATG" 
out type);

#line  1739 "cs.ATG" 
			List<Expression> parameters = new List<Expression>(); 
			if (la.kind == 20) {
				lexer.NextToken();

#line  1744 "cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters); 
				if (StartOf(21)) {
					Argument(
#line  1745 "cs.ATG" 
out expr);

#line  1745 "cs.ATG" 
					if (expr != null) { parameters.Add(expr); } 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  1746 "cs.ATG" 
out expr);

#line  1746 "cs.ATG" 
						if (expr != null) { parameters.Add(expr); } 
					}
				}
				Expect(21);

#line  1748 "cs.ATG" 
				pexpr = oce; 
			} else if (la.kind == 18) {
				lexer.NextToken();

#line  1750 "cs.ATG" 
				isArrayCreation = true; ArrayCreateExpression ace = new ArrayCreateExpression(type); pexpr = ace; 

#line  1751 "cs.ATG" 
				int dims = 0; List<int> ranks = new List<int>(); 
				if (la.kind == 14 || la.kind == 19) {
					while (la.kind == 14) {
						lexer.NextToken();

#line  1753 "cs.ATG" 
						dims += 1; 
					}
					Expect(19);

#line  1754 "cs.ATG" 
					ranks.Add(dims); dims = 0; 
					while (la.kind == 18) {
						lexer.NextToken();
						while (la.kind == 14) {
							lexer.NextToken();

#line  1755 "cs.ATG" 
							++dims; 
						}
						Expect(19);

#line  1755 "cs.ATG" 
						ranks.Add(dims); dims = 0; 
					}

#line  1756 "cs.ATG" 
					ace.CreateType.RankSpecifier = ranks.ToArray(); 
					ArrayInitializer(
#line  1757 "cs.ATG" 
out expr);

#line  1757 "cs.ATG" 
					ace.ArrayInitializer = (ArrayInitializerExpression)expr; 
				} else if (StartOf(5)) {
					Expr(
#line  1758 "cs.ATG" 
out expr);

#line  1758 "cs.ATG" 
					if (expr != null) parameters.Add(expr); 
					while (la.kind == 14) {
						lexer.NextToken();

#line  1759 "cs.ATG" 
						dims += 1; 
						Expr(
#line  1760 "cs.ATG" 
out expr);

#line  1760 "cs.ATG" 
						if (expr != null) parameters.Add(expr); 
					}
					Expect(19);

#line  1762 "cs.ATG" 
					ranks.Add(dims); ace.Arguments = parameters; dims = 0; 
					while (la.kind == 18) {
						lexer.NextToken();
						while (la.kind == 14) {
							lexer.NextToken();

#line  1763 "cs.ATG" 
							++dims; 
						}
						Expect(19);

#line  1763 "cs.ATG" 
						ranks.Add(dims); dims = 0; 
					}

#line  1764 "cs.ATG" 
					ace.CreateType.RankSpecifier = ranks.ToArray(); 
					if (la.kind == 16) {
						ArrayInitializer(
#line  1765 "cs.ATG" 
out expr);

#line  1765 "cs.ATG" 
						ace.ArrayInitializer = (ArrayInitializerExpression)expr; 
					}
				} else SynErr(181);
			} else SynErr(182);
		} else if (la.kind == 114) {
			lexer.NextToken();
			Expect(20);
			if (
#line  1770 "cs.ATG" 
NotVoidPointer()) {
				Expect(122);

#line  1770 "cs.ATG" 
				type = new TypeReference("void"); 
			} else if (StartOf(9)) {
				TypeWithRestriction(
#line  1771 "cs.ATG" 
out type, true, true);
			} else SynErr(183);
			Expect(21);

#line  1772 "cs.ATG" 
			pexpr = new TypeOfExpression(type); 
		} else if (la.kind == 62) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1774 "cs.ATG" 
out type);
			Expect(21);

#line  1774 "cs.ATG" 
			pexpr = new DefaultValueExpression(type); 
		} else if (la.kind == 104) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1775 "cs.ATG" 
out type);
			Expect(21);

#line  1775 "cs.ATG" 
			pexpr = new SizeOfExpression(type); 
		} else if (la.kind == 57) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1776 "cs.ATG" 
out expr);
			Expect(21);

#line  1776 "cs.ATG" 
			pexpr = new CheckedExpression(expr); 
		} else if (la.kind == 117) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1777 "cs.ATG" 
out expr);
			Expect(21);

#line  1777 "cs.ATG" 
			pexpr = new UncheckedExpression(expr); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			AnonymousMethodExpr(
#line  1778 "cs.ATG" 
out expr);

#line  1778 "cs.ATG" 
			pexpr = expr; 
		} else SynErr(184);
		while (StartOf(29) || 
#line  1789 "cs.ATG" 
IsGenericFollowedBy(Tokens.Dot) && IsTypeReferenceExpression(pexpr) || 
#line  1798 "cs.ATG" 
IsGenericFollowedBy(Tokens.OpenParenthesis)) {
			if (la.kind == 31 || la.kind == 32) {
				if (la.kind == 31) {
					lexer.NextToken();

#line  1782 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostIncrement); 
				} else if (la.kind == 32) {
					lexer.NextToken();

#line  1783 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostDecrement); 
				} else SynErr(185);
			} else if (la.kind == 47) {
				lexer.NextToken();
				Expect(1);

#line  1786 "cs.ATG" 
				pexpr = new PointerReferenceExpression(pexpr, t.val); 
			} else if (la.kind == 15) {
				lexer.NextToken();
				Expect(1);

#line  1787 "cs.ATG" 
				pexpr = new FieldReferenceExpression(pexpr, t.val);
			} else if (
#line  1789 "cs.ATG" 
IsGenericFollowedBy(Tokens.Dot) && IsTypeReferenceExpression(pexpr)) {
				TypeArgumentList(
#line  1790 "cs.ATG" 
out typeList, false);
				Expect(15);
				Expect(1);

#line  1792 "cs.ATG" 
				pexpr = new FieldReferenceExpression(GetTypeReferenceExpression(pexpr, typeList), t.val);
			} else if (la.kind == 20) {
				lexer.NextToken();

#line  1794 "cs.ATG" 
				List<Expression> parameters = new List<Expression>(); 
				if (StartOf(21)) {
					Argument(
#line  1795 "cs.ATG" 
out expr);

#line  1795 "cs.ATG" 
					if (expr != null) {parameters.Add(expr);} 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  1796 "cs.ATG" 
out expr);

#line  1796 "cs.ATG" 
						if (expr != null) {parameters.Add(expr);} 
					}
				}
				Expect(21);

#line  1797 "cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters); 
			} else if (
#line  1798 "cs.ATG" 
IsGenericFollowedBy(Tokens.OpenParenthesis)) {
				TypeArgumentList(
#line  1798 "cs.ATG" 
out typeList, false);
				Expect(20);

#line  1799 "cs.ATG" 
				List<Expression> parameters = new List<Expression>(); 
				if (StartOf(21)) {
					Argument(
#line  1800 "cs.ATG" 
out expr);

#line  1800 "cs.ATG" 
					if (expr != null) {parameters.Add(expr);} 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  1801 "cs.ATG" 
out expr);

#line  1801 "cs.ATG" 
						if (expr != null) {parameters.Add(expr);} 
					}
				}
				Expect(21);

#line  1802 "cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters, typeList); 
			} else {

#line  1804 "cs.ATG" 
				if (isArrayCreation) Error("element access not allow on array creation");
				List<Expression> indices = new List<Expression>();
				
				lexer.NextToken();
				Expr(
#line  1807 "cs.ATG" 
out expr);

#line  1807 "cs.ATG" 
				if (expr != null) { indices.Add(expr); } 
				while (la.kind == 14) {
					lexer.NextToken();
					Expr(
#line  1808 "cs.ATG" 
out expr);

#line  1808 "cs.ATG" 
					if (expr != null) { indices.Add(expr); } 
				}
				Expect(19);

#line  1809 "cs.ATG" 
				pexpr = new IndexerExpression(pexpr, indices); 
			}
		}
	}

	void AnonymousMethodExpr(
#line  1813 "cs.ATG" 
out Expression outExpr) {

#line  1815 "cs.ATG" 
		AnonymousMethodExpression expr = new AnonymousMethodExpression();
		expr.StartLocation = t.Location;
		Statement stmt;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		outExpr = expr;
		
		if (la.kind == 20) {
			lexer.NextToken();
			if (StartOf(10)) {
				FormalParameterList(
#line  1824 "cs.ATG" 
p);

#line  1824 "cs.ATG" 
				expr.Parameters = p; 
			}
			Expect(21);

#line  1826 "cs.ATG" 
			expr.HasParameterList = true; 
		}

#line  1830 "cs.ATG" 
		if (compilationUnit != null) { 
		Block(
#line  1831 "cs.ATG" 
out stmt);

#line  1831 "cs.ATG" 
		expr.Body  = (BlockStatement)stmt; 

#line  1832 "cs.ATG" 
		} else { 
		Expect(16);

#line  1834 "cs.ATG" 
		lexer.SkipCurrentBlock(0); 
		Expect(17);

#line  1836 "cs.ATG" 
		} 

#line  1837 "cs.ATG" 
		expr.EndLocation = t.Location; 
	}

	void TypeArgumentList(
#line  2010 "cs.ATG" 
out List<TypeReference> types, bool canBeUnbound) {

#line  2012 "cs.ATG" 
		types = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(23);
		if (
#line  2017 "cs.ATG" 
canBeUnbound && (la.kind == Tokens.GreaterThan || la.kind == Tokens.Comma)) {

#line  2018 "cs.ATG" 
			types.Add(TypeReference.Null); 
			while (la.kind == 14) {
				lexer.NextToken();

#line  2019 "cs.ATG" 
				types.Add(TypeReference.Null); 
			}
		} else if (StartOf(9)) {
			Type(
#line  2020 "cs.ATG" 
out type);

#line  2020 "cs.ATG" 
			if (type != null) { types.Add(type); } 
			while (la.kind == 14) {
				lexer.NextToken();
				Type(
#line  2021 "cs.ATG" 
out type);

#line  2021 "cs.ATG" 
				if (type != null) { types.Add(type); } 
			}
		} else SynErr(186);
		Expect(22);
	}

	void ConditionalAndExpr(
#line  1846 "cs.ATG" 
ref Expression outExpr) {

#line  1847 "cs.ATG" 
		Expression expr; 
		InclusiveOrExpr(
#line  1849 "cs.ATG" 
ref outExpr);
		while (la.kind == 25) {
			lexer.NextToken();
			UnaryExpr(
#line  1849 "cs.ATG" 
out expr);
			InclusiveOrExpr(
#line  1849 "cs.ATG" 
ref expr);

#line  1849 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalAnd, expr);  
		}
	}

	void InclusiveOrExpr(
#line  1852 "cs.ATG" 
ref Expression outExpr) {

#line  1853 "cs.ATG" 
		Expression expr; 
		ExclusiveOrExpr(
#line  1855 "cs.ATG" 
ref outExpr);
		while (la.kind == 29) {
			lexer.NextToken();
			UnaryExpr(
#line  1855 "cs.ATG" 
out expr);
			ExclusiveOrExpr(
#line  1855 "cs.ATG" 
ref expr);

#line  1855 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseOr, expr);  
		}
	}

	void ExclusiveOrExpr(
#line  1858 "cs.ATG" 
ref Expression outExpr) {

#line  1859 "cs.ATG" 
		Expression expr; 
		AndExpr(
#line  1861 "cs.ATG" 
ref outExpr);
		while (la.kind == 30) {
			lexer.NextToken();
			UnaryExpr(
#line  1861 "cs.ATG" 
out expr);
			AndExpr(
#line  1861 "cs.ATG" 
ref expr);

#line  1861 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.ExclusiveOr, expr);  
		}
	}

	void AndExpr(
#line  1864 "cs.ATG" 
ref Expression outExpr) {

#line  1865 "cs.ATG" 
		Expression expr; 
		EqualityExpr(
#line  1867 "cs.ATG" 
ref outExpr);
		while (la.kind == 28) {
			lexer.NextToken();
			UnaryExpr(
#line  1867 "cs.ATG" 
out expr);
			EqualityExpr(
#line  1867 "cs.ATG" 
ref expr);

#line  1867 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseAnd, expr);  
		}
	}

	void EqualityExpr(
#line  1870 "cs.ATG" 
ref Expression outExpr) {

#line  1872 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		RelationalExpr(
#line  1876 "cs.ATG" 
ref outExpr);
		while (la.kind == 33 || la.kind == 34) {
			if (la.kind == 34) {
				lexer.NextToken();

#line  1879 "cs.ATG" 
				op = BinaryOperatorType.InEquality; 
			} else {
				lexer.NextToken();

#line  1880 "cs.ATG" 
				op = BinaryOperatorType.Equality; 
			}
			UnaryExpr(
#line  1882 "cs.ATG" 
out expr);
			RelationalExpr(
#line  1882 "cs.ATG" 
ref expr);

#line  1882 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void RelationalExpr(
#line  1886 "cs.ATG" 
ref Expression outExpr) {

#line  1888 "cs.ATG" 
		TypeReference type;
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  1893 "cs.ATG" 
ref outExpr);
		while (StartOf(30)) {
			if (StartOf(31)) {
				if (la.kind == 23) {
					lexer.NextToken();

#line  1895 "cs.ATG" 
					op = BinaryOperatorType.LessThan; 
				} else if (la.kind == 22) {
					lexer.NextToken();

#line  1896 "cs.ATG" 
					op = BinaryOperatorType.GreaterThan; 
				} else if (la.kind == 36) {
					lexer.NextToken();

#line  1897 "cs.ATG" 
					op = BinaryOperatorType.LessThanOrEqual; 
				} else if (la.kind == 35) {
					lexer.NextToken();

#line  1898 "cs.ATG" 
					op = BinaryOperatorType.GreaterThanOrEqual; 
				} else SynErr(187);
				UnaryExpr(
#line  1900 "cs.ATG" 
out expr);
				ShiftExpr(
#line  1901 "cs.ATG" 
ref expr);

#line  1902 "cs.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
			} else {
				if (la.kind == 84) {
					lexer.NextToken();
					TypeWithRestriction(
#line  1905 "cs.ATG" 
out type, false, false);
					if (
#line  1906 "cs.ATG" 
la.kind == Tokens.Question && Tokens.CastFollower[Peek(1).kind] == false) {
						NullableQuestionMark(
#line  1907 "cs.ATG" 
ref type);
					}

#line  1908 "cs.ATG" 
					outExpr = new TypeOfIsExpression(outExpr, type); 
				} else if (la.kind == 49) {
					lexer.NextToken();
					TypeWithRestriction(
#line  1910 "cs.ATG" 
out type, false, false);
					if (
#line  1911 "cs.ATG" 
la.kind == Tokens.Question && Tokens.CastFollower[Peek(1).kind] == false) {
						NullableQuestionMark(
#line  1912 "cs.ATG" 
ref type);
					}

#line  1913 "cs.ATG" 
					outExpr = new CastExpression(type, outExpr, CastType.TryCast); 
				} else SynErr(188);
			}
		}
	}

	void ShiftExpr(
#line  1918 "cs.ATG" 
ref Expression outExpr) {

#line  1920 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		AdditiveExpr(
#line  1924 "cs.ATG" 
ref outExpr);
		while (la.kind == 37 || 
#line  1927 "cs.ATG" 
IsShiftRight()) {
			if (la.kind == 37) {
				lexer.NextToken();

#line  1926 "cs.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				Expect(22);
				Expect(22);

#line  1928 "cs.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			UnaryExpr(
#line  1931 "cs.ATG" 
out expr);
			AdditiveExpr(
#line  1931 "cs.ATG" 
ref expr);

#line  1931 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AdditiveExpr(
#line  1935 "cs.ATG" 
ref Expression outExpr) {

#line  1937 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		MultiplicativeExpr(
#line  1941 "cs.ATG" 
ref outExpr);
		while (la.kind == 4 || la.kind == 5) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  1944 "cs.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  1945 "cs.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			UnaryExpr(
#line  1947 "cs.ATG" 
out expr);
			MultiplicativeExpr(
#line  1947 "cs.ATG" 
ref expr);

#line  1947 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void MultiplicativeExpr(
#line  1951 "cs.ATG" 
ref Expression outExpr) {

#line  1953 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		while (la.kind == 6 || la.kind == 7 || la.kind == 8) {
			if (la.kind == 6) {
				lexer.NextToken();

#line  1959 "cs.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else if (la.kind == 7) {
				lexer.NextToken();

#line  1960 "cs.ATG" 
				op = BinaryOperatorType.Divide; 
			} else {
				lexer.NextToken();

#line  1961 "cs.ATG" 
				op = BinaryOperatorType.Modulus; 
			}
			UnaryExpr(
#line  1963 "cs.ATG" 
out expr);

#line  1963 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void TypeParameterConstraintsClauseBase(
#line  2067 "cs.ATG" 
out TypeReference type) {

#line  2068 "cs.ATG" 
		TypeReference t; type = null; 
		if (la.kind == 108) {
			lexer.NextToken();

#line  2070 "cs.ATG" 
			type = TypeReference.StructConstraint; 
		} else if (la.kind == 58) {
			lexer.NextToken();

#line  2071 "cs.ATG" 
			type = TypeReference.ClassConstraint; 
		} else if (la.kind == 88) {
			lexer.NextToken();
			Expect(20);
			Expect(21);

#line  2072 "cs.ATG" 
			type = TypeReference.NewConstraint; 
		} else if (StartOf(9)) {
			Type(
#line  2073 "cs.ATG" 
out t);

#line  2073 "cs.ATG" 
			type = t; 
		} else SynErr(189);
	}


	
	public override void Parse()
	{
		CS();

	}
	
	protected override void SynErr(int line, int col, int errorNumber)
	{
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
			case 151: s = "invalid StructMemberDecl"; break;
			case 152: s = "invalid InterfaceMemberDecl"; break;
			case 153: s = "invalid InterfaceMemberDecl"; break;
			case 154: s = "invalid InterfaceMemberDecl"; break;
			case 155: s = "invalid TypeWithRestriction"; break;
			case 156: s = "invalid TypeWithRestriction"; break;
			case 157: s = "invalid SimpleType"; break;
			case 158: s = "invalid AccessorModifiers"; break;
			case 159: s = "invalid EventAccessorDecls"; break;
			case 160: s = "invalid ConstructorInitializer"; break;
			case 161: s = "invalid OverloadableOperator"; break;
			case 162: s = "invalid AccessorDecls"; break;
			case 163: s = "invalid InterfaceAccessors"; break;
			case 164: s = "invalid InterfaceAccessors"; break;
			case 165: s = "invalid GetAccessorDecl"; break;
			case 166: s = "invalid SetAccessorDecl"; break;
			case 167: s = "invalid VariableInitializer"; break;
			case 168: s = "invalid Statement"; break;
			case 169: s = "invalid AssignmentOperator"; break;
			case 170: s = "invalid EmbeddedStatement"; break;
			case 171: s = "invalid EmbeddedStatement"; break;
			case 172: s = "invalid EmbeddedStatement"; break;
			case 173: s = "invalid ForInitializer"; break;
			case 174: s = "invalid GotoStatement"; break;
			case 175: s = "invalid TryStatement"; break;
			case 176: s = "invalid ResourceAcquisition"; break;
			case 177: s = "invalid SwitchLabel"; break;
			case 178: s = "invalid CatchClauses"; break;
			case 179: s = "invalid UnaryExpr"; break;
			case 180: s = "invalid PrimaryExpr"; break;
			case 181: s = "invalid PrimaryExpr"; break;
			case 182: s = "invalid PrimaryExpr"; break;
			case 183: s = "invalid PrimaryExpr"; break;
			case 184: s = "invalid PrimaryExpr"; break;
			case 185: s = "invalid PrimaryExpr"; break;
			case 186: s = "invalid TypeArgumentList"; break;
			case 187: s = "invalid RelationalExpr"; break;
			case 188: s = "invalid RelationalExpr"; break;
			case 189: s = "invalid TypeParameterConstraintsClauseBase"; break;

			default: s = "error " + errorNumber; break;
		}
		this.Errors.Error(line, col, s);
	}
	
	private bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	static bool[,] set = {
	{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,T, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,x,x,T, T,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,T,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, T,T,T,T, T,T,x,T, T,T,T,x, T,T,T,T, x,T,T,T, x,T,T,T, T,T,T,x, x,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, T,T,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,T,T,x, x,x,x,T, x,T,T,T, T,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, T,x,T,x, x,x,x,T, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,T,T,x, x,x,x,T, x,T,T,T, x,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,T,x, x,T,T,x, x,x,x,T, x,T,T,T, x,x,T,x, T,x,T,x, x,T,x,T, T,T,T,x, x,T,T,T, x,x,T,T, T,x,x,x, x,x,x,T, T,x,T,T, x,T,T,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, T,x,x,x, x,x,x,T, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,T, T,T,T,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,T, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,T,T, x,T,x,T, x,T,x,T, T,T,x,x, x,x,T,x, x,x,x,T, x,T,T,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, T,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,T,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, T,T,x,T, T,T,T,T, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,T,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, T,x,x,x, x,x,x,T, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,x,x, T,T,x,x, T,T,T,T, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,T,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,x, T,T,x,T, T,T,T,T, T,T,x,x, x,x,x,T, x,T,T,T, T,T,T,x, x,T,x,x, x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,T,x,T, T,x,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x},
	{x,x,x,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,T,x,x, T,T,x,x, x,T,T,T, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, T,x,x,T, x,x,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,T,x,x, T,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,x,T, x,x,x,x, x,x,x,T, T,x,x,T, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x}

	};
} // end Parser

}