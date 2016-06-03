//
// FakeNuGetPackageManager.cs
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
using NuGet.Logging;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeNuGetPackageManager : INuGetPackageManager
	{
		public INuGetProjectContext ClearDirectInstallProjectContext;

		public void ClearDirectInstall (INuGetProjectContext nuGetProjectContext)
		{
			ClearDirectInstallProjectContext = nuGetProjectContext;
		}

		public PackageIdentity SetDirectInstallPackageIdentity;
		public INuGetProjectContext SetDirectInstallProjectContext;

		public void SetDirectInstall (PackageIdentity directInstall, INuGetProjectContext nuGetProjectContext)
		{
			SetDirectInstallPackageIdentity = directInstall;
			SetDirectInstallProjectContext = nuGetProjectContext;
		}

		public NuGetProject ExecutedNuGetProject;
		public List<NuGetProjectAction> ExecutedActions;
		public INuGetProjectContext ExecutedProjectContext;
		public CancellationToken ExecutedCancellationToken;

		public Action BeforeExecuteAction = () => { };

		public Task ExecuteNuGetProjectActionsAsync (
			NuGetProject nuGetProject,
			IEnumerable<NuGetProjectAction> nuGetProjectActions,
			INuGetProjectContext nuGetProjectContext,
			CancellationToken token)
		{
			ExecutedNuGetProject = nuGetProject;
			ExecutedActions = nuGetProjectActions.ToList ();
			ExecutedProjectContext = nuGetProjectContext;
			ExecutedCancellationToken = token;

			BeforeExecuteAction ();

			return Task.FromResult (0);
		}

		public NuGetVersion LatestVersion = new NuGetVersion ("1.2.3");
		public string GetLatestVersionPackageId;
		public NuGetProject GetLatestVersionProject;
		public ResolutionContext GetLatestVersionResolutionContext;
		public List<SourceRepository> GetLatestVersionSources;
		public ILogger GetLatestVersionLogger;
		public CancellationToken GetLatestVersionCancellationToken;

		public Task<NuGetVersion> GetLatestVersionAsync (
			string packageId,
			NuGetProject project,
			ResolutionContext resolutionContext,
			IEnumerable<SourceRepository> sources,
			ILogger log,
			CancellationToken token)
		{
			GetLatestVersionPackageId = packageId;
			GetLatestVersionProject = project;
			GetLatestVersionResolutionContext = resolutionContext;
			GetLatestVersionSources = sources.ToList ();
			GetLatestVersionLogger = log;
			GetLatestVersionCancellationToken = token;

			return Task.FromResult (LatestVersion);
		}

		public List<FakeNuGetProjectAction> InstallActions = new List<FakeNuGetProjectAction> ();

		public NuGetProject PreviewInstallProject;
		public PackageIdentity PreviewInstallPackageIdentity;
		public ResolutionContext PreviewInstallResolutionContext;
		public List<SourceRepository> PreviewInstallPrimarySources;
		public IEnumerable<SourceRepository> PreviewInstallSecondarySources;
		public CancellationToken PreviewInstallCancellationToken;

		public Task<IEnumerable<NuGetProjectAction>> PreviewInstallPackageAsync (
			NuGetProject nuGetProject,
			PackageIdentity packageIdentity,
			ResolutionContext resolutionContext,
			INuGetProjectContext nuGetProjectContext,
			IEnumerable<SourceRepository> primarySources,
			IEnumerable<SourceRepository> secondarySources,
			CancellationToken token)
		{
			PreviewInstallProject = nuGetProject;
			PreviewInstallPackageIdentity = packageIdentity;
			PreviewInstallResolutionContext = resolutionContext;
			PreviewInstallPrimarySources = primarySources.ToList ();
			PreviewInstallSecondarySources = secondarySources;
			PreviewInstallCancellationToken = token;

			IEnumerable<NuGetProjectAction> actions = InstallActions.ToArray ();
			return Task.FromResult (actions);
		}

		public List<PackageIdentity> PackagesInPackagesFolder = new List<PackageIdentity> ();

		public bool PackageExistsInPackagesFolder (PackageIdentity packageIdentity)
		{
			return PackagesInPackagesFolder.Contains (packageIdentity);
		}
	}
}

