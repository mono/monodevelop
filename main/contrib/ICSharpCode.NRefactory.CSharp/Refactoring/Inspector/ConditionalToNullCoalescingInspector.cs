// 
// ConditionalToNullCoalescingInspector.cs
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
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Checks for "a != null ? a : other"<expr>
	/// Converts to: "a ?? other"<expr>
	/// </summary>
	public class ConditionalToNullCoalescingInspector : IInspector
	{
		static readonly Pattern pattern = new Choice {
			// a != null ? a : other
			new ConditionalExpression(
				new Choice {
					// a != null
					new BinaryOperatorExpression(new AnyNode("a"), BinaryOperatorType.InEquality, new NullReferenceExpression()),
					// null != a
					new BinaryOperatorExpression(new NullReferenceExpression(), BinaryOperatorType.InEquality, new AnyNode("a")),
				},
				new Backreference("a"),
				new AnyNode("other")
			),
			// a == null ? other : a
			new ConditionalExpression(
				new Choice {
					// a == null
					new BinaryOperatorExpression(new AnyNode("a"), BinaryOperatorType.Equality, new NullReferenceExpression()),
					// null == a
					new BinaryOperatorExpression(new NullReferenceExpression(), BinaryOperatorType.Equality, new AnyNode("a")),
				},
				new AnyNode("other"),
				new Backreference("a")
			),
		};
		
		string title = "Convert to '??' expression";

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
			readonly ConditionalToNullCoalescingInspector inspector;
			
			public GatherVisitor (BaseRefactoringContext ctx, ConditionalToNullCoalescingInspector inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
			{
				Match m = pattern.Match(conditionalExpression);
				if (m.Success) {
					var a = m.Get<Expression>("a").Single();
					var other = m.Get<Expression>("other").Single();
					AddIssue(conditionalExpression, inspector.Title, delegate {
						using (var script = ctx.StartScript ()) {
							var expr = new BinaryOperatorExpression (a.Clone (), BinaryOperatorType.NullCoalescing, other.Clone ());
							script.Replace (conditionalExpression, expr);
						}
					});
				}
				base.VisitConditionalExpression (conditionalExpression);
			}
		}
	}
}
