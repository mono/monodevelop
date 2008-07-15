// DomParser.cs created with MonoDevelop
// User: mkrueger at 13:18 27.05.2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.CSharp;

namespace MonoDevelop.VBNetBinding
{
	public class DomParser : IParser
	{
		public bool CanParseMimeType (string mimeType)
		{
			return "text/x-vb" == mimeType;
		}
		
		public bool CanParseProjectType (string projectType)
		{
			return "VB.NET" == projectType;
		}
		
		public bool CanParse (string fileName)
		{
			return Path.GetExtension (fileName) == ".vb";
		}
		
		public IExpressionFinder CreateExpressionFinder ()
		{
			return null;
		}
		
		public IDocumentMetaInformation CreateMetaInformation (Stream stream)
		{
			return null;
		}
				

		public ICompilationUnit Parse (string fileName, string content)
		{
			using (ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.VBNet, new StringReader(content))) {
				return Parse (parser, fileName);
			}
		}
		
		ICompilationUnit Parse (ICSharpCode.NRefactory.IParser parser, string fileName)
		{
			parser.Parse();
			
			DomConverter visitor = new DomConverter (fileName);
			ICompilationUnit result = (ICompilationUnit)visitor.VisitCompilationUnit(parser.CompilationUnit, null);
/*			visitor.Cu.ErrorsDuringCompile = p.Errors.Count > 0;
			visitor.Cu.Tag = p.CompilationUnit;
			RetrieveRegions(visitor.Cu, p.Lexer.SpecialTracker);
			foreach (IClass c in visitor.Cu.Classes)
				c.Region.FileName = fileName;
			AddCommentTags(visitor.Cu, p.Lexer.TagComments);*/
			return result;
		}
		
		class DomConverter : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			MonoDevelop.Projects.Dom.CompilationUnit cu;
				
			Stack<string>  currentNamespace = new Stack<string> ();
			Stack<DomType> currentType      = new Stack<DomType> ();
			
//			static ICSharpCode.NRefactory.Visitors.CodeDomVisitor domVisitor = new ICSharpCode.NRefactory.Visitors.CodeDomVisitor ();
			
			public DomConverter (string fileName)
			{
				cu = new CompilationUnit (fileName);
			}
			
			public override object VisitCompilationUnit(ICSharpCode.NRefactory.Ast.CompilationUnit compilationUnit, object data)
			{
				//TODO: Imports, Comments
				compilationUnit.AcceptChildren (this, data);
				return cu;
			}
			
//			public override object VisitUsing(ICSharpCode.NRefactory.Ast.Using usingDeclaration, object data)
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
			
			public override object VisitNamespaceDeclaration(ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
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
			
			ClassType TranslateClassType(ICSharpCode.NRefactory.Ast.ClassType type)
			{
				switch (type) {
					case ICSharpCode.NRefactory.Ast.ClassType.Class:
						return ClassType.Class;
					case ICSharpCode.NRefactory.Ast.ClassType.Enum:
						return ClassType.Enum;
					case ICSharpCode.NRefactory.Ast.ClassType.Interface:
						return ClassType.Interface;
					case ICSharpCode.NRefactory.Ast.ClassType.Struct:
						return ClassType.Struct;
				}
				return ClassType.Unknown;
			}
			
			public override object VisitTypeDeclaration(ICSharpCode.NRefactory.Ast.TypeDeclaration typeDeclaration, object data)
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
					foreach (ICSharpCode.NRefactory.Ast.TypeReference baseType in typeDeclaration.BaseTypes) {
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
			
			static DomReturnType TranslateTypeReference (ICSharpCode.NRefactory.Ast.TypeReference type)
			{
				return new DomReturnType (type.Type);
			}
			static DomRegion TranslateRegion (ICSharpCode.NRefactory.Location start, ICSharpCode.NRefactory.Location end)
			{
				return new DomRegion (start.Y, start.X, end.Y, end.X);
			}
			
			public override object VisitMethodDeclaration(ICSharpCode.NRefactory.Ast.MethodDeclaration methodDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				type.Add (new DomMethod (methodDeclaration.Name,
				                         (Modifiers)methodDeclaration.Modifier,
				                         false, // isConstructor
				                         new DomLocation (methodDeclaration.StartLocation.Line, methodDeclaration.StartLocation.Column),
				                         methodDeclaration.Body != null ? TranslateRegion (methodDeclaration.Body.StartLocation, methodDeclaration.Body.EndLocation) : DomRegion.Empty));
				return null;
			}
			
			public override object VisitConstructorDeclaration(ICSharpCode.NRefactory.Ast.ConstructorDeclaration constructorDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				type.Add (new DomMethod (constructorDeclaration.Name,
				                         (Modifiers)constructorDeclaration.Modifier,
				                         true, // isConstructor
				                         new DomLocation (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column),
				                         constructorDeclaration.Body != null ? TranslateRegion (constructorDeclaration.Body.StartLocation, constructorDeclaration.Body.EndLocation) : DomRegion.Empty));
				return null;
			}
			
			public override object VisitFieldDeclaration(ICSharpCode.NRefactory.Ast.FieldDeclaration fieldDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				foreach (ICSharpCode.NRefactory.Ast.VariableDeclaration field in fieldDeclaration.Fields) {
					type.Add (new DomField (field.Name,
					                        (Modifiers)fieldDeclaration.Modifier,
					                        new DomLocation (fieldDeclaration.StartLocation.Line, fieldDeclaration.StartLocation.Column),
					                        TranslateTypeReference (fieldDeclaration.TypeReference)));
				}
				return null;
			}
			
			public override object VisitPropertyDeclaration(ICSharpCode.NRefactory.Ast.PropertyDeclaration propertyDeclaration, object data)
			{
				Debug.Assert (currentType.Count > 0);
				DomType type = currentType.Peek ();
				
				type.Add (new DomProperty (propertyDeclaration.Name,
				                           (Modifiers)propertyDeclaration.Modifier,
				                           new DomLocation (propertyDeclaration.StartLocation.Line, propertyDeclaration.StartLocation.Column),
				                           TranslateRegion (propertyDeclaration.BodyStart, propertyDeclaration.BodyEnd),
				                           TranslateTypeReference (propertyDeclaration.TypeReference)));
				return null;
			}
			
			public override object VisitEventDeclaration(ICSharpCode.NRefactory.Ast.EventDeclaration eventDeclaration, object data)
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
