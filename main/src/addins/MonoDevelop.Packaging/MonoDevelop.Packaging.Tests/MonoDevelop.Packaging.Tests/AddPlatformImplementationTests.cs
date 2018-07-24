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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Projects.SharedAssetsProjects;

namespace MonoDevelop.Packaging.Tests
{
	[TestFixture]
	public class AddPlatformImplementationTests : IdeTestBase
	{
		[TestFixtureSetUp]
		public void SetUp ()
		{
			if (!Platform.IsMac)
				Assert.Ignore ("Platform not Mac - Ignoring AddPlatformImplementationTests");
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

			var solution = await template.CreateWorkspaceItem (cinfo) as Solution;
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
			var viewModel = new TestableAddPlatformImplementationViewModel (pclProject);
			viewModel.CreateAndroidProject = true;
			viewModel.CreateSharedProject = false;
			viewModel.CreateIOSProject = false;

			await viewModel.CreateProjects (Util.GetMonitor ());

			// Verify projects created as expected.
			solution = (Solution) await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

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

			// DefaultNamespace is the same for all projects.
			Assert.AreEqual ("MyProject", ((DotNetProject)androidProject).DefaultNamespace);
			Assert.AreEqual ("MyProject", ((DotNetProject)pclProject).DefaultNamespace);
		}

		[Test]
		public async Task AddIOSProjectForPCLProject ()
		{
			string templateId = "MonoDevelop.CSharp.PortableLibrary";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir ("AddIOSProjectForPCLProject");
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = Path.Combine (dir, "MyProject"),
				ProjectName = "MyProject",
				SolutionName = "Solution",
				SolutionPath = dir
			};

			var solution = await template.CreateWorkspaceItem (cinfo) as Solution;
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
			var viewModel = new TestableAddPlatformImplementationViewModel (pclProject);
			viewModel.CreateAndroidProject = false;
			viewModel.CreateSharedProject = false;
			viewModel.CreateIOSProject = true;

			await viewModel.CreateProjects (Util.GetMonitor ());

			// Verify projects created as expected.
			solution = (Solution) await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

			pclProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.Name == "MyProject");

