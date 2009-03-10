
#line  1 "VBNET.ATG" 
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser.VB;
using ASTAttribute = ICSharpCode.NRefactory.Ast.Attribute;
/*
  Parser.frame file for NRefactory.
 */
using System;
using System.Reflection;

namespace ICSharpCode.NRefactory.Parser.VB {



partial class Parser : AbstractParser
{
	const int maxT = 222;

	const  bool   T            = true;
	const  bool   x            = false;
	

#line  12 "VBNET.ATG" 


/*

*/

	void VBNET() {

#line  246 "VBNET.ATG" 
		lexer.NextToken(); // get the first token
		compilationUnit = new CompilationUnit();
		
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (la.kind == 159) {
			OptionStmt();
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		while (la.kind == 124) {
			ImportsStmt();
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		while (
#line  253 "VBNET.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		Expect(0);
	}

	void EndOfStmt() {
		if (la.kind == 1) {
			lexer.NextToken();
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(223);
	}

	void OptionStmt() {

#line  258 "VBNET.ATG" 
		INode node = null; bool val = true; 
		Expect(159);

#line  259 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (la.kind == 108) {
			lexer.NextToken();
			if (la.kind == 156 || la.kind == 157) {
				OptionValue(
#line  261 "VBNET.ATG" 
ref val);
			}

#line  262 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Explicit, val); 
		} else if (la.kind == 192) {
			lexer.NextToken();
			if (la.kind == 156 || la.kind == 157) {
				OptionValue(
#line  264 "VBNET.ATG" 
ref val);
			}

#line  265 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Strict, val); 
		} else if (la.kind == 74) {
			lexer.NextToken();
			if (la.kind == 54) {
				lexer.NextToken();

#line  267 "VBNET.ATG" 
				node = new OptionDeclaration(OptionType.CompareBinary, val); 
			} else if (la.kind == 198) {
				lexer.NextToken();

#line  268 "VBNET.ATG" 
				node = new OptionDeclaration(OptionType.CompareText, val); 
			} else SynErr(224);
		} else if (la.kind == 126) {
			lexer.NextToken();
			if (la.kind == 156 || la.kind == 157) {
				OptionValue(
#line  271 "VBNET.ATG" 
ref val);
			}

#line  272 "VBNET.ATG" 
			node = new OptionDeclaration(OptionType.Infer, val); 
		} else SynErr(225);
		EndOfStmt();

#line  276 "VBNET.ATG" 
		if (node != null) {
		node.StartLocation = startPos;
		node.EndLocation   = t.Location;
		compilationUnit.AddChild(node);
		}
		
	}

	void ImportsStmt() {

#line  299 "VBNET.ATG" 
		List<Using> usings = new List<Using>();
		
		Expect(124);

#line  303 "VBNET.ATG" 
		Location startPos = t.Location;
		Using u;
		
		ImportClause(
#line  306 "VBNET.ATG" 
out u);

#line  306 "VBNET.ATG" 
		if (u != null) { usings.Add(u); } 
		while (la.kind == 12) {
			lexer.NextToken();
			ImportClause(
#line  308 "VBNET.ATG" 
out u);

#line  308 "VBNET.ATG" 
			if (u != null) { usings.Add(u); } 
		}
		EndOfStmt();

#line  312 "VBNET.ATG" 
		UsingDeclaration usingDeclaration = new UsingDeclaration(usings);
		usingDeclaration.StartLocation = startPos;
		usingDeclaration.EndLocation   = t.Location;
		compilationUnit.AddChild(usingDeclaration);
		
	}

	void GlobalAttributeSection() {
		Expect(28);

#line  2521 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (la.kind == 52) {
			lexer.NextToken();
		} else if (la.kind == 141) {
			lexer.NextToken();
		} else SynErr(226);

#line  2523 "VBNET.ATG" 
		string attributeTarget = t.val != null ? t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture) : null;
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(11);
		Attribute(
#line  2527 "VBNET.ATG" 
out attribute);

#line  2527 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  2528 "VBNET.ATG" 
NotFinalComma()) {
			if (la.kind == 12) {
				lexer.NextToken();
				if (la.kind == 52) {
					lexer.NextToken();
				} else if (la.kind == 141) {
					lexer.NextToken();
				} else SynErr(227);
				Expect(11);
			}
			Attribute(
#line  2528 "VBNET.ATG" 
out attribute);

#line  2528 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(27);
		EndOfStmt();

#line  2533 "VBNET.ATG" 
		AttributeSection section = new AttributeSection {
		AttributeTarget = attributeTarget,
		Attributes = attributes,
		StartLocation = startPos,
		EndLocation = t.EndLocation
		};
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  341 "VBNET.ATG" 
		ModifierList m = new ModifierList();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		string qualident;
		
		if (la.kind == 146) {
			lexer.NextToken();

#line  348 "VBNET.ATG" 
			Location startPos = t.Location;
			
			Qualident(
#line  350 "VBNET.ATG" 
out qualident);

#line  352 "VBNET.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			compilationUnit.AddChild(node);
			compilationUnit.BlockStart(node);
			
			EndOfStmt();
			NamespaceBody();

#line  360 "VBNET.ATG" 
			node.EndLocation = t.Location;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 28) {
				AttributeSection(
#line  364 "VBNET.ATG" 
out section);

#line  364 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  365 "VBNET.ATG" 
m);
			}
			NonModuleDeclaration(
#line  365 "VBNET.ATG" 
m, attributes);
		} else SynErr(228);
	}

	void OptionValue(
#line  284 "VBNET.ATG" 
ref bool val) {
		if (la.kind == 157) {
			lexer.NextToken();

#line  286 "VBNET.ATG" 
			val = true; 
		} else if (la.kind == 156) {
			lexer.NextToken();

#line  288 "VBNET.ATG" 
			val = false; 
		} else SynErr(229);
	}

	void ImportClause(
#line  319 "VBNET.ATG" 
out Using u) {

#line  321 "VBNET.ATG" 
		string qualident  = null;
		TypeReference aliasedType = null;
		u = null;
		
		Qualident(
#line  325 "VBNET.ATG" 
out qualident);
		if (la.kind == 10) {
			lexer.NextToken();
			TypeName(
#line  326 "VBNET.ATG" 
out aliasedType);
		}

#line  328 "VBNET.ATG" 
		if (qualident != null && qualident.Length > 0) {
		if (aliasedType != null) {
			u = new Using(qualident, aliasedType);
		} else {
			u = new Using(qualident);
		}
		}
		
	}

	void Qualident(
#line  3279 "VBNET.ATG" 
out string qualident) {

#line  3281 "VBNET.ATG" 
		string name;
		qualidentBuilder.Length = 0; 
		
		Identifier();

#line  3285 "VBNET.ATG" 
		qualidentBuilder.Append(t.val); 
		while (
#line  3286 "VBNET.ATG" 
DotAndIdentOrKw()) {
			Expect(16);
			IdentifierOrKeyword(
#line  3286 "VBNET.ATG" 
out name);

#line  3286 "VBNET.ATG" 
			qualidentBuilder.Append('.'); qualidentBuilder.Append(name); 
		}

#line  3288 "VBNET.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void TypeName(
#line  2394 "VBNET.ATG" 
out TypeReference typeref) {

#line  2395 "VBNET.ATG" 
		ArrayList rank = null; 
		NonArrayTypeName(
#line  2397 "VBNET.ATG" 
out typeref, false);
		ArrayTypeModifiers(
#line  2401 "VBNET.ATG" 
out rank);

#line  2402 "VBNET.ATG" 
		if (rank != null && typeref != null) {
		typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
		}
		
	}

	void NamespaceBody() {
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		Expect(100);
		Expect(146);
		EndOfStmt();
	}

	void AttributeSection(
#line  2596 "VBNET.ATG" 
out AttributeSection section) {

#line  2598 "VBNET.ATG" 
		string attributeTarget = "";List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		
		Expect(28);

#line  2602 "VBNET.ATG" 
		Location startPos = t.Location; 
		if (
#line  2603 "VBNET.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 106) {
				lexer.NextToken();

#line  2604 "VBNET.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 180) {
				lexer.NextToken();

#line  2605 "VBNET.ATG" 
				attributeTarget = "return";
			} else {
				Identifier();

#line  2608 "VBNET.ATG" 
				string val = t.val.ToLower(System.Globalization.CultureInfo.InvariantCulture);
				if (val != "field"	|| val != "method" ||
					val != "module" || val != "param"  ||
					val != "property" || val != "type")
				Error("attribute target specifier (event, return, field," +
						"method, module, param, property, or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(11);
		}
		Attribute(
#line  2618 "VBNET.ATG" 
out attribute);

#line  2618 "VBNET.ATG" 
		attributes.Add(attribute); 
		while (
#line  2619 "VBNET.ATG" 
NotFinalComma()) {
			Expect(12);
			Attribute(
#line  2619 "VBNET.ATG" 
out attribute);

#line  2619 "VBNET.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 12) {
			lexer.NextToken();
		}
		Expect(27);

#line  2623 "VBNET.ATG" 
		section = new AttributeSection {
		AttributeTarget = attributeTarget,
		Attributes = attributes,
		StartLocation = startPos,
		EndLocation = t.EndLocation
		};
		
	}

	void TypeModifier(
#line  3362 "VBNET.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 173: {
			lexer.NextToken();

#line  3363 "VBNET.ATG" 
			m.Add(Modifiers.Public, t.Location); 
			break;
		}
		case 172: {
			lexer.NextToken();

#line  3364 "VBNET.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			break;
		}
		case 112: {
			lexer.NextToken();

#line  3365 "VBNET.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			break;
		}
		case 170: {
			lexer.NextToken();

#line  3366 "VBNET.ATG" 
			m.Add(Modifiers.Private, t.Location); 
			break;
		}
		case 185: {
			lexer.NextToken();

#line  3367 "VBNET.ATG" 
			m.Add(Modifiers.Static, t.Location); 
			break;
		}
		case 184: {
			lexer.NextToken();

#line  3368 "VBNET.ATG" 
			m.Add(Modifiers.New, t.Location); 
			break;
		}
		case 142: {
			lexer.NextToken();

#line  3369 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location); 
			break;
		}
		case 152: {
			lexer.NextToken();

#line  3370 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location); 
			break;
		}
		case 168: {
			lexer.NextToken();

#line  3371 "VBNET.ATG" 
			m.Add(Modifiers.Partial, t.Location); 
			break;
		}
		default: SynErr(230); break;
		}
	}

	void NonModuleDeclaration(
#line  424 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  426 "VBNET.ATG" 
		TypeReference typeRef = null;
		List<TypeReference> baseInterfaces = null;
		
		switch (la.kind) {
		case 71: {

#line  429 "VBNET.ATG" 
			m.Check(Modifiers.Classes); 
			lexer.NextToken();

#line  432 "VBNET.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = t.Location;
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			
			newType.Type       = ClassType.Class;
			
			Identifier();

#line  439 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  440 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  442 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			if (la.kind == 127) {
				ClassBaseType(
#line  443 "VBNET.ATG" 
out typeRef);

#line  443 "VBNET.ATG" 
				SafeAdd(newType, newType.BaseTypes, typeRef); 
			}
			while (la.kind == 123) {
				TypeImplementsClause(
#line  444 "VBNET.ATG" 
out baseInterfaces);

#line  444 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces); 
			}
			ClassBody(
#line  445 "VBNET.ATG" 
newType);
			Expect(100);
			Expect(71);

#line  446 "VBNET.ATG" 
			newType.EndLocation = t.EndLocation; 
			EndOfStmt();

#line  449 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 141: {
			lexer.NextToken();

#line  453 "VBNET.ATG" 
			m.Check(Modifiers.VBModules);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			newType.Type = ClassType.Module;
			
			Identifier();

#line  460 "VBNET.ATG" 
			newType.Name = t.val; 
			EndOfStmt();

#line  462 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			ModuleBody(
#line  463 "VBNET.ATG" 
newType);

#line  465 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 194: {
			lexer.NextToken();

#line  469 "VBNET.ATG" 
			m.Check(Modifiers.VBStructures);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			newType.Type = ClassType.Struct;
			
			Identifier();

#line  476 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  477 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  479 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			while (la.kind == 123) {
				TypeImplementsClause(
#line  480 "VBNET.ATG" 
out baseInterfaces);

#line  480 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces);
			}
			StructureBody(
#line  481 "VBNET.ATG" 
newType);

#line  483 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 102: {
			lexer.NextToken();

#line  488 "VBNET.ATG" 
			m.Check(Modifiers.VBEnums);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			
			newType.Type = ClassType.Enum;
			
			Identifier();

#line  496 "VBNET.ATG" 
			newType.Name = t.val; 
			if (la.kind == 50) {
				lexer.NextToken();
				NonArrayTypeName(
#line  497 "VBNET.ATG" 
out typeRef, false);

#line  497 "VBNET.ATG" 
				SafeAdd(newType, newType.BaseTypes, typeRef); 
			}
			EndOfStmt();

#line  499 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			EnumBody(
#line  500 "VBNET.ATG" 
newType);

#line  502 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 129: {
			lexer.NextToken();

#line  507 "VBNET.ATG" 
			m.Check(Modifiers.VBInterfacs);
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.Type = ClassType.Interface;
			
			Identifier();

#line  514 "VBNET.ATG" 
			newType.Name = t.val; 
			TypeParameterList(
#line  515 "VBNET.ATG" 
newType.Templates);
			EndOfStmt();

#line  517 "VBNET.ATG" 
			newType.BodyStartLocation = t.Location; 
			while (la.kind == 127) {
				InterfaceBase(
#line  518 "VBNET.ATG" 
out baseInterfaces);

#line  518 "VBNET.ATG" 
				newType.BaseTypes.AddRange(baseInterfaces); 
			}
			InterfaceBody(
#line  519 "VBNET.ATG" 
newType);

#line  521 "VBNET.ATG" 
			compilationUnit.BlockEnd();
			
			break;
		}
		case 90: {
			lexer.NextToken();

#line  526 "VBNET.ATG" 
			m.Check(Modifiers.VBDelegates);
			DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
			delegateDeclr.ReturnType = new TypeReference("System.Void", true);
			delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
			List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
			
			if (la.kind == 195) {
				lexer.NextToken();
				Identifier();

#line  533 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				TypeParameterList(
#line  534 "VBNET.ATG" 
delegateDeclr.Templates);
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  535 "VBNET.ATG" 
p);
					}
					Expect(26);

#line  535 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
			} else if (la.kind == 114) {
				lexer.NextToken();
				Identifier();

#line  537 "VBNET.ATG" 
				delegateDeclr.Name = t.val; 
				TypeParameterList(
#line  538 "VBNET.ATG" 
delegateDeclr.Templates);
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  539 "VBNET.ATG" 
p);
					}
					Expect(26);

#line  539 "VBNET.ATG" 
					delegateDeclr.Parameters = p; 
				}
				if (la.kind == 50) {
					lexer.NextToken();

#line  540 "VBNET.ATG" 
					TypeReference type; 
					TypeName(
#line  540 "VBNET.ATG" 
out type);

#line  540 "VBNET.ATG" 
					delegateDeclr.ReturnType = type; 
				}
			} else SynErr(231);

#line  542 "VBNET.ATG" 
			delegateDeclr.EndLocation = t.EndLocation; 
			EndOfStmt();

#line  545 "VBNET.ATG" 
			compilationUnit.AddChild(delegateDeclr);
			
			break;
		}
		default: SynErr(232); break;
		}
	}

	void TypeParameterList(
#line  369 "VBNET.ATG" 
List<TemplateDefinition> templates) {

#line  371 "VBNET.ATG" 
		TemplateDefinition template;
		
		if (
#line  374 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			lexer.NextToken();
			Expect(155);
			TypeParameter(
#line  375 "VBNET.ATG" 
out template);

#line  377 "VBNET.ATG" 
			if (template != null) templates.Add(template);
			
			while (la.kind == 12) {
				lexer.NextToken();
				TypeParameter(
#line  380 "VBNET.ATG" 
out template);

#line  382 "VBNET.ATG" 
				if (template != null) templates.Add(template);
				
			}
			Expect(26);
		}
	}

	void TypeParameter(
#line  390 "VBNET.ATG" 
out TemplateDefinition template) {
		Identifier();

#line  392 "VBNET.ATG" 
		template = new TemplateDefinition(t.val, null); 
		if (la.kind == 50) {
			TypeParameterConstraints(
#line  393 "VBNET.ATG" 
template);
		}
	}

	void Identifier() {
		if (StartOf(5)) {
			IdentifierForFieldDeclaration();
		} else if (la.kind == 85) {
			lexer.NextToken();
		} else SynErr(233);
	}

	void TypeParameterConstraints(
#line  397 "VBNET.ATG" 
TemplateDefinition template) {

#line  399 "VBNET.ATG" 
		TypeReference constraint;
		
		Expect(50);
		if (la.kind == 23) {
			lexer.NextToken();
			TypeParameterConstraint(
#line  405 "VBNET.ATG" 
out constraint);

#line  405 "VBNET.ATG" 
			if (constraint != null) { template.Bases.Add(constraint); } 
			while (la.kind == 12) {
				lexer.NextToken();
				TypeParameterConstraint(
#line  408 "VBNET.ATG" 
out constraint);

#line  408 "VBNET.ATG" 
				if (constraint != null) { template.Bases.Add(constraint); } 
			}
			Expect(24);
		} else if (StartOf(6)) {
			TypeParameterConstraint(
#line  411 "VBNET.ATG" 
out constraint);

#line  411 "VBNET.ATG" 
			if (constraint != null) { template.Bases.Add(constraint); } 
		} else SynErr(234);
	}

	void TypeParameterConstraint(
#line  415 "VBNET.ATG" 
out TypeReference constraint) {

#line  416 "VBNET.ATG" 
		constraint = null; 
		if (la.kind == 71) {
			lexer.NextToken();

#line  417 "VBNET.ATG" 
			constraint = TypeReference.ClassConstraint; 
		} else if (la.kind == 194) {
			lexer.NextToken();

#line  418 "VBNET.ATG" 
			constraint = TypeReference.StructConstraint; 
		} else if (la.kind == 148) {
			lexer.NextToken();

#line  419 "VBNET.ATG" 
			constraint = TypeReference.NewConstraint; 
		} else if (StartOf(7)) {
			TypeName(
#line  420 "VBNET.ATG" 
out constraint);
		} else SynErr(235);
	}

	void ClassBaseType(
#line  765 "VBNET.ATG" 
out TypeReference typeRef) {

#line  767 "VBNET.ATG" 
		typeRef = null;
		
		Expect(127);
		TypeName(
#line  770 "VBNET.ATG" 
out typeRef);
		EndOfStmt();
	}

	void TypeImplementsClause(
#line  1563 "VBNET.ATG" 
out List<TypeReference> baseInterfaces) {

#line  1565 "VBNET.ATG" 
		baseInterfaces = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(123);
		TypeName(
#line  1568 "VBNET.ATG" 
out type);

#line  1570 "VBNET.ATG" 
		if (type != null) baseInterfaces.Add(type);
		
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  1573 "VBNET.ATG" 
out type);

