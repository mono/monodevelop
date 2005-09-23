// CSharpVisitor.cs
// Copyright (C) 2004 Markus Palme (markuspalme@gmx.de)
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

using ICSharpCode.SharpRefactory.Parser.VB;
using ICSharpCode.SharpRefactory.Parser.AST.VB;

namespace ICSharpCode.SharpRefactory.PrettyPrinter.VB
{
	public class CSharpVisitor : IASTVisitor
	{
		readonly string newLineSep  = Environment.NewLine;
		StringBuilder   sourceText  = new StringBuilder();
		int             indentLevel = 0;
		Errors          errors      = new Errors();
		TypeDeclaration currentType = null;		
		Stack   withExpressionStack = new Stack();
		
		public StringBuilder SourceText {
			get {
				return sourceText;
			}
		}
		
		public void AppendIndentation()
		{
			for (int i = 0; i < indentLevel; ++i) {
				sourceText.Append("\t");
			}
		}
		
		public void AppendNewLine()
		{
			sourceText.Append(newLineSep);
		}
		
		public void AppendStatementEnd()
		{
			sourceText.Append(";");
			AppendNewLine();
		}
		
		void DebugOutput(object o)
		{
//			Console.WriteLine(o.ToString());
		}
		
		#region ICSharpCode.SharpRefactory.Parser.VB.IASTVisitor interface implementation
		public object Visit(INode node, object data)
		{
			AppendIndentation();
			sourceText.Append("// warning visited unknown node :");
			sourceText.Append(node);
			AppendNewLine();
			return String.Empty;
		}
		
		public object Visit(CompilationUnit compilationUnit, object data)
		{
			DebugOutput(compilationUnit);
			compilationUnit.AcceptChildren(this, data);
			return null;
		}
		
#region GlobalScope
		public object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			DebugOutput(namespaceDeclaration);
			AppendIndentation();
			sourceText.Append("namespace ");
			sourceText.Append(namespaceDeclaration.NameSpace);
			AppendNewLine();
			sourceText.Append("{");
			AppendNewLine();
			++indentLevel;
			namespaceDeclaration.AcceptChildren(this, data);
			--indentLevel;
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			return null;
		}
		
