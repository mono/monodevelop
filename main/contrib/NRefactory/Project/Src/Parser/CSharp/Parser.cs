
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
	const int maxT = 145;

	const  bool   T            = true;
	const  bool   x            = false;
	

#line  18 "cs.ATG" 


/*

*/

	void CS() {

#line  179 "cs.ATG" 
		lexer.NextToken(); /* get the first token */ 
		while (la.kind == 71) {
			ExternAliasDirective();
		}
		while (la.kind == 121) {
			UsingDirective();
		}
		while (
#line  183 "cs.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void ExternAliasDirective() {

#line  353 "cs.ATG" 
		ExternAliasDirective ead = new ExternAliasDirective { StartLocation = la.Location }; 
		Expect(71);
		Identifier();

#line  356 "cs.ATG" 
		if (t.val != "alias") Error("Expected 'extern alias'."); 
		Identifier();

#line  357 "cs.ATG" 
		ead.Name = t.val; 
		Expect(11);

#line  358 "cs.ATG" 
		ead.EndLocation = t.EndLocation; 

#line  359 "cs.ATG" 
		compilationUnit.AddChild(ead); 
	}

	void UsingDirective() {

#line  190 "cs.ATG" 
		string qualident = null; TypeReference aliasedType = null;
		
		Expect(121);

#line  193 "cs.ATG" 
		Location startPos = t.Location; 
		Qualident(
#line  194 "cs.ATG" 
out qualident);
		if (la.kind == 3) {
			lexer.NextToken();
			NonArrayType(
#line  195 "cs.ATG" 
out aliasedType);
		}
		Expect(11);

#line  197 "cs.ATG" 
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

#line  213 "cs.ATG" 
		Location startPos = t.Location; 
		Identifier();

#line  214 "cs.ATG" 
		if (t.val != "assembly" && t.val != "module") Error("global attribute target specifier (assembly or module) expected");
		string attributeTarget = t.val;
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(9);
		Attribute(
#line  219 "cs.ATG" 
out attribute);

#line  219 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  220 "cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  220 "cs.ATG" 
out attribute);

#line  220 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  222 "cs.ATG" 
		AttributeSection section = new AttributeSection {
		   AttributeTarget = attributeTarget,
		   Attributes = attributes,
		   StartLocation = startPos,
		   EndLocation = t.EndLocation
		};
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  326 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		ModifierList m = new ModifierList();
		string qualident;
		
		if (la.kind == 88) {
			lexer.NextToken();

#line  332 "cs.ATG" 
			Location startPos = t.Location; 
			Qualident(
#line  333 "cs.ATG" 
out qualident);

#line  333 "cs.ATG" 
			INode node =  new NamespaceDeclaration(qualident);
			node.StartLocation = startPos;
			compilationUnit.AddChild(node);
			compilationUnit.BlockStart(node);
			
			Expect(16);
			while (la.kind == 71) {
				ExternAliasDirective();
			}
			while (la.kind == 121) {
				UsingDirective();
			}
			while (StartOf(1)) {
				NamespaceMemberDecl();
			}
			Expect(17);
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  343 "cs.ATG" 
			node.EndLocation   = t.EndLocation;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 18) {
				AttributeSection(
#line  347 "cs.ATG" 
out section);

#line  347 "cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  348 "cs.ATG" 
m);
			}
			TypeDecl(
#line  349 "cs.ATG" 
m, attributes);
		} else SynErr(146);
	}

	void Qualident(
#line  483 "cs.ATG" 
out string qualident) {
		Identifier();

#line  485 "cs.ATG" 
		qualidentBuilder.Length = 0; qualidentBuilder.Append(t.val); 
		while (
#line  486 "cs.ATG" 
DotAndIdent()) {
			Expect(15);
			Identifier();

#line  486 "cs.ATG" 
			qualidentBuilder.Append('.');
			qualidentBuilder.Append(t.val); 
			
		}

#line  489 "cs.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void NonArrayType(
#line  601 "cs.ATG" 
out TypeReference type) {

#line  603 "cs.ATG" 
		Location startPos = la.Location;
		string name;
		int pointer = 0;
		type = null;
		
		if (StartOf(4)) {
			ClassType(
#line  609 "cs.ATG" 
out type, false);
		} else if (StartOf(5)) {
			SimpleType(
#line  610 "cs.ATG" 
out name);

#line  610 "cs.ATG" 
			type = new TypeReference(name, true); 
		} else if (la.kind == 123) {
			lexer.NextToken();
			Expect(6);

#line  611 "cs.ATG" 
			pointer = 1; type = new TypeReference("System.Void", true); 
		} else SynErr(147);
		if (la.kind == 12) {
			NullableQuestionMark(
#line  614 "cs.ATG" 
ref type);
		}
		while (
#line  616 "cs.ATG" 
IsPointer()) {
			Expect(6);

#line  617 "cs.ATG" 
			++pointer; 
		}

#line  619 "cs.ATG" 
		if (type != null) {
		type.PointerNestingLevel = pointer; 
		type.EndLocation = t.EndLocation;
		type.StartLocation = startPos;
		} 
		
	}

	void Identifier() {
		switch (la.kind) {
		case 1: {
			lexer.NextToken();
			break;
		}
		case 126: {
			lexer.NextToken();
			break;
		}
		case 127: {
			lexer.NextToken();
			break;
		}
		case 128: {
			lexer.NextToken();
			break;
		}
		case 129: {
			lexer.NextToken();
			break;
		}
		case 130: {
			lexer.NextToken();
			break;
		}
		case 131: {
			lexer.NextToken();
			break;
		}
		case 132: {
			lexer.NextToken();
			break;
		}
		case 133: {
			lexer.NextToken();
			break;
		}
		case 134: {
			lexer.NextToken();
			break;
		}
		case 135: {
			lexer.NextToken();
			break;
		}
		case 136: {
			lexer.NextToken();
			break;
		}
		case 137: {
			lexer.NextToken();
			break;
		}
		case 138: {
			lexer.NextToken();
			break;
		}
		case 139: {
			lexer.NextToken();
			break;
		}
		case 140: {
			lexer.NextToken();
			break;
		}
		case 141: {
			lexer.NextToken();
			break;
		}
		case 142: {
			lexer.NextToken();
			break;
		}
		case 143: {
			lexer.NextToken();
			break;
		}
		case 144: {
			lexer.NextToken();
			break;
		}
		default: SynErr(148); break;
		}
	}

	void Attribute(
#line  232 "cs.ATG" 
out ASTAttribute attribute) {

#line  233 "cs.ATG" 
		string qualident;
		string alias = null;
		

#line  237 "cs.ATG" 
		Location startPos = la.Location; 
		if (
#line  238 "cs.ATG" 
IdentAndDoubleColon()) {
			Identifier();

#line  239 "cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  242 "cs.ATG" 
out qualident);

#line  243 "cs.ATG" 
		List<Expression> positional = null;
		List<NamedArgumentExpression> named = null;
		string name = (alias != null && alias != "global") ? alias + "." + qualident : qualident;
		
		if (la.kind == 20) {
			AttributeArguments(
#line  247 "cs.ATG" 
out positional, out named);
		}

#line  248 "cs.ATG" 
		attribute = new ASTAttribute(name, positional, named); 
		attribute.StartLocation = startPos;
		attribute.EndLocation = t.EndLocation;
		
	}

	void AttributeArguments(
#line  254 "cs.ATG" 
out List<Expression> positional, out List<NamedArgumentExpression> named) {

#line  256 "cs.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		positional = new List<Expression>();
		named = new List<NamedArgumentExpression> ();
		
		Expect(20);
		if (StartOf(6)) {
			if (
#line  266 "cs.ATG" 
IsAssignment()) {

#line  266 "cs.ATG" 
				nameFound = true; 
				Identifier();

#line  267 "cs.ATG" 
				name = t.val; 
				Expect(3);
			}
			Expr(
#line  269 "cs.ATG" 
out expr);

#line  269 "cs.ATG" 
			if (expr != null) {  
			if(name == "") positional.Add(expr);
			                          else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			                          }
			                       
			while (la.kind == 14) {
				lexer.NextToken();
				if (
#line  278 "cs.ATG" 
IsAssignment()) {

#line  278 "cs.ATG" 
					nameFound = true; 
					Identifier();

#line  279 "cs.ATG" 
					name = t.val; 
					Expect(3);
				} else if (StartOf(6)) {

#line  281 "cs.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(149);
				Expr(
#line  282 "cs.ATG" 
out expr);

#line  282 "cs.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				}
				
			}
		}
		Expect(21);
	}

	void Expr(
#line  1789 "cs.ATG" 
out Expression expr) {

#line  1790 "cs.ATG" 
		expr = null; Expression expr1 = null, expr2 = null; AssignmentOperatorType op; 

#line  1792 "cs.ATG" 
		Location startLocation = la.Location; 
		UnaryExpr(
#line  1793 "cs.ATG" 
out expr);
		if (StartOf(7)) {
			AssignmentOperator(
#line  1796 "cs.ATG" 
out op);
			Expr(
#line  1796 "cs.ATG" 
out expr1);

#line  1796 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (
#line  1797 "cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			AssignmentOperator(
#line  1798 "cs.ATG" 
out op);
			Expr(
#line  1798 "cs.ATG" 
out expr1);

#line  1798 "cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (StartOf(8)) {
			ConditionalOrExpr(
#line  1800 "cs.ATG" 
ref expr);
			if (la.kind == 13) {
				lexer.NextToken();
				Expr(
#line  1801 "cs.ATG" 
out expr1);

#line  1801 "cs.ATG" 
				expr = new BinaryOperatorExpression(expr, BinaryOperatorType.NullCoalescing, expr1); 
			}
			if (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  1802 "cs.ATG" 
out expr1);
				Expect(9);
				Expr(
#line  1802 "cs.ATG" 
out expr2);

#line  1802 "cs.ATG" 
				expr = new ConditionalExpression(expr, expr1, expr2);  
			}
		} else SynErr(150);

#line  1805 "cs.ATG" 
		if (expr != null) {
		expr.StartLocation = startLocation;
		expr.EndLocation = t.EndLocation;
		}
		
	}

	void AttributeSection(
#line  291 "cs.ATG" 
out AttributeSection section) {

#line  293 "cs.ATG" 
		string attributeTarget = "";
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		
		Expect(18);

#line  299 "cs.ATG" 
		Location startPos = t.Location; 
		if (
#line  300 "cs.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 69) {
				lexer.NextToken();

#line  301 "cs.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 101) {
				lexer.NextToken();

#line  302 "cs.ATG" 
				attributeTarget = "return";
			} else {
				Identifier();

#line  303 "cs.ATG" 
				if (t.val != "field"   && t.val != "method" &&
				  t.val != "param" &&
				  t.val != "property" && t.val != "type")
				Error("attribute target specifier (field, event, method, param, property, return or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(9);
		}
		Attribute(
#line  312 "cs.ATG" 
out attribute);

#line  312 "cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  313 "cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  313 "cs.ATG" 
out attribute);

#line  313 "cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  315 "cs.ATG" 
		section = new AttributeSection {
		   AttributeTarget = attributeTarget,
		   Attributes = attributes,
		   StartLocation = startPos,
		   EndLocation = t.EndLocation
		};
		
	}

	void TypeModifier(
#line  694 "cs.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 89: {
			lexer.NextToken();

#line  696 "cs.ATG" 
			m.Add(Modifiers.New, t.Location); 
			break;
		}
		case 98: {
			lexer.NextToken();

#line  697 "cs.ATG" 
			m.Add(Modifiers.Public, t.Location); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  698 "cs.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			break;
		}
		case 84: {
			lexer.NextToken();

#line  699 "cs.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  700 "cs.ATG" 
			m.Add(Modifiers.Private, t.Location); 
			break;
		}
		case 119: {
			lexer.NextToken();

#line  701 "cs.ATG" 
			m.Add(Modifiers.Unsafe, t.Location); 
			break;
		}
		case 49: {
			lexer.NextToken();

#line  702 "cs.ATG" 
			m.Add(Modifiers.Abstract, t.Location); 
			break;
		}
		case 103: {
			lexer.NextToken();

#line  703 "cs.ATG" 
			m.Add(Modifiers.Sealed, t.Location); 
			break;
		}
		case 107: {
			lexer.NextToken();

#line  704 "cs.ATG" 
			m.Add(Modifiers.Static, t.Location); 
			break;
		}
		case 126: {
			lexer.NextToken();

#line  705 "cs.ATG" 
			m.Add(Modifiers.Partial, t.Location); 
			break;
		}
		default: SynErr(151); break;
		}
	}

	void TypeDecl(
#line  362 "cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  364 "cs.ATG" 
		TypeReference type;
		List<TypeReference> names;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		string name;
		List<TemplateDefinition> templates;
		
		if (la.kind == 59) {

#line  370 "cs.ATG" 
			m.Check(Modifiers.Classes); 
			lexer.NextToken();

#line  371 "cs.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			templates = newType.Templates;
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			
			newType.Type = Types.Class;
			
			Identifier();

#line  379 "cs.ATG" 
			newType.Name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  382 "cs.ATG" 
templates);
			}
			if (la.kind == 9) {
				ClassBase(
#line  384 "cs.ATG" 
out names);

#line  384 "cs.ATG" 
				newType.BaseTypes = names; 
			}
			while (la.kind == 127) {
				TypeParameterConstraintsClause(
#line  387 "cs.ATG" 
templates);
			}

#line  389 "cs.ATG" 
			newType.BodyStartLocation = t.EndLocation; 
			Expect(16);
			ClassBody();
			Expect(17);
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  393 "cs.ATG" 
			newType.EndLocation = t.EndLocation; 
			compilationUnit.BlockEnd();
			
		} else if (StartOf(9)) {

#line  396 "cs.ATG" 
			m.Check(Modifiers.StructsInterfacesEnumsDelegates); 
			if (la.kind == 109) {
				lexer.NextToken();

#line  397 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.Type = Types.Struct; 
				
				Identifier();

#line  404 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  407 "cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					StructInterfaces(
#line  409 "cs.ATG" 
out names);

#line  409 "cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (la.kind == 127) {
					TypeParameterConstraintsClause(
#line  412 "cs.ATG" 
templates);
				}

#line  415 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				StructBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  417 "cs.ATG" 
				newType.EndLocation = t.EndLocation; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 83) {
				lexer.NextToken();

#line  421 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Interface;
				
				Identifier();

#line  428 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  431 "cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					InterfaceBase(
#line  433 "cs.ATG" 
out names);

#line  433 "cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (la.kind == 127) {
					TypeParameterConstraintsClause(
#line  436 "cs.ATG" 
templates);
				}

#line  438 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				InterfaceBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  440 "cs.ATG" 
				newType.EndLocation = t.EndLocation; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 68) {
				lexer.NextToken();

#line  444 "cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Enum;
				
				Identifier();

#line  450 "cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					lexer.NextToken();
					IntegralType(
#line  451 "cs.ATG" 
out name);

#line  451 "cs.ATG" 
					newType.BaseTypes.Add(new TypeReference(name, true)); 
				}

#line  453 "cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				EnumBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  455 "cs.ATG" 
				newType.EndLocation = t.EndLocation; 
				compilationUnit.BlockEnd();
				
			} else {
				lexer.NextToken();

#line  459 "cs.ATG" 
				DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
				templates = delegateDeclr.Templates;
				delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
				
				if (
#line  463 "cs.ATG" 
NotVoidPointer()) {
					Expect(123);

#line  463 "cs.ATG" 
					delegateDeclr.ReturnType = new TypeReference("System.Void", true); 
				} else if (StartOf(10)) {
					Type(
#line  464 "cs.ATG" 
out type);

#line  464 "cs.ATG" 
					delegateDeclr.ReturnType = type; 
				} else SynErr(152);
				Identifier();

#line  466 "cs.ATG" 
				delegateDeclr.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  469 "cs.ATG" 
templates);
				}
				Expect(20);
				if (StartOf(11)) {
					FormalParameterList(
#line  471 "cs.ATG" 
p);

#line  471 "cs.ATG" 
					delegateDeclr.Parameters = p; 
				}
				Expect(21);
				while (la.kind == 127) {
					TypeParameterConstraintsClause(
#line  475 "cs.ATG" 
templates);
				}
				Expect(11);

#line  477 "cs.ATG" 
				delegateDeclr.EndLocation = t.EndLocation;
				compilationUnit.AddChild(delegateDeclr);
				
			}
		} else SynErr(153);
	}

	void TypeParameterList(
#line  2366 "cs.ATG" 
List<TemplateDefinition> templates) {

#line  2368 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		Expect(23);
		while (la.kind == 18) {
			AttributeSection(
#line  2372 "cs.ATG" 
out section);

#line  2372 "cs.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  2373 "cs.ATG" 
		templates.Add(new TemplateDefinition(t.val, attributes)); 
		while (la.kind == 14) {
			lexer.NextToken();
			while (la.kind == 18) {
				AttributeSection(
#line  2374 "cs.ATG" 
out section);

#line  2374 "cs.ATG" 
				attributes.Add(section); 
			}
			Identifier();

#line  2375 "cs.ATG" 
			templates.Add(new TemplateDefinition(t.val, attributes)); 
		}
		Expect(22);
	}

	void ClassBase(
#line  492 "cs.ATG" 
out List<TypeReference> names) {

#line  494 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		ClassType(
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

	void TypeParameterConstraintsClause(
#line  2379 "cs.ATG" 
List<TemplateDefinition> templates) {

#line  2380 "cs.ATG" 
		string name = ""; TypeReference type; 
		Expect(127);
		Identifier();

#line  2383 "cs.ATG" 
		name = t.val; 
		Expect(9);
		TypeParameterConstraintsClauseBase(
#line  2385 "cs.ATG" 
out type);

#line  2386 "cs.ATG" 
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
#line  2395 "cs.ATG" 
out type);

#line  2396 "cs.ATG" 
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

#line  503 "cs.ATG" 
		AttributeSection section; 
		while (StartOf(12)) {

#line  505 "cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (!(StartOf(13))) {SynErr(154); lexer.NextToken(); }
			while (la.kind == 18) {
				AttributeSection(
#line  509 "cs.ATG" 
out section);

#line  509 "cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  510 "cs.ATG" 
m);
			ClassMemberDecl(
#line  511 "cs.ATG" 
m, attributes);
		}
	}

	void StructInterfaces(
#line  515 "cs.ATG" 
out List<TypeReference> names) {

#line  517 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  521 "cs.ATG" 
out typeRef, false);

#line  521 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  522 "cs.ATG" 
out typeRef, false);

