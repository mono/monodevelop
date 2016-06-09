//
// CoreExtensionsTests.cs
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
using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class CoreExtensionsTests
	{
		[Test]
		public void ConcatWorks ()
		{
			string toAdd = "Test String";

			var initial = Enumerable.Repeat (toAdd, 4);
			var expected = Enumerable.Repeat (toAdd, 5);

			Assert.That (expected, Is.EquivalentTo (initial.Concat (toAdd)));
		}

		[TestCase ("item1", "item2", "item1", 0)]
		[TestCase ("item1", "item2", "item2", 1)]
		[TestCase ("item1", "item2", "item3", -1)]
		public void IndexOfWorks (string toAdd1, string toAdd2, string toFind, int expectedIndex)
		{
			var toSearch = Enumerable.Empty<string> ().Concat (toAdd1).Concat (toAdd2);

			Assert.AreEqual (expectedIndex, toSearch.IndexOf (toFind));
		}

		[TestCase ("item1", "item2", "item1", 0)]
		[TestCase ("item1", "item2", "item2", 1)]
		[TestCase ("item1", "item2", "item3", -1)]
		public void FindIndexWorks (string toAdd1, string toAdd2, string toFind, int expectedIndex)
		{
			var toSearch = Enumerable.Empty<string> ().Concat (toAdd1).Concat (toAdd2);

			Assert.AreEqual (expectedIndex, toSearch.FindIndex (i => i == toFind));
		}
	}
}

