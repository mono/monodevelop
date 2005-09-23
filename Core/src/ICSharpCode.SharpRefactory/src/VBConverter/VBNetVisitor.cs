// VBNetVisitor.cs
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
using System.Reflection;
using System.CodeDom;
using System.Text;
using System.Collections;


using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.PrettyPrinter
{
	public class VBNetVisitor : AbstractASTVisitor
	{
		StringBuilder   sourceText  = new StringBuilder();
		int             indentLevel = 0;
		TypeDeclaration currentType = null;
		bool            generateAttributeUnderScore = false;
		Errors          errors      = new Errors();
		public StringBuilder SourceText {
			get {
				return sourceText;
			}
		}
		
#region ICSharpCode.SharpRefactory.Parser.IASTVisitor interface implementation
		public override object Visit(INode node, object data)
		{
			errors.Error(-1, -1, String.Format("Visited INode (should NEVER HAPPEN)"));
			Console.WriteLine("Visitor was: " + this.GetType());
			Console.WriteLine("Node was : " + node.GetType());
			return node.AcceptChildren(this, data);
		}
		
		public void AppendIndentation()
		{
			for (int i = 0; i < indentLevel; ++i) {
				sourceText.Append("\t");
			}
		}
		
		public void AppendNewLine()
		{
			sourceText.Append("\n");
		}
		
		void DebugOutput(object o)
		{
//			Console.WriteLine(o.ToString());
		}
		
		public override object Visit(CompilationUnit compilationUnit, object data)
		{
			DebugOutput(compilationUnit);
			new VBNetRefactory().Refactor(compilationUnit);
			compilationUnit.AcceptChildren(this, data);
			return null;
		}
		
		public override object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			DebugOutput(namespaceDeclaration);
			AppendIndentation();sourceText.Append("Namespace ");
			sourceText.Append(namespaceDeclaration.NameSpace);
			AppendNewLine();
			++indentLevel;
			namespaceDeclaration.AcceptChildren(this, data);
			--indentLevel;
			AppendIndentation();sourceText.Append("End Namespace");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(UsingDeclaration usingDeclaration, object data)
		{
			DebugOutput(usingDeclaration);
			AppendIndentation();sourceText.Append("Imports ");
			sourceText.Append(usingDeclaration.Namespace);
			AppendNewLine();
			return null;
		}
		
		public override object Visit(UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			DebugOutput(usingAliasDeclaration);
			AppendIndentation();sourceText.Append("Imports ");
			sourceText.Append(usingAliasDeclaration.Alias);
			sourceText.Append(" = ");
			sourceText.Append(usingAliasDeclaration.Namespace);
			AppendNewLine();
			return null;
		}
		
		public override object Visit(AttributeSection attributeSection, object data)
		{
			DebugOutput(attributeSection);
			AppendIndentation();sourceText.Append("<");
			if (attributeSection.AttributeTarget != null && attributeSection.AttributeTarget.Length > 0) {
				sourceText.Append(attributeSection.AttributeTarget);
				sourceText.Append(": ");
			}
			for (int j = 0; j < attributeSection.Attributes.Count; ++j) {
				ICSharpCode.SharpRefactory.Parser.AST.Attribute attr = (ICSharpCode.SharpRefactory.Parser.AST.Attribute)attributeSection.Attributes[j];
				
				sourceText.Append(attr.Name);
				sourceText.Append("(");
				for (int i = 0; i < attr.PositionalArguments.Count; ++i) {
					Expression expr = (Expression)attr.PositionalArguments[i];
					sourceText.Append(expr.AcceptVisitor(this, data).ToString());
					if (i + 1 < attr.PositionalArguments.Count) { 
						sourceText.Append(", ");
					}
				}
				
				for (int i = 0; i < attr.NamedArguments.Count; ++i) {
					NamedArgument named = (NamedArgument)attr.NamedArguments[i];
					sourceText.Append(named.Name);
					sourceText.Append("=");
					sourceText.Append(named.Expr.AcceptVisitor(this, data).ToString());
					if (i + 1 < attr.NamedArguments.Count) { 
						sourceText.Append(", ");
					}
				}
				sourceText.Append(")");
				if (j + 1 < attributeSection.Attributes.Count) {
					sourceText.Append(", ");
				}
			}
			sourceText.Append("> ");
			if (generateAttributeUnderScore) {
				sourceText.Append("_");
			}
			AppendNewLine();
			return null;
		}
		
		public override object Visit(TypeDeclaration typeDeclaration, object data)
		{
			DebugOutput(typeDeclaration);
			AppendNewLine();
			generateAttributeUnderScore = true;
			AppendAttributes(typeDeclaration.Attributes);
			string modifier =  GetModifier(typeDeclaration.Modifier);
			string type = String.Empty;
			
			switch (typeDeclaration.Type) {
				case Types.Class:
					type = "Class ";
					break;
				case Types.Enum:
					type = "Enum ";
					break;
				case Types.Interface:
					type = "Interface ";
					break;
				case Types.Struct:
					// this should be better in VBNetRefactory class because it is an AST transformation, but currently I'm too lazy
					if (TypeHasOnlyStaticMembers(typeDeclaration)) {
						goto case Types.Class;
					}
					type = "Structure ";
					break;
			}
			AppendIndentation();sourceText.Append(modifier);
			sourceText.Append(type);
			sourceText.Append(typeDeclaration.Name);
			AppendNewLine();
			
			if (typeDeclaration.BaseTypes != null) {
				foreach (string baseType in typeDeclaration.BaseTypes) {
					AppendIndentation();
					
					bool baseTypeIsInterface = baseType.StartsWith("I") && (baseType.Length <= 1 || Char.IsUpper(baseType[1]));
					
					if (!baseTypeIsInterface || typeDeclaration.Type == Types.Interface) {
						sourceText.Append("Inherits "); 
					} else {
						sourceText.Append("Implements "); 
					}
					sourceText.Append(baseType); 
					AppendNewLine();
				}
			}
			
			++indentLevel;
			TypeDeclaration oldType = currentType;
			currentType = typeDeclaration;
			typeDeclaration.AcceptChildren(this, data);
			currentType = oldType;
			--indentLevel;
			AppendIndentation();sourceText.Append("End ");
			sourceText.Append(type);
			AppendNewLine();
			generateAttributeUnderScore = false;
			return null;
		}
		
		public override object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
			DebugOutput(delegateDeclaration);
			AppendNewLine();
			AppendAttributes(delegateDeclaration.Attributes);
			AppendIndentation();sourceText.Append(GetModifier(delegateDeclaration.Modifier));
			sourceText.Append("Delegate ");
			bool isFunction = (delegateDeclaration.ReturnType.Type != "void");
			if (isFunction) {
				sourceText.Append("Function ");
			} else {
				sourceText.Append("Sub ");
			}
			
			sourceText.Append(delegateDeclaration.Name);
			sourceText.Append("(");
			AppendParameters(delegateDeclaration.Parameters);
			sourceText.Append(")");
			if (isFunction) {
				sourceText.Append(" As ");
				sourceText.Append(GetTypeString(delegateDeclaration.ReturnType));
			}
			AppendNewLine();
			return null;
		}
		
		public override object Visit(VariableDeclaration variableDeclaration, object data)
		{
			// called inside ENUMS
//			AppendAttributes(field.Attributes);
			AppendIndentation();sourceText.Append(variableDeclaration.Name);
			if (variableDeclaration.Initializer != null) {
				sourceText.Append(" = ");
				sourceText.Append(variableDeclaration.Initializer.AcceptVisitor(this, data));
			}
			AppendNewLine();
			return null;
		}
		
		public override object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			DebugOutput(fieldDeclaration);
			foreach (VariableDeclaration field in fieldDeclaration.Fields) {
				AppendAttributes(fieldDeclaration.Attributes);
				AppendIndentation();
				if (fieldDeclaration.Modifier == Modifier.None) {
					sourceText.Append(" Private ");
				} else {
					sourceText.Append(GetModifier(fieldDeclaration.Modifier));
				}
				sourceText.Append(field.Name);
				sourceText.Append(" As ");
				sourceText.Append(GetTypeString(fieldDeclaration.TypeReference));
				if (field.Initializer != null) {
					sourceText.Append(" = ");
					sourceText.Append(field.Initializer.AcceptVisitor(this, data).ToString());
				}
				AppendNewLine();
			}

			return null;
		}
		
		public override object Visit(MethodDeclaration methodDeclaration, object data)
		{
			DebugOutput(methodDeclaration);
			AppendNewLine();
			AppendAttributes(methodDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(GetModifier(methodDeclaration.Modifier));
			bool isFunction = methodDeclaration.TypeReference.Type != "void";
			string defStr   = isFunction ? "Function" : "Sub";
			sourceText.Append(defStr);
			sourceText.Append(" ");
			sourceText.Append(methodDeclaration.Name);
			sourceText.Append("(");
			AppendParameters(methodDeclaration.Parameters);
			sourceText.Append(")");
			if (isFunction) {
				sourceText.Append(" As ");
				sourceText.Append(GetTypeString(methodDeclaration.TypeReference));
			}
			AppendNewLine();
			
			if (currentType.Type != Types.Interface) {
				if (methodDeclaration.Body != null) {
					++indentLevel;
					methodDeclaration.Body.AcceptVisitor(this, data);
					--indentLevel;
				}
				
				AppendIndentation();sourceText.Append("End ");sourceText.Append(defStr);
				AppendNewLine();
			}
			return null;
		}
		
		public override object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			DebugOutput(propertyDeclaration);
			AppendNewLine();
			AppendAttributes(propertyDeclaration.Attributes);
			AppendIndentation();sourceText.Append(GetModifier(propertyDeclaration.Modifier));
			if (propertyDeclaration.IsReadOnly) {
				sourceText.Append("ReadOnly ");
			} else if (propertyDeclaration.IsWriteOnly) {
				sourceText.Append("WriteOnly ");
			}
			sourceText.Append("Property ");
			sourceText.Append(propertyDeclaration.Name);
			sourceText.Append("() As ");
			sourceText.Append(GetTypeString(propertyDeclaration.TypeReference));
			AppendNewLine();
			
			if (currentType.Type != Types.Interface) {
				if (propertyDeclaration.GetRegion != null) {
					++indentLevel;
					propertyDeclaration.GetRegion.AcceptVisitor(this, data);
					--indentLevel;
				}
				
				if (propertyDeclaration.SetRegion != null) {
					++indentLevel;
					propertyDeclaration.SetRegion.AcceptVisitor(this, data);
					--indentLevel;
				}
				
				AppendIndentation();sourceText.Append("End Property");
				AppendNewLine();
			}
			return null;
		}
		
		public override object Visit(PropertyGetRegion propertyGetRegion, object data)
		{
			DebugOutput(propertyGetRegion);
			AppendAttributes(propertyGetRegion.Attributes);
			AppendIndentation();sourceText.Append("Get");
			AppendNewLine();
			
			if (propertyGetRegion.Block != null) {
				++indentLevel;
				propertyGetRegion.Block.AcceptVisitor(this, data);
				--indentLevel;
			} 
			AppendIndentation();sourceText.Append("End Get");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(PropertySetRegion propertySetRegion, object data)
		{
			DebugOutput(propertySetRegion);
			AppendAttributes(propertySetRegion.Attributes);
			AppendIndentation();sourceText.Append("Set");
			AppendNewLine();
			
			if (propertySetRegion.Block != null) {
				++indentLevel;
				propertySetRegion.Block.AcceptVisitor(this, data);
				--indentLevel;
			}
			
			AppendIndentation();sourceText.Append("End Set");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(EventDeclaration eventDeclaration, object data)
		{
			DebugOutput(eventDeclaration);
			AppendNewLine();
			if (eventDeclaration.Name == null) { 
				foreach (VariableDeclaration field in eventDeclaration.VariableDeclarators) {
					AppendAttributes(eventDeclaration.Attributes);
					AppendIndentation();
					sourceText.Append(GetModifier(eventDeclaration.Modifier));
					sourceText.Append("Event ");
					sourceText.Append(field.Name);
					sourceText.Append(" As ");
					sourceText.Append(GetTypeString(eventDeclaration.TypeReference));
				}
			} else {
				AppendAttributes(eventDeclaration.Attributes);
				AppendIndentation();
				sourceText.Append(GetModifier(eventDeclaration.Modifier));
				sourceText.Append("Event ");
				sourceText.Append(eventDeclaration.Name);
				sourceText.Append(" As ");
				sourceText.Append(GetTypeString(eventDeclaration.TypeReference));
				if (eventDeclaration.HasAddRegion) {
					errors.Error(-1, -1, String.Format("Event add region can't be converted"));
				}
				if (eventDeclaration.HasRemoveRegion) {
					errors.Error(-1, -1, String.Format("Event remove region can't be converted"));
				}
			}
			
			AppendNewLine();
			return data;
		}
		
		public override object Visit(EventAddRegion eventAddRegion, object data)
		{
			// should never be called:
			throw new System.NotSupportedException();
		}
		
		public override object Visit(EventRemoveRegion eventRemoveRegion, object data)
		{
			// should never be called:
			throw new System.NotSupportedException();
		}
		
		public override object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			DebugOutput(constructorDeclaration);
			AppendNewLine();
			AppendIndentation();sourceText.Append(GetModifier(constructorDeclaration.Modifier));
			sourceText.Append("Sub New");
			sourceText.Append("(");
			AppendParameters(constructorDeclaration.Parameters);
			sourceText.Append(")");
			AppendNewLine();
			
			++indentLevel;
			constructorDeclaration.Body.AcceptChildren(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("End Sub");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(DestructorDeclaration destructorDeclaration, object data)
		{
			DebugOutput(destructorDeclaration);
			AppendNewLine();
			AppendIndentation();sourceText.Append("Overrides Protected Sub Finalize()");
			AppendNewLine();
			
			++indentLevel;
			destructorDeclaration.Body.AcceptChildren(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("End Sub");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(OperatorDeclaration operatorDeclaration, object data)
		{
			errors.Error(-1, -1, String.Format("Operator overloading cannot be performed"));
			return null;
		}
		
		public override object Visit(IndexerDeclaration indexerDeclaration, object data)
		{
			DebugOutput(indexerDeclaration);
			
			AppendNewLine();
			AppendAttributes(indexerDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append("Default ");
			sourceText.Append(GetModifier(indexerDeclaration.Modifier));
			sourceText.Append("Property Blubber(");
			AppendParameters(indexerDeclaration.Parameters);
			sourceText.Append(") As ");
			sourceText.Append(GetTypeString(indexerDeclaration.TypeReference));
			AppendNewLine();
			
			if (indexerDeclaration.GetRegion != null) {
				++indentLevel;
				indexerDeclaration.GetRegion.AcceptVisitor(this, data);
				--indentLevel;
			}
			
			if (indexerDeclaration.SetRegion != null) {
				++indentLevel;
				indexerDeclaration.SetRegion.AcceptVisitor(this, data);
				--indentLevel;
			}
			
			AppendIndentation();sourceText.Append("End Property");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(BlockStatement blockStatement, object data)
		{
			DebugOutput(blockStatement);
			blockStatement.AcceptChildren(this, data);
			return null;
		}
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			DebugOutput(statementExpression);
			AppendIndentation();sourceText.Append(statementExpression.Expression.AcceptVisitor(this, statementExpression).ToString());
			AppendNewLine();
			return null;
		}
		
		public override object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			DebugOutput(localVariableDeclaration);
			foreach (VariableDeclaration localVar in localVariableDeclaration.Variables) {
				AppendIndentation();sourceText.Append(GetModifier(localVariableDeclaration.Modifier));
				ArrayCreateExpression ace = localVar.Initializer as ArrayCreateExpression;
				if (ace != null && (ace.ArrayInitializer == null || ace.ArrayInitializer.CreateExpressions == null)) {
					string arrayParameters  = String.Empty;
					foreach (Expression expr in ace.Parameters) {
						arrayParameters += "(";
						arrayParameters += expr.AcceptVisitor(this, data);
						arrayParameters += " - 1)";
					}
					sourceText.Append("Dim ");
					sourceText.Append(localVar.Name);
					sourceText.Append(arrayParameters);
					sourceText.Append(" As ");
					sourceText.Append(ConvertTypeString(ace.CreateType.Type));
				} else {
					sourceText.Append("Dim ");
					sourceText.Append(localVar.Name);
					sourceText.Append(" As ");
					sourceText.Append(GetTypeString(localVariableDeclaration.Type));
					if (localVar.Initializer != null) {
						sourceText.Append(" = ");
						sourceText.Append(localVar.Initializer.AcceptVisitor(this, data).ToString());
					}
				}
				AppendNewLine();
			}
			return null;
		}
		
		public override object Visit(EmptyStatement emptyStatement, object data)
		{
			DebugOutput(emptyStatement);
			AppendNewLine();
			return null;
		}
		
		public override object Visit(ReturnStatement returnStatement, object data)
		{
			DebugOutput(returnStatement);
			AppendIndentation();sourceText.Append("Return");
			if (returnStatement.ReturnExpression != null) {
				sourceText.Append(" ");
				sourceText.Append(returnStatement.ReturnExpression.AcceptVisitor(this, data).ToString());
			}
			AppendNewLine();
			return null;
		}
		
		public override object Visit(IfStatement ifStatement, object data)
		{
			DebugOutput(ifStatement);
			AppendIndentation();
			InvocationExpression ie = GetEventHandlerRaise(ifStatement);
			
			if (ie == null) {
				sourceText.Append("If ");
				sourceText.Append(ifStatement.Condition.AcceptVisitor(this, data).ToString());
				sourceText.Append(" Then");
				AppendNewLine();
				
				++indentLevel;
				ifStatement.EmbeddedStatement.AcceptVisitor(this, data);
				--indentLevel;
				
				AppendIndentation();sourceText.Append("End If");
				AppendNewLine();
			} else {
				sourceText.Append("RaiseEvent ");
				sourceText.Append(ie.AcceptVisitor(this, data));
				AppendNewLine();
			}
			return null;
		}
		
		public override object Visit(IfElseStatement ifElseStatement, object data)
		{
			DebugOutput(ifElseStatement);
			AppendIndentation();sourceText.Append("If ");
			sourceText.Append(ifElseStatement.Condition.AcceptVisitor(this, data).ToString());
			sourceText.Append(" Then");
			AppendNewLine();
			
			++indentLevel;
			ifElseStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("Else");
			AppendNewLine();
			
			++indentLevel;
			ifElseStatement.EmbeddedElseStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("End If");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(WhileStatement whileStatement, object data)
		{
			DebugOutput(whileStatement);
			AppendIndentation();sourceText.Append("While ");
			sourceText.Append(whileStatement.Condition.AcceptVisitor(this, data).ToString());
			AppendNewLine();
			
			++indentLevel;
			whileStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("End While");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(DoWhileStatement doWhileStatement, object data)
		{
			DebugOutput(doWhileStatement);
			AppendIndentation();sourceText.Append("Do While");
			AppendNewLine();
			
			++indentLevel;
			doWhileStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("Loop");
			sourceText.Append(doWhileStatement.Condition.AcceptVisitor(this, data).ToString());
			AppendNewLine();
			return null;
		}
		
		public override object Visit(ForStatement forStatement, object data)
		{
			DebugOutput(forStatement);
			if (forStatement.Initializers != null) {
				foreach (object o in forStatement.Initializers) {
					if (o is Expression) {
						Expression expr = (Expression)o;
						AppendIndentation();
						sourceText.Append(expr.AcceptVisitor(this, data).ToString());
						AppendNewLine();
					}
					if (o is Statement) {
						((Statement)o).AcceptVisitor(this, data);
					}
				}
			}
			AppendIndentation();sourceText.Append("While ");
			if (forStatement.Condition == null) {
				sourceText.Append("True ");
			} else {
				sourceText.Append(forStatement.Condition.AcceptVisitor(this, data).ToString());
			}
			AppendNewLine();
			
			++indentLevel;
			forStatement.EmbeddedStatement.AcceptVisitor(this, data);
			if (forStatement.Iterator != null) {
				foreach (Statement stmt in forStatement.Iterator) {
					stmt.AcceptVisitor(this, data);
				}
			}
			--indentLevel;
			
			AppendIndentation();sourceText.Append("End While");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(LabelStatement labelStatement, object data)
		{
			DebugOutput(labelStatement);
			AppendIndentation();sourceText.Append(labelStatement.Label);
			sourceText.Append(":");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(GotoStatement gotoStatement, object data)
		{
			DebugOutput(gotoStatement);
			AppendIndentation();sourceText.Append("Goto ");
			sourceText.Append(gotoStatement.Label);
			AppendNewLine();
			return null;
		}
		
		public override object Visit(SwitchStatement switchStatement, object data)
		{
			DebugOutput(switchStatement);
			AppendIndentation();sourceText.Append("Select ");
			sourceText.Append(switchStatement.SwitchExpression.AcceptVisitor(this, data).ToString());
			AppendNewLine();
			foreach (SwitchSection section in switchStatement.SwitchSections) {
				AppendIndentation();sourceText.Append("Case ");
				
				for (int i = 0; i < section.SwitchLabels.Count; ++i) {
					Expression label = (Expression)section.SwitchLabels[i];
					if (label == null) {
						sourceText.Append("Else");
						continue;
					}
					sourceText.Append(label.AcceptVisitor(this, data));
					if (i + 1 < section.SwitchLabels.Count) {
						sourceText.Append(", ");
					}
				}
				AppendNewLine();
				
				++indentLevel;
				section.AcceptVisitor(this, data);
				--indentLevel;
			}
			AppendIndentation();sourceText.Append("End Select ");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(BreakStatement breakStatement, object data)
		{
			DebugOutput(breakStatement);
			AppendIndentation();sourceText.Append("' break");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(ContinueStatement continueStatement, object data)
		{
			DebugOutput(continueStatement);
			AppendIndentation();sourceText.Append("' continue");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(GotoCaseStatement gotoCaseStatement, object data)
		{
			DebugOutput(gotoCaseStatement);
			AppendIndentation();sourceText.Append("' goto case ");
			if (gotoCaseStatement.CaseExpression == null) {
				sourceText.Append("default");
			} else {
				sourceText.Append(gotoCaseStatement.CaseExpression.AcceptVisitor(this, data));
			}
			AppendNewLine();
			return null;
		}
		
		public override object Visit(ForeachStatement foreachStatement, object data)
		{
			DebugOutput(foreachStatement);
			AppendIndentation();sourceText.Append("For Each ");
			sourceText.Append(foreachStatement.VariableName);
			sourceText.Append(" As ");
			sourceText.Append(this.GetTypeString(foreachStatement.TypeReference));
			sourceText.Append(" In ");
			sourceText.Append(foreachStatement.Expression.AcceptVisitor(this, data));
			AppendNewLine();
			
			++indentLevel;
			foreachStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("Next");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(LockStatement lockStatement, object data)
		{
			DebugOutput(lockStatement);
			AppendIndentation();sourceText.Append("SyncLock ");
			sourceText.Append(lockStatement.LockExpression.AcceptVisitor(this, data));
			AppendNewLine();
			
			++indentLevel;
			lockStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();sourceText.Append("End SyncLock");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(UsingStatement usingStatement, object data)
		{
			DebugOutput(usingStatement);
			// TODO : anything like this ?
			AppendIndentation();sourceText.Append("' Using ");AppendNewLine();
			usingStatement.UsingStmnt.AcceptVisitor(this, data);
			AppendIndentation();sourceText.Append("' Inside ");AppendNewLine();
			usingStatement.EmbeddedStatement.AcceptVisitor(this, data);
			AppendIndentation();sourceText.Append("' End Using");AppendNewLine();
			return null;
		}
		
		public override object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			DebugOutput(tryCatchStatement);
			AppendIndentation();sourceText.Append("Try");
			AppendNewLine();
			
			++indentLevel;
			tryCatchStatement.StatementBlock.AcceptVisitor(this, data);
			--indentLevel;
			
			if (tryCatchStatement.CatchClauses != null) {
				int generated = 0;
				foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
					AppendIndentation();sourceText.Append("Catch ");
					if (catchClause.VariableName == null) {
						sourceText.Append("generatedExceptionVariable" + generated.ToString());
						++generated;
					} else {
						sourceText.Append(catchClause.VariableName);
					}
					sourceText.Append(" As ");
					sourceText.Append(catchClause.Type);
					AppendNewLine();
					++indentLevel;
					catchClause.StatementBlock.AcceptVisitor(this, data);
					--indentLevel;
				}
			}
			
			if (tryCatchStatement.FinallyBlock != null) {
				AppendIndentation();sourceText.Append("Finally");
				AppendNewLine();
				
				++indentLevel;
				tryCatchStatement.FinallyBlock.AcceptVisitor(this, data);
				--indentLevel;
			}
			AppendIndentation();sourceText.Append("End Try");
			AppendNewLine();
			return null;
		}
		
		public override object Visit(ThrowStatement throwStatement, object data)
		{
			DebugOutput(throwStatement);
			AppendIndentation();sourceText.Append("Throw ");
			sourceText.Append(throwStatement.ThrowExpression.AcceptVisitor(this, data).ToString());
			AppendNewLine();
			return null;
		}
		
		public override object Visit(FixedStatement fixedStatement, object data)
		{
			DebugOutput(fixedStatement);
			errors.Error(-1, -1, String.Format("fixed statement not suported by VB.NET"));
			return null;
		}
		
		public override object Visit(CheckedStatement checkedStatement, object data)
		{
			DebugOutput(checkedStatement);
			errors.Error(-1, -1, String.Format("checked statement not suported by VB.NET"));
			return null;
		}
		
		public override object Visit(UncheckedStatement uncheckedStatement, object data)
		{
			DebugOutput(uncheckedStatement);
			errors.Error(-1, -1, String.Format("unchecked statement not suported by VB.NET"));
			return null;
		}
		
		public override object Visit(PrimitiveExpression primitiveExpression, object data)
		{
			DebugOutput(primitiveExpression);
			if (primitiveExpression.Value == null) {
				return "Nothing";
			}
			if (primitiveExpression.Value is bool) {
				if ((bool)primitiveExpression.Value) {
					return "True";
				}
				return "False";
			}
			
			if (primitiveExpression.Value is string) {
				return String.Concat('"',
				                     primitiveExpression.Value,
				                     '"');
			}
			
			if (primitiveExpression.Value is char) {
				return String.Concat("'",
				                     primitiveExpression.Value,
				                     "'");
			}
			
			return primitiveExpression.Value;
		}
		
		public override object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			DebugOutput(binaryOperatorExpression);
			string op = null;
			string left = binaryOperatorExpression.Left.AcceptVisitor(this, data).ToString();
			string right = binaryOperatorExpression.Right.AcceptVisitor(this, data).ToString();
			
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Add:
					op = " + ";
					break;
				
				case BinaryOperatorType.Subtract:
					op = " - ";
					break;
				
				case BinaryOperatorType.Multiply:
					op = " * ";
					break;
				
				case BinaryOperatorType.Divide:
					op = " / ";
					break;
				
				case BinaryOperatorType.Modulus:
					op = " Mod ";
					break;
				
				case BinaryOperatorType.ShiftLeft:
					op = " << ";
					break;
				
				case BinaryOperatorType.ShiftRight:
					op = " >> ";
					break;
				
				case BinaryOperatorType.BitwiseAnd:
					op = " And ";
					break;
				case BinaryOperatorType.BitwiseOr:
					op = " Or ";
					break;
				case BinaryOperatorType.ExclusiveOr:
					op = " Xor ";
					break;
				
				case BinaryOperatorType.LogicalAnd:
					op = " AndAlso ";
					break;
				case BinaryOperatorType.LogicalOr:
					op = " OrElse ";
					break;
				
				case BinaryOperatorType.AS:
					return String.Concat("CType(ConversionHelpers.AsWorkaround(",
					                     left,
					                     ", GetType(",
					                     right,
					                     ")), ",
					                     right,
					                     ")");
				case BinaryOperatorType.IS:
					return String.Concat("TypeOf ",
					                     left,
					                     " Is ",
					                     right);
				
				case BinaryOperatorType.Equality:
					op = " = ";
					if (right == "Nothing") {
						op = " Is ";
					}
					break;
				case BinaryOperatorType.GreaterThan:
					op = " > ";
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					op = " >= ";
					break;
				case BinaryOperatorType.InEquality:
					if (right == "Nothing") {
						return String.Concat("Not (",
						                     left,
						                     " Is ",
						                     right,
						                     ")");
					} else {
						return String.Concat("Not (",
						                     left,
						                     " = ",
						                     right,
						                     ")");
					}
				case BinaryOperatorType.LessThan:
					op = " < ";
					break;
				case BinaryOperatorType.LessThanOrEqual:
					op = " <= ";
					break;
			}
			
			return String.Concat(left,
			                     op,
			                     right);
		}
		
		public override object Visit(ParenthesizedExpression parenthesizedExpression, object data)
		{
			DebugOutput(parenthesizedExpression);
			string innerExpr = parenthesizedExpression.Expression.AcceptVisitor(this, data).ToString();
			
			// parenthesized cast expressions evaluate to a single 'method call' and don't need
			// to be parenthesized anymore like in C#. Parenthesized cast expresions may lead to
			// a vb.net compiler error (method calls)
			if (parenthesizedExpression.Expression is CastExpression) {
				return innerExpr;
			}
			return String.Concat("(", innerExpr, ")");
		}
		
		public override object Visit(InvocationExpression invocationExpression, object data)
		{
			DebugOutput(invocationExpression);
			string backString;
			
			if (invocationExpression.TargetObject is ObjectCreateExpression) {
				backString = String.Concat("(",
				                     invocationExpression.TargetObject.AcceptVisitor(this, data),
				                     ")",
				                     GetParameters(invocationExpression.Parameters));
			} else {
				backString = String.Concat(invocationExpression.TargetObject.AcceptVisitor(this, data),
				                     GetParameters(invocationExpression.Parameters));
			}
			//todo: invocationExpression, indexer ... etc.
			Expression expr = invocationExpression.TargetObject;
			while (expr is FieldReferenceExpression) {
				expr = ((FieldReferenceExpression)expr).TargetObject;
			}
			
			if (data is StatementExpression && expr is ObjectCreateExpression) {
				return String.Concat("call ", backString);
			}
			return backString;
		}
		
		public override object Visit(IdentifierExpression identifierExpression, object data)
		{
			DebugOutput(identifierExpression);
			return identifierExpression.Identifier;
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			DebugOutput(typeReferenceExpression);
			return GetTypeString(typeReferenceExpression.TypeReference);
		}
		
		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			DebugOutput(unaryOperatorExpression);
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.BitNot:
					return String.Concat("Not ", unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Decrement:
					return String.Concat("ConversionHelpers.Decrement (", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.Increment:
					return String.Concat("ConversionHelpers.Increment (", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.Minus:
					return String.Concat("-", unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Not:
					return String.Concat("Not ", unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Plus:
					return unaryOperatorExpression.Expression.AcceptVisitor(this, data);
				case UnaryOperatorType.PostDecrement:
					return String.Concat("ConversionHelpers.PostDecrement (", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.PostIncrement:
					return String.Concat("ConversionHelpers.PostIncrement (", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.Star:
				case UnaryOperatorType.BitWiseAnd:
					break;
			}
			throw new System.NotSupportedException();
		}
		
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			DebugOutput(assignmentExpression);
			string op   = null;
			string left = assignmentExpression.Left.AcceptVisitor(this, data).ToString();
			string right = assignmentExpression.Right.AcceptVisitor(this, data).ToString();
			switch (assignmentExpression.Op) {
				case AssignmentOperatorType.Assign:
					op = " = ";
					break;
				case AssignmentOperatorType.Add:
					op = " += ";
					if (IsEventHandlerCreation(assignmentExpression.Right)) {
						return String.Format("AddHandler {0}, AddressOf {1}", 
						                     left, 
						                     ((Expression)((ObjectCreateExpression)assignmentExpression.Right).Parameters[0]).AcceptVisitor(this, data).ToString());
					}
					break;
				case AssignmentOperatorType.Subtract:
					op = " -= ";
					if (IsEventHandlerCreation(assignmentExpression.Right)) {
						return String.Format("RemoveHandler {0}, AddressOf {1}", 
						                     left, 
						                     ((Expression)((ObjectCreateExpression)assignmentExpression.Right).Parameters[0]).AcceptVisitor(this, data).ToString());
					}
					break;
				case AssignmentOperatorType.Multiply:
					op = " *= ";
					break;
				case AssignmentOperatorType.Divide:
					op = " /= ";
					break;
				case AssignmentOperatorType.ShiftLeft:
					op = " <<= ";
					break;
				case AssignmentOperatorType.ShiftRight:
					op = " >>= ";
					break;
				
				case AssignmentOperatorType.ExclusiveOr:
					return String.Format("{0} = {0} Xor ({1})", left, right);
				case AssignmentOperatorType.Modulus:
					return String.Format("{0} = {0} Mod ({1})", left, right);
				case AssignmentOperatorType.BitwiseAnd:
					return String.Format("{0} = {0} And ({1})", left, right);
				case AssignmentOperatorType.BitwiseOr:
					return String.Format("{0} = {0} Or ({1})", left, right);
			}
			return String.Concat(left,
			                     op,
			                     right);
		}
		
		public override object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			DebugOutput(sizeOfExpression);
			errors.Error(-1, -1, String.Format("sizeof expression not suported by VB.NET"));
			return null;
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			DebugOutput(typeOfExpression);
			return String.Concat("GetType(",
			                     GetTypeString(typeOfExpression.TypeReference),
			                     ")");
		}
		
		public override object Visit(CheckedExpression checkedExpression, object data)
		{
			return String.Concat("'Checked expression (can't convert):",
			                     checkedExpression.Expression.AcceptVisitor(this, data));
		}
		
		public override object Visit(UncheckedExpression uncheckedExpression, object data)
		{
			return String.Concat("'Unhecked expression (can't convert):",
			                     uncheckedExpression.Expression.AcceptVisitor(this, data));
		}
		
		public override object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			errors.Error(-1, -1, String.Format("pointer reference (->) not suported by VB.NET"));
			return String.Empty;
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			DebugOutput(castExpression);
			return String.Format("CType({0}, {1})",
			                     castExpression.Expression.AcceptVisitor(this, data).ToString(),
			                     GetTypeString(castExpression.CastTo));
		}
		
		public override object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			errors.Error(-1, -1, String.Format("stack alloc expression not suported by VB.NET"));
			return String.Empty;
		}
		
		public override object Visit(IndexerExpression indexerExpression, object data)
		{
			DebugOutput(indexerExpression);
			return String.Concat(indexerExpression.TargetObject.AcceptVisitor(this, data),
			                     GetParameters(indexerExpression.Indices));
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			DebugOutput(thisReferenceExpression);
			return "Me";
		}
		
		public override object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			DebugOutput(baseReferenceExpression);
			return "MyBase";
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			DebugOutput(objectCreateExpression);
			if (IsEventHandlerCreation(objectCreateExpression)) {
				Expression expr = (Expression)objectCreateExpression.Parameters[0];
				string handler;
				if (expr is FieldReferenceExpression) {
					handler = ((FieldReferenceExpression)expr).FieldName;
				} else {
					handler = expr.AcceptVisitor(this, data).ToString();
				}
				return String.Format("AddressOf {0}", handler);
			}
			return String.Format("New {0} {1}",
			                     GetTypeString(objectCreateExpression.CreateType),
			                     GetParameters(objectCreateExpression.Parameters)
			                     );
		}
		
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			DebugOutput(arrayCreateExpression);
			string arrayInitializer = String.Empty;
			string arrayParameters  = String.Empty;
			
			if (arrayCreateExpression.ArrayInitializer != null && arrayCreateExpression.ArrayInitializer.CreateExpressions != null) {
				arrayInitializer = String.Concat(" {",
				                                 GetExpressionList(arrayCreateExpression.ArrayInitializer.CreateExpressions),
				                                 "}");
			}
			
			if (arrayCreateExpression.Parameters != null && arrayCreateExpression.Parameters.Count > 0) {
				foreach (ArrayCreationParameter param in arrayCreateExpression.Parameters) {
					// TODO: multidimensional arrays ?
					foreach (Expression expr in param.Expressions) {
						arrayParameters += "(";
						arrayParameters += expr.AcceptVisitor(this, data);
						arrayParameters += ")";
					}
				}
			} else {
				arrayParameters = "()";
			}
			
			return String.Format("New {0}{2} {1}",
			                     GetTypeString(arrayCreateExpression.CreateType),
			                     arrayInitializer,
			                     arrayParameters
			                     );
		}
		
		public override object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			// should never be called:
			throw new System.NotImplementedException();
		}
		
		public override object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			DebugOutput(fieldReferenceExpression);
			if (fieldReferenceExpression.TargetObject is ObjectCreateExpression) {
				return String.Concat("(",
				                     fieldReferenceExpression.TargetObject.AcceptVisitor(this, data),
				                     ").",
				                     fieldReferenceExpression.FieldName);
			}
			return String.Concat(fieldReferenceExpression.TargetObject.AcceptVisitor(this, data),
			                     ".",
			                     fieldReferenceExpression.FieldName);
		}
		
		public override object Visit(DirectionExpression directionExpression, object data)
		{
			DebugOutput(directionExpression);
			string fieldDirection = String.Empty;
			// TODO: is this correct that there is nothing in a VB.NET method call for out & ref ?
//			switch (directionExpression.FieldDirection) {
//				case FieldDirection.Out:
//					break;
//				case FieldDirection.Ref:
//					break;
//			}
			return String.Concat(fieldDirection, directionExpression.Expression.AcceptVisitor(this, data));
		}
		
		public override object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			return String.Concat(" {",
			                     GetExpressionList(arrayInitializerExpression.CreateExpressions),
			                     "}");
		}
		
		public override object Visit(ConditionalExpression conditionalExpression, object data)
		{
			errors.Error(-1, -1, String.Format("TODO: Conditionals :)"));
			return String.Empty;
		}
#endregion
		string ConvertTypeString(string typeString)
		{
			switch (typeString) {
				case "bool":
					return "Boolean";
				case "string":
					return "String";
				case "char":
					return "Char";
				case "double":
					return "Double";
				case "float":
					return "Single";
				case "decimal":
					return "Decimal";
				case "System.DateTime":
					return "Date";
				case "long":
					return "Long";
				case "int":
					return "Integer";
				case "short":
					return "Short";
				case "byte":
					return "Byte";
				case "void":
					return "Void";
				case "object":
					return "Object";
				case "ulong":
					return "System.UInt64";
				case "uint":
					return "System.UInt32";
				case "ushort":
					return "System.UInt16";
			}
			return typeString;
		}
		string GetTypeString(TypeReference typeRef)
		{
			if (typeRef == null) {
				errors.Error(-1, -1, String.Format("Got empty type string (internal error, check generated source code for empty types"));
				return String.Empty;
			}
			
			string typeStr = ConvertTypeString(typeRef.Type);
		
			StringBuilder arrays = new StringBuilder();
			if (typeRef.RankSpecifier != null) {
				for (int i = 0; i < typeRef.RankSpecifier.Length; ++i) {
					arrays.Append("(");
					for (int j = 1; j < typeRef.RankSpecifier[i]; ++j) {
						arrays.Append(",");
					}
					arrays.Append(")");
				}
			}
			
			if (typeRef.PointerNestingLevel > 0) {
				errors.Error(-1, -1, String.Format("Pointer types are not suported by VB.NET"));
			}
			
			return typeStr + arrays.ToString();
		}
		
		string GetModifier(Modifier modifier)
		{
			StringBuilder builder = new StringBuilder();
			
			if ((modifier & Modifier.Public) == Modifier.Public) {
				builder.Append("Public ");
			} else if ((modifier & Modifier.Private) == Modifier.Private) {
				builder.Append("Private ");
			} else if ((modifier & (Modifier.Protected | Modifier.Internal)) == (Modifier.Protected | Modifier.Internal)) {
				builder.Append("Protected Friend ");
			} else if ((modifier & Modifier.Internal) == Modifier.Internal) {
				builder.Append("Friend ");
			} else if ((modifier & Modifier.Protected) == Modifier.Protected) {
				builder.Append("Protected ");
			}
			
			if ((modifier & Modifier.Static) == Modifier.Static) {
				builder.Append("Shared ");
			}
			if ((modifier & Modifier.Virtual) == Modifier.Virtual) {
				builder.Append("Overridable ");
			}
			if ((modifier & Modifier.Abstract) == Modifier.Abstract) {
				builder.Append("MustOverride ");
			}
			if ((modifier & Modifier.Override) == Modifier.Override) {
				builder.Append("Overloads Overrides ");
			}
			if ((modifier & Modifier.New) == Modifier.New) {
				builder.Append("Shadows ");
			}
			
			if ((modifier & Modifier.Sealed) == Modifier.Sealed) {
				builder.Append("NotInheritable ");
			}
			
			if ((modifier & Modifier.Const) == Modifier.Const) {
				builder.Append("Const ");
			}
			if ((modifier & Modifier.Readonly) == Modifier.Readonly) {
				builder.Append("ReadOnly ");
			}
			
			// TODO : Extern 
			if ((modifier & Modifier.Extern) == Modifier.Extern) {
				errors.Error(-1, -1, String.Format("'Extern' modifier not convertable"));
			}
			// TODO : Volatile 
			if ((modifier & Modifier.Volatile) == Modifier.Volatile) {
				errors.Error(-1, -1, String.Format("'Volatile' modifier not convertable"));
			}
			// TODO : Unsafe 
			if ((modifier & Modifier.Unsafe) == Modifier.Unsafe) {
				errors.Error(-1, -1, String.Format("'Unsafe' modifier not convertable"));
			}
			return builder.ToString();
		}
		
		string GetParameters(ArrayList list)
		{
			if (list == null || list.Count == 0) {
				return String.Empty;
			}
			return String.Concat("(",
			                     GetExpressionList(list),
			                     ")");
		}
		
		string GetExpressionList(ArrayList list)
		{
			StringBuilder sb = new StringBuilder();
			if (list != null) {
				for (int i = 0; i < list.Count; ++i) {
					Expression exp = (Expression)list[i];
					sb.Append(exp.AcceptVisitor(this, null));
					if (i + 1 < list.Count) {
						sb.Append(", ");
					}
				}
			}
			return sb.ToString();
		}
		public void AppendParameters(ArrayList parameters)
		{
			if (parameters == null) {
				return;
			}
			for (int i = 0; i < parameters.Count; ++i) {
				ParameterDeclarationExpression pde = (ParameterDeclarationExpression)parameters[i];
				AppendAttributes(pde.Attributes);
				if ((pde.ParamModifiers & ParamModifiers.Ref) == ParamModifiers.Ref) {
					sourceText.Append("ByRef ");
				} else if ((pde.ParamModifiers & ParamModifiers.Out) == ParamModifiers.Out) {
					// TODO : is ByRef correct for out parameters ?
					sourceText.Append("ByRef ");
				} else if ((pde.ParamModifiers & ParamModifiers.Params) == ParamModifiers.Params) {
					sourceText.Append("ParamArray ");
				} else {
					sourceText.Append("ByVal ");
				}
				sourceText.Append(pde.ParameterName);
				sourceText.Append(" As ");
				sourceText.Append(GetTypeString(pde.TypeReference));
				if (i + 1 < parameters.Count) {
					sourceText.Append(", ");
				}
			}
		}
		public void AppendAttributes(ArrayList attr)
		{
			if (attr != null) {
				foreach (AttributeSection section in attr) {
					section.AcceptVisitor(this, null);
				}
			}
		}
		
		InvocationExpression GetEventHandlerRaise(IfStatement ifStatement)
		{
			BinaryOperatorExpression op = ifStatement.Condition as BinaryOperatorExpression;
			if (op != null && op.Op == BinaryOperatorType.InEquality) {
				if (op.Left is IdentifierExpression && op.Right is PrimitiveExpression && ((PrimitiveExpression)op.Right).Value == null) {
					string identifier = ((IdentifierExpression)op.Left).Identifier;
					StatementExpression se = null;
					if (ifStatement.EmbeddedStatement is StatementExpression) {
						se = (StatementExpression)ifStatement.EmbeddedStatement;
					} else if (ifStatement.EmbeddedStatement.Children.Count == 1) {
						se = ifStatement.EmbeddedStatement.Children[0] as StatementExpression;
					}
					if (se != null) {
						InvocationExpression ie = se.Expression as InvocationExpression;
						if (ie != null) {
							Expression ex = ie.TargetObject;
							string methodName = null;
							if (ex is IdentifierExpression) {
								methodName = ((IdentifierExpression)ex).Identifier;
							} else if (ex is FieldReferenceExpression) {
								FieldReferenceExpression fre = (FieldReferenceExpression)ex;
								if (fre.TargetObject is ThisReferenceExpression) {
									methodName = fre.FieldName;
								}
							}
							if (methodName != null && methodName == identifier) {
								foreach (object o in this.currentType.Children) {
									EventDeclaration ed = o as EventDeclaration;
									if (ed != null) {
										if (ed.Name == methodName) {
											return ie;
										}
										foreach (VariableDeclaration field in ed.VariableDeclarators) {
											if (field.Name == methodName) {
												return ie;
											}
										}
									
									}
								}
							}
						}
					}
				}
			}
			return null;
		}
		
		bool IsEventHandlerCreation(Expression expr)
		{
			if (expr is ObjectCreateExpression) {
				ObjectCreateExpression oce = (ObjectCreateExpression) expr;
				if (oce.Parameters.Count == 1) {
					expr = (Expression)oce.Parameters[0];
					string methodName = null;
					if (expr is IdentifierExpression) {
						methodName = ((IdentifierExpression)expr).Identifier;
					} else if (expr is FieldReferenceExpression) {
						methodName = ((FieldReferenceExpression)expr).FieldName;
					}
					if (methodName != null) {
						foreach (object o in this.currentType.Children) {
							if (o is MethodDeclaration && ((MethodDeclaration)o).Name == methodName) {
								return true;
							}
						}
					}
					
				}
			}
			
			return false;
		}
		bool TypeHasOnlyStaticMembers(TypeDeclaration typeDeclaration)
		{
			foreach (object o in typeDeclaration.Children) {
				if (o is MethodDeclaration) {
					if ((((MethodDeclaration)o).Modifier & Modifier.Static) != Modifier.Static) {
						return false;
					}
				} else if (o is PropertyDeclaration) {
					if ((((PropertyDeclaration)o).Modifier & Modifier.Static) != Modifier.Static) {
						return false;
					}
				} else if (o is FieldDeclaration) {
					if ((((FieldDeclaration)o).Modifier & Modifier.Static) != Modifier.Static) {
						return false;
					}
				}else if (o is EventDeclaration) {
					if ((((EventDeclaration)o).Modifier & Modifier.Static) != Modifier.Static) {
						return false;
					}
				}
			}
			return true;
		}
	}
}