#line  522 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void StructBody() {

#line  526 "cs.ATG" 
		AttributeSection section; 
		Expect(16);
		while (StartOf(14)) {

#line  529 "cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 18) {
				AttributeSection(
#line  532 "cs.ATG" 
out section);

#line  532 "cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  533 "cs.ATG" 
m);
			StructMemberDecl(
#line  534 "cs.ATG" 
m, attributes);
		}
		Expect(17);
	}

	void InterfaceBase(
#line  539 "cs.ATG" 
out List<TypeReference> names) {

#line  541 "cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  545 "cs.ATG" 
out typeRef, false);

#line  545 "cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  546 "cs.ATG" 
out typeRef, false);

#line  546 "cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void InterfaceBody() {
		Expect(16);
		while (StartOf(15)) {
			while (!(StartOf(16))) {SynErr(155); lexer.NextToken(); }
			InterfaceMemberDecl();
		}
		Expect(17);
	}

	void IntegralType(
#line  716 "cs.ATG" 
out string name) {

#line  716 "cs.ATG" 
		name = ""; 
		switch (la.kind) {
		case 102: {
			lexer.NextToken();

#line  718 "cs.ATG" 
			name = "System.SByte"; 
			break;
		}
		case 54: {
			lexer.NextToken();

#line  719 "cs.ATG" 
			name = "System.Byte"; 
			break;
		}
		case 104: {
			lexer.NextToken();

#line  720 "cs.ATG" 
			name = "System.Int16"; 
			break;
		}
		case 120: {
			lexer.NextToken();

#line  721 "cs.ATG" 
			name = "System.UInt16"; 
			break;
		}
		case 82: {
			lexer.NextToken();

#line  722 "cs.ATG" 
			name = "System.Int32"; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  723 "cs.ATG" 
			name = "System.UInt32"; 
			break;
		}
		case 87: {
			lexer.NextToken();

#line  724 "cs.ATG" 
			name = "System.Int64"; 
			break;
		}
		case 117: {
			lexer.NextToken();

#line  725 "cs.ATG" 
			name = "System.UInt64"; 
			break;
		}
		case 57: {
			lexer.NextToken();

#line  726 "cs.ATG" 
			name = "System.Char"; 
			break;
		}
		default: SynErr(156); break;
		}
	}

	void EnumBody() {

#line  555 "cs.ATG" 
		FieldDeclaration f; 
		Expect(16);
		if (StartOf(17)) {
			EnumMemberDecl(
#line  558 "cs.ATG" 
out f);

#line  558 "cs.ATG" 
			compilationUnit.AddChild(f); 
			while (
#line  559 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				EnumMemberDecl(
#line  560 "cs.ATG" 
out f);

#line  560 "cs.ATG" 
				compilationUnit.AddChild(f); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);
	}

	void Type(
#line  566 "cs.ATG" 
out TypeReference type) {
		TypeWithRestriction(
#line  568 "cs.ATG" 
out type, true, false);
	}

	void FormalParameterList(
#line  638 "cs.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  641 "cs.ATG" 
		ParameterDeclarationExpression p;
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		while (la.kind == 18) {
			AttributeSection(
#line  646 "cs.ATG" 
out section);

#line  646 "cs.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(18)) {
			FixedParameter(
#line  648 "cs.ATG" 
out p);

#line  648 "cs.ATG" 
			bool paramsFound = false;
			p.Attributes = attributes;
			parameter.Add(p);
			
			while (la.kind == 14) {
				lexer.NextToken();

#line  653 "cs.ATG" 
				attributes = new List<AttributeSection>(); if (paramsFound) Error("params array must be at end of parameter list"); 
				while (la.kind == 18) {
					AttributeSection(
#line  654 "cs.ATG" 
out section);

#line  654 "cs.ATG" 
					attributes.Add(section); 
				}
				if (StartOf(18)) {
					FixedParameter(
#line  656 "cs.ATG" 
out p);

#line  656 "cs.ATG" 
					p.Attributes = attributes; parameter.Add(p); 
				} else if (la.kind == 95) {
					ParameterArray(
#line  657 "cs.ATG" 
out p);

#line  657 "cs.ATG" 
					paramsFound = true; p.Attributes = attributes; parameter.Add(p); 
				} else SynErr(157);
			}
		} else if (la.kind == 95) {
			ParameterArray(
#line  660 "cs.ATG" 
out p);

#line  660 "cs.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		} else SynErr(158);
	}

	void ClassType(
#line  708 "cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  709 "cs.ATG" 
		TypeReference r; typeRef = null; 
		if (StartOf(19)) {
			TypeName(
#line  711 "cs.ATG" 
out r, canBeUnbound);

#line  711 "cs.ATG" 
			typeRef = r; 
		} else if (la.kind == 91) {
			lexer.NextToken();

#line  712 "cs.ATG" 
			typeRef = new TypeReference("System.Object", true); typeRef.StartLocation = t.Location; 
		} else if (la.kind == 108) {
			lexer.NextToken();

#line  713 "cs.ATG" 
			typeRef = new TypeReference("System.String", true); typeRef.StartLocation = t.Location; 
		} else SynErr(159);
	}

	void TypeName(
#line  2307 "cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  2308 "cs.ATG" 
		List<TypeReference> typeArguments = null;
		string alias = null;
		string qualident;
		Location startLocation = la.Location;
		
		if (
#line  2314 "cs.ATG" 
IdentAndDoubleColon()) {
			Identifier();

#line  2315 "cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  2318 "cs.ATG" 
out qualident);
		if (la.kind == 23) {
			TypeArgumentList(
#line  2319 "cs.ATG" 
out typeArguments, canBeUnbound);
		}

#line  2321 "cs.ATG" 
		if (alias == null) {
		typeRef = new TypeReference(qualident, typeArguments);
		} else if (alias == "global") {
			typeRef = new TypeReference(qualident, typeArguments);
			typeRef.IsGlobal = true;
		} else {
			typeRef = new TypeReference(alias + "." + qualident, typeArguments);
		}
		
		while (
#line  2330 "cs.ATG" 
DotAndIdent()) {
			Expect(15);

#line  2331 "cs.ATG" 
			typeArguments = null; 
			Qualident(
#line  2332 "cs.ATG" 
out qualident);
			if (la.kind == 23) {
				TypeArgumentList(
#line  2333 "cs.ATG" 
out typeArguments, canBeUnbound);
			}

#line  2334 "cs.ATG" 
			typeRef = new InnerClassTypeReference(typeRef, qualident, typeArguments); 
		}

#line  2336 "cs.ATG" 
		typeRef.StartLocation = startLocation; 
	}

	void MemberModifiers(
#line  729 "cs.ATG" 
ModifierList m) {
		while (StartOf(20)) {
			switch (la.kind) {
			case 49: {
				lexer.NextToken();

#line  732 "cs.ATG" 
				m.Add(Modifiers.Abstract, t.Location); 
				break;
			}
			case 71: {
				lexer.NextToken();

#line  733 "cs.ATG" 
				m.Add(Modifiers.Extern, t.Location); 
				break;
			}
			case 84: {
				lexer.NextToken();

#line  734 "cs.ATG" 
				m.Add(Modifiers.Internal, t.Location); 
				break;
			}
			case 89: {
				lexer.NextToken();

#line  735 "cs.ATG" 
				m.Add(Modifiers.New, t.Location); 
				break;
			}
			case 94: {
				lexer.NextToken();

#line  736 "cs.ATG" 
				m.Add(Modifiers.Override, t.Location); 
				break;
			}
			case 96: {
				lexer.NextToken();

#line  737 "cs.ATG" 
				m.Add(Modifiers.Private, t.Location); 
				break;
			}
			case 97: {
				lexer.NextToken();

#line  738 "cs.ATG" 
				m.Add(Modifiers.Protected, t.Location); 
				break;
			}
			case 98: {
				lexer.NextToken();

#line  739 "cs.ATG" 
				m.Add(Modifiers.Public, t.Location); 
				break;
			}
			case 99: {
				lexer.NextToken();

#line  740 "cs.ATG" 
				m.Add(Modifiers.ReadOnly, t.Location); 
				break;
			}
			case 103: {
				lexer.NextToken();

#line  741 "cs.ATG" 
				m.Add(Modifiers.Sealed, t.Location); 
				break;
			}
			case 107: {
				lexer.NextToken();

#line  742 "cs.ATG" 
				m.Add(Modifiers.Static, t.Location); 
				break;
			}
			case 74: {
				lexer.NextToken();

#line  743 "cs.ATG" 
				m.Add(Modifiers.Fixed, t.Location); 
				break;
			}
			case 119: {
				lexer.NextToken();

#line  744 "cs.ATG" 
				m.Add(Modifiers.Unsafe, t.Location); 
				break;
			}
			case 122: {
				lexer.NextToken();

#line  745 "cs.ATG" 
				m.Add(Modifiers.Virtual, t.Location); 
				break;
			}
			case 124: {
				lexer.NextToken();

#line  746 "cs.ATG" 
				m.Add(Modifiers.Volatile, t.Location); 
				break;
			}
			case 126: {
				lexer.NextToken();

#line  747 "cs.ATG" 
				m.Add(Modifiers.Partial, t.Location); 
				break;
			}
			}
		}
	}

	void ClassMemberDecl(
#line  1063 "cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  1064 "cs.ATG" 
		Statement stmt = null; 
		if (StartOf(21)) {
			StructMemberDecl(
#line  1066 "cs.ATG" 
m, attributes);
		} else if (la.kind == 27) {

#line  1067 "cs.ATG" 
			m.Check(Modifiers.Destructors); Location startPos = la.Location; 
			lexer.NextToken();
			Identifier();

#line  1068 "cs.ATG" 
			DestructorDeclaration d = new DestructorDeclaration(t.val, m.Modifier, attributes); 
			d.Modifier = m.Modifier;
			d.StartLocation = m.GetDeclarationLocation(startPos);
			
			Expect(20);
			Expect(21);

#line  1072 "cs.ATG" 
			d.EndLocation = t.EndLocation; 
			if (la.kind == 16) {
				Block(
#line  1072 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(160);

#line  1073 "cs.ATG" 
			d.Body = (BlockStatement)stmt;
			compilationUnit.AddChild(d);
			
		} else SynErr(161);
	}

	void StructMemberDecl(
#line  751 "cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  753 "cs.ATG" 
		string qualident = null;
		TypeReference type;
		Expression expr;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		TypeReference explicitInterface = null;
		bool isExtensionMethod = false;
		
		if (la.kind == 60) {

#line  763 "cs.ATG" 
			m.Check(Modifiers.Constants); 
			lexer.NextToken();

#line  764 "cs.ATG" 
			Location startPos = t.Location; 
			Type(
#line  765 "cs.ATG" 
out type);
			Identifier();

#line  765 "cs.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier | Modifiers.Const);
			fd.StartLocation = m.GetDeclarationLocation(startPos);
			VariableDeclaration f = new VariableDeclaration(t.val);
			f.StartLocation = t.Location;
			f.TypeReference = type;
			SafeAdd(fd, fd.Fields, f);
			
			Expect(3);
			Expr(
#line  772 "cs.ATG" 
out expr);

#line  772 "cs.ATG" 
			f.Initializer = expr; 
			while (la.kind == 14) {
				lexer.NextToken();
				Identifier();

#line  773 "cs.ATG" 
				f = new VariableDeclaration(t.val);
				f.StartLocation = t.Location;
				f.TypeReference = type;
				SafeAdd(fd, fd.Fields, f);
				
				Expect(3);
				Expr(
#line  778 "cs.ATG" 
out expr);

#line  778 "cs.ATG" 
				f.EndLocation = t.EndLocation; f.Initializer = expr; 
			}
			Expect(11);

#line  779 "cs.ATG" 
			fd.EndLocation = t.EndLocation; compilationUnit.AddChild(fd); 
		} else if (
#line  783 "cs.ATG" 
NotVoidPointer()) {

#line  783 "cs.ATG" 
			m.Check(Modifiers.PropertysEventsMethods); 
			Expect(123);

#line  784 "cs.ATG" 
			Location startPos = t.Location; 
			if (
#line  785 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  786 "cs.ATG" 
out explicitInterface, false);

#line  787 "cs.ATG" 
				if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
				 } 
			} else if (StartOf(19)) {
				Identifier();

#line  790 "cs.ATG" 
				qualident = t.val; 
			} else SynErr(162);
			if (la.kind == 23) {
				TypeParameterList(
#line  793 "cs.ATG" 
templates);
			}
			Expect(20);
			if (la.kind == 111) {
				lexer.NextToken();

#line  796 "cs.ATG" 
				isExtensionMethod = true; /* C# 3.0 */ 
			}
			if (StartOf(11)) {
				FormalParameterList(
#line  797 "cs.ATG" 
p);
			}
			Expect(21);

#line  798 "cs.ATG" 
			MethodDeclaration methodDeclaration = new MethodDeclaration {
			Name = qualident,
			Modifier = m.Modifier,
			TypeReference = new TypeReference("System.Void", true),
			Parameters = p,
			Attributes = attributes,
			StartLocation = m.GetDeclarationLocation(startPos),
			EndLocation = t.EndLocation,
			Templates = templates,
			IsExtensionMethod = isExtensionMethod
			};
			if (explicitInterface != null)
				SafeAdd(methodDeclaration, methodDeclaration.InterfaceImplementations, new InterfaceImplementation(explicitInterface, qualident));
			compilationUnit.AddChild(methodDeclaration);
			compilationUnit.BlockStart(methodDeclaration);
			
			while (la.kind == 127) {
				TypeParameterConstraintsClause(
#line  816 "cs.ATG" 
templates);
			}
			if (la.kind == 16) {
				Block(
#line  818 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(163);

#line  818 "cs.ATG" 
			compilationUnit.BlockEnd();
			methodDeclaration.Body  = (BlockStatement)stmt;
			
		} else if (la.kind == 69) {

#line  822 "cs.ATG" 
			m.Check(Modifiers.PropertysEventsMethods); 
			lexer.NextToken();

#line  824 "cs.ATG" 
			EventDeclaration eventDecl = new EventDeclaration {
			Modifier = m.Modifier, 
			Attributes = attributes,
			StartLocation = t.Location
			};
			compilationUnit.AddChild(eventDecl);
			compilationUnit.BlockStart(eventDecl);
			EventAddRegion addBlock = null;
			EventRemoveRegion removeBlock = null;
			
			Type(
#line  834 "cs.ATG" 
out type);

#line  834 "cs.ATG" 
			eventDecl.TypeReference = type; 
			if (
#line  835 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  836 "cs.ATG" 
out explicitInterface, false);

#line  837 "cs.ATG" 
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface); 

#line  838 "cs.ATG" 
				eventDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident)); 
			} else if (StartOf(19)) {
				Identifier();

#line  840 "cs.ATG" 
				qualident = t.val; 
			} else SynErr(164);

#line  842 "cs.ATG" 
			eventDecl.Name = qualident; eventDecl.EndLocation = t.EndLocation; 
			if (la.kind == 3) {
				lexer.NextToken();
				Expr(
#line  843 "cs.ATG" 
out expr);

#line  843 "cs.ATG" 
				eventDecl.Initializer = expr; 
			}
			if (la.kind == 16) {
				lexer.NextToken();

#line  844 "cs.ATG" 
				eventDecl.BodyStart = t.Location; 
				EventAccessorDecls(
#line  845 "cs.ATG" 
out addBlock, out removeBlock);
				Expect(17);

#line  846 "cs.ATG" 
				eventDecl.BodyEnd   = t.EndLocation; 
			}
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  849 "cs.ATG" 
			compilationUnit.BlockEnd();
			eventDecl.AddRegion = addBlock;
			eventDecl.RemoveRegion = removeBlock;
			
		} else if (
#line  855 "cs.ATG" 
IdentAndLPar()) {

#line  855 "cs.ATG" 
			m.Check(Modifiers.Constructors | Modifiers.StaticConstructors); 
			Identifier();

#line  856 "cs.ATG" 
			string name = t.val; Location startPos = t.Location; 
			Expect(20);
			if (StartOf(11)) {

#line  856 "cs.ATG" 
				m.Check(Modifiers.Constructors); 
				FormalParameterList(
#line  857 "cs.ATG" 
p);
			}
			Expect(21);

#line  859 "cs.ATG" 
			ConstructorInitializer init = null;  
			if (la.kind == 9) {

#line  860 "cs.ATG" 
				m.Check(Modifiers.Constructors); 
				ConstructorInitializer(
#line  861 "cs.ATG" 
out init);
			}

#line  863 "cs.ATG" 
			ConstructorDeclaration cd = new ConstructorDeclaration(name, m.Modifier, p, init, attributes);
			cd.StartLocation = startPos;
			cd.EndLocation   = t.EndLocation;
			
			if (la.kind == 16) {
				Block(
#line  868 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(165);

#line  868 "cs.ATG" 
			cd.Body = (BlockStatement)stmt; compilationUnit.AddChild(cd); 
		} else if (la.kind == 70 || la.kind == 80) {

#line  871 "cs.ATG" 
			m.Check(Modifiers.Operators);
			if (m.isNone) Error("at least one modifier must be set"); 
			bool isImplicit = true;
			Location startPos = Location.Empty;
			
			if (la.kind == 80) {
				lexer.NextToken();

#line  876 "cs.ATG" 
				startPos = t.Location; 
			} else {
				lexer.NextToken();

#line  876 "cs.ATG" 
				isImplicit = false; startPos = t.Location; 
			}
			Expect(92);
			Type(
#line  877 "cs.ATG" 
out type);

#line  877 "cs.ATG" 
			TypeReference operatorType = type; 
			Expect(20);
			Type(
#line  878 "cs.ATG" 
out type);
			Identifier();

#line  878 "cs.ATG" 
			string varName = t.val; 
			Expect(21);

#line  879 "cs.ATG" 
			Location endPos = t.Location; 
			if (la.kind == 16) {
				Block(
#line  880 "cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();

#line  880 "cs.ATG" 
				stmt = null; 
			} else SynErr(166);

#line  883 "cs.ATG" 
			List<ParameterDeclarationExpression> parameters = new List<ParameterDeclarationExpression>();
			parameters.Add(new ParameterDeclarationExpression(type, varName));
			OperatorDeclaration operatorDeclaration = new OperatorDeclaration {
				Name = (isImplicit ? "op_Implicit" : "op_Explicit"),
				Modifier = m.Modifier,
				Attributes = attributes, 
				Parameters = parameters, 
				TypeReference = operatorType,
				ConversionType = isImplicit ? ConversionType.Implicit : ConversionType.Explicit,
				Body = (BlockStatement)stmt,
				StartLocation = m.GetDeclarationLocation(startPos),
				EndLocation = endPos
			};
			compilationUnit.AddChild(operatorDeclaration);
			
		} else if (StartOf(22)) {
			TypeDecl(
#line  901 "cs.ATG" 
m, attributes);
		} else if (StartOf(10)) {
			Type(
#line  903 "cs.ATG" 
out type);

#line  903 "cs.ATG" 
			Location startPos = t.Location;  
			if (la.kind == 92) {

#line  905 "cs.ATG" 
				OverloadableOperatorType op;
				m.Check(Modifiers.Operators);
				if (m.isNone) Error("at least one modifier must be set");
				
				lexer.NextToken();
				OverloadableOperator(
#line  909 "cs.ATG" 
out op);

#line  909 "cs.ATG" 
				TypeReference firstType, secondType = null; string secondName = null; 
				Expect(20);
				Type(
#line  910 "cs.ATG" 
out firstType);
				Identifier();

#line  910 "cs.ATG" 
				string firstName = t.val; 
				if (la.kind == 14) {
					lexer.NextToken();
					Type(
#line  911 "cs.ATG" 
out secondType);
					Identifier();

#line  911 "cs.ATG" 
					secondName = t.val; 
				} else if (la.kind == 21) {
				} else SynErr(167);

#line  919 "cs.ATG" 
				Location endPos = t.Location; 
				Expect(21);
				if (la.kind == 16) {
					Block(
#line  920 "cs.ATG" 
out stmt);
				} else if (la.kind == 11) {
					lexer.NextToken();
				} else SynErr(168);

#line  922 "cs.ATG" 
				if (op == OverloadableOperatorType.Add && secondType == null)
				op = OverloadableOperatorType.UnaryPlus;
				if (op == OverloadableOperatorType.Subtract && secondType == null)
					op = OverloadableOperatorType.UnaryMinus;
				OperatorDeclaration operatorDeclaration = new OperatorDeclaration {
					Modifier = m.Modifier,
					Attributes = attributes,
					TypeReference = type,
					OverloadableOperator = op,
					Name = GetReflectionNameForOperator(op),
					Body = (BlockStatement)stmt,
					StartLocation = m.GetDeclarationLocation(startPos),
					EndLocation = endPos
				};
				SafeAdd(operatorDeclaration, operatorDeclaration.Parameters, new ParameterDeclarationExpression(firstType, firstName));
				if (secondType != null) {
					SafeAdd(operatorDeclaration, operatorDeclaration.Parameters, new ParameterDeclarationExpression(secondType, secondName));
				}
				compilationUnit.AddChild(operatorDeclaration);
				
			} else if (
#line  944 "cs.ATG" 
IsVarDecl()) {

#line  945 "cs.ATG" 
				m.Check(Modifiers.Fields);
				FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
				fd.StartLocation = m.GetDeclarationLocation(startPos); 
				
				if (
#line  949 "cs.ATG" 
m.Contains(Modifiers.Fixed)) {
					VariableDeclarator(
#line  950 "cs.ATG" 
fd);
					Expect(18);
					Expr(
#line  952 "cs.ATG" 
out expr);

#line  952 "cs.ATG" 
					if (fd.Fields.Count > 0)
					fd.Fields[fd.Fields.Count-1].FixedArrayInitialization = expr; 
					Expect(19);
					while (la.kind == 14) {
						lexer.NextToken();
						VariableDeclarator(
#line  956 "cs.ATG" 
fd);
						Expect(18);
						Expr(
#line  958 "cs.ATG" 
out expr);

#line  958 "cs.ATG" 
						if (fd.Fields.Count > 0)
						fd.Fields[fd.Fields.Count-1].FixedArrayInitialization = expr; 
						Expect(19);
					}
				} else if (StartOf(19)) {
					VariableDeclarator(
#line  963 "cs.ATG" 
fd);
					while (la.kind == 14) {
						lexer.NextToken();
						VariableDeclarator(
#line  964 "cs.ATG" 
fd);
					}
				} else SynErr(169);
				Expect(11);

#line  966 "cs.ATG" 
				fd.EndLocation = t.EndLocation; compilationUnit.AddChild(fd); 
			} else if (la.kind == 111) {

#line  969 "cs.ATG" 
				m.Check(Modifiers.Indexers); 
				lexer.NextToken();
				Expect(18);
				FormalParameterList(
#line  970 "cs.ATG" 
p);
				Expect(19);

#line  970 "cs.ATG" 
				Location endLocation = t.EndLocation; 
				Expect(16);

#line  971 "cs.ATG" 
				IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
				indexer.StartLocation = startPos;
				indexer.EndLocation   = endLocation;
				indexer.BodyStart     = t.Location;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  978 "cs.ATG" 
out getRegion, out setRegion);
				Expect(17);

#line  979 "cs.ATG" 
				indexer.BodyEnd    = t.EndLocation;
				indexer.GetRegion = getRegion;
				indexer.SetRegion = setRegion;
				compilationUnit.AddChild(indexer);
				
			} else if (
#line  984 "cs.ATG" 
IsIdentifierToken(la)) {
				if (
#line  985 "cs.ATG" 
IsExplicitInterfaceImplementation()) {
					TypeName(
#line  986 "cs.ATG" 
out explicitInterface, false);

#line  987 "cs.ATG" 
					if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
					qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
					 } 
				} else if (StartOf(19)) {
					Identifier();

#line  990 "cs.ATG" 
					qualident = t.val; 
				} else SynErr(170);

#line  992 "cs.ATG" 
				Location qualIdentEndLocation = t.EndLocation; 
				if (la.kind == 16 || la.kind == 20 || la.kind == 23) {
					if (la.kind == 20 || la.kind == 23) {

#line  996 "cs.ATG" 
						m.Check(Modifiers.PropertysEventsMethods); 
						if (la.kind == 23) {
							TypeParameterList(
#line  998 "cs.ATG" 
templates);
						}
						Expect(20);
						if (la.kind == 111) {
							lexer.NextToken();

#line  1000 "cs.ATG" 
							isExtensionMethod = true; 
						}
						if (StartOf(11)) {
							FormalParameterList(
#line  1001 "cs.ATG" 
p);
						}
						Expect(21);

#line  1003 "cs.ATG" 
						MethodDeclaration methodDeclaration = new MethodDeclaration {
						Name = qualident,
						Modifier = m.Modifier,
						TypeReference = type,
						Parameters = p, 
						Attributes = attributes
						};
						if (explicitInterface != null)
							methodDeclaration.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
						methodDeclaration.StartLocation = m.GetDeclarationLocation(startPos);
						methodDeclaration.EndLocation   = t.EndLocation;
						methodDeclaration.IsExtensionMethod = isExtensionMethod;
						methodDeclaration.Templates = templates;
						compilationUnit.AddChild(methodDeclaration);
						                                      
						while (la.kind == 127) {
							TypeParameterConstraintsClause(
#line  1018 "cs.ATG" 
templates);
						}
						if (la.kind == 16) {
							Block(
#line  1019 "cs.ATG" 
out stmt);
						} else if (la.kind == 11) {
							lexer.NextToken();
						} else SynErr(171);

#line  1019 "cs.ATG" 
						methodDeclaration.Body  = (BlockStatement)stmt; 
					} else {
						lexer.NextToken();

#line  1022 "cs.ATG" 
						PropertyDeclaration pDecl = new PropertyDeclaration(qualident, type, m.Modifier, attributes); 
						if (explicitInterface != null)
						pDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
						      pDecl.StartLocation = m.GetDeclarationLocation(startPos);
						      pDecl.EndLocation   = qualIdentEndLocation;
						      pDecl.BodyStart   = t.Location;
						      PropertyGetRegion getRegion;
						      PropertySetRegion setRegion;
						   
						AccessorDecls(
#line  1031 "cs.ATG" 
out getRegion, out setRegion);
						Expect(17);

#line  1033 "cs.ATG" 
						pDecl.GetRegion = getRegion;
						pDecl.SetRegion = setRegion;
						pDecl.BodyEnd = t.EndLocation;
						compilationUnit.AddChild(pDecl);
						
					}
				} else if (la.kind == 15) {

#line  1041 "cs.ATG" 
					m.Check(Modifiers.Indexers); 
					lexer.NextToken();
					Expect(111);
					Expect(18);
					FormalParameterList(
#line  1042 "cs.ATG" 
p);
					Expect(19);

#line  1043 "cs.ATG" 
					IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
					indexer.StartLocation = m.GetDeclarationLocation(startPos);
					indexer.EndLocation   = t.EndLocation;
					if (explicitInterface != null)
					SafeAdd(indexer, indexer.InterfaceImplementations, new InterfaceImplementation(explicitInterface, "this"));
					      PropertyGetRegion getRegion;
					      PropertySetRegion setRegion;
					    
					Expect(16);

#line  1051 "cs.ATG" 
					Location bodyStart = t.Location; 
					AccessorDecls(
#line  1052 "cs.ATG" 
out getRegion, out setRegion);
					Expect(17);

#line  1053 "cs.ATG" 
					indexer.BodyStart = bodyStart;
					indexer.BodyEnd   = t.EndLocation;
					indexer.GetRegion = getRegion;
					indexer.SetRegion = setRegion;
					compilationUnit.AddChild(indexer);
					
				} else SynErr(172);
			} else SynErr(173);
		} else SynErr(174);
	}

	void InterfaceMemberDecl() {

#line  1080 "cs.ATG" 
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
#line  1093 "cs.ATG" 
out section);

