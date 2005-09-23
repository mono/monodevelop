// PrettyPrintVisitor.cs
// Copyright (C) 2003 Mike Krueger (mike@icsharpcode.net)
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.

using System;
using System.Text;
using System.Collections;
using System.Diagnostics;

using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
	public class PrettyPrintVisitor : AbstractASTVisitor
	{
		OutputFormatter outputFormatter;
		PrettyPrintOptions prettyPrintOptions = new PrettyPrintOptions();
		
		public string Text {
			get {
				return outputFormatter.Text;
			}
		}
		
		public PrettyPrintOptions PrettyPrintOptions {
			get {
				return prettyPrintOptions;
			}
		}
		
		public PrettyPrintVisitor(string originalSourceFile)
		{
			outputFormatter = new OutputFormatter(originalSourceFile, prettyPrintOptions);
		}
		
		public override object Visit(INode node, object data)
		{
			Errors.Error(-1, -1, String.Format("Visited INode (should NEVER HAPPEN)"));
			Console.WriteLine("Visitor was: " + this.GetType());
			Console.WriteLine("Node was : " + node.GetType());
			return node.AcceptChildren(this, data);
		}
		
		public override object Visit(AttributeSection section, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			if (section.AttributeTarget != null && section.AttributeTarget != String.Empty) {
				outputFormatter.PrintIdentifier(section.AttributeTarget);
				outputFormatter.PrintToken(Tokens.Colon);
				outputFormatter.Space();
			}
			Debug.Assert(section.Attributes != null);
			for (int j = 0; j < section.Attributes.Count; ++j) {
				ICSharpCode.SharpRefactory.Parser.AST.Attribute a = (ICSharpCode.SharpRefactory.Parser.AST.Attribute)section.Attributes[j];
				outputFormatter.PrintIdentifier(a.Name);
				if (a.PositionalArguments != null && a.PositionalArguments.Count > 0) {
					outputFormatter.PrintToken(Tokens.OpenParenthesis);
					this.AppendCommaSeparatedList(a.PositionalArguments);
				
					if (a.NamedArguments != null && a.NamedArguments.Count > 0) {
						if (a.PositionalArguments.Count > 0) {
							outputFormatter.PrintToken(Tokens.Comma);
							outputFormatter.Space();
						}
						for (int i = 0; i < a.NamedArguments.Count; ++i) {
							NamedArgument n = (NamedArgument)a.NamedArguments[i];
							outputFormatter.PrintIdentifier(n.Name);
							outputFormatter.Space();
							outputFormatter.PrintToken(Tokens.Assign);
							outputFormatter.Space();
							n.Expr.AcceptVisitor(this, data);
							if (i + 1 < a.NamedArguments.Count) {
								outputFormatter.PrintToken(Tokens.Comma);
								outputFormatter.Space();
							}
						}
					}
					outputFormatter.PrintToken(Tokens.CloseParenthesis);
				}
				if (j + 1 < section.Attributes.Count) {
					outputFormatter.PrintToken(Tokens.Comma);
					outputFormatter.Space();
				}
			}
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(CompilationUnit compilationUnit, object data)
		{
			compilationUnit.AcceptChildren(this, data);
			outputFormatter.EndFile();
			return null;
		}
		
		public override object Visit(UsingDeclaration usingDeclaration, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Using);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(usingDeclaration.Namespace);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Using);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(usingAliasDeclaration.Alias);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Assign);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(usingAliasDeclaration.Namespace);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			outputFormatter.NewLine ();
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Namespace);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(namespaceDeclaration.NameSpace);
			
			outputFormatter.BeginBrace(this.prettyPrintOptions.NameSpaceBraceStyle);
			
			namespaceDeclaration.AcceptChildren(this, data);
			
			outputFormatter.EndBrace();
			
			return null;
		}
		
		object VisitModifier(Modifier modifier)
		{
			ArrayList tokenList = new ArrayList();
			if ((modifier & Modifier.Unsafe) != 0) {
				tokenList.Add(Tokens.Unsafe);
			}
			if ((modifier & Modifier.Public) != 0) {
				tokenList.Add(Tokens.Public);
			}
			if ((modifier & Modifier.Private) != 0) {
				tokenList.Add(Tokens.Private);
			}
			if ((modifier & Modifier.Protected) != 0) {
				tokenList.Add(Tokens.Protected);
			}
			if ((modifier & Modifier.Static) != 0) {
				tokenList.Add(Tokens.Static);
			}
			if ((modifier & Modifier.Internal) != 0) {
				tokenList.Add(Tokens.Internal);
			}
			if ((modifier & Modifier.Override) != 0) {
				tokenList.Add(Tokens.Override);
			}
			if ((modifier & Modifier.Abstract) != 0) {
				tokenList.Add(Tokens.Abstract);
			}
			if ((modifier & Modifier.Virtual) != 0) {
				tokenList.Add(Tokens.Virtual);
			}
			if ((modifier & Modifier.New) != 0) {
				tokenList.Add(Tokens.New);
			}
			if ((modifier & Modifier.Sealed) != 0) {
				tokenList.Add(Tokens.Sealed);
			}
			if ((modifier & Modifier.Extern) != 0) {
				tokenList.Add(Tokens.Extern);
			}
			if ((modifier & Modifier.Const) != 0) {
				tokenList.Add(Tokens.Const);
			}
			if ((modifier & Modifier.Readonly) != 0) {
				tokenList.Add(Tokens.Readonly);
			}
			if ((modifier & Modifier.Volatile) != 0) {
				tokenList.Add(Tokens.Volatile);
			}
			outputFormatter.PrintTokenList(tokenList);
			return null;
		}
				
		object VisitParamModifiers(ParamModifiers modifier)
		{
			switch (modifier) {
				case ParamModifiers.Out:
					outputFormatter.PrintToken(Tokens.Out);
					break;
				case ParamModifiers.Params:
					outputFormatter.PrintToken(Tokens.Params);
					break;
				case ParamModifiers.Ref:
					outputFormatter.PrintToken(Tokens.Ref);
					break;
			}
			outputFormatter.Space();
			return null;
		}
		
		object VisitAttributes(ArrayList attributes, object data)
		{
			if (attributes == null || attributes.Count <= 0) {
				return null;
			}
			foreach (AttributeSection section in attributes) {
				Visit(section, data);
			}
			return null;
		}
		
		object Visit(TypeReference type, object data)
		{
			outputFormatter.PrintIdentifier(type.Type);
			for (int i = 0; i < type.PointerNestingLevel; ++i) {
				outputFormatter.PrintToken(Tokens.Times);
			}
			if (type.IsArrayType) {
				for (int i = 0; i < type.RankSpecifier.Length; ++i) {
					outputFormatter.PrintToken(Tokens.OpenSquareBracket);
					for (int j = 1; j < type.RankSpecifier[i]; ++j) {
						outputFormatter.PrintToken(Tokens.Comma);
					}
					outputFormatter.PrintToken(Tokens.CloseSquareBracket);
				}
			}
			return null;
		}
		
		object VisitEnumMembers(TypeDeclaration typeDeclaration, object data)
		{
			foreach (FieldDeclaration fieldDeclaration in typeDeclaration.Children) {
				VariableDeclaration f = (VariableDeclaration)fieldDeclaration.Fields[0];
				VisitAttributes(fieldDeclaration.Attributes, data);
				outputFormatter.Indent();
				outputFormatter.PrintIdentifier(f.Name);
				if (f.Initializer != null) {
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Assign);
					outputFormatter.Space();
					f.Initializer.AcceptVisitor(this, data);
				}
				outputFormatter.PrintToken(Tokens.Comma);
				outputFormatter.NewLine();
			}
			return null;
		}
		
		public override object Visit(TypeDeclaration typeDeclaration, object data)
		{
			VisitAttributes(typeDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(typeDeclaration.Modifier);
			switch (typeDeclaration.Type) {
				case Types.Class:
					outputFormatter.PrintToken(Tokens.Class);
					break;
				case Types.Enum:
					outputFormatter.PrintToken(Tokens.Enum);
					break;
				case Types.Interface:
					outputFormatter.PrintToken(Tokens.Interface);
					break;
				case Types.Struct:
					outputFormatter.PrintToken(Tokens.Struct);
					break;
			}
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(typeDeclaration.Name);
			if (typeDeclaration.BaseTypes != null && typeDeclaration.BaseTypes.Count > 0) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Colon);
				for (int i = 0; i < typeDeclaration.BaseTypes.Count; ++i) {
					outputFormatter.Space();
					outputFormatter.PrintIdentifier(typeDeclaration.BaseTypes[i]);
					if (i + 1 < typeDeclaration.BaseTypes.Count) {
						outputFormatter.PrintToken(Tokens.Comma);
						outputFormatter.Space();
					}
				}
			}
			
			switch (typeDeclaration.Type) {
				case Types.Class:
					outputFormatter.BeginBrace(this.prettyPrintOptions.ClassBraceStyle);
					break;
				case Types.Enum:
					outputFormatter.BeginBrace(this.prettyPrintOptions.EnumBraceStyle);
					break;
				case Types.Interface:
					outputFormatter.BeginBrace(this.prettyPrintOptions.InterfaceBraceStyle);
					break;
				case Types.Struct:
					outputFormatter.BeginBrace(this.prettyPrintOptions.StructBraceStyle);
					break;
			}
			
			if (typeDeclaration.Type == Types.Enum) {
				VisitEnumMembers(typeDeclaration, data);
			} else {
				typeDeclaration.AcceptChildren(this, data);
			}
			outputFormatter.EndBrace();
			
			return null;
		}
		
		public override object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			VisitAttributes(parameterDeclarationExpression.Attributes, data);
			VisitParamModifiers(parameterDeclarationExpression.ParamModifiers);
			Visit(parameterDeclarationExpression.TypeReference, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(parameterDeclarationExpression.ParameterName);
			return null;
		}
		
		public override object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
			VisitAttributes(delegateDeclaration.Attributes, data);
			VisitModifier(delegateDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Delegate);
			outputFormatter.Space();
			Visit(delegateDeclaration.ReturnType, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(delegateDeclaration.Name);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(delegateDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(VariableDeclaration variableDeclaration, object data)
		{
			outputFormatter.PrintIdentifier(variableDeclaration.Name);
			if (variableDeclaration.Initializer != null) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Assign);
				outputFormatter.Space();
				variableDeclaration.Initializer.AcceptVisitor(this, data);
			}
			return null;
		}
		
		public void AppendCommaSeparatedList(IList list)
		{
			if (list != null) {
				for (int i = 0; i < list.Count; ++i) {
					((INode)list[i]).AcceptVisitor(this, null);
					if (i + 1 < list.Count) {
						outputFormatter.PrintToken(Tokens.Comma);
						outputFormatter.Space();
					}
					if ((i + 1) % 10 == 0) {
						outputFormatter.NewLine();
						outputFormatter.Indent();
					}
				}
			}
		}
		
		public override object Visit(EventDeclaration eventDeclaration, object data)
		{
			VisitAttributes(eventDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(eventDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Event);
			outputFormatter.Space();
			Visit(eventDeclaration.TypeReference, data);
			outputFormatter.Space();
			
			if (eventDeclaration.VariableDeclarators != null && eventDeclaration.VariableDeclarators.Count > 0) {
				AppendCommaSeparatedList(eventDeclaration.VariableDeclarators);
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.PrintIdentifier(eventDeclaration.Name);
				if (eventDeclaration.AddRegion == null && eventDeclaration.RemoveRegion == null) {
					outputFormatter.PrintToken(Tokens.Semicolon);
					outputFormatter.NewLine();
				} else {
					outputFormatter.BeginBrace(this.prettyPrintOptions.PropertyBraceStyle);
					if (eventDeclaration.AddRegion != null) {
						eventDeclaration.AddRegion.AcceptVisitor(this, data);
					}
					if (eventDeclaration.RemoveRegion != null) {
						eventDeclaration.RemoveRegion.AcceptVisitor(this, data);
					}
					outputFormatter.EndBrace();
				}
			}
			return null;
		}
		
		public override object Visit(EventAddRegion addRegion, object data)
		{
			VisitAttributes(addRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintIdentifier("add");
			if (addRegion.Block == null) {
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.BeginBrace(this.prettyPrintOptions.PropertyGetBraceStyle);
				addRegion.Block.AcceptChildren(this, false);
				outputFormatter.EndBrace();
			}
			return null;
		}
		
		public override object Visit(EventRemoveRegion removeRegion, object data)
		{
			VisitAttributes(removeRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintIdentifier("remove");
			if (removeRegion.Block == null) {
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.BeginBrace(this.prettyPrintOptions.PropertySetBraceStyle);
				removeRegion.Block.AcceptChildren(this, false);
				outputFormatter.EndBrace();
			}
			return null;
		}
		
		public override object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			VisitAttributes(fieldDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(fieldDeclaration.Modifier);
			Visit(fieldDeclaration.TypeReference, data);
			outputFormatter.Space();
			AppendCommaSeparatedList(fieldDeclaration.Fields);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			VisitAttributes(constructorDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(constructorDeclaration.Modifier);
			outputFormatter.PrintIdentifier(constructorDeclaration.Name);
			outputFormatter.Space ();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(constructorDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			if (constructorDeclaration.ConstructorInitializer != null) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Colon);
				outputFormatter.Space();
				if (constructorDeclaration.ConstructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base) {
					outputFormatter.PrintToken(Tokens.Base);
				} else {
					outputFormatter.PrintToken(Tokens.This);
				}
				outputFormatter.Space ();
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				AppendCommaSeparatedList(constructorDeclaration.ConstructorInitializer.Arguments);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			
			outputFormatter.BeginBrace(this.prettyPrintOptions.ConstructorBraceStyle);
			constructorDeclaration.Body.AcceptChildren(this, data);
			outputFormatter.EndBrace();
			return null;
		}
		
		public override object Visit(DestructorDeclaration destructorDeclaration, object data)
		{
			VisitAttributes(destructorDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(destructorDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.BitwiseComplement);
			outputFormatter.PrintIdentifier(destructorDeclaration.Name);
			outputFormatter.Space ();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			outputFormatter.BeginBrace(this.prettyPrintOptions.DestructorBraceStyle);
			destructorDeclaration.Body.AcceptChildren(this, data);
			outputFormatter.EndBrace();
			return null;
		}
		
		public override object Visit(MethodDeclaration methodDeclaration, object data)
		{
			VisitAttributes(methodDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(methodDeclaration.Modifier);
			Visit(methodDeclaration.TypeReference, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(methodDeclaration.Name);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(methodDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			if (methodDeclaration.Body == null) {
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.BeginBrace(this.prettyPrintOptions.MethodBraceStyle);
				methodDeclaration.Body.AcceptChildren(this, data);
				outputFormatter.EndBrace();
			}
			return null;
		}
		
		public override object Visit(IndexerDeclaration indexerDeclaration, object data)
		{
			VisitAttributes(indexerDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(indexerDeclaration.Modifier);
			Visit(indexerDeclaration.TypeReference, data);
			outputFormatter.Space();
			if (indexerDeclaration.NamespaceName != null && indexerDeclaration.NamespaceName.Length > 0) {
				outputFormatter.PrintIdentifier(indexerDeclaration.NamespaceName);
				outputFormatter.PrintToken(Tokens.Dot);
			}
			outputFormatter.PrintToken(Tokens.This);
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			AppendCommaSeparatedList(indexerDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			outputFormatter.NewLine();
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			if (indexerDeclaration.GetRegion != null) {
				indexerDeclaration.GetRegion.AcceptVisitor(this, data);
			}
			if (indexerDeclaration.SetRegion != null) {
				indexerDeclaration.SetRegion.AcceptVisitor(this, data);
			}
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			VisitAttributes(propertyDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(propertyDeclaration.Modifier);
			Visit(propertyDeclaration.TypeReference, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(propertyDeclaration.Name);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			if (propertyDeclaration.GetRegion != null) {
				propertyDeclaration.GetRegion.AcceptVisitor(this, data);
			}
			if (propertyDeclaration.SetRegion != null) {
				propertyDeclaration.SetRegion.AcceptVisitor(this, data);
			}
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(PropertyGetRegion getRegion, object data)
		{
			this.VisitAttributes(getRegion.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(getRegion.Modifier);
			outputFormatter.PrintIdentifier("get");
			if (getRegion.Block == null) {
				outputFormatter.PrintToken(Tokens.Semicolon);
			} else {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				outputFormatter.NewLine();
				++outputFormatter.IndentationLevel;
				getRegion.Block.AcceptChildren(this, false);
				--outputFormatter.IndentationLevel;
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(PropertySetRegion setRegion, object data)
		{
			this.VisitAttributes(setRegion.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(setRegion.Modifier);
			outputFormatter.PrintIdentifier("set");
			if (setRegion.Block == null) {
				outputFormatter.PrintToken(Tokens.Semicolon);
			} else {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				outputFormatter.NewLine();
				++outputFormatter.IndentationLevel;
				setRegion.Block.AcceptChildren(this, false);
				--outputFormatter.IndentationLevel;
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(OperatorDeclaration operatorDeclaration, object data)
		{
			VisitAttributes(operatorDeclaration.Attributes, data);
			outputFormatter.Indent();
			VisitModifier(operatorDeclaration.Modifier);
			switch (operatorDeclaration.OpratorDeclarator.OperatorType) {
				case OperatorType.Explicit:
					outputFormatter.PrintToken(Tokens.Explicit);
					break;
				case OperatorType.Implicit:
					outputFormatter.PrintToken(Tokens.Implicit);
					break;
				default:
					Visit(operatorDeclaration.OpratorDeclarator.TypeReference, data);
					break;
			}
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Operator);
			outputFormatter.Space();
			if (!operatorDeclaration.OpratorDeclarator.IsConversion) {
				outputFormatter.PrintIdentifier(Tokens.GetTokenString(operatorDeclaration.OpratorDeclarator.OverloadOperatorToken));
			} else {
				Visit(operatorDeclaration.OpratorDeclarator.TypeReference, data);
			}
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			Visit(operatorDeclaration.OpratorDeclarator.FirstParameterType, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(operatorDeclaration.OpratorDeclarator.FirstParameterName);
			if (operatorDeclaration.OpratorDeclarator.OperatorType == OperatorType.Binary) {
				outputFormatter.PrintToken(Tokens.Comma);
				outputFormatter.Space();
				Visit(operatorDeclaration.OpratorDeclarator.SecondParameterType, data);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier(operatorDeclaration.OpratorDeclarator.SecondParameterName);
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			if (operatorDeclaration.Body == null) {
				outputFormatter.PrintToken(Tokens.Semicolon);
				outputFormatter.NewLine();
			} else {
				outputFormatter.NewLine();
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				outputFormatter.NewLine();
				++outputFormatter.IndentationLevel;
				operatorDeclaration.Body.AcceptChildren(this, data);
				--outputFormatter.IndentationLevel;
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
				outputFormatter.NewLine();
			}
			return null;
		}
		
		public override object Visit(EmptyStatement emptyStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}

		public override object Visit(BlockStatement blockStatement, object data)
		{
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			blockStatement.AcceptChildren(this, true);
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(ForStatement forStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.For);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			outputFormatter.DoIndent = false;
			outputFormatter.DoNewLine = false;
			outputFormatter.EmitSemicolon = false;
			if (forStatement.Initializers != null && forStatement.Initializers.Count > 0) {
				for (int i = 0; i < forStatement.Initializers.Count; ++i) {
					INode node = (INode)forStatement.Initializers[i];
					node.AcceptVisitor(this, false);
					if (i + 1 < forStatement.Initializers.Count) {
						outputFormatter.PrintToken(Tokens.Comma);
					}
				}
			}
			outputFormatter.EmitSemicolon = true;
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.EmitSemicolon = false;
			if (forStatement.Condition != null) {
				outputFormatter.Space();
				forStatement.Condition.AcceptVisitor(this, data);
			}
			outputFormatter.EmitSemicolon = true;
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.EmitSemicolon = false;
			if (forStatement.Iterator != null && forStatement.Iterator.Count > 0) {
				outputFormatter.Space();
				for (int i = 0; i < forStatement.Iterator.Count; ++i) {
					INode node = (INode)forStatement.Iterator[i];
					node.AcceptVisitor(this, false);
					if (i + 1 < forStatement.Iterator.Count) {
						outputFormatter.PrintToken(Tokens.Comma);
					}
				}
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.EmitSemicolon = true;
			outputFormatter.DoNewLine     = true;
			outputFormatter.DoIndent      = true;
			if (forStatement.EmbeddedStatement is BlockStatement) {
				Visit((BlockStatement)forStatement.EmbeddedStatement, false);
			} else {
				outputFormatter.NewLine();
				forStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			return null;
		}
		
		public override object Visit(ForeachStatement foreachStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Foreach);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			Visit(foreachStatement.TypeReference, data);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(foreachStatement.VariableName);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.In);
			outputFormatter.Space();
			foreachStatement.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			if (foreachStatement.EmbeddedStatement is BlockStatement) {
				Visit((BlockStatement)foreachStatement.EmbeddedStatement, false);
			} else {
				outputFormatter.NewLine();
				foreachStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			return null;
		}
		
		public override object Visit(WhileStatement whileStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.While);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			whileStatement.Condition.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			if (whileStatement.EmbeddedStatement is BlockStatement) {
				Visit((BlockStatement)whileStatement.EmbeddedStatement, false);
			} else {
				outputFormatter.NewLine();
				whileStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			return null;
		}
		
		public override object Visit(DoWhileStatement doWhileStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Do);
			if (doWhileStatement.EmbeddedStatement is BlockStatement) {
				Visit((BlockStatement)doWhileStatement.EmbeddedStatement, false);
			} else {
				outputFormatter.NewLine();
				doWhileStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.While);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			doWhileStatement.Condition.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(BreakStatement breakStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Break);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(ContinueStatement continueStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Continue);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(CheckedStatement checkedStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Checked);
			checkedStatement.Block.AcceptChildren(this, false);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(UncheckedStatement uncheckedStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Unchecked);
			uncheckedStatement.Block.AcceptVisitor(this, false);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(FixedStatement fixedStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Fixed);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			Visit(fixedStatement.TypeReference, data);
			outputFormatter.Space();
			AppendCommaSeparatedList(fixedStatement.PointerDeclarators);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			if (fixedStatement.EmbeddedStatement is BlockStatement) {
				Visit((BlockStatement)fixedStatement.EmbeddedStatement, false);
			} else {
				outputFormatter.NewLine();
				fixedStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(GotoCaseStatement gotoCaseStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Goto);
			outputFormatter.Space();
			if (gotoCaseStatement.IsDefaultCase) {
				outputFormatter.PrintToken(Tokens.Default);
			} else {
				outputFormatter.PrintToken(Tokens.Case);
				outputFormatter.Space();
				gotoCaseStatement.CaseExpression.AcceptVisitor(this, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(GotoStatement gotoStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Goto);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(gotoStatement.Label);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(IfElseStatement ifElseStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.If);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			ifElseStatement.Condition.AcceptVisitor(this,data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.NewLine();
			ifElseStatement.EmbeddedStatement.AcceptVisitor(this,data);
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Else);
			outputFormatter.NewLine();
			ifElseStatement.EmbeddedElseStatement.AcceptVisitor(this,data);
			return null;
		}
		
		public override object Visit(IfStatement ifStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.If);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			ifStatement.Condition.AcceptVisitor(this,data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			ifStatement.EmbeddedStatement.AcceptVisitor(this,data);
			return null;
		}
		
		public override object Visit(LabelStatement labelStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintIdentifier(labelStatement.Label);
			outputFormatter.PrintToken(Tokens.Colon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(LockStatement lockStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Lock);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			lockStatement.LockExpression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.Space();
			lockStatement.EmbeddedStatement.AcceptVisitor(this, data);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(ReturnStatement returnStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Return);
			if (returnStatement.ReturnExpression != null) {
				outputFormatter.Space();
				returnStatement.ReturnExpression.AcceptVisitor(this, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(SwitchStatement switchStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Switch);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			switchStatement.SwitchExpression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			foreach (SwitchSection section in switchStatement.SwitchSections) {
				for (int i = 0; i < section.SwitchLabels.Count; ++i) {
					Expression label = (Expression)section.SwitchLabels[i];
					if (label == null) {
						outputFormatter.Indent();
						outputFormatter.PrintToken(Tokens.Default);
						outputFormatter.PrintToken(Tokens.Colon);
						outputFormatter.NewLine();
						continue;
					}
					
					outputFormatter.Indent();
					outputFormatter.PrintToken(Tokens.Case);
					outputFormatter.Space();
					label.AcceptVisitor(this, data);
					outputFormatter.PrintToken(Tokens.Colon);
					outputFormatter.NewLine();
				}
				
				++outputFormatter.IndentationLevel;
				section.AcceptChildren(this, data);
				--outputFormatter.IndentationLevel;
			}
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(ThrowStatement throwStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Throw);
			if (throwStatement.ThrowExpression != null) {
				outputFormatter.Space();
				throwStatement.ThrowExpression.AcceptVisitor(this, data);
			}
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Try);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			tryCatchStatement.StatementBlock.AcceptChildren(this, data);
			--outputFormatter.IndentationLevel;
			
			if (tryCatchStatement.CatchClauses != null) {
//				int generated = 0;
				foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
					outputFormatter.Indent();
					outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
					outputFormatter.NewLine();
					outputFormatter.Indent();
					outputFormatter.PrintToken(Tokens.Catch);
					outputFormatter.Space();
					if (catchClause.Type == null) {
					} else {
						outputFormatter.PrintToken(Tokens.OpenParenthesis);
						outputFormatter.PrintIdentifier(catchClause.Type);
						if (catchClause.VariableName != null) {
							outputFormatter.Space();
							outputFormatter.PrintIdentifier(catchClause.VariableName);
						}
						outputFormatter.PrintToken(Tokens.CloseParenthesis);
					}
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
					outputFormatter.NewLine();
					++outputFormatter.IndentationLevel;
					catchClause.StatementBlock.AcceptChildren(this, data);
					--outputFormatter.IndentationLevel;
				}
			}
			
			if (tryCatchStatement.FinallyBlock != null) {
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
				outputFormatter.NewLine();
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.Finally);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				outputFormatter.NewLine();
				++outputFormatter.IndentationLevel;
				tryCatchStatement.FinallyBlock.AcceptChildren(this, data);
				--outputFormatter.IndentationLevel;
			}
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(UsingStatement usingStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Using);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			outputFormatter.DoIndent = false;
			outputFormatter.DoNewLine = false;
			outputFormatter.EmitSemicolon = false;
			
			usingStatement.UsingStmnt.AcceptVisitor(this,data);
			outputFormatter.DoIndent = true;
			outputFormatter.DoNewLine = true;
			outputFormatter.EmitSemicolon = true;
			
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			usingStatement.EmbeddedStatement.AcceptVisitor(this,data);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
//			Console.WriteLine(localVariableDeclaration);
			outputFormatter.Indent();
			VisitModifier(localVariableDeclaration.Modifier);
			Visit(localVariableDeclaration.Type, data);
			outputFormatter.Space();
			this.AppendCommaSeparatedList(localVariableDeclaration.Variables);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			outputFormatter.Indent();
			statementExpression.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.Semicolon);
			outputFormatter.NewLine();
			return null;
		}
		
		public override object Visit(UnsafeStatement unsafeStatement, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Unsafe);
			unsafeStatement.Block.AcceptVisitor(this, data);
			return null;
		}
		
		
#region Expressions
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.New);
			outputFormatter.Space();
			Visit(arrayCreateExpression.CreateType, null);
			for (int i = 0; i < arrayCreateExpression.Parameters.Count; ++i) {
				outputFormatter.PrintToken(Tokens.OpenSquareBracket);
				ArrayCreationParameter creationParameter = (ArrayCreationParameter)arrayCreateExpression.Parameters[i];
				if (creationParameter.IsExpressionList) {
					AppendCommaSeparatedList(creationParameter.Expressions);
				} else {
					for (int j = 0; j < creationParameter.Dimensions; ++j) {
						outputFormatter.PrintToken(Tokens.Comma);
					}
				}
				outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			}
			
			
			if (arrayCreateExpression.ArrayInitializer != null) {
				outputFormatter.Space();
				arrayCreateExpression.ArrayInitializer.AcceptVisitor(this, null);
			}
			return null;
		}
		
		public override object Visit(ArrayInitializerExpression arrayCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			this.AppendCommaSeparatedList(arrayCreateExpression.CreateExpressions);
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			return null;
		}
		
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			assignmentExpression.Left.AcceptVisitor(this, data);
			outputFormatter.Space();
			switch (assignmentExpression.Op) {
				case AssignmentOperatorType.Assign:
					outputFormatter.PrintToken(Tokens.Assign);
					break;
				case AssignmentOperatorType.Add:
					outputFormatter.PrintToken(Tokens.PlusAssign);
					break;
				case AssignmentOperatorType.Subtract:
					outputFormatter.PrintToken(Tokens.MinusAssign);
					break;
				case AssignmentOperatorType.Multiply:
					outputFormatter.PrintToken(Tokens.TimesAssign);
					break;
				case AssignmentOperatorType.Divide:
					outputFormatter.PrintToken(Tokens.DivAssign);
					break;
				case AssignmentOperatorType.ShiftLeft:
					outputFormatter.PrintToken(Tokens.ShiftLeftAssign);
					break;
				case AssignmentOperatorType.ShiftRight:
					outputFormatter.PrintToken(Tokens.ShiftRightAssign);
					break;
				case AssignmentOperatorType.ExclusiveOr:
					outputFormatter.PrintToken(Tokens.XorAssign);
					break;
				case AssignmentOperatorType.Modulus:
					outputFormatter.PrintToken(Tokens.ModAssign);
					break;
				case AssignmentOperatorType.BitwiseAnd:
					outputFormatter.PrintToken(Tokens.BitwiseAndAssign);
					break;
				case AssignmentOperatorType.BitwiseOr:
					outputFormatter.PrintToken(Tokens.BitwiseOrAssign);
					break;
			}
			outputFormatter.Space();
			assignmentExpression.Right.AcceptVisitor(this, data);
			return null;
		}
		
		public override object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Base);
			return null;
		}
		
		public override object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			binaryOperatorExpression.Left.AcceptVisitor(this, data);
			outputFormatter.Space();
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Add:
					outputFormatter.PrintToken(Tokens.Plus);
					break;
				
				case BinaryOperatorType.Subtract:
					outputFormatter.PrintToken(Tokens.Minus);
					break;
				
				case BinaryOperatorType.Multiply:
					outputFormatter.PrintToken(Tokens.Times);
					break;
				
				case BinaryOperatorType.Divide:
					outputFormatter.PrintToken(Tokens.Div);
					break;
				
				case BinaryOperatorType.Modulus:
					outputFormatter.PrintToken(Tokens.Mod);
					break;
				
				case BinaryOperatorType.ShiftLeft:
					outputFormatter.PrintToken(Tokens.ShiftLeft);
					break;
				
				case BinaryOperatorType.ShiftRight:
					outputFormatter.PrintToken(Tokens.ShiftRight);
					break;
				
				case BinaryOperatorType.BitwiseAnd:
					outputFormatter.PrintToken(Tokens.BitwiseAnd);
					break;
				case BinaryOperatorType.BitwiseOr:
					outputFormatter.PrintToken(Tokens.BitwiseOr);
					break;
				case BinaryOperatorType.ExclusiveOr:
					outputFormatter.PrintToken(Tokens.Xor);
					break;
				
				case BinaryOperatorType.LogicalAnd:
					outputFormatter.PrintToken(Tokens.LogicalAnd);
					break;
				case BinaryOperatorType.LogicalOr:
					outputFormatter.PrintToken(Tokens.LogicalOr);
					break;
				
				case BinaryOperatorType.AS:
					outputFormatter.PrintToken(Tokens.As);
					break;
				
				case BinaryOperatorType.IS:
					outputFormatter.PrintToken(Tokens.Is);
					break;
				case BinaryOperatorType.Equality:
					outputFormatter.PrintToken(Tokens.Equal);
					break;
				case BinaryOperatorType.GreaterThan:
					outputFormatter.PrintToken(Tokens.GreaterThan);
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					outputFormatter.PrintToken(Tokens.GreaterEqual);
					break;
				case BinaryOperatorType.InEquality:
					outputFormatter.PrintToken(Tokens.NotEqual);
					break;
				case BinaryOperatorType.LessThan:
					outputFormatter.PrintToken(Tokens.LessThan);
					break;
				case BinaryOperatorType.LessThanOrEqual:
					outputFormatter.PrintToken(Tokens.LessEqual);
					break;
			}
			outputFormatter.Space();
			binaryOperatorExpression.Right.AcceptVisitor(this, data);
			return null;
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			Visit(castExpression.CastTo, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.Space ();
			castExpression.Expression.AcceptVisitor(this, data);
			return null;
		}
		
		public override object Visit(CheckedExpression checkedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Checked);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			checkedExpression.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object Visit(ConditionalExpression conditionalExpression, object data)
		{
			conditionalExpression.TestCondition.AcceptVisitor(this, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Question);
			outputFormatter.Space();
			conditionalExpression.TrueExpression.AcceptVisitor(this, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Colon);
			outputFormatter.Space();
			conditionalExpression.FalseExpression.AcceptVisitor(this, data);
			return null;
		}
		
		public override object Visit(DirectionExpression directionExpression, object data)
		{
			switch (directionExpression.FieldDirection) {
				case FieldDirection.Out:
					outputFormatter.PrintToken(Tokens.Out);
					outputFormatter.Space();
					break;
				case FieldDirection.Ref:
					outputFormatter.PrintToken(Tokens.Ref);
					outputFormatter.Space();
					break;
			}
			directionExpression.Expression.AcceptVisitor(this, data);
			return null;
		}
		
		public override object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			fieldReferenceExpression.TargetObject.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.Dot);
			outputFormatter.PrintIdentifier(fieldReferenceExpression.FieldName);
			return null;
		}
		
		public override object Visit(IdentifierExpression identifierExpression, object data)
		{
			outputFormatter.PrintIdentifier(identifierExpression.Identifier);
			return null;
		}
		
		public override object Visit(IndexerExpression indexerExpression, object data)
		{
			indexerExpression.TargetObject.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			AppendCommaSeparatedList(indexerExpression.Indices);
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			return null;
		}
		
		public override object Visit(InvocationExpression invocationExpression, object data)
		{
			invocationExpression.TargetObject.AcceptVisitor(this, data);
			outputFormatter.Space ();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(invocationExpression.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.New);
			outputFormatter.Space();
			this.Visit(objectCreateExpression.CreateType, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(objectCreateExpression.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object Visit(ParenthesizedExpression parenthesizedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			parenthesizedExpression.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			pointerReferenceExpression.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.Pointer);
			outputFormatter.PrintIdentifier(pointerReferenceExpression.Identifier);
			return null;
		}
		
		public override object Visit(PrimitiveExpression primitiveExpression, object data)
		{
			outputFormatter.PrintIdentifier(primitiveExpression.StringValue);
			return null;
		}
		
		public override object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Sizeof);
			outputFormatter.Space ();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			Visit(sizeOfExpression.TypeReference, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Stackalloc);
			outputFormatter.Space();
			Visit(stackAllocExpression.Type, data);
			outputFormatter.PrintToken(Tokens.OpenSquareBracket);
			stackAllocExpression.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseSquareBracket);
			return null;
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.This);
			return null;
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Typeof);
			outputFormatter.Space ();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			Visit(typeOfExpression.TypeReference, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			Visit(typeReferenceExpression.TypeReference, data);
			return null;
		}
		
		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.BitNot:
					outputFormatter.PrintToken(Tokens.BitwiseComplement);
					break;
				case UnaryOperatorType.Decrement:
					outputFormatter.PrintToken(Tokens.Decrement);
					break;
				case UnaryOperatorType.Increment:
					outputFormatter.PrintToken(Tokens.Increment);
					break;
				case UnaryOperatorType.Minus:
					outputFormatter.PrintToken(Tokens.Minus);
					break;
				case UnaryOperatorType.Not:
					outputFormatter.PrintToken(Tokens.Not);
					break;
				case UnaryOperatorType.Plus:
					outputFormatter.PrintToken(Tokens.Plus);
					break;
				case UnaryOperatorType.PostDecrement:
					unaryOperatorExpression.Expression.AcceptVisitor(this, data);
					outputFormatter.PrintToken(Tokens.Decrement);
					return null;
				case UnaryOperatorType.PostIncrement:
					unaryOperatorExpression.Expression.AcceptVisitor(this, data);
					outputFormatter.PrintToken(Tokens.Increment);
					return null;
				case UnaryOperatorType.Star:
					outputFormatter.PrintToken(Tokens.Times);
					break;
				case UnaryOperatorType.BitWiseAnd:
					outputFormatter.PrintToken(Tokens.BitwiseAnd);
					break;
			}
			unaryOperatorExpression.Expression.AcceptVisitor(this, data);
			return null;
		}
		
		public override object Visit(UncheckedExpression uncheckedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Unchecked);
			outputFormatter.Space ();
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			uncheckedExpression.Expression.AcceptVisitor(this, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
#endregion
	}
}
