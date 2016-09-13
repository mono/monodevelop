//
// AddPlatformImplementationTests.cs
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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Packaging.Tests
{
	[TestFixture]
	public class AddPlatformImplementationTests : TestBase
	{
		public AddPlatformImplementationTests ()
		{
			Simulate ();

			#pragma warning disable 219
			// Ensure NuGet.ProjectManagement assembly is loaded otherwise creating
			// a PackagingProject will fail.
			string binDirectory = NuGet.ProjectManagement.Constants.BinDirectory;
			#pragma warning restore 219
		}

		[Test]
		public async Task AddAndroidProjectForPCLProject ()
		{
			string templateId = "MonoDevelop.CSharp.PortableLibrary";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir ("AddAndroidProjectForPCLProject");
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = Path.Combine (dir, "MyProject"),
				ProjectName = "MyProject",
				SolutionName = "Solution",
				SolutionPath = dir
			};

			var solution = template.CreateWorkspaceItem (cinfo) as Solution;
			string solutionFileName = Path.Combine (dir, "Solution.sln");
			await solution.SaveAsync (solutionFileName, Util.GetMonitor ());

			var pclProject = solution.GetAllProjects ().OfType<DotNetProject> ().First ();

			// Add NuGet package metadata to PCL project.
			var metadata = new NuGetPackageMetadata ();
			metadata.Load (pclProject);
			metadata.Id = "MyPackage";
			metadata.Authors = "Authors";
			metadata.Owners = "Owners";
			metadata.Version = "1.2.3";
			metadata.UpdateProject (pclProject);
			await pclProject.SaveAsync (Util.GetMonitor ());

			// Add platform implementation.
			var viewModel = new AddPlatformImplementationViewModel (pclProject);
			viewModel.CreateAndroidProject = true;
			viewModel.CreateSharedProject = false;
			viewModel.CreateIOSProject = false;

			await viewModel.CreateProjects (Util.GetMonitor ());

			// Verify projects created as expected.
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

			pclProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.Name == "MyProject");

			// Solution contains Android project.
			var androidProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.Android");
			Assert.That (androidProject.GetTypeTags (), Contains.Item ("MonoDroid"));
			Assert.AreEqual ("MyProject.Android.csproj", androidProject.FileName.FileName);

			// Solution contains NuGet packaging project.
			var nugetProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.NuGet") as PackagingProject;
			Assert.AreEqual ("MonoDevelop.Packaging.PackagingProject", nugetProject.GetType ().FullName);
			Assert.AreEqual ("MyProject.NuGet.nuproj", nugetProject.FileName.FileName);

			// NuGet packaging project references Android project.
			var androidProjectReference = nugetProject.References.Single (r => r.ResolveProject (solution) == androidProject);
			Assert.IsNotNull (androidProjectReference);

			// NuGet packaging project references PCL project.
			var projectReference = nugetProject.References.Single (r => r.ResolveProject (solution) == pclProject);
			Assert.IsNotNull (projectReference);

			// NuGet packaging project contains metadata from PCL project.
			metadata = nugetProject.GetPackageMetadata ();
			Assert.AreEqual ("MyPackage", metadata.Id);
			Assert.AreEqual ("1.2.3", metadata.Version);
			Assert.AreEqual ("Authors", metadata.Authors);
			Assert.AreEqual ("Owners", metadata.Owners);

			// NuGet packaging metadata is removed from PCL project.
			metadata = new NuGetPackageMetadata ();
			metadata.Load (pclProject);
			Assert.IsTrue (metadata.IsEmpty ());

			// Configurations created for Android and NuGet packaging project.
			foreach (var config in solution.Configurations) {
				Assert.That (androidProject.GetConfigurations (), Contains.Item (config.Id));
				Assert.That (nugetProject.GetConfigurations (), Contains.Item (config.Id));
			}

			// Ensure NuGet imports are added to the Android project.
			Assert.IsTrue (androidProject.MSBuildProject.ImportExists (DotNetProjectExtensions.packagingCommonTargets));
		}
	}
}
