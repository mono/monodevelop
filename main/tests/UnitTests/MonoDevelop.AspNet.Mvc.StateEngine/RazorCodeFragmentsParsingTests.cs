//
// RazorCodeFragmentsParsingTests.cs
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
	public class RazorCodeFragmentsParsingTests : RazorParsingTests
	{
		// Tests that are common for code fragments, i.e. code blocks, directives and statements

		[Test]
		public void StateNotSwitchedToHtmlTagInGenerics ()
		{
			parser.Parse ("@{ IEnumerable<string$> foo = new List<$string> (); }", () => {
				parser.AssertStateIs<RazorCodeFragmentState> ();
			}, () => {
				parser.AssertStateIs<RazorCodeFragmentState> ();
				parser.AssertPath ("//@{ }");
			});
		}

		[Test]
		public void StateNotSwitchedToHtmlTagInBrackets ()
		{
			parser.Parse ("@foreach (var item in Model) { if (item <$ 1) { } }", () => {
				parser.AssertStateIs<RazorCodeFragmentState> ();
				parser.AssertPath ("//@foreach");
			});
		}

		[Test]
		public void ExplicitExpressionInCodeFragment ()
		{
			parser.Parse ("@section Foo { <p>@(Bar.$Baz)</p> }", () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//@section/p/@( )");
			});
		}

		[Test]
		public void ImplicitExpressionInCodeFragment ()
		{
			parser.Parse ("@{ <p>@Foo.Xbar</p> }", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//@{ }/p/@");
			});
		}

		[Test]
		public void NestedCodeFragments ()
		{
			parser.Parse (
			@"@foreach (var item in Model) {
				<foo>
					@if (true) {
						@{
							int x = 1;
							<p>@(item.Value + xX)</p>
						}
					}
					else {
						<p>@BarX</p>
					}
				</foo>
			}", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//@foreach/foo/@if/@{ }/p/@( )");
			  }, () => {
				  parser.AssertStateIs<RazorExpressionState> ();
				  parser.AssertPath ("//@foreach/foo/@else/p/@");
			  });
		}

		[Test]
		public void StateSwitchesBetweenHtmlAndCode ()
		{
			parser.Parse (
			@"<html>
			<body>
				@{
					<ul>
						@foreach (var item in Model) {
							<li>@item.ValueX</li>
						}
					</ul>
				}
			</body>
			</html>", 'X', () => {
				parser.AssertStateIs<RazorExpressionState> ();
				parser.AssertPath ("//html/body/@{ }/ul/@foreach/li/@");
			});
		}
	}
}
