// 
// InconsistentNamingIssue.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Inconsistent Naming",
	       Description = "Name doesn't match the defined style for this entity.",
	       Category = IssueCategories.ConstraintViolations,
	       Severity = Severity.Warning)]
	public class InconsistentNamingIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			var visitor = new GatherVisitor(context, this);
			context.RootNode.AcceptVisitor(visitor);
			return visitor.FoundIssues;
		}

		class GatherVisitor : GatherVisitorBase
		{
			readonly InconsistentNamingIssue inspector;
			List<NamingRule> rules;

			public GatherVisitor (BaseRefactoringContext ctx, InconsistentNamingIssue inspector) : base (ctx)
			{
				this.inspector = inspector;
				
				rules = new List<NamingRule> (ctx.RequestData<IEnumerable<NamingRule>> () ?? Enumerable.Empty<NamingRule> ());
			}

			void CheckName(AffectedEntity entity, Identifier identifier, Modifiers accessibilty)
			{
				foreach (var rule in rules) {
					if (!rule.AffectedEntity.HasFlag(entity)) {
						continue;
					}
					if (!rule.IsValid(identifier.Name)) {
						IList<string> suggestedNames;
						var msg = rule.GetErrorMessage(ctx, identifier.Name, out suggestedNames);
						AddIssue(identifier, msg, suggestedNames.Select(n => new CodeAction(string.Format(ctx.TranslateString("Rename to '{0}'"), n), (Script script) => {
							// TODO: Real rename.
							script.Replace(identifier, Identifier.Create(n));
						})));
					}
				}
			}

			public override void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
			{
				base.VisitNamespaceDeclaration(namespaceDeclaration);
				foreach (var id in namespaceDeclaration.Identifiers) {
					CheckName(AffectedEntity.Namespace, id, Modifiers.None);
				}
			}

			Modifiers  GetAccessibiltiy(EntityDeclaration decl, Modifiers defaultModifier)
			{
				var accessibility = (decl.Modifiers & Modifiers.VisibilityMask);
				if (accessibility == Modifiers.None) {
					return defaultModifier;
				}
				return accessibility;
			}

			public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
			{
				base.VisitTypeDeclaration(typeDeclaration);
				AffectedEntity entity;
				switch (typeDeclaration.ClassType) {
					case ClassType.Class:
						entity = AffectedEntity.Class;
						break;
					case ClassType.Struct:
						entity = AffectedEntity.Struct;
						break;
					case ClassType.Interface:
						entity = AffectedEntity.Interface;
						break;
					case ClassType.Enum:
						entity = AffectedEntity.Enum;
						break;
					default:
						throw new System.ArgumentOutOfRangeException();
				}

				CheckName(entity, typeDeclaration.NameToken, GetAccessibiltiy(typeDeclaration, typeDeclaration.Parent is TypeDeclaration ? Modifiers.Private : Modifiers.Internal));
			}

			public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
			{
				base.VisitDelegateDeclaration(delegateDeclaration);
				CheckName(AffectedEntity.Delegate, delegateDeclaration.NameToken, GetAccessibiltiy(delegateDeclaration, delegateDeclaration.Parent is TypeDeclaration ? Modifiers.Private : Modifiers.Internal));
			}

			public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
			{
				base.VisitPropertyDeclaration(propertyDeclaration);
				CheckName(AffectedEntity.Property, propertyDeclaration.NameToken, GetAccessibiltiy(propertyDeclaration, Modifiers.Private));
			}

			public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
			{
				base.VisitMethodDeclaration(methodDeclaration);
				CheckName(AffectedEntity.Method, methodDeclaration.NameToken, GetAccessibiltiy(methodDeclaration, Modifiers.Private));
			}

			public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
			{
				base.VisitFieldDeclaration(fieldDeclaration);
				foreach (var init in fieldDeclaration.Variables) {
					CheckName(AffectedEntity.Field, init.NameToken, GetAccessibiltiy(fieldDeclaration, Modifiers.Private));
				}
			}

			public override void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
			{
				base.VisitFixedFieldDeclaration(fixedFieldDeclaration);
				CheckName(AffectedEntity.Field, fixedFieldDeclaration.NameToken, GetAccessibiltiy(fixedFieldDeclaration, Modifiers.Private));
			}

			public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
			{
				base.VisitEventDeclaration(eventDeclaration);
				foreach (var init in eventDeclaration.Variables) {
					CheckName(AffectedEntity.Event, init.NameToken, GetAccessibiltiy(eventDeclaration, Modifiers.Private));
				}
			}

			public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
			{
				base.VisitCustomEventDeclaration(eventDeclaration);
				CheckName(AffectedEntity.Event, eventDeclaration.NameToken, GetAccessibiltiy(eventDeclaration, Modifiers.Private));
			}

			public override void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
			{
				base.VisitEnumMemberDeclaration(enumMemberDeclaration);
				CheckName(AffectedEntity.EnumMember, enumMemberDeclaration.NameToken, GetAccessibiltiy(enumMemberDeclaration, Modifiers.Private));
			}

			public override void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
			{
				base.VisitParameterDeclaration(parameterDeclaration);
				CheckName(parameterDeclaration.Parent is LambdaExpression ? AffectedEntity.LambdaParameter : AffectedEntity.Parameter, parameterDeclaration.NameToken, Modifiers.None);
			}

			public override void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
			{
				base.VisitTypeParameterDeclaration(typeParameterDeclaration);
				CheckName(AffectedEntity.TypeParameter, typeParameterDeclaration.NameToken, Modifiers.None);
			}

			public override void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
			{
				base.VisitVariableDeclarationStatement(variableDeclarationStatement);
				foreach (var init in variableDeclarationStatement.Variables) {
					CheckName(AffectedEntity.LocalVariable, init.NameToken, Modifiers.None);
				}
			}

			public override void VisitLabelStatement(LabelStatement labelStatement)
			{
				base.VisitLabelStatement(labelStatement);
				CheckName(AffectedEntity.Label, labelStatement.LabelToken, Modifiers.None);
			}
		}

	}
}

