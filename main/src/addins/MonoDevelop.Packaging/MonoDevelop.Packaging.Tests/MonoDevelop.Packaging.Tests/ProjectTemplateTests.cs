//
// ProjectTemplateTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Projects.SharedAssetsProjects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Packaging.Tests
{
	[TestFixture]
	public class ProjectTemplateTests : IdeTestBase
	{
		[Test]
		public async Task CreatePackagingProjectFromTemplate ()
		{
			string templateId = "MonoDevelop.Packaging.Project";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};

			var workspaceItem = await template.CreateWorkspaceItem (cinfo);
			string solutionFileName = Path.Combine (dir, "SolutionName.sln");
			await workspaceItem.SaveAsync (solutionFileName, Util.GetMonitor ());

			string projectFileName = Path.Combine (dir, "ProjectName.nuproj");
			var project = await MSBuildProject.LoadAsync (projectFileName);

			// First element is NuGet.Packaging.props
			var import = project.GetAllObjects ().FirstOrDefault () as MSBuildImport;
			Assert.AreEqual (import.Project, @"$(NuGetAuthoringPath)\NuGet.Packaging.Authoring.props");

			// NuGet.Packaging.targets exists.
			import = project.Imports.LastOrDefault () as MSBuildImport;
			Assert.AreEqual (import.Project, @"$(NuGetAuthoringPath)\NuGet.Packaging.Authoring.targets");

			int count = project.Imports.Count ();
			import = project.Imports.Skip (count - 2).FirstOrDefault ();
			Assert.AreEqual (import.Project, @"$(MSBuildBinPath)\Microsoft.Common.targets");
		}

		[Test]
		public async Task BuildPackagingProjectFromTemplate ()
		{
			string templateId = "MonoDevelop.Packaging.Project";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};
			cinfo.Parameters["PackageAuthors"] = "authors";
			cinfo.Parameters["PackageId"] = "ProjectName";
			cinfo.Parameters["PackageDescription"] = "Description";
			cinfo.Parameters["PackageVersion"] = "1.0.0";

			var workspaceItem = await template.CreateWorkspaceItem (cinfo);
			string solutionFileName = Path.Combine (dir, "SolutionName.sln");
			await workspaceItem.SaveAsync (solutionFileName, Util.GetMonitor ());

			await NuGetPackageInstaller.InstallPackages ((Solution)workspaceItem, template.PackageReferencesForCreatedProjects);

			var solution = (Solution)await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

			// Ensure readme.txt has metadata to include it in the NuGet package.
			var wizard = new TestablePackagingProjectTemplateWizard ();
			wizard.ItemsCreated (new [] { solution });

			BuildResult cr = await solution.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);

			string packageFileName = Path.Combine (dir, "bin", "Debug", "ProjectName.1.0.0.nupkg");
			bool packageCreated = File.Exists (packageFileName);
			Assert.IsTrue (packageCreated, "NuGet package not created.");
		}

		[Test]
		public async Task CreateMultiPlatformProjectFromTemplateWithAndroidOnly ()
		{
			if (!Platform.IsMac)
				Assert.Ignore ("Platform not Mac - Ignoring CreateMultiPlatformProjectFromTemplateWithAndroidOnly");

			string templateId = "MonoDevelop.Packaging.CrossPlatformLibrary";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};
			cinfo.Parameters["CreateAndroidProject"] = bool.TrueString;
			cinfo.Parameters["CreateSharedProject"] = bool.TrueString;
			cinfo.Parameters["CreateNuGetProject"] = bool.TrueString;

			var workspaceItem = await template.CreateWorkspaceItem (cinfo);
			string solutionFileName = Path.Combine (dir, "SolutionName.sln");
			await workspaceItem.SaveAsync (solutionFileName, Util.GetMonitor ());

			var solution = (Solution) await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);

			var project = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.FileName.FileName == "ProjectName.NuGet.nuproj");
			Assert.IsNotNull (project);

			var androidProject = solution.GetAllProjects ().OfType<DotNetProject> ().FirstOrDefault (p => p.FileName.FileName == "ProjectName.Android.csproj");
			Assert.IsNotNull (androidProject);

			var sharedProject = solution.GetAllProjects ().OfType<SharedAssetsProject> ().FirstOrDefault (p => p.FileName.FileName == "ProjectName.Shared.shproj");
			Assert.IsNotNull (sharedProject);

			var projectReference = project.References.FirstOrDefault (r => r.ReferenceType == ReferenceType.Project);
			Assert.AreEqual (androidProject, projectReference.ResolveProject (solution));

			projectReference = androidProject.References.FirstOrDefault (r => r.ReferenceType == ReferenceType.Project);
			Assert.AreEqual (sharedProject, projectReference.ResolveProject (solution));
		}

		[Test]
		[Ignore] // https://github.com/mono/monodevelop/issues/5602
		public async Task CreateMultiPlatformProjectFromTemplateWithPCLOnly ()
		{
			string templateId = "MonoDevelop.Packaging.CrossPlatformLibrary";
			var template = ProjectTemplate.ProjectTemplates.FirstOrDefault (t => t.Id == templateId);
			var dir = Util.CreateTmpDir (template.Id);
			var cinfo = new ProjectCreateInformation {
				ProjectBasePath = dir,
				ProjectName = "ProjectName",
				SolutionName = "SolutionName",
				SolutionPath = dir
			};
			cinfo.Parameters["ProjectName"] = cinfo.ProjectName;
			cinfo.Parameters["CreatePortableProject"] = bool.TrueString;
			cinfo.Parameters["PackageAuthors"] = "authors";
			cinfo.Parameters["PackageId"] = "ProjectName";
			cinfo.Parameters["PackageDescription"] = "Description";
			cinfo.Parameters["PackageVersion"] = "1.0.0";

			var workspaceItem = await template.CreateWorkspaceItem (cinfo);

			var wizard = new TestableCrossPlatformLibraryTemplateWizard ();
			wizard.Parameters = cinfo.Parameters;
			wizard.ItemsCreated (new [] { workspaceItem });

			var project = ((Solution)workspaceItem).GetAllProjects ().First ();
			project.MSBuildProject.GetGlobalPropertyGroup ().SetValue ("PackOnBuild", "true");
			string solutionFileName = Path.Combine (dir, "SolutionName.sln");
			await workspaceItem.SaveAsync (solutionFileName, Util.GetMonitor ());

			await NuGetPackageInstaller.InstallPackages ((Solution)workspaceItem, template.PackageReferencesForCreatedProjects);

			var solution = (Solution)await Ide.Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			project = solution.GetAllProjects ().First ();
			BuildResult cr = await solution.Build (Util.GetMonitor (), "Debug");
			Assert.IsNotNull (cr);
			Assert.AreEqual (0, cr.ErrorCount);
			Assert.AreEqual (0, cr.WarningCount);

			string packageFileName = Path.Combine (dir, "bin", "Debug", "ProjectName.1.0.0.nupkg");
			bool packageCreated = File.Exists (packageFileName);
			Assert.IsTrue (packageCreated, "NuGet package not created.");
		}
	}
}