#line  1093 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 89) {
			lexer.NextToken();

#line  1094 "cs.ATG" 
			mod = Modifiers.New; startLocation = t.Location; 
		}
		if (
#line  1097 "cs.ATG" 
NotVoidPointer()) {
			Expect(123);

#line  1097 "cs.ATG" 
			if (startLocation.IsEmpty) startLocation = t.Location; 
			Identifier();

#line  1098 "cs.ATG" 
			name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  1099 "cs.ATG" 
templates);
			}
			Expect(20);
			if (StartOf(11)) {
				FormalParameterList(
#line  1100 "cs.ATG" 
parameters);
			}
			Expect(21);
			while (la.kind == 127) {
				TypeParameterConstraintsClause(
#line  1101 "cs.ATG" 
templates);
			}
			Expect(11);

#line  1103 "cs.ATG" 
			MethodDeclaration md = new MethodDeclaration {
			Name = name, Modifier = mod, TypeReference = new TypeReference("System.Void", true), 
			Parameters = parameters, Attributes = attributes, Templates = templates,
			StartLocation = startLocation, EndLocation = t.EndLocation
			};
			compilationUnit.AddChild(md);
			
		} else if (StartOf(23)) {
			if (StartOf(10)) {
				Type(
#line  1111 "cs.ATG" 
out type);

#line  1111 "cs.ATG" 
				if (startLocation.IsEmpty) startLocation = t.Location; 
				if (StartOf(19)) {
					Identifier();

#line  1113 "cs.ATG" 
					name = t.val; Location qualIdentEndLocation = t.EndLocation; 
					if (la.kind == 20 || la.kind == 23) {
						if (la.kind == 23) {
							TypeParameterList(
#line  1117 "cs.ATG" 
templates);
						}
						Expect(20);
						if (StartOf(11)) {
							FormalParameterList(
#line  1118 "cs.ATG" 
parameters);
						}
						Expect(21);
						while (la.kind == 127) {
							TypeParameterConstraintsClause(
#line  1120 "cs.ATG" 
templates);
						}
						Expect(11);

#line  1121 "cs.ATG" 
						MethodDeclaration md = new MethodDeclaration {
						Name = name, Modifier = mod, TypeReference = type,
						Parameters = parameters, Attributes = attributes, Templates = templates,
						StartLocation = startLocation, EndLocation = t.EndLocation
						};
						compilationUnit.AddChild(md);
						
					} else if (la.kind == 16) {

#line  1130 "cs.ATG" 
						PropertyDeclaration pd = new PropertyDeclaration(name, type, mod, attributes);
						compilationUnit.AddChild(pd); 
						lexer.NextToken();

#line  1133 "cs.ATG" 
						Location bodyStart = t.Location;
						InterfaceAccessors(
#line  1134 "cs.ATG" 
out getBlock, out setBlock);
						Expect(17);

#line  1135 "cs.ATG" 
						pd.GetRegion = getBlock; pd.SetRegion = setBlock; pd.StartLocation = startLocation; pd.EndLocation = qualIdentEndLocation; pd.BodyStart = bodyStart; pd.BodyEnd = t.EndLocation; 
					} else SynErr(175);
				} else if (la.kind == 111) {
					lexer.NextToken();
					Expect(18);
					FormalParameterList(
#line  1138 "cs.ATG" 
parameters);
					Expect(19);

#line  1139 "cs.ATG" 
					Location bracketEndLocation = t.EndLocation; 

#line  1140 "cs.ATG" 
					IndexerDeclaration id = new IndexerDeclaration(type, parameters, mod, attributes);
					compilationUnit.AddChild(id); 
					Expect(16);

#line  1142 "cs.ATG" 
					Location bodyStart = t.Location;
					InterfaceAccessors(
#line  1143 "cs.ATG" 
out getBlock, out setBlock);
					Expect(17);

#line  1145 "cs.ATG" 
					id.GetRegion = getBlock; id.SetRegion = setBlock; id.StartLocation = startLocation;  id.EndLocation = bracketEndLocation; id.BodyStart = bodyStart; id.BodyEnd = t.EndLocation;
				} else SynErr(176);
			} else {
				lexer.NextToken();

#line  1148 "cs.ATG" 
				if (startLocation.IsEmpty) startLocation = t.Location; 
				Type(
#line  1149 "cs.ATG" 
out type);
				Identifier();

#line  1150 "cs.ATG" 
				EventDeclaration ed = new EventDeclaration {
				TypeReference = type, Name = t.val, Modifier = mod, Attributes = attributes
				};
				compilationUnit.AddChild(ed);
				
				Expect(11);

#line  1156 "cs.ATG" 
				ed.StartLocation = startLocation; ed.EndLocation = t.EndLocation; 
			}
		} else SynErr(177);
	}

	void EnumMemberDecl(
#line  1161 "cs.ATG" 
out FieldDeclaration f) {

#line  1163 "cs.ATG" 
		Expression expr = null;
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1169 "cs.ATG" 
out section);

