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

/*
using System;
using System.Linq;
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
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Class;
				
				MemberLocation location = LocationStorage.Get<MemberLocation> (c);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "class".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (c.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				newType.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (c);
				newType.AddChild (new CSharpTokenNode  (Convert (location[3]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Struct s)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Struct;
				
				MemberLocation location = LocationStorage.Get<MemberLocation> (s);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "struct".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (s.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				newType.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (s);
				newType.AddChild (new CSharpTokenNode  (Convert (location[3]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Interface i)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Interface;
				
				MemberLocation location = LocationStorage.Get<MemberLocation> (i);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "interface".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (i.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				newType.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (i);
				newType.AddChild (new CSharpTokenNode  (Convert (location[3]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Mono.CSharp.Delegate d)
			{
				DelegateDeclaration newDelegate = new DelegateDeclaration ();
				MemberLocation location = LocationStorage.Get<MemberLocation> (d);
				
				AddModifiers (newDelegate, location);
				
				newDelegate.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), TypeDeclaration.TypeKeyword);
				newDelegate.AddChild (ConvertToReturnType (d.TypeName), AbstractNode.Roles.ReturnType);
				newDelegate.AddChild (new Identifier (d.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				
				newDelegate.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DelegateDeclaration.Roles.LPar);
				newDelegate.AddChild (new CSharpTokenNode (Convert (location[3]), 1), DelegateDeclaration.Roles.RPar);
				newDelegate.AddChild (new CSharpTokenNode (Convert (location[4]), 1), DelegateDeclaration.Roles.Semicolon);
				
				AddType (newDelegate);
			}
			
			void AddType (INode child)
			{
				if (typeStack.Count > 0) {
					typeStack.Peek ().AddChild (child);
				} else {
					unit.AddChild (child);
				}
			}
			
			public override void Visit (Mono.CSharp.Enum e)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Enum;
				
				MemberLocation location = LocationStorage.Get<MemberLocation> (e);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "enum".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (e.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				newType.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (e);
				newType.AddChild (new CSharpTokenNode  (Convert (location[3]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
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
					
					Identifier fieldName = new Identifier (curField.MemberName.Name, Convert (curField.MemberName.Location));
					variable.AddChild (fieldName, AbstractNode.Roles.Identifier);
					newField.AddChild (variable, AbstractNode.Roles.Initializer);
					if (curField.ConnectedField != null) {
						CommaLocation location = LocationStorage.Get<CommaLocation> (curField);
				
						newField.AddChild (new CSharpTokenNode (Convert (location.Location), 1), AbstractNode.Roles.Comma);
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
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (o);
				newOperator.AddChild (new CSharpTokenNode (Convert (location.Open), 1), OperatorDeclaration.Roles.LPar);
				newOperator.AddChild (new CSharpTokenNode (Convert (location.Close), 1), OperatorDeclaration.Roles.RPar);
				
				if (o.Block != null)
					newOperator.AddChild ((INode)o.Block.Accept (this), OperatorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newOperator, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Indexer indexer)
			{
				IndexerDeclaration newIndexer = new IndexerDeclaration ();
				
				MemberLocation location = LocationStorage.Get<MemberLocation> (indexer);
				AddModifiers (newIndexer, location);
				
				newIndexer.AddChild (ConvertToReturnType (indexer.TypeName), AbstractNode.Roles.ReturnType);
				
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IndexerDeclaration.Roles.LBracket);
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IndexerDeclaration.Roles.RBracket);
				
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[2]), 1), IndexerDeclaration.Roles.LBrace);
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[3]), 1), IndexerDeclaration.Roles.RBrace);
				
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
				
				newMethod.AddChild (ConvertToReturnType (m.TypeName), AbstractNode.Roles.ReturnType);
				newMethod.AddChild (new Identifier (m.Name, Convert (m.Location)), AbstractNode.Roles.Identifier);
				MemberLocation location = LocationStorage.Get<MemberLocation> (m);
				AddModifiers (newMethod, location);
				newMethod.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				newMethod.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
				if (m.Block != null)
					newMethod.AddChild ((INode)m.Block.Accept (this), MethodDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newMethod, TypeDeclaration.Roles.Member);
			}
			
			static Dictionary<Mono.CSharp.Modifiers, MonoDevelop.Projects.Dom.Modifiers> modifierTable = new Dictionary<Mono.CSharp.Modifiers, MonoDevelop.Projects.Dom.Modifiers> ();
			static ConversionVisitor ()
			{
				modifierTable[Mono.CSharp.Modifiers.NEW] = MonoDevelop.Projects.Dom.Modifiers.New;
				modifierTable[Mono.CSharp.Modifiers.PUBLIC] = MonoDevelop.Projects.Dom.Modifiers.Public;
				modifierTable[Mono.CSharp.Modifiers.PROTECTED] = MonoDevelop.Projects.Dom.Modifiers.Protected;
				modifierTable[Mono.CSharp.Modifiers.PRIVATE] = MonoDevelop.Projects.Dom.Modifiers.Private;
				modifierTable[Mono.CSharp.Modifiers.INTERNAL] = MonoDevelop.Projects.Dom.Modifiers.Internal;
				modifierTable[Mono.CSharp.Modifiers.ABSTRACT] = MonoDevelop.Projects.Dom.Modifiers.Abstract;
				modifierTable[Mono.CSharp.Modifiers.VIRTUAL] = MonoDevelop.Projects.Dom.Modifiers.Virtual;
				modifierTable[Mono.CSharp.Modifiers.SEALED] = MonoDevelop.Projects.Dom.Modifiers.Sealed;
				modifierTable[Mono.CSharp.Modifiers.STATIC] = MonoDevelop.Projects.Dom.Modifiers.Static;
				modifierTable[Mono.CSharp.Modifiers.OVERRIDE] = MonoDevelop.Projects.Dom.Modifiers.Override;
				modifierTable[Mono.CSharp.Modifiers.READONLY] = MonoDevelop.Projects.Dom.Modifiers.Readonly;
//				modifierTable[Mono.CSharp.Modifiers.] = MonoDevelop.Projects.Dom.Modifiers.Const;
				modifierTable[Mono.CSharp.Modifiers.PARTIAL] = MonoDevelop.Projects.Dom.Modifiers.Partial;
				modifierTable[Mono.CSharp.Modifiers.EXTERN] = MonoDevelop.Projects.Dom.Modifiers.Extern;
				modifierTable[Mono.CSharp.Modifiers.VOLATILE] = MonoDevelop.Projects.Dom.Modifiers.Volatile;
				modifierTable[Mono.CSharp.Modifiers.UNSAFE] = MonoDevelop.Projects.Dom.Modifiers.Unsafe;
				modifierTable[Mono.CSharp.Modifiers.OVERRIDE] = MonoDevelop.Projects.Dom.Modifiers.Overloads;
			}
			
			void AddModifiers (AbstractNode parent, Mono.CSharp.MemberLocation location)
			{
				if (location.Modifiers == null)
					return;
				foreach (var modifier in location.Modifiers) {
					parent.AddChild (new CSharpModifierToken (Convert (modifier.Item1), modifierTable[modifier.Item2]), AbstractNode.Roles.Modifier);
				}
			}
			
			public override void Visit (Property p)
			{
				PropertyDeclaration newProperty = new PropertyDeclaration ();
				
				MemberLocation location = LocationStorage.Get<MemberLocation> (p);
				AddModifiers (newProperty, location);
				
				newProperty.AddChild (ConvertToReturnType (p.TypeName), AbstractNode.Roles.ReturnType);
				newProperty.AddChild (new Identifier (p.MemberName.Name, Convert (p.MemberName.Location)), AbstractNode.Roles.Identifier);
				newProperty.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LBrace);
				
				if (p.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					MemberLocation getLocation = LocationStorage.Get<MemberLocation> (p.Get);
					AddModifiers (getAccessor, getLocation);
					getAccessor.AddChild (new CSharpTokenNode (Convert (getLocation[0]), "get".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Get.Block != null)
						getAccessor.AddChild ((INode)p.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					newProperty.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
				}
				
				if (p.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					MemberLocation setLocation = LocationStorage.Get<MemberLocation> (p.Set);
					AddModifiers (setAccessor, setLocation);
					setAccessor.AddChild (new CSharpTokenNode (Convert (setLocation[0]), "set".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Set.Block != null)
						setAccessor.AddChild ((INode)p.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
					newProperty.AddChild (setAccessor, PropertyDeclaration.PropertySetRole);
				}
				newProperty.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newProperty, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Constructor c)
			{
				ConstructorDeclaration newConstructor = new ConstructorDeclaration ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (c);
				newConstructor.AddChild (new CSharpTokenNode (Convert (location.Open), 1), MethodDeclaration.Roles.LPar);
				newConstructor.AddChild (new CSharpTokenNode (Convert (location.Close), 1), MethodDeclaration.Roles.RPar);
				
				typeStack.Peek ().AddChild (newConstructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Destructor d)
			{
				DestructorDeclaration newDestructor = new DestructorDeclaration ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (d);
				newDestructor.AddChild (new CSharpTokenNode (Convert (location.Open), 1), MethodDeclaration.Roles.LPar);
				newDestructor.AddChild (new CSharpTokenNode (Convert (location.Close), 1), MethodDeclaration.Roles.RPar);
			
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
				var result = new IfElseStatement ();
				
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (ifStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "if".Length), IfElseStatement.IfKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), IfElseStatement.Roles.LPar);
				result.AddChild ((INode)ifStatement.Expr.Accept (this), IfElseStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), IfElseStatement.Roles.RPar);
				
				result.AddChild ((INode)ifStatement.TrueStatement.Accept (this), IfElseStatement.TrueEmbeddedStatementRole);
				
				if (ifStatement.FalseStatement != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "else".Length), IfElseStatement.ElseKeywordRole);
					result.AddChild ((INode)ifStatement.FalseStatement.Accept (this), IfElseStatement.FalseEmbeddedStatementRole);
				}
				
				return result;
			}
			
			public override object Visit (Do doStatement)
			{
				var result = new WhileStatement (WhilePosition.Begin);
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (doStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "do".Length), WhileStatement.DoKeywordRole);
				result.AddChild ((INode)doStatement.EmbeddedStatement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "while".Length), WhileStatement.WhileKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), WhileStatement.Roles.LPar);
				result.AddChild ((INode)doStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), WhileStatement.Roles.RPar);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), WhileStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (While whileStatement)
			{
				var result = new WhileStatement (WhilePosition.End);
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (whileStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "while".Length), WhileStatement.WhileKeywordRole);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), WhileStatement.Roles.LPar);
				result.AddChild ((INode)whileStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), WhileStatement.Roles.RPar);
				
				result.AddChild ((INode)whileStatement.Statement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (For forStatement)
			{
				var result = new ForStatement ();
				
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (forStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "for".Length), ForStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), ForStatement.Roles.LPar);
				if (forStatement.InitStatement != null)
					result.AddChild ((INode)forStatement.InitStatement.Accept (this), ForStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Test != null)
					result.AddChild ((INode)forStatement.Test.Accept (this), ForStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Increment != null)
					result.AddChild ((INode)forStatement.Increment.Accept (this), ForStatement.Roles.Iterator);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), ForStatement.Roles.RPar);
				
				result.AddChild ((INode)forStatement.Statement.Accept (this), ForStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (StatementExpression statementExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.ExpressionStatement ();
				SemicolonLocation location = LocationStorage.Get<SemicolonLocation> (statementExpression);
				
				result.AddChild ((INode)statementExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ExpressionStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Location), 1), MonoDevelop.CSharp.Dom.ExpressionStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Return returnStatement)
			{
				var result = new ReturnStatement ();
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (returnStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "return".Length), ReturnStatement.Roles.Keyword);
				if (returnStatement.Expr != null)
					result.AddChild ((INode)returnStatement.Expr.Accept (this), ReturnStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), ReturnStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (Goto gotoStatement)
			{
				var result = new GotoStatement (GotoType.Label);
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (gotoStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new Identifier (gotoStatement.Target, Convert (gotoStatement.loc)), GotoStatement.Roles.Identifier);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), GotoStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (LabeledStatement labeledStatement)
			{
				var result = new LabelStatement ();
				result.AddChild (new Identifier (labeledStatement.Name, Convert (labeledStatement.loc)), LabelStatement.Roles.Identifier);
				return result;
			}
			
			public override object Visit (GotoDefault gotoDefault)
			{
				var result = new GotoStatement (GotoType.CaseDefault);
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (gotoDefault);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "default".Length), GotoStatement.DefaultKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), GotoStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (GotoCase gotoCase)
			{
				var result = new GotoStatement (GotoType.Case);
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (gotoCase);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "case".Length), GotoStatement.CaseKeywordRole);
				result.AddChild ((INode)gotoCase.Expr.Accept (this), GotoStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), GotoStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Throw throwStatement)
			{
				var result = new ThrowStatement ();
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (throwStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "throw".Length), ThrowStatement.Roles.Keyword);
				if (throwStatement.Expr != null)
					result.AddChild ((INode)throwStatement.Expr.Accept (this), ThrowStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), ThrowStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Break breakStatement)
			{
				var result = new BreakStatement ();
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (breakStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "break".Length), BreakStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), BreakStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Continue continueStatement)
			{
				var result = new ContinueStatement ();
				KeywordWithSemicolon location = LocationStorage.Get<KeywordWithSemicolon> (continueStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "continue".Length), ContinueStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Semicolon), 1), ContinueStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Block blockStatement)
			{
				if (blockStatement.IsGenerated)
					return blockStatement.Statements.Last ().Accept (this);
				var result = new BlockStatement ();
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation), 1), AbstractCSharpNode.Roles.LBrace);
				foreach (Statement stmt in blockStatement.Statements) {
					result.AddChild ((INode)stmt.Accept (this), AbstractCSharpNode.Roles.Statement);
				}
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation), 1), AbstractCSharpNode.Roles.RBrace);
				
				return result;
			}
			
			public override object Visit (Switch switchStatement)
			{
				var result = new SwitchStatement ();
				
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (switchStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "switch".Length), SwitchStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), SwitchStatement.Roles.LPar);
				result.AddChild ((INode)switchStatement.Expr.Accept (this), SwitchStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), SwitchStatement.Roles.RPar);
				
				foreach (var section in switchStatement.Sections) {
					var newSection = new MonoDevelop.CSharp.Dom.SwitchSection ();
					foreach (var caseLabel in section.Labels) {
						var newLabel = new CaseLabel ();
						newLabel.AddChild (new CSharpTokenNode (Convert (caseLabel.Location), "case".Length), SwitchStatement.Roles.Keyword);
						if (caseLabel.Label != null)
							newLabel.AddChild ((INode)caseLabel.Label.Accept (this), SwitchStatement.Roles.Expression);
						
						newSection.AddChild (newLabel, MonoDevelop.CSharp.Dom.SwitchSection.CaseLabelRole);
					}
					newSection.AddChild ((INode)section.Block.Accept (this), MonoDevelop.CSharp.Dom.SwitchSection.Roles.Body);
					result.AddChild (newSection, SwitchStatement.SwitchSectionRole);
				}
				return result;
			}
			
			public override object Visit (Lock lockStatement)
			{
				var result = new LockStatement ();
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (lockStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "lock".Length), LockStatement.Roles.Keyword);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), LockStatement.Roles.LPar);
				result.AddChild ((INode)lockStatement.Expr.Accept (this), LockStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), LockStatement.Roles.RPar);
				result.AddChild ((INode)lockStatement.Statement.Accept (this), LockStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Unchecked uncheckedStatement)
			{
				var result = new UncheckedStatement ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (uncheckedStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "unchecked".Length), UncheckedStatement.Roles.Keyword);
				result.AddChild ((INode)uncheckedStatement.Block.Accept (this), UncheckedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			
			public override object Visit (Checked checkedStatement)
			{
				var result = new CheckedStatement ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (checkedStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "checked".Length), CheckedStatement.Roles.Keyword);
				result.AddChild ((INode)checkedStatement.Block.Accept (this), CheckedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Unsafe unsafeStatement)
			{
				var result = new UnsafeStatement ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (unsafeStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "unsafe".Length), UnsafeStatement.Roles.Keyword);
				result.AddChild ((INode)unsafeStatement.Block.Accept (this), UnsafeStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Fixed fixedStatement)
			{
				var result = new FixedStatement ();
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (fixedStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "fixed".Length), FixedStatement.FixedKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), FixedStatement.Roles.LPar);
				
				result.AddChild ((INode)fixedStatement.Type.Accept (this), FixedStatement.PointerDeclarationRole);
				
				foreach (KeyValuePair<LocalInfo, Expression> declarator in fixedStatement.Declarators) {
					result.AddChild ((INode)declarator.Value.Accept (this), FixedStatement.DeclaratorRole);
				}
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), FixedStatement.Roles.RPar);
				result.AddChild ((INode)fixedStatement.Statement.Accept (this), FixedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (TryFinally tryFinallyStatement)
			{
				TryCatchStatement result;
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (tryFinallyStatement);
				
				if (tryFinallyStatement.Stmt is TryCatch) {
					result = (TryCatchStatement)tryFinallyStatement.Stmt.Accept (this);
				} else {
					result = new TryCatchStatement ();
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "try".Length), TryCatchStatement.TryKeywordRole);
					result.AddChild ((INode)tryFinallyStatement.Stmt.Accept (this), TryCatchStatement.TryBlockRole);
				}
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "finally".Length), TryCatchStatement.FinallyKeywordRole);
				result.AddChild ((INode)tryFinallyStatement.Fini.Accept (this), TryCatchStatement.FinallyBlockRole);
				
				return result;
			}
			
			CatchClause ConvertCatch (Catch ctch) 
			{
				CatchClause result = new CatchClause ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (ctch);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "catch".Length), CatchClause.Roles.Keyword);
				
				if (ctch.Type_expr != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), CatchClause.Roles.LPar);
					
					result.AddChild ((INode)ctch.Type_expr.Accept (this), CatchClause.Roles.ReturnType);
					if (!string.IsNullOrEmpty (ctch.Name))
						result.AddChild (new Identifier (ctch.Name, Convert (location[1])), CatchClause.Roles.Identifier);
					
					result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), CatchClause.Roles.RPar);
				}
				
				result.AddChild ((INode)ctch.Block.Accept (this), CatchClause.Roles.Body);
				
				return result;
			}
			
			public override object Visit (TryCatch tryCatchStatement)
			{
				var result = new TryCatchStatement ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (tryCatchStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "try".Length), TryCatchStatement.TryKeywordRole);
				result.AddChild ((INode)tryCatchStatement.Block.Accept (this), TryCatchStatement.TryBlockRole);
				foreach (Catch ctch in tryCatchStatement.Specific) {
					result.AddChild (ConvertCatch (ctch), TryCatchStatement.CatchClauseRole);
				}
				if (tryCatchStatement.General != null)
					result.AddChild (ConvertCatch (tryCatchStatement.General), TryCatchStatement.CatchClauseRole);
				
				return result;
			}
			
			public override object Visit (Using usingStatement)
			{
				var result = new UsingStatement ();
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (usingStatement);
				// TODO: Usings with more than 1 variable are compiled differently using (a) { using (b) { using (c) ...}}
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "using".Length), UsingStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), UsingStatement.Roles.LPar);
				
				if (usingStatement.Var != null)
					result.AddChild ((INode)usingStatement.Var.Accept (this), UsingStatement.Roles.Identifier);
				
				if (usingStatement.Init != null)
					result.AddChild ((INode)usingStatement.Init.Accept (this), UsingStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), UsingStatement.Roles.RPar);
				
				result.AddChild ((INode)usingStatement.EmbeddedStatement.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (UsingTemporary usingTemporary)
			{
				var result = new UsingStatement ();
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (usingTemporary);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "using".Length), UsingStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), UsingStatement.Roles.LPar);
				
				result.AddChild ((INode)usingTemporary.Expr.Accept (this), UsingStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), UsingStatement.Roles.RPar);
				
				result.AddChild ((INode)usingTemporary.Statement.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			
			public override object Visit (Foreach foreachStatement)
			{
				var result = new ForeachStatement ();
				
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation> (foreachStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "foreach".Length), ForeachStatement.ForEachKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), ForeachStatement.Roles.LPar);
				
				if (foreachStatement.TypeExpr == null)
					result.AddChild ((INode)foreachStatement.TypeExpr.Accept (this), ForeachStatement.Roles.ReturnType);
				if (foreachStatement.Variable != null)
					result.AddChild ((INode)foreachStatement.Variable.Accept (this), ForeachStatement.Roles.Identifier);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), ForeachStatement.InKeywordRole);
				
				if (foreachStatement.Expr != null)
					result.AddChild ((INode)foreachStatement.Expr.Accept (this), ForeachStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), ForeachStatement.Roles.RPar);
				result.AddChild ((INode)foreachStatement.Statement.Accept (this), ForeachStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Yield yieldStatement)
			{
				var result = new YieldStatement ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (yieldStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "yield".Length), YieldStatement.YieldKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "return".Length), YieldStatement.ReturnKeywordRole);
				if (yieldStatement.Expr != null)
					result.AddChild ((INode)yieldStatement.Expr.Accept (this), YieldStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), ";".Length), YieldStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (YieldBreak yieldBreakStatement)
			{
				var result = new YieldStatement ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (yieldBreakStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "yield".Length), YieldStatement.YieldKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "break".Length), YieldStatement.BreakKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), ";".Length), YieldStatement.Roles.Semicolon);
				return result;
			}
			#endregion
			
			#region Expression
			public override object Visit (Expression expression)
			{
				Console.WriteLine ("Visit unknown expression:" + expression);
				return null;
			}
			
			public override object Visit (TypeLookupExpression typeLookupExpression)
			{
				if (!string.IsNullOrEmpty (typeLookupExpression.Namespace))
					return new FullTypeName (typeLookupExpression.Namespace + "." + typeLookupExpression.Name, Convert (typeLookupExpression.Location));
				return new FullTypeName (typeLookupExpression.Name, Convert (typeLookupExpression.Location));
			}

			public override object Visit (LocalVariableReference localVariableReference)
			{
				return new Identifier (localVariableReference.Name, Convert (localVariableReference.Location));;
			}

			public override object Visit (MemberAccess memberAccess)
			{
				var result = new MemberReferenceExpression ();
				result.AddChild ((INode)memberAccess.Left.Accept (this), MemberReferenceExpression.Roles.TargetExpression);
				result.AddChild (new Identifier (memberAccess.Name, Convert (memberAccess.Location)), MemberReferenceExpression.Roles.Identifier);
				return result;
			}
			
			public override object Visit (Constant constant)
			{
				var result = new PrimitiveExpression (constant.GetValue (), Convert (constant.Location), constant.AsString ().Length);
				return result;
			}

			public override object Visit (SimpleName simpleName)
			{
				
				return new Identifier (simpleName.Name, Convert (simpleName.Location));
			}
			
			public override object Visit (BooleanExpression booleanExpression)
			{
				return booleanExpression.Expr.Accept (this);
			}

			
			public override object Visit (Mono.CSharp.ParenthesizedExpression parenthesizedExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.ParenthesizedExpression ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (parenthesizedExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.LPar);
				result.AddChild ((INode)parenthesizedExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.RPar);
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
				result.AddChild (new CSharpTokenNode (Convert (unaryExpression.Location), 1), UnaryOperatorExpression.Operator);
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
					result.AddChild (new CSharpTokenNode (Convert (location.Location), 2), UnaryOperatorExpression.Operator);
					break;
				case UnaryMutator.Mode.PostIncrement:
					result.UnaryOperatorType = UnaryOperatorType.PostIncrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (location.Location), 2), UnaryOperatorExpression.Operator);
					break;
					
				case UnaryMutator.Mode.PreIncrement:
					result.UnaryOperatorType = UnaryOperatorType.Increment;
					result.AddChild (new CSharpTokenNode (Convert (location.Location), 2), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				case UnaryMutator.Mode.PreDecrement:
					result.UnaryOperatorType = UnaryOperatorType.Decrement;
					result.AddChild (new CSharpTokenNode (Convert (location.Location), 2), UnaryOperatorExpression.Operator);
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
				result.AddChild (new CSharpTokenNode (Convert (location.Location), 2), UnaryOperatorExpression.Operator);
				result.AddChild ((INode)indirectionExpression.Expr.Accept (this), UnaryOperatorExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (Is isExpression)
			{
				var result = new IsExpression ();
				result.AddChild ((INode)isExpression.Expr.Accept (this), IsExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (isExpression.Location), "is".Length), IsExpression.Roles.Keyword);
				result.AddChild ((INode)isExpression.ProbeType.Accept (this), IsExpression.Roles.ReturnType);
				return result;
			}
			
			public override object Visit (As asExpression)
			{
				var result = new AsExpression ();
				result.AddChild ((INode)asExpression.Expr.Accept (this), AsExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (asExpression.Location), "as".Length), AsExpression.Roles.Keyword);
				result.AddChild ((INode)asExpression.ProbeType.Accept (this), AsExpression.Roles.ReturnType);
				return result;
			}
			
			public override object Visit (Cast castExpression)
			{
				var result = new CastExpression ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (castExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), CastExpression.Roles.LPar);
				if (castExpression.TargetType != null)
					result.AddChild ((INode)castExpression.TargetType.Accept (this), CastExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), CastExpression.Roles.RPar);
				if (castExpression.Expr != null)
					result.AddChild ((INode)castExpression.Expr.Accept (this), CastExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (ComposedCast composedCast)
			{
				var result = new PointerReferenceExpression ();
				result.Dim = composedCast.Dim;
				result.AddChild (new CSharpTokenNode (Convert(composedCast.Location), composedCast.Dim.Length), PointerReferenceExpression.Roles.Argument);
				result.AddChild ((INode)composedCast.Left.Accept (this), PointerReferenceExpression.Roles.TargetExpression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.DefaultValueExpression defaultValueExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.DefaultValueExpression ();
				result.AddChild (new CSharpTokenNode (Convert (defaultValueExpression.Location), "default".Length), CastExpression.Roles.Keyword);
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (defaultValueExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), CastExpression.Roles.LPar);
				result.AddChild ((INode)defaultValueExpression.Expr.Accept (this), CastExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), CastExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (Binary binaryExpression)
			{
				var result = new BinaryOperatorExpression ();
				int opLength = 1;
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
						opLength = 2;
						break;
					case Binary.Operator.RightShift:
						result.BinaryOperatorType = BinaryOperatorType.ShiftRight;
						opLength = 2;
						break;
					case Binary.Operator.LessThan:
						result.BinaryOperatorType = BinaryOperatorType.LessThan;
						break;
					case Binary.Operator.GreaterThan:
						result.BinaryOperatorType = BinaryOperatorType.GreaterThan;
						break;
					case Binary.Operator.LessThanOrEqual:
						result.BinaryOperatorType = BinaryOperatorType.LessThanOrEqual;
						opLength = 2;
						break;
					case Binary.Operator.GreaterThanOrEqual:
						result.BinaryOperatorType = BinaryOperatorType.GreaterThanOrEqual;
						opLength = 2;
						break;
					case Binary.Operator.Equality:
						result.BinaryOperatorType = BinaryOperatorType.Equality;
						opLength = 2;
						break;
					case Binary.Operator.Inequality:
						result.BinaryOperatorType = BinaryOperatorType.InEquality;
						opLength = 2;
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
						opLength = 2;
						break;
					case Binary.Operator.LogicalOr:
						result.BinaryOperatorType = BinaryOperatorType.LogicalOr;
						opLength = 2;
						break;
				}
				result.AddChild ((INode)binaryExpression.Left.Accept (this), BinaryOperatorExpression.LeftExpressionRole);
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (binaryExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Location), opLength), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)binaryExpression.Left.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Nullable.NullCoalescingOperator nullCoalescingOperator)
			{
				var result = new BinaryOperatorExpression ();
				result.BinaryOperatorType = BinaryOperatorType.NullCoalescing;
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.LeftExpressionRole);
				OperatorLocation location = LocationStorage.Get<OperatorLocation> (nullCoalescingOperator);
				result.AddChild (new CSharpTokenNode (Convert (location.Location), 2), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Conditional conditionalExpression)
			{
				var result = new ConditionalExpression ();
				
				result.AddChild ((INode)conditionalExpression.Expr.Accept (this), ConditionalExpression.Roles.Condition);
				ConditionalLocation location = LocationStorage.Get<ConditionalLocation> (conditionalExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (location.QuestionMark), 1), ConditionalExpression.Roles.QuestionMark);
				result.AddChild ((INode)conditionalExpression.TrueExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location.Colon), 1), ConditionalExpression.Roles.Colon);
				result.AddChild ((INode)conditionalExpression.FalseExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				return result;
			}
			
			public override object Visit (Invocation invocationExpression)
			{
				var result = new InvocationExpression ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (invocationExpression);
				result.AddChild ((INode)invocationExpression.Expr.Accept (this), InvocationExpression.Roles.TargetExpression);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), InvocationExpression.Roles.LPar);
				if (invocationExpression.Arguments != null) {
					for (int i = 0 ;i < invocationExpression.Arguments.Count; i++) {
						if (i > 0)
							result.AddChild (new CSharpTokenNode (Convert (invocationExpression.Arguments[i].SeparatingCommaLocation), 1), InvocationExpression.Roles.Comma);
						result.AddChild ((INode)invocationExpression.Arguments[i].Expr.Accept (this), InvocationExpression.Roles.Argument);
					}
				}
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), InvocationExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (New newExpression)
			{
				var result = new ObjectCreateExpression ();
				
				KeywordWithParenthesisLocation location = LocationStorage.Get<KeywordWithParenthesisLocation > (newExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "new".Length), ObjectCreateExpression.Roles.Keyword);
				
				if (newExpression.NewType != null)
					result.AddChild ((INode)newExpression.NewType.Accept (this), ObjectCreateExpression.Roles.ReturnType);
				
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), ObjectCreateExpression.Roles.LPar);
				if (newExpression.NewArguments != null) {
					for (int i = 0 ;i < newExpression.NewArguments.Count; i++) {
						if (i > 0)
							result.AddChild (new CSharpTokenNode (Convert (newExpression.NewArguments[i].SeparatingCommaLocation), 1), ObjectCreateExpression.Roles.Comma);
						result.AddChild ((INode)newExpression.NewArguments[i].Expr.Accept (this), ObjectCreateExpression.Roles.Argument);
					}
				}
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), ObjectCreateExpression.Roles.RPar);
				
				return result;
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
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (typeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (typeOfExpression.Location), "typeof".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)typeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (SizeOf sizeOfExpression)
			{
				var result = new SizeOfExpression ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (sizeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (sizeOfExpression.Location), "sizeof".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)sizeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (CheckedExpr checkedExpression)
			{
				var result = new CheckedExpression ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (checkedExpression);
				result.AddChild (new CSharpTokenNode (Convert (checkedExpression.Location), "checked".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)checkedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (UnCheckedExpr uncheckedExpression)
			{
				var result = new UncheckedExpression ();
				ParenthesisLocation location = LocationStorage.Get<ParenthesisLocation> (uncheckedExpression);
				result.AddChild (new CSharpTokenNode (Convert (uncheckedExpression.Location), "unchecked".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location.Open), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)uncheckedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location.Close), 1), TypeOfExpression.Roles.RPar);
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
			
			public override object Visit (SimpleAssign simpleAssign)
			{
				var result = new AssignmentExpression ();
				
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (simpleAssign);
				
				result.AssignmentOperatorType = AssignmentOperatorType.Assign;
				result.AddChild ((INode)simpleAssign.Target.Accept (this), AssignmentExpression.LeftExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), AssignmentExpression.OperatorRole);
				result.AddChild ((INode)simpleAssign.Source.Accept (this), AssignmentExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (CompoundAssign compoundAssign)
			{
				var result = new AssignmentExpression ();
				KeywordLocation location = LocationStorage.Get<KeywordLocation> (compoundAssign);
				int opLength = 2;
				switch (compoundAssign.Op) {
				case Binary.Operator.Multiply:
					result.AssignmentOperatorType = AssignmentOperatorType.Multiply;
					break;
				case Binary.Operator.Division:
					result.AssignmentOperatorType = AssignmentOperatorType.Divide;
					break;
				case Binary.Operator.Modulus:
					result.AssignmentOperatorType = AssignmentOperatorType.Modulus;
					break;
				case Binary.Operator.Addition:
					result.AssignmentOperatorType = AssignmentOperatorType.Add;
					break;
				case Binary.Operator.Subtraction:
					result.AssignmentOperatorType = AssignmentOperatorType.Subtract;
					break;
				case Binary.Operator.LeftShift:
					result.AssignmentOperatorType = AssignmentOperatorType.ShiftLeft;
					opLength = 3;
					break;
				case Binary.Operator.RightShift:
					result.AssignmentOperatorType = AssignmentOperatorType.ShiftRight;
					opLength = 3;
					break;
				case Binary.Operator.BitwiseAnd:
					result.AssignmentOperatorType = AssignmentOperatorType.BitwiseAnd;
					break;
				case Binary.Operator.BitwiseOr:
					result.AssignmentOperatorType = AssignmentOperatorType.BitwiseOr;
					break;
				case Binary.Operator.ExclusiveOr:
					result.AssignmentOperatorType = AssignmentOperatorType.ExclusiveOr;
					break;
				}
				
				result.AddChild ((INode)compoundAssign.Target.Accept (this), AssignmentExpression.LeftExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), opLength), AssignmentExpression.OperatorRole);
				result.AddChild ((INode)compoundAssign.Source.Accept (this), AssignmentExpression.RightExpressionRole);
				return result;
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