#line  1574 "VBNET.ATG" 
			if (type != null) baseInterfaces.Add(type); 
		}
		EndOfStmt();
	}

	void ClassBody(
#line  559 "VBNET.ATG" 
TypeDeclaration newType) {

#line  560 "VBNET.ATG" 
		AttributeSection section; 
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (StartOf(8)) {

#line  563 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 28) {
				AttributeSection(
#line  566 "VBNET.ATG" 
out section);

#line  566 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(9)) {
				MemberModifier(
#line  567 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  568 "VBNET.ATG" 
m, attributes);
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
	}

	void ModuleBody(
#line  590 "VBNET.ATG" 
TypeDeclaration newType) {

#line  591 "VBNET.ATG" 
		AttributeSection section; 
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (StartOf(8)) {

#line  594 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 28) {
				AttributeSection(
#line  597 "VBNET.ATG" 
out section);

#line  597 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(9)) {
				MemberModifier(
#line  598 "VBNET.ATG" 
m);
			}
			ClassMemberDecl(
#line  599 "VBNET.ATG" 
m, attributes);
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		Expect(100);
		Expect(141);

#line  602 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void StructureBody(
#line  573 "VBNET.ATG" 
TypeDeclaration newType) {

#line  574 "VBNET.ATG" 
		AttributeSection section; 
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (StartOf(8)) {

#line  577 "VBNET.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 28) {
				AttributeSection(
#line  580 "VBNET.ATG" 
out section);

#line  580 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(9)) {
				MemberModifier(
#line  581 "VBNET.ATG" 
m);
			}
			StructureMemberDecl(
#line  582 "VBNET.ATG" 
m, attributes);
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		Expect(100);
		Expect(194);

#line  585 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void NonArrayTypeName(
#line  2420 "VBNET.ATG" 
out TypeReference typeref, bool canBeUnbound) {

#line  2422 "VBNET.ATG" 
		string name;
		typeref = null;
		bool isGlobal = false;
		
		if (StartOf(10)) {
			if (la.kind == 117) {
				lexer.NextToken();
				Expect(16);

#line  2427 "VBNET.ATG" 
				isGlobal = true; 
			}
			QualIdentAndTypeArguments(
#line  2428 "VBNET.ATG" 
out typeref, canBeUnbound);

#line  2429 "VBNET.ATG" 
			typeref.IsGlobal = isGlobal; 
			while (la.kind == 16) {
				lexer.NextToken();

#line  2430 "VBNET.ATG" 
				TypeReference nestedTypeRef; 
				QualIdentAndTypeArguments(
#line  2431 "VBNET.ATG" 
out nestedTypeRef, canBeUnbound);

#line  2432 "VBNET.ATG" 
				typeref = new InnerClassTypeReference(typeref, nestedTypeRef.Type, nestedTypeRef.GenericTypes); 
			}
		} else if (la.kind == 154) {
			lexer.NextToken();

#line  2435 "VBNET.ATG" 
			typeref = new TypeReference("System.Object", true); 
			if (la.kind == 21) {
				lexer.NextToken();

#line  2439 "VBNET.ATG" 
				List<TypeReference> typeArguments = new List<TypeReference>(1);
				if (typeref != null) typeArguments.Add(typeref);
				typeref = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };
				
			}
		} else if (StartOf(11)) {
			PrimitiveTypeName(
#line  2445 "VBNET.ATG" 
out name);

#line  2445 "VBNET.ATG" 
			typeref = new TypeReference(name, true); 
			if (la.kind == 21) {
				lexer.NextToken();

#line  2449 "VBNET.ATG" 
				List<TypeReference> typeArguments = new List<TypeReference>(1);
				if (typeref != null) typeArguments.Add(typeref);
				typeref = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };
				
			}
		} else SynErr(236);
	}

	void EnumBody(
#line  606 "VBNET.ATG" 
TypeDeclaration newType) {

#line  607 "VBNET.ATG" 
		FieldDeclaration f; 
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (StartOf(12)) {
			EnumMemberDecl(
#line  610 "VBNET.ATG" 
out f);

#line  612 "VBNET.ATG" 
			compilationUnit.AddChild(f);
			
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		Expect(100);
		Expect(102);

#line  616 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void InterfaceBase(
#line  1548 "VBNET.ATG" 
out List<TypeReference> bases) {

#line  1550 "VBNET.ATG" 
		TypeReference type;
		bases = new List<TypeReference>();
		
		Expect(127);
		TypeName(
#line  1554 "VBNET.ATG" 
out type);

#line  1554 "VBNET.ATG" 
		if (type != null) bases.Add(type); 
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  1557 "VBNET.ATG" 
out type);

#line  1557 "VBNET.ATG" 
			if (type != null) bases.Add(type); 
		}
		EndOfStmt();
	}

	void InterfaceBody(
#line  620 "VBNET.ATG" 
TypeDeclaration newType) {
		while (la.kind == 1 || la.kind == 11) {
			EndOfStmt();
		}
		while (StartOf(13)) {
			InterfaceMemberDecl();
			while (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
			}
		}
		Expect(100);
		Expect(129);

#line  626 "VBNET.ATG" 
		newType.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void FormalParameterList(
#line  2633 "VBNET.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  2634 "VBNET.ATG" 
		ParameterDeclarationExpression p; 
		FormalParameter(
#line  2636 "VBNET.ATG" 
out p);

#line  2636 "VBNET.ATG" 
		if (p != null) parameter.Add(p); 
		while (la.kind == 12) {
			lexer.NextToken();
			FormalParameter(
#line  2638 "VBNET.ATG" 
out p);

#line  2638 "VBNET.ATG" 
			if (p != null) parameter.Add(p); 
		}
	}

	void MemberModifier(
#line  3374 "VBNET.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 142: {
			lexer.NextToken();

#line  3375 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 89: {
			lexer.NextToken();

#line  3376 "VBNET.ATG" 
			m.Add(Modifiers.Default, t.Location);
			break;
		}
		case 112: {
			lexer.NextToken();

#line  3377 "VBNET.ATG" 
			m.Add(Modifiers.Internal, t.Location);
			break;
		}
		case 184: {
			lexer.NextToken();

#line  3378 "VBNET.ATG" 
			m.Add(Modifiers.New, t.Location);
			break;
		}
		case 166: {
			lexer.NextToken();

#line  3379 "VBNET.ATG" 
			m.Add(Modifiers.Override, t.Location);
			break;
		}
		case 143: {
			lexer.NextToken();

#line  3380 "VBNET.ATG" 
			m.Add(Modifiers.Abstract, t.Location);
			break;
		}
		case 170: {
			lexer.NextToken();

#line  3381 "VBNET.ATG" 
			m.Add(Modifiers.Private, t.Location);
			break;
		}
		case 172: {
			lexer.NextToken();

#line  3382 "VBNET.ATG" 
			m.Add(Modifiers.Protected, t.Location);
			break;
		}
		case 173: {
			lexer.NextToken();

#line  3383 "VBNET.ATG" 
			m.Add(Modifiers.Public, t.Location);
			break;
		}
		case 152: {
			lexer.NextToken();

#line  3384 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 153: {
			lexer.NextToken();

#line  3385 "VBNET.ATG" 
			m.Add(Modifiers.Sealed, t.Location);
			break;
		}
		case 185: {
			lexer.NextToken();

#line  3386 "VBNET.ATG" 
			m.Add(Modifiers.Static, t.Location);
			break;
		}
		case 165: {
			lexer.NextToken();

#line  3387 "VBNET.ATG" 
			m.Add(Modifiers.Virtual, t.Location);
			break;
		}
		case 164: {
			lexer.NextToken();

#line  3388 "VBNET.ATG" 
			m.Add(Modifiers.Overloads, t.Location);
			break;
		}
		case 175: {
			lexer.NextToken();

#line  3389 "VBNET.ATG" 
			m.Add(Modifiers.ReadOnly, t.Location);
			break;
		}
		case 220: {
			lexer.NextToken();

#line  3390 "VBNET.ATG" 
			m.Add(Modifiers.WriteOnly, t.Location);
			break;
		}
		case 219: {
			lexer.NextToken();

#line  3391 "VBNET.ATG" 
			m.Add(Modifiers.WithEvents, t.Location);
			break;
		}
		case 92: {
			lexer.NextToken();

#line  3392 "VBNET.ATG" 
			m.Add(Modifiers.Dim, t.Location);
			break;
		}
		case 168: {
			lexer.NextToken();

#line  3393 "VBNET.ATG" 
			m.Add(Modifiers.Partial, t.Location);
			break;
		}
		default: SynErr(237); break;
		}
	}

	void ClassMemberDecl(
#line  761 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {
		StructureMemberDecl(
#line  762 "VBNET.ATG" 
m, attributes);
	}

	void StructureMemberDecl(
#line  775 "VBNET.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  777 "VBNET.ATG" 
		TypeReference type = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<VariableDeclaration> variableDeclarators = new List<VariableDeclaration>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		
		switch (la.kind) {
		case 71: case 90: case 102: case 129: case 141: case 194: {
			NonModuleDeclaration(
#line  784 "VBNET.ATG" 
m, attributes);
			break;
		}
		case 195: {
			lexer.NextToken();

#line  788 "VBNET.ATG" 
			Location startPos = t.Location;
			
			if (StartOf(14)) {

#line  792 "VBNET.ATG" 
				string name = String.Empty;
				MethodDeclaration methodDeclaration; List<string> handlesClause = null;
				List<InterfaceImplementation> implementsClause = null;
				
				Identifier();

#line  798 "VBNET.ATG" 
				name = t.val;
				m.Check(Modifiers.VBMethods);
				
				TypeParameterList(
#line  801 "VBNET.ATG" 
templates);
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  802 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				if (la.kind == 121 || la.kind == 123) {
					if (la.kind == 123) {
						ImplementsClause(
#line  805 "VBNET.ATG" 
out implementsClause);
					} else {
						HandlesClause(
#line  807 "VBNET.ATG" 
out handlesClause);
					}
				}

#line  810 "VBNET.ATG" 
				Location endLocation = t.EndLocation; 
				if (
#line  813 "VBNET.ATG" 
IsMustOverride(m)) {
					EndOfStmt();

#line  816 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration {
					Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
					StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
					TypeReference = new TypeReference("System.Void", true),
					Templates = templates,
					HandlesClause = handlesClause,
					InterfaceImplementations = implementsClause
					};
					compilationUnit.AddChild(methodDeclaration);
					
				} else if (la.kind == 1) {
					lexer.NextToken();

#line  829 "VBNET.ATG" 
					methodDeclaration = new MethodDeclaration {
					Name = name, Modifier = m.Modifier, Parameters = p, Attributes = attributes,
					StartLocation = m.GetDeclarationLocation(startPos), EndLocation = endLocation,
					TypeReference = new TypeReference("System.Void", true),
					Templates = templates,
					HandlesClause = handlesClause,
					InterfaceImplementations = implementsClause
					};
					compilationUnit.AddChild(methodDeclaration);
					

#line  840 "VBNET.ATG" 
					if (ParseMethodBodies) { 
					Block(
#line  841 "VBNET.ATG" 
out stmt);
					Expect(100);
					Expect(195);

#line  843 "VBNET.ATG" 
					} else {
					// don't parse method body
					lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
					  }
					

#line  849 "VBNET.ATG" 
					methodDeclaration.Body  = (BlockStatement)stmt; 

#line  850 "VBNET.ATG" 
					methodDeclaration.Body.EndLocation = t.EndLocation; 
					EndOfStmt();
				} else SynErr(238);
			} else if (la.kind == 148) {
				lexer.NextToken();
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  854 "VBNET.ATG" 
p);
					}
					Expect(26);
				}

#line  855 "VBNET.ATG" 
				m.Check(Modifiers.Constructors); 

#line  856 "VBNET.ATG" 
				Location constructorEndLocation = t.EndLocation; 
				Expect(1);

#line  859 "VBNET.ATG" 
				if (ParseMethodBodies) { 
				Block(
#line  860 "VBNET.ATG" 
out stmt);
				Expect(100);
				Expect(195);

#line  862 "VBNET.ATG" 
				} else {
				// don't parse method body
				lexer.SkipCurrentBlock(Tokens.Sub); stmt = new BlockStatement();
				  }
				

#line  868 "VBNET.ATG" 
				Location endLocation = t.EndLocation; 
				EndOfStmt();

#line  871 "VBNET.ATG" 
				ConstructorDeclaration cd = new ConstructorDeclaration("New", m.Modifier, p, attributes);
				cd.StartLocation = m.GetDeclarationLocation(startPos);
				cd.EndLocation   = constructorEndLocation;
				cd.Body = (BlockStatement)stmt;
				cd.Body.EndLocation   = endLocation;
				compilationUnit.AddChild(cd);
				
			} else SynErr(239);
			break;
		}
		case 114: {
			lexer.NextToken();

#line  883 "VBNET.ATG" 
			m.Check(Modifiers.VBMethods);
			string name = String.Empty;
			Location startPos = t.Location;
			MethodDeclaration methodDeclaration;List<string> handlesClause = null;
			List<InterfaceImplementation> implementsClause = null;
			AttributeSection returnTypeAttributeSection = null;
			
			Identifier();

#line  890 "VBNET.ATG" 
			name = t.val; 
			TypeParameterList(
#line  891 "VBNET.ATG" 
templates);
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  892 "VBNET.ATG" 
p);
				}
				Expect(26);
			}
			if (la.kind == 50) {
				lexer.NextToken();
				while (la.kind == 28) {
					AttributeSection(
#line  893 "VBNET.ATG" 
out returnTypeAttributeSection);
				}
				TypeName(
#line  893 "VBNET.ATG" 
out type);
			}

#line  895 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object", true);
			}
			
			if (la.kind == 121 || la.kind == 123) {
				if (la.kind == 123) {
					ImplementsClause(
#line  901 "VBNET.ATG" 
out implementsClause);
				} else {
					HandlesClause(
#line  903 "VBNET.ATG" 
out handlesClause);
				}
			}
			if (
#line  908 "VBNET.ATG" 
IsMustOverride(m)) {
				EndOfStmt();

#line  911 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration {
				Name = name, Modifier = m.Modifier, TypeReference = type,
				Parameters = p, Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation   = t.EndLocation,
				HandlesClause = handlesClause,
				Templates     = templates,
				InterfaceImplementations = implementsClause
				};
				if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					methodDeclaration.Attributes.Add(returnTypeAttributeSection);
				}
				compilationUnit.AddChild(methodDeclaration);
				
			} else if (la.kind == 1) {
				lexer.NextToken();

#line  929 "VBNET.ATG" 
				methodDeclaration = new MethodDeclaration {
				Name = name, Modifier = m.Modifier, TypeReference = type,
				Parameters = p, Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation   = t.EndLocation,
				Templates     = templates,
				HandlesClause = handlesClause,
				InterfaceImplementations = implementsClause
				};
				if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					methodDeclaration.Attributes.Add(returnTypeAttributeSection);
				}
				
				compilationUnit.AddChild(methodDeclaration);
				
				if (ParseMethodBodies) { 
				Block(
#line  946 "VBNET.ATG" 
out stmt);
				Expect(100);
				Expect(114);

#line  948 "VBNET.ATG" 
				} else {
				// don't parse method body
				lexer.SkipCurrentBlock(Tokens.Function); stmt = new BlockStatement();
				}
				methodDeclaration.Body = (BlockStatement)stmt;
				methodDeclaration.Body.StartLocation = methodDeclaration.EndLocation;
				methodDeclaration.Body.EndLocation   = t.EndLocation;
				
				EndOfStmt();
			} else SynErr(240);
			break;
		}
		case 88: {
			lexer.NextToken();

#line  962 "VBNET.ATG" 
			m.Check(Modifiers.VBExternalMethods);
			Location startPos = t.Location;
			CharsetModifier charsetModifer = CharsetModifier.None;
			string library = String.Empty;
			string alias = null;
			string name = String.Empty;
			
			if (StartOf(15)) {
				Charset(
#line  969 "VBNET.ATG" 
out charsetModifer);
			}
			if (la.kind == 195) {
				lexer.NextToken();
				Identifier();

#line  972 "VBNET.ATG" 
				name = t.val; 
				Expect(135);
				Expect(3);

#line  973 "VBNET.ATG" 
				library = t.literalValue as string; 
				if (la.kind == 46) {
					lexer.NextToken();
					Expect(3);

#line  974 "VBNET.ATG" 
					alias = t.literalValue as string; 
				}
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  975 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				EndOfStmt();

#line  978 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, null, p, attributes, library, alias, charsetModifer);
				declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				declareDeclaration.EndLocation   = t.EndLocation;
				compilationUnit.AddChild(declareDeclaration);
				
			} else if (la.kind == 114) {
				lexer.NextToken();
				Identifier();

#line  985 "VBNET.ATG" 
				name = t.val; 
				Expect(135);
				Expect(3);

#line  986 "VBNET.ATG" 
				library = t.literalValue as string; 
				if (la.kind == 46) {
					lexer.NextToken();
					Expect(3);

#line  987 "VBNET.ATG" 
					alias = t.literalValue as string; 
				}
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  988 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				if (la.kind == 50) {
					lexer.NextToken();
					TypeName(
#line  989 "VBNET.ATG" 
out type);
				}
				EndOfStmt();

#line  992 "VBNET.ATG" 
				DeclareDeclaration declareDeclaration = new DeclareDeclaration(name, m.Modifier, type, p, attributes, library, alias, charsetModifer);
				declareDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
				declareDeclaration.EndLocation   = t.EndLocation;
				compilationUnit.AddChild(declareDeclaration);
				
			} else SynErr(241);
			break;
		}
		case 106: {
			lexer.NextToken();

#line  1002 "VBNET.ATG" 
			m.Check(Modifiers.VBEvents);
			Location startPos = t.Location;
			EventDeclaration eventDeclaration;
			string name = String.Empty;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1008 "VBNET.ATG" 
			name= t.val; 
			if (la.kind == 50) {
				lexer.NextToken();
				TypeName(
#line  1010 "VBNET.ATG" 
out type);
			} else if (StartOf(16)) {
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  1012 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
			} else SynErr(242);
			if (la.kind == 123) {
				ImplementsClause(
#line  1014 "VBNET.ATG" 
out implementsClause);
			}

#line  1016 "VBNET.ATG" 
			eventDeclaration = new EventDeclaration {
			Name = name, TypeReference = type, Modifier = m.Modifier, 
			Parameters = p, Attributes = attributes, InterfaceImplementations = implementsClause,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = t.EndLocation
			};
			compilationUnit.AddChild(eventDeclaration);
			
			EndOfStmt();
			break;
		}
		case 2: case 45: case 49: case 51: case 52: case 53: case 54: case 57: case 74: case 91: case 94: case 103: case 108: case 113: case 120: case 126: case 130: case 133: case 156: case 162: case 169: case 188: case 197: case 198: case 208: case 209: case 215: {

#line  1026 "VBNET.ATG" 
			Location startPos = t.Location; 

#line  1028 "VBNET.ATG" 
			m.Check(Modifiers.Fields);
			FieldDeclaration fd = new FieldDeclaration(attributes, null, m.Modifier);
			fd.StartLocation = m.GetDeclarationLocation(startPos); 
			
			IdentifierForFieldDeclaration();

#line  1032 "VBNET.ATG" 
			string name = t.val; 
			VariableDeclaratorPartAfterIdentifier(
#line  1033 "VBNET.ATG" 
variableDeclarators, name);
			while (la.kind == 12) {
				lexer.NextToken();
				VariableDeclarator(
#line  1034 "VBNET.ATG" 
variableDeclarators);
			}
			EndOfStmt();

#line  1037 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			fd.Fields = variableDeclarators;
			compilationUnit.AddChild(fd);
			
			break;
		}
		case 75: {

#line  1042 "VBNET.ATG" 
			m.Check(Modifiers.Fields); 
			lexer.NextToken();

#line  1043 "VBNET.ATG" 
			m.Add(Modifiers.Const, t.Location);  

#line  1045 "VBNET.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
			fd.StartLocation = m.GetDeclarationLocation(t.Location);
			List<VariableDeclaration> constantDeclarators = new List<VariableDeclaration>();
			
			ConstantDeclarator(
#line  1049 "VBNET.ATG" 
constantDeclarators);
			while (la.kind == 12) {
				lexer.NextToken();
				ConstantDeclarator(
#line  1050 "VBNET.ATG" 
constantDeclarators);
			}

#line  1052 "VBNET.ATG" 
			fd.Fields = constantDeclarators;
			fd.EndLocation = t.Location;
			
			EndOfStmt();

#line  1057 "VBNET.ATG" 
			fd.EndLocation = t.EndLocation;
			compilationUnit.AddChild(fd);
			
			break;
		}
		case 171: {
			lexer.NextToken();

#line  1063 "VBNET.ATG" 
			m.Check(Modifiers.VBProperties);
			Location startPos = t.Location;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1067 "VBNET.ATG" 
			string propertyName = t.val; 
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1068 "VBNET.ATG" 
p);
				}
				Expect(26);
			}
			if (la.kind == 50) {
				lexer.NextToken();
				TypeName(
#line  1069 "VBNET.ATG" 
out type);
			}

#line  1071 "VBNET.ATG" 
			if(type == null) {
			type = new TypeReference("System.Object", true);
			}
			
			if (la.kind == 123) {
				ImplementsClause(
#line  1075 "VBNET.ATG" 
out implementsClause);
			}
			EndOfStmt();
			if (
#line  1079 "VBNET.ATG" 
IsMustOverride(m)) {

#line  1081 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.StartLocation = m.GetDeclarationLocation(startPos);
				pDecl.EndLocation   = t.Location;
				pDecl.TypeReference = type;
				pDecl.InterfaceImplementations = implementsClause;
				pDecl.Parameters = p;
				compilationUnit.AddChild(pDecl);
				
			} else if (StartOf(17)) {

#line  1091 "VBNET.ATG" 
				PropertyDeclaration pDecl = new PropertyDeclaration(propertyName, type, m.Modifier, attributes);
				pDecl.StartLocation = m.GetDeclarationLocation(startPos);
				pDecl.EndLocation   = t.Location;
				pDecl.BodyStart   = t.Location;
				pDecl.TypeReference = type;
				pDecl.InterfaceImplementations = implementsClause;
				pDecl.Parameters = p;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  1101 "VBNET.ATG" 
out getRegion, out setRegion);
				Expect(100);
				Expect(171);
				EndOfStmt();

