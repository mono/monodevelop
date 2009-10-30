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
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Refactoring.ExtractMethod
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
		
		public VariableDescriptor (string name)
		{
			this.Name = name;
			this.GetsChanged = this.IsDefined = this.InitialValueUsed = false;
		}
		
		public override string ToString ()
		{
			return string.Format ("[VariableDescriptor: Name={0}, GetsChanged={1}, InitialValueUsed={2}, IsDefined={3}, ReturnType={4}]", Name, GetsChanged, InitialValueUsed, IsDefined, ReturnType);
		}
		
	}
	
	public class VariableLookupVisitor : AbstractAstVisitor
	{
		List<KeyValuePair <string, IReturnType>> unknownVariables = new List<KeyValuePair <string, IReturnType>> ();
		Dictionary<string, VariableDescriptor> variables = new Dictionary<string, VariableDescriptor> ();
		
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
		
		public Location MemberLocation {
			get;
			set;
		}
		
		IResolver resolver;
		DomLocation position;
		public VariableLookupVisitor (IResolver resolver, DomLocation position)
		{
			this.resolver = resolver;
			this.position = position;
			this.MemberLocation = Location.Empty;
		}
		
		static IReturnType ConvertTypeReference (TypeReference typeRef)
		{
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
			return result;
		}
		
		public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
		{
			foreach (VariableDeclaration varDecl in localVariableDeclaration.Variables) {
				variables[varDecl.Name] = new VariableDescriptor (varDecl.Name) {
					IsDefined = true, 
					ReturnType = ConvertTypeReference (localVariableDeclaration.TypeReference)
				};
			}
			return base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
		{
			if (!variables.ContainsKey (identifierExpression.Identifier)) {

				ExpressionResult expressionResult = new ExpressionResult (identifierExpression.Identifier);

				ResolveResult result = resolver.Resolve (expressionResult, position);
				
				MemberResolveResult mrr = result as MemberResolveResult;
				ReferencesMember |= mrr != null && mrr.ResolvedMember != null && !mrr.ResolvedMember.IsStatic;
				
				if (!(result is LocalVariableResolveResult || result is ParameterResolveResult))
					return base.VisitIdentifierExpression (identifierExpression, data);
				
				// result.ResolvedType == null may be true for namespace names or undeclared variables
				if (!result.StaticResolve && !variables.ContainsKey (identifierExpression.Identifier)) {
					variables[identifierExpression.Identifier] = new VariableDescriptor (identifierExpression.Identifier) {
						InitialValueUsed = !valueGetsChanged
					};
					variables[identifierExpression.Identifier].ReturnType = result.ResolvedType;
				}
				if (result != null && !result.StaticResolve && result.ResolvedType != null && !(result is MethodResolveResult) && !(result is NamespaceResolveResult) && !(result is MemberResolveResult))
					unknownVariables.Add (new KeyValuePair <string, IReturnType> (identifierExpression.Identifier, result.ResolvedType));
			}
			return base.VisitIdentifierExpression (identifierExpression, data);
		}
		bool valueGetsChanged = false;
		public override object VisitAssignmentExpression (ICSharpCode.NRefactory.Ast.AssignmentExpression assignmentExpression, object data)
		{
			assignmentExpression.Right.AcceptVisitor(this, data);
				
			valueGetsChanged = true;
			IdentifierExpression left = assignmentExpression.Left as IdentifierExpression;
			bool isInitialUse = left != null && !variables.ContainsKey (left.Identifier);
			assignmentExpression.Left.AcceptVisitor(this, data);
			valueGetsChanged = false;
			
			if (left != null && variables.ContainsKey (left.Identifier)) {
				variables[left.Identifier].GetsChanged = true;
				if (isInitialUse)
					variables[left.Identifier].GetsAssigned = true;
			}
			return null;
		}
		
		public override object VisitUnaryOperatorExpression (ICSharpCode.NRefactory.Ast.UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			switch (unaryOperatorExpression.Op) {
			case UnaryOperatorType.Increment:
			case UnaryOperatorType.Decrement:
			case UnaryOperatorType.PostIncrement:
			case UnaryOperatorType.PostDecrement:
				valueGetsChanged = true;
				break;
			}
			object result = base.VisitUnaryOperatorExpression (unaryOperatorExpression, data);
			valueGetsChanged = false;
			switch (unaryOperatorExpression.Op) {
			case UnaryOperatorType.Increment:
			case UnaryOperatorType.Decrement:
			case UnaryOperatorType.PostIncrement:
			case UnaryOperatorType.PostDecrement:
				IdentifierExpression left = unaryOperatorExpression.Expression as IdentifierExpression;
				if (left != null && variables.ContainsKey (left.Identifier))
					variables[left.Identifier].GetsChanged = true;
				break;
			}
			return result;
		}

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
	}
}
