// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	public class CodeDomVisitor : AbstractAstVisitor
	{
		Stack<CodeNamespace>  namespaceDeclarations = new Stack<CodeNamespace>();
		Stack<CodeTypeDeclaration> typeDeclarations = new Stack<CodeTypeDeclaration>();
		Stack<CodeStatementCollection> codeStack    = new Stack<CodeStatementCollection>();
		List<CodeVariableDeclarationStatement> variables = new List<CodeVariableDeclarationStatement>();
		
		TypeDeclaration currentTypeDeclaration = null;
		
		IEnvironmentInformationProvider environmentInformationProvider = new DummyEnvironmentInformationProvider();
		
		public IEnvironmentInformationProvider EnvironmentInformationProvider {
			get {
				return environmentInformationProvider;
			}
			set {
				environmentInformationProvider = value;
			}
		}
		
		// dummy collection used to swallow statements
		CodeStatementCollection NullStmtCollection = new CodeStatementCollection();
		
		public CodeCompileUnit codeCompileUnit   = new CodeCompileUnit();
		
		CodeTypeReference ConvType(TypeReference type)
		{
			if (type == null) {
				throw new ArgumentNullException("type");
			}
			if (string.IsNullOrEmpty(type.SystemType)) {
				throw new InvalidOperationException("empty type");
			}
			
			CodeTypeReference t = new CodeTypeReference(type.SystemType);
			foreach (TypeReference gt in type.GenericTypes) {
				t.TypeArguments.Add(ConvType(gt));
			}
			if (type.IsArrayType) {
				t = new CodeTypeReference(t, type.RankSpecifier.Length);
			}
			
			return t;
		}
		
		void AddStmt(CodeStatement stmt)
		{
			if (codeStack.Count == 0)
				return;
			CodeStatementCollection stmtCollection = codeStack.Peek();
			if (stmtCollection != null) {
				stmtCollection.Add(stmt);
			}
		}
		
		// FIXME: map all modifiers correctly
		static MemberAttributes ConvMemberAttributes(Modifiers modifier)
		{
			MemberAttributes attr = (MemberAttributes)0;
			
			if ((modifier & Modifiers.Abstract) != 0)
				attr |=  MemberAttributes.Abstract;
			if ((modifier & Modifiers.Const) != 0)
				attr |=  MemberAttributes.Const;
			if ((modifier & Modifiers.Sealed) != 0)
				attr |=  MemberAttributes.Final;
			if ((modifier & Modifiers.New) != 0)
				attr |=  MemberAttributes.New;
			if ((modifier & Modifiers.Virtual) != 0)
				attr |=  MemberAttributes.Overloaded;
			if ((modifier & Modifiers.Override) != 0)
				attr |=  MemberAttributes.Override;
			if ((modifier & Modifiers.Static) != 0)
				attr |=  MemberAttributes.Static;
			
			if ((modifier & Modifiers.Private) != 0)
				attr |=  MemberAttributes.Private;
			else if ((modifier & Modifiers.Public) != 0)
				attr |=  MemberAttributes.Public;
			else if ((modifier & Modifiers.Internal) != 0 && (modifier & Modifiers.Protected) != 0)
				attr |=  MemberAttributes.FamilyOrAssembly;
			else if ((modifier & Modifiers.Internal) != 0)
				attr |=  MemberAttributes.Assembly;
			else if ((modifier & Modifiers.Protected) != 0)
				attr |=  MemberAttributes.Family;
			
			return attr;
		}
		
		#region ICSharpCode.SharpRefactory.Parser.IASTVisitor interface implementation
		public override object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			if (compilationUnit == null) {
				throw new ArgumentNullException("compilationUnit");
			}
			CodeNamespace globalNamespace = new CodeNamespace("Global");
			//namespaces.Add(globalNamespace);
			namespaceDeclarations.Push(globalNamespace);
			compilationUnit.AcceptChildren(this, data);
			codeCompileUnit.Namespaces.Add(globalNamespace);
			return globalNamespace;
		}
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			CodeNamespace currentNamespace = new CodeNamespace(namespaceDeclaration.Name);
			//namespaces.Add(currentNamespace);
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
		
		public override object VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			foreach (Using u in usingDeclaration.Usings) {
				namespaceDeclarations.Peek().Imports.Add(new CodeNamespaceImport(u.Name));
			}
			return null;
		}
		
		
		public override object VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			return null;
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			TypeDeclaration oldTypeDeclaration = currentTypeDeclaration;
			this.currentTypeDeclaration = typeDeclaration;
			CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration(typeDeclaration.Name);
			codeTypeDeclaration.IsClass     = typeDeclaration.Type == ClassType.Class;
			codeTypeDeclaration.IsEnum      = typeDeclaration.Type == ClassType.Enum;
			codeTypeDeclaration.IsInterface = typeDeclaration.Type == ClassType.Interface;
			codeTypeDeclaration.IsStruct    = typeDeclaration.Type == ClassType.Struct;
			codeTypeDeclaration.IsPartial   = ((typeDeclaration.Modifier & Modifiers.Partial) !=0);
			
			if (typeDeclaration.BaseTypes != null) {
				foreach (TypeReference typeRef in typeDeclaration.BaseTypes) {
					codeTypeDeclaration.BaseTypes.Add(ConvType(typeRef));
				}
			}
			
			typeDeclarations.Push(codeTypeDeclaration);
			typeDeclaration.AcceptChildren(this, data);
			typeDeclarations.Pop();
			
			if (typeDeclarations.Count > 0) {
				typeDeclarations.Peek().Members.Add(codeTypeDeclaration);
			} else {
				namespaceDeclarations.Peek().Types.Add(codeTypeDeclaration);
			}
			currentTypeDeclaration = oldTypeDeclaration;
			
			return null;
		}
		
		public override object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
