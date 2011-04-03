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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Mono.CSharp;
using System.Text;
using Mono.TextEditor;
using MonoDevelop.CSharp.Ast;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Parser
{
	public class CSharpParser
	{
		class ConversionVisitor : StructuralVisitor
		{
			MonoDevelop.CSharp.Ast.CompilationUnit unit = new MonoDevelop.CSharp.Ast.CompilationUnit ();
			
			public MonoDevelop.CSharp.Ast.CompilationUnit Unit {
				get {
					return unit;
				}
				set {
					unit = value;
				}
			}
			
			public LocationsBag LocationsBag  {
				get;
				private set;
			}
			
			public ConversionVisitor (LocationsBag locationsBag)
			{
				this.LocationsBag = locationsBag;
			}
			
			public static AstLocation Convert (Mono.CSharp.Location loc)
			{
				return new AstLocation (loc.Row, loc.Column);
			}
			
			#region Global
			Stack<NamespaceDeclaration> namespaceStack = new Stack<NamespaceDeclaration> ();
			
			void AddTypeArguments (ATypeNameExpression texpr, AstType result)
			{
				if (!texpr.HasTypeArguments)
					return;
				foreach (var arg in texpr.TypeArguments.Args) {
					result.AddChild (ConvertToType (arg), AstType.Roles.TypeArgument);
				}
			}
			
			AstType ConvertToType (Mono.CSharp.Expression typeName)
			{
				if (typeName is TypeExpression) {
					var typeExpr = (Mono.CSharp.TypeExpression)typeName;
					return new PrimitiveType (typeExpr.GetSignatureForError (), Convert (typeExpr.Location));
				}
				
				if (typeName is Mono.CSharp.QualifiedAliasMember) {
					var qam = (Mono.CSharp.QualifiedAliasMember)typeName;
					// TODO: Overwork the return type model - atm we don't have a good representation
					// for qualified alias members.
					return new SimpleType (qam.Name, Convert (qam.Location));
				}
				
				if (typeName is MemberAccess) {
					MemberAccess ma = (MemberAccess)typeName;
					
					var memberType = new MonoDevelop.CSharp.Ast.MemberType ();
					memberType.AddChild (ConvertToType (ma.LeftExpression), MonoDevelop.CSharp.Ast.MemberType.TargetRole);
					memberType.MemberName = ma.Name;
					
					AddTypeArguments (ma, memberType);
					return memberType;
				}
				
				if (typeName is SimpleName) {
					var sn = (SimpleName)typeName;
					var result = new SimpleType (sn.Name, Convert (sn.Location));
					AddTypeArguments (sn, result);
					return result;
				}
				
				if (typeName is ComposedCast) {
					var cc = (ComposedCast)typeName;
					var baseType = ConvertToType (cc.Left);
					var result = new ComposedType () { BaseType = baseType };
					
					if (cc.Spec.IsNullable) {
						result.HasNullableSpecifier = true;
					} else if (cc.Spec.IsPointer) {
						result.PointerRank++;
					} else {
						var location = LocationsBag.GetLocations (cc.Spec);
						var spec = new ArraySpecifier () { Dimensions = cc.Spec.Dimension - 1 };
						spec.AddChild (new CSharpTokenNode (Convert (cc.Spec.Location), 1), FieldDeclaration.Roles.LBracket);
						if (location != null)
							spec.AddChild (new CSharpTokenNode (Convert (location [0]), 1), FieldDeclaration.Roles.RBracket);
						
						result.ArraySpecifiers = new ArraySpecifier[] {
							spec
						};
					}
					return result;
				}
				
				System.Console.WriteLine ("Error while converting :" + typeName + " - unknown type name");
				System.Console.WriteLine (Environment.StackTrace);
				return new SimpleType ("unknown");
			}
			
			public override void Visit (UsingsBag.Namespace nspace)
			{
				NamespaceDeclaration nDecl = null;
				if (nspace.Name != null) {
					nDecl = new NamespaceDeclaration ();
					nDecl.AddChild (new CSharpTokenNode (Convert (nspace.NamespaceLocation), "namespace".Length), NamespaceDeclaration.Roles.Keyword);
					ConvertNamespaceName (nspace.Name, nDecl);
					nDecl.AddChild (new CSharpTokenNode (Convert (nspace.OpenBrace), 1), NamespaceDeclaration.Roles.LBrace);
					AddToNamespace (nDecl);
					namespaceStack.Push (nDecl);
					
				}
				VisitNamespaceUsings (nspace);
				VisitNamespaceBody (nspace);
				
				if (nDecl != null) {
					nDecl.AddChild (new CSharpTokenNode (Convert (nspace.CloseBrace), 1), NamespaceDeclaration.Roles.RBrace);
					if (!nspace.OptSemicolon.IsNull)
						nDecl.AddChild (new CSharpTokenNode (Convert (nspace.OptSemicolon), 1), NamespaceDeclaration.Roles.Semicolon);
					
					namespaceStack.Pop ();
				}
			}
			
			void ConvertNamespaceName (MemberName memberName, NamespaceDeclaration namespaceDecl)
			{
				AstNode insertPos = null;
				while (memberName != null) {
					Identifier newIdent = new Identifier (memberName.Name, Convert (memberName.Location));
					namespaceDecl.InsertChildBefore (insertPos, newIdent, NamespaceDeclaration.Roles.Identifier);
					insertPos = newIdent;
					memberName = memberName.Left;
				}
			}
			
			public override void Visit (UsingsBag.Using u)
			{
				UsingDeclaration ud = new UsingDeclaration ();
				ud.AddChild (new CSharpTokenNode (Convert (u.UsingLocation), "using".Length), UsingDeclaration.Roles.Keyword);
				ud.AddChild (ConvertImport (u.NSpace), UsingDeclaration.ImportRole);
				ud.AddChild (new CSharpTokenNode (Convert (u.SemicolonLocation), 1), UsingDeclaration.Roles.Semicolon);
				AddToNamespace (ud);
			}
			
			public override void Visit (UsingsBag.AliasUsing u)
			{
				UsingAliasDeclaration ud = new UsingAliasDeclaration ();
				ud.AddChild (new CSharpTokenNode (Convert (u.UsingLocation), "using".Length), UsingAliasDeclaration.Roles.Keyword);
				ud.AddChild (new Identifier (u.Identifier.Value, Convert (u.Identifier.Location)), UsingAliasDeclaration.AliasRole);
				ud.AddChild (new CSharpTokenNode (Convert (u.AssignLocation), 1), UsingAliasDeclaration.Roles.Assign);
				ud.AddChild (ConvertImport (u.Nspace), UsingAliasDeclaration.ImportRole);
				ud.AddChild (new CSharpTokenNode (Convert (u.SemicolonLocation), 1), UsingAliasDeclaration.Roles.Semicolon);
				AddToNamespace (ud);
			}
			
			AstType ConvertImport (MemberName memberName)
			{
				if (memberName.Left != null) {
					// left.name
					var t = new MonoDevelop.CSharp.Ast.MemberType();
					t.IsDoubleColon = memberName.IsDoubleColon;
					t.AddChild (ConvertImport (memberName.Left), MonoDevelop.CSharp.Ast.MemberType.TargetRole);
					t.AddChild (new Identifier (memberName.Name, Convert(memberName.Location)), MonoDevelop.CSharp.Ast.MemberType.Roles.Identifier);
					return t;
				} else {
					SimpleType t = new SimpleType();
					t.AddChild (new Identifier (memberName.Name, Convert(memberName.Location)), SimpleType.Roles.Identifier);
					// TODO type arguments
					return t;
				}
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
				newType.ClassType = ClassType.Class;
				
				var location = LocationsBag.GetMemberLocation (c);
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "class".Length), TypeDeclaration.Roles.Keyword);
				newType.AddChild (new Identifier (c.Basename, Convert (c.MemberName.Location)), AstNode.Roles.Identifier);
				if (c.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (c.MemberName);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), TypeDeclaration.Roles.LChevron);
					AddTypeParameters (newType, typeArgLocation, c.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), TypeDeclaration.Roles.RChevron);
					AddConstraints (newType, c);
				}
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AstNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (c);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AstNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Struct s)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = ClassType.Struct;
				
				var location = LocationsBag.GetMemberLocation (s);
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "struct".Length), TypeDeclaration.Roles.Keyword);
				newType.AddChild (new Identifier (s.Basename, Convert (s.MemberName.Location)), AstNode.Roles.Identifier);
				if (s.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (s.MemberName);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), TypeDeclaration.Roles.LChevron);
					AddTypeParameters (newType, typeArgLocation, s.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), TypeDeclaration.Roles.RChevron);
					AddConstraints (newType, s);
				}
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AstNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (s);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AstNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Interface i)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = ClassType.Interface;
				
				var location = LocationsBag.GetMemberLocation (i);
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "interface".Length), TypeDeclaration.Roles.Keyword);
				newType.AddChild (new Identifier (i.Basename, Convert (i.MemberName.Location)), AstNode.Roles.Identifier);
				if (i.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (i.MemberName);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeParameters (newType, typeArgLocation, i.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, i);
				}
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AstNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (i);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AstNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Mono.CSharp.Delegate d)
			{
				DelegateDeclaration newDelegate = new DelegateDeclaration ();
				var location = LocationsBag.GetMemberLocation (d);
				
				AddModifiers (newDelegate, location);
				if (location != null)
					newDelegate.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), TypeDeclaration.Roles.Keyword);
				newDelegate.AddChild (ConvertToType (d.ReturnType), AstNode.Roles.Type);
				newDelegate.AddChild (new Identifier (d.Basename, Convert (d.MemberName.Location)), AstNode.Roles.Identifier);
				if (d.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (d.MemberName);
					if (typeArgLocation != null)
						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), TypeDeclaration.Roles.LChevron);
					AddTypeParameters (newDelegate, typeArgLocation, d.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), TypeDeclaration.Roles.RChevron);
					AddConstraints (newDelegate, d);
				}
				if (location != null)
					newDelegate.AddChild (new CSharpTokenNode (Convert (location[1]), 1), DelegateDeclaration.Roles.LPar);
				AddParameter (newDelegate, d.Parameters);
				
				if (location != null) {
					newDelegate.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DelegateDeclaration.Roles.RPar);
					newDelegate.AddChild (new CSharpTokenNode (Convert (location[3]), 1), DelegateDeclaration.Roles.Semicolon);
				}
				AddType (newDelegate);
			}
			
			void AddType (AttributedNode child)
			{
				if (typeStack.Count > 0) {
					typeStack.Peek ().AddChild (child, TypeDeclaration.MemberRole);
				} else {
					AddToNamespace (child);
				}
			}
			
			void AddToNamespace (AstNode child)
			{
				if (namespaceStack.Count > 0) {
					namespaceStack.Peek ().AddChild (child, NamespaceDeclaration.MemberRole);
				} else {
					unit.AddChild (child, MonoDevelop.CSharp.Ast.CompilationUnit.MemberRole);
				}
			}
			
			public override void Visit (Mono.CSharp.Enum e)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = ClassType.Enum;
				var location = LocationsBag.GetMemberLocation (e);
				
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "enum".Length), TypeDeclaration.Roles.Keyword);
				newType.AddChild (new Identifier (e.Basename, Convert (e.MemberName.Location)), AstNode.Roles.Identifier);
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AstNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (e);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AstNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (EnumMember em)
			{
				EnumMemberDeclaration newField = new EnumMemberDeclaration ();
				VariableInitializer variable = new VariableInitializer ();
				
				variable.AddChild (new Identifier (em.Name, Convert (em.Location)), AstNode.Roles.Identifier);
				
				if (em.Initializer != null) {
					variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)em.Initializer.Accept (this), VariableInitializer.Roles.Expression);
				}
				
				newField.AddChild (variable, AstNode.Roles.Variable);
				typeStack.Peek ().AddChild (newField, TypeDeclaration.MemberRole);
			}
			#endregion
			
			#region Type members
			
			
			public override void Visit (FixedField f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				
				AddModifiers (newField, location);
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[0]), "fixed".Length), FieldDeclaration.Roles.Keyword);
				newField.AddChild (ConvertToType (f.TypeName), FieldDeclaration.Roles.Type);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
				if (!f.Initializer.IsNull) {
					variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)f.Initializer.Accept (this), FieldDeclaration.Roles.Expression);
				}
				
				newField.AddChild (variable, FieldDeclaration.Roles.Variable);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
						if (!decl.Initializer.IsNull) {
							variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)decl.Initializer.Accept (this), FieldDeclaration.Roles.Expression);
						}
						newField.AddChild (variable, FieldDeclaration.Roles.Variable);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FieldDeclaration.Roles.Semicolon);
				typeStack.Peek ().AddChild (newField, TypeDeclaration.MemberRole);
				
			}

			public override void Visit (Field f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				
				AddModifiers (newField, location);
				newField.AddChild (ConvertToType (f.TypeName), FieldDeclaration.Roles.Type);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
				
				if (f.Initializer != null) {
					if (location != null)
						variable.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FieldDeclaration.Roles.Assign);
					variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)f.Initializer.Accept (this), VariableInitializer.Roles.Expression);
				}
				newField.AddChild (variable, FieldDeclaration.Roles.Variable);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), VariableInitializer.Roles.Identifier);
						if (decl.Initializer != null) {
							variable.AddChild (new CSharpTokenNode (Convert (decl.Initializer.Location), 1), FieldDeclaration.Roles.Assign);
							variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)decl.Initializer.Accept (this), VariableInitializer.Roles.Expression);
						}
						newField.AddChild (variable, FieldDeclaration.Roles.Variable);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), FieldDeclaration.Roles.Semicolon);

				typeStack.Peek ().AddChild (newField, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (Const f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				
				AddModifiers (newField, location);
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[0]), "const".Length), FieldDeclaration.Roles.Keyword);
				newField.AddChild (ConvertToType (f.TypeName), FieldDeclaration.Roles.Type);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), VariableInitializer.Roles.Identifier);
				
				if (f.Initializer != null) {
					variable.AddChild (new CSharpTokenNode (Convert (f.Initializer.Location), 1), VariableInitializer.Roles.Assign);
					variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)f.Initializer.Accept (this), VariableInitializer.Roles.Expression);
				}
				newField.AddChild (variable, FieldDeclaration.Roles.Variable);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
						if (decl.Initializer != null) {
							variable.AddChild (new CSharpTokenNode (Convert (decl.Initializer.Location), 1), FieldDeclaration.Roles.Assign);
							variable.AddChild ((MonoDevelop.CSharp.Ast.Expression)decl.Initializer.Accept (this), VariableInitializer.Roles.Expression);
						}
						newField.AddChild (variable, FieldDeclaration.Roles.Variable);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FieldDeclaration.Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newField, TypeDeclaration.MemberRole);

				
			}
			
			public override void Visit (Operator o)
			{
				OperatorDeclaration newOperator = new OperatorDeclaration ();
				newOperator.OperatorType = (OperatorType)o.OperatorType;
				
				var location = LocationsBag.GetMemberLocation (o);
				
				AddModifiers (newOperator, location);
				
				newOperator.AddChild (ConvertToType (o.TypeName), AstNode.Roles.Type);
				
				if (o.OperatorType == Operator.OpType.Implicit) {
					if (location != null) {
						newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), "implicit".Length), OperatorDeclaration.OperatorTypeRole);
						newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
					}
				} else if (o.OperatorType == Operator.OpType.Explicit) {
					if (location != null) {
						newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), "explicit".Length), OperatorDeclaration.OperatorTypeRole);
						newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
					}
				} else {
					if (location != null)
						newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
					
					int opLength = 1;
					switch (newOperator.OperatorType) {
						case OperatorType.LeftShift:
						case OperatorType.RightShift:
						case OperatorType.LessThanOrEqual:
						case OperatorType.GreaterThanOrEqual:
						case OperatorType.Equality:
						case OperatorType.Inequality:
//					case OperatorType.LogicalAnd:
//					case OperatorType.LogicalOr:
							opLength = 2;
							break;
						case OperatorType.True:
							opLength = "true".Length;
							break;
						case OperatorType.False:
							opLength = "false".Length;
							break;
					}
					if (location != null)
						newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), opLength), OperatorDeclaration.OperatorTypeRole);
				}
				if (location != null)
					newOperator.AddChild (new CSharpTokenNode (Convert (location[2]), 1), OperatorDeclaration.Roles.LPar);
				AddParameter (newOperator, o.ParameterInfo);
				if (location != null)
					newOperator.AddChild (new CSharpTokenNode (Convert (location[3]), 1), OperatorDeclaration.Roles.RPar);
				
				if (o.Block != null)
					newOperator.AddChild ((BlockStatement)o.Block.Accept (this), OperatorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newOperator, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (Indexer indexer)
			{
				IndexerDeclaration newIndexer = new IndexerDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (indexer);
				AddModifiers (newIndexer, location);
				
				newIndexer.AddChild (ConvertToType (indexer.TypeName), AstNode.Roles.Type);
				
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IndexerDeclaration.Roles.LBracket);
				AddParameter (newIndexer, indexer.Parameters);
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IndexerDeclaration.Roles.RBracket);
				
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[2]), 1), IndexerDeclaration.Roles.LBrace);
				if (indexer.Get != null) {
					Accessor getAccessor = new Accessor ();
					var getLocation = LocationsBag.GetMemberLocation (indexer.Get);
					AddModifiers (getAccessor, getLocation);
					if (getLocation != null)
						getAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Get.Location), "get".Length), PropertyDeclaration.Roles.Keyword);
					if (indexer.Get.Block != null) {
						getAccessor.AddChild ((BlockStatement)indexer.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (getLocation != null && getLocation.Count > 0)
							newIndexer.AddChild (new CSharpTokenNode (Convert (getLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newIndexer.AddChild (getAccessor, PropertyDeclaration.GetterRole);
				}
				
				if (indexer.Set != null) {
					Accessor setAccessor = new Accessor ();
					var setLocation = LocationsBag.GetMemberLocation (indexer.Set);
					AddModifiers (setAccessor, setLocation);
					if (setLocation != null)
						setAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Set.Location), "set".Length), PropertyDeclaration.Roles.Keyword);
					
					if (indexer.Set.Block != null) {
						setAccessor.AddChild ((BlockStatement)indexer.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (setLocation != null && setLocation.Count > 0)
							newIndexer.AddChild (new CSharpTokenNode (Convert (setLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newIndexer.AddChild (setAccessor, PropertyDeclaration.SetterRole);
				}
				
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[3]), 1), IndexerDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newIndexer, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (Method m)
			{
				MethodDeclaration newMethod = new MethodDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (m);
				AddModifiers (newMethod, location);
				
				newMethod.AddChild (ConvertToType (m.TypeName), AstNode.Roles.Type);
				newMethod.AddChild (new Identifier (m.Name, Convert (m.Location)), AstNode.Roles.Identifier);
				
				if (m.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (m.MemberName);
					if (typeArgLocation != null)
						newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeParameters (newMethod, typeArgLocation, m.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					
					AddConstraints (newMethod, m.GenericMethod);
				}
				
				if (location != null)
					newMethod.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				AddParameter (newMethod, m.ParameterInfo);
				
				if (location != null)
					newMethod.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
				if (m.Block != null) {
					var bodyBlock = (BlockStatement)m.Block.Accept (this);
//					if (m.Block is ToplevelBlock) {
//						newMethod.AddChild (bodyBlock.FirstChild.NextSibling, MethodDeclaration.Roles.Body);
//					} else {
					newMethod.AddChild (bodyBlock, MethodDeclaration.Roles.Body);
//					}
				}
				typeStack.Peek ().AddChild (newMethod, TypeDeclaration.MemberRole);
			}
			
			static Dictionary<Mono.CSharp.Modifiers, MonoDevelop.Projects.Dom.Modifiers> modifierTable = new Dictionary<Mono.CSharp.Modifiers, MonoDevelop.Projects.Dom.Modifiers> ();
			static string[] keywordTable;
			
			static ConversionVisitor ()
			{
				modifierTable [Mono.CSharp.Modifiers.NEW] = MonoDevelop.Projects.Dom.Modifiers.New;
				modifierTable [Mono.CSharp.Modifiers.PUBLIC] = MonoDevelop.Projects.Dom.Modifiers.Public;
				modifierTable [Mono.CSharp.Modifiers.PROTECTED] = MonoDevelop.Projects.Dom.Modifiers.Protected;
				modifierTable [Mono.CSharp.Modifiers.PRIVATE] = MonoDevelop.Projects.Dom.Modifiers.Private;
				modifierTable [Mono.CSharp.Modifiers.INTERNAL] = MonoDevelop.Projects.Dom.Modifiers.Internal;
				modifierTable [Mono.CSharp.Modifiers.ABSTRACT] = MonoDevelop.Projects.Dom.Modifiers.Abstract;
				modifierTable [Mono.CSharp.Modifiers.VIRTUAL] = MonoDevelop.Projects.Dom.Modifiers.Virtual;
				modifierTable [Mono.CSharp.Modifiers.SEALED] = MonoDevelop.Projects.Dom.Modifiers.Sealed;
				modifierTable [Mono.CSharp.Modifiers.STATIC] = MonoDevelop.Projects.Dom.Modifiers.Static;
				modifierTable [Mono.CSharp.Modifiers.OVERRIDE] = MonoDevelop.Projects.Dom.Modifiers.Override;
				modifierTable [Mono.CSharp.Modifiers.READONLY] = MonoDevelop.Projects.Dom.Modifiers.Readonly;
				//				modifierTable[Mono.CSharp.Modifiers.] = Modifiers.Const;
				modifierTable [Mono.CSharp.Modifiers.PARTIAL] = MonoDevelop.Projects.Dom.Modifiers.Partial;
				modifierTable [Mono.CSharp.Modifiers.EXTERN] = MonoDevelop.Projects.Dom.Modifiers.Extern;
				modifierTable [Mono.CSharp.Modifiers.VOLATILE] = MonoDevelop.Projects.Dom.Modifiers.Volatile;
				modifierTable [Mono.CSharp.Modifiers.UNSAFE] = MonoDevelop.Projects.Dom.Modifiers.Unsafe;
				
				keywordTable = new string[255];
				for (int i = 0; i< keywordTable.Length; i++) 
					keywordTable [i] = "unknown";
				
				keywordTable [(int)BuiltinTypeSpec.Type.Other] = "void";
				keywordTable [(int)BuiltinTypeSpec.Type.String] = "string";
				keywordTable [(int)BuiltinTypeSpec.Type.Int] = "int";
				keywordTable [(int)BuiltinTypeSpec.Type.Object] = "object";
				keywordTable [(int)BuiltinTypeSpec.Type.Float] = "float";
				keywordTable [(int)BuiltinTypeSpec.Type.Double] = "double";
				keywordTable [(int)BuiltinTypeSpec.Type.Long] = "long";
				keywordTable [(int)BuiltinTypeSpec.Type.Byte] = "byte";
				keywordTable [(int)BuiltinTypeSpec.Type.UInt] = "uint";
				keywordTable [(int)BuiltinTypeSpec.Type.ULong] = "ulong";
				keywordTable [(int)BuiltinTypeSpec.Type.Short] = "short";
				keywordTable [(int)BuiltinTypeSpec.Type.UShort] = "ushort";
				keywordTable [(int)BuiltinTypeSpec.Type.SByte] = "sbyte";
				keywordTable [(int)BuiltinTypeSpec.Type.Decimal] = "decimal";
				keywordTable [(int)BuiltinTypeSpec.Type.Char] = "char";
			}
			
			void AddModifiers (AttributedNode parent, LocationsBag.MemberLocations location)
			{
				if (location == null || location.Modifiers == null)
					return;
				foreach (var modifier in location.Modifiers) {
					parent.AddChild (new CSharpModifierToken (Convert (modifier.Item2), modifierTable[modifier.Item1]), AttributedNode.ModifierRole);
				}
			}
			
			public override void Visit (Property p)
			{
				PropertyDeclaration newProperty = new PropertyDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (p);
				AddModifiers (newProperty, location);
				newProperty.AddChild (ConvertToType (p.TypeName), AstNode.Roles.Type);
				newProperty.AddChild (new Identifier (p.MemberName.Name, Convert (p.MemberName.Location)), AstNode.Roles.Identifier);
				if (location != null)
					newProperty.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LBrace);
				
				if (p.Get != null) {
					Accessor getAccessor = new Accessor ();
					var getLocation = LocationsBag.GetMemberLocation (p.Get);
					AddModifiers (getAccessor, getLocation);
					getAccessor.AddChild (new CSharpTokenNode (Convert (p.Get.Location), "get".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Get.Block != null) {
						getAccessor.AddChild ((BlockStatement)p.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (getLocation != null && getLocation.Count > 0)
							newProperty.AddChild (new CSharpTokenNode (Convert (getLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newProperty.AddChild (getAccessor, PropertyDeclaration.GetterRole);
				}
				
				if (p.Set != null) {
					Accessor setAccessor = new Accessor ();
					var setLocation = LocationsBag.GetMemberLocation (p.Set);
					AddModifiers (setAccessor, setLocation);
					setAccessor.AddChild (new CSharpTokenNode (Convert (p.Set.Location), "set".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Set.Block != null) {
						setAccessor.AddChild ((BlockStatement)p.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (setLocation != null && setLocation.Count > 0)
							newProperty.AddChild (new CSharpTokenNode (Convert (setLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newProperty.AddChild (setAccessor, PropertyDeclaration.SetterRole);
				}
				if (location != null)
					newProperty.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newProperty, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (Constructor c)
			{
				ConstructorDeclaration newConstructor = new ConstructorDeclaration ();
				var location = LocationsBag.GetMemberLocation (c);
				AddModifiers (newConstructor, location);
				newConstructor.AddChild (new Identifier (c.MemberName.Name, Convert (c.MemberName.Location)), AstNode.Roles.Identifier);
				if (location != null)
					newConstructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				
				AddParameter (newConstructor, c.ParameterInfo);
				if (location != null)
					newConstructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
				
				if (c.Block != null)
					newConstructor.AddChild ((BlockStatement)c.Block.Accept (this), ConstructorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newConstructor, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (Destructor d)
			{
				DestructorDeclaration newDestructor = new DestructorDeclaration ();
				var location = LocationsBag.GetMemberLocation (d);
				AddModifiers (newDestructor, location);
				if (location != null)
					newDestructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), DestructorDeclaration.TildeRole);
				newDestructor.AddChild (new Identifier (d.MemberName.Name, Convert (d.MemberName.Location)), AstNode.Roles.Identifier);
				
				if (location != null) {
					newDestructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), DestructorDeclaration.Roles.LPar);
					newDestructor.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DestructorDeclaration.Roles.RPar);
				}
				
				if (d.Block != null)
					newDestructor.AddChild ((BlockStatement)d.Block.Accept (this), DestructorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newDestructor, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (EventField e)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (e);
				AddModifiers (newEvent, location);
				
				if (location != null)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
				newEvent.AddChild (ConvertToType (e.TypeName), AstNode.Roles.Type);
				newEvent.AddChild (new Identifier (e.MemberName.Name, Convert (e.MemberName.Location)), EventDeclaration.Roles.Identifier);
				if (location != null)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), EventDeclaration.Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.MemberRole);
			}
			
			public override void Visit (EventProperty ep)
			{
				CustomEventDeclaration newEvent = new CustomEventDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (ep);
				AddModifiers (newEvent, location);
				
				if (location != null)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), CustomEventDeclaration.Roles.Keyword);
				newEvent.AddChild (ConvertToType (ep.TypeName), CustomEventDeclaration.Roles.Type);
				newEvent.AddChild (new Identifier (ep.MemberName.Name, Convert (ep.MemberName.Location)), CustomEventDeclaration.Roles.Identifier);
				if (location != null && location.Count >= 2)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), 1), CustomEventDeclaration.Roles.LBrace);
				
				if (ep.Add != null) {
					Accessor addAccessor = new Accessor ();
					var addLocation = LocationsBag.GetMemberLocation (ep.Add);
					AddModifiers (addAccessor, addLocation);
					addAccessor.AddChild (new CSharpTokenNode (Convert (ep.Add.Location), "add".Length), CustomEventDeclaration.Roles.Keyword);
					if (ep.Add.Block != null)
						addAccessor.AddChild ((BlockStatement)ep.Add.Block.Accept (this), CustomEventDeclaration.Roles.Body);
					newEvent.AddChild (addAccessor, CustomEventDeclaration.AddAccessorRole);
				}
				
				if (ep.Remove != null) {
					Accessor removeAccessor = new Accessor ();
					var removeLocation = LocationsBag.GetMemberLocation (ep.Remove);
					AddModifiers (removeAccessor, removeLocation);
					removeAccessor.AddChild (new CSharpTokenNode (Convert (ep.Remove.Location), "remove".Length), CustomEventDeclaration.Roles.Keyword);
					
					if (ep.Remove.Block != null)
						removeAccessor.AddChild ((BlockStatement)ep.Remove.Block.Accept (this), CustomEventDeclaration.Roles.Body);
					newEvent.AddChild (removeAccessor, CustomEventDeclaration.RemoveAccessorRole);
				}
				if (location != null && location.Count >= 3)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[2]), 1), CustomEventDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.MemberRole);
			}
			
			#endregion
			
			#region Statements
			public override object Visit (Mono.CSharp.Statement stmt)
			{
				Console.WriteLine ("unknown statement:" + stmt);
				return null;
			}
			
			public override object Visit (BlockVariableDeclaration blockVariableDeclaration)
			{
				var result = new VariableDeclarationStatement ();
				result.AddChild (ConvertToType (blockVariableDeclaration.TypeExpression), VariableDeclarationStatement.Roles.Type);
				
				var varInit = new VariableInitializer ();
				var location = LocationsBag.GetLocations (blockVariableDeclaration);
				varInit.AddChild (new Identifier (blockVariableDeclaration.Variable.Name, Convert (blockVariableDeclaration.Variable.Location)), VariableInitializer.Roles.Identifier);
				if (blockVariableDeclaration.Initializer != null) {
					if (location != null)
						varInit.AddChild (new CSharpTokenNode (Convert (location[0]), 1), VariableInitializer.Roles.Assign);
					varInit.AddChild ((MonoDevelop.CSharp.Ast.Expression)blockVariableDeclaration.Initializer.Accept (this), VariableInitializer.Roles.Expression);
				}
				
				result.AddChild (varInit, VariableDeclarationStatement.Roles.Variable);
				
				if (blockVariableDeclaration.Declarators != null) {
					foreach (var decl in blockVariableDeclaration.Declarators) {
						var loc = LocationsBag.GetLocations (decl);
						var init = new VariableInitializer ();
						if (loc != null && loc.Count > 0)
							result.AddChild (new CSharpTokenNode (Convert (loc [0]), 1), VariableInitializer.Roles.Comma);
						init.AddChild (new Identifier (decl.Variable.Name, Convert (decl.Variable.Location)), VariableInitializer.Roles.Identifier);
						if (decl.Initializer != null) {
							if (loc != null && loc.Count > 1)
								result.AddChild (new CSharpTokenNode (Convert (loc [1]), 1), VariableInitializer.Roles.Assign);
							init.AddChild ((MonoDevelop.CSharp.Ast.Expression)decl.Initializer.Accept (this), VariableInitializer.Roles.Expression);
						} else {
						}
						result.AddChild (init, VariableDeclarationStatement.Roles.Variable);
					}
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), VariableDeclarationStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (BlockConstantDeclaration blockVariableDeclaration)
			{
				var result = new VariableDeclarationStatement ();
				
				var location = LocationsBag.GetLocations (blockVariableDeclaration);
				if (location != null)
					result.AddChild (new CSharpModifierToken (Convert (location [0]), MonoDevelop.Projects.Dom.Modifiers.Const), VariableDeclarationStatement.ModifierRole);
				
				result.AddChild (ConvertToType (blockVariableDeclaration.TypeExpression), VariableDeclarationStatement.Roles.Type);
				
				var varInit = new VariableInitializer ();
				varInit.AddChild (new Identifier (blockVariableDeclaration.Variable.Name, Convert (blockVariableDeclaration.Variable.Location)), VariableInitializer.Roles.Identifier);
				if (blockVariableDeclaration.Initializer != null) {
					if (location != null)
						varInit.AddChild (new CSharpTokenNode (Convert (location[1]), 1), VariableInitializer.Roles.Assign);
					varInit.AddChild ((MonoDevelop.CSharp.Ast.Expression)blockVariableDeclaration.Initializer.Accept (this), VariableInitializer.Roles.Expression);
				}
				
				result.AddChild (varInit, VariableDeclarationStatement.Roles.Variable);
				
				if (blockVariableDeclaration.Declarators != null) {
					foreach (var decl in blockVariableDeclaration.Declarators) {
						var loc = LocationsBag.GetLocations (decl);
						var init = new VariableInitializer ();
						init.AddChild (new Identifier (decl.Variable.Name, Convert (decl.Variable.Location)), VariableInitializer.Roles.Identifier);
						if (decl.Initializer != null) {
							if (loc != null)
								init.AddChild (new CSharpTokenNode (Convert (loc[0]), 1), VariableInitializer.Roles.Assign);
							init.AddChild ((MonoDevelop.CSharp.Ast.Expression)decl.Initializer.Accept (this), VariableInitializer.Roles.Expression);
							if (loc != null && loc.Count > 1)
								result.AddChild (new CSharpTokenNode (Convert (loc[1]), 1), VariableInitializer.Roles.Comma);
						} else {
							if (loc != null && loc.Count > 0)
								result.AddChild (new CSharpTokenNode (Convert (loc[0]), 1), VariableInitializer.Roles.Comma);
						}
						result.AddChild (init, VariableDeclarationStatement.Roles.Variable);
					}
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), VariableDeclarationStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Mono.CSharp.EmptyStatement emptyStatement)
			{
				var result = new MonoDevelop.CSharp.Ast.EmptyStatement ();
				result.Location = Convert (emptyStatement.loc);
				return result;
			}
			
			public override object Visit (EmptyExpressionStatement emptyExpressionStatement)
			{
				return new MonoDevelop.CSharp.Ast.EmptyExpression (Convert (emptyExpressionStatement.Location));
			}
			
			public override object Visit (If ifStatement)
			{
				var result = new IfElseStatement ();
				
				var location = LocationsBag.GetLocations (ifStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (ifStatement.loc), "if".Length), IfElseStatement.IfKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IfElseStatement.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)ifStatement.Expr.Accept (this), IfElseStatement.Roles.Condition);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IfElseStatement.Roles.RPar);
				
				if (ifStatement.TrueStatement != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Statement)ifStatement.TrueStatement.Accept (this), IfElseStatement.TrueRole);
				
				if (ifStatement.FalseStatement != null) {
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[2]), "else".Length), IfElseStatement.ElseKeywordRole);
					result.AddChild ((MonoDevelop.CSharp.Ast.Statement)ifStatement.FalseStatement.Accept (this), IfElseStatement.FalseRole);
				}
				
				return result;
			}
			
			public override object Visit (Do doStatement)
			{
				var result = new DoWhileStatement ();
				var location = LocationsBag.GetLocations (doStatement);
				result.AddChild (new CSharpTokenNode (Convert (doStatement.loc), "do".Length), DoWhileStatement.DoKeywordRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)doStatement.EmbeddedStatement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "while".Length), DoWhileStatement.WhileKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), DoWhileStatement.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)doStatement.expr.Accept (this), DoWhileStatement.Roles.Condition);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DoWhileStatement.Roles.RPar);
					result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), DoWhileStatement.Roles.Semicolon);
				}
				
				return result;
			}
			
			public override object Visit (While whileStatement)
			{
				var result = new WhileStatement ();
				var location = LocationsBag.GetLocations (whileStatement);
				result.AddChild (new CSharpTokenNode (Convert (whileStatement.loc), "while".Length), WhileStatement.WhileKeywordRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), WhileStatement.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)whileStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), WhileStatement.Roles.RPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)whileStatement.Statement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			void AddStatementOrList (ForStatement forStatement, Mono.CSharp.Statement init, Role<MonoDevelop.CSharp.Ast.Statement> role)
			{
				if (init == null)
					return;
				if (init is StatementList) {
					foreach (var stmt in ((StatementList)init).Statements) {
						forStatement.AddChild ((MonoDevelop.CSharp.Ast.Statement)stmt.Accept (this), role);
					}
				} else {
					forStatement.AddChild ((MonoDevelop.CSharp.Ast.Statement)init.Accept (this), role);
				}
			}

			public override object Visit (For forStatement)
			{
				var result = new ForStatement ();
				
				var location = LocationsBag.GetLocations (forStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (forStatement.loc), "for".Length), ForStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ForStatement.Roles.LPar);
				
				AddStatementOrList (result, forStatement.InitStatement, ForStatement.InitializerRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Test != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)forStatement.Test.Accept (this), ForStatement.Roles.Condition);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), ForStatement.Roles.Semicolon);
				
				AddStatementOrList (result, forStatement.Increment, ForStatement.IteratorRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), ForStatement.Roles.RPar);
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)forStatement.Statement.Accept (this), ForStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (StatementExpression statementExpression)
			{
				var result = new MonoDevelop.CSharp.Ast.ExpressionStatement ();
				object expr = statementExpression.Expr.Accept (this);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)expr, MonoDevelop.CSharp.Ast.ExpressionStatement.Roles.Expression);
				var location = LocationsBag.GetLocations (statementExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MonoDevelop.CSharp.Ast.ExpressionStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Return returnStatement)
			{
				var result = new ReturnStatement ();
				
				result.AddChild (new CSharpTokenNode (Convert (returnStatement.loc), "return".Length), ReturnStatement.Roles.Keyword);
				if (returnStatement.Expr != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)returnStatement.Expr.Accept (this), ReturnStatement.Roles.Expression);
				
				var location = LocationsBag.GetLocations (returnStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ReturnStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (Goto gotoStatement)
			{
				var result = new GotoStatement ();
				var location = LocationsBag.GetLocations (gotoStatement);
				result.AddChild (new CSharpTokenNode (Convert (gotoStatement.loc), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new Identifier (gotoStatement.Target, Convert (gotoStatement.loc)), GotoStatement.Roles.Identifier);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), GotoStatement.Roles.Semicolon);
				
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
				var result = new GotoDefaultStatement ();
				result.AddChild (new CSharpTokenNode (Convert (gotoDefault.loc), "goto".Length), GotoDefaultStatement.Roles.Keyword);
				var location = LocationsBag.GetLocations (gotoDefault);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "default".Length), GotoDefaultStatement.DefaultKeywordRole);
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), GotoDefaultStatement.Roles.Semicolon);
				}
				
				return result;
			}
			
			public override object Visit (GotoCase gotoCase)
			{
				var result = new GotoCaseStatement ();
				result.AddChild (new CSharpTokenNode (Convert (gotoCase.loc), "goto".Length), GotoCaseStatement.Roles.Keyword);
				
				var location = LocationsBag.GetLocations (gotoCase);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "case".Length), GotoCaseStatement.CaseKeywordRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)gotoCase.Expr.Accept (this), GotoCaseStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), GotoCaseStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Throw throwStatement)
			{
				var result = new ThrowStatement ();
				var location = LocationsBag.GetLocations (throwStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (throwStatement.loc), "throw".Length), ThrowStatement.Roles.Keyword);
				if (throwStatement.Expr != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)throwStatement.Expr.Accept (this), ThrowStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ThrowStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Break breakStatement)
			{
				var result = new BreakStatement ();
				var location = LocationsBag.GetLocations (breakStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (breakStatement.loc), "break".Length), BreakStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), BreakStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Continue continueStatement)
			{
				var result = new ContinueStatement ();
				var location = LocationsBag.GetLocations (continueStatement);
				result.AddChild (new CSharpTokenNode (Convert (continueStatement.loc), "continue".Length), ContinueStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ContinueStatement.Roles.Semicolon);
				return result;
			}
			
			public static bool IsLower (Location left, Location right)
			{
				return left.Row < right.Row || left.Row == right.Row && left.Column < right.Column;
			}
			
			public UsingStatement CreateUsingStatement (Block blockStatement)
			{
				var usingResult = new UsingStatement ();
				Mono.CSharp.Statement cur = blockStatement.Statements[0];
				if (cur is Using) {
					Using u = (Using)cur;
					usingResult.AddChild (new CSharpTokenNode (Convert (u.loc), "using".Length), UsingStatement.Roles.Keyword);
					usingResult.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation), 1), UsingStatement.Roles.LPar);
					if (u.Variables != null) {
						usingResult.AddChild (ConvertToType (u.Variables.TypeExpression), UsingStatement.Roles.Type);
						usingResult.AddChild (new Identifier (u.Variables.Variable.Name, Convert (u.Variables.Variable.Location)), UsingStatement.Roles.Identifier);
						var loc = LocationsBag.GetLocations (u.Variables);
						if (loc != null)
							usingResult.AddChild (new CSharpTokenNode (Convert (loc[1]), 1), ContinueStatement.Roles.Assign);
						if (u.Variables.Initializer != null)
							usingResult.AddChild ((AstNode)u.Variables.Initializer.Accept (this), UsingStatement.ResourceAcquisitionRole);
						
					}
					cur = u.Statement;
					usingResult.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation), 1), UsingStatement.Roles.RPar);
					usingResult.AddChild ((MonoDevelop.CSharp.Ast.Statement)cur.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				}
				return usingResult;
			}
			
			void AddBlockChildren (BlockStatement result, Block blockStatement, ref int curLocal)
			{
				foreach (Mono.CSharp.Statement stmt in blockStatement.Statements) {
					if (stmt == null)
						continue;
					/*					if (curLocal < localVariables.Count && IsLower (localVariables[curLocal].Location, stmt.loc)) {
						result.AddChild (CreateVariableDeclaration (localVariables[curLocal]), AstNode.Roles.Statement);
						curLocal++;
					}*/
					if (stmt is Block && !(stmt is ToplevelBlock || stmt is ExplicitBlock)) {
						AddBlockChildren (result, (Block)stmt, ref curLocal);
					} else {
						result.AddChild ((MonoDevelop.CSharp.Ast.Statement)stmt.Accept (this), BlockStatement.StatementRole);
					}
				}
			}
			
			public override object Visit (Block blockStatement)
			{
				if (blockStatement.IsCompilerGenerated && blockStatement.Statements.Any ()) {
					if (blockStatement.Statements.First () is Using)
						return CreateUsingStatement (blockStatement);
					return blockStatement.Statements.Last ().Accept (this);
				}
				var result = new BlockStatement ();
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation), 1), AstNode.Roles.LBrace);
				int curLocal = 0;
				AddBlockChildren (result, blockStatement, ref curLocal);
				
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation), 1), AstNode.Roles.RBrace);
				return result;
			}

			public override object Visit (Switch switchStatement)
			{
				var result = new SwitchStatement ();
				
				var location = LocationsBag.GetLocations (switchStatement);
				result.AddChild (new CSharpTokenNode (Convert (switchStatement.loc), "switch".Length), SwitchStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), SwitchStatement.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)switchStatement.Expr.Accept (this), SwitchStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), SwitchStatement.Roles.RPar);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), SwitchStatement.Roles.LBrace);
				foreach (var section in switchStatement.Sections) {
					var newSection = new MonoDevelop.CSharp.Ast.SwitchSection ();
					foreach (var caseLabel in section.Labels) {
						var newLabel = new CaseLabel ();
						newLabel.AddChild (new CSharpTokenNode (Convert (caseLabel.Location), "case".Length), SwitchStatement.Roles.Keyword);
						if (caseLabel.Label != null)
							newLabel.AddChild ((MonoDevelop.CSharp.Ast.Expression)caseLabel.Label.Accept (this), SwitchStatement.Roles.Expression);
						
						newSection.AddChild (newLabel, MonoDevelop.CSharp.Ast.SwitchSection.CaseLabelRole);
					}
					
					var blockStatement = section.Block;
					var bodyBlock = new BlockStatement ();
					int curLocal = 0;
					AddBlockChildren (bodyBlock, blockStatement, ref curLocal);
					foreach (var statement in bodyBlock.Statements) {
						statement.Remove ();
						newSection.AddChild (statement, MonoDevelop.CSharp.Ast.SwitchSection.Roles.EmbeddedStatement);
						
					}
					result.AddChild (newSection, SwitchStatement.SwitchSectionRole);
				}
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), SwitchStatement.Roles.RBrace);
				return result;
			}
			
			public override object Visit (Lock lockStatement)
			{
				var result = new LockStatement ();
				var location = LocationsBag.GetLocations (lockStatement);
				result.AddChild (new CSharpTokenNode (Convert (lockStatement.loc), "lock".Length), LockStatement.Roles.Keyword);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), LockStatement.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)lockStatement.Expr.Accept (this), LockStatement.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), LockStatement.Roles.RPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)lockStatement.Statement.Accept (this), LockStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Unchecked uncheckedStatement)
			{
				var result = new UncheckedStatement ();
				result.AddChild (new CSharpTokenNode (Convert (uncheckedStatement.loc), "unchecked".Length), UncheckedStatement.Roles.Keyword);
				result.AddChild ((BlockStatement)uncheckedStatement.Block.Accept (this), UncheckedStatement.Roles.Body);
				return result;
			}
			
			
			public override object Visit (Checked checkedStatement)
			{
				var result = new CheckedStatement ();
				result.AddChild (new CSharpTokenNode (Convert (checkedStatement.loc), "checked".Length), CheckedStatement.Roles.Keyword);
				result.AddChild ((BlockStatement)checkedStatement.Block.Accept (this), CheckedStatement.Roles.Body);
				return result;
			}
			
			public override object Visit (Unsafe unsafeStatement)
			{
				var result = new UnsafeStatement ();
				result.AddChild (new CSharpTokenNode (Convert (unsafeStatement.loc), "unsafe".Length), UnsafeStatement.Roles.Keyword);
				result.AddChild ((BlockStatement)unsafeStatement.Block.Accept (this), UnsafeStatement.Roles.Body);
				return result;
			}
			
			public override object Visit (Fixed fixedStatement)
			{
				var result = new FixedStatement ();
				var location = LocationsBag.GetLocations (fixedStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (fixedStatement.loc), "fixed".Length), FixedStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FixedStatement.Roles.LPar);
				
				if (fixedStatement.Variables != null) {
//					result.AddChild (ConvertToType (fixedStatement.Variables), UsingStatement.Roles.Type);
					result.AddChild (new Identifier (fixedStatement.Variables.Variable.Name, Convert (fixedStatement.Variables.Variable.Location)), UsingStatement.Roles.Identifier);
					var loc = LocationsBag.GetLocations (fixedStatement.Variables);
					if (loc != null)
						result.AddChild (new CSharpTokenNode (Convert (loc[1]), 1), ContinueStatement.Roles.Assign);
//					if (fixedStatement.Variables.Initializer != null)
//						result.AddChild (fixedStatement.Variables.Initializer.Accept (this), UsingStatement.Roles.Variable);
				}
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FixedStatement.Roles.RPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)fixedStatement.Statement.Accept (this), FixedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (TryFinally tryFinallyStatement)
			{
				TryCatchStatement result;
				var location = LocationsBag.GetLocations (tryFinallyStatement);
				
				if (tryFinallyStatement.Stmt is TryCatch) {
					result = (TryCatchStatement)tryFinallyStatement.Stmt.Accept (this);
				} else {
					result = new TryCatchStatement ();
					result.AddChild (new CSharpTokenNode (Convert (tryFinallyStatement.loc), "try".Length), TryCatchStatement.TryKeywordRole);
					result.AddChild ((BlockStatement)tryFinallyStatement.Stmt.Accept (this), TryCatchStatement.TryBlockRole);
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "finally".Length), TryCatchStatement.FinallyKeywordRole);
				result.AddChild ((BlockStatement)tryFinallyStatement.Fini.Accept (this), TryCatchStatement.FinallyBlockRole);
				
				return result;
			}
			
			CatchClause ConvertCatch (Catch ctch)
			{
				CatchClause result = new CatchClause ();
				var location = LocationsBag.GetLocations (ctch);
				result.AddChild (new CSharpTokenNode (Convert (ctch.loc), "catch".Length), CatchClause.Roles.Keyword);
				if (ctch.TypeExpression != null) {
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CatchClause.Roles.LPar);
					
					result.AddChild (ConvertToType (ctch.TypeExpression), CatchClause.Roles.Type);
					if (ctch.Variable != null && !string.IsNullOrEmpty (ctch.Variable.Name))
						result.AddChild (new Identifier (ctch.Variable.Name, Convert (ctch.Variable.Location)), CatchClause.Roles.Identifier);
					
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), CatchClause.Roles.RPar);
				}
				
				result.AddChild ((BlockStatement)ctch.Block.Accept (this), CatchClause.Roles.Body);
				
				return result;
			}
			
			public override object Visit (TryCatch tryCatchStatement)
			{
				var result = new TryCatchStatement ();
				result.AddChild (new CSharpTokenNode (Convert (tryCatchStatement.loc), "try".Length), TryCatchStatement.TryKeywordRole);
				result.AddChild ((BlockStatement)tryCatchStatement.Block.Accept (this), TryCatchStatement.TryBlockRole);
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
				var location = LocationsBag.GetLocations (usingStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (usingStatement.loc), "using".Length), UsingStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), UsingStatement.Roles.LPar);
				
				result.AddChild ((AstNode)usingStatement.Expression.Accept (this), UsingStatement.ResourceAcquisitionRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), UsingStatement.Roles.RPar);
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)usingStatement.Statement.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Foreach foreachStatement)
			{
				var result = new ForeachStatement ();
				
				var location = LocationsBag.GetLocations (foreachStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (foreachStatement.loc), "foreach".Length), ForeachStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ForeachStatement.Roles.LPar);
				
				if (foreachStatement.TypeExpr == null)
					result.AddChild (ConvertToType (foreachStatement.TypeExpr), ForeachStatement.Roles.Type);
				if (foreachStatement.Variable != null)
					result.AddChild (new Identifier (foreachStatement.Variable.Name, Convert (foreachStatement.Variable.Location)), ForeachStatement.Roles.Identifier);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), ForeachStatement.Roles.InKeyword);
				
				if (foreachStatement.Expr != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)foreachStatement.Expr.Accept (this), ForeachStatement.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), ForeachStatement.Roles.RPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Statement)foreachStatement.Statement.Accept (this), ForeachStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Yield yieldStatement)
			{
				var result = new YieldStatement ();
				var location = LocationsBag.GetLocations (yieldStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (yieldStatement.loc), "yield".Length), YieldStatement.YieldKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "return".Length), YieldStatement.ReturnKeywordRole);
				if (yieldStatement.Expr != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)yieldStatement.Expr.Accept (this), YieldStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), YieldStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (YieldBreak yieldBreakStatement)
			{
				var result = new YieldBreakStatement ();
				var location = LocationsBag.GetLocations (yieldBreakStatement);
				result.AddChild (new CSharpTokenNode (Convert (yieldBreakStatement.loc), "yield".Length), YieldBreakStatement.YieldKeywordRole);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "break".Length), YieldBreakStatement.BreakKeywordRole);
					result.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), YieldBreakStatement.Roles.Semicolon);
				}
				return result;
			}
			#endregion
			
			#region Expression
			public override object Visit (Mono.CSharp.Expression expression)
			{
				Console.WriteLine ("Visit unknown expression:" + expression);
				System.Console.WriteLine (Environment.StackTrace);
				return null;
			}
			
			public override object Visit (Mono.CSharp.DefaultParameterValueExpression defaultParameterValueExpression)
			{
				return defaultParameterValueExpression.Child.Accept (this);
			}

			public override object Visit (TypeExpression typeExpression)
			{
				return new IdentifierExpression (keywordTable [(int)typeExpression.Type.BuiltinType], Convert (typeExpression.Location));
			}

			public override object Visit (LocalVariableReference localVariableReference)
			{
				return new Identifier (localVariableReference.Name, Convert (localVariableReference.Location));;
			}

			public override object Visit (MemberAccess memberAccess)
			{
				var result = new MemberReferenceExpression ();
				if (memberAccess.LeftExpression != null) {
					var leftExpr = memberAccess.LeftExpression.Accept (this);
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)leftExpr, MemberReferenceExpression.Roles.TargetExpression);
				}
				result.AddChild (new Identifier (memberAccess.Name, Convert (memberAccess.Location)), MemberReferenceExpression.Roles.Identifier);
				if (memberAccess.TypeArguments != null)  {
					var location = LocationsBag.GetLocations (memberAccess);
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (result, location, memberAccess.TypeArguments);
					if (location != null && location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MemberReferenceExpression.Roles.RChevron);
				}
				return result;
			}
			
			public override object Visit (Constant constant)
			{
				var result = new PrimitiveExpression (constant.GetValue (), Convert (constant.Location), constant.GetValueAsLiteral ().Length);
				return result;
			}

			public override object Visit (SimpleName simpleName)
			{
				var result = new IdentifierExpression ();
				result.AddChild (new Identifier (simpleName.Name, Convert (simpleName.Location)), IdentifierExpression.Roles.Identifier);
				if (simpleName.TypeArguments != null)  {
					var location = LocationsBag.GetLocations (simpleName);
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IdentifierExpression.Roles.LChevron);
					AddTypeArguments (result, location, simpleName.TypeArguments);
					if (location != null && location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IdentifierExpression.Roles.RChevron);
				}
				return result;
			}
			
			public override object Visit (BooleanExpression booleanExpression)
			{
				return booleanExpression.Expr.Accept (this);
			}

			
			public override object Visit (Mono.CSharp.ParenthesizedExpression parenthesizedExpression)
			{
				var result = new MonoDevelop.CSharp.Ast.ParenthesizedExpression ();
				var location = LocationsBag.GetLocations (parenthesizedExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MonoDevelop.CSharp.Ast.ParenthesizedExpression.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)parenthesizedExpression.Expr.Accept (this), MonoDevelop.CSharp.Ast.ParenthesizedExpression.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MonoDevelop.CSharp.Ast.ParenthesizedExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (Unary unaryExpression)
			{
				var result = new UnaryOperatorExpression ();
				switch (unaryExpression.Oper) {
					case Unary.Operator.UnaryPlus:
						result.Operator = UnaryOperatorType.Plus;
						break;
					case Unary.Operator.UnaryNegation:
						result.Operator = UnaryOperatorType.Minus;
						break;
					case Unary.Operator.LogicalNot:
						result.Operator = UnaryOperatorType.Not;
						break;
					case Unary.Operator.OnesComplement:
						result.Operator = UnaryOperatorType.BitNot;
						break;
					case Unary.Operator.AddressOf:
						result.Operator = UnaryOperatorType.AddressOf;
						break;
				}
				result.AddChild (new CSharpTokenNode (Convert (unaryExpression.Location), 1), UnaryOperatorExpression.OperatorRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)unaryExpression.Expr.Accept (this), UnaryOperatorExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (UnaryMutator unaryMutatorExpression)
			{
				var result = new UnaryOperatorExpression ();
				
				var expression = (MonoDevelop.CSharp.Ast.Expression)unaryMutatorExpression.Expr.Accept (this);
				switch (unaryMutatorExpression.UnaryMutatorMode) {
					case UnaryMutator.Mode.PostDecrement:
						result.Operator = UnaryOperatorType.PostDecrement;
						result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.OperatorRole);
						break;
					case UnaryMutator.Mode.PostIncrement:
						result.Operator = UnaryOperatorType.PostIncrement;
						result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.OperatorRole);
						break;
						
					case UnaryMutator.Mode.PreIncrement:
						result.Operator = UnaryOperatorType.Increment;
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.OperatorRole);
						result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
						break;
					case UnaryMutator.Mode.PreDecrement:
						result.Operator = UnaryOperatorType.Decrement;
						result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.OperatorRole);
						result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
						break;
				}
				
				return result;
			}
			
			public override object Visit (Indirection indirectionExpression)
			{
				var result = new UnaryOperatorExpression ();
				result.Operator = UnaryOperatorType.Dereference;
				var location = LocationsBag.GetLocations (indirectionExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), UnaryOperatorExpression.OperatorRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)indirectionExpression.Expr.Accept (this), UnaryOperatorExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (Is isExpression)
			{
				var result = new IsExpression ();
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)isExpression.Expr.Accept (this), IsExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (isExpression.Location), "is".Length), IsExpression.Roles.Keyword);
				result.AddChild (ConvertToType (isExpression.ProbeType), IsExpression.Roles.Type);
				return result;
			}
			
			public override object Visit (As asExpression)
			{
				var result = new AsExpression ();
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)asExpression.Expr.Accept (this), AsExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (asExpression.Location), "as".Length), AsExpression.Roles.Keyword);
				result.AddChild (ConvertToType (asExpression.ProbeType), AsExpression.Roles.Type);
				return result;
			}
			
			public override object Visit (Cast castExpression)
			{
				var result = new CastExpression ();
				var location = LocationsBag.GetLocations (castExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (castExpression.Location), 1), CastExpression.Roles.LPar);
				if (castExpression.TargetType != null)
					result.AddChild (ConvertToType (castExpression.TargetType), CastExpression.Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CastExpression.Roles.RPar);
				if (castExpression.Expr != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)castExpression.Expr.Accept (this), CastExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (ComposedCast composedCast)
			{
				var result = new ComposedType ();
				result.AddChild (ConvertToType (composedCast.Left), ComposedType.Roles.Type);
				
				var spec = composedCast.Spec;
				while (spec != null) {
					if (spec.IsNullable) {
						result.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.NullableRole);
					} else if (spec.IsPointer) {
						result.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.PointerRole);
					} else {
						var aSpec = new ArraySpecifier ();
						aSpec.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.Roles.LBracket);
						var location = LocationsBag.GetLocations (spec);
						if (location != null)
							aSpec.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.Roles.RBracket);
						result.AddChild (aSpec, ComposedType.ArraySpecifierRole);
					}
					spec = spec.Next;
				}
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.DefaultValueExpression defaultValueExpression)
			{
				var result = new MonoDevelop.CSharp.Ast.DefaultValueExpression ();
				result.AddChild (new CSharpTokenNode (Convert (defaultValueExpression.Location), "default".Length), CastExpression.Roles.Keyword);
				var location = LocationsBag.GetLocations (defaultValueExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CastExpression.Roles.LPar);
				result.AddChild (ConvertToType (defaultValueExpression.Expr), CastExpression.Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), CastExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (Binary binaryExpression)
			{
				var result = new BinaryOperatorExpression ();
				int opLength = 1;
				switch (binaryExpression.Oper) {
					case Binary.Operator.Multiply:
						result.Operator = BinaryOperatorType.Multiply;
						break;
					case Binary.Operator.Division:
						result.Operator = BinaryOperatorType.Divide;
						break;
					case Binary.Operator.Modulus:
						result.Operator = BinaryOperatorType.Modulus;
						break;
					case Binary.Operator.Addition:
						result.Operator = BinaryOperatorType.Add;
						break;
					case Binary.Operator.Subtraction:
						result.Operator = BinaryOperatorType.Subtract;
						break;
					case Binary.Operator.LeftShift:
						result.Operator = BinaryOperatorType.ShiftLeft;
						opLength = 2;
						break;
					case Binary.Operator.RightShift:
						result.Operator = BinaryOperatorType.ShiftRight;
						opLength = 2;
						break;
					case Binary.Operator.LessThan:
						result.Operator = BinaryOperatorType.LessThan;
						break;
					case Binary.Operator.GreaterThan:
						result.Operator = BinaryOperatorType.GreaterThan;
						break;
					case Binary.Operator.LessThanOrEqual:
						result.Operator = BinaryOperatorType.LessThanOrEqual;
						opLength = 2;
						break;
					case Binary.Operator.GreaterThanOrEqual:
						result.Operator = BinaryOperatorType.GreaterThanOrEqual;
						opLength = 2;
						break;
					case Binary.Operator.Equality:
						result.Operator = BinaryOperatorType.Equality;
						opLength = 2;
						break;
					case Binary.Operator.Inequality:
						result.Operator = BinaryOperatorType.InEquality;
						opLength = 2;
						break;
					case Binary.Operator.BitwiseAnd:
						result.Operator = BinaryOperatorType.BitwiseAnd;
						break;
					case Binary.Operator.ExclusiveOr:
						result.Operator = BinaryOperatorType.ExclusiveOr;
						break;
					case Binary.Operator.BitwiseOr:
						result.Operator = BinaryOperatorType.BitwiseOr;
						break;
					case Binary.Operator.LogicalAnd:
						result.Operator = BinaryOperatorType.ConditionalAnd;
						opLength = 2;
						break;
					case Binary.Operator.LogicalOr:
						result.Operator = BinaryOperatorType.ConditionalOr;
						opLength = 2;
						break;
				}
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)binaryExpression.Left.Accept (this), BinaryOperatorExpression.LeftRole);
				result.AddChild (new CSharpTokenNode (Convert (binaryExpression.Location), opLength), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)binaryExpression.Right.Accept (this), BinaryOperatorExpression.RightRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Nullable.NullCoalescingOperator nullCoalescingOperator)
			{
				var result = new BinaryOperatorExpression ();
				result.Operator = BinaryOperatorType.NullCoalescing;
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.LeftRole);
				result.AddChild (new CSharpTokenNode (Convert (nullCoalescingOperator.Location), 2), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)nullCoalescingOperator.Right.Accept (this), BinaryOperatorExpression.RightRole);
				return result;
			}
			
			public override object Visit (Conditional conditionalExpression)
			{
				var result = new ConditionalExpression ();
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)conditionalExpression.Expr.Accept (this), ConditionalExpression.Roles.Condition);
				var location = LocationsBag.GetLocations (conditionalExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (conditionalExpression.Location), 1), ConditionalExpression.QuestionMarkRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)conditionalExpression.TrueExpr.Accept (this), ConditionalExpression.TrueRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ConditionalExpression.ColonRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)conditionalExpression.FalseExpr.Accept (this), ConditionalExpression.FalseRole);
				return result;
			}
			
			void AddParameter (AstNode parent, Mono.CSharp.AParametersCollection parameters)
			{
				if (parameters == null)
					return;
				var paramLocation = LocationsBag.GetLocations (parameters);
				
				for (int i = 0; i < parameters.Count; i++) {
					if (paramLocation != null && i > 0 && i - 1 < paramLocation.Count)
						parent.AddChild (new CSharpTokenNode (Convert (paramLocation [i - 1]), 1), ParameterDeclaration.Roles.Comma);
					var p = (Parameter)parameters.FixedParameters [i];
					var location = LocationsBag.GetLocations (p);
					
					ParameterDeclaration parameterDeclarationExpression = new ParameterDeclaration ();
					switch (p.ModFlags) {
					case Parameter.Modifier.OUT:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Out;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0]), "out".Length), ParameterDeclaration.Roles.Keyword);
						break;
					case Parameter.Modifier.REF:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Ref;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0]), "ref".Length), ParameterDeclaration.Roles.Keyword);
						break;
					case Parameter.Modifier.PARAMS:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Params;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0]), "params".Length), ParameterDeclaration.Roles.Keyword);
						break;
					default:
						if (p.HasExtensionMethodModifier) {
							parameterDeclarationExpression.ParameterModifier = ParameterModifier.This;
							if (location != null)
								parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [0]), "this".Length), ParameterDeclaration.Roles.Keyword);
						}
						break;
					}
					if (p.TypeExpression != null) // lambdas may have no types (a, b) => ...
						parameterDeclarationExpression.AddChild (ConvertToType (p.TypeExpression), ParameterDeclaration.Roles.Type);
					parameterDeclarationExpression.AddChild (new Identifier (p.Name, Convert (p.Location)), ParameterDeclaration.Roles.Identifier);
					if (p.HasDefaultValue) {
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location [1]), 1), ParameterDeclaration.Roles.Assign);
						parameterDeclarationExpression.AddChild ((MonoDevelop.CSharp.Ast.Expression)p.DefaultValue.Accept (this), ParameterDeclaration.Roles.Expression);
					}
					parent.AddChild (parameterDeclarationExpression, InvocationExpression.Roles.Parameter);
				}
			}
			
			void AddTypeParameters (AstNode parent, List<Location> location, Mono.CSharp.TypeArguments typeArguments)
			{
				if (typeArguments == null || typeArguments.IsEmpty)
					return;
				for (int i = 0; i < typeArguments.Count; i++) {
					if (location != null && i > 0 && i - 1 < location.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location[i - 1]), 1), InvocationExpression.Roles.Comma);
					var arg = (TypeParameterName)typeArguments.Args[i];
					if (arg == null)
						continue;
					TypeParameterDeclaration tp = new TypeParameterDeclaration();
					// TODO: attributes
					if (arg.Variance != Variance.None)
						throw new NotImplementedException(); // TODO: variance
					tp.AddChild (new Identifier (arg.Name, Convert (arg.Location)), InvocationExpression.Roles.Identifier);
					parent.AddChild (tp, InvocationExpression.Roles.TypeParameter);
				}
			}
			
			void AddTypeArguments (AstNode parent, LocationsBag.MemberLocations location, Mono.CSharp.TypeArguments typeArguments)
			{
				if (typeArguments == null || typeArguments.IsEmpty)
					return;
				for (int i = 0; i < typeArguments.Count; i++) {
					if (location != null && i > 0 && i - 1 < location.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location[i - 1]), 1), InvocationExpression.Roles.Comma);
					var arg = typeArguments.Args[i];
					if (arg == null)
						continue;
					parent.AddChild (ConvertToType (arg), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddTypeArguments (AstNode parent, List<Location> location, Mono.CSharp.TypeArguments typeArguments)
			{
				if (typeArguments == null || typeArguments.IsEmpty)
					return;
				for (int i = 0; i < typeArguments.Count; i++) {
					if (location != null && i > 0 && i - 1 < location.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location[i - 1]), 1), InvocationExpression.Roles.Comma);
					var arg = typeArguments.Args[i];
					if (arg == null)
						continue;
					parent.AddChild (ConvertToType (arg), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddConstraints (AstNode parent, DeclSpace d)
			{
				if (d == null || d.Constraints == null)
					return;
				for (int i = 0; i < d.Constraints.Count; i++) {
					Constraints c = d.Constraints [i];
					var location = LocationsBag.GetLocations (c);
					var constraint = new Constraint ();
					constraint.AddChild (new CSharpTokenNode (Convert (location [0]), "where".Length), InvocationExpression.Roles.Keyword);
					constraint.AddChild (new Identifier (c.TypeParameter.Value, Convert (c.TypeParameter.Location)), InvocationExpression.Roles.Identifier);
					constraint.AddChild (new CSharpTokenNode (Convert (location [1]), 1), Constraint.ColonRole);
					foreach (var expr in c.ConstraintExpressions)
						constraint.AddChild (ConvertToType (expr), Constraint.BaseTypeRole);
					parent.AddChild (constraint, AstNode.Roles.Constraint);
				}
			}
			
			void AddArguments (AstNode parent, object location, Mono.CSharp.Arguments args)
			{
				if (args == null)
					return;
				
				var commaLocations = LocationsBag.GetLocations (args);
				
				for (int i = 0; i < args.Count; i++) {
					Argument arg = args[i];
					if (arg.ArgType == Argument.AType.Out || arg.ArgType == Argument.AType.Ref) {
						DirectionExpression direction = new DirectionExpression ();
						direction.FieldDirection = arg.ArgType == Argument.AType.Out ? FieldDirection.Out : FieldDirection.Ref;
						var argLocation = LocationsBag.GetLocations (arg);
						if (argLocation != null)
							direction.AddChild (new CSharpTokenNode (Convert (argLocation[0]), "123".Length), InvocationExpression.Roles.Keyword);
						direction.AddChild ((MonoDevelop.CSharp.Ast.Expression)arg.Expr.Accept (this), InvocationExpression.Roles.Expression);
						
						parent.AddChild (direction, InvocationExpression.Roles.Argument);
					} else {
						parent.AddChild ((MonoDevelop.CSharp.Ast.Expression)arg.Expr.Accept (this), InvocationExpression.Roles.Argument);
					}
					if (commaLocations != null && i > 0) {
						int idx = commaLocations.Count - i;
						if (idx >= 0)
							parent.AddChild (new CSharpTokenNode (Convert (commaLocations[idx]), 1), InvocationExpression.Roles.Comma);
					}
				}
				if (commaLocations != null && commaLocations.Count > args.Count)
					parent.AddChild (new CSharpTokenNode (Convert (commaLocations[0]), 1), InvocationExpression.Roles.Comma);
			}
			
			public override object Visit (Invocation invocationExpression)
			{
				var result = new InvocationExpression ();
				var location = LocationsBag.GetLocations (invocationExpression);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)invocationExpression.Expression.Accept (this), InvocationExpression.Roles.TargetExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), InvocationExpression.Roles.LPar);
				AddArguments (result, location, invocationExpression.Arguments);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), InvocationExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (New newExpression)
			{
				var result = new ObjectCreateExpression ();
				
				var location = LocationsBag.GetLocations (newExpression);
				result.AddChild (new CSharpTokenNode (Convert (newExpression.Location), "new".Length), ObjectCreateExpression.Roles.Keyword);
				
				if (newExpression.TypeRequested != null)
					result.AddChild (ConvertToType (newExpression.TypeRequested), ObjectCreateExpression.Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ObjectCreateExpression.Roles.LPar);
				AddArguments (result, location, newExpression.Arguments);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ObjectCreateExpression.Roles.RPar);
				
				// TODO: Collection initializer ?
				
				return result;
			}
			
			
			public override object Visit (NewInitialize newInitializeExpression)
			{
				var result = new ObjectCreateExpression ();
				result.AddChild (new CSharpTokenNode (Convert (newInitializeExpression.Location), "new".Length), ObjectCreateExpression.Roles.Keyword);
				
				if (newInitializeExpression.TypeRequested != null)
					result.AddChild (ConvertToType (newInitializeExpression.TypeRequested), ObjectCreateExpression.Roles.Type);
				
				var location = LocationsBag.GetLocations (newInitializeExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ObjectCreateExpression.Roles.LPar);
				AddArguments (result, location, newInitializeExpression.Arguments);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ObjectCreateExpression.Roles.RPar);
				
				return result;
			}
			
			
			public override object Visit (ArrayCreation arrayCreationExpression)
			{
				var result = new ArrayCreateExpression ();
				
				var location = LocationsBag.GetLocations (arrayCreationExpression);
				result.AddChild (new CSharpTokenNode (Convert (arrayCreationExpression.Location), "new".Length), ArrayCreateExpression.Roles.Keyword);
				
				if (arrayCreationExpression.NewType != null)
					result.AddChild (ConvertToType (arrayCreationExpression.NewType), ArrayCreateExpression.Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ArrayCreateExpression.Roles.LBracket);
				if (arrayCreationExpression.Arguments != null) {
					var commaLocations = LocationsBag.GetLocations (arrayCreationExpression.Arguments);
					for (int i = 0 ;i < arrayCreationExpression.Arguments.Count; i++) {
						result.AddChild ((MonoDevelop.CSharp.Ast.Expression)arrayCreationExpression.Arguments[i].Accept (this), ArrayCreateExpression.Roles.Argument);
						if (commaLocations != null && i > 0)
							result.AddChild (new CSharpTokenNode (Convert (commaLocations [commaLocations.Count - i]), 1), ArrayCreateExpression.Roles.Comma);
					}
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ArrayCreateExpression.Roles.RBracket);
				
				if (arrayCreationExpression.Initializers != null && arrayCreationExpression.Initializers.Count != 0) {
					//throw new NotImplementedException();
					/* TODO: use ArrayInitializerExpression
					var initLocation = LocationsBag.GetLocations (arrayCreationExpression.Initializers);
					result.AddChild (new CSharpTokenNode (Convert (arrayCreationExpression.Initializers.Location), 1), ArrayCreateExpression.Roles.LBrace);
					var commaLocations = LocationsBag.GetLocations (arrayCreationExpression.Initializers.Elements);
					for (int i = 0; i < arrayCreationExpression.Initializers.Count; i++) {
						result.AddChild ((AstNode)arrayCreationExpression.Initializers[i].Accept (this), ObjectCreateExpression.Roles.Variable);
						if (commaLocations != null && i > 0) {
							result.AddChild (new CSharpTokenNode (Convert (commaLocations [commaLocations.Count - i]), 1), IndexerExpression.Roles.Comma);
						}
					}
					
					if (initLocation != null)
						result.AddChild (new CSharpTokenNode (Convert (initLocation[initLocation.Count - 1]), 1), ArrayCreateExpression.Roles.RBrace); */
				}
				
				return result;
			}
			
			public override object Visit (This thisExpression)
			{
				var result = new ThisReferenceExpression ();
				result.Location = Convert (thisExpression.Location);
				return result;
			}
			
			public override object Visit (ArglistAccess argListAccessExpression)
			{
				var result = new ArgListExpression ();
				result.IsAccess = true;
				result.AddChild (new CSharpTokenNode (Convert (argListAccessExpression.Location), "__arglist".Length), ArgListExpression.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Arglist argListExpression)
			{
				var result = new ArgListExpression ();
				result.AddChild (new CSharpTokenNode (Convert (argListExpression.Location), "__arglist".Length), ArgListExpression.Roles.Keyword);
				var location = LocationsBag.GetLocations (argListExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ArgListExpression.Roles.LPar);
				
				AddArguments (result, location, argListExpression.Arguments);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ArgListExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (TypeOf typeOfExpression)
			{
				var result = new TypeOfExpression ();
				var location = LocationsBag.GetLocations (typeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (typeOfExpression.Location), "typeof".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild (ConvertToType (typeOfExpression.TypeExpression), TypeOfExpression.Roles.Type);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (SizeOf sizeOfExpression)
			{
				var result = new SizeOfExpression ();
				var location = LocationsBag.GetLocations (sizeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (sizeOfExpression.Location), "sizeof".Length), TypeOfExpression.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild (ConvertToType (sizeOfExpression.QueriedType), TypeOfExpression.Roles.Type);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (CheckedExpr checkedExpression)
			{
				var result = new CheckedExpression ();
				var location = LocationsBag.GetLocations (checkedExpression);
				result.AddChild (new CSharpTokenNode (Convert (checkedExpression.Location), "checked".Length), TypeOfExpression.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)checkedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (UnCheckedExpr uncheckedExpression)
			{
				var result = new UncheckedExpression ();
				var location = LocationsBag.GetLocations (uncheckedExpression);
				result.AddChild (new CSharpTokenNode (Convert (uncheckedExpression.Location), "unchecked".Length), TypeOfExpression.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)uncheckedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (ElementAccess elementAccessExpression)
			{
				IndexerExpression result = new IndexerExpression ();
				var location = LocationsBag.GetLocations (elementAccessExpression);
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)elementAccessExpression.Expr.Accept (this), IndexerExpression.Roles.TargetExpression);
				result.AddChild (new CSharpTokenNode (Convert (elementAccessExpression.Location), 1), TypeOfExpression.Roles.LBracket);
				AddArguments (result, location, elementAccessExpression.Arguments);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.RBracket);
				return result;
			}
			
			public override object Visit (BaseThis baseAccessExpression)
			{
				var result = new BaseReferenceExpression ();
				result.Location = Convert (baseAccessExpression.Location);
				return result;
			}
			
			public override object Visit (StackAlloc stackAllocExpression)
			{
				var result = new StackAllocExpression ();
				
				var location = LocationsBag.GetLocations (stackAllocExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "stackalloc".Length), StackAllocExpression.Roles.Keyword);
				result.AddChild (ConvertToType (stackAllocExpression.TypeExpression), StackAllocExpression.Roles.Type);
				if (location != null && location.Count > 1)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), StackAllocExpression.Roles.LBracket);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)stackAllocExpression.CountExpression.Accept (this), StackAllocExpression.Roles.Expression);
				if (location != null && location.Count > 2)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), StackAllocExpression.Roles.RBracket);
				return result;
			}
			
			public override object Visit (SimpleAssign simpleAssign)
			{
				var result = new AssignmentExpression ();
				
				result.Operator = AssignmentOperatorType.Assign;
				if (simpleAssign.Target != null)
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)simpleAssign.Target.Accept (this), AssignmentExpression.LeftRole);
				result.AddChild (new CSharpTokenNode (Convert (simpleAssign.Location), 1), AssignmentExpression.OperatorRole);
				
				if (simpleAssign.Source != null) {
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)simpleAssign.Source.Accept (this), AssignmentExpression.RightRole);
				}
				return result;
			}
			
			public override object Visit (CompoundAssign compoundAssign)
			{
				var result = new AssignmentExpression ();
				int opLength = 2;
				switch (compoundAssign.Op) {
					case Binary.Operator.Multiply:
						result.Operator = AssignmentOperatorType.Multiply;
						break;
					case Binary.Operator.Division:
						result.Operator = AssignmentOperatorType.Divide;
						break;
					case Binary.Operator.Modulus:
						result.Operator = AssignmentOperatorType.Modulus;
						break;
					case Binary.Operator.Addition:
						result.Operator = AssignmentOperatorType.Add;
						break;
					case Binary.Operator.Subtraction:
						result.Operator = AssignmentOperatorType.Subtract;
						break;
					case Binary.Operator.LeftShift:
						result.Operator = AssignmentOperatorType.ShiftLeft;
						opLength = 3;
						break;
					case Binary.Operator.RightShift:
						result.Operator = AssignmentOperatorType.ShiftRight;
						opLength = 3;
						break;
					case Binary.Operator.BitwiseAnd:
						result.Operator = AssignmentOperatorType.BitwiseAnd;
						break;
					case Binary.Operator.BitwiseOr:
						result.Operator = AssignmentOperatorType.BitwiseOr;
						break;
					case Binary.Operator.ExclusiveOr:
						result.Operator = AssignmentOperatorType.ExclusiveOr;
						break;
				}
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)compoundAssign.Target.Accept (this), AssignmentExpression.LeftRole);
				result.AddChild (new CSharpTokenNode (Convert (compoundAssign.Location), opLength), AssignmentExpression.OperatorRole);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)compoundAssign.Source.Accept (this), AssignmentExpression.RightRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.AnonymousMethodExpression anonymousMethodExpression)
			{
				var result = new MonoDevelop.CSharp.Ast.AnonymousMethodExpression ();
				var location = LocationsBag.GetLocations (anonymousMethodExpression);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), AssignmentExpression.Roles.Keyword);
					
					if (location.Count > 1) {
						result.HasParameterList = true;
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AssignmentExpression.Roles.LPar);
						AddParameter (result, anonymousMethodExpression.Parameters);
						result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AssignmentExpression.Roles.RPar);
					}
				}
				
				result.AddChild ((BlockStatement)anonymousMethodExpression.Block.Accept (this), AssignmentExpression.Roles.Body);
				return result;
			}

			public override object Visit (Mono.CSharp.LambdaExpression lambdaExpression)
			{
				var result = new MonoDevelop.CSharp.Ast.LambdaExpression ();
				var location = LocationsBag.GetLocations (lambdaExpression);
				
				if (location == null || location.Count == 1) {
					AddParameter (result, lambdaExpression.Parameters);
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location [0]), "=>".Length), MonoDevelop.CSharp.Ast.LambdaExpression.ArrowRole);
				} else {
					result.AddChild (new CSharpTokenNode (Convert (lambdaExpression.Location), 1), MonoDevelop.CSharp.Ast.LambdaExpression.Roles.LPar);
					AddParameter (result, lambdaExpression.Parameters);
					if (location != null) {
						result.AddChild (new CSharpTokenNode (Convert (location [0]), 1), MonoDevelop.CSharp.Ast.LambdaExpression.Roles.RPar);
						result.AddChild (new CSharpTokenNode (Convert (location [1]), "=>".Length), MonoDevelop.CSharp.Ast.LambdaExpression.ArrowRole);
					}
				}
				if (lambdaExpression.Block.IsCompilerGenerated) {
					ContextualReturn generatedReturn = (ContextualReturn)lambdaExpression.Block.Statements [0];
					result.AddChild ((AstNode)generatedReturn.Expr.Accept (this), MonoDevelop.CSharp.Ast.LambdaExpression.BodyRole);
				} else {
					result.AddChild ((AstNode)lambdaExpression.Block.Accept (this), MonoDevelop.CSharp.Ast.LambdaExpression.BodyRole);
				}
				
				return result;
			}
			
			public override object Visit (ConstInitializer constInitializer)
			{
				return constInitializer.Expr.Accept (this);
			}
			
			public override object Visit (ArrayInitializer arrayInitializer)
			{
				var result = new ArrayInitializerExpression ();
				var location = LocationsBag.GetLocations (arrayInitializer);
				result.AddChild (new CSharpTokenNode (Convert (arrayInitializer.Location), "{".Length), ArrayInitializerExpression.Roles.LBrace);
				var commaLocations = LocationsBag.GetLocations (arrayInitializer.Elements);
				for (int i = 0; i < arrayInitializer.Count; i++) {
					result.AddChild ((MonoDevelop.CSharp.Ast.Expression)arrayInitializer[i].Accept (this), ArrayInitializerExpression.Roles.Expression);
					if (commaLocations != null && i < commaLocations.Count)
						result.AddChild (new CSharpTokenNode (Convert (commaLocations[i]), ",".Length), ArrayInitializerExpression.Roles.Comma);
				}
				
				if (location != null) {
					if (location.Count == 2) // optional comma
						result.AddChild (new CSharpTokenNode (Convert (location[1]), ",".Length), ArrayInitializerExpression.Roles.Comma);
					result.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), "}".Length), ArrayInitializerExpression.Roles.RBrace);
				}
				return result;
			}
			
			#endregion
			
			#region LINQ expressions
