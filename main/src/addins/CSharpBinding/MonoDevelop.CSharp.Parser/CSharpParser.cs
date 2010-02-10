/*
// 
// CSharpParser.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.CSharp;
using System.Text;
using Mono.TextEditor;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Parser
{
	public class CSharpParser
	{
		class ConversionVisitor : AbstractStructuralVisitor
		{
			MonoDevelop.CSharp.Dom.CompilationUnit unit = new MonoDevelop.CSharp.Dom.CompilationUnit ();
			
			public MonoDevelop.CSharp.Dom.CompilationUnit Unit {
				get {
					return unit;
				}
				set {
					unit = value;
				}
			}
			
			public LocationStorage LocationStorage  {
				get;
				private set;
			}
			
			public ConversionVisitor (LocationStorage locationStorage)
			{
				this.LocationStorage = locationStorage;
			}
			
			public static DomLocation Convert (Mono.CSharp.Location loc)
			{
				return new DomLocation (loc.Row - 1, loc.Column - 1);
			}
			public static FullTypeName ConvertToReturnType (FullNamedExpression typeName)
			{
				return new FullTypeName (typeName.ToString (), Convert (typeName.Location));
			}
			
			
			#region Global
			public override void Visit (ModuleCompiled mc)
			{
				base.Visit (mc);
			}

			public override void Visit (MemberCore member)
			{
				Console.WriteLine ("Unknown member:");
				Console.WriteLine (member.GetType () + "-> Member {0}", member.GetSignatureForError ());
			}
			
			Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration> ();
			
			public override void Visit (Class c)
			{
				TypeDeclaration newType = CreateTypeDeclaration (c);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Class;
				
				newType.AddChild (new CSharpTokenNode (Convert (c.BodyStartLocation)), AbstractCSharpNode.Roles.LBrace);
				
				typeStack.Push (newType);
				
				base.Visit (c);
				newType.AddChild (new CSharpTokenNode  (Convert (c.BodyEndLocation)), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
			}

			TypeDeclaration CreateTypeDeclaration (Mono.CSharp.TypeContainer tc)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				
				Identifier nameIdentifier = new Identifier () {
					Name = tc.Name
				};
				newType.AddChild (new CSharpTokenNode (Convert (tc.ContainerTypeTokenPosition)), TypeDeclaration.TypeKeyword);
				newType.AddChild (nameIdentifier, AbstractNode.Roles.Identifier);
				unit.AddChild (newType);
				return newType;
			}

			
			public override void Visit (Struct s)
			{
				TypeDeclaration newType = CreateTypeDeclaration (s);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Struct;
				typeStack.Push (newType);
				base.Visit (s);
				typeStack.Pop ();
			}
			
			public override void Visit (Interface i)
			{
				TypeDeclaration newType = CreateTypeDeclaration (i);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Interface;
				typeStack.Push (newType);
				base.Visit (i);
				typeStack.Pop ();
			}
			
			public override void Visit (Mono.CSharp.Delegate d)
			{
				DelegateDeclaration newDelegate = new DelegateDeclaration ();
				Identifier nameIdentifier = new Identifier () {
					Name = d.Name
				};
				
				newDelegate.AddChild (new CSharpTokenNode (Convert (d.ContainerTypeTokenPosition)), TypeDeclaration.TypeKeyword);
				newDelegate.AddChild (ConvertToReturnType (d.TypeName), AbstractNode.Roles.ReturnType);
				newDelegate.AddChild (nameIdentifier, AbstractNode.Roles.Identifier);
				
				ParanthesisLocation parenthesisLocation = LocationStorage.Get<ParanthesisLocation> (d);
				SemicolonLocation semicolonLocation = LocationStorage.Get<SemicolonLocation> (d);
				
				newDelegate.AddChild (new CSharpTokenNode (Convert (parenthesisLocation.OpenParenthesisLocation)), DelegateDeclaration.Roles.LPar);
				newDelegate.AddChild (new CSharpTokenNode (Convert (parenthesisLocation.CloseParenthesisLocation)), DelegateDeclaration.Roles.RPar);
				newDelegate.AddChild (new CSharpTokenNode (Convert (semicolonLocation.Location)), DelegateDeclaration.Roles.Semicolon);
				
				if (typeStack.Count > 0) {
					typeStack.Peek ().AddChild (newDelegate);
				} else {
					unit.AddChild (newDelegate);
				}
			}
			
			public override void Visit (Mono.CSharp.Enum e)
			{
				TypeDeclaration newType = CreateTypeDeclaration (e);
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Enum;
				typeStack.Push (newType);
				base.Visit (e);
				typeStack.Pop ();
			}
			
			#endregion
			
			#region Type members
			public override void Visit (FixedField f)
			{
			}
			
			
			HashSet<Field> visitedFields = new HashSet<Field> ();
			public override void Visit (Field f)
			{
				if (visitedFields.Contains (f))
					return;
				
				TypeDeclaration typeDeclaration = typeStack.Peek ();
				
				FieldDeclaration newField = new FieldDeclaration ();
				newField.AddChild (ConvertToReturnType (f.TypeName), AbstractNode.Roles.ReturnType);
				
				Field curField = f;
				while (curField != null) {
					visitedFields.Add (curField);
					VariableInitializer variable = new VariableInitializer ();
					
					Identifier fieldName = new Identifier () {
						Name = curField.MemberName.Name,
						Location = Convert (curField.MemberName.Location)
					};
					variable.AddChild (fieldName, AbstractNode.Roles.Identifier);
					newField.AddChild (variable, AbstractNode.Roles.Initializer);
					if (curField.ConnectedField != null) {
						CommaLocation location = LocationStorage.Get<CommaLocation> (curField);
				
						newField.AddChild (new CSharpTokenNode (Convert (location.Location)), AbstractNode.Roles.Comma);
					}
					curField = curField.ConnectedField;
				}
				typeDeclaration.AddChild (newField, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Operator o)
			{
				OperatorDeclaration newOperator = new OperatorDeclaration ();
				newOperator.OperatorType = (OperatorType)o.OperatorType;
				
				newOperator.AddChild (ConvertToReturnType (o.TypeName), AbstractNode.Roles.ReturnType);
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (o);
				newOperator.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), MethodDeclaration.Roles.LPar);
				newOperator.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), MethodDeclaration.Roles.RPar);
				
				if (o.Block == null) {
					
				}
				
				typeStack.Peek ().AddChild (newOperator, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Indexer indexer)
			{
				IndexerDeclaration newIndexer = new IndexerDeclaration ();
				newIndexer.AddChild (ConvertToReturnType (indexer.TypeName), AbstractNode.Roles.ReturnType);
				
				BracketLocation location = LocationStorage.Get<BracketLocation> (indexer);
				newIndexer.AddChild (new CSharpTokenNode (Convert (location.OpenBracketLocation)), IndexerDeclaration.Roles.LBracket);
				newIndexer.AddChild (new CSharpTokenNode (Convert (location.CloseBracketLocation)), IndexerDeclaration.Roles.RBracket);
				
				newIndexer.AddChild (new CSharpTokenNode (Convert (indexer.BodyStartLocation)), IndexerDeclaration.Roles.LBrace);
				newIndexer.AddChild (new CSharpTokenNode (Convert (indexer.BodyEndLocation)), IndexerDeclaration.Roles.RBrace);
				
				if (indexer.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					getAccessor.Location = Convert (indexer.Get.Location);
					newIndexer.AddChild (getAccessor, IndexerDeclaration.PropertyGetRole);
				}
				
				if (indexer.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					getAccessor.Location = Convert (indexer.Set.Location);
					newIndexer.AddChild (getAccessor, IndexerDeclaration.PropertySetRole);
				}
				
				typeStack.Peek ().AddChild (newIndexer, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Method m)
			{
				MethodDeclaration newMethod = new MethodDeclaration ();
				
				Identifier nameIdentifier = new Identifier () {
					Name = m.Name
				};
				newMethod.AddChild (ConvertToReturnType (m.TypeName), AbstractNode.Roles.ReturnType);
				newMethod.AddChild (nameIdentifier, AbstractNode.Roles.Identifier);
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (m);
				newMethod.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), MethodDeclaration.Roles.LPar);
				newMethod.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), MethodDeclaration.Roles.RPar);
				
				if (m.Block == null) {
					
				}
				
				typeStack.Peek ().AddChild (newMethod, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Property p)
			{
				PropertyDeclaration newProperty = new PropertyDeclaration ();
				newProperty.AddChild (ConvertToReturnType (p.TypeName), AbstractNode.Roles.ReturnType);
				
				newProperty.AddChild (new CSharpTokenNode (Convert (p.BodyStartLocation)), MethodDeclaration.Roles.LBrace);
				newProperty.AddChild (new CSharpTokenNode (Convert (p.BodyEndLocation)), MethodDeclaration.Roles.RBrace);
				
				if (p.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					getAccessor.Location = Convert (p.Get.Location);
					newProperty.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
				}
				
				if (p.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					getAccessor.Location = Convert (p.Set.Location);
					newProperty.AddChild (getAccessor, PropertyDeclaration.PropertySetRole);
				}
				
				typeStack.Peek ().AddChild (newProperty, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Constructor c)
			{
				ConstructorDeclaration newConstructor = new ConstructorDeclaration ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (c);
				newConstructor.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), MethodDeclaration.Roles.LPar);
				newConstructor.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), MethodDeclaration.Roles.RPar);
				
				typeStack.Peek ().AddChild (newConstructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Destructor d)
			{
				DestructorDeclaration newDestructor = new DestructorDeclaration ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (d);
				newDestructor.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), MethodDeclaration.Roles.LPar);
				newDestructor.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), MethodDeclaration.Roles.RPar);
			
				typeStack.Peek ().AddChild (newDestructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Event e)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				
				newEvent.AddChild (ConvertToReturnType (e.TypeName), AbstractNode.Roles.ReturnType);
				
				Identifier nameIdentifier = new Identifier () {
					Name = e.Name
				};
				newEvent.AddChild (nameIdentifier, AbstractNode.Roles.Identifier);
				
				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.Roles.Member);
			}
			
			#endregion
			
			#region Statements
			public override object Visit (Statement stmt)
			{
				Console.WriteLine ("unknown statement:" + stmt);
				return null;
			}
			
			public override object Visit (Mono.CSharp.EmptyStatement emptyStatement)
			{
				var result = new MonoDevelop.CSharp.Dom.EmptyStatement ();
				result.Location = Convert (emptyStatement.loc);
				return result;
			}
			
			public override object Visit (If ifStatement)
			{
				return null;
			}
			
			public override object Visit (Do doStatement)
			{
				return null;
			}
			
			public override object Visit (While whileStatement)
			{
				return null;
			}
			
			public override object Visit (For forStatement)
			{
				return null;
			}
			
			public override object Visit (StatementExpression statementExpression)
			{
				return null;
			}
			
			public override object Visit (Return returnStatement)
			{
				return null;
			}
			
			public override object Visit (Goto gotoStatement)
			{
				return null;
			}
			
			public override object Visit (LabeledStatement labeledStatement)
			{
				return null;
			}
			
			public override object Visit (GotoDefault gotoDefault)
			{
				return null;
			}
			
			public override object Visit (GotoCase gotoCase)
			{
				return null;
			}
			
			public override object Visit (Throw throwStatement)
			{
				return null;
			}
			
			public override object Visit (Break breakStatement)
			{
				return null;
			}
			
			public override object Visit (Continue continueStatement)
			{
				return null;
			}
			
			public override object Visit (Block blockStatement)
			{
				var result = new BlockStatement ();
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation)), AbstractCSharpNode.Roles.LBrace);
				foreach (Statement stmt in blockStatement.Statements) {
					result.AddChild ((INode)stmt.Accept (this), AbstractCSharpNode.Roles.Statement);
				}
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation)), AbstractCSharpNode.Roles.RBrace);
				return result;
			}
			
			public override object Visit (Switch switchStatement)
			{
				return null;
			}
			
			public override object Visit (Lock lockStatement)
			{
				return null;
			}
			
			public override object Visit (Unchecked uncheckedStatement)
			{
				return null;
			}
			
			
			public override object Visit (Checked checkedStatement)
			{
				return null;
			}
			
			public override object Visit (Unsafe unsafeStatement)
			{
				return null;
			}
			
			public override object Visit (Fixed fixedStatement)
			{
				return null;
			}
			
			public override object Visit (TryFinally tryFinallyStatement)
			{
				return null;
			}
			
			public override object Visit (TryCatch tryCatchStatement)
			{
				return null;
			}
			
			public override object Visit (Using usingStatement)
			{
				return null;
			}
			
			
			public override object Visit (Foreach foreachStatement)
			{
				return null;
			}
			#endregion
			
			#region Expression
			public override object Visit (Expression expression)
			{
				Console.WriteLine ("Visit unknown expression:" + expression);
				return null;
			}
			
			public override object Visit (Mono.CSharp.ParenthesizedExpression parenthesizedExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.ParenthesizedExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (parenthesizedExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.LPar);
				result.AddChild ((INode)parenthesizedExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (Unary unaryExpression)
			{
				var result = new UnaryOperatorExpression ();
				switch (unaryExpression.Oper) {
				case Unary.Operator.UnaryPlus:
					result.UnaryOperatorType = UnaryOperatorType.Plus;
					break;
				case Unary.Operator.UnaryNegation:
					result.UnaryOperatorType = UnaryOperatorType.Minus;
					break;
				case Unary.Operator.LogicalNot:
					result.UnaryOperatorType = UnaryOperatorType.Not;
					break;
				case Unary.Operator.OnesComplement:
					result.UnaryOperatorType = UnaryOperatorType.BitNot;
					break;
				case Unary.Operator.AddressOf:
					result.UnaryOperatorType = UnaryOperatorType.AddressOf;
					break;
				}
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (unaryExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Location)), UnaryOperatorExpression.Operator);
				result.AddChild ((INode)unaryExpression.Expr.Accept (this), UnaryOperatorExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (UnaryMutator unaryMutatorExpression)
			{
				var result = new UnaryOperatorExpression ();
				
				INode expression = (INode)unaryMutatorExpression.Expr.Accept (this);
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (unaryMutatorExpression);
				switch (unaryMutatorExpression.UnaryMutatorMode) {
				case UnaryMutator.Mode.PostDecrement:
					result.UnaryOperatorType = UnaryOperatorType.PostDecrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (location.Location)), UnaryOperatorExpression.Operator);
					break;
				case UnaryMutator.Mode.PostIncrement:
					result.UnaryOperatorType = UnaryOperatorType.PostIncrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (location.Location)), UnaryOperatorExpression.Operator);
					break;
					
				case UnaryMutator.Mode.PreIncrement:
					result.UnaryOperatorType = UnaryOperatorType.Increment;
					result.AddChild (new CSharpTokenNode (Convert (location.Location)), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				case UnaryMutator.Mode.PreDecrement:
					result.UnaryOperatorType = UnaryOperatorType.Decrement;
					result.AddChild (new CSharpTokenNode (Convert (location.Location)), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				}
				
				return result;
			}
			
			public override object Visit (Indirection indirectionExpression)
			{
				var result = new UnaryOperatorExpression ();
				result.UnaryOperatorType = UnaryOperatorType.Dereference;
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (indirectionExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Location)), UnaryOperatorExpression.Operator);
				result.AddChild ((INode)indirectionExpression.Expr.Accept (this), UnaryOperatorExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (Is isExpression)
			{
				var result = new IsExpression ();
				result.AddChild ((INode)isExpression.Expr.Accept (this), IsExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (isExpression.Location)), IsExpression.Roles.Keyword);
				result.AddChild ((INode)isExpression.ProbeType.Accept (this), IsExpression.Roles.ReturnType);
				return result;
			}
			
			public override object Visit (As asExpression)
			{
				var result = new AsExpression ();
				result.AddChild ((INode)asExpression.Expr.Accept (this), AsExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (asExpression.Location)), AsExpression.Roles.Keyword);
				result.AddChild ((INode)asExpression.ProbeType.Accept (this), AsExpression.Roles.ReturnType);
				return result;
			}
			
			public override object Visit (Cast castExpression)
			{
				var result = new CastExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (castExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), CastExpression.Roles.LPar);
				result.AddChild ((INode)castExpression.TargetType.Accept (this), CastExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), CastExpression.Roles.RPar);
				result.AddChild ((INode)castExpression.Expr.Accept (this), CastExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.DefaultValueExpression defaultValueExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.DefaultValueExpression ();
				result.AddChild (new CSharpTokenNode (Convert (defaultValueExpression.Location)), CastExpression.Roles.Keyword);
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (defaultValueExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), CastExpression.Roles.LPar);
				result.AddChild ((INode)defaultValueExpression.Expr.Accept (this), CastExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), CastExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (Binary binaryExpression)
			{
				var result = new BinaryOperatorExpression ();
				switch (binaryExpression.Oper) {
					case Binary.Operator.Multiply:
						result.BinaryOperatorType = BinaryOperatorType.Multiply;
						break;
					case Binary.Operator.Division:
						result.BinaryOperatorType = BinaryOperatorType.Divide;
						break;
					case Binary.Operator.Modulus:
						result.BinaryOperatorType = BinaryOperatorType.Modulus;
						break;
					case Binary.Operator.Addition:
						result.BinaryOperatorType = BinaryOperatorType.Add;
						break;
					case Binary.Operator.Subtraction:
						result.BinaryOperatorType = BinaryOperatorType.Subtract;
						break;
					case Binary.Operator.LeftShift:
						result.BinaryOperatorType = BinaryOperatorType.ShiftLeft;
						break;
					case Binary.Operator.RightShift:
						result.BinaryOperatorType = BinaryOperatorType.ShiftRight;
						break;
					case Binary.Operator.LessThan:
						result.BinaryOperatorType = BinaryOperatorType.LessThan;
						break;
					case Binary.Operator.GreaterThan:
						result.BinaryOperatorType = BinaryOperatorType.GreaterThan;
						break;
					case Binary.Operator.LessThanOrEqual:
						result.BinaryOperatorType = BinaryOperatorType.LessThanOrEqual;
						break;
					case Binary.Operator.GreaterThanOrEqual:
						result.BinaryOperatorType = BinaryOperatorType.GreaterThanOrEqual;
						break;
					case Binary.Operator.Equality:
						result.BinaryOperatorType = BinaryOperatorType.Equality;
						break;
					case Binary.Operator.Inequality:
						result.BinaryOperatorType = BinaryOperatorType.InEquality;
						break;
					case Binary.Operator.BitwiseAnd:
						result.BinaryOperatorType = BinaryOperatorType.BitwiseAnd;
						break;
					case Binary.Operator.ExclusiveOr:
						result.BinaryOperatorType = BinaryOperatorType.ExclusiveOr;
						break;
					case Binary.Operator.BitwiseOr:
						result.BinaryOperatorType = BinaryOperatorType.BitwiseOr;
						break;
					case Binary.Operator.LogicalAnd:
						result.BinaryOperatorType = BinaryOperatorType.LogicalAnd;
						break;
					case Binary.Operator.LogicalOr:
						result.BinaryOperatorType = BinaryOperatorType.LogicalOr;
						break;
				}
				result.AddChild ((INode)binaryExpression.Left.Accept (this), BinaryOperatorExpression.LeftExpressionRole);
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (binaryExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Location)), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)binaryExpression.Left.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Nullable.NullCoalescingOperator nullCoalescingOperator)
			{
				var result = new BinaryOperatorExpression ();
				result.BinaryOperatorType = BinaryOperatorType.NullCoalescing;
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.LeftExpressionRole);
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (nullCoalescingOperator);
				result.AddChild (new CSharpTokenNode (Convert (location.Location)), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Conditional conditionalExpression)
			{
				var result = new ConditionalExpression ();
				result.AddChild ((INode)conditionalExpression.Expr.Accept (this), ConditionalExpression.Roles.Condition);
				ConditionalLocation location = LocationStorage.Get<ConditionalLocation> (conditionalExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.QuestionMarkLocation)), ConditionalExpression.Roles.QuestionMark);
				result.AddChild ((INode)conditionalExpression.TrueExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location.ColonLocation)), ConditionalExpression.Roles.Colon);
				result.AddChild ((INode)conditionalExpression.FalseExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				return result;
			}
			
			public override object Visit (Invocation invocationExpression)
			{
				var result = new InvocationExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (invocationExpression);
				result.AddChild ((INode)invocationExpression.Expr.Accept (this), InvocationExpression.Roles.TargetExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), InvocationExpression.Roles.RPar);
				for (int i = 0 ;i < invocationExpression.Arguments.Count; i++) {
					if (i > 0)
						result.AddChild (new CSharpTokenNode (Convert (invocationExpression.Arguments[i].SeparatingCommaLocation)), InvocationExpression.Roles.Comma);
					result.AddChild ((INode)invocationExpression.Arguments[i].Expr.Accept (this), InvocationExpression.Roles.Argument);
				}
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), InvocationExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (New newExpression)
			{
				return null;
			}
			
			public override object Visit (ArrayCreation ArrayCreationExpression)
			{
				return null;
			}
			
			public override object Visit (This thisExpression)
			{
				var result = new ThisReferenceExpression ();
				result.Location = Convert (thisExpression.Location);
				return result;
			}
			
			public override object Visit (ArglistAccess argListAccessExpression)
			{
				return null;
			}
			
			public override object Visit (Arglist argListExpression)
			{
				return null;
			}
			
			public override object Visit (TypeOf typeOfExpression)
			{
				var result = new TypeOfExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (typeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (typeOfExpression.Location)), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), TypeOfExpression.Roles.RPar);
				result.AddChild ((INode)typeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (SizeOf sizeOfExpression)
			{
				var result = new SizeOfExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (sizeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (sizeOfExpression.Location)), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), TypeOfExpression.Roles.RPar);
				result.AddChild ((INode)sizeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (CheckedExpr checkedExpression)
			{
				var result = new CheckedExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (checkedExpression);
				result.AddChild (new CSharpTokenNode (Convert (checkedExpression.Location)), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), TypeOfExpression.Roles.RPar);
				result.AddChild ((INode)checkedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (UnCheckedExpr uncheckedExpression)
			{
				var result = new UncheckedExpression ();
				ParanthesisLocation location = LocationStorage.Get<ParanthesisLocation> (uncheckedExpression);
				result.AddChild (new CSharpTokenNode (Convert (uncheckedExpression.Location)), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.OpenParenthesisLocation)), TypeOfExpression.Roles.RPar);
				result.AddChild ((INode)uncheckedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.CloseParenthesisLocation)), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (ElementAccess elementAccessExpression)
			{
				return null;
			}
			
			public override object Visit (BaseAccess baseAccessExpression)
			{
				return null;
			}
			
			public override object Visit (StackAlloc stackAllocExpression)
			{
				return null;
			}
			#endregion
		}

		public MonoDevelop.CSharp.Dom.CompilationUnit Parse (TextEditorData data)
		{
			Tokenizer.InterpretTabAsSingleChar = true;
			CompilerCompilationUnit top;
			using (Stream stream = data.OpenStream ()) {
				top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v"}, stream, data.Document.FileName, Console.Out);
			}

			if (top == null)
				return null;
			CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor (top.LocationStorage);
			top.ModuleCompiled.Accept (conversionVisitor);
			return conversionVisitor.Unit;
		}
	}
}*/
