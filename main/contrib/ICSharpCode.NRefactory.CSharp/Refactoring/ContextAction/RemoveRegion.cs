// 
// RemoveRegion.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class RemoveRegion : IContextAction
	{
		public bool IsValid (RefactoringContext context, CancellationToken cancellationToken)
		{
			return GetDirective (context) != null;
		}
		
		public void Run (RefactoringContext context)
		{
			var directive = GetDirective (context);
			var endDirective = DirectiveSearcher.GetEndRegion (context.Unit, directive);
			if (endDirective == null)
				return;
			using (var script = context.StartScript ()) {
				script.Remove (directive);
				script.Remove (endDirective);
			}
		}
		
		class DirectiveSearcher : DepthFirstAstVisitor
		{
			readonly PreProcessorDirective regionDirective;
			bool searchDirectives = false;
			int depth;
			PreProcessorDirective endregion;
			
			DirectiveSearcher (PreProcessorDirective regionDirective)
			{
				if (regionDirective == null)
					throw new ArgumentNullException ("regionDirective");
				this.regionDirective = regionDirective;
			}
			
			public static PreProcessorDirective GetEndRegion (CompilationUnit unit, PreProcessorDirective regionDirective)
			{
				var visitor = new DirectiveSearcher (regionDirective);
				unit.AcceptVisitor (visitor);
				return visitor.endregion;
			}
			
			protected override void VisitChildren (AstNode node)
			{
				if (endregion != null)
					return;
				if (!searchDirectives && !regionDirective.Ancestors.Any (a => a == node))
					return;
				base.VisitChildren (node);
			}
			
			public override void VisitPreProcessorDirective (PreProcessorDirective preProcessorDirective)
			{
				if (searchDirectives) {
					if (preProcessorDirective.Type == PreProcessorDirectiveType.Region) {
						depth++;
					} else if (preProcessorDirective.Type == PreProcessorDirectiveType.Endregion) {
						depth--;
						if (depth == 0) {
							endregion = preProcessorDirective;
							searchDirectives = false;
						}
					}
				} else if (preProcessorDirective == regionDirective) {
					searchDirectives = true;
					depth = 1;
				}
				
				base.VisitPreProcessorDirective (preProcessorDirective);
			}
		}
		
		static PreProcessorDirective GetDirective (RefactoringContext context)
		{
			var directive = context.GetNode<PreProcessorDirective> ();
			if (directive == null || directive.Type != PreProcessorDirectiveType.Region)
				return null;
			return directive;
		}
	}
}

