
#line  1 "Frames/cs.ATG" 
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
	

#line  18 "Frames/cs.ATG" 


/*

*/

	void CS() {

#line  179 "Frames/cs.ATG" 
		lexer.NextToken(); /* get the first token */ 
		while (la.kind == 71) {
			ExternAliasDirective();
		}
		while (la.kind == 121) {
			UsingDirective();
		}
		while (
#line  183 "Frames/cs.ATG" 
IsGlobalAttrTarget()) {
			GlobalAttributeSection();
		}
		while (StartOf(1)) {
			NamespaceMemberDecl();
		}
		Expect(0);
	}

	void ExternAliasDirective() {

#line  353 "Frames/cs.ATG" 
		ExternAliasDirective ead = new ExternAliasDirective { StartLocation = la.Location }; 
		Expect(71);
		Identifier();

#line  356 "Frames/cs.ATG" 
		if (t.val != "alias") Error("Expected 'extern alias'."); 
		Identifier();

#line  357 "Frames/cs.ATG" 
		ead.Name = t.val; 
		Expect(11);

#line  358 "Frames/cs.ATG" 
		ead.EndLocation = t.EndLocation; 

#line  359 "Frames/cs.ATG" 
		compilationUnit.AddChild(ead); 
	}

	void UsingDirective() {

#line  190 "Frames/cs.ATG" 
		string qualident = null; TypeReference aliasedType = null;
		
		Expect(121);

#line  193 "Frames/cs.ATG" 
		Location startPos = t.Location; 
		Qualident(
#line  194 "Frames/cs.ATG" 
out qualident);
		if (la.kind == 3) {
			lexer.NextToken();
			NonArrayType(
#line  195 "Frames/cs.ATG" 
out aliasedType);
		}
		Expect(11);

#line  197 "Frames/cs.ATG" 
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

#line  213 "Frames/cs.ATG" 
		Location startPos = t.Location; 
		Identifier();

#line  214 "Frames/cs.ATG" 
		if (t.val != "assembly" && t.val != "module") Error("global attribute target specifier (assembly or module) expected");
		string attributeTarget = t.val;
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		Expect(9);
		Attribute(
#line  219 "Frames/cs.ATG" 
out attribute);

#line  219 "Frames/cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  220 "Frames/cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  220 "Frames/cs.ATG" 
out attribute);

#line  220 "Frames/cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  222 "Frames/cs.ATG" 
		AttributeSection section = new AttributeSection {
		   AttributeTarget = attributeTarget,
		   Attributes = attributes,
		   StartLocation = startPos,
		   EndLocation = t.EndLocation
		};
		compilationUnit.AddChild(section);
		
	}

	void NamespaceMemberDecl() {

#line  326 "Frames/cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		ModifierList m = new ModifierList();
		string qualident;
		
		if (la.kind == 88) {
			lexer.NextToken();

#line  332 "Frames/cs.ATG" 
			Location startPos = t.Location; 
			Qualident(
#line  333 "Frames/cs.ATG" 
out qualident);

#line  333 "Frames/cs.ATG" 
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

#line  343 "Frames/cs.ATG" 
			node.EndLocation   = t.EndLocation;
			compilationUnit.BlockEnd();
			
		} else if (StartOf(2)) {
			while (la.kind == 18) {
				AttributeSection(
#line  347 "Frames/cs.ATG" 
out section);

#line  347 "Frames/cs.ATG" 
				attributes.Add(section); 
			}
			while (StartOf(3)) {
				TypeModifier(
#line  348 "Frames/cs.ATG" 
m);
			}
			TypeDecl(
#line  349 "Frames/cs.ATG" 
m, attributes);
		} else SynErr(146);
	}

	void Qualident(
#line  483 "Frames/cs.ATG" 
out string qualident) {
		Identifier();

#line  485 "Frames/cs.ATG" 
		qualidentBuilder.Length = 0; qualidentBuilder.Append(t.val); 
		while (
#line  486 "Frames/cs.ATG" 
DotAndIdent()) {
			Expect(15);
			Identifier();

#line  486 "Frames/cs.ATG" 
			qualidentBuilder.Append('.');
			qualidentBuilder.Append(t.val); 
			
		}

#line  489 "Frames/cs.ATG" 
		qualident = qualidentBuilder.ToString(); 
	}

	void NonArrayType(
#line  601 "Frames/cs.ATG" 
out TypeReference type) {

#line  603 "Frames/cs.ATG" 
		Location startPos = la.Location;
		string name;
		int pointer = 0;
		type = null;
		
		if (StartOf(4)) {
			ClassType(
#line  609 "Frames/cs.ATG" 
out type, false);
		} else if (StartOf(5)) {
			SimpleType(
#line  610 "Frames/cs.ATG" 
out name);

#line  610 "Frames/cs.ATG" 
			type = new TypeReference(name, true); 
		} else if (la.kind == 123) {
			lexer.NextToken();
			Expect(6);

#line  611 "Frames/cs.ATG" 
			pointer = 1; type = new TypeReference("System.Void", true); 
		} else SynErr(147);
		if (la.kind == 12) {
			NullableQuestionMark(
#line  614 "Frames/cs.ATG" 
ref type);
		}
		while (
#line  616 "Frames/cs.ATG" 
IsPointer()) {
			Expect(6);

#line  617 "Frames/cs.ATG" 
			++pointer; 
		}

#line  619 "Frames/cs.ATG" 
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
#line  232 "Frames/cs.ATG" 
out ASTAttribute attribute) {

#line  233 "Frames/cs.ATG" 
		string qualident;
		string alias = null;
		

#line  237 "Frames/cs.ATG" 
		Location startPos = la.Location; 
		if (
#line  238 "Frames/cs.ATG" 
IdentAndDoubleColon()) {
			Identifier();

#line  239 "Frames/cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  242 "Frames/cs.ATG" 
out qualident);

#line  243 "Frames/cs.ATG" 
		List<Expression> positional = null;
		List<NamedArgumentExpression> named = null;
		string name = (alias != null && alias != "global") ? alias + "." + qualident : qualident;
		
		if (la.kind == 20) {
			AttributeArguments(
#line  247 "Frames/cs.ATG" 
out positional, out named);
		}

#line  248 "Frames/cs.ATG" 
		attribute = new ASTAttribute(name, positional, named); 
		attribute.StartLocation = startPos;
		attribute.EndLocation = t.EndLocation;
		
	}

	void AttributeArguments(
#line  254 "Frames/cs.ATG" 
out List<Expression> positional, out List<NamedArgumentExpression> named) {

#line  256 "Frames/cs.ATG" 
		bool nameFound = false;
		string name = "";
		Expression expr;
		positional = new List<Expression>();
		named = new List<NamedArgumentExpression> ();
		
		Expect(20);
		if (StartOf(6)) {
			if (
#line  266 "Frames/cs.ATG" 
IsAssignment()) {

#line  266 "Frames/cs.ATG" 
				nameFound = true; 
				Identifier();

#line  267 "Frames/cs.ATG" 
				name = t.val; 
				Expect(3);
			}
			Expr(
#line  269 "Frames/cs.ATG" 
out expr);

#line  269 "Frames/cs.ATG" 
			if (expr != null) {  
			if(name == "") positional.Add(expr);
			                          else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
			                          }
			                       
			while (la.kind == 14) {
				lexer.NextToken();
				if (
#line  278 "Frames/cs.ATG" 
IsAssignment()) {

#line  278 "Frames/cs.ATG" 
					nameFound = true; 
					Identifier();

#line  279 "Frames/cs.ATG" 
					name = t.val; 
					Expect(3);
				} else if (StartOf(6)) {

#line  281 "Frames/cs.ATG" 
					if (nameFound) Error("no positional argument after named argument"); 
				} else SynErr(149);
				Expr(
#line  282 "Frames/cs.ATG" 
out expr);

#line  282 "Frames/cs.ATG" 
				if (expr != null) { if(name == "") positional.Add(expr);
				else { named.Add(new NamedArgumentExpression(name, expr)); name = ""; }
				}
				
			}
		}
		Expect(21);
	}

	void Expr(
#line  1804 "Frames/cs.ATG" 
out Expression expr) {

#line  1805 "Frames/cs.ATG" 
		expr = null; Expression expr1 = null, expr2 = null; AssignmentOperatorType op; 

#line  1807 "Frames/cs.ATG" 
		Location startLocation = la.Location; 
		UnaryExpr(
#line  1808 "Frames/cs.ATG" 
out expr);
		if (StartOf(7)) {
			AssignmentOperator(
#line  1811 "Frames/cs.ATG" 
out op);
			Expr(
#line  1811 "Frames/cs.ATG" 
out expr1);

#line  1811 "Frames/cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (
#line  1812 "Frames/cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			AssignmentOperator(
#line  1813 "Frames/cs.ATG" 
out op);
			Expr(
#line  1813 "Frames/cs.ATG" 
out expr1);

#line  1813 "Frames/cs.ATG" 
			expr = new AssignmentExpression(expr, op, expr1); 
		} else if (StartOf(8)) {
			ConditionalOrExpr(
#line  1815 "Frames/cs.ATG" 
ref expr);
			if (la.kind == 13) {
				lexer.NextToken();
				Expr(
#line  1816 "Frames/cs.ATG" 
out expr1);

#line  1816 "Frames/cs.ATG" 
				expr = new BinaryOperatorExpression(expr, BinaryOperatorType.NullCoalescing, expr1); 
			}
			if (la.kind == 12) {
				lexer.NextToken();
				Expr(
#line  1817 "Frames/cs.ATG" 
out expr1);
				Expect(9);
				Expr(
#line  1817 "Frames/cs.ATG" 
out expr2);

#line  1817 "Frames/cs.ATG" 
				expr = new ConditionalExpression(expr, expr1, expr2);  
			}
		} else SynErr(150);

#line  1820 "Frames/cs.ATG" 
		if (expr != null) {
		expr.StartLocation = startLocation;
		expr.EndLocation = t.EndLocation;
		}
		
	}

	void AttributeSection(
#line  291 "Frames/cs.ATG" 
out AttributeSection section) {

#line  293 "Frames/cs.ATG" 
		string attributeTarget = "";
		List<ASTAttribute> attributes = new List<ASTAttribute>();
		ASTAttribute attribute;
		
		
		Expect(18);

#line  299 "Frames/cs.ATG" 
		Location startPos = t.Location; 
		if (
#line  300 "Frames/cs.ATG" 
IsLocalAttrTarget()) {
			if (la.kind == 69) {
				lexer.NextToken();

#line  301 "Frames/cs.ATG" 
				attributeTarget = "event";
			} else if (la.kind == 101) {
				lexer.NextToken();

#line  302 "Frames/cs.ATG" 
				attributeTarget = "return";
			} else {
				Identifier();

#line  303 "Frames/cs.ATG" 
				if (t.val != "field"   && t.val != "method" &&
				  t.val != "param" &&
				  t.val != "property" && t.val != "type")
				Error("attribute target specifier (field, event, method, param, property, return or type) expected");
				attributeTarget = t.val;
				
			}
			Expect(9);
		}
		Attribute(
#line  312 "Frames/cs.ATG" 
out attribute);

#line  312 "Frames/cs.ATG" 
		attributes.Add(attribute); 
		while (
#line  313 "Frames/cs.ATG" 
NotFinalComma()) {
			Expect(14);
			Attribute(
#line  313 "Frames/cs.ATG" 
out attribute);

#line  313 "Frames/cs.ATG" 
			attributes.Add(attribute); 
		}
		if (la.kind == 14) {
			lexer.NextToken();
		}
		Expect(19);

#line  315 "Frames/cs.ATG" 
		section = new AttributeSection {
		   AttributeTarget = attributeTarget,
		   Attributes = attributes,
		   StartLocation = startPos,
		   EndLocation = t.EndLocation
		};
		
	}

	void TypeModifier(
#line  696 "Frames/cs.ATG" 
ModifierList m) {
		switch (la.kind) {
		case 89: {
			lexer.NextToken();

#line  698 "Frames/cs.ATG" 
			m.Add(Modifiers.New, t.Location); 
			break;
		}
		case 98: {
			lexer.NextToken();

#line  699 "Frames/cs.ATG" 
			m.Add(Modifiers.Public, t.Location); 
			break;
		}
		case 97: {
			lexer.NextToken();

#line  700 "Frames/cs.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			break;
		}
		case 84: {
			lexer.NextToken();

#line  701 "Frames/cs.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			break;
		}
		case 96: {
			lexer.NextToken();

#line  702 "Frames/cs.ATG" 
			m.Add(Modifiers.Private, t.Location); 
			break;
		}
		case 119: {
			lexer.NextToken();

#line  703 "Frames/cs.ATG" 
			m.Add(Modifiers.Unsafe, t.Location); 
			break;
		}
		case 49: {
			lexer.NextToken();

#line  704 "Frames/cs.ATG" 
			m.Add(Modifiers.Abstract, t.Location); 
			break;
		}
		case 103: {
			lexer.NextToken();

#line  705 "Frames/cs.ATG" 
			m.Add(Modifiers.Sealed, t.Location); 
			break;
		}
		case 107: {
			lexer.NextToken();

#line  706 "Frames/cs.ATG" 
			m.Add(Modifiers.Static, t.Location); 
			break;
		}
		case 126: {
			lexer.NextToken();

#line  707 "Frames/cs.ATG" 
			m.Add(Modifiers.Partial, t.Location); 
			break;
		}
		default: SynErr(151); break;
		}
	}

	void TypeDecl(
#line  362 "Frames/cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  364 "Frames/cs.ATG" 
		TypeReference type;
		List<TypeReference> names;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		string name;
		List<TemplateDefinition> templates;
		
		if (la.kind == 59) {

#line  370 "Frames/cs.ATG" 
			m.Check(Modifiers.Classes); 
			lexer.NextToken();

#line  371 "Frames/cs.ATG" 
			TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
			templates = newType.Templates;
			compilationUnit.AddChild(newType);
			compilationUnit.BlockStart(newType);
			newType.StartLocation = m.GetDeclarationLocation(t.Location);
			
			newType.Type = Types.Class;
			
			Identifier();

#line  379 "Frames/cs.ATG" 
			newType.Name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  382 "Frames/cs.ATG" 
templates);
			}
			if (la.kind == 9) {
				ClassBase(
#line  384 "Frames/cs.ATG" 
out names);

#line  384 "Frames/cs.ATG" 
				newType.BaseTypes = names; 
			}
			while (la.kind == 127) {
				TypeParameterConstraintsClause(
#line  387 "Frames/cs.ATG" 
templates);
			}

#line  389 "Frames/cs.ATG" 
			newType.BodyStartLocation = t.EndLocation; 
			Expect(16);
			ClassBody();
			Expect(17);
			if (la.kind == 11) {
				lexer.NextToken();
			}

#line  393 "Frames/cs.ATG" 
			newType.EndLocation = t.EndLocation; 
			compilationUnit.BlockEnd();
			
		} else if (StartOf(9)) {

#line  396 "Frames/cs.ATG" 
			m.Check(Modifiers.StructsInterfacesEnumsDelegates); 
			if (la.kind == 109) {
				lexer.NextToken();

#line  397 "Frames/cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.Type = Types.Struct; 
				
				Identifier();

#line  404 "Frames/cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  407 "Frames/cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					StructInterfaces(
#line  409 "Frames/cs.ATG" 
out names);

#line  409 "Frames/cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (la.kind == 127) {
					TypeParameterConstraintsClause(
#line  412 "Frames/cs.ATG" 
templates);
				}

#line  415 "Frames/cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				StructBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  417 "Frames/cs.ATG" 
				newType.EndLocation = t.EndLocation; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 83) {
				lexer.NextToken();

#line  421 "Frames/cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				templates = newType.Templates;
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Interface;
				
				Identifier();

#line  428 "Frames/cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  431 "Frames/cs.ATG" 
templates);
				}
				if (la.kind == 9) {
					InterfaceBase(
#line  433 "Frames/cs.ATG" 
out names);

#line  433 "Frames/cs.ATG" 
					newType.BaseTypes = names; 
				}
				while (la.kind == 127) {
					TypeParameterConstraintsClause(
#line  436 "Frames/cs.ATG" 
templates);
				}

#line  438 "Frames/cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				InterfaceBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  440 "Frames/cs.ATG" 
				newType.EndLocation = t.EndLocation; 
				compilationUnit.BlockEnd();
				
			} else if (la.kind == 68) {
				lexer.NextToken();

#line  444 "Frames/cs.ATG" 
				TypeDeclaration newType = new TypeDeclaration(m.Modifier, attributes);
				compilationUnit.AddChild(newType);
				compilationUnit.BlockStart(newType);
				newType.StartLocation = m.GetDeclarationLocation(t.Location);
				newType.Type = Types.Enum;
				
				Identifier();

#line  450 "Frames/cs.ATG" 
				newType.Name = t.val; 
				if (la.kind == 9) {
					lexer.NextToken();
					IntegralType(
#line  451 "Frames/cs.ATG" 
out name);

#line  451 "Frames/cs.ATG" 
					newType.BaseTypes.Add(new TypeReference(name, true)); 
				}

#line  453 "Frames/cs.ATG" 
				newType.BodyStartLocation = t.EndLocation; 
				EnumBody();
				if (la.kind == 11) {
					lexer.NextToken();
				}

#line  455 "Frames/cs.ATG" 
				newType.EndLocation = t.EndLocation; 
				compilationUnit.BlockEnd();
				
			} else {
				lexer.NextToken();

#line  459 "Frames/cs.ATG" 
				DelegateDeclaration delegateDeclr = new DelegateDeclaration(m.Modifier, attributes);
				templates = delegateDeclr.Templates;
				delegateDeclr.StartLocation = m.GetDeclarationLocation(t.Location);
				
				if (
#line  463 "Frames/cs.ATG" 
NotVoidPointer()) {
					Expect(123);

#line  463 "Frames/cs.ATG" 
					delegateDeclr.ReturnType = new TypeReference("System.Void", true); 
				} else if (StartOf(10)) {
					Type(
#line  464 "Frames/cs.ATG" 
out type);

#line  464 "Frames/cs.ATG" 
					delegateDeclr.ReturnType = type; 
				} else SynErr(152);
				Identifier();

#line  466 "Frames/cs.ATG" 
				delegateDeclr.Name = t.val; 
				if (la.kind == 23) {
					TypeParameterList(
#line  469 "Frames/cs.ATG" 
templates);
				}
				Expect(20);
				if (StartOf(11)) {
					FormalParameterList(
#line  471 "Frames/cs.ATG" 
p);

#line  471 "Frames/cs.ATG" 
					delegateDeclr.Parameters = p; 
				}
				Expect(21);
				while (la.kind == 127) {
					TypeParameterConstraintsClause(
#line  475 "Frames/cs.ATG" 
templates);
				}
				Expect(11);

#line  477 "Frames/cs.ATG" 
				delegateDeclr.EndLocation = t.EndLocation;
				compilationUnit.AddChild(delegateDeclr);
				
			}
		} else SynErr(153);
	}

	void TypeParameterList(
#line  2381 "Frames/cs.ATG" 
List<TemplateDefinition> templates) {

#line  2383 "Frames/cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		Expect(23);
		while (la.kind == 18) {
			AttributeSection(
#line  2387 "Frames/cs.ATG" 
out section);

#line  2387 "Frames/cs.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  2388 "Frames/cs.ATG" 
		templates.Add(new TemplateDefinition(t.val, attributes)); 
		while (la.kind == 14) {
			lexer.NextToken();
			while (la.kind == 18) {
				AttributeSection(
#line  2389 "Frames/cs.ATG" 
out section);

#line  2389 "Frames/cs.ATG" 
				attributes.Add(section); 
			}
			Identifier();

#line  2390 "Frames/cs.ATG" 
			templates.Add(new TemplateDefinition(t.val, attributes)); 
		}
		Expect(22);
	}

	void ClassBase(
#line  492 "Frames/cs.ATG" 
out List<TypeReference> names) {

#line  494 "Frames/cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		ClassType(
#line  498 "Frames/cs.ATG" 
out typeRef, false);

#line  498 "Frames/cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  499 "Frames/cs.ATG" 
out typeRef, false);

