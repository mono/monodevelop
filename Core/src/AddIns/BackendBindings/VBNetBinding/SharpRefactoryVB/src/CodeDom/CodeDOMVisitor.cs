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

using ICSharpCode.SharpRefactory.Parser.AST.VB;

namespace ICSharpCode.SharpRefactory.Parser.VB
{
	public class CodeDOMVisitor : AbstractASTVisitor
	{
		Stack namespaceDeclarations = new Stack();
		Stack typeDeclarations     = new Stack();
		CodeMemberMethod currentMethod = null;
		TypeDeclaration currentTypeDeclaration;
		
		public CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
		public ArrayList namespaces = new ArrayList();
		
		static string[,] typeConversionList = new string[,] {
			{"System.Object",	"OBJECT"},
			{"System.Boolean",	"BOOLEAN"},
			{"System.Byte",		"BYTE"},
			{"System.Char",		"CHAR"},
			{"System.Int16",	"SHORT"},
			{"System.Int32",	"INTEGER"},
			{"System.Int64",	"LONG"},
			{"System.Single",	"SINGLE"},
			{"System.Double",	"DOUBLE"},
			{"System.Decimal",	"DECIMAL"},
			{"System.String",	"STRING"},
			{"System.DateTime",	"DATE"}
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
			string upperType = type.ToUpper();
			if (typeConversionTable[upperType] != null) {
				return typeConversionTable[upperType].ToString();
			}
			return type;
		}
		// FIXME: map all modifiers correctly
		MemberAttributes ConvMemberAttributes(Modifier modifier) 
		{
			MemberAttributes attr = (MemberAttributes)0;
			
			if ((modifier & Modifier.Private) != 0) {
				attr |=  MemberAttributes.Private;
			}
			if ((modifier & Modifier.Public) != 0) {
				attr |=  MemberAttributes.Public;
			}
			if ((modifier & Modifier.Protected) != 0) {
				attr |=  MemberAttributes.Family;
			}
			if ((modifier & Modifier.Friend) != 0) {
				attr |=  MemberAttributes.Assembly;
			}
			if ((modifier & Modifier.Static) != 0) {
				attr |=  MemberAttributes.Static;
			}
			return attr;
		}
		
#region ICSharpCode.SharpRefactory.Parser.IASTVisitor interface implementation
		public override object Visit(INode node, object data)
		{
			return null;
		}
		
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
		
		public override object Visit(ImportsDeclaration importsDeclaration, object data)
		{
			((CodeNamespace)namespaceDeclarations.Peek()).Imports.Add(new CodeNamespaceImport(importsDeclaration.Namespace));
			return null;
		}
		
		public override object Visit(ImportsAliasDeclaration importsAliasDeclaration, object data)
		{
			return null;
		}
		
		public override object Visit(AttributeSection attributeSection, object data)
		{
			return null;
		}
		
		// TODO: OptionCompareDeclaration, OptionExplicitDeclaration, OptionStrictDeclaration
		
		public override object Visit(TypeDeclaration typeDeclaration, object data)
		{
			// skip nested types.
			if (currentTypeDeclaration != null) {
				return data;
			}
			if (typeDeclaration.Type == Types.Enum) {
				return data;
			}
			this.currentTypeDeclaration = typeDeclaration;
			CodeTypeDeclaration codeTypeDeclaration = new CodeTypeDeclaration(typeDeclaration.Name);
			codeTypeDeclaration.IsClass     = typeDeclaration.Type == Types.Class;
			codeTypeDeclaration.IsEnum      = typeDeclaration.Type == Types.Enum;
			codeTypeDeclaration.IsInterface = typeDeclaration.Type == Types.Interface;
			codeTypeDeclaration.IsStruct    = typeDeclaration.Type == Types.Structure;
			
			if (typeDeclaration.BaseType != null) {
				codeTypeDeclaration.BaseTypes.Add(new CodeTypeReference(typeDeclaration.BaseType));
			}
			
			if (typeDeclaration.BaseInterfaces != null) {
				foreach (object o in typeDeclaration.BaseInterfaces) {
					codeTypeDeclaration.BaseTypes.Add(new CodeTypeReference(o.ToString()));
				}
			}
			
			typeDeclarations.Push(codeTypeDeclaration);
			typeDeclaration.AcceptChildren(this,data);
//			((INode)typeDeclaration.Children[0]).(this, data);
			
			typeDeclarations.Pop();
			
			((CodeNamespace)namespaceDeclarations.Peek()).Types.Add(codeTypeDeclaration);
			this.currentTypeDeclaration = null;
			return null;
		}
		
