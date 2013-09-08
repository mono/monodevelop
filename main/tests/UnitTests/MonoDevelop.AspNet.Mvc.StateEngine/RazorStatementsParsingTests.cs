//
// RazorStatementsParsingTests.cs
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
using MonoDevelop.Xml.StateEngine;

namespace MonoDevelop.AspNet.Mvc.StateEngine
{
	[TestFixture]
	public class RazorStatementsParsingTests : RazorParsingTests
	{
		[Test]
		public void ForStatement ()
		{
			parser.Parse ("@for(int i = 0; i++; i < length) { foo($); }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@for");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void ForeachStatement ()
		{
			parser.Parse ("@foreach (var item in Model) { foo($); }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@foreach");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void SwitchStatement ()
		{
			parser.Parse ("@switch (foo) { case bar: $break; }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@switch");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void WhileStatement ()
		{
			parser.Parse ("@while (foo !=$ bar) { foo (); }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@while");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void LockStatement ()
		{
			parser.Parse ("@lock (foo) { bar()$; }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@lock");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void DoStatement ()
		{
			parser.Parse ("@do { bar()$; } while (true)$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@do");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void UsingStatement ()
		{
			parser.Parse ("@using (resource) { foo ($); }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@using");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void IfStatementRemainsInStatementStateIfCanBeContinued ()
		{
			parser.Parse ("@if(true) { $foo(); }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@if");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("/");
				parser.AssertNodeIs<XDocument> (); // if statement is ended
			});
		}

		[Test]
		public void IfStatementReturnsToParentStateWhenNotContinued ()
		{
			parser.Parse ("@if(true) { $foo(); } bar$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@if");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void IfStatementCanBeContinuedWithElseIf ()
		{
			parser.Parse ("@if (true) { $foo(); } else if (false) { $foo(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@if");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@else if");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void IfStatementCanBeContinuedWithElse ()
		{
			parser.Parse ("@if (true) { $foo(); } else { $foo(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@if");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@else");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void ElseIfStatementCanBeContinuedWithElse ()
		{
			parser.Parse ("@if (true) { foo(); } else if (false) { $foo(); } else { bar $(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@else if");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@else");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void ElseIfStatementCanBeContinuedWithElseIf ()
		{
			parser.Parse ("@if (true) { foo(); } else if (true) { $foo(); } else if (false) { bar $(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@else if");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@else if");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void TryStatementRemainsInStatementStateIfCanBeContinued ()
		{
			parser.Parse ("@try { $foo(); }$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@try");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("/");
				parser.AssertNodeIs<XDocument> (); // try statement is ended
			});
		}

		[Test]
		public void TryStatementReturnsToParentStateWhenNotContinued ()
		{
			parser.Parse ("@try { $foo(); } bar$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@try");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void TryStatementCanBeContinuedWithCatch ()
		{
			parser.Parse ("@try { $foo(); } catch(Exception e) { $Foo(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@try");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@catch");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void TryStatementCanBeContinuedWithFinally ()
		{
			parser.Parse ("@try { $foo(); } finally { $Foo(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@try");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@finally");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void CatchStatementCanBeContinuedWithCatchOrFinally ()
		{
			parser.Parse ("@try { foo(); } catch { $Foo(); } catch (Exception e) { $Foo(); } finally { $Foo(); }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@catch");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@catch");
				parser.AssertNodeIs<RazorStatement> ();
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@finally");
				parser.AssertNodeIs<RazorStatement> ();
			});
		}

		[Test]
		public void StatementWithNestedBrackets ()
		{
			parser.Parse ("@foreach (var item in Foo) { { { $ } } $}$", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@foreach");
			}, () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@foreach");
			}, () => {
				parser.AssertStateIs<RazorFreeState> ();
				parser.AssertPath ("/");
			});
		}

		[Test]
		public void StatementInHtml ()
		{
			parser.Parse ("<foo><bar>@if (true) { Foo $ bar }</bar></foo>", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//foo/bar/@if");
			});
		}

		[Test]
		public void StatementSupportsHtml ()
		{
			parser.Parse ("@while (true) {<foo><bar>$</bar></foo> }", () => {
				parser.AssertStateIs<RazorStatementState> ();
				parser.AssertPath ("//@while/foo/bar");
			});
		}
	}
}
