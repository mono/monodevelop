//
// DotNetCoreProjectTests.cs
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

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	class DotNetCoreProjectExtensionTests : DotNetCoreTestBase
	{
		Solution solution;

		[TearDown]
		public override void TearDown ()
		{
			solution?.Dispose ();
			solution = null;

			base.TearDown ();
		}

		/// <summary>
		/// ProjectGuid and DefaultTargets should not be added to .NET Core project when it is saved.
		/// </summary>
		[Test]
		public async Task ConsoleProject_SaveProject_DoesNotAddExtraProperties ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-sdk-console.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().Single ();

			// Original project does not have ProjectGuid nor DefaultTargets.
			var globalPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			Assert.IsFalse (globalPropertyGroup.HasProperty ("ProjectGuid"));
			Assert.IsNull (project.MSBuildProject.DefaultTargets);

			await project.SaveAsync (Util.GetMonitor ());

			// Reload project.
			solution.Dispose ();
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			project = solution.GetAllProjects ().Single ();

			globalPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();

			Assert.IsFalse (globalPropertyGroup.HasProperty ("ProjectGuid"));
			Assert.IsNull (project.MSBuildProject.DefaultTargets);
			Assert.AreEqual ("15.0", project.MSBuildProject.ToolsVersion);
		}

		[Test]
		public async Task SdkConsoleProject_AddPackageReference_VersionWrittenAsAttribute ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-sdk-console.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().Single ();
			string projectFileName = project.FileName;

			var packageReference = ProjectPackageReference.Create ("Test", "1.2.3");
			project.Items.Add (packageReference);

			await project.SaveAsync (Util.GetMonitor ());

			// Reload project.
			var doc = new XmlDocument ();
			doc.Load (projectFileName);

			var itemGroupElement = (XmlElement)doc.DocumentElement.ChildNodes[1];
			var packageReferenceElement = (XmlElement)itemGroupElement.ChildNodes[1];

			Assert.AreEqual ("PackageReference", packageReferenceElement.Name);
			Assert.AreEqual ("Test", packageReferenceElement.GetAttribute ("Include"));
			Assert.AreEqual ("1.2.3", packageReferenceElement.GetAttribute ("Version"));
			Assert.AreEqual (0, packageReferenceElement.ChildNodes.Count);
			Assert.IsTrue (packageReferenceElement.IsEmpty);
		}

		/// <summary>
		/// LibC project references LibB project which references LibA project.
		/// </summary>
		[Test]
		public async Task GetReferences_ThreeProjectReferences_TransitivelyReferencedProjectsIncluded ()
		{
			string solutionFileName = Util.GetSampleProject ("TransitiveProjectReferences", "TransitiveProjectReferences.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var projectLibC = solution.FindProjectByName ("LibC") as DotNetProject;
			var projectLibB = solution.FindProjectByName ("LibB") as DotNetProject;
			var projectLibA = solution.FindProjectByName ("LibA") as DotNetProject;

			var referencesLibC = await projectLibC.GetReferences (ConfigurationSelector.Default);
			var referencesLibB = await projectLibB.GetReferences (ConfigurationSelector.Default);

			var projectReferencesLibC = referencesLibC.Where (r => r.IsProjectReference).ToList ();
			var projectReferencesLibB = referencesLibB.Where (r => r.IsProjectReference).ToList ();
			var libCToLibAProjectReference = projectReferencesLibC.FirstOrDefault (r => r.FilePath.FileName == "LibA.dll");
			var libCToLibBProjectReference = projectReferencesLibC.FirstOrDefault (r => r.FilePath.FileName == "LibB.dll");
			var libBToLibAProjectReference = projectReferencesLibB.FirstOrDefault ();

			Assert.AreEqual (1, projectReferencesLibB.Count);
			Assert.AreEqual (2, projectReferencesLibC.Count);
			Assert.AreEqual (projectLibA, libBToLibAProjectReference.GetReferencedItem (solution));
			Assert.AreEqual (projectLibA, libCToLibAProjectReference.GetReferencedItem (solution));
			Assert.AreEqual (projectLibB, libCToLibBProjectReference.GetReferencedItem (solution));
			Assert.IsTrue (libBToLibAProjectReference.ReferenceOutputAssembly);
			Assert.IsTrue (libCToLibAProjectReference.ReferenceOutputAssembly);
			Assert.IsTrue (libCToLibBProjectReference.ReferenceOutputAssembly);
			Assert.IsTrue (libBToLibAProjectReference.IsCopyLocal);
			Assert.IsTrue (libCToLibAProjectReference.IsCopyLocal);
			Assert.IsTrue (libCToLibBProjectReference.IsCopyLocal);
			Assert.AreEqual (1, projectLibC.References.Count);
			Assert.AreEqual (1, projectLibB.References.Count);
		}

		/// <summary>
		/// Similar to above test but ReferenceOutputAssembly is set to false on the LibB project reference
		/// defined in the LibC project so the transitive project reference should not be included.
		/// </summary>
		[Test]
		public async Task GetReferences_ThreeProjectReferencesAndReferenceOutputAssemblyIsFalse_ReferenceOutputAssemblyIsFalseProjectsNotReturned ()
		{
			string solutionFileName = Util.GetSampleProject ("TransitiveProjectReferences", "TransitiveProjectReferences.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var projectLibC = solution.FindProjectByName ("LibC") as DotNetProject;
			var projectLibB = solution.FindProjectByName ("LibB") as DotNetProject;
			var projectLibA = solution.FindProjectByName ("LibA") as DotNetProject;

			var projectReference = projectLibC.References.Single (r => r.ReferenceType == ReferenceType.Project);
			projectReference.ReferenceOutputAssembly = false;
			await projectLibC.SaveAsync (Util.GetMonitor ());

			var referencesLibC = await projectLibC.GetReferences (ConfigurationSelector.Default);
			var referencesLibB = await projectLibB.GetReferences (ConfigurationSelector.Default);

			var projectReferencesLibC = referencesLibC.Where (r => r.IsProjectReference).ToList ();
			var projectReferencesLibB = referencesLibB.Where (r => r.IsProjectReference).ToList ();

			Assert.AreEqual (1, projectReferencesLibB.Count);
			Assert.AreEqual (1, projectReferencesLibC.Count);
			Assert.IsTrue (projectReferencesLibB [0].ReferenceOutputAssembly);
			Assert.IsFalse (projectReferencesLibC [0].ReferenceOutputAssembly);
		}

		[Test]
		public async Task GetReferences_ThreeProjectReferencesJsonNet_JsonNetReferenceAvailableToReferencingProjects ()
		{
			string solutionFileName = Util.GetSampleProject ("TransitiveProjectReferences", "TransitiveProjectReferences.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var projectLibC = solution.FindProjectByName ("LibC") as DotNetProject;
			var projectLibB = solution.FindProjectByName ("LibB") as DotNetProject;
			var projectLibA = solution.FindProjectByName ("LibA") as DotNetProject;

			//string fileName = GetType ().Assembly.Location;
			//var reference = ProjectReference.CreateAssemblyFileReference (fileName);
			//projectLibA.Items.Add (reference);
			var packageReference = ProjectPackageReference.Create ("Newtonsoft.Json", "10.0.1");
			projectLibA.Items.Add (packageReference);
			await projectLibA.SaveAsync (Util.GetMonitor ());

			CreateNuGetConfigFile (solution.BaseDirectory);

			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{solutionFileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);

			var referencesLibC = await projectLibC.GetReferences (ConfigurationSelector.Default);
			var referencesLibB = await projectLibB.GetReferences (ConfigurationSelector.Default);
			var referencesLibA = await projectLibA.GetReferences (ConfigurationSelector.Default);

			var jsonNetReferenceLibC = referencesLibC.FirstOrDefault (r => r.FilePath.FileName == "Newtonsoft.Json.dll");
			var jsonNetReferenceLibB = referencesLibB.FirstOrDefault (r => r.FilePath.FileName == "Newtonsoft.Json.dll");
			var jsonNetReferenceLibA = referencesLibA.FirstOrDefault (r => r.FilePath.FileName == "Newtonsoft.Json.dll");

			Assert.IsNotNull (jsonNetReferenceLibA);
			Assert.IsNotNull (jsonNetReferenceLibB);
			Assert.IsNotNull (jsonNetReferenceLibC);
		}

		/// <summary>
		/// Mirror Visual Studio on Windows behaviour where a .NET Standard project or a .NET Core
		/// project can add a reference to any PCL project.
		/// </summary>
		[Test]
		public async Task CanReference_PortableClassLibrary_FromNetStandardOrNetCoreAppProject ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-pcl", "dotnetcore-pcl.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var pclProject = solution.FindProjectByName ("PclProfile111") as DotNetProject;
			var netStandardProject = solution.FindProjectByName ("NetStandard14") as DotNetProject;
			var netCoreProject = solution.FindProjectByName ("NetCore11") as DotNetProject;

			string reason = null;
			bool canReferenceFromNetStandard = netStandardProject.CanReferenceProject (pclProject, out reason);
			bool canReferenceFromNetCore = netCoreProject.CanReferenceProject (pclProject, out reason);
			bool canReferenceNetCoreFromNetStandard = netStandardProject.CanReferenceProject (netCoreProject, out reason);
			bool canReferenceNetStandardFromNetCore = netCoreProject.CanReferenceProject (netStandardProject, out reason);

			Assert.IsTrue (canReferenceFromNetStandard);
			Assert.IsTrue (canReferenceFromNetCore);
			Assert.IsFalse (canReferenceNetCoreFromNetStandard);
			Assert.IsTrue (canReferenceNetStandardFromNetCore);
		}

		[Test]
		public async Task TizenProject_OpenProject_LoadedAsDotNetProjectNotUnknownSolutionItem ()
		{
			string solutionFileName = Util.GetSampleProject ("TizenProject", "TizenProject.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.Items.Single (item => item.Name == "TizenProject");

			Assert.IsInstanceOf<DotNetProject> (project);
		}

		[Test]
		public async Task ConsoleProject_UseDefaultMetadataForExcludedExpandedItems_IsTrue ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-sdk-console.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().Single ();

			Assert.IsTrue (project.UseDefaultMetadataForExcludedExpandedItems);
			Assert.IsTrue (project.UseAdvancedGlobSupport);
			Assert.IsTrue (project.UseFileWatcher);
		}

		/// <summary>
		/// ASP.NET Core projects have different build actions for files in the wwwroot folder. This
		/// tests that the correct build actions are used for different folders.
		/// </summary>
		[Test]
		public async Task AspNetCoreProject_DefaultBuildActions ()
		{
			string projectFileName = Util.GetSampleProject ("aspnetcore", "aspnetcore.csproj");
			using (var project = (DotNetProject) await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName)) {

				var fileName = project.BaseDirectory.Combine ("test.txt");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("MyClass.cs");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("Compile", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("MyPage.html");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("MyPage.cshtml");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("Content", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("settings.json");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("Content", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("test.config");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("Content", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("MyPage.resx");
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("EmbeddedResource", project.GetDefaultBuildAction (fileName));

				fileName = project.BaseDirectory.Combine ("wwwroot", "MyPage.html");
				Directory.CreateDirectory (fileName.ParentDirectory);
				File.WriteAllText (fileName, string.Empty);
				Assert.AreEqual ("Content", project.GetDefaultBuildAction (fileName));
			}
		}

		[Test]
		public async Task DotNetCoreProject_DefaultBuildActions ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-sdk-console.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().Single ();

			var fileName = project.BaseDirectory.Combine ("test.txt");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("MyClass.cs");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("Compile", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("MyPage.html");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("MyPage.cshtml");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("settings.json");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("test.config");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("MyPage.resx");
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("EmbeddedResource", project.GetDefaultBuildAction (fileName));

			fileName = project.BaseDirectory.Combine ("wwwroot", "MyPage.html");
			Directory.CreateDirectory (fileName.ParentDirectory);
			File.WriteAllText (fileName, string.Empty);
			Assert.AreEqual ("None", project.GetDefaultBuildAction (fileName));
		}

		[Test]
		public async Task ConsoleProjectNoMainPropertyGroup_ChangeToLibraryAndSaveProject_OutputTypeSavedInProject ()
		{
			string solutionFileName = Util.GetSampleProject ("DotNetCoreNoMainPropertyGroup", "DotNetCoreNoMainPropertyGroup.sln");
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllDotNetProjects ().Single ();

			// Original project does not have OutputType.
			var globalPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();
			Assert.IsFalse (globalPropertyGroup.HasProperty ("OutputType"));
			Assert.AreEqual (CompileTarget.Exe, project.CompileTarget);

			string outputTypeEvaluatedValue = project.MSBuildProject.EvaluatedProperties.GetValue ("OutputType");
			Assert.AreEqual ("Exe", outputTypeEvaluatedValue);

			project.CompileTarget = CompileTarget.Library;
			await project.SaveAsync (Util.GetMonitor ());

			// Reload project.
			solution.Dispose ();
			solution = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			project = solution.GetAllDotNetProjects ().Single ();

			globalPropertyGroup = project.MSBuildProject.GetGlobalPropertyGroup ();

			string outputTypeProperty = globalPropertyGroup.GetValue ("OutputType");
			Assert.AreEqual ("Library", outputTypeProperty);
			Assert.AreEqual (CompileTarget.Library, project.CompileTarget);
		}

		[Test]
		public async Task IsWeb_ProjectHasSdkAttribute ()
		{
			string projFile = Util.GetSampleProject ("DotNetCoreSdkFormat", "DotNetCoreProjectSdk", "DotNetCoreProjectSdk.csproj");

			using (var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				var projectExtension = p.AsFlavor<DotNetCoreProjectExtension> ();

				Assert.AreEqual ("Microsoft.NET.Sdk", p.MSBuildProject.Sdk);
				Assert.IsFalse (projectExtension.IsWeb);

				p.MSBuildProject.Sdk = "Microsoft.NET.Sdk.Web";
				Assert.IsTrue (projectExtension.IsWeb);

				p.MSBuildProject.Sdk = "Microsoft.NET.Sdk;Microsoft.NET.Sdk.Web";
				Assert.IsTrue (projectExtension.IsWeb);
			}
		}

		[Test]
		public async Task LoadDotNetCoreProjectWithSdkNode ()
		{
			string projFile = Util.GetSampleProject ("DotNetCoreSdkFormat", "DotNetCoreSdkNode", "DotNetCoreSdkNode.csproj");

			using (var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				Assert.IsTrue (p.HasFlavor<DotNetCoreProjectExtension> ());
			}
		}

		[Test]
		public async Task LoadDotNetCoreProjectWithSdkImports ()
		{
			string projFile = Util.GetSampleProject ("DotNetCoreSdkFormat", "DotNetCoreImportSdk", "DotNetCoreImportSdk.csproj");

			using (var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				Assert.IsTrue (p.HasFlavor<DotNetCoreProjectExtension> ());
			}
		}

		[Test]
		public async Task SolutionUsingCSharpProjectTypeGuid_SaveSolution_ProjectTypeGuidUnchanged ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-sdk-console.sln");
			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().Single ();
			string solutionFileText = File.ReadAllText (solutionFileName);

			await solution.SaveAsync (Util.GetMonitor ());

			string newSolutionFileText = File.ReadAllText (solutionFileName);
			Assert.AreEqual ("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", project.TypeGuid);
			Assert.AreEqual (solutionFileText, newSolutionFileText);
		}

		[Test]
		public async Task SolutionUsingAlternativeVisualStudioProjectTypeGuid_SaveSolution_ProjectTypeGuidUnchanged ()
		{
			string solutionFileName = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-alternative-guid.sln");
			solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName);
			var project = solution.GetAllProjects ().Single ();
			string solutionFileText = File.ReadAllText (solutionFileName);

			await solution.SaveAsync (Util.GetMonitor ());

			string newSolutionFileText = File.ReadAllText (solutionFileName);
			Assert.AreEqual ("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}", project.TypeGuid);
			Assert.AreEqual (solutionFileText, newSolutionFileText);
		}
	}
}