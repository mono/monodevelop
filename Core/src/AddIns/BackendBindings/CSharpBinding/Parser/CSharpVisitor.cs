// created on 04.08.2003 at 17:49
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;

using RefParser = ICSharpCode.SharpRefactory.Parser;
using AST = ICSharpCode.SharpRefactory.Parser.AST;
using MonoDevelop.Internal.Parser;
using CSharpBinding.Parser.SharpDevelopTree;

namespace CSharpBinding.Parser
{
	public class Using : AbstractUsing
	{
	}
	
	public class CSharpVisitor : RefParser.AbstractASTVisitor
	{
		CompilationUnit cu = new CompilationUnit();
		Stack currentNamespace = new Stack();
		Stack currentClass = new Stack();
		
		public CompilationUnit Cu {
			get {
				return cu;
			}
		}
		
		public override object Visit(AST.CompilationUnit compilationUnit, object data)
		{
			//TODO: usings, Comments
			compilationUnit.AcceptChildren(this, data);
			return cu;
		}
		
		public override object Visit(AST.UsingDeclaration usingDeclaration, object data)
		{
			Using u = new Using();
			u.Usings.Add(usingDeclaration.Namespace);
			cu.Usings.Add(u);
			return data;
		}
		
		public override object Visit(AST.UsingAliasDeclaration usingAliasDeclaration, object data)
		{
			Using u = new Using();
			u.Aliases[usingAliasDeclaration.Alias] = usingAliasDeclaration.Namespace;
			cu.Usings.Add(u);
			return data;
		}
		
		AttributeSectionCollection VisitAttributes(ArrayList attributes)
		{
			// TODO Expressions???
			//AttributeSectionCollection result = new AttributeSectionCollection();
			foreach (AST.AttributeSection section in attributes) {
				//AttributeCollection resultAttributes = new AttributeCollection();
				foreach (AST.Attribute attribute in section.Attributes) {
					IAttribute a = new ASTAttribute(attribute.Name, new ArrayList(attribute.PositionalArguments), new SortedList());
					foreach (AST.NamedArgument n in attribute.NamedArguments) {
						a.NamedArguments[n.Name] = n.Expr;
					}
				}
				//IAttributeSection s = new AttributeSection((AttributeTarget)Enum.Parse(typeof (AttributeTarget), section.AttributeTarget), resultAttributes);
			}
			return null;
		}
		
//		ModifierEnum VisitModifier(ICSharpCode.SharpRefactory.Parser.Modifier m)
//		{
//			return (ModifierEnum)m;
//		}
		
		public override object Visit(AST.NamespaceDeclaration namespaceDeclaration, object data)
		{
			string name;
			if (currentNamespace.Count == 0) {
				name = namespaceDeclaration.NameSpace;
			} else {
				name = String.Concat((string)currentNamespace.Peek(), '.', namespaceDeclaration.NameSpace);
			}
			currentNamespace.Push(name);
			object ret = namespaceDeclaration.AcceptChildren(this, data);
			currentNamespace.Pop();
			return ret;
		}
		
		ClassType TranslateClassType(RefParser.Types type)
		{
			switch (type) {
				case RefParser.Types.Class:
					return ClassType.Class;
				case RefParser.Types.Enum:
					return ClassType.Enum;
				case RefParser.Types.Interface:
					return ClassType.Interface;
				case RefParser.Types.Struct:
					return ClassType.Struct;
			}
			return ClassType.Class;
		}
		
		public override object Visit(AST.TypeDeclaration typeDeclaration, object data)
		{
			DefaultRegion region = GetRegion(typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			Class c = new Class(cu, TranslateClassType(typeDeclaration.Type), typeDeclaration.Modifier, region);
			
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
				foreach (string type in typeDeclaration.BaseTypes) {
					c.BaseTypes.Add(type);
				}
			}
			currentClass.Push(c);
			object ret = typeDeclaration.AcceptChildren(this, data);
			currentClass.Pop();
			c.UpdateModifier();
			return ret;
		}
		
