//
// DotNetCoreSdkTests.cs
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

using MonoDevelop.Core.Assemblies;
using NUnit.Framework;
using System.Linq;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreSdkTests
	{
		/// <summary>
		/// Checks that a project's target framework is supported based on the install .NET Core SDKs.
		/// Also takes into account if Mono includes the .NET Core v1 SDKs
		/// </summary>
		[TestCase (".NETCoreApp", "1.0", new [] { "1.0.4" }, false, true)]
		[TestCase (".NETCoreApp", "1.1", new [] { "1.0.4" }, false, true)] // .NET Core sdk 1.0 supports 1.1 projects.
		[TestCase (".NETStandard" ,"1.0", new [] { "1.0.4" }, false, true)]
		[TestCase (".NETCoreApp", "1.0", new [] { "1.0.4" }, true, true)] // Mono has .NET Core sdks.
		[TestCase (".NETCoreApp", "1.1", new string [0], true, true)] // Mono has .NET Core sdks.
		[TestCase (".NETStandard", "1.0", new string [0], true, true)] // Mono has .NET Core sdks.
		[TestCase (".NETCoreApp", "2.0", new [] { "1.0.4" }, false, false)]
		[TestCase (".NETStandard", "2.0", new [] { "1.0.4" }, false, false)]
		[TestCase (".NETCoreApp", "2.0", new [] { "1.0.4", "2.0.0" }, false, true)]
		[TestCase (".NETStandard", "2.0", new [] { "1.0.4", "2.0.0" }, false, true)]
		[TestCase (".NETCoreApp", "2.0", new [] { "2.0.0-preview2-006497" }, false, true)] // Allow preview versions.
		[TestCase (".NETStandard", "2.0", new [] { "2.0.0-preview2-006497" }, false, true)] // Allow preview versions.
		[TestCase (".NETFramework", "2.0", new [] { "2.0.0" }, false, true)] // Allow other non-.NET Core frameworks to be supported.
		[TestCase (".NETCoreApp", "1.1", new [] { "2.0.0" }, false, true)] // v2.0 SDK can compile v1 projects
		[TestCase (".NETStandard", "1.6", new [] { "2.0.0" }, false, true)] // v2.0 SDK can compile v1 projects
		public void IsSupportedTargetFramework (
			string frameworkIdentifier,
			string frameworkVersion,
			string[] installedSdkVersions,
			bool msbuildSdksInstalled, // True if Mono has the .NET Core sdks.
			bool expected)
		{
			string framework = $"{frameworkIdentifier},Version=v{frameworkVersion}";
			var targetFrameworkMoniker = TargetFrameworkMoniker.Parse (framework);
			var versions = installedSdkVersions.Select (version => DotNetCoreVersion.Parse (version))
				.OrderByDescending (version => version)
				.ToArray ();

			bool actual = DotNetCoreSdk.IsSupported (targetFrameworkMoniker, versions, msbuildSdksInstalled);

			Assert.AreEqual (expected, actual);
		}
	}
}
