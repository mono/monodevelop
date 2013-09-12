//
// RazorExplicitExpressionParsingTests.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using NUnit.Framework;
using MonoDevelop.AspNet.Mvc.StateEngine;

namespace MonoDevelop.AspNet.Mvc.StateEngine
{
	[TestFixture]
	public class RazorExplicitExpressionsParsingTests : RazorParsingTests
	{
		[Test]
		public void EmptyExplicitExpression ()
		{
			parser.Parse ("@($)$", () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//@( )");
				parser.AssertNodeIs<RazorExplicitExpression> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test ()]
		public void ExplicitExpression ()
		{
			parser.Parse ("@(Model.Foo$)$", () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//@( )");
				parser.AssertNodeIs<RazorExplicitExpression> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test ()]
		public void ExplicitExpressionWithNestedBrackets ()
		{
			parser.Parse ("@(Foo.Bar(a, b)$)$", () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//@( )");
				parser.AssertNodeIs<RazorExplicitExpression> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test ()]
		public void ExplicitExpressionInHtml ()
		{
			parser.Parse ("<p>@(FooBar$)</p>", () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorExplicitExpression> ();
				parser.AssertPath ("//p/@( )");
			});
		}

		[Test ()]
		public void ExplicitExpressionInText ()
		{
			parser.Parse ("Lorem ipsum@(FooBar$)lorem", () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorExplicitExpression> ();
				parser.AssertPath ("//@( )");
			});
		}
	}
}
