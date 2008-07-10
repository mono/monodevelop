//
// ExpressionResult.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Projects.Dom
{
	
	
	public class ExpressionResult
	{
		string expression;
		ExpressionContext expressionContext;
		DomRegion region;
		
		public string Expression {
			get {
				return expression;
			}
		}

		public ExpressionContext ExpressionContext {
			get {
				return expressionContext;
			}
			set {
				expressionContext = value;
			}
		}

		public DomRegion Region {
			get {
				return region;
			}
		}
		
		public ExpressionResult (string expression) : this (expression, DomRegion.Empty, null)
		{
		}
		
		public ExpressionResult (string expression, DomRegion region, ExpressionContext expressionContext)
		{
			this.expression = expression;
			this.region = region;
			this.expressionContext = expressionContext;
		}
		
		public ExpressionResult (string expression, ExpressionContext expressionContext) : this (expression, DomRegion.Empty, expressionContext)
		{
		}
		
		public override string ToString ()
		{
			return String.Format ("[ExpressionResult:Expression={0}, ExpressionContext={1}, Region={2}]",
			                      this.expression,
			                      this.expressionContext,
			                      this.region);
		}
	}
}
