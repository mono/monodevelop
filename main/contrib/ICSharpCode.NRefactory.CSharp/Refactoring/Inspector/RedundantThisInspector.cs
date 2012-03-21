// 
// RedundantThisInspector.cs
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
using ICSharpCode.NRefactory.PatternMatching;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Finds redundant namespace usages.
	/// </summary>
	public class RedundantThisInspector : IInspector
	{
		string title = "Remove redundant 'this.'";

		public string Title {
			get {
				return title;
			}
			set {
				title = value;
			}
		}

		public IEnumerable<InspectionIssue> Run (BaseRefactoringContext context)
		{
			var visitor = new GatherVisitor (context, this);
			context.RootNode.AcceptVisitor (visitor);
			return visitor.FoundIssues;
		}

		class GatherVisitor : GatherVisitorBase
		{
			readonly RedundantThisInspector inspector;
			
			public GatherVisitor (BaseRefactoringContext ctx, RedundantThisInspector inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			static IMember GetMember(ResolveResult result)
			{
				if (result is MemberResolveResult) {
					return ((MemberResolveResult)result).Member;
				} 
				return null;
			}

			static IEnumerable<IMember> GetMembers (ResolveResult result)
			{
				if (result is MemberResolveResult) {
					return new IMember[] {  ((MemberResolveResult)result).Member };
				} else if (result is MethodGroupResolveResult) {
					return ((MethodGroupResolveResult)result).Methods;
				}

				return null;
			}
				

			public override void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
			{
				base.VisitThisReferenceExpression(thisReferenceExpression);
				var memberReference = thisReferenceExpression.Parent as MemberReferenceExpression;
				if (memberReference == null) {
					return;
				}

				var state = ctx.GetResolverStateAfter(thisReferenceExpression);
				var wholeResult = ctx.Resolve(thisReferenceExpression);
			
				var result = state.LookupSimpleNameOrTypeName(memberReference.MemberName, new List<IType> (), SimpleNameLookupMode.Expression);
				if (result == null || wholeResult == null) {
					return;
				}
			
				IMember member = GetMember(wholeResult);
				if (member == null) { 
					return;
				}

				bool isRedundant;
				if (result is MemberResolveResult) {
					isRedundant = ((MemberResolveResult)result).Member.Region.Equals(member.Region);
				} else if (result is MethodGroupResolveResult) {
					isRedundant = ((MethodGroupResolveResult)result).Methods.Any(m => m.Region.Equals(member.Region));
				} else {
					return;
				}

				if (isRedundant) {
					AddIssue(thisReferenceExpression.StartLocation, memberReference.MemberNameToken.StartLocation, inspector.Title, delegate {
						using (var script = ctx.StartScript ()) {
							script.Replace(memberReference, new IdentifierExpression (memberReference.MemberName));
						}
					}
					);
				}
			}
		}
	}
}