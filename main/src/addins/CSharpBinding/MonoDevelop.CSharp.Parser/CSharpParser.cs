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
				
				LocationDescriptor location = LocationStorage.Get (c);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "class".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (c.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				if (c.MemberName.TypeArguments != null)  {
					LocationDescriptor typeArgLocation = LocationStorage.Get (c.MemberName);
					newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (newType, typeArgLocation, c.MemberName.TypeArguments);
					newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, c);
				}
				
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
				
				LocationDescriptor location = LocationStorage.Get (s);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "struct".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (s.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				if (s.MemberName.TypeArguments != null)  {
					LocationDescriptor typeArgLocation = LocationStorage.Get (s.MemberName);
					newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (newType, typeArgLocation, s.MemberName.TypeArguments);
					newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, s);
				}
				
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
				
				LocationDescriptor location = LocationStorage.Get (i);
				AddModifiers (newType, location);
				newType.AddChild (new CSharpTokenNode (Convert (location[0]), "interface".Length), TypeDeclaration.TypeKeyword);
				newType.AddChild (new Identifier (i.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				if (i.MemberName.TypeArguments != null)  {
					LocationDescriptor typeArgLocation = LocationStorage.Get (i.MemberName);
					newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (newType, typeArgLocation, i.MemberName.TypeArguments);
					newType.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newType, i);
				}
				
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
				LocationDescriptor location = LocationStorage.Get (d);
				
				AddModifiers (newDelegate, location);
				
				newDelegate.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), TypeDeclaration.TypeKeyword);
				newDelegate.AddChild ((INode)d.ReturnType.Accept (this), AbstractNode.Roles.ReturnType);
				newDelegate.AddChild (new Identifier (d.Name, Convert (location[1])), AbstractNode.Roles.Identifier);
				if (d.MemberName.TypeArguments != null)  {
					LocationDescriptor typeArgLocation = LocationStorage.Get (d.MemberName);
					newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (newDelegate, typeArgLocation, d.MemberName.TypeArguments);
					newDelegate.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					AddConstraints (newDelegate, d);
				}
				
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
				LocationDescriptor location = LocationStorage.Get (e);
				
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
			
			public override object Visit (EnumInitializer initializer)
			{
				return initializer.Expr != null ? initializer.Expr.Accept (this) : null;
			}
			#endregion
			
			#region Type members
			public override void Visit (FixedField f)
			{
				LocationDescriptor location = LocationStorage.Get (f);
				
				FieldDeclaration newField;
				
				DomLocation semicolonLocation = Convert (location[0]);
				if (!visitedFields.TryGetValue (semicolonLocation, out newField)) {
					newField = new FieldDeclaration ();
					
					newField.AddChild (new CSharpTokenNode (Convert (location[1]), "fixed".Length), FieldDeclaration.Roles.Keyword);
					newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
					newField.AddChild (new CSharpTokenNode (semicolonLocation, 1), FieldDeclaration.Roles.Semicolon);
					
					typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
					
					visitedFields[semicolonLocation] = newField;
				} else {
					newField.InsertChildBefore (newField.Semicolon, new CSharpTokenNode (Convert (location.LocationList [newField.Variables.Count () - 1]), 1), FieldDeclaration.Roles.Comma);
				}
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), AbstractNode.Roles.Identifier);
				newField.InsertChildBefore (newField.Semicolon, variable, AbstractNode.Roles.Initializer);
			}
			
			Dictionary<DomLocation, FieldDeclaration> visitedFields = new Dictionary<DomLocation, FieldDeclaration> ();
			public override void Visit (Field f)
			{
				LocationDescriptor location = LocationStorage.Get (f);
				
				FieldDeclaration newField;
				
				DomLocation semicolonLocation = Convert (location[0]);
				if (!visitedFields.TryGetValue (semicolonLocation, out newField)) {
					newField = new FieldDeclaration ();
					newField.AddChild ((INode)f.TypeName.Accept (this), FieldDeclaration.Roles.ReturnType);
					newField.AddChild (new CSharpTokenNode (semicolonLocation, 1), FieldDeclaration.Roles.Semicolon);
					
					typeStack.Peek ().AddChild (newField, TypeDeclaration.Roles.Member);
					
					visitedFields[semicolonLocation] = newField;
				} else {
					newField.InsertChildBefore (newField.Semicolon, new CSharpTokenNode (Convert (location.LocationList [newField.Variables.Count () - 1]), 1), FieldDeclaration.Roles.Comma);
				}
				
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (f.MemberName.Name, Convert (f.MemberName.Location)), AbstractNode.Roles.Identifier);
				newField.InsertChildBefore (newField.Semicolon, variable, AbstractNode.Roles.Initializer);
			}
			
			public override void Visit (Operator o)
			{
				OperatorDeclaration newOperator = new OperatorDeclaration ();
				newOperator.OperatorType = (OperatorType)o.OperatorType;
				
				LocationDescriptor location = LocationStorage.Get (o);
				AddModifiers (newOperator, location);
				
				newOperator.AddChild ((INode)o.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				
				if (o.OperatorType == Operator.OpType.Implicit) {
					newOperator.AddChild (new CSharpTokenNode (Convert (location[3]), "implicit".Length), OperatorDeclaration.OperatorTypeRole);
					newOperator.AddChild (new CSharpTokenNode (Convert (location[2]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
				} else if (o.OperatorType == Operator.OpType.Explicit) {
					newOperator.AddChild (new CSharpTokenNode (Convert (location[3]), "explicit".Length), OperatorDeclaration.OperatorTypeRole);
					newOperator.AddChild (new CSharpTokenNode (Convert (location[2]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
				} else {
					newOperator.AddChild (new CSharpTokenNode (Convert (location[2]), "operator".Length), OperatorDeclaration.OperatorKeywordRole);
					
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
					newOperator.AddChild (new CSharpTokenNode (Convert (location[3]), opLength), OperatorDeclaration.OperatorTypeRole);
				}
				
				newOperator.AddChild (new CSharpTokenNode (Convert (location[0]), 1), OperatorDeclaration.Roles.LPar);
				AddParameter (newOperator, o.ParameterInfo);
				newOperator.AddChild (new CSharpTokenNode (Convert (location[1]), 1), OperatorDeclaration.Roles.RPar);
				
				if (o.Block != null)
					newOperator.AddChild ((INode)o.Block.Accept (this), OperatorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newOperator, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Indexer indexer)
			{
				IndexerDeclaration newIndexer = new IndexerDeclaration ();
				
				LocationDescriptor location = LocationStorage.Get (indexer);
				AddModifiers (newIndexer, location);
				
				newIndexer.AddChild ((INode)indexer.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IndexerDeclaration.Roles.LBracket);
				AddParameter (newIndexer, indexer.parameters);
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IndexerDeclaration.Roles.RBracket);
				
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[2]), 1), IndexerDeclaration.Roles.LBrace);
				if (indexer.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					LocationDescriptor getLocation = LocationStorage.Get (indexer.Get);
					AddModifiers (getAccessor, getLocation);
					getAccessor.AddChild (new CSharpTokenNode (Convert (getLocation[0]), "get".Length), PropertyDeclaration.Roles.Keyword);
					if (indexer.Get.Block != null)
						getAccessor.AddChild ((INode)indexer.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					newIndexer.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
				}
				
				if (indexer.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					LocationDescriptor setLocation = LocationStorage.Get (indexer.Set);
					AddModifiers (setAccessor, setLocation);
					setAccessor.AddChild (new CSharpTokenNode (Convert (setLocation[0]), "set".Length), PropertyDeclaration.Roles.Keyword);
					
					if (indexer.Set.Block != null)
						setAccessor.AddChild ((INode)indexer.Set.Block.Accept (this), MethodDeclaration.Roles.Body);
					newIndexer.AddChild (setAccessor, PropertyDeclaration.PropertySetRole);
				}
				
				newIndexer.AddChild (new CSharpTokenNode (Convert (location[3]), 1), IndexerDeclaration.Roles.RBrace);
				
				typeStack.Peek ().AddChild (newIndexer, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Method m)
			{
				MethodDeclaration newMethod = new MethodDeclaration ();
				
				LocationDescriptor location = LocationStorage.Get (m);
				AddModifiers (newMethod, location);
				
				newMethod.AddChild ((INode)m.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				newMethod.AddChild (new Identifier (m.Name, Convert (m.Location)), AbstractNode.Roles.Identifier);
				
				if (m.MemberName.TypeArguments != null)  {
					LocationDescriptor typeArgLocation = LocationStorage.Get (m.MemberName);
					newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (newMethod, typeArgLocation, m.MemberName.TypeArguments);
					newMethod.AddChild (new CSharpTokenNode (Convert (typeArgLocation[1]), 1), MemberReferenceExpression.Roles.RChevron);
					
					AddConstraints (newMethod, m.GenericMethod);
				}
				
				newMethod.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				
				AddParameter (newMethod, m.ParameterInfo);
				
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
			
			void AddModifiers (AbstractNode parent, Mono.CSharp.LocationDescriptor location)
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
				
				LocationDescriptor location = LocationStorage.Get (p);
				AddModifiers (newProperty, location);
				
				newProperty.AddChild ((INode)p.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				newProperty.AddChild (new Identifier (p.MemberName.Name, Convert (p.MemberName.Location)), AbstractNode.Roles.Identifier);
				newProperty.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LBrace);
				
				if (p.Get != null) {
					MonoDevelop.CSharp.Dom.Accessor getAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					LocationDescriptor getLocation = LocationStorage.Get (p.Get);
					AddModifiers (getAccessor, getLocation);
					getAccessor.AddChild (new CSharpTokenNode (Convert (getLocation[0]), "get".Length), PropertyDeclaration.Roles.Keyword);
					
					if (p.Get.Block != null)
						getAccessor.AddChild ((INode)p.Get.Block.Accept (this), MethodDeclaration.Roles.Body);
					newProperty.AddChild (getAccessor, PropertyDeclaration.PropertyGetRole);
				}
				
				if (p.Set != null) {
					MonoDevelop.CSharp.Dom.Accessor setAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					LocationDescriptor setLocation = LocationStorage.Get (p.Set);
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
				LocationDescriptor location = LocationStorage.Get (c);
				AddModifiers (newConstructor, location);
				newConstructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				newConstructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
				
				if (c.Block != null)
					newConstructor.AddChild ((INode)c.Block.Accept (this), ConstructorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newConstructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (Destructor d)
			{
				DestructorDeclaration newDestructor = new DestructorDeclaration ();
				LocationDescriptor location = LocationStorage.Get (d);
				AddModifiers (newDestructor, location);
				newDestructor.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MethodDeclaration.Roles.LPar);
				newDestructor.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MethodDeclaration.Roles.RPar);
				
				if (d.Block != null)
					newDestructor.AddChild ((INode)d.Block.Accept (this), DestructorDeclaration.Roles.Body);
				
				typeStack.Peek ().AddChild (newDestructor, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (EventField e)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				
				LocationDescriptor location = LocationStorage.Get (e);
				AddModifiers (newEvent, location);
				
				newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
				newEvent.AddChild ((INode)e.TypeName.Accept (this), AbstractNode.Roles.ReturnType);
				newEvent.AddChild (new Identifier (e.MemberName.Name, Convert (e.MemberName.Location)), EventDeclaration.Roles.Identifier);
				newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), ";".Length), EventDeclaration.Roles.Semicolon);
				
				typeStack.Peek ().AddChild (newEvent, TypeDeclaration.Roles.Member);
			}
			
			public override void Visit (EventProperty ep)
			{
				EventDeclaration newEvent = new EventDeclaration ();
				
				LocationDescriptor location = LocationStorage.Get (ep);
				AddModifiers (newEvent, location);
				
				newEvent.AddChild (new CSharpTokenNode (Convert (location[0]), "event".Length), EventDeclaration.Roles.Keyword);
				newEvent.AddChild ((INode)ep.TypeName.Accept (this), EventDeclaration.Roles.ReturnType);
				newEvent.AddChild (new Identifier (ep.MemberName.Name, Convert (ep.MemberName.Location)), EventDeclaration.Roles.Identifier);
				newEvent.AddChild (new CSharpTokenNode (Convert (location[1]), 1), EventDeclaration.Roles.LBrace);
				
				if (ep.Add != null) {
					MonoDevelop.CSharp.Dom.Accessor addAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					LocationDescriptor addLocation = LocationStorage.Get (ep.Add);
					AddModifiers (addAccessor, addLocation);
					addAccessor.AddChild (new CSharpTokenNode (Convert (addLocation[0]), "add".Length), EventDeclaration.Roles.Keyword);
					if (ep.Add.Block != null)
						addAccessor.AddChild ((INode)ep.Add.Block.Accept (this), EventDeclaration.Roles.Body);
					newEvent.AddChild (addAccessor, EventDeclaration.EventAddRole);
				}
				
				if (ep.Remove != null) {
					MonoDevelop.CSharp.Dom.Accessor removeAccessor = new MonoDevelop.CSharp.Dom.Accessor ();
					LocationDescriptor removeLocation = LocationStorage.Get (ep.Remove);
					AddModifiers (removeAccessor, removeLocation);
					removeAccessor.AddChild (new CSharpTokenNode (Convert (removeLocation[0]), "remove".Length), EventDeclaration.Roles.Keyword);
					
					if (ep.Remove.Block != null)
						removeAccessor.AddChild ((INode)ep.Remove.Block.Accept (this), EventDeclaration.Roles.Body);
					newEvent.AddChild (removeAccessor, EventDeclaration.EventRemoveRole);
				}
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
				
				LocationDescriptor location = LocationStorage.Get (ifStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "if".Length), IfElseStatement.IfKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), IfElseStatement.Roles.LPar);
				result.AddChild ((INode)ifStatement.Expr.Accept (this), IfElseStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), IfElseStatement.Roles.RPar);
				
				result.AddChild ((INode)ifStatement.TrueStatement.Accept (this), IfElseStatement.TrueEmbeddedStatementRole);
				
				if (ifStatement.FalseStatement != null) {
					result.AddChild (new CSharpTokenNode (Convert (location[3]), "else".Length), IfElseStatement.ElseKeywordRole);
					result.AddChild ((INode)ifStatement.FalseStatement.Accept (this), IfElseStatement.FalseEmbeddedStatementRole);
				}
				
				return result;
			}
			
			public override object Visit (Do doStatement)
			{
				var result = new WhileStatement (WhilePosition.End);
				LocationDescriptor location = LocationStorage.Get (doStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "do".Length), WhileStatement.DoKeywordRole);
				result.AddChild ((INode)doStatement.EmbeddedStatement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[3]), "while".Length), WhileStatement.WhileKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), WhileStatement.Roles.LPar);
				result.AddChild ((INode)doStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), WhileStatement.Roles.RPar);
				result.AddChild (new CSharpTokenNode (Convert (location[4]), 1), WhileStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (While whileStatement)
			{
				var result = new WhileStatement (WhilePosition.Begin);
				LocationDescriptor location = LocationStorage.Get (whileStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "while".Length), WhileStatement.WhileKeywordRole);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), WhileStatement.Roles.LPar);
				result.AddChild ((INode)whileStatement.expr.Accept (this), WhileStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), WhileStatement.Roles.RPar);
				
				result.AddChild ((INode)whileStatement.Statement.Accept (this), WhileStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (For forStatement)
			{
				var result = new ForStatement ();
				
				LocationDescriptor location = LocationStorage.Get (forStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "for".Length), ForStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ForStatement.Roles.LPar);
				if (forStatement.InitStatement != null)
					result.AddChild ((INode)forStatement.InitStatement.Accept (this), ForStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Test != null)
					result.AddChild ((INode)forStatement.Test.Accept (this), ForStatement.Roles.Condition);
				result.AddChild (new CSharpTokenNode (Convert (location[4]), 1), ForStatement.Roles.Semicolon);
				if (forStatement.Increment != null)
					result.AddChild ((INode)forStatement.Increment.Accept (this), ForStatement.Roles.Iterator);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ForStatement.Roles.RPar);
				
				result.AddChild ((INode)forStatement.Statement.Accept (this), ForStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (StatementExpression statementExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.ExpressionStatement ();
				LocationDescriptor location = LocationStorage.Get (statementExpression);
				
				result.AddChild ((INode)statementExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ExpressionStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MonoDevelop.CSharp.Dom.ExpressionStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Return returnStatement)
			{
				var result = new ReturnStatement ();
				LocationDescriptor location = LocationStorage.Get (returnStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "return".Length), ReturnStatement.Roles.Keyword);
				if (returnStatement.Expr != null)
					result.AddChild ((INode)returnStatement.Expr.Accept (this), ReturnStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ReturnStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (Goto gotoStatement)
			{
				var result = new GotoStatement (GotoType.Label);
				LocationDescriptor location = LocationStorage.Get (gotoStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new Identifier (gotoStatement.Target, Convert (gotoStatement.loc)), GotoStatement.Roles.Identifier);
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
				LocationDescriptor location = LocationStorage.Get (gotoDefault);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "default".Length), GotoStatement.DefaultKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), GotoStatement.Roles.Semicolon);
				
				return result;
			}
			
			public override object Visit (GotoCase gotoCase)
			{
				var result = new GotoStatement (GotoType.Case);
				LocationDescriptor location = LocationStorage.Get (gotoCase);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "goto".Length), GotoStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "case".Length), GotoStatement.CaseKeywordRole);
				result.AddChild ((INode)gotoCase.Expr.Accept (this), GotoStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), GotoStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Throw throwStatement)
			{
				var result = new ThrowStatement ();
				LocationDescriptor location = LocationStorage.Get (throwStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "throw".Length), ThrowStatement.Roles.Keyword);
				if (throwStatement.Expr != null)
					result.AddChild ((INode)throwStatement.Expr.Accept (this), ThrowStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ThrowStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Break breakStatement)
			{
				var result = new BreakStatement ();
				LocationDescriptor location = LocationStorage.Get (breakStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "break".Length), BreakStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), BreakStatement.Roles.Semicolon);
				return result;
			}
			
			public override object Visit (Continue continueStatement)
			{
				var result = new ContinueStatement ();
				LocationDescriptor location = LocationStorage.Get (continueStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "continue".Length), ContinueStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ContinueStatement.Roles.Semicolon);
				return result;
			}
			
			public static bool IsLower (Location left, Location right)
			{
				return left.Row < right.Row || left.Row == right.Row && left.Column < right.Column;
			}
			public override object Visit (Block blockStatement)
			{
				if (blockStatement.IsGenerated)
					return blockStatement.Statements.Last ().Accept (this);
				var result = new BlockStatement ();
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.StartLocation), 1), AbstractCSharpNode.Roles.LBrace);
				int curLocal = 0;
				List<LocalInfo> localVariables = new List<LocalInfo> (blockStatement.Variables.Values);
				foreach (Statement stmt in blockStatement.Statements) {
					if (curLocal < localVariables.Count && IsLower (localVariables[curLocal].Location, stmt.loc)) {
						result.AddChild (CreateVariableDeclaration (localVariables[curLocal]), AbstractCSharpNode.Roles.Statement);
						curLocal++;
					}
					result.AddChild ((INode)stmt.Accept (this), AbstractCSharpNode.Roles.Statement);
				}
				
				while (curLocal < localVariables.Count) {
					result.AddChild (CreateVariableDeclaration (localVariables[curLocal]), AbstractCSharpNode.Roles.Statement);
					curLocal++;
				}
				
				result.AddChild (new CSharpTokenNode (Convert (blockStatement.EndLocation), 1), AbstractCSharpNode.Roles.RBrace);
				
				return result;
			}
			
			VariableDeclarationStatement CreateVariableDeclaration (LocalInfo info)
			{
				VariableDeclarationStatement result = new VariableDeclarationStatement ();
				result.AddChild ((INode)info.Type.Accept (this), CatchClause.Roles.ReturnType);
				VariableInitializer variable = new VariableInitializer ();
				variable.AddChild (new Identifier (info.Name, Convert (info.Location)), AbstractNode.Roles.Identifier);
				result.AddChild (variable, AbstractNode.Roles.Initializer);
				
				result.AddChild (new Identifier (info.Name, Convert (info.Location)), CatchClause.Roles.ReturnType);
				return result;
			}
			
			public override object Visit (Switch switchStatement)
			{
				var result = new SwitchStatement ();
				
				LocationDescriptor location = LocationStorage.Get (switchStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "switch".Length), SwitchStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), SwitchStatement.Roles.LPar);
				result.AddChild ((INode)switchStatement.Expr.Accept (this), SwitchStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), SwitchStatement.Roles.RPar);
				
				result.AddChild (new CSharpTokenNode (Convert (location[3]), 1), SwitchStatement.Roles.LBrace);
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
				
				result.AddChild (new CSharpTokenNode (Convert (location[4]), 1), SwitchStatement.Roles.RBrace);
				return result;
			}
			
			public override object Visit (Lock lockStatement)
			{
				var result = new LockStatement ();
				LocationDescriptor location = LocationStorage.Get (lockStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "lock".Length), LockStatement.Roles.Keyword);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), LockStatement.Roles.LPar);
				result.AddChild ((INode)lockStatement.Expr.Accept (this), LockStatement.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), LockStatement.Roles.RPar);
				result.AddChild ((INode)lockStatement.Statement.Accept (this), LockStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Unchecked uncheckedStatement)
			{
				var result = new UncheckedStatement ();
				LocationDescriptor location = LocationStorage.Get (uncheckedStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "unchecked".Length), UncheckedStatement.Roles.Keyword);
				result.AddChild ((INode)uncheckedStatement.Block.Accept (this), UncheckedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			
			public override object Visit (Checked checkedStatement)
			{
				var result = new CheckedStatement ();
				LocationDescriptor location = LocationStorage.Get (checkedStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "checked".Length), CheckedStatement.Roles.Keyword);
				result.AddChild ((INode)checkedStatement.Block.Accept (this), CheckedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (Unsafe unsafeStatement)
			{
				var result = new UnsafeStatement ();
				LocationDescriptor location = LocationStorage.Get (unsafeStatement);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "unsafe".Length), UnsafeStatement.Roles.Keyword);
				result.AddChild ((INode)unsafeStatement.Block.Accept (this), UnsafeStatement.Roles.Body);
				return result;
			}
			
			public override object Visit (Fixed fixedStatement)
			{
				var result = new FixedStatement ();
				LocationDescriptor location = LocationStorage.Get (fixedStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "fixed".Length), FixedStatement.FixedKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FixedStatement.Roles.LPar);
				
				result.AddChild ((INode)fixedStatement.Type.Accept (this), FixedStatement.PointerDeclarationRole);
				
				foreach (KeyValuePair<LocalInfo, Expression> declarator in fixedStatement.Declarators) {
					result.AddChild ((INode)declarator.Value.Accept (this), FixedStatement.DeclaratorRole);
				}
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), FixedStatement.Roles.RPar);
				result.AddChild ((INode)fixedStatement.Statement.Accept (this), FixedStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (TryFinally tryFinallyStatement)
			{
				TryCatchStatement result;
				LocationDescriptor location = LocationStorage.Get (tryFinallyStatement);
				
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
				LocationDescriptor location = LocationStorage.Get (ctch);
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
				LocationDescriptor location = LocationStorage.Get (tryCatchStatement);
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
				LocationDescriptor location = LocationStorage.Get (usingStatement);
				// TODO: Usings with more than 1 variable are compiled differently using (a) { using (b) { using (c) ...}}
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "using".Length), UsingStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), UsingStatement.Roles.LPar);
				
				if (usingStatement.Var != null)
					result.AddChild ((INode)usingStatement.Var.Accept (this), UsingStatement.Roles.Identifier);
				
				if (usingStatement.Init != null)
					result.AddChild ((INode)usingStatement.Init.Accept (this), UsingStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), UsingStatement.Roles.RPar);
				
				result.AddChild ((INode)usingStatement.EmbeddedStatement.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			public override object Visit (UsingTemporary usingTemporary)
			{
				var result = new UsingStatement ();
				LocationDescriptor location = LocationStorage.Get (usingTemporary);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "using".Length), UsingStatement.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), UsingStatement.Roles.LPar);
				
				result.AddChild ((INode)usingTemporary.Expr.Accept (this), UsingStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), UsingStatement.Roles.RPar);
				
				result.AddChild ((INode)usingTemporary.Statement.Accept (this), UsingStatement.Roles.EmbeddedStatement);
				return result;
			}
			
			
			public override object Visit (Foreach foreachStatement)
			{
				var result = new ForeachStatement ();
				
				LocationDescriptor location = LocationStorage.Get (foreachStatement);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "foreach".Length), ForeachStatement.ForEachKeywordRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ForeachStatement.Roles.LPar);
				
				if (foreachStatement.TypeExpr == null)
					result.AddChild ((INode)foreachStatement.TypeExpr.Accept (this), ForeachStatement.Roles.ReturnType);
				if (foreachStatement.Variable != null)
					result.AddChild ((INode)foreachStatement.Variable.Accept (this), ForeachStatement.Roles.Identifier);
				
				result.AddChild (new CSharpTokenNode (Convert (location[3]), "in".Length), ForeachStatement.InKeywordRole);
				
				if (foreachStatement.Expr != null)
					result.AddChild ((INode)foreachStatement.Expr.Accept (this), ForeachStatement.Roles.Initializer);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ForeachStatement.Roles.RPar);
				result.AddChild ((INode)foreachStatement.Statement.Accept (this), ForeachStatement.Roles.EmbeddedStatement);
				
				return result;
			}
			
			public override object Visit (Yield yieldStatement)
			{
				var result = new YieldStatement ();
				LocationDescriptor location = LocationStorage.Get (yieldStatement);
				
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
				LocationDescriptor location = LocationStorage.Get (yieldBreakStatement);
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
				var result = new FullTypeName ();
				if (!string.IsNullOrEmpty (typeLookupExpression.Namespace)) {
					result.AddChild (new Identifier (typeLookupExpression.Namespace + "." + typeLookupExpression.Name, Convert (typeLookupExpression.Location)));
				} else {
					result.AddChild (new Identifier (typeLookupExpression.Name, Convert (typeLookupExpression.Location)));
				}
				return result;
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
				if (memberAccess.TypeArguments != null)  {
					LocationDescriptor location = LocationStorage.Get (memberAccess);
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MemberReferenceExpression.Roles.LChevron);
					AddTypeArguments (result, location, memberAccess.TypeArguments);
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
				var result = new FullTypeName ();
				result.AddChild (new Identifier (simpleName.Name, Convert (simpleName.Location)), FullTypeName.Roles.Identifier);
				if (simpleName.TypeArguments != null)  {
					LocationDescriptor location = LocationStorage.Get (simpleName);
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), FullTypeName.Roles.LChevron);
					AddTypeArguments (result, location, simpleName.TypeArguments);
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
				LocationDescriptor location = LocationStorage.Get (parenthesizedExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.LPar);
				result.AddChild ((INode)parenthesizedExpression.Expr.Accept (this), MonoDevelop.CSharp.Dom.ParenthesizedExpression.Roles.Expression);
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
				LocationDescriptor location = LocationStorage.Get (unaryMutatorExpression);
				switch (unaryMutatorExpression.UnaryMutatorMode) {
				case UnaryMutator.Mode.PostDecrement:
					result.UnaryOperatorType = UnaryOperatorType.PostDecrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), UnaryOperatorExpression.Operator);
					break;
				case UnaryMutator.Mode.PostIncrement:
					result.UnaryOperatorType = UnaryOperatorType.PostIncrement;
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), UnaryOperatorExpression.Operator);
					break;
					
				case UnaryMutator.Mode.PreIncrement:
					result.UnaryOperatorType = UnaryOperatorType.Increment;
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				case UnaryMutator.Mode.PreDecrement:
					result.UnaryOperatorType = UnaryOperatorType.Decrement;
					result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), UnaryOperatorExpression.Operator);
					result.AddChild (expression, UnaryOperatorExpression.Roles.Expression);
					break;
				}
				
				return result;
			}
			
			public override object Visit (Indirection indirectionExpression)
			{
				var result = new UnaryOperatorExpression ();
				result.UnaryOperatorType = UnaryOperatorType.Dereference;
				LocationDescriptor location = LocationStorage.Get (indirectionExpression);
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
				LocationDescriptor location = LocationStorage.Get (castExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CastExpression.Roles.LPar);
				if (castExpression.TargetType != null)
					result.AddChild ((INode)castExpression.TargetType.Accept (this), CastExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), CastExpression.Roles.RPar);
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
				LocationDescriptor location = LocationStorage.Get (defaultValueExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), CastExpression.Roles.LPar);
				result.AddChild ((INode)defaultValueExpression.Expr.Accept (this), CastExpression.Roles.ReturnType);
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
				LocationDescriptor location = LocationStorage.Get (binaryExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), opLength), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)binaryExpression.Left.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Nullable.NullCoalescingOperator nullCoalescingOperator)
			{
				var result = new BinaryOperatorExpression ();
				result.BinaryOperatorType = BinaryOperatorType.NullCoalescing;
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.LeftExpressionRole);
				LocationDescriptor location = LocationStorage.Get (nullCoalescingOperator);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 2), BinaryOperatorExpression.OperatorRole);
				result.AddChild ((INode)nullCoalescingOperator.Left.Accept (this), BinaryOperatorExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (Conditional conditionalExpression)
			{
				var result = new ConditionalExpression ();
				
				result.AddChild ((INode)conditionalExpression.Expr.Accept (this), ConditionalExpression.Roles.Condition);
				LocationDescriptor location = LocationStorage.Get (conditionalExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ConditionalExpression.Roles.QuestionMark);
				result.AddChild ((INode)conditionalExpression.TrueExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ConditionalExpression.Roles.Colon);
				result.AddChild ((INode)conditionalExpression.FalseExpr.Accept (this), ConditionalExpression.FalseExpressionRole);
				return result;
			}
			
			void AddParameter (AbstractCSharpNode parent, Mono.CSharp.ParametersCompiled parameters)
			{
				if (parameters == null || !parameters.HasParams)
					return;
				LocationDescriptor paramLocation = LocationStorage.Get (parameters);
				
				for (int i = 0; i < parameters.Count; i++) {
					if (paramLocation.LocationList != null && i > 0 && i - 1 < paramLocation.LocationList.Count) 
						parent.AddChild (new CSharpTokenNode (Convert (paramLocation.LocationList[i - 1]), 1), ParameterDeclarationExpression.Roles.Comma);
					Parameter p = parameters[i];
					LocationDescriptor location = LocationStorage.Get (p);
					
					ParameterDeclarationExpression parameterDeclarationExpression = new ParameterDeclarationExpression ();
					switch (p.ParameterModifier) {
					case Parameter.Modifier.OUT:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Out;
						parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location.LocationList[0]), "out".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					case Parameter.Modifier.REF:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Ref;
						parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location.LocationList[0]), "ref".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					case Parameter.Modifier.PARAMS:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.Params;
						parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location.LocationList[0]), "params".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					case Parameter.Modifier.This:
						parameterDeclarationExpression.ParameterModifier = ParameterModifier.This;
						parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location.LocationList[0]), "this".Length), ParameterDeclarationExpression.Roles.Keyword);
						break;
					}
					parameterDeclarationExpression.AddChild ((INode)p.TypeName.Accept (this), ParameterDeclarationExpression.Roles.ReturnType);
					parameterDeclarationExpression.AddChild (new Identifier (p.Name, Convert (p.Location)), ParameterDeclarationExpression.Roles.Identifier);
					if (p.DefaultExpression != null) {
						parameterDeclarationExpression.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ParameterDeclarationExpression.Roles.Assign);
						parameterDeclarationExpression.AddChild ((INode)p.DefaultExpression.Accept (this), ParameterDeclarationExpression.Roles.Expression);
					}
					parent.AddChild (parameterDeclarationExpression, InvocationExpression.Roles.Argument);
				}
			}

			void AddTypeArguments (AbstractCSharpNode parent, LocationDescriptor location, Mono.CSharp.TypeArguments typeArguments)
			{
				if (typeArguments == null)
					return;
				for (int i = 0; i < typeArguments.Count; i++) {
					if (location.LocationList != null && i > 0 && i - 1 < location.LocationList.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location.LocationList[i - 1]), 1), InvocationExpression.Roles.Comma);
					parent.AddChild ((INode)typeArguments.Args[i].Accept (this), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddConstraints (AbstractCSharpNode parent, DeclSpace d)
			{
				if (d == null || d.Constraints == null)
					return;
				for (int i = 0; i < d.Constraints.Count; i++) {
					Constraints c = d.Constraints[i];
					LocationDescriptor location = LocationStorage.Get (c);
					var constraint = new Constraint ();
					parent.AddChild (new CSharpTokenNode (Convert (location[0]), "where".Length), InvocationExpression.Roles.Keyword);
					parent.AddChild (new Identifier (c.TypeParameter.Value, Convert (c.TypeParameter.Location)), InvocationExpression.Roles.Identifier);
					parent.AddChild (new CSharpTokenNode (Convert (location[1]), 1), InvocationExpression.Roles.Colon);
					foreach (var expr in c.ConstraintExpressions)
						parent.AddChild ((INode)expr.Accept (this), InvocationExpression.Roles.TypeArgument);
				}
			}
			
			void AddArguments (AbstractCSharpNode parent, LocationDescriptor location, Mono.CSharp.Arguments args)
			{
				if (args == null)
					return;
				for (int i = 0; i < args.Count; i++) {
					if (location.LocationList != null && i > 0 && i - 1 < location.LocationList.Count)
						parent.AddChild (new CSharpTokenNode (Convert (location.LocationList[i - 1]), 1), InvocationExpression.Roles.Comma);
					parent.AddChild ((INode)args[i].Expr.Accept (this), InvocationExpression.Roles.Argument);
				}
			}
			
			public override object Visit (Invocation invocationExpression)
			{
				var result = new InvocationExpression ();
				LocationDescriptor location = LocationStorage.Get (invocationExpression);
				result.AddChild ((INode)invocationExpression.Expr.Accept (this), InvocationExpression.Roles.TargetExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), InvocationExpression.Roles.LPar);
				AddArguments (result, location, invocationExpression.Arguments);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), InvocationExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (New newExpression)
			{
				var result = new ObjectCreateExpression ();
				
				LocationDescriptor location = LocationStorage.Get (newExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "new".Length), ObjectCreateExpression.Roles.Keyword);
				
				if (newExpression.NewType != null)
					result.AddChild ((INode)newExpression.NewType.Accept (this), ObjectCreateExpression.Roles.ReturnType);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ObjectCreateExpression.Roles.LPar);
				AddArguments (result, location, newExpression.NewArguments);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ObjectCreateExpression.Roles.RPar);
				
				return result;
			}
			
			public override object Visit (ArrayCreation arrayCreationExpression)
			{
				var result = new ArrayObjectCreateExpression ();
				
				var location = LocationStorage.Get (arrayCreationExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "new".Length), ArrayObjectCreateExpression.Roles.Keyword);
				
				if (arrayCreationExpression.NewType != null)
					result.AddChild ((INode)arrayCreationExpression.NewType.Accept (this), ArrayObjectCreateExpression.Roles.ReturnType);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ArrayObjectCreateExpression.Roles.LBracket);
				if (arrayCreationExpression.Arguments != null) {
					for (int i = 0 ;i < arrayCreationExpression.Arguments.Count; i++) {
						if (i > 0)
							result.AddChild (new CSharpTokenNode (Convert (location.LocationList [i - 1]), 1), IndexerExpression.Roles.Comma);
						result.AddChild ((INode)arrayCreationExpression.Arguments[i].Accept (this), ObjectCreateExpression.Roles.Initializer);
					}
				}
				result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), ArrayObjectCreateExpression.Roles.RBracket);
				
				if (arrayCreationExpression.Initializers != null) {
					var initLocation = LocationStorage.Get (arrayCreationExpression.Initializers);
					result.AddChild (new CSharpTokenNode (Convert (initLocation[0]), 1), ArrayObjectCreateExpression.Roles.LBrace);
					if (arrayCreationExpression.Initializers.Elements != null) {
						for (int i = 0; i < arrayCreationExpression.Initializers.Elements.Count; i++) {
							if (i > 0)
								result.AddChild (new CSharpTokenNode (Convert (initLocation.LocationList [i - 1]), 1), IndexerExpression.Roles.Comma);
							result.AddChild ((INode)arrayCreationExpression.Initializers.Elements[i].Accept (this), ObjectCreateExpression.Roles.Initializer);
						}
					}
					result.AddChild (new CSharpTokenNode (Convert (initLocation[1]), 1), ArrayObjectCreateExpression.Roles.RBrace);
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
				LocationDescriptor location = LocationStorage.Get (argListExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), ArgListExpression.Roles.LPar);
				
				AddArguments (result, location, argListExpression.Arguments);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), ArgListExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (TypeOf typeOfExpression)
			{
				var result = new TypeOfExpression ();
				LocationDescriptor location = LocationStorage.Get (typeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (typeOfExpression.Location), "typeof".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)typeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (SizeOf sizeOfExpression)
			{
				var result = new SizeOfExpression ();
				LocationDescriptor location = LocationStorage.Get (sizeOfExpression);
				result.AddChild (new CSharpTokenNode (Convert (sizeOfExpression.Location), "sizeof".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)sizeOfExpression.QueriedType.Accept (this), TypeOfExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (CheckedExpr checkedExpression)
			{
				var result = new CheckedExpression ();
				LocationDescriptor location = LocationStorage.Get (checkedExpression);
				result.AddChild (new CSharpTokenNode (Convert (checkedExpression.Location), "checked".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)checkedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (UnCheckedExpr uncheckedExpression)
			{
				var result = new UncheckedExpression ();
				LocationDescriptor location = LocationStorage.Get (uncheckedExpression);
				result.AddChild (new CSharpTokenNode (Convert (uncheckedExpression.Location), "unchecked".Length), TypeOfExpression.Roles.Keyword);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LPar);
				result.AddChild ((INode)uncheckedExpression.Expr.Accept (this), TypeOfExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RPar);
				return result;
			}
			
			public override object Visit (ElementAccess elementAccessExpression)
			{
				IndexerExpression result = new IndexerExpression ();
				LocationDescriptor location = LocationStorage.Get (elementAccessExpression);
				 
				result.AddChild ((INode)elementAccessExpression.Expr.Accept (this), IndexerExpression.Roles.TargetExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), TypeOfExpression.Roles.LBracket);
				AddArguments (result, location, elementAccessExpression.Arguments);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), TypeOfExpression.Roles.RBracket);
				return result;
			}
			
			public override object Visit (BaseAccess baseAccessExpression)
			{
				var result = new MemberReferenceExpression ();
				
				LocationDescriptor location = LocationStorage.Get (baseAccessExpression);
				
				BaseReferenceExpression baseReferenceExpression = new BaseReferenceExpression ();
				baseReferenceExpression.Location = Convert (location[0]);
				Console.WriteLine (baseReferenceExpression.Location);
				result.AddChild (baseReferenceExpression, MemberReferenceExpression.Roles.TargetExpression);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), MemberReferenceExpression.Roles.Dot);
				result.AddChild (new Identifier (baseAccessExpression.Identifier, Convert (location[2])), MemberReferenceExpression.Roles.Identifier);
				
				return result;
			}
			
			public override object Visit (StackAlloc stackAllocExpression)
			{
				var result = new StackAllocExpression ();
				
				LocationDescriptor location = LocationStorage.Get (stackAllocExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "stackalloc".Length), StackAllocExpression.StackAllocKeywordRole);
				result.AddChild ((INode)stackAllocExpression.TypeExpression.Accept (this), StackAllocExpression.Roles.ReturnType);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), StackAllocExpression.Roles.LBracket);
				result.AddChild ((INode)stackAllocExpression.CountExpression.Accept (this), StackAllocExpression.Roles.Expression);
				result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), StackAllocExpression.Roles.RBracket);
				return result;
			}
			
			public override object Visit (SimpleAssign simpleAssign)
			{
				var result = new AssignmentExpression ();
				
				LocationDescriptor location = LocationStorage.Get (simpleAssign);
				
				result.AssignmentOperatorType = AssignmentOperatorType.Assign;
				if (simpleAssign.Target != null)
					result.AddChild ((INode)simpleAssign.Target.Accept (this), AssignmentExpression.LeftExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), 1), AssignmentExpression.OperatorRole);
				
				if (simpleAssign.Source != null)
					result.AddChild ((INode)simpleAssign.Source.Accept (this), AssignmentExpression.RightExpressionRole);
				return result;
			}
			
			public override object Visit (CompoundAssign compoundAssign)
			{
				var result = new AssignmentExpression ();
				LocationDescriptor location = LocationStorage.Get (compoundAssign);
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
			
			public override object Visit (Mono.CSharp.AnonymousMethodExpression anonymousMethodExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.AnonymousMethodExpression ();
				LocationDescriptor location = LocationStorage.Get (anonymousMethodExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "delegate".Length), AssignmentExpression.Roles.Keyword);
				
				if (location.Keywords.Length > 1) {
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AssignmentExpression.Roles.LPar);
					AddParameter (result, anonymousMethodExpression.Parameters);
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AssignmentExpression.Roles.RPar);
				}
				result.AddChild ((INode)anonymousMethodExpression.Block.Accept (this), AssignmentExpression.Roles.Body);
				return result;
			}

			public override object Visit (Mono.CSharp.LambdaExpression lambdaExpression)
			{
				var result = new MonoDevelop.CSharp.Dom.LambdaExpression ();
				LocationDescriptor location = LocationStorage.Get (lambdaExpression);
				
				if (location.Keywords == null || location.Keywords.Length == 1) {
					AddParameter (result, lambdaExpression.Parameters);
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "=>".Length), AssignmentExpression.Roles.Assign);
				} else {
					result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), AssignmentExpression.Roles.LPar);
					AddParameter (result, lambdaExpression.Parameters);
					result.AddChild (new CSharpTokenNode (Convert (location[2]), 1), AssignmentExpression.Roles.RPar);
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "=>".Length), AssignmentExpression.Roles.Assign);
				}
				if (lambdaExpression.Block.IsGenerated) {
					ContextualReturn generatedReturn = (ContextualReturn)lambdaExpression.Block.Statements[0];
					result.AddChild ((INode)generatedReturn.Expr.Accept (this), AssignmentExpression.Roles.Expression);
				} else {
					result.AddChild ((INode)lambdaExpression.Block.Accept (this), AssignmentExpression.Roles.Expression);
				}
				
				return result;
			}
			#endregion
			
			#region LINQ expressions
			public override object Visit (Mono.CSharp.Linq.QueryExpression queryExpression)
			{
				var result = new QueryExpressionFromClause ();
				LocationDescriptor location = LocationStorage.Get (queryExpression);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "from".Length), QueryExpressionFromClause.FromKeywordRole);
				// TODO: select identifier
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionFromClause.InKeywordRole);
				result.AddChild ((INode)((Mono.CSharp.Linq.AQueryClause)queryExpression.Expr).Expr.Accept (this), QueryExpressionFromClause.Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.SelectMany selectMany)
			{
				var result = new QueryExpressionFromClause ();
				Mono.CSharp.Linq.Cast cast = selectMany.Expr as Mono.CSharp.Linq.Cast;
				LocationDescriptor location = LocationStorage.Get (selectMany);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "from".Length), QueryExpressionFromClause.FromKeywordRole);
				
				if (cast != null)
					result.AddChild ((INode)cast.TypeExpr.Accept (this), QueryExpressionFromClause.Roles.ReturnType);
				
				result.AddChild (new Identifier (selectMany.SelectIdentifier.Value, Convert (selectMany.SelectIdentifier.Location)), QueryExpressionFromClause.Roles.Identifier);
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionFromClause.InKeywordRole);
				
				result.AddChild ((INode)(cast != null ? cast.Expr : selectMany.Expr).Accept (this), QueryExpressionFromClause.Roles.Expression);
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Select sel)
			{
				var result = new QueryExpressionSelectClause ();
				LocationDescriptor location = LocationStorage.Get (sel);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "select".Length), QueryExpressionWhereClause.Roles.Keyword);
				result.AddChild ((INode)sel.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupBy groupBy)
			{
				var result = new QueryExpressionGroupClause ();
				LocationDescriptor location = LocationStorage.Get (groupBy);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "group".Length), QueryExpressionGroupClause.GroupKeywordRole);
				result.AddChild ((INode)groupBy.ElementSelector.Accept (this), QueryExpressionGroupClause.GroupByExpressionRole);
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "by".Length), QueryExpressionGroupClause.ByKeywordRole);
				result.AddChild ((INode)groupBy.Expr.Accept (this), QueryExpressionGroupClause.ProjectionExpressionRole);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Let l)
			{
				var result = new QueryExpressionLetClause ();
				LocationDescriptor location = LocationStorage.Get (l);
				
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "let".Length), QueryExpressionWhereClause.Roles.Keyword);
				
				NewAnonymousType aType = l.Expr as NewAnonymousType;
				AnonymousTypeParameter param = ((AnonymousTypeParameter)aType.Parameters[1]);
				result.AddChild (new Identifier (param.Name, Convert (param.Location)));
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), 1), QueryExpressionWhereClause.Roles.Assign);
				
				result.AddChild ((INode)param.Expr.Accept (this), QueryExpressionWhereClause.Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Where w)
			{
				var result = new QueryExpressionWhereClause ();
				LocationDescriptor location = LocationStorage.Get (w);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "where".Length), QueryExpressionWhereClause.Roles.Keyword);
				result.AddChild ((INode)w.Expr.Accept (this), QueryExpressionWhereClause.Roles.Condition);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.Join join)
			{
				var result = new QueryExpressionJoinClause ();
				LocationDescriptor location = LocationStorage.Get (join);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "join".Length), QueryExpressionJoinClause.JoinKeywordRole);
				
				result.AddChild (new Identifier (join.JoinIdentifier.Value, Convert (join.JoinIdentifier.Location)));
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionJoinClause.InKeywordRole);
				
				result.AddChild ((INode)join.Expr.Accept (this), QueryExpressionJoinClause.Roles.Expression);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "on".Length), QueryExpressionJoinClause.OnKeywordRole);
				// TODO: on expression
				
				result.AddChild (new CSharpTokenNode (Convert (location[3]), "equals".Length), QueryExpressionJoinClause.EqualsKeywordRole);
				// TODO: equals expression
				
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.GroupJoin groupJoin)
			{
				var result = new QueryExpressionJoinClause ();
				LocationDescriptor location = LocationStorage.Get (groupJoin);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "join".Length), QueryExpressionJoinClause.JoinKeywordRole);
				
				result.AddChild (new Identifier (groupJoin.JoinIdentifier.Value, Convert (groupJoin.JoinIdentifier.Location)));
				
				result.AddChild (new CSharpTokenNode (Convert (location[1]), "in".Length), QueryExpressionJoinClause.InKeywordRole);
				
				result.AddChild ((INode)groupJoin.Expr.Accept (this), QueryExpressionJoinClause.Roles.Expression);
				
				result.AddChild (new CSharpTokenNode (Convert (location[2]), "on".Length), QueryExpressionJoinClause.OnKeywordRole);
				// TODO: on expression
				
				result.AddChild (new CSharpTokenNode (Convert (location[3]), "equals".Length), QueryExpressionJoinClause.EqualsKeywordRole);
				// TODO: equals expression
				
				result.AddChild (new CSharpTokenNode (Convert (location[4]), "into".Length), QueryExpressionJoinClause.IntoKeywordRole);
				
				result.AddChild (new Identifier (groupJoin.IntoVariable.Value, Convert (groupJoin.IntoVariable.Location)));
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByAscending orderByAscending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = true;
				result.AddChild ((INode)orderByAscending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				LocationDescriptor location = LocationStorage.Get (orderByAscending);
				if (location.Keywords != null) 
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "ascending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.OrderByDescending orderByDescending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = false;
				result.AddChild ((INode)orderByDescending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				LocationDescriptor location = LocationStorage.Get (orderByDescending);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "descending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByAscending thenByAscending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = true;
				result.AddChild ((INode)thenByAscending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				LocationDescriptor location = LocationStorage.Get (thenByAscending);
				if (location.Keywords != null) 
					result.AddChild (new CSharpTokenNode (Convert (location[0]), "ascending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			
			public override object Visit (Mono.CSharp.Linq.ThenByDescending thenByDescending)
			{
				var result = new QueryExpressionOrderClause ();
				result.OrderAscending = false;
				result.AddChild ((INode)thenByDescending.Expr.Accept (this), QueryExpressionWhereClause.Roles.Expression);
				LocationDescriptor location = LocationStorage.Get (thenByDescending);
				result.AddChild (new CSharpTokenNode (Convert (location[0]), "descending".Length), QueryExpressionWhereClause.Roles.Keyword);
				return result;
			}
			#endregion
		}

		public MonoDevelop.CSharp.Dom.CompilationUnit Parse (TextEditorData data)
		{
			Tokenizer.InterpretTabAsSingleChar = true;
			CompilerCompilationUnit top;
			using (Stream stream = data.OpenStream ()) {
				top = CompilerCallableEntryPoint.ParseFile (new string[] { "-v", "-unsafe"}, stream, data.Document.FileName, Console.Out);
			}

			if (top == null)
				return null;
			CSharpParser.ConversionVisitor conversionVisitor = new ConversionVisitor (top.LocationStorage);
			top.ModuleCompiled.Accept (conversionVisitor);
			return conversionVisitor.Unit;
		}
	}
}*/
