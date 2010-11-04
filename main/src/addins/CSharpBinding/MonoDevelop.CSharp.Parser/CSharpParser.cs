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
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Parser
{
	public class CSharpParser
	{
		class ConversionVisitor : StructuralVisitor
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
			
			public LocationsBag LocationsBag  {
				get;
				private set;
			}
			
			public ConversionVisitor (LocationsBag locationsBag)
			{
				this.LocationsBag = locationsBag;
			}
			
			public static DomLocation Convert (Mono.CSharp.Location loc)
			{
				return new DomLocation (loc.Row, loc.Column);
			}
			
			#region Global
			Stack<NamespaceDeclaration> namespaceStack = new Stack<NamespaceDeclaration> ();
			
			public override void Visit (UsingsBag.Namespace nspace)
			{
				NamespaceDeclaration nDecl = null;
				if (nspace.Name != null) {
					nDecl = new NamespaceDeclaration ();
					nDecl.AddChild (new CSharpTokenNode (Convert (nspace.NamespaceLocation), "namespace".Length), NamespaceDeclaration.Roles.Keyword);
					nDecl.AddChild (new Identifier (nspace.Name.Name, Convert (nspace.Name.Location)), NamespaceDeclaration.Roles.Identifier);
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
			
			public override void Visit (UsingsBag.Using u)
			{
				UsingDeclaration ud = new UsingDeclaration ();
				ud.AddChild (new CSharpTokenNode (Convert (u.UsingLocation), "using".Length), UsingDeclaration.Roles.Keyword);
				ud.AddChild (new Identifier (u.NSpace.Name, Convert (u.NSpace.Location)), UsingDeclaration.Roles.Identifier);
				ud.AddChild (new CSharpTokenNode (Convert (u.SemicolonLocation), 1), UsingDeclaration.Roles.Semicolon);
				AddToNamespace (ud);
			}
			
			public override void Visit (UsingsBag.AliasUsing u)
			{
				UsingAliasDeclaration ud = new UsingAliasDeclaration ();
				ud.AddChild (new CSharpTokenNode (Convert (u.UsingLocation), "using".Length), UsingAliasDeclaration.Roles.Keyword);
				ud.AddChild (new Identifier (u.Identifier.Value, Convert (u.Identifier.Location)), UsingAliasDeclaration.AliasRole);
				ud.AddChild (new CSharpTokenNode (Convert (u.AssignLocation), 1), UsingAliasDeclaration.Roles.Assign);
				ud.AddChild (new Identifier (u.Nspace.Name, Convert (u.Nspace.Location)), UsingAliasDeclaration.Roles.Identifier);
				ud.AddChild (new CSharpTokenNode (Convert (u.SemicolonLocation), 1), UsingAliasDeclaration.Roles.Semicolon);
				AddToNamespace (ud);
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
				 
				var location = LocationsBag.GetMemberLocation (c);
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "class".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (c.Name, Convert (c.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (c.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (c.MemberName);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
//					AddTypeArguments (newType, typeArgLocation, c.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, c);
				}
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (c);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Struct s)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Struct;
				
				var location = LocationsBag.GetMemberLocation (s);
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "struct".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (s.Name, Convert (s.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (s.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (s.MemberName);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
//					AddTypeArguments (newType, typeArgLocation, s.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, s);
				}
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (s);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Interface i)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Interface;
				
				var location = LocationsBag.GetMemberLocation (i);
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "interface".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (i.Name, Convert (i.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (i.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (i.MemberName);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
//					AddTypeArguments (newType, typeArgLocation, i.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, i);
				}
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (i);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (Mono.CSharp.Delegate d)
			{
				DelegateDeclaration newDelegate = new DelegateDeclaration ();
				var location = LocationsBag.GetMemberLocation (d);
				
				AddModifiers (newDelegate, location);
				if (location != null)
					newDelegate.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), TypeDeclaration.TypeKeyword);
				newDelegate.AddChild ((INode)d.ReturnType.Accept (this), AbstractNode.Roles.ReturnType);
				newDelegate.AddChild (new Identifier (d.Name, Convert (d.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (d.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (d.MemberName);
					if (typeArgLocation != null)
						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
//					AddTypeArguments (newDelegate, typeArgLocation, d.MemberName.TypeArguments);
					if (typeArgLocation != null)
						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
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
			
			void AddType (INode child)
			{
				if (typeStack.Count > 0) {
					typeStack.Peek ().AddChild (child, AbstractNode.Roles.Member);
				} else {
					AddToNamespace (child);
				}
			}
			
			void AddToNamespace (INode child)
			{
				if (namespaceStack.Count > 0) {
					namespaceStack.Peek ().AddChild (child, NamespaceDeclaration.Roles.Member);
				} else {
					unit.AddChild (child);
				}	
			}
			
			public override void Visit (Mono.CSharp.Enum e)
			{
				TypeDeclaration newType = new TypeDeclaration ();
				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Enum;
				var location = LocationsBag.GetMemberLocation (e);
				
				AddModifiers (newType, location);
				if (location != null)
					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "enum".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (e.Name, Convert (e.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (location != null && location.Count > 1)
					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
				typeStack.Push (newType);
				base.Visit (e);
				if (location != null && location.Count > 2)
					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
				typeStack.Pop ();
				AddType (newType);
			}
			
			public override void Visit (EnumMember em)
			{
				FieldDeclaration newField = new FieldDeclaration ();
				VariableInitializer variable = new VariableInitializer ();
				
				variable.AddChild (new Identifier (em.Name, Convert (em.Location)), AbstractNode.Roles.Identifier);
				
				if (em.Initializer != null) {
					INode initializer = (INode)Visit (em.Initializer);
					if (initializer != null)
						variable.AddChild (initializer, AbstractNode.Roles.Initializer);
				}
				
				newField.AddChild (variable, AbstractNode.Roles.Initializer);
				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
			}
			#endregion
			
			#region Type members
			
			void AddFixedFieldInitializer (VariableInitializer variable, Expression initializer)
			{
				if (initializer == null)
					return;
				if (initializer is ConstInitializer) {
					variable.AddChild (new CSharpTokenNode (Convert (initializer.Location), 1), FieldDeclaration.Roles.LBracket);
					variable.AddChild ((INode)initializer.Accept (this), FieldDeclaration.Roles.Initializer);
					var initializerLoc = LocationsBag.GetLocations (initializer);
					if (initializerLoc != null)
						variable.AddChild (new CSharpTokenNode (Convert (initializerLoc[0]), 1), FieldDeclaration.Roles.RBracket);
				} else {
					variable.AddChild (new CSharpTokenNode (Convert (initializer.Location), 1), FieldDeclaration.Roles.Assign);
					variable.AddChild ((INode)initializer.Accept (this), FieldDeclaration.Roles.Initializer);
				}
			}
			
			public override void Visit (FixedField f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				
				AddModifiers (newField, location);
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[0]), "fixed".Length), FieldDeclaration.Roles.Keyword);
				newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
				AddFixedFieldInitializer (variable, f.Initializer);
				
				newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
						AddFixedFieldInitializer (variable, decl.Initializer);
						newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FieldDeclaration.Roles.Semicolon);
				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
				
			}

			public override void Visit (Field f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				
				AddModifiers (newField, location);
				newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
				
				if (f.Initializer != null) {
					if (location != null)
						variable.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FieldDeclaration.Roles.Assign);
					variable.AddChild ((INode)f.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
				}
				newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
						if (decl.Initializer != null) {
							variable.AddChild (new CSharpTokenNode (Convert (decl.Initializer.Location), 1), FieldDeclaration.Roles.Assign);
							variable.AddChild ((INode)decl.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
						}
						newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), FieldDeclaration.Roles.Semicolon);

				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Const f)
			{
				var location = LocationsBag.GetMemberLocation (f);
				
				FieldDeclaration newField = new FieldDeclaration ();
				
				AddModifiers (newField, location);
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[0]), "const".Length), FieldDeclaration.Roles.Keyword);
				newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
				
				if (f.Initializer != null) {
					variable.AddChild (new CSharpTokenNode (Convert (f.Initializer.Location), 1), FieldDeclaration.Roles.Assign);
					variable.AddChild ((INode)f.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
				}
				newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
				if (f.Declarators != null) {
					foreach (var decl in f.Declarators) {
						var declLoc = LocationsBag.GetLocations (decl);
						if (declLoc != null)
							newField.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Comma);
						
						variable = new VariableInitializer ();
						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
						if (decl.Initializer != null) {
							variable.AddChild (new CSharpTokenNode (Convert (decl.Initializer.Location), 1), FieldDeclaration.Roles.Assign);
							variable.AddChild ((INode)decl.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
						}
						newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
					}
				}
				if (location != null)
					newField.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FieldDeclaration.Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);

				
			}
			
			public override void Visit (Operator o)
			{
				OperatorDeclaration newOperator = new OperatorDeclaration ();
				newOperator.OperatorType = (OperatorType)o.OperatorType;
				
				var location = LocationsBag.GetMemberLocation (o);
				
				AddModifiers (newOperator, location);
				
				newOperator.AddChild ((INode)o.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				
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
					newOperator.AddChild ((INode)o.Block.Accept (this), OperatorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newOperator, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Indexer indexer)
			{
				IndexerDeclaration newIndexer = new IndexerDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (indexer);
				AddModifiers (newIndexer, location);
				
				newIndexer.AddChild ((INode)indexer.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IndexerDeclaration.Roles.LBracket);
				AddParameter (newIndexer, indexer.Parameters);
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IndexerDeclaration.Roles.RBracket);
				
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[2]), 1), IndexerDeclaration.Roles.LBrace);
				if (indexer.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					var getLocation = LocationsBag.GetMemberLocation (indexer.Get);
					AddModifiers (getAccessor, getLocation);
					if (getLocation != null)
						getAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Get.Location), "get".Length), PropertyDeclaration.Roles.Keyword);
					if (indexer.Get.Block != null) {
						getAccessor.AddChild ((INode)indexer.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (getLocation != null && getLocation.Count > 0)
							newIndexer.AddChild (new CSharpTokenNode (Convert (getLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newIndexer.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
				}
				
				if (indexer.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					var setLocation = LocationsBag.GetMemberLocation (indexer.Set);
					AddModifiers (setAccessor, setLocation);
					if (setLocation != null)
						setAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Set.Location), "set".Length), PropertyDeclaration.Roles.Keyword);
					
					if (indexer.Set.Block != null) {
						setAccessor.AddChild ((INode)indexer.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (setLocation != null && setLocation.Count > 0)
							newIndexer.AddChild (new CSharpTokenNode (Convert (setLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newIndexer.AddChild (setAccessor, PropertyDeclaration.PropertySetRole);
				}
				
				if (location != null)
					newIndexer.AddChild (new CSharpTokenNode (Convert (location[3]), 1), IndexerDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newIndexer, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Method m)
			{
				MethodDeclaration newMethod = new MethodDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (m);
				AddModifiers (newMethod, location);
				
				newMethod.AddChild ((INode)m.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				newMethod.AddChild (new Identifier (m.Name, Convert (m.Location)), AbstractNode.Roles.Identifier);
				
				if (m.MemberName.TypeArguments != null)  {
					var typeArgLocation = LocationsBag.GetLocations (m.MemberName);
					if (typeArgLocation != null)
						newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
//					AddTypeArguments (newMethod, typeArgLocation, m.MemberName.TypeArguments);
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
					INode bodyBlock = (INode)m.Block.Accept (this);
//					if (m.Block is ToplevelBlock) {
//						newMethod.AddChild (bodyBlock.FirstChild.NextSibling, MethodDeclaration.Roles.Body);
//					} else {
						newMethod.AddChild (bodyBlock, MethodDeclaration.Roles.Body);
//					}
				}
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
			
			void AddModifiers (AbstractNode parent, LocationsBag.MemberLocations location)
			{
				if (location == null || location.Modifiers == null)
					return;
				foreach (var modifier in location.Modifiers) {
					parent.AddChild (new CSharpModifierToken (Convert (modifier.Item2), modifierTable[modifier.Item1]), AbstractNode.Roles.Modifier);
				}
			}
			
			public override void Visit (Property p)
			{
				PropertyDeclaration newProperty = new PropertyDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (p);
				AddModifiers (newProperty, location);
				
				newProperty.AddChild ((INode)p.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				newProperty.AddChild (new Identifier (p.MemberName.Name, Convert (p.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (location != null)
					newProperty.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LBrace);
				
				if (p.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					var getLocation = LocationsBag.GetMemberLocation (p.Get);
					AddModifiers (getAccessor, getLocation);
					getAccessor.AddChild (new CSharpTokenNode (Convert (p.Get.Location), "get".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Get.Block != null) {
						getAccessor.AddChild ((INode)p.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (getLocation != null && getLocation.Count > 0)
							newProperty.AddChild (new CSharpTokenNode (Convert (getLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newProperty.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
				}
				
				if (p.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					var setLocation = LocationsBag.GetMemberLocation (p.Set);
					AddModifiers (setAccessor, setLocation);
					setAccessor.AddChild (new CSharpTokenNode (Convert (p.Set.Location), "set".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Set.Block != null) {
						setAccessor.AddChild ((INode)p.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
					} else {
						if (setLocation != null && setLocation.Count > 0)
							newProperty.AddChild (new CSharpTokenNode (Convert (setLocation[0]), 1), MethodDeclaration.Roles.Semicolon);
					}
					newProperty.AddChild (setAccessor, PropertyDeclaration.PropertySetRole);
				}
				if (location != null)
					newProperty.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newProperty, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Constructor c)
			{
				ConstructorDeclaration newConstructor = new ConstructorDeclaration ();
				var location = LocationsBag.GetMemberLocation (c);
				AddModifiers (newConstructor, location);
				newConstructor.AddChild (new Identifier (c.MemberName.Name, Convert (c.MemberName.Location)), AbstractNode.Roles.Identifier);
				if (location != null) 
					newConstructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				
				AddParameter (newConstructor, c.ParameterInfo);
				if (location != null) 
					newConstructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
				
				if (c.Block != null)
					newConstructor.AddChild ((INode)c.Block.Accept (this), ConstructorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newConstructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Destructor d)
			{
				DestructorDeclaration newDestructor = new DestructorDeclaration ();
				var location = LocationsBag.GetMemberLocation (d);
				AddModifiers (newDestructor, location);
				if (location != null)
					newDestructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), DestructorDeclaration.TildeRole);
				newDestructor.AddChild (new Identifier (d.MemberName.Name, Convert (d.MemberName.Location)), AbstractNode.Roles.Identifier);
				
				if (location != null) {
					newDestructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), DestructorDeclaration.Roles.LPar);
					newDestructor.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DestructorDeclaration.Roles.RPar);
				}
				
				if (d.Block != null)
					newDestructor.AddChild ((INode)d.Block.Accept (this), DestructorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newDestructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (EventField e)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (e);
				AddModifiers (newEvent, location);
				
				if (location != null)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
				newEvent.AddChild ((INode)e.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				newEvent.AddChild (new Identifier (e.MemberName.Name, Convert (e.MemberName.Location)), EventDeclaration.Roles.Identifier);
				if (location != null)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), EventDeclaration.Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (EventProperty ep)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				
				var location = LocationsBag.GetMemberLocation (ep);
				AddModifiers (newEvent, location);
				
				if (location != null)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
				newEvent.AddChild ((INode)ep.TypeName.Accept (this), EventDeclaration.Roles.ReturnType);
				newEvent.AddChild (new Identifier (ep.MemberName.Name, Convert (ep.MemberName.Location)), EventDeclaration.Roles.Identifier);
				if (location != null && location.Count >= 2)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), 1), EventDeclaration.Roles.LBrace);
				
				if (ep.Add != null) {
					MonoDevelop.CSharp.Dom.Accessor addAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					var addLocation = LocationsBag.GetMemberLocation (ep.Add);
					AddModifiers (addAccessor, addLocation);
					addAccessor.AddChild (new CSharpTokenNode (Convert (ep.Add.Location), "add".Length), EventDeclaration.Roles.Keyword);
					if (ep.Add.Block != null)
						addAccessor.AddChild ((INode)ep.Add.Block.Accept (this), EventDeclaration.Roles.Body);
					newEvent.AddChild (addAccessor, EventDeclaration.EventAddRole);
				}
				
				if (ep.Remove != null) {
					MonoDevelop.CSharp.Dom.Accessor removeAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					var removeLocation = LocationsBag.GetMemberLocation (ep.Remove);
					AddModifiers (removeAccessor, removeLocation);
					removeAccessor.AddChild (new CSharpTokenNode (Convert (ep.Remove.Location), "remove".Length), EventDeclaration.Roles.Keyword);
					
					if (ep.Remove.Block != null)
						removeAccessor.AddChild ((INode)ep.Remove.Block.Accept (this), EventDeclaration.Roles.Body);
					newEvent.AddChild (removeAccessor, EventDeclaration.EventRemoveRole);
				}
				if (location != null && location.Count >= 3)
					newEvent.AddChild (new CSharpTokenNode (Convert (location[2]), 1), EventDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.Roles.Member);
			}
			
			#endregion
			
			#region Statements
			public override object Visit (Statement stmt)
			{
				Console.WriteLine ("unknown statement:" + stmt);
				return null;
			}
			
			public override object Visit (BlockVariableDeclaration blockVariableDeclaration)
			{
				var result = new VariableDeclarationStatement ();
				result.AddChild ((INode)blockVariableDeclaration.TypeExpression.Accept (this), VariableDeclarationStatement.Roles.ReturnType);
				
				var varInit = new VariableInitializer ();
				var location = LocationsBag.GetLocations (blockVariableDeclaration);
				varInit.AddChild (new Identifier (blockVariableDeclaration.Variable.Name, Convert (blockVariableDeclaration.Variable.Location)), VariableInitializer.Roles.Identifier);
				if (blockVariableDeclaration.Initializer != null) {
					if (location != null)
						varInit.AddChild (new CSharpTokenNode (Convert (location[0]), 1), VariableInitializer.Roles.Assign);
					varInit.AddChild ((INode)blockVariableDeclaration.Initializer.Accept (this), VariableInitializer.Roles.Initializer);
				}
				
				result.AddChild (varInit, VariableDeclarationStatement.Roles.Initializer);
				
				if (blockVariableDeclaration.Declarators != null) {
					foreach (var decl in blockVariableDeclaration.Declarators) {
						var loc = LocationsBag.GetLocations (decl);
						var init = new VariableInitializer ();
						init.AddChild (new Identifier (decl.Variable.Name, Convert (decl.Variable.Location)), VariableInitializer.Roles.Identifier);
						if (decl.Initializer != null) {
							if (loc != null)
								init.AddChild (new CSharpTokenNode (Convert (loc[0]), 1), VariableInitializer.Roles.Assign);
							init.AddChild ((INode)decl.Initializer.Accept (this), VariableInitializer.Roles.Initializer);
							if (loc != null && loc.Count > 1)
								result.AddChild (new CSharpTokenNode (Convert (loc[1]), 1), VariableInitializer.Roles.Comma);
						} else {
							if (loc != null && loc.Count > 0)
								result.AddChild (new CSharpTokenNode (Convert (loc[0]), 1), VariableInitializer.Roles.Comma);
						}
						result.AddChild (init, VariableDeclarationStatement.Roles.Initializer);
					}
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), VariableDeclarationStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Mono.CSharp.EmptyStatement emptyStatement)
			{
				var result = new MonoDevelop.CSharp.Dom.EmptyStatement ();
				result.Location = Convert (emptyStatement.loc);
				return result;
			}
			
			public override object Visit (EmptyExpressionStatement emptyExpressionStatement)
			{
				// indicates an error
				return new MonoDevelop.CSharp.Dom.EmptyStatement ();
			}
		
			
			public override object Visit (If ifStatement)
			{
				var result = new IfElseStatement ();
				
				var location = LocationsBag.GetLocations (ifStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (ifStatement.loc), "if".Length), IfElseStatement.IfKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IfElseStatement.Roles.LPar);
				result.AddChild ((INode)ifStatement.Expr.Accept (this), IfElseStatement.Roles.Condition);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IfElseStatement.Roles.RPar);
				
				result.AddChild ((INode)ifStatement.TrueStatement.Accept (this), IfElseStatement.TrueEmbeddedStatementRole);
				
				if (ifStatement.FalseStatement != null) {
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[2]), "else".Length), IfElseStatement.ElseKeywordRole);
					result.AddChild ((INode)ifStatement.FalseStatement.Accept (this), IfElseStatement.FalseEmbeddedStatementRole);
				}
				
				return result;
			}
			
			public override object Visit (Do doStatement)
			{
				var result = new WhileStatement (WhilePosition.End);
				var location = LocationsBag.GetLocations (doStatement);
				result.AddChild (new CSharpTokenNode (Convert (doStatement.loc), "do".Length), WhileStatement.DoKeywordRole);
				result.AddChild ((INode)doStatement.EmbeddedStatement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "while".Length), WhileStatement.WhileKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), WhileStatement.Roles.LPar);
				result.AddChild ((INode)doStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), WhileStatement.Roles.RPar);
					result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), WhileStatement.Roles.Semicolon);
				}
				
				return result;
			}
			
			public override object Visit (While whileStatement)
			{
				var result = new WhileStatement (WhilePosition.Begin);
				var location = LocationsBag.GetLocations (whileStatement);
				result.AddChild (new CSharpTokenNode (Convert (whileStatement.loc), "while".Length), WhileStatement.WhileKeywordRole);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), WhileStatement.Roles.LPar);
				result.AddChild ((INode)whileStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), WhileStatement.Roles.RPar);
				result.AddChild ((INode)whileStatement.Statement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (For forStatement)
			{
				var result = new ForStatement ();
				
				var location = LocationsBag.GetLocations (forStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (forStatement.loc), "for".Length), ForStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ForStatement.Roles.LPar);
				if (forStatement.InitStatement != null)
					result.AddChild ((INode)forStatement.InitStatement.Accept (this), ForStatement.Roles.Initializer);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Test != null)
					result.AddChild ((INode)forStatement.Test.Accept (this), ForStatement.Roles.Condition);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Increment != null)
					result.AddChild ((INode)forStatement.Increment.Accept (this), ForStatement.Roles.Iterator);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), ForStatement.Roles.RPar);
				
				result.AddChild ((INode)forStatement.Statement.Accept (this), ForStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (StatementExpression statementExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.ExpressionStatement ();
				result.AddChild ((INode)statementExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ExpressionStatement.Roles.Expression);
				var location = LocationsBag.GetLocations (statementExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MonoDevelop.CSharp.Dom.ExpressionStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Return returnStatement)
			{
				var result = new ReturnStatement ();
				
				result.AddChild (new CSharpTokenNode (Convert (returnStatement.loc), "return".Length), ReturnStatement.Roles.Keyword);
				if (returnStatement.Expr != null)
					result.AddChild ((INode)returnStatement.Expr.Accept (this), ReturnStatement.Roles.Expression);
				
				var location = LocationsBag.GetLocations (returnStatement);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ReturnStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (Goto gotoStatement)
			{
				var result = new GotoStatement (GotoType.Label);
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
				var result = new GotoStatement (GotoType.CaseDefault);
				result.AddChild (new CSharpTokenNode (Convert (gotoDefault.loc), "goto".Length), GotoStatement.Roles.Keyword);
				var location = LocationsBag.GetLocations (gotoDefault);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "default".Length), GotoStatement.DefaultKeywordRole);
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), GotoStatement.Roles.Semicolon);
				}
				
				return result;
			}
			
			public override object Visit (GotoCase gotoCase)
			{
				var result = new GotoStatement (GotoType.Case);
				result.AddChild (new CSharpTokenNode (Convert (gotoCase.loc), "goto".Length), GotoStatement.Roles.Keyword);
				
				var location = LocationsBag.GetLocations (gotoCase);
				if (location != null) 
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "case".Length), GotoStatement.CaseKeywordRole);
				result.AddChild ((INode)gotoCase.Expr.Accept (this), GotoStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), GotoStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Throw throwStatement)
			{
				var result = new ThrowStatement ();
				var location = LocationsBag.GetLocations (throwStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (throwStatement.loc), "throw".Length), ThrowStatement.Roles.Keyword);
				if (throwStatement.Expr != null)
					result.AddChild ((INode)throwStatement.Expr.Accept (this), ThrowStatement.Roles.Expression);
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
				Statement cur = blockStatement.Statements[0];
				if (cur is Using) {
					Using u = (Using)cur;
					usingResult.AddChild (new CSharpTokenNode (Convert (u.loc), "using".Length), UsingStatement.Roles.Keyword);
					usingResult.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation), 1), UsingStatement.Roles.LPar);
					if (u.Variables != null) {
						usingResult.AddChild ((INode)u.Variables.TypeExpression.Accept (this), UsingStatement.Roles.ReturnType);
						usingResult.AddChild (new Identifier (u.Variables.Variable.Name, Convert (u.Variables.Variable.Location)), UsingStatement.Roles.Identifier);
						var loc = LocationsBag.GetLocations (u.Variables);
						if (loc != null)
							usingResult.AddChild (new CSharpTokenNode (Convert (loc[1]), 1), ContinueStatement.Roles.Assign);
						if (u.Variables.Initializer != null)
							usingResult.AddChild ((INode)u.Variables.Initializer.Accept (this), UsingStatement.Roles.Initializer);
						
					}
					cur = u.Statement;
					usingResult.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation), 1), UsingStatement.Roles.RPar);
					usingResult.AddChild ((INode)cur.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				}
				return usingResult;
			}
			
			void AddBlockChildren (BlockStatement result, Block blockStatement, ref int curLocal)
			{
				foreach (Statement stmt in blockStatement.Statements) {
					if (stmt == null)
						continue;
/*					if (curLocal < localVariables.Count && IsLower (localVariables[curLocal].Location, stmt.loc)) {
						result.AddChild (CreateVariableDeclaration (localVariables[curLocal]), AbstractCSharpNode.Roles.Statement);
						curLocal++;
					}*/
					if (stmt is Block && !(stmt is ToplevelBlock || stmt is ExplicitBlock)) {
						AddBlockChildren (result, (Block)stmt, ref curLocal);
					} else {
						result.AddChild ((INode)stmt.Accept (this), AbstractCSharpNode.Roles.Statement);
					}
				}
			}
			
			public override object Visit (Block blockStatement)
			{
				if (blockStatement.IsCompilerGenerated) {
					if (blockStatement.Statements.First () is Using)
						return CreateUsingStatement (blockStatement);
					return blockStatement.Statements.Last ().Accept (this);
				}
				var result = new BlockStatement ();
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation), 1), AbstractCSharpNode.Roles.LBrace);
				int curLocal = 0;
				AddBlockChildren (result, blockStatement, ref curLocal);
				
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation), 1), AbstractCSharpNode.Roles.RBrace);
				return result;
			}

			public override object Visit (Switch switchStatement)
			{
				var result = new SwitchStatement ();
				
				var location = LocationsBag.GetLocations (switchStatement);
				result.AddChild (new CSharpTokenNode (Convert (switchStatement.loc), "switch".Length), SwitchStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), SwitchStatement.Roles.LPar);
				result.AddChild ((INode)switchStatement.Expr.Accept (this), SwitchStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), SwitchStatement.Roles.RPar);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), SwitchStatement.Roles.LBrace);
				foreach (var section in switchStatement.Sections) {
					var newSection = new MonoDevelop.CSharp.Dom.SwitchSection ();
					foreach (var caseLabel in section.Labels) {
						var newLabel = new CaseLabel ();
						newLabel.AddChild (new CSharpTokenNode (Convert (caseLabel.Location), "case".Length), SwitchStatement.Roles.Keyword);
						if (caseLabel.Label != null)
							newLabel.AddChild ((INode)caseLabel.Label.Accept (this), SwitchStatement.Roles.Expression);
						
						newSection.AddChild (newLabel, MonoDevelop.CSharp.Dom.SwitchSection.CaseLabelRole);
					}
					
					var blockStatement = section.Block;
					var bodyBlock = new BlockStatement ();
					int curLocal = 0;
					AddBlockChildren (bodyBlock, blockStatement, ref curLocal);
					
					newSection.AddChild (bodyBlock, MonoDevelop.CSharp.Dom.SwitchSection.Roles.Body);
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
				result.AddChild ((INode)lockStatement.Expr.Accept (this), LockStatement.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), LockStatement.Roles.RPar);
				result.AddChild ((INode)lockStatement.Statement.Accept (this), LockStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Unchecked uncheckedStatement)
			{
				var result = new UncheckedStatement ();
				result.AddChild (new CSharpTokenNode (Convert (uncheckedStatement.loc), "unchecked".Length), UncheckedStatement.Roles.Keyword);
				result.AddChild ((INode)uncheckedStatement.Block.Accept (this), UncheckedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			
			public override object Visit (Checked checkedStatement)
			{
				var result = new CheckedStatement ();
				result.AddChild (new CSharpTokenNode (Convert (checkedStatement.loc), "checked".Length), CheckedStatement.Roles.Keyword);
				result.AddChild ((INode)checkedStatement.Block.Accept (this), CheckedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Unsafe unsafeStatement)
			{
				var result = new UnsafeStatement ();
				result.AddChild (new CSharpTokenNode (Convert (unsafeStatement.loc), "unsafe".Length), UnsafeStatement.Roles.Keyword);
				result.AddChild ((INode)unsafeStatement.Block.Accept (this), UnsafeStatement.Roles.Body);
				return result;
			}
			
			public override object Visit (Fixed fixedStatement)
			{
				var result = new FixedStatement ();
				var location = LocationsBag.GetLocations (fixedStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (fixedStatement.loc), "fixed".Length), FixedStatement.FixedKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FixedStatement.Roles.LPar);
				
				if (fixedStatement.Variables != null) {
					result.AddChild ((INode)fixedStatement.Variables.TypeExpression.Accept (this), UsingStatement.Roles.ReturnType);
					result.AddChild (new Identifier (fixedStatement.Variables.Variable.Name, Convert (fixedStatement.Variables.Variable.Location)), UsingStatement.Roles.Identifier);
					var loc = LocationsBag.GetLocations (fixedStatement.Variables);
					if (loc != null)
						result.AddChild (new CSharpTokenNode (Convert (loc[1]), 1), ContinueStatement.Roles.Assign);
					if (fixedStatement.Variables.Initializer != null)
						result.AddChild ((INode)fixedStatement.Variables.Initializer.Accept (this), UsingStatement.Roles.Initializer);
				}
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FixedStatement.Roles.RPar);
				result.AddChild ((INode)fixedStatement.Statement.Accept (this), FixedStatement.Roles.EmbeddedStatement);
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
					result.AddChild ((INode)tryFinallyStatement.Stmt.Accept (this), TryCatchStatement.TryBlockRole);
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "finally".Length), TryCatchStatement.FinallyKeywordRole);
				result.AddChild ((INode)tryFinallyStatement.Fini.Accept (this), TryCatchStatement.FinallyBlockRole);
				
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
					
					result.AddChild ((INode)ctch.TypeExpression.Accept (this), CatchClause.Roles.ReturnType);
					if (ctch.Variable != null && !string.IsNullOrEmpty (ctch.Variable.Name))
						result.AddChild (new Identifier (ctch.Variable.Name, Convert (ctch.Variable.Location)), CatchClause.Roles.Identifier);
					
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), CatchClause.Roles.RPar);
				}
				
				result.AddChild ((INode)ctch.Block.Accept (this), CatchClause.Roles.Body);
				
				return result;
			}
			
			public override object Visit (TryCatch tryCatchStatement)
			{
				var result = new TryCatchStatement ();
				result.AddChild (new CSharpTokenNode (Convert (tryCatchStatement.loc), "try".Length), TryCatchStatement.TryKeywordRole);
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
				var location = LocationsBag.GetLocations (usingStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (usingStatement.loc), "using".Length), UsingStatement.Roles.Keyword);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), UsingStatement.Roles.LPar);
				
				result.AddChild ((INode)usingStatement.Expression.Accept (this), UsingStatement.Roles.Initializer);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), UsingStatement.Roles.RPar);
				
				result.AddChild ((INode)usingStatement.Statement.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Foreach foreachStatement)
			{
				var result = new ForeachStatement ();
				
				var location = LocationsBag.GetLocations (foreachStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (foreachStatement.loc), "foreach".Length), ForeachStatement.ForEachKeywordRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ForeachStatement.Roles.LPar);
				
				if (foreachStatement.TypeExpr == null)
					result.AddChild ((INode)foreachStatement.TypeExpr.Accept (this), ForeachStatement.Roles.ReturnType);
				if (foreachStatement.Variable != null)
					result.AddChild (new Identifier (foreachStatement.Variable.Name, Convert (foreachStatement.Variable.Location)), ForeachStatement.Roles.Identifier);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), ForeachStatement.InKeywordRole);
				
				if (foreachStatement.Expr != null)
					result.AddChild ((INode)foreachStatement.Expr.Accept (this), ForeachStatement.Roles.Initializer);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), ForeachStatement.Roles.RPar);
				result.AddChild ((INode)foreachStatement.Statement.Accept (this), ForeachStatement.Roles.EmbeddedStatement);
				
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
					result.AddChild ((INode)yieldStatement.Expr.Accept (this), YieldStatement.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), YieldStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (YieldBreak yieldBreakStatement)
			{
				var result = new YieldStatement ();
				var location = LocationsBag.GetLocations (yieldBreakStatement);
				result.AddChild (new CSharpTokenNode (Convert (yieldBreakStatement.loc), "yield".Length), YieldStatement.YieldKeywordRole);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "break".Length), YieldStatement.BreakKeywordRole);
					result.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), YieldStatement.Roles.Semicolon);
				}
				return result;
			}
			#endregion
			
			#region Expression
			public override object Visit (Expression expression)
			{
				Console.WriteLine ("Visit unknown expression:" + expression);
				return null;
			}
			
			public override object Visit (TypeExpression typeExpression)
			{
				var result = new FullTypeName ();
				if (typeExpression.Type != null) {
					result.AddChild (new Identifier (typeExpression.Type.Name, Convert (typeExpression.Location)), FullTypeName.Roles.Identifier);
				}
//				if (!string.IsNullOrEmpty (typeExpression.)) {
//					result.AddChild (new Identifier (typeExpression.Namespace + "." + typeExpression.Name, Convert (typeExpression.Location)));
//				} else {
//					result.AddChild (new Identifier (typeExpression.Name, Convert (typeExpression.Location)));
//				}
				return result;
			}

			public override object Visit (LocalVariableReference localVariableReference)
			{
				return new Identifier (localVariableReference.Name, Convert (localVariableReference.Location));;
			}

			public override object Visit (MemberAccess memberAccess)
			{
				var result = new MemberReferenceExpression ();
				if (memberAccess.LeftExpression != null)
					result.AddChild ((INode)memberAccess.LeftExpression.Accept (this), MemberReferenceExpression.Roles.TargetExpression);
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
				var result = new PrimitiveExpression (constant.GetValue (), Convert (constant.Location), constant.AsString ().Length);
				return result;
			}

			public override object Visit (SimpleName simpleName)
			{
				var result = new IdentifierExpression ();
				result.AddChild (new Identifier (simpleName.Name, Convert (simpleName.Location)), FullTypeName.Roles.Identifier);
				if (simpleName.TypeArguments != null)  {
					var location = LocationsBag.GetLocations (simpleName);
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FullTypeName.Roles.LChevron);
//					AddTypeArguments (result, location, simpleName.TypeArguments);
					if (location != null && location.Count > 1)
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FullTypeName.Roles.RChevron);
				}
				return result;
			}
			
			public override object Visit (BooleanExpression booleanExpression)
			{
				return booleanExpression.Expr.Accept (this);
			}

			
			public override object Visit (Mono.CSharp.ParenthesizedExpression parenthesizedExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.ParenthesizedExpression ();
				var location = LocationsBag.GetLocations (parenthesizedExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.LPar);
				result.AddChild ((INode)parenthesizedExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.RPar);
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
				switch (unaryMutatorExpression.UnaryMutatorMode) {
				case UnaryMutator.Mode.PostDecrement:
					result.UnaryOperatorType = UnaryOperatorType.PostDecrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.Operator);
					break;
				case UnaryMutator.Mode.PostIncrement:
					result.UnaryOperatorType = UnaryOperatorType.PostIncrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.Operator);
					break;
					
				case UnaryMutator.Mode.PreIncrement:
					result.UnaryOperatorType = UnaryOperatorType.Increment;
					result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				case UnaryMutator.Mode.PreDecrement:
					result.UnaryOperatorType = UnaryOperatorType.Decrement;
					result.AddChild (new CSharpTokenNode (Convert (unaryMutatorExpression.Location), 2), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				}
				
				return result;
			}
			
			public override object Visit (Indirection indirectionExpression)
			{
				var result = new UnaryOperatorExpression ();
				result.UnaryOperatorType = UnaryOperatorType.Dereference;
				var location = LocationsBag.GetLocations (indirectionExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), UnaryOperatorExpression.Operator);
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
				var location = LocationsBag.GetLocations (castExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (castExpression.Location), 1), CastExpression.Roles.LPar);
				if (castExpression.TargetType != null)
					result.AddChild ((INode)castExpression.TargetType.Accept (this), CastExpression.Roles.ReturnType);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CastExpression.Roles.RPar);
				if (castExpression.Expr != null)
					result.AddChild ((INode)castExpression.Expr.Accept (this), CastExpression.Roles.Expression);
				return result;
			}
			
			public override object Visit (ComposedCast composedCast)
			{
				var result = new ComposedType ();
				result.AddChild ((INode)composedCast.Left.Accept (this), ComposedType.Roles.ReturnType);
				
				var spec = composedCast.Spec;
				while (spec != null) {
					if (spec.IsNullable) {
						result.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.NullableRole);
					} else if (spec.IsPointer) {
						result.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.PointerRole);
					} else {
						var aSpec = new ComposedType.ArraySpecifier ();
						aSpec.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.Roles.LBracket);
						var location = LocationsBag.GetLocations (spec);
						if (location != null)
							aSpec.AddChild (new CSharpTokenNode (Convert (spec.Location), 1), ComposedType.Roles.RBracket);
						result.AddChild (aSpec, ComposedType.ArraySpecRole);
					}
					spec = spec.Next;
				}
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.DefaultValueExpression defaultValueExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.DefaultValueExpression ();
				result.AddChild (new CSharpTokenNode (Convert (defaultValueExpression.Location), "default".Length), CastExpression.Roles.Keyword);
				var location = LocationsBag.GetLocations (defaultValueExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CastExpression.Roles.LPar);
				result.AddChild ((INode)defaultValueExpression.Expr.Accept (this), CastExpression.Roles.ReturnType);
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
				result.AddChild (new CSharpTokenNode (Convert (binaryExpression.Location), opLength), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)binaryExpression.Right.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Nullable.NullCoalescingOperator nullCoalescingOperator)
			{
				var result = new BinaryOperatorExpression ();
				result.BinaryOperatorType = BinaryOperatorType.NullCoalescing;
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.LeftExpressionRole);
				var location = LocationsBag.GetLocations (nullCoalescingOperator);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)nullCoalescingOperator.Right.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Conditional conditionalExpression)
			{
				var result = new ConditionalExpression ();
				
				result.AddChild ((INode)conditionalExpression.Expr.Accept (this), ConditionalExpression.Roles.Condition);
				var location = LocationsBag.GetLocations (conditionalExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (conditionalExpression.Location), 1), ConditionalExpression.Roles.QuestionMark);
				result.AddChild ((INode)conditionalExpression.TrueExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ConditionalExpression.Roles.Colon);
				result.AddChild ((INode)conditionalExpression.FalseExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				return result;
			}
		
			void AddParameter (AbstractCSharpNode parent, Mono.CSharp.AParametersCollection parameters)
			{
				if (parameters == null)
					return;
				var paramLocation = LocationsBag.GetLocations (parameters);
			
				for (int i = 0; i < parameters.Count; i++) {
					if (paramLocation != null && i > 0 && i - 1 < paramLocation.Count) 
						parent.AddChild (new CSharpTokenNode (Convert (paramLocation[i - 1]), 1), ParameterDeclarationExpression.Roles.Comma);
					var p = (Parameter)parameters.FixedParameters[i];
					var location = LocationsBag.GetLocations (p);
					
					ParameterDeclarationExpression parameterDeclarationExpression = new ParameterDeclarationExpression ();
					switch (p.ModFlags) {
					case Parameter.Modifier.OUT:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Out;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location[0]), "out".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					case Parameter.Modifier.REF:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Ref;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location[0]), "ref".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					case Parameter.Modifier.PARAMS:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Params;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location[0]), "params".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					case Parameter.Modifier.This:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.This;
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location[0]), "this".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					}
					if (p.TypeExpression != null) // lambdas may have no types (a, b) => ...
						parameterDeclarationExpression.AddChild ((INode)p.TypeExpression.Accept (this), ParameterDeclarationExpression.Roles.ReturnType);
					parameterDeclarationExpression.AddChild (new Identifier (p.Name, Convert (p.Location)), ParameterDeclarationExpression.Roles.Identifier);
					if (p.HasDefaultValue) {
						if (location != null)
							parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ParameterDeclarationExpression.Roles.Assign);
						parameterDeclarationExpression.AddChild ((INode)p.DefaultValue.Accept (this), ParameterDeclarationExpression.Roles.Expression);
					}
					parent.AddChild (parameterDeclarationExpression, InvocationExpression.Roles.Argument);
				}
			}
			
			void AddTypeArguments (AbstractCSharpNode parent, LocationsBag.MemberLocations location, Mono.CSharp.TypeArguments typeArguments)
			{
				if (typeArguments == null)
					return;
				for (int i = 0; i < typeArguments.Count; i++) {
					if (location != null && i > 0 && i - 1 < location.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location[i - 1]), 1), InvocationExpression.Roles.Comma);
					parent.AddChild ((INode)typeArguments.Args[i].Accept (this), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddTypeArguments (AbstractCSharpNode parent, List<Location> location, Mono.CSharp.TypeArguments typeArguments)
			{
				if (typeArguments == null)
					return;
				for (int i = 0; i < typeArguments.Count; i++) {
					if (location != null && i > 0 && i - 1 < location.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location[i - 1]), 1), InvocationExpression.Roles.Comma);
					parent.AddChild ((INode)typeArguments.Args[i].Accept (this), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddConstraints (AbstractCSharpNode parent, DeclSpace d)
			{
				if (d == null || d.Constraints == null)
					return;
				for (int i = 0; i < d.Constraints.Count; i++) {
					Constraints c = d.Constraints[i];
					var location = LocationsBag.GetLocations (c);
					var constraint = new Constraint ();
					parent.AddChild (new CSharpTokenNode (Convert (location[0]), "where".Length), InvocationExpression.Roles.Keyword);
					parent.AddChild (new Identifier (c.TypeParameter.Value, Convert (c.TypeParameter.Location)), InvocationExpression.Roles.Identifier);
					parent.AddChild (new CSharpTokenNode (Convert (location[1]), 1), InvocationExpression.Roles.Colon);
					foreach (var expr in c.ConstraintExpressions)
						parent.AddChild ((INode)expr.Accept (this), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddArguments (AbstractCSharpNode parent, object location, Mono.CSharp.Arguments args)
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
						if (location != null)
							direction.AddChild (new CSharpTokenNode (Convert (argLocation[0]), "123".Length), InvocationExpression.Roles.Keyword);
						direction.AddChild ((INode)arg.Expr.Accept (this), InvocationExpression.Roles.Expression);
						
						parent.AddChild (direction, InvocationExpression.Roles.Argument);
					} else {
						parent.AddChild ((INode)arg.Expr.Accept (this), InvocationExpression.Roles.Argument);
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
				result.AddChild ((INode)invocationExpression.Expression.Accept (this), InvocationExpression.Roles.TargetExpression);
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
					result.AddChild ((INode)newExpression.TypeRequested.Accept (this), ObjectCreateExpression.Roles.ReturnType);
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
					result.AddChild ((INode)newInitializeExpression.TypeRequested.Accept (this), ObjectCreateExpression.Roles.ReturnType);
				
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
				var result = new ArrayObjectCreateExpression ();
				
				var location = LocationsBag.GetLocations (arrayCreationExpression);
				result.AddChild (new CSharpTokenNode (Convert (arrayCreationExpression.Location), "new".Length), ArrayObjectCreateExpression.Roles.Keyword);
				
				if (arrayCreationExpression.NewType != null)
					result.AddChild ((INode)arrayCreationExpression.NewType.Accept (this), ArrayObjectCreateExpression.Roles.ReturnType);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ArrayObjectCreateExpression.Roles.LBracket);
				if (arrayCreationExpression.Arguments != null) {
					var commaLocations = LocationsBag.GetLocations (arrayCreationExpression.Arguments);
					for (int i = 0 ;i < arrayCreationExpression.Arguments.Count; i++) {
						result.AddChild ((INode)arrayCreationExpression.Arguments[i].Accept (this), ObjectCreateExpression.Roles.Initializer);
						if (commaLocations != null && i > 0)
							result.AddChild (new CSharpTokenNode (Convert (commaLocations [commaLocations.Count - i]), 1), IndexerExpression.Roles.Comma);
					}
				}
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ArrayObjectCreateExpression.Roles.RBracket);
				
				if (arrayCreationExpression.Initializers != null && arrayCreationExpression.Initializers.Count != 0) {
					var initLocation = LocationsBag.GetLocations (arrayCreationExpression.Initializers);
					result.AddChild (new CSharpTokenNode (Convert (arrayCreationExpression.Initializers.Location), 1), ArrayObjectCreateExpression.Roles.LBrace);
					var commaLocations = LocationsBag.GetLocations (arrayCreationExpression.Initializers.Elements);
					for (int i = 0; i < arrayCreationExpression.Initializers.Count; i++) {
						result.AddChild ((INode)arrayCreationExpression.Initializers[i].Accept (this), ObjectCreateExpression.Roles.Initializer);
						if (commaLocations != null && i > 0) {
							result.AddChild (new CSharpTokenNode (Convert (commaLocations [commaLocations.Count - i]), 1), IndexerExpression.Roles.Comma);
						}
					}
					
					if (initLocation != null)
						result.AddChild (new CSharpTokenNode (Convert (initLocation[initLocation.Count - 1]), 1), ArrayObjectCreateExpression.Roles.RBrace);
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
				result.AddChild ((INode)typeOfExpression.TypeExpression.Accept (this), TypeOfExpression.Roles.ReturnType);
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
				result.AddChild ((INode)sizeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
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
				result.AddChild ((INode)checkedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
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
				result.AddChild ((INode)uncheckedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (ElementAccess elementAccessExpression)
			{
				IndexerExpression result = new IndexerExpression ();
				var location = LocationsBag.GetLocations (elementAccessExpression);
				 
				result.AddChild ((INode)elementAccessExpression.Expr.Accept (this), IndexerExpression.Roles.TargetExpression);
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
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "stackalloc".Length), StackAllocExpression.StackAllocKeywordRole);
				result.AddChild ((INode)stackAllocExpression.TypeExpression.Accept (this), StackAllocExpression.Roles.ReturnType);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), StackAllocExpression.Roles.LBracket);
				result.AddChild ((INode)stackAllocExpression.CountExpression.Accept (this), StackAllocExpression.Roles.Expression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), StackAllocExpression.Roles.RBracket);
				return result;
			}
			
			public override object Visit (SimpleAssign simpleAssign)
			{
				var result = new AssignmentExpression ();
				
				result.AssignmentOperatorType = AssignmentOperatorType.Assign;
				if (simpleAssign.Target != null)
					result.AddChild ((INode)simpleAssign.Target.Accept (this), AssignmentExpression.LeftExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (simpleAssign.Location), 1), AssignmentExpression.OperatorRole);
				
				if (simpleAssign.Source != null) {
					result.AddChild ((INode)simpleAssign.Source.Accept (this), AssignmentExpression.RightExpressionRole);
				}
				return result;
			}
			
			public override object Visit (CompoundAssign compoundAssign)
			{
				var result = new AssignmentExpression ();
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
				result.AddChild (new CSharpTokenNode (Convert (compoundAssign.Location), opLength), AssignmentExpression.OperatorRole);
				result.AddChild ((INode)compoundAssign.Source.Accept (this), AssignmentExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.AnonymousMethodExpression anonymousMethodExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.AnonymousMethodExpression ();
				var location = LocationsBag.GetLocations (anonymousMethodExpression);
				if (location != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), AssignmentExpression.Roles.Keyword);
					
					if (location.Count > 1) {
						result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AssignmentExpression.Roles.LPar);
						AddParameter (result, anonymousMethodExpression.Parameters);
						result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AssignmentExpression.Roles.RPar);
					}
				}
				
				result.AddChild ((INode)anonymousMethodExpression.Block.Accept (this), AssignmentExpression.Roles.Body);
				return result;
			}

			public override object Visit (Mono.CSharp.LambdaExpression lambdaExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.LambdaExpression ();
				var location = LocationsBag.GetLocations (lambdaExpression);
				
				if (location == null || location.Count == 1) {
					AddParameter (result, lambdaExpression.Parameters);
					if (location != null)
						result.AddChild (new CSharpTokenNode (Convert (location[0]), "=>".Length), AssignmentExpression.Roles.Assign);
				} else {
					result.AddChild (new CSharpTokenNode (Convert (lambdaExpression.Location), 1), AssignmentExpression.Roles.LPar);
					AddParameter (result, lambdaExpression.Parameters);
					if (location != null) {
						result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), AssignmentExpression.Roles.RPar);
						result.AddChild (new CSharpTokenNode (Convert (location[1]), "=>".Length), AssignmentExpression.Roles.Assign);
					}
				}
				if (lambdaExpression.Block.IsCompilerGenerated) {
					ContextualReturn generatedReturn = (ContextualReturn)lambdaExpression.Block.Statements[0];
					result.AddChild ((INode)generatedReturn.Expr.Accept (this), AssignmentExpression.Roles.Expression);
				} else {
					result.AddChild ((INode)lambdaExpression.Block.Accept (this), AssignmentExpression.Roles.Expression);
				}
				
				return result;
			}
			
			public override object Visit (ConstInitializer constInitializer)
			{
				return new Identifier (constInitializer.Name, Convert (constInitializer.Location));
			}
			
			#endregion
			
			#region LINQ expressions
			public override object Visit (Mono.CSharp.Linq.QueryExpression queryExpression)
			{
				var result = new QueryExpressionFromClause ();
				var location = LocationsBag.GetLocations (queryExpression);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "from".Length), QueryExpressionFromClause.FromKeywordRole);
				// TODO: select identifier
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionFromClause.InKeywordRole);
				var query = queryExpression.Expr as Mono.CSharp.Linq.AQueryClause;
				if (query != null && query.Expr != null)
					result.AddChild ((INode)query.Expr.Accept (this), QueryExpressionFromClause.Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.SelectMany selectMany)
			{
				var result = new QueryExpressionFromClause ();
// TODO:
//				Mono.CSharp.Linq.Cast cast = selectMany.Expr as Mono.CSharp.Linq.Cast;
				var location = LocationsBag.GetLocations (selectMany);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "from".Length), QueryExpressionFromClause.FromKeywordRole);
				
				//					result.AddChild ((INode)cast.TypeExpr.Accept (this), QueryExpressionFromClause.Roles.ReturnType);
