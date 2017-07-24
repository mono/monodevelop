//
// MonoDevelopBuildIntegratedRestorer.cs
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
using NuGet.Common;
using NuGet.Configuration;
using NuGet.LibraryModel;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopBuildIntegratedRestorer : IMonoDevelopBuildIntegratedRestorer
	{
		IPackageManagementEvents packageManagementEvents;
		List<SourceRepository> sourceRepositories;
		ISettings settings;
		IMonoDevelopSolutionManager solutionManager;
		DependencyGraphCacheContext context;

		public MonoDevelopBuildIntegratedRestorer (IMonoDevelopSolutionManager solutionManager)
			: this (
				solutionManager,
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings)
		{
		}

		public MonoDevelopBuildIntegratedRestorer (
			IMonoDevelopSolutionManager solutionManager,
			ISourceRepositoryProvider repositoryProvider,
			ISettings settings)
		{
			this.solutionManager = solutionManager;
			sourceRepositories = repositoryProvider.GetRepositories ().ToList ();
			this.settings = settings;

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			context = CreateRestoreContext ();
		}

		public bool LockFileChanged { get; private set; }

		public async Task RestorePackages (
			IEnumerable<BuildIntegratedNuGetProject> projects,
			CancellationToken cancellationToken)
		{
			var changedLocks = new List<FilePath> ();
			var affectedProjects = new List<BuildIntegratedNuGetProject> ();

			foreach (BuildIntegratedNuGetProject project in projects) {
				DotNetProject projectToReload = GetProjectToReloadAfterRestore (project);
				var changedLock = await RestorePackagesInternal (project, cancellationToken);
				if (projectToReload != null) {
					await ReloadProject (projectToReload, changedLock);
				} else if (changedLock != null) {
					changedLocks.Add (changedLock);
					affectedProjects.Add (project);
				}
			}

			if (changedLocks.Count > 0) {
				LockFileChanged = true;
				await Runtime.RunInMainThread (() => {
					FileService.NotifyFilesChanged (changedLocks);
					foreach (var project in affectedProjects) {
						// Restoring the entire solution so do not refresh references for
						// transitive  project references since they should be refreshed anyway.
						NotifyProjectReferencesChanged (project, includeTransitiveProjectReferences: false);
					}
				});
			}
		}

		public async Task RestorePackages (
			BuildIntegratedNuGetProject project,
			CancellationToken cancellationToken)
		{
			DotNetProject projectToReload = GetProjectToReloadAfterRestore (project);

			var changedLock = await RestorePackagesInternal (project, cancellationToken);

			if (projectToReload != null) {
				// Need to ensure transitive project references are refreshed if only the single
				// project is reloaded since they will still be out of date.
				await ReloadProject (projectToReload, changedLock, refreshTransitiveReferences: true);
			} else if (changedLock != null) {
				LockFileChanged = true;
				await Runtime.RunInMainThread (() => {
					FileService.NotifyFileChanged (changedLock);

					// Restoring a single project so ensure references are refreshed for
					// transitive project references.
					NotifyProjectReferencesChanged (project, includeTransitiveProjectReferences: true);
				});
			}
		}

		//returns the lock file, if it changed
		async Task<string> RestorePackagesInternal (
			BuildIntegratedNuGetProject project,
			CancellationToken cancellationToken)
		{
			var now = DateTime.UtcNow;
			Action<SourceCacheContext> cacheContextModifier = c => c.MaxAge = now;

			RestoreResult restoreResult = await DependencyGraphRestoreUtility.RestoreProjectAsync (
				solutionManager,
				project,
				context,
				new RestoreCommandProvidersCache (),
				cacheContextModifier,
				sourceRepositories,
				settings,
				context.Logger,
				cancellationToken);

			if (restoreResult.Success) {
				if (!object.Equals (restoreResult.LockFile, restoreResult.PreviousLockFile)) {
					return restoreResult.LockFilePath;
				}
			} else {
				ReportRestoreError (restoreResult);
			}
			return null;
		}

		static void NotifyProjectReferencesChanged (
			BuildIntegratedNuGetProject project,
			bool includeTransitiveProjectReferences)
		{
			var buildIntegratedProject = project as IBuildIntegratedNuGetProject;
			if (buildIntegratedProject != null) {
				buildIntegratedProject.NotifyProjectReferencesChanged (includeTransitiveProjectReferences);
			}
		}

		ILogger CreateLogger ()
		{
			return new PackageManagementLogger (packageManagementEvents);
		}

		DependencyGraphCacheContext CreateRestoreContext ()
		{
			return new DependencyGraphCacheContext (CreateLogger ());
		}

		void ReportRestoreError (RestoreResult restoreResult)
		{
			foreach (LibraryRange libraryRange in restoreResult.GetAllUnresolved ()) {
				packageManagementEvents.OnPackageOperationMessageLogged (
					MessageLevel.Info,
					GettextCatalog.GetString ("Restore failed for '{0}'."),
					libraryRange.ToString ());
			}
			throw new ApplicationException (GettextCatalog.GetString ("Restore failed."));
		}

		public Task<bool> IsRestoreRequired (BuildIntegratedNuGetProject project)
		{
			var pathContext = NuGetPathContext.Create (settings);
			var packageFolderPaths = new List<string> ();
			packageFolderPaths.Add (pathContext.UserPackageFolder);
			packageFolderPaths.AddRange (pathContext.FallbackPackageFolders);
			var pathResolvers = packageFolderPaths.Select (path => new VersionFolderPathResolver (path));

			var packagesChecked = new HashSet<PackageIdentity> ();

			return project.IsRestoreRequired (pathResolvers, packagesChecked, context);
		}

		public async Task<IEnumerable<BuildIntegratedNuGetProject>> GetProjectsRequiringRestore (
			IEnumerable<BuildIntegratedNuGetProject> projects)
		{
			var projectsToBeRestored = new List<BuildIntegratedNuGetProject> ();

			foreach (BuildIntegratedNuGetProject project in projects) {
				bool restoreRequired = await IsRestoreRequired (project);
				if (restoreRequired) {
					projectsToBeRestored.Add (project);
				}
			}

			return projectsToBeRestored;
		}

		DotNetProject GetProjectToReloadAfterRestore (BuildIntegratedNuGetProject project)
		{
			var dotNetCoreNuGetProject = project as DotNetCoreNuGetProject;
			if (dotNetCoreNuGetProject?.ProjectRequiresReloadAfterRestore () == true)
				return dotNetCoreNuGetProject.DotNetProject;

			return null;
		}

		Task ReloadProject (DotNetProject projectToReload, string changedLock, bool refreshTransitiveReferences = false)
		{
			return Runtime.RunInMainThread (async () => {
				if (changedLock != null) {
					LockFileChanged = true;
					FileService.NotifyFileChanged (changedLock);
				}
				await projectToReload.ReevaluateProject (new ProgressMonitor ());

				if (refreshTransitiveReferences)
					projectToReload.DotNetCoreNotifyReferencesChanged (transitiveOnly: true);
			});
		}
	}
}

