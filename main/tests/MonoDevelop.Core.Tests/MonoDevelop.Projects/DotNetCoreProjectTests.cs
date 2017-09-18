//
// DotNetCoreProjectTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.IO;
using System.Xml;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class DotNetCoreProjectTests: TestBase
	{
		/// <summary>
		/// EnableDefaultItems set to false in project. C# file directly in
		/// project. The Sdk imports define C# files if EnableDefaultItems is true.
		/// This test ensures that duplicate files are not added to the project. This
		/// happens because Project.EvaluatedItemsIgnoringCondition is used when adding
		/// files to the project.
		/// </summary>
		[Test]
		public async Task LoadDotNetCoreProjectWithDefaultItemsDisabled ()
		{
			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-disable-default-items.sln");
			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);

			try {
				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				var files = mp.Files.Select (f => f.FilePath.FileName).ToArray ();
				Assert.AreEqual (new string [] {
					"Program.cs"
				}, files);

				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		[Test]
		public async Task AddFileToDotNetCoreProjectWithDefaultItemsDisabled ()
		{
			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-disable-default-items.sln");
			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);

			try {
				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;
				var files = mp.Files.Select (f => f.FilePath.FileName).ToArray ();
				Assert.AreEqual (new string [] {
					"Program.cs"
				}, files);

				string newFile = mp.Files [0].FilePath.ChangeName ("NewFile");
				File.WriteAllText (newFile, string.Empty);
				mp.AddFile (newFile);
				await mp.SaveAsync (Util.GetMonitor ());

				var itemGroup = mp.MSBuildProject.ItemGroups.LastOrDefault ();
				Assert.IsTrue (itemGroup.Items.Any (item => item.Include == "NewFile.cs"));
				Assert.AreEqual (2, itemGroup.Items.Count ());
				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		[Test]
		public async Task GeneratedNuGetMSBuildFilesAreImportedWithDotNetCoreProject ()
		{
			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-disable-default-items.sln");
			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			FilePath baseIntermediateOutputPath = solFile.ParentDirectory.Combine ("dotnetcore-console", "obj");
			string projectFileName = "dotnetcore-disable-default-items.csproj";

			try {
				string nugetProps =
					"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
					"  <PropertyGroup>\r\n" +
					"    <NuGetPropsImported>True</NuGetPropsImported>\r\n" +
					"  </PropertyGroup>\r\n" +
					"</Project>";

				string nugetTargets =
					"<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">\r\n" +
					"  <PropertyGroup>\r\n" +
					"    <NuGetTargetsImported>True</NuGetTargetsImported>\r\n" +
					"  </PropertyGroup>\r\n" +
					"</Project>";

				Directory.CreateDirectory (baseIntermediateOutputPath);
				File.WriteAllText (baseIntermediateOutputPath.Combine (projectFileName + ".nuget.g.props"), nugetProps);
				File.WriteAllText (baseIntermediateOutputPath.Combine (projectFileName + ".nuget.g.targets"), nugetTargets);

				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				Assert.IsInstanceOf<Project> (p);
				var mp = (Project)p;

				Assert.AreEqual ("True", p.MSBuildProject.EvaluatedProperties.GetValue ("NuGetPropsImported"));
				Assert.AreEqual ("True", p.MSBuildProject.EvaluatedProperties.GetValue ("NuGetTargetsImported"));

				sol.Dispose ();
			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		/// <summary>
		/// Tests that the MSBuildSDKsPath property is set when building a project.
		/// This is used by Microsoft.NET.Sdk.Web .NET Core projects when importing
		/// other MSBuild .targets and .props.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		[Ignore]
		public async Task BuildDotNetCoreProjectWithImportUsingMSBuildSDKsPathProperty ()
		{
			// This test is being ignored for now because relying on MSBuildSDKsPath is not entirely correct,
			// the correct approach is to use the Sdk attribute in the import.
			// In any case this currently works for web projects because MSBuildSDKsPath ends being resolved
			// to the Mono's msbuild dir, which has the web targets.

			FilePath solFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-msbuildsdkspath-import.sln");

			FilePath sdksPath = solFile.ParentDirectory.Combine ("Sdks");
			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);

			try {
				var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var p = (Project)sol.Items [0];
				p.RequiresMicrosoftBuild = true;

				p.DefaultConfiguration = new DotNetProjectConfiguration ("Debug") {
					OutputAssembly = p.BaseDirectory.Combine ("bin", "test.dll")
				};
				var res = await p.RunTarget (Util.GetMonitor (), "Build", ConfigurationSelector.Default);
				var buildResult = res.BuildResult;

				Assert.AreEqual (1, buildResult.Errors.Count);
				string expectedMessage = string.Format ("Something failed (test-import.targets): {0}", sdksPath);
				Assert.AreEqual (expectedMessage, buildResult.Errors [0].ErrorText);

				sol.Dispose ();

			} finally {
				MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdksPath);
			}
		}

		/// <summary>
		/// The MSBuildSDKsPath property needs to be set when a non .NET Core project references
		/// a .NET Core project. Otherwise the build will fail with an error similar to:
		/// Error MSB4019: The imported project "~/Library/Caches/VisualStudio/7.0/MSBuild/8209_1/Sdks/Microsoft.NET.Sdk/Sdk/Sdk.props"
		/// was not found.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildProjectReferencingDotNetCoreProject ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreProjectReference", "DotNetCoreProjectReference.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			Assert.AreEqual ("DotNetFrameworkProject", p.Name);
			p.RequiresMicrosoftBuild = true;

			p.DefaultConfiguration = new DotNetProjectConfiguration ("Debug") {
				OutputAssembly = p.BaseDirectory.Combine ("bin", "test.dll")
			};
			var res = await p.RunTarget (Util.GetMonitor (false), "Build", ConfigurationSelector.Default);
			var buildResult = res.BuildResult;

			Assert.AreEqual (0, buildResult.Errors.Count);

			sol.Dispose ();
		}

		/// <summary>
		/// Ensure project builder is updated when SdksPath changes after referencing a .NET Core project
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildProjectAfterReferencingDotNetCoreProject ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreProjectReference", "NoProjectReference.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];
			Assert.AreEqual ("DotNetFrameworkProject", p.Name);
			p.RequiresMicrosoftBuild = true;

			FilePath projectFile = sol.BaseDirectory.Combine ("DotNetCoreNetStandardProject", "DotNetCoreNetStandardProject.csproj");
			var dotNetCoreProject = (Project)await sol.RootFolder.AddItem (Util.GetMonitor (), projectFile);
			//var dotNetCoreProject = (DotNetProject)sol.Items [0];
			dotNetCoreProject.RequiresMicrosoftBuild = true;
			await sol.SaveAsync (Util.GetMonitor ());

			p.DefaultConfiguration = new DotNetProjectConfiguration ("Debug") {
				OutputAssembly = p.BaseDirectory.Combine ("bin", "test.dll")
			};
			var res = await p.RunTarget (Util.GetMonitor (false), "Clean", ConfigurationSelector.Default);

			var pr = ProjectReference.CreateProjectReference ((DotNetProject)dotNetCoreProject);
			pr.ReferenceOutputAssembly = false;
			pr.LocalCopy = false;
			p.References.Add (pr);
			await p.SaveAsync (Util.GetMonitor ());

			res = await p.RunTarget (Util.GetMonitor (false), "Build", ConfigurationSelector.Default);
			var buildResult = res.BuildResult;

			Assert.AreEqual (0, buildResult.Errors.Count);

			sol.Dispose ();
		}

		/// <summary>
		/// Tests that the first target framework is used to evaluate the project.
		/// </summary>
		[Test]
		public async Task LoadDotNetCoreProjectWithMultipleTargetFrameworks ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreMultiTargetFramework", "DotNetCoreMultiTargetFramework.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			var capabilities = p.GetProjectCapabilities ().ToList ();

			Assert.That (capabilities, Contains.Item ("TestCapabilityNetStandard"));
			Assert.That (capabilities, Has.None.EqualTo ("TestCapabilityNetCoreApp"));

			await p.ReevaluateProject (Util.GetMonitor ());

			capabilities = p.GetProjectCapabilities ().ToList ();

			Assert.That (capabilities, Contains.Item ("TestCapabilityNetStandard"));
			Assert.That (capabilities, Has.None.EqualTo ("TestCapabilityNetCoreApp"));

			sol.Dispose ();
		}
	}
}
