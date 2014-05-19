//
// IPackageExtensionsTests.cs
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
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class IPackageExtensionsTests
	{
		FakePackage package;

		void CreatePackageWithSummary (string summary)
		{
			package = new FakePackage { Summary = summary };
		}

		void CreatePackageWithTitle (string title)
		{
			package = new FakePackage { Title = title };
		}

		[Test]
		public void SummaryOrDescription_PackageHasSummary_ReturnsSummary ()
		{
			CreatePackageWithSummary ("summary");

			string result = package.SummaryOrDescription ();

			Assert.AreEqual ("summary", result);
		}

		[Test]
		public void SummaryOrDescription_PackageHasDescriptionButNullSummary_ReturnsDescription ()
		{
			CreatePackageWithSummary (null);
			package.Description = "description";

			string result = package.SummaryOrDescription ();

			Assert.AreEqual ("description", result);
		}

		[Test]
		public void SummaryOrDescription_PackageHasDescriptionButEmptySummary_ReturnsDescription ()
		{
			CreatePackageWithSummary (String.Empty);
			package.Description = "description";

			string result = package.SummaryOrDescription ();

			Assert.AreEqual ("description", result);
		}

		[Test]
		public void GetName_PackageHasTitle_ReturnsTitle ()
		{
			CreatePackageWithTitle ("title");

			string result = package.GetName ();

			Assert.AreEqual ("title", result);
		}

		[Test]
		public void GetName_PackageHasNullTitle_ReturnsPackageId ()
		{
			CreatePackageWithTitle (null);
			package.Id = "Id";

			string result = package.GetName ();

			Assert.AreEqual ("Id", result);
		}

		[Test]
		public void GetName_PackageHasEmptyStringTitle_ReturnsPackageId ()
		{
			CreatePackageWithTitle (String.Empty);
			package.Id = "Id";

			string result = package.GetName ();

			Assert.AreEqual ("Id", result);
		}
	}
}

