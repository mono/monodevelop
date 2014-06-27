//
// PackageSearchCriteriaTests.cs
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
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageSearchCriteriaTests
	{
		PackageSearchCriteria search;

		void CreateSearch (string text)
		{
			search = new PackageSearchCriteria (text);
		}

		[TestCase ("NUnit", false)]
		[TestCase ("NUnit version:*", true)]
		[TestCase ("NUnit version:", true)]
		[TestCase ("NUnit ver", false)]
		[TestCase ("NUnit VERSION:*", true)]
		[TestCase ("NUnit aversion:", false)]
		[TestCase ("NUnit Version:*", true)]
		[TestCase ("  NUnit   Version:  ", true)]
		[TestCase ("  NUnit   Version:*  ", true)]
		public void IsPackageVersionSearch (string searchText, bool expectedResult)
		{
			CreateSearch (searchText);

			bool result = search.IsPackageVersionSearch;

			Assert.AreEqual (expectedResult, result);
		}

		[TestCase ("NUnit version:", "1.0", true)]
		[TestCase ("NUnit version:*", "1.0", true)]
		[TestCase ("NUnit version:1.0", "1.0", true)]
		[TestCase ("NUnit version:1.0", "1.1", false)]
		[TestCase ("NUnit version:1", "1.0", true)]
		[TestCase ("NUnit version:1", "1.1", true)]
		[TestCase ("NUnit version:1", "1.9", true)]
		[TestCase ("NUnit version:1", "1.9.2", true)]
		[TestCase ("NUnit version:1", "2.0", false)]
		[TestCase ("   NUnit    version:1   ", "2.0", false)]
		[TestCase ("   NUnit    version:1   ", "1.9", true)]
		public void IsVersionMatch (string searchText, string versionToMatch, bool expectedResult)
		{
			CreateSearch (searchText);

			bool result = search.IsVersionMatch (new SemanticVersion (versionToMatch));

			Assert.AreEqual (expectedResult, result);
		}
	}
}

