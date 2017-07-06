//
// ReinstallNuGetPackageAction.cs
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
using System.Threading;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class ReinstallNuGetPackageAction : IPackageAction
	{
		INuGetProjectContext context;
		InstallNuGetPackageAction installAction;
		UninstallNuGetPackageAction uninstallAction;
		IPackageManagementEvents packageManagementEvents;

		public ReinstallNuGetPackageAction (
			IDotNetProject project,
			IMonoDevelopSolutionManager solutionManager)
			: this (
				project,
				solutionManager,
				new NuGetProjectContext (),
				PackageManagementServices.PackageManagementEvents)
		{
		}

		public ReinstallNuGetPackageAction (
			IDotNetProject project,
			IMonoDevelopSolutionManager solutionManager,
			INuGetProjectContext projectContext,
			IPackageManagementEvents packageManagementEvents)
		{
			this.context = projectContext;
			this.packageManagementEvents = packageManagementEvents;

			var repositories = solutionManager.CreateSourceRepositoryProvider ().GetRepositories ();

			installAction = CreateInstallAction (solutionManager, project, repositories);
			uninstallAction = CreateUninstallAction (solutionManager, project);
		}

		public string PackageId { get; set; }
		public NuGetVersion Version { get; set; }

		public PackageActionType ActionType {
			get { return PackageActionType.Install; }
		}

		public void Execute ()
		{
			Execute (CancellationToken.None);
		}

		public void Execute (CancellationToken cancellationToken)
		{
			using (IDisposable referenceMaintainer = CreateLocalCopyReferenceMaintainer ()) {
				using (IDisposable fileMonitor = CreateFileMonitor ()) {
					uninstallAction.PackageId = PackageId;
					uninstallAction.ForceRemove = true;
					uninstallAction.Execute (cancellationToken);

					installAction.PackageId = PackageId;
					installAction.Version = Version;
					installAction.LicensesMustBeAccepted = false;
					installAction.OpenReadmeFile = false;

					// Local copy references need to be preserved before the uninstall starts so
					// we must disable this for the install action otherwise they will not be
					// preserved.
					installAction.PreserveLocalCopyReferences = false;

					installAction.Execute (cancellationToken);
				}
			}
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		protected virtual UninstallNuGetPackageAction CreateUninstallAction (IMonoDevelopSolutionManager solutionManager, IDotNetProject project)
		{
			return new UninstallNuGetPackageAction (
				solutionManager,
				project) {
			};
		}

		protected virtual InstallNuGetPackageAction CreateInstallAction (
			IMonoDevelopSolutionManager solutionManager,
			IDotNetProject project,
			IEnumerable<SourceRepository> repositories)
		{
			return new InstallNuGetPackageAction (
				repositories,
				solutionManager,
				project,
				context
			);
		}

		LocalCopyReferenceMaintainer CreateLocalCopyReferenceMaintainer ()
		{
			return new LocalCopyReferenceMaintainer (packageManagementEvents);
		}

		IDisposable CreateFileMonitor ()
		{
			return new PreventPackagesConfigFileBeingRemovedOnUpdateMonitor (
				packageManagementEvents,
				GetFileRemover ());
		}

		protected virtual IFileRemover GetFileRemover ()
		{
			return new FileRemover ();
		}
	}
}

