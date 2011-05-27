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
using MonoDevelop.CSharp.QuickFix;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Analysis
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
				return true;
			} else {
				// a == null ? other : a
				if (condition.Operator == BinaryOperatorType.Equality)
					return true;
				// a != null ? other : a
				return false;
			}
		}
		
		public override void Attach (ObservableAstVisitor visitior)
		{
			visitior.ConditionalExpressionVisited += delegate(ConditionalExpression node) {
				foreach (var match in Matches) {
					if (match.IsMatch (node) && IsCandidate (node)) {
						results.Add (new FixableResult (
							new DomRegion (node.StartLocation.Line, node.StartLocation.Column, node.EndLocation.Line, node.EndLocation.Column),
							GettextCatalog.GetString ("'?:' expression can be converted to '??' expression"),
							ResultLevel.Suggestion, ResultCertainty.High, ResultImportance.Medium,
							new UseNullableOperatorFix (node)));
						
					}
				}
			};
		}
		
		internal class UseNullableOperatorFix : IAnalysisFix
		{
			public ConditionalExpression ConditionalExpression { get; private set; }

			public UseNullableOperatorFix (ConditionalExpression conditionalExpression)
			{
				this.ConditionalExpression = conditionalExpression;
			}
			
			public Tuple<Expression, Expression> GetExpressions ()
			{
				var condition = ConditionalExpression.Condition as BinaryOperatorExpression;
				var compareNode = condition.Left is NullReferenceExpression ? condition.Right : condition.Left;
			
				if (compareNode.IsMatch (ConditionalExpression.TrueExpression)) {
					// a != null ? a : other
					return new Tuple<Expression, Expression> (ConditionalExpression.TrueExpression, ConditionalExpression.FalseExpression);
				}
				
				// a == null ? other : a
				return new Tuple<Expression, Expression> (ConditionalExpression.FalseExpression, ConditionalExpression.TrueExpression);
			}
			
			#region IAnalysisFix implementation
			public string FixType {
				get {
					return "ConditionalToNullCoalescingFix";
				}
			}
			#endregion
			
		}
	}
	
	class ConditionalToNullCoalescingFixHandler : IFixHandler
	{
		#region IFixHandler implementation
		public System.Collections.Generic.IEnumerable<IAnalysisFixAction> GetFixes (MonoDevelop.Ide.Gui.Document doc, object fix)
		{
			yield return new ConditionalToNullCoalescingFixAction () {
				document = doc,
				fix = fix as ConditionalToNullCoalescingInspector.UseNullableOperatorFix
			};
				
		}
		#endregion
	}
	
	class ConditionalToNullCoalescingFixAction : IAnalysisFixAction
	{
		internal MonoDevelop.Ide.Gui.Document document;
		internal ConditionalToNullCoalescingInspector.UseNullableOperatorFix fix;
			
		#region IAnalysisFixAction implementation
		public void Fix ()
		{
			var expressions = fix.GetExpressions ();
			
			Expression expr = new BinaryOperatorExpression (expressions.Item1.Clone (), BinaryOperatorType.NullCoalescing, expressions.Item2.Clone ());
			
			string text = CSharpQuickFix.OutputNode (document, expr, "").Trim ();
				
			int offset = document.Editor.LocationToOffset (fix.ConditionalExpression.StartLocation.Line, fix.ConditionalExpression.StartLocation.Column);
			int endOffset = document.Editor.LocationToOffset (fix.ConditionalExpression.EndLocation.Line, fix.ConditionalExpression.EndLocation.Column);
				
			document.Editor.Replace (offset, endOffset - offset, text);
			document.Editor.Document.CommitUpdateAll ();
		}
			
		public string Label {
			get {
				return GettextCatalog.GetString ("Convert to '??' expression");
			}
		}
		#endregion
	}
}

