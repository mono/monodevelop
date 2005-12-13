// created on 04.08.2003 at 17:49
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;

using RefParser = ICSharpCode.SharpRefactory.Parser;
using AST = ICSharpCode.SharpRefactory.Parser.AST;
using MonoDevelop.Projects.Parser;
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
		
		public AttributeTarget GetAttributeTarget (string attb)
		{
			switch (attb)
			{
				// check the 'assembly', I didn't saw it being used in c# parser....
				case "assembly":
					return AttributeTarget.Assembly;
				case "event":
					return AttributeTarget.Event;
				case "return":
					return AttributeTarget.Return;
				case "field":
					return AttributeTarget.Field;
				case "method":
					return AttributeTarget.Method;
				case "module":
					return AttributeTarget.Module;
				case "param":
					return AttributeTarget.Param;
				case "property":
					return AttributeTarget.Property;
				case "type":
					return AttributeTarget.Type;
				default:
					return AttributeTarget.None;
			}
		} 
		
		void FillAttributes (IDecoration decoration, ArrayList attributes)
		{
			// TODO Expressions???
			foreach (AST.AttributeSection section in attributes) {
				AttributeSection resultSection = new AttributeSection (GetAttributeTarget (section.AttributeTarget), GetRegion (section.StartLocation, section.EndLocation));
				
				foreach (AST.Attribute attribute in section.Attributes) {
					CSharpBinding.Parser.SharpDevelopTree.Attribute resultAttribute = new CSharpBinding.Parser.SharpDevelopTree.Attribute (attribute.Name, new ArrayList(attribute.PositionalArguments), new SortedList(), GetRegion (attribute.StartLocation, attribute.EndLocation));
					foreach (AST.NamedArgument n in attribute.NamedArguments) {
						resultAttribute.NamedArguments[n.Name] = n.Expr;
					}
					resultSection.Attributes.Add (resultAttribute);
				}
				decoration.Attributes.Add (resultSection);				
			}

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
			DefaultRegion bodyRegion = GetRegion(typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			DefaultRegion declarationRegion = GetRegion (typeDeclaration.DeclarationStartLocation, typeDeclaration.DeclarationEndLocation);
			Class c = new Class(cu, TranslateClassType(typeDeclaration.Type), typeDeclaration.Modifiers.Code, declarationRegion, bodyRegion);
			
			FillAttributes (c, typeDeclaration.Attributes);
			
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

			ReturnType type = new ReturnType(methodDeclaration.TypeReference);
			Class c       = (Class)currentClass.Peek();
			
			Method method = new Method (c, String.Concat(methodDeclaration.Name), type, methodDeclaration.Modifiers.Code, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (methodDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in methodDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter (method, par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			method.Parameters = parameters;
			
			FillAttributes (method, methodDeclaration.Attributes);
			
			c.Methods.Add(method);
			return null;
		}
		
		public override object Visit(AST.ConstructorDeclaration constructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(constructorDeclaration.StartLocation, constructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new Point(-1, -1));
			Class c       = (Class)currentClass.Peek();
			
			Constructor constructor = new Constructor (c, constructorDeclaration.Modifiers.Code, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (constructorDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in constructorDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter (constructor, par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			constructor.Parameters = parameters;
			
			FillAttributes (constructor, constructorDeclaration.Attributes);
			
			c.Methods.Add(constructor);
			return null;
		}
		
		public override object Visit(AST.DestructorDeclaration destructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(destructorDeclaration.StartLocation, destructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : new Point(-1, -1));
			
			Class c       = (Class)currentClass.Peek();
			
			Destructor destructor = new Destructor (c, c.Name, destructorDeclaration.Modifiers.Code, region, bodyRegion);
			
			FillAttributes (destructor, destructorDeclaration.Attributes);
			
			c.Methods.Add(destructor);
			return null;
		}
		
		// No attributes?
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
					Field f = new Field (c, type, field.Name, fieldDeclaration.Modifiers.Code, region);	
					c.Fields.Add(f);
					FillAttributes (f, fieldDeclaration.Attributes);
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
			
			Property property = new Property (c, propertyDeclaration.Name, type, propertyDeclaration.Modifiers.Code, region, bodyRegion);
			
			FillAttributes (property, propertyDeclaration.Attributes);
			
			if (propertyDeclaration.HasGetRegion)
				property.GetterRegion = GetRegion (propertyDeclaration.GetRegion.StartLocation, propertyDeclaration.GetRegion.EndLocation);
			if (propertyDeclaration.HasSetRegion) 
				property.SetterRegion = GetRegion (propertyDeclaration.SetRegion.StartLocation, propertyDeclaration.SetRegion.EndLocation);
						
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
					e = new Event (c, varDecl.Name, type, eventDeclaration.Modifiers.Code, region, bodyRegion);
					FillAttributes (e, eventDeclaration.Attributes);
					c.Events.Add(e);
				}
			} else {
				e = new Event (c, eventDeclaration.Name, type, eventDeclaration.Modifiers.Code, region, bodyRegion);
				FillAttributes (e, eventDeclaration.Attributes);
				c.Events.Add(e);
			}
			return null;
		}
		
		public override object Visit(AST.IndexerDeclaration indexerDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(indexerDeclaration.StartLocation, indexerDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(indexerDeclaration.BodyStart,     indexerDeclaration.BodyEnd);
			ParameterCollection parameters = new ParameterCollection();
			Class c = (Class)currentClass.Peek();
			Indexer i = new Indexer (c, new ReturnType(indexerDeclaration.TypeReference), parameters, indexerDeclaration.Modifiers.Code, region, bodyRegion);
			if (indexerDeclaration.Parameters != null) {
				foreach (AST.ParameterDeclarationExpression par in indexerDeclaration.Parameters) {
					ReturnType parType = new ReturnType(par.TypeReference);
					Parameter p = new Parameter (i, par.ParameterName, parType);
					parameters.Add(p);
				}
			}
			FillAttributes (i, indexerDeclaration.Attributes);
			c.Indexer.Add(i);
			return null;
		}
	}
}
