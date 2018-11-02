//
// MSBuildPackageSpecCreatorTests.cs
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
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NuGet.Common;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MSBuildPackageSpecCreatorTests : TestBase
	{
		[Test]
		public async Task NetStandardProject_XamarinFormsPackageReference ()
		{
			string projectFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms", "NetStandardXamarinForms.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {
				var spec = await MSBuildPackageSpecCreator.CreatePackageSpec (project, NullLogger.Instance);

				var targetFramework = spec.TargetFrameworks.Single ();
				var dependency = targetFramework.Dependencies.Single (d => d.Name == "Xamarin.Forms");
				Assert.AreEqual ("NetStandardXamarinForms", spec.Name);
				Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
				Assert.AreEqual ("NetStandardXamarinForms", spec.RestoreMetadata.ProjectName);
				Assert.AreEqual ("netstandard1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
				Assert.AreEqual (".NETStandard,Version=v1.0", targetFramework.FrameworkName.ToString ());
				Assert.AreEqual ("Xamarin.Forms", dependency.Name);
				Assert.AreEqual (LibraryDependencyType.Default, dependency.Type);
				Assert.AreEqual (LibraryIncludeFlags.All, dependency.IncludeType);
				Assert.AreEqual (LibraryIncludeFlagUtils.DefaultSuppressParent, dependency.SuppressParent);
				Assert.AreEqual ("[2.4.0.280, )", dependency.LibraryRange.VersionRange.ToString ());
				Assert.AreEqual (LibraryDependencyTarget.Package, dependency.LibraryRange.TypeConstraint);
				Assert.AreEqual ("Xamarin.Forms", dependency.LibraryRange.Name);
			}
		}

		[Test]
		public async Task MultiTarget_NetCoreApp_NetStandard ()
		{
			string projectFile = Util.GetSampleProject ("multi-target", "multi-target.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {
				var spec = await MSBuildPackageSpecCreator.CreatePackageSpec (project, NullLogger.Instance);

				var netstandard = spec.TargetFrameworks.Single (f => f.FrameworkName.Framework == ".NETStandard");
				var netcoreapp = spec.TargetFrameworks.Single (f => f.FrameworkName.Framework == ".NETCoreApp");
				var dependency = netstandard.Dependencies.Single (d => d.Name == "Newtonsoft.Json");
				Assert.IsFalse (netcoreapp.Dependencies.Any (d => d.Name == "Newtonsoft.Json"));
				Assert.AreEqual ("multi-target", spec.Name);
				Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
				Assert.AreEqual ("multi-target", spec.RestoreMetadata.ProjectName);
				Assert.IsTrue (spec.RestoreMetadata.OriginalTargetFrameworks.Contains ("netstandard1.0"));
				Assert.IsTrue (spec.RestoreMetadata.OriginalTargetFrameworks.Contains ("netcoreapp1.1"));
				Assert.AreEqual (".NETCoreApp,Version=v1.1", netcoreapp.FrameworkName.ToString ());
				Assert.AreEqual (".NETStandard,Version=v1.0", netstandard.FrameworkName.ToString ());
				Assert.AreEqual ("Newtonsoft.Json", dependency.Name);
				Assert.AreEqual (LibraryDependencyType.Default, dependency.Type);
				Assert.AreEqual (LibraryIncludeFlags.All, dependency.IncludeType);
				Assert.AreEqual (LibraryIncludeFlagUtils.DefaultSuppressParent, dependency.SuppressParent);
				Assert.AreEqual ("[10.0.1, )", dependency.LibraryRange.VersionRange.ToString ());
				Assert.AreEqual (LibraryDependencyTarget.Package, dependency.LibraryRange.TypeConstraint);
				Assert.AreEqual ("Newtonsoft.Json", dependency.LibraryRange.Name);
			}
		}
	}
}
