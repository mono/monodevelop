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
using System.Diagnostics;
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

		/// <summary>
		/// ProjectName.nuget.g.props and ProjectName.nuget.g.targets files are imported by Microsoft.Common.props
		/// and Microsoft.Common.targets that are included with Mono:
		///
		/// /Library/Frameworks/Mono.framework/Versions/5.4.0/lib/mono/xbuild/15.0/Microsoft.Common.props
		/// /Library/Frameworks/Mono.framework/Versions/5.4.0/lib/mono/msbuild/15.0/bin/Microsoft.Common.targets
		/// </summary>
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

			FilePath projectFile = sol.BaseDirectory.Combine ("DotNetCoreNetStandardProject", "DotNetCoreNetStandardProject.csproj");
			var dotNetCoreProject = (Project)await sol.RootFolder.AddItem (Util.GetMonitor (), projectFile);
			//var dotNetCoreProject = (DotNetProject)sol.Items [0];
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

		/// <summary>
		/// Project defines an MSBuild property in an imported file which is used as the value of
		/// TargetFrameworks in the main project file.
		/// </summary>
		[Test]
		public async Task LoadDotNetCoreProjectWithMultipleTargetFrameworksDefinedByMSBuildProperty ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreMultiTargetFrameworkProperty", "DotNetCoreMultiTargetFrameworkProperty.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			var capabilities = p.GetProjectCapabilities ().ToList ();

			Assert.That (capabilities, Contains.Item ("TestCapabilityNetStandard10"));
			Assert.That (capabilities, Has.None.EqualTo ("TestCapabilityNetStandard11"));

			await p.ReevaluateProject (Util.GetMonitor ());

			capabilities = p.GetProjectCapabilities ().ToList ();

			Assert.That (capabilities, Contains.Item ("TestCapabilityNetStandard10"));
			Assert.That (capabilities, Has.None.EqualTo ("TestCapabilityNetStandard11"));

			sol.Dispose ();
		}

		/// <summary>
		/// Tests that metadata from the imported file globs for the Compile update items is not saved
		/// in the main project file. The DependentUpon property was being saved with the evaluated
		/// filename.
		/// 
		/// Compile Update="**\*.xaml$(DefaultLanguageSourceExtension)" DependentUpon="%(Filename)" SubType="Code"
		/// </summary>
		[Test]
		public async Task SaveNetStandardProjectWithXamarinFormsVersion24PackageReference ()
		{
			FilePath solFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms.sln");

			var process = Process.Start ("msbuild", $"/t:Restore \"{solFile}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			string expectedProjectXml = File.ReadAllText (p.FileName);

			var xamlCSharpFile = p.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml.cs");
			var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml");

			Assert.AreEqual (xamlFile, xamlCSharpFile.DependsOnFile);

			// Ensure the expanded %(FileName) does not get added to the main project on saving.
			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (expectedProjectXml, projectXml);

			sol.Dispose ();
		}

		[Test]
		public async Task AddFiles_NetStandardProjectWithXamarinFormsVersion24PackageReference ()
		{
			FilePath solFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms.sln");

			var process = Process.Start ("msbuild", $"/t:Restore \"{solFile}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			string expectedProjectXml = File.ReadAllText (p.FileName);

			// Add new xaml files.
			var xamlFileName1 = p.BaseDirectory.Combine ("MyView1.xaml");
			File.WriteAllText (xamlFileName1, "xaml1");
			var xamlCSharpFileName = p.BaseDirectory.Combine ("MyView1.xaml.cs");
			File.WriteAllText (xamlCSharpFileName, "csharpxaml");

			// Xaml file with Generator and Subtype set to match that defined in the glob.
			var xamlFile1 = new ProjectFile (xamlFileName1, BuildAction.EmbeddedResource);
			xamlFile1.Generator = "MSBuild:UpdateDesignTimeXaml";
			xamlFile1.ContentType = "Designer";
			p.Files.Add (xamlFile1);

			var xamlCSharpFile = p.AddFile (xamlCSharpFileName);
			xamlCSharpFile.DependsOn = "MyView1.xaml";

			// The project file should be unchanged after saving.
			await p.SaveAsync (Util.GetMonitor ());

			string projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (expectedProjectXml, projectXml);

			// Save again. A second save was adding an include for the .xaml file whilst
			// the first save was not.
			await p.SaveAsync (Util.GetMonitor ());

			projectXml = File.ReadAllText (p.FileName);
			Assert.AreEqual (expectedProjectXml, projectXml);

			sol.Dispose ();
		}

		/// <summary>
		/// Adds a .xaml and .xaml.cs file, then excludes them from the project, then
		/// removes the remove items from the project file by overwriting it, then
		/// reload the project. The DependsOn information was being lost after the
		/// items were first excluded from the project.
		/// </summary>
		[Test]
		public async Task ReloadModifiedFile_XamarinFormsVersion24PackageReference ()
		{
			FilePath solFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms.sln");

			var process = Process.Start ("msbuild", $"/t:Restore \"{solFile}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];
			string originalProjectXml = File.ReadAllText (p.FileName);

			var xamlCSharpFile = p.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml.cs");
			var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml");

			Assert.AreEqual (xamlFile, xamlCSharpFile.DependsOnFile);

			p.Files.Remove (xamlCSharpFile);
			p.Files.Remove (xamlFile);

			await p.SaveAsync (Util.GetMonitor ());

			// Remove items added.
			string projectXml = File.ReadAllText (p.FileName);
			string expectedProjectXml = File.ReadAllText (p.FileName.ChangeName ("NetStandardXamarinForms-saved"));
			Assert.AreEqual (expectedProjectXml, projectXml);

			File.WriteAllText (p.FileName, originalProjectXml);
			var reloadedProject = (DotNetProject)await sol.RootFolder.ReloadItem (Util.GetMonitor (), p);

			xamlCSharpFile = reloadedProject.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml.cs");
			xamlFile = reloadedProject.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml");

			Assert.AreEqual (xamlFile.FilePath.ToString (), xamlCSharpFile.DependsOn);
			Assert.AreEqual (xamlCSharpFile.DependsOnFile, xamlFile);

			sol.Dispose ();
		}

		/// <summary>
		/// Verifies that the .xaml files can be found by ProjectFileCollection's GetFile method
		/// after the project is re-evaluated after the package references are restored the first time
		/// for the project. There is a separate lookup cache of files in the ProjectFileCollection.
		/// </summary>
		[Test]
		public async Task ReevaluateXamarinFormsVersion24PackageReference ()
		{
			FilePath solFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (Project)sol.Items [0];
				var xamlCSharpFile = p.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml.cs");
				var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "MyPage.xaml");

				// No generator set before NuGet restore and re-evaluation.
				Assert.AreEqual (string.Empty, xamlFile.Generator);
				// No DependsOn set until NuGet restore and re-evaluation.
				Assert.AreEqual (string.Empty, xamlCSharpFile.DependsOn);

				var process = Process.Start ("msbuild", $"/t:Restore \"{solFile}\"");
				Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
				Assert.AreEqual (0, process.ExitCode);

				await p.ReevaluateProject (Util.GetMonitor ());

				xamlCSharpFile = p.Files.GetFile (xamlCSharpFile.FilePath);
				xamlFile = p.Files.GetFile (xamlFile.FilePath);

				Assert.IsNotNull (xamlCSharpFile);
				Assert.IsNotNull (xamlFile);
				Assert.AreEqual ("MSBuild:UpdateDesignTimeXaml", xamlFile.Generator);
				Assert.AreEqual (xamlFile, xamlCSharpFile.DependsOnFile);

				Assert.IsNotNull (p.GetProjectFile (xamlCSharpFile.FilePath));
				Assert.IsNotNull (p.GetProjectFile (xamlFile.FilePath));

				xamlCSharpFile = p.GetProjectFile (xamlCSharpFile.FilePath);
				xamlFile = p.GetProjectFile(xamlFile.FilePath);

				Assert.IsNotNull (xamlCSharpFile);
				Assert.IsNotNull (xamlFile);
				Assert.AreEqual ("MSBuild:UpdateDesignTimeXaml", xamlFile.Generator);
				Assert.AreEqual (xamlFile, xamlCSharpFile.DependsOnFile);
			}
		}

		/// <summary>
		/// Verifes that the DependentUpon property is correct for .xaml.cs files in a subdirectory.
		/// </summary>
		[Test]
		public async Task DependsOn_FilesInProjectSubDirectory_XamarinFormsVersion24PackageReference ()
		{
			FilePath solFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms.sln");
			FilePath viewsSubDirectory = solFile.ParentDirectory.Combine ("NetStandardXamarinForms", "Views");
			Directory.CreateDirectory (viewsSubDirectory);

			// Add new xaml files.
			var xamlFileName = viewsSubDirectory.Combine ("MyView.xaml");
			File.WriteAllText (xamlFileName, "xaml1");
			var xamlCSharpFileName = viewsSubDirectory.Combine ("MyView.xaml.cs");
			File.WriteAllText (xamlCSharpFileName, "csharpxaml");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (Project)sol.Items [0];

				var process = Process.Start ("msbuild", $"/t:Restore \"{solFile}\"");
				Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
				Assert.AreEqual (0, process.ExitCode);

				await p.ReevaluateProject (Util.GetMonitor ());

				var xamlCSharpFile = p.Files.Single (fi => fi.FilePath.FileName == "MyView.xaml.cs");
				var xamlFile = p.Files.Single (fi => fi.FilePath.FileName == "MyView.xaml");

				Assert.IsNotNull (xamlCSharpFile);
				Assert.IsNotNull (xamlFile);
				Assert.AreEqual ("MSBuild:UpdateDesignTimeXaml", xamlFile.Generator);
				Assert.AreEqual (xamlFile, xamlCSharpFile.DependsOnFile);
				Assert.AreEqual ("MyView.xaml", xamlCSharpFile.Metadata.GetValue ("DependentUpon"));
				Assert.AreEqual (xamlFileName.ToString (), xamlCSharpFile.DependsOn);
			}
		}

		[Test]
		public async Task DotNetCoreNoMainPropertyGroup ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreNoMainPropertyGroup", "DotNetCoreNoMainPropertyGroup.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (Project)sol.Items [0];
				Assert.AreEqual ("DotNetCoreNoMainPropertyGroup", p.Name);

				var process = Process.Start ("msbuild", $"/t:Restore \"{solFile}\"");
				Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
				Assert.AreEqual (0, process.ExitCode);

				await p.ReevaluateProject (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				await p.SaveAsync (Util.GetMonitor ());

				// Project xml should not be changed.
				string savedProjectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (projectXml, savedProjectXml);
			}
		}

		[Test]
		public async Task ItemDefinitionGroup ()
		{
			FilePath projFile = Util.GetSampleProject ("project-with-item-def-group", "netstandard-sdk.csproj");
			RunMSBuildRestore (projFile);

			using (var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				var projectItem = p.Files.Single (f => f.Include == "Class1.cs");

				Assert.AreEqual ("NewValue", projectItem.Metadata.GetValue ("OverriddenProperty"));
				Assert.AreEqual ("Test", projectItem.Metadata.GetValue ("TestProperty"));
				Assert.AreEqual (FileCopyMode.PreserveNewest, projectItem.CopyToOutputDirectory);
			}
		}

		[Test]
		public async Task ItemDefinitionGroup_AddFileWithSameMetadataAsItemDefinition_MetadataNotSaved ()
		{
			FilePath projFile = Util.GetSampleProject ("project-with-item-def-group", "netstandard-sdk.csproj");
			RunMSBuildRestore (projFile);

			using (var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				var projectItem = p.Files.Single (f => f.Include == "Class1.cs");

				var refXml = File.ReadAllText (p.FileName);

				var newItemFileName = projectItem.FilePath.ChangeName ("NewItem");
				var newProjectItem = new ProjectFile (newItemFileName, projectItem.BuildAction);
				newProjectItem.CopyToOutputDirectory = FileCopyMode.PreserveNewest;
				newProjectItem.Metadata.SetValue ("TestProperty", "Test");
				newProjectItem.Metadata.SetValue ("OverriddenProperty", "OriginalValue");

				File.WriteAllText (newItemFileName, "class NewItem {}");

				p.Files.Add (newProjectItem);
				await p.SaveAsync (Util.GetMonitor ());

				var savedXml = File.ReadAllText (p.FileName);

				Assert.AreEqual (refXml, savedXml);
			}
		}

		[Test]
		public async Task ItemDefinitionGroup_AddFileWithoutMetadata_MetadataUsesEmptyElements ()
		{
			FilePath projFile = Util.GetSampleProject ("project-with-item-def-group", "netstandard-sdk.csproj");
			RunMSBuildRestore (projFile);

			using (var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				var projectItem = p.Files.Single (f => f.Include == "Class1.cs");

				var newItemFileName = projectItem.FilePath.ChangeName ("NewItem");
				var newProjectItem = new ProjectFile (newItemFileName, projectItem.BuildAction);
				newProjectItem.CopyToOutputDirectory = FileCopyMode.PreserveNewest;

				File.WriteAllText (newItemFileName, "class NewItem {}");

				p.Files.Add (newProjectItem);
				await p.SaveAsync (Util.GetMonitor ());

				var refXml = File.ReadAllText (p.FileName + ".add-file-no-metadata");
				var savedXml = File.ReadAllText (p.FileName);

				Assert.AreEqual (refXml, savedXml);
			}
		}

		[Test]
		public async Task CustomAvailableItemName_FileImportedWithWildcard_FileAvailableInProject ()
		{
			FilePath solFile = Util.GetSampleProject ("sdk-imported-files", "netstandard.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (Project)sol.Items [0];
				var textFile = p.Files.SingleOrDefault (fi => fi.FilePath.FileName == "test.txt");

				Assert.AreEqual ("MyTextFile", textFile.BuildAction);

				// Ensure build actions are not cached too early and contain any custom MSBuild items
				// defined directly in the project file.

				string[] buildActions = p.GetBuildActions ();
				Assert.That (buildActions, Contains.Item ("CustomBuildActionInProject"));
			}
		}

		[Test]
		[Platform (Exclude = "Win")]
		public async Task BuildMultiTargetProject ()
		{
			FilePath projFile = Util.GetSampleProject ("multi-target", "multi-target2.csproj");

			RunMSBuildRestore (projFile);

			using (var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {

				var res = await p.RunTarget (Util.GetMonitor (false), "Build", ConfigurationSelector.Default);
				var buildResult = res.BuildResult;

				var expectedNetCoreOutputFile = projFile.ParentDirectory.Combine ("bin", "Debug", "netcoreapp1.1", "multi-target2.dll");
				var expectedNetStandardOutputFile = projFile.ParentDirectory.Combine ("bin", "Debug", "netstandard1.0", "multi-target2.dll");

				Assert.AreEqual (0, buildResult.Errors.Count);
				Assert.IsTrue (File.Exists (expectedNetCoreOutputFile), ".NET Core assembly not built");
				Assert.IsTrue (File.Exists (expectedNetStandardOutputFile), ".NET Standard assembly not built");

				res = await p.RunTarget (Util.GetMonitor (false), "Clean", ConfigurationSelector.Default);
				buildResult = res.BuildResult;

				Assert.AreEqual (0, buildResult.Errors.Count);
				Assert.IsFalse (File.Exists (expectedNetCoreOutputFile), ".NET Core assembly not removed on clean");
				Assert.IsFalse (File.Exists (expectedNetStandardOutputFile), ".NET Standard assembly not removed on clean");
			}
		}

		/// <summary>
		/// Tests that on saving the project after making a small change the .xaml.fs files still depend on the
		/// correct .xaml file.
		/// </summary>
		[Test]
		[Platform (Exclude = "Win")]
		public async Task FSharpXamarinFormsProject_SaveProject_XamlFilesDependentUponUnchanged ()
		{
			FilePath solFile = Util.GetSampleProject ("FSharpForms", "FSharpForms.sln");

			RunMSBuildRestore (solFile);

			using (var s = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = s.GetAllProjects ().OfType<DotNetProject> ().Single ();

				await p.SaveAsync (Util.GetMonitor ());

				// Re-evaluate to force the Project.Files to be refreshed. This would cause the DependsUpon
				// to be changed for the ProjectFiles.
				await p.ReevaluateProject (Util.GetMonitor ());

				var mainPageXamlFSharp = p.Files.FirstOrDefault (f => f.FilePath.FileName == "MainPage.xaml.fs");
				var appXamlFSharp = p.Files.FirstOrDefault (f => f.FilePath.FileName == "App.xaml.fs");

				Assert.IsTrue (appXamlFSharp.DependsOn.EndsWith ("App.xaml", StringComparison.Ordinal), "Should end with App.xaml was '{0}'", appXamlFSharp.DependsOn);
				Assert.IsTrue (mainPageXamlFSharp.DependsOn.EndsWith ("MainPage.xaml", StringComparison.Ordinal), "Should end with MainPage.xaml was '{0}'", mainPageXamlFSharp.DependsOn);
			}
		}

		[Test]
		public async Task LoadProject_UnknownNuGetSDKPackage_SDKResolutionErrorsReported ()
		{
			FilePath solFile = Util.GetSampleProject ("unknown-nuget-sdk", "UnknownNuGetSdk.sln");

			using (var item = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = item.Items [0] as UnknownSolutionItem;
				Assert.AreEqual (p.LoadError, "Unable to find SDK 'Test.Unknown.NET.Sdk/1.2.3'");
			}
		}

		static void RunMSBuildRestore (FilePath fileName)
		{
			CreateNuGetConfigFile (fileName.ParentDirectory);

			var process = Process.Start ("msbuild", $"/t:Restore \"{fileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);
		}

		/// <summary>
		/// Clear all other package sources and just use the main NuGet package source when
		/// restoring the packages for the project tests.
		/// </summary>
		static void CreateNuGetConfigFile (FilePath directory)
		{
			var fileName = directory.Combine ("NuGet.Config");

			string xml =
				"<configuration>\r\n" +
				"  <packageSources>\r\n" +
				"    <clear />\r\n" +
				"    <add key=\"NuGet v3 Official\" value=\"https://api.nuget.org/v3/index.json\" />\r\n" +
				"  </packageSources>\r\n" +
				"</configuration>";

			File.WriteAllText (fileName, xml);
		}
	}
}