#line  499 "Frames/cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void TypeParameterConstraintsClause(
#line  2394 "Frames/cs.ATG" 
List<TemplateDefinition> templates) {

#line  2395 "Frames/cs.ATG" 
		string name = ""; TypeReference type; 
		Expect(127);
		Identifier();

#line  2398 "Frames/cs.ATG" 
		name = t.val; 
		Expect(9);
		TypeParameterConstraintsClauseBase(
#line  2400 "Frames/cs.ATG" 
out type);

#line  2401 "Frames/cs.ATG" 
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
#line  2410 "Frames/cs.ATG" 
out type);

#line  2411 "Frames/cs.ATG" 
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

#line  503 "Frames/cs.ATG" 
		AttributeSection section; 
		while (StartOf(12)) {

#line  505 "Frames/cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (!(StartOf(13))) {SynErr(154); lexer.NextToken(); }
			while (la.kind == 18) {
				AttributeSection(
#line  509 "Frames/cs.ATG" 
out section);

#line  509 "Frames/cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  510 "Frames/cs.ATG" 
m);
			ClassMemberDecl(
#line  511 "Frames/cs.ATG" 
m, attributes);
		}
	}

	void StructInterfaces(
#line  515 "Frames/cs.ATG" 
out List<TypeReference> names) {

#line  517 "Frames/cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  521 "Frames/cs.ATG" 
out typeRef, false);

#line  521 "Frames/cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  522 "Frames/cs.ATG" 
out typeRef, false);

#line  522 "Frames/cs.ATG" 
			if (typeRef != null) { names.Add(typeRef); } 
		}
	}

	void StructBody() {

#line  526 "Frames/cs.ATG" 
		AttributeSection section; 
		Expect(16);
		while (StartOf(14)) {

#line  529 "Frames/cs.ATG" 
			List<AttributeSection> attributes = new List<AttributeSection>();
			ModifierList m = new ModifierList();
			
			while (la.kind == 18) {
				AttributeSection(
#line  532 "Frames/cs.ATG" 
out section);

#line  532 "Frames/cs.ATG" 
				attributes.Add(section); 
			}
			MemberModifiers(
#line  533 "Frames/cs.ATG" 
m);
			StructMemberDecl(
#line  534 "Frames/cs.ATG" 
m, attributes);
		}
		Expect(17);
	}

	void InterfaceBase(
#line  539 "Frames/cs.ATG" 
out List<TypeReference> names) {

#line  541 "Frames/cs.ATG" 
		TypeReference typeRef;
		names = new List<TypeReference>();
		
		Expect(9);
		TypeName(
#line  545 "Frames/cs.ATG" 
out typeRef, false);

#line  545 "Frames/cs.ATG" 
		if (typeRef != null) { names.Add(typeRef); } 
		while (la.kind == 14) {
			lexer.NextToken();
			TypeName(
#line  546 "Frames/cs.ATG" 
out typeRef, false);

#line  546 "Frames/cs.ATG" 
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
#line  718 "Frames/cs.ATG" 
out string name) {

#line  718 "Frames/cs.ATG" 
		name = ""; 
		switch (la.kind) {
		case 102: {
			lexer.NextToken();

#line  720 "Frames/cs.ATG" 
			name = "System.SByte"; 
			break;
		}
		case 54: {
			lexer.NextToken();

#line  721 "Frames/cs.ATG" 
			name = "System.Byte"; 
			break;
		}
		case 104: {
			lexer.NextToken();

#line  722 "Frames/cs.ATG" 
			name = "System.Int16"; 
			break;
		}
		case 120: {
			lexer.NextToken();

#line  723 "Frames/cs.ATG" 
			name = "System.UInt16"; 
			break;
		}
		case 82: {
			lexer.NextToken();

#line  724 "Frames/cs.ATG" 
			name = "System.Int32"; 
			break;
		}
		case 116: {
			lexer.NextToken();

#line  725 "Frames/cs.ATG" 
			name = "System.UInt32"; 
			break;
		}
		case 87: {
			lexer.NextToken();

#line  726 "Frames/cs.ATG" 
			name = "System.Int64"; 
			break;
		}
		case 117: {
			lexer.NextToken();

#line  727 "Frames/cs.ATG" 
			name = "System.UInt64"; 
			break;
		}
		case 57: {
			lexer.NextToken();

#line  728 "Frames/cs.ATG" 
			name = "System.Char"; 
			break;
		}
		default: SynErr(156); break;
		}
	}

	void EnumBody() {

#line  555 "Frames/cs.ATG" 
		FieldDeclaration f; 
		Expect(16);
		if (StartOf(17)) {
			EnumMemberDecl(
#line  558 "Frames/cs.ATG" 
out f);

#line  558 "Frames/cs.ATG" 
			compilationUnit.AddChild(f); 
			while (
#line  559 "Frames/cs.ATG" 
NotFinalComma()) {
				Expect(14);
				EnumMemberDecl(
#line  560 "Frames/cs.ATG" 
out f);

#line  560 "Frames/cs.ATG" 
				compilationUnit.AddChild(f); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);
	}

	void Type(
#line  566 "Frames/cs.ATG" 
out TypeReference type) {
		TypeWithRestriction(
#line  568 "Frames/cs.ATG" 
out type, true, false);
	}

	void FormalParameterList(
#line  638 "Frames/cs.ATG" 
List<ParameterDeclarationExpression> parameter) {

#line  641 "Frames/cs.ATG" 
		ParameterDeclarationExpression p;
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		
		while (la.kind == 18) {
			AttributeSection(
#line  646 "Frames/cs.ATG" 
out section);

#line  646 "Frames/cs.ATG" 
			attributes.Add(section); 
		}
		if (StartOf(18)) {
			FixedParameter(
#line  648 "Frames/cs.ATG" 
out p);

#line  648 "Frames/cs.ATG" 
			bool paramsFound = false;
			p.Attributes = attributes;
			parameter.Add(p);
			
			while (la.kind == 14) {
				lexer.NextToken();

#line  653 "Frames/cs.ATG" 
				attributes = new List<AttributeSection>(); if (paramsFound) Error("params array must be at end of parameter list"); 
				while (la.kind == 18) {
					AttributeSection(
#line  654 "Frames/cs.ATG" 
out section);

#line  654 "Frames/cs.ATG" 
					attributes.Add(section); 
				}
				if (StartOf(18)) {
					FixedParameter(
#line  656 "Frames/cs.ATG" 
out p);

#line  656 "Frames/cs.ATG" 
					p.Attributes = attributes; parameter.Add(p); 
				} else if (la.kind == 95) {
					ParameterArray(
#line  657 "Frames/cs.ATG" 
out p);

#line  657 "Frames/cs.ATG" 
					paramsFound = true; p.Attributes = attributes; parameter.Add(p); 
				} else SynErr(157);
			}
		} else if (la.kind == 95) {
			ParameterArray(
#line  660 "Frames/cs.ATG" 
out p);

#line  660 "Frames/cs.ATG" 
			p.Attributes = attributes; parameter.Add(p); 
		} else SynErr(158);
	}

	void ClassType(
#line  710 "Frames/cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  711 "Frames/cs.ATG" 
		TypeReference r; typeRef = null; 
		if (StartOf(19)) {
			TypeName(
#line  713 "Frames/cs.ATG" 
out r, canBeUnbound);

#line  713 "Frames/cs.ATG" 
			typeRef = r; 
		} else if (la.kind == 91) {
			lexer.NextToken();

#line  714 "Frames/cs.ATG" 
			typeRef = new TypeReference("System.Object", true); typeRef.StartLocation = t.Location; 
		} else if (la.kind == 108) {
			lexer.NextToken();

#line  715 "Frames/cs.ATG" 
			typeRef = new TypeReference("System.String", true); typeRef.StartLocation = t.Location; 
		} else SynErr(159);
	}

	void TypeName(
#line  2322 "Frames/cs.ATG" 
out TypeReference typeRef, bool canBeUnbound) {

#line  2323 "Frames/cs.ATG" 
		List<TypeReference> typeArguments = null;
		string alias = null;
		string qualident;
		Location startLocation = la.Location;
		
		if (
#line  2329 "Frames/cs.ATG" 
IdentAndDoubleColon()) {
			Identifier();

#line  2330 "Frames/cs.ATG" 
			alias = t.val; 
			Expect(10);
		}
		Qualident(
#line  2333 "Frames/cs.ATG" 
out qualident);
		if (la.kind == 23) {
			TypeArgumentList(
#line  2334 "Frames/cs.ATG" 
out typeArguments, canBeUnbound);
		}

#line  2336 "Frames/cs.ATG" 
		if (alias == null) {
		typeRef = new TypeReference(qualident, typeArguments);
		} else if (alias == "global") {
			typeRef = new TypeReference(qualident, typeArguments);
			typeRef.IsGlobal = true;
		} else {
			typeRef = new TypeReference(alias + "." + qualident, typeArguments);
		}
		
		while (
#line  2345 "Frames/cs.ATG" 
DotAndIdent()) {
			Expect(15);

#line  2346 "Frames/cs.ATG" 
			typeArguments = null; 
			Qualident(
#line  2347 "Frames/cs.ATG" 
out qualident);
			if (la.kind == 23) {
				TypeArgumentList(
#line  2348 "Frames/cs.ATG" 
out typeArguments, canBeUnbound);
			}

#line  2349 "Frames/cs.ATG" 
			typeRef = new InnerClassTypeReference(typeRef, qualident, typeArguments); 
		}

#line  2351 "Frames/cs.ATG" 
		typeRef.StartLocation = startLocation; 
	}

	void MemberModifiers(
#line  731 "Frames/cs.ATG" 
ModifierList m) {
		while (StartOf(20)) {
			switch (la.kind) {
			case 49: {
				lexer.NextToken();

#line  734 "Frames/cs.ATG" 
				m.Add(Modifiers.Abstract, t.Location); 
				break;
			}
			case 71: {
				lexer.NextToken();

#line  735 "Frames/cs.ATG" 
				m.Add(Modifiers.Extern, t.Location); 
				break;
			}
			case 84: {
				lexer.NextToken();

#line  736 "Frames/cs.ATG" 
				m.Add(Modifiers.Internal, t.Location); 
				break;
			}
			case 89: {
				lexer.NextToken();

#line  737 "Frames/cs.ATG" 
				m.Add(Modifiers.New, t.Location); 
				break;
			}
			case 94: {
				lexer.NextToken();

#line  738 "Frames/cs.ATG" 
				m.Add(Modifiers.Override, t.Location); 
				break;
			}
			case 96: {
				lexer.NextToken();

#line  739 "Frames/cs.ATG" 
				m.Add(Modifiers.Private, t.Location); 
				break;
			}
			case 97: {
				lexer.NextToken();

#line  740 "Frames/cs.ATG" 
				m.Add(Modifiers.Protected, t.Location); 
				break;
			}
			case 98: {
				lexer.NextToken();

#line  741 "Frames/cs.ATG" 
				m.Add(Modifiers.Public, t.Location); 
				break;
			}
			case 99: {
				lexer.NextToken();

#line  742 "Frames/cs.ATG" 
				m.Add(Modifiers.ReadOnly, t.Location); 
				break;
			}
			case 103: {
				lexer.NextToken();

#line  743 "Frames/cs.ATG" 
				m.Add(Modifiers.Sealed, t.Location); 
				break;
			}
			case 107: {
				lexer.NextToken();

#line  744 "Frames/cs.ATG" 
				m.Add(Modifiers.Static, t.Location); 
				break;
			}
			case 74: {
				lexer.NextToken();

#line  745 "Frames/cs.ATG" 
				m.Add(Modifiers.Fixed, t.Location); 
				break;
			}
			case 119: {
				lexer.NextToken();

#line  746 "Frames/cs.ATG" 
				m.Add(Modifiers.Unsafe, t.Location); 
				break;
			}
			case 122: {
				lexer.NextToken();

#line  747 "Frames/cs.ATG" 
				m.Add(Modifiers.Virtual, t.Location); 
				break;
			}
			case 124: {
				lexer.NextToken();

#line  748 "Frames/cs.ATG" 
				m.Add(Modifiers.Volatile, t.Location); 
				break;
			}
			case 126: {
				lexer.NextToken();

#line  749 "Frames/cs.ATG" 
				m.Add(Modifiers.Partial, t.Location); 
				break;
			}
			}
		}
	}

	void ClassMemberDecl(
#line  1076 "Frames/cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  1077 "Frames/cs.ATG" 
		Statement stmt = null; 
		if (StartOf(21)) {
			StructMemberDecl(
#line  1079 "Frames/cs.ATG" 
m, attributes);
		} else if (la.kind == 27) {

#line  1080 "Frames/cs.ATG" 
			m.Check(Modifiers.Destructors); Location startPos = la.Location; 
			lexer.NextToken();
			Identifier();

#line  1081 "Frames/cs.ATG" 
			DestructorDeclaration d = new DestructorDeclaration(t.val, m.Modifier, attributes); 
			d.Modifier = m.Modifier;
			d.StartLocation = m.GetDeclarationLocation(startPos);
			
			Expect(20);
			Expect(21);

#line  1085 "Frames/cs.ATG" 
			d.EndLocation = t.EndLocation; 
			if (la.kind == 16) {
				Block(
#line  1085 "Frames/cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(160);

#line  1086 "Frames/cs.ATG" 
			d.Body = (BlockStatement)stmt;
			compilationUnit.AddChild(d);
			
		} else SynErr(161);
	}

	void StructMemberDecl(
#line  753 "Frames/cs.ATG" 
ModifierList m, List<AttributeSection> attributes) {

#line  755 "Frames/cs.ATG" 
		string qualident = null;
		TypeReference type;
		Expression expr;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		Statement stmt = null;
		List<TemplateDefinition> templates = new List<TemplateDefinition>();
		TypeReference explicitInterface = null;
		bool isExtensionMethod = false;
		
		if (la.kind == 60) {

#line  765 "Frames/cs.ATG" 
			m.Check(Modifiers.Constants); 
			lexer.NextToken();

#line  766 "Frames/cs.ATG" 
			Location startPos = t.Location; 
			Type(
#line  767 "Frames/cs.ATG" 
out type);
			Identifier();

#line  767 "Frames/cs.ATG" 
			FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier | Modifiers.Const);
			fd.StartLocation = m.GetDeclarationLocation(startPos);
			VariableDeclaration f = new VariableDeclaration(t.val);
			f.StartLocation = t.Location;
			f.TypeReference = type;
			SafeAdd(fd, fd.Fields, f);
			
			Expect(3);
			Expr(
#line  774 "Frames/cs.ATG" 
out expr);

#line  774 "Frames/cs.ATG" 
			f.Initializer = expr; 
			while (la.kind == 14) {
				lexer.NextToken();
				Identifier();

#line  775 "Frames/cs.ATG" 
				f = new VariableDeclaration(t.val);
				f.StartLocation = t.Location;
				f.TypeReference = type;
				SafeAdd(fd, fd.Fields, f);
				
				Expect(3);
				Expr(
#line  780 "Frames/cs.ATG" 
out expr);

#line  780 "Frames/cs.ATG" 
				f.EndLocation = t.EndLocation; f.Initializer = expr; 
			}
			Expect(11);

#line  781 "Frames/cs.ATG" 
			fd.EndLocation = t.EndLocation; compilationUnit.AddChild(fd); 
		} else if (
#line  785 "Frames/cs.ATG" 
NotVoidPointer()) {

#line  785 "Frames/cs.ATG" 
			m.Check(Modifiers.PropertysEventsMethods); 
			Expect(123);

#line  786 "Frames/cs.ATG" 
			Location startPos = t.Location; 
			if (
#line  787 "Frames/cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  788 "Frames/cs.ATG" 
out explicitInterface, false);

#line  789 "Frames/cs.ATG" 
				if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
				 } 
			} else if (StartOf(19)) {
				Identifier();

#line  792 "Frames/cs.ATG" 
				qualident = t.val; 
			} else SynErr(162);
			if (la.kind == 23) {
				TypeParameterList(
#line  795 "Frames/cs.ATG" 
templates);
			}
			Expect(20);
			if (la.kind == 111) {
				lexer.NextToken();

#line  798 "Frames/cs.ATG" 
				isExtensionMethod = true; /* C# 3.0 */ 
			}
			if (StartOf(11)) {
				FormalParameterList(
#line  799 "Frames/cs.ATG" 
p);
			}
			Expect(21);

#line  800 "Frames/cs.ATG" 
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
#line  818 "Frames/cs.ATG" 
templates);
			}
			if (la.kind == 16) {
				Block(
#line  820 "Frames/cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(163);

#line  820 "Frames/cs.ATG" 
			compilationUnit.BlockEnd();
			methodDeclaration.Body  = (BlockStatement)stmt;
			
		} else if (la.kind == 69) {

#line  824 "Frames/cs.ATG" 
			m.Check(Modifiers.PropertysEventsMethods); 
			lexer.NextToken();

#line  826 "Frames/cs.ATG" 
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
#line  836 "Frames/cs.ATG" 
out type);

#line  836 "Frames/cs.ATG" 
			eventDecl.TypeReference = type; 
			if (
#line  837 "Frames/cs.ATG" 
IsExplicitInterfaceImplementation()) {
				TypeName(
#line  838 "Frames/cs.ATG" 
out explicitInterface, false);

#line  839 "Frames/cs.ATG" 
				qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface); 

#line  840 "Frames/cs.ATG" 
				eventDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident)); 
			} else if (StartOf(19)) {
				Identifier();

#line  842 "Frames/cs.ATG" 
				qualident = t.val; 
			} else SynErr(164);

