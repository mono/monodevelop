//
// MissingRequiredProtocolMemberIssue.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.CodeIssues;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using System.Linq;
using MonoDevelop.CodeGeneration;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	public class MissingRequiredProtocolMemberIssue : MonoDevelop.CodeIssues.CodeIssueProvider
	{
		public override bool HasSubIssues {
			get {
				return false;
			}
		}

		public MissingRequiredProtocolMemberIssue ()
		{
			this.Title = "Missing protocol member issue";
			this.Description = "Missing protocol member issue";
			this.Category = IssueCategories.Notifications;
			this.SetMimeType ("text/x-csharp");
			this.IsEnabledByDefault = true;
			this.SetSeverity (ICSharpCode.NRefactory.Refactoring.Severity.Warning); 
			this.SetIsEnabled (true);
		}

		public override IEnumerable<MonoDevelop.CodeIssues.CodeIssue> GetIssues (object refactoringContext, System.Threading.CancellationToken cancellationToken)
		{
			var context = refactoringContext as MDRefactoringContext;
			if (context == null || context.IsInvalid || context.RootNode == null || context.ParsedDocument.HasErrors)
				return new MonoDevelop.CodeIssues.CodeIssue[0];
			var visitor = new MissingRequiredProtocolMemberIssueVisitor (context);
			context.RootNode.AcceptVisitor (visitor);
			return visitor.Issues;
		}

		class MissingRequiredProtocolMemberIssueVisitor : DepthFirstAstVisitor
		{
			readonly MDRefactoringContext ctx;
			public readonly List<MonoDevelop.CodeIssues.CodeIssue> Issues = new List<MonoDevelop.CodeIssues.CodeIssue> ();

			public MissingRequiredProtocolMemberIssueVisitor  (MDRefactoringContext ctx)
			{
				this.ctx = ctx;
			}

			bool FindProtocolMember (IType type, IMember member)
			{
				foreach (var t in type.GetMembers ()) {
					if (SignatureComparer.Ordinal.Equals (t, member))
						return true;
				}
				return false;
			}

			void AddIssue (IType type, AstType bt, IType t)
			{
				Issues.Add (
					new MonoDevelop.CodeIssues.CodeIssue (
						ICSharpCode.NRefactory.Refactoring.IssueMarker.WavedLine,
						"Some required protocol members are missing",
						new DomRegion (bt.StartLocation, bt.EndLocation),
						"MissingRequiredProtocolMemberIssue",
						new MonoDevelop.CodeActions.CodeAction[] { 
							new MonoDevelop.CodeActions.DefaultCodeAction (
								"Implement required protocol members",
								(ctx, s) => {
									var service = (ICSharpCode.NRefactory.CSharp.CodeGenerationService)ctx.GetService (typeof (ICSharpCode.NRefactory.CSharp.CodeGenerationService));
									var implementedNodes = GetAllProtocolMembers (t).Where (member => !FindProtocolMember (type, member)).Select (pm => (AstNode)service.GenerateMemberImplementation (ctx, pm, false)).ToList ();
									s.InsertWithCursor ("Add missing protocol members", Script.InsertPosition.End, implementedNodes);
								}
							)
						}
				)); 
			}

			public IEnumerable<IMember> GetAllProtocolMembers (IType t)
			{
				string name;
				if (!BaseExportCodeGenerator.HasProtocolAttribute (t, out name))
					yield break;

				var protocolType = ctx.Compilation.FindType (new FullTypeName (new TopLevelTypeName (t.Namespace, name)));
				if (protocolType == null)
					yield break;

				foreach (var member in protocolType.GetMethods (null, GetMemberOptions.IgnoreInheritedMembers)) {
					if (member.ImplementedInterfaceMembers.Any ())
						continue;
					if (member.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation")) {
						yield return member;
					}
				}

				foreach (var member in protocolType.GetProperties (null, GetMemberOptions.IgnoreInheritedMembers)) {
					if (member.ImplementedInterfaceMembers.Any ())
						continue;
					if (member.CanGet && member.Getter.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation") ||
						member.CanSet && member.Setter.Attributes.Any (a => a.AttributeType.Name == "ExportAttribute" &&  a.AttributeType.Namespace == "MonoTouch.Foundation")) {
						yield return member;
					}
				}
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				base.VisitTypeDeclaration (typeDeclaration);
				var type = ctx.Resolve (typeDeclaration).Type;

				foreach (var bt in typeDeclaration.BaseTypes) {
					var t = ctx.Resolve (bt).Type;

					foreach (var member in GetAllProtocolMembers (t)) {
						if (!member.IsAbstract)
							continue;
						if (!FindProtocolMember (type, member)) {
							AddIssue (type, bt, t);
							return;
						}
					}
				}
			}

			public override void VisitBlockStatement (BlockStatement blockStatement)
			{
				// SKIP
			}
		}
	}
}

