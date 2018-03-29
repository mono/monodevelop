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

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreProjectSupportedTargetFrameworksTests : DotNetCoreVersionsRestorerTestBase
	{
		[Test]
		public void GetNetStandardTargetFrameworks_NetCore20RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("2.0.5");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETStandard,Version=v2.0", frameworks [0].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.6", frameworks [1].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.5", frameworks [2].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.4", frameworks [3].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.3", frameworks [4].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.2", frameworks [5].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.1", frameworks [6].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.0", frameworks [7].Id.ToString ());
			Assert.AreEqual (8, frameworks.Count);
		}

		[Test]
		public void GetNetStandardTargetFrameworks_NetCore11RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("1.1");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETStandard,Version=v1.6", frameworks [0].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.5", frameworks [1].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.4", frameworks [2].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.3", frameworks [3].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.2", frameworks [4].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.1", frameworks [5].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.0", frameworks [6].Id.ToString ());
			Assert.AreEqual (7, frameworks.Count);
		}

		[Test]
		public void GetNetStandardTargetFrameworks_NoRuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled (new string [0]);

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

			Assert.AreEqual (0, frameworks.Count);
		}

		[Test]
		public void GetNetStandardTargetFrameworks_NetCore21RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("2.1.0");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETStandard,Version=v2.0", frameworks [0].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.6", frameworks [1].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.5", frameworks [2].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.4", frameworks [3].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.3", frameworks [4].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.2", frameworks [5].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.1", frameworks [6].Id.ToString ());
			Assert.AreEqual (".NETStandard,Version=v1.0", frameworks [7].Id.ToString ());
			Assert.AreEqual (8, frameworks.Count);
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
		public void GetNetCoreAppTargetFrameworks_NetCore21RuntimeInstalled ()
		{
			DotNetCoreRuntimesInstalled ("2.1.0");

			var frameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

			Assert.AreEqual (".NETCoreApp,Version=v2.1", frameworks [0].Id.ToString ());
			Assert.AreEqual (1, frameworks.Count);
		}
	}
}
