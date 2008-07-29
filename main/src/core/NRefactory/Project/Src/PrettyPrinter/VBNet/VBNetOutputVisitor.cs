// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 2200 $</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.VB;

namespace ICSharpCode.NRefactory.PrettyPrinter
{
	public class VBNetOutputVisitor : IOutputAstVisitor
	{
		Errors                  errors             = new Errors();
		VBNetOutputFormatter    outputFormatter;
		VBNetPrettyPrintOptions prettyPrintOptions = new VBNetPrettyPrintOptions();
		NodeTracker             nodeTracker;
		TypeDeclaration         currentType;
		bool printFullSystemType;
		
		Stack<int> exitTokenStack = new Stack<int>();
		
		public string Text {
			get {
				return outputFormatter.Text;
			}
		}
		
		public Errors Errors {
			get {
				return errors;
			}
		}
		
		AbstractPrettyPrintOptions IOutputAstVisitor.Options {
			get { return prettyPrintOptions; }
		}
		
		public VBNetPrettyPrintOptions Options {
			get { return prettyPrintOptions; }
		}
		
		public NodeTracker NodeTracker {
			get {
				return nodeTracker;
			}
		}
		
		public IOutputFormatter OutputFormatter {
			get {
				return outputFormatter;
			}
		}
		
		public VBNetOutputVisitor()
		{
			outputFormatter = new VBNetOutputFormatter(prettyPrintOptions);
			nodeTracker     = new NodeTracker(this);
		}
		
		void Error(string text, Location position)
		{
			errors.Error(position.Y, position.X, text);
		}
		
		void UnsupportedNode(INode node)
		{
			Error(node.GetType().Name + " is unsupported", node.StartLocation);
		}
		
		#region ICSharpCode.NRefactory.Parser.IASTVisitor interface implementation
		public object VisitCompilationUnit(CompilationUnit compilationUnit, object data)
		{
			nodeTracker.TrackedVisitChildren(compilationUnit, data);
			outputFormatter.EndFile();
			return null;
		}
		
		/// <summary>
		/// Converts type name to primitive type name. Returns null if typeString is not
		/// a primitive type.
		/// </summary>
		static string ConvertTypeString(string typeString)
		{
			string primitiveType;
			TypeReference.PrimitiveTypesVBReverse.TryGetValue(typeString, out primitiveType);
			return primitiveType;
		}

		public object VisitTypeReference(TypeReference typeReference, object data)
		{
			if (typeReference == TypeReference.ClassConstraint) {
				outputFormatter.PrintToken(Tokens.Class);
			} else if (typeReference == TypeReference.StructConstraint) {
				outputFormatter.PrintToken(Tokens.Structure);
			} else if (typeReference == TypeReference.NewConstraint) {
				outputFormatter.PrintToken(Tokens.New);
			} else {
				PrintTypeReferenceWithoutArray(typeReference);
				if (typeReference.IsArrayType) {
					PrintArrayRank(typeReference.RankSpecifier, 0);
				}
			}
			return null;
		}
		
		void PrintTypeReferenceWithoutArray(TypeReference typeReference)
		{
			if (typeReference.IsGlobal) {
				outputFormatter.PrintToken(Tokens.Global);
				outputFormatter.PrintToken(Tokens.Dot);
			}
			if (typeReference.Type == null || typeReference.Type.Length ==0) {
				outputFormatter.PrintText("Void");
			} else if (printFullSystemType || typeReference.IsGlobal) {
				outputFormatter.PrintIdentifier(typeReference.SystemType);
			} else {
				string shortTypeName = ConvertTypeString(typeReference.SystemType);
				if (shortTypeName != null) {
					outputFormatter.PrintText(shortTypeName);
				} else {
					outputFormatter.PrintIdentifier(typeReference.Type);
				}
			}
			if (typeReference.GenericTypes != null && typeReference.GenericTypes.Count > 0) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				outputFormatter.PrintToken(Tokens.Of);
				outputFormatter.Space();
				AppendCommaSeparatedList(typeReference.GenericTypes);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			for (int i = 0; i < typeReference.PointerNestingLevel; ++i) {
				outputFormatter.PrintToken(Tokens.Times);
			}
		}
		
		void PrintArrayRank(int[] rankSpecifier, int startRank)
		{
			for (int i = startRank; i < rankSpecifier.Length; ++i) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				for (int j = 0; j < rankSpecifier[i]; ++j) {
					outputFormatter.PrintToken(Tokens.Comma);
				}
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
		}
		
		public object VisitInnerClassTypeReference(InnerClassTypeReference innerClassTypeReference, object data)
		{
			nodeTracker.TrackedVisit(innerClassTypeReference.BaseType, data);
			outputFormatter.PrintToken(Tokens.Dot);
			return VisitTypeReference((TypeReference)innerClassTypeReference, data);
		}
		
		#region Global scope
		public object VisitAttributeSection(AttributeSection attributeSection, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintText("<");
			if (attributeSection.AttributeTarget != null && attributeSection.AttributeTarget.Length > 0) {
				outputFormatter.PrintIdentifier(attributeSection.AttributeTarget);
				outputFormatter.PrintToken(Tokens.Colon);
				outputFormatter.Space();
			}
			Debug.Assert(attributeSection.Attributes != null);
			AppendCommaSeparatedList(attributeSection.Attributes);
			
			outputFormatter.PrintText(">");
			
			if ("assembly".Equals(attributeSection.AttributeTarget, StringComparison.InvariantCultureIgnoreCase)
			    || "module".Equals(attributeSection.AttributeTarget, StringComparison.InvariantCultureIgnoreCase)) {
				outputFormatter.NewLine();
			} else {
				outputFormatter.PrintLineContinuation();
			}
			
			return null;
		}
		
		public object VisitAttribute(ICSharpCode.NRefactory.Ast.Attribute attribute, object data)
		{
			outputFormatter.PrintIdentifier(attribute.Name);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(attribute.PositionalArguments);
			
			if (attribute.NamedArguments != null && attribute.NamedArguments.Count > 0) {
				if (attribute.PositionalArguments.Count > 0) {
					outputFormatter.PrintToken(Tokens.Comma);
					outputFormatter.Space();
				}
				AppendCommaSeparatedList(attribute.NamedArguments);
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			outputFormatter.PrintIdentifier(namedArgumentExpression.Name);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Colon);
			outputFormatter.PrintToken(Tokens.Assign);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(namedArgumentExpression.Expression, data);
			return null;
		}
		
		public object VisitUsing(Using @using, object data)
		{
			Debug.Fail("Should never be called. The usings should be handled in Visit(UsingDeclaration)");
			return null;
		}
		
