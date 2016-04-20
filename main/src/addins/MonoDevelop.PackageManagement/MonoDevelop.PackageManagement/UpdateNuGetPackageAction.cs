//
// UpdateNuGetPackageAction.cs
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
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;

namespace MonoDevelop.PackageManagement
{
	internal class UpdateNuGetPackageAction : INuGetPackageAction
	{
		NuGetPackageManager packageManager;
		NuGetProject project;
		CancellationToken cancellationToken;
		List<SourceRepository> primarySources;
		ISourceRepositoryProvider sourceRepositoryProvider;

		public UpdateNuGetPackageAction (
			IMonoDevelopSolutionManager solutionManager,
			NuGetProject project,
			CancellationToken cancellationToken = default(CancellationToken))
		{
			this.project = project;
			this.cancellationToken = cancellationToken;

			var restartManager = new DeleteOnRestartManager ();

			sourceRepositoryProvider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			primarySources = sourceRepositoryProvider.GetRepositories ().ToList ();

			packageManager = new NuGetPackageManager (
				sourceRepositoryProvider,
				solutionManager.Settings,
				solutionManager,
				restartManager
			);
		}

		public string PackageId { get; set; }
		public bool IncludePrerelease { get; set; }

		public void Execute ()
		{
			ExecuteAsync ().Wait ();
		}

		async Task ExecuteAsync ()
		{
			INuGetProjectContext context = CreateProjectContext ();

			var actions = await packageManager.PreviewUpdatePackagesAsync (
				PackageId,
				project,
				CreateResolutionContext (),
				context,
				primarySources,
				new SourceRepository[0],
				cancellationToken);

			await CheckLicenses (actions);

			using (IDisposable referenceMaintainer = CreateLocalCopyReferenceMaintainer ()) {
				await packageManager.ExecuteNuGetProjectActionsAsync (
					project,
					actions,
					context,
					cancellationToken);
			}

			await project.RunPostProcessAsync (context, cancellationToken);
		}

		public bool HasPackageScriptsToRun ()
		{
			return false;
		}

		ResolutionContext CreateResolutionContext ()
		{
			return new ResolutionContext (
				DependencyBehavior.Lowest,
				IncludePrerelease,
				false,
				VersionConstraints.None
			);
		}

		INuGetProjectContext CreateProjectContext ()
		{
			return new NuGetProjectContext (); 
		}

		Task CheckLicenses (IEnumerable<NuGetProjectAction> actions)
		{
			return NuGetPackageLicenseAuditor.AcceptLicenses (primarySources, actions, cancellationToken);
		}

		LocalCopyReferenceMaintainer CreateLocalCopyReferenceMaintainer ()
		{
			return new LocalCopyReferenceMaintainer (PackageManagementServices.PackageManagementEvents);
		}
	}
}

