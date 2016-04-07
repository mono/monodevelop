//
// RestoreNuGetPackagesInNuGetIntegratedProject.cs
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
using NuGet.Configuration;
using NuGet.LibraryModel;
using NuGet.Logging;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class RestoreNuGetPackagesInNuGetIntegratedProject : IPackageAction
	{
		DotNetProject project;
		BuildIntegratedNuGetProject nugetProject;
		CancellationToken cancellationToken;
		IPackageManagementEvents packageManagementEvents;
		List<SourceRepository> packageSources;
		string packagesFolder;

		public RestoreNuGetPackagesInNuGetIntegratedProject (
			DotNetProject project,
			BuildIntegratedNuGetProject nugetProject,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.project = project;
			this.nugetProject = nugetProject;
			this.cancellationToken = cancellationToken;
			this.packageManagementEvents = PackageManagementServices.PackageManagementEvents;

			packageSources = SourceRepositoryProviderFactory
				.CreateSourceRepositoryProvider ()
				.GetRepositories ()
				.ToList ();

			ISettings settings = Settings.LoadDefaultSettings (null, null, null);

			packagesFolder = BuildIntegratedProjectUtility.GetEffectiveGlobalPackagesFolder ( 
				project.ParentSolution.BaseDirectory,
				settings); 
		}

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		async Task ExecuteAsync ()
		{
			RestoreResult restoreResult = await BuildIntegratedRestoreUtility.RestoreAsync (
				nugetProject,
				CreateLogger (),
				packageSources, 
				packagesFolder, 
				cancellationToken);

			if (!restoreResult.Success) {
				ReportRestoreError (restoreResult);
			}
		}

		ILogger CreateLogger ()
		{
			return new PackageManagementLogger (packageManagementEvents);
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
	}
}

