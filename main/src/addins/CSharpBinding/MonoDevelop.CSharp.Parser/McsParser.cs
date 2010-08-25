// 
// McsParser.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Resolver;

namespace MonoDevelop.CSharp.Parser
{
	public class McsParser : AbstractParser
	{
		public override IExpressionFinder CreateExpressionFinder (ProjectDom dom)
		{
			return new NewCSharpExpressionFinder (dom);
		}

		public override IResolver CreateResolver (ProjectDom dom, object editor, string fileName)
		{
			MonoDevelop.Ide.Gui.Document doc = (MonoDevelop.Ide.Gui.Document)editor;
			return new NRefactoryResolver (dom, doc.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, doc.Editor, fileName);
		}
		
		
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			CompilerCompilationUnit top;
			using (var stream = new MemoryStream (Encoding.UTF8.GetBytes (content))) {
				top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v", "-unsafe"}, stream, fileName, Console.Out);
			}

			if (top == null)
				return null;

			var conversionVisitor = new ConversionVisitor (top.LocationsBag);
			conversionVisitor.ParsedDocument = new ParsedDocument (fileName);
			var unit = new MonoDevelop.Projects.Dom.CompilationUnit (fileName);
			conversionVisitor.ParsedDocument.CompilationUnit = unit;
			conversionVisitor.unit = unit;
			
			top.ModuleCompiled.Accept (conversionVisitor);
			return conversionVisitor.ParsedDocument;
		}

		class ConversionVisitor : StructuralVisitor
		{
			ProjectDom Dom {
				get;
				set;
			}
			
			public ParsedDocument ParsedDocument {
				get;
				set;
			}
			
			public LocationsBag LocationsBag  {
				get;
				private set;
			}
			public MonoDevelop.Projects.Dom.CompilationUnit unit;
			public ConversionVisitor (LocationsBag locationsBag)
			{
				this.LocationsBag = locationsBag;
			}
			