/*			public override object Visit (Mono.CSharp.Linq.Query queryExpression)
			{
				var result = new QueryFromClause ();
				var location = LocationsBag.GetLocations (queryExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "from".Length), QueryFromClause.FromKeywordRole);
				// TODO: select identifier
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryFromClause.InKeywordRole);
				var query = queryExpression.Expr as Mono.CSharp.Linq.AQueryClause;
//				if (query != null && query.Expr != null)
//					result.AddChild ((AstNode)query.Expr.Accept (this), QueryFromClause.Roles.Expression);
				return result;
			}				 */
			
			public override object Visit (Mono.CSharp.Linq.SelectMany selectMany)
			{
				var result = new QueryFromClause ();
				// TODO:
//				Mono.CSharp.Linq.Cast cast = selectMany.Expr as Mono.CSharp.Linq.Cast;
				var location = LocationsBag.GetLocations (selectMany);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "from".Length), QueryFromClause.FromKeywordRole);
				
				//					result.AddChild ((AstNode)cast.TypeExpr.Accept (this), QueryFromClause.Roles.ReturnType);
//				if (cast != null)
				
//				result.AddChild (new Identifier (selectMany.SelectIdentifier.Value, Convert (selectMany.SelectIdentifier.Location)), QueryFromClause.Roles.Identifier);
				//				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryFromClause.InKeywordRole);
				
				//				result.AddChild ((AstNode)(cast != null ? cast.Expr : selectMany.Expr).Accept (this), QueryFromClause.Roles.Expression);
				
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Select sel)
			{
				var result = new QuerySelectClause ();
				var location = LocationsBag.GetLocations (sel);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "select".Length), QueryWhereClause.Roles.Keyword);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)sel.Expr.Accept (this), QueryWhereClause.Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupBy groupBy)
			{
				var result = new QueryGroupClause ();
				var location = LocationsBag.GetLocations (groupBy);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "group".Length), QueryGroupClause.GroupKeywordRole);
