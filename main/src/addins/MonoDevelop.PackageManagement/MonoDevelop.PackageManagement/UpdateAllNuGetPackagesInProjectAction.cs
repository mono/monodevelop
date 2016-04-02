//
// UpdateAllNuGetPackagesInProjectAction.cs
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
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace MonoDevelop.PackageManagement
{
	internal class UpdateAllNuGetPackagesInProjectAction : IPackageAction
	{
		NuGetPackageManager packageManager;
		PackageRestoreManager restoreManager;
		ISolutionManager solutionManager;
		IPackageManagementEvents packageManagementEvents;
		DotNetProject dotNetProject;
		NuGetProject project;
		CancellationToken cancellationToken;
		ISourceRepositoryProvider sourceRepositoryProvider;
		bool includePrerelease;
		string projectName;

		public UpdateAllNuGetPackagesInProjectAction (
			ISolutionManager solutionManager,
			DotNetProject dotNetProject,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.solutionManager = solutionManager;
			this.dotNetProject = dotNetProject;
			this.cancellationToken = cancellationToken;

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			project = new MonoDevelopNuGetProjectFactory ()
				.CreateNuGetProject (dotNetProject);
			
			projectName = dotNetProject.Name;

			var settings = Settings.LoadDefaultSettings (null, null, null);
			var restartManager = new DeleteOnRestartManager ();

			sourceRepositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();

			packageManager = new NuGetPackageManager (
				sourceRepositoryProvider,
				settings,
				solutionManager,
				restartManager
			);

			restoreManager = new PackageRestoreManager (
				SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (),
				Settings.LoadDefaultSettings (null, null, null),
				solutionManager
			);
		}

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		async Task ExecuteAsync ()
		{
			INuGetProjectContext context = CreateProjectContext ();

			includePrerelease = await ProjectHasPrereleasePackages ();

			await RestoreAnyMissingPackages (context);

			var actions = await packageManager.PreviewUpdatePackagesAsync (
				project,
				CreateResolutionContext (),
				context,
				sourceRepositoryProvider.GetRepositories ().ToList (),
				new SourceRepository[0],
				cancellationToken);

			await packageManager.ExecuteNuGetProjectActionsAsync (
				project,
				actions,
				context,
				cancellationToken);
		}

		async Task<bool> ProjectHasPrereleasePackages ()
		{
			var packageReferences = await project.GetInstalledPackagesAsync (cancellationToken);
			return packageReferences.Any (packageReference => packageReference.PackageIdentity.Version.IsPrerelease);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		ResolutionContext CreateResolutionContext ()
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				includePrerelease,
				false,
				VersionConstraints.None
			);
		}

		INuGetProjectContext CreateProjectContext ()
		{
			return new NuGetProjectContext (); 
		}

		async Task RestoreAnyMissingPackages (INuGetProjectContext context)
		{
			var packages = await restoreManager.GetPackagesInSolutionAsync (
				solutionManager.SolutionDirectory,
				cancellationToken);

			var missingPackages = packages.Select (package => IsMissingForCurrentProject (package)).ToList ();
			if (missingPackages.Any ()) {
				await restoreManager.RestoreMissingPackagesAsync (
					solutionManager.SolutionDirectory,
					project,
					context,
					cancellationToken);

				await Runtime.RunInMainThread (() => dotNetProject.RefreshReferenceStatus ());

				packageManagementEvents.OnPackagesRestored ();
			}
		}

		bool IsMissingForCurrentProject (PackageRestoreData package)
		{
			return package.IsMissing && package.ProjectNames.Any (name => name == projectName);
		}
	}
}

