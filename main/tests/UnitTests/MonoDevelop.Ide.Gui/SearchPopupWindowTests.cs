//
// SearchPopupWindowTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components.MainToolbar;
using NUnit.Framework;

namespace MonoDevelop.Ide.Gui
{
	[TestFixture()]
	public class SearchPopupWindowTests
	{
		[Test()]
		public void TestEmptyPattern ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("");
			Assert.AreEqual (new SearchPopupSearchPattern (null, "", -1), pattern);
		}

		[Test()]
		public void TestSimplePattern ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("foo");
			Assert.AreEqual (new SearchPopupSearchPattern (null, "foo", -1), pattern);
		}

		[Test()]
		public void TestLineNumber ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("foo:4711");
			Assert.AreEqual (new SearchPopupSearchPattern (null, "foo", 4711), pattern);
		}

		[Test()]
		public void TestEmptySecondPart ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("foo:");
			Assert.AreEqual (new SearchPopupSearchPattern ("foo", "", -1), pattern);
		}

		[Test()]
		public void TestEmptyThirdPart ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("foo:bar:");
			Assert.AreEqual (new SearchPopupSearchPattern ("foo", "bar", 0), pattern);
		}

		
		[Test()]
		public void TestLineNumberOnly ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern (":4711");
			Assert.AreEqual (new SearchPopupSearchPattern (null, null, 4711), pattern);
		}

		[Test()]
		public void TestCategory ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("cat:foo");
			Assert.AreEqual (new SearchPopupSearchPattern ("cat", "foo", -1), pattern);
		}

		[Test()]
		public void TestCategoryAndLineNumber ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("cat:foo:1337");
			Assert.AreEqual (new SearchPopupSearchPattern ("cat", "foo", 1337), pattern);
		}

		[Test()]
		public void TestInvalidLineNumber ()
		{
			var pattern = SearchPopupSearchPattern.ParsePattern ("cat:foo:bar");
			Assert.AreEqual (new SearchPopupSearchPattern ("cat", "foo", 0), pattern);
		}
	}
}

