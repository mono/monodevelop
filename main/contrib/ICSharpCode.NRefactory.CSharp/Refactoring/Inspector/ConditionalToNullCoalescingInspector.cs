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
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Checks for  obj != null ? obj : <expr> 
	/// Converts to: obj ?? <expr>
	/// </summary>
	public class ConditionalToNullCoalescingInspector : IInspector
	{
		static ConditionalExpression[] Matches;

		string title = "Convert to '??' expression";

		public string Title {
			get {
				return title;
			}
			set {
				title = value;
			}
		}		

		public ConditionalToNullCoalescingInspector ()
		{
			Matches = new [] {
				new ConditionalExpression (new BinaryOperatorExpression (new NullReferenceExpression (), BinaryOperatorType.Equality, new AnyNode ()), new AnyNode (), new AnyNode ()),
				new ConditionalExpression (new BinaryOperatorExpression (new AnyNode (), BinaryOperatorType.Equality, new NullReferenceExpression ()), new AnyNode (), new AnyNode ()),
				new ConditionalExpression (new BinaryOperatorExpression (new NullReferenceExpression (), BinaryOperatorType.InEquality, new AnyNode ()), new AnyNode (), new AnyNode ()),
				new ConditionalExpression (new BinaryOperatorExpression (new AnyNode (), BinaryOperatorType.InEquality, new NullReferenceExpression ()), new AnyNode (), new AnyNode ()),
			};
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

			public override void VisitConditionalExpression (ConditionalExpression conditionalExpression)
			{
				foreach (var match in Matches) {
					if (match.IsMatch (conditionalExpression) && IsCandidate (conditionalExpression)) {
						AddIssue (conditionalExpression,
						               inspector.Title,
						               delegate {
							using (var script = ctx.StartScript ()) {
								var expressions = SortExpressions (conditionalExpression);
								var expr = new BinaryOperatorExpression (expressions.Item1.Clone (), BinaryOperatorType.NullCoalescing, expressions.Item2.Clone ());
								script.Replace (conditionalExpression, expr);
							}
						});
					}
				}
				base.VisitConditionalExpression (conditionalExpression);
			}
		}

		static bool IsCandidate (ConditionalExpression node)
		{
			var condition = node.Condition as BinaryOperatorExpression;
			var compareNode = condition.Left is NullReferenceExpression ? condition.Right : condition.Left;
			
			
			if (compareNode.IsMatch (node.TrueExpression)) {
				// a == null ? a : other
				if (condition.Operator == BinaryOperatorType.Equality) 
					return false;
				// a != null ? a : other
				return compareNode.IsMatch (node.TrueExpression);
			} else {
				// a == null ? other : a
				if (condition.Operator == BinaryOperatorType.Equality)
					return compareNode.IsMatch (node.FalseExpression);
				// a != null ? other : a
				return false;
			}
		}

		static Tuple<Expression, Expression> SortExpressions (ConditionalExpression cond)
		{
			var condition = cond.Condition as BinaryOperatorExpression;
			var compareNode = condition.Left is NullReferenceExpression ? condition.Right : condition.Left;

			if (compareNode.IsMatch (cond.TrueExpression)) {
				// a != null ? a : other
				return new Tuple<Expression, Expression> (cond.TrueExpression, cond.FalseExpression);
			}

			// a == null ? other : a
			return new Tuple<Expression, Expression> (cond.FalseExpression, cond.TrueExpression);
		}
	}
}