#line  1169 "cs.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  1170 "cs.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl         = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = t.Location;
		f.EndLocation = t.EndLocation;
		
		if (la.kind == 3) {
			lexer.NextToken();
			Expr(
#line  1176 "cs.ATG" 
out expr);

#line  1176 "cs.ATG" 
			varDecl.Initializer = expr; 
		}
	}

	void TypeWithRestriction(
#line  571 "cs.ATG" 
out TypeReference type, bool allowNullable, bool canBeUnbound) {

#line  573 "cs.ATG" 
		Location startPos = la.Location;
		string name;
		int pointer = 0;
		type = null;
		
		if (StartOf(4)) {
			ClassType(
#line  579 "cs.ATG" 
out type, canBeUnbound);
		} else if (StartOf(5)) {
			SimpleType(
#line  580 "cs.ATG" 
out name);

#line  580 "cs.ATG" 
			type = new TypeReference(name, true); 
		} else if (la.kind == 123) {
			lexer.NextToken();
			Expect(6);

#line  581 "cs.ATG" 
			pointer = 1; type = new TypeReference("System.Void", true); 
		} else SynErr(178);

#line  582 "cs.ATG" 
		List<int> r = new List<int>(); 
		if (
#line  584 "cs.ATG" 
allowNullable && la.kind == Tokens.Question) {
			NullableQuestionMark(
#line  584 "cs.ATG" 
ref type);
		}
		while (
#line  586 "cs.ATG" 
IsPointerOrDims()) {

#line  586 "cs.ATG" 
			int i = 0; 
			if (la.kind == 6) {
				lexer.NextToken();

#line  587 "cs.ATG" 
				++pointer; 
			} else if (la.kind == 18) {
				lexer.NextToken();
				while (la.kind == 14) {
					lexer.NextToken();

#line  588 "cs.ATG" 
					++i; 
				}
				Expect(19);

#line  588 "cs.ATG" 
				r.Add(i); 
			} else SynErr(179);
		}

#line  591 "cs.ATG" 
		if (type != null) {
		type.RankSpecifier = r.ToArray();
		type.PointerNestingLevel = pointer;
		type.EndLocation = t.EndLocation;
		type.StartLocation = startPos;
		}
		
	}

	void SimpleType(
#line  627 "cs.ATG" 
out string name) {

#line  628 "cs.ATG" 
		name = String.Empty; 
		if (StartOf(24)) {
			IntegralType(
#line  630 "cs.ATG" 
out name);
		} else if (la.kind == 75) {
			lexer.NextToken();

#line  631 "cs.ATG" 
			name = "System.Single"; 
		} else if (la.kind == 66) {
			lexer.NextToken();

#line  632 "cs.ATG" 
			name = "System.Double"; 
		} else if (la.kind == 62) {
			lexer.NextToken();

#line  633 "cs.ATG" 
			name = "System.Decimal"; 
		} else if (la.kind == 52) {
			lexer.NextToken();

#line  634 "cs.ATG" 
			name = "System.Boolean"; 
		} else SynErr(180);
	}

	void NullableQuestionMark(
#line  2340 "cs.ATG" 
ref TypeReference typeRef) {

#line  2341 "cs.ATG" 
		List<TypeReference> typeArguments = new List<TypeReference>(1); 
		Expect(12);

#line  2345 "cs.ATG" 
		if (typeRef != null) typeArguments.Add(typeRef);
		typeRef = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };
		
	}

	void FixedParameter(
#line  664 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  666 "cs.ATG" 
		TypeReference type;
		ParameterModifiers mod = ParameterModifiers.In;
		Location start = la.Location;
		
		if (la.kind == 93 || la.kind == 100) {
			if (la.kind == 100) {
				lexer.NextToken();

#line  672 "cs.ATG" 
				mod = ParameterModifiers.Ref; 
			} else {
				lexer.NextToken();

#line  673 "cs.ATG" 
				mod = ParameterModifiers.Out; 
			}
		}
		Type(
#line  675 "cs.ATG" 
out type);
		Identifier();

#line  675 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, mod); p.StartLocation = start; p.EndLocation = t.Location; 
	}

	void ParameterArray(
#line  678 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  679 "cs.ATG" 
		TypeReference type; 
		Expect(95);
		Type(
#line  681 "cs.ATG" 
out type);
		Identifier();

#line  681 "cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, ParameterModifiers.Params); 
	}

	void AccessorModifiers(
#line  684 "cs.ATG" 
out ModifierList m) {

#line  685 "cs.ATG" 
		m = new ModifierList(); 
		if (la.kind == 96) {
			lexer.NextToken();

#line  687 "cs.ATG" 
			m.Add(Modifiers.Private, t.Location); 
		} else if (la.kind == 97) {
			lexer.NextToken();

#line  688 "cs.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			if (la.kind == 84) {
				lexer.NextToken();

#line  689 "cs.ATG" 
				m.Add(Modifiers.Internal, t.Location); 
			}
		} else if (la.kind == 84) {
			lexer.NextToken();

#line  690 "cs.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			if (la.kind == 97) {
				lexer.NextToken();

#line  691 "cs.ATG" 
				m.Add(Modifiers.Protected, t.Location); 
			}
		} else SynErr(181);
	}

	void Block(
#line  1296 "cs.ATG" 
out Statement stmt) {
		Expect(16);

#line  1298 "cs.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		blockStmt.StartLocation = t.Location;
		compilationUnit.BlockStart(blockStmt);
		if (!ParseMethodBodies) lexer.SkipCurrentBlock(0);
		
		while (StartOf(25)) {
			Statement();
		}
		while (!(la.kind == 0 || la.kind == 17)) {SynErr(182); lexer.NextToken(); }
		Expect(17);

#line  1306 "cs.ATG" 
		stmt = blockStmt;
		blockStmt.EndLocation = t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void EventAccessorDecls(
#line  1233 "cs.ATG" 
out EventAddRegion addBlock, out EventRemoveRegion removeBlock) {

#line  1234 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		Statement stmt;
		addBlock = null;
		removeBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1241 "cs.ATG" 
out section);

#line  1241 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 130) {

#line  1243 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); 
			AddAccessorDecl(
#line  1244 "cs.ATG" 
out stmt);

#line  1244 "cs.ATG" 
			attributes = new List<AttributeSection>(); addBlock.Block = (BlockStatement)stmt; 
			while (la.kind == 18) {
				AttributeSection(
#line  1245 "cs.ATG" 
out section);

#line  1245 "cs.ATG" 
				attributes.Add(section); 
			}
			RemoveAccessorDecl(
#line  1246 "cs.ATG" 
out stmt);

#line  1246 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; 
		} else if (la.kind == 131) {
			RemoveAccessorDecl(
#line  1248 "cs.ATG" 
out stmt);

#line  1248 "cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; attributes = new List<AttributeSection>(); 
			while (la.kind == 18) {
				AttributeSection(
#line  1249 "cs.ATG" 
out section);

#line  1249 "cs.ATG" 
				attributes.Add(section); 
			}
			AddAccessorDecl(
#line  1250 "cs.ATG" 
out stmt);

#line  1250 "cs.ATG" 
			addBlock = new EventAddRegion(attributes); addBlock.Block = (BlockStatement)stmt; 
		} else SynErr(183);
	}

	void ConstructorInitializer(
#line  1326 "cs.ATG" 
out ConstructorInitializer ci) {

#line  1327 "cs.ATG" 
		Expression expr; ci = new ConstructorInitializer(); 
		Expect(9);
		if (la.kind == 51) {
			lexer.NextToken();

#line  1331 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.Base; 
		} else if (la.kind == 111) {
			lexer.NextToken();

#line  1332 "cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.This; 
		} else SynErr(184);
		Expect(20);
		if (StartOf(26)) {
			Argument(
#line  1335 "cs.ATG" 
out expr);

#line  1335 "cs.ATG" 
			SafeAdd(ci, ci.Arguments, expr); 
			while (la.kind == 14) {
				lexer.NextToken();
				Argument(
#line  1336 "cs.ATG" 
out expr);

#line  1336 "cs.ATG" 
				SafeAdd(ci, ci.Arguments, expr); 
			}
		}
		Expect(21);
	}

	void OverloadableOperator(
#line  1349 "cs.ATG" 
out OverloadableOperatorType op) {

#line  1350 "cs.ATG" 
		op = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 4: {
			lexer.NextToken();

#line  1352 "cs.ATG" 
			op = OverloadableOperatorType.Add; 
			break;
		}
		case 5: {
			lexer.NextToken();

#line  1353 "cs.ATG" 
			op = OverloadableOperatorType.Subtract; 
			break;
		}
		case 24: {
			lexer.NextToken();

#line  1355 "cs.ATG" 
			op = OverloadableOperatorType.Not; 
			break;
		}
		case 27: {
			lexer.NextToken();

#line  1356 "cs.ATG" 
			op = OverloadableOperatorType.BitNot; 
			break;
		}
		case 31: {
			lexer.NextToken();

#line  1358 "cs.ATG" 
			op = OverloadableOperatorType.Increment; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1359 "cs.ATG" 
			op = OverloadableOperatorType.Decrement; 
			break;
		}
		case 113: {
			lexer.NextToken();

#line  1361 "cs.ATG" 
			op = OverloadableOperatorType.IsTrue; 
			break;
		}
		case 72: {
			lexer.NextToken();

#line  1362 "cs.ATG" 
			op = OverloadableOperatorType.IsFalse; 
			break;
		}
		case 6: {
			lexer.NextToken();

#line  1364 "cs.ATG" 
			op = OverloadableOperatorType.Multiply; 
			break;
		}
		case 7: {
			lexer.NextToken();

#line  1365 "cs.ATG" 
			op = OverloadableOperatorType.Divide; 
			break;
		}
		case 8: {
			lexer.NextToken();

#line  1366 "cs.ATG" 
			op = OverloadableOperatorType.Modulus; 
			break;
		}
		case 28: {
			lexer.NextToken();

#line  1368 "cs.ATG" 
			op = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 29: {
			lexer.NextToken();

#line  1369 "cs.ATG" 
			op = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1370 "cs.ATG" 
			op = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1372 "cs.ATG" 
			op = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 33: {
			lexer.NextToken();

#line  1373 "cs.ATG" 
			op = OverloadableOperatorType.Equality; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1374 "cs.ATG" 
			op = OverloadableOperatorType.InEquality; 
			break;
		}
		case 23: {
			lexer.NextToken();

#line  1375 "cs.ATG" 
			op = OverloadableOperatorType.LessThan; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1376 "cs.ATG" 
			op = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1377 "cs.ATG" 
			op = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 22: {
			lexer.NextToken();

#line  1378 "cs.ATG" 
			op = OverloadableOperatorType.GreaterThan; 
			if (la.kind == 22) {
				lexer.NextToken();

#line  1378 "cs.ATG" 
				op = OverloadableOperatorType.ShiftRight; 
			}
			break;
		}
		default: SynErr(185); break;
		}
	}

	void VariableDeclarator(
#line  1288 "cs.ATG" 
FieldDeclaration parentFieldDeclaration) {

#line  1289 "cs.ATG" 
		Expression expr = null; 
		Identifier();

#line  1291 "cs.ATG" 
		VariableDeclaration f = new VariableDeclaration(t.val); f.StartLocation = t.Location; 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1292 "cs.ATG" 
out expr);

#line  1292 "cs.ATG" 
			f.Initializer = expr; 
		}

#line  1293 "cs.ATG" 
		f.EndLocation = t.EndLocation; SafeAdd(parentFieldDeclaration, parentFieldDeclaration.Fields, f); 
	}

	void AccessorDecls(
#line  1180 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1182 "cs.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>(); 
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		ModifierList modifiers = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1189 "cs.ATG" 
out section);

#line  1189 "cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 84 || la.kind == 96 || la.kind == 97) {
			AccessorModifiers(
#line  1190 "cs.ATG" 
out modifiers);
		}
		if (la.kind == 128) {
			GetAccessorDecl(
#line  1192 "cs.ATG" 
out getBlock, attributes);

#line  1193 "cs.ATG" 
			if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(27)) {

#line  1194 "cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1195 "cs.ATG" 
out section);

#line  1195 "cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 84 || la.kind == 96 || la.kind == 97) {
					AccessorModifiers(
#line  1196 "cs.ATG" 
out modifiers);
				}
				SetAccessorDecl(
#line  1197 "cs.ATG" 
out setBlock, attributes);

#line  1198 "cs.ATG" 
				if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (la.kind == 129) {
			SetAccessorDecl(
#line  1201 "cs.ATG" 
out setBlock, attributes);

#line  1202 "cs.ATG" 
			if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(28)) {

#line  1203 "cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1204 "cs.ATG" 
out section);

#line  1204 "cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 84 || la.kind == 96 || la.kind == 97) {
					AccessorModifiers(
#line  1205 "cs.ATG" 
out modifiers);
				}
				GetAccessorDecl(
#line  1206 "cs.ATG" 
out getBlock, attributes);

#line  1207 "cs.ATG" 
				if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (StartOf(19)) {
			Identifier();

#line  1209 "cs.ATG" 
			Error("get or set accessor declaration expected"); 
		} else SynErr(186);
	}

	void InterfaceAccessors(
#line  1254 "cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1256 "cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		getBlock = null; setBlock = null;
		PropertyGetSetRegion lastBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1262 "cs.ATG" 
out section);

#line  1262 "cs.ATG" 
			attributes.Add(section); 
		}

#line  1263 "cs.ATG" 
		Location startLocation = la.Location; 
		if (la.kind == 128) {
			lexer.NextToken();

#line  1265 "cs.ATG" 
			getBlock = new PropertyGetRegion(null, attributes); 
		} else if (la.kind == 129) {
			lexer.NextToken();

#line  1266 "cs.ATG" 
			setBlock = new PropertySetRegion(null, attributes); 
		} else SynErr(187);
		Expect(11);

#line  1269 "cs.ATG" 
		if (getBlock != null) { getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; }
		if (setBlock != null) { setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; }
		attributes = new List<AttributeSection>(); 
		if (la.kind == 18 || la.kind == 128 || la.kind == 129) {
			while (la.kind == 18) {
				AttributeSection(
#line  1273 "cs.ATG" 
out section);

#line  1273 "cs.ATG" 
				attributes.Add(section); 
			}

#line  1274 "cs.ATG" 
			startLocation = la.Location; 
			if (la.kind == 128) {
				lexer.NextToken();

#line  1276 "cs.ATG" 
				if (getBlock != null) Error("get already declared");
				                 else { getBlock = new PropertyGetRegion(null, attributes); lastBlock = getBlock; }
				              
			} else if (la.kind == 129) {
				lexer.NextToken();

#line  1279 "cs.ATG" 
				if (setBlock != null) Error("set already declared");
				                 else { setBlock = new PropertySetRegion(null, attributes); lastBlock = setBlock; }
				              
			} else SynErr(188);
			Expect(11);

#line  1284 "cs.ATG" 
			if (lastBlock != null) { lastBlock.StartLocation = startLocation; lastBlock.EndLocation = t.EndLocation; } 
		}
	}

	void GetAccessorDecl(
#line  1213 "cs.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1214 "cs.ATG" 
		Statement stmt = null; 
		Expect(128);

#line  1217 "cs.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1218 "cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(189);

#line  1219 "cs.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 

#line  1220 "cs.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
	}

	void SetAccessorDecl(
#line  1223 "cs.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1224 "cs.ATG" 
		Statement stmt = null; 
		Expect(129);

#line  1227 "cs.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1228 "cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(190);

#line  1229 "cs.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes); 

#line  1230 "cs.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
	}

	void AddAccessorDecl(
#line  1312 "cs.ATG" 
out Statement stmt) {

#line  1313 "cs.ATG" 
		stmt = null;
		Expect(130);
		Block(
#line  1316 "cs.ATG" 
out stmt);
	}

	void RemoveAccessorDecl(
#line  1319 "cs.ATG" 
out Statement stmt) {

#line  1320 "cs.ATG" 
		stmt = null;
		Expect(131);
		Block(
#line  1323 "cs.ATG" 
out stmt);
	}

	void VariableInitializer(
#line  1341 "cs.ATG" 
out Expression initializerExpression) {

#line  1342 "cs.ATG" 
		TypeReference type = null; Expression expr = null; initializerExpression = null; 
		if (StartOf(6)) {
			Expr(
#line  1344 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 16) {
			CollectionInitializer(
#line  1345 "cs.ATG" 
out initializerExpression);
		} else if (la.kind == 106) {
			lexer.NextToken();
			Type(
#line  1346 "cs.ATG" 
out type);
			Expect(18);
			Expr(
#line  1346 "cs.ATG" 
out expr);
			Expect(19);

#line  1346 "cs.ATG" 
			initializerExpression = new StackAllocExpression(type, expr); 
		} else SynErr(191);
	}

	void Statement() {

#line  1489 "cs.ATG" 
		TypeReference type;
		Expression expr;
		Statement stmt = null;
		Location startPos = la.Location;
		
		while (!(StartOf(29))) {SynErr(192); lexer.NextToken(); }
		if (
#line  1498 "cs.ATG" 
IsLabel()) {
			Identifier();

#line  1498 "cs.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val)); 
			Expect(9);
			Statement();
		} else if (la.kind == 60) {
			lexer.NextToken();
			Type(
#line  1501 "cs.ATG" 
out type);

#line  1501 "cs.ATG" 
			LocalVariableDeclaration var = new LocalVariableDeclaration(type, Modifiers.Const); string ident = null; var.StartLocation = t.Location; 
			Identifier();

#line  1502 "cs.ATG" 
			ident = t.val; Location varStart = t.Location; 
			Expect(3);
			Expr(
#line  1503 "cs.ATG" 
out expr);

#line  1505 "cs.ATG" 
			SafeAdd(var, var.Variables, new VariableDeclaration(ident, expr) {
			StartLocation = varStart,
			EndLocation = t.EndLocation,
			TypeReference = type
			}); 
			 
			while (la.kind == 14) {
				lexer.NextToken();
				Identifier();

#line  1511 "cs.ATG" 
				ident = t.val; 
				Expect(3);
				Expr(
#line  1511 "cs.ATG" 
out expr);

#line  1513 "cs.ATG" 
				SafeAdd(var, var.Variables, new VariableDeclaration(ident, expr) {
				StartLocation = varStart,
				EndLocation = t.EndLocation,
				TypeReference = type
				});
				 
			}
			Expect(11);

#line  1519 "cs.ATG" 
			var.EndLocation = t.EndLocation; compilationUnit.AddChild(var); 
		} else if (
#line  1522 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1522 "cs.ATG" 
out stmt);
			Expect(11);

#line  1522 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else if (StartOf(30)) {
			EmbeddedStatement(
#line  1524 "cs.ATG" 
out stmt);

#line  1524 "cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(193);

#line  1530 "cs.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startPos;
		stmt.EndLocation = t.EndLocation;
		}
		
	}

	void Argument(
#line  1381 "cs.ATG" 
out Expression argumentexpr) {

#line  1383 "cs.ATG" 
		Expression expr;
		FieldDirection fd = FieldDirection.None;
		
		if (la.kind == 93 || la.kind == 100) {
			if (la.kind == 100) {
				lexer.NextToken();

#line  1388 "cs.ATG" 
				fd = FieldDirection.Ref; 
			} else {
				lexer.NextToken();

#line  1389 "cs.ATG" 
				fd = FieldDirection.Out; 
			}
		}
		Expr(
#line  1391 "cs.ATG" 
out expr);

#line  1392 "cs.ATG" 
		argumentexpr = fd != FieldDirection.None ? argumentexpr = new DirectionExpression(fd, expr) : expr; 
	}

	void CollectionInitializer(
#line  1412 "cs.ATG" 
out Expression outExpr) {

#line  1414 "cs.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		
		Expect(16);

#line  1418 "cs.ATG" 
		initializer.StartLocation = t.Location; 
		if (StartOf(31)) {
			VariableInitializer(
#line  1419 "cs.ATG" 
out expr);

#line  1420 "cs.ATG" 
			SafeAdd(initializer, initializer.CreateExpressions, expr); 
			while (
#line  1421 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				VariableInitializer(
#line  1422 "cs.ATG" 
out expr);

#line  1423 "cs.ATG" 
				SafeAdd(initializer, initializer.CreateExpressions, expr); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);

#line  1427 "cs.ATG" 
		initializer.EndLocation = t.Location; outExpr = initializer; 
	}

	void AssignmentOperator(
#line  1395 "cs.ATG" 
out AssignmentOperatorType op) {

#line  1396 "cs.ATG" 
		op = AssignmentOperatorType.None; 
		if (la.kind == 3) {
			lexer.NextToken();

#line  1398 "cs.ATG" 
			op = AssignmentOperatorType.Assign; 
		} else if (la.kind == 38) {
			lexer.NextToken();

#line  1399 "cs.ATG" 
			op = AssignmentOperatorType.Add; 
		} else if (la.kind == 39) {
			lexer.NextToken();

#line  1400 "cs.ATG" 
			op = AssignmentOperatorType.Subtract; 
		} else if (la.kind == 40) {
			lexer.NextToken();

#line  1401 "cs.ATG" 
			op = AssignmentOperatorType.Multiply; 
		} else if (la.kind == 41) {
			lexer.NextToken();

#line  1402 "cs.ATG" 
			op = AssignmentOperatorType.Divide; 
		} else if (la.kind == 42) {
			lexer.NextToken();

#line  1403 "cs.ATG" 
			op = AssignmentOperatorType.Modulus; 
		} else if (la.kind == 43) {
			lexer.NextToken();

#line  1404 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseAnd; 
		} else if (la.kind == 44) {
			lexer.NextToken();

#line  1405 "cs.ATG" 
			op = AssignmentOperatorType.BitwiseOr; 
		} else if (la.kind == 45) {
			lexer.NextToken();

#line  1406 "cs.ATG" 
			op = AssignmentOperatorType.ExclusiveOr; 
		} else if (la.kind == 46) {
			lexer.NextToken();

#line  1407 "cs.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
		} else if (
#line  1408 "cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			Expect(22);
			Expect(35);

#line  1409 "cs.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
		} else SynErr(194);
	}

	void CollectionOrObjectInitializer(
#line  1430 "cs.ATG" 
out Expression outExpr) {

#line  1432 "cs.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		
		Expect(16);

#line  1436 "cs.ATG" 
		initializer.StartLocation = t.Location; 
		if (StartOf(31)) {
			ObjectPropertyInitializerOrVariableInitializer(
#line  1437 "cs.ATG" 
out expr);

#line  1438 "cs.ATG" 
			SafeAdd(initializer, initializer.CreateExpressions, expr); 
			while (
#line  1439 "cs.ATG" 
NotFinalComma()) {
				Expect(14);
				ObjectPropertyInitializerOrVariableInitializer(
#line  1440 "cs.ATG" 
out expr);

#line  1441 "cs.ATG" 
				SafeAdd(initializer, initializer.CreateExpressions, expr); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);

#line  1445 "cs.ATG" 
		initializer.EndLocation = t.Location; outExpr = initializer; 
	}

	void ObjectPropertyInitializerOrVariableInitializer(
#line  1448 "cs.ATG" 
out Expression expr) {

#line  1449 "cs.ATG" 
		expr = null; 
		if (
#line  1451 "cs.ATG" 
IdentAndAsgn()) {
			Identifier();

#line  1453 "cs.ATG" 
			NamedArgumentExpression nae = new NamedArgumentExpression(t.val, null);
			nae.StartLocation = t.Location;
			Expression r = null; 
			Expect(3);
			if (la.kind == 16) {
				CollectionOrObjectInitializer(
#line  1457 "cs.ATG" 
out r);
			} else if (StartOf(31)) {
				VariableInitializer(
#line  1458 "cs.ATG" 
out r);
			} else SynErr(195);

#line  1459 "cs.ATG" 
			nae.Expression = r; nae.EndLocation = t.EndLocation; expr = nae; 
		} else if (StartOf(31)) {
			VariableInitializer(
#line  1461 "cs.ATG" 
out expr);
		} else SynErr(196);
	}

	void LocalVariableDecl(
#line  1465 "cs.ATG" 
out Statement stmt) {

#line  1467 "cs.ATG" 
		TypeReference type;
		VariableDeclaration      var = null;
		LocalVariableDeclaration localVariableDeclaration; 
		Location startPos = la.Location;
		
		Type(
#line  1473 "cs.ATG" 
out type);

#line  1473 "cs.ATG" 
		localVariableDeclaration = new LocalVariableDeclaration(type); localVariableDeclaration.StartLocation = startPos; 
		LocalVariableDeclarator(
#line  1474 "cs.ATG" 
out var);

#line  1474 "cs.ATG" 
		SafeAdd(localVariableDeclaration, localVariableDeclaration.Variables, var); 
		while (la.kind == 14) {
			lexer.NextToken();
			LocalVariableDeclarator(
#line  1475 "cs.ATG" 
out var);

#line  1475 "cs.ATG" 
			SafeAdd(localVariableDeclaration, localVariableDeclaration.Variables, var); 
		}

#line  1476 "cs.ATG" 
		stmt = localVariableDeclaration; stmt.EndLocation = t.EndLocation; 
	}

	void LocalVariableDeclarator(
#line  1479 "cs.ATG" 
out VariableDeclaration var) {

#line  1480 "cs.ATG" 
		Expression expr = null; 
		Identifier();

#line  1482 "cs.ATG" 
		var = new VariableDeclaration(t.val); var.StartLocation = t.Location; 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1483 "cs.ATG" 
out expr);

#line  1483 "cs.ATG" 
			var.Initializer = expr; 
		}

