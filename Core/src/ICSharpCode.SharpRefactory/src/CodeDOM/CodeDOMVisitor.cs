// CodeDOMVisitor.cs
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

using ICSharpCode.SharpRefactory.Parser.AST;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class CodeDOMVisitor : AbstractASTVisitor
	{
		Stack namespaceDeclarations = new Stack();
		Stack typeDeclarations     = new Stack();
		Stack codeStack  = new Stack();
		TypeDeclaration currentTypeDeclaration;

		// dummy collection used to swallow statements
		System.CodeDom.CodeStatementCollection NullStmtCollection = new CodeStatementCollection();
		
		public CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
		public ArrayList namespaces = new ArrayList();
		
		static string[,] typeConversionList = new string[,] {
			{"System.Void",    "void"},
			{"System.Object",  "object"},
			{"System.Boolean", "bool"},
			{"System.Byte",    "byte"},
			{"System.SByte",   "sbyte"},
			{"System.Char",    "char"},
			{"System.Enum",    "enum"},
			{"System.Int16",   "short"},
			{"System.Int32",   "int"},
			{"System.Int64",   "long"},
			{"System.UInt16",  "ushort"},
			{"System.UInt32",  "uint"},
			{"System.UInt64",  "ulong"},
			{"System.Single",  "float"},
			{"System.Double",  "double"},
			{"System.Decimal", "decimal"},
			{"System.String",  "string"}
		};
		
		static Hashtable typeConversionTable = new Hashtable();
		
		static CodeDOMVisitor()
		{
			for (int i = 0; i < typeConversionList.GetLength(0); ++i) {
				typeConversionTable[typeConversionList[i, 1]] = typeConversionList[i, 0];
			}
		}

		string ConvType(string type) 
		{
			if (typeConversionTable[type] != null) {
				return typeConversionTable[type].ToString();
			}
			return type;
		}

		void AddStmt(System.CodeDom.CodeStatement stmt)
		{
			System.CodeDom.CodeStatementCollection stmtCollection = codeStack.Peek() as System.CodeDom.CodeStatementCollection;
			if (stmtCollection != null) {
				stmtCollection.Add(stmt);
			}
		}

		void AddStmt(System.CodeDom.CodeExpression expr)
		{
			System.CodeDom.CodeStatementCollection stmtCollection = codeStack.Peek() as System.CodeDom.CodeStatementCollection;
			if (stmtCollection != null) {
				stmtCollection.Add(expr);
			}
		}

		// FIXME: map all modifiers correctly
		MemberAttributes ConvMemberAttributes(Modifier modifier) 
		{
			MemberAttributes attr = (MemberAttributes)0;

			if ((modifier & Modifier.Abstract) != 0)
				attr |=  MemberAttributes.Abstract;
//			if ((modifier & Modifier.None) != 0)
//				attr |=  MemberAttributes.AccessMask;
			if ((modifier & Modifier.Internal) != 0)
				attr |=  MemberAttributes.Assembly;
			if ((modifier & Modifier.Const) != 0)
				attr |=  MemberAttributes.Const;
			if ((modifier & Modifier.Protected) != 0)
				attr |=  MemberAttributes.Family;
			if ((modifier & Modifier.Protected) != 0 && (modifier & Modifier.Internal) != 0)
				attr |=  MemberAttributes.FamilyAndAssembly;
//			if ((modifier & Modifier.None) != 0)
//				attr |=  MemberAttributes.FamilyOrAssembly;
			if ((modifier & Modifier.Sealed) != 0)
				attr |=  MemberAttributes.Final;
			if ((modifier & Modifier.New) != 0)
				attr |=  MemberAttributes.New;
			if ((modifier & Modifier.Virtual) != 0)
				attr |=  MemberAttributes.Overloaded;
			if ((modifier & Modifier.Override) != 0)
				attr |=  MemberAttributes.Override;
			if ((modifier & Modifier.Private) != 0)
				attr |=  MemberAttributes.Private;
			if ((modifier & Modifier.Public) != 0)
				attr |=  MemberAttributes.Public;
//			if ((modifier & Modifier.None) != 0)
//				attr |=  MemberAttributes.ScopeMask;
			if ((modifier & Modifier.Static) != 0)
				attr |=  MemberAttributes.Static;
//			if ((modifier & Modifier.None) != 0)
//				attr |=  MemberAttributes.VTableMask;

			return attr;
		}

		void ProcessSpecials(Hashtable specials)
		{
			if (specials == null) {
				return;
			}
			
			foreach (object special in specials) {
				if (special is BlankLine) {
					AddStmt(new CodeSnippetStatement());
				} else if (special is PreProcessingDirective) {
					// TODO
				} else if (special is Comment) {
					Comment comment = (Comment)special;
					switch (comment.CommentType) {
						case CommentType.SingleLine:
							AddStmt(new CodeCommentStatement(comment.CommentText, false));
							break;
						case CommentType.Documentation:
							AddStmt(new CodeCommentStatement(comment.CommentText, true));
							break;
						case CommentType.Block:
							AddStmt(new CodeCommentStatement(comment.CommentText, false));
							break;
					}
				}
			}
		}

#region ICSharpCode.SharpRefactory.Parser.IASTVisitor interface implementation
		public override object Visit(CompilationUnit compilationUnit, object data)
		{
			CodeNamespace globalNamespace = new CodeNamespace("Global");
			namespaces.Add(globalNamespace);
			namespaceDeclarations.Push(globalNamespace);
			compilationUnit.AcceptChildren(this, data);
			codeCompileUnit.Namespaces.Add(globalNamespace);
			return globalNamespace;
		}
		
		public override object Visit(NamespaceDeclaration namespaceDeclaration, object data)
		{
			ProcessSpecials(namespaceDeclaration.Specials);

			CodeNamespace currentNamespace = new CodeNamespace(namespaceDeclaration.NameSpace);
			namespaces.Add(currentNamespace);
			// add imports from mother namespace
			foreach (CodeNamespaceImport import in ((CodeNamespace)namespaceDeclarations.Peek()).Imports) {
				currentNamespace.Imports.Add(import);
			}
			namespaceDeclarations.Push(currentNamespace);
			namespaceDeclaration.AcceptChildren(this, data);
			namespaceDeclarations.Pop();
			codeCompileUnit.Namespaces.Add(currentNamespace);
			
			// TODO : Nested namespaces allowed in CodeDOM ? Doesn't seem so :(
			return null;
		}
		
		public override object Visit(UsingDeclaration usingDeclaration, object data)
		{
			ProcessSpecials(usingDeclaration.Specials);

			((CodeNamespace)namespaceDeclarations.Peek()).Imports.Add(new CodeNamespaceImport(usingDeclaration.Namespace));
			return null;
		}
		
		public override object Visit(UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(AttributeSection attributeSection, object data)
		{
			return null;
		}
		
		public override object Visit(TypeDeclaration typeDeclaration, object data)
		{
			ProcessSpecials(typeDeclaration.Specials);

			this.currentTypeDeclaration = typeDeclaration;
			CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration(typeDeclaration.Name);
			codeTypeDeclaration.IsClass     = typeDeclaration.Type == Types.Class;
			codeTypeDeclaration.IsEnum      = typeDeclaration.Type == Types.Enum;
			codeTypeDeclaration.IsInterface = typeDeclaration.Type == Types.Interface;
			codeTypeDeclaration.IsStruct    = typeDeclaration.Type == Types.Struct;
			
			if (typeDeclaration.BaseTypes != null) {
				foreach (object o in typeDeclaration.BaseTypes) {
					codeTypeDeclaration.BaseTypes.Add(new CodeTypeReference(o.ToString()));
				}
			}
			
			typeDeclarations.Push(codeTypeDeclaration);
			typeDeclaration.AcceptChildren(this,data);
//			((INode)typeDeclaration.Children[0]).(this, data);
			
			typeDeclarations.Pop();
			
			((CodeNamespace)namespaceDeclarations.Peek()).Types.Add(codeTypeDeclaration);
			
			return null;
		}
		
		public override object Visit(DelegateDeclaration delegateDeclaration, object data)
		{
//			CodeTypeDelegate codeTypeDelegate = new CodeTypeDelegate(delegateDeclaration.Name);
//			codeTypeDelegate.Parameters
//			
//			((CodeNamespace)namespaceDeclarations.Peek()).Types.Add(codeTypeDelegate);
			return null;
		}
		
		public override object Visit(VariableDeclaration variableDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			ProcessSpecials(fieldDeclaration.Specials);

			for (int i = 0; i < fieldDeclaration.Fields.Count; ++i) 
			{
				VariableDeclaration field = (VariableDeclaration)fieldDeclaration.Fields[i];
				
				CodeMemberField memberField = new CodeMemberField(new CodeTypeReference(ConvType(fieldDeclaration.TypeReference.Type)), field.Name);
				memberField.Attributes = ConvMemberAttributes(fieldDeclaration.Modifier);
				if (field.Initializer != null) {
					memberField.InitExpression =  (CodeExpression)((INode)field.Initializer).AcceptVisitor(this, data);
				}
				
				((CodeTypeDeclaration)typeDeclarations.Peek()).Members.Add(memberField);
			}
			
			return null;
		}
		
		public override object Visit(MethodDeclaration methodDeclaration, object data)
		{
			ProcessSpecials(methodDeclaration.Specials);

			CodeMemberMethod memberMethod = new CodeMemberMethod();
			memberMethod.Name = methodDeclaration.Name;
			memberMethod.Attributes = ConvMemberAttributes(methodDeclaration.Modifier);
			
			codeStack.Push(memberMethod.Statements);

			((CodeTypeDeclaration)typeDeclarations.Peek()).Members.Add(memberMethod);

			// Add Method Parameters
			foreach (ParameterDeclarationExpression parameter in methodDeclaration.Parameters)
			{
				memberMethod.Parameters.Add((CodeParameterDeclarationExpression)Visit(parameter, data));
			}

			methodDeclaration.Body.AcceptChildren(this, data);

			codeStack.Pop();

			return null;
		}
		
		public override object Visit(PropertyDeclaration propertyDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(PropertyGetRegion propertyGetRegion, object data)
		{
			return null;
		}
		
		public override object Visit(PropertySetRegion PropertySetRegion, object data)
		{
			return null;
		}
		
		public override object Visit(EventDeclaration eventDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(EventAddRegion eventAddRegion, object data)
		{
			return null;
		}
		
		public override object Visit(EventRemoveRegion eventRemoveRegion, object data)
		{
			return null;
		}
		
		public override object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			ProcessSpecials(constructorDeclaration.Specials);

			CodeMemberMethod memberMethod = new CodeConstructor();

			codeStack.Push(memberMethod.Statements);
			((CodeTypeDeclaration)typeDeclarations.Peek()).Members.Add(memberMethod);
//			constructorDeclaration.AcceptChildren(this, data);
			codeStack.Pop();

			return null;
		}
		
		public override object Visit(DestructorDeclaration destructorDeclaration, object data)
		{
			return null;
		}

		public override object Visit(OperatorDeclaration operatorDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(IndexerDeclaration indexerDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(BlockStatement blockStatement, object data)
		{
			ProcessSpecials(blockStatement.Specials);

			blockStatement.AcceptChildren(this, data);
			return null;
		}
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			object exp = statementExpression.Expression.AcceptVisitor(this, data);
			if (exp is CodeExpression) {
				AddStmt(new CodeExpressionStatement((CodeExpression)exp));
			}
			return exp;
		}
		
		public string Convert(TypeReference typeRef)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(ConvType(typeRef.Type));
			
			for (int i = 0; i < typeRef.PointerNestingLevel; ++i) {
				builder.Append('*');
			}
			
			
			for (int i = 0; i < typeRef.RankSpecifier.Length; ++i) {
				builder.Append('[');
				for (int j = 1; j < typeRef.RankSpecifier[i]; ++j) {
					builder.Append(',');
				}
				builder.Append(']');
			}
			
			return builder.ToString();
		}
		
		public override object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			CodeVariableDeclarationStatement declStmt = null;

			CodeTypeReference type = new CodeTypeReference(Convert(localVariableDeclaration.Type));
			
			foreach (VariableDeclaration var in localVariableDeclaration.Variables) {
				if (var.Initializer != null) {
					declStmt = new CodeVariableDeclarationStatement(type,
					                                             var.Name,
					                                             (CodeExpression)((INode)var.Initializer).AcceptVisitor(this, data));
				} else {
					declStmt = new CodeVariableDeclarationStatement(type,
					                                             var.Name);
				}
			}

			AddStmt(declStmt);

			return declStmt;
		}
		
		public override object Visit(EmptyStatement emptyStatement, object data)
		{
			CodeSnippetStatement emptyStmt = new CodeSnippetStatement();

			AddStmt(emptyStmt);

			return emptyStmt;
		}
		
		public override object Visit(ReturnStatement returnStatement, object data)
		{
			ProcessSpecials(returnStatement.Specials);

			CodeMethodReturnStatement returnStmt = new CodeMethodReturnStatement((CodeExpression)returnStatement.ReturnExpression.AcceptVisitor(this,data));

			AddStmt(returnStmt);

			return returnStmt;
		}
		
		public override object Visit(IfStatement ifStatement, object data)
		{
			ProcessSpecials(ifStatement.Specials);

			CodeConditionStatement ifStmt = new CodeConditionStatement();
			
			ifStmt.Condition = (CodeExpression)ifStatement.Condition.AcceptVisitor(this, data);

			codeStack.Push(ifStmt.TrueStatements);
			ifStatement.EmbeddedStatement.AcceptChildren(this, data);
			codeStack.Pop();

			AddStmt(ifStmt);

			return ifStmt;
		}
		
		public override object Visit(IfElseStatement ifElseStatement, object data)
		{
			ProcessSpecials(ifElseStatement.Specials);

			CodeConditionStatement ifStmt = new CodeConditionStatement();

			ifStmt.Condition = (CodeExpression)ifElseStatement.Condition.AcceptVisitor(this, data);

			codeStack.Push(ifStmt.TrueStatements);
			ifElseStatement.EmbeddedStatement.AcceptChildren(this, data);
			codeStack.Pop();

			codeStack.Push(ifStmt.FalseStatements);
			ifElseStatement.EmbeddedElseStatement.AcceptChildren(this, data);
			codeStack.Pop();

			AddStmt(ifStmt);

			return ifStmt;
		}
		
		public override object Visit(WhileStatement whileStatement, object data)
		{
			return null;
		}
		
		public override object Visit(DoWhileStatement doWhileStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ForStatement forStatement, object data)
		{
			CodeIterationStatement forLoop = new CodeIterationStatement();

			if (forStatement.Initializers != null) 
			{
				if (forStatement.Initializers.Count > 1)
				{
					throw new NotSupportedException("CodeDom does not support Multiple For-Loop Initializer Statements");
				}

				foreach (object o in forStatement.Initializers)
				{
					if (o is Expression) 
					{
						forLoop.InitStatement = new CodeExpressionStatement((CodeExpression)((Expression)o).AcceptVisitor(this,data));
					}
					if (o is Statement) 
					{
						codeStack.Push(NullStmtCollection);
						forLoop.InitStatement = (CodeStatement)((Statement)o).AcceptVisitor(this, data);
						codeStack.Pop();
					}
				}
			}

			if (forStatement.Condition == null) 
			{
				forLoop.TestExpression = new CodePrimitiveExpression(true);
			} 
			else 
			{
				forLoop.TestExpression = (CodeExpression)forStatement.Condition.AcceptVisitor(this, data);
			}

			codeStack.Push(forLoop.Statements);
			forStatement.EmbeddedStatement.AcceptVisitor(this, data);
			codeStack.Pop();

			if (forStatement.Iterator != null) 
			{
				if (forStatement.Initializers.Count > 1)
				{
					throw new NotSupportedException("CodeDom does not support Multiple For-Loop Iterator Statements");
				}

				foreach (Statement stmt in forStatement.Iterator) 
				{
					forLoop.IncrementStatement = (CodeStatement)stmt.AcceptVisitor(this, data);
				}
			}

			AddStmt(forLoop);

			return forLoop;
		}
		
		public override object Visit(LabelStatement labelStatement, object data)
		{
			ProcessSpecials(labelStatement.Specials);

			System.CodeDom.CodeLabeledStatement labelStmt = new CodeLabeledStatement(labelStatement.Label,(CodeStatement)labelStatement.AcceptVisitor(this, data));

			// Add Statement to Current Statement Collection
			AddStmt(labelStmt);

			return labelStmt;
		}
		
		public override object Visit(GotoStatement gotoStatement, object data)
		{
			ProcessSpecials(gotoStatement.Specials);

			System.CodeDom.CodeGotoStatement gotoStmt = new CodeGotoStatement(gotoStatement.Label);

			// Add Statement to Current Statement Collection
			AddStmt(gotoStmt);

			return gotoStmt;
		}
		
		public override object Visit(SwitchStatement switchStatement, object data)
		{
			throw new NotSupportedException("CodeDom does not support Switch Statement");
		}
		
		public override object Visit(BreakStatement breakStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ContinueStatement continueStatement, object data)
		{
			return null;
		}
		
		public override object Visit(GotoCaseStatement gotoCaseStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ForeachStatement foreachStatement, object data)
		{
			return null;
		}
		
		public override object Visit(LockStatement lockStatement, object data)
		{
			return null;
		}
		
		public override object Visit(UsingStatement usingStatement, object data)
		{
			return null;
		}
		
		public override object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			ProcessSpecials(tryCatchStatement.Specials);

			// add a try-catch-finally
			CodeTryCatchFinallyStatement tryStmt = new CodeTryCatchFinallyStatement();

			codeStack.Push(tryStmt.TryStatements);
			ProcessSpecials(tryCatchStatement.StatementBlock.Specials);
			tryCatchStatement.StatementBlock.AcceptChildren(this, data);
			codeStack.Pop();

			if (tryCatchStatement.FinallyBlock != null)
			{
				codeStack.Push(tryStmt.FinallyStatements);
				ProcessSpecials(tryCatchStatement.FinallyBlock.Specials);
				tryCatchStatement.FinallyBlock.AcceptChildren(this,data);
				codeStack.Pop();
			}

			if (tryCatchStatement.CatchClauses != null) 
			{
				foreach (CatchClause clause in tryCatchStatement.CatchClauses) 
				{
					CodeCatchClause catchClause = new CodeCatchClause(clause.VariableName);
					catchClause.CatchExceptionType = new CodeTypeReference(clause.Type);
					tryStmt.CatchClauses.Add(catchClause);

					codeStack.Push(catchClause.Statements);
					ProcessSpecials(clause.StatementBlock.Specials);
					clause.StatementBlock.AcceptChildren(this, data);
					codeStack.Pop();
				}
			}

			// Add Statement to Current Statement Collection
			AddStmt(tryStmt);

			return tryStmt;
		}
		
		public override object Visit(ThrowStatement throwStatement, object data)
		{
			ProcessSpecials(throwStatement.Specials);

			CodeThrowExceptionStatement throwStmt = new CodeThrowExceptionStatement((CodeExpression)throwStatement.ThrowExpression.AcceptVisitor(this, data));

			// Add Statement to Current Statement Collection
			AddStmt(throwStmt);

			return throwStmt;
		}
		
		public override object Visit(FixedStatement fixedStatement, object data)
		{
			throw new NotSupportedException("CodeDom does not support Fixed Statement");
		}
		
		public override object Visit(PrimitiveExpression expression, object data)
		{
			return new CodePrimitiveExpression(expression.Value);
		}
		
		public override object Visit(BinaryOperatorExpression expression, object data)
		{
			CodeBinaryOperatorType op = CodeBinaryOperatorType.Add;
			switch (expression.Op) {
				case BinaryOperatorType.Add:
					op = CodeBinaryOperatorType.Add;
					break;
				case BinaryOperatorType.BitwiseAnd:
					op = CodeBinaryOperatorType.BitwiseAnd;
					break;
				case BinaryOperatorType.BitwiseOr:
					op = CodeBinaryOperatorType.BitwiseOr;
					break;
				case BinaryOperatorType.LogicalAnd:
					op = CodeBinaryOperatorType.BooleanAnd;
					break;
				case BinaryOperatorType.LogicalOr:
					op = CodeBinaryOperatorType.BooleanOr;
					break;
				case BinaryOperatorType.Divide:
					op = CodeBinaryOperatorType.Divide;
					break;
				case BinaryOperatorType.GreaterThan:
					op = CodeBinaryOperatorType.GreaterThan;
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					op = CodeBinaryOperatorType.GreaterThanOrEqual;
					break;
				case BinaryOperatorType.Equality:
					op = CodeBinaryOperatorType.IdentityEquality;
					break;
				case BinaryOperatorType.InEquality:
					op = CodeBinaryOperatorType.IdentityInequality;
					break;
				case BinaryOperatorType.LessThan:
					op = CodeBinaryOperatorType.LessThan;
					break;
				case BinaryOperatorType.LessThanOrEqual:
					op = CodeBinaryOperatorType.LessThanOrEqual;
					break;
				case BinaryOperatorType.Modulus:
					op = CodeBinaryOperatorType.Modulus;
					break;
				case BinaryOperatorType.Multiply:
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.Subtract:
					op = CodeBinaryOperatorType.Subtract;
					break;
				case BinaryOperatorType.ValueEquality:
					op = CodeBinaryOperatorType.ValueEquality;
					break;
				case BinaryOperatorType.ShiftLeft:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.ShiftRight:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.IS:
					op = CodeBinaryOperatorType.IdentityEquality;
					break;
				case BinaryOperatorType.AS:
					op = CodeBinaryOperatorType.IdentityEquality;
					break;
				case BinaryOperatorType.ExclusiveOr:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.BitwiseAnd;
					break;
			}
			return new CodeBinaryOperatorExpression((CodeExpression)expression.Left.AcceptVisitor(this, data),
			                                        op,
			                                        (CodeExpression)expression.Right.AcceptVisitor(this, data));
		}
		
		public override object Visit(ParenthesizedExpression expression, object data)
		{
			return expression.Expression.AcceptVisitor(this, data);
		}
		
		public override object Visit(InvocationExpression invocationExpression, object data)
		{
			Expression     target     = invocationExpression.TargetObject;
			CodeExpression targetExpr;
			string         methodName = null;
			if (target == null) {
				targetExpr = new CodeThisReferenceExpression();
			} else if (target is FieldReferenceExpression) {
				FieldReferenceExpression fRef = (FieldReferenceExpression)target;
				targetExpr = (CodeExpression)fRef.TargetObject.AcceptVisitor(this, data);
				if (fRef.TargetObject is FieldReferenceExpression) {
					FieldReferenceExpression fRef2 = (FieldReferenceExpression)fRef.TargetObject;
					if (fRef2.FieldName != null && Char.IsUpper(fRef2.FieldName[0])) {
						// an exception is thrown if it doesn't end in an indentifier exception
						// for example for : this.MyObject.MyMethod() leads to an exception, which 
						// is correct in this case ... I know this is really HACKY :)
						try {
							targetExpr = ConvertToIdentifier(fRef2);
						} catch (Exception) {}
					}
				}
				methodName = fRef.FieldName;
			} else {
				targetExpr = (CodeExpression)target.AcceptVisitor(this, data);
			}
			return new CodeMethodInvokeExpression(targetExpr, methodName, GetExpressionList(invocationExpression.Parameters));
		}
		
		public override object Visit(IdentifierExpression expression, object data)
		{
			if (IsField(expression.Identifier)) {
				return new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
				                                        expression.Identifier);
			}
			return new CodeVariableReferenceExpression(expression.Identifier);
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return null;
		}

		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			CodeExpression var;

			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Minus:
					if (unaryOperatorExpression.Expression is PrimitiveExpression) {
						PrimitiveExpression expression = (PrimitiveExpression)unaryOperatorExpression.Expression;
						if (expression.Value is System.UInt32 || expression.Value is System.UInt16) {
							return new CodePrimitiveExpression(Int32.Parse("-" + expression.StringValue));
						}
						
						if (expression.Value is int) {
							return new CodePrimitiveExpression(- (int)expression.Value);
						}
						if (expression.Value is long) {
							return new CodePrimitiveExpression(- (long)expression.Value);
						}
						if (expression.Value is double) {
							return new CodePrimitiveExpression(- (double)expression.Value);
						}
						if (expression.Value is float) {
							return new CodePrimitiveExpression(- (float)expression.Value);
						}
						
					} 
					return  new CodeBinaryOperatorExpression(new CodePrimitiveExpression(0),
			                                        CodeBinaryOperatorType.Subtract,
			                                        (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data));
				case UnaryOperatorType.Plus:
					return unaryOperatorExpression.Expression.AcceptVisitor(this, data);

				case UnaryOperatorType.PostIncrement:
					// emulate i++, with i = i + 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					return new CodeAssignStatement(var,
									new CodeBinaryOperatorExpression(var,
																	 CodeBinaryOperatorType.Add,
																	 new CodePrimitiveExpression(1)));

				case UnaryOperatorType.PostDecrement:
					// emulate i--, with i = i - 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					return new CodeAssignStatement(var,
						new CodeBinaryOperatorExpression(var,
						CodeBinaryOperatorType.Subtract,
						new CodePrimitiveExpression(1)));

				case UnaryOperatorType.Decrement:
					// emulate --i, with i = i - 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					return new CodeAssignStatement(var,
						new CodeBinaryOperatorExpression(var,
						CodeBinaryOperatorType.Subtract,
						new CodePrimitiveExpression(1)));

				case UnaryOperatorType.Increment:
					// emulate ++i, with i = i + 1
					var = (CodeExpression)unaryOperatorExpression.Expression.AcceptVisitor(this, data);

					return new CodeAssignStatement(var,
						new CodeBinaryOperatorExpression(var,
						CodeBinaryOperatorType.Add,
						new CodePrimitiveExpression(1)));

			}
			return null;
		}
		bool methodReference = false;
		public override object Visit(AssignmentExpression assignmentExpression, object data)
		{
			if (assignmentExpression.Op == AssignmentOperatorType.Add) {
				
				methodReference = true;
				CodeExpression methodInvoker = (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null);
				methodReference = false;
					
				if (assignmentExpression.Left is IdentifierExpression) 
				{
					AddStmt(new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeThisReferenceExpression(), ((IdentifierExpression)assignmentExpression.Left).Identifier),
					                                                          methodInvoker));
				} else {
					FieldReferenceExpression fr = (FieldReferenceExpression)assignmentExpression.Left;
					
					AddStmt(new CodeAttachEventStatement(new CodeEventReferenceExpression((CodeExpression)fr.TargetObject.AcceptVisitor(this, data), fr.FieldName),
					                                                          methodInvoker));
				}
			} else {
				if (assignmentExpression.Left is IdentifierExpression) {
					AddStmt(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				} else {
					AddStmt(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				}
			}
			return null;
		}
		
		public override object Visit(CheckedStatement checkedStatement, object data)
		{
			return null;
		}
		
		public override object Visit(UncheckedStatement uncheckedStatement, object data)
		{
			return null;
		}
		
		public override object Visit(SizeOfExpression sizeOfExpression, object data)
		{
			return null;
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return new CodeTypeOfExpression(ConvType(typeOfExpression.TypeReference.Type));
		}
		
		public override object Visit(CheckedExpression checkedExpression, object data)
		{
			return null;
		}
		
		public override object Visit(UncheckedExpression uncheckedExpression, object data)
		{
			return null;
		}
		
		public override object Visit(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			return null;
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			string typeRef = castExpression.CastTo.Type;
			return new CodeCastExpression(typeRef, (CodeExpression)castExpression.Expression.AcceptVisitor(this, data));
		}
		
		public override object Visit(StackAllocExpression stackAllocExpression, object data)
		{
			// TODO
			return null;
		}
		
		public override object Visit(IndexerExpression indexerExpression, object data)
		{
			return new CodeIndexerExpression((CodeExpression)indexerExpression.TargetObject.AcceptVisitor(this, data), GetExpressionList(indexerExpression.Indices));
		}
		
		public override object Visit(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return new CodeThisReferenceExpression();
		}
		
		public override object Visit(BaseReferenceExpression baseReferenceExpression, object data)
		{
			return new CodeBaseReferenceExpression();
		}
		
		public override object Visit(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.ArrayInitializer == null) {
				if (arrayCreateExpression.Rank != null && arrayCreateExpression.Rank.Length > 0) {
					return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType.Type),
					                                     arrayCreateExpression.Rank[0]);
				}
				return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType.Type),
				                                     0);
			}
			return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType.Type),
			                                     GetExpressionList(arrayCreateExpression.ArrayInitializer.CreateExpressions));
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			return new CodeObjectCreateExpression(ConvType(objectCreateExpression.CreateType.Type),
			                                      objectCreateExpression.Parameters == null ? null : GetExpressionList(objectCreateExpression.Parameters));
		}
		
		public override object Visit(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			return new CodeParameterDeclarationExpression(new CodeTypeReference(ConvType(parameterDeclarationExpression.TypeReference.Type)), parameterDeclarationExpression.ParameterName);
		}
		
		bool IsField(string type, string fieldName)
		{
			Type t       = null;
			Assembly asm = null;
			
			t = this.GetType(type);
			if (t == null)
			{
				asm = typeof(System.Drawing.Point).Assembly;
				t = asm.GetType(type);
			}
			
			//if (t == null) {
			//	asm = typeof(System.Windows.Forms.Control).Assembly;
			//	t = asm.GetType(type);
			//}
			
			if (t == null) {
				asm = typeof(System.String).Assembly;
				t = asm.GetType(type);
			}
			
			return t != null && t.GetField(fieldName) != null;
		}
		
		bool IsFieldReferenceExpression(FieldReferenceExpression fieldReferenceExpression)
		{
			if (fieldReferenceExpression.TargetObject is ThisReferenceExpression) {
				foreach (object o in this.currentTypeDeclaration.Children) {
					if (o is FieldDeclaration) {
						FieldDeclaration fd = (FieldDeclaration)o;
						foreach (VariableDeclaration field in fd.Fields) {
							if (fieldReferenceExpression.FieldName == field.Name) {
								return true;
							}
						}
					}
				}
			}
			return false; //Char.IsLower(fieldReferenceExpression.FieldName[0]);
		}
		
		public override object Visit(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (methodReference) {
				return new CodeMethodReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data), fieldReferenceExpression.FieldName);
			}
			if (IsFieldReferenceExpression(fieldReferenceExpression)) {
				return new CodeFieldReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data),
				                                        fieldReferenceExpression.FieldName);
			} else {
				if (fieldReferenceExpression.TargetObject is FieldReferenceExpression) {
					if (IsQualIdent((FieldReferenceExpression)fieldReferenceExpression.TargetObject)) {
						CodeTypeReferenceExpression typeRef = ConvertToIdentifier((FieldReferenceExpression)fieldReferenceExpression.TargetObject);
						if (IsField(typeRef.Type.BaseType, fieldReferenceExpression.FieldName)) {
							return new CodeFieldReferenceExpression(typeRef,
							                                           fieldReferenceExpression.FieldName);
						} else {
							return new CodePropertyReferenceExpression(typeRef,
							                                           fieldReferenceExpression.FieldName);
						}
					}
				}
				
				CodeExpression codeExpression = (CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data);
				return new CodePropertyReferenceExpression(codeExpression,
				                                           fieldReferenceExpression.FieldName);
			}
		}
		
		public override object Visit(DirectionExpression directionExpression, object data)
		{
			return null;
		}
		public override object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			return null;
		}
		public override object Visit(ConditionalExpression conditionalExpression, object data)
		{
			return null;
		}
