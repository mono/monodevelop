﻿//
// RestoreAndCheckForUpdatesAction.cs
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
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class RestoreAndCheckForUpdatesAction : IPackageAction
	{
		List<PackageRestoreData> packagesToRestore;
		IPackageRestoreManager restoreManager;
		MonoDevelopBuildIntegratedRestorer buildIntegratedRestorer;
		NuGetAwareProjectPackageRestoreManager nugetAwareRestorer;
		IMonoDevelopSolutionManager solutionManager;
		IPackageManagementEvents packageManagementEvents;
		Solution solution;
		ISourceRepositoryProvider sourceRepositoryProvider;
		List<NuGetProject> nugetProjects;
		List<BuildIntegratedNuGetProject> buildIntegratedProjectsToBeRestored;
		List<INuGetAwareProject> nugetAwareProjectsToBeRestored;
		List<INuGetAwareProject> nugetAwareProjects;

		public RestoreAndCheckForUpdatesAction (Solution solution)
		{
			this.solution = solution;
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			solutionManager = new MonoDevelopSolutionManager (solution);

			// Use the same source repository provider for all restores and updates to prevent
			// the credential dialog from being displayed for each restore and updates.
			sourceRepositoryProvider = solutionManager.CreateSourceRepositoryProvider ();
		}

		async Task PrepareForExecute ()
		{
			nugetProjects = (await solutionManager.GetNuGetProjectsAsync ()).ToList ();
			if (AnyProjectsUsingPackagesConfig ()) {
				restoreManager = new PackageRestoreManager (
					sourceRepositoryProvider,
					solutionManager.Settings,
					solutionManager
				);
			}

			if (AnyDotNetCoreProjectsOrProjectsUsingProjectJson ()) {
				buildIntegratedRestorer = new MonoDevelopBuildIntegratedRestorer (
					solutionManager,
					sourceRepositoryProvider,
					solutionManager.Settings);
			}

			if (AnyNuGetAwareProjects ()) {
				nugetAwareRestorer = new NuGetAwareProjectPackageRestoreManager (solutionManager);
			}
		}

		bool AnyProjectsUsingPackagesConfig ()
		{
			return nugetProjects.Any (project => !(project is BuildIntegratedNuGetProject));
		}

		bool AnyDotNetCoreProjectsOrProjectsUsingProjectJson ()
		{
			return GetBuildIntegratedNuGetProjects ().Any ();
		}

		IEnumerable<BuildIntegratedNuGetProject> GetBuildIntegratedNuGetProjects ()
		{
			return nugetProjects.OfType<BuildIntegratedNuGetProject> ();
		}

		bool AnyNuGetAwareProjects ()
		{
			nugetAwareProjects = solution.GetAllProjects ().OfType<INuGetAwareProject> ().ToList ();
			return nugetAwareProjects.Any ();
		}

		public PackageActionType ActionType {
			get { return PackageActionType.Restore; }
		}

		public bool CheckForUpdatesAfterRestore { get; set; }

		public async Task<bool> HasMissingPackages (CancellationToken cancellationToken = default(CancellationToken))
		{
			await PrepareForExecute ();

			if (restoreManager != null) {
				var packages = await restoreManager.GetPackagesInSolutionAsync (
					solutionManager.SolutionDirectory,
					cancellationToken);

				packagesToRestore = packages.ToList ();
			}

			if (buildIntegratedRestorer != null) {
				buildIntegratedProjectsToBeRestored = GetBuildIntegratedNuGetProjects ().ToList ();
			}

			if (nugetAwareRestorer != null) {
				var projects = await nugetAwareRestorer.GetProjectsRequiringRestore (nugetAwareProjects);
				nugetAwareProjectsToBeRestored = projects.ToList ();
			}

			return packagesToRestore?.Any (package => package.IsMissing) == true ||
				buildIntegratedProjectsToBeRestored?.Any () == true ||
				nugetAwareProjectsToBeRestored?.Any () == true;
		}

		public void Execute ()
		{
		}

		public void Execute (CancellationToken cancellationToken)
		{
			Task task = RestorePackagesAsync (cancellationToken);
			using (var restoreTask = new PackageRestoreTask (task)) {
				task.Wait (cancellationToken);
			}

			if (CheckForUpdatesAfterRestore && !cancellationToken.IsCancellationRequested) {
				CheckForUpdates ();
			}
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		void CheckForUpdates ()
		{
			try {
				PackageManagementServices.UpdatedPackagesInWorkspace.CheckForUpdates (new SolutionProxy (solution), sourceRepositoryProvider);
			} catch (Exception ex) {
				LoggingService.LogError ("Check for NuGet package updates error.", ex);
			}
		}

		async Task RestorePackagesAsync (CancellationToken cancellationToken)
		{
			if (restoreManager != null) {
				using (var monitor = new PackageRestoreMonitor (restoreManager)) {
					using (var cacheContext = new SourceCacheContext ()) {
						var downloadContext = new PackageDownloadContext (cacheContext);
						await restoreManager.RestoreMissingPackagesAsync (
							solutionManager.SolutionDirectory,
							packagesToRestore,
							new NuGetProjectContext (solutionManager.Settings),
							downloadContext,
							cancellationToken);
					}
				}
			}

			if (buildIntegratedRestorer != null) {
				await buildIntegratedRestorer.RestorePackages (buildIntegratedProjectsToBeRestored, cancellationToken);
			}

			if (nugetAwareRestorer != null) {
				await nugetAwareRestorer.RestoreMissingPackagesAsync (
					nugetAwareProjectsToBeRestored,
					new NuGetProjectContext (solutionManager.Settings),
					cancellationToken);
			}

			await Runtime.RunInMainThread (() => RefreshProjectReferences ());

			packageManagementEvents.OnPackagesRestored ();
		}

		void RefreshProjectReferences ()
		{
			foreach (DotNetProject dotNetProject in solution.GetAllDotNetProjects ()) {
				dotNetProject.RefreshReferenceStatus ();
			}
		}
	}
}