#line  1484 "cs.ATG" 
		var.EndLocation = t.EndLocation; 
	}

	void EmbeddedStatement(
#line  1537 "cs.ATG" 
out Statement statement) {

#line  1539 "cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		

#line  1545 "cs.ATG" 
		Location startLocation = la.Location; 
		if (la.kind == 16) {
			Block(
#line  1547 "cs.ATG" 
out statement);
		} else if (la.kind == 11) {
			lexer.NextToken();

#line  1550 "cs.ATG" 
			statement = new EmptyStatement(); 
		} else if (
#line  1553 "cs.ATG" 
UnCheckedAndLBrace()) {

#line  1553 "cs.ATG" 
			Statement block; bool isChecked = true; 
			if (la.kind == 58) {
				lexer.NextToken();
			} else if (la.kind == 118) {
				lexer.NextToken();

#line  1554 "cs.ATG" 
				isChecked = false;
			} else SynErr(197);
			Block(
#line  1555 "cs.ATG" 
out block);

#line  1555 "cs.ATG" 
			statement = isChecked ? (Statement)new CheckedStatement(block) : (Statement)new UncheckedStatement(block); 
		} else if (la.kind == 79) {
			IfStatement(
#line  1558 "cs.ATG" 
out statement);
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  1560 "cs.ATG" 
			List<SwitchSection> switchSections = new List<SwitchSection>(); 
			Expect(20);
			Expr(
#line  1561 "cs.ATG" 
out expr);
			Expect(21);
			Expect(16);
			SwitchSections(
#line  1562 "cs.ATG" 
switchSections);
			Expect(17);

#line  1564 "cs.ATG" 
			statement = new SwitchStatement(expr, switchSections); 
		} else if (la.kind == 125) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1567 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1568 "cs.ATG" 
out embeddedStatement);

#line  1569 "cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
		} else if (la.kind == 65) {
			lexer.NextToken();
			EmbeddedStatement(
#line  1571 "cs.ATG" 
out embeddedStatement);
			Expect(125);
			Expect(20);
			Expr(
#line  1572 "cs.ATG" 
out expr);
			Expect(21);
			Expect(11);

#line  1573 "cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.End); 
		} else if (la.kind == 76) {
			lexer.NextToken();

#line  1575 "cs.ATG" 
			List<Statement> initializer = null; List<Statement> iterator = null; 
			Expect(20);
			if (StartOf(6)) {
				ForInitializer(
#line  1576 "cs.ATG" 
out initializer);
			}
			Expect(11);
			if (StartOf(6)) {
				Expr(
#line  1577 "cs.ATG" 
out expr);
			}
			Expect(11);
			if (StartOf(6)) {
				ForIterator(
#line  1578 "cs.ATG" 
out iterator);
			}
			Expect(21);
			EmbeddedStatement(
#line  1579 "cs.ATG" 
out embeddedStatement);

#line  1580 "cs.ATG" 
			statement = new ForStatement(initializer, expr, iterator, embeddedStatement); 
		} else if (la.kind == 77) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1582 "cs.ATG" 
out type);
			Identifier();

#line  1582 "cs.ATG" 
			string varName = t.val; 
			Expect(81);
			Expr(
#line  1583 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1584 "cs.ATG" 
out embeddedStatement);

#line  1585 "cs.ATG" 
			statement = new ForeachStatement(type, varName , expr, embeddedStatement); 
		} else if (la.kind == 53) {
			lexer.NextToken();
			Expect(11);

#line  1588 "cs.ATG" 
			statement = new BreakStatement(); 
		} else if (la.kind == 61) {
			lexer.NextToken();
			Expect(11);

#line  1589 "cs.ATG" 
			statement = new ContinueStatement(); 
		} else if (la.kind == 78) {
			GotoStatement(
#line  1590 "cs.ATG" 
out statement);
		} else if (
#line  1592 "cs.ATG" 
IsYieldStatement()) {
			Expect(132);
			if (la.kind == 101) {
				lexer.NextToken();
				Expr(
#line  1593 "cs.ATG" 
out expr);

#line  1593 "cs.ATG" 
				statement = new YieldStatement(new ReturnStatement(expr)); 
			} else if (la.kind == 53) {
				lexer.NextToken();

#line  1594 "cs.ATG" 
				statement = new YieldStatement(new BreakStatement()); 
			} else SynErr(198);
			Expect(11);
		} else if (la.kind == 101) {
			lexer.NextToken();
			if (StartOf(6)) {
				Expr(
#line  1597 "cs.ATG" 
out expr);
			}
			Expect(11);

#line  1597 "cs.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 112) {
			lexer.NextToken();
			if (StartOf(6)) {
				Expr(
#line  1598 "cs.ATG" 
out expr);
			}
			Expect(11);

#line  1598 "cs.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (StartOf(6)) {
			StatementExpr(
#line  1601 "cs.ATG" 
out statement);
			while (!(la.kind == 0 || la.kind == 11)) {SynErr(199); lexer.NextToken(); }
			Expect(11);
		} else if (la.kind == 114) {
			TryStatement(
#line  1604 "cs.ATG" 
out statement);
		} else if (la.kind == 86) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1607 "cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1608 "cs.ATG" 
out embeddedStatement);

#line  1608 "cs.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 121) {

#line  1611 "cs.ATG" 
			Statement resourceAcquisitionStmt = null; 
			lexer.NextToken();
			Expect(20);
			ResourceAcquisition(
#line  1613 "cs.ATG" 
out resourceAcquisitionStmt);
			Expect(21);
			EmbeddedStatement(
#line  1614 "cs.ATG" 
out embeddedStatement);

#line  1614 "cs.ATG" 
			statement = new UsingStatement(resourceAcquisitionStmt, embeddedStatement); 
		} else if (la.kind == 119) {
			lexer.NextToken();
			Block(
#line  1617 "cs.ATG" 
out embeddedStatement);

#line  1617 "cs.ATG" 
			statement = new UnsafeStatement(embeddedStatement); 
		} else if (la.kind == 74) {

#line  1619 "cs.ATG" 
			Statement pointerDeclarationStmt = null; 
			lexer.NextToken();
			Expect(20);
			ResourceAcquisition(
#line  1621 "cs.ATG" 
out pointerDeclarationStmt);
			Expect(21);
			EmbeddedStatement(
#line  1622 "cs.ATG" 
out embeddedStatement);

#line  1622 "cs.ATG" 
			statement = new FixedStatement(pointerDeclarationStmt, embeddedStatement); 
		} else SynErr(200);

#line  1624 "cs.ATG" 
		if (statement != null) {
		statement.StartLocation = startLocation;
		statement.EndLocation = t.EndLocation;
		}
		
	}

	void IfStatement(
#line  1631 "cs.ATG" 
out Statement statement) {

#line  1633 "cs.ATG" 
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		
		Expect(79);
		Expect(20);
		Expr(
#line  1639 "cs.ATG" 
out expr);
		Expect(21);
		EmbeddedStatement(
#line  1640 "cs.ATG" 
out embeddedStatement);

#line  1641 "cs.ATG" 
		Statement elseStatement = null; 
		if (la.kind == 67) {
			lexer.NextToken();
			EmbeddedStatement(
#line  1642 "cs.ATG" 
out elseStatement);
		}

#line  1643 "cs.ATG" 
		statement = elseStatement != null ? new IfElseStatement(expr, embeddedStatement, elseStatement) : new IfElseStatement(expr, embeddedStatement); 

#line  1644 "cs.ATG" 
		if (elseStatement is IfElseStatement && (elseStatement as IfElseStatement).TrueStatement.Count == 1) {
		/* else if-section (otherwise we would have a BlockStatment) */
		(statement as IfElseStatement).ElseIfSections.Add(
		             new ElseIfSection((elseStatement as IfElseStatement).Condition,
		                               (elseStatement as IfElseStatement).TrueStatement[0]));
		(statement as IfElseStatement).ElseIfSections.AddRange((elseStatement as IfElseStatement).ElseIfSections);
		(statement as IfElseStatement).FalseStatement = (elseStatement as IfElseStatement).FalseStatement;
		}
		
	}

	void SwitchSections(
#line  1674 "cs.ATG" 
List<SwitchSection> switchSections) {

#line  1676 "cs.ATG" 
		SwitchSection switchSection = new SwitchSection();
		CaseLabel label;
		
		SwitchLabel(
#line  1680 "cs.ATG" 
out label);

#line  1680 "cs.ATG" 
		SafeAdd(switchSection, switchSection.SwitchLabels, label); 

#line  1681 "cs.ATG" 
		compilationUnit.BlockStart(switchSection); 
		while (StartOf(32)) {
			if (la.kind == 55 || la.kind == 63) {
				SwitchLabel(
#line  1683 "cs.ATG" 
out label);

#line  1684 "cs.ATG" 
				if (label != null) {
				if (switchSection.Children.Count > 0) {
					// open new section
					compilationUnit.BlockEnd(); switchSections.Add(switchSection);
					switchSection = new SwitchSection();
					compilationUnit.BlockStart(switchSection);
				}
				SafeAdd(switchSection, switchSection.SwitchLabels, label);
				}
				
			} else {
				Statement();
			}
		}

#line  1696 "cs.ATG" 
		compilationUnit.BlockEnd(); switchSections.Add(switchSection); 
	}

	void ForInitializer(
#line  1655 "cs.ATG" 
out List<Statement> initializer) {

#line  1657 "cs.ATG" 
		Statement stmt; 
		initializer = new List<Statement>();
		
		if (
#line  1661 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1661 "cs.ATG" 
out stmt);

#line  1661 "cs.ATG" 
			initializer.Add(stmt);
		} else if (StartOf(6)) {
			StatementExpr(
#line  1662 "cs.ATG" 
out stmt);

#line  1662 "cs.ATG" 
			initializer.Add(stmt);
			while (la.kind == 14) {
				lexer.NextToken();
				StatementExpr(
#line  1662 "cs.ATG" 
out stmt);

#line  1662 "cs.ATG" 
				initializer.Add(stmt);
			}
		} else SynErr(201);
	}

	void ForIterator(
#line  1665 "cs.ATG" 
out List<Statement> iterator) {

#line  1667 "cs.ATG" 
		Statement stmt; 
		iterator = new List<Statement>();
		
		StatementExpr(
#line  1671 "cs.ATG" 
out stmt);

#line  1671 "cs.ATG" 
		iterator.Add(stmt);
		while (la.kind == 14) {
			lexer.NextToken();
			StatementExpr(
#line  1671 "cs.ATG" 
out stmt);

#line  1671 "cs.ATG" 
			iterator.Add(stmt); 
		}
	}

	void GotoStatement(
#line  1753 "cs.ATG" 
out Statement stmt) {

#line  1754 "cs.ATG" 
		Expression expr; stmt = null; 
		Expect(78);
		if (StartOf(19)) {
			Identifier();

#line  1758 "cs.ATG" 
			stmt = new GotoStatement(t.val); 
			Expect(11);
		} else if (la.kind == 55) {
			lexer.NextToken();
			Expr(
#line  1759 "cs.ATG" 
out expr);
			Expect(11);

#line  1759 "cs.ATG" 
			stmt = new GotoCaseStatement(expr); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			Expect(11);

#line  1760 "cs.ATG" 
			stmt = new GotoCaseStatement(null); 
		} else SynErr(202);
	}

	void StatementExpr(
#line  1780 "cs.ATG" 
out Statement stmt) {

#line  1781 "cs.ATG" 
		Expression expr; 
		Expr(
#line  1783 "cs.ATG" 
out expr);

#line  1786 "cs.ATG" 
		stmt = new ExpressionStatement(expr); 
	}

	void TryStatement(
#line  1706 "cs.ATG" 
out Statement tryStatement) {

#line  1708 "cs.ATG" 
		Statement blockStmt = null, finallyStmt = null;
		CatchClause catchClause = null;
		List<CatchClause> catchClauses = new List<CatchClause>();
		
		Expect(114);
		Block(
#line  1713 "cs.ATG" 
out blockStmt);
		while (la.kind == 56) {
			CatchClause(
#line  1715 "cs.ATG" 
out catchClause);

#line  1716 "cs.ATG" 
			if (catchClause != null) catchClauses.Add(catchClause); 
		}
		if (la.kind == 73) {
			lexer.NextToken();
			Block(
#line  1718 "cs.ATG" 
out finallyStmt);
		}

#line  1720 "cs.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		if (catchClauses != null) {
			foreach (CatchClause cc in catchClauses) cc.Parent = tryStatement;
		}
		
	}

	void ResourceAcquisition(
#line  1764 "cs.ATG" 
out Statement stmt) {

#line  1766 "cs.ATG" 
		stmt = null;
		Expression expr;
		
		if (
#line  1771 "cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1771 "cs.ATG" 
out stmt);
		} else if (StartOf(6)) {
			Expr(
#line  1772 "cs.ATG" 
out expr);

#line  1776 "cs.ATG" 
			stmt = new ExpressionStatement(expr); 
		} else SynErr(203);
	}

	void SwitchLabel(
#line  1699 "cs.ATG" 
out CaseLabel label) {

#line  1700 "cs.ATG" 
		Expression expr = null; label = null; 
		if (la.kind == 55) {
			lexer.NextToken();
			Expr(
#line  1702 "cs.ATG" 
out expr);
			Expect(9);

#line  1702 "cs.ATG" 
			label =  new CaseLabel(expr); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			Expect(9);

#line  1703 "cs.ATG" 
			label =  new CaseLabel(); 
		} else SynErr(204);
	}

	void CatchClause(
#line  1727 "cs.ATG" 
out CatchClause catchClause) {
		Expect(56);

#line  1729 "cs.ATG" 
		string identifier;
		Statement stmt;
		TypeReference typeRef;
		Location startPos = t.Location;
		catchClause = null;
		
		if (la.kind == 16) {
			Block(
#line  1737 "cs.ATG" 
out stmt);

#line  1737 "cs.ATG" 
			catchClause = new CatchClause(stmt);  
		} else if (la.kind == 20) {
			lexer.NextToken();
			ClassType(
#line  1740 "cs.ATG" 
out typeRef, false);

#line  1740 "cs.ATG" 
			identifier = null; 
			if (StartOf(19)) {
				Identifier();

#line  1741 "cs.ATG" 
				identifier = t.val; 
			}
			Expect(21);
			Block(
#line  1742 "cs.ATG" 
out stmt);

#line  1743 "cs.ATG" 
			catchClause = new CatchClause(typeRef, identifier, stmt); 
		} else SynErr(205);

#line  1746 "cs.ATG" 
		if (catchClause != null) {
		catchClause.StartLocation = startPos;
		catchClause.EndLocation = t.Location;
		}
		
	}

	void UnaryExpr(
#line  1813 "cs.ATG" 
out Expression uExpr) {

#line  1815 "cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		ArrayList expressions = new ArrayList();
		uExpr = null;
		
		while (StartOf(33) || 
#line  1837 "cs.ATG" 
IsTypeCast()) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  1824 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Plus)); 
			} else if (la.kind == 5) {
				lexer.NextToken();

#line  1825 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Minus)); 
			} else if (la.kind == 24) {
				lexer.NextToken();

#line  1826 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Not)); 
			} else if (la.kind == 27) {
				lexer.NextToken();

#line  1827 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitNot)); 
			} else if (la.kind == 6) {
				lexer.NextToken();

#line  1828 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Dereference)); 
			} else if (la.kind == 31) {
				lexer.NextToken();

#line  1829 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Increment)); 
			} else if (la.kind == 32) {
				lexer.NextToken();

#line  1830 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Decrement)); 
			} else if (la.kind == 28) {
				lexer.NextToken();

#line  1831 "cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.AddressOf)); 
			} else {
				Expect(20);
				Type(
#line  1837 "cs.ATG" 
out type);
				Expect(21);

#line  1837 "cs.ATG" 
				expressions.Add(new CastExpression(type)); 
			}
		}
		if (
#line  1842 "cs.ATG" 
LastExpressionIsUnaryMinus(expressions) && IsMostNegativeIntegerWithoutTypeSuffix()) {
			Expect(2);

#line  1845 "cs.ATG" 
			expressions.RemoveAt(expressions.Count - 1);
			if (t.literalValue is uint) {
				expr = new PrimitiveExpression(int.MinValue, int.MinValue.ToString());
			} else if (t.literalValue is ulong) {
				expr = new PrimitiveExpression(long.MinValue, long.MinValue.ToString());
			} else {
				throw new Exception("t.literalValue must be uint or ulong");
			}
			
		} else if (StartOf(34)) {
			PrimaryExpr(
#line  1854 "cs.ATG" 
out expr);
		} else SynErr(206);

