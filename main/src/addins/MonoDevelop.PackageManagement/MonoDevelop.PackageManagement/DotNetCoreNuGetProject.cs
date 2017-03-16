﻿//
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
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
		IPackageManagementEvents packageManagementEvents;
		string msbuildProjectPath;
		string projectName;
		bool restoreRequired;

		public DotNetCoreNuGetProject (
			DotNetProject project,
			IEnumerable<string> targetFrameworks)
			: this (project, targetFrameworks, PackageManagementServices.PackageManagementEvents)
		{
		}

		public DotNetCoreNuGetProject (
			DotNetProject project,
			IEnumerable<string> targetFrameworks,
			IPackageManagementEvents packageManagementEvents)
		{
			this.project = project;
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

		public static NuGetProject Create (DotNetProject project)
		{
			var targetFrameworks = project.GetDotNetCoreTargetFrameworks ();
			if (targetFrameworks.Any ())
				return new DotNetCoreNuGetProject (project, targetFrameworks);

			return null;
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
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity);
			if (packageReference != null) {
				context.Log (MessageLevel.Warning, GettextCatalog.GetString ("Package '{0}' already exists in project '{1}'", packageIdentity, project.Name));
				return false;
			}

			RemoveExistingPackageReference (packageIdentity);

			packageReference = ProjectPackageReference.Create (packageIdentity);
			project.Items.Add (packageReference);

			return true;
		}

		void RemoveExistingPackageReference (PackageIdentity packageIdentity)
		{
			ProjectPackageReference packageReference = project.GetPackageReference (packageIdentity, matchVersion: false);
			if (packageReference != null) {
				project.Items.Remove (packageReference);
			}
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

		public override async Task<IReadOnlyList<PackageSpec>> GetPackageSpecsAsync (DependencyGraphCacheContext context)
		{
			PackageSpec existingPackageSpec = GetExistingProjectPackageSpec (context);
			if (existingPackageSpec != null) {
				return new [] { existingPackageSpec };
			}

			PackageSpec packageSpec = await CreateProjectPackageSpec ();

			if (context != null) {
				AddToCache (context, packageSpec);
			}

			return new [] { packageSpec };
		}

		PackageSpec GetExistingProjectPackageSpec (DependencyGraphCacheContext context)
		{
			PackageSpec packageSpec = null;
			if (context != null) {
				if (context.PackageSpecCache.TryGetValue (MSBuildProjectPath, out packageSpec)) {
					return packageSpec;
				}
			}
			return packageSpec;
		}

		async Task<PackageSpec> CreateProjectPackageSpec ()
		{
			PackageSpec packageSpec = await Runtime.RunInMainThread (() => CreateProjectPackageSpec (project));
			return packageSpec;
		}

		static PackageSpec CreateProjectPackageSpec (DotNetProject project)
		{
			PackageSpec packageSpec = PackageSpecCreator.CreatePackageSpec (project);
			return packageSpec;
		}

		void AddToCache (DependencyGraphCacheContext context, PackageSpec projectPackageSpec)
		{
			if (IsMissingFromCache (context, projectPackageSpec)) {
				context.PackageSpecCache.Add (
					projectPackageSpec.RestoreMetadata.ProjectUniqueName, 
					projectPackageSpec);
			}
		}

		bool IsMissingFromCache (
			DependencyGraphCacheContext context,
			PackageSpec packageSpec)
		{
			PackageSpec ignore;
			return !context.PackageSpecCache.TryGetValue (
				packageSpec.RestoreMetadata.ProjectUniqueName,
				out ignore);
		}

		public override Task<bool> ExecuteInitScriptAsync (
			PackageIdentity identity,
			string packageInstallPath,
			INuGetProjectContext projectContext,
			bool throwOnFailure)
		{
			// Not supported. This gets called for every NuGet package
			// even if they do not have an init.ps1 so do not report this.
			return Task.FromResult (false);
		}

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			if (restoreRequired) {
				return RestorePackages (nuGetProjectContext, token);
			}

			Runtime.RunInMainThread (() => {
				DotNetProject.NotifyModified ("References");
			});

			packageManagementEvents.OnFileChanged (project.GetNuGetAssetsFilePath ());

			return base.PostProcessAsync (nuGetProjectContext, token);
		}

		async Task RestorePackages (INuGetProjectContext nuGetProjectContext, CancellationToken token)
		{
			var packageRestorer = await Runtime.RunInMainThread (() => {
				var solutionManager = PackageManagementServices.Workspace.GetSolutionManager (project.ParentSolution);
				return new MonoDevelopBuildIntegratedRestorer (solutionManager);
			});

			var restoreTask = packageRestorer.RestorePackages (this, token);
			using (var task = new PackageRestoreTask (restoreTask)) {
				await restoreTask;
			}

			if (!packageRestorer.LockFileChanged) {
				// Need to refresh the references since the restore did not.
				await Runtime.RunInMainThread (() => {
					DotNetProject.NotifyModified ("References");
					packageManagementEvents.OnFileChanged (project.GetNuGetAssetsFilePath ());
				});
			}

			await base.PostProcessAsync (nuGetProjectContext, token);
		}

		public void OnBeforeUninstall (IEnumerable<NuGetProjectAction> actions)
		{
		}

		public void OnAfterExecuteActions (IEnumerable<NuGetProjectAction> actions)
		{
			restoreRequired = actions.Any (action => action.NuGetProjectActionType == NuGetProjectActionType.Install);
		}

		public void NotifyProjectReferencesChanged ()
		{
			Runtime.AssertMainThread ();

			DotNetProject.RefreshProjectBuilder ();
			DotNetProject.NotifyModified ("References");
		}

		public bool ProjectRequiresReloadAfterRestore ()
		{
			// Disabled. Newer Sdk style projects no longer need to be
			// re-evaluated since the sdk imports are not downloaded
			// from NuGet. Possibly a check should be made for the
			// generated nuget.g.targets and nuget.g.props files. These
			// are still generated by the latest .NET Core SDK on restore
			// but they do not contain any Sdk imports. Disabling this
			// also reduces the chance of the project model and project
			// builder reading/writing the MSBuildProject at the same time
			// causing inconsistencies.
			return false;

			//if (project.DotNetCoreNuGetMSBuildFilesExist ())
			//	return false;

			//return true;
		}
	}
}