#line  844 "Frames/cs.ATG" 
			eventDecl.Name = qualident; eventDecl.EndLocation = t.EndLocation; 
			if (la.kind == 3) {
				lexer.NextToken();
				Expr(
#line  845 "Frames/cs.ATG" 
out expr);

#line  845 "Frames/cs.ATG" 
				eventDecl.Initializer = expr; 
			}
			if (la.kind == 16) {
				lexer.NextToken();

#line  846 "Frames/cs.ATG" 
				eventDecl.BodyStart = t.Location; 
				EventAccessorDecls(
#line  847 "Frames/cs.ATG" 
out addBlock, out removeBlock);
				Expect(17);

#line  848 "Frames/cs.ATG" 
				eventDecl.BodyEnd   = t.EndLocation; 
			}

#line  850 "Frames/cs.ATG" 
			compilationUnit.BlockEnd();
			eventDecl.AddRegion = addBlock;
			eventDecl.RemoveRegion = removeBlock;
			
			while (la.kind == 14) {
				lexer.NextToken();
				Identifier();

#line  854 "Frames/cs.ATG" 
				EventDeclaration additionalEventDeclaration = new EventDeclaration {
				    Modifier = eventDecl.Modifier, 
				    Attributes = eventDecl.Attributes,
				    StartLocation = eventDecl.StartLocation,
				    TypeReference = eventDecl.TypeReference,
				    Name = t.val
				};
				compilationUnit.AddChild (additionalEventDeclaration);
				
			}
			if (la.kind == 11) {
				lexer.NextToken();
			}
		} else if (
#line  868 "Frames/cs.ATG" 
IdentAndLPar()) {

#line  868 "Frames/cs.ATG" 
			m.Check(Modifiers.Constructors | Modifiers.StaticConstructors); 
			Identifier();

#line  869 "Frames/cs.ATG" 
			string name = t.val; Location startPos = t.Location; 
			Expect(20);
			if (StartOf(11)) {

#line  869 "Frames/cs.ATG" 
				m.Check(Modifiers.Constructors); 
				FormalParameterList(
#line  870 "Frames/cs.ATG" 
p);
			}
			Expect(21);

#line  872 "Frames/cs.ATG" 
			ConstructorInitializer init = null;  
			if (la.kind == 9) {

#line  873 "Frames/cs.ATG" 
				m.Check(Modifiers.Constructors); 
				ConstructorInitializer(
#line  874 "Frames/cs.ATG" 
out init);
			}

#line  876 "Frames/cs.ATG" 
			ConstructorDeclaration cd = new ConstructorDeclaration(name, m.Modifier, p, init, attributes);
			cd.StartLocation = startPos;
			cd.EndLocation   = t.EndLocation;
			
			if (la.kind == 16) {
				Block(
#line  881 "Frames/cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();
			} else SynErr(165);

#line  881 "Frames/cs.ATG" 
			cd.Body = (BlockStatement)stmt; compilationUnit.AddChild(cd); 
		} else if (la.kind == 70 || la.kind == 80) {

#line  884 "Frames/cs.ATG" 
			m.Check(Modifiers.Operators);
			if (m.isNone) Error("at least one modifier must be set"); 
			bool isImplicit = true;
			Location startPos = Location.Empty;
			
			if (la.kind == 80) {
				lexer.NextToken();

#line  889 "Frames/cs.ATG" 
				startPos = t.Location; 
			} else {
				lexer.NextToken();

#line  889 "Frames/cs.ATG" 
				isImplicit = false; startPos = t.Location; 
			}
			Expect(92);
			Type(
#line  890 "Frames/cs.ATG" 
out type);

#line  890 "Frames/cs.ATG" 
			TypeReference operatorType = type; 
			Expect(20);
			Type(
#line  891 "Frames/cs.ATG" 
out type);
			Identifier();

#line  891 "Frames/cs.ATG" 
			string varName = t.val; 
			Expect(21);

#line  892 "Frames/cs.ATG" 
			Location endPos = t.Location; 
			if (la.kind == 16) {
				Block(
#line  893 "Frames/cs.ATG" 
out stmt);
			} else if (la.kind == 11) {
				lexer.NextToken();

#line  893 "Frames/cs.ATG" 
				stmt = null; 
			} else SynErr(166);

#line  896 "Frames/cs.ATG" 
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
#line  914 "Frames/cs.ATG" 
m, attributes);
		} else if (StartOf(10)) {
			Type(
#line  916 "Frames/cs.ATG" 
out type);

#line  916 "Frames/cs.ATG" 
			Location startPos = t.Location;  
			if (la.kind == 92) {

#line  918 "Frames/cs.ATG" 
				OverloadableOperatorType op;
				m.Check(Modifiers.Operators);
				if (m.isNone) Error("at least one modifier must be set");
				
				lexer.NextToken();
				OverloadableOperator(
#line  922 "Frames/cs.ATG" 
out op);

#line  922 "Frames/cs.ATG" 
				TypeReference firstType, secondType = null; string secondName = null; 
				Expect(20);
				Type(
#line  923 "Frames/cs.ATG" 
out firstType);
				Identifier();

#line  923 "Frames/cs.ATG" 
				string firstName = t.val; 
				if (la.kind == 14) {
					lexer.NextToken();
					Type(
#line  924 "Frames/cs.ATG" 
out secondType);
					Identifier();

#line  924 "Frames/cs.ATG" 
					secondName = t.val; 
				} else if (la.kind == 21) {
				} else SynErr(167);

#line  932 "Frames/cs.ATG" 
				Location endPos = t.Location; 
				Expect(21);
				if (la.kind == 16) {
					Block(
#line  933 "Frames/cs.ATG" 
out stmt);
				} else if (la.kind == 11) {
					lexer.NextToken();
				} else SynErr(168);

#line  935 "Frames/cs.ATG" 
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
#line  957 "Frames/cs.ATG" 
IsVarDecl()) {

#line  958 "Frames/cs.ATG" 
				m.Check(Modifiers.Fields);
				FieldDeclaration fd = new FieldDeclaration(attributes, type, m.Modifier);
				fd.StartLocation = m.GetDeclarationLocation(startPos); 
				
				if (
#line  962 "Frames/cs.ATG" 
m.Contains(Modifiers.Fixed)) {
					VariableDeclarator(
#line  963 "Frames/cs.ATG" 
fd);
					Expect(18);
					Expr(
#line  965 "Frames/cs.ATG" 
out expr);

#line  965 "Frames/cs.ATG" 
					if (fd.Fields.Count > 0)
					fd.Fields[fd.Fields.Count-1].FixedArrayInitialization = expr; 
					Expect(19);
					while (la.kind == 14) {
						lexer.NextToken();
						VariableDeclarator(
#line  969 "Frames/cs.ATG" 
fd);
						Expect(18);
						Expr(
#line  971 "Frames/cs.ATG" 
out expr);

#line  971 "Frames/cs.ATG" 
						if (fd.Fields.Count > 0)
						fd.Fields[fd.Fields.Count-1].FixedArrayInitialization = expr; 
						Expect(19);
					}
				} else if (StartOf(19)) {
					VariableDeclarator(
#line  976 "Frames/cs.ATG" 
fd);
					while (la.kind == 14) {
						lexer.NextToken();
						VariableDeclarator(
#line  977 "Frames/cs.ATG" 
fd);
					}
				} else SynErr(169);
				Expect(11);

#line  979 "Frames/cs.ATG" 
				fd.EndLocation = t.EndLocation; compilationUnit.AddChild(fd); 
			} else if (la.kind == 111) {

#line  982 "Frames/cs.ATG" 
				m.Check(Modifiers.Indexers); 
				lexer.NextToken();
				Expect(18);
				FormalParameterList(
#line  983 "Frames/cs.ATG" 
p);
				Expect(19);

#line  983 "Frames/cs.ATG" 
				Location endLocation = t.EndLocation; 
				Expect(16);

#line  984 "Frames/cs.ATG" 
				IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
				indexer.StartLocation = startPos;
				indexer.EndLocation   = endLocation;
				indexer.BodyStart     = t.Location;
				PropertyGetRegion getRegion;
				PropertySetRegion setRegion;
				
				AccessorDecls(
#line  991 "Frames/cs.ATG" 
out getRegion, out setRegion);
				Expect(17);

#line  992 "Frames/cs.ATG" 
				indexer.BodyEnd    = t.EndLocation;
				indexer.GetRegion = getRegion;
				indexer.SetRegion = setRegion;
				compilationUnit.AddChild(indexer);
				
			} else if (
#line  997 "Frames/cs.ATG" 
IsIdentifierToken(la)) {
				if (
#line  998 "Frames/cs.ATG" 
IsExplicitInterfaceImplementation()) {
					TypeName(
#line  999 "Frames/cs.ATG" 
out explicitInterface, false);

#line  1000 "Frames/cs.ATG" 
					if (la.kind != Tokens.Dot || Peek(1).kind != Tokens.This) {
					qualident = TypeReference.StripLastIdentifierFromType(ref explicitInterface);
					 } 
				} else if (StartOf(19)) {
					Identifier();

#line  1003 "Frames/cs.ATG" 
					qualident = t.val; 
				} else SynErr(170);

#line  1005 "Frames/cs.ATG" 
				Location qualIdentEndLocation = t.EndLocation; 
				if (la.kind == 16 || la.kind == 20 || la.kind == 23) {
					if (la.kind == 20 || la.kind == 23) {

#line  1009 "Frames/cs.ATG" 
						m.Check(Modifiers.PropertysEventsMethods); 
						if (la.kind == 23) {
							TypeParameterList(
#line  1011 "Frames/cs.ATG" 
templates);
						}
						Expect(20);
						if (la.kind == 111) {
							lexer.NextToken();

#line  1013 "Frames/cs.ATG" 
							isExtensionMethod = true; 
						}
						if (StartOf(11)) {
							FormalParameterList(
#line  1014 "Frames/cs.ATG" 
p);
						}
						Expect(21);

#line  1016 "Frames/cs.ATG" 
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
#line  1031 "Frames/cs.ATG" 
templates);
						}
						if (la.kind == 16) {
							Block(
#line  1032 "Frames/cs.ATG" 
out stmt);
						} else if (la.kind == 11) {
							lexer.NextToken();
						} else SynErr(171);

#line  1032 "Frames/cs.ATG" 
						methodDeclaration.Body  = (BlockStatement)stmt; 
					} else {
						lexer.NextToken();

#line  1035 "Frames/cs.ATG" 
						PropertyDeclaration pDecl = new PropertyDeclaration(qualident, type, m.Modifier, attributes); 
						if (explicitInterface != null)
						pDecl.InterfaceImplementations.Add(new InterfaceImplementation(explicitInterface, qualident));
						      pDecl.StartLocation = m.GetDeclarationLocation(startPos);
						      pDecl.EndLocation   = qualIdentEndLocation;
						      pDecl.BodyStart   = t.Location;
						      PropertyGetRegion getRegion;
						      PropertySetRegion setRegion;
						   
						AccessorDecls(
#line  1044 "Frames/cs.ATG" 
out getRegion, out setRegion);
						Expect(17);

#line  1046 "Frames/cs.ATG" 
						pDecl.GetRegion = getRegion;
						pDecl.SetRegion = setRegion;
						pDecl.BodyEnd = t.EndLocation;
						compilationUnit.AddChild(pDecl);
						
					}
				} else if (la.kind == 15) {

#line  1054 "Frames/cs.ATG" 
					m.Check(Modifiers.Indexers); 
					lexer.NextToken();
					Expect(111);
					Expect(18);
					FormalParameterList(
#line  1055 "Frames/cs.ATG" 
p);
					Expect(19);

#line  1056 "Frames/cs.ATG" 
					IndexerDeclaration indexer = new IndexerDeclaration(type, p, m.Modifier, attributes);
					indexer.StartLocation = m.GetDeclarationLocation(startPos);
					indexer.EndLocation   = t.EndLocation;
					if (explicitInterface != null)
					SafeAdd(indexer, indexer.InterfaceImplementations, new InterfaceImplementation(explicitInterface, "this"));
					      PropertyGetRegion getRegion;
					      PropertySetRegion setRegion;
					    
					Expect(16);

#line  1064 "Frames/cs.ATG" 
					Location bodyStart = t.Location; 
					AccessorDecls(
#line  1065 "Frames/cs.ATG" 
out getRegion, out setRegion);
					Expect(17);

#line  1066 "Frames/cs.ATG" 
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

#line  1093 "Frames/cs.ATG" 
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
#line  1106 "Frames/cs.ATG" 
out section);

#line  1106 "Frames/cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 89) {
			lexer.NextToken();

#line  1107 "Frames/cs.ATG" 
			mod = Modifiers.New; startLocation = t.Location; 
		}
		if (
#line  1110 "Frames/cs.ATG" 
NotVoidPointer()) {
			Expect(123);

#line  1110 "Frames/cs.ATG" 
			if (startLocation.IsEmpty) startLocation = t.Location; 
			Identifier();

#line  1111 "Frames/cs.ATG" 
			name = t.val; 
			if (la.kind == 23) {
				TypeParameterList(
#line  1112 "Frames/cs.ATG" 
templates);
			}
			Expect(20);
			if (StartOf(11)) {
				FormalParameterList(
#line  1113 "Frames/cs.ATG" 
parameters);
			}
			Expect(21);
			while (la.kind == 127) {
				TypeParameterConstraintsClause(
#line  1114 "Frames/cs.ATG" 
templates);
			}
			Expect(11);

#line  1116 "Frames/cs.ATG" 
			MethodDeclaration md = new MethodDeclaration {
			Name = name, Modifier = mod, TypeReference = new TypeReference("System.Void", true), 
			Parameters = parameters, Attributes = attributes, Templates = templates,
			StartLocation = startLocation, EndLocation = t.EndLocation
			};
			compilationUnit.AddChild(md);
			
		} else if (StartOf(23)) {
			if (StartOf(10)) {
				Type(
#line  1124 "Frames/cs.ATG" 
out type);

#line  1124 "Frames/cs.ATG" 
				if (startLocation.IsEmpty) startLocation = t.Location; 
				if (StartOf(19)) {
					Identifier();

#line  1126 "Frames/cs.ATG" 
					name = t.val; Location qualIdentEndLocation = t.EndLocation; 
					if (la.kind == 20 || la.kind == 23) {
						if (la.kind == 23) {
							TypeParameterList(
#line  1130 "Frames/cs.ATG" 
templates);
						}
						Expect(20);
						if (StartOf(11)) {
							FormalParameterList(
#line  1131 "Frames/cs.ATG" 
parameters);
						}
						Expect(21);
						while (la.kind == 127) {
							TypeParameterConstraintsClause(
#line  1133 "Frames/cs.ATG" 
templates);
						}
						Expect(11);

#line  1134 "Frames/cs.ATG" 
						MethodDeclaration md = new MethodDeclaration {
						Name = name, Modifier = mod, TypeReference = type,
						Parameters = parameters, Attributes = attributes, Templates = templates,
						StartLocation = startLocation, EndLocation = t.EndLocation
						};
						compilationUnit.AddChild(md);
						
					} else if (la.kind == 16) {

#line  1143 "Frames/cs.ATG" 
						PropertyDeclaration pd = new PropertyDeclaration(name, type, mod, attributes);
						compilationUnit.AddChild(pd); 
						lexer.NextToken();

#line  1146 "Frames/cs.ATG" 
						Location bodyStart = t.Location;
						InterfaceAccessors(
#line  1147 "Frames/cs.ATG" 
out getBlock, out setBlock);
						Expect(17);

#line  1148 "Frames/cs.ATG" 
						pd.GetRegion = getBlock; pd.SetRegion = setBlock; pd.StartLocation = startLocation; pd.EndLocation = qualIdentEndLocation; pd.BodyStart = bodyStart; pd.BodyEnd = t.EndLocation; 
					} else SynErr(175);
				} else if (la.kind == 111) {
					lexer.NextToken();
					Expect(18);
					FormalParameterList(
#line  1151 "Frames/cs.ATG" 
parameters);
					Expect(19);

#line  1152 "Frames/cs.ATG" 
					Location bracketEndLocation = t.EndLocation; 

#line  1153 "Frames/cs.ATG" 
					IndexerDeclaration id = new IndexerDeclaration(type, parameters, mod, attributes);
					compilationUnit.AddChild(id); 
					Expect(16);

#line  1155 "Frames/cs.ATG" 
					Location bodyStart = t.Location;
					InterfaceAccessors(
#line  1156 "Frames/cs.ATG" 
out getBlock, out setBlock);
					Expect(17);

#line  1158 "Frames/cs.ATG" 
					id.GetRegion = getBlock; id.SetRegion = setBlock; id.StartLocation = startLocation;  id.EndLocation = bracketEndLocation; id.BodyStart = bodyStart; id.BodyEnd = t.EndLocation;
				} else SynErr(176);
			} else {
				lexer.NextToken();

#line  1161 "Frames/cs.ATG" 
				if (startLocation.IsEmpty) startLocation = t.Location; 
				Type(
#line  1162 "Frames/cs.ATG" 
out type);
				Identifier();

#line  1163 "Frames/cs.ATG" 
				EventDeclaration ed = new EventDeclaration {
				TypeReference = type, Name = t.val, Modifier = mod, Attributes = attributes
				};
				compilationUnit.AddChild(ed);
				
				Expect(11);

#line  1169 "Frames/cs.ATG" 
				ed.StartLocation = startLocation; ed.EndLocation = t.EndLocation; 
			}
		} else SynErr(177);
	}

	void EnumMemberDecl(
#line  1174 "Frames/cs.ATG" 
out FieldDeclaration f) {

#line  1176 "Frames/cs.ATG" 
		Expression expr = null;
		List<AttributeSection> attributes = new List<AttributeSection>();
		AttributeSection section = null;
		VariableDeclaration varDecl = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1182 "Frames/cs.ATG" 
out section);

#line  1182 "Frames/cs.ATG" 
			attributes.Add(section); 
		}
		Identifier();

