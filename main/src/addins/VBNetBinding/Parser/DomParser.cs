// 
// DomParser.cs
//  
// Author:
//       Mike Krueger <mkrueger@novell.com>
// 
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.VBNetBinding
{
	public class DomParser : AbstractParser
	{
		public override ParsedDocument Parse (ProjectDom dom, string fileName, string content)
		{
			using (ICSharpCode.OldNRefactory.IParser parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (ICSharpCode.OldNRefactory.SupportedLanguage.VBNet, new StringReader(content))) {
				return Parse (parser, fileName);
			}
		}
		
		ParsedDocument Parse (ICSharpCode.OldNRefactory.IParser parser, string fileName)
		{
			parser.Parse();
			
			DomConverter visitor = new DomConverter (fileName);
			ParsedDocument result = new ParsedDocument (fileName);
			result.CompilationUnit = (ICompilationUnit)visitor.VisitCompilationUnit(parser.CompilationUnit, null);
/*			visitor.Cu.ErrorsDuringCompile = p.Errors.Count > 0;
			visitor.Cu.Tag = p.CompilationUnit;
			RetrieveRegions(visitor.Cu, p.Lexer.SpecialTracker);
			foreach (IType c in visitor.Cu.Classes)
				c.Region.FileName = fileName;
			AddCommentTags(visitor.Cu, p.Lexer.TagComments);*/
			return result;
		}
		
		class DomConverter : ICSharpCode.OldNRefactory.Visitors.AbstractAstVisitor
		{
			MonoDevelop.Projects.Dom.CompilationUnit cu;
				
			Stack<string>  currentNamespace = new Stack<string> ();
			Stack<DomType> currentType      = new Stack<DomType> ();
			
//			static ICSharpCode.OldNRefactory.Visitors.CodeDomVisitor domVisitor = new ICSharpCode.OldNRefactory.Visitors.CodeDomVisitor ();
			
			public DomConverter (string fileName)
			{
				cu = new CompilationUnit (fileName);
			}
			
			public override object VisitCompilationUnit(ICSharpCode.OldNRefactory.Ast.CompilationUnit compilationUnit, object data)
			{
				//TODO: Imports, Comments
				compilationUnit.AcceptChildren (this, data);
				return cu;
			}
			
//			public override object VisitUsing(ICSharpCode.OldNRefactory.Ast.Using usingDeclaration, object data)
//			{
//				DefaultUsing u = new DefaultUsing();
//				if (usingDeclaration.IsAlias)
//					u.Aliases[usingDeclaration.Name] = new ReturnType (usingDeclaration.Alias.Type);
//				else
//					u.Usings.Add(usingDeclaration.Name);
//				cu.Usings.Add(u);
//				return data;
//			}
			
//		ModifierEnum VisitModifier(ICSharpCode.SharpRefactory.Parser.Modifier m)
//		{
//			return (ModifierEnum)m;
//		}
			
			public override object VisitNamespaceDeclaration(ICSharpCode.OldNRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
			{
				string name;
				if (currentNamespace.Count == 0) {
					name = namespaceDeclaration.Name;
				} else {
					name = String.Concat((string)currentNamespace.Peek(), '.', namespaceDeclaration.Name);
				}
				currentNamespace.Push(name);
				object ret = namespaceDeclaration.AcceptChildren(this, data);
				currentNamespace.Pop();
				return ret;
			}
			
			ClassType TranslateClassType(ICSharpCode.OldNRefactory.Ast.ClassType type)
			{
				switch (type) {
					case ICSharpCode.OldNRefactory.Ast.ClassType.Class:
						return ClassType.Class;
					case ICSharpCode.OldNRefactory.Ast.ClassType.Enum:
						return ClassType.Enum;
					case ICSharpCode.OldNRefactory.Ast.ClassType.Interface:
						return ClassType.Interface;
					case ICSharpCode.OldNRefactory.Ast.ClassType.Struct:
						return ClassType.Struct;
				}
				return ClassType.Unknown;
			}
			
			public override object VisitTypeDeclaration(ICSharpCode.OldNRefactory.Ast.TypeDeclaration typeDeclaration, object data)
			{
				DomType type = new DomType (cu, 
				                            TranslateClassType (typeDeclaration.Type),
				                            (Modifiers)typeDeclaration.Modifier, 
				                            typeDeclaration.Name,
				                            new DomLocation (typeDeclaration.StartLocation.Line, typeDeclaration.StartLocation.Column), 
				                            currentNamespace.Count > 0 ? currentNamespace.Peek() : "",
				                            TranslateRegion (typeDeclaration.StartLocation, typeDeclaration.EndLocation));
				
				if (currentType.Count > 0) {
					currentType.Peek().Add (type);
				} else {
					cu.Add (type);
				}
				
				if (typeDeclaration.BaseTypes != null) {
					foreach (ICSharpCode.OldNRefactory.Ast.TypeReference baseType in typeDeclaration.BaseTypes) {
						if (type.BaseType == null) {
							type.BaseType = TranslateTypeReference (baseType);
						} else {
							type.AddInterfaceImplementation (TranslateTypeReference (baseType));
						}
					}
				}
				currentType.Push (type);
				typeDeclaration.AcceptChildren (this, data);
				currentType.Pop();
				
				return null;
			}
			
			static DomReturnType TranslateTypeReference (ICSharpCode.OldNRefactory.Ast.TypeReference type)
			{
				return new DomReturnType (type.Type);
			}
			static DomRegion TranslateRegion (ICSharpCode.OldNRefactory.Location start, ICSharpCode.OldNRefactory.Location end)
			{
				return new DomRegion (start.Y, start.X, end.Y, end.X);
			}
			
			public override object VisitMethodDeclaration(ICSharpCode.OldNRefactory.Ast.MethodDeclaration methodDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				type.Add (new DomMethod (methodDeclaration.Name,
				                         (Modifiers)methodDeclaration.Modifier,
				                         MethodModifier.None,
				                         new DomLocation (methodDeclaration.StartLocation.Line, methodDeclaration.StartLocation.Column),
				                         methodDeclaration.Body != null ? TranslateRegion (methodDeclaration.Body.StartLocation, methodDeclaration.Body.EndLocation) : DomRegion.Empty));
				return null;
			}
			
			public override object VisitConstructorDeclaration(ICSharpCode.OldNRefactory.Ast.ConstructorDeclaration constructorDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				type.Add (new DomMethod (constructorDeclaration.Name,
				                         (Modifiers)constructorDeclaration.Modifier,
				                         MethodModifier.IsConstructor,
				                         new DomLocation (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column),
				                         constructorDeclaration.Body != null ? TranslateRegion (constructorDeclaration.Body.StartLocation, constructorDeclaration.Body.EndLocation) : DomRegion.Empty));
				return null;
			}
			
			public override object VisitFieldDeclaration(ICSharpCode.OldNRefactory.Ast.FieldDeclaration fieldDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				foreach (ICSharpCode.OldNRefactory.Ast.VariableDeclaration field in fieldDeclaration.Fields) {
					type.Add (new DomField (field.Name,
					                        (Modifiers)fieldDeclaration.Modifier,
					                        new DomLocation (fieldDeclaration.StartLocation.Line, fieldDeclaration.StartLocation.Column),
					                        TranslateTypeReference (fieldDeclaration.TypeReference)));
				}
				return null;
			}
			
			public override object VisitPropertyDeclaration(ICSharpCode.OldNRefactory.Ast.PropertyDeclaration propertyDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				var getterModifier = (Modifiers)propertyDeclaration.Modifier;
				var setterModifier = (Modifiers)propertyDeclaration.Modifier;
				if (propertyDeclaration.HasGetRegion && propertyDeclaration.GetRegion.Modifier != ICSharpCode.OldNRefactory.Ast.Modifiers.None)
					getterModifier = (Modifiers) propertyDeclaration.GetRegion.Modifier;
				if (propertyDeclaration.HasSetRegion && propertyDeclaration.SetRegion.Modifier != ICSharpCode.OldNRefactory.Ast.Modifiers.None)
					setterModifier = (Modifiers) propertyDeclaration.SetRegion.Modifier;
				
				type.Add (new DomProperty (propertyDeclaration.Name,
				                           getterModifier, setterModifier,
				                           new DomLocation (propertyDeclaration.StartLocation.Line, propertyDeclaration.StartLocation.Column),
				                           TranslateRegion (propertyDeclaration.BodyStart, propertyDeclaration.BodyEnd),
				                           TranslateTypeReference (propertyDeclaration.TypeReference)));
				return null;
			}
			
			public override object VisitEventDeclaration(ICSharpCode.OldNRefactory.Ast.EventDeclaration eventDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				type.Add (new DomEvent (eventDeclaration.Name,
				                        (Modifiers)eventDeclaration.Modifier,
				                        new DomLocation (eventDeclaration.StartLocation.Line, eventDeclaration.StartLocation.Column),
				                        TranslateTypeReference (eventDeclaration.TypeReference)));
				return null;
			}
			
		}
	}
}