#endregion
		bool IsQualIdent(FieldReferenceExpression fieldReferenceExpression)
		{
			while (fieldReferenceExpression.TargetObject is FieldReferenceExpression) {
				fieldReferenceExpression = (FieldReferenceExpression)fieldReferenceExpression.TargetObject;
			}
			return fieldReferenceExpression.TargetObject is IdentifierExpression;
		}
		
		bool IsField(string identifier)
		{
			foreach (INode node in currentTypeDeclaration.Children) {
				if (node is FieldDeclaration) {
					FieldDeclaration fd = (FieldDeclaration)node;
					if (fd.GetVariableDeclaration(identifier) != null) {
						return true;
					}
				}
			}
			return false;
		}
		
		CodeTypeReferenceExpression ConvertToIdentifier(FieldReferenceExpression fieldReferenceExpression)
		{
//			CodeFieldReferenceExpression  cpre = new CodeFieldReferenceExpression (); 
//			CodeFieldReferenceExpression firstCpre = cpre,newCpre;
			string type = String.Empty;
			
			while (fieldReferenceExpression.TargetObject is FieldReferenceExpression) {
//				newCpre = new CodeFieldReferenceExpression(); 
//				Console.WriteLine(fieldReferenceExpression.FieldName);
//				cpre.FieldName  = fieldReferenceExpression.FieldName;
//				cpre.TargetObject = newCpre;
//				cpre = newCpre;
				type = "."  + fieldReferenceExpression.FieldName + type;
				fieldReferenceExpression = (FieldReferenceExpression)fieldReferenceExpression.TargetObject;
			}
			type = "."  + fieldReferenceExpression.FieldName + type;
//			newCpre = new CodeFieldReferenceExpression(); 
//			Console.WriteLine(fieldReferenceExpression.FieldName);
//			cpre.FieldName  = fieldReferenceExpression.FieldName;
//			cpre.TargetObject = newCpre;
//			cpre = newCpre;
				
			if (fieldReferenceExpression.TargetObject is IdentifierExpression) {
				return new CodeTypeReferenceExpression(((IdentifierExpression)fieldReferenceExpression.TargetObject).Identifier + type);
//				cpre.TargetObject =
//				return firstCpre;
			} else {
				throw new Exception();
			}
		}
		
		CodeExpression[] GetExpressionList(ArrayList expressionList)
		{
			if (expressionList == null) {
				return new CodeExpression[0];
			}
			CodeExpression[] list = new CodeExpression[expressionList.Count];
			for (int i = 0; i < expressionList.Count; ++i) {
				list[i] = (CodeExpression)((Expression)expressionList[i]).AcceptVisitor(this, null);
				if (list[i] == null) {
					list[i] = new CodePrimitiveExpression(0);
				}
			}
			return list;
		}
		
		Type GetType(string typeName)
		{
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) 
			{
				Type type = asm.GetType(typeName);
				if (type != null) 
				{
					return type;
				}
			}
			return Type.GetType(typeName);
		}
	}
}
