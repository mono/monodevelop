//
// ImportedPackageReferenceProjectTests.cs
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

using System.IO;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ImportedPackageReferenceProjectTests
	{
		string tempDirectory;

		[TearDown]
		public void TearDown ()
		{
			if (tempDirectory != null && Directory.Exists (tempDirectory)) {
				Directory.Delete (tempDirectory, true);
			}
		}

		void CreateTempDirectory ()
		{
			tempDirectory = FileService.CreateTempDirectory ();
		}

		void SaveFileInTempDirectory (string fileName, string contents)
		{
			string fullPath = Path.Combine (tempDirectory, fileName);
			File.WriteAllText (fullPath, contents, Encoding.UTF8);
		}

		[Test]
		public async Task CreatePackageReferenceNuGetProject_OneImportedPackageReference_ReturnsNuGetProject ()
		{
			string projectXml =
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Project DefaultTargets=\"Build\" ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <PropertyGroup>\r\n" +
				"    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>\r\n" +
				"    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>\r\n" +
				"    <ProjectGuid>{29DE8CC5-1378-4A68-94CB-2B520618C4A1}</ProjectGuid>\r\n" +
				"    <OutputType>Library</OutputType>\r\n" +
				"    <RootNamespace>TestImportedPackageReferences</RootNamespace>\r\n" +
				"    <AssemblyName>TestImportedPackageReferences</AssemblyName>\r\n" +
				"    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>\r\n" +
				"    <OutputPath>bin</OutputPath>\r\n" +
				"  </PropertyGroup>\r\n" +
				"  <Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />\r\n" +
				"  <Import Project=\"PackageReferences.proj\" />\r\n" +
				"</Project>";

			string importedProjectXml =
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<Project DefaultTargets=\"Build\" ToolsVersion=\"4.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
				"  <ItemGroup>\r\n" +
				"    <PackageReference Include=\"Newtonsoft.Json\" Version=\"10.0.1\" />\r\n" +
				"  </ItemGroup>\r\n" +
				"</Project>";

			CreateTempDirectory ();
			SaveFileInTempDirectory ("PackageReferences.proj", importedProjectXml);
			SaveFileInTempDirectory ("Project.csproj", projectXml);

			string fileName = Path.Combine (tempDirectory, "Project.csproj");
			var project = (DotNetProject) await Services.ProjectService.ReadSolutionItem (new ProgressMonitor (), fileName);

			var nugetProject = new MonoDevelopNuGetProjectFactory ()
				.CreateNuGetProject (project);

			Assert.IsNotNull (nugetProject);
			Assert.IsInstanceOf<PackageReferenceNuGetProject> (nugetProject);
		}
	}
}