#line  1105 "VBNET.ATG" 
				pDecl.GetRegion = getRegion;
				pDecl.SetRegion = setRegion;
				pDecl.BodyEnd = t.EndLocation;
				compilationUnit.AddChild(pDecl);
				
			} else SynErr(243);
			break;
		}
		case 85: {
			lexer.NextToken();

#line  1112 "VBNET.ATG" 
			Location startPos = t.Location; 
			Expect(106);

#line  1114 "VBNET.ATG" 
			m.Check(Modifiers.VBCustomEvents);
			EventAddRemoveRegion eventAccessorDeclaration;
			EventAddRegion addHandlerAccessorDeclaration = null;
			EventRemoveRegion removeHandlerAccessorDeclaration = null;
			EventRaiseRegion raiseEventAccessorDeclaration = null;
			List<InterfaceImplementation> implementsClause = null;
			
			Identifier();

#line  1121 "VBNET.ATG" 
			string customEventName = t.val; 
			Expect(50);
			TypeName(
#line  1122 "VBNET.ATG" 
out type);
			if (la.kind == 123) {
				ImplementsClause(
#line  1123 "VBNET.ATG" 
out implementsClause);
			}
			EndOfStmt();
			while (StartOf(18)) {
				EventAccessorDeclaration(
#line  1126 "VBNET.ATG" 
out eventAccessorDeclaration);

#line  1128 "VBNET.ATG" 
				if(eventAccessorDeclaration is EventAddRegion)
				{
					addHandlerAccessorDeclaration = (EventAddRegion)eventAccessorDeclaration;
				}
				else if(eventAccessorDeclaration is EventRemoveRegion)
				{
					removeHandlerAccessorDeclaration = (EventRemoveRegion)eventAccessorDeclaration;
				}
				else if(eventAccessorDeclaration is EventRaiseRegion)
				{
					raiseEventAccessorDeclaration = (EventRaiseRegion)eventAccessorDeclaration;
				}
				
			}
			Expect(100);
			Expect(106);
			EndOfStmt();

#line  1144 "VBNET.ATG" 
			if(addHandlerAccessorDeclaration == null)
			{
				Error("Need to provide AddHandler accessor.");
			}
			
			if(removeHandlerAccessorDeclaration == null)
			{
				Error("Need to provide RemoveHandler accessor.");
			}
			
			if(raiseEventAccessorDeclaration == null)
			{
				Error("Need to provide RaiseEvent accessor.");
			}
			
			EventDeclaration decl = new EventDeclaration {
				TypeReference = type, Name = customEventName, Modifier = m.Modifier,
				Attributes = attributes,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation = t.EndLocation,
				AddRegion = addHandlerAccessorDeclaration,
				RemoveRegion = removeHandlerAccessorDeclaration,
				RaiseRegion = raiseEventAccessorDeclaration
			};
			compilationUnit.AddChild(decl);
			
			break;
		}
		case 147: case 158: case 217: {

#line  1170 "VBNET.ATG" 
			ConversionType opConversionType = ConversionType.None; 
			if (la.kind == 147 || la.kind == 217) {
				if (la.kind == 217) {
					lexer.NextToken();

#line  1171 "VBNET.ATG" 
					opConversionType = ConversionType.Implicit; 
				} else {
					lexer.NextToken();

#line  1172 "VBNET.ATG" 
					opConversionType = ConversionType.Explicit;
				}
			}
			Expect(158);

#line  1175 "VBNET.ATG" 
			m.Check(Modifiers.VBOperators);
			Location startPos = t.Location;
			TypeReference returnType = NullTypeReference.Instance;
			TypeReference operandType = NullTypeReference.Instance;
			string operandName;
			OverloadableOperatorType operatorType;
			AttributeSection section;
			List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
			List<AttributeSection> returnTypeAttributes = new List<AttributeSection>();
			
			OverloadableOperator(
#line  1185 "VBNET.ATG" 
out operatorType);
			Expect(25);
			if (la.kind == 59) {
				lexer.NextToken();
			}
			Identifier();

#line  1186 "VBNET.ATG" 
			operandName = t.val; 
			if (la.kind == 50) {
				lexer.NextToken();
				TypeName(
#line  1187 "VBNET.ATG" 
out operandType);
			}

#line  1188 "VBNET.ATG" 
			parameters.Add(new ParameterDeclarationExpression(operandType, operandName, ParameterModifiers.In)); 
			while (la.kind == 12) {
				lexer.NextToken();
				if (la.kind == 59) {
					lexer.NextToken();
				}
				Identifier();

#line  1192 "VBNET.ATG" 
				operandName = t.val; 
				if (la.kind == 50) {
					lexer.NextToken();
					TypeName(
#line  1193 "VBNET.ATG" 
out operandType);
				}

#line  1194 "VBNET.ATG" 
				parameters.Add(new ParameterDeclarationExpression(operandType, operandName, ParameterModifiers.In)); 
			}
			Expect(26);

#line  1197 "VBNET.ATG" 
			Location endPos = t.EndLocation; 
			if (la.kind == 50) {
				lexer.NextToken();
				while (la.kind == 28) {
					AttributeSection(
#line  1198 "VBNET.ATG" 
out section);

#line  1198 "VBNET.ATG" 
					returnTypeAttributes.Add(section); 
				}
				TypeName(
#line  1198 "VBNET.ATG" 
out returnType);

#line  1198 "VBNET.ATG" 
				endPos = t.EndLocation; 
			}
			Expect(1);
			Block(
#line  1200 "VBNET.ATG" 
out stmt);
			Expect(100);
			Expect(158);
			EndOfStmt();

#line  1202 "VBNET.ATG" 
			OperatorDeclaration operatorDeclaration = new OperatorDeclaration {
			Modifier = m.Modifier,
			Attributes = attributes,
			Parameters = parameters,
			TypeReference = returnType,
			OverloadableOperator = operatorType,
			ConversionType = opConversionType,
			ReturnTypeAttributes = returnTypeAttributes,
			Body = (BlockStatement)stmt,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = endPos
			};
			operatorDeclaration.Body.StartLocation = startPos;
			operatorDeclaration.Body.EndLocation = t.Location;
			compilationUnit.AddChild(operatorDeclaration);
			
			break;
		}
		default: SynErr(244); break;
		}
	}

	void EnumMemberDecl(
#line  743 "VBNET.ATG" 
out FieldDeclaration f) {

#line  745 "VBNET.ATG" 
		Expression expr = null;List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 28) {
			AttributeSection(
#line  749 "VBNET.ATG" 
out section);

#line  749 "VBNET.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  752 "VBNET.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = varDecl.StartLocation = t.Location;
		
		if (la.kind == 10) {
			lexer.NextToken();
			Expr(
#line  757 "VBNET.ATG" 
out expr);

#line  757 "VBNET.ATG" 
			varDecl.Initializer = expr; 
		}
		EndOfStmt();
	}

	void InterfaceMemberDecl() {

#line  634 "VBNET.ATG" 
		TypeReference type =null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		AttributeSection section, returnTypeAttributeSection = null;
		ModifierList mod = new ModifierList();
		List<AttributeSection> attributes = new List<AttributeSection>();
		string name;
		
		if (StartOf(19)) {
			while (la.kind == 28) {
				AttributeSection(
#line  642 "VBNET.ATG" 
out section);

#line  642 "VBNET.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(9)) {
				MemberModifier(
#line  645 "VBNET.ATG" 
mod);
			}
			if (la.kind == 106) {
				lexer.NextToken();

#line  649 "VBNET.ATG" 
				mod.Check(Modifiers.VBInterfaceEvents);
				Location startLocation = t.Location;
				
				Identifier();

#line  652 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  653 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				if (la.kind == 50) {
					lexer.NextToken();
					TypeName(
#line  654 "VBNET.ATG" 
out type);
				}
				EndOfStmt();

#line  657 "VBNET.ATG" 
				EventDeclaration ed = new EventDeclaration {
				Name = name, TypeReference = type, Modifier = mod.Modifier,
				Parameters = p, Attributes = attributes,
				StartLocation = startLocation, EndLocation = t.EndLocation
				};
				compilationUnit.AddChild(ed);
				
			} else if (la.kind == 195) {
				lexer.NextToken();

#line  667 "VBNET.ATG" 
				Location startLocation =  t.Location;
				mod.Check(Modifiers.VBInterfaceMethods);
				
				Identifier();

#line  670 "VBNET.ATG" 
				name = t.val; 
				TypeParameterList(
#line  671 "VBNET.ATG" 
templates);
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  672 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				EndOfStmt();

#line  675 "VBNET.ATG" 
				MethodDeclaration md = new MethodDeclaration {
				Name = name, 
				Modifier = mod.Modifier, 
				Parameters = p,
				Attributes = attributes,
				TypeReference = new TypeReference("System.Void", true),
				StartLocation = startLocation,
				EndLocation = t.EndLocation,
				Templates = templates
				};
				compilationUnit.AddChild(md);
				
			} else if (la.kind == 114) {
				lexer.NextToken();

#line  690 "VBNET.ATG" 
				mod.Check(Modifiers.VBInterfaceMethods);
				Location startLocation = t.Location;
				
				Identifier();

#line  693 "VBNET.ATG" 
				name = t.val; 
				TypeParameterList(
#line  694 "VBNET.ATG" 
templates);
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  695 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				if (la.kind == 50) {
					lexer.NextToken();
					while (la.kind == 28) {
						AttributeSection(
#line  696 "VBNET.ATG" 
out returnTypeAttributeSection);
					}
					TypeName(
#line  696 "VBNET.ATG" 
out type);
				}

#line  698 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object", true);
				}
				MethodDeclaration md = new MethodDeclaration {
					Name = name, Modifier = mod.Modifier, 
					TypeReference = type, Parameters = p, Attributes = attributes
				};
				if (returnTypeAttributeSection != null) {
					returnTypeAttributeSection.AttributeTarget = "return";
					md.Attributes.Add(returnTypeAttributeSection);
				}
				md.StartLocation = startLocation;
				md.EndLocation = t.EndLocation;
				md.Templates = templates;
				compilationUnit.AddChild(md);
				
				EndOfStmt();
			} else if (la.kind == 171) {
				lexer.NextToken();

#line  718 "VBNET.ATG" 
				Location startLocation = t.Location;
				mod.Check(Modifiers.VBInterfaceProperties);
				
				Identifier();

#line  721 "VBNET.ATG" 
				name = t.val;  
				if (la.kind == 25) {
					lexer.NextToken();
					if (StartOf(4)) {
						FormalParameterList(
#line  722 "VBNET.ATG" 
p);
					}
					Expect(26);
				}
				if (la.kind == 50) {
					lexer.NextToken();
					TypeName(
#line  723 "VBNET.ATG" 
out type);
				}

#line  725 "VBNET.ATG" 
				if(type == null) {
				type = new TypeReference("System.Object", true);
				}
				
				EndOfStmt();

#line  731 "VBNET.ATG" 
				PropertyDeclaration pd = new PropertyDeclaration(name, type, mod.Modifier, attributes);
				pd.Parameters = p;
				pd.EndLocation = t.EndLocation;
				pd.StartLocation = startLocation;
				compilationUnit.AddChild(pd);
				
			} else SynErr(245);
		} else if (StartOf(20)) {
			NonModuleDeclaration(
#line  739 "VBNET.ATG" 
mod, attributes);
		} else SynErr(246);
	}

	void Expr(
#line  1607 "VBNET.ATG" 
out Expression expr) {

#line  1608 "VBNET.ATG" 
		expr = null; 
		if (
#line  1609 "VBNET.ATG" 
IsQueryExpression() ) {
			QueryExpr(
#line  1610 "VBNET.ATG" 
out expr);
		} else if (la.kind == 114) {
			LambdaExpr(
#line  1611 "VBNET.ATG" 
out expr);
		} else if (StartOf(21)) {
			DisjunctionExpr(
#line  1612 "VBNET.ATG" 
out expr);
		} else SynErr(247);
	}

	void ImplementsClause(
#line  1580 "VBNET.ATG" 
out List<InterfaceImplementation> baseInterfaces) {

#line  1582 "VBNET.ATG" 
		baseInterfaces = new List<InterfaceImplementation>();
		TypeReference type = null;
		string memberName = null;
		
		Expect(123);
		NonArrayTypeName(
#line  1587 "VBNET.ATG" 
out type, false);

#line  1588 "VBNET.ATG" 
		if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type); 

#line  1589 "VBNET.ATG" 
		baseInterfaces.Add(new InterfaceImplementation(type, memberName)); 
		while (la.kind == 12) {
			lexer.NextToken();
			NonArrayTypeName(
#line  1591 "VBNET.ATG" 
out type, false);

#line  1592 "VBNET.ATG" 
			if (type != null) memberName = TypeReference.StripLastIdentifierFromType(ref type); 

#line  1593 "VBNET.ATG" 
			baseInterfaces.Add(new InterfaceImplementation(type, memberName)); 
		}
	}

	void HandlesClause(
#line  1538 "VBNET.ATG" 
out List<string> handlesClause) {

#line  1540 "VBNET.ATG" 
		handlesClause = new List<string>();
		string name;
		
		Expect(121);
		EventMemberSpecifier(
#line  1543 "VBNET.ATG" 
out name);

#line  1543 "VBNET.ATG" 
		if (name != null) handlesClause.Add(name); 
		while (la.kind == 12) {
			lexer.NextToken();
			EventMemberSpecifier(
#line  1544 "VBNET.ATG" 
out name);

#line  1544 "VBNET.ATG" 
			if (name != null) handlesClause.Add(name); 
		}
	}

	void Block(
#line  2680 "VBNET.ATG" 
out Statement stmt) {

#line  2683 "VBNET.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		/* in snippet parsing mode, t might be null */
		if (t != null) blockStmt.StartLocation = t.EndLocation;
		compilationUnit.BlockStart(blockStmt);
		
		while (StartOf(22) || 
#line  2689 "VBNET.ATG" 
IsEndStmtAhead()) {
			if (
#line  2689 "VBNET.ATG" 
IsEndStmtAhead()) {
				Expect(100);
				EndOfStmt();

#line  2689 "VBNET.ATG" 
				compilationUnit.AddChild(new EndStatement()); 
			} else {
				Statement();
				EndOfStmt();
			}
		}

#line  2694 "VBNET.ATG" 
		stmt = blockStmt;
		if (t != null) blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void Charset(
#line  1530 "VBNET.ATG" 
out CharsetModifier charsetModifier) {

#line  1531 "VBNET.ATG" 
		charsetModifier = CharsetModifier.None; 
		if (la.kind == 114 || la.kind == 195) {
		} else if (la.kind == 49) {
			lexer.NextToken();

#line  1532 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Ansi; 
		} else if (la.kind == 53) {
			lexer.NextToken();

#line  1533 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Auto; 
		} else if (la.kind == 208) {
			lexer.NextToken();

#line  1534 "VBNET.ATG" 
			charsetModifier = CharsetModifier.Unicode; 
		} else SynErr(248);
	}

	void IdentifierForFieldDeclaration() {
		switch (la.kind) {
		case 2: {
			lexer.NextToken();
			break;
		}
		case 45: {
			lexer.NextToken();
			break;
		}
		case 49: {
			lexer.NextToken();
			break;
		}
		case 51: {
			lexer.NextToken();
			break;
		}
		case 52: {
			lexer.NextToken();
			break;
		}
		case 53: {
			lexer.NextToken();
			break;
		}
		case 54: {
			lexer.NextToken();
			break;
		}
		case 57: {
			lexer.NextToken();
			break;
		}
		case 74: {
			lexer.NextToken();
			break;
		}
		case 91: {
			lexer.NextToken();
			break;
		}
		case 94: {
			lexer.NextToken();
			break;
		}
		case 103: {
			lexer.NextToken();
			break;
		}
		case 108: {
			lexer.NextToken();
			break;
		}
		case 113: {
			lexer.NextToken();
			break;
		}
		case 120: {
			lexer.NextToken();
			break;
		}
		case 126: {
			lexer.NextToken();
			break;
		}
		case 130: {
			lexer.NextToken();
			break;
		}
		case 133: {
			lexer.NextToken();
			break;
		}
		case 156: {
			lexer.NextToken();
			break;
		}
		case 162: {
			lexer.NextToken();
			break;
		}
		case 169: {
			lexer.NextToken();
			break;
		}
		case 188: {
			lexer.NextToken();
			break;
		}
		case 197: {
			lexer.NextToken();
			break;
		}
		case 198: {
			lexer.NextToken();
			break;
		}
		case 208: {
			lexer.NextToken();
			break;
		}
		case 209: {
			lexer.NextToken();
			break;
		}
		case 215: {
			lexer.NextToken();
			break;
		}
		default: SynErr(249); break;
		}
	}

	void VariableDeclaratorPartAfterIdentifier(
#line  1406 "VBNET.ATG" 
List<VariableDeclaration> fieldDeclaration, string name) {

#line  1408 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		ArrayList rank = null;
		List<Expression> dimension = null;
		Location startLocation = t.Location;
		
		if (
#line  1414 "VBNET.ATG" 
IsSize() && !IsDims()) {
			ArrayInitializationModifier(
#line  1414 "VBNET.ATG" 
out dimension);
		}
		if (
#line  1415 "VBNET.ATG" 
IsDims()) {
			ArrayNameModifier(
#line  1415 "VBNET.ATG" 
out rank);
		}
		if (
#line  1417 "VBNET.ATG" 
IsObjectCreation()) {
			Expect(50);
			ObjectCreateExpression(
#line  1417 "VBNET.ATG" 
out expr);

#line  1419 "VBNET.ATG" 
			if (expr is ObjectCreateExpression) {
			type = ((ObjectCreateExpression)expr).CreateType.Clone();
			} else {
				type = ((ArrayCreateExpression)expr).CreateType.Clone();
			}
			
		} else if (StartOf(23)) {
			if (la.kind == 50) {
				lexer.NextToken();
				TypeName(
#line  1426 "VBNET.ATG" 
out type);

#line  1428 "VBNET.ATG" 
				if (type != null) {
				for (int i = fieldDeclaration.Count - 1; i >= 0; i--) {
					VariableDeclaration vd = fieldDeclaration[i];
					if (vd.TypeReference.Type.Length > 0) break;
					TypeReference newType = type.Clone();
					newType.RankSpecifier = vd.TypeReference.RankSpecifier;
					vd.TypeReference = newType;
				}
				}
				 
			}

#line  1440 "VBNET.ATG" 
			if (type == null && (dimension != null || rank != null)) {
			type = new TypeReference("");
			}
			if (dimension != null) {
				if(type.RankSpecifier != null) {
					Error("array rank only allowed one time");
				} else {
					if (rank == null) {
						type.RankSpecifier = new int[] { dimension.Count - 1 };
					} else {
						rank.Insert(0, dimension.Count - 1);
						type.RankSpecifier = (int[])rank.ToArray(typeof(int));
					}
					expr = new ArrayCreateExpression(type.Clone(), dimension);
				}
			} else if (rank != null) {
				if(type.RankSpecifier != null) {
					Error("array rank only allowed one time");
				} else {
					type.RankSpecifier = (int[])rank.ToArray(typeof(int));
				}
			}
			
			if (la.kind == 10) {
				lexer.NextToken();
				VariableInitializer(
#line  1463 "VBNET.ATG" 
out expr);
			}
		} else SynErr(250);

#line  1466 "VBNET.ATG" 
		VariableDeclaration varDecl = new VariableDeclaration(name, expr, type);
		varDecl.StartLocation = startLocation;
		varDecl.EndLocation = t.Location;
		fieldDeclaration.Add(varDecl);
		
	}

	void VariableDeclarator(
#line  1400 "VBNET.ATG" 
List<VariableDeclaration> fieldDeclaration) {
		Identifier();

#line  1402 "VBNET.ATG" 
		string name = t.val; 
		VariableDeclaratorPartAfterIdentifier(
#line  1403 "VBNET.ATG" 
fieldDeclaration, name);
	}

	void ConstantDeclarator(
#line  1381 "VBNET.ATG" 
List<VariableDeclaration> constantDeclaration) {

#line  1383 "VBNET.ATG" 
		Expression expr = null;
		TypeReference type = null;
		string name = String.Empty;
		Location location;
		
		Identifier();

#line  1388 "VBNET.ATG" 
		name = t.val; location = t.Location; 
		if (la.kind == 50) {
			lexer.NextToken();
			TypeName(
#line  1389 "VBNET.ATG" 
out type);
		}
		Expect(10);
		Expr(
#line  1390 "VBNET.ATG" 
out expr);

#line  1392 "VBNET.ATG" 
		VariableDeclaration f = new VariableDeclaration(name, expr);
		f.TypeReference = type;
		f.StartLocation = location;
		constantDeclaration.Add(f);
		
	}

	void AccessorDecls(
#line  1315 "VBNET.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1317 "VBNET.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		
		while (la.kind == 28) {
			AttributeSection(
#line  1322 "VBNET.ATG" 
out section);

#line  1322 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(24)) {
			GetAccessorDecl(
#line  1324 "VBNET.ATG" 
out getBlock, attributes);
			if (StartOf(25)) {

#line  1326 "VBNET.ATG" 
				attributes = new List<AttributeSection>(); 
				while (la.kind == 28) {
					AttributeSection(
#line  1327 "VBNET.ATG" 
out section);

#line  1327 "VBNET.ATG" 
					attributes.Add(section); 
				}
				SetAccessorDecl(
#line  1328 "VBNET.ATG" 
out setBlock, attributes);
			}
		} else if (StartOf(26)) {
			SetAccessorDecl(
#line  1331 "VBNET.ATG" 
out setBlock, attributes);
			if (StartOf(27)) {

#line  1333 "VBNET.ATG" 
				attributes = new List<AttributeSection>(); 
				while (la.kind == 28) {
					AttributeSection(
#line  1334 "VBNET.ATG" 
out section);

#line  1334 "VBNET.ATG" 
					attributes.Add(section); 
				}
				GetAccessorDecl(
#line  1335 "VBNET.ATG" 
out getBlock, attributes);
			}
		} else SynErr(251);
	}

	void EventAccessorDeclaration(
#line  1278 "VBNET.ATG" 
out EventAddRemoveRegion eventAccessorDeclaration) {

#line  1280 "VBNET.ATG" 
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		eventAccessorDeclaration = null;
		
		while (la.kind == 28) {
			AttributeSection(
#line  1286 "VBNET.ATG" 
out section);

#line  1286 "VBNET.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 43) {
			lexer.NextToken();
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1288 "VBNET.ATG" 
p);
				}
				Expect(26);
			}
			Expect(1);
			Block(
#line  1289 "VBNET.ATG" 
out stmt);
			Expect(100);
			Expect(43);
			EndOfStmt();

#line  1291 "VBNET.ATG" 
			eventAccessorDeclaration = new EventAddRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else if (la.kind == 178) {
			lexer.NextToken();
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1296 "VBNET.ATG" 
p);
				}
				Expect(26);
			}
			Expect(1);
			Block(
#line  1297 "VBNET.ATG" 
out stmt);
			Expect(100);
			Expect(178);
			EndOfStmt();

#line  1299 "VBNET.ATG" 
			eventAccessorDeclaration = new EventRemoveRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else if (la.kind == 174) {
			lexer.NextToken();
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(4)) {
					FormalParameterList(
#line  1304 "VBNET.ATG" 
p);
				}
				Expect(26);
			}
			Expect(1);
			Block(
#line  1305 "VBNET.ATG" 
out stmt);
			Expect(100);
			Expect(174);
			EndOfStmt();

#line  1307 "VBNET.ATG" 
			eventAccessorDeclaration = new EventRaiseRegion(attributes);
			eventAccessorDeclaration.Block = (BlockStatement)stmt;
			eventAccessorDeclaration.Parameters = p;
			
		} else SynErr(252);
	}

	void OverloadableOperator(
#line  1220 "VBNET.ATG" 
out OverloadableOperatorType operatorType) {

#line  1221 "VBNET.ATG" 
		operatorType = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 19: {
			lexer.NextToken();

#line  1223 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Add; 
			break;
		}
		case 18: {
			lexer.NextToken();

#line  1225 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Subtract; 
			break;
		}
		case 22: {
			lexer.NextToken();

#line  1227 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Multiply; 
			break;
		}
		case 14: {
			lexer.NextToken();

#line  1229 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Divide; 
			break;
		}
		case 15: {
			lexer.NextToken();

#line  1231 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.DivideInteger; 
			break;
		}
		case 13: {
			lexer.NextToken();

#line  1233 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Concat; 
			break;
		}
		case 136: {
			lexer.NextToken();

#line  1235 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Like; 
			break;
		}
		case 140: {
			lexer.NextToken();

#line  1237 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Modulus; 
			break;
		}
		case 47: {
			lexer.NextToken();

#line  1239 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 161: {
			lexer.NextToken();

#line  1241 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 221: {
			lexer.NextToken();

#line  1243 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 20: {
			lexer.NextToken();

#line  1245 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Power; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1247 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 33: {
			lexer.NextToken();

#line  1249 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.ShiftRight; 
			break;
		}
		case 10: {
			lexer.NextToken();

#line  1251 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.Equality; 
			break;
		}
		case 29: {
			lexer.NextToken();

#line  1253 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.InEquality; 
			break;
		}
		case 28: {
			lexer.NextToken();

#line  1255 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.LessThan; 
			break;
		}
		case 31: {
			lexer.NextToken();

#line  1257 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 27: {
			lexer.NextToken();

#line  1259 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.GreaterThan; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1261 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 81: {
			lexer.NextToken();

#line  1263 "VBNET.ATG" 
			operatorType = OverloadableOperatorType.CType; 
			break;
		}
		case 2: case 45: case 49: case 51: case 52: case 53: case 54: case 57: case 74: case 85: case 91: case 94: case 103: case 108: case 113: case 120: case 126: case 130: case 133: case 156: case 162: case 169: case 188: case 197: case 198: case 208: case 209: case 215: {
			Identifier();

#line  1267 "VBNET.ATG" 
			string opName = t.val; 
			if (string.Equals(opName, "istrue", StringComparison.InvariantCultureIgnoreCase)) {
				operatorType = OverloadableOperatorType.IsTrue;
			} else if (string.Equals(opName, "isfalse", StringComparison.InvariantCultureIgnoreCase)) {
				operatorType = OverloadableOperatorType.IsFalse;
			} else {
				Error("Invalid operator. Possible operators are '+', '-', 'Not', 'IsTrue', 'IsFalse'.");
			}
			
			break;
		}
		default: SynErr(253); break;
		}
	}

	void GetAccessorDecl(
#line  1341 "VBNET.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1342 "VBNET.ATG" 
		Statement stmt = null; Modifiers m; 
		PropertyAccessorAccessModifier(
#line  1344 "VBNET.ATG" 
out m);
		Expect(115);

#line  1346 "VBNET.ATG" 
		Location startLocation = t.Location; 
		Expect(1);
		Block(
#line  1348 "VBNET.ATG" 
out stmt);

#line  1349 "VBNET.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 
		Expect(100);
		Expect(115);

#line  1351 "VBNET.ATG" 
		getBlock.Modifier = m; 

#line  1352 "VBNET.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void SetAccessorDecl(
#line  1357 "VBNET.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1359 "VBNET.ATG" 
		Statement stmt = null;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Modifiers m;
		
		PropertyAccessorAccessModifier(
#line  1364 "VBNET.ATG" 
out m);
		Expect(183);

#line  1366 "VBNET.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 25) {
			lexer.NextToken();
			if (StartOf(4)) {
				FormalParameterList(
#line  1367 "VBNET.ATG" 
p);
			}
			Expect(26);
		}
		Expect(1);
		Block(
#line  1369 "VBNET.ATG" 
out stmt);

