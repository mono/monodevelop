// 
// StringIsNullOrEmptyInspector.cs
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
	/// Checks for str == null && str == "" 
	/// Converts to: string.IsNullOrEmpty (str)
	/// </summary>
	public class StringIsNullOrEmptyInspector : IInspector
	{
		static readonly Pattern pattern = new Choice {
			// str == null || str == "" 
			new BinaryOperatorExpression (
				PatternHelper.CommutativeOperator(new AnyNode ("str"), BinaryOperatorType.Equality, new NullReferenceExpression ()),
				BinaryOperatorType.ConditionalOr,
				PatternHelper.CommutativeOperator(new Backreference ("str"), BinaryOperatorType.Equality, new PrimitiveExpression (""))
			),
			// str == "" || str == null
			new BinaryOperatorExpression (
				PatternHelper.CommutativeOperator(new Backreference ("str"), BinaryOperatorType.Equality, new PrimitiveExpression ("")),
				BinaryOperatorType.ConditionalOr,
				PatternHelper.CommutativeOperator(new AnyNode ("str"), BinaryOperatorType.Equality, new NullReferenceExpression ())
			),
		};

		static readonly Pattern negPattern = new Choice {
			// str != null && str != "" 
			new BinaryOperatorExpression (
				PatternHelper.CommutativeOperator(new AnyNode ("str"), BinaryOperatorType.InEquality, new NullReferenceExpression ()),
				BinaryOperatorType.ConditionalAnd,
				PatternHelper.CommutativeOperator(new Backreference ("str"), BinaryOperatorType.InEquality, new PrimitiveExpression (""))
			),
			// str != "" && str != null
			new BinaryOperatorExpression (
				PatternHelper.CommutativeOperator(new Backreference ("str"), BinaryOperatorType.InEquality, new PrimitiveExpression ("")),
				BinaryOperatorType.ConditionalAnd,
				PatternHelper.CommutativeOperator(new AnyNode ("str"), BinaryOperatorType.InEquality, new NullReferenceExpression ())
			),
		};
		
		string title = "Use string.IsNullOrEmpty";

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
			readonly StringIsNullOrEmptyInspector inspector;
			
			public GatherVisitor (BaseRefactoringContext ctx, StringIsNullOrEmptyInspector inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
			{
				base.VisitBinaryOperatorExpression(binaryOperatorExpression);
				Match m = pattern.Match(binaryOperatorExpression);
				bool isNegated = false;
				if (!m.Success) {
					m = negPattern.Match(binaryOperatorExpression);
					isNegated = true;
				}
				if (m.Success) {
					var str = m.Get<Expression>("str").Single();
					AddIssue(binaryOperatorExpression, inspector.Title, delegate {
						using (var script = ctx.StartScript ()) {
							Expression expr = new PrimitiveType ("string").Invoke("IsNullOrEmpty", str.Clone());
							if (isNegated)
								expr = new UnaryOperatorExpression (UnaryOperatorType.Not, expr);
							script.Replace(binaryOperatorExpression, expr);
						}
					});
					return;
				}
			}

		}
	}
}