		DefaultRegion GetRegion(Point start, Point end)
		{
			return new DefaultRegion(start.Y, start.X, end.Y, end.X);
		}
		
		public override object Visit(AST.MethodDeclaration methodDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(methodDeclaration.StartLocation, methodDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(methodDeclaration.EndLocation, methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new Point(-1, -1));
			//			Console.WriteLine(region + " --- " + bodyRegion);
			ReturnType type = new ReturnType(methodDeclaration.TypeReference);
			Class c       = (Class)currentClass.Peek();
			
			Method method = new Method(String.Concat(methodDeclaration.Name), type, methodDeclaration.Modifier, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (methodDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in methodDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter(par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			method.Parameters = parameters;
			c.Methods.Add(method);
			return null;
		}
		
		public override object Visit(AST.ConstructorDeclaration constructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(constructorDeclaration.StartLocation, constructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new Point(-1, -1));
			Class c       = (Class)currentClass.Peek();
			
			Constructor constructor = new Constructor(constructorDeclaration.Modifier, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (constructorDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in constructorDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter(par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			constructor.Parameters = parameters;
			c.Methods.Add(constructor);
			return null;
		}
		
		public override object Visit(AST.DestructorDeclaration destructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(destructorDeclaration.StartLocation, destructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : new Point(-1, -1));
			
			Class c       = (Class)currentClass.Peek();
			
			Destructor destructor = new Destructor(c.Name, destructorDeclaration.Modifier, region, bodyRegion);
			c.Methods.Add(destructor);
			return null;
		}
		
		
		public override object Visit(AST.FieldDeclaration fieldDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(fieldDeclaration.StartLocation, fieldDeclaration.EndLocation);
			Class c = (Class)currentClass.Peek();
			ReturnType type = null;
			if (fieldDeclaration.TypeReference == null) {
				Debug.Assert(c.ClassType == ClassType.Enum);
			} else {
				type = new ReturnType(fieldDeclaration.TypeReference);
			}
			if (currentClass.Count > 0) {
				foreach (AST.VariableDeclaration field in fieldDeclaration.Fields) {
					Field f = new Field(type, field.Name, fieldDeclaration.Modifier, region);
					
					c.Fields.Add(f);
				}
			}
			return null;
		}
		
		public override object Visit(AST.PropertyDeclaration propertyDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(propertyDeclaration.StartLocation, propertyDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(propertyDeclaration.BodyStart,     propertyDeclaration.BodyEnd);
			
			ReturnType type = new ReturnType(propertyDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			
			Property property = new Property(propertyDeclaration.Name, type, propertyDeclaration.Modifier, region, bodyRegion);
			c.Properties.Add(property);
			return null;
		}
		
		public override object Visit(AST.EventDeclaration eventDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(eventDeclaration.StartLocation, eventDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(eventDeclaration.BodyStart,     eventDeclaration.BodyEnd);
			ReturnType type = new ReturnType(eventDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			Event e = null;
			
			if (eventDeclaration.VariableDeclarators != null) {
				foreach (ICSharpCode.SharpRefactory.Parser.AST.VariableDeclaration varDecl in eventDeclaration.VariableDeclarators) {
					e = new Event(varDecl.Name, type, eventDeclaration.Modifier, region, bodyRegion);
					c.Events.Add(e);
				}
			} else {
				e = new Event(eventDeclaration.Name, type, eventDeclaration.Modifier, region, bodyRegion);
				c.Events.Add(e);
			}
			return null;
		}
		
		public override object Visit(AST.IndexerDeclaration indexerDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(indexerDeclaration.StartLocation, indexerDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(indexerDeclaration.BodyStart,     indexerDeclaration.BodyEnd);
			ParameterCollection parameters = new ParameterCollection();
			Indexer i = new Indexer(new ReturnType(indexerDeclaration.TypeReference), parameters, indexerDeclaration.Modifier, region, bodyRegion);
			if (indexerDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in indexerDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter(par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			Class c = (Class)currentClass.Peek();
			c.Indexer.Add(i);
			return null;
		}
	}
}