			public static DocumentLocation Convert (Mono.CSharp.Location loc)
			{
				return new DocumentLocation (loc.Row, loc.Column);
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
			
			Stack<DomType> typeStack = new Stack<DomType> ();
			public override void Visit (Class c)
			{
				DomType newType = new DomType ();
				newType.SourceProjectDom = Dom;
				newType.Name = c.MemberName.Name;
				/*
				newType.Documentation = RetrieveDocumentation (typeDeclaration.StartLocation.Line);
				newType.Location = ConvertLocation (typeDeclaration.StartLocation);
				newType.ClassType = ConvertClassType (typeDeclaration.Type);
				DomRegion region = ConvertRegion (typeDeclaration.BodyStartLocation, typeDeclaration.EndLocation);
				region.End = new DomLocation (region.End.Line, region.End.Column);
				newType.BodyRegion = region;
				newType.Modifiers = ConvertModifiers (typeDeclaration.Modifier);

				AddAttributes (newType, typeDeclaration.Attributes);

				foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition template in typeDeclaration.Templates) {
					TypeParameter parameter = ConvertTemplateDefinition (template);
					newType.AddTypeParameter (parameter);
				}

				if (typeDeclaration.BaseTypes != null) {

					foreach (ICSharpCode.NRefactory.Ast.TypeReference type in typeDeclaration.BaseTypes) {
						if (type == typeDeclaration.BaseTypes[0]) {
							newType.BaseType = ConvertReturnType (type);
						} else {
							newType.AddInterfaceImplementation (ConvertReturnType (type));
						}
					}
				}
				AddType (newType);*/

				// visit members
				typeStack.Push (newType);
				c.Accept (this);
				typeStack.Pop ();

			}
			
//			public override void Visit (Struct s)
//			{
//				TypeDeclaration newType = new TypeDeclaration ();
//				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Struct;
//				
//				var location = LocationsBag.GetMemberLocation (s);
//				AddModifiers (newType, location);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "struct".Length), TypeDeclaration.TypeKeyword);
//				newType.AddChild (new Identifier (s.Name, Convert (s.MemberName.Location)), AbstractNode.Roles.Identifier);
//				if (s.MemberName.TypeArguments != null)  {
//					var typeArgLocation = LocationsBag.GetLocations (s.MemberName);
//					if (typeArgLocation != null)
//						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
////					AddTypeArguments (newType, typeArgLocation, s.MemberName.TypeArguments);
//					if (typeArgLocation != null)
//						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
//					AddConstraints (newType, s);
//				}
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
//				typeStack.Push (newType);
//				base.Visit (s);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
//				typeStack.Pop ();
//				AddType (newType);
//			}
//			
//			public override void Visit (Interface i)
//			{
//				TypeDeclaration newType = new TypeDeclaration ();
//				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Interface;
//				
//				var location = LocationsBag.GetMemberLocation (i);
//				AddModifiers (newType, location);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "interface".Length), TypeDeclaration.TypeKeyword);
//				newType.AddChild (new Identifier (i.Name, Convert (i.MemberName.Location)), AbstractNode.Roles.Identifier);
//				if (i.MemberName.TypeArguments != null)  {
//					var typeArgLocation = LocationsBag.GetLocations (i.MemberName);
//					if (typeArgLocation != null)
//						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
////					AddTypeArguments (newType, typeArgLocation, i.MemberName.TypeArguments);
//					if (typeArgLocation != null)
//						newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
//					AddConstraints (newType, i);
//				}
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
//				typeStack.Push (newType);
//				base.Visit (i);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
//				typeStack.Pop ();
//				AddType (newType);
//			}
//			
//			public override void Visit (Mono.CSharp.Delegate d)
//			{
//				DelegateDeclaration newDelegate = new DelegateDeclaration ();
//				var location = LocationsBag.GetMemberLocation (d);
//				
//				AddModifiers (newDelegate, location);
//				if (location != null)
//					newDelegate.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), TypeDeclaration.TypeKeyword);
//				newDelegate.AddChild ((INode)d.ReturnType.Accept (this), AbstractNode.Roles.ReturnType);
//				newDelegate.AddChild (new Identifier (d.Name, Convert (d.MemberName.Location)), AbstractNode.Roles.Identifier);
//				if (d.MemberName.TypeArguments != null)  {
//					var typeArgLocation = LocationsBag.GetLocations (d.MemberName);
//					if (typeArgLocation != null)
//						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
////					AddTypeArguments (newDelegate, typeArgLocation, d.MemberName.TypeArguments);
//					if (typeArgLocation != null)
//						newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
//					AddConstraints (newDelegate, d);
//				}
//				if (location != null) {
//					newDelegate.AddChild (new CSharpTokenNode (Convert (location[1]), 1), DelegateDeclaration.Roles.LPar);
//					newDelegate.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DelegateDeclaration.Roles.RPar);
//					newDelegate.AddChild (new CSharpTokenNode (Convert (location[3]), 1), DelegateDeclaration.Roles.Semicolon);
//				}
//				AddType (newDelegate);
//			}
//			
//			void AddType (INode child)
//			{
//				if (typeStack.Count > 0) {
//					typeStack.Peek ().AddChild (child);
//				} else {
//					unit.AddChild (child);
//				}
//			}
//			
//			public override void Visit (Mono.CSharp.Enum e)
//			{
//				TypeDeclaration newType = new TypeDeclaration ();
//				newType.ClassType = MonoDevelop.Projects.Dom.ClassType.Enum;
//				var location = LocationsBag.GetMemberLocation (e);
//				
//				AddModifiers (newType, location);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode (Convert (location[0]), "enum".Length), TypeDeclaration.TypeKeyword);
//				newType.AddChild (new Identifier (e.Name, Convert (e.MemberName.Location)), AbstractNode.Roles.Identifier);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AbstractCSharpNode.Roles.LBrace);
//				typeStack.Push (newType);
//				base.Visit (e);
//				if (location != null)
//					newType.AddChild (new CSharpTokenNode  (Convert (location[2]), 1), AbstractCSharpNode.Roles.RBrace);
//				typeStack.Pop ();
//				AddType (newType);
//			}
//			
//			public override void Visit (EnumMember em)
//			{
//				FieldDeclaration newField = new FieldDeclaration ();
//				VariableInitializer variable = new VariableInitializer ();
//				
//				variable.AddChild (new Identifier (em.Name, Convert (em.Location)), AbstractNode.Roles.Identifier);
//				
//				if (em.Initializer != null) {
//					INode initializer = (INode)Visit (em.Initializer);
//					if (initializer != null)
//						variable.AddChild (initializer, AbstractNode.Roles.Initializer);
//				}
//				
//				newField.AddChild (variable, AbstractNode.Roles.Initializer);
//				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
//			}
//			#endregion
//			
////			#region Type members
//			public override void Visit (FixedField f)
//			{
//				var location = LocationsBag.GetMemberLocation (f);
//				
//				FieldDeclaration newField = new FieldDeclaration ();
//				
//				AddModifiers (newField, location);
//				if (location != null)
//					newField.AddChild (new CSharpTokenNode (Convert (location[1]), "fixed".Length), FieldDeclaration.Roles.Keyword);
//				newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
//				
//				VariableInitializer variable = new VariableInitializer ();
//				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
//				
//				if (f.Initializer != null) {
//					if (location != null)
//						variable.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FieldDeclaration.Roles.Assign);
//					variable.AddChild ((INode)f.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
//				}
//				newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
//				if (f.Declarators != null) {
//					foreach (var decl in f.Declarators) {
//						var declLoc = LocationsBag.GetLocations (decl);
//						if (declLoc != null)
//							newField.AddChild (new CSharpTokenNode (Convert (declLoc[declLoc.Length - 1]), 1), FieldDeclaration.Roles.Comma);
//						
//						variable = new VariableInitializer ();
//						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
//						if (decl.Initializer != null) {
//							if (declLoc != null)
//								variable.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Assign);
//							variable.AddChild ((INode)decl.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
//						}
//						newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
//					}
//				}
//				if (location != null)
//					newField.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), FieldDeclaration.Roles.Semicolon);
//
//				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
//
//				
//			}
//			
//			Dictionary<DomLocation, FieldDeclaration> visitedFields = new Dictionary<DomLocation, FieldDeclaration> ();
//			public override void Visit (Field f)
//			{
//				var location = LocationsBag.GetMemberLocation (f);
//				
//				FieldDeclaration newField = new FieldDeclaration ();
//				
//				AddModifiers (newField, location);
//				newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
//				
//				VariableInitializer variable = new VariableInitializer ();
//				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
//				
//				if (f.Initializer != null) {
//					if (location != null)
//						variable.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FieldDeclaration.Roles.Assign);
//					variable.AddChild ((INode)f.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
//				}
//				newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
//				if (f.Declarators != null) {
//					foreach (var decl in f.Declarators) {
//						var declLoc = LocationsBag.GetLocations (decl);
//						if (declLoc != null)
//							newField.AddChild (new CSharpTokenNode (Convert (declLoc[declLoc.Length - 1]), 1), FieldDeclaration.Roles.Comma);
//						
//						variable = new VariableInitializer ();
//						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
//						if (decl.Initializer != null) {
//							if (declLoc != null)
//								variable.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Assign);
//							variable.AddChild ((INode)decl.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
//						}
//						newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
//					}
//				}
//				if (location != null)
//					newField.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), FieldDeclaration.Roles.Semicolon);
//
//				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (Const f)
//			{
//				var location = LocationsBag.GetMemberLocation (f);
//				
//				FieldDeclaration newField = new FieldDeclaration ();
//				
//				AddModifiers (newField, location);
//				if (location != null)
//					newField.AddChild (new CSharpTokenNode (Convert (location[1]), "fixed".Length), FieldDeclaration.Roles.Keyword);
//				newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
//				
//				VariableInitializer variable = new VariableInitializer ();
//				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), FieldDeclaration.Roles.Identifier);
//				
//				if (f.Initializer != null) {
//					if (location != null)
//						variable.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FieldDeclaration.Roles.Assign);
//					variable.AddChild ((INode)f.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
//				}
//				newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
//				if (f.Declarators != null) {
//					foreach (var decl in f.Declarators) {
//						var declLoc = LocationsBag.GetLocations (decl);
//						if (declLoc != null)
//							newField.AddChild (new CSharpTokenNode (Convert (declLoc[declLoc.Length - 1]), 1), FieldDeclaration.Roles.Comma);
//						
//						variable = new VariableInitializer ();
//						variable.AddChild (new Identifier (decl.Name.Value, Convert (decl.Name.Location)), FieldDeclaration.Roles.Identifier);
//						if (decl.Initializer != null) {
//							if (declLoc != null)
//								variable.AddChild (new CSharpTokenNode (Convert (declLoc[0]), 1), FieldDeclaration.Roles.Assign);
//							variable.AddChild ((INode)decl.Initializer.Accept (this), FieldDeclaration.Roles.Initializer);
//						}
//						newField.AddChild (variable, FieldDeclaration.Roles.Initializer);
//					}
//				}
//				if (location != null)
//					newField.AddChild (new CSharpTokenNode (Convert (location[location.Count - 1]), 1), FieldDeclaration.Roles.Semicolon);
//				
//				typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
//
//				
//			}
//			
//			public override void Visit (Operator o)
//			{
//				OperatorDeclaration newOperator = new OperatorDeclaration ();
//				newOperator.OperatorType = (OperatorType)o.OperatorType;
//				
//				var location = LocationsBag.GetMemberLocation (o);
//				
//				AddModifiers (newOperator, location);
//				
//				newOperator.AddChild ((INode)o.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
//				
//				if (o.OperatorType == Operator.OpType.Implicit) {
//					if (location != null) {
//						newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), "implicit".Length), OperatorDeclaration.OperatorTypeRole);
//						newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
//					}
//				} else if (o.OperatorType == Operator.OpType.Explicit) {
//					if (location != null) {
//						newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), "explicit".Length), OperatorDeclaration.OperatorTypeRole);
//						newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
//					}
//				} else {
//					if (location != null)
//						newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
//					
//					int opLength = 1;
//					switch (newOperator.OperatorType) {
//					case OperatorType.LeftShift:
//					case OperatorType.RightShift:
//					case OperatorType.LessThanOrEqual:
//					case OperatorType.GreaterThanOrEqual:
//					case OperatorType.Equality:
//					case OperatorType.Inequality:
////					case OperatorType.LogicalAnd:
////					case OperatorType.LogicalOr:
//						opLength = 2;
//						break;
//					case OperatorType.True:
//						opLength = "true".Length;
//						break;
//					case OperatorType.False:
//						opLength = "false".Length;
//						break;
//					}
//					if (location != null)
//						newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), opLength), OperatorDeclaration.OperatorTypeRole);
//				}
//				if (location != null)
//					newOperator.AddChild (new CSharpTokenNode (Convert (location[2]), 1), OperatorDeclaration.Roles.LPar);
//				AddParameter (newOperator, o.ParameterInfo);
//				if (location != null)
//					newOperator.AddChild (new CSharpTokenNode (Convert (location[3]), 1), OperatorDeclaration.Roles.RPar);
//				
//				if (o.Block != null)
//					newOperator.AddChild ((INode)o.Block.Accept (this), OperatorDeclaration.Roles.Body);
//				
//				typeStack.Peek ().AddChild (newOperator, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (Indexer indexer)
//			{
//				IndexerDeclaration newIndexer = new IndexerDeclaration ();
//				
//				var location = LocationsBag.GetMemberLocation (indexer);
//				AddModifiers (newIndexer, location);
//				
//				newIndexer.AddChild ((INode)indexer.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
//				
//				if (location != null)
//					newIndexer.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IndexerDeclaration.Roles.LBracket);
//				AddParameter (newIndexer, indexer.Parameters);
//				if (location != null)
//					newIndexer.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IndexerDeclaration.Roles.RBracket);
//				
//				if (location != null)
//					newIndexer.AddChild (new CSharpTokenNode (Convert (location[2]), 1), IndexerDeclaration.Roles.LBrace);
//				if (indexer.Get != null) {
//					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
//					var getLocation = LocationsBag.GetMemberLocation (indexer.Get);
//					AddModifiers (getAccessor, getLocation);
//					if (getLocation != null)
//						getAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Get.Location), "get".Length), PropertyDeclaration.Roles.Keyword);
//					if (indexer.Get.Block != null)
//						getAccessor.AddChild ((INode)indexer.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
//					newIndexer.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
//				}
//				
//				if (indexer.Set != null) {
//					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
//					var setLocation = LocationsBag.GetMemberLocation (indexer.Set);
//					AddModifiers (setAccessor, setLocation);
//					if (setLocation != null)
//						setAccessor.AddChild (new CSharpTokenNode (Convert (indexer.Set.Location), "set".Length), PropertyDeclaration.Roles.Keyword);
//					
//					if (indexer.Set.Block != null)
//						setAccessor.AddChild ((INode)indexer.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
//					newIndexer.AddChild (setAccessor, PropertyDeclaration.PropertySetRole);
//				}
//				
//				if (location != null)
//					newIndexer.AddChild (new CSharpTokenNode (Convert (location[3]), 1), IndexerDeclaration.Roles.RBrace);
//				
//				typeStack.Peek ().AddChild (newIndexer, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (Method m)
//			{
//				MethodDeclaration newMethod = new MethodDeclaration ();
//				
//				var location = LocationsBag.GetMemberLocation (m);
//				AddModifiers (newMethod, location);
//				
//				newMethod.AddChild ((INode)m.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
//				newMethod.AddChild (new Identifier (m.Name, Convert (m.Location)), AbstractNode.Roles.Identifier);
//				
//				if (m.MemberName.TypeArguments != null)  {
//					var typeArgLocation = LocationsBag.GetLocations (m.MemberName);
//					if (typeArgLocation != null)
//						newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
////					AddTypeArguments (newMethod, typeArgLocation, m.MemberName.TypeArguments);
//					if (typeArgLocation != null)
//						newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
//					
//					AddConstraints (newMethod, m.GenericMethod);
//				}
//				
//				if (location != null)
//					newMethod.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
//				
//				AddParameter (newMethod, m.ParameterInfo);
//				
//				if (location != null)
//					newMethod.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
//				if (m.Block != null) {
//					INode bodyBlock = (INode)m.Block.Accept (this);
////					if (m.Block is ToplevelBlock) {
////						newMethod.AddChild (bodyBlock.FirstChild.NextSibling, MethodDeclaration.Roles.Body);
////					} else {
//						newMethod.AddChild (bodyBlock, MethodDeclaration.Roles.Body);
////					}
//				}
//				typeStack.Peek ().AddChild (newMethod, TypeDeclaration.Roles.Member);
//			}
//			
//			static Dictionary<Mono.CSharp.Modifiers, MonoDevelop.Projects.Dom.Modifiers> modifierTable = new Dictionary<Mono.CSharp.Modifiers, MonoDevelop.Projects.Dom.Modifiers> ();
//			static ConversionVisitor ()
//			{
//				modifierTable[Mono.CSharp.Modifiers.NEW] = MonoDevelop.Projects.Dom.Modifiers.New;
//				modifierTable[Mono.CSharp.Modifiers.PUBLIC] = MonoDevelop.Projects.Dom.Modifiers.Public;
//				modifierTable[Mono.CSharp.Modifiers.PROTECTED] = MonoDevelop.Projects.Dom.Modifiers.Protected;
//				modifierTable[Mono.CSharp.Modifiers.PRIVATE] = MonoDevelop.Projects.Dom.Modifiers.Private;
//				modifierTable[Mono.CSharp.Modifiers.INTERNAL] = MonoDevelop.Projects.Dom.Modifiers.Internal;
//				modifierTable[Mono.CSharp.Modifiers.ABSTRACT] = MonoDevelop.Projects.Dom.Modifiers.Abstract;
//				modifierTable[Mono.CSharp.Modifiers.VIRTUAL] = MonoDevelop.Projects.Dom.Modifiers.Virtual;
//				modifierTable[Mono.CSharp.Modifiers.SEALED] = MonoDevelop.Projects.Dom.Modifiers.Sealed;
//				modifierTable[Mono.CSharp.Modifiers.STATIC] = MonoDevelop.Projects.Dom.Modifiers.Static;
//				modifierTable[Mono.CSharp.Modifiers.OVERRIDE] = MonoDevelop.Projects.Dom.Modifiers.Override;
//				modifierTable[Mono.CSharp.Modifiers.READONLY] = MonoDevelop.Projects.Dom.Modifiers.Readonly;
////				modifierTable[Mono.CSharp.Modifiers.] = MonoDevelop.Projects.Dom.Modifiers.Const;
//				modifierTable[Mono.CSharp.Modifiers.PARTIAL] = MonoDevelop.Projects.Dom.Modifiers.Partial;
//				modifierTable[Mono.CSharp.Modifiers.EXTERN] = MonoDevelop.Projects.Dom.Modifiers.Extern;
//				modifierTable[Mono.CSharp.Modifiers.VOLATILE] = MonoDevelop.Projects.Dom.Modifiers.Volatile;
//				modifierTable[Mono.CSharp.Modifiers.UNSAFE] = MonoDevelop.Projects.Dom.Modifiers.Unsafe;
//				modifierTable[Mono.CSharp.Modifiers.OVERRIDE] = MonoDevelop.Projects.Dom.Modifiers.Overloads;
//			}
//			
//			void AddModifiers (AbstractNode parent, LocationsBag.MemberLocations location)
//			{
//				if (location == null || location.Modifiers == null)
//					return;
//				foreach (var modifier in location.Modifiers) {
//					parent.AddChild (new CSharpModifierToken (Convert (modifier.Item2), modifierTable[modifier.Item1]), AbstractNode.Roles.Modifier);
//				}
//			}
//			
//			public override void Visit (Property p)
//			{
//				PropertyDeclaration newProperty = new PropertyDeclaration ();
//				
//				var location = LocationsBag.GetMemberLocation (p);
//				AddModifiers (newProperty, location);
//				
//				newProperty.AddChild ((INode)p.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
//				newProperty.AddChild (new Identifier (p.MemberName.Name, Convert (p.MemberName.Location)), AbstractNode.Roles.Identifier);
//				if (location != null)
//					newProperty.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LBrace);
//				
//				if (p.Get != null) {
//					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
//					var getLocation = LocationsBag.GetMemberLocation (p.Get);
//					AddModifiers (getAccessor, getLocation);
//					getAccessor.AddChild (new CSharpTokenNode (Convert (p.Get.Location), "get".Length), PropertyDeclaration.Roles.Keyword);
//					
//					if (p.Get.Block != null)
//						getAccessor.AddChild ((INode)p.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
//					newProperty.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
//				}
//				
//				if (p.Set != null) {
//					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
//					var setLocation = LocationsBag.GetMemberLocation (p.Set);
//					AddModifiers (setAccessor, setLocation);
//					setAccessor.AddChild (new CSharpTokenNode (Convert (p.Set.Location), "set".Length), PropertyDeclaration.Roles.Keyword);
//					
//					if (p.Set.Block != null)
//						setAccessor.AddChild ((INode)p.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
//					newProperty.AddChild (setAccessor, PropertyDeclaration.PropertySetRole);
//				}
//				if (location != null)
//					newProperty.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RBrace);
//				
//				typeStack.Peek ().AddChild (newProperty, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (Constructor c)
//			{
//				ConstructorDeclaration newConstructor = new ConstructorDeclaration ();
//				var location = LocationsBag.GetMemberLocation (c);
//				AddModifiers (newConstructor, location);
//				newConstructor.AddChild (new Identifier (c.MemberName.Name, Convert (c.MemberName.Location)), AbstractNode.Roles.Identifier);
//				if (location != null) {
//					newConstructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
//					newConstructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
//				}
//				
//				if (c.Block != null)
//					newConstructor.AddChild ((INode)c.Block.Accept (this), ConstructorDeclaration.Roles.Body);
//				
//				typeStack.Peek ().AddChild (newConstructor, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (Destructor d)
//			{
//				DestructorDeclaration newDestructor = new DestructorDeclaration ();
//				var location = LocationsBag.GetMemberLocation (d);
//				AddModifiers (newDestructor, location);
//				if (location != null)
//					newDestructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), DestructorDeclaration.TildeRole);
//				newDestructor.AddChild (new Identifier (d.MemberName.Name, Convert (d.MemberName.Location)), AbstractNode.Roles.Identifier);
//				
//				if (location != null) {
//					newDestructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), DestructorDeclaration.Roles.LPar);
//					newDestructor.AddChild (new CSharpTokenNode (Convert (location[2]), 1), DestructorDeclaration.Roles.RPar);
//				}
//				
//				if (d.Block != null)
//					newDestructor.AddChild ((INode)d.Block.Accept (this), DestructorDeclaration.Roles.Body);
//				
//				typeStack.Peek ().AddChild (newDestructor, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (EventField e)
//			{
//				EventDeclaration newEvent = new EventDeclaration ();
//				
//				var location = LocationsBag.GetMemberLocation (e);
//				AddModifiers (newEvent, location);
//				
//				if (location != null)
//					newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
//				newEvent.AddChild ((INode)e.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
//				newEvent.AddChild (new Identifier (e.MemberName.Name, Convert (e.MemberName.Location)), EventDeclaration.Roles.Identifier);
//				if (location != null)
//					newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), EventDeclaration.Roles.Semicolon);
//				
//				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.Roles.Member);
//			}
//			
//			public override void Visit (EventProperty ep)
//			{
//				EventDeclaration newEvent = new EventDeclaration ();
//				
//				var location = LocationsBag.GetMemberLocation (ep);
//				AddModifiers (newEvent, location);
//				
//				if (location != null)
//					newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
//				newEvent.AddChild ((INode)ep.TypeName.Accept (this), EventDeclaration.Roles.ReturnType);
//				newEvent.AddChild (new Identifier (ep.MemberName.Name, Convert (ep.MemberName.Location)), EventDeclaration.Roles.Identifier);
//				if (location != null)
//					newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), 1), EventDeclaration.Roles.LBrace);
//				
//				if (ep.Add != null) {
//					MonoDevelop.CSharp.Dom.Accessor addAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
//					var addLocation = LocationsBag.GetMemberLocation (ep.Add);
//					AddModifiers (addAccessor, addLocation);
//					addAccessor.AddChild (new CSharpTokenNode (Convert (ep.Add.Location), "add".Length), EventDeclaration.Roles.Keyword);
//					if (ep.Add.Block != null)
//						addAccessor.AddChild ((INode)ep.Add.Block.Accept (this), EventDeclaration.Roles.Body);
//					newEvent.AddChild (addAccessor, EventDeclaration.EventAddRole);
//				}
//				
//				if (ep.Remove != null) {
//					MonoDevelop.CSharp.Dom.Accessor removeAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
//					var removeLocation = LocationsBag.GetMemberLocation (ep.Remove);
//					AddModifiers (removeAccessor, removeLocation);
//					removeAccessor.AddChild (new CSharpTokenNode (Convert (ep.Remove.Location), "remove".Length), EventDeclaration.Roles.Keyword);
//					
//					if (ep.Remove.Block != null)
//						removeAccessor.AddChild ((INode)ep.Remove.Block.Accept (this), EventDeclaration.Roles.Body);
//					newEvent.AddChild (removeAccessor, EventDeclaration.EventRemoveRole);
//				}
//				if (location != null)
//					newEvent.AddChild (new CSharpTokenNode (Convert (location[2]), 1), EventDeclaration.Roles.RBrace);
//				
//				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.Roles.Member);
//			}
//			
			#endregion
		}
	}
}