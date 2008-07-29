// created on 04.08.2003 at 17:49
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.CodeDom;

using RefParser = ICSharpCode.NRefactory.Parser;
using MonoDevelop.Projects.Parser;
using VBBinding.Parser.SharpDevelopTree;
using ModifierFlags = ICSharpCode.NRefactory.Ast.Modifiers;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;

namespace VBBinding.Parser
{
	public class VBNetVisitor : AbstractAstVisitor
	{
		VBBinding.Parser.SharpDevelopTree.CompilationUnit cu = new VBBinding.Parser.SharpDevelopTree.CompilationUnit();
		Stack<string> currentNamespace = new Stack<string> ();
		Stack<Class> currentClass = new Stack<Class> ();
		static ICSharpCode.NRefactory.Visitors.CodeDomVisitor domVisitor = new ICSharpCode.NRefactory.Visitors.CodeDomVisitor ();
		
		public VBBinding.Parser.SharpDevelopTree.CompilationUnit Cu {
			get {
				return cu;
			}
		}
		
		public override object VisitCompilationUnit(ICSharpCode.NRefactory.Ast.CompilationUnit compilationUnit, object data)
		{
			//TODO: Imports, Comments
			compilationUnit.AcceptChildren(this, data);
			return cu;
		}
		
		public override object VisitUsing(ICSharpCode.NRefactory.Ast.Using usingDeclaration, object data)
		{
			DefaultUsing u = new DefaultUsing();
			if (usingDeclaration.IsAlias)
				u.Aliases[usingDeclaration.Name] = new ReturnType (usingDeclaration.Alias.Type);
			else
				u.Usings.Add(usingDeclaration.Name);
			cu.Usings.Add(u);
			return data;
		}
		
//		ModifierEnum VisitModifier(ICSharpCode.SharpRefactory.Parser.Modifier m)
//		{
//			return (ModifierEnum)m;
//		}
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
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
		
		MonoDevelop.Projects.Parser.ClassType TranslateClassType(ICSharpCode.NRefactory.Ast.ClassType type)
		{
			switch (type) {
				case ICSharpCode.NRefactory.Ast.ClassType.Class:
					return MonoDevelop.Projects.Parser.ClassType.Class;
				case ICSharpCode.NRefactory.Ast.ClassType.Enum:
					return MonoDevelop.Projects.Parser.ClassType.Enum;
				case ICSharpCode.NRefactory.Ast.ClassType.Interface:
					return MonoDevelop.Projects.Parser.ClassType.Interface;
				case ICSharpCode.NRefactory.Ast.ClassType.Struct:
					return MonoDevelop.Projects.Parser.ClassType.Struct;
			}
			return MonoDevelop.Projects.Parser.ClassType.Class;
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			DefaultRegion region = GetRegion(typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			Class c = new Class(cu, TranslateClassType (typeDeclaration.Type), typeDeclaration.Modifier, region);
			if (currentClass.Count > 0) {
				Class cur = ((Class)currentClass.Peek());
				cur.InnerClasses.Add(c);
				c.FullyQualifiedName = String.Concat(cur.FullyQualifiedName, '.', typeDeclaration.Name);
			} else {
				if (currentNamespace.Count == 0) {
					c.FullyQualifiedName = typeDeclaration.Name;
				} else {
					c.FullyQualifiedName = String.Concat(currentNamespace.Peek(), '.', typeDeclaration.Name);
				}
				cu.Classes.Add(c);
			}
			if (typeDeclaration.BaseTypes != null) {
				foreach (TypeReference type in typeDeclaration.BaseTypes) {
					c.BaseTypes.Add(new ReturnType (type.Type));
				}
			}
			currentClass.Push(c);
			object ret = typeDeclaration.AcceptChildren(this, data);
			currentClass.Pop();
			c.UpdateModifier();
			return ret;
		}
		
		DefaultRegion GetRegion(Location start, Location end)
		{
			return new DefaultRegion(start.Y, start.X, end.Y, end.X);
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(methodDeclaration.StartLocation, methodDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(methodDeclaration.EndLocation,   methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new Location(-1, -1));
			
			ReturnType type = methodDeclaration.TypeReference == null ? new ReturnType("System.Void") : new ReturnType(methodDeclaration.TypeReference);
			Class c       = (Class)currentClass.Peek();
			
			
			DefaultMethod method = new DefaultMethod (String.Concat(methodDeclaration.Name), type, (ModifierEnum)methodDeclaration.Modifier, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (methodDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression par in methodDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					DefaultParameter p = new DefaultParameter (method, par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			method.Parameters = parameters;
			c.Methods.Add(method);
			return null;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(constructorDeclaration.StartLocation, constructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new Location (-1, -1));
			
			Class c       = (Class)currentClass.Peek();
			
			Constructor constructor = new Constructor(constructorDeclaration.Modifier, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (constructorDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression par in constructorDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					DefaultParameter p = new DefaultParameter (constructor, par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			constructor.Parameters = parameters;
			c.Methods.Add(constructor);
			return null;
		}
		
		
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(fieldDeclaration.StartLocation, fieldDeclaration.EndLocation);
			Class c = (Class)currentClass.Peek();
			if (currentClass.Count > 0) {
				foreach (VariableDeclaration field in fieldDeclaration.Fields) {
					ReturnType type = null;
					if (field.TypeReference != null) {
						type = new ReturnType(field.TypeReference);
					}
					DefaultField f = new DefaultField (type, field.Name, (ModifierEnum)fieldDeclaration.Modifier, region);
					if (type == null) {
						f.Modifiers = ModifierEnum.Const | ModifierEnum.SpecialName;
					}
					c.Fields.Add(f);
				}
			}
			return null;
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(propertyDeclaration.StartLocation, propertyDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(propertyDeclaration.BodyStart,     propertyDeclaration.BodyEnd);
			
			ReturnType type = propertyDeclaration.TypeReference == null ? new ReturnType("System.Void") : new ReturnType(propertyDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			
			DefaultProperty property = new DefaultProperty (propertyDeclaration.Name, type, (ModifierEnum)propertyDeclaration.Modifier, region, bodyRegion);
			c.Properties.Add(property);
			return null;
		}
		
		public override object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(eventDeclaration.StartLocation, eventDeclaration.EndLocation);
			DefaultRegion bodyRegion = null;
			
			ReturnType type = eventDeclaration.TypeReference != null ? new ReturnType(eventDeclaration.TypeReference) : null;
			Class c = (Class)currentClass.Peek();
			DefaultEvent e = new DefaultEvent(eventDeclaration.Name, type, (ModifierEnum)eventDeclaration.Modifier, region, bodyRegion);
			c.Events.Add(e);
			return null;
		}
	}
}
