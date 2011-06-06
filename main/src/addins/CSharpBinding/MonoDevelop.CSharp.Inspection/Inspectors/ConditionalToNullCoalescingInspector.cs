// 
// CheckConditionalExpression.cs
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Core;
using MonoDevelop.AnalysisCore;
using MonoDevelop.CSharp.ContextAction;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Inspection
{
	public class ConditionalToNullCoalescingInspector : CSharpInspector
	{
		static ConditionalExpression[] Matches;

		public ConditionalToNullCoalescingInspector ()
		{
			Matches = new [] { new ConditionalExpression (new BinaryOperatorExpression (new NullReferenceExpression (), BinaryOperatorType.Equality, new AnyNode ()), new AnyNode (), new AnyNode ()),
				new ConditionalExpression (new BinaryOperatorExpression (new AnyNode (), BinaryOperatorType.Equality, new NullReferenceExpression ()), new AnyNode (), new AnyNode ()),
				new ConditionalExpression (new BinaryOperatorExpression (new NullReferenceExpression (), BinaryOperatorType.InEquality, new AnyNode ()), new AnyNode (), new AnyNode ()),
				new ConditionalExpression (new BinaryOperatorExpression (new AnyNode (), BinaryOperatorType.InEquality, new NullReferenceExpression ()), new AnyNode (), new AnyNode ()),
			};
		}
		
		public bool IsCandidate (ConditionalExpression node)
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
		
		public Tuple<Expression, Expression> GetExpressions (ConditionalExpression cond)
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
		
		protected override void Attach (ObservableAstVisitor<InspectionData, object> visitior)
		{
			visitior.ConditionalExpressionVisited += delegate(ConditionalExpression node, InspectionData data) {
				foreach (var match in Matches) {
					if (match.IsMatch (node) && IsCandidate (node)) {
						
						AddResult (data,
							new DomRegion (node.StartLocation.Line, node.StartLocation.Column, node.EndLocation.Line, node.EndLocation.Column),
							GettextCatalog.GetString ("Convert to '??' expression"),
							delegate {
								var expressions = GetExpressions (node);
										
								Expression expr = new BinaryOperatorExpression (expressions.Item1.Clone (), BinaryOperatorType.NullCoalescing, expressions.Item2.Clone ());
//								node.Replace (data.Document, expr);
							}
						);
					}
				}
			};
		}
	}
}

