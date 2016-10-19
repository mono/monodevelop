//
// ConditionTokenizer.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;

namespace MonoDevelop.Projects.MSBuild.Conditions
{
	[TestFixture]
	public class ConditionParserTests
	{
		static IEnumerable<Type> Enumerate (ConditionExpression node)
		{
			yield return node.GetType ();
			var and = node as ConditionAndExpression;
			if (and != null) {
				foreach (var left in Enumerate (and.Left))
					yield return left;
				foreach (var right in Enumerate (and.Right))
					yield return right;
			}
			var or = node as ConditionOrExpression;
			if (or != null) {
				foreach (var left in Enumerate (or.Left))
					yield return left;
				foreach (var right in Enumerate (or.Right))
					yield return right;
			}
		}

		public class ParseTestCase
		{
			public string Condition;
			public Type[] Expected;
		}

		public ParseTestCase [] ParseTestCases = {
			new ParseTestCase {
				Condition = " '$(a)' and '!$(b)' or '$(c) + $(d) == 5' ",
				Expected = new[] {
					typeof(ConditionAndExpression), typeof(ConditionFactorExpression),
					typeof(ConditionOrExpression), typeof(ConditionFactorExpression), typeof(ConditionFactorExpression), typeof(ConditionFactorExpression), }
			},
		};

		[TestCaseSource ("ParseTestCases")]
		public void ParseCondition (ParseTestCase testCase)
		{
			ConditionExpression result = ConditionParser.ParseCondition (testCase.Condition);

			int i = 0;
			foreach (var node in Enumerate (result)) {
				Assert.AreEqual (testCase.Expected [i], node, "At {0}", i);
				i++;
			}
		}
	}
}