		//FIXME
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
			for (int i = 0; i < fieldDeclaration.Fields.Count; ++i) {
				VariableDeclaration field = (VariableDeclaration)fieldDeclaration.Fields[i];
				if (field.Type != null) {
					CodeMemberField memberField = new CodeMemberField(new CodeTypeReference(ConvType(field.Type.Type)), field.Name);
					memberField.Attributes = ConvMemberAttributes(fieldDeclaration.Modifier);
					if (field.Initializer != null) {
						memberField.InitExpression =  (CodeExpression)((INode)field.Initializer).AcceptVisitor(this, data);
					}
					((CodeTypeDeclaration)typeDeclarations.Peek()).Members.Add(memberField);
				}
			}
			
			return null;
		}
		
		public override object Visit(MethodDeclaration methodDeclaration, object data)
		{
			CodeMemberMethod memberMethod = new CodeMemberMethod();
			memberMethod.Name = methodDeclaration.Name;
			currentMethod = memberMethod;
			((CodeTypeDeclaration)typeDeclarations.Peek()).Members.Add(memberMethod);
			if (memberMethod.Name.ToUpper() == "INITIALIZECOMPONENT") {
				methodDeclaration.Body.AcceptChildren(this, data);
			}
			currentMethod = null;
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
		
		// TODO: HandlesClause, ImplementsClause
		
		public override object Visit(ConstructorDeclaration constructorDeclaration, object data)
		{
			CodeConstructor memberMethod = new CodeConstructor();
			// HACK: fake public constructor
			memberMethod.Attributes = MemberAttributes.Public;
			currentMethod = memberMethod;
			((CodeTypeDeclaration)typeDeclarations.Peek()).Members.Add(memberMethod);
			constructorDeclaration.Body.AcceptChildren(this, data);
			currentMethod = null;
			return null;
		}
		
		public override object Visit(BlockStatement blockStatement, object data)
		{
			blockStatement.AcceptChildren(this, data);
			return null;
		}
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			if (statementExpression.Expression == null) {
				//Console.WriteLine("Warning: Got empty statement expression!!!");
				return null;
			}
			CodeExpression expr = (CodeExpression)statementExpression.Expression.AcceptVisitor(this, data);
			if (expr == null) {
				//if (!(statementExpression.Expression is AssignmentExpression)) {
					//Console.WriteLine("NULL EXPRESSION : " + statementExpression.Expression);
				//}
			} else {
				currentMethod.Statements.Add(new CodeExpressionStatement(expr));
			}
			
			return null;
		}
		
		public string Convert(TypeReference typeRef)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(ConvType(typeRef.Type));
			
			if (typeRef.RankSpecifier != null) {
				for (int i = 0; i < typeRef.RankSpecifier.Count; ++i) {
					builder.Append('[');
					for (int j = 1; j < (int)typeRef.RankSpecifier[i]; ++j) {
						builder.Append(',');
					}
					builder.Append(']');
				}
			}
			