#line  1183 "Frames/cs.ATG" 
		f = new FieldDeclaration(attributes);
		varDecl         = new VariableDeclaration(t.val);
		f.Fields.Add(varDecl);
		f.StartLocation = t.Location;
		f.EndLocation = t.EndLocation;
		
		if (la.kind == 3) {
			lexer.NextToken();
			Expr(
#line  1189 "Frames/cs.ATG" 
out expr);

#line  1189 "Frames/cs.ATG" 
			varDecl.Initializer = expr; 
		}
	}

	void TypeWithRestriction(
#line  571 "Frames/cs.ATG" 
out TypeReference type, bool allowNullable, bool canBeUnbound) {

#line  573 "Frames/cs.ATG" 
		Location startPos = la.Location;
		string name;
		int pointer = 0;
		type = null;
		
		if (StartOf(4)) {
			ClassType(
#line  579 "Frames/cs.ATG" 
out type, canBeUnbound);
		} else if (StartOf(5)) {
			SimpleType(
#line  580 "Frames/cs.ATG" 
out name);

#line  580 "Frames/cs.ATG" 
			type = new TypeReference(name, true); 
		} else if (la.kind == 123) {
			lexer.NextToken();
			Expect(6);

#line  581 "Frames/cs.ATG" 
			pointer = 1; type = new TypeReference("System.Void", true); 
		} else SynErr(178);

#line  582 "Frames/cs.ATG" 
		List<int> r = new List<int>(); 
		if (
#line  584 "Frames/cs.ATG" 
allowNullable && la.kind == Tokens.Question) {
			NullableQuestionMark(
#line  584 "Frames/cs.ATG" 
ref type);
		}
		while (
#line  586 "Frames/cs.ATG" 
IsPointerOrDims()) {

#line  586 "Frames/cs.ATG" 
			int i = 0; 
			if (la.kind == 6) {
				lexer.NextToken();

#line  587 "Frames/cs.ATG" 
				++pointer; 
			} else if (la.kind == 18) {
				lexer.NextToken();
				while (la.kind == 14) {
					lexer.NextToken();

#line  588 "Frames/cs.ATG" 
					++i; 
				}
				Expect(19);

#line  588 "Frames/cs.ATG" 
				r.Add(i); 
			} else SynErr(179);
		}

#line  591 "Frames/cs.ATG" 
		if (type != null) {
		type.RankSpecifier = r.ToArray();
		type.PointerNestingLevel = pointer;
		type.EndLocation = t.EndLocation;
		type.StartLocation = startPos;
		}
		
	}

	void SimpleType(
#line  627 "Frames/cs.ATG" 
out string name) {

#line  628 "Frames/cs.ATG" 
		name = String.Empty; 
		if (StartOf(24)) {
			IntegralType(
#line  630 "Frames/cs.ATG" 
out name);
		} else if (la.kind == 75) {
			lexer.NextToken();

#line  631 "Frames/cs.ATG" 
			name = "System.Single"; 
		} else if (la.kind == 66) {
			lexer.NextToken();

#line  632 "Frames/cs.ATG" 
			name = "System.Double"; 
		} else if (la.kind == 62) {
			lexer.NextToken();

#line  633 "Frames/cs.ATG" 
			name = "System.Decimal"; 
		} else if (la.kind == 52) {
			lexer.NextToken();

#line  634 "Frames/cs.ATG" 
			name = "System.Boolean"; 
		} else SynErr(180);
	}

	void NullableQuestionMark(
#line  2355 "Frames/cs.ATG" 
ref TypeReference typeRef) {

#line  2356 "Frames/cs.ATG" 
		List<TypeReference> typeArguments = new List<TypeReference>(1); 
		Expect(12);

#line  2360 "Frames/cs.ATG" 
		if (typeRef != null) typeArguments.Add(typeRef);
		typeRef = new TypeReference("System.Nullable", typeArguments) { IsKeyword = true };
		
	}

	void FixedParameter(
#line  664 "Frames/cs.ATG" 
out ParameterDeclarationExpression p) {

#line  666 "Frames/cs.ATG" 
		TypeReference type;
		ParameterModifiers mod = ParameterModifiers.In;
		Location start = la.Location;
		Expression defaultParameterExpr = null;
		
		if (la.kind == 93 || la.kind == 100) {
			if (la.kind == 100) {
				lexer.NextToken();

#line  673 "Frames/cs.ATG" 
				mod = ParameterModifiers.Ref; 
			} else {
				lexer.NextToken();

#line  674 "Frames/cs.ATG" 
				mod = ParameterModifiers.Out; 
			}
		}
		Type(
#line  676 "Frames/cs.ATG" 
out type);
		Identifier();

#line  676 "Frames/cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, mod); p.StartLocation = start; p.EndLocation = t.Location; 
		if (la.kind == 3) {
			lexer.NextToken();
			Expr(
#line  677 "Frames/cs.ATG" 
out defaultParameterExpr);
		}
	}

	void ParameterArray(
#line  680 "Frames/cs.ATG" 
out ParameterDeclarationExpression p) {

#line  681 "Frames/cs.ATG" 
		TypeReference type; 
		Expect(95);
		Type(
#line  683 "Frames/cs.ATG" 
out type);
		Identifier();

#line  683 "Frames/cs.ATG" 
		p = new ParameterDeclarationExpression(type, t.val, ParameterModifiers.Params); 
	}

	void AccessorModifiers(
#line  686 "Frames/cs.ATG" 
out ModifierList m) {

#line  687 "Frames/cs.ATG" 
		m = new ModifierList(); 
		if (la.kind == 96) {
			lexer.NextToken();

#line  689 "Frames/cs.ATG" 
			m.Add(Modifiers.Private, t.Location); 
		} else if (la.kind == 97) {
			lexer.NextToken();

#line  690 "Frames/cs.ATG" 
			m.Add(Modifiers.Protected, t.Location); 
			if (la.kind == 84) {
				lexer.NextToken();

#line  691 "Frames/cs.ATG" 
				m.Add(Modifiers.Internal, t.Location); 
			}
		} else if (la.kind == 84) {
			lexer.NextToken();

#line  692 "Frames/cs.ATG" 
			m.Add(Modifiers.Internal, t.Location); 
			if (la.kind == 97) {
				lexer.NextToken();

#line  693 "Frames/cs.ATG" 
				m.Add(Modifiers.Protected, t.Location); 
			}
		} else SynErr(181);
	}

	void Block(
#line  1309 "Frames/cs.ATG" 
out Statement stmt) {
		Expect(16);

#line  1311 "Frames/cs.ATG" 
		BlockStatement blockStmt = new BlockStatement();
		blockStmt.StartLocation = t.Location;
		compilationUnit.BlockStart(blockStmt);
		if (!ParseMethodBodies) lexer.SkipCurrentBlock(0);
		
		while (StartOf(25)) {
			Statement();
		}
		while (!(la.kind == 0 || la.kind == 17)) {SynErr(182); lexer.NextToken(); }
		Expect(17);

#line  1319 "Frames/cs.ATG" 
		stmt = blockStmt;
		blockStmt.EndLocation = t.Kind != Tokens.CloseCurlyBrace ? Location.Empty : t.EndLocation;
		compilationUnit.BlockEnd();
		
	}

	void EventAccessorDecls(
#line  1246 "Frames/cs.ATG" 
out EventAddRegion addBlock, out EventRemoveRegion removeBlock) {

#line  1247 "Frames/cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		Statement stmt;
		addBlock = null;
		removeBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1254 "Frames/cs.ATG" 
out section);

#line  1254 "Frames/cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 130) {

#line  1256 "Frames/cs.ATG" 
			addBlock = new EventAddRegion(attributes); 
			AddAccessorDecl(
#line  1257 "Frames/cs.ATG" 
out stmt);

#line  1257 "Frames/cs.ATG" 
			attributes = new List<AttributeSection>(); addBlock.Block = (BlockStatement)stmt; 
			while (la.kind == 18) {
				AttributeSection(
#line  1258 "Frames/cs.ATG" 
out section);

#line  1258 "Frames/cs.ATG" 
				attributes.Add(section); 
			}
			RemoveAccessorDecl(
#line  1259 "Frames/cs.ATG" 
out stmt);

#line  1259 "Frames/cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; 
		} else if (la.kind == 131) {
			RemoveAccessorDecl(
#line  1261 "Frames/cs.ATG" 
out stmt);

#line  1261 "Frames/cs.ATG" 
			removeBlock = new EventRemoveRegion(attributes); removeBlock.Block = (BlockStatement)stmt; attributes = new List<AttributeSection>(); 
			while (la.kind == 18) {
				AttributeSection(
#line  1262 "Frames/cs.ATG" 
out section);

#line  1262 "Frames/cs.ATG" 
				attributes.Add(section); 
			}
			AddAccessorDecl(
#line  1263 "Frames/cs.ATG" 
out stmt);

#line  1263 "Frames/cs.ATG" 
			addBlock = new EventAddRegion(attributes); addBlock.Block = (BlockStatement)stmt; 
		} else SynErr(183);
	}

	void ConstructorInitializer(
#line  1339 "Frames/cs.ATG" 
out ConstructorInitializer ci) {

#line  1340 "Frames/cs.ATG" 
		Expression expr; ci = new ConstructorInitializer(); 
		Expect(9);
		if (la.kind == 51) {
			lexer.NextToken();

#line  1344 "Frames/cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.Base; 
		} else if (la.kind == 111) {
			lexer.NextToken();

#line  1345 "Frames/cs.ATG" 
			ci.ConstructorInitializerType = ConstructorInitializerType.This; 
		} else SynErr(184);
		Expect(20);
		if (StartOf(26)) {
			Argument(
#line  1348 "Frames/cs.ATG" 
out expr);

#line  1348 "Frames/cs.ATG" 
			SafeAdd(ci, ci.Arguments, expr); 
			while (la.kind == 14) {
				lexer.NextToken();
				Argument(
#line  1349 "Frames/cs.ATG" 
out expr);

#line  1349 "Frames/cs.ATG" 
				SafeAdd(ci, ci.Arguments, expr); 
			}
		}
		Expect(21);
	}

	void OverloadableOperator(
#line  1362 "Frames/cs.ATG" 
out OverloadableOperatorType op) {

#line  1363 "Frames/cs.ATG" 
		op = OverloadableOperatorType.None; 
		switch (la.kind) {
		case 4: {
			lexer.NextToken();

#line  1365 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Add; 
			break;
		}
		case 5: {
			lexer.NextToken();

#line  1366 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Subtract; 
			break;
		}
		case 24: {
			lexer.NextToken();

#line  1368 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Not; 
			break;
		}
		case 27: {
			lexer.NextToken();

#line  1369 "Frames/cs.ATG" 
			op = OverloadableOperatorType.BitNot; 
			break;
		}
		case 31: {
			lexer.NextToken();

#line  1371 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Increment; 
			break;
		}
		case 32: {
			lexer.NextToken();

#line  1372 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Decrement; 
			break;
		}
		case 113: {
			lexer.NextToken();

#line  1374 "Frames/cs.ATG" 
			op = OverloadableOperatorType.IsTrue; 
			break;
		}
		case 72: {
			lexer.NextToken();

#line  1375 "Frames/cs.ATG" 
			op = OverloadableOperatorType.IsFalse; 
			break;
		}
		case 6: {
			lexer.NextToken();

#line  1377 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Multiply; 
			break;
		}
		case 7: {
			lexer.NextToken();

#line  1378 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Divide; 
			break;
		}
		case 8: {
			lexer.NextToken();

#line  1379 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Modulus; 
			break;
		}
		case 28: {
			lexer.NextToken();

#line  1381 "Frames/cs.ATG" 
			op = OverloadableOperatorType.BitwiseAnd; 
			break;
		}
		case 29: {
			lexer.NextToken();

#line  1382 "Frames/cs.ATG" 
			op = OverloadableOperatorType.BitwiseOr; 
			break;
		}
		case 30: {
			lexer.NextToken();

#line  1383 "Frames/cs.ATG" 
			op = OverloadableOperatorType.ExclusiveOr; 
			break;
		}
		case 37: {
			lexer.NextToken();

#line  1385 "Frames/cs.ATG" 
			op = OverloadableOperatorType.ShiftLeft; 
			break;
		}
		case 33: {
			lexer.NextToken();

#line  1386 "Frames/cs.ATG" 
			op = OverloadableOperatorType.Equality; 
			break;
		}
		case 34: {
			lexer.NextToken();

#line  1387 "Frames/cs.ATG" 
			op = OverloadableOperatorType.InEquality; 
			break;
		}
		case 23: {
			lexer.NextToken();

#line  1388 "Frames/cs.ATG" 
			op = OverloadableOperatorType.LessThan; 
			break;
		}
		case 35: {
			lexer.NextToken();

#line  1389 "Frames/cs.ATG" 
			op = OverloadableOperatorType.GreaterThanOrEqual; 
			break;
		}
		case 36: {
			lexer.NextToken();

#line  1390 "Frames/cs.ATG" 
			op = OverloadableOperatorType.LessThanOrEqual; 
			break;
		}
		case 22: {
			lexer.NextToken();

#line  1391 "Frames/cs.ATG" 
			op = OverloadableOperatorType.GreaterThan; 
			if (la.kind == 22) {
				lexer.NextToken();

#line  1391 "Frames/cs.ATG" 
				op = OverloadableOperatorType.ShiftRight; 
			}
			break;
		}
		default: SynErr(185); break;
		}
	}

	void VariableDeclarator(
#line  1301 "Frames/cs.ATG" 
FieldDeclaration parentFieldDeclaration) {

#line  1302 "Frames/cs.ATG" 
		Expression expr = null; 
		Identifier();

#line  1304 "Frames/cs.ATG" 
		VariableDeclaration f = new VariableDeclaration(t.val); f.StartLocation = t.Location; 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1305 "Frames/cs.ATG" 
out expr);

#line  1305 "Frames/cs.ATG" 
			f.Initializer = expr; 
		}

#line  1306 "Frames/cs.ATG" 
		f.EndLocation = t.EndLocation; SafeAdd(parentFieldDeclaration, parentFieldDeclaration.Fields, f); 
	}

	void AccessorDecls(
#line  1193 "Frames/cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1195 "Frames/cs.ATG" 
		List<AttributeSection> attributes = new List<AttributeSection>(); 
		AttributeSection section;
		getBlock = null;
		setBlock = null; 
		ModifierList modifiers = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1202 "Frames/cs.ATG" 
out section);

#line  1202 "Frames/cs.ATG" 
			attributes.Add(section); 
		}
		if (la.kind == 84 || la.kind == 96 || la.kind == 97) {
			AccessorModifiers(
#line  1203 "Frames/cs.ATG" 
out modifiers);
		}
		if (la.kind == 128) {
			GetAccessorDecl(
#line  1205 "Frames/cs.ATG" 
out getBlock, attributes);

#line  1206 "Frames/cs.ATG" 
			if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(27)) {

#line  1207 "Frames/cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1208 "Frames/cs.ATG" 
out section);

#line  1208 "Frames/cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 84 || la.kind == 96 || la.kind == 97) {
					AccessorModifiers(
#line  1209 "Frames/cs.ATG" 
out modifiers);
				}
				SetAccessorDecl(
#line  1210 "Frames/cs.ATG" 
out setBlock, attributes);

