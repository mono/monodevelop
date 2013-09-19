//
// MonoTODOIssue.cs
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


namespace MonoDevelop.CSharp.Refactoring.CodeIssues
{
	public class MonoTODOIssue : MonoDevelop.CodeIssues.CodeIssueProvider
	{
		public override bool HasSubIssues {
			get {
				return false;
			}
		}

		public MonoTODOIssue ()
		{
			this.Title = "Mono TODO";
			this.Description = "Find usages of mono todo items";
			this.Category = IssueCategories.Notifications;
			this.SetMimeType ("text/x-csharp");
			this.IsEnabledByDefault = true;
			this.SetSeverity (ICSharpCode.NRefactory.Refactoring.Severity.Error); 
			this.SetIsEnabled (true);
		}

		public override IEnumerable<CodeIssue> GetIssues (object refactoringContext, System.Threading.CancellationToken cancellationToken)
		{
			var context = refactoringContext as MDRefactoringContext;
			if (context == null || context.IsInvalid || context.RootNode == null || context.ParsedDocument.HasErrors)
				return new CodeIssue[0];
			var visitor = new MonoTODOVisitor (this, context);
			context.RootNode.AcceptVisitor (visitor);
			return visitor.Issues;
		}

		class MonoTODOVisitor : DepthFirstAstVisitor
		{
			readonly MonoTODOIssue issue;
			readonly MDRefactoringContext ctx;
			public readonly List<CodeIssue> Issues = new List<CodeIssue> ();

			public MonoTODOVisitor (MonoTODOIssue issue, MDRefactoringContext ctx)
			{
				this.issue = issue;
				this.ctx = ctx;
			}
			static readonly Dictionary<string, string> attributes = new Dictionary<string, string> {
				{ "MonoTODOAttribute", "Mono TODO" },
				{ "MonoNotSupportedAttribute", "Mono NOT SUPPORTED" },
				{ "MonoLimitationAttribute", "Mono LIMITATION" }
			};
			void Check (AstNode node, IMember member)
			{
				foreach (var attr in member.Attributes) {
					if (attr.AttributeType.Namespace != "System")
						continue;

					string val;
					if (attributes.TryGetValue (attr.AttributeType.Name, out val)) {
						string msg = null;
						var arg = attr.PositionalArguments.FirstOrDefault ();
						if (arg != null)
							msg = arg.ConstantValue != null ? arg.ConstantValue.ToString () : null;
						Issues.Add (new CodeIssue (ICSharpCode.NRefactory.Refactoring.IssueMarker.WavedLine,
							string.IsNullOrEmpty (msg) ? val : val + ": " + msg,
							new DomRegion (node.StartLocation, node.EndLocation),
							issue.IdString
						)); 
					}
				}

			}

			public override void VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression)
			{
				base.VisitMemberReferenceExpression (memberReferenceExpression);
				var rr = ctx.Resolve (memberReferenceExpression) as MemberResolveResult;
				if (rr == null || rr.IsError)
					return;
				Check (memberReferenceExpression, rr.Member);
			}

			public override void VisitIdentifierExpression (IdentifierExpression identifierExpression)
			{
				base.VisitIdentifierExpression (identifierExpression);
				var rr = ctx.Resolve (identifierExpression) as MemberResolveResult;
				if (rr == null || rr.IsError)
					return;
				Check (identifierExpression, rr.Member);
			}

			public override void VisitInvocationExpression (InvocationExpression invocationExpression)
			{
				base.VisitInvocationExpression (invocationExpression);
				var rr = ctx.Resolve (invocationExpression) as CSharpInvocationResolveResult;
				if (rr == null || rr.IsError)
					return;
				Check (invocationExpression, rr.Member);
			}

			public override void VisitBlockStatement (BlockStatement blockStatement)
			{
				ctx.CancellationToken.ThrowIfCancellationRequested ();
				base.VisitBlockStatement (blockStatement);
			}
		}
	}
}

