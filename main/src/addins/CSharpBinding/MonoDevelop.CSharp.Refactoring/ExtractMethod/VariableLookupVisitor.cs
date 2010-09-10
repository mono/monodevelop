// 
// VariableLookupVisitor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextEditor;
using MonoDevelop.CSharp.Dom;

namespace MonoDevelop.CSharp.Refactoring.ExtractMethod
{
	public class VariableDescriptor 
	{
		public string Name {
			get;
			set;
		}
		
		public bool GetsChanged {
			get;
			set;
		}
		
		public bool InitialValueUsed {
			get;
			set;
		}
		
		public bool GetsAssigned {
			get;
			set;
		}
		
		public bool IsDefined {
			get;
			set;
		}
		
		public IReturnType ReturnType {
			get;
			set;
		}
		
		public DocumentLocation Location {
			get;
			set;
		}
		
		public VariableDescriptor (string name)
		{
			this.Name = name;
			this.GetsChanged = this.IsDefined = this.InitialValueUsed = false;
		}
		
		public override string ToString ()
		{
			return string.Format("[VariableDescriptor: Name={0}, GetsChanged={1}, InitialValueUsed={2}, GetsAssigned={3}, IsDefined={4}, ReturnType={5}, Location={6}]", Name, GetsChanged, InitialValueUsed, GetsAssigned, IsDefined, ReturnType, Location);
		}
	}
	
	public class VariableLookupVisitor : AbtractCSharpDomVisitor<object, object>
	{
		List<KeyValuePair <string, IReturnType>> unknownVariables = new List<KeyValuePair <string, IReturnType>> ();
		Dictionary<string, VariableDescriptor> variables = new Dictionary<string, VariableDescriptor> ();
		bool valueGetsChanged;
		
		public bool ReferencesMember {
			get;
			set;
		}
		
		public List<KeyValuePair <string, IReturnType>> UnknownVariables {
			get {
				return unknownVariables;
			}
		}

		public Dictionary<string, VariableDescriptor> Variables {
			get {
				return variables;
			}
		}
		
		public List<VariableDescriptor> VariableList {
			get {
				return new List<VariableDescriptor> (variables.Values);
			}
		}
		
		public DomLocation MemberLocation {
			get;
			set;
		}
		
		IResolver resolver;
		DomLocation position;
		public DomRegion CutRegion {
			get;
			set;
		}
		public VariableLookupVisitor (IResolver resolver, DomLocation position)
		{
			this.resolver = resolver;
			this.position = position;
			this.MemberLocation = DomLocation.Empty;
		}
		
		static IReturnType ConvertTypeReference (ICSharpNode node)
		{
			if (node is FullTypeName) {
				var ftn = (FullTypeName)node;
				var type = new DomReturnType (ftn.Identifier.Name);
				foreach (var param in ftn.TypeArguments) {
					type.AddTypeParameter (ConvertTypeReference (param));
				}
				return type;
			}
			return DomReturnType.Object;
			
			/* old version:
			if (typeRef == null)
				return null;
			DomReturnType result = new DomReturnType (typeRef.Type);
			foreach (TypeReference genericArgument in typeRef.GenericTypes) {
				result.AddTypeParameter (ConvertTypeReference (genericArgument));
			}
			result.PointerNestingLevel = typeRef.PointerNestingLevel;
			if (typeRef.IsArrayType) {
				result.ArrayDimensions = typeRef.RankSpecifier.Length;
				for (int i = 0; i < typeRef.RankSpecifier.Length; i++) {
					result.SetDimension (i, typeRef.RankSpecifier[i]);
				}
			}
			return result;*/
		}
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			if (!CutRegion.Contains (variableDeclarationStatement.StartLocation)) {
				foreach (var varDecl in variableDeclarationStatement.Variables) {
					variables[varDecl.Name] = new VariableDescriptor (varDecl.Name) {
						IsDefined = true, 
						ReturnType = ConvertTypeReference (variableDeclarationStatement.ReturnType),
						Location = new DocumentLocation (variableDeclarationStatement.StartLocation.Line, variableDeclarationStatement.StartLocation.Column)
					};
				}
			}
			return base.VisitVariableDeclarationStatement (variableDeclarationStatement, data);
		}
		