#line  1856 "cs.ATG" 
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
#line  2178 "cs.ATG" 
ref Expression outExpr) {

#line  2179 "cs.ATG" 
		Expression expr;   
		ConditionalAndExpr(
#line  2181 "cs.ATG" 
ref outExpr);
		while (la.kind == 26) {
			lexer.NextToken();
			UnaryExpr(
#line  2181 "cs.ATG" 
out expr);
			ConditionalAndExpr(
#line  2181 "cs.ATG" 
ref expr);

#line  2181 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalOr, expr);  
		}
	}

	void PrimaryExpr(
#line  1873 "cs.ATG" 
out Expression pexpr) {

#line  1875 "cs.ATG" 
		TypeReference type = null;
		Expression expr;
		pexpr = null;
		

#line  1880 "cs.ATG" 
		Location startLocation = la.Location; 
		if (la.kind == 113) {
			lexer.NextToken();

#line  1882 "cs.ATG" 
			pexpr = new PrimitiveExpression(true, "true");  
		} else if (la.kind == 72) {
			lexer.NextToken();

#line  1883 "cs.ATG" 
			pexpr = new PrimitiveExpression(false, "false"); 
		} else if (la.kind == 90) {
			lexer.NextToken();

#line  1884 "cs.ATG" 
			pexpr = new PrimitiveExpression(null, "null");  
		} else if (la.kind == 2) {
			lexer.NextToken();

#line  1885 "cs.ATG" 
			pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
		} else if (
#line  1886 "cs.ATG" 
StartOfQueryExpression()) {
			QueryExpression(
#line  1887 "cs.ATG" 
out pexpr);
		} else if (
#line  1888 "cs.ATG" 
IdentAndDoubleColon()) {
			Identifier();

#line  1889 "cs.ATG" 
			type = new TypeReference(t.val); 
			Expect(10);

#line  1890 "cs.ATG" 
			pexpr = new TypeReferenceExpression(type); 
			Identifier();

#line  1891 "cs.ATG" 
			if (type.Type == "global") { type.IsGlobal = true; type.Type = t.val ?? "?"; } else type.Type += "." + (t.val ?? "?"); 
		} else if (StartOf(19)) {
			Identifier();

#line  1895 "cs.ATG" 
			pexpr = new IdentifierExpression(t.val); 
			if (la.kind == 48 || 
#line  1898 "cs.ATG" 
IsGenericInSimpleNameOrMemberAccess()) {
				if (la.kind == 48) {
					ShortedLambdaExpression(
#line  1897 "cs.ATG" 
(IdentifierExpression)pexpr, out pexpr);
				} else {

#line  1899 "cs.ATG" 
					List<TypeReference> typeList; 
					TypeArgumentList(
#line  1900 "cs.ATG" 
out typeList, false);

#line  1901 "cs.ATG" 
					((IdentifierExpression)pexpr).TypeArguments = typeList; 
				}
			}
		} else if (
#line  1903 "cs.ATG" 
IsLambdaExpression()) {
			LambdaExpression(
#line  1904 "cs.ATG" 
out pexpr);
		} else if (la.kind == 20) {
			lexer.NextToken();
			Expr(
#line  1907 "cs.ATG" 
out expr);
			Expect(21);

#line  1907 "cs.ATG" 
			pexpr = new ParenthesizedExpression(expr); 
		} else if (StartOf(35)) {

#line  1910 "cs.ATG" 
			string val = null; 
			switch (la.kind) {
			case 52: {
				lexer.NextToken();

#line  1911 "cs.ATG" 
				val = "System.Boolean"; 
				break;
			}
			case 54: {
				lexer.NextToken();

#line  1912 "cs.ATG" 
				val = "System.Byte"; 
				break;
			}
			case 57: {
				lexer.NextToken();

#line  1913 "cs.ATG" 
				val = "System.Char"; 
				break;
			}
			case 62: {
				lexer.NextToken();

#line  1914 "cs.ATG" 
				val = "System.Decimal"; 
				break;
			}
			case 66: {
				lexer.NextToken();

#line  1915 "cs.ATG" 
				val = "System.Double"; 
				break;
			}
			case 75: {
				lexer.NextToken();

#line  1916 "cs.ATG" 
				val = "System.Single"; 
				break;
			}
			case 82: {
				lexer.NextToken();

#line  1917 "cs.ATG" 
				val = "System.Int32"; 
				break;
			}
			case 87: {
				lexer.NextToken();

#line  1918 "cs.ATG" 
				val = "System.Int64"; 
				break;
			}
			case 91: {
				lexer.NextToken();

#line  1919 "cs.ATG" 
				val = "System.Object"; 
				break;
			}
			case 102: {
				lexer.NextToken();

#line  1920 "cs.ATG" 
				val = "System.SByte"; 
				break;
			}
			case 104: {
				lexer.NextToken();

#line  1921 "cs.ATG" 
				val = "System.Int16"; 
				break;
			}
			case 108: {
				lexer.NextToken();

#line  1922 "cs.ATG" 
				val = "System.String"; 
				break;
			}
			case 116: {
				lexer.NextToken();

#line  1923 "cs.ATG" 
				val = "System.UInt32"; 
				break;
			}
			case 117: {
				lexer.NextToken();

#line  1924 "cs.ATG" 
				val = "System.UInt64"; 
				break;
			}
			case 120: {
				lexer.NextToken();

#line  1925 "cs.ATG" 
				val = "System.UInt16"; 
				break;
			}
			case 123: {
				lexer.NextToken();

#line  1926 "cs.ATG" 
				val = "System.Void"; 
				break;
			}
			}

#line  1928 "cs.ATG" 
			pexpr = new TypeReferenceExpression(new TypeReference(val, true)) { StartLocation = t.Location, EndLocation = t.EndLocation }; 
		} else if (la.kind == 111) {
			lexer.NextToken();

#line  1931 "cs.ATG" 
			pexpr = new ThisReferenceExpression(); pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation; 
		} else if (la.kind == 51) {
			lexer.NextToken();

#line  1933 "cs.ATG" 
			pexpr = new BaseReferenceExpression(); pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation; 
		} else if (la.kind == 89) {
			NewExpression(
#line  1936 "cs.ATG" 
out pexpr);
		} else if (la.kind == 115) {
			lexer.NextToken();
			Expect(20);
			if (
#line  1940 "cs.ATG" 
NotVoidPointer()) {
				Expect(123);

#line  1940 "cs.ATG" 
				type = new TypeReference("System.Void", true); 
			} else if (StartOf(10)) {
				TypeWithRestriction(
#line  1941 "cs.ATG" 
out type, true, true);
			} else SynErr(207);
			Expect(21);

#line  1943 "cs.ATG" 
			pexpr = new TypeOfExpression(type); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1945 "cs.ATG" 
out type);
			Expect(21);

#line  1945 "cs.ATG" 
			pexpr = new DefaultValueExpression(type); 
		} else if (la.kind == 105) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1946 "cs.ATG" 
out type);
			Expect(21);

#line  1946 "cs.ATG" 
			pexpr = new SizeOfExpression(type); 
		} else if (la.kind == 58) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1947 "cs.ATG" 
out expr);
			Expect(21);

#line  1947 "cs.ATG" 
			pexpr = new CheckedExpression(expr); 
		} else if (la.kind == 118) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1948 "cs.ATG" 
out expr);
			Expect(21);

#line  1948 "cs.ATG" 
			pexpr = new UncheckedExpression(expr); 
		} else if (la.kind == 64) {
			lexer.NextToken();
			AnonymousMethodExpr(
#line  1949 "cs.ATG" 
out expr);

#line  1949 "cs.ATG" 
			pexpr = expr; 
		} else SynErr(208);