#line  1371 "VBNET.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes);
		setBlock.Modifier = m;
		setBlock.Parameters = p;
		
		Expect(100);
		Expect(183);

#line  1376 "VBNET.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
		EndOfStmt();
	}

	void PropertyAccessorAccessModifier(
#line  3396 "VBNET.ATG" 
out Modifiers m) {

#line  3397 "VBNET.ATG" 
		m = Modifiers.None; 
		while (StartOf(28)) {
			if (la.kind == 173) {
				lexer.NextToken();

#line  3399 "VBNET.ATG" 
				m |= Modifiers.Public; 
			} else if (la.kind == 172) {
				lexer.NextToken();

#line  3400 "VBNET.ATG" 
				m |= Modifiers.Protected; 
			} else if (la.kind == 112) {
				lexer.NextToken();

#line  3401 "VBNET.ATG" 
				m |= Modifiers.Internal; 
			} else {
				lexer.NextToken();

#line  3402 "VBNET.ATG" 
				m |= Modifiers.Private; 
			}
		}
	}

	void ArrayInitializationModifier(
#line  1474 "VBNET.ATG" 
out List<Expression> arrayModifiers) {

#line  1476 "VBNET.ATG" 
		arrayModifiers = null;
		
		Expect(25);
		InitializationRankList(
#line  1478 "VBNET.ATG" 
out arrayModifiers);
		Expect(26);
	}

	void ArrayNameModifier(
#line  2473 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  2475 "VBNET.ATG" 
		arrayModifiers = null;
		
		ArrayTypeModifiers(
#line  2477 "VBNET.ATG" 
out arrayModifiers);
	}

	void ObjectCreateExpression(
#line  1935 "VBNET.ATG" 
out Expression oce) {

#line  1937 "VBNET.ATG" 
		TypeReference type = null;
		Expression initializer = null;
		List<Expression> arguments = null;
		ArrayList dimensions = null;
		oce = null;
		bool canBeNormal; bool canBeReDim;
		
		Expect(148);
		if (StartOf(7)) {
			NonArrayTypeName(
#line  1945 "VBNET.ATG" 
out type, false);
			if (la.kind == 25) {
				lexer.NextToken();
				NormalOrReDimArgumentList(
#line  1946 "VBNET.ATG" 
out arguments, out canBeNormal, out canBeReDim);
				Expect(26);
				if (la.kind == 23 || 
#line  1947 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis) {
					if (
#line  1947 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis) {
						ArrayTypeModifiers(
#line  1948 "VBNET.ATG" 
out dimensions);
						CollectionInitializer(
#line  1949 "VBNET.ATG" 
out initializer);
					} else {
						CollectionInitializer(
#line  1950 "VBNET.ATG" 
out initializer);
					}
				}

#line  1952 "VBNET.ATG" 
				if (canBeReDim && !canBeNormal && initializer == null) initializer = new CollectionInitializerExpression(); 
			}
		}

#line  1956 "VBNET.ATG" 
		if (initializer == null) {
		oce = new ObjectCreateExpression(type, arguments);
		} else {
			if (dimensions == null) dimensions = new ArrayList();
			dimensions.Insert(0, (arguments == null) ? 0 : Math.Max(arguments.Count - 1, 0));
			type.RankSpecifier = (int[])dimensions.ToArray(typeof(int));
			ArrayCreateExpression ace = new ArrayCreateExpression(type, initializer as CollectionInitializerExpression);
			ace.Arguments = arguments;
			oce = ace;
		}
		
		if (la.kind == 218) {

#line  1970 "VBNET.ATG" 
			NamedArgumentExpression memberInitializer = null;
			
			lexer.NextToken();

#line  1974 "VBNET.ATG" 
			CollectionInitializerExpression memberInitializers = new CollectionInitializerExpression();
			memberInitializers.StartLocation = la.Location;
			
			Expect(23);
			MemberInitializer(
#line  1978 "VBNET.ATG" 
out memberInitializer);

#line  1979 "VBNET.ATG" 
			memberInitializers.CreateExpressions.Add(memberInitializer); 
			while (la.kind == 12) {
				lexer.NextToken();
				MemberInitializer(
#line  1981 "VBNET.ATG" 
out memberInitializer);

#line  1982 "VBNET.ATG" 
				memberInitializers.CreateExpressions.Add(memberInitializer); 
			}
			Expect(24);

#line  1986 "VBNET.ATG" 
			memberInitializers.EndLocation = t.Location;
			if(oce is ObjectCreateExpression)
			{
				((ObjectCreateExpression)oce).ObjectInitializer = memberInitializers;
			}
			
		}
	}

	void VariableInitializer(
#line  1502 "VBNET.ATG" 
out Expression initializerExpression) {

#line  1504 "VBNET.ATG" 
		initializerExpression = null;
		
		if (StartOf(29)) {
			Expr(
#line  1506 "VBNET.ATG" 
out initializerExpression);
		} else if (la.kind == 23) {
			CollectionInitializer(
#line  1507 "VBNET.ATG" 
out initializerExpression);
		} else SynErr(254);
	}

	void InitializationRankList(
#line  1482 "VBNET.ATG" 
out List<Expression> rank) {

#line  1484 "VBNET.ATG" 
		rank = new List<Expression>();
		Expression expr = null;
		
		Expr(
#line  1487 "VBNET.ATG" 
out expr);
		if (la.kind == 201) {
			lexer.NextToken();

#line  1488 "VBNET.ATG" 
			EnsureIsZero(expr); 
			Expr(
#line  1489 "VBNET.ATG" 
out expr);
		}

#line  1491 "VBNET.ATG" 
		if (expr != null) { rank.Add(expr); } 
		while (la.kind == 12) {
			lexer.NextToken();
			Expr(
#line  1493 "VBNET.ATG" 
out expr);
			if (la.kind == 201) {
				lexer.NextToken();

#line  1494 "VBNET.ATG" 
				EnsureIsZero(expr); 
				Expr(
#line  1495 "VBNET.ATG" 
out expr);
			}

#line  1497 "VBNET.ATG" 
			if (expr != null) { rank.Add(expr); } 
		}
	}

	void CollectionInitializer(
#line  1511 "VBNET.ATG" 
out Expression outExpr) {

#line  1513 "VBNET.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		
		Expect(23);
		if (StartOf(30)) {
			VariableInitializer(
#line  1518 "VBNET.ATG" 
out expr);

#line  1520 "VBNET.ATG" 
			if (expr != null) { initializer.CreateExpressions.Add(expr); }
			
			while (
#line  1523 "VBNET.ATG" 
NotFinalComma()) {
				Expect(12);
				VariableInitializer(
#line  1523 "VBNET.ATG" 
out expr);

#line  1524 "VBNET.ATG" 
				if (expr != null) { initializer.CreateExpressions.Add(expr); } 
			}
		}
		Expect(24);

#line  1527 "VBNET.ATG" 
		outExpr = initializer; 
	}

	void EventMemberSpecifier(
#line  1597 "VBNET.ATG" 
out string name) {

#line  1598 "VBNET.ATG" 
		string eventName; 
		if (StartOf(14)) {
			Identifier();
		} else if (la.kind == 144) {
			lexer.NextToken();
		} else if (la.kind == 139) {
			lexer.NextToken();
		} else SynErr(255);

#line  1601 "VBNET.ATG" 
		name = t.val; 
		Expect(16);
		IdentifierOrKeyword(
#line  1603 "VBNET.ATG" 
out eventName);

#line  1604 "VBNET.ATG" 
		name = name + "." + eventName; 
	}

	void IdentifierOrKeyword(
#line  3329 "VBNET.ATG" 
out string name) {

#line  3331 "VBNET.ATG" 
		lexer.NextToken(); name = t.val;  
	}

	void QueryExpr(
#line  2013 "VBNET.ATG" 
out Expression expr) {

#line  2015 "VBNET.ATG" 
		QueryExpression qexpr = new QueryExpression();
		qexpr.StartLocation = la.Location;
		List<QueryExpressionClause> middleClauses = new List<QueryExpressionClause>();
		expr = qexpr;
		
		FromOrAggregateQueryOperator(
#line  2020 "VBNET.ATG" 
middleClauses);
		while (StartOf(31)) {
			QueryOperator(
#line  2021 "VBNET.ATG" 
middleClauses);
		}

#line  2023 "VBNET.ATG" 
		qexpr.EndLocation = t.EndLocation;
		
	}

	void LambdaExpr(
#line  1995 "VBNET.ATG" 
out Expression expr) {

#line  1997 "VBNET.ATG" 
		Expression inner = null;
		LambdaExpression lambda = new LambdaExpression();
		lambda.StartLocation = la.Location;
		
		Expect(114);
		if (la.kind == 25) {
			lexer.NextToken();
			if (StartOf(4)) {
				FormalParameterList(
#line  2003 "VBNET.ATG" 
lambda.Parameters);
			}
			Expect(26);
		}
		Expr(
#line  2004 "VBNET.ATG" 
out inner);

#line  2006 "VBNET.ATG" 
		lambda.ExpressionBody = inner;
		lambda.EndLocation = t.EndLocation; // la.Location?
		
		expr = lambda;
		
	}

	void DisjunctionExpr(
#line  1779 "VBNET.ATG" 
out Expression outExpr) {

#line  1781 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ConjunctionExpr(
#line  1784 "VBNET.ATG" 
out outExpr);
		while (la.kind == 161 || la.kind == 163 || la.kind == 221) {
			if (la.kind == 161) {
				lexer.NextToken();

#line  1787 "VBNET.ATG" 
				op = BinaryOperatorType.BitwiseOr; 
			} else if (la.kind == 163) {
				lexer.NextToken();

#line  1788 "VBNET.ATG" 
				op = BinaryOperatorType.LogicalOr; 
			} else {
				lexer.NextToken();

#line  1789 "VBNET.ATG" 
				op = BinaryOperatorType.ExclusiveOr; 
			}
			ConjunctionExpr(
#line  1791 "VBNET.ATG" 
out expr);

#line  1791 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AssignmentOperator(
#line  1615 "VBNET.ATG" 
out AssignmentOperatorType op) {

#line  1616 "VBNET.ATG" 
		op = AssignmentOperatorType.None; 
		switch (la.kind) {
		case 10: {
			lexer.NextToken();

#line  1617 "VBNET.ATG" 
			op = AssignmentOperatorType.Assign; 
			break;
		}
		case 42: {
			lexer.NextToken();

#line  1618 "VBNET.ATG" 
			op = AssignmentOperatorType.ConcatString; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1619 "VBNET.ATG" 
			op = AssignmentOperatorType.Add; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1620 "VBNET.ATG" 
			op = AssignmentOperatorType.Subtract; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1621 "VBNET.ATG" 
			op = AssignmentOperatorType.Multiply; 
			break;
		}
		case 38: {
			lexer.NextToken();

#line  1622 "VBNET.ATG" 
			op = AssignmentOperatorType.Divide; 
			break;
		}
		case 39: {
			lexer.NextToken();

#line  1623 "VBNET.ATG" 
			op = AssignmentOperatorType.DivideInteger; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1624 "VBNET.ATG" 
			op = AssignmentOperatorType.Power; 
			break;
		}
		case 40: {
			lexer.NextToken();

#line  1625 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
			break;
		}
		case 41: {
			lexer.NextToken();

#line  1626 "VBNET.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
			break;
		}
		default: SynErr(256); break;
		}
	}

	void SimpleExpr(
#line  1630 "VBNET.ATG" 
out Expression pexpr) {

#line  1631 "VBNET.ATG" 
		string name; 
		SimpleNonInvocationExpression(
#line  1633 "VBNET.ATG" 
out pexpr);
		while (la.kind == 16 || la.kind == 17 || la.kind == 25) {
			if (la.kind == 16) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  1635 "VBNET.ATG" 
out name);

#line  1636 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(pexpr, name); 
				if (
#line  1637 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
					lexer.NextToken();
					Expect(155);
					TypeArgumentList(
#line  1638 "VBNET.ATG" 
((MemberReferenceExpression)pexpr).TypeArguments);
					Expect(26);
				}
			} else if (la.kind == 17) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  1640 "VBNET.ATG" 
out name);

