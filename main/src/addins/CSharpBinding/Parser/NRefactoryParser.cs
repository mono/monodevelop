//
// NRefactoryParser.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.CSharpBinding
{
	public class NRefactoryParser : AbstractParser
	{
		public override bool CanParse (string fileName)
		{
			return Path.GetExtension (fileName) == ".cs";
		}
		
		public NRefactoryParser () : base ("C#", "text/x-csharp")
		{
		}
		
		public override IExpressionFinder CreateExpressionFinder (ProjectDom dom)
		{
			return new MonoDevelop.CSharpBinding.Gui.NewCSharpExpressionFinder (dom);
		}
		
		public override IResolver CreateResolver (ProjectDom dom, object editor, string fileName)
		{
			MonoDevelop.Ide.Gui.Document doc = (MonoDevelop.Ide.Gui.Document)editor;
			return new NRefactoryResolver (dom, doc.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, doc.TextEditor, fileName);
		}
		
		public override IDocumentMetaInformation CreateMetaInformation (TextReader reader)
		{
			return new NRefactoryDocumentMetaInformation (ICSharpCode.NRefactory.SupportedLanguage.CSharp, reader);
		}
		
		public override ICompilationUnit Parse (string fileName, string content)
		{
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StringReader (content))) {
				MonoDevelop.Projects.Dom.CompilationUnit result = new MonoDevelop.Projects.Dom.CompilationUnit (fileName);
				parser.Errors.Error += delegate (int line, int col, string message) {
					result.Add (new Error (ErrorType.Error, col, line, message));
				};
				parser.Parse ();
				ConversionVisitior visitor = new ConversionVisitior (result);
				visitor.VisitCompilationUnit (parser.CompilationUnit, null);
				
				return result;
			}
		}

		class ConversionVisitior : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			MonoDevelop.Projects.Dom.CompilationUnit result;
			
			public ConversionVisitior (MonoDevelop.Projects.Dom.CompilationUnit result)
			{
				this.result = result;
			}
			
			static DomRegion ConvertRegion (ICSharpCode.NRefactory.Location start, ICSharpCode.NRefactory.Location end)
			{
				return new DomRegion (start.Line, start.Column, end.Line, end.Column);
			}
			
			static DomLocation ConvertLocation (ICSharpCode.NRefactory.Location location)
			{
				return new DomLocation (location.Line, location.Column);
			}

			static Modifiers ConvertModifiers (ICSharpCode.NRefactory.Ast.Modifiers modifiers)
			{
				return (Modifiers)modifiers;
			}
			
			static ClassType ConvertClassType (ICSharpCode.NRefactory.Ast.ClassType nrClassType)
			{
				switch (nrClassType) {
					case ICSharpCode.NRefactory.Ast.ClassType.Class:
						return ClassType.Class;
					case ICSharpCode.NRefactory.Ast.ClassType.Struct:
						return ClassType.Struct;
					case ICSharpCode.NRefactory.Ast.ClassType.Interface:
						return ClassType.Interface;
					case ICSharpCode.NRefactory.Ast.ClassType.Enum:
						return ClassType.Enum;
				}
				return ClassType.Class;
			}

			static DomReturnType ConvertReturnType (ICSharpCode.NRefactory.Ast.TypeReference typeReference)
			{
				DomReturnType result = new DomReturnType (typeReference.SystemType != null ? typeReference.SystemType : typeReference.Type);
				result.PointerNestingLevel = typeReference.PointerNestingLevel;
				result.ArrayDimensions = typeReference.RankSpecifier != null ? typeReference.RankSpecifier.Length : 0;
				for (int i = 0; i < result.ArrayDimensions; i++) {
					result.SetDimension (i, typeReference.RankSpecifier[i]);
				}
				
				if (typeReference.GenericTypes != null && typeReference.GenericTypes.Count > 0) {
					foreach (ICSharpCode.NRefactory.Ast.TypeReference genericArgument in typeReference.GenericTypes) {
						result.AddTypeParameter (ConvertReturnType (genericArgument));
					}
				}
				
				return result;
			}
			
			static void AddAttributes (AbstractMember member, IEnumerable<ICSharpCode.NRefactory.Ast.AttributeSection> attributes)
			{
				foreach (ICSharpCode.NRefactory.Ast.AttributeSection attributeSection in attributes) {
					foreach (ICSharpCode.NRefactory.Ast.Attribute attribute in attributeSection.Attributes) {
						DomAttribute domAttribute = new DomAttribute ();
						domAttribute.Name   = attribute.Name;
						domAttribute.Region = ConvertRegion (attribute.StartLocation, attribute.EndLocation);
						member.Add (domAttribute);
					}
				}
			}
			
			static void AddExplicitInterfaces (AbstractMember member, IEnumerable<ICSharpCode.NRefactory.Ast.InterfaceImplementation> interfaceImplementations)
			{
				if (interfaceImplementations == null)
					return;
				
				foreach (ICSharpCode.NRefactory.Ast.InterfaceImplementation impl in interfaceImplementations) {
					member.AddExplicitInterface (ConvertReturnType (impl.InterfaceType));
				}
			}
			
			public override object VisitUsingDeclaration (ICSharpCode.NRefactory.Ast.UsingDeclaration usingDeclaration, object data)
			{
				DomUsing domUsing = new DomUsing ();
				domUsing.Region   = ConvertRegion (usingDeclaration.StartLocation, usingDeclaration.EndLocation);
				foreach (ICSharpCode.NRefactory.Ast.Using u in usingDeclaration.Usings) {
					if (u.IsAlias) {
						domUsing.Add (u.Name, ConvertReturnType (u.Alias));
					} else {
						domUsing.Add (u.Name);
					}
				}
				result.Add (domUsing);
				return data;
			}

			Stack<string> namespaceStack = new Stack<string> ();
			public override object VisitNamespaceDeclaration (ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
			{
				namespaceStack.Push (namespaceStack.Count == 0 ? namespaceDeclaration.Name : namespaceStack.Peek() + "." + namespaceDeclaration.Name);
				namespaceDeclaration.AcceptChildren (this, data);
				namespaceStack.Pop ();
				return null;
			}

			Stack<DomType> typeStack = new Stack<DomType> ();
			public override object VisitTypeDeclaration (ICSharpCode.NRefactory.Ast.TypeDeclaration typeDeclaration, object data)
			{
				DomType newType = new DomType ();
				newType.Name      = typeDeclaration.Name;
				newType.Location  = ConvertLocation (typeDeclaration.StartLocation);
				newType.ClassType = ConvertClassType (typeDeclaration.Type);
				newType.BodyRegion = ConvertRegion (typeDeclaration.BodyStartLocation, typeDeclaration.EndLocation);
				newType.Modifiers  = ConvertModifiers (typeDeclaration.Modifier);
				
				AddAttributes (newType, typeDeclaration.Attributes);
				
				if (typeDeclaration.BaseTypes != null) {
					foreach (ICSharpCode.NRefactory.Ast.TypeReference type in typeDeclaration.BaseTypes) {
						if (newType.BaseType == null) {
							newType.BaseType = ConvertReturnType (type);
						} else {
							newType.AddInterfaceImplementation (ConvertReturnType (type));
						}
					}
				}
				AddType (newType);
				
				// visit members
				typeStack.Push (newType);
				typeDeclaration.AcceptChildren (this, data);
				typeStack.Pop ();
				
				return null;
			}

			void AddType (DomType type)
			{
				// add type to compilation unit or outer type
				if (typeStack.Count > 0) {
					DomType outerType = typeStack.Peek ();
					type.DeclaringType = outerType;
					outerType.Add (type);
				} else {
					if (namespaceStack.Count > 0) 
						type.Namespace = namespaceStack.Peek ();
					result.Add (type);
				}
			}

			static ParameterModifiers ConvertParameterModifiers (ICSharpCode.NRefactory.Ast.ParameterModifiers modifier)
			{
				if ((modifier & ICSharpCode.NRefactory.Ast.ParameterModifiers.Out) != 0)
					return ParameterModifiers.Out;
				if ((modifier & ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref) != 0)
					return ParameterModifiers.Ref;
				if ((modifier & ICSharpCode.NRefactory.Ast.ParameterModifiers.Params) != 0)
					return ParameterModifiers.Params;
				return ParameterModifiers.None;
			}
			
			static DomParameter ConvertParameter (IMember declaringMember, ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression pde)
			{
				DomParameter result = new DomParameter ();
				result.Name               = pde.ParameterName;
				result.DeclaringMember    = declaringMember;
				result.ReturnType         = ConvertReturnType (pde.TypeReference);
				result.ParameterModifiers = ConvertParameterModifiers (pde.ParamModifier);
				return result;
			}
			
			static List<IParameter> ConvertParameterList (IMember declaringMember, IEnumerable<ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression> parameters)
			{
				List<IParameter> result = new List<IParameter> ();
				if (parameters != null) {
					foreach (ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression pde in parameters) {
						result.Add (ConvertParameter (declaringMember, pde));
					}
				}
				return result;
			}
			
			public override object VisitDelegateDeclaration (ICSharpCode.NRefactory.Ast.DelegateDeclaration delegateDeclaration, object data)
			{
				List<IParameter> parameter = ConvertParameterList (null, delegateDeclaration.Parameters);
				DomType delegateType = DomType.CreateDelegate (result, 
				                                               delegateDeclaration.Name,
				                                               ConvertLocation (delegateDeclaration.StartLocation),
				                                               ConvertReturnType (delegateDeclaration.ReturnType),
				                                               parameter);
				delegateType.Modifiers  = ConvertModifiers (delegateDeclaration.Modifier);
				AddAttributes (delegateType, delegateDeclaration.Attributes);
				
				foreach (DomParameter p in parameter) {
					p.DeclaringMember = delegateType;
				}
				
				AddType (delegateType);
				
				return null;
			}
			
			public override object VisitConstructorDeclaration (ICSharpCode.NRefactory.Ast.ConstructorDeclaration constructorDeclaration, object data)
			{
				DomMethod constructor = new DomMethod ();
				constructor.Name      = ".ctor";
				constructor.IsConstructor = true;
				constructor.Location  = ConvertLocation (constructorDeclaration.StartLocation);
				constructor.BodyRegion = ConvertRegion (constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				constructor.Modifiers  = ConvertModifiers (constructorDeclaration.Modifier);
				AddAttributes (constructor, constructorDeclaration.Attributes);
				constructor.Add (ConvertParameterList (constructor, constructorDeclaration.Parameters));
				
				constructor.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (constructor);
				return null;
			}
		
			public override object VisitMethodDeclaration (ICSharpCode.NRefactory.Ast.MethodDeclaration methodDeclaration, object data)
			{
				DomMethod method = new DomMethod ();
				method.Name      = methodDeclaration.Name;
				method.Location  = ConvertLocation (methodDeclaration.StartLocation);
				method.BodyRegion = ConvertRegion (methodDeclaration.EndLocation, methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				method.Modifiers  = ConvertModifiers (methodDeclaration.Modifier);
				method.ReturnType = ConvertReturnType (methodDeclaration.TypeReference);
				AddAttributes (method, methodDeclaration.Attributes);
				method.Add (ConvertParameterList (method, methodDeclaration.Parameters));
				AddExplicitInterfaces (method, methodDeclaration.InterfaceImplementations);
				
				if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
					foreach (ICSharpCode.NRefactory.Ast.TemplateDefinition td in methodDeclaration.Templates) {
						method.AddGenericParameter (new DomReturnType (td.Name));
					}
				}
				method.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (method);
				
				return null;
			}

			public override object VisitDestructorDeclaration (ICSharpCode.NRefactory.Ast.DestructorDeclaration destructorDeclaration, object data)
			{
				DomMethod destructor = new DomMethod ();
				destructor.Name      = ".dtor";
				destructor.Location  = ConvertLocation (destructorDeclaration.StartLocation);
				destructor.BodyRegion = ConvertRegion (destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : new ICSharpCode.NRefactory.Location (-1, -1));
				destructor.Modifiers  = ConvertModifiers (destructorDeclaration.Modifier);
				AddAttributes (destructor, destructorDeclaration.Attributes);
				
				destructor.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (destructor);
				
				return null;
			}
			
			public override object VisitOperatorDeclaration (ICSharpCode.NRefactory.Ast.OperatorDeclaration operatorDeclaration, object data)
			{
				// TODO
				return null;
			}

			public override object VisitFieldDeclaration (ICSharpCode.NRefactory.Ast.FieldDeclaration fieldDeclaration, object data)
			{
				foreach (ICSharpCode.NRefactory.Ast.VariableDeclaration varDecl in fieldDeclaration.Fields) {
					DomField field = new DomField ();
					field.Name      = varDecl.Name;
					field.Location  = ConvertLocation (fieldDeclaration.StartLocation);
					field.Modifiers  = ConvertModifiers (fieldDeclaration.Modifier);
					field.ReturnType = ConvertReturnType (fieldDeclaration.TypeReference);
					AddAttributes (field, fieldDeclaration.Attributes);
					field.DeclaringType = typeStack.Peek ();
					typeStack.Peek ().Add (field);
				}
				return null;
			}
			
			public override object VisitPropertyDeclaration (ICSharpCode.NRefactory.Ast.PropertyDeclaration propertyDeclaration, object data)
			{
				DomProperty property = new DomProperty ();
				property.Name      = propertyDeclaration.Name;
				property.Location  = ConvertLocation (propertyDeclaration.StartLocation);
				property.BodyRegion = ConvertRegion (propertyDeclaration.BodyStart, propertyDeclaration.BodyEnd);
				property.Modifiers  = ConvertModifiers (propertyDeclaration.Modifier);
				property.ReturnType = ConvertReturnType (propertyDeclaration.TypeReference);
				AddAttributes (property, propertyDeclaration.Attributes);
				AddExplicitInterfaces (property, propertyDeclaration.InterfaceImplementations);
				if (propertyDeclaration.HasGetRegion) {
					property.GetMethod = new DomMethod ();
					property.GetMethod.BodyRegion = ConvertRegion (propertyDeclaration.GetRegion.StartLocation, propertyDeclaration.GetRegion.EndLocation);
				}
				if (propertyDeclaration.HasSetRegion) {
					property.SetMethod = new DomMethod ();
					property.SetMethod.BodyRegion = ConvertRegion (propertyDeclaration.SetRegion.StartLocation, propertyDeclaration.SetRegion.EndLocation);
				}
				property.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (property);
				return null;
			}
			
			public override object VisitIndexerDeclaration (ICSharpCode.NRefactory.Ast.IndexerDeclaration indexerDeclaration, object data)
			{
				DomProperty indexer = new DomProperty ();
				indexer.Name      = "this";
				indexer.IsIndexer = true;
				indexer.Location  = ConvertLocation (indexerDeclaration.StartLocation);
				indexer.BodyRegion = ConvertRegion (indexerDeclaration.BodyStart, indexerDeclaration.BodyEnd);
				indexer.Modifiers  = ConvertModifiers (indexerDeclaration.Modifier);
				indexer.ReturnType = ConvertReturnType (indexerDeclaration.TypeReference);
				indexer.Add (ConvertParameterList (indexer, indexerDeclaration.Parameters));
				
				AddAttributes (indexer, indexerDeclaration.Attributes);
				AddExplicitInterfaces (indexer, indexerDeclaration.InterfaceImplementations);
				
				if (indexerDeclaration.HasGetRegion) {
					indexer.GetMethod = new DomMethod ();
					indexer.GetMethod.BodyRegion = ConvertRegion (indexerDeclaration.GetRegion.StartLocation, indexerDeclaration.GetRegion.EndLocation);
				}
				if (indexerDeclaration.HasSetRegion) {
					indexer.SetMethod = new DomMethod ();
					indexer.SetMethod.BodyRegion = ConvertRegion (indexerDeclaration.SetRegion.StartLocation, indexerDeclaration.SetRegion.EndLocation);
				}
				indexer.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (indexer);
				return null;
			}
			
			public override object VisitEventDeclaration (ICSharpCode.NRefactory.Ast.EventDeclaration eventDeclaration, object data)
			{
				DomEvent evt = new DomEvent ();
				evt.Name      = eventDeclaration.Name;
				evt.Location  = ConvertLocation (eventDeclaration.StartLocation);
				evt.Modifiers  = ConvertModifiers (eventDeclaration.Modifier);
				evt.ReturnType = ConvertReturnType (eventDeclaration.TypeReference);
				AddAttributes (evt, eventDeclaration.Attributes);
				AddExplicitInterfaces (evt, eventDeclaration.InterfaceImplementations);
				evt.DeclaringType = typeStack.Peek ();
				typeStack.Peek ().Add (evt);
				return null;
			}
		}
	}
}
