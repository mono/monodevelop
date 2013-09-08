//
// RazorDirectivesParsingTests.cs
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

using MonoDevelop.AspNet.Mvc.StateEngine;
using NUnit.Framework;

namespace MonoDevelop.AspNet.Mvc.StateEngine
{
	[TestFixture]
	public class RazorDirectivesParsingTests : RazorParsingTests
	{
		[Test]
		public void InheritsDirective ()
		{
			parser.Parse ("@inherits Class.Foo.Bar$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@inherits");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void ModelDirective ()
		{
			parser.Parse ("@model Foo.Bar$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@model");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void SessionstateDirective ()
		{
			parser.Parse ("@sessionstate Bar$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@sessionstate");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void LayoutDirective ()
		{
			parser.Parse ("@layout Foo Bar$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@layout");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void UsingDirective ()
		{
			parser.Parse ("@using System.Foo.Bar$\n", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@using");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void UsingDirectiveAsTypeAlias ()
		{
			parser.Parse ("@using Foo = System.Foo.Bar$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@using");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void UsingDirectivesCanBeInOneLine ()
		{
			parser.Parse ("@using Foo.Bar$  @using Bar.Foo$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@using");
			}, () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@using");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void SimpleDirectiveSupportsGenerics ()
		{
			parser.Parse ("@inherits Web.Foo<IEnumerable<Model<Bar>>$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@inherits");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void SimpleDirectiveSupportsArray ()
		{
			parser.Parse ("@inherits Web.Foo[][]$\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@inherits");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void SimpleDirectiveSupportsSpacesInsideName ()
		{
			parser.Parse ("@model    Web.Foo [] [] $\n$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@model");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void FunctionsDirective ()
		{
			parser.Parse ("@functions ${ foo();$\n bar(); }$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@functions");
			}, () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@functions");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void SectionDirective ()
		{
			parser.Parse ("@section $Section { <p>Foo $ Bar</p> }$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@section");
			}, () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@section/p");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void HelperDirective ()
		{
			parser.Parse ("@helper $Strong(string value) {\n foo($); }$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@helper");
			}, () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@helper");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void DirectiveWithNestedBrackets ()
		{
			parser.Parse ("@helper Strong (string value) { { { $ } } $}$", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@helper");
			}, () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@helper");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void DirectiveInHtml ()
		{
			parser.Parse ("<foo><bar>@section Section { Foo $ bar }</bar></foo>", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//foo/bar/@section");
			});
		}

		[Test]
		public void DirectiveSupportsHtml ()
		{
			parser.Parse ("@section Section {<foo><bar>$</bar></foo> }", () => {
				parser.AssertStateIs<RazorDirectiveState> ();
				parser.AssertPath ("//@section/foo/bar");
			});
		}
	}
}