#line  1211 "Frames/cs.ATG" 
				if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (la.kind == 129) {
			SetAccessorDecl(
#line  1214 "Frames/cs.ATG" 
out setBlock, attributes);

#line  1215 "Frames/cs.ATG" 
			if (modifiers != null) {setBlock.Modifier = modifiers.Modifier; } 
			if (StartOf(28)) {

#line  1216 "Frames/cs.ATG" 
				attributes = new List<AttributeSection>(); modifiers = null; 
				while (la.kind == 18) {
					AttributeSection(
#line  1217 "Frames/cs.ATG" 
out section);

#line  1217 "Frames/cs.ATG" 
					attributes.Add(section); 
				}
				if (la.kind == 84 || la.kind == 96 || la.kind == 97) {
					AccessorModifiers(
#line  1218 "Frames/cs.ATG" 
out modifiers);
				}
				GetAccessorDecl(
#line  1219 "Frames/cs.ATG" 
out getBlock, attributes);

#line  1220 "Frames/cs.ATG" 
				if (modifiers != null) {getBlock.Modifier = modifiers.Modifier; } 
			}
		} else if (StartOf(19)) {
			Identifier();

#line  1222 "Frames/cs.ATG" 
			Error("get or set accessor declaration expected"); 
		} else SynErr(186);
	}

	void InterfaceAccessors(
#line  1267 "Frames/cs.ATG" 
out PropertyGetRegion getBlock, out PropertySetRegion setBlock) {

#line  1269 "Frames/cs.ATG" 
		AttributeSection section;
		List<AttributeSection> attributes = new List<AttributeSection>();
		getBlock = null; setBlock = null;
		PropertyGetSetRegion lastBlock = null;
		
		while (la.kind == 18) {
			AttributeSection(
#line  1275 "Frames/cs.ATG" 
out section);

#line  1275 "Frames/cs.ATG" 
			attributes.Add(section); 
		}

#line  1276 "Frames/cs.ATG" 
		Location startLocation = la.Location; 
		if (la.kind == 128) {
			lexer.NextToken();

#line  1278 "Frames/cs.ATG" 
			getBlock = new PropertyGetRegion(null, attributes); 
		} else if (la.kind == 129) {
			lexer.NextToken();

#line  1279 "Frames/cs.ATG" 
			setBlock = new PropertySetRegion(null, attributes); 
		} else SynErr(187);
		Expect(11);

#line  1282 "Frames/cs.ATG" 
		if (getBlock != null) { getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; }
		if (setBlock != null) { setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; }
		attributes = new List<AttributeSection>(); 
		if (la.kind == 18 || la.kind == 128 || la.kind == 129) {
			while (la.kind == 18) {
				AttributeSection(
#line  1286 "Frames/cs.ATG" 
out section);

#line  1286 "Frames/cs.ATG" 
				attributes.Add(section); 
			}

#line  1287 "Frames/cs.ATG" 
			startLocation = la.Location; 
			if (la.kind == 128) {
				lexer.NextToken();

#line  1289 "Frames/cs.ATG" 
				if (getBlock != null) Error("get already declared");
				                 else { getBlock = new PropertyGetRegion(null, attributes); lastBlock = getBlock; }
				              
			} else if (la.kind == 129) {
				lexer.NextToken();

#line  1292 "Frames/cs.ATG" 
				if (setBlock != null) Error("set already declared");
				                 else { setBlock = new PropertySetRegion(null, attributes); lastBlock = setBlock; }
				              
			} else SynErr(188);
			Expect(11);

#line  1297 "Frames/cs.ATG" 
			if (lastBlock != null) { lastBlock.StartLocation = startLocation; lastBlock.EndLocation = t.EndLocation; } 
		}
	}

	void GetAccessorDecl(
#line  1226 "Frames/cs.ATG" 
out PropertyGetRegion getBlock, List<AttributeSection> attributes) {

#line  1227 "Frames/cs.ATG" 
		Statement stmt = null; 
		Expect(128);

#line  1230 "Frames/cs.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1231 "Frames/cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(189);

#line  1232 "Frames/cs.ATG" 
		getBlock = new PropertyGetRegion((BlockStatement)stmt, attributes); 

#line  1233 "Frames/cs.ATG" 
		getBlock.StartLocation = startLocation; getBlock.EndLocation = t.EndLocation; 
	}

	void SetAccessorDecl(
#line  1236 "Frames/cs.ATG" 
out PropertySetRegion setBlock, List<AttributeSection> attributes) {

#line  1237 "Frames/cs.ATG" 
		Statement stmt = null; 
		Expect(129);

#line  1240 "Frames/cs.ATG" 
		Location startLocation = t.Location; 
		if (la.kind == 16) {
			Block(
#line  1241 "Frames/cs.ATG" 
out stmt);
		} else if (la.kind == 11) {
			lexer.NextToken();
		} else SynErr(190);

#line  1242 "Frames/cs.ATG" 
		setBlock = new PropertySetRegion((BlockStatement)stmt, attributes); 

#line  1243 "Frames/cs.ATG" 
		setBlock.StartLocation = startLocation; setBlock.EndLocation = t.EndLocation; 
	}

	void AddAccessorDecl(
#line  1325 "Frames/cs.ATG" 
out Statement stmt) {

#line  1326 "Frames/cs.ATG" 
		stmt = null;
		Expect(130);
		Block(
#line  1329 "Frames/cs.ATG" 
out stmt);
	}

	void RemoveAccessorDecl(
#line  1332 "Frames/cs.ATG" 
out Statement stmt) {

#line  1333 "Frames/cs.ATG" 
		stmt = null;
		Expect(131);
		Block(
#line  1336 "Frames/cs.ATG" 
out stmt);
	}

	void VariableInitializer(
#line  1354 "Frames/cs.ATG" 
out Expression initializerExpression) {

#line  1355 "Frames/cs.ATG" 
		TypeReference type = null; Expression expr = null; initializerExpression = null; 
		if (StartOf(6)) {
			Expr(
#line  1357 "Frames/cs.ATG" 
out initializerExpression);
		} else if (la.kind == 16) {
			CollectionInitializer(
#line  1358 "Frames/cs.ATG" 
out initializerExpression);
		} else if (la.kind == 106) {
			lexer.NextToken();
			Type(
#line  1359 "Frames/cs.ATG" 
out type);
			Expect(18);
			Expr(
#line  1359 "Frames/cs.ATG" 
out expr);
			Expect(19);

#line  1359 "Frames/cs.ATG" 
			initializerExpression = new StackAllocExpression(type, expr); 
		} else SynErr(191);
	}

	void Statement() {

#line  1502 "Frames/cs.ATG" 
		TypeReference type;
		Expression expr;
		Statement stmt = null;
		Location startPos = la.Location;
		
		while (!(StartOf(29))) {SynErr(192); lexer.NextToken(); }
		if (
#line  1511 "Frames/cs.ATG" 
IsLabel()) {
			Identifier();

#line  1511 "Frames/cs.ATG" 
			compilationUnit.AddChild(new LabelStatement(t.val)); 
			Expect(9);
			Statement();
		} else if (la.kind == 60) {
			lexer.NextToken();
			Type(
#line  1514 "Frames/cs.ATG" 
out type);

#line  1514 "Frames/cs.ATG" 
			LocalVariableDeclaration var = new LocalVariableDeclaration(type, Modifiers.Const); string ident = null; var.StartLocation = t.Location; 
			Identifier();

#line  1515 "Frames/cs.ATG" 
			ident = t.val; Location varStart = t.Location; 
			Expect(3);
			Expr(
#line  1516 "Frames/cs.ATG" 
out expr);

#line  1518 "Frames/cs.ATG" 
			SafeAdd(var, var.Variables, new VariableDeclaration(ident, expr) {
			StartLocation = varStart,
			EndLocation = t.EndLocation,
			TypeReference = type
			}); 
			 
			while (la.kind == 14) {
				lexer.NextToken();
				Identifier();

#line  1524 "Frames/cs.ATG" 
				ident = t.val; 
				Expect(3);
				Expr(
#line  1524 "Frames/cs.ATG" 
out expr);

#line  1526 "Frames/cs.ATG" 
				SafeAdd(var, var.Variables, new VariableDeclaration(ident, expr) {
				StartLocation = varStart,
				EndLocation = t.EndLocation,
				TypeReference = type
				});
				 
			}
			Expect(11);

#line  1532 "Frames/cs.ATG" 
			var.EndLocation = t.EndLocation; if (t.Kind == Tokens.Semicolon) var.SemicolonPosition = t.EndLocation; compilationUnit.AddChild(var); 
		} else if (
#line  1535 "Frames/cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1535 "Frames/cs.ATG" 
out stmt);
			Expect(11);

#line  1535 "Frames/cs.ATG" 
			if (t.Kind == Tokens.Semicolon) ((LocalVariableDeclaration)stmt).SemicolonPosition = t.EndLocation; compilationUnit.AddChild(stmt); 
		} else if (StartOf(30)) {
			EmbeddedStatement(
#line  1537 "Frames/cs.ATG" 
out stmt);

#line  1537 "Frames/cs.ATG" 
			compilationUnit.AddChild(stmt); 
		} else SynErr(193);

#line  1543 "Frames/cs.ATG" 
		if (stmt != null) {
		stmt.StartLocation = startPos;
		stmt.EndLocation = t.EndLocation;
		}
		
	}

	void Argument(
#line  1394 "Frames/cs.ATG" 
out Expression argumentexpr) {

#line  1396 "Frames/cs.ATG" 
		Expression expr;
		FieldDirection fd = FieldDirection.None;
		
		if (la.kind == 93 || la.kind == 100) {
			if (la.kind == 100) {
				lexer.NextToken();

#line  1401 "Frames/cs.ATG" 
				fd = FieldDirection.Ref; 
			} else {
				lexer.NextToken();

#line  1402 "Frames/cs.ATG" 
				fd = FieldDirection.Out; 
			}
		}
		Expr(
#line  1404 "Frames/cs.ATG" 
out expr);

#line  1405 "Frames/cs.ATG" 
		argumentexpr = fd != FieldDirection.None ? argumentexpr = new DirectionExpression(fd, expr) : expr; 
	}

	void CollectionInitializer(
#line  1425 "Frames/cs.ATG" 
out Expression outExpr) {

#line  1427 "Frames/cs.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		
		Expect(16);

#line  1431 "Frames/cs.ATG" 
		initializer.StartLocation = t.Location; 
		if (StartOf(31)) {
			VariableInitializer(
#line  1432 "Frames/cs.ATG" 
out expr);

#line  1433 "Frames/cs.ATG" 
			SafeAdd(initializer, initializer.CreateExpressions, expr); 
			while (
#line  1434 "Frames/cs.ATG" 
NotFinalComma()) {
				Expect(14);
				VariableInitializer(
#line  1435 "Frames/cs.ATG" 
out expr);

#line  1436 "Frames/cs.ATG" 
				SafeAdd(initializer, initializer.CreateExpressions, expr); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);

#line  1440 "Frames/cs.ATG" 
		initializer.EndLocation = t.Location; outExpr = initializer; 
	}

	void AssignmentOperator(
#line  1408 "Frames/cs.ATG" 
out AssignmentOperatorType op) {

#line  1409 "Frames/cs.ATG" 
		op = AssignmentOperatorType.None; 
		if (la.kind == 3) {
			lexer.NextToken();

#line  1411 "Frames/cs.ATG" 
			op = AssignmentOperatorType.Assign; 
		} else if (la.kind == 38) {
			lexer.NextToken();

#line  1412 "Frames/cs.ATG" 
			op = AssignmentOperatorType.Add; 
		} else if (la.kind == 39) {
			lexer.NextToken();

#line  1413 "Frames/cs.ATG" 
			op = AssignmentOperatorType.Subtract; 
		} else if (la.kind == 40) {
			lexer.NextToken();

#line  1414 "Frames/cs.ATG" 
			op = AssignmentOperatorType.Multiply; 
		} else if (la.kind == 41) {
			lexer.NextToken();

#line  1415 "Frames/cs.ATG" 
			op = AssignmentOperatorType.Divide; 
		} else if (la.kind == 42) {
			lexer.NextToken();

#line  1416 "Frames/cs.ATG" 
			op = AssignmentOperatorType.Modulus; 
		} else if (la.kind == 43) {
			lexer.NextToken();

#line  1417 "Frames/cs.ATG" 
			op = AssignmentOperatorType.BitwiseAnd; 
		} else if (la.kind == 44) {
			lexer.NextToken();

#line  1418 "Frames/cs.ATG" 
			op = AssignmentOperatorType.BitwiseOr; 
		} else if (la.kind == 45) {
			lexer.NextToken();

#line  1419 "Frames/cs.ATG" 
			op = AssignmentOperatorType.ExclusiveOr; 
		} else if (la.kind == 46) {
			lexer.NextToken();

#line  1420 "Frames/cs.ATG" 
			op = AssignmentOperatorType.ShiftLeft; 
		} else if (
#line  1421 "Frames/cs.ATG" 
la.kind == Tokens.GreaterThan && Peek(1).kind == Tokens.GreaterEqual) {
			Expect(22);
			Expect(35);

#line  1422 "Frames/cs.ATG" 
			op = AssignmentOperatorType.ShiftRight; 
		} else SynErr(194);
	}

	void CollectionOrObjectInitializer(
#line  1443 "Frames/cs.ATG" 
out Expression outExpr) {

#line  1445 "Frames/cs.ATG" 
		Expression expr = null;
		CollectionInitializerExpression initializer = new CollectionInitializerExpression();
		
		Expect(16);

#line  1449 "Frames/cs.ATG" 
		initializer.StartLocation = t.Location; 
		if (StartOf(31)) {
			ObjectPropertyInitializerOrVariableInitializer(
#line  1450 "Frames/cs.ATG" 
out expr);

#line  1451 "Frames/cs.ATG" 
			SafeAdd(initializer, initializer.CreateExpressions, expr); 
			while (
#line  1452 "Frames/cs.ATG" 
NotFinalComma()) {
				Expect(14);
				ObjectPropertyInitializerOrVariableInitializer(
#line  1453 "Frames/cs.ATG" 
out expr);

#line  1454 "Frames/cs.ATG" 
				SafeAdd(initializer, initializer.CreateExpressions, expr); 
			}
			if (la.kind == 14) {
				lexer.NextToken();
			}
		}
		Expect(17);

#line  1458 "Frames/cs.ATG" 
		initializer.EndLocation = t.Location; outExpr = initializer; 
	}

	void ObjectPropertyInitializerOrVariableInitializer(
#line  1461 "Frames/cs.ATG" 
out Expression expr) {

#line  1462 "Frames/cs.ATG" 
		expr = null; 
		if (
#line  1464 "Frames/cs.ATG" 
IdentAndAsgn()) {
			Identifier();

#line  1466 "Frames/cs.ATG" 
			NamedArgumentExpression nae = new NamedArgumentExpression(t.val, null);
			nae.StartLocation = t.Location;
			Expression r = null; 
			Expect(3);
			if (la.kind == 16) {
				CollectionOrObjectInitializer(
#line  1470 "Frames/cs.ATG" 
out r);
			} else if (StartOf(31)) {
				VariableInitializer(
#line  1471 "Frames/cs.ATG" 
out r);
			} else SynErr(195);

#line  1472 "Frames/cs.ATG" 
			nae.Expression = r; nae.EndLocation = t.EndLocation; expr = nae; 
		} else if (StartOf(31)) {
			VariableInitializer(
#line  1474 "Frames/cs.ATG" 
out expr);
		} else SynErr(196);
	}

	void LocalVariableDecl(
#line  1478 "Frames/cs.ATG" 
out Statement stmt) {

#line  1480 "Frames/cs.ATG" 
		TypeReference type;
		VariableDeclaration      var = null;
		LocalVariableDeclaration localVariableDeclaration; 
		Location startPos = la.Location;
		
		Type(
#line  1486 "Frames/cs.ATG" 
out type);

#line  1486 "Frames/cs.ATG" 
		localVariableDeclaration = new LocalVariableDeclaration(type); localVariableDeclaration.StartLocation = startPos; 
		LocalVariableDeclarator(
#line  1487 "Frames/cs.ATG" 
out var);

#line  1487 "Frames/cs.ATG" 
		SafeAdd(localVariableDeclaration, localVariableDeclaration.Variables, var); 
		while (la.kind == 14) {
			lexer.NextToken();
			LocalVariableDeclarator(
#line  1488 "Frames/cs.ATG" 
out var);

#line  1488 "Frames/cs.ATG" 
			SafeAdd(localVariableDeclaration, localVariableDeclaration.Variables, var); 
		}

#line  1489 "Frames/cs.ATG" 
		stmt = localVariableDeclaration; stmt.EndLocation = t.EndLocation; 
	}

	void LocalVariableDeclarator(
#line  1492 "Frames/cs.ATG" 
out VariableDeclaration var) {

#line  1493 "Frames/cs.ATG" 
		Expression expr = null; 
		Identifier();

#line  1495 "Frames/cs.ATG" 
		var = new VariableDeclaration(t.val); var.StartLocation = t.Location; 
		if (la.kind == 3) {
			lexer.NextToken();
			VariableInitializer(
#line  1496 "Frames/cs.ATG" 
out expr);

#line  1496 "Frames/cs.ATG" 
			var.Initializer = expr; 
		}

#line  1497 "Frames/cs.ATG" 
		var.EndLocation = t.EndLocation; 
	}

	void EmbeddedStatement(
#line  1550 "Frames/cs.ATG" 
out Statement statement) {

#line  1552 "Frames/cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		

#line  1558 "Frames/cs.ATG" 
		Location startLocation = la.Location; 
		if (la.kind == 16) {
			Block(
#line  1560 "Frames/cs.ATG" 
out statement);
		} else if (la.kind == 11) {
			lexer.NextToken();

#line  1563 "Frames/cs.ATG" 
			statement = new EmptyStatement(); 
		} else if (
#line  1566 "Frames/cs.ATG" 
UnCheckedAndLBrace()) {

#line  1566 "Frames/cs.ATG" 
			Statement block; bool isChecked = true; 
			if (la.kind == 58) {
				lexer.NextToken();
			} else if (la.kind == 118) {
				lexer.NextToken();

#line  1567 "Frames/cs.ATG" 
				isChecked = false;
			} else SynErr(197);
			Block(
#line  1568 "Frames/cs.ATG" 
out block);

#line  1568 "Frames/cs.ATG" 
			statement = isChecked ? (Statement)new CheckedStatement(block) : (Statement)new UncheckedStatement(block); 
		} else if (la.kind == 79) {
			IfStatement(
#line  1571 "Frames/cs.ATG" 
out statement);
		} else if (la.kind == 110) {
			lexer.NextToken();

#line  1573 "Frames/cs.ATG" 
			List<SwitchSection> switchSections = new List<SwitchSection>(); 
			Expect(20);
			Expr(
#line  1574 "Frames/cs.ATG" 
out expr);
			Expect(21);
			Expect(16);
			SwitchSections(
#line  1575 "Frames/cs.ATG" 
switchSections);
			Expect(17);

#line  1577 "Frames/cs.ATG" 
			statement = new SwitchStatement(expr, switchSections); 
		} else if (la.kind == 125) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1580 "Frames/cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1581 "Frames/cs.ATG" 
out embeddedStatement);

#line  1582 "Frames/cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.Start);
		} else if (la.kind == 65) {
			lexer.NextToken();
			EmbeddedStatement(
#line  1584 "Frames/cs.ATG" 
out embeddedStatement);
			Expect(125);
			Expect(20);
			Expr(
#line  1585 "Frames/cs.ATG" 
out expr);
			Expect(21);
			Expect(11);

#line  1586 "Frames/cs.ATG" 
			statement = new DoLoopStatement(expr, embeddedStatement, ConditionType.While, ConditionPosition.End); 
		} else if (la.kind == 76) {
			lexer.NextToken();

#line  1588 "Frames/cs.ATG" 
			List<Statement> initializer = null; List<Statement> iterator = null; 
			Expect(20);
			if (StartOf(6)) {
				ForInitializer(
#line  1589 "Frames/cs.ATG" 
out initializer);
			}
			Expect(11);
			if (StartOf(6)) {
				Expr(
#line  1590 "Frames/cs.ATG" 
out expr);
			}
			Expect(11);
			if (StartOf(6)) {
				ForIterator(
#line  1591 "Frames/cs.ATG" 
out iterator);
			}
			Expect(21);
			EmbeddedStatement(
#line  1592 "Frames/cs.ATG" 
out embeddedStatement);

#line  1593 "Frames/cs.ATG" 
			statement = new ForStatement(initializer, expr, iterator, embeddedStatement); 
		} else if (la.kind == 77) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1595 "Frames/cs.ATG" 
out type);
			Identifier();

#line  1595 "Frames/cs.ATG" 
			string varName = t.val; 
			Expect(81);
			Expr(
#line  1596 "Frames/cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1597 "Frames/cs.ATG" 
out embeddedStatement);

#line  1598 "Frames/cs.ATG" 
			statement = new ForeachStatement(type, varName , expr, embeddedStatement); 
		} else if (la.kind == 53) {
			lexer.NextToken();
			Expect(11);

#line  1601 "Frames/cs.ATG" 
			statement = new BreakStatement(); 
		} else if (la.kind == 61) {
			lexer.NextToken();
			Expect(11);

#line  1602 "Frames/cs.ATG" 
			statement = new ContinueStatement(); 
		} else if (la.kind == 78) {
			GotoStatement(
#line  1603 "Frames/cs.ATG" 
out statement);
		} else if (
#line  1605 "Frames/cs.ATG" 
IsYieldStatement()) {
			Expect(132);
			if (la.kind == 101) {
				lexer.NextToken();
				Expr(
#line  1606 "Frames/cs.ATG" 
out expr);

#line  1606 "Frames/cs.ATG" 
				statement = new YieldStatement(new ReturnStatement(expr)); 
			} else if (la.kind == 53) {
				lexer.NextToken();

#line  1607 "Frames/cs.ATG" 
				statement = new YieldStatement(new BreakStatement()); 
			} else SynErr(198);
			Expect(11);
		} else if (la.kind == 101) {
			lexer.NextToken();
			if (StartOf(6)) {
				Expr(
#line  1610 "Frames/cs.ATG" 
out expr);
			}
			Expect(11);

#line  1610 "Frames/cs.ATG" 
			statement = new ReturnStatement(expr); 
		} else if (la.kind == 112) {
			lexer.NextToken();
			if (StartOf(6)) {
				Expr(
#line  1611 "Frames/cs.ATG" 
out expr);
			}
			Expect(11);

#line  1611 "Frames/cs.ATG" 
			statement = new ThrowStatement(expr); 
		} else if (StartOf(6)) {
			StatementExpr(
#line  1614 "Frames/cs.ATG" 
out statement);
			while (!(la.kind == 0 || la.kind == 11)) {SynErr(199); lexer.NextToken(); }
			Expect(11);
		} else if (la.kind == 114) {
			TryStatement(
#line  1617 "Frames/cs.ATG" 
out statement);
		} else if (la.kind == 86) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1620 "Frames/cs.ATG" 
