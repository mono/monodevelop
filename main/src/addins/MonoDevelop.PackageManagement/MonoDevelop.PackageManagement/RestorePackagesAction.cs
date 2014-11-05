//
// RestorePackagesAction.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.PackageManagement;
using NuGet;
using MonoDevelop.Core;

namespace MonoDevelop.PackageManagement
{
	public class RestorePackagesAction : IPackageAction
	{
		IPackageManagementSolution solution;
		IPackageManagementEvents packageManagementEvents;
		ISolutionPackageRepository solutionPackageRepository;
		IPackageRepositoryCache repositoryCache;
		IPackageManagerFactory packageManagerFactory;
		ILogger logger;

		public RestorePackagesAction ()
			: this (
				PackageManagementServices.Solution,
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public RestorePackagesAction (
			IPackageManagementSolution solution,
			IPackageManagementEvents packageManagementEvents)
			: this (
				solution,
				packageManagementEvents,
				PackageManagementServices.PackageRepositoryCache,
				new SharpDevelopPackageManagerFactory ())
		{
		}

		public RestorePackagesAction (
			IPackageManagementSolution solution,
			IPackageManagementEvents packageManagementEvents,
			IPackageRepositoryCache repositoryCache,
			IPackageManagerFactory packageManagerFactory)
		{
			this.solution = solution;
			this.packageManagementEvents = packageManagementEvents;
			this.repositoryCache = repositoryCache;
			this.packageManagerFactory = packageManagerFactory;

			logger = new PackageManagementLogger (packageManagementEvents);
		}

		public IDotNetProject Project { get; set; }

		public void Execute ()
		{
			Log ("Restoring packages...");

			int packagesRestored = 0;
			List<PackageReference> packageReferences = GetPackageReferences ().ToList ();
			foreach (PackageReference packageReference in packageReferences) {
				if (IsPackageRestored (packageReference)) {
					LogPackageAlreadyRestored (packageReference);
				} else {
					packagesRestored++;
					RestorePackage (packageReference.Id, packageReference.Version);
				}
			}

			LogResult (packageReferences.Count, packagesRestored);
		}

		void Log (string message)
		{
			logger.Log (MessageLevel.Info, message);
		}

		void LogPackageAlreadyRestored (PackageReference packageReference)
		{
			logger.Log (MessageLevel.Debug, GettextCatalog.GetString ("Skipping '{0}' because it is already restored.", packageReference));
		}

		void LogResult (int totalPackageReferences, int packagesRestored)
		{
			if (packagesRestored == 0) {
				Log ("All packages are already restored.");
			} else if (packagesRestored == 1) {
				Log (GettextCatalog.GetString ("1 package restored successfully."));
			} else if (packagesRestored > 0) {
				Log (GettextCatalog.GetString ("{0} packages restored successfully.", packagesRestored));
			}
		}

		IEnumerable<PackageReference> GetPackageReferences ()
		{
			if (Project != null) {
				return GetPackageReferencesForSingleProject ();
			}
			return GetPackageReferencesForSolution ()
				.Concat (GetPackageReferencesForAllProjects ());
		}

		IEnumerable<PackageReference> GetPackageReferencesForSingleProject ()
		{
			IPackageRepository repository = repositoryCache.CreateAggregateRepository ();
			IPackageManagementProject project = solution.GetProject (repository, Project);
			return project.GetPackageReferences ();
		}

		IEnumerable<PackageReference> GetPackageReferencesForSolution ()
		{
			return SolutionPackageRepository.GetPackageReferences ();
		}

		IEnumerable<PackageReference> GetPackageReferencesForAllProjects ()
		{
			return solution
				.GetProjects (repositoryCache.CreateAggregateRepository ())
				.SelectMany (project => project.GetPackageReferences ())
				.Distinct ();
		}

		bool IsPackageRestored (PackageReference packageReference)
		{
			return SolutionPackageRepository.IsRestored (packageReference);
		}

		ISolutionPackageRepository SolutionPackageRepository {
			get {
				if (solutionPackageRepository == null) {
					solutionPackageRepository = solution.GetRepository ();
				}
				return solutionPackageRepository;
			}
		}

		void RestorePackage (string packageId, SemanticVersion version)
		{
			IPackageRepository sourceRepository = CreateSourceRepository ();
			using (IDisposable operation = sourceRepository.StartRestoreOperation (packageId, version.ToString ())) {
				IPackage package = PackageHelper.ResolvePackage (sourceRepository, packageId, version);
				IPackageManager packageManager = CreatePackageManager (sourceRepository);

				packageManager.InstallPackage (
					package,
					ignoreDependencies: true,
					allowPrereleaseVersions: true,
					ignoreWalkInfo: true);

				packageManagementEvents.OnPackageRestored (package);
			}
		}

		IPackageRepository CreateSourceRepository ()
		{
			return repositoryCache.CreateAggregateWithPriorityMachineCacheRepository ();
		}

		IPackageManager CreatePackageManager (IPackageRepository sourceRepository)
		{
			IPackageManager packageManager = packageManagerFactory.CreatePackageManager (sourceRepository, SolutionPackageRepository);
			packageManager.Logger = logger;
			return packageManager;
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}
	}
}