			// Solution contains Android project.
			var iosProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.Name == "MyProject.iOS");
			Assert.That (iosProject.GetTypeTags (), Contains.Item ("XamarinIOS"));
			Assert.AreEqual ("MyProject.iOS.csproj", iosProject.FileName.FileName);
			Assert.AreEqual (CompileTarget.Library, iosProject.CompileTarget);

			// Solution contains NuGet packaging project.
			var nugetProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.NuGet") as PackagingProject;
			Assert.AreEqual ("MyProject.NuGet.nuproj", nugetProject.FileName.FileName);

			// NuGet packaging project references iOS project.
			var iosProjectReference = nugetProject.References.Single (r => r.ResolveProject (solution) == iosProject);
			Assert.IsNotNull (iosProjectReference);

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

			var assemblyInfoFile = iosProject.Items.OfType<ProjectFile> ().Single (file => file.FilePath.FileName == "AssemblyInfo.cs");
			Assert.IsNotNull (assemblyInfoFile);

			// DefaultNamespace is the same for all projects.
			Assert.AreEqual ("MyProject", iosProject.DefaultNamespace);
			Assert.AreEqual ("MyProject", pclProject.DefaultNamespace);
		}

		[Test]
		public async Task AddSharedProjectForPCLProject ()
		{
			string templateId = "MonoDevelop.CSharp.PortableLibrary";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir ("AddSharedProjectForPCLProject");
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = Path.Combine (dir, "MyProject"),
				ProjectName = "MyProject",
				SolutionName = "Solution",
				SolutionPath = dir
			};

			var solution = await template.CreateWorkspaceItem (cinfo) as Solution;
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

			// Add another csharp file to the pclProject in a subdirectory.
			string anotherCSharpFileName = pclProject.BaseDirectory.Combine ("src", "AnotherClass.cs");
			Directory.CreateDirectory (Path.GetDirectoryName (anotherCSharpFileName));
			File.WriteAllText (anotherCSharpFileName, "class AnotherClass {}");
			pclProject.AddFile (anotherCSharpFileName);
			await pclProject.SaveAsync (Util.GetMonitor ());

			// Add platform implementation.
			var viewModel = new TestableAddPlatformImplementationViewModel (pclProject);
			viewModel.CreateAndroidProject = true;
			viewModel.CreateSharedProject = true;
			viewModel.CreateIOSProject = true;

			await viewModel.CreateProjects (Util.GetMonitor ());

			// Verify projects created as expected.
			solution = (Solution) await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

			pclProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.Name == "MyProject");

			// Solution contains Shared project.
			var sharedProject = solution.GetAllProjects ().OfType<SharedAssetsProject> ().FirstOrDefault (p => p.Name == "MyProject.Shared");
			Assert.AreEqual ("MyProject.Shared.shproj", sharedProject.FileName.FileName);

			// PCL project references the Shared project.
			var projectReference = pclProject.References.Single (r => r.ResolveProject (solution) == sharedProject);
			Assert.IsNotNull (projectReference);

			// Solution contains NuGet packaging project.
			var nugetProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.NuGet") as PackagingProject;
			Assert.AreEqual ("MyProject.NuGet.nuproj", nugetProject.FileName.FileName);

			// NuGet packaging project references PCL project.
			projectReference = nugetProject.References.Single (r => r.ResolveProject (solution) == pclProject);
			Assert.IsNotNull (projectReference);

			// Android project references shared project
			var androidProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.Name == "MyProject.Android");
			projectReference = androidProject.References.Single (r => r.ResolveProject (solution) == sharedProject);
			Assert.IsNotNull (projectReference);

			// iOS project references shared project
			var iosProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.Name == "MyProject.iOS");
			projectReference = iosProject.References.Single (r => r.ResolveProject (solution) == sharedProject);
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

			// PCL project should only have the assembly info file directly in the project.
			Assert.IsTrue (pclProject.MSBuildProject.GetAllItems ().Any (item => item.Include.Contains ("AssemblyInfo.cs")));
			Assert.IsFalse (pclProject.MSBuildProject.GetAllItems ().Any (item => item.Include.Contains ("MyClass.cs")));
			Assert.IsFalse (pclProject.MSBuildProject.GetAllItems ().Any (item => item.Include.Contains ("AnotherClass.cs")));
			string assemblyInfoFileName = pclProject.BaseDirectory.Combine ("Properties", "AssemblyInfo.cs");
			Assert.IsTrue (File.Exists (assemblyInfoFileName));
			string csharpFileName = pclProject.BaseDirectory.Combine ("MyClass.cs");
			Assert.IsFalse (File.Exists (csharpFileName));
			Assert.IsFalse (File.Exists (anotherCSharpFileName));

			// Shared project should have files from PCL project.
			string copiedCSharpFileName = sharedProject.BaseDirectory.Combine ("MyClass.cs");
			Assert.That (sharedProject.Files.Select (f => f.FilePath.ToString ()), Contains.Item (copiedCSharpFileName));
			Assert.IsTrue (File.Exists (copiedCSharpFileName));
			string copiedAnotherCSharpFileName = sharedProject.BaseDirectory.Combine ("src", "AnotherClass.cs");
			Assert.That (sharedProject.Files.Select (f => f.FilePath.ToString ()), Contains.Item (copiedAnotherCSharpFileName));
			Assert.IsTrue (File.Exists (copiedAnotherCSharpFileName));
			string copiedAssemblyInfoFileName = sharedProject.BaseDirectory.Combine ("Properties", "AssemblyInfo.cs");
			Assert.That (sharedProject.Files.Select (f => f.FilePath.ToString ()), Has.No.Member (copiedAssemblyInfoFileName));
			Assert.IsFalse (File.Exists (copiedAssemblyInfoFileName));

			var expectedBaseDirectory = pclProject.BaseDirectory.ParentDirectory;
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.Android", "MyProject.Android.csproj"), androidProject.FileName);
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.iOS", "MyProject.iOS.csproj"), iosProject.FileName);
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.NuGet", "MyProject.NuGet.nuproj"), nugetProject.FileName);
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.Shared", "MyProject.Shared.shproj"), sharedProject.FileName);

			// DefaultNamespace is the same for all projects.
			Assert.AreEqual ("MyProject", androidProject.DefaultNamespace);
			Assert.AreEqual ("MyProject", iosProject.DefaultNamespace);
			Assert.AreEqual ("MyProject", pclProject.DefaultNamespace);
			Assert.AreEqual ("MyProject", sharedProject.DefaultNamespace);

			// OutputAssemblyName is the same for PCL, iOS and Android project.
			Assert.IsTrue (androidProject.Configurations.OfType<DotNetProjectConfiguration> ().All (config => config.OutputAssembly == "MyProject"));
			Assert.IsTrue (iosProject.Configurations.OfType<DotNetProjectConfiguration> ().All (config => config.OutputAssembly == "MyProject"));
			Assert.IsTrue (pclProject.Configurations.OfType<DotNetProjectConfiguration> ().All (config => config.OutputAssembly == "MyProject"));

			// iOS and Android project should have an AssemblyInfo file.
			Assert.IsTrue (androidProject.MSBuildProject.GetAllItems ().Any (item => item.Include.Contains ("AssemblyInfo.cs")));
			Assert.IsTrue (iosProject.MSBuildProject.GetAllItems ().Any (item => item.Include.Contains ("AssemblyInfo.cs")));
		}

		[Test]
		public async Task PCLProjectInSameDirectoryAsSolution ()
		{
			string templateId = "MonoDevelop.CSharp.PortableLibrary";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir ("AddAndroidProjectForPCLProjectInSameDirectoryAsSolution");
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "MyProject",
				SolutionName = "Solution",
				SolutionPath = dir
			};

			var solution = await template.CreateWorkspaceItem (cinfo) as Solution;
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
			var viewModel = new TestableAddPlatformImplementationViewModel (pclProject);
			viewModel.CreateAndroidProject = true;
			viewModel.CreateSharedProject = true;
			viewModel.CreateIOSProject = true;

			await viewModel.CreateProjects (Util.GetMonitor ());

			// Verify projects created as expected.
			solution = (Solution) await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

			var androidProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.Android");
			var nugetProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.NuGet");
			var iosProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.iOS");
			var sharedProject = solution.GetAllProjects ().FirstOrDefault (p => p.Name == "MyProject.Shared");

			var expectedBaseDirectory = solution.BaseDirectory;
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.Android", "MyProject.Android.csproj"), androidProject.FileName);
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.iOS", "MyProject.iOS.csproj"), iosProject.FileName);
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.NuGet", "MyProject.NuGet.nuproj"), nugetProject.FileName);
			Assert.AreEqual (expectedBaseDirectory.Combine ("MyProject.Shared", "MyProject.Shared.shproj"), sharedProject.FileName);
		}
	}
}
