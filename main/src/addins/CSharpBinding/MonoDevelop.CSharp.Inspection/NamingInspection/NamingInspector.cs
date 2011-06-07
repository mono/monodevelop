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
		
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitor)
		{
			visitor.VariableDeclarationStatementVisited += HandleVisitorVariableDeclarationStatementVisited;
			visitor.FixedFieldDeclarationVisited += HandleVisitorFixedFieldDeclarationVisited;
			visitor.ParameterDeclarationVisited += HandleVisitorParameterDeclarationVisited;
			visitor.PropertyDeclarationVisited += HandleVisitorPropertyDeclarationVisited;
			visitor.MethodDeclarationVisited += HandleVisitorMethodDeclarationVisited;
			visitor.FieldDeclarationVisited += HandleVisitorFieldDeclarationVisited;
			visitor.CustomEventDeclarationVisited += HandleVisitorCustomEventDeclarationVisited;
			visitor.EventDeclarationVisited += HandleVisitorEventDeclarationVisited;
			visitor.TypeDeclarationVisited += HandleVisitorTypeDeclarationVisited;
			visitor.DelegateDeclarationVisited += HandleVisitorDelegateDeclarationVisited;
			visitor.NamespaceDeclarationVisited += HandleVisitorNamespaceDeclarationVisited;
			visitor.TypeParameterDeclarationVisited += HandleVisitorTypeParameterDeclarationVisited;
			visitor.EnumMemberDeclarationVisited += HandleVisitorEnumMemberDeclarationVisited;
		}

		void HandleVisitorVariableDeclarationStatementVisited (VariableDeclarationStatement node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckVariableDeclaration (node, data))
					return;
			}
		}

		void HandleVisitorFixedFieldDeclarationVisited (FixedFieldDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckField (node, data))
					return;
			}
		}

		void HandleVisitorParameterDeclarationVisited (ParameterDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckParameter (node, data))
					return;
			}
		}

		void HandleVisitorPropertyDeclarationVisited (PropertyDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckProperty (node, data))
					return;
			}
		}
		
		void HandleVisitorMethodDeclarationVisited (MethodDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckMethod (node, data))
					return;
			}
		}

		void HandleVisitorFieldDeclarationVisited (FieldDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckField (node, data))
					return;
			}
		}

		void HandleVisitorCustomEventDeclarationVisited (CustomEventDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckEvent (node, data))
					return;
			}
		}

		void HandleVisitorEventDeclarationVisited (EventDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckEvent (node, data))
					return;
			}
		}

		void HandleVisitorTypeDeclarationVisited (TypeDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckType (node, data))
					return;
			}
		}

		void HandleVisitorEnumMemberDeclarationVisited (EnumMemberDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckEnumMember (node, data))
					return;
			}
		}

		void HandleVisitorTypeParameterDeclarationVisited (TypeParameterDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckTypeParameter (node, data))
					return;
			}
		}

		void HandleVisitorNamespaceDeclarationVisited (NamespaceDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckNamespace (node, data))
					return;
			}
		}

		void HandleVisitorDelegateDeclarationVisited (DelegateDeclaration node, InspectionData data)
		{
			foreach (var rule in policy.Rules) {
				if (rule.CheckDelegate (node, data))
					return;
			}
		}

	}
}
