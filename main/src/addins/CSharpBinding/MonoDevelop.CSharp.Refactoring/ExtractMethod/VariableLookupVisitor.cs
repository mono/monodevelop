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
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp.Refactoring.ExtractMethod
{
	public class VariableDescriptor 
	{
		public string Name {
			get;
			set;
		}
		
		public IReturnType ReturnType {
			get;
			set;
		}
		
		public bool IsDefinedInsideCutRegion {
			get;
			set;
		}
		
		public bool UsedInCutRegion {
			get;
			set;
		}
		
		public bool UsedBeforeCutRegion {
			get;
			set;
		}
		
		public bool UsedAfterCutRegion {
			get;
			set;
		}
		
		public bool IsChangedInsideCutRegion {
			get;
			set;
		}
		
		public VariableDeclarationStatement Declaration {
			get;
			set;
		}
		
		public VariableDescriptor (string name)
		{
			this.Name = name;
		}
		
		public override string ToString ()
		{
			return string.Format ("[VariableDescriptor: Name={0}, ReturnType={1}, IsDefinedInsideCutRegion={2}, UsedInCutRegion={3}, UsedBeforeCutRegion={4}, UsedAfterCutRegion={5}, IsChangedInsideCutRegion={6}]", Name, ReturnType, IsDefinedInsideCutRegion, UsedInCutRegion, UsedBeforeCutRegion, UsedAfterCutRegion, IsChangedInsideCutRegion);
		}

	}
	
	public class VariableLookupVisitor : DepthFirstAstVisitor<object, object>
	{
		List<KeyValuePair <string, IReturnType>> unknownVariables = new List<KeyValuePair <string, IReturnType>> ();
		Dictionary<string, VariableDescriptor> variables = new Dictionary<string, VariableDescriptor> ();
//		bool valueGetsChanged;
		
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
		
		public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			bool isDefinedInsideCutRegion = CutRegion.Contains (variableDeclarationStatement.StartLocation.Line, variableDeclarationStatement.StartLocation.Column);
			foreach (var varDecl in variableDeclarationStatement.Variables) {
				var descr = new VariableDescriptor (varDecl.Name) {
					IsDefinedInsideCutRegion = isDefinedInsideCutRegion,
					Declaration = variableDeclarationStatement
				};
				if (varDecl.Initializer != null) {
					if (isDefinedInsideCutRegion) {
						descr.UsedInCutRegion = true;
					} else if (variableDeclarationStatement.StartLocation < new AstLocation (CutRegion.Start.Line, CutRegion.Start.Column)) {
						descr.UsedBeforeCutRegion = !varDecl.Initializer.IsNull; 
					} else {
						descr.UsedAfterCutRegion = true;
					}
				}
				variables[varDecl.Name] = descr;
			}
			return base.VisitVariableDeclarationStatement (variableDeclarationStatement, data);
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.CSharp.IdentifierExpression identifierExpression, object data)
		{
			ExpressionResult expressionResult = new ExpressionResult (identifierExpression.Identifier);
			ResolveResult result = resolver.Resolve (expressionResult, position);
			MemberResolveResult mrr = result as MemberResolveResult;
			ReferencesMember |= mrr != null && mrr.ResolvedMember != null && !mrr.ResolvedMember.IsStatic;
			
			if (!(result is LocalVariableResolveResult || result is ParameterResolveResult))
				return null;
			if (!variables.ContainsKey (identifierExpression.Identifier))
				return null;
			var v = variables[identifierExpression.Identifier];
			v.ReturnType = result.ResolvedType;
			if (CutRegion.Contains (identifierExpression.StartLocation.Line, identifierExpression.StartLocation.Column)) {
				if (!v.IsChangedInsideCutRegion)
					v.UsedInCutRegion = true;
			} else if (identifierExpression.StartLocation < new AstLocation (CutRegion.Start.Line, CutRegion.Start.Column)) {
				v.UsedBeforeCutRegion = true;
			} else {
				v.UsedAfterCutRegion = true;
			}
			return null;
		}
		
		public override object VisitAssignmentExpression (ICSharpCode.NRefactory.CSharp.AssignmentExpression assignmentExpression, object data)
		{
			assignmentExpression.Right.AcceptVisitor(this, data);
//			valueGetsChanged = true;

			var left = assignmentExpression.Left as ICSharpCode.NRefactory.CSharp.IdentifierExpression;
			
			if (left != null && variables.ContainsKey (left.Identifier)) {
				var v = variables[left.Identifier];
				v.IsChangedInsideCutRegion = CutRegion.Contains (assignmentExpression.StartLocation.Line, assignmentExpression.StartLocation.Column);
				if (!v.IsChangedInsideCutRegion) {
					if (assignmentExpression.StartLocation < new AstLocation (CutRegion.Start.Line, CutRegion.Start.Column)) {
						v.UsedBeforeCutRegion = true;
					} else {
						v.UsedAfterCutRegion = true;
					}
				}
			}
			return null;
		}
		
		public override object VisitUnaryOperatorExpression (ICSharpCode.NRefactory.CSharp.UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			base.VisitUnaryOperatorExpression (unaryOperatorExpression, data);
			if (CutRegion.Contains (unaryOperatorExpression.StartLocation.Line, unaryOperatorExpression.StartLocation.Column)) {
				var left = unaryOperatorExpression.Expression as ICSharpCode.NRefactory.CSharp.IdentifierExpression;
				if (left != null && variables.ContainsKey (left.Identifier)) {
					variables[left.Identifier].IsChangedInsideCutRegion = true;
				}
			}
			/*
			switch (unaryOperatorExpression.UnaryOperatorType) {
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Increment:
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Decrement:
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.PostIncrement:
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.PostDecrement:
				valueGetsChanged = true;
				break;
			}
			object result = base.VisitUnaryOperatorExpression (unaryOperatorExpression, data);
			valueGetsChanged = false;
			switch (unaryOperatorExpression.UnaryOperatorType) {
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Increment:
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.Decrement:
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.PostIncrement:
			case ICSharpCode.NRefactory.CSharp.UnaryOperatorType.PostDecrement:
				var left = unaryOperatorExpression.Expression as ICSharpCode.NRefactory.CSharp.IdentifierExpression;
				if (left != null && variables.ContainsKey (left.Identifier.Name))
					variables[left.Identifier.Name].GetsChanged = true;
				break;
			}*/
			return null;
		}
		
		public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			return base.VisitBinaryOperatorExpression (binaryOperatorExpression, data);
		}

		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			if (!MemberLocation.IsEmpty && methodDeclaration.StartLocation.Line != MemberLocation.Line)
				return null;
			foreach (var param in methodDeclaration.Parameters) {
				
				variables[param.Name] = new VariableDescriptor (param.Name);
				
			}
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
		public override object VisitDirectionExpression (ICSharpCode.OldNRefactory.Ast.DirectionExpression directionExpression, object data)
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
