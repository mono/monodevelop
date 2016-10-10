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
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.LibraryModel;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopBuildIntegratedRestorer
	{
		IPackageManagementEvents packageManagementEvents;
		List<SourceRepository> sourceRepositories;
		ISettings settings;
		ExternalProjectReferenceContext context;

		public MonoDevelopBuildIntegratedRestorer (
			ISourceRepositoryProvider repositoryProvider,
			ISettings settings)
		{
			sourceRepositories = repositoryProvider.GetRepositories ().ToList ();
			this.settings = settings;

			packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			context = CreateRestoreContext ();
		}

		public async Task RestorePackages (
			IEnumerable<BuildIntegratedNuGetProject> projects,
			CancellationToken cancellationToken)
		{
			foreach (BuildIntegratedNuGetProject project in projects) {
				await RestorePackages (project, cancellationToken);
			}
		}

		public async Task RestorePackages (
			BuildIntegratedNuGetProject project,
			CancellationToken cancellationToken)
		{
			var nugetPaths = NuGetPathContext.Create (settings);

			RestoreResult restoreResult = await BuildIntegratedRestoreUtility.RestoreAsync (
				project,
				context,
				sourceRepositories, 
				nugetPaths.UserPackageFolder,
				nugetPaths.FallbackPackageFolders,
				cancellationToken);

			if (!restoreResult.Success) {
				ReportRestoreError (restoreResult);
			}
		}

		ILogger CreateLogger ()
		{
			return new PackageManagementLogger (packageManagementEvents);
		}

		ExternalProjectReferenceContext CreateRestoreContext ()
		{
			return new ExternalProjectReferenceContext (CreateLogger ());
		}

		void ReportRestoreError (RestoreResult restoreResult)
		{
			foreach (LibraryRange libraryRange in restoreResult.GetAllUnresolved ()) {
				packageManagementEvents.OnPackageOperationMessageLogged (
					NuGet.MessageLevel.Info,
					GettextCatalog.GetString ("Restore failed for '{0}'."),
					libraryRange.ToString ());
			}
			throw new ApplicationException (GettextCatalog.GetString ("Restore failed."));
		}

		public Task<bool> IsRestoreRequired (BuildIntegratedNuGetProject project)
		{
			var nugetPaths = NuGetPathContext.Create (settings);
			var projects = new BuildIntegratedNuGetProject[] { project };
			return DependencyGraphRestoreUtility.IsRestoreRequiredAsync (projects, false, nugetPaths, context);
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
	}
}

