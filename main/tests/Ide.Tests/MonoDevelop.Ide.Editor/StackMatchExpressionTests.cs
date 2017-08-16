//
// StackMatchExpressionTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class StackMatchExpressionTests
	{
		[Test]
		public void TestExactMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("foo"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (new ScopeStack ("bar"), ref tmp).Item1);
		}

		[Test]
		public void TestDashChar ()
		{
			var expr = StackMatchExpression.Parse ("entity.other.attribute-name");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("entity.other.attribute-name"), ref tmp).Item1);
		}


		[Test]
		public void TestSubstringMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("foo.bar"), ref tmp).Item1);
		}


		[Test]
		public void TestOrMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo, bar");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("foo"), ref tmp).Item1);
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("bar"), ref tmp).Item1);
		}

		[Test]
		public void TestSubtraction ()
		{
			var expr = StackMatchExpression.Parse ("foo - foo.bar");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("foo"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (new ScopeStack ("foo.bar"), ref tmp).Item1);
		}

		[Test]
		public void TestMinusOr ()
		{
			var expr = StackMatchExpression.Parse ("foo - (foo.bar | foo.foobar)");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("foo"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (new ScopeStack ("foo.bar"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (new ScopeStack ("foo.foobar"), ref tmp).Item1);
		}


		// keyword - (source.c keyword.operator | source.c++ keyword.operator | source.objc keyword.operator | source.objc++ keyword.operator), keyword.operator.word
		[Test]
		public void TestStackMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo bar");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("foo").Push ("bar"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (new ScopeStack ("bar").Push ("foo"), ref tmp).Item1);
		}


		/// <summary>
		/// Bug 45378 - [Text Mate] Syntax Highlighting not works properly for XML file while applying "Tomorrow.tmTheme" to Xamarin Studio.
		/// </summary>
		[Test]
		public void TestBug45378 ()
		{
			var expr = StackMatchExpression.Parse ("string, constant.other.symbol, entity.other.inherited-class, markup.heading, markup.inserted.git_gutter");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (new ScopeStack ("text.xml").Push("meta.tag.xml").Push ("string.quoted.double.xml"), ref tmp).Item1);
		}
	}
}