#line  1951 "cs.ATG" 
		if (pexpr != null) {
		if (pexpr.StartLocation.IsEmpty)
			pexpr.StartLocation = startLocation;
		if (pexpr.EndLocation.IsEmpty)
			pexpr.EndLocation = t.EndLocation;
		}
		
		while (StartOf(36)) {
			if (la.kind == 31 || la.kind == 32) {

#line  1959 "cs.ATG" 
				startLocation = la.Location; 
				if (la.kind == 31) {
					lexer.NextToken();

#line  1961 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostIncrement); 
				} else if (la.kind == 32) {
					lexer.NextToken();

#line  1962 "cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostDecrement); 
				} else SynErr(209);
			} else if (la.kind == 47) {
				PointerMemberAccess(
#line  1965 "cs.ATG" 
out pexpr, pexpr);
			} else if (la.kind == 15) {
				MemberAccess(
#line  1966 "cs.ATG" 
out pexpr, pexpr);
			} else if (la.kind == 20) {
				lexer.NextToken();

#line  1970 "cs.ATG" 
				List<Expression> parameters = new List<Expression>(); 

#line  1971 "cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters); 
				if (StartOf(26)) {
					Argument(
#line  1972 "cs.ATG" 
out expr);

#line  1972 "cs.ATG" 
					SafeAdd(pexpr, parameters, expr); 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  1973 "cs.ATG" 
out expr);

#line  1973 "cs.ATG" 
						SafeAdd(pexpr, parameters, expr); 
					}
				}
				Expect(21);
			} else {

#line  1979 "cs.ATG" 
				List<Expression> indices = new List<Expression>();
				pexpr = new IndexerExpression(pexpr, indices);
				
				lexer.NextToken();
				Expr(
#line  1982 "cs.ATG" 
out expr);

#line  1982 "cs.ATG" 
				SafeAdd(pexpr, indices, expr); 
				while (la.kind == 14) {
					lexer.NextToken();
					Expr(
#line  1983 "cs.ATG" 
out expr);

#line  1983 "cs.ATG" 
					SafeAdd(pexpr, indices, expr); 
				}
				Expect(19);

#line  1986 "cs.ATG" 
				if (pexpr != null) {
				pexpr.StartLocation = startLocation;
				pexpr.EndLocation = t.EndLocation;
				}
				
			}
		}
	}

	void QueryExpression(
#line  2416 "cs.ATG" 
out Expression outExpr) {

#line  2417 "cs.ATG" 
		QueryExpression q = new QueryExpression(); outExpr = q; q.StartLocation = la.Location; 
		QueryExpressionFromClause fromClause;
		
		QueryExpressionFromClause(
#line  2421 "cs.ATG" 
out fromClause);

#line  2421 "cs.ATG" 
		q.FromClause = fromClause; 
		QueryExpressionBody(
#line  2422 "cs.ATG" 
ref q);

#line  2423 "cs.ATG" 
		q.EndLocation = t.EndLocation; 
		outExpr = q; /* set outExpr to q again if QueryExpressionBody changed it (can happen with 'into' clauses) */ 
		
	}

	void ShortedLambdaExpression(
#line  2098 "cs.ATG" 
IdentifierExpression ident, out Expression pexpr) {

#line  2099 "cs.ATG" 
		LambdaExpression lambda = new LambdaExpression(); pexpr = lambda; 
		Expect(48);

#line  2104 "cs.ATG" 
		lambda.StartLocation = ident.StartLocation;
		SafeAdd(lambda, lambda.Parameters, new ParameterDeclarationExpression(null, ident.Identifier));
		lambda.Parameters[0].StartLocation = ident.StartLocation;
		lambda.Parameters[0].EndLocation = ident.EndLocation;
		
		LambdaExpressionBody(
#line  2109 "cs.ATG" 
lambda);
	}

	void TypeArgumentList(
#line  2350 "cs.ATG" 
out List<TypeReference> types, bool canBeUnbound) {

#line  2352 "cs.ATG" 
		types = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(23);
		if (
#line  2357 "cs.ATG" 
canBeUnbound && (la.kind == Tokens.GreaterThan || la.kind == Tokens.Comma)) {

#line  2358 "cs.ATG" 
			types.Add(TypeReference.Null); 
			while (la.kind == 14) {
				lexer.NextToken();

#line  2359 "cs.ATG" 
				types.Add(TypeReference.Null); 
			}
		} else if (StartOf(10)) {
			Type(
#line  2360 "cs.ATG" 
out type);

#line  2360 "cs.ATG" 
			if (type != null) { types.Add(type); } 
			while (la.kind == 14) {
				lexer.NextToken();
				Type(
#line  2361 "cs.ATG" 
out type);

#line  2361 "cs.ATG" 
				if (type != null) { types.Add(type); } 
			}
		} else SynErr(210);
		Expect(22);
	}

	void LambdaExpression(
#line  2078 "cs.ATG" 
out Expression outExpr) {

#line  2080 "cs.ATG" 
		LambdaExpression lambda = new LambdaExpression();
		lambda.StartLocation = la.Location;
		ParameterDeclarationExpression p;
		outExpr = lambda;
		
		Expect(20);
		if (StartOf(18)) {
			LambdaExpressionParameter(
#line  2088 "cs.ATG" 
out p);

#line  2088 "cs.ATG" 
			SafeAdd(lambda, lambda.Parameters, p); 
			while (la.kind == 14) {
				lexer.NextToken();
				LambdaExpressionParameter(
#line  2090 "cs.ATG" 
out p);

#line  2090 "cs.ATG" 
				SafeAdd(lambda, lambda.Parameters, p); 
			}
		}
		Expect(21);
		Expect(48);
		LambdaExpressionBody(
#line  2095 "cs.ATG" 
lambda);
	}

	void NewExpression(
#line  2025 "cs.ATG" 
out Expression pexpr) {

#line  2026 "cs.ATG" 
		pexpr = null;
		List<Expression> parameters = new List<Expression>();
		TypeReference type = null;
		Expression expr;
		
		Expect(89);
		if (StartOf(10)) {
			NonArrayType(
#line  2033 "cs.ATG" 
out type);
		}
		if (la.kind == 16 || la.kind == 20) {
			if (la.kind == 20) {

#line  2039 "cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters); 
				lexer.NextToken();

#line  2040 "cs.ATG" 
				if (type == null) Error("Cannot use an anonymous type with arguments for the constructor"); 
				if (StartOf(26)) {
					Argument(
#line  2041 "cs.ATG" 
out expr);

#line  2041 "cs.ATG" 
					SafeAdd(oce, parameters, expr); 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  2042 "cs.ATG" 
out expr);

#line  2042 "cs.ATG" 
						SafeAdd(oce, parameters, expr); 
					}
				}
				Expect(21);

#line  2044 "cs.ATG" 
				pexpr = oce; 
				if (la.kind == 16) {
					CollectionOrObjectInitializer(
#line  2045 "cs.ATG" 
out expr);

#line  2045 "cs.ATG" 
					oce.ObjectInitializer = (CollectionInitializerExpression)expr; 
				}
			} else {

#line  2046 "cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters); 
				CollectionOrObjectInitializer(
#line  2047 "cs.ATG" 
out expr);

#line  2047 "cs.ATG" 
				oce.ObjectInitializer = (CollectionInitializerExpression)expr; 

#line  2048 "cs.ATG" 
				pexpr = oce; 
			}
		} else if (la.kind == 18) {
			lexer.NextToken();

#line  2053 "cs.ATG" 
			ArrayCreateExpression ace = new ArrayCreateExpression(type);
			/* we must not change RankSpecifier on the null type reference*/
			if (ace.CreateType.IsNull) { ace.CreateType = new TypeReference(""); }
			pexpr = ace;
			int dims = 0; List<int> ranks = new List<int>();
			
			if (la.kind == 14 || la.kind == 19) {
				while (la.kind == 14) {
					lexer.NextToken();

#line  2060 "cs.ATG" 
					dims += 1; 
				}
				Expect(19);

#line  2061 "cs.ATG" 
				ranks.Add(dims); dims = 0; 
				while (la.kind == 18) {
					lexer.NextToken();
					while (la.kind == 14) {
						lexer.NextToken();

#line  2062 "cs.ATG" 
						++dims; 
					}
					Expect(19);

#line  2062 "cs.ATG" 
					ranks.Add(dims); dims = 0; 
				}

#line  2063 "cs.ATG" 
				ace.CreateType.RankSpecifier = ranks.ToArray(); 
				CollectionInitializer(
#line  2064 "cs.ATG" 
out expr);

#line  2064 "cs.ATG" 
				ace.ArrayInitializer = (CollectionInitializerExpression)expr; 
			} else if (StartOf(6)) {
				Expr(
#line  2065 "cs.ATG" 
out expr);

#line  2065 "cs.ATG" 
				if (expr != null) parameters.Add(expr); 
				while (la.kind == 14) {
					lexer.NextToken();

#line  2066 "cs.ATG" 
					dims += 1; 
					Expr(
#line  2067 "cs.ATG" 
out expr);

#line  2067 "cs.ATG" 
					if (expr != null) parameters.Add(expr); 
				}
				Expect(19);

#line  2069 "cs.ATG" 
				ranks.Add(dims); ace.Arguments = parameters; dims = 0; 
				while (la.kind == 18) {
					lexer.NextToken();
					while (la.kind == 14) {
						lexer.NextToken();

#line  2070 "cs.ATG" 
						++dims; 
					}
					Expect(19);

#line  2070 "cs.ATG" 
					ranks.Add(dims); dims = 0; 
				}

#line  2071 "cs.ATG" 
				ace.CreateType.RankSpecifier = ranks.ToArray(); 
				if (la.kind == 16) {
					CollectionInitializer(
#line  2072 "cs.ATG" 
out expr);

#line  2072 "cs.ATG" 
					ace.ArrayInitializer = (CollectionInitializerExpression)expr; 
				}
			} else SynErr(211);
		} else SynErr(212);
	}

	void AnonymousMethodExpr(
#line  2145 "cs.ATG" 
out Expression outExpr) {

#line  2147 "cs.ATG" 
		AnonymousMethodExpression expr = new AnonymousMethodExpression();
		expr.StartLocation = t.Location;
		BlockStatement stmt;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		outExpr = expr;
		
		if (la.kind == 20) {
			lexer.NextToken();
			if (StartOf(11)) {
				FormalParameterList(
#line  2156 "cs.ATG" 
p);

#line  2156 "cs.ATG" 
				expr.Parameters = p; 
			}
			Expect(21);

#line  2158 "cs.ATG" 
			expr.HasParameterList = true; 
		}
		BlockInsideExpression(
#line  2160 "cs.ATG" 
out stmt);

#line  2160 "cs.ATG" 
		expr.Body  = stmt; 

#line  2161 "cs.ATG" 
		expr.EndLocation = t.Location; 
	}

	void PointerMemberAccess(
#line  2013 "cs.ATG" 
out Expression expr, Expression target) {

#line  2014 "cs.ATG" 
		List<TypeReference> typeList; 
		Expect(47);
		Identifier();

#line  2018 "cs.ATG" 
		expr = new PointerReferenceExpression(target, t.val); expr.StartLocation = t.Location; expr.EndLocation = t.EndLocation; 
		if (
#line  2019 "cs.ATG" 
IsGenericInSimpleNameOrMemberAccess()) {
			TypeArgumentList(
#line  2020 "cs.ATG" 
out typeList, false);

#line  2021 "cs.ATG" 
			((MemberReferenceExpression)expr).TypeArguments = typeList; 
		}
	}

	void MemberAccess(
#line  1994 "cs.ATG" 
out Expression expr, Expression target) {

#line  1995 "cs.ATG" 
		List<TypeReference> typeList; 

#line  1997 "cs.ATG" 
		if (ShouldConvertTargetExpressionToTypeReference(target)) {
		TypeReference type = GetTypeReferenceFromExpression(target);
		if (type != null) {
			target = new TypeReferenceExpression(type) { StartLocation = t.Location, EndLocation = t.EndLocation };
		}
		}
		
		Expect(15);
		Identifier();

#line  2006 "cs.ATG" 
		expr = new MemberReferenceExpression(target, t.val); expr.StartLocation = t.Location; expr.EndLocation = t.EndLocation; 
		if (
#line  2007 "cs.ATG" 
IsGenericInSimpleNameOrMemberAccess()) {
			TypeArgumentList(
#line  2008 "cs.ATG" 
out typeList, false);

#line  2009 "cs.ATG" 
			((MemberReferenceExpression)expr).TypeArguments = typeList; 
		}
	}

	void LambdaExpressionParameter(
#line  2112 "cs.ATG" 
out ParameterDeclarationExpression p) {

#line  2113 "cs.ATG" 
		Location start = la.Location; p = null;
		TypeReference type;
		ParameterModifiers mod = ParameterModifiers.In;
		
		if (
#line  2118 "cs.ATG" 
Peek(1).kind == Tokens.Comma || Peek(1).kind == Tokens.CloseParenthesis) {
			Identifier();

#line  2120 "cs.ATG" 
			p = new ParameterDeclarationExpression(null, t.val);
			p.StartLocation = start; p.EndLocation = t.EndLocation;
			
		} else if (StartOf(18)) {
			if (la.kind == 93 || la.kind == 100) {
				if (la.kind == 100) {
					lexer.NextToken();

#line  2123 "cs.ATG" 
					mod = ParameterModifiers.Ref; 
				} else {
					lexer.NextToken();

#line  2124 "cs.ATG" 
					mod = ParameterModifiers.Out; 
				}
			}
			Type(
#line  2126 "cs.ATG" 
out type);
			Identifier();

#line  2128 "cs.ATG" 
			p = new ParameterDeclarationExpression(type, t.val, mod);
			p.StartLocation = start; p.EndLocation = t.EndLocation;
			
		} else SynErr(213);
	}

	void LambdaExpressionBody(
#line  2134 "cs.ATG" 
LambdaExpression lambda) {

#line  2135 "cs.ATG" 
		Expression expr; BlockStatement stmt; 
		if (la.kind == 16) {
			BlockInsideExpression(
#line  2138 "cs.ATG" 
out stmt);

#line  2138 "cs.ATG" 
			lambda.StatementBody = stmt; 
		} else if (StartOf(6)) {
			Expr(
#line  2139 "cs.ATG" 
out expr);

#line  2139 "cs.ATG" 
			lambda.ExpressionBody = expr; 
		} else SynErr(214);

#line  2141 "cs.ATG" 
		lambda.EndLocation = t.EndLocation; 

#line  2142 "cs.ATG" 
		lambda.ExtendedEndLocation = la.Location; 
	}

	void BlockInsideExpression(
#line  2164 "cs.ATG" 
out BlockStatement outStmt) {

#line  2165 "cs.ATG" 
		Statement stmt = null; outStmt = null; 

#line  2169 "cs.ATG" 
		if (compilationUnit != null) { 
		Block(
#line  2170 "cs.ATG" 
out stmt);

#line  2170 "cs.ATG" 
		outStmt = (BlockStatement)stmt; 

#line  2171 "cs.ATG" 
		} else { 
		Expect(16);

#line  2173 "cs.ATG" 
		lexer.SkipCurrentBlock(0); 
		Expect(17);

#line  2175 "cs.ATG" 
		} 
	}

	void ConditionalAndExpr(
#line  2184 "cs.ATG" 
ref Expression outExpr) {

#line  2185 "cs.ATG" 
		Expression expr; 
		InclusiveOrExpr(
#line  2187 "cs.ATG" 
ref outExpr);
		while (la.kind == 25) {
			lexer.NextToken();
			UnaryExpr(
#line  2187 "cs.ATG" 
out expr);
			InclusiveOrExpr(
#line  2187 "cs.ATG" 
ref expr);

#line  2187 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalAnd, expr);  
		}
	}

	void InclusiveOrExpr(
#line  2190 "cs.ATG" 
ref Expression outExpr) {

#line  2191 "cs.ATG" 
		Expression expr; 
		ExclusiveOrExpr(
#line  2193 "cs.ATG" 
ref outExpr);
		while (la.kind == 29) {
			lexer.NextToken();
			UnaryExpr(
#line  2193 "cs.ATG" 
out expr);
			ExclusiveOrExpr(
#line  2193 "cs.ATG" 
ref expr);

#line  2193 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseOr, expr);  
		}
	}

	void ExclusiveOrExpr(
#line  2196 "cs.ATG" 
ref Expression outExpr) {

#line  2197 "cs.ATG" 
		Expression expr; 
		AndExpr(
#line  2199 "cs.ATG" 
ref outExpr);
		while (la.kind == 30) {
			lexer.NextToken();
			UnaryExpr(
#line  2199 "cs.ATG" 
out expr);
			AndExpr(
#line  2199 "cs.ATG" 
ref expr);

#line  2199 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.ExclusiveOr, expr);  
		}
	}

	void AndExpr(
#line  2202 "cs.ATG" 
ref Expression outExpr) {

#line  2203 "cs.ATG" 
		Expression expr; 
		EqualityExpr(
#line  2205 "cs.ATG" 
ref outExpr);
		while (la.kind == 28) {
			lexer.NextToken();
			UnaryExpr(
#line  2205 "cs.ATG" 
out expr);
			EqualityExpr(
#line  2205 "cs.ATG" 
ref expr);

#line  2205 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseAnd, expr);  
		}
	}

	void EqualityExpr(
#line  2208 "cs.ATG" 
ref Expression outExpr) {

#line  2210 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		RelationalExpr(
#line  2214 "cs.ATG" 
ref outExpr);
		while (la.kind == 33 || la.kind == 34) {
			if (la.kind == 34) {
				lexer.NextToken();

#line  2217 "cs.ATG" 
				op = BinaryOperatorType.InEquality; 
			} else {
				lexer.NextToken();

#line  2218 "cs.ATG" 
				op = BinaryOperatorType.Equality; 
			}
			UnaryExpr(
#line  2220 "cs.ATG" 
out expr);
			RelationalExpr(
#line  2220 "cs.ATG" 
ref expr);

#line  2220 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void RelationalExpr(
#line  2224 "cs.ATG" 
ref Expression outExpr) {

#line  2226 "cs.ATG" 
		TypeReference type;
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  2231 "cs.ATG" 
ref outExpr);
		while (StartOf(37)) {
			if (StartOf(38)) {
				if (la.kind == 23) {
					lexer.NextToken();

#line  2233 "cs.ATG" 
					op = BinaryOperatorType.LessThan; 
				} else if (la.kind == 22) {
					lexer.NextToken();

#line  2234 "cs.ATG" 
					op = BinaryOperatorType.GreaterThan; 
				} else if (la.kind == 36) {
					lexer.NextToken();

#line  2235 "cs.ATG" 
					op = BinaryOperatorType.LessThanOrEqual; 
				} else if (la.kind == 35) {
					lexer.NextToken();

#line  2236 "cs.ATG" 
					op = BinaryOperatorType.GreaterThanOrEqual; 
				} else SynErr(215);
				UnaryExpr(
#line  2238 "cs.ATG" 
out expr);
				ShiftExpr(
#line  2239 "cs.ATG" 
ref expr);

#line  2240 "cs.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
			} else {
				if (la.kind == 85) {
					lexer.NextToken();
					TypeWithRestriction(
#line  2243 "cs.ATG" 
out type, false, false);
					if (
#line  2244 "cs.ATG" 
la.kind == Tokens.Question && !IsPossibleExpressionStart(Peek(1).kind)) {
						NullableQuestionMark(
#line  2245 "cs.ATG" 
ref type);
					}

#line  2246 "cs.ATG" 
					outExpr = new TypeOfIsExpression(outExpr, type); 
				} else if (la.kind == 50) {
					lexer.NextToken();
					TypeWithRestriction(
#line  2248 "cs.ATG" 
out type, false, false);
					if (
#line  2249 "cs.ATG" 
la.kind == Tokens.Question && !IsPossibleExpressionStart(Peek(1).kind)) {
						NullableQuestionMark(
#line  2250 "cs.ATG" 
ref type);
					}

#line  2251 "cs.ATG" 
					outExpr = new CastExpression(type, outExpr, CastType.TryCast); 
				} else SynErr(216);
			}
		}
	}

	void ShiftExpr(
#line  2256 "cs.ATG" 
ref Expression outExpr) {

#line  2258 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		AdditiveExpr(
#line  2262 "cs.ATG" 
ref outExpr);
		while (la.kind == 37 || 
#line  2265 "cs.ATG" 
IsShiftRight()) {
			if (la.kind == 37) {
				lexer.NextToken();

#line  2264 "cs.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				Expect(22);
				Expect(22);

#line  2266 "cs.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			UnaryExpr(
#line  2269 "cs.ATG" 
out expr);
			AdditiveExpr(
#line  2269 "cs.ATG" 
ref expr);

#line  2269 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AdditiveExpr(
#line  2273 "cs.ATG" 
ref Expression outExpr) {

#line  2275 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		MultiplicativeExpr(
#line  2279 "cs.ATG" 
ref outExpr);
		while (la.kind == 4 || la.kind == 5) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  2282 "cs.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  2283 "cs.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			UnaryExpr(
#line  2285 "cs.ATG" 
out expr);
			MultiplicativeExpr(
#line  2285 "cs.ATG" 
ref expr);

