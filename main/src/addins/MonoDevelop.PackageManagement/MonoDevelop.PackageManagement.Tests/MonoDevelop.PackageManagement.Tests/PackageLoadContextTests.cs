//
// PackageLoadContextTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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

using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NuGet.PackageManagement.UI;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	class PackageLoadContextTests
	{
		/// <summary>
		/// Manage NuGet Packages has problems with packages that have no version in the Updates tab
		/// so the PackageCollection has been modified to ignore these packages.
		/// </summary>
		[Test]
		public async Task GetInstalledPackages_PackageHasNoVersion_PackageIsNotIncluded ()
		{
			var project = new FakeDotNetProject (@"d:\projects\MyProject\MyProject.csproj");
			var nugetProject = new FakeNuGetProject (project);
			nugetProject.AddPackageReference ("One", "1.2");
			nugetProject.AddPackageReference ("Two", version: null);
			var projects = new [] { nugetProject };
			var context = new PackageLoadContext (null, false, projects);

			var packages = await context.GetInstalledPackagesAsync ();

			Assert.IsTrue (packages.ContainsId ("One"));
			Assert.IsFalse (packages.ContainsId ("Two"));
		}
	}
}
