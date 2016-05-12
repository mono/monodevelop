//
// NuGetPackageUninstallerTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects.MSBuild;
using NuGet.Packaging;
using NuGet.Versioning;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class NuGetPackageUninstallerTests
	{
		NuGetPackageUninstaller uninstaller;
		FakeDotNetProject project;
		MSBuildProject msbuildProject;

		void CreateMSBuildProject ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"</Project>");
		}

		void CreateMSBuildProject (string xml)
		{
			msbuildProject = new MSBuildProject ();
			msbuildProject.LoadXml (xml);
		}

		void CreateProject (string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			fileName = fileName.ToNativePath ();
			project = new FakeDotNetProject (fileName);
			project.SaveAction = SaveProject;
		}

		void CreateUninstallerWithPackagesFolder (string packagesDirectory)
		{
			packagesDirectory = packagesDirectory.ToNativePath ();
			CreateProject ();
			var resolver = new PackagePathResolver (packagesDirectory);
			uninstaller = new NuGetPackageUninstaller (project, resolver);
		}

		void AddReferenceToProject (string name, string hintPath = null)
		{
			if (hintPath != null)
				hintPath = hintPath.ToNativePath ();
			ProjectHelper.AddReference (project, name, hintPath);
		}

		Task ForceUninstall (string packageId, string packageVersion)
		{
			msbuildProject.FileName = project.FileName;
			return uninstaller.ForceUninstall (packageId, new NuGetVersion (packageVersion));
		}

		void SaveProject ()
		{
			var msbuildExtension = new PackageManagementMSBuildExtension ();
			msbuildExtension.UpdateProject (msbuildProject);
		}

		[Test]
		public async Task ForceUninstall_TwoReferencesOneMatchesPackageBeingRemoved_OneReferenceRemoved ()
		{
			CreateMSBuildProject ();
			CreateUninstallerWithPackagesFolder (@"d:\MyProject\packages");
			AddReferenceToProject ("System");
			AddReferenceToProject ("MyLib", @"d:\MyProject\packages\MyLib.1.2\lib\net40\MyLib.dll");

			await ForceUninstall ("MyLib", "1.2");

			Assert.AreEqual (1, project.References.Count);
			Assert.AreEqual ("System", project.References[0].Reference);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task ForceUninstall_TwoImportsOneMatchesPackageBeingRemoved_OneImportRemoved ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"    <Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />\r\n" +
				"    <Import Project=\"packages\\MyLib.1.2\\build\\net45\\MyLib.targets\" Condition=\"Exists('packages\\MyLib.1.2\\build\\net45\\MyLib.targets')\" />\r\n" +
				"</Project>");
			CreateUninstallerWithPackagesFolder (@"d:\MyProject\packages");
			project.ChangeFileName (@"d:\MyProject\MyProject.csproj");
			int importsBeforeUninstall = msbuildProject.Imports.Count ();

			await ForceUninstall ("MyLib", "1.2");

			Assert.AreEqual (2, importsBeforeUninstall);
			Assert.IsTrue (project.IsSaved);
			Assert.AreEqual (1, msbuildProject.Imports.Count ());
			Assert.AreEqual (@"$(MSBuildBinPath)\Microsoft.CSharp.targets", msbuildProject.Imports.First ().Project);
		}

		[Test]
		public async Task ForceUninstall_EnsureNuGetPackageBuildImportExists_ImportAndEnsureNuGetPackageBuildImportIsRemoved ()
		{
			CreateMSBuildProject (
				"<Project ToolsVersion=\"12.0\" DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <Target Name=\"EnsureNuGetPackageBuildImports\" BeforeTargets=\"PrepareForBuild\">\r\n" +
				"    <PropertyGroup>\r\n" +
				"      <ErrorText>Error.</ErrorText>\r\n" +
				"    </PropertyGroup>\r\n" +
				"    <Error Condition=\"!Exists('packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets')\" Text=\"$([System.String]::Format('$(ErrorText)', 'packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets'))\" />\r\n" +
				"  </Target>\r\n" +
				"  <Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />\r\n" +
				"  <Import Project=\"packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets\" Condition=\"Exists('packages\\Xamarin.Forms.1.2.3.6257\\build\\portable-win+net45+wp80+MonoAndroid10+MonoTouch10\\Xamarin.Forms.targets)\" />\r\n" +
				"</Project>");
			CreateUninstallerWithPackagesFolder (@"d:\MyProject\packages");
			project.ChangeFileName (@"d:\MyProject\MyProject.csproj");
			int targetCountBeforeUninstall = msbuildProject.Targets.Count ();
			int importsBeforeUninstall = msbuildProject.Imports.Count ();

			await ForceUninstall ("Xamarin.Forms", "1.2.3.6257");

			Assert.AreEqual (1, targetCountBeforeUninstall);
			Assert.AreEqual (2, importsBeforeUninstall);
			Assert.AreEqual (1, msbuildProject.Imports.Count ());
			Assert.AreEqual (@"$(MSBuildBinPath)\Microsoft.CSharp.targets", msbuildProject.Imports.First ().Project);
			Assert.AreEqual (0, msbuildProject.Targets.Count ());
		}
	}
}

