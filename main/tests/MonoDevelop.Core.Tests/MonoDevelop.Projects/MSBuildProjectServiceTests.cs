//
// MSBuildProjectServiceTests.cs
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
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildProjectServiceTests
	{
		[TestCase ("a(b)", "a%28b%29")]
		[TestCase ("a%bcd", "a%25bcd")]
		[TestCase ("%a%b%c%d%", "%25a%25b%25c%25d%25")]
		[TestCase ("%%%", "%25%25%25")]
		[TestCase ("abc", "abc")]
		public void EscapeString (string input, string expected)
		{
			Assert.AreEqual (expected, MSBuildProjectService.EscapeString (input));
		}

		[TestCase ("a%28b%29", "a(b)")]
		[TestCase ("a%25bcd", "a%bcd")]
		[TestCase ("%25a%25b%25c%25d%25", "%a%b%c%d%")]
		[TestCase ("%25%25%25", "%%%")]
		[TestCase ("abc", "abc")]
		public void UnescapeString (string input, string expected)
		{
			Assert.AreEqual (expected, MSBuildProjectService.UnscapeString (input));
		}
	}
}