		public object VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Imports);
			outputFormatter.Space();
			for (int i = 0; i < usingDeclaration.Usings.Count; ++i) {
				outputFormatter.PrintIdentifier(((Using)usingDeclaration.Usings[i]).Name);
				if (((Using)usingDeclaration.Usings[i]).IsAlias) {
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Assign);
					outputFormatter.Space();
					printFullSystemType = true;
					nodeTracker.TrackedVisit(((Using)usingDeclaration.Usings[i]).Alias, data);
					printFullSystemType = false;
				}
				if (i + 1 < usingDeclaration.Usings.Count) {
					outputFormatter.PrintToken(Tokens.Comma);
					outputFormatter.Space();
				}
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Namespace);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(namespaceDeclaration.Name);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisitChildren(namespaceDeclaration, data);
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Namespace);
			outputFormatter.NewLine();
			return null;
		}
		
		static int GetTypeToken(TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.Type) {
				case ClassType.Class:
					return Tokens.Class;
				case ClassType.Enum:
					return Tokens.Enum;
				case ClassType.Interface:
					return Tokens.Interface;
				case ClassType.Struct:
					return Tokens.Structure;
				default:
					return Tokens.Class;
			}
		}
		
		void PrintTemplates(List<TemplateDefinition> templates)
		{
			if (templates != null && templates.Count > 0) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				outputFormatter.PrintToken(Tokens.Of);
				outputFormatter.Space();
				AppendCommaSeparatedList(templates);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
		}
		
		public object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			VisitAttributes(typeDeclaration.Attributes, data);
			
			outputFormatter.Indent();
			OutputModifier(typeDeclaration.Modifier, true);
			
			int typeToken = GetTypeToken(typeDeclaration);
			outputFormatter.PrintToken(typeToken);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(typeDeclaration.Name);
			
			PrintTemplates(typeDeclaration.Templates);
			
			if (typeDeclaration.Type == ClassType.Enum
			    && typeDeclaration.BaseTypes != null && typeDeclaration.BaseTypes.Count > 0)
			{
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				foreach (TypeReference baseTypeRef in typeDeclaration.BaseTypes) {
					nodeTracker.TrackedVisit(baseTypeRef, data);
				}
			}
			
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			
			if (typeDeclaration.BaseTypes != null && typeDeclaration.Type != ClassType.Enum) {
				foreach (TypeReference baseTypeRef in typeDeclaration.BaseTypes) {
					outputFormatter.Indent();
					
					string baseType = baseTypeRef.Type;
					if (baseType.IndexOf('.') >= 0) {
						baseType = baseType.Substring(baseType.LastIndexOf('.') + 1);
					}
					bool baseTypeIsInterface = baseType.Length >= 2 && baseType[0] == 'I' && Char.IsUpper(baseType[1]);
					
					if (!baseTypeIsInterface || typeDeclaration.Type == ClassType.Interface) {
						outputFormatter.PrintToken(Tokens.Inherits);
					} else {
						outputFormatter.PrintToken(Tokens.Implements);
					}
					outputFormatter.Space();
					nodeTracker.TrackedVisit(baseTypeRef, data);
					outputFormatter.NewLine();
				}
			}
			
			TypeDeclaration oldType = currentType;
			currentType = typeDeclaration;
			
			if (typeDeclaration.Type == ClassType.Enum) {
				OutputEnumMembers(typeDeclaration, data);
			} else {
				nodeTracker.TrackedVisitChildren(typeDeclaration, data);
			}
			currentType = oldType;
			
			--outputFormatter.IndentationLevel;
			
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(typeToken);
			outputFormatter.NewLine();
			return null;
		}
		
		void OutputEnumMembers(TypeDeclaration typeDeclaration, object data)
		{
			foreach (FieldDeclaration fieldDeclaration in typeDeclaration.Children) {
				nodeTracker.BeginNode(fieldDeclaration);
				VariableDeclaration f = (VariableDeclaration)fieldDeclaration.Fields[0];
				VisitAttributes(fieldDeclaration.Attributes, data);
				outputFormatter.Indent();
				outputFormatter.PrintIdentifier(f.Name);
				if (f.Initializer != null && !f.Initializer.IsNull) {
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Assign);
					outputFormatter.Space();
					nodeTracker.TrackedVisit(f.Initializer, data);
				}
				outputFormatter.NewLine();
				nodeTracker.EndNode(fieldDeclaration);
			}
		}
		
		public object VisitTemplateDefinition(TemplateDefinition templateDefinition, object data)
		{
			outputFormatter.PrintIdentifier(templateDefinition.Name);
			if (templateDefinition.Bases.Count > 0) {
				outputFormatter.PrintText(" As ");
				if (templateDefinition.Bases.Count == 1) {
					nodeTracker.TrackedVisit(templateDefinition.Bases[0], data);
				} else {
					outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
					AppendCommaSeparatedList(templateDefinition.Bases);
					outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
				}
			}
			return null;
		}
		
		public object VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			VisitAttributes(delegateDeclaration.Attributes, data);
			
			outputFormatter.Indent();
			OutputModifier(delegateDeclaration.Modifier, true);
			outputFormatter.PrintToken(Tokens.Delegate);
			outputFormatter.Space();
			
			bool isFunction = (delegateDeclaration.ReturnType.Type != "void");
			if (isFunction) {
				outputFormatter.PrintToken(Tokens.Function);
				outputFormatter.Space();
			} else {
				outputFormatter.PrintToken(Tokens.Sub);
				outputFormatter.Space();
			}
			outputFormatter.PrintIdentifier(delegateDeclaration.Name);
			
			PrintTemplates(delegateDeclaration.Templates);
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(delegateDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			if (isFunction) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(delegateDeclaration.ReturnType, data);
			}
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitOptionDeclaration(OptionDeclaration optionDeclaration, object data)
		{
			outputFormatter.PrintToken(Tokens.Option);
			outputFormatter.Space();
			switch (optionDeclaration.OptionType) {
				case OptionType.Strict:
					outputFormatter.PrintToken(Tokens.Strict);
					if (!optionDeclaration.OptionValue) {
						outputFormatter.Space();
						outputFormatter.PrintToken(Tokens.Off);
					}
					break;
				case OptionType.Explicit:
					outputFormatter.PrintToken(Tokens.Explicit);
					outputFormatter.Space();
					if (!optionDeclaration.OptionValue) {
						outputFormatter.Space();
						outputFormatter.PrintToken(Tokens.Off);
					}
					break;
				case OptionType.CompareBinary:
					outputFormatter.PrintToken(Tokens.Compare);
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Binary);
					break;
				case OptionType.CompareText:
					outputFormatter.PrintToken(Tokens.Compare);
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.Text);
					break;
			}
			outputFormatter.NewLine();
			return null;
		}
		#endregion
		
		#region Type level
		TypeReference currentVariableType;
		public object VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			
			VisitAttributes(fieldDeclaration.Attributes, data);
			outputFormatter.Indent();
			if (fieldDeclaration.Modifier == Modifiers.None) {
				outputFormatter.PrintToken(Tokens.Private);
				outputFormatter.Space();
			} else if (fieldDeclaration.Modifier == Modifiers.Dim) {
				outputFormatter.PrintToken(Tokens.Dim);
				outputFormatter.Space();
			} else {
				OutputModifier(fieldDeclaration.Modifier);
			}
			currentVariableType = fieldDeclaration.TypeReference;
			AppendCommaSeparatedList(fieldDeclaration.Fields);
			currentVariableType = null;
			
			outputFormatter.NewLine();

			return null;
		}
		
		public object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			outputFormatter.PrintIdentifier(variableDeclaration.Name);
			
			TypeReference varType = currentVariableType;
			if (varType != null && varType.IsNull)
				varType = null;
			if (varType == null && !variableDeclaration.TypeReference.IsNull)
				varType = variableDeclaration.TypeReference;
			
			if (varType != null) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				ObjectCreateExpression init = variableDeclaration.Initializer as ObjectCreateExpression;
				if (init != null && TypeReference.AreEqualReferences(init.CreateType, varType)) {
					nodeTracker.TrackedVisit(variableDeclaration.Initializer, data);
					return null;
				} else {
					nodeTracker.TrackedVisit(varType, data);
				}
			}
			
			if (!variableDeclaration.Initializer.IsNull) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Assign);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(variableDeclaration.Initializer, data);
			}
			return null;
		}
		
		public object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			VisitAttributes(propertyDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(propertyDeclaration.Modifier);
			
			if ((propertyDeclaration.Modifier & (Modifiers.ReadOnly | Modifiers.WriteOnly)) == Modifiers.None) {
				if (propertyDeclaration.IsReadOnly) {
					outputFormatter.PrintToken(Tokens.ReadOnly);
					outputFormatter.Space();
				} else if (propertyDeclaration.IsWriteOnly) {
					outputFormatter.PrintToken(Tokens.WriteOnly);
					outputFormatter.Space();
				}
			}
			
			outputFormatter.PrintToken(Tokens.Property);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(propertyDeclaration.Name);
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(propertyDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.As);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(propertyDeclaration.TypeReference, data);
			
			PrintInterfaceImplementations(propertyDeclaration.InterfaceImplementations);
			
			outputFormatter.NewLine();
			
			if (!IsAbstract(propertyDeclaration)) {
				++outputFormatter.IndentationLevel;
				exitTokenStack.Push(Tokens.Property);
				nodeTracker.TrackedVisit(propertyDeclaration.GetRegion, data);
				nodeTracker.TrackedVisit(propertyDeclaration.SetRegion, data);
				exitTokenStack.Pop();
				--outputFormatter.IndentationLevel;
				
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.End);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Property);
				outputFormatter.NewLine();
			}
			
			return null;
		}
		
		public object VisitPropertyGetRegion(PropertyGetRegion propertyGetRegion, object data)
		{
			VisitAttributes(propertyGetRegion.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(propertyGetRegion.Modifier);
			outputFormatter.PrintToken(Tokens.Get);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(propertyGetRegion.Block, data);
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Get);
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitPropertySetRegion(PropertySetRegion propertySetRegion, object data)
		{
			VisitAttributes(propertySetRegion.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(propertySetRegion.Modifier);
			outputFormatter.PrintToken(Tokens.Set);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(propertySetRegion.Block, data);
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Set);
			outputFormatter.NewLine();
			return null;
		}
		
		TypeReference currentEventType = null;
		public object VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			bool customEvent = eventDeclaration.HasAddRegion  || eventDeclaration.HasRemoveRegion;
			
			VisitAttributes(eventDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(eventDeclaration.Modifier);
			if (customEvent) {
				outputFormatter.PrintText("Custom");
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(Tokens.Event);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(eventDeclaration.Name);
			
			if (eventDeclaration.Parameters.Count > 0) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				this.AppendCommaSeparatedList(eventDeclaration.Parameters);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.As);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(eventDeclaration.TypeReference, data);
			
			PrintInterfaceImplementations(eventDeclaration.InterfaceImplementations);
			
			if (!eventDeclaration.Initializer.IsNull) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Assign);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(eventDeclaration.Initializer, data);
			}
			
			outputFormatter.NewLine();
			
			if (customEvent) {
				++outputFormatter.IndentationLevel;
				currentEventType = eventDeclaration.TypeReference;
				exitTokenStack.Push(Tokens.Sub);
				nodeTracker.TrackedVisit(eventDeclaration.AddRegion, data);
				nodeTracker.TrackedVisit(eventDeclaration.RemoveRegion, data);
				exitTokenStack.Pop();
				--outputFormatter.IndentationLevel;
				
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.End);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Event);
				outputFormatter.NewLine();
			}
			return null;
		}
		
		void PrintInterfaceImplementations(IList<InterfaceImplementation> list)
		{
			if (list == null || list.Count == 0)
				return;
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Implements);
			for (int i = 0; i < list.Count; i++) {
				if (i > 0)
					outputFormatter.PrintToken(Tokens.Comma);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(list[i].InterfaceType, null);
				outputFormatter.PrintToken(Tokens.Dot);
				outputFormatter.PrintIdentifier(list[i].MemberName);
			}
		}
		
		public object VisitEventAddRegion(EventAddRegion eventAddRegion, object data)
		{
			VisitAttributes(eventAddRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintText("AddHandler(");
			if (eventAddRegion.Parameters.Count == 0) {
				outputFormatter.PrintToken(Tokens.ByVal);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier("value");
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(currentEventType, data);
			} else {
				this.AppendCommaSeparatedList(eventAddRegion.Parameters);
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(eventAddRegion.Block, data);
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintText("AddHandler");
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitEventRemoveRegion(EventRemoveRegion eventRemoveRegion, object data)
		{
			VisitAttributes(eventRemoveRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintText("RemoveHandler");
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (eventRemoveRegion.Parameters.Count == 0) {
				outputFormatter.PrintToken(Tokens.ByVal);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier("value");
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(currentEventType, data);
			} else {
				this.AppendCommaSeparatedList(eventRemoveRegion.Parameters);
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(eventRemoveRegion.Block, data);
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintText("RemoveHandler");
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitEventRaiseRegion(EventRaiseRegion eventRaiseRegion, object data)
		{
			VisitAttributes(eventRaiseRegion.Attributes, data);
			outputFormatter.Indent();
			outputFormatter.PrintText("RaiseEvent");
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			if (eventRaiseRegion.Parameters.Count == 0) {
				outputFormatter.PrintToken(Tokens.ByVal);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier("value");
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(currentEventType, data);
			} else {
				this.AppendCommaSeparatedList(eventRaiseRegion.Parameters);
			}
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(eventRaiseRegion.Block, data);
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintText("RaiseEvent");
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			VisitAttributes(parameterDeclarationExpression.Attributes, data);
			OutputModifier(parameterDeclarationExpression.ParamModifier, parameterDeclarationExpression.StartLocation);
			outputFormatter.PrintIdentifier(parameterDeclarationExpression.ParameterName);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.As);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(parameterDeclarationExpression.TypeReference, data);
			return null;
		}
		
		public object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			VisitAttributes(methodDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(methodDeclaration.Modifier);
			
			bool isSub = methodDeclaration.TypeReference.IsNull ||
				methodDeclaration.TypeReference.SystemType == "System.Void";
			
			if (isSub) {
				outputFormatter.PrintToken(Tokens.Sub);
			} else {
				outputFormatter.PrintToken(Tokens.Function);
			}
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(methodDeclaration.Name);
			
			PrintTemplates(methodDeclaration.Templates);
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(methodDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			if (!isSub) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(methodDeclaration.TypeReference, data);
			}
			
			PrintInterfaceImplementations(methodDeclaration.InterfaceImplementations);
			
			outputFormatter.NewLine();
			
			if (!IsAbstract(methodDeclaration)) {
				nodeTracker.BeginNode(methodDeclaration.Body);
				++outputFormatter.IndentationLevel;
				exitTokenStack.Push(isSub ? Tokens.Sub : Tokens.Function);
				methodDeclaration.Body.AcceptVisitor(this, data);
				exitTokenStack.Pop();
				--outputFormatter.IndentationLevel;
				
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.End);
				outputFormatter.Space();
				if (isSub) {
					outputFormatter.PrintToken(Tokens.Sub);
				} else {
					outputFormatter.PrintToken(Tokens.Function);
				}
				outputFormatter.NewLine();
				nodeTracker.EndNode(methodDeclaration.Body);
			}
			return null;
		}
		
		public object VisitInterfaceImplementation(InterfaceImplementation interfaceImplementation, object data)
		{
			throw new InvalidOperationException();
		}
		
		bool IsAbstract(AttributedNode node)
		{
			if ((node.Modifier & Modifiers.Abstract) == Modifiers.Abstract)
				return true;
			return currentType != null && currentType.Type == ClassType.Interface;
		}
		
		public object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			VisitAttributes(constructorDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(constructorDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Sub);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.New);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(constructorDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			exitTokenStack.Push(Tokens.Sub);
			
			nodeTracker.TrackedVisit(constructorDeclaration.ConstructorInitializer, data);
			
			nodeTracker.TrackedVisit(constructorDeclaration.Body, data);
			exitTokenStack.Pop();
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Sub);
			outputFormatter.NewLine();
			
			return null;
		}
		
		public object VisitConstructorInitializer(ConstructorInitializer constructorInitializer, object data)
		{
			outputFormatter.Indent();
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This) {
				outputFormatter.PrintToken(Tokens.Me);
			} else {
				outputFormatter.PrintToken(Tokens.MyBase);
			}
			outputFormatter.PrintToken(Tokens.Dot);
			outputFormatter.PrintToken(Tokens.New);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(constructorInitializer.Arguments);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			VisitAttributes(indexerDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(indexerDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Default);
			outputFormatter.Space();
			if (indexerDeclaration.IsReadOnly) {
				outputFormatter.PrintToken(Tokens.ReadOnly);
				outputFormatter.Space();
			} else if (indexerDeclaration.IsWriteOnly) {
				outputFormatter.PrintToken(Tokens.WriteOnly);
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(Tokens.Property);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier("Item");
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(indexerDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.As);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(indexerDeclaration.TypeReference, data);
			PrintInterfaceImplementations(indexerDeclaration.InterfaceImplementations);
			
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			exitTokenStack.Push(Tokens.Property);
			nodeTracker.TrackedVisit(indexerDeclaration.GetRegion, data);
			nodeTracker.TrackedVisit(indexerDeclaration.SetRegion, data);
			exitTokenStack.Pop();
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Property);
			outputFormatter.NewLine();
			return null;
		}
		
		public object VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintText("Protected Overrides Sub Finalize()");
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			exitTokenStack.Push(Tokens.Sub);
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Try);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(destructorDeclaration.Body, data);
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Finally);
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintText("MyBase.Finalize()");
			outputFormatter.NewLine();
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Try);
			outputFormatter.NewLine();
			
			exitTokenStack.Pop();
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Sub);
			outputFormatter.NewLine();
			
			return null;
		}
		
		public object VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			VisitAttributes(operatorDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(operatorDeclaration.Modifier);
			
			if (operatorDeclaration.IsConversionOperator) {
				if (operatorDeclaration.ConversionType == ConversionType.Implicit) {
					outputFormatter.PrintToken(Tokens.Widening);
				} else {
					outputFormatter.PrintToken(Tokens.Narrowing);
				}
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(Tokens.Operator);
			outputFormatter.Space();
			
			int op = -1;
			
			switch(operatorDeclaration.OverloadableOperator)
			{
				case OverloadableOperatorType.Add:
					op = Tokens.Plus;
					break;
				case OverloadableOperatorType.Subtract:
					op = Tokens.Minus;
					break;
				case OverloadableOperatorType.Multiply:
					op = Tokens.Times;
					break;
				case OverloadableOperatorType.Divide:
					op = Tokens.Div;
					break;
				case OverloadableOperatorType.Modulus:
					op = Tokens.Mod;
					break;
				case OverloadableOperatorType.Concat:
					op = Tokens.ConcatString;
					break;
				case OverloadableOperatorType.Not:
					op = Tokens.Not;
					break;
				case OverloadableOperatorType.BitNot:
					op = Tokens.Not;
					break;
				case OverloadableOperatorType.BitwiseAnd:
					op = Tokens.And;
					break;
				case OverloadableOperatorType.BitwiseOr:
					op = Tokens.Or;
					break;
				case OverloadableOperatorType.ExclusiveOr:
					op = Tokens.Xor;
					break;
				case OverloadableOperatorType.ShiftLeft:
					op = Tokens.ShiftLeft;
					break;
				case OverloadableOperatorType.ShiftRight:
					op = Tokens.ShiftRight;
					break;
				case OverloadableOperatorType.GreaterThan:
					op = Tokens.GreaterThan;
					break;
				case OverloadableOperatorType.GreaterThanOrEqual:
					op = Tokens.GreaterEqual;
					break;
				case OverloadableOperatorType.Equality:
					op = Tokens.Assign;
					break;
				case OverloadableOperatorType.InEquality:
					op = Tokens.NotEqual;
					break;
				case OverloadableOperatorType.LessThan:
					op = Tokens.LessThan;
					break;
				case OverloadableOperatorType.LessThanOrEqual:
					op = Tokens.LessEqual;
					break;
				case OverloadableOperatorType.Increment:
					Error("Increment operator is not supported in Visual Basic", operatorDeclaration.StartLocation);
					break;
				case OverloadableOperatorType.Decrement:
					Error("Decrement operator is not supported in Visual Basic", operatorDeclaration.StartLocation);
					break;
				case OverloadableOperatorType.IsTrue:
					outputFormatter.PrintText("IsTrue");
					break;
				case OverloadableOperatorType.IsFalse:
					outputFormatter.PrintText("IsFalse");
					break;
				case OverloadableOperatorType.Like:
					op = Tokens.Like;
					break;
				case OverloadableOperatorType.Power:
					op = Tokens.Power;
					break;
				case OverloadableOperatorType.CType:
					op = Tokens.CType;
					break;
				case OverloadableOperatorType.DivideInteger:
					op = Tokens.DivInteger;
					break;
			}
			
			
			
			if (operatorDeclaration.IsConversionOperator) {
				outputFormatter.PrintToken(Tokens.CType);
			} else {
				if(op != -1)  outputFormatter.PrintToken(op);
			}
			
			PrintTemplates(operatorDeclaration.Templates);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(operatorDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			if (!operatorDeclaration.TypeReference.IsNull) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(operatorDeclaration.TypeReference, data);
			}
			
			outputFormatter.NewLine();
			
			++outputFormatter.IndentationLevel;
			nodeTracker.TrackedVisit(operatorDeclaration.Body, data);
			--outputFormatter.IndentationLevel;
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Operator);
			outputFormatter.NewLine();
			
			return null;
		}
		
		public object VisitDeclareDeclaration(DeclareDeclaration declareDeclaration, object data)
		{
			VisitAttributes(declareDeclaration.Attributes, data);
			outputFormatter.Indent();
			OutputModifier(declareDeclaration.Modifier);
			outputFormatter.PrintToken(Tokens.Declare);
			outputFormatter.Space();
			
			switch (declareDeclaration.Charset) {
				case CharsetModifier.Auto:
					outputFormatter.PrintToken(Tokens.Auto);
					outputFormatter.Space();
					break;
				case CharsetModifier.Unicode:
					outputFormatter.PrintToken(Tokens.Unicode);
					outputFormatter.Space();
					break;
				case CharsetModifier.Ansi:
					outputFormatter.PrintToken(Tokens.Ansi);
					outputFormatter.Space();
					break;
			}
			
			bool isVoid = declareDeclaration.TypeReference.IsNull || declareDeclaration.TypeReference.SystemType == "System.Void";
			if (isVoid) {
				outputFormatter.PrintToken(Tokens.Sub);
			} else {
				outputFormatter.PrintToken(Tokens.Function);
			}
			outputFormatter.Space();
			
			outputFormatter.PrintIdentifier(declareDeclaration.Name);
			
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Lib);
			outputFormatter.Space();
			outputFormatter.PrintText('"' + ConvertString(declareDeclaration.Library) + '"');
			outputFormatter.Space();
			
			if (declareDeclaration.Alias.Length > 0) {
				outputFormatter.PrintToken(Tokens.Alias);
				outputFormatter.Space();
				outputFormatter.PrintText('"' + ConvertString(declareDeclaration.Alias) + '"');
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(declareDeclaration.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			
			if (!isVoid) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(declareDeclaration.TypeReference, data);
			}
			
			outputFormatter.NewLine();
			
			return null;
		}
		#endregion
		
		#region Statements
		public object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			VisitStatementList(blockStatement.Children);
			return null;
		}
		
		void PrintIndentedBlock(Statement stmt)
		{
			outputFormatter.IndentationLevel += 1;
			if (stmt is BlockStatement) {
				nodeTracker.TrackedVisit(stmt, null);
			} else {
				outputFormatter.Indent();
				nodeTracker.TrackedVisit(stmt, null);
				outputFormatter.NewLine();
			}
			outputFormatter.IndentationLevel -= 1;
		}
		
		void PrintIndentedBlock(IEnumerable statements)
		{
			outputFormatter.IndentationLevel += 1;
			VisitStatementList(statements);
			outputFormatter.IndentationLevel -= 1;
		}
		
		void VisitStatementList(IEnumerable statements)
		{
			foreach (Statement stmt in statements) {
				if (stmt is BlockStatement) {
					nodeTracker.TrackedVisit(stmt, null);
				} else {
					outputFormatter.Indent();
					nodeTracker.TrackedVisit(stmt, null);
					outputFormatter.NewLine();
				}
			}
		}
		
		public object VisitAddHandlerStatement(AddHandlerStatement addHandlerStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.AddHandler);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(addHandlerStatement.EventExpression, data);
			outputFormatter.PrintToken(Tokens.Comma);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(addHandlerStatement.HandlerExpression, data);
			return null;
		}
		
		public object VisitRemoveHandlerStatement(RemoveHandlerStatement removeHandlerStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.RemoveHandler);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(removeHandlerStatement.EventExpression, data);
			outputFormatter.PrintToken(Tokens.Comma);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(removeHandlerStatement.HandlerExpression, data);
			return null;
		}
		
		public object VisitRaiseEventStatement(RaiseEventStatement raiseEventStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.RaiseEvent);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(raiseEventStatement.EventName);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(raiseEventStatement.Arguments);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitEraseStatement(EraseStatement eraseStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Erase);
			outputFormatter.Space();
			AppendCommaSeparatedList(eraseStatement.Expressions);
			return null;
		}
		
		public object VisitErrorStatement(ErrorStatement errorStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Error);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(errorStatement.Expression, data);
			return null;
		}
		
		public object VisitOnErrorStatement(OnErrorStatement onErrorStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.On);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Error);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(onErrorStatement.EmbeddedStatement, data);
			return null;
		}
		
		public object VisitReDimStatement(ReDimStatement reDimStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.ReDim);
			outputFormatter.Space();
			if (reDimStatement.IsPreserve) {
				outputFormatter.PrintToken(Tokens.Preserve);
				outputFormatter.Space();
			}
			
			AppendCommaSeparatedList(reDimStatement.ReDimClauses);
			return null;
		}
		
		public object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			nodeTracker.TrackedVisit(expressionStatement.Expression, data);
			return null;
		}
		
		public object VisitLocalVariableDeclaration(LocalVariableDeclaration localVariableDeclaration, object data)
		{
			if (localVariableDeclaration.Modifier != Modifiers.None) {
				OutputModifier(localVariableDeclaration.Modifier);
			}
			if (!isUsingResourceAcquisition) {
				if ((localVariableDeclaration.Modifier & Modifiers.Const) == 0) {
					outputFormatter.PrintToken(Tokens.Dim);
				}
				outputFormatter.Space();
			}
			currentVariableType = localVariableDeclaration.TypeReference;
			
			AppendCommaSeparatedList(localVariableDeclaration.Variables);
			currentVariableType = null;
			
			return null;
		}
		
		public object VisitEmptyStatement(EmptyStatement emptyStatement, object data)
		{
			outputFormatter.NewLine();
			return null;
		}
		
		public virtual object VisitYieldStatement(YieldStatement yieldStatement, object data)
		{
			UnsupportedNode(yieldStatement);
			return null;
		}
		
		public object VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Return);
			if (!returnStatement.Expression.IsNull) {
				outputFormatter.Space();
				nodeTracker.TrackedVisit(returnStatement.Expression, data);
			}
			return null;
		}
		
		public object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.If);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(ifElseStatement.Condition, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Then);
			outputFormatter.NewLine();
			
			PrintIndentedBlock(ifElseStatement.TrueStatement);
			
			foreach (ElseIfSection elseIfSection in ifElseStatement.ElseIfSections) {
				nodeTracker.TrackedVisit(elseIfSection, data);
			}
			
			if (ifElseStatement.HasElseStatements) {
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.Else);
				outputFormatter.NewLine();
				PrintIndentedBlock(ifElseStatement.FalseStatement);
			}
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.If);
			return null;
		}
		
		public object VisitElseIfSection(ElseIfSection elseIfSection, object data)
		{
			outputFormatter.PrintToken(Tokens.ElseIf);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(elseIfSection.Condition, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Then);
			outputFormatter.NewLine();
			PrintIndentedBlock(elseIfSection.EmbeddedStatement);
			return null;
		}
		
		public object VisitForStatement(ForStatement forStatement, object data)
		{
			// Is converted to {initializer} while <Condition> {Embedded} {Iterators} end while
			exitTokenStack.Push(Tokens.While);
			bool isFirstLine = true;
			foreach (INode node in forStatement.Initializers) {
				if (!isFirstLine)
					outputFormatter.Indent();
				isFirstLine = false;
				nodeTracker.TrackedVisit(node, data);
				outputFormatter.NewLine();
			}
			if (!isFirstLine)
				outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.While);
			outputFormatter.Space();
			if (forStatement.Condition.IsNull) {
				outputFormatter.PrintToken(Tokens.True);
			} else {
				nodeTracker.TrackedVisit(forStatement.Condition, data);
			}
			outputFormatter.NewLine();
			
			PrintIndentedBlock(forStatement.EmbeddedStatement);
			PrintIndentedBlock(forStatement.Iterator);
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.While);
			exitTokenStack.Pop();
			return null;
		}
		
		public object VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			outputFormatter.PrintIdentifier(labelStatement.Label);
			outputFormatter.PrintToken(Tokens.Colon);
			return null;
		}
		
		public object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.GoTo);
			outputFormatter.Space();
			outputFormatter.PrintIdentifier(gotoStatement.Label);
			return null;
		}
		
		public object VisitSwitchStatement(SwitchStatement switchStatement, object data)
		{
			exitTokenStack.Push(Tokens.Select);
			outputFormatter.PrintToken(Tokens.Select);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Case);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(switchStatement.SwitchExpression, data);
			outputFormatter.NewLine();
			++outputFormatter.IndentationLevel;
			foreach (SwitchSection section in switchStatement.SwitchSections) {
				nodeTracker.TrackedVisit(section, data);
			}
			--outputFormatter.IndentationLevel;
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Select);
			exitTokenStack.Pop();
			return null;
		}
		
		public object VisitSwitchSection(SwitchSection switchSection, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Case);
			outputFormatter.Space();
			this.AppendCommaSeparatedList(switchSection.SwitchLabels);
			outputFormatter.NewLine();
			
			PrintIndentedBlock(switchSection.Children);
			
			return null;
		}
		
		public object VisitCaseLabel(CaseLabel caseLabel, object data)
		{
			if (caseLabel.IsDefault) {
				outputFormatter.PrintToken(Tokens.Else);
			} else {
				if (caseLabel.BinaryOperatorType != BinaryOperatorType.None) {
					switch (caseLabel.BinaryOperatorType) {
						case BinaryOperatorType.Equality:
							outputFormatter.PrintToken(Tokens.Assign);
							break;
						case BinaryOperatorType.InEquality:
							outputFormatter.PrintToken(Tokens.LessThan);
							outputFormatter.PrintToken(Tokens.GreaterThan);
							break;
							
						case BinaryOperatorType.GreaterThan:
							outputFormatter.PrintToken(Tokens.GreaterThan);
							break;
						case BinaryOperatorType.GreaterThanOrEqual:
							outputFormatter.PrintToken(Tokens.GreaterEqual);
							break;
						case BinaryOperatorType.LessThan:
							outputFormatter.PrintToken(Tokens.LessThan);
							break;
						case BinaryOperatorType.LessThanOrEqual:
							outputFormatter.PrintToken(Tokens.LessEqual);
							break;
					}
					outputFormatter.Space();
				}
				
				nodeTracker.TrackedVisit(caseLabel.Label, data);
				if (!caseLabel.ToExpression.IsNull) {
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.To);
					outputFormatter.Space();
					nodeTracker.TrackedVisit(caseLabel.ToExpression, data);
				}
			}
			
			return null;
		}
		
		public object VisitBreakStatement(BreakStatement breakStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Exit);
			if (exitTokenStack.Count > 0) {
				outputFormatter.Space();
				outputFormatter.PrintToken(exitTokenStack.Peek());
			}
			return null;
		}
		
		public object VisitStopStatement(StopStatement stopStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Stop);
			return null;
		}
		
		public object VisitResumeStatement(ResumeStatement resumeStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Resume);
			outputFormatter.Space();
			if (resumeStatement.IsResumeNext) {
				outputFormatter.PrintToken(Tokens.Next);
			} else {
				outputFormatter.PrintIdentifier(resumeStatement.LabelName);
			}
			return null;
		}
		
		public object VisitEndStatement(EndStatement endStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.End);
			return null;
		}
		
		public object VisitContinueStatement(ContinueStatement continueStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Continue);
			outputFormatter.Space();
			switch (continueStatement.ContinueType) {
				case ContinueType.Do:
					outputFormatter.PrintToken(Tokens.Do);
					break;
				case ContinueType.For:
					outputFormatter.PrintToken(Tokens.For);
					break;
				case ContinueType.While:
					outputFormatter.PrintToken(Tokens.While);
					break;
				default:
					outputFormatter.PrintToken(exitTokenStack.Peek());
					break;
			}
			return null;
		}
		
		public object VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, object data)
		{
			outputFormatter.PrintText("goto case ");
			if (gotoCaseStatement.IsDefaultCase) {
				outputFormatter.PrintText("default");
			} else {
				nodeTracker.TrackedVisit(gotoCaseStatement.Expression, null);
			}
			return null;
		}
		
		public object VisitDoLoopStatement(DoLoopStatement doLoopStatement, object data)
		{
			if (doLoopStatement.ConditionPosition == ConditionPosition.None) {
				Error(String.Format("Unknown condition position for loop : {0}.", doLoopStatement), doLoopStatement.StartLocation);
			}
			
			if (doLoopStatement.ConditionPosition == ConditionPosition.Start) {
				switch (doLoopStatement.ConditionType) {
					case ConditionType.DoWhile:
						exitTokenStack.Push(Tokens.Do);
						outputFormatter.PrintToken(Tokens.Do);
						outputFormatter.Space();
						outputFormatter.PrintToken(Tokens.While);
						break;
					case ConditionType.While:
						exitTokenStack.Push(Tokens.While);
						outputFormatter.PrintToken(Tokens.While);
						break;
					case ConditionType.Until:
						exitTokenStack.Push(Tokens.Do);
						outputFormatter.PrintToken(Tokens.Do);
						outputFormatter.Space();
						outputFormatter.PrintToken(Tokens.While);
						break;
					default:
						throw new InvalidOperationException();
				}
				outputFormatter.Space();
				nodeTracker.TrackedVisit(doLoopStatement.Condition, null);
			} else {
				exitTokenStack.Push(Tokens.Do);
				outputFormatter.PrintToken(Tokens.Do);
			}
			
			outputFormatter.NewLine();
			
			PrintIndentedBlock(doLoopStatement.EmbeddedStatement);
			
			outputFormatter.Indent();
			if (doLoopStatement.ConditionPosition == ConditionPosition.Start && doLoopStatement.ConditionType == ConditionType.While) {
				outputFormatter.PrintToken(Tokens.End);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.While);
			} else {
				outputFormatter.PrintToken(Tokens.Loop);
			}
			
			if (doLoopStatement.ConditionPosition == ConditionPosition.End && !doLoopStatement.Condition.IsNull) {
				outputFormatter.Space();
				switch (doLoopStatement.ConditionType) {
					case ConditionType.While:
					case ConditionType.DoWhile:
						outputFormatter.PrintToken(Tokens.While);
						break;
					case ConditionType.Until:
						outputFormatter.PrintToken(Tokens.Until);
						break;
				}
				outputFormatter.Space();
				nodeTracker.TrackedVisit(doLoopStatement.Condition, null);
			}
			exitTokenStack.Pop();
			return null;
		}
		
		public object VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			exitTokenStack.Push(Tokens.For);
			outputFormatter.PrintToken(Tokens.For);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Each);
			outputFormatter.Space();
			
			// loop control variable
			outputFormatter.PrintIdentifier(foreachStatement.VariableName);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.As);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(foreachStatement.TypeReference, data);
			
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.In);
			outputFormatter.Space();
			
			nodeTracker.TrackedVisit(foreachStatement.Expression, data);
			outputFormatter.NewLine();
			
			PrintIndentedBlock(foreachStatement.EmbeddedStatement);
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Next);
			if (!foreachStatement.NextExpression.IsNull) {
				outputFormatter.Space();
				nodeTracker.TrackedVisit(foreachStatement.NextExpression, data);
			}
			exitTokenStack.Pop();
			return null;
		}
		
		public object VisitLockStatement(LockStatement lockStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.SyncLock);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(lockStatement.LockExpression, data);
			outputFormatter.NewLine();
			
			PrintIndentedBlock(lockStatement.EmbeddedStatement);
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.SyncLock);
			return null;
		}
		
		bool isUsingResourceAcquisition;
		
		public object VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Using);
			outputFormatter.Space();
			
			isUsingResourceAcquisition = true;
			nodeTracker.TrackedVisit(usingStatement.ResourceAcquisition, data);
			isUsingResourceAcquisition = false;
			outputFormatter.NewLine();
			
			PrintIndentedBlock(usingStatement.EmbeddedStatement);
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Using);
			
			return null;
		}
		
		public object VisitWithStatement(WithStatement withStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.With);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(withStatement.Expression, data);
			outputFormatter.NewLine();
			
			PrintIndentedBlock(withStatement.Body);
			
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.With);
			return null;
		}
		
		public object VisitTryCatchStatement(TryCatchStatement tryCatchStatement, object data)
		{
			exitTokenStack.Push(Tokens.Try);
			outputFormatter.PrintToken(Tokens.Try);
			outputFormatter.NewLine();
			
			PrintIndentedBlock(tryCatchStatement.StatementBlock);
			
			foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
				nodeTracker.TrackedVisit(catchClause, data);
			}
			
			if (!tryCatchStatement.FinallyBlock.IsNull) {
				outputFormatter.Indent();
				outputFormatter.PrintToken(Tokens.Finally);
				outputFormatter.NewLine();
				PrintIndentedBlock(tryCatchStatement.FinallyBlock);
			}
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.End);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Try);
			exitTokenStack.Pop();
			return null;
		}
		
		public object VisitCatchClause(CatchClause catchClause, object data)
		{
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Catch);
			
			if (!catchClause.TypeReference.IsNull) {
				outputFormatter.Space();
				if (catchClause.VariableName.Length > 0) {
					outputFormatter.PrintIdentifier(catchClause.VariableName);
				} else {
					outputFormatter.PrintIdentifier("generatedExceptionName");
				}
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				outputFormatter.PrintIdentifier(catchClause.TypeReference.Type);
			}
			
			if (!catchClause.Condition.IsNull)  {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.When);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(catchClause.Condition, data);
			}
			outputFormatter.NewLine();
			
			PrintIndentedBlock(catchClause.StatementBlock);
			
			return null;
		}
		
		public object VisitThrowStatement(ThrowStatement throwStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Throw);
			if (!throwStatement.Expression.IsNull) {
				outputFormatter.Space();
				nodeTracker.TrackedVisit(throwStatement.Expression, data);
			}
			return null;
		}
		
		public object VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			UnsupportedNode(fixedStatement);
			return nodeTracker.TrackedVisit(fixedStatement.EmbeddedStatement, data);
		}
		
		public object VisitUnsafeStatement(UnsafeStatement unsafeStatement, object data)
		{
			UnsupportedNode(unsafeStatement);
			return nodeTracker.TrackedVisit(unsafeStatement.Block, data);
		}
		
		public object VisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			UnsupportedNode(checkedStatement);
			return nodeTracker.TrackedVisit(checkedStatement.Block, data);
		}
		
		public object VisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			UnsupportedNode(uncheckedStatement);
			return nodeTracker.TrackedVisit(uncheckedStatement.Block, data);
		}
		
		public object VisitExitStatement(ExitStatement exitStatement, object data)
		{
			outputFormatter.PrintToken(Tokens.Exit);
			if (exitStatement.ExitType != ExitType.None) {
				outputFormatter.Space();
				switch (exitStatement.ExitType) {
					case ExitType.Sub:
						outputFormatter.PrintToken(Tokens.Sub);
						break;
					case ExitType.Function:
						outputFormatter.PrintToken(Tokens.Function);
						break;
					case ExitType.Property:
						outputFormatter.PrintToken(Tokens.Property);
						break;
					case ExitType.Do:
						outputFormatter.PrintToken(Tokens.Do);
						break;
					case ExitType.For:
						outputFormatter.PrintToken(Tokens.For);
						break;
					case ExitType.Try:
						outputFormatter.PrintToken(Tokens.Try);
						break;
					case ExitType.While:
						outputFormatter.PrintToken(Tokens.While);
						break;
					case ExitType.Select:
						outputFormatter.PrintToken(Tokens.Select);
						break;
					default:
						Error(String.Format("Unsupported exit type : {0}", exitStatement.ExitType), exitStatement.StartLocation);
						break;
				}
			}
			
			return null;
		}
		
		public object VisitForNextStatement(ForNextStatement forNextStatement, object data)
		{
			exitTokenStack.Push(Tokens.For);
			outputFormatter.PrintToken(Tokens.For);
			outputFormatter.Space();
			
			outputFormatter.PrintIdentifier(forNextStatement.VariableName);
			
			if (!forNextStatement.TypeReference.IsNull) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.As);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(forNextStatement.TypeReference, data);
			}
			
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Assign);
			outputFormatter.Space();
			
			nodeTracker.TrackedVisit(forNextStatement.Start, data);
			
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.To);
			outputFormatter.Space();
			
			nodeTracker.TrackedVisit(forNextStatement.End, data);
			
			if (!forNextStatement.Step.IsNull) {
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Step);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(forNextStatement.Step, data);
			}
			outputFormatter.NewLine();
			
			PrintIndentedBlock(forNextStatement.EmbeddedStatement);
			
			outputFormatter.Indent();
			outputFormatter.PrintToken(Tokens.Next);
			
			if (forNextStatement.NextExpressions.Count > 0) {
				outputFormatter.Space();
				AppendCommaSeparatedList(forNextStatement.NextExpressions);
			}
			exitTokenStack.Pop();
			return null;
		}
		#endregion
		
		#region Expressions
		
		public object VisitClassReferenceExpression(ClassReferenceExpression classReferenceExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.MyClass);
			return null;
		}
		
		
		static string ConvertCharLiteral(char ch)
		{
			if (Char.IsControl(ch)) {
				return "Chr(" + ((int)ch) + ")";
			} else {
				if (ch == '"') {
					return "\"\"\"\"C";
				}
				return String.Concat("\"", ch.ToString(), "\"C");
			}
		}
		
		static string ConvertString(string str)
		{
			StringBuilder sb = new StringBuilder();
			foreach (char ch in str) {
				if (char.IsControl(ch)) {
					sb.Append("\" & Chr(" + ((int)ch) + ") & \"");
				} else if (ch == '"') {
					sb.Append("\"\"");
				} else {
					sb.Append(ch);
				}
			}
			return sb.ToString();
		}
		
		public object VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			object val = primitiveExpression.Value;
			if (val == null) {
				outputFormatter.PrintToken(Tokens.Nothing);
				return null;
			}
			if (val is bool) {
				if ((bool)primitiveExpression.Value) {
					outputFormatter.PrintToken(Tokens.True);
				} else {
					outputFormatter.PrintToken(Tokens.False);
				}
				return null;
			}
			
			if (val is string) {
				outputFormatter.PrintText('"' + ConvertString((string)val) + '"');
				return null;
			}
			
			if (val is char) {
				outputFormatter.PrintText(ConvertCharLiteral((char)primitiveExpression.Value));
				return null;
			}

			if (val is decimal) {
				outputFormatter.PrintText(((decimal)primitiveExpression.Value).ToString(NumberFormatInfo.InvariantInfo) + "D");
				return null;
			}
			
			if (val is float) {
				outputFormatter.PrintText(((float)primitiveExpression.Value).ToString(NumberFormatInfo.InvariantInfo) + "F");
				return null;
			}
			
			if (val is IFormattable) {
				outputFormatter.PrintText(((IFormattable)val).ToString(null, NumberFormatInfo.InvariantInfo));
			} else {
				outputFormatter.PrintText(val.ToString());
			}
			
			return null;
		}
		
		public object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			int  op = 0;
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Add:
					op = Tokens.Plus;
					break;
					
				case BinaryOperatorType.Subtract:
					op = Tokens.Minus;
					break;
					
				case BinaryOperatorType.Multiply:
					op = Tokens.Times;
					break;
					
				case BinaryOperatorType.Divide:
					op = Tokens.Div;
					break;
					
				case BinaryOperatorType.Modulus:
					op = Tokens.Mod;
					break;
					
				case BinaryOperatorType.ShiftLeft:
					op = Tokens.ShiftLeft;
					break;
					
				case BinaryOperatorType.ShiftRight:
					op = Tokens.ShiftRight;
					break;
					
				case BinaryOperatorType.BitwiseAnd:
					op = Tokens.And;
					break;
				case BinaryOperatorType.BitwiseOr:
					op = Tokens.Or;
					break;
				case BinaryOperatorType.ExclusiveOr:
					op = Tokens.Xor;
					break;
					
				case BinaryOperatorType.LogicalAnd:
					op = Tokens.AndAlso;
					break;
				case BinaryOperatorType.LogicalOr:
					op = Tokens.OrElse;
					break;
				case BinaryOperatorType.ReferenceEquality:
					op = Tokens.Is;
					break;
				case BinaryOperatorType.ReferenceInequality:
					op = Tokens.IsNot;
					break;
					
				case BinaryOperatorType.Equality:
					op = Tokens.Assign;
					break;
				case BinaryOperatorType.GreaterThan:
					op = Tokens.GreaterThan;
					break;
				case BinaryOperatorType.GreaterThanOrEqual:
					op = Tokens.GreaterEqual;
					break;
				case BinaryOperatorType.InEquality:
					nodeTracker.TrackedVisit(binaryOperatorExpression.Left, data);
					outputFormatter.Space();
					outputFormatter.PrintToken(Tokens.LessThan);
					outputFormatter.PrintToken(Tokens.GreaterThan);
					outputFormatter.Space();
					nodeTracker.TrackedVisit(binaryOperatorExpression.Right, data);
					return null;
				case BinaryOperatorType.NullCoalescing:
					outputFormatter.PrintText("IIf(");
					nodeTracker.TrackedVisit(binaryOperatorExpression.Left, data);
					outputFormatter.PrintText(" Is Nothing, ");
					nodeTracker.TrackedVisit(binaryOperatorExpression.Right, data);
					outputFormatter.PrintToken(Tokens.Comma);
					outputFormatter.Space();
					nodeTracker.TrackedVisit(binaryOperatorExpression.Left, data);
					outputFormatter.PrintToken(Tokens.CloseParenthesis);
					return null;
				case BinaryOperatorType.LessThan:
					op = Tokens.LessThan;
					break;
				case BinaryOperatorType.LessThanOrEqual:
					op = Tokens.LessEqual;
					break;
			}
			
			nodeTracker.TrackedVisit(binaryOperatorExpression.Left, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(op);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(binaryOperatorExpression.Right, data);
			
			return null;
		}
		
		public object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			nodeTracker.TrackedVisit(parenthesizedExpression.Expression, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			nodeTracker.TrackedVisit(invocationExpression.TargetObject, data);
			if (invocationExpression.TypeArguments != null && invocationExpression.TypeArguments.Count > 0) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				outputFormatter.PrintToken(Tokens.Of);
				outputFormatter.Space();
				AppendCommaSeparatedList(invocationExpression.TypeArguments);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(invocationExpression.Arguments);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			outputFormatter.PrintIdentifier(identifierExpression.Identifier);
			return null;
		}
		
		public object VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			nodeTracker.TrackedVisit(typeReferenceExpression.TypeReference, data);
			return null;
		}
		
		public object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.Not:
				case UnaryOperatorType.BitNot:
					outputFormatter.PrintToken(Tokens.Not);
					outputFormatter.Space();
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					return null;
					
				case UnaryOperatorType.Decrement:
					outputFormatter.PrintText("System.Threading.Interlocked.Decrement(");
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintText(")");
					return null;
					
				case UnaryOperatorType.Increment:
					outputFormatter.PrintText("System.Threading.Interlocked.Increment(");
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintText(")");
					return null;
					
				case UnaryOperatorType.Minus:
					outputFormatter.PrintToken(Tokens.Minus);
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					return null;
					
				case UnaryOperatorType.Plus:
					outputFormatter.PrintToken(Tokens.Plus);
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					return null;
					
				case UnaryOperatorType.PostDecrement:
					outputFormatter.PrintText("System.Math.Max(System.Threading.Interlocked.Decrement(");
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintText("),");
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintText(" + 1)");
					return null;
					
				case UnaryOperatorType.PostIncrement:
					outputFormatter.PrintText("System.Math.Max(System.Threading.Interlocked.Increment(");
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintText("),");
					nodeTracker.TrackedVisit(unaryOperatorExpression.Expression, data);
					outputFormatter.PrintText(" - 1)");
					return null;
					
				case UnaryOperatorType.Star:
					outputFormatter.PrintToken(Tokens.Times);
					return null;
				case UnaryOperatorType.BitWiseAnd:
					outputFormatter.PrintToken(Tokens.AddressOf);
					return null;
				default:
					Error("unknown unary operator: " + unaryOperatorExpression.Op.ToString(), unaryOperatorExpression.StartLocation);
					return null;
			}
		}
		
		public object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			int  op = 0;
			bool unsupportedOpAssignment = false;
			switch (assignmentExpression.Op) {
				case AssignmentOperatorType.Assign:
					op = Tokens.Assign;
					break;
				case AssignmentOperatorType.Add:
					op = Tokens.PlusAssign;
					if (IsEventHandlerCreation(assignmentExpression.Right)) {
						outputFormatter.PrintToken(Tokens.AddHandler);
						outputFormatter.Space();
						nodeTracker.TrackedVisit(assignmentExpression.Left, data);
						outputFormatter.PrintToken(Tokens.Comma);
						outputFormatter.Space();
						outputFormatter.PrintToken(Tokens.AddressOf);
						outputFormatter.Space();
						nodeTracker.TrackedVisit(GetEventHandlerMethod(assignmentExpression.Right), data);
						return null;
					}
					break;
				case AssignmentOperatorType.Subtract:
					op = Tokens.MinusAssign;
					if (IsEventHandlerCreation(assignmentExpression.Right)) {
						outputFormatter.PrintToken(Tokens.RemoveHandler);
						outputFormatter.Space();
						nodeTracker.TrackedVisit(assignmentExpression.Left, data);
						outputFormatter.PrintToken(Tokens.Comma);
						outputFormatter.Space();
						outputFormatter.PrintToken(Tokens.AddressOf);
						outputFormatter.Space();
						nodeTracker.TrackedVisit(GetEventHandlerMethod(assignmentExpression.Right), data);
						return null;
					}
					break;
				case AssignmentOperatorType.Multiply:
					op = Tokens.TimesAssign;
					break;
				case AssignmentOperatorType.Divide:
					op = Tokens.DivAssign;
					break;
				case AssignmentOperatorType.ShiftLeft:
					op = Tokens.ShiftLeftAssign;
					break;
				case AssignmentOperatorType.ShiftRight:
					op = Tokens.ShiftRightAssign;
					break;
					
				case AssignmentOperatorType.ExclusiveOr:
					op = Tokens.Xor;
					unsupportedOpAssignment = true;
					break;
				case AssignmentOperatorType.Modulus:
					op = Tokens.Mod;
					unsupportedOpAssignment = true;
					break;
				case AssignmentOperatorType.BitwiseAnd:
					op = Tokens.And;
					unsupportedOpAssignment = true;
					break;
				case AssignmentOperatorType.BitwiseOr:
					op = Tokens.Or;
					unsupportedOpAssignment = true;
					break;
			}
			
			nodeTracker.TrackedVisit(assignmentExpression.Left, data);
			outputFormatter.Space();
			
			if (unsupportedOpAssignment) { // left = left OP right
				outputFormatter.PrintToken(Tokens.Assign);
				outputFormatter.Space();
				nodeTracker.TrackedVisit(assignmentExpression.Left, data);
				outputFormatter.Space();
			}
			
			outputFormatter.PrintToken(op);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(assignmentExpression.Right, data);
			
			return null;
		}
		
		public object VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			UnsupportedNode(sizeOfExpression);
			return null;
		}
		
		public object VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.GetType);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			nodeTracker.TrackedVisit(typeOfExpression.TypeReference, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			// assigning nothing to a generic type in VB compiles to a DefaultValueExpression
			outputFormatter.PrintToken(Tokens.Nothing);
			return null;
		}
		
		public object VisitTypeOfIsExpression(TypeOfIsExpression typeOfIsExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.TypeOf);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(typeOfIsExpression.Expression, data);
			outputFormatter.Space();
			outputFormatter.PrintToken(Tokens.Is);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(typeOfIsExpression.TypeReference, data);
			return null;
		}
		
		public object VisitAddressOfExpression(AddressOfExpression addressOfExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.AddressOf);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(addressOfExpression.Expression, data);
			return null;
		}
		
		public object VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			UnsupportedNode(anonymousMethodExpression);
			return null;
		}
		
		public object VisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			UnsupportedNode(checkedExpression);
			return nodeTracker.TrackedVisit(checkedExpression.Expression, data);
		}
		
		public object VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			UnsupportedNode(uncheckedExpression);
			return nodeTracker.TrackedVisit(uncheckedExpression.Expression, data);
		}
		
		public object VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			UnsupportedNode(pointerReferenceExpression);
			return null;
		}
		
		public object VisitCastExpression(CastExpression castExpression, object data)
		{
			if (castExpression.CastType == CastType.Cast) {
				return PrintCast(Tokens.DirectCast, castExpression);
			}
			if (castExpression.CastType == CastType.TryCast) {
				return PrintCast(Tokens.TryCast, castExpression);
			}
			switch (castExpression.CastTo.SystemType) {
				case "System.Boolean":
					outputFormatter.PrintToken(Tokens.CBool);
					break;
				case "System.Byte":
					outputFormatter.PrintToken(Tokens.CByte);
					break;
				case "System.SByte":
					outputFormatter.PrintToken(Tokens.CSByte);
					break;
				case "System.Char":
					outputFormatter.PrintToken(Tokens.CChar);
					break;
				case "System.DateTime":
					outputFormatter.PrintToken(Tokens.CDate);
					break;
				case "System.Decimal":
					outputFormatter.PrintToken(Tokens.CDec);
					break;
				case "System.Double":
					outputFormatter.PrintToken(Tokens.CDbl);
					break;
				case "System.Int16":
					outputFormatter.PrintToken(Tokens.CShort);
					break;
				case "System.Int32":
					outputFormatter.PrintToken(Tokens.CInt);
					break;
				case "System.Int64":
					outputFormatter.PrintToken(Tokens.CLng);
					break;
				case "System.UInt16":
					outputFormatter.PrintToken(Tokens.CUShort);
					break;
				case "System.UInt32":
					outputFormatter.PrintToken(Tokens.CInt);
					break;
				case "System.UInt64":
					outputFormatter.PrintToken(Tokens.CLng);
					break;
				case "System.Object":
					outputFormatter.PrintToken(Tokens.CObj);
					break;
				case "System.Single":
					outputFormatter.PrintToken(Tokens.CSng);
					break;
				case "System.String":
					outputFormatter.PrintToken(Tokens.CStr);
					break;
				default:
					return PrintCast(Tokens.CType, castExpression);
			}
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			nodeTracker.TrackedVisit(castExpression.Expression, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		object PrintCast(int castToken, CastExpression castExpression)
		{
			outputFormatter.PrintToken(castToken);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			nodeTracker.TrackedVisit(castExpression.Expression, null);
			outputFormatter.PrintToken(Tokens.Comma);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(castExpression.CastTo, null);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			UnsupportedNode(stackAllocExpression);
			return null;
		}
		
		public object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			nodeTracker.TrackedVisit(indexerExpression.TargetObject, data);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(indexerExpression.Indexes);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.Me);
			return null;
		}
		
		public object VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.MyBase);
			return null;
		}
		
		public object VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.New);
			outputFormatter.Space();
			nodeTracker.TrackedVisit(objectCreateExpression.CreateType, data);
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			AppendCommaSeparatedList(objectCreateExpression.Parameters);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		public object VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.New);
			outputFormatter.Space();
			PrintTypeReferenceWithoutArray(arrayCreateExpression.CreateType);
			
			if (arrayCreateExpression.Arguments.Count > 0) {
				outputFormatter.PrintToken(Tokens.OpenParenthesis);
				AppendCommaSeparatedList(arrayCreateExpression.Arguments);
				outputFormatter.PrintToken(Tokens.CloseParenthesis);
				PrintArrayRank(arrayCreateExpression.CreateType.RankSpecifier, 1);
			} else {
				PrintArrayRank(arrayCreateExpression.CreateType.RankSpecifier, 0);
			}
			
			outputFormatter.Space();
			
			if (arrayCreateExpression.ArrayInitializer.IsNull) {
				outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
				outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			} else {
				nodeTracker.TrackedVisit(arrayCreateExpression.ArrayInitializer, data);
			}
			return null;
		}
		
		public object VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			outputFormatter.PrintToken(Tokens.OpenCurlyBrace);
			this.AppendCommaSeparatedList(arrayInitializerExpression.CreateExpressions);
			outputFormatter.PrintToken(Tokens.CloseCurlyBrace);
			return null;
		}
		
		public object VisitFieldReferenceExpression(FieldReferenceExpression fieldReferenceExpression, object data)
		{
			nodeTracker.TrackedVisit(fieldReferenceExpression.TargetObject, data);
			outputFormatter.PrintToken(Tokens.Dot);
			outputFormatter.PrintIdentifier(fieldReferenceExpression.FieldName);
			return null;
		}
		
		public object VisitDirectionExpression(DirectionExpression directionExpression, object data)
		{
			// VB does not need to specify the direction in method calls
			nodeTracker.TrackedVisit(directionExpression.Expression, data);
			return null;
		}
		
		
		public object VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			// No representation in VB.NET, but VB conversion is possible.
			outputFormatter.PrintText("IIf");
			outputFormatter.PrintToken(Tokens.OpenParenthesis);
			nodeTracker.TrackedVisit(conditionalExpression.Condition, data);
			outputFormatter.PrintToken(Tokens.Comma);
			nodeTracker.TrackedVisit(conditionalExpression.TrueExpression, data);
			outputFormatter.PrintToken(Tokens.Comma);
			nodeTracker.TrackedVisit(conditionalExpression.FalseExpression, data);
			outputFormatter.PrintToken(Tokens.CloseParenthesis);
			return null;
		}
		
		#endregion
		#endregion
		
		
		void OutputModifier(ParameterModifiers modifier, Location position)
		{
			switch (modifier) {
				case ParameterModifiers.None:
				case ParameterModifiers.In:
					outputFormatter.PrintToken(Tokens.ByVal);
					break;
				case ParameterModifiers.Out:
					Error("Out parameter converted to ByRef", position);
					outputFormatter.PrintToken(Tokens.ByRef);
					break;
				case ParameterModifiers.Params:
					outputFormatter.PrintToken(Tokens.ParamArray);
					break;
				case ParameterModifiers.Ref:
					outputFormatter.PrintToken(Tokens.ByRef);
					break;
				case ParameterModifiers.Optional:
					outputFormatter.PrintToken(Tokens.Optional);
					break;
				default:
					Error(String.Format("Unsupported modifier : {0}", modifier), position);
					break;
			}
			outputFormatter.Space();
		}
		
		void OutputModifier(Modifiers modifier)
		{
			OutputModifier(modifier, false);
		}
		
		void OutputModifier(Modifiers modifier, bool forTypeDecl)
		{
			if ((modifier & Modifiers.Public) == Modifiers.Public) {
				outputFormatter.PrintToken(Tokens.Public);
				outputFormatter.Space();
			} else if ((modifier & Modifiers.Private) == Modifiers.Private) {
				outputFormatter.PrintToken(Tokens.Private);
				outputFormatter.Space();
			} else if ((modifier & (Modifiers.Protected | Modifiers.Internal)) == (Modifiers.Protected | Modifiers.Internal)) {
				outputFormatter.PrintToken(Tokens.Protected);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Friend);
				outputFormatter.Space();
			} else if ((modifier & Modifiers.Internal) == Modifiers.Internal) {
				outputFormatter.PrintToken(Tokens.Friend);
				outputFormatter.Space();
			} else if ((modifier & Modifiers.Protected) == Modifiers.Protected) {
				outputFormatter.PrintToken(Tokens.Protected);
				outputFormatter.Space();
			}
			
			if ((modifier & Modifiers.Static) == Modifiers.Static) {
				outputFormatter.PrintToken(Tokens.Shared);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.Virtual) == Modifiers.Virtual) {
				outputFormatter.PrintToken(Tokens.Overridable);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.Abstract) == Modifiers.Abstract) {
				outputFormatter.PrintToken(forTypeDecl ? Tokens.MustInherit : Tokens.MustOverride);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.Override) == Modifiers.Override) {
				outputFormatter.PrintToken(Tokens.Overloads);
				outputFormatter.Space();
				outputFormatter.PrintToken(Tokens.Overrides);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.New) == Modifiers.New) {
				outputFormatter.PrintToken(Tokens.Shadows);
				outputFormatter.Space();
			}
			
			if ((modifier & Modifiers.Sealed) == Modifiers.Sealed) {
				outputFormatter.PrintToken(forTypeDecl ? Tokens.NotInheritable : Tokens.NotOverridable);
				outputFormatter.Space();
			}
			
			if ((modifier & Modifiers.ReadOnly) == Modifiers.ReadOnly) {
				outputFormatter.PrintToken(Tokens.ReadOnly);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.WriteOnly) == Modifiers.WriteOnly) {
				outputFormatter.PrintToken(Tokens.WriteOnly);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.Const) == Modifiers.Const) {
				outputFormatter.PrintToken(Tokens.Const);
				outputFormatter.Space();
			}
			if ((modifier & Modifiers.Partial) == Modifiers.Partial) {
				outputFormatter.PrintToken(Tokens.Partial);
				outputFormatter.Space();
			}
			
			if ((modifier & Modifiers.Extern) == Modifiers.Extern) {
				// not required in VB
			}
			
			// TODO : Volatile
			if ((modifier & Modifiers.Volatile) == Modifiers.Volatile) {
				Error("'Volatile' modifier not convertable", Location.Empty);
			}
			
			// TODO : Unsafe
			if ((modifier & Modifiers.Unsafe) == Modifiers.Unsafe) {
				Error("'Unsafe' modifier not convertable", Location.Empty);
			}
		}
		
		public void AppendCommaSeparatedList<T>(ICollection<T> list) where T : class, INode
		{
			if (list != null) {
				int i = 0;
				foreach (T node in list) {
					nodeTracker.TrackedVisit(node, null);
					if (i + 1 < list.Count) {
						outputFormatter.PrintToken(Tokens.Comma);
						outputFormatter.Space();
						if ((i + 1) % 6 == 0) {
							outputFormatter.PrintLineContinuation();
							outputFormatter.Indent();
							outputFormatter.PrintText("\t");
						}
					}
					i++;
				}
			}
		}
		
		void VisitAttributes(ICollection attributes, object data)
		{
			if (attributes == null || attributes.Count <= 0) {
				return;
			}
			foreach (AttributeSection section in attributes) {
				nodeTracker.TrackedVisit(section, data);
			}
		}
		
		
		static bool IsEventHandlerCreation(Expression expr)
		{
			if (expr is ObjectCreateExpression) {
				ObjectCreateExpression oce = (ObjectCreateExpression) expr;
				if (oce.Parameters.Count == 1) {
					return oce.CreateType.SystemType.EndsWith("Handler");
				}
			}
			return false;
		}
		
		// can only get called if IsEventHandlerCreation returned true for the expression
		static Expression GetEventHandlerMethod(Expression expr)
		{
			ObjectCreateExpression oce = (ObjectCreateExpression)expr;
			return oce.Parameters[0];
		}
	}
}
