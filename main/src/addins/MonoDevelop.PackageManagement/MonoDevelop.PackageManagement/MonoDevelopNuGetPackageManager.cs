//
// MonoDevelopNuGetPackageManager.cs
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
using System.Threading.Tasks;
using NuGet.Logging;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopNuGetPackageManager : INuGetPackageManager
	{
		NuGetPackageManager packageManager;

		public MonoDevelopNuGetPackageManager (IMonoDevelopSolutionManager solutionManager)
		{
			var restartManager = new DeleteOnRestartManager ();

			packageManager = new NuGetPackageManager (
				solutionManager.CreateSourceRepositoryProvider (),
				solutionManager.Settings,
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
			CancellationToken token)
		{
			return packageManager.ExecuteNuGetProjectActionsAsync (
				nuGetProject,
				nuGetProjectActions,
				nuGetProjectContext,
				token);
		}

		public Task<NuGetVersion> GetLatestVersionAsync (
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
				nuGetProject,
				resolutionContext,
				nuGetProjectContext,
				primarySources,
				secondarySources,
				token
			);
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
	}
}

