//
// PackageReferenceNuGetProjectTests.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Versioning;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageReferenceNuGetProjectTests : TestBase
	{
		DotNetProject dotNetProject;
		TestablePackageReferenceNuGetProject project;
		FakeNuGetProjectContext context;
		DependencyGraphCacheContext dependencyGraphCacheContext;

		void CreateNuGetProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			context = new FakeNuGetProjectContext ();
			dotNetProject = new DummyDotNetProject ();
			dotNetProject.Name = projectName;
			dotNetProject.FileName = fileName.ToNativePath ();
			project = new TestablePackageReferenceNuGetProject (dotNetProject);
		}

		ProjectPackageReference AddDotNetProjectPackageReference (string packageId, string version)
		{
			var packageReference = ProjectPackageReference.Create (packageId, version);
			dotNetProject.Items.Add (packageReference);
			return packageReference;
		}

		Task<bool> UninstallPackageAsync (string packageId, string version)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			return project.UninstallPackageAsync (packageIdentity, context, CancellationToken.None);
		}

		Task<bool> InstallPackageAsync (string packageId, string version)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			var versionRange = new VersionRange (packageIdentity.Version);
			return project.InstallPackageAsync (packageId, versionRange, context, null, CancellationToken.None);
		}

		Task<PackageSpec> GetPackageSpecsAsync ()
		{
			dependencyGraphCacheContext = new DependencyGraphCacheContext ();
			return GetPackageSpecsAsync (dependencyGraphCacheContext);
		}

		async Task<PackageSpec> GetPackageSpecsAsync (DependencyGraphCacheContext cacheContext)
		{
			var specs = await project.GetPackageSpecsAsync (cacheContext);
			return specs.Single ();
		}

		void AddRestoreProjectStyle (string restoreStyle)
		{
			dotNetProject.ProjectProperties.SetValue ("RestoreProjectStyle", restoreStyle);
		}

		[Test]
		public async Task GetInstalledPackagesAsync_OnePackageReference_ReturnsOnePackageReference ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			var packageReferences = await project.GetInstalledPackagesAsync (CancellationToken.None);

			var packageReference = packageReferences.Single ();
			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
		}

		[Test]
		public async Task UninstallPackageAsync_OldPackageInstalled_PackageReferenceRemoved ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			bool result = await UninstallPackageAsync ("NUnit", "2.6.1");

			Assert.IsFalse (dotNetProject.Items.OfType<ProjectPackageReference> ().Any ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task UninstallPackageAsync_OldPackageIsNotInstalled_PackageReferenceRemoved ()
		{
			CreateNuGetProject ();
			dotNetProject.Name = "MyProject";

			bool result = await UninstallPackageAsync ("NUnit", "2.6.1");

			Assert.IsFalse (result);
			Assert.IsFalse (project.IsSaved);
			Assert.AreEqual (MessageLevel.Warning, context.LastLogLevel);
			Assert.AreEqual ("Package 'NUnit' does not exist in project 'MyProject'", context.LastMessageLogged);
		}

		[Test]
		public async Task UninstallPackageAsync_DifferentPackageVersionInstalled_PackageReferenceRemoved ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			bool result = await UninstallPackageAsync ("NUnit", "3.5");

			Assert.IsFalse (dotNetProject.Items.OfType<ProjectPackageReference> ().Any ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_PackageNotInstalled_PackageReferenceAdded ()
		{
			CreateNuGetProject ();

			bool result = await InstallPackageAsync ("NUnit", "2.6.1");

			var packageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ()
				.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_PackageAlreadyInstalled_PackageReferenceNotAdded ()
		{
			CreateNuGetProject ("MyProject");
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			bool result = await InstallPackageAsync ("NUnit", "2.6.1");

			var packageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ()
				.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsFalse (result);
			Assert.IsFalse (project.IsSaved);
			Assert.AreEqual ("Package 'NUnit.2.6.1' already exists in project 'MyProject'", context.LastMessageLogged);
			Assert.AreEqual (MessageLevel.Warning, context.LastLogLevel);
		}

		/// <summary>
		/// Original PackageReference in project should be updated with new version and not removed since this
		/// will keep any custom metadata added to the PackageReference.
		/// </summary>
		[Test]
		public async Task InstallPackageAsync_PackageAlreadyInstalledWithDifferentVersion_OldPackageReferenceIsRemoved ()
		{
			CreateNuGetProject ("MyProject");
			var nunitPackageReference = AddDotNetProjectPackageReference ("NUnit", "2.6.1");
			nunitPackageReference.Metadata.SetValue ("PrivateAssets", "All");

			bool result = await InstallPackageAsync ("NUnit", "3.6.0");

			var projectPackageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ();
			var packageReference = projectPackageReference.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("3.6.0", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
			Assert.IsFalse (packageReference.HasAllowedVersions);
			Assert.IsNull (packageReference.AllowedVersions);
			Assert.AreEqual ("All", projectPackageReference.Metadata.GetValue ("PrivateAssets"));
		}

		[Test]
		public async Task GetAssetsFilePathAsync_BaseIntermediatePathNotSet_BaseIntermediatePathUsedForProjectAssetsJsonFile ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			string expectedAssetsFilePath = @"d:\projects\MyProject\obj\project.assets.json".ToNativePath ();

			string assetsFilePath = await project.GetAssetsFilePathAsync ();

			Assert.AreEqual (expectedAssetsFilePath, assetsFilePath);
		}

		[Test]
		public async Task GetPackageSpecsAsync_NewProject_BaseIntermediatePathUsedForProjectAssetsJsonFile ()
		{
			string projectFile = Util.GetSampleProject ("NetStandardXamarinForms", "NetStandardXamarinForms", "NetStandardXamarinForms.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				dependencyGraphCacheContext = new DependencyGraphCacheContext ();
				var nugetProject = new DotNetCoreNuGetProject (project);
				var specs = await nugetProject.GetPackageSpecsAsync (dependencyGraphCacheContext);
				var spec = specs.Single ();

				Assert.AreEqual (projectFile, spec.FilePath);
				Assert.AreEqual ("NetStandardXamarinForms", spec.Name);
				Assert.AreEqual ("1.0.0", spec.Version.ToString ());
				Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
				Assert.AreEqual ("NetStandardXamarinForms", spec.RestoreMetadata.ProjectName);
				Assert.AreEqual (projectFile, spec.RestoreMetadata.ProjectPath);
				Assert.AreEqual (projectFile, spec.RestoreMetadata.ProjectUniqueName);
				Assert.AreSame (spec, dependencyGraphCacheContext.PackageSpecCache [projectFile]);
				Assert.AreEqual ("netstandard1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			}
		}

		[Test]
		public async Task GetPackageSpecsAsync_PackageSpecExistsInCache_CachedPackageSpecReturned ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			var cachedSpec = new PackageSpec ();
			dependencyGraphCacheContext = new DependencyGraphCacheContext ();
			dependencyGraphCacheContext.PackageSpecCache.Add (dotNetProject.FileName.ToString (), cachedSpec);

			PackageSpec spec = await GetPackageSpecsAsync (dependencyGraphCacheContext);

			Assert.AreSame (cachedSpec, spec);
		}

		[Test]
		public async Task GetInstalledPackagesAsync_FloatingVersion_ReturnsOnePackageReference ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0-*");

			var packageReferences = await project.GetInstalledPackagesAsync (CancellationToken.None);

			var packageReference = packageReferences.Single ();
			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.IsTrue (packageReference.IsFloating ());
			Assert.AreEqual ("2.6.0-*", packageReference.AllowedVersions.Float.ToString ());
		}

		[Test]
		public async Task GetCacheFilePathAsync_BaseIntermediatePathNotSet_BaseIntermediatePathUsedForCacheFilePath ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			string expectedCacheFilePath = @"d:\projects\MyProject\obj\MyProject.csproj.nuget.cache".ToNativePath ();

			string cacheFilePath = await project.GetCacheFilePathAsync ();

			Assert.AreEqual (expectedCacheFilePath, cacheFilePath);
		}

		[Test]
		public void Create_NoPackageReferences_ReturnsNull ()
		{
			CreateNuGetProject ();

			var nugetProject = PackageReferenceNuGetProject.Create (dotNetProject);

			Assert.IsNull (nugetProject);
		}

		[Test]
		public void Create_OnePackageReference_ReturnsNuGetProject ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			var nugetProject = PackageReferenceNuGetProject.Create (dotNetProject);

			Assert.IsNotNull (nugetProject);
		}

		/// <summary>
		/// If a project has the MSBuild property RestoreProjectStyle set to PackageReference
		/// then it should be restored in the same way as if it had PackageReferences
		/// even if the project does not have any.
		///
		/// https://www.hanselman.com/blog/ReferencingNETStandardAssembliesFromBothNETCoreAndNETFramework.aspx
		/// </summary>
		[TestCase ("PackageReference")]
		[TestCase ("packagereference")]
		public void Create_RestoreProjectStyle_ReturnsNuGetProject (string restoreStyle)
		{
			CreateNuGetProject ();
			AddRestoreProjectStyle (restoreStyle);

			var nugetProject = PackageReferenceNuGetProject.Create (dotNetProject);

			Assert.IsNotNull (nugetProject);
		}

		[Test]
		public void Create_RestoreProjectStyleIsNotPackageReference_ReturnsNull ()
		{
			CreateNuGetProject ();
			AddRestoreProjectStyle ("Unknown");

			var nugetProject = PackageReferenceNuGetProject.Create (dotNetProject);

			Assert.IsNull (nugetProject);
		}

		[Test]
		public async Task AddFile_NewFile_AddsFileToProject ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.cs".ToNativePath ();

			await project.AddFileToProjectAsync (fileName);

			ProjectFile fileItem = dotNetProject.Files.GetFile (fileName);
			var expectedFileName = new FilePath (fileName);
			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (Projects.BuildAction.Compile, fileItem.BuildAction);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task AddFile_NewTextFile_AddsFileToProjectWithCorrectItemType ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.txt".ToNativePath ();

			await project.AddFileToProjectAsync (fileName);

			ProjectFile fileItem = dotNetProject.Files.GetFile (fileName);
			var expectedFileName = new FilePath (fileName);
			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (Projects.BuildAction.None, fileItem.BuildAction);
		}

		[Test]
		public async Task AddFile_RelativeFileNameUsed_AddsFileToProject ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.cs".ToNativePath ();
			string relativeFileName = @"src\NewFile.cs".ToNativePath ();

			await project.AddFileToProjectAsync (relativeFileName);

			ProjectFile fileItem = dotNetProject.Files.GetFile (fileName);
			var expectedFileName = new FilePath (fileName);
			Assert.AreEqual (expectedFileName, fileItem.FilePath);
			Assert.AreEqual (Projects.BuildAction.Compile, fileItem.BuildAction);
		}

		[Test]
		public async Task AddFile_FileAlreadyExistsInProject_FileIsNotAddedToProject ()
		{
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			string fileName = @"d:\projects\MyProject\src\NewFile.cs".ToNativePath ();

			await project.AddFileToProjectAsync (fileName);
			await project.AddFileToProjectAsync (fileName);

			int projectItemsCount = dotNetProject.Files.Count;
			Assert.AreEqual (1, projectItemsCount);
		}

		[Test]
		public void OnAfterExecuteActions_PackageInstallAction_PackageInstalledEventFired ()
		{
			CreateNuGetProject ();
			var packageIdentity = new PackageIdentity ("Test", NuGetVersion.Parse ("1.2"));
			var actions = new List<NuGetProjectAction> ();
			var action = NuGetProjectAction.CreateInstallProjectAction (packageIdentity, null, project);
			actions.Add (action);
			PackageManagementEventArgs eventArgs = null;
			project.PackageManagementEvents.PackageInstalled += (sender, e) => {
				eventArgs = e;
			};

			project.OnAfterExecuteActions (actions);

			Assert.AreEqual ("Test", eventArgs.Id);
			Assert.AreEqual ("1.2", eventArgs.Version.ToString ());
			Assert.AreEqual (packageIdentity, eventArgs.Package);
		}

		[Test]
		public void OnAfterExecuteActions_PackageUninstallAction_PackageUninstalledEventFired ()
		{
			CreateNuGetProject ();
			var packageIdentity = new PackageIdentity ("Test", NuGetVersion.Parse ("1.2"));
			var actions = new List<NuGetProjectAction> ();
			var action = NuGetProjectAction.CreateUninstallProjectAction (packageIdentity, project);
			actions.Add (action);
			PackageManagementEventArgs eventArgs = null;
			project.PackageManagementEvents.PackageUninstalled += (sender, e) => {
				eventArgs = e;
			};

			project.OnAfterExecuteActions (actions);

			Assert.AreEqual ("Test", eventArgs.Id);
			Assert.AreEqual ("1.2", eventArgs.Version.ToString ());
			Assert.AreEqual (packageIdentity, eventArgs.Package);
		}
	}
}
