//
// RecentPackageInfoTests.cs
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
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class RecentPackageInfoTests
	{
		[Test]
		public void ToString_IdAndVersionSpecified_ContainsIdAndVersion ()
		{
			var recentPackageInfo = new RecentPackageInfo ("id", new SemanticVersion ("1.0"));

			string actual = recentPackageInfo.ToString ();

			string expected = "[RecentPackageInfo Id=id, Version=1.0]";
			Assert.AreEqual (expected, actual);
		}

		[Test]
		public void IsMatch_PackageWithSameIdAndVersionPassed_ReturnsTrue ()
		{
			string id = "id";
			var version = new SemanticVersion (1, 0, 0, 0);
			var recentPackageInfo = new RecentPackageInfo (id, version);
			var package = new FakePackage (id);
			package.Version = version;

			bool result = recentPackageInfo.IsMatch (package);

			Assert.IsTrue (result);
		}

		[Test]
		public void IsMatch_PackageWithSameIdButDifferentVersionPassed_ReturnsFalse ()
		{
			string id = "id";
			var version = new SemanticVersion (1, 0, 0, 0);
			var recentPackageInfo = new RecentPackageInfo (id, version);
			var package = new FakePackage (id);
			package.Version = new SemanticVersion (2, 0, 0, 0);

			bool result = recentPackageInfo.IsMatch (package);

			Assert.IsFalse (result);
		}

		[Test]
		public void IsMatch_PackageWithDifferentIdButSameVersionPassed_ReturnsFalse ()
		{
			var version = new SemanticVersion (1, 0, 0, 0);
			var recentPackageInfo = new RecentPackageInfo ("id", version);
			var package = new FakePackage ("different-id");
			package.Version = version;

			bool result = recentPackageInfo.IsMatch (package);

			Assert.IsFalse (result);
		}
	}
}

