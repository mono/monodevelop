//
// RazorImplicitExpressionsParsingTests.cs
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
	public class RazorImplicitExpressionsParsingTests : RazorParsingTests
	{
		// Can't use $ as a trigger, because implicit expression terminates

		[Test]
		public void ImplicitExpression ()
		{
			parser.Parse ("@FoobarX", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorImplicitExpression> ();
				parser.AssertPath ("//@");
			});
		}

		[Test]
		public void ImplicitExpressionMethodCall ()
		{
			parser.Parse ("@Foo.Bar()X", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorImplicitExpression> ();
				parser.AssertPath ("//@");
			});
		}

		[Test]
		public void ImplicitExpressionArray ()
		{
			parser.Parse ("@Foo[][]X", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorImplicitExpression> ();
				parser.AssertPath ("//@");
			});
		}

		[Test]
		public void ImplicitExpressionQuotes ()
		{
			parser.Parse ("@ViewData[\"Foo$\"]", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorImplicitExpression> ();
				parser.AssertPath ("//@");
			});
		}

		[Test]
		public void ImplicitExpressionTerminatesAtWhitespace ()
		{
			parser.Parse ("@Foo X", 'X', () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void ImplicitExpressionNotTerminatesAtWhitespaceInsideBrackets ()
		{
			parser.Parse ("@Html.Raw(a, bX, c) X", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorImplicitExpression> ();
				parser.AssertPath ("//@");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void ImplicitExpressionTerminatesAtTag ()
		{
			parser.Parse ("@Foo<p>X", 'X', () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("//p");
			});
		}

		[Test]
		public void ImplicitExpressionTerminatesAtTransition ()
		{
			parser.Parse ("@Foo@X", 'X', () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void ImplicitExpressionInHtml ()
		{
			parser.Parse ("<body><p>@FoXo</p></body>X", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertNodeIs<RazorImplicitExpression> ();
				parser.AssertPath ("//body/p/@");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void EmailAddressNotRecognizedAsExpression ()
		{
			parser.Parse ("foo@barX.com", 'X', () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}
	}
}