//			CodeTypeDelegate codeTypeDelegate = new CodeTypeDelegate(delegateDeclaration.Name);
//			codeTypeDelegate.Parameters
//
//			((CodeNamespace)namespaceDeclarations.Peek()).Types.Add(codeTypeDelegate);
			return null;
		}
		
		public override object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			return null;
		}
		
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			for (int i = 0; i < fieldDeclaration.Fields.Count; ++i) {
				VariableDeclaration field = (VariableDeclaration)fieldDeclaration.Fields[i];
				
				if ((fieldDeclaration.Modifier & Modifiers.WithEvents) != 0) {
					//this.withEventsFields.Add(field);
				}
				TypeReference fieldType = fieldDeclaration.GetTypeForField(i);
				
				if (fieldType.IsNull) {
					fieldType = new TypeReference(typeDeclarations.Peek().Name);
				}
				
				CodeMemberField memberField = new CodeMemberField(ConvType(fieldType), field.Name);
				memberField.Attributes = ConvMemberAttributes(fieldDeclaration.Modifier);
				if (!field.Initializer.IsNull) {
					memberField.InitExpression = (CodeExpression)field.Initializer.AcceptVisitor(this, data);
				}
				
				typeDeclarations.Peek().Members.Add(memberField);
			}
			
			return null;
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			CodeMemberMethod memberMethod = new CodeMemberMethod();
			memberMethod.Name = methodDeclaration.Name;
			memberMethod.Attributes = ConvMemberAttributes(methodDeclaration.Modifier);
			
			codeStack.Push(memberMethod.Statements);
			
			typeDeclarations.Peek().Members.Add(memberMethod);
			
			// Add Method Parameters
			foreach (ParameterDeclarationExpression parameter in methodDeclaration.Parameters)
			{
				memberMethod.Parameters.Add((CodeParameterDeclarationExpression)VisitParameterDeclarationExpression(parameter, data));
			}
			
			variables.Clear();
			methodDeclaration.Body.AcceptChildren(this, data);
			
			codeStack.Pop();
			
			return null;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			CodeMemberMethod memberMethod = new CodeConstructor();
			
			codeStack.Push(memberMethod.Statements);
			typeDeclarations.Peek().Members.Add(memberMethod);
			constructorDeclaration.Body.AcceptChildren(this, data);
			codeStack.Pop();
			
			return null;
		}
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			blockStatement.AcceptChildren(this, data);
			return null;
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			object exp = expressionStatement.Expression.AcceptVisitor(this, data);
			if (exp is CodeExpression) {
				AddStmt(new CodeExpressionStatement((CodeExpression)exp));
			}
			return exp;
		}
		
		public override object VisitLocalVariableDeclaration(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			CodeVariableDeclarationStatement declStmt = null;
			
			for (int i = 0; i < localVariableDeclaration.Variables.Count; ++i) {
				CodeTypeReference type = ConvType(localVariableDeclaration.GetTypeForVariable(i) ?? new TypeReference("object"));
				VariableDeclaration var = (VariableDeclaration)localVariableDeclaration.Variables[i];
				if (!var.Initializer.IsNull) {
					declStmt = new CodeVariableDeclarationStatement(type,
					                                                var.Name,
					                                                (CodeExpression)((INode)var.Initializer).AcceptVisitor(this, data));
				} else {
					declStmt = new CodeVariableDeclarationStatement(type,
					                                                var.Name);
				}
				variables.Add(declStmt);
				AddStmt(declStmt);
			}
			
			return declStmt;
		}
		
		public override object VisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			CodeSnippetStatement emptyStmt = new CodeSnippetStatement();
			
			AddStmt(emptyStmt);
			
			return emptyStmt;
		}
		
		public override object VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			CodeMethodReturnStatement returnStmt;
			if (returnStatement.Expression.IsNull)
				returnStmt = new CodeMethodReturnStatement();
			else
				returnStmt = new CodeMethodReturnStatement((CodeExpression)returnStatement.Expression.AcceptVisitor(this,data));
			
			AddStmt(returnStmt);
			
			return returnStmt;
		}
		
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			CodeConditionStatement ifStmt = new CodeConditionStatement();
			
			ifStmt.Condition = (CodeExpression)ifElseStatement.Condition.AcceptVisitor(this, data);
			
			codeStack.Push(ifStmt.TrueStatements);
			foreach (Statement stmt in ifElseStatement.TrueStatement) {
				if (stmt is BlockStatement) {
					stmt.AcceptChildren(this, data);
				} else {
					stmt.AcceptVisitor(this, data);
				}
			}
			codeStack.Pop();
			
			codeStack.Push(ifStmt.FalseStatements);
			foreach (Statement stmt in ifElseStatement.FalseStatement) {
				if (stmt is BlockStatement) {
					stmt.AcceptChildren(this, data);
				} else {
					stmt.AcceptVisitor(this, data);
				}
			}
			codeStack.Pop();
			
			AddStmt(ifStmt);
			
			return ifStmt;
		}
		
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			CodeIterationStatement forLoop = new CodeIterationStatement();
			
			if (forStatement.Initializers.Count > 0)  {
				if (forStatement.Initializers.Count > 1) {
					throw new NotSupportedException("CodeDom does not support Multiple For-Loop Initializer Statements");
				}
				
				foreach (object o in forStatement.Initializers) {
					if (o is Expression) {
						forLoop.InitStatement = new CodeExpressionStatement((CodeExpression)((Expression)o).AcceptVisitor(this,data));
					}
					if (o is Statement) {
						codeStack.Push(NullStmtCollection);
						forLoop.InitStatement = (CodeStatement)((Statement)o).AcceptVisitor(this, data);
						codeStack.Pop();
					}
				}
			}
			
			if (forStatement.Condition == null) {
				forLoop.TestExpression = new CodePrimitiveExpression(true);
			} else {
				forLoop.TestExpression = (CodeExpression)forStatement.Condition.AcceptVisitor(this, data);
			}
			
			codeStack.Push(forLoop.Statements);
			forStatement.EmbeddedStatement.AcceptVisitor(this, data);
			codeStack.Pop();
			
			if (forStatement.Iterator.Count > 0) {
				if (forStatement.Initializers.Count > 1) {
					throw new NotSupportedException("CodeDom does not support Multiple For-Loop Iterator Statements");
				}
				
				foreach (Statement stmt in forStatement.Iterator) {
					forLoop.IncrementStatement = (CodeStatement)stmt.AcceptVisitor(this, data);
				}
			}
			
			AddStmt(forLoop);
			
			return forLoop;
		}
		
		public override object VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			System.CodeDom.CodeLabeledStatement labelStmt = new CodeLabeledStatement(labelStatement.Label,(CodeStatement)labelStatement.AcceptVisitor(this, data));
			
			// Add Statement to Current Statement Collection
			AddStmt(labelStmt);
			
			return labelStmt;
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			System.CodeDom.CodeGotoStatement gotoStmt = new CodeGotoStatement(gotoStatement.Label);
			
			// Add Statement to Current Statement Collection
			AddStmt(gotoStmt);
			
			return gotoStmt;
		}
		
		public override object VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			throw new NotSupportedException("CodeDom does not support Switch Statement");
		}
		
		public override object VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			// add a try-catch-finally
			CodeTryCatchFinallyStatement tryStmt = new CodeTryCatchFinallyStatement();
			
			codeStack.Push(tryStmt.TryStatements);
			
			tryCatchStatement.StatementBlock.AcceptChildren(this, data);
			codeStack.Pop();
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				codeStack.Push(tryStmt.FinallyStatements);
				
				tryCatchStatement.FinallyBlock.AcceptChildren(this,data);
				codeStack.Pop();
			}
			
			foreach (CatchClause clause in tryCatchStatement.CatchClauses)
			{
				CodeCatchClause catchClause = new CodeCatchClause(clause.VariableName);
				catchClause.CatchExceptionType = new CodeTypeReference(clause.TypeReference.Type);
				tryStmt.CatchClauses.Add(catchClause);
				
				codeStack.Push(catchClause.Statements);
				
				clause.StatementBlock.AcceptChildren(this, data);
				codeStack.Pop();
			}
			
			// Add Statement to Current Statement Collection
			AddStmt(tryStmt);
			
			return tryStmt;
		}
		
		public override object VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			CodeThrowExceptionStatement throwStmt = new CodeThrowExceptionStatement((CodeExpression)throwStatement.Expression.AcceptVisitor(this, data));
			
			// Add Statement to Current Statement Collection
			AddStmt(throwStmt);
			
			return throwStmt;
		}
		
		public override object VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			throw new NotSupportedException("CodeDom does not support Fixed Statement");
		}
		
		#region Expressions
		public override object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			return new CodePrimitiveExpression(primitiveExpression.Value);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			CodeBinaryOperatorType op = CodeBinaryOperatorType.Add;
			switch (binaryOperatorExpression.Op) {
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
				case BinaryOperatorType.DivideInteger:
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
					//case BinaryOperatorType.ValueEquality:
					//	op = CodeBinaryOperatorType.ValueEquality;
					//	break;
				case BinaryOperatorType.ShiftLeft:
				case BinaryOperatorType.ShiftRight:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.Multiply;
					break;
				case BinaryOperatorType.ReferenceEquality:
					op = CodeBinaryOperatorType.IdentityEquality;
					break;
				case BinaryOperatorType.ReferenceInequality:
					op = CodeBinaryOperatorType.IdentityInequality;
					break;
					
				case BinaryOperatorType.ExclusiveOr:
					// TODO ExclusiveOr
					op = CodeBinaryOperatorType.BitwiseAnd;
					break;
			}
			return new CodeBinaryOperatorExpression((CodeExpression)binaryOperatorExpression.Left.AcceptVisitor(this, data),
			                                        op,
			                                        (CodeExpression)binaryOperatorExpression.Right.AcceptVisitor(this, data));
		}
		
		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			return parenthesizedExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			Expression     target     = invocationExpression.TargetObject;
			CodeExpression targetExpr;
			string         methodName = null;
			if (target == null) {
				targetExpr = new CodeThisReferenceExpression();
			} else if (target is FieldReferenceExpression) {
				FieldReferenceExpression fRef = (FieldReferenceExpression)target;
				targetExpr = null;
				if (fRef.TargetObject is FieldReferenceExpression) {
					if (IsPossibleTypeReference((FieldReferenceExpression)fRef.TargetObject)) {
						targetExpr = ConvertToTypeReference((FieldReferenceExpression)fRef.TargetObject);
					}
				}
				if (targetExpr == null)
					targetExpr = (CodeExpression)fRef.TargetObject.AcceptVisitor(this, data);
				
				methodName = fRef.FieldName;
				// HACK for : Microsoft.VisualBasic.ChrW(NUMBER)
				if (methodName == "ChrW") {
					return new CodeCastExpression("System.Char", GetExpressionList(invocationExpression.Arguments)[0]);
				}
			} else if (target is IdentifierExpression) {
				targetExpr = new CodeThisReferenceExpression();
				methodName = ((IdentifierExpression)target).Identifier;
			} else {
				targetExpr = (CodeExpression)target.AcceptVisitor(this, data);
			}
			return new CodeMethodInvokeExpression(targetExpr, methodName, GetExpressionList(invocationExpression.Arguments));
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			if (!IsLocalVariable(identifierExpression.Identifier) && IsField(identifierExpression.Identifier)) {
				return new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), identifierExpression.Identifier);
			}
			return new CodeVariableReferenceExpression(identifierExpression.Identifier);
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			CodeExpression var;
			
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Minus:
					if (unaryOperatorExpression.Expression is PrimitiveExpression) {
						PrimitiveExpression expression = (PrimitiveExpression)unaryOperatorExpression.Expression;
						if (expression.Value is int) {
							return new CodePrimitiveExpression(- (int)expression.Value);
						}
						if (expression.Value is System.UInt32 || expression.Value is System.UInt16) {
							return new CodePrimitiveExpression(Int32.Parse("-" + expression.StringValue));
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
		
		void AddEventHandler(Expression eventExpr, Expression handler, object data)
		{
			methodReference = true;
			CodeExpression methodInvoker = (CodeExpression)handler.AcceptVisitor(this, data);
			methodReference = false;
			if (!(methodInvoker is CodeObjectCreateExpression)) {
				// we need to create an event handler here
				methodInvoker = new CodeObjectCreateExpression(new CodeTypeReference("System.EventHandler"), methodInvoker);
			}
			
			if (eventExpr is IdentifierExpression) {
				AddStmt(new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeThisReferenceExpression(), ((IdentifierExpression)eventExpr).Identifier),
				                                     methodInvoker));
			} else {
				FieldReferenceExpression fr = (FieldReferenceExpression)eventExpr;
				AddStmt(new CodeAttachEventStatement(new CodeEventReferenceExpression((CodeExpression)fr.TargetObject.AcceptVisitor(this, data), fr.FieldName),
				                                     methodInvoker));
			}
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			if (assignmentExpression.Op == AssignmentOperatorType.Add) {
				AddEventHandler(assignmentExpression.Left, assignmentExpression.Right, data);
			} else {
				if (assignmentExpression.Left is IdentifierExpression) {
					AddStmt(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				} else {
					AddStmt(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				}
			}
			return null;
		}
		
		public override object VisitAddHandlerStatement(AddHandlerStatement addHandlerStatement, object data)
		{
			AddEventHandler(addHandlerStatement.EventExpression, addHandlerStatement.HandlerExpression, data);
			return null;
		}
		
		public override object VisitAddressOfExpression(AddressOfExpression addressOfExpression, object data)
		{
			return addressOfExpression.Expression.AcceptVisitor(this, data);
		}
		
		public override object VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			return new CodeTypeOfExpression(ConvType(typeOfExpression.TypeReference));
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			CodeTypeReference typeRef = ConvType(castExpression.CastTo);
			return new CodeCastExpression(typeRef, (CodeExpression)castExpression.Expression.AcceptVisitor(this, data));
		}
		
		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			return new CodeIndexerExpression((CodeExpression)indexerExpression.TargetObject.AcceptVisitor(this, data), GetExpressionList(indexerExpression.Indexes));
		}
		
		public override object VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			return new CodeThisReferenceExpression();
		}
		
		public override object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			return new CodeBaseReferenceExpression();
		}
		
		public override object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			if (arrayCreateExpression.ArrayInitializer == null) {
				return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType),
				                                     arrayCreateExpression.Arguments[0].AcceptVisitor(this, data) as CodeExpression);
			}
			return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType),
			                                     GetExpressionList(arrayCreateExpression.ArrayInitializer.CreateExpressions));
		}
		
		public override object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			return new CodeObjectCreateExpression(ConvType(objectCreateExpression.CreateType),
			                                      objectCreateExpression.Parameters == null ? null : GetExpressionList(objectCreateExpression.Parameters));
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			return new CodeParameterDeclarationExpression(ConvType(parameterDeclarationExpression.TypeReference), parameterDeclarationExpression.ParameterName);
		}
		
		bool IsField(string type, string fieldName)
		{
			bool isField = environmentInformationProvider.HasField(type, fieldName);
			
			if (!isField) {
				int idx = type.LastIndexOf('.');
				if (idx >= 0) {
					type = type.Substring(0, idx) + "+" + type.Substring(idx + 1);
					isField = IsField(type, fieldName);
				}
			}
			
			return isField;
		}
		
		bool IsFieldReferenceExpression(FieldReferenceExpression fieldReferenceExpression)
		{
			if (fieldReferenceExpression.TargetObject is ThisReferenceExpression
			    || fieldReferenceExpression.TargetObject is BaseReferenceExpression)
			{
				//field detection for fields\props inherited from base classes
				return IsField(fieldReferenceExpression.FieldName);
			}
			return false;
		}
		
		public override object VisitFieldReferenceExpression(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			if (methodReference) {
				methodReference = false;
				return new CodeMethodReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data), fieldReferenceExpression.FieldName);
			}
			if (IsFieldReferenceExpression(fieldReferenceExpression)) {
				return new CodeFieldReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data),
				                                        fieldReferenceExpression.FieldName);
			} else {
				if (fieldReferenceExpression.TargetObject is FieldReferenceExpression) {
					if (IsPossibleTypeReference((FieldReferenceExpression)fieldReferenceExpression.TargetObject)) {
						CodeTypeReferenceExpression typeRef = ConvertToTypeReference((FieldReferenceExpression)fieldReferenceExpression.TargetObject);
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
		#endregion
		
		#endregion
		bool IsPossibleTypeReference(FieldReferenceExpression fieldReferenceExpression)
		{
			while (fieldReferenceExpression.TargetObject is FieldReferenceExpression) {
				fieldReferenceExpression = (FieldReferenceExpression)fieldReferenceExpression.TargetObject;
			}
			IdentifierExpression identifier = fieldReferenceExpression.TargetObject as IdentifierExpression;
			if (identifier != null)
				return !IsField(identifier.Identifier) && !IsLocalVariable(identifier.Identifier);
			TypeReferenceExpression tre = fieldReferenceExpression.TargetObject as TypeReferenceExpression;
			if (tre != null)
				return true;
			return false;
		}
		
		bool IsLocalVariable(string identifier)
		{
			foreach (CodeVariableDeclarationStatement variable in variables) {
				if (variable.Name == identifier)
					return true;
			}
			return false;
		}
		
		bool IsField(string identifier)
		{
			if (currentTypeDeclaration == null) // e.g. in unit tests
				return false;
			foreach (INode node in currentTypeDeclaration.Children) {
				if (node is FieldDeclaration) {
					FieldDeclaration fd = (FieldDeclaration)node;
					if (fd.GetVariableDeclaration(identifier) != null) {
						return true;
					}
				}
			}
			//field detection for fields\props inherited from base classes
			if (currentTypeDeclaration.BaseTypes.Count > 0) {
				return IsField(currentTypeDeclaration.BaseTypes[0].ToString(), identifier);
			}
			return false;
		}
		
		static CodeTypeReferenceExpression ConvertToTypeReference(FieldReferenceExpression fieldReferenceExpression)
		{
			StringBuilder type = new StringBuilder("");
			
			while (fieldReferenceExpression.TargetObject is FieldReferenceExpression) {
				type.Insert(0,'.');
				type.Insert(1,fieldReferenceExpression.FieldName.ToCharArray());
				fieldReferenceExpression = (FieldReferenceExpression)fieldReferenceExpression.TargetObject;
			}
			
			type.Insert(0,'.');
			type.Insert(1,fieldReferenceExpression.FieldName.ToCharArray());
			
			if (fieldReferenceExpression.TargetObject is IdentifierExpression) {
				type.Insert(0, ((IdentifierExpression)fieldReferenceExpression.TargetObject).Identifier.ToCharArray());
				string oldType = type.ToString();
				int idx = oldType.LastIndexOf('.');
				while (idx > 0) {
					if (Type.GetType(type.ToString()) != null) {
						break;
					}
					string stype = type.ToString().Substring(idx + 1);
					type = new StringBuilder(type.ToString().Substring(0, idx));
					type.Append("+");
					type.Append(stype);
					idx = type.ToString().LastIndexOf('.');
				}
				if (Type.GetType(type.ToString()) == null) {
					type = new StringBuilder(oldType);
				}
				return new CodeTypeReferenceExpression(type.ToString());
			} else if (fieldReferenceExpression.TargetObject is TypeReferenceExpression) {
				type.Insert(0, ((TypeReferenceExpression)fieldReferenceExpression.TargetObject).TypeReference.SystemType);
				return new CodeTypeReferenceExpression(type.ToString());
			} else {
				return null;
			}
		}
		
		CodeExpression[] GetExpressionList(IList expressionList)
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
		
		
	}
}
