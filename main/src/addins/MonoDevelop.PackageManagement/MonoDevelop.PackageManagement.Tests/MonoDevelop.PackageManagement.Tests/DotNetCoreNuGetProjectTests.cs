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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class DotNetCoreNuGetProjectTests
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

		void AddDotNetProjectPackageReference (string packageId, string version)
		{
			var packageReference = ProjectPackageReference.Create (packageId, version);

			dotNetProject.Items.Add (packageReference);
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

		[Test]
		public async Task InstallPackageAsync_PackageAlreadyInstalledWithDifferentVersion_OldPackageReferenceIsRemoved ()
		{
			CreateNuGetProject ("MyProject");
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			bool result = await InstallPackageAsync ("NUnit", "3.6.0");

			var packageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ()
				.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("3.6.0", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
			Assert.IsFalse (packageReference.HasAllowedVersions);
			Assert.IsNull (packageReference.AllowedVersions);
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
			CreateNuGetProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");

			PackageSpec spec = await GetPackageSpecsAsync ();

			Assert.AreEqual (dotNetProject.FileName.ToString (), spec.FilePath);
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual ("1.0.0", spec.Version.ToString ());
			Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual (dotNetProject.FileName.ToString (), spec.RestoreMetadata.ProjectPath);
			Assert.AreEqual (dotNetProject.FileName.ToString (), spec.RestoreMetadata.ProjectUniqueName);
			Assert.AreEqual (dotNetProject.BaseIntermediateOutputPath.ToString (), spec.RestoreMetadata.OutputPath);
			Assert.AreSame (spec, dependencyGraphCacheContext.PackageSpecCache[dotNetProject.FileName.ToString ()]);

			// Cannot currently test this - needs the target framework to be available in the 
			// MSBuild.EvaluatedProperties
			//Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
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
		public async Task PostProcessAsync_ProjectAssetsFile_NotifyChangeInAssetsFile ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			dotNetProject.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			string fileNameChanged = null;
			PackageManagementServices.PackageManagementEvents.FileChanged += (sender, e) => {
				fileNameChanged = e.Single ().FileName;
			};

			await project.PostProcessAsync (context, CancellationToken.None);

			string expectedFileNameChanged = @"d:\projects\MyProject\obj\project.assets.json".ToNativePath ();
			Assert.AreEqual (expectedFileNameChanged, fileNameChanged);
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

		[Test]
		public async Task PostProcessAsync_RestoreRunLockFileNotChanged_NotifyChangeInAssetsFile ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.0");
			dotNetProject.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			string fileNameChanged = null;
			PackageManagementServices.PackageManagementEvents.FileChanged += (sender, e) => {
				fileNameChanged = e.Single ().FileName;
			};
			OnAfterExecuteActions ("NUnit", "2.6.3", NuGetProjectActionType.Install);

			await project.PostProcessAsync (context, CancellationToken.None);

			string expectedFileNameChanged = @"d:\projects\MyProject\obj\project.assets.json".ToNativePath ();
			Assert.AreEqual (expectedFileNameChanged, fileNameChanged);
			Assert.AreEqual (project, buildIntegratedRestorer.ProjectRestored);
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
	}
}
