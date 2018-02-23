//
// DotNetCoreVersionsRestorerTestBase.cs
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

using System.Linq;
using NUnit.Framework;

namespace MonoDevelop.DotNetCore.Tests
{
	/// <summary>
	/// Ensures the .NET Core sdk and runtime version information is reset after
	/// the tests are completed.
	/// </summary>
	class DotNetCoreVersionsRestorerTestBase
	{
		DotNetCoreVersion[] sdkVersions;
		bool sdkInstalled;
		DotNetCoreVersion[] runtimeVersions;
		bool runtimeInstalled;

		[TestFixtureSetUp]
		public void SetupTestFixture ()
		{
			sdkInstalled = DotNetCoreSdk.IsInstalled;
			sdkVersions = DotNetCoreSdk.Versions;

			runtimeInstalled = DotNetCoreRuntime.IsInstalled;
			runtimeVersions = DotNetCoreRuntime.Versions;
		}

		[TestFixtureTearDown]
		public void TearDownTestFixture ()
		{
			DotNetCoreSdk.SetInstalled (sdkInstalled);
			DotNetCoreSdk.SetVersions (sdkVersions);

			DotNetCoreRuntime.SetInstalled (runtimeInstalled);
			DotNetCoreRuntime.SetVersions (runtimeVersions);
		}

		protected void DotNetCoreRuntimesInstalled (params string[] versions)
		{
			var dotNetCoreVersions = versions.Select (DotNetCoreVersion.Parse)
				.OrderByDescending (version => version);

			DotNetCoreRuntime.SetVersions (dotNetCoreVersions);
		}

		protected void DotNetCoreSdksInstalled (params string[] versions)
		{
			var dotNetCoreVersions = versions.Select (DotNetCoreVersion.Parse)
				.OrderByDescending (version => version);

			DotNetCoreSdk.SetVersions (dotNetCoreVersions);
		}
	}
}
