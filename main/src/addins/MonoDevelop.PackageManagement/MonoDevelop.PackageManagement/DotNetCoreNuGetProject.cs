//
// DotNetCoreNuGetProject.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Commands;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	class DotNetCoreNuGetProject : BuildIntegratedNuGetProject, IBuildIntegratedNuGetProject, IHasDotNetProject
	{
		DotNetProject project;
		ConfigurationSelector configuration;
		IPackageManagementEvents packageManagementEvents;
		string msbuildProjectPath;
		string projectName;
		bool restoreRequired;

		public DotNetCoreNuGetProject (DotNetProject project)
			: this (project, ConfigurationSelector.Default)
		{
		}

		public DotNetCoreNuGetProject (DotNetProject project, ConfigurationSelector configuration)
			: this (project, project.GetDotNetCoreTargetFrameworks (), configuration)
		{
		}

		public DotNetCoreNuGetProject (
			DotNetProject project,
			IEnumerable<string> targetFrameworks,
			ConfigurationSelector configuration)
			: this (project, targetFrameworks, configuration, PackageManagementServices.PackageManagementEvents)
		{
		}

		public DotNetCoreNuGetProject (
			DotNetProject project,
			IEnumerable<string> targetFrameworks,
			ConfigurationSelector configuration,
			IPackageManagementEvents packageManagementEvents)
		{
			this.project = project;
			this.configuration = configuration;
			this.packageManagementEvents = packageManagementEvents;

			var targetFramework = NuGetFramework.UnsupportedFramework;

			if (targetFrameworks.Count () == 1) {
				targetFramework = NuGetFramework.Parse (targetFrameworks.First ());
			}

			InternalMetadata.Add (NuGetProjectMetadataKeys.TargetFramework, targetFramework);
			InternalMetadata.Add (NuGetProjectMetadataKeys.Name, project.Name);
			InternalMetadata.Add (NuGetProjectMetadataKeys.FullPath, project.BaseDirectory);

			msbuildProjectPath = project.FileName;
			projectName = project.Name;
		}

		internal DotNetProject DotNetProject {
			get { return project; }
		}

		public override string ProjectName {
			get { return projectName; }
		}

		public override string MSBuildProjectPath {
			get { return msbuildProjectPath; }
		}

		public static bool CanCreate (DotNetProject project)
		{
			var msbuildProject = project.MSBuildProject;
			if (msbuildProject == null)
				return false;

			return msbuildProject.GetReferencedSDKs ().Any ();
		}

		public static NuGetProject Create (DotNetProject project)
		{
			return Create (project, ConfigurationSelector.Default);
		}

		public static NuGetProject Create (DotNetProject project, ConfigurationSelector configuration)
		{
			if (CanCreate (project))
				return new DotNetCoreNuGetProject (project, configuration);

			return null;
		}

		public IDotNetProject Project {
			get { return new DotNetProjectProxy (project); }
		}

		public virtual Task SaveProject ()
		{
			return project.SaveAsync (new ProgressMonitor ());
		}

		public override Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync (CancellationToken token)
		{
			return Task.FromResult (GetPackageReferences ());
		}

		IEnumerable<PackageReference> GetPackageReferences ()
		{
			return project.Items.OfType<ProjectPackageReference> ()
				.Select (projectItem => projectItem.CreatePackageReference ())
				.ToList ();
		}

		public async override Task<bool> InstallPackageAsync (
			string packageId,
			VersionRange range,
			INuGetProjectContext nuGetProjectContext,
			BuildIntegratedInstallationContext installationContext,
			CancellationToken token)
		{
			var packageIdentity = new PackageIdentity (packageId, range.MinVersion);

			bool added = await Runtime.RunInMainThread (() => {
				return AddPackageReference (packageIdentity, nuGetProjectContext);
			});

			if (added) {
				await SaveProject ();
			}

			return added;
		}

		bool AddPackageReference (PackageIdentity packageIdentity, INuGetProjectContext context)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity, matchVersion: false);
			if (packageReference?.Equals (packageIdentity, matchVersion: true) == true) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' already exists in project '{1}'", packageIdentity, project.Name));
				return false;
			}

			if (packageReference != null) {
				packageReference.Version = packageIdentity.Version.ToNormalizedString ();
			} else {
				packageReference = ProjectPackageReference.Create (packageIdentity);
				project.Items.Add (packageReference);
			}

			return true;
		}

		public override async Task<bool> UninstallPackageAsync (
			PackageIdentity packageIdentity,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			bool removed = await Runtime.RunInMainThread (() => {
				return RemovePackageReference (packageIdentity, nuGetProjectContext);
			});

			if (removed) {
				await SaveProject ();
			}

			return removed;
		}

		bool RemovePackageReference (PackageIdentity packageIdentity, INuGetProjectContext context)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity, matchVersion: false);

			if (packageReference == null) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' does not exist in project '{1}'", packageIdentity.Id, project.Name));
				return false;
			}

			project.Items.Remove (packageReference);

			return true;
		}

		public override Task<string> GetAssetsFilePathAsync ()
		{
			string assetsFilePath = project.GetNuGetAssetsFilePath ();
			return Task.FromResult (assetsFilePath);
		}

		public override Task<string> GetAssetsFilePathOrNullAsync ()
		{
			return GetAssetsFilePathAsync ();
		}

		public override Task<string> GetCacheFilePathAsync ()
		{
			string cacheFilePath = NoOpRestoreUtilities.GetProjectCacheFilePath (
				project.BaseIntermediateOutputPath,
				msbuildProjectPath);
			return Task.FromResult (cacheFilePath);
		}

		public override async Task<IReadOnlyList<PackageSpec>> GetPackageSpecsAsync (DependencyGraphCacheContext context)
		{
			PackageSpec existingPackageSpec = context.GetExistingProjectPackageSpec (MSBuildProjectPath);
			if (existingPackageSpec != null) {
				return new [] { existingPackageSpec };
			}

			PackageSpec packageSpec = await CreateProjectPackageSpec (context);
			return new [] { packageSpec };
		}

		async Task<PackageSpec> CreateProjectPackageSpec (DependencyGraphCacheContext context)
		{
			DependencyGraphSpec dependencySpec = await MSBuildPackageSpecCreator.GetDependencyGraphSpec (project, configuration, context?.Logger);

			context.AddToCache (dependencySpec);

			PackageSpec spec = dependencySpec.GetProjectSpec (project.FileName);
			if (spec != null)
				return spec;

			throw new InvalidOperationException (GettextCatalog.GetString ("Unable to create package spec for project. '{0}'", project.FileName));
		}

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			if (restoreRequired) {
				return RestorePackages (nuGetProjectContext, token);
			}

			Runtime.RunInMainThread (() => {
				DotNetProject.DotNetCoreNotifyReferencesChanged ();
			});

			return base.PostProcessAsync (nuGetProjectContext, token);
		}

		async Task RestorePackages (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			var packageRestorer = await Runtime.RunInMainThread (() => {
				return CreateBuildIntegratedRestorer (project.ParentSolution);
			});

			var restoreTask = packageRestorer.RestorePackages (this, token);
			using (var task = new PackageRestoreTask (restoreTask)) {
				await restoreTask;
			}

			// Ensure MSBuild tasks are up to date when the next build is run.
			project.ShutdownProjectBuilder ();

			if (!packageRestorer.LockFileChanged) {
				// Need to refresh the references since the restore did not.
				await Runtime.RunInMainThread (() => {
					DotNetProject.DotNetCoreNotifyReferencesChanged ();
				});
			}

			await base.PostProcessAsync (nuGetProjectContext, token);
		}

		protected virtual IMonoDevelopBuildIntegratedRestorer CreateBuildIntegratedRestorer (Solution solution)
		{
			var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
			return new MonoDevelopBuildIntegratedRestorer (solutionManager);
		}

		public void OnBeforeUninstall (IEnumerable<NuGetProjectAction> actions)
		{
		}

		public void OnAfterExecuteActions (IEnumerable<NuGetProjectAction> actions)
		{
			restoreRequired = actions.Any (action => action.NuGetProjectActionType == NuGetProjectActionType.Install);
		}

		public void NotifyProjectReferencesChanged (bool includeTransitiveProjectReferences)
		{
			Runtime.AssertMainThread ();

			DotNetProject.RefreshProjectBuilder ();

			if (includeTransitiveProjectReferences)
				DotNetProject.DotNetCoreNotifyReferencesChanged ();
			else
				DotNetProject.NotifyModified ("References");
		}

		/// <summary>
		/// Always returns true so the project is re-evaluated after a restore.
		/// This ensures any imports in the generated .nuget.g.targets are
		/// re-evaluated. Without this custom MSBuild targets used by a NuGet package
		/// that was restored into the local NuGet package cache would not be available
		/// until the solution is closed and re-opened. Also handles the project file
		/// being edited by hand and a new package reference being added that has a
		/// custom MSBuild target.
		/// </summary>
		public bool ProjectRequiresReloadAfterRestore ()
		{
			return true;
		}

		public Task AddFileToProjectAsync (string filePath)
		{
			// NuGet does nothing here for .NET Core sdk projects so we also do nothing.
			// This method is called when adding the packages lock file to the project.
			return Task.CompletedTask;
		}
	}
}
