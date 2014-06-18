//
// PageCollectionAssert.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.PackageManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public static class PageCollectionAssert
	{
		public static void AreEqual (IEnumerable<Page> expectedPages, IEnumerable<Page> actualPages)
		{
			List<string> convertedExpectedPages = ConvertToStrings (expectedPages);
			List<string> convertedActualPages = ConvertToStrings (actualPages);

			CollectionAssert.AreEqual (convertedExpectedPages, convertedActualPages);
		}

		static List<string> ConvertToStrings (IEnumerable<Page> pages)
		{
			List<string> pagesAsText = new List<string> ();
			foreach (Page page in pages) {
				pagesAsText.Add (GetPageAsString (page));
			}
			return pagesAsText;
		}

		static string GetPageAsString (Page page)
		{
			return String.Format ("Page: Number: {0}, IsSelected: {1}",
				page.Number,
				page.IsSelected);
		}
	}
}
