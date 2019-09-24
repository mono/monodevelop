//
// MonoDevelopNuGetPackageManager.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// OpenReadmeFiles based on NuGet.Clients
// src/NuGet.Core/NuGet.PackageManagement/NuGetPackageManager.cs
//
// Copyright (c) 2016 Xamarin Inc.
// Copyright (c) .NET Foundation. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.Common;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopNuGetPackageManager : INuGetPackageManager
	{
		readonly IMonoDevelopSolutionManager solutionManager;
		readonly NuGetPackageManager packageManager;
		readonly ISettings settings;

		public MonoDevelopNuGetPackageManager (IMonoDevelopSolutionManager solutionManager)
		{
			this.solutionManager = solutionManager;

			var restartManager = new DeleteOnRestartManager ();

			settings = solutionManager.Settings;

			packageManager = new NuGetPackageManager (
				solutionManager.CreateSourceRepositoryProvider (),
				settings,
				solutionManager,
				restartManager
			);
		}

		public MonoDevelopNuGetPackageManager (NuGetPackageManager packageManager)
		{
			this.packageManager = packageManager;
		}

		public void ClearDirectInstall (INuGetProjectContext nuGetProjectContext)
		{
			NuGetPackageManager.ClearDirectInstall (nuGetProjectContext);
		}

		public void SetDirectInstall (PackageIdentity directInstall, INuGetProjectContext nuGetProjectContext)
		{
			NuGetPackageManager.SetDirectInstall (directInstall, nuGetProjectContext);
		}

		public Task ExecuteNuGetProjectActionsAsync (
			NuGetProject nuGetProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			SourceCacheContext sourceCacheContext,
			CancellationToken token)
		{
			return packageManager.ExecuteNuGetProjectActionsAsync (
				nuGetProject,
				nuGetProjectActions,
				nuGetProjectContext,
				sourceCacheContext,
				token);
		}

		public Task ExecuteNuGetProjectActionsAsync (
			IEnumerable<NuGetProject> nuGetProjects,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			SourceCacheContext sourceCacheContext,
			CancellationToken token)
		{
			return packageManager.ExecuteNuGetProjectActionsAsync (
				nuGetProjects,
				nuGetProjectActions,
				nuGetProjectContext,
				sourceCacheContext,
				token);
		}

		public Task<ResolvedPackage> GetLatestVersionAsync (
			string packageId,
			NuGetProject project,
			ResolutionContext resolutionContext,
			IEnumerable<SourceRepository> sources,
			ILogger log,
			CancellationToken token)
		{
			return NuGetPackageManager.GetLatestVersionAsync (
				packageId,
				project,
				resolutionContext,
				sources,
				log,
				token
			);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewInstallPackageAsync (
			NuGetProject nuGetProject,
			PackageIdentity packageIdentity,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			return packageManager.PreviewInstallPackageAsync (
				nuGetProject,
				packageIdentity,
				resolutionContext,
				nuGetProjectContext,
				primarySources,
				secondarySources,
				token
			);
		}

		public bool PackageExistsInPackagesFolder (PackageIdentity packageIdentity)
		{
			return packageManager.PackageExistsInPackagesFolder (packageIdentity);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			string packageId,
			NuGetProject nuGetProject,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			return packageManager.PreviewUpdatePackagesAsync (
				packageId,
				new [] { nuGetProject },
				resolutionContext,
				nuGetProjectContext,
				primarySources,
				secondarySources,
				token
			);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			NuGetProject nuGetProject,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			return packageManager.PreviewUpdatePackagesAsync (
				new [] { nuGetProject },
				resolutionContext,
				nuGetProjectContext,
				primarySources,
				secondarySources,
				token
			);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewUpdatePackagesAsync (
			List<PackageIdentity> packageIdentities,
			IEnumerable<NuGetProject> nuGetProjects,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			return packageManager.PreviewUpdatePackagesAsync (
				packageIdentities,
				nuGetProjects,
				resolutionContext,
				nuGetProjectContext,
				primarySources,
				secondarySources,
				token);
		}

		public Task<IEnumerable<NuGetProjectAction>> PreviewUninstallPackageAsync (
			NuGetProject nuGetProject,
			string packageId,
			UninstallationContext uninstallationContext,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			return packageManager.PreviewUninstallPackageAsync (
				nuGetProject,
				packageId,
				uninstallationContext,
				nuGetProjectContext,
				token
			);
		}

		public Task<BuildIntegratedProjectAction> PreviewBuildIntegratedProjectActionsAsync(
			IBuildIntegratedNuGetProject buildIntegratedProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			return packageManager.PreviewBuildIntegratedProjectActionsAsync (
				(BuildIntegratedNuGetProject)buildIntegratedProject,
				nuGetProjectActions,
				nuGetProjectContext,
				token
			);
		}

		public async Task OpenReadmeFiles (
			NuGetProject nuGetProject,
			IEnumerable<PackageIdentity> packages,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			var executionContext = nuGetProjectContext.ExecutionContext;
			if (executionContext != null) {
				foreach (var package in packages) {
					await OpenReadmeFiles (nuGetProject, package, executionContext);
					if (token.IsCancellationRequested) {
						return;
					}
				}
			}
		}

		async Task<bool> OpenReadmeFiles (
			NuGetProject nuGetProject,
			PackageIdentity package,
			NuGet.ProjectManagement.ExecutionContext executionContext)
		{
			//packagesPath is different for project.json vs Packages.config scenarios. So check if the project is a build-integrated project
			var buildIntegratedProject = nuGetProject as BuildIntegratedNuGetProject;
			var readmeFilePath = String.Empty;

			if (buildIntegratedProject != null) {
				var pathContext = NuGetPathContext.Create (settings);
				var pathResolver = new FallbackPackagePathResolver (pathContext);
				string packageFolderPath = pathResolver.GetPackageDirectory (package.Id, package.Version);

				if (Directory.Exists (packageFolderPath)) {
					readmeFilePath = Path.Combine (packageFolderPath, Constants.ReadmeFileName);
				}
			} else {
				var packagePath = packageManager.PackagesFolderNuGetProject.GetInstalledPackageFilePath (package);
				if (File.Exists(packagePath)) {
					readmeFilePath = Path.Combine (Path.GetDirectoryName (packagePath), Constants.ReadmeFileName);
				}
			}

			if (File.Exists (readmeFilePath)) {
				await executionContext.OpenFile (readmeFilePath);
				return true;
			}
			return false;
		}

		public async Task OpenReadmeFiles (
			IEnumerable<NuGetProject> nuGetProjects,
			IEnumerable<PackageIdentity> packages,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			var executionContext = nuGetProjectContext.ExecutionContext;
			if (executionContext == null)
				return;

			var buildIntegratedProject = nuGetProjects
				.OfType<BuildIntegratedNuGetProject> ()
				.FirstOrDefault ();

			var nonBuildIntegratedProject = nuGetProjects
				.Where (project => !(project is BuildIntegratedNuGetProject))
				.FirstOrDefault ();

			foreach (PackageIdentity package in packages) {
				if (nonBuildIntegratedProject != null) {
					if (await OpenReadmeFiles (nonBuildIntegratedProject, package, executionContext))
						continue;
				}

				if (buildIntegratedProject != null)
					await OpenReadmeFiles (buildIntegratedProject, package, executionContext);

				if (token.IsCancellationRequested)
					return;
			}
		}

		public Task RunPostProcessAsync (
			List<NuGetProject> nuGetProjects,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			if (nuGetProjects.Count == 1) {
				return nuGetProjects [0].RunPostProcessAsync (nuGetProjectContext, token);
			}

			var buildIntegratedProjects = nuGetProjects.OfType<BuildIntegratedNuGetProject> ().ToList ();
			if (!buildIntegratedProjects.Any ())
				return Task.CompletedTask;

			return RestorePackagesAsync (buildIntegratedProjects, token);
		}

		async Task RestorePackagesAsync (
			List<BuildIntegratedNuGetProject> projects,
			CancellationToken token)
		{
			var packageRestorer = new MonoDevelopBuildIntegratedRestorer (solutionManager);

			var restoreTask = packageRestorer.RestorePackages (projects, token);
			using (var task = new PackageRestoreTask (restoreTask)) {
				await restoreTask;
			}

			// Ensure MSBuild tasks are up to date when the next build is run.
			foreach (BuildIntegratedNuGetProject project in projects) {
				var dotNetProject = project.GetDotNetProject ();
				dotNetProject.DotNetProject.ShutdownProjectBuilder ();

				if (!packageRestorer.LockFileChanged) {
					// Need to refresh the references since the restore did not.
					await Runtime.RunInMainThread (() => {
						dotNetProject.DotNetProject.DotNetCoreNotifyReferencesChanged ();
					});
				}
			}
		}
	}
}

