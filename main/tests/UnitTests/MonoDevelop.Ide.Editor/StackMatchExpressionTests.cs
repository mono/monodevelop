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

using System;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor.Util;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Editor.Highlighting;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using System.Threading;
using System.Collections.Immutable;

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
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("bar"), ref tmp).Item1);
		}

		[Test]
		public void TestDashChar ()
		{
			var expr = StackMatchExpression.Parse ("entity.other.attribute-name");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("entity.other.attribute-name"), ref tmp).Item1);
		}


		[Test]
		public void TestSubstringMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo.bar"), ref tmp).Item1);
		}


		[Test]
		public void TestOrMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo, bar");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo"), ref tmp).Item1);
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("bar"), ref tmp).Item1);
		}

		[Test]
		public void TestSubtraction ()
		{
			var expr = StackMatchExpression.Parse ("foo - foo.bar");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo.bar"), ref tmp).Item1);
		}

		[Test]
		public void TestMinusOr ()
		{
			var expr = StackMatchExpression.Parse ("foo - (foo.bar | foo.foobar)");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo.bar"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo.foobar"), ref tmp).Item1);
		}


		// keyword - (source.c keyword.operator | source.c++ keyword.operator | source.objc keyword.operator | source.objc++ keyword.operator), keyword.operator.word
		[Test]
		public void TestStackMatch ()
		{
			var expr = StackMatchExpression.Parse ("foo bar");
			string tmp = "";
			Assert.IsTrue (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("foo").Push ("bar"), ref tmp).Item1);
			Assert.IsFalse (expr.MatchesStack (ImmutableStack<string>.Empty.Push ("bar").Push ("foo"), ref tmp).Item1);
		}
	}
}