out expr);
			Expect(21);
			EmbeddedStatement(
#line  1621 "Frames/cs.ATG" 
out embeddedStatement);

#line  1621 "Frames/cs.ATG" 
			statement = new LockStatement(expr, embeddedStatement); 
		} else if (la.kind == 121) {

#line  1624 "Frames/cs.ATG" 
			Statement resourceAcquisitionStmt = null; 
			lexer.NextToken();
			Expect(20);
			ResourceAcquisition(
#line  1626 "Frames/cs.ATG" 
out resourceAcquisitionStmt);
			Expect(21);
			EmbeddedStatement(
#line  1627 "Frames/cs.ATG" 
out embeddedStatement);

#line  1627 "Frames/cs.ATG" 
			statement = new UsingStatement(resourceAcquisitionStmt, embeddedStatement); 
		} else if (la.kind == 119) {
			lexer.NextToken();
			Block(
#line  1630 "Frames/cs.ATG" 
out embeddedStatement);

#line  1630 "Frames/cs.ATG" 
			statement = new UnsafeStatement(embeddedStatement); 
		} else if (la.kind == 74) {

#line  1632 "Frames/cs.ATG" 
			Statement pointerDeclarationStmt = null; 
			lexer.NextToken();
			Expect(20);
			ResourceAcquisition(
#line  1634 "Frames/cs.ATG" 
out pointerDeclarationStmt);
			Expect(21);
			EmbeddedStatement(
#line  1635 "Frames/cs.ATG" 
out embeddedStatement);

#line  1635 "Frames/cs.ATG" 
			statement = new FixedStatement(pointerDeclarationStmt, embeddedStatement); 
		} else SynErr(200);

#line  1637 "Frames/cs.ATG" 
		if (statement != null) {
		statement.StartLocation = startLocation;
		statement.EndLocation = t.EndLocation;
		}
		
	}

	void IfStatement(
#line  1644 "Frames/cs.ATG" 
out Statement statement) {

#line  1646 "Frames/cs.ATG" 
		Expression expr = null;
		Statement embeddedStatement = null;
		statement = null;
		Location elseStart = Location.Empty;
		
		Expect(79);
		Expect(20);
		Expr(
#line  1653 "Frames/cs.ATG" 
out expr);
		Expect(21);
		EmbeddedStatement(
#line  1654 "Frames/cs.ATG" 
out embeddedStatement);

#line  1655 "Frames/cs.ATG" 
		Statement elseStatement = null; 
		if (la.kind == 67) {
			lexer.NextToken();

#line  1656 "Frames/cs.ATG" 
			elseStart = t.Location; 
			EmbeddedStatement(
#line  1656 "Frames/cs.ATG" 
out elseStatement);
		}

#line  1657 "Frames/cs.ATG" 
		statement = elseStatement != null ? new IfElseStatement(expr, embeddedStatement, elseStatement) : new IfElseStatement(expr, embeddedStatement); 

#line  1658 "Frames/cs.ATG" 
		if (elseStatement is IfElseStatement && (elseStatement as IfElseStatement).TrueStatement.Count == 1) {
		/* else if-section (otherwise we would have a BlockStatment) */
		ElseIfSection elseIfSection = new ElseIfSection((elseStatement as IfElseStatement).Condition, (elseStatement as IfElseStatement).TrueStatement[0]);
		elseIfSection.StartLocation = elseStart;
		elseIfSection.EndLocation = (elseStatement as IfElseStatement).TrueStatement[0].EndLocation;
		(statement as IfElseStatement).ElseIfSections.Add(elseIfSection);
		(statement as IfElseStatement).ElseIfSections.AddRange((elseStatement as IfElseStatement).ElseIfSections);
		(statement as IfElseStatement).FalseStatement = (elseStatement as IfElseStatement).FalseStatement;
		}
		
	}

	void SwitchSections(
#line  1689 "Frames/cs.ATG" 
List<SwitchSection> switchSections) {

#line  1691 "Frames/cs.ATG" 
		SwitchSection switchSection = new SwitchSection();
		CaseLabel label;
		
		SwitchLabel(
#line  1695 "Frames/cs.ATG" 
out label);

#line  1695 "Frames/cs.ATG" 
		SafeAdd(switchSection, switchSection.SwitchLabels, label); 

#line  1696 "Frames/cs.ATG" 
		compilationUnit.BlockStart(switchSection); 
		while (StartOf(32)) {
			if (la.kind == 55 || la.kind == 63) {
				SwitchLabel(
#line  1698 "Frames/cs.ATG" 
out label);

#line  1699 "Frames/cs.ATG" 
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

#line  1711 "Frames/cs.ATG" 
		compilationUnit.BlockEnd(); switchSections.Add(switchSection); 
	}

	void ForInitializer(
#line  1670 "Frames/cs.ATG" 
out List<Statement> initializer) {

#line  1672 "Frames/cs.ATG" 
		Statement stmt; 
		initializer = new List<Statement>();
		
		if (
#line  1676 "Frames/cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1676 "Frames/cs.ATG" 
out stmt);

#line  1676 "Frames/cs.ATG" 
			((LocalVariableDeclaration)stmt).SemicolonPosition = t.EndLocation; initializer.Add(stmt); 
		} else if (StartOf(6)) {
			StatementExpr(
#line  1677 "Frames/cs.ATG" 
out stmt);

#line  1677 "Frames/cs.ATG" 
			initializer.Add(stmt);
			while (la.kind == 14) {
				lexer.NextToken();
				StatementExpr(
#line  1677 "Frames/cs.ATG" 
out stmt);

#line  1677 "Frames/cs.ATG" 
				initializer.Add(stmt);
			}
		} else SynErr(201);
	}

	void ForIterator(
#line  1680 "Frames/cs.ATG" 
out List<Statement> iterator) {

#line  1682 "Frames/cs.ATG" 
		Statement stmt; 
		iterator = new List<Statement>();
		
		StatementExpr(
#line  1686 "Frames/cs.ATG" 
out stmt);

#line  1686 "Frames/cs.ATG" 
		iterator.Add(stmt);
		while (la.kind == 14) {
			lexer.NextToken();
			StatementExpr(
#line  1686 "Frames/cs.ATG" 
out stmt);

#line  1686 "Frames/cs.ATG" 
			iterator.Add(stmt); 
		}
	}

	void GotoStatement(
#line  1768 "Frames/cs.ATG" 
out Statement stmt) {

#line  1769 "Frames/cs.ATG" 
		Expression expr; stmt = null; 
		Expect(78);
		if (StartOf(19)) {
			Identifier();

#line  1773 "Frames/cs.ATG" 
			stmt = new GotoStatement(t.val); 
			Expect(11);
		} else if (la.kind == 55) {
			lexer.NextToken();
			Expr(
#line  1774 "Frames/cs.ATG" 
out expr);
			Expect(11);

#line  1774 "Frames/cs.ATG" 
			stmt = new GotoCaseStatement(expr); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			Expect(11);

#line  1775 "Frames/cs.ATG" 
			stmt = new GotoCaseStatement(null); 
		} else SynErr(202);
	}

	void StatementExpr(
#line  1795 "Frames/cs.ATG" 
out Statement stmt) {

#line  1796 "Frames/cs.ATG" 
		Expression expr; 
		Expr(
#line  1798 "Frames/cs.ATG" 
out expr);

#line  1801 "Frames/cs.ATG" 
		stmt = new ExpressionStatement(expr); if (expr is PrimitiveExpression) Error("Primitive expressions are not allowed as statements."); 
	}

	void TryStatement(
#line  1721 "Frames/cs.ATG" 
out Statement tryStatement) {

#line  1723 "Frames/cs.ATG" 
		Statement blockStmt = null, finallyStmt = null;
		CatchClause catchClause = null;
		List<CatchClause> catchClauses = new List<CatchClause>();
		
		Expect(114);
		Block(
#line  1728 "Frames/cs.ATG" 
out blockStmt);
		while (la.kind == 56) {
			CatchClause(
#line  1730 "Frames/cs.ATG" 
out catchClause);

#line  1731 "Frames/cs.ATG" 
			if (catchClause != null) catchClauses.Add(catchClause); 
		}
		if (la.kind == 73) {
			lexer.NextToken();
			Block(
#line  1733 "Frames/cs.ATG" 
out finallyStmt);
		}

#line  1735 "Frames/cs.ATG" 
		tryStatement = new TryCatchStatement(blockStmt, catchClauses, finallyStmt);
		if (catchClauses != null) {
			foreach (CatchClause cc in catchClauses) cc.Parent = tryStatement;
		}
		
	}

	void ResourceAcquisition(
#line  1779 "Frames/cs.ATG" 
out Statement stmt) {

#line  1781 "Frames/cs.ATG" 
		stmt = null;
		Expression expr;
		
		if (
#line  1786 "Frames/cs.ATG" 
IsLocalVarDecl()) {
			LocalVariableDecl(
#line  1786 "Frames/cs.ATG" 
out stmt);

#line  1786 "Frames/cs.ATG" 
			((LocalVariableDeclaration)stmt).SemicolonPosition = t.EndLocation; 
		} else if (StartOf(6)) {
			Expr(
#line  1787 "Frames/cs.ATG" 
out expr);

#line  1791 "Frames/cs.ATG" 
			stmt = new ExpressionStatement(expr); 
		} else SynErr(203);
	}

	void SwitchLabel(
#line  1714 "Frames/cs.ATG" 
out CaseLabel label) {

#line  1715 "Frames/cs.ATG" 
		Expression expr = null; label = null; 
		if (la.kind == 55) {
			lexer.NextToken();
			Expr(
#line  1717 "Frames/cs.ATG" 
out expr);
			Expect(9);

#line  1717 "Frames/cs.ATG" 
			label =  new CaseLabel(expr); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			Expect(9);

#line  1718 "Frames/cs.ATG" 
			label =  new CaseLabel(); 
		} else SynErr(204);
	}

	void CatchClause(
#line  1742 "Frames/cs.ATG" 
out CatchClause catchClause) {
		Expect(56);

#line  1744 "Frames/cs.ATG" 
		string identifier;
		Statement stmt;
		TypeReference typeRef;
		Location startPos = t.Location;
		catchClause = null;
		
		if (la.kind == 16) {
			Block(
#line  1752 "Frames/cs.ATG" 
out stmt);

#line  1752 "Frames/cs.ATG" 
			catchClause = new CatchClause(stmt);  
		} else if (la.kind == 20) {
			lexer.NextToken();
			ClassType(
#line  1755 "Frames/cs.ATG" 
out typeRef, false);

#line  1755 "Frames/cs.ATG" 
			identifier = null; 
			if (StartOf(19)) {
				Identifier();

#line  1756 "Frames/cs.ATG" 
				identifier = t.val; 
			}
			Expect(21);
			Block(
#line  1757 "Frames/cs.ATG" 
out stmt);

#line  1758 "Frames/cs.ATG" 
			catchClause = new CatchClause(typeRef, identifier, stmt); 
		} else SynErr(205);

#line  1761 "Frames/cs.ATG" 
		if (catchClause != null) {
		catchClause.StartLocation = startPos;
		catchClause.EndLocation = t.Location;
		}
		
	}

	void UnaryExpr(
#line  1828 "Frames/cs.ATG" 
out Expression uExpr) {

#line  1830 "Frames/cs.ATG" 
		TypeReference type = null;
		Expression expr = null;
		ArrayList expressions = new ArrayList();
		uExpr = null;
		
		while (StartOf(33) || 
#line  1852 "Frames/cs.ATG" 
IsTypeCast()) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  1839 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Plus)); 
			} else if (la.kind == 5) {
				lexer.NextToken();

#line  1840 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Minus)); 
			} else if (la.kind == 24) {
				lexer.NextToken();

#line  1841 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Not)); 
			} else if (la.kind == 27) {
				lexer.NextToken();

#line  1842 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.BitNot)); 
			} else if (la.kind == 6) {
				lexer.NextToken();

#line  1843 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Dereference)); 
			} else if (la.kind == 31) {
				lexer.NextToken();

#line  1844 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Increment)); 
			} else if (la.kind == 32) {
				lexer.NextToken();

#line  1845 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.Decrement)); 
			} else if (la.kind == 28) {
				lexer.NextToken();

#line  1846 "Frames/cs.ATG" 
				expressions.Add(new UnaryOperatorExpression(UnaryOperatorType.AddressOf)); 
			} else {
				Expect(20);
				Type(
#line  1852 "Frames/cs.ATG" 
out type);
				Expect(21);

#line  1852 "Frames/cs.ATG" 
				expressions.Add(new CastExpression(type)); 
			}
		}
		if (
#line  1857 "Frames/cs.ATG" 
LastExpressionIsUnaryMinus(expressions) && IsMostNegativeIntegerWithoutTypeSuffix()) {
			Expect(2);

#line  1860 "Frames/cs.ATG" 
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
#line  1869 "Frames/cs.ATG" 
out expr);
		} else SynErr(206);

#line  1871 "Frames/cs.ATG" 
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
#line  2193 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2194 "Frames/cs.ATG" 
		Expression expr;   
		ConditionalAndExpr(
#line  2196 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 26) {
			lexer.NextToken();
			UnaryExpr(
#line  2196 "Frames/cs.ATG" 
out expr);
			ConditionalAndExpr(
#line  2196 "Frames/cs.ATG" 
ref expr);

#line  2196 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalOr, expr);  
		}
	}

	void PrimaryExpr(
#line  1888 "Frames/cs.ATG" 
out Expression pexpr) {

#line  1890 "Frames/cs.ATG" 
		TypeReference type = null;
		Expression expr;
		pexpr = null;
		

#line  1895 "Frames/cs.ATG" 
		Location startLocation = la.Location; 
		if (la.kind == 113) {
			lexer.NextToken();

#line  1897 "Frames/cs.ATG" 
			pexpr = new PrimitiveExpression(true, "true");  
		} else if (la.kind == 72) {
			lexer.NextToken();

#line  1898 "Frames/cs.ATG" 
			pexpr = new PrimitiveExpression(false, "false"); 
		} else if (la.kind == 90) {
			lexer.NextToken();

#line  1899 "Frames/cs.ATG" 
			pexpr = new PrimitiveExpression(null, "null");  
		} else if (la.kind == 2) {
			lexer.NextToken();

#line  1900 "Frames/cs.ATG" 
			pexpr = new PrimitiveExpression(t.literalValue, t.val) { LiteralFormat = t.literalFormat };  
		} else if (
#line  1901 "Frames/cs.ATG" 
StartOfQueryExpression()) {
			QueryExpression(
#line  1902 "Frames/cs.ATG" 
out pexpr);
		} else if (
#line  1903 "Frames/cs.ATG" 
IdentAndDoubleColon()) {
			Identifier();

#line  1904 "Frames/cs.ATG" 
			type = new TypeReference(t.val); 
			Expect(10);

#line  1905 "Frames/cs.ATG" 
			pexpr = new TypeReferenceExpression(type); 
			Identifier();

#line  1906 "Frames/cs.ATG" 
			if (type.Type == "global") { type.IsGlobal = true; type.Type = t.val ?? "?"; } else type.Type += "." + (t.val ?? "?"); 
		} else if (StartOf(19)) {
			Identifier();

#line  1910 "Frames/cs.ATG" 
			pexpr = new IdentifierExpression(t.val); 
			if (la.kind == 48 || 
#line  1913 "Frames/cs.ATG" 
IsGenericInSimpleNameOrMemberAccess()) {
				if (la.kind == 48) {
					ShortedLambdaExpression(
#line  1912 "Frames/cs.ATG" 
(IdentifierExpression)pexpr, out pexpr);
				} else {

#line  1914 "Frames/cs.ATG" 
					List<TypeReference> typeList; 
					TypeArgumentList(
#line  1915 "Frames/cs.ATG" 
out typeList, false);

#line  1916 "Frames/cs.ATG" 
					((IdentifierExpression)pexpr).TypeArguments = typeList; 
				}
			}
		} else if (
#line  1918 "Frames/cs.ATG" 
IsLambdaExpression()) {
			LambdaExpression(
#line  1919 "Frames/cs.ATG" 
out pexpr);
		} else if (la.kind == 20) {
			lexer.NextToken();
			Expr(
#line  1922 "Frames/cs.ATG" 
out expr);
			Expect(21);

#line  1922 "Frames/cs.ATG" 
			pexpr = new ParenthesizedExpression(expr); 
		} else if (StartOf(35)) {

#line  1925 "Frames/cs.ATG" 
			string val = null; 
			switch (la.kind) {
			case 52: {
				lexer.NextToken();

#line  1926 "Frames/cs.ATG" 
				val = "System.Boolean"; 
				break;
			}
			case 54: {
				lexer.NextToken();

#line  1927 "Frames/cs.ATG" 
				val = "System.Byte"; 
				break;
			}
			case 57: {
				lexer.NextToken();

#line  1928 "Frames/cs.ATG" 
				val = "System.Char"; 
				break;
			}
			case 62: {
				lexer.NextToken();

#line  1929 "Frames/cs.ATG" 
				val = "System.Decimal"; 
				break;
			}
			case 66: {
				lexer.NextToken();

#line  1930 "Frames/cs.ATG" 
				val = "System.Double"; 
				break;
			}
			case 75: {
				lexer.NextToken();

#line  1931 "Frames/cs.ATG" 
				val = "System.Single"; 
				break;
			}
			case 82: {
				lexer.NextToken();

#line  1932 "Frames/cs.ATG" 
				val = "System.Int32"; 
				break;
			}
			case 87: {
				lexer.NextToken();

#line  1933 "Frames/cs.ATG" 
				val = "System.Int64"; 
				break;
			}
			case 91: {
				lexer.NextToken();

#line  1934 "Frames/cs.ATG" 
				val = "System.Object"; 
				break;
			}
			case 102: {
				lexer.NextToken();

#line  1935 "Frames/cs.ATG" 
				val = "System.SByte"; 
				break;
			}
			case 104: {
				lexer.NextToken();

#line  1936 "Frames/cs.ATG" 
				val = "System.Int16"; 
				break;
			}
			case 108: {
				lexer.NextToken();

#line  1937 "Frames/cs.ATG" 
				val = "System.String"; 
				break;
			}
			case 116: {
				lexer.NextToken();

#line  1938 "Frames/cs.ATG" 
				val = "System.UInt32"; 
				break;
			}
			case 117: {
				lexer.NextToken();

#line  1939 "Frames/cs.ATG" 
				val = "System.UInt64"; 
				break;
			}
			case 120: {
				lexer.NextToken();

#line  1940 "Frames/cs.ATG" 
				val = "System.UInt16"; 
				break;
			}
			case 123: {
				lexer.NextToken();

#line  1941 "Frames/cs.ATG" 
				val = "System.Void"; 
				break;
			}
			}

#line  1943 "Frames/cs.ATG" 
			pexpr = new TypeReferenceExpression(new TypeReference(val, true)) { StartLocation = t.Location, EndLocation = t.EndLocation }; 
		} else if (la.kind == 111) {
			lexer.NextToken();

#line  1946 "Frames/cs.ATG" 
			pexpr = new ThisReferenceExpression(); pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation; 
		} else if (la.kind == 51) {
			lexer.NextToken();

#line  1948 "Frames/cs.ATG" 
			pexpr = new BaseReferenceExpression(); pexpr.StartLocation = t.Location; pexpr.EndLocation = t.EndLocation; 
		} else if (la.kind == 89) {
			NewExpression(
#line  1951 "Frames/cs.ATG" 
out pexpr);
		} else if (la.kind == 115) {
			lexer.NextToken();
			Expect(20);
			if (
#line  1955 "Frames/cs.ATG" 
NotVoidPointer()) {
				Expect(123);

#line  1955 "Frames/cs.ATG" 
				type = new TypeReference("System.Void", true); 
			} else if (StartOf(10)) {
				TypeWithRestriction(
#line  1956 "Frames/cs.ATG" 
out type, true, true);
			} else SynErr(207);
			Expect(21);

#line  1958 "Frames/cs.ATG" 
			pexpr = new TypeOfExpression(type); 
		} else if (la.kind == 63) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1960 "Frames/cs.ATG" 
out type);
			Expect(21);

