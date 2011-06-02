// 
// NamingConventions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.AnalysisCore;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.AnalysisCore.Fixes;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Text;

namespace MonoDevelop.CSharp.Inspection
{
	class NamingInspector : CSharpInspector
	{
		CSharpNamingPolicy policy = new CSharpNamingPolicy ();
		
		void Check (InspectionData data, NamingRule rule, AstLocation loc, string name)
		{
			if (!rule.IsValid (name))
				data.Add (rule.GetFixableResult (loc, null, name));
		}
		
		void Check (InspectionData data, NamingRule rule, AstLocation loc, string name, IBaseMember member)
		{
			if (!rule.IsValid (name))
				data.Add (rule.GetFixableResult (loc, member, name));
		}
		
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitor)
		{
			visitor.VariableDeclarationStatementVisited += HandleVisitorVariableDeclarationStatementVisited;
			visitor.FixedFieldDeclarationVisited += HandleVisitorFixedFieldDeclarationVisited;
			visitor.ParameterDeclarationVisited += HandleVisitorParameterDeclarationVisited;
			visitor.PropertyDeclarationVisited += HandleVisitorPropertyDeclarationVisited;
			visitor.FieldDeclarationVisited += HandleVisitorFieldDeclarationVisited;
			visitor.CustomEventDeclarationVisited += HandleVisitorCustomEventDeclarationVisited;
			visitor.EventDeclarationVisited += HandleVisitorEventDeclarationVisited;
			visitor.TypeDeclarationVisited += HandleVisitorTypeDeclarationVisited;
			visitor.DelegateDeclarationVisited += HandleVisitorDelegateDeclarationVisited;
			visitor.NamespaceDeclarationVisited += HandleVisitorNamespaceDeclarationVisited;
			visitor.TypeParameterDeclarationVisited += HandleVisitorTypeParameterDeclarationVisited;
			visitor.EnumMemberDeclarationVisited += HandleVisitorEnumMemberDeclarationVisited;
		}

		void HandleVisitorVariableDeclarationStatementVisited (VariableDeclarationStatement variableDeclarationStatement, InspectionData data)
		{
			var member = data.Document.CompilationUnit.GetMemberAt (variableDeclarationStatement.StartLocation.Line, variableDeclarationStatement.StartLocation.Column);
			foreach (var var in variableDeclarationStatement.Variables) {
				var v = new LocalVariable (member, var.Name, DomReturnType.Void, new DomRegion (variableDeclarationStatement.StartLocation.Line, variableDeclarationStatement.StartLocation.Column, variableDeclarationStatement.EndLocation.Line, variableDeclarationStatement.EndLocation.Column));
				if ((variableDeclarationStatement.Modifiers & ICSharpCode.NRefactory.CSharp.Modifiers.Const) == ICSharpCode.NRefactory.CSharp.Modifiers.Const) {
					Check (data, policy.LocalConstant, var.StartLocation, var.Name, v);
				} else {
					Check (data, policy.LocalVariable, var.StartLocation, var.Name, v);
				}
			}
		}

		void HandleVisitorFixedFieldDeclarationVisited (FixedFieldDeclaration fixedFieldDeclaration, InspectionData data)
		{
			foreach (var var in fixedFieldDeclaration.Variables) {
				Check (data, policy.Field, var.StartLocation, var.Name);
			}
		}

		void HandleVisitorParameterDeclarationVisited (ParameterDeclaration parameterDeclaration, InspectionData data)
		{
			Check (data, policy.Parameter, parameterDeclaration.NameToken.StartLocation, parameterDeclaration.Name);
		}

		void HandleVisitorPropertyDeclarationVisited (PropertyDeclaration propertyDeclaration, InspectionData data)
		{
			Check (data, policy.Property, propertyDeclaration.NameToken.StartLocation, propertyDeclaration.Name);
		}

		void HandleVisitorFieldDeclarationVisited (FieldDeclaration fieldDeclaration, InspectionData data)
		{
			NamingRule namingRule;
			
			bool isPrivate = (fieldDeclaration.Modifiers & (ICSharpCode.NRefactory.CSharp.Modifiers.Public | ICSharpCode.NRefactory.CSharp.Modifiers.Protected | ICSharpCode.NRefactory.CSharp.Modifiers.Internal)) == 0; 
			
			if ((fieldDeclaration.Modifiers & ICSharpCode.NRefactory.CSharp.Modifiers.Const) == ICSharpCode.NRefactory.CSharp.Modifiers.Const) {
				if (isPrivate) {
					namingRule = policy.InstanceConstant;
				} else {
					namingRule = policy.Constant;
				}
			} else {
				if (isPrivate) {
					if ((fieldDeclaration.Modifiers & ICSharpCode.NRefactory.CSharp.Modifiers.Static) == ICSharpCode.NRefactory.CSharp.Modifiers.Static) {
						namingRule = policy.InstanceStaticField;
					} else {
						namingRule = policy.InstanceField;
					}
				} else {
					namingRule = policy.Field;
				}
			}
			
			foreach (var var in fieldDeclaration.Variables) {
				Check (data, namingRule, var.StartLocation, var.Name);
			}
		}

		void HandleVisitorCustomEventDeclarationVisited (CustomEventDeclaration eventDeclaration, InspectionData data)
		{
			Check (data, policy.Event, eventDeclaration.NameToken.StartLocation, eventDeclaration.Name);
		}

		void HandleVisitorEventDeclarationVisited (EventDeclaration eventDeclaration, InspectionData data)
		{
			foreach (var var in eventDeclaration.Variables) {
				Check (data, policy.Event, var.StartLocation, var.Name);
			}
		}

		void HandleVisitorTypeDeclarationVisited (TypeDeclaration typeDeclaration, InspectionData data)
		{
			switch (typeDeclaration.ClassType) {
			case ICSharpCode.NRefactory.TypeSystem.ClassType.Interface:
				Check (data, policy.Interface, typeDeclaration.NameToken.StartLocation, typeDeclaration.Name);
				break;
			default:
				Check (data, policy.Type, typeDeclaration.NameToken.StartLocation, typeDeclaration.Name);
				break;
			}
			
		}

		void HandleVisitorEnumMemberDeclarationVisited (EnumMemberDeclaration node, InspectionData data)
		{
			// TODO
		}

		void HandleVisitorTypeParameterDeclarationVisited (TypeParameterDeclaration node, InspectionData data)
		{
			// TODO
		}

		void HandleVisitorNamespaceDeclarationVisited (NamespaceDeclaration node, InspectionData data)
		{
			// TODO
		}

		void HandleVisitorDelegateDeclarationVisited (DelegateDeclaration node, InspectionData data)
		{
			// TODO
		}

	}
}