		public override object VisitIdentifierExpression (MonoDevelop.CSharp.Dom.IdentifierExpression identifierExpression, object data)
		{
			if (!variables.ContainsKey (identifierExpression.Identifier.Name)) {
				ExpressionResult expressionResult = new ExpressionResult (identifierExpression.Identifier.Name);
				ResolveResult result = resolver.Resolve (expressionResult, position);
				Console.WriteLine (identifierExpression.Identifier.Name + ":" + result);
				MemberResolveResult mrr = result as MemberResolveResult;
				ReferencesMember |= mrr != null && mrr.ResolvedMember != null && !mrr.ResolvedMember.IsStatic;
				
				if (!(result is LocalVariableResolveResult || result is ParameterResolveResult))
					return null;
				
				// result.ResolvedType == null may be true for namespace names or undeclared variables
				if (!result.StaticResolve && !variables.ContainsKey (identifierExpression.Identifier.Name)) {
					variables[identifierExpression.Identifier.Name] = new VariableDescriptor (identifierExpression.Identifier.Name) {
						InitialValueUsed = !valueGetsChanged,
						Location = new DocumentLocation (identifierExpression.StartLocation.Line, identifierExpression.StartLocation.Column)
					};
					variables[identifierExpression.Identifier.Name].ReturnType = result.ResolvedType;
				}
				if (result != null && !result.StaticResolve && result.ResolvedType != null && !(result is MethodResolveResult) && !(result is NamespaceResolveResult) && !(result is MemberResolveResult))
					unknownVariables.Add (new KeyValuePair <string, IReturnType> (identifierExpression.Identifier.Name, result.ResolvedType));
			}
			return null;
		}
		
		public override object VisitAssignmentExpression (MonoDevelop.CSharp.Dom.AssignmentExpression assignmentExpression, object data)
		{
			((ICSharpNode)assignmentExpression.Right).AcceptVisitor(this, data);
				
			valueGetsChanged = true;
			var left = assignmentExpression.Left as MonoDevelop.CSharp.Dom.IdentifierExpression;
			
			bool isInitialUse = left != null && !variables.ContainsKey (left.Identifier.Name);
			((ICSharpNode)assignmentExpression.Left).AcceptVisitor(this, data);
			valueGetsChanged = false;
			
			if (left != null && variables.ContainsKey (left.Identifier.Name)) {
				variables[left.Identifier.Name].GetsChanged = true;
				if (isInitialUse)
					variables[left.Identifier.Name].GetsAssigned = true;
			}
			return null;
		}
		
		public override object VisitUnaryOperatorExpression (MonoDevelop.CSharp.Dom.UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.UnaryOperatorType) {
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.Increment:
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.Decrement:
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.PostIncrement:
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.PostDecrement:
				valueGetsChanged = true;
				break;
			}
			object result = base.VisitUnaryOperatorExpression (unaryOperatorExpression, data);
			valueGetsChanged = false;
			switch (unaryOperatorExpression.UnaryOperatorType) {
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.Increment:
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.Decrement:
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.PostIncrement:
			case MonoDevelop.CSharp.Dom.UnaryOperatorType.PostDecrement:
				var left = unaryOperatorExpression.Expression as MonoDevelop.CSharp.Dom.IdentifierExpression;
				if (left != null && variables.ContainsKey (left.Identifier.Name))
					variables[left.Identifier.Name].GetsChanged = true;
				break;
			}
			return result;
		}
		
		public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			return base.VisitBinaryOperatorExpression (binaryOperatorExpression, data);
		}

		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && methodDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}

		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && propertyDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		public override object VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && eventDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			return base.VisitEventDeclaration (eventDeclaration, data);
		}
		
		/*
		public override object VisitDirectionExpression (ICSharpCode.NRefactory.Ast.DirectionExpression directionExpression, object data)
		{
			valueGetsChanged = true;
			IdentifierExpression left = directionExpression.Expression as IdentifierExpression;
			bool isInitialUse = left != null && !variables.ContainsKey (left.Identifier);
			object result = base.VisitDirectionExpression (directionExpression, data);
			valueGetsChanged = false;
			if (left != null && variables.ContainsKey (left.Identifier)) {
				variables[left.Identifier].GetsChanged = true;
				if (isInitialUse && directionExpression.FieldDirection == FieldDirection.Out)
					variables[left.Identifier].GetsAssigned = true;
			}
			
			return result;
		}
		*/
	}
}
