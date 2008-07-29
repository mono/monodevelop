// created on 04.08.2003 at 17:49
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.CodeDom;

using MonoDevelop.Projects.Dom;
using CSharpBinding.Parser.SharpDevelopTree;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace CSharpBinding.Parser
{/*
	public class CSharpVisitor : AbstractAstVisitor
	{
		DefaultCompilationUnit cu = new DefaultCompilationUnit();
		Stack currentNamespace = new Stack();
		Stack currentClass = new Stack();
		static CodeDomVisitor domVisitor = new  CodeDomVisitor ();
		
		public DefaultCompilationUnit Cu {
			get {
				return cu;
			}
		}
		
		public override object VisitCompilationUnit (CompilationUnit compilationUnit, object data)
		{
			//TODO: usings, Comments
			compilationUnit.AcceptChildren(this, data);
			return cu;
		}
		
		public override object VisitUsingDeclaration (ICSharpCode.NRefactory.Ast.UsingDeclaration usingDeclaration, object data)
		{
			DefaultUsing u = new DefaultUsing();
			u.Region = GetRegion(usingDeclaration.StartLocation, usingDeclaration.EndLocation);
			foreach (ICSharpCode.NRefactory.Ast.Using us in usingDeclaration.Usings) {
				if (us.IsAlias)
					u.Aliases [us.Name] = new ReturnType (us.Alias);
				else
					u.Usings.Add (us.Name);
			}
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
		
		void FillAttributes (IDecoration decoration, IEnumerable attributes)
		{
			// TODO Expressions???
			foreach (AttributeSection section in attributes) {
				DefaultAttributeSection resultSection = new DefaultAttributeSection (GetAttributeTarget (section.AttributeTarget), GetRegion (section.StartLocation, section.EndLocation));
				foreach (ICSharpCode.NRefactory.Ast.Attribute attribute in section.Attributes)
				{
					CodeExpression[] positionals = new CodeExpression [attribute.PositionalArguments.Count];
					for (int n=0; n<attribute.PositionalArguments.Count; n++) {
						positionals [n] = GetDomExpression (attribute.PositionalArguments[n]);
					}
					
					NamedAttributeArgument[] named = new NamedAttributeArgument [attribute.NamedArguments.Count];
					for (int n=0; n<attribute.NamedArguments.Count; n++) {
						ICSharpCode.NRefactory.Ast.NamedArgumentExpression arg = (ICSharpCode.NRefactory.Ast.NamedArgumentExpression) attribute.NamedArguments [n];
						named [n] = new NamedAttributeArgument (arg.Name, GetDomExpression (arg.Expression));
					}
					
					DefaultAttribute resultAttribute = new DefaultAttribute (attribute.Name, positionals, named, GetRegion (attribute.StartLocation, attribute.EndLocation));
					resultSection.Attributes.Add (resultAttribute);
				}
				decoration.Attributes.Add (resultSection);				
			}

		}
		
		CodeExpression GetDomExpression (object ob)
		{
			return (CodeExpression) ((ICSharpCode.NRefactory.Ast.Expression)ob).AcceptVisitor (domVisitor, null);
		}
		
//		ModifierEnum VisitModifier(ICSharpCode.NRefactory.Parser.Modifier m)
//		{
//			return (ModifierEnum)m;
//		}
		
		public override object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{./Parser/CSharpVisitor.cs
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
			DefaultRegion bodyRegion = GetRegion(typeDeclaration.BodyStartLocation, typeDeclaration.EndLocation);
			bodyRegion.EndColumn++;
			DefaultRegion declarationRegion = GetRegion(typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			Modifiers mf = typeDeclaration.Modifier;
			Class c = new Class(cu, TranslateClassType(typeDeclaration.Type), mf, declarationRegion, bodyRegion);
			
			FillAttributes (c, typeDeclaration.Attributes);
			
			c.Name = typeDeclaration.Name;
			if (currentClass.Count > 0) {
				Class cur = ((Class)currentClass.Peek());
				cur.InnerClasses.Add(c);
				c.DeclaredIn = cur;
			} else {
				if (currentNamespace.Count > 0)
					c.Namespace = (string) currentNamespace.Peek();
				cu.Classes.Add(c);
			}
			
			// Get base classes (with generic arguments et al)
			if (typeDeclaration.BaseTypes != null) {
				foreach (ICSharpCode.NRefactory.Ast.TypeReference type in typeDeclaration.BaseTypes) {
					c.BaseTypes.Add(new ReturnType(type));
				}
			}
			if (c.ClassType == MonoDevelop.Projects.Parser.ClassType.Enum) {
				c.BaseTypes.Add (new ReturnType ("System.Enum"));
			}
			
			// Get generic parameters for this type
			if (typeDeclaration.Templates != null && typeDeclaration.Templates.Count > 0) {
				c.GenericParameters = new GenericParameterList();
				c.Name = String.Concat (c.Name, "`", typeDeclaration.Templates.Count.ToString());
				foreach (TemplateDefinition td in typeDeclaration.Templates) {
					c.GenericParameters.Add (new CSharpBinding.Parser.SharpDevelopTree.GenericParameter(td));
				}
			}
			
			currentClass.Push(c);
			object ret = typeDeclaration.AcceptChildren(this, data);
			currentClass.Pop();
			c.UpdateModifier();
			return ret;
		}
		
		public override object VisitDelegateDeclaration(DelegateDeclaration typeDeclaration, object data)
		{
			DefaultRegion declarationRegion = GetRegion (typeDeclaration.StartLocation, typeDeclaration.EndLocation);
			Modifiers mf = typeDeclaration.Modifier;
			Class c = new Class (cu, MonoDevelop.Projects.Parser.ClassType.Delegate, mf, declarationRegion, null);
			
			FillAttributes (c, typeDeclaration.Attributes);
			
			c.Name = typeDeclaration.Name;
			if (currentClass.Count > 0) {
				Class cur = ((Class)currentClass.Peek());
				cur.InnerClasses.Add(c);
				c.DeclaredIn = cur;
			} else {
				if (currentNamespace.Count > 0)
					c.Namespace = (string) currentNamespace.Peek();
				cu.Classes.Add(c);
			}
			
			if (typeDeclaration.ReturnType == null)
				return c;
				
			ReturnType type = new ReturnType (typeDeclaration.ReturnType);
			
			mf = Modifiers.None;
			Method method = new Method ("Invoke", type, mf, null, null);
			ParameterCollection parameters = new ParameterCollection();
			if (typeDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression par in typeDeclaration.Parameters) {
					parameters.Add (CreateParameter (par, method));
				}
			}
			method.Parameters = parameters;
			c.Methods.Add(method);			
			
			c.UpdateModifier();
			return c;
		}
		
		DefaultRegion GetRegion(Location start, Location end)
		{
			return new DefaultRegion(start.Y, start.X, end.Y, end.X);
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			VisitMethod (methodDeclaration, data);
			return null;
		}
		
		Method VisitMethod (MethodDeclaration methodDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(methodDeclaration.StartLocation, methodDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(methodDeclaration.EndLocation, methodDeclaration.Body != null ? methodDeclaration.Body.EndLocation : new Location (-1, -1));

			ReturnType type = new ReturnType(methodDeclaration.TypeReference);
			Class c       = (Class)currentClass.Peek();
			
			Modifiers mf = methodDeclaration.Modifier;
			Method method = new Method (String.Concat(methodDeclaration.Name), type, mf, region, bodyRegion);
			if (methodDeclaration.InterfaceImplementations != null && methodDeclaration.InterfaceImplementations.Count > 0) {
				method.ExplicitDeclaration = new ReturnType(methodDeclaration.InterfaceImplementations[0].InterfaceType);
			}
			
			ParameterCollection parameters = method.Parameters;
			if (methodDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression par in methodDeclaration.Parameters) {
					parameters.Add (CreateParameter (par, method));
				}
			}
			
			// Get generic parameters for this type
			if (methodDeclaration.Templates != null && methodDeclaration.Templates.Count > 0) {
				method.GenericParameters = new GenericParameterList();
				foreach (TemplateDefinition td in methodDeclaration.Templates) {
					method.GenericParameters.Add (new CSharpBinding.Parser.SharpDevelopTree.GenericParameter(td));
				}
			}
			
			FillAttributes (method, methodDeclaration.Attributes);
			
			c.Methods.Add(method);
			return method;
		}
		
		IParameter CreateParameter (ParameterDeclarationExpression par, IMember declaringMember)
		{
			ReturnType parType = new ReturnType (par.TypeReference);
			DefaultParameter p = new DefaultParameter (declaringMember, par.ParameterName, parType);
			if ((par.ParamModifier & ParameterModifiers.Out) != 0)
				p.Modifier |= ParameterModifier.Out;
			if ((par.ParamModifier & ParameterModifiers.Ref) != 0)
				p.Modifier |= ParameterModifier.Ref;
			if ((par.ParamModifier & ParameterModifiers.Params) != 0)
				p.Modifier |= ParameterModifier.Params;
			return p;
		}
		
		public override object VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			string name = null;
			bool binary = operatorDeclaration.Parameters.Count == 2;
			
			switch (operatorDeclaration.OverloadableOperator) {
				case OverloadableOperatorType.Add:
					if (binary)
						name = "op_Addition";
					else
						name = "op_UnaryPlus";
					break;
				case OverloadableOperatorType.Subtract:
					if (binary)
						name = "op_Subtraction";
					else
						name = "op_UnaryNegation";
					break;
				case OverloadableOperatorType.Multiply:
					if (binary)
						name = "op_Multiply";
					break;
				case OverloadableOperatorType.Divide:
					if (binary)
						name = "op_Division";
					break;
				case OverloadableOperatorType.Modulus:
					if (binary)
						name = "op_Modulus";
					break;
				
				case OverloadableOperatorType.Not:
					name = "op_LogicalNot";
					break;
				case OverloadableOperatorType.BitNot:
					name = "op_OnesComplement";
					break;
				
				case OverloadableOperatorType.BitwiseAnd:
					if (binary)
						name = "op_BitwiseAnd";
					break;
				case OverloadableOperatorType.BitwiseOr:
					if (binary)
						name = "op_BitwiseOr";
					break;
				case OverloadableOperatorType.ExclusiveOr:
					if (binary)
						name = "op_ExclusiveOr";
					break;
				
				case OverloadableOperatorType.ShiftLeft:
					if (binary)
						name = "op_LeftShift";
					break;
				case OverloadableOperatorType.ShiftRight:
					if (binary)
						name = "op_RightShift";
					break;
				
				case OverloadableOperatorType.GreaterThan:
					if (binary)
						name = "op_GreaterThan";
					break;
				case OverloadableOperatorType.GreaterThanOrEqual:
					if (binary)
						name = "op_GreaterThanOrEqual";
					break;
				case OverloadableOperatorType.Equality:
					if (binary)
						name = "op_Equality";
					break;
				case OverloadableOperatorType.InEquality:
					if (binary)
						name = "op_Inequality";
					break;
				case OverloadableOperatorType.LessThan:
					if (binary)
						name = "op_LessThan";
					break;
				case OverloadableOperatorType.LessThanOrEqual:
					if (binary)
						name = "op_LessThanOrEqual";
					break;
				
				case OverloadableOperatorType.Increment:
					name = "op_Increment";
					break;
				case OverloadableOperatorType.Decrement:
					name = "op_Decrement";
					break;
				
				case OverloadableOperatorType.IsTrue:
					name = "op_True";
					break;
				case OverloadableOperatorType.IsFalse:
					name = "op_False";
					break;
				
				// Handle the non-overloadable case
				case OverloadableOperatorType.None:
					switch(operatorDeclaration.ConversionType)
					{
						case ConversionType.Implicit:
							name = "op_Implicit";
							break;
						case ConversionType.Explicit:
							name = "op_Explicit";
							break;
					}
					break;
			}
			if (name != null) {
				Method method = VisitMethod (operatorDeclaration, data);
				method.Name = name;
				method.Modifiers = method.Modifiers | ModifierEnum.SpecialName;
				return null;
			} 
			return null;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(constructorDeclaration.StartLocation, constructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(constructorDeclaration.EndLocation, constructorDeclaration.Body != null ? constructorDeclaration.Body.EndLocation : new Location (-1, -1));
			Class c       = (Class)currentClass.Peek();
			
			Modifiers mf = constructorDeclaration.Modifier;
			Constructor constructor = new Constructor (mf, region, bodyRegion);
			ParameterCollection parameters = new ParameterCollection();
			if (constructorDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression par in constructorDeclaration.Parameters) {
					parameters.Add (CreateParameter (par, constructor));
				}
			}
			constructor.Parameters = parameters;
			
			FillAttributes (constructor, constructorDeclaration.Attributes);
			
			c.Methods.Add(constructor);
			return null;
		}
		
		public override object VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(destructorDeclaration.StartLocation, destructorDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(destructorDeclaration.EndLocation, destructorDeclaration.Body != null ? destructorDeclaration.Body.EndLocation : new Location (-1, -1));
			
			Class c       = (Class)currentClass.Peek();
			
			Modifiers mf = destructorDeclaration.Modifier;
			Destructor destructor = new Destructor (c.Name, mf, region, bodyRegion);
			
			FillAttributes (destructor, destructorDeclaration.Attributes);
			
			c.Methods.Add(destructor);
			return null;
		}
		
		// No attributes?
		public override object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(fieldDeclaration.StartLocation, fieldDeclaration.EndLocation);
			Class c = (Class)currentClass.Peek();
			ReturnType type = null;
			if (fieldDeclaration.TypeReference == null) {
				Debug.Assert(c.ClassType == MonoDevelop.Projects.Parser.ClassType.Enum);
			} else {
				type = new ReturnType(fieldDeclaration.TypeReference);
			}
			if (currentClass.Count > 0) {
				foreach (VariableDeclaration field in fieldDeclaration.Fields) {
					DefaultField f;
					f = new DefaultField (type, field.Name, (ModifierEnum) fieldDeclaration.Modifier, region);	
					if (c.ClassType == MonoDevelop.Projects.Parser.ClassType.Enum)
						f.AddModifier (ModifierEnum.Const);
					c.Fields.Add(f);
					FillAttributes (f, fieldDeclaration.Attributes);
				}
			}
			
			return null;
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(propertyDeclaration.StartLocation, propertyDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(propertyDeclaration.BodyStart,     propertyDeclaration.BodyEnd);
			
			ReturnType type = new ReturnType(propertyDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			
			Modifiers mf = propertyDeclaration.Modifier;
			DefaultProperty property = new DefaultProperty (propertyDeclaration.Name, type, (ModifierEnum)mf, region, bodyRegion);
			
			if (propertyDeclaration.InterfaceImplementations != null && propertyDeclaration.InterfaceImplementations.Count > 0) {
				property.ExplicitDeclaration = new ReturnType (propertyDeclaration.InterfaceImplementations[0].InterfaceType);
			}
			
			FillAttributes (property, propertyDeclaration.Attributes);
			
			if (propertyDeclaration.HasGetRegion)
				property.GetterRegion = GetRegion (propertyDeclaration.GetRegion.StartLocation, propertyDeclaration.GetRegion.EndLocation);
			if (propertyDeclaration.HasSetRegion) 
				property.SetterRegion = GetRegion (propertyDeclaration.SetRegion.StartLocation, propertyDeclaration.SetRegion.EndLocation);
						
			c.Properties.Add(property);
			return null;
		}
		
		public override object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(eventDeclaration.StartLocation, eventDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(eventDeclaration.BodyStart,     eventDeclaration.BodyEnd);
			ReturnType type = new ReturnType(eventDeclaration.TypeReference);
			Class c = (Class)currentClass.Peek();
			DefaultEvent e = null;
			
			if (eventDeclaration.VariableDeclarators != null) {
				foreach (ICSharpCode.NRefactory.Ast.VariableDeclaration varDecl in eventDeclaration.VariableDeclarators) {
					ModifierFlags mf = eventDeclaration.Modifier;
					e = new DefaultEvent (c, varDecl.Name, type, mf, region, bodyRegion);
					FillAttributes (e, eventDeclaration.Attributes);
					c.Events.Add(e);
				}
			} else {
				Modifiers mf = eventDeclaration.Modifier;
				e = new DefaultEvent (eventDeclaration.Name, type, (ModifierEnum)mf, region, bodyRegion);
				FillAttributes (e, eventDeclaration.Attributes);
				c.Events.Add(e);
//			}
			if (eventDeclaration.InterfaceImplementations != null && eventDeclaration.InterfaceImplementations.Count > 0) {
				e.ExplicitDeclaration = new ReturnType(eventDeclaration.InterfaceImplementations[0].InterfaceType);
			}
			return null;
		}
		
		public override object VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			DefaultRegion region     = GetRegion(indexerDeclaration.StartLocation, indexerDeclaration.EndLocation);
			DefaultRegion bodyRegion = GetRegion(indexerDeclaration.BodyStart,     indexerDeclaration.BodyEnd);
			ParameterCollection parameters = new ParameterCollection();
			Class c = (Class)currentClass.Peek();
			Modifiers mf = indexerDeclaration.Modifier;
			DefaultIndexer i = new DefaultIndexer (new ReturnType(indexerDeclaration.TypeReference), parameters, (ModifierEnum)mf, region, bodyRegion);
			if (indexerDeclaration.Parameters != null) {
				foreach (ParameterDeclarationExpression par in indexerDeclaration.Parameters) {
					parameters.Add (CreateParameter (par, i));
				}
			}
			FillAttributes (i, indexerDeclaration.Attributes);
			c.Indexer.Add(i);
			if (indexerDeclaration.InterfaceImplementations != null && indexerDeclaration.InterfaceImplementations.Count > 0) {
				i.ExplicitDeclaration = new ReturnType (indexerDeclaration.InterfaceImplementations[0].InterfaceType);
			}
			return null;
		}
	}*/
}
