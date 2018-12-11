//
// DotNetCoreProjectSupportedTargetFrameworksTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using NUnit.Framework;
using System.Linq;
using System;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreProjectSupportedTargetFrameworksTests : DotNetCoreVersionsRestorerTestBase
	{
		static string[] netStandardVersions = { "2.1", "2.0", "1.6", "1.5", "1.4", "1.3", "1.2", "1.1", "1.0" };

		[TestCase ("5.4.0", "2.0", "2.0.5")]
		[TestCase ("5.3.99", "1.6", new string[0])]
		[TestCase ("5.4.0", "2.0", new string[0])]
		[TestCase ("4.8.0", "1.6", "1.1")]
		[TestCase ("4.8.0", "2.0", "2.2.0")]
		[TestCase ("5.3.1", "2.0", "2.1.0")]
		[TestCase ("5.16.0", "2.0", "1.1")]
		public void GetNetStandardTargetFrameworks_MonoAndSdkInstalled (string monoVersion, string maxNetStandardVersion, params string[] sdkVersions)
		{
			DotNetCoreRuntimesInstalled (sdkVersions);
			MonoRuntimeInfoExtensions.CurrentRuntimeVersion = new Version (monoVersion);

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

			int start = netStandardVersions.IndexOf (maxNetStandardVersion);
			Assert.That (start, Is.GreaterThanOrEqualTo (0));
			Assert.That (frameworks.Count, Is.EqualTo (netStandardVersions.Length - start));
			for (int i = start; i < netStandardVersions.Length; i++) {
				Assert.AreEqual ($".NETStandard,Version=v{netStandardVersions[i]}", frameworks[i - start].Id.ToString ());
			}
		}

		[Test]
		public void GetNetCoreAppTargetFrameworks_NetCore20RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("2.0.5");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETCoreApp,Version=v2.0", frameworks [0].Id.ToString ());
			Assert.AreEqual (1, frameworks.Count);
		}

		[Test]
		public void GetNetCoreAppTargetFrameworks_NetCore1xRuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("1.1.0", "1.0.5");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETCoreApp,Version=v1.1", frameworks [0].Id.ToString ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", frameworks [1].Id.ToString ());
			Assert.AreEqual (2, frameworks.Count);
		}

		[Test]
		public void GetNetCoreAppTargetFrameworks_NetCore30RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("3.0.0");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETCoreApp,Version=v3.0", frameworks [0].Id.ToString ());
			Assert.AreEqual (1, frameworks.Count);
		}

		[Test]
		public void GetNetCoreAppTargetFrameworks_NetCore22RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("2.2.0");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETCoreApp,Version=v2.2", frameworks[0].Id.ToString ());
			Assert.AreEqual (1, frameworks.Count);
		}

		[Test]
		public void GetNetCoreAppTargetFrameworks_NetCore21RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("2.1.0");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETCoreApp,Version=v2.1", frameworks [0].Id.ToString ());
			Assert.AreEqual (1, frameworks.Count);
		}
	}
}
