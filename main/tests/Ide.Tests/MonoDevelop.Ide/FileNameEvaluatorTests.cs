//
// FileNameEvaluator.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using MonoDevelop.Ide.Extensions;
using NUnit.Framework;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public class FileNameEvaluatorTests
	{
		[Test]
		[TestCaseSource (nameof(Patterns))]
		public void SimplePattern (string[] patterns, string[] matches, string[] nonMatches)
		{
			var fileNameEvalutor = FileNameEvalutor.CreateFileNameEvaluator (patterns, ',');
			foreach (var p in matches)
				Assert.IsTrue (fileNameEvalutor.SupportsFile (p), "File name '" + p + "' did not match");
			foreach (var p in nonMatches)
				Assert.IsFalse (fileNameEvalutor.SupportsFile (p), "File name '" + p + "' should not match");
		}

		public static object [] Patterns = {
			new TestCaseData (
				new string[] { "*.ex1" },
				new string[] { "foo.ex1", "foo.bar.ex1", ".ex1", "FOO.EX1", "FOO.eX1" },
				new string[] { "ex1", ". ex1" }
			).SetName ("*.ex1"),
			new TestCaseData (
				new string[] { "*.ex1, *.ex2,*ex3", "*.ex4" },
				new string[] { "foo.ex1", "bar.ex2", "foo.ex3", ".ex1" },
				new string[] { "ex1", ". ex2" }
			).SetName ("*.ex1, *.ex2,*ex3, *.ex4"),
			new TestCaseData (
				new string[] { "a*b?c" },
				new string[] { "ab1c", "axxxxbbc", "abcc" },
				new string[] { "ex1", "abc" }
			).SetName ("a*b?c"),
			new TestCaseData (
				new string[] { "*.*.txt" },
				new string[] { "aaa.bbb.txt", "AA.BB.TXT" },
				new string[] { "a.txt", }
			).SetName ("*.*.txt"),
			new TestCaseData (
				new string[] { "*?.txt" },
				new string[] { "a.txt", "foo..txt" },
				new string[] { ".txt", }
			).SetName ("*?.txt"),
			new TestCaseData (
				new string[] { "test" },
				new string[] { "test" },
				new string[] { "base.test", }
			).SetName ("test"),
		};
	}
}