//				if (cast != null)
				
//				result.AddChild (new Identifier (selectMany.SelectIdentifier.Value, Convert (selectMany.SelectIdentifier.Location)), QueryExpressionFromClause.Roles.Identifier);
				//				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionFromClause.InKeywordRole);
				
				//				result.AddChild ((INode)(cast != null ? cast.Expr : selectMany.Expr).Accept (this), QueryExpressionFromClause.Roles.Expression);
				
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Select sel)
			{
				var result = new QueryExpressionSelectClause ();
				var location = LocationsBag.GetLocations (sel);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "select".Length), QueryExpressionWhereClause.Roles.Keyword);
				result.AddChild ((INode)sel.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupBy groupBy)
			{
				var result = new QueryExpressionGroupClause ();
				var location = LocationsBag.GetLocations (groupBy);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "group".Length), QueryExpressionGroupClause.GroupKeywordRole);
				result.AddChild ((INode)groupBy.ElementSelector.Accept (this), QueryExpressionGroupClause.GroupByExpressionRole);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "by".Length), QueryExpressionGroupClause.ByKeywordRole);
				result.AddChild ((INode)groupBy.Expr.Accept (this), QueryExpressionGroupClause.ProjectionExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Let l)
			{
				var result = new QueryExpressionLetClause ();
				var location = LocationsBag.GetLocations (l);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "let".Length), QueryExpressionWhereClause.Roles.Keyword);
				
				NewAnonymousType aType = l.Expr as NewAnonymousType;
				AnonymousTypeParameter param = ((AnonymousTypeParameter)aType.Parameters[1]);
				result.AddChild (new Identifier (param.Name, Convert (param.Location)));
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), QueryExpressionWhereClause.Roles.Assign);
				
				result.AddChild ((INode)param.Expr.Accept (this), QueryExpressionWhereClause.Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Where w)
			{
				var result = new QueryExpressionWhereClause ();
				var location = LocationsBag.GetLocations (w);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "where".Length), QueryExpressionWhereClause.Roles.Keyword);
				result.AddChild ((INode)w.Expr.Accept (this), QueryExpressionWhereClause.Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Join join)
			{
				var result = new QueryExpressionJoinClause ();
				var location = LocationsBag.GetLocations (join);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "join".Length), QueryExpressionJoinClause.JoinKeywordRole);
				
				result.AddChild (new Identifier (join.JoinVariable.Name, Convert (join.JoinVariable.Location)));
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionJoinClause.InKeywordRole);
				
				result.AddChild ((INode)join.Expr.Accept (this), QueryExpressionJoinClause.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), "on".Length), QueryExpressionJoinClause.OnKeywordRole);
				// TODO: on expression
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), "equals".Length), QueryExpressionJoinClause.EqualsKeywordRole);
				// TODO: equals expression
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupJoin groupJoin)
			{
				var result = new QueryExpressionJoinClause ();
				var location = LocationsBag.GetLocations (groupJoin);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "join".Length), QueryExpressionJoinClause.JoinKeywordRole);
				
				result.AddChild (new Identifier (groupJoin.JoinVariable.Name, Convert (groupJoin.JoinVariable.Location)));
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionJoinClause.InKeywordRole);
				
				result.AddChild ((INode)groupJoin.Expr.Accept (this), QueryExpressionJoinClause.Roles.Expression);
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[2]), "on".Length), QueryExpressionJoinClause.OnKeywordRole);
				// TODO: on expression
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[3]), "equals".Length), QueryExpressionJoinClause.EqualsKeywordRole);
				// TODO: equals expression
				
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[4]), "into".Length), QueryExpressionJoinClause.IntoKeywordRole);
				
				result.AddChild (new Identifier (groupJoin.JoinVariable.Name, Convert (groupJoin.JoinVariable.Location)));
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByAscending orderByAscending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = true;
				result.AddChild ((INode)orderByAscending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (orderByAscending);
				if (location != null) 
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "ascending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByDescending orderByDescending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = false;
				result.AddChild ((INode)orderByDescending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (orderByDescending);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "descending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByAscending thenByAscending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = true;
				result.AddChild ((INode)thenByAscending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (thenByAscending);
				if (location != null) 
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "ascending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByDescending thenByDescending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = false;
				result.AddChild ((INode)thenByDescending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				var location = LocationsBag.GetLocations (thenByDescending);
				if (location != null)
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "descending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			#endregion
		}
		public MonoDevelop.CSharp.Dom.CompilationUnit Parse (TextEditorData data)
		{
			lock (CompilerCallableEntryPoint.parseLock) {
				CompilerCompilationUnit top;
				using (Stream stream = data.OpenStream ()) {
					top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v", "-unsafe"}, stream, data.Document.FileName, Console.Out);
				}
	
				if (top == null)
					return null;
				CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor (top.LocationsBag);
				top.UsingsBag.Global.Accept (conversionVisitor);
				return conversionVisitor.Unit;
			}
		}
	}
}