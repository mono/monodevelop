//
// DotNetCoreNuGetProjectTests.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Frameworks;
using NuGet.LibraryModel;
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
	public class DotNetCoreNuGetProjectTests : TestBase
	{
		DotNetProject dotNetProject;
		TestableDotNetCoreNuGetProject project;
		FakeNuGetProjectContext context;
		DependencyGraphCacheContext dependencyGraphCacheContext;
		FakeMonoDevelopBuildIntegratedRestorer buildIntegratedRestorer;

		void CreateNuGetProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			context = new FakeNuGetProjectContext ();
			dotNetProject = CreateDotNetCoreProject (projectName, fileName);
			var solution = new Solution ();
			solution.RootFolder.AddItem (dotNetProject);
			project = new TestableDotNetCoreNuGetProject (dotNetProject);
			buildIntegratedRestorer = project.BuildIntegratedRestorer;
		}

		static DummyDotNetProject CreateDotNetCoreProject (string projectName = "MyProject", string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			var project = new DummyDotNetProject ();
			project.Name = projectName;
			project.FileName = fileName.ToNativePath ();
			return project;
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
			var installationContext = CreateInstallationContext ();
			return InstallPackageAsync (packageId, version, installationContext);
		}

		Task<bool> InstallPackageAsync (string packageId, string version, BuildIntegratedInstallationContext installationContext)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			var versionRange = new VersionRange (packageIdentity.Version);

			return project.InstallPackageAsync (packageId, versionRange, context, installationContext, CancellationToken.None);
		}

		BuildIntegratedInstallationContext CreateInstallationContext (
			LibraryIncludeFlags? includeType = null,
			LibraryIncludeFlags? suppressParent = null)
		{
			var framework = NuGetFramework.Parse (project.Project.TargetFrameworkMoniker.ToString ());
			var frameworks = new NuGetFramework [] {
				framework
			};

			var originalFrameworks = new Dictionary<NuGetFramework, string> ();
			originalFrameworks [framework] = framework.GetShortFolderName ();

			var installationContext = new BuildIntegratedInstallationContext (
				frameworks,
				Enumerable.Empty<NuGetFramework> (),
				originalFrameworks);

			if (includeType.HasValue)
				installationContext.IncludeType = includeType.Value;

			if (suppressParent.HasValue)
				installationContext.SuppressParent = suppressParent.Value;

			return installationContext;
		}

		Task<bool> InstallPackageAsync (string packageId, string version, LibraryIncludeFlags includeType, LibraryIncludeFlags suppressParent)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			var versionRange = new VersionRange (packageIdentity.Version);

			var installationContext = CreateInstallationContext (includeType, suppressParent);
			return project.InstallPackageAsync (packageId, versionRange, context, installationContext, CancellationToken.None);
		}

		async Task<PackageSpec> GetPackageSpecsAsync (DependencyGraphCacheContext cacheContext)
		{
			var specs = await project.GetPackageSpecsAsync (cacheContext);
			return specs.Single ();
		}

		void OnAfterExecuteActions (string packageId, string version, NuGetProjectActionType actionType)
		{
			var action = new FakeNuGetProjectAction (packageId, version, actionType);
			project.OnAfterExecuteActions (new [] { action });
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

			var projectPackageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ();

			var packageReference = projectPackageReference.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
			Assert.IsFalse (projectPackageReference.Metadata.HasProperty ("IncludeAssets"));
			Assert.IsFalse (projectPackageReference.Metadata.HasProperty ("PrivateAssets"));
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
		public async Task InstallPackageAsync_PackageAlreadyInstalledWithDifferentVersion_OldPackageReferenceIsUpdated ()
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
		public async Task InstallPackageAsync_DevelopmentDependency_AssetInfoAddedToPackageReference ()
		{
			CreateNuGetProject ();

			var includeType = LibraryIncludeFlags.Runtime |
				LibraryIncludeFlags.Build |
				LibraryIncludeFlags.Native |
				LibraryIncludeFlags.ContentFiles |
				LibraryIncludeFlags.Analyzers;

			var suppressParent = LibraryIncludeFlags.All;

			bool result = await InstallPackageAsync ("GitInfo", "2.0.20", includeType, suppressParent);

			var projectPackageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ();

			var packageReference = projectPackageReference.CreatePackageReference ();

			Assert.AreEqual ("GitInfo", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.0.20", packageReference.PackageIdentity.Version.ToNormalizedString ());

			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
			Assert.AreEqual ("runtime; build; native; contentfiles; analyzers", projectPackageReference.Metadata.GetValue ("IncludeAssets"));
			Assert.AreEqual ("all", projectPackageReference.Metadata.GetValue ("PrivateAssets"));
			Assert.AreEqual (ProjectStyle.PackageReference, project.ProjectStyle);
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
				Assert.AreSame (spec, dependencyGraphCacheContext.PackageSpecCache[projectFile]);
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
		public async Task GetPackageSpecsAsync_DotNetCliTool_PackageSpecsIncludeDotNetCliToolReference ()
		{
			string projectFile = Util.GetSampleProject ("dotnet-cli-tool", "dotnet-cli-tool-netcore.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile)) {

				dependencyGraphCacheContext = new DependencyGraphCacheContext ();
				var nugetProject = new DotNetCoreNuGetProject (project);
				var specs = await nugetProject.GetPackageSpecsAsync (dependencyGraphCacheContext);
				var projectSpec = specs.FirstOrDefault (s => s.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference);
				var dotNetCliToolSpec = specs.FirstOrDefault (s => s.RestoreMetadata.ProjectStyle == ProjectStyle.DotnetCliTool);

				Assert.AreEqual (2, specs.Count);
				Assert.AreEqual (projectFile, projectSpec.FilePath);
				Assert.AreEqual ("dotnet-cli-tool-netcore", projectSpec.Name);
				Assert.AreEqual ("dotnet-cli-tool-netcore", projectSpec.RestoreMetadata.ProjectName);
				Assert.AreEqual (projectFile, projectSpec.RestoreMetadata.ProjectPath);
				Assert.AreEqual (projectFile, projectSpec.RestoreMetadata.ProjectUniqueName);
				Assert.AreSame (projectSpec, dependencyGraphCacheContext.PackageSpecCache [projectFile]);
				Assert.AreEqual ("bundlerminifier.core-netcoreapp2.1-[2.9.406, )", dotNetCliToolSpec.Name);
				Assert.AreEqual (projectFile, dotNetCliToolSpec.RestoreMetadata.ProjectPath);
				Assert.AreSame (dotNetCliToolSpec, dependencyGraphCacheContext.PackageSpecCache [dotNetCliToolSpec.Name]);
			}
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
		public async Task PostProcessAsync_References_NotifyReferencesChangedEventFired ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.Single ().Hint;
			};

			await project.PostProcessAsync (context, CancellationToken.None);

			Assert.AreEqual ("References", modifiedHint);
		}

		[Test]
		public async Task PostProcessAsync_RestoreRunLockFileNotChanged_NotifyReferencesChangedEventFired ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.Single ().Hint;
			};
			OnAfterExecuteActions ("NUnit", "2.6.3", NuGetProjectActionType.Install);

			await project.PostProcessAsync (context, CancellationToken.None);

			Assert.IsNotNull (dotNetProject.ParentSolution);
			Assert.AreEqual (dotNetProject.ParentSolution, project.SolutionUsedToCreateBuildIntegratedRestorer);
			Assert.AreEqual (project, buildIntegratedRestorer.ProjectRestored);
			Assert.AreEqual ("References", modifiedHint);
		}

		/// <summary>
		/// Build restorer would trigger the notification itself.
		/// </summary>
		[Test]
		public async Task PostProcessAsync_RestoreRunLockFileChanged_NotifyReferencesChangedEventNotFired ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.Single ().Hint;
			};
			buildIntegratedRestorer.LockFileChanged = true;
			OnAfterExecuteActions ("NUnit", "2.6.3", NuGetProjectActionType.Install);

			await project.PostProcessAsync (context, CancellationToken.None);

			Assert.AreEqual (project, buildIntegratedRestorer.ProjectRestored);
			Assert.IsNull (modifiedHint);
		}

		/// <summary>
		/// Build restorer would trigger the notification itself.
		/// </summary>
		[Test]
		public async Task PostProcessAsync_RestoreRunLockFileChanged_NotifyChangeInAssetsFileIsNotMade ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			dotNetProject.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			string fileNameChanged = null;
			PackageManagementServices.PackageManagementEvents.FileChanged += (sender, e) => {
				fileNameChanged = e.Single ().FileName;
			};
			buildIntegratedRestorer.LockFileChanged = true;
			OnAfterExecuteActions ("NUnit", "2.6.3", NuGetProjectActionType.Install);

			await project.PostProcessAsync (context, CancellationToken.None);

			Assert.IsNull (fileNameChanged);
			Assert.AreEqual (project, buildIntegratedRestorer.ProjectRestored);
		}

		[Test]
		public void NotifyProjectReferencesChanged_References_NotifyReferencesChangedEventFired ()
		{
			CreateNuGetProject ();
			string modifiedHint = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHint = e.Single ().Hint;
			};

			project.NotifyProjectReferencesChanged (false);

			Assert.AreEqual ("References", modifiedHint);
		}

		/// <summary>
		/// Assembly references are transitive for .NET Core projects so any .NET Core project
		/// that references the project having a NuGet package being installed needs to refresh
		/// references for the other projects not just itself.
		/// </summary>
		[Test]
		public async Task PostProcessAsync_DotNetCoreProjectReferencesThisProject_NotifyReferencesChangedEventFiredForBothProjects ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			var dotNetProjectWithProjectReference = CreateDotNetCoreProject ("MyProject2", @"d:\projects\MyProject2\MyProject2.csproj");
			dotNetProject.ParentSolution.RootFolder.AddItem (dotNetProjectWithProjectReference);
			var projectReference = ProjectReference.CreateProjectReference (dotNetProject);
			dotNetProjectWithProjectReference.References.Add (projectReference);
			string modifiedHintMainProject = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHintMainProject = e.Single ().Hint;
			};
			string modifiedHintProjectWithReference = null;
			dotNetProjectWithProjectReference.Modified += (sender, e) => {
				modifiedHintProjectWithReference = e.Single ().Hint;
			};

			await project.PostProcessAsync (context, CancellationToken.None);

			Assert.AreEqual ("References", modifiedHintMainProject);
			Assert.AreEqual ("References", modifiedHintProjectWithReference);
		}

		[Test]
		public async Task PostProcessAsync_DotNetCoreProjectReferencesThisProjectLockFileNotChanged_NotifyReferencesChangedEventFiredForBothProjects ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			var dotNetProjectWithProjectReference = CreateDotNetCoreProject ("MyProject2", @"d:\projects\MyProject2\MyProject2.csproj");
			dotNetProject.ParentSolution.RootFolder.AddItem (dotNetProjectWithProjectReference);
			var projectReference = ProjectReference.CreateProjectReference (dotNetProject);
			dotNetProjectWithProjectReference.References.Add (projectReference);
			string modifiedHintMainProject = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHintMainProject = e.Single ().Hint;
			};
			string modifiedHintProjectWithReference = null;
			dotNetProjectWithProjectReference.Modified += (sender, e) => {
				modifiedHintProjectWithReference = e.Single ().Hint;
			};
			OnAfterExecuteActions ("NUnit", "2.6.3", NuGetProjectActionType.Install);

			await project.PostProcessAsync (context, CancellationToken.None);

			Assert.AreEqual (project, buildIntegratedRestorer.ProjectRestored);
			Assert.AreEqual ("References", modifiedHintMainProject);
			Assert.AreEqual ("References", modifiedHintProjectWithReference);
		}

		[Test]
		public void NotifyProjectReferencesChanged_IncludeTransitiveReferences_NotifyReferencesChangedEventFiredForAllProjects ()
		{
			CreateNuGetProject ();
			var dotNetProjectWithProjectReference = CreateDotNetCoreProject ("MyProject2", @"d:\projects\MyProject2\MyProject2.csproj");
			dotNetProject.ParentSolution.RootFolder.AddItem (dotNetProjectWithProjectReference);
			var projectReference = ProjectReference.CreateProjectReference (dotNetProject);
			dotNetProjectWithProjectReference.References.Add (projectReference);
			string modifiedHintMainProject = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHintMainProject = e.Single ().Hint;
			};
			string modifiedHintProjectWithReference = null;
			dotNetProjectWithProjectReference.Modified += (sender, e) => {
				modifiedHintProjectWithReference = e.Single ().Hint;
			};

			project.NotifyProjectReferencesChanged (true);

			Assert.AreEqual ("References", modifiedHintMainProject);
			Assert.AreEqual ("References", modifiedHintProjectWithReference);
		}

		[Test]
		public void NotifyProjectReferencesChanged_DoNotIncludeTransitiveReferences_NotifyReferencesChangedEventFiredForMainProjectOnly ()
		{
			CreateNuGetProject ();
			var dotNetProjectWithProjectReference = CreateDotNetCoreProject ("MyProject2", @"d:\projects\MyProject2\MyProject2.csproj");
			dotNetProject.ParentSolution.RootFolder.AddItem (dotNetProjectWithProjectReference);
			var projectReference = ProjectReference.CreateProjectReference (dotNetProject);
			dotNetProjectWithProjectReference.References.Add (projectReference);
			string modifiedHintMainProject = null;
			dotNetProject.Modified += (sender, e) => {
				modifiedHintMainProject = e.Single ().Hint;
			};
			string modifiedHintProjectWithReference = null;
			dotNetProjectWithProjectReference.Modified += (sender, e) => {
				modifiedHintProjectWithReference = e.Single ().Hint;
			};

			project.NotifyProjectReferencesChanged (false);

			Assert.AreEqual ("References", modifiedHintMainProject);
			Assert.IsNull (modifiedHintProjectWithReference);
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
		public void CanCreate_MSBuildProjectIsNull_DoesNotThrowNullReferenceException ()
		{
			var dotNetCoreProject = CreateDotNetCoreProject ();
			dotNetCoreProject.Dispose ();

			bool result = false;
			Assert.DoesNotThrow (() => {
				result = DotNetCoreNuGetProject.CanCreate (dotNetCoreProject);
			});

			Assert.IsNull (dotNetCoreProject.MSBuildProject);
			Assert.IsFalse (result);
		}

		[Test]
		public async Task InstallPackageAsync_MultiTargetPartialInstall_PackageReferenceAddedWithCondition ()
		{
			CreateNuGetProject ();
			FilePath projectDirectory = Util.CreateTmpDir ("MultiTargetInstallTest");
			dotNetProject.FileName = projectDirectory.Combine ("MultiTargetTest.csproj");
			project.CallBaseSaveProject = true;

			var successfulFrameworks = new NuGetFramework[] {
				NuGetFramework.Parse ("net472"),
			};

			var unsuccessfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("netstandard1.5")
			};

			var originalFrameworks = new Dictionary<NuGetFramework, string> ();

			var installationContext = new BuildIntegratedInstallationContext (
				successfulFrameworks,
				unsuccessfulFrameworks,
				originalFrameworks);

			bool result = await InstallPackageAsync ("TestPackage", "2.6.1", installationContext);

			var packageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.SingleOrDefault (item => item.Include == "TestPackage");

			Assert.AreEqual ("'$(TargetFramework)' == 'net472'", packageReference.ParentGroup.Condition);
			Assert.AreEqual ("2.6.1", packageReference.Metadata.GetValue ("Version"));
			Assert.IsFalse (packageReference.Metadata.HasProperty ("IncludeAssets"));
			Assert.IsFalse (packageReference.Metadata.HasProperty ("PrivateAssets"));
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_MultiTargetPartialInstallNonStandardOriginalFramework_OriginalFrameworkUsedInCondition ()
		{
			CreateNuGetProject ();
			FilePath projectDirectory = Util.CreateTmpDir ("MultiTargetInstallTest");
			dotNetProject.FileName = projectDirectory.Combine ("MultiTargetTest.csproj");
			project.CallBaseSaveProject = true;

			var net472Framework = NuGetFramework.Parse ("net472");

			var successfulFrameworks = new NuGetFramework [] {
				net472Framework
			};

			var unsuccessfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("netstandard1.5")
			};

			var originalFrameworks = new Dictionary<NuGetFramework, string> ();
			originalFrameworks [net472Framework] = ".NETFramework4.7.2";

			var installationContext = new BuildIntegratedInstallationContext (
				successfulFrameworks,
				unsuccessfulFrameworks,
				originalFrameworks);

			bool result = await InstallPackageAsync ("TestPackage", "2.6.1", installationContext);

			var packageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.SingleOrDefault (item => item.Include == "TestPackage");

			Assert.AreEqual ("'$(TargetFramework)' == '.NETFramework4.7.2'", packageReference.ParentGroup.Condition);
			Assert.AreEqual (packageReference.Metadata.GetValue ("Version"), "2.6.1");
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_MultiTargetPartialInstallPrivateAndIncludeAssets_PackageReferenceHasAssetInfo ()
		{
			CreateNuGetProject ();
			FilePath projectDirectory = Util.CreateTmpDir ("MultiTargetInstallTest");
			dotNetProject.FileName = projectDirectory.Combine ("MultiTargetTest.csproj");
			project.CallBaseSaveProject = true;

			var successfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("net472"),
			};

			var unsuccessfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("netstandard1.5")
			};

			var originalFrameworks = new Dictionary<NuGetFramework, string> ();

			var installationContext = new BuildIntegratedInstallationContext (
				successfulFrameworks,
				unsuccessfulFrameworks,
				originalFrameworks);

			installationContext.IncludeType = LibraryIncludeFlags.Runtime |
				LibraryIncludeFlags.Build |
				LibraryIncludeFlags.Native |
				LibraryIncludeFlags.ContentFiles |
				LibraryIncludeFlags.Analyzers;

			installationContext.SuppressParent = LibraryIncludeFlags.All;

			bool result = await InstallPackageAsync ("TestPackage", "2.6.1", installationContext);

			var packageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.SingleOrDefault (item => item.Include == "TestPackage");

			Assert.AreEqual ("'$(TargetFramework)' == 'net472'", packageReference.ParentGroup.Condition);
			Assert.AreEqual ("2.6.1", packageReference.Metadata.GetValue ("Version"));
			Assert.AreEqual ("runtime; build; native; contentfiles; analyzers", packageReference.Metadata.GetValue ("IncludeAssets"));
			Assert.AreEqual ("all", packageReference.Metadata.GetValue ("PrivateAssets"));
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_MultiTargetPartialInstallTwice_PackageReferenceAddedToSameItemGroup ()
		{
			CreateNuGetProject ();
			FilePath projectDirectory = Util.CreateTmpDir ("MultiTargetInstallTest");
			dotNetProject.FileName = projectDirectory.Combine ("MultiTargetTest.csproj");
			project.CallBaseSaveProject = true;

			var successfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("net472"),
			};

			var unsuccessfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("netstandard1.5")
			};

			var originalFrameworks = new Dictionary<NuGetFramework, string> ();

			var installationContext = new BuildIntegratedInstallationContext (
				successfulFrameworks,
				unsuccessfulFrameworks,
				originalFrameworks);

			bool firstInstallResult = await InstallPackageAsync ("TestPackage", "2.6.1", installationContext);
			var packageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.SingleOrDefault (item => item.Include == "TestPackage");

			// Update.
			bool result = await InstallPackageAsync ("AnotherTestPackage", "3.0.1", installationContext);

			var secondPackageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.FirstOrDefault (item => item.Include == "AnotherTestPackage");

			Assert.AreEqual ("'$(TargetFramework)' == 'net472'", packageReference.ParentGroup.Condition);
			Assert.AreEqual ("2.6.1", packageReference.Metadata.GetValue ("Version"));
			Assert.IsTrue (firstInstallResult);
			Assert.AreEqual ("'$(TargetFramework)' == 'net472'", secondPackageReference.ParentGroup.Condition);
			Assert.AreEqual ("3.0.1", secondPackageReference.Metadata.GetValue ("Version"));
			Assert.AreEqual (packageReference.ParentGroup, secondPackageReference.ParentGroup);
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_MultiTargetPartialUpdate_PackageReferenceUpdated ()
		{
			CreateNuGetProject ();
			FilePath projectDirectory = Util.CreateTmpDir ("MultiTargetInstallTest");
			dotNetProject.FileName = projectDirectory.Combine ("MultiTargetTest.csproj");
			project.CallBaseSaveProject = true;

			var successfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("net472"),
			};

			var unsuccessfulFrameworks = new NuGetFramework [] {
				NuGetFramework.Parse ("netstandard1.5")
			};

			var originalFrameworks = new Dictionary<NuGetFramework, string> ();

			var installationContext = new BuildIntegratedInstallationContext (
				successfulFrameworks,
				unsuccessfulFrameworks,
				originalFrameworks);

			bool firstInstallResult = await InstallPackageAsync ("TestPackage", "2.6.1", installationContext);

			var packageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.SingleOrDefault (item => item.Include == "TestPackage");
			Assert.AreEqual ("2.6.1", packageReference.Metadata.GetValue ("Version"));

			bool result = await InstallPackageAsync ("TestPackage", "3.0.1", installationContext);

			var updatedPackageReference = dotNetProject
				.MSBuildProject
				.GetAllItems ()
				.FirstOrDefault (item => item.Include == "TestPackage");

			Assert.AreEqual ("'$(TargetFramework)' == 'net472'", packageReference.ParentGroup.Condition);
			Assert.AreEqual ("3.0.1", packageReference.Metadata.GetValue ("Version"));
			Assert.IsTrue (firstInstallResult);
			Assert.AreEqual ("'$(TargetFramework)' == 'net472'", updatedPackageReference.ParentGroup.Condition);
			Assert.AreEqual ("3.0.1", updatedPackageReference.Metadata.GetValue ("Version"));
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}
	}
}