#line  1960 "Frames/cs.ATG" 
			pexpr = new DefaultValueExpression(type); 
		} else if (la.kind == 105) {
			lexer.NextToken();
			Expect(20);
			Type(
#line  1961 "Frames/cs.ATG" 
out type);
			Expect(21);

#line  1961 "Frames/cs.ATG" 
			pexpr = new SizeOfExpression(type); 
		} else if (la.kind == 58) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1962 "Frames/cs.ATG" 
out expr);
			Expect(21);

#line  1962 "Frames/cs.ATG" 
			pexpr = new CheckedExpression(expr); 
		} else if (la.kind == 118) {
			lexer.NextToken();
			Expect(20);
			Expr(
#line  1963 "Frames/cs.ATG" 
out expr);
			Expect(21);

#line  1963 "Frames/cs.ATG" 
			pexpr = new UncheckedExpression(expr); 
		} else if (la.kind == 64) {
			lexer.NextToken();
			AnonymousMethodExpr(
#line  1964 "Frames/cs.ATG" 
out expr);

#line  1964 "Frames/cs.ATG" 
			pexpr = expr; 
		} else SynErr(208);

#line  1966 "Frames/cs.ATG" 
		if (pexpr != null) {
		if (pexpr.StartLocation.IsEmpty)
			pexpr.StartLocation = startLocation;
		if (pexpr.EndLocation.IsEmpty)
			pexpr.EndLocation = t.EndLocation;
		}
		
		while (StartOf(36)) {
			if (la.kind == 31 || la.kind == 32) {

#line  1974 "Frames/cs.ATG" 
				startLocation = la.Location; 
				if (la.kind == 31) {
					lexer.NextToken();

#line  1976 "Frames/cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostIncrement); 
				} else if (la.kind == 32) {
					lexer.NextToken();

#line  1977 "Frames/cs.ATG" 
					pexpr = new UnaryOperatorExpression(pexpr, UnaryOperatorType.PostDecrement); 
				} else SynErr(209);
			} else if (la.kind == 47) {
				PointerMemberAccess(
#line  1980 "Frames/cs.ATG" 
out pexpr, pexpr);
			} else if (la.kind == 15) {
				MemberAccess(
#line  1981 "Frames/cs.ATG" 
out pexpr, pexpr);
			} else if (la.kind == 20) {
				lexer.NextToken();

#line  1985 "Frames/cs.ATG" 
				List<Expression> parameters = new List<Expression>(); 

#line  1986 "Frames/cs.ATG" 
				pexpr = new InvocationExpression(pexpr, parameters); pexpr.StartLocation = startLocation; 
				if (StartOf(26)) {
					Argument(
#line  1987 "Frames/cs.ATG" 
out expr);

#line  1987 "Frames/cs.ATG" 
					SafeAdd(pexpr, parameters, expr); 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  1988 "Frames/cs.ATG" 
out expr);

#line  1988 "Frames/cs.ATG" 
						SafeAdd(pexpr, parameters, expr); 
					}
				}
				Expect(21);
			} else {

#line  1994 "Frames/cs.ATG" 
				List<Expression> indices = new List<Expression>();
				pexpr = new IndexerExpression(pexpr, indices);
				
				lexer.NextToken();
				Expr(
#line  1997 "Frames/cs.ATG" 
out expr);

#line  1997 "Frames/cs.ATG" 
				SafeAdd(pexpr, indices, expr); 
				while (la.kind == 14) {
					lexer.NextToken();
					Expr(
#line  1998 "Frames/cs.ATG" 
out expr);

#line  1998 "Frames/cs.ATG" 
					SafeAdd(pexpr, indices, expr); 
				}
				Expect(19);

#line  2001 "Frames/cs.ATG" 
				if (pexpr != null) {
				pexpr.StartLocation = startLocation;
				pexpr.EndLocation = t.EndLocation;
				}
				
			}
		}
	}

	void QueryExpression(
#line  2431 "Frames/cs.ATG" 
out Expression outExpr) {

#line  2432 "Frames/cs.ATG" 
		QueryExpression q = new QueryExpression(); outExpr = q; q.StartLocation = la.Location; 
		QueryExpressionFromClause fromClause;
		
		QueryExpressionFromClause(
#line  2436 "Frames/cs.ATG" 
out fromClause);

#line  2436 "Frames/cs.ATG" 
		q.FromClause = fromClause; 
		QueryExpressionBody(
#line  2437 "Frames/cs.ATG" 
ref q);

#line  2438 "Frames/cs.ATG" 
		q.EndLocation = t.EndLocation; 
		outExpr = q; /* set outExpr to q again if QueryExpressionBody changed it (can happen with 'into' clauses) */ 
		
	}

	void ShortedLambdaExpression(
#line  2113 "Frames/cs.ATG" 
IdentifierExpression ident, out Expression pexpr) {

#line  2114 "Frames/cs.ATG" 
		LambdaExpression lambda = new LambdaExpression(); pexpr = lambda; 
		Expect(48);

#line  2119 "Frames/cs.ATG" 
		lambda.StartLocation = ident.StartLocation;
		SafeAdd(lambda, lambda.Parameters, new ParameterDeclarationExpression(null, ident.Identifier));
		lambda.Parameters[0].StartLocation = ident.StartLocation;
		lambda.Parameters[0].EndLocation = ident.EndLocation;
		
		LambdaExpressionBody(
#line  2124 "Frames/cs.ATG" 
lambda);
	}

	void TypeArgumentList(
#line  2365 "Frames/cs.ATG" 
out List<TypeReference> types, bool canBeUnbound) {

#line  2367 "Frames/cs.ATG" 
		types = new List<TypeReference>();
		TypeReference type = null;
		
		Expect(23);
		if (
#line  2372 "Frames/cs.ATG" 
canBeUnbound && (la.kind == Tokens.GreaterThan || la.kind == Tokens.Comma)) {

#line  2373 "Frames/cs.ATG" 
			types.Add(TypeReference.Null); 
			while (la.kind == 14) {
				lexer.NextToken();

#line  2374 "Frames/cs.ATG" 
				types.Add(TypeReference.Null); 
			}
		} else if (StartOf(10)) {
			Type(
#line  2375 "Frames/cs.ATG" 
out type);

#line  2375 "Frames/cs.ATG" 
			if (type != null) { types.Add(type); } 
			while (la.kind == 14) {
				lexer.NextToken();
				Type(
#line  2376 "Frames/cs.ATG" 
out type);

#line  2376 "Frames/cs.ATG" 
				if (type != null) { types.Add(type); } 
			}
		} else SynErr(210);
		Expect(22);
	}

	void LambdaExpression(
#line  2093 "Frames/cs.ATG" 
out Expression outExpr) {

#line  2095 "Frames/cs.ATG" 
		LambdaExpression lambda = new LambdaExpression();
		lambda.StartLocation = la.Location;
		ParameterDeclarationExpression p;
		outExpr = lambda;
		
		Expect(20);
		if (StartOf(18)) {
			LambdaExpressionParameter(
#line  2103 "Frames/cs.ATG" 
out p);

#line  2103 "Frames/cs.ATG" 
			SafeAdd(lambda, lambda.Parameters, p); 
			while (la.kind == 14) {
				lexer.NextToken();
				LambdaExpressionParameter(
#line  2105 "Frames/cs.ATG" 
out p);

#line  2105 "Frames/cs.ATG" 
				SafeAdd(lambda, lambda.Parameters, p); 
			}
		}
		Expect(21);
		Expect(48);
		LambdaExpressionBody(
#line  2110 "Frames/cs.ATG" 
lambda);
	}

	void NewExpression(
#line  2040 "Frames/cs.ATG" 
out Expression pexpr) {

#line  2041 "Frames/cs.ATG" 
		pexpr = null;
		List<Expression> parameters = new List<Expression>();
		TypeReference type = null;
		Expression expr;
		
		Expect(89);
		if (StartOf(10)) {
			NonArrayType(
#line  2048 "Frames/cs.ATG" 
out type);
		}
		if (la.kind == 16 || la.kind == 20) {
			if (la.kind == 20) {

#line  2054 "Frames/cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters); 
				lexer.NextToken();

#line  2055 "Frames/cs.ATG" 
				if (type == null) Error("Cannot use an anonymous type with arguments for the constructor"); 
				if (StartOf(26)) {
					Argument(
#line  2056 "Frames/cs.ATG" 
out expr);

#line  2056 "Frames/cs.ATG" 
					SafeAdd(oce, parameters, expr); 
					while (la.kind == 14) {
						lexer.NextToken();
						Argument(
#line  2057 "Frames/cs.ATG" 
out expr);

#line  2057 "Frames/cs.ATG" 
						SafeAdd(oce, parameters, expr); 
					}
				}
				Expect(21);

#line  2059 "Frames/cs.ATG" 
				pexpr = oce; 
				if (la.kind == 16) {
					CollectionOrObjectInitializer(
#line  2060 "Frames/cs.ATG" 
out expr);

#line  2060 "Frames/cs.ATG" 
					oce.ObjectInitializer = (CollectionInitializerExpression)expr; 
				}
			} else {

#line  2061 "Frames/cs.ATG" 
				ObjectCreateExpression oce = new ObjectCreateExpression(type, parameters); 
				CollectionOrObjectInitializer(
#line  2062 "Frames/cs.ATG" 
out expr);

#line  2062 "Frames/cs.ATG" 
				oce.ObjectInitializer = (CollectionInitializerExpression)expr; 

#line  2063 "Frames/cs.ATG" 
				pexpr = oce; 
			}
		} else if (la.kind == 18) {
			lexer.NextToken();

#line  2068 "Frames/cs.ATG" 
			ArrayCreateExpression ace = new ArrayCreateExpression(type);
			/* we must not change RankSpecifier on the null type reference*/
			if (ace.CreateType.IsNull) { ace.CreateType = new TypeReference(""); }
			pexpr = ace;
			int dims = 0; List<int> ranks = new List<int>();
			
			if (la.kind == 14 || la.kind == 19) {
				while (la.kind == 14) {
					lexer.NextToken();

#line  2075 "Frames/cs.ATG" 
					dims += 1; 
				}
				Expect(19);

#line  2076 "Frames/cs.ATG" 
				ranks.Add(dims); dims = 0; 
				while (la.kind == 18) {
					lexer.NextToken();
					while (la.kind == 14) {
						lexer.NextToken();

#line  2077 "Frames/cs.ATG" 
						++dims; 
					}
					Expect(19);

#line  2077 "Frames/cs.ATG" 
					ranks.Add(dims); dims = 0; 
				}

#line  2078 "Frames/cs.ATG" 
				ace.CreateType.RankSpecifier = ranks.ToArray(); 
				CollectionInitializer(
#line  2079 "Frames/cs.ATG" 
out expr);

#line  2079 "Frames/cs.ATG" 
				ace.ArrayInitializer = (CollectionInitializerExpression)expr; 
			} else if (StartOf(6)) {
				Expr(
#line  2080 "Frames/cs.ATG" 
out expr);

#line  2080 "Frames/cs.ATG" 
				if (expr != null) parameters.Add(expr); 
				while (la.kind == 14) {
					lexer.NextToken();

#line  2081 "Frames/cs.ATG" 
					dims += 1; 
					Expr(
#line  2082 "Frames/cs.ATG" 
out expr);

#line  2082 "Frames/cs.ATG" 
					if (expr != null) parameters.Add(expr); 
				}
				Expect(19);

#line  2084 "Frames/cs.ATG" 
				ranks.Add(dims); ace.Arguments = parameters; dims = 0; 
				while (la.kind == 18) {
					lexer.NextToken();
					while (la.kind == 14) {
						lexer.NextToken();

#line  2085 "Frames/cs.ATG" 
						++dims; 
					}
					Expect(19);

#line  2085 "Frames/cs.ATG" 
					ranks.Add(dims); dims = 0; 
				}

#line  2086 "Frames/cs.ATG" 
				ace.CreateType.RankSpecifier = ranks.ToArray(); 
				if (la.kind == 16) {
					CollectionInitializer(
#line  2087 "Frames/cs.ATG" 
out expr);

#line  2087 "Frames/cs.ATG" 
					ace.ArrayInitializer = (CollectionInitializerExpression)expr; 
				}
			} else SynErr(211);
		} else SynErr(212);
	}

	void AnonymousMethodExpr(
#line  2160 "Frames/cs.ATG" 
out Expression outExpr) {

#line  2162 "Frames/cs.ATG" 
		AnonymousMethodExpression expr = new AnonymousMethodExpression();
		expr.StartLocation = t.Location;
		BlockStatement stmt;
		List<ParameterDeclarationExpression> p = new List<ParameterDeclarationExpression>();
		outExpr = expr;
		
		if (la.kind == 20) {
			lexer.NextToken();
			if (StartOf(11)) {
				FormalParameterList(
#line  2171 "Frames/cs.ATG" 
p);

#line  2171 "Frames/cs.ATG" 
				expr.Parameters = p; 
			}
			Expect(21);

#line  2173 "Frames/cs.ATG" 
			expr.HasParameterList = true; 
		}
		BlockInsideExpression(
#line  2175 "Frames/cs.ATG" 
out stmt);

#line  2175 "Frames/cs.ATG" 
		expr.Body  = stmt; 

#line  2176 "Frames/cs.ATG" 
		expr.EndLocation = t.Location; 
	}

	void PointerMemberAccess(
#line  2028 "Frames/cs.ATG" 
out Expression expr, Expression target) {

#line  2029 "Frames/cs.ATG" 
		List<TypeReference> typeList; 
		Expect(47);
		Identifier();

#line  2033 "Frames/cs.ATG" 
		expr = new PointerReferenceExpression(target, t.val); expr.StartLocation = t.Location; expr.EndLocation = t.EndLocation; 
		if (
#line  2034 "Frames/cs.ATG" 
IsGenericInSimpleNameOrMemberAccess()) {
			TypeArgumentList(
#line  2035 "Frames/cs.ATG" 
out typeList, false);

#line  2036 "Frames/cs.ATG" 
			((MemberReferenceExpression)expr).TypeArguments = typeList; 
		}
	}

	void MemberAccess(
#line  2009 "Frames/cs.ATG" 
out Expression expr, Expression target) {

#line  2010 "Frames/cs.ATG" 
		List<TypeReference> typeList; 

#line  2012 "Frames/cs.ATG" 
		if (ShouldConvertTargetExpressionToTypeReference(target)) {
		TypeReference type = GetTypeReferenceFromExpression(target);
		if (type != null) {
			target = new TypeReferenceExpression(type) { StartLocation = t.Location, EndLocation = t.EndLocation };
		}
		}
		
		Expect(15);
		Identifier();

#line  2021 "Frames/cs.ATG" 
		expr = new MemberReferenceExpression(target, t.val); expr.StartLocation = t.Location; expr.EndLocation = t.EndLocation; 
		if (
#line  2022 "Frames/cs.ATG" 
IsGenericInSimpleNameOrMemberAccess()) {
			TypeArgumentList(
#line  2023 "Frames/cs.ATG" 
out typeList, false);

#line  2024 "Frames/cs.ATG" 
			((MemberReferenceExpression)expr).TypeArguments = typeList; 
		}
	}

	void LambdaExpressionParameter(
#line  2127 "Frames/cs.ATG" 
out ParameterDeclarationExpression p) {

#line  2128 "Frames/cs.ATG" 
		Location start = la.Location; p = null;
		TypeReference type;
		ParameterModifiers mod = ParameterModifiers.In;
		
		if (
#line  2133 "Frames/cs.ATG" 
Peek(1).kind == Tokens.Comma || Peek(1).kind == Tokens.CloseParenthesis) {
			Identifier();

#line  2135 "Frames/cs.ATG" 
			p = new ParameterDeclarationExpression(null, t.val);
			p.StartLocation = start; p.EndLocation = t.EndLocation;
			
		} else if (StartOf(18)) {
			if (la.kind == 93 || la.kind == 100) {
				if (la.kind == 100) {
					lexer.NextToken();

#line  2138 "Frames/cs.ATG" 
					mod = ParameterModifiers.Ref; 
				} else {
					lexer.NextToken();

#line  2139 "Frames/cs.ATG" 
					mod = ParameterModifiers.Out; 
				}
			}
			Type(
#line  2141 "Frames/cs.ATG" 
out type);
			Identifier();

#line  2143 "Frames/cs.ATG" 
			p = new ParameterDeclarationExpression(type, t.val, mod);
			p.StartLocation = start; p.EndLocation = t.EndLocation;
			
		} else SynErr(213);
	}

	void LambdaExpressionBody(
#line  2149 "Frames/cs.ATG" 
LambdaExpression lambda) {

#line  2150 "Frames/cs.ATG" 
		Expression expr; BlockStatement stmt; 
		if (la.kind == 16) {
			BlockInsideExpression(
#line  2153 "Frames/cs.ATG" 
out stmt);

#line  2153 "Frames/cs.ATG" 
			lambda.StatementBody = stmt; 
		} else if (StartOf(6)) {
			Expr(
#line  2154 "Frames/cs.ATG" 
out expr);

#line  2154 "Frames/cs.ATG" 
			lambda.ExpressionBody = expr; 
		} else SynErr(214);

#line  2156 "Frames/cs.ATG" 
		lambda.EndLocation = t.EndLocation; 

#line  2157 "Frames/cs.ATG" 
		lambda.ExtendedEndLocation = la.Location; 
	}

	void BlockInsideExpression(
#line  2179 "Frames/cs.ATG" 
out BlockStatement outStmt) {

#line  2180 "Frames/cs.ATG" 
		Statement stmt = null; outStmt = null; 

#line  2184 "Frames/cs.ATG" 
		if (compilationUnit != null) { 
		Block(
#line  2185 "Frames/cs.ATG" 
out stmt);

#line  2185 "Frames/cs.ATG" 
		outStmt = (BlockStatement)stmt; 

#line  2186 "Frames/cs.ATG" 
		} else { 
		Expect(16);

#line  2188 "Frames/cs.ATG" 
		lexer.SkipCurrentBlock(0); 
		Expect(17);

#line  2190 "Frames/cs.ATG" 
		} 
	}

	void ConditionalAndExpr(
#line  2199 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2200 "Frames/cs.ATG" 
		Expression expr; 
		InclusiveOrExpr(
#line  2202 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 25) {
			lexer.NextToken();
			UnaryExpr(
#line  2202 "Frames/cs.ATG" 
out expr);
			InclusiveOrExpr(
#line  2202 "Frames/cs.ATG" 
ref expr);

#line  2202 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.LogicalAnd, expr);  
		}
	}

	void InclusiveOrExpr(
#line  2205 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2206 "Frames/cs.ATG" 
		Expression expr; 
		ExclusiveOrExpr(
#line  2208 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 29) {
			lexer.NextToken();
			UnaryExpr(
#line  2208 "Frames/cs.ATG" 
out expr);
			ExclusiveOrExpr(
#line  2208 "Frames/cs.ATG" 
ref expr);

#line  2208 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseOr, expr);  
		}
	}

	void ExclusiveOrExpr(
#line  2211 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2212 "Frames/cs.ATG" 
		Expression expr; 
		AndExpr(
#line  2214 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 30) {
			lexer.NextToken();
			UnaryExpr(
#line  2214 "Frames/cs.ATG" 
out expr);
			AndExpr(
#line  2214 "Frames/cs.ATG" 
ref expr);

#line  2214 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.ExclusiveOr, expr);  
		}
	}

	void AndExpr(
#line  2217 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2218 "Frames/cs.ATG" 
		Expression expr; 
		EqualityExpr(
#line  2220 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 28) {
			lexer.NextToken();
			UnaryExpr(
#line  2220 "Frames/cs.ATG" 
out expr);
			EqualityExpr(
#line  2220 "Frames/cs.ATG" 
ref expr);

#line  2220 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, BinaryOperatorType.BitwiseAnd, expr);  
		}
	}

	void EqualityExpr(
#line  2223 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2225 "Frames/cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		RelationalExpr(
#line  2229 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 33 || la.kind == 34) {
			if (la.kind == 34) {
				lexer.NextToken();

#line  2232 "Frames/cs.ATG" 
				op = BinaryOperatorType.InEquality; 
			} else {
				lexer.NextToken();

#line  2233 "Frames/cs.ATG" 
				op = BinaryOperatorType.Equality; 
			}
			UnaryExpr(
#line  2235 "Frames/cs.ATG" 
out expr);
			RelationalExpr(
#line  2235 "Frames/cs.ATG" 
ref expr);

#line  2235 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void RelationalExpr(
#line  2239 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2241 "Frames/cs.ATG" 
		TypeReference type;
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		ShiftExpr(
#line  2246 "Frames/cs.ATG" 
ref outExpr);
		while (StartOf(37)) {
			if (StartOf(38)) {
				if (la.kind == 23) {
					lexer.NextToken();

#line  2248 "Frames/cs.ATG" 
					op = BinaryOperatorType.LessThan; 
				} else if (la.kind == 22) {
					lexer.NextToken();

#line  2249 "Frames/cs.ATG" 
					op = BinaryOperatorType.GreaterThan; 
				} else if (la.kind == 36) {
					lexer.NextToken();

#line  2250 "Frames/cs.ATG" 
					op = BinaryOperatorType.LessThanOrEqual; 
				} else if (la.kind == 35) {
					lexer.NextToken();

#line  2251 "Frames/cs.ATG" 
					op = BinaryOperatorType.GreaterThanOrEqual; 
				} else SynErr(215);
				UnaryExpr(
#line  2253 "Frames/cs.ATG" 
out expr);
				ShiftExpr(
#line  2254 "Frames/cs.ATG" 
ref expr);

#line  2255 "Frames/cs.ATG" 
				outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
			} else {
				if (la.kind == 85) {
					lexer.NextToken();
					TypeWithRestriction(
#line  2258 "Frames/cs.ATG" 
out type, false, false);
					if (
#line  2259 "Frames/cs.ATG" 
la.kind == Tokens.Question && !IsPossibleExpressionStart(Peek(1).kind)) {
						NullableQuestionMark(
#line  2260 "Frames/cs.ATG" 
ref type);
					}

#line  2261 "Frames/cs.ATG" 
					outExpr = new TypeOfIsExpression(outExpr, type); 
				} else if (la.kind == 50) {
					lexer.NextToken();
					TypeWithRestriction(
#line  2263 "Frames/cs.ATG" 
out type, false, false);
					if (
#line  2264 "Frames/cs.ATG" 
la.kind == Tokens.Question && !IsPossibleExpressionStart(Peek(1).kind)) {
						NullableQuestionMark(
#line  2265 "Frames/cs.ATG" 
ref type);
					}

#line  2266 "Frames/cs.ATG" 
					outExpr = new CastExpression(type, outExpr, CastType.TryCast); 
				} else SynErr(216);
			}
		}
	}

	void ShiftExpr(
#line  2271 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2273 "Frames/cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		AdditiveExpr(
#line  2277 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 37 || 
#line  2280 "Frames/cs.ATG" 
IsShiftRight()) {
			if (la.kind == 37) {
				lexer.NextToken();

#line  2279 "Frames/cs.ATG" 
				op = BinaryOperatorType.ShiftLeft; 
			} else {
				Expect(22);
				Expect(22);

#line  2281 "Frames/cs.ATG" 
				op = BinaryOperatorType.ShiftRight; 
			}
			UnaryExpr(
#line  2284 "Frames/cs.ATG" 
out expr);
			AdditiveExpr(
#line  2284 "Frames/cs.ATG" 
ref expr);

#line  2284 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void AdditiveExpr(
#line  2288 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2290 "Frames/cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		MultiplicativeExpr(
#line  2294 "Frames/cs.ATG" 
ref outExpr);
		while (la.kind == 4 || la.kind == 5) {
			if (la.kind == 4) {
				lexer.NextToken();

#line  2297 "Frames/cs.ATG" 
				op = BinaryOperatorType.Add; 
			} else {
				lexer.NextToken();

#line  2298 "Frames/cs.ATG" 
				op = BinaryOperatorType.Subtract; 
			}
			UnaryExpr(
#line  2300 "Frames/cs.ATG" 
out expr);
			MultiplicativeExpr(
#line  2300 "Frames/cs.ATG" 
ref expr);

#line  2300 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr);  
		}
	}

	void MultiplicativeExpr(
#line  2304 "Frames/cs.ATG" 
ref Expression outExpr) {

#line  2306 "Frames/cs.ATG" 
		Expression expr;
		BinaryOperatorType op = BinaryOperatorType.None;
		
		while (la.kind == 6 || la.kind == 7 || la.kind == 8) {
			if (la.kind == 6) {
				lexer.NextToken();

#line  2312 "Frames/cs.ATG" 
				op = BinaryOperatorType.Multiply; 
			} else if (la.kind == 7) {
				lexer.NextToken();

#line  2313 "Frames/cs.ATG" 
				op = BinaryOperatorType.Divide; 
			} else {
				lexer.NextToken();

#line  2314 "Frames/cs.ATG" 
				op = BinaryOperatorType.Modulus; 
			}
			UnaryExpr(
#line  2316 "Frames/cs.ATG" 
out expr);

#line  2316 "Frames/cs.ATG" 
			outExpr = new BinaryOperatorExpression(outExpr, op, expr); 
		}
	}

	void TypeParameterConstraintsClauseBase(
#line  2422 "Frames/cs.ATG" 
out TypeReference type) {

#line  2423 "Frames/cs.ATG" 
		TypeReference t; type = null; 
		if (la.kind == 109) {
			lexer.NextToken();

#line  2425 "Frames/cs.ATG" 
			type = TypeReference.StructConstraint; 
		} else if (la.kind == 59) {
			lexer.NextToken();

#line  2426 "Frames/cs.ATG" 
			type = TypeReference.ClassConstraint; 
		} else if (la.kind == 89) {
			lexer.NextToken();
			Expect(20);
			Expect(21);

#line  2427 "Frames/cs.ATG" 
			type = TypeReference.NewConstraint; 
		} else if (StartOf(10)) {
			Type(
#line  2428 "Frames/cs.ATG" 
out t);

#line  2428 "Frames/cs.ATG" 
			type = t; 
		} else SynErr(217);
	}

	void QueryExpressionFromClause(
#line  2443 "Frames/cs.ATG" 
out QueryExpressionFromClause fc) {

#line  2444 "Frames/cs.ATG" 
		fc = new QueryExpressionFromClause(); fc.StartLocation = la.Location; 
		
		Expect(137);
		QueryExpressionFromOrJoinClause(
#line  2448 "Frames/cs.ATG" 
fc);

#line  2449 "Frames/cs.ATG" 
		fc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionBody(
#line  2479 "Frames/cs.ATG" 
ref QueryExpression q) {

#line  2480 "Frames/cs.ATG" 
		QueryExpressionFromClause fromClause;     QueryExpressionWhereClause whereClause;
		QueryExpressionLetClause letClause;       QueryExpressionJoinClause joinClause;
		QueryExpressionOrderClause orderClause;
		QueryExpressionSelectClause selectClause; QueryExpressionGroupClause groupClause;
		
		while (StartOf(39)) {
			if (la.kind == 137) {
				QueryExpressionFromClause(
#line  2486 "Frames/cs.ATG" 
out fromClause);

#line  2486 "Frames/cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, fromClause); 
			} else if (la.kind == 127) {
				QueryExpressionWhereClause(
#line  2487 "Frames/cs.ATG" 
out whereClause);

#line  2487 "Frames/cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, whereClause); 
			} else if (la.kind == 141) {
				QueryExpressionLetClause(
#line  2488 "Frames/cs.ATG" 
out letClause);

#line  2488 "Frames/cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, letClause); 
			} else if (la.kind == 142) {
				QueryExpressionJoinClause(
#line  2489 "Frames/cs.ATG" 
out joinClause);

#line  2489 "Frames/cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, joinClause); 
			} else {
				QueryExpressionOrderByClause(
#line  2490 "Frames/cs.ATG" 
out orderClause);

#line  2490 "Frames/cs.ATG" 
				SafeAdd<QueryExpressionClause>(q, q.MiddleClauses, orderClause); 
			}
		}
		if (la.kind == 133) {
			QueryExpressionSelectClause(
#line  2492 "Frames/cs.ATG" 
out selectClause);

#line  2492 "Frames/cs.ATG" 
			q.SelectOrGroupClause = selectClause; 
		} else if (la.kind == 134) {
			QueryExpressionGroupClause(
#line  2493 "Frames/cs.ATG" 
out groupClause);

#line  2493 "Frames/cs.ATG" 
			q.SelectOrGroupClause = groupClause; 
		} else SynErr(218);
		if (la.kind == 136) {
			QueryExpressionIntoClause(
#line  2495 "Frames/cs.ATG" 
ref q);
		}
	}

	void QueryExpressionFromOrJoinClause(
#line  2469 "Frames/cs.ATG" 
QueryExpressionFromOrJoinClause fjc) {

#line  2470 "Frames/cs.ATG" 
		TypeReference type; Expression expr; 

#line  2472 "Frames/cs.ATG" 
		fjc.Type = null; 
		if (
#line  2473 "Frames/cs.ATG" 
IsLocalVarDecl()) {
			Type(
#line  2473 "Frames/cs.ATG" 
out type);

#line  2473 "Frames/cs.ATG" 
			fjc.Type = type; 
		}
		Identifier();

#line  2474 "Frames/cs.ATG" 
		fjc.Identifier = t.val; 
		Expect(81);
		Expr(
#line  2476 "Frames/cs.ATG" 
out expr);

#line  2476 "Frames/cs.ATG" 
		fjc.InExpression = expr; 
	}

	void QueryExpressionJoinClause(
#line  2452 "Frames/cs.ATG" 
out QueryExpressionJoinClause jc) {

#line  2453 "Frames/cs.ATG" 
		jc = new QueryExpressionJoinClause(); jc.StartLocation = la.Location; 
		Expression expr;
		
		Expect(142);
		QueryExpressionFromOrJoinClause(
#line  2458 "Frames/cs.ATG" 
jc);
		Expect(143);
		Expr(
#line  2460 "Frames/cs.ATG" 
out expr);

#line  2460 "Frames/cs.ATG" 
		jc.OnExpression = expr; 
		Expect(144);
		Expr(
#line  2462 "Frames/cs.ATG" 
out expr);

#line  2462 "Frames/cs.ATG" 
		jc.EqualsExpression = expr; 
		if (la.kind == 136) {
			lexer.NextToken();
			Identifier();

#line  2464 "Frames/cs.ATG" 
			jc.IntoIdentifier = t.val; 
		}

#line  2466 "Frames/cs.ATG" 
		jc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionWhereClause(
#line  2498 "Frames/cs.ATG" 
out QueryExpressionWhereClause wc) {

#line  2499 "Frames/cs.ATG" 
		Expression expr; wc = new QueryExpressionWhereClause(); wc.StartLocation = la.Location; 
		Expect(127);
		Expr(
#line  2502 "Frames/cs.ATG" 
out expr);

#line  2502 "Frames/cs.ATG" 
		wc.Condition = expr; 

#line  2503 "Frames/cs.ATG" 
		wc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionLetClause(
#line  2506 "Frames/cs.ATG" 
out QueryExpressionLetClause wc) {

#line  2507 "Frames/cs.ATG" 
		Expression expr; wc = new QueryExpressionLetClause(); wc.StartLocation = la.Location; 
		Expect(141);
		Identifier();

#line  2510 "Frames/cs.ATG" 
		wc.Identifier = t.val; 
		Expect(3);
		Expr(
#line  2512 "Frames/cs.ATG" 
out expr);

#line  2512 "Frames/cs.ATG" 
		wc.Expression = expr; 

#line  2513 "Frames/cs.ATG" 
		wc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionOrderByClause(
#line  2516 "Frames/cs.ATG" 
out QueryExpressionOrderClause oc) {

#line  2517 "Frames/cs.ATG" 
		QueryExpressionOrdering ordering; oc = new QueryExpressionOrderClause(); oc.StartLocation = la.Location; 
		Expect(140);
		QueryExpressionOrdering(
#line  2520 "Frames/cs.ATG" 
out ordering);

#line  2520 "Frames/cs.ATG" 
		SafeAdd(oc, oc.Orderings, ordering); 
		while (la.kind == 14) {
			lexer.NextToken();
			QueryExpressionOrdering(
#line  2522 "Frames/cs.ATG" 
out ordering);

#line  2522 "Frames/cs.ATG" 
			SafeAdd(oc, oc.Orderings, ordering); 
		}

#line  2524 "Frames/cs.ATG" 
		oc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionSelectClause(
#line  2537 "Frames/cs.ATG" 
out QueryExpressionSelectClause sc) {

#line  2538 "Frames/cs.ATG" 
		Expression expr; sc = new QueryExpressionSelectClause(); sc.StartLocation = la.Location; 
		Expect(133);
		Expr(
#line  2541 "Frames/cs.ATG" 
out expr);

#line  2541 "Frames/cs.ATG" 
		sc.Projection = expr; 

#line  2542 "Frames/cs.ATG" 
		sc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionGroupClause(
#line  2545 "Frames/cs.ATG" 
out QueryExpressionGroupClause gc) {

#line  2546 "Frames/cs.ATG" 
		Expression expr; gc = new QueryExpressionGroupClause(); gc.StartLocation = la.Location; 
		Expect(134);
		Expr(
#line  2549 "Frames/cs.ATG" 
out expr);

#line  2549 "Frames/cs.ATG" 
		gc.Projection = expr; 
		Expect(135);
		Expr(
#line  2551 "Frames/cs.ATG" 
out expr);

#line  2551 "Frames/cs.ATG" 
		gc.GroupBy = expr; 

#line  2552 "Frames/cs.ATG" 
		gc.EndLocation = t.EndLocation; 
	}

	void QueryExpressionIntoClause(
#line  2555 "Frames/cs.ATG" 
ref QueryExpression q) {

#line  2556 "Frames/cs.ATG" 
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

#line  2569 "Frames/cs.ATG" 
		continuedQuery.FromClause.Identifier = t.val; 

#line  2570 "Frames/cs.ATG" 
		continuedQuery.FromClause.EndLocation = t.EndLocation; 
		QueryExpressionBody(
#line  2571 "Frames/cs.ATG" 
ref q);
	}

	void QueryExpressionOrdering(
#line  2527 "Frames/cs.ATG" 
out QueryExpressionOrdering ordering) {

#line  2528 "Frames/cs.ATG" 
		Expression expr; ordering = new QueryExpressionOrdering(); ordering.StartLocation = la.Location; 
		Expr(
#line  2530 "Frames/cs.ATG" 
out expr);

#line  2530 "Frames/cs.ATG" 
		ordering.Criteria = expr; 
		if (la.kind == 138 || la.kind == 139) {
			if (la.kind == 138) {
				lexer.NextToken();

#line  2531 "Frames/cs.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Ascending; 
			} else {
				lexer.NextToken();

#line  2532 "Frames/cs.ATG" 
				ordering.Direction = QueryExpressionOrderingDirection.Descending; 
			}
		}

#line  2534 "Frames/cs.ATG" 
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