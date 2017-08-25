﻿//
// SdkVersionTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects.MSBuild;
using NUnit.Framework;

namespace MonoDevelop.Projects
{
	[TestFixture]
	class SdkVersionTests
	{
		[Test]
		public void Parse_StableVersion ()
		{
			var version = SdkVersion.Parse ("1.2.3");

			Assert.AreEqual (1, version.Major);
			Assert.AreEqual (2, version.Minor);
			Assert.AreEqual (3, version.Patch);
			Assert.AreEqual ("1.2.3", version.Version.ToString ());
			Assert.IsFalse (version.IsPrerelease);
			Assert.AreEqual ("1.2.3", version.ToString ());
		}

		[Test]
		public void Parse_InvalidVersion ()
		{
			Assert.Throws<FormatException> (() => {
				SdkVersion.Parse ("invalid");
			});
		}

		[Test]
		public void Parse_NullVersion ()
		{
			Assert.Throws<ArgumentException> (() => {
				SdkVersion.Parse (null);
			});
		}

		[Test]
		public void Parse_VersionIsEmptyString ()
		{
			Assert.Throws<ArgumentException> (() => {
				SdkVersion.Parse (null);
			});
		}

		[Test]
		public void Parse_PrereleaseVersion ()
		{
			var version = SdkVersion.Parse ("2.0.0-preview2-002093-00+beta1");

			Assert.AreEqual (2, version.Major);
			Assert.AreEqual (0, version.Minor);
			Assert.AreEqual (0, version.Patch);
			Assert.AreEqual ("2.0.0", version.Version.ToString ());
			Assert.IsTrue (version.IsPrerelease);
			Assert.AreEqual ("preview2-002093-00", version.ReleaseLabel);
			Assert.AreEqual ("beta1", version.BuildLabel);
			Assert.AreEqual ("2.0.0-preview2-002093-00+beta1", version.ToString ());
		}

		[Test]
		public void TryParse_NullVersion ()
		{
			SdkVersion version = null;
			bool result = SdkVersion.TryParse (null, out version);

			Assert.IsFalse (result);
		}

		[Test]
		public void TryParse_VersionIsEmptyString ()
		{
			SdkVersion version = null;
			bool result = SdkVersion.TryParse (string.Empty, out version);

			Assert.IsFalse (result);
		}

		[TestCase ("1.0.2", "1.0.2", true)]
		[TestCase ("1.2.3", "1.0.2", false)]
		[TestCase ("1.0.2", "1.0.2-preview1-002912-00", false)]
		[TestCase ("1.0.2-preview1-002912-00", "1.0.2-preview1-002912-00", true)]
		public void Equals_Version (string x, string y, bool expected)
		{
			var versionX = SdkVersion.Parse (x);
			var versionY = SdkVersion.Parse (y);
			Assert.AreEqual (expected, versionX.Equals (versionY));
		}

		[Test]
		public void Equals_NullVersion ()
		{
			var x = SdkVersion.Parse ("1.0");
			Assert.IsFalse (x.Equals (null));
		}

		[Test]
		public void CompareTo_StableVersion ()
		{
			var versionX = SdkVersion.Parse ("1.0.2");
			var versionY = SdkVersion.Parse ("1.2.3");

			Assert.IsTrue (versionX.CompareTo (versionX) == 0);
			Assert.IsTrue (versionY.CompareTo (versionY) == 0);
			Assert.IsTrue (versionX.CompareTo (versionY) < 0);
			Assert.IsTrue (versionY.CompareTo (versionX) > 0);
		}

		[Test]
		public void CompareTo_PrereleaseVersions ()
		{
			var versionX = SdkVersion.Parse ("1.0.2");
			var versionY = SdkVersion.Parse ("1.0.2-preview1-002912-00");

			Assert.IsTrue (versionX.CompareTo (versionX) == 0);
			Assert.IsTrue (versionY.CompareTo (versionY) == 0);
			Assert.IsTrue (versionX.CompareTo (versionY) > 0);
			Assert.IsTrue (versionY.CompareTo (versionX) < 0);
		}

		[Test]
		public void SortingPrereleaseVersions ()
		{
			var version100 = SdkVersion.Parse ("1.0.0");
			var version102 = SdkVersion.Parse ("1.0.2");
			var version102preview1_1 = SdkVersion.Parse ("1.0.2-preview1-002912-00");
			var version102preview1_2 = SdkVersion.Parse ("1.0.2-preview1-003912-00");
			var version102preview2_1 = SdkVersion.Parse ("1.0.2-preview2-001112-00");
			var version200preview2_1 = SdkVersion.Parse ("2.0.0-preview2-002093-00");
			var version200preview2_2 = SdkVersion.Parse ("2.0.0-preview2-002094-00");
			var version200 = SdkVersion.Parse ("2.0.0");
			var version201 = SdkVersion.Parse ("2.0.1");

			var expected = new [] {
				version100,
				version102preview1_1,
				version102preview1_2,
				version102preview2_1,
				version102,
				version200preview2_2,
				version200,
				version201
			};

			var unsorted = new [] {
				version200,
				version102,
				version102preview1_2,
				version201,
				version102preview2_1,
				version100,
				version102preview1_1,
				version200preview2_2,
			};

			var sorted = unsorted.OrderBy (v => v).ToArray ();

			Assert.AreEqual (expected, sorted);
		}
	}
}