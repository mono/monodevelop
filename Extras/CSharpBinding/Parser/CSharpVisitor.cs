// created on 04.08.2003 at 17:49
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.CodeDom;

using RefParser = ICSharpCode.SharpRefactory.Parser;
using AST = ICSharpCode.SharpRefactory.Parser.AST;
using MonoDevelop.Projects.Parser;
using CSharpBinding.Parser.SharpDevelopTree;
using ModifierFlags = ICSharpCode.SharpRefactory.Parser.ModifierFlags;

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
		static ICSharpCode.SharpRefactory.Parser.CodeDOMVisitor domVisitor = new ICSharpCode.SharpRefactory.Parser.CodeDOMVisitor ();
		
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
				foreach (AST.Attribute attribute in section.Attributes)
				{
					CodeExpression[] positionals = new CodeExpression [attribute.PositionalArguments.Count];
					for (int n=0; n<attribute.PositionalArguments.Count; n++) {
						positionals [n] = GetDomExpression (attribute.PositionalArguments[n]);
					}
					
					NamedAttributeArgument[] named = new NamedAttributeArgument [attribute.NamedArguments.Count];
					for (int n=0; n<attribute.NamedArguments.Count; n++) {
						ICSharpCode.SharpRefactory.Parser.AST.NamedArgument arg = (ICSharpCode.SharpRefactory.Parser.AST.NamedArgument) attribute.NamedArguments [n];
						named [n] = new NamedAttributeArgument (arg.Name, GetDomExpression (arg.Expr));
					}
					
					CSharpBinding.Parser.SharpDevelopTree.Attribute resultAttribute = new CSharpBinding.Parser.SharpDevelopTree.Attribute (attribute.Name, positionals, named, GetRegion (attribute.StartLocation, attribute.EndLocation));
					resultSection.Attributes.Add (resultAttribute);
				}
				decoration.Attributes.Add (resultSection);				
			}

		}
		
		CodeExpression GetDomExpression (object ob)
		{
			return (CodeExpression) ((ICSharpCode.SharpRefactory.Parser.AST.Expression)ob).AcceptVisitor (domVisitor, null);
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
			ModifierFlags mf = typeDeclaration.Modifiers != null ? typeDeclaration.Modifiers.Code : ModifierFlags.None;
			Class c = new Class(cu, TranslateClassType(typeDeclaration.Type), mf, declarationRegion, bodyRegion);
			
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
			
			ModifierFlags mf = methodDeclaration.Modifiers != null ? methodDeclaration.Modifiers.Code : ModifierFlags.None;
			Method method = new Method (c, String.Concat(methodDeclaration.Name), type, mf, region, bodyRegion);
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
			
			ModifierFlags mf = constructorDeclaration.Modifiers != null ? constructorDeclaration.Modifiers.Code : ModifierFlags.None;
			Constructor constructor = new Constructor (c, mf, region, bodyRegion);
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
			
			ModifierFlags mf = destructorDeclaration.Modifiers != null ? destructorDeclaration.Modifiers.Code : ModifierFlags.None;
			Destructor destructor = new Destructor (c, c.Name, mf, region, bodyRegion);
			
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
					ModifierFlags mf = fieldDeclaration.Modifiers != null ? fieldDeclaration.Modifiers.Code : ModifierFlags.None;
					Field f = new Field (c, type, field.Name, mf, region);	
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
			
			ModifierFlags mf = propertyDeclaration.Modifiers != null ? propertyDeclaration.Modifiers.Code : ModifierFlags.None;
			Property property = new Property (c, propertyDeclaration.Name, type, mf, region, bodyRegion);
			
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
					ModifierFlags mf = eventDeclaration.Modifiers != null ? eventDeclaration.Modifiers.Code : ModifierFlags.None;
					e = new Event (c, varDecl.Name, type, mf, region, bodyRegion);
					FillAttributes (e, eventDeclaration.Attributes);
					c.Events.Add(e);
				}
			} else {
				ModifierFlags mf = eventDeclaration.Modifiers != null ? eventDeclaration.Modifiers.Code : ModifierFlags.None;
				e = new Event (c, eventDeclaration.Name, type, mf, region, bodyRegion);
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
			ModifierFlags mf = indexerDeclaration.Modifiers != null ? indexerDeclaration.Modifiers.Code : ModifierFlags.None;
			Indexer i = new Indexer (c, new ReturnType(indexerDeclaration.TypeReference), parameters, mf, region, bodyRegion);
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