		public object Visit(ImportsStatement importsStatement, object data)
		{
			foreach (INode node in importsStatement.ImportClauses) {
				node.AcceptVisitor(this, data);
			}
			return null;
		}
		
		
		public object Visit(ImportsDeclaration importsDeclaration, object data)
		{
			DebugOutput(importsDeclaration);
			AppendIndentation();
			sourceText.Append("using ");
			sourceText.Append(importsDeclaration.Namespace);
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		public object Visit(ImportsAliasDeclaration importsAliasDeclaration, object data)
		{
			DebugOutput(importsAliasDeclaration);
			AppendIndentation();
			sourceText.Append("using ");
			sourceText.Append(importsAliasDeclaration.Alias);
			sourceText.Append(" = ");
			sourceText.Append(importsAliasDeclaration.Namespace);
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		
		public object Visit(TypeDeclaration typeDeclaration, object data)
		{
			DebugOutput(typeDeclaration);
			AppendAttributes(typeDeclaration.Attributes);
			string modifier =  GetModifier(typeDeclaration.Modifier);
			string type = String.Empty;
			
			switch (typeDeclaration.Type) {
				case Types.Class:
					type = "class ";
					break;
				case Types.Enum:
					type = "enum ";
					break;
				case Types.Interface:
					type = "interface ";
					break;
				case Types.Module:
				case Types.Structure:
					type = "struct ";
					break;
			}
			AppendIndentation();
			sourceText.Append(modifier);
			sourceText.Append(type);
			sourceText.Append(typeDeclaration.Name);
			
			bool hasBaseType = typeDeclaration.BaseType != null;
			if (hasBaseType) {
				sourceText.Append(" : "); 
				sourceText.Append(ConvertTypeString(typeDeclaration.BaseType)); 
			}
			
			if (typeDeclaration.BaseInterfaces != null && typeDeclaration.BaseInterfaces.Count > 0) {
				if (!hasBaseType) {
					sourceText.Append(" : "); 
				} else {
					sourceText.Append(", "); 
				}
				for (int i = 0; i < typeDeclaration.BaseInterfaces.Count; ++i) {
					if (typeDeclaration.BaseInterfaces[i] is TypeReference) {
						sourceText.Append((typeDeclaration.BaseInterfaces[i] as TypeReference).Type);
					} else {
						sourceText.Append(typeDeclaration.BaseInterfaces[i].ToString());
					}
					if (i + 1 < typeDeclaration.BaseInterfaces.Count) {
						sourceText.Append(", "); 
					}
				}
			}
			AppendNewLine();
			AppendIndentation();
			sourceText.Append("{");
			AppendNewLine();
			++indentLevel;
			TypeDeclaration oldType = currentType;
			currentType = typeDeclaration;
			typeDeclaration.AcceptChildren(this, data);
			currentType = oldType;
			--indentLevel;
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			return null;
		}
		
		public object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
			DebugOutput(delegateDeclaration);
			AppendAttributes(delegateDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(GetModifier(delegateDeclaration.Modifier));
			sourceText.Append("delegate ");
			sourceText.Append(GetTypeString(delegateDeclaration.ReturnType));
			sourceText.Append(" ");
			sourceText.Append(delegateDeclaration.Name);
			sourceText.Append("(");
			AppendParameters(delegateDeclaration.Parameters);
			sourceText.Append(");");
			AppendNewLine();
			return null;
		}
		
		public object Visit(EventDeclaration eventDeclaration, object data)
		{
			DebugOutput(eventDeclaration);
			AppendAttributes(eventDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(GetModifier(eventDeclaration.Modifier));
			sourceText.Append("event ");
			
			if (eventDeclaration.TypeReference == null) {
				sourceText.Append(eventDeclaration.Name);
				sourceText.Append("EventHandler");
			} else {
				sourceText.Append(GetTypeString(eventDeclaration.TypeReference));
			}
			sourceText.Append(" ");
			sourceText.Append(eventDeclaration.Name);
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
#endregion

#region TypeLevel
		public object Visit(VariableDeclaration variableDeclaration, object data)
		{
			// called inside ENUMS
//			AppendAttributes(field.Attributes);
			AppendIndentation();
			sourceText.Append(variableDeclaration.Name);
			if (variableDeclaration.Initializer != null) {
				sourceText.Append(" = ");
				sourceText.Append(variableDeclaration.Initializer.AcceptVisitor(this, data));
			}
			AppendNewLine();
			return null;
		}
		
		public object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			DebugOutput(fieldDeclaration);
			
			foreach (VariableDeclaration field in fieldDeclaration.Fields) {
				AppendAttributes(fieldDeclaration.Attributes);
				AppendIndentation();
				if (currentType.Type == Types.Enum) {
					if (fieldDeclaration.Fields.IndexOf(field) > 0) {
						sourceText.Append(", ");
					}
					sourceText.Append(field.Name);
					if (field.Initializer != null) {
						sourceText.Append(" = ");
						sourceText.Append(field.Initializer.AcceptVisitor(this, data).ToString());
					}
				} else {
					if (fieldDeclaration.Modifier == Modifier.None) {
						sourceText.Append(" private ");
					} else {
						sourceText.Append(GetModifier(fieldDeclaration.Modifier));
					}
					if (field.Type == null)
						sourceText.Append("object");
					else	
						sourceText.Append(GetTypeString(field.Type));
					sourceText.Append(" ");
					sourceText.Append(field.Name);
					if (field.Initializer != null) {
						sourceText.Append(" = ");
						sourceText.Append(field.Initializer.AcceptVisitor(this, data).ToString());
					} else {
						if (field.Type != null && field.Type.Dimension != null) {
							sourceText.Append(" = new ");
							sourceText.Append(ConvertTypeString(field.Type.Type));
							sourceText.Append("[");
							sourceText.Append(GetExpressionList(field.Type.Dimension));
							sourceText.Append("]");
						}
					}
					sourceText.Append(";");
					AppendNewLine();
				}
			}
			
			// if that's not the last enum member, add a comma
			if (currentType.Type == Types.Enum) {
				int pos = currentType.Children.IndexOf(fieldDeclaration);
				if (pos >= 0) {
					for (int i = pos+1; i < currentType.Children.Count; i++) {
						if (currentType.Children[i] is FieldDeclaration) {
							sourceText.Append(",");
							break;
						}
					}
				}
				AppendNewLine();
			}
			return null;
		}
		
		public object Visit(MethodDeclaration methodDeclaration, object data)
		{
			DebugOutput(methodDeclaration);
			exitConstructStack.Push(new DictionaryEntry(typeof(MethodDeclaration), null));
			
			AppendNewLine();
			AppendAttributes(methodDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(GetModifier(methodDeclaration.Modifier));
			sourceText.Append(GetTypeString(methodDeclaration.TypeReference));
			sourceText.Append(" ");
			sourceText.Append(methodDeclaration.Name);
			sourceText.Append("(");
			AppendParameters(methodDeclaration.Parameters);
			sourceText.Append(")");
			
			if (currentType.Type != Types.Interface &&
				(methodDeclaration.Modifier & Modifier.MustOverride) != Modifier.MustOverride)
			{
				AppendNewLine();
				AppendIndentation();
				sourceText.Append("{");
				AppendNewLine();
				if (methodDeclaration.Body != null) {
					++indentLevel;
					methodDeclaration.Body.AcceptVisitor(this, data);
					GenerateExitConstructLabel();
					--indentLevel;
				}
				AppendIndentation();
				sourceText.Append("}");
			} else {
				sourceText.Append(";");
			}
			AppendNewLine();
			return null;
		}
		
		public object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			DebugOutput(constructorDeclaration);
			exitConstructStack.Push(new DictionaryEntry(typeof(MethodDeclaration), null));
			AppendNewLine();
			AppendAttributes(constructorDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(GetModifier(constructorDeclaration.Modifier));
			sourceText.Append(this.currentType.Name);
			sourceText.Append("(");
			AppendParameters(constructorDeclaration.Parameters);
			sourceText.Append(")");
			
			AppendNewLine();
			AppendIndentation();
			sourceText.Append("{");
			AppendNewLine();
			if (constructorDeclaration.Body != null) {
				++indentLevel;
				constructorDeclaration.Body.AcceptVisitor(this, data);
				GenerateExitConstructLabel();
				--indentLevel;
			}
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			return null;
		}
		
		public object Visit(DeclareDeclaration declareDeclaration, object data)
		{
			DebugOutput(declareDeclaration);
			AppendAttributes(declareDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(String.Format("[System.Runtime.InteropServices.DllImport({0}", declareDeclaration.Library));
			if (declareDeclaration.Alias != null) {
				sourceText.Append(String.Format(", EntryPoint={0}", declareDeclaration.Alias));
			}
			
			switch (declareDeclaration.Charset) {
				case CharsetModifier.ANSI:
					sourceText.Append(", CharSet=System.Runtime.InteropServices.CharSet.Ansi");
					break;
				case CharsetModifier.Unicode:
					sourceText.Append(", CharSet=System.Runtime.InteropServices.CharSet.Unicode");
					break;
				case CharsetModifier.Auto:
					sourceText.Append(", CharSet=System.Runtime.InteropServices.CharSet.Auto");
					break;
			}
			
			sourceText.Append(")]");
			AppendNewLine();
			AppendIndentation();
			sourceText.Append(GetModifier(declareDeclaration.Modifier));
			sourceText.Append("static extern ");
			sourceText.Append(GetTypeString(declareDeclaration.ReturnType));
			sourceText.Append(" ");
			sourceText.Append(declareDeclaration.Name);
			sourceText.Append("(");
			AppendParameters(declareDeclaration.Parameters);
			sourceText.Append(");");
			AppendNewLine();
			return null;
		}
		
		public object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			DebugOutput(propertyDeclaration);
			AppendNewLine();
			AppendAttributes(propertyDeclaration.Attributes);
			AppendIndentation();
			sourceText.Append(GetModifier(propertyDeclaration.Modifier & ~Modifier.ReadOnly));
			
			sourceText.Append(GetTypeString(propertyDeclaration.TypeReference));
			sourceText.Append(" ");
			sourceText.Append(propertyDeclaration.Name);
			sourceText.Append(" {");
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
				
			}
			// if abstract, add default get/set
			if ((propertyDeclaration.Modifier & Modifier.MustOverride) == Modifier.MustOverride &&
			    propertyDeclaration.GetRegion == null &&
			    propertyDeclaration.SetRegion == null) {
				AppendIndentation();
				sourceText.Append("get;");
				AppendNewLine();
				AppendIndentation();
				sourceText.Append("set;");
				AppendNewLine();
			}			
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			return null;
		}
		
		public object Visit(PropertyGetRegion propertyGetRegion, object data)
		{
			DebugOutput(propertyGetRegion);
			exitConstructStack.Push(new DictionaryEntry(typeof(PropertyDeclaration), null));
			AppendAttributes(propertyGetRegion.Attributes);
			AppendIndentation();
			sourceText.Append("get {");
			AppendNewLine();
			if (propertyGetRegion.Block != null) {
				++indentLevel;
				propertyGetRegion.Block.AcceptVisitor(this, data);
				--indentLevel;
			} 
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(PropertySetRegion propertySetRegion, object data)
		{
			DebugOutput(propertySetRegion);
			exitConstructStack.Push(new DictionaryEntry(typeof(PropertyDeclaration), null));
			AppendAttributes(propertySetRegion.Attributes);
			AppendIndentation();
			sourceText.Append("set {");
			AppendNewLine();
			if (propertySetRegion.Block != null) {
				++indentLevel;
				propertySetRegion.Block.AcceptVisitor(this, data);
				--indentLevel;
			}
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(TypeReference typeReference, object data)
		{
			return ConvertTypeString(typeReference.Type);
		}
#endregion

#region Statements
		public object Visit(Statement statement, object data)
		{
			AppendIndentation();
			sourceText.Append("// warning visited unknown statment :");
			sourceText.Append(statement);
			AppendNewLine();
			return String.Empty;
		}
		
		public object Visit(BlockStatement blockStatement, object data)
		{
			DebugOutput(blockStatement);
			blockStatement.AcceptChildren(this, data);
			return null;
		}
		
		public object Visit(StatementExpression statementExpression, object data)
		{
			DebugOutput(statementExpression);
			AppendIndentation();
			if (statementExpression.Expression == null) {
				sourceText.Append("// warning got empty statement expression :");
				sourceText.Append(statementExpression);
			} else {
				sourceText.Append(statementExpression.Expression.AcceptVisitor(this, data).ToString());
				sourceText.Append(";");
			}
			AppendNewLine();
			return null;
		}
		
		public object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			DebugOutput(localVariableDeclaration);
			for (int i = 0; i < localVariableDeclaration.Variables.Count; ++i) {
				VariableDeclaration localVar = (VariableDeclaration)localVariableDeclaration.Variables[i];
				AppendIndentation();
				sourceText.Append(GetModifier(localVariableDeclaration.Modifier));
				ArrayCreateExpression ace = localVar.Initializer as ArrayCreateExpression;
				if (ace != null && (ace.ArrayInitializer == null || ace.ArrayInitializer.CreateExpressions == null)) {
					sourceText.Append(ConvertTypeString(ace.CreateType.Type));
					sourceText.Append(" ");
					sourceText.Append(localVar.Name);
					sourceText.Append("[");
					sourceText.Append(GetExpressionList(ace.Parameters));
					sourceText.Append("]");
					
				} else {
					if (localVar.Type == null) {
						bool foundType = false;
						for (int j = i + 1; j < localVariableDeclaration.Variables.Count; ++j) {
							VariableDeclaration nextLocalVar = (VariableDeclaration)localVariableDeclaration.Variables[j];
							if (nextLocalVar.Type != null) {
								sourceText.Append(GetTypeString(nextLocalVar.Type));
								foundType = true;
								break;
							}
						}
						if (!foundType) {
							sourceText.Append("object");
						}
					} else {
						sourceText.Append(GetTypeString(localVar.Type));
					}
					sourceText.Append(" ");
					sourceText.Append(localVar.Name);
					if (localVar.Initializer != null) {
						sourceText.Append(" = ");
						sourceText.Append(localVar.Initializer.AcceptVisitor(this, data).ToString());
					} else {
						if (localVar.Type != null && localVar.Type.Dimension != null) {
							sourceText.Append(" = new ");
							sourceText.Append(ConvertTypeString(localVar.Type.Type));
							sourceText.Append("[");
							sourceText.Append(GetExpressionList(localVar.Type.Dimension));
							sourceText.Append("]");
						}
					}
				}
				sourceText.Append(";");
				AppendNewLine();
			}
			return null;
		}
		
		public object Visit(SimpleIfStatement ifStatement, object data)
		{
			AppendIndentation();
			sourceText.Append("if (");
			sourceText.Append(ifStatement.Condition.AcceptVisitor(this, data).ToString());
			sourceText.Append(") {");
			AppendNewLine();
			++indentLevel;
			foreach(Statement statement in ifStatement.Statements) {
				statement.AcceptVisitor(this, data);
			}
			--indentLevel;
			AppendIndentation();
			sourceText.Append("}");
			
			if(ifStatement.ElseStatements != null && ifStatement.ElseStatements.Count > 0) {
				sourceText.Append(" else {");
				AppendNewLine();
				++indentLevel;
				foreach(Statement statement in ifStatement.ElseStatements) {
					statement.AcceptVisitor(this, data);
				}
				--indentLevel;
				AppendIndentation();
				sourceText.Append("}");
			}
			
			AppendNewLine();
			return null;
		}
		
		public object Visit(IfStatement ifStatement, object data)
		{
			DebugOutput(ifStatement);
			AppendIndentation();
			sourceText.Append("if (");
			sourceText.Append(ifStatement.Condition.AcceptVisitor(this, data).ToString());
			sourceText.Append(") {");
			AppendNewLine();
			++indentLevel;
			ifStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();
			sourceText.Append("}");
			
			if (ifStatement.ElseIfStatements != null) {
				foreach (ElseIfSection elseIfSection in ifStatement.ElseIfStatements) {
					sourceText.Append(" else if (");
					sourceText.Append(elseIfSection.Condition.AcceptVisitor(this, data).ToString());
					sourceText.Append(") {");
					AppendNewLine();
					++indentLevel;
					elseIfSection.EmbeddedStatement.AcceptVisitor(this, data);
					--indentLevel;
					AppendIndentation();
					sourceText.Append("}");
				}
			}
			
			if (ifStatement.EmbeddedElseStatement != null) {
				sourceText.Append(" else {");
				AppendNewLine();
				++indentLevel;
				ifStatement.EmbeddedElseStatement.AcceptVisitor(this, data);
				--indentLevel;
				AppendIndentation();
				sourceText.Append("}");
			}
			
			AppendNewLine();
			return null;
		}
		
		public object Visit(LabelStatement labelStatement, object data)
		{
			DebugOutput(labelStatement);
			AppendIndentation();
			sourceText.Append(labelStatement.Label);
			sourceText.Append(":");
			AppendNewLine();
			if (labelStatement.EmbeddedStatement != null) {
				labelStatement.EmbeddedStatement.AcceptVisitor(this, data);
			}
			return null;
		}
		
		public object Visit(GoToStatement goToStatement, object data)
		{
			DebugOutput(goToStatement);
			AppendIndentation();
			sourceText.Append("goto");
			sourceText.Append(goToStatement.LabelName);
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		public object Visit(SelectStatement selectStatement, object data)
		{
			DebugOutput(selectStatement);
			exitConstructStack.Push(new DictionaryEntry(typeof(SelectStatement), null));
			string selectExpression = selectStatement.SelectExpression.AcceptVisitor(this, data).ToString();
			AppendIndentation();
			for (int j = 0; j < selectStatement.SelectSections.Count; ++j) {
				SelectSection selectSection = (SelectSection)selectStatement.SelectSections[j];
				if (selectSection.CaseClauses.Count == 1 && ((CaseClause)selectSection.CaseClauses[0]).IsDefaultCase) {
					sourceText.Append("{");
				} else {
					sourceText.Append("if (");
					for (int i = 0; i < selectSection.CaseClauses.Count; ++i) {
						CaseClause caseClause = (CaseClause)selectSection.CaseClauses[i];
						if (caseClause.BoundaryExpression != null) {
							sourceText.Append(caseClause.ComparisonExpression.AcceptVisitor(this, data));
							sourceText.Append(" <= ");
							sourceText.Append(selectExpression);
							sourceText.Append(" && ");
							sourceText.Append(selectExpression);
							sourceText.Append(" <= ");
							sourceText.Append(caseClause.BoundaryExpression.AcceptVisitor(this, data));
						} else {
							if (caseClause.ComparisonExpression != null) {
								sourceText.Append(selectExpression);
								sourceText.Append(" == ");
								sourceText.Append(caseClause.ComparisonExpression.AcceptVisitor(this, data));
							} else {
								// dummy default should never evaluate (only for default case)
								sourceText.Append(" true ");
							}
						}
						if (i + 1 < selectSection.CaseClauses.Count) {
							sourceText.Append(" || ");
						}
					}
					sourceText.Append(") {");
				}
				AppendNewLine();
				++indentLevel;
				selectSection.EmbeddedStatement.AcceptChildren(this, data);
				--indentLevel;
				AppendIndentation();
				sourceText.Append("}");
				if (j + 1 < selectStatement.SelectSections.Count) {
					sourceText.Append(" else ");
				} 
			}
			AppendNewLine();
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(StopStatement stopStatement, object data)
		{
			DebugOutput(stopStatement);
			AppendIndentation();
			sourceText.Append("Debugger.Break();");
			AppendNewLine();
			return null;
		}
		
		public object Visit(ResumeStatement resumeStatement, object data)
		{
			DebugOutput(resumeStatement);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(resumeStatement);
			AppendNewLine();
			return null;
		}
		
		public object Visit(EraseStatement eraseStatement, object data)
		{
			DebugOutput(eraseStatement);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(eraseStatement);
			AppendNewLine();
			return null;
		}
		
		public object Visit(ErrorStatement errorStatement, object data)
		{
			DebugOutput(errorStatement);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(errorStatement);
			AppendNewLine();
			return null;
		}
		
		public object Visit(OnErrorStatement onErrorStatement, object data)
		{
			DebugOutput(onErrorStatement);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(onErrorStatement);
			AppendNewLine();
			return null;
		}
		
		public object Visit(ReDimStatement reDimStatement, object data)
		{
			DebugOutput(reDimStatement);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(reDimStatement);
			AppendNewLine();
			return null;
		}
		
		public object Visit(AddHandlerStatement addHandlerStatement, object data)
		{
			DebugOutput(addHandlerStatement);
			AppendIndentation();
			sourceText.Append(addHandlerStatement.EventExpression.AcceptVisitor(this, data));
			sourceText.Append(" += ");
			sourceText.Append(addHandlerStatement.HandlerExpression.AcceptVisitor(this, data));
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		public object Visit(RemoveHandlerStatement removeHandlerStatement, object data)
		{
			DebugOutput(removeHandlerStatement);
			AppendIndentation();
			sourceText.Append(removeHandlerStatement.EventExpression.AcceptVisitor(this, data));
			sourceText.Append(" -= ");
			sourceText.Append(removeHandlerStatement.HandlerExpression.AcceptVisitor(this, data));
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		public object Visit(DoLoopStatement doLoopStatement, object data)
		{
			DebugOutput(doLoopStatement);
			exitConstructStack.Push(new DictionaryEntry(typeof(DoLoopStatement), null));
			if (doLoopStatement.ConditionPosition == ConditionPosition.Start) {
				AppendIndentation();
				sourceText.Append("while (");
				if (doLoopStatement.ConditionType == ConditionType.Until) {
					sourceText.Append("!(");
				}
				sourceText.Append(doLoopStatement.Condition.AcceptVisitor(this, data).ToString());
				if (doLoopStatement.ConditionType == ConditionType.Until) {
					sourceText.Append(")");
				}
				sourceText.Append(") {");
				
				AppendNewLine();
				
				++indentLevel;
				doLoopStatement.EmbeddedStatement.AcceptVisitor(this, data);
				--indentLevel;
				
				AppendIndentation();
				sourceText.Append("}");
				AppendNewLine();
			} else {
				AppendIndentation();
				sourceText.Append("do {");
				AppendNewLine();
				
				++indentLevel;
				doLoopStatement.EmbeddedStatement.AcceptVisitor(this, data);
				--indentLevel;
				
				AppendIndentation();
				sourceText.Append("} while (");
				if (doLoopStatement.Condition == null) {
					sourceText.Append("true");
				} else {
					if (doLoopStatement.ConditionType == ConditionType.Until) {
						sourceText.Append("!(");
					}
					sourceText.Append(doLoopStatement.Condition.AcceptVisitor(this, data).ToString());
					if (doLoopStatement.ConditionType == ConditionType.Until) {
						sourceText.Append(")");
					}
				}
				sourceText.Append(");");
				AppendNewLine();
			}
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(EndStatement endStatement, object data)
		{
			DebugOutput(endStatement);
			AppendIndentation();
			sourceText.Append("System.Environment.Exit(0);");
			AppendNewLine();
			return null;
		}
		
		Stack exitConstructStack = new Stack();
		int   exitLabelCount     = 0;
		public string AddExitOnConstructStack(Type exitType)
		{
			string labelName = String.Concat("exit" + exitType.Name, exitLabelCount++);
			if (exitConstructStack.Count > 0) {
				object[] exitArray = exitConstructStack.ToArray();
				for (int i = exitArray.Length - 1; i >= 0; --i) {
					if ((Type)((DictionaryEntry)exitArray[i]).Key == exitType) {
						exitArray[i] = new DictionaryEntry(((DictionaryEntry)exitArray[i]).Key, labelName);
					}
				}
				Array.Reverse(exitArray);
				exitConstructStack = new Stack(exitArray);
			}
			return String.Concat(labelName);
		}
		
		public void GenerateExitConstructLabel()
		{
			if (exitConstructStack.Count > 0) {
				DictionaryEntry entry = (DictionaryEntry)exitConstructStack.Pop();
				if (entry.Value != null) {
					AppendIndentation();
					sourceText.Append(entry.Value.ToString());
					sourceText.Append(": ;");
					AppendNewLine();
				}
			}
		}
		
		public object Visit(ExitStatement exitStatement, object data)
		{
			DebugOutput(exitStatement);
			Type   exitType  = null;
			switch (exitStatement.ExitType) {
				case ExitType.Sub:
					sourceText.Append("return;");
					AppendNewLine();
					return null;
				case ExitType.Function:
					sourceText.Append("return null;");
					AppendNewLine();
					return null;
				case ExitType.Property:
					exitType = typeof(PropertyDeclaration);
					break;
				case ExitType.Do:
					exitType = typeof(DoLoopStatement);
					break;
				case ExitType.For:
					exitType = typeof(ForStatement);
					break;
				case ExitType.While:
					exitType = typeof(WhileStatement);
					break;
				case ExitType.Select:
					exitType = typeof(SelectStatement);
					break;
				case ExitType.Try:
					exitType = typeof(TryCatchStatement);
					break;
			}
			if (exitType != null) {
				AppendIndentation();
				sourceText.Append("goto ");
				sourceText.Append(AddExitOnConstructStack(exitType));
				sourceText.Append(";");
				AppendNewLine();
			} else {
				AppendIndentation();
				sourceText.Append("ERROR IN GENERATION: EXIT TO ");
				sourceText.Append(exitStatement.ExitType);
				sourceText.Append(" FAILED!!!");
				AppendNewLine();
			}
			return null;
		}
		
		public object Visit(ForeachStatement foreachStatement, object data)
		{
			DebugOutput(foreachStatement);
			exitConstructStack.Push(new DictionaryEntry(typeof(ForStatement), null));
			
			AppendIndentation();
			sourceText.Append("foreach (");
			if (foreachStatement.LoopControlVariable.Type != null) {
				sourceText.Append(this.GetTypeString(foreachStatement.LoopControlVariable.Type));
				sourceText.Append(" ");
			}
			sourceText.Append(foreachStatement.LoopControlVariable.Name);
			sourceText.Append(" in ");
			sourceText.Append(foreachStatement.Expression.AcceptVisitor(this, data));
			sourceText.Append(") {");
			AppendNewLine();
			
			++indentLevel;
			foreachStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(ForStatement forStatement, object data)
		{
			DebugOutput(forStatement);
			exitConstructStack.Push(new DictionaryEntry(typeof(ForStatement), null));
			bool   stepIsNegative = false;
			string step           = null;
			if (forStatement.Step != null) {
				step = forStatement.Step.AcceptVisitor(this, data).ToString();
				stepIsNegative = step.StartsWith("-");
			}
			
			AppendIndentation();
			sourceText.Append("for (");
			
			
			if (forStatement.LoopControlVariable.Type != null) {
				sourceText.Append(this.GetTypeString(forStatement.LoopControlVariable.Type));
				sourceText.Append(" ");
			}
			sourceText.Append(forStatement.LoopControlVariable.Name);
			sourceText.Append(" = ");
			
			sourceText.Append(forStatement.Start.AcceptVisitor(this, data));
			sourceText.Append("; ");
			sourceText.Append(forStatement.LoopControlVariable.Name);
			sourceText.Append(stepIsNegative ? " >= " : " <= ");
			sourceText.Append(forStatement.End.AcceptVisitor(this, data));
			sourceText.Append("; ");
			if (forStatement.Step == null) {
				sourceText.Append(forStatement.LoopControlVariable.Name);
				sourceText.Append("++");
			} else {
				sourceText.Append(forStatement.LoopControlVariable.Name);
				if (stepIsNegative) {
					if (step == "-1") {
						sourceText.Append("--");
					} else {
						sourceText.Append(" -= ");
						sourceText.Append(step.Substring(1));
					}
				} else {
					sourceText.Append(" += ");
					sourceText.Append(step);
				}
			}
			sourceText.Append(") {");
			AppendNewLine();
			
			++indentLevel;
			forStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			GenerateExitConstructLabel();
			
			return null;
		}
		
		public object Visit(LockStatement lockStatement, object data)
		{
			DebugOutput(lockStatement);
			AppendIndentation();
			sourceText.Append("lock (");
			sourceText.Append(lockStatement.LockExpression.AcceptVisitor(this, data));
			sourceText.Append(") {");
			AppendNewLine();
			
			++indentLevel;
			lockStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			return null;
		}
		
		public object Visit(RaiseEventStatement raiseEventStatement, object data)
		{
			DebugOutput(raiseEventStatement);
			AppendIndentation();
			sourceText.Append("if (");
			sourceText.Append(raiseEventStatement.EventName);
			sourceText.Append(" != null) {");
			AppendNewLine();
			
			++indentLevel;
			AppendIndentation();
			sourceText.Append(raiseEventStatement.EventName);
			sourceText.Append(GetParameters(raiseEventStatement.Parameters));
			sourceText.Append(";");
			AppendNewLine();
			--indentLevel;
			
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			return null;
		}
		
		
		public object Visit(ReturnStatement returnStatement, object data)
		{
			DebugOutput(returnStatement);
			AppendIndentation();
			sourceText.Append("return");
			if (returnStatement.ReturnExpression != null) {
				sourceText.Append(" ");
				sourceText.Append(returnStatement.ReturnExpression.AcceptVisitor(this,data));
			}
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		public object Visit(ThrowStatement throwStatement, object data)
		{
			DebugOutput(throwStatement);
			AppendIndentation();
			sourceText.Append("throw");
			if (throwStatement.ThrowExpression != null) {
				sourceText.Append(" ");
				sourceText.Append(throwStatement.ThrowExpression.AcceptVisitor(this, data).ToString());
			}
			sourceText.Append(";");
			AppendNewLine();
			return null;
		}
		
		public object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			DebugOutput(tryCatchStatement);
			exitConstructStack.Push(new DictionaryEntry(typeof(TryCatchStatement), null));
			AppendIndentation();
			sourceText.Append("try {");
			AppendNewLine();
			
			++indentLevel;
			tryCatchStatement.StatementBlock.AcceptVisitor(this, data);
			--indentLevel;
			AppendIndentation();
			sourceText.Append("}");
			if (tryCatchStatement.CatchClauses != null) {
				foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
					sourceText.Append(" catch ");
					if (catchClause.Type != null) {
						sourceText.Append("(");
						sourceText.Append(GetTypeString(catchClause.Type));
						if (catchClause.VariableName != null) {
							sourceText.Append(" ");
							sourceText.Append(catchClause.VariableName);
						}
						sourceText.Append(") ");
					}
					sourceText.Append("{");
					AppendNewLine();
					++indentLevel;
					if (catchClause.Condition != null) {
						AppendIndentation();
						sourceText.Append("//TODO: review the original conditional catch clause");
						AppendNewLine();
						AppendIndentation();
						sourceText.Append("if (");
						sourceText.Append(catchClause.Condition.AcceptVisitor(this, data));
						sourceText.Append(") {");
						AppendNewLine();
						++indentLevel;
						catchClause.StatementBlock.AcceptVisitor(this, data);
						--indentLevel;
						AppendIndentation();
						sourceText.Append("}");
						AppendNewLine();
					} else {
						catchClause.StatementBlock.AcceptVisitor(this, data);
					}
					--indentLevel;
					AppendIndentation();
					sourceText.Append("}");
				}
			}
			
			if (tryCatchStatement.FinallyBlock != null) {
				sourceText.Append(" finally {");
				AppendNewLine();
				
				++indentLevel;
				tryCatchStatement.FinallyBlock.AcceptVisitor(this, data);
				--indentLevel;
				AppendIndentation();
				sourceText.Append("}");
			}
			AppendNewLine();
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(WhileStatement whileStatement, object data)
		{
			DebugOutput(whileStatement);
			exitConstructStack.Push(new DictionaryEntry(typeof(WhileStatement), null));
			AppendIndentation();
			sourceText.Append("while (");
			sourceText.Append(whileStatement.Condition.AcceptVisitor(this, data).ToString());
			sourceText.Append(") {");
			AppendNewLine();
			
			++indentLevel;
			whileStatement.EmbeddedStatement.AcceptVisitor(this, data);
			--indentLevel;
			
			AppendIndentation();
			sourceText.Append("}");
			AppendNewLine();
			GenerateExitConstructLabel();
			return null;
		}
		
		public object Visit(WithStatement withStatement, object data)
		{
			DebugOutput(withStatement);
			withExpressionStack.Push(withStatement.WithExpression);
			withStatement.Body.AcceptVisitor(this, data);
			withExpressionStack.Pop();
			return null;
		}
		
		public object Visit(ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attribute, object data)
		{
			DebugOutput(attribute);
			AppendIndentation();
			sourceText.Append("// Should never happen (this is handled in AttributeSection) attribute was:");
			sourceText.Append(attribute);
			AppendNewLine();
			return null;
		}
		
		public object Visit(AttributeSection attributeSection, object data)
		{
			DebugOutput(attributeSection);
			AppendIndentation();
			sourceText.Append("[");
			if (attributeSection.AttributeTarget != null && attributeSection.AttributeTarget.Length > 0) {
				sourceText.Append(attributeSection.AttributeTarget);
				sourceText.Append(": ");
			}
			for (int j = 0; j < attributeSection.Attributes.Count; ++j) {
				ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute attr = (ICSharpCode.SharpRefactory.Parser.AST.VB.Attribute)attributeSection.Attributes[j];
				
				sourceText.Append(attr.Name);
				sourceText.Append("(");
				for (int i = 0; i < attr.PositionalArguments.Count; ++i) {
					Expression expr = (Expression)attr.PositionalArguments[i];
					sourceText.Append(expr.AcceptVisitor(this, data).ToString());
					if (i + 1 < attr.PositionalArguments.Count | attr.NamedArguments.Count > 0) { 
						sourceText.Append(", ");
					}
				}

				for (int i = 0; i < attr.NamedArguments.Count; ++i) {
					NamedArgumentExpression named = (NamedArgumentExpression)attr.NamedArguments[i];
					sourceText.Append(named.AcceptVisitor(this, data).ToString());
					if (i + 1 < attr.NamedArguments.Count) { 
						sourceText.Append(", ");
					}
				}
				sourceText.Append(")");
				if (j + 1 < attributeSection.Attributes.Count) {
					sourceText.Append(", ");
				}
			}
			sourceText.Append("]");
			AppendNewLine();
			return null;
		}
		
		public object Visit(OptionCompareDeclaration optionCompareDeclaration, object data)
		{
			DebugOutput(optionCompareDeclaration);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(optionCompareDeclaration);
			AppendNewLine();
			return null;
		}
		
		public object Visit(OptionExplicitDeclaration optionExplicitDeclaration, object data)
		{
			DebugOutput(optionExplicitDeclaration);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(optionExplicitDeclaration);
			AppendNewLine();
			return null;
		}
		
		public object Visit(OptionStrictDeclaration optionStrictDeclaration, object data)
		{
			DebugOutput(optionStrictDeclaration);
			AppendIndentation();
			sourceText.Append("// TODO: NotImplemented statement: ");
			sourceText.Append(optionStrictDeclaration);
			AppendNewLine();
			return null;
		}
#endregion

#region Expressions
		public object Visit(PrimitiveExpression primitiveExpression, object data)
		{
			DebugOutput(primitiveExpression);
			if (primitiveExpression.Value == null) {
				return "null";
			}
			if (primitiveExpression.Value is bool) {
				if ((bool)primitiveExpression.Value) {
					return "true";
				}
				return "false";
			}
			
			if (primitiveExpression.Value is string) {
				string s = primitiveExpression.Value.ToString();
				s = s.Replace("\\","\\\\");
				s = s.Replace("\"","\\\"");
				return String.Concat('"', s, '"');
			}
			
			if (primitiveExpression.Value is char) {
				string s = primitiveExpression.Value.ToString();
				s = s.Replace("\\","\\\\");
				s = s.Replace("\'","\\\'");
				return String.Concat("'", s, "'");
			}
			
			if (primitiveExpression.Value is System.DateTime) {
				string s = primitiveExpression.StringValue;
				s = s.Replace("\\","\\\\");
				s = s.Replace("\"","\\\"");
				return String.Concat("System.DateTime.Parse(\"", s, "\")");
			}

			return primitiveExpression.Value;
		}
		
		public object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			DebugOutput(binaryOperatorExpression);
			string op   = null;
			string left = binaryOperatorExpression.Left.AcceptVisitor(this, data).ToString();
			string right = binaryOperatorExpression.Right.AcceptVisitor(this, data).ToString();
			
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Concat:
					op = " + ";
					break;
				
				case BinaryOperatorType.Add:
					op = " + ";
					break;
				
				case BinaryOperatorType.Subtract:
					op = " - ";
					break;
				
				case BinaryOperatorType.Multiply:
					op = " * ";
					break;
				
				case BinaryOperatorType.DivideInteger:
				case BinaryOperatorType.Divide:
					op = " / ";
					break;
				
				case BinaryOperatorType.Modulus:
					op = " % ";
					break;
				
				case BinaryOperatorType.ShiftLeft:
					op = " << ";
					break;
				
				case BinaryOperatorType.ShiftRight:
					op = " >> ";
					break;
				
				case BinaryOperatorType.BitwiseAnd:
					op = " & ";
					break;
				case BinaryOperatorType.BitwiseOr:
					op = " | ";
					break;
				case BinaryOperatorType.ExclusiveOr:
					op = " ^ ";
					break;
				
				case BinaryOperatorType.BooleanAnd:
					op = " && ";
					break;
				case BinaryOperatorType.BooleanOr:
					op = " || ";
					break;
				
				case BinaryOperatorType.Equality:
					op = " == ";
					break;
				case BinaryOperatorType.GreaterThan:
					op = " > ";
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					op = " >= ";
					break;
				case BinaryOperatorType.InEquality:
					op = " != ";
					break;
				case BinaryOperatorType.LessThan:
					op = " < ";
					break;
				case BinaryOperatorType.IS:
					op = " == ";
					break;
				case BinaryOperatorType.LessThanOrEqual:
					op = " <= ";
					break;
				case BinaryOperatorType.Power:
					return "Math.Pow(" + left + ", " + right + ")";
				default:
					throw new Exception("Unknown binary operator:" + binaryOperatorExpression.Op);
			}
			
			return String.Concat(left,
			                     op,
			                     right);
		}
		
		public object Visit(ParenthesizedExpression parenthesizedExpression, object data)
		{
			DebugOutput(parenthesizedExpression);
			string innerExpr = parenthesizedExpression.Expression.AcceptVisitor(this, data).ToString();
			return String.Concat("(", innerExpr, ")");
		}
		
		public object Visit(InvocationExpression invocationExpression, object data)
		{
			DebugOutput(invocationExpression);
			return String.Concat(invocationExpression.TargetObject.AcceptVisitor(this, data),
			                     GetParameters(invocationExpression.Parameters)
			                     );
		}
		
		public object Visit(IdentifierExpression identifierExpression, object data)
		{
			DebugOutput(identifierExpression);
			return identifierExpression.Identifier;
		}
		
		public object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			DebugOutput(typeReferenceExpression);
			return GetTypeString(typeReferenceExpression.TypeReference);
		}
		
		public object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			DebugOutput(unaryOperatorExpression);
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.BitNot:
					return String.Concat("~", unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Decrement:
					return String.Concat("--", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.Increment:
					return String.Concat("++", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.Minus:
					return String.Concat("-", unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Not:
					return String.Concat("!(", unaryOperatorExpression.Expression.AcceptVisitor(this, data), ")");
				case UnaryOperatorType.Plus:
					return String.Concat("+", unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.PostDecrement:
					return String.Concat(unaryOperatorExpression.Expression.AcceptVisitor(this, data), "--");
				case UnaryOperatorType.PostIncrement:
					return String.Concat(unaryOperatorExpression.Expression.AcceptVisitor(this, data), "++");
			}
			throw new System.NotSupportedException();
		}
		
		public object Visit(AssignmentExpression assignmentExpression, object data)
		{
			DebugOutput(assignmentExpression);
			string op    = null;
			string left  = assignmentExpression.Left.AcceptVisitor(this, data).ToString();
			string right = assignmentExpression.Right.AcceptVisitor(this, data).ToString();
			
			switch (assignmentExpression.Op) {
				case AssignmentOperatorType.Assign:
					op = " = ";
					break;
				case AssignmentOperatorType.ConcatString:
				case AssignmentOperatorType.Add:
					op = " += ";
					break;
				case AssignmentOperatorType.Subtract:
					op = " -= ";
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
					op = " ^= ";
					break;
				case AssignmentOperatorType.Modulus:
					op = " %= ";
					break;
				case AssignmentOperatorType.BitwiseAnd:
					op = " &= ";
					break;
				case AssignmentOperatorType.BitwiseOr:
					op = " |= ";
					break;
			}
			return String.Concat(left,
			                     op,
			                     right);
		}
		
		public object Visit(CastExpression castExpression, object data)
		{
			DebugOutput(castExpression);
			string type     = ConvertTypeString(castExpression.CastTo.Type);
			string castExpr = castExpression.Expression.AcceptVisitor(this, data).ToString();
			
			if (castExpression.IsSpecializedCast) {
				switch (type) {
					case "System.Object":
						break;
					default:
						string convToType = type.Substring("System.".Length);
						return String.Format("System.Convert.To{0}({1})", convToType, castExpr);
				}
			}
			return String.Format("(({0})({1}))", type, castExpr);
		}
		
		public object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			DebugOutput(thisReferenceExpression);
			return "this";
		}
		
		public object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			DebugOutput(baseReferenceExpression);
			return "base";
		}
		
		public object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			return String.Format("new {0}{1}",
			                     GetTypeString(objectCreateExpression.CreateType),
			                     GetParameters(objectCreateExpression.Parameters)
			                     );
		}
		
		public object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			DebugOutput(parameterDeclarationExpression);
			// Is handled in the AppendParameters method
			return "// should never happen" + parameterDeclarationExpression;
		}
		
		public object Visit(FieldReferenceOrInvocationExpression fieldReferenceOrInvocationExpression, object data)
		{
			DebugOutput(fieldReferenceOrInvocationExpression);
			INode target = fieldReferenceOrInvocationExpression.TargetObject;
			if (target == null && withExpressionStack.Count > 0) {
				target = withExpressionStack.Peek() as INode;
			}
			return String.Concat(target.AcceptVisitor(this, data),
			                     '.',
			                     fieldReferenceOrInvocationExpression.FieldName);
		}
		
		public object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			DebugOutput(arrayInitializerExpression);
			if (arrayInitializerExpression.CreateExpressions.Count > 0) {
				return String.Concat("{",
				                     GetExpressionList(arrayInitializerExpression.CreateExpressions),
				                     "}");
			}
			return String.Empty;
		}
		
		public object Visit(GetTypeExpression getTypeExpression, object data)
		{
			DebugOutput(getTypeExpression);
			return String.Concat("typeof(",
			                     this.GetTypeString(getTypeExpression.Type),
			                     ")");
		}
		
		public object Visit(ClassReferenceExpression classReferenceExpression, object data)
		{
			// ALMOST THE SAME AS '.this' but ignores all overridings from virtual
			// members. How can this done in C# ?
			DebugOutput(classReferenceExpression);
			return "TODO : " + classReferenceExpression;
		}
		
		public object Visit(LoopControlVariableExpression loopControlVariableExpression, object data)
		{
			// I think the LoopControlVariableExpression is only used in the for statement
			// and there it is handled
			DebugOutput(loopControlVariableExpression);
			return "Should Never happen : " + loopControlVariableExpression;
		}
		
		public object Visit(NamedArgumentExpression namedArgumentExpression, object data)
		{
			return String.Concat(namedArgumentExpression.Parametername,
			                     "=",
			                    namedArgumentExpression.Expression.AcceptVisitor(this, data));
		}
		
		public object Visit(AddressOfExpression addressOfExpression, object data)
		{
			DebugOutput(addressOfExpression);
			string procedureName    = addressOfExpression.Procedure.AcceptVisitor(this, data).ToString();
			string eventHandlerType = "EventHandler";
			bool   foundEventHandler = false;
			// try to resolve the type of the eventhandler using a little trick :)
			foreach (INode node in currentType.Children) {
				MethodDeclaration md = node as MethodDeclaration;
				if (md != null && md.Parameters != null && md.Parameters.Count > 0) {
					if (procedureName == md.Name || procedureName.EndsWith("." + md.Name)) {
						ParameterDeclarationExpression pde = (ParameterDeclarationExpression)md.Parameters[md.Parameters.Count - 1];
						string typeName = GetTypeString(pde.TypeReference);
						if (typeName.EndsWith("Args")) {
							eventHandlerType = typeName.Substring(0, typeName.Length - "Args".Length) + "Handler";
							foundEventHandler = true;
						}
					}
				}
			}
			return String.Concat(foundEventHandler ? "new " : "/* might be wrong, please check */ new ",
			                     eventHandlerType,
			                     "(",
			                     procedureName,
			                     ")");
		}
		
		public object Visit(TypeOfExpression typeOfExpression, object data)
		{
			DebugOutput(typeOfExpression);
			return String.Concat(typeOfExpression.Expression.AcceptVisitor(this, data),
			                     " is ",
			                     GetTypeString(typeOfExpression.Type));
		}
		
		public object Visit(ArrayCreateExpression ace, object data)
		{
			DebugOutput(ace);
			
			return String.Concat("new ",
			                     GetTypeString(ace.CreateType),
			                     "[",
			                     GetExpressionList(ace.Parameters),
			                     "]",
			                     ace.ArrayInitializer.AcceptVisitor(this, data));
		}
#endregion
#endregion
		
		public void AppendAttributes(ArrayList attr)
		{
			if (attr != null) {
				foreach (AttributeSection section in attr) {
					section.AcceptVisitor(this, null);
				}
			}
		}
		
		public void AppendParameters(ArrayList parameters)
		{
			if (parameters == null) {
				return;
			}
			for (int i = 0; i < parameters.Count; ++i) {
				ParameterDeclarationExpression pde = (ParameterDeclarationExpression)parameters[i];
				AppendAttributes(pde.Attributes);
				
				if ((pde.ParamModifiers.Modifier & ParamModifier.ByRef) == ParamModifier.ByRef) {
					sourceText.Append("ref ");
				} else if ((pde.ParamModifiers.Modifier & ParamModifier.ParamArray) == ParamModifier.ParamArray) {
					sourceText.Append("params ");
				}
				
				sourceText.Append(GetTypeString(pde.TypeReference));
				sourceText.Append(" ");
				sourceText.Append(pde.ParameterName);
				if (i + 1 < parameters.Count) {
					sourceText.Append(", ");
				}
			}
		}
		
		string ConvertTypeString(string typeString)
		{
			switch (typeString.ToLower()) {
				case "boolean":
					return "bool";
				case "string":
					return "string";
				case "char":
					return "char";
				case "double":
					return "double";
				case "single":
					return "float";
				case "decimal":
					return "decimal";
				case "date":
					return "System.DateTime";
				case "long":
					return "long";
				case "integer":
					return "int";
				case "short":
					return "short";
				case "byte":
					return "byte";
				case "void":
					return "void";
				case "system.object":
				case "object":
					return "object";
				case "system.uint64":
					return "ulong";
				case "system.uint32":
					return "uint";
				case "system.uint16":
					return "ushort";
			}
			return typeString;
		}
		
		string GetTypeString(TypeReference typeRef)
		{
			if (typeRef == null) {
				return "void";
			}
			
			string typeStr = ConvertTypeString(typeRef.Type);
		
			StringBuilder arrays = new StringBuilder();

			if (typeRef.RankSpecifier != null) {
				for (int i = 0; i < typeRef.RankSpecifier.Count; ++i) {
					arrays.Append("[");
					arrays.Append(new String(',', (int)typeRef.RankSpecifier[i]));
					arrays.Append("]");
				}
			} else {
				if (typeRef.Dimension != null) {
					arrays.Append("[");
					if (typeRef.Dimension.Count > 0) {
						arrays.Append(new String(',', typeRef.Dimension.Count - 1));
					}
					arrays.Append("]");
				}
			}
			
			return typeStr + arrays.ToString();
		}
		
		string GetModifier(Modifier modifier)
		{
			StringBuilder builder = new StringBuilder();
			
			if ((modifier & Modifier.Public) == Modifier.Public) {
				builder.Append("public ");
			} else if ((modifier & Modifier.Private) == Modifier.Private) {
				builder.Append("private ");
			} else if ((modifier & (Modifier.Protected | Modifier.Friend)) == (Modifier.Protected | Modifier.Friend)) {
				builder.Append("protected internal ");
			} else if ((modifier & Modifier.Friend) == Modifier.Friend) {
				builder.Append("internal ");
			} else if ((modifier & Modifier.Protected) == Modifier.Protected) {
				builder.Append("protected ");
			}
			
			if ((modifier & Modifier.MustInherit) == Modifier.MustInherit) {
				builder.Append("abstract ");
			}
			if ((modifier & Modifier.Shared) == Modifier.Shared) {
				builder.Append("static ");
			}
			if ((modifier & Modifier.Overridable) == Modifier.Overridable) {
				builder.Append("virtual ");
			}
			if ((modifier & Modifier.MustOverride) == Modifier.MustOverride) {
				builder.Append("abstract ");
			}
			if ((modifier & Modifier.Overrides) == Modifier.Overrides) {
				builder.Append("override ");
			}
			if ((modifier & Modifier.Shadows) == Modifier.Shadows) {
				builder.Append("new ");
			}
			
			if ((modifier & Modifier.NotInheritable) == Modifier.NotInheritable) {
				builder.Append("sealed ");
			}
			
			if ((modifier & Modifier.Constant) == Modifier.Constant) {
				builder.Append("const ");
			}
			if ((modifier & Modifier.ReadOnly) == Modifier.ReadOnly) {
				builder.Append("readonly ");
			}
			return builder.ToString();
		}

		string GetParameters(ArrayList list)
		{
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
					if (exp != null) {
						sb.Append(exp.AcceptVisitor(this, null));
						if (i + 1 < list.Count) {
							sb.Append(", ");
						}
					}
				}
			}
			return sb.ToString();
		}
		
	}
}	