			return builder.ToString();
		}
		
		public override object Visit(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			foreach (VariableDeclaration var in localVariableDeclaration.Variables) {
				CodeTypeReference type = new CodeTypeReference(Convert(var.Type));
				if (var.Initializer != null) {
					currentMethod.Statements.Add(new CodeVariableDeclarationStatement(type,
					                                                                  var.Name,
					                                                                  (CodeExpression)((INode)var.Initializer).AcceptVisitor(this, data)));
				} else {
					currentMethod.Statements.Add(new CodeVariableDeclarationStatement(type,
					                                                                  var.Name));
				}
			}
			return null;
		}
		
		public override object Visit(ReturnStatement returnStatement, object data)
		{
			return null;
		}
		
		public override object Visit(IfStatement ifStatement, object data)
		{
			return null;
		}
		
		public override object Visit(WhileStatement doWhileStatement, object data)
		{
			return null;
		}
		
		public override object Visit(DoLoopStatement doWhileStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ForStatement forStatement, object data)
		{
			return null;
		}
		
		public override object Visit(LabelStatement labelStatement, object data)
		{
			return null;
		}
		
		public override object Visit(GoToStatement gotoStatement, object data)
		{
			return null;
		}
		
		public override object Visit(SelectStatement switchStatement, object data)
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
		
		public override object Visit(TryCatchStatement tryCatchStatement, object data)
		{
			return null;
		}
		
		public override object Visit(AddHandlerStatement addHandlerStatement, object data)
		{
			CodeExpression methodInvoker = (CodeExpression)addHandlerStatement.HandlerExpression.AcceptVisitor(this, null);
			
			if (addHandlerStatement.EventExpression is IdentifierExpression) {
				currentMethod.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeThisReferenceExpression(), ((IdentifierExpression)addHandlerStatement.EventExpression).Identifier),
				                                                          methodInvoker));
			} else {
				FieldReferenceOrInvocationExpression fr = (FieldReferenceOrInvocationExpression)addHandlerStatement.EventExpression;
				currentMethod.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression((CodeExpression)fr.TargetObject.AcceptVisitor(this, data), fr.FieldName),
				                                                          methodInvoker));
			}
			return null;
		}
		
		public override object Visit(RemoveHandlerStatement removeHandlerStatement, object data)
		{
			return null;
		}
		
		public override object Visit(EndStatement endStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ExitStatement exitStatement, object data)
		{
			return null;
		}
		
		public override object Visit(StopStatement stopStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ResumeStatement resumeStatement, object data)
		{
			return null;
		}
		
		public override object Visit(EraseStatement eraseStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ErrorStatement errorStatement, object data)
		{
			return null;
		}
		
		public override object Visit(OnErrorStatement onErrorStatement, object data)
		{
			return null;
		}
		
		public override object Visit(RaiseEventStatement raiseEventStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ReDimStatement reDimStatement, object data)
		{
			return null;
		}
		
		public override object Visit(ThrowStatement throwStatement, object data)
		{
			return new CodeThrowExceptionStatement((CodeExpression)throwStatement.ThrowExpression.AcceptVisitor(this, data));
		}
		
		public override object Visit(PrimitiveExpression expression, object data)
		{
//			if (expression.Value is string) {
//				return new CodePrimitiveExpression(expression.Value);
//			} else if (expression.Value is char) {
//				return new CodePrimitiveExpression((char)expression.Value);
//			} else if (expression.Value == null) {
//				return new CodePrimitiveExpression(null);
//			}
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
				case BinaryOperatorType.Concat:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.Add;
					break;
				case BinaryOperatorType.BooleanAnd:
					op = CodeBinaryOperatorType.BooleanAnd;
					break;
				case BinaryOperatorType.BooleanOr:
					op = CodeBinaryOperatorType.BooleanOr;
					break;
				case BinaryOperatorType.Divide:
					op = CodeBinaryOperatorType.Divide;
					break;
				case BinaryOperatorType.DivideInteger:
					// CodeDOM suxx
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
				case BinaryOperatorType.Like:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.IdentityEquality;
					break;
				case BinaryOperatorType.ExclusiveOr:
					// CodeDOM suxx
					op = CodeBinaryOperatorType.BitwiseOr;
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
			} else if (target is FieldReferenceOrInvocationExpression) {
				FieldReferenceOrInvocationExpression fRef = (FieldReferenceOrInvocationExpression)target;
				targetExpr = (CodeExpression)fRef.TargetObject.AcceptVisitor(this, data);
				if (fRef.TargetObject is FieldReferenceOrInvocationExpression) {
					FieldReferenceOrInvocationExpression fRef2 = (FieldReferenceOrInvocationExpression)fRef.TargetObject;
					if (fRef2.FieldName != null && Char.IsUpper(fRef2.FieldName[0])) {
						// an exception is thrown if it doesn't end in an indentifier exception
						// for example for : this.MyObject.MyMethod() leads to an exception, which 
						// is correct in this case ... I know this is really HACKY :)
						try {
							CodeExpression tExpr = ConvertToIdentifier(fRef2);
							if (tExpr != null) {
								targetExpr = tExpr;
							}
						} catch (Exception) {}
					}
				}
				methodName = fRef.FieldName;
				// HACK for : Microsoft.VisualBasic.ChrW(NUMBER)
				//Console.WriteLine(methodName);
				if (methodName == "ChrW") {
					//Console.WriteLine("Return CAST EXPRESSION" + GetExpressionList(invocationExpression.Parameters)[0]);
					
					return new CodeCastExpression("System.Char", GetExpressionList(invocationExpression.Parameters)[0]);
				}
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
		
		public override object Visit(GetTypeExpression getTypeExpression, object data)
		{
			// TODO
			return null;
		}
		
		public override object Visit(TypeReferenceExpression typeReferenceExpression, object data)
		{
			return null;
		}
		
		public override object Visit(ClassReferenceExpression classReferenceExpression, object data)
		{
			//TODO
			return null;
		}
		
		public override object Visit(LoopControlVariableExpression loopControlVariableExpression, object data)
		{
			//TODO
			return null;
		}
		
		public override object Visit(NamedArgumentExpression namedParameterExpression, object data)
		{
			//TODO
			return null;
		}
		
		public override object Visit(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Minus:
					if (unaryOperatorExpression.Expression is PrimitiveExpression) {
						PrimitiveExpression expression = (PrimitiveExpression)unaryOperatorExpression.Expression;
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
				
				if (assignmentExpression.Left is IdentifierExpression) {
					currentMethod.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeThisReferenceExpression(), ((IdentifierExpression)assignmentExpression.Left).Identifier),
					                                                          methodInvoker));
				} else {
					FieldReferenceOrInvocationExpression fr = (FieldReferenceOrInvocationExpression)assignmentExpression.Left;
					
					currentMethod.Statements.Add(new CodeAttachEventStatement(new CodeEventReferenceExpression((CodeExpression)fr.TargetObject.AcceptVisitor(this, data), fr.FieldName),
					                                                          methodInvoker));
				}
			} else {
				if (assignmentExpression.Left is IdentifierExpression) {
					currentMethod.Statements.Add(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
				} else {
					currentMethod.Statements.Add(new CodeAssignStatement((CodeExpression)assignmentExpression.Left.AcceptVisitor(this, null), (CodeExpression)assignmentExpression.Right.AcceptVisitor(this, null)));
					
				}
			}
			return null;
		}
		
		public override object Visit(AddressOfExpression addressOfExpression, object data)
		{
			if (addressOfExpression.Procedure is FieldReferenceOrInvocationExpression) {
				FieldReferenceOrInvocationExpression fr = (FieldReferenceOrInvocationExpression)addressOfExpression.Procedure;
				return new CodeObjectCreateExpression("System.EventHandler", new CodeExpression[] {
					new CodeMethodReferenceExpression((CodeExpression)fr.TargetObject.AcceptVisitor(this, null),
					                                        fr.FieldName)
				});
			}
			return addressOfExpression.Procedure.AcceptVisitor(this, null);
		}
		
		public override object Visit(TypeOfExpression typeOfExpression, object data)
		{
			return new CodeTypeOfExpression(ConvType(typeOfExpression.Type.Type));
		}
		
		public override object Visit(CastExpression castExpression, object data)
		{
			string typeRef = ConvType(castExpression.CastTo.Type);
			return new CodeCastExpression(typeRef, (CodeExpression)castExpression.Expression.AcceptVisitor(this, data));
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
				return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType.Type),
				                                     0);
			}
			return new CodeArrayCreateExpression(ConvType(arrayCreateExpression.CreateType.Type),
			                                     GetExpressionList(arrayCreateExpression.ArrayInitializer.CreateExpressions));
		}
		
		public override object Visit(ObjectCreateExpression objectCreateExpression, object data)
		{
			return new CodeObjectCreateExpression(ConvType(objectCreateExpression.CreateType.Type),
			                                      GetExpressionList(objectCreateExpression.Parameters));
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
			if (t == null) {
				asm = typeof(System.Drawing.Point).Assembly;
				t = asm.GetType(type);
			}
			
			//necessary?
//			if (t == null) {
//				asm = typeof(System.Windows.Forms.Control).Assembly;
//				t = asm.GetType(type);
//			}
			
			if (t == null) {
				asm = typeof(System.String).Assembly;
				t = asm.GetType(type);
			}
			
			bool isField = t != null && (t.IsEnum || t.GetField(fieldName) != null);
			if (!isField) {
				int idx = type.LastIndexOf('.');
				if (idx >= 0) {
					type = type.Substring(0, idx) + "+" + type.Substring(idx + 1);
					isField = IsField(type, fieldName);
				}
			}
			return isField;
		}
		
		bool IsFieldReferenceExpression(FieldReferenceOrInvocationExpression fieldReferenceExpression)
		{
			if (fieldReferenceExpression.TargetObject is ThisReferenceExpression) {
				foreach (object o in this.currentTypeDeclaration.Children) {
					if (o is FieldDeclaration) {
						FieldDeclaration fd = (FieldDeclaration)o;
						foreach (VariableDeclaration field in fd.Fields) {
							if (fieldReferenceExpression.FieldName.ToUpper() == field.Name.ToUpper()) {
								return true;
							}
						}
					}
				}
			}
			return false; //Char.IsLower(fieldReferenceExpression.FieldName[0]);
		}
		
		public override object Visit(FieldReferenceOrInvocationExpression fieldReferenceExpression, object data)
		{
			if (methodReference) {
				methodReference = false;
				return new CodeMethodReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data), fieldReferenceExpression.FieldName);
			}
			if (IsFieldReferenceExpression(fieldReferenceExpression)) {
				return new CodeFieldReferenceExpression((CodeExpression)fieldReferenceExpression.TargetObject.AcceptVisitor(this, data),
				                                        fieldReferenceExpression.FieldName);
			} else {
				if (fieldReferenceExpression.TargetObject is FieldReferenceOrInvocationExpression) {
					if (IsQualIdent((FieldReferenceOrInvocationExpression)fieldReferenceExpression.TargetObject)) {
						CodeTypeReferenceExpression typeRef = ConvertToIdentifier((FieldReferenceOrInvocationExpression)fieldReferenceExpression.TargetObject);
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
		
		public override object Visit(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			return null;
		}
#endregion
		
		bool IsQualIdent(FieldReferenceOrInvocationExpression fieldReferenceExpression)
		{
			while (fieldReferenceExpression.TargetObject is FieldReferenceOrInvocationExpression) {
				fieldReferenceExpression = (FieldReferenceOrInvocationExpression)fieldReferenceExpression.TargetObject;
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
		
		CodeTypeReferenceExpression ConvertToIdentifier(FieldReferenceOrInvocationExpression fieldReferenceExpression)
		{
			string type = String.Empty;
			
			while (fieldReferenceExpression.TargetObject is FieldReferenceOrInvocationExpression) {
				type = "."  + fieldReferenceExpression.FieldName + type;
				fieldReferenceExpression = (FieldReferenceOrInvocationExpression)fieldReferenceExpression.TargetObject;
			}
			
			type = "."  + fieldReferenceExpression.FieldName + type;
			
			if (fieldReferenceExpression.TargetObject is IdentifierExpression) {
				type = ((IdentifierExpression)fieldReferenceExpression.TargetObject).Identifier + type;
				string oldType = type;
				int idx = type.LastIndexOf('.');
				while (idx >= 0) {
					if (Type.GetType(type) != null) {
						break;
					}
					type = type.Substring(0, idx) + "+" + type.Substring(idx + 1);
					idx = type.LastIndexOf('.');
				}
				if (Type.GetType(type) == null) {
					type = oldType;
				}
				return new CodeTypeReferenceExpression(type);
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
				if (expressionList[i] != null) {
					list[i] = (CodeExpression)((Expression)expressionList[i]).AcceptVisitor(this, null);
				}
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
