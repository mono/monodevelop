// created on 04.08.2003 at 17:49
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;

using RefParser = ICSharpCode.SharpRefactory.Parser.VB;
using AST = ICSharpCode.SharpRefactory.Parser.AST.VB;
using MonoDevelop.Internal.Parser;
using VBBinding.Parser.SharpDevelopTree;

namespace VBBinding.Parser
{
	public class Using : AbstractUsing
	{
	}
	
	public class VBNetVisitor : RefParser.AbstractASTVisitor
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
			//TODO: Imports, Comments
			compilationUnit.AcceptChildren(this, data);
			return cu;
		}
		
		public override object Visit(AST.ImportsDeclaration usingDeclaration, object data)
		{
			Using u = new Using();
			u.Usings.Add(usingDeclaration.Namespace);
			cu.Usings.Add(u);
			return data;
		}
		
		public override object Visit(AST.ImportsAliasDeclaration usingAliasDeclaration, object data)
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
					foreach (AST.NamedArgumentExpression n in attribute.NamedArguments) {
						a.NamedArguments[n.Parametername] = n.Expression;
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
				case RefParser.Types.Structure:
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
			if (typeDeclaration.BaseType != null) {
				c.BaseTypes.Add(typeDeclaration.BaseType);
			}
			if (typeDeclaration.BaseInterfaces != null) {
				foreach (AST.TypeReference type in typeDeclaration.BaseInterfaces) {
					c.BaseTypes.Add(type.Type);
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
			DefaultRegion bodyRegion = GetRegion(methodDeclaration.EndLocation,   methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new Point(-1, -1));
			
			ReturnType type = methodDeclaration.TypeReference == null ? new ReturnType("System.Void") : new ReturnType(methodDeclaration.TypeReference);
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
		
		
		public override object Visit(AST.FieldDeclaration fieldDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(fieldDeclaration.StartLocation, fieldDeclaration.EndLocation);
			Class c = (Class)currentClass.Peek();
			if (currentClass.Count > 0) {
				foreach (AST.VariableDeclaration field in fieldDeclaration.Fields) {
					ReturnType type = null;
					if (field.Type != null) {
						type = new ReturnType(field.Type);
					}
					Field f = new Field(type, field.Name, fieldDeclaration.Modifier, region);
					if (type == null) {
						f.SetModifiers(ModifierEnum.Const | ModifierEnum.SpecialName);
					}
					c.Fields.Add(f);
				}
			}
			return null;
		}
		
		public override object Visit(AST.PropertyDeclaration propertyDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(propertyDeclaration.StartLocation, propertyDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(propertyDeclaration.BodyStart,     propertyDeclaration.BodyEnd);
			
			ReturnType type = propertyDeclaration.TypeReference == null ? new ReturnType("System.Void") : new ReturnType(propertyDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			
			Property property = new Property(propertyDeclaration.Name, type, propertyDeclaration.Modifier, region, bodyRegion);
			c.Properties.Add(property);
			return null;
		}
		
		public override object Visit(AST.EventDeclaration eventDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(eventDeclaration.StartLocation, eventDeclaration.EndLocation);
			DefaultRegion bodyRegion = null;
			
			ReturnType type = eventDeclaration.TypeReference != null ? new ReturnType(eventDeclaration.TypeReference) : null;
			Class c = (Class)currentClass.Peek();
			Event e = new Event(eventDeclaration.Name, type, eventDeclaration.Modifier, region, bodyRegion);
			c.Events.Add(e);
			return null;
		}
	}
}