#line  1640 "VBNET.ATG" 
				pexpr = new BinaryOperatorExpression(pexpr, BinaryOperatorType.DictionaryAccess, new PrimitiveExpression(name, name)); 
			} else {
				InvocationExpression(
#line  1641 "VBNET.ATG" 
ref pexpr);
			}
		}
	}

	void SimpleNonInvocationExpression(
#line  1645 "VBNET.ATG" 
out Expression pexpr) {

#line  1647 "VBNET.ATG" 
		Expression expr;
		TypeReference type = null;
		string name = String.Empty;
		pexpr = null;
		
		if (StartOf(32)) {
			switch (la.kind) {
			case 3: {
				lexer.NextToken();

#line  1655 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 4: {
				lexer.NextToken();

#line  1656 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 7: {
				lexer.NextToken();

#line  1657 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 6: {
				lexer.NextToken();

#line  1658 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 5: {
				lexer.NextToken();

#line  1659 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 9: {
				lexer.NextToken();

#line  1660 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 8: {
				lexer.NextToken();

#line  1661 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
				break;
			}
			case 202: {
				lexer.NextToken();

#line  1663 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(true, "true");  
				break;
			}
			case 109: {
				lexer.NextToken();

#line  1664 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(false, "false"); 
				break;
			}
			case 151: {
				lexer.NextToken();

#line  1665 "VBNET.ATG" 
				pexpr = new PrimitiveExpression(null, "null");  
				break;
			}
			case 25: {
				lexer.NextToken();
				Expr(
#line  1666 "VBNET.ATG" 
out expr);
				Expect(26);

#line  1666 "VBNET.ATG" 
				pexpr = new ParenthesizedExpression(expr); 
				break;
			}
			case 2: case 45: case 49: case 51: case 52: case 53: case 54: case 57: case 74: case 85: case 91: case 94: case 103: case 108: case 113: case 120: case 126: case 130: case 133: case 156: case 162: case 169: case 188: case 197: case 198: case 208: case 209: case 215: {
				Identifier();

#line  1668 "VBNET.ATG" 
				pexpr = new IdentifierExpression(t.val);
				pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation;
				
				if (
#line  1671 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
					lexer.NextToken();
					Expect(155);
					TypeArgumentList(
#line  1672 "VBNET.ATG" 
((IdentifierExpression)pexpr).TypeArguments);
					Expect(26);
				}
				break;
			}
			case 55: case 58: case 69: case 86: case 87: case 96: case 128: case 137: case 154: case 181: case 186: case 187: case 193: case 206: case 207: case 210: {

#line  1674 "VBNET.ATG" 
				string val = String.Empty; 
				if (StartOf(11)) {
					PrimitiveTypeName(
#line  1675 "VBNET.ATG" 
out val);
				} else if (la.kind == 154) {
					lexer.NextToken();

#line  1675 "VBNET.ATG" 
					val = "System.Object"; 
				} else SynErr(257);

#line  1676 "VBNET.ATG" 
				pexpr = new TypeReferenceExpression(new TypeReference(val, true)); 
				break;
			}
			case 139: {
				lexer.NextToken();

#line  1677 "VBNET.ATG" 
				pexpr = new ThisReferenceExpression(); 
				break;
			}
			case 144: case 145: {

#line  1678 "VBNET.ATG" 
				Expression retExpr = null; 
				if (la.kind == 144) {
					lexer.NextToken();

#line  1679 "VBNET.ATG" 
					retExpr = new BaseReferenceExpression(); 
				} else if (la.kind == 145) {
					lexer.NextToken();

#line  1680 "VBNET.ATG" 
					retExpr = new ClassReferenceExpression(); 
				} else SynErr(258);
				Expect(16);
				IdentifierOrKeyword(
#line  1682 "VBNET.ATG" 
out name);

#line  1682 "VBNET.ATG" 
				pexpr = new MemberReferenceExpression(retExpr, name); 
				break;
			}
			case 117: {
				lexer.NextToken();
				Expect(16);
				Identifier();

#line  1684 "VBNET.ATG" 
				type = new TypeReference(t.val ?? ""); 

#line  1686 "VBNET.ATG" 
				type.IsGlobal = true; 

#line  1687 "VBNET.ATG" 
				pexpr = new TypeReferenceExpression(type); 
				break;
			}
			case 148: {
				ObjectCreateExpression(
#line  1688 "VBNET.ATG" 
out expr);

#line  1688 "VBNET.ATG" 
				pexpr = expr; 
				break;
			}
			case 81: case 93: case 204: {

#line  1690 "VBNET.ATG" 
				CastType castType = CastType.Cast; 
				if (la.kind == 93) {
					lexer.NextToken();
				} else if (la.kind == 81) {
					lexer.NextToken();

#line  1692 "VBNET.ATG" 
					castType = CastType.Conversion; 
				} else if (la.kind == 204) {
					lexer.NextToken();

#line  1693 "VBNET.ATG" 
					castType = CastType.TryCast; 
				} else SynErr(259);
				Expect(25);
				Expr(
#line  1695 "VBNET.ATG" 
out expr);
				Expect(12);
				TypeName(
#line  1695 "VBNET.ATG" 
out type);
				Expect(26);

#line  1696 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, castType); 
				break;
			}
			case 63: case 64: case 65: case 66: case 67: case 68: case 70: case 72: case 73: case 77: case 78: case 79: case 80: case 82: case 83: case 84: {
				CastTarget(
#line  1697 "VBNET.ATG" 
out type);
				Expect(25);
				Expr(
#line  1697 "VBNET.ATG" 
out expr);
				Expect(26);

#line  1697 "VBNET.ATG" 
				pexpr = new CastExpression(type, expr, CastType.PrimitiveConversion); 
				break;
			}
			case 44: {
				lexer.NextToken();
				Expr(
#line  1698 "VBNET.ATG" 
out expr);

#line  1698 "VBNET.ATG" 
				pexpr = new AddressOfExpression(expr); 
				break;
			}
			case 116: {
				lexer.NextToken();
				Expect(25);
				GetTypeTypeName(
#line  1699 "VBNET.ATG" 
out type);
				Expect(26);

#line  1699 "VBNET.ATG" 
				pexpr = new TypeOfExpression(type); 
				break;
			}
			case 205: {
				lexer.NextToken();
				SimpleExpr(
#line  1700 "VBNET.ATG" 
out expr);
				Expect(131);
				TypeName(
#line  1700 "VBNET.ATG" 
out type);

#line  1700 "VBNET.ATG" 
				pexpr = new TypeOfIsExpression(expr, type); 
				break;
			}
			case 122: {
				ConditionalExpression(
#line  1701 "VBNET.ATG" 
out pexpr);
				break;
			}
			}
		} else if (la.kind == 16) {
			lexer.NextToken();
			IdentifierOrKeyword(
#line  1705 "VBNET.ATG" 
out name);

#line  1705 "VBNET.ATG" 
			pexpr = new MemberReferenceExpression(null, name);
		} else SynErr(260);
	}

	void TypeArgumentList(
#line  2509 "VBNET.ATG" 
List<TypeReference> typeArguments) {

#line  2511 "VBNET.ATG" 
		TypeReference typeref;
		
		TypeName(
#line  2513 "VBNET.ATG" 
out typeref);

#line  2513 "VBNET.ATG" 
		if (typeref != null) typeArguments.Add(typeref); 
		while (la.kind == 12) {
			lexer.NextToken();
			TypeName(
#line  2516 "VBNET.ATG" 
out typeref);

#line  2516 "VBNET.ATG" 
			if (typeref != null) typeArguments.Add(typeref); 
		}
	}

	void InvocationExpression(
#line  1743 "VBNET.ATG" 
ref Expression pexpr) {

#line  1744 "VBNET.ATG" 
		List<Expression> parameters = null; 
		Expect(25);

#line  1746 "VBNET.ATG" 
		Location start = t.Location; 
		ArgumentList(
#line  1747 "VBNET.ATG" 
out parameters);
		Expect(26);

#line  1750 "VBNET.ATG" 
		pexpr = new InvocationExpression(pexpr, parameters);
		

#line  1752 "VBNET.ATG" 
		pexpr.StartLocation = start; pexpr.EndLocation = t.Location; 
	}

	void PrimitiveTypeName(
#line  3336 "VBNET.ATG" 
out string type) {

#line  3337 "VBNET.ATG" 
		type = String.Empty; 
		switch (la.kind) {
		case 55: {
			lexer.NextToken();

#line  3338 "VBNET.ATG" 
			type = "System.Boolean"; 
			break;
		}
		case 86: {
			lexer.NextToken();

#line  3339 "VBNET.ATG" 
			type = "System.DateTime"; 
			break;
		}
		case 69: {
			lexer.NextToken();

#line  3340 "VBNET.ATG" 
			type = "System.Char"; 
			break;
		}
		case 193: {
			lexer.NextToken();

#line  3341 "VBNET.ATG" 
			type = "System.String"; 
			break;
		}
		case 87: {
			lexer.NextToken();

#line  3342 "VBNET.ATG" 
			type = "System.Decimal"; 
			break;
		}
		case 58: {
			lexer.NextToken();

#line  3343 "VBNET.ATG" 
			type = "System.Byte"; 
			break;
		}
		case 186: {
			lexer.NextToken();

#line  3344 "VBNET.ATG" 
			type = "System.Int16"; 
			break;
		}
		case 128: {
			lexer.NextToken();

#line  3345 "VBNET.ATG" 
			type = "System.Int32"; 
			break;
		}
		case 137: {
			lexer.NextToken();

#line  3346 "VBNET.ATG" 
			type = "System.Int64"; 
			break;
		}
		case 187: {
			lexer.NextToken();

#line  3347 "VBNET.ATG" 
			type = "System.Single"; 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  3348 "VBNET.ATG" 
			type = "System.Double"; 
			break;
		}
		case 206: {
			lexer.NextToken();

#line  3349 "VBNET.ATG" 
			type = "System.UInt32"; 
			break;
		}
		case 207: {
			lexer.NextToken();

#line  3350 "VBNET.ATG" 
			type = "System.UInt64"; 
			break;
		}
		case 210: {
			lexer.NextToken();

#line  3351 "VBNET.ATG" 
			type = "System.UInt16"; 
			break;
		}
		case 181: {
			lexer.NextToken();

#line  3352 "VBNET.ATG" 
			type = "System.SByte"; 
			break;
		}
		default: SynErr(261); break;
		}
	}

	void CastTarget(
#line  1757 "VBNET.ATG" 
out TypeReference type) {

#line  1759 "VBNET.ATG" 
		type = null;
		
		switch (la.kind) {
		case 63: {
			lexer.NextToken();

#line  1761 "VBNET.ATG" 
			type = new TypeReference("System.Boolean", true); 
			break;
		}
		case 64: {
			lexer.NextToken();

#line  1762 "VBNET.ATG" 
			type = new TypeReference("System.Byte", true); 
			break;
		}
		case 77: {
			lexer.NextToken();

#line  1763 "VBNET.ATG" 
			type = new TypeReference("System.SByte", true); 
			break;
		}
		case 65: {
			lexer.NextToken();

#line  1764 "VBNET.ATG" 
			type = new TypeReference("System.Char", true); 
			break;
		}
		case 66: {
			lexer.NextToken();

#line  1765 "VBNET.ATG" 
			type = new TypeReference("System.DateTime", true); 
			break;
		}
		case 68: {
			lexer.NextToken();

#line  1766 "VBNET.ATG" 
			type = new TypeReference("System.Decimal", true); 
			break;
		}
		case 67: {
			lexer.NextToken();

#line  1767 "VBNET.ATG" 
			type = new TypeReference("System.Double", true); 
			break;
		}
		case 78: {
			lexer.NextToken();

#line  1768 "VBNET.ATG" 
			type = new TypeReference("System.Int16", true); 
			break;
		}
		case 70: {
			lexer.NextToken();

#line  1769 "VBNET.ATG" 
			type = new TypeReference("System.Int32", true); 
			break;
		}
		case 72: {
			lexer.NextToken();

#line  1770 "VBNET.ATG" 
			type = new TypeReference("System.Int64", true); 
			break;
		}
		case 84: {
			lexer.NextToken();

#line  1771 "VBNET.ATG" 
			type = new TypeReference("System.UInt16", true); 
			break;
		}
		case 82: {
			lexer.NextToken();

#line  1772 "VBNET.ATG" 
			type = new TypeReference("System.UInt32", true); 
			break;
		}
		case 83: {
			lexer.NextToken();

#line  1773 "VBNET.ATG" 
			type = new TypeReference("System.UInt64", true); 
			break;
		}
		case 73: {
			lexer.NextToken();

#line  1774 "VBNET.ATG" 
			type = new TypeReference("System.Object", true); 
			break;
		}
		case 79: {
			lexer.NextToken();

#line  1775 "VBNET.ATG" 
			type = new TypeReference("System.Single", true); 
			break;
		}
		case 80: {
			lexer.NextToken();

#line  1776 "VBNET.ATG" 
			type = new TypeReference("System.String", true); 
			break;
		}
		default: SynErr(262); break;
		}
	}

	void GetTypeTypeName(
#line  2408 "VBNET.ATG" 
out TypeReference typeref) {

#line  2409 "VBNET.ATG" 
		ArrayList rank = null; 
		NonArrayTypeName(
#line  2411 "VBNET.ATG" 
out typeref, true);
		ArrayTypeModifiers(
#line  2412 "VBNET.ATG" 
out rank);

#line  2413 "VBNET.ATG" 
		if (rank != null && typeref != null) {
		typeref.RankSpecifier = (int[])rank.ToArray(typeof(int));
		}
		
	}

	void ConditionalExpression(
#line  1709 "VBNET.ATG" 
out Expression expr) {

#line  1711 "VBNET.ATG" 
		ConditionalExpression conditionalExpression = new ConditionalExpression();
		BinaryOperatorExpression binaryOperatorExpression = new BinaryOperatorExpression();
		conditionalExpression.StartLocation = binaryOperatorExpression.StartLocation = la.Location;
		
		Expression condition = null;
		Expression trueExpr = null;
		Expression falseExpr = null;
		
		Expect(122);
		Expect(25);
		Expr(
#line  1720 "VBNET.ATG" 
out condition);
		Expect(12);
		Expr(
#line  1720 "VBNET.ATG" 
out trueExpr);
		if (la.kind == 12) {
			lexer.NextToken();
			Expr(
#line  1720 "VBNET.ATG" 
out falseExpr);
		}
		Expect(26);

#line  1722 "VBNET.ATG" 
		if(falseExpr != null)
		{
			conditionalExpression.Condition = condition;
			conditionalExpression.TrueExpression = trueExpr;
			conditionalExpression.FalseExpression = falseExpr;
			conditionalExpression.EndLocation = t.EndLocation;
			
			expr = conditionalExpression;
		}
		else
		{
			binaryOperatorExpression.Left = condition;
			binaryOperatorExpression.Right = trueExpr;
			binaryOperatorExpression.Op = BinaryOperatorType.NullCoalescing;
			binaryOperatorExpression.EndLocation = t.EndLocation;
			
			expr = binaryOperatorExpression;
		}
		
	}

	void ArgumentList(
#line  2340 "VBNET.ATG" 
out List<Expression> arguments) {

#line  2342 "VBNET.ATG" 
		arguments = new List<Expression>();
		Expression expr = null;
		
		if (StartOf(29)) {
			Argument(
#line  2345 "VBNET.ATG" 
out expr);
		}
		while (la.kind == 12) {
			lexer.NextToken();

#line  2346 "VBNET.ATG" 
			arguments.Add(expr ?? Expression.Null); expr = null; 
			if (StartOf(29)) {
				Argument(
#line  2347 "VBNET.ATG" 
out expr);
			}

#line  2348 "VBNET.ATG" 
			if (expr == null) expr = Expression.Null; 
		}

#line  2350 "VBNET.ATG" 
		if (expr != null) arguments.Add(expr); 
	}

	void ConjunctionExpr(
#line  1795 "VBNET.ATG" 
out Expression outExpr) {

#line  1797 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		NotExpr(
#line  1800 "VBNET.ATG" 
out outExpr);
		while (la.kind == 47 || la.kind == 48) {
			if (la.kind == 47) {
				lexer.NextToken();

#line  1803 "VBNET.ATG" 
				op = BinaryOperatorType.BitwiseAnd; 
			} else {
				lexer.NextToken();

#line  1804 "VBNET.ATG" 
				op = BinaryOperatorType.LogicalAnd; 
			}
			NotExpr(
#line  1806 "VBNET.ATG" 
out expr);

#line  1806 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void NotExpr(
#line  1810 "VBNET.ATG" 
out Expression outExpr) {

#line  1811 "VBNET.ATG" 
		UnaryOperatorType uop = UnaryOperatorType.None; 
		while (la.kind == 150) {
			lexer.NextToken();

#line  1812 "VBNET.ATG" 
			uop = UnaryOperatorType.Not; 
		}
		ComparisonExpr(
#line  1813 "VBNET.ATG" 
out outExpr);

#line  1814 "VBNET.ATG" 
		if (uop != UnaryOperatorType.None)
		outExpr = new UnaryOperatorExpression(outExpr, uop);
		
	}

	void ComparisonExpr(
#line  1819 "VBNET.ATG" 
out Expression outExpr) {

#line  1821 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  1824 "VBNET.ATG" 
out outExpr);
		while (StartOf(33)) {
			switch (la.kind) {
			case 28: {
				lexer.NextToken();

#line  1827 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 27: {
				lexer.NextToken();

#line  1828 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 31: {
				lexer.NextToken();

#line  1829 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 30: {
				lexer.NextToken();

#line  1830 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 29: {
				lexer.NextToken();

#line  1831 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			case 10: {
				lexer.NextToken();

#line  1832 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 136: {
				lexer.NextToken();

#line  1833 "VBNET.ATG" 
				op = BinaryOperatorType.Like; 
				break;
			}
			case 131: {
				lexer.NextToken();

#line  1834 "VBNET.ATG" 
				op = BinaryOperatorType.ReferenceEquality; 
				break;
			}
			case 132: {
				lexer.NextToken();

#line  1835 "VBNET.ATG" 
				op = BinaryOperatorType.ReferenceInequality; 
				break;
			}
			}
			if (StartOf(34)) {
				ShiftExpr(
#line  1838 "VBNET.ATG" 
out expr);

#line  1838 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
			} else if (la.kind == 150) {
				lexer.NextToken();
				ShiftExpr(
#line  1841 "VBNET.ATG" 
out expr);

#line  1841 "VBNET.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, new UnaryOperatorExpression(expr, UnaryOperatorType.Not));  
			} else SynErr(263);
		}
	}

	void ShiftExpr(
#line  1846 "VBNET.ATG" 
out Expression outExpr) {

#line  1848 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ConcatenationExpr(
#line  1851 "VBNET.ATG" 
out outExpr);
		while (la.kind == 32 || la.kind == 33) {
			if (la.kind == 32) {
				lexer.NextToken();

#line  1854 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				lexer.NextToken();

#line  1855 "VBNET.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			ConcatenationExpr(
#line  1857 "VBNET.ATG" 
out expr);

#line  1857 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void ConcatenationExpr(
#line  1861 "VBNET.ATG" 
out Expression outExpr) {

#line  1862 "VBNET.ATG" 
		Expression expr; 
		AdditiveExpr(
#line  1864 "VBNET.ATG" 
out outExpr);
		while (la.kind == 13) {
			lexer.NextToken();
			AdditiveExpr(
#line  1864 "VBNET.ATG" 
out expr);

#line  1864 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Concat, expr);  
		}
	}

	void AdditiveExpr(
#line  1867 "VBNET.ATG" 
out Expression outExpr) {

#line  1869 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ModuloExpr(
#line  1872 "VBNET.ATG" 
out outExpr);
		while (la.kind == 18 || la.kind == 19) {
			if (la.kind == 19) {
				lexer.NextToken();

#line  1875 "VBNET.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  1876 "VBNET.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			ModuloExpr(
#line  1878 "VBNET.ATG" 
out expr);

#line  1878 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void ModuloExpr(
#line  1882 "VBNET.ATG" 
out Expression outExpr) {

#line  1883 "VBNET.ATG" 
		Expression expr; 
		IntegerDivisionExpr(
#line  1885 "VBNET.ATG" 
out outExpr);
		while (la.kind == 140) {
			lexer.NextToken();
			IntegerDivisionExpr(
#line  1885 "VBNET.ATG" 
out expr);

#line  1885 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Modulus, expr);  
		}
	}

	void IntegerDivisionExpr(
#line  1888 "VBNET.ATG" 
out Expression outExpr) {

#line  1889 "VBNET.ATG" 
		Expression expr; 
		MultiplicativeExpr(
#line  1891 "VBNET.ATG" 
out outExpr);
		while (la.kind == 15) {
			lexer.NextToken();
			MultiplicativeExpr(
#line  1891 "VBNET.ATG" 
out expr);

#line  1891 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.DivideInteger, expr);  
		}
	}

	void MultiplicativeExpr(
#line  1894 "VBNET.ATG" 
out Expression outExpr) {

#line  1896 "VBNET.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		UnaryExpr(
#line  1899 "VBNET.ATG" 
out outExpr);
		while (la.kind == 14 || la.kind == 22) {
			if (la.kind == 22) {
				lexer.NextToken();

#line  1902 "VBNET.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else {
				lexer.NextToken();

#line  1903 "VBNET.ATG" 
				op = BinaryOperatorType.Divide; 
			}
			UnaryExpr(
#line  1905 "VBNET.ATG" 
out expr);

#line  1905 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void UnaryExpr(
#line  1909 "VBNET.ATG" 
out Expression uExpr) {

#line  1911 "VBNET.ATG" 
		Expression expr;
		UnaryOperatorType uop = UnaryOperatorType.None;
		bool isUOp = false;
		
		while (la.kind == 18 || la.kind == 19 || la.kind == 22) {
			if (la.kind == 19) {
				lexer.NextToken();

#line  1915 "VBNET.ATG" 
				uop = UnaryOperatorType.Plus; isUOp = true; 
			} else if (la.kind == 18) {
				lexer.NextToken();

#line  1916 "VBNET.ATG" 
				uop = UnaryOperatorType.Minus; isUOp = true; 
			} else {
				lexer.NextToken();

#line  1917 "VBNET.ATG" 
				uop = UnaryOperatorType.Dereference;  isUOp = true;
			}
		}
		ExponentiationExpr(
#line  1919 "VBNET.ATG" 
out expr);

#line  1921 "VBNET.ATG" 
		if (isUOp) {
		uExpr = new UnaryOperatorExpression(expr, uop);
		} else {
			uExpr = expr;
		}
		
	}

	void ExponentiationExpr(
#line  1929 "VBNET.ATG" 
out Expression outExpr) {

#line  1930 "VBNET.ATG" 
		Expression expr; 
		SimpleExpr(
#line  1932 "VBNET.ATG" 
out outExpr);
		while (la.kind == 20) {
			lexer.NextToken();
			SimpleExpr(
#line  1932 "VBNET.ATG" 
out expr);

#line  1932 "VBNET.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.Power, expr);  
		}
	}

	void NormalOrReDimArgumentList(
#line  2354 "VBNET.ATG" 
out List<Expression> arguments, out bool canBeNormal, out bool canBeRedim) {

#line  2356 "VBNET.ATG" 
		arguments = new List<Expression>();
		canBeNormal = true; canBeRedim = !IsNamedAssign();
		Expression expr = null;
		
		if (StartOf(29)) {
			Argument(
#line  2361 "VBNET.ATG" 
out expr);
			if (la.kind == 201) {
				lexer.NextToken();

#line  2362 "VBNET.ATG" 
				EnsureIsZero(expr); canBeNormal = false; 
				Expr(
#line  2363 "VBNET.ATG" 
out expr);
			}
		}
		while (la.kind == 12) {
			lexer.NextToken();

#line  2366 "VBNET.ATG" 
			if (expr == null) canBeRedim = false; 

#line  2367 "VBNET.ATG" 
			arguments.Add(expr ?? Expression.Null); expr = null; 

#line  2368 "VBNET.ATG" 
			canBeRedim &= !IsNamedAssign(); 
			if (StartOf(29)) {
				Argument(
#line  2369 "VBNET.ATG" 
out expr);
				if (la.kind == 201) {
					lexer.NextToken();

#line  2370 "VBNET.ATG" 
					EnsureIsZero(expr); canBeNormal = false; 
					Expr(
#line  2371 "VBNET.ATG" 
out expr);
				}
			}

#line  2373 "VBNET.ATG" 
			if (expr == null) { canBeRedim = false; expr = Expression.Null; } 
		}

#line  2375 "VBNET.ATG" 
		if (expr != null) arguments.Add(expr); else canBeRedim = false; 
	}

	void ArrayTypeModifiers(
#line  2482 "VBNET.ATG" 
out ArrayList arrayModifiers) {

#line  2484 "VBNET.ATG" 
		arrayModifiers = new ArrayList();
		int i = 0;
		
		while (
#line  2487 "VBNET.ATG" 
IsDims()) {
			Expect(25);
			if (la.kind == 12 || la.kind == 26) {
				RankList(
#line  2489 "VBNET.ATG" 
out i);
			}

#line  2491 "VBNET.ATG" 
			arrayModifiers.Add(i);
			
			Expect(26);
		}

#line  2496 "VBNET.ATG" 
		if(arrayModifiers.Count == 0) {
		 arrayModifiers = null;
		}
		
	}

	void MemberInitializer(
#line  2324 "VBNET.ATG" 
out NamedArgumentExpression memberInitializer) {

#line  2326 "VBNET.ATG" 
		memberInitializer = new NamedArgumentExpression();
		memberInitializer.StartLocation = la.Location;
		Expression initExpr = null;
		string name = null;
		
		Expect(16);
		IdentifierOrKeyword(
#line  2331 "VBNET.ATG" 
out name);
		Expect(10);
		Expr(
#line  2331 "VBNET.ATG" 
out initExpr);

#line  2333 "VBNET.ATG" 
		memberInitializer.Name = name;
		memberInitializer.Expression = initExpr;
		memberInitializer.EndLocation = t.EndLocation;
		
	}

	void FromOrAggregateQueryOperator(
#line  2027 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2028 "VBNET.ATG" 
		
		if (la.kind == 113) {
			FromQueryOperator(
#line  2029 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 45) {
			AggregateQueryOperator(
#line  2030 "VBNET.ATG" 
middleClauses);
		} else SynErr(264);
	}

	void QueryOperator(
#line  2033 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2035 "VBNET.ATG" 
		QueryExpressionJoinVBClause joinClause = null;
		QueryExpressionGroupVBClause groupByClause = null;
		QueryExpressionPartitionVBClause partitionClause = null;
		QueryExpressionGroupJoinVBClause groupJoinClause = null;
		
		if (la.kind == 113) {
			FromQueryOperator(
#line  2040 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 45) {
			AggregateQueryOperator(
#line  2041 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 182) {
			SelectQueryOperator(
#line  2042 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 94) {
			DistinctQueryOperator(
#line  2043 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 215) {
			WhereQueryOperator(
#line  2044 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 162) {
			OrderByQueryOperator(
#line  2045 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 188 || la.kind == 197) {
			PartitionQueryOperator(
#line  2046 "VBNET.ATG" 
out partitionClause);
		} else if (la.kind == 134) {
			LetQueryOperator(
#line  2047 "VBNET.ATG" 
middleClauses);
		} else if (la.kind == 133) {
			JoinQueryOperator(
#line  2048 "VBNET.ATG" 
out joinClause);

#line  2049 "VBNET.ATG" 
			middleClauses.Add(joinClause); 
		} else if (
#line  2050 "VBNET.ATG" 
la.kind == Tokens.Group && Peek(1).kind == Tokens.Join) {
			GroupJoinQueryOperator(
#line  2050 "VBNET.ATG" 
out groupJoinClause);

#line  2051 "VBNET.ATG" 
			middleClauses.Add(groupJoinClause); 
		} else if (la.kind == 120) {
			GroupByQueryOperator(
#line  2052 "VBNET.ATG" 
out groupByClause);

#line  2053 "VBNET.ATG" 
			middleClauses.Add(groupByClause); 
		} else SynErr(265);
	}

	void FromQueryOperator(
#line  2128 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2130 "VBNET.ATG" 
		
		Expect(113);
		CollectionRangeVariableDeclarationList(
#line  2131 "VBNET.ATG" 
middleClauses);
	}

	void AggregateQueryOperator(
#line  2191 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2193 "VBNET.ATG" 
		QueryExpressionFromClause fromClause = null;
		QueryExpressionAggregateClause aggregateClause = new QueryExpressionAggregateClause();
		aggregateClause.IntoVariables = new List<ExpressionRangeVariable>();
		aggregateClause.StartLocation = la.Location;
		
		Expect(45);
		CollectionRangeVariableDeclaration(
#line  2198 "VBNET.ATG" 
out fromClause);

#line  2200 "VBNET.ATG" 
		aggregateClause.FromClause = fromClause;
		
		while (StartOf(31)) {
			QueryOperator(
#line  2203 "VBNET.ATG" 
aggregateClause.MiddleClauses);
		}
		Expect(130);
		ExpressionRangeVariableDeclarationList(
#line  2205 "VBNET.ATG" 
aggregateClause.IntoVariables);

#line  2207 "VBNET.ATG" 
		aggregateClause.EndLocation = t.EndLocation;
		middleClauses.Add(aggregateClause);
		
	}

	void SelectQueryOperator(
#line  2134 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2136 "VBNET.ATG" 
		QueryExpressionSelectVBClause selectClause = new QueryExpressionSelectVBClause();
		selectClause.StartLocation = la.Location;
		
		Expect(182);
		ExpressionRangeVariableDeclarationList(
#line  2139 "VBNET.ATG" 
selectClause.Variables);

#line  2141 "VBNET.ATG" 
		selectClause.EndLocation = t.Location;
		middleClauses.Add(selectClause);
		
	}

	void DistinctQueryOperator(
#line  2146 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2148 "VBNET.ATG" 
		QueryExpressionDistinctClause distinctClause = new QueryExpressionDistinctClause();
		distinctClause.StartLocation = la.Location;
		
		Expect(94);

#line  2153 "VBNET.ATG" 
		distinctClause.EndLocation = t.EndLocation;
		middleClauses.Add(distinctClause);
		
	}

	void WhereQueryOperator(
#line  2158 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2160 "VBNET.ATG" 
		QueryExpressionWhereClause whereClause = new QueryExpressionWhereClause();
		whereClause.StartLocation = la.Location;
		Expression operand = null;
		
		Expect(215);
		Expr(
#line  2164 "VBNET.ATG" 
out operand);

#line  2166 "VBNET.ATG" 
		whereClause.Condition = operand;
		whereClause.EndLocation = t.EndLocation;
		
		middleClauses.Add(whereClause);
		
	}

	void OrderByQueryOperator(
#line  2056 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2058 "VBNET.ATG" 
		QueryExpressionOrderClause orderClause = new QueryExpressionOrderClause();
		orderClause.StartLocation = la.Location;
		List<QueryExpressionOrdering> orderings = null;
		
		Expect(162);
		Expect(57);
		OrderExpressionList(
#line  2062 "VBNET.ATG" 
out orderings);

#line  2064 "VBNET.ATG" 
		orderClause.Orderings = orderings;
		orderClause.EndLocation = t.EndLocation;
		middleClauses.Add(orderClause);
		
	}

	void PartitionQueryOperator(
#line  2173 "VBNET.ATG" 
out QueryExpressionPartitionVBClause partitionClause) {

#line  2175 "VBNET.ATG" 
		partitionClause = new QueryExpressionPartitionVBClause();
		partitionClause.StartLocation = la.Location;
		Expression expr = null;
		
		if (la.kind == 197) {
			lexer.NextToken();

#line  2179 "VBNET.ATG" 
			partitionClause.PartitionType = QueryExpressionPartitionType.Take; 
			if (la.kind == 216) {
				lexer.NextToken();

#line  2180 "VBNET.ATG" 
				partitionClause.PartitionType = QueryExpressionPartitionType.TakeWhile; 
			}
			Expr(
#line  2181 "VBNET.ATG" 
out expr);
		} else if (la.kind == 188) {
			lexer.NextToken();

#line  2182 "VBNET.ATG" 
			partitionClause.PartitionType = QueryExpressionPartitionType.Skip; 
			if (la.kind == 216) {
				lexer.NextToken();
			}

#line  2183 "VBNET.ATG" 
			partitionClause.PartitionType = QueryExpressionPartitionType.SkipWhile; 
			Expr(
#line  2184 "VBNET.ATG" 
out expr);

#line  2186 "VBNET.ATG" 
			partitionClause.Expression = expr;
			partitionClause.EndLocation = t.EndLocation;
			
		} else SynErr(266);
	}

	void LetQueryOperator(
#line  2212 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2214 "VBNET.ATG" 
		QueryExpressionLetVBClause letClause = new QueryExpressionLetVBClause();
		letClause.StartLocation = la.Location;
		
		Expect(134);
		ExpressionRangeVariableDeclarationList(
#line  2217 "VBNET.ATG" 
letClause.Variables);

#line  2219 "VBNET.ATG" 
		letClause.EndLocation = t.EndLocation;
		middleClauses.Add(letClause);
		
	}

	void JoinQueryOperator(
#line  2256 "VBNET.ATG" 
out QueryExpressionJoinVBClause joinClause) {

#line  2258 "VBNET.ATG" 
		joinClause = new QueryExpressionJoinVBClause();
		joinClause.StartLocation = la.Location;
		QueryExpressionFromClause joinVariable = null;
		QueryExpressionJoinVBClause subJoin = null;
		QueryExpressionJoinConditionVB condition = null;
		
		
		Expect(133);
		CollectionRangeVariableDeclaration(
#line  2265 "VBNET.ATG" 
out joinVariable);

#line  2266 "VBNET.ATG" 
		joinClause.JoinVariable = joinVariable; 
		if (la.kind == 133) {
			JoinQueryOperator(
#line  2268 "VBNET.ATG" 
out subJoin);

#line  2269 "VBNET.ATG" 
			joinClause.SubJoin = subJoin; 
		}
		Expect(157);
		JoinCondition(
#line  2272 "VBNET.ATG" 
out condition);

#line  2273 "VBNET.ATG" 
		SafeAdd(joinClause, joinClause.Conditions, condition); 
		while (la.kind == 47) {
			lexer.NextToken();
			JoinCondition(
#line  2275 "VBNET.ATG" 
out condition);

#line  2276 "VBNET.ATG" 
			SafeAdd(joinClause, joinClause.Conditions, condition); 
		}

#line  2279 "VBNET.ATG" 
		joinClause.EndLocation = t.EndLocation;
		
	}

	void GroupJoinQueryOperator(
#line  2114 "VBNET.ATG" 
out QueryExpressionGroupJoinVBClause groupJoinClause) {

#line  2116 "VBNET.ATG" 
		groupJoinClause = new QueryExpressionGroupJoinVBClause();
		groupJoinClause.StartLocation = la.Location;
		QueryExpressionJoinVBClause joinClause = null;
		
		Expect(120);
		JoinQueryOperator(
#line  2120 "VBNET.ATG" 
out joinClause);
		Expect(130);
		ExpressionRangeVariableDeclarationList(
#line  2121 "VBNET.ATG" 
groupJoinClause.IntoVariables);

#line  2123 "VBNET.ATG" 
		groupJoinClause.JoinClause = joinClause;
		groupJoinClause.EndLocation = t.EndLocation;
		
	}

	void GroupByQueryOperator(
#line  2101 "VBNET.ATG" 
out QueryExpressionGroupVBClause groupByClause) {

#line  2103 "VBNET.ATG" 
		groupByClause = new QueryExpressionGroupVBClause();
		groupByClause.StartLocation = la.Location;
		
		Expect(120);
		ExpressionRangeVariableDeclarationList(
#line  2106 "VBNET.ATG" 
groupByClause.GroupVariables);
		Expect(57);
		ExpressionRangeVariableDeclarationList(
#line  2107 "VBNET.ATG" 
groupByClause.ByVariables);
		Expect(130);
		ExpressionRangeVariableDeclarationList(
#line  2108 "VBNET.ATG" 
groupByClause.IntoVariables);

#line  2110 "VBNET.ATG" 
		groupByClause.EndLocation = t.EndLocation;
		
	}

	void OrderExpressionList(
#line  2070 "VBNET.ATG" 
out List<QueryExpressionOrdering> orderings) {

#line  2072 "VBNET.ATG" 
		orderings = new List<QueryExpressionOrdering>();
		QueryExpressionOrdering ordering = null;
		
		OrderExpression(
#line  2075 "VBNET.ATG" 
out ordering);

#line  2076 "VBNET.ATG" 
		orderings.Add(ordering); 
		while (la.kind == 12) {
			lexer.NextToken();
			OrderExpression(
#line  2078 "VBNET.ATG" 
out ordering);

#line  2079 "VBNET.ATG" 
			orderings.Add(ordering); 
		}
	}

	void OrderExpression(
#line  2083 "VBNET.ATG" 
out QueryExpressionOrdering ordering) {

#line  2085 "VBNET.ATG" 
		ordering = new QueryExpressionOrdering();
		ordering.StartLocation = la.Location;
		ordering.Direction = QueryExpressionOrderingDirection.None;
		Expression orderExpr = null;
		
		Expr(
#line  2090 "VBNET.ATG" 
out orderExpr);

#line  2092 "VBNET.ATG" 
		ordering.Criteria = orderExpr;
		
		if (la.kind == 51 || la.kind == 91) {
			if (la.kind == 51) {
				lexer.NextToken();

#line  2095 "VBNET.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Ascending; 
			} else {
				lexer.NextToken();

#line  2096 "VBNET.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Descending; 
			}
		}

#line  2098 "VBNET.ATG" 
		ordering.EndLocation = t.EndLocation; 
	}

	void ExpressionRangeVariableDeclarationList(
#line  2224 "VBNET.ATG" 
List<ExpressionRangeVariable> variables) {

#line  2226 "VBNET.ATG" 
		ExpressionRangeVariable variable = null;
		
		ExpressionRangeVariableDeclaration(
#line  2228 "VBNET.ATG" 
out variable);

#line  2229 "VBNET.ATG" 
		variables.Add(variable); 
		while (la.kind == 12) {
			lexer.NextToken();
			ExpressionRangeVariableDeclaration(
#line  2230 "VBNET.ATG" 
out variable);

#line  2230 "VBNET.ATG" 
			variables.Add(variable); 
		}
	}

	void CollectionRangeVariableDeclarationList(
#line  2283 "VBNET.ATG" 
List<QueryExpressionClause> middleClauses) {

#line  2285 "VBNET.ATG" 
		QueryExpressionFromClause fromClause = null;
		
		CollectionRangeVariableDeclaration(
#line  2287 "VBNET.ATG" 
out fromClause);

#line  2288 "VBNET.ATG" 
		middleClauses.Add(fromClause); 
		while (la.kind == 12) {
			lexer.NextToken();
			CollectionRangeVariableDeclaration(
#line  2289 "VBNET.ATG" 
out fromClause);

#line  2289 "VBNET.ATG" 
			middleClauses.Add(fromClause); 
		}
	}

	void CollectionRangeVariableDeclaration(
#line  2292 "VBNET.ATG" 
out QueryExpressionFromClause fromClause) {

#line  2294 "VBNET.ATG" 
		fromClause = new QueryExpressionFromClause();
		fromClause.StartLocation = la.Location;
		TypeReference typeName = null;
		Expression inExpr = null;
		
		Identifier();
		if (la.kind == 50) {
			lexer.NextToken();
			TypeName(
#line  2300 "VBNET.ATG" 
out typeName);

#line  2300 "VBNET.ATG" 
			fromClause.Type = typeName; 
		}
		Expect(125);
		Expr(
#line  2301 "VBNET.ATG" 
out inExpr);

#line  2303 "VBNET.ATG" 
		fromClause.InExpression = inExpr;
		fromClause.EndLocation = t.EndLocation;
		
	}

	void ExpressionRangeVariableDeclaration(
#line  2233 "VBNET.ATG" 
out ExpressionRangeVariable variable) {

#line  2235 "VBNET.ATG" 
		variable = new ExpressionRangeVariable();
		variable.StartLocation = la.Location;
		Expression rhs = null;
		TypeReference typeName = null;
		
		if (
#line  2241 "VBNET.ATG" 
IsIdentifiedExpressionRange()) {
			Identifier();

#line  2242 "VBNET.ATG" 
			variable.Identifier = t.val; 
			if (la.kind == 50) {
				lexer.NextToken();
				TypeName(
#line  2244 "VBNET.ATG" 
out typeName);

#line  2245 "VBNET.ATG" 
				variable.Type = typeName; 
			}
			Expect(10);
		}
		Expr(
#line  2249 "VBNET.ATG" 
out rhs);

#line  2251 "VBNET.ATG" 
		variable.Expression = rhs;
		variable.EndLocation = t.EndLocation;
		
	}

	void JoinCondition(
#line  2308 "VBNET.ATG" 
out QueryExpressionJoinConditionVB condition) {

#line  2310 "VBNET.ATG" 
		condition = new QueryExpressionJoinConditionVB();
		condition.StartLocation = la.Location;
		
		Expression lhs = null;
		Expression rhs = null;
		
		Expr(
#line  2316 "VBNET.ATG" 
out lhs);
		Expect(103);
		Expr(
#line  2316 "VBNET.ATG" 
out rhs);

#line  2318 "VBNET.ATG" 
		condition.LeftSide = lhs;
		condition.RightSide = rhs;
		condition.EndLocation = t.EndLocation;
		
	}

	void Argument(
#line  2379 "VBNET.ATG" 
out Expression argumentexpr) {

#line  2381 "VBNET.ATG" 
		Expression expr;
		argumentexpr = null;
		string name;
		
		if (
#line  2385 "VBNET.ATG" 
IsNamedAssign()) {
			Identifier();

#line  2385 "VBNET.ATG" 
			name = t.val;  
			Expect(11);
			Expect(10);
			Expr(
#line  2385 "VBNET.ATG" 
out expr);

#line  2387 "VBNET.ATG" 
			argumentexpr = new NamedArgumentExpression(name, expr);
			
		} else if (StartOf(29)) {
			Expr(
#line  2390 "VBNET.ATG" 
out argumentexpr);
		} else SynErr(267);
	}

	void QualIdentAndTypeArguments(
#line  2456 "VBNET.ATG" 
out TypeReference typeref, bool canBeUnbound) {

#line  2457 "VBNET.ATG" 
		string name; typeref = null; 
		Qualident(
#line  2459 "VBNET.ATG" 
out name);

#line  2460 "VBNET.ATG" 
		typeref = new TypeReference(name); 
		if (
#line  2461 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			lexer.NextToken();
			Expect(155);
			if (
#line  2463 "VBNET.ATG" 
canBeUnbound && (la.kind == Tokens.CloseParenthesis || la.kind == Tokens.Comma)) {

#line  2464 "VBNET.ATG" 
				typeref.GenericTypes.Add(NullTypeReference.Instance); 
				while (la.kind == 12) {
					lexer.NextToken();

#line  2465 "VBNET.ATG" 
					typeref.GenericTypes.Add(NullTypeReference.Instance); 
				}
			} else if (StartOf(7)) {
				TypeArgumentList(
#line  2466 "VBNET.ATG" 
typeref.GenericTypes);
			} else SynErr(268);
			Expect(26);
		}
	}

	void RankList(
#line  2503 "VBNET.ATG" 
out int i) {

#line  2504 "VBNET.ATG" 
		i = 0; 
		while (la.kind == 12) {
			lexer.NextToken();

#line  2505 "VBNET.ATG" 
			++i; 
		}
	}

	void Attribute(
#line  2544 "VBNET.ATG" 
out ASTAttribute attribute) {

#line  2545 "VBNET.ATG" 
		string name;
		List<Expression> positional = new List<Expression>();
		List<NamedArgumentExpression> named = new List<NamedArgumentExpression>();
		
		if (la.kind == 117) {
			lexer.NextToken();
			Expect(16);
		}
		Qualident(
#line  2550 "VBNET.ATG" 
out name);
		if (la.kind == 25) {
			AttributeArguments(
#line  2551 "VBNET.ATG" 
positional, named);
		}

#line  2553 "VBNET.ATG" 
		attribute  = new ASTAttribute(name, positional, named);
		
	}

	void AttributeArguments(
#line  2558 "VBNET.ATG" 
List<Expression> positional, List<NamedArgumentExpression> named) {

#line  2560 "VBNET.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		
		Expect(25);
		if (
#line  2566 "VBNET.ATG" 
IsNotClosingParenthesis()) {
			if (
#line  2568 "VBNET.ATG" 
IsNamedAssign()) {

#line  2568 "VBNET.ATG" 
				nameFound = true; 
				IdentifierOrKeyword(
#line  2569 "VBNET.ATG" 
out name);
				if (la.kind == 11) {
					lexer.NextToken();
				}
				Expect(10);
			}
			Expr(
#line  2571 "VBNET.ATG" 
out expr);

#line  2573 "VBNET.ATG" 
			if (expr != null) {
			if (string.IsNullOrEmpty(name)) { positional.Add(expr); }
			else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			}
			
			while (la.kind == 12) {
				lexer.NextToken();
				if (
#line  2581 "VBNET.ATG" 
IsNamedAssign()) {

#line  2581 "VBNET.ATG" 
					nameFound = true; 
					IdentifierOrKeyword(
#line  2582 "VBNET.ATG" 
out name);
					if (la.kind == 11) {
						lexer.NextToken();
					}
					Expect(10);
				} else if (StartOf(29)) {

#line  2584 "VBNET.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(269);
				Expr(
#line  2585 "VBNET.ATG" 
out expr);

#line  2585 "VBNET.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				}
				
			}
		}
		Expect(26);
	}

	void FormalParameter(
#line  2642 "VBNET.ATG" 
out ParameterDeclarationExpression p) {

#line  2644 "VBNET.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		TypeReference type = null;
		ParamModifierList mod = new ParamModifierList(this);
		Expression expr = null;
		p = null;
		ArrayList arrayModifiers = null;
		
		while (la.kind == 28) {
			AttributeSection(
#line  2653 "VBNET.ATG" 
out section);

#line  2653 "VBNET.ATG" 
			attributes.Add(section); 
		}
		while (StartOf(35)) {
			ParameterModifier(
#line  2654 "VBNET.ATG" 
mod);
		}
		Identifier();

#line  2655 "VBNET.ATG" 
		string parameterName = t.val; 
		if (
#line  2656 "VBNET.ATG" 
IsDims()) {
			ArrayTypeModifiers(
#line  2656 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 50) {
			lexer.NextToken();
			TypeName(
#line  2657 "VBNET.ATG" 
out type);
		}

#line  2659 "VBNET.ATG" 
		if(type != null) {
		if (arrayModifiers != null) {
			if (type.RankSpecifier != null) {
				Error("array rank only allowed one time");
			} else {
				type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
			}
		}
		} else {
			type = new TypeReference("System.Object", arrayModifiers == null ? null : (int[])arrayModifiers.ToArray(typeof(int)));
		}
		
		if (la.kind == 10) {
			lexer.NextToken();
			Expr(
#line  2671 "VBNET.ATG" 
out expr);
		}

#line  2673 "VBNET.ATG" 
		mod.Check();
		p = new ParameterDeclarationExpression(type, parameterName, mod.Modifier, expr);
		p.Attributes = attributes;
		
	}

	void ParameterModifier(
#line  3355 "VBNET.ATG" 
ParamModifierList m) {
		if (la.kind == 59) {
			lexer.NextToken();

#line  3356 "VBNET.ATG" 
			m.Add(ParameterModifiers.In); 
		} else if (la.kind == 56) {
			lexer.NextToken();

#line  3357 "VBNET.ATG" 
			m.Add(ParameterModifiers.Ref); 
		} else if (la.kind == 160) {
			lexer.NextToken();

#line  3358 "VBNET.ATG" 
			m.Add(ParameterModifiers.Optional); 
		} else if (la.kind == 167) {
			lexer.NextToken();

#line  3359 "VBNET.ATG" 
			m.Add(ParameterModifiers.Params); 
		} else SynErr(270);
	}

	void Statement() {

#line  2702 "VBNET.ATG" 
		Statement stmt = null;
		Location startPos = la.Location;
		string label = String.Empty;
		
		
		if (la.kind == 1 || la.kind == 11) {
		} else if (
#line  2708 "VBNET.ATG" 
IsLabel()) {
			LabelName(
#line  2708 "VBNET.ATG" 
out label);

#line  2710 "VBNET.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val));
			
			Expect(11);
			Statement();
		} else if (StartOf(36)) {
			EmbeddedStatement(
#line  2713 "VBNET.ATG" 
out stmt);

#line  2713 "VBNET.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(271);

#line  2716 "VBNET.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startPos;
		stmt.EndLocation = t.Location;
		}
		
	}

	void LabelName(
#line  3131 "VBNET.ATG" 
out string name) {

#line  3133 "VBNET.ATG" 
		name = String.Empty;
		
		if (StartOf(14)) {
			Identifier();

#line  3135 "VBNET.ATG" 
			name = t.val; 
		} else if (la.kind == 5) {
			lexer.NextToken();

#line  3136 "VBNET.ATG" 
			name = t.val; 
		} else SynErr(272);
	}

	void EmbeddedStatement(
#line  2755 "VBNET.ATG" 
out Statement statement) {

#line  2757 "VBNET.ATG" 
		Statement embeddedStatement = null;
		statement = null;
		Expression expr = null;
		string name = String.Empty;
		List<Expression> p = null;
		
		if (la.kind == 107) {
			lexer.NextToken();

#line  2763 "VBNET.ATG" 
			ExitType exitType = ExitType.None; 
			switch (la.kind) {
			case 195: {
				lexer.NextToken();

#line  2765 "VBNET.ATG" 
				exitType = ExitType.Sub; 
				break;
			}
			case 114: {
				lexer.NextToken();

#line  2767 "VBNET.ATG" 
				exitType = ExitType.Function; 
				break;
			}
			case 171: {
				lexer.NextToken();

#line  2769 "VBNET.ATG" 
				exitType = ExitType.Property; 
				break;
			}
			case 95: {
				lexer.NextToken();

#line  2771 "VBNET.ATG" 
				exitType = ExitType.Do; 
				break;
			}
			case 111: {
				lexer.NextToken();

#line  2773 "VBNET.ATG" 
				exitType = ExitType.For; 
				break;
			}
			case 203: {
				lexer.NextToken();

#line  2775 "VBNET.ATG" 
				exitType = ExitType.Try; 
				break;
			}
			case 216: {
				lexer.NextToken();

#line  2777 "VBNET.ATG" 
				exitType = ExitType.While; 
				break;
			}
			case 182: {
				lexer.NextToken();

#line  2779 "VBNET.ATG" 
				exitType = ExitType.Select; 
				break;
			}
			default: SynErr(273); break;
			}

#line  2781 "VBNET.ATG" 
			statement = new ExitStatement(exitType); 
		} else if (la.kind == 203) {
			TryStatement(
#line  2782 "VBNET.ATG" 
out statement);
		} else if (la.kind == 76) {
			lexer.NextToken();

#line  2783 "VBNET.ATG" 
			ContinueType continueType = ContinueType.None; 
			if (la.kind == 95 || la.kind == 111 || la.kind == 216) {
				if (la.kind == 95) {
					lexer.NextToken();

#line  2783 "VBNET.ATG" 
					continueType = ContinueType.Do; 
				} else if (la.kind == 111) {
					lexer.NextToken();

#line  2783 "VBNET.ATG" 
					continueType = ContinueType.For; 
				} else {
					lexer.NextToken();

#line  2783 "VBNET.ATG" 
					continueType = ContinueType.While; 
				}
			}

#line  2783 "VBNET.ATG" 
			statement = new ContinueStatement(continueType); 
		} else if (la.kind == 200) {
			lexer.NextToken();
			if (StartOf(29)) {
				Expr(
#line  2785 "VBNET.ATG" 
out expr);
			}

#line  2785 "VBNET.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (la.kind == 180) {
			lexer.NextToken();
			if (StartOf(29)) {
				Expr(
#line  2787 "VBNET.ATG" 
out expr);
			}

#line  2787 "VBNET.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 196) {
			lexer.NextToken();
			Expr(
#line  2789 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  2789 "VBNET.ATG" 
out embeddedStatement);
			Expect(100);
			Expect(196);

#line  2790 "VBNET.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 174) {
			lexer.NextToken();
			Identifier();

#line  2792 "VBNET.ATG" 
			name = t.val; 
			if (la.kind == 25) {
				lexer.NextToken();
				if (StartOf(37)) {
					ArgumentList(
#line  2793 "VBNET.ATG" 
out p);
				}
				Expect(26);
			}

#line  2795 "VBNET.ATG" 
			statement = new RaiseEventStatement(name, p);
			
		} else if (la.kind == 218) {
			WithStatement(
#line  2798 "VBNET.ATG" 
out statement);
		} else if (la.kind == 43) {
			lexer.NextToken();

#line  2800 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  2801 "VBNET.ATG" 
out expr);
			Expect(12);
			Expr(
#line  2801 "VBNET.ATG" 
out handlerExpr);

#line  2803 "VBNET.ATG" 
			statement = new AddHandlerStatement(expr, handlerExpr);
			
		} else if (la.kind == 178) {
			lexer.NextToken();

#line  2806 "VBNET.ATG" 
			Expression handlerExpr = null; 
			Expr(
#line  2807 "VBNET.ATG" 
out expr);
			Expect(12);
			Expr(
#line  2807 "VBNET.ATG" 
out handlerExpr);

#line  2809 "VBNET.ATG" 
			statement = new RemoveHandlerStatement(expr, handlerExpr);
			
		} else if (la.kind == 216) {
			lexer.NextToken();
			Expr(
#line  2812 "VBNET.ATG" 
out expr);
			EndOfStmt();
			Block(
#line  2813 "VBNET.ATG" 
out embeddedStatement);
			Expect(100);
			Expect(216);

#line  2815 "VBNET.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
			
		} else if (la.kind == 95) {
			lexer.NextToken();

#line  2820 "VBNET.ATG" 
			ConditionType conditionType = ConditionType.None;
			
			if (la.kind == 209 || la.kind == 216) {
				WhileOrUntil(
#line  2823 "VBNET.ATG" 
out conditionType);
				Expr(
#line  2823 "VBNET.ATG" 
out expr);
				EndOfStmt();
				Block(
#line  2824 "VBNET.ATG" 
out embeddedStatement);
				Expect(138);

#line  2827 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, 
				                               embeddedStatement, 
				                               conditionType == ConditionType.While ? ConditionType.DoWhile : conditionType, 
				                               ConditionPosition.Start);
				
			} else if (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
				Block(
#line  2834 "VBNET.ATG" 
out embeddedStatement);
				Expect(138);
				if (la.kind == 209 || la.kind == 216) {
					WhileOrUntil(
#line  2835 "VBNET.ATG" 
out conditionType);
					Expr(
#line  2835 "VBNET.ATG" 
out expr);
				}

#line  2837 "VBNET.ATG" 
				statement = new DoLoopStatement(expr, embeddedStatement, conditionType, ConditionPosition.End);
				
			} else SynErr(274);
		} else if (la.kind == 111) {
			lexer.NextToken();

#line  2842 "VBNET.ATG" 
			Expression group = null;
			TypeReference typeReference;
			string        typeName;
			Location startLocation = t.Location;
			
			if (la.kind == 97) {
				lexer.NextToken();
				LoopControlVariable(
#line  2849 "VBNET.ATG" 
out typeReference, out typeName);
				Expect(125);
				Expr(
#line  2850 "VBNET.ATG" 
out group);
				EndOfStmt();
				Block(
#line  2851 "VBNET.ATG" 
out embeddedStatement);
				Expect(149);
				if (StartOf(29)) {
					Expr(
#line  2852 "VBNET.ATG" 
out expr);
				}

#line  2854 "VBNET.ATG" 
				statement = new ForeachStatement(typeReference, 
				                                typeName,
				                                group, 
				                                embeddedStatement, 
				                                expr);
				statement.StartLocation = startLocation;
				statement.EndLocation   = t.EndLocation;
				
				
			} else if (StartOf(38)) {

#line  2865 "VBNET.ATG" 
				Expression start = null;
				Expression end = null;
				Expression step = null;
				Expression variableExpr = null;
				Expression nextExpr = null;
				List<Expression> nextExpressions = null;
				
				if (
#line  2872 "VBNET.ATG" 
IsLoopVariableDeclaration()) {
					LoopControlVariable(
#line  2873 "VBNET.ATG" 
out typeReference, out typeName);
				} else {

#line  2875 "VBNET.ATG" 
					typeReference = null; typeName = null; 
					SimpleExpr(
#line  2876 "VBNET.ATG" 
out variableExpr);
				}
				Expect(10);
				Expr(
#line  2878 "VBNET.ATG" 
out start);
				Expect(201);
				Expr(
#line  2878 "VBNET.ATG" 
out end);
				if (la.kind == 190) {
					lexer.NextToken();
					Expr(
#line  2878 "VBNET.ATG" 
out step);
				}
				EndOfStmt();
				Block(
#line  2879 "VBNET.ATG" 
out embeddedStatement);
				Expect(149);
				if (StartOf(29)) {
					Expr(
#line  2882 "VBNET.ATG" 
out nextExpr);

#line  2884 "VBNET.ATG" 
					nextExpressions = new List<Expression>();
					nextExpressions.Add(nextExpr);
					
					while (la.kind == 12) {
						lexer.NextToken();
						Expr(
#line  2887 "VBNET.ATG" 
out nextExpr);

#line  2887 "VBNET.ATG" 
						nextExpressions.Add(nextExpr); 
					}
				}

#line  2890 "VBNET.ATG" 
				statement = new ForNextStatement {
				TypeReference = typeReference,
				VariableName = typeName, 
				LoopVariableExpression = variableExpr,
				Start = start, 
				End = end, 
				Step = step, 
				EmbeddedStatement = embeddedStatement, 
				NextExpressions = nextExpressions
				};
				
			} else SynErr(275);
		} else if (la.kind == 105) {
			lexer.NextToken();
			Expr(
#line  2903 "VBNET.ATG" 
out expr);

#line  2903 "VBNET.ATG" 
			statement = new ErrorStatement(expr); 
		} else if (la.kind == 176) {
			lexer.NextToken();

#line  2905 "VBNET.ATG" 
			bool isPreserve = false; 
			if (la.kind == 169) {
				lexer.NextToken();

#line  2905 "VBNET.ATG" 
				isPreserve = true; 
			}
			ReDimClause(
#line  2906 "VBNET.ATG" 
out expr);

#line  2908 "VBNET.ATG" 
			ReDimStatement reDimStatement = new ReDimStatement(isPreserve);
			statement = reDimStatement;
			SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression);
			
			while (la.kind == 12) {
				lexer.NextToken();
				ReDimClause(
#line  2912 "VBNET.ATG" 
out expr);

#line  2913 "VBNET.ATG" 
				SafeAdd(reDimStatement, reDimStatement.ReDimClauses, expr as InvocationExpression); 
			}
		} else if (la.kind == 104) {
			lexer.NextToken();
			Expr(
#line  2917 "VBNET.ATG" 
out expr);

#line  2919 "VBNET.ATG" 
			EraseStatement eraseStatement = new EraseStatement();
			if (expr != null) { SafeAdd(eraseStatement, eraseStatement.Expressions, expr);}
			
			while (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  2922 "VBNET.ATG" 
out expr);

#line  2922 "VBNET.ATG" 
				if (expr != null) { SafeAdd(eraseStatement, eraseStatement.Expressions, expr); }
			}

#line  2923 "VBNET.ATG" 
			statement = eraseStatement; 
		} else if (la.kind == 191) {
			lexer.NextToken();

#line  2925 "VBNET.ATG" 
			statement = new StopStatement(); 
		} else if (
#line  2927 "VBNET.ATG" 
la.kind == Tokens.If) {
			Expect(122);

#line  2928 "VBNET.ATG" 
			Location ifStartLocation = t.Location; 
			Expr(
#line  2928 "VBNET.ATG" 
out expr);
			if (la.kind == 199) {
				lexer.NextToken();
			}
			if (la.kind == 1 || la.kind == 11) {
				EndOfStmt();
				Block(
#line  2931 "VBNET.ATG" 
out embeddedStatement);

#line  2933 "VBNET.ATG" 
				IfElseStatement ifStatement = new IfElseStatement(expr, embeddedStatement);
				ifStatement.StartLocation = ifStartLocation;
				Location elseIfStart;
				
				while (la.kind == 99 || 
#line  2939 "VBNET.ATG" 
IsElseIf()) {
					if (
#line  2939 "VBNET.ATG" 
IsElseIf()) {
						Expect(98);

#line  2939 "VBNET.ATG" 
						elseIfStart = t.Location; 
						Expect(122);
					} else {
						lexer.NextToken();

#line  2940 "VBNET.ATG" 
						elseIfStart = t.Location; 
					}

#line  2942 "VBNET.ATG" 
					Expression condition = null; Statement block = null; 
					Expr(
#line  2943 "VBNET.ATG" 
out condition);
					if (la.kind == 199) {
						lexer.NextToken();
					}
					EndOfStmt();
					Block(
#line  2944 "VBNET.ATG" 
out block);

#line  2946 "VBNET.ATG" 
					ElseIfSection elseIfSection = new ElseIfSection(condition, block);
					elseIfSection.StartLocation = elseIfStart;
					elseIfSection.EndLocation = t.Location;
					elseIfSection.Parent = ifStatement;
					ifStatement.ElseIfSections.Add(elseIfSection);
					
				}
				if (la.kind == 98) {
					lexer.NextToken();
					EndOfStmt();
					Block(
#line  2955 "VBNET.ATG" 
out embeddedStatement);

#line  2957 "VBNET.ATG" 
					ifStatement.FalseStatement.Add(embeddedStatement);
					
				}
				Expect(100);
				Expect(122);

#line  2961 "VBNET.ATG" 
				ifStatement.EndLocation = t.Location;
				statement = ifStatement;
				
			} else if (StartOf(39)) {

#line  2966 "VBNET.ATG" 
				IfElseStatement ifStatement = new IfElseStatement(expr);
				ifStatement.StartLocation = ifStartLocation;
				
				SingleLineStatementList(
#line  2969 "VBNET.ATG" 
ifStatement.TrueStatement);
				if (la.kind == 98) {
					lexer.NextToken();
					if (StartOf(39)) {
						SingleLineStatementList(
#line  2972 "VBNET.ATG" 
ifStatement.FalseStatement);
					}
				}

#line  2974 "VBNET.ATG" 
				ifStatement.EndLocation = t.Location; statement = ifStatement; 
			} else SynErr(276);
		} else if (la.kind == 182) {
			lexer.NextToken();
			if (la.kind == 61) {
				lexer.NextToken();
			}
			Expr(
#line  2977 "VBNET.ATG" 
out expr);
			EndOfStmt();

#line  2978 "VBNET.ATG" 
			List<SwitchSection> selectSections = new List<SwitchSection>();
			Statement block = null;
			
			while (la.kind == 61) {

#line  2982 "VBNET.ATG" 
				List<CaseLabel> caseClauses = null; Location caseLocation = la.Location; 
				lexer.NextToken();
				CaseClauses(
#line  2983 "VBNET.ATG" 
out caseClauses);
				if (
#line  2983 "VBNET.ATG" 
IsNotStatementSeparator()) {
					lexer.NextToken();
				}
				EndOfStmt();

#line  2985 "VBNET.ATG" 
				SwitchSection selectSection = new SwitchSection(caseClauses);
				selectSection.StartLocation = caseLocation;
				
				Block(
#line  2988 "VBNET.ATG" 
out block);

#line  2990 "VBNET.ATG" 
				selectSection.Children = block.Children;
				selectSection.EndLocation = t.EndLocation;
				selectSections.Add(selectSection);
				
			}

#line  2996 "VBNET.ATG" 
			statement = new SwitchStatement(expr, selectSections);
			
			Expect(100);
			Expect(182);
		} else if (la.kind == 157) {

#line  2999 "VBNET.ATG" 
			OnErrorStatement onErrorStatement = null; 
			OnErrorStatement(
#line  3000 "VBNET.ATG" 
out onErrorStatement);

#line  3000 "VBNET.ATG" 
			statement = onErrorStatement; 
		} else if (la.kind == 119) {

#line  3001 "VBNET.ATG" 
			GotoStatement goToStatement = null; 
			GotoStatement(
#line  3002 "VBNET.ATG" 
out goToStatement);

#line  3002 "VBNET.ATG" 
			statement = goToStatement; 
		} else if (la.kind == 179) {

#line  3003 "VBNET.ATG" 
			ResumeStatement resumeStatement = null; 
			ResumeStatement(
#line  3004 "VBNET.ATG" 
out resumeStatement);

#line  3004 "VBNET.ATG" 
			statement = resumeStatement; 
		} else if (StartOf(38)) {

#line  3007 "VBNET.ATG" 
			Expression val = null;
			AssignmentOperatorType op;
			
			bool mustBeAssignment = la.kind == Tokens.Plus  || la.kind == Tokens.Minus ||
			                        la.kind == Tokens.Not   || la.kind == Tokens.Times;
			
			SimpleExpr(
#line  3013 "VBNET.ATG" 
out expr);
			if (StartOf(40)) {
				AssignmentOperator(
#line  3015 "VBNET.ATG" 
out op);
				Expr(
#line  3015 "VBNET.ATG" 
out val);

#line  3015 "VBNET.ATG" 
				expr = new AssignmentExpression(expr, op, val); 
			} else if (la.kind == 1 || la.kind == 11 || la.kind == 98) {

#line  3016 "VBNET.ATG" 
				if (mustBeAssignment) Error("error in assignment."); 
			} else SynErr(277);

#line  3019 "VBNET.ATG" 
			// a field reference expression that stands alone is a
			// invocation expression without parantheses and arguments
			if(expr is MemberReferenceExpression || expr is IdentifierExpression) {
				expr = new InvocationExpression(expr);
			}
			statement = new ExpressionStatement(expr);
			
		} else if (la.kind == 60) {
			lexer.NextToken();
			SimpleExpr(
#line  3026 "VBNET.ATG" 
out expr);

#line  3026 "VBNET.ATG" 
			statement = new ExpressionStatement(expr); 
		} else if (la.kind == 211) {
			lexer.NextToken();

#line  3028 "VBNET.ATG" 
			Statement block;  
			if (
#line  3029 "VBNET.ATG" 
Peek(1).kind == Tokens.As) {

#line  3030 "VBNET.ATG" 
				LocalVariableDeclaration resourceAquisition = new LocalVariableDeclaration(Modifiers.None); 
				VariableDeclarator(
#line  3031 "VBNET.ATG" 
resourceAquisition.Variables);
				while (la.kind == 12) {
					lexer.NextToken();
					VariableDeclarator(
#line  3033 "VBNET.ATG" 
resourceAquisition.Variables);
				}
				Block(
#line  3035 "VBNET.ATG" 
out block);

#line  3037 "VBNET.ATG" 
				statement = new UsingStatement(resourceAquisition, block);
				
			} else if (StartOf(29)) {
				Expr(
#line  3039 "VBNET.ATG" 
out expr);
				Block(
#line  3040 "VBNET.ATG" 
out block);

#line  3041 "VBNET.ATG" 
				statement = new UsingStatement(new ExpressionStatement(expr), block); 
			} else SynErr(278);
			Expect(100);
			Expect(211);
		} else if (StartOf(41)) {
			LocalDeclarationStatement(
#line  3044 "VBNET.ATG" 
out statement);
		} else SynErr(279);
	}

	void LocalDeclarationStatement(
#line  2724 "VBNET.ATG" 
out Statement statement) {

#line  2726 "VBNET.ATG" 
		ModifierList m = new ModifierList();
		LocalVariableDeclaration localVariableDeclaration;
		bool dimfound = false;
		
		while (la.kind == 75 || la.kind == 92 || la.kind == 189) {
			if (la.kind == 75) {
				lexer.NextToken();

#line  2732 "VBNET.ATG" 
				m.Add(Modifiers.Const, t.Location); 
			} else if (la.kind == 189) {
				lexer.NextToken();

#line  2733 "VBNET.ATG" 
				m.Add(Modifiers.Static, t.Location); 
			} else {
				lexer.NextToken();

#line  2734 "VBNET.ATG" 
				dimfound = true; 
			}
		}

#line  2737 "VBNET.ATG" 
		if(dimfound && (m.Modifier & Modifiers.Const) != 0) {
		Error("Dim is not allowed on constants.");
		}
		
		if(m.isNone && dimfound == false) {
			Error("Const, Dim or Static expected");
		}
		
		localVariableDeclaration = new LocalVariableDeclaration(m.Modifier);
		localVariableDeclaration.StartLocation = t.Location;
		
		VariableDeclarator(
#line  2748 "VBNET.ATG" 
localVariableDeclaration.Variables);
		while (la.kind == 12) {
			lexer.NextToken();
			VariableDeclarator(
#line  2749 "VBNET.ATG" 
localVariableDeclaration.Variables);
		}

#line  2751 "VBNET.ATG" 
		statement = localVariableDeclaration;
		
	}

	void TryStatement(
#line  3245 "VBNET.ATG" 
out Statement tryStatement) {

#line  3247 "VBNET.ATG" 
		Statement blockStmt = null, finallyStmt = null;List<CatchClause> catchClauses = null;
		
		Expect(203);
		EndOfStmt();
		Block(
#line  3250 "VBNET.ATG" 
out blockStmt);
		if (la.kind == 62 || la.kind == 100 || la.kind == 110) {
			CatchClauses(
#line  3251 "VBNET.ATG" 
out catchClauses);
		}
		if (la.kind == 110) {
			lexer.NextToken();
			EndOfStmt();
			Block(
#line  3252 "VBNET.ATG" 
out finallyStmt);
		}
		Expect(100);
		Expect(203);

#line  3255 "VBNET.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		
	}

	void WithStatement(
#line  3225 "VBNET.ATG" 
out Statement withStatement) {

#line  3227 "VBNET.ATG" 
		Statement blockStmt = null;
		Expression expr = null;
		
		Expect(218);

#line  3230 "VBNET.ATG" 
		Location start = t.Location; 
		Expr(
#line  3231 "VBNET.ATG" 
out expr);
		EndOfStmt();

#line  3233 "VBNET.ATG" 
		withStatement = new WithStatement(expr);
		withStatement.StartLocation = start;
		
		Block(
#line  3236 "VBNET.ATG" 
out blockStmt);

#line  3238 "VBNET.ATG" 
		((WithStatement)withStatement).Body = (BlockStatement)blockStmt;
		
		Expect(100);
		Expect(218);

#line  3241 "VBNET.ATG" 
		withStatement.EndLocation = t.Location; 
	}

	void WhileOrUntil(
#line  3218 "VBNET.ATG" 
out ConditionType conditionType) {

#line  3219 "VBNET.ATG" 
		conditionType = ConditionType.None; 
		if (la.kind == 216) {
			lexer.NextToken();

#line  3220 "VBNET.ATG" 
			conditionType = ConditionType.While; 
		} else if (la.kind == 209) {
			lexer.NextToken();

#line  3221 "VBNET.ATG" 
			conditionType = ConditionType.Until; 
		} else SynErr(280);
	}

	void LoopControlVariable(
#line  3061 "VBNET.ATG" 
out TypeReference type, out string name) {

#line  3062 "VBNET.ATG" 
		ArrayList arrayModifiers = null;
		type = null;
		
		Qualident(
#line  3066 "VBNET.ATG" 
out name);
		if (
#line  3067 "VBNET.ATG" 
IsDims()) {
			ArrayTypeModifiers(
#line  3067 "VBNET.ATG" 
out arrayModifiers);
		}
		if (la.kind == 50) {
			lexer.NextToken();
			TypeName(
#line  3068 "VBNET.ATG" 
out type);

#line  3068 "VBNET.ATG" 
			if (name.IndexOf('.') > 0) { Error("No type def for 'for each' member indexer allowed."); } 
		}

#line  3070 "VBNET.ATG" 
		if (type != null) {
		if(type.RankSpecifier != null && arrayModifiers != null) {
			Error("array rank only allowed one time");
		} else if (arrayModifiers != null) {
			type.RankSpecifier = (int[])arrayModifiers.ToArray(typeof(int));
		}
		}
		
	}

	void ReDimClause(
#line  3140 "VBNET.ATG" 
out Expression expr) {
		SimpleNonInvocationExpression(
#line  3142 "VBNET.ATG" 
out expr);
		ReDimClauseInternal(
#line  3143 "VBNET.ATG" 
ref expr);
	}

	void SingleLineStatementList(
#line  3047 "VBNET.ATG" 
List<Statement> list) {

#line  3048 "VBNET.ATG" 
		Statement embeddedStatement = null; 
		if (la.kind == 100) {
			lexer.NextToken();

#line  3050 "VBNET.ATG" 
			embeddedStatement = new EndStatement(); 
		} else if (StartOf(36)) {
			EmbeddedStatement(
#line  3051 "VBNET.ATG" 
out embeddedStatement);
		} else SynErr(281);

#line  3052 "VBNET.ATG" 
		if (embeddedStatement != null) list.Add(embeddedStatement); 
		while (la.kind == 11) {
			lexer.NextToken();
			while (la.kind == 11) {
				lexer.NextToken();
			}
			if (la.kind == 100) {
				lexer.NextToken();

#line  3054 "VBNET.ATG" 
				embeddedStatement = new EndStatement(); 
			} else if (StartOf(36)) {
				EmbeddedStatement(
#line  3055 "VBNET.ATG" 
out embeddedStatement);
			} else SynErr(282);

#line  3056 "VBNET.ATG" 
			if (embeddedStatement != null) list.Add(embeddedStatement); 
		}
	}

	void CaseClauses(
#line  3178 "VBNET.ATG" 
out List<CaseLabel> caseClauses) {

#line  3180 "VBNET.ATG" 
		caseClauses = new List<CaseLabel>();
		CaseLabel caseClause = null;
		
		CaseClause(
#line  3183 "VBNET.ATG" 
out caseClause);

#line  3183 "VBNET.ATG" 
		if (caseClause != null) { caseClauses.Add(caseClause); } 
		while (la.kind == 12) {
			lexer.NextToken();
			CaseClause(
#line  3184 "VBNET.ATG" 
out caseClause);

#line  3184 "VBNET.ATG" 
			if (caseClause != null) { caseClauses.Add(caseClause); } 
		}
	}

	void OnErrorStatement(
#line  3081 "VBNET.ATG" 
out OnErrorStatement stmt) {

#line  3083 "VBNET.ATG" 
		stmt = null;
		GotoStatement goToStatement = null;
		
		Expect(157);
		Expect(105);
		if (
#line  3089 "VBNET.ATG" 
IsNegativeLabelName()) {
			Expect(119);
			Expect(18);
			Expect(5);

#line  3091 "VBNET.ATG" 
			long intLabel = Int64.Parse(t.val);
			if(intLabel != 1) {
				Error("invalid label in on error statement.");
			}
			stmt = new OnErrorStatement(new GotoStatement((intLabel * -1).ToString()));
			
		} else if (la.kind == 119) {
			GotoStatement(
#line  3097 "VBNET.ATG" 
out goToStatement);

#line  3099 "VBNET.ATG" 
			string val = goToStatement.Label;
			
			// if value is numeric, make sure that is 0
			try {
				long intLabel = Int64.Parse(val);
				if(intLabel != 0) {
					Error("invalid label in on error statement.");
				}
			} catch {
			}
			stmt = new OnErrorStatement(goToStatement);
			
		} else if (la.kind == 179) {
			lexer.NextToken();
			Expect(149);

#line  3113 "VBNET.ATG" 
			stmt = new OnErrorStatement(new ResumeStatement(true));
			
		} else SynErr(283);
	}

	void GotoStatement(
#line  3119 "VBNET.ATG" 
out GotoStatement goToStatement) {

#line  3121 "VBNET.ATG" 
		string label = String.Empty;
		
		Expect(119);
		LabelName(
#line  3124 "VBNET.ATG" 
out label);

#line  3126 "VBNET.ATG" 
		goToStatement = new GotoStatement(label);
		
	}

	void ResumeStatement(
#line  3167 "VBNET.ATG" 
out ResumeStatement resumeStatement) {

#line  3169 "VBNET.ATG" 
		resumeStatement = null;
		string label = String.Empty;
		
		if (
#line  3172 "VBNET.ATG" 
IsResumeNext()) {
			Expect(179);
			Expect(149);

#line  3173 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(true); 
		} else if (la.kind == 179) {
			lexer.NextToken();
			if (StartOf(42)) {
				LabelName(
#line  3174 "VBNET.ATG" 
out label);
			}

#line  3174 "VBNET.ATG" 
			resumeStatement = new ResumeStatement(label); 
		} else SynErr(284);
	}

	void ReDimClauseInternal(
#line  3146 "VBNET.ATG" 
ref Expression expr) {

#line  3147 "VBNET.ATG" 
		List<Expression> arguments; bool canBeNormal; bool canBeRedim; string name; 
		while (la.kind == 16 || 
#line  3150 "VBNET.ATG" 
la.kind == Tokens.OpenParenthesis && Peek(1).kind == Tokens.Of) {
			if (la.kind == 16) {
				lexer.NextToken();
				IdentifierOrKeyword(
#line  3149 "VBNET.ATG" 
out name);

#line  3149 "VBNET.ATG" 
				expr = new MemberReferenceExpression(expr, name); 
			} else {
				InvocationExpression(
#line  3151 "VBNET.ATG" 
ref expr);
			}
		}
		Expect(25);
		NormalOrReDimArgumentList(
#line  3154 "VBNET.ATG" 
out arguments, out canBeNormal, out canBeRedim);
		Expect(26);

#line  3156 "VBNET.ATG" 
		expr = new InvocationExpression(expr, arguments);
		if (canBeRedim == false || canBeNormal && (la.kind == Tokens.Dot || la.kind == Tokens.OpenParenthesis)) {
			if (this.Errors.Count == 0) {
				// don't recurse on parse errors - could result in endless recursion
				ReDimClauseInternal(ref expr);
			}
		}
		
	}

	void CaseClause(
#line  3188 "VBNET.ATG" 
out CaseLabel caseClause) {

#line  3190 "VBNET.ATG" 
		Expression expr = null;
		Expression sexpr = null;
		BinaryOperatorType op = BinaryOperatorType.None;
		caseClause = null;
		
		if (la.kind == 98) {
			lexer.NextToken();

#line  3196 "VBNET.ATG" 
			caseClause = new CaseLabel(); 
		} else if (StartOf(43)) {
			if (la.kind == 131) {
				lexer.NextToken();
			}
			switch (la.kind) {
			case 28: {
				lexer.NextToken();

#line  3200 "VBNET.ATG" 
				op = BinaryOperatorType.LessThan; 
				break;
			}
			case 27: {
				lexer.NextToken();

#line  3201 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThan; 
				break;
			}
			case 31: {
				lexer.NextToken();

#line  3202 "VBNET.ATG" 
				op = BinaryOperatorType.LessThanOrEqual; 
				break;
			}
			case 30: {
				lexer.NextToken();

#line  3203 "VBNET.ATG" 
				op = BinaryOperatorType.GreaterThanOrEqual; 
				break;
			}
			case 10: {
				lexer.NextToken();

#line  3204 "VBNET.ATG" 
				op = BinaryOperatorType.Equality; 
				break;
			}
			case 29: {
				lexer.NextToken();

#line  3205 "VBNET.ATG" 
				op = BinaryOperatorType.InEquality; 
				break;
			}
			default: SynErr(285); break;
			}
			Expr(
#line  3207 "VBNET.ATG" 
out expr);

#line  3209 "VBNET.ATG" 
			caseClause = new CaseLabel(op, expr);
			
		} else if (StartOf(29)) {
			Expr(
#line  3211 "VBNET.ATG" 
out expr);
			if (la.kind == 201) {
				lexer.NextToken();
				Expr(
#line  3211 "VBNET.ATG" 
out sexpr);
			}

#line  3213 "VBNET.ATG" 
			caseClause = new CaseLabel(expr, sexpr);
			
		} else SynErr(286);
	}

	void CatchClauses(
#line  3260 "VBNET.ATG" 
out List<CatchClause> catchClauses) {

#line  3262 "VBNET.ATG" 
		catchClauses = new List<CatchClause>();
		TypeReference type = null;
		Statement blockStmt = null;
		Expression expr = null;
		string name = String.Empty;
		
		while (la.kind == 62) {
			lexer.NextToken();
			if (StartOf(14)) {
				Identifier();

#line  3270 "VBNET.ATG" 
				name = t.val; 
				if (la.kind == 50) {
					lexer.NextToken();
					TypeName(
#line  3270 "VBNET.ATG" 
out type);
				}
			}
			if (la.kind == 214) {
				lexer.NextToken();
				Expr(
#line  3271 "VBNET.ATG" 
out expr);
			}
			EndOfStmt();
			Block(
#line  3273 "VBNET.ATG" 
out blockStmt);

#line  3274 "VBNET.ATG" 
			catchClauses.Add(new CatchClause(type, name, blockStmt, expr)); 
		}
	}


	
	void ParseRoot()
	{
		VBNET();

	}
	
	protected override void SynErr(int line, int col, int errorNumber)
	{
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
			case 10: s = "\"=\" expected"; break;
			case 11: s = "\":\" expected"; break;
			case 12: s = "\",\" expected"; break;
			case 13: s = "\"&\" expected"; break;
			case 14: s = "\"/\" expected"; break;
			case 15: s = "\"\\\\\" expected"; break;
			case 16: s = "\".\" expected"; break;
			case 17: s = "\"!\" expected"; break;
			case 18: s = "\"-\" expected"; break;
			case 19: s = "\"+\" expected"; break;
			case 20: s = "\"^\" expected"; break;
			case 21: s = "\"?\" expected"; break;
			case 22: s = "\"*\" expected"; break;
			case 23: s = "\"{\" expected"; break;
			case 24: s = "\"}\" expected"; break;
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
			case 45: s = "\"Aggregate\" expected"; break;
			case 46: s = "\"Alias\" expected"; break;
			case 47: s = "\"And\" expected"; break;
			case 48: s = "\"AndAlso\" expected"; break;
			case 49: s = "\"Ansi\" expected"; break;
			case 50: s = "\"As\" expected"; break;
			case 51: s = "\"Ascending\" expected"; break;
			case 52: s = "\"Assembly\" expected"; break;
			case 53: s = "\"Auto\" expected"; break;
			case 54: s = "\"Binary\" expected"; break;
			case 55: s = "\"Boolean\" expected"; break;
			case 56: s = "\"ByRef\" expected"; break;
			case 57: s = "\"By\" expected"; break;
			case 58: s = "\"Byte\" expected"; break;
			case 59: s = "\"ByVal\" expected"; break;
			case 60: s = "\"Call\" expected"; break;
			case 61: s = "\"Case\" expected"; break;
			case 62: s = "\"Catch\" expected"; break;
			case 63: s = "\"CBool\" expected"; break;
			case 64: s = "\"CByte\" expected"; break;
			case 65: s = "\"CChar\" expected"; break;
			case 66: s = "\"CDate\" expected"; break;
			case 67: s = "\"CDbl\" expected"; break;
			case 68: s = "\"CDec\" expected"; break;
			case 69: s = "\"Char\" expected"; break;
			case 70: s = "\"CInt\" expected"; break;
			case 71: s = "\"Class\" expected"; break;
			case 72: s = "\"CLng\" expected"; break;
			case 73: s = "\"CObj\" expected"; break;
			case 74: s = "\"Compare\" expected"; break;
			case 75: s = "\"Const\" expected"; break;
			case 76: s = "\"Continue\" expected"; break;
			case 77: s = "\"CSByte\" expected"; break;
			case 78: s = "\"CShort\" expected"; break;
			case 79: s = "\"CSng\" expected"; break;
			case 80: s = "\"CStr\" expected"; break;
			case 81: s = "\"CType\" expected"; break;
			case 82: s = "\"CUInt\" expected"; break;
			case 83: s = "\"CULng\" expected"; break;
			case 84: s = "\"CUShort\" expected"; break;
			case 85: s = "\"Custom\" expected"; break;
			case 86: s = "\"Date\" expected"; break;
			case 87: s = "\"Decimal\" expected"; break;
			case 88: s = "\"Declare\" expected"; break;
			case 89: s = "\"Default\" expected"; break;
			case 90: s = "\"Delegate\" expected"; break;
			case 91: s = "\"Descending\" expected"; break;
			case 92: s = "\"Dim\" expected"; break;
			case 93: s = "\"DirectCast\" expected"; break;
			case 94: s = "\"Distinct\" expected"; break;
			case 95: s = "\"Do\" expected"; break;
			case 96: s = "\"Double\" expected"; break;
			case 97: s = "\"Each\" expected"; break;
			case 98: s = "\"Else\" expected"; break;
			case 99: s = "\"ElseIf\" expected"; break;
			case 100: s = "\"End\" expected"; break;
			case 101: s = "\"EndIf\" expected"; break;
			case 102: s = "\"Enum\" expected"; break;
			case 103: s = "\"Equals\" expected"; break;
			case 104: s = "\"Erase\" expected"; break;
			case 105: s = "\"Error\" expected"; break;
			case 106: s = "\"Event\" expected"; break;
			case 107: s = "\"Exit\" expected"; break;
			case 108: s = "\"Explicit\" expected"; break;
			case 109: s = "\"False\" expected"; break;
			case 110: s = "\"Finally\" expected"; break;
			case 111: s = "\"For\" expected"; break;
			case 112: s = "\"Friend\" expected"; break;
			case 113: s = "\"From\" expected"; break;
			case 114: s = "\"Function\" expected"; break;
			case 115: s = "\"Get\" expected"; break;
			case 116: s = "\"GetType\" expected"; break;
			case 117: s = "\"Global\" expected"; break;
			case 118: s = "\"GoSub\" expected"; break;
			case 119: s = "\"GoTo\" expected"; break;
			case 120: s = "\"Group\" expected"; break;
			case 121: s = "\"Handles\" expected"; break;
			case 122: s = "\"If\" expected"; break;
			case 123: s = "\"Implements\" expected"; break;
			case 124: s = "\"Imports\" expected"; break;
			case 125: s = "\"In\" expected"; break;
			case 126: s = "\"Infer\" expected"; break;
			case 127: s = "\"Inherits\" expected"; break;
			case 128: s = "\"Integer\" expected"; break;
			case 129: s = "\"Interface\" expected"; break;
			case 130: s = "\"Into\" expected"; break;
			case 131: s = "\"Is\" expected"; break;
			case 132: s = "\"IsNot\" expected"; break;
			case 133: s = "\"Join\" expected"; break;
			case 134: s = "\"Let\" expected"; break;
			case 135: s = "\"Lib\" expected"; break;
			case 136: s = "\"Like\" expected"; break;
			case 137: s = "\"Long\" expected"; break;
			case 138: s = "\"Loop\" expected"; break;
			case 139: s = "\"Me\" expected"; break;
			case 140: s = "\"Mod\" expected"; break;
			case 141: s = "\"Module\" expected"; break;
			case 142: s = "\"MustInherit\" expected"; break;
			case 143: s = "\"MustOverride\" expected"; break;
			case 144: s = "\"MyBase\" expected"; break;
			case 145: s = "\"MyClass\" expected"; break;
			case 146: s = "\"Namespace\" expected"; break;
			case 147: s = "\"Narrowing\" expected"; break;
			case 148: s = "\"New\" expected"; break;
			case 149: s = "\"Next\" expected"; break;
			case 150: s = "\"Not\" expected"; break;
			case 151: s = "\"Nothing\" expected"; break;
			case 152: s = "\"NotInheritable\" expected"; break;
			case 153: s = "\"NotOverridable\" expected"; break;
			case 154: s = "\"Object\" expected"; break;
			case 155: s = "\"Of\" expected"; break;
			case 156: s = "\"Off\" expected"; break;
			case 157: s = "\"On\" expected"; break;
			case 158: s = "\"Operator\" expected"; break;
			case 159: s = "\"Option\" expected"; break;
			case 160: s = "\"Optional\" expected"; break;
			case 161: s = "\"Or\" expected"; break;
			case 162: s = "\"Order\" expected"; break;
			case 163: s = "\"OrElse\" expected"; break;
			case 164: s = "\"Overloads\" expected"; break;
			case 165: s = "\"Overridable\" expected"; break;
			case 166: s = "\"Overrides\" expected"; break;
			case 167: s = "\"ParamArray\" expected"; break;
			case 168: s = "\"Partial\" expected"; break;
			case 169: s = "\"Preserve\" expected"; break;
			case 170: s = "\"Private\" expected"; break;
			case 171: s = "\"Property\" expected"; break;
			case 172: s = "\"Protected\" expected"; break;
			case 173: s = "\"Public\" expected"; break;
			case 174: s = "\"RaiseEvent\" expected"; break;
			case 175: s = "\"ReadOnly\" expected"; break;
			case 176: s = "\"ReDim\" expected"; break;
			case 177: s = "\"Rem\" expected"; break;
			case 178: s = "\"RemoveHandler\" expected"; break;
			case 179: s = "\"Resume\" expected"; break;
			case 180: s = "\"Return\" expected"; break;
			case 181: s = "\"SByte\" expected"; break;
			case 182: s = "\"Select\" expected"; break;
			case 183: s = "\"Set\" expected"; break;
			case 184: s = "\"Shadows\" expected"; break;
			case 185: s = "\"Shared\" expected"; break;
			case 186: s = "\"Short\" expected"; break;
			case 187: s = "\"Single\" expected"; break;
			case 188: s = "\"Skip\" expected"; break;
			case 189: s = "\"Static\" expected"; break;
			case 190: s = "\"Step\" expected"; break;
			case 191: s = "\"Stop\" expected"; break;
			case 192: s = "\"Strict\" expected"; break;
			case 193: s = "\"String\" expected"; break;
			case 194: s = "\"Structure\" expected"; break;
			case 195: s = "\"Sub\" expected"; break;
			case 196: s = "\"SyncLock\" expected"; break;
			case 197: s = "\"Take\" expected"; break;
			case 198: s = "\"Text\" expected"; break;
			case 199: s = "\"Then\" expected"; break;
			case 200: s = "\"Throw\" expected"; break;
			case 201: s = "\"To\" expected"; break;
			case 202: s = "\"True\" expected"; break;
			case 203: s = "\"Try\" expected"; break;
			case 204: s = "\"TryCast\" expected"; break;
			case 205: s = "\"TypeOf\" expected"; break;
			case 206: s = "\"UInteger\" expected"; break;
			case 207: s = "\"ULong\" expected"; break;
			case 208: s = "\"Unicode\" expected"; break;
			case 209: s = "\"Until\" expected"; break;
			case 210: s = "\"UShort\" expected"; break;
			case 211: s = "\"Using\" expected"; break;
			case 212: s = "\"Variant\" expected"; break;
			case 213: s = "\"Wend\" expected"; break;
			case 214: s = "\"When\" expected"; break;
			case 215: s = "\"Where\" expected"; break;
			case 216: s = "\"While\" expected"; break;
			case 217: s = "\"Widening\" expected"; break;
			case 218: s = "\"With\" expected"; break;
			case 219: s = "\"WithEvents\" expected"; break;
			case 220: s = "\"WriteOnly\" expected"; break;
			case 221: s = "\"Xor\" expected"; break;
			case 222: s = "??? expected"; break;
			case 223: s = "invalid EndOfStmt"; break;
			case 224: s = "invalid OptionStmt"; break;
			case 225: s = "invalid OptionStmt"; break;
			case 226: s = "invalid GlobalAttributeSection"; break;
			case 227: s = "invalid GlobalAttributeSection"; break;
			case 228: s = "invalid NamespaceMemberDecl"; break;
			case 229: s = "invalid OptionValue"; break;
			case 230: s = "invalid TypeModifier"; break;
			case 231: s = "invalid NonModuleDeclaration"; break;
			case 232: s = "invalid NonModuleDeclaration"; break;
			case 233: s = "invalid Identifier"; break;
			case 234: s = "invalid TypeParameterConstraints"; break;
			case 235: s = "invalid TypeParameterConstraint"; break;
			case 236: s = "invalid NonArrayTypeName"; break;
			case 237: s = "invalid MemberModifier"; break;
			case 238: s = "invalid StructureMemberDecl"; break;
			case 239: s = "invalid StructureMemberDecl"; break;
			case 240: s = "invalid StructureMemberDecl"; break;
			case 241: s = "invalid StructureMemberDecl"; break;
			case 242: s = "invalid StructureMemberDecl"; break;
			case 243: s = "invalid StructureMemberDecl"; break;
			case 244: s = "invalid StructureMemberDecl"; break;
			case 245: s = "invalid InterfaceMemberDecl"; break;
			case 246: s = "invalid InterfaceMemberDecl"; break;
			case 247: s = "invalid Expr"; break;
			case 248: s = "invalid Charset"; break;
			case 249: s = "invalid IdentifierForFieldDeclaration"; break;
			case 250: s = "invalid VariableDeclaratorPartAfterIdentifier"; break;
			case 251: s = "invalid AccessorDecls"; break;
			case 252: s = "invalid EventAccessorDeclaration"; break;
			case 253: s = "invalid OverloadableOperator"; break;
			case 254: s = "invalid VariableInitializer"; break;
			case 255: s = "invalid EventMemberSpecifier"; break;
			case 256: s = "invalid AssignmentOperator"; break;
			case 257: s = "invalid SimpleNonInvocationExpression"; break;
			case 258: s = "invalid SimpleNonInvocationExpression"; break;
			case 259: s = "invalid SimpleNonInvocationExpression"; break;
			case 260: s = "invalid SimpleNonInvocationExpression"; break;
			case 261: s = "invalid PrimitiveTypeName"; break;
			case 262: s = "invalid CastTarget"; break;
			case 263: s = "invalid ComparisonExpr"; break;
			case 264: s = "invalid FromOrAggregateQueryOperator"; break;
			case 265: s = "invalid QueryOperator"; break;
			case 266: s = "invalid PartitionQueryOperator"; break;
			case 267: s = "invalid Argument"; break;
			case 268: s = "invalid QualIdentAndTypeArguments"; break;
			case 269: s = "invalid AttributeArguments"; break;
			case 270: s = "invalid ParameterModifier"; break;
			case 271: s = "invalid Statement"; break;
			case 272: s = "invalid LabelName"; break;
			case 273: s = "invalid EmbeddedStatement"; break;
			case 274: s = "invalid EmbeddedStatement"; break;
			case 275: s = "invalid EmbeddedStatement"; break;
			case 276: s = "invalid EmbeddedStatement"; break;
			case 277: s = "invalid EmbeddedStatement"; break;
			case 278: s = "invalid EmbeddedStatement"; break;
			case 279: s = "invalid EmbeddedStatement"; break;
			case 280: s = "invalid WhileOrUntil"; break;
			case 281: s = "invalid SingleLineStatementList"; break;
			case 282: s = "invalid SingleLineStatementList"; break;
			case 283: s = "invalid OnErrorStatement"; break;
			case 284: s = "invalid ResumeStatement"; break;
			case 285: s = "invalid CaseClause"; break;
			case 286: s = "invalid CaseClause"; break;

			default: s = "error " + errorNumber; break;
		}
		this.Errors.Error(line, col, s);
	}
	
	private bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	static bool[,] set = {
	{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, T,T,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, T,T,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, T,T,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, T,T,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,T,x, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,x, x,x,x,x, x,T,x,T, x,x,T,x, x,x,x,x, x,x,x,x, x,T,T,T, x,x,x,T, x,x,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,T,x, x,T,T,x, x,x,x,x, x,x,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,T,T, x,x,x,T, x,x,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,x,x, x,x,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,T, x,x,x,x, x,x,x,x, x,T,x,x, T,T,T,T, T,x,T,x, x,x,x,x, x,x,T,T, x,x,T,x, T,x,x,x, T,T,T,x, x,x,x,x, T,x,x,x, x,x,T,x, x,T,T,x, x,T,x,x, x,x,x,x, x,T,T,T, x,x,x,T, x,x,x,x, T,T,x,x, T,x,T,x, x,x,T,x, T,T,T,x, T,T,T,T, T,T,x,T, x,x,x,x, x,x,x,x, T,T,x,x, T,x,x,x, x,x,T,T, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,T,x,T, T,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, T,T,T,x, T,x,T,x, T,T,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,T,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, T,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,T,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, T,T,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, T,T,T,x, T,x,T,T, T,T,x,T, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,T,T, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,x,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,T,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,x,T, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,T,T,T, T,x,x,x, x,x,x,T, T,T,x,T, T,T,x,T, x,T,x,x, T,T,x,T, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,T, T,T,T,x, x,x,T,T, T,T,x,T, x,T,x,x, T,T,T,x, T,x,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,x,T,x, x,x,x,x},
	{x,T,T,T, T,T,T,T, T,T,T,T, T,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,T,T,T, T,T,T,T, x,T,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,T,T,T, T,x,T,x, T,x,x,T, T,T,x,T, T,T,x,T, x,T,x,x, T,T,x,T, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,T, T,T,T,x, x,x,T,T, T,T,x,T, x,T,x,x, T,T,T,x, T,x,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,x,T,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,T,T, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,T,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,T,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,T,T, x,x,T,T, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,T,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,T,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,x,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,T,T, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,x,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,T,T,T, T,x,x,x, x,x,x,T, T,T,x,T, T,T,x,T, x,T,x,x, T,T,x,T, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,T, T,T,T,x, x,x,T,T, T,T,x,T, x,T,x,x, T,T,T,x, T,x,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,x,T,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, T,x,x,x, T,x,T,T, x,x,T,x, x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,T,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,T,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, x,x,x,T, T,T,T,T, T,T,T,x, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, x,T,T,x, T,x,x,x, x,x,x,T, x,x,x,x, T,T,x,x, x,T,x,x, T,T,x,x, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,T,T, T,x,x,x, x,T,x,x, x,T,T,x, x,x,T,x, T,T,T,T, T,T,T,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,T, T,T,T,T, T,T,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,x,x, x,T,x,T, T,T,T,T, x,T,T,x, T,x,x,T, T,T,T,T, T,T,T,x, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,T,T,T, T,x,x,x, T,x,x,T, T,T,x,T, T,T,x,T, x,T,x,x, T,T,x,T, T,x,T,x, x,x,T,x, T,x,T,x, x,T,x,x, x,T,x,T, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,x, T,T,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,T,x, T,x,T,T, T,T,T,x, x,x,T,T, T,T,x,T, x,T,x,x, T,T,T,x, T,x,T,T, T,T,T,T, T,T,T,T, x,x,x,T, T,x,T,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, T,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,T,x,T, T,T,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,T,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,T,T,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,T, x,x,x,x, x,x,x,x},
	{x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x}

	};
} // end Parser

}