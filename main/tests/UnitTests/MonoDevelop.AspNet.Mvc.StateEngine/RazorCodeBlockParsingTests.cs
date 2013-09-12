//
// RazorCodeBlockParsingTests.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MonoDevelop.AspNet.Mvc.StateEngine;

namespace MonoDevelop.AspNet.Mvc.StateEngine
{
	[TestFixture]
	public class RazorCodeBlockParsingTests : RazorParsingTests
	{
		[Test]
		public void EmptyCodeBlock ()
		{
			parser.Parse ("@{$}$", () => {
				parser.AssertStateIs<RazorCodeBlockState> ();
				parser.AssertPath ("//@{ }");
				parser.AssertNodeIs<RazorCodeBlock> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void CodeBlock ()
		{
			parser.Parse ("@{Foo Bar() \n\n $ Baz}$", () => {
				parser.AssertStateIs<RazorCodeBlockState> ();
				parser.AssertPath ("//@{ }");
				parser.AssertNodeIs<RazorCodeBlock> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void CodeBlockInHtml ()
		{
			parser.Parse ("<foo><bar>@{ $ }</bar></foo>", () => {
				parser.AssertStateIs<RazorCodeBlockState> ();
				parser.AssertPath ("//foo/bar/@{ }");
			});
		}

		[Test]
		public void CodeBlockSupportsHtml ()
		{
			parser.Parse ("@{<foo><bar>$</bar></foo> }", () => {
				parser.AssertStateIs<RazorCodeBlockState> ();
				parser.AssertPath ("//@{ }/foo/bar");
			});
		}

		[Test]
		public void CodeBlockWithNestedBrackets ()
		{
			parser.Parse ("@{ { { $ } } $}", () => {
				parser.AssertStateIs<RazorCodeBlockState> ();
				parser.AssertPath ("//@{ }");
			}, () => {
				parser.AssertStateIs<RazorCodeBlockState> ();
				parser.AssertPath ("//@{ }");
			});
		}
	}
}