#line  2285 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void MultiplicativeExpr(
#line  2289 "cs.ATG" 
ref Expression outExpr) {

#line  2291 "cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		while (la.kind == 6 || la.kind == 7 || la.kind == 8) {
			if (la.kind == 6) {
				lexer.NextToken();

#line  2297 "cs.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else if (la.kind == 7) {
				lexer.NextToken();

#line  2298 "cs.ATG" 
				op = BinaryOperatorType.Divide; 
			} else {
				lexer.NextToken();

#line  2299 "cs.ATG" 
				op = BinaryOperatorType.Modulus; 
			}
			UnaryExpr(
#line  2301 "cs.ATG" 
out expr);

#line  2301 "cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void TypeParameterConstraintsClauseBase(
#line  2407 "cs.ATG" 
out TypeReference type) {

#line  2408 "cs.ATG" 
		TypeReference t; type = null; 
		if (la.kind == 109) {
			lexer.NextToken();

#line  2410 "cs.ATG" 
			type = TypeReference.StructConstraint; 
		} else if (la.kind == 59) {
			lexer.NextToken();

#line  2411 "cs.ATG" 
			type = TypeReference.ClassConstraint; 
		} else if (la.kind == 89) {
			lexer.NextToken();
			Expect(20);
			Expect(21);

#line  2412 "cs.ATG" 
			type = TypeReference.NewConstraint; 
		} else if (StartOf(10)) {
			Type(
#line  2413 "cs.ATG" 
out t);

#line  2413 "cs.ATG" 
			type = t; 
		} else SynErr(217);
	}

	void QueryExpressionFromClause(
#line  2428 "cs.ATG" 
out QueryExpressionFromClause fc) {

#line  2429 "cs.ATG" 
		fc = new QueryExpressionFromClause(); fc.StartLocation = la.Location; 
		
		Expect(137);
		QueryExpressionFromOrJoinClause(
#line  2433 "cs.ATG" 
fc);

#line  2434 "cs.ATG" 
		fc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionBody(
#line  2464 "cs.ATG" 
ref QueryExpression q) {

#line  2465 "cs.ATG" 
		QueryExpressionFromClause fromClause;     QueryExpressionWhereClause whereClause;
		QueryExpressionLetClause letClause;       QueryExpressionJoinClause joinClause;
		QueryExpressionOrderClause orderClause;
		QueryExpressionSelectClause selectClause; QueryExpressionGroupClause groupClause;
		
		while (StartOf(39)) {
			if (la.kind == 137) {
				QueryExpressionFromClause(
#line  2471 "cs.ATG" 
out fromClause);

#line  2471 "cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, fromClause); 
			} else if (la.kind == 127) {
				QueryExpressionWhereClause(
#line  2472 "cs.ATG" 
out whereClause);

#line  2472 "cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, whereClause); 
			} else if (la.kind == 141) {
				QueryExpressionLetClause(
#line  2473 "cs.ATG" 
out letClause);

#line  2473 "cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, letClause); 
			} else if (la.kind == 142) {
				QueryExpressionJoinClause(
#line  2474 "cs.ATG" 
out joinClause);

#line  2474 "cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, joinClause); 
			} else {
				QueryExpressionOrderByClause(
#line  2475 "cs.ATG" 
out orderClause);

#line  2475 "cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, orderClause); 
			}
		}
		if (la.kind == 133) {
			QueryExpressionSelectClause(
#line  2477 "cs.ATG" 
out selectClause);

#line  2477 "cs.ATG" 
			q.SelectOrGroupClause = selectClause; 
		} else if (la.kind == 134) {
			QueryExpressionGroupClause(
#line  2478 "cs.ATG" 
out groupClause);

#line  2478 "cs.ATG" 
			q.SelectOrGroupClause = groupClause; 
		} else SynErr(218);
		if (la.kind == 136) {
			QueryExpressionIntoClause(
#line  2480 "cs.ATG" 
ref q);
		}
	}

	void QueryExpressionFromOrJoinClause(
#line  2454 "cs.ATG" 
QueryExpressionFromOrJoinClause fjc) {

#line  2455 "cs.ATG" 
		TypeReference type; Expression expr; 

#line  2457 "cs.ATG" 
		fjc.Type = null; 
		if (
#line  2458 "cs.ATG" 
IsLocalVarDecl()) {
			Type(
#line  2458 "cs.ATG" 
out type);

#line  2458 "cs.ATG" 
			fjc.Type = type; 
		}
		Identifier();

#line  2459 "cs.ATG" 
		fjc.Identifier = t.val; 
		Expect(81);
		Expr(
#line  2461 "cs.ATG" 
out expr);

#line  2461 "cs.ATG" 
		fjc.InExpression = expr; 
	}

	void QueryExpressionJoinClause(
#line  2437 "cs.ATG" 
out QueryExpressionJoinClause jc) {

#line  2438 "cs.ATG" 
		jc = new QueryExpressionJoinClause(); jc.StartLocation = la.Location; 
		Expression expr;
		
		Expect(142);
		QueryExpressionFromOrJoinClause(
#line  2443 "cs.ATG" 
jc);
		Expect(143);
		Expr(
#line  2445 "cs.ATG" 
out expr);

#line  2445 "cs.ATG" 
		jc.OnExpression = expr; 
		Expect(144);
		Expr(
#line  2447 "cs.ATG" 
out expr);

#line  2447 "cs.ATG" 
		jc.EqualsExpression = expr; 
		if (la.kind == 136) {
			lexer.NextToken();
			Identifier();

#line  2449 "cs.ATG" 
			jc.IntoIdentifier = t.val; 
		}

#line  2451 "cs.ATG" 
		jc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionWhereClause(
#line  2483 "cs.ATG" 
out QueryExpressionWhereClause wc) {

#line  2484 "cs.ATG" 
		Expression expr; wc = new QueryExpressionWhereClause(); wc.StartLocation = la.Location; 
		Expect(127);
		Expr(
#line  2487 "cs.ATG" 
out expr);

#line  2487 "cs.ATG" 
		wc.Condition = expr; 

#line  2488 "cs.ATG" 
		wc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionLetClause(
#line  2491 "cs.ATG" 
out QueryExpressionLetClause wc) {

#line  2492 "cs.ATG" 
		Expression expr; wc = new QueryExpressionLetClause(); wc.StartLocation = la.Location; 
		Expect(141);
		Identifier();

#line  2495 "cs.ATG" 
		wc.Identifier = t.val; 
		Expect(3);
		Expr(
#line  2497 "cs.ATG" 
out expr);

#line  2497 "cs.ATG" 
		wc.Expression = expr; 

#line  2498 "cs.ATG" 
		wc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionOrderByClause(
#line  2501 "cs.ATG" 
out QueryExpressionOrderClause oc) {

#line  2502 "cs.ATG" 
		QueryExpressionOrdering ordering; oc = new QueryExpressionOrderClause(); oc.StartLocation = la.Location; 
		Expect(140);
		QueryExpressionOrdering(
#line  2505 "cs.ATG" 
out ordering);

#line  2505 "cs.ATG" 
		SafeAdd(oc, oc.Orderings, ordering); 
		while (la.kind == 14) {
			lexer.NextToken();
			QueryExpressionOrdering(
#line  2507 "cs.ATG" 
out ordering);

#line  2507 "cs.ATG" 
			SafeAdd(oc, oc.Orderings, ordering); 
		}

#line  2509 "cs.ATG" 
		oc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionSelectClause(
#line  2522 "cs.ATG" 
out QueryExpressionSelectClause sc) {

#line  2523 "cs.ATG" 
		Expression expr; sc = new QueryExpressionSelectClause(); sc.StartLocation = la.Location; 
		Expect(133);
		Expr(
#line  2526 "cs.ATG" 
out expr);

#line  2526 "cs.ATG" 
		sc.Projection = expr; 

#line  2527 "cs.ATG" 
		sc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionGroupClause(
#line  2530 "cs.ATG" 
out QueryExpressionGroupClause gc) {

#line  2531 "cs.ATG" 
		Expression expr; gc = new QueryExpressionGroupClause(); gc.StartLocation = la.Location; 
		Expect(134);
		Expr(
#line  2534 "cs.ATG" 
out expr);

#line  2534 "cs.ATG" 
		gc.Projection = expr; 
		Expect(135);
		Expr(
#line  2536 "cs.ATG" 
out expr);

#line  2536 "cs.ATG" 
		gc.GroupBy = expr; 

#line  2537 "cs.ATG" 
		gc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionIntoClause(
#line  2540 "cs.ATG" 
ref QueryExpression q) {

#line  2541 "cs.ATG" 
		QueryExpression firstQuery = q;
		QueryExpression continuedQuery = new QueryExpression(); 
		continuedQuery.StartLocation = q.StartLocation;
		firstQuery.EndLocation = la.Location;
		continuedQuery.FromClause = new QueryExpressionFromClause();
		continuedQuery.FromClause.StartLocation = la.Location;
		// nest firstQuery inside continuedQuery.
		continuedQuery.FromClause.InExpression = firstQuery;
		continuedQuery.IsQueryContinuation = true;
		q = continuedQuery;
		
		Expect(136);
		Identifier();

#line  2554 "cs.ATG" 
		continuedQuery.FromClause.Identifier = t.val; 

#line  2555 "cs.ATG" 
		continuedQuery.FromClause.EndLocation = t.EndLocation; 
		QueryExpressionBody(
#line  2556 "cs.ATG" 
ref q);
	}

	void QueryExpressionOrdering(
#line  2512 "cs.ATG" 
out QueryExpressionOrdering ordering) {

#line  2513 "cs.ATG" 
		Expression expr; ordering = new QueryExpressionOrdering(); ordering.StartLocation = la.Location; 
		Expr(
#line  2515 "cs.ATG" 
out expr);

#line  2515 "cs.ATG" 
		ordering.Criteria = expr; 
		if (la.kind == 138 || la.kind == 139) {
			if (la.kind == 138) {
				lexer.NextToken();

#line  2516 "cs.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Ascending; 
			} else {
				lexer.NextToken();

#line  2517 "cs.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Descending; 
			}
		}

#line  2519 "cs.ATG" 
		ordering.EndLocation = t.EndLocation; 
	}


	
	void ParseRoot()
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
			case 48: s = "\"=>\" expected"; break;
			case 49: s = "\"abstract\" expected"; break;
			case 50: s = "\"as\" expected"; break;
			case 51: s = "\"base\" expected"; break;
			case 52: s = "\"bool\" expected"; break;
			case 53: s = "\"break\" expected"; break;
			case 54: s = "\"byte\" expected"; break;
			case 55: s = "\"case\" expected"; break;
			case 56: s = "\"catch\" expected"; break;
			case 57: s = "\"char\" expected"; break;
			case 58: s = "\"checked\" expected"; break;
			case 59: s = "\"class\" expected"; break;
			case 60: s = "\"const\" expected"; break;
			case 61: s = "\"continue\" expected"; break;
			case 62: s = "\"decimal\" expected"; break;
			case 63: s = "\"default\" expected"; break;
			case 64: s = "\"delegate\" expected"; break;
			case 65: s = "\"do\" expected"; break;
			case 66: s = "\"double\" expected"; break;
			case 67: s = "\"else\" expected"; break;
			case 68: s = "\"enum\" expected"; break;
			case 69: s = "\"event\" expected"; break;
			case 70: s = "\"explicit\" expected"; break;
			case 71: s = "\"extern\" expected"; break;
			case 72: s = "\"false\" expected"; break;
			case 73: s = "\"finally\" expected"; break;
			case 74: s = "\"fixed\" expected"; break;
			case 75: s = "\"float\" expected"; break;
			case 76: s = "\"for\" expected"; break;
			case 77: s = "\"foreach\" expected"; break;
			case 78: s = "\"goto\" expected"; break;
			case 79: s = "\"if\" expected"; break;
			case 80: s = "\"implicit\" expected"; break;
			case 81: s = "\"in\" expected"; break;
			case 82: s = "\"int\" expected"; break;
			case 83: s = "\"interface\" expected"; break;
			case 84: s = "\"internal\" expected"; break;
			case 85: s = "\"is\" expected"; break;
			case 86: s = "\"lock\" expected"; break;
			case 87: s = "\"long\" expected"; break;
			case 88: s = "\"namespace\" expected"; break;
			case 89: s = "\"new\" expected"; break;
			case 90: s = "\"null\" expected"; break;
			case 91: s = "\"object\" expected"; break;
			case 92: s = "\"operator\" expected"; break;
			case 93: s = "\"out\" expected"; break;
			case 94: s = "\"override\" expected"; break;
			case 95: s = "\"params\" expected"; break;
			case 96: s = "\"private\" expected"; break;
			case 97: s = "\"protected\" expected"; break;
			case 98: s = "\"public\" expected"; break;
			case 99: s = "\"readonly\" expected"; break;
			case 100: s = "\"ref\" expected"; break;
			case 101: s = "\"return\" expected"; break;
			case 102: s = "\"sbyte\" expected"; break;
			case 103: s = "\"sealed\" expected"; break;
			case 104: s = "\"short\" expected"; break;
			case 105: s = "\"sizeof\" expected"; break;
			case 106: s = "\"stackalloc\" expected"; break;
			case 107: s = "\"static\" expected"; break;
			case 108: s = "\"string\" expected"; break;
			case 109: s = "\"struct\" expected"; break;
			case 110: s = "\"switch\" expected"; break;
			case 111: s = "\"this\" expected"; break;
			case 112: s = "\"throw\" expected"; break;
			case 113: s = "\"true\" expected"; break;
			case 114: s = "\"try\" expected"; break;
			case 115: s = "\"typeof\" expected"; break;
			case 116: s = "\"uint\" expected"; break;
			case 117: s = "\"ulong\" expected"; break;
			case 118: s = "\"unchecked\" expected"; break;
			case 119: s = "\"unsafe\" expected"; break;
			case 120: s = "\"ushort\" expected"; break;
			case 121: s = "\"using\" expected"; break;
			case 122: s = "\"virtual\" expected"; break;
			case 123: s = "\"void\" expected"; break;
			case 124: s = "\"volatile\" expected"; break;
			case 125: s = "\"while\" expected"; break;
			case 126: s = "\"partial\" expected"; break;
			case 127: s = "\"where\" expected"; break;
			case 128: s = "\"get\" expected"; break;
			case 129: s = "\"set\" expected"; break;
			case 130: s = "\"add\" expected"; break;
			case 131: s = "\"remove\" expected"; break;
			case 132: s = "\"yield\" expected"; break;
			case 133: s = "\"select\" expected"; break;
			case 134: s = "\"group\" expected"; break;
			case 135: s = "\"by\" expected"; break;
			case 136: s = "\"into\" expected"; break;
			case 137: s = "\"from\" expected"; break;
			case 138: s = "\"ascending\" expected"; break;
			case 139: s = "\"descending\" expected"; break;
			case 140: s = "\"orderby\" expected"; break;
			case 141: s = "\"let\" expected"; break;
			case 142: s = "\"join\" expected"; break;
			case 143: s = "\"on\" expected"; break;
			case 144: s = "\"equals\" expected"; break;
			case 145: s = "??? expected"; break;
			case 146: s = "invalid NamespaceMemberDecl"; break;
			case 147: s = "invalid NonArrayType"; break;
			case 148: s = "invalid Identifier"; break;
			case 149: s = "invalid AttributeArguments"; break;
			case 150: s = "invalid Expr"; break;
			case 151: s = "invalid TypeModifier"; break;
			case 152: s = "invalid TypeDecl"; break;
			case 153: s = "invalid TypeDecl"; break;
			case 154: s = "this symbol not expected in ClassBody"; break;
			case 155: s = "this symbol not expected in InterfaceBody"; break;
			case 156: s = "invalid IntegralType"; break;
			case 157: s = "invalid FormalParameterList"; break;
			case 158: s = "invalid FormalParameterList"; break;
			case 159: s = "invalid ClassType"; break;
			case 160: s = "invalid ClassMemberDecl"; break;
			case 161: s = "invalid ClassMemberDecl"; break;
			case 162: s = "invalid StructMemberDecl"; break;
			case 163: s = "invalid StructMemberDecl"; break;
			case 164: s = "invalid StructMemberDecl"; break;
			case 165: s = "invalid StructMemberDecl"; break;
			case 166: s = "invalid StructMemberDecl"; break;
			case 167: s = "invalid StructMemberDecl"; break;
			case 168: s = "invalid StructMemberDecl"; break;
			case 169: s = "invalid StructMemberDecl"; break;
			case 170: s = "invalid StructMemberDecl"; break;
			case 171: s = "invalid StructMemberDecl"; break;
			case 172: s = "invalid StructMemberDecl"; break;
			case 173: s = "invalid StructMemberDecl"; break;
			case 174: s = "invalid StructMemberDecl"; break;
			case 175: s = "invalid InterfaceMemberDecl"; break;
			case 176: s = "invalid InterfaceMemberDecl"; break;
			case 177: s = "invalid InterfaceMemberDecl"; break;
			case 178: s = "invalid TypeWithRestriction"; break;
			case 179: s = "invalid TypeWithRestriction"; break;
			case 180: s = "invalid SimpleType"; break;
			case 181: s = "invalid AccessorModifiers"; break;
			case 182: s = "this symbol not expected in Block"; break;
			case 183: s = "invalid EventAccessorDecls"; break;
			case 184: s = "invalid ConstructorInitializer"; break;
			case 185: s = "invalid OverloadableOperator"; break;
			case 186: s = "invalid AccessorDecls"; break;
			case 187: s = "invalid InterfaceAccessors"; break;
			case 188: s = "invalid InterfaceAccessors"; break;
			case 189: s = "invalid GetAccessorDecl"; break;
			case 190: s = "invalid SetAccessorDecl"; break;
			case 191: s = "invalid VariableInitializer"; break;
			case 192: s = "this symbol not expected in Statement"; break;
			case 193: s = "invalid Statement"; break;
			case 194: s = "invalid AssignmentOperator"; break;
			case 195: s = "invalid ObjectPropertyInitializerOrVariableInitializer"; break;
			case 196: s = "invalid ObjectPropertyInitializerOrVariableInitializer"; break;
			case 197: s = "invalid EmbeddedStatement"; break;
			case 198: s = "invalid EmbeddedStatement"; break;
			case 199: s = "this symbol not expected in EmbeddedStatement"; break;
			case 200: s = "invalid EmbeddedStatement"; break;
			case 201: s = "invalid ForInitializer"; break;
			case 202: s = "invalid GotoStatement"; break;
			case 203: s = "invalid ResourceAcquisition"; break;
			case 204: s = "invalid SwitchLabel"; break;
			case 205: s = "invalid CatchClause"; break;
			case 206: s = "invalid UnaryExpr"; break;
			case 207: s = "invalid PrimaryExpr"; break;
			case 208: s = "invalid PrimaryExpr"; break;
			case 209: s = "invalid PrimaryExpr"; break;
			case 210: s = "invalid TypeArgumentList"; break;
			case 211: s = "invalid NewExpression"; break;
			case 212: s = "invalid NewExpression"; break;
			case 213: s = "invalid LambdaExpressionParameter"; break;
			case 214: s = "invalid LambdaExpressionBody"; break;
			case 215: s = "invalid RelationalExpr"; break;
			case 216: s = "invalid RelationalExpr"; break;
			case 217: s = "invalid TypeParameterConstraintsClauseBase"; break;
			case 218: s = "invalid QueryExpressionBody"; break;

			default: s = "error " + errorNumber; break;
		}
		this.Errors.Error(line, col, s);
	}
	
	private bool StartOf(int s)
	{
		return set[s, lexer.LookAhead.kind];
	}
	
	static bool[,] set = {
	{T,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,T,T,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,T, T,T,T,x, x,T,T,T, T,T,T,T, T,T,T,x, T,T,T,T, T,x,T,T, T,T,T,T, T,x,T,T, T,x,T,T, x,T,T,T, x,x,T,x, T,T,T,T, x,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, T,T,x,x, x,x,x,x, T,T,T,x, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,T,x,x, x,x,x,x, T,T,T,x, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,x,x, T,T,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,x, x,T,T,x, x,x,T,T, T,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, T,x,x,T, x,T,x,T, T,T,T,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,T, T,T,T,T, T,T,x,T, T,T,T,T, T,T,T,T, T,T,T,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, x,T,T,x, T,x,T,x, x,T,x,T, T,x,T,x, T,x,T,x, T,T,T,T, x,x,T,T, x,x,x,x, T,x,T,T, T,T,x,T, x,T,x,T, x,x,T,x, T,T,T,T, x,x,T,T, T,x,x,T, T,T,x,x, x,x,x,x, T,T,x,T, T,x,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,T,x,T, x,x,x,x, T,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,T,x, x,T,x,T, T,x,T,x, T,x,T,x, T,T,T,T, x,x,T,T, x,x,x,x, T,x,T,T, T,x,x,T, x,T,x,T, x,x,T,x, T,T,T,T, x,x,T,T, T,x,x,T, T,T,x,x, x,x,x,x, T,T,x,T, T,x,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,T,x, x,T,x,T, T,x,T,x, T,x,T,x, T,T,T,T, x,x,T,T, x,x,x,x, T,x,T,T, T,x,x,T, x,T,x,T, x,x,T,x, T,T,T,T, x,x,T,T, T,x,x,T, T,T,x,x, x,x,x,x, T,T,x,T, T,x,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, T,x,T,x, x,T,x,T, T,x,T,x, T,x,T,x, T,T,T,T, x,x,T,T, x,x,x,x, T,x,T,T, T,x,x,T, x,T,x,T, x,x,T,x, T,T,T,T, x,x,T,T, T,x,x,T, T,T,x,x, x,x,x,x, T,T,x,T, T,x,T,T, T,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,T,x,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,T,x,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,T,x,x, x,x,x,x, T,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, x,x,x,x, x,x,x,x, T,x,x,x, x,T,x,x, x,x,T,x, T,T,T,T, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, T,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,T, T,x,T,x, T,x,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,T,T, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,T,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,T,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,T,T,x, T,T,T,T, T,T,T,x, x,x,x,x, T,x,T,T, T,T,T,T, x,x,T,x, x,x,T,T, x,T,T,T, x,x,x,x, x,x,x,x, x,T,T,x, T,T,x,x, T,x,T,T, T,T,T,T, T,T,T,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,x, x,T,T,x, x,x,T,T, T,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,T,T,T, x,T,x,x, x,x,x,x, T,x,T,x, T,T,x,x, T,x,x,T, x,T,x,T, T,T,T,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, T,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{T,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,T,T,x, T,T,T,T, T,T,T,x, x,x,x,x, T,x,T,T, T,T,T,T, x,x,T,x, x,x,T,T, x,T,T,T, x,x,x,x, x,x,x,x, x,T,T,x, T,T,x,x, T,x,T,T, T,T,T,T, T,T,T,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,x, x,T,T,x, x,T,T,T, T,T,T,x, x,x,x,x, T,x,T,T, T,T,T,T, x,x,T,x, x,x,T,T, x,T,T,T, x,x,x,x, x,x,x,x, x,T,T,x, T,T,x,x, T,x,T,T, T,T,T,T, T,T,T,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,x, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,x, x,T,T,x, x,x,T,T, T,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,T,x, T,T,T,x, T,x,x,T, x,T,x,T, T,T,T,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,T,T,x, T,T,T,x, x,x,x,T, x,x,x,x, T,x,x,x, T,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,T,T,T, x,T,T,x, T,T,T,T, T,T,T,x, x,x,x,x, T,x,T,T, T,T,T,T, x,x,T,x, x,x,T,T, x,T,T,T, x,x,x,x, x,x,x,x, x,T,T,x, T,T,x,x, T,x,T,T, T,T,T,T, T,T,T,T, T,T,x,T, x,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, T,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,T, T,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,T,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, T,x,T,x, x,T,T,x, x,x,T,T, T,x,T,x, x,x,x,x, T,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,T,T,T, x,x,x,x, x,x,x,x, x,x,T,x, T,T,x,x, T,x,x,T, x,T,x,T, T,T,T,x, T,x,x,T, x,x,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,T,T,T, T,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, T,x,T,x, x,T,x,x, x,x,T,x, x,x,T,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,T,x, x,x,x,T, x,x,x,T, x,x,x,x, x,x,x,x, x,x,T,x, T,x,x,x, T,x,x,x, x,x,x,x, T,T,x,x, T,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,T,x, T,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,T,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,T,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,T,T, x,x,x,x, x,x,x,x, x,x,x,T, T,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x},
	{x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,x, x,x,x,T, x,x,x,x, x,x,x,x, x,T,x,x, T,T,T,x, x,x,x}

	};
} // end Parser

}