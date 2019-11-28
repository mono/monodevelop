//
// TargetFrameworkTests.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 Microsoft, Corp. (http://www.microsoft.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using NUnit.Framework;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class TargetFrameworkTests : DotNetCoreTestBase
	{
		[TestCase ("1.0", "1.0", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1")]
		[TestCase ("1.1", "1.1", "2.0", "2.1", "2.2", "3.0", "3.1")]
		[TestCase ("2.0", "2.0", "2.1", "2.2", "3.0", "3.1")]
		[TestCase ("2.1", "2.1", "2.2", "3.0", "3.1")]
		[TestCase ("2.2", "2.2", "3.0", "3.1")]
		[TestCase ("3.0", "3.0", "3.1")]
		public void NetCoreApp_IsVersionOrHigher (string version, params string[] versionsToCheck)
		{
			var frameworkVersion = DotNetCoreVersion.Parse (version);
			foreach (var v in versionsToCheck) {
				var framework = CreateTargetFramework (".NETCoreApp", v);
				Assert.True (framework.IsNetCoreAppOrHigher (frameworkVersion));
			}
		}

		[TestCase ("1.0", "1.0", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6", "2.0", "2.1")]
		[TestCase ("1.1", "1.1", "1.2", "1.3", "1.4", "1.5", "1.6", "2.0", "2.1")]
		[TestCase ("1.2", "1.2", "1.3", "1.4", "1.5", "1.6", "2.0", "2.1")]
		[TestCase ("1.3", "1.3", "1.4", "1.5", "1.6", "2.0", "2.1")]
		[TestCase ("1.4", "1.4", "1.5", "1.6", "2.0", "2.1")]
		[TestCase ("1.5", "1.5", "1.6", "2.0", "2.1")]
		[TestCase ("1.6", "1.6", "2.0", "2.1")]
		[TestCase ("2.0", "2.0", "2.1")]
		[TestCase ("2.1", "2.1")]
		public void NetStandard_IsVersionOrHigher (string version, params string [] versionsToCheck)
		{
			var frameworkVersion = DotNetCoreVersion.Parse (version);
			foreach (var v in versionsToCheck) {
				var framework = CreateTargetFramework (".NETStandard", v);
				Assert.True (framework.IsNetStandardOrHigher (frameworkVersion));
			}
		}

		static TargetFramework CreateTargetFramework (string identifier, string version)
		{
			var moniker = new TargetFrameworkMoniker (identifier, version);
			return Runtime.SystemAssemblyService.GetTargetFramework (moniker);
		}
	}
}