//				result.AddChild ((AstNode)groupBy.ElementSelector.Accept (this), QueryGroupClause.GroupByExpressionRole);
//				if (location != null)
//					result.AddChild (new CSharpTokenNode (Convert (location[1]), "by".Length), QueryGroupClause.ByKeywordRole);
//				result.AddChild ((AstNode)groupBy.Expr.Accept (this), QueryGroupClause.ProjectionExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Let l)
			{
				var result = new QueryLetClause ();
				var location = LocationsBag.GetLocations (l);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "let".Length), QueryWhereClause.Roles.Keyword);
				
				NewAnonymousType aType = l.Expr as NewAnonymousType;
				AnonymousTypeParameter param = ((AnonymousTypeParameter)aType.Parameters[1]);
				result.AddChild (new Identifier (param.Name, Convert (param.Location)), Identifier.Roles.Identifier);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), QueryWhereClause.Roles.Assign);
				
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)param.Expr.Accept (this), QueryWhereClause.Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Where w)
			{
				var result = new QueryWhereClause ();
				var location = LocationsBag.GetLocations (w);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "where".Length), QueryWhereClause.Roles.Keyword);
				result.AddChild ((MonoDevelop.CSharp.Ast.Expression)w.Expr.Accept (this), QueryWhereClause.Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Join join)
			{
				var result = new QueryJoinClause ();
		/*		var location = LocationsBag.GetLocations (join);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "join".Length), QueryJoinClause.JoinKeywordRole);
				
				result.AddChild (new Identifier (join.JoinVariable.Name, Convert (join.JoinVariable.Location)), Identifier.Roles.Identifier);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryJoinClause.InKeywordRole);
				
				result.AddChild ((AstNode)join.Expr.Accept (this), QueryJoinClause.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), "on".Length), QueryJoinClause.OnKeywordRole);
				// TODO: on expression
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), "equals".Length), QueryJoinClause.EqualsKeywordRole);
				// TODO: equals expression
				*/
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupJoin groupJoin)
			{
				var result = new QueryJoinClause ();
/*				var location = LocationsBag.GetLocations (groupJoin);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "join".Length), QueryJoinClause.JoinKeywordRole);
				
				result.AddChild (new Identifier (groupJoin.JoinVariable.Name, Convert (groupJoin.JoinVariable.Location)), Identifier.Roles.Identifier);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryJoinClause.InKeywordRole);
				
				result.AddChild ((AstNode)groupJoin.Expr.Accept (this), QueryJoinClause.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), "on".Length), QueryJoinClause.OnKeywordRole);
				// TODO: on expression
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), "equals".Length), QueryJoinClause.EqualsKeywordRole);
				// TODO: equals expression
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[4]), "into".Length), QueryJoinClause.IntoKeywordRole);
				
				result.AddChild (new Identifier (groupJoin.JoinVariable.Name, Convert (groupJoin.JoinVariable.Location)), Identifier.Roles.Identifier);*/
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByAscending orderByAscending)
			{
				var result = new QueryOrderClause ();
			/*	result.OrderAscending = true;
				result.AddChild ((AstNode)orderByAscending.Expr.Accept (this), QueryWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (orderByAscending);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "ascending".Length), QueryWhereClause.Roles.Keyword);*/
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByDescending orderByDescending)
			{
				var result = new QueryOrderClause ();
		/*		result.OrderAscending = false;
				result.AddChild ((AstNode)orderByDescending.Expr.Accept (this), QueryWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (orderByDescending);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "descending".Length), QueryWhereClause.Roles.Keyword);*/
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByAscending thenByAscending)
			{
				var result = new QueryOrderClause ();
			/*	result.OrderAscending = true;
				result.AddChild ((AstNode)thenByAscending.Expr.Accept (this), QueryWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (thenByAscending);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "ascending".Length), QueryWhereClause.Roles.Keyword);*/
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByDescending thenByDescending)
			{
				var result = new QueryOrderClause ();
/*				result.OrderAscending = false;
				result.AddChild ((AstNode)thenByDescending.Expr.Accept (this), QueryWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (thenByDescending);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "descending".Length), QueryWhereClause.Roles.Keyword);*/
				return result;
			}
			#endregion
		}

		public static void InsertComment (AstNode node, MonoDevelop.CSharp.Ast.Comment comment)
		{
			if (node.EndLocation < comment.StartLocation) {
				node.AddChild (comment, AstNode.Roles.Comment);
				return;
			}
			
			foreach (var child in node.Children) {
				if (child.StartLocation < comment.StartLocation && comment.StartLocation < child.EndLocation) {
					InsertComment (child, comment);
					return;
				}
				if (comment.StartLocation < child.StartLocation) {
					node.InsertChildBefore (child, comment, AstNode.Roles.Comment);
					return;
				}
			}
			
			node.AddChild (comment, AstNode.Roles.Comment);
		}
		
		static void InsertComments (CompilerCompilationUnit top, ConversionVisitor conversionVisitor)
		{
			foreach (var special in top.SpecialsBag.Specials) {
				var comment = special as SpecialsBag.Comment;
				
				if (comment != null) {
					var type = (MonoDevelop.CSharp.Ast.CommentType)comment.CommentType;
					var start = new AstLocation (comment.Line, comment.Col);
					var end = new AstLocation (comment.EndLine, comment.EndCol);
					var domComment = new MonoDevelop.CSharp.Ast.Comment (type, start, end);
					domComment.StartsLine = comment.StartsLine;
					domComment.Content = comment.Content;
					InsertComment (conversionVisitor.Unit, domComment);
				}
			}
		}
		
		internal static MonoDevelop.CSharp.Ast.CompilationUnit Parse (CompilerCompilationUnit top)
		{
			if (top == null)
				return null;
			CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor (top.LocationsBag);
			top.UsingsBag.Global.Accept (conversionVisitor);
			InsertComments (top, conversionVisitor);
			return conversionVisitor.Unit;
		}

		McsParser.ErrorReportPrinter errorReportPrinter = new McsParser.ErrorReportPrinter ();
		
		public McsParser.ErrorReportPrinter ErrorReportPrinter {
			get {
				return errorReportPrinter;
			}
		}
		
		public bool HasErrors {
			get {
				return errorReportPrinter.ErrorsCount + errorReportPrinter.FatalCounter > 0;
			}
		}
		
		public bool HasWarnings {
			get {
				return errorReportPrinter.WarningsCount > 0;
			}
		}
		
		public MonoDevelop.CSharp.Ast.CompilationUnit Parse (TextEditorData data)
		{
			lock (CompilerCallableEntryPoint.parseLock) {
				CompilerCompilationUnit top;
				using (Stream stream = data.OpenStream ()) {
					top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v", "-unsafe"}, stream, data.Document.FileName ?? "empty.cs", errorReportPrinter);
				}
	
				return Parse (top);
			}
		}
		
		public MonoDevelop.CSharp.Ast.CompilationUnit Parse (TextReader reader)
		{
			// TODO: can we optimize this to avoid the text->stream->text roundtrip?
			using (MemoryStream stream = new MemoryStream ()) {
				StreamWriter w = new StreamWriter(stream, Encoding.UTF8);
				char[] buffer = new char[2048];
				int read;
				while ((read = reader.ReadBlock(buffer, 0, buffer.Length)) > 0)
					w.Write(buffer, 0, read);
				w.Flush(); // we can't close the StreamWriter because that would also close the MemoryStream
				stream.Position = 0;
				return Parse(stream);
			}
		}
		
		public MonoDevelop.CSharp.Ast.CompilationUnit Parse (Stream stream)
		{
			lock (CompilerCallableEntryPoint.parseLock) {
				CompilerCompilationUnit top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v", "-unsafe"}, stream, "parsed.cs", Console.Out);
				
				if (top == null)
					return null;
				CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor (top.LocationsBag);
				top.UsingsBag.Global.Accept (conversionVisitor);
				InsertComments (top, conversionVisitor);
				return conversionVisitor.Unit;
			}
		}
		
		public IEnumerable<AttributedNode> ParseTypeMembers(TextReader reader)
		{
			string code = "class MyClass { " + reader.ReadToEnd() + "}";
			var cu = Parse(new StringReader(code));
			var td = cu.Children.FirstOrDefault() as TypeDeclaration;
			if (td != null)
				return td.Members;
			return Enumerable.Empty<AttributedNode> ();
		}
		
		public IEnumerable<MonoDevelop.CSharp.Ast.Statement> ParseStatements(TextReader reader)
		{
			string code = "void M() { " + reader.ReadToEnd() + "}";
			var members = ParseTypeMembers(new StringReader(code));
			var method = members.FirstOrDefault() as MethodDeclaration;
			if (method != null && method.Body != null)
				return method.Body.Statements;
			return Enumerable.Empty<MonoDevelop.CSharp.Ast.Statement> ();
		}
		
		public AstType ParseTypeReference(TextReader reader)
		{
			// TODO: add support for parsing type references
			throw new NotImplementedException();
		}
		
		public AstNode ParseExpression(TextReader reader)
		{
			var es = ParseStatements(new StringReader("tmp = " + reader.ReadToEnd() + ";")).FirstOrDefault() as MonoDevelop.CSharp.Ast.ExpressionStatement;
			if (es != null) {
				AssignmentExpression ae = es.Expression as AssignmentExpression;
				if (ae != null)
					return ae.Right;
			}
			return null;
		}
		
		/// <summary>
		/// Parses a file snippet; guessing what the code snippet represents (compilation unit, type members, block, type reference, expression).
		/// </summary>
		public AstNode ParseSnippet(TextReader reader)
		{
			// TODO: add support for parsing a part of a file
			throw new NotImplementedException();
		}
	}
